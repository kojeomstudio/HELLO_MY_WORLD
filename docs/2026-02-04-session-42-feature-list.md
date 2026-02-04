# Session 42 Feature Matrix (Core/Content/Util)

**Date:** 2026-02-04  
**Scope:** Client + Server (core/content/util)  
**Reference Commits:** 3d16634d, 24c484fb, 0118644c  
**Hydrology Signature:** 2026-02-04-hydrology-riverlake-v13

## Core
- **Worldgen Hydrology v14 (caves/rivers/lakes)** — Server: `GameServer/World/Generation/*` (Improved* generators, coordinator, pipeline), Client: `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`, Data: `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`.  
  - *Sequence:* 1) Stabilize river/lake seams; 2) Blend cave hydrology/erosion; 3) Recompute map-control profile hash.
- **World Map Control Architecture (shared DLL)** — Shared: `GameCommon/World/*`, Server: `GameServer/World/WorldMapControlManager.cs`, Client: `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs`.  
  - *Sequence:* 1) Introduce shared profile/contracts in GameCommon; 2) Update server loaders; 3) Wire client/world preview to shared signatures.
- **Protocol Contracts & Registry** — Shared: `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, Generated DTOs in `Assets/Generated/Protobuf/`, Server handlers under `GameServer/Handlers/`, Dummy probe `GameServer/Testing/DummyProtocolClient.cs`.  
  - *Sequence:* 1) Validate registry vs generated descriptors; 2) Extend dummy probes (config-driven); 3) Regenerate protobuf outputs if drift detected.

## Content
- **Terrain Content (cave ventilation, river/lake continuity)** — Server masks in `ImprovedCaveGenerator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`; Client preview in `EnhancedTerrainGenerator.cs`; Data overrides in `config/world.json`.  
  - *Sequence:* 1) Apply seam-aware smoothing; 2) Tune lake outflow taper; 3) Boost cave moisture guards.
- **World Map Preview Parity** — Client controllers `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs`; Shared signatures `GameCommon/World/WorldMapSignature.cs`.  
  - *Sequence:* 1) Load shared profile; 2) Validate signature against proto fingerprint; 3) Persist profile JSON for StreamingAssets.

## Utilities
- **Dummy Protocol Client & Packet Matrix** — `GameServer/Testing/DummyProtocolClient.cs`, config `config/protocol_dummy_client.json`, reports `reports/proto_probe_report.json`.  
  - *Sequence:* 1) Add hydrology/profile context to probe; 2) Validate optional bindings; 3) Gate network probe behind config.
- **Data/Config (JSON-driven)** — Server configs `config/server_config.json`, `config/world.json`, client configs `config/client_config.json`, data-driven world map control `config/world_map_control_profile.json`.  
  - *Sequence:* 1) Keep overrides in JSON; 2) Mirror shared DLL signature; 3) Track environment defaults in config files.

## Implementation Order (today)
1. Refresh feature JSON (`config/minecraft_feature_client_server_core_content_util_2026-02-04-session-42.json`) to reflect the above matrix.
2. Improve worldgen masks (cave/river/lake) and recompute stability hooks used by map control.
3. Introduce shared GameCommon world map control profile + server/client loaders; align proto dummy probe with shared data.
4. Update docs/README, run builds/tests, and push.

## Notes
- Maintain data-driven configs (JSON) for both client and server; avoid hard-coded tuning constants.
- Shared DLL (`GameCommon.dll`) is the source of truth for world map contracts/enums to prevent drift across Unity and server runtimes.
- Proto validation must include registry audit, optional message coverage, and descriptor fingerprint checks via `DummyProtocolClient`.
