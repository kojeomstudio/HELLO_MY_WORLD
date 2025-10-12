# Minecraft Feature Scope – October 2025

This note tracks the cross-cutting Minecraft features that must ship for parity with vanilla survival. It complements `docs/minecraft_feature_masterlist.md` by highlighting the live scope and immediate next actions.

## High-Level Checklist
| ID | Feature | Server Focus | Client Focus | Status |
|----|---------|--------------|--------------|--------|
| F-01 | Authentication & Session | Token auth, session heartbeats | Login UI & reconnect flow | ✅ Complete |
| F-02 | Movement Sync | Movement validation, velocity clamps | Prediction & reconciliation | ✅ Complete |
| F-03 | Chunk Streaming | Chunk cache, residency tracking | Chunk loader & mesh rebuilds | ✅ Complete |
| F-04 | Block Interaction | Authoritative mutations & drops | Local rebuild + FX/audio | ✅ Complete |
| F-09 | Inventory Persistence | Snapshot storage, diffs | Hotbar refresh, event feed | ✅ Complete |
| F-10 | Time & Weather | WorldTimeSystem, WeatherSystem | Lighting/HUD/weather FX | ✅ Complete |
| F-11 | Entity Sync | Spawn/update/despawn broadcasts | Remote entity pooling & lerp | ✅ Complete |
| F-12 | Crafting & Containers | **ContainerSystem** persists shared slots, broadcasts updates | **ContainerManager** tracks state; UI wiring pending | 🚧 In progress |
| F-14 | Weather FX & Audio | Intensity snapshots | Bind audio/lighting presets | 🚧 In progress |
| F-15 | Combat Feedback | Combat event schema | Damage popups, feedback | ⏳ Planned |
| F-16 | Mob AI & Spawning | Mob simulation loop | Mob proxies & animation | ⏳ Planned |

## Implementation Plan (Sprint Oct-2)
1. **Finish container round-trip**: expose ContainerManager events to UI and author baseline chest panel.
2. **Extend container types**: add furnace fuel/progress sync and validate slot metadata.
3. **Regression coverage**: add protocol regression tests for container diff + inventory snapshots.
4. **Ambient polish**: land Task-10E by binding default lighting/audio presets to WeatherSystem intensity.

## Follow-up Backlog
- Author UX for container close/reopen cues and conflict handling (multiple users).
- Persist furnace smelt timers server-side so reconnecting players resume progress.
- Expand server metrics pipeline (Task-13A) to include container occupancy stats.
- Begin combat event schema (F-15) once container/UI polish lands.

Keep updating this scope file whenever a feature status moves or the plan changes so future sessions can resume without rediscovery.
