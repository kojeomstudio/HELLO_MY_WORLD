# Session 196: 게임 데이터 정합성 개선 설계

## 개요
본 문서는 minetest 데이터 모델을 참조하여 Unity 프로젝트의 게임 데이터(blocks.json, items.json, recipes.json) 정합성을 개선하기 위한 설계 내용을 기술한다.

## 문제 분석

### 1. ID 체계 불일치
- **blocks.json**: PascalCase 식별자 (예: "Stone", "CoalOre")
- **items.json**: snake_case 식별자 (예: "stone", "coal_ore")
- **결과**: 블록 파괴 시 드롭 아이템 매핑 오류 가능

### 2. 구조적 차이
| 구분 | blocks.json | items.json |
|------|-------------|------------|
| 키 이름 | BlockTypes | items.blocks |
| ID 필드 | Id (int) | id (int) |
| 이름 필드 | Name, DisplayName | name, displayName |
| 분류 | 없음 | categories |

### 3. recipes.json 참조 문제
```json
{
  "ingredients": [{"itemId": "log", ...}],  // snake_case
  "results": [{"itemId": "wood_planks", ...}]
}
```
- blocks.json에는 "Log" (PascalCase)로 정의됨
- 매핑 불일치로 제작 시스템 오류 가능

## 설계 방안

### Phase 1: ID 통합 매핑 (후속 세션)

```json
// id_mapping.json
{
  "blocks": {
    "air": {"id": 0, "aliases": ["Air"]},
    "stone": {"id": 1, "aliases": ["Stone"]},
    "dirt": {"id": 3, "aliases": ["Dirt"]}
  },
  "items": {
    "coal": {"id": 263, "aliases": ["Coal"]},
    "iron_ingot": {"id": 265, "aliases": ["IronIngot"]}
  }
}
```

### Phase 2: 그룹 시스템 도입

minetest ItemGroupList 패턴 적용:
```json
{
  "groups": ["cracky=3", "stone=1"]
}
```

Unity 적용:
```json
{
  "groups": {
    "cracky": 3,
    "stone": true
  }
}
```

### Phase 3: 제작 메서드 분류

```csharp
public enum CraftMethod
{
    Normal,   // 일반 제작대
    Cooking,  // 화로 조리
    Fuel,     // 연료
    Brewing,  // 양조
    Smithing  // 대장장이
}
```

## 구현 계획

### 1단계 (Session 196)
- [x] blocks.json 중복 데이터 수정
- [x] 오래된 세션 JSON 파일 정리
- [x] minetest 데이터 모델 분석 문서화

### 2단계 (Session 197 예정)
- [ ] ID 매핑 통합 시스템 구현
- [ ] BlockType ↔ ItemType 변환 유틸리티
- [ ] 그룹 시스템 기본 구조

### 3단계 (Session 198 예정)
- [ ] CraftMethod enum 기반 제작 시스템 리팩터링
- [ ] 레시피 검증 로직 강화
- [ ] 데이터 드리븐 검증 도구 개선

## 데이터 스키마 개선안

### blocks.json v2 (제안)
```json
{
  "version": "2.0",
  "blocks": {
    "stone": {
      "id": 1,
      "displayName": "Stone",
      "groups": {"cracky": 3, "stone": true},
      "hardness": 1.5,
      "resistance": 6.0,
      "drops": [{"item": "cobblestone", "count": 1}]
    }
  }
}
```

### items.json v2 (제안)
```json
{
  "version": "2.0",
  "items": {
    "cobblestone": {
      "id": 4,
      "type": "block",
      "displayName": "Cobblestone",
      "stackSize": 64,
      "groups": {"cracky": 3, "stone": true}
    }
  }
}
```

## 검증 항목

1. [ ] 모든 블록 ID가 아이템 ID와 일치하는지 확인
2. [ ] 모든 레시피 재료/결과물이 items.json에 존재하는지 확인
3. [ ] 모든 드롭 아이템이 items.json에 정의되어 있는지 확인

## 참조
- minetest Project: `minetest_project/src/`
- Unity Data: `Assets/StreamingAssets/`
- Tools: `Tools/GameDataTemplateExporter/`
