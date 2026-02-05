# 2026-02-05 Core/Content/Utility Feature Snapshot

Source manifest: `config/minecraft_feature_core_content_util_2026-02-05.json` (hydrology signature `2026-02-05-hydrology-riverlake-cave-v14`, profile v17).

## Core
- Hydrology-stable worldgen v14 (caves/rivers/lakes) — `GameServer/World/WorldManager.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`.
- World map control architecture/signature (v17) — shared profile utility + hashes in `GameCommon/World/WorldMapControlProfile*.cs`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`.
- Shared contracts via DLLs — `GameCommon/GameCommon.csproj`, `SharedProtocol/SharedProtocol.csproj`, Unity plugin copies in `Assets/Plugins/`.

## Content
- Riparian cave guard and river/lake sealing — applied to server/worldgen and Unity previews to prevent seam punctures (`WorldManager`, `WorldMapController`, world configs).
- World map preview parity — keep hydrology/cave masks in Unity aligned with server values (`WorldMapControlProfile.cs`, `EnhancedTerrainGenerator.cs`).

## Utility
- Dummy protocol client + registry validation — `GameServer/Testing/DummyProtocolClient.cs`, `config/protocol_dummy_client.json`, `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`.
- Data-driven config parity — world/profile JSONs and manifest files kept under `config/` and `Assets/StreamingAssets/` for server/client consumption.
- Shared DLL distribution — rebuild/copy `GameCommon.dll` and `SharedProtocol.dll` after protocol/profile updates.
