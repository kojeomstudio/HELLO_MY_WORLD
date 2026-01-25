# 2026-01-26 Minecraft Feature Categorization (Core / Content / Utility)

## Context
- Based on commit `35b394dd` baseline; plan `plans/2026-01-26-plan.md`.
- Curvature-guided hydrology + proto validation shipped today; catalog synced with `minecraft_feature_core_content_util.json` v1.1.0.

## Core (server + client)
- **Worldgen hydrology parity** — `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `ImprovedCaveGenerator.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs` (curvature/slope-aware masks; pipeline `2026-01-26-curvature-sync`).
- **Networking/protocol health** — `SharedProtocol/EnhancedMinecraft/*`, `GameServer/Network/EnhancedProtocolHandler.cs`, `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs` (ProtoRuntime + ProtoDiagnostics, handler/fingerprint guards).
- **World map control** — `config/world.json`, `Assets/StreamingAssets/world-config.json`, map-control profile JSONs; signatures now include curvature/slope tuning to invalidate stale previews.
- **Configuration & data** — JSON-driven env/gameplay configs (`server-config.json`, `enhanced_*`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`); data assets (`config/blocks.json`, `items.json`, `recipes.json`, `biomes.json`).

## Content
- **Blocks/items/crafting** — definitions in `config/blocks.json`, `items.json`, `recipes.json`; inventory/crafting managers under `Assets/MyAssets/Scripts/GameWorld/*`.
- **Biomes & structures** — baseline biome noise (`BiomeGenerationSystem.cs`); structure generation pending (villages/temples/mineshafts).
- **Mobs/entities** — spawn/AI placeholders (see `GameServer/World/WorldManager.cs`, entity handlers); expansion needed for hostile/passive AI and drops.
- **World features** — caves/rivers/lakes now hydrology-driven; ores via `OreDistributionSystem.cs`; weather/day-night basic.

## Utility
- **Diagnostics & tooling** — Proto health (`ProtocolValidator`, `ProtoDiagnostics`, server/client bootstrap), worldgen preview (`WorldMapController`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`), config sync scripts (`scripts/sync_world_config.ps1`).
- **Server ops** — network settings in `config/network.default.json`; monitoring hooks via `ProtocolStatistics` and console logs.
- **Data management** — SQLite save files (`Assets/StreamingAssets/userDB.db`), JSON configs for env toggles; no secrets stored in repo.

## Implementation Order (next slices)
1) Harden core: run builds, tune curvature weights, regenerate map-control profiles, expand proto CI checks.
2) Advance content: expand structures/mob behaviors; align JSON data with new gameplay requirements.
3) Utilities/polish: add automated proto rebuild checks, integrate diagnostics into CI, extend worldgen preview overlays.

## Data-Driven Notes
- Keep all tunables in JSON (server/client config, world-gen profiles, gameplay data). Update `config/world.json` + `Assets/StreamingAssets/world-config.json` together when changing hydrology/cave parameters; regenerate map-control JSONs after tuning.
- Avoid hardcoded environment variables; prefer JSON config entries under `config/` and `Assets/StreamingAssets/`.
