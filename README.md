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
- `SharedProtocol/` – Shared networking contracts/utilities (legacy `protobuf-net` + Google.Protobuf `EnhancedMinecraftProtocol`).
- `GameServer/` – TCP server using `SharedProtocol`, `SessionManager`, and SQLite persistence.
- `KojeomNetWorkSpace/` – legacy `KojeomNet` network library and test clients.
- `MapGeneratorLib/` – standalone library for procedural map generation.
- `CustomToolSet/` – editor utilities such as `ActorGeneratorTool` and `MapTool`.
- `Documents/` – design documents and guides (`Project_PDD.md`).
- `Packages/` – Unity package manifest listing engine dependencies.
- `proto/` – Protobuf IDL files compiled into C# under `Assets/Generated/Protobuf`.
- `docs/` – networking overview, protocol notes, and the Minecraft feature workplan (`docs/minecraft_features_comprehensive_list.md`).
- `scripts/` – protobuf generation/verification + shared config sync helpers.
- `config/`, `ProjectSettings/`, `UserSettings/` – engine configuration files.
- `Recordings/` – gameplay capture sessions.

## Recent Updates
- 2026-02-16: Flow-shadowed hydrology and meander jitter keep caves/rivers/lakes stitched across seams on server (`ImprovedTerrainCoordinator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`, `ImprovedCaveGenerator`) and Unity previews (`EnhancedTerrainGenerator`, MapGeneratorLib smoothing), refreshed enhanced-terrain overrides (`config/` + `Assets/StreamingAssets/enhanced-terrain-config.json`), tightened protobuf descriptor origin guards (`ProtocolStandardization`), and published the latest core/content/util rollout (`config/minecraft_feature_core_content_util_2026-02-16.json`, `docs/minecraft_feature_core_content_util_2026-02-16.md`, `docs/minecraft_feature_core_content_util_latest.md`).
- 2026-01-03: Added basin-fill + edge-stitched hydrology/flow masks on server (`ImprovedTerrainCoordinator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`, `ImprovedCaveGenerator`) and Unity previews (`WorldMapController`), hot-reloaded map-control/world-config JSON on both server and client controllers, enforced EnhancedMinecraft descriptor filename checks (`ProtocolStandardization`), and published the refreshed core/content/util rollout (`config/minecraft_feature_core_content_util_2026-01-03.json`, `docs/minecraft_feature_core_content_util_2026-01-03.md`, `docs/minecraft_feature_core_content_util_latest.md`).
- 2026-02-15: Added variance-blended hydrology and confluence-aware river/lake masks on both server (`ImprovedTerrainCoordinator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`) and Unity preview (`WorldMapController`, refreshed `EnhancedTerrainGenerator` + cleaned `enhanced-terrain-config.json`), introduced a descriptor coverage audit in `ProtocolStandardization`, and published the updated core/content/util rollout (`config/minecraft_feature_core_content_util_2026-02-15.json`, `docs/minecraft_feature_core_content_util_2026-02-15.md`, `docs/minecraft_feature_core_content_util_latest.md`).
- 2026-01-02: Blended hydrology with flow to keep caves/rivers/lakes aligned across chunk seams (`ImprovedTerrainCoordinator`, Unity `WorldMapController`), added flow-suppressed caves and inflow/outflow-aware lakes, reloaded map-control profiles on `world.json` changes with bounded preview caches (`WorldMapControlManager`), tightened protobuf parser/fingerprint validation (`ProtocolStandardization`), and published the refreshed feature matrix (`config/minecraft_feature_core_content_util_2026-01-02.json`, `docs/minecraft_feature_core_content_util_2026-01-02.md`, `docs/minecraft_feature_core_content_util_latest.md`).
- 2026-02-14: Hardened hydrology/flow worldgen (edge flow locks, river delta sanding, lake radius caps/wetlands, moisture-aware cave supports) on server + Unity, refreshed the hashed map-control profile with `lakeMaxRadius`/simulation distance sync (new CLI `--generate-map-profile`) and StreamingAssets copy, enforced EnhancedMinecraft descriptor fingerprints in `ProtoRuntime`, published the latest core/content/util rollout (`config/minecraft_feature_core_content_util_2026-02-14.json`, `docs/minecraft_feature_core_content_util_2026-02-14.md`), and added baseline JSON defaults under `config/*.default.json`.
- 2026-02-13: Cached improved hydrology/flow masks per chunk in the server pipeline so caves/rivers/lakes share a single generation pass (`WorldManager.GenerateImproved*`), added a StreamingAssets profile auto-reloader to Unity `WorldMapController`, wired the Unity `ProtobufNetworkClient` through `ProtocolStandardization` + `EnhancedProtoManifest` validation, and published the updated core/content/util matrix (`config/minecraft_feature_core_content_util_2026-02-13.json`, `docs/minecraft_feature_core_content_util_2026-02-13.md`).
- 2026-02-12: Added edge-bleed hydrology smoothing and flow-aware cave/river/lake masks in the improved terrain pipeline (`GameServer/World/Generation/Improved*Generator.cs`, `EnhancedTerrainGenerationPipeline.cs`) and the Unity preview (`MapGeneratorLib/.../WorldGenAlgorithms.cs`), auto-reload the world-map-control profile + cache flush on hash drift (`WorldMapControlManager`, `WorldMapController`), tightened protobuf descriptor binding validation in `ProtocolValidator`, and refreshed the core/content/util matrix (`config/minecraft_feature_core_content_util.json`, `docs/minecraft_feature_core_content_util_2026-02-12.md`).
- 2026-02-11: Cleaned the duplicated Unity world config and aligned it with `config/world.json` (hydrology/cave/lake knobs + map-control version/path), upgraded the Unity terrain generator with slope/flow-aware river masks, wetland-friendly lakes, and stability-biased caves, and taught the Unity map controller to rebuild map-control profiles when versions drift. Feature matrix refreshed in `config/minecraft_feature_core_content_util.json` and `docs/minecraft_feature_core_content_util_2026-02-11.md`.
- 2025-12-30: Upgraded cave/river/lake generation (stability smoothing, confluence boosts, wetland rings, edge sealing) in `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs` + `Improved*Generator.cs`, added map-control profile/hash propagation in `WorldMapControlManager` and Unity `EnhancedWorldMapController` (chunk-size aware map textures + server profile rebinding), refreshed the core/content/util JSON plan (`config/minecraft_feature_core_content_util.json`) and documented the execution order in `docs/minecraft_feature_execution_plan.md`, and verified EnhancedMinecraft protobuf bindings remain covered by `ProtocolValidator`.
- 2025-12-29: Rebuilt the world-map controllers (server/client) around a single hashed `WorldMapControlProfile`, replaced the duplicate `EnhancedTerrainGenerationPipeline` with a hydrology-aware version that honors `config/world.json` noise + cave/river/lake knobs, and added proactive EnhancedMinecraft handler contract validation in `UnifiedMessageHandler`/`ProtocolValidator`. A fresh core/content/util rollout is captured in `docs/minecraft_feature_core_content_util_2025-12-29-world-control.md`.
- 2026-02-11: Harmonized river intensity with hydrology gradients and flow accumulation (seam-aware passes in `GameServer/World/Generation/ImprovedWorldGeneration.cs`), added lake rim stabilization and moisture-based cave barriers, and tightened Unity map-control loading to rebuild hashed profiles when config drift is detected. `ProtocolValidator` now also checks generated EnhancedMinecraft protobufs resolve from the expected descriptor file to catch stale `using` bindings early. Core/content/utility planning refreshed in `docs/minecraft_feature_core_content_util_2026-01-04.md`.
- 2025-12-28: Added hydrology edge-noise damping on both the server (`GameServer/World/WorldManager.cs`) and Unity preview (`MapGeneratorLib/.../WorldGenAlgorithms.cs`) to keep caves/rivers/lakes aligned across chunk seams; `EnhancedWorldMapController` now loads the hashed `WorldMapControlProfile` shared with StreamingAssets; protobuf bindings are validated at boot via `ProtocolStandardization.ValidateProtocolImplementation()`; the core/content/utility feature inventory now mirrors to `config/minecraft_feature_core_content_util.json` and `docs/minecraft_feature_core_content_util_2025-12-28.md`.
- 2025-12-28: Added riparian-aware hydrology buffering so river/lake/cave passes blend nearby saturation before shaping (server `GameServer/World/WorldManager.cs`, MapGenerator preview `MapGeneratorLib/.../WorldGenAlgorithms.cs`, map-control wiring in `WorldAreaManager`). Lake wetland pocket spawning now honors the JSON wetland buffer radius, and a data-driven feature inventory lives at `config/minecraft_feature_core_content_util.json` with the latest rollout mirrored in `docs/minecraft_feature_core_content_util_latest.md`.
- 2026-02-10: Added riparian buffer dilation and river seam-fill passes to keep rivers/lakes continuous across chunk edges, grew configurable lake wetland buffers, and reinforced cave ceilings in saturated terrain. New JSON knobs (`Water.RiparianBufferRadius`, `Water.RiverSeamFillStrength`, `Lakes.WetlandBufferRadius`, `Caves.CeilingStabilityWeight`) are mirrored into the map-control profile/hash for Unity StreamingAssets.
- 2025-12-27: Hardened river mouths, lake outflows, and chunk-edge cave sealing across the dedicated server (`WorldManager`) and Unity terrain generator (now reading hydrology knobs directly from `StreamingAssets/world-config.json`). Cleaned the duplicated world map controller, added map-control path fallback, and removed UnityEngine-only logging from shared `GameCommon` config/data managers to keep `dotnet build` healthy.
- 2025-12-25: Smoothed riparian saturation with new JSON knobs (`Water.RiparianSmoothIterations/Blend/SaturationBoost`), added riparian cave plugs (`Caves.RiparianPlugDepth`) and configurable lake shoreline shelves (`Lakes.ShelfDepth`) in both the server world-gen and MapGeneratorLib, refreshed map-control profiles, and added `ProtocolValidator.ValidateHandlerBindings()` so EnhancedMinecraft handlers are validated against generated protobuf DTOs at startup.
- 2025-12-24: Added a curvature-aware hydrology stability pass for rivers/lakes/caves on both server (`GameServer/World/WorldManager.cs`) and MapGeneratorLib, and tightened map-control loading by falling back to JSON config when the streamed profile hash mismatches. `MinecraftMessageDispatcher` now warns when handlers register packets without EnhancedMinecraft bindings so protobuf drift is surfaced during bootstrap.
- 2025-12-24: Expanded world-map control profile (warp/edge stability, anisotropy, cave support weights) synced to `config/world_map_control_profile.json` and `Assets/StreamingAssets/world-map-control.json` with new hash, added warp-aware hydrology smoothing + anisotropic river banks/lake outlets + gradient-aware cave supports on both server and MapGeneratorLib, and refreshed the core/content/utility feature matrix for server/client sequencing.
- 2026-02-09: Added a riparian saturation map shared by the dedicated server (`GameServer/World/Generation/ImprovedWorldGeneration.cs`) and MapGeneratorLib to smooth river benches, reinforce lake wetlands, and seal aquifer-heavy caves near rivers/lakes. Wired `WorldSynchronizationManager` into `WorldBlockHandler` with a periodic dispatcher so block edits are queued/batched and broadcast via protobuf world-sync DTOs.
- 2026-02-06: Exported hashed world-map-control snapshots (`config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`) with Unity loader parity, added support pillars + river mouth delta smoothing + lake outflow channels (new JSON knobs: `RiverMouthSmoothRadius`, `RiverDeltaWetlandStrength`, `SupportPillarChance`, `WetlandSaturationThreshold`, `OutflowCarveDepth`), and cleaned duplicate protobuf client enums/handlers.
- 2026-01-22: Added flow-aligned hydrology smoothing with divergence clamp for rivers/lakes/caves on both server (`WorldManager.StabilizeHydrologyGradients`) and MapGeneratorLib, driven by new JSON knobs (`Water.HydrologyDirectionalIterations/Blend/FlowDivergenceClamp`). World map control now applies render/simulation distance to chunk residency and Unity subworld loaders, and chunk handlers assert protobuf registry health via `ProtoDiagnostics.AssertRegistryClean()`.
- 2026-01-21: Added hydrology edge-flux projection and river seam feathering (JSON: `Water.HydrologyEdgeFluxBlend`, `Water.RiverEdgeFeather`) plus cave edge sealing (`Caves.EdgeSealStrength`) so rivers/lakes/caves stay aligned across chunk seams on both server and MapGeneratorLib. Refreshed the core/content/utility feature matrix at `docs/minecraft_feature_core_content_util_2026-01-21.md` to keep client/server sequencing in sync.
- 2025-12-22: Implemented comprehensive core gameplay systems including PlayerController with movement and block interaction, InventoryManager with hotbar and main inventory support, CraftingManager with multiple crafting types, and HealthHungerSystem with survival mechanics. Added data-driven JSON configuration files for items, recipes, and client/server settings. All systems are designed to work with the existing protobuf protocol and terrain generation systems.
- 2026-01-20: Hydrology edge consistency now blends chunk-border samples toward downhill gradients before rivers/lakes/caves run, and river intensity gains an edge-feather pass so channels stay aligned at seams on both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`. Noise caves now read river pressure to suppress carving beneath active channels and flood more realistically under rivers, and chunk load requests clamp to the JSON-driven map-control render distance in `MinecraftChunkHandler`. Docs refreshed (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2026-01-20.md`).
- 2025-12-18: Added a variance-aware hydrology smoothing pass before cave/river/lake carving on both server (`GameServer/World/WorldManager.cs`) and Unity previews (`MapGeneratorLib/.../WorldGenAlgorithms.cs`), driven by new JSON knobs (`HydrologyVarianceBlend`, `HydrologyVarianceClamp`) mirrored into the map-control profile. ProtocolValidator now guards duplicate/mismatched EnhancedMinecraft descriptor bindings so stale generated DTOs fail fast.
- 2025-12-17: Server now dual-supports legacy vs Google.Protobuf EnhancedMinecraftProtocol packets per-session (auto-detected via chunk/action handlers) and applies improved cave/river/lake worldgen stages based on `config/world.json`. Added `scripts/generate_proto.ps1` and `scripts/sync_world_config.ps1` with docs under `docs/`.
- 2025-12-16: Added curvature-weighted hydrology gradients and confluence-aware river bank widening across `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, plus lake basin smoothing driven by new JSON knobs (`HydrologyCurvatureWeight`, `RiverConfluenceBoost`, `LakeBasinSmoothIterations`). `ProtoDiagnostics.LogHandlerCoverage()` now reports handler gaps during startup, and docs were refreshed (`docs/minecraft_feature_core_content_util_2025-12-16.md`, `terrain_generation_improvements.md`, `protobuf_protocol_improvements.md`).

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
- Chunk streaming uses Google.Protobuf `EnhancedMinecraftProtocol.ChunkLoadRequest/ChunkLoadResponse` on the wire (batched chunk positions + multi-chunk responses). Legacy `ChunkDataResponseMessage` still exists for backwards compatibility, including the optional `EnhancedPayload` bridge.
- Chunk unload flow uses Google.Protobuf `EnhancedMinecraftProtocol.ChunkUnloadNotification/ChunkUnloadAck` (the server still accepts legacy `ChunkUnloadNotificationMessage` and may respond with legacy `ChunkUnloadAcknowledgeMessage` when required).
- After changing `.proto` definitions regenerate Unity-side contract classes:
  - Preferred (no system `protoc` install): `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/generate_proto.ps1`
  - Then validate: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1`
  - If you already have `protoc` installed: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`
- `SharedProtocol.EnhancedMinecraft.ChunkPayloadBuilder` now executes `ProtocolValidator.ValidateEnhancedContracts()` on first use; run `dotnet build SharedProtocol/SharedProtocol.csproj` after regenerating protobufs so descriptor mismatches fail fast.
- Unity networking (`GameNetworkManager`) also calls `ProtocolRegistry.ValidateBindings()` and `ProtocolValidator.ValidateEnhancedContracts()` during bootstrap so stale generated DTOs are caught before any packets are sent.

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
- 2025-11-10 hydrology refresh: karst sinkholes + aquifer vents, sub-chunk tributary stitching, and clay/sand shoreline terraces now keep `MapGeneratorLib` and `GameServer.WorldManager` outputs visually identical, which simplifies authoring chunk previews inside the Unity tools.
- 2025-12-27 map-control parity: Unity now overrides `WorldConfigData.json` fields with the exported `config/world_map_control_profile.json` on startup, runs the shared hydrology seam clamps, and mirrors the server river-mouth smoother so chunk borders, caves, rivers, and lakes stay in lockstep with streamed server chunks.

## Core Gameplay Systems
- **PlayerController**: Comprehensive player movement, block interaction, and inventory management with support for both first-person and third-person controls
- **InventoryManager**: Full inventory system with hotbar, main inventory, item stacking, and save/load functionality
- **CraftingManager**: Multi-type crafting system (hand, workbench, furnace) with recipe management and validation
- **HealthHungerSystem**: Survival mechanics with health, hunger, status effects, and regeneration systems
- All systems are data-driven through JSON configuration files in `Assets/MyAssets/Scripts/DataFiles/`

## Configuration Management
- **Server Configuration**: `server-config.json` contains network, world, gameplay, and performance settings
- **Client Configuration**: `Assets/StreamingAssets/client-config.json` contains graphics, audio, controls, and interface settings
- **Game Data**: `Assets/MyAssets/Scripts/DataFiles/items.json` and `crafting_recipes.json` provide item definitions and crafting recipes
- All configuration files follow a hierarchical structure for easy maintenance and modding support

## Known Issues
- Protobuf validation may emit `[Proto][WARN]` for helper/nested messages that are not network-level packets; startup still fails fast for missing network packet bindings.
