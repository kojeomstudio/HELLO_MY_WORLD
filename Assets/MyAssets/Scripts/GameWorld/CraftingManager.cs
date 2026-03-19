using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Crafting system for creating items from materials.
/// Recipes are loaded from JSON and mapped to inventory item IDs.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    [Header("Crafting UI References")]
    public GameObject craftingUI;
    public Transform handCraftingPanel;
    public Transform workbenchPanel;
    public Transform furnacePanel;

    [Header("Crafting Configuration")]
    public float craftingSpeed = 1.0f;
    public string streamingRecipesFileName = "recipes.json";
    public string configRecipesRelativePath = "config/recipes.json";

    private InventoryManager inventoryManager;
    private readonly Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>(StringComparer.OrdinalIgnoreCase);
    private CraftingType currentCraftingType = CraftingType.Hand;
    private bool isCrafting;
    private float currentCraftingProgress;
    private CraftingRecipe currentRecipe;

    public delegate void CraftingUpdateHandler();
    public event CraftingUpdateHandler OnCraftingStarted;
    public event CraftingUpdateHandler OnCraftingProgress;
    public event CraftingUpdateHandler OnCraftingCompleted;

    private void Start()
    {
        inventoryManager = GetComponent<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("[CraftingManager] InventoryManager component is required.");
            return;
        }

        inventoryManager.EnsureItemDatabaseLoaded();
        LoadRecipes();
    }

    private void LoadRecipes()
    {
        recipes.Clear();
        bool loaded = TryLoadRecipesFromJson();
        if (!loaded)
        {
            CreateDefaultRecipes();
            Debug.LogWarning("[CraftingManager] Could not load recipe JSON. Using default recipes.");
        }

        if (recipes.Count == 0)
        {
            CreateDefaultRecipes();
            Debug.LogWarning("[CraftingManager] Recipe list was empty after JSON load. Using default recipes.");
        }
    }

    private bool TryLoadRecipesFromJson()
    {
        string[] candidates = BuildRecipeJsonCandidates();
        foreach (string path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                string json = File.ReadAllText(path);
                var token = JToken.Parse(json);
                int loadedCount = ParseRecipeRoot(token);
                if (loadedCount > 0)
                {
                    Debug.Log($"[CraftingManager] Loaded {loadedCount} recipes from '{path}'.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CraftingManager] Failed to parse '{path}': {ex.Message}");
            }
        }

        return false;
    }

    private string[] BuildRecipeJsonCandidates()
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(streamingRecipesFileName))
        {
            paths.Add(Path.Combine(Application.streamingAssetsPath, streamingRecipesFileName));
        }

        if (!string.IsNullOrWhiteSpace(configRecipesRelativePath))
        {
            paths.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", configRecipesRelativePath)));
        }

        paths.Add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "config", "game-data", "recipes.json")));
        return paths.ToArray();
    }

    private int ParseRecipeRoot(JToken root)
    {
        int loaded = 0;
        if (root is JObject objectRoot && objectRoot["recipes"] is JArray recipesArray)
        {
            foreach (JToken token in recipesArray)
            {
                if (token is JObject recipeObject && TryRegisterRecipe(recipeObject))
                {
                    loaded++;
                }
            }

            return loaded;
        }

        if (root is JArray arrayRoot)
        {
            foreach (JToken token in arrayRoot)
            {
                if (token is JObject recipeObject && TryRegisterRecipe(recipeObject))
                {
                    loaded++;
                }
            }
        }

        return loaded;
    }

    private bool TryRegisterRecipe(JObject recipeObject)
    {
        string recipeId = recipeObject["recipeId"]?.Value<string>()
            ?? recipeObject["id"]?.Value<string>()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        var ingredients = ParseIngredients(recipeObject["ingredients"] as JArray);
        var results = ParseResults(recipeObject);
        if (ingredients.Count == 0 || results.Count == 0)
        {
            return false;
        }

        string displayName = recipeObject["displayName"]?.Value<string>()
            ?? recipeObject["name"]?.Value<string>()
            ?? recipeId;
        string station = recipeObject["craftingStation"]?.Value<string>()
            ?? recipeObject["station"]?.Value<string>()
            ?? recipeObject["type"]?.Value<string>()
            ?? "hand";

        float craftingTime = recipeObject["craftingTime"]?.Value<float?>()
            ?? recipeObject["crafting_time"]?.Value<float?>()
            ?? 0.0f;

        var recipe = new CraftingRecipe
        {
            id = recipeId,
            name = displayName,
            type = ParseCraftingType(station),
            craftingTime = Mathf.Max(0.0f, craftingTime),
            ingredients = ingredients.ToArray(),
            results = results.ToArray()
        };

        recipes[recipeId] = recipe;
        return true;
    }

    private List<CraftingIngredient> ParseIngredients(JArray ingredientsArray)
    {
        var ingredients = new List<CraftingIngredient>();
        if (ingredientsArray == null)
        {
            return ingredients;
        }

        foreach (JToken ingredientToken in ingredientsArray)
        {
            if (ingredientToken is not JObject ingredientObject)
            {
                continue;
            }

            if (!TryResolveItemId(ingredientObject["itemId"] ?? ingredientObject["item_id"], out int itemId))
            {
                continue;
            }

            int amount = ingredientObject["quantity"]?.Value<int?>()
                ?? ingredientObject["count"]?.Value<int?>()
                ?? ingredientObject["amount"]?.Value<int?>()
                ?? 0;
            if (amount <= 0)
            {
                continue;
            }

            ingredients.Add(new CraftingIngredient
            {
                itemId = itemId,
                amount = amount
            });
        }

        return ingredients;
    }

    private List<CraftingResult> ParseResults(JObject recipeObject)
    {
        var results = new List<CraftingResult>();
        if (recipeObject["results"] is JArray resultsArray)
        {
            foreach (JToken resultToken in resultsArray)
            {
                if (resultToken is not JObject resultObject)
                {
                    continue;
                }

                if (TryParseResult(resultObject, out CraftingResult result))
                {
                    results.Add(result);
                }
            }

            return results;
        }

        if (recipeObject["result"] is JObject singleResult && TryParseResult(singleResult, out CraftingResult parsedResult))
        {
            results.Add(parsedResult);
        }

        return results;
    }

    private bool TryParseResult(JObject resultObject, out CraftingResult result)
    {
        result = new CraftingResult();
        if (!TryResolveItemId(resultObject["itemId"] ?? resultObject["item_id"], out int itemId))
        {
            return false;
        }

        int amount = resultObject["quantity"]?.Value<int?>()
            ?? resultObject["count"]?.Value<int?>()
            ?? resultObject["amount"]?.Value<int?>()
            ?? 0;
        if (amount <= 0)
        {
            return false;
        }

        result.itemId = itemId;
        result.amount = amount;
        return true;
    }

    private bool TryResolveItemId(JToken token, out int itemId)
    {
        itemId = 0;
        if (token == null)
        {
            return false;
        }

        if (token.Type == JTokenType.Integer)
        {
            itemId = token.Value<int>();
            return itemId > 0;
        }

        if (token.Type != JTokenType.String)
        {
            return false;
        }

        string key = token.Value<string>();
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (int.TryParse(key, out int parsed))
        {
            itemId = parsed;
            return itemId > 0;
        }

        return inventoryManager.TryGetItemIdByKey(key, out itemId);
    }

    private CraftingType ParseCraftingType(string station)
    {
        if (string.IsNullOrWhiteSpace(station))
        {
            return CraftingType.Hand;
        }

        switch (station.Trim().ToLowerInvariant())
        {
            case "hand":
                return CraftingType.Hand;
            case "crafting_table":
            case "workbench":
                return CraftingType.Workbench;
            case "furnace":
            case "smelting":
                return CraftingType.Furnace;
            case "anvil":
                return CraftingType.Anvil;
            case "enchanting_table":
                return CraftingType.EnchantingTable;
            default:
                return CraftingType.Hand;
        }
    }

    private void CreateDefaultRecipes()
    {
        recipes["wood_planks"] = new CraftingRecipe
        {
            id = "wood_planks",
            name = "Wood Planks",
            type = CraftingType.Hand,
            craftingTime = 3.0f,
            ingredients = new[]
            {
                new CraftingIngredient { itemId = ResolveItemId("wood", 4), amount = 1 }
            },
            results = new[]
            {
                new CraftingResult { itemId = ResolveItemId("oak_planks", 5), amount = 4 }
            }
        };

        recipes["sticks"] = new CraftingRecipe
        {
            id = "sticks",
            name = "Sticks",
            type = CraftingType.Hand,
            craftingTime = 2.0f,
            ingredients = new[]
            {
                new CraftingIngredient { itemId = ResolveItemId("oak_planks", 5), amount = 2 }
            },
            results = new[]
            {
                new CraftingResult { itemId = ResolveItemId("stick", 7), amount = 4 }
            }
        };
    }

    private int ResolveItemId(string key, int fallback)
    {
        return inventoryManager.TryGetItemIdByKey(key, out int itemId) ? itemId : fallback;
    }

    public void OpenCraftingUI(CraftingType type)
    {
        if (craftingUI == null)
        {
            return;
        }

        currentCraftingType = type;
        craftingUI.SetActive(true);

        handCraftingPanel?.gameObject.SetActive(type == CraftingType.Hand);
        workbenchPanel?.gameObject.SetActive(type == CraftingType.Workbench);
        furnacePanel?.gameObject.SetActive(type == CraftingType.Furnace);

        UpdateAvailableRecipes();
    }

    public void CloseCraftingUI()
    {
        if (craftingUI == null)
        {
            return;
        }

        craftingUI.SetActive(false);
        StopCrafting();
    }

    private void UpdateAvailableRecipes()
    {
        Debug.Log($"[CraftingManager] Updating available recipes for {currentCraftingType}.");
    }

    public bool CanCraftRecipe(string recipeId)
    {
        if (!recipes.TryGetValue(recipeId, out CraftingRecipe recipe))
        {
            return false;
        }

        if (recipe.type != currentCraftingType)
        {
            return false;
        }

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
        StartCoroutine(CraftingCoroutine());
    }

    public void StopCrafting()
    {
        if (!isCrafting)
        {
            return;
        }

        isCrafting = false;
        currentCraftingProgress = 0.0f;
        currentRecipe = null;
        StopAllCoroutines();
    }

    private IEnumerator CraftingCoroutine()
    {
        float craftingTime = Mathf.Max(0.05f, currentRecipe.craftingTime / Mathf.Max(0.1f, craftingSpeed));
        while (currentCraftingProgress < craftingTime)
        {
            currentCraftingProgress += Time.deltaTime;
            OnCraftingProgress?.Invoke();
            yield return null;
        }

        CompleteCrafting();
    }

    private void CompleteCrafting()
    {
        if (currentRecipe == null || inventoryManager == null)
        {
            return;
        }

        foreach (CraftingIngredient ingredient in currentRecipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.itemId, ingredient.amount);
        }

        foreach (CraftingResult result in currentRecipe.results)
        {
            inventoryManager.AddItem(result.itemId, result.amount);
        }

        isCrafting = false;
        currentCraftingProgress = 0.0f;
        OnCraftingCompleted?.Invoke();
        Debug.Log($"[CraftingManager] Completed crafting: {currentRecipe.name}");
    }

    public CraftingRecipe GetRecipe(string recipeId)
    {
        return recipes.TryGetValue(recipeId, out CraftingRecipe recipe) ? recipe : null;
    }

    public List<CraftingRecipe> GetAvailableRecipes(CraftingType type)
    {
        var availableRecipes = new List<CraftingRecipe>();
        foreach (KeyValuePair<string, CraftingRecipe> entry in recipes)
        {
            if (entry.Value.type == type && CanCraftRecipe(entry.Key))
            {
                availableRecipes.Add(entry.Value);
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

        float target = Mathf.Max(0.05f, currentRecipe.craftingTime / Mathf.Max(0.1f, craftingSpeed));
        return Mathf.Clamp01(currentCraftingProgress / target);
    }

    public CraftingRecipe GetCurrentRecipe()
    {
        return currentRecipe;
    }

    public string SaveCraftingData()
    {
        var saveData = new CraftingSaveData
        {
            craftingSpeed = craftingSpeed
        };

        return JsonUtility.ToJson(saveData);
    }

    public void LoadCraftingData(string jsonData)
    {
        try
        {
            CraftingSaveData saveData = JsonUtility.FromJson<CraftingSaveData>(jsonData);
            if (saveData != null)
            {
                craftingSpeed = Mathf.Max(0.1f, saveData.craftingSpeed);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CraftingManager] Failed to load crafting data: {ex.Message}");
        }
    }
}

[Serializable]
public class CraftingRecipe
{
    public string id = string.Empty;
    public string name = string.Empty;
    public CraftingType type;
    public float craftingTime;
    public CraftingIngredient[] ingredients = Array.Empty<CraftingIngredient>();
    public CraftingResult[] results = Array.Empty<CraftingResult>();
}

[Serializable]
public class CraftingIngredient
{
    public int itemId;
    public int amount;
}

[Serializable]
public class CraftingResult
{
    public int itemId;
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

[Serializable]
public class CraftingSaveData
{
    public float craftingSpeed;
}
