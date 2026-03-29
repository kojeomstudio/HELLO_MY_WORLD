# Session 194 Design Execution (2026-03-20)

## Objective
minetest 참조 구조를 기반으로 블록/아이템 ID 통합 체계를 설계하고, 서버-클라이언트 간 데이터 정합성을 확보하기 위한 설계 방향을 정립한다.

## Design Constraints
- 게임 데이터는 JSON 기반이어야 한다.
- 블록과 아이템은 통합된 식별 체계를 가져야 한다.
- 서버와 클라이언트는 동일한 데이터 정의를 사용한다.
- minetest의 이름 기반 식별 + ID 매핑 방식을 따른다.

## Minetest Reference
- `minetest_project/src/itemdef.h`: ItemType 열거형 및 ItemDefinition 구조
- `minetest_project/src/nodedef.h`: ContentFeatures (블록 속성)
- `minetest_project/src/nameidmapping.h`: 이름-ID 양방향 매핑

## Data Model Decisions

### 1. 통합 ItemType 정의
```
ItemType:
  - node: 배치 가능한 블록
  - craft: 제작 재료/결과물
  - tool: 내구도가 있는 도구
  - food: 섭취 가능한 음식
```

### 2. 통합 ItemDefinition 구조
```json
{
  "name": "stone",
  "type": "node",
  "description": "Stone Block",
  "stackable": true,
  "max_stack": 64,
  "node_properties": {
    "solid": true,
    "transparent": false,
    "hardness": 1.5,
    "drop": "cobblestone"
  }
}
```

### 3. ID 매핑 전략
- **이름 기반 식별**: JSON에서 name 필드를 고유 키로 사용
- **런타임 ID 할당**: 로딩 시 순차적 ushort ID 할당
- **양방향 조회**: NameRegistry (name↔id) 클래스 제공

### 4. 데이터 파일 구조
```
config/game-data/
├── items.json        # 모든 아이템/블록 정의 (통합)
├── recipes.json      # 제작 레시피
├── monsters.json     # 몬스터 데이터
├── npcs.json         # NPC 데이터
└── character_stats.json  # 캐릭터 스탯
```

## Flow Design

### 1. 데이터 로딩 흐름
```
[서버/클라이언트 시작]
    → ItemRegistry.Load("items.json")
    → 각 항목에 대해 name→id 매핑 생성
    → ItemDefinition[] 배열 구축
    → 검증 완료
```

### 2. 아이템 참조 흐름
```
[코드에서 아이템 참조]
    → ItemRegistry.GetId("stone") → 1
    → ItemRegistry.GetDefinition(1) → ItemDefinition
    → 속성 접근
```

### 3. 레시피 처리 흐름
```
[제작 요청]
    → CraftingManager.FindRecipe(ingredient_ids)
    → ItemRegistry.GetId(recipe.result)
    → InventoryManager.AddItem(result_id, count)
```

## Migration Plan

### Phase 1: 데이터 구조 통합
- items.json에 블록 정의 포함 (type: "node")
- 기존 BlockType enum을 내부 ID로만 사용

### Phase 2: 레지스트리 구현
- GameCommon/Items/ItemRegistry.cs 구현
- 서버/클라이언트 공통 사용

### Phase 3: 기존 코드 마이그레이션
- BlockType 직접 참조 → ItemRegistry.GetId() 사용
- 하드코딩 ID → 문자열 상수 사용

## Expected Outcome
- 블록/아이템 식별 체계 통일
- JSON 데이터 변경만으로 새 아이템/블록 추가 가능
- 서버-클라이언트 데이터 정합성 확보
- minetest 기준 데이터-로직 분리와 정합성 향상
