# Minecraft Feature Inventory (Core / Content / Util)

This inventory lists the Minecraft-critical client and server capabilities by category, tied back to the directories that own them. Use it as the order-of-operations checklist when landing work: ship **Core** first, then **Content**, then **Util/Tooling**.

## Core (gameplay must-have)
- **World streaming & control**: `GameServer/World/WorldManager.cs`, `GameServer/Handlers/MinecraftChunkHandler.cs`, `Assets/MyAssets/Scripts/GameWorld/*` (chunk/area managers), `MapGeneratorLib` (shared terrain). Includes river/lake/cave passes, chunk residency, and save/load.
- **Networking & protocol**: `SharedProtocol/*`, `SharedProtocol/EnhancedMinecraft/*`, generated protobufs in `Assets/Generated/Protobuf`, client glue in `Assets/MyAssets/Scripts/Network/*`. Validated by `ProtocolValidator.ValidateEnhancedContracts()`.
- **Session, auth, movement**: `GameServer/SessionManager.cs`, auth/handshake handlers under `GameServer/Handlers/`, movement/state sync DTOs in `SharedProtocol/MinecraftMessages.cs`, client controllers in `Assets/MyAssets/Scripts/MovableObjects` and input in `Assets/MyAssets/Scripts/Input`.
- **Block/world mutation**: block data + chunk meshes (`Assets/MyAssets/Scripts/GameWorld/Chunk/*`, `Assets/MyAssets/Scripts/GameWorld/ModifyWorldManager.cs`), server-side persistence and apply path (`GameServer/Systems/*`, `GameServer/Database/*`), shared block enums in `MapGeneratorLib` and `GameCommon`.
- **Config & data-driven state**: JSON configs in `config/*.json`, runtime settings in `server-config.json`, data files under `Assets/MyAssets/Scripts/DataFiles`. All new tunables belong in JSON with matching runtime reader.

## Content (player-facing features)
- **Blocks, biomes, materials**: `config/blocks.json`, block metadata in `Assets/MyAssets/Scripts/GameWorld`, biome/world rules in `config/world.json` and `MapGeneratorLib` passes (ore, sediment, shoreline, caves).
- **Items, crafting, inventory**: DTOs in `SharedProtocol/MinecraftMessages.cs`, item data in `config/gameplay.json`, Unity inventory UI/logic under `Assets/MyAssets/Scripts/UI` and `Assets/MyAssets/Scripts/Inventory` (when present).
- **Entities (mobs/AI)**: AI protocols in `SharedProtocol/GameProtocol.cs`, server logic in `GameServer/AI` and `GameServer/Systems`, client-side behaviors in `Assets/MyAssets/Scripts/AI`.
- **Structures & set dressing**: tree/env passes in `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/EnviromentGenAlgorithms.cs`, dungeon/ore/feature passes in `GameServer/World/WorldManager.cs` and `MapGeneratorLib`.
- **UI/HUD**: chat, time/weather, container and status displays in `Assets/MyAssets/Scripts/UI`, network bindings in `Assets/MyAssets/Scripts/Network`.

## Util / Tooling / Ops
- **Proto/tooling**: IDL in `proto/*.proto`, regeneration scripts `scripts/generate_proto.*`, diagnostics in `SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`.
- **Build & test**: `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`, Unity play/edit-mode tests in `Assets/Tests`, server smoke test `dotnet run --project GameServer -- --selftest`.
- **Debug/metrics**: protocol logging (`SharedProtocol/MinecraftMessageDispatcher.cs`), server diagnostics under `docs/` (feature rollout logs), capture files in `Recordings/`.
- **Editor/runtime utilities**: map tools under `CustomToolSet/`, octree/pathfinding helpers in `Assets/MyAssets/Scripts/CustomStructure` and `Assets/MyAssets/Scripts/PathFinding`, math/noise helpers in `MapGeneratorLib/MapGeneratorLib/Sources/Math` and `MapGeneratorLib/MapGeneratorLib/Sources/Noise`.

## Suggested sequencing
1. Stabilize **Core** paths (protocol registry checks, worldgen + chunk flow, session + movement).
2. Layer **Content** (biomes/blocks/entities/structures) with JSON-driven data and matching protobufs.
3. Wire **Util/Tooling** (proto generation, tests, diagnostics) to guard regressions before release.
