# Session 202 Design Execution

## Design Goal
Define a practical Minecraft-clone execution track for Unity client + .NET server with minetest-aligned content semantics and data-driven delivery.

## Core Design Principles
- Reference-first: `minetest_project/` behavior is the source baseline.
- Data-driven content: gameplay content must be authored as JSON datasets.
- Deterministic validation: compile/test and export steps must be scriptable.

## Required Content Domains
- `items.json`
- `recipes.json`
- `monsters.json`
- `npcs.json`
- `character_stats.json`

Each domain is isolated by responsibility to reduce schema coupling.

## Crafting Design (minetest-aligned)
- Recipe methods: `NORMAL`, `COOKING`, `FUEL`
- Recipe forms: shaped (`shaped`, `width`, `height`) and non-shaped
- Group ingredients: `group:*` (ex: `group:wood`, `group:grain`)
- Replacement semantics: `replacements` (ex: milk bucket -> bucket)

## Execution Workflow
1. Update `design/templates/game-data-template.md`.
2. Export datasets with `Tools/GameDataTemplateExporter`.
3. Validate server build and selftest.
4. Run Unity commandlet compile/test in batch mode.
5. Record work status in `plans/` with commit/date tracking.

## Milestones
1. M1 Data Schema Stability
- Keep Unity parser (`CraftingManager`) and server parser (`GameDataCatalog`) compatible with both legacy and current recipe fields.

2. M2 Core Gameplay Loop Coverage
- Ensure gathering -> crafting -> progression flow is fully data-driven.

3. M3 Automation Hardening
- Keep commandlet + batch script runnable for local/CI validation.

## Operational Rule
Any new core/content feature must follow:
1. Design update in `design/*.md`
2. Template update in `design/templates/*.md`
3. JSON export via C# tool in `Tools/`
4. Compile/test validation logs
