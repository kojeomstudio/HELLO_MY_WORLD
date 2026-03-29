# Minetest Architecture Reference (2026-03-18)

## Purpose
minetest_project 서브모듈을 기반으로 한 아키텍처 참조 문서. Minecraft 모작 개발 시 따라야 할 패턴과 구조를 정의.

## Core Components

### 1. Server Architecture (`src/server.cpp`)
```
Server
├── ServerThread (메인 서버 루프)
├── ServerEnvironment (월드 시뮬레이션)
├── ServerMap (맵 데이터 관리)
├── EmergeManager (맵 생성 큐)
├── ServerScripting (Lua 모딩)
└── Network Connection (클라이언트 통신)
```

**핵심 패턴:**
- 스레드 분리: ServerThread가 메인 루프 담당
- Environment 패턴: ServerEnvironment가 게임 상태 관리
- Emerge 시스템: 청크 생성을 큐 기반으로 관리

### 2. Client Architecture (`src/client/client.cpp`)
```
Client
├── Network Connection (서버 통신)
├── ClientMap (로컬 맵 렌더링)
├── LocalPlayer (플레이어 상태)
├── MeshGeneratorThread (렌더링 메시 생성)
└── Sound/Texture/Particle Managers
```

**핵심 패턴:**
- 수신 패킷 큐 기반 처리
- 클라이언트 사이드 예측
- 메시 생성 별도 스레드

### 3. World Generation (`src/emerge.cpp`)
```
EmergeManager
├── BiomeGen (바이옴 생성)
├── OreManager (광석 분포)
├── DecorationManager (구조물 배치)
├── SchematicManager (스키메틱 로드)
└── EmergeThread[] (병렬 생성 워커)
```

**핵심 패턴:**
- 큐 기반 청크 생성 요청
- 병렬 워커 스레드 풀
- Biome → Ore → Decoration → Schematic 순서

### 4. World Format (`doc/world_format.md`)
```
World/
├── auth.txt/sqlite (인증 데이터)
├── env_meta.txt (환경 메타데이터)
├── map_meta.txt (맵 메타데이터)
├── map.sqlite (맵 블록 데이터)
├── players/ (플레이어 파일)
└── world.mt (월드 설정)
```

**핵심 패턴:**
- SQLite 기반 맵 저장 (map.sqlite)
- 플레이어별 파일 분리
- 메타데이터 텍스트 파일

## Protocol Flow

### Connection Sequence
1. Client → Server: 인증 요청 (SRP)
2. Server → Client: 인증 응답 + 월드 정보
3. Client → Server: 초기 위치/상태 요청
4. Server → Client: 청크 데이터 스트리밍

### Chunk Loading
1. Client → Server: ChunkLoadRequest
2. Server: EmergeManager 큐에 등록
3. EmergeThread: 청크 생성/로드
4. Server → Client: ChunkDataResponse

## Adoption Guidelines

### 서버 구현 시
- [ ] ServerEnvironment 패턴 채용 (월드 시뮬레이션 분리)
- [ ] EmergeManager 스타일 큐 기반 청크 생성
- [ ] SQLite 기반 월드 저장

### 클라이언트 구현 시
- [ ] 수신 패킷 큐 기반 처리
- [ ] 메시 생성 별도 스레드
- [ ] 클라이언트 사이드 예측

### 프로토콜 구현 시
- [ ] Google Protobuf 유지
- [ ] 핸드셰이크 → 인증 → 월드 로드 시퀀스
- [ ] 청크 요청/응답 패턴

## Current Project Mapping

| Minetest | Our Project | Status |
|----------|-------------|--------|
| Server | GameServer | ✅ 구현됨 |
| Client | Unity Client | ✅ 구현됨 |
| EmergeManager | WorldManager | ✅ 구현됨 |
| ServerMap | WorldManager | ✅ 구현됨 |
| NetworkProtocol | protobuf | ✅ 구현됨 |
| BiomeGen | TerrainGenerator | ✅ 구현됨 |

## References
- `minetest_project/src/server.cpp`
- `minetest_project/src/client/client.cpp`
- `minetest_project/src/emerge.cpp`
- `minetest_project/doc/world_format.md`
- `minetest_project/doc/protocol.txt`
