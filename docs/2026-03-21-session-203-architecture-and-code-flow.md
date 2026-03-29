# Session 203 Architecture And Code Flow

## Objective
Verify and document the current project state including compile/test infrastructure, game data pipeline, and minetest reference integration as required by `work/work.md`.

## Baseline Summary
- Baseline HEAD before work: `75b0f850` (`2026-03-21 01:11:36 +0900`).
- All .NET projects compile successfully.
- Server selftest passes with valid game data and protocol parity.

## Project Architecture Overview

### Directory Structure (Key Paths)
```
HelloMyWorld_repo/
├── Assets/                     # Unity client
│   ├── MyAssets/Scripts/
│   │   ├── Editor/Automation/  # CI commandlet
│   │   ├── GameWorld/          # Gameplay systems
│   │   └── Network/            # Client networking
│   └── StreamingAssets/        # Runtime data (game-data, world-map-control)
├── GameServer/                 # .NET server
│   ├── Handlers/               # Request handlers
│   ├── Systems/                # Game systems
│   └── config/                 # Server configuration
├── config/                     # Shared configuration
│   └── game-data/              # JSON game data
├── design/                     # Design documents
│   └── templates/              # MD templates for game data
├── docs/                       # Architecture documentation
├── minetest_project/           # Reference submodule
├── plans/                      # Work plans with checkboxes
├── proto/                      # Protocol buffer definitions
├── scripts/                    # Utility scripts
├── SharedProtocol/             # Shared DTOs
└── Tools/                      # C# utility tools
    ├── GameDataTemplateExporter/
    └── DummyMinecraftClient/
```

## Game Data Pipeline

### Flow
1. Author template in `design/templates/game-data-template.md`
2. Export via `Tools/GameDataTemplateExporter` (.NET 8.0)
3. Output to `config/game-data/*.json`
4. Mirror to:
   - `GameServer/config/game-data/`
   - `Assets/StreamingAssets/game-data/`

### Supported Datasets
| Dataset | Type | Description |
|---------|------|-------------|
| items | Array | Item definitions with groups |
| recipes | Array | Crafting recipes (NORMAL/COOKING/FUEL) |
| monsters | Array | Monster definitions |
| npcs | Array | NPC definitions |
| character_stats | Object | Player stat baselines |

### Recipe Schema (minetest-aligned)
```json
{
  "id": "recipe_id",
  "method": "NORMAL|COOKING|FUEL",
  "shaped": true|false,
  "width": 3,
  "height": 3,
  "results": [{ "item_id": "...", "amount": 1 }],
  "ingredients": [{ "item_id": "..." }|{ "group": "group:..." }],
  "replacements": [{ "from": "...", "to": "..." }]
}
```

## Unity Compile/Test Commandlet

### Entry Points
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileAndTests`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileOnly`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunEditModeTests`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunPlayModeTests`

### Execution Flow
1. Request script compilation via `CompilationPipeline`
2. Poll until compilation completes or fails
3. Run test modes via `TestRunnerApi`
4. Write JSON summaries to `reports/unity-tests/`
5. Exit with deterministic code in batch mode

### Batch Runner
```batch
scripts\unity_compile_test.bat --unity "C:\Path\To\Unity.exe" --mode all|compile|edit|play
```

## minetest Reference Mapping

### Used Patterns
| Minetest Pattern | Our Implementation |
|------------------|-------------------|
| `core.register_item` | JSON items dataset |
| `core.register_craft` | JSON recipes dataset |
| `group:*` ingredients | `ingredients[].group` field |
| Shaped recipes | `shaped`, `width`, `height` fields |
| Replacements | `replacements` array |
| Method types | `method` enum (NORMAL/COOKING/FUEL) |

### Reference Files
- `minetest_project/builtin/game/register.lua` - Registration system
- `minetest_project/builtin/game/item.lua` - Item handling
- `minetest_project/src/craftdef.cpp` - Craft implementation

## Document Cleanup Review

### Session Documents (plans/, docs/)
- Sessions 181-202: All within 7 days, internally consistent
- No deletions required

### Persistent Reference
- `docs/2026-03-18-minetest-architecture-reference.md` - Long-term architecture reference

## Validation Status
- [x] SharedProtocol builds
- [x] GameServer builds
- [x] GameDataTemplateExporter builds
- [x] Game data export succeeds (5 datasets)
- [x] Server selftest passes (game data validation, protocol parity)
