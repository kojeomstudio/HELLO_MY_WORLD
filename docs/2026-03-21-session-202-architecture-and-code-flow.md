# Session 202 Architecture And Code Flow

## Objective
This session establishes repeatable Unity compile/test commandlet entry points and aligns the game-data authoring pipeline with minetest-style crafting semantics.

## Baseline Summary
- Baseline HEAD before work: `75b0f850` (`2026-03-21 01:11:36 +0900`).
- Checked recent 1-week commit history and current working tree state before modifications.
- `minetest_project/` reference sources were used for crafting and item grouping semantics.

## minetest Reference Mapping
Reference files:
- `minetest_project/src/craftdef.h`
- `minetest_project/src/craftdef.cpp`
- `minetest_project/src/itemdef.h`

Mapped concepts:
- `group:*` ingredient matching
- shaped recipe metadata (`width`, `height`)
- replacement outputs (`replacements`)
- method split (`NORMAL`, `COOKING`, `FUEL`)

## Data Pipeline
1. Author template datasets in `design/templates/game-data-template.md`.
2. Export with `Tools/GameDataTemplateExporter`.
3. Generate runtime JSON to `config/game-data/*.json`.
4. Runtime consumers:
- Unity: `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- Server: `GameServer/Systems/GameDataCatalog.cs`

## Unity Compile/Test Commandlet
Added editor automation entry points:
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileAndTests`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileOnly`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunEditModeTests`
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunPlayModeTests`

Commandlet flow:
1. Request script compilation.
2. Fail immediately on compile errors.
3. Run requested test mode(s) via `TestRunnerApi`.
4. Write JSON test summaries under `reports/unity-tests/`.
5. Exit with deterministic process code in batch mode.

## Batch Runner
Added `scripts/unity_compile_test.bat`.

Supported options:
- `--unity <path>`
- `--mode all|compile|edit|play`
- `--log <path>`

Env fallback support:
- `UNITY_EXE_PATH`
- `UNITY_EXE_ENV`
- `UNITY_PATH`

## Server Validation Compatibility Fix
`GameServer/Program.cs` game-data validation was updated for recipes:
- Previous strict rule required only `result`.
- New rule accepts either `result` (legacy) or `results` (current schema).
- `ingredients` type validation is enforced as array.

This change resolved selftest failure caused by schema mismatch in `GameServer/config/game-data/recipes.json`.

## Documentation Consistency Review
`plans/` and `docs/` were reviewed for stale/inconsistent entries in this session.
- No deletions were applied because current session records remain consistent with the active codebase history.
