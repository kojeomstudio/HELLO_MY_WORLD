# Session 165 Comprehensive Work Plan (2026-03-13)

## Scope
- 분산된 기존 구현을 기준으로 이번 세션의 증분 개선을 적용한다.
- 클라이언트/서버 기능을 Core, Content, Util로 재정리하고 세션 산출물(JSON)로 고정한다.
- 동굴/강/호수 지형 생성 알고리즘의 결합 안정성을 추가 개선한다.
- 월드맵 제어(서버/클라이언트) 큐 정책 및 시그니처 검증 경로를 강화한다.
- Protobuf 생성 코드 참조/바인딩/더미 프로브 검증 범위를 확장한다.
- 문서(README + docs)와 설정(JSON) 동기화를 완료한다.

## Reference: Recent Git History
- `81e768a0` feat(session-164): feature categorization core/content/util + hydrology v85 validation + map-control v89 verification
- `d2b12605` feat(session-163): apply hydrology v85 map-control v89 recharge cascade parity
- `78ab4d31` feat(session-162): hydrology v84 map-control v88 subterranean flow conduit + feature categorization
- `6db762f1` docs(session-161): mark work plan completed
- `b0945e05` feat(session-161): apply hydrology v83 map-control v87 aquifer conduit parity

## Baseline
- Branch: `master` tracking `origin/master`
- Working tree before start: clean
- Shared DLL projects: `GameCommon`, `SharedProtocol`
- Dummy probe client: `GameServer/Testing/DummyProtocolClient.cs`
- Proto source: `proto/*.proto`
- Generated Protobuf DTOs: `Assets/Generated/Protobuf/*.cs`

## TODO
- [x] 기능 카탈로그(Core/Content/Util) 세션 165 JSON 작성
- [x] SharedFeatureCatalog 시그니처/프로필 버전 증분 반영(v86/v90)
- [x] Cave/River/Lake 알고리즘 결합 안정성 증분 적용
- [x] World map control 서버/클라 큐 정책 파라미터 증분 반영
- [x] Protobuf 더미 프로브에서 생성 소스 신선도 검증 범위 확장
- [x] 설정 JSON(world/profile/queue/dummy client) 세션 165 값 반영
- [x] docs/와 README 요약 갱신
- [x] 빌드/테스트/dummy proto probe 검증
- [x] using/참조 무결성 점검
- [x] 최종 커밋 + origin/master push

## COMPLETED
- [x] 작업 시작 전 로컬 변경사항 및 브랜치 상태 점검
- [x] 최근 커밋 이력 기반 이번 세션 작업 계획 수립
- [x] 지형 생성(v86) 개선 메서드 적용(Cave/River/Lake)
- [x] 월드맵 큐 정책(v90) karst-spillway 가중치 서버/클라 반영
- [x] 프로토 생성 소스 신선도 검증 범위 동적 계산으로 확대
- [x] JSON parity/manifest 경로 session 165 기준 동기화
- [x] session 165 문서화 완료(README + docs + latest 포인터)
