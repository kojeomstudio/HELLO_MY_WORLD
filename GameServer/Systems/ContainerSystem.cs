using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameServerApp.Database;
using GameServerApp.Models;
using SharedProtocol;
using ProtocolItemType = SharedProtocol.ItemType;

namespace GameServerApp.Systems
{
    public class ContainerSystem
    {
        private readonly DatabaseHelper _database;
        private readonly SessionManager _sessions;
        private readonly Dictionary<ContainerKey, ContainerInstance> _containersByKey = new();
        private readonly Dictionary<int, ContainerInstance> _containersById = new();
        private readonly Dictionary<string, HashSet<int>> _subscriptionsByPlayer = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _syncRoot = new();

        private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public ContainerSystem(DatabaseHelper database, SessionManager sessions)
        {
            _database = database;
            _sessions = sessions;
            _sessions.SessionRemoved += HandleSessionRemoved;
        }

        public async Task HandleOpenAsync(Session session, ContainerOpenRequestMessage request)
        {
            var playerName = session.UserName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                await SendOpenFailureAsync(session, "Session is not authenticated.");
                return;
            }

            var position = request.Position ?? new Vector3I();
            var worldId = await _database.GetDefaultWorldIdAsync();
            var key = new ContainerKey(worldId, position.X, position.Y, position.Z, request.ContainerType);

            ContainerInstance instance;
            lock (_syncRoot)
            {
                if (_containersByKey.TryGetValue(key, out instance!))
                {
                    instance.Subscribers.Add(playerName);
                    TrackPlayerSubscription(playerName, instance.ContainerId);
                }
            }

            if (instance == null)
            {
                var record = await _database.LoadContainerAsync(worldId, position.X, position.Y, position.Z, request.ContainerType);
                if (record == null)
                {
                    var slotCount = GetDefaultSlotCount(request.ContainerType);
                    var blankSnapshot = BuildEmptySnapshotJson(slotCount);
                    var containerId = await _database.InsertContainerAsync(worldId, position.X, position.Y, position.Z, request.ContainerType, slotCount, blankSnapshot);
                    record = new ContainerRecord
                    {
                        Id = containerId,
                        WorldId = worldId,
                        X = position.X,
                        Y = position.Y,
                        Z = position.Z,
                        ContainerType = request.ContainerType,
                        SlotCount = slotCount,
                        ItemsJson = blankSnapshot,
                        LastUpdatedUtc = DateTime.UtcNow
                    };
                }

                instance = BuildInstanceFromRecord(record);
                instance.Subscribers.Add(playerName);
                TrackPlayerSubscription(playerName, instance.ContainerId);

                lock (_syncRoot)
                {
                    _containersByKey[key] = instance;
                    _containersById[instance.ContainerId] = instance;
                }
            }

            var response = new ContainerOpenResponseMessage
            {
                Success = true,
                ContainerId = instance.ContainerId,
                Slots = BuildSlotUpdates(instance),
                ContainerTitle = BuildContainerTitle(instance.ContainerType),
                Properties = BuildProperties(instance),
                ErrorMessage = string.Empty
            };

            await SendMinecraftAsync(session, MinecraftMessageType.ContainerOpen, response);
        }

        public async Task HandleCloseAsync(Session session, ContainerCloseRequestMessage request)
        {
            var playerName = session.UserName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            var container = GetContainerById(request.ContainerId);
            if (container == null)
            {
                return;
            }

            bool removedSubscriber;
            lock (_syncRoot)
            {
                removedSubscriber = container.Subscribers.Remove(playerName);
                if (_subscriptionsByPlayer.TryGetValue(playerName, out var set))
                {
                    set.Remove(container.ContainerId);
                    if (set.Count == 0)
                    {
                        _subscriptionsByPlayer.Remove(playerName);
                    }
                }
            }

            if (!removedSubscriber)
            {
                return;
            }

            var notification = new ContainerCloseNotificationMessage
            {
                ContainerId = container.ContainerId,
                Reason = "client_closed"
            };

            await BroadcastToSubscribersAsync(container, notification, session);
        }

        public async Task HandleUpdateAsync(Session session, ContainerUpdateRequestMessage request)
        {
            if (request.SlotUpdates == null || request.SlotUpdates.Count == 0)
            {
                return;
            }

            var container = GetContainerById(request.ContainerId);
            if (container == null)
            {
                await SendOpenFailureAsync(session, "Container no longer active on server.");
                return;
            }

            var playerName = session.UserName;
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                lock (_syncRoot)
                {
                    container.Subscribers.Add(playerName);
                    TrackPlayerSubscription(playerName, container.ContainerId);
                }
            }

