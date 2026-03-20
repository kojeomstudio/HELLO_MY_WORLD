# Session 197: game-data 미러 정합성 강화 및 문서 정리

## 작업 일자
2026-03-20

## 작업 목표
- `work/work.md` 지시에 따라 최근 1주 커밋/로컬 변경 파일을 먼저 점검한다.
- minetest 참조 구조에 맞춰 게임 데이터(JSON) 로딩 경로와 미러 동기화 체계를 정렬한다.
- `docs/`, `plans/` 경로의 오래되거나 정합성이 낮은 문서를 정리한다.
- 세션 문서(`plans`, `docs`, `design`)를 최신 기준으로 갱신한다.

## 현재 상황 파악

### 최근 1주 커밋 요약 (2026-03-13 ~ 2026-03-20)
- session-193~196에서 데이터 드리븐 인벤토리/제작, 블록 ID 정합성, 문서 정리가 반복 수행되었다.
- `655ddc9b`(2026-03-18) 이후 minetest 서브모듈 기반 정렬 작업이 본격 반영되었다.
- 직전 세션(`4499cfe0`, `3b308d11`)에서 blocks/items/recipes 정합성 및 계획 문서 완료 처리가 수행되었다.

### 작업 시작 시 로컬 상태
- `git status --short`: 변경 없음(clean).

## 작업 체크리스트

### 1. 사전 분석
- [x] 최근 1주 커밋 로그 분석
- [x] 로컬 변경 파일 상태 확인
- [x] minetest 참조 포인트(`itemdef.h`, `nodedef.h`, `craftdef.h`, `nameidmapping.h`) 재확인

### 2. 코드/데이터 정합성 개선
- [x] `InventoryManager` 로딩 후보를 `StreamingAssets/game-data` + `config/game-data` 우선으로 정렬
- [x] `CraftingManager` 로딩 후보를 `StreamingAssets/game-data` + `config/game-data` 우선으로 정렬
- [x] `config/config_parity_manifest.json`에 game-data(items/recipes/monsters/npcs/character_stats) 미러 그룹 추가
- [x] `GameServer/config/game-data`, `Assets/StreamingAssets/game-data` 미러 파일 동기화

### 3. 문서 정리
- [x] `docs/`에서 2026-03-18 이전 세션 문서 및 구식 종합 문서 제거
- [x] `docs/archive/` 구식 아카이브 문서 제거
- [x] `plans/`에서 2026-03-18 이전 계획 문서 제거

### 4. 신규 문서 작성
- [x] 아키텍처/코드 흐름 문서 작성 (`docs/2026-03-20-session-197-architecture-and-code-flow.md`)
- [x] 기획 실행 문서 작성 (`design/2026-03-20-session-197-design-execution.md`)
- [x] 작업 계획 문서 작성 (`plans/2026-03-20-session-197-comprehensive-work-plan.md`)
- [x] README 문서 링크 최신 세션 기준으로 갱신

### 5. 검증 및 Git 반영
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] `dotnet build GameServer/GameServer.csproj`
- [x] 변경사항 커밋
- [ ] `git push origin master`

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| game-data 로딩 경로 우선순위 정렬 | `10a16122` | 2026-03-20 |
| game-data parity manifest 확장 | `10a16122` | 2026-03-20 |
| game-data 미러 파일 동기화 | `10a16122` | 2026-03-20 |
| docs/plans 구식 문서 정리 | `10a16122` | 2026-03-20 |
| session-197 문서 작성 | `10a16122` | 2026-03-20 |
| 컴파일 검증 및 원격 반영 | `10a16122` | 2026-03-20 |
