# Config and Data-Driven Approach - Session 116

## Overview

This document describes the configuration and data-driven approach used in the Minecraft-like server implementation, including all JSON configuration files and their usage.

## Configuration Files

### Enhanced Terrain Generation Configuration

**File**: [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)

**Purpose**: Configuration for enhanced terrain generation algorithms (caves, rivers, lakes)

**Key Sections**:

1. **Water Configuration**
   - Global water level
   - River center and bank thresholds
   - River noise scale and depth
   - Hydrology parameters
   - Flow shadow parameters
   - River-specific parameters

2. **Caves Configuration**
   - Enable/disable caves
   - Cave threshold and frequencies
   - Support density and pillar chance
   - Hydrology and flow stability weights
   - River suppression
   - Moisture retention
   - Edge sealing
   - Ceiling stability and moisture
   - Flooded cave parameters
   - Water and lava thresholds

3. **Lakes Configuration**
   - Min/max depth and radius
   - Shelf depth
   - Basin smooth iterations
   - Spawn weight bias
   - Shoreline blend
   - River proximity suppression
   - Wetland saturation
   - Outflow parameters
   - Flow seepage
   - Variance and rim erosion
   - Inflow blend

4. **Coordination Configuration**
   - Cave-river interaction
   - Cave-lake interaction
   - River-lake interaction

### Enhanced World Map Control - Server Configuration

**File**: [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)

**Purpose**: Server-side world map control configuration

**Key Sections**:

1. **World Map Control**
   - Enable/disable world map control
   - Profile path and version
   - Default settings (render distance, map scale, coordinates, biome info, quality settings)

2. **Cache Configuration**
   - Max cached chunks
   - Max queued chunk requests
   - Queue pressure factor
   - Queue slack ratio
   - Queue burst slack multiplier
   - Queue load shedding threshold
   - Queue emergency brake threshold
   - Queue load EMA blend
   - Queue emergency release ratio
   - Queue trend boost weight
   - Queue shock absorber weight
   - Queue overload drain factor
   - Queue backoff delay
   - Queue emergency hold ticks
   - Queue recovery ramp ticks
   - Cleanup interval
   - Enable chunk cache
   - Inflight chunk timeout
   - Inflight prune interval
   - Queue hotspot bias
   - Queue hotspot emergency penalty

3. **Real-Time Updates**
   - Enable/disable real-time updates
   - Update interval
   - Broadcast to chunk only

4. **Terrain Generation**
   - Chunk size
   - Seed
   - Max concurrent chunk generations
   - Update batch size
   - Update interval
   - Max queued chunk requests
   - Queue parameters (same as cache section)
   - Inflight chunk timeout
   - Inflight prune interval
   - Queue hotspot bias
   - Queue hotspot emergency penalty

### Enhanced World Map Control - Client Configuration

**File**: [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)

**Purpose**: Client-side world map control configuration

**Key Sections**:

1. **UI Configuration**
   - Show mini map
   - Mini map position
   - Mini map size
   - Mini map opacity
   - Show player marker
   - Show chunk borders

2. **Display Configuration**
   - Show coordinates
   - Show biome info
   - Show FPS
   - Show ping

3. **Performance Configuration**
   - Chunk update throttle
   - Max concurrent chunk requests
   - Max queued chunk requests
   - Queue pressure factor
   - Queue slack ratio
   - Queue burst slack multiplier
   - Queue load shedding threshold
   - Queue emergency brake threshold
   - Queue load EMA blend
   - Queue emergency release ratio
   - Queue trend boost weight
   - Queue shock absorber weight
   - Queue overload drain factor
   - Queue backoff delay
   - Enable chunk prediction
   - Queue request TTL
   - Queue emergency hold ticks
   - Queue recovery ramp ticks
   - Queue hotspot bias
   - Queue hotspot emergency penalty

4. **Defaults Configuration**
   - Render distance
   - Max loaded preview chunks
   - Map scale
   - Quality settings (terrain, water, vegetation)
   - Fog and shadow enabled
   - Max chunk updates per frame
   - Max queued chunk updates
   - Chunk LOD
   - Unload distance

## Data-Driven Approach

