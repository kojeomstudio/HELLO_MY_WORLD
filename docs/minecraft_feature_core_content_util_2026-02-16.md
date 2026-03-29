## Minecraft core/content/util rollout (2026-02-16)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-16.json` (ordered phases + owners).
- Data-driven: world + enhanced-terrain knobs stay mirrored between `config/world.json`, `config/world_map_control_profile.json`, and Unity StreamingAssets (world/map-control/enhanced terrain) with hash validation.
- Focus: flow-shadow hydrology masks that stitch caves/rivers/lakes, hashed map-control reloads, and proto descriptor-origin guards.

### Core (in order)
1. `core.hydrology_flow_shadow` – flow-shadow hydrology masks drive caves/rivers/lakes with variance damping and confluence-aware boosts.
2. `core.worldmap_control` – render/simulation/water/terrain tuning hashed across server JSON and Unity StreamingAssets with reloads on drift.
3. `core.protocol_guard` – protobuf registry + parser/fingerprint + descriptor-origin validation before handlers wire up.

### Content (in order)
1. `content.river_meanders` – flow-shadowed river pressure with meander jitter, confluence boosts, and seam feathering.
2. `content.lake_shore_complexity` – shoreline complexity + wetland padding + downhill outflows tied to hydrology stability masks.
3. `content.cave_resilience` – cave thresholds dampened by flow shadows and moisture-biased support pillars to avoid flooded seams.

### Utilities (in order)
1. `util.config_profiles` – JSON-first world/map-control/enhanced-terrain configs; hash + reload + StreamingAssets parity.
2. `util.proto_ci` – shared protobuf validation gates (registry coverage, parser bindings, descriptor origin/fingerprint).
3. `util.tests_builds` – dotnet build + smoke validation for SharedProtocol/GameServer; regen protobufs when proto changes.
