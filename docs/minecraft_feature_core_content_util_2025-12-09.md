# Minecraft Feature Core/Content/Util (2025-12-09)

Updated slice of Minecraft-critical server/client capabilities grouped by Core/Content/Utility with the current rollout order. Hydrology seam smoothing now uses the new edge/persistence knobs shared across server (`config/world.json`) and Unity (`Resources/TextAsset/GameWorld/WorldConfigData.json`).

## Core (authority, sync, map control)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| World map control & chunks | Seeded generation, chunk cache/diff streaming, seam-safe hydrology masks (`HydrologyEdgeBlendRadius`, `HydrologyFlowPersistence`), room-aware routing | Request chunks, apply deltas, unload gracefully, mirror hydrology knobs for previews | `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`; `config/world.json`, `Resources/TextAsset/GameWorld/WorldConfigData.json` |
| Session & movement | Auth/heartbeat, spawn/respawn, anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | `LoginRequest/Response`, `PlayerStateUpdate`, `PlayerRespawnBroadcast`; `server-config.json` |
| Block interaction & permissions | Placement/break validation, durability, ownership, rollback | Input → request, VFX/SFX, undo/feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange`; block tables in `config/blocks.json` |
| Room/instance routing | Room create/join/leave, per-room chunk routing | Room list UI, migration UX | `RoomEnter/Leave/List`; `docs/server-rooms-architecture.md`, `docs/ROOM_BASED_ARCHITECTURE.md` |
| Protocol registry (protobuf) | Validate EnhancedMinecraft descriptors/registry before handlers run | Surface proto drift; refresh generated DTOs when proto changes | `proto/*.proto`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, `ChunkPayloadBuilder` guard |

## Content (world, gameplay, entities)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | Heightmap + hydrology generation, edge-blend/persistence smoothing, caves/rivers/lakes carved with erosion-aware masks | Render streamed chunks; MapGeneratorLib preview uses the same hydrology settings | `Water.*`, `Caves.*`, `HydrologyEdgeBlendRadius`, `HydrologyFlowPersistence`, `RiverNoiseScale`, `RiverDepth` in JSON; chunk payloads |
| Biomes, weather, sky | Biome tagging, weather schedule, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherChange`, biome JSON tables |
| Structures & loot | Placement rules, loot tables, persistence hooks | Render + interact, container UI | Chunk payloads, `ContainerOpen/Update`; loot tables JSON |
| Entities/AI/combat | Spawn rules, combat resolution, pathing | Render entities, client-side prediction, hit FX | `EntitySpawn/Update/Despawn`, `PlayerAttack`, `AIStateSyncBroadcast`; gameplay tuning JSON |
| Items, crafting, inventory | Recipe validation, authoritative inventory | UI drag/drop, recipe book | `InventoryUpdate`, `ItemUse/Drop/Pickup`; recipes JSON |

## Utility (data, tooling, operations)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `server-config.json`, `config/world.json`; expose hydrology/cave/river/lake knobs to generators | Mirror knobs in Unity JSON for previews/UI | Seeds, `HydrologySmooth*`, `HydrologyEdgeBlendRadius`, `HydrologyFlowPersistence`, erosion weights, room/time limits |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON resources | Shared schemas across `config/*.json` and `Resources/...` |
| Metrics & observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD/overlays | `ServerStatusRequest/Response`, logs under `Recordings/` |
| Tooling & protobuf | Generate C# DTOs from `proto/*.proto`, validate registry coverage | Consume generated classes in client networking | `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`, `SharedProtocol/SharedProtocol.csproj` build; `ProtocolValidator.ValidateEnhancedContracts()` must pass before chunk streaming |

## Sequenced rollout (server/client)
- [x] Edge-aware hydrology seam blending and flow persistence feed rivers/lakes/caves in both `WorldManager` and `WorldGenAlgorithms` using JSON knobs.
- [x] Client `WorldAreaManager` now applies the same edge/persistence knobs before MapGeneratorLib previews, keeping streamed chunks aligned.
- [x] Protobuf registry/descriptors validated via `ProtoRuntime.EnsureInitialized()` and `ProtocolValidator.ValidateEnhancedContracts()` before chunk handlers run.
- [ ] Expand biome/entity/content beats after stabilizing the new hydrology smoothing across streamed chunks and Unity previews.

## Notes
- Keep `config/world.json` and `Resources/TextAsset/GameWorld/WorldConfigData.json` in lockstep when tuning hydrology/cave/river/lake numbers so server and Unity previews match.
- Run `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj` after regenerating protobufs to confirm the registry guard still passes.
