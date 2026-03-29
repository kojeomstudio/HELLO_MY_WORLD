# Session 143 Terrain / Map-Control / Proto Validation (2026-03-07)

## Summary
- Hydrology signature and profile baseline uplifted to `2026-03-07-hydrology-riverlake-cave-v67` and `MapControlProfileVersion=71`.
- Added a new riparian aquifer momentum coupling pass to both server and client terrain pipelines.
- Synced data-driven JSON configs/profiles/manifests across root/server/client mirrors.
- Re-validated protobuf registry/reference and dummy protocol probe paths.

## Core / Content / Utility Inventory
Reference manifest:
- `config/minecraft_feature_client_server_core_content_util_2026-03-07-session-143.json`
- `GameServer/config/minecraft_feature_client_server_core_content_util_2026-03-07-session-143.json`
- `Assets/StreamingAssets/minecraft_feature_client_server_core_content_util_2026-03-07-session-143.json`

### Core
- `S23-CORE-01` Shared DLL Contracts
- `S23-CORE-02` Google Protobuf Registry and Diagnostics
- `S23-CORE-03` Authoritative JSON World Config
- `S23-CORE-04` Hydrology Signature v67 + Profile v71
- `S23-CORE-05` World Map Queue and Runtime Control
- `S23-CORE-06` Session and Network Lifecycle

### Content
- `S23-CONTENT-01` Deterministic Base Terrain and Biomes
- `S23-CONTENT-02` Improved River Generator
- `S23-CONTENT-03` Improved Lake Generator
- `S23-CONTENT-04` Improved Cave Generator
- `S23-CONTENT-05` Session 143 Riparian Aquifer Momentum Coupling
- `S23-CONTENT-06` Session 143 Terrain Pipeline Parity Alignment
- `S23-CONTENT-07` Session 143 Profile Baseline Uplift
- `S23-CONTENT-08` World Map Control Client Preview Parity
- `S23-CONTENT-09` Data-Driven Blocks, Items, and Biomes

### Utility
- `S23-UTIL-01` Data-Driven Environment and Runtime Config Files
- `S23-UTIL-02` Data-Driven Gameplay Content Catalogs
- `S23-UTIL-03` Server Dummy Protocol Probe
- `S23-UTIL-04` Standalone Dummy Minecraft Protocol Client
- `S23-UTIL-05` Using and Reference Compile Verification
- `S23-UTIL-06` Session Planning and Documentation

## Implemented Changes

### Terrain Algorithm (Cave/River/Lake)
- Server: Added `ApplyRiparianAquiferMomentumCoupling(...)` and integrated it into the hydrology/coupling stage order.
  - File: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
- Client: Added matching `ApplyRiparianAquiferMomentumCoupling(...)` in Unity preview generator and integrated with the same order.
  - File: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`

### World-Map Control Architecture
- Shared baseline updated in shared DLL catalog:
  - `GameCommon/World/SharedFeatureCatalog.cs`
  - `HydrologySignature`: `v67`
  - `MapControlProfileVersion`: `71`
- Regenerated/realigned world-map control profiles:
  - `config/world_map_control_profile.json`
  - `GameServer/config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`

### Protobuf Packet Reference / Usage Review
- Strengthened dummy protocol tool startup checks by adding:
  - `ProtocolValidator.ValidateEnhancedContracts()`
  - `ProtoDiagnostics.AssertRegistryClean()`
  - File: `Tools/DummyMinecraftClient/Program.cs`
- Regenerated proto reference outputs through server runtime/probe commands:
  - `config/proto_reference_report.json`
  - `reports/proto_probe_report.json`

### JSON Config / Data-Driven Sync
- World version uplift (`MapControlProfileVersion=71`):
  - `config/world.json`
  - `GameServer/config/world.json`
  - `Assets/StreamingAssets/world-config.json`
- Probe guard uplift (`minMapControlProfileVersion=71`):
  - `config/protocol_dummy_client.json`
  - `GameServer/config/protocol_dummy_client.json`
  - `config/dummy_minecraft_client.json`
  - `GameServer/config/dummy_minecraft_client.json`
- Parity manifest updated for session-143 feature manifest:
  - `config/config_parity_manifest.json`
  - `GameServer/config/config_parity_manifest.json`
  - `Assets/StreamingAssets/config_parity_manifest.json`

## Validation Commands
- `dotnet build SharedProtocol/SharedProtocol.csproj` (PASS)
- `dotnet build GameCommon/GameCommon.csproj` (PASS)
- `dotnet build GameServer/GameServer.csproj` (PASS)
- `dotnet test SharedProtocol/SharedProtocol.csproj --no-build` (PASS)
- `dotnet test GameServer/GameServer.csproj --no-build` (PASS)
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` (PASS)
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe` (PASS)
- `dotnet run --project GameServer/GameServer.csproj -- --selftest` (PASS)

## Validation Notes
- `--proto-probe` and `--selftest` still emit existing optional-packet warnings (optional bindings not registered/generated), but required fingerprint/binding coverage checks pass with current policy.
- Self-test log still contains legacy scenario-level warnings (`Unexpected response type`, terrain stage warnings) while process exits successfully.
