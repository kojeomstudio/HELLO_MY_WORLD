# Session 214 Architecture and Code Flow

## Date
- 2026-03-23

## Overview
This session verified the project infrastructure status, compile-test capabilities, and documented the current architecture per worksheet.md requirements.

## Architecture Summary

### 1. Project Structure

```
HelloMyWorld_repo/
├── Assets/                      # Unity client assets
│   ├── MyAssets/Scripts/        # Gameplay scripts
│   │   ├── GameWorld/           # World, chunks, player controller
│   │   ├── Network/             # Packet handling
│   │   ├── UI/                  # UI managers
│   │   ├── StateMachine/        # Game/player state machines
│   │   ├── DataFiles/           # Data file readers
│   │   └── Editor/Automation/   # CI commandlets
│   └── Generated/Protobuf/      # Generated Protobuf DTOs
├── GameServer/                  # .NET game server
│   ├── Handlers/                # Protocol handlers
│   ├── World/                   # World generation
│   ├── Systems/                 # Game systems
│   └── Models/                  # Data models
├── SharedProtocol/              # Shared protocol definitions
├── GameCommon/                  # Common .NET utilities
├── minetest_project/            # Minetest submodule (reference)
├── proto/                       # .proto source files
├── scripts/                     # Build/utility scripts
├── docs/                        # Architecture documentation
├── design/                      # Design documents
└── plans/                       # Session work plans
```

### 2. Compile-Test Infrastructure

#### UnityCiCommandlet (Assets/MyAssets/Scripts/Editor/Automation/UnityCiCommandlet.cs)
Entry points for Unity batch mode CI:
- `RunCompileAndTests()`: Compile + EditMode + PlayMode tests
- `RunCompileOnly()`: Compile verification only
- `RunEditModeTests()`: EditMode unit tests
- `RunPlayModeTests()`: PlayMode integration tests

Features:
- 30-minute timeout protection
- JSON summary reports in `reports/unity-tests/`
- Proper cleanup and exit code handling

#### Batch Script (scripts/unity_compile_test.bat)
```
Usage: scripts\unity_compile_test.bat --unity "C:\Path\To\Unity.exe" [--mode all|compile|edit|play] [--log "path"]
```

Environment variables for Unity path:
- UNITY_EXE_PATH
- UNITY_EXE_ENV
- UNITY_PATH

### 3. Data-Driven Configuration

Per worksheet.md requirements, JSON configuration files are used:
- `server-config.json`: Server configuration
- Game data files in `Assets/MyAssets/DataFiles/`

### 4. Build Commands

#### .NET Projects
```bash
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameServer/GameServer.csproj
```

#### Server Run
```bash
dotnet run --project GameServer -- --server
```

#### Self-Test Mode
```bash
dotnet run --project GameServer -- --selftest
```

### 5. Code Flow

#### Client Connection Flow
1. `NetworkManager` establishes TCP connection
2. `PacketHandler` receives/parses incoming packets
3. `GameStateMachine` manages connection states
4. `GameWorldManager` loads/unloads chunks based on player position

#### Server Request Flow
1. `SessionManager` manages client sessions
2. Handlers in `Handlers/` process protocol messages
3. `WorldManager` provides chunk/block data
4. Response packets serialized via SharedProtocol

## Key Files

| File | Purpose |
|------|---------|
| GameServer/Program.cs | Server entry point |
| GameServer/SessionManager.cs | Client session management |
| Assets/MyAssets/Scripts/Network/PacketHandler.cs | Client packet handling |
| Assets/MyAssets/Scripts/GameWorld/GameWorldManager.cs | World management |
| SharedProtocol/Protocol.cs | Protocol definitions |

## Platform Targets
- Windows (primary)
- Linux
- macOS

Per worksheet.md: PC platform support required for all three operating systems.
