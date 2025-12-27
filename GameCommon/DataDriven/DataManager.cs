using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
}
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
}
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data manager for all game data (blocks, items, recipes, etc.)
    /// Provides data-driven approach with JSON serialization, validation, and caching
    /// </summary>
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();
        
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<string, BlockData> _blocks = new();
        private readonly Dictionary<string, ItemData> _items = new();
        private readonly Dictionary<string, RecipeData> _recipes = new();
        private readonly Dictionary<string, FoodData> _foodItems = new();
        private readonly Dictionary<string, DrinkData> _drinkItems = new();
        private readonly Dictionary<string, EffectData> _effects = new();
        private readonly Dictionary<string, BiomeData> _biomes = new();
        private readonly Dictionary<string, EntityData> _entities = new();
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        private readonly string _dataPath = "config/";
        
        #region Public Properties
        public IReadOnlyDictionary<string, BlockData> Blocks => _blocks;
        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;
        public IReadOnlyDictionary<string, FoodData> FoodItems => _foodItems;
        public IReadOnlyDictionary<string, DrinkData> DrinkItems => _drinkItems;
        public IReadOnlyDictionary<string, EffectData> Effects => _effects;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;
        public IReadOnlyDictionary<string, EntityData> Entities => _entities;
        #endregion
        
        private DataManager()
        {
            LoadAllData();
        }
        
        #region Data Loading
        public void LoadAllData()
        {
            try
            {
                LoadBlocks();
                LoadItems();
                LoadRecipes();
                LoadFoodData();
                LoadDrinkData();
                LoadEffects();
                LoadBiomes();
                LoadEntities();
                
                ValidateAllData();
                
                Debug.Log("[DataManager] All game data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to load game data: {ex.Message}");
                throw;
            }
        }
        
        public void ReloadData(string dataType)
        {
            try
            {
                switch (dataType.ToLower())
                {
                    case "blocks":
                        LoadBlocks();
                        break;
                    case "items":
                        LoadItems();
                        break;
                    case "recipes":
                        LoadRecipes();
                        break;
                    case "food":
                        LoadFoodData();
                        break;
                    case "drinks":
                        LoadDrinkData();
                        break;
                    case "effects":
                        LoadEffects();
                        break;
                    case "biomes":
                        LoadBiomes();
                        break;
                    case "entities":
                        LoadEntities();
                        break;
                    case "all":
                        LoadAllData();
                        break;
                    default:
                        Debug.LogWarning($"[DataManager] Unknown data type: {dataType}");
                        break;
                }
                
                Debug.Log($"[DataManager] Reloaded {dataType} data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to reload {dataType} data: {ex.Message}");
            }
        }
        
        private void LoadBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var blocksData = LoadJsonData<BlockDataContainer>(filePath);
            
            if (blocksData?.Blocks != null)
            {
                _blocks.Clear();
                foreach (var block in blocksData.Blocks)
                {
                    _blocks[block.Name] = block;
                }
            }
        }
        
        private void LoadItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var itemsData = LoadJsonData<ItemsDataContainer>(filePath);
            
            if (itemsData?.Items != null)
            {
                _items.Clear();
                foreach (var item in itemsData.Items)
                {
                    _items[item.ItemId] = item;
                }
            }
        }
        
        private void LoadRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var recipesData = LoadJsonData<RecipeDataContainer>(filePath);
            
            if (recipesData?.Recipes != null)
            {
                _recipes.Clear();
                foreach (var recipe in recipesData.Recipes)
                {
                    _recipes[recipe.RecipeId] = recipe;
                }
            }
        }
        
        private void LoadFoodData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var foodData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (foodData?.FoodItems != null)
            {
                _foodItems.Clear();
                foreach (var kvp in foodData.FoodItems)
                {
                    _foodItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadDrinkData()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var drinkData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (drinkData?.DrinkItems != null)
            {
                _drinkItems.Clear();
                foreach (var kvp in drinkData.DrinkItems)
                {
                    _drinkItems[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadEffects()
        {
            var filePath = Path.Combine(_dataPath, "hunger_config.json");
            var effectsData = LoadJsonData<HungerConfigContainer>(filePath);
            
            if (effectsData?.Effects != null)
            {
                _effects.Clear();
                foreach (var kvp in effectsData.Effects)
                {
                    _effects[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void LoadBiomes()
        {
            // Load biome data from world configuration
            var filePath = Path.Combine(_dataPath, "world.json");
            var worldData = LoadJsonData<WorldConfigContainer>(filePath);
            
            if (worldData?.Biomes != null)
            {
                _biomes.Clear();
                foreach (var biome in worldData.Biomes)
                {
                    _biomes[biome.Name] = biome;
                }
            }
        }
        
        private void LoadEntities()
        {
            // For now, create basic entity data
            _entities.Clear();
            
            // Add basic entities
            _entities["player"] = new EntityData
            {
                Id = "player",
                Name = "Player",
                Type = EntityType.Player,
                Health = 20,
                Speed = 4.317f,
                Width = 0.6f,
                Height = 1.8f
            };
            
            _entities["zombie"] = new EntityData
            {
                Id = "zombie",
                Name = "Zombie",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 3
            };
            
            _entities["skeleton"] = new EntityData
            {
                Id = "skeleton",
                Name = "Skeleton",
                Type = EntityType.HostileMob,
                Health = 20,
                Speed = 1.2f,
                Width = 0.6f,
                Height = 1.8f,
                Damage = 4
            };
            
            _entities["cow"] = new EntityData
            {
                Id = "cow",
                Name = "Cow",
                Type = EntityType.PassiveMob,
                Health = 10,
                Speed = 0.8f,
                Width = 0.9f,
                Height = 1.4f
            };
        }
        
        private T LoadJsonData<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DataManager] Data file not found: {filePath}");
                    return default(T);
                }
                
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Error loading data from {filePath}: {ex.Message}");
                return default(T);
            }
        }
        #endregion
        
        #region Data Access
        public BlockData GetBlock(string name)
        {
            return _blocks.TryGetValue(name, out var block) ? block : null;
        }
        
        public BlockData GetBlock(int type)
        {
            return _blocks.Values.FirstOrDefault(b => b.Type == type);
        }
        
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item : null;
        }
        
        public RecipeData GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }
        
        public IEnumerable<RecipeData> GetRecipesByCategory(string category)
        {
            return _recipes.Values.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        
        public IEnumerable<RecipeData> GetRecipesForItem(string itemId)
        {
            return _recipes.Values.Where(r => r.Results.Any(result => result.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)));
        }
        
        public FoodData GetFood(string itemId)
        {
            return _foodItems.TryGetValue(itemId, out var food) ? food : null;
        }
        
        public DrinkData GetDrink(string itemId)
        {
            return _drinkItems.TryGetValue(itemId, out var drink) ? drink : null;
        }
        
        public EffectData GetEffect(string effectType)
        {
            return _effects.TryGetValue(effectType, out var effect) ? effect : null;
        }
        
        public BiomeData GetBiome(string name)
        {
            return _biomes.TryGetValue(name, out var biome) ? biome : null;
        }
        
        public EntityData GetEntity(string entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public IEnumerable<EntityData> GetEntitiesByType(EntityType type)
        {
            return _entities.Values.Where(e => e.Type == type);
        }
        #endregion
        
        #region Data Validation
        public void ValidateAllData()
        {
            ValidateBlocks();
            ValidateItems();
            ValidateRecipes();
            ValidateFoodData();
            ValidateDrinkData();
            ValidateEffects();
            ValidateBiomes();
            ValidateEntities();
        }
        
        private void ValidateBlocks()
        {
            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.Name))
                {
                    Debug.LogError($"[DataManager] Block has invalid name: {block.Type}");
                }
                
                if (block.Type < 0)
                {
                    Debug.LogError($"[DataManager] Block has invalid type: {block.Name} ({block.Type})");
                }
                
                if (block.Hardness < 0 && block.Type != 0) // Air can have 0 hardness
                {
                    Debug.LogError($"[DataManager] Block has invalid hardness: {block.Name} ({block.Hardness})");
                }
            }
        }
        
        private void ValidateItems()
        {
            foreach (var item in _items.Values)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[DataManager] Item has invalid ID: {item.DisplayName}");
                }
                
                if (item.MaxStackSize <= 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid max stack size: {item.ItemId} ({item.MaxStackSize})");
                }
                
                if (item.Value < 0)
                {
                    Debug.LogError($"[DataManager] Item has invalid value: {item.ItemId} ({item.Value})");
                }
            }
        }
        
        private void ValidateRecipes()
        {
            foreach (var recipe in _recipes.Values)
            {
                if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    Debug.LogError($"[DataManager] Recipe has invalid ID: {recipe.DisplayName}");
                }
                
                if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no ingredients: {recipe.RecipeId}");
                }
                
                if (recipe.Results == null || recipe.Results.Count == 0)
                {
                    Debug.LogError($"[DataManager] Recipe has no results: {recipe.RecipeId}");
                }
            }
        }
        
        private void ValidateFoodData()
        {
            foreach (var food in _foodItems.Values)
            {
                if (string.IsNullOrWhiteSpace(food.ItemId))
                {
                    Debug.LogError($"[DataManager] Food has invalid ID: {food.DisplayName}");
                }
                
                if (food.Nutrition < 0)
                {
                    Debug.LogError($"[DataManager] Food has invalid nutrition: {food.ItemId} ({food.Nutrition})");
                }
            }
        }
        
        private void ValidateDrinkData()
        {
            foreach (var drink in _drinkItems.Values)
            {
                if (string.IsNullOrWhiteSpace(drink.ItemId))
                {
                    Debug.LogError($"[DataManager] Drink has invalid ID: {drink.DisplayName}");
                }
                
                if (drink.Hydration < 0)
                {
                    Debug.LogError($"[DataManager] Drink has invalid hydration: {drink.ItemId} ({drink.Hydration})");
                }
            }
        }
        
        private void ValidateEffects()
        {
            foreach (var effect in _effects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.Type))
                {
                    Debug.LogError($"[DataManager] Effect has invalid type: {effect.DisplayName}");
                }
                
                if (effect.Duration < 0)
                {
                    Debug.LogError($"[DataManager] Effect has invalid duration: {effect.Type} ({effect.Duration})");
                }
            }
        }
        
        private void ValidateBiomes()
        {
            foreach (var biome in _biomes.Values)
            {
                if (string.IsNullOrWhiteSpace(biome.Name))
                {
                    Debug.LogError($"[DataManager] Biome has invalid name: {biome.Temperature}");
                }
                
                if (biome.Temperature < -50 || biome.Temperature > 50)
                {
                    Debug.LogError($"[DataManager] Biome has invalid temperature: {biome.Name} ({biome.Temperature})");
                }
            }
        }
        
        private void ValidateEntities()
        {
            foreach (var entity in _entities.Values)
            {
                if (string.IsNullOrWhiteSpace(entity.Id))
                {
                    Debug.LogError($"[DataManager] Entity has invalid ID: {entity.Name}");
                }
                
                if (entity.Health <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid health: {entity.Id} ({entity.Health})");
                }
                
                if (entity.Speed <= 0)
                {
                    Debug.LogError($"[DataManager] Entity has invalid speed: {entity.Id} ({entity.Speed})");
                }
            }
        }
        #endregion
        
        #region Data Export/Import
        public void ExportData(string dataType, string outputPath)
        {
            try
            {
                object dataToExport = dataType.ToLower() switch
                {
                    "blocks" => new { Blocks = _blocks.Values.ToList() },
                    "items" => new { Items = _items.Values.ToList() },
                    "recipes" => new { Recipes = _recipes.Values.ToList() },
                    "food" => new { FoodItems = _foodItems.ToList() },
                    "drinks" => new { DrinkItems = _drinkItems.ToList() },
                    "effects" => new { Effects = _effects.Values.ToList() },
                    "biomes" => new { Biomes = _biomes.Values.ToList() },
                    "entities" => new { Entities = _entities.Values.ToList() },
                    "all" => new
                    {
                        Blocks = _blocks.Values.ToList(),
                        Items = _items.Values.ToList(),
                        Recipes = _recipes.Values.ToList(),
                        FoodItems = _foodItems.ToList(),
                        DrinkItems = _drinkItems.ToList(),
                        Effects = _effects.Values.ToList(),
                        Biomes = _biomes.Values.ToList(),
                        Entities = _entities.Values.ToList()
                    },
                    _ => throw new ArgumentException($"Unknown data type: {dataType}")
                };
                
                var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);
                File.WriteAllText(outputPath, json);
                
                Debug.Log($"[DataManager] Exported {dataType} data to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to export {dataType} data: {ex.Message}");
            }
        }
        
        public void ImportData(string dataType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Data file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (dataType.ToLower())
                {
                    case "blocks":
                        var blocksData = JsonSerializer.Deserialize<BlockDataContainer>(json, _jsonOptions);
                        if (blocksData?.Blocks != null)
                        {
                            _blocks.Clear();
                            foreach (var block in blocksData.Blocks)
                            {
                                _blocks[block.Name] = block;
                            }
                        }
                        break;
                    case "items":
                        var itemsData = JsonSerializer.Deserialize<ItemsDataContainer>(json, _jsonOptions);
                        if (itemsData?.Items != null)
                        {
                            _items.Clear();
                            foreach (var item in itemsData.Items)
                            {
                                _items[item.ItemId] = item;
                            }
                        }
                        break;
                    case "recipes":
                        var recipesData = JsonSerializer.Deserialize<RecipeDataContainer>(json, _jsonOptions);
                        if (recipesData?.Recipes != null)
                        {
                            _recipes.Clear();
                            foreach (var recipe in recipesData.Recipes)
                            {
                                _recipes[recipe.RecipeId] = recipe;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unsupported import data type: {dataType}");
                }
                
                ValidateAllData();
                Debug.Log($"[DataManager] Imported {dataType} data from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataManager] Failed to import {dataType} data: {ex.Message}");
            }
        }
        #endregion
        
        #region Runtime Data Modification
        public void AddBlock(BlockData block)
        {
            _blocks[block.Name] = block;
            SaveBlocks();
        }
        
        public void AddItem(ItemData item)
        {
            _items[item.ItemId] = item;
            SaveItems();
        }
        
        public void AddRecipe(RecipeData recipe)
        {
            _recipes[recipe.RecipeId] = recipe;
            SaveRecipes();
        }
        
        public void RemoveBlock(string name)
        {
            _blocks.Remove(name);
            SaveBlocks();
        }
        
        public void RemoveItem(string itemId)
        {
            _items.Remove(itemId);
            SaveItems();
        }
        
        public void RemoveRecipe(string recipeId)
        {
            _recipes.Remove(recipeId);
            SaveRecipes();
        }
        
        private void SaveBlocks()
        {
            var filePath = Path.Combine(_dataPath, "blocks.json");
            var container = new BlockDataContainer { Blocks = _blocks.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveItems()
        {
            var filePath = Path.Combine(_dataPath, "items.json");
            var container = new ItemsDataContainer { Items = _items.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        private void SaveRecipes()
        {
            var filePath = Path.Combine(_dataPath, "recipes.json");
            var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
            var json = JsonSerializer.Serialize(container, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        #endregion
    }
}
