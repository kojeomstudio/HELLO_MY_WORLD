# Session 199: 아키텍처 및 코드 흐름 분석

## 문서 목적
게임 데이터 스키마 확장에 따른 아키텍처 및 코드 흐름 분석.

## 게임 데이터 스키마 구조

### items.json 스키마 (minetest itemdef 기반)
```json
{
  "id": "wooden_pickaxe",
  "type": "tool",
  "name": "Wooden Pickaxe",
  "description": "A basic wooden pickaxe for mining stone",
  "stack_max": 1,
  "durability": 59,
  "groups": ["tool", "pickaxe", "wooden"],
  "tool_capabilities": {
    "dig_speed": { "stone": 2.0, "wood": 1.0 },
    "damage": 2
  }
}
```

### recipes.json 스키마 (minetest craftdef 기반)
```json
{
  "id": "recipe_iron_ingot",
  "name": "Iron Ingot",
  "method": "COOKING",
  "craft_time": 10.0,
  "results": [{ "item_id": "iron_ingot", "amount": 1 }],
  "ingredients": [{ "item_id": "iron_ore", "amount": 1 }],
  "replacements": []
}
```

### blocks.json 스키마 (minetest nodedef 기반)
```json
{
  "Type": 1,
  "Name": "stone",
  "DisplayName": "Stone",
  "Hardness": 1.5,
  "Groups": ["stone", "cracky", "pickaxe_diggable"],
  ...
}
```

## 데이터 로딩 계층

### GameServer
```
GameDataCatalog.cs
  → LoadDefault()
    → config/game-data/items.json
    → config/game-data/recipes.json
  → GetMaxStack(itemId)
  → ItemHasGroup(itemId, group)
```

### minetest 참조 매핑
| minetest | HelloMyWorld |
|----------|--------------|
| CRAFT_METHOD_NORMAL | method: "NORMAL" |
| CRAFT_METHOD_COOKING | method: "COOKING" |
| CRAFT_METHOD_FUEL | method: "FUEL" |
| ItemDefinition.groups | groups: [] |
| ItemDefinition.stack_max | stack_max |
| ToolCapabilities | tool_capabilities |
| ItemGroupList | Groups (blocks) |

## 코드 흐름

### 게임 데이터 로딩
```
1. GameServer 시작
2. GameDataCatalog.LoadDefault()
3. items.json, recipes.json 파싱
4. 그룹 인덱스 구축 (_groupIndex)
5. 레시피 등록 (_recipes)
```

## 참조 파일
- `GameServer/Systems/GameDataCatalog.cs`
- `config/game-data/items.json`
- `config/game-data/recipes.json`
- `config/blocks.json`
- `minetest_project/src/craftdef.h`
- `minetest_project/src/itemdef.h`
- `minetest_project/src/nodedef.h`
