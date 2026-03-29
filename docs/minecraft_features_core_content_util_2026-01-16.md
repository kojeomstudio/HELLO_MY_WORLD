# Minecraft Features (Core/Content/Utility) ? 2026-01-16

## Core (client + server)
- **1. Proto fingerprint + registry guard** ? `SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`, `GameServer/Program.cs`; client bootstrap `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`. Data/IDL: `proto/*.proto`, generated C# under `SharedProtocol/EnhancedMinecraft` and `Assets/Generated/Protobuf`. Goal: assert descriptor fingerprints/registrations before handlers run.
- **2. World map control pipeline** ? server profile/watchers in `GameServer/World/WorldMapControlManager.cs` + `GameServer/World/WorldMapControlProfile.cs`; client preview/controller in `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`. Data: `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`.
- **3. Terrain generation (hydrology, rivers, lakes, caves)** ? server pipeline `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs` + `Generation/Improved*Generator.cs`; client preview `MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`, wiring via `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`. Data: `config/world.json`, `Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.
- **4. Chunk streaming + generation signature** ? server chunk cache/signature in `WorldMapControlManager` + `WorldMapController`; client preview uses signature checks in `WorldMapController`/`EnhancedWorldMapController`. Data: `config/world_map_control_profile.json`, cached preview chunks in memory.

## Content
- **1. Rivers/lakes/wetlands** ? server `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `EnhancedTerrainGenerationPipeline.cs`; client `WorldGenAlgorithms.GenerateRiverSystems/GenerateSurfaceLakes`. Data knobs: `Water.*`, `Lakes.*` sections in world config/profile JSON.
- **2. Caves (stability, moisture, supports)** ? server `EnhancedCaveGenerator.cs`, `ImprovedCaveGenerator.cs`, `EnhancedTerrainGenerationPipeline.CarveCaves`; client cave passes in `WorldGenAlgorithms` (cave stability field + sealing) with config under `Caves.*`.
- **3. Biomes/vegetation/ores** ? server `BiomeGenerationSystem.cs`, `OreDistributionSystem.cs`; client vegetation/biome passes in `WorldGenAlgorithms.GenerateImprovedVegetation` & world data files. Data: `enhanced_game_data.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/GameData.json`.

## Utility
- **1. Config/data sync (JSON-first)** ? `config/world.json`, `config/world_map_control_profile.json`, `server-config.json`, Unity StreamingAssets mirrors; sync helpers in `scripts/`.
- **2. Proto tooling + diagnostics** ? `ProtoDiagnostics`, `ProtocolValidator`, `ProtoRuntime`; regeneration/validation scripts in `scripts/generate_proto.*`, `scripts/verify_protobuf.*`, `proto/*.proto` sources.
- **3. Build/test + self-test hooks** ? `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`, `dotnet run --project GameServer -- --selftest`; Unity validation via editor play/test modes.
- **4. World map telemetry + cache hygiene** ? server cache budget + signature refresh in `WorldMapControlManager`; client logging in `WorldMapControlSystem`/`WorldMapController`. Data persisted as JSON profiles and StreamingAssets copies.

## Session sequencing
1) Lock proto fingerprint/registry guards before handler bring-up.
2) Refresh world map control profile/signature + chunk previews; keep JSON hashes in sync.
3) Apply hydrology envelope + seam stabilization to rivers/lakes/caves (server + MapGeneratorLib) using existing JSON knobs.
4) Verify builds/tests and regenerate protobuf outputs if fingerprints drift.
