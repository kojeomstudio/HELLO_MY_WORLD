# 2026-02-24 Session 117 Core/Content/Util Feature List

- Signature: `2026-02-24-hydrology-riverlake-cave-v51`
- Map-Control Profile Version: `55`
- Manifest: `config/minecraft_feature_client_server_core_content_util_2026-02-24-session-117.json`

## Core
- `S117-CORE-01` Hydrology signature/profile version alignment (server+client shared profile pipeline).
- `S117-CORE-02` World-map queue hotspot retention architecture (`queueHotspotRetentionSeconds`) across server/client and runtime JSON.

## Content
- `S117-CONTENT-01` Floodplain-confluence hydrology feedback update for river/lake coupling.
- `S117-CONTENT-02` Riparian cave buffer improvement with subsurface pressure + aquifer barrier weighting.

## Utility
- `S117-UTIL-01` Dummy protobuf probe reference-report drift guard.
- `S117-UTIL-02` Session plan/manifest/docs synchronization.

## Sequential Implementation Order
1. Plan and commit-history gap analysis.
2. Queue architecture update on server/client.
3. Terrain cave/river/lake coupling algorithm pass.
4. Protobuf reference guard update.
5. Signature/version + config synchronization.
6. Build/test/proto validation.
7. Documentation + commit/push.
