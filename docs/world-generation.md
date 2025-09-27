# World Generation Overview (GameServer)

The 2024 refresh of `WorldManager` adds coastal oceans, navigable rivers, inland lakes, and highland terrain (mountains, hills, cliffs) on top of the existing cave and dungeon pass. This document explains how the generation pipeline works so future tweaks and biome additions stay consistent.

## Terrain Pipeline

Each chunk column (`16×16`) now flows through a deterministic terrain profile:

1. **Continentalness mask** – low-frequency Simplex noise blended with classic Perlin through domain warping determines whether a column lies on land, shallow coast, or deep ocean. Columns that fall below the `OceanThreshold` are marked as Ocean biomes and automatically filled with water up to the global sea level (Y=62).
2. **Erosion & ridges** – additional Simplex/Perlin passes shape hills versus mountains. A ridged profile produces sharp peaks while the erosion signal dampens steep slopes, giving rolling foothills and dramatic ridgelines.
3. **Biome selection** – temperature/humidity noise now uses both Simplex and Perlin samplers. The result drives the existing biomes as well as the extended highland set (`Mountains`, `Hills`, `Cliffs`, `Beach`).
4. **Column sculpting** – the pipeline lays down bedrock, filler stone, biome-specific topsoil (grass/dirt, sand, cobblestone cliffs) and applies water caps for oceans and freshwater pockets.
5. **Post-processing** – after the base terrain is in place we apply ore generation, caves, dungeons, rivers, lakes, vegetation, and clouds.

> **Note**: The new pipeline keeps all previous features (ores, caves, dungeons, vegetation) but now feeds them richer biome context and more varied elevation data so worlds feel less flat and more “Minecraft-like”.

### Stage-based Execution

`WorldManager` now orchestrates chunk creation through `TerrainGenerationPipeline`, an ordered series of `ITerrainGenerationStage` implementations. Each stage receives a shared `TerrainGenerationContext` so base heightmap data, ore placement, caves/dungeons, rivers, lakes, vegetation, and clouds can collaborate without duplicating setup. The pipeline provides a natural extension point for future biome or structure passes while keeping legacy systems composable and testable.

## Water Features

- **Global water level**: a constant `GlobalWaterLevel` (62) controls sea height. Ocean biomes carve a seafloor using sand and stone, then fill to this level.
- **Rivers**: a signed noise field locates river centres and banks. Columns near the river core are cleared, filled with flowing water, and lined with sand.
- **Lakes**: per-chunk randomised ellipses carve depressions, line them with sand, and fill them to a locally sampled water level. Adjacent banks receive light smoothing to blend into surrounding terrain.

## Highlands & Cliffs

- **Mountains**: ridged noise creates high elevation peaks; above ~Y105 we favour stone caps, optionally transitioning to cobblestone for rugged summits.
- **Hills**: gentle noise adds rolling terrain between plains and mountains.
- **Cliffs**: steep slopes set `UseCliffFace`, replacing the top layers with cobblestone to create sheer faces and overhangs.
- **Beaches**: continentalness thresholds mark coastline cells and swap the surface for sand while keeping the column open to shallow seawater.

## Underground Content

- **Caves**: the existing multi-pass cave system (worm tunnels, caverns, vertical shafts) now operates on the new stratified terrain without change.
- **Dungeons**: `GenerateDungeons` was expanded with:
  - Interior decoration (pillars, water/lava pools, ore “loot” pedestals).
  - Support for multi-room layouts and simple mazes.
  - Procedural entrances and varied room connections for smoother exploration loops.

## Vegetation & Clouds

Vegetation density now respects the extended biome list:

- Forests remain dense with a mix of trees and tall grass.
- Plains receive sparse tall grass.
- Deserts and beaches sprinkle dead bushes.
- Mountains and cliffs keep vegetation minimal to preserve rocky silhouettes.

Clouds still float above the world but now vary in altitude based on a domain-warped Simplex/Perlin blend, producing broken skies instead of parallel bands.

## Biome & Block Enumerations

- `BiomeType` now includes `Ocean`, `Mountains`, `Hills`, `Cliffs`, and `Beach`. These propagate to the client via chunk metadata and lobby protocols.
- No new block IDs were introduced; existing materials (stone, dirt, sand, cobblestone, water) are reused to express the new terrain features.

## Extending Further

- **New biomes**: Add entries to `BiomeType`, extend `CalculateTerrainProfile`, and adjust vegetation density/loot tables accordingly.
- **Custom structures**: Insert new post-processing passes after terrain generation but before caves/dungeons if large scale structures are required.
- **Tuning water features**: Adjust `RiverCenterThreshold`, `RiverBankThreshold`, and lake probability constants for different world densities.

The updated generator aims to produce striking, traversable landscapes while remaining deterministic and race-free for multiplayer synchronisation.
