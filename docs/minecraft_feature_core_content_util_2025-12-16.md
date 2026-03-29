## Minecraft core/content/util rollout (2025-12-16)
- Hydrology curvature smoothing + confluence-aware rivers now land in both the dedicated server (`GameServer/World/WorldManager.cs` + `GameServer/World/Generation/ImprovedWorldGeneration.cs`) and Unity preview (`MapGeneratorLib/.../WorldGenAlgorithms.cs`), driven by new JSON knobs in `config/world.json` and `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.
- Proto diagnostics now report handler coverage via `ProtoDiagnostics.LogHandlerCoverage`, keeping the generated EnhancedMinecraft packets and registered handlers aligned during startup.

### Core (authority, world map control, protocol)
- World map control profile (chunk/render/simulation distance, water level, gradient stability, curvature weight, lake basin smoothing) stays data-driven via `WorldMapControlProfile` on server/client and the mirrored world config JSON.
- Chunk + protobuf safety: `ProtocolValidator.ValidateEnhancedContracts()` plus `ProtoDiagnostics.LogHandlerCoverage()` guard registry/handler gaps so every `MinecraftMessageType` backed by generated DTOs is registered and dispatched.
- Session/auth/movement: server-side heartbeat and spawn, client prediction/interp, room-aware chunk routing (`SessionManager`, `RoomManager`, `Handlers/*`); protocol IDs from `proto/*.proto` -> `SharedProtocol/EnhancedMinecraft`.

### Content (terrain/worldgen, gameplay)
- Terrain & hydrology: curvature-weighted gradients, confluence-aware river banks, and lake basin smoothing applied to caves/rivers/lakes across server + Unity previews (hydrology/flow/curvature caches, improved banks/wetlands/entrances).
- Biomes/weather/sky: world time + weather broadcasts with client FX; biome-aware vegetation and ore passes.
- Entities, combat, items: authoritative spawn/update/despawn + combat resolution and inventory/crafting; client rendering and UI; DTOs registered in `ProtocolRegistry`.

### Utility (data, tooling, ops)
- Config + data parity: worldgen knobs live in JSON (`config/world.json`, Unity `WorldConfigData.json`) with `WorldGenerationConfig`/`WorldConfigFile` readers; additional world-control data exposed through `WorldMapControlProfile`.
- Protobuf pipeline + diagnostics: IDL under `proto/`, generated assets in `SharedProtocol` + `Assets/Generated/Protobuf`, registry/handler coverage logging via `ProtoDiagnostics`.
- Build/test hooks: `dotnet build SharedProtocol/SharedProtocol.csproj` and `dotnet build GameServer/GameServer.csproj` remain the smoke checks; protoc regeneration via `scripts/generate_proto.*`.

### Execution order (sequential rollout)
1. Core parity: load JSON configs -> populate `WorldMapControlProfile` (chunk/render/sim/water/curvature/basin smoothing) on server/client.
2. Content passes: run hydrology/flow/curvature builds -> carve rivers/lakes/caves with the new curvature/confluence tuning in server chunks and Unity previews.
3. Protocol guardrails: initialize `ProtoRuntime` -> `ProtocolValidator.ValidateEnhancedContracts()` -> `ProtoDiagnostics.LogHandlerCoverage()` to ensure generated DTOs and handlers stay in sync.
