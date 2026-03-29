# Minecraft Core/Content/Utility Roster (2026-01-10)

- Data source: `config/minecraft_feature_client_server_core_content_util_2026-01-10.json` (keeps client/server feature parity and implementation order).

## Core
- World map control parity (profile hashes + hydrology knobs) — `GameServer/World/WorldMapControlManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs`
- Terrain generation (caves/rivers/lakes) — `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`
- Protobuf registry/packet handling — `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Network/EnhancedProtocolHandler.cs`, `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`
- Chunk streaming/networking — `GameServer/Handlers/MinecraftChunkHandler.cs`, `SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldArea.cs`

## Content
- Biomes & surface layers — `GameServer/World/Generation/BiomeGenerationSystem.cs`, `config/biomes.json`
- Structures/underground content — `GameServer/World/Generation/ImprovedWorldGeneration.cs`, `MapGeneratorLib/*WorldGenAlgorithms.cs`, `Assets/MyAssets/Scripts/GameWorld/Chunk/StructurePlacer.cs`
- Blocks/items/recipes — `config/blocks.json`, `config/items.json`, `config/recipes.json`, matching Unity data files

## Utility
- Config + hot reload — `GameServer/Configuration/DataDrivenConfigManager.cs`, `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs`, `config/world.json`, `config/world_map_control_profile.json`
- Telemetry/diagnostics — `GameServer/Systems/Telemetry/TelemetryReporter.cs`, `SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`, `Assets/Scripts/Minecraft/Core/MinecraftDiagnostics.cs`
- Tooling/data pipelines — `scripts/generate_proto.ps1`, `SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs`, generated DTOs in `Assets/Generated/Protobuf`

Implementation order (see JSON): world-map-control parity → terrain hydrology/caves/rivers/lakes → protobuf registry → chunk streaming → biomes/surface → structures/underground → config/hotreload → telemetry → tooling.
