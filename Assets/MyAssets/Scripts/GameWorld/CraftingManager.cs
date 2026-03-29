using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// Crafting system for creating items from materials
/// Supports different crafting types (hand, workbench, furnace)
/// </summary>
public class CraftingManager : MonoBehaviour
{
    [Header("Crafting UI References")]
    public GameObject craftingUI;
    public Transform handCraftingPanel;
    public Transform workbenchPanel;
    public Transform furnacePanel;
    
    [Header("Crafting Configuration")]
    public float craftingSpeed = 1.0f; // Multiplier for crafting speed
    
    private InventoryManager inventoryManager;
    private Dictionary<string, CraftingRecipe> recipes;
    private CraftingType currentCraftingType = CraftingType.Hand;
    private bool isCrafting = false;
    private float currentCraftingProgress = 0.0f;
    private CraftingRecipe currentRecipe;
    
    // Events
    public delegate void CraftingUpdateHandler();
    public event CraftingUpdateHandler OnCraftingStarted;
    public event CraftingUpdateHandler OnCraftingProgress;
    public event CraftingUpdateHandler OnCraftingCompleted;
    
    void Start()
    {
        inventoryManager = GetComponent<InventoryManager>();
        LoadRecipes();
    }
    
    void LoadRecipes()
    {
        recipes = new Dictionary<string, CraftingRecipe>();
        
        // Load from JSON or create default recipes
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/crafting_recipes");
        if (jsonFile != null)
        {
            // TODO: Parse JSON and load recipes
            Debug.Log("Loading crafting recipes from JSON");
        }
        else
        {
            CreateDefaultRecipes();
        }
    }
    
