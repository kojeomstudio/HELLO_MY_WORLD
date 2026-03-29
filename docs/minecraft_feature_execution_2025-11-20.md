# Minecraft Feature Execution Log — 2025-11-20

This log tracks the client/server feature work completed today, the touched code surfaces, and the next sequenced steps. Pair it with `docs/minecraft_feature_client_server_sequence.md` when continuing the roadmap.

## Feature Inventory

| ID | Feature | Server Implementation | Client/Tools Implementation | Status |
|----|---------|----------------------|-----------------------------|--------|
| F-21 | Edge-Stitched Hydrology Masks | `WorldManager.BlendHydrologySeams` smooths hydrology + flow accumulation at chunk borders before caves, rivers, and lakes run. | `WorldGenAlgorithms.BlendHydrologySeams` mirrors the smoothing so Unity previews avoid seam artefacts. | ✅ New |
| F-22 | Descriptor Coverage Guard | `ProtocolValidator.ValidateRegistryDescriptors` enforces presence/package/registration for every generated EnhancedMinecraft descriptor. | `ProtoDiagnostics` now reports unbound descriptors so stale Unity protobuf generation fails fast. | ✅ New |
| F-17 Fix | Riparian Parity for Rivers | — | `GenerateRiverSystems` now builds the riparian saturation map before carving rivers, matching the server’s behavior. | ✅ Fixed |

## Implementation Notes

1. **Chunk-border hydrology** — New `BlendHydrologySeams` runs in caves, rivers, and lakes on the server and MapGeneratorLib to blend hydrology/flow edges with deterministic noise and neighbor sampling, eliminating visible seams between streamed chunks and Unity previews.
2. **Riparian parity** — River generation in MapGeneratorLib now initializes the riparian saturation map, keeping wet benches and bank stabilization in lockstep with `WorldManager` for client/server visual consistency.
3. **Proto descriptor validation** — `ProtocolValidator.ValidateRegistryDescriptors` and the expanded `ProtoDiagnostics` ensure every EnhancedMinecraft descriptor exists, matches the expected package, and is bound in `ProtocolRegistry`, catching broken `using EnhancedMinecraftProtocol` references during bootstrap.

## Sequential Plan

1. Fold the seam-blended hydrology masks into wetland vegetation spawning (F-08) so reeds/pads honor the stabilized banks and cave ceilings.
2. Keep the descriptor guard in CI to block stale protobuf generations before publishing Unity bundles.
3. Resume residency telemetry (F-09) once the river/lake seams are stable under live chunk streaming.
