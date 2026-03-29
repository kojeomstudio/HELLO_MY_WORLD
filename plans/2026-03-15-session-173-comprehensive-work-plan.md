# Session 173 Comprehensive Work Plan (2026-03-15)

## Reference Commits (Recent)
- `278d85b7` feat(session-172): comprehensive feature categorization and validation
- `24581ea0` feat(session-171): add legacy protobuf fallback for optional packets
- `d1ecca90` feat(session-170): organize docs archive and update README
- `4d9a98bb` feat(session-169): fix simplex terrain crash and refresh validation docs
- `979d1393` feat(session-168): apply hydrology v88 map-control v92 queue/proto parity

## To Do
- [x] Add a new cave-river-lake hydrology coupling stabilization pass in server terrain generation (`MapGeneratorLib`).
- [x] Mirror equivalent coupling pass in Unity client world-map generation preview (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`).
- [x] Bump shared hydrology signature and map-control profile version in `GameCommon/World/SharedFeatureCatalog.cs`.
- [x] Regenerate/update world-map control profile JSON snapshots for parity:
  - `config/world_map_control_profile.json`
  - `GameServer/config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`
- [x] Align dummy protocol client minimum profile requirements.
- [x] Refresh core/content/utility feature inventory JSON for this session.
- [x] Run compile/protocol validation commands (`dotnet build`, `dotnet test`, `--selftest` when available).
- [x] Update docs and README with session 173 changes and validation evidence.

## Completed
- [x] Decoded and reviewed `work/work.md` instructions (UTF-8).
- [x] Collected repository baseline (`git status`, recent commits, existing plans/docs inventory).
- [x] Identified current parity baseline: Hydrology `v88`, Map Control `v92`, profile snapshot drift between root/server configs.
- [x] Implemented server/client cave-river-lake confluence stabilization pass and integrated it into generation pipelines.
- [x] Upgraded signature/version baseline to Hydrology `v89` + Map Control `v93`.
- [x] Executed validation suite (`dotnet build`, `dotnet test`, `dotnet run -- --selftest`) and captured updated proto probe output.
