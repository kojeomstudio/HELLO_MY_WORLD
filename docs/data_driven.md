# Data-Driven Approach Documentation

## Overview

This document describes the data-driven approach used in the Minecraft-style game server. The data-driven approach ensures that game data is externalized in JSON format, making it easy to modify without code changes.

## Architecture

### Core Components

- **JSON Configuration Files**: All game data stored in JSON format
- **Data Loaders**: Load and parse JSON data
- **Data Registries**: Store and manage loaded data
- **Data Validators**: Validate data integrity

### Data-driven Features

1. **JSON Format**: All game data in JSON format
2. **Hot-reload Support**: Data can be reloaded without restart
3. **Validation**: Comprehensive validation of data
4. **Extensibility**: Easy to add new data types
5. **Versioning**: Support for data versioning

## Game Data Files

### Blocks Data

**File**: `config/blocks.json`

Blocks data defines all block types in the game:

```json
{
  "blocks": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "solid": false,
      "transparent": true,
      "liquid": false,
      "hardness": 0.0,
      "resistance": 0.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 1.5,
      "resistance": 6.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 1,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 2,
      "name": "grass",
      "displayName": "Grass",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.6,
      "resistance": 0.6,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 3,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 3,
      "name": "dirt",
      "displayName": "Dirt",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.5,
      "resistance": 0.5,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 3,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 8,
      "name": "water",
      "displayName": "Water",
      "solid": false,
      "transparent": true,
      "liquid": true,
      "hardness": 100.0,
      "resistance": 100.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 9,
      "name": "lava",
      "displayName": "Lava",
      "solid": false,
      "transparent": true,
      "liquid": true,
      "hardness": 100.0,
      "resistance": 100.0,
      "lightLevel": 15,
      "tool": null,
      "drops": []
    },
    {
      "id": 10,
      "name": "wood",
      "displayName": "Wood",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 2.0,
      "resistance": 2.0,
      "lightLevel": 0,
      "tool": "axe",
      "drops": [
        {
          "itemId": 10,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 11,
      "name": "leaves",
      "displayName": "Leaves",
      "solid": true,
      "transparent": true,
      "liquid": false,
      "hardness": 0.2,
      "resistance": 0.2,
      "lightLevel": 0,
      "tool": null,
      "drops": [
        {
          "itemId": 10,
          "minCount": 0,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 12,
      "name": "sand",
      "displayName": "Sand",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.5,
      "resistance": 0.5,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 12,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 13,
      "name": "gravel",
      "displayName": "Gravel",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.6,
      "resistance": 0.6,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 13,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 14,
      "name": "coal_ore",
      "displayName": "Coal Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 263,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 15,
      "name": "iron_ore",
      "displayName": "Iron Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 264,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 16,
      "name": "gold_ore",
      "displayName": "Gold Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 265,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 17,
      "name": "diamond_ore",
      "displayName": "Diamond Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 266,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    }
  ]
}
```

### Items Data

**File**: `config/items.json`

Items data defines all item types in the game:

```json
{
  "items": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "maxStackSize": 1,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 2,
      "name": "grass",
      "displayName": "Grass",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 3,
      "name": "dirt",
      "displayName": "Dirt",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 256,
      "name": "iron_shovel",
      "displayName": "Iron Shovel",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "shovel",
      "toolPower": 2
    },
    {
      "id": 257,
      "name": "iron_pickaxe",
      "displayName": "Iron Pickaxe",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "pickaxe",
      "toolPower": 2
    },
    {
      "id": 258,
      "name": "iron_axe",
      "displayName": "Iron Axe",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "axe",
      "toolPower": 2
    },
    {
      "id": 259,
      "name": "flint_and_steel",
      "displayName": "Flint and Steel",
      "maxStackSize": 1,
      "maxDurability": 65,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 260,
      "name": "apple",
      "displayName": "Apple",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 261,
      "name": "bow",
      "displayName": "Bow",
      "maxStackSize": 1,
      "maxDurability": 384,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 262,
      "name": "arrow",
      "displayName": "Arrow",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 263,
      "name": "coal",
      "displayName": "Coal",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 264,
      "name": "iron_ingot",
      "displayName": "Iron Ingot",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 265,
      "name": "gold_ingot",
      "displayName": "Gold Ingot",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 266,
      "name": "diamond",
      "displayName": "Diamond",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    }
  ]
}
```

### Recipes Data

