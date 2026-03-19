# Session 193: 데이터 드리븐 인벤토리/제작 리팩터 및 문서 정리

## 작업 일자
2026-03-19

## 작업 목표
- `work/work.md` 지침에 따라 현재 프로젝트 상태를 파악하고 작업 계획을 수립한다.
- Unity 인벤토리/제작 시스템을 JSON 데이터 드리븐 방식으로 보강한다.
- `docs`, `plans` 경로의 오래되거나 정합성이 낮은 문서를 정리한다.
- minetest 서브모듈을 기준으로 아키텍처/코드 흐름 개선 포인트를 문서화한다.

## 현재 상황 분석

### 최근 1주일 커밋 기록 (2026-03-13 ~ 2026-03-19)
- hydrology/map-control/proto 패리티 작업이 session 163~192까지 연속 반영됨.
- `minetest_project` 서브모듈 참조 기반 아키텍처 검토/문서 정리가 지속 수행됨.
- game-data(JSON), exporter 도구, CI(workflow) 관련 작업이 이미 도입됨.

### 작업 시작 시 로컬 상태
- `git status --short` 기준 변경사항 없음(clean).

### 현재 이슈
- `InventoryManager`, `CraftingManager`에 클래스 중복 정의가 존재하여 유지보수/정합성 리스크가 높음.
- 인벤토리/제작 로직이 `Resources/Data` TODO 상태로 남아 있어 JSON 데이터 활용 경로가 불완전함.
- `docs/plans` 경로에 2025~초기 2026 기준의 구형 문서가 다수 잔존.

---

## 작업 항목

### 1. 코드 리팩터링 및 데이터 드리븐 보강
- [x] `InventoryManager` 중복 클래스 정의 제거 및 단일 구현 정리
- [x] `CraftingManager` 중복 클래스 정의 제거 및 단일 구현 정리
- [x] `StreamingAssets/config` JSON 우선 로딩 경로 추가
- [x] `recipes.json`을 `Assets/StreamingAssets`로 동기화

### 2. minetest 기준 아키텍처 검토 문서화
- [x] minetest 서버 구조(`server.h`, `server.cpp`, `serverenvironment.h`, `emerge.h`)와 현 프로젝트 매핑
- [x] Unity/Server 코드 흐름 및 개선 사항 정리

### 3. 문서 정리
- [x] 2025 기준 구형 문서 및 AI 초기 보고서 삭제
- [x] `plans` 경로의 구형 비세션 계획 문서 삭제

### 4. 검증 및 Git 반영
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] `dotnet build GameServer/GameServer.csproj`
- [ ] 변경사항 커밋
- [ ] `git push origin master`

---

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| 코드 리팩터링 및 문서 정리 | `TBD` | 2026-03-19 |
| 컴파일 테스트 | `TBD` | 2026-03-19 |
| origin 반영 | `TBD` | 2026-03-19 |
