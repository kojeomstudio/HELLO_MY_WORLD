# Minecraft Features by Category (Core · Content · Utility)

All Minecraft features required for this project are grouped by category and split by server/client responsibilities so both sides can be implemented in lockstep. Settings remain data-driven through JSON (`config/world.json`, `Assets/StreamingAssets/client-config.json`, `Assets/MyAssets/Resources/.../WorldConfigData.json`) and protobuf packets stay the single source of truth for network shapes.

## Core
- **World generation**
  - Server: hydrology-aware chunk pipeline (edge flux feathering, river/lake/cave coordination), chunk residency enforcement, SQLite persistence hooks.
  - Client: MapGeneratorLib previews honoring `WorldMapControlProfile` (render/simulation distance, hydrology edge flux, river feathering), streaming/mesh rebuild cadence.
- **Networking + protocols**
  - Server: EnhancedMinecraftProtocol registry/validator coverage, chunk load/unload enforcement, action/interaction handlers, handler coverage reporting.
  - Client: packet deserializers bound to generated protobuf DTOs, action request/response routing, chunk/state reconcilers.
- **Player/world systems**
  - Server: room-scoped chat/block broadcasts, time/weather ticks, entity spawn/update/despawn, block persistence.
  - Client: player controller, remote entity manager, HUD sync (time/weather/health), block placement/breaking feedback.

## Content
- **Blocks/items/entities**
  - Server: block registry/state persistence, item crafting/smelting validation, loot/ore tables, mob spawn rules.
  - Client: block/item definitions from JSON, inventory/crafting UI, model/material bindings, mob prefabs + animations.
- **World features**
  - Server: biomes, rivers/lakes/caves/dungeons, vegetation, structure hooks (villages/mineshafts), confluence-aware shorelines.
  - Client: same feature toggles for previews, biome-driven visuals (sky/foliage tint), particle/audio hooks for water/caves.

## Utility
- **Config + data**
  - Server: JSON-backed world/server configs (worldgen knobs, environment variables -> json), proto fingerprints, migration safe defaults.
  - Client: mirrored JSON for worldgen/render distances, input/audio/graphics settings, safe fallbacks when fields are missing.
- **Tooling + observability**
  - Server: protobuf verification, handler coverage logs, worldgen metrics (hydrology flux/river seam stats), backup/export helpers.
  - Client: chunk loader debug overlay, perf capture (LOD/culling), protocol mismatch warnings sourced from registry results.

## Implementation Order (server ↔ client)
1. Lock config parity (worldgen + map control JSON) and regenerate/verify protobufs; ensure registry/validator clean.
2. Harden worldgen pipeline (hydrology edge flux, river seam feathering, cave edge sealing) on server and MapGeneratorLib mirror.
3. Update chunk streaming + map-control consumers to the new profile knobs; smoke test chunk requests at render-distance clamps.
4. Integrate gameplay/content systems (blocks/items/entities) with refreshed data files; run end-to-end build/test.
