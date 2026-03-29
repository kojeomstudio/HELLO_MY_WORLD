# Session 181 Design Execution Guide (2026-03-18)

## 1. Purpose
- Keep core/content implementation anchored to design-first, data-driven rules.
- Ensure runtime behavior is validated from generated JSON datasets, not hardcoded constants.

## 2. Mandatory Design References
- Gameplay/system baseline:
  - `design/2026-03-16-minecraft-clone-game-design.md`
- Data template/export pipeline:
  - `design/2026-03-16-game-data-template-pipeline.md`
- Current architecture/runtime validation snapshot:
  - `docs/2026-03-18-session-181-architecture-and-code-flow.md`

## 3. Execution Rules (Session-181+)
1. Core/content feature work must begin from the design references above.
2. Tunable values and gameplay datasets must remain JSON-backed.
3. Dataset authoring path must be:
   - markdown template (`design/templates/game-data-template.md`)
   - C# exporter (`Tools/GameDataTemplateExporter`, `.NET 8`)
   - runtime JSON under `config/game-data/`
4. Runtime validation gate must remain active via server startup and selftest.

## 4. Minimum Dataset Contract
- `items`: array entries include `id`
- `recipes`: array entries include `id`, `result`, `ingredients`
- `monsters`: array entries include `id`, `health`, `attack`
- `npcs`: array entries include `id`, `role`
- `character_stats`: object includes `base`, `growth_per_level`

## 5. Near-Term Design-Linked Priorities
1. Replace remaining hardcoded gameplay tables with `config/game-data/*.json` reads.
2. Keep mirror parity across server/client world-map-control JSON outputs.
3. Continue optional packet migration from protobuf-net fallbacks to generated Google.Protobuf bindings when promoted from optional to required.