            var appliedSlots = new List<SlotUpdate>(request.SlotUpdates.Count);
            foreach (var update in request.SlotUpdates)
            {
                if (update == null)
                {
                    continue;
                }

                var slotIndex = update.Slot;
                if (slotIndex < 0 || slotIndex >= container.SlotCount)
                {
                    continue;
                }

                var identifier = DetermineIdentifier(update);
                var quantity = update.Item?.Quantity ?? 0;
                var customData = update.Item?.CustomData ?? string.Empty;
                var slotState = new ContainerSlotState(slotIndex, identifier, quantity, customData);

                lock (_syncRoot)
                {
                    container.SetSlot(slotState);
                }

                appliedSlots.Add(BuildSlotUpdate(slotState));
            }

            if (appliedSlots.Count == 0 && !request.ForceFullSync)
            {
                return;
            }

            await PersistContainerAsync(container);

            var broadcast = new ContainerUpdateBroadcastMessage
            {
                ContainerId = container.ContainerId,
                SlotUpdates = request.ForceFullSync ? BuildSlotUpdates(container) : appliedSlots,
                Properties = BuildProperties(container),
                IsFullSync = request.ForceFullSync
            };

            await BroadcastToSubscribersAsync(container, broadcast, null);
        }

        private ContainerInstance BuildInstanceFromRecord(ContainerRecord record)
        {
            var slots = ParseSnapshot(record.ItemsJson);
            return new ContainerInstance(
                new ContainerKey(record.WorldId, record.X, record.Y, record.Z, record.ContainerType),
                record.Id,
                record.ContainerType,
                record.SlotCount,
                slots)
            {
                LastUpdated = record.LastUpdatedUtc
            };
        }

        private static string DetermineIdentifier(SlotUpdate update)
        {
            if (!string.IsNullOrWhiteSpace(update.ItemIdentifier))
            {
                return update.ItemIdentifier;
            }

            if (!string.IsNullOrWhiteSpace(update.Item?.CustomData))
            {
                return update.Item!.CustomData;
            }

            return update.Item?.ItemName ?? string.Empty;
        }

        private async Task SendOpenFailureAsync(Session session, string message)
        {
            var response = new ContainerOpenResponseMessage
            {
                Success = false,
                ContainerId = -1,
                Slots = new List<SlotUpdate>(),
                ContainerTitle = string.Empty,
                Properties = new ContainerProperties(),
                ErrorMessage = message
            };

            await SendMinecraftAsync(session, MinecraftMessageType.ContainerOpen, response);
        }

        private ContainerInstance? GetContainerById(int containerId)
        {
            lock (_syncRoot)
            {
                return _containersById.TryGetValue(containerId, out var instance) ? instance : null;
            }
        }

        private async Task PersistContainerAsync(ContainerInstance instance)
        {
            var snapshotJson = BuildSnapshotJson(instance);
            await _database.UpdateContainerItemsAsync(instance.ContainerId, instance.SlotCount, snapshotJson);
            instance.LastUpdated = DateTime.UtcNow;
        }

        private static async Task SendMinecraftAsync(Session session, MinecraftMessageType type, object message)
        {
            await using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, message);
            await session.SendAsync((int)type, stream.ToArray());
        }

        private async Task BroadcastToSubscribersAsync<T>(ContainerInstance instance, T message, Session? exclude)
            where T : class
        {
            Session[] targets;
            lock (_syncRoot)
            {
                var candidateNames = instance.Subscribers.ToArray();
                var resolved = new List<Session>(candidateNames.Length);
                foreach (var name in candidateNames)
                {
                    var session = _sessions.GetSession(name);
                    if (session != null && session != exclude)
                    {
                        resolved.Add(session);
                    }
                }

                targets = resolved.ToArray();
            }

            if (targets.Length == 0)
            {
                return;
            }

            await using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, message);
            var payload = stream.ToArray();

            var tasks = targets.Select(s => s.SendAsync((int)ResolveMessageType(message), payload));
            await Task.WhenAll(tasks);
        }

        private static MinecraftMessageType ResolveMessageType<T>(T message)
            where T : class
        {
            return message switch
            {
                ContainerUpdateBroadcastMessage => MinecraftMessageType.ContainerUpdate,
                ContainerCloseNotificationMessage => MinecraftMessageType.ContainerClose,
                ContainerOpenResponseMessage => MinecraftMessageType.ContainerOpen,
                _ => MinecraftMessageType.ContainerUpdate
            };
        }

        private static List<SlotUpdate> BuildSlotUpdates(ContainerInstance instance)
        {
            var updates = new List<SlotUpdate>(instance.SlotCount);
            foreach (var slot in instance.Slots.Values.OrderBy(s => s.Slot))
            {
                updates.Add(BuildSlotUpdate(slot));
            }

            return updates;
        }

        private static SlotUpdate BuildSlotUpdate(ContainerSlotState slot)
        {
            return new SlotUpdate
            {
                Slot = slot.Slot,
                Item = CreateInventoryItem(slot.ItemIdentifier, slot.Amount, slot.CustomData),
                ItemIdentifier = slot.ItemIdentifier
            };
        }

        private static InventoryItemInfo CreateInventoryItem(string identifier, int amount, string customData)
        {
            if (string.IsNullOrWhiteSpace(identifier) || amount <= 0)
            {
                return new InventoryItemInfo();
            }

            var itemType = GuessItemType(identifier);
            return new InventoryItemInfo
            {
                ItemId = ComputeStableItemId(identifier),
                ItemName = FormatItemName(identifier),
                Quantity = amount,
                Durability = 0,
                MaxDurability = 0,
                CustomData = customData ?? string.Empty,
                ItemType = itemType
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

        private static ProtocolItemType GuessItemType(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return ProtocolItemType.Material;
            }

            var normalized = identifier.ToLowerInvariant();
            if (normalized.Contains("sword") || normalized.Contains("bow") || normalized.Contains("trident"))
            {
                return ProtocolItemType.Weapon;
            }

            if (normalized.Contains("pickaxe") || normalized.Contains("axe") || normalized.Contains("shovel") || normalized.Contains("hoe"))
            {
                return ProtocolItemType.Tool;
            }

            if (normalized.Contains("helmet") || normalized.Contains("chestplate") || normalized.Contains("leggings") || normalized.Contains("boots"))
            {
                return ProtocolItemType.Armor;
            }

            if (normalized.Contains("bread") || normalized.Contains("apple") || normalized.Contains("stew") || normalized.Contains("food"))
            {
                return ProtocolItemType.Food;
            }

            if (normalized.Contains("log") || normalized.Contains("plank") || normalized.Contains("stone") || normalized.Contains("block"))
            {
                return ProtocolItemType.Block;
            }

            return ProtocolItemType.Material;
        }

        private static int ComputeStableItemId(string identifier)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(identifier));
            return BitConverter.ToInt32(hash, 0) & int.MaxValue;
        }

        private static int GetDefaultSlotCount(ContainerType type)
        {
            return type switch
            {
                ContainerType.Furnace => 3,
                ContainerType.CraftingTable => 9,
                ContainerType.EnchantingTable => 2,
                ContainerType.BrewingStand => 5,
                ContainerType.Dispenser => 9,
                ContainerType.Hopper => 5,
                ContainerType.Beacon => 1,
                ContainerType.Anvil => 3,
                _ => 27
            };
        }

        private static string BuildContainerTitle(ContainerType type)
        {
            return type switch
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

        private static ContainerProperties BuildProperties(ContainerInstance instance)
        {
            var properties = new ContainerProperties
            {
                SlotCount = instance.SlotCount,
                FuelSlot = -1,
                ResultSlot = -1,
                Progress = 0f
            };

            switch (instance.ContainerType)
            {
                case ContainerType.Furnace:
                    properties.FuelSlot = 1;
                    properties.ResultSlot = 2;
                    break;
                case ContainerType.CraftingTable:
                    properties.ResultSlot = instance.SlotCount - 1;
                    break;
                case ContainerType.BrewingStand:
                    properties.FuelSlot = 4;
                    properties.ResultSlot = 3;
                    break;
                case ContainerType.Anvil:
                    properties.ResultSlot = 2;
                    break;
            }

            return properties;
        }

        private static string BuildSnapshotJson(ContainerInstance instance)
        {
            var payload = new ContainerSnapshotPayload
            {
                SlotCount = instance.SlotCount,
                SavedAtUtc = instance.LastUpdated,
                Slots = instance.Slots.Values
                    .Where(slot => !slot.IsEmpty)
                    .Select(slot => new ContainerSlotPayload
                    {
                        Slot = slot.Slot,
                        ItemIdentifier = slot.ItemIdentifier,
                        Amount = slot.Amount,
                        ItemData = slot.CustomData
                    })
                    .ToList()
            };

            return JsonSerializer.Serialize(payload, SnapshotSerializerOptions);
        }

        private static string BuildEmptySnapshotJson(int slotCount)
        {
            var payload = new ContainerSnapshotPayload
            {
                SlotCount = slotCount,
                SavedAtUtc = DateTime.UtcNow
            };
            return JsonSerializer.Serialize(payload, SnapshotSerializerOptions);
        }

        private static Dictionary<int, ContainerSlotState> ParseSnapshot(string json)
        {
            var result = new Dictionary<int, ContainerSlotState>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<ContainerSnapshotPayload>(json, SnapshotSerializerOptions);
                if (payload?.Slots == null)
                {
                    return result;
                }

                foreach (var slot in payload.Slots)
                {
                    if (slot.Slot < 0)
                    {
                        continue;
                    }

                    result[slot.Slot] = new ContainerSlotState(slot.Slot, slot.ItemIdentifier ?? string.Empty, slot.Amount, slot.ItemData ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                // Ignore malformed payloads and return empty state.
            }

            return result;
        }

        private void TrackPlayerSubscription(string playerName, int containerId)
        {
            lock (_syncRoot)
            {
                if (!_subscriptionsByPlayer.TryGetValue(playerName, out var set))
                {
                    set = new HashSet<int>();
                    _subscriptionsByPlayer[playerName] = set;
                }

                set.Add(containerId);
            }
        }

        private void HandleSessionRemoved(Session session)
        {
            if (string.IsNullOrWhiteSpace(session.UserName))
            {
                return;
            }

            HashSet<int>? subscriptions;
            lock (_syncRoot)
            {
                if (!_subscriptionsByPlayer.Remove(session.UserName, out subscriptions))
                {
                    return;
                }
            }

            foreach (var containerId in subscriptions)
            {
                var container = GetContainerById(containerId);
                if (container == null)
                {
                    continue;
                }

                lock (_syncRoot)
                {
                    container.Subscribers.Remove(session.UserName!);
                }
            }
        }

        private readonly record struct ContainerKey(int WorldId, int X, int Y, int Z, ContainerType ContainerType);

        private sealed class ContainerInstance
        {
            public ContainerInstance(ContainerKey key, int containerId, ContainerType containerType, int slotCount, Dictionary<int, ContainerSlotState> slots)
            {
                Key = key;
                ContainerId = containerId;
                ContainerType = containerType;
                SlotCount = slotCount;
                Slots = slots;
            }

            public ContainerKey Key { get; }
            public int ContainerId { get; }
            public ContainerType ContainerType { get; }
            public int SlotCount { get; }
            public Dictionary<int, ContainerSlotState> Slots { get; }
            public HashSet<string> Subscribers { get; } = new(StringComparer.OrdinalIgnoreCase);
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

            public void SetSlot(ContainerSlotState slot)
            {
                if (slot.IsEmpty)
                {
                    Slots.Remove(slot.Slot);
                }
                else
                {
                    Slots[slot.Slot] = slot;
                }
            }
        }

        private sealed class ContainerSlotState
        {
            public ContainerSlotState(int slot, string itemIdentifier, int amount, string customData)
            {
                Slot = slot;
                ItemIdentifier = itemIdentifier ?? string.Empty;
                Amount = amount;
                CustomData = customData ?? string.Empty;
            }

            public int Slot { get; }
            public string ItemIdentifier { get; }
            public int Amount { get; }
            public string CustomData { get; }
            public bool IsEmpty => Amount <= 0 || string.IsNullOrWhiteSpace(ItemIdentifier);
        }

        private sealed class ContainerSnapshotPayload
        {
            public int SlotCount { get; set; }
            public List<ContainerSlotPayload> Slots { get; set; } = new();
            public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        }

        private sealed class ContainerSlotPayload
        {
            public int Slot { get; set; }
            public string? ItemIdentifier { get; set; }
            public int Amount { get; set; }
            public string? ItemData { get; set; }
        }
    }
}

