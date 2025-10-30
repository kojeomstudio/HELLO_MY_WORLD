# Minecraft Feature Taskboard (2025-10-29)

This taskboard captures the Minecraft-style client/server features in flight, tracks what shipped during this session, and breaks the remaining work into small, sequential steps so the next pass can resume quickly.

## Delivered This Session
- Added world-space combat damage numbers on the Unity client via `Assets/Scripts/Minecraft/UI/CombatDamagePopupController.cs`, covering Task-15C1 and keeping combat feedback visible both in the HUD and in the world.

## Feature Inventory Snapshot
| ID | Server Focus | Client Focus | Current Status |
|----|--------------|--------------|----------------|
| F-12 | Maintain container open/close lifecycle hooks and persistence | Trigger `ContainerManager.RequestOpen/Close` from world interactions | In progress – Task-12D outstanding |
| F-14 | Weather intensity to particle/audio bindings exposed in world settings | Author ambient presets and bind intensity to scene lights & audio controllers | In progress – Task-10E outstanding |
| F-15 | Combat event schema & broadcast (complete) | HUD feed + world popups live; tactile feedback outstanding | In progress – Task-15C2 |
| F-20 | Extend analytics snapshots with telemetry fields | Surface analytics in pause menu and extended HUD widgets | In progress – Task-13A |
| F-21 | Mute/block list persistence, profanity filtering | Mute/block UI, filtered chat feed, moderation messaging | Planned |
| F-22 | Persist per-player options & keybind configuration | Settings UI pushes deltas to the server | Planned |

## Immediate Next Steps
1. **Task-15C2** – Apply hit pause, subtle screen shake, and controller rumble when the local player takes high-damage hits; ensure remote avatar animation feedback mirrors critical strikes.
2. **Task-13A** – Surface the enriched telemetry snapshot inside the pause menu overlay so analytics are visible beyond the HUD ticker.
3. **Task-12D** – Detect chest/furnace interactions in-world and call `ContainerManager.RequestOpen/Close` with optimistic UI updates.
4. **Task-10E** – Author baseline ambient presets (clear/rain/storm/snow) and bind weather intensity to scene lighting and audio mixers.

## Parking Lot / Follow-Up Notes
- Keep new or refactored functions under 200 lines; split existing monoliths when touching related code paths.
- After Task-15C2 lands, consider a lightweight self-test in `GameServer --selftest` that validates CombatEvent damage numbers for both local and remote recipients.
- Document tactile feedback bindings in `docs/minecraft_feature_execution.md` once the rumble/screen shake work ships.
