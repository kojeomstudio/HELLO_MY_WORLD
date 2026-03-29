# Session 185 Validation Report (2026-03-18)

## Summary
이번 세션은 `work/work.md` 요구사항에 맞춰 minetest 참조 기반 문서 정리와 빌드/실행 검증을 수행했다.

## Executed Commands

### 1. Baseline
- `git pull origin master` -> `Already up to date`
- `git submodule status` -> `minetest_project` at `00f670cf289adbd56faa66035661e45437296405`

### 2. Template Export
- `dotnet run --project Tools/GameDataTemplateExporter/GameDataTemplateExporter.csproj -- --input design/templates/game-data-template.md --output config/game-data`
- 결과: 5개 dataset JSON 출력 성공

### 3. Build
- `dotnet build SharedProtocol/SharedProtocol.csproj`
  - 결과: 0 errors, 8 warnings
- `dotnet build GameServer/GameServer.csproj`
  - 결과: 0 errors, 41 warnings

### 4. Runtime Smoke Test
- `dotnet run --project GameServer -- --selftest`
  - 결과: exit code 0

## Key Runtime Metrics
- Proto descriptor fingerprint: expected == computed
- Binding coverage: `14/54` (ratio `0.259`)
- Optional handler coverage: `7/10`
- Missing optional prototype bindings:
  - `MultiBlockChange`
  - `ItemPickup`
  - `EntityInteract`
- Selftest sequence:
  - login/move/chat/ping/block-change 경로 실행
  - 일부 `Unexpected response type` 로그 존재(비치명)

## Refreshed Artifacts
- `config/world_map_control_profile.json`
- `GameServer/config/world_map_control_profile.json`
- `Assets/StreamingAssets/world-map-control.json`
- `GameServer/Assets/StreamingAssets/world-map-control.json`
- `reports/proto_probe_report.json`

## Documentation Hygiene Check
- `docs/plans/design` 내 `.md` 해시 중복 그룹: 없음
- 0-byte `.md` 파일: 없음
- 이번 세션에서 안전하게 삭제 가능한 문서 후보는 발견하지 못함

## Notes
- 콘텐츠/설정은 JSON 중심 데이터 드리븐 흐름을 유지했다.
- 템플릿 추출 도구는 `net8.0`을 사용하므로 도구 버전 요구사항(C# .NET 8~9)을 충족한다.
