# Minecraft Feature Breakdown (2026-01-15)

Source data: `config/minecraft_feature_client_server_core_content_util_2026-01-15.json`

## Core
- **Server**: `world-map-control-parity` (order 1, in-progress) — `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapControlProfile.cs`, `GameServer/World/WorldManager.cs`, `config/world_map_control_profile.json`
- **Server**: `hydrology-envelope` (order 2, in-progress) — `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `config/enhanced_terrain_generation.json`
- **Server**: `protocol-registry-validation` (order 3, planned) — `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Network/EnhancedProtocolHandler.cs`
- **Client**: `world-map-preview-parity` (order 1, in-progress) — `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`
- **Client**: `protocol-client-bindings` (order 3, planned) — `Assets/MyAssets/Scripts/Network/NetworkManager.cs`, `Assets/Generated/Protobuf`, `Assets/Scripts/Minecraft/Network/ProtobufNetworkClient.cs`

## Content
- **Server**: `cave-river-lake-coherence` (order 2, in-progress) — `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`
- **Client**: `map-visualization` (order 4, planned) — `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`

## Utility
- **Server**: `data-driven-config-refresh` (order 0, in-progress) — `GameServer/Configuration/DataDrivenConfigManager.cs`, `config/world.json`, `config/world_map_control_profile.json`
- **Client**: `streaming-profile-hotload` (order 0, in-progress) — `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/StreamingAssets/world-config.json`

## Sequencing Notes
- Orders reflect intended implementation flow per category (lower runs earlier). Core parity and hydrology envelope updates unblock content coherence and protocol validations.
- Keep JSON-driven data (`config/*`, `Assets/StreamingAssets/*`) as the single source; regenerate map-control profiles when hashes diverge between server and client.
