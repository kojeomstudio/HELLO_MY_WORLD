# Minecraft Feature Execution Tracking

This document enumerates the Minecraft-style features required across the Unity client and .NET server and captures the task queue for sequential delivery. Update this file whenever scope or status changes so future iterations can resume quickly.

## Feature Catalogue
| ID | Feature | Server Status | Client Status | Notes |
|----|---------|---------------|---------------|-------|
| F-01 | Authentication & session heartbeat | Done | Done | Login handlers issue tokens; Unity login flow covers reconnect and heartbeat. |
| F-02 | Player movement & state sync | Done | Done | Tick-based corrections already in place. |
| F-03 | Chunk streaming & caching | Done | Done | Cache hints and duplicate suppression live on both sides. |
| F-04 | Block interaction broadcast | Done | Done | Broadcast with drop metadata via SessionManager helpers. |
| F-05 | Item drop visibility | Done | Done | Unity surfaces drop events for UI and pickups. |
| F-06 | Chunk residency tracking | Done | Done | Residency registry with TTL pruning runs server-side; client maintains loaded set. |
| F-07 | Chunk residency eviction | Done | Passive | Server enforces budgets; client unaffected. |
| F-08 | Client chunk unload signal | Done | Done | Bidirectional unload handshake trims residency instantly. |
| F-09 | Inventory snapshot persistence | Done | Done | Server snapshots and Unity diff consumer online. |
| F-10 | World time and weather broadcasts | Done | Done | Server systems stream ticks; Unity drives lighting, HUD, and weather events. |
| F-11 | Remote player entity sync & interpolation | Done | Done | Velocity samples, distance culling, and pooled avatars keep remote players responsive without leaks. |
| F-12 | Crafting and container persistence | In progress (snapshot hashes + telemetry) | Planned (UI wiring pending) | Builds on inventory diff support; Task-12C will bind UI; telemetry shipped 2025-10-15. |
| F-13 | Server status HUD | Done | Done | Overlay refreshes metrics automatically every 15 seconds. |
| F-14 | Weather FX and ambient audio | In progress | In progress | Weather controller routes intensity to particles and audio; asset wiring remains. |
| F-15 | Combat feedback and damage numbers | Planned | Planned | Requires combat log events and client damage indicators. |
| F-16 | Mob AI & spawning framework | Planned | Planned | Needs server mob simulation, pathing, and client proxy actors. |
| F-17 | World persistence & backup automation | Planned | Planned | Needs incremental world saves, rotation policies, and client save notifications. |
| F-18 | Block lighting & sky light propagation | Planned | Planned | Waiting on chunk mesh analysis to carry block light values. |
| F-19 | Death & respawn notifications | In progress (server broadcast added) | Planned (remote entity/HUD wiring) | Respawn broadcast now ships to peers; Unity still needs handlers and death feed. |

## Active Task Queue (Oct 2025)
- [x] Task-12B - Instrument container hash mismatches and expose the counter via ServerStatusResponse.
- [ ] Task-12C - Bind container diff events into chest/furnace UI prefabs with optimistic updates.
- [ ] Task-10E - Author ambient presets and bind weather intensity to scene lights/sounds.
- [ ] Task-13A - Surface server metrics in the pause menu overlay.
- [x] Task-13B - Capture chunk residency metrics for server observability.
- [x] Task-19A - Broadcast PlayerRespawn messages to active sessions.
- [ ] Task-19B - Unity consumes PlayerRespawn broadcasts to refresh remote avatars and death feed.

## Recently Completed
- Container hash mismatch telemetry now feeds the diagnostics endpoint so the HUD can display snapshot correction counts.
- Server status requests now return chunk residency counters so the HUD can track total and peak residency.
- Remote player distance culling and avatar pooling landed, keeping remote entities lightweight and out of view when far away.
- Unity inventory snapshot diff consumer kept hotbar in sync with server reconnections.
- Server chunk residency eviction now enforces TTL and budgets without leaks.
- Time and weather broadcasts now update Unity lighting, HUD, and FX controllers.
- Entity sync groundwork landed: server now emits spawn/update/despawn messages while the client spawns and smooths remote player avatars.
- Player respawn broadcasts now reach all connected sessions, unblocking client-side death feed work.