### Block Data

**File**: [`config/blocks.json`](../config/blocks.json)

**Purpose**: Defines all block types and their properties

**Block Properties**:

- `Type`: Block ID
- `Name`: Internal block name
- `DisplayName`: Human-readable name
- `Hardness`: Block hardness (mining resistance)
- `Resistance`: Explosion resistance
- `IsTransparent`: Whether block is transparent
- `IsFluid`: Whether block is a fluid
- `AffectedByGravity`: Whether block is affected by gravity
- `RequiredTool`: Tool required to mine (pickaxe, shovel, axe, etc.)
- `RequiredToolLevel`: Minimum tool level required
- `LightLevel`: Light level emitted by block
- `Drops`: Items dropped when block is broken
- `ConductsRedstone`: Whether block conducts redstone
- `IsPowerSource`: Whether block is a redstone power source

**Example Block Definition**:

```json
{
  "Type": 1,
  "Name": "stone",
  "DisplayName": "Stone",
  "Hardness": 1.5,
  "Resistance": 6.0,
  "IsTransparent": false,
  "IsFluid": false,
  "AffectedByGravity": false,
  "RequiredTool": "pickaxe",
  "RequiredToolLevel": 0,
  "LightLevel": 0,
  "Drops": [
    {
      "ItemId": "cobblestone",
      "Chance": 1.0,
      "MinCount": 1,
      "MaxCount": 1
    }
  ]
}
```

### Item Data

**File**: [`config/items.json`](../config/items.json)

**Purpose**: Defines all item types and their properties

**Item Properties**:

- `itemId`: Unique item identifier
- `displayName`: Human-readable name
- `description`: Item description
- `categoryId`: Item category (food, drink, weapon, tool, armor, block, material)
- `rarity`: Item rarity (common, uncommon, rare, epic, legendary)
- `maxStackSize`: Maximum stack size
- `nutrition`: Nutrition value (for food)
- `hydration`: Hydration value (for drinks)
- `toolType`: Tool type (hand, sword, pickaxe, shovel, axe, etc.)
- `toolStrength`: Tool strength
- `durability`: Current durability
- `maxDurability`: Maximum durability
- `repairItem`: Item used for repair
- `value`: Item value
- `weight`: Item weight
- `canEnchant`: Whether item can be enchanted
- `enchantableTypes`: List of enchantment types
- `customProperties`: Custom properties specific to item type

**Example Item Definition**:

```json
{
  "itemId": "diamond_pickaxe",
  "displayName": "Diamond Pickaxe",
  "description": "An exceptional pickaxe made from diamond.",
  "categoryId": "tool",
  "rarity": "rare",
  "maxStackSize": 1,
  "nutrition": 0.0,
  "hydration": 0.0,
  "toolType": "pickaxe",
  "toolStrength": 5.0,
  "durability": 1562,
  "maxDurability": 1562,
  "repairItem": "diamond",
  "value": 500,
  "weight": 2.5,
  "canEnchant": true,
  "enchantableTypes": [
    "efficiency",
    "fortune",
    "silk_touch",
    "unbreaking",
    "mending"
  ],
  "customProperties": {
    "mineSpeed": 8.0,
    "canMine": [
      "stone",
      "coal_ore",
      "iron_ore",
      "gold_ore",
      "diamond_ore",
      "obsidian"
    ]
  }
}
```

### Recipe Data

**File**: [`config/recipes.json`](../config/recipes.json)

**Purpose**: Defines all crafting recipes

**Recipe Properties**:

- `recipeId`: Unique recipe identifier
- `displayName`: Human-readable name
- `description`: Recipe description
- `category`: Recipe category (basic, tools, weapons, smelting, cooking, storage, decoration)
- `requiredLevel`: Minimum level required
- `experienceCost`: Experience cost to craft
- `ingredients`: List of required items
  - `itemId`: Item ID
  - `quantity`: Quantity required
  - `metadata`: Item metadata
- `results`: List of result items
  - `itemId`: Item ID
  - `quantity`: Quantity produced
  - `metadata`: Item metadata
- `craftingTime`: Time to craft (in seconds)
- `craftingStation`: Station required (hand, crafting_table, furnace, water_source)

