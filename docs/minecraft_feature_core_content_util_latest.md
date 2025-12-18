## Minecraft core/content/util rollout (latest 2025-12-18b)
- Hydrology variance smoothing now runs ahead of the cave/river/lake passes on both server (`GameServer/World/WorldManager.cs`) and Unity previews (`MapGeneratorLib/.../WorldGenAlgorithms.cs`), driven by new JSON knobs to clamp flow spikes before carving.
- Map-control profile stays JSON-first (`config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`) and now carries the variance blend/clamp settings so chunk-edge hydrology stability stays aligned between server streaming and Unity previews. See `docs/minecraft_feature_core_content_util_2025-12-18b.md` for the detailed split.
- Proto diagnostics continue to guard EnhancedMinecraft descriptor/handler coverage at startup via `ProtocolValidator` + `ProtoDiagnostics` to catch stale generated DTOs early.

### Core (authority, world map control, protocol)
- World map control & chunk streaming ??Server: `GameServer/World/WorldManager.cs`, `World/Generation/*`, `SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs`; Client: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; Data: `config/world.json`, `Assets/.../WorldConfigData.json`; Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`.
- Session/auth/movement ??Server auth/heartbeat/spawn/anti-cheat (`Program.cs`, `SessionManager.cs`, `Handlers/*`); Client prediction/interp; Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`.
- Block interaction & permissions ??Server validation/durability/ownership (`Handlers/WorldBlockHandler.cs`); Client placement/break UI; Data: `config/blocks.json`; Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`.
- Protocol registry & guards ??Runtime validation in `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` + `ProtoRuntime.EnsureInitialized()`; covers chunk/world/time/weather/status/entity/container/action descriptors; DTOs generated from `proto/*.proto` into `SharedProtocol` and `Assets/Generated/Protobuf`.
- Room/instance routing ??`SessionManager.cs` / `RoomManager` handle per-room chunk routing; client room UI in `Assets/MyAssets/Scripts/Network`; Proto: `RoomEnter/Leave/List`.

### Content (terrain/worldgen, gameplay, entities)
- Terrain & hydrology (caves/rivers/lakes) ??Shared hydrology/flow/curvature caches drive carving in `WorldManager`, with curvature-aware gradients and confluence-boosted banks; MapGeneratorLib mirrors the same smoothing so Unity previews match streamed chunks. Config knobs: `Water.*`, `Caves.*`, `Lakes.*` in JSON (incl. `HydrologyGradientSlopeWeight`, `HydrologyGradientClamp`, `HydrologyCurvatureWeight`, `LakeBasinSmoothIterations`, `RiverConfluenceBoost`).
- Biomes/weather/sky ??Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`); client sky/weather FX; Proto: `WorldInfo`, `WeatherChange`.
- Structures & loot ??`DungeonGenerationStage` + container handlers on server; client container UI; Proto: `ContainerOpen/Update`, `EntitySpawn/Despawn`, item drops.
- Entities/combat ??Spawn/update/despawn + combat resolution in handlers/systems; client render/prediction; Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`, `Health/Hunger` broadcasts.
- Items/crafting/inventory ??Authoritative inventory/recipe validation in handlers; client drag/drop UI; Data: recipes JSON; Proto: `InventoryUpdate`, item use/drop/pickup.

### Utility (data, tooling, ops)
- Config/tuning alignment ??JSON configs (`config/world.json`, `config/server.json`, Unity `WorldConfigData.json`) parsed by `WorldGenerationConfig` and `WorldConfigFile`; keep hydrology/cave/river/lake knobs in sync.
- Protobuf pipeline ??IDL in `proto/*.proto`; generated DTOs in `SharedProtocol` and `Assets/Generated/Protobuf`; validation + handler coverage via `ProtocolValidator`, `ProtoDiagnostics`.
- Tooling/metrics ??Scripts under `scripts/`, chunk residency + server status telemetry, recordings under `Recordings/`; server status exposed via `ServerStatusRequest/Response`.
