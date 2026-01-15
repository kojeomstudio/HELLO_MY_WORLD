# Terrain Generation & Protocol Updates (2026-01-15)

## Worldgen Improvements
- Server hydrology/flow masks now run variance blending, directional smoothing, and gradient stability clamps in `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs` using existing JSON knobs (`HydrologyVarianceBlend`, `HydrologyDirectionalIterations/Blend`, `HydrologyGradientStabilityIterations/Blend/Clamp`). This matches Unity previews and MapGeneratorLib smoothing to cut cave/river/lake seam jitter.
- Flow masks reuse the same directional + gradient stability passes before edge normalization so river depth/pressure stays continuous across chunk boundaries.
- No new config keys were added; behavior is fully data-driven through existing `config/world.json` + `config/world_map_control_profile.json`.

## Protocol Alignment
- Unity client now binds EnhancedMinecraft packets to the generated classes (Broadcast messages) and runs `ProtocolRegistry.ValidateBindings()` during startup in `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`, preventing stale `using` references and catching missing DTOs early.
- Added EnhancedMinecraft events for block changes, entity spawn/despawn, and world time/weather broadcasts so scene systems can subscribe without touching raw registry plumbing.

## Feature Inventory
- Session feature breakdown (Core/Content/Utility, client/server) stored in `config/minecraft_feature_client_server_core_content_util_2026-01-15.json` with a readable summary at `docs/minecraft_feature_core_content_util_2026-01-15.md`.

## Next Steps
- Regenerate map-control profiles if configs change (version bump optional) to propagate the stabilized hydrology/flow outputs into StreamingAssets.
- Wire the new EnhancedMinecraft events into client-side entity/world systems to visualize incoming broadcasts.