**Example Recipe Definition**:

```json
{
  "recipeId": "diamond_pickaxe",
  "displayName": "Diamond Pickaxe",
  "description": "Craft a diamond pickaxe for elite mining.",
  "category": "tools",
  "requiredLevel": 10,
  "experienceCost": 50,
  "ingredients": [
    {
      "itemId": "diamond",
      "quantity": 3,
      "metadata": 0
    },
    {
      "itemId": "stick",
      "quantity": 2,
      "metadata": 0
    }
  ],
  "results": [
    {
      "itemId": "diamond_pickaxe",
      "quantity": 1,
      "metadata": 0
    }
  ],
  "craftingTime": 8.0,
  "craftingStation": "crafting_table"
}
```

## Configuration Loading

### Server Configuration Loading

The server loads configuration from JSON files:

1. **World Generation Config**: Loaded from `config/world.json` or specified path
2. **World Map Control Profile**: Loaded from `config/world_map_control_profile.json`
3. **Enhanced Terrain Generation**: Loaded from `config/enhanced_terrain_generation.json`
4. **Enhanced World Map Control**: Loaded from `config/enhanced_world_map_control_server.json`

### Client Configuration Loading

The client loads configuration from JSON files:

1. **Client Config**: Loaded from `config/client_config.json`
2. **Enhanced World Map Control**: Loaded from `config/enhanced_world_map_control_client.json`

### Data Loading

Game data is loaded from JSON files:

1. **Blocks**: Loaded from `config/blocks.json`
2. **Items**: Loaded from `config/items.json`
3. **Recipes**: Loaded from `config/recipes.json`
4. **Biomes**: Loaded from `config/biomes.json`
5. **Item Categories**: Loaded from `config/item_categories.json`

## Configuration Validation

### Profile Validation

The system validates profiles by:

1. **Hash Verification**: SHA-256 hash of profile content
2. **Version Checking**: Profile version compatibility
3. **Signature Verification**: Hydrology signature matching
4. **File Watch**: Monitors file changes for hot reload

### Configuration Reload

The system automatically reloads configuration when:

1. **Config File Updated**: File modification time changes
2. **Profile File Updated**: Profile file modification time changes
3. **Hash Mismatch**: Computed hash doesn't match stored hash
4. **Version Mismatch**: Profile version is outdated

## Best Practices

### Configuration Design

1. **JSON Format**: Use JSON for all configuration files
2. **Versioning**: Include version fields for backward compatibility
3. **Validation**: Validate configuration on load
4. **Defaults**: Provide sensible defaults for all settings
5. **Documentation**: Document all configuration options

### Data-Driven Design

1. **External Data**: Store game data in external JSON files
2. **Type Safety**: Use strongly-typed data structures
3. **Validation**: Validate data on load
4. **Extensibility**: Make data structures extensible
5. **Performance**: Cache loaded data for performance

## Configuration Management

### Environment Variables

The system supports environment variables for configuration:

1. **Server Config Path**: `MINECRAFT_SERVER_CONFIG`
2. **World Seed**: `MINECRAFT_WORLD_SEED`
3. **Port**: `MINECRAFT_SERVER_PORT`
4. **Log Level**: `MINECRAFT_LOG_LEVEL`

### Command Line Arguments

The system supports command line arguments:

1. **--config**: Path to configuration file
2. **--seed**: World generation seed
3. **--port**: Server port
4. **--server**: Run as server
5. **--selftest**: Run self-test

## Future Improvements

### Configuration Enhancements

1. **Configuration UI**: Add web-based configuration UI
2. **Hot Reload**: Implement hot reload for configuration changes
3. **Configuration Validation**: Add comprehensive validation
4. **Configuration Migration**: Support configuration version migration
5. **Configuration Templates**: Provide configuration templates

### Data Enhancements

1. **Data Validation**: Add comprehensive data validation
2. **Data Migration**: Support data version migration
3. **Data Caching**: Implement intelligent data caching
4. **Data Compression**: Compress large data files
5. **Data Streaming**: Stream large data files

## References

- [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)
- [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)
- [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)
- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Dummy client creation, shared DLL setup, commit and push

