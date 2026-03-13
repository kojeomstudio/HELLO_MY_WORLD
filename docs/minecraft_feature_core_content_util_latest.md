# Minecraft Core/Content/Util Latest (2026-03-13, Session 165)

## Source of Truth
- `config/minecraft_feature_client_server_core_content_util_2026-03-13-session-165.json`
- `config/minecraft_features_client_server_core_content_util_2026-03-13-session-165.json`

## Core (v86/v90 baseline)
1. Shared DLL contracts (`GameCommon.dll`, `SharedProtocol.dll`) and shared enum/code boundary.
2. Protobuf registry, descriptor fingerprint, and generated source freshness validation.
3. World map control profile/version synchronization (`MapControlProfileVersion=90`).
4. Queue-policy runtime controls including `queueAlluvialRelayWeight` + `queueKarstSpillwayWeight`.

## Content (terrain)
1. Cave generation v86: subsurface spillway convergence bridge applied.
2. River generation v86: subsurface confluence stability bridge applied.
3. Lake generation v86: subsurface overflow balancing bridge applied.

## Utility (operations)
1. Data-driven JSON parity across `config/`, `GameServer/config/`, `Assets/StreamingAssets/`.
2. Dummy protocol probe client for required packet round-trip verification.
3. Build/test/proto-probe validation workflow for session delivery.
