# Terrain Generation & Proto Update (2026-01-18)

## Changes
- Hydrology edge envelope now blends seam memory and normalization to reduce chunk seams in both the improved coordinator and base pipeline (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`). MapGeneratorLib mirrors the edge-seal stability so Unity previews stay aligned (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`).
- Rivers and lakes respect flow/hydrology gradients when expanding channels: river pressure is damped by edge stability, and lake outflow channels down-weight steep or noisy gradients (`ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`).
- Caves gain a riparian ceiling guard that biases thresholds against wet ceilings near hydrology/flow/river pressure to keep subterranean water aligned with surface features (`ImprovedCaveGenerator.cs`).
- World map control pipeline version bumped to `2026-01-18-hydrology-continuity+proto`; generation signatures now include the proto descriptor fingerprint and edge stability iterations, and both server/client assert fingerprints when loading profiles (`WorldMapControlManager.cs`, `WorldMapController.cs`).

## Data & Config
- World/terrain knobs remain JSON-driven (`config/world.json`, `config/world_map_control_profile.json`) and mirrored on the client (`Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`).
- Updated feature map + JSON source: `docs/minecraft_features_client_server_core_content_util_2026-01-18.md`, `config/minecraft_feature_client_server_core_content_util_2026-01-18.json`.

## Verification Plan
- Build: `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj`.
- Runtime check: `dotnet run --project GameServer -- --selftest` to validate terrain pipeline and proto bindings.
- Proto regeneration reminder: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` if descriptor fingerprints drift.

## Verification Results
- `dotnet build SharedProtocol/SharedProtocol.csproj` — succeeds with existing nullable/async warnings.
- `dotnet build GameServer/GameServer.csproj` — succeeds with existing nullable/async warnings.
- `dotnet run --project GameServer -- --selftest` — completes; proto diagnostics still warn about optional EnhancedMinecraft packets and unbound descriptors/handlers (expected until registry is expanded); map-control profile regenerated to `config/world_map_control_profile.json` with hash `9d5d2eeafc185ec80e003678b7b7d5e48cb74fe1c07925fb3b40362c02dafde3`.
