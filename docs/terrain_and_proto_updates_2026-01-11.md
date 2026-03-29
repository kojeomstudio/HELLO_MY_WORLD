# Terrain & Protocol Updates (2026-01-11)

## Terrain / World-Map Control
- Added shared hydrology/flow edge normalization on server (`ImprovedTerrainCoordinator.NormalizeHydrologyFlowEdges`) and Unity preview (`WorldMapController.EnhancedTerrainGenerator.NormalizeHydrologyFlowEdges`) to reduce seam artifacts for caves, rivers, and lakes.
- Cave edge sealing is now hydrology/river-aware on both server and client generators, preventing open seams without overcarving dry edges.
- World-map generation signatures include `HydrologyWatershedStitchRadius`, forcing preview/profile reloads when seam radii change.
- Client preview generator mirrors watershed-aware blends; height previews now clear cached chunks when signature drifts.

## Protobuf Protocol
- `ProtocolRegistry.ValidateBindings` now checks generated package names against `EnhancedMinecraftProtocol` to catch stale DTO references after protoc regeneration.
- Registry/validator still enforce descriptor presence and factory creation for all EnhancedMinecraft message types.

## Data & Config Notes
- Feature inventory refreshed: `docs/minecraft_features_core_content_util_2026-01-11.md` and `config/minecraft_feature_client_server_core_content_util_2026-01-11.json`.
- Hydrology/flow tuning remains data-driven via `config/world.json` and `config/world_map_control_profile.json` (mirrored in `Assets/StreamingAssets/` for Unity).

## Build/Verification
- Run compilation checks:
  - `dotnet build SharedProtocol/SharedProtocol.csproj`
  - `dotnet build GameServer/GameServer.csproj`
- Regenerate protobuf DTOs when proto files change: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` (before reopening Unity).
