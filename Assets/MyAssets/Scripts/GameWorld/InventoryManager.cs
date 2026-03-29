using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Inventory system for managing player items and blocks
/// Supports hotbar, main inventory, and crafting
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Configuration")]
    public int hotbarSize = 9;
    public int mainInventorySize = 27;
    public int maxStackSize = 64;
    
    [Header("UI References")]
    public GameObject inventoryUI;
    public Transform hotbarPanel;
    public Transform mainInventoryPanel;
    
    // Inventory data structure
    private InventorySlot[] hotbarSlots;
    private InventorySlot[] mainInventorySlots;
    private Dictionary<byte, ItemData> itemDatabase;
    
    // Current selected slot
    private int selectedSlot = 0;
    
    // Events
    public delegate void InventoryUpdateHandler();
    public event InventoryUpdateHandler OnInventoryChanged;
    public event InventoryUpdateHandler OnHotbarChanged;
    
    void Start()
    {
        InitializeInventory();
        LoadItemDatabase();
    }
    
    void InitializeInventory()
    {
        // Initialize hotbar
        hotbarSlots = new InventorySlot[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new InventorySlot();
        }
        
        // Initialize main inventory
        mainInventorySlots = new InventorySlot[mainInventorySize];
        for (int i = 0; i < mainInventorySize; i++)
        {
            mainInventorySlots[i] = new InventorySlot();
        }
        
        // Give starting items
        AddStartingItems();
    }
    
    void LoadItemDatabase()
    {
        itemDatabase = new Dictionary<byte, ItemData>();
        
        // Load from JSON or create default database
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/items");
        if (jsonFile != null)
        {
            // TODO: Parse JSON and load items
            Debug.Log("Loading items from JSON");
        }
        else
        {
            // Create default items
            CreateDefaultItems();
        }
    }
    
    void CreateDefaultItems()
    {
        // Basic blocks
        itemDatabase[1] = new ItemData { id = 1, name = "Stone", maxStack = 64, type = ItemType.Block };
        itemDatabase[2] = new ItemData { id = 2, name = "Dirt", maxStack = 64, type = ItemType.Block };
        itemDatabase[3] = new ItemData { id = 3, name = "Grass", maxStack = 64, type = ItemType.Block };
        itemDatabase[4] = new ItemData { id = 4, name = "Wood", maxStack = 64, type = ItemType.Block };
        itemDatabase[5] = new ItemData { id = 5, name = "Leaves", maxStack = 64, type = ItemType.Block };
        
        // Tools
        itemDatabase[10] = new ItemData { id = 10, name = "Wooden Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 60 };
        itemDatabase[11] = new ItemData { id = 11, name = "Stone Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 132 };
        itemDatabase[12] = new ItemData { id = 12, name = "Wooden Sword", maxStack = 1, type = ItemType.Tool, durability = 60 };
        
        // Food
        itemDatabase[20] = new ItemData { id = 20, name = "Apple", maxStack = 64, type = ItemType.Food, nutrition = 4 };
        itemDatabase[21] = new ItemData { id = 21, name = "Bread", maxStack = 64, type = ItemType.Food, nutrition = 5 };
        itemDatabase[22] = new ItemData { id = 22, name = "Cooked Meat", maxStack = 64, type = ItemType.Food, nutrition = 8 };
    }
    
    void AddStartingItems()
    {
        // Give player some starting items
        AddItem(1, 10); // 10 stone
        AddItem(4, 5);  // 5 wood
        AddItem(10, 1); // 1 wooden pickaxe
        AddItem(20, 3); // 3 apples
    }
    
    public bool AddItem(byte itemId, int amount)
    {
        // Try to add to existing stacks first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId && 
                mainInventorySlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - mainInventorySlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                mainInventorySlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Try to add to hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId && 
                hotbarSlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - hotbarSlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                hotbarSlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == 0)
            {
                mainInventorySlots[i].itemId = itemId;
                mainInventorySlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= mainInventorySlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == 0)
            {
                hotbarSlots[i].itemId = itemId;
                hotbarSlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= hotbarSlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Inventory is full
        return amount <= 0;
    }
    
    public bool RemoveItem(byte itemId, int amount)
    {
        int remainingToRemove = amount;
        
        // Remove from main inventory first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, mainInventorySlots[i].amount);
                mainInventorySlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (mainInventorySlots[i].amount <= 0)
                {
                    mainInventorySlots[i].itemId = 0;
                    mainInventorySlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Remove from hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, hotbarSlots[i].amount);
                hotbarSlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (hotbarSlots[i].amount <= 0)
                {
                    hotbarSlots[i].itemId = 0;
                    hotbarSlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Not enough items
        return remainingToRemove <= 0;
    }
    
    public int GetItemCount(byte itemId)
    {
        int count = 0;
        
        // Count in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                count += mainInventorySlots[i].amount;
            }
        }
        
        // Count in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                count += hotbarSlots[i].amount;
            }
        }
        
        return count;
    }
    
    public byte GetHotbarItem(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot].itemId;
        }
        return 0;
    }
    
    public InventorySlot GetHotbarSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot];
        }
        return new InventorySlot();
    }
    
    public InventorySlot GetMainInventorySlot(int slot)
    {
        if (slot >= 0 && slot < mainInventorySize)
        {
            return mainInventorySlots[slot];
        }
        return new InventorySlot();
    }
    
    public void SetSelectedSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
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
        InventorySlot fromData = fromHotbar ? hotbarSlots[fromSlot] : mainInventorySlots[fromSlot];
        InventorySlot toData = toHotbar ? hotbarSlots[toSlot] : mainInventorySlots[toSlot];
        
        // Swap slots
        if (fromHotbar)
            hotbarSlots[fromSlot] = toData;
        else
            mainInventorySlots[fromSlot] = toData;
            
        if (toHotbar)
            hotbarSlots[toSlot] = fromData;
        else
            mainInventorySlots[toSlot] = fromData;
        
        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        return true;
    }
    
    private int GetItemMaxStack(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId].maxStack;
        }
        return maxStackSize;
    }
    
    public ItemData GetItemData(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId];
        }
        return null;
    }
    
    public void ToggleInventoryUI()
    {
        if (inventoryUI != null)
        {
            bool isActive = !inventoryUI.activeSelf;
            inventoryUI.SetActive(isActive);
            
            if (isActive)
            {
                // Update UI with current inventory
                UpdateInventoryUI();
            }
        }
    }
    
    void UpdateInventoryUI()
    {
        // TODO: Update UI elements with inventory data
        Debug.Log("Updating inventory UI");
    }
    
    // Save/Load functionality
    public string SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData
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
            hotbarSlots = saveData.hotbarSlots;
            mainInventorySlots = saveData.mainInventorySlots;
            selectedSlot = saveData.selectedSlot;
            
            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
}

