# HELLO MY WORLD

Unity 클라이언트 + .NET 서버 기반 Minecraft 스타일 샌드박스 프로젝트입니다.

## 핵심 구조
- `Assets/`: Unity 클라이언트 코드/리소스
- `GameServer/`: .NET 서버
- `SharedProtocol/`: 프로토콜/직렬화 공용 코드
- `GameCommon/`: 클라/서버 공용 DLL (`GameCommon.dll`)
- `proto/`: Google Protobuf 스키마
- `config/`: JSON 기반 설정/데이터
- `docs/`: 상세 문서
- `plans/`: 세션별 To Do / Completed 작업 계획

## 빌드
```bash
dotnet build GameCommon/GameCommon.csproj
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameServer/GameServer.csproj
```

## 실행 / 검증
```bash
dotnet run --project GameServer -- --server
dotnet run --project GameServer -- --proto-probe
dotnet run --project GameServer -- --generate-map-profile
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1
```

## 현재 기준 (Session 151)
- Hydrology signature: `2026-03-10-hydrology-riverlake-cave-v74`
- Map control profile version: `78`
- Queue policy version: `32`
- Feature manifest:
  - `config/minecraft_feature_client_server_core_content_util_2026-03-10-session-151.json`
  - `GameServer/config/minecraft_feature_client_server_core_content_util_2026-03-10-session-151.json`

## 문서
- Session 151 상세 보고서: `docs/2026-03-10-session-151-implementation-report.md`
- Session 151 작업 계획: `plans/2026-03-10-session-151-comprehensive-work-plan.md`
