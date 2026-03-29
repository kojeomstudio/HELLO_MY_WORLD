# Terrain Generation & Proto Update (2026-01-15 Session 02)

## Summary
- Added a hydrology envelope pass that boosts river/lake masks with seam-aware continuity before carving, reducing chunk-edge artifacts and flooded cave edges.
- Carving now respects hydrology gradients near chunk borders, adding stability penalties to plug edge caves where water pressure is high.
- Map-control generation signatures include a pipeline version stamp so Unity previews invalidate stale caches when worldgen logic changes.
- Unity protobuf client bootstrap now runs `ProtoRuntime.EnsureInitialized()` and `ProtoDiagnostics.AssertRegistryClean()` alongside registry validation to catch missing DTO references early.

## Implementation Notes
- Server worldgen: `GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`
  - New `ApplyHydrologyEnvelope` blends hydrology/flow with edge continuity for river/lake masks.
  - River/lake depth and wetland pressure calculations incorporate continuity boosts; edge caves honor hydrology gradients.
- Client worldgen parity: `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`
  - Mirrors the hydrology envelope and continuity-aware depth/wetland adjustments so previews match server chunks.
- Map control: `GameServer/World/WorldMapControlManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
  - Pipeline version baked into generation signatures; cache invalidation triggers on version/config/profile changes.
- Proto guardrails: `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`
  - Added ProtoRuntime + ProtoDiagnostics checks to the Unity bootstrap path to surface stale generated classes.
- Feature tracking: `config/minecraft_feature_client_server_core_content_util_2026-01-15-session-02.json`, `docs/minecraft_features_client_server_core_content_util_2026-01-15-session-02.md`.

## Data & Config
- No new schema keys; existing JSON sources (`config/world.json`, `Assets/StreamingAssets/world-config.json`, `config/world_map_control_profile.json`) remain the single source for hydrology/cave tuning. Pipeline versioning protects against stale caches without altering JSON structure.

## Next Steps
- Extend handler coverage checks in `ProtocolRegistry`/`ProtocolValidator` to reflect any new EnhancedMinecraft messages.
- Wire map visualization UI to expose wetland/shoreline indicators now available from continuity-aware preview masks.
