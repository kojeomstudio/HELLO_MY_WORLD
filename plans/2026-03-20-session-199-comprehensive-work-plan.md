# Session 199: 게임 데이터 스키마 확장 및 3중 미러 동기화

## 작업 일자
2026-03-20

## 작업 목표
- minetest `craftdef.h`, `itemdef.h`, `nodedef.h` 구조 기반 게임 데이터 스키마 확장
- items.json에 groups, description, stack_max, tool_capabilities 필드 추가
- recipes.json에 method, craft_time, replacements 필드 추가
- blocks.json에 Groups 필드 추가 (nodedef 그룹 시스템)
- 3중 미러(game-data) 정합성 동기화

## 작업 체크리스트

### 1. 게임 데이터 스키마 확장
- [x] items.json에 minetest ItemDefinition 필드 매핑 (groups, stack_max, tool_capabilities)
- [x] recipes.json에 CraftMethod 타입 추가 (NORMAL/COOKING/FUEL)
- [x] blocks.json에 nodedef 그룹 시스템 반영

### 2. 데이터 파이프라인 검증
- [x] GameDataCatalog.cs 정상 컴파일 확인
- [x] 3중 미러(game-data) 정합성 동기화
  - config/game-data -> GameServer/config/game-data
  - config/game-data -> Assets/StreamingAssets/game-data
  - config/blocks.json -> Assets/StreamingAssets/blocks.json

### 3. 검증 및 Git 반영
- [x] `dotnet build GameServer/GameServer.csproj` 성공
- [ ] 변경사항 커밋
- [ ] `git push origin master`

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| | | |

## 변경 파일 목록
- `config/game-data/items.json` - 스키마 확장 (groups, stack_max, tool_capabilities)
- `config/game-data/recipes.json` - 스키마 확장 (method, craft_time, replacements)
- `config/blocks.json` - Groups 필드 추가
- `GameServer/Systems/GameDataCatalog.cs` - 새로 추가된 데이터 카탈로그
