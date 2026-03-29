Terrain Generation Improvements — 2026-01-13
============================================

Scope
- Date: 2026-01-13
- Branch: master (`973edf61`)
- Related configs/docs: `config/minecraft_feature_client_server_core_content_util_2026-01-13-session.json`, `docs/minecraft_features_client_server_core_content_util_2026-01-13-session.md`

Server Updates
- Rivers: Added hydrology stability pass in `ImprovedRiverGenerator` that blends flow/wetness into channel pressure before smoothing to keep seams stitched across chunks.
- Lakes: Applied gradient stability + variance clamping in `ImprovedLakeGenerator`; penalizes steep rims and stabilizes basins before wetland/outflow passes.
- Caves: `ImprovedCaveGenerator` now seals wet ceilings near the water table using hydrology/flow masks to prevent thin riparian holes.
- Map control: `WorldMapController` tracks a generation signature and rebuilds the terrain pipeline on config/profile reloads or generation failures.

Client/Preview Updates
- MapGeneratorLib underground lakes are rehydrated/filled based on hydrology + flow, eliminating dry seams and stray pools on chunk edges.
- Cave ceilings near saturated terrain are sealed with riparian saturation/flow data so client previews match server carving near rivers/lakes.

Protocol/Validation
- `ProtocolRegistry.ValidateBindings()` now asserts the EnhancedMinecraft descriptor fingerprint up front to catch stale generated protobuf assets even outside full validator runs.

Notes
- JSON feature sequencing for this session is captured in `config/minecraft_feature_client_server_core_content_util_2026-01-13-session.json`.
- Use `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj` to verify protocol + worldgen changes.
