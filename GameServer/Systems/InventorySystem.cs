using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Systems;

/// <summary>
/// Runtime inventory coordinator that keeps server snapshots in sync with persistent storage.
/// </summary>
public class InventorySystem
{
    private readonly DatabaseHelper _database;
    private readonly Dictionary<string, PlayerInventory> _playerInventories;
    private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public InventorySystem(DatabaseHelper database)
    {
        _database = database;
        _playerInventories = new Dictionary<string, PlayerInventory>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PlayerInventory> GetPlayerInventoryAsync(string userName)
    {
        if (_playerInventories.TryGetValue(userName, out var cached))
        {
            return cached;
        }

        var inventory = await LoadPlayerInventoryFromDatabase(userName);
        if (inventory == null)
        {
            inventory = CreateDefaultInventory(userName);
            await SavePlayerInventoryToDatabase(userName, inventory);
        }

        _playerInventories[userName] = inventory;
        return inventory;
    }

    public async Task SavePlayerInventoryAsync(string userName, PlayerInventory inventory)
    {
        inventory.LastUpdate = DateTime.UtcNow;
        _playerInventories[userName] = inventory;
        await SavePlayerInventoryToDatabase(userName, inventory);
    }

    public async Task PersistSnapshotAsync(string userName)
    {
        if (!_playerInventories.TryGetValue(userName, out var inventory))
        {
            var loaded = await LoadPlayerInventoryFromDatabase(userName);
            if (loaded != null)
            {
                await SavePlayerInventoryToDatabase(userName, loaded);
            }
            return;
        }

        await SavePlayerInventoryToDatabase(userName, inventory);
    }

    public List<InventorySlotData> CreateSlotSnapshot(PlayerInventory inventory)
    {
        var result = new List<InventorySlotData>(inventory.Slots.Length);
        foreach (var slot in inventory.Slots)
        {
            result.Add(new InventorySlotData
            {
                SlotIndex = slot.SlotIndex,
                ItemId = slot.ItemId,
                Amount = slot.Amount,
                ItemData = slot.ItemData
            });
        }

        return result;
    }

    private async Task<PlayerInventory?> LoadPlayerInventoryFromDatabase(string userName)
    {
        var snapshotJson = await _database.LoadInventorySnapshotAsync(userName);
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return null;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<InventorySnapshotPayload>(snapshotJson, SnapshotSerializerOptions);
            if (snapshot == null)
            {
                return null;
            }

            var inventory = new PlayerInventory(userName)
            {
                LastUpdate = snapshot.SavedAtUtc
            };

            foreach (var slotPayload in snapshot.Slots)
            {
                var slot = inventory.GetSlot(slotPayload.SlotIndex);
                if (slot == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(slotPayload.ItemId) || slotPayload.Amount <= 0)
                {
                    slot.Clear();
                }
                else
                {
                    slot.SetItem(slotPayload.ItemId, slotPayload.Amount, slotPayload.ItemData);
                }
            }

            return inventory;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("[InventorySystem] Failed to deserialize inventory snapshot for " + userName + ": " + ex.Message);
            return null;
        }
    }