## Overview

This document describes the configuration and data-driven approach used in the Minecraft-like server implementation, including all JSON configuration files and their usage.

## Configuration Files

### Enhanced Terrain Generation Configuration

**File**: [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)

**Purpose**: Configuration for enhanced terrain generation algorithms (caves, rivers, lakes)

**Key Sections**:

1. **Water Configuration**
   - Global water level
   - River center and bank thresholds
   - River noise scale and depth
   - Hydrology parameters
   - Flow shadow parameters
   - River-specific parameters

2. **Caves Configuration**
   - Enable/disable caves
   - Cave threshold and frequencies
   - Support density and pillar chance
   - Hydrology and flow stability weights
   - River suppression
   - Moisture retention
   - Edge sealing
   - Ceiling stability and moisture
   - Flooded cave parameters
   - Water and lava thresholds

3. **Lakes Configuration**
   - Min/max depth and radius
   - Shelf depth
   - Basin smooth iterations
   - Spawn weight bias
   - Shoreline blend
   - River proximity suppression
   - Wetland saturation
   - Outflow parameters
   - Flow seepage
   - Variance and rim erosion
   - Inflow blend

4. **Coordination Configuration**
   - Cave-river interaction
   - Cave-lake interaction
   - River-lake interaction

### Enhanced World Map Control - Server Configuration

**File**: [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)

**Purpose**: Server-side world map control configuration

**Key Sections**:

1. **World Map Control**
   - Enable/disable world map control
   - Profile path and version
   - Default settings (render distance, map scale, coordinates, biome info, quality settings)

2. **Cache Configuration**
   - Max cached chunks
   - Max queued chunk requests
   - Queue pressure factor
   - Queue slack ratio
   - Queue burst slack multiplier
   - Queue load shedding threshold
   - Queue emergency brake threshold
   - Queue load EMA blend
   - Queue emergency release ratio
   - Queue trend boost weight
   - Queue shock absorber weight
   - Queue overload drain factor
   - Queue backoff delay
   - Queue emergency hold ticks
   - Queue recovery ramp ticks
   - Cleanup interval
   - Enable chunk cache
   - Inflight chunk timeout
   - Inflight prune interval
   - Queue hotspot bias
   - Queue hotspot emergency penalty

3. **Real-Time Updates**
   - Enable/disable real-time updates
   - Update interval
   - Broadcast to chunk only

4. **Terrain Generation**
   - Chunk size
   - Seed
   - Max concurrent chunk generations
   - Update batch size
   - Update interval
   - Max queued chunk requests
   - Queue parameters (same as cache section)
   - Inflight chunk timeout
   - Inflight prune interval
   - Queue hotspot bias
   - Queue hotspot emergency penalty

### Enhanced World Map Control - Client Configuration

**File**: [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)

**Purpose**: Client-side world map control configuration

**Key Sections**:

1. **UI Configuration**
   - Show mini map
   - Mini map position
   - Mini map size
   - Mini map opacity
   - Show player marker
   - Show chunk borders

2. **Display Configuration**
   - Show coordinates
   - Show biome info
   - Show FPS
   - Show ping

3. **Performance Configuration**
   - Chunk update throttle
   - Max concurrent chunk requests
   - Max queued chunk requests
   - Queue pressure factor
   - Queue slack ratio
   - Queue burst slack multiplier
   - Queue load shedding threshold
   - Queue emergency brake threshold
   - Queue load EMA blend
   - Queue emergency release ratio
   - Queue trend boost weight
   - Queue shock absorber weight
   - Queue overload drain factor
   - Queue backoff delay
   - Enable chunk prediction
   - Queue request TTL
   - Queue emergency hold ticks
   - Queue recovery ramp ticks
   - Queue hotspot bias
   - Queue hotspot emergency penalty

4. **Defaults Configuration**
   - Render distance
   - Max loaded preview chunks
   - Map scale
   - Quality settings (terrain, water, vegetation)
   - Fog and shadow enabled
   - Max chunk updates per frame
   - Max queued chunk updates
   - Chunk LOD
   - Unload distance

## Data-Driven Approach

### Block Data

