# Minecraft Feature Execution Log — 2025-11-16

This log captures the Minecraft-style client/server features addressed in this session and how they map to the shared sequence in `docs/minecraft_feature_client_server_sequence.md`. Use it as a handoff so follow-up work can continue in order without rediscovery.

## Feature Inventory

| ID | Feature | Server Implementation | Client/Tools Implementation | Status |
|----|---------|----------------------|-----------------------------|--------|
| F-17 | Riparian-weighted rivers & shelves | `WorldManager.GenerateRiversInternal` now builds `BuildRiparianSaturationMap`, reweights channel pressure, and applies `ApplyRiparianBankStabilization` to flood benches in wet columns. | `MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.GenerateRiverSystems` consumes the same riparian mask and stabilization pass so Unity previews track server river breadth and shelves. | ✅ |
| F-18 | Moist cave ceiling stabilization | `WorldManager.StabilizeMoistCaveCeilings` seals thin hydrology-heavy roofs after the erosion pass to prevent leaks/collapses. | `MapGeneratorLib...StabilizeMoistCaveCeilings` runs inside `GenerateSphereCaves` to mirror the sealing in tooling. | ✅ |
| F-19 | Riparian-aware surface lakes | `GenerateLakesInternal` sizes basins, depth, and water level with the riparian mask + flow accumulation. | `GenerateSurfaceLakes` mirrors the riparian weighting for spawn chance, basin size, and rim smoothing. | ✅ |
| F-20 | Proto enum coverage guard | `ProtoDiagnostics` now reports `MinecraftMessageType` values missing `ProtocolRegistry` bindings; enforced via `ProtoRuntime.EnsureInitialized()`. | Unity hits the same guard during `ProtoRuntime` initialization, surfacing stale or missing generated DTOs before connecting. | ✅ |

## Implementation Notes

1. Rivers: riparian saturation now feeds channel pressure and a new stabilization pass to widen or flood benches where wetlands should exist, keeping flow accumulation, hydrology, and bank geometry aligned across server and MapGeneratorLib.
2. Caves: moisture-heavy near-surface cavities gain deterministic sealing so hydrology-driven erosion no longer pokes through thin roofs; this keeps biome and lighting passes stable in both codebases.
3. Lakes: surface lake spawn weight, basin dimensions, and water table height now include riparian and flow data, reducing mismatches between river plains and inland basins in Unity captures versus live chunks.
4. Protobuf: diagnostics now fail fast when a `MinecraftMessageType` lacks a matching generated descriptor, tightening the `using`/registry alignment for Google.Protobuf DTOs on both client and server.

## Sequential Plan

1. Finish F-08 by attaching vegetation/wetland decorators to the shared riparian mask so reeds/pads spawn where the new benches and lake rims form.
2. Resume F-09 residency telemetry once the riparian-weighted geometry is stable, ensuring packet metrics reflect the updated chunk residency profile.
3. Keep the proto coverage guard in CI by rerunning `protoc` before builds and extending handler registration tests to assert registry completeness for new message types.
