# Session 196: minetest 게임 데이터 모델 아키텍처 분석

## 개요
본 문서는 minetest 서브모듈 프로젝트의 게임 데이터 핸들링 구조를 분석하고, Unity 기반 Minecraft 클론 프로젝트에 적용하기 위한 아키텍처 가이드를 제공한다.

## minetest 데이터 모델 구조

### 1. NodeDef (ContentFeatures)
`minetest_project/src/nodedef.h`

```cpp
// 핵심 구조
struct ContentFeatures {
    std::string name;           // 노드 식별자 (예: "default:stone")
    ItemGroupList groups;       // 그룹 태그 (예: {"cracky=3", "stone=1"})
    ContentParamType param_type;   // param1 용도
    ContentParamType2 param_type2; // param2 용도
    LiquidType liquid_type;     // 액체 타입
    // ... 시각적/물리적 속성
};
```

**핵심 개념**:
- `name`: string 기반 식별자 (namespace:item_name 형식)
- `groups`: 아이템 그룹핑 및 속성 태그 시스템
- `param1/param2`: 노드 메타데이터 (라이트 레벨, 회전 등)

### 2. ItemDef (ItemDefinition)
`minetest_project/src/itemdef.h`

```cpp
struct ItemDefinition {
    ItemType type;              // ITEM_NODE, ITEM_CRAFT, ITEM_TOOL
    std::string name;           // 아이템 식별자
    std::string description;    // 표시 이름
    u16 stack_max;              // 스택 크기
    ItemGroupList groups;       // 그룹 태그
    ToolCapabilities *tool_capabilities; // 도구 속성
    // ... 시각적 속성
};
```

**핵심 개념**:
- `ItemType`: 아이템 타입 분류 (NODE/CRAFT/TOOL)
- `stack_max`: 최대 스택 크기
- `tool_capabilities`: 도구의 채굴 능력 정의

### 3. CraftDef (CraftInput/CraftOutput)
`minetest_project/src/craftdef.h`

```cpp
struct CraftInput {
    CraftMethod method;         // NORMAL, COOKING, FUEL
    unsigned int width;         // 그리드 너비
    std::vector<ItemStack> items; // 입력 아이템들
};

struct CraftOutput {
    std::string item;           // 결과 아이템 문자열
    float time;                 // 제작/조리 시간
};
```

**핵심 개념**:
- `CraftMethod`: 제작 방식 (일반/조리/연료)
- `width`: 3x3 그리드에서의 너비
- `time`: 제작 소요 시간

## Unity 프로젝트 적용 방안

### 현재 데이터 구조

#### blocks.json
```json
{
  "BlockTypes": {
    "Stone": {
      "Id": 1,
      "Name": "Stone",
      "Solid": true,
      "Hardness": 1.5,
      ...
    }
  }
}
```

#### items.json
```json
{
  "items": {
    "blocks": {
      "stone": {
        "id": 1,
        "name": "Stone",
        "toolType": "pickaxe",
        ...
      }
    }
  }
}
```

### 개선 제안

1. **ID 체계 통일**
   - blocks.json과 items.json의 ID 매핑 정합성 확보
   - string 기반 식별자 사용 권장 (minetest 방식)

2. **그룹 시스템 도입**
   - ItemGroupList 패턴 적용
   - 카테고리 기반 아이템 분류

3. **제작 시스템 개선**
   - CraftMethod 타입 추가 (NORMAL/COOKING/FUEL)
   - 제작 시간 및 경험치 시스템 연동

## 코드 흐름

```
[게임 시작]
    ↓
[StreamingAssets/*.json 로드]
    ↓
[BlockTypeRegistry 초기화]
[ItemTypeRegistry 초기화]
[RecipeRegistry 초기화]
    ↓
[월드 생성/클라이언트 접속]
    ↓
[청크 데이터 로드 → BlockType 조회]
[인벤토리 조작 → ItemType 조회]
[제작 요청 → RecipeRegistry 조회]
```

## 후속 작업

1. blocks.json ↔ items.json ID 매핑 통합
2. ItemGroupList 시스템 구현
3. CraftMethod enum 기반 제작 시스템 리팩터링
4. 데이터 드리븐 검증 도구 개선

## 참조
- minetest Project: `minetest_project/src/nodedef.h`, `itemdef.h`, `craftdef.h`
- Unity Data: `Assets/StreamingAssets/blocks.json`, `items.json`, `recipes.json`
