# Terrain Generation & Map-Control Update (Session 09 - 2026-01-22)

## Highlights
- Hydrology momentum added to reduce pooled seams; riparian cave buffers dampen flooding near rivers/lakes.
- River/lake masks use divergence-aware pressure with adjusted confluence boost and lake seepage tuning.
- Map-control profile bumped to v5 with pipeline signature `2026-01-22-river-lake-cave-coupling`; generation signature now tracks hydrology flow gain/divergence and river relief penalties.

## Server Changes
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`: hydrology momentum pass plus riparian cave erosion buffer before mask generation.
- `GameServer/World/Generation/ImprovedRiverGenerator.cs`: divergence penalty + braided assist on river pressure; confluence boost retuned.
- `GameServer/World/Generation/ImprovedLakeGenerator.cs`: divergence-aware basin weighting, momentum assist, downhill-aware outflow anchors.
- `GameServer/World/Generation/ImprovedCaveGenerator.cs`: saturation/variance brakes reduce riparian openings.
- `GameServer/World/WorldMapControlManager.cs`, `GameServer/World/WorldMapController.cs`: pipeline version updated, generation signature expanded.

## Client Changes
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`: parity hydrology momentum, riparian cave buffers, divergence-aware river/lake masks; pipeline signature updated.
- `Assets/Scripts/Minecraft/Core/WorldConfig.cs`: default map-control profile version raised to 5.

## Config & Data
- `config/world.json`, `Assets/StreamingAssets/world-config.json`: MapControlProfileVersion=5, HydrologyFlowGain=0.58, HydrologyFlowDivergenceClamp=0.5, RiverConfluenceBoost=0.4, RiverReliefPenaltyWeight=0.3, Lake FlowSeepageWeight=0.42.
- Profiles regenerated: `config/world_map_control_profile.json` (hash: `121092ceedc4738f5b9b5456ee00c40c0fc6c10257f4cb78d59f24f6cd07a817`), copied to `Assets/StreamingAssets/world-map-control.json`.
- Feature catalog refresh: `config/minecraft_feature_client_server_core_content_util_2026-01-22-session-09.json`, `docs/minecraft_features_client_server_core_content_util_2026-01-22-session-09.md`.

## Tests
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` (passes; proto registry still emits warnings for unmapped optional EnhancedMinecraft descriptors).
- No additional automated client tests executed (Unity runtime not available in this environment).

## Notes
- Optional overlay/preview tasks remain: client river/lake overlay smoothing and cave moisture debug overlay (tracked in feature catalog).
