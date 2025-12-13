# Minecraft Feature Catalog (Core / Content / Utility)

Use this list to keep Minecraft-style features aligned between the authoritative server and the Unity client. Items are grouped into Core (stability/compatibility), Content (gameplay loops), and Utility (operability/tooling). File references show the primary implementation points.

## Server
- Core: ✅ Hydrology gradient stability + seam smoothing for rivers/lakes/caves (`GameServer/World/WorldManager.cs`, `MapGeneratorLib/.../WorldGenAlgorithms.cs`); ✅ Enhanced protobuf registry validation + startup summary (`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`, `ProtoDiagnostics.LogSummary()` in `GameServer/Program.cs`); ☐ Chunk lifecycle prioritisation + unload acks; ☐ Session/auth hardening with reconnect-safe state; ☐ Protocol coverage tests for every handler.
- Content: ✅ Health/hunger loop; ☐ Inventory/equipment/crafting driven by `config/items.json` / `config/recipes.json`; ☐ Combat tuning + reconciliation; ☐ Weather/day-night/environment feedback tied to time/weather broadcasts; ☐ Entity AI/spawn/sync loops.
- Utility: ✅ Data-driven worldgen (`config/world.json`) mirrored into Unity; ✅ World map control profile for render/simulation distances and hydrology tuning (`GameServer/World/WorldMapControlProfile.cs`); ☐ Config hot-reload + schema validation; ☐ Metrics/telemetry and backup/export tooling.

## Client
- Core: ✅ World map control profile sourced from `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (render/sim distance + hydrology stability); ☐ Chunk request/unload resilience and residency tracking; ☐ Movement prediction/interpolation with reconciliation; ☐ Protobuf gate/fingerprint check before entering play.
- Content: ☐ Health/hunger/experience UI bound to server ticks; ☐ Inventory/equipment/crafting UI fed by JSON data; ☐ Combat feedback (hit-stop/VFX/SFX); ☐ Weather + day/night visuals matched to time/weather updates; ☐ Entity rendering/interactions via spawn/update/despawn protobufs.
- Utility: ✅ Config mirroring from `config/` into Unity resources; ☐ Diagnostics overlays (chunk timings, hydrology/cave/ribbon visualisers); ☐ Logging/trace export for repros with capture toggles.

## Data & Config Sources
- `config/world.json` (server) ↔ `Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json` (client) hold hydrology gradient stability, render/simulation distance, chunk size, and water level used by map control.
- Protobuf contracts: `proto/*.proto` → regenerate `SharedProtocol` + `Assets/Generated/Protobuf` together; keep `ProtocolRegistry` bindings updated.
- Environment/gameplay configs: `config/server.json`, `config/gameplay.json`, `config/items.json`, `config/recipes.json`, `config/item_categories.json` power both server systems and Unity UI.
