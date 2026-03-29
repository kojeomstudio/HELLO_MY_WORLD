# Session 209 Architecture and Code Flow (2026-03-22)

## Overview
This session focused on verifying the current development infrastructure and ensuring compliance with worksheet.md requirements. All compile tests and selftest validations passed successfully.

## Current Architecture Status

### 1. Data-Driven Pipeline

```
design/templates/game-data-template.md
         |
         v (GameDataTemplateExporter)
         |
config/game-data/*.json  <--->  GameServer/config/game-data/
         |                         |
         v                         v
Assets/StreamingAssets/game-data/  (runtime load)
```

**Components:**
- `Tools/GameDataTemplateExporter/` (.NET 8.0): Parses markdown templates and exports JSON
- Input: Markdown with `## dataset: <name>` headings and JSON code blocks
- Output: Normalized JSON files in game-data directories

### 2. Server Architecture

```
GameServer/
├── Program.cs          (entry point, --selftest mode)
├── Handlers/           (request handlers)
├── World/              (chunk generation, world management)
├── Systems/            (game systems: crafting, inventory)
├── Models/             (data models)
└── config/             (JSON configuration files)
```

**Key Handlers:**
- LoginHandler
- MovementHandler
- WorldBlockHandler
- InventoryHandler
- CraftingHandler
- ChatHandler
- CommandHandler

### 3. Protocol Stack

```
Unity Client                GameServer
     |                          |
     v                          v
SharedProtocol (Protobuf) <--->
     |
     v
EnhancedMinecraftProtocol
     |
     v
ProtocolRegistry (binding layer)
```

**Binding Coverage:** 14/54 messages bound (optional messages not yet registered)

### 4. Unity Client Structure

```
Assets/MyAssets/Scripts/
├── GameWorld/          (world management, chunk handling)
├── Network/            (protocol communication)
├── UI/                 (user interface)
├── Player/             (player controllers)
└── Editor/             (editor tools, CI commandlet)
```

### 5. Configuration Sync

**Config Parity Manifest** (`config/config_parity_manifest.json`):
- Ensures consistency between server and client configs
- 12 tracked configuration files
- Automatic sync during build/deploy

## Code Flow Analysis

### Chunk Loading Flow
1. Client sends `ChunkLoadRequest` via protobuf
2. Server `MinecraftChunkHandler` processes request
3. `WorldManager` generates/loads chunk data
4. Server sends `ChunkLoadResponse` with chunk data
5. Client renders chunk meshes

### Crafting Flow
1. Client sends `CraftingRequest` with recipe ID
2. Server `CraftingHandler` validates recipe against `recipes.json`
3. `GameDataCatalog` looks up item/recipe data
4. Server validates ingredients in player inventory
5. Server sends `CraftingResponse` with results

### Data Validation Flow
1. Server startup: `GameDataCatalog.ValidateGameData()`
2. Load required datasets (items, recipes, monsters, npcs, character_stats)
3. Compute hash for each dataset
4. Verify mirror parity across directories
5. Log validation summary

## Minetest Reference Compliance

| Minetest Pattern | Our Implementation | Status |
|-----------------|-------------------|--------|
| Server authoritative | GameServer validation | OK |
| Chunk streaming | ChunkLoadRequest/Response | OK |
| Item registration | items.json + GameDataCatalog | OK |
| Recipe system | recipes.json + CraftingHandler | OK |
| Entity spawning | EntitySpawnBroadcast | OK |

## Warnings and Notes

### Protocol Warnings (Expected)
- Optional EnhancedMinecraft packets not bound (ContainerOpen, InventoryUpdate, etc.)
- These are reserved for future features and don't affect current functionality

### Compile Warnings (Non-blocking)
- CS8618: Non-nullable property warnings (design decision, not bugs)
- CS1998: Async methods without await (intentional for interface compatibility)

## Recommendations

1. **Protocol Expansion**: Gradually implement optional EnhancedMinecraft packets as features are developed
2. **Data Expansion**: Add more items/recipes to game-data-template.md as content grows
3. **Test Coverage**: Add Unity PlayMode tests for chunk loading and crafting flows
