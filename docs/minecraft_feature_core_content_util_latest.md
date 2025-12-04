Current feature split and rollout order across server and Unity client. Updated 2025-12-27; see `docs/minecraft_feature_core_content_util_2025-12-27.md` for the snapshot. This iteration adds a water-table-aware hydrology clamp shared between WorldManager and MapGeneratorLib (driven by `HydrologyWaterTableClampWeight/Range` and `GlobalWaterLevel`) so caves/rivers/lakes bias toward the configured sea level, wires Unity previews to the same water level, and extends ProtocolValidator coverage to `WeatherUpdateBroadcast.change_timestamp`.

## Task-specific feature list (core/content/util)
- Core: world seed-driven chunk RNG and world map control in `GameServer/World/WorldManager.cs`, session/room routing + proto guards for chunk/time/weather/block/status messages via `Program.cs`, `SessionManager.cs`, and `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`.
- Content: terrain/hydrology (caves/rivers/lakes), dungeons/loot, ore/vegetation, and biome/weather sync across server (`WorldManager`, `WorldGenerationConfig`) and client previews (`Assets/MyAssets/Scripts/GameWorld/*`, `MapGeneratorLib`).
- Utility: data-driven JSON configs (`config/world.json`, `server-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`), protobuf pipeline (`proto/*.proto`, `Assets/Generated/Protobuf`, `SharedProtocol`), and validation/tooling (`scripts/verify_protobuf.ps1`).

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming/persistence; hydrology seam blend + edge stability + water-table clamp run before river/lake carving to keep seams continuous and aligned to configured sea level | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors seam blend + stability + water-table clamp and river smoothing so Unity previews match server chunks; sky/time/weather updates | Config: `config/world.json`, `Assets/.../WorldConfigData.json` mirror `GlobalWaterLevel`, `HydrologyWaterTableClampWeight/Range`, `HydrologyEdge*`, `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `HydrologyWarpFrequency`, `HydrologyWarpAmplitude`, `CaveSupport*`. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()` invoked in `Program.cs`; registry/descriptor/parser validation + assembly consistency guard now also checks `WeatherUpdateBroadcast.change_timestamp` alongside status/world/time/transfer descriptors | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with seam blend, edge stability, anisotropic river smoothing, moisture/flow-biased cave supports, plus water-table clamp driven by `GlobalWaterLevel` | MapGeneratorLib mirrors hydrology/cave/river/lake pipeline including seam blend + stability + water-table clamp so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp/tangent/flow-lock/stability/water-table, flow persistence, river noise/depth/smoothing, anisotropy weights) + `Caves.*` stability/support knobs shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | New JSON knobs: `HydrologyWaterTableClampWeight/Range`, `GlobalWaterLevel` mirrored into MapGeneratorLib, plus existing hydrology/cave/river tuning (`HydrologyEdge*`, `HydrologyWarp*`, `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `SupportHydrationBias`, `SupportFlowBias`). |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. Weather updates now validated for `change_timestamp` to catch stale DTOs affecting HUD timing. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + hydrology seam blend/stability + water-table clamp and proto validation (`ProtoRuntime.EnsureInitialized`, `ProtocolValidator.ValidateEnhancedContracts`) before accepting traffic (Chunk/World/Time/Weather/Status/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with seam stability, hydrology warp sampling, water-table bias, and moisture/flow-biased cave supports using shared JSON knobs.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology seam stability, and proto registry health.

## Current iteration scope
- [x] Water-table clamp shared between `WorldManager` and MapGeneratorLib biases hydrology/flow toward `GlobalWaterLevel` to stabilize rivers/lakes/caves at chunk seams.
- [x] MapGeneratorLib now respects the configured `GlobalWaterLevel` from Unity world config instead of a fixed constant, keeping previews aligned to server world map control.
- [x] ProtocolValidator now also asserts `WeatherUpdateBroadcast.change_timestamp` along with registry/descriptor/parser/assembly drift checks so weather HUD bindings fail fast when protobufs go stale.
- [x] Configs kept in sync: `config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, `WorldConfigFile`, and `WorldGenerationConfig` cover the hydrology/river/cave knobs in use (including water-table clamp).
- Next: surface a hydrology seam debug overlay in Unity and biome-tune tangent/flow-lock weights based on seam QA captures.
