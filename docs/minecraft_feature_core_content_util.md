# Minecraft Feature Core/Content/Util Map

Snapshot 2025-11-24. The list aligns server and client responsibilities and the protobuf messages that keep them in sync. Categories follow the current Minecraft-style scope.

## Core (Simulation, Networking, Persistence)
| Area | Server ownership | Client side | Protocol / Data | Status |
| --- | --- | --- | --- | --- |
| World authority | Chunk generation, persistence, block updates, time/weather, seed + config broadcast | Request/stream chunks, apply block diffs, render time/weather | `ChunkLoadRequest/Response`, `ChunkUnloadNotification`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherInfo` | In progress (new worldgen config wiring) |
| Player lifecycle | Auth, session HB, spawn/respawn, death handling | Login UI, render remote players, death/respawn UX | `LoginRequest/Response`, `PlayerRespawnBroadcast`, `PlayerDeathBroadcast` | Mostly done |
| Movement + sync | Validation, anti-cheat, authoritative positions | Input, prediction/interp, remote smoothing | `MovementUpdate`, `PositionUpdateBroadcast` | Mostly done |
| Block interaction | Authoritative place/break, durability, persistence | Input -> request, VFX/SFX | `BlockChangeRequest`, `BlockChangeBroadcast` | Mostly done |

## Content (Gameplay, World Features)
| Area | Server ownership | Client side | Protocol / Data | Status |
| --- | --- | --- | --- | --- |
| Terrain features | Procedural terrain/caves/rivers/lakes, ores, vegetation | Visualize streamed chunks; optional local preview via MapGeneratorLib | Streamed chunk payloads | Updated (hydrology-configurable) |
| Structures & dungeons | Placement, loot tables, persistence | Render structures, handle interactions | Streamed chunk payloads, container messages | In progress |
| Biomes & weather | Biome tagging, weather schedule | Biome VFX/SFX, skybox/weather FX | `WorldInfo`, `WeatherInfo` | In progress |
| Entities & AI | Spawn rules, combat/responses | Render entities, client-side feedback | `AISpawn`, `EntitySync` messages | In progress |
| Items & crafting | Recipe validation, inventories, drops | UI, recipe browsing, drag/drop | `InventoryUpdate`, `CraftingRequest/Result`, container ops | In progress |

## Utility (Operational, Tools, Data)
| Area | Server ownership | Client side | Protocol / Data | Status |
| --- | --- | --- | --- | --- |
| Config & tuning | `server-config.json`, `config/world.json` drive worldgen, weather, limits | Consume synced config for visuals; resource JSON in `Resources/TextAsset` | Server pushes `WorldInfo`; shared JSON | Updated (worldgen section) |
| Metrics & logging | Server metrics, chunk residency, rate limits | HUD overlays for status/metrics | `ServerStatusRequest/Response` | In progress |
| Data-driven tables | JSON for blocks, recipes, world settings | Load from `Resources` JSON | Shared JSON assets | Ongoing maintenance |

## Recommended Implementation Order (server → client)
1) Core: enforce protobuf validation (`ProtoRuntime`) and world authority (chunk/time/weather).  
2) Content: tune worldgen hydrology (rivers/lakes/caves) from `config/world.json`, align chunk payloads.  
3) Utility: surface config + metrics to client UI, keep JSON tables in sync.
