# Architecture Overview - Session 23 (2026-01-27)

## Table of Contents
1. [System Architecture](#system-architecture)
2. [Server Components](#server-components)
3. [Client Components](#client-components)
4. [Shared Components](#shared-components)
5. [Protocol Architecture](#protocol-architecture)
6. [Data Flow](#data-flow)
7. [Configuration Management](#configuration-management)
8. [Terrain Generation Pipeline](#terrain-generation-pipeline)
9. [World Map Control System](#world-map-control-system)
10. [Deployment Architecture](#deployment-architecture)

---

## System Architecture

The HELLO_MY_WORLD project implements a voxel-based Minecraft-like game with a client-server architecture using Unity for the client and .NET 6.0 for the server.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Unity Client (C#/.NET 4.5)            │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ GameWorld │ Network │ UI │ Player │ AI │        │  │
│  └─────────────────────────────────────────────────────────┘  │
│                         │                                     │
└─────────────────────────┼─────────────────────────────────────┘
                          │ TCP/IP
                          │
┌─────────────────────────┼─────────────────────────────────────┐
│                    .NET 6.0 Server (C#)            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Handlers │ World │ Systems │ Utils │ Testing  │  │
│  └──────────────────────────────────────────────────────┘  │
│                         │                                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ SharedProtocol │ GameCommon │ Configuration │      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Server Components

### Core Server Architecture

**Entry Point:** [`GameServer/Program.cs`](../GameServer/Program.cs)

**Main Components:**

#### 1. Session Management
- **File:** [`GameServer/SessionManager.cs`](../GameServer/SessionManager.cs)
- **Purpose:** Manages player sessions, authentication, and state tracking
- **Key Features:**
  - Player login/logout handling
  - Session state management
  - Player data persistence
  - Authentication validation

#### 2. Network Handlers
- **Location:** [`GameServer/Handlers/`](../GameServer/Handlers/)
- **Purpose:** Handle incoming protocol messages from clients
- **Key Handlers:**
  - [`LoginHandler.cs`](../GameServer/Handlers/LoginHandler.cs) - Authentication
  - [`MovementHandler.cs`](../GameServer/Handlers/MovementHandler.cs) - Player movement
  - [`WorldBlockHandler.cs`](../GameServer/Handlers/WorldBlockHandler.cs) - Block interactions
  - [`MinecraftChunkHandler.cs`](../GameServer/Handlers/MinecraftChunkHandler.cs) - Chunk loading
  - [`InventoryHandler.cs`](../GameServer/Handlers/InventoryHandler.cs) - Inventory management
  - [`CraftingHandler.cs`](../GameServer/Handlers/CraftingHandler.cs) - Crafting system
  - [`FoodSystemHandler.cs`](../GameServer/Handlers/FoodSystemHandler.cs) - Food consumption
  - [`HealthHandler.cs`](../GameServer/Handlers/HealthHandler.cs) - Health management
  - [`ChatHandler.cs`](../GameServer/Handlers/ChatHandler.cs) - Chat system
  - [`PlayerAttackHandler.cs`](../GameServer/Handlers/PlayerAttackHandler.cs) - Combat
  - [`MinecraftPlayerActionHandler.cs`](../GameServer/Handlers/MinecraftPlayerActionHandler.cs) - Player actions

#### 3. World Management
- **File:** [`GameServer/World/WorldManager.cs`](../GameServer/World/WorldManager.cs)
- **Purpose:** Central world state management and chunk coordination
- **Key Features:**
  - Chunk loading/unloading
  - World state persistence
  - Entity management
  - Block change tracking

#### 4. World Map Control
- **File:** [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- **Purpose:** World map preview generation and profile management
- **Key Features:**
  - Profile-based map control
  - Chunk caching with budget management
  - Signature-based regeneration
  - Config hot-reloading
  - Hash-based validation

#### 5. Terrain Generation Pipeline
- **Location:** [`GameServer/World/Generation/`](../GameServer/World/Generation/)
- **Purpose:** Procedural terrain, cave, river, and lake generation
- **Key Components:**
  - [`EnhancedTerrainGenerationPipeline.cs`](../GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs) - Main pipeline coordinator
  - [`ImprovedTerrainCoordinator.cs`](../GameServer/World/Generation/ImprovedTerrainCoordinator.cs) - Terrain stage coordinator
  - [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs) - Hydrology-aware cave generation
  - [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs) - Pressure-stabilised river generation
  - [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs) - Pressure-blend lake generation

#### 6. Gameplay Systems
- **Location:** [`GameServer/Systems/`](../GameServer/Systems/)
- **Purpose:** Core gameplay mechanics
- **Key Systems:**
  - [`CombatSystem.cs`](../GameServer/Systems/CombatSystem.cs) - Combat mechanics
  - [`InventorySystem.cs`](../GameServer/Systems/InventorySystem.cs) - Inventory management
  - [`HealthAndHungerSystem.cs`](../GameServer/Systems/HealthAndHungerSystem.cs) - Survival mechanics
  - [`EntitySyncService.cs`](../GameServer/Systems/EntitySyncService.cs) - Entity synchronization
  - [`WeatherSystem.cs`](../GameServer/Systems/WeatherSystem.cs) - Weather management
  - [`WorldTimeSystem.cs`](../GameServer/Systems/WorldTimeSystem.cs) - Day/night cycle
  - [`ContainerSystem.cs`](../GameServer/Systems/ContainerSystem.cs) - Container management

#### 7. Synchronization
- **Location:** [`GameServer/Synchronization/`](../GameServer/Synchronization/)
- **Purpose:** Coordinate state synchronization between server and clients
- **Key Components:**
  - [`BlockSyncCoordinator.cs`](../GameServer/Synchronization/BlockSyncCoordinator.cs) - Block change sync
  - [`ChunkSyncCoordinator.cs`](../GameServer/Synchronization/ChunkSyncCoordinator.cs) - Chunk sync
  - [`EntitySyncCoordinator.cs`](../GameServer/Synchronization/EntitySyncCoordinator.cs) - Entity sync
  - [`SyncManager.cs`](../GameServer/Synchronization/SyncManager.cs) - Sync coordination

#### 8. Utilities
- **Location:** [`GameServer/Utils/`](../GameServer/Utils/)
- **Purpose:** Helper utilities and services
- **Key Utilities:**
  - [`Logger.cs`](../GameServer/Utils/Logger.cs) - Logging system
  - [`Noise.cs`](../GameServer/Utils/Noise.cs) - Noise generation
  - [`SimplexNoise.cs`](../GameServer/Utils/SimplexNoise.cs) - Simplex noise algorithm
  - [`PerformanceMonitor.cs`](../GameServer/Utils/PerformanceMonitor.cs) - Performance monitoring
  - [`ConfigValidator.cs`](../GameServer/Utils/ConfigValidator.cs) - Config validation
  - [`ErrorHandler.cs`](../GameServer/Utils/ErrorHandler.cs) - Error handling

#### 9. Testing
- **Location:** [`GameServer/Testing/`](../GameServer/Testing/)
- **Purpose:** Testing and validation tools
- **Key Components:**
  - [`DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs) - Protocol round-trip testing

---

## Client Components

### Unity Client Architecture

**Entry Point:** Unity Editor/Player

**Main Components:**

#### 1. Game World
- **Location:** [`Assets/MyAssets/Scripts/GameWorld/`](../Assets/MyAssets/Scripts/GameWorld/)
- **Purpose:** World rendering and management
- **Key Components:**
  - [`WorldMapController.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs) - World map control and preview
  - [`WorldMapControlProfile.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs) - Profile management
  - [`ChunkManager.cs`](../Assets/MyAssets/Scripts/GameWorld/ChunkManager.cs) - Chunk loading and rendering
  - [`BlockManager.cs`](../Assets/MyAssets/Scripts/GameWorld/BlockManager.cs) - Block placement and rendering
  - [`BlockRenderer.cs`](../Assets/MyAssets/Scripts/GameWorld/BlockRenderer.cs) - Block visualization
  - [`BiomeManager.cs`](../Assets/MyAssets/Scripts/GameWorld/BiomeManager.cs) - Biome rendering
  - [`EntityManager.cs`](../Assets/MyAssets/Scripts/GameWorld/EntityManager.cs) - Entity management

#### 2. Network
- **Location:** [`Assets/MyAssets/Scripts/Network/`](../Assets/MyAssets/Scripts/Network/)
- **Purpose:** Network communication with server
- **Key Components:**
  - [`NetworkManager.cs`](../Assets/MyAssets/Scripts/Network/NetworkManager.cs) - Network connection management
  - Protocol message handling
  - Packet serialization/deserialization

#### 3. Data Management
- **Location:** [`Assets/MyAssets/Scripts/DataManageMent/`](../Assets/MyAssets/Scripts/DataManageMent/)
- **Purpose:** Configuration and data loading
- **Key Components:**
  - [`ConfigManager.cs`](../Assets/MyAssets/Scripts/DataManageMent/ConfigManager.cs) - Config loading and validation

#### 4. UI
- **Location:** [`Assets/MyAssets/Scripts/UI/`](../Assets/MyAssets/Scripts/UI/)
- **Purpose:** User interface components
- **Key Components:**
  - HUD elements
  - Inventory UI
  - Mini-map
  - Chat interface

#### 5. Player
- **Location:** [`Assets/MyAssets/Scripts/Player/`](../Assets/MyAssets/Scripts/Player/)
- **Purpose:** Player controller and input handling
- **Key Components:**
  - [`PlayerController.cs`](../Assets/MyAssets/Scripts/Player/PlayerController.cs) - Movement and interaction
  - Input handling
  - Animation control

#### 6. Utilities
- **Location:** [`Assets/MyAssets/Scripts/Utility/`](../Assets/MyAssets/Scripts/Utility/)
- **Purpose:** Helper utilities
- **Key Components:**
  - [`Logger.cs`](../Assets/MyAssets/Scripts/Utility/Logger.cs) - Logging system
  - [`PerformanceMonitor.cs`](../Assets/MyAssets/Scripts/Utility/PerformanceMonitor.cs) - FPS tracking

---

## Shared Components

### GameCommon.dll Architecture

**Location:** [`GameCommon/`](../GameCommon/)

**Purpose:** Shared code compiled to DLL for client-server synchronization

**Key Components:**

#### 1. World Contracts
- **File:** [`GameCommon/World/WorldMapContracts.cs`](../GameCommon/World/WorldMapContracts.cs)
- **Purpose:** Shared world map contracts and interfaces
- **Key Features:**
  - World map request/response contracts
  - Profile contracts
  - Chunk data contracts

#### 2. World Map Signature
- **File:** [`GameCommon/World/WorldMapSignature.cs`](../GameCommon/World/WorldMapSignature.cs)
- **Purpose:** World generation signature for client-server parity
- **Key Features:**
  - Signature computation
  - Version tracking
  - Hash validation
  - Hydrology signature v4 with flow-lock

#### 3. Shared Feature Catalog
- **File:** [`GameCommon/World/SharedFeatureCatalog.cs`](../GameCommon/World/SharedFeatureCatalog.cs)
- **Purpose:** Shared feature definitions and catalog
- **Key Features:**
  - Feature enumeration
  - Hydrology signature constants
  - Version information

#### 4. Block System
- **Location:** [`GameCommon/Blocks/`](../GameCommon/Blocks/)
- **Purpose:** Shared block type definitions
- **Key Components:**
  - [`BlockType.cs`](../GameCommon/Blocks/BlockType.cs) - Block type enumeration
  - [`BlockRegistry.cs`](../GameCommon/Blocks/BlockRegistry.cs) - Block registry
  - [`BlockProperties.cs`](../GameCommon/Blocks/BlockProperties.cs) - Block properties

#### 5. Configuration Models
- **File:** [`GameCommon/Configuration/ConfigModels.cs`](../GameCommon/Configuration/ConfigModels.cs)
- **Purpose:** Shared configuration data structures
- **Key Features:**
  - World settings models
  - Terrain generation config models
  - World map control config models

#### 6. Configuration Manager
- **File:** [`GameCommon/Configuration/ConfigManager.cs`](../GameCommon/Configuration/ConfigManager.cs)
- **Purpose:** Configuration loading and management
- **Key Features:**
  - JSON config loading
  - Config validation
  - Hot-reloading support

#### 7. Data Models
- **File:** [`GameCommon/DataDriven/DataModels.cs`](../GameCommon/DataDriven/DataModels.cs)
- **Purpose:** Shared data models
- **Key Features:**
  - Block data models
  - Item data models
  - Biome data models

#### 8. Data Manager
- **File:** [`GameCommon/DataDriven/DataManager.cs`](../GameCommon/DataDriven/DataManager.cs)
- **Purpose:** Data loading and management
- **Key Features:**
  - JSON data loading
  - Data validation
  - Data caching

---

## Protocol Architecture

### Protocol Stack

**Protocol Definition:** Google.Protobuf

**Proto Files:** [`proto/`](../proto/)

**Generated Code:**
- Server: [`SharedProtocol/`](../SharedProtocol/)
- Client: [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)

### Protocol Components

#### 1. Protocol Registry
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)
- **Purpose:** Central registry for message type to protobuf message mapping
- **Key Features:**
  - 13 registered message types
  - Required binding enforcement
  - Optional binding tracking
  - Descriptor validation
  - Fingerprint computation

#### 2. Protocol Validator
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)
- **Purpose:** Protocol validation and diagnostics
- **Key Features:**
  - Descriptor validation
  - Parser validation
  - Package validation
  - Registry health checks

#### 3. Protocol Runtime
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs)
- **Purpose:** Protocol runtime initialization and management
- **Key Features:**
  - Runtime initialization
  - Lazy loading
  - Singleton pattern

#### 4. Protocol Diagnostics
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)
- **Purpose:** Protocol diagnostics and logging
- **Key Features:**
  - Registry summary logging
  - Missing binding detection
  - Health check reporting

#### 5. Protocol Fingerprint
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)
- **Purpose:** Protocol fingerprint computation for validation
- **Key Features:**
  - Descriptor fingerprint
  - Fingerprint computation
  - Fingerprint assertions

#### 6. Message Dispatcher
- **File:** [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs)
- **Purpose:** Message dispatching to handlers
- **Key Features:**
  - Message routing
  - Handler registration
  - Async message handling

#### 7. World Sync Messages
- **File:** [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs)
- **Purpose:** World synchronization message definitions
- **Key Features:**
  - Chunk sync messages
  - Entity sync messages
  - Block sync messages

#### 8. Session Management
- **File:** [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs)
- **Purpose:** Shared session management
- **Key Features:**
  - Session state tracking
  - Incoming message handling
  - Outgoing message handling

### Registered Message Types

**Required Messages (13):**
1. PlayerStateUpdate
2. PlayerActionRequest
3. PlayerActionResponse
4. ChunkDataRequest
5. ChunkDataResponse
6. ChunkUnloadNotification
7. ChunkUnloadAcknowledge
8. BlockChangeNotification
9. EntitySpawn
10. EntityDespawn
11. TimeUpdate
12. WeatherChange
13. SoundEffect
14. ParticleEffect

**Optional Messages (10):**
1. MultiBlockChange
2. InventoryUpdate
3. ItemUse
4. ItemDrop
5. ItemPickup
6. EntityUpdate
7. EntityInteract
8. ContainerOpen
9. ContainerClose
10. ContainerUpdate

---

## Data Flow

### Client-Server Communication Flow

```
┌──────────────┐
│ Unity Client │
└──────┬───────┘
       │ TCP/IP
       │
┌──────▼──────────────────────────────────────────────┐
│         .NET 6.0 Server                    │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. MessageDispatcher receives packet   │  │
│  │ 2. Routes to appropriate handler     │  │
│  │ 3. Handler processes request        │  │
│  │ 4. Updates world state            │  │
│  │ 5. Sends response back to client   │  │
│  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### World Generation Flow

```
┌─────────────────────────────────────────────────────────┐
│ EnhancedTerrainGenerationPipeline                 │
│  ┌─────────────────────────────────────────────┐  │
│  │ 1. Load configuration                  │  │
│  │ 2. Generate base terrain               │  │
│  │ 3. Generate hydrology mask             │  │
│  │ 4. Generate flow mask                 │  │
│  │ 5. Generate erosion risk mask         │  │
│  │ 6. Generate river mask                │  │
│  │ 7. Generate lake mask                 │  │
│  │ 8. Generate cave mask                 │  │
│  │ 9. Apply masks to terrain             │  │
│  │ 10. Generate ore distribution          │  │
│  │ 11. Generate vegetation              │  │
│  │ 12. Generate structures               │  │
│  │ 13. Create chunk data                 │  │
│  └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Configuration Management

### Configuration Files

#### Server Configuration
- **File:** [`config/world.json`](../config/world.json)
- **Purpose:** Server world and gameplay settings
- **Key Sections:**
  - Network settings
  - World settings (seed, name)
  - Terrain generation parameters
  - Gameplay settings
  - Performance settings

#### World Map Control Configuration
- **File:** [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- **Purpose:** World map control profile
- **Key Sections:**
  - Profile version
  - Hydrology signature
  - Generation parameters
  - Quality settings
  - Cache settings

#### Enhanced Terrain Configuration
- **File:** [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)
- **Purpose:** Enhanced terrain generation parameters
- **Key Sections:**
  - Terrain parameters
  - Water parameters (hydrology, rivers, lakes)
  - Cave parameters
  - Ore distribution
  - Vegetation settings

#### Client Configuration
- **File:** [`Assets/StreamingAssets/world-config.json`](../Assets/StreamingAssets/world-config.json)
- **Purpose:** Client world configuration
- **Key Sections:**
  - World settings
  - Terrain generation parameters
  - Quality settings

#### Client World Map Control Configuration
- **File:** [`Assets/StreamingAssets/world-map-control.json`](../Assets/StreamingAssets/world-map-control.json)
- **Purpose:** Client world map control profile
- **Key Sections:**
  - Profile version
  - Hydrology signature
  - Generation parameters
  - Quality settings

#### Data Configuration Files
- **Biomes:** [`config/biomes.json`](../config/biomes.json)
- **Blocks:** [`config/blocks.json`](../config/blocks.json)
- **Items:** [`config/items.json`](../config/items.json)

### Configuration Loading Flow

```
┌─────────────────────────────────────────────────────────┐
│ ConfigManager (GameCommon)                     │
│  ┌─────────────────────────────────────────────┐  │
│  │ 1. Load JSON file                    │  │
│  │ 2. Validate JSON structure            │  │
│  │ 3. Deserialize to config models       │  │
│  │ 4. Apply default values              │  │
│  │ 5. Validate config values            │  │
│  └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Terrain Generation Pipeline

### Pipeline Stages

#### Stage 1: Base Terrain Generation
- **Purpose:** Generate base terrain heightmap
- **Algorithm:** Perlin noise with multiple octaves
- **Output:** Heightmap array

#### Stage 2: Hydrology Mask Generation
- **Purpose:** Generate hydrology (water) mask
- **Algorithm:** Noise-based hydrology with flow accumulation
- **Output:** Hydrology mask array

#### Stage 3: Flow Mask Generation
- **Purpose:** Generate water flow mask
- **Algorithm:** Downhill flow accumulation
- **Output:** Flow mask array

#### Stage 4: Erosion Risk Mask Generation
- **Purpose:** Generate erosion risk mask
- **Algorithm:** Slope-based erosion calculation
- **Output:** Erosion risk array

#### Stage 5: River Generation
- **File:** [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs)
- **Purpose:** Generate river mask with hydrology v4 features
- **Key Features:**
  - Pressure stabiliser (pressure blend/gradient clamp)
  - Flow shadow weighting
  - Anisotropy damping
  - Bank stability clamping
  - Confluence boosting
  - Headwater stability
  - Edge flow-lock bias

#### Stage 6: Lake Generation
- **File:** [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs)
- **Purpose:** Generate lake mask with hydrology v4 features
- **Key Features:**
  - Pressure blend for wetlands
  - Rim erosion weight tuning
  - Variance weight adjustment
  - Outflow channel carving
  - Lake shelves
  - Wetland buffer

#### Stage 7: Cave Generation
- **File:** [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs)
- **Purpose:** Generate cave mask with hydrology v4 features
- **Key Features:**
  - Moisture continuity clamp
  - Hydrology shadow thresholding
  - Flow drift term in stability scoring
  - Edge sealing with gradient-based suppression
  - Support pillars biased toward saturated terrain
  - Riparian cave plugging

#### Stage 8: Ore Distribution
- **Purpose:** Distribute ores throughout terrain
- **Algorithm:** Depth-based distribution with rarity weighting
- **Output:** Ore placement data

#### Stage 9: Vegetation Generation
- **Purpose:** Generate trees and plants
- **Algorithm:** Biome-based vegetation placement
- **Output:** Vegetation data

#### Stage 10: Structure Generation
- **Purpose:** Generate dungeons and structures
- **Algorithm:** Procedural structure placement
- **Output:** Structure data

---

## World Map Control System

### Server-Side Architecture

**Component:** [`WorldMapControlManager`](../GameServer/World/WorldMapControlManager.cs)

**Key Features:**

#### 1. Profile Management
- Profile loading from JSON
- Profile validation with hash checking
- Profile version tracking (v7)
- Signature validation (hydrology v4)

#### 2. Chunk Caching
- Concurrent dictionary for chunk cache
- Budget-based cache management
- Automatic cache eviction

#### 3. Generation Signature
- Computation of generation signature
- Hash-based validation
- Automatic pipeline regeneration on signature change

#### 4. Config Hot-Reloading
- File write time monitoring
- Hash-based change detection
- Automatic config reloading

### Client-Side Architecture

**Component:** [`WorldMapController`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs)

**Key Features:**

#### 1. Profile Loading
- Load profile from StreamingAssets
- Validate profile hash
- Check signature compatibility

#### 2. Chunk Streaming
- Async chunk generation
- Progressive loading
- Quality-based generation

#### 3. Preview Generation
- Real-time terrain preview
- Hydrology v4 support
- Pressure-stabilised features

### Signature Context

**Hydrology Signature:** `2026-01-27-hydrology-shield-v4-flow-lock`

**Signature Fields:**
- Pipeline version
- World name and seed
- Proto descriptor fingerprint
- Proto computed fingerprint
- Profile version and hash
- Hydrology signature
- Chunk size and world height
- Render distance
- Sea level
- Hydrology parameters (flow persistence, flow gain, watershed stitch weight, etc.)
- Cave parameters (ceiling moisture weight, moisture clamp, etc.)
- River parameters (anisotropy damping, bank stability clamp, etc.)
- Lake parameters (min/max depth, shelf depth, flow seepage weight, etc.)
- Edge parameters (edge blend radius, variance clamp, normalization, etc.)
- Flow parameters (flow memory weight, continuity weight, etc.)
- Pressure parameters (pressure blend, gradient clamp, edge flow lock weight)

---

## Deployment Architecture

### Build Process

#### Server Build
```bash
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameCommon/GameCommon.csproj
dotnet build GameServer/GameServer.csproj
```

#### Client Build
- Unity Editor build
- StreamingAssets copying
- GameCommon.dll copying to Plugins

### Runtime Architecture

#### Server Runtime
- .NET 6.0 runtime
- TCP/IP networking
- SQLite for persistence
- Multi-threaded processing

#### Client Runtime
- Unity 6000.0.23f1 engine
- .NET Framework 4.5
- TCP/IP networking
- StreamingAssets for data

### Shared DLL Deployment

**Build Output:** `GameCommon/bin/Debug/netstandard2.1/GameCommon.dll`

**Deployment Locations:**
1. Server: Automatically referenced via project reference
2. Client: `Assets/Plugins/GameCommon.dll` (manual copy after build)

**Synchronization Process:**
1. Build GameCommon project
2. Copy DLL to Unity Plugins folder
3. Reimport in Unity
4. Verify signature compatibility

---

## Performance Considerations

### Server Performance
- Chunk caching reduces generation overhead
- Concurrent processing for multiple clients
- Config hot-reloading without restart
- Efficient terrain generation algorithms

### Client Performance
- Progressive chunk loading
- Quality settings for performance scaling
- Efficient block rendering
- Async operations for non-blocking UI

### Network Performance
- Efficient packet serialization with protobuf
- Batched chunk data transmission
- Optimized entity synchronization

---

## Security Considerations

### Authentication
- Login validation
- Session token management
- Secure password handling (if implemented)

### Data Validation
- Config validation on load
- Packet validation on receive
- Input sanitization

### Error Handling
- Comprehensive error logging
- Graceful degradation
- Recovery mechanisms

---

## Testing Strategy

### Unit Testing
- Protocol message serialization/deserialization
- Configuration loading and validation
- Utility functions

### Integration Testing
- Client-server communication
- World generation pipeline
- Gameplay systems

### Protocol Testing
- Dummy client round-trip tests
- Message type validation
- Registry validation

---

## Future Improvements

### Short Term
1. Add missing round-trip tests for optional packets
2. Capture Unity preview screenshots
3. Add comprehensive unit tests
4. Improve error messages

### Medium Term
1. Add performance profiling
2. Implement load balancing
3. Add comprehensive logging
4. Improve documentation

### Long Term
1. Implement world save/load
2. Add modding support
3. Implement multiplayer features
4. Add AI systems

---

**Document Version:** 1.0
**Last Updated:** 2026-01-27
**Author:** Kilo Code
**Session:** 23 - Comprehensive Implementation & Validation

## Table of Contents
1. [System Architecture](#system-architecture)
2. [Server Components](#server-components)
3. [Client Components](#client-components)
4. [Shared Components](#shared-components)
5. [Protocol Architecture](#protocol-architecture)
6. [Data Flow](#data-flow)
7. [Configuration Management](#configuration-management)
8. [Terrain Generation Pipeline](#terrain-generation-pipeline)
9. [World Map Control System](#world-map-control-system)
10. [Deployment Architecture](#deployment-architecture)

---

## System Architecture

The HELLO_MY_WORLD project implements a voxel-based Minecraft-like game with a client-server architecture using Unity for the client and .NET 6.0 for the server.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Unity Client (C#/.NET 4.5)            │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ GameWorld │ Network │ UI │ Player │ AI │        │  │
│  └─────────────────────────────────────────────────────────┘  │
│                         │                                     │
└─────────────────────────┼─────────────────────────────────────┘
                          │ TCP/IP
                          │
┌─────────────────────────┼─────────────────────────────────────┐
│                    .NET 6.0 Server (C#)            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Handlers │ World │ Systems │ Utils │ Testing  │  │
│  └──────────────────────────────────────────────────────┘  │
│                         │                                     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ SharedProtocol │ GameCommon │ Configuration │      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Server Components

### Core Server Architecture

**Entry Point:** [`GameServer/Program.cs`](../GameServer/Program.cs)

**Main Components:**

#### 1. Session Management
- **File:** [`GameServer/SessionManager.cs`](../GameServer/SessionManager.cs)
- **Purpose:** Manages player sessions, authentication, and state tracking
- **Key Features:**
  - Player login/logout handling
  - Session state management
  - Player data persistence
  - Authentication validation

#### 2. Network Handlers
- **Location:** [`GameServer/Handlers/`](../GameServer/Handlers/)
- **Purpose:** Handle incoming protocol messages from clients
- **Key Handlers:**
  - [`LoginHandler.cs`](../GameServer/Handlers/LoginHandler.cs) - Authentication
  - [`MovementHandler.cs`](../GameServer/Handlers/MovementHandler.cs) - Player movement
  - [`WorldBlockHandler.cs`](../GameServer/Handlers/WorldBlockHandler.cs) - Block interactions
  - [`MinecraftChunkHandler.cs`](../GameServer/Handlers/MinecraftChunkHandler.cs) - Chunk loading
  - [`InventoryHandler.cs`](../GameServer/Handlers/InventoryHandler.cs) - Inventory management
  - [`CraftingHandler.cs`](../GameServer/Handlers/CraftingHandler.cs) - Crafting system
  - [`FoodSystemHandler.cs`](../GameServer/Handlers/FoodSystemHandler.cs) - Food consumption
  - [`HealthHandler.cs`](../GameServer/Handlers/HealthHandler.cs) - Health management
  - [`ChatHandler.cs`](../GameServer/Handlers/ChatHandler.cs) - Chat system
  - [`PlayerAttackHandler.cs`](../GameServer/Handlers/PlayerAttackHandler.cs) - Combat
  - [`MinecraftPlayerActionHandler.cs`](../GameServer/Handlers/MinecraftPlayerActionHandler.cs) - Player actions

#### 3. World Management
- **File:** [`GameServer/World/WorldManager.cs`](../GameServer/World/WorldManager.cs)
- **Purpose:** Central world state management and chunk coordination
- **Key Features:**
  - Chunk loading/unloading
  - World state persistence
  - Entity management
  - Block change tracking

#### 4. World Map Control
- **File:** [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- **Purpose:** World map preview generation and profile management
- **Key Features:**
  - Profile-based map control
  - Chunk caching with budget management
  - Signature-based regeneration
  - Config hot-reloading
  - Hash-based validation

#### 5. Terrain Generation Pipeline
- **Location:** [`GameServer/World/Generation/`](../GameServer/World/Generation/)
- **Purpose:** Procedural terrain, cave, river, and lake generation
- **Key Components:**
  - [`EnhancedTerrainGenerationPipeline.cs`](../GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs) - Main pipeline coordinator
  - [`ImprovedTerrainCoordinator.cs`](../GameServer/World/Generation/ImprovedTerrainCoordinator.cs) - Terrain stage coordinator
  - [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs) - Hydrology-aware cave generation
  - [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs) - Pressure-stabilised river generation
  - [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs) - Pressure-blend lake generation

#### 6. Gameplay Systems
- **Location:** [`GameServer/Systems/`](../GameServer/Systems/)
- **Purpose:** Core gameplay mechanics
- **Key Systems:**
  - [`CombatSystem.cs`](../GameServer/Systems/CombatSystem.cs) - Combat mechanics
  - [`InventorySystem.cs`](../GameServer/Systems/InventorySystem.cs) - Inventory management
  - [`HealthAndHungerSystem.cs`](../GameServer/Systems/HealthAndHungerSystem.cs) - Survival mechanics
  - [`EntitySyncService.cs`](../GameServer/Systems/EntitySyncService.cs) - Entity synchronization
  - [`WeatherSystem.cs`](../GameServer/Systems/WeatherSystem.cs) - Weather management
  - [`WorldTimeSystem.cs`](../GameServer/Systems/WorldTimeSystem.cs) - Day/night cycle
  - [`ContainerSystem.cs`](../GameServer/Systems/ContainerSystem.cs) - Container management

#### 7. Synchronization
- **Location:** [`GameServer/Synchronization/`](../GameServer/Synchronization/)
- **Purpose:** Coordinate state synchronization between server and clients
- **Key Components:**
  - [`BlockSyncCoordinator.cs`](../GameServer/Synchronization/BlockSyncCoordinator.cs) - Block change sync
  - [`ChunkSyncCoordinator.cs`](../GameServer/Synchronization/ChunkSyncCoordinator.cs) - Chunk sync
  - [`EntitySyncCoordinator.cs`](../GameServer/Synchronization/EntitySyncCoordinator.cs) - Entity sync
  - [`SyncManager.cs`](../GameServer/Synchronization/SyncManager.cs) - Sync coordination

#### 8. Utilities
- **Location:** [`GameServer/Utils/`](../GameServer/Utils/)
- **Purpose:** Helper utilities and services
- **Key Utilities:**
  - [`Logger.cs`](../GameServer/Utils/Logger.cs) - Logging system
  - [`Noise.cs`](../GameServer/Utils/Noise.cs) - Noise generation
  - [`SimplexNoise.cs`](../GameServer/Utils/SimplexNoise.cs) - Simplex noise algorithm
  - [`PerformanceMonitor.cs`](../GameServer/Utils/PerformanceMonitor.cs) - Performance monitoring
  - [`ConfigValidator.cs`](../GameServer/Utils/ConfigValidator.cs) - Config validation
  - [`ErrorHandler.cs`](../GameServer/Utils/ErrorHandler.cs) - Error handling

#### 9. Testing
- **Location:** [`GameServer/Testing/`](../GameServer/Testing/)
- **Purpose:** Testing and validation tools
- **Key Components:**
  - [`DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs) - Protocol round-trip testing

---

## Client Components

### Unity Client Architecture

**Entry Point:** Unity Editor/Player

**Main Components:**

#### 1. Game World
- **Location:** [`Assets/MyAssets/Scripts/GameWorld/`](../Assets/MyAssets/Scripts/GameWorld/)
- **Purpose:** World rendering and management
- **Key Components:**
  - [`WorldMapController.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs) - World map control and preview
  - [`WorldMapControlProfile.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs) - Profile management
  - [`ChunkManager.cs`](../Assets/MyAssets/Scripts/GameWorld/ChunkManager.cs) - Chunk loading and rendering
  - [`BlockManager.cs`](../Assets/MyAssets/Scripts/GameWorld/BlockManager.cs) - Block placement and rendering
  - [`BlockRenderer.cs`](../Assets/MyAssets/Scripts/GameWorld/BlockRenderer.cs) - Block visualization
  - [`BiomeManager.cs`](../Assets/MyAssets/Scripts/GameWorld/BiomeManager.cs) - Biome rendering
  - [`EntityManager.cs`](../Assets/MyAssets/Scripts/GameWorld/EntityManager.cs) - Entity management

#### 2. Network
- **Location:** [`Assets/MyAssets/Scripts/Network/`](../Assets/MyAssets/Scripts/Network/)
- **Purpose:** Network communication with server
- **Key Components:**
  - [`NetworkManager.cs`](../Assets/MyAssets/Scripts/Network/NetworkManager.cs) - Network connection management
  - Protocol message handling
  - Packet serialization/deserialization

#### 3. Data Management
- **Location:** [`Assets/MyAssets/Scripts/DataManageMent/`](../Assets/MyAssets/Scripts/DataManageMent/)
- **Purpose:** Configuration and data loading
- **Key Components:**
  - [`ConfigManager.cs`](../Assets/MyAssets/Scripts/DataManageMent/ConfigManager.cs) - Config loading and validation

#### 4. UI
- **Location:** [`Assets/MyAssets/Scripts/UI/`](../Assets/MyAssets/Scripts/UI/)
- **Purpose:** User interface components
- **Key Components:**
  - HUD elements
  - Inventory UI
  - Mini-map
  - Chat interface

#### 5. Player
- **Location:** [`Assets/MyAssets/Scripts/Player/`](../Assets/MyAssets/Scripts/Player/)
- **Purpose:** Player controller and input handling
- **Key Components:**
  - [`PlayerController.cs`](../Assets/MyAssets/Scripts/Player/PlayerController.cs) - Movement and interaction
  - Input handling
  - Animation control

#### 6. Utilities
- **Location:** [`Assets/MyAssets/Scripts/Utility/`](../Assets/MyAssets/Scripts/Utility/)
- **Purpose:** Helper utilities
- **Key Components:**
  - [`Logger.cs`](../Assets/MyAssets/Scripts/Utility/Logger.cs) - Logging system
  - [`PerformanceMonitor.cs`](../Assets/MyAssets/Scripts/Utility/PerformanceMonitor.cs) - FPS tracking

---

## Shared Components

### GameCommon.dll Architecture

**Location:** [`GameCommon/`](../GameCommon/)

**Purpose:** Shared code compiled to DLL for client-server synchronization

**Key Components:**

#### 1. World Contracts
- **File:** [`GameCommon/World/WorldMapContracts.cs`](../GameCommon/World/WorldMapContracts.cs)
- **Purpose:** Shared world map contracts and interfaces
- **Key Features:**
  - World map request/response contracts
  - Profile contracts
  - Chunk data contracts

#### 2. World Map Signature
- **File:** [`GameCommon/World/WorldMapSignature.cs`](../GameCommon/World/WorldMapSignature.cs)
- **Purpose:** World generation signature for client-server parity
- **Key Features:**
  - Signature computation
  - Version tracking
  - Hash validation
  - Hydrology signature v4 with flow-lock

#### 3. Shared Feature Catalog
- **File:** [`GameCommon/World/SharedFeatureCatalog.cs`](../GameCommon/World/SharedFeatureCatalog.cs)
- **Purpose:** Shared feature definitions and catalog
- **Key Features:**
  - Feature enumeration
  - Hydrology signature constants
  - Version information

#### 4. Block System
- **Location:** [`GameCommon/Blocks/`](../GameCommon/Blocks/)
- **Purpose:** Shared block type definitions
- **Key Components:**
  - [`BlockType.cs`](../GameCommon/Blocks/BlockType.cs) - Block type enumeration
  - [`BlockRegistry.cs`](../GameCommon/Blocks/BlockRegistry.cs) - Block registry
  - [`BlockProperties.cs`](../GameCommon/Blocks/BlockProperties.cs) - Block properties

#### 5. Configuration Models
- **File:** [`GameCommon/Configuration/ConfigModels.cs`](../GameCommon/Configuration/ConfigModels.cs)
- **Purpose:** Shared configuration data structures
- **Key Features:**
  - World settings models
  - Terrain generation config models
  - World map control config models

#### 6. Configuration Manager
- **File:** [`GameCommon/Configuration/ConfigManager.cs`](../GameCommon/Configuration/ConfigManager.cs)
- **Purpose:** Configuration loading and management
- **Key Features:**
  - JSON config loading
  - Config validation
  - Hot-reloading support

#### 7. Data Models
- **File:** [`GameCommon/DataDriven/DataModels.cs`](../GameCommon/DataDriven/DataModels.cs)
- **Purpose:** Shared data models
- **Key Features:**
  - Block data models
  - Item data models
  - Biome data models

#### 8. Data Manager
- **File:** [`GameCommon/DataDriven/DataManager.cs`](../GameCommon/DataDriven/DataManager.cs)
- **Purpose:** Data loading and management
- **Key Features:**
  - JSON data loading
  - Data validation
  - Data caching

---

## Protocol Architecture

### Protocol Stack

**Protocol Definition:** Google.Protobuf

**Proto Files:** [`proto/`](../proto/)

**Generated Code:**
- Server: [`SharedProtocol/`](../SharedProtocol/)
- Client: [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)

### Protocol Components

#### 1. Protocol Registry
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)
- **Purpose:** Central registry for message type to protobuf message mapping
- **Key Features:**
  - 13 registered message types
  - Required binding enforcement
  - Optional binding tracking
  - Descriptor validation
  - Fingerprint computation

#### 2. Protocol Validator
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)
- **Purpose:** Protocol validation and diagnostics
- **Key Features:**
  - Descriptor validation
  - Parser validation
  - Package validation
  - Registry health checks

#### 3. Protocol Runtime
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs)
- **Purpose:** Protocol runtime initialization and management
- **Key Features:**
  - Runtime initialization
  - Lazy loading
  - Singleton pattern

#### 4. Protocol Diagnostics
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)
- **Purpose:** Protocol diagnostics and logging
- **Key Features:**
  - Registry summary logging
  - Missing binding detection
  - Health check reporting

#### 5. Protocol Fingerprint
- **File:** [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)
- **Purpose:** Protocol fingerprint computation for validation
- **Key Features:**
  - Descriptor fingerprint
  - Fingerprint computation
  - Fingerprint assertions

#### 6. Message Dispatcher
- **File:** [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs)
- **Purpose:** Message dispatching to handlers
- **Key Features:**
  - Message routing
  - Handler registration
  - Async message handling

#### 7. World Sync Messages
- **File:** [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs)
- **Purpose:** World synchronization message definitions
- **Key Features:**
  - Chunk sync messages
  - Entity sync messages
  - Block sync messages

#### 8. Session Management
- **File:** [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs)
- **Purpose:** Shared session management
- **Key Features:**
  - Session state tracking
  - Incoming message handling
  - Outgoing message handling

### Registered Message Types

**Required Messages (13):**
1. PlayerStateUpdate
2. PlayerActionRequest
3. PlayerActionResponse
4. ChunkDataRequest
5. ChunkDataResponse
6. ChunkUnloadNotification
7. ChunkUnloadAcknowledge
8. BlockChangeNotification
9. EntitySpawn
10. EntityDespawn
11. TimeUpdate
12. WeatherChange
13. SoundEffect
14. ParticleEffect

**Optional Messages (10):**
1. MultiBlockChange
2. InventoryUpdate
3. ItemUse
4. ItemDrop
5. ItemPickup
6. EntityUpdate
7. EntityInteract
8. ContainerOpen
9. ContainerClose
10. ContainerUpdate

---

## Data Flow

### Client-Server Communication Flow

```
┌──────────────┐
│ Unity Client │
└──────┬───────┘
       │ TCP/IP
       │
┌──────▼──────────────────────────────────────────────┐
│         .NET 6.0 Server                    │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. MessageDispatcher receives packet   │  │
│  │ 2. Routes to appropriate handler     │  │
│  │ 3. Handler processes request        │  │
│  │ 4. Updates world state            │  │
│  │ 5. Sends response back to client   │  │
│  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### World Generation Flow

```
┌─────────────────────────────────────────────────────────┐
│ EnhancedTerrainGenerationPipeline                 │
│  ┌─────────────────────────────────────────────┐  │
│  │ 1. Load configuration                  │  │
│  │ 2. Generate base terrain               │  │
│  │ 3. Generate hydrology mask             │  │
│  │ 4. Generate flow mask                 │  │
│  │ 5. Generate erosion risk mask         │  │
│  │ 6. Generate river mask                │  │
│  │ 7. Generate lake mask                 │  │
│  │ 8. Generate cave mask                 │  │
│  │ 9. Apply masks to terrain             │  │
│  │ 10. Generate ore distribution          │  │
│  │ 11. Generate vegetation              │  │
│  │ 12. Generate structures               │  │
│  │ 13. Create chunk data                 │  │
│  └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Configuration Management

### Configuration Files

#### Server Configuration
- **File:** [`config/world.json`](../config/world.json)
- **Purpose:** Server world and gameplay settings
- **Key Sections:**
  - Network settings
  - World settings (seed, name)
  - Terrain generation parameters
  - Gameplay settings
  - Performance settings

#### World Map Control Configuration
- **File:** [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- **Purpose:** World map control profile
- **Key Sections:**
  - Profile version
  - Hydrology signature
  - Generation parameters
  - Quality settings
  - Cache settings

#### Enhanced Terrain Configuration
- **File:** [`config/enhanced_terrain_generation.json`](../config/enhanced_terrain_generation.json)
- **Purpose:** Enhanced terrain generation parameters
- **Key Sections:**
  - Terrain parameters
  - Water parameters (hydrology, rivers, lakes)
  - Cave parameters
  - Ore distribution
  - Vegetation settings

#### Client Configuration
- **File:** [`Assets/StreamingAssets/world-config.json`](../Assets/StreamingAssets/world-config.json)
- **Purpose:** Client world configuration
- **Key Sections:**
  - World settings
  - Terrain generation parameters
  - Quality settings

#### Client World Map Control Configuration
- **File:** [`Assets/StreamingAssets/world-map-control.json`](../Assets/StreamingAssets/world-map-control.json)
- **Purpose:** Client world map control profile
- **Key Sections:**
  - Profile version
  - Hydrology signature
  - Generation parameters
  - Quality settings

#### Data Configuration Files
- **Biomes:** [`config/biomes.json`](../config/biomes.json)
- **Blocks:** [`config/blocks.json`](../config/blocks.json)
- **Items:** [`config/items.json`](../config/items.json)

### Configuration Loading Flow

```
┌─────────────────────────────────────────────────────────┐
│ ConfigManager (GameCommon)                     │
│  ┌─────────────────────────────────────────────┐  │
│  │ 1. Load JSON file                    │  │
│  │ 2. Validate JSON structure            │  │
│  │ 3. Deserialize to config models       │  │
│  │ 4. Apply default values              │  │
│  │ 5. Validate config values            │  │
│  └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Terrain Generation Pipeline

### Pipeline Stages

#### Stage 1: Base Terrain Generation
- **Purpose:** Generate base terrain heightmap
- **Algorithm:** Perlin noise with multiple octaves
- **Output:** Heightmap array

#### Stage 2: Hydrology Mask Generation
- **Purpose:** Generate hydrology (water) mask
- **Algorithm:** Noise-based hydrology with flow accumulation
- **Output:** Hydrology mask array

#### Stage 3: Flow Mask Generation
- **Purpose:** Generate water flow mask
- **Algorithm:** Downhill flow accumulation
- **Output:** Flow mask array

#### Stage 4: Erosion Risk Mask Generation
- **Purpose:** Generate erosion risk mask
- **Algorithm:** Slope-based erosion calculation
- **Output:** Erosion risk array

#### Stage 5: River Generation
- **File:** [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs)
- **Purpose:** Generate river mask with hydrology v4 features
- **Key Features:**
  - Pressure stabiliser (pressure blend/gradient clamp)
  - Flow shadow weighting
  - Anisotropy damping
  - Bank stability clamping
  - Confluence boosting
  - Headwater stability
  - Edge flow-lock bias

#### Stage 6: Lake Generation
- **File:** [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs)
- **Purpose:** Generate lake mask with hydrology v4 features
- **Key Features:**
  - Pressure blend for wetlands
  - Rim erosion weight tuning
  - Variance weight adjustment
  - Outflow channel carving
  - Lake shelves
  - Wetland buffer

#### Stage 7: Cave Generation
- **File:** [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs)
- **Purpose:** Generate cave mask with hydrology v4 features
- **Key Features:**
  - Moisture continuity clamp
  - Hydrology shadow thresholding
  - Flow drift term in stability scoring
  - Edge sealing with gradient-based suppression
  - Support pillars biased toward saturated terrain
  - Riparian cave plugging

#### Stage 8: Ore Distribution
- **Purpose:** Distribute ores throughout terrain
- **Algorithm:** Depth-based distribution with rarity weighting
- **Output:** Ore placement data

#### Stage 9: Vegetation Generation
- **Purpose:** Generate trees and plants
- **Algorithm:** Biome-based vegetation placement
- **Output:** Vegetation data

#### Stage 10: Structure Generation
- **Purpose:** Generate dungeons and structures
- **Algorithm:** Procedural structure placement
- **Output:** Structure data

---

## World Map Control System

### Server-Side Architecture

**Component:** [`WorldMapControlManager`](../GameServer/World/WorldMapControlManager.cs)

**Key Features:**

#### 1. Profile Management
- Profile loading from JSON
- Profile validation with hash checking
- Profile version tracking (v7)
- Signature validation (hydrology v4)

#### 2. Chunk Caching
- Concurrent dictionary for chunk cache
- Budget-based cache management
- Automatic cache eviction

#### 3. Generation Signature
- Computation of generation signature
- Hash-based validation
- Automatic pipeline regeneration on signature change

#### 4. Config Hot-Reloading
- File write time monitoring
- Hash-based change detection
- Automatic config reloading

### Client-Side Architecture

**Component:** [`WorldMapController`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs)

**Key Features:**

#### 1. Profile Loading
- Load profile from StreamingAssets
- Validate profile hash
- Check signature compatibility

#### 2. Chunk Streaming
- Async chunk generation
- Progressive loading
- Quality-based generation

#### 3. Preview Generation
- Real-time terrain preview
- Hydrology v4 support
- Pressure-stabilised features

### Signature Context

**Hydrology Signature:** `2026-01-27-hydrology-shield-v4-flow-lock`

**Signature Fields:**
- Pipeline version
- World name and seed
- Proto descriptor fingerprint
- Proto computed fingerprint
- Profile version and hash
- Hydrology signature
- Chunk size and world height
- Render distance
- Sea level
- Hydrology parameters (flow persistence, flow gain, watershed stitch weight, etc.)
- Cave parameters (ceiling moisture weight, moisture clamp, etc.)
- River parameters (anisotropy damping, bank stability clamp, etc.)
- Lake parameters (min/max depth, shelf depth, flow seepage weight, etc.)
- Edge parameters (edge blend radius, variance clamp, normalization, etc.)
- Flow parameters (flow memory weight, continuity weight, etc.)
- Pressure parameters (pressure blend, gradient clamp, edge flow lock weight)

---

## Deployment Architecture

### Build Process

#### Server Build
```bash
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameCommon/GameCommon.csproj
dotnet build GameServer/GameServer.csproj
```

#### Client Build
- Unity Editor build
- StreamingAssets copying
- GameCommon.dll copying to Plugins

### Runtime Architecture

#### Server Runtime
- .NET 6.0 runtime
- TCP/IP networking
- SQLite for persistence
- Multi-threaded processing

#### Client Runtime
- Unity 6000.0.23f1 engine
- .NET Framework 4.5
- TCP/IP networking
- StreamingAssets for data

### Shared DLL Deployment

**Build Output:** `GameCommon/bin/Debug/netstandard2.1/GameCommon.dll`

**Deployment Locations:**
1. Server: Automatically referenced via project reference
2. Client: `Assets/Plugins/GameCommon.dll` (manual copy after build)

**Synchronization Process:**
1. Build GameCommon project
2. Copy DLL to Unity Plugins folder
3. Reimport in Unity
4. Verify signature compatibility

---

## Performance Considerations

### Server Performance
- Chunk caching reduces generation overhead
- Concurrent processing for multiple clients
- Config hot-reloading without restart
- Efficient terrain generation algorithms

### Client Performance
- Progressive chunk loading
- Quality settings for performance scaling
- Efficient block rendering
- Async operations for non-blocking UI

### Network Performance
- Efficient packet serialization with protobuf
- Batched chunk data transmission
- Optimized entity synchronization

---

## Security Considerations

### Authentication
- Login validation
- Session token management
- Secure password handling (if implemented)

### Data Validation
- Config validation on load
- Packet validation on receive
- Input sanitization

### Error Handling
- Comprehensive error logging
- Graceful degradation
- Recovery mechanisms

---

## Testing Strategy

### Unit Testing
- Protocol message serialization/deserialization
- Configuration loading and validation
- Utility functions

### Integration Testing
- Client-server communication
- World generation pipeline
- Gameplay systems

### Protocol Testing
- Dummy client round-trip tests
- Message type validation
- Registry validation

---

## Future Improvements

### Short Term
1. Add missing round-trip tests for optional packets
2. Capture Unity preview screenshots
3. Add comprehensive unit tests
4. Improve error messages

### Medium Term
1. Add performance profiling
2. Implement load balancing
3. Add comprehensive logging
4. Improve documentation

### Long Term
1. Implement world save/load
2. Add modding support
3. Implement multiplayer features
4. Add AI systems

---

**Document Version:** 1.0
**Last Updated:** 2026-01-27
**Author:** Kilo Code
**Session:** 23 - Comprehensive Implementation & Validation

