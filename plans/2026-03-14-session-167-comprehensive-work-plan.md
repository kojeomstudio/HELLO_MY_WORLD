# Session 167 Comprehensive Work Plan (2026-03-14)

## Scope
- Verify pre-existing local changes and sync policy before implementation.
- Classify Minecraft client/server features into Core / Content / Utility and publish updated list artifacts.
- Improve cave/river/lake terrain generation algorithms and apply server-client world-map control architecture updates.
- Re-validate protobuf-generated packet protocol references/usages and improve weak points.
- Ensure JSON-driven config/data handling for server/client runtime paths.
- Run compilation/tests and using/reference verification.
- Update README (concise) + detailed docs under `docs/`.
- Commit all staged/modified outputs and push to `origin/master`.

## Reference: Recent Git History
- `d0ce4113` feat(session-166): feature categorization verification + build validation + docs update
- `bd6dc65c` feat(session-166): feature categorization verification + build validation + documentation update
- `96c16f06` feat(session-165): apply hydrology v86 map-control v90 queue/proto parity

## Baseline
- Branch: `master`
- Working tree before start: clean (`git status --short` empty)
- Shared DLL projects: `GameCommon`, `SharedProtocol`
- Dummy protocol probe client: `GameServer/Testing/DummyProtocolClient.cs`

## TODO
- [x] Confirm local workspace cleanliness before starting.
- [x] Draft and publish this plan file in `plans/`.
- [x] Implement terrain generation update (cave/river/lake) for next hydrology signature/version.
- [x] Apply server/client world-map control queue architecture parity update.
- [x] Update feature categorization artifacts (Core/Content/Utility) in JSON + docs.
- [x] Validate protobuf packet generation/reference usage and adjust diagnostics/probe if needed.
- [x] Verify `using`-based references through full build.
- [x] Run compile/test commands (`SharedProtocol`, `GameCommon`, `GameServer`, dummy protocol path).
- [x] Update README + session docs under `docs/`.
- [x] Commit all changes and push to `origin/master`.

## COMPLETED
- [x] Verified branch and recent commit history.
- [x] Verified working tree clean state.
- [x] Initialized session work-plan with TODO/COMPLETED separation.
- [x] Upgraded shared/runtime baseline to hydrology `v87`, map-control profile `v91`.
- [x] Applied terrain generation bridge steps for cave/river/lake (`Improved*Generator`).
- [x] Added hyporheic queue scaling path in shared + server + client world map controllers.
- [x] Synced JSON-driven config/data mirrors across `config/`, `GameServer/config/`, `Assets/StreamingAssets/`.
- [x] Published core/content/utility manifests for session 167 and updated parity manifest references.
- [x] Generated and mirrored world-map control profile (`version=91`, hydrology signature `2026-03-14-hydrology-riverlake-cave-v87`).
- [x] Ran compile/validation commands:
  - `dotnet build SharedProtocol/SharedProtocol.csproj` (success, warnings only)
  - `dotnet build GameCommon/GameCommon.csproj` (success)
  - `dotnet build GameServer/GameServer.csproj` (success, warnings only)
  - `dotnet test GameServer/GameServer.csproj` (restore-only, no failing tests)
  - `dotnet run --project GameServer -- --generate-map-profile` (success)
  - `dotnet run --project GameServer -- --proto-probe` (success, optional packet warnings retained)
  - `dotnet run --project GameServer -- --selftest` (command success; legacy test-suite warnings retained)
- [x] Updated `README.md` and docs for session 167 implementation traceability.
