# Terrain, World Map, and Proto Updates (2026-01-16)

## What changed
- Hardened EnhancedMinecraft proto validation: `ProtoDiagnostics.AssertRegistryClean()` now fails on fingerprint drift; bootstrap calls added in `GameServer/Program.cs`.
- World-map generation signature now carries proto fingerprints and a bumped pipeline version to invalidate stale previews (`GameServer/World/WorldMapControlManager.cs`).
- Hydrology envelope + edge stabilization tightened for rivers/lakes/caves on server (`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`) and Unity previews (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`). Cave carving now factors moisture envelope/ventilation to avoid watery ceilings and seam leaks.
- Updated feature categorization by Core/Content/Utility for this session (`docs/minecraft_features_core_content_util_2026-01-16.md` + JSON companion).

## Config/data touch points (JSON-driven)
- Worldgen knobs: `config/world.json`, `Assets/StreamingAssets/world-config.json`, `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (Hydrology*, Water.River*/Lake*, Caves.*).
- Map control profile/signature: `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-map-control.json`.
- Proto sources: `proto/*.proto` -> generated C# in `SharedProtocol/EnhancedMinecraft` and `Assets/Generated/Protobuf` (regenerate if fingerprints drift).

## Testing guidance
- `dotnet build SharedProtocol/SharedProtocol.csproj`
- `dotnet build GameServer/GameServer.csproj`
- Optional smoke: `dotnet run --project GameServer -- --selftest`
- Unity previews rely on updated `MapGeneratorLib` hydrology envelope; ensure StreamingAssets configs are synced before play mode.
