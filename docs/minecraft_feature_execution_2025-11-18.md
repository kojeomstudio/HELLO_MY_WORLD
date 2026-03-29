# Minecraft Feature Execution Log — 2025-11-18

This log captures the client/server features required for the Minecraft-style roadmap that were addressed today, the code surfaces that changed, and the sequential plan for the next increments. Use it with `docs/minecraft_feature_client_server_sequence.md` when picking up follow-on work.

## Feature Inventory

| ID | Feature | Server Implementation | Client/Tools Implementation | Status |
|----|---------|----------------------|-----------------------------|--------|
| F-13 | Hydrology Runoff Alignment | `WorldManager.ApplyCaveHydrologyErosion` now invokes `ExtendCaveHydrologyRunoff`, carving gradient-aligned cave streams and laying clay/cobblestone beds. | `MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.ApplyCaveHydrologyErosion` mirrors the helper so Unity previews render matching runoff ribbons. | ✅ |
| F-14 | Riparian Seepage Channels | `GenerateRiversInternal.AddRiverSeepageChannels` scans hydrology/flow masks to cut levee seep paths, flooding them when saturation stays high. | `MapGeneratorLib` executes the same pass so wetlands, benches, and seep-fed ponds align between tooling and server chunks. | ✅ |
| F-15 | Lake Hydrology Feedback | `GenerateLakesInternal.ApplyLakeHydrologyFeedback` lowers or floods shoreline voxels based on hydrology + flow accumulation. | `MapGeneratorLib` applies the identical shoreline feedback helper, keeping MapTool captures identical to server output. | ✅ |
| F-16 | Proto Binding Verifier | `ProtocolRegistry.ValidateBindings()` ensures every EnhancedMinecraft binding references a real generated descriptor before handlers register. | `ProtoRuntime.EnsureInitialized()` reuses the diagnostics so Unity tooling surfaces binding issues before connecting. | ✅ |

## Implementation Notes

1. **Caves** — `ExtendCaveHydrologyRunoff` follows hydrology gradients, carving multi-column water ribbons and propagating clay/cobblestone beds. The helper exists in both `GameServer/World/WorldManager.cs` and `MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`.
2. **Rivers** — The new `AddRiverSeepageChannels` pass reads river intensity + flow accumulation to sculpt seepage paths from saturated banks into the river core, reusing identical logic in MapGeneratorLib.
3. **Lakes** — `ApplyLakeHydrologyFeedback` now recalculates rim voxels after overflow carving, dropping shoreline levels or flooding them to create terraced aprons that match in tooling and live chunks.
4. **Protobuf registry** — `ProtocolValidator.ValidateEnhancedContracts()` now calls `ProtocolRegistry.ValidateBindings()` so stale `using` directives or missing generated DTOs fail fast during bootstrap; Unity tooling sees the same diagnostics via `ProtoRuntime.EnsureInitialized()`.

## Sequential Plan

1. **Vegetation Hydrology Pass (F-08)** — With runoff/seepage geometry stabilized, author wetland vegetation prefabs that read the same hydrology masks in Unity and on the server.
2. **Residency Telemetry (F-09)** — Once wetlands land, resume work on the residency HUD so `ServerStatusResponse` exposes the new counters and Unity’s overlay renders them.
3. **Biome/Lighting Expansion (F-10)** — After telemetry, expand the lighting/biome pass using the stabilized terrain artifacts from F-13 through F-15.

Keep the sequence above plus `docs/minecraft_feature_client_server_sequence.md` handy so future sessions continue building features in order without rediscovery.
