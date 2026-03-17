# Session 178 Design Execution Guide (2026-03-17)

## 1. Purpose
- Define how session-178 and onward core/content implementation work must consume design references.
- Keep gameplay/system decisions aligned with server-authoritative multiplayer and data-driven content constraints.

## 2. Mandatory Reference Documents
- Primary gameplay design:
  - `design/2026-03-16-minecraft-clone-game-design.md`
- Data template and JSON pipeline:
  - `design/2026-03-16-game-data-template-pipeline.md`
- Architecture/code-flow snapshot for current baseline:
  - `docs/2026-03-17-session-178-architecture-and-code-flow.md`

## 3. Feature Execution Rule
- Before implementing any core/content feature, identify:
  - gameplay pillar mapping (exploration/building/survival/social),
  - authoritative owner (server vs client),
  - dataset source (`config/game-data/*.json`),
  - protocol message impacts (`proto/*.proto`, `SharedProtocol/*`).

## 4. Data-Driven Rule
- Runtime tunable values must be sourced from JSON.
- Authoring starts from markdown templates and is exported by C# tooling.
- Hardcoding constants is allowed only for non-tunable engine invariants.

## 5. Session-178 Priority Backlog (Execution-Ready)
1. Data loading contract checks:
   - validate all required game-data datasets exist on startup.
2. JSON schema guardrails:
   - add strict validation for `items`, `recipes`, `monsters`, `npcs`, `character_stats`.
3. Runtime drift prevention:
   - ensure server/client profile or content hash parity logging is explicit in startup/selftest logs.
