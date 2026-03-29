# Terrain & Map Control Update (2026-01-20)

## Summary
- Added seam-safe hydrology tweaks: cave masks now prune isolated voxels and respect stability smoothing; rivers erode banks directly in the height map; lake rims bleed less along chunk seams.
- Bumped map-control profile version to `2` and introduced a pipeline signature for server/client parity so new profiles regenerate automatically.
- Cleaned enhanced protobuf handler imports (Google.Protobuf-first) to reduce protobuf-net confusion.

## Implementation notes
- **Server**: `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs` now stabilizes cave masks, erodes river banks (using `RiverBankErosionWeight`), and seals lake rims with hydrology/flow gradients. Height maps are adjusted before water fill.  
- **Client preview**: `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs` mirrors cave pruning and lake rim sealing to keep Unity previews aligned with the server.
- **Map control**: Pipeline signature updated (`GameServer/World/WorldMapController.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`) and configs bumped (`config/world.json`, `Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`). New profiles will regenerate due to the version change.
- **Proto hygiene**: Removed unused `ProtoBuf` import from `SharedProtocol/EnhancedMinecraft/UnifiedMessageHandler.cs`; validation report refreshed (`docs/protobuf_protocol_validation_report.md`).

## Data-driven knobs (JSON)
- `RiverBankErosionWeight` controls new bank carving strength.
- `LakeRimErosionWeight`, `HydrologyEdgeStabilityWeight`, and `LakeBasinSmoothIterations` influence lake rim sealing and smoothing.
- `Caves.StabilitySmoothIterations/StabilitySmoothBlend` feed the new cave pruning pass.
- `MapControlProfileVersion` is now `2` (server/client configs and StreamingAssets) to force regeneration with the updated pipeline signature.

## Next steps
- Rebuild world-map control profiles and distribute updated JSON to clients.
- Validate handler coverage during server startup (`ProtocolValidator.ValidateHandlerBindings`) and watch `ProtoDiagnostics` logs for any missing bindings.
- Run end-to-end chunk streaming smoke test once Unity client picks up the refreshed profiles.
