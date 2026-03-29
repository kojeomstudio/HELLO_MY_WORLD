# Terrain & Proto Updates - 2026-01-22

- Lake seepage normalization now runs on hydrology/flow masks before carving rivers, lakes, and caves on the server (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `EnhancedTerrainGenerationPipeline.cs`). Masks blend flow memory plus slope guards and re-normalize edges to keep wetlands and cave ceilings stable around lakes.
- Unity preview mirrors seepage-aware hydrology by boosting lake-edge moisture and smoothing masks (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`) with updated defaults (edge radius 4, flow memory 0.42, flow persistence 0.75, lake seepage 0.38).
- Map-control profile bumped to v4 with pipeline signature `2026-01-22-lake-seepage+proto-guard`; configs synced (`config/world.json`, `Assets/StreamingAssets/world-config.json`, regenerated `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`).
- Proto safety tightened: handler validation now fails fast if a registered message lacks a generated Google.Protobuf prototype (`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`), reinforcing using/import correctness.
- Data-driven knobs adjusted (hydration smoothing, edge stability, river mouth smoothing, lake basin smoothing) to reduce chunk seam artifacts and riparian flooding.

## Tests
- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameServer/GameServer.csproj`
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`
