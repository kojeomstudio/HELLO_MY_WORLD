Minecraft Features (Core/Content/Util) — 2026-01-13
====================================================

Snapshot
- Date: 2026-01-13
- Branch: master
- Head: `973edf61` (fix: resolve using statement issues and document implementation work)
- JSON source: `config/minecraft_feature_client_server_core_content_util_2026-01-13-session.json`

Core
- Server
  - World-map control resilience (`GameServer/World/WorldMapController.cs`, `GameServer/World/WorldMapControlManager.cs`): reload on profile/config hash drift, stable generation signature, cleanup of idle chunk caches.
  - Hydrology envelope sync (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `ImprovedCaveGenerator.cs`): reuse the same hydrology/flow masks across rivers, lakes, and caves to keep seams stable.
  - Proto registry guardrails (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtocolValidator.cs`, `GameServer/Program.cs`): validate generated EnhancedMinecraft DTO coverage before handlers bind.
- Client
  - Map-control hash tracking (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `WorldMapControlProfile.cs`, `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs`): reload previews when world config or profile hashes change; honor server generation signatures.
  - Hydrology preview envelope (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`): apply river/lake/cave smoothing in previews to mirror server masks.

Content
- Server
  - River delta/wetland balance (`ImprovedRiverGenerator.cs`): channel pressure shaped by flow memory, edge stabilization, and downstream seepage.
  - Lake outflow equalization (`ImprovedLakeGenerator.cs`): basin shaping tied to hydrology variance, outflow channels, and wetland buffers near rivers.
  - Riparian cave stability (`ImprovedCaveGenerator.cs`): suppress moist ceilings near hydrology seams, seal edges, and bias pillars toward saturated terrain.
- Client
  - Preview river/lake sync (`WorldGenAlgorithms.cs`, `WorldMapController.cs`): use the same hydrology fields to avoid seam mismatches.
  - Preview cave stability (`WorldGenAlgorithms.cs`): fold hydrology/flow suppression into cave masking for client preview chunks.

Util
- Server: JSON config parity (`config/enhanced_world_map_control_server.json`, `config/world.json`) and proto telemetry (`ProtoDiagnostics`, `ProtocolValidator`).
- Client: StreamingAssets parity for map-control + world config (`Assets/StreamingAssets/world-map-control.json`, `Assets/StreamingAssets/world-config.json`).

Sequencing (current session)
1. Hydrology/flow envelope alignment across caves, rivers, and lakes (server/client).
2. River delta + wetland balance (server).
3. Lake basin/outflow equalization (server).
4. Riparian cave stability (server).
5. World map control resilience (server).
6. Map-control hash tracking (client).
7. Hydrology envelope mirrored in client previews (client).
8. Proto registry guardrails (server).
9. JSON config parity (server/client).
10. StreamingAssets sync (client).
11. Proto telemetry (server).
