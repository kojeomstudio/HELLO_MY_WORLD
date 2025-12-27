## Minecraft core/content/util rollout (latest 2025-12-27)
- World + network remain JSON/protobuf driven: `config/world.json` (server) mirrors `Assets/StreamingAssets/world-config.json` and emits `config/world_map_control_profile.json` that Unity reads via `WorldMapControlProfile`.
- Core paths are explicitly split by responsibility and platform: server worldgen + protocol registry in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`, client chunk/map/render in `Assets/MyAssets/Scripts/GameWorld/*` and `Assets/Scripts/Minecraft/World/*`.
- Data + ops live in JSON/config managers (`GameCommon/Configuration`, `GameCommon/DataDriven`) so environment/config values stay versioned and auditable.

### Core (server + client)
- Server authority: chunk lifecycle + hydrology-aware terrain pipeline (`WorldManager`, `World/Generation/*`), session/auth/routing (`SessionManager.cs`, handlers), EnhancedMinecraft protocol registry/validator, map-control profile export.
- Client authority: chunk subscription/render/prediction (`ChunkManager`, `ImprovedChunkManager`), world-map control UI/overlays (`EnhancedWorldMapController`, `WorldAreaManager`), profile bootstrap (hash/version) + reconnection logic.
- Shared data contracts: protobuf DTOs from `proto/*.proto` and generated C# in `Assets/Generated/Protobuf`, config contracts in `GameCommon/Configuration/ConfigModels.cs`.

### Content (terrain + gameplay)
- Terrain & hydrology: caves/rivers/lakes share caches and water-table blending; smoother cave stability, river meanders/deltas, and lake shoreline/outflow rules are tuned from JSON and consumed by both server (`WorldManager`) and client generators (`TerrainGenerator`, `ImprovedTerrainGenerator`).
- Map & exploration: map markers/biome overlays, player visibility toggles, and chunk heatmaps feed the enhanced world map controller; chunk/radar data respects the map-control profile.
- Gameplay layers: block placement/break, entities, inventory/recipes, weather/time sync, and container flows carried over protobuf packets (`ChunkLoad/Unload`, `BlockChange`, `Entity*`, `TimeUpdate`, `WeatherChange`, `Container*`).

### Utility (tooling, validation, data)
- Config + data-driven pipeline: unified JSON loaders (`UnifiedConfigManager`, `DataManager`) keep server/client/environment knobs in versioned config files and validate shapes before use.
- Protocol health: `ProtocolRegistry`, `ProtocolValidator`, `ProtoDiagnostics` ensure EnhancedMinecraft packet handlers line up with generated DTOs after `protoc` runs.
- Observability & safeguards: map-control profile hashes/versioning, chunk residency hooks, and config-path sanity (json, streaming assets) keep worldgen deterministic across platforms.

### Implementation order (current sprint)
1. Finalize this core/content/util catalog and align config sources (server `config/*.json`, client `StreamingAssets`/Resources).
2. Upgrade terrain algorithms for caves/rivers/lakes on server + client (stability, hydrology smoothing, shoreline/delta blending) and wire to map-control profile.
3. Re-validate protobuf references/registries on both ends and repair any missing handlers/usings after regeneration.
4. Run builds/tests, then publish updated docs/config so data-driven defaults remain reproducible.
