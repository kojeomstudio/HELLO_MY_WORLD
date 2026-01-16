# Terrain Generation & Proto Update — 2026-01-17

## Summary
- Hardened hydrology seam handling for caves/rivers/lakes on server and Unity preview to keep world-map control aligned.
- Added EnhancedMinecraft protocol registry guard to block duplicate descriptor bindings when regenerating protobuf DTOs.
- Captured refreshed core/content/util feature map (Markdown + JSON) for data-driven planning.

## Changes
- Server `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`
  - New hydrology+flow blending before mask generation to stabilize gradients at chunk edges.
  - River and lake masks now factor hydrology/flow envelopes to reduce jagged shorelines and riparian leakage.
  - Cave mask generation is hydrology-aware with seam guards and legacy fallback for missing masks.
- Client `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`
  - Cave mask uses hydrology envelope + seam guard to mirror server sealing.
  - Lake mask penalizes flow gradients to avoid over-saturation near river outflows.
- Proto `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`
  - Validates descriptor bindings are unique, catching stale/duplicated `using` references after protoc regeneration.
- Planning/Data
  - Feature categorization refreshed: `docs/minecraft_feature_core_content_util_2026-01-17.md` + JSON source `docs/minecraft_feature_core_content_util_2026-01-17.json`.

## Config & Data
- Worldgen, networking, and map control configs remain JSON-driven (`config/world_generation.json`, `Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`).
- No new keys added; algorithms consume existing hydrology/cave tuning fields.

## Tests
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj` (warn: nullable + NU1603/protobuf-net version float)
- [x] `dotnet build GameServer/GameServer.csproj` (warn: nullable + NU1603, existing async/await omissions)
- [x] `dotnet run --project GameServer -- --selftest` (completes; emits existing proto handler coverage warnings and test-client flow rejects movement/block-change with unexpected response types)
