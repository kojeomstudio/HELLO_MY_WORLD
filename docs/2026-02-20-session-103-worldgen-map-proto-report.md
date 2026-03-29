# 2026-02-20 Session 103 WorldGen / MapControl / Proto Report

## Scope
This session focused on four areas:
1. Cave-river-lake terrain generation coupling quality
2. Server/client world-map control queue recovery behavior
3. Protobuf packet probe client API/runtime validation
4. Data-driven profile/signature/version parity updates

## Code Changes

### Terrain Generation
- Updated `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`.
- Added basin/relief-aware weighting and coupling logic to:
  - `BuildRiverMask(...)`
  - `BuildLakeMask(...)`
  - `BuildCaveMask(...)`
  - `CarveCaves(...)`
- Added helper utilities:
  - `ComputeLocalRelief(...)`
  - `ComputeBasinPotential(...)`
  - `SampleNeighborhoodMax(...)`

### World Map Control Architecture
- Updated server adaptive queue policy in `GameServer/World/WorldMapControlManager.cs`.
- Added low-load queue decay so queue-limit expansion recovers after load spikes.
- Mirrored recovery behavior on Unity side in `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`.
- Updated queue/runtime JSON knobs:
  - `config/world_map_control_queue_policy.json` (v15)
  - `config/enhanced_world_map_control_server.json`
  - `Assets/StreamingAssets/enhanced_world_map_control_client.json`

### Protobuf Dummy Client
- Replaced `GameServer/Testing/DummyProtocolClient.cs` with probe-oriented implementation expected by `Program.cs`:
  - `CreateFromConfig(...)`
  - `RunAsync(...)`
  - data-driven settings + output reports
  - optional TCP packet send probe
- Guard updates:
  - `config/protocol_dummy_client.json`
  - `config/dummy_minecraft_client.json`

### Profile/Signature/Version Parity
- Updated signature and profile defaults:
  - `GameCommon/World/SharedFeatureCatalog.cs` -> `v44`
  - `GameServer/World/WorldGenerationConfig.cs` -> profile `48`
  - `Assets/Scripts/Minecraft/Core/WorldConfig.cs` -> profile `48`
  - `Assets/MyAssets/Scripts/DataFiles/DataFile/WorldConfigFile.cs` -> profile `48`
- Updated world configs:
  - `config/world.json`
  - `Assets/StreamingAssets/world-config.json`
- Generated/validated profile outputs:
  - `config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`

## Data-Driven Feature Categorization
- New manifest: `config/minecraft_feature_client_server_core_content_util_2026-02-20-session-103.json`
- Runtime loader priority updated in `GameServer/Program.cs`.

## Validation Commands
1. `dotnet build SharedProtocol/SharedProtocol.csproj`
2. `dotnet build GameServer/GameServer.csproj`
3. `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`
4. `dotnet run --project GameServer/GameServer.csproj -- --proto-probe`
5. `dotnet run --project GameServer/GameServer.csproj -- --selftest`
6. `dotnet test GameServer/GameServer.csproj`
7. `dotnet test SharedProtocol/SharedProtocol.csproj`

## Validation Results
- `dotnet build SharedProtocol/SharedProtocol.csproj`: PASS
- `dotnet build GameServer/GameServer.csproj`: PASS
- `--generate-map-profile`: PASS
  - `config/world_map_control_profile.json` regenerated
  - `Assets/StreamingAssets/world-map-control.json` mirrored
  - profile `48`, signature `2026-02-20-hydrology-riverlake-cave-v44`
- `--proto-probe`: PASS
  - descriptor fingerprint matched expected fingerprint
  - required packet round-trip set validated
  - optional packets (`MultiBlockChange`, `InventoryUpdate`, etc.) remain WARN-only and are intentionally non-blocking
- `--selftest`: PASS (exit code 0)
  - server startup/session flow completed
  - test log still contains pre-existing runtime WARN lines for async response ordering and optional packet bindings
- `dotnet test` on `GameServer` / `SharedProtocol`: command PASS (both projects are non-test app/library projects, so no NUnit/xUnit test cases executed)

## Outcome Summary
- Required protobuf packet path is healthy for runtime and probe flow.
- Profile version guard is `48`.
- Hydrology signature guard is `2026-02-20-hydrology-riverlake-cave-v44`.
- Queue-limit behavior now recovers under low-load conditions after transient spikes.
