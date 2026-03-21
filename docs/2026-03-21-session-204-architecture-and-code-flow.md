# Session 204 Architecture and Code Flow

## Session Date
- 2026-03-21 (KST)

## Overview
이 세션은 work/worksheet.md 요구사항을 기반으로 프로젝트 검증 및 불필요한 문서 정리 작업을 수행함.

## Architecture Analysis

### 1. Game Data Pipeline
```
design/templates/game-data-template.md
        ↓ (GameDataTemplateExporter tool)
config/game-data/*.json (items, recipes, monsters, npcs, character_stats)
        ↓
Unity Client (StreamingAssets) ← GameServer (config/game-data/)
```

**Tool Implementation:**
- `Tools/GameDataTemplateExporter/` - .NET 8.0 C# tool
- Template parsing → JSON extraction
- Grouped by data type (items, recipes, monsters, etc.)

### 2. minetest Reference Architecture
minetest_project 서브모듈에서 참조할 핵심 패턴:

```
minetest_project/
├── builtin/game/
│   ├── register.lua   # Item/node/craft registration
│   ├── item.lua       # Item definition handling
│   └── falling.lua    # Physics/gravity
├── games/devtest/     # Example game implementation
└── src/               # C++ core engine
```

**Adopted Patterns:**
- group-based ingredients (`group:*`)
- shaped recipes with replacements
- method split (NORMAL/COOKING/FUEL)
- item registration via definition tables

### 3. Unity CI Commandlet Architecture
```
scripts/unity_compile_test.bat
        ↓
UnityCiCommandlet.cs (Editor mode)
        ↓
┌─────────────────────────────────────┐
│ RunCompileAndTests()                │
│   ├── RequestScriptCompilation()    │
│   ├── RunEditModeTests()            │
│   └── RunPlayModeTests()            │
│           ↓                         │
│   reports/unity-tests/*.json        │
└─────────────────────────────────────┘
```

**Modes:**
- `all` - Compile + EditMode + PlayMode tests
- `compile` - Compilation only
- `edit` - EditMode tests only
- `play` - PlayMode tests only

### 4. .NET Build Pipeline
```
SharedProtocol.csproj (net6.0)
        ↓
GameServer.csproj (net6.0)
        ↓
GameCommon.csproj (netstandard2.1) ← NuGet package
        ↓
GameDataTemplateExporter.csproj (net8.0)
```

## Configuration Cleanup Results

### Before Cleanup
- config/: 206 JSON files (mostly session snapshots)
- Root: 10+ outdated JSON files

### After Cleanup
- config/: 26 essential JSON files (biomes, blocks, items, recipes, server, world, etc.)
- Root: Only server-config.json (active server config)

### Kept Files
- Core game data: biomes.json, blocks.json, items.json, recipes.json, item_categories.json
- Server config: server.json, server_config.json, world.json, network.default.json
- Client config: client_config.json
- Terrain: terrain_generation_comprehensive_config.json, enhanced_terrain_generation.json

## Code Flow Summary

### Server Startup Flow
```
Program.cs → Load server-config.json
    → Initialize GameDataCatalog
        → Load config/game-data/*.json
    → Initialize WorldManager
        → Load world.json, terrain config
    → Start TCP listener
    → Accept client connections
```

### Game Data Loading
```
GameDataCatalog.LoadAllAsync()
    → LoadItemsAsync() → items.json
    → LoadRecipesAsync() → recipes.json
    → ValidateRecipeDefinitions()
```

## Recommendations for Future Sessions

1. **Data-Driven Development**
   - Keep all game content in JSON format under config/game-data/
   - Use templates in design/templates/ for authoring
   - Run GameDataTemplateExporter to regenerate JSON

2. **Documentation**
   - Keep session plans in plans/ with checkboxes
   - Architecture docs in docs/
   - Design docs in design/

3. **Build Verification**
   - Always run `dotnet build` before commits
   - Use Unity commandlet for Unity-side verification

4. **minetest Reference**
   - Consult minetest_project/builtin/game/ for patterns
   - Lua registration patterns → JSON equivalents
