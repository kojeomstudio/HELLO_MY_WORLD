# Minecraft Feature Categories (Core / Content / Util)

Purpose: inventory of required client/server capabilities for Minecraft-style gameplay, grouped by Core, Content, and Util to drive sequential implementation and data-driven configs.

## Client
- **Core**: chunk streaming + mesh generation; world map control preload; block interaction & placement; inventory UI; damage/health HUD; lighting & fog; network session/bootstrap.
- **Content**: biome-tinted terrain/foliage; cave/river/lake visualization (water levels, wetland blending); mobs & NPC rendering hooks; crafting UI flow; particles (block break, fluids, weather); ambience (music/sfx events such as `AMBIENT_CAVE`).
- **Util**: analytics/telemetry hooks; debug overlays (chunk bounds, hydrology/cave masks); config loading from JSON (StreamingAssets) for world gen/render; localization scaffolding; error/report pipeline for proto desync.

## Server
- **Core**: world map control generation; hydrology + terrain cache; chunk save/load; player/session lifecycle; combat/damage; inventory/crafting rules; authentication/handshake packets.
- **Content**: biome table + loot tables (JSON-driven); structure placement (villages, ores, dungeons); cave/river/lake generation with hydrology-aware sealing; mob spawn controller; weather scheduler; achievements/progression events.
- **Util**: config management (JSON for `server-config.json`, worldgen profiles); monitoring/logging; admin commands; protobuf DTO registration/validation; data-driven tuning (drop rates, mob stats, XP curves).

## Sequenced Delivery (shared priorities)
1) World map control parity: hydrology/cave/river/lake parameters persisted to JSON (server) and consumed by client bootstrap.
2) Terrain synthesis: improved cave sealing + river/lake continuity; chunk mesh/render alignment on client.
3) Gameplay loops: block interact/place/break, inventory/crafting, combat/damage sync.
4) Content expansion: biomes, mobs, structures, loot tables (JSON-driven).
5) Observability + validation: telemetry, debug overlays, protobuf packet health checks, and admin tools.

Data reference: JSON mirror stored at `config/minecraft_feature_client_server_core_content_util_2026-01-19.json`.
