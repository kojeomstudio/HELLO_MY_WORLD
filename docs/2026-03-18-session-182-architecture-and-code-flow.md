# Session 182 Architecture and Code Flow (2026-03-18)

## 1. System Overview
This document describes the current architecture and code flow for the Minecraft-like game server and client.

## 2. Core Components

### 2.1 Server Architecture (GameServer/)
- **Program.cs**: Entry point, server initialization, command registration
- **Handlers/**: Request handlers for all packet types (login, movement, inventory, crafting, etc.)
- **SessionManager.cs**: Player session management and state persistence
- **World/**: World generation, chunk management, terrain algorithms
- **World/Generation/**: Terrain generators (Simplex, Cave, Hydrology)

### 2.2 Protocol Layer (SharedProtocol/)
- **MinecraftMessageDispatcher.cs**: Message routing and dispatch
- **Session.cs**: Session abstraction for networking
- **WorldSyncMessages.cs**: World synchronization DTOs
- **ProtocolRegistry.cs**: Packet type registration and binding

### 2.3 Client Assets (Assets/)
- **MyAssets/Scripts/GameWorld/**: World rendering and chunk handling
- **MyAssets/Scripts/Network/**: Client-server communication
- **MyAssets/Scripts/UI/**: User interface components
- **Generated/Protobuf/**: Auto-generated protobuf DTOs

### 2.4 Data-Driven Configuration
- **config/**: Server configuration JSON files
- **config/game-data/**: Game data (items, recipes, monsters, npcs, character_stats)
- **design/templates/**: Markdown templates for game data authoring
- **Tools/GameDataTemplateExporter/**: C# tool to export templates to JSON

## 3. Code Flow

### 3.1 Server Startup
```
Program.Main()
  -> LoadConfiguration()
  -> InitializeProtobuf()
  -> RegisterHandlers()
  -> WorldManager.Initialize()
  -> StartTcpServer()
```

### 3.2 Client Connection
```
TcpServer.AcceptClient()
  -> Session.Create()
  -> SessionManager.Register()
  -> SendWelcomePacket()
```

### 3.3 Chunk Loading
```
Client -> ChunkLoadRequest
  -> MinecraftChunkHandler.Handle()
  -> WorldManager.GetOrGenerateChunk()
  -> ChunkLoadResponse -> Client
```

### 3.4 Game Data Flow
```
design/templates/game-data-template.md
  -> Tools/GameDataTemplateExporter (C# .NET 8)
  -> config/game-data/*.json
  -> GameServer loads on startup
  -> Validation via GameDataValidator
```

## 4. Protocol Stack

### 4.1 Google Protobuf (Primary)
- EnhancedMinecraftProtocol package
- Generated descriptors in Assets/Generated/Protobuf/
- Fingerprint verification on startup

### 4.2 Optional Packets (Fallback)
- MultiBlockChange, InventoryUpdate, ItemUse, ItemDrop, ItemPickup
- EntityUpdate, EntityInteract, ContainerOpen, ContainerClose, ContainerUpdate
- protobuf-net fallback when Google.Protobuf bindings unavailable

## 5. World Generation Pipeline

### 5.1 Terrain Generation
- Simplex noise for base terrain
- Hydrology system for rivers and lakes
- Cave generation with structural support
- Riparian buffers and wetland zones

### 5.2 Map Control Profile
- Version: 94 (hydrology-riverlake-cave-v90)
- Server/client parity enforced
- Queue policy with pressure management

## 6. Configuration Management

### 6.1 Parity Enforcement
- WorldMapControlProfile: Server/client mirrors
- WorldMapQueuePolicy: Version-locked settings
- GameData: Required datasets validated on startup

### 6.2 Mirror Locations
- config/ -> canonical
- GameServer/config/ -> server mirror
- GameServer/Assets/StreamingAssets/ -> client mirror
- Assets/StreamingAssets/ -> Unity client mirror
