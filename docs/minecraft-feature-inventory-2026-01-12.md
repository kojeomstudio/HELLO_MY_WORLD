# Minecraft Feature Inventory (2026-01-12)

This session refreshes the core/content/util feature list for the Minecraft-style client and server. Source of truth JSON: `config/minecraft_feature_inventory_2026-01-12-session.json`.

## Core
- **Hydrology masks & terrain coordinator** — server `ImprovedTerrainCoordinator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`, `ImprovedCaveGenerator`; client `WorldGenAlgorithms`, `WorldMapController`; data `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`.
- **World map control & preview** — server `WorldMapControlManager`, `WorldMapControlProfile`; client `WorldMapController`; JSON under `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`.
- **Networking & EnhancedMinecraft protocol** — server `ProtocolRegistry`, `ProtocolValidator`, `EnhancedProtocolHandler`; client `Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`, `GameNetworkManager`; IDL `proto/enhanced_minecraft_game.proto`.
- **Chunk streaming & synchronization** — server `MinecraftChunkHandler`, `ChunkSnapshot`; client `ChunkManager`, `ChunkRenderer`; data `config/world.json`, `config/blocks.json`, `Assets/StreamingAssets/blocks.json`.

## Content
- **Rivers, lakes, wetlands** — server `ImprovedRiverGenerator`, `ImprovedLakeGenerator`; client `WorldGenAlgorithms`; data `config/world.json`, `Assets/StreamingAssets/world-config.json`.
- **Cave systems** — server `ImprovedCaveGenerator`; client `WorldGenAlgorithms`; data `config/world.json`.
- **Biomes, blocks, items** — server `BiomeGenerationSystem`, `WorldBlockHandler`, `Items/*`; client `Minecraft/World`, `Minecraft/Items`; data `config/biomes.json`, `config/blocks.json`, `config/items.json`, `Assets/StreamingAssets/blocks.json`, `Assets/StreamingAssets/items.json`.

## Util
- **Config parity & validation** — server configs under `config/*.json`, `ServerConfig.cs`, `WorldGenerationConfig.cs`; client configs under `Assets/StreamingAssets/*.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`; focus on world/map-control parity.
- **Docs & plan traceability** — `plans/2026-01-12-worldgen-proto-session.md`, `docs/*`, including this inventory and `docs/worldgen-proto-update-2026-01-12.md`.
- **Diagnostics & telemetry** — proto diagnostics (`ProtoDiagnostics`), map control signatures (`WorldMapControlManager`, `WorldMapController`), hydrology preview logging.
