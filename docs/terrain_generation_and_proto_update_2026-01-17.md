# Terrain Generation & Proto Update (2026-01-17)

- Added a hydrology edge envelope pass to reduce chunk-seam artifacts for caves/rivers/lakes across server (`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `ImprovedTerrainCoordinator.cs`), Unity preview (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`), and map tooling (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`).
- World map control pipeline bumped to `2026-01-17-hydrology-edge-envelope+proto`; generation signatures now include hydrology continuity/edge flux weights and clear cached chunks when the signature changes to avoid stale previews.
- Protocol validation now logs optional EnhancedMinecraft packet coverage so missing/stale optional bindings are surfaced without blocking required traffic (`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`).
- Feature classification for this session lives in `docs/minecraft_feature_core_content_util_2026-01-17-session-03.md` with JSON source `config/minecraft_feature_client_server_core_content_util_2026-01-17-session-03.json`.
- Data remains JSON-driven (`config/world.json`, `config/world_map_control_profile.json`, StreamingAssets) with no new keys required; hydrology weights continue to flow from config to profile to generator.
- Test plan: `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`, `dotnet run --project GameServer -- --selftest` to validate protobuf bindings and terrain generation.
