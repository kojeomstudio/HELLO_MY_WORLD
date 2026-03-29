## Core / Content / Util map (2025-12-19)
- Core: world map authority + chunk routing kept in sync with proto guards. Headwater-aware river smoothing and lake inflow validation run on the authoritative server (`GameServer/World/WorldManager.cs`) with the same JSON knobs mirrored in Unity/MapGeneratorLib. Chunk handlers now call `ProtoRuntime.EnsureInitialized()` + `ProtocolValidator.ValidateChunkContracts()` before parsing EnhancedMinecraft payloads to catch stale generated DTOs early.
- Content: terrain/hydrology (caves/rivers/lakes) share the new data-driven knobs `RiverHeadwaterStabilityWeight`, `LakeInflowBlendWeight`, and `MoistureRetentionWeight` so caves avoid over-carving in saturated columns, rivers smooth noisy headwaters, and lakes prefer inflow-aligned outlets. MapGeneratorLib mirrors the same logic for Unity previews.
- Util: data-driven configs live in `config/world.json` + `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, parsed by `WorldGenerationConfig` (server) and `WorldConfigFile` (Unity). Protobuf health is guarded by `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` and runtime bootstrap in `ProtoRuntime`.

### Rollup
| Category | What | Where | Data / Notes |
| --- | --- | --- | --- |
| Core | Chunk authority, hydrology seams, proto validation | `GameServer/World/WorldManager.cs`, `Generation/*`, `Handlers/MinecraftChunkHandler.cs`, `SharedProtocol/EnhancedMinecraft/*` | JSON: `GlobalWaterLevel`, hydrology edge/warp/water-table knobs; proto: `EnhancedMinecraftProtocol` registries validated via `ValidateChunkContracts()` & `ProtoRuntime.EnsureInitialized()`. |
| Content | Worldgen: caves/rivers/lakes with moisture-aware stability | Server + MapGeneratorLib pipelines (cave stability, headwater smoothing, lake inflow linking) | JSON knobs: `RiverHeadwaterStabilityWeight`, `LakeInflowBlendWeight`, `MoistureRetentionWeight`, existing `Hydrology*`, `River*`, `Caves*`. |
| Util | Data alignment + tooling | `config/world.json`, `Assets/.../WorldConfigData.json`, parsers in `WorldGenerationConfig` and `WorldConfigFile`; proto regeneration scripts in `scripts/generate_proto.*` | Keep configs/protos in lockstep; run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` + `dotnet build SharedProtocol/SharedProtocol.csproj`. |

### Execution order
1) Validate proto bindings and chunk registry (`ProtoRuntime.EnsureInitialized`, `ProtocolValidator.ValidateChunkContracts`) before accepting EnhancedMinecraft traffic.
2) Run hydrology + river headwater smoothing and lake inflow routing with the shared JSON knobs so server chunks and Unity previews stay aligned.
3) Apply cave stability with moisture retention plus data-driven configs; keep JSON/proto artifacts regenerated together.
