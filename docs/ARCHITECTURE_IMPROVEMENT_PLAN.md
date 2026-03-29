# HELLO_MY_WORLD 종합 아키텍처 개선 계획

**작성일**: 2025-11-08
**버전**: 1.0
**상태**: Planning → Implementation

**프로젝트 환경**:
- Unity 6000.0.23f1 (Unity 6)
- .NET Standard 2.1 (Unity 호환)
- C# 9.0

---

## 목차

1. [개요](#1-개요)
2. [현재 상태 평가](#2-현재-상태-평가)
3. [목표 아키텍처](#3-목표-아키텍처)
4. [Phase 1: 기반 재편성 (우선순위: P0)](#phase-1-기반-재편성)
5. [Phase 2: 핵심 기능 완성 (우선순위: P1)](#phase-2-핵심-기능-완성)
6. [Phase 3: 고급 기능 구현 (우선순위: P2)](#phase-3-고급-기능-구현)
7. [Phase 4: 최적화 및 완성도 (우선순위: P3)](#phase-4-최적화-및-완성도)

---

## 1. 개요

### 1.1 목적

HELLO_MY_WORLD 프로젝트의 아키텍처를 체계적으로 개선하여:
- ✅ 서버-클라이언트 코드 중복 제거
- ✅ 설정 외부화 및 통합
- ✅ 유지보수성 향상
- ✅ 누락된 마인크래프트 핵심 기능 구현

### 1.2 현재 성숙도

| 항목 | 점수 | 등급 |
|------|------|------|
| 전체 평가 | 4.9/10 | 알파 단계 |
| 블록 시스템 | 9/10 | 우수 |
| 월드 생성 | 8/10 | 우수 |
| 인벤토리 | 8/10 | 양호 |
| 크래프팅 | 5/10 | 기본 |
| 몹 AI | 2/10 | 미흡 |
| 레드스톤 | 0/10 | 미구현 |

### 1.3 핵심 문제점

1. **코드 중복** (Critical)
   - BlockType 중복: 서버 vs 클라이언트
   - 월드 생성 로직 중복
   - 수학 라이브러리 3중 정의

2. **하드코딩** (Critical)
   - 50+ 개의 매직 넘버
   - 설정 파일 분산 (4개 위치)

3. **레거시 부담** (High)
   - KojeomNetWorkSpace 13MB (사용 안 함)
   - P2P 코드 잔재

4. **기능 미완성** (High)
   - 몹 AI (방랑만 가능)
   - 물리 시스템 (블록 중력 없음)
   - 레드스톤 완전 미구현

---

## 2. 현재 상태 평가

### 2.1 프로젝트 구조

```
HELLO_MY_WORLD/
├── GameServer/              # .NET 6.0 서버
│   ├── Program.cs
│   ├── GameServer.cs
│   ├── Handlers/            # 메시지 핸들러
│   ├── Systems/             # 게임 시스템
│   ├── World/               # 월드 관리
│   ├── Synchronization/     # 동기화 (NEW)
│   └── Utils/               # 유틸리티 (NEW)
├── Assets/                  # Unity 클라이언트
│   └── MyAssets/Scripts/
│       ├── GameWorld/
│       ├── Player/
│       ├── AI/
│       └── Network/
├── SharedProtocol/          # 공유 프로토콜
├── KojeomNetWorkSpace/      # ❌ 레거시 (P2P)
├── MapGeneratorLib/         # 맵 생성 (독립 DLL)
├── proto/                   # 프로토버프 정의
└── server-config.json       # 서버 설정
```

### 2.2 의존성 문제

**현재 (문제 있음)**:
```
GameServer ──→ SharedProtocol ──→ Assets/Generated/Protobuf
   ↓                                        ↑
   └────────────────────────────────────────┘
```

**개선 후 (목표)**:
```
proto/*.proto ──(protoc)──→ Generated/
                                ↓
GameServer.Core ←──────── GameCommon.dll
                                ↑
Unity Client ←──────────────────┘
```

### 2.3 설정 분산 문제

**현재**:
1. `/server-config.json` - 서버 설정
2. `/Config/ServerConfig.json` - 중복?
3. `/Assets/.../GameConfigData.json` - 클라이언트 설정
4. `/Assets/.../WorldConfigData.json` - 월드 설정
5. `/GameServer/World/WorldManager.cs` - 하드코딩 50+ 상수

**문제**:
- 서버와 클라이언트 설정 불일치 가능
- 런타임 조정 불가
- 테스트 어려움

---

## 3. 목표 아키텍처

### 3.1 새로운 프로젝트 구조

```
HELLO_MY_WORLD/
├── GameCommon/                  # ✨ NEW: 공유 라이브러리
│   ├── BlockDefinitions.cs      # 통합 블록 타입
│   ├── WorldConstants.cs        # 설정 가능한 상수
│   ├── MathUtils.cs             # 공통 수학
│   ├── NoiseGenerator.cs        # 노이즈 생성
│   ├── GameplayRules.cs         # 게임플레이 규칙
│   └── GameCommon.csproj        # .NET Standard 2.1
│
├── GameServer.Core/             # ✨ NEW: 서버 코어
│   ├── CoreEngine.cs            # 서버 엔진
│   ├── TickScheduler.cs         # 틱 스케줄러
│   ├── World/                   # 월드 관리
│   ├── Entity/                  # 엔티티 시뮬레이션
│   ├── Physics/                 # 물리 시뮬레이션
│   └── GameServer.Core.csproj
│
├── GameServer.Launcher/         # ✨ NEW: 런처
│   ├── Program.cs               # GUI 또는 CLI 런처
│   ├── ServerMonitor.cs         # 서버 모니터링
│   ├── ConfigEditor.cs          # 설정 편집
│   └── GameServer.Launcher.csproj
│
├── GameServer/                  # ✅ REFACTOR: 슬림화
│   ├── Handlers/
│   ├── Database/
│   └── GameServer.csproj → Core 참조
│
├── Assets/                      # ✅ REFACTOR
│   ├── Plugins/
│   │   ├── GameCommon.dll       # 공유 라이브러리
│   │   └── MapGeneratorLib.dll
│   └── MyAssets/Scripts/
│
├── config/                      # ✨ NEW: 통합 설정
│   ├── server.json              # 서버 설정
│   ├── world.json               # 월드 설정
│   ├── gameplay.json            # 게임플레이 설정
│   └── blocks.json              # 블록 정의
│
└── [레거시 제거]
    ├── ❌ KojeomNetWorkSpace/
    └── ❌ Assets/.../Network/p2p/
```

### 3.2 공통 라이브러리 (GameCommon)

**목적**: 서버와 클라이언트가 공유하는 로직을 DLL로 분리

**포함 내용**:

```csharp
// GameCommon/BlockDefinitions.cs
namespace GameCommon.Blocks
{
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        // 통합된 정의
    }

    public class BlockProperties
    {
        public BlockType Type { get; set; }
        public string Name { get; set; }
        public float Hardness { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsFluid { get; set; }
        public bool AffectedByGravity { get; set; }

        public static BlockProperties Get(BlockType type);
    }
}
```

```csharp
// GameCommon/WorldConstants.cs
namespace GameCommon.World
{
    public class WorldConstants
    {
        // JSON에서 로드
        public static int GlobalWaterLevel { get; set; } = 62;
        public static double RiverCenterThreshold { get; set; } = 0.0125;
        public static int MinSurfaceHeight { get; set; } = 45;
        public static int MaxSurfaceHeight { get; set; } = 150;

        public static void LoadFromConfig(string configPath);
    }
}
```

```csharp
// GameCommon/MathUtils.cs
namespace GameCommon.Math
{
    // 플랫폼 독립적 Vector 구현
    public struct Vector3
    {
        public double X, Y, Z;

        public static implicit operator System.Numerics.Vector3(Vector3 v);
        public static implicit operator UnityEngine.Vector3(Vector3 v);
    }
}
```

### 3.3 통합 설정 시스템

**설계 원칙**:
1. **단일 진실 공급원** (Single Source of Truth)
2. **계층적 구조** (네임스페이스별 분리)
3. **검증 가능** (스키마 검증)
4. **핫 리로드** (런타임 변경 감지)

**파일 구조**:

```
config/
├── server.json          # 서버 전용 설정
├── world.json           # 월드 생성 설정
├── gameplay.json        # 게임플레이 규칙
├── blocks.json          # 블록 속성 정의
├── entities.json        # 엔티티 정의
├── recipes.json         # 크래프팅 레시피
└── _schema.json         # JSON 스키마
```

**config/world.json 예시**:
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

**config/blocks.json 예시**:
```json
{
  "blocks": [
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "hardness": 1.5,
      "resistance": 6.0,
      "isTransparent": false,
      "isFluid": false,
      "affectedByGravity": false,
      "tool": "pickaxe",
      "toolLevel": 0,
      "drops": [
        {
          "item": "cobblestone",
          "chance": 1.0,
          "count": { "min": 1, "max": 1 }
        }
      ]
    },
    {
      "id": 12,
      "name": "sand",
      "displayName": "Sand",
      "hardness": 0.5,
      "affectedByGravity": true,
      "tool": "shovel"
    }
  ]
}
```

### 3.4 서버 런처 프로그램

**목적**:
- 서버 시작/중지/재시작
- 설정 편집 UI
- 실시간 모니터링
- 로그 뷰어

**구현 옵션**:

**Option A: CLI 런처** (빠른 구현)
```
┌─────────────────────────────────────┐
│  HELLO_MY_WORLD 서버 런처          │
├─────────────────────────────────────┤
│ [1] 서버 시작                       │
│ [2] 서버 중지                       │
│ [3] 설정 편집                       │
│ [4] 로그 보기                       │
│ [5] 플레이어 목록                   │
│ [6] 통계                            │
│ [Q] 종료                            │
├─────────────────────────────────────┤
│ 상태: ● 실행 중                     │
│ 플레이어: 5/20                      │
│ 메모리: 512MB / 2GB                 │
│ 업타임: 2시간 34분                  │
└─────────────────────────────────────┘
```

**Option B: GUI 런처** (장기)
- WPF 또는 Avalonia UI
- 대시보드
- 차트 (플레이어 수, 메모리, TPS)

**기능**:
```csharp
// GameServer.Launcher/Program.cs
public class ServerLauncher
{
    private Process? _serverProcess;
    private ConfigManager _configManager;
    private LogMonitor _logMonitor;

    public void StartServer()
    {
        _serverProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "GameServer.dll",
            WorkingDirectory = "./server",
            RedirectStandardOutput = true
        });

        _logMonitor.Watch(_serverProcess.StandardOutput);
    }

    public void EditConfig(string category)
    {
        var config = _configManager.Load(category);
        // 인터랙티브 편집
        _configManager.Save(category, config);

        // 핫 리로드 신호
        SendReloadSignal();
    }

    public ServerMetrics GetMetrics()
    {
        // 서버 프로세스에서 메트릭 수집
    }
}
```

---

## Phase 1: 기반 재편성

**기간**: 2주
**우선순위**: P0 (Critical)

### Task 1-1: GameCommon 라이브러리 생성

**목표**: 서버-클라이언트 공유 코드를 독립 DLL로 분리

**작업 내용**:

1. **프로젝트 생성**
```bash
cd /home/user/HELLO_MY_WORLD
dotnet new classlib -n GameCommon -f netstandard2.1
```

2. **블록 정의 통합**
```csharp
// GameCommon/Blocks/BlockType.cs
public enum BlockType
{
    Air = 0,
    Stone = 1,
    Grass = 2,
    Dirt = 3,
    Cobblestone = 4,
    Wood = 5,
    Sand = 12,
    Gravel = 13,
    Water = 8,
    StationaryWater = 9,
    // ... 전체 블록
}

// GameCommon/Blocks/BlockRegistry.cs
public class BlockRegistry
{
    private static Dictionary<BlockType, BlockProperties> _blocks;

    public static void LoadFromJson(string path)
    {
        // config/blocks.json 로드
    }

    public static BlockProperties Get(BlockType type);
}
```

3. **설정 시스템**
```csharp
// GameCommon/Configuration/ConfigManager.cs
public class ConfigManager
{
    public WorldConfig World { get; private set; }
    public GameplayConfig Gameplay { get; private set; }

    public void LoadAll(string configDir)
    {
        World = JsonSerializer.Deserialize<WorldConfig>(
            File.ReadAllText($"{configDir}/world.json")
        );
        // ...
    }

    public void Reload(string category);
    public event Action<string> OnConfigChanged;
}
```

4. **공통 수학**
```csharp
// GameCommon/Math/Vector3.cs
public struct Vector3
{
    public double X, Y, Z;

    // 변환 연산자
    public static implicit operator System.Numerics.Vector3(Vector3 v)
        => new((float)v.X, (float)v.Y, (float)v.Z);
}

// GameCommon/Math/NoiseGenerator.cs
public class NoiseGenerator
{
    private readonly int _seed;

    public float Get3D(double x, double y, double z);
    public float Get2D(double x, double z);
}
```

**체크리스트**:
- [ ] GameCommon.csproj 생성
- [ ] BlockType enum 통합
- [ ] BlockProperties 클래스
- [ ] ConfigManager 구현
- [ ] Vector3 구현
- [ ] NoiseGenerator 이전
- [ ] NuGet 패키지 빌드

### Task 1-2: 통합 설정 파일 시스템

**목표**: 모든 하드코딩된 상수를 JSON으로 이동

**작업 내용**:

1. **설정 파일 생성**
```bash
mkdir -p config
touch config/server.json
touch config/world.json
touch config/gameplay.json
touch config/blocks.json
touch config/entities.json
touch config/recipes.json
```

2. **월드 상수 이동**

**Before (WorldManager.cs)**:
```csharp
private const int GlobalWaterLevel = 62;
private const double RiverCenterThreshold = 0.0125;
```

**After (config/world.json)**:
```json
{
  "generation": {
    "water": {
      "globalLevel": 62,
      "riverCenterThreshold": 0.0125
    }
  }
}
```

**After (WorldManager.cs)**:
```csharp
public WorldManager(ConfigManager config)
{
    _waterLevel = config.World.Generation.Water.GlobalLevel;
    _riverThreshold = config.World.Generation.Water.RiverCenterThreshold;
}
```

3. **블록 속성 이동**

**Before (하드코딩)**:
```csharp
switch (blockType)
{
    case BlockType.Stone:
        hardness = 1.5f;
        break;
    case BlockType.Dirt:
        hardness = 0.5f;
        break;
}
```

**After (config/blocks.json + BlockRegistry)**:
```csharp
var props = BlockRegistry.Get(blockType);
float hardness = props.Hardness;
```

**체크리스트**:
- [ ] config/ 디렉토리 생성
- [ ] world.json 작성
- [ ] blocks.json 작성 (전체 블록)
- [ ] gameplay.json 작성
- [ ] entities.json 작성
- [ ] recipes.json 작성
- [ ] server.json 업데이트
- [ ] WorldManager 리팩터링
- [ ] BlockRegistry 구현
- [ ] 테스트 (기존 기능 동작 확인)

### Task 1-3: 서버 구조 재편성

**목표**: GameServer를 Core와 Launcher로 분리

**새로운 구조**:
```
GameServer.Core/             # 비즈니스 로직
├── CoreEngine.cs
├── TickScheduler.cs
├── World/
├── Entity/
└── Physics/

GameServer.Launcher/         # 진입점 및 관리
├── Program.cs
├── ServerMonitor.cs
└── ConfigEditor.cs

GameServer/                  # 네트워크 계층
├── Handlers/
├── SessionManager.cs
└── GameServer.cs
```

**작업 내용**:

1. **GameServer.Core 프로젝트 생성**
```bash
dotnet new classlib -n GameServer.Core -f net6.0
dotnet add GameServer.Core reference GameCommon
```

2. **코어 엔진 구현**
```csharp
// GameServer.Core/CoreEngine.cs
public class CoreEngine
{
    private readonly ConfigManager _config;
    private readonly TickScheduler _tickScheduler;
    private readonly WorldManager _worldManager;
    private readonly EntityManager _entityManager;
    private readonly PhysicsEngine _physicsEngine;

    public void Initialize()
    {
        _config.LoadAll("./config");
        BlockRegistry.LoadFromJson("./config/blocks.json");

        _worldManager = new WorldManager(_config);
        _entityManager = new EntityManager();
        _physicsEngine = new PhysicsEngine();

        _tickScheduler.Start(TickRate.TwentyPerSecond);
    }

    public void Tick()
    {
        _entityManager.Tick();
        _physicsEngine.Tick();
        _worldManager.Tick();
    }
}
```

3. **틱 스케줄러**
```csharp
// GameServer.Core/TickScheduler.cs
public class TickScheduler
{
    private Timer? _timer;
    private readonly int _tickRateMs;

    public event Action OnTick;

    public void Start(TickRate rate)
    {
        _tickRateMs = rate == TickRate.TwentyPerSecond ? 50 : 100;
        _timer = new Timer(_ => OnTick?.Invoke(), null, 0, _tickRateMs);
    }
}

public enum TickRate
{
    TenPerSecond,
    TwentyPerSecond
}
```

4. **GameServer.Launcher 프로젝트**
```bash
dotnet new console -n GameServer.Launcher -f net6.0
dotnet add GameServer.Launcher reference GameServer.Core
dotnet add GameServer.Launcher reference GameServer
```

```csharp
// GameServer.Launcher/Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var launcher = new ServerLauncher();

        if (args.Contains("--gui"))
        {
            launcher.ShowGUI();
        }
        else
        {
            launcher.ShowCLI();
        }
    }
}

// GameServer.Launcher/ServerLauncher.cs
public class ServerLauncher
{
    private CoreEngine? _engine;
    private GameServer? _server;

    public void ShowCLI()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("┌─────────────────────────────────────┐");
            Console.WriteLine("│  HELLO_MY_WORLD 서버 런처          │");
            Console.WriteLine("├─────────────────────────────────────┤");
            Console.WriteLine("│ [1] 서버 시작                       │");
            Console.WriteLine("│ [2] 서버 중지                       │");
            Console.WriteLine("│ [3] 설정 편집                       │");
            Console.WriteLine("│ [4] 로그 보기                       │");
            Console.WriteLine("│ [Q] 종료                            │");
            Console.WriteLine("└─────────────────────────────────────┘");

            var choice = Console.ReadKey();

            switch (choice.KeyChar)
            {
                case '1':
                    StartServer();
                    break;
                case '2':
                    StopServer();
                    break;
                case '3':
                    EditConfig();
                    break;
                case 'q':
                case 'Q':
                    return;
            }
        }
    }

    private void StartServer()
    {
        _engine = new CoreEngine();
        _engine.Initialize();

        _server = new GameServer();
        _server.Start();

        Console.WriteLine("✓ 서버 시작됨");
        Console.ReadKey();
    }
}
```

**체크리스트**:
- [ ] GameServer.Core 프로젝트 생성
- [ ] CoreEngine 구현
- [ ] TickScheduler 구현
- [ ] GameServer.Launcher 프로젝트 생성
- [ ] ServerLauncher CLI 구현
- [ ] ConfigEditor 구현
- [ ] LogMonitor 구현
- [ ] 기존 GameServer 리팩터링
- [ ] 빌드 스크립트 작성

### Task 1-4: 레거시 코드 제거

**목표**: 사용하지 않는 13MB P2P 코드 제거

**삭제 대상**:
```
❌ /KojeomNetWorkSpace/HMWGameServer/
❌ /KojeomNetWorkSpace/KojeomNet/PeerToPeerNetwork.cs
❌ /Assets/MyAssets/Scripts/Network/p2p/
❌ /GameServer/Handlers/Disabled/
```

**보존 (분석 후 판단)**:
```
⚠️  /KojeomNetWorkSpace/KojeomNet/FrameWork/
    (TCP 기반 코드는 보존 가능)
```

**작업 내용**:

1. **의존성 분석**
```bash
# P2P 코드 사용 여부 확인
grep -r "PeerToPeerNetwork" /home/user/HELLO_MY_WORLD/GameServer
grep -r "PeerToPeerNetwork" /home/user/HELLO_MY_WORLD/Assets
```

2. **안전 제거**
```bash
# 백업
git checkout -b feature/remove-legacy-p2p

# 제거
rm -rf KojeomNetWorkSpace/HMWGameServer
rm -rf Assets/MyAssets/Scripts/Network/p2p
rm -rf GameServer/Handlers/Disabled

# 커밋
git add -A
git commit -m "chore: remove legacy P2P code (13MB)"
```

3. **.gitignore 업데이트**
```
# 레거시 경로 추가
**/p2p/
**/Disabled/
```

**체크리스트**:
- [ ] 의존성 분석 완료
- [ ] HMWGameServer 제거
- [ ] p2p/ 디렉토리 제거
- [ ] Handlers/Disabled/ 제거
- [ ] 빌드 테스트
- [ ] 커밋

---

## Phase 2: 핵심 기능 완성

**기간**: 4주
**우선순위**: P1 (High)

### Task 2-1: 몹 AI 시스템 구현

**목표**: 적대적 몹 AI를 서버 측에서 시뮬레이션

**현재 상태**: 방랑만 가능

**목표 상태**:
- ✅ Zombie, Skeleton, Creeper AI
- ✅ 타겟팅 (플레이어 감지)
- ✅ 경로 탐색 (A* 3D)
- ✅ 공격 패턴
- ✅ 서버-클라이언트 동기화

**설계**:

```csharp
// GameServer.Core/Entity/AI/AIController.cs
public abstract class AIController
{
    protected Entity Entity { get; }
    protected AIState CurrentState { get; set; }

    public virtual void Tick()
    {
        CurrentState = DetermineState();

        switch (CurrentState)
        {
            case AIState.Idle:
                TickIdle();
                break;
            case AIState.Wander:
                TickWander();
                break;
            case AIState.Chase:
                TickChase();
                break;
            case AIState.Attack:
                TickAttack();
                break;
        }
    }

    protected abstract AIState DetermineState();
    protected abstract void TickChase();
    protected abstract void TickAttack();
}

// GameServer.Core/Entity/AI/ZombieAI.cs
public class ZombieAI : AIController
{
    private const float DetectionRange = 16.0f;
    private const float AttackRange = 2.0f;
    private const float Speed = 2.5f;

    protected override AIState DetermineState()
    {
        var nearestPlayer = FindNearestPlayer(DetectionRange);

        if (nearestPlayer == null)
            return AIState.Wander;

        float distance = Vector3.Distance(Entity.Position, nearestPlayer.Position);

        if (distance <= AttackRange)
            return AIState.Attack;

        return AIState.Chase;
    }

    protected override void TickChase()
    {
        var target = FindNearestPlayer(DetectionRange);
        var path = Pathfinder.FindPath(Entity.Position, target.Position);

        if (path.Count > 0)
        {
            var nextPos = path[0];
            Entity.MoveTo(nextPos, Speed);
        }
    }

    protected override void TickAttack()
    {
        var target = FindNearestPlayer(AttackRange);

        if (CanAttack())
        {
            DealDamage(target, Damage);
            _lastAttackTime = DateTime.UtcNow;
        }
    }
}
```

**경로 탐색**:
```csharp
// GameServer.Core/Entity/AI/Pathfinder.cs
public class Pathfinder
{
    private readonly WorldManager _world;

    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        // A* 알고리즘 (복셀 공간)
        var openSet = new PriorityQueue<Node>();
        var closedSet = new HashSet<Vector3Int>();

        openSet.Enqueue(new Node(start, 0, Heuristic(start, end)));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (Vector3.Distance(current.Position, end) < 1.0f)
            {
                return ReconstructPath(current);
            }

            closedSet.Add(ToInt(current.Position));

            foreach (var neighbor in GetNeighbors(current.Position))
            {
                if (closedSet.Contains(ToInt(neighbor)))
                    continue;

                if (_world.IsBlockSolid(neighbor))
                    continue;

                var cost = current.GCost + 1;
                var node = new Node(neighbor, cost, Heuristic(neighbor, end));
                node.Parent = current;

                openSet.Enqueue(node);
            }
        }

        return new List<Vector3>();
    }
}
```

**체크리스트**:
- [ ] AIController 베이스 클래스
- [ ] ZombieAI 구현
- [ ] SkeletonAI 구현 (원거리 공격)
- [ ] CreeperAI 구현 (폭발)
- [ ] Pathfinder A* 구현
- [ ] EntityManager 통합
- [ ] 클라이언트 동기화 (엔티티 위치)
- [ ] 테스트 (봇 스포닝)

### Task 2-2: 블록 물리 시스템

**목표**: 모래/자갈 중력, 물/용암 흐름

**설계**:

```csharp
// GameServer.Core/Physics/PhysicsEngine.cs
public class PhysicsEngine
{
    private readonly WorldManager _world;
    private Queue<BlockPhysicsUpdate> _updateQueue;

    public void Tick()
    {
        ProcessGravity();
        ProcessFluids();
    }

    private void ProcessGravity()
    {
        // 모래/자갈 블록 탐색
        foreach (var chunk in _world.ActiveChunks)
        {
            for (int x = 0; x < 16; x++)
            for (int y = 1; y < 256; y++)
            for (int z = 0; z < 16; z++)
            {
                var block = chunk.GetBlock(x, y, z);

                if (BlockRegistry.Get(block).AffectedByGravity)
                {
                    CheckGravity(chunk, x, y, z);
                }
            }
        }
    }

    private void CheckGravity(Chunk chunk, int x, int y, int z)
    {
        var blockBelow = chunk.GetBlock(x, y - 1, z);

        if (blockBelow == BlockType.Air || BlockRegistry.Get(blockBelow).IsFluid)
        {
            // 낙하
            _updateQueue.Enqueue(new BlockPhysicsUpdate
            {
                OldPos = new Vector3Int(x, y, z),
                NewPos = new Vector3Int(x, y - 1, z),
                BlockType = chunk.GetBlock(x, y, z)
            });
        }
    }

    private void ProcessFluids()
    {
        // 물/용암 흐름 시뮬레이션
        foreach (var chunk in _world.ActiveChunks)
        {
            for (int x = 0; x < 16; x++)
            for (int y = 0; y < 256; y++)
            for (int z = 0; z < 16; z++)
            {
                var block = chunk.GetBlock(x, y, z);

                if (BlockRegistry.Get(block).IsFluid)
                {
                    SimulateFluidFlow(chunk, x, y, z);
                }
            }
        }
    }
}
```

**체크리스트**:
- [ ] PhysicsEngine 클래스
- [ ] 중력 시뮬레이션
- [ ] 물 흐름 시뮬레이션
- [ ] 용암 흐름 시뮬레이션
- [ ] 블록 업데이트 큐
- [ ] 클라이언트 동기화
- [ ] 성능 최적화 (청크당 틱)
- [ ] 테스트

### Task 2-3: 크래프팅 고도화

**현재**: 간단한 레시피만 지원

**목표**:
- ✅ 3x3 그리드 패턴 매칭
- ✅ 레시피 북 시스템
- ✅ 제련 진행 UI

**설계**:

```csharp
// GameCommon/Crafting/CraftingRecipe.cs
public class CraftingRecipe
{
    public string Id { get; set; }
    public CraftingType Type { get; set; }
    public ItemPattern Pattern { get; set; }
    public ItemStack[] Results { get; set; }

    public bool Matches(ItemStack[] input)
    {
        if (Type == CraftingType.Shapeless)
        {
            return MatchesShapeless(input);
        }
        else
        {
            return MatchesShaped(input);
        }
    }

    private bool MatchesShaped(ItemStack[] input)
    {
        // 3x3 그리드 패턴 매칭
        for (int i = 0; i < 9; i++)
        {
            if (!Pattern.Grid[i].Matches(input[i]))
                return false;
        }
        return true;
    }
}

// config/recipes.json
{
  "recipes": [
    {
      "id": "wooden_pickaxe",
      "type": "shaped",
      "pattern": [
        "WWW",
        " S ",
        " S "
      ],
      "key": {
        "W": "minecraft:planks",
        "S": "minecraft:stick"
      },
      "result": {
        "item": "minecraft:wooden_pickaxe",
        "count": 1
      }
    }
  ]
}
```

**체크리스트**:
- [ ] CraftingRecipe 클래스
- [ ] PatternMatcher 구현
- [ ] recipes.json 작성 (전체 레시피)
- [ ] RecipeRegistry 구현
- [ ] 레시피 북 시스템
- [ ] 제련 진행 시스템
- [ ] 클라이언트 UI 개선
- [ ] 테스트

---

## Phase 3: 고급 기능 구현

**기간**: 8주
**우선순위**: P2 (Medium)

### Task 3-1: 레드스톤 시스템

**목표**: 기본 레드스톤 회로 구현

**설계**:

```csharp
// GameServer.Core/Redstone/RedstoneEngine.cs
public class RedstoneEngine
{
    private Dictionary<Vector3Int, RedstoneComponent> _components;

    public void Tick()
    {
        // 전원 공급 블록 업데이트
        UpdatePowerSources();

        // 신호 전파
        PropagateSignals();

        // 출력 블록 활성화
        ActivateOutputs();
    }

    private void PropagateSignals()
    {
        // BFS로 신호 전파
        var queue = new Queue<RedstoneComponent>();

        foreach (var source in _components.Values.Where(c => c.IsPowerSource))
        {
            queue.Enqueue(source);
        }

        while (queue.Count > 0)
        {
            var component = queue.Dequeue();

            foreach (var neighbor in GetNeighbors(component.Position))
            {
                if (CanPropagateTo(neighbor))
                {
                    neighbor.PowerLevel = Math.Max(0, component.PowerLevel - 1);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
}
```

**체크리스트**:
- [ ] RedstoneEngine 클래스
- [ ] 레드스톤 와이어
- [ ] 레드스톤 토치
- [ ] 리피터/컴퍼레이터
- [ ] 레버/버튼/압력판
- [ ] 피스톤/디스펜서
- [ ] 신호 전파 알고리즘
- [ ] 테스트

### Task 3-2: 인챈트 시스템

(상세 생략, 유사 구조)

### Task 3-3: 포션 시스템

(상세 생략)

---

## Phase 4: 최적화 및 완성도

**기간**: 4주
**우선순위**: P3 (Low)

### Task 4-1: 성능 최적화

- 청크 생성 병렬화
- 엔티티 공간 분할 (Octree)
- 네트워크 패킷 압축
- 메모리 풀링

### Task 4-2: 테스트 커버리지

- 단위 테스트 (xUnit)
- 통합 테스트
- 부하 테스트 (100+ 플레이어)

### Task 4-3: 문서화

- API 문서 (Doxygen)
- 플레이어 가이드
- 모드 제작 가이드

---

## 4. 마일스톤

| Phase | 완료 예정 | 주요 결과물 |
|-------|----------|------------|
| Phase 1 | +2주 | GameCommon DLL, 통합 설정, 런처 |
| Phase 2 | +6주 | 몹 AI, 블록 물리, 크래프팅 완성 |
| Phase 3 | +14주 | 레드스톤, 인챈트, 포션 |
| Phase 4 | +18주 | 최적화, 테스트, 문서 |

---

## 5. 참고 자료

- `docs/PROJECT_REVIEW_REPORT.md` - 프로젝트 분석
- `docs/CRITICAL_IMPROVEMENTS.md` - Critical 이슈 해결
- `docs/SYNCHRONIZATION_ARCHITECTURE.md` - 동기화 아키텍처

---

**버전 히스토리**:
- v1.0 (2025-11-08): 초안 작성
