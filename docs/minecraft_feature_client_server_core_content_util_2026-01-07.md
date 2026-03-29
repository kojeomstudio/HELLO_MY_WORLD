# Minecraft Feature Matrix (Client + Server) — 2026-01-07

Source of truth is `config/minecraft_feature_client_server_core_content_util_2026-01-07.json`. Items are ordered for sequential implementation.

## Core
- **world-map-control-edge-normalization** — Server (`GameServer/World/WorldMapControlManager.cs`, `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`) and client (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`) share a hydrology edge-normalization pass. Generation signatures should expose the new weights so previews and dedicated server masks stay aligned.
- **hydrology-flow-memory** — Server (`ImprovedTerrainCoordinator` + river/lake/cave generators) and Unity `EnhancedTerrainGenerator` blend hydrology with cached flow accumulation across chunk seams, keeping water features continuous.

## Content
- **seam-aware-rivers** — `ImprovedRiverGenerator` + `EnhancedTerrainGenerator` apply meander smoothing, edge tangents, and depth continuity to avoid seam artifacts.
- **wetland-stable-lakes** — `ImprovedLakeGenerator` + `EnhancedTerrainGenerator` use rim stabilization, inflow-weighted depth, and hydrology variance-based wetland buffers.
- **moisture-safe-caves** — `ImprovedCaveGenerator` + `EnhancedTerrainGenerator` clamp carving near saturated ceilings, add moisture-aware supports, and tighten river suppression with the new edge weights.

## Util
- **config-sync** — Mirror the new hydrology edge/flow-memory knobs across `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, and `Assets/StreamingAssets/world-map-control.json` so both runtimes stay data-driven.
- **proto-registry-validation** — `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` (wired from `GameServer/Program.cs` and Unity `GameNetworkManager`) validates Google.Protobuf bindings against expected descriptor/assembly origins and using directives to catch stale generated packets early.
