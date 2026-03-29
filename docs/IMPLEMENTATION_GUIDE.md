# HELLO_MY_WORLD 구현 가이드

**최종 업데이트**: 2025-11-08
**관련 문서**:
- `ARCHITECTURE_IMPROVEMENT_PLAN.md` - 전체 계획
- `PROJECT_REVIEW_REPORT.md` - 현재 상태 분석
- `CRITICAL_IMPROVEMENTS.md` - Critical 이슈 해결
- `UNITY_COMPATIBILITY.md` - Unity 6 호환성 가이드 ⭐ NEW

---

## 🎯 Unity 6 호환성

**프로젝트 환경**:
- **Unity 버전**: 6000.0.23f1 (Unity 6)
- **API Compatibility Level**: .NET Standard 2.1
- **GameCommon 타겟**: .NET Standard 2.1
- **C# 버전**: 9.0

**중요**: GameCommon 라이브러리는 Unity 6와 완벽하게 호환됩니다. .NET Standard 2.1은 Unity 공식 권장사항입니다. 자세한 내용은 `UNITY_COMPATIBILITY.md`를 참조하세요.

---

## 📋 빠른 시작

### 현재 완료된 작업

✅ **Phase 0 준비**:
1. 프로젝트 전체 구조 분석 완료
2. 아키텍처 개선안 문서 작성
3. GameCommon 프로젝트 구조 생성
   - `GameCommon/GameCommon.csproj`
   - `GameCommon/Blocks/BlockType.cs` - 통합 블록 타입
   - `GameCommon/Blocks/BlockProperties.cs` - 블록 속성

---

## 🚀 다음 단계 (순서대로 진행)

### Step 1: GameCommon 라이브러리 완성

**위치**: `/home/user/HELLO_MY_WORLD/GameCommon/`

**필요한 파일 작성**:

1. **Blocks/BlockRegistry.cs**
```csharp
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GameCommon.Blocks
{
    public static class BlockRegistry
    {
        private static Dictionary<BlockType, BlockProperties> _registry = new();

        public static void LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<BlockDatabase>(json);

            foreach (var block in data.Blocks)
            {
                _registry[block.Type] = block;
            }
        }

        public static BlockProperties Get(BlockType type)
        {
            return _registry.TryGetValue(type, out var props) ? props : GetDefault();
        }

        private static BlockProperties GetDefault()
        {
            return new BlockProperties
            {
                Type = BlockType.Air,
                Name = "air",
                Hardness = 0f
            };
        }
    }

    class BlockDatabase
    {
        public List<BlockProperties> Blocks { get; set; } = new();
    }
}
```

2. **Configuration/WorldConfig.cs**
```csharp
namespace GameCommon.Configuration
{
    public class WorldConfig
    {
        public GenerationConfig Generation { get; set; } = new();
        public ChunkConfig Chunks { get; set; } = new();
    }

    public class GenerationConfig
    {
        public int? Seed { get; set; }
        public DimensionsConfig Dimensions { get; set; } = new();
        public TerrainConfig Terrain { get; set; } = new();
        public WaterConfig Water { get; set; } = new();
        public CaveConfig Caves { get; set; } = new();
        public FeaturesConfig Features { get; set; } = new();
    }

    public class DimensionsConfig
    {
        public int MinHeight { get; set; } = -64;
        public int MaxHeight { get; set; } = 320;
        public int SeaLevel { get; set; } = 62;
    }

    public class TerrainConfig
    {
        public int BaseHeightMin { get; set; } = 45;
        public int BaseHeightMax { get; set; } = 150;
        public double OceanThreshold { get; set; } = 0.36;
        public double BeachThreshold { get; set; } = 0.42;
        public double CliffThreshold { get; set; } = 0.55;
    }

    public class WaterConfig
    {
        public int GlobalLevel { get; set; } = 62;
        public double RiverCenterThreshold { get; set; } = 0.0125;
        public double RiverBankThreshold { get; set; } = 0.028;
    }

    public class CaveConfig
    {
        public double HorizontalFrequency { get; set; } = 0.0026;
        public double VerticalFrequency { get; set; } = 0.018;
        public double Threshold { get; set; } = 0.42;
        public double LavaThreshold { get; set; } = 0.28;
    }

    public class FeaturesConfig
    {
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool EnableCaves { get; set; } = true;
        public bool EnableDungeons { get; set; } = true;
        public bool EnableOres { get; set; } = true;
        public bool EnableVegetation { get; set; } = true;
    }

    public class ChunkConfig
    {
        public int Size { get; set; } = 16;
        public int LoadRadius { get; set; } = 8;
        public int UnloadTimeoutMinutes { get; set; } = 30;
        public int SaveIntervalMinutes { get; set; } = 10;
    }
}
```

