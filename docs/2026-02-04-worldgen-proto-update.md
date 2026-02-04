# 2026-02-04 Worldgen + Proto Update

## Overview
- Hydrology signature bumped to `2026-02-04-hydrology-riverlake-v13`; map-control profile version 15 regenerated and mirrored to StreamingAssets.
- Worldgen focuses on river/lake seam continuity and cave entrance dampening; shared DLL/proto artifacts rebuilt and synced to Unity.
- Feature manifest refreshed (`config/minecraft_feature_core_content_util_2026-02-04.json`) with core/content/util ordering for today's work.

## Changes
- **Worldgen**
  - Added river edge continuity blend and stronger seam smoothing in `WorldGenAlgorithms.cs` (`RiverEdgeContinuityWeight`, `HydrologyEdgeBlendRadius` 8).
  - Introduced lake outflow taper for smoother spillways (`LakeOutflowTaper`, seal/stability weight increases) and updated config defaults.
  - Cave entrance dampening now considers flow-driven moisture (`CaveEntranceFlowDampening`, higher stability smoothing/support density).
  - MapGeneratorLib rebuilt and copied to `Assets/Plugins/MapGeneratorLib.dll`.
- **Map Control / Shared Signatures**
  - World map control profile v15 generated (`config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`) with new hydrology signature and added fields (river edge continuity, lake taper, cave entrance dampening).
  - SharedFeatureCatalog hydrology signature updated; WorldMapSignatureContext includes new parameters for parity.
  - GameCommon.dll rebuilt and copied to `Assets/Plugins/GameCommon.dll`.
- **Config & Data**
  - `config/world.json` tuned for v13 hydrology (edge normalization 0.58, seam clamp 0.22, reservoir/smoothing iterations 6, river mouth radius 8, lake/cave stability tweaks).
  - Added explicit JSON keys for `riverEdgeContinuityWeight`, `lakeOutflowTaper`, and `caveEntranceFlowDampening` (mirrored to `Assets/StreamingAssets/world-config.json`) to keep server/Unity parity.
  - StreamingAssets world config synced from server config.
  - Feature manifest now includes `features` list for tooling consumption.
- **Protocol / Dummy Client**
  - DummyProtocolClient logs hydrology signature and packet counts; proto probe/reference reports regenerated.
  - `config/proto_reference_report.json` and `reports/proto_probe_report.json` refreshed via `--proto-probe`.

## Tests
- `dotnet build GameCommon/GameCommon.csproj -c Release`
- `dotnet build MapGeneratorLib/MapGeneratorLib/MapGeneratorLib.csproj -c Release`
- `dotnet build SharedProtocol/SharedProtocol.csproj -c Release`
- `dotnet build GameServer/GameServer.csproj -c Release`
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe`

## Notes / Next Steps
- Optional EnhancedMinecraft proto messages remain unbound; regenerate protoc outputs and extend ProtocolRegistry if/when they become required.
- Keep feature manifest in sync with future hydrology signatures and profile versions when tuning worldgen parameters.
