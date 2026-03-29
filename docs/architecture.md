# HELLO MY WORLD - Architecture Document

## Project Overview

HELLO MY WORLD is a Minecraft-like voxel game built with Unity 6000.0.71f1. The project focuses on a single-player/multiplayer voxel world with terrain generation, block manipulation, AI-driven NPCs/animals, and a P2P networking layer for multiplayer sessions.

## Project Structure

```
hello_my_world/
├── Assets/
│   ├── MyAssets/
│   │   ├── Atlas/              # Sprite atlases (blocks, items, UI, mobile)
│   │   ├── CustomShaders/      # Shader Graph shaders
│   │   ├── Font/               # Font assets
│   │   ├── RenderingPipeline/  # URP rendering settings
│   │   ├── Resources/          # Prefabs, materials, textures, text assets
│   │   ├── Scene/              # Unity scenes (MainMenu, InGame, UI_Popup, tests)
│   │   ├── Scripts/            # Core gameplay scripts (~130 .cs files)
│   │   └── Texture/            # Texture assets (blocks, items, UI, environment)
│   ├── Plugins/                # Third-party DLLs
│   │   ├── NetworkLib/         # KojeomNet.dll (TCP/P2P networking)
│   │   ├── Pixelplacement/     # iTween animation library
│   │   └── CsvHelper.dll       # CSV parsing
│   ├── ReferenceAssets/        # Third-party Unity assets (characters, shaders, UI)
│   ├── Shaders/                # Custom shader files
│   └── StreamingAssets/        # Runtime data files
├── Packages/                   # Unity Package Manager dependencies
├── ProjectSettings/            # Unity project configuration
├── docs/                       # Project documentation
├── AGENTS.md                   # AI agent coding guidelines
└── README.md
```

## Core Architecture

### Game Flow

```
GameSupervisor (Entry Point)
    ├── Init Phase
    │   ├── GameDataManager       # Data file loading (CSV tables, configs)
    │   ├── WorldAreaManager      # World terrain generation (Perlin noise)
    │   ├── GamePlayerManager     # Player character management
    │   ├── ActorSuperviosr       # NPC/Animal lifecycle
    │   ├── GameSoundManager      # Audio management
    │   └── GameParticleEffectManager
    ├── Game Mode Selection
    │   ├── SingleGameMode        # Single-player mode
    │   └── MultiGameMode         # Multi-player mode (P2P via KojeomNet)
    └── In-Game Loop
        ├── GameStateManager      # State machine (Start → Prepare → InGame → End)
        ├── InputManager          # Input abstraction (Desktop / Mobile)
        ├── ModifyWorldManager    # Block place/break system
        └── InGameUISupervisor    # HUD, popups, inventory
```

### Script Organization (Assets/MyAssets/Scripts/)

| Directory | Responsibility |
|---|---|
| `CentralSupervisor/` | `GameSupervisor` - main entry point, game lifecycle |
| `GameWorld/` | World areas, chunks, terrain, block modification |
| `Player/` | Player controller, camera, character instance |
| `MovableObjects/` | NPC/Animal AI, actors, spawners |
| `StateMachine/` | Game states, actor states, player states |
| `Input/` | Input abstraction (desktop, mobile, virtual joystick) |
| `UI/` | All UI managers (main menu, in-game, popups, shop) |
| `AI/` | Behavior trees, blackboard, NPC/animal AI logic |
| `PathFinding/` | Custom 3D A* pathfinding |
| `DataFiles/` | Data file readers (CSV tables, JSON configs) |
| `DataManageMent/` | Save/Load, SQLite DB, compression |
| `Network/` | P2P multiplayer via KojeomNet |
| `CustomStructure/Collision/` | AABB, OBB, Octree, RayCast |
| `GameMode/` | Single/Multi game mode implementations |
| `GameSound/` | Audio management |
| `CharacterBelt/` | Belt item selection UI |
| `ParticleSystem/` | Particle effect management |
| `MemorySystem/` | Soft object pointers |
| `CustomEditor/` | Unity editor tools |
| `Experiments/` | Test experiments |
| `Utility/` | Logger, constants, coroutine helpers, utilities |

