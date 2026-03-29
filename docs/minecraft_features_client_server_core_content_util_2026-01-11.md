# Minecraft Feature Matrix (2026-01-11 Hydrology/Map-Control Pass)

- Source data: `config/minecraft_feature_client_server_core_content_util_2026-01-11-hydrology-sync.json`
- Scope: Align client/server hydrology-driven worldgen, map-control parity, protobuf registry validation, and config hygiene.

## Core
- (1) Hydrology continuity envelope — Server `ImprovedTerrainCoordinator/RiverGenerator/LakeGenerator/CaveGenerator`; Client `WorldMapController` + profile; Data `config/world.json`, `config/world_map_control_profile.json`.
- (2) World-map control parity/signatures — Server `WorldMapControlManager`, `WorldMapController`, `WorldMapControlProfile`, `EnhancedTerrainGenerationPipeline`; Client `WorldMapController`, `WorldAreaManager`, `WorldConfigFile`; Data `config/enhanced_world_map_control_*.json`, `config/world_map_control_profile.json`.
- (3) Protobuf registry guardrails — Server `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtocolValidator.cs`; Client `GameNetworkManager`, generated DTOs; Data `proto/enhanced_minecraft_game.proto`, `Assets/Generated/Protobuf`.

## Content
- (4) River/lake alignment with hydrology — Server `ImprovedRiverGenerator`, `ImprovedLakeGenerator`; Client `WorldMapController` river/lake mask builders; Data `config/world.json`, `config/enhanced-terrain-config.json`.
- (5) Cave entrance/ceiling stability — Server `ImprovedCaveGenerator`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`; Client `WorldMapController` cave mask; Data `config/world.json`, `config/world_map_control_profile.json`.

## Util
- (6) Config hygiene and data-driven defaults — `config/enhanced_world_map_control_server.json`, `config/enhanced_world_map_control_client.json`, `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`.
- (7) Docs/reporting — `docs/minecraft_features_client_server_core_content_util_2026-01-11.md`, `docs/terrain_generation_improvements_2026-01-11.md`, `README.md`, `plans/2026-01-11-session-plan.md`.

## Implementation Sequence
1. Apply hydrology-flow envelope and seam normalization.
2. Refine river pressure and lake basin masks with updated hydrology.
3. Stabilize caves (riparian plugs, ceiling moisture clamp, edge seals).
4. Refresh world-map control signatures/profiles and ensure parity in client/server loaders.
5. Run protobuf registry validation (package/descriptor/parser) and builds.
6. Document outcomes and update JSON configs.
