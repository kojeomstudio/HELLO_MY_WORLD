# Minecraft Feature Plan

## Feature Matrix
| ID | Feature | Description | Server Status | Client Status | Notes |
|----|---------|-------------|---------------|----------------|-------|
| F-01 | Authentication & Session | Login, token validation, session heartbeat | Done (LoginHandler, SessionManager) | Done (MinecraftGameClient login flow) | Covered by existing handlers |
| F-02 | Player Movement Sync | Position/rotation updates, world entry | Done (MovementHandler) | Done (state updates & events) | Uses tick-based heartbeats |
| F-03 | Chunk Streaming & Caching | Serve/generate chunk payloads, prevent redundant requests, mark cache hits | Done (enhanced cache flag + per-player registry) | Done (deduplicates chunk requests, respects cache hints) | Response logs cache hits for diagnostics |
| F-04 | Block Interaction Broadcast | Propagate block break/place results with drops to nearby players | Done (broadcast via SessionManager.BroadcastToAreaAsync) | Done (BlockDropsReceived surfaced with block mutations) | Chunk-scoped fan-out replaces console-only TODO |
| F-05 | Item Drop Visibility | Notify clients about block-derived drops | Done (drop metadata bundled with notifications) | Done (drop stream surfaced through new event) | Complements F-04 for survival gameplay |
| F-06 | Chunk Residency Tracking | Track which chunks each player has loaded | Done (in-memory registry with eviction hooks) | Done (client maintains loaded chunks) | Consider eviction policy for long sessions |
| F-07 | Chunk Residency Eviction | Evict stale per-player chunk residency entries and cap budgets using server config | Done (TTL pruning + budget caps) | Passive (no change) | Uses WorldSettings.ChunkUnloadTimeoutMinutes and periodic cleanup to drop offline players. |
| F-08 | Client Chunk Unload Signal | Send explicit unload notices so server can drop residency immediately | Done (explicit ack via HandleChunkUnloadAsync) | Done (automatic unload sweep + notifications) | Bidirectional ack gives residency telemetry for diagnostics. |
| F-09 | Inventory Snapshot Persistence | Persist inventory snapshots on logout and push diffs on reconnect | Done (server JSON snapshots persisted) | Done (Unity consumes diff snapshots via InventoryItemsUpdated) | Snapshot pipeline feeds MinecraftGameClient hotbar/events. |
| F-10 | World Time & Weather Sync | Broadcast day/night and weather deltas with smoothing | Done (WorldTimeSystem/WeatherSystem) | Done (WorldTimeController/WorldWeatherController) | Server systems dispose cleanly on shutdown. |
| F-11 | Remote Player Entity Sync & Interpolation | Broadcast player spawn/update/despawn, smooth avatars, cull off-screen actors | In progress (EntitySyncService broadcasting + velocity stubs) | In progress (RemoteEntityManager baseline smoothing) | Task-11B/11C will add velocity-aware blending and culling. |
| F-12 | Crafting & Container Persistence | Sync crafting grids and shared containers across sessions | Planned | Planned | Builds on F-09 plus container open/close protocol wiring. |
| F-13 | Server Status HUD | Poll server metrics and render overlay with manual refresh | Done (MinecraftGameClient auto polling & events) | Done (MinecraftGameManager overlay + refresh button) | Auto-refresh every 15s with manual override tapping latest ServerStatusResponse. |
| F-14 | Weather FX and Ambient Audio | Route weather intensity to FX/audio | In progress (WeatherSystem intensity snapshots) | In progress (WorldWeatherController needs asset bindings) | Requires ambient preset authoring. |
| F-15 | Combat Feedback and Damage Numbers | Show combat events with damage popups | Planned | Planned | Needs combat log events and UI popups. |
| F-16 | Mob AI & Spawning Framework | Simulate mobs server-side, reflect to client proxies | Planned | Planned | Depends on world tick scheduler and navmesh. |

