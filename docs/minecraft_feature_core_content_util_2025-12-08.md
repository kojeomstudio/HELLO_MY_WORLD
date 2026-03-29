# Minecraft Feature Core/Content/Util (2025-12-08)

Categorizes Minecraft-critical client/server responsibilities by feature tier and ties them to data/protocol/config touchpoints. Use this list to keep Unity and the dedicated server aligned as we iterate.

## Core (authority, sync, map control)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| World map control & chunks | Terrain pipeline (hydrology normalization, chunk cache, seam stitching), chunk save/load, diff streaming, time/weather broadcast | Request chunks, apply deltas, render time/weather, unload gracefully | `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`; `config/world.json`, `Resources/TextAsset/GameWorld/WorldConfigData.json` |
| Session & movement safety | Auth/heartbeat, spawn/respawn, anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | `LoginRequest/Response`, `PlayerStateUpdate`, `PlayerRespawnBroadcast`; `server-config.json` session limits |
| Block interaction & permissions | Placement/break validation, durability, ownership, rollback | Input → request, VFX/SFX, undo/feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange`; block tables in `config/blocks.json` |
| Room/instance routing | Room create/join/leave, per-room chunk routing | Room list UI, migration UX | `RoomEnter/Leave/List`; `docs/server-rooms-architecture.md` |
| Protocol registry (protobuf) | Validate EnhancedMinecraft registry/descriptor bindings before handlers run | Surface proto drift; refresh generated DTOs when proto changes | `proto/*.proto`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ChunkPayloadBuilder`, `ProtoRuntime.EnsureInitialized()` |

## Content (world, gameplay, entities)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | Hydrology/flow normalization, seam blending, river/lake/cave carving with erosion-aware masks | Render streamed chunks; MapGeneratorLib preview uses same knobs | `Water.*`, `Caves.*` in `config/world.json` & Unity `WorldConfigData.json`; chunk payloads |
| Biomes, weather, sky | Biome tagging, weather scheduling, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherUpdateBroadcast`; biome tables JSON |
| Structures & loot | Placement rules, loot tables, persistence hooks | Render + interact, container UI | Chunk payloads, `ContainerOpen/Update`; loot tables JSON |
| Entities/AI/combat | Spawn rules, combat resolution, pathing | Render entities, client-side prediction, hit FX | `EntitySpawn/Update/Despawn`, `PlayerAttack`, `AIStateSyncBroadcast`; gameplay tuning JSON |
| Items, crafting, inventory | Recipe validation, authoritative inventory | UI drag/drop, recipe book | `InventoryUpdate`, `ItemUse/Drop/Pickup`; recipes JSON |

## Utility (data, tooling, operations)
| Feature | Server responsibilities | Client responsibilities | Data / Protocol / Config |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `server-config.json`, `config/world.json`; expose to worldgen/networking | Mirror knobs in Unity JSON for previews/UI | Seeds, hydrology/cave toggles, erosion weights, room/time limits |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON resources | Shared schemas across `config/*.json` and `Resources/...` |
| Metrics & observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD/overlays | `ServerStatusRequest/Response`, logs under `Recordings/` |
| Tooling & protobuf | Generate C# DTOs from `proto/*.proto`, validate registry coverage | Consume generated classes in client networking | `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`, `SharedProtocol/SharedProtocol.csproj` build |
| Build/test | CI or local `dotnet build SharedProtocol && dotnet build GameServer` plus Unity Test Runner | Surface errors in editor UI | Build logs in repo root |

## Delivery order
1) Core authority + protocol validation (chunk/time/weather/block/room, auth/anti-cheat).
2) Worldgen content: hydrology-normalized rivers/lakes/caves; biome/weather updates.
3) Utility: config/metrics/protobuf/tooling kept in lockstep so Unity previews and server stay in sync.

## Notes for the current cycle
- Hydrology/flow fields now normalize before seam blending in both `WorldManager` and `MapGeneratorLib`, reducing chunk-edge ripples and feeding caves/rivers/lakes the same erosion masks.
- Keep `config/world.json` and `Resources/TextAsset/GameWorld/WorldConfigData.json` synchronized when tuning `HydrologySmooth*`, `HydrologyShorePush/SlopePenalty/FlowGain`, `RiverNoiseScale/Depth`, and cave stability smoothing.
- Run `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj` after regenerating protobufs to ensure `ProtocolValidator.ValidateEnhancedContracts()` passes before chunk streaming.