**File**: `config/recipes.json`

Recipes data defines all crafting recipes:

```json
{
  "recipes": [
    {
      "result": {
        "itemId": 1,
        "count": 4
      },
      "pattern": [
        "S",
        "S"
      ],
      "ingredients": {
        "S": {
          "itemId": 3,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 5,
        "count": 1
      },
      "pattern": [
        "WW",
        "WW"
      ],
      "ingredients": {
        "W": {
          "itemId": 10,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 58,
        "count": 1
      },
      "pattern": [
        "CCC",
        "CSC",
        "CCC"
      ],
      "ingredients": {
        "C": {
          "itemId": 4,
          "metadata": null
        },
        "S": {
          "itemId": 331,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 256,
        "count": 1
      },
      "pattern": [
        " I ",
        " S ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 257,
        "count": 1
      },
      "pattern": [
        "III",
        " S ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 258,
        "count": 1
      },
      "pattern": [
        "II ",
        "IS ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    }
  ]
}
```

### Biomes Data

**File**: `config/biomes.json`

Biomes data defines all biome types:

```json
{
  "biomes": [
    {
      "id": 0,
      "name": "plains",
      "displayName": "Plains",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#91BD59",
      "foliageColor": "#77AB2F"
    },
    {
      "id": 1,
      "name": "forest",
      "displayName": "Forest",
      "temperature": 0.7,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#5E9F3E",
      "foliageColor": "#4A7A2E"
    },
    {
      "id": 2,
      "name": "desert",
      "displayName": "Desert",
      "temperature": 2.0,
      "humidity": 0.0,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#BFB755",
      "foliageColor": "#AEA42A"
    },
    {
      "id": 3,
      "name": "mountains",
      "displayName": "Mountains",
      "temperature": 0.2,
      "humidity": 0.3,
      "baseHeight": 100,
      "heightVariation": 0.5,
      "waterColor": "#3F76E4",
      "grassColor": "#8AB689",
      "foliageColor": "#6DA36B"
    },
    {
      "id": 4,
      "name": "taiga",
      "displayName": "Taiga",
      "temperature": 0.25,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#86B783",
      "foliageColor": "#639B59"
    },
    {
      "id": 5,
      "name": "swamp",
      "displayName": "Swamp",
      "temperature": 0.8,
      "humidity": 0.9,
      "baseHeight": 62,
      "heightVariation": 0.1,
      "waterColor": "#617B64",
      "grassColor": "#6A7039",
      "foliageColor": "#8DB127"
    },
    {
      "id": 6,
      "name": "ocean",
      "displayName": "Ocean",
      "temperature": 0.5,
      "humidity": 0.5,
      "baseHeight": 45,
      "heightVariation": 0.05,
      "waterColor": "#3F76E4",
      "grassColor": "#8EB971",
      "foliageColor": "#71A74D"
    },
    {
      "id": 7,
      "name": "river",
      "displayName": "River",
      "temperature": 0.5,
      "humidity": 0.7,
      "baseHeight": 55,
      "heightVariation": 0.05,
      "waterColor": "#3F76E4",
      "grassColor": "#8EB971",
      "foliageColor": "#71A74D"
    },
    {
      "id": 8,
      "name": "beach",
      "displayName": "Beach",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 62,
      "heightVariation": 0.025,
      "waterColor": "#3F76E4",
      "grassColor": "#F2EDD5",
      "foliageColor": "#D9C9A1"
    },
    {
      "id": 9,
      "name": "jungle",
      "displayName": "Jungle",
      "temperature": 0.95,
      "humidity": 0.9,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#59C93C",
      "foliageColor": "#30BB0B"
    }
  ]
}
```

## Data Loading

### Block Data Loading

```csharp
public class BlockData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Solid { get; set; }
    public bool Transparent { get; set; }
    public bool Liquid { get; set; }
    public double Hardness { get; set; }
    public double Resistance { get; set; }
    public int LightLevel { get; set; }
    public string Tool { get; set; }
    public List<BlockDrop> Drops { get; set; }
}

public class BlockDrop
{
    public int ItemId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
}

public class BlockRegistry
{
    private readonly Dictionary<int, BlockData> _blocks = new();
    private readonly Dictionary<string, BlockData> _blocksByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<BlockDataCollection>(json);
        
        foreach (var block in data.Blocks)
        {
            _blocks[block.Id] = block;
            _blocksByName[block.Name] = block;
        }
    }

    public BlockData GetBlock(int id)
    {
        return _blocks.TryGetValue(id, out var block) ? block : null;
    }

    public BlockData GetBlock(string name)
    {
        return _blocksByName.TryGetValue(name, out var block) ? block : null;
    }
}

public class BlockDataCollection
{
    public List<BlockData> Blocks { get; set; }
}
```

