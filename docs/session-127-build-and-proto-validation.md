# Session 127 Build and Protobuf Validation

- Date: 2026-02-26
- Session: 127

## Executed Commands

- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameCommon/GameCommon.csproj`
- `dotnet build GameServer/GameServer.csproj`
- `dotnet build Tools/DummyMinecraftClient/DummyMinecraftClient.csproj`
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1`
- `dotnet run --project GameServer/GameServer.csproj -- --proto-probe`

## Results

- Build:
  - `SharedProtocol`: PASS
  - `GameCommon`: PASS
  - `GameServer`: PASS
  - `DummyMinecraftClient`: PASS
- Profile generation:
  - PASS (`version=60`, `signature=2026-02-26-hydrology-riverlake-cave-v56` in synchronized profile files)
- Protobuf source/generated freshness:
  - `scripts/verify_protobuf.ps1`: PASS
  - generated protobuf C# files are up-to-date relative to `proto/`.
- Proto probe:
  - PASS (`RoundTrip=True`)
  - descriptor coverage ratio: `0.259`
  - optional packets without registry bindings remain warning-level and are tracked.

## Validation Interpretation

- Required packet registry path is operational.
- Fingerprint validation remains consistent (`Expected == Computed`).
- Optional packet gaps do not block runtime probe success but should be closed when promoted to required contracts.
