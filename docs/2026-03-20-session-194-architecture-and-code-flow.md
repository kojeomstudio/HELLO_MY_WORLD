# Session 194: 아키텍처 및 코드 흐름 분석 - 블록/아이템 ID 통합

## 작성 일자
2026-03-20

## 목적
`minetest_project`의 `NodeDef`/`ItemDef` 구조를 기준으로 현재 Unity 클라이언트와 .NET 서버의 블록/아이템 ID 체계를 분석하고, 통합 식별체계 설계를 위한 개선 사항을 도출한다.

---

## 1. minetest 참조 구조

### 1.1 아이템 타입 분류
```cpp
// minetest_project/src/itemdef.h
enum ItemType : u8
{
    ITEM_NONE,
    ITEM_NODE,    // 블록 (배치 가능)
    ITEM_CRAFT,   // 제작 아이템
    ITEM_TOOL,    // 도구 (내구도 있음)
};
```

### 1.2 ItemDefinition 구조
- `type`: ItemType (NODE/CRAFT/TOOL)
- `name`: 문자열 식별자 (예: "default:stone")
- `description`: 표시명
- `inventory_image`: 인벤토리 이미지
- `color`: 기본 색상

### 1.3 ContentFeatures (NodeDef)
```cpp
// minetest_project/src/nodedef.h
class ContentFeatures
{
    // 기본 속성
    std::string name;
    ItemGroupList groups;
    
    // 시각 속성
    TileDef tiledef[6];
    NodeDrawType drawtype;
    u8 param_type;
    u8 param_type_2;
    
    // 물리 속성
    bool walkable;
    bool climbable;
    bool buildable_to;
    
    // 액체 속성
    LiquidType liquid_type;
    
    // 상호작용
    std::string drop;
};
```

### 1.4 설계 관점 시사점
- **이름 기반 식별**: 문자열 name을 기본 식별자로 사용, content_t (u16)으로 매핑
- **통합 정의**: 블록과 아이템이 동일한 ItemDefinition 구조에서 출발
- **그룹 시스템**: ItemGroupList를 통한 속성 그룹화
- **드롭 정의**: 블록 파괴 시 드롭될 아이템을 문자열로 정의

---

## 2. 현재 프로젝트 코드 흐름 분석

### 2.1 블록 타입 정의 중복 문제
**GameCommon/Blocks/BlockType.cs**:
- 155개 블록 타입 정의
- enum 기반 정수 ID

**GameServer/Models/BlockType.cs**:
- 21개 블록 타입만 정의
- **다른 번호 매핑** (예: Grass=2 vs Grass=2 일치하지만 Water=8 vs Water=8 등 불일치 존재)

### 2.2 아이템 데이터 구조
**config/game-data/items.json**:
```json
[
  {"id": "wooden_pickaxe", "type": "tool", "durability": 59},
  {"id": "bread", "type": "food", "hunger_restore": 5},
  {"id": "iron_ingot", "type": "material"}
]
```

### 2.3 정합성 이슈
1. **이원화된 식별 체계**: enum 기반 BlockType vs 문자열 기반 item id
2. **타입 체계 불일치**: BlockType은 별도, items.json은 type 필드 사용
3. **서버-클라 BlockType 불일치**: GameServer와 GameCommon의 enum이 다름

---

## 3. 개선 방향

### 3.1 통합 아이템/블록 정의 체계 (minetest 방식)
```
ItemDefinition (공통)
├── ItemType: Node | Craft | Tool | Food
├── name: string (고유 식별자)
├── description: string
├── stackable: bool
├── max_stack: int
└── [Node 전용]
    ├── solid: bool
    ├── transparent: bool
    ├── liquid: bool
    └── drop: string (드롭 아이템)
```

### 3.2 ID 매핑 시스템
- **이름 → ID**: Dictionary<string, ushort> 매핑
- **ID → 정의**: ItemDefinition[] 배열
- **런타임 등록**: JSON 로딩 시 자동 매핑

### 3.3 서버-클라 동기화
- GameCommon에 `ItemRegistry` 클래스 배치
- 서버와 클라이언트 모두 동일 JSON 로드
- 프로토콜에서는 ushort ID 사용

---

## 4. 네트워크 계층 개선 분석

### 4.1 현재 GameNetworkManager 상태
- `CPacket` 기반 레거시 프로토콜 유지
- `SharedProtocol.EnhancedMinecraft` protobuf 참조만 존재
- 실제 protobuf 전송 미구현

### 4.2 개선 방향
1. protobuf 메시지로 통일
2. 기존 NetProtocol enum을 protobuf message type으로 매핑
3. 서버 핸들러와 클라이언트 콜백을 protobuf 기반으로 리팩터

---

## 5. 남은 개선 포인트
1. GameServer/Models/BlockType.cs 제거 → GameCommon/Blocks/BlockType.cs로 통일
2. items.json에 블록 정의 포함 (type: "node")
3. ItemRegistry 구현 및 JSON 로딩
4. GameNetworkManager protobuf 전송 계층 구현

---

## 참조 파일
- `minetest_project/src/itemdef.h`
- `minetest_project/src/nodedef.h`
- `minetest_project/src/inventorymanager.h`
- `GameCommon/Blocks/BlockType.cs`
- `GameServer/Models/BlockType.cs`
- `config/game-data/items.json`
