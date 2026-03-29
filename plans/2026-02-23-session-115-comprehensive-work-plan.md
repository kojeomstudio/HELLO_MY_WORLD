# Session 115 Comprehensive Minecraft Implementation Work Plan

**Date**: 2026-02-23  
**Session**: 115  
**Status**: In Progress

## Reference: Recent Git Commits
- `dbab480c` feat(session-114): Add comprehensive implementation plan and documentation
- `3b97088d` docs(session-113): finalize work plan completion status
- `f1e65b09` feat(session-113): hydrology v49 map-control v53 and proto coverage guards
- `8b551763` docs(session-112): add comprehensive validation report and work plan
- `1b8eb724` feat(session-111): upgrade hydrology v48 map-control v52 and hotspot queue admission

## Gap Summary (for this session)
- Terrain seasonal runoff coupling exists on server but parity is incomplete on client preview pipeline.
- Inflight stale pruning is mostly timeout-driven on server and can be improved with focus-distance pressure pruning.
- Protocol probe exists, but profile-version/signal updates must stay aligned with hydrology/map-control revision.
- Feature list/config/docs need session-level refresh after code changes.

## TODO

### Phase 1: Pre-checks
- [x] Verify clean local working tree before implementation
- [x] Review latest commits for session continuity
- [x] Create session-115 work plan in `plans/`

### Phase 2: Core / Content / Util inventory update
- [x] Refresh Core/Content/Util categorized feature markdown
- [x] Refresh JSON feature inventory snapshot for session-115
- [x] Link changed source/config paths in inventory

### Phase 3: Terrain generation improvements (cave/river/lake)
- [x] Add seasonal runoff coupling pass to client enhanced terrain preview pipeline
- [x] Tune coupling with profile-driven parameters and deterministic seasonal noise seed
- [x] Validate cave/river/lake coupling flow remains deterministic per chunk

### Phase 4: World map control architecture improvements
- [x] Add focus-aware stale pruning for server inflight chunk generation
- [x] Reuse shared queue pressure policy for distance-based stale dropping
- [x] Ensure stale mitigation behavior remains compatible with existing queue policy JSON

### Phase 5: Protobuf + dummy protocol verification
- [x] Revalidate generated protobuf registry/probe flow after profile revision
- [x] Update dummy probe minimum profile version guard
- [x] Re-check shared dll references (`GameCommon`, `SharedProtocol`) are intact

### Phase 6: Config/data-driven updates
- [x] Update profile/version/signature values in JSON config files
- [x] Keep server/client runtime config parity in JSON

### Phase 7: Build & validation
- [x] Run `dotnet build SharedProtocol/SharedProtocol.csproj`
- [x] Run `dotnet build GameServer/GameServer.csproj`
- [x] Run `dotnet test` for available server tests
- [x] Run protobuf probe path/build verification where feasible

### Phase 8: Documentation & finalization
- [x] Update README or related docs under `docs/`
- [x] Update this plan with completed checklist
- [ ] Commit all changes locally
- [ ] Push to `origin/master`

## COMPLETED

### Pre-work completed
- [x] Confirmed clean working tree before coding (`git status --short` empty)
- [x] Confirmed active branch/remotes (`master`, `origin`)
- [x] Reviewed recent commit chain for continuity and regression risk
- [x] Created session-115 plan document before implementation changes

### Implementation completed
- [x] Added client seasonal runoff coupling parity pass in world map terrain preview pipeline
- [x] Added server focus-aware inflight stale pruning with shared queue pressure policy
- [x] Raised profile/signature baseline to `v54` / `v50`
- [x] Updated data-driven feature manifests for session-115

### Validation completed
- [x] `dotnet build SharedProtocol/SharedProtocol.csproj` (PASS, warnings only)
- [x] `dotnet build GameServer/GameServer.csproj` (PASS, warnings only)
- [x] `dotnet test GameServer/TerrainGenerationTest.csproj` (PASS; no executable unit tests declared)
- [x] `powershell -ExecutionPolicy Bypass -File scripts/verify_protobuf.ps1` (PASS)
- [x] `dotnet run --project GameServer/GameServer.csproj -- --proto-probe` (PASS, optional binding WARN)
- [x] `dotnet run --project Tools/DummyMinecraftClient/DummyMinecraftClient.csproj -- --required-only` (PASS)
- [x] `dotnet run --project GameServer/GameServer.csproj -- --selftest` (PASS)
