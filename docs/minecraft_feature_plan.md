# Minecraft Feature Breakdown (Core / Content / Utility)

This file inventories Minecraft-like features for both **server** and **client**, grouped into Core, Content, and Utility. Each item notes current status and the intended rollout order so implementation can proceed sequentially.

## Core

**Server**
- ✅ World seed + chunk streaming
- ✅ Base terrain heightmap generation
- ✅ Hydrology masks and river/lake toggles
- ✅ Basic cave carving (with improved stabilizers)
- ✅ Protobuf packet framing and dispatcher
- ✅ Session/auth pipeline
- ◻ Biome synthesis (temperature/humidity gradient)
- ◻ Chunk compression for network payloads
- ◻ Connection rate limiting and reconnect logic
- ◻ World border enforcement
- ◻ Server reconciliation for client prediction

**Client**
- ✅ Chunk mesh generation and rendering
- ✅ Block placement/break controls
- ✅ HUD/inventory overlay
- ✅ Protobuf network client
- ◻ Frustum/LOD culling
- ◻ Transparent/animated block rendering
- ◻ Debug overlays for worldgen fields (hydrology, caves)
- ◻ Settings/menu shell
- ◻ Input rebinding and controller profiles

## Content

**Server**
- ✅ Block crafting/furnace logic
- ✅ Hunger/health systems
- ✅ Basic player entity lifecycle
- ◻ Tool durability + enchanting
- ◻ Potion brewing
- ◻ Mob spawning + AI behaviors
- ◻ Weather + day/night effects
- ◻ Structure generation (villages/dungeons framework)
- ◻ Respawn/bed handling

**Client**
- ✅ Base block textures
- ✅ Inventory UI wiring
- ◻ Crafting/furnace/enchanting interfaces
- ◻ Entity models/animations
- ◻ Particle and weather visuals
- ◻ Sky + day/night cycle visuals
- ◻ Resource/skin pack support

## Utility

**Server**
- ✅ JSON-driven configs (`config/server.json`, `config/world.json`)
- ✅ Shared worldgen profile broadcast (map control)
- ◻ Admin command/permission system
- ◻ Monitoring + profiling hooks
- ◻ Backup/restore workflow and data validation
- ◻ Protocol version negotiation + compatibility gate

**Client**
- ✅ Octree collision acceleration
- ◻ Render distance/quality presets
- ◻ Network quality indicator
- ◻ Crash/log capture + uploader
- ◻ Replay/screenshot tools

## Implementation Order (sequential)
1) **Protocol + Config Hardening**: finalize protobuf usage, validate JSON world/config files, and keep map-control profile in sync.
2) **Worldgen Enhancements**: refine caves/rivers/lakes with hydrology-aware parameters; expose tunables via JSON and profile.
3) **Core Performance**: add culling/LOD client-side; add compression/rate-limit server-side.
4) **Content Layers**: progress enchanting/durability, structure generation, and matching UIs.
5) **Utility/Operations**: admin commands, monitoring, backup/restore, and quality-of-life client tools.

Status legend: ✅ done, ◻ pending.
