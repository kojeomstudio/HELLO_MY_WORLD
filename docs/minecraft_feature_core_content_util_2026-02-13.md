# Minecraft Feature Matrix — 2026-02-13

## Core
- World Generation (core_001): hydrology-aware terrain masks with biome-aware carving and seam smoothing.
- Networking & Protocol (core_002): Google.Protobuf pipeline with handler validation and descriptor fingerprinting.
- World Map Control & Hydrology Sync (core_007): hashed map-control profile shared between server and Unity previews.
- EnhancedMinecraft Protocol Validation (core_008): registry coverage, parser binding checks, descriptor/assembly guards.
- World Config & Map Control Parity (core_009): keep StreamingAssets/world and map-control JSON aligned with server knobs.
- Terrain Mask Parity (core_010): reuse improved hydrology/flow masks across server chunk generation and map previews; reload pipelines when profile hashes drift.

## Content
- Items & Equipment (content_001): tools/armor/weapons with tiering and durability.
- Crafting System (content_002): hand/workbench/furnace crafting backed by JSON recipes.
- Mobs & Entities (content_003): basic entity spawning with planned AI and drops.
- Structures & Buildings (content_004): planned villages, dungeons, mineshafts, and blueprint-driven builds.
- World Features (content_005): biomes, weather/day-night, future dimensions/portals.
- Ores & Resources (content_006): JSON-driven ore distribution and vein generation.

## Utilities
- User Interface (util_001): HUD, inventory UI, map toggles, accessibility backlog.
- Server Management (util_002): console, auth, permissions, monitoring/backups roadmap.
- Development Tools (util_003): debugging/config editors, planned world/editor tooling.
- Data Management (util_004): JSON configs, SQLite persistence, validation and backup goals.
- Performance & Optimization (util_005): chunk loading, multithreading, bandwidth/rendering/memory optimizations.

## Execution Order (current)
1) Align map-control profile and hydrology defaults (core_001, core_007, core_009, core_010)  
2) Protocol validation and packet registry hardening (core_002, core_008)  
3) Content balance after terrain/protocol changes (content_001, content_003, content_006)  
4) Utility polish and tooling (util_001, util_005)
