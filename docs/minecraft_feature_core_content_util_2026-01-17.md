# Minecraft Feature Map (Core / Content / Util) — 2026-01-17

This list groups client/server responsibilities for Minecraft features. Use it as the active backlog for sequencing work. Status values: `planned`, `in_progress`, `partial`, `done`.

## Core
- Core-WorldGen (planned) — Server: hydrology-aware terrain pipeline, caves/rivers/lakes seam control; Client: preview generator that mirrors server masks. Docs: `docs/terrain_generation_and_proto_update_2026-01-17.md`.
- Core-Networking (partial) — Server: EnhancedProtocolHandler with protobuf validation, handler coverage; Client: ProtobufNetworkClient bootstrap, connection lifecycle, reconnect policy.
- Core-ChunkStreaming (partial) — Server: chunk build + diff + compression; Client: chunk request batching, block mesh/update, pool reuse.
- Core-SessionSync (planned) — Server: session auth, spawn/respawn, time/weather broadcast; Client: time/weather render sync, spawn flow, death/respawn UI.
- Core-WorldMapControl (in_progress) — Server: world-map control hash + profile, hydrology/flow export; Client: map control profile loader, preview stitching, cache invalidation on profile hash change.

## Content
- Content-BlocksItems (partial) — Server: canonical block/item registry, drop tables; Client: block/item assets, placement/break UX, inventory UI wiring.
- Content-BiomesStructures (planned) — Server: biome mask, structure placement hooks; Client: biome-based visuals, point-of-interest markers.
- Content-Entities (planned) — Server: entity spawn/despawn/state updates; Client: interpolation, culling, basic AI client hints.
- Content-AudioVFX (planned) — Server: triggers for sound/particle packets; Client: effect registry aligned to protobuf packets.
- Content-UIFlows (partial) — Server: minimal menu/session state; Client: menus (world select, settings), HUD sync for health/hunger/oxygen.

## Util
- Util-ConfigData (in_progress) — JSON configs for worldgen, networking, map control; data-driven game data in `config/` and `Assets/StreamingAssets/`.
- Util-ProtoTooling (partial) — Protoc regeneration (`proto/*.proto` → `SharedProtocol` + `Assets/Generated/Protobuf`), `ProtocolRegistry.ValidateBindings`, fingerprint guard.
- Util-TelemetryDiagnostics (planned) — Logging/metrics hooks for worldgen timings, network throughput; client debug overlays for hydrology/cave preview.
- Util-BuildTest (partial) — `dotnet build`/`dotnet test`, Unity test runner, CI smoke `dotnet run --project GameServer -- --selftest`.
- Util-DataPipelines (planned) — JSON-driven game data ingestion, migration scripts, validation of config vs. runtime profiles.

## Implementation Order
1. Core-WorldGen — finalize hydrology seam blending + cave masks.
2. Core-WorldMapControl — align profile propagation between server/client.
3. Core-ChunkStreaming — validate chunk protobufs and handler coverage.
4. Content-BlocksItems — ensure registries align with proto payloads.
5. Util-ProtoTooling — rerun protoc + fingerprint validation as part of builds.
6. Util-TelemetryDiagnostics — wire metrics for worldgen/network hot paths.
