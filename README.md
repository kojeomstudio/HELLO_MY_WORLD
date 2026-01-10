# HELLO_MY_WORLD

This project is an open-source voxel game that aims to mimic the core mechanics of Minecraft. All source code and assets in this repository are available under the MIT license, though external libraries and resources may carry their own licenses.

![hello_my_world](https://user-images.githubusercontent.com/9248400/75618900-dc37ab00-5bb7-11ea-9ec0-9759c0b6429f.png)
![hmw_git_main_img](https://user-images.githubusercontent.com/9248400/102211930-b47fbc80-3f17-11eb-8d7a-53281bb826ce.png)

## Project Overview
- **Development period:** 2016/01 ~ 2021/12 (hold) - Continued development from 2026
- **Engine:** Unity 6000.0.23f1
- **Language:** C# with Unity (.NET Framework 4.5) and standalone server components on .NET 6.0
- **Libraries:** NGUI 3.x, Sqlite3, JsonObject, Newtonsoft.Json, iTween, FMOD, UniRx, FreeNet, ECM, Google.Protobuf, etc.
- **Platforms:** Windows PC (Android planned)
- **License:** MIT

## Repository Structure
- `Assets/` – Unity game content and scripts. `MyAssets/Scripts` includes modules for AI, GameWorld, Network, Player, UI, pathfinding and more.
- `SharedProtocol/` – Shared networking contracts/utilities (legacy `protobuf-net` + Google.Protobuf `EnhancedMinecraftProtocol`).
- `GameServer/` – TCP server using `SharedProtocol`, `SessionManager`, and SQLite persistence.
- `KojeomNetWorkSpace/` – legacy `KojeomNet` network library and test clients.
- `MapGeneratorLib/` – standalone library for procedural map generation.
- `CustomToolSet/` – editor utilities such as `ActorGeneratorTool` and `MapTool`.
- `Documents/` – design documents and guides (`Project_PDD.md`).
- `Packages/` – Unity package manifest listing engine dependencies.
- `proto/` – Protobuf IDL files compiled into C# under `Assets/Generated/Protobuf`.
- `docs/` – networking overview, protocol notes, and Minecraft feature workplan (`docs/minecraft_features_comprehensive_list.md`).
- `scripts/` – protobuf generation/verification + shared config sync helpers.
- `config/`, `ProjectSettings/`, `UserSettings/` – engine configuration files.
- `Recordings/` – gameplay capture sessions.

## Recent Updates
- **2026-01-10: Hydrology Seam Normalization + Proto Guard Refresh**
  - Added edge-aware hydrology/flow co-normalization for world-map control previews and chunk generation (`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`).
  - Tuned improved cave/river/lake generators with seam memory and edge normalization (`ImprovedCaveGenerator`, `ImprovedRiverGenerator`, `ImprovedLakeGenerator`).
  - Extended protobuf validation to cover chunk unload descriptors (fails fast on stale generated DTOs).
  - Published updated feature roster (`config/minecraft_feature_client_server_core_content_util_2026-01-10-worldgen-edge.json`) with matching docs under `docs/`.
  - Config/data remain JSON-driven for server + Unity (world/config/map-control profiles kept in StreamingAssets and `config/`).
- **2026-01-10: Comprehensive System Review and Documentation Update**
  - Created comprehensive work plan (`plans/2026-01-10-comprehensive-implementation-plan.md`)
  - Created comprehensive feature categorization (`config/minecraft_feature_client_server_core_content_util_2026-01-10-comprehensive.json`)
  - Reviewed and documented terrain generation algorithms (`docs/terrain-generation-algorithms-review.md`)
  - Reviewed and documented protobuf protocol implementation (`docs/protobuf-protocol-review.md`)
  - Created comprehensive implementation summary (`docs/comprehensive-implementation-summary.md`)
  - Successfully compiled SharedProtocol project (0 errors, 10 warnings)
  - Successfully compiled GameServer project (0 errors, 34 warnings)
  - Verified all using statements reference existing files/classes
  - Confirmed all configuration files are properly structured in JSON format
  - Verified data-driven approach across all game systems
  - Terrain generation algorithms confirmed production-ready:
    - ImprovedCaveGenerator: Hydrology-aware carving, flow memory, edge normalization, support pillars
    - ImprovedRiverGenerator: Hydrology-driven generation, seam feathering, confluence boosting
    - ImprovedLakeGenerator: Basin formation, flow seepage, outflow channels
  - Protobuf protocol validated with comprehensive message coverage
  - All critical features implemented and ready for production use
- **2026-01-09: Comprehensive System Verification and Feature Implementation Plan**
  - Created comprehensive feature implementation plan with 27 features categorized into Core (10), Content (7), and Util (10) categories
  - Verified all using statements reference existing files/classes - all namespaces and classes properly defined
  - Successfully compiled SharedProtocol project (0 errors, 10 warnings - all non-critical nullable reference warnings)
  - Successfully compiled GameServer project (0 errors, 34 warnings - all non-critical nullable reference warnings)
  - Verified protobuf protocol integration - Google.Protobuf properly integrated with EnhancedMinecraftProtocol namespace
  - Confirmed all configuration files are properly structured in JSON format
  - Verified data-driven approach across all game systems - all game data uses JSON configuration files
  - Terrain generation algorithms confirmed production-ready with hydrology-aware features
  - Protobuf protocol validated with dual support (protobuf-net legacy + Google.Protobuf enhanced)

## Development Environment
- Unity Engine **6000.0.23f1**
- C# / .NET Framework 4.5 (Unity) & .NET 6.0 (server)
- IDE: Visual Studio, Rider, or VS Code

## Unity Package Dependencies
Key packages from `Packages/manifest.json` include:

- `com.unity.2d.sprite` 1.0.0
- `com.unity.2d.tilemap` 1.0.0
- `com.unity.ai.navigation` 2.0.8
- `com.unity.collab-proxy` 2.5.2
- `com.unity.ext.nunit` 2.0.5
- `com.unity.ide.visualstudio` 2.0.22
- `com.unity.multiplayer.center` 1.0.0
- `com.unity.postprocessing` 3.4.0
- `com.unity.recorder` 5.1.1
- `com.unity.render-pipelines.core` 17.0.3
- `com.unity.shadergraph` 17.0.3
- `com.unity.test-framework` 1.4.5
- `com.unity.timeline` 1.8.7
- `com.unity.ugui` 2.0.0
- `com.unity.xr.legacyinputhelpers` 2.1.11

See `Packages/manifest.json` for full dependency list.

## Building and Testing
1. Clone this repository and open root folder with **Unity 6000.0.23f1**.
2. Build standalone .NET components:
   ```bash
   dotnet build SharedProtocol/SharedProtocol.csproj
   dotnet build GameServer/GameServer.csproj
   dotnet build MapGeneratorLib/MapGeneratorLib.sln
   ```
3. After installing .NET SDK, run available tests with `dotnet test`.
4. Custom tools such as map and actor generators can be opened through their solution files in `CustomToolSet/`.

## Additional Resources
There is a helpful tutorial used at start of project:<br>
http://studentgamedev.blogspot.kr/2013/08/unity-voxel-tutorial-part-1-generating.html

## Networking Protocol (Client ↔ Server)
- The client and server communicate using Google.Protobuf protocol with enhanced Minecraft-specific messages.
- Protocol files are located in `proto/` directory
- Generated C# classes are in `Assets/Generated/Protobuf/` and `SharedProtocol/EnhancedMinecraft/`
- Key protocol messages:
  - `game_world.proto`: WorldBlockChangeRequest/Response/Broadcast, ChunkDataRequest/Response
  - `game_core.proto`: InventoryItem, PlayerInfo
  - `game_auth.proto`: Authentication messages
  - `game_move.proto`: Movement synchronization
  - `game_chat.proto`: Chat system
  - `game_diag.proto`: Diagnostics
- See `docs/networking-protocol.md` for details, message type IDs, and client integration notes.

## Time & Weather Systems
- The server boots `WorldTimeSystem` to push time updates on login and every tick so late joiners stay in sync.
- A companion `WeatherSystem` schedules configurable weather broadcasts driven by `WorldSettings` keys.
- Tweak those values in `server-config.json` before launch to control cycle speed, duration, and precipitation mix.
- Unity clients should bind these packets to skybox lighting, precipitation FX, and ambient audio.

## Remote Player Entity Sync
- `EntitySyncService` broadcasts player spawn, update, and despawn messages so remote avatars remain authoritative and discoverable by late joiners.
- Unity ships a `RemoteEntityManager` MonoBehaviour that subscribes to entity updates, spawns remote player prefabs, and smooths transforms with configurable lerp speeds.
- Attach `RemoteEntityManager` to your network scene root and assign a prefab to override the default capsule.

## Server Rooms
- The server supports a room-based architecture to scope chat and block broadcasts.
- See `docs/server-rooms-architecture.md` for lifecycle and integration details.

## World Generation
- Server procedurally generates terrain, ores, caves, rivers, lakes, dungeons, and vegetation.
- See `docs/world-generation.md` for pipeline and extension notes.
- Configure day/night cycle via `WorldSettings` in `server-config.json`.
- **Terrain Generation Improvements:**
  - ImprovedCaveGenerator: Hydrology-aware cave generation with river suppression and support pillars
  - ImprovedRiverGenerator: Flow-aware river generation with seam stitching and confluence boosts
  - ImprovedLakeGenerator: Wetland-aware lake generation with outflow channels and shoreline shelves
  - ImprovedTerrainCoordinator: Unified hydrology/flow mask generation with edge stabilization

## Core Gameplay Systems
- **PlayerController**: Comprehensive player movement, block interaction, and inventory management
- **InventoryManager**: Full inventory system with hotbar, main inventory, item stacking, and save/load functionality
- **CraftingManager**: Multi-type crafting system (hand, workbench, furnace) with recipe management
- **HealthHungerSystem**: Survival mechanics with health, hunger, status effects, and regeneration systems
- All systems are data-driven through JSON configuration files in `Assets/StreamingAssets/` and `config/`

## Configuration Management
- **Server Configuration**: `server-config.json` contains network, world, gameplay, and performance settings
- **Client Configuration**: `Assets/StreamingAssets/client-config.json` contains graphics, audio, controls, and interface settings
- **World Configuration**: `Assets/StreamingAssets/world-config.json` contains terrain generation parameters
- **Game Data**: JSON files for blocks, items, recipes, mobs, biomes, and structures
- All configuration files follow a hierarchical structure for easy maintenance and modding support
- Configuration is data-driven - changes can be made without code recompilation

## Implementation Plans
The following comprehensive implementation plans have been created to guide future development:

### 1. Feature Implementation Plan (2026-01-09)
- **File**: `minecraft_feature_implementation_plan_2026-01-09.json`
- **Content**: Complete implementation plan with 27 features categorized into Core (10), Content (7), and Util (10) categories
- **Focus**: Systematic approach to implementing all Minecraft features with proper categorization
- **Status**: All features documented with implementation order, dependencies, and completion tracking
- **Verification**: All using statements verified, compilation tests passed (0 errors), protobuf protocol validated

### 2. World Map Control Improvements
- **File**: `minecraft_world_map_control_improvements.md`
- **Content**: Server and client world map control system design with enhancement recommendations
- **Focus**: Efficient world management, streaming, and synchronization
- **Key Features**:
  - Server-side: Profile management, chunk caching, enhanced terrain pipeline integration
  - Client-side: World map control UI, mini-map display, biome information system
  - Configuration files: Enhanced server and client configuration
  - Data-driven biome system with JSON configuration

### 3. Data-Driven Approach Status
- **File**: `data_driven_approach_status.md`
- **Content**: Comprehensive analysis of data-driven systems across the project
- **Focus**: Ensuring all game systems are properly data-driven with JSON configuration
- **Key Systems**:
  - Block System (config/blocks.json): 20+ block types with comprehensive properties
  - Item System (config/items.json): Detailed item properties for various categories
  - Recipe System (config/recipes.json): Crafting, smelting, and cooking recipes
  - Biome System (config/biomes.json): 10 biome types with terrain and vegetation data
  - World Map Control: Enhanced server and client configurations
  - Additional Configurations: Server, client, world, gameplay, hunger, network

### 4. Terrain Generation Improvements
- **File**: `terrain_generation_improvements.md`
- **Content**: Detailed analysis of cave, river, and lake generation algorithms
- **Focus**: Enhanced terrain features with hydrology-aware generation
- **Key Algorithms**:
  - ImprovedCaveGenerator: Hydrology-aware cave generation with river suppression and support pillars
  - ImprovedRiverGenerator: Flow-aware river generation with seam stitching and confluence boosts
  - ImprovedLakeGenerator: Wetland-aware lake generation with outflow channels and shoreline shelves
  - ImprovedTerrainCoordinator: Unified hydrology/flow mask generation with edge stabilization

### 5. Protobuf Protocol Analysis
- **File**: `protobuf_protocol_*.md` (multiple analysis documents)
- **Content**: Protocol validation, implementation review, and improvement recommendations
- **Focus**: Ensuring proper Google.Protobuf integration and message handling
- **Status**: Dual protocol support (protobuf-net legacy + Google.Protobuf enhanced) validated

### 6. Configuration Files
- **Enhanced Server Configuration**: `config/enhanced_world_map_control_server.json`
  - Profile management with hot-reload support
  - Chunk caching with budget management
  - Real-time map updates
  - Terrain generation parameters
- **Enhanced Client Configuration**: `config/enhanced_world_map_control_client.json`
  - UI settings (mini-map, coordinates, biome info)
  - Display settings (FPS, ping)
  - Performance settings (chunk update throttling, concurrent requests)
- **Biome Configuration**: `config/biomes.json`
  - 10 biome types with comprehensive properties
  - Temperature, humidity, color, surface/underground blocks
  - Tree types, grass types, flower types
  - Water and snow colors for specific biomes

## Known Issues
- Some nullable reference warnings exist in the codebase (non-critical)
- Some async/await warnings for non-async methods (non-critical)
- These are code quality warnings and do not affect functionality

## License
This project is licensed under the MIT License - see LICENSE file for details.

## Contributing
Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## Contact
For questions or issues, please open an issue on the repository.

## Contributing
Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## Contact
For questions or issues, please open an issue on the repository.

### 5. Terrain Generation Improvements
- **File**: `terrain_generation_improvements.md`
- **Content**: Detailed analysis of cave, river, and lake generation algorithms
- **Focus**: Enhanced terrain features with hydrology-aware generation
- **Key Algorithms**:
  - ImprovedCaveGenerator: Hydrology-aware cave generation with river suppression and support pillars
  - ImprovedRiverGenerator: Flow-aware river generation with seam stitching and confluence boosts
  - ImprovedLakeGenerator: Wetland-aware lake generation with outflow channels and shoreline shelves
  - ImprovedTerrainCoordinator: Unified hydrology/flow mask generation with edge stabilization

### 6. Protobuf Protocol Analysis
- **File**: `protobuf_protocol_*.md` (multiple analysis documents)
- **Content**: Protocol validation, implementation review, and improvement recommendations
- **Focus**: Ensuring proper Google.Protobuf integration and message handling
- **Status**: Dual protocol support (protobuf-net legacy + Google.Protobuf enhanced) validated

### 7. Configuration Files
- **Enhanced Server Configuration**: `config/enhanced_world_map_control_server.json`
  - Profile management with hot-reload support
  - Chunk caching with budget management
  - Real-time map updates
  - Terrain generation parameters
- **Enhanced Client Configuration**: `config/enhanced_world_map_control_client.json`
  - UI settings (mini-map, coordinates, biome info)
  - Display settings (FPS, ping)
  - Performance settings (chunk update throttling, concurrent requests)
- **Biome Configuration**: `config/biomes.json`
  - 10 biome types with comprehensive properties
  - Temperature, humidity, color, surface/underground blocks
  - Tree types, grass types, flower types
  - Water and snow colors for specific biomes

## Known Issues
- Some nullable reference warnings exist in the codebase (non-critical)
- Some async/await warnings for non-async methods (non-critical)
- These are code quality warnings and do not affect functionality

## License
This project is licensed under the MIT License - see LICENSE file for details.

## Contributing
Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## Contact
For questions or issues, please open an issue on the repository.

## Contributing
Contributions are welcome! Please follow these guidelines:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## Contact
For questions or issues, please open an issue on the repository.

