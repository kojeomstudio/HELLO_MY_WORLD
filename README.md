# HelloMyWorld

Unity 클라이언트 + .NET 서버 기반의 Minecraft 스타일 프로젝트입니다.

## Quick Start
- 서버 빌드: `dotnet build SharedProtocol/SharedProtocol.csproj && dotnet build GameServer/GameServer.csproj`
- 서버 실행: `dotnet run --project GameServer -- --server`
- 서버+더미 검증: `dotnet run --project GameServer -- --selftest`
- 월드맵 프로필 재생성: `dotnet run --project GameServer -- --generate-map-profile`
- 프로토 프로브: `dotnet run --project GameServer -- --proto-probe`
- 더미 클라이언트: `dotnet run --project Tools/DummyMinecraftClient/DummyMinecraftClient.csproj -- --config config/dummy_minecraft_client.json --required-only`

## Core Paths
- Unity scripts: `Assets/MyAssets/Scripts/`
- Server: `GameServer/`
- Shared DLL contracts: `GameCommon/`, `SharedProtocol/`
- Proto sources: `proto/`
- Runtime configs(JSON): `config/`, `GameServer/config/`, `Assets/StreamingAssets/`

## Architecture
- **Client**: Unity 6 (6000.0.23f1), .NET Standard 2.1
- **Server**: .NET 6.0, SQLite, Protocol Buffers
- **Shared DLLs**: GameCommon (blocks, world, config), SharedProtocol (messages, dispatchers)
- **Terrain**: Hydrology v90 (rivers, lakes, caves), Map Control v94

## Documentation
- 작업 계획: `plans/2026-03-20-session-197-comprehensive-work-plan.md`
- 아키텍처/코드 흐름: `docs/2026-03-20-session-197-architecture-and-code-flow.md`
- 게임 기획 실행: `design/2026-03-20-session-197-design-execution.md`
- 데이터 템플릿 파이프라인: `design/2026-03-16-game-data-template-pipeline.md`
- 템플릿 -> JSON 도구: `Tools/GameDataTemplateExporter/`
- Core/Content/Util 분류: `config/minecraft_features_client_server_core_content_util_2026-03-16-session-175.json`
- 세션 문서: `plans/2026-*.md`, `docs/2026-*.md`, `design/2026-*.md`
- 정리 기준: minetest 도입(2026-03-18) 이전 문서는 정합성 기준으로 정리함

