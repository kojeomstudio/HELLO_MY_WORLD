# Session 200: minetest 스키마-Unity 파서 정합성 보완 작업 계획

## 작업 일자
2026-03-20

## 작업 목표
- minetest `craftdef.h`/`itemdef.h` 기준으로 Unity 파서의 누락 필드 처리 보완
- `items.json`의 `stack_max`를 Unity 인벤토리 스택 계산에 반영
- `recipes.json`의 `method`(NORMAL/COOKING/FUEL)를 Unity 제작 타입에 반영
- 세션 문서 정합성(특히 Session 199 계획 문서 완료 상태) 보정

## 사전 현황 파악 (최근 1주 + 로컬 변경)
- 최근 1주 커밋 확인 완료 (`git log --since="7 days ago"`)
- HEAD: `dd6b4ce7` (`feat(session-199): extend game-data schema with minetest reference`)
- 로컬 변경 확인 완료 (`git status --short`) - 작업 시작 시점 기준 변경 파일 없음
- 원격 추적 확인 완료 (`git branch -vv`) - `master`가 `origin/master`와 동기화된 상태에서 시작

## 작업 체크리스트

### 1. minetest 참조 검토
- [x] `minetest_project/src/craftdef.h`의 `CraftMethod` 확인
- [x] `minetest_project/src/itemdef.h`의 `stack_max`, `groups` 확인
- [x] Unity 현재 파서 동작과 스키마 간 불일치 지점 식별

### 2. 코드 정합성 개선
- [x] `InventoryManager`에 `stack_max` 필드 우선 파싱 추가
- [x] `CraftingManager`에 `method` 기반 제작 타입 매핑 추가
- [x] FUEL 레시피를 제작 레시피와 분리해 연료 소모시간 데이터로 로드

### 3. 문서화
- [x] 아키텍처/코드 흐름 문서 작성 (`docs/2026-03-20-session-200-architecture-and-code-flow.md`)
- [x] 기획 실행 문서 작성 (`design/2026-03-20-session-200-design-execution.md`)
- [x] Session 199 계획 문서 완료 상태 정합성 보정

### 4. 검증 및 Git 반영
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] `dotnet build GameServer/GameServer.csproj`
- [ ] 변경사항 커밋
- [ ] `git push origin master`

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| | | |
