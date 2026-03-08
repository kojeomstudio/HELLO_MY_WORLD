# HELLO MY WORLD

Minecraft-style client/server sandbox game project with Unity client + .NET server.

## 핵심 구조
- `Assets/`: Unity 클라이언트
- `GameServer/`: .NET 서버 (`Program.cs` 진입점)
- `SharedProtocol/`: 패킷/프로토콜 공통 계층
- `GameCommon/`: 클라/서버 공통 타입을 제공하는 공유 DLL(`GameCommon.dll`)
- `proto/`: Google Protobuf 원본 스키마
- `config/`: 서버/클라이언트 데이터 드리븐 JSON 설정
- `docs/`: 상세 설계/검증 문서
- `plans/`: 세션별 To Do / Completed 작업 계획

## 빠른 시작
```bash
dotnet build GameCommon/GameCommon.csproj
dotnet build SharedProtocol/SharedProtocol.csproj
dotnet build GameServer/GameServer.csproj
dotnet run --project GameServer -- --server
```

## Protobuf 재생성/검증
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/generate_proto.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1
dotnet run --project GameServer -- --proto-probe
```

## 월드맵 컨트롤/지형 생성
- 하이드롤로지 시그니처: `2026-03-08-hydrology-riverlake-cave-v69`
- 맵 컨트롤 최소 프로필 버전: `73`
- 기준 프로필: `config/world_map_control_profile.json`
- 서버/클라 스트리밍 미러:
  - `GameServer/Assets/StreamingAssets/world-map-control.json`
  - `Assets/StreamingAssets/world-map-control.json`

## 데이터 드리븐 정책
- 월드/지형 설정: `config/world.json`
- 맵 런타임 정책: `config/enhanced_world_map_control_server.json`, `config/enhanced_world_map_control_client.json`
- 더미 프로토콜 클라이언트 설정: `config/protocol_dummy_client.json`, `config/dummy_minecraft_client.json`
- 세션 145 기능 분류(코어/콘텐츠/유틸):
  - `config/minecraft_feature_client_server_core_content_util_2026-03-08-session-145.json`

## 문서
- Session 145 구현 보고서: `docs/2026-03-08-session-145-implementation-report.md`
- 최신 작업 계획: `plans/2026-03-08-session-145-comprehensive-work-plan.md`

