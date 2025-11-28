# Minecraft Core/Content/Utility Inventory (2025-12-06)

End-to-end checklist of Minecraft-required features split by Core, Content, and Utility. Each row calls out the server/client owners plus the data/protocol sources so the rollout can proceed in order.

## Core (authority, world control, protocol)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| World control & chunk streaming | `GameServer.WorldManager` pipeline, chunk cache/DB, room-aware routing | `WorldAreaManager`, chunk mesh bake/load/unload | `config/world.json`, `WorldConfigData.json`, `ChunkLoadRequest/Response`, `WorldInfo` | Hydrology/cave/lake masks stay aligned with MapGeneratorLib; room-aware chunk routes keep shard separation. |
| Session/login & respawn | Auth, spawn/respawn authority, death feed source | Login UI, spawn positioning, death/respawn messaging | `LoginRequest/Response`, `PlayerRespawnBroadcast`, `ServerStatusResponse` | Respawn coordinates must sync with remote avatar smoothing. |
| Entity sync & time/weather | Tick loop, entity spawn/update/despawn, time/weather schedulers | Remote entity interpolation, skybox/weather FX | `EntitySpawn/Update/Despawn`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast` | Keep tick/lerp tunables in JSON config. |
| Block interaction & permissions | Validate/place/break, rollback, ownership | Input -> request, VFX/SFX feedback | `BlockChangeBroadcast`, `MultiBlockChange`, recipes JSON | Recipes/blocks remain data-driven. |
| Protocol registry & validation | `ProtocolValidator.ValidateEnhancedContracts`, `ProtocolRegistry` bindings | Unity startup calls `ProtoRuntime.EnsureInitialized()` | `proto/*.proto`, generated C# under `Assets/Generated/Protobuf` | Guards stale `using` references and descriptor drift across client/server. |

## Content (world + gameplay)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Terrain, rivers, lakes, caves | WorldManager chunk passes with hydrology/cave smoothing | MapGeneratorLib mirrors for previews, mesh bake | `config/world.json`, `WorldConfigData.json`, chunk payloads | Shared erosion/hydrology masks keep seams identical; banks/shorelines stay data-driven. |
| Biomes, weather, sky | Biome tagging, weather scheduler, light levels | Skybox/weather FX, biome VFX/SFX | `WorldInfo`, `WeatherUpdateBroadcast`, biome tables JSON | Driven by JSON world config; keep day/night + precipitation tunables mirrored. |
| Structures & dungeons | Placement rules, loot persistence | Render + interact | Chunk payloads, container messages | Loot/structure tables stay JSON-driven; server authoritative. |
| Entities, AI, combat | Spawn rules, combat resolution | Render entities, hit FX, input | `EntitySpawn/Update/Despawn`, `PlayerAttack` | Damage/AI parameters stored in data files. |
| Items, crafting, inventory | Recipe validation, inventory authority | UI, drag/drop, recipe book | `InventoryUpdate`, item use/drop/pickup messages | Items/recipes remain JSON; hashes must align for snapshot diffs. |

## Utility (ops, data, tooling)
| Feature | Server owner | Client owner | Data / Protocol | Notes |
| --- | --- | --- | --- | --- |
| Config & tuning | `server-config.json`, `config/world.json` loaders | `WorldConfigData.json` mirror | `WorldInfo` sync | JSON-first; seeds + thresholds stay aligned. |
| Metrics & observability | Chunk residency, tick/frame timing, handler coverage | Dev HUD overlays | `ServerStatusRequest/Response` | Log hydrology/cave tuning on boot. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | `Resources` JSON loaders | Shared JSON schemas | Validate hash/version to avoid drift. |
| Tooling & validation | `ProtoRuntime`, `ProtocolValidator`, data schema lint | Editor tooling, MapTool previews | `proto/*.proto`, schema JSON | Keep proto/data regeneration part of CI. |

Hydrology smoothing and shoreline pressure now expose `HydrologyShorePush`, `HydrologySlopePenalty`, and `HydrologyFlowGain` alongside the existing smooth iterations/blend in both `config/world.json` and `Resources/TextAsset/GameWorld/WorldConfigData.json`. River frequency and depth are likewise data-driven (`RiverNoiseScale`, `RiverDepth`) so WorldManager and MapGeneratorLib generate identical river spacing and channel cuts.

## Sequenced rollout
1) Core world control + registry validation (chunk/time/weather/entity/block, room routing) with synchronized config paths.  
2) Content passes (terrain + hydrology + caves + rivers + lakes + biomes/entities) using shared masks so MapGeneratorLib and server chunks stay identical.  
3) Utility surfaces (config mirrors, metrics, proto/data validation) to keep tuning, observability, and regeneration safe.
