# 2026-01-17 Session Plan (worldgen/proto refresh)

## Completed (recent commits)
- 49d93d2a: docs – comprehensive implementation plan and protocol review (2026-01-16).
- 190c8615: feat(worldgen) – aligned hydrology masks and proto guards.
- cd67a223: docs – protocol & configuration audit (2026-01-16).
- 44a5b19b: feat(worldgen) – hydrology envelope hardening and proto gating.
- 78d9cadb: docs – Minecraft implementation review (2026-01-15).

## To Do (today)
- Refresh Minecraft feature requirements grouped into Core/Content/Util for client/server; store lists to file and align with data-driven JSON.
- Improve terrain generation (caves, rivers, lakes) algorithms and integrate into world map control for server + client.
- Review protobuf-generated packets (references/usage), regenerate if needed, and fix handlers; ensure using directives target existing classes.
- Strengthen world map control architecture and synchronization between server/client; ensure configs and gameplay data remain JSON-driven.
- Update configs (server/client) in JSON as needed; document any new keys.
- Update docs under `docs/` (and README if applicable) for worldgen/proto/config changes.
- Run builds/tests: `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`, and `dotnet run --project GameServer -- --selftest` if feasible; validate protobuf handling.
- Commit and push all staged/modified work to origin when tasks complete.

## Notes
- Keep data and settings data-driven (JSON) for both client and server.
- Cross-check git history above to avoid regressions and ensure continuity with prior worldgen/proto changes.
