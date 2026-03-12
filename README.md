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

## Documentation
- Session 162 변경 보고서: `docs/2026-03-12-session-162-implementation-report.md`
- 작업 계획: `plans/2026-03-12-session-162-comprehensive-work-plan.md`
- 기존 분석/설계 문서: `docs/`