### Item Data Loading

```csharp
public class ItemData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public int MaxStackSize { get; set; }
    public int MaxDurability { get; set; }
    public string ToolType { get; set; }
    public int ToolPower { get; set; }
}

public class ItemRegistry
{
    private readonly Dictionary<int, ItemData> _items = new();
    private readonly Dictionary<string, ItemData> _itemsByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<ItemDataCollection>(json);
        
        foreach (var item in data.Items)
        {
            _items[item.Id] = item;
            _itemsByName[item.Name] = item;
        }
    }

    public ItemData GetItem(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public ItemData GetItem(string name)
    {
        return _itemsByName.TryGetValue(name, out var item) ? item : null;
    }
}

public class ItemDataCollection
{
    public List<ItemData> Items { get; set; }
}
```

### Recipe Data Loading

```csharp
public class RecipeData
{
    public RecipeResult Result { get; set; }
    public List<string> Pattern { get; set; }
    public Dictionary<string, RecipeIngredient> Ingredients { get; set; }
}

public class RecipeResult
{
    public int ItemId { get; set; }
    public int Count { get; set; }
}

public class RecipeIngredient
{
    public int ItemId { get; set; }
    public int? Metadata { get; set; }
}

public class RecipeRegistry
{
    private readonly List<RecipeData> _recipes = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<RecipeDataCollection>(json);
        
        _recipes.AddRange(data.Recipes);
    }

    public List<RecipeData> GetRecipes()
    {
        return _recipes;
    }

    public RecipeData FindRecipe(List<List<int>> ingredients)
    {
        // Find matching recipe based on ingredients
        foreach (var recipe in _recipes)
        {
            if (MatchesRecipe(recipe, ingredients))
            {
                return recipe;
            }
        }
        return null;
    }

    private bool MatchesRecipe(RecipeData recipe, List<List<int>> ingredients)
    {
        // Implement recipe matching logic
        return false;
    }
}

public class RecipeDataCollection
{
    public List<RecipeData> Recipes { get; set; }
}
```

### Biome Data Loading

```csharp
public class BiomeData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public int BaseHeight { get; set; }
    public double HeightVariation { get; set; }
    public string WaterColor { get; set; }
    public string GrassColor { get; set; }
    public string FoliageColor { get; set; }
}

public class BiomeRegistry
{
    private readonly Dictionary<int, BiomeData> _biomes = new();
    private readonly Dictionary<string, BiomeData> _biomesByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<BiomeDataCollection>(json);
        
        foreach (var biome in data.Biomes)
        {
            _biomes[biome.Id] = biome;
            _biomesByName[biome.Name] = biome;
        }
    }

    public BiomeData GetBiome(int id)
    {
        return _biomes.TryGetValue(id, out var biome) ? biome : null;
    }

    public BiomeData GetBiome(string name)
    {
        return _biomesByName.TryGetValue(name, out var biome) ? biome : null;
    }
}

public class BiomeDataCollection
{
    public List<BiomeData> Biomes { get; set; }
}
```

## Data Validation

### Validation Overview

Data is validated on load to ensure integrity:

```csharp
public class DataValidator
{
    public static ValidationResult ValidateBlockData(List<BlockData> blocks)
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();

        foreach (var block in blocks)
        {
            // Check for duplicate IDs
            if (ids.Contains(block.Id))
            {
                issues.Add($"Duplicate block ID: {block.Id}");
            }
            ids.Add(block.Id);

            // Validate hardness
            if (block.Hardness < 0)
            {
                issues.Add($"Invalid hardness for block {block.Name}: {block.Hardness}");
            }

            // Validate resistance
            if (block.Resistance < 0)
            {
                issues.Add($"Invalid resistance for block {block.Name}: {block.Resistance}");
            }

            // Validate light level
            if (block.LightLevel < 0 || block.LightLevel > 15)
            {
                issues.Add($"Invalid light level for block {block.Name}: {block.LightLevel}");
            }
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }

    public static ValidationResult ValidateItemData(List<ItemData> items)
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();

        foreach (var item in items)
        {
            // Check for duplicate IDs
            if (ids.Contains(item.Id))
            {
                issues.Add($"Duplicate item ID: {item.Id}");
            }
            ids.Add(item.Id);

            // Validate max stack size
            if (item.MaxStackSize < 1 || item.MaxStackSize > 64)
            {
                issues.Add($"Invalid max stack size for item {item.Name}: {item.MaxStackSize}");
            }

            // Validate max durability
            if (item.MaxDurability < 0)
            {
                issues.Add($"Invalid max durability for item {item.Name}: {item.MaxDurability}");
            }
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; }
}
```

