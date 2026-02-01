# Data-Driven JSON Structures - Session S32

**Date:** 2026-01-31  
**Status:** IMPLEMENTATION IN PROGRESS

## Overview

This document outlines the data-driven JSON structures used throughout the Minecraft clone project. All game data is stored in JSON format for easy modification and maintenance.

## Configuration Files Structure

### Server Configuration (`config/server_config.json`)

```json
{
  "server": {
    "Network": { ... },
    "Database": { ... },
    "World": { ... },
    "Gameplay": { ... },
    "Security": { ... },
    "Performance": { ... }
  }
}
```

**Sections:**
- **Network**: Port, bind address, connection settings
- **Database**: SQLite database configuration
- **World**: World generation settings, terrain options
- **Gameplay**: Game rules, player limits
- **Security**: Authentication, rate limiting
- **Performance**: Maintenance intervals, optimization settings

### Client Configuration (`config/client_config.json`)

```json
{
  "client": {
    "network": { ... },
    "graphics": { ... },
    "audio": { ... },
    "controls": { ... },
    "ui": { ... },
    "gameplay": { ... },
    "world": { ... },
    "performance": { ... },
    "debug": { ... }
  },
  "server": { ... },
  "compatibility": { ... }
}
```

**Sections:**
- **network**: Connection settings, timeouts, compression
- **graphics**: Rendering options, quality settings
- **audio**: Volume controls, audio device
- **controls**: Key bindings, mouse sensitivity
- **ui**: HUD settings, interface options
- **gameplay**: Game rules, difficulty settings
- **world**: World generation options
- **performance**: Chunk loading, memory limits
- **debug**: Debug rendering, logging options
- **server**: Default server connection settings
- **compatibility**: Protocol version compatibility

## Game Data Files

### Block Data (`config/blocks.json`)

**Structure:**
```json
{
  "BlockTypes": {
    "BlockName": {
      "Id": 0,
      "Name": "BlockName",
      "DisplayName": "Display Name",
      "Solid": true,
      "Transparent": false,
      "LightPassing": false,
      "CanBreak": true,
      "CanPlace": true,
      "HasGravity": false,
      "LightLevel": 0,
      "Resistance": 6.0,
      "Hardness": 1.5,
      "Texture": {
        "Top": "texture_top",
        "Bottom": "texture_bottom",
        "Sides": "texture_side"
      },
      "Drops": [
        {
          "Item": "DropItem",
          "Count": 1,
          "Chance": 1.0
        }
      ],
      "Tool": "ToolType",
      "Sounds": {
        "Break": "break_sound",
        "Place": "place_sound",
        "Step": "step_sound"
      }
    }
  },
  "ToolTypes": {
    "ToolName": {
      "Name": "ToolName",
      "Efficiency": 1.0,
      "CanHarvest": ["Block1", "Block2"]
    }
  }
}
```

**Block Properties:**
- `Id`: Unique numeric identifier
- `Name`: Internal block name
- `DisplayName`: User-facing name
- `Solid`: Whether blocks movement
- `Transparent`: Whether light passes through
- `LightPassing`: Whether light propagates
- `CanBreak`: Whether players can break it
- `CanPlace`: Whether players can place it
- `HasGravity`: Whether it falls
- `LightLevel`: Emitted light level (0-15)
- `Resistance`: Explosion resistance
- `Hardness`: Mining hardness
- `Texture`: Texture references
- `Drops`: Drop table with items and probabilities
- `Tool`: Required tool type
- `Sounds`: Sound effect references

### Item Data (`config/items.json`)

**Structure:**
```json
{
  "items": {
    "blocks": { ... },
    "tools": { ... },
    "weapons": { ... },
    "armor": { ... },
    "food": { ... },
    "materials": { ... }
  },
  "version": "1.0.0",
  "lastUpdated": "2026-01-31T12:00:00Z"
}
```

**Item Categories:**

#### Blocks
```json
{
  "block_name": {
    "id": 0,
    "name": "BlockName",
    "displayName": "Display Name",
    "hardness": 1.5,
    "resistance": 6.0,
    "lightLevel": 0,
    "toolType": "pickaxe",
    "stackSize": 64,
    "categories": ["blocks", "stone", "building"]
  }
}
```

