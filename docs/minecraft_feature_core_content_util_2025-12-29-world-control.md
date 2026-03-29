## Core/Content/Util feature map (server & client)

Date: 2025-12-29  
Purpose: capture Minecraft-style features we must deliver next, grouped by Core / Content / Util for both server and client. Items are ordered in the sequence we plan to implement; today’s work covers the highlighted entries.

| Category | Server feature (target/owner) | Client feature (target/owner) | Data/Config | Status/sequence |
| --- | --- | --- | --- | --- |
| Core | World map control service that streams hashed world map profile + chunk data (`GameServer/World/WorldMapController.cs`, `EnhancedTerrainGenerationPipeline`) | Unity world map controller that consumes the profile hash + chunk payloads for preview and culling (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`) | `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json` | In progress (1) |
| Core | Hydrology-aware terrain pipeline with seam-stable height, river and lake channels; caves gated by world seed (`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`) | Mirrored terrain generator for previews with the same knobs and thresholds | `WorldGenerationConfig` (server) ↔ `WorldMapControlProfile` (client) | In progress (2) |
| Content | Cave tuning: regional worm tunnels + flooded pockets using `Caves.*` thresholds; support pillars and ceiling stability | Cave masks mirrored for chunk previews; wet caves respect water table to avoid floating lakes | `Caves.*` JSON fields | Planned (3) |
| Content | River/lake refinement: flow-aligned smoothing, basin carving, shoreline feathering, river mouth deltas | River/lake visualization shares river intensity mask; lakes suppressed near river seams | `Water.*`, `Lakes.*` JSON fields | Planned (4) |
| Util | Protobuf contract guard: handler binding check and registry validation for EnhancedMinecraft payloads | Client consumes EnhancedMinecraft proto payloads for chunk/world info without manual toggles | `proto/enhanced_minecraft_game.proto`, generated C# bindings | Planned (5) |
| Util | Config parity automation: regenerate and persist map control profile hash whenever world JSON changes; keep StreamingAssets copy in sync | StreamingAssets profile read-only; warns when profile hash mismatches server-provided value | `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json` | Planned (6) |

Notes:
- Core steps (1) and (2) are executed first; Content/Util items (3)-(6) follow once the shared profile and pipeline are stable.
- All features are data-driven through JSON configs; no hard-coded seeds or thresholds should remain in code paths touched in this cycle.
