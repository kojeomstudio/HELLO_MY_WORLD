# Session 193 Design Execution (2026-03-19)

## Objective
minetest 참조 구조를 기반으로 Unity 인벤토리/제작 기능을 데이터 드리븐(JSON) 방식으로 정렬하고, 서버 권한형 구조와 호환되는 클라이언트 데이터 계층을 확립한다.

## Design Constraints
- 게임 데이터는 JSON 기반이어야 한다.
- 템플릿은 Markdown, 런타임 데이터는 JSON으로 유지한다.
- Unity 클라이언트 로직은 `StreamingAssets` 우선 로딩을 사용한다.
- minetest의 서버 분리 구조(환경/생성/인벤토리/모드 저장)와 유사한 책임 분리를 유지한다.

## Minetest Reference
- `minetest_project/src/server.h`: 서버 책임 분리와 매니저 구조
- `minetest_project/src/server.cpp`: 수신 루프 + 명령 핸들러 패턴
- `minetest_project/src/emerge.h`: 청크 생성 큐 관리
- `minetest_project/src/serverenvironment.h`: 월드/엔티티 수명주기

## Data Model Decisions
1. Inventory item 식별자는 런타임에서 정수 ID로 취급한다.
2. JSON에서 문자열 item key가 제공되면 key->id 매핑을 통해 해석한다.
3. 레시피는 다음 두 형식을 모두 허용한다.
   - `{"recipes": [...]}`
   - `[...]`
4. 로딩 우선순위는 다음과 같다.
   - `Assets/StreamingAssets/*.json`
   - `config/*.json`
   - `config/game-data/*.json`

## Flow Design
1. `InventoryManager`가 시작 시 아이템 DB를 JSON에서 로드한다.
2. `CraftingManager`는 `InventoryManager`의 key->id 매핑을 이용해 레시피를 해석한다.
3. 제작 가능 판정 시 인벤토리 수량을 참조하고, 완료 시 재료 차감/결과 지급을 수행한다.
4. JSON 파싱 실패 또는 데이터 부족 시 기본 내장 레시피/아이템으로 폴백한다.

## Expected Outcome
- JSON 데이터 변경만으로 인벤토리/제작 규칙 확장이 가능하다.
- 중복 클래스 정의 제거로 Unity 컴파일 안정성이 개선된다.
- minetest 기준의 데이터-로직 분리 방향과 정합성이 높아진다.
