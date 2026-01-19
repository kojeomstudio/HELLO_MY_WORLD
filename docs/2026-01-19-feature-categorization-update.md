# 2026-01-19 Feature Categorization Update

## Artifacts
- Updated master categorization: `minecraft_feature_core_content_util.json` (last_updated 2026-01-19).
- Session breakdown: `config/minecraft_feature_client_server_core_content_util_2026-01-19-session-06.json` (core/content/util split for client + server).

## Client
- **Core**: chunk streaming/mesh + map-control hash checks; hydrology/erosion overlays; block interaction + HUD with water-level awareness; lighting/fog tuned by biome moisture; protobuf bootstrap guards.
- **Content**: biome-tinted rivers/lakes/wetlands; cave/river/lake visualization with erosion shading; structure hooks respecting hydrology; crafting UI with JSON recipes; particles + ambient water/cave audio.
- **Util**: telemetry + debug overlays (chunk edges, hydrology, erosion risk); JSON config load from StreamingAssets; proto desync/error reporting; localization scaffolding.

## Server
- **Core**: world map control generation/caching with erosion-risk masks; hydrology/flow/erosion cache feeding improved terrain pipeline; session lifecycle/auth/keepalive; chunk save/load with profile hash + proto fingerprints.
- **Content**: biome/loot JSON tables; structure placement honoring hydrology + erosion; cave/river/lake generation with riparian sealing; weather scheduler and progression events.
- **Util**: JSON config management (`server-config.json`, `config/world.json`, `config/world_map_control_profile.json`); admin/monitoring for map/profile reload; protobuf registry validation + fingerprints; data-driven tuning for drops/mobs/XP.

## Sequential Implementation (session focus)
1. Map-control parity: refresh profile hash and ensure hydrology/erosion knobs align across server + Unity (`config/world_map_control_profile.json`, `config/world.json`, `Assets/StreamingAssets/world-map-control.json`).
2. Terrain synthesis: apply erosion-risk-aware hydrology masks to rivers/lakes/caves in both server pipeline and MapGeneratorLib previews.
3. Protocol audit: validate generated protobuf DTO registration and handler coverage; align namespaces/imports for client/server handlers.
4. Content glue: wire biome/structure/mob placement to updated hydrology + erosion signals; keep JSON tables in sync.
5. Observability: telemetry overlays, proto fingerprint logging, config parity checks after builds/tests.

## Data-Driven Sources
- `config/world.json`, `config/world_map_control_profile.json`, `config/world.default.json`
- `Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`
- `Assets/Generated/Protobuf`, `proto/*.proto`
