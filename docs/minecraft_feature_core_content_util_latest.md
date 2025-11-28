# Minecraft Feature Core/Content/Util (current)

Categorizes Minecraft-critical capabilities across client and server. Each item maps to JSON data/config, protobuf packets, and code areas so rollout stays deterministic and testable.

## Core (authority, sync, map control)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| World map control & chunks | Seeded generation, chunk save/load, diff streaming, world time/weather broadcast, room-aware routing | Request chunks, apply deltas, render sky/time/weather, gracefully unload | `ChunkDataRequest/Response`, `ChunkUnloadNotification`, `TimeUpdate`, `WeatherChange`, `WorldInfo`; `server-config.json`, `config/world.json`, `Resources/TextAsset/GameWorld/WorldConfigData.json` |
| Session & movement | Auth, heartbeat, spawn/respawn, anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | `LoginRequest/Response`, `MovementUpdate`, `PositionUpdateBroadcast`, `PlayerRespawnBroadcast` |
| Block interaction & permissions | Placement/break validation, durability, ownership, rollback | Input → request, VFX/SFX, undo/feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange`; block tables in `config/blocks.json` |
| Room/instance routing | Room create/join/leave, per-room chunk routing | Room list UI, migration UX | `RoomEnter/Leave/List`; docs `docs/server-rooms-architecture.md`, `docs/ROOM_BASED_ARCHITECTURE.md` |
| Persistence & safety | Chunk/player saves, backups, metrics, auth tokens | Save notifications, UI hints | DB settings in `server-config.json`, WAL toggle; metrics protobuf in `ServerStatusRequest/Response` |
| Protocol registry (protobuf) | Validate EnhancedMinecraft descriptor/registry bindings before handlers run | Surface proto drift in tooling logs; refresh generated DTOs when proto changes | `proto/*.proto`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ChunkPayloadBuilder` guard, `ProtoRuntime.EnsureInitialized()` |

## Content (world, gameplay, entities)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | Heightmap + hydrology generation, caves, dungeons, river/lake carving with JSON thresholds | Render streamed chunks; MapGeneratorLib preview uses same knobs | World JSON (`Water.*`, `Caves.*`), Unity `WorldConfigData.json`; chunks carry block payloads |
| Biomes, weather, sky | Biome tagging, weather schedule, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherChange`, biome tables in JSON |
| Structures & loot | Placement rules, loot tables, persistence hooks | Render + interact, container UI | Chunk payloads, `ContainerOpen/Update`; loot tables JSON |
| Entities/AI/combat | Spawn rules, combat resolution, pathing, aggression | Render entities, client-side prediction, hit FX | `EntitySpawn/Update/Despawn`, `PlayerAttack`, `AIStateSyncBroadcast`; gameplay tuning JSON |
| Items, crafting, inventory | Recipe validation, authoritative inventory | UI drag/drop, recipe book | `InventoryUpdate`, `ItemUse/Drop/Pickup`; recipes JSON |

## Utility (data, tooling, operations)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `server-config.json`, `config/world.json`; expose to worldgen and networking | Mirror configs in `Resources/TextAsset/GameWorld/WorldConfigData.json` for previews/UI | Keep seeds, hydrology/cave toggles, erosion weights, and room/time limits in JSON |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON resources | Shared schemas across `config/*.json` and `Resources/...` |
| Metrics & observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD/overlays | `ServerStatusRequest/Response`, logs under `Recordings/` |
| Tooling & protobuf | Generate C# DTOs from `proto/*.proto`, validate registry coverage | Consume generated classes in client networking | `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`, `SharedProtocol/SharedProtocol.csproj` build; `ProtocolValidator.ValidateEnhancedContracts()` must pass before chunk streaming |

Hydrology gradient/seam tuning (`HydrologyShorePush`, `HydrologySlopePenalty`, `HydrologyFlowGain` plus the existing smooth iterations/blend and erosion weights) now lives alongside the water knobs in both `config/world.json` and `Resources/TextAsset/GameWorld/WorldConfigData.json`, keeping seam smoothing and shoreline stabilization identical between WorldManager and MapGeneratorLib previews. River frequency and base depth are likewise data-driven (`RiverNoiseScale`, `RiverDepth`) so server streams and Unity previews pick up the same meander spacing and channel depth without code changes.

## Sequenced rollout (server/client)
- [x] MapGeneratorLib now smooths hydrology/flow masks (config-driven iterations/blend) before rivers, lakes, and caves to match server seam handling.
- [x] Pond generation carves shallow basins with sand floors and clears air above waterline to better approximate lake rim behaviour in streamed chunks.
- [x] EnhancedMinecraft proto registry validation runs via `ChunkPayloadBuilder`/`ProtoRuntime`; keep generated classes aligned with `proto/*.proto` fingerprint before handler registration.
- [ ] Align client container/inventory EnhancedMinecraft bindings and update registry once DTOs are regenerated.
- [ ] Expand biome/entity content once river/lake/cave tuning stabilizes across server and Unity previews.

## Delivery order
1) Core authority + protocol validation (chunk/time/weather/block/room, auth/anti-cheat).  
2) Content/worldgen: caves, rivers, lakes, dungeons, biomes/entities/loot using JSON knobs.  
3) Utility: config/metrics/protobuf/tooling kept in lockstep so Unity previews and dedicated server stay in sync.  
