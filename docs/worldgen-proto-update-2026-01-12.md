# Worldgen & Proto Update (2026-01-12)

## Terrain generation
- Rivers: layered simplex noise (macro/detail) with flow-memory continuity; added seam stitching for chunk edges (`GameServer/World/Generation/ImprovedRiverGenerator.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`).
- Lakes: basin weighting now uses macro/detail noise and flow-memory drift damping to smooth shorelines and inflow continuity (`ImprovedLakeGenerator.cs`, `WorldGenAlgorithms.cs`).
- Caves: multi-layer noise blend plus flow-memory clamp to damp saturated ceilings while carving deeper interiors (`ImprovedCaveGenerator.cs`).
- Hydrology previews: extra smoothing pass in Unity hydrology fields to mirror server seam relax (`WorldGenAlgorithms.cs`).

## Map control
- Server `WorldMapControlManager` now fingerprints world/map-control JSON files (SHA-256) to invalidate cached previews when content changes even without timestamp bumps.
- Unity `WorldMapController` mirrors hash checks to rebuild preview generators when map-control JSON changes on disk.

## Protocol
- EnhancedMinecraft registry/validator remain enforced during server/client bootstrap; use `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` if fingerprints drift.

## Data & docs
- Feature inventory refreshed: `config/minecraft_feature_inventory_2026-01-12-session.json`, `docs/minecraft-feature-inventory-2026-01-12.md`.
- Session plan: `plans/2026-01-12-worldgen-proto-session.md`.
