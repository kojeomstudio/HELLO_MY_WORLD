# Minecraft Feature Plan (Core/Content/Util) — 2026-02-10

## Core (Server & Client)
- Protocol validation stays enforced (`ProtocolValidator.ValidateEnhancedContracts/ValidateHandlerBindings`, `ProtoRuntime.EnsureInitialized`); Unity `GameNetworkManager` and the dedicated server both fail fast on stale generated protobuf DTOs.
- World map control profile remains authoritative: `config/world_map_control_profile.json` is mirrored to `Assets/StreamingAssets/world-map-control.json` with the same hash, render/simulation distance, and hydrology knobs so chunk previews match server output.
- Data-driven world inputs stay in JSON (`config/world.json`, `server-config.json`, `world-config.json` in StreamingAssets); map-control overrides still cascade through `WorldConfigFile.OverrideWithProfile`.

## Content (Gameplay Systems)
- Rivers: riparian buffers now dilate across chunk edges and a seam-fill pass (`Water.RiverSeamFillStrength`) patches partial channels before bank shaping.
- Lakes: shoreline wetlands grow with a configurable buffer radius (`Lakes.WetlandBufferRadius`) so outflows blend into terrain and rivers.
- Caves: hydrology-weighted ceiling reinforcement (`Caves.CeilingStabilityWeight`) plus riparian sealing keeps shallow caves stable near water tables and rivers.
- Hydrology: riparian saturation uses `Water.RiparianBufferRadius` to soften chunk borders and feed the improved river/lake/cave passes.

## Util (Tooling, Data, Operations)
- Config knobs added: `Water.RiparianBufferRadius`, `Water.RiverSeamFillStrength`, `Lakes.WetlandBufferRadius`, `Caves.CeilingStabilityWeight` in both server and client loaders.
- Map-control profile hash regenerated to include the new fields; StreamingAssets copy stays in sync for Unity editor/testing flows.
- Proto/tooling commands remain: `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto`, `dotnet build SharedProtocol/SharedProtocol.csproj`, `dotnet build GameServer/GameServer.csproj`.

## Execution Order
1) Core validation and map-control parity (hash + streaming assets) ✅
2) Hydrology content passes (riparian buffer, river seam fill, lake wetlands, cave ceilings) ✅
3) Config/documentation sync + protobuf verification commands noted for next runs ✅
