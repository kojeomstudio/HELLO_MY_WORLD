## Minecraft core/content/util rollout (2026-01-06)
- Hydrology gradients now blend hydrology masks with surface slope and flow accumulation using the new `HydrologyGradientSlopeWeight`, clamping downhill vector magnitude so caves/rivers/lakes stay coherent across chunk seams in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`.
- Knobs remain JSON-first (`config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`) and flow through `WorldGenerationConfig`/`WorldConfigFile` to keep server + Unity parity.

### Core (authority, world map control, protocol)
1. World map control & chunk streaming — Server: `GameServer/World/WorldManager.cs`, `World/Generation/*`, `SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs`; Client: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; Data: `config/world.json`, `Assets/.../WorldConfigData.json`; Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`.
2. Protocol registry & guards — `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` + `ProtoRuntime.EnsureInitialized()` validate descriptors/parsers/assemblies for chunk/world/time/weather/status/entity/container/action messages; DTOs generated from `proto/*.proto` into `SharedProtocol` and `Assets/Generated/Protobuf`.
3. Session/auth/movement — Server auth/heartbeat/spawn/anti-cheat (`Program.cs`, `SessionManager.cs`, `Handlers/*`); client prediction/interpolation; Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`.
4. Block interaction & permissions — Server validation/durability/ownership (`Handlers/WorldBlockHandler.cs`); client placement/break UI; Data: `config/blocks.json`; Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`.
5. Room/instance routing — `SessionManager.cs` / `RoomManager` handle per-room chunk routing; client room UI in `Assets/MyAssets/Scripts/Network`; Proto: `RoomEnter/Leave/List`.

### Content (terrain/worldgen, gameplay, entities)
1. Terrain & hydrology (caves/rivers/lakes) — Shared hydrology/flow/erosion caches feed carving in `WorldManager` with slope- and flow-aware gradients (`HydrologyGradientSlopeWeight`, `HydrologyGradientWeight`); MapGeneratorLib mirrors the same pass. Config knobs: `Water.*`, `Caves.*`, `Lakes.*` in JSON.
2. Biomes/weather/sky — Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`); client sky/weather FX; Proto: `WorldInfo`, `WeatherChange`.
3. Structures & loot — `DungeonGenerationStage` + container handlers on server; client container UI; Proto: `ContainerOpen/Update`, `EntitySpawn/Despawn`, item drops.
4. Entities/combat — Spawn/update/despawn + combat resolution in handlers/systems; client render/prediction; Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`, `Health/Hunger` broadcasts.
5. Items/crafting/inventory — Authoritative inventory/recipe validation in handlers; client drag/drop UI; Data: recipes JSON; Proto: `InventoryUpdate`, item use/drop/pickup.

### Utility (data, tooling, ops)
1. Config/tuning alignment — JSON configs (`config/world.json`, `config/server.json`, Unity `WorldConfigData.json`) parsed by `WorldGenerationConfig` and `WorldConfigFile`; new `HydrologyGradientSlopeWeight` stays mirrored for server/Unity world map control.
2. Protobuf pipeline — IDL in `proto/*.proto`; generated DTOs in `SharedProtocol` and `Assets/Generated/Protobuf`; validation via `ProtocolValidator`, `ProtoDiagnostics`, and `ProtoRuntime.EnsureInitialized()`.
3. Data-driven assets — World/biome/tuning data stays in JSON (world/caves/lakes/water/blocks); server + client consume the same files to keep behaviour deterministic.
4. Tooling/metrics — Scripts under `scripts/`, chunk residency + server status telemetry, and `Recordings/`; server status exposed via `ServerStatusRequest/Response` for ops dashboards.
