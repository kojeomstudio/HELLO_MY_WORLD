# Minecraft Feature Categorization (Session 38 – 2026-02-02)

## Core
- Hydrology v10 river/lake/cave coherence across server + Unity (`GameServer/World/Generation/Improved*Generator.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`, `config/world.json`, `Assets/StreamingAssets/world-config.json`).
- World map control profile/signature parity with hydrology bump (`GameServer/World/WorldMapControlManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `config/world_map_control_profile.json`, `GameCommon/World/WorldMapSignature.cs`).

## Content
- Terrain streaming + world map previews consuming updated control profile and hydrology-aware chunk caches (`GameServer/World/WorldManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `GameCommon.dll`).
- Cave/river/lake masks honoring erosion + water-table stability for surface and underground content placement (`ImprovedTerrainGenerationPipeline`, `ImprovedCaveGenerator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`).

## Utility
- Proto registry + diagnostics tightened with DummyProtocolClient reporting (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Testing/DummyProtocolClient.cs`, `config/protocol_dummy_client.json`, `config/proto_reference_report.json`).
- Shared DLL distribution pipeline (GameCommon.dll, SharedProtocol.dll, MapGeneratorLib.dll) copied to Unity Plugins to keep enums/contracts/worldgen parity (`Assets/Plugins/`).
- Data-driven manifests tracking core/content/utility scope (`config/minecraft_feature_core_content_util_2026-02-02-session-38.json`, `plans/2026-02-02-session-38-plan.md`).
