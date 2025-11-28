# Minecraft Feature Inventory

## Core
- Server: chunk auth/session pipeline (`GameServer/SessionManager.cs`), deterministic worldgen pipeline with hydrology cache and caves/rivers/lakes stages (`GameServer/World/WorldManager.cs`), data-driven knobs via `config/world.json` and `server-config.json`, protobuf registry/validator (`SharedProtocol/EnhancedMinecraft`).
- Client: world area + chunk streaming (`Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `SubWorld.cs`), block editing managers (`ModifyWorldManager.cs`, `EnhancedModifyWorldManager.cs`), data-driven world sizing and water/cave toggles (`Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`).
- Shared: protocol DTOs generated from `proto/*.proto` into `SharedProtocol` and `Assets/Generated/Protobuf`, runtime registry (`SharedProtocol/MinecraftMessages.cs`) and fingerprint guard (`ProtoFingerprint.cs`).

## Content
- Terrain/biomes: base terrain, caves, rivers, lakes, clouds, vegetation, ores (`GameServer/World/WorldManager.cs` stages; `MapGeneratorLib/.../WorldGenAlgorithms.cs` for client-side preview).
- Structures: dungeons and tree placement hooks (`GameServer/World/WorldManager.cs`, `Assets/MyAssets/Scripts/GameWorld/Enviroment`).
- Entities/gameplay: player movement/chat/auth (proto/game_*), entity spawn/despawn + world time/weather broadcasts (`SharedProtocol/GameProtocol.cs`, `EnhancedMinecraftGame.cs`).

## Utility
- Tooling: map generator library (`MapGeneratorLib`), custom toolset scripts (`CustomToolSet`), build scripts/logs in repo root.
- Diagnostics: protocol validator/diagnostics (`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoDiagnostics.cs`), server logs in `build_*.log`.
- Configuration data: block/gameplay/server/world JSON under `config/`, Unity text assets under `Assets/MyAssets/Resources/TextAsset/`.

## Sequencing Notes
- Short term: keep proto outputs in sync (run `protoc` against `proto/*.proto`, refresh `SharedProtocol` and `Assets/Generated/Protobuf`; run `ProtocolValidator.ValidateEnhancedContracts` on boot).
- Worldgen focus: tune `config/world.json` and matching Unity config to drive caves/rivers/lakes, using the shared hydrology cache to keep stages consistent.
- Gameplay focus: wire auth/move/chat/entity handlers to the enhanced proto messages and ensure chunk streaming respects updated worldgen parameters.
