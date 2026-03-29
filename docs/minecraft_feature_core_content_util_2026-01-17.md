# Minecraft Feature Categorization (2026-01-17)

- Source of truth: `config/minecraft_feature_client_server_core_content_util_2026-01-17.json`
- Order is sequential per category; priorities emphasize hydrology-aware worldgen and protocol validation for this session.

## Core
- **Server**
  - `world-map-control-parity` (order 1, in-progress) — share hydrology/cave tuning via `GameServer/World/WorldMapControlManager.cs`, `config/world_map_control_profile.json`.
  - `protocol-registry-validation` (order 2, planned) — guard EnhancedMinecraft DTO bindings in `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtocolValidator.cs`.
- **Client**
  - `world-map-preview-parity` (order 1, in-progress) — mirror server map-control (hydrology + cave smoothing) via `WorldMapController.cs`, `WorldMapControlProfile.cs`, `world-config.json`.
  - `protocol-client-bindings` (order 2, planned) — keep Unity network stack aligned with generated protobuf DTOs (`Assets/Generated/Protobuf`, `ProtobufNetworkClient.cs`).

## Content
- **Server**
  - `cave-river-lake-coherence` (order 1, in-progress) — hydrology-driven carving in `MapGeneratorLib/.../WorldGenAlgorithms.cs`, configs `enhanced_terrain_generation.json`, `enhanced-terrain-config.json`.
- **Client**
  - `map-visualization` (order 2, planned) — render stabilized rivers/lakes/caves per control profile (`WorldMapController.cs`, `WorldAreaManager.cs`, `EnhancedTerrainGenerator.cs`).

## Utility
- **Server**
  - `data-driven-config-refresh` (order 0, in-progress) — JSON-driven world/control loaders (`config/world.json`, `world_map_control_profile.json`) via `DataDrivenConfigManager`.
- **Client**
  - `streaming-profile-hotload` (order 0, in-progress) — streaming world-profile hot reload from `Assets/StreamingAssets/world-config.json` through `WorldMapController.cs` and `WorldConfig.cs`.

## Notes
- Keep implementations data-driven (JSON) on both sides.
- Worldgen tuning for caves/rivers/lakes must stay in sync between server and client control profiles and referenced by protobuf-driven map updates.
