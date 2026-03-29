# Session 200: Design Execution (minetest CraftMethod/Stack Alignment)

## 목적
마인크래프트 모작 진행 시 minetest 데이터 모델(`CraftMethod`, `stack_max`)을 Unity 실행 코드에 직접 반영해 데이터 드리븐 기반을 강화한다.

## 참조 기준
- `minetest_project/src/craftdef.h`
- `minetest_project/src/itemdef.h`
- `design/2026-03-16-minecraft-clone-game-design.md`
- `design/2026-03-16-game-data-template-pipeline.md`

## 실행 항목

### A. 아이템 스택 규칙 정합성
- [x] `stack_max`를 Unity 인벤토리 파서 1순위 필드로 채택
- [x] 기존 `max_stack`, `maxStack` 호환성 유지
- [x] 도구/소모품 최대 스택 수량이 데이터 파일 기준으로 적용되도록 보장

### B. 제작 방식(method) 정합성
- [x] `method`(`NORMAL`/`COOKING`/`FUEL`) 파싱 추가
- [x] `COOKING`과 `FUEL`을 `CraftingType.Furnace`로 분류
- [x] `FUEL` 항목은 제작 결과가 없어도 연료시간 데이터로 유지

### C. 데이터 드리븐 확장 기반
- [x] 연료 시간 카탈로그(`itemId -> burnTime`) 추가
- [x] 기존 레시피/스테이션 필드(`station`, `crafting_type`)와 병행 지원
- [x] 신규 스키마와 레거시 스키마 동시 지원 원칙 유지

## 구현 상세
- `InventoryManager`
  - `stack_max` -> `max_stack` -> `maxStack` 순서로 파싱
- `CraftingManager`
  - 내부 `RecipeMethod` 열거형 추가
  - `method` 기반 분기 처리 추가
  - `FUEL`은 레시피 목록 대신 연료 카탈로그로 적재

## 검증 기준
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj` 성공
- [x] `dotnet build GameServer/GameServer.csproj` 성공
- [ ] Unity 실행 시 `items.json`, `recipes.json` 로드 경고/오류 로그 재확인

## 후속 설계 메모
- 화로 전용 UI/시뮬레이션 구현 시 `TryGetFuelBurnTime` API를 직접 사용해 연료 소모량 계산 로직을 분리한다.
- `recipes.json`의 그룹 기반 재료(`group`)를 Unity 제작 로직에서도 지원하도록 확장한다.
