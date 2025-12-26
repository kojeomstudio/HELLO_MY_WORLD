## Minecraft core/content/util rollout (latest 2025-12-27)
- Data-driven world map control profile (`config/world_map_control_profile.json`) remains the single source for chunk sizing, hydrology seams, caves/rivers/lakes flags, and riparian knobs; it is emitted server-side in `GameServer/World/WorldManager.cs` and consumed by Unity in `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`.
- World tuning stays JSON-first: `config/world.json` (server) and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (client) feed the profile so terrain previews and authoritative chunks stay aligned.
- EnhancedMinecraft protobuf is enforced through `SharedProtocol/EnhancedMinecraft` registry + validator; Unity networking should call into the same registry to catch descriptor drift after `protoc` regeneration.

### Core (server vs client split)
- Server: chunk lifecycle + hydrology-aware world map control (`World/WorldManager.cs`, `World/Generation/*`), protocol registry/validation (`SharedProtocol/EnhancedMinecraft/*`), session/auth/routing (`SessionManager.cs`, `GameServer.cs`), persistence + world seed.
- Client: chunk subscription/render/prediction + map-control bootstrap (`Assets/MyAssets/Scripts/GameWorld/*`), reconnection + interest management, HUD/time/weather projection using the profile hashes.
- Data: JSON configs for world/server/client, protobuf DTOs from `proto/*.proto`, generated map-control profile cache at `config/world_map_control_profile.json`.

### Content (terrain + gameplay)
- Terrain/hydrology: rivers, lakes, and caves share hydrology/curvature caches in `MapGeneratorLib/.../WorldGenAlgorithms.cs` and server chunk generation; river mouth smoothing, lake shoreline blending, and riparian sealing are tuned through JSON profile fields.
- Gameplay: block interaction, inventory/recipes, entities/spawn/combat, container flows, time/weather authority in server handlers; protobuf packets carry deltas (`ChunkLoad/Unload`, `BlockChange`, `Entity*`, `TimeUpdate`, `WeatherChange`, `Container*`).
- Data-driven assets: item/block/recipe tables in `config/*.json` drive server validation and client presentation.

### Utility (tooling, ops, validation)
- Protocol health: `ProtocolRegistry`, `ProtocolValidator`, and `ProtoDiagnostics` verify EnhancedMinecraft descriptors are registered and handlers exist; protobuf regeneration remains the source of truth.
- World tuning/presets: JSON configs plus generated map-control profile allow deterministic repros; hydrology/cave/river/lake knobs are documented in `docs/world-generation.md`.
- Metrics/ops: chunk residency + server status hooks, scripts under `scripts/`, recordings under `Recordings/`.

### Implementation order (current sprint)
1. Harden hydrology seams and river mouth/lake shoreline blending using the shared map-control profile.
2. Enforce map-control bootstrap on the Unity client (profile hash/versions) to avoid drift from server exports.
3. Validate EnhancedMinecraft registry usage on the client and server after any `.proto` regeneration.
4. Extend chunk lifecycle/content layers (biomes/structures/loot) once hydrology + protocol guardrails stay green.
