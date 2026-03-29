## Minecraft feature catalog (core / content / utility) — 2026-01-09
- Current iteration: edge-feather hydrology smoothing (existing edge blend/variance knobs) now runs before rivers/lakes/caves in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, river width modulation factors flow variance + hydrology gradients, and lake orientation/perturbation align to inflow gradients. `ProtoDiagnostics` now logs missing optional `MinecraftMessageType` bindings while still failing fast on required EnhancedMinecraft contracts.
- Scope: server authority (`GameServer/` + `SharedProtocol`), Unity client & preview (`Assets/MyAssets/...`, `MapGeneratorLib`), JSON configs (`config/world.json`, `Assets/.../WorldConfigData.json`), proto IDL (`proto/*.proto`).

### Core (authority, world map control, protocol)
- World map control & chunk streaming: `WorldManager`, `World/Generation/*`, `ChunkPayloadBuilder`; client mirrors via `WorldAreaManager`, `SubWorld`, and `MapGeneratorLib`.
- Session/auth/movement: `Program.cs`, `SessionManager.cs`, `Handlers/*`, base protocol (`game_auth.proto`, `game_move.proto`); client prediction/interp under `Assets/MyAssets/Scripts/Network`.
- Protocol registry & guards: `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, `ProtocolRegistry` bindings; generated DTOs under `SharedProtocol` and `Assets/Generated/Protobuf`.
- Time/weather/world control: `WeatherSystem`, `WorldTimeSystem`, chunk/time/weather/world descriptors validated via `ProtocolValidator.ValidateEnhancedContracts()`.

### Content (terrain/worldgen, gameplay, entities)
- Terrain/hydrology: edge-feathered hydrology masks, flow-variance river width modulation, gradient-aware lake anisotropy/perturbation, and moist cave stability smoothing shared between server and MapGeneratorLib.
- Entities/combat: spawn/update/despawn + combat resolution handlers and Unity render/prediction (`EntitySpawn/Update/Despawn`, `PlayerAttack`, health/hunger HUD).
- Structures/containers: `DungeonGenerationStage`, container handlers, Unity container UI; data tables in JSON.
- Items/crafting/inventory: authoritative inventory/recipe validation; client drag/drop UI; data-driven recipes/loot tables.

### Utility (data, tooling, ops)
- Data-driven configs: `config/world.json`, `server-config.json`, Unity `WorldConfigData.json` kept in sync through `WorldGenerationConfig` and `WorldConfigFile`.
- Protobuf pipeline: IDL in `proto/*.proto`, generated DTOs under `SharedProtocol` + `Assets/Generated/Protobuf`; `ProtoDiagnostics` reports optional/required registry bindings and fingerprints.
- Tooling/metrics: scripts under `scripts/`, server status/recordings under `Recordings/`, chunk residency counters in handlers/metrics services.

### Iteration checklist
- [x] Edge-feather hydrology smoothing before river/lake/cave passes on server/client generators.
- [x] Flow-variance + hydrology-gradient river width modulation mirrored between WorldManager and MapGeneratorLib.
- [x] Lake anisotropy/perturbation aligned to inflow gradients with shared ripple noise.
- [x] Optional EnhancedMinecraft enums logged (not fatal) while required registry/descriptor bindings still enforced during bootstrap.
