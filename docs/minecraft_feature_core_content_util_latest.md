## Minecraft core/content/util rollout (2026-02-17)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-17.json` (ordered phases + owners).
- Data-driven: JSON configs mirrored between `config/` and StreamingAssets (world/map-control/enhanced terrain) with hashes for parity.
- Focus: watershed-aware hydrology seam stitching, braided river/wetland integration, map-control hash streaming, and stronger protobuf reference guards.

### Core (order)
1. `core.hydrology_watershed_stitch` — Downhill + flow-shadow hydrology seams keep river/lake/cave masks continuous across chunks.
2. `core.worldmap_profile_sync` — Hashed world-map control profiles reload on world JSON changes and stream hashes to clients.
3. `core.protocol_reference_guard` — Descriptor-origin/parser/handler coverage validation for Google.Protobuf bindings before wiring handlers.

### Content (order)
1. `content.river_braid_inflow` — Confluence-aware river pressure with seam refills, downhill bias, and braided inflow support.
2. `content.lake_watershed_integration` — Shoreline jitter, rim erosion, wetlands, and outflow channels that react to hydrology/flow gradients.
3. `content.cave_flow_resilience` — Cave thresholds dampened by moisture/river pressure with ceiling support near saturated terrain.

### Utility (order)
1. `util.config_json_parity` — JSON-first configs mirrored between server and StreamingAssets with hashes.
2. `util.proto_validation` — Fingerprint + descriptor-origin + handler binding checks via ProtocolValidator/ProtoDiagnostics + `scripts/verify_protobuf.ps1`.
3. `util.build_test` — `dotnet build` for SharedProtocol/GameServer and protobuf smoke validation.
