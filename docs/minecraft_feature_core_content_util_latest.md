## Minecraft core/content/util rollout (2026-02-18)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-18.json` (ordered phases + owners).
- Data-driven: JSON configs mirrored between `config/` and StreamingAssets (world/map-control/enhanced terrain) with generation signatures to protect caches.
- Focus: flow-memory hydrology seams, seepage/outflow-friendly lakes, moisture-aware cave ceilings, refreshed world-map generation signature, and descriptor-origin protobuf guards.

### Core (order)
1. `core.flow_memory_hydrology` ??Hydrology + flow memory stitched across chunk edges so river/lake/cave masks stay continuous in server and Unity previews.
2. `core.worldmap_signature_sync` ??Generation signature blends world-config + map-control inputs so previews reload when configs drift.
3. `core.protocol_descriptor_origin` ??Validate EnhancedMinecraft bindings resolve from the generated descriptor/assembly and surface stale using directives early.

### Content (order)
1. `content.river_edge_repair` ??Flow-shadowed meanders and watershed edge blending keep river banks stable and stitched.
2. `content.lake_seepage_outflow` ??Lake basins use seepage + rim erosion + outflow channels guided by flow/hydrology gradients.
3. `content.cave_moisture_ceiling` ??Cave thresholds factor flow shadow and ceiling moisture clamps to seal seams under rivers/lakes.

### Utility (order)
1. `util.config_json_alignment` ??JSON parity for world + map-control configs shared by server and StreamingAssets with signatures.
2. `util.data_driven_worldgen` ??Hydrology/flow/cave/lake seam tuning exposed via JSON knobs.
3. `util.build_proto_validation` ??`dotnet build` for SharedProtocol/GameServer plus protobuf descriptor/handler validation.