    void CreateDefaultRecipes()
    {
        // Hand crafting recipes
        
        // Wood planks
        recipes["wood_planks"] = new CraftingRecipe
        {
            id = "wood_planks",
            name = "Wood Planks",
            type = CraftingType.Hand,
            craftingTime = 3.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 4, amount = 1 } // Wood
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 6, amount = 4 } // Wood planks
            }
        };
        
        // Sticks
        recipes["sticks"] = new CraftingRecipe
        {
            id = "sticks",
            name = "Sticks",
            type = CraftingType.Hand,
            craftingTime = 2.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 2 } // Wood planks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 7, amount = 4 } // Sticks
            }
        };
        
        // Workbench recipes
        
        // Wooden pickaxe
        recipes["wooden_pickaxe"] = new CraftingRecipe
        {
            id = "wooden_pickaxe",
            name = "Wooden Pickaxe",
            type = CraftingType.Workbench,
            craftingTime = 5.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 3 }, // Wood planks
                new CraftingIngredient { itemId = 7, amount = 2 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 10, amount = 1 } // Wooden pickaxe
            }
        };
        
        // Wooden sword
        recipes["wooden_sword"] = new CraftingRecipe
        {
            id = "wooden_sword",
            name = "Wooden Sword",
            type = CraftingType.Workbench,
            craftingTime = 4.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 2 }, // Wood planks
                new CraftingIngredient { itemId = 7, amount = 1 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 12, amount = 1 } // Wooden sword
            }
        };
        
        // Stone pickaxe
        recipes["stone_pickaxe"] = new CraftingRecipe
        {
            id = "stone_pickaxe",
            name = "Stone Pickaxe",
            type = CraftingType.Workbench,
            craftingTime = 6.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 1, amount = 3 }, // Stone
                new CraftingIngredient { itemId = 7, amount = 2 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 11, amount = 1 } // Stone pickaxe
            }
        };
        
        // Furnace recipes
        
        // Cooked meat
        recipes["cooked_meat"] = new CraftingRecipe
        {
            id = "cooked_meat",
            name = "Cooked Meat",
            type = CraftingType.Furnace,
            craftingTime = 10.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 23, amount = 1 } // Raw meat
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 22, amount = 1 } // Cooked meat
            }
        };
        
        // Smelting
        recipes["iron_ingot"] = new CraftingRecipe
        {
            id = "iron_ingot",
            name = "Iron Ingot",
            type = CraftingType.Furnace,
            craftingTime = 8.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 24, amount = 1 } // Iron ore
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 25, amount = 1 } // Iron ingot
            }
        };
    }
    
    public void OpenCraftingUI(CraftingType type)
    {
        if (craftingUI != null)
        {
            currentCraftingType = type;
            craftingUI.SetActive(true);
            
            // Show appropriate crafting panel
            handCraftingPanel?.gameObject.SetActive(type == CraftingType.Hand);
            workbenchPanel?.gameObject.SetActive(type == CraftingType.Workbench);
            furnacePanel?.gameObject.SetActive(type == CraftingType.Furnace);
            
            // Update available recipes
            UpdateAvailableRecipes();
        }
    }
    
    public void CloseCraftingUI()
    {
        if (craftingUI != null)
        {
            craftingUI.SetActive(false);
            StopCrafting();
        }
    }
    
    void UpdateAvailableRecipes()
    {
        // TODO: Update UI with available recipes based on current crafting type
        Debug.Log($"Updating available recipes for {currentCraftingType}");
    }
    
    public bool CanCraftRecipe(string recipeId)
    {
        if (!recipes.ContainsKey(recipeId))
        {
            return false;
        }
        
        CraftingRecipe recipe = recipes[recipeId];
        
        // Check if recipe type matches current crafting type
        if (recipe.type != currentCraftingType)
        {
            return false;
        }
        
        // Check if player has all required ingredients
        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (inventoryManager.GetItemCount(ingredient.itemId) < ingredient.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void StartCrafting(string recipeId)
    {
        if (isCrafting || !CanCraftRecipe(recipeId))
        {
            return;
        }
        
        currentRecipe = recipes[recipeId];
        isCrafting = true;
        currentCraftingProgress = 0.0f;
        
        OnCraftingStarted?.Invoke();
        
        // Start crafting coroutine
        StartCoroutine(CraftingCoroutine());
    }
    
    public void StopCrafting()
    {
        if (isCrafting)
        {
            isCrafting = false;
            currentCraftingProgress = 0.0f;
            currentRecipe = null;
            StopAllCoroutines();
        }
    }
    
    IEnumerator CraftingCoroutine()
    {
        float craftingTime = currentRecipe.craftingTime / craftingSpeed;
        
        while (currentCraftingProgress < craftingTime)
        {
            currentCraftingProgress += Time.deltaTime;
            
            // Update progress
            OnCraftingProgress?.Invoke();
            
            yield return null;
        }
        
        // Crafting completed
        CompleteCrafting();
    }
    
    void CompleteCrafting()
    {
        if (currentRecipe == null || inventoryManager == null)
        {
            return;
        }
        
        // Remove ingredients from inventory
        foreach (CraftingIngredient ingredient in currentRecipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.itemId, ingredient.amount);
        }
        
        // Add results to inventory
        foreach (CraftingResult result in currentRecipe.results)
        {
            inventoryManager.AddItem(result.itemId, result.amount);
        }
        
        // Reset crafting state
        isCrafting = false;
        currentCraftingProgress = 0.0f;
        
        OnCraftingCompleted?.Invoke();
        
        Debug.Log($"Completed crafting: {currentRecipe.name}");
    }
    
    public CraftingRecipe GetRecipe(string recipeId)
    {
        if (recipes.ContainsKey(recipeId))
        {
            return recipes[recipeId];
        }
        return null;
    }
    
    public List<CraftingRecipe> GetAvailableRecipes(CraftingType type)
    {
        List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        
        foreach (var kvp in recipes)
        {
            if (kvp.Value.type == type && CanCraftRecipe(kvp.Key))
            {
                availableRecipes.Add(kvp.Value);
            }
        }
        
        return availableRecipes;
    }
    
    public CraftingType GetCurrentCraftingType()
    {
        return currentCraftingType;
    }
    
    public bool IsCrafting()
    {
        return isCrafting;
    }
    
    public float GetCraftingProgress()
    {
        if (!isCrafting || currentRecipe == null)
        {
            return 0.0f;
        }
        
        return currentCraftingProgress / (currentRecipe.craftingTime / craftingSpeed);
    }
    
    public CraftingRecipe GetCurrentRecipe()
    {
        return currentRecipe;
    }
    
    // Save/Load functionality
    public string SaveCraftingData()
    {
        CraftingSaveData saveData = new CraftingSaveData
        {
            craftingSpeed = this.craftingSpeed
            // Add other crafting-related data to save
        };
        
        return JsonUtility.ToJson(saveData);
    }
    
    public void LoadCraftingData(string jsonData)
    {
        try
        {
            CraftingSaveData saveData = JsonUtility.FromJson<CraftingSaveData>(jsonData);
            this.craftingSpeed = saveData.craftingSpeed;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load crafting data: {e.Message}");
        }
    }
}

// Data structures
[System.Serializable]
public class CraftingRecipe
{
    public string id;
    public string name;
    public CraftingType type;
    public float craftingTime;
    public CraftingIngredient[] ingredients;
    public CraftingResult[] results;
}

[System.Serializable]
public class CraftingIngredient
{
    public byte itemId;
    public int amount;
}

[System.Serializable]
public class CraftingResult
{
    public byte itemId;
    public int amount;
}

public enum CraftingType
{
    Hand,
    Workbench,
    Furnace,
    Anvil,
    EnchantingTable
}

