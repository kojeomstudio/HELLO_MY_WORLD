## Minecraft core/content/util plan (2025-12-18)
- Updated to capture the client/server split for the Minecraft feature set plus the new hydrology variance smoothing we are rolling into worldgen. Keep configs JSON-first and mirror them into Unity so previews stay aligned with streamed chunks.

### Core (authority, world map control, protocol)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `World/Generation/*`, `SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs`; map-control profile now carries hydrology variance knobs for chunk-edge stability | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib uses the same map-control profile to drive preview generation and chunk load cadence | Config: `config/world.json`, Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`. Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdateBroadcast`, `WeatherUpdateBroadcast`. |
| Session/auth/movement | `Program.cs`, `SessionManager.cs`, `Handlers/*` for auth/heartbeat/spawn/anti-cheat | Prediction/interp + respawn UX | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | `Handlers/WorldBlockHandler.cs` for validation/durability/ownership, rollback, EnhancedModifyWorldManager bridge | Placement/break UI, VFX/SFX | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`; registry/descriptor/parser coverage plus enhanced handler coverage checks on boot | Unity tooling uses generated DTOs; regenerate when proto changes | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

### Content (terrain/worldgen, gameplay, entities)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology/flow/curvature fields feed improved cave/river/lake stages with variance-aware blending at chunk seams | MapGeneratorLib mirrors hydrology smoothing + variance clamping so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology smooth/edge/gradient/curvature, variance blend/clamp, river depth/noise, confluence boost) + `Caves.*` (stability/support) + `Lakes.*` (basin smoothing/proximity). |
| Biomes/weather/sky | `WorldTimeSystem`, `WeatherSystem`, biome tagging | Sky/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | `Generation/Stages/DungeonGenerationStage.cs` + container handlers | Container UI + loot render | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`, `Health/Hunger` broadcasts. |
| Items/crafting/inventory | Inventory/recipe validation handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

### Utility (data, tooling, ops)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | JSON-first (`config/world.json`, `server-config.json`); expose hydrology variance + map-control settings through `WorldGenerationConfig` and `WorldMapControlProfile` | Mirror values in `WorldConfigData.json` for previews/UI; apply to MapGeneratorLib static knobs | Keep seeds/hydrology/cave/lake toggles, seam smoothing, variance blend/clamp, river/lake erosion weights in sync; document new keys. |
| Protobuf pipeline | `proto/*.proto` -> generated DTOs in `SharedProtocol` and Unity | Consume generated classes; handler coverage logging | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Tooling/metrics | Scripts under `scripts/`, chunk residency + server status telemetry, recordings | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |

### Sequenced implementation order
1) Core: wire updated map-control profile (chunk/render/sim distances + hydrology variance knobs) and keep EnhancedMinecraft protocol validation on boot.
2) Content: apply hydrology variance smoothing before cave/river/lake passes in both server worldgen and MapGeneratorLib so streamed chunks match Unity previews.
3) Utility: keep JSON configs + generated protobufs synchronized; emit diagnostics when handlers/descriptors drift; document config changes.
