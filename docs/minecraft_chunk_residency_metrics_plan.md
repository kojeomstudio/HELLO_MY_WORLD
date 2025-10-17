# Chunk Residency Metrics Plan (2025-10-16)

This mini plan tracks the full-stack work required to surface useful chunk residency analytics for the Minecraft-style server and Unity client. Keep items scoped so that each can be tackled in a short session, and leave remaining tasks marked for follow-up.

## Sequential Feature Steps
| Step | Description | Server Responsibilities | Client Responsibilities | Status | Notes |
|------|-------------|-------------------------|-------------------------|--------|-------|
| 1 | Instrument per-player chunk residency counts | Extend `ServerMetricsService` and chunk handlers to publish residency counts | Passive | Done | New counters track per-player residency and peak usage. |
| 2 | Expose residency metrics via status responses | Add the new counters to `ServerStatusResponse` payloads | Consume additional fields when present | Done | HUD and any tooling can now read residency load. |
| 3 | Surface metrics in the in-game HUD | None | Update status overlay to display residency totals/peaks | Done | Players and testers can monitor residency from the status overlay. |
| 4 | Persist metrics for diagnostics | Emit rolling averages and add them to docs/self-test output | Extend pause-menu overlay with historical chart | Pending | Leave for a follow-up session once live counters look healthy. |

## Next Actions
- Validate the metrics during self-test runs and record any anomalies here.
- Regenerate Unity protobuf bindings (`protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`) once the toolchain is available.
- Tackle Step 4 by adding rolling averages + pause menu surfacing in a future session.
- Log any blockers or large follow-up tasks directly in this file so the next session can continue without rediscovery.
