## Minecraft core/content/util rollout (2026-01-02)

- Source JSON: `config/minecraft_feature_core_content_util_2026-01-02.json` (update first, mirror here + `docs/minecraft_feature_core_content_util_latest.md`).
- Scope: flow-aware worldgen parity (server + Unity), map-control/profile reload safety, protobuf registry validation, and data-driven configs.
- Config: `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`.

### Core
1. **Flow-coupled worldgen** — blend hydrology/flow masks and damp caves under active flow; rivers/lakes respect chunk-edge seam blending.  
   - Server: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedCaveGenerator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`  
   - Client: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `WorldMapControlProfile.cs`
2. **World-map control reload + cache limits** — detect `world.json`/profile writes, rebuild the profile, reset pipelines, and keep preview chunk caches bounded.  
   - Server: `GameServer/World/WorldMapControlManager.cs`, `WorldMapControlProfile.cs`  
   - Client: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
3. **Protocol health** — parser + fingerprint validation so EnhancedMinecraft DTOs stay aligned before handlers run.  
   - Shared: `SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs`, `ProtocolValidator.cs`  
   - Client: `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`, `Assets/Scripts/Minecraft/Core/EnhancedProtoManifest.cs`

### Content
4. **Wetlands & outflows** — lake basins add inflow/outflow weighting and buffered wetlands for believable water bodies.  
   - `ImprovedLakeGenerator.cs`, `EnhancedTerrainGenerationPipeline.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
5. **Flow-aligned rivers** — downhill-aware anisotropy plus seam blending keeps river channels continuous across streamed chunks.  
   - `ImprovedRiverGenerator.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`

### Utility
6. **Config/profile sync** — JSON-first configs & feature matrix stay the source of truth for server/client parity.  
   - `config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, `Assets/StreamingAssets/world-map-control.json`, `config/minecraft_feature_core_content_util_2026-01-02.json`
