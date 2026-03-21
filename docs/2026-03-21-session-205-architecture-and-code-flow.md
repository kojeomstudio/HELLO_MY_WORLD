# Session 205: Architecture and Code Flow

## Overview
이 세션에서는 worksheet.md 요구사항에 따라 프로젝트 인프라를 검증하고, 컴파일/테스트 파이프라인을 확인했다.

## Compile/Test Infrastructure

### Unity CI Commandlet
**Location**: `Assets/MyAssets/Scripts/Editor/Automation/UnityCiCommandlet.cs`

**Entry Points** (via `-executeMethod`):
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileAndTests` - 전체 테스트
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunCompileOnly` - 컴파일만
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunEditModeTests` - EditMode 테스트
- `HelloMyWorld.EditorAutomation.UnityCiCommandlet.RunPlayModeTests` - PlayMode 테스트

**Flow**:
1. Script compilation request via `CompilationPipeline.RequestScriptCompilation()`
2. Wait for compilation to complete
3. Execute tests via `TestRunnerApi`
4. Write summary JSON to `reports/unity-tests/`
5. Exit with success/failure code

### Batch Scripts
**Location**: `scripts/`

| Script | Purpose |
|--------|---------|
| `unity_compile_test.bat` | Unity CI commandlet 실행 (모드 선택 가능) |
| `generate_proto.bat` | Protobuf C# 코드 생성 |

**Unity Compile Test Usage**:
```batch
scripts\unity_compile_test.bat --unity "C:\Path\To\Unity.exe" [--mode all|compile|edit|play] [--log "path\to\log"]
```

## .NET Build Pipeline

### Projects
| Project | Target Framework | Warnings | Errors |
|---------|-----------------|----------|--------|
| SharedProtocol | .NET 8.0 | 8 | 0 |
| GameServer | .NET 8.0 | 32 | 0 |
| Tools/GameDataTemplateExporter | .NET 8.0 | 0 | 0 |

### Build Commands
```bash
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameServer/GameServer.csproj
dotnet build Tools/GameDataTemplateExporter/GameDataTemplateExporter.csproj
```

## Data-Driven Architecture

### Template Pipeline
```
design/templates/*.md (markdown with JSON blocks)
        ↓
Tools/GameDataTemplateExporter (C# .NET 8.0)
        ↓
Assets/StreamingAssets/game-data/*.json
```

**Usage**:
```bash
dotnet run --project Tools/GameDataTemplateExporter -- --input design/templates/game-data-template.md --output Assets/StreamingAssets/game-data
```

### Template Format
```markdown
## dataset: <name>
```json
{ ... }
```
```

## minetest Reference Integration

### Key Reference Files
| Path | Purpose |
|------|---------|
| `minetest_project/builtin/game/register.lua` | Item/node/recipe registration |
| `minetest_project/builtin/game/item.lua` | Item system implementation |
| `minetest_project/src/server.cpp` | Server architecture reference |
| `minetest_project/src/client/client.cpp` | Client architecture reference |
| `minetest_project/doc/world_format.md` | World storage format |

### Adoption Mapping
| Minetest | Our Project | Status |
|----------|-------------|--------|
| Server | GameServer | ✅ |
| Client | Unity Client | ✅ |
| EmergeManager | WorldManager | ✅ |
| Lua Mods | JSON Game Data | ✅ |

## Current Project Structure

```
HelloMyWorld_repo/
├── Assets/
│   ├── Generated/Protobuf/     # Generated C# from .proto
│   ├── MyAssets/Scripts/
│   │   ├── Editor/Automation/  # Unity CI commandlet
│   │   ├── GameWorld/          # World management
│   │   ├── Network/            # Network layer
│   │   └── UI/                 # UI components
│   └── StreamingAssets/
│       └── game-data/          # JSON game data
├── GameServer/                 # .NET server
│   ├── Handlers/               # Request handlers
│   ├── World/                  # World management
│   └── Systems/                # Game systems
├── SharedProtocol/             # Shared protobuf definitions
├── Tools/
│   ├── GameDataTemplateExporter/
│   └── DummyMinecraftClient/
├── design/                     # Design documents
│   └── templates/              # Markdown data templates
├── docs/                       # Architecture docs
├── plans/                      # Work plans
├── proto/                      # Protobuf definitions
├── scripts/                    # Build/test scripts
└── minetest_project/           # Reference submodule
```
