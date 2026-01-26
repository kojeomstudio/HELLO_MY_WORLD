# 2026-01-26 Minecraft Feature Catalog (Session 18)

Latest commit: `93f18cce`  
Hydrology signature: `2026-01-26-hydrology-shield-v2`  
Data sources: `config/minecraft_feature_client_server_core_content_util_2026-01-26-session-18.json`, `config/world.json`, `config/world_map_control_profile.json`

## Core
| Seq | ID | Layer | Name | Status | Files |
| --- | --- | --- | --- | --- | --- |
| 1 | S18-CORE-01 | Shared | Hydrology signature alignment (`-v2`) for caves/rivers/lakes + map control | in-progress | GameCommon/World/SharedFeatureCatalog.cs, GameServer/World/WorldMapControlProfile.cs, Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs, config/world_map_control_profile.json |
| 2 | S18-CORE-02 | Shared | GameCommon/SharedProtocol DLL pipeline (enums, profile hash, proto registry) | planned | GameCommon/GameCommon.csproj, SharedProtocol/SharedProtocol.csproj, Assets/Plugins/GameCommon.dll |

## Content
| Seq | ID | Layer | Name | Status | Files |
| --- | --- | --- | --- | --- | --- |
| 1 | S18-CONTENT-01 | Server | Hydrology-aware cave stability & water-table blending | planned | GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs, MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs |
| 2 | S18-CONTENT-02 | Shared | River curvature smoothing + seam-safe hydrology warp | planned | GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs, config/world.json |
| 3 | S18-CONTENT-03 | Shared | Lake shoreline/outflow harmonization with river mask | planned | GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs, config/world.json |

## Utility
| Seq | ID | Layer | Name | Status | Files |
| --- | --- | --- | --- | --- | --- |
| 1 | S18-UTIL-01 | Server | Dummy protocol client round-trips (TimeUpdate + WorldGenerationRequest) | planned | GameServer/Testing/DummyProtocolClient.cs |
| 2 | S18-UTIL-02 | Shared | Proto registry/fingerprint validation in map-control + terrain signature | planned | SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs, GameServer/World/WorldMapControlManager.cs |
| 3 | S18-UTIL-03 | Shared | Data-driven config parity (world/map-control JSON served to Unity) | planned | config/world.json, config/world_map_control_profile.json, Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs |

## Notes
- Sequence reflects required implementation order to keep hydrology signature in sync across server and client.
- The JSON catalog in `config/…session-18.json` is the authoritative data-driven source; docs mirror that data for review.
- GameCommon DLL remains the shared delivery mechanism for enums and feature descriptors consumed by Unity and the server.
