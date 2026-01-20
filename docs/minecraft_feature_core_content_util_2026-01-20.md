## Minecraft core/content/util plan (2026-01-20)
- Captures the current Minecraft feature split across core (authority/protocol), content (worldgen/gameplay), and utility (config/tooling), aligned to the server + Unity client.

### Core (authority, world map control, protocol)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/Handlers/MinecraftChunkHandler.cs` clamps chunk requests to the map-control render distance, pulls chunk data via `WorldManager` | `WorldAreaManager` consumes `WorldMapControlProfile` for render/simulation distance and chunk cadence | JSON: `config/world.json` (`ChunkSize`, `RenderDistance`, `SimulationDistance`); Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`. |
| Hydrology seam tension (edge blending) | `WorldManager.EnforceHydrologyEdgeConsistency` now blends edge hydrology/flow toward downhill gradients before stabilization | `WorldGenAlgorithms.EnforceHydrologyEdgeConsistency` mirrors the gradient-aware tension so previews match streamed chunks | Uses existing knobs: `HydrologyEdgeBlendRadius`, `HydrologyEdgeVarianceClamp`, `HydrologyEdgeFlowLockWeight`, `HydrologyGradientWeight`. |
| Protocol/registry guards | `ProtoRuntime.EnsureInitialized()` + `ProtocolValidator.ValidateChunkContracts()` + `ValidateHandlerBindings` guard EnhancedMinecraft DTOs and handler contracts before chunk traffic | Unity tooling uses generated DTOs in `Assets/Generated/Protobuf`; bridge calls `ProtoRuntime` | Re-run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

### Content (terrain/worldgen, gameplay)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| River intensity edge feather | `WorldManager.SmoothRiverIntensity` now calls `FeatherRiverIntensityEdges` to keep river banks aligned across chunk seams | `WorldGenAlgorithms.SmoothRiverIntensity` mirrors the edge feather so Unity previews keep continuous rivers | Knobs: `HydrologyEdgeBlendRadius`, `HydrologyEdgeVarianceClamp`, `RiverIntensitySmoothIterations/RiverIntensitySmoothBlend`. |
| Riparian-aware noise caves | `GenerateNoiseCavePass` samples river pressure to suppress carving under active channels and bias flooding beneath rivers | `GenerateNoiseCaves` mirrors river-pressure suppression/flooding so tooling previews match streamed chunks | Knobs: `CaveRiverSuppressionWeight`, `RiverBankThreshold`, `GlobalWaterLevel`; data lives in `config/world.json` + Unity mirror. |
| Hydrology-guided caves/rivers/lakes | Shared hydrology/flow/gradient fields power cave stability, river smoothing, lake spawn masks; new pressure-balancing pass keeps inflow/outflow steady | MapGeneratorLib consumes the same fields for offline previews | JSON: `Hydrology*`, `River*`, `Lake*`, `Caves.*` in `config/world.json` and Unity `WorldConfigData.json` (incl. `HydrologyPressureBlend`, `HydrologyPressureGradientClamp`). |

### Utility (data, tooling, operations)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config + map-control parity | `WorldGenerationConfig` + `WorldMapControlProfile` load JSON; chunk handlers enforce render distance | `WorldConfigFile` + `WorldMapControlProfile` mirror the same fields for previews | Keep `config/world.json` and `Assets/.../WorldConfigData.json` in sync; version alongside protobuf regeneration. |
| Protobuf/tooling health | `scripts/generate_proto.ps1`, `scripts/verify_protobuf.ps1`; runtime registry validation | Unity relies on generated DTOs; MapTool/recordings consume EnhancedMinecraft payloads | Build `SharedProtocol` after regenerating; align proto + handler coverage logs. |

### Sequenced implementation order
1. Core: enforce map-control render distance on chunk requests; keep proto registry validation on boot.
2. Content: apply hydrology edge tension + river intensity feathering, and riparian-aware noise caves on both server and client previews.
3. Utility: maintain JSON/world-config parity and protobuf regeneration when protocol or world-control knobs change.
