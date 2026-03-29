# Minecraft Core/Content/Util Alignment (2025-12-11)

Summary of Minecraft-critical client/server features grouped by Core, Content, and Utility, plus current blockers and next actions. Keep configs/data/proto in lockstep so WorldManager, MapGeneratorLib, and Unity previews stay aligned.

## Core (authority, world control, protocol)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| World map control | `GameServer/World/WorldManager.cs` drives chunk pipeline (base terrain → caves → dungeons → rivers → lakes → vegetation/clouds) with hydrology caches and seam blending. Room-aware chunk routing via `SessionManager`. | `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs` request/unload chunks, apply streamed payloads, sky/time/weather. | Config: `config/world.json`, `server-config.json`; Unity mirror `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`. Protocol: `ChunkDataRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`. |
| Session/auth/movement | Auth/heartbeat/spawn/respawn/anti-cheat in `SessionManager`, handlers under `GameServer/Handlers`. | Prediction/interp in `Assets/MyAssets/Scripts/Network`, death/respawn UX. | Proto: `game_auth.proto`, `game_move.proto`, `game_world.proto`. |
| Block interaction | Validation + durability + ownership in `Handlers/WorldBlockHandler.cs`, `EnhancedModifyWorldManager` compatibility. | Placement/break UI, VFX/SFX feedback. | Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`. |
| Room/instance routing | Room lifecycle and per-room chunk broadcasts in `RoomManager`/`SessionManager`. | Room list UI and migration prompts. | Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`. |
| Protocol registry & guards | `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoRuntime.EnsureInitialized()`, `ChunkPayloadBuilder` fingerprint checks. | Unity tooling logs proto drift; regenerate DTOs when proto changes. | Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Content (worldgen/gameplay/entities)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Terrain + hydrology (caves/rivers/lakes) | `WorldManager` stages use hydrology/flow/stability caches with seam blending (`BlendHydrologySeams`, `RelaxHydrologySeams`) and erosion masks feeding caves/rivers/lakes. | MapGeneratorLib `WorldGenAlgorithms.cs` mirrors passes for Unity preview; streamed chunk payloads rendered by world scripts. | Config knobs: `Water.*` (HydrologySmooth*, HydrologyEdgeBlendRadius, HydrologyFlowPersistence, HydrologySeamRelax*, RiverNoiseScale, RiverDepth, RiverBankErosionWeight, LakeRimErosionWeight, LakeSpawnWeightBias, LakeShorelineBlend), `Caves.*` stability/noise thresholds. |
| Biomes/weather/sky | Biome tagging + weather scheduler (`WorldTimeSystem`, `WeatherSystem`). | Skybox/weather FX + biome VFX/SFX. | Proto: `WorldInfo`, `WeatherChange`. |
| Structures & loot | Dungeons/structures in `DungeonGenerationStage`; container broadcast handlers. | Render + interact via container UI. | Proto: `ContainerOpen/Update`; data: loot tables JSON. |
| Entities/combat | Spawn/update/despawn handlers, combat resolution, pathing. | Remote entity render/prediction. | Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`. |
| Items/crafting/inventory | Authoritative inventory/recipe validation in handlers. | UI drag/drop, recipe book. | Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON. |

## Utility (data, tooling, ops)
| Feature | Server | Client | Data / Protocol / Notes |
| --- | --- | --- | --- |
| Config/tuning alignment | Load `server-config.json` and `config/world.json`; expose to worldgen/networking. | Mirror values in `WorldConfigData.json` for previews/UI. | Keep seeds/hydrology/cave toggles, seam smoothing, erosion weights in JSON; document new keys. |
| Data-driven tables | Blocks, recipes, mobs, loot, worldgen knobs. | Load matching Unity JSON. | Ensure schemas shared across `config/*.json` and `Assets/...`. |
| Metrics/observability | Chunk residency, tick/time, rate limits, protocol validation. | Dev HUD overlays. | Proto: `ServerStatusRequest/Response`; recordings under `Recordings/`. |
| Tooling/protobuf | Regenerate DTOs from `proto/*.proto`; validate registry coverage. | Consume generated classes in networking layer. | Commands: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`; `dotnet build SharedProtocol/SharedProtocol.csproj`. |

## Immediate blockers / to-fix
- GameServer build errors (see log below): missing `PlayerInfo.DisplayName`, Vector3 type mismatch between `GameServerApp` and `SharedProtocol`, and `BroadcastToAreaAsync` signature mismatch in `Handlers/PlayerAttackHandler.cs`; Vector3 conversion errors in `Systems/CommandSystem.cs`. These need code-level fixes before server compile/test.
- MapGeneratorLib build fails locally because .NET Framework 4.5 reference assemblies are absent. Install the targeting pack or retarget to a supported framework before validating the cave/river/lake algorithms.
- `using` validation: the above errors show `using`/type drift between `GameServerApp.Vector3` and `SharedProtocol.Vector3`; align namespaces and DTO usage when fixing the build to ensure packet payloads use the generated protobuf vectors.

## Build/test status (2025-12-11)
- ✅ `dotnet build SharedProtocol/SharedProtocol.csproj` (warnings only: protobuf-net version float; nullable + async warnings in `Session.cs` and `MinecraftMessageDispatcher.cs`). Artifact: `SharedProtocol/bin/Debug/net6.0/SharedProtocol.dll`.
- ❌ `dotnet build GameServer/GameServer.csproj` failed with 5 errors (see blockers above) plus nullable/async warnings in handlers and worldgen.
- ❌ `dotnet build MapGeneratorLib/MapGeneratorLib.sln` failed: missing .NETFramework v4.5 reference assemblies (install developer pack).

## Next actions (server + client)
1. Fix GameServer compile blockers: add/access `DisplayName` on `PlayerInfo` or use existing property; normalize Vector3 usage between `SharedProtocol.Vector3` and `GameServerApp.Vector3`; update `BroadcastToAreaAsync` call site to match latest signature. Re-run `dotnet build GameServer/GameServer.csproj`.
2. Install .NET Framework 4.5 targeting pack (or retarget MapGeneratorLib) and rebuild to verify cave/river/lake algorithm alignment with `WorldManager`.
3. Regenerate protobuf DTOs (`protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`), then rebuild `SharedProtocol` and Unity-side compile to ensure `ProtocolValidator` stays green and all `using` references resolve.
4. Keep `config/world.json` and Unity `WorldConfigData.json` synchronized for hydrology/cave knobs (edge blend, flow persistence, seam relax, river depth/noise, rim erosion). Document any new keys in both JSON and `docs/world-generation.md`.
5. After fixes, run `dotnet run --project GameServer -- --selftest` and update client-side chunk streaming/protobuf handler coverage if packet shapes changed.
