# Session 201: 아키텍처 및 코드 흐름 분석

## 문서 목적
minetest `craftdef.h` 기반으로 CraftReplacements, Shaped Recipe, Item Groups 지원을 Unity 제작 시스템에 구현한 내용을 정리한다.

## minetest 참조 기준
- `minetest_project/src/craftdef.h`
  - `CraftReplacements`: 제작 시 소모된 아이템을 다른 아이템으로 교체
  - `CraftDefinitionShaped`: 그리드 기반 배치 필수 레시피
  - `CraftDefinitionShapeless`: 순서 무관 레시피
- `minetest_project/src/itemdef.h`
  - `ItemGroupList`: 아이템 그룹 기반 재료 매칭 (예: `group:wood`)

## 변경 아키텍처

### 1. CraftReplacements 구조 추가
**CraftingManager.cs**
- `CraftingReplacement` 클래스 추가: `consumeItemId`, `replaceWithItemId`
- `CraftingRecipe.replacements` 배열 필드 추가
- `ParseReplacements()` 메서드로 JSON의 `replacements` 배열 파싱
- `CompleteCrafting()`에서 교체 로직 수행

**처리 흐름:**
1. 레시피의 `replacements` 배열 확인
2. 소모될 재료가 `consumeItemId`와 일치하면
3. `removeItem` 대신 `replaceWithItemId`를 인벤토리에 추가

### 2. Shaped Recipe 지원
**CraftingManager.cs**
- `CraftingRecipe`에 `isShaped`, `width`, `height` 필드 추가
- `CanCraftShapedRecipe()`: 그리드 패턴 매칭 검증
- `MatchesShapedPattern()`: 오프셋 기반 패턴 검사

**Shaped Recipe JSON 예시:**
```json
{
  "id": "recipe_pickaxe",
  "shaped": true,
  "width": 3,
  "height": 3,
  "ingredients": [...]
}
```

### 3. Item Groups 지원
**InventoryManager.cs**
- `ItemData.groups` 필드 추가: 문자열 배열
- `ParseGroups()`: JSON groups 배열 파싱
- `ItemHasGroup()`: 특정 그룹 소속 확인
- `GetAllItemIdMappings()`: 전체 아이템 ID 매핑 반환

**CraftingManager.cs**
- `CraftingIngredient.group` 필드 추가
- `ParseIngredients()`: `group` 필드 파싱
- `HasEnoughIngredients()`: 그룹 기반 수량 확인
- `CountItemsInGroup()`: 그룹에 속한 아이템 총 수량 계산
- `IngredientMatchesItem()`: 그룹 매칭 검증

**Groups 기반 레시피 예시:**
```json
{
  "id": "recipe_torch",
  "ingredients": [
    { "group": "group:fuel", "amount": 1 },
    { "item_id": "stick", "amount": 1 }
  ]
}
```

## 코드 흐름

### 제작 가능 확인 흐름
1. `CanCraftRecipe(recipeId)`
2. `HasEnoughIngredients(ingredient)` for each ingredient
3. 그룹 재료인 경우 `CountItemsInGroup()` 호출
4. 일반 재료인 경우 `inventoryManager.GetItemCount()` 호출

### 제작 완료 흐름
1. `CompleteCrafting()`
2. 각 재료에 대해:
   - `replacements` 배열에서 매칭 확인
   - 교체 대상이면: 소모 후 `replaceWithItemId` 추가
   - 아니면: 단순 소모
3. 결과 아이템 추가

## 기대 효과
- minetest와 호환되는 레시피 시스템 구축
- 물 양동이 → 빈 양동이 같은 교체 로직 지원
- 그룹 기반으로 유연한 레시피 정의 가능
- 그리드 기반 제작 UI 확장 가능

## 참조 파일
- `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
- `config/game-data/items.json`
- `config/game-data/recipes.json`
- `minetest_project/src/craftdef.h`
- `minetest_project/src/itemdef.h`
