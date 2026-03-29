# Minecraft Feature Execution Plan

This checklist mirrors `config/minecraft_feature_core_content_util.json` and keeps the core/content/utility roadmap data-driven. Update the JSON first, then reflect high-level notes here when priorities shift.

## Categories & Current Focus
- Core: world generation, networking/protocol, player/chunk systems, configuration, world map control/hydrology sync (`core_007`), protocol validation guardrails (`core_008`).
- Content: items/equipment, crafting, mobs/entities, structures, world mechanics, ores.
- Utility: UI/UX, server management, development tools, data management, performance/optimization.

## Immediate Execution Order
1) Align map-control profile + hydrology defaults across server/client (`core_001`, `core_007`).  
2) Harden protobuf/handler validation and registry coverage (`core_002`, `core_008`).  
3) Rebalance content impacted by terrain/protocol changes (`content_001`, `content_003`, `content_006`).  
4) Polish utility UX and performance knobs (`util_001`, `util_005`).  

## Data Sources
- JSON source of truth: `config/minecraft_feature_core_content_util.json` (includes IDs, status, improvement targets, and execution_order).  
- Config references: `config/world.json`, `config/world_map_control_profile.json`, `server-config.json`, `client_config.json`.  

Keep this plan in sync with the JSON so the server, Unity client, and docs all reference the same feature IDs and sequencing.
