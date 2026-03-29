# Unity Client Integration Skill

당신은 HELLO_MY_WORLD Unity 클라이언트를 통합하고 개선하는 전문가입니다.

## Unity 프로젝트 정보

- **Unity 버전**: 6000.0.23f1 (Unity 6.0)
- **타겟 프레임워크**: .NET Framework 4.5
- **프로젝트 경로**: `/home/user/HELLO_MY_WORLD/`

## 주요 클라이언트 컴포넌트

### 네트워킹 (Assets/MyAssets/Scripts/Network/)
- **GameNetworkManager.cs**: 서버 연결, 메시지 송수신
- **MultiPlayLobbyManager.cs**: 멀티플레이 로비

### 게임월드 (Assets/MyAssets/Scripts/GameWorld/)
- **청크 시스템**
  - AChunk.cs: 기본 청크 클래스
  - TerrainChunk.cs: 지형 메시 렌더링
  - WaterChunk.cs: 물 렌더링
  - EnviromentChunk.cs: 환경 객체 (나무, 풀)

- **월드 관리**
  - ModifyWorldManager.cs: 블록 변경
  - EnhancedModifyWorldManager.cs: 강화된 프로토버퍼 기반 블록 변경
  - WorldAreaManager.cs: 청크 로드/언로드

### 플레이어 (Assets/MyAssets/Scripts/Player/)
- 움직임, 카메라, 점프
- 블록 배치/파괴
- 인벤토리 상호작용

### UI (Assets/MyAssets/Scripts/UI/)
- **InGame/**
  - HUD (상태 표시, 채팅)
  - CombatFeedbackUI: 데미지 피드백
  - ContainerPanelUI: 상자, 화로
  - ServerStatusHUD: 청크 거주, 플레이어 수

- **InChSelect/**: 캐릭터 선택 화면

### AI 및 엔티티 (Assets/MyAssets/Scripts/MovableObjects/)
- NPC AI (경로찾기, 상호작용)
- 동물 AI (산책, 번식, 도주)
- RemoteEntityManager: 원격 엔티티 렌더링

## 프로토버퍼 통합

생성된 프로토버퍼 파일은 `Assets/Generated/Protobuf/`에 위치:
- EnhancedMinecraftGame.cs
- GameAuth.cs
- GameChat.cs
- GameCore.cs
- GameDiag.cs
- GameMove.cs
- GameWorld.cs

Unity 스크립트에서 사용:
```csharp
using EnhancedMinecraftProtocol;
using GameProtocol;
```

## 플러그인 관리

외부 라이브러리는 `Assets/Plugins/`에 배치:
- MapGeneratorLib.dll: 지형 생성 라이브러리
- SQLite 관련 DLL (필요시)

MapGeneratorLib 빌드 후 자동 복사:
```bash
dotnet build /home/user/HELLO_MY_WORLD/MapGeneratorLib/MapGeneratorLib.sln
# BuildEvent로 자동으로 Assets/Plugins에 복사됨
```

## Unity 패키지 의존성

주요 패키지 (`Packages/manifest.json`):
- com.unity.2d.sprite
- com.unity.ai.navigation
- com.unity.postprocessing
- com.unity.render-pipelines.core
- com.unity.ugui

## 빌드 및 실행 워크플로우

1. **프로토버퍼 재생성** (필요시)
   ```bash
   protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
   ```

2. **서버 빌드 및 실행**
   ```bash
   dotnet build GameServer/GameServer.csproj
   dotnet run --project GameServer/GameServer.csproj -- --server
   ```

3. **Unity 에디터에서 클라이언트 실행**
   - Unity 6000.0.23f1로 프로젝트 열기
   - Play 버튼 클릭
   - 로컬호스트:9000으로 연결

## 일반적인 통합 문제

### 프로토버퍼 직렬화 오류
- Unity와 서버의 프로토버퍼 버전 일치 확인
- Google.Protobuf 패키지 버전: 3.27.2

### 네트워크 연결 실패
- 서버가 9000번 포트에서 실행 중인지 확인
- 방화벽 설정 확인
- GameNetworkManager의 서버 주소 확인

### 청크 렌더링 문제
- 청크 메타데이터 파싱 확인
- EnhancedPayload 필드 검증
- ChunkPayloadBuilder 로직 검토

### IL2CPP 빌드 문제
- `Assets/link.xml`에 프로토버퍼 타입 보존 설정 추가
- AOT 컴파일 이슈 해결

## 최적화 팁

### 청크 로딩 최적화
- 배치 청크 로드 요청 사용
- ChunkLoadRequest에 여러 청크 위치 전달
- 뷰 거리(view_distance) 조정

### 엔티티 동기화 최적화
- EntitySyncService를 통한 원격 플레이어 보간
- 업데이트 빈도 조절
- 거리 기반 LOD 적용

### 메모리 관리
- 사용하지 않는 청크 언로드
- ChunkUnloadNotification 전송
- 메시 풀링 적용

## 디버깅

Unity 콘솔에서 확인할 로그:
- 네트워크 메시지 송수신
- 청크 로드/언로드
- 블록 변경 이벤트
- 엔티티 스폰/디스폰

서버 측 로그와 교차 확인하여 문제 진단
