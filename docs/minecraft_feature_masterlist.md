# Minecraft Feature Master List

This master list enumerates the Minecraft-style gameplay features spanning both the .NET server and the Unity client. Keep statuses and next steps updated so future sessions can resume implementation without rediscovery.

| ID | Feature | Server Responsibilities | Client Responsibilities | Status | Next Steps |
|----|---------|-------------------------|-------------------------|--------|------------|
| F-01 | Authentication & Session | Validate credentials, issue tokens, track heartbeats | Present login UI, maintain session token | Done | Monitor reconnect edge cases |
| F-02 | Player Movement Sync | Authoritative movement validation, positional clamps | Predict & reconcile motion, surface transforms | Done | Teleport safeguards in place |
| F-03 | Chunk Streaming & Caching | Serve chunk payloads, cache residency per player | Request chunks, avoid duplicates, rebuild meshes | Done | Evaluate memcache eviction metrics |
| F-04 | Block Interaction Broadcast | Apply block changes, broadcast deltas & drops | Trigger actions, refresh local chunk data, play FX | Done | Add particle/audio polish |
| F-05 | Item Drop Visibility | Persist dropped items, include metadata in updates | Spawn pickup visuals, attach loot UI | Done | None |
| F-06 | Chunk Residency Tracking | Maintain loaded chunk registry per session | Maintain loaded set to avoid redundant fetches | Done | Residency analytics feed the status HUD (Task-13B delivered). |
| F-07 | Residency Eviction Policies | TTL pruning, budget caps, offline cleanup | Passive | Done | Periodic metrics logging |
| F-08 | Client Chunk Unload Signal | Accept unload requests, ack residency removal | Emit unload notifications when chunks despawn | Done | Expand telemetry counters |
| F-09 | Inventory Snapshot Persistence | Store JSON snapshots, diff on reconnect | Consume diffs, refresh hotbar/inventory UI | Done | Integrate crafting containers |
| F-10 | World Time & Weather Sync | Tick world time, schedule weather broadcasts | Update lighting, HUD, FX, ambient audio | Done | Author ambient presets (Task-10E) |
| F-11 | Remote Player Entity Sync | Broadcast spawn/update/despawn with velocity samples | Spawn avatars, smooth transforms, cull & pool | Done | Monitor culling thresholds and pooling hit rate |
| F-12 | Crafting & Container Persistence | Persist crafting grids, shared containers, recipes with hash validation, log hash mismatches | Present crafting UI, reconcile hash diffs | In progress | Task-12A delivered (snapshot hashes); Task-12B telemetry live; ContainerPanelUI scaffolding done; wire prefabs & interactions (Task-12C) |
| F-13 | Server Status HUD | Supply metrics endpoint & responses | Render overlay, support manual refresh | Done | Extend to pause menu (Task-13A) |
| F-14 | Weather FX & Ambient Audio | Provide intensity, weather types, durations | Bind intensity to particle/audio presets | In progress | Author preset assets (Task-10E) |
| F-15 | Combat Feedback & Damage Numbers | Emit combat events, damage payloads | Display damage popups & hit feedback | Planned | Define combat event schema |
| F-16 | Mob AI & Spawning Framework | Simulate mobs, pathing, spawn rules | Render mob proxies, animate, cull | Planned | Prototype server tick scheduler |
| F-17 | World Persistence & Backup | Save chunks/players to disk, schedule backups | Handle save notifications, reload state | Planned | Evaluate SQLite/world file split |
| F-18 | Block Lighting & Sky Light | Compute light levels per block, sync to client | Apply lightmaps/shaders per chunk | Planned | Analyse existing chunk mesh data |
| F-19 | Death & Respawn Notifications | Broadcast death/respawn payloads, persist spawn anchors, expose analytics | Refresh remote avatars, show death feed, manage respawn UI | In progress | Task-19A delivered respawn broadcast; Task-19D server death broadcast landed; Task-19B Unity wiring pending. |
| F-20 | Server Analytics & Telemetry | Aggregate residency, performance, and death metrics; expose status snapshots | Display telemetry in HUD overlays and pause menu | In progress | Task-20A delivered death/respawn counters; Task-20B will surface the data beyond the HUD ticker. |

## Sequenced Work Items
- [x] Task-11C ? Remote player distance culling and avatar pooling delivered.
- [x] Task-12A ? Deliver container snapshot hashes and diff validation handshake.
- [x] Task-12B ? Record container hash mismatch telemetry via diagnostics endpoint.
- [ ] Task-12C ? Bind container diff events into chest/furnace UI prefabs.
- [ ] Task-10E ? Provide default ambient presets & bindings for weather intensity.
- [ ] Task-13A ? Surface server metrics in the pause menu overlay.
- [x] Task-13B ? Capture chunk residency metrics for server observability.
- [x] Task-20A ? Extend ServerStatusResponse with death/respawn counters so the HUD analytics ticker can plot mortality spikes.


