# 2026-01-25 Riparian Flow Bridge & Map-Control Sync

- **Pipeline version:** `2026-01-25-riparian-bridge`
- **Scope:** Hydrology/cave/river/lake coupling, world-map control parity, protobuf runtime guard, feature catalog refresh.

## Changes
- Added a riparian flow bridge pass that blends downhill/seam hydrology and flow with edge/tangent weights before feature generation (server `ImprovedTerrainCoordinator`, Unity `WorldMapController`/`EnhancedTerrainGenerator`). Reduces chunk seams and keeps wet caves aligned with river/lake pressure.
- Tuned river/lake masks to respect edge flow lock/bias and directional blend so channels and basins stay coherent near seams and wetlands (`ImprovedRiverGenerator`, `ImprovedLakeGenerator`, Unity river/lake masks).
- Increased cave suppression in wet/riparian corridors via hydrology envelope + flow continuity penalties on both server and client cave masks.
- World-map control signature now includes `HydrologyEdgeTangentWeight` and bumps pipeline version to invalidate stale previews/caches.
- Unity world-map controller now initializes `ProtoRuntime` on startup to ensure generated EnhancedMinecraft protobufs are validated alongside manifest fingerprints.
- Feature catalog refreshed: `config/minecraft_feature_client_server_core_content_util_2026-01-25.json` (core/content/util split with sequencing).

## Affected Files (key)
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`, `ImprovedCaveGenerator.cs`
- `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`
- `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
- `GameServer/World/WorldMapControlManager.cs`
- `config/minecraft_feature_client_server_core_content_util_2026-01-25.json`

## Config & Data
- No new knobs added; changes rely on existing JSON values (`HydrologyEdge*`, directional blends, riparian weights). Signatures now capture `HydrologyEdgeTangentWeight`.
- Data remains JSON-driven for both server (`config/*.json`) and Unity (`StreamingAssets/world-config.json` mirrors).

## Validation
- Build SharedProtocol then GameServer:  
  - `dotnet build SharedProtocol/SharedProtocol.csproj`  
  - `dotnet build GameServer/GameServer.csproj`
- Unity side: ensure `StreamingAssets/world-config.json` and profile hashes match; new pipeline version requires refreshed previews.

## Notes
- Proto runtime guard (`ProtoRuntime.EnsureInitialized`) now runs in the Unity world-map controller to catch stale generated DTOs before map-control traffic.
- World-map control generation signatures include tangent weight + pipeline version to force cache invalidation after hydrology tuning.
