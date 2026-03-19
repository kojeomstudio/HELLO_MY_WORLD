# Session 195: 블록 ID 프로토콜 정규화 작업 계획

## 작업 일자
2026-03-19

## 작업 목표
- `work/work.md` 지침에 따라 최근 1주일 커밋 및 로컬 변경 상태를 먼저 분석한다.
- minetest `nameidmapping` 구조를 참조해 서버 내부 ID와 프로토콜 ID의 정합성 레이어를 구축한다.
- 기존 서버 저장 포맷은 유지하면서 네트워크 경계에서 블록 ID를 정규화한다.
- 컴파일 검증 후 커밋/원격 반영까지 완료한다.

## 현재 상황 분석

### 최근 1주일 커밋 기록 (2026-03-13 ~ 2026-03-19)
- `0c3a9aee`: session-194 계획 완료 반영
- `bf6794e4`: docs/plans 정리 + 블록/아이템 ID 통합 분석 문서화
- `9324c792`: 인벤토리/제작 데이터 드리븐 리팩터
- `690fe06b`: .NET CI + protobuf 검증 워크플로 추가
- `1c45cf85`: 게임 데이터 시작 시 검증 로직 추가

### 작업 시작 시 로컬 상태
- `git status --short`: 변경사항 없음(clean).

### 현재 이슈
1. 서버 내부 `GameServerApp.Models.BlockType` ID와 공유/클라이언트 기준 ID(`config/blocks.json`, `GameCommon.Blocks.BlockType`) 간 불일치.
2. 블록 변경 요청/브로드캐스트/청크 페이로드에서 ID 정규화 계층 부재.
3. 저장 포맷(DB/청크 바이트)을 즉시 변경하면 기존 데이터 호환 리스크가 큼.

---

## 작업 항목

### 1. minetest 참조 기반 설계 정리
- [x] `minetest_project/src/nameidmapping.h` 분석
- [x] 네트워크 경계에서의 이름/ID 매핑 레이어 설계

### 2. 코드 구현
- [x] `GameServer/Models/BlockTypeProtocolMapper.cs` 추가
- [x] `WorldBlockHandler` 요청 검증/브로드캐스트에 프로토콜 ID 정규화 적용
- [x] `WorldSynchronizationManager` 큐 데이터 정규화 적용
- [x] `MinecraftChunkHandler`에서 Enhanced payload 블록 데이터 프로토콜 ID 변환 적용

### 3. 문서화
- [x] 아키텍처/코드 흐름 문서 작성 (`docs/2026-03-19-session-195-architecture-and-code-flow.md`)
- [x] 설계 실행 문서 작성 (`design/2026-03-19-session-195-design-execution.md`)
- [x] `docs`, `plans` 경로 정합성 점검 (삭제 대상 없음)

### 4. 검증 및 Git 반영
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] `dotnet build GameServer/GameServer.csproj`
- [ ] 변경사항 커밋
- [ ] `git push origin master`

---

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| 블록 ID 프로토콜 정규화 레이어 구현 | `TBD` | `TBD` |
| 문서화 및 계획서 갱신 | `TBD` | `TBD` |
| 컴파일 검증 | `TBD` | `2026-03-19` |
| origin 반영 | `TBD` | `TBD` |
