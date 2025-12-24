## Minecraft core/content/util rollout (latest 2025-12-24)
- Shared JSON-driven world map control profile (generated in `GameServer/World/WorldManager.cs`, consumed by `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`) keeps caves, rivers, lakes, and hydrology masks aligned between server chunks and Unity previews.
- Map generation stays data-driven: knobs live in `config/world.json` (server) and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (client) and are exported to `config/world_map_control_profile.json` for auditability/versioning.
- EnhancedMinecraft protobuf remains the canonical packet surface; registry/validator/diagnostics in `SharedProtocol/EnhancedMinecraft` guard descriptor and handler drift on both server and client.

### Core (server vs client split)
- Server: chunk lifecycle + world map control (`World/WorldManager.cs`, `World/Generation/*`), protocol registry/validation (`SharedProtocol/EnhancedMinecraft/*`), session/auth/routing (`SessionManager.cs`, `GameServer.cs`), persistence gates and world save.
- Client: chunk subscription/render/prediction (`Assets/MyAssets/Scripts/GameWorld/*`, `Assets/Scripts/Minecraft/*`), reconnection + interest management, HUD/time/weather projection.
- Data: JSON configs for world/server/client, protobuf DTOs from `proto/*.proto`, world map control profile cache at `config/world_map_control_profile.json`.

### Content (terrain + gameplay)
- Terrain/hydrology: caves, rivers, lakes, shorelines, and riparian stabilization share the same hydrology/curvature caches in `MapGeneratorLib/.../WorldGenAlgorithms.cs` and server chunk generation; parameters stay tuneable through JSON.
- Gameplay: block interaction, inventory/recipes, entities/spawn/combat, container flows, weather/time—authoritative in server handlers/systems, mirrored by client UIs/prediction; protobuf packets carry deltas (`ChunkLoad/Unload`, `BlockChange`, `Entity*`, `TimeUpdate`, `WeatherChange`, `Container*`).
- Data-driven assets: item/block/recipe tables in `config/*.json` drive server validation and client presentation.

### Utility (tooling, ops, validation)
- Protocol health: `ProtocolRegistry`, `ProtocolValidator`, and `ProtoDiagnostics` verify EnhancedMinecraft descriptors are registered and handlers exist; protobuf regeneration stays source of truth.
- World tuning/presets: JSON configs plus generated map-control profile allow deterministic repros; hydrology/cave/river/lake knobs documented in `docs/world-generation.md`.
- Metrics/ops: chunk residency + server status hooks, scripts under `scripts/`, recordings under `Recordings/`.

### Implementation order (current sprint)
1. Harden world map control profile (JSON generation + client bootstrap) and hydrology-aware terrain carving (caves/rivers/lakes).
2. Complete EnhancedMinecraft protocol coverage (registry/validator) and keep Unity-side decoders aligned.
3. Expand chunk lifecycle + entity/container flows with data-driven tuning.
4. Layer remaining content (biomes/structures/loot) and live-tuning utilities as configs mature.
