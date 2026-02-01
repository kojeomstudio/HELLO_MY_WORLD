# 2026-02-01 WorldGen + Proto Update (S33)

## Overview
- Hydrology signature bumped to `2026-02-01-hydrology-riverlake-v8` with map-control profile v10.
- River/lake edge smoothing and cave riparian buffers tightened for chunk-seam stability.
- Proto registry/dummy client now emit JSON reports and surface missing bindings.

## WorldGen Changes
- Rivers: added hydrology-aware edge feathering, tighter bank erosion clamp, refreshed river thresholds (`center=0.0118`, `bank=0.0245`), `HydrologyReservoirIterations` -> 3, `HydrologyEdgeStabilityWeight` -> 0.44.
- Lakes: shoreline smoothing increased (`LakeBasinSmoothIterations` 5, `ShorelineBlend` 0.7), inflow/outflow stability raised, buffer radius 4, seepage weight 0.54, rim erosion 0.42.
- Caves: riparian guard stronger (`RiparianCaveGuardWeight` 0.46, edge seal 0.6, pillar chance 0.3), extra stability smoothing and moisture clamps for wet seams.
- MapGeneratorLib defaults aligned with server config for Unity previews.

## Map-Control Architecture
- `--generate-map-profile` now regenerates the profile (v10) using `SharedFeatureCatalog.HydrologySignature`, mirrors it to `Assets/StreamingAssets/world-map-control.json`, and logs hash/signature.
- Unity `WorldMapControlProfile.LoadFromFile` validates hydrology signature and version; mismatches fall back to world config.
- Server `EnsureWorldMapProfile` enforces profile version/signature and reuses shared DLL (GameCommon).

## Proto & Dummy Client
- `ProtocolRegistry` exposes missing required/optional bindings; `DummyProtocolClient` can validate all registered packets, optionally probe the network, and write a JSON report (`config/protocol_dummy_client.json` controls flags/output path).
- `GameServer Program --selftest/--proto-probe` prints validated packets and missing bindings; proto reference report still emitted to `config/proto_reference_report.json`.

## Data-Driven Assets
- Updated configs: `config/world.json`, `Assets/StreamingAssets/world-config.json`, `config/world_map_control_profile.json`, `config/minecraft_feature_core_content_util_2026-02-01.json`, `config/protocol_dummy_client.json`.
- Feature catalog updated via `GameCommon.World.SharedFeatureCatalog` and manifest `config/minecraft_feature_core_content_util_2026-02-01.json`.

## Validation
- Build shared/server: `dotnet build SharedProtocol/SharedProtocol.csproj` && `dotnet build GameServer/GameServer.csproj`.
- Regenerate profile: `dotnet run --project GameServer -- --generate-map-profile`.
- Optional proto probe: `dotnet run --project GameServer -- --proto-probe`.
