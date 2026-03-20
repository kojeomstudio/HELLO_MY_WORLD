# Session 200: 아키텍처 및 코드 흐름 분석

## 문서 목적
Session 199에서 확장된 게임 데이터 스키마(`stack_max`, `method`)가 Unity 클라이언트 로딩 경로에서 실제로 반영되도록 코드 흐름을 보완한 내용을 정리한다.

## 사전 문제 요약
- `items.json`에 `stack_max`가 추가되었으나 Unity `InventoryManager`는 `max_stack`, `maxStack`만 읽고 있어 일부 아이템(예: 도구)의 스택 제한이 잘못 계산될 수 있었다.
- `recipes.json`에 `method`(`NORMAL`, `COOKING`, `FUEL`)가 추가되었으나 Unity `CraftingManager`는 이를 제작 타입 판정에 사용하지 않아 `COOKING` 레시피가 손 제작(`Hand`)으로 분류될 수 있었다.
- `FUEL` 데이터는 결과 아이템이 없는 정의가 가능하지만 기존 로더는 결과가 없으면 폐기했다.

## minetest 참조 기준
- `minetest_project/src/craftdef.h`
  - `CRAFT_METHOD_NORMAL`
  - `CRAFT_METHOD_COOKING`
  - `CRAFT_METHOD_FUEL`
- `minetest_project/src/itemdef.h`
  - `ItemDefinition.stack_max`
  - `ItemDefinition.groups`

## 변경 아키텍처

### Inventory 데이터 로딩
`Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
- `ParseItemFromArray` / `ParseItemFromCatalog`에서 스택 수량 파싱 우선순위를 다음으로 통일:
  1. `stack_max`
  2. `max_stack`
  3. `maxStack` 또는 기존 필드
- 결과: 최신 JSON 스키마와 레거시 스키마를 동시에 수용.

### Crafting 데이터 로딩
`Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- 내부 `RecipeMethod`(Unknown/Normal/Cooking/Fuel) 분류 추가.
- `method`/`craft_method`를 우선 파싱하고, `COOKING`/`FUEL`은 `CraftingType.Furnace`로 매핑.
- `FUEL` 레시피는 제작 결과 목록 검증에서 제외하고, `fuelBurnTimeByItemId` 딕셔너리에 연료 소모시간으로 저장.
- 기존 `station`/`crafting_type` 기반 매핑은 유지해 하위 호환성 보장.

## 코드 흐름

### 아이템 로딩 흐름
1. `InventoryManager.Start()`
2. `EnsureItemDatabaseLoaded()`
3. `TryLoadItemDatabaseFromJson()`
4. `ParseItemFromArray()`
5. `stack_max` 우선 파싱 후 `ItemData.maxStack` 설정

### 레시피 로딩 흐름
1. `CraftingManager.Start()`
2. `LoadRecipes()`
3. `TryLoadRecipesFromJson()`
4. `TryRegisterRecipe()`
5. `ParseRecipeMethod()`
6. 분기 처리
   - `FUEL`: `RegisterFuelRecipe()`로 연료 시간 등록
   - `NORMAL`/`COOKING`: 결과/재료 검증 후 제작 레시피 등록

## 기대 효과
- Unity 클라이언트와 서버 간 게임 데이터 스키마 해석 차이를 축소.
- minetest `CraftMethod` 모델을 Unity 제작 타입으로 명시적으로 연결.
- 향후 화로/연료 로직 확장 시 사용할 수 있는 연료 시간 카탈로그를 사전 확보.

## 참조 파일
- `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
- `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- `config/game-data/items.json`
- `config/game-data/recipes.json`
- `minetest_project/src/craftdef.h`
- `minetest_project/src/itemdef.h`
