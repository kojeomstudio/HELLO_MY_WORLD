# 2026-01-21 Worldgen + Proto Update

## Summary
- Added erosion-aware damping to hydrology/flow masks before carving rivers, lakes, and caves on both server (`ImprovedTerrainCoordinator`) and client preview (`WorldMapController`), reducing over-saturated seams on steep terrain.
- River and lake masks now factor erosion risk and riparian stability more aggressively (width modulation, rim erosion brakes, headwater smoothing).
- Cave stability accounts for local erosion risk to avoid fragile columns near steep slopes while keeping riparian sealing.
- Map-control pipeline version bumped to `2026-01-21-erosion-damping+proto-guard` to track parity between server and Unity preview.
- Protobuf handler registration now enforces Google.Protobuf DTOs for all EnhancedMinecraft registry entries and logs when optional messages fall back to protobuf-net.

## Files Touched
- Server worldgen: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`
- Client preview: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`
- Proto guard: `SharedProtocol/MinecraftMessageDispatcher.cs`
- Docs/plan: `docs/2026-01-21-feature-categorization.md`, `plans/2026-01-21-comprehensive-implementation-work-plan.md`, `README.md`

## Config & Data
- No new JSON keys; existing hydrology/erosion weights drive the new damping logic. Regenerate `world_map_control_profile.json` if config weights change.
- Feature catalog reference: `config/minecraft_feature_client_server_core_content_util_2026-01-21.json`.

## Testing
- `dotnet build SharedProtocol/SharedProtocol.csproj` (warnings: NU1603 protobuf-net version resolution; existing nullable warnings CS8618/CS8600/CS8604; async without await CS1998).
- `dotnet build GameServer/GameServer.csproj` (warnings: NU1603 from SharedProtocol plus existing nullable/async warnings across handlers/worldgen).
- Unity client build not run in this session.
