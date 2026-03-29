# Minecraft Feature Breakdown (Core / Content / Util)

This document tracks required Minecraft-style features across server and Unity client, grouped by **Core**, **Content**, and **Util** buckets so we can implement them sequentially without missing dependencies.

## Server
- **Core**: session/auth pipeline (login, rate limiting, heartbeat), protobuf packet router & validation, chunk lifecycle (stream/save/unload), world generation pipeline (terrain/caves/rivers/lakes), physics/collision & movement validation, persistence (player/world state), config-driven toggles (server-config.json, config/world.json).
- **Content**: biome palette & block rules, caves/rivers/lakes & aquifers, structures (dungeons/clouds/vegetation), mobs/NPC AI hooks, items/recipes/inventory rules, weather & day/night simulation, combat/damage systems.
- **Util**: metrics/logging tracing, admin commands/GM tools, profiling hooks, backup/maintenance jobs, data-driven balancing (JSON tables for loot, spawns, worldgen weights), protobuf regeneration workflow.

## Unity Client
- **Core**: network client with protobuf codecs, chunk streaming & culling, player controller + physics sync, render pipeline for blocks/water/sky, settings & config ingestion (json under Assets/ or StreamingAssets).
- **Content**: UI/UX (hud/chat/inventory), block/item library visuals, VFX/SFX for weather & biomes, mob rendering/animation hooks, recipe crafting flows.
- **Util**: debugging overlays (net stats, chunk borders), replay/recording toggles, QA tools (noclip/fly), localization pipeline, data-driven content loaders (json atlases for blocks, UI layouts).

## Suggested Implementation Order
1. Harden core pipeline: auth/session, protobuf schema alignment, chunk streaming contract, baseline physics validation.
2. Worldgen + hydrology: caves/rivers/lakes tuning with data-driven config, map control exposed to client.
3. Gameplay content: biomes/blocks/recipes/mobs and UI surfaces.
4. Utilities: metrics/telemetry, admin tools, profiling & content loading automation.

Update this file as features land to keep both client and server in sync.
