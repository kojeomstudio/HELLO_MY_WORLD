# Minecraft Feature Plan

## Feature Matrix
| ID | Feature | Description | Server Status | Client Status | Notes |
|----|---------|-------------|---------------|----------------|-------|
| F-01 | Authentication & Session | Login, token validation, session heartbeat | Done (LoginHandler, SessionManager) | Done (`MinecraftGameClient` login flow) | Covered by existing handlers |
| F-02 | Player Movement Sync | Position/rotation updates, world entry | Done (MovementHandler) | Done (state updates & events) | Uses tick-based heartbeats |
| F-03 | Chunk Streaming & Caching | Serve/generate chunk payloads, prevent redundant requests, mark cache hits | Done (enhanced cache flag + per-player registry) | Done (deduplicates chunk requests, respects cache hints) | Response logs cache hits for diagnostics |
| F-04 | Block Interaction Broadcast | Propagate block break/place results with drops to nearby players | Done (broadcast via `SessionManager.BroadcastToAreaAsync`) | Done (`BlockDropsReceived` surfaced with block mutations) | Chunk-scoped fan-out replaces console-only TODO |
| F-05 | Item Drop Visibility | Notify clients about block-derived drops | Done (drop metadata bundled with notifications) | Done (drop stream surfaced through new event) | Complements F-04 for survival gameplay |
| F-06 | Chunk Residency Tracking | Track which chunks each player has loaded | Partial (in-memory registry lacks eviction) | Done (client maintains loaded chunks) | Consider eviction policy for long sessions |
| F-07 | Chunk Residency Eviction | Evict stale per-player chunk residency entries and cap budgets using server config | Done (TTL pruning + budget caps) | Passive (no change) | Uses `WorldSettings.ChunkUnloadTimeoutMinutes` and periodic cleanup to drop offline players. |
| F-08 | Client Chunk Unload Signal | Send explicit unload notices so server can drop residency immediately | Planned | Planned | Requires lightweight request/ack pair tied to chunk streaming. |
| F-09 | Inventory Snapshot Persistence | Persist inventory snapshots on logout and push diffs on reconnect | Planned | Planned | Depends on InventorySystem delta serialization. |

## Implementation Order
- [x] Enhance chunk streaming on the server (`MinecraftChunkHandler`) to report cache hits and persist player chunk residency via `SessionManager`.
- [x] Deliver authoritative block change broadcasts with drop metadata from `MinecraftPlayerActionHandler` and reuse `SessionManager` proximity helpers.
- [x] Extend the Unity client (`MinecraftGameClient`) to avoid duplicate chunk requests, react to server cache hints, and surface block drop notifications to listeners.
- [x] F-07 Server chunk residency eviction with TTL enforcement and per-player budget pruning.
- [ ] F-08 Client chunk unload notifications with matching server acknowledgements.

## Implementation Notes
- `MinecraftChunkHandler` now tracks per-player served chunks and folds cache-hit insights into `ChunkDataResponseMessage.IsFromCache`.
- `MinecraftPlayerActionHandler` pushes block changes (including drops) to chunk peers using `SessionManager.BroadcastToAreaAsync`, while responses include the initiating player’s drop summary.
- `MinecraftGameClient` keeps a `_pendingChunkRequests` set to suppress duplicate fetches, clears it on disconnect, and emits a new `BlockDropsReceived` event when servers advertise drops.
- Server chunk residency eviction now enforces TTL and radius budgets from `WorldSettings` and drops offline players during cleanup (F-07).

## Backlog & Follow-ups
- **F-09** Inventory snapshot persistence and reconnect diff streaming (server/client).
