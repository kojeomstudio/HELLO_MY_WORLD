# 2026-02-21 Session 107 Core / Content / Utility Feature List

## Metadata
- Session: 107
- Date: 2026-02-21
- Hydrology Signature: `2026-02-21-hydrology-riverlake-cave-v46`
- Map Control Profile Version: `50`
- Source Manifest: `config/minecraft_feature_client_server_core_content_util_2026-02-21-session-107.json`

## Core
1. `S107-CORE-01` Shared DLL Contracts (`GameCommon.dll`, `SharedProtocol.dll`)
2. `S107-CORE-02` World Map Profile v50 + Hydrology Signature v46
3. `S107-CORE-03` Server World-Map Inflight Timeout Guard
4. `S107-CORE-04` Client Chunk Queue Budget Guard (Runtime JSON)

## Content
5. `S107-CONTENT-01` Cave Groundwater Pressure-Relief Bridge
6. `S107-CONTENT-02` River Confluence Floodplain Relay Bridge
7. `S107-CONTENT-03` Lake Karst Outlet Stability Bridge
8. `S107-CONTENT-04` Hydrology-Coupled Terrain Pipeline (Cave/River/Lake)

## Utility
9. `S107-UTIL-01` Protobuf Registry and Descriptor Validation
10. `S107-UTIL-02` Dummy Protocol Client Guard v50
11. `S107-UTIL-03` Session 107 Runtime Feature Manifest Loader
12. `S107-UTIL-04` JSON Config Parity Sync (Server/Client)

## Sequential Implementation Order
1. Baseline check (`git status`, `git log`, plans refresh)
2. Feature inventory refresh and categorized manifest update
3. Terrain generation bridge updates (cave -> river -> lake)
4. Server/client world-map control architecture safety updates
5. Signature/profile guard bump (`v46` / `v50`) + config parity synchronization
6. Protobuf packet reference and dummy probe validation
7. Build/probe verification + docs update + commit/push
