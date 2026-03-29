# Minecraft Feature Inventory (Core / Content / Util)

**Date:** 2026-01-11  
**Scope:** Client + Server alignment (terrain, protobuf, world-map control)  
**Order:** Execute tasks in the listed order for today.

---

## Core
1) **Hydrology-stable world generation** – *In Progress*  
   - Server: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`, `ImprovedCaveGenerator.cs`, `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`  
   - Client: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs` (`EnhancedTerrainGenerator`)  
   - Data: `config/world.json`, `config/enhanced-terrain-config.json`, `config/world_map_control_profile.json`
2) **World-map control parity** – *Planned*  
   - Server: `WorldMapControlManager`, `WorldMapControlProfileUtility`, generation signature  
   - Client: `WorldMapController`, `WorldMapControlProfile`, StreamingAssets `world-map-control.json`
3) **Networking & protobuf registry** – *Stable / Validate today*  
   - Shared: `SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`, `ProtocolValidator.cs`, generated DTOs under `Assets/Generated/Protobuf/`
4) **Chunk + session pipeline** – *Monitor*  
   - Server: `GameServer/Handlers/*`, `SessionManager.cs`, `WorldManager.cs`  
   - Client: `Assets/MyAssets/Scripts/GameWorld/WorldAreaManager.cs`

## Content
1) **River / lake carving & wetlands** – *In Progress*  
   - Server: `ImprovedRiverGenerator.cs`, `ImprovedLakeGenerator.cs`  
   - Client: `EnhancedTerrainGenerator.BuildRiverMask/BuildLakeMask`
2) **Cave carving / supports** – *In Progress*  
   - Server: `ImprovedCaveGenerator.cs`  
   - Client: `EnhancedTerrainGenerator.BuildCaveMask`
3) **Biomes & surface dressing** – *Stable* (review biomes JSON)  
   - Data: `config/biomes.json`

## Util
1) **Data-driven configs** – *Stable*  
   - JSON: `config/world.json`, `config/world_map_control_profile.json`, `server-config.json`, `config/client_config.json`
2) **Diagnostics & validation** – *Planned*  
   - Proto integrity checks, generation-signature drift logging
3) **Tools & pipelines** – *Stable*  
   - Protoc refresh: `proto/*.proto` → `Assets/Generated/Protobuf/`

---

## Sequenced To-Do (Today)
- [ ] Tighten hydrology/flow edge normalization (server + client) for caves/rivers/lakes.
- [ ] Harden world-map control parity (generation signature includes watershed radius; preview generator mirrors seam fixes).
- [ ] Revalidate protobuf registry bindings and descriptor packages.
- [ ] Keep configs/json data as the single source of truth; mirror into docs.

## Completed References (recent commits)
- `d4c05bbe` terrain/protobuf/doc refresh
- `d11a693f` hydrology seam normalization + proto guard
- `2f069380` documentation + system review
