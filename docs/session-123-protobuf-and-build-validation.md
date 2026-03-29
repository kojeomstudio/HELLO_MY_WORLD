# Session 123 Build / Protobuf Validation

- Date: 2026-02-25
- Scope: server/client shared protocol, world-map control runtime, dummy probe

## Executed Commands

- `dotnet build SharedProtocol/SharedProtocol.csproj -v minimal`
- `dotnet build GameCommon/GameCommon.csproj -v minimal`
- `dotnet build GameServer/GameServer.csproj -v minimal`
- `dotnet test GameServer/TerrainGenerationTest.csproj -v minimal`
- `powershell -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1`
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe`
- `dotnet run --project GameServer/GameServer.csproj -- --selftest`

## Results

- Build:
  - `SharedProtocol`: PASS (warning only)
  - `GameCommon`: PASS
  - `GameServer`: PASS (warning only)
- Test:
  - `TerrainGenerationTest.csproj`: PASS (실패 없음)
- Protobuf timestamp/hash check:
  - `scripts/verify_protobuf.ps1`: PASS
  - proto source newer drift 없음
- Dummy probe (`--proto-probe`): PASS
  - `RoundTrip=True`
  - Descriptor coverage ratio reported: `0.259`
- Selftest (`--selftest`): PASS

## Important Notes

- Optional protobuf message binding warning은 기존 설계상 optional/미등록 타입에 대한 경고이며, required path 실패는 없음.
- `using` 참조 유효성은 `dotnet build` 경로에서 컴파일러 해석 성공으로 검증됨.
- Unity C# 스크립트는 .NET CLI 단독으로 완전 컴파일되지 않으므로, Unity Editor(Test Runner/Assembly compile)에서 추가 확인 권장.

