# Minecraft Feature Split — 2026-01-14

Tracked features grouped by Core / Content / Utility for both server and client. Paths reference current code so data-driven configs and map-control profiles stay aligned with worldgen and protocol behavior.

## Core
- **Server**
  - World map control cohesion — `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapControlProfile.cs`, `GameServer/World/WorldManager.cs` (sync streamed masks + profile hashes).
  - Terrain hydrology envelope — `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `GameServer/World/Generation/ImprovedRiverGenerator.cs`, `GameServer/World/Generation/ImprovedLakeGenerator.cs`, `GameServer/World/Generation/EnhancedCaveGenerator.cs` (shared hydrology for rivers/lakes/caves).
  - Enhanced protobuf registry — `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `GameServer/Network/EnhancedProtocolHandler.cs` (DTO validation + handler coverage).
- **Client**
  - World map preview — `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs` (Unity preview parity with streamed terrain).
  - Networking protocol sync — `Assets/MyAssets/Scripts/Network/NetworkManager.cs`, `Assets/Generated/Protobuf`, `Assets/Scripts/Minecraft/Network/ProtobufNetworkClient.cs` (client bindings stay in sync with SharedProtocol).

## Content
- **Server** — Biomes and ores: `GameServer/World/Generation/BiomeGenerationSystem.cs`, `GameServer/World/Generation/OreDistributionSystem.cs`.
- **Client** — Player systems: `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`, `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`, `Assets/MyAssets/Scripts/GameWorld/PlayerController.cs`.

## Utility
- **Server** — Config + session management: `GameServer/Configuration/DataDrivenConfigManager.cs`, `GameServer/SessionManager.cs`, `config/world.json`.
- **Client** — Map-control UI/diagnostics: `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs` (runtime toggles + profile hash usage).

Data-driven sources: `config/minecraft_feature_client_server_core_content_util_2026-01-14.json`, `config/world.json`, and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` remain the authoritative knobs for map-control + worldgen parity.
