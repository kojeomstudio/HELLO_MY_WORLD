# Session 129 Implementation and Validation (2026-02-27)

## Summary

This session applies a coordinated update across terrain generation, world-map control architecture, protobuf validation, and data-driven config parity.

- Hydrology signature raised to `2026-02-27-hydrology-riverlake-cave-v57`
- Map-control profile baseline raised to `61`
- Server/client queue stale-prune cap moved to JSON-driven runtime policy
- Cave-river-lake subsurface conduit exchange algorithm applied on both server and Unity preview paths

## Implemented Changes

### 1) Core/Content/Utility Feature Inventory Refresh

- Added new session manifest:
  - `config/minecraft_feature_client_server_core_content_util_2026-02-27-session-129.json`
  - `GameServer/config/minecraft_feature_client_server_core_content_util_2026-02-27-session-129.json`
  - `Assets/StreamingAssets/minecraft_feature_client_server_core_content_util_2026-02-27-session-129.json`
- Updated startup manifest priority:
  - `GameServer/Program.cs`

### 2) Terrain Algorithm Improvement (Caves/Rivers/Lakes)

- Added subsurface conduit exchange pass in server terrain coordinator:
  - `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
  - new pass: `ApplySubsurfaceConduitExchangeBridge`
- Mirrored equivalent pass for Unity preview generation:
  - `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - new pass: `ApplySubsurfaceConduitExchangeBridge`

### 3) Server/Client World-Map Architecture Improvements

- Added queue stale-prune max budget as a data-driven setting:
  - `GameServer/Configuration/ConfigurationModels.cs`
  - `GameServer/World/WorldMapControlManager.cs`
  - `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - `GameServer/Program.cs` (runtime and queue-policy override parsing)
- Updated queue policy JSONs (server/client/shared mirrors):
  - `config/world_map_control_queue_policy.json`
  - `GameServer/config/world_map_control_queue_policy.json`
  - `Assets/StreamingAssets/world_map_control_queue_policy.json`

### 4) Shared DLL / Signature Baseline

- Updated shared feature constants:
  - `GameCommon/World/SharedFeatureCatalog.cs`
- Updated client default profile-version fallback:
  - `Assets/Scripts/Minecraft/Core/WorldConfig.cs`

### 5) Profile Hash Consistency Fix

- Recomputed profile hash after runtime profile mutations before save:
  - `GameServer/Program.cs` (`EnsureWorldMapProfile`)

### 6) JSON Data-Driven Parity Updates

- Updated world/profile/probe and runtime queue configs:
  - `config/world.json`
  - `GameServer/config/world.json`
  - `Assets/StreamingAssets/world-config.json`
  - `config/protocol_dummy_client.json`
  - `GameServer/config/protocol_dummy_client.json`
  - `config/enhanced_world_map_control_server.json`
  - `GameServer/config/enhanced_world_map_control_server.json`
  - `config/enhanced_world_map_control_client.json`
  - `Assets/StreamingAssets/enhanced_world_map_control_client.json`
  - `config/world_map_control_profile.json`
  - `GameServer/config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`

## Protobuf Packet Protocol Review

- Descriptor fingerprint integrity remains valid.
- Required packet round-trip path is successful.
- Optional packet bindings still report warnings (non-blocking) for prototype/optional packet set:
  - `MultiBlockChange`, `InventoryUpdate`, `ItemUse`, `ItemDrop`, `ItemPickup`, `EntityUpdate`, `EntityInteract`, `ContainerOpen`, `ContainerClose`, `ContainerUpdate`

These warnings are expected under current optional-message policy and do not fail probe execution.

## Validation Evidence

- `dotnet build SharedProtocol/SharedProtocol.csproj` PASS
- `dotnet build GameCommon/GameCommon.csproj` PASS
- `dotnet build GameServer/GameServer.csproj` PASS
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` PASS
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1` PASS
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe` PASS
- `dotnet run --project GameServer/GameServer.csproj -- --selftest` PASS

## Using/Reference Integrity

- No compile-time missing-type or missing-namespace errors were detected in the updated projects.
- Updated `using` dependencies resolve successfully under the project build graph.

