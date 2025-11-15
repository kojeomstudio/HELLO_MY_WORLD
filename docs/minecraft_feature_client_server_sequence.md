# Minecraft Client/Server Feature Sequence (2025-11-16)

This matrix enumerates the Minecraft-style capabilities that must exist on both the dedicated server (`GameServer`) and the Unity client toolchain (`Assets` + `MapGeneratorLib`). Each row links the owning code and records whether the implementation is complete, actively being enhanced, or queued. The rightmost column documents how we plan to iterate sequentially so follow-up work can pick up without rediscovery.

| ID | Feature | Server Surface | Client/Tools Surface | Status | Sequence Notes |
|----|---------|----------------|-----------------------|--------|----------------|
| F-01 | Authentication & Sessions | `GameServer/Handlers/LoginHandler.cs`, `SessionManager.cs` issue tokens, heartbeats, and persistence. | `Assets/MyAssets/Scripts/Network/Login` UI + token cache. | ✅ Done | Base requirement before any gameplay; monitor reconnect & token rotation once F-07 lands. |
| F-02 | Chunk Streaming & Residency | `MinecraftChunkHandler.cs`, `WorldManager.GetChunkAsync`, SQLite caching. | `Assets/Scripts/Minecraft/Core/ChunkStreaming`, `EnhancedChunkPayloadBridge`. | ✅ Done | Already shipping; sequence checkpoint for future perf profiling (F-09). |
| F-03 | Block Interaction Broadcast | `Handlers/MinecraftPlayerActionHandler.cs`, `WorldBlockHandler.cs`. | `Assets/MyAssets/Scripts/GameWorld/ModifyWorldManager.cs`, VFX hooks. | ✅ Done | Baseline feature—future iterations add particle/audio polish after F-06. |
| F-04 | Entity Lifecycle & Combat | `Handlers/PlayerAttackHandler.cs`, `Systems/HealthSystem`. | `Assets/MyAssets/Scripts/Player/RemoteAvatarController.cs`, HUD combat feed. | 🟡 In progress | Current sprint focuses on mob AI (F-08) once telemetry confirms hit registration. |
| F-05 | Inventory & Containers | `Handlers/InventoryHandler.cs`, `SharedProtocol/MinecraftContainerMessages.cs`. | `Assets/MyAssets/Scripts/UI/Inventory`, generated protobuf DTOs. | 🟡 In progress | Next sequential step is wiring chest/furnace prefabs on the client (Task 12C). |
| F-06 | World Generation (Terrain, Caves, Rivers, Lakes) | `GameServer/World/WorldManager.cs` (new cave ribbon terraces, meander carving, shoreline benches), `GameServer/World/Generation`. | `MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`, Unity editor previews. | ✅ Enhanced | **This change** upgrades caves (support terraces), rivers (perpendicular benches), and lakes (multi-ring benches). Remains a prerequisite for biome tooling (F-10). |
| F-07 | Networking/Protocol Validation | `SharedProtocol/EnhancedMinecraft/*` (registry metadata, diagnostics, fingerprint guard). | `Assets/Generated/Protobuf`, `Assets/Scripts/Minecraft/Core/EnhancedChunkPayloadBridge.cs`. | ✅ Enhanced | Registry metadata now drives ProtoDiagnostics; sequential follow-up is wiring automated regeneration checks into CI. |
| F-08 | Hydrology-Sensitive Vegetation & Wetlands | `WorldManager.GenerateRiversInternal` (wetlands/swales) & `AddLakeWetlandPockets`. | `Assets/MyAssets/Scripts/GameWorld/Enviroment`, vegetation prefabs. | 🟢 Planned | Activates after F-06 so both codebases share the same hydrology masks. |
| F-09 | Chunk Residency Telemetry | `ServerMetricsService` counters, `Handlers/ServerStatusHandler.cs`. | HUD overlay (`Assets/Scripts/Minecraft/UI/ServerStatusPanel`). | 🟢 Planned | Needs F-02 (streaming) stabilized; telemetry dashboard consumes the same protobuf payloads. |
| F-10 | Future Biome/Lighting Pass | `WorldManager.GenerateBaseTerrainInternal`, lighting propagation helpers. | Unity lighting shaders + `Assets/MyAssets/Scripts/GameWorld/Chunk/TerrainChunk`. | 🟢 Planned | Unlocks once enhanced world-gen (F-06) + proto validation (F-07) are stable. |
| F-11 | Hydrology Feedback Loop | `WorldManager.ApplyCaveHydrologyErosion`, `GenerateRiversInternal.ApplyRiverHydrologyFeedback`, `GenerateLakesInternal.StabilizeLakeCatchments`. | `MapGeneratorLib` mirrors each helper so editor previews match the dedicated server. | ✅ New | **This change** finishes the shared hydrology feedback loop so caves, rivers, and lakes all reference the same saturation data. Sequence unblocks biome moisture tuning. |
| F-12 | Unified Proto Runtime Guard | `ProtoRuntime.EnsureInitialized()` is called from both `GameServer` bootstrap and chunk handlers. | `EnhancedChunkPayloadBridge` now executes the same guard before decoding EnhancedMinecraft payloads. | ✅ New | Lock step validation now fires regardless of whether chunks are loaded in Unity tooling or from a live server connection. |

## Execution Order & Current Focus

1. **Foundation (complete)** – F-01 through F-03 were prioritized first so authentication, chunk streaming, and block deltas worked end-to-end.
2. **Gameplay sync (active)** – F-04/F-05 keep entity combat and inventory persistence in lockstep; these continue in the background.
3. **Terrain alignment (complete)** – F-06 (terrain) + F-07 (proto validation) stabilize the base pipeline for the remaining work.
4. **Hydrology feedback (this change)** – F-11 closes the hydrology loop (caves/rivers/lakes) and F-12 ensures Unity tooling validates protobufs before connecting.
5. **Hydrology polish (next)** – F-08 expands wetlands/vegetation once the new feedback passes settle.
6. **Observability (near future)** – F-09 adds residency metrics using the enhanced protocol payloads.
7. **Visual fidelity (future)** – F-10 covers lighting and biome-specific tweaks once the above are stable.

Use this sequence when picking up new work: verify upstream items first, then continue down the list so client/server features stay synchronized.
