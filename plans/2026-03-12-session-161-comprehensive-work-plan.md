# Session 161 Comprehensive Work Plan (2026-03-12)

## Reference: Recent Git History
- `31d9791f` feat(session-160): hydrology v83 / map-control v87 parity + feature categorization + proto validation
- `ad6b13aa` feat(session-159): hydrology v82 / map-control v86 terrain-proto sync
- `1016c16a` docs(session-158): work plan completed update
- `0aacb501` feat(session-158): hydrology v81 / map-control v85 + docs sync
- `4d572a7c` feat(session-157): hydrology v80 / map-control v84 + terrain bridge parity

## Current Baseline Check
- Working tree: clean (`master` == `origin/master`), no pre-existing local changes to commit.
- Existing architecture already has: shared DLL contracts, JSON-driven world config, world-map queue policy, and dummy protobuf clients.
- Remaining action in this session: apply next hydrology/map-control uplift with server/client parity and refresh documentation artifacts.

## TODO
- [x] Core/Content/Util 기능 카탈로그를 세션 161 기준으로 갱신하고 구현 순서를 명시한다.
- [x] 동굴/강/호수 지형 생성 알고리즘을 서버/클라이언트에 동일하게 개선한다.
- [x] 월드맵 제어 아키텍처(큐 제어/부하 대응) 공통 정책을 서버/클라이언트에 반영한다.
- [x] 프로토버퍼 레지스트리/패킷 참조 상태를 점검하고 더미 클라이언트 설정 가드를 상향한다.
- [x] JSON 기반 설정/데이터 드리븐 파일(`world.json`, map profile, queue policy, feature catalog)을 동기화한다.
- [x] 문서(`README.md`, `docs/`)를 최신 변경 기준으로 갱신한다.
- [x] 빌드/테스트(`dotnet build`, proto probe)를 수행해 컴파일 및 프로토콜 동작을 검증한다.
- [x] 종료 시 변경 파일을 커밋하고 `origin/master`에 push 한다.

## COMPLETED
- [x] 저장소 상태/최근 커밋 이력 확인.
- [x] 세션 161 작업 계획 문서 생성.
- [x] HydrologySignature `v83` / MapControlProfileVersion `87` 상향 및 profile 재생성.
- [x] Aquifer Conduit Exchange terrain bridge(서버/클라) 적용.
- [x] WorldMapQueuePolicy v84 queue scale + map-control queue policy v40 동기화.
- [x] Proto probe + dummy client required round-trip 검증 완료(옵션 패킷은 warning 유지).


