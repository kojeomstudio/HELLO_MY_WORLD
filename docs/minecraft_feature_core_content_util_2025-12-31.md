# Minecraft feature split (core / content / util) - 2025-12-31

Snapshot of Minecraft-grade capabilities required on both server and Unity client, grouped by core authority, content, and utility. This iteration adds river-intensity anisotropy (flow-aligned smoothing) shared by `GameServer/World/WorldManager.cs` and `MapGeneratorLib` and keeps protobuf/JSON data sources in lockstep.

## Core (authority, world map control, protocol)
- World map & chunk control: `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` stream/persist chunks; Unity mirrors via `Assets/MyAssets/Scripts/GameWorld/*` + `MapGeneratorLib` so previews match server seams. Hydrology warp/seam relax, gradient cache, water-table clamp, and anisotropic river smoothing (`RiverAnisotropyWeight`) keep seams continuous.
- Sessions & movement: auth/spawn/respawn/anti-cheat + rate limits on server; prediction/interp/UI on client. Protos `game_auth.proto`, `game_move.proto`, `game_core.proto`.
- Block/state authority: validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; placement/break UI + VFX/SFX on client. Protos `BlockChange*`, `MultiBlockChange`; data `config/blocks.json`.
- Protocol registry & guards: `SharedProtocol/EnhancedMinecraft/ProtocolValidator` + `ProtoRuntime.EnsureInitialized()` validate descriptors/assemblies before chunk/time/weather/status handlers run. Generated DTOs in `Assets/Generated/Protobuf` and `SharedProtocol`.
- Room/instance routing: `RoomManager`/`SessionManager` lifecycle and per-room chunk broadcasts; UI migration prompts on client. Protos `RoomEnter/Leave/List`.

## Content (terrain, entities, gameplay)
- Terrain & hydrology: caves/rivers/lakes share water-table-aware hydrology mask, seam relax, edge stability, anisotropic river smoothing (flow-parallel bias), and hydrology-gradient-driven channels on both server and Unity (`MapGeneratorLib`). Config knobs live in `config/world.json` + `Assets/.../WorldConfigData.json` + `WorldGenerationConfig`.
- Biomes/weather/sky: biome tagging and weather scheduler on server (`WorldTimeSystem`, `WeatherSystem`); skybox/weather FX on client. Protos `WorldInfo`, `WeatherChange`.
- Structures/loot: dungeon/container generation + loot broadcast handlers; client container UI rendering. Protos `ContainerOpen/Update`, loot tables JSON.
- Entities/combat: spawn/update/despawn, combat resolution/pathing on server; prediction/rendering on client. Protos `EntitySpawn/Update/Despawn`, `PlayerAttack`.
- Items/crafting/inventory: authoritative inventory/recipe validation; UI drag/drop and recipe book on client. Protos `InventoryUpdate`, item use/drop/pickup; recipes JSON.

## Utility (data, tooling, ops)
- Config parity & tuning: JSON worldgen knobs (`config/world.json`, `Assets/.../WorldConfigData.json`) including `Hydrology*`, `River*`, `Lake*`, `Caves*`, plus new `RiverAnisotropyWeight` for flow-aligned smoothing; server loads via `WorldGenerationConfig`.
- Data-driven tables: blocks/recipes/mobs/loot/worldgen knobs all JSON-backed for maintainability.
- Tooling/protobuf: `proto/*.proto` -> `Assets/Generated/Protobuf` + `SharedProtocol`; validated by `ProtocolValidator.ValidateEnhancedContracts()`. Regenerate with `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`.
- Metrics/observability: chunk residency/time/weather metrics surfaced in `ServerStatusResponse`; recordings under `Recordings/`; dev HUD overlays on client.

## Implementation order (must-do)
1) Core authority first: keep proto registry validation (`ProtoRuntime.EnsureInitialized`, `ProtocolValidator.ValidateEnhancedContracts`) and hydrology seam/gradient/water-table clamps enabled before shipping chunks/world/time/weather/status.
2) Content next: tune hydrology-driven caves/rivers/lakes with flow-aligned anisotropic smoothing and config parity between server and Unity JSON.
3) Utility last: keep protobuf generation, configs, and data tables synchronized; capture metrics for river/lake seam stability and proto registry health.
