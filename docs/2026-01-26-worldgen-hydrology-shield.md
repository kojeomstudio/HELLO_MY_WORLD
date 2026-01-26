# 2026-01-26 Hydrology Shield & Protocol Harness

## Worldgen Updates
- Added subterranean hydrology shield + erosion-aware flow damping across server (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`), MapGeneratorLib (`WorldGenAlgorithms.cs`), and Unity preview (`WorldMapController.cs`). The pass blends slope, curvature, and erosion to keep caves from flooding while rivers/lakes stay stitched at seams.
- Introduced river/lake hydrology feedback loops that lock edge flow/tangents and rebalance masks before cave carving. Applied in server masks, Unity previews, and MapGeneratorLib river/lake previews.
- Pipeline version bumped to `2026-01-26-hydrology-shield` and baked into generation signatures for both server and client.

## World Map Control Architecture
- Map-control profiles now carry a `hydrologySignature` (SharedFeatureCatalog.HydrologySignature) and will regenerate when signatures drift (`WorldMapControlProfile.cs`, `WorldMapControlManager.cs`).
- Updated map-control JSONs (`config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`) to include the new signature and force a fresh hash.
- Unity world map controllers warn on signature mismatch and include the signature in generation signatures.

## Shared Contracts & Dummy Client
- Added `GameCommon/World/SharedFeatureCatalog.cs` and shipped `Assets/Plugins/GameCommon.dll` so Unity and server share feature IDs/signatures.
- Introduced `GameServer/Testing/DummyProtocolClient.cs` to serialize/deserialize EnhancedMinecraft packets (TimeUpdate) and emit a framed payload for on-demand protocol tests.
- Feature catalog for this session: `config/minecraft_feature_client_server_core_content_util_2026-01-26.json`.

## Data & Config Touchpoints
- Map-control JSONs refreshed with `hydrologySignature` and cleared hashes to force regeneration.
- Generation signatures now include hydrology signature/hash for parity; profile hash includes the new field.

## Test Plan
- Build shared + server:  
  - `dotnet build GameCommon/GameCommon.csproj -c Release`  
  - `dotnet build SharedProtocol/SharedProtocol.csproj`  
  - `dotnet build GameServer/GameServer.csproj`
- Optional protocol sanity: run `DummyProtocolClient.BuildTimeUpdateRoundTrip()` or `SendAsync()` (requires server endpoint) to verify EnhancedMinecraft serialization.
- Note: `MapGeneratorLib/MapGeneratorLib.csproj` targets .NET Framework 4.5 and cannot build here without that targeting pack.