[System.Serializable]
public class CraftingSaveData
{
    public float craftingSpeed;
}using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// Crafting system for creating items from materials
/// Supports different crafting types (hand, workbench, furnace)
/// </summary>
public class CraftingManager : MonoBehaviour
{
    [Header("Crafting UI References")]
    public GameObject craftingUI;
    public Transform handCraftingPanel;
    public Transform workbenchPanel;
    public Transform furnacePanel;
    
    [Header("Crafting Configuration")]
    public float craftingSpeed = 1.0f; // Multiplier for crafting speed
    
    private InventoryManager inventoryManager;
    private Dictionary<string, CraftingRecipe> recipes;
    private CraftingType currentCraftingType = CraftingType.Hand;
    private bool isCrafting = false;
    private float currentCraftingProgress = 0.0f;
    private CraftingRecipe currentRecipe;
    
    // Events
    public delegate void CraftingUpdateHandler();
    public event CraftingUpdateHandler OnCraftingStarted;
    public event CraftingUpdateHandler OnCraftingProgress;
    public event CraftingUpdateHandler OnCraftingCompleted;
    
    void Start()
    {
        inventoryManager = GetComponent<InventoryManager>();
        LoadRecipes();
    }
    
    void LoadRecipes()
    {
        recipes = new Dictionary<string, CraftingRecipe>();
        
        // Load from JSON or create default recipes
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/crafting_recipes");
        if (jsonFile != null)
        {
            // TODO: Parse JSON and load recipes
            Debug.Log("Loading crafting recipes from JSON");
        }
        else
        {
            CreateDefaultRecipes();
        }
    }
    
