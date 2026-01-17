# Minecraft Feature Map (2026-01-17 Session 03)

Source data: `config/minecraft_feature_client_server_core_content_util_2026-01-17-session-03.json` (kept data-driven for reuse in tools and pipelines).

## Core
- **Server** — `world-map-control-signature` (planned, order 1): `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapController.cs`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `config/world_map_control_profile.json` — keep control profile hash/signature in lockstep with world/proto content and reuse terrain masks in cache-aware map streaming.
- **Server** — `protocol-registry-validation` (planned, order 2): `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `GameServer/Network/EnhancedProtocolHandler.cs` — verify generated EnhancedMinecraft DTOs + handler bindings at startup and flag gaps.
- **Client** — `world-map-preview-parity` (planned, order 1): `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/StreamingAssets/world-config.json` — mirror server hydrology/cave tuning with seam-safe smoothing and profile hash reloads.
- **Client** — `protocol-client-bindings` (planned, order 2): `Assets/MyAssets/Scripts/Network/GameNetworkManager.cs`, `Assets/Generated/Protobuf`, `Assets/Scripts/Minecraft/Network/ProtobufNetworkClient.cs` — keep client protobuf DTO references aligned with the SharedProtocol registry and guard stale assemblies.

## Content
- **Server** — `hydrology-coherent-caves` (in-progress, order 1): `GameServer/World/Generation/ImprovedCaveGenerator.cs`, `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs` — hydrology-driven carving with seam-aware smoothing, riparian plugs, and ceiling clamps to align subterranean water with rivers/basins.
- **Server** — `river-lake-outflow-alignment` (planned, order 2): `GameServer/World/Generation/ImprovedRiverGenerator.cs`, `GameServer/World/Generation/ImprovedLakeGenerator.cs`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs` — blend rivers into lake basins with flow accumulation carry-over and stitch hydrology envelopes across chunk seams.
- **Client** — `terrain-preview-masks` (planned, order 1): `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs` — preview renderer consumes shared mask metadata (hydrology/flow/river/lake) to display stitched terrain in editor/runtime.

## Utility
- **Server** — `data-driven-config-fingerprint` (in-progress, order 0): `GameServer/Configuration/DataDrivenConfigManager.cs`, `config/world.json`, `config/world_map_control_profile.json` — JSON-backed config loader with checksum logging feeding world map control + hydrology/lake/cave thresholds.
- **Client** — `streaming-profile-hotload` (in-progress, order 0): `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/StreamingAssets/world-config.json`, `Assets/Scripts/Minecraft/Core/WorldConfig.cs` — hot-reload map-control JSON and reset preview generator when hash/signature changes to keep data-driven parameters synced.
