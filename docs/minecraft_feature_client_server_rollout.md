# Minecraft Client & Server Feature Rollout (2025-10-25)

This living note consolidates every Minecraft-style feature currently tracked across the .NET server and the Unity client. Keep the feature inventory and the sequential delivery list updated so future sessions can resume quickly without over-sized plans.

## Feature Inventory
| ID | Feature | Server Scope | Client Scope | Status | Next Action / Owner |
|----|---------|--------------|--------------|--------|---------------------|
| F-01 | Authentication & Session | Credential validation, session tokens, heartbeats | Login UI, token retention | Done | Monitor reconnect edge cases. |
| F-02 | Player Movement Sync | Authoritative validation, clamp velocity, persist snapshots | Prediction + reconciliation, transform smoothing | Done | Validate teleport safeguards post self-test. |
| F-03 | Chunk Streaming & Caching | Serve chunk payloads, maintain residency cache | Request/cull chunks, rebuild meshes | Done | Review cache eviction metrics quarterly. |
| F-04 | Block Interaction Broadcast | Apply block deltas, broadcast drops | Update local chunks, trigger FX/SFX | Done | Particle/audio polish (Task-04A). |
| F-05 | Item Drop Visibility | Persist dropped items, include metadata in updates | Spawn pickup visuals, attach loot UI | Done | None. |
| F-06 | Chunk Residency Tracking | Track loaded chunks per session | Maintain loaded set, avoid redundant fetches | Done | Residency analytics now powering HUD. |
| F-07 | Residency Eviction Policies | TTL pruning, memory budget enforcement, offline cleanup | Passive | Done | Periodic metrics logging only. |
| F-08 | Client Chunk Unload Signal | Accept unload requests, acknowledge residency removal | Emit unload notifications when despawning chunks | Done | Expand telemetry counters when needed. |
| F-09 | Inventory Snapshot Persistence | Store JSON snapshots, diff on reconnect | Consume diffs, refresh hotbar/inventory UI | Done | Monitor SQLite growth rate. |
| F-10 | World Time & Weather Sync | Tick world time, broadcast weather payloads | Update lighting, HUD, ambient FX/audio | Done | Task-10E ambient preset authoring. |
| F-11 | Remote Player Entity Sync | Broadcast spawn/update/despawn + velocity samples | Spawn avatars, smooth transforms, pool/cull | Done | Watch pooling hit rate and distance thresholds. |
| F-12 | Crafting & Container Persistence | Persist containers, validate hash handshake, log mismatches | Wire container UI prefabs, reconcile diffs | In Progress | Task-12C UI hookup, Task-12D interaction triggers. |
| F-13 | Server Status HUD | Supply metrics endpoint & responses | Render overlay, manual refresh | Done | Task-13A pause-menu mirror. |
| F-14 | Weather FX & Ambient Audio | Provide intensity/duration snapshots | Bind intensity to particles/audio presets | In Progress | Task-10E preset bindings. |
| F-15 | Combat Feedback & Damage Numbers | Emit combat event payloads | Display damage popups & hit feedback | Planned | Task-15A define combat schema. |
| F-16 | Mob AI & Spawning Framework | Simulate mobs, pathing, spawn rules | Render mob proxies, animate, cull | Planned | Task-16A prototype tick scheduler. |
| F-17 | World Persistence & Backup | Save chunks/players, schedule backups | Handle save notifications, reload state | Planned | Task-17A evaluate SQLite/world split. |
| F-18 | Block Lighting & Sky Light | Compute/propagate light levels | Apply lightmaps/shaders per chunk | Planned | Task-18A analyze chunk mesh data. |
| F-19 | Death & Respawn Notifications | Broadcast death/respawn payloads, persist respawn anchors | Refresh remote avatars, HUD death feed, respawn UI | Done | Task-19E analytics polish (post HUD). |
| F-20 | Server Analytics & Telemetry | Aggregate residency + death/respawn counters, expose status snapshots | Display analytics in HUD + pause menu | In Progress | Task-20A delivered death counters; Task-20B add pause-menu telemetry. |
| F-21 | Chat Moderation & Social Tools | Maintain mute/block lists, profanity filtering, reporting | Provide moderation UI, filtered chat feed | Planned | Task-21A wire profanity pipeline + mute sync. |
| F-22 | Player Options & Keybind Sync | Persist per-player settings, expose config endpoints | Present settings UI, push updates | Planned | Task-22A define settings contract + persistence layout. |

## Sequential Delivery Plan (Next Iterations)
1. ✅ **Task-20A** – Extended `ServerStatusResponse` with death/respawn counters and surfaced them in the Unity HUD analytics ticker (2025-10-25).
2. ☐ **Task-12C** – Bind container diff events into chest/furnace UI prefabs with optimistic updates (requires prefab wiring + UX validation).
3. ☐ **Task-19B** – Consume `PlayerRespawnBroadcast`/`PlayerDeath` on the Unity side for remote avatar refresh + death feed polish.
4. ☐ **Task-10E** – Author ambient/weather presets and bind the intensity scalar to lighting and audio controllers.
5. ☐ **Task-13A** – Mirror the server status telemetry (including new death counters) inside the pause menu overlay.
6. ☐ **Task-12D** – Detect in-world container interactions and invoke `ContainerManager.RequestOpen/Close` to complete the loop.
7. ☐ **Task-20B** – Extend analytics snapshots with pause-menu surfacing and rolling averages (depends on Task-13A HUD plumbing).

Keep the list capped to work that fits within a single session. When picking up the next task, update the plan to reflect what was delivered and what remains.

## Carryover Checklist
- [ ] Validate Task-12C UI wiring (prefabs + optimistic slot updates).
- [ ] Wire Task-19B client listeners into `RemoteEntityManager` and `DeathFeedUI`.
- [ ] Build ambient preset assets & bindings (Task-10E) and document required Unity scenes.
- [ ] Implement pause-menu metrics panel (Task-13A) leveraging the extended server status DTO.
- [ ] Add Task-20B pause-menu analytics once Task-13A lands, including rolling death-rate samples.
- [ ] Kick off Task-21A profanity filter + mute list sync once analytics/containers stabilize.
