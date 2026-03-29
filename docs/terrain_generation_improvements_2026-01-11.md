# Terrain Generation & Map Control Improvements (2026-01-11)

## Summary
- Added a hydrology/flow continuity envelope shared by server (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`) and Unity preview (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`) to reduce chunk seams and keep caves/rivers/lakes aligned across platforms.
- Tuned river and lake masks with floodplain/variance assists and seam-relax blending to stabilize deltas, wetlands, and outflow channels.
- Tightened cave stability with flow variance sampling and gradient-aware thresholds to avoid flooded ceilings and ragged entrances.
- Expanded world-map generation signatures on both server/client to include hydrology variance, edge flow locks, seam relax, and cave/lake stability knobs so profile drift is detected immediately.
- Cleaned world-map control configs (`config/enhanced_world_map_control_server.json`, `config/enhanced_world_map_control_client.json`) to ensure JSON hygiene for data-driven loading.
- Hardened protobuf registry validation to require initialized descriptors and parsers, catching stale/missing generated DTOs before handlers register.

## File Touchpoints
- Server worldgen: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `ImprovedCaveGenerator.cs`
- Client preview: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs` (river/lake/cave masks, hydrology envelope, signatures)
- Map control: `GameServer/World/WorldMapControlManager.cs` (signature), `Assets/.../WorldMapController.cs` (signature)
- Config/data: `config/enhanced_world_map_control_server.json`, `config/enhanced_world_map_control_client.json`, `config/minecraft_feature_client_server_core_content_util_2026-01-11-hydrology-sync.json`
- Proto guard: `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`
- Documentation: `docs/minecraft_features_client_server_core_content_util_2026-01-11.md`

## Behavioral Notes
- Hydrology envelope blends flow memory, edge stability, and directional downhill sampling to smooth masks before river/lake/cave passes, reducing edge tearing.
- Rivers now account for variance-driven floodplains; lakes factor seam relax and variance assists to avoid harsh basins near chunk borders.
- Cave thresholds incorporate flow variance/gradients and moisture clamps, reducing ceiling leaks and stabilizing entrances.
- Generation signatures now reflect hydrology variance/edge locking/seam relax plus cave/lake stability so Unity regenerates previews when knobs move.
- Protobuf validation fails fast if the enhanced reflection descriptor or per-message parser is missing, keeping `using EnhancedMinecraftProtocol` references honest after protoc runs.

## Test Plan
- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameServer/GameServer.csproj`
- `dotnet run --project GameServer -- --generate-map-profile` (refresh map-control profile/signature)
- Manual smoke: Unity preview chunks (WorldMapController) should reload when profile hash/signature changes; no JSON parse errors from enhanced world-map configs.
