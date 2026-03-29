# Session 179 Design Execution Guide (2026-03-17)

## 1. Purpose
- Align core/content implementation with the data-driven design baseline.
- Ensure game data integrity is validated before runtime behavior is exercised.

## 2. Mandatory Design References
- Gameplay/system intent:
  - `design/2026-03-16-minecraft-clone-game-design.md`
- Template/export pipeline:
  - `design/2026-03-16-game-data-template-pipeline.md`
- Current architecture/code-flow snapshot:
  - `docs/2026-03-17-session-179-architecture-and-code-flow.md`

## 3. Execution Rules (Session-179+)
1. Core/content implementation must start from design docs above.
2. Tunable gameplay data must remain JSON-based and be sourced from `config/game-data/*.json`.
3. Dataset authoring must start in markdown template and be exported through the C# tool:
   - `Tools/GameDataTemplateExporter` (`net8.0`)
4. Server startup must continue to pass required game-data validation checks:
   - dataset presence
   - JSON root kind
   - required keys
   - hash/profile hash logging

## 4. Data Schema Contract (Minimum)
- `items`: each element includes `id`
- `recipes`: each element includes `id`, `result`, `ingredients`
- `monsters`: each element includes `id`, `health`, `attack`
- `npcs`: each element includes `id`, `role`
- `character_stats`: object includes `base`, `growth_per_level`

## 5. Next Design-Linked Priorities
1. Wire gameplay runtime systems to consume `config/game-data/*.json` as primary data source where hardcoded lists still exist.
2. Decide mirror strategy for `game-data` datasets (`GameServer/config/game-data`, `Assets/StreamingAssets/game-data`) and automate parity if needed.
3. Extend schema guardrails from minimum required keys to domain-specific constraints (value ranges, enum domains, unique IDs).

