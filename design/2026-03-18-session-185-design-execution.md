# Session 185 Design Execution (2026-03-18)

## 1. Design Goal
minetest 구조를 기준 레퍼런스로 삼아, 현재 프로젝트의 Minecraft 모작 개발 우선순위를 명확히 하고 코어/콘텐츠 개발 시 따라야 할 실행 규칙을 고정한다.

## 2. Reference Baseline (Required)
- Submodule: `minetest_project`
- Core references:
  - `src/main.cpp` (런타임 엔트리/옵션 분기)
  - `src/server.cpp` (서버 루프/월드 시뮬레이션)
  - `src/client/client.cpp` (클라이언트 상태/네트워크 반영)
  - `src/network/networkprotocol.h` (프로토콜 명세 구조)
  - `src/emerge.cpp` (맵 생성 큐/스레드 관리)
  - `doc/world_format.md` (월드 저장/맵블록 포맷 개념)
  - `doc/protocol.txt` (핸드셰이크/프로토콜 흐름 개념)

## 3. Core Experience Targets
- Exploration: 바이옴/하천/호수/동굴 기반 탐험
- Building: 서버 권위형 블록 설치/파괴 루프
- Progression: 인벤토리/제작/전투/체력-허기 루프
- Multiplayer: 룸 기반 접속, 채팅, 동기화된 월드 상태

## 4. Execution Rules
- 코어/콘텐츠 구현 전 반드시 `design/*.md` 최신 문서를 참조한다.
- 프로토콜/핸들러 추가 시:
  1. `proto/*.proto` 계약 반영
  2. generated DTO 갱신
  3. `ProtocolRegistry`/`ProtocolValidator` 정합성 유지
  4. selftest 통과 확인
- 상수/튜닝/콘텐츠 데이터는 코드 하드코딩 대신 JSON으로 관리한다.
- 게임 데이터(`items`, `recipes`, `monsters`, `npcs`, `character_stats`)는 JSON을 런타임 소스로 사용한다.

## 5. Data-Driven Content Authoring Flow
1. 템플릿 작성: `design/templates/game-data-template.md`
2. 추출 도구 실행: `Tools/GameDataTemplateExporter` (`net8.0`)
3. 출력: `config/game-data/*.json`
4. 서버 실행 전 빌드 + selftest로 검증

## 6. Current Gap and Next Design Actions
- Gap:
  - optional protobuf 패킷 일부가 미바인딩/미핸들러 상태
  - selftest에 비치명 응답 타입 불일치 로그 존재
- Next:
  1. optional packet 3종(`MultiBlockChange`, `ItemPickup`, `EntityInteract`) 우선 바인딩/핸들러 보강
  2. 테스트 클라이언트 응답 시퀀스와 서버 응답 타입 매핑 재정렬
  3. `minetest_project/doc/world_format.md` 기반으로 월드/청크 영속 포맷 점검 문서 추가

## 7. Done in This Session
- minetest 참조 기반 아키텍처/코드흐름 문서 업데이트
- 검증 리포트 작성
- 템플릿 추출, 빌드, selftest 실행 결과 반영
