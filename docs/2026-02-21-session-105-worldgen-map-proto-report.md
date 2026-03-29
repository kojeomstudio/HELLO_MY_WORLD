# 2026-02-21 Session 105 WorldGen / Map Control / Proto Report

## 1) Scope
- Cave / river / lake terrain algorithm refinement
- Server/client world-map queue control architecture refinement
- Protobuf packet usage/path validation with dummy probe
- JSON config + data-driven manifest/profile synchronization

## 2) Terrain Algorithm Improvements

### Cave
- File: `GameServer/World/Generation/ImprovedCaveGenerator.cs`
- Added `ApplyFloodBypassVentDampingBridge(...)` and integrated into cave mask post-process pipeline.
- Goal: damp unstable bypass cavities around high-moisture flow corridors while keeping ventilated channels.

### River
- File: `GameServer/World/Generation/ImprovedRiverGenerator.cs`
- Added `ApplyConfluenceLagStorageBridge(...)` and integrated into river mask final stabilization.
- Goal: improve lag-storage continuity around tributary confluences to reduce sudden channel collapse.

### Lake
- File: `GameServer/World/Generation/ImprovedLakeGenerator.cs`
- Added `ApplySpillwayBackflowDampingBridge(...)` and integrated before leakage clamp stage.
- Goal: reduce spillway backflow oscillation and stabilize basin-retention transitions.

## 3) World-Map Control Architecture (Server + Client)

### Shared Policy
- File: `GameCommon/World/WorldMapQueuePolicy.cs`
- Added `ComputeRecoveryRamp(...)` helper for deterministic ramp-based queue recovery.

### Server
- Files:
  - `GameServer/Configuration/ConfigurationModels.cs`
  - `GameServer/Program.cs`
  - `GameServer/World/WorldMapControlManager.cs`
- Added queue hysteresis knobs:
  - `QueueEmergencyHoldTicks`
  - `QueueRecoveryRampTicks`
- Applied to runtime override parsing, queue policy parsing, and adaptive queue-limit calculations.

### Client
- File: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
- Added matching runtime/policy fields and queue-state counters:
  - `queueEmergencyHoldTicks`, `queueRecoveryRampTicks`
  - remaining tick counters for hold/recovery phases
- Applied to effective-load latch transitions and adaptive queue-limit recovery behavior.

## 4) Data-Driven Config / Profile Synchronization
- Updated:
  - `config/world.json`
  - `Assets/StreamingAssets/world-config.json`
  - `config/world_map_control_queue_policy.json`
  - `Assets/StreamingAssets/world_map_control_queue_policy.json`
  - `config/enhanced_world_map_control_server.json`
  - `Assets/StreamingAssets/enhanced_world_map_control_client.json`
- Regenerated/aligned world-map profile:
  - `config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`

## 5) Signature / Version / Feature Catalog
- Hydrology signature upgraded to `2026-02-21-hydrology-riverlake-cave-v45`
  - `GameCommon/World/SharedFeatureCatalog.cs`
- Map-control profile baseline raised to `49`
  - `GameServer/World/WorldGenerationConfig.cs`
  - `config/world.json`
  - `Assets/StreamingAssets/world-config.json`
- Session 105 feature manifest added:
  - `config/minecraft_feature_client_server_core_content_util_2026-02-21-session-105.json`

## 6) Protobuf / Dummy Client Validation
- Updated minimum profile guard in dummy probe:
  - `GameServer/Testing/DummyProtocolClient.cs`
  - `config/protocol_dummy_client.json`
  - `config/dummy_minecraft_client.json`
- Manifest loader priority updated:
  - `GameServer/Program.cs`

## 7) Validation Commands
- `dotnet build SharedProtocol/SharedProtocol.csproj` -> PASS
- `dotnet build GameServer/GameServer.csproj` -> PASS
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` -> PASS
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe` -> PASS
  - Required packets: pass
  - Optional packets: warning-only (unregistered by design)
- `dotnet run --project GameServer/GameServer.csproj -- --selftest` -> PASS

## 8) Notes
- Existing repository-wide warnings (nullable/async + optional proto packet registration) remain non-blocking and pre-existing patterns.
- Session 105 focuses on stability and parity improvements without changing optional packet requirement policy.

