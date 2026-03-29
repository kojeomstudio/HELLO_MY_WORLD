# Session 179 Architecture and Code Flow (2026-03-17)

## 1. Current Status Snapshot
- `git pull --ff-only`: already synchronized with `origin/master`.
- Baseline at work start: clean tree.
- This session focus: make `config/game-data` startup validation explicit and strict.

## 2. Runtime Validation Chain (Program Startup)
Current `Program.Main` startup validation flow:
1. Proto runtime/contract validation (`ProtoRuntime`, `ProtocolValidator`, `ProtoDiagnostics`).
2. Generated protobuf source freshness check.
3. Feature manifest discovery/validation.
4. Queue/profile/config parity validation.
5. `ValidateGameDataDatasets()` (new in session-179).

## 3. Game-Data Validation Flow (New)
`ValidateGameDataDatasets()` now enforces:
- Required dataset files under `config/game-data/`:
  - `items.json` (array, each element requires `id`)
  - `recipes.json` (array, each element requires `id`, `result`, `ingredients`)
  - `monsters.json` (array, each element requires `id`, `health`, `attack`)
  - `npcs.json` (array, each element requires `id`, `role`)
  - `character_stats.json` (object, requires `base`, `growth_per_level`)
- Required dataset file missing -> startup failure.
- Invalid JSON structure or null required fields -> startup failure.
- Per-dataset SHA-256 hash logging and combined profile hash logging.
- Mirror drift signal:
  - warns if `GameServer/config/game-data` or `Assets/StreamingAssets/game-data` mirror directory is missing.
  - warns if mirror file exists but hash differs from source dataset.

## 4. Data-Driven Content Flow (Template -> Runtime JSON)
1. Author template markdown: `design/templates/game-data-template.md`
2. Export using .NET 8 tool: `Tools/GameDataTemplateExporter`
3. Produce runtime datasets: `config/game-data/*.json`
4. Server startup validates datasets before entering server/selftest run paths.

## 5. Design References for Core/Content Work
- `design/2026-03-16-minecraft-clone-game-design.md`
- `design/2026-03-16-game-data-template-pipeline.md`
- `design/2026-03-17-session-179-design-execution.md`

