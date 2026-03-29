# Terrain Generation Improvements (2026-01-14)

## Changes
- Added cross-chunk hydrology/flow stitching to reduce seam artifacts in both server masks (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`) and Unity preview generation (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`).
- Reinforced cave ceilings near wet columns to stop shallow water leaks and ceiling collapses (`GameServer/World/WorldManager.cs`).
- Lake basins now carve outflow channels on the server side to match client previews and maintain wetland continuity (`GameServer/World/WorldManager.cs`).
- River beds now swap to clay under higher channel pressure for smoother channel floors (`GameServer/World/WorldManager.cs`).

## Architecture & Data
- Map-control parity: server and client pipelines share the same hydrology stitch logic to keep preview and runtime aligned.
- Lake outflow carving uses existing JSON config values (no schema changes) and respects bedrock/water levels.
- Feature categorization refreshed in `config/minecraft_feature_classification_2026-01-14.json` with summary in `docs/minecraft_feature_classification_2026-01-14.md` to track core/content/util work across client/server.

## Validation Plan
- Build: `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj`.
- Proto usage: rely on build warnings/errors to surface missing generated types; confirm `EnhancedMinecraftProtocol` references remain intact.
- World-map previews: reload Unity StreamingAssets profile after server profile regeneration to observe stitched seams and lake outflows.
