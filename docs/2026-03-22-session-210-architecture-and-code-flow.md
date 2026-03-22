# Session 210 Architecture and Code Flow

## Session Date
- 2026-03-22 (KST)

## Summary
Fixed corrupted JSON data files and verified all build infrastructure. The project maintains data-driven architecture with JSON-based game data and template-based data extraction tools.

## Architecture Overview

### Data-Driven Game Data Pipeline
```
design/templates/game-data-template.md
         │
         ▼ (GameDataTemplateExporter tool)
         │
    ┌────┴────┐
    ▼         ▼
items.json  recipes.json
monsters.json  npcs.json
character_stats.json
```

### Build Infrastructure
```
┌─────────────────────────────────────────────────────────────┐
│                    .NET Projects                            │
├─────────────────────────────────────────────────────────────┤
│ SharedProtocol (net6.0)                                     │
│   └── Protocol buffers, message types, session management   │
├─────────────────────────────────────────────────────────────┤
│ GameServer (net6.0)                                         │
│   └── Handlers, World management, Player systems            │
├─────────────────────────────────────────────────────────────┤
│ GameCommon (netstandard2.1)                                 │
│   └── Shared game logic and models                          │
├─────────────────────────────────────────────────────────────┤
│ Tools/GameDataTemplateExporter (net8.0)                     │
│   └── MD template → JSON extraction                         │
├─────────────────────────────────────────────────────────────┤
│ Tools/DummyMinecraftClient (net8.0)                         │
│   └── Test client for server validation                     │
└─────────────────────────────────────────────────────────────┘
```

### Unity CI Pipeline
```
scripts/unity_compile_test.bat
         │
         ▼
UnityCiCommandlet.cs
         │
    ┌────┼────┐
    ▼    ▼    ▼
compile edit play
 mode mode mode
```

## Code Flow

### Game Data Loading
1. Server startup reads JSON files from `GameServer/config/game-data/`
2. GameDataCatalog validates and indexes items, recipes, monsters, NPCs
3. Data is synchronized to clients via protocol buffers

### Template Extraction
1. Edit `design/templates/game-data-template.md`
2. Run: `dotnet run --project Tools/GameDataTemplateExporter -- --input design/templates/game-data-template.md --output config/game-data/`
3. JSON files are generated with normalized formatting

## Key Files Modified

| File | Change |
|------|--------|
| `Assets/MyAssets/Scripts/DataFiles/items.json` | Fixed duplicated content |
| `Assets/MyAssets/Scripts/DataFiles/crafting_recipes.json` | Fixed duplicated content |

## Minetest Reference Patterns Applied

From `minetest_project/builtin/game/`:
- **item.lua**: Item registration pattern → JSON items.json
- **craftdef.cpp**: Recipe system → JSON recipes.json
- **object_properties.cpp**: Entity stats → JSON monsters.json, npcs.json
