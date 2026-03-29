# Session 171 Comprehensive Work Plan (2026-03-15)

## Scope
- `work/work.md` 지침 기반 지속 작업
- optional Minecraft 프로토콜 패킷 검증 공백 보완

## Reference: Recent Git History
- `d1ecca90` feat(session-170): organize docs archive and update README
- `4d9a98bb` feat(session-169): fix simplex terrain crash and refresh validation docs
- `979d1393` feat(session-168): apply hydrology v88 map-control v92 queue/proto parity

## Baseline
- Branch: `master`
- Working tree: untracked `work/` input docs
- Focus area: `GameServer/Testing/DummyProtocolClient.cs`

## TODO
- [x] optional 패킷 프로브 폴백(legacy protobuf-net) 설계
- [x] 더미 프로브 optional payload 생성/검증 로직 구현
- [x] proto probe/selftest 기반 검증
- [x] 문서(README/docs) 및 계획 파일 갱신

## COMPLETED
- [x] `work/work.md` 요구사항 재확인
- [x] 최근 작업 이력/현행 계획(`session-170`) 검토
- [x] 세션 171 작업 계획 수립
- [x] `DummyProtocolClient`에 optional legacy payload fallback 추가
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj` (성공, warning only)
- [x] `dotnet build GameServer/GameServer.csproj` (성공, warning only)
- [x] `dotnet run --project GameServer -- --proto-probe` (성공)
- [x] `dotnet run --project GameServer -- --selftest` (성공)
- [x] `reports/proto_probe_report.json` 갱신(legacyFallbackValidated=5)
