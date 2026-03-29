# Minecraft Core / Content / Utility (2025-11-30)

Authoritative client/server feature breakdown for Minecraft gameplay. Use this list to stage work in order (core authority → content/worldgen → utility/tooling) and to keep JSON data + protobufs aligned.

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming + persistence; room-aware routing in `SessionManager.cs` | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs` request/unload + apply payloads; sky/time/weather updates | Config: `config/world.json`, `server-config.json`; Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`. Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat in `SessionManager.cs`, handlers under `GameServer/Handlers/*` | Prediction/interp in `Assets/MyAssets/Scripts/Network/*`, death/respawn UX | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, `ChunkPayloadBuilder` fingerprint checks | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion, generation stages in `Generation/Stages/*` | MapGeneratorLib mirrors hydrology/cave/rive/lake masks for Unity preview; streamed chunks rendered by world scripts | Config knobs: `Water.*` (`HydrologySmooth*`, `HydrologyEdgeBlendRadius`, `HydrologyFlowPersistence`, `HydrologySeamRelax*`, `RiverNoiseScale`, `RiverDepth`, erosion weights) + `Caves.*` thresholds/frequencies. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | Keep seeds/hydrology/cave toggles, seam smoothing, erosion weights in JSON; document new keys. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1. Core/authority: chunk routing + proto validation before accepting traffic (Chunk/World/Time/Weather/Unload messages).
2. Content/worldgen: hydrology-driven caves/rivers/lakes + dungeon/biome pass using JSON knobs shared with Unity previews.
3. Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control.
