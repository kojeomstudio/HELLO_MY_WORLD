# Core/Content/Utility rollout (2026-02-13)

Source of truth: `config/minecraft_feature_core_content_util_2026-02-13.json`. The JSON lists the same items with component paths for automation.

## Core (sequential order)
1. **World generation parity** — keep caves/rivers/lakes aligned across server (`WorldManager`, enhanced pipeline) and Unity previews (`ImprovedTerrainGenerator`, `WorldMapController`), driven by `config/world.json`.
2. **Map control profile sync** — hash + versioned `world_map_control_profile.json` shared with Unity controllers so chunk streaming and hydrology smoothing stay consistent.
3. **Protobuf protocol validation** — ensure EnhancedMinecraft DTOs are registered and fingerprints match via `ProtocolStandardization` + `EnhancedProtoManifest` on both runtimes.
4. **Chunk streaming & residency** — enforce render/simulation distance in `MinecraftChunkHandler`/`WorldManager` and Unity chunk managers.

## Content (sequential order)
1. **Block interaction & inventory** — synchronized block edits and inventory state (`WorldBlockHandler`, `InventoryHandler`, Unity modify/inventory managers).
2. **Crafting & furnace flows** — recipe/furnace validation on the server with matching Unity crafting/furnace UIs.
3. **Survival systems** — hunger/health ticks driven by server handlers with HUD updates in `HealthHungerSystem`.
4. **Entity sync & combat** — AI/combat packet handling (`AIHandlers`, `PlayerAttackHandler`) mirrored in `RemoteEntityManager` and world-time controllers.

## Utility (sequential order)
1. **Config/profile sync** — JSON-first configs (`world.json`, `world_map_control_profile.json`, `server-config.json`) validated in `ServerConfig`/`ConfigValidator` and mirrored by Unity `WorldConfigFile`.
2. **Protobuf asset pipeline** — regenerate `.proto` assets (`scripts/generate_proto.ps1`) and keep `SharedProtocol` + `Assets/Generated/Protobuf` in lockstep.
3. **Logging & monitoring** — lightweight logging/perf monitors on the server with client-side network diagnostics (`GameNetworkManager`, `MessageDispatcher`).

Notes:
- All items are data-driven (JSON + proto) and should be kept in sync when updating configs or schemas.
- New work should start at the top of each list before proceeding to later items to preserve sequencing.
