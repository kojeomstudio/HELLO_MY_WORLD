# Session 178 Architecture and Code Flow (2026-03-17)

## 1. Current Status Snapshot
- `git pull` result: already up to date (`master...origin/master`).
- Local working tree at start: clean.
- Last 1-week commits show recent emphasis on:
  - data-driven pipeline and template exporter tooling,
  - protocol/map/hydrology parity increments,
  - per-session plan/report updates.

## 2. Runtime Architecture (Current)
- Unity client:
  - `Assets/MyAssets/Scripts/GameWorld/*`
  - `Assets/MyAssets/Scripts/Network/*`
  - generated protobuf DTOs in `Assets/Generated/Protobuf/`
- .NET game server:
  - startup entry: `GameServer/Program.cs`
  - request handlers: `GameServer/Handlers/*`
  - session lifecycle: `GameServer/SessionManager.cs`
  - world systems: `GameServer/World/*`
- Shared protocol/contracts:
  - `SharedProtocol/*`
  - source schemas: `proto/*.proto`

## 3. Server Startup and Validation Flow
1. `Program.Main` parses mode arguments (`--server`, `--selftest`, tooling modes).
2. Protocol/runtime validations execute (registry, descriptors, handler contracts).
3. Server config and runtime JSON are loaded.
4. `GameServer` starts accept loop and periodic systems.

## 4. Network Dispatch Flow
1. Session receives packet bytes.
2. Dispatcher maps packet to typed handler.
3. Handler updates room/session/world authoritative state.
4. Synchronization layer broadcasts delta/state to room peers.
5. Unity client applies mirrored updates in world/map controllers.

## 5. Data-Driven Content Flow
1. Author content template in markdown:
  - `design/templates/game-data-template.md`
2. Export with .NET 8 tool:
  - `Tools/GameDataTemplateExporter`
3. Emit runtime JSON datasets to:
  - `config/game-data/*.json`
4. Server/client load JSON data as runtime tuning/content source.

## 6. Design References for Core/Content Work
- Core/content feature development must reference:
  - `design/2026-03-16-minecraft-clone-game-design.md`
  - `design/2026-03-16-game-data-template-pipeline.md`
  - `design/2026-03-17-session-178-design-execution.md`

## 7. Tooling Version Constraint
- Tool-like programs in this repository should target .NET 8.0~9.0.
- Existing data-template exporter target is `net8.0` and remains compliant.
