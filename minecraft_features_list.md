# Minecraft Features Implementation List

This list is deduplicated and mapped to Core / Content / Utility. Each item notes which side is responsible so work can be sequenced without drift. See `docs/minecraft_feature_core_content_util_2026-01-21.md` for the detailed breakdown and order of operations.

## Core
- **World generation** (Server: chunk/world seed, hydrology-aware rivers/lakes/caves, ore/vegetation; Client: MapGeneratorLib previews, render/simulation distance, mesh/LOD rebuild cadence).
- **Networking + protocols** (Server: EnhancedMinecraftProtocol registry + handler coverage, chunk stream/unload guards; Client: generated protobuf DTO bindings, chunk/entity sync pipelines).
- **Simulation systems** (Server: time/weather, rooms + block broadcasts, entity spawn/update/despawn, persistence; Client: player controller, remote entity manager, HUD sync for time/weather/health).

## Content
- **Blocks/items/entities** (Server: registry/state persistence, crafting/smelting validation, loot + mob spawn rules; Client: JSON-driven definitions, inventory/crafting UI, prefabs/materials/animations).
- **World features** (Server: biomes, rivers/lakes/caves/dungeons/vegetation/structures; Client: same toggles for previews, biome-driven visuals, particles/audio for water/caves).

## Utility
- **Configuration/data** (Server: JSON configs + env projection, proto fingerprints/validators; Client: mirrored JSON for worldgen/render/input/audio/graphics, safe fallbacks).
- **Tooling/diagnostics** (Server: protobuf verification, worldgen metrics, handler coverage logs; Client: chunk loader debug overlay, perf capture/LOD/culling, protocol mismatch warnings).

## Sequencing
1. Keep JSON/world-map-control parity and regenerate/verify protobuf contracts.
2. Apply worldgen refinements (edge flux, river seam feathering, cave edge sealing) on server and client preview paths.
3. Wire chunk streaming to the tuned map-control profile; integrate gameplay/content systems next.
4. Run builds/tests and capture coverage gaps for the next sprint.
