# Session 213 Architecture and Code Flow

## Date
- 2026-03-23 (KST)

## Project Overview
This project is a Minecraft-like voxel game with Unity3D client and .NET server, following data-driven development principles.

## Architecture Summary

### 1. Client Architecture (Unity3D)

#### Core Modules
```
Assets/MyAssets/Scripts/
├── GameWorld/           # World and chunk management
│   ├── PlayerController.cs
│   ├── WorldMapController.cs
│   ├── Chunk/           # Chunk types (Terrain, Water, Environment)
│   └── Enviroment/      # Weather system
├── Network/             # Network communication
│   └── CPacket.cs
├── UI/                  # User interface
│   ├── MainMenuManager.cs
│   ├── InGame/          # In-game UI
│   └── InChSelect/      # Character selection
├── StateMachine/        # State management
│   ├── GameState/       # Game states (Start, Prepare, InGame, End)
│   ├── playerState/     # Player states (Idle, Move, Jump)
│   └── ActorState/      # NPC and Animal states
├── DataFiles/           # Data loading and management
│   ├── DataFile/        # Data file definitions
│   └── Tables/          # Table readers
├── Input/               # Input handling
│   ├── InputManager.cs
│   └── VirtualJoystick/
└── Utility/             # Helper utilities
```

### 2. Server Architecture (.NET 6.0)

#### Core Modules
```
GameServer/
├── Program.cs           # Entry point
├── GameServer.cs        # Main server class
├── SessionManager.cs    # Session management
├── Handlers/            # Protocol handlers
│   ├── LoginHandler.cs
│   ├── MovementHandler.cs
│   ├── InventoryHandler.cs
│   ├── CraftingHandler.cs
│   ├── ChatHandler.cs
│   └── ... (other handlers)
├── World/               # World generation and management
│   ├── WorldManager.cs
│   ├── Generation/      # Terrain generation pipeline
│   │   ├── Stages/      # Generation stages
│   │   └── ... (generators)
│   └── Spawning/        # Mob spawning
├── Systems/             # Game systems
│   ├── InventorySystem.cs
│   ├── HealthAndHungerSystem.cs
│   ├── PhysicsSystem.cs
│   ├── CombatSystem.cs
│   └── ... (other systems)
├── Models/              # Data models
│   ├── BlockType.cs
│   ├── Item.cs
│   ├── Entity.cs
│   └── ... (other models)
├── Room/                # Room management
│   ├── RoomManager.cs
│   └── GameRoom.cs
└── Configuration/       # Configuration
    └── DataDrivenConfigManager.cs
```

### 3. Shared Protocol

#### Protocol Structure
```
SharedProtocol/
├── Session.cs              # Session management
├── MinecraftMessageDispatcher.cs  # Message routing
├── WorldSyncMessages.cs    # World synchronization messages
└── ... (protocol definitions)
```

### 4. Minetest Reference

The minetest submodule provides reference implementations for:
- Game logic patterns (`builtin/game/`)
- Common utilities (`builtin/common/`)
- UI patterns (`builtin/mainmenu/`)

## Data Flow

### Client-Server Communication
```
Unity Client <--TCP--> SharedProtocol <--TCP--> GameServer
     |                      |                      |
CPacket.cs          MessageDispatcher      Handlers/
     |                      |                      |
GameWorld/          Protocol Messages      Systems/
```

### World Data Flow
```
GameServer/
├── World/WorldManager.cs
│   ├── Generation/TerrainGenerationPipeline.cs
│   │   └── Stages/*GenerationStage.cs
│   └── WorldSynchronizationManager.cs
└── Handlers/MinecraftChunkHandler.cs
        │
        v
    Network Protocol
        │
        v
Unity Client/
└── GameWorld/WorldMapController.cs
    └── Chunk/TerrainChunk.cs
```

## Configuration

### Data-Driven Design
- Configuration files: `*.json`
- Game data template: `design/templates/game-data-template.md`
- Data exporter: `Tools/GameDataTemplateExporter/`
- Output paths:
  - Client: `Assets/StreamingAssets/game-data/`
  - Server: `GameServer/config/game-data/`

## Build Commands

```bash
# Server build
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameServer/GameServer.csproj

# Run server
dotnet run --project GameServer -- --server

# Self-test mode
dotnet run --project GameServer -- --selftest

# Protobuf regeneration
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

## Compile Test Results

| Project | Target | Warnings | Errors | Status |
|---------|--------|----------|--------|--------|
| SharedProtocol | net6.0 | 8 | 0 | PASS |
| GameServer | net6.0 | 27 | 0 | PASS |
