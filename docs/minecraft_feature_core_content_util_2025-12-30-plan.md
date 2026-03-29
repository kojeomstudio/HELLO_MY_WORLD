# Minecraft Feature Matrix (Core / Content / Util)

Latest breakdown of required Minecraft-style features for both server and client. Items are grouped by category and ordered for implementation so server/client stay in lockstep.

## Server
- Core: protobuf contract validation + registry guards; session/login + rate limiting; chunk lifecycle (load/save/unload) with hydrology-aware generation; world map control profile publish + hash validation; authoritative block/entity sync.
- Content: biome-aware terrain blending; improved caves/rivers/lakes shaping with erosion + warp; ore/vegetation placement respecting hydrology; day/night + weather broadcast hooks; crafting/recipe sync.
- Util: data-driven JSON configs (server/world/gameplay/network/world_map_control_profile); config hot-reload + backups; metrics/trace hooks for worldgen latency; admin commands for map/profile reload; protobuf diagnostics export.

## Client
- Core: protobuf-driven network manager; chunk streaming + culling; world map preview/overlay honoring server profile hash; player/compass HUD sync with server ticks.
- Content: biome shading + water rendering tied to map-control profile; cave entrance hints and river/lake banks for navigation; block interaction + inventory UIs.
- Util: JSON-driven world config mirroring server; debug overlays for chunk gen time + proto stats; graceful fallback when profile hash mismatch; offline cache for map preview data.

## Ordered Implementation Steps
1) Wiring: validate protobuf contracts at startup, load JSON configs via `DataDrivenConfigManager`, and export updated `world_map_control_profile.json`/hash.  
2) Terrain/hydrology: apply improved river/lake/cave masks and seam smoothing; feed knobs into `WorldMapController` + Unity preview.  
3) Networking: route chunk/map packets through the protocol registry with size/descriptor checks; add diagnostics for malformed payloads.  
4) Gameplay/content: re-run ore/vegetation passes with new heightfields; ensure crafting/inventory remain deterministic.  
5) Utilities: enable config hot-reload/backups, expose metrics for chunk gen + proto throughput, and surface admin reload commands.
