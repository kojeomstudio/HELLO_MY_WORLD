# 2026-01-28 Client/Server Feature Split (Core/Content/Util)

- Hydrology signature: `2026-01-28-hydrology-shield-v5-aquifer`
- Map control profile: v8 (rebuild required for Unity + server parity)
- Source commits referenced: 97314dff, f418c0a6, b83e7370

## Client
- **Core**: World map control + preview (`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`, `WorldMapControlProfile.cs`, `Assets/StreamingAssets/world-map-control.json`); Network proto runtime (`Assets/Generated/Protobuf/*`, `NetworkManager.cs`); Shared DLL integration (`Assets/Plugins/GameCommon.dll`).
- **Content**: Hydrology v5 previews (rivers/lakes/caves) with aquifer damping (`WorldMapController.cs`, `Assets/Scripts/Minecraft/World/EnhancedTerrainGenerator.cs`); World map UI overlays (`Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs`, `WorldAreaManager.cs`).
- **Util**: Profile reload/hash guards for StreamingAssets (`WorldMapController.cs`).

## Server
- **Core**: World map control manager + profile generation (`GameServer/World/WorldMapControlManager.cs`, `WorldMapControlProfile.cs`); Shared protocol wiring (`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtocolValidator.cs`).
- **Content**: Hydrology masks (rivers/lakes feedback) (`ImprovedTerrainCoordinator.cs`, river/lake generators); Cave generator with aquifer shield (`ImprovedCaveGenerator.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`).
- **Util**: Dummy protocol client round-trips (`GameServer/Testing/DummyProtocolClient.cs`); Map profile generator CLI (`GameServer/Program.cs`, `config/world_map_control_profile.json`).

## Implementation Order
1) Server map control/profile (SV-CORE-01)  
2) Hydrology masks (SV-CONTENT-01)  
3) Aquifer-aware caves (SV-CONTENT-02)  
4) Client map control/preview (CL-CORE-01)  
5) Client hydrology previews (CL-CONTENT-01)  
6) Dummy protocol client (SV-UTIL-01)  
7) Map profile generation + StreamingAssets sync (SV-UTIL-02)

## Notes
- Keep data-driven configs in `config/world.json` and mirror to `Assets/StreamingAssets/world-config.json`.
- Rebuild GameCommon.dll after changing shared contracts or hydrology signature.
- Proto registry/fingerprint checks must pass before accepting new packet handlers or dummy client flows.