#### Tools
```json
{
  "tool_name": {
    "id": 270,
    "name": "Tool Name",
    "displayName": "Display Name",
    "type": "tool",
    "tier": "wood|stone|iron|diamond",
    "durability": 59,
    "efficiency": 2.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "damage": 2.0,
    "attackSpeed": 1.2,
    "categories": ["tools", "wood", "basic"]
  }
}
```

#### Weapons
```json
{
  "weapon_name": {
    "id": 268,
    "name": "Weapon Name",
    "displayName": "Display Name",
    "type": "weapon",
    "tier": "wood|stone|iron|diamond",
    "durability": 59,
    "efficiency": 2.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "damage": 2.0,
    "attackSpeed": 1.6,
    "categories": ["weapons", "wood", "basic"]
  }
}
```

#### Armor
```json
{
  "armor_name": {
    "id": 298,
    "name": "Armor Name",
    "displayName": "Display Name",
    "type": "armor",
    "tier": "leather|iron|diamond",
    "durability": 55,
    "protection": 1.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "categories": ["armor", "leather", "basic"]
  }
}
```

#### Food
```json
{
  "food_name": {
    "id": 260,
    "name": "Food Name",
    "displayName": "Display Name",
    "type": "food",
    "nutrition": 4,
    "saturation": 2.4,
    "stackSize": 64,
    "categories": ["food", "fruit", "nature"]
  }
}
```

#### Materials
```json
{
  "material_name": {
    "id": 263,
    "name": "Material Name",
    "displayName": "Display Name",
    "type": "material",
    "stackSize": 64,
    "categories": ["materials", "fuel", "basic"]
  }
}
```

## Terrain Generation Configuration

### Enhanced Terrain Config (`config/enhanced_terrain_generation.json`)

```json
{
  "terrain": {
    "worldHeight": 256,
    "worldDepth": 64,
    "seaLevel": 62,
    "chunkSize": 16,
    "baseHeight": 64,
    "heightVariation": 32,
    "heightScale": 0.01,
    "roughnessScale": 0.05,
    "detailScale": 0.1
  },
  "caves": {
    "enabled": true,
    "caveRarity": 7.0,
    "caveFrequency": 1.0,
    "caveMinAltitude": 8,
    "caveMaxAltitude": 56,
    "individualCaveRarity": 25.0,
    "caveSystemFrequency": 0.5,
    "caveSystemPocketChance": 0.5,
    "caveSystemPocketMinSize": 1,
    "caveSystemPocketMaxSize": 4
  },
  "rivers": {
    "enabled": true,
    "riverWidth": 5,
    "riverDepth": 3,
    "riverFrequency": 0.001,
    "riverCurvature": 0.5
  },
  "lakes": {
    "enabled": true,
    "lakeFrequency": 0.0005,
    "lakeMinSize": 16,
    "lakeMaxSize": 64,
    "lakeDepth": 8
  },
  "ores": {
    "coal": {
      "minHeight": 5,
      "maxHeight": 128,
      "clusterSize": 17,
      "frequency": 20
    },
    "iron": {
      "minHeight": 5,
      "maxHeight": 64,
      "clusterSize": 9,
      "frequency": 20
    },
    "gold": {
      "minHeight": 5,
      "maxHeight": 32,
      "clusterSize": 9,
      "frequency": 2
    },
    "diamond": {
      "minHeight": 5,
      "maxHeight": 16,
      "clusterSize": 8,
      "frequency": 1
    }
  }
}
```

## World Map Control Configuration

### Server World Map Control (`config/enhanced_world_map_control_server.json`)

```json
{
  "server": {
    "worldMapControl": {
      "enabled": true,
      "profileManagement": {
        "enabled": true,
        "maxProfilesPerPlayer": 5,
        "defaultProfileName": "default"
      },
      "generationSettings": {
        "seed": 0,
        "worldType": "normal",
        "generateStructures": true,
        "customBiomes": []
      },
      "terrainFeatures": {
        "caves": true,
        "ravines": true,
        "mineshafts": true,
        "strongholds": true,
        "villages": true,
        "temples": true
      },
      "worldBorder": {
        "enabled": false,
        "centerX": 0,
        "centerZ": 0,
        "size": 29999984,
        "warningDistance": 1000,
        "damagePerBlock": 0.2
      },
      "timeSettings": {
        "dayLength": 1200,
        "startTime": 0
      },
      "weatherSettings": {
        "enabled": true,
        "rainChance": 0.1,
        "thunderChance": 0.01,
        "rainDuration": 6000,
        "thunderDuration": 3000
      }
    }
  }
}
```

