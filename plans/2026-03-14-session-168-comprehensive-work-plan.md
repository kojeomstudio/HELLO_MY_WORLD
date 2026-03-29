# Session 168 Comprehensive Work Plan (2026-03-14)

## Scope
- Verify pre-existing local changes and branch sync policy before implementation.
- Classify Minecraft client/server features into Core / Content / Utility and publish updated list artifacts.
- Improve cave/river/lake terrain generation algorithms and apply server-client world-map control architecture update.
- Re-validate protobuf-generated packet protocol references/usages and improve weak points.
- Ensure JSON-driven config/data handling for server/client runtime paths.
- Run compilation/tests and using/reference verification.
- Update README (concise) + detailed docs under `docs/`.
- Commit all staged/modified outputs and push to `origin/master`.

## Reference: Recent Git History
- `c1e85678` feat(session-167): apply hydrology v87 map-control v91 worldgen queue parity
- `d0ce4113` feat(session-166): feature categorization verification + build validation + docs update
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
- [ ] Commit all changes and push to `origin/master`.

## COMPLETED
- [x] Verified branch and recent commit history.
- [x] Verified working tree clean state.
- [x] Initialized session work-plan with TODO/COMPLETED separation.
- [x] Upgraded shared feature baseline to hydrology `v88` / map-control `v92`.
- [x] Added phreatic resonance queue scaling in shared/server/client world-map controllers.
- [x] Added terrain generation resonance bridges for river/lake/cave.
- [x] Synced JSON runtime config and mirrored profile/policy artifacts.
- [x] Published session-168 Core/Content/Utility manifests and mirrored copies.
- [x] Improved proto handler validation scope (`ProtocolValidator`, `MinecraftMessageDispatcher`) to reduce false-positive handler warnings.
- [x] Executed build/test/probe commands:
  - `dotnet build SharedProtocol/SharedProtocol.csproj` (pass)
  - `dotnet build GameCommon/GameCommon.csproj` (pass)
  - `dotnet build GameServer/GameServer.csproj` (pass; existing warnings only)
  - `dotnet test GameServer/GameServer.csproj` (pass; no dedicated test discovery output)
  - `dotnet run --project GameServer -- --generate-map-profile` (pass; profile version/signature synchronized to `v92`/`v88`)
  - `dotnet run --project GameServer -- --proto-probe` (pass; descriptor fingerprint and round-trip validated)
  - `dotnet run --project GameServer -- --selftest` (process pass; existing runtime issue remains: `Terrain stage 'base-terrain' failed for chunk (0,0)` and legacy response-order mismatches)
