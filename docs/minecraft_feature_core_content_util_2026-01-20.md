# Minecraft Features by Category (2026-01-20)

This session groups required Minecraft client/server features by **Core**, **Content**, and **Utility** so implementation can proceed in sequence. Commit context: `3616c383` (worldgen pressure balance + proto guard).

## Client
- **Core**: world-map profile preload, chunk streaming/mesh rebuilds under map-control gating, reconnect/keepalive, block place/break + HUD.
- **Content**: biome-tinted terrain with rivers/lakes/caves, shoreline/wetland/aquifer visuals, ambient FX/structure preview hooks.
- **Utility**: StreamingAssets JSON loaders, map-control preview + debug overlays, protobuf desync/error reporting, localization/analytics stubs.

## Server
- **Core**: world map control generation/cache + profile export, hydrology/flow cache feeding caves/rivers/lakes, session lifecycle/auth/keepalive, chunk save/load with profile hash.
- **Content**: JSON-driven biome/loot/structure tables, riparian-safe cave/river/lake generation, weather scheduler, data-driven blocks/ore distribution.
- **Utility**: JSON config management with reload hooks, monitoring/logging/admin commands, protobuf DTO registration/validation, tuning knobs exposed via JSON.

## Implementation order for this session
1) **Protocol integrity**: validate Google.Protobuf DTO usage and handler registration, fix `using` drift.  
2) **Terrain synthesis**: improve cave connectivity/support, river flow alignment/bank erosion, and lake rim sealing tied to hydrology pressure balance.  
3) **World map control parity**: strengthen profile hash checking and JSON-driven knobs on server/client.  
4) **Docs/configs**: update README + docs and ensure new tuning lands in JSON (server/client/world map).
