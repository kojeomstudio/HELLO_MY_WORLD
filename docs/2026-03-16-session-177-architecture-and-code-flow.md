# Session 177 Architecture and Code Flow (2026-03-16)

## 1. Current Status Snapshot
- `git pull` executed before work: repository is up to date.
- Local working changes at start: only untracked `work/`.
- Recent 1-week commit trend:
  - `e9457de2` (2026-03-16): docs(session-176) validation report.
  - `1cbfc629` (2026-03-16): hydrology v90 + map-control v94 parity and optional packet handler expansion.
  - `0b05bc62` (2026-03-15): validation, feature categorization, and docs update.

## 2. High-Level Architecture
- Client (Unity):
  - `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`
  - `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`
  - `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
- Server (.NET):
  - `GameServer/Program.cs` (startup, validation gates, modes)
  - `GameServer/GameServer.cs` (socket loop, dispatch, systems)
  - `GameServer/SessionManager.cs` (session and player-state lifecycle)
  - `GameServer/World/*.cs` (world generation, sync, map control)
- Shared contracts:
  - `SharedProtocol/` (dispatch, session framing, protocol registry)
  - `SharedProtocol/EnhancedMinecraft/` (protobuf diagnostics and validation)
  - `GameCommon/` (shared world/profile contracts)

## 3. Server Startup Flow
1. `Program.Main` initializes protocol runtime and validates registry/contracts.
2. Proto diagnostics run (`LogSummary`, `AssertRegistryClean`, report emit).
3. Profile/config parity checks mirror server/client JSON if drift is detected.
4. Startup mode switch:
  - `--server` for long-running server.
  - `--selftest` for server + test workflow.
  - `--generate-map-profile` / `--proto-probe` for tooling modes.
5. `GameServer.StartAsync` begins TCP accept loop and periodic maintenance timers.

## 4. Message Dispatch Flow
1. Client packet received by `Session.ReceiveAsync`.
2. Base message path:
  - Routed through `MessageDispatcher` to typed handlers.
3. Enhanced Minecraft path:
  - Unknown base type fallback to `MinecraftMessageDispatcher`.
  - Raw payload parsed by handler contract (`Google.Protobuf` for enhanced, protobuf-net fallback for optional legacy).
4. Room/session/world services broadcast authoritative updates.

## 5. World Generation and Sync Flow
1. World config/profile JSON loaded at startup.
2. `WorldManager` applies hydrology/cave/river/lake parameters into terrain pipeline.
3. Chunk generation and block updates are server-authoritative.
4. `WorldSynchronizationManager` batches world changes and broadcasts to room peers.
5. Client `WorldMapController`/`WorldAreaManager` consume mirrored profile and render deterministic previews.

## 6. Protocol Reliability Flow
1. `ProtocolValidator.ValidateEnhancedContracts()` verifies required contracts and descriptor integrity.
2. `ProtoDiagnostics` verifies:
  - descriptor fingerprint parity,
  - registered descriptor coverage,
  - optional packet visibility and missing bindings.
3. Dispatcher-level handler contract checks prevent message-type drift at runtime.

## 7. Data-Driven Content Flow
- Runtime game/config data remains JSON under `config/` and mirrored runtime folders.
- Template-to-JSON flow is documented in:
  - `design/2026-03-16-game-data-template-pipeline.md`
- New tool:
  - `Tools/GameDataTemplateExporter` converts markdown template datasets to validated JSON files.

## 8. Design Document References
- Gameplay/product design:
  - `design/2026-03-16-minecraft-clone-game-design.md`
- Data/template pipeline:
  - `design/2026-03-16-game-data-template-pipeline.md`
- Core/content implementation tasks should reference the above two design documents before code changes.

