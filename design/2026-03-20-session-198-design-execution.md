# Session 198: 디자인 실행 문서

## 작업 일자
2026-03-20

## 디자인 목표
minetest 참조 구조를 기반으로 게임 데이터 스키마를 확장하고, 데이터 드리븐 파이프라인을 강화한다.

## 실행 항목

### 1. 게임 데이터 스키마 확장

#### 1.1 items.json 스키마
- [ ] `stack_max` 필드 추가 (minetest stack_max 대응)
- [ ] `tool_capabilities` 구조체 추가 (dig_speed, damage)
- [ ] `groups` 배열 추가 (아이템 그룹화)
- [ ] `sound_place` 필드 추가

#### 1.2 recipes.json 스키마
- [ ] `method` 필드 추가 (NORMAL/COOKING/FUEL)
- [ ] `craft_time` 필드 추가 (화로 조리 시간)
- [ ] `replacements` 배열 추가 (제작 후 아이템 교체)

#### 1.3 blocks.json 스키마
- [ ] `groups` 배열 추가 (블록 그룹화)
- [ ] `digging` 구조체 추가 (채굴 관련 속성)
- [ ] `sounds` 구조체 추가 (블록 사운드)

### 2. 데이터 파이프라인 검증

#### 2.1 Template Exporter 검증
- [ ] GameDataTemplateExporter 정상 동작 확인
- [ ] 스키마 변경 후 JSON 파싱 검증

#### 2.2 미러 정합성 검증
- [ ] config/game-data ↔ GameServer/config/game-data
- [ ] config/game-data ↔ Assets/StreamingAssets/game-data
- [ ] 파일 동기화 스크립트/프로세스 확인

### 3. 코드 연동 검증

#### 3.1 Unity 클라이언트
- [ ] InventoryManager 스키마 호환성
- [ ] CraftingManager 스키마 호환성

#### 3.2 GameServer
- [ ] InventorySystem 스키마 호환성
- [ ] CraftingHandler 스키마 호환성

## minetest 참조 매핑

| minetest | Our Project | 구현 상태 |
|----------|-------------|----------|
| ItemType | items.json type | ✅ 기본 구현 |
| stack_max | items.json stackable/max_stack | ⚠️ 스키마 정렬 필요 |
| tool_capabilities | items.json tool_capabilities | ❌ 미구현 |
| CraftMethod | recipes.json method | ❌ 미구현 |
| CraftReplacements | recipes.json replacements | ❌ 미구현 |
| ItemGroupList | items.json groups | ❌ 미구현 |

## 산출물
1. 확장된 game-data-template.md
2. 갱신된 JSON 데이터 파일들
3. 본 디자인 실행 문서
4. 아키텍처 분석 문서

## 완료 기준
- [ ] 모든 스키마 확장 항목 완료
- [ ] 데이터 파이프라인 정상 동작
- [ ] Unity/GameServer 양쪽 호환성 확보
- [ ] 컴파일 테스트 통과
