# Session 197 Design Execution (2026-03-20)

## 1. Design Goal
minetest 참조 구조를 기반으로, Unity/.NET 환경에서 데이터 드리븐 Minecraft 모작 개발을 안정적으로 지속할 수 있는 실행 기준을 확정한다.

## 2. Minetest Reference Scope
- `minetest_project/src/itemdef.h`: 아이템 타입/스택/툴 능력치 모델
- `minetest_project/src/nodedef.h`: 블록 물성/파라미터 모델
- `minetest_project/src/craftdef.h`: 제작 입력/출력 모델
- `minetest_project/src/nameidmapping.h`: 문자열 ID 중심 매핑 전략

## 3. Session 197 Design Decisions

### A. Canonical Data Location
- 정본 데이터는 `config/game-data/*.json`으로 단일화한다.
- 서버/클라이언트는 정본을 직접 사용하거나 미러 복제본을 사용한다.

### B. Runtime Mirror Strategy
- 서버 시작 단계에서 parity manifest 기반으로 미러를 최신화한다.
- 미러 대상:
  - `GameServer/config/game-data/*.json`
  - `Assets/StreamingAssets/game-data/*.json`

### C. Loader Fallback Policy
- 로더는 `game-data` 경로를 우선 사용한다.
- 기존 경로(`config/items.json`, `config/recipes.json`)는 구버전 호환 폴백으로 유지한다.

### D. Documentation Lifecycle Policy
- minetest 도입 이전이거나 현재 베이스와 불일치한 `docs/plans`는 삭제한다.
- 신규 문서는 세션 단위로 `plans + docs + design` 3종을 함께 유지한다.

## 4. Execution Backlog

### Phase 1: Data Contract Unification
1. `blocks`/`items`/`recipes` 간 문자열 ID 참조 규칙 고정
2. 제작법에서 `CraftMethod(normal/cooking/fuel)` 모델 추가
3. 툴/방어구/소모품 타입 확장을 minetest 분류 체계에 맞춰 반영

### Phase 2: Validation Hardening
1. 서버 시작 검증 실패 시 원인 JSON 경로/필드명을 명시
2. 템플릿(`design/templates/game-data-template.md`)과 서버 검증 규칙 자동 비교
3. CI 단계에서 parity drift를 실패 조건으로 승격

### Phase 3: Content Expansion
1. 몬스터/NPC 데이터셋에 행동 패턴/전리품 테이블 확장
2. 캐릭터 스탯에 레벨/장비 보정 모델 추가
3. 생존 루프(채집-제작-전투-건축) 기준 기본 콘텐츠 팩 확장

## 5. Completion Criteria
- 빌드 성공: `SharedProtocol`, `GameServer`
- game-data 정합성 검증 통과
- 문서 3종(`plans/docs/design`) 갱신 완료
- 변경 사항 커밋 및 원격 반영