### Client World Map Control (`config/enhanced_world_map_control_client.json`)

```json
{
  "client": {
    "worldMapControl": {
      "enabled": true,
      "profileManagement": {
        "enabled": true,
        "autoSelectDefault": true
      },
      "rendering": {
        "chunkRenderDistance": 10,
        "maxLoadedChunks": 500,
        "chunkUpdateInterval": 0.1
      },
      "ui": {
        "showCoordinates": true,
        "showBiomeInfo": true,
        "showTime": true,
        "showWeather": true
      }
    }
  }
}
```

## Data Validation

### Schema Validation

All JSON files should be validated against their respective schemas. The following validation rules apply:

1. **Required Fields**: All required fields must be present
2. **Type Checking**: Values must match expected types
3. **Range Checking**: Numeric values must be within valid ranges
4. **Reference Integrity**: References to other items must be valid
5. **Uniqueness**: IDs and names must be unique

### Validation Scripts

Create validation scripts in `scripts/validate_data.py`:

```python
import json
import sys

def validate_json_file(filepath, schema):
    """Validate a JSON file against a schema."""
    with open(filepath, 'r') as f:
        data = json.load(f)
    # Validation logic here
    return True
```

## Data Loading

### Server-Side Data Loading

```csharp
public class DataManager
{
    private Dictionary<int, BlockData> blocks = new();
    private Dictionary<int, ItemData> items = new();
    
    public void LoadData()
    {
        LoadBlocks("config/blocks.json");
        LoadItems("config/items.json");
        LoadConfig("config/server_config.json");
    }
    
    private void LoadBlocks(string path)
    {
        string json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<BlockDataCollection>(json);
        foreach (var block in data.BlockTypes.Values)
        {
            blocks[block.Id] = block;
        }
    }
}
```

### Client-Side Data Loading

```csharp
public class ClientDataManager : MonoBehaviour
{
    private Dictionary<int, BlockData> blocks = new();
    
    public void LoadData()
    {
        StartCoroutine(LoadBlocksAsync());
    }
    
    private IEnumerator LoadBlocksAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "blocks.json");
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            string json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<BlockDataCollection>(json);
            // Process data
        }
    }
}
```

## Data Migration

When updating data structures, provide migration scripts:

```csharp
public class DataMigrator
{
    public void MigrateFromV1ToV2(string filepath)
    {
        string json = File.ReadAllText(filepath);
        var v1Data = JsonSerializer.Deserialize<V1Data>(json);
        var v2Data = ConvertToV2(v1Data);
        string newJson = JsonSerializer.Serialize(v2Data, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(filepath, newJson);
    }
}
```

## Best Practices

1. **Versioning**: Always include version information in data files
2. **Timestamps**: Track last update time for caching purposes
3. **Comments**: Use JSON5 or external documentation for comments
4. **Indentation**: Use 2-space indentation for readability
5. **Naming**: Use camelCase for property names
6. **Types**: Be explicit about numeric types (int vs float)
7. **Defaults**: Provide sensible default values
8. **Extensibility**: Design for future additions without breaking changes

## Current Status

### Completed
- ✅ Server configuration structure defined
- ✅ Client configuration structure defined
- ✅ Block data structure defined
- ✅ Item data structure defined
- ✅ Terrain generation configuration defined

### In Progress
- 🔄 Fixing duplicate content in blocks.json
- 🔄 Fixing duplicate content in items.json
- 🔄 Creating data validation scripts

### Pending
- ⏳ Create data loading utilities
- ⏳ Create data migration scripts
- ⏳ Add JSON schema definitions
- ⏳ Create data editor tools

## References

