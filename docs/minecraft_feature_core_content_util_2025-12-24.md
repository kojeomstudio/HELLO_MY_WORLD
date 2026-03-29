# Minecraft feature split (core/content/util) – 2025-12-24
Server + Unity client features grouped for the current world-map control iteration. Focus: hydrology edge consistency (shared server/Unity), lake basin stability, and protocol registry guards.

## Core (authority, world map, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `World/Generation/*`, `ChunkPayloadBuilder` streaming/persistence. New hydrology edge consistency pass keeps river/lake masks stable at chunk seams before carving. | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors the hydrology edge consistency + river smoothing knobs so Unity previews match server chunks. | `config/world.json` (Hydrology/River/Lake/Cave knobs), `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`; proto `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn, rate limiting | Prediction/interp, death/respawn UX | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback hooks | Placement/break UI + VFX/SFX | Data: `config/blocks.json`; proto: block change/multi-change. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/*` (`ProtocolValidator`, `ProtoRuntime`, `ProtocolRegistry`, `ChunkPayloadBuilder`) ensure generated DTOs are present and parsers/descriptors come from the same assembly | Unity networking bootstrap calls `ProtoRuntime.EnsureInitialized()` | Regenerate via `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; build `SharedProtocol/SharedProtocol.csproj`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion; new edge-consistency smoothing feeds rivers and lakes; lake spawn checks basin relief/hydrology gradient; cave stability keeps moisture/flow bias for supports. | MapGeneratorLib mirrors hydrology/river/lake/cave pipeline including the edge-consistency pass so Unity previews align with streamed chunks. | `config/world.json` (`Water.*`, `Caves.*`, `Lakes.*`), Unity JSON mirrors. |
| Biomes/weather/sky | `WorldTimeSystem`, `WeatherSystem`, biome tagging in `WorldManager` | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | `DungeonGenerationStage`, container broadcasts | Render + interact via container UI | Proto: container open/update; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, AI | Render + prediction | Proto: entity spawn/update/despawn, combat events. |
| Items/crafting/inventory | Authoritative inventory/recipes in handlers | UI drag/drop, recipe book | Proto: inventory/item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json`, `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | Keep hydrology/river/lake/cave knobs in sync between JSONs; no new keys added this iteration. |
| Data-driven tables | Blocks, recipes, mobs, worldgen knobs | Load matching Unity JSON | Keep schemas aligned across `config/*.json` and Unity resources. |
| Tooling/protobuf | Regenerate DTOs; validate registry coverage + parser/assembly availability | Consume generated classes in networking | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Metrics/observability | Chunk residency, hydrology quality, proto registry health | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |

## Sequenced implementation order (current iteration)
1. Core: keep proto registry/descriptor validation enforced (`ProtocolValidator.ValidateEnhancedContracts`) and keep hydrology/river defaults loaded from `config/world.json` on server and Unity.
2. Content: apply hydrology edge-consistency smoothing before river/lake carving; maintain moisture/flow-biased cave supports so server chunks and Unity previews stay in lockstep.
3. Utility: update docs, keep JSON knobs aligned, run `dotnet build` (SharedProtocol, GameServer), refresh protobufs if `.proto` changes before shipping new world data.