3. **Configuration/ConfigManager.cs**
```csharp
using System;
using System.IO;
using System.Text.Json;

namespace GameCommon.Configuration
{
    public class ConfigManager
    {
        public WorldConfig World { get; private set; } = new();
        public GameplayConfig Gameplay { get; private set; } = new();
        public ServerConfig Server { get; private set; } = new();

        public void LoadAll(string configDir)
        {
            World = LoadConfig<WorldConfig>(Path.Combine(configDir, "world.json"));
            Gameplay = LoadConfig<GameplayConfig>(Path.Combine(configDir, "gameplay.json"));
            Server = LoadConfig<ServerConfig>(Path.Combine(configDir, "server.json"));

            Console.WriteLine($"[ConfigManager] 설정 로드 완료: {configDir}");
        }

        private T LoadConfig<T>(string path) where T : new()
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[ConfigManager] 경고: {path} 파일이 없습니다. 기본값 사용.");
                return new T();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }

        public void Reload(string category)
        {
            // TODO: 카테고리별 리로드
            Console.WriteLine($"[ConfigManager] {category} 설정 리로드");
        }

        public event Action<string>? OnConfigChanged;
    }

    public class GameplayConfig
    {
        public int MaxPlayersPerWorld { get; set; } = 20;
        public bool EnablePvP { get; set; } = true;
        public float MaxBlockReachDistance { get; set; } = 6.0f;
        public InventoryConfig Inventory { get; set; } = new();
    }

    public class InventoryConfig
    {
        public int HotbarSlots { get; set; } = 9;
        public int MainSlots { get; set} = 27;
        public int ArmorSlots { get; set; } = 4;
    }

    public class ServerConfig
    {
        public NetworkConfig Network { get; set; } = new();
        public DatabaseConfig Database { get; set; } = new();
    }

    public class NetworkConfig
    {
        public int Port { get; set; } = 9000;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int MaxConnections { get; set; } = 100;
    }

    public class DatabaseConfig
    {
        public string DatabaseFile { get; set; } = "minecraft_game.db";
        public bool EnableWALMode { get; set; } = true;
    }
}
```

4. **빌드**
```bash
cd /home/user/HELLO_MY_WORLD/GameCommon
dotnet build
dotnet pack --configuration Release --output ./nupkg
```

---

### Step 2: 통합 설정 파일 생성

**위치**: `/home/user/HELLO_MY_WORLD/config/`

**생성할 파일**:

1. **config/world.json**
```json
{
  "generation": {
    "seed": null,
    "dimensions": {
      "minHeight": -64,
      "maxHeight": 320,
      "seaLevel": 62
    },
    "terrain": {
      "baseHeightMin": 45,
      "baseHeightMax": 150,
      "oceanThreshold": 0.36,
      "beachThreshold": 0.42,
      "cliffThreshold": 0.55
    },
    "water": {
      "globalLevel": 62,
      "riverCenterThreshold": 0.0125,
      "riverBankThreshold": 0.028
    },
    "caves": {
      "horizontalFrequency": 0.0026,
      "verticalFrequency": 0.018,
      "threshold": 0.42,
      "lavaThreshold": 0.28
    },
    "features": {
      "enableRivers": true,
      "enableLakes": true,
      "enableCaves": true,
      "enableDungeons": true,
      "enableOres": true,
      "enableVegetation": true
    }
  },
  "chunks": {
    "size": 16,
    "loadRadius": 8,
    "unloadTimeoutMinutes": 30,
    "saveIntervalMinutes": 10
  }
}
```

2. **config/blocks.json** (샘플, 전체 블록 추가 필요)
```json
{
  "blocks": [
    {
      "type": 1,
      "name": "stone",
      "displayName": "Stone",
      "hardness": 1.5,
      "resistance": 6.0,
      "isTransparent": false,
      "isFluid": false,
      "affectedByGravity": false,
      "requiredTool": "pickaxe",
      "requiredToolLevel": 0,
      "lightLevel": 0,
      "drops": [
        {
          "itemId": "minecraft:cobblestone",
          "chance": 1.0,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    },
    {
      "type": 12,
      "name": "sand",
      "displayName": "Sand",
      "hardness": 0.5,
      "affectedByGravity": true,
      "requiredTool": "shovel",
      "drops": [
        {
          "itemId": "minecraft:sand",
          "chance": 1.0,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    }
  ]
}
```

