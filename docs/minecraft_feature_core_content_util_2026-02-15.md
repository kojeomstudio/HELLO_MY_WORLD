## Minecraft core/content/util rollout (2026-02-15)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-15.json` (ordered phases + owners).
- Data-driven: world/map-control knobs stay mirrored between `config/world.json`, `config/world_map_control_profile.json`, and Unity StreamingAssets copies (world + enhanced terrain tuning).
- Focus: hydrology-aligned terrain (caves/rivers/lakes), world-map control parity, and protobuf registry validation before handlers wire up.

### Core (in order)
1. `core.worldgen_masks` — seam-safe hydrology/flow masks that feed caves, rivers, and lakes; variance blending + confluence awareness.
2. `core.worldmap_control` — render/simulation/water-level knobs stay hashed across server JSON and Unity StreamingAssets.
3. `core.protocol_health` — protobuf registry + parser/fingerprint validation on server bootstrap and Unity client network init.

### Content (in order)
1. `content.river_meanders` — tributary-friendly river pressure with confluence boosts and edge feathering.
2. `content.lake_outflow` — lake basins with inflow suppression near rivers, downhill outflows, and wetland padding.
3. `content.cave_stability` — hydrology/flow-dampened cave masks to avoid flooded seams and unstable ceilings.

### Utilities (in order)
1. `util.config_profiles` — JSON-first configs for worldgen/map-control/enhanced terrain; hash + reload where applicable.
2. `util.proto_ci` — shared protobuf validation gates (registry coverage, parser bindings, descriptor fingerprint).
3. `util.tests_builds` — dotnet build + smoke validation for SharedProtocol and GameServer; regen protobufs when proto changes.
