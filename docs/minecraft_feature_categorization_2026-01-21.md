# Minecraft Feature Categorization - 2026-01-21

Snapshot of Minecraft-like features grouped by Core / Content / Utility with pointers to primary files and immediate actions.

## Core
- World Generation (WorldGenAlgorithms.cs, ImprovedTerrainGenerator.cs, ChunkManager.cs) ? refine caves, river/lake carving, multi-layer noise, hydrology parity.
- Networking & Protocol (GameServer/Handlers, Assets/MyAssets/Scripts/Network, Assets/Generated/Protobuf) ? ensure handler registration, versioning, and consistent Protobuf DTO usage.
- Player & Block Systems (PlayerController, BlockDataManager) ? keep state sync stable; expand block states as data.
- Chunk & Map Control (GameServer/WorldMapController.cs, WorldMapControlProfile.cs in server/client) ? keep profile hash parity and streaming guards.
- Configuration & Data Pipeline (config/*.json, Assets/MyAssets/Scripts/DataFiles) ? validate JSON schema, support env overrides and hot reload hooks.

## Content
- Items & Crafting (enhanced_game_data.json, crafting_recipes.json, CraftingManager) ? tiered tools, book/queue UX.
- Mobs & Entities (EntityInfo, spawning rules) ? AI behaviors, drops, taming, breeding.
- Structures & World Features (WorldGenAlgorithms.cs structure hooks) ? villages/temples/mineshafts plus biome-tuned placements.
- Ores & Resources (ore config JSON, generation tuning) ? rare variants, geodes, fossils, balanced distribution.
- Weather/Day-Night/Biomes (WorldConfig, environment handlers) ? seasonal effects and custom world presets.

## Utility
- UI & UX (Assets/MyAssets/Scripts/UI/*) ? inventory/crafting maps, accessibility, stats/achievements surface.
- Server Operations (GameServer/Configuration, SessionManager.cs) ? permissions, backups, monitoring, remote admin.
- Development & Diagnostics (ProtoDiagnostics, profiling tools) ? handler audits, proto rebuild checks, worldgen profiling.
- Data Management (DataFiles, SQLite integration) ? backups, import/export, version compatibility and integrity checks.
- Performance (chunk loading/rendering, network bandwidth) ? multithreading, culling, asset load optimization.

## Implementation Order (current session focus)
1) Core: worldgen hydrology + map control parity + proto audit
2) Utility: diagnostics/config validation to de-risk data-driven flow
3) Content: light tuning hooks for structures/ores to align with new terrain shapes

Use this as the execution list for the session and update alongside commits.