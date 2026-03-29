# Minecraft Feature Categories (2026-02-12)

## Core
- World generation: hydrology edge-bleed smoothing, flow-aware river/lake masks, cave stability pillars, map-control aligned noise masks.
- Networking & protocol: EnhancedMinecraft Google.Protobuf pipeline with descriptor fingerprinting and required-message descriptor bindings.
- Player/block systems: player control/auth, block placement/breaking, chunk lifecycle, JSON-driven configs.
- World map control & hydrology sync: hashed profile export (`config/world_map_control_profile.json`), auto-reload + cache flush when the hash drifts, Unity preview parity.
- World config & map-control parity: shared JSON knobs across `config/world.json`, StreamingAssets, and `WorldAreaManager` hydration.

## Content
- Items & equipment: JSON-driven items, durability, tool/weapon/armor balance follow-ups.
- Crafting system: hand/workbench/furnace crafting with recipes, UX improvements planned.
- Mobs & entities: basic entity pipeline, AI/breeding/boss systems pending.
- Structures & buildings: dungeons present, villages/mineshafts/strongholds planned.
- World features: weather/time/biomes active, seasonal/dimension mechanics queued.
- Ores & resources: data-driven ore distribution with further geode/fossil variants planned.

## Utility
- UI/UX: chat, login, HUD and inventory; roadmap for map UI, recipe book, accessibility.
- Server management: auth/permissions foundations; backups, metrics, admin tooling pending.
- Development tools: config/data editors, profiling and network diagnostics to follow.
- Data management: JSON configs + SQLite saves with integrity/backup goals.
- Performance & optimization: chunk loading/rendering and multithreading improvements in progress.

## Execution Order
1. Core worldgen/map-control alignment (edge bleed, river/lake seam smoothing, cave stability pillars) and profile hash validation.
2. Protocol guardrails (descriptor binding checks for required packets, handler coverage) and protobuf self-tests in CI/local builds.
3. Content balance passes after terrain/protocol changes (items, mobs, ores) followed by UI/utility polish.
