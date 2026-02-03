# 2026-02-03 Worldgen & Proto Update

**Hydrology Signature:** `2026-02-03-hydrology-riverlake-v11`  
**Profile Version:** 13 (`config/world.json`, `config/world_map_control_profile.json`, `Assets/StreamingAssets/world-config.json`)  
**Feature Manifest:** `config/minecraft_feature_core_content_util_2026-02-03.json`

## Worldgen Changes (Server + MapGeneratorLib)
- Hydrology stability: added in-place stabilization for hydrology/flow masks before carving (`EnhancedTerrainGenerationPipeline.StabilizeHydrologyFields`), using variance clamps, flow memory, and water-table bias.
- Rivers: directional stability now damped by hydrology gradients; tuned anisotropy/meander/edge weights for smoother chunk seams.
- Lakes: variance/edge damping and reservoir memory applied; inflow/outflow thresholds now consider seepage + river proximity with updated smoothing weights.
- Caves: moisture clamps and support bias reduce over-carving; riparian plugs respect `RiparianPlugDepth`; edge sealing scales with moisture; flooded caves respect updated thresholds.
- MapGeneratorLib alignment: constants raised to hydrology v11 values and new `StabilizeCaveHydrology` reduces variance spikes across chunk boundaries.

## Config Updates
- Water: higher smoothing/reservoir counts, stronger edge/variance clamps, warp amplitude, directional damping, deeper rivers, and widened edge feathering.
- Lakes: increased basin smoothing, shelf depth, radius, shoreline blend, river proximity suppression, and outflow stability/seal weights.
- Caves: higher stability smoothing/support density, moisture retention clamp, stronger riparian guarding, plug depth, ceiling moisture clamp, and edge sealing.
- Streaming parity: `config/world.json` copied to `Assets/StreamingAssets/world-config.json`; map-control profile will be regenerated/mirrored.

## Proto & Dummy Client
- Packet matrix expanded (MultiBlockChange, ItemUse/Drop/Pickup, EntityInteract, ContainerClose) with higher round-trip count; config at `config/protocol_dummy_client.json`.
- Dummy client keeps registry/fingerprint assertions and writes probe + reference reports (`reports/proto_probe_report.json`, `config/proto_reference_report.json`).

## Shared DLL / Data-Driven Notes
- SharedFeatureCatalog hydrology signature bumped; rebuild `GameCommon.dll` and `MapGeneratorLib.dll` and copy to `Assets/Plugins/` to keep Unity aligned.
- Data remains JSON-driven across server/client (world, map-control, protocol dummy client, feature manifest).

## Regeneration & Validation Steps
1. Regenerate map-control profile + mirror to StreamingAssets:  
   `dotnet run --project GameServer -- --generate-map-profile`
2. Build shared/server assemblies:  
   `dotnet build SharedProtocol/SharedProtocol.csproj`  
   `dotnet build GameCommon/GameCommon.csproj -c Release` (copy dll to `Assets/Plugins/`)  
   `dotnet build MapGeneratorLib/MapGeneratorLib/MapGeneratorLib.csproj -c Release` (copy dll to `Assets/Plugins/`)  
   `dotnet build GameServer/GameServer.csproj`
3. Proto probe (optional network):  
   `dotnet run --project GameServer -- --proto-probe`

## Proto Probe Notes
- Dummy client validated required packets but optional packets still lack generated descriptors/bindings (MultiBlockChange, InventoryUpdate, ItemUse/Drop/Pickup, EntityUpdate/Interact, ContainerOpen/Close/Update); regenerate protoc outputs and extend ProtocolRegistry when promoting them to required.
- Network probe attempted against localhost:9000 and timed out (server not running); serialization/deserialization loop succeeded offline.
