## Overview
- Date: 2026-01-10
- Scope: hydrology seam normalization across world-map control, caves/rivers/lakes; protobuf registry guardrails; config/StreamingAssets sync.
- Source JSON: `config/minecraft_feature_client_server_core_content_util_2026-01-10-worldgen-edge.json`.

## Core
- world-map-control-hydrology-sync (in-progress): `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `GameServer/World/WorldMapControlManager.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`. Hydrology+flow edge co-normalization with flow-memory/edge-normalization weights; profile hash drives Unity/Server parity.
- cave-river-lake-edge-normalization (in-progress): `GameServer/World/Generation/ImprovedCaveGenerator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`. Thresholds include seam memory (hydrology+flow) and edge normalization to reduce chunk seam artifacts.

## Content
- biome-and-surface-polish (planned): `GameServer/World/Generation/BiomeGenerationSystem.cs`, `config/biomes.json`, `Assets/StreamingAssets/biomes.json`. Align shoreline/wetland benches with updated hydrology masks.

## Util
- protobuf-registry-guard (in-progress): `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtocolRegistry`, `proto/enhanced_minecraft_game.proto`, Unity network bootstrap. Validates chunk load/unload/time/weather/entity descriptors and registry bindings on startup; rerun `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` when hashes drift.
- data-driven-config-refresh (stable): `config/world.json`, `config/enhanced-terrain-config.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, `DataDrivenConfigManager`. JSON-backed hydrology/flow knobs stay reloadable for server and mirrored to Unity profiles.
