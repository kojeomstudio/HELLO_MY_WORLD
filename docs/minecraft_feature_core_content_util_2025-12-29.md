Core/content/utility feature map for Minecraft functionality (server + Unity client). Updated 2025-12-29 after water-table-aware flow accumulation; protobuf registry/descriptor validation audited for enhanced contracts.

## Task-specific feature list (core/content/util)
- Core: world map control + chunk streaming, protocol/registry validation (including ServerStatus), auth/session/room routing.
- Content: terrain/hydrology (caves/rivers/lakes) with data-driven tuning, biomes/weather sync, structures/loot/entities.
- Utility: JSON config parity, protobuf toolchain/validation, metrics + data-driven tables.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `WorldGenerationConfig`, `Generation/*` build cached hydrology/flow/erosion fields then carve terrain/rivers/lakes/caves; flow accumulation now biases toward `GlobalWaterLevel` with slope/edge weighting before seam blending. Chunk payloads stream via `ChunkPayloadBuilder`. | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors hydrology + flow accumulation (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`) so Unity previews match streamed chunks. | Config parity: `config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` mirror hydrology/river/cave knobs (`Hydrology*`, `River*`, `Caves.*`, `GlobalWaterLevel`). Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` enforces descriptor/registry/assembly bindings for chunk/time/weather/entity/block/action/world control contracts; `ProtoRuntime.EnsureInitialized()` runs at boot. | Unity tooling logs proto drift; generated DTOs live in `Assets/Generated/Protobuf`. | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Session/auth/movement/rooms | Auth/heartbeat/spawn/respawn + anti-cheat in `Handlers/` and routing in `SessionManager`/`RoomManager`; room chunk broadcasts in `WorldManager`. | Lobby UI + prediction/interp; room list/migration prompts. | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`, room messages. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` builds hydrology/flow/erosion caches, runs water-table-aware flow accumulation, seam blending/relaxation, cave stability/supports, river/lake carving. | MapGeneratorLib mirrors the same passes (hydrology/flow smoothing, water-table clamp, edge variance/stability, cave/riparian helpers) so Unity previews align with streamed chunks. | Config knobs: `Water.*` (`HydrologySmooth*`, `HydrologyFlowPersistence`, `HydrologyEdge*`, `HydrologyWaterTableClampWeight/Range/SlopeWeight`, `River*`, `Lake*`) and `Caves.*` stability/support weights. |
| Biomes/weather/sky | `WorldTimeSystem`, `WeatherSystem`, biome tagging in `WorldManager`. | Sky/weather FX, biome VFX/SFX. | Proto: `WorldInfo`, `WeatherChange`, `TimeUpdate`. |
| Structures/loot/entities | Dungeons/containers (`DungeonGenerationStage`, container handlers); entity spawn/update/despawn handlers. | Container/loot UI, entity render/prediction. | Proto: `ContainerOpen/Update`, `EntitySpawn/Update/Despawn`, `PlayerAttack`. Data: loot/recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | `config/world.json`, `server-config.json` feed `WorldGenerationConfig`; same knobs mirrored into Unity JSON for previews/UI. | `Assets/.../WorldConfigData.json` consumed by MapGeneratorLib + UI. | Keys cover hydrology/river/cave support weights (`Hydrology*`, `River*`, `Caves.*`, `GlobalWaterLevel`, `HydrologyWaterTableClampRange/SlopeWeight`). |
| Protobuf pipeline & validation | Proto sources in `proto/*.proto`; generated C# in `SharedProtocol` + Unity. `ProtocolValidator` enforces registry/descriptor coverage (now including ServerStatus) and parser/assembly binding checks. | Unity consumes generated DTOs and logs drift. | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Metrics/analytics & data tables | Server status / chunk residency metrics exposed via status proto; data-driven tables for blocks/recipes/mobs/worldgen tuning remain JSON-driven. | HUD overlays consume status + world data. | Ensure JSON schemas stay aligned across `config/*.json` and Unity resources. |

## Sequenced implementation order
1) Core/authority: keep proto registry healthy (action/world/time/weather/status/transfer contracts) and chunk routing/hydrology cache building before accepting traffic.
2) Content/worldgen: run updated water-table-aware flow accumulation + seam-safe hydrology smoothing for rivers/lakes/caves in both WorldManager and MapGeneratorLib so previews == streamed chunks.
3) Utility/tooling: keep JSON knobs in lockstep, regenerate protobufs when `.proto` changes, and monitor status metrics through the validated ServerStatus descriptors.
