# Repository Guidelines

## Project Overview

Minecraft-like voxel game with a Unity 6000.0.71f1 client (`Assets/`). The project focuses on single-player/multiplayer voxel world with terrain generation, block manipulation, AI-driven NPCs/animals, and a P2P networking layer. Core game data types and world generation algorithms are provided by the `HMWCore` library.

## Project Structure

```
Assets/MyAssets/Scripts/   - Unity client scripts (GameWorld/, Network/, UI/, AI/, Player/, etc.)
Assets/Plugins/            - Third-party DLLs (HMWCore.dll, KojeomNet.dll, CsvHelper.dll)
Assets/Shaders/            - Custom shaders (voxel block, water, foliage wind)
Assets/ReferenceAssets/    - Third-party Unity assets (characters, shaders, UI)
HMWCore/                   - Core library: BlockTileType, world gen, math, noise (netstandard2.0)
docs/                      - Architecture documentation
```

## Build, Test, and Development Commands

```bash
# Build HMWCore library
dotnet build HMWCore/HMWCore/HMWCore.csproj -c Release

# Copy built DLL to Unity (after building HMWCore)
cp HMWCore/HMWCore/bin/Release/netstandard2.0/HMWCore.dll Assets/Plugins/HMWCore.dll

# Unity CLI compilation test
"C:\Program Files\Unity\Hub\Editor\6000.0.71f1\Editor\Unity.exe" ^
  -batchmode -nographics -projectPath . -quit -returnCode
```

**Unity version:** 6000.0.71f1

**Testing:** Unity Test Framework (NUnit) installed (`com.unity.test-framework 1.6.0`). No formal test suites written yet. Tests should go in `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`.

**Linting/Formatting:** No linter or formatter is configured.

## Coding Style & Naming Conventions

### General

- **Braces:** Allman style (opening brace on new line). Exception: short Unity one-liners may use K&R.
- **Indentation:** Tabs for Unity client scripts. 4 spaces for HMWCore library code.
- **Namespaces:** Block-scoped in Unity code. `namespace HMWCore` in the core library.
- **Access modifiers:** Always explicit in HMWCore. Unity lifecycle methods (`Start`, `Update`, `Awake`) may omit the modifier.
- **Comments:** Bilingual (Korean and English).

### Naming

| Element | Convention | Example |
|---|---|---|
| Types, methods, properties | PascalCase | `GameSupervisor`, `HandleAsync`, `OnlinePlayerCount` |
| Local variables, parameters | camelCase | `chunkX`, `userName` |
| Private fields | `_camelCase` | `_listener`, `_database` |
| Constants | PascalCase | `GlobalWaterLevel` |
| Enums (C#) | PascalCase values | `BlockTileType.GRASS`, `TreeType.NORMAL` |
| Abstract Unity classes | `A` prefix | `AGameState`, `AActorState`, `AGameModeBase` |

### Using Directive Ordering

System namespaces first, then third-party, then project namespaces:
```csharp
using System;
using System.Collections.Generic;
using HMWCore;
using UnityEngine;
```

## HMWCore Library

- **Target:** netstandard2.0 (Unity Mono compatible)
- **Namespace:** `HMWCore`
- **Build:** SDK-style .csproj, `dotnet build`
- **Key types:** `BlockTileType`, `ChunkType`, `Block`, `PlaneData`, `WorldGenAlgorithms`, `EnviromentGenAlgorithms`, `WorldGenerateUtils`, `CustomVector3`, `CustomMathf`, `Noise`
- **No dependencies:** All math/types are self-contained

## Unity Client Patterns

- **Singleton:** `public static MyClass Instance { get; private set; }` in `Awake()`.
- **Manager pattern:** Each subsystem has a `Manager` class with `Init()`/`Begin()` methods.
- **State machine:** Abstract base class (`AGameState`) with `StartState()`, `UpdateState()`, `EndState()`.
- **Inspector fields:** `[SerializeField] private` for editor-exposed fields. Use `#region` blocks.
- **`virtual` placement:** Legacy Unity code uses `virtual public`. New code should use `public virtual`.

## Commit & Pull Request Guidelines

Use conventional commits: `feat(core): add diamond ore generation`, `fix(world): correct cave carving radius`, `docs: update architecture`. Update `docs/` when altering architecture.

## Security & Configuration

- Never commit secrets, credentials, or local database dumps.
- Review changes for inadvertent credential exposure before submission.
