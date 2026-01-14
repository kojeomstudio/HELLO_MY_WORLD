## Session Plan (2026-01-14)

### Completed (recent commits)
- `9cb7d4d2` docs: comprehensive implementation status report and plan (2026-01-14)
- `e4efc861` feat(worldgen): stabilize caves and hydrology seams
- `33cec546` docs: Add comprehensive documentation for terrain generation, world map control, protocol, configuration, and data-driven approach
- `df0527d61` chore(worldgen): seal hydrology seams and refresh plans
- `973edf61` fix: resolve using statement issues and document implementation work (2026-01-13)

### TODO (this session)
- Classify Minecraft-required client/server features into core, content, and util; record in a repo file for implementation tracking.
- Improve terrain generation algorithms for caves, rivers, and lakes; apply changes to world map control architecture across server and client.
- Review and fix protobuf packet generation/usage to ensure generated DTOs are referenced correctly.
- Verify `using` references correspond to real files/classes; fix missing/invalid references.
- Ensure configs and data remain JSON-driven (server/client settings and gameplay data); add/adjust files if needed.
- Update docs (Markdown under `docs/`) and README as needed after changes.
- Run compilation/tests (`dotnet build`, `dotnet test`, relevant Unity-safe checks) and validate protobuf handling.
- Finalize changes with git add/commit/push (required by task).
