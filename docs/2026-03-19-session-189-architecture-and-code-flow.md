# Session 189 Architecture and Code Flow (2026-03-19)

## Scope
This document captures the current architecture and request/data flow for the Unity client, .NET server, shared protocol, and game-data pipeline. It follows `work/work.md` and uses `minetest_project` as the structural reference baseline.

## Reference Baseline (Minetest / Luanti)
- `minetest_project/src/server.cpp`
  - Server-thread centric runtime step and network receive loop pattern.
- `minetest_project/src/emerge.cpp`
  - Emerge queue limits, generation concurrency boundaries, and mapgen manager ownership model.
- `minetest_project/doc/world_format.md`
  - World persistence split between metadata/auth/player/map storage.

## Current Project Architecture
1. Unity Client
- Root: `Assets/MyAssets/Scripts/`
- Responsibilities: input, world rendering, player/UI sync, packet serialization boundary calls.

2. .NET Game Server
- Root: `GameServer/`
- Entry: `Program.cs`
- Responsibilities: session lifecycle, request handlers, world state authority, selftest harness.

3. Shared Protocol Contracts
- Root: `SharedProtocol/` and generated DTOs in `Assets/Generated/Protobuf/`
- Responsibilities: protocol compatibility and data contract alignment across client/server.

4. Data-Driven Content Layer
- Runtime JSON: `config/game-data/*.json`
- Template source: `design/templates/game-data-template.md`
- Export tool: `Tools/GameDataTemplateExporter` (net8.0)

## Code Flow Summary
1. Server startup
- `dotnet run --project GameServer -- --server`
- Loads config and game-data JSON, initializes session/world subsystems.

2. Client connection and packet handling
- Client sends protocol messages mapped to shared contracts.
- Server handlers validate and apply authoritative world/session changes.
- Server broadcasts resulting state deltas/events.

3. World generation and map control
- Current implementation keeps queue/profile artifacts synchronized in:
  - `config/world_map_control_profile.json`
  - `GameServer/config/world_map_control_profile.json`
  - `Assets/StreamingAssets/world-map-control.json`
  - `GameServer/Assets/StreamingAssets/world-map-control.json`
- This mirrors Minetest's explicit mapgen-manager and queue policy approach while fitting Unity runtime constraints.

4. Content data pipeline
- Designers author datasets in Markdown template blocks.
- Export tool normalizes and emits JSON datasets for runtime loading.
- Runtime validation catches invalid or missing datasets before gameplay-critical paths execute.

## Unity-First Modernization Focus (Compared with Minetest)
- Keep server authoritative flow, but expose strongly typed C# contracts end-to-end.
- Prefer JSON-driven balancing/content updates over hardcoded gameplay constants.
- Keep generation/profile settings externally configurable to support rapid iteration.
- Preserve deterministic server validation while keeping Unity client responsive with prediction/reconciliation-friendly packet boundaries.

## Implementation Rule
Core gameplay/content development should reference `design/*.md` first, then implement in code. Design changes should precede data schema or gameplay behavior updates.
