using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GameServerApp.Systems;

/// <summary>
/// Loads data-driven gameplay catalogs (items/recipes) from config/game-data.
/// </summary>
public sealed class GameDataCatalog
{
    private readonly Dictionary<string, GameDataItemDefinition> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _groupIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<GameDataRecipeDefinition> _recipes = new();

    public string DataDirectory { get; private set; } = string.Empty;

    public IReadOnlyDictionary<string, GameDataItemDefinition> Items => _items;

    public IReadOnlyList<GameDataRecipeDefinition> Recipes => _recipes;

    public static GameDataCatalog LoadDefault()
    {
        var catalog = new GameDataCatalog();
        foreach (string candidate in BuildCandidateDataDirectories())
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            if (catalog.TryLoadFromDirectory(candidate, out string error))
            {
                Console.WriteLine($"[GameDataCatalog] Loaded items={catalog._items.Count}, recipes={catalog._recipes.Count} from {candidate}");
                return catalog;
            }

            Console.WriteLine($"[GameDataCatalog][WARN] Failed to load catalog from {candidate}: {error}");
        }

        Console.WriteLine("[GameDataCatalog][WARN] Falling back to empty catalog. Recipe and stack-size fallbacks will be used.");
        return catalog;
    }

    public int GetMaxStack(string itemId)
    {
        if (!TryGetItem(itemId, out GameDataItemDefinition? item))
        {
            return 64;
        }

        return Math.Clamp(item.StackMax, 1, 999);
    }

    public bool ItemHasGroup(string itemId, string group)
    {
        if (string.IsNullOrWhiteSpace(group) || !TryGetItem(itemId, out GameDataItemDefinition? item))
        {
            return false;
        }

        return item.Groups.Contains(group, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetItem(string itemId, out GameDataItemDefinition? item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (_items.TryGetValue(itemId, out item))
        {
            return true;
        }

        int separator = itemId.IndexOf(':');
        if (separator >= 0 && separator < itemId.Length - 1)
        {
            string simpleId = itemId[(separator + 1)..];
            return _items.TryGetValue(simpleId, out item);
        }

        return false;
    }

    private bool TryLoadFromDirectory(string directory, out string error)
    {
        error = string.Empty;

        string itemsPath = Path.Combine(directory, "items.json");
        string recipesPath = Path.Combine(directory, "recipes.json");
        if (!File.Exists(itemsPath) || !File.Exists(recipesPath))
        {
            error = "Missing items.json or recipes.json.";
            return false;
        }

        try
        {
            _items.Clear();
            _recipes.Clear();
            _groupIndex.Clear();

            ParseItems(itemsPath);
            ParseRecipes(recipesPath);
            DataDirectory = directory;
            return true;
        }
        catch (Exception ex)
        {
            _items.Clear();
            _recipes.Clear();
            _groupIndex.Clear();
            DataDirectory = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private void ParseItems(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"items.json root must be an array ({path}).");
        }

        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string id = ReadString(entry, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            string type = ReadString(entry, "type");
            int stackMax = ReadInt(entry, "stack_max", "max_stack", "maxStack");
            if (stackMax <= 0)
            {
                bool stackable = ReadBoolean(entry, defaultValue: true, "stackable");
                stackMax = stackable ? InferStackMaxByType(type) : 1;
            }

            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string group in ReadStringArray(entry, "groups"))
            {
                groups.Add(group);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                groups.Add(type.Trim());
            }

            var item = new GameDataItemDefinition
            {
                Id = id,
                Type = string.IsNullOrWhiteSpace(type) ? "material" : type,
                Name = ReadString(entry, "name", "display_name", "displayName"),
                StackMax = Math.Max(1, stackMax),
                Durability = ReadInt(entry, "durability"),
                HungerRestore = ReadInt(entry, "hunger_restore", "nutrition"),
                Groups = groups.ToArray()
            };

            _items[id] = item;
            foreach (string group in item.Groups)
            {
                if (!_groupIndex.TryGetValue(group, out HashSet<string>? members))
                {
                    members = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _groupIndex[group] = members;
                }

                members.Add(id);
            }
        }
    }

    private void ParseRecipes(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"recipes.json root must be an array ({path}).");
        }

        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string id = ReadString(entry, "id", "recipeId");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            GameDataCraftMethod method = ParseCraftMethod(ReadString(entry, "method", "craft_method", "type"));
            float craftTime = ReadFloat(entry, "craft_time", "crafting_time", "craftingTime");

            var ingredients = new List<GameDataIngredientDefinition>();
            if (entry.TryGetProperty("ingredients", out JsonElement ingredientsElement) &&
                ingredientsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement ingredientElement in ingredientsElement.EnumerateArray())
                {
                    if (ingredientElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    string itemId = ReadString(ingredientElement, "item_id", "itemId");
                    string group = ReadString(ingredientElement, "group");
                    int amount = ReadInt(ingredientElement, "count", "amount", "quantity");
                    if (amount <= 0 || (string.IsNullOrWhiteSpace(itemId) && string.IsNullOrWhiteSpace(group)))
                    {
                        continue;
                    }

                    ingredients.Add(new GameDataIngredientDefinition
                    {
                        ItemId = itemId,
                        Group = group,
                        Amount = amount
                    });
                }
            }

            var results = new List<GameDataResultDefinition>();
            if (entry.TryGetProperty("results", out JsonElement resultsElement) &&
                resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement resultElement in resultsElement.EnumerateArray())
                {
                    TryAddResult(results, resultElement);
                }
            }
            else if (entry.TryGetProperty("result", out JsonElement resultElement))
            {
                TryAddResult(results, resultElement);
            }

            var replacements = new List<GameDataReplacementDefinition>();
            if (entry.TryGetProperty("replacements", out JsonElement replacementsElement) &&
                replacementsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement replacementElement in replacementsElement.EnumerateArray())
                {
                    if (replacementElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    string from = ReadString(replacementElement, "from", "input", "consume");
                    string to = ReadString(replacementElement, "to", "output", "replace_with");
                    if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                    {
                        continue;
                    }

                    replacements.Add(new GameDataReplacementDefinition
                    {
                        From = from,
                        To = to
                    });
                }
            }

            if (ingredients.Count == 0 || results.Count == 0)
            {
                continue;
            }

            _recipes.Add(new GameDataRecipeDefinition
            {
                Id = id,
                Name = ReadString(entry, "name", "displayName", "display_name"),
                Method = method,
                CraftTime = Math.Max(0.0f, craftTime),
                Ingredients = ingredients,
                Results = results,
                Replacements = replacements
            });
        }
    }

    private static void TryAddResult(ICollection<GameDataResultDefinition> results, JsonElement resultElement)
    {
        if (resultElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        string itemId = ReadString(resultElement, "item_id", "itemId");
        int amount = ReadInt(resultElement, "count", "amount", "quantity");
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        results.Add(new GameDataResultDefinition
        {
            ItemId = itemId,
            Amount = amount
        });
    }

    private static IEnumerable<string> ReadStringArray(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out JsonElement arrayElement) ||
            arrayElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement value in arrayElement.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string text = value.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text.Trim();
            }
        }
    }

    private static string ReadString(JsonElement source, params string[] names)
    {
        foreach (string name in names)
        {
            if (!source.TryGetProperty(name, out JsonElement property) || property.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string text = property.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return string.Empty;
    }

    private static int ReadInt(JsonElement source, params string[] names)
    {
        foreach (string name in names)
        {
            if (!source.TryGetProperty(name, out JsonElement property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int numeric))
            {
                return numeric;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static float ReadFloat(JsonElement source, params string[] names)
    {
        foreach (string name in names)
        {
            if (!source.TryGetProperty(name, out JsonElement property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double numeric))
            {
                return (float)numeric;
            }

            if (property.ValueKind == JsonValueKind.String &&
                float.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
        }

        return 0.0f;
    }

    private static bool ReadBoolean(JsonElement source, bool defaultValue, params string[] names)
    {
        foreach (string name in names)
        {
            if (!source.TryGetProperty(name, out JsonElement property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool parsed))
            {
                return parsed;
            }
        }

        return defaultValue;
    }

    private static int InferStackMaxByType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return 64;
        }

        string normalized = type.Trim().ToLowerInvariant();
        return normalized switch
        {
            "tool" => 1,
            "weapon" => 1,
            "armor" => 1,
            _ => 64
        };
    }

    private static GameDataCraftMethod ParseCraftMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return GameDataCraftMethod.Normal;
        }

        return method.Trim().ToUpperInvariant() switch
        {
            "NORMAL" => GameDataCraftMethod.Normal,
            "COOKING" => GameDataCraftMethod.Cooking,
            "FUEL" => GameDataCraftMethod.Fuel,
            _ => GameDataCraftMethod.Normal
        };
    }

    private static IEnumerable<string> BuildCandidateDataDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DirectoryInfo? current = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (int depth = 0; depth < 10 && current != null; depth++)
        {
            string repoConfig = Path.Combine(current.FullName, "config", "game-data");
            if (seen.Add(repoConfig))
            {
                yield return repoConfig;
            }

            string serverConfig = Path.Combine(current.FullName, "GameServer", "config", "game-data");
            if (seen.Add(serverConfig))
            {
                yield return serverConfig;
            }

            current = current.Parent;
        }
    }
}

public enum GameDataCraftMethod
{
    Normal,
    Cooking,
    Fuel
}

public sealed class GameDataItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "material";
    public int StackMax { get; set; } = 64;
    public int Durability { get; set; }
    public int HungerRestore { get; set; }
    public string[] Groups { get; set; } = Array.Empty<string>();
}

public sealed class GameDataRecipeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GameDataCraftMethod Method { get; set; } = GameDataCraftMethod.Normal;
    public float CraftTime { get; set; }
    public List<GameDataIngredientDefinition> Ingredients { get; set; } = new();
    public List<GameDataResultDefinition> Results { get; set; } = new();
    public List<GameDataReplacementDefinition> Replacements { get; set; } = new();
}

public sealed class GameDataIngredientDefinition
{
    public string ItemId { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class GameDataResultDefinition
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class GameDataReplacementDefinition
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
