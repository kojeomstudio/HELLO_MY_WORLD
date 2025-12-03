# Minecraft Feature Plan (Core / Content / Util)
Project-wide breakdown of Minecraft-style features, grouped by category and scoped to client vs. server. Use this as the sequence for upcoming work; mark items as we implement.

## Client (Unity)
- Core: chunk streaming & LOD (`Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`, `MapGeneratorLib`); network/session bootstrap (`Assets/MyAssets/Scripts/Network`); protobuf packet handling (`Assets/Generated/Protobuf`).
- Content: terrain visuals (caves/rivers/lakes) driven by `WorldConfigData.json`; biome decoration/vegetation (`WorldArea`, `Chunk` scripts); UI for inventory/chat/time/weather.
- Util: data-driven configs (`Assets/MyAssets/Resources/TextAsset/GameWorld/*.json`), debug overlays/logging, editor tooling (`CustomToolSet`), automated hydration of map data assets.

## Server (.NET)
- Core: world auth and chunk lifecycle (`GameServer/World/WorldManager.cs`, `Handlers/`, `SessionManager.cs`); protobuf registry/validation (`SharedProtocol/EnhancedMinecraft`); persistence and session safety (`GameServerApp`).
- Content: world generation (caves/rivers/lakes/ore/vegetation) using `config/world.json`; time/weather cycles; entity spawn/despawn handling; block change and chunk streaming.
- Util: config surfaces (`server-config.json`, `config/world.json`), telemetry/metrics hooks, maintenance jobs (backups, chunk save intervals), diagnostics (`docs/`, build logs).

## Implementation Order
1) Core world-map control: keep client/server hydrology, river, lake, and cave generation in lockstep via config-driven knobs. (In progress)
2) Proto transport hardening: validate enum/registry coverage and keep generated assemblies in sync. (In progress)
3) Content polish: wetlands, terraces, biome decorations tuned per data files; ensure data-driven JSON stays authoritative.
4) Utility/ops: metrics, backups, and editor tooling to speed map authoring; keep configs and docs aligned with code.