## Hot-reload Support

### Hot-reload Implementation

Data can be hot-reloaded without restart:

```csharp
public class DataHotReload
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

    public void WatchData(string dataPath, Action<string> onReload)
    {
        var directory = Path.GetDirectoryName(dataPath);
        var fileName = Path.GetFileName(dataPath);

        var watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (sender, e) =>
        {
            // Debounce file change events
            Thread.Sleep(100);
            onReload(dataPath);
        };

        watcher.EnableRaisingEvents = true;
        _watchers[dataPath] = watcher;
    }
}
```

## Data-driven Best Practices

### File Organization

1. **Separate Data Files**: Separate data files for different data types
2. **Consistent Naming**: Use consistent naming conventions
3. **Validation**: Validate data on load
4. **Hot-reload**: Support hot-reload for non-critical data
5. **Versioning**: Support data versioning

### Data Structure

1. **JSON Format**: Use JSON for all game data
2. **Hierarchical**: Use hierarchical structure for complex data
3. **Type Safety**: Use strongly-typed models
4. **Extensibility**: Easy to add new data fields
5. **Documentation**: Document data structure

### Performance

1. **Caching**: Cache loaded data
2. **Lazy Loading**: Load data on demand
3. **Indexing**: Use dictionaries for fast lookups
4. **Batch Loading**: Load data in batches
5. **Memory Management**: Manage memory efficiently

## References

- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)
- [`config/biomes.json`](../config/biomes.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

## Overview

This document describes the data-driven approach used in the Minecraft-style game server. The data-driven approach ensures that game data is externalized in JSON format, making it easy to modify without code changes.

## Architecture

### Core Components

- **JSON Configuration Files**: All game data stored in JSON format
- **Data Loaders**: Load and parse JSON data
- **Data Registries**: Store and manage loaded data
- **Data Validators**: Validate data integrity

### Data-driven Features

1. **JSON Format**: All game data in JSON format
2. **Hot-reload Support**: Data can be reloaded without restart
3. **Validation**: Comprehensive validation of data
4. **Extensibility**: Easy to add new data types
5. **Versioning**: Support for data versioning

## Game Data Files

### Blocks Data

**File**: `config/blocks.json`

Blocks data defines all block types in the game:

```json
{
  "blocks": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "solid": false,
      "transparent": true,
      "liquid": false,
      "hardness": 0.0,
      "resistance": 0.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 1.5,
      "resistance": 6.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 1,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 2,
      "name": "grass",
      "displayName": "Grass",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.6,
      "resistance": 0.6,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 3,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 3,
      "name": "dirt",
      "displayName": "Dirt",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.5,
      "resistance": 0.5,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 3,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 8,
      "name": "water",
      "displayName": "Water",
      "solid": false,
      "transparent": true,
      "liquid": true,
      "hardness": 100.0,
      "resistance": 100.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 9,
      "name": "lava",
      "displayName": "Lava",
      "solid": false,
      "transparent": true,
      "liquid": true,
      "hardness": 100.0,
      "resistance": 100.0,
      "lightLevel": 15,
      "tool": null,
      "drops": []
    },
    {
      "id": 10,
      "name": "wood",
      "displayName": "Wood",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 2.0,
      "resistance": 2.0,
      "lightLevel": 0,
      "tool": "axe",
      "drops": [
        {
          "itemId": 10,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 11,
      "name": "leaves",
      "displayName": "Leaves",
      "solid": true,
      "transparent": true,
      "liquid": false,
      "hardness": 0.2,
      "resistance": 0.2,
      "lightLevel": 0,
      "tool": null,
      "drops": [
        {
          "itemId": 10,
          "minCount": 0,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 12,
      "name": "sand",
      "displayName": "Sand",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.5,
      "resistance": 0.5,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 12,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 13,
      "name": "gravel",
      "displayName": "Gravel",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 0.6,
      "resistance": 0.6,
      "lightLevel": 0,
      "tool": "shovel",
      "drops": [
        {
          "itemId": 13,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 14,
      "name": "coal_ore",
      "displayName": "Coal Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 263,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 15,
      "name": "iron_ore",
      "displayName": "Iron Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 264,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 16,
      "name": "gold_ore",
      "displayName": "Gold Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 265,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "id": 17,
      "name": "diamond_ore",
      "displayName": "Diamond Ore",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 3.0,
      "resistance": 3.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 266,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    }
  ]
}
```

### Items Data

**File**: `config/items.json`

Items data defines all item types in the game:

```json
{
  "items": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "maxStackSize": 1,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 2,
      "name": "grass",
      "displayName": "Grass",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 3,
      "name": "dirt",
      "displayName": "Dirt",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 256,
      "name": "iron_shovel",
      "displayName": "Iron Shovel",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "shovel",
      "toolPower": 2
    },
    {
      "id": 257,
      "name": "iron_pickaxe",
      "displayName": "Iron Pickaxe",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "pickaxe",
      "toolPower": 2
    },
    {
      "id": 258,
      "name": "iron_axe",
      "displayName": "Iron Axe",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "axe",
      "toolPower": 2
    },
    {
      "id": 259,
      "name": "flint_and_steel",
      "displayName": "Flint and Steel",
      "maxStackSize": 1,
      "maxDurability": 65,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 260,
      "name": "apple",
      "displayName": "Apple",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 261,
      "name": "bow",
      "displayName": "Bow",
      "maxStackSize": 1,
      "maxDurability": 384,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 262,
      "name": "arrow",
      "displayName": "Arrow",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 263,
      "name": "coal",
      "displayName": "Coal",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 264,
      "name": "iron_ingot",
      "displayName": "Iron Ingot",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 265,
      "name": "gold_ingot",
      "displayName": "Gold Ingot",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 266,
      "name": "diamond",
      "displayName": "Diamond",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    }
  ]
}
```

### Recipes Data

**File**: `config/recipes.json`

Recipes data defines all crafting recipes:

```json
{
  "recipes": [
    {
      "result": {
        "itemId": 1,
        "count": 4
      },
      "pattern": [
        "S",
        "S"
      ],
      "ingredients": {
        "S": {
          "itemId": 3,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 5,
        "count": 1
      },
      "pattern": [
        "WW",
        "WW"
      ],
      "ingredients": {
        "W": {
          "itemId": 10,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 58,
        "count": 1
      },
      "pattern": [
        "CCC",
        "CSC",
        "CCC"
      ],
      "ingredients": {
        "C": {
          "itemId": 4,
          "metadata": null
        },
        "S": {
          "itemId": 331,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 256,
        "count": 1
      },
      "pattern": [
        " I ",
        " S ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 257,
        "count": 1
      },
      "pattern": [
        "III",
        " S ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    },
    {
      "result": {
        "itemId": 258,
        "count": 1
      },
      "pattern": [
        "II ",
        "IS ",
        " S "
      ],
      "ingredients": {
        "I": {
          "itemId": 265,
          "metadata": null
        },
        "S": {
          "itemId": 5,
          "metadata": null
        }
      }
    }
  ]
}
```

### Biomes Data

**File**: `config/biomes.json`

Biomes data defines all biome types:

```json
{
  "biomes": [
    {
      "id": 0,
      "name": "plains",
      "displayName": "Plains",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#91BD59",
      "foliageColor": "#77AB2F"
    },
    {
      "id": 1,
      "name": "forest",
      "displayName": "Forest",
      "temperature": 0.7,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#5E9F3E",
      "foliageColor": "#4A7A2E"
    },
    {
      "id": 2,
      "name": "desert",
      "displayName": "Desert",
      "temperature": 2.0,
      "humidity": 0.0,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#BFB755",
      "foliageColor": "#AEA42A"
    },
    {
      "id": 3,
      "name": "mountains",
      "displayName": "Mountains",
      "temperature": 0.2,
      "humidity": 0.3,
      "baseHeight": 100,
      "heightVariation": 0.5,
      "waterColor": "#3F76E4",
      "grassColor": "#8AB689",
      "foliageColor": "#6DA36B"
    },
    {
      "id": 4,
      "name": "taiga",
      "displayName": "Taiga",
      "temperature": 0.25,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#86B783",
      "foliageColor": "#639B59"
    },
    {
      "id": 5,
      "name": "swamp",
      "displayName": "Swamp",
      "temperature": 0.8,
      "humidity": 0.9,
      "baseHeight": 62,
      "heightVariation": 0.1,
      "waterColor": "#617B64",
      "grassColor": "#6A7039",
      "foliageColor": "#8DB127"
    },
    {
      "id": 6,
      "name": "ocean",
      "displayName": "Ocean",
      "temperature": 0.5,
      "humidity": 0.5,
      "baseHeight": 45,
      "heightVariation": 0.05,
      "waterColor": "#3F76E4",
      "grassColor": "#8EB971",
      "foliageColor": "#71A74D"
    },
    {
      "id": 7,
      "name": "river",
      "displayName": "River",
      "temperature": 0.5,
      "humidity": 0.7,
      "baseHeight": 55,
      "heightVariation": 0.05,
      "waterColor": "#3F76E4",
      "grassColor": "#8EB971",
      "foliageColor": "#71A74D"
    },
    {
      "id": 8,
      "name": "beach",
      "displayName": "Beach",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 62,
      "heightVariation": 0.025,
      "waterColor": "#3F76E4",
      "grassColor": "#F2EDD5",
      "foliageColor": "#D9C9A1"
    },
    {
      "id": 9,
      "name": "jungle",
      "displayName": "Jungle",
      "temperature": 0.95,
      "humidity": 0.9,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#59C93C",
      "foliageColor": "#30BB0B"
    }
  ]
}
```

## Data Loading

### Block Data Loading

```csharp
public class BlockData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Solid { get; set; }
    public bool Transparent { get; set; }
    public bool Liquid { get; set; }
    public double Hardness { get; set; }
    public double Resistance { get; set; }
    public int LightLevel { get; set; }
    public string Tool { get; set; }
    public List<BlockDrop> Drops { get; set; }
}

public class BlockDrop
{
    public int ItemId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
}

public class BlockRegistry
{
    private readonly Dictionary<int, BlockData> _blocks = new();
    private readonly Dictionary<string, BlockData> _blocksByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<BlockDataCollection>(json);
        
        foreach (var block in data.Blocks)
        {
            _blocks[block.Id] = block;
            _blocksByName[block.Name] = block;
        }
    }

    public BlockData GetBlock(int id)
    {
        return _blocks.TryGetValue(id, out var block) ? block : null;
    }

    public BlockData GetBlock(string name)
    {
        return _blocksByName.TryGetValue(name, out var block) ? block : null;
    }
}

public class BlockDataCollection
{
    public List<BlockData> Blocks { get; set; }
}
```

### Item Data Loading

```csharp
public class ItemData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public int MaxStackSize { get; set; }
    public int MaxDurability { get; set; }
    public string ToolType { get; set; }
    public int ToolPower { get; set; }
}

public class ItemRegistry
{
    private readonly Dictionary<int, ItemData> _items = new();
    private readonly Dictionary<string, ItemData> _itemsByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<ItemDataCollection>(json);
        
        foreach (var item in data.Items)
        {
            _items[item.Id] = item;
            _itemsByName[item.Name] = item;
        }
    }

    public ItemData GetItem(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public ItemData GetItem(string name)
    {
        return _itemsByName.TryGetValue(name, out var item) ? item : null;
    }
}

public class ItemDataCollection
{
    public List<ItemData> Items { get; set; }
}
```

### Recipe Data Loading

```csharp
public class RecipeData
{
    public RecipeResult Result { get; set; }
    public List<string> Pattern { get; set; }
    public Dictionary<string, RecipeIngredient> Ingredients { get; set; }
}

public class RecipeResult
{
    public int ItemId { get; set; }
    public int Count { get; set; }
}

public class RecipeIngredient
{
    public int ItemId { get; set; }
    public int? Metadata { get; set; }
}

public class RecipeRegistry
{
    private readonly List<RecipeData> _recipes = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<RecipeDataCollection>(json);
        
        _recipes.AddRange(data.Recipes);
    }

    public List<RecipeData> GetRecipes()
    {
        return _recipes;
    }

    public RecipeData FindRecipe(List<List<int>> ingredients)
    {
        // Find matching recipe based on ingredients
        foreach (var recipe in _recipes)
        {
            if (MatchesRecipe(recipe, ingredients))
            {
                return recipe;
            }
        }
        return null;
    }

    private bool MatchesRecipe(RecipeData recipe, List<List<int>> ingredients)
    {
        // Implement recipe matching logic
        return false;
    }
}

public class RecipeDataCollection
{
    public List<RecipeData> Recipes { get; set; }
}
```

### Biome Data Loading

```csharp
public class BiomeData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public int BaseHeight { get; set; }
    public double HeightVariation { get; set; }
    public string WaterColor { get; set; }
    public string GrassColor { get; set; }
    public string FoliageColor { get; set; }
}

public class BiomeRegistry
{
    private readonly Dictionary<int, BiomeData> _biomes = new();
    private readonly Dictionary<string, BiomeData> _biomesByName = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<BiomeDataCollection>(json);
        
        foreach (var biome in data.Biomes)
        {
            _biomes[biome.Id] = biome;
            _biomesByName[biome.Name] = biome;
        }
    }

    public BiomeData GetBiome(int id)
    {
        return _biomes.TryGetValue(id, out var biome) ? biome : null;
    }

    public BiomeData GetBiome(string name)
    {
        return _biomesByName.TryGetValue(name, out var biome) ? biome : null;
    }
}

public class BiomeDataCollection
{
    public List<BiomeData> Biomes { get; set; }
}
```

## Data Validation

### Validation Overview

Data is validated on load to ensure integrity:

```csharp
public class DataValidator
{
    public static ValidationResult ValidateBlockData(List<BlockData> blocks)
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();

        foreach (var block in blocks)
        {
            // Check for duplicate IDs
            if (ids.Contains(block.Id))
            {
                issues.Add($"Duplicate block ID: {block.Id}");
            }
            ids.Add(block.Id);

            // Validate hardness
            if (block.Hardness < 0)
            {
                issues.Add($"Invalid hardness for block {block.Name}: {block.Hardness}");
            }

            // Validate resistance
            if (block.Resistance < 0)
            {
                issues.Add($"Invalid resistance for block {block.Name}: {block.Resistance}");
            }

            // Validate light level
            if (block.LightLevel < 0 || block.LightLevel > 15)
            {
                issues.Add($"Invalid light level for block {block.Name}: {block.LightLevel}");
            }
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }

    public static ValidationResult ValidateItemData(List<ItemData> items)
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();

        foreach (var item in items)
        {
            // Check for duplicate IDs
            if (ids.Contains(item.Id))
            {
                issues.Add($"Duplicate item ID: {item.Id}");
            }
            ids.Add(item.Id);

            // Validate max stack size
            if (item.MaxStackSize < 1 || item.MaxStackSize > 64)
            {
                issues.Add($"Invalid max stack size for item {item.Name}: {item.MaxStackSize}");
            }

            // Validate max durability
            if (item.MaxDurability < 0)
            {
                issues.Add($"Invalid max durability for item {item.Name}: {item.MaxDurability}");
            }
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; }
}
```

## Hot-reload Support

### Hot-reload Implementation

Data can be hot-reloaded without restart:

```csharp
public class DataHotReload
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

    public void WatchData(string dataPath, Action<string> onReload)
    {
        var directory = Path.GetDirectoryName(dataPath);
        var fileName = Path.GetFileName(dataPath);

        var watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (sender, e) =>
        {
            // Debounce file change events
            Thread.Sleep(100);
            onReload(dataPath);
        };

        watcher.EnableRaisingEvents = true;
        _watchers[dataPath] = watcher;
    }
}
```

## Data-driven Best Practices

### File Organization

1. **Separate Data Files**: Separate data files for different data types
2. **Consistent Naming**: Use consistent naming conventions
3. **Validation**: Validate data on load
4. **Hot-reload**: Support hot-reload for non-critical data
5. **Versioning**: Support data versioning

### Data Structure

1. **JSON Format**: Use JSON for all game data
2. **Hierarchical**: Use hierarchical structure for complex data
3. **Type Safety**: Use strongly-typed models
4. **Extensibility**: Easy to add new data fields
5. **Documentation**: Document data structure

### Performance

1. **Caching**: Cache loaded data
2. **Lazy Loading**: Load data on demand
3. **Indexing**: Use dictionaries for fast lookups
4. **Batch Loading**: Load data in batches
5. **Memory Management**: Manage memory efficiently

## References

- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)
- [`config/biomes.json`](../config/biomes.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

