# Minecraft Core/Content/Util Latest (2026-03-14, Session 168)

## Source of Truth
- `config/minecraft_feature_client_server_core_content_util_2026-03-14-session-168.json`
- `config/minecraft_features_client_server_core_content_util_2026-03-14-session-168.json`

## Core (v88/v92 baseline)
1. Shared DLL contracts (`GameCommon.dll`, `SharedProtocol.dll`) and shared enum/code boundary.
2. Protobuf registry, descriptor fingerprint, and generated source freshness validation.
3. World map control profile/version synchronization (`MapControlProfileVersion=92`).
4. Queue-policy runtime controls including `queueAlluvialRelayWeight` + `queueKarstSpillwayWeight` + `queueHyporheicExchangeWeight` + `queuePhreaticResonanceWeight`.

## Content (terrain)
1. Cave generation v88: phreatic resonance vault bridge applied.
2. River generation v88: phreatic resonance relay bridge applied.
3. Lake generation v88: phreatic resonance storage bridge applied.

## Utility (operations)
1. Data-driven JSON parity across `config/`, `GameServer/config/`, `Assets/StreamingAssets/`.
2. Dummy protocol probe client for required packet round-trip verification.
3. Build/test/proto-probe validation workflow for session delivery.
