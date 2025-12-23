Core/content/utility feature map for Minecraft functionality (server + Unity client). Updated 2026-01-22 to capture the new flow-aligned hydrology smoothing and map-control enforcement planned for this iteration.

## Task-specific feature list (core/content/util)
- Core: map control (render/simulation distance, chunk residency) plus protocol/registry validation for chunk/world control packets.
- Content: terrain/hydrology (caves/rivers/lakes) with directional smoothing + divergence clamp so rivers/caves/lakes stay aligned at seams and under water-table pressure.
- Utility: JSON config parity (new hydrology directional knobs) and protobuf toolchain/validation.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`; chunk handler enforces render/simulation distance when trimming residency and serving chunks. | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `WorldArea.cs` consume `WorldMapControlProfile` to size dynamic subworld loader radius against render/simulation distance. | Config: `config/world.json` (`ChunkSize`, `RenderDistance`, `SimulationDistance`, hydrology directional knobs). Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoDiagnostics.AssertRegistryClean()` on startup; `MinecraftChunkHandler` validates chunk contracts and registry coverage before serving data. | Unity tooling logs proto drift; generated DTOs in `Assets/Generated/Protobuf`. | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology pipeline gains directional smoothing + divergence clamp; rivers/lakes/caves consume the stabilized fields for carving/sealing. | `MapGeneratorLib/.../WorldGenAlgorithms.cs` mirrors hydrology steps for Unity previews; `WorldAreaManager` wires the new knobs. | Config knobs: `Water.HydrologyDirectionalBlend`, `Water.HydrologyDirectionalIterations`, `Water.HydrologyFlowDivergenceClamp` plus existing `Hydrology*`, `River*`, `Caves.*`, `GlobalWaterLevel`. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | `config/world.json`, `WorldGenerationConfig`, `WorldMapControlProfile`; server loads directional smoothing knobs into runtime. | `Assets/.../WorldConfigData.json`, `WorldConfigFile`, `WorldAreaManager` set the same knobs into `WorldGenAlgorithms`. | Keep JSON/proto regeneration scripts (`scripts/generate_proto.*`, `scripts/sync_world_config.ps1`) in sync when fields change. |

## Sequenced implementation order
1) Core: enforce render/simulation distance through chunk residency and loader radius; validate enhanced protobuf registry on startup/handlers.
2) Content: apply hydrology directional smoothing + divergence clamp before rivers/lakes/caves on server and MapGeneratorLib for seam-safe previews.
3) Utility: mirror new JSON knobs across server/client configs and document the map-control + hydrology changes.
