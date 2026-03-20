# Session 197: game-data 정합성 기준 아키텍처/코드 흐름

## 개요
본 문서는 minetest 참조 구조를 기준으로, Unity 클라이언트와 .NET 서버가 공유하는 게임 데이터(JSON) 로딩 흐름을 정리하고 Session 197에서 반영한 정합성 개선 사항을 기록한다.

## minetest 참조 기준
- `minetest_project/src/itemdef.h`: 아이템 정의(ItemDefinition)와 타입 체계
- `minetest_project/src/nodedef.h`: 노드(블록) 특성(ContentFeatures)
- `minetest_project/src/craftdef.h`: 제작 입력/출력과 제작 방식
- `minetest_project/src/nameidmapping.h`: 이름 기반 식별자 매핑 패턴

핵심 참조 원칙:
- 식별은 문자열 ID를 정본으로 유지하고, 런타임에서 필요한 숫자 ID를 파생한다.
- 콘텐츠 데이터는 코드 하드코딩보다 JSON 데이터셋으로 유지한다.
- 제작/아이템/엔티티 데이터는 동일한 스키마 규칙으로 검증한다.

## 현재 데이터 흐름

### 1. 정본 데이터셋
- 정본 경로: `config/game-data/*.json`
- 대상 파일: `items.json`, `recipes.json`, `monsters.json`, `npcs.json`, `character_stats.json`

### 2. 서버 시작 시 동기화/검증
- `GameServer/Program.cs`에서 `ValidateConfigParityManifest()` 후 `ValidateGameDataDatasets()` 순으로 수행한다.
- `config/config_parity_manifest.json`에 선언된 미러 대상으로 파일을 복제/동기화한다.
- 데이터셋 필수 속성 및 루트 타입(array/object)을 검증한다.

### 3. 클라이언트 로딩 우선순위
- `InventoryManager`:
  1. `Assets/StreamingAssets/game-data/items.json`
  2. `Assets/StreamingAssets/items.json`
  3. `config/game-data/items.json`
  4. `config/items.json` (레거시 폴백)
- `CraftingManager`:
  1. `Assets/StreamingAssets/game-data/recipes.json`
  2. `Assets/StreamingAssets/recipes.json`
  3. `config/game-data/recipes.json`
  4. `config/recipes.json` (레거시 폴백)

## Session 197 변경 사항

### A. 데이터 경로 우선순위 정렬
- 파일: `Assets/MyAssets/Scripts/GameWorld/InventoryManager.cs`
- 파일: `Assets/MyAssets/Scripts/GameWorld/CraftingManager.cs`
- 변경 내용:
  - 기본 상대 경로를 `config/game-data/*.json`으로 변경
  - `StreamingAssets/game-data` 경로를 우선 탐색
  - 후보 경로 중복 제거(HashSet) 적용
  - 레거시 경로(`config/items.json`, `config/recipes.json`)는 마지막 폴백으로 유지

### B. parity manifest 확장
- 파일: `config/config_parity_manifest.json`
- 추가 그룹:
  - `game-data-items`
  - `game-data-recipes`
  - `game-data-monsters`
  - `game-data-npcs`
  - `game-data-character-stats`
- 미러 대상:
  - `GameServer/config/game-data/*.json`
  - `Assets/StreamingAssets/game-data/*.json`

### C. 미러 파일 실제 동기화
- 생성/갱신 경로:
  - `GameServer/config/game-data/`
  - `Assets/StreamingAssets/game-data/`
- 정본(`config/game-data`) 기준으로 5개 데이터셋을 동기화함.

## 코드 흐름(요약)

```text
[Canonical Data]
config/game-data/*.json
    ↓
[Parity Sync]
ValidateConfigParityManifest()
    ↓
[Schema Validation]
ValidateGameDataDatasets()
    ↓
[Runtime Load]
InventoryManager / CraftingManager candidate paths
    ↓
[Gameplay]
인벤토리 조회, 제작 레시피 판정, 몬스터/NPC/스탯 데이터 활용
```

## 문서 정리 정책 반영
- minetest 도입 이전(2026-03-18 이전) 또는 정합성 낮은 `docs/`, `plans/` 문서를 삭제했다.
- 이후 세션 문서는 minetest 참조 기반 아키텍처/설계/실행 문서만 유지한다.

## 후속 권장 작업
1. `blocks.json`도 동일한 `game-data` 체계로 흡수해 item/block 참조를 단일 문자열 ID로 통합
2. `GameDataTemplateExporter` 출력 검증 규칙을 서버 검증 규칙과 동일하게 맞춘 CLI 검사 모드 추가
3. CI에서 game-data parity 및 스키마 검증을 독립 단계로 분리
