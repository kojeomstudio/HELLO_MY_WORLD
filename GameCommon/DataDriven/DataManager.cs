using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GameCommon.DataDriven
{
    /// <summary>
    /// Centralized data loader for blocks, items, and recipes.
    /// Reads JSON assets into dictionaries so both client and server can stay data-driven.
    /// </summary>
    public sealed class DataManager
    {
        private static readonly Lazy<DataManager> _instance = new(() => new DataManager());
        public static DataManager Instance => _instance.Value;

        private readonly Dictionary<string, BlockData> _blocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ItemData> _items = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RecipeData> _recipes = new(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        private DataManager()
        {
        }

        public void LoadAll(string rootPath = "config/data")
        {
            LoadBlocks(Path.Combine(rootPath, "blocks.json"));
            LoadItems(Path.Combine(rootPath, "items.json"));
            LoadRecipes(Path.Combine(rootPath, "recipes.json"));
        }

        public void LoadBlocks(string path)
        {
            LoadCollection(path, _blocks, data => data.Name);
        }

        public void LoadItems(string path)
        {
            LoadCollection(path, _items, data => data.ItemId);
        }

        public void LoadRecipes(string path)
        {
            LoadCollection(path, _recipes, data => data.RecipeId);
        }

        private void LoadCollection<T>(
            string path,
            Dictionary<string, T> target,
            Func<T, string?> keySelector)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"[DataManager] No data file at '{path}', skipping.");
                    target.Clear();
                    return;
                }

                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
                target.Clear();
                foreach (var entry in list)
                {
                    var key = keySelector(entry);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        target[key] = entry;
                    }
                }

                Console.WriteLine($"[DataManager] Loaded {target.Count} entries from '{path}'.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DataManager] Failed to load '{path}': {ex.Message}");
            }
        }

        public IEnumerable<BlockData> Blocks => _blocks.Values;
        public IEnumerable<ItemData> Items => _items.Values;
        public IEnumerable<RecipeData> Recipes => _recipes.Values;

        public bool TryGetBlock(string name, out BlockData data)
        {
            return _blocks.TryGetValue(name, out data!);
        }

        public bool TryGetItem(string itemId, out ItemData data)
        {
            return _items.TryGetValue(itemId, out data!);
        }

        public bool TryGetRecipe(string recipeId, out RecipeData data)
        {
            return _recipes.TryGetValue(recipeId, out data!);
        }

        public void SaveRecipes(string path)
        {
            try
            {
                var container = new RecipeDataContainer { Recipes = _recipes.Values.ToList() };
                var json = JsonSerializer.Serialize(container, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DataManager] Failed to save recipes to '{path}': {ex.Message}");
            }
        }
    }
}
