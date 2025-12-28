## Minecraft core/content/util rollout (2025-12-28)
- Source of truth: `config/minecraft_feature_core_content_util.json` (ordered, JSON, client/server owners). Update the JSON first, then mirror sequencing here.
- Data-driven world/protocol: `config/world.json` ?? `Assets/StreamingAssets/world-config.json` and `config/world_map_control_profile.json` ?? `Assets/StreamingAssets/world-map-control.json` stay hash-checked for parity.
- Ownership split: server worldgen/protocol in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`; client chunk/render/map control in `Assets/MyAssets/Scripts/GameWorld/*` and `Assets/Scripts/Minecraft/World/*`.

### Core (order)
1. `core.chunk_streaming` — chunk streaming/residency via EnhancedMinecraft protobuf, render/sim distances driven by map-control profile.
2. `core.player_state_sync` — authoritative movement/action sequencing across handlers + client dispatcher.
3. `core.block_interaction` — data-driven block edits with queued sync broadcasts and container safety.
4. `core.inventory_crafting` — inventory + crafting with recipe JSON and protobuf container diffs.
5. `core.worldgen_parity` — shared hydrology/riparian/cave sealing pipeline across server, MapGeneratorLib, and Unity terrain.
6. `core.protocol_health` — registry/handler validation (Google.Protobuf) at bootstrap.

### Content (order)
1. `content.biomes_weather` — biome palette, time/weather updates bound to sky/lighting.
2. `content.ores_structures` — ore ranges and structures with wetlands-aware placement.
3. `content.mobs_npcs` — spawn tables + AI hooks, entity spawn/update/despawn sync.
4. `content.redstone_fx` — particle/sound glue tied to block changes and action results.

### Utility (order)
1. `util.config_sync` — JSON config management + map-control hash verification.
2. `util.proto_tooling` — protoc generation/verification; registry standardization.
3. `util.telemetry_logging` — worldgen/packet metrics surfaced via server status.
4. `util.selftest_ci` — self-test loop and Unity Edit/PlayMode coverage.
