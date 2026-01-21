# 2026-01-21 Feature Categorization (Core / Content / Utility)

Source of truth for the full catalog: `config/minecraft_feature_client_server_core_content_util_2026-01-21.json`. This snapshot highlights the primary features that must stay aligned across client and server while we iterate on terrain, networking, and protobuf plumbing.

## Client Features
- **Core**: chunk streaming & mesh rebuilds, map-control profile bootstrap, network bootstrap/keepalive/auth, player state sync, block placement/break + inventory HUD, session lifecycle, world-gen preview.
- **Content**: biome-tinted terrain (rivers/lakes/caves), shoreline/wetland/aquifer viz, structure/loot preview hooks, ambient FX/audio, day/night + weather, block/item/entity rendering.
- **Utility**: debug overlays + perf monitor, JSON config loading (StreamingAssets), protobuf desync/error reporting, localization/analytics stubs, logging, UI (menus/inventory/crafting/status/loading/messages), save/load.

## Server Features
- **Core**: world map-control generation/cache/export, hydrology/flow cache feeding caves/rivers/lakes, session lifecycle/auth/keepalive handlers, chunk save/load with profile hash, network routing, movement/interaction validation, block change broadcast, world seed management.
- **Content**: JSON-driven biome/loot/structure tables, cave/river/lake gen with riparian sealing, weather scheduler + progression, data-driven block/ore distribution, entity spawning/AI, crafting, inventory, health/hunger systems.
- **Utility**: JSON config with reload hooks + versioning, monitoring/logging/admin commands, protobuf DTO registration/validation, data-driven tuning (drops/mobs/XP), database persistence, profiling/memory/object pooling.

## Notes
- All features are tracked by ID, priority, status, and owning files in the JSON catalog. Use the JSON when planning work or validating coverage.
- Terrain, hydrology, and protocol changes in this session must keep client/server parity: `GameServer/World/Generation/*`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, and `SharedProtocol/EnhancedMinecraft/*`.

