## Minecraft core/content/util rollout (2026-01-04)
- Source of truth: `config/minecraft_feature_core_content_util_2026-01-04.json` (phased ordering + owners).
- Focus: directional flow-shadow hydrology, seam-safe river/lake/cave masks, map-control drift guards, and parser round-trip validation.

### Core (order)
1. `core.flow_shadow_directional` — Blend hydrology with downhill-aware flow shadows so caves/rivers/lakes share stitched masks across chunks.
2. `core.worldmap_profile_guard` — Regenerate hashed world-map control profiles when `world.json` changes on server or StreamingAssets and reload cached previews.
3. `core.protocol_parser_guard` — Parser/descriptor round-trip checks to ensure EnhancedMinecraft generated DTOs match registry bindings.

### Content (order)
1. `content.river_seam_flow` — Gradient-aware river pressure with seam guards, tributary boosts, and flow-shadow modulation.
2. `content.lake_wetland_guard` — Shoreline jitter + hydrology-gradient dampening for lakes with downhill outflows and wetland padding.
3. `content.cave_flow_resilience` — Flow-gradient penalties and seam-aware stability smoothing to keep caves resilient near rivers/wetlands.

### Utility (order)
1. `util.config_parity` — Keep world/map-control JSON mirrored between server config and StreamingAssets with hashed profiles.
1. `util.proto_validation` — Run parser/descriptor round-trip checks via `ProtocolStandardization` and `scripts/verify_protobuf.ps1`.
1. `util.build_tests` — `dotnet build` for SharedProtocol/GameServer and protobuf smoke validation.
