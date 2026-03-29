# 2026-02-19 Session 99 Minecraft Feature Catalog (Core/Content/Utility)

## Core
1. Shared DLL contracts (`GameCommon`, `SharedProtocol`) for client/server common enum and code sharing
2. World-map profile/signature synchronization (`v46`, `hydrology v42`) across server + Unity StreamingAssets
3. Queue shock-absorber architecture in shared policy + server + client controllers
4. JSON-driven runtime queue policy/config governance (`enhanced_world_map_control_*`, `world_map_control_queue_policy.json`)

## Content
1. Karst/confluence retention field added to server terrain coordinator
2. Karst/confluence retention field mirrored in Unity preview terrain generator
3. Sink-stability + floodplain leakage continuity pass retained and chained with new retention stage
4. Improved cave/river/lake coupling pipeline validation for chunk continuity

## Utility
1. Protobuf registry/descriptor/binding validation paths (`ProtocolValidator`, `ProtoDiagnostics`, dummy probes)
2. Dummy client minimum map-control profile version guard (`>=46`) and signature guard
3. Data-driven JSON game data/config validation (`blocks`, `items`, `biomes`, runtime config files)

## Implementation Sequence (Applied)
1. Session plan and git-history gap analysis
2. Queue architecture hardening (shock-absorber)
3. Terrain algorithm convergence pass update (server + client mirror)
4. Profile/config/signature/version synchronization
5. Protobuf probe and dummy client version guard update
6. Build/protocol/using reference validation
7. Documentation and commit/push

## Source Files
- JSON source of this catalog: `config/minecraft_feature_client_server_core_content_util_2026-02-19-session-99.json`