**File**: [`config/blocks.json`](../config/blocks.json)

**Purpose**: Defines all block types and their properties

**Block Properties**:

- `Type`: Block ID
- `Name`: Internal block name
- `DisplayName`: Human-readable name
- `Hardness`: Block hardness (mining resistance)
- `Resistance`: Explosion resistance
- `IsTransparent`: Whether block is transparent
- `IsFluid`: Whether block is a fluid
- `AffectedByGravity`: Whether block is affected by gravity
- `RequiredTool`: Tool required to mine (pickaxe, shovel, axe, etc.)
- `RequiredToolLevel`: Minimum tool level required
- `LightLevel`: Light level emitted by block
- `Drops`: Items dropped when block is broken
- `ConductsRedstone`: Whether block conducts redstone
- `IsPowerSource`: Whether block is a redstone power source

**Example Block Definition**:

```json
{
  "Type": 1,
  "Name": "stone",
  "DisplayName": "Stone",
  "Hardness": 1.5,
  "Resistance": 6.0,
  "IsTransparent": false,
  "IsFluid": false,
  "AffectedByGravity": false,
  "RequiredTool": "pickaxe",
  "RequiredToolLevel": 0,
  "LightLevel": 0,
  "Drops": [
    {
      "ItemId": "cobblestone",
      "Chance": 1.0,
      "MinCount": 1,
      "MaxCount": 1
    }
  ]
}
```

### Item Data

**File**: [`config/items.json`](../config/items.json)

**Purpose**: Defines all item types and their properties

**Item Properties**:

- `itemId`: Unique item identifier
- `displayName`: Human-readable name
- `description`: Item description
- `categoryId`: Item category (food, drink, weapon, tool, armor, block, material)
- `rarity`: Item rarity (common, uncommon, rare, epic, legendary)
- `maxStackSize`: Maximum stack size
- `nutrition`: Nutrition value (for food)
- `hydration`: Hydration value (for drinks)
- `toolType`: Tool type (hand, sword, pickaxe, shovel, axe, etc.)
- `toolStrength`: Tool strength
- `durability`: Current durability
- `maxDurability`: Maximum durability
- `repairItem`: Item used for repair
- `value`: Item value
- `weight`: Item weight
- `canEnchant`: Whether item can be enchanted
- `enchantableTypes`: List of enchantment types
- `customProperties`: Custom properties specific to item type

**Example Item Definition**:

```json
{
  "itemId": "diamond_pickaxe",
  "displayName": "Diamond Pickaxe",
  "description": "An exceptional pickaxe made from diamond.",
  "categoryId": "tool",
  "rarity": "rare",
  "maxStackSize": 1,
  "nutrition": 0.0,
  "hydration": 0.0,
  "toolType": "pickaxe",
  "toolStrength": 5.0,
  "durability": 1562,
  "maxDurability": 1562,
  "repairItem": "diamond",
  "value": 500,
  "weight": 2.5,
  "canEnchant": true,
  "enchantableTypes": [
    "efficiency",
    "fortune",
    "silk_touch",
    "unbreaking",
    "mending"
  ],
  "customProperties": {
    "mineSpeed": 8.0,
    "canMine": [
      "stone",
      "coal_ore",
      "iron_ore",
      "gold_ore",
      "diamond_ore",
      "obsidian"
    ]
  }
}
```

### Recipe Data

**File**: [`config/recipes.json`](../config/recipes.json)

**Purpose**: Defines all crafting recipes

**Recipe Properties**:

- `recipeId`: Unique recipe identifier
- `displayName`: Human-readable name
- `description`: Recipe description
- `category`: Recipe category (basic, tools, weapons, smelting, cooking, storage, decoration)
- `requiredLevel`: Minimum level required
- `experienceCost`: Experience cost to craft
- `ingredients`: List of required items
  - `itemId`: Item ID
  - `quantity`: Quantity required
  - `metadata`: Item metadata
- `results`: List of result items
  - `itemId`: Item ID
  - `quantity`: Quantity produced
  - `metadata`: Item metadata
- `craftingTime`: Time to craft (in seconds)
- `craftingStation`: Station required (hand, crafting_table, furnace, water_source)

