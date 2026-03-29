Current feature split and rollout order across server and Unity client. Updated 2025-12-04; river intensity normalization now weights hydrology continuity/flow persistence on both server and MapGeneratorLib, surface lake spawning penalizes erosion risk, and Unity map previews consume the same cave stability weights (hydrology/flow/roughness/depth) as the server. See `docs/world-generation.md` for algorithm notes.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` stream/persist; river intensity normalization now scales with hydrology continuity + flow persistence before anisotropic smoothing | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors hydrology warp/edge/tangent/flow-lock sampling and the new normalization weights so Unity previews match server chunks | Config: `config/world.json`, `Assets/.../WorldConfigData.json` keep `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyContinuityWeight`, `HydrologyFlowPersistence`, `HydrologyWarp*`, `HydrologyEdge*`, `CaveSupport*`. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()` in `Program.cs`, registry/descriptor/parser validation + duplicate binding guard | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion; river intensity normalization blends hydrology continuity + flow persistence, lake spawn weight penalizes erosion risk, cave stability weights use data-driven hydrology/flow/roughness/depth and water-table bias | MapGeneratorLib mirrors hydrology/cave/river/lake masks with the same normalization weights, erosion-aware lake heatmap, and JSON-driven cave stability (hydrology/flow/roughness/depth + suppression) so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp/tangent/flow-lock, flow persistence, river noise/depth/smoothing, anisotropy weights) + `Caves.*` stability/support knobs (`SupportDensity`, `SupportHydrationBias`, `SupportFlowBias`, `HydrologyStabilityWeight`, `FlowStabilityWeight`, `RoughnessStabilityWeight`, `RiverSuppressionWeight`) shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json`; WorldAreaManager now pushes cave stability weights to MapGeneratorLib | JSON knobs: `HydrologyEdgeFlowLockWeight`, `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `HydrologyContinuityWeight`, `HydrologyFlowPersistence`, `SupportHydrationBias`, `SupportFlowBias`, cave stability weights. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Keep schemas aligned across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + hydrology seam smoothing, hydrology continuity/persistence-weighted river normalization, proto validation (Chunk/World/Time/Weather/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with erosion-penalized lake spawn heatmap and data-driven cave stability weights (hydrology/flow/roughness/depth + suppression) shared by MapGeneratorLib and WorldManager.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology quality, seam stability, and proto registry health.

## Current iteration scope
- [x] Hydrology continuity + flow persistence now weight river intensity normalization in both WorldManager and MapGeneratorLib before anisotropic smoothing.
- [x] Lake candidate scoring penalizes erosion risk and shrinks basin radius/depth under unstable terrain on both server and Unity previews.
- [x] Unity MapGeneratorLib consumes the same cave stability weights as the server (hydrology/flow/roughness/depth with suppression) via `WorldAreaManager` and `WorldConfigData.json`.
- Next: surface a hydrology seam debug overlay in Unity and biome-tune flow-lock/tangent weights based on seam QA captures.
