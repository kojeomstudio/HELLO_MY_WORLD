# 2026-01-28 Worldgen & Proto Update

- Hydrology signature bumped to `2026-01-28-hydrology-shield-v5-aquifer`; map control profile version 8 regenerated (`config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`).
- Config tuning: higher flow shadow/lock weights, tighter edge variance clamp, stronger lake seepage/outflow stability, and cave moisture/edge seal increases (`config/world.json`, `Assets/StreamingAssets/world-config.json`).
- Server/client terrain: aquifer suppression and lake seepage smoothing added to world-map previews and improved terrain coordinator; cave masks now penalize saturated columns (GameServer/World/Generation/ImprovedTerrainCoordinator.cs, Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs).
- MapGeneratorLib retargeted to netstandard2.1 with missing enum values and helpers restored; rebuilt plugin (`Assets/Plugins/MapGeneratorLib.dll`).
- Shared DLL: hydrology signature v5 propagated via GameCommon.dll (`Assets/Plugins/GameCommon.dll`).
- Dummy protocol client now exercises `PlayerInfo` round-trip (nested inventory/stats) alongside time/chunk/block frames for registry validation (GameServer/Testing/DummyProtocolClient.cs).
- Proto audit (`dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile`) reports missing optional bindings (MultiBlockChange, InventoryUpdate, ItemUse, ItemDrop, ItemPickup, EntityUpdate, EntityInteract, ContainerOpen/Close/Update) — regenerate proto/registry if these packets become required.

## Build/Validation
- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameCommon/GameCommon.csproj`
- `dotnet build MapGeneratorLib/MapGeneratorLib/MapGeneratorLib.csproj`
- `dotnet build GameServer/GameServer.csproj` (warnings only)
- `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` (proto warnings about optional/unused descriptors)

## Next Steps
- Wire optional EnhancedMinecraft packets into `MinecraftMessageType`/`ProtocolRegistry` if promoted to required.
- Capture Unity preview with updated `world-map-control.json` and refreshed plugins to confirm river/lake/cave parity.
