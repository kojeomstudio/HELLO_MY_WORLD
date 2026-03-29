# Session 183 Design Execution Guide (2026-03-18)

## 1. Purpose
- Keep feature work design-first and data-driven.
- Enforce JSON-backed runtime datasets for tunable gameplay content.
- Maintain parity between design intent, runtime validation, and session documentation.

## 2. Required Design References
- Core design baseline:
  - `design/2026-03-16-minecraft-clone-game-design.md`
- Data template/export process:
  - `design/2026-03-16-game-data-template-pipeline.md`
- Session architecture snapshot:
  - `docs/2026-03-18-session-183-architecture-and-code-flow.md`

## 3. Session 183 Execution Rules
1. Core/content implementation must start from design docs before code edits.
2. Configurable gameplay domains must remain data-driven (`*.json`).
3. Dataset authoring path remains fixed:
   - markdown template (`design/templates/game-data-template.md`)
   - exporter tool (`Tools/GameDataTemplateExporter`, `.NET 8`)
   - runtime JSON (`config/game-data/*.json`)
4. Validate with compile + selftest on each session.

## 4. Data Contract Baseline
- `items`: array, each entry contains `id`
- `recipes`: array, each entry contains `id`, `result`, `ingredients`
- `monsters`: array, each entry contains `id`, `health`, `attack`
- `npcs`: array, each entry contains `id`, `role`
- `character_stats`: object with `base`, `growth_per_level`

## 5. Session 183 Outcome Anchors
1. Template export succeeded for all 5 required datasets.
2. Compile validation completed with zero errors.
3. Runtime selftest completed (exit 0) with known non-fatal warnings.
4. World-map profile/proto probe artifacts refreshed.

## 6. Immediate Design-Linked Priorities
1. Resolve selftest `Unexpected response type` mismatches by updating protocol expectation design and test flow.
2. Define promotion criteria for optional protobuf packet bindings currently in fallback/partial state.
3. Decide and document strategy for game-data mirror directories to remove startup warning noise.
