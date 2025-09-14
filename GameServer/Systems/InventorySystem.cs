using GameServerApp.Database;
using System.Text.Json;

namespace GameServerApp.Handlers;

/// <summary>
/// 인벤토리 시스템 - 플레이어 인벤토리 관리
/// </summary>
public class InventorySystem
{
    private readonly DatabaseHelper _database;
    private readonly Dictionary<string, PlayerInventory> _playerInventories;

    public InventorySystem(DatabaseHelper database)
    {
        _database = database;
        _playerInventories = new Dictionary<string, PlayerInventory>();
    }

    public async Task<PlayerInventory?> GetPlayerInventoryAsync(string userName)
    {
        if (_playerInventories.TryGetValue(userName, out var inventory))
        {
            return inventory;
        }

        inventory = await LoadPlayerInventoryFromDatabase(userName);
        if (inventory != null)
        {
            _playerInventories[userName] = inventory;
        }

        return inventory;
    }

    public async Task SavePlayerInventoryAsync(string userName, PlayerInventory inventory)
    {
        _playerInventories[userName] = inventory;
        await SavePlayerInventoryToDatabase(userName, inventory);
    }

    private async Task<PlayerInventory?> LoadPlayerInventoryFromDatabase(string userName)
    {
        await Task.Delay(10); // DB 접근 시뮬레이션
        
        // TODO: 실제 데이터베이스에서 인벤토리 로드
        // 현재는 기본 인벤토리를 생성
        return new PlayerInventory(userName);
    }

    private async Task SavePlayerInventoryToDatabase(string userName, PlayerInventory inventory)
    {
        await Task.Delay(10); // DB 저장 시뮬레이션
        
        // TODO: 실제 데이터베이스에 인벤토리 저장
        Console.WriteLine($"Inventory saved for player: {userName}");
    }

    public async Task<bool> AddItemAsync(string userName, string itemId, int amount)
    {
        var inventory = await GetPlayerInventoryAsync(userName);
        if (inventory == null) return false;

        return inventory.AddItem(itemId, amount);
    }

    public async Task<bool> RemoveItemAsync(string userName, string itemId, int amount)
    {
        var inventory = await GetPlayerInventoryAsync(userName);
        if (inventory == null) return false;

        return inventory.RemoveItem(itemId, amount);
    }
}

/// <summary>
/// 플레이어 인벤토리
/// </summary>
public class PlayerInventory
{
    private const int HOTBAR_SLOTS = 9;
    private const int MAIN_INVENTORY_SLOTS = 27;
    private const int ARMOR_SLOTS = 4;
    private const int OFFHAND_SLOTS = 1;
    private const int TOTAL_SLOTS = HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS + ARMOR_SLOTS + OFFHAND_SLOTS;

    public string UserName { get; set; }
    public InventorySlot[] Slots { get; set; }
    public DateTime LastUpdate { get; set; }

    public PlayerInventory(string userName)
    {
        UserName = userName;
        Slots = new InventorySlot[TOTAL_SLOTS];
        
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            Slots[i] = new InventorySlot(i);
        }
        
        LastUpdate = DateTime.UtcNow;
    }

    public bool AddItem(string itemId, int amount)
    {
        int remainingAmount = amount;
        int maxStackSize = GetMaxStackSize(itemId);

        // 먼저 기존 스택에 추가 시도
        foreach (var slot in Slots)
        {
            if (slot.ItemId == itemId && slot.Amount < maxStackSize)
            {
                int canAdd = Math.Min(remainingAmount, maxStackSize - slot.Amount);
                slot.Amount += canAdd;
                remainingAmount -= canAdd;

                if (remainingAmount <= 0)
                    break;
            }
        }

        // 남은 아이템을 빈 슬롯에 추가
        while (remainingAmount > 0)
        {
            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
                break;

            int amountToAdd = Math.Min(remainingAmount, maxStackSize);
            emptySlot.SetItem(itemId, amountToAdd);
            remainingAmount -= amountToAdd;
        }

        LastUpdate = DateTime.UtcNow;
        return remainingAmount == 0;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        int totalAvailable = GetItemAmount(itemId);
        if (totalAvailable < amount)
            return false;

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
        if (index < 0 || index >= TOTAL_SLOTS)
            return null;
        return Slots[index];
    }

    public IEnumerable<InventorySlot> GetAllSlots()
    {
        return Slots;
    }

    public IEnumerable<InventorySlot> GetHotbarSlots()
    {
        return Slots.Take(HOTBAR_SLOTS);
    }

    public IEnumerable<InventorySlot> GetMainInventorySlots()
    {
        return Slots.Skip(HOTBAR_SLOTS).Take(MAIN_INVENTORY_SLOTS);
    }

    public IEnumerable<InventorySlot> GetArmorSlots()
    {
        return Slots.Skip(HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS).Take(ARMOR_SLOTS);
    }

    public InventorySlot GetOffhandSlot()
    {
        return Slots[HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS + ARMOR_SLOTS];
    }

    private int GetMaxStackSize(string itemId)
    {
        return itemId switch
        {
            // 도구류 - 스택 불가
            var item when item.Contains("sword") => 1,
            var item when item.Contains("pickaxe") => 1,
            var item when item.Contains("axe") => 1,
            var item when item.Contains("shovel") => 1,
            var item when item.Contains("hoe") => 1,
            // 방어구 - 스택 불가
            var item when item.Contains("helmet") => 1,
            var item when item.Contains("chestplate") => 1,
            var item when item.Contains("leggings") => 1,
            var item when item.Contains("boots") => 1,
            // 기본 아이템 - 64개 스택
            _ => 64
        };
    }

    public bool IsSlotValid(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < TOTAL_SLOTS;
    }

    public bool IsHotbarSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < HOTBAR_SLOTS;
    }

    public bool IsMainInventorySlot(int slotIndex)
    {
        return slotIndex >= HOTBAR_SLOTS && slotIndex < HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS;
    }

    public bool IsArmorSlot(int slotIndex)
    {
        return slotIndex >= HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS && 
               slotIndex < HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS + ARMOR_SLOTS;
    }

    public bool IsOffhandSlot(int slotIndex)
    {
        return slotIndex == HOTBAR_SLOTS + MAIN_INVENTORY_SLOTS + ARMOR_SLOTS;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}

/// <summary>
/// 인벤토리 슬롯
/// </summary>
public class InventorySlot
{
    public int SlotIndex { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; } = 0;
    public string ItemData { get; set; } = string.Empty; // 추가 아이템 데이터 (내구도, 마법부여 등)

    public InventorySlot(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(ItemId) || Amount <= 0;
    }

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