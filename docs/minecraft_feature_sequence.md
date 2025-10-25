# Minecraft Feature Implementation Sequence (2025-10-15)

This sequence lists the Unity client and .NET server features that bring the project in line with core Minecraft gameplay. Keep the status and next action columns up to date so we can resume work in small, tractable steps.

## Feature Inventory
| ID | Feature | Server Responsibilities | Client Responsibilities | Status | Notes |
|----|---------|-------------------------|-------------------------|--------|-------|
| F-01 | Authentication & Session | Credential validation, session heartbeats | Login UI, token retention | Done | Keep reconnect logic under observation. |
| F-02 | Player Movement Sync | Authoritative movement validation & clamps | Prediction, reconciliation, transform events | Done | Teleport safeguards in place. |
| F-03 | Chunk Streaming & Caching | Serve chunk payloads, track residency cache | Request/cull chunks, rebuild meshes | Done | Monitor cache hit telemetry. |
| F-04 | Block Interaction Broadcast | Apply block changes, broadcast drops | Refresh local chunks, play FX/audio | Done | Future polish: particles/audio (Task-04A). |
| F-05 | Item Drop Visibility | Persist loose item entities & metadata | Spawn pickup visuals, attach loot UI | Done | No active follow-up. |
| F-06 | Chunk Residency Tracking | Maintain per-session loaded chunk registry | Maintain loaded set client-side | Done | Residency analytics now surface in the status HUD (Task-13B). |
| F-07 | Residency Eviction Policies | TTL pruning & budget caps | Passive | Done | Logging hooks exist. |
| F-08 | Client Chunk Unload Signals | Accept unload requests & ack removal | Emit unload notifications | Done | Expand telemetry counters later. |
| F-09 | Inventory Snapshot Persistence | Store JSON snapshots, diff on reconnect | Consume diffs & refresh UI | Done | Validate SQLite growth periodically. |
| F-10 | World Time & Weather Sync | Tick world time, schedule weather broadcasts | Update lighting, HUD, FX, ambient audio | Done | Author ambient presets (Task-10E). |
| F-11 | Remote Player Entity Sync | Broadcast spawn/update/despawn, velocity clamps | Spawn avatars, smooth & cull | Done | Distance culling & pooling landed (Task-11C). |
| F-12 | Crafting & Container Persistence | Persist containers, broadcast diffs, validate hashes, track hash mismatches | Present container UI, reconcile diffs | In Progress | Task-12A hash handshake shipped; Task-12B telemetry active; Task-12C UI wiring pending. |
| F-13 | Server Status HUD | Supply metrics endpoint & responses | Render overlay, manual refresh | Done | Extend into pause menu (Task-13A). |
| F-14 | Weather FX & Ambient Audio | Provide intensity/duration snapshots | Bind to particle/audio presets | In Progress | Need preset authoring (Task-10E). |
| F-15 | Combat Feedback & Damage Numbers | Emit combat events with payloads | Display popups & hit feedback | Planned | Define combat event schema (Task-15A). |
| F-16 | Mob AI & Spawning | Simulate mobs, pathing, spawn rules | Render mob proxies, animate, cull | Planned | Prototype tick scheduler (Task-16A). |
| F-17 | World Persistence & Backup | Save world/chunks, schedule backups | Handle save notifications, reload state | Planned | Evaluate SQLite/world file split (Task-17A). |
| F-18 | Block Lighting & Sky Light | Compute light levels & propagate | Apply lightmaps/shaders | Planned | Requires chunk mesh analysis (Task-18A). |
| F-19 | Death & Respawn Notifications | Broadcast death/respawn payloads to world peers, persist respawn anchors | Refresh remote avatars, surface death feed, trigger respawn UI | In Progress | Task-19A respawn + Task-19D death broadcasts delivered; Unity HUD wiring (Task-19B) still open. |
| F-20 | Server Analytics & Telemetry | Capture residency, death/respawn, and performance counters | Surface analytics in HUD and pause menu overlays | In Progress | Task-20A shipped death counters; Task-20B will extend pause menu telemetry. |

Legend: Done | In Progress | Planned

## Sequential Work Items
- [x] Task-12A - Introduced container snapshot hashes and diff validation (delivered 2025-10-12).
- [x] Task-12B - Recorded container hash mismatches and exposed the counter via diagnostics (delivered 2025-10-15).
- [ ] Task-12C - Bind container diff events into chest/furnace UI prefabs with optimistic updates. *Scripts for panel & slot views landed; prefab wiring + user input still pending.*
- [x] Task-11C - Added distance-based culling and pooled avatar reuse for remote players.
- [ ] Task-10E - Author ambient presets and bind weather intensity to scene lights/sounds.
- [x] Task-13B - Captured chunk residency metrics and exposed them through the server status path (delivered 2025-10-16).
- [x] Task-19A - Broadcast PlayerRespawn events from the server to online sessions (delivered 2025-10-17).
- [x] Task-19D - Broadcast PlayerDeath messages to active sessions (delivered 2025-10-21).
- [ ] Task-19B - Consume PlayerRespawn broadcasts inside the Unity remote entity manager and HUD death feed.
- [x] Task-20A - Extend server status telemetry with death/respawn counters for HUD analytics (delivered 2025-10-25).

## Parking Lot
- After Task-12C, expand the self-test harness to cover container open/update/close flows.
- Document the container hash handshake in docs/networking-protocol.md once UI churn settles.
- Revisit crafting grid persistence once UI validation is complete (Task-12D placeholder).
- Extend Task-19B with death broadcast wiring and respawn UI polish once Unity handlers are in place.
