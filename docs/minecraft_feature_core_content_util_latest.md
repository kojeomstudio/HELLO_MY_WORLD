## Minecraft core/content/util rollout (2026-02-15)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-15.json` (phased ordering + owners). Update the JSON first, then mirror sequencing here.
- Data-driven world/protocol: keep `config/world.json`, `config/world_map_control_profile.json`, and Unity StreamingAssets (world-map-control + enhanced-terrain tuning) in sync.
- Ownership split: server worldgen/protocol in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`; client previews/network in `Assets/MyAssets/Scripts/GameWorld/*` and `Assets/Scripts/Minecraft/*`.

### Core (order)
1. `core.worldgen_masks` — hydrology/flow masks with variance blending that feed caves/rivers/lakes.
2. `core.worldmap_control` — hashed profile parity and reload between server JSON and Unity StreamingAssets.
3. `core.protocol_health` — protobuf registry/parser/fingerprint validation before handler wiring.

### Content (order)
1. `content.river_meanders` — tributary-friendly rivers with confluence boosts and edge feathering.
2. `content.lake_outflow` — inflow-aware lakes with downhill outflow hints and wetland padding.
3. `content.cave_stability` — hydrology/flow-dampened cave masks to avoid flooded seams.

### Utility (order)
1. `util.config_profiles` — JSON-first worldgen/map-control/enhanced-terrain configs and hash checks.
2. `util.proto_ci` — shared protobuf validation gates for server/client.
3. `util.tests_builds` — dotnet build + smoke validation for SharedProtocol/GameServer.
