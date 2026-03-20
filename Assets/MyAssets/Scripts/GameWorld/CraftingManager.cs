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
    public string configRecipesRelativePath = "config/game-data/recipes.json";

    private InventoryManager inventoryManager;
    private readonly Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, float> fuelBurnTimeByItemId = new Dictionary<int, float>();
    private CraftingType currentCraftingType = CraftingType.Hand;
    private bool isCrafting;
    private float currentCraftingProgress;
    private CraftingRecipe currentRecipe;

    public delegate void CraftingUpdateHandler();
    public event CraftingUpdateHandler OnCraftingStarted;
    public event CraftingUpdateHandler OnCraftingProgress;
    public event CraftingUpdateHandler OnCraftingCompleted;

    private enum RecipeMethod
    {
        Unknown,
        Normal,
        Cooking,
        Fuel
    }

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
        fuelBurnTimeByItemId.Clear();
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

        AddCandidate(Path.Combine(Application.streamingAssetsPath, "game-data", "recipes.json"));

        if (!string.IsNullOrWhiteSpace(streamingRecipesFileName))
        {
            AddCandidate(Path.Combine(Application.streamingAssetsPath, streamingRecipesFileName));
        }

        if (!string.IsNullOrWhiteSpace(configRecipesRelativePath))
        {
            AddCandidate(Path.Combine(Application.dataPath, "..", configRecipesRelativePath));
        }

        AddCandidate(Path.Combine(Application.dataPath, "..", "config", "game-data", "recipes.json"));
        AddCandidate(Path.Combine(Application.dataPath, "..", "config", "recipes.json"));
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

        RecipeMethod method = ParseRecipeMethod(
            recipeObject["method"]?.Value<string>()
            ?? recipeObject["craft_method"]?.Value<string>()
            ?? string.Empty);

        var ingredients = ParseIngredients(recipeObject["ingredients"] as JArray);
        float craftingTime = recipeObject["craftingTime"]?.Value<float?>()
            ?? recipeObject["crafting_time"]?.Value<float?>()
            ?? recipeObject["craft_time"]?.Value<float?>()
            ?? recipeObject["cooktime"]?.Value<float?>()
            ?? recipeObject["burntime"]?.Value<float?>()
            ?? 0.0f;

        if (method == RecipeMethod.Fuel)
        {
            RegisterFuelRecipe(ingredients, craftingTime);
            return ingredients.Count > 0;
        }

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
            ?? recipeObject["crafting_type"]?.Value<string>()
            ?? string.Empty;

        var replacements = ParseReplacements(recipeObject["replacements"] as JArray);
        bool isShaped = recipeObject["shaped"]?.Value<bool?>()
            ?? recipeObject["is_shaped"]?.Value<bool?>()
            ?? (recipeObject["width"] != null || recipeObject["height"] != null);
        int width = recipeObject["width"]?.Value<int?>() ?? 0;
        int height = recipeObject["height"]?.Value<int?>() ?? 0;

        var recipe = new CraftingRecipe
        {
            id = recipeId,
            name = displayName,
            type = ParseCraftingType(station, method),
            craftingTime = Mathf.Max(0.0f, craftingTime),
            ingredients = ingredients.ToArray(),
            results = results.ToArray(),
            replacements = replacements.ToArray(),
            isShaped = isShaped,
            width = width,
            height = height
        };

        recipes[recipeId] = recipe;
        return true;
    }

    private void RegisterFuelRecipe(IReadOnlyList<CraftingIngredient> ingredients, float burnTime)
    {
        if (ingredients.Count == 0)
        {
            return;
        }

        float clampedBurnTime = Mathf.Max(0.0f, burnTime);
        foreach (CraftingIngredient ingredient in ingredients)
        {
            if (ingredient.itemId <= 0 || ingredient.amount <= 0)
            {
                continue;
            }

            float perItemBurnTime = clampedBurnTime / ingredient.amount;
            if (fuelBurnTimeByItemId.TryGetValue(ingredient.itemId, out float existing))
            {
                fuelBurnTimeByItemId[ingredient.itemId] = Mathf.Max(existing, perItemBurnTime);
            }
            else
            {
                fuelBurnTimeByItemId[ingredient.itemId] = perItemBurnTime;
            }
        }
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

            string groupValue = ingredientObject["group"]?.Value<string>()
                ?? ingredientObject["item_group"]?.Value<string>()
                ?? string.Empty;

            if (!TryResolveItemId(ingredientObject["itemId"] ?? ingredientObject["item_id"], out int itemId))
            {
                if (!string.IsNullOrWhiteSpace(groupValue))
                {
                    itemId = 0;
                }
                else
                {
                    continue;
                }
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
                amount = amount,
                group = groupValue
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

    private List<CraftingReplacement> ParseReplacements(JArray replacementsArray)
    {
        var replacements = new List<CraftingReplacement>();
        if (replacementsArray == null)
        {
            return replacements;
        }

        foreach (JToken replacementToken in replacementsArray)
        {
            if (replacementToken is not JObject replacementObject)
            {
                continue;
            }

            JToken consumeToken = replacementObject["consume"] ?? replacementObject["from"] ?? replacementObject["consume_item"];
            JToken replaceToken = replacementObject["replace"] ?? replacementObject["to"] ?? replacementObject["replace_with"];

            if (!TryResolveItemId(consumeToken, out int consumeItemId))
            {
                continue;
            }

            if (!TryResolveItemId(replaceToken, out int replaceWithItemId))
            {
                continue;
            }

            replacements.Add(new CraftingReplacement
            {
                consumeItemId = consumeItemId,
                replaceWithItemId = replaceWithItemId
            });
        }

        return replacements;
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

    private bool IngredientMatchesItem(int ingredientItemId, int actualItemId, string ingredientGroup)
    {
        if (ingredientItemId > 0 && ingredientItemId == actualItemId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ingredientGroup) && ingredientGroup.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
        {
            string groupName = ingredientGroup.Substring(6);
            return inventoryManager.ItemHasGroup(actualItemId, groupName);
        }

        return false;
    }

    private CraftingType ParseCraftingType(string station, RecipeMethod method)
    {
        if (method == RecipeMethod.Cooking || method == RecipeMethod.Fuel)
        {
            return CraftingType.Furnace;
        }

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
            case "cooking":
            case "fuel":
                return CraftingType.Furnace;
            case "normal":
                return CraftingType.Hand;
            case "anvil":
                return CraftingType.Anvil;
            case "enchanting_table":
                return CraftingType.EnchantingTable;
            default:
                return CraftingType.Hand;
        }
    }

    private static RecipeMethod ParseRecipeMethod(string methodText)
    {
        if (string.IsNullOrWhiteSpace(methodText))
        {
            return RecipeMethod.Unknown;
        }

        switch (methodText.Trim().ToUpperInvariant())
        {
            case "NORMAL":
                return RecipeMethod.Normal;
            case "COOKING":
                return RecipeMethod.Cooking;
            case "FUEL":
                return RecipeMethod.Fuel;
            default:
                return RecipeMethod.Unknown;
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
            if (!HasEnoughIngredients(ingredient))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasEnoughIngredients(CraftingIngredient ingredient)
    {
        if (ingredient.itemId > 0)
        {
            return inventoryManager.GetItemCount(ingredient.itemId) >= ingredient.amount;
        }

        if (!string.IsNullOrWhiteSpace(ingredient.group) && ingredient.group.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
        {
            string groupName = ingredient.group.Substring(6);
            return CountItemsInGroup(groupName) >= ingredient.amount;
        }

        return false;
    }

    private int CountItemsInGroup(string groupName)
    {
        int count = 0;
        foreach (KeyValuePair<string, int> entry in inventoryManager.GetAllItemIdMappings())
        {
            if (inventoryManager.ItemHasGroup(entry.Value, groupName))
            {
                count += inventoryManager.GetItemCount(entry.Value);
            }
        }
        return count;
    }

    public bool CanCraftShapedRecipe(string recipeId, int[,] craftingGrid, int gridWidth, int gridHeight)
    {
        if (!recipes.TryGetValue(recipeId, out CraftingRecipe recipe))
        {
            return false;
        }

        if (!recipe.isShaped)
        {
            return CanCraftRecipe(recipeId);
        }

        if (recipe.width <= 0 || recipe.height <= 0)
        {
            return CanCraftRecipe(recipeId);
        }

        if (recipe.width > gridWidth || recipe.height > gridHeight)
        {
            return false;
        }

        for (int offsetY = 0; offsetY <= gridHeight - recipe.height; offsetY++)
        {
            for (int offsetX = 0; offsetX <= gridWidth - recipe.width; offsetX++)
            {
                if (MatchesShapedPattern(recipe, craftingGrid, gridWidth, gridHeight, offsetX, offsetY))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchesShapedPattern(CraftingRecipe recipe, int[,] craftingGrid, int gridWidth, int gridHeight, int offsetX, int offsetY)
    {
        int ingredientIndex = 0;
        for (int y = 0; y < recipe.height; y++)
        {
            for (int x = 0; x < recipe.width; x++)
            {
                int gridX = x + offsetX;
                int gridY = y + offsetY;

                if (gridX >= gridWidth || gridY >= gridHeight)
                {
                    return false;
                }

                int gridItemId = craftingGrid[gridX, gridY];

                if (ingredientIndex >= recipe.ingredients.Length)
                {
                    if (gridItemId != 0)
                    {
                        return false;
                    }
                    continue;
                }

                CraftingIngredient ingredient = recipe.ingredients[ingredientIndex];
                if (gridItemId != ingredient.itemId)
                {
                    return false;
                }

                ingredientIndex++;
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
            bool replaced = false;
            foreach (CraftingReplacement replacement in currentRecipe.replacements)
            {
                if (ingredient.itemId == replacement.consumeItemId)
                {
                    inventoryManager.RemoveItem(ingredient.itemId, ingredient.amount);
                    inventoryManager.AddItem(replacement.replaceWithItemId, ingredient.amount);
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                inventoryManager.RemoveItem(ingredient.itemId, ingredient.amount);
            }
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

    public bool TryGetFuelBurnTime(int itemId, out float burnTime)
    {
        return fuelBurnTimeByItemId.TryGetValue(itemId, out burnTime);
    }

    public bool TryGetFuelBurnTime(byte itemId, out float burnTime)
    {
        return TryGetFuelBurnTime((int)itemId, out burnTime);
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
    public CraftingReplacement[] replacements = Array.Empty<CraftingReplacement>();
    public bool isShaped;
    public int width;
    public int height;
}

[Serializable]
public class CraftingIngredient
{
    public int itemId;
    public int amount;
    public string group = string.Empty;
}

[Serializable]
public class CraftingResult
{
    public int itemId;
    public int amount;
}

[Serializable]
public class CraftingReplacement
{
    public int consumeItemId;
    public int replaceWithItemId;
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
