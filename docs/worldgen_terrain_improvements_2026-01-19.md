# Worldgen Improvements (2026-01-19)

## Algorithm Updates
- Rivers: added hydrology-aware continuity smoothing near chunk seams (uses `HydrologyEdgeBlendRadius`, `HydrologyEdgeNormalizationIterations/Blend`, `HydrologyEdgeFluxBlend`) to damp jagged intensity and align flow across boundaries.
- Lakes: candidate scoring now factors nearby river pressure using existing `LakeRiverProximitySuppression` and `LakeInflowBlendWeight` to bias basins toward stable hydrology pockets instead of river banks.
- Caves: stability field includes seam hydrology/flow continuity and riparian saturation to avoid wet cave ceilings and river-adjacent collapses; moisture retention now attenuates when seam deltas grow.
- Client cave mask: mirrors seam/riparian penalties so previews match server map-control output.

## Data-Driven Controls
- Tunable via JSON world map control profile (`GameServer/World/WorldMapControlProfile.cs` → `world_map_control_profile.json`, consumed by Unity `WorldMapControlProfile`).
- Key knobs: `HydrologyEdgeBlendRadius`, `HydrologyEdgeNormalizationIterations/Blend`, `HydrologyEdgeFluxBlend`, `LakeRiverProximitySuppression`, `LakeInflowBlendWeight`, `Cave*` hydrology weights.
- Regenerate/refresh profile after config tweaks: `dotnet run --project GameServer -- --server` (auto-saves profile) or run the map-control manager.

## Validation
- Build commands: `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj`.
- Protobuf: ensure generated DTOs are up to date via `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` before reopening Unity to keep handlers aligned.
