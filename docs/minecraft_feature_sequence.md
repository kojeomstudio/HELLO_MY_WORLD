# Minecraft Feature Implementation Sequence (2025-10-12)

This sequence lists the Unity client and .NET server features that bring the project in line with core Minecraft gameplay. Keep the status and next action columns up to date so we can resume work in small, tractable steps.

## Feature Inventory
| ID | Feature | Server Responsibilities | Client Responsibilities | Status | Notes |
|----|---------|-------------------------|-------------------------|--------|-------|
| F-01 | Authentication & Session | Credential validation, session heartbeats | Login UI, token retention | ✅ Done | Keep reconnect logic under observation. |
| F-02 | Player Movement Sync | Authoritative movement validation & clamps | Prediction, reconciliation, transform events | ✅ Done | Teleport safeguards in place. |
| F-03 | Chunk Streaming & Caching | Serve chunk payloads, track residency cache | Request/cull chunks, rebuild meshes | ✅ Done | Monitor cache hit telemetry. |
| F-04 | Block Interaction Broadcast | Apply block changes, broadcast drops | Refresh local chunks, play FX/ audio | ✅ Done | Future polish: particles/audio (Task-04A). |
| F-05 | Item Drop Visibility | Persist loose item entities & metadata | Spawn pickup visuals, attach loot UI | ✅ Done | No active follow-up. |
| F-06 | Chunk Residency Tracking | Maintain per-session loaded chunk registry | Maintain loaded set client-side | ✅ Done | Add residency analytics (Task-13B). |
| F-07 | Residency Eviction Policies | TTL pruning & budget caps | Passive | ✅ Done | Logging hooks exist. |
| F-08 | Client Chunk Unload Signals | Accept unload requests & ack removal | Emit unload notifications | ✅ Done | Expand telemetry counters later. |
| F-09 | Inventory Snapshot Persistence | Store JSON snapshots, diff on reconnect | Consume diffs & refresh UI | ✅ Done | Validate SQLite growth periodically. |
| F-10 | World Time & Weather Sync | Tick world time, schedule weather broadcasts | Update lighting, HUD, FX, ambient audio | ✅ Done | Author ambient presets (Task-10E). |
| F-11 | Remote Player Entity Sync | Broadcast spawn/update/despawn, velocity clamps | Spawn avatars, smooth & cull | 🟡 In Progress | Distance culling & pooling tracked under Task-11C. |
| F-12 | Crafting & Container Persistence | Persist containers, broadcast diffs, validate hashes | Present container UI, reconcile diffs | 🟡 In Progress | Task-12A shipped (hash handshake); UI wiring remains. |
| F-13 | Server Status HUD | Supply metrics endpoint & responses | Render overlay, manual refresh | ✅ Done | Extend into pause menu (Task-13A). |
| F-14 | Weather FX & Ambient Audio | Provide intensity/duration snapshots | Bind to particle/audio presets | 🟡 In Progress | Need preset authoring (Task-10E). |
| F-15 | Combat Feedback & Damage Numbers | Emit combat events with payloads | Display popups & hit feedback | 🔄 Planned | Define combat event schema (Task-15A). |
| F-16 | Mob AI & Spawning | Simulate mobs, pathing, spawn rules | Render mob proxies, animate, cull | 🔄 Planned | Prototype tick scheduler (Task-16A). |
| F-17 | World Persistence & Backup | Save world/chunks, schedule backups | Handle save notifications, reload state | 🔄 Planned | Evaluate SQLite/world file split (Task-17A). |
| F-18 | Block Lighting & Sky Light | Compute light levels & propagate | Apply lightmaps/shaders | 🔄 Planned | Requires chunk mesh analysis (Task-18A). |

Legend: ✅ done · 🟡 in progress · 🔄 planned

## Sequential Work Items
1. ✅ **Task-12A** – Introduce container snapshot hashes, server diff validation, and client resend triggers. (Delivered 2025-10-12.)
2. ☐ **Task-12B** – Record container hash mismatches & diff stats for diagnostics; expose metrics endpoint.
3. ☐ **Task-12C** – Bind ContainerManager events into chest/furnace UI prefabs with optimistic updates.
4. ☐ **Task-11C** – Add distance-based culling and pooled avatar reuse for remote players.
5. ☐ **Task-10E** – Author baseline weather ambient presets and bind intensity curves.

## Parking Lot
- After Task-12C, expand selftest harness to cover open/update/close happy path.
- Document the container hash handshake in `docs/networking-protocol.md` once churn stabilises.
- Revisit crafting grid persistence once UI flow is validated (Task-12D placeholder).
