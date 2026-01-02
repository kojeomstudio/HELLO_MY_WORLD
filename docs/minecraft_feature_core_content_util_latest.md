## Minecraft core/content/util rollout (2026-01-02)
- Source of truth: `config/minecraft_feature_core_content_util_2026-01-02.json` (ordered, JSON, client/server owners). Update the JSON first, then mirror sequencing here.
- Data-driven world/protocol: `config/world.json` + `Assets/StreamingAssets/world-config.json` and `config/world_map_control_profile.json` + `Assets/StreamingAssets/world-map-control.json` stay hash-checked for parity.
- Ownership split: server worldgen/protocol in `GameServer/World/*` + `SharedProtocol/EnhancedMinecraft/*`; client previews/network in `Assets/MyAssets/Scripts/GameWorld/*` and `Assets/Scripts/Minecraft/*`.

### Core (order)
1. `core.worldgen_flow_coupling` ??hydrology+flow blending, flow-suppressed caves, river/lake seam smoothing (server + Unity preview).
2. `core.worldmap_control_reload` ??detect world/profile writes, rebuild map-control profiles, reset terrain pipeline, bound preview chunk caches.
3. `core.protocol_health` ??EnhancedMinecraft parser + fingerprint validation before handler registration on server/Unity.
4. `core.chunk_streaming` ??chunk streaming/residency via EnhancedMinecraft protobuf, render/sim distances driven by map-control profile.

### Content (order)
1. `content.hydrology_wetlands` ??lake wetlands/outflows with inflow weighting and downhill channel hints.
2. `content.river_anisotropy` ??downhill-aware river anisotropy + seam blending for coherent channels.
3. `content.biomes_weather` ??biome palette, time/weather updates bound to sky/lighting.
4. `content.ores_structures` ??ore ranges and structures with wetlands-aware placement.

### Utility (order)
1. `util.config_sync` ??JSON config management + map-control hash verification.
2. `util.proto_tooling` ??protoc generation/verification; registry standardization with parser checks.
3. `util.telemetry_logging` ??worldgen/packet metrics surfaced via server status.
4. `util.selftest_ci` ??self-test loop and Unity Edit/PlayMode coverage.
