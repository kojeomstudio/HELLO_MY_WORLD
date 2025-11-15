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
- **River continuity**: the 2025-10-30 update feathers riverbanks into neighbouring columns so meanders persist across chunk seams and sand shoulders drop smoothly beneath the waterline.
- **2025-10-31 alignment**: MapGeneratorLib now shares the server river/lake heuristics so Unity tooling and the dedicated server render identical sandbanks, basins, and meanders across chunk seams.
- **Pond shaping**: inland ponds now relax their banks after filling, replacing cliffy edges with gentle sand shelves to avoid hard transitions in Unity meshes.

### November 2025 Hydrology Update

- **Karst sinkholes & aquifer vents**: both `WorldManager.GenerateCavesInternal` and `MapGeneratorLib.GenerateSphereCaves` sample the hydrology + flow-accumulation masks to punch surface sinkholes, floodable cave pools, and short connector tunnels that track the local slope/river direction. The new helpers (`IntegrateKarstInlets` / `IntegrateKarstSinkholes`) keep underground lakes aligned with nearby river plains so Unity previews match the dedicated server.
- **Tributary stitching**: `StitchTributaryChannels` now extends minor watercourses from high-hydrology columns into the nearest river flow vector, reducing orphaned basins. The helper runs after the primary river pass in both codebases so erosion and sediment passes still honour the newly carved channels.
- **Clay/sand terraces**: `DepositLakeSedimentRings` lays down clay inside the lake bowl and sand on the immediate rim so the Unity mesh baker can shade shorelines accurately. This runs for procedural lakes in both the server and `MapGeneratorLib` so tooling screenshots and live gameplay stay in sync.
- **Validation loop**: `ChunkPayloadBuilder` calls `ProtocolValidator.ValidateEnhancedContracts()` on first use. If the Unity-generated protobuf classes fall out of date you will see a compile/build failure before chunk data ships to clients. Re-run `protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto` and rebuild `SharedProtocol/SharedProtocol.csproj` to clear the guard.
- **2025-11-11 stability + wetlands**: A shared `BuildCaveStabilityField` now measures hydrology pressure per column and feeds both `WorldManager` and `MapGeneratorLib`. The server uses the field to vary worm radii and drop cobblestone buttresses (`AddCaveColumnSupports`) so tall caverns remain navigable, while the Unity tooling mirrors the same supports for preview screenshots. Rivers gained a point-bar pass plus a floodplain wetland sculptor so braided channels spawn shallow clay pans that match across chunk seams. Lakes now layer shoreline vegetation rings and inlet seeps, again applied in both codebases so artisan tools see the same silhouettes the server streams to clients.
- **2025-11-13 shelves/swales/spillways**: Cave generation now adds stability-weighted shelf terraces and mid-span walkways so long caverns expose natural routes, with MapGeneratorLib mirroring the same `AddCaveShelfBands` pass for editor previews. Rivers gained a floodplain swale pass that braids shallow overflow gutters using the river-intensity field, hydrating them with sand or water rims on both server and tooling. Lakes now emit wetland spillways, carving seep-fed pockets just beyond the shoreline so Unity meshes match server lakeshores. These passes share the same hydrology masks, ensuring MapGeneratorLib screenshots match the dedicated server output exactly.
- **2025-11-14 vents/deltas/overflow**: Hydrology-weighted ventilation shafts now pierce long caverns, adding basalt rims and drip pools in both `WorldManager` and `MapGeneratorLib`. River generation gained `AddRiverDeltaFans`, which uses flow accumulation to widen braided confluences and seed sandbars that the Unity tooling reproduces one-to-one. Lakes evaluate nearby relief plus flow accumulation to carve overflow channels that reconnect isolated basins to the river field, again mirrored inside MapGeneratorLib so screenshots and live terrain stay synchronized.
- **2025-11-15 aquifers/gradient/equalizer**: Both codebases now spawn hydrology-driven cave aquifer channels, apply a river gradient smoothing pass that replaces jagged banks with sand shelves, and run a lake water-table equalizer before overflow carving so Unity captures and server chunks stay pixel-aligned. The new pass also feeds the enhanced protobuf payloads, which are validated via the tighter `ProtocolRegistry` coverage.
- **2025-11-15 terrace refresh**: Cave generation gained `AddCaveRibbonTerraces`, carving stability-weighted ledges with supporting buttresses so long caverns include playable walkways on both server (`WorldManager`) and tooling (`MapGeneratorLib`). Rivers now run `ApplyRiverMeanderTerraces`, which samples cross-channel intensity to widen inner-bank shoals and cut outer-bank undercuts, preventing razor-thin channels. Lakes add `AddLakeShorelineBenches`, a dual-ring sculptor that lays down submerged clay shelves and dry grass benches so shoreline silhouettes match between Unity previews and streamed chunks.
- **2025-11-16 hydrology feedback loop**: The new `ApplyCaveHydrologyErosion` pass runs after shelf bands to carve saturated underground streams, refill them with water based on the shared hydrology masks, and open side vents when pressure stays high. Rivers now execute `ApplyRiverHydrologyFeedback` after meander terraces, cutting micro-channels where flow accumulation spikes and reinforcing sand/grass levees on over-pressured banks so wetlands blend seamlessly into plains. Lakes call `StabilizeLakeCatchments` immediately after overflow trenching to balance shoreline pressure, lowering rim voxels, adding inlet pockets, and infilling them with sand or water to match the current hydrology masks. Because these passes live in both `WorldManager` and MapGeneratorLib, server chunks and Unity previews continue to align perfectly.

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
- **Noise layer**: an extra 3D noise pass carves out porous cave pockets and underground lakes that align across chunk boundaries, while deep strata occasionally flood with lava or groundwater depending on depth.

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
