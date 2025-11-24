# Minecraft Feature Core/Content/Util Map

Snapshot 2025-12-03. Categorizes must-have Minecraft-style features and the cross-cutting data/protocol wiring that keeps server and client aligned.

## Core (authority, sync, persistence)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Chunk & world control | Seeded generation, chunk save/load, block diffs, time/weather broadcast | Request chunks, apply deltas, render time/weather | `ChunkDataRequest/Response`, `ChunkUnloadNotification`, `TimeUpdate`, `WeatherChange`, `WorldInfo` | Driven by `server-config.json` + `config/world.json`; client mirrors in `Resources/TextAsset/GameWorld/WorldConfigData.json`. |
| Player lifecycle & movement | Auth, session HB, spawn/respawn, death, anti-cheat | Prediction/interp, death/respawn UX | `LoginRequest/Response`, `PlayerRespawnBroadcast`, `MovementUpdate`, `PositionUpdateBroadcast` | Authority stays server-side; rate limits in config. |
| Block interaction & permissions | Place/break validation, durability, rollback, ownership | Input -> request, VFX/SFX, UI feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange` | Uses data-driven block tables (`config/blocks.json`). |
| Room/instance routing | Room creation/join/leave, chunk routing per room | Room list UI, migration UX | `RoomEnter/Leave/List` | Keeps simulation sharded and predictable. |

## Content (world + gameplay)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Terrain features | Procedural height, caves, rivers, lakes, aquifers, ore veins, vegetation | Render streamed chunks; optional local preview via MapGeneratorLib | Chunk payloads (blocks + biome tags) | Hydrology/cave tuning sourced from `world.json`/`WorldConfigData.json`. |
| Biomes, weather, sky | Biome tagging, weather schedule, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherChange`, `ParticleEffect` | Biome table stays JSON-driven. |
| Structures & dungeons | Placement rules, loot tables, persistence | Render + interact; container UI | Chunk payloads, `ContainerOpen/Update` | Loot/structure tables in JSON. |
| Entities, AI, combat | Spawn rules, combat resolution | Render entities, local hit FX | `EntitySpawn/Update/Despawn`, `PlayerAttack`, `AISpawn`, `AIStateSyncBroadcast` | Damage rules align with gameplay config JSON. |
| Items, crafting, inventory | Recipe validation, inventory authority | UI, drag/drop, recipe book | `InventoryUpdate`, `ItemUse/Drop/Pickup`, crafting messages | Recipes remain data-driven. |

## Utility (ops, data, tooling)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Config & tuning | `server-config.json`, `config/world.json` (hydrology/cave/lake tuning, world seeds) | `Resources/TextAsset/GameWorld/WorldConfigData.json` mirror for preview/UI | `WorldInfo` syncs high-level knobs | JSON-first; keep seeds + thresholds aligned. |
| Metrics & observability | Chunk residency, tick/frame timing, rate limits | Dev HUD/overlays | `ServerStatusRequest/Response` | Used during perf passes. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Loads from `Resources` JSON | Shared JSON | Keep schema mirrored between server/client. |

## Delivery order
1) Core authority + protocol validation (chunk/time/weather/block/room).  
2) Content: worldgen (caves/rivers/lakes/biomes/structures) and combat/entities driven by JSON.  
3) Utility: config sync surfaces + metrics dashboards, keep data tables mirrored.
