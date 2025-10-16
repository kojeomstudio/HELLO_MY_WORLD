using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SharedProtocol;
using UnityEngine;

namespace Minecraft.Containers
{
    /// <summary>
    /// Maintains live container state on the client and relays updates back to the server.
    /// </summary>
    public class ContainerManager : MonoBehaviour
    {
        private MinecraftGameClient? _client;
        private readonly Dictionary<int, ContainerState> _containers = new();

        public event Action<ContainerState>? ContainerOpened;
        public event Action<ContainerState>? ContainerUpdated;
        public event Action<int>? ContainerClosed;

        private void Awake()
        {
            _client = FindObjectOfType<MinecraftGameClient>();
            if (_client == null)
            {
                Debug.LogWarning("ContainerManager could not locate a MinecraftGameClient in the scene.");
            }
        }

        private void OnEnable()
        {
            if (_client == null)
            {
                return;
            }

            _client.ContainerOpened += HandleContainerOpened;
            _client.ContainerUpdated += HandleContainerUpdated;
            _client.ContainerClosed += HandleContainerClosed;
        }

        private void OnDisable()
        {
            if (_client == null)
            {
                return;
            }

            _client.ContainerOpened -= HandleContainerOpened;
            _client.ContainerUpdated -= HandleContainerUpdated;
            _client.ContainerClosed -= HandleContainerClosed;
        }

        public IReadOnlyDictionary<int, ContainerState> Containers => _containers;

        public bool TryGetContainer(int containerId, out ContainerState state) => _containers.TryGetValue(containerId, out state);

        public void RequestOpen(Vector3Int position, ContainerType containerType)
        {
            _client?.RequestContainerOpen(position, containerType);
        }

        public void RequestClose(int containerId)
        {
            _client?.RequestContainerClose(containerId);
        }

        public void SubmitSlotUpdates(int containerId, IEnumerable<ContainerSlotUpdate> updates, bool forceFullSync = false)
        {
            if (_client == null)
            {
                Debug.LogWarning("Cannot submit container updates without an active MinecraftGameClient.");
                return;
            }

            if (updates == null)
            {
                return;
            }

            var currentHash = _containers.TryGetValue(containerId, out var knownState)
                ? knownState.SnapshotHash
                : string.Empty;

            var slotUpdates = updates
                .Where(update => update != null)
                .Select(CreateSlotUpdate)
                .Where(update => update != null)
                .Cast<SlotUpdate>()
                .ToList();

            _client.SendContainerUpdate(containerId, slotUpdates, forceFullSync, currentHash);
        }

        private void HandleContainerOpened(ContainerOpenResponseMessage response)
        {
            if (!response.Success)
            {
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    Debug.LogWarning($"Server rejected container open: {response.ErrorMessage}");
                }
                return;
            }

            var state = ContainerState.FromOpenResponse(response);
            _containers[state.ContainerId] = state;
            ContainerOpened?.Invoke(state);
        }

        private void HandleContainerUpdated(ContainerUpdateBroadcastMessage message)
        {
            if (!_containers.TryGetValue(message.ContainerId, out var state))
            {
                state = ContainerState.FromUpdate(message);
                _containers[state.ContainerId] = state;
                ContainerOpened?.Invoke(state);
                return;
            }

            state.ApplyUpdate(message);
            ContainerUpdated?.Invoke(state);
        }

        private void HandleContainerClosed(ContainerCloseNotificationMessage message)
        {
            if (_containers.Remove(message.ContainerId))
            {
                ContainerClosed?.Invoke(message.ContainerId);
            }
        }

