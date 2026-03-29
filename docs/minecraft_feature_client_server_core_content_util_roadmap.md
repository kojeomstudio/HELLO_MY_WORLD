# Client/Server Feature Map (Core/Content/Util)

Source of truth (JSON): `config/minecraft_feature_client_server_core_content_util_2026-02-19.json`

## Core (authority + infra)
- World map control parity — `GameServer/World/WorldMapControlManager.cs`, `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs`; share chunk/render/simulation distances, hydrology gradient stability, and profile hashes through a common generation signature.
- Terrain mask alignment — `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` + {River/Lake/Cave} generators mirrored in Unity previews.
- Proto registry validation — `SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `GameServer/Program.cs`, `Assets/MyAssets/Scripts/Network/GameNetworkManager.cs` guard descriptor/package/namespace drift before handlers run.

## Content (gameplay-facing)
- Hydrology-aware rivers — seam-stable flow memory + watershed blending (`ImprovedRiverGenerator`, Unity mirror).
- Seam-safe lakes — outflow carving + flow shadowing gated by gradient stability (`ImprovedLakeGenerator`, Unity mirror).
- Cave stability — riparian plugs, ceiling stability, and support pillars driven by hydrology/flow masks (`ImprovedCaveGenerator`, Unity mirror).

## Util (tooling/config/data)
- Config sync — JSON-first knobs (`config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`, `world-map-control.json`) for chunk sizing, hydrology weights, and profile versions.
- Generation signature export — shared signature for invalidating cached previews (`WorldMapControlManager`, `WorldMapController`).
- Data-driven world map — loaders keep render/simulation distance and water levels in sync (`WorldGenerationConfig`, `WorldConfig`).

## Implementation order (sequential)
1. Enforce proto registry/package validation and refresh generated DTOs if needed.
2. Apply shared generation signature (render/simulation distance, hydrology gradient stability, profile hash) to server/client map control.
3. Normalize hydrology/flow masks with gradient stability, then feed rivers/lakes/caves.
4. Verify config JSON parity (server ↔ Unity StreamingAssets) and bump profile version if knobs change.
5. Run builds: `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj`.
