# Minecraft Features Core/Content/Util - Session 121 (2026-02-25)

## Scope
- Session: 121
- Signature: `2026-02-25-hydrology-riverlake-cave-v53`
- Map Control Profile Version: `57`
- Manifest:
  - `config/minecraft_feature_client_server_core_content_util_2026-02-25-session-121.json`
  - `GameServer/config/minecraft_feature_client_server_core_content_util_2026-02-25-session-121.json`

## Core
1. Shared baseline uplift (`HydrologySignature v53`, `MapControlProfileVersion 57`)
2. Server world-map profile baseline auto-heal architecture (`signature/version/hash drift guard`)

## Content
1. Hyporheic exchange relay pass for cave-river-lake hydrology continuity (server)
2. Matching hyporheic exchange relay pass in Unity enhanced terrain generation (client parity)
3. Hydrology continuity/lake inflow/river edge/groundwater/spill tuning in world JSON

## Utility
1. Proto probe descriptor coverage regression guard
2. Proto probe missing-generated-required-descriptor guard
3. Session 121 manifest priority load path in server bootstrap
4. Queue policy metadata synchronization for profile `v57`

## Server/Client/Shared Artifacts
- Server:
  - `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
  - `GameServer/World/WorldMapController.cs`
  - `GameServer/Testing/DummyProtocolClient.cs`
  - `GameServer/Program.cs`
  - `GameServer/config/world.json`
  - `GameServer/config/protocol_dummy_client.json`
  - `GameServer/config/world_map_control_profile.json`
- Client:
  - `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - `Assets/StreamingAssets/world-config.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `Assets/StreamingAssets/world_map_control_queue_policy.json`
- Shared DLL:
  - `GameCommon/World/SharedFeatureCatalog.cs`
  - `GameServer/World/WorldGenerationConfig.cs`

## Implementation Sequence
1. Create/update session plan in `plans/2026-02-25-session-121-comprehensive-work-plan.md`
2. Refresh Core/Content/Util manifest JSON
3. Apply terrain coupling improvements (server + client parity)
4. Apply world-map profile drift auto-heal architecture
5. Harden proto dummy probe and data-driven config guards
6. Build/test/protobuf validation and docs update
