# Minecraft Feature Backlog (Core / Content / Utility)

This backlog groups every required Minecraft capability for both server and client. Items are classified as **Core** (blocking stability/compatibility), **Content** (gameplay loops), or **Utility** (operability, tooling, data/diagnostics). Keep server and client in lock-step and mirror JSON config between `config/` and Unity `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json`.

## Server
### Core
- [ ] World generation parity: terrain + caves + rivers/lakes stay identical between `GameServer/World/WorldManager.cs` and `MapGeneratorLib` (noise fields, hydrology seams, gradients).
- [ ] Chunk lifecycle: multi-threaded generation, caching/compression, priority streaming, chunk unload acknowledgements.
- [ ] Session/auth pipeline with rate limiting, anti-cheat hooks, and reconnect-safe state.
- [ ] Protocol validation: protobuf fingerprint/descriptor/registry coverage, handler coverage, and framed packet guard rails.
- [ ] Persistence/backups: player/world state snapshots, recovery hooks, and data-driven world seeds.

### Content
- [x] Health & hunger (server-authoritative).
- [ ] Inventory/equipment + crafting recipes driven by `config/items.json`, `config/recipes.json`, and `config/item_categories.json`.
- [ ] Combat tuning (weapons/armor/PvP rules) with tick reconciliation.
- [ ] Environment loops: day/night, weather, temperature/seasonal modifiers, surface hydrology feedback.
- [ ] Entities & AI: spawning, behaviours, persistence, sync.

### Utility
- [x] JSON config loading (`server-config.json`, `config/world.json`, and other data files in `config/`).
- [ ] Hot-reloadable configs + schema validation.
- [ ] Metrics/telemetry (TPS, chunk counts, protocol health) and operational tooling (backups, protocol diagnostics export).
- [ ] Data-driven world/ore tuning with documented defaults.

## Client
### Core
- [ ] Chunk request/unload pipeline with graceful fallback on packet loss and residency tracking.
- [ ] Prediction/interpolation for movement with reconciliation on server corrections.
- [ ] Protobuf gate before entering play mode (fingerprint/registry guard) and handler coverage checks.
- [ ] World map controls + hydrology/cave tuning preview sourced from `WorldConfigData.json`.

### Content
- [ ] UI for health/hunger/experience synced to server ticks.
- [ ] Inventory/equipment/crafting UI backed by JSON data (`config/items.json`, `config/recipes.json`).
- [ ] Combat feedback (damage numbers, hit-stop, VFX/SFX hooks).
- [ ] Weather + day/night visuals aligned to `TimeUpdate` / `WeatherChange`.
- [ ] Entity rendering/interactions tied to `EntitySpawn/Update/Despawn` protobuf updates.

### Utility
- [ ] Config mirroring of server JSON into Unity resources for offline previews.
- [ ] Diagnostics overlays: chunk network timings, protobuf handler coverage, hydrology/cave/ribbon visualizers.
- [ ] Logging/trace export for client-server repros (chunk payload hashes, handler timings), plus capture toggles.

## Cross-Cutting Data
- Config and data remain JSON-first (worldgen, recipes, items, category tags, runtime settings).
- proto/*.proto is the single source of truth; regenerate `Assets/Generated/Protobuf` after edits and build `SharedProtocol` to sync descriptors.
- Keep `server-config.json` as the entry for environment variables and split configs only when maintainability improves clarity.

## Execution Order (recommended)
1) Stabilize **Core** worldgen + protocol guards (hydrology gradients/seams, protobuf coverage).  
2) Ship movement/chunk/Core networking, then health/hunger UI sync.  
3) Layer Content loops (inventory/crafting/combat/weather) backed by JSON data.  
4) Add Utility telemetry, config hot-reload, and diagnostics overlays.  
5) Continuously mirror server/client configs and regenerate protobuf assets when `.proto` changes.
