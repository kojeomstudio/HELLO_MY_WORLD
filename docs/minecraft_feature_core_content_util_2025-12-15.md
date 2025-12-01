# Minecraft Core / Content / Utility (2025-12-15 snapshot)

Snapshot of the current feature split and rollout order across server and Unity client. Adds flow-aligned hydrology seam bias (`HydrologyEdgeFlowBias`) shared by server and MapGeneratorLib plus stricter EnhancedMinecraft protobuf assembly validation; mirrors `docs/minecraft_feature_core_content_util_latest.md`.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming + persistence; room-aware routing in `SessionManager.cs`; flow-aligned hydrology seam bias for chunk edges | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs` request/unload + apply payloads; MapGeneratorLib mirrors hydrology seam blending/bias; sky/time/weather updates | Config: `config/world.json`, `server-config.json`; Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (includes `HydrologyEdgeFlowBias`). Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, registry/descriptor/parser validation + assembly consistency guard | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with flow-aligned multi-ring seam smoothing/relaxation; generation stages in `Generation/Stages/*` | MapGeneratorLib mirrors hydrology/cave/river/lake masks for Unity preview with the same flow-aware seam smoothing; streamed chunks rendered by world scripts | Config knobs: `Water.*` (`HydrologySmooth*`, `HydrologyEdgeBlendRadius`, `HydrologyEdgeFlowBias`, `HydrologyFlowPersistence`, `HydrologySeamRelax*`, `RiverNoiseScale`, `RiverDepth`, erosion weights) + `Caves.*` stability/noise settings. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | Keep seeds/hydrology/cave toggles, seam smoothing, `HydrologyEdgeFlowBias`, erosion weights in JSON; document new keys. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + flow-aligned seam blending and proto validation (registry + parser + assembly) before accepting traffic (Chunk/World/Time/Weather/Unload messages).  
2) Content/worldgen: hydrology-driven caves/rivers/lakes with flow-biased seam smoothing + dungeon/biome passes using JSON knobs shared with Unity previews.  
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control and seam quality.  
