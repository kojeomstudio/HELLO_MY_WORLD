# 2026-01-17 Worldgen & Protocol Update

## World Generation (caves/rivers/lakes)
- Added hydrology edge normalization to server generation to smooth chunk seams and align river/lake/cave moisture fields (`MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs`).
- Lake candidate scoring now factors seam continuity and variance assistance to avoid spawning on unstable gradients; mirrors Unity preview tuning.
- Unity preview lake weights now honor edge-normalization and flow-consistency penalties (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`), keeping client/server basins in sync.
- Map control profile now forwards edge-normalization parameters to the server pipeline (`WorldAreaManager.cs`), using JSON-driven values from `world-map-control.json` / `world-config.json`.

## Protocol
- Registry validation now checks that every registered `MinecraftMessageType` exists in the generated descriptor set and fails fast when protoc output is stale (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`).

## Data Assets
- Updated feature categorization (Core/Content/Util, client+server) lives in `docs/minecraft_feature_core_content_util_2026-01-17.md` with JSON source `config/minecraft_feature_client_server_core_content_util_2026-01-17.json`.

## Test Results
- `dotnet build SharedProtocol/SharedProtocol.csproj` (warnings: NU1603, nullable advisories).
- `dotnet build GameServer/GameServer.csproj` (warnings: NU1603, nullable/async advisories).
- `dotnet run --project GameServer/GameServer.csproj -- --selftest` (completes; proto registry reports unmapped optional descriptors/handlers as warnings; stub client responses remain as expected).

## Next Steps / Verification
- Build: `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj`.
- Runtime smoke: `dotnet run --project GameServer -- --selftest`.
- Unity: reopen with regenerated map-control profile if `world-map-control.json` changes; verify lake/cave seams in preview chunks.
