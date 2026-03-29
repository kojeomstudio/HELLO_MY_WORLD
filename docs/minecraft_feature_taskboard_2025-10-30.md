# Minecraft Feature Taskboard (2025-10-30)

This session rolls the Minecraft client/server plan forward, catalogues the open feature gaps, and breaks the work into bite-sized steps so the next pass can resume quickly. See `docs/minecraft_feature_execution.md` for the broader historical log; this sheet focuses on the actionable queue.

## Feature Coverage Snapshot
| ID | Server Focus | Client Focus | Status | Notes |
|----|--------------|--------------|--------|-------|
| F-01 | Authentication & session resume | Issue/validate tokens, manage heartbeats | Login UI, reconnect flow | ✅ Complete | Continue monitoring reconnect edge cases. |
| F-02 | Player movement sync | Authoritative transforms, clamp deltas | Predict, reconcile, smooth | ✅ Complete | Teleport safeguards live. |
| F-03 | Chunk streaming | Serve chunk payloads, cache residency | Request, dedupe, rebuild meshes | ✅ Complete | Track eviction telemetry. |
| F-04 | Block interaction broadcast | Apply block edits, drops | Update chunks, play FX | ✅ Complete | Particle polish queued. |
| F-05 | Item drop visibility | Persist drops, broadcast updates | Spawn pickup visuals, loot UI | ✅ Complete | None. |
| F-06 | Chunk residency tracking | Maintain per-session registry | Maintain loaded chunk set | ✅ Complete | Residency analytics in HUD. |
| F-07 | Residency eviction policy | TTL pruning, budget caps | Passive | ✅ Complete | Log eviction metrics. |
| F-08 | Client chunk unload signal | Accept unload messages | Emit unload events | ✅ Complete | Expand telemetry counters. |
| F-09 | Inventory snapshot persistence | Store JSON snapshots, diff | Consume diffs, refresh UI | ✅ Complete | Crafting integration live. |
| F-10 | World time & weather sync | Tick world time, stream weather | Light/FX/audio bindings | 🟠 In progress | Task-10E wiring presets. |
| F-11 | Remote entity sync | Broadcast spawn/update/despawn | Spawn avatars, smooth, pool | ✅ Complete | Monitor culling thresholds. |
| F-12 | Crafting & containers | Persist grids, hash validation | Container UI diff wiring | 🟠 In progress | Task-12D open/close hooks remain. |
| F-13 | Server status overlays | Status endpoint, metrics snapshot | HUD refresh, pause menu surfacing | 🟠 In progress | Task-13A pause overlay not started. |
| F-14 | Weather FX palette | Expose intensity curves | Bind presets to scenes | 🟠 In progress | Paired with Task-10E. |
| F-15 | Combat feedback polish | Emit combat events (done) | HUD feed (done), world popups, hit feedback | 🟠 In progress | Task-15C2 underway this session. |
| F-16 | Mob AI & spawning | Simulate mobs, pathing | Render proxies, animate | ⏳ Planned | Needs server tick scheduler. |
| F-17 | World persistence & backup | Save chunks/players, rotate backups | Respond to save notifications | ⏳ Planned | Evaluate SQLite/world split. |
| F-18 | Block & sky lighting | Compute light propagation | Apply lightmaps/shaders | ⏳ Planned | Await chunk mesh audit. |
| F-19 | Death & respawn loop | Broadcast death/respawn context | Death feed, respawn UI | 🟢 Mostly done | Pause menu analytics integration pending. |
| F-20 | Analytics & telemetry | Aggregate residency/death metrics | Display in HUD/pause overlays | 🟠 In progress | Pause menu surfacing tracked as Task-13A. |
| F-21 | Moderation pipeline | Persist mute/block, profanity filtering | UI to manage mute/block, filtered chat | ⏳ Planned | Requires server filtering rules. |
| F-22 | Player options persistence | Persist per-player settings/keybinds | Settings UI pushing deltas | ⏳ Planned | Blocked on options serialization. |

Legend: ✅ complete, 🟢 mostly complete, 🟠 in progress, ⏳ planned.

## Immediate Work Queue
1. **Task-15C follow-up** – Mirror critical strike cues on remote avatars and validate controller rumble coverage across common hardware.
2. **Task-12D** – Detect chest/furnace interactions in-world and call `ContainerManager.RequestOpen/Close` with optimistic UI updates.
3. **Task-13A** – Surface the server telemetry snapshot inside the pause menu overlay so analytics live outside the HUD ticker.
4. **Task-10E** – Author baseline weather ambient presets (clear/rain/storm/snow) and bind intensity to lighting and audio mixers.

## Delivered 2025-10-30
- Implemented `CombatHitFeedbackEffects` to add local hit pause, screen shake, and optional controller rumble for high-damage CombatEventMessage payloads.
- Auto-register the hit feedback component from `MinecraftGameManager` so the effect is active without manual scene wiring.
- Documented the updated feature coverage snapshot and queued the remaining combat feedback polish work.

## Follow-Up Parking Lot
- Mirror critical strike feedback on remote avatars (animation trigger or highlight).
- Extend analytics self-test via `dotnet run --project GameServer -- --selftest` to validate CombatEvent damage numbers for local/remote players.
- Document tactile feedback bindings in `docs/minecraft_feature_execution.md` once rumble rollout is verified on hardware.

## Hand-off Notes
- Keep new or refactored functions under 200 lines; split monoliths when touching related code paths.
- If rumble support is unavailable, guard code paths so older input systems still compile.
- Record doc updates alongside feature code and push to origin/master before starting the next task batch.
