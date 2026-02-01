# 2026-02-02 Minecraft Features (Core / Content / Util)

**Sources:** `config/minecraft_feature_core_content_util_2026-02-02.json`, git log up to `d6e35598`.

## Core
- **CORE-012 – World map control profile sync + hydrology signature persistence** (Shared, order 1, planned)  
  Keep map-control hashes/version aligned between server exports and Unity StreamingAssets; ensure hydrology signature matches `SharedFeatureCatalog.HydrologySignature`.
- **CORE-013 – Shared DLL and protocol contract alignment** (Shared, order 2, planned)  
  GameCommon.dll + SharedProtocol.dll stay the single source for enums/DTOs used across server/client builds; tie into proto reference report.

## Content
- **CONTENT-012 – Client terrain hydrology-aware caves/rivers/lakes** (Client, order 1, planned)  
  Unity terrain generator respects JSON thresholds, river warp/smoothing, lake suppression near rivers, and cave sealing driven by config.
- **CONTENT-013 – Server/client world map control stability** (Shared, order 2, planned)  
  Reload map-control cache on config/profile hash drift and reuse hydrology signatures for overlays to avoid drift between server and Unity.

## Utility
- **UTIL-012 – Dummy protocol client coverage expansion** (Shared, order 1, planned)  
  Broaden packet probe set, emit richer reports, and fail fast on missing registry bindings.
- **UTIL-013 – Documentation and plan refresh** (Shared, order 2, planned)  
  Update docs/README with categorization, worldgen/proto validation notes, and session S35 pointers.

## Ordering & Dependencies
- Core tasks unblock Content (CORE-012 → CORE-013 → CONTENT-012/013).  
- Protocol/report work (UTIL-012) depends on SharedProtocol; docs (UTIL-013) consolidate outcomes from CONTENT-012/013 and UTIL-012.

## Next Steps (S35)
- Wire `TerrainGenerator` to new hydrology-aware tuning and rerun builds.  
- Persist server map-control profiles to StreamingAssets and surface signature/hash drift in UI.  
- Expand dummy client packet list and re-run proto probe to regenerate `reports/proto_probe_report.json`.  
- Capture results in session report + README updates.
