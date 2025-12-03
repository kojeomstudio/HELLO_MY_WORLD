# Minecraft Feature Catalog (Core / Content / Util)

## Core Features
- Server: connection/auth handshake, session lifecycle, and protocol routing via `SharedProtocol` + `GameServer/Handlers` with protobuf messages.
- Server: chunk streaming and world generation orchestration (`MapGeneratorLib` worldgen, `SessionManager` dispatch, time/weather ticks).
- Client: network bootstrap and reconnect, chunk request/ack pipeline (`Assets/MyAssets/Scripts/Network/*`), chunk mesh rebuild safety.
- Client: render/collision core (chunk meshing, block update propagation), input-to-action plumbing for block break/place.
- Shared: deterministic config + seed handling (`server-config.json`, world seed plumbed to client for preview/minimap).

## Content Features
- Blocks/biomes: data-driven block catalog (durability, drop tables, textures) and biome palettes; extend JSON tables instead of code constants.
- Waterforms: rivers/lakes shoreline blending, cave water pockets, and erosion-aware banks (see `WorldGenAlgorithms`).
- Structures: dungeons, villages/outposts, and surface points of interest; spawn tables stored in JSON for server + client previews.
- Entities: passive/hostile mob definitions, spawning rules, AI state replicated via protobuf, client-side animation hooks.
- Items/Crafting: item stats, recipes, and tool efficiencies configured in JSON; UI panels aligned with server validation.
- UI/UX: HUD widgets for health/hunger/position/chunk, debug overlays for perf/worldgen seeds.

## Util / Tooling
- Configuration: keep runtime knobs in JSON (`config/`, `server-config.json`); split client/server files when helpful for maintenance.
- Protocol pipeline: `.proto` sources in `proto/` regenerate to `Assets/Generated/Protobuf/`; keep `SharedProtocol` validators in sync.
- Diagnostics: lightweight tracing (worldgen timings, chunk send/recv), crash repro captures, and replay-friendly logs.
- Build/Test: scripted `dotnet build SharedProtocol` + `dotnet build GameServer` and Unity CI hooks; smoke `--selftest` target for end-to-end checks.
- Developer utilities: editor tools under `CustomToolSet/`, map visualizers, and data pack hot-reload helpers.

## Suggested Implementation Order
1) Lock Core networking + worldgen loop (seed/config handling, chunk lifecycle, protobuf handler wiring).  
2) Stand up Content slices iteratively (blocks/biomes → waterforms/structures → entities/items), keeping data in JSON.  
3) Layer Util/diagnostics (protocol regeneration scripts, config validators, replay/log capture) to keep releases stable.  
4) Validate each milestone with `dotnet build`, `dotnet run --project GameServer -- --selftest`, and client smoke through Unity.  
