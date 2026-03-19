# Session 192: 아키텍처 및 코드 흐름 분석

## 작성 일자
2026-03-19

## 개요
minetest 서브모듈 프로젝트를 참조하여 현재 Unity 클라이언트 및 .NET 서버 아키텍처를 분석하고 개선점을 도출한다.

---

## 1. Minetest 서버 아키텍처 분석

### 1.1 핵심 클래스 구조 (minetest/src/server.h)

```
Server
├── ServerEnvironment (m_env)
├── EmergeManager (m_emerge) - 맵 생성 관리
├── ServerScripting (m_script) - Lua 스크립팅
├── ServerModManager (m_modmgr) - 모드 관리
├── ClientInterface (m_clients) - 클라이언트 연결 관리
├── ServerInventoryManager (m_inventory_mgr)
├── BanManager (m_banmanager)
├── IItemDefManager (m_itemdef)
├── NodeDefManager (m_nodedef)
├── ICraftDefManager (m_craftdef)
└── ModStorageDatabase (m_mod_storage_database)
```

### 1.2 주요 서버 기능

1. **네트워크 핸들러**
   - `handleCommand_*()` 시리즈 - 클라이언트 요청 처리
   - `Send*()` 시리즈 - 클라이언트에 데이터 전송
   - `Receive()` - 패킷 수신 처리

2. **월드 관리**
   - `EmergeManager` - 청크/블록 생성
   - `Map` - 월드 맵 데이터
   - `sendRemoveNode()` / `sendAddNode()` - 블록 변경 동기화

3. **플레이어 관리**
   - `emergePlayer()` - 플레이어 생성
   - `SendInventory()` - 인벤토리 동기화
   - `HandlePlayerHPChange()` - HP 변경 처리

4. **미디어 관리**
   - `fillMediaCache()` - 미디어 캐시
   - `sendMediaAnnouncement()` - 미디어 공지
   - `dynamicAddMedia()` - 동적 미디어 추가

---

## 2. 현재 프로젝트 아키텍처 비교

### 2.1 GameServer (.NET)

```
GameServer/
├── Program.cs - 진입점
├── ServerConfig.cs - 설정
├── SessionManager.cs - 세션 관리
├── Handlers/ - 요청 핸들러
└── (SharedProtocol) - 프로토콜 공유
```

### 2.2 Unity 클라이언트

```
Assets/MyAssets/Scripts/
├── GameWorld/ - 월드 관리
├── Network/ - 네트워크 통신
└── UI/ - 사용자 인터페이스
```

---

## 3. 개선 제안 사항

### 3.1 서버 아키텍처

| Minetest | 현재 구현 | 개선 필요 |
|----------|-----------|-----------|
| EmergeManager | WorldGenerator | 청크 생성 비동기화 |
| ClientInterface | SessionManager | 연결 상태 관리 강화 |
| ServerInventoryManager | 미구현 | 인벤토리 시스템 필요 |
| NodeDefManager | BlockDatabase | 블록 정의 체계화 |
| ModStorageDatabase | JSON 설정 | 데이터 영속성 강화 |

### 3.2 네트워크 프로토콜

- Protobuf 기반 메시지 직렬화 유지
- Minetest의 패킷 핸들러 패턴 참조
- 클라이언트-서버 동기화 로직 개선

### 3.3 데이터 드라이븐 접근

- 블록/아이템 정의를 JSON으로 관리
- 게임 데이터(제작식, 몬스터 등) JSON화
- 템플릿에서 JSON 변환 도구 활용

---

## 4. 다음 단계

1. 인벤토리 시스템 설계 및 구현
2. 블록 정의 JSON화
3. 청크 생성 비동기 처리
4. 클라이언트-서버 동기화 개선

---

## 참조
- minetest/src/server.h
- minetest/src/environment.cpp
- minetest/src/map.cpp
- GameServer/Handlers/
- Assets/MyAssets/Scripts/Network/
