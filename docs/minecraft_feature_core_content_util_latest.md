## Minecraft core/content/util rollout (2026-01-03)
- Source of truth: `config/minecraft_feature_core_content_util_2026-01-03.json` (phased ordering + owners).
- Data-driven world/protocol: keep `config/world.json`, `config/world_map_control_profile.json`, and Unity StreamingAssets (world + map-control JSON) in sync with hot reload on both sides.
- Ownership split: server worldgen/protocol in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`; client previews/network in `Assets/MyAssets/Scripts/GameWorld/*` and `Assets/Scripts/Minecraft/*`.

### Core (order)
1. `core.worldgen_masks` — basin-fill + edge-stitched hydrology/flow masks that feed caves/rivers/lakes.
2. `core.worldmap_control` — reload map-control/world-config JSON on server + Unity, rebuilding pipelines and caches on hash drift.
3. `core.protocol_health` — EnhancedMinecraft descriptor filename guard plus registry/parser/fingerprint validation.

### Content (order)
1. `content.river_corridors` — flow-aligned river pressure with directional smoothing and seam stitching.
2. `content.lake_stability` — lake basins damp gradients, cushion seams, and carve outflows with shared masks.
3. `content.cave_edge_stability` — hydrology-gradient aware cave thresholds to avoid flooded seams.

### Utility (order)
1. `util.data_parity` — JSON-first world/map-control configs mirrored to StreamingAssets.
2. `util.proto_ci` — descriptor filename validation + registry/parser coverage for EnhancedMinecraft protobufs.
3. `util.tests_builds` — dotnet build + protobuf smoke validation for SharedProtocol/GameServer.
