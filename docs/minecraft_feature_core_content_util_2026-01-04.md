# Core/Content/Utility Feature Plan (2026-01-04)

This checklist groups required Minecraft-like features for both client and server into **Core**, **Content**, and **Utility** buckets. Items are ordered for staged implementation so we can land terrain + networking upgrades first, then ship gameplay layers.

## Core (engine + networking)
- World generation pipeline (server-authoritative, Unity mirror): terrain noise, biome masks, caves, rivers, lakes, hydrology smoothing, chunk cache; config: `config/world.json`, `Assets/StreamingAssets/world-config.json`.
- World map control + sync: `WorldMapControlProfile` (server/client), map hash verification, chunk sync, view-distance negotiation.
- Protobuf packet layer: Google.Protobuf-only flow using `EnhancedMinecraftProtocol` DTOs, registry validation, chunk load/unload handlers, action/player state handlers.
- Chunk management/render: server `WorldManager` staging, client `ChunkManager/ChunkSnapshot`, mesh gen budgets, unload reasons.
- Data-driven tuning: JSON presets for hydrology/cave thresholds, lake/river toggles, biome parameters; scripts to regenerate protobufs and sync configs.

## Content (gameplay systems)
- World features: ore distribution map, biome decorations, structures (villages/dungeons placeholder hooks), weather/day-night sync.
- Player systems: auth/session, spawn + respawn rules, movement replication, interaction/action requests, inventory slots mirrored via Enhanced protocol payloads.
- Blocks/items: block palette JSON, block change broadcasts, item/tool metadata (durability/enchant hints), block drops + experience envelopes.

## Utility (tooling + ops)
- Configuration management: `server-config.json`, `client-config.json`, `world-config.json`, map profile JSON; validation + hash guard; environment overrides per deployment slot.
- Data pipeline: JSON-driven tables for blocks, items, ores, recipes; ensure loaders validate presence/types.
- Observability + QA: protocol validator (`SharedProtocol/EnhancedMinecraft/*`), chunk payload diagnostics, worldgen sampling tools, deterministic seeds for repeatable tests.

## Execution order for this iteration
1. Core: tighten terrain generation (caves/rivers/lakes) + world map control sync.
2. Core: clean protobuf usage (EnhancedMinecraftProtocol only) and validate registry/descriptors.
3. Utility: refresh configs/docs to keep JSON + proto flows reproducible, then re-run builds/tests.
