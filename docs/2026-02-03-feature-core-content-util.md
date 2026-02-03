# 2026-02-03 Core / Content / Utility Feature List

**Source:** `config/minecraft_feature_core_content_util_2026-02-03-session-40.json`  
**Scope:** Minecraft-like client/server features grouped by Core, Content, Utility with implementation order.  
**Hydrology Signature:** `2026-02-03-hydrology-riverlake-v12` (SharedFeatureCatalog) / Map-control profile v14

## Core
- **CORE-015 (order 1)** Hydrology v12 map-control + profile sync – update hydrology smoothing/seam stability, regenerate map-control profile (v14), mirror to Unity.
- **CORE-016 (order 2)** Shared DLL + protobuf contract alignment — keep GameCommon.dll enums and SharedProtocol DTOs aligned; refresh proto reports.

## Content
- **CONTENT-015 (order 1)** Cave hydrology continuity + ventilation tuning — apply moisture clamps/riparian sealing in cave carving.
- **CONTENT-016 (order 2)** River and lake seam stabilization — improve meanders, buffers, and hydrology edge damping for chunk seams.
- **CONTENT-017 (order 3)** Unity map-control parity — ensure StreamingAssets and controllers reload refreshed hydrology signatures.

## Utility
- **UTIL-015 (order 1)** Dummy protocol client coverage + probes — expand packet matrix, emit probe/reference reports.
- **UTIL-016 (order 2)** Documentation + plans — docs/README updates for today’s worldgen/proto changes and plan links.

## Implementation Notes
- Build order: finish CORE items before CONTENT; run proto registry validation and dummy client probe after code changes.
- Data-driven configs stay JSON (`config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`).
- Shared artifacts: `GameCommon/World/SharedFeatureCatalog.cs`, `SharedProtocol/*`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`.
- Regeneration: `dotnet run --project GameServer -- --generate-map-profile` mirrors updated profile to StreamingAssets and refreshes proto reports.
