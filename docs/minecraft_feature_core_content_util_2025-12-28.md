## Minecraft feature catalog (core / content / utility) – 2025-12-28

- Current iteration: slope-aware water-table clamping now attenuates hydrology/flow boosts on steep terrain using `HydrologyWaterTableSlopeWeight` so rivers, lakes, and cave moisture stay tied to the configured sea level without flooding cliffs. The knob is mirrored across `config/world.json`, `WorldConfigData.json`, `WorldManager`, and `MapGeneratorLib`.
- Scope: server authority in `GameServer/` + SharedProtocol protobuf validation + Unity client/world preview (`Assets/MyAssets/...`, MapGeneratorLib).

### Core (authority, world map control, protocol)
- World map control & chunk streaming (seams, water-table clamp, slope-aware hydrology): `GameServer/World/WorldManager.cs`, `WorldGenerationConfig`, `Generation/*`, `ChunkPayloadBuilder`; client mirrors via `MapGeneratorLib/WorldGenAlgorithms.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`.
- Session/room routing & lifecycle: `Program.cs`, `SessionManager.cs`, `Handlers/*` for auth/move/block/inventory/entity; client bindings in `Assets/MyAssets/Scripts/Network/*`.
- Protocol registry & validation: `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, `ProtocolRegistry` (guards chunk/time/weather/status/entity/world descriptors and parser assemblies).
- Chunk/time/weather/world control packets: proto (`proto/*.proto`), generated DTOs (`Assets/Generated/Protobuf`, `SharedProtocol/EnhancedMinecraft/*`); framed transport `[len][type][protobuf]`.

### Content (worldgen, gameplay, entities)
- Terrain & hydrology (rivers/lakes/caves with seam blend, edge stability, water-table clamp, slope-aware attenuation): `WorldManager` passes (BuildHydrologyMask → Normalize/Blend/Clamp/Relax → River/Lake/Cave stages); mirrored in `MapGeneratorLib`.
- Biomes/weather/sky + time: `WorldTimeSystem`, `WeatherSystem`; Unity binds to skybox/FX (`Assets/MyAssets/Scripts/GameWorld`).
- Structures/loot/containers: `DungeonGenerationStage`, container handlers, Unity container UI; data tables in JSON.
- Entities/combat/remote player sync: `EntitySpawn/Update/Despawn` handlers, `EntitySyncService`; Unity `RemoteEntityManager`, combat HUD.
- Items/crafting/inventory: authoritative handlers + UI drag/drop; recipes/loot JSON.

### Utility (data, tooling, ops)
- Data-driven configs: `config/world.json`, `server-config.json`, `Assets/.../WorldConfigData.json`; hydrology knobs include `HydrologyWaterTableClampWeight/Range/SlopeWeight`, seam/tangent/flow-lock weights, river smoothing, cave support biases.
- Protobuf/tooling: `proto/*.proto` → `Assets/Generated/Protobuf`, `SharedProtocol`; validator + `scripts/verify_protobuf.ps1`; rebuild via `dotnet build SharedProtocol/SharedProtocol.csproj`.
- Metrics/observability: server status HUD/telemetry (`ServerStatusResponse`), chunk residency counters, recordings under `Recordings/`.
- Data tables: blocks/recipes/mobs/loot/worldgen tunables in JSON; keep server/client copies in sync.

### Recommended sequencing
1) Core: validate protobufs, world map control, room/session routing, slope-aware water-table clamp active before traffic.
2) Content: hydrology-driven rivers/lakes/caves with seam stability + new slope-aware clamp; biome/weather/time sync; entity and container flows.
3) Utility: keep configs/DTOs/data tables synchronized; capture telemetry for seam stability and proto registry health.
