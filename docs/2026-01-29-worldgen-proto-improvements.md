# 2026-01-29 Worldgen + Proto Updates

## Terrain (caves / rivers / lakes)
- Added riparian stabilization pass to cave generation (`WorldGenAlgorithms.StabilizeRiparianTerrain`) to seal wet chunk seams and thicken river/lake banks using existing erosion knobs.
- Underground lake spawning now clamps water surfaces to the global water level to reduce flooded ceilings.
- World map control signature bumped to `2026-01-29-hydrology-shield-v6-riparian` (shared via `GameCommon.World.SharedFeatureCatalog`).

## Protocol + Shared Contracts
- Proto registry now emits a JSON audit report (`config/proto_reference_report.json`) via `ProtoDiagnostics.WriteReportToFile`.
- Added data-driven feature manifest loader (`GameCommon/DataDriven/FeatureManifest.cs`) used on server boot to validate the shared Core/Content/Utility plan (`config/minecraft_feature_core_content_util_2026-01-29.json`).
- Introduced dummy protocol client (`GameServer/Testing/DummyProtocolClient.cs`) with config (`config/protocol_dummy_client.json`) for offline encode/decode probes and optional TCP checks.

## Usage
- Regenerate/refresh map profile after hydrology changes: `dotnet run --project GameServer -- --generate-map-profile`.
- Self-test with proto probe: `dotnet run --project GameServer -- --selftest --proto-probe`.
- Feature manifest source: `config/minecraft_feature_core_content_util_2026-01-29.json` (summary: `docs/minecraft_feature_core_content_util_2026-01-29.md`).
