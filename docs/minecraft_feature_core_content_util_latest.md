Current feature split and rollout order across server and Unity client. Updated 2025-12-24; adds a hydrology edge-consistency pass shared between server worldgen and MapGeneratorLib so rivers/lakes stay continuous across chunk seams, plus existing proto registry guards. See also `docs/minecraft_feature_core_content_util_2025-12-24.md` for the dated snapshot.

## Task-specific feature list (core/content/util)
- Core: world seed-driven chunk RNG and world map control in `GameServer/World/WorldManager.cs`, session/room routing + proto guards for chunk/time/weather/block messages via `Program.cs`, `SessionManager.cs`, and `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`.
- Content: terrain/hydrology (caves/rivers/lakes), dungeons/loot, ore/vegetation, and biome/weather sync across server (`WorldManager`, `WorldGenerationConfig`) and client previews (`Assets/MyAssets/Scripts/GameWorld/*`, `MapGeneratorLib`).
- Utility: data-driven JSON configs (`config/world.json`, `server-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`), protobuf pipeline (`proto/*.proto`, `Assets/Generated/Protobuf`, `SharedProtocol`), and validation tooling (`scripts/verify_protobuf.ps1`).

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming/persistence; hydrology edge-consistency smoothing now runs before river/lake carving to keep seam continuity | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors the edge-consistency + river smoothing knobs so Unity previews match server chunks; sky/time/weather updates | Config: `config/world.json`, `Assets/.../WorldConfigData.json` mirror `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `HydrologyEdgeTangentWeight`, `HydrologyEdgeFlowLockWeight`, `CaveSupport*`. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()` invoked in `Program.cs`, registry/descriptor/parser validation + assembly consistency guard; now also validates entity descriptors and rejects duplicate descriptor bindings so stale `using` references fail fast | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with edge-consistency smoothing prior to river/lake carving; river smoothing remains anisotropic (`RiverFlowAlignmentWeight`/`RiverGradientPenalty`); cave supports stay moisture/flow-biased (`SupportHydrationBias`/`SupportFlowBias`) | MapGeneratorLib mirrors hydrology/cave/river/lake pipeline including the edge-consistency pass so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp/tangent/flow-lock, flow persistence, river noise/depth/smoothing, anisotropy weights) + `Caves.*` stability/support knobs shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | New JSON knobs: `HydrologyEdgeFlowLockWeight`, `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `SupportHydrationBias`, `SupportFlowBias` plus existing hydrology/cave/river tuning. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + hydrology edge-consistency smoothing and proto validation (`ProtoRuntime.EnsureInitialized`) before accepting traffic (Chunk/World/Time/Weather/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with edge-consistency smoothing, hydrology warp sampling, and moisture/flow-biased cave supports using shared JSON knobs.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology quality, seam stability, and proto registry health.

## Current iteration scope
- [x] Edge-consistency smoothing shared across `WorldManager` and MapGeneratorLib so river/lake masks stay continuous at chunk seams.
- [x] Deterministic chunk RNG now mixes the world seed into caves/rivers/lakes/dungeons/ores/vegetation so server and client previews stay aligned per world id.
- [x] ProtocolValidator guards duplicate bindings and descriptor/parser/assembly drift so stale EnhancedMinecraft `using` directives fail fast before networking.
- [x] Configs kept in sync: `config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, `WorldConfigFile`, and `WorldGenerationConfig` cover the hydrology/river/cave knobs in use.
- Next: surface a hydrology seam debug overlay in Unity and biome-tune tangent/flow-lock weights based on seam QA captures.
