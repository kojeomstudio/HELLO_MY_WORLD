# Minecraft Core/Content/Utility Execution (2025-12-07)

Scoped rollout plan that keeps server and client in lockstep for Minecraft features. Each row maps to code, data, and protocol anchors so work can proceed in order without drift.

## Core (authority, world control, protocol)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| World map authority & chunk streaming | `GameServer.WorldManager`, chunk cache/DB, room-aware routing | `WorldAreaManager`, chunk mesh bake/load/unload | `config/world.json`, `WorldConfigData.json`, `ChunkLoadRequest/Response`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast` | Hydrology/cave masks share tunables (`HydrologyShorePush/SlopePenalty/FlowGain`, `HydrologySmooth*`, `RiverNoiseScale`, `RiverDepth`) across server and MapGeneratorLib. |
| Session/login & respawn | Auth, spawn/respawn authority, death feed source | Login UI, spawn positioning, death/respawn messaging | `LoginRequest/Response`, `PlayerRespawnBroadcast`, `ServerStatusResponse` | Respawn coordinates stay consistent with streamed chunk ownership and room routing. |
| Block interaction & permissions | Validate/place/break, rollback, ownership | Input -> request, VFX/SFX feedback | `BlockChangeRequest/Broadcast`, `MultiBlockChange`, recipes JSON | Block/table data remains JSON-driven for diff-safe updates. |
| Protocol registry & validation | `ProtocolValidator.ValidateEnhancedContracts`, `ProtocolRegistry` bindings | Unity bootstrap calls `ProtoRuntime.EnsureInitialized()` | `proto/*.proto`, generated C# under `Assets/Generated/Protobuf` | Guards stale `using` references and descriptor drift before handlers run. |

## Content (world, gameplay, entities)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Terrain, rivers, lakes, caves | `WorldManager` chunk passes with hydrology/cave smoothing | MapGeneratorLib preview mirrors tunables, mesh bake | `config/world.json`, `WorldConfigData.json`, chunk payloads | River spacing/depth keyed off `RiverNoiseScale`/`RiverDepth`; shoreline smoothing via `HydrologyShorePush/SlopePenalty/FlowGain` to keep seams aligned. |
| Biomes, weather, sky | Biome tagging, weather scheduler, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherUpdateBroadcast`, biome tables JSON | Driven by JSON world config; day/night + precipitation tunables mirrored. |
| Structures & loot | Placement rules, loot persistence | Render + interact | Chunk payloads, container messages | Loot/structure tables stay JSON-driven; server authoritative. |
| Entities, AI, combat | Spawn rules, combat resolution | Render entities, hit FX, input | `EntitySpawn/Update/Despawn`, `PlayerAttack` | Damage/AI parameters stored in data files. |
| Items, crafting, inventory | Recipe validation, inventory authority | UI, drag/drop, recipe book | `InventoryUpdate`, item use/drop/pickup messages | Items/recipes remain JSON; hashes must align for snapshot diffs. |

## Utility (data, tooling, operations)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Config & tuning | `server-config.json`, `config/world.json` loaders | `WorldConfigData.json` mirror | `WorldInfo` sync | JSON-first; seeds + hydrology/cave thresholds stay aligned. |
| Metrics & observability | Chunk residency, tick/frame timing, handler coverage | Dev HUD overlays | `ServerStatusRequest/Response` | Log hydrology/river tuning on boot. |
| Tooling & protobuf | `ProtoRuntime`, `ProtocolValidator`, data schema lint | Editor tooling, MapTool previews | `proto/*.proto`, schema JSON | Keep proto/data regeneration part of CI; build `SharedProtocol` to catch drift. |

## Execution order
1) Core world control + registry validation (chunk/time/weather/entity/block/room) with synchronized config paths.  
2) Content passes (terrain + hydrology + caves + rivers + lakes + biomes/entities) using shared masks so MapGeneratorLib and server chunks stay identical.  
3) Utility surfaces (config mirrors, metrics, proto/data validation) to keep tuning, observability, and regeneration safe.  

## Config/Proto checkpoints
- Verify `HydrologyShorePush`, `HydrologySlopePenalty`, `HydrologyFlowGain`, `HydrologySmooth*`, `RiverNoiseScale`, and `RiverDepth` match between `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.
- Re-run protobuf generation (`protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`) and `dotnet build SharedProtocol/SharedProtocol.csproj` when `.proto` files change to satisfy `ProtocolValidator.ValidateEnhancedContracts()` guards.
