# 2026-02-08 Session 55: Minecraft Feature Core/Content/Utility Inventory

## Scope
- This inventory is the session-55 implementation set required for server/client Minecraft runtime continuity.
- Source of truth JSON: `config/minecraft_feature_client_server_core_content_util_2026-02-08-session-55.json`
- Shared hydrology signature: `2026-02-08-hydrology-riverlake-cave-v19`
- Map-control profile version: `23`

## Implementation Sequence
1. Core
2. Content
3. Utility

## Core Features
1. `S55-CORE-01` Shared world-map signature contract v19
2. `S55-CORE-02` Server-authoritative worldgen config v23
3. `S55-CORE-03` Unity world config parity for v19 controls

## Content Features
1. `S55-CONTENT-01` Catchment-aware river pressure
2. `S55-CONTENT-02` Spillway continuity tuned lakes
3. `S55-CONTENT-03` Aquifer barrier cave stabilization
4. `S55-CONTENT-04` Terrain tuning profile v23

## Utility Features
1. `S55-UTIL-01` Server map-control cache recency eviction
2. `S55-UTIL-02` Client preview queue budget control
3. `S55-UTIL-03` Feature manifest loader session-55 priority
4. `S55-UTIL-04` Protobuf runtime validation continuity

## Key Artifacts
- Server terrain/world-map:
  - `GameServer/World/WorldGenerationConfig.cs`
  - `GameServer/World/Generation/ImprovedRiverGenerator.cs`
  - `GameServer/World/Generation/ImprovedLakeGenerator.cs`
  - `GameServer/World/Generation/ImprovedCaveGenerator.cs`
  - `GameServer/World/WorldMapControlManager.cs`
- Client parity/runtime:
  - `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - `Assets/Scripts/Minecraft/Core/WorldConfig.cs`
  - `Assets/StreamingAssets/world-config.json`
  - `Assets/StreamingAssets/enhanced_world_map_control_client.json`
- Shared DLL contract:
  - `GameCommon/World/SharedFeatureCatalog.cs`
  - `GameCommon/World/WorldMapContracts.cs`
  - `GameCommon/World/WorldMapSignature.cs`
- Data-driven config:
  - `config/world.json`
  - `config/world_map_control_profile.json`
  - `config/enhanced_world_map_control_server.json`
  - `config/enhanced_world_map_control_client.json`

## Status
- All listed session-55 features are implemented and wired into startup manifest resolution.
