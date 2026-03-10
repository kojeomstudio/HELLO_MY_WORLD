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

## 현재 기준 (Session 153)
- Hydrology signature: `2026-03-10-hydrology-riverlake-cave-v76`
- Map control profile version: `80`
- Queue policy version: `34`
- Feature manifest:
  - `config/minecraft_feature_client_server_core_content_util_2026-03-10-session-153.json`
  - `GameServer/config/minecraft_feature_client_server_core_content_util_2026-03-10-session-153.json`
  - `Assets/StreamingAssets/minecraft_feature_client_server_core_content_util_2026-03-10-session-153.json`

## 문서
- Session 153 상세 보고서: `docs/2026-03-10-session-153-implementation-report.md`
- Session 153 작업 계획: `plans/2026-03-10-session-153-comprehensive-work-plan.md`

## 기능 카테고리 (85개 기능)
- **Core (32개)**: 청크 로딩, 블록 배치, 이동, 네트워킹, 인증, 전투 등
- **Content (27개)**: 바이옴, 블록, 아이템, 엔티티, 제작, 동굴/강/호수 생성 등
- **Utility (26개)**: 로깅, 설정, 성능 모니터링, 안티치트, 경로 찾기 등

자세한 내용은 `config/minecraft_feature_client_server_core_content_util_2026-03-10-session-153.json` 참조.
