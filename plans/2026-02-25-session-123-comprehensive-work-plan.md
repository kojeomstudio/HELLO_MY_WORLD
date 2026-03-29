# Session 123 Comprehensive Minecraft Work Plan

- Date: 2026-02-25
- Session: 123
- Branch: `master`
- Status: Completed

## Reference Commits (latest)
- `ea9de4e7` chore(prework): snapshot existing local changes before task
- `d99e38d9` feat(session-121): apply hydrology v53 map-control v57 and proto/map-control hardening
- `134a2883` feat(session-120): comprehensive review and improvement
- `13ff91c9` feat(session-119): apply hydrology v52 map-control v56 and floodplain basin coupling

## Recent Work Summary (from history)
- Completed: hydrology-coupled terrain generation chain (`v53`) and map-control profile drift guards (`v57`).
- Completed: protobuf dummy probe, descriptor coverage guard, and profile/version regression checks.
- Completed: data-driven JSON config architecture (`world.json`, queue policy, profile) on server/client.
- Missing/To strengthen in this session:
  - remove duplicated SharedProtocol constants/messages definitions and re-verify build integrity,
  - improve cave/river/lake coupling algorithm step for stronger lowland spring-floodplain continuity,
  - harden world-map queue architecture with explicit near-chunk keep budget shared by server/client,
  - publish new core/content/util classification and implementation sequence for this session.

## TODO

### Phase 1: Pre-check and planning
- [x] Local change pre-commit and origin push before implementation
- [x] Create/update `plans` document before code changes
- [x] Recheck repo status and verify no leftover unstaged scope confusion

### Phase 2: Core/Content/Utility classification
- [x] Create session-123 feature classification JSON under `config/`
- [x] Include implementation sequence and category summary
- [x] Wire latest manifest path priority in server startup

### Phase 3: Terrain algorithm improvement (cave/river/lake)
- [x] Add new hydrology-karst-floodplain coupling pass (server)
- [x] Mirror equivalent coupling pass in Unity preview generator (client)
- [x] Tune world/profile JSON values for new coupling behavior

### Phase 4: World-map control architecture improvement
- [x] Add server queue near-chunk keep budget setting
- [x] Add client queue near-chunk keep budget setting
- [x] Extend shared queue policy JSON and runtime override bindings

### Phase 5: Protobuf and shared DLL review
- [x] Deduplicate/normalize newly added SharedProtocol constants/messages/enums files
- [x] Verify generated protobuf references + probe guards remain valid
- [x] Reconfirm shared enum/code use through `GameCommon.dll` + `SharedProtocol.dll`

### Phase 6: Documentation and validation
- [x] Update `README.md` and session docs in `docs/`
- [x] Run build/probe checks (`dotnet build`, `dotnet run -- --proto-probe`, `scripts/verify_protobuf.ps1`)
- [x] Check `using` references via compile outcomes

### Phase 7: Finalize
- [x] Stage all changes
- [x] Local conventional commit
- [x] Push to `origin/master`

## COMPLETED
- [x] Pre-work snapshot commit and push (`ea9de4e7`)
- [x] Session-123 initial work plan created
- [x] Core/Content/Utility session-123 manifest + startup priority applied
- [x] Cave/River/Lake karst spring floodplain coupling pass applied (server/client)
- [x] World-map queue near-keep budget architecture applied (server/client)
- [x] SharedProtocol duplicate constants/messages/enums deduplicated
- [x] Profile/signature baseline raised to `v58` / `v54`
- [x] Build/test/protobuf/probe/selftest validation completed
- [x] Session-123 docs and README update completed
