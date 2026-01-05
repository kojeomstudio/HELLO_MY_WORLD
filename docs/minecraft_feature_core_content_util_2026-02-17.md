## Minecraft core/content/util rollout (2026-02-17)
- Source of truth: `config/minecraft_feature_core_content_util_2026-02-17.json` (ordered phases + owners).
- Data-driven: world/map-control/enhanced-terrain knobs mirrored between `config/` and Unity StreamingAssets with hashes so server + client previews stay in sync.
- Focus: watershed-aware hydrology stitches that keep rivers/lakes/caves continuous, map-control hash streaming, and stricter protobuf descriptor/handler validation.

### Core (in order)
1. `core.hydrology_watershed_stitch` – downhill + flow-shadow hydrology seams so river/lake/cave masks stay continuous across chunks on server/clients.
2. `core.worldmap_profile_sync` – hashed world-map control profiles reload on world JSON changes and stream hashes to clients for parity.
3. `core.protocol_reference_guard` – validate Google.Protobuf bindings (descriptor origin, parser coverage, handler contracts) before wiring handlers.

### Content (in order)
1. `content.river_braid_inflow` – confluence-aware river pressure with seam refills, downhill bias, and braided inflow support.
2. `content.lake_watershed_integration` – shoreline jitter, rim erosion, wetlands, and outflow channels that react to hydrology/flow gradients.
3. `content.cave_flow_resilience` – cave thresholds dampened by moisture/river pressure with ceiling support where hydrology is high.

### Utilities (in order)
1. `util.config_json_parity` – JSON-first configs mirrored between server and StreamingAssets with hashes (world, map-control, enhanced terrain).
2. `util.proto_validation` – fingerprint + descriptor-origin + handler binding checks via ProtocolValidator/ProtoDiagnostics + `scripts/verify_protobuf.ps1`.
3. `util.build_test` – `dotnet build` (SharedProtocol/GameServer) and protobuf smoke validation gates.
