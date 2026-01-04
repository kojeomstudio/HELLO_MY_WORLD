## Minecraft core/content/util rollout (2026-01-04)
- Source of truth: `config/minecraft_feature_core_content_util_2026-01-04.json` (phased ordering + owners).
- Focus: directional flow-shadow hydrology, seam-safe river/lake/cave masks, map-control profile drift guards, and parser round-trip validation.

### Core (order)
1. `core.flow_shadow_directional` — Downhill-aware hydrology + flow shadows that keep caves/rivers/lakes stitched across chunks.
2. `core.worldmap_profile_guard` — Hash/regenerate world-map control profiles when `world.json` changes on server or StreamingAssets.
3. `core.protocol_parser_guard` — Parser/descriptor round-trip checks to catch stale EnhancedMinecraftProtocol bindings.

### Content (order)
1. `content.river_seam_flow` — Gradient-aware river pressure with seam guards and tributary boosts.
2. `content.lake_wetland_guard` — Shoreline jitter + hydrology-gradient dampening for lakes with downhill outflows.
3. `content.cave_flow_resilience` — Flow-gradient penalties and seam-aware stability smoothing for caves near rivers/wetlands.

### Utility (order)
1. `util.config_parity` — Mirror world/map-control JSON between server config and StreamingAssets with hashes.
2. `util.proto_validation` — Run `ProtocolStandardization` parser/descriptor checks and `scripts/verify_protobuf.ps1`.
3. `util.build_tests` — `dotnet build` for SharedProtocol/GameServer and protobuf smoke validation.
