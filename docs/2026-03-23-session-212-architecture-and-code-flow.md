# Session 212 Architecture and Code Flow

## Overview
This document captures the current architecture and code flow for the Minecraft-like game project, following the worksheet.md requirements.

## Project Structure

### Unity Client (`Assets/MyAssets/Scripts/`)
```
Assets/MyAssets/Scripts/
├── GameWorld/         # World rendering, chunk management
├── Network/           # Client networking, protocol handling
├── UI/                # User interface components
├── DataFiles/         # JSON data files for game data
└── Editor/
    └── Automation/    # CI commandlet for compile/test
```

### .NET Server (`GameServer/`)
```
GameServer/
├── Program.cs         # Entry point, server initialization
├── Handlers/          # Request handlers (Login, Movement, Inventory, etc.)
├── Models/            # Data models (Item, Map, Player)
├── Systems/           # Game systems (GameDataCatalog)
├── World/             # World generation and management
│   └── Generation/    # Terrain, cave generators
├── SessionManager.cs  # Player session management
└── config/
    └── game-data/     # JSON game data files
```

### Shared Protocol (`SharedProtocol/`)
- Protocol Buffers message definitions
- Session and message handling
- Protocol registry for type mapping

### Tools (`Tools/`)
```
Tools/
├── GameDataTemplateExporter/  # MD template -> JSON converter (.NET 8.0)
└── DummyMinecraftClient/      # Test client (.NET 8.0)
```

## Data-Driven Architecture

### Game Data Flow
1. **Template Definition**: `design/templates/game-data-template.md`
   - Contains datasets in markdown format with JSON code blocks
   - Format: `## dataset: <name>` followed by ```json block

2. **Export Tool**: `Tools/GameDataTemplateExporter/`
   - .NET 8.0 console application
   - Parses markdown templates and extracts JSON datasets
   - Validates JSON syntax and normalizes output

3. **Output Locations**:
   - Server: `GameServer/config/game-data/`
   - Client: `Assets/StreamingAssets/game-data/`

### Game Data Files
| File | Purpose |
|------|---------|
| items.json | Item definitions |
| recipes.json | Crafting recipes |
| monsters.json | Monster definitions |
| npcs.json | NPC definitions |
| character_stats.json | Character stats configuration |

## Compile Test Infrastructure

### Unity CI Commandlet
- Location: `Assets/MyAssets/Scripts/Editor/Automation/UnityCiCommandlet.cs`
- Methods:
  - `RunCompileAndTests()` - Full compile + tests
  - `RunCompileOnly()` - Compile without tests
  - `RunEditModeTests()` - EditMode tests only
  - `RunPlayModeTests()` - PlayMode tests only

### Batch Script
- Location: `scripts/unity_compile_test.bat`
- Usage:
  ```batch
  scripts\unity_compile_test.bat --unity "C:\Path\To\Unity.exe" [--mode all|compile|edit|play] [--log "path\to\unity.log"]
  ```

### Output
- Test results: `reports/unity-tests/`
- Summary JSON files per test mode

## Minetest Reference Architecture

### Key Patterns from Minetest
1. **Registration System** (`builtin/game/register.lua`)
   - Items, nodes, craftitems registered via tables
   - `core.registered_items`, `core.registered_nodes`, etc.

2. **Mod Structure**
   - `init.lua` - Entry point
   - Data defined in Lua tables
   - Callback-based event handling

3. **Game Data Organization**
   - Games in `games/<game>/`
   - Mods in `mods/` or `games/<game>/mods/`
   - Configuration via `game.conf`

## Network Protocol

### Protocol Buffers
- Source: `proto/*.proto`
- Generated DTOs: `Assets/Generated/Protobuf/`
- Package: `EnhancedMinecraftProtocol`

### Message Types
- PlayerInfo, PlayerActionRequest/Response
- ChunkLoadRequest/Response, ChunkUnloadNotification/Ack
- BlockChangeBroadcast, EntitySpawn/Despawn
- CraftingRequest/Response, etc.

### Current Binding Coverage
- 14 of 54 declared messages bound
- Optional packets available for future expansion

## Configuration Files

| File | Purpose |
|------|---------|
| server-config.json | Main server configuration |
| config/world.json | World generation settings |
| config/world_map_control_*.json | Map control profiles |
| config/game-data/*.json | Game data (items, recipes, etc.) |
