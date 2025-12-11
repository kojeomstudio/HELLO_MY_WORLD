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
- `docs/` – networking overview, protocol notes, and the Minecraft feature workplan (`docs/minecraft_feature_plan.md`, `docs/minecraft_feature_client_server_sequence.md`).
- `Config/`, `ProjectSettings/`, `UserSettings/` – engine configuration files.
- `Recordings/` – gameplay capture sessions.

## Recent Updates
- 2026-01-12: Flow-aware river width modulation and inflow-aligned lake shaping landed in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, with edge-relaxed cave thresholds to stitch noise caves across chunk seams. `MinecraftMessageDispatcher.AssertHandlerCoverage()` now guards EnhancedMinecraft handler registration at startup, and gameplay JSON configs (`config/*.json`) were normalized for item/hunger/client data. See `terrain_generation_improvements.md` and `protobuf_protocol_improvements.md` for details.
- 2026-01-07: Hydrology gradients now blend slope + flow accumulation with a configurable clamp (`HydrologyGradientSlopeWeight`, `HydrologyGradientClamp`) in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, keeping rivers/lakes/caves aligned across chunk seams and Unity previews. Configs updated (`config/world.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`) with rollout notes in `docs/minecraft_feature_core_content_util_2026-01-07.md`.
- 2026-01-05: Hydrology-gradient smoothing now aligns downhill vectors for caves/rivers/lakes across chunk seams in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, keeping world map control consistent between streamed chunks and Unity previews. Docs refreshed (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2026-01-05.md`, and `docs/minecraft_feature_core_content_util_latest.md`).
- 2026-01-09: Added edge-feathered hydrology smoothing (reusing the existing edge blend/variance knobs) ahead of river/lake/cave passes in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, added flow-variance + hydrology-gradient river width modulation, and aligned lake orientation/perturbation to inflow gradients. `ProtoDiagnostics` now treats optional Minecraft enums as informational so required EnhancedMinecraft bindings still fail fast without blocking optional packets. See `docs/terrain_generation_improvements.md`, `docs/protobuf_protocol_improvements.md`, `docs/minecraft_feature_core_content_util_2026-01-09.md`, and `docs/minecraft_feature_core_content_util_latest.md`.
- 2025-12-31: Added flow-aligned anisotropic river smoothing controlled by `RiverAnisotropyWeight` in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, keeping chunk-seam banks coherent while respecting hydrology gradients and water-table clamps. Configs are mirrored in `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`; docs refreshed (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-31.md`, and `docs/minecraft_feature_core_content_util_latest.md`).
- 2025-12-30: Hydrology gradient cache (new `HydrologyGradientWeight`) now guides cave density/thresholds, river channel pressure/normalization, and lake overflow direction across `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, still mirrored via JSON. Headwater-aware river smoothing, lake inflow routing, and moisture-retention-biased cave stability remain in place, and chunk handlers call `ProtoRuntime.EnsureInitialized()` + `ProtocolValidator.ValidateChunkContracts()` to guard EnhancedMinecraft payloads. See `docs/minecraft_feature_core_content_util_2025-12-30.md` and `docs/minecraft_feature_core_content_util_latest.md`. 
- 2025-12-29: Flow accumulation now biases uphill contributions by water-table proximity, slope attenuation, and edge persistence in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/.../WorldGenAlgorithms.cs`, reducing cliff floods and chunk-seam jitter for rivers/lakes/caves. ProtocolValidator coverage was re-audited for the existing action/world/time/weather/status contracts. Docs refreshed (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-29.md`, and `..._latest.md`).
- 2025-12-28: Slope-aware water-table clamping now attenuates hydrology/flow boosts on steep terrain via `HydrologyWaterTableSlopeWeight` (shared between `WorldManager` and MapGeneratorLib) so rivers, lakes, and cave moisture stay tied to the configured sea level without flooding cliffs. ProtocolValidator also audits `PlayerActionRequest/Response/ActionResult` descriptors to catch stale generated protobuf bindings, and configs/docs mirror the new knob (`config/world.json`, `Assets/.../WorldConfigData.json`, `docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-28.md`, and `..._latest.md`).
- 2025-12-27: Water-table clamp biases hydrology/flow toward the configured `GlobalWaterLevel` across `WorldManager` and MapGeneratorLib using `HydrologyWaterTableClampWeight/Range`, and Unity now reads the same water level from `WorldConfigData.json`. `ProtocolValidator` also validates `WeatherUpdateBroadcast.change_timestamp` to catch stale generated DTOs. Docs refreshed (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-27.md`, and `..._latest.md`).
- 2025-12-26: Hydrology seam stability adds `HydrologyEdgeStabilityIterations/Weight` shared between `WorldManager` and MapGeneratorLib so rivers/lakes/caves stay aligned at chunk seams, and `ProtocolValidator` now validates `ServerStatusResponse` descriptors. Configs/docs updated (`docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-26.md` and `..._latest.md`).
- 2025-12-04: Water-table-aware cave stability and hydrology-weighted river intensity normalization keep caves/rivers/lakes aligned between WorldManager and MapGeneratorLib; lake candidate heatmaps now penalize erosion risk. ProtocolValidator now audits enum coverage (with optional messages excluded) to catch missing EnhancedMinecraft bindings. See the refreshed rollout map in `docs/minecraft_feature_plan.md`.
- 2025-12-03: Edge flow-lock hydrology seams now blend toward downstream samples using `HydrologyEdgeFlowLockWeight` in both WorldManager and MapGeneratorLib, keeping rivers/lakes/cave moisture continuous across chunk borders. ProtocolValidator also rejects duplicate EnhancedMinecraft descriptor bindings to catch stale `using` directives. Docs and JSON configs mirror the new knob.
- 2025-12-23: Hydrology seam anchoring now projects edge rings along the local hydrology tangent to keep rivers/lakes/cave moisture aligned across chunk borders; tune with `HydrologyEdgeTangentWeight` in `config/world.json` and Unity `WorldConfigData.json`. Updated worldgen docs and a refreshed core/content/util rollout map in `docs/minecraft_feature_core_content_util_2025-12-23.md` (linked from `docs/minecraft_feature_core_content_util_latest.md`).
- 2025-12-22: Hydrology edge consistency pass now clamps hydrology/flow at chunk borders before stabilization, with smoother defaults for hydrology/cave stability; see `MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`. Added a refreshed core/content/util feature catalog at `docs/minecraft_feature_catalog.md` to coordinate client/server rollout.
- 2025-12-21: Added relief-aware river intensity dampening and river-proximity suppression for lakes (configurable via `RiverReliefPenaltyWeight` and `RiverProximitySuppression` in `config/world.json`), plus a shared client/server feature catalog at `docs/minecraft_feature_catalog.md` to track core/content/utility rollouts.
- 2025-12-20: River intensity smoothing now aligns with flow direction and penalizes cross-gradient blending (`RiverFlowAlignmentWeight`, `RiverGradientPenalty`) in both `GameServer/World/WorldManager.cs` and MapGeneratorLib, and cave support columns react to moisture/flow via `SupportHydrationBias`/`SupportFlowBias`. Hydrology humidity sampling in MapGeneratorLib now uses the shared warp knobs (`HydrologyWarpFrequency`, `HydrologyWarpAmplitude`), with JSON mirrored in `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`. See `docs/minecraft_feature_core_content_util_2025-12-20.md` and `docs/minecraft_feature_core_content_util_latest.md`.
- 2025-12-18: Slope-anchored hydrology seam projection now aligns rivers/lakes/caves to terrain grade at chunk edges across WorldManager and MapGeneratorLib, and ProtocolValidator validates the WeatherUpdateBroadcast descriptor so stale protobuf `using` bindings fail fast. See `docs/world-generation.md` and `docs/minecraft_feature_core_content_util_2025-12-18.md`.
- 2025-12-17: Hydrology seam variance clamping (`HydrologyEdgeVarianceClamp`) now aligns hydrology/flow at chunk edges in both `WorldManager` and MapGeneratorLib, keeping rivers, lakes, and caves continuous with the same JSON knob mirrored in Unity. Server boot now calls `ProtoRuntime.EnsureInitialized()` so stale EnhancedMinecraft protobuf bindings fail fast before networking. See `docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-17.md`, and `docs/minecraft_feature_core_content_util_latest.md`.
- 2025-12-16: River intensity smoothing now runs multiple hydrology-aware passes driven by `RiverIntensitySmoothIterations/RiverIntensitySmoothBlend`, and cave supports respect a shared `CaveSupportDensity` knob so wet caverns stay stable across server chunks and Unity previews. See `docs/world-generation.md`, `docs/minecraft_feature_core_content_util_2025-12-16.md`, and `docs/minecraft_feature_core_content_util_latest.md`.
- 2025-12-15: Flow-aligned hydrology seam bias (`HydrologyEdgeFlowBias`) now guides edge smoothing/relaxation in both `WorldManager` and MapGeneratorLib, keeping caves/rivers/lakes aligned across chunk borders. `ProtocolValidator` also checks generated EnhancedMinecraft protobuf assemblies to catch stale `using` bindings. See `docs/world-generation.md` and `docs/minecraft_feature_core_content_util_2025-12-15.md`.
- 2025-12-13: Hydrology seam blending now walks multi-ring neighborhoods (up to `HydrologyEdgeBlendRadius`) with continuity + flow-persistence weighting in both `WorldManager` and MapGeneratorLib, eliminating river/lake/cave stair-steps at chunk borders. `docs/world-generation.md` and `docs/minecraft_feature_core_content_util_2025-12-13.md` capture the rollout, and `ProtocolValidator` still guards generated parser availability to catch stale protobuf `using` references.
- 2025-12-10: Hydrology seam relaxation (`HydrologySeamRelaxIterations/Blend`) now runs after normalization on both WorldManager and MapGeneratorLib, aligned with shared hydrology smoothing/flow persistence so Unity previews match streamed chunks. `ProtocolValidator` now validates `ChunkUnloadNotification/Ack` descriptors to catch stale protobuf bindings. See `docs/minecraft_feature_core_content_util_2025-12-10.md` and `docs/world-generation.md`.
- 2025-12-09: Added edge-aware hydrology seam blending/persistence knobs (`HydrologyEdgeBlendRadius`, `HydrologyFlowPersistence`) shared by `WorldManager` and `MapGeneratorLib` to reduce chunk-edge drift for rivers/lakes/caves; Unity now loads the same settings. See `docs/minecraft_feature_core_content_util_2025-12-09.md` and `docs/minecraft_feature_core_content_util_latest.md` for rollout details.
- 2025-12-08: Added JSON-driven hydrology seam continuity (`HydrologyContinuityWeight`) so `WorldManager.BlendHydrologySeams` and `MapGeneratorLib` blend hydrology/flow masks across edge rings, and erosion-risk smoothing now follows the shared hydrology smooth settings from `config/world.json`/Unity. See `docs/world-generation.md` and `docs/minecraft_feature_core_content_util_latest.md` for rollout notes.
- 2025-12-07: River noise/depth and hydrology smoothing knobs (`HydrologyShorePush/SlopePenalty/FlowGain`, `HydrologySmooth*`, `RiverNoiseScale`, `RiverDepth`) are now data-driven in both `config/world.json` and Unity `WorldConfigData.json`, applied in `WorldManager` and `MapGeneratorLib` so rivers/lakes/caves stay aligned. See `docs/minecraft_feature_core_content_util_2025-12-07.md` and `docs/minecraft_feature_core_content_util_latest.md` for the refreshed rollout.
- 2025-12-05: Hydrology gradient stabilization now mirrors MapGeneratorLib inside `WorldManager` before river/lake/cave passes, and ProtocolValidator requires the full EnhancedMinecraft registry set (chunk/time/weather/entity/block/SFX/VFX). See `docs/world-generation.md` and `docs/minecraft_feature_core_content_util_2025-12-05.md` for the updated rollout.
- 2025-12-03: Hydrology and cave smoothing knobs (`HydrologySmoothIterations/HydrologySmoothBlend`, `RiverBankErosionWeight`, `LakeRimErosionWeight`, `StabilitySmoothIterations/StabilitySmoothBlend`) now live in both `config/world.json` and Unity `WorldConfigData.json`, applied in `WorldManager` and `MapGeneratorLib` for aligned rivers/lakes/caves. `docs/world-generation.md` and `docs/minecraft_feature_core_content_util.md` capture the updated rollout and tuning map.
- 2025-11-25: World generation now loads hydrology/cave/lake tuning from `config/world.json` (path controlled by `server-config.json`), with Unity `WorldConfigData.json` mirroring the knobs. Lakes clamp radius/depth and shoreline smoothing from config, and the server now fails fast if any EnhancedMinecraft protobuf handlers are missing. See `docs/minecraft_feature_core_content_util.md` for the refreshed core/content/util rollout map.
- 2025-11-24: Added hydrology-gradient stabilization across rivers, lakes, and noise caves in `MapGeneratorLib` so water tables and flow accumulation stay smooth near chunk seams. Enhanced `ProtocolValidator` now validates world-control descriptors (`WorldInfo`, `WeatherInfo`, `WorldBorder`, `TimeUpdateBroadcast`) to catch stale protobuf `using` references early, and the new `docs/minecraft_feature_inventory.md` captures the core/content/util rollout order for Minecraft features.
- 2025-11-23: Introduced an erosion-risk field shared by caves, rivers, and lakes so hydrology/relief now guide carve depth across chunk seams in both `WorldManager` and `MapGeneratorLib`. River intensity now smooths with that field to avoid jagged banks, and lakes pick up irregular, noise-perturbed rims in wetter basins. `ProtocolValidator.ValidateRegistryPrototypes()` now guards generated EnhancedMinecraft namespaces/descriptors so stale `using` references fail fast. See `docs/world-generation.md` and `docs/minecraft_feature_client_server_sequence.md` for rollout details.
- 2025-11-22: Improved the cave generation system in `GameServer/World/WorldManager.cs`. Refactored the noise-based cave pass to use a new `CaveGenerationSettings` class, making it easier to tune. Added a new "Flooded Caves" feature that can generate large, water-filled cave systems below the global water level. A detailed plan for further improvements has been added to `docs/minecraft_feature_plan.md`.
- 2025-11-20: Hydrology seam stitching now smooths river/lake/cave masks across chunk borders in both `WorldManager` and `MapGeneratorLib`, and MapGeneratorLib river generation restores the riparian saturation map used by the server. `ProtocolValidator.ValidateRegistryDescriptors` plus expanded `ProtoDiagnostics` now guard every EnhancedMinecraft descriptor/package binding. See `docs/world-generation.md`, `docs/minecraft_feature_client_server_sequence.md`, and `docs/minecraft_feature_execution_2025-11-20.md` for details.
- 2025-11-19: Riparian-weighted river benches, moisture-aware cave ceiling sealing, and riparian-driven surface lake sizing now run in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib`. `ProtoDiagnostics` also flags any `MinecraftMessageType` that lacks a `ProtocolRegistry` binding so stale protobuf `using` references fail fast. See `docs/world-generation.md`, `docs/minecraft_feature_client_server_sequence.md`, and `docs/minecraft_feature_execution_2025-11-16.md` for rollout details.
- 2025-11-18: Gradient-aware cave runoff (`ExtendCaveHydrologyRunoff`), riparian seepage channels, and shoreline hydrology feedback now run in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib`, keeping caves, rivers, and lakes visually identical across streamed chunks and Unity previews. `ProtocolRegistry.ValidateBindings()` was added to `ProtocolValidator.ValidateEnhancedContracts()` so stale EnhancedMinecraft protobuf bindings fail fast on both server boots and Unity tooling. See `docs/world-generation.md`, `docs/minecraft_feature_client_server_sequence.md`, and `docs/minecraft_feature_execution_2025-11-18.md` for the full rollout notes.
- 2025-11-17: Introduced the riparian saturation field, lake candidate heatmap, and hydrology-weighted cave dripstone pass to MapGeneratorLib so Unity tooling previews the same rivers, shorelines, and cavern props the server will stream. `MinecraftMessageDispatcher` now validates that every EnhancedMinecraft message type has a registered handler before gameplay starts, and the new `docs/minecraft_feature_execution_2025-11-17.md` log captures the client/server rollout sequence alongside the refreshed notes in `docs/world-generation.md`.
- 2025-11-16: Hydrology feedback tuning added `ApplyCaveHydrologyErosion`, `ApplyRiverHydrologyFeedback`, and `StabilizeLakeCatchments` so caves, rivers, and lakes now respond to the shared saturation masks identically across MapGeneratorLib and WorldManager. SharedProtocol introduces `ProtoRuntime.EnsureInitialized()` so protobuf validation/logging fires exactly once regardless of how the server boots. See `docs/world-generation.md` for the algorithms and `docs/minecraft_feature_client_server_matrix.md` for the updated client/server runlist.
- 2025-11-15: Cave ribbon terraces, river meander benches, and multi-ring shoreline terraces landed in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib`, keeping spelunking routes, sandbars, and lake benches identical across server chunks and Unity previews. The refreshed `ProtocolRegistry` now publishes descriptor metadata so `ProtoDiagnostics` automatically flags stale EnhancedMinecraft protobufs. See `docs/world-generation.md` for the terrain details and `docs/minecraft_feature_client_server_sequence.md` for the updated feature rollout.
- 2025-11-14: Ventilation shafts, confluence delta fans, and lake overflow channels now run in lock-step across WorldManager and MapGeneratorLib, and `ProtoDiagnostics.AssertRegistryClean()` fails startup if EnhancedMinecraft registrations drift from the generated protobufs (see docs/minecraft_feature_client_server_matrix.md & docs/world-generation.md).
- 2025-11-13: Stability-weighted cave shelf terraces, river floodplain swales, and lake wetland spillways now run in both WorldManager and MapGeneratorLib, and the new `ProtoFingerprint` gate blocks stale protobuf assets before the server boots or the Unity client connects (see `docs/world-generation.md` & `docs/minecraft_feature_client_server_matrix.md`).
- 2025-11-11: Added a shared cave-stability field with stone support columns, braided river wetlands/point bars, shoreline vegetation + seep carving for lakes, and a new `ProtoDiagnostics` report that validates EnhancedMinecraft registrations during both server boot and Unity tooling (see `docs/world-generation.md` & `docs/minecraft_feature_client_server_matrix.md`).
- 2025-11-10: Karst sinkholes, tributary stitching, clay-banked lakes, and stricter EnhancedMinecraft protobuf validation now run in both `WorldManager` and `MapGeneratorLib`. The shared `ChunkPayloadBuilder` now enforces `ProtocolValidator.ValidateEnhancedContracts()` so stale generated code is caught instantly (see `docs/minecraft_feature_plan.md` & `docs/world-generation.md`).
- 2025-11-09: Hydrology-driven cave pools, river sediment terraces, and terraced lakes now run in both WorldManager and MapGeneratorLib, and the server validates ChunkLoadRequest/Response descriptors at startup (see docs/minecraft_feature_worldgen_alignment.md & docs/minecraft_feature_plan.md).
- 2025-11-07: Flow-accumulation rivers/lakes plus enhanced chunk metadata decoding landed. Catchment-weighted carving now runs in both `WorldManager` and `MapGeneratorLib`, and the Unity client logs/records EnhancedMinecraft protobuf payloads (see docs/minecraft_feature_worldgen_alignment.md & docs/minecraft_feature_plan.md).
- 2025-11-06: Hydrology-driven rivers/lakes, updated multi-frequency noise caves, and the runtime protobuf validator landed; see docs/minecraft_feature_worldgen_alignment.md for details.
- 2025-11-03: Hydrology tuning adds riverbank erosion, lake outflows, and cross-platform cave detail; see docs/minecraft_feature_plan.md for the updated backlog and linkage notes.
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
- `SharedProtocol.EnhancedMinecraft.ChunkPayloadBuilder` now executes `ProtocolValidator.ValidateEnhancedContracts()` on first use; run `dotnet build SharedProtocol/SharedProtocol.csproj` after regenerating protobufs so descriptor mismatches fail fast.

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

## Known Issues
- The `GameServer` project is currently failing to build due to a number of pre-existing errors. These issues were discovered during recent feature work and appear to be unrelated to the changes made. A fix is in progress, but the current commit contains these build errors.
