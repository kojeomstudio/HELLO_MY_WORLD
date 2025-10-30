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
| F-12 | Crafting and container persistence | Done (snapshot hashes + telemetry) | In progress (UI diff wiring landed) | ContainerManager + panel consume diff broadcasts; follow-up Task-12D will trigger open/close from world interaction. |
| F-13 | Server status HUD | Done | Done | Overlay refreshes metrics automatically every 15 seconds. |
| F-14 | Weather FX and ambient audio | In progress | In progress | Weather controller routes intensity to particles and audio; asset wiring remains. |
| F-15 | Combat feedback and damage numbers | In progress (combat event schema + broadcast shipped) | In progress (HUD damage feed + world popups live; remote cues pending) | Combat events now drive HUD feed, world popups, and local hit pause; remote avatar critical feedback still outstanding. |
| F-16 | Mob AI & spawning framework | Planned | Planned | Needs server mob simulation, pathing, and client proxy actors. |
| F-17 | World persistence & backup automation | Planned | Planned | Needs incremental world saves, rotation policies, and client save notifications. |
| F-18 | Block lighting & sky light propagation | Planned | Planned | Waiting on chunk mesh analysis to carry block light values. |
| F-19 | Death & respawn notifications | Done (respawn + death broadcasts live) | Done (HUD feed + remote respawn sync) | Remote avatars snap to respawn points while death analytics remain surfaced in HUD. |
| F-20 | Server analytics & telemetry | In progress (death/respawn counters live) | Planned (HUD overlays beyond status panel) | Server status snapshot now exposes death analytics; UI needs pause-menu surfacing. |

## Active Task Queue (Oct 2025)
- [x] Task-12B - Instrument container hash mismatches and expose the counter via ServerStatusResponse.
- [x] Task-12C - Bind container diff events into chest/furnace UI prefabs with optimistic updates.
- [ ] Task-12D - Detect container interactions in-world and invoke ContainerManager.RequestOpen/Close.
- [ ] Task-10E - Author ambient presets and bind weather intensity to scene lights/sounds.
- [ ] Task-13A - Surface server metrics in the pause menu overlay.
- [x] Task-13B - Capture chunk residency metrics for server observability.
- [x] Task-19A - Broadcast PlayerRespawn messages to active sessions.
- [x] Task-20A - Extend ServerStatusResponse with death/respawn counters so the Unity HUD can chart analytics spikes (delivered 2025-10-25).
- [x] Task-15A - Add the CombatEvent broadcast to HealthAndHungerSystem.
- [x] Task-15B - Wire CombatFeedbackUI to the new CombatEvent payload for HUD damage numbers.
- [ ] Task-15C - Mirror critical strike cues on remote avatars and broaden controller rumble (local hit pause + screen shake shipped 2025-10-30).

## Recently Completed
- Container diff broadcasts now hydrate the ContainerPanelUI via ContainerManager so Unity reflects server slot deltas immediately.
- Server now broadcasts `PlayerDeathMessage` payloads alongside respawn events so peers and the originating player receive HUD-ready death context.
- Server status requests now include death/respawn counters and the Unity HUD overlays the running totals for analytics sampling.
- Container hash mismatch telemetry now feeds the diagnostics endpoint so the HUD can display snapshot correction counts.
- Server status requests now return chunk residency counters so the HUD can track total and peak residency.
- Remote player distance culling and avatar pooling landed, keeping remote entities lightweight and out of view when far away.
- Unity inventory snapshot diff consumer kept hotbar in sync with server reconnections.
- Server chunk residency eviction now enforces TTL and budgets without leaks.
- Time and weather broadcasts now update Unity lighting, HUD, and FX controllers.
- Entity sync groundwork landed: server now emits spawn/update/despawn messages while the client spawns and smooths remote player avatars.
- Player respawn broadcasts now reach all connected sessions, unblocking client-side death feed work.
- Unity remote avatars now retarget to respawn locations as soon as PlayerRespawn broadcasts arrive, keeping entity state and the death feed aligned.

## Session Task Board (2025-10-26)
1. ✅ Task-15A – Define and emit `CombatEventMessage` packets from `HealthAndHungerSystem`.
2. ✅ Task-15B – Surface the new payload inside the Unity HUD via `CombatFeedbackUI`.
3. ✅ Task-19B – Sync PlayerRespawn broadcasts with remote avatar states and refresh the death feed message.
4. ⏭ Task-15C – Spawn world-space damage popups, hook screen shake, and add optional controller rumble. Blocked on prefabs/VFX but tracked for the next session.
