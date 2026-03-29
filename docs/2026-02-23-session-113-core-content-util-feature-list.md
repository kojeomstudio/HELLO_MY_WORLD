# Session 113 Core/Content/Utility Feature List

**Date**: 2026-02-23  
**Session**: 113  
**Signature**: `2026-02-23-hydrology-riverlake-cave-v49`  
**Map-Control Profile Version**: `53`

## Core
- `S113-CORE-01` Shared queue stale-prune budget policy (`GameCommon/World/WorldMapQueuePolicy.cs`, server/client map controllers)
- `S113-CORE-02` Signature/profile synchronization v49/v53 (`GameCommon/World/SharedFeatureCatalog.cs`, world config defaults, profile JSON)
- `S113-CORE-03` Server feature manifest priority for session-113 (`GameServer/Program.cs`)

## Content
- `S113-CONTENT-01` Seasonal recharge cave seal bridge (`GameServer/World/Generation/ImprovedCaveGenerator.cs`)
- `S113-CONTENT-02` Seasonal runoff pulse river bridge (`GameServer/World/Generation/ImprovedRiverGenerator.cs`)
- `S113-CONTENT-03` Seasonal floodplain recharge lake bridge (`GameServer/World/Generation/ImprovedLakeGenerator.cs`)
- `S113-CONTENT-04` Seasonal runoff terrain coupling pass (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`)

## Utility
- `S113-UTIL-01` Protocol optional-message set parity validation (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`)
- `S113-UTIL-02` Dummy probe required-packet coverage guard (`GameServer/Testing/DummyProtocolClient.cs`, `Tools/DummyMinecraftClient/Program.cs`, JSON configs)
- `S113-UTIL-03` Queue policy JSON v19 runtime/config parity (`config/world_map_control_queue_policy.json` + server/client mirrors)

## Data-Driven Artifacts
- `config/minecraft_feature_client_server_core_content_util_2026-02-23-session-113.json`
- `GameServer/config/minecraft_feature_client_server_core_content_util_2026-02-23-session-113.json`
