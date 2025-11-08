# GameCommon Configuration Files

이 디렉토리의 파일들은 **GameCommon 라이브러리의 예시 설정 파일**입니다.

## 파일 목적

이 설정 파일들은 GameCommon 라이브러리(`GameCommon/Configuration/`)의 ConfigManager에서 사용하는 구조를 보여주는 **템플릿 및 예시**입니다.

### 파일 목록

| 파일 | 목적 | 사용 주체 |
|------|------|-----------|
| `blocks.json` | 블록 타입 정의 및 속성 (BlockRegistry) | GameServer, Unity Client (공통) |
| `world.json` | 지형 생성 파라미터 (WorldConfig) | GameServer (월드 생성) |
| `gameplay.json` | 게임플레이 규칙 및 메카닉 (GameplayConfig) | GameServer, Unity Client (공통) |
| `server.json` | 서버 운영 설정 예시 | **사용되지 않음** (예시만) |

## 실제 프로덕션 설정 파일

**중요**: 실제 게임 서버는 이 폴더의 파일들을 사용하지 **않습니다**!

### GameServer 설정

GameServer는 루트 디렉토리의 설정 파일을 사용합니다:

```
/home/user/HELLO_MY_WORLD/server-config.json
```

이 파일은 `GameServer/ServerConfig.cs`에서 로드되며 다음 항목을 포함합니다:
- Network 설정 (Host, Port, MaxPlayers 등)
- Database 설정 (ConnectionString 등)
- World 설정 (WorldSeed, ChunkSize 등)
- Gameplay 설정 (Difficulty, PhysicsEnabled 등)
- Security 설정 (EnableAntiCheat 등)
- Performance 설정 (MaxChunkLoadsPerTick 등)

### Unity Client 설정

Unity 클라이언트는 다음 위치의 설정 파일들을 사용합니다:

```
Assets/MyAssets/Resources/TextAsset/
├── GameConfigData.json          (UI 설정)
├── GameServerData.json          (서버 접속 정보)
├── GameWorld/WorldConfigData.json
├── ChDatas/characterDatas.json
├── ActorData/AnimalDatas.json
└── ... (기타 게임 데이터)
```

## GameCommon 통합 계획

향후 GameServer와 Unity Client가 GameCommon 라이브러리를 완전히 통합하면:

1. **GameServer**가 `config/` 폴더의 파일들을 사용하도록 리팩토링
2. **Unity Client**가 동일한 `config/` 파일들을 Assets/StreamingAssets에서 참조
3. `server-config.json`의 내용을 `config/server.json`으로 이전

이를 통해 서버와 클라이언트가 **동일한 블록/월드/게임플레이 설정**을 공유하게 됩니다.

## 사용 방법

### GameServer에서 ConfigManager 사용 (향후)

```csharp
using GameCommon.Configuration;
using GameCommon.Blocks;

// 설정 로드
ConfigManager.Instance.LoadAll("config");

// 월드 설정 사용
var worldConfig = ConfigManager.Instance.World;
Console.WriteLine($"Sea Level: {worldConfig.Water.GlobalWaterLevel}");

// 블록 레지스트리 로드
BlockRegistry.LoadFromJson("config/blocks.json");
var stoneProps = BlockRegistry.Get(BlockType.Stone);
Console.WriteLine($"Stone Hardness: {stoneProps.Hardness}");
```

### Unity Client에서 ConfigManager 사용 (향후)

```csharp
using GameCommon.Configuration;
using UnityEngine;

// Unity의 StreamingAssets 폴더에서 로드
string configPath = Application.streamingAssetsPath + "/config";
ConfigManager.Instance.LoadAll(configPath);

// 게임플레이 설정 사용
var gameplay = ConfigManager.Instance.Gameplay;
Debug.Log($"Gravity: {gameplay.Physics.Gravity}");
```

## 파일 구조 참고

각 파일의 자세한 구조는 다음 문서를 참조하세요:
- `docs/IMPLEMENTATION_GUIDE.md` - GameCommon 통합 가이드
- `docs/ARCHITECTURE_IMPROVEMENT_PLAN.md` - 설정 시스템 개선 계획
- `GameCommon/Configuration/` - 설정 클래스 정의

## 수정 시 주의사항

1. **JSON 유효성**: 모든 파일은 유효한 JSON 형식이어야 합니다
2. **타입 매칭**: C# 클래스 구조와 일치해야 합니다 (`GameCommon/Configuration/` 참조)
3. **버전 관리**: 설정 파일 변경 시 git으로 버전 관리
4. **백업**: 중요한 변경 전 백업 권장

## 문제 해결

### JSON 파싱 오류

```csharp
// ConfigManager가 자동으로 기본값으로 폴백합니다
ConfigManager.Instance.LoadAll("config");
// 콘솔에서 에러 메시지 확인
```

### 파일이 없을 때

ConfigManager는 파일이 없으면 자동으로 기본값을 생성하고 저장합니다.

## 관련 문서

- [Unity 6 호환성](../docs/UNITY_COMPATIBILITY.md)
- [구현 가이드](../docs/IMPLEMENTATION_GUIDE.md)
- [아키텍처 개선 계획](../docs/ARCHITECTURE_IMPROVEMENT_PLAN.md)
