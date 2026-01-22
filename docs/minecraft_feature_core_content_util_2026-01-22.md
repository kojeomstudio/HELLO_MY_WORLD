# Minecraft Feature Categorization – 2026-01-22

## Client
- **Core**
  - World map control profile v4 with lake seepage-aware previews (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`, `Assets/StreamingAssets/world-config.json`)
  - Hydrology/flow preview parity using updated config knobs (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/Core/WorldConfig.cs`)
- **Content**
  - River/lake overlay rendering fed by stitched hydrology masks (`Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`)
- **Util**
  - Proto fingerprint guard before preview generation (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Generated/Protobuf`)
  - StreamingAssets config + profile sync (`Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`)

## Server
- **Core**
  - Hydrology-aware terrain pipeline with lake seepage normalization (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `config/world.json`)
  - World map control profile v4 + signature `2026-01-22-lake-seepage+proto-guard` (`GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapController.cs`, `config/world_map_control_profile.json`)
- **Content**
  - Cave stability tuned against seepage-adjusted hydrology and sealed river/lake edges (`GameServer/World/Generation/ImprovedCaveGenerator.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`)
- **Util**
  - Proto handler validation requires generated prototypes for bound messages (`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `GameServer/GameServer.cs`)
  - Config/profile signatures include proto fingerprint + hydrology/lake parameters (`GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapController.cs`)