- [JSON Specification](https://www.json.org/)
- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Unity JsonUtility](https://docs.unity3d.com/ScriptReference/JsonUtility.html)

**Date:** 2026-01-31  
**Status:** IMPLEMENTATION IN PROGRESS

## Overview

This document outlines the data-driven JSON structures used throughout the Minecraft clone project. All game data is stored in JSON format for easy modification and maintenance.

## Configuration Files Structure

### Server Configuration (`config/server_config.json`)

```json
{
  "server": {
    "Network": { ... },
    "Database": { ... },
    "World": { ... },
    "Gameplay": { ... },
    "Security": { ... },
    "Performance": { ... }
  }
}
```

**Sections:**
- **Network**: Port, bind address, connection settings
- **Database**: SQLite database configuration
- **World**: World generation settings, terrain options
- **Gameplay**: Game rules, player limits
- **Security**: Authentication, rate limiting
- **Performance**: Maintenance intervals, optimization settings

### Client Configuration (`config/client_config.json`)

```json
{
  "client": {
    "network": { ... },
    "graphics": { ... },
    "audio": { ... },
    "controls": { ... },
    "ui": { ... },
    "gameplay": { ... },
    "world": { ... },
    "performance": { ... },
    "debug": { ... }
  },
  "server": { ... },
  "compatibility": { ... }
}
```

**Sections:**
- **network**: Connection settings, timeouts, compression
- **graphics**: Rendering options, quality settings
- **audio**: Volume controls, audio device
- **controls**: Key bindings, mouse sensitivity
- **ui**: HUD settings, interface options
- **gameplay**: Game rules, difficulty settings
- **world**: World generation options
- **performance**: Chunk loading, memory limits
- **debug**: Debug rendering, logging options
- **server**: Default server connection settings
- **compatibility**: Protocol version compatibility

## Game Data Files

### Block Data (`config/blocks.json`)

**Structure:**
```json
{
  "BlockTypes": {
    "BlockName": {
      "Id": 0,
      "Name": "BlockName",
      "DisplayName": "Display Name",
      "Solid": true,
      "Transparent": false,
      "LightPassing": false,
      "CanBreak": true,
      "CanPlace": true,
      "HasGravity": false,
      "LightLevel": 0,
      "Resistance": 6.0,
      "Hardness": 1.5,
      "Texture": {
        "Top": "texture_top",
        "Bottom": "texture_bottom",
        "Sides": "texture_side"
      },
      "Drops": [
        {
          "Item": "DropItem",
          "Count": 1,
          "Chance": 1.0
        }
      ],
      "Tool": "ToolType",
      "Sounds": {
        "Break": "break_sound",
        "Place": "place_sound",
        "Step": "step_sound"
      }
    }
  },
  "ToolTypes": {
    "ToolName": {
      "Name": "ToolName",
      "Efficiency": 1.0,
      "CanHarvest": ["Block1", "Block2"]
    }
  }
}
```

**Block Properties:**
- `Id`: Unique numeric identifier
- `Name`: Internal block name
- `DisplayName`: User-facing name
- `Solid`: Whether blocks movement
- `Transparent`: Whether light passes through
- `LightPassing`: Whether light propagates
- `CanBreak`: Whether players can break it
- `CanPlace`: Whether players can place it
- `HasGravity`: Whether it falls
- `LightLevel`: Emitted light level (0-15)
- `Resistance`: Explosion resistance
- `Hardness`: Mining hardness
- `Texture`: Texture references
- `Drops`: Drop table with items and probabilities
- `Tool`: Required tool type
- `Sounds`: Sound effect references

### Item Data (`config/items.json`)

**Structure:**
```json
{
  "items": {
    "blocks": { ... },
    "tools": { ... },
    "weapons": { ... },
    "armor": { ... },
    "food": { ... },
    "materials": { ... }
  },
  "version": "1.0.0",
  "lastUpdated": "2026-01-31T12:00:00Z"
}
```

**Item Categories:**

#### Blocks
```json
{
  "block_name": {
    "id": 0,
    "name": "BlockName",
    "displayName": "Display Name",
    "hardness": 1.5,
    "resistance": 6.0,
    "lightLevel": 0,
    "toolType": "pickaxe",
    "stackSize": 64,
    "categories": ["blocks", "stone", "building"]
  }
}
```

#### Tools
```json
{
  "tool_name": {
    "id": 270,
    "name": "Tool Name",
    "displayName": "Display Name",
    "type": "tool",
    "tier": "wood|stone|iron|diamond",
    "durability": 59,
    "efficiency": 2.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "damage": 2.0,
    "attackSpeed": 1.2,
    "categories": ["tools", "wood", "basic"]
  }
}
```

#### Weapons
```json
{
  "weapon_name": {
    "id": 268,
    "name": "Weapon Name",
    "displayName": "Display Name",
    "type": "weapon",
    "tier": "wood|stone|iron|diamond",
    "durability": 59,
    "efficiency": 2.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "damage": 2.0,
    "attackSpeed": 1.6,
    "categories": ["weapons", "wood", "basic"]
  }
}
```

#### Armor
```json
{
  "armor_name": {
    "id": 298,
    "name": "Armor Name",
    "displayName": "Display Name",
    "type": "armor",
    "tier": "leather|iron|diamond",
    "durability": 55,
    "protection": 1.0,
    "enchantability": 15,
    "repairMaterial": "material_name",
    "categories": ["armor", "leather", "basic"]
  }
}
```

#### Food
```json
{
  "food_name": {
    "id": 260,
    "name": "Food Name",
    "displayName": "Display Name",
    "type": "food",
    "nutrition": 4,
    "saturation": 2.4,
    "stackSize": 64,
    "categories": ["food", "fruit", "nature"]
  }
}
```

#### Materials
```json
{
  "material_name": {
    "id": 263,
    "name": "Material Name",
    "displayName": "Display Name",
    "type": "material",
    "stackSize": 64,
    "categories": ["materials", "fuel", "basic"]
  }
}
```

## Terrain Generation Configuration

### Enhanced Terrain Config (`config/enhanced_terrain_generation.json`)

```json
{
  "terrain": {
    "worldHeight": 256,
    "worldDepth": 64,
    "seaLevel": 62,
    "chunkSize": 16,
    "baseHeight": 64,
    "heightVariation": 32,
    "heightScale": 0.01,
    "roughnessScale": 0.05,
    "detailScale": 0.1
  },
  "caves": {
    "enabled": true,
    "caveRarity": 7.0,
    "caveFrequency": 1.0,
    "caveMinAltitude": 8,
    "caveMaxAltitude": 56,
    "individualCaveRarity": 25.0,
    "caveSystemFrequency": 0.5,
    "caveSystemPocketChance": 0.5,
    "caveSystemPocketMinSize": 1,
    "caveSystemPocketMaxSize": 4
  },
  "rivers": {
    "enabled": true,
    "riverWidth": 5,
    "riverDepth": 3,
    "riverFrequency": 0.001,
    "riverCurvature": 0.5
  },
  "lakes": {
    "enabled": true,
    "lakeFrequency": 0.0005,
    "lakeMinSize": 16,
    "lakeMaxSize": 64,
    "lakeDepth": 8
  },
  "ores": {
    "coal": {
      "minHeight": 5,
      "maxHeight": 128,
      "clusterSize": 17,
      "frequency": 20
    },
    "iron": {
      "minHeight": 5,
      "maxHeight": 64,
      "clusterSize": 9,
      "frequency": 20
    },
    "gold": {
      "minHeight": 5,
      "maxHeight": 32,
      "clusterSize": 9,
      "frequency": 2
    },
    "diamond": {
      "minHeight": 5,
      "maxHeight": 16,
      "clusterSize": 8,
      "frequency": 1
    }
  }
}
```

## World Map Control Configuration

### Server World Map Control (`config/enhanced_world_map_control_server.json`)

```json
{
  "server": {
    "worldMapControl": {
      "enabled": true,
      "profileManagement": {
        "enabled": true,
        "maxProfilesPerPlayer": 5,
        "defaultProfileName": "default"
      },
      "generationSettings": {
        "seed": 0,
        "worldType": "normal",
        "generateStructures": true,
        "customBiomes": []
      },
      "terrainFeatures": {
        "caves": true,
        "ravines": true,
        "mineshafts": true,
        "strongholds": true,
        "villages": true,
        "temples": true
      },
      "worldBorder": {
        "enabled": false,
        "centerX": 0,
        "centerZ": 0,
        "size": 29999984,
        "warningDistance": 1000,
        "damagePerBlock": 0.2
      },
      "timeSettings": {
        "dayLength": 1200,
        "startTime": 0
      },
      "weatherSettings": {
        "enabled": true,
        "rainChance": 0.1,
        "thunderChance": 0.01,
        "rainDuration": 6000,
        "thunderDuration": 3000
      }
    }
  }
}
```

### Client World Map Control (`config/enhanced_world_map_control_client.json`)

```json
{
  "client": {
    "worldMapControl": {
      "enabled": true,
      "profileManagement": {
        "enabled": true,
        "autoSelectDefault": true
      },
      "rendering": {
        "chunkRenderDistance": 10,
        "maxLoadedChunks": 500,
        "chunkUpdateInterval": 0.1
      },
      "ui": {
        "showCoordinates": true,
        "showBiomeInfo": true,
        "showTime": true,
        "showWeather": true
      }
    }
  }
}
```

## Data Validation

### Schema Validation

All JSON files should be validated against their respective schemas. The following validation rules apply:

1. **Required Fields**: All required fields must be present
2. **Type Checking**: Values must match expected types
3. **Range Checking**: Numeric values must be within valid ranges
4. **Reference Integrity**: References to other items must be valid
5. **Uniqueness**: IDs and names must be unique

### Validation Scripts

Create validation scripts in `scripts/validate_data.py`:

```python
import json
import sys

def validate_json_file(filepath, schema):
    """Validate a JSON file against a schema."""
    with open(filepath, 'r') as f:
        data = json.load(f)
    # Validation logic here
    return True
```

## Data Loading

### Server-Side Data Loading

```csharp
public class DataManager
{
    private Dictionary<int, BlockData> blocks = new();
    private Dictionary<int, ItemData> items = new();
    
    public void LoadData()
    {
        LoadBlocks("config/blocks.json");
        LoadItems("config/items.json");
        LoadConfig("config/server_config.json");
    }
    
    private void LoadBlocks(string path)
    {
        string json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<BlockDataCollection>(json);
        foreach (var block in data.BlockTypes.Values)
        {
            blocks[block.Id] = block;
        }
    }
}
```

### Client-Side Data Loading

```csharp
public class ClientDataManager : MonoBehaviour
{
    private Dictionary<int, BlockData> blocks = new();
    
    public void LoadData()
    {
        StartCoroutine(LoadBlocksAsync());
    }
    
    private IEnumerator LoadBlocksAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "blocks.json");
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            string json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<BlockDataCollection>(json);
            // Process data
        }
    }
}
```

## Data Migration

When updating data structures, provide migration scripts:

```csharp
public class DataMigrator
{
    public void MigrateFromV1ToV2(string filepath)
    {
        string json = File.ReadAllText(filepath);
        var v1Data = JsonSerializer.Deserialize<V1Data>(json);
        var v2Data = ConvertToV2(v1Data);
        string newJson = JsonSerializer.Serialize(v2Data, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(filepath, newJson);
    }
}
```

## Best Practices

1. **Versioning**: Always include version information in data files
2. **Timestamps**: Track last update time for caching purposes
3. **Comments**: Use JSON5 or external documentation for comments
4. **Indentation**: Use 2-space indentation for readability
5. **Naming**: Use camelCase for property names
6. **Types**: Be explicit about numeric types (int vs float)
7. **Defaults**: Provide sensible default values
8. **Extensibility**: Design for future additions without breaking changes

## Current Status

### Completed
- ✅ Server configuration structure defined
- ✅ Client configuration structure defined
- ✅ Block data structure defined
- ✅ Item data structure defined
- ✅ Terrain generation configuration defined

### In Progress
- 🔄 Fixing duplicate content in blocks.json
- 🔄 Fixing duplicate content in items.json
- 🔄 Creating data validation scripts

### Pending
- ⏳ Create data loading utilities
- ⏳ Create data migration scripts
- ⏳ Add JSON schema definitions
- ⏳ Create data editor tools

## References

- [JSON Specification](https://www.json.org/)
- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Unity JsonUtility](https://docs.unity3d.com/ScriptReference/JsonUtility.html)

