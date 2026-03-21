# Session 207 Architecture and Code Flow

## Session Date
- 2026-03-21 (KST)

## Overview
This session focused on verifying the development infrastructure, reviewing minetest game data patterns, and ensuring the data-driven architecture is properly implemented.

## Development Infrastructure

### .NET Projects
```
C:\Workspace\HelloMyWorld_repo\
├── SharedProtocol/          # Protocol messages (net6.0)
├── GameServer/              # Game server (net6.0)
├── GameCommon/              # Shared game logic (netstandard2.1)
└── Tools/
    ├── GameDataTemplateExporter/  # MD → JSON converter (net8.0)
    └── DummyMinecraftClient/      # Test client (net8.0)
```

### Unity CI Commandlet
- Location: `Assets/MyAssets/Scripts/Editor/Automation/UnityCiCommandlet.cs`
- Entry Points:
  - `RunCompileAndTests()` - Full compile + test
  - `RunCompileOnly()` - Compile check only
  - `RunEditModeTests()` - EditMode tests
  - `RunPlayModeTests()` - PlayMode tests
- Batch Script: `scripts/unity_compile_test.bat`

## Data-Driven Architecture

### Game Data Pipeline
```
design/templates/game-data-template.md
         ↓ (GameDataTemplateExporter)
config/game-data/*.json
    ↓               ↓
Assets/StreamingAssets/   GameServer/config/
game-data/*.json          game-data/*.json
```

### JSON Data Categories
1. **items.json** - Item definitions
   - Fields: id, type, name, stack_max, durability, groups
   - Types: tool, material, resource, food

2. **recipes.json** - Crafting recipes
   - Methods: NORMAL, COOKING, FUEL
   - Properties: shaped, width, height, ingredients, results, replacements

3. **monsters.json** - Entity definitions
   - Fields: id, tier, health, attack, speed, drops

4. **npcs.json** - NPC definitions
   - Fields: id, role, shop_tier, dialogue_pool

5. **character_stats.json** - Player stats
   - Base stats and growth per level

## Minetest Reference Patterns

### Key Adaptations from minetest_project
1. **Item Groups**: `groups: ["tool", "pickaxe", "wood"]` → crafting ingredient matching
2. **Recipe Methods**: NORMAL (shaped/shapeless), COOKING, FUEL
3. **Replacements**: `{ "from": "milk_bucket", "to": "bucket" }` for container return

### Recommended Enhancements
1. Add `tool_capabilities` for mining speed calculation
2. Add `drop` tables with rarity for node drops
3. Add `node` type items with `tiles`, `drawtype` properties

## Code Flow Summary

### Server Startup
```
Program.cs → LoadConfig() → InitializeWorld() → StartTcpServer()
    → SessionManager.RegisterHandlers()
    → GameDataCatalog.LoadRecipes()
```

### Client Game Data Loading
```
UnityClient → GameDataManager.LoadFromStreamingAssets()
    → Parse items.json, recipes.json
    → Build ItemRegistry, RecipeRegistry
```

### Crafting Flow
```
PlayerActionHandler.OnCraftRequest()
    → RecipeManager.FindMatchingRecipe(input)
    → ValidateIngredients() → ConsumeIngredients()
    → ApplyReplacements() → GiveResults()
```
