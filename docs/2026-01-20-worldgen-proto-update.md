# 2026-01-20 Worldgen & Proto Update

## Terrain & hydrology
- Added a hydrology pressure-balancing pass shared by server and MapGeneratorLib to smooth inflow/outflow spikes before carving rivers/lakes/caves (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `EnhancedTerrainGenerationPipeline.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`).
- New JSON knobs: `HydrologyPressureBlend`, `HydrologyPressureGradientClamp` in `config/world.json` and Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`; included in `config/world_map_control_profile.json`.
- World map control profile now exports/consumes the pressure knobs so Unity previews stay in lockstep (`GameServer/World/WorldMapControlProfile.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`, `WorldAreaManager.cs`).

## Protocol & validation
- Server bootstrap validates Minecraft handler bindings against generated protobuf contracts to catch stale `using` directives or missing DTOs before processing traffic (`GameServer/GameServer.cs`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`).

## Feature inventory
- Added session-aligned core/content/util split with sequencing at `config/minecraft_feature_client_server_core_content_util_2026-01-20.json` to guide rollout order.

## Notes
- Keep `config/world.json`, `config/world_map_control_profile.json`, and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` in sync; map-control hash will refresh automatically when the server regenerates the profile.
- Unity previews require the updated map-control JSON so `WorldGenAlgorithms` picks up the new pressure-balancing knobs.
