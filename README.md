# HELLO_MY_WORLD

This project is an open-source voxel game that aims to mimic the core mechanics of Minecraft. All source code and assets in this repository are available under the MIT license, though external libraries and resources may carry their own licenses.

![hello_my_world](https://user-images.githubusercontent.com/9248400/75618900-dc37ab00-5bb7-11ea-9ec0-9759c0b6429f.png)
![hmw_git_main_img](https://user-images.githubusercontent.com/9248400/102211930-b47fbc80-3f17-11eb-8d7a-53281bb826ce.png)

## Project Overview
- **Development period:** 2016/01 ~ 2021/12 (hold)
- **Engine:** Unity 6000.0.23f1
- **Language:** C# with Unity (.NET Framework 4.5) and standalone server components on .NET 6.0
- **Libraries:** NGUI 3.x, Sqlite3, JsonObject, Newtonsoft.Json, iTween, FMOD, UniRx, FreeNet, ECM, etc.
- **Platforms:** Windows PC (Android planned)
- **License:** MIT

## Repository Structure
- `Assets/` – Unity game content and scripts. `MyAssets/Scripts` includes modules for AI, GameWorld, Network, Player, UI, pathfinding, and more.
- `SharedProtocol/` – ProtoBuf-based message definitions and networking helpers shared between client and server.
- `GameServer/` – TCP server using `SharedProtocol`, `SessionManager`, and SQLite persistence.
- `KojeomNetWorkSpace/` – legacy `KojeomNet` network library and test clients.
- `MapGeneratorLib/` – standalone library for procedural map generation.
- `CustomToolSet/` – editor utilities such as `ActorGeneratorTool` and `MapTool`.
- `Documents/` – design documents and guides (`Project_PDD.md`).
- `Packages/` – Unity package manifest listing engine dependencies.
- `proto/` – Protobuf IDL files compiled into C# under `Assets/Generated/Protobuf`.
- `docs/` – networking overview and protocol notes.
- `Config/`, `ProjectSettings/`, `UserSettings/` – engine configuration files.
- `Recordings/` – gameplay capture sessions.

## Recent Updates
- 2025-10-31: World chunks now stream from the shared WorldManager pipeline with enhanced river/lake generation; see docs/minecraft_feature_worldgen_alignment.md for the rollout.
- 2025-10-27: Unity remote avatars now snap to respawn coordinates as soon as PlayerRespawn broadcasts arrive, keeping death feed messaging and entity state in sync (Task-19B / see docs/minecraft_feature_execution.md).
- 2025-10-26: Combat event broadcasts now drive the Unity CombatFeedbackUI damage feed (Task-15A/B). Track next steps in docs/minecraft_feature_execution.md (Task-15C).
- 2025-10-25: Server status analytics now include death/respawn counters and surface them in the Unity HUD (Task-20A / see docs/minecraft_feature_client_server_matrix.md).
- 2025-10-24: Unity HUD death feed now consumes `PlayerDeath` and `PlayerRespawnBroadcast` packets; see docs/minecraft_feature_client_server_matrix.md (F-19).
- 2025-10-17: Player respawn broadcasts now notify active sessions; follow the rollout in docs/minecraft_respawn_feature_plan.md.
- 2025-10-16: Server status HUD now reports chunk residency totals and peak players (see docs/minecraft_chunk_residency_metrics_plan.md).
- 2025-10-15: ContainerPanelUI scaffolding exposes shared container contents in the Unity HUD (see docs/minecraft_container_feature_plan.md).
- 2025-10-15: Server status telemetry now reports container hash mismatches and the Unity HUD surfaces the counter (see docs/minecraft_feature_masterlist.md).
- 2025-10-12: Container snapshot hash handshake ensures clients resync on mismatches (see docs/minecraft_container_feature_plan.md).
- 2025-10-09: Remote player distance culling and object pooling keeps remote avatars lightweight (see docs/minecraft_feature_execution.md).
- 2025-10-06: Velocity-aware remote player smoothing now clamps server velocity updates and predicts client transforms with damped interpolation (see docs/minecraft_feature_execution.md).
- 2025-10-05: Introduced EntitySyncService and the Unity RemoteEntityManager for server-authoritative remote player interpolation (see docs/minecraft_feature_execution.md).
- 2025-10-03: Added server-side inventory snapshot persistence with SQLite JSON storage and login-time broadcast sync (see docs/minecraft-feature-plan.md).
- 2025-10-04: Unity now consumes time and weather broadcasts for lighting, ambient FX, and HUD readouts (see docs/minecraft_feature_execution.md).

## Development Environment
- Unity Engine **6000.0.23f1**
- C# / .NET Framework 4.5 (Unity) & .NET 6.0 (server)
- IDE: Visual Studio, Rider, or VS Code

## Unity Package Dependencies
Key packages from `Packages/manifest.json` include:

- `com.unity.2d.sprite` 1.0.0
- `com.unity.2d.tilemap` 1.0.0
- `com.unity.ai.navigation` 2.0.8
- `com.unity.collab-proxy` 2.5.2
- `com.unity.ext.nunit` 2.0.5
- `com.unity.ide.visualstudio` 2.0.22
- `com.unity.multiplayer.center` 1.0.0
- `com.unity.postprocessing` 3.4.0
- `com.unity.recorder` 5.1.1
- `com.unity.render-pipelines.core` 17.0.3
- `com.unity.shadergraph` 17.0.3
- `com.unity.test-framework` 1.4.5
- `com.unity.timeline` 1.8.7
- `com.unity.ugui` 2.0.0
- `com.unity.xr.legacyinputhelpers` 2.1.11

See `Packages/manifest.json` for the full dependency list.

## Building and Testing
1. Clone this repository and open the root folder with **Unity 6000.0.23f1**.
2. Build the standalone .NET components:
   ```bash
   dotnet build SharedProtocol/SharedProtocol.csproj
   dotnet build GameServer/GameServer.csproj
   dotnet build MapGeneratorLib/MapGeneratorLib.sln
   ```
3. After installing the .NET SDK, run available tests with `dotnet test`.
4. Custom tools such as the map and actor generators can be opened through their solution files in `CustomToolSet/`.

## Additional Resources
There is a helpful tutorial used at the start of the project:<br>
http://studentgamedev.blogspot.kr/2013/08/unity-voxel-tutorial-part-1-generating.html

## Networking Protocol (Client ↔ Server)
- The client and server communicate over a simple framed protocol: `[TotalLength:int][MessageType:int][Payload:protobuf]`.
- See `docs/networking-protocol.md` for details, message type IDs, and client integration notes.
- Container messages now carry `container_type` and `snapshot_hash` fields so clients can validate diffs and request full resyncs on hash mismatches.
- Clients now emit `ChunkUnloadNotificationMessage` when dropping chunks and await `ChunkUnloadAcknowledgeMessage` so the server can free residency immediately.
- `ChunkDataResponseMessage` now includes an `EnhancedPayload` field containing the serialized `EnhancedMinecraftProtocol.ChunkLoadResponse`, and the server accepts batched `ChunkLoadRequest` messages alongside the legacy single-chunk request for backwards compatibility.
- After changing `.proto` definitions run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` to regenerate Unity-side contract classes.

## Time & Weather Systems
- The server now boots `WorldTimeSystem` to push `TimeUpdateMessage` snapshots on login and every tick so late joiners stay in sync.
- A companion `WeatherSystem` schedules configurable `WeatherChangeMessage` broadcasts driven by the new `WorldSettings` keys (`EnableWeatherCycle`, `WeatherTickIntervalSeconds`, `ClearWeatherDurationSeconds`, `RainWeatherDurationSeconds`, `StormWeatherDurationSeconds`, `SnowWeatherDurationSeconds`, `WeatherStormProbability`, `WeatherSnowProbability`).
- Tweak those values in `server-config.json` before launch to control cycle speed, duration, and precipitation mix.
- Unity clients should bind these packets to skybox lighting, precipitation FX, and ambient audio (see `docs/minecraft-feature-plan.md` F-10).

## Remote Player Entity Sync
- `EntitySyncService` now broadcasts player spawn, update, and despawn messages so remote avatars remain authoritative and discoverable by late joiners.
- Unity ships a `RemoteEntityManager` MonoBehaviour that subscribes to `MinecraftGameClient.EntityUpdated`, spawns remote player prefabs (or a fallback capsule), and smooths transforms with configurable lerp speeds.
- Attach `RemoteEntityManager` to your network scene root and assign a prefab to override the default capsule. See `docs/minecraft_feature_execution.md` (F-11) for remaining velocity and culling follow-ups.

## Server Rooms
- The server supports a room-based architecture to scope chat and block broadcasts.
- See `docs/server-rooms-architecture.md` for lifecycle and integration details.

## World Generation
- Server procedurally generates terrain, ores, caves, dungeons, and vegetation.
- See `docs/world-generation.md` for the pipeline and extension notes.
- Configure the day/night cycle via `WorldSettings` in `server-config.json` (`InitialWorldTime`, `InitialDayTime`, `EnableDayNightCycle`, `DayNightCycleSecondsPerDay`).

