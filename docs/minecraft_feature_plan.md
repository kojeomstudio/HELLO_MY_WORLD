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
| P1 | Procedural Structures | Trees, lakes, caves, rivers, dungeons | Hydrology-weighted rivers/lakes plus new karst sinkholes, tributary stitching, and clay-banked lake terraces (2025-11-10 in progress) | `MapGeneratorLib/...`, `WorldManager` stages | Surface tunables via config & MapGenerator UI; capture erosion metrics (current task) |
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
1. **Terrain Algorithm Enhancements** ? Update cave, river, and lake generation heuristics on both the dedicated server (WorldManager stages) and shared generator (MapGeneratorLib) to produce richer formations and fewer artifacts.
   - 2025-11-09: hydrology-driven cave pools, sedimented riverbeds, terraced lakes, plus the prior catchment-weighted rivers and basin-stability lakes.
   - **2025-11-10 (in progress):** add karst sinkholes with aquifer vents, stitch minor tributaries into the main channel flow, and lay down clay/sand lake terraces with matching Unity preview data.
   - 2025-11-13: stability-weighted cave shelf terraces, river floodplain swales, and lake wetland spillways now run in both WorldManager and MapGeneratorLib so hydrology parity is maintained end-to-end.
   - Deliverables: tuned noise parameters, erosion passes, improved flood-fill stability, and parity between tooling + dedicated server.
2. **Protobuf Contract Audit** ??Ensure `proto/*.proto` schemas map cleanly to generated C# under `SharedProtocol` and `Assets/Generated/Protobuf`. Remove dead legacy payloads and validate handler wiring on both ends. *(ChunkLoadRequest/Response descriptors are now validated at startup; the new `ProtoFingerprint` guard blocks mismatched generated assets ahead of runtime.)*
3. **Client Integration Updates** ??Adjust Unity client systems to consume the enhanced protobuf payloads for chunk data, block updates, and entity events. *(Unity now parses enhanced chunk payload metadata for residency + diagnostics.)*
4. **Documentation & Tooling** ??Update README and protocol docs to describe the data flow, regeneration commands, and testing recipes.
5. **Validation Pipeline** ??Run `dotnet build`, `dotnet test`, and targeted smoke checks; document any Unity-specific validation requirements.

## 5. Validation Checklist
- [ ] Terrain generation stages produce deterministic caves, rivers, and lakes across seeds.
- [ ] All protobuf messages referenced via `using` directives resolve to existing types in SharedProtocol or generated assets.
- [x] Enhanced chunk request/response descriptors are validated at startup (missing fields fail fast).
- [x] Enhanced chunk payload metadata is parsed on the client and cross-checked against legacy chunk responses.
- [x] SharedProtocol and Unity builds share a verified `ProtoFingerprint` so stale `Assets/Generated/Protobuf` artifacts are rejected at startup.
- [ ] Client and server feature matrices remain synchronized after each change (update this document as features evolve).
- [ ] README includes latest build/test instructions when modifications impact developer workflow.

Keep this plan updated whenever feature scope or implementation status changes.

## 6. November 2025 Hydrology Execution Order
| Order | Feature | Client Touchpoints | Server/Tooling Touchpoints | Status & Notes |
|---|---|---|---|---|
| 1 | Karst sinkholes & aquifer vents | Unity chunk renderer consumes `EnhancedChunkMetadata.GenerationTimestamp` to flag moisture overlays and surface decals. | `WorldManager.GenerateCavesInternal`, `MapGeneratorLib.WorldGenAlgorithms.GenerateSphereCaves` gain hydrology-aware sinkholes and drip-fed pools. | Complete (2025-11-10) |
| 2 | Tributary weaving & catchment stitching | Chunk streaming controller tracks hydrology tags from enhanced payloads to request adjacent tributary chunks earlier. | `WorldManager.GenerateRiversInternal` and `MapGeneratorLib.WorldGenAlgorithms.GenerateRiverSystems` add tributary channel carving + slope blending. | Complete (2025-11-10) |
| 3 | Lake sediment terraces & proto contract audit | Unity mesh baker references enhanced payload metadata to apply shoreline materials; README/docs updated with hydration commands. | `WorldManager.GenerateLakesInternal`, `MapGeneratorLib` lake passes, and `SharedProtocol.EnhancedMinecraft.ProtocolValidator` capture clay bank rules + regeneration steps. | Complete (2025-11-11) |
| 4 | Shelfed caves, swales, and wetland spillways | Unity preview tooling renders the new shelf/swale/spillway metadata so editors match runtime terrain. | `WorldManager` + `MapGeneratorLib` share shelf/swale/spillway passes and `ProtoFingerprint.AssertDescriptorFingerprint()` blocks stale protobufs. | Complete (2025-11-13) |

These steps must be executed sequentially so client rendering logic always trails server/tooling changes by at most one feature batch, preventing protocol or terrain drift.

