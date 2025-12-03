Current feature split and rollout order across server and Unity client. Updated 2025-12-23; adds hydrology edge-tangent projection plus entity descriptor validation so rivers/lakes/cave moisture and proto bindings stay aligned across server chunks and Unity previews. See also `docs/minecraft_feature_core_content_util_2025-12-23.md` for the dated snapshot.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming/persistence; river intensity smoothing uses flow-aware/gradient-penalized passes and new hydrology edge-tangent projection (`HydrologyEdgeTangentWeight`) before shipment | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors river smoothing knobs + hydrology warp/tangent sampling so Unity previews match server chunks; sky/time/weather updates | Config: `config/world.json`, `Assets/.../WorldConfigData.json` mirror `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `HydrologyEdgeTangentWeight`, `CaveSupport*`. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()` invoked in `Program.cs`, registry/descriptor/parser validation + assembly consistency guard; now also validates `EntityData/EntitySpawnBroadcast/EntityDespawnBroadcast` descriptors | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with flow-aligned seam smoothing/relaxation and edge-tangent projection; river smoothing remains anisotropic (`RiverFlowAlignmentWeight`/`RiverGradientPenalty`), cave supports are moisture/flow-biased (`SupportHydrationBias`/`SupportFlowBias`), hydrology warp keeps humidity sampling coherent across seams | MapGeneratorLib mirrors hydrology/cave/river/lake masks with the same anisotropic river smoothing, hydrology warp/tangent sampling, and moisture-biased support columns so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp/tangent, flow persistence, river noise/depth/smoothing, anisotropy weights) + `Caves.*` stability/support knobs (`SupportDensity`, `SupportHydrationBias`, `SupportFlowBias`, stability weights) shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | New JSON knobs: `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `SupportHydrationBias`, `SupportFlowBias` plus existing hydrology/cave/river tuning. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + hydrology seam smoothing with anisotropic river blending and proto validation (`ProtoRuntime.EnsureInitialized`) before accepting traffic (Chunk/World/Time/Weather/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with flow-aware smoothing, hydrology warp sampling, and moisture/flow-biased cave supports using shared JSON knobs.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology quality, seam stability, and proto registry health.

## Current iteration scope
- [x] Edge-tangent hydrology projection shared across `WorldManager` and MapGeneratorLib, tuned by `HydrologyEdgeTangentWeight` to keep rivers/lakes/cave moisture continuous across chunk seams.
- [x] ProtocolValidator now checks entity descriptors (`EntityData`, `EntitySpawnBroadcast`, `EntityDespawnBroadcast`) alongside chunk/world/time/weather contracts so stale generated bindings fail fast.
- [x] Configs kept in sync: `config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, `WorldConfigFile`, and `WorldGenerationConfig` parse the new tangent weight alongside existing hydrology/river/cave knobs.
- Next: surface a lightweight hydrology/river anisotropy debug overlay in Unity while tuning the new weights per biome.
