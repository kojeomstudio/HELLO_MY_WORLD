# Session 198: 아키텍처 및 코드 흐름 분석

## 문서 목적
minetest 서브모듈 기반 게임 데이터 시스템 확장 작업에 대한 아키텍처 분석 및 코드 흐름 정리.

## 현재 아키텍처 개요

### 1. 게임 데이터 파이프라인
```
design/templates/game-data-template.md
        ↓ (GameDataTemplateExporter)
config/game-data/*.json
        ↓ (미러 동기화)
├── GameServer/config/game-data/*.json
└── Assets/StreamingAssets/game-data/*.json
```

### 2. 데이터 로딩 계층
```
Unity Client:
  GameDataManager.cs
    → InventoryManager (items.json)
    → CraftingManager (recipes.json)
    → [TBD] MonsterManager (monsters.json)
    → [TBD] NPCManager (npcs.json)

GameServer:
  DataDrivenConfigManager.cs
    → InventorySystem
    → CraftingHandler
    → HealthAndHungerSystem
```

### 3. minetest 참조 구조

#### craftdef.h 핵심 구조
```cpp
enum CraftMethod {
    CRAFT_METHOD_NORMAL,   // 제작대
    CRAFT_METHOD_COOKING,  // 화로
    CRAFT_METHOD_FUEL      // 연료
};

struct CraftInput {
    CraftMethod method;
    unsigned int width;
    std::vector<ItemStack> items;
};

struct CraftOutput {
    std::string item;  // 결과 아이템
    float time;        // 제작/조리 시간
};

struct CraftReplacements {
    // 예: 물 양동이 → 빈 양동이
    std::vector<std::pair<std::string, std::string>> pairs;
};
```

#### itemdef.h 핵심 구조
```cpp
enum ItemType : u8 {
    ITEM_NONE,
    ITEM_NODE,    // 블록
    ITEM_CRAFT,   // 제작 재료
    ITEM_TOOL     // 도구
};

struct ItemDefinition {
    ItemType type;
    std::string name;
    std::string description;
    u16 stack_max;
    bool usable;
    ToolCapabilities* tool_capabilities;
    ItemGroupList groups;  // "wood", "flammable" 등
    SoundSpec sound_place;
    f32 range;
};
```

## 개선 필요 사항

### 1. items.json 스키마 확장
현재:
```json
{ "id": "wooden_pickaxe", "type": "tool", "durability": 59, "stackable": false }
```

minetest 기반 확장안:
```json
{
  "id": "wooden_pickaxe",
  "type": "tool",
  "description": "Wooden Pickaxe",
  "stack_max": 1,
  "tool_capabilities": {
    "dig_speed": { "wood": 2.0, "stone": 1.0 },
    "damage": 2,
    "durability": 59
  },
  "groups": ["tool", "pickaxe", "wooden"],
  "sound_place": "dig_wood"
}
```

### 2. recipes.json 스키마 확장
현재:
```json
{ "id": "recipe_wooden_pickaxe", "result": {...}, "ingredients": [...] }
```

minetest 기반 확장안:
```json
{
  "id": "recipe_wooden_pickaxe",
  "method": "NORMAL",
  "result": { "item_id": "wooden_pickaxe", "count": 1 },
  "ingredients": [
    { "item_id": "plank", "count": 3 },
    { "item_id": "stick", "count": 2 }
  ],
  "craft_time": 0.0,
  "replacements": []
}
```

### 3. 그룹 시스템 도입
minetest의 그룹 시스템을 통해:
- 아이템 카테고리화 (`groups: ["wood", "flammable"]`)
- 레시피에서 그룹 매칭 (`ingredient: { "group": "wood", "count": 3 }`)
- 도구 채굴 속도 (`dig_speed: { "wood": 2.0 }`)

## 코드 흐름

### Unity 클라이언트 데이터 로딩
```
1. GameSupervisor.Awake()
2. GameDataManager.Initialize()
3. InventoryManager.LoadGameItems()
   - 후보 경로: StreamingAssets/game-data → config/game-data
4. CraftingManager.LoadRecipes()
5. 게임 시작
```

### GameServer 데이터 로딩
```
1. Program.Main()
2. DataDrivenConfigManager.Initialize()
3. InventorySystem.LoadItems()
4. CraftingHandler.LoadRecipes()
5. 클라이언트 연결 대기
```

## 참조 파일
- `minetest_project/src/craftdef.h`
- `minetest_project/src/itemdef.h`
- `minetest_project/src/nodedef.h`
- `Assets/MyAssets/Scripts/DataFiles/GameDataManager.cs`
- `GameServer/Configuration/DataDrivenConfigManager.cs`