// Data structures
[System.Serializable]
public class InventorySlot
{
    public byte itemId = 0;
    public int amount = 0;
}

[System.Serializable]
public class ItemData
{
    public byte id;
    public string name;
    public int maxStack = 64;
    public ItemType type;
    public int nutrition; // For food items
    public int durability; // For tools
}

public enum ItemType
{
    Block,
    Tool,
    Food,
    Material
}

[System.Serializable]
public class InventorySaveData
{
    public InventorySlot[] hotbarSlots;
    public InventorySlot[] mainInventorySlots;
    public int selectedSlot;
}
using System.Collections.Generic;
using System;

/// <summary>
/// Inventory system for managing player items and blocks
/// Supports hotbar, main inventory, and crafting
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Configuration")]
    public int hotbarSize = 9;
    public int mainInventorySize = 27;
    public int maxStackSize = 64;
    
    [Header("UI References")]
    public GameObject inventoryUI;
    public Transform hotbarPanel;
    public Transform mainInventoryPanel;
    
    // Inventory data structure
    private InventorySlot[] hotbarSlots;
    private InventorySlot[] mainInventorySlots;
    private Dictionary<byte, ItemData> itemDatabase;
    
    // Current selected slot
    private int selectedSlot = 0;
    
    // Events
    public delegate void InventoryUpdateHandler();
    public event InventoryUpdateHandler OnInventoryChanged;
    public event InventoryUpdateHandler OnHotbarChanged;
    
    void Start()
    {
        InitializeInventory();
        LoadItemDatabase();
    }
    
    void InitializeInventory()
    {
        // Initialize hotbar
        hotbarSlots = new InventorySlot[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new InventorySlot();
        }
        
        // Initialize main inventory
        mainInventorySlots = new InventorySlot[mainInventorySize];
        for (int i = 0; i < mainInventorySize; i++)
        {
            mainInventorySlots[i] = new InventorySlot();
        }
        
        // Give starting items
        AddStartingItems();
    }
    
    void LoadItemDatabase()
    {
        itemDatabase = new Dictionary<byte, ItemData>();
        
        // Load from JSON or create default database
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/items");
        if (jsonFile != null)
        {
            // TODO: Parse JSON and load items
            Debug.Log("Loading items from JSON");
        }
        else
        {
            // Create default items
            CreateDefaultItems();
        }
    }
    
    void CreateDefaultItems()
    {
        // Basic blocks
        itemDatabase[1] = new ItemData { id = 1, name = "Stone", maxStack = 64, type = ItemType.Block };
        itemDatabase[2] = new ItemData { id = 2, name = "Dirt", maxStack = 64, type = ItemType.Block };
        itemDatabase[3] = new ItemData { id = 3, name = "Grass", maxStack = 64, type = ItemType.Block };
        itemDatabase[4] = new ItemData { id = 4, name = "Wood", maxStack = 64, type = ItemType.Block };
        itemDatabase[5] = new ItemData { id = 5, name = "Leaves", maxStack = 64, type = ItemType.Block };
        
        // Tools
        itemDatabase[10] = new ItemData { id = 10, name = "Wooden Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 60 };
        itemDatabase[11] = new ItemData { id = 11, name = "Stone Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 132 };
        itemDatabase[12] = new ItemData { id = 12, name = "Wooden Sword", maxStack = 1, type = ItemType.Tool, durability = 60 };
        
        // Food
        itemDatabase[20] = new ItemData { id = 20, name = "Apple", maxStack = 64, type = ItemType.Food, nutrition = 4 };
        itemDatabase[21] = new ItemData { id = 21, name = "Bread", maxStack = 64, type = ItemType.Food, nutrition = 5 };
        itemDatabase[22] = new ItemData { id = 22, name = "Cooked Meat", maxStack = 64, type = ItemType.Food, nutrition = 8 };
    }
    
    void AddStartingItems()
    {
        // Give player some starting items
        AddItem(1, 10); // 10 stone
        AddItem(4, 5);  // 5 wood
        AddItem(10, 1); // 1 wooden pickaxe
        AddItem(20, 3); // 3 apples
    }
    
    public bool AddItem(byte itemId, int amount)
    {
        // Try to add to existing stacks first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId && 
                mainInventorySlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - mainInventorySlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                mainInventorySlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Try to add to hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId && 
                hotbarSlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - hotbarSlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                hotbarSlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == 0)
            {
                mainInventorySlots[i].itemId = itemId;
                mainInventorySlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= mainInventorySlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == 0)
            {
                hotbarSlots[i].itemId = itemId;
                hotbarSlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= hotbarSlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Inventory is full
        return amount <= 0;
    }
    
    public bool RemoveItem(byte itemId, int amount)
    {
        int remainingToRemove = amount;
        
        // Remove from main inventory first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, mainInventorySlots[i].amount);
                mainInventorySlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (mainInventorySlots[i].amount <= 0)
                {
                    mainInventorySlots[i].itemId = 0;
                    mainInventorySlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Remove from hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, hotbarSlots[i].amount);
                hotbarSlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (hotbarSlots[i].amount <= 0)
                {
                    hotbarSlots[i].itemId = 0;
                    hotbarSlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Not enough items
        return remainingToRemove <= 0;
    }
    
    public int GetItemCount(byte itemId)
    {
        int count = 0;
        
        // Count in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                count += mainInventorySlots[i].amount;
            }
        }
        
        // Count in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                count += hotbarSlots[i].amount;
            }
        }
        
        return count;
    }
    
    public byte GetHotbarItem(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot].itemId;
        }
        return 0;
    }
    
    public InventorySlot GetHotbarSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot];
        }
        return new InventorySlot();
    }
    
    public InventorySlot GetMainInventorySlot(int slot)
    {
        if (slot >= 0 && slot < mainInventorySize)
        {
            return mainInventorySlots[slot];
        }
        return new InventorySlot();
    }
    
    public void SetSelectedSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
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
        InventorySlot fromData = fromHotbar ? hotbarSlots[fromSlot] : mainInventorySlots[fromSlot];
        InventorySlot toData = toHotbar ? hotbarSlots[toSlot] : mainInventorySlots[toSlot];
        
        // Swap slots
        if (fromHotbar)
            hotbarSlots[fromSlot] = toData;
        else
            mainInventorySlots[fromSlot] = toData;
            
        if (toHotbar)
            hotbarSlots[toSlot] = fromData;
        else
            mainInventorySlots[toSlot] = fromData;
        
        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        return true;
    }
    
    private int GetItemMaxStack(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId].maxStack;
        }
        return maxStackSize;
    }
    
    public ItemData GetItemData(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId];
        }
        return null;
    }
    
    public void ToggleInventoryUI()
    {
        if (inventoryUI != null)
        {
            bool isActive = !inventoryUI.activeSelf;
            inventoryUI.SetActive(isActive);
            
            if (isActive)
            {
                // Update UI with current inventory
                UpdateInventoryUI();
            }
        }
    }
    
    void UpdateInventoryUI()
    {
        // TODO: Update UI elements with inventory data
        Debug.Log("Updating inventory UI");
    }
    
    // Save/Load functionality
    public string SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData
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
            hotbarSlots = saveData.hotbarSlots;
            mainInventorySlots = saveData.mainInventorySlots;
            selectedSlot = saveData.selectedSlot;
            
            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
}

