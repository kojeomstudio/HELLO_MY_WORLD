using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Inventory system for managing player items and blocks.
/// Uses JSON data first and falls back to built-in defaults.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Configuration")]
    public int hotbarSize = 9;
    public int mainInventorySize = 27;
    public int maxStackSize = 64;

    [Header("Data Source")]
    public string streamingItemsFileName = "items.json";
    public string configItemsRelativePath = "config/game-data/items.json";

    [Header("UI References")]
    public GameObject inventoryUI;
    public Transform hotbarPanel;
    public Transform mainInventoryPanel;

    private InventorySlot[] hotbarSlots = Array.Empty<InventorySlot>();
    private InventorySlot[] mainInventorySlots = Array.Empty<InventorySlot>();
    private readonly Dictionary<int, ItemData> itemDatabase = new Dictionary<int, ItemData>();
    private readonly Dictionary<string, int> itemIdByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private int selectedSlot;

    public delegate void InventoryUpdateHandler();
    public event InventoryUpdateHandler OnInventoryChanged;
    public event InventoryUpdateHandler OnHotbarChanged;

    private void Start()
    {
        InitializeInventory();
        EnsureItemDatabaseLoaded();
    }

    public void EnsureItemDatabaseLoaded()
    {
        if (itemDatabase.Count > 0)
        {
            return;
        }

        LoadItemDatabase();
    }

    private void InitializeInventory()
    {
        hotbarSlots = new InventorySlot[Mathf.Max(1, hotbarSize)];
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i] = new InventorySlot();
        }

        mainInventorySlots = new InventorySlot[Mathf.Max(1, mainInventorySize)];
        for (int i = 0; i < mainInventorySlots.Length; i++)
        {
            mainInventorySlots[i] = new InventorySlot();
        }

        AddStartingItems();
    }

    private void LoadItemDatabase()
    {
        itemDatabase.Clear();
        itemIdByKey.Clear();

        bool loadedFromJson = TryLoadItemDatabaseFromJson();
        if (!loadedFromJson)
        {
            CreateDefaultItems();
            Debug.LogWarning("[InventoryManager] Could not load item JSON. Using default item set.");
        }

        if (itemDatabase.Count == 0)
        {
            CreateDefaultItems();
            Debug.LogWarning("[InventoryManager] Item database was empty after JSON load. Using default item set.");
        }
    }

    private bool TryLoadItemDatabaseFromJson()
    {
        string[] candidates = BuildItemJsonCandidates();
        foreach (string path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                string json = File.ReadAllText(path);
                var root = JToken.Parse(json);
                int loadedCount = ParseItemRoot(root);
                if (loadedCount > 0)
                {
                    Debug.Log($"[InventoryManager] Loaded {loadedCount} items from '{path}'.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventoryManager] Failed to parse '{path}': {ex.Message}");
            }
        }

        return false;
    }

    private string[] BuildItemJsonCandidates()
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            if (seen.Add(fullPath))
            {
                paths.Add(fullPath);
            }
        }

        AddCandidate(Path.Combine(Application.streamingAssetsPath, "game-data", "items.json"));

        if (!string.IsNullOrWhiteSpace(streamingItemsFileName))
        {
            AddCandidate(Path.Combine(Application.streamingAssetsPath, streamingItemsFileName));
        }

        if (!string.IsNullOrWhiteSpace(configItemsRelativePath))
        {
            AddCandidate(Path.Combine(Application.dataPath, "..", configItemsRelativePath));
        }

        AddCandidate(Path.Combine(Application.dataPath, "..", "config", "game-data", "items.json"));
        AddCandidate(Path.Combine(Application.dataPath, "..", "config", "items.json"));
        return paths.ToArray();
    }

    private int ParseItemRoot(JToken root)
    {
        int beforeCount = itemDatabase.Count;

        if (root is JArray arrayRoot)
        {
            foreach (JToken token in arrayRoot)
            {
                if (token is not JObject itemObject)
                {
                    continue;
                }

                ParseItemFromArray(itemObject);
            }

            return itemDatabase.Count - beforeCount;
        }

        if (root is not JObject objectRoot)
        {
            return 0;
        }

        JToken? itemsToken = objectRoot["items"];
        if (itemsToken is JObject itemsObject)
        {
            foreach (JProperty category in itemsObject.Properties())
            {
                if (category.Value is not JObject categoryObject)
                {
                    continue;
                }

                foreach (JProperty entry in categoryObject.Properties())
                {
                    if (entry.Value is not JObject itemObject)
                    {
                        continue;
                    }

                    ParseItemFromCatalog(entry.Name, category.Name, itemObject);
                }
            }
        }

        return itemDatabase.Count - beforeCount;
    }

    private void ParseItemFromArray(JObject itemObject)
    {
        string key = itemObject["id"]?.Value<string>() ?? string.Empty;
        int id = ResolveItemId(itemObject["id"], key);
        if (id < 0)
        {
            return;
        }

        string name = itemObject["name"]?.Value<string>()
            ?? itemObject["displayName"]?.Value<string>()
            ?? key;
        string typeText = itemObject["type"]?.Value<string>() ?? "material";

        int stackSize = itemObject["stack_max"]?.Value<int?>()
            ?? itemObject["max_stack"]?.Value<int?>()
            ?? itemObject["maxStack"]?.Value<int?>()
            ?? (itemObject["stackable"]?.Value<bool?>() == false ? 1 : maxStackSize);

        var itemData = new ItemData
        {
            id = id,
            name = string.IsNullOrWhiteSpace(name) ? $"item_{id}" : name,
            maxStack = Mathf.Max(1, stackSize),
            type = ParseItemType(typeText),
            nutrition = itemObject["hunger_restore"]?.Value<int?>()
                ?? itemObject["nutrition"]?.Value<int?>()
                ?? 0,
            durability = itemObject["durability"]?.Value<int?>() ?? 0
        };

        RegisterItem(itemData, key);
    }

    private void ParseItemFromCatalog(string key, string categoryName, JObject itemObject)
    {
        int id = ResolveItemId(itemObject["id"], key);
        if (id < 0)
        {
            return;
        }

        string name = itemObject["displayName"]?.Value<string>()
            ?? itemObject["name"]?.Value<string>()
            ?? key;

        string typeText = itemObject["type"]?.Value<string>() ?? categoryName;
        int stackSize = itemObject["stack_max"]?.Value<int?>()
            ?? itemObject["max_stack"]?.Value<int?>()
            ?? itemObject["stackSize"]?.Value<int?>()
            ?? itemObject["maxStack"]?.Value<int?>()
            ?? InferDefaultStackSize(typeText);

        var itemData = new ItemData
        {
            id = id,
            name = string.IsNullOrWhiteSpace(name) ? key : name,
            maxStack = Mathf.Max(1, stackSize),
            type = ParseItemType(typeText),
            nutrition = itemObject["nutrition"]?.Value<int?>() ?? 0,
            durability = itemObject["durability"]?.Value<int?>() ?? 0
        };

        RegisterItem(itemData, key);
        RegisterKeyAlias(itemData.id, itemData.name);
    }

    private int ResolveItemId(JToken? idToken, string key)
    {
        if (idToken != null && idToken.Type == JTokenType.Integer)
        {
            int idValue = idToken.Value<int>();
            return idValue >= 0 ? idValue : -1;
        }

        if (idToken != null && idToken.Type == JTokenType.String)
        {
            string? idString = idToken.Value<string>();
            if (!string.IsNullOrWhiteSpace(idString) && int.TryParse(idString, out int parsed))
            {
                return parsed >= 0 ? parsed : -1;
            }

            if (!string.IsNullOrWhiteSpace(idString))
            {
                return BuildStableIdFromKey(idString);
            }
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            return BuildStableIdFromKey(key);
        }

        return -1;
    }

    private static int BuildStableIdFromKey(string key)
    {
        unchecked
        {
            uint hash = 2166136261;
            string lower = key.ToLowerInvariant();
            for (int i = 0; i < lower.Length; i++)
            {
                hash ^= lower[i];
                hash *= 16777619;
            }

            int value = (int)(hash & 0x7FFFFFFF);
            return 100000 + (value % 900000);
        }
    }

    private void RegisterItem(ItemData itemData, string keyHint)
    {
        if (!itemDatabase.ContainsKey(itemData.id))
        {
            itemDatabase[itemData.id] = itemData;
        }

        if (!string.IsNullOrWhiteSpace(keyHint))
        {
            RegisterKeyAlias(itemData.id, keyHint);
        }
    }

    private void RegisterKeyAlias(int id, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return;
        }

        string normalized = alias.Trim();
        if (!itemIdByKey.ContainsKey(normalized))
        {
            itemIdByKey.Add(normalized, id);
        }
    }

    private ItemType ParseItemType(string typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText))
        {
            return ItemType.Material;
        }

        switch (typeText.Trim().ToLowerInvariant())
        {
            case "block":
            case "blocks":
                return ItemType.Block;
            case "tool":
            case "tools":
                return ItemType.Tool;
            case "food":
                return ItemType.Food;
            default:
                return ItemType.Material;
        }
    }

    private int InferDefaultStackSize(string typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText))
        {
            return maxStackSize;
        }

        string normalized = typeText.Trim().ToLowerInvariant();
        if (normalized.Contains("tool") || normalized.Contains("weapon") || normalized.Contains("armor"))
        {
            return 1;
        }

        return maxStackSize;
    }

    private void CreateDefaultItems()
    {
        RegisterDefaultItem(1, "Stone", ItemType.Block, 64);
        RegisterDefaultItem(2, "Dirt", ItemType.Block, 64);
        RegisterDefaultItem(3, "Grass", ItemType.Block, 64);
        RegisterDefaultItem(4, "Wood", ItemType.Block, 64);
        RegisterDefaultItem(5, "Leaves", ItemType.Block, 64);
        RegisterDefaultItem(10, "Wooden Pickaxe", ItemType.Tool, 1, durability: 60);
        RegisterDefaultItem(11, "Stone Pickaxe", ItemType.Tool, 1, durability: 132);
        RegisterDefaultItem(12, "Wooden Sword", ItemType.Tool, 1, durability: 60);
        RegisterDefaultItem(20, "Apple", ItemType.Food, 64, nutrition: 4);
        RegisterDefaultItem(21, "Bread", ItemType.Food, 64, nutrition: 5);
        RegisterDefaultItem(22, "Cooked Meat", ItemType.Food, 64, nutrition: 8);

        RegisterKeyAlias(1, "stone");
        RegisterKeyAlias(4, "wood");
        RegisterKeyAlias(10, "wooden_pickaxe");
        RegisterKeyAlias(20, "apple");
    }

    private void RegisterDefaultItem(int id, string name, ItemType type, int stackSize, int nutrition = 0, int durability = 0)
    {
        var item = new ItemData
        {
            id = id,
            name = name,
            type = type,
            maxStack = Mathf.Max(1, stackSize),
            nutrition = nutrition,
            durability = durability
        };

        RegisterItem(item, name);
    }

    private void AddStartingItems()
    {
        AddItem(ResolveItemIdFromKeyOrDefault("stone", 1), 10);
        AddItem(ResolveItemIdFromKeyOrDefault("wood", 4), 5);
        AddItem(ResolveItemIdFromKeyOrDefault("wooden_pickaxe", 10), 1);
        AddItem(ResolveItemIdFromKeyOrDefault("apple", 20), 3);
    }

    private int ResolveItemIdFromKeyOrDefault(string key, int fallback)
    {
        if (TryGetItemIdByKey(key, out int id))
        {
            return id;
        }

        return fallback;
    }

    public bool TryGetItemIdByKey(string itemKey, out int itemId)
    {
        EnsureItemDatabaseLoaded();
        return itemIdByKey.TryGetValue(itemKey, out itemId);
    }

    public bool AddItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0)
        {
            return false;
        }

        int remaining = AddItemToExistingStacks(mainInventorySlots, itemId, amount);
        remaining = AddItemToExistingStacks(hotbarSlots, itemId, remaining);
        remaining = AddItemToEmptySlots(mainInventorySlots, itemId, remaining);
        remaining = AddItemToEmptySlots(hotbarSlots, itemId, remaining);

        bool completed = remaining <= 0;
        if (completed)
        {
            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }

        return completed;
    }

    public bool AddItem(byte itemId, int amount)
    {
        return AddItem((int)itemId, amount);
    }

    private int AddItemToExistingStacks(InventorySlot[] slots, int itemId, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemId != itemId || slots[i].amount >= GetItemMaxStack(itemId))
            {
                continue;
            }

            int space = GetItemMaxStack(itemId) - slots[i].amount;
            int toAdd = Mathf.Min(amount, space);
            slots[i].amount += toAdd;
            amount -= toAdd;
            if (amount <= 0)
            {
                return 0;
            }
        }

        return amount;
    }

    private int AddItemToEmptySlots(InventorySlot[] slots, int itemId, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemId != 0)
            {
                continue;
            }

            int toAdd = Mathf.Min(amount, GetItemMaxStack(itemId));
            slots[i].itemId = itemId;
            slots[i].amount = toAdd;
            amount -= toAdd;
            if (amount <= 0)
            {
                return 0;
            }
        }

        return amount;
    }

    public bool RemoveItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0)
        {
            return false;
        }

        int remaining = RemoveItemFromSlots(mainInventorySlots, itemId, amount);
        remaining = RemoveItemFromSlots(hotbarSlots, itemId, remaining);

        bool completed = remaining <= 0;
        if (completed)
        {
            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }

        return completed;
    }

    public bool RemoveItem(byte itemId, int amount)
    {
        return RemoveItem((int)itemId, amount);
    }

    private static int RemoveItemFromSlots(InventorySlot[] slots, int itemId, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemId != itemId)
            {
                continue;
            }

            int toRemove = Mathf.Min(amount, slots[i].amount);
            slots[i].amount -= toRemove;
            amount -= toRemove;

            if (slots[i].amount <= 0)
            {
                slots[i].itemId = 0;
                slots[i].amount = 0;
            }

            if (amount <= 0)
            {
                return 0;
            }
        }

        return amount;
    }

    public int GetItemCount(int itemId)
    {
        if (itemId <= 0)
        {
            return 0;
        }

        int count = 0;
        count += CountItemInSlots(mainInventorySlots, itemId);
        count += CountItemInSlots(hotbarSlots, itemId);
        return count;
    }

    public int GetItemCount(byte itemId)
    {
        return GetItemCount((int)itemId);
    }

    private static int CountItemInSlots(InventorySlot[] slots, int itemId)
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemId == itemId)
            {
                count += slots[i].amount;
            }
        }

        return count;
    }

    public int GetHotbarItemId(int slot)
    {
        if (slot >= 0 && slot < hotbarSlots.Length)
        {
            return hotbarSlots[slot].itemId;
        }

        return 0;
    }

    public byte GetHotbarItem(int slot)
    {
        int id = GetHotbarItemId(slot);
        return (byte)Mathf.Clamp(id, 0, byte.MaxValue);
    }

    public InventorySlot GetHotbarSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSlots.Length)
        {
            return hotbarSlots[slot];
        }

        return new InventorySlot();
    }

    public InventorySlot GetMainInventorySlot(int slot)
    {
        if (slot >= 0 && slot < mainInventorySlots.Length)
        {
            return mainInventorySlots[slot];
        }

        return new InventorySlot();
    }

    public void SetSelectedSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSlots.Length)
        {
            selectedSlot = slot;
            OnHotbarChanged?.Invoke();
        }
    }

    public int GetSelectedSlot()
    {
        return selectedSlot;
    }

    public bool SwapSlots(int fromSlot, int toSlot, bool fromHotbar, bool toHotbar)
    {
        if (!ValidateSwapIndex(fromSlot, fromHotbar) || !ValidateSwapIndex(toSlot, toHotbar))
        {
            return false;
        }

        InventorySlot fromData = fromHotbar ? hotbarSlots[fromSlot] : mainInventorySlots[fromSlot];
        InventorySlot toData = toHotbar ? hotbarSlots[toSlot] : mainInventorySlots[toSlot];

        if (fromHotbar)
        {
            hotbarSlots[fromSlot] = toData;
        }
        else
        {
            mainInventorySlots[fromSlot] = toData;
        }

        if (toHotbar)
        {
            hotbarSlots[toSlot] = fromData;
        }
        else
        {
            mainInventorySlots[toSlot] = fromData;
        }

        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        return true;
    }

    private bool ValidateSwapIndex(int slot, bool hotbar)
    {
        return hotbar
            ? slot >= 0 && slot < hotbarSlots.Length
            : slot >= 0 && slot < mainInventorySlots.Length;
    }

    private int GetItemMaxStack(int itemId)
    {
        EnsureItemDatabaseLoaded();
        if (itemDatabase.TryGetValue(itemId, out ItemData data))
        {
            return Mathf.Max(1, data.maxStack);
        }

        return Mathf.Max(1, maxStackSize);
    }

    public ItemData GetItemData(int itemId)
    {
        EnsureItemDatabaseLoaded();
        return itemDatabase.TryGetValue(itemId, out ItemData data) ? data : null;
    }

    public ItemData GetItemData(byte itemId)
    {
        return GetItemData((int)itemId);
    }

    public void ToggleInventoryUI()
    {
        if (inventoryUI == null)
        {
            return;
        }

        bool isActive = !inventoryUI.activeSelf;
        inventoryUI.SetActive(isActive);

        if (isActive)
        {
            UpdateInventoryUI();
        }
    }

    private void UpdateInventoryUI()
    {
        Debug.Log("[InventoryManager] Inventory UI refresh requested.");
    }

    public string SaveInventory()
    {
        var saveData = new InventorySaveData
        {
            hotbarSlots = hotbarSlots,
            mainInventorySlots = mainInventorySlots,
            selectedSlot = selectedSlot
        };

        return JsonUtility.ToJson(saveData);
    }

    public void LoadInventory(string jsonData)
    {
        try
        {
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);
            if (saveData == null)
            {
                return;
            }

            hotbarSlots = saveData.hotbarSlots ?? Array.Empty<InventorySlot>();
            mainInventorySlots = saveData.mainInventorySlots ?? Array.Empty<InventorySlot>();
            selectedSlot = saveData.selectedSlot;

            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[InventoryManager] Failed to load inventory: {ex.Message}");
        }
    }
}

[Serializable]
public class InventorySlot
{
    public int itemId;
    public int amount;
}

[Serializable]
public class ItemData
{
    public int id;
    public string name = string.Empty;
    public int maxStack = 64;
    public ItemType type;
    public int nutrition;
    public int durability;
}

public enum ItemType
{
    Block,
    Tool,
    Food,
    Material
}

[Serializable]
public class InventorySaveData
{
    public InventorySlot[] hotbarSlots = Array.Empty<InventorySlot>();
    public InventorySlot[] mainInventorySlots = Array.Empty<InventorySlot>();
    public int selectedSlot;
}
