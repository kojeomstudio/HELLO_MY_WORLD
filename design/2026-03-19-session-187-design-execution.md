# Session 187 Design Execution (2026-03-19)

## 1. Design Goal
`work/work.md` 지시와 직전 세션 갭을 반영하여, optional Minecraft packet 3종(`MultiBlockChange`, `ItemPickup`, `EntityInteract`)의 서버 수용 경로를 보강한다.

## 2. Required Minetest References
- `minetest_project/src/network/serverpackethandler.cpp`
  - `handleCommand_Interact`에서 상호작용 권한/거리/상태를 서버에서 검증
  - 상호작용 결과 후 `SendInventory`/`SetBlockNotSent`로 클라이언트 상태를 서버 권위로 보정
- `minetest_project/src/server.cpp`
  - `SendActiveObjectRemoveAdd`/SAO 메시지 라우팅으로 엔티티 상태를 중앙 루프에서 관리
- `minetest_project/src/serverenvironment.cpp`
  - active object step과 mapblock 변경 통지를 분리해 주기적으로 처리

## 3. Design Rules (This Session)
- optional 패킷은 즉시 Google.Protobuf required 세트로 승격하지 않는다.
- descriptor 미생성 상태 패킷은 legacy protobuf-net 계약을 통해 안전하게 수용한다.
- 서버 권위 원칙을 유지하기 위해 본 세션의 신규 핸들러는 "승인/무시 응답" 중심으로 구현한다.
- 실제 게임 로직(월드 반영, 엔티티 상호작용, 아이템 수량 확정)은 후속 단계에서 시스템과 연결한다.

## 4. Packet-by-Packet Execution Plan
1. `MultiBlockChange`
- request/response 계약을 분리하고, 배치 변경 목록을 서버가 수용 가능한 형태로 직렬화한다.
- 현재 단계에서는 각 항목을 acknowledged 결과로 반환해 네트워크 경로와 디스패처 결합을 검증한다.

2. `ItemPickup`
- 엔티티 식별자 + 요청 수량 기반 요청 계약을 추가한다.
- 응답에는 수령 아이템/남은 수량/시퀀스를 포함하여 후속 인벤토리 시스템 연동 포인트를 남긴다.

3. `EntityInteract`
- 상호작용 타입(enum) + 대상 엔티티 ID + 사용 아이템을 포함한 요청 계약을 추가한다.
- 응답은 성공 여부/메시지/시퀀스 중심으로 구성해 상태 전이 기반 확장을 가능하게 한다.

## 5. Data-Driven Alignment
- 이번 변경은 네트워크 계약/핸들러 계층 보강이며, 콘텐츠 데이터는 기존 JSON 기반(`config/game-data/*.json`) 흐름을 유지한다.
- 후속으로 상호작용 룰(허용 거리, 타입별 권한, 아이템 소모 규칙)은 JSON 구성값으로 외부화한다.

## 6. Done in This Session
- optional 3종 메시지 계약(legacy protobuf-net) 추가
- optional 3종 핸들러 추가 및 디스패처 등록
- DummyProtocolClient fallback payload 3종 추가
- selftest 기준 optional coverage `10/10`, missing optional prototypes `0` 달성
