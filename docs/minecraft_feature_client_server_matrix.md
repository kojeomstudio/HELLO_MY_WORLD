# Minecraft Client & Server Feature Matrix (2025-10-21)

This matrix consolidates the end-to-end Minecraft-style features we are tracking. Each row pairs the .NET server responsibilities with the Unity client work so future sessions can pick up sequentially without rediscovery.

| ID | Feature | Server Responsibilities | Client Responsibilities | Status | Next Increment |
|----|---------|-------------------------|-------------------------|--------|----------------|
| F-01 | Authentication & Session | Validate credentials, issue session tokens, enforce heartbeats | Present login UI, persist token, surface errors | Done | Monitor reconnect edge cases. |
| F-02 | Player Movement Sync | Authoritative validation, clamp velocity, persist state | Prediction + reconciliation, transform smoothing | Done | Teleport safeguard telemetry. |
| F-03 | Chunk Streaming & Caching | Serve chunk payloads, maintain residency cache | Request/cull chunks, rebuild meshes | Done | Evaluate cache eviction metrics. |
| F-04 | Block Interaction Broadcast | Apply block deltas, broadcast drops | Refresh local chunks, trigger VFX/SFX | Done | Particle/audio polish tracked separately. |
| F-05 | Item Drop Visibility | Persist dropped items, include metadata in updates | Spawn pickup visuals, attach loot UI | Done | None. |
| F-06 | Chunk Residency Tracking | Maintain loaded-chunk registry per session | Maintain loaded set, avoid redundant fetches | Done | Residency analytics live in HUD. |
| F-07 | Residency Eviction Policies | TTL pruning, memory budgeting, offline cleanup | Passive | Done | Periodic metrics logging. |
| F-08 | Client Chunk Unload Signal | Accept unload requests, acknowledge residency removal | Emit unload notifications when despawning chunks | Done | Expand telemetry counters. |
| F-09 | Inventory Snapshot Persistence | Store JSON snapshots, diff on reconnect | Consume diffs, refresh hotbar/inventory UI | Done | Monitor SQLite growth. |
| F-10 | World Time & Weather Sync | Tick world time, broadcast weather payloads | Update lighting, HUD, ambient audio | Done | Author ambient presets (Task-10E). |
| F-11 | Remote Player Entity Sync | Broadcast spawn/update/despawn + velocity samples | Spawn avatars, smooth transforms, pool/cull | Done | Watch pooling hit rate. |
| F-12 | Crafting & Container Persistence | Persist grids/containers, validate hash handshake, log mismatches | Wire container UI prefabs, reconcile diffs | In Progress | Task-12C: hook chest/furnace prefabs. |
| F-13 | Server Status HUD | Supply metrics endpoint + responses | Render overlay, support manual refresh | Done | Extend into pause menu (Task-13A). |
| F-14 | Weather FX & Ambient Audio | Provide intensity/duration snapshots | Bind intensity to particle/audio presets | In Progress | Task-10E authoring outstanding. |
| F-15 | Combat Feedback & Damage Numbers | Emit combat event payloads with combat log context | Display damage popups & hit feedback | Planned | Task-15A: define combat event schema. |
| F-16 | Mob AI & Spawning Framework | Simulate mobs, pathing, spawn rules, server tick loop | Render mob proxies, animate, cull | Planned | Task-16A: prototype tick scheduler. |
| F-17 | World Persistence & Backup | Save world/chunk data, schedule backups | Handle save notifications, reload state | Planned | Task-17A: evaluate SQLite/world split. |
| F-18 | Block Lighting & Sky Light | Compute and propagate light levels | Apply lightmaps/shaders per chunk | Planned | Task-18A: analyse chunk mesh data. |
| F-19 | Death & Respawn Notifications | Broadcast death/respawn payloads, persist respawn anchors | Refresh remote avatars, HUD death feed, respawn UI | In Progress | Task-19B: Unity handlers; Task-19D server broadcast delivered this session. |
| F-20 | Server Analytics & Telemetry | Aggregate residency, performance, death metrics | Display telemetry in HUD overlays | Planned | Task-20A: extend status endpoint with death counters. |

## Sequential Work Notes
- Completed this session: server death broadcasts (Task-19D) now notify every active session alongside respawn payloads.
- Next actionable client step: bind `PlayerRespawnBroadcast` and `PlayerDeath` messages into the Unity networking bridge and HUD feed (Task-19B).
- Keep function implementations under 200 lines; split existing monoliths when touching related code.

