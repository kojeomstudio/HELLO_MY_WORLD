# Minecraft Client/Server Feature Categorization (2026-01-18 Session 04)

Source data: `config/minecraft_feature_client_server_core_content_util_2026-01-18-session-04.json`

## Core
- **Client**
  - `client-worldmap-preview`: Hydrology-aware map preview pipeline with protobuf chunk handshake.
  - `client-profile-reload`: Profile reload/resync on generation signature drift using JSON defaults.
  - `client-network-sync`: World map request/response binding to generated protobuf DTOs.
- **Server**
  - `server-mapcontrol-pipeline`: WorldMapControlManager + enhanced terrain pipeline with hydrology signature and cache.
  - `server-remote-profile`: Profile hash validation and regeneration from JSON defaults on drift.
  - `server-proto-routing`: World map request/response handlers using generated protobuf types and compression.

## Content
- **Client**
  - `client-hydrology-visual`: Preview overlays for rivers/lakes/cave skylight from data-driven palettes.
  - `client-biome-overlay`: Biome/height overlays driven by configurable palettes.
- **Server**
  - `server-river-delta`: Hydrology-guided river mask with confluence stability and delta smoothing.
  - `server-lake-basin`: Lake basins with wetland buffers/outflow carving tuned to flow and hydrology.
  - `server-cave-support`: Cave mask layering moisture stability, pillar placement, and ceiling sealing.

## Util
- **Client**
  - `client-config-loader`: JSON loaders for world map profile/config with hash validation.
  - `client-proto-diagnostics`: Protobuf ID diagnostics for world map packets.
- **Server**
  - `server-config-guards`: Validation/sha tracking for `world_map_control_profile.json` and hydrology configs.
  - `server-telemetry`: Terrain pipeline telemetry (flow/hydrology) surfaced in protobuf responses.

## Implementation Order
1. `server-mapcontrol-pipeline`
2. `server-river-delta`
3. `server-lake-basin`
4. `server-cave-support`
5. `client-worldmap-preview`
6. `client-profile-reload`
7. `server-config-guards`
8. `client-config-loader`

Use the sequence above to drive incremental implementation and testing across server and client.
