# Core/Content/Util feature map (2025-12-27)

Update: Added water-table-aware hydrology clamp shared between server `WorldManager` and Unity/MapGeneratorLib so caves/rivers/lakes stay aligned to the configured `GlobalWaterLevel`, plus stricter protobuf validation for weather updates. Use the tables below to keep client/server feature parity organized.

## Core (authority, world map control, protocol)
- World map control & chunk streaming — Server: `GameServer/World/WorldManager.cs`, `Generation/*`, `ChunkPayloadBuilder`; Client: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`, MapGeneratorLib mirror. Hydrology seam blend + edge stability + new water-table clamp keep chunk edges continuous. Config: `config/world.json` / `Assets/.../WorldConfigData.json` (`HydrologyEdge*`, `HydrologyWaterTableClampWeight/Range`, `HydrologyWarp*`, `RiverFlowAlignmentWeight`, `RiverGradientPenalty`, `GlobalWaterLevel`). Proto: `ChunkLoadRequest/Response`, `ChunkUnloadNotification/Ack`, `WorldInfo`, `TimeUpdate`, `WeatherChange`.
- Session/auth/movement — Server auth/heartbeat/spawn/respawn/anti-cheat, rate limiting; Client prediction/interp + death/respawn UX. Proto: `game_auth.proto`, `game_move.proto`, `game_core.proto`.
- Block interaction & permissions — Server validation/durability/ownership in `Handlers/WorldBlockHandler.cs`; Client placement/break UX + VFX/SFX. Proto: `BlockChangeRequest/Broadcast`, `MultiBlockChange`; data `config/blocks.json`.
- Protocol registry & guards — `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` validates registry/descriptor/parser bindings; now also requires `WeatherUpdateBroadcast.change_timestamp` so stale generated DTOs fail fast. `ProtoRuntime.EnsureInitialized()` runs at startup. Unity tooling should regen DTOs on proto edits.
- Room/instance routing — Room lifecycle + per-room chunk broadcasts in `RoomManager`/`SessionManager`; Unity room list UI + migration prompts. Proto: `RoomEnter/Leave/List`. Docs: `docs/server-rooms-architecture.md`.

## Content (worldgen, gameplay, entities)
- Terrain & hydrology (caves/rivers/lakes) — Server `WorldManager` hydrology mask/flow/erosion runs with seam blend, edge stability, anisotropic river smoothing, moisture/flow-biased cave supports, and new water-table clamp to bias rivers/lakes/caves toward configured sea level; MapGeneratorLib mirrors the pipeline for Unity previews. Config: `Water.*`, `Caves.*`, `Lakes.*`.
- Biomes/weather/sky — Server `WorldTimeSystem`/`WeatherSystem`; Client skybox/weather FX + biome VFX/SFX. Proto: `WorldInfo`, `WeatherChange`.
- Structures & loot — Server `DungeonGenerationStage`; Client container UI + loot rendering. Proto: `ContainerOpen/Update`; loot tables JSON.
- Entities/combat — Server spawn/update/despawn handlers + combat resolution; Client remote entity render/prediction. Proto: `EntitySpawn/Update/Despawn`, `PlayerAttack`.
- Items/crafting/inventory — Server authoritative inventory/recipes; Client drag/drop UI + recipe book. Proto: `InventoryUpdate`, item use/drop/pickup; recipes JSON.

## Utility (data, tooling, ops)
- Config/tuning alignment — Server loads `config/world.json` and `server-config.json`; Unity mirrors via `Assets/.../WorldConfigData.json`. New knobs: `HydrologyWaterTableClampWeight/Range` and configurable `GlobalWaterLevel` piped into MapGeneratorLib to match server water table.
- Data-driven tables — Blocks/recipes/mobs/loot/worldgen knobs stay JSON-driven across server + Unity.
- Metrics/observability — Chunk residency/time/rate-limit/status metrics surfaced via `ServerStatusResponse` and HUD hooks; recordings under `Recordings/`.
- Tooling/protobuf — Run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` then `dotnet build SharedProtocol/SharedProtocol.csproj`; ProtocolValidator guards namespace/descriptor/assembly drift, now including weather update timestamps.

## Sequenced rollout
1) Core: keep chunk routing + hydrology seam blend/stability + water-table clamp aligned across server/Unity; proto validation (`ProtoRuntime.EnsureInitialized`, `ProtocolValidator`) blocks stale DTOs (Chunk/World/Time/Weather/Status/Unload).
2) Content: hydrology-driven caves/rivers/lakes with seam stability and water-table bias using shared JSON knobs; keep riparian/erosion/slope feedback mirrored in MapGeneratorLib.
3) Utility: keep configs/JSON/protos/tools in lockstep; observe registry/descriptor health and world map control metrics (status payloads, seam QA captures).

## Current iteration focus
- Water-table clamp biases hydrology/flow near `GlobalWaterLevel` to stabilize rivers/lakes/caves at chunk seams on both server and Unity previews.
- MapGeneratorLib now reads `GlobalWaterLevel` from config instead of a fixed constant to stay in sync with server world map control.
- Protocol validation adds `WeatherUpdateBroadcast.change_timestamp` coverage to catch stale generated protobufs in both server and client builds.