        private static SlotUpdate? CreateSlotUpdate(ContainerSlotUpdate update)
        {
            if (update == null)
            {
                return null;
            }

            if (update.Quantity <= 0 || string.IsNullOrWhiteSpace(update.ItemIdentifier))
            {
                return new SlotUpdate
                {
                    Slot = update.Slot,
                    Item = new InventoryItemInfo(),
                    ItemIdentifier = string.Empty
                };
            }

            var itemInfo = new InventoryItemInfo
            {
                ItemId = ComputeStableItemId(update.ItemIdentifier!),
                ItemName = FormatItemName(update.ItemIdentifier!),
                Quantity = update.Quantity,
                Durability = update.Durability,
                MaxDurability = update.MaxDurability,
                CustomData = update.CustomData ?? string.Empty,
                ItemType = update.ItemType ?? GuessItemType(update.ItemIdentifier!)
            };

            return new SlotUpdate
            {
                Slot = update.Slot,
                Item = itemInfo,
                ItemIdentifier = update.ItemIdentifier!
            };
        }

        private static string FormatItemName(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return "Empty";
            }

            var core = identifier.Contains(':') ? identifier.Split(':')[1] : identifier;
            core = core.Replace('_', ' ');
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(core);
        }

        private static ItemType GuessItemType(string identifier)
        {
            var normalized = identifier.ToLowerInvariant();
            if (normalized.Contains("sword") || normalized.Contains("bow") || normalized.Contains("trident"))
            {
                return ItemType.Weapon;
            }

            if (normalized.Contains("pickaxe") || normalized.Contains("axe") || normalized.Contains("shovel") || normalized.Contains("hoe"))
            {
                return ItemType.Tool;
            }

            if (normalized.Contains("helmet") || normalized.Contains("chestplate") || normalized.Contains("leggings") || normalized.Contains("boots"))
            {
                return ItemType.Armor;
            }

            if (normalized.Contains("bread") || normalized.Contains("apple") || normalized.Contains("stew") || normalized.Contains("food"))
            {
                return ItemType.Food;
            }

            if (normalized.Contains("log") || normalized.Contains("plank") || normalized.Contains("stone") || normalized.Contains("block"))
            {
                return ItemType.Block;
            }

            return ItemType.Material;
        }

        private static int ComputeStableItemId(string identifier)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(identifier));
            return BitConverter.ToInt32(hash, 0) & int.MaxValue;
        }
    }

    public sealed class ContainerState
    {
        private readonly Dictionary<int, ContainerSlotState> _slots;

        private ContainerState(
            int containerId,
            ContainerType containerType,
            ContainerProperties properties,
            Dictionary<int, ContainerSlotState> slots,
            string snapshotHash,
            string title)
        {
            ContainerId = containerId;
            ContainerType = containerType;
            Properties = properties ?? new ContainerProperties();
            SnapshotHash = snapshotHash ?? string.Empty;
            Title = FormatTitle(containerType, title);
            _slots = slots;
        }

        public int ContainerId { get; }
        public ContainerType ContainerType { get; }
        public ContainerProperties Properties { get; private set; }
        public string SnapshotHash { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public IReadOnlyDictionary<int, ContainerSlotState> Slots => _slots;
        public int SlotCount => Properties?.SlotCount ?? 0;

        public static ContainerState FromOpenResponse(ContainerOpenResponseMessage response)
        {
            var slots = BuildSlotDictionary(response.Slots);
            var properties = response.Properties ?? new ContainerProperties();
            var snapshotHash = !string.IsNullOrWhiteSpace(response.SnapshotHash)
                ? response.SnapshotHash
                : ComputeLocalSnapshotHash(slots, properties.SlotCount);

            return new ContainerState(response.ContainerId, response.ContainerType, properties, slots, snapshotHash, response.ContainerTitle);
        }

        public static ContainerState FromUpdate(ContainerUpdateBroadcastMessage update)
        {
            var slots = BuildSlotDictionary(update.SlotUpdates);
            var properties = update.Properties ?? new ContainerProperties();
            var snapshotHash = !string.IsNullOrWhiteSpace(update.SnapshotHash)
                ? update.SnapshotHash
                : ComputeLocalSnapshotHash(slots, properties.SlotCount);

            var containerType = update.ContainerType;
            return new ContainerState(update.ContainerId, containerType, properties, slots, snapshotHash, string.Empty);
        }

        public void ApplyUpdate(ContainerUpdateBroadcastMessage update)
        {
            Properties = update.Properties ?? Properties;
            if (update.SlotUpdates != null)
            {
                foreach (var slotUpdate in update.SlotUpdates)
                {
                    var slotState = ContainerSlotState.FromSlotUpdate(slotUpdate);
                    if (slotState.IsEmpty)
                    {
                        _slots.Remove(slotUpdate.Slot);
                    }
                    else
                    {
                        _slots[slotState.Slot] = slotState;
                    }
                }

                if (update.IsFullSync)
                {
                    var validSlots = new HashSet<int>(update.SlotUpdates.Select(s => s.Slot));
                    var toRemove = _slots.Keys.Where(slot => !validSlots.Contains(slot)).ToList();
                    foreach (var slot in toRemove)
                    {
                        _slots.Remove(slot);
                    }
                }
            }

            SnapshotHash = !string.IsNullOrWhiteSpace(update.SnapshotHash)
                ? update.SnapshotHash
                : ComputeLocalSnapshotHash(_slots, SlotCount);
        }

        private static string FormatTitle(ContainerType containerType, string title)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title.Trim();
            }

            return containerType switch
            {
                ContainerType.Furnace => "Furnace",
                ContainerType.CraftingTable => "Crafting Table",
                ContainerType.EnchantingTable => "Enchanting Table",
                ContainerType.BrewingStand => "Brewing Stand",
                ContainerType.Dispenser => "Dispenser",
                ContainerType.Hopper => "Hopper",
                ContainerType.Beacon => "Beacon",
                ContainerType.Anvil => "Anvil",
                _ => "Chest"
            };
        }

        private static string ComputeLocalSnapshotHash(Dictionary<int, ContainerSlotState> slots, int slotCount)
        {
            var builder = new StringBuilder();
            builder.Append(slotCount).Append('|');

            foreach (var entry in slots.Values.OrderBy(s => s.Slot))
            {
                var item = entry.Item ?? new InventoryItemInfo();
                builder
                    .Append(entry.Slot).Append(':')
                    .Append(entry.ItemIdentifier).Append(':')
                    .Append(item.Quantity).Append(':')
                    .Append(item.CustomData ?? string.Empty)
                    .Append(';');
            }

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(hash);
        }

        private static Dictionary<int, ContainerSlotState> BuildSlotDictionary(IEnumerable<SlotUpdate>? updates)
        {
            var dict = new Dictionary<int, ContainerSlotState>();
            if (updates == null)
            {
                return dict;
            }

            foreach (var update in updates)
            {
                if (update == null)
                {
                    continue;
                }

                var state = ContainerSlotState.FromSlotUpdate(update);
                if (state.IsEmpty)
                {
                    continue;
                }

                dict[state.Slot] = state;
            }

            return dict;
        }
    }

    public sealed class ContainerSlotState
    {
        private ContainerSlotState(int slot, string itemIdentifier, InventoryItemInfo item)
        {
            Slot = slot;
            ItemIdentifier = itemIdentifier;
            Item = item;
        }

        public int Slot { get; }
        public string ItemIdentifier { get; }
        public InventoryItemInfo Item { get; }
        public bool IsEmpty => Item == null || Item.Quantity <= 0 || string.IsNullOrWhiteSpace(ItemIdentifier);

        public static ContainerSlotState FromSlotUpdate(SlotUpdate update)
        {
            if (update == null)
            {
                return new ContainerSlotState(0, string.Empty, new InventoryItemInfo());
            }

            var identifier = !string.IsNullOrWhiteSpace(update.ItemIdentifier)
                ? update.ItemIdentifier
                : update.Item?.CustomData ?? string.Empty;

            var item = update.Item ?? new InventoryItemInfo();
            return new ContainerSlotState(update.Slot, identifier, item);
        }
    }

    /// <summary>
    /// Lightweight DTO used by UI or gameplay input to describe desired slot mutations.
    /// </summary>
    [Serializable]
    public sealed class ContainerSlotUpdate
    {
        public int Slot { get; set; }
        public string? ItemIdentifier { get; set; }
        public int Quantity { get; set; }
        public string? CustomData { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public ItemType? ItemType { get; set; }
    }
}
