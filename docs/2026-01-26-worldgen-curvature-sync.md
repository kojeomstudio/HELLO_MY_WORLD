# 2026-01-26 Worldgen Curvature Sync

## Summary
- Hydrology now blends curvature/slope guidance across server and Unity previews; pipeline version set to `2026-01-26-curvature-sync` for map-control signature tracking.
- Rivers and lakes bias basin curvature while damping ridge channels; caves apply slope stability and ceiling penalties to reduce steep collapses.
- Proto health tightened: server handler logs ProtoDiagnostics summaries on startup, Unity client boots with ProtoRuntime + registry clean validation.
- Feature catalog refreshed with a curvature-guided hydrology entry (`minecraft_feature_core_content_util.json`).

## Server Changes
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` — curvature-guided hydrology step feeds rivers/lakes/caves before continuity smoothing.
- `GameServer/World/Generation/ImprovedRiverGenerator.cs` — curvature bias widens basins and suppresses ridge channels.
- `GameServer/World/Generation/ImprovedLakeGenerator.cs` — basin weighting added to lake scoring to favor depressions and avoid ridge puddles.
- `GameServer/World/Generation/ImprovedCaveGenerator.cs` — slope-aware stability/threshold penalties for safer ceilings on steep terrain.
- `GameServer/Network/EnhancedProtocolHandler.cs` — runs ProtoDiagnostics summary after registry/fingerprint asserts.

## Client Changes
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs` — mirrors curvature hydrology step, slope-aware caves, and pipeline signature updates.
- `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs` — boot now calls ProtoRuntime + ProtoDiagnostics.AssertRegistryClean (logs summary in editor).

## Data & Config Notes
- No new schema keys; curvature/slope tuning uses existing JSON config (`config/world.json`, `Assets/StreamingAssets/world-config.json`). Regenerate `config/world_map_control_profile.json` / `Assets/StreamingAssets/world-map-control.json` after hydrology tuning to refresh hashes.
- Curvature-guided feature captured in `minecraft_feature_core_content_util.json` (version 1.1.0, last_updated 2026-01-26T12:00:00Z).
- Pipeline signature now includes `HydrologyCurvatureWeight` and slope penalty for cache invalidation across server/client previews.

## Validation
- Build: `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj` (warnings only: NU1603 protobuf-net resolved to 3.2.26).
- Protocol checks: verify startup logs contain ProtoDiagnostics summary and no missing registrations on both server and Unity client bootstrap.
