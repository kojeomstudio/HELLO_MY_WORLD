# GameServer.Launcher

게임 서버를 실행하고 관리하는 독립 런처 프로그램입니다.

## 개요

GameServer.Launcher는 HELLO_MY_WORLD 게임 서버를 쉽게 시작, 중지, 관리할 수 있는 명령줄 도구입니다.

### 주요 기능

- ✅ 서버 시작/중지
- ✅ 대화형 메뉴 인터페이스
- ✅ 명령줄 인자 지원
- ✅ 설정 파일 관리
- ✅ GameCommon 라이브러리 통합 준비
- ✅ 룸 기반 아키텍처 정보 표시
- ⏳ 자동 재시작 (향후 구현)
- ⏳ 실시간 모니터링 (향후 구현)

## 빌드

```bash
cd GameServer.Launcher
dotnet build
dotnet run
```

또는 루트 디렉토리에서:

```bash
dotnet build GameServer.Launcher/GameServer.Launcher.csproj
dotnet run --project GameServer.Launcher/GameServer.Launcher.csproj
```

## 사용 방법

### 1. 대화형 모드 (기본)

```bash
./GameServerLauncher
```

메뉴가 표시되며 다음 옵션을 선택할 수 있습니다:
- 1: 서버 시작
- 2: 서버 중지
- 3: 서버 상태 확인
- 4: 설정 보기
- 5: GameCommon 정보
- 6: 룸 아키텍처 정보
- 0: 종료

### 2. 명령줄 모드

#### 서버 시작

```bash
./GameServerLauncher start
# 또는
./GameServerLauncher --start
./GameServerLauncher -s
```

#### 서버 상태 확인

```bash
./GameServerLauncher status
# 또는
./GameServerLauncher --status
```

#### 도움말

```bash
./GameServerLauncher help
# 또는
./GameServerLauncher --help
./GameServerLauncher -h
```

## 설정 파일

### launcher-config.json

런처 자체 설정 파일입니다.

```json
{
  "serverConfigPath": "server-config.json",
  "loadGameCommonConfig": false,
  "gameCommonConfigPath": "config",
  "waitForExit": true,
  "autoRestart": false,
  "restartDelaySeconds": 5,
  "logLevel": "Information"
}
```

**주요 설정**:

| 키 | 설명 | 기본값 |
|---|---|---|
| `serverConfigPath` | GameServer 설정 파일 경로 | `server-config.json` |
| `loadGameCommonConfig` | GameCommon 설정 로드 여부 | `false` (향후 `true`) |
| `gameCommonConfigPath` | GameCommon config 디렉토리 | `config` |
| `waitForExit` | 서버 종료까지 대기 여부 | `true` |
| `autoRestart` | 자동 재시작 여부 | `false` |
| `logLevel` | 로그 레벨 | `Information` |

### server-config.json

GameServer 자체 설정 파일입니다. 자세한 내용은 [GameServer README](../GameServer/README.md)를 참조하세요.

## 프로젝트 구조

```
GameServer.Launcher/
├── GameServer.Launcher.csproj    # 프로젝트 파일
├── Program.cs                     # 메인 진입점
├── LauncherConfig.cs              # 설정 클래스
├── launcher-config.json           # 설정 파일
└── README.md                      # 이 문서
```

## 의존성

### 프로젝트 참조

- **GameServer**: 게임 서버 핵심 로직
- **GameCommon**: 서버-클라이언트 공통 라이브러리

### NuGet 패키지

GameServer 프로젝트를 통해 간접 참조됩니다.

## 배포

### Release 빌드

```bash
dotnet publish GameServer.Launcher/GameServer.Launcher.csproj \
  --configuration Release \
  --output ./publish
```

### 단일 실행 파일 생성 (Self-Contained)

```bash
dotnet publish GameServer.Launcher/GameServer.Launcher.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./publish/linux-x64

dotnet publish GameServer.Launcher/GameServer.Launcher.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64
```

### Docker 배포 (향후)

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY publish/ /app
WORKDIR /app
ENTRYPOINT ["./GameServerLauncher", "start"]
```

## GameCommon 통합

현재 GameCommon 라이브러리는 참조되어 있지만 아직 완전히 통합되지 않았습니다.

### 향후 통합 시 변경사항

1. **launcher-config.json** 수정:
   ```json
   {
     "loadGameCommonConfig": true
   }
   ```

2. **서버 시작 시 자동 로드**:
   - `config/blocks.json` → BlockRegistry 로드
   - `config/world.json` → WorldConfig 로드
   - `config/gameplay.json` → GameplayConfig 로드

3. **설정 통합**:
   - `server-config.json`의 일부 설정이 `config/` 파일들로 이동
   - 중복 제거 및 단일 소스 유지

자세한 내용은 [구현 가이드](../docs/IMPLEMENTATION_GUIDE.md)를 참조하세요.

## 문제 해결

### 포트 이미 사용 중

```
[ERROR] Failed to start server: Address already in use
```

**해결**: `server-config.json`에서 다른 포트 번호로 변경하거나, 기존 서버 프로세스를 종료하세요.

```bash
# 포트 사용 확인 (Linux/macOS)
lsof -i :25565

# 포트 사용 확인 (Windows)
netstat -ano | findstr :25565
```

### 설정 파일 없음

```
[CONFIG] Launcher config not found at launcher-config.json, creating default...
```

**정상**: 처음 실행 시 자동으로 기본 설정 파일이 생성됩니다.

### GameCommon 설정 로드 실패

```
[WARNING] GameCommon config failed: ...
```

**원인**: `config/` 디렉토리 또는 JSON 파일이 없거나 형식이 잘못됨

**해결**:
1. `config/` 디렉토리 확인
2. JSON 파일 유효성 검사
3. 또는 `loadGameCommonConfig: false`로 설정 (현재 기본값)

## 로깅

### 로그 레벨

`launcher-config.json`에서 설정:

```json
{
  "logLevel": "Information"
}
```

**사용 가능한 레벨**:
- `Trace`: 모든 상세 정보
- `Debug`: 디버깅 정보
- `Information`: 일반 정보 (기본값)
- `Warning`: 경고
- `Error`: 에러
- `Critical`: 치명적 오류

### 콘솔 컬러 출력

기본적으로 활성화되어 있습니다. 비활성화하려면:

```json
{
  "enableColorOutput": false
}
```

## 성능 모니터링 (향후)

향후 버전에서 다음 기능이 추가될 예정입니다:

- 📊 실시간 플레이어 수
- 📊 활성 룸 수
- 📊 메모리 사용량
- 📊 네트워크 트래픽
- 📊 청크 로드/언로드 통계

## 관련 문서

- [GameServer README](../GameServer/README.md) - 게임 서버 핵심 로직
- [GameCommon 구현 가이드](../docs/IMPLEMENTATION_GUIDE.md) - GameCommon 통합 방법
- [룸 기반 아키텍처](../docs/ROOM_BASED_ARCHITECTURE.md) - 멀티플레이어 구조
- [Unity 호환성](../docs/UNITY_COMPATIBILITY.md) - Unity 6 통합

## 라이선스

HELLO_MY_WORLD 프로젝트와 동일한 라이선스를 따릅니다.

## 기여

GameServer.Launcher에 대한 개선 사항이나 버그 리포트는 GitHub Issues를 통해 제출해주세요.

---

**버전**: 1.0.0
**작성일**: 2025-11-08
**대상 프레임워크**: .NET 6.0
