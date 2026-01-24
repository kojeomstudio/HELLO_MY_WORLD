# Worldgen & Map-Control Update (2026-01-24)
**Scope:** Hydrology-aware terrain (caves/rivers/lakes) plus world-map control parity for session 14.  
**Pipeline Version:** `2026-01-24-water-table-envelope`

## Summary of Changes
- Added water-table envelope smoothing to terrain hydrology on all paths:
  - `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs` now dampens hydrology/flow near sea level and chunk edges before carving rivers, lakes, and caves.
  - Server pipeline (`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`) and coordinator (`ImprovedTerrainCoordinator.cs`) apply the same envelope so server masks stay in sync with Unity previews.
  - Unity preview controller (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`) mirrors the envelope and bumps the map-control pipeline version.
- World-map control signature bump to invalidate stale previews/caches after hydrology changes (server + client constants set to `2026-01-24-water-table-envelope`).
- Feature categorization refreshed for session 14 with core/content/util sequencing: `config/minecraft_feature_client_server_core_content_util_2026-01-24-session-14.json`.

## Feature Categorization Highlights (session 14)
- **Core:** Hydrology-terrain parity, world map control sync, protobuf network pipeline (client/server files listed in the JSON above).
- **Content:** Biome decoration/structures and water features tied to hydrology masks.
- **Util:** Data-driven configs and validation/instrumentation hooks for terrain/proto fingerprints.

## Proto & Validation
- Proto fingerprints remain enforced (`EnhancedProtoManifest.AssertFingerprint`, `ProtoFingerprint.AssertDescriptorFingerprint`) and are included in map-control generation signatures.
- No .proto schema changes this session; build warnings stem from `protobuf-net` version resolution (known, non-blocking).

## Tests Executed
- `dotnet build SharedProtocol/SharedProtocol.csproj` (warnings: NU1603 protobuf-net version resolution)
- `dotnet build GameServer/GameServer.csproj --no-restore` (warnings: NU1603 protobuf-net version resolution)
