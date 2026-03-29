# Minecraft Features (Session 22 - 2026-01-27)

- Source: `config/minecraft_feature_client_server_core_content_util_2026-01-27-session-22.json`
- Hydrology signature: `2026-01-27-hydrology-shield-v4-flow-lock`
- Map control profile version: `7`

## Core / Content / Utility Highlights
- Client core: world-map control/generator updated for flow-shadow + pressure blend; GameCommon.dll synced with new signature context.
- Server core: enhanced terrain pipeline + world-map control manager rebuilt with extended signature fields and hydrology v4 parameters.
- Content: caves/rivers/lakes tuned (moisture continuity clamp, pressure-stabilised rivers, lake rim/variance tweaks) on both server and Unity preview.
- Utility: JSON configs refreshed (`config/world.json`, `Assets/StreamingAssets/world-config.json`); proto registry audited with optional coverage surfaced via dummy client.

## Terrain Generation Updates
- Caves: hydrology shadow and moisture continuity feed stability/threshold; flow drift clamp reduces riparian bleed.
- Rivers: pressure stabiliser using pressure blend + gradient clamp; flow-shadow weight tied to hydrology signature; edge flow-lock bias included in signature hash.
- Lakes: pressure blend for wetland smoothing; rim erosion weight bumped; variance weight increased for basin shaping.

## Protocol & Shared DLL
- ProtocolRegistry now enforces required bindings and reports missing optional IDs; dummy client builds TimeUpdate, ChunkDataRequest, and BlockChangeNotification frames.
- Shared DLL/GameCommon rebuilt with new `WorldMapSignatureContext` fields; copy to `Assets/Plugins/GameCommon.dll` after build for Unity parity.

## Files
- Config: `config/minecraft_feature_client_server_core_content_util_2026-01-27-session-22.json`
- World configs: `config/world.json`, `Assets/StreamingAssets/world-config.json`
- World map control/profile: `GameServer/World/WorldMapControlManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
- Proto registry/dummy client: `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `GameServer/Testing/DummyProtocolClient.cs`
