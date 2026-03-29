# Session 189 Design Execution (2026-03-19)

## Goal
Prepare the next Minecraft-clone implementation slice with Minetest-aligned architecture references, Unity-adapted execution rules, and data-driven content constraints.

## Required Upstream References
- `minetest_project/src/server.cpp`
- `minetest_project/src/emerge.cpp`
- `minetest_project/doc/world_format.md`

## Design Principles for This Repository
1. Authoritative server loop
- Server remains source of truth for world, session, and gameplay outcomes.
- Client applies prediction and visual smoothing only.

2. Queue-based world generation control
- Adopt Minetest-like queue governance (limits, priority, completion accounting).
- Keep queue profile in external JSON so tuning does not require code edits.

3. Data-driven gameplay content
- Keep recipes, monsters, NPCs, items, and stats in JSON runtime files.
- Author source templates in Markdown, export via a C# helper tool (`net8.0` or `net9.0`).

4. Design-first implementation
- New content/core features must be described in `design/*.md` before coding.
- Design and data schema updates move together.

## Session Execution Tasks
- Verify baseline status from recent commits and local diffs.
- Produce architecture/code-flow documentation for current state.
- Validate game-data template-to-JSON export path.
- Run compile and runtime smoke tests.
- Record execution/commit evidence in `plans/`.

## Deliverables
- Plan: `plans/2026-03-19-session-189-comprehensive-work-plan.md`
- Architecture doc: `docs/2026-03-19-session-189-architecture-and-code-flow.md`
- This design execution doc: `design/2026-03-19-session-189-design-execution.md`

## Definition of Done
- Build succeeds for `SharedProtocol` and `GameServer`.
- `GameServer --selftest` exits with code 0.
- Game-data exporter emits valid JSON datasets.
- Plan doc records commit hash/date and origin reflection result.
