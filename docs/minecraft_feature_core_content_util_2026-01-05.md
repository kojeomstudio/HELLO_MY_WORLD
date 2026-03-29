## Minecraft core/content/util rollout (2026-01-05)
- New hydrology-gradient smoothing aligns downhill vectors for caves, rivers, and lakes across chunk seams in both the dedicated server (`GameServer/World/WorldManager.cs`) and Unity preview library (`MapGeneratorLib/.../WorldGenAlgorithms.cs`). This reduces river/lake divergence and keeps world map control consistent between streamed chunks and client meshes.
- Config knobs remain JSON-first (`config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`) and flow through `WorldGenerationConfig`/`WorldConfigFile` so server and client stay in lockstep.

### Core (authority, world map control, protocol)
- World map control & chunk streaming — Server: `GameServer/World/WorldManager.cs`, `World/Generation/*`, `SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs`; Client: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; Data: `config/world.json`, `Assets/.../WorldConfigData.json`; Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`.
- Session/auth/movement — Server auth/heartbeat/spawn/anti-cheat (`Program.cs`, `SessionManager.cs`, `Handlers/*`); Client prediction/interp; Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`.
- Block interaction & permissions — Server validation/durability/ownership (`Handlers/WorldBlockHandler.cs`); Client placement/break UI; Data: `config/blocks.json`; Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`.
- Protocol registry & guards — Runtime validation in `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` + `ProtoRuntime.EnsureInitialized()`; covers chunk/world/time/weather/status/entity/container/action descriptors; DTOs generated from `proto/*.proto` into `SharedProtocol` and `Assets/Generated/Protobuf`.
- Room/instance routing — `SessionManager.cs` / `RoomManager` handle per-room chunk routing; client room UI in `Assets/MyAssets/Scripts/Network`; Proto: `RoomEnter/Leave/List`.

### Content (terrain/worldgen, gameplay, entities)
- Terrain & hydrology (caves/rivers/lakes) — Shared hydrology/flow/erosion caches drive carving in `WorldManager` with the new flow-aware hydrology-gradient smoother; MapGeneratorLib mirrors the same smoothing so Unity previews match streamed chunks. Config knobs: `Water.*`, `Caves.*`, `Lakes.*` in JSON.
- Biomes/weather/sky — Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`); client sky/weather FX; Proto: `WorldInfo`, `WeatherChange`.
- Structures & loot — `DungeonGenerationStage` + container handlers on server; client container UI; Proto: `ContainerOpen/Update`, `EntitySpawn/Despawn`, item drops.
- Entities/combat — Spawn/update/despawn + combat resolution in handlers/systems; client render/prediction; Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`, `Health/Hunger` broadcasts.
- Items/crafting/inventory — Authoritative inventory/recipe validation in handlers; client drag/drop UI; Data: recipes JSON; Proto: `InventoryUpdate`, item use/drop/pickup.

### Utility (data, tooling, ops)
- Config/tuning alignment — JSON configs (`config/world.json`, `config/server.json`, Unity `WorldConfigData.json`) parsed by `WorldGenerationConfig` and `WorldConfigFile`; keep hydrology/cave/river/lake knobs in sync.
- Protobuf pipeline — IDL in `proto/*.proto`; generated DTOs in `SharedProtocol` and `Assets/Generated/Protobuf`; validation via `ProtocolValidator` and `ProtoDiagnostics`.
- Tooling/metrics — Scripts under `scripts/`, chunk residency + server status telemetry, recordings under `Recordings/`; server status exposed via `ServerStatusRequest/Response`.