// Data structures
[System.Serializable]
public class InventorySlot
{
    public byte itemId = 0;
    public int amount = 0;
}

[System.Serializable]
public class ItemData
{
    public byte id;
    public string name;
    public int maxStack = 64;
    public ItemType type;
    public int nutrition; // For food items
    public int durability; // For tools
}

public enum ItemType
{
    Block,
    Tool,
    Food,
    Material
}

[System.Serializable]
public class InventorySaveData
{
    public InventorySlot[] hotbarSlots;
    public InventorySlot[] mainInventorySlots;
    public int selectedSlot;
}
}

/// <summary>
/// Inventory system for managing player items and blocks
/// Supports hotbar, main inventory, and crafting
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Configuration")]
    public int hotbarSize = 9;
    public int mainInventorySize = 27;
    public int maxStackSize = 64;
    
    [Header("UI References")]
    public GameObject inventoryUI;
    public Transform hotbarPanel;
    public Transform mainInventoryPanel;
    
    // Inventory data structure
    private InventorySlot[] hotbarSlots;
    private InventorySlot[] mainInventorySlots;
    private Dictionary<byte, ItemData> itemDatabase;
    
    // Current selected slot
    private int selectedSlot = 0;
    
    // Events
    public delegate void InventoryUpdateHandler();
    public event InventoryUpdateHandler OnInventoryChanged;
    public event InventoryUpdateHandler OnHotbarChanged;
    
    void Start()
    {
        InitializeInventory();
        LoadItemDatabase();
    }
    
    void InitializeInventory()
    {
        // Initialize hotbar
        hotbarSlots = new InventorySlot[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new InventorySlot();
        }
        
        // Initialize main inventory
        mainInventorySlots = new InventorySlot[mainInventorySize];
        for (int i = 0; i < mainInventorySize; i++)
        {
            mainInventorySlots[i] = new InventorySlot();
        }
        
        // Give starting items
        AddStartingItems();
    }
    
    void LoadItemDatabase()
    {
        itemDatabase = new Dictionary<byte, ItemData>();
        
        // Load from JSON or create default database
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/items");
        if (jsonFile != null)
        {
            // TODO: Parse JSON and load items
            Debug.Log("Loading items from JSON");
        }
        else
        {
            // Create default items
            CreateDefaultItems();
        }
    }
    
    void CreateDefaultItems()
    {
        // Basic blocks
        itemDatabase[1] = new ItemData { id = 1, name = "Stone", maxStack = 64, type = ItemType.Block };
        itemDatabase[2] = new ItemData { id = 2, name = "Dirt", maxStack = 64, type = ItemType.Block };
        itemDatabase[3] = new ItemData { id = 3, name = "Grass", maxStack = 64, type = ItemType.Block };
        itemDatabase[4] = new ItemData { id = 4, name = "Wood", maxStack = 64, type = ItemType.Block };
        itemDatabase[5] = new ItemData { id = 5, name = "Leaves", maxStack = 64, type = ItemType.Block };
        
        // Tools
        itemDatabase[10] = new ItemData { id = 10, name = "Wooden Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 60 };
        itemDatabase[11] = new ItemData { id = 11, name = "Stone Pickaxe", maxStack = 1, type = ItemType.Tool, durability = 132 };
        itemDatabase[12] = new ItemData { id = 12, name = "Wooden Sword", maxStack = 1, type = ItemType.Tool, durability = 60 };
        
        // Food
        itemDatabase[20] = new ItemData { id = 20, name = "Apple", maxStack = 64, type = ItemType.Food, nutrition = 4 };
        itemDatabase[21] = new ItemData { id = 21, name = "Bread", maxStack = 64, type = ItemType.Food, nutrition = 5 };
        itemDatabase[22] = new ItemData { id = 22, name = "Cooked Meat", maxStack = 64, type = ItemType.Food, nutrition = 8 };
    }
    
    void AddStartingItems()
    {
        // Give player some starting items
        AddItem(1, 10); // 10 stone
        AddItem(4, 5);  // 5 wood
        AddItem(10, 1); // 1 wooden pickaxe
        AddItem(20, 3); // 3 apples
    }
    
    public bool AddItem(byte itemId, int amount)
    {
        // Try to add to existing stacks first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId && 
                mainInventorySlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - mainInventorySlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                mainInventorySlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Try to add to hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId && 
                hotbarSlots[i].amount < GetItemMaxStack(itemId))
            {
                int spaceAvailable = GetItemMaxStack(itemId) - hotbarSlots[i].amount;
                int amountToAdd = Mathf.Min(amount, spaceAvailable);
                
                hotbarSlots[i].amount += amountToAdd;
                amount -= amountToAdd;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == 0)
            {
                mainInventorySlots[i].itemId = itemId;
                mainInventorySlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= mainInventorySlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Add to empty slots in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == 0)
            {
                hotbarSlots[i].itemId = itemId;
                hotbarSlots[i].amount = Mathf.Min(amount, GetItemMaxStack(itemId));
                amount -= hotbarSlots[i].amount;
                
                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Inventory is full
        return amount <= 0;
    }
    
    public bool RemoveItem(byte itemId, int amount)
    {
        int remainingToRemove = amount;
        
        // Remove from main inventory first
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, mainInventorySlots[i].amount);
                mainInventorySlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (mainInventorySlots[i].amount <= 0)
                {
                    mainInventorySlots[i].itemId = 0;
                    mainInventorySlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Remove from hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, hotbarSlots[i].amount);
                hotbarSlots[i].amount -= amountToRemove;
                remainingToRemove -= amountToRemove;
                
                if (hotbarSlots[i].amount <= 0)
                {
                    hotbarSlots[i].itemId = 0;
                    hotbarSlots[i].amount = 0;
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    OnHotbarChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Not enough items
        return remainingToRemove <= 0;
    }
    
    public int GetItemCount(byte itemId)
    {
        int count = 0;
        
        // Count in main inventory
        for (int i = 0; i < mainInventorySize; i++)
        {
            if (mainInventorySlots[i].itemId == itemId)
            {
                count += mainInventorySlots[i].amount;
            }
        }
        
        // Count in hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId)
            {
                count += hotbarSlots[i].amount;
            }
        }
        
        return count;
    }
    
    public byte GetHotbarItem(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot].itemId;
        }
        return 0;
    }
    
    public InventorySlot GetHotbarSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
        {
            return hotbarSlots[slot];
        }
        return new InventorySlot();
    }
    
    public InventorySlot GetMainInventorySlot(int slot)
    {
        if (slot >= 0 && slot < mainInventorySize)
        {
            return mainInventorySlots[slot];
        }
        return new InventorySlot();
    }
    
    public void SetSelectedSlot(int slot)
    {
        if (slot >= 0 && slot < hotbarSize)
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
        InventorySlot fromData = fromHotbar ? hotbarSlots[fromSlot] : mainInventorySlots[fromSlot];
        InventorySlot toData = toHotbar ? hotbarSlots[toSlot] : mainInventorySlots[toSlot];
        
        // Swap the slots
        if (fromHotbar)
            hotbarSlots[fromSlot] = toData;
        else
            mainInventorySlots[fromSlot] = toData;
            
        if (toHotbar)
            hotbarSlots[toSlot] = fromData;
        else
            mainInventorySlots[toSlot] = fromData;
        
        OnInventoryChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        return true;
    }
    
    private int GetItemMaxStack(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId].maxStack;
        }
        return maxStackSize;
    }
    
    public ItemData GetItemData(byte itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId];
        }
        return null;
    }
    
    public void ToggleInventoryUI()
    {
        if (inventoryUI != null)
        {
            bool isActive = !inventoryUI.activeSelf;
            inventoryUI.SetActive(isActive);
            
            if (isActive)
            {
                // Update UI with current inventory
                UpdateInventoryUI();
            }
        }
    }
    
    void UpdateInventoryUI()
    {
        // TODO: Update UI elements with inventory data
        Debug.Log("Updating inventory UI");
    }
    
    // Save/Load functionality
    public string SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData
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
            hotbarSlots = saveData.hotbarSlots;
            mainInventorySlots = saveData.mainInventorySlots;
            selectedSlot = saveData.selectedSlot;
            
            OnInventoryChanged?.Invoke();
            OnHotbarChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
}

// Data structures
[System.Serializable]
public class InventorySlot
{
    public byte itemId = 0;
    public int amount = 0;
}

[System.Serializable]
public class ItemData
{
    public byte id;
    public string name;
    public int maxStack = 64;
    public ItemType type;
    public int nutrition; // For food items
    public int durability; // For tools
}

public enum ItemType
{
    Block,
    Tool,
    Food,
    Material
}

[System.Serializable]
public class InventorySaveData
{
    public InventorySlot[] hotbarSlots;
    public InventorySlot[] mainInventorySlots;
    public int selectedSlot;
}
}
