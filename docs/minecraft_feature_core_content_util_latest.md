## Minecraft core/content/util rollout (2026-02-16)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-16.json` (phased ordering + owners).
- Data-driven world/protocol: keep `config/world.json`, `config/world_map_control_profile.json`, enhanced-terrain tuning, and Unity StreamingAssets (world + map-control + enhanced) in sync with hash validation + hot reload.
- Ownership split: server worldgen/protocol in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`; client previews/network in `Assets/MyAssets/Scripts/GameWorld/*`, `Assets/Scripts/Minecraft/*`, and `MapGeneratorLib/...`.

### Core (order)
1. `core.hydrology_flow_shadow` – flow-shadow hydrology/flow masks that feed caves/rivers/lakes with variance damping.
2. `core.worldmap_control` – map-control/world/enhanced JSON stay hashed across server + Unity StreamingAssets with reload on drift.
3. `core.protocol_guard` – registry + parser + fingerprint + descriptor-origin validation before handlers wire up.

### Content (order)
1. `content.river_meanders` – flow-shadowed river pressure with meander jitter, confluence boosts, and seam feathering.
2. `content.lake_shore_complexity` – shoreline complexity + wetland padding + downhill outflows tied to shared hydrology masks.
3. `content.cave_resilience` – hydrology/flow-dampened cave thresholds with moisture-biased supports.

### Utility (order)
1. `util.config_profiles` – JSON-first world/map-control/enhanced configs mirrored to StreamingAssets with hash parity.
2. `util.proto_ci` – descriptor origin + registry/parser coverage gates for EnhancedMinecraft protobufs.
3. `util.tests_builds` – dotnet builds + protobuf smoke validation for SharedProtocol/GameServer.
