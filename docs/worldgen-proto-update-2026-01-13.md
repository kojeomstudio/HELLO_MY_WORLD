# 2026-01-13 Worldgen & Proto Update

## Summary
- Added a hydrology/flow edge-cohesion pass to the shared worldgen pipeline (`ImprovedTerrainCoordinator`, Unity `EnhancedTerrainGenerator`) so cave/river/lake masks stay stitched at chunk seams using flow memory + edge variance clamps.
- Tightened MapGeneratorLib seam blending by applying edge stability and divergence clamping during `BlendHydrologySeams`, aligning in-game generation with the server/Unity preview passes.
- Recorded a fresh client/server core-content-util feature list (`config/minecraft_feature_client_server_core_content_util_2026-01-13.json`) to sequence worldgen + protocol tasks by scope.
- Kept protobuf registry/descriptor validation active via `ProtocolStandardization.ValidateProtocolImplementation` and `ProtoDiagnostics.LogSummary` at server startup.

## Files Touched
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` — new hydrology edge cohesion step feeding river/lake/cave masks.
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs` — mirrored edge cohesion for Unity previews to maintain parity with server generation.
- `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs` — seam blending now respects edge stability and divergence clamps.
- `config/minecraft_feature_client_server_core_content_util_2026-01-13.json` — data-driven feature inventory (core/content/util, server/client scopes).

## Notes
- No new config keys introduced; the new passes reuse existing hydrology weights (`HydrologyEdgeBlendRadius`, `HydrologyEdgeVarianceClamp`, `HydrologySeamRelaxBlend`, `HydrologyFlowDivergenceClamp`).
- World map control profiles remain JSON-driven; regeneration will pick up the smoother edge handling without schema changes.

## Validation
- Build: `dotnet build SharedProtocol/SharedProtocol.csproj`
- Build: `dotnet build GameServer/GameServer.csproj`
- Proto: startup still calls `ProtocolStandardization.ValidateProtocolImplementation()` and `ProtoDiagnostics.LogSummary()` to guard registry/descriptor drift.
