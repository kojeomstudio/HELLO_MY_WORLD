# Server Build and Test Skill

당신은 HELLO_MY_WORLD 마인크래프트 서버를 빌드하고 테스트하는 전문가입니다.

## 프로젝트 구조

- **SharedProtocol**: 클라이언트-서버 공유 프로토콜 (.NET 6.0)
- **GameServer**: TCP 게임 서버 (.NET 6.0)
- **MapGeneratorLib**: 지형 생성 라이브러리 (.NET 4.5)

## 빌드 순서

1. **SharedProtocol 빌드** (먼저 빌드 필수)
   ```bash
   dotnet build /home/user/HELLO_MY_WORLD/SharedProtocol/SharedProtocol.csproj
   ```

2. **GameServer 빌드**
   ```bash
   dotnet build /home/user/HELLO_MY_WORLD/GameServer/GameServer.csproj
   ```

3. **MapGeneratorLib 빌드** (옵션)
   ```bash
   dotnet build /home/user/HELLO_MY_WORLD/MapGeneratorLib/MapGeneratorLib.sln
   ```

## 서버 실행

### 일반 서버 시작
```bash
cd /home/user/HELLO_MY_WORLD
dotnet run --project GameServer/GameServer.csproj -- --server
```

### 자가 테스트 실행
```bash
dotnet run --project GameServer/GameServer.csproj -- --selftest
```

### 테스트 클라이언트 실행
```bash
dotnet run --project GameServer/GameServer.csproj -- --test-client
```

## 서버 구성

서버 설정은 `/home/user/HELLO_MY_WORLD/server-config.json`에서 관리됩니다.

주요 설정:
- **Network.Port**: 기본 9000
- **Network.MaxConnections**: 기본 100
- **World.ChunkLoadRadius**: 기본 8
- **World.WorldSeed**: 지형 생성 시드
- **Database.DatabaseFile**: SQLite 데이터베이스 파일

## 컴파일 최적화

### Release 빌드
```bash
dotnet build -c Release /home/user/HELLO_MY_WORLD/GameServer/GameServer.csproj
```

### 경고를 오류로 처리
```bash
dotnet build -warnaserror /home/user/HELLO_MY_WORLD/GameServer/GameServer.csproj
```

## 테스트

현재 프로젝트에 단위 테스트가 없다면, 다음과 같이 추가할 수 있습니다:

```bash
# 테스트 프로젝트 생성
dotnet new xunit -o GameServer.Tests
cd GameServer.Tests
dotnet add reference ../GameServer/GameServer.csproj
```

## 주요 서버 컴포넌트

### 핸들러 시스템 (Handlers/)
- LoginHandler: 인증 및 세션 관리
- MinecraftChunkHandler: 청크 로드/언로드
- MinecraftPlayerActionHandler: 블록 파괴/배치
- WorldBlockHandler: 블록 변경 브로드캐스트
- ChatHandler: 채팅 시스템

### 시스템 컴포넌트 (Systems/)
- WorldTimeSystem: 주야 사이클
- WeatherSystem: 날씨 시스템
- InventorySystem: 인벤토리 관리
- ContainerSystem: 상자, 화로 등
- EntitySyncService: 엔티티 동기화

### 월드 시스템 (World/)
- WorldManager: 청크 및 지형 관리
- TerrainGenerationPipeline: 지형 생성 파이프라인
  - BaseTerrainStage
  - OreGenerationStage
  - CaveGenerationStage
  - RiverGenerationStage (catchment-weighted)
  - LakeGenerationStage (basin-aware)
  - VegetationGenerationStage

## 일반적인 문제 해결

### 빌드 오류
- **CS0246**: 타입을 찾을 수 없음 → SharedProtocol 먼저 빌드
- **CS1061**: 멤버 정의 없음 → 프로토버퍼 파일 재생성
- **CS0104**: 네임스페이스 충돌 → using 문 확인

### 런타임 오류
- **SQLite 오류**: Database 폴더 권한 확인
- **포트 바인딩 실패**: 9000번 포트 사용 중인지 확인
- **프로토버퍼 역직렬화 실패**: 클라이언트-서버 프로토콜 버전 확인

## 성능 모니터링

서버 상태 확인:
- ServerMetricsService를 통한 TPS 모니터링
- 청크 거주 통계
- 플레이어 통계 (사망/부활)
