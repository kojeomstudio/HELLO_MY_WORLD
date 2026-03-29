# Session 115 Core/Content/Utility Feature List

**Date**: 2026-02-23  
**Session**: 115  
**Signature**: `2026-02-23-hydrology-riverlake-cave-v50`  
**Map-Control Profile Version**: `54`

## Core
- `S115-CORE-01` Hydrology signature/profile version synchronization (`GameCommon/World/SharedFeatureCatalog.cs`, profile JSONs)
- `S115-CORE-02` Shared DLL contracts and protocol references (`GameCommon`, `SharedProtocol`, `GameServer` project references)
- `S115-CORE-03` Focus-aware inflight stale pruning for server world-map control (`GameServer/World/WorldMapControlManager.cs`)

## Content
- `S115-CONTENT-01` Seasonal runoff coupling parity pass added to Unity terrain preview pipeline (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`)
- `S115-CONTENT-02` Hydrology-coupled cave/river/lake generation pipeline retained and aligned with profile/signature v50 (`GameServer/World/Generation/*`)

## Utility
- `S115-UTIL-01` Dummy protocol probe profile-version guard raised to v54 (`GameServer/Testing/DummyProtocolClient.cs`, `config/protocol_dummy_client.json`)
- `S115-UTIL-02` Queue policy description/runtime config updated for focus-aware stale pruning (`config/world_map_control_queue_policy.json` and mirrors)
- `S115-UTIL-03` Session 115 plans/docs/data-driven manifest synchronized (`plans/`, `docs/`, `config/`)

## Data-Driven Artifacts
- `config/minecraft_feature_client_server_core_content_util_2026-02-23-session-115.json`
- `GameServer/config/minecraft_feature_client_server_core_content_util_2026-02-23-session-115.json`