3. **config/gameplay.json**
```json
{
  "maxPlayersPerWorld": 20,
  "enablePvP": true,
  "maxBlockReachDistance": 6.0,
  "inventory": {
    "hotbarSlots": 9,
    "mainSlots": 27,
    "armorSlots": 4
  }
}
```

---

### Step 3: GameServer 리팩터링

**작업 내용**:

1. **GameServer.csproj 업데이트**
```xml
<ItemGroup>
  <ProjectReference Include="../GameCommon/GameCommon.csproj" />
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
</ItemGroup>
```

2. **WorldManager.cs 리팩터링**

**Before**:
```csharp
private const int GlobalWaterLevel = 62;
private const double RiverCenterThreshold = 0.0125;
```

**After**:
```csharp
private readonly ConfigManager _config;

public WorldManager(DatabaseHelper database, ConfigManager config)
{
    _database = database;
    _config = config;

    _waterLevel = _config.World.Generation.Water.GlobalLevel;
    _riverThreshold = _config.World.Generation.Water.RiverCenterThreshold;
}
```

3. **Program.cs 업데이트**
```csharp
using GameCommon.Configuration;
using GameCommon.Blocks;

class Program
{
    static async Task Main(string[] args)
    {
        // 설정 로드
        var configManager = new ConfigManager();
        configManager.LoadAll("./config");

        // 블록 레지스트리 초기화
        BlockRegistry.LoadFromJson("./config/blocks.json");

        // 서버 시작
        var server = new GameServer(configManager);
        await server.StartAsync();
    }
}
```

---

### Step 4: Unity 클라이언트 통합

**작업 내용**:

1. **GameCommon.dll 복사**
```bash
cp GameCommon/bin/Release/netstandard2.1/GameCommon.dll Assets/Plugins/
```

2. **Unity에서 사용**
```csharp
using GameCommon.Blocks;
using GameCommon.Configuration;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // 블록 레지스트리 초기화
        BlockRegistry.LoadFromJson("config/blocks.json");

        var props = BlockRegistry.Get(BlockType.Stone);
        Debug.Log($"Stone hardness: {props.Hardness}");
    }
}
```

---

## 🎯 우선순위 작업 목록

### Immediate (이번 주)
- [ ] GameCommon 라이브러리 완성
- [ ] 통합 설정 파일 작성
- [ ] GameServer 리팩터링 (설정 사용)
- [ ] blocks.json 전체 블록 정의
- [ ] 테스트 및 검증

### Short-term (2주 내)
- [ ] 레거시 코드 제거
- [ ] GameServer.Core 분리
- [ ] GameServer.Launcher 구현
- [ ] MapGeneratorLib 통합

### Medium-term (1-2개월)
- [ ] 몹 AI 시스템
- [ ] 블록 물리 시스템
- [ ] 크래프팅 고도화

### Long-term (3-6개월)
- [ ] 레드스톤 시스템
- [ ] 인챈트/포션 시스템
- [ ] 네더/엔드 차원

---

## 📚 참고 자료

### 관련 문서
- `docs/ARCHITECTURE_IMPROVEMENT_PLAN.md` - 전체 로드맵
- `docs/PROJECT_REVIEW_REPORT.md` - 현재 상태 분석
- `docs/CRITICAL_IMPROVEMENTS.md` - Critical 이슈 해결

### 샘플 코드
- `GameCommon/` - 공유 라이브러리 시작점
- `config/` - 설정 파일 예시

### 빌드 명령어
```bash
# GameCommon 빌드
cd GameCommon
dotnet build --configuration Release

# GameServer 빌드
cd ../GameServer
dotnet build --configuration Release

# 전체 솔루션 빌드
cd ..
dotnet build
```

---

## ⚠️ 주의사항

1. **.NET SDK 필요**: 이 환경에는 .NET SDK가 없으므로 로컬에서 빌드 필요
2. **Unity 호환성**: GameCommon은 .NET Standard 2.1로 Unity와 호환
3. **점진적 마이그레이션**: 한 번에 모든 것을 변경하지 말고 단계적 진행
4. **테스트**: 각 단계마다 기존 기능이 정상 작동하는지 확인

---

## 🐛 문제 해결

### Q: GameCommon을 Unity에서 인식 못 함
A: DLL을 Assets/Plugins/에 복사하고 Unity 재시작

### Q: 설정 파일 로드 실패
A: JSON 형식 검증 (https://jsonlint.com/)

### Q: 빌드 오류
A: NuGet 패키지 복원 (`dotnet restore`)

---

**버전**: 1.0.0
**최종 업데이트**: 2025-11-08
