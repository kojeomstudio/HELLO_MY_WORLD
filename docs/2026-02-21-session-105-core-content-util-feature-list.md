# 2026-02-21 Session 105 Core / Content / Utility Feature List

## Metadata
- Session: 105
- Date: 2026-02-21
- Hydrology Signature: `2026-02-21-hydrology-riverlake-cave-v45`
- Map Control Profile Version: `49`
- Source Manifest: `config/minecraft_feature_client_server_core_content_util_2026-02-21-session-105.json`

## Core
1. `S105-CORE-01` Shared DLL Contracts (`GameCommon.dll`, `SharedProtocol.dll`)
2. `S105-CORE-02` World Map Profile v49 + Hydrology Signature v45
3. `S105-CORE-03` Queue Hysteresis (Emergency Hold + Recovery Ramp)

## Content
4. `S105-CONTENT-01` Cave Flood-Bypass Vent Damping Bridge
5. `S105-CONTENT-02` River Confluence Lag Storage Bridge
6. `S105-CONTENT-03` Lake Spillway Backflow Damping Bridge

## Utility
7. `S105-UTIL-01` Dummy Protocol Probe Version Guard v49
8. `S105-UTIL-02` Runtime Feature Manifest Loader Session 105
9. `S105-UTIL-03` Config-Driven Queue Policy Extension

## Sequential Implementation Order
1. Baseline (`git status`, `git log`, plans refresh)
2. Feature inventory refresh (core/content/utility manifest)
3. Terrain generation bridges (cave -> river -> lake)
4. Server/client world-map queue hysteresis integration
5. Signature/profile bump (v45/v49) + JSON parity updates
6. Proto probe guard/version updates
7. Build/probe/selftest + document refresh + commit/push

