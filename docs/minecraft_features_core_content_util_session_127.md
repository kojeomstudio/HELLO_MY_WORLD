# Minecraft Features Core/Content/Utility - Session 127

- Date: 2026-02-26
- Session: 127
- Signature: `2026-02-26-hydrology-riverlake-cave-v56`
- Map Control Profile Version: `60`

## Manifests

- `config/minecraft_feature_client_server_core_content_util_2026-02-26-session-127.json`
- `GameServer/config/minecraft_feature_client_server_core_content_util_2026-02-26-session-127.json`
- `Assets/StreamingAssets/minecraft_feature_client_server_core_content_util_2026-02-26-session-127.json`

## Core

1. Shared DLL baselines (`GameCommon.dll`, `SharedProtocol.dll`) aligned to session-127 constants.
2. Shared world-map queue budget helper (`ComputeQueueLimitFromBudget`) introduced.
3. Server/client map-control queue policy parity updated to shared helper path.
4. Feature manifest loading priority extended to session-127 in server startup.

## Content

1. Cave-river-lake recharge bridge pass added to terrain coordinator pipeline.
2. Hydrology parameter tuning refreshed for cave/river/lake coupling.
3. Queue runtime/profile JSON values synchronized to profile version 60.
4. Map-control profile regenerated and synchronized for server and client streaming assets.

## Utility

1. Proto reference drift guard extended in server dummy probe.
2. Standalone dummy client reference report drift verification added.
3. Protobuf freshness check rerun and verified (`verify_protobuf.ps1`).
4. Proto probe rerun with updated threshold and session-127 baseline.
5. Session-127 docs/plan/readme updates completed.

## Sequential Implementation Order

1. Plan initialization and git-history checkpointing.
2. Session-127 feature manifest publication and mirroring.
3. Shared baseline bump (`v56`/`60`) and queue policy helper update.
4. Terrain coupling algorithm pass implementation and parameter tuning.
5. Server/client world-map architecture parity updates.
6. Protobuf drift guard enhancements in dummy probe tools.
7. Profile generation + JSON synchronization across server/client roots.
8. Build/protobuf/probe validation and documentation refresh.
