# Minecraft Features (Client/Server) — Core/Content/Util

- Date: 2026-02-05 (Session 45)
- Hydrology signature: `2026-02-05-hydrology-riverlake-cave-v15`
- Profile version: 18
- Source data: `config/minecraft_feature_client_server_core_content_util_2026-02-05-session-45.json`

## Core
- **Shared (seq 1-2)**: world map control profile/version bump, hydrology signature, and shared DLL exports (`GameCommon/World/WorldMapControlProfile*`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`). Maintain profile hash parity and expose metadata through GameCommon.
- **Server (seq 3)**: hydrology-driven terrain masks with edge diffusion and cave ceiling divergence guard (`ImprovedTerrainCoordinator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`, `ImprovedCaveGenerator`).
- **Client (seq 4)**: Unity preview parity mirroring hydrology diffusion/ceiling guards (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`).

## Content
- **Shared (seq 5)**: river/lake continuity tuning driven by JSON (edge diffusion, seam stitching, curvature-aware flow) across `config/world.json`, `config/world_map_control_profile.json`, and `Assets/StreamingAssets/world-config.json`.

## Utilities
- **Shared (seq 6)**: protocol registry validation + diagnostics (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtoDiagnostics.cs`), including profile hash/signature emission in dummy probes.
- **Server (seq 7)**: dummy protocol client round-trip + packet checks with hydrology context (`GameServer/Testing/DummyProtocolClient.cs`, `reports/proto_probe_report.json`).

## Implementation Order (sequential)
1. Shared world map control profile/signature update (v15, hash refresh).
2. Shared DLL alignment (expose hydrology metadata, ensure using directives resolve to GameCommon).
3. Server terrain mask upgrades (edge diffusion, cave ceiling guard, river/lake seams).
4. Client map preview parity (mirror new hydrology steps in Unity + MapGeneratorLib).
5. Hydrology continuity tuning via JSON configs (world + streaming assets).
6. Protocol registry audit and probe report with profile metadata.
7. Dummy protocol client execution covering chunk packets and profile context.

## Data-Driven Notes
- All knobs live in JSON (`config/world.json`, `Assets/StreamingAssets/world-config.json`, `config/world_map_control_profile.json`); profile hash is recomputed via `GameCommon.World.WorldMapControlProfileUtility`.
- Feature catalog JSON: `config/minecraft_feature_client_server_core_content_util_2026-02-05-session-45.json` (client/server split + sequences). This file should be consulted before ordering implementation tasks.