### Key Design Patterns

**Singleton Pattern**
```csharp
public static GameSupervisor Instance { get; private set; }
// Set in Awake(), accessed globally
```

**Manager Pattern**
Each subsystem has a Manager class with `Init()` / `Begin()` lifecycle:
```csharp
WorldAreaManager.Instance.Init();
PlayerManagerInstance.Init();
ActorSuperviosrInstance.Begin();
```

**State Machine Pattern**
Abstract base `AGameState` with `StartState()`, `UpdateState()`, `EndState()`:
- `StartGameState` → `PrepareGameState` → `InGameState` → `EndGameState`

Actor states follow the same pattern via `AActorState` (NPC/Animal idle/walk/run).

**Behavior Tree (AI)**
NPC and Animal AI use a custom behavior tree with `BehaviorTree` node system and `BlackBoard` for shared state.

## World System

### Terrain Generation

Uses `HMWCore.dll` (core library with Perlin noise-based algorithms):
- `WorldGenAlgorithms.GenerateNormalTerrain()` - surface terrain
- `WorldGenAlgorithms.GenerateUndergroundTerrain()` - caves/underground
- `WorldGenAlgorithms.GenerateSubWorldWithPerlinNoise()` - subworlds
- Configurable via `WorldConfigFile` and `WorldMapDataFile`

### Chunk System

- `AChunk` - abstract base chunk (terrain, water, environment)
- `TerrainChunk` - solid terrain blocks
- `WaterChunk` - water bodies
- `EnviromentChunk` - environmental objects
- `WorldArea` - collection of chunks forming a play area
- `WorldAreaManager` - manages all world areas

### Block Modification

- `ModifyWorldManager` - main block modification controller
- `EnhancedModifyWorldManager` - extended modification features
- Supports block place, break, and modification with collision integration

## Networking

### Legacy P2P (KojeomNet)

Uses `KojeomNet.dll` (custom TCP framework) for peer-to-peer multiplayer:
- `GameNetworkManager` - connection lifecycle, world data sync
- `MultiPlayLobbyManager` - lobby/host/join flow
- Binary serialization with `CPacket` framing
- Protocol: `NetProtocol` enum (REQ/ACK/PUSH pattern)

## Data Management

### Data Files (CSV-based)
- `BlockTileDataFile` - block definitions
- `NPCDataFile` / `AnimalDataFile` - actor definitions
- `CraftItemListDataFile` - crafting recipes
- `GameConfigDataFile` - game configuration
- `WorldMapDataFile` / `WorldConfigFile` - world generation config

### Save/Load
- `SaveAndLoadManager` - JSON-based save files
- `GameDBManager` - SQLite persistence via `Mono.Data.Sqlite`
- `LZFCompress` - LZF compression for large data

## Key Dependencies

| Package | Purpose |
|---|---|
| `HMWCore.dll` | Core library: block types, world gen, math, noise |
| `MapGeneratorLib.dll` | Terrain generation (Perlin noise) - legacy, replaced by HMWCore |
| `CsvHelper.dll` | CSV file parsing |
| `Mono.Data.Sqlite.dll` | SQLite database access |
| `I18N.dll` / `I18N.CJK.dll` | Internationalization support |
| `iTween` | Animation tweening |
| Unity Netcode for GameObjects | Multiplayer framework (installed, not yet integrated) |
| Unity Relay | Relay service (installed, not yet integrated) |
| Unity AI Navigation | NavMesh pathfinding |

## Unity Configuration

- **Editor Version:** 6000.0.71f1
- **Scripting Backend:** Mono (.NET 4.x compatibility)
- **Color Space:** Gamma
- **Rendering:** Forward path, static batching enabled
- **Android:** Min SDK 23, Game category
- **Scripting Defines:** `UNITY_POST_PROCESSING_STACK_V2`

## Testing

- Unity Test Framework (NUnit) package installed (`com.unity.test-framework 1.6.0`)
- No formal test suites written yet
- Tests should be placed under `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`
