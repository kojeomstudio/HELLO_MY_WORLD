# 2026-02-02 Worldgen & Protocol Update (v9 Hydrology)

## Summary
- Hydrology signature advanced to `2026-02-02-hydrology-riverlake-v9` with map-control profile v11 and updated world configs.
- River, lake, and cave generation now apply seam-fill smoothing, meander jitter, lake variance/outflow stability, and stronger edge sealing in MapGeneratorLib previews.
- World map signature context now includes lake radius/outflow and river seam fill parameters; GameCommon.dll rebuilt and copied to Unity plugins.
- Dummy protocol client output now captures registered packets + descriptor fingerprint with an expanded packet matrix.

## Worldgen Changes
- Added river seam fill smoothing (`RiverSeamFillStrength`) and bank stability clamp to normalize intensity near chunk borders.
- River width modulation now uses configurable meander jitter and anisotropy damping.
- Lake carving respects `LakeMaxRadius` and variance weight; overflow channels use outflow seal/stability weights to reduce erosion spikes.
- Cave thresholds gain additional seam sealing via `CaveEdgeSealStrength` to cut chunk-edge leaks.

## Protocol & Shared DLL
- Hydrology signature bumped; `WorldMapSignatureContext`/`WorldMapSignature` include lake radius/outflow stability and river seam fill fields.
- GameCommon.dll rebuilt (netstandard2.1) and copied to `Assets/Plugins/GameCommon.dll` to keep Unity aligned with new signatures.
- Dummy protocol client (`GameServer/Testing/DummyProtocolClient`) now reports registered packet set and descriptor fingerprint; config packet matrix expanded.

## Config/Data Updates
- `config/world.json`, `Assets/StreamingAssets/world-config.json`: `MapControlProfileVersion` -> 11, hydrology signature v9.
- `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`: hydrology signature v9, version 11 (hash left blank to force regeneration on load).
- `config/minecraft_feature_core_content_util_2026-02-02.json`: added CORE-014/CONTENT-014/UTIL-014 entries for hydrology v9 and protocol hardening.
- `config/protocol_dummy_client.json`: expanded packet list for proto probe coverage.

## Tests
- `dotnet build SharedProtocol/SharedProtocol.csproj` (warnings: nullable + async without await; protobuf-net version resolution).
- `dotnet build GameServer/GameServer.csproj` (same NU1603 warning; successful build, GameCommon package/dll produced).
- `dotnet build MapGeneratorLib/MapGeneratorLib/MapGeneratorLib.csproj` (clean).
- Dummy protocol probe not executed in-process due to loader constraints on `System.Runtime`; client implementation updated to emit richer reports once executed in runtime environment.
