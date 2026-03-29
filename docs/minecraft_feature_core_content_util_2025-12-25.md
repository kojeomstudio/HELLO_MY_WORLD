## Minecraft core/content/util (2025-12-25)

### Core (authority, sync, proto)
| Area | Server (authoritative) | Client (Unity) | Data / config |
| --- | --- | --- | --- |
| World map control | Generate/export `world_map_control_profile.json` (riparian smoothing, shelf depth, cave plugs); apply in `WorldManager` pipeline | Load profile + `WorldConfigData.json`, push to `WorldGenAlgorithms`, validate profile hash before previews | `config/world.json`, `Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`, `Assets/StreamingAssets/world-map-control.json` |
| Chunk + world sync | Chunk gen/stream, block/world updates, time/weather; handler coverage + room scoping | Chunk subscription/render/prediction, interest culling, sky/time/weather projection | Protobuf packets from `proto/*.proto`, EnhancedMinecraft registry/handlers |
| Protocol guardrails | `ProtocolValidator.ValidateEnhancedContracts()` + new `ValidateHandlerBindings()` to ensure handler contracts match generated DTOs | `ProtoRuntime.EnsureInitialized()`, chunk bridge validates descriptors | Generated C# under `SharedProtocol/` and `Assets/Generated/Protobuf` |
| Persistence/session | Auth, session routing, room membership, persistence checkpoints | Reconnect, HUD/session status, client-side prediction gates | `server-config.json`, SQLite DB |

### Content (terrain + gameplay loops)
| Area | Server focus | Client focus | Data |
| --- | --- | --- | --- |
| Terrain & hydrology | Riparian-smoothed hydrology, cave riparian plugs, lake shoreline shelves, river edge feathering; chunk-edge stability | MapGeneratorLib mirrors hydrology/cave/lake passes for previews; streaming chunk visuals | World config + map-control profile |
| Blocks/items/entities | Block break/place, inventory/crafting validation, entity spawn/update/despawn | UI, VFX/SFX, prediction/interp for entities and block edits | `config/items.json`, `config/recipes.json`, `config/blocks.json` |
| Systems | Time/weather, hunger/health, combat, containers, rooms | HUD binding, container UI, controls | `server-config.json`, `config/gameplay.json`, protobuf DTOs |

### Utility (tooling, ops, observability)
- Map/control exports: `WorldMapControlProfileUtility.Save` mirrors knobs to hashed JSON; keep server/Unity copies in sync.
- Proto/tooling: `scripts/generate_proto.ps1`, `scripts/verify_protobuf.ps1`, handler coverage via `ProtoDiagnostics.LogHandlerCoverage`.
- Config sync: `scripts/sync_world_config.ps1` copies server world config into Unity JSON; all tunables live in JSON for data-driven iteration.
- Metrics/ops: chunk residency + server status handlers, build logs under `build_*.log`.

### Implementation order (next steps)
1) Refresh world config + map-control profile with riparian smoothing (`RiparianSmoothIterations/Blend/SaturationBoost`), lake shelves (`Lakes.ShelfDepth`), cave plugs (`Caves.RiparianPlugDepth`) on server and Unity copies.  
2) Regenerate `world-map-control.json`/`world_map_control_profile.json` and push hydrology/cave/lake knobs into MapGeneratorLib via `WorldAreaManager`.  
3) Enforce protobuf health: run `ProtocolValidator.ValidateEnhancedContracts()` + `ValidateHandlerBindings()` during startup; regenerate DTOs if any mismatches surface.  
4) Keep content loops data-driven: tweak JSON tables for blocks/items/recipes, re-run `sync_world_config.ps1` when changing world-gen knobs.  
5) Validate via `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`, and Unity map preview to confirm riparian smoothing and shelf plugs align.
