# Minecraft Feature Execution Tracking

This document aggregates the core Minecraft-style features across the Unity client and .NET server, captures their current status, and records the next actionable items. It complements `minecraft-feature-plan.md` by offering a compact execution view for day-to-day work sequencing.

## Feature Catalogue

| ID | Feature | Server Status | Client Status | Notes |
|----|---------|---------------|---------------|-------|
| F-01 | Authentication & Session | Done | Done | Login/heartbeat solid via `LoginHandler` and `MinecraftGameClient`. |
| F-02 | Player Movement Sync | Done | Done | Tick updates authoritative; Unity reconciles corrections. |
| F-03 | Chunk Streaming & Caching | Done | Done | Cache hints + duplicate suppression live. |
| F-04 | Block Interaction Broadcast | Done | Done | Broadcast + drop summaries in place. |
| F-05 | Item Drop Visibility | Done | Done | Drop stream surfaced through Unity events. |
| F-06 | Chunk Residency Tracking | Done | Done | Residency + eviction budgets wired. |
| F-07 | Chunk Residency Eviction | Done | Passive | Server pruning only; client unaffected. |
| F-08 | Client Chunk Unload Signal | Done | Done | Bidirectional unload handshake. |
| F-09 | Inventory Snapshot Persistence | Done | Done | Unity now applies diff snapshots via `InventoryItemsUpdated`. |
| F-10 | World Time & Weather Sync | In progress | Planned | Need skybox/day-night binding + HUD indicators. |
| F-11 | Entity Interpolation & Culling | Planned | Planned | Requires interpolation buffers and culling heuristics. |
| F-12 | Crafting & Container Persistence | Planned | Planned | Build on F-09 plus shared container protocol. |
| F-13 | Server Status HUD | Done | Done | Overlay + auto-refresh complete. |

## Current Execution Order

1. ✅ Task-09B — Unity inventory snapshot/diff consumer (hotbar wiring + event feed).
2. 🔄 Task-09C — Session shutdown hook to persist final snapshot and analytics counters.
3. ⏭ Task-10A — Bind Unity skybox/day-night controller to `TimeUpdateBroadcast` and surface HUD time/weather text.
4. ⏭ Task-10B — Author weather FX toggles and lerped intensity handlers in Unity.
5. ⏭ Task-11A — Prototype remote entity interpolation buffers and culling heuristics.

Progress should proceed in order; if any task proves too large for a single iteration, capture the remaining work here before moving on.

## Recent Highlights

- Unity client now parses server-provided inventory snapshots, builds stable numeric IDs, and raises `InventoryItemsUpdated` for UI systems.
- `MinecraftPlayerController` consumes the new event to keep the hotbar aligned with server state.
- Documentation and backlog entries updated to reflect the completed inventory diff consumer.

## Next Steps Checklist

- [ ] Implement Task-09C persist-on-shutdown pipeline.
- [ ] Deliver Task-10A/10B time & weather visual binding.
- [ ] Spike Task-11A interpolation buffers; capture metrics for tuning.

