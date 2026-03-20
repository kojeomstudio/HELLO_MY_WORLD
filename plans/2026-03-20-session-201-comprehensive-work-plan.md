# Session 201: minetest 기반 CraftReplacements 및 Shaped Recipe 지원

## 작업 일자
2026-03-20

## 작업 목표
- minetest `craftdef.h`의 `CraftReplacements` 구조를 Unity 제작 시스템에 구현
- Shaped vs Shapeless 레시피 구분 지원 추가
- Item groups를 활용한 레시피 재료 매칭 개선

## 사전 현황 파악 (최근 1주 + 로컬 변경)
- 최근 1주 커밋: Session 199-200 게임 데이터 스키마 정합성 작업 완료
- HEAD: `44be901c` (`docs(session-200): mark work plan completed`)
- 로컬 변경: 없음 (working tree clean)
- 원격 추적: master == origin/master 동기화

## minetest 참조 분석
- `minetest_project/src/craftdef.h`
  - `CraftReplacements`: 제작 시 아이템 교체 (예: water_bucket → empty_bucket)
  - `CraftDefinitionShaped`: 그리드 기반 배치 필수
  - `CraftDefinitionShapeless`: 순서 무관
  - `RecipePriority`: 레시피 우선순위 체계
- `minetest_project/src/itemdef.h`
  - `ItemGroupList`: 아이템 그룹 기반 재료 매칭

## 작업 체크리스트

### 1. CraftReplacements 구현
- [x] `CraftingRecipe` 클래스에 `replacements` 필드 추가
- [x] `TryRegisterRecipe`에서 replacements 파싱
- [x] `CompleteCrafting`에서 교체 아이템 반환 처리

### 2. Shaped Recipe 지원
- [x] `CraftingRecipe`에 `isShaped`, `width`, `height` 필드 추가
- [x] 그리드 기반 재료 검증 로직 구현
- [x] Shapeless 레시피와 Shaped 레시피 구분 처리

### 3. Item Groups 활용
- [x] `InventoryManager`에서 groups 파싱 및 저장
- [x] 레시피 재료 매칭 시 그룹 기반 매칭 지원
- [x] 예: `group:wood` → plank, stick 등 wood 그룹 아이템

### 4. 문서화
- [x] 아키텍처/코드 흐름 문서 작성 (`docs/2026-03-20-session-201-architecture-and-code-flow.md`)
- [x] 기획 실행 문서 작성 (`design/2026-03-20-session-201-design-execution.md`)

### 5. 검증 및 Git 반영
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] `dotnet build GameServer/GameServer.csproj`
- [x] Unity 컴파일 확인
- [x] 변경사항 커밋
- [x] `git push origin master`

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| CraftReplacements, Shaped Recipe, Item Groups 지원 추가 | 17032ade | 2026-03-20 |
