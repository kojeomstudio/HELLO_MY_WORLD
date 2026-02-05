# 2026-02-05 Worldgen & Proto Update

## What changed
- Bumped hydrology signature to `2026-02-05-hydrology-riverlake-cave-v14` and raised the map-control profile to v17 to capture the new cave/riparian continuity tuning. Updated `config/world.json`, `config/world_map_control_profile.json`, and `Assets/StreamingAssets/world-map-control.json` accordingly.
- Added riparian cave guard weighting to server generation (`GameServer/World/WorldManager.cs`) and MapGeneratorLib so caves no longer puncture river/lake seams across chunk edges. Unity previews now apply the same guard in `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`.
- Refreshed the shared feature manifest (`config/minecraft_feature_core_content_util_2026-02-05.json`) and hydrology signature source of truth (`GameCommon/World/SharedFeatureCatalog.cs`).
- Dummy protocol client now surfaces missing required bindings explicitly in logs to keep protoc-generated packets aligned (`GameServer/Testing/DummyProtocolClient.cs`).

## Config/data alignment
- Profile hash must be regenerated after editing `config/world_map_control_profile.json`; copy the result into `Assets/StreamingAssets/world-map-control.json` to keep Unity in sync.
- Shared DLLs remain the source of truth for enums and contracts; rebuild/copy `GameCommon.dll` and `SharedProtocol.dll` to `Assets/Plugins/` after profile or protocol changes.

## Test plan
- `dotnet build GameCommon/GameCommon.csproj`
- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameServer/GameServer.csproj`
- Optional: run the dummy protocol client with `config/protocol_dummy_client.json` to emit `reports/proto_probe_report.json` and confirm registry fingerprints.
