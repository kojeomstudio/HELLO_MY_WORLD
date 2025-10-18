# Player Respawn Notification Plan

The death and respawn flow requires both server and client work so that remote players are returned to the world consistently and UI surfaces the event. The goal is to deliver this in incremental slices so each session can pick up where the previous one stopped.

## Scope Overview
- **Server:** Emit `PlayerRespawnBroadcast` and (later) `PlayerDeath` payloads to interested sessions, persist respawn metadata, and expose hooks for future analytics.
- **Client:** Refresh remote avatars, clear ragdoll/death states, and drive the HUD death feed plus respawn countdown when broadcasts arrive.
- **Docs & Tests:** Track the flow in this plan, update sequence/master documents, and extend the self-test harness once end-to-end wiring exists.

## Task Breakdown
| Task ID | Description | Owner (Server/Client/Docs) | Status | Notes |
|---------|-------------|----------------------------|--------|-------|
| Task-19A | Broadcast `PlayerRespawnBroadcast` to all online sessions and update documentation. | Server + Docs | Done (2025-10-17) | Implemented in this session; `RespawnHandler` now pushes events through `SessionManager`. |
| Task-19B | Consume respawn broadcasts inside Unity (remote entity manager + HUD death feed) and update README instructions. | Client | Planned | Requires wiring the new message into the Unity networking bridge plus UI updates. |
| Task-19C | Surface respawn events in the .NET self-test harness with assertions for multiple clients. | Server/Test | Planned | Add regression coverage once client consumption exists. |
| Task-19D | Extend death handling to broadcast `PlayerDeathMessage` and record analytics counters. | Server | Planned | Share metadata (damage type, killer) for HUD and telemetry. |

## Current Session Outcome
- Server now pushes respawn events to all active sessions using strongly typed messages.
- Shared docs (`minecraft_feature_sequence.md`, `minecraft_feature_execution.md`, and this plan) outline the remaining client and testing work so follow-up sessions can continue sequentially.

## Next Recommended Steps
1. Implement Unity-side message handling for `PlayerRespawnBroadcast`, updating remote avatar state and death feed (Task-19B).
2. Add a regression scenario to the self-test or smoke harness that verifies multi-client respawn propagation (Task-19C).
3. Revisit death broadcasts and analytics (Task-19D) once respawn consumption stabilises.
