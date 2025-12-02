Current feature split and rollout order across server and Unity client. Updated 2025-12-02; retunes hydrology anisotropy and cave support hydration so MapGeneratorLib previews stay in lockstep with WorldManager chunks (flow persistence 0.68, river flow alignment 0.28 / gradient penalty 0.42, three river smoothing passes, river depth 6, shoreline blend 0.66, cave support hydration/flow bias 0.42/0.20).

## Core (authority, world map control, protocol)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control & chunk streaming | `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder` streaming/persistence; river smoothing uses flow-aware/gradient-penalized passes before shipment | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`; MapGeneratorLib mirrors hydrology/river knobs so Unity previews match streamed chunks; time/weather updates | Config: `config/world.json`, `Assets/.../WorldConfigData.json` mirror hydrology smoothing (flow persistence 0.68, edge blend), anisotropic rivers (flow alignment 0.28, gradient penalty 0.42, 3 passes, depth 6). Proto: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat, rate limiting | Prediction/interp, death/respawn UX, reconnection | Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`. |
| Block interaction & permissions | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`; rollback + EnhancedModifyWorldManager compatibility | Placement/break UI, VFX/SFX feedback | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()` invoked in `Program.cs`, registry/descriptor/parser validation + assembly consistency guard | Unity tooling logs proto drift; regenerate DTOs when proto changes | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |
| Room/instance routing | Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager` | Room list UI + migration prompts | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |

## Content (worldgen, gameplay, entities)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain & hydrology (caves/rivers/lakes) | `WorldManager` hydrology mask + flow + erosion with flow-aligned seam smoothing/relaxation; anisotropic river smoothing (flow alignment 0.28, gradient penalty 0.42, 3-pass intensity smoothing, depth 6); cave supports moisture/flow-biased (0.42/0.20); hydrology warp keeps humidity sampling coherent | MapGeneratorLib mirrors hydrology/cave/river/lake masks with the same anisotropic river smoothing and moisture-biased support columns so Unity previews match streamed chunks | Config knobs: `Water.*` (hydrology seam/warp, flow persistence 0.68, river noise/depth/smoothing/anisotropy) + `Caves.*` stability/support knobs (`SupportDensity`, `SupportHydrationBias` 0.42, `SupportFlowBias` 0.20, stability weights) shared between server and Unity JSON. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`) | Skybox/weather FX + biome VFX/SFX | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers | Render + interact via container UI | Proto: `ContainerOpen/Update`; loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing | Remote entity render/prediction | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers | UI drag/drop, recipe book | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server (files) | Client (files) | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `config/world.json` + `server-config.json`; expose to worldgen/networking | Mirror values in `WorldConfigData.json` for previews/UI | Hydrology/river/lake knobs aligned: flow persistence 0.68, edge blend radius 3, river anisotropy (0.28/0.42) with 3 smoothing passes, river depth 6, shoreline blend 0.66, lake rim erosion 0.30, cave support hydration/flow 0.42/0.20. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs | Load matching Unity JSON | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation | Dev HUD overlays | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage + parser/assembly availability | Consume generated classes in networking layer | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Sequenced implementation order
1) Core/authority: chunk routing + hydrology seam smoothing with anisotropic river blending and proto validation before accepting traffic (Chunk/World/Time/Weather/Unload messages).
2) Content/worldgen: hydrology-driven caves/rivers/lakes with stronger flow persistence, deeper channels, smoother shorelines, and moisture/flow-biased cave supports using shared JSON knobs.
3) Utility/tooling: keep configs, generated protobufs, and data tables in lockstep; add metrics/recordings around world map control, hydrology quality, seam stability, and proto registry health.

## Current iteration scope (2025-12-02)
- Raise hydrology flow persistence to 0.68 and align MapGeneratorLib defaults with JSON so flow direction and humidity stay coherent across chunk seams.
- Add flow-directional hydrology smoothing (anisotropic blending that favors downhill vectors + continuity weight) in both WorldManager and MapGeneratorLib to keep world map control/chunk seams stable before streaming.
- Increase anisotropic river smoothing (flow alignment 0.28, gradient penalty 0.42) with 3-pass intensity smoothing (blend 0.58) and river depth 6 to stabilize channels/banks for caves, rivers, and lakes.
- Smooth lakes more aggressively (shoreline blend 0.66, rim erosion 0.30) and bias cave supports toward hydrated/flowing cells (0.42/0.20) to keep flooded caverns stable.
- Keep protobuf validation enforced via `ProtocolValidator`; rebuild `SharedProtocol` after any proto regeneration to ensure `using` references remain valid on server and Unity.
