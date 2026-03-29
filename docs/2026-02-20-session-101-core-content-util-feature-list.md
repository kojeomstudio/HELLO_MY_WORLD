# 2026-02-20 Session 101 Minecraft Feature List (Core / Content / Utility)

## Scope
- Signature: `2026-02-20-hydrology-riverlake-cave-v43`
- Map profile version: `47`
- Source manifest: `config/minecraft_feature_client_server_core_content_util_2026-02-20-session-101.json`

## Core
1. Shared DLL contracts (`GameCommon.dll`, `SharedProtocol.dll`) maintained as client/server common dependency.
2. World-map control profile upgraded to v47 with new hydrology/cave/lake control fields.
3. World-map generation signature context extended for tributary/avulsion/spill-retention/ventilation drift detection.

## Content
1. Terrain algorithm upgrade: `ApplySubsurfaceVentilationRetentionField` added to cave/river/lake coupling pipeline.
2. River behavior controls expanded: tributary capture + avulsion resistance included in profile/config propagation.
3. Lake/cave controls expanded: spill retention + groundwater connectivity + cave ventilation included in profile/config propagation.

## Utility
1. Protobuf packet validation flow verified via `ProtocolRegistry`/`ProtocolValidator` + dummy client probe.
2. Runtime feature manifest loader upgraded to prioritize Session 101 JSON manifest.
3. Dummy-client guards updated to require map-control profile v47 (`config/protocol_dummy_client.json`, `config/dummy_minecraft_client.json`).

## Implementation Sequence (Executed)
1. Plan refresh + git history review.
2. Shared contracts/profile/signature field expansion.
3. Terrain generation algorithm refinement and application.
4. Server/client world-map control integration updates.
5. Protobuf probe + selftest validation.
6. Documentation + config manifest refresh.
