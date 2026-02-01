# Core/Content/Utility Feature List (2026-02-01)

## Core (order)
1. **World map control v10 & hydrology signature v8** — `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`, `GameServer/World/WorldMapControlProfile.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`.
2. **Shared DLL + manifest parity** — `GameCommon/GameCommon.csproj`, `SharedProtocol/SharedProtocol.csproj`, `config/minecraft_feature_core_content_util_2026-02-01.json`.

## Content (order)
1. **River/Lake seam smoothing** — `GameServer/World/Generation/ImprovedRiverGenerator.cs`, `GameServer/World/Generation/ImprovedLakeGenerator.cs`, `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`.
2. **Cave riparian guard** — `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `GameServer/World/Generation/ImprovedCaveGenerator.cs`.

## Utility (order)
1. **Proto registry audit + dummy client report** — `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Testing/DummyProtocolClient.cs`, `config/protocol_dummy_client.json`, `config/proto_reference_report.json`.
2. **Docs & plan refresh** — `docs/2026-02-01-worldgen-proto-update.md`, `plans/2026-02-01-session-33-plan.md`, `README.md`.

### Notes
- Hydrology signature: `2026-02-01-hydrology-riverlake-v8`; map-control profile version: `10`.
- Use `dotnet run --project GameServer -- --generate-map-profile` after tweaking `config/world.json` to keep server/Unity in sync.