    void CreateDefaultRecipes()
    {
        // Hand crafting recipes
        
        // Wood planks
        recipes["wood_planks"] = new CraftingRecipe
        {
            id = "wood_planks",
            name = "Wood Planks",
            type = CraftingType.Hand,
            craftingTime = 3.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 4, amount = 1 } // Wood
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 6, amount = 4 } // Wood planks
            }
        };
        
        // Sticks
        recipes["sticks"] = new CraftingRecipe
        {
            id = "sticks",
            name = "Sticks",
            type = CraftingType.Hand,
            craftingTime = 2.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 2 } // Wood planks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 7, amount = 4 } // Sticks
            }
        };
        
        // Workbench recipes
        
        // Wooden pickaxe
        recipes["wooden_pickaxe"] = new CraftingRecipe
        {
            id = "wooden_pickaxe",
            name = "Wooden Pickaxe",
            type = CraftingType.Workbench,
            craftingTime = 5.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 3 }, // Wood planks
                new CraftingIngredient { itemId = 7, amount = 2 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 10, amount = 1 } // Wooden pickaxe
            }
        };
        
        // Wooden sword
        recipes["wooden_sword"] = new CraftingRecipe
        {
            id = "wooden_sword",
            name = "Wooden Sword",
            type = CraftingType.Workbench,
            craftingTime = 4.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 6, amount = 2 }, // Wood planks
                new CraftingIngredient { itemId = 7, amount = 1 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 12, amount = 1 } // Wooden sword
            }
        };
        
        // Stone pickaxe
        recipes["stone_pickaxe"] = new CraftingRecipe
        {
            id = "stone_pickaxe",
            name = "Stone Pickaxe",
            type = CraftingType.Workbench,
            craftingTime = 6.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 1, amount = 3 }, // Stone
                new CraftingIngredient { itemId = 7, amount = 2 }  // Sticks
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 11, amount = 1 } // Stone pickaxe
            }
        };
        
        // Furnace recipes
        
        // Cooked meat
        recipes["cooked_meat"] = new CraftingRecipe
        {
            id = "cooked_meat",
            name = "Cooked Meat",
            type = CraftingType.Furnace,
            craftingTime = 10.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 23, amount = 1 } // Raw meat
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 22, amount = 1 } // Cooked meat
            }
        };
        
        // Smelting
        recipes["iron_ingot"] = new CraftingRecipe
        {
            id = "iron_ingot",
            name = "Iron Ingot",
            type = CraftingType.Furnace,
            craftingTime = 8.0f,
            ingredients = new CraftingIngredient[]
            {
                new CraftingIngredient { itemId = 24, amount = 1 } // Iron ore
            },
            results = new CraftingResult[]
            {
                new CraftingResult { itemId = 25, amount = 1 } // Iron ingot
            }
        };
    }
    
    public void OpenCraftingUI(CraftingType type)
    {
        if (craftingUI != null)
        {
            currentCraftingType = type;
            craftingUI.SetActive(true);
            
            // Show appropriate crafting panel
            handCraftingPanel?.gameObject.SetActive(type == CraftingType.Hand);
            workbenchPanel?.gameObject.SetActive(type == CraftingType.Workbench);
            furnacePanel?.gameObject.SetActive(type == CraftingType.Furnace);
            
            // Update available recipes
            UpdateAvailableRecipes();
        }
    }
    
    public void CloseCraftingUI()
    {
        if (craftingUI != null)
        {
            craftingUI.SetActive(false);
            StopCrafting();
        }
    }
    
    void UpdateAvailableRecipes()
    {
        // TODO: Update UI with available recipes based on current crafting type
        Debug.Log($"Updating available recipes for {currentCraftingType}");
    }
    
    public bool CanCraftRecipe(string recipeId)
    {
        if (!recipes.ContainsKey(recipeId))
        {
            return false;
        }
        
        CraftingRecipe recipe = recipes[recipeId];
        
        // Check if recipe type matches current crafting type
        if (recipe.type != currentCraftingType)
        {
            return false;
        }
        
        // Check if player has all required ingredients
        foreach (CraftingIngredient ingredient in recipe.ingredients)
        {
            if (inventoryManager.GetItemCount(ingredient.itemId) < ingredient.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void StartCrafting(string recipeId)
    {
        if (isCrafting || !CanCraftRecipe(recipeId))
        {
            return;
        }
        
        currentRecipe = recipes[recipeId];
        isCrafting = true;
        currentCraftingProgress = 0.0f;
        
        OnCraftingStarted?.Invoke();
        
        // Start crafting coroutine
        StartCoroutine(CraftingCoroutine());
    }
    
    public void StopCrafting()
    {
        if (isCrafting)
        {
            isCrafting = false;
            currentCraftingProgress = 0.0f;
            currentRecipe = null;
            StopAllCoroutines();
        }
    }
    
    IEnumerator CraftingCoroutine()
    {
        float craftingTime = currentRecipe.craftingTime / craftingSpeed;
        
        while (currentCraftingProgress < craftingTime)
        {
            currentCraftingProgress += Time.deltaTime;
            
            // Update progress
            OnCraftingProgress?.Invoke();
            
            yield return null;
        }
        
        // Crafting completed
        CompleteCrafting();
    }
    
    void CompleteCrafting()
    {
        if (currentRecipe == null || inventoryManager == null)
        {
            return;
        }
        
        // Remove ingredients from inventory
        foreach (CraftingIngredient ingredient in currentRecipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.itemId, ingredient.amount);
        }
        
        // Add results to inventory
        foreach (CraftingResult result in currentRecipe.results)
        {
            inventoryManager.AddItem(result.itemId, result.amount);
        }
        
        // Reset crafting state
        isCrafting = false;
        currentCraftingProgress = 0.0f;
        
        OnCraftingCompleted?.Invoke();
        
        Debug.Log($"Completed crafting: {currentRecipe.name}");
    }
    
    public CraftingRecipe GetRecipe(string recipeId)
    {
        if (recipes.ContainsKey(recipeId))
        {
            return recipes[recipeId];
        }
        return null;
    }
    
    public List<CraftingRecipe> GetAvailableRecipes(CraftingType type)
    {
        List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        
        foreach (var kvp in recipes)
        {
            if (kvp.Value.type == type && CanCraftRecipe(kvp.Key))
            {
                availableRecipes.Add(kvp.Value);
            }
        }
        
        return availableRecipes;
    }
    
    public CraftingType GetCurrentCraftingType()
    {
        return currentCraftingType;
    }
    
    public bool IsCrafting()
    {
        return isCrafting;
    }
    
    public float GetCraftingProgress()
    {
        if (!isCrafting || currentRecipe == null)
        {
            return 0.0f;
        }
        
        return currentCraftingProgress / (currentRecipe.craftingTime / craftingSpeed);
    }
    
    public CraftingRecipe GetCurrentRecipe()
    {
        return currentRecipe;
    }
    
    // Save/Load functionality
    public string SaveCraftingData()
    {
        CraftingSaveData saveData = new CraftingSaveData
        {
            craftingSpeed = this.craftingSpeed
            // Add other crafting-related data to save
        };
        
        return JsonUtility.ToJson(saveData);
    }
    
    public void LoadCraftingData(string jsonData)
    {
        try
        {
            CraftingSaveData saveData = JsonUtility.FromJson<CraftingSaveData>(jsonData);
            this.craftingSpeed = saveData.craftingSpeed;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load crafting data: {e.Message}");
        }
    }
}

// Data structures
[System.Serializable]
public class CraftingRecipe
{
    public string id;
    public string name;
    public CraftingType type;
    public float craftingTime;
    public CraftingIngredient[] ingredients;
    public CraftingResult[] results;
}

[System.Serializable]
public class CraftingIngredient
{
    public byte itemId;
    public int amount;
}

[System.Serializable]
public class CraftingResult
{
    public byte itemId;
    public int amount;
}

public enum CraftingType
{
    Hand,
    Workbench,
    Furnace,
    Anvil,
    EnchantingTable
}

[System.Serializable]
public class CraftingSaveData
{
    public float craftingSpeed;
}
