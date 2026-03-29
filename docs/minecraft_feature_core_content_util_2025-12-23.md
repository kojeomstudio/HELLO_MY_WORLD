# Core / Content / Utility Feature Map (2025-12-23)

Latest split of Minecraft-aligned features across server and Unity client, with data/protocol anchors and the order to land them. This supersedes the 2025-12-20 map.

## Core (authority, world map control, protocol)
- **World map control & chunk streaming** – Server: `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder`; Unity: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `MapGeneratorLib` previews. Hydrology edge tangent projection (`HydrologyEdgeTangentWeight`) keeps river/lake/cave masks aligned across chunk borders.
- **Session/auth/movement** – Server authority for auth/heartbeat/spawn/respawn/anti-cheat; Unity prediction/interp. Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`.
- **Block interaction & permissions** – Server validation + rollback (`Handlers/WorldBlockHandler.cs`, EnhancedModifyWorldManager); Unity placement/break UI + VFX/SFX. Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`.
- **Protocol registry & guards** – `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` now also validates entity descriptors (`EntityData/Spawn/Despawn`) plus existing chunk/world/time/weather checks. Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`.
- **Room/instance routing** – Room lifecycle + per-room chunk broadcasts (`RoomManager`, `SessionManager`); Unity room list UI. Proto: `RoomEnter/Leave/List`.

## Content (worldgen, gameplay, entities)
- **Terrain & hydrology (caves/rivers/lakes)** – Shared hydrology/flow masks with seam blending, relaxation, slope anchoring, and new edge-tangent projection so rivers/lakes/cave moisture follow downhill/tangent paths across chunks. Config knobs mirrored in Unity: `Hydrology*`, `River*`, `Lake*`, `Caves.*` (see `config/world.json`, `Assets/.../WorldConfigData.json`).
- **Biomes/weather/sky** – Server: `WorldTimeSystem`, `WeatherSystem`; Unity skybox/weather FX. Proto: `WorldInfo`, `WeatherChange`, `TimeUpdate`.
- **Structures & loot** – Dungeons/structures (`Generation/DungeonGenerationStage`), container snapshots/handlers; Unity renders + UI. Proto: `ContainerOpen/Update`.
- **Entities/combat** – Spawn/update/despawn, combat resolution, pathing; Unity remote entity render/prediction. Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`.
- **Items/crafting/inventory** – Server authoritative inventory/recipes; Unity drag/drop + recipe book. Proto: `InventoryUpdate`, item use/drop/pickup; JSON recipes.

## Utility (data, tooling, ops)
- **Config/tuning alignment** – JSON-driven knobs for hydrology/caves/rivers/lakes; new `HydrologyEdgeTangentWeight` mirrored between server (`config/world.json`) and Unity (`Assets/.../WorldConfigData.json`). Server parser: `WorldGenerationConfig`; Unity parser: `WorldConfigFile`.
- **Data-driven tables** – Blocks, recipes, mobs, loot, worldgen knobs all JSON-backed.
- **Metrics/observability** – Chunk residency, tick/time, rate limits, protocol validation; Unity dev HUD overlays. Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`.
- **Tooling/protobuf** – Regenerate DTOs from `proto/*.proto`; `ProtocolValidator` enforces descriptor/namespace/assembly/parser consistency including entity/world control messages.

## Sequenced implementation order
1. **Core/authority**: keep chunk streaming + hydrology seam/tangent projection in sync, validate proto registry/entity descriptors before accepting traffic.
2. **Content/worldgen**: apply shared hydrology/tangent smoothing in `WorldManager` and `MapGeneratorLib` so rivers/lakes/cave moisture stay continuous across chunk borders; tune with JSON knobs.
3. **Utility/tooling**: ensure configs remain mirrored, regen protobufs after IDL changes, keep data tables (blocks/recipes/mobs/worldgen) aligned for client/server parity and metrics visibility.
