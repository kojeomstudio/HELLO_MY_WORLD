# Minecraft Feature Implementation Plan

This document enumerates the client and server level capabilities that are required to support the enhanced Minecraft experience in this repository. It also records the current implementation status and the immediate follow-up actions so the features can be implemented or refined sequentially.

## 1. High-Level Goals
- Deliver a cohesive Minecraft-like gameplay loop covering world generation, player interaction, progression, and live service needs.
- Ensure client and server responsibilities are clearly delineated while sharing protocol contracts through protobuf.
- Maintain deterministic world state through robust terrain algorithms (caves, rivers, lakes) and synchronized network messaging.

## 2. Server Capabilities
| Priority | Feature | Description | Status | Owner System / File(s) | Next Action |
|---|---|---|---|---|---|
| P0 | Session & Authentication | Account login, session lifecycle, rate limits | Implemented | `GameServer/Handlers/LoginHandler.cs`, `GameServer/SessionManager.cs` | Periodic audit |
| P0 | Chunk Generation & Storage | Deterministic chunk pipeline with persistence | Implemented | `GameServer/World/WorldManager.cs`, `GameServer/World/Generation/TerrainGenerationPipeline.cs` | Enhance cave/river/lake stages (current task) |
| P0 | Chunk Streaming | Serve chunk payloads, unload notifications | Implemented | `GameServer/Handlers/MinecraftChunkHandler.cs` | Validate enhanced payload sanity checks |
| P0 | Block Updates | Authoritative block mutations, history logging | Implemented | `GameServer/Handlers/MinecraftPlayerActionHandler.cs`, `GameServer/Handlers/WorldBlockHandler.cs` | Ensure terrain edits propagate protobuf events |
| P1 | Entity Synchronization | Spawn/Despawn/Movement broadcast | Implemented | `GameServer/Systems/EntitySyncService.cs` | Confirm proto usage for entity payload |
| P1 | Inventory & Crafting | Snapshot persistence, crafting rules | Implemented | `GameServer/Handlers/InventoryHandler.cs`, `GameServer/Handlers/CraftingHandler.cs` | Add protobuf-backed inventory updates |
| P1 | Environment Simulation | Time, weather, biome-aware updates | Implemented | `GameServer/Systems/WorldTimeSystem.cs`, `GameServer/Systems/WeatherSystem.cs` | Integrate client notifications |
| P1 | Procedural Structures | Trees, lakes, caves, rivers, dungeons | Implemented w/ hydrology-weighted rivers & basin-aware lakes (2025-11-07) | `MapGeneratorLib/...`, `WorldManager` stages | Monitor catchment tuning + sediment smoothing |
| P2 | Metrics & Maintenance | Health checks, chunk eviction, backups | Implemented | `GameServer/Systems/ServerMetricsService.cs`, `GameServer/GameServer.cs` | Automate reporting |
| P2 | Chat & Social | Room management, chat dispatch | Implemented | `GameServer/Handlers/ChatHandler.cs`, `GameServer/Room` | Extend moderation hooks |

## 3. Client Capabilities
| Priority | Feature | Description | Status | Owner System / File(s) | Next Action |
|---|---|---|---|---|---|
| P0 | Networking Core | Connect/disconnect, heartbeat, reconnection | Implemented | `Assets/MyAssets/Scripts/Network/GameNetworkManager.cs` | Ensure protobuf channel integration |
| P0 | Chunk Streaming & Rendering | Request chunks, mesh generation, culling | Implemented | `Assets/MyAssets/Scripts/GameWorld/...`, `Assets/MyAssets/Scripts/InstancingHelper.cs` | Enhanced protobuf metadata parsed on client; monitor mesh rebuild timings |
| P0 | Block Interaction | Place/destroy blocks, durability feedback | Implemented | `Assets/MyAssets/Scripts/GameWorld/...`, `Assets/MyAssets/Scripts/Player/...` | Align with server action protocol |
| P0 | Player Control | Movement, camera, input mapping | Implemented | `Assets/MyAssets/Scripts/Input/...`, `Assets/MyAssets/Scripts/Player/...` | Verify physics sync with server |
| P1 | Inventory UI | Hotbar, crafting grid, tooltips | Implemented | `Assets/MyAssets/Scripts/UI/...`, `Assets/MyAssets/Scripts/GameMode/...` | Add protobuf-driven updates |
| P1 | Entity Rendering | Visualize nearby entities with animations | Implemented | `Assets/MyAssets/Scripts/GameWorld/...` | Sync entity payload transformations |
| P1 | Environment Effects | Weather visuals, day/night cycle | Implemented | `Assets/MyAssets/Scripts/GameWorld/...`, `Assets/MyAssets/Scripts/GameSound/...` | Trigger from server proto notifications |
| P1 | Terrain Caching | Disk cache & streaming optimizations | Partial | `Assets/MyAssets/Scripts/DataManagement/...` | Align with server chunk residency |
| P2 | Diagnostics & Telemetry | Performance HUD, logging hooks | Partial | `Assets/MyAssets/Scripts/Utility/...`, `docs/` | Define protobuf diagnostics channel |

## 4. Sequential Implementation Roadmap
1. **Terrain Algorithm Enhancements** – Update cave, river, and lake generation heuristics on both the dedicated server (`WorldManager` stages) and shared generator (`MapGeneratorLib`) to produce richer formations and fewer artifacts. *(Completed 2025-11-07: catchment-weighted rivers, basin-stability lakes, and refreshed noise caves.)*  
   - Deliverables: tuned noise parameters, erosion passes, improved flood-fill stability.
2. **Protobuf Contract Audit** – Ensure `proto/*.proto` schemas map cleanly to generated C# under `SharedProtocol` and `Assets/Generated/Protobuf`. Remove dead legacy payloads and validate handler wiring on both ends. *(Enhanced chunk metadata decoder landed 2025-11-07; legacy message sweep still in progress.)*
3. **Client Integration Updates** – Adjust Unity client systems to consume the enhanced protobuf payloads for chunk data, block updates, and entity events. *(Unity now parses enhanced chunk payload metadata for residency + diagnostics.)*
4. **Documentation & Tooling** – Update README and protocol docs to describe the data flow, regeneration commands, and testing recipes.
5. **Validation Pipeline** – Run `dotnet build`, `dotnet test`, and targeted smoke checks; document any Unity-specific validation requirements.

## 5. Validation Checklist
- [ ] Terrain generation stages produce deterministic caves, rivers, and lakes across seeds.
- [ ] All protobuf messages referenced via `using` directives resolve to existing types in SharedProtocol or generated assets.
- [x] Enhanced chunk payload metadata is parsed on the client and cross-checked against legacy chunk responses.
- [ ] Client and server feature matrices remain synchronized after each change (update this document as features evolve).
- [ ] README includes latest build/test instructions when modifications impact developer workflow.

Keep this plan updated whenever feature scope or implementation status changes.
