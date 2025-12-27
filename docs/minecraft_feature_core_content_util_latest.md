## Minecraft core/content/util rollout (latest 2026-02-10)
- World + network remain JSON/protobuf driven: `config/world.json` (server) mirrors `Assets/StreamingAssets/world-config.json` and emits `config/world_map_control_profile.json` that Unity reads via `WorldMapControlProfile` (hash now includes riparian buffer, river seam fill, lake wetland buffer, cave ceiling stability).
- Core paths are explicitly split by responsibility and platform: server worldgen + protocol registry in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`, client chunk/map/render in `Assets/MyAssets/Scripts/GameWorld/*`, `Assets/MyAssets/Scripts/DataFiles/*`, and `Assets/Scripts/Minecraft/World/*`.
- Data + ops live in JSON/config managers (`GameCommon/Configuration`, `GameCommon/DataDriven`) so environment/config values stay versioned and auditable.

### Core (server + client)
- Server authority: chunk lifecycle + hydrology-aware terrain pipeline (`WorldManager`, `World/Generation/*`), session/auth/routing (`SessionManager.cs`, handlers), EnhancedMinecraft protocol registry/validator (`ProtocolValidator.ValidateEnhancedContracts/ValidateHandlerBindings`), map-control profile export.
- Client authority: chunk subscription/render/prediction (`ChunkManager`, `ImprovedChunkManager`), world-map control UI/overlays (`EnhancedWorldMapController`, `WorldAreaManager`), profile bootstrap (hash/version) + reconnection logic (hash validation in `WorldMapControlProfile.LoadFromFile`).
- Shared data contracts: protobuf DTOs from `proto/*.proto` and generated C# in `Assets/Generated/Protobuf`, config contracts in `GameCommon/Configuration/ConfigModels.cs`.

### Content (terrain + gameplay)
- Terrain & hydrology: riparian saturation buffers expand across chunk edges, rivers run a seam-fill carve before bank shaping, lakes grow buffered wetlands/outflows, and cave ceilings reinforce near wet/riparian spans (all JSON-driven knobs).
- Map & exploration: map markers/biome overlays, player visibility toggles, and chunk heatmaps feed the enhanced world map controller; chunk/radar data respects the map-control profile (render/simulation distance + hydrology knobs).
- Gameplay layers: block placement/break, entities, inventory/recipes, weather/time sync, and container flows carried over protobuf packets (`ChunkLoad/Unload`, `BlockChange`, `Entity*`, `TimeUpdate`, `WeatherChange`, `Container*`).

### Utility (tooling, validation, data)
- Config + data-driven pipeline: unified JSON loaders (`UnifiedConfigManager`, `DataManager`) keep server/client/environment knobs in versioned config files and validate shapes before use; new knobs (`RiparianBufferRadius`, `RiverSeamFillStrength`, `WetlandBufferRadius`, `CeilingStabilityWeight`) are now parsed on both sides.
- Protocol health: `ProtocolRegistry`, `ProtocolValidator`, `ProtoDiagnostics` ensure EnhancedMinecraft packet handlers line up with generated DTOs after `protoc` runs.
- Observability & safeguards: map-control profile hashes/versioning, chunk residency hooks, and config-path sanity (json, streaming assets) keep worldgen deterministic across platforms.

### Implementation order (current sprint)
1. Align config/profile sources (server `config/*.json`, client `StreamingAssets`/Resources`) and regenerate map-control hash. ✅
2. Upgrade terrain algorithms for caves/rivers/lakes (riparian buffer, river seam fill, lake wetlands, cave ceiling stability) on server + client, wired to map-control profile. ✅
3. Re-validate protobuf references/registries on both ends and repair any missing handlers/usings after regeneration. ✅
4. Run builds/tests, then publish updated docs/config so data-driven defaults remain reproducible. 🔄
