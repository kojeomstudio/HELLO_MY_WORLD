## Minecraft core/content/util rollout (2026-01-03)
- Source of truth: `config/minecraft_feature_core_content_util_2026-01-03.json` (ordered phases + owners).
- Data-driven: world/map-control JSON stays mirrored between server (`config/world.json`, `config/world_map_control_profile.json`) and Unity StreamingAssets copies with hot reload in both controllers.
- Focus: seam-safe hydrology masks (basin fill + edge stitching), world-map control reload, and EnhancedMinecraft descriptor validation.

### Core (in order)
1. `core.worldgen_masks` — fill basins, stitch edges, and shadow hydrology by flow so caves/rivers/lakes share stable masks.
2. `core.worldmap_control` — reload map-control/world-config JSON on server + Unity, rebuilding pipelines and caches when hashes drift.
3. `core.protocol_health` — ensure EnhancedMinecraft descriptors come from `enhanced_minecraft_game.proto` with registry/parser coverage.

### Content (in order)
1. `content.river_corridors` — flow-aligned river pressure with directional smoothing and seam stitching.
2. `content.lake_stability` — lake basins damp gradients, cushion seams, and carve outflows with shared masks.
3. `content.cave_edge_stability` — hydrology-gradient aware cave thresholds to avoid flooded seams and unstable ceilings.

### Utilities (in order)
1. `util.data_parity` — JSON config parity for world + map-control across server and client StreamingAssets.
1. `util.proto_ci` — descriptor filename guard + registry/parser validation for EnhancedMinecraft protobufs.
1. `util.tests_builds` — dotnet builds for SharedProtocol/GameServer plus protobuf smoke validation.
