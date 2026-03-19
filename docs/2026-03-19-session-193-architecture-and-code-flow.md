# Session 193: 아키텍처 및 코드 흐름 분석

## 작성 일자
2026-03-19

## 목적
`minetest_project` 구조를 기준으로 현재 Unity 클라이언트와 .NET 서버의 코드 흐름을 재점검하고, 데이터 드리븐 정합성 관점에서 개선 사항을 도출한다.

---

## 1. minetest 참조 구조

### 1.1 서버 핵심 구성
- `minetest_project/src/server.h`
  - `Server`
  - `ServerEnvironment`
  - `EmergeManager`
  - `ServerScripting`
  - `ServerModManager`
  - `ServerInventoryManager`
  - `ModStorageDatabase`
- `minetest_project/src/server.cpp`
  - `Server::Receive()` 기반 수신 루프
  - `handleCommand_*` 처리 흐름
  - `Send*` 계열 브로드캐스트/응답 함수
  - `fillMediaCache()`, `sendMediaAnnouncement()`, `dynamicAddMedia()`
- `minetest_project/src/serverenvironment.h`
  - 월드 환경/엔티티 생명주기 관리
- `minetest_project/src/emerge.h`
  - `EmergeManager`를 통한 청크 생성/큐 처리

### 1.2 설계 관점 시사점
- 네트워크, 월드 생성, 데이터 저장, 모드/스크립트가 명확히 계층 분리됨.
- 데이터 정의와 런타임 로직 간 결합도를 낮게 유지함.
- 서버 권한(authoritative) 모델이 일관되게 적용됨.

---

## 2. 현재 프로젝트 코드 흐름

### 2.1 서버 진입 및 검증 체인
- `GameServer/Program.cs`
  - 프로토콜/생성코드 검증
  - feature manifest 로딩
  - world-map-control queue/profile parity 검증
  - game-data dataset 검증 후 서버 기동

### 2.2 런타임 서버
- `GameServer/GameServer.cs`
  - `TcpListener` 수신 루프
  - `MessageDispatcher` + `MinecraftMessageDispatcher` 이중 디스패치
  - `SessionManager`, `WorldManager`, `WorldSynchronizationManager` 중심 상태 관리

### 2.3 세션/브로드캐스트
- `GameServer/SessionManager.cs`
  - 세션/플레이어 상태 동시성 관리(`ConcurrentDictionary`)
  - 하트비트 타임아웃 정리
  - legacy/enhanced 프로토콜 듀얼 브로드캐스트 지원

### 2.4 청크 처리
- `GameServer/Handlers/MinecraftChunkHandler.cs`
  - legacy + enhanced 요청 파싱
  - 청크 로드/생성 + 압축 + 바이옴 + 엔티티 동봉
  - 플레이어별 chunk residency 추적 및 정리

### 2.5 Unity 클라이언트
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - `StreamingAssets`의 world-map-control/profile/queue-policy 기반 스트리밍 튜닝
- `Assets/MyAssets/Scripts/Network/GameNetworkManager.cs`
  - 기존 `CPacket` 중심 로직 유지(구형 경로)
  - protobuf 검증 호출 포함이나 전송 계층은 이원화 상태

---

## 3. 이번 세션 반영 개선

### 3.1 인벤토리/제작 리팩터
- `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
  - 중복 클래스 정의 제거
  - `StreamingAssets/items.json` 및 `config/items.json` JSON 로딩 경로 추가
  - 문자열 키 기반 아이템 ID 해석 및 fallback default 처리
- `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
  - 중복 클래스 정의 제거
  - `StreamingAssets/recipes.json` 및 `config/recipes.json` JSON 로딩 경로 추가
  - recipe ingredient/result의 문자열 item key를 inventory item ID로 매핑

### 3.2 데이터 드리븐 정합성
- `Assets/StreamingAssets/recipes.json` 추가로 런타임 JSON 데이터 접근 경로를 보강.
- `Resources/Data` TODO 의존 대신 JSON 우선 로딩으로 전환.

---

## 4. 남은 개선 포인트
- Unity 네트워크 계층(`GameNetworkManager`)을 protobuf 단일 경로로 수렴 필요.
- 아이템 ID 자료형 정합성(블록/아이템 통합 식별체계) 재설계 필요.
- minetest `ServerInventoryManager`/`NodeDefManager` 대응 서버-클라 공통 데이터 모델 강화 필요.

---

## 참조 파일
- `minetest_project/src/server.h`
- `minetest_project/src/server.cpp`
- `minetest_project/src/serverenvironment.h`
- `minetest_project/src/emerge.h`
- `GameServer/Program.cs`
- `GameServer/GameServer.cs`
- `GameServer/SessionManager.cs`
- `GameServer/Handlers/MinecraftChunkHandler.cs`
- `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
- `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