## Implementation Order
- [x] Enhance chunk streaming on the server (MinecraftChunkHandler) to report cache hits and persist player chunk residency via SessionManager.
- [x] Deliver authoritative block change broadcasts with drop metadata from MinecraftPlayerActionHandler and reuse SessionManager proximity helpers.
- [x] Extend the Unity client (MinecraftGameClient) to avoid duplicate chunk requests, react to server cache hints, and surface block drop notifications to listeners.
- [x] F-07 Server chunk residency eviction with TTL enforcement and per-player budget pruning.
- [x] F-08 Client chunk unload notifications with matching server acknowledgements.
- [x] F-13 Server status HUD wiring server metrics to the Unity overlay.
- [x] F-09 Inventory snapshot persistence and reconnect diffs (server snapshot storage + Unity diff consumer delivered).
- [x] F-10 Time/weather broadcast parity (server systems + Unity bindings).
- [x] F-11A Entity sync groundwork (spawn/update/despawn plus RemoteEntityManager baseline).
- [ ] F-11B Velocity-aware interpolation and view-radius culling.
- [ ] F-12 Crafting/container persistence alignment with survival gameplay.

## Implementation Notes
- MinecraftChunkHandler tracks per-player served chunks and folds cache-hit insights into ChunkDataResponseMessage.IsFromCache.
- MinecraftPlayerActionHandler pushes block changes (including drops) to chunk peers using SessionManager.BroadcastToAreaAsync, while responses include the initiating player's drop summary.
- MinecraftGameClient keeps a _pendingChunkRequests set to suppress duplicate fetches, clears it on disconnect, and emits a BlockDropsReceived event when servers advertise drops.
- Server chunk residency eviction enforces TTL and radius budgets from WorldSettings and drops offline players during cleanup (F-07).
- Added ChunkUnloadNotificationMessage/ChunkUnloadAcknowledgeMessage handshake so the server trims residency immediately after the client unloads a chunk (F-08).
- World time snapshots go out immediately on login, and the WeatherSystem drives configurable weather broadcasts (F-10 server side).
- MinecraftGameClient emits ServerStatusReceived events with a 15s auto-poll while MinecraftGameManager exposes the HUD overlay and manual refresh control for server metrics (F-13).
- EntitySyncService now serialises player spawn/update/despawn messages for nearby sessions, enabling the Unity RemoteEntityManager to spawn and smooth remote avatars (F-11A).

## Backlog & Follow-ups
- **F-11B** Finish velocity-aware interpolation, jitter clamps, and remote avatar pooling.
- **F-12** Crafting/container persistence alignment with survival gameplay.
- **F-14** Author ambient presets and asset bindings for weather-driven audio/FX.
- **F-16** Prototype mob spawning, AI ticks, and client proxy lifecycle.

## Task Queue (Oct 2025)
1. [x] Task-09A - Server inventory snapshot persistence (SQLite JSON storage + API).
2. [x] Task-09B - Unity client inventory snapshot/diff consumer event + hotbar refresh.
3. [x] Task-09C - Session shutdown hook to persist final snapshot and analytics counters.
4. [x] Task-10A - Hook MinecraftGameClient into TimeUpdateMessage, expose events, and cache the latest world/day ticks.
5. [x] Task-10B - Drive Unity skybox lighting and ambient settings via WorldTimeController using cached ticks.
6. [x] Task-10C - Surface WeatherChangeMessage through client events and a WorldWeatherController for FX toggles.
7. [x] Task-10D - Present formatted time and weather status in the HUD (MinecraftGameManager).
8. [x] Task-11A - EntitySyncService + RemoteEntityManager baseline interpolation for remote players.
9. [ ] Task-11B - Velocity-aware smoothing and teleport safeguards for remote players.
10. [ ] Task-11C - Distance-based culling & pooling for remote player avatars.
11. [ ] Task-10E - Ambient preset authoring for weather-driven lighting/audio.
