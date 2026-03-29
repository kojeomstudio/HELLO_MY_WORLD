# Minecraft World-Generation & Protocol Alignment Checklist (2025-11-06)

This session extends the cross-team checklist so both the Unity tooling (`MapGeneratorLib`) and the .NET server (`WorldManager`) implement the same Minecraft-style terrain primitives while sharing a verifiable protobuf contract. Client and server responsibilities remain paired so future work can execute sequentially without rediscovery.

| Step | Feature | Server Responsibilities | Client/Tooling Responsibilities | Status |
|------|---------|-------------------------|----------------------------------|--------|
| 1 | Hydrology-Driven River Systems | Introduce slope/valley hydrology masks, blend noise flow with terrain gradients, erode banks with adjusted intensity | Mirror hydrology masks in `WorldGenAlgorithms`, bias river carving & previews | Complete (2025-11-06) |
| 2 | Inland Lake Formation & Outflow | Gate lakes by hydrology, bias depth/size, auto-link to nearest river channels | Apply same gating to tooling so Unity scenes preview river-fed lakes | Complete (2025-11-06) |
| 3 | Multi-Frequency Noise Caves | Domain-warp cave noise, add ridged detail + aquifer bias, keep lava/water aquifers consistent across chunks | Upgrade `MapGeneratorLib` noise caves so offline previews match server output | Complete (2025-11-06) |
| 4 | Protobuf Contract Validation | Validate `EnhancedMinecraftGame` descriptors & registry entries at launch, guard chunk payload fields | SharedProtocol links generated assets; Unity regenerations reuse the same source files | Complete (2025-11-06) |
| 5 | Catchment-Aware Rivers & Lakes | Use flow-accumulation fields to scale channel depth/width, basin relief to gate lakes, and enforce smarter lake→river channels | Mirror accumulation/relief logic in `MapGeneratorLib` plus expose enhanced chunk metadata to Unity tooling | Complete (2025-11-07) |

## Sequential Execution Plan
1. ✅ Build reusable surface-height caches and hydrology masks so both the server and tooling can weight rivers/lakes by slope, elevation, and humidity.
2. ✅ Apply the masks to river generation (channel thresholds, bank erosion, flow vectors) and lake formation (spawn weights, channel linking) across `WorldManager` and `WorldGenAlgorithms`.
3. ✅ Refresh the noise-cave pass with domain-warped samples, layered ridged detail, and aquifer-aware thresholds to keep water/lava pockets deterministic across chunk seams.
4. ✅ Add `ProtocolValidator.ValidateEnhancedContracts()` to the server bootstrap so regenerated protobuf assets are verified before accepting player traffic.

## Session Outcomes
- River carving now uses blended noise + slope vectors plus downstream catchment pressure, reducing jagged seams and widening channels proportionally on both implementations.
- Lakes only form in low-lying, stable basins; depth, radius, and lake-to-river channels respect hydrology and relief fields, so Unity previews match the .NET generator.
- Noise caves gained higher-frequency ridges, vertical strata fades, and aquifer bias so magma/water pockets appear predictably without duplicated tuning per codebase.
- Enhanced chunk payload metadata is validated on the server and parsed on the client, ensuring the protobuf contract stays in sync with shared chunk data.

## Notes
- `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs` is invoked during server startup; rerun `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` whenever proto files change so the validator can enforce the required fields.
- Unity tooling should refresh `MapGeneratorLib` assemblies after pulling these changes to keep chunk previews identical to the authoritative server output.
