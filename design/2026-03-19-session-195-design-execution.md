# Session 195 Design Execution (2026-03-19)

## Objective
서버 내부 월드 데이터(레거시 BlockType ID)를 깨지 않으면서, 클라이언트/공유 규약 기준 블록 ID 정합성을 네트워크 계층에서 확보한다.

## Design Constraints
- minetest `NameIdMapping`처럼 내부 표현과 전송 표현을 분리한다.
- 기존 DB/청크 저장 포맷은 유지한다.
- 블록 변경 요청/브로드캐스트/Enhanced 청크 페이로드는 정규화 ID를 사용한다.
- 구현은 기존 C#/.NET 서버 코드와 호환되어야 한다.

## Minetest Reference
- `minetest_project/src/nameidmapping.h`: 이름/ID 양방향 매핑 모델
- `minetest_project/src/itemdef.h`: 아이템 정의와 식별의 분리 구조
- `minetest_project/src/nodedef.h`: 콘텐츠(노드) 정의와 런타임 매핑 계층

## Design Decisions

### 1. 전용 매핑 계층 추가
- `BlockTypeProtocolMapper`
- 책임:
  - protocol/raw ID -> server enum 변환
  - server enum -> protocol ID 변환
  - 청크 2바이트 블록 배열 변환

### 2. 요청 처리 정책
- 유효성 검사는 mapper 기준으로 수행한다.
- 월드 반영은 해석된 서버 enum 사용.
- 외부 재전송 값은 mapper의 protocol ID 사용.

### 3. 청크 페이로드 정책
- Legacy 필드(`CompressedBlockData`)는 기존 값을 유지.
- Enhanced payload는 protocol ID로 변환 후 압축하여 전송.
- 이중 경로로 점진적 마이그레이션 안전성 확보.

## Flow

1. `WorldBlockChangeRequest` 수신
2. `TryProtocolToServer` 변환
3. 서버 월드 갱신
4. `ToProtocol`로 브로드캐스트 정규화
5. 청크 응답 시 Enhanced payload에 `ConvertChunkBlockDataToProtocol` 적용

## Follow-up
- 서버 내부 `BlockType`를 `GameCommon` 기준으로 단일화하는 단계적 마이그레이션 계획 수립
- 청크 DB 복구/마이그레이션 유틸리티 설계
- Legacy/Enhanced 클라이언트별 전송 정책 최종 통합