    private async Task SavePlayerInventoryToDatabase(string userName, PlayerInventory inventory)
    {
        var payload = new InventorySnapshotPayload
        {
            UserName = userName,
            SavedAtUtc = DateTime.UtcNow,
            Slots = inventory.Slots.Select(slot => new InventorySlotPayload
            {
                SlotIndex = slot.SlotIndex,
                ItemId = slot.ItemId,
                Amount = slot.Amount,
                ItemData = slot.ItemData
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, SnapshotSerializerOptions);
        await _database.SaveInventorySnapshotAsync(userName, json);
    }

    private static PlayerInventory CreateDefaultInventory(string userName)
    {
        var inventory = new PlayerInventory(userName);

        // Provide a light starter kit so players can interact immediately after onboarding.
        inventory.SetSlotItem(0, "wooden_pickaxe", 1);
        inventory.SetSlotItem(1, "torch", 16);
        inventory.SetSlotItem(8, "bread", 4);

        return inventory;
    }

    private sealed class InventorySnapshotPayload
    {
        public string UserName { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        public List<InventorySlotPayload> Slots { get; set; } = new();
    }

    private sealed class InventorySlotPayload
    {
        public int SlotIndex { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string ItemData { get; set; } = string.Empty;
    }
}

/// <summary>
/// Player inventory laid out to mirror Minecraft slot layout.
/// </summary>
public class PlayerInventory
{
    private const int HotbarSlots = 9;
    private const int MainInventorySlots = 27;
    private const int ArmorSlots = 4;
    private const int OffhandSlots = 1;
    private const int TotalSlots = HotbarSlots + MainInventorySlots + ArmorSlots + OffhandSlots;

    public string UserName { get; }
    public InventorySlot[] Slots { get; }
    public DateTime LastUpdate { get; set; }

    public PlayerInventory(string userName)
    {
        UserName = userName;
        Slots = new InventorySlot[TotalSlots];

        for (int i = 0; i < TotalSlots; i++)
        {
            Slots[i] = new InventorySlot(i);
        }

        LastUpdate = DateTime.UtcNow;
    }

    public bool AddItem(string itemId, int amount)
    {
        int remainingAmount = amount;
        int maxStackSize = GetMaxStackSize(itemId);

        foreach (var slot in Slots)
        {
            if (slot.ItemId == itemId && slot.Amount < maxStackSize)
            {
                int canAdd = Math.Min(remainingAmount, maxStackSize - slot.Amount);
                slot.Amount += canAdd;
                remainingAmount -= canAdd;

                if (remainingAmount <= 0)
                {
                    LastUpdate = DateTime.UtcNow;
                    return true;
                }
            }
        }

        while (remainingAmount > 0)
        {
            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                break;
            }

            int amountToAdd = Math.Min(remainingAmount, maxStackSize);
            emptySlot.SetItem(itemId, amountToAdd);
            remainingAmount -= amountToAdd;
        }

        if (remainingAmount == 0)
        {
            LastUpdate = DateTime.UtcNow;
        }

        return remainingAmount == 0;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        int totalAvailable = GetItemAmount(itemId);
        if (totalAvailable < amount)
        {
            return false;
        }

        int remainingToRemove = amount;
        foreach (var slot in Slots)
        {
            if (slot.ItemId == itemId && remainingToRemove > 0)
            {
                int removed = Math.Min(slot.Amount, remainingToRemove);
                slot.Amount -= removed;
                remainingToRemove -= removed;

                if (slot.Amount <= 0)
                {
                    slot.Clear();
                }
            }
        }

        LastUpdate = DateTime.UtcNow;
        return true;
    }

    public int GetItemAmount(string itemId)
    {
        return Slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Amount);
    }

    public InventorySlot? FindEmptySlot()
    {
        return Slots.FirstOrDefault(slot => slot.IsEmpty());
    }

    public InventorySlot? GetSlot(int index)
    {
        if (index < 0 || index >= TotalSlots)
        {
            return null;
        }

        return Slots[index];
    }

    public IEnumerable<InventorySlot> GetAllSlots() => Slots;
    public IEnumerable<InventorySlot> GetHotbarSlots() => Slots.Take(HotbarSlots);
    public IEnumerable<InventorySlot> GetMainInventorySlots() => Slots.Skip(HotbarSlots).Take(MainInventorySlots);
    public IEnumerable<InventorySlot> GetArmorSlots() => Slots.Skip(HotbarSlots + MainInventorySlots).Take(ArmorSlots);
    public InventorySlot GetOffhandSlot() => Slots[HotbarSlots + MainInventorySlots + ArmorSlots];

    public bool IsSlotValid(int slotIndex) => slotIndex >= 0 && slotIndex < TotalSlots;
    public bool IsHotbarSlot(int slotIndex) => slotIndex >= 0 && slotIndex < HotbarSlots;
    public bool IsMainInventorySlot(int slotIndex) => slotIndex >= HotbarSlots && slotIndex < HotbarSlots + MainInventorySlots;
    public bool IsArmorSlot(int slotIndex) => slotIndex >= HotbarSlots + MainInventorySlots && slotIndex < HotbarSlots + MainInventorySlots + ArmorSlots;
    public bool IsOffhandSlot(int slotIndex) => slotIndex == HotbarSlots + MainInventorySlots + ArmorSlots;

    public void SetSlotItem(int slotIndex, string itemId, int amount, string itemData = "")
    {
        var slot = GetSlot(slotIndex);
        if (slot == null)
        {
            return;
        }

        if (amount <= 0 || string.IsNullOrEmpty(itemId))
        {
            slot.Clear();
        }
        else
        {
            slot.SetItem(itemId, amount, itemData);
        }

        LastUpdate = DateTime.UtcNow;
    }

    private static int GetMaxStackSize(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 64;
        }

        return itemId switch
        {
            var item when item.Contains("sword", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("pickaxe", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("axe", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("shovel", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("hoe", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("helmet", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("chestplate", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("leggings", StringComparison.OrdinalIgnoreCase) => 1,
            var item when item.Contains("boots", StringComparison.OrdinalIgnoreCase) => 1,
            _ => 64
        };
    }
}

/// <summary>
/// Individual inventory slot with stack metadata.
/// </summary>
public class InventorySlot
{
    public int SlotIndex { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string ItemData { get; set; } = string.Empty;

    public InventorySlot(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public bool IsEmpty() => string.IsNullOrEmpty(ItemId) || Amount <= 0;

    public void SetItem(string itemId, int amount, string itemData = "")
    {
        ItemId = itemId;
        Amount = amount;
        ItemData = itemData;
    }

    public void Clear()
    {
        ItemId = string.Empty;
        Amount = 0;
        ItemData = string.Empty;
    }

    public bool CanStackWith(InventorySlot other)
    {
        return ItemId == other.ItemId && ItemData == other.ItemData;
    }

    public InventorySlot Clone()
    {
        return new InventorySlot(SlotIndex)
        {
            ItemId = ItemId,
            Amount = Amount,
            ItemData = ItemData
        };
    }
}
