# Session 201: 기획 실행 문서

## 개요
minetest `craftdef.h`와 `itemdef.h`를 참조하여 Unity 클라이언트의 제작 시스템을 확장했다.

## 구현 항목

### 1. CraftReplacements
**목표:** 제작 시 특정 재료를 소모하지 않고 다른 아이템으로 교체

**구현 내용:**
- `CraftingReplacement` 데이터 구조 정의
- JSON `replacements` 필드 파싱
- `CompleteCrafting()`에서 교체 로직 실행

**사용 예시:**
```json
{
  "id": "recipe_cake",
  "ingredients": [
    { "item_id": "milk_bucket", "amount": 3 }
  ],
  "replacements": [
    { "consume": "milk_bucket", "replace_with": "empty_bucket" }
  ]
}
```

### 2. Shaped Recipe
**목표:** 그리드 기반 배치가 필요한 레시피 지원

**구현 내용:**
- `isShaped`, `width`, `height` 필드 추가
- `CanCraftShapedRecipe()` 그리드 패턴 검증
- 오프셋 기반 패턴 매칭 (3x3 그리드 내 2x2 레시피 등)

**사용 예시:**
```json
{
  "id": "recipe_pickaxe",
  "shaped": true,
  "width": 3,
  "height": 3,
  "ingredients": [
    { "item_id": "iron_ingot", "amount": 3 },
    { "item_id": "stick", "amount": 2 }
  ]
}
```

### 3. Item Groups
**목표:** 그룹 기반으로 유연한 재료 매칭

**구현 내용:**
- `ItemData.groups` 배열 필드
- `group:xxx` 형식으로 그룹 재료 지정
- 그룹에 속한 모든 아이템 수량 합산

**items.json 예시:**
```json
{
  "id": "oak_plank",
  "groups": ["material", "wood", "flammable"]
},
{
  "id": "birch_plank",
  "groups": ["material", "wood", "flammable"]
}
```

**recipes.json 예시:**
```json
{
  "id": "recipe_stick",
  "ingredients": [
    { "group": "group:wood", "amount": 2 }
  ],
  "results": [{ "item_id": "stick", "amount": 4 }]
}
```

## 검증 항목
- [ ] 빌드 테스트 통과
- [ ] Unity 컴파일 에러 없음
- [ ] 기존 레시피 로드 정상 동작

## 추후 확장 가능
- Tool Repair 레시피 타입
- Recipe Priority 시스템
- 그룹 가중치 기반 재료 선택
