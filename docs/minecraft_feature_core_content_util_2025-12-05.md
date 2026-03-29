# Minecraft Core/Content/Utility Rollout (2025-12-05)

Snapshot of the Minecraft features required on both client and server, organized by Core, Content, and Utility. Each row calls out where code lives and which data/protocol assets drive it so the rollout can proceed in order.

## Core (authority, world control, protocol)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| World/map control | `WorldManager` chunk pipeline, hydrology/cave/lake smoothing, room-aware routing | `WorldAreaManager`, chunk mesh bake, room/UI hooks | `config/world.json`, `WorldConfigData.json`, `ChunkLoadRequest/Response`, `WorldInfo`, `TimeUpdate`, `WeatherChange` | Hydrology gradient stabilization now mirrors MapGeneratorLib to keep rivers/lakes/caves aligned across chunk seams. |
| Player lifecycle & movement | Session auth/respawn, anti-cheat, position/tick broadcast | Prediction/interp, respawn UX | `LoginRequest/Response`, `PositionUpdateBroadcast`, `PlayerRespawnBroadcast` | Keep tick rates and caps in JSON config. |
| Block interaction & permissions | Validation, rollback, ownership | Input → request, VFX/SFX feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange` | Block/recipe tables remain JSON-driven. |
| Proto registry & validation | `ProtocolValidator.ValidateEnhancedContracts`, `ProtocolRegistry` binding checks | Unity startup uses same generated descriptors | `proto/*.proto`, generated C# in `Assets/Generated/Protobuf` | Now requires all registered EnhancedMinecraft messages (chunk/time/weather/entity/block/SFX/VFX) to be bound. |
| Room/instance routing | Room create/join/leave, chunk routing | Room list UI & migration UX | `RoomEnter/Leave/List` | Shards simulation and chat. |

## Content (world + gameplay)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Terrain & hydrology | `WorldManager` caves/rivers/lakes, MapGeneratorLib mirrors | Chunk render, preview via MapGeneratorLib | `config/world.json`, `WorldConfigData.json`, chunk payloads | Hydrology gradient stabilization, erosion-risk smoothing, and riparian masks kept in sync. |
| Biomes, weather, sky | Biome tagging, weather scheduler, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherChange`, `ParticleEffect` | Driven by JSON world config. |
| Structures & dungeons | Placement rules, loot persistence | Render + interact | Chunk payloads, container messages | Loot/structure tables in JSON. |
| Entities, AI, combat | Spawn rules, combat resolution | Render entities, hit FX | `EntitySpawn/Update/Despawn`, `PlayerAttack` | Damage rules stay data-driven. |
| Items, crafting, inventory | Recipe validation, inventory authority | UI, drag/drop, recipe book | `InventoryUpdate`, item use/drop/pickup messages | Recipes and items stay JSON-driven. |

## Utility (ops, data, tooling)
| Feature | Server | Client | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Config & tuning | `server-config.json`, `config/world.json` | `WorldConfigData.json` mirror | `WorldInfo` sync | JSON-first; keep seeds + thresholds aligned. |
| Metrics & observability | Chunk residency, tick/frame timing, rate limits | Dev HUD/overlays | `ServerStatusRequest/Response` | Log hydrology/cave tuning when worlds boot. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Loads from `Resources` JSON | Shared JSON schemas | Keep client/server schemas matched. |

## Execution order
1) Core world control and registry validation (chunk/time/weather/block/room) using updated `ProtocolValidator`.  
2) Content worldgen passes (rivers/lakes/caves/biomes/entities) with stabilized hydrology gradients shared between server and MapGeneratorLib.  
3) Utility surfaces (config mirrors, metrics, data-table audits) to keep tuning and observability consistent.
