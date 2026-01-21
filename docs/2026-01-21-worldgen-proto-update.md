# 2026-01-21 Worldgen & Proto Update

## Highlights
- Hydrology edge cohesion tightened for caves/rivers/lakes; riparian bias now dampens seams and flow spikes across chunk borders.
- World generation defaults retuned (edge stability, river depth/thresholds, lake basin smoothing) and map-control profile bumped to v3 with pipeline `2026-01-21-hydrology-edge-cohesion+proto`.
- Protobuf handler validation now runs on both server and client to catch stale EnhancedMinecraft DTO references early.
- Server/client configs stay data-driven in JSON (`config/world.json`, `Assets/StreamingAssets/world-config.json`) with matching profile hashes.

## Key Config Changes
- MapControlProfileVersion: 3 (server `config/world.json`, client `Assets/StreamingAssets/world-config.json`).
- Hydrology: edge stability iterations 2, stability weight 0.35, variance clamp 0.62/edge clamp 0.28, seam relax blend 0.56 (3 iterations), edge flux blend 0.6, flow persistence 0.72.
- Rivers: bank threshold 0.026, depth 7, mouth smoothing radius 4, edge feather 0.5, delta wetland strength 0.5, confluence boost 0.38, intensity blend 0.6.
- Lakes: basin smoothing iterations 3, wetland buffer radius 3, flow seepage weight 0.32, variance weight 0.3, outflow stability weight 0.36, rim erosion weight 0.32.
- Caves: edge seal strength 0.5 to better plug riparian openings.

## Actions
- Regenerate map-control profile after pulling changes: `dotnet run --project GameServer -- --generate-map-profile` then copy `config/world_map_control_profile.json` to `Assets/StreamingAssets/world-map-control.json` for Unity previews.
- Keep configs in JSON; profile hashes and generation signatures now include the v3 pipeline identifier.
- Proto guards: server `EnhancedProtocolHandler` and client `ProtobufNetworkClient` now run handler binding validation and descriptor fingerprints on startup.

## Test Plan
- [x] dotnet build SharedProtocol/SharedProtocol.csproj *(warning NU1603: protobuf-net fallback to 3.2.26)*
- [x] dotnet build GameServer/GameServer.csproj *(existing nullable/async warnings; NU1603 protobuf-net fallback)*
- [x] dotnet run --project GameServer -- --generate-map-profile *(profile hash `854de8f0c6b67c51fc6865205f02155cb7ee16096cd1e73fae4ca39007ee7822`; proto diagnostics warn about optional/unbound descriptors—regenerate DTOs if promoting optional packets)*
