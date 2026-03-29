# Session 206 Architecture and Code Flow

## Session Date
- 2026-03-21 (KST)

## Summary
Session 206 focused on cleanup tasks per worksheet.md requirements:
1. Removing outdated documentation files
2. Deleting unused code (client utilities, server handlers, legacy implementations)
3. Updating README.md references

## Deleted Documentation

### Root Level
- `ANALYSIS_SUMMARY.txt` - Pre-minetest analysis from 2025-11-09

### Config
- `config/README.md` - Referenced non-existent docs

### GitHub
- `.github/PULL_REQUEST_TEMPLATE.md` - Generic template with broken references

### GameServer.Launcher
- `GameServer.Launcher/README.md` - Outdated .NET 6.0 references

### Design (Session 178, 179)
- `design/2026-03-17-session-178-design-execution.md`
- `design/2026-03-17-session-179-design-execution.md`

## Deleted Code

### Client (Assets/MyAssets/Scripts/)
```
Utility/
├── KojeomUtilitySimple.cs    (unused)
├── KojeomLoggerSimple.cs     (unused)
├── CLock.cs                  (unused)
└── CustomConst.cs            (unused)

GameWorld/
└── EnhancedModifyWorldManager.cs  (unused)
```

### Server (GameServer/)
```
Network/
└── EnhancedProtocolHandler.cs     (unused)

Handlers/
├── SimpleMinecraftHandler.cs      (unused)
└── Disabled/                      (entire folder removed)

Utils/
└── ConfigValidator.cs             (Validate() never called)
```

### Legacy (KojeomNetWorkSpace/)
```
KojeomNetWorkSpace/
├── HMWGameServer/      (legacy server - replaced by GameServer/)
├── HMWTest/            (legacy test code)
├── SimpleTestServer/   (old test server)
└── SimpleTestClient/   (old test client)
```

Note: `KojeomNet.FrameWork/` retained - still used by Unity client.

## Current Architecture Reference

### Key Paths
- **Unity scripts**: `Assets/MyAssets/Scripts/`
- **Server**: `GameServer/`
- **Shared DLLs**: `GameCommon/`, `SharedProtocol/`
- **Proto sources**: `proto/`
- **Runtime configs**: `config/`, `GameServer/config/`, `Assets/StreamingAssets/`

### Compile-Test Infrastructure
- **Unity Commandlet**: `Assets/MyAssets/Scripts/Editor/Automation/UnityCiCommandlet.cs`
- **Batch Script**: `scripts/unity_compile_test.bat`
- **Proto Generation**: `scripts/generate_proto.bat`

### Game Data Pipeline
- **Templates**: `design/templates/*.md` (markdown format)
- **Exported JSON**: `Assets/StreamingAssets/game-data/` (generated)
- **Tool**: `Tools/GameDataTemplateExporter/` (.NET 8.0)

### Minetest Reference
- **Location**: `minetest_project/`
- **Key folders**:
  - `builtin/game/` - Core game logic (crafting, items, etc.)
  - `games/devtest/mods/` - Example game content
  - `src/` - C++ server/client implementation

## Remaining TODOs (Not Addressed This Session)

### Known Technical Debt
1. Duplicate terrain generators in `Assets/Scripts/Minecraft/World/`
2. Custom `Vector3` class in `GameServer/SessionManager.cs` (should use System.Numerics)
3. Multiple world map controller implementations
4. TODO comments in handlers for permission system, effects, etc.

### Future Cleanup Candidates
- Unused generation systems: `WaterPhysicsSystem`, `EntityCollisionSystem`, `MobSpawningSystem`, `BiomeGenerationSystem`, `OreDistributionSystem`
- These are defined but not instantiated - kept for future use
