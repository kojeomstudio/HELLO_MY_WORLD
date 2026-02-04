# 2026-02-04 Core / Content / Utility Feature List

**Source:** `config/minecraft_feature_core_content_util_2026-02-04.json`  
**Scope:** Minecraft-like client/server features grouped by Core, Content, Utility with implementation order.  
**Hydrology Signature:** `2026-02-04-hydrology-riverlake-v13` (SharedFeatureCatalog) / Map-control profile v15

## Core
- **CORE-017 (order 1)** Hydrology v13 worldgen seams - cave/river/lake continuity, updated MapGeneratorLib smoothing, refreshed world map profile.
- **CORE-018 (order 2)** World map control architecture/signature - GameCommon signature context, profile hash, StreamingAssets parity.
- **CORE-019 (order 3)** Shared DLL + protobuf contracts - GameCommon.dll + SharedProtocol registry validation and proto probe reports.

## Content
- **CONTENT-018 (order 4)** Cave ventilation + riparian sealing v13 - moisture dampening at entrances, seam-aware supports, riparian plug depth update.
- **CONTENT-019 (order 5)** River/lake seam stabilization - edge continuity blend, lake outflow taper, hydrology profile v15 mirrored to Unity.

## Utility
- **UTIL-017 (order 6)** Dummy protocol client/probe - packet matrix + hydrology-aware reporting, reference/probe JSON outputs.
- **UTIL-018 (order 7)** Config validation hooks - JSON-driven world/map-control/profile regeneration, StreamingAssets sync.

## Implementation Notes
- Profile/source: `config/world.json` - copied to `Assets/StreamingAssets/world-config.json`; profile regenerated to `config/world_map_control_profile.json` and mirrored to `Assets/StreamingAssets/world-map-control.json`.
- Shared DLLs: rebuild `GameCommon` + `MapGeneratorLib` and copy to `Assets/Plugins/` to keep Unity/server parity.
- Proto/dummy client: run `dotnet run --project GameServer -- --proto-probe` to refresh `reports/proto_probe_report.json` and `config/proto_reference_report.json`.
- Order: apply CORE items before CONTENT; rerun profile/proto probes after changing worldgen or hydrology parameters.
