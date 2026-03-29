# Core/Content/Util Snapshot — 2026-02-11

## Core
- World config & map-control parity (implemented): client `Assets/Scripts/Minecraft/Core/WorldConfig.cs` now mirrors server `config/world.json` hydrology/cave/lake knobs and map-control profile version/path; Unity map control builds profiles from the same JSON (`Assets/StreamingAssets/world-config.json`, `world-map-control.json`).
- Hydrology-aware terrain (implemented): caves/rivers/lakes use slope/aniso/flow-aware masks in `Assets/Scripts/Minecraft/World/ImprovedTerrainGenerator.cs` with stability bias, wetland buffers, and seam smoothing; server pipeline remains in `GameServer/World/Generation/Improved*Generator.cs`.
- Map-control runtime alignment (implemented): `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs` reloads profiles when the version in config increases and falls back to config when hashes drift; `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs` consumes nested config knobs directly.
- Protocol validation (in-progress): `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` + registry/handler guards keep EnhancedMinecraft generated DTOs in sync with handlers; continue expanding handler coverage.

## Content
- Terrain/biome surfacing (in-progress): biome-aware surface decoration and ore distribution stay aligned with the improved height/river/lake masks; content balance work tracks under `content_001/content_003/content_006`.
- World map UX (planned): maintain player profile toggles (caves/rivers/lakes/players) and chunk preview quality targets tied to map-control profile hashes.

## Utility
- Data-driven configs (implemented): JSON sources (`config/world.json`, `Assets/StreamingAssets/world-config.json`, `config/minecraft_feature_core_content_util.json`) drive both server and Unity; profile hashes record the exact inputs.
- Execution order refresh: 1) hydrology/map-control parity (`core_001`, `core_007`, `core_009`), 2) protocol validation (`core_002`, `core_008`), 3) content balance (`content_001`, `content_003`, `content_006`), 4) utility polish (`util_001`, `util_005`).
