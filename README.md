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
- **2026-01-08: Comprehensive Minecraft Feature Implementation and Code Quality Review**
  - Created comprehensive implementation roadmap v2.0 (`minecraft_implementation_roadmap_v2.md`)
  - Verified terrain generation algorithms (caves, rivers, lakes) - all implemented with advanced hydrology-aware features
  - Reviewed world map control system architecture - server and client implementations exist with profile-based configuration
  - Reviewed protobuf protocol usage and references - all using statements verified, Google.Protobuf properly integrated
  - Verified configuration files are properly structured in JSON format (server-config.json, world-config.json, client-config.json, blocks.json, items.json)
  - Verified data-driven approach with JSON files for blocks, items, and recipes
  - Successfully compiled SharedProtocol (0 errors, 10 warnings) and GameServer (0 errors, 34 warnings)
  - All warnings are nullable reference warnings and async/await warnings - no critical issues
  - Terrain generation improvements confirmed: ImprovedCaveGenerator, ImprovedRiverGenerator, ImprovedLakeGenerator with hydrology-aware algorithms
  - World map control architecture confirmed: WorldMapControlManager (server) and EnhancedWorldMapController (client)
  - Protobuf protocol confirmed: Google.Protobuf generated classes properly referenced throughout codebase

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

### 1. Implementation Roadmap v2.0
- **File**: `minecraft_implementation_roadmap_v2.md`
- **Content**: Complete implementation plan with Core, Content, and Utility feature categories
- **Focus**: Systematic approach to implementing all Minecraft features with proper categorization
- **Phases**:
  - Phase 1: Critical Infrastructure (terrain generation, networking, player systems, block system, chunk management, configuration)
  - Phase 2: Essential Gameplay (items & equipment, crafting, mobs & entities, ores & resources)
  - Phase 3: Advanced Content (structures & buildings, world features)
  - Phase 4: Polish & Optimization (UI, server management, development tools, data management, performance)

### 2. Feature Categorization
- **File**: `minecraft_feature_core_content_util.json`
- **Content**: Categorized feature list with Core, Content, and Utility categories
- **Focus**: Clear separation of concerns and implementation priorities

### 3. Terrain Generation Improvements
- **File**: `terrain_generation_improvements.md`
- **Content**: Detailed analysis of cave, river, and lake generation algorithms
- **Focus**: Enhanced terrain features with hydrology-aware generation

### 4. Protobuf Protocol Analysis
- **File**: `protobuf_protocol_*.md` (multiple analysis documents)
- **Content**: Protocol validation, implementation review, and improvement recommendations
- **Focus**: Ensuring proper Google.Protobuf integration and message handling

### 5. World Map Control Architecture
- **File**: `world_map_control_architecture_*.md` (multiple architecture documents)
- **Content**: Server and client world map control system design
- **Focus**: Efficient world management, streaming, and synchronization

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
