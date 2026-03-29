Minecraft Feature Split — 2026-01-12
====================================

Core
- Hydrology flow-memory stabilization for caves/rivers/lakes shared by server (`GameServer/World/WorldManager.cs`) and client preview (`MapGeneratorLib/.../WorldGenAlgorithms.cs`, `WorldMapController`).
- World-map control parity: propagate flow-memory and wetland buffers through `WorldMapControlProfile`, `WorldMapControlManager`, and `WorldAreaManager`.
- Protobuf integrity: tighter registry/descriptor validation in `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` to ensure generated DTOs remain referenced.

Content
- River/lake hydrology smoothing that tempers catchment spikes and shoreline flooding using flow-memory + slope-aware blending.
- Cave density threshold tuning that respects the stabilized hydrology envelope to keep ceilings/entrances coherent near rivers and lakes.
- Wetland padding aligned to downhill flow for lakes and riparian seams.

Utility
- Data-driven configs remain JSON-backed (`config/world.json`, `Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, `config/world_map_control_profile.json`).
- Feature inventory JSON (`config/minecraft_feature_client_server_core_content_util_2026-01-12.json`) tracks the above split for traceability and sequencing.
