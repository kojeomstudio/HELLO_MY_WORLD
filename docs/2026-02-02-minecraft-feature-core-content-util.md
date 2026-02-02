# 2026-02-02 Minecraft Features (Core / Content / Util)

**Sources:** `config/minecraft_feature_core_content_util_2026-02-02.json`, `plans/2026-02-02-plan.md`, git log up to `997a1850`.

## Core (server/shared)
- **CORE-012 – World map control profile sync** (order 1, in-progress): keep map-control hashes/version aligned between server exports and Unity StreamingAssets; enforce hydrology signature from `SharedFeatureCatalog`.
- **CORE-013 – Shared DLL & protocol contract alignment** (order 2, in-progress): GameCommon.dll + SharedProtocol.dll stay the single source for enums/DTOs consumed on both sides; keep proto reference report updated.
- **CORE-014 – Hydrology v9 seam-locked rivers/lakes/caves** (order 3, planned): refresh hydrology signature with river seam filling, meander jitter controls, and lake variance/outflow stability tuned for server map-control and Unity previews.

## Content (client/shared gameplay)
- **CONTENT-012 – Hydrology-aware terrain previews** (order 1, in-progress): Unity terrain generator respects JSON thresholds, river warp/smoothing, lake suppression near rivers, and cave sealing driven by map-control config.
- **CONTENT-013 – World map control stability** (order 2, in-progress): reload map-control cache on config/profile hash drift and reuse hydrology signatures for overlays to avoid server/Unity drift.
- **CONTENT-014 – Unity parity for hydrology v9** (order 3, planned): apply seam-filled river masks, lake variance/outflow stability tuning, and cave sealing tweaks in Unity previews.

## Utility (tooling/docs)
- **UTIL-012 – Dummy protocol client coverage expansion** (order 1, in-progress): broaden packet probe set, emit richer reports, and fail fast on missing registry bindings.
- **UTIL-014 – Protocol registry + dummy probe hardening** (order 2, planned): harden registry validation, expand dummy client packet matrix, and refresh proto probe reports.
- **UTIL-013 – Documentation & plan refresh** (order 3, planned): update docs/README with categorization, worldgen/proto validation notes, and session outputs.

## Ordering & Dependencies
- Core unlocks Content (CORE-012/013 precede CONTENT-012/013/014; CORE-014 builds on CORE-012).
- Protocol/report work (UTIL-012/014) depends on SharedProtocol and feeds docs (UTIL-013).

## Today’s Focus
- Bump hydrology signature to v9 with seam-filled river masks, lake variance/outflow stability parameters, and cave sealing tweaks (CORE-014, CONTENT-014).
- Verify GameCommon/SharedProtocol DLL usage for shared enums/contracts and refresh proto reference reporting (CORE-013, UTIL-014).
- Expand dummy client packet probes and regenerate reports while keeping docs/README in sync (UTIL-012/013).
