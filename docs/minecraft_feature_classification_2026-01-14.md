# Minecraft Feature Classification (Core / Content / Util)

- Source: `config/minecraft_feature_classification_2026-01-14.json`
- Context: Built for 2026-01-14 session (`plans/2026-01-14-session-plan-02.md`) to keep client/server parity.
- Guidance: Implement in order within each category; keep data-driven JSON as the single source and mirror updates in docs when statuses change.

## Server
- **Core (order)**: 1) protocol handshake/auth; 2) session lifecycle; 3) chunk worldgen & streaming (improved hydrology/caves/rivers/lakes); 4) block state updates; 5) entity simulation sync; 6) persistence profile.
- **Content**: 1) biome generation; 2) caves/rivers/lakes (hydrology-aware); 3) mob spawning rules; 4) items & crafting (data-driven); 5) structures/POI.
- **Util**: 1) config reload/watchers; 2) observability/logging/metrics; 3) selftest pipeline (dotnet + proto smoke); 4) data-driven content loader.

## Client
- **Core**: 1) network connect/protobuf client; 2) chunk streaming/meshing with map-control profile; 3) input/combat; 4) UI inventory/hotbar; 5) persistence/cache validation.
- **Content**: 1) biome visuals/weather; 2) block/item rendering; 3) mobs FX/audio; 4) world map overlay (preview chunks); 5) crafting UI/tooltips.
- **Util**: 1) debug overlay/profiler; 2) data loader validation; 3) asset bundle management; 4) telemetry/crash reporting.

## Implementation Notes
- Core items must remain deterministic and validated by `dotnet build` + runtime smoke (`--selftest` when feasible).
- Any change to world-gen (caves/rivers/lakes) must update the control profile JSON and client `WorldMapControlProfile` to avoid drift.
- Treat the JSON file as canonical; docs summarize intent. Keep statuses in sync when tasks progress.
