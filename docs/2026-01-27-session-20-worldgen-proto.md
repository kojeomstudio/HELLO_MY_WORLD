# 2026-01-27 Session 20 — Worldgen Hydrology v3 & Protocol Sync

## Overview
- Hydrology shield bumped to `2026-01-27-hydrology-shield-v3` with river anisotropy damping, bank stability clamp, lake outflow sealing, and cave moisture flow clamp applied across server + Unity preview.
- Map-control profile version raised to **6**; profiles regenerated and signatures now come from the shared GameCommon DLL to prevent drift between client/server.
- Dummy protocol client rebuilt to validate EnhancedMinecraft fingerprints/registry bindings and to emit time-update + chunk-load frames for manual packet testing.
- Data remains JSON-driven; new fields added to `config/world.json` and mirrored into `Assets/StreamingAssets/world-config.json` and `world-map-control.json`.

## World Generation Changes
- **Rivers:** Added `RiverAnisotropyDamping` and `RiverBankStabilityClamp` to soften meanders on steep relief; preview generator mirrors the same damping/clamp before carving.
- **Lakes:** Added `LakeOutflowSealWeight` to bias outflow stitches toward existing river masks and dampen wetland overrun.
- **Caves:** Added `MoistureFlowClamp` to cap hydrology influence on subterranean flow memory and reduce runaway saturation near rivers.
- **Hydrology signature:** `SharedFeatureCatalog.HydrologySignature` -> `2026-01-27-hydrology-shield-v3`; map-control profile version -> 6.
- Updated configs: `config/world.json`, `Assets/StreamingAssets/world-config.json`, regenerated `config/world_map_control_profile.json` and mirrored to `Assets/StreamingAssets/world-map-control.json`.

## Shared DLL & Signatures
- Introduced `GameCommon/World/WorldMapSignature.cs` + `WorldMapContracts.cs`; both server (`WorldMapControlManager`) and Unity (`WorldMapController`) compute generation signatures from the shared context.
- Built `GameCommon.dll` and copied to `Assets/Plugins/GameCommon.dll` for Unity use.
- Map-control profile hash/signature now include proto fingerprints (`ProtoFingerprint.DescriptorFingerprint` + `ProtoFingerprint.ComputeFingerprint()`), hydrology signature, and new config fields.

## Protocol & Dummy Client
- `GameServer/Testing/DummyProtocolClient.cs` now validates registry bindings/fingerprints before building frames.
- Helpers: `BuildTimeUpdateRoundTrip`, `BuildChunkLoadRequestRoundTrip`, `SendAsync`, `SendChunkRequestAsync`.
- Usage example:
  - Start server: `dotnet run --project GameServer/GameServer.csproj -- --server`
  - Send frames: call `DummyProtocolClientMain.RunAsync(new[]{"--host","127.0.0.1","--port","9000"})` from a harness, or reference `DummyProtocolClient` in tests to fetch framed payloads.
- Proto validation warnings remain for optional/legacy packets; re-run protoc if those DTOs are promoted.

## Build & Data-Driven Notes
- Builds: `dotnet build GameServer/GameServer.csproj`.
- Map profile regeneration: `dotnet run --project GameServer/GameServer.csproj -- --generate-map-profile` (copies to `config/world_map_control_profile.json`; manually mirror to `Assets/StreamingAssets/world-map-control.json`).
- Config remains JSON-first; keep new fields (`RiverAnisotropyDamping`, `RiverBankStabilityClamp`, `LakeOutflowSealWeight`, `MoistureFlowClamp`) in both server + client JSON.

## Follow-Ups
- When protoc is rerun, bind optional packets noted in registry warnings or document intentional omissions.
- After Unity rebuild, capture a short preview to confirm map-control parity with the new GameCommon.dll.
