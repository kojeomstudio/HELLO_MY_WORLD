# World Map Hydrology Update (2026-01-18)

## Overview
- Tuned river, lake, and cave generation to respect water-table clamps, shelf depths, and flooded-cave thresholds.
- Synced world map control signatures across server/client to include new hydrology and cave parameters.
- Cleaned and versioned `config/enhanced_terrain_generation.json` for data-driven worldgen inputs.

## Key Changes
- **Rivers**: Added water-table bias, slope dampening, and variance clamp to stabilize channels near sea level; river masks now honor flow memory and depth bias.
- **Lakes**: Lake masks factor min/max depth and shoreline shelves; water-table clamp reduces over-elevation spawning; variance clamp and shelf pass added.
- **Caves**: Hydrology-aware flooded-cave suppression using `FloodedCave*` thresholds plus lava-depth bias; ceiling moisture penalties retained.
- **World Map Control**: Generation signature now includes water-table, lake depth, and flooded-cave fields; request handling re-validates protobuf runtime.
- **Client Preview**: World map controller mirrors the new river/lake logic, shelf shaping, and signature fields.
- **Config**: Rebuilt `config/enhanced_terrain_generation.json` (v1.1.0, 2026-01-18) capturing water, river, lake, cave knobs without duplicate payloads.
- **Feature Catalog**: Added `docs/minecraft_feature_client_server_core_content_util_2026-01-18-session-04.md` and JSON companion in `config/` for core/content/util sequencing.

## Data/Config Touchpoints
- `config/enhanced_terrain_generation.json` — cleaned, versioned hydrology/cave/lake settings.
- `config/world.json` + `config/world_map_control_profile.json` — sources for runtime signatures and profile sync.
- `config/minecraft_feature_client_server_core_content_util_2026-01-18-session-04.json` — session feature ordering (core/content/util).

## Notes for Validation
- Re-run builds/tests: `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj`, then `dotnet run --project GameServer -- --selftest`.
- Unity preview paths now rely on the updated signature; ensure profile reload picks up new hash when configs change.
