# Core/Content/Util roll-up (2026-01-05)

- **Core** – Hydrology/flow harmonization tied to surface slope/curvature so river/lake/cave masks stay continuous across chunk seams (`GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`, Unity `WorldMapController` preview pipeline). Map-control responses now ship a generation signature to keep StreamingAssets previews aligned with the server (`WorldMapControlManager`, `WorldMapController`).
- **Content** – River/lake carving consumes the stabilized hydrology field so channel width, wetlands, and shoreline shelves respect the harmonized flow masks (shared masks reused by server + Unity previews).
- **Util** – Enhanced protobuf guard rails validate player-facing descriptors (PlayerInfo/Inventory/ItemStack) to catch stale generated DTOs or `using` drift, and README/doc updates describe the rollout.

Execution order:
1) Core hydrology + map-control signature sync  
2) Content alignment for rivers/lakes/caves that depend on the harmonized masks  
3) Validation/docs (protobuf descriptor guard + plan refresh)
