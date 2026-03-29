Current feature split and rollout order across server and Unity client. Updated 2025-12-03; adds hydrology edge flow-lock seams plus a duplicate-binding guard in the EnhancedMinecraft proto validator. See also `docs/minecraft_feature_core_content_util_latest.md` for the forward-looking pointer.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder`; hydrology seams now blend slope + tangent plus a flow-lock step (`HydrologyEdgeFlowLockWeight`) so rivers/lakes/cave moisture follow interior flow when crossing chunk borders | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors flow-lock/tangent/warp knobs so Unity previews match streamed chunks | Config: `config/world.json`, `Assets/.../WorldConfigData.json` mirror `HydrologyEdgeFlowLockWeight`, `HydrologyEdgeTangentWeight`, hydrology warp, river anisotropy, cave support knobs. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` validates registry coverage, descriptors, parsers; now rejects duplicate descriptor bindings so stale `using` references are caught before networking | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with flow-aligned seam smoothing/relaxation, edge tangent projection, and new flow-lock blending (`HydrologyEdgeFlowLockWeight`) to keep chunk seams on the same downstream flow | MapGeneratorLib mirrors hydrology/cave/river/lake masks with the same flow-lock/tangent/warp sampling so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp/tangent/flow-lock, flow persistence, river noise/depth/smoothing, anisotropy weights) + `Caves.*` stability/support knobs (`SupportDensity`, `SupportHydrationBias`, `SupportFlowBias`, stability weights) shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | New JSON knob `HydrologyEdgeFlowLockWeight` joins existing hydrology/cave/river tuning; Unity and server stay in lockstep. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability and duplicate descriptor bindings | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing with proto validation/duplicate-binding guard and flow-aware seam smoothing before accepting traffic (Chunk/World/Time/Weather/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with flow-lock + tangent seam anchoring, hydrology warp sampling, and moisture/flow-biased cave supports using shared JSON knobs.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology seam quality, and proto registry health.

## Current iteration scope
- [x] Added flow-lock seam anchoring for hydrology/flow masks in both `WorldManager` and MapGeneratorLib, driven by `HydrologyEdgeFlowLockWeight` to keep river/lake/cave moisture continuous across chunk borders.
- [x] Configs mirrored in `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, with Unity `WorldAreaManager` wiring the new knob into `WorldGenAlgorithms`.
- [x] ProtocolValidator now rejects duplicate descriptor bindings so stale EnhancedMinecraft `using` directives are caught before runtime.
- Next: expose a hydrology seam debug overlay in Unity and tune flow-lock weight per biome based on recorded chunk seam diagnostics.
