# Minecraft Feature Map (2026-01-18)

Source data: `config/minecraft_feature_client_server_core_content_util_2026-01-18.json` (kept data-driven for reuse in tools and pipelines).

## Core
- **Server (in-progress)** `world-map-control-parity`: `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapControlProfile.cs`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `config/world_map_control_profile.json` — keep control profile hash/signature aligned with hydrology/cave tuning and protobuf fingerprints; refresh pipeline version and cache invalidation rules for streamed map tiles.
- **Server (in-progress)** `protocol-registry-validation`: `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `GameServer/Network/EnhancedProtocolHandler.cs` — ensure generated EnhancedMinecraft DTOs and handler bindings validate at startup; tie world-map signatures to the descriptor fingerprint to catch stale assets.
- **Client (in-progress)** `world-map-preview-parity`: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`, `Assets/StreamingAssets/world-config.json` — Unity previews mirror server hydrology/cave parameters and reload when profile hash, config, or proto fingerprint drifts.
- **Client (planned)** `protocol-client-bindings`: `Assets/MyAssets/Scripts/Network/GameNetworkManager.cs`, `Assets/Scripts/Minecraft/Core/EnhancedProtoManifest.cs`, `Assets/Generated/Protobuf` — keep client protobuf DTO references aligned with the shared registry and descriptor fingerprint.

## Content
- **Server (in-progress)** `cave-river-lake-continuity`: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `ImprovedCaveGenerator.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs` — hydrology-driven carving with edge continuity envelopes, seam-aware rivers/lakes, and ceiling sealing to keep subterranean water aligned with rivers and basins.
- **Client (planned)** `map-visualization`: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/MyAssets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs` — preview renderer consumes hydrology/flow/river/lake masks with seam-safe smoothing to display stitched terrain.

## Utility
- **Server (in-progress)** `data-driven-config-refresh`: `GameServer/Configuration/DataDrivenConfigManager.cs`, `config/world.json`, `config/world_map_control_profile.json` — JSON-backed config loader with checksum logging feeding map-control and hydrology/lake/cave thresholds.
- **Client (in-progress)** `streaming-profile-hotload`: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/StreamingAssets/world-config.json`, `Assets/Scripts/Minecraft/Core/WorldConfig.cs` — hot-reloads map-control JSON and resets preview generator when hash or signature changes to keep data-driven parameters synced.