**Example Recipe Definition**:

```json
{
  "recipeId": "diamond_pickaxe",
  "displayName": "Diamond Pickaxe",
  "description": "Craft a diamond pickaxe for elite mining.",
  "category": "tools",
  "requiredLevel": 10,
  "experienceCost": 50,
  "ingredients": [
    {
      "itemId": "diamond",
      "quantity": 3,
      "metadata": 0
    },
    {
      "itemId": "stick",
      "quantity": 2,
      "metadata": 0
    }
  ],
  "results": [
    {
      "itemId": "diamond_pickaxe",
      "quantity": 1,
      "metadata": 0
    }
  ],
  "craftingTime": 8.0,
  "craftingStation": "crafting_table"
}
```

## Configuration Loading

### Server Configuration Loading

The server loads configuration from JSON files:

1. **World Generation Config**: Loaded from `config/world.json` or specified path
2. **World Map Control Profile**: Loaded from `config/world_map_control_profile.json`
3. **Enhanced Terrain Generation**: Loaded from `config/enhanced_terrain_generation.json`
4. **Enhanced World Map Control**: Loaded from `config/enhanced_world_map_control_server.json`

### Client Configuration Loading

The client loads configuration from JSON files:

1. **Client Config**: Loaded from `config/client_config.json`
2. **Enhanced World Map Control**: Loaded from `config/enhanced_world_map_control_client.json`

### Data Loading

Game data is loaded from JSON files:

1. **Blocks**: Loaded from `config/blocks.json`
2. **Items**: Loaded from `config/items.json`
3. **Recipes**: Loaded from `config/recipes.json`
4. **Biomes**: Loaded from `config/biomes.json`
5. **Item Categories**: Loaded from `config/item_categories.json`

## Configuration Validation

### Profile Validation

The system validates profiles by:

1. **Hash Verification**: SHA-256 hash of profile content
2. **Version Checking**: Profile version compatibility
3. **Signature Verification**: Hydrology signature matching
4. **File Watch**: Monitors file changes for hot reload

### Configuration Reload

The system automatically reloads configuration when:

1. **Config File Updated**: File modification time changes
2. **Profile File Updated**: Profile file modification time changes
3. **Hash Mismatch**: Computed hash doesn't match stored hash
4. **Version Mismatch**: Profile version is outdated

## Best Practices

### Configuration Design

1. **JSON Format**: Use JSON for all configuration files
2. **Versioning**: Include version fields for backward compatibility
3. **Validation**: Validate configuration on load
4. **Defaults**: Provide sensible defaults for all settings
5. **Documentation**: Document all configuration options

### Data-Driven Design

1. **External Data**: Store game data in external JSON files
2. **Type Safety**: Use strongly-typed data structures
3. **Validation**: Validate data on load
4. **Extensibility**: Make data structures extensible
5. **Performance**: Cache loaded data for performance

## Configuration Management

### Environment Variables

The system supports environment variables for configuration:

1. **Server Config Path**: `MINECRAFT_SERVER_CONFIG`
2. **World Seed**: `MINECRAFT_WORLD_SEED`
3. **Port**: `MINECRAFT_SERVER_PORT`
4. **Log Level**: `MINECRAFT_LOG_LEVEL`

### Command Line Arguments

The system supports command line arguments:

1. **--config**: Path to configuration file
2. **--seed**: World generation seed
3. **--port**: Server port
4. **--server**: Run as server
5. **--selftest**: Run self-test

## Future Improvements

### Configuration Enhancements

1. **Configuration UI**: Add web-based configuration UI
2. **Hot Reload**: Implement hot reload for configuration changes
3. **Configuration Validation**: Add comprehensive validation
4. **Configuration Migration**: Support configuration version migration
5. **Configuration Templates**: Provide configuration templates

### Data Enhancements

1. **Data Validation**: Add comprehensive data validation
2. **Data Migration**: Support data version migration
3. **Data Caching**: Implement intelligent data caching
4. **Data Compression**: Compress large data files
5. **Data Streaming**: Stream large data files

## References

- [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)
- [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)
- [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)
- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Dummy client creation, shared DLL setup, commit and push

