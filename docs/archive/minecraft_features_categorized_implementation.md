# Minecraft Feature Map (Core / Content / Utility)

This checklist groups required Minecraft-like features by category and splits ownership across the dedicated server and Unity client. Items marked ✅ are already present; unchecked items are planned/ongoing. Data flows are expected to stay data-driven (JSON configs/data tables + protobuf DTOs).

## Core (Server)
- ✅ Chunked world generation (terrain heightmaps) driven by `config/world.json`
- ☐ Advanced terrain carving (caves/rivers/lakes) with shared hydrology masks
- ☐ Chunk lifecycle: generate -> stream -> delta sync -> unload/ack
- ✅ Session/auth lifecycle and player state cache
- ☐ World/block change batching + near-player interest management
- ☐ Environment simulation hooks (time/weather/bome stubs)
- ☐ Persistence gates for player/world saves
- ☐ Protobuf protocol registry validation + handler coverage

## Core (Client)
- ✅ Chunk render/cache pipeline and seed alignment from server
- ☐ Chunk subscription controller (load radius, unload acks)
- ☐ Player movement sync + remote player interpolation
- ☐ Block placement/removal requests + local prediction guardrails
- ☐ Time/weather/bome visual hooks
- ✅ Inventory/state application from server payloads
- ☐ Disconnection/reconnect safety for streamed chunks

## Content (Shared/Gameplay)
- ✅ Crafting/inventory base systems and item database (JSON tables)
- ☐ Recipe progression gates + smelting/brewing/enchanting
- ☐ Equipment stats + durability tuning
- ☐ Mobs/AI spawn tables and loot (JSON-driven)
- ☐ Farming/fishing/food effects and buffs
- ☐ Biome-aware block/loot tables and decorations

## Utility & Tooling
- ✅ Logging/diagnostics (editor + runtime)
- ✅ Data-driven configs (JSON for world, items, recipes; protobuf for packets)
- ☐ World generation tuning UI + preset management
- ☐ Network/protocol health dashboard (startup summaries, handler coverage)
- ☐ Admin/dev commands for chunk reload, seed swap, and profiling
- ☐ Automated smoke tests (server build + protobuf sanity)

## Implementation Order (current focus)
1. Align world generation (caves/rivers/lakes) and chunk synchronization between server and client using JSON-driven knobs.
2. Harden protobuf packet handling (registry validation, chunk payload DTOs, handler bindings).
3. Expand chunk lifecycle (load radius control, unload acks) and player/world sync.
4. Layer in content progression (recipes/equipment/mobs) with JSON data tables.
5. Add tooling (preset manager, protocol diagnostics, admin commands) to support future iterations.
