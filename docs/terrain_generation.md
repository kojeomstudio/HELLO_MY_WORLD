# Terrain Generation Documentation

## Overview

This document describes the advanced terrain generation algorithms used in the Minecraft-style game server. The terrain generation system implements hydrology-aware generation with flow memory, seam stabilization, and multi-stage processing.

## Architecture

### Core Components

- **ImprovedTerrainCoordinator.cs**: Central coordinator managing the complete terrain generation pipeline
- **ImprovedCaveGenerator.cs**: Hydrology-aware cave mask generation
- **ImprovedRiverGenerator.cs**: Hydrology-driven river mask builder
- **ImprovedLakeGenerator.cs**: Lake basin mask generator

### Generation Pipeline

1. **Base Terrain**: Noise-based heightmap generation
2. **Hydrology**: Water flow accumulation and drainage network
3. **Caves**: Regional main caves with hydrology awareness
4. **Rivers**: Hydrology-driven river networks
5. **Lakes**: Basin formation with wetland alignment

## Cave Generation

### Algorithm Overview

The cave generation system uses a regional main cave approach with hydrology awareness:

- **Regional Main Caves**: Large-scale cave systems spanning multiple chunks
- **Worm-based Generation**: 3D worm algorithms for cave tunnel creation
- **Hydrology Awareness**: Caves respect water tables and river networks
- **Entrance Stability**: Cave entrances are stabilized near hydrology features

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `RegionalMainCaveRegionSizeChunks` | Size of cave regions in chunks | 4 |
| `RegionalMainCaveWormCountMin` | Minimum worms per region | 4 |
| `RegionalMainCaveWormCountMax` | Maximum worms per region | 9 |
| `RegionalMainCaveStepsMin` | Minimum steps per worm | 180 |
| `RegionalMainCaveStepsMax` | Maximum steps per worm | 320 |
| `RegionalMainCaveMinY` | Minimum Y level | 14 |
| `RegionalMainCaveMaxY` | Maximum Y level | 72 |
| `CaveDensity` | Overall cave density | 0.3 |
| `CaveNoiseScale` | Noise scale for cave generation | 0.05 |

### Hydrology Integration

Caves interact with hydrology through:

1. **Support Pillars**: Pillars placed near hydrology seams to prevent collapse
2. **Moisture Retention**: Cave ceilings retain moisture from nearby water
3. **River Suppression**: Caves are suppressed near river networks
4. **Edge Sealing**: Cave edges are sealed at chunk boundaries

### Stability Features

- **Support Pillars**: Generated near hydrology seams with configurable density
- **Riparian Plugs**: Depth-based plugs at river interfaces
- **Ceiling Stability**: Moisture-aware ceiling stabilization
- **Edge Sealing**: Strong sealing at chunk boundaries

## River Generation

### Algorithm Overview

River generation uses hydrology-driven algorithms with flow memory:

- **Hydrology-driven**: Rivers follow natural drainage patterns
- **Flow Memory**: Rivers remember previous flow direction for continuity
- **Confluence Detection**: Detects and handles river confluences
- **Seam Feathering**: Smooth transitions at chunk boundaries

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `RiverCenterThreshold` | Threshold for river center | 0.0125 |
| `RiverBankThreshold` | Threshold for river banks | 0.028 |
| `RiverDepth` | Depth of river channels | 6 |
| `RiverNoiseScale` | Noise scale for river variation | 0.015 |
| `RiverEdgeFeather` | Feathering at river edges | 0.45 |
| `RiverMouthSmoothRadius` | Smoothing radius at river mouths | 3 |
| `RiverDeltaWetlandStrength` | Strength of delta wetlands | 0.45 |
| `RiverConfluenceBoost` | Boost at river confluences | 0.35 |

### Flow Memory System

The flow memory system ensures river continuity:

1. **Flow Memory**: Stores previous flow direction
2. **Flow Persistence**: Controls how long flow memory persists (0.68)
3. **Gradient Weight**: Weight of gradient in flow calculation (0.35)
4. **Directional Iterations**: Number of directional smoothing passes (1)

### Hydrology Integration

Rivers interact with hydrology through:

1. **Hydrology Smooth**: Smooths hydrology field (2 iterations, 0.6 blend)
2. **Shore Push**: Pushes rivers toward shores (5.0)
3. **Slope Penalty**: Penalizes steep slopes (6.0)
4. **Flow Gain**: Gains flow from hydrology (0.5)
5. **Flow Shadow**: Considers flow shadows (0.45 weight, 0.35 slope weight)

### Seam Stabilization

Rivers are stabilized at chunk boundaries:

1. **Edge Flow Bias**: Bias toward edge flow (0.35)
2. **Edge Tangent Weight**: Weight of tangent flow (0.45)
3. **Edge Flow Lock**: Lock flow at edges (0.38)
4. **Seam Relax**: Relax seams (2 iterations, 0.5 blend)

## Lake Generation

### Algorithm Overview

Lake generation creates basin-shaped water bodies with wetland alignment:

- **Basin Formation**: Creates basin-shaped lake beds
- **Hydrology Blending**: Blends with hydrology field
- **River Suppression**: Suppresses lakes near rivers
- **Wetland Buffer**: Creates wetland buffers around lakes
- **Outflow Carving**: Carves outflow channels

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `MinDepth` | Minimum lake depth | 3 |
| `MaxDepth` | Maximum lake depth | 9 |
| `MaxRadius` | Maximum lake radius | 9 |
| `LakeBasinSmoothIterations` | Smoothing iterations for basin | 2 |
| `ShelfDepth` | Depth of lake shelf | 2 |
| `SpawnWeightBias` | Bias in spawn weight | 0.3 |
| `ShorelineBlend` | Blending at shoreline | 0.66 |
| `RiverProximitySuppression` | Suppression near rivers | 0.35 |
| `WetlandSaturationThreshold` | Threshold for wetlands | 0.55 |
| `OutflowCarveDepth` | Depth of outflow carving | 2 |
| `WetlandBufferRadius` | Radius of wetland buffer | 2 |

### Hydrology Integration

Lakes interact with hydrology through:

1. **Hydrology Suppression**: Suppression based on hydrology field
2. **Flow Suppression**: Suppression based on flow field
3. **River Suppression**: Suppression near rivers (0.35)
4. **Flow Seepage**: Seepage from flow field (0.25)

### Stability Features

- **Outflow Stability**: Stability of outflow channels (0.3)
- **Wetland Buffer**: Buffer radius around lakes (2)
- **Shelf Depth**: Depth of lake shelf (2)
- **Basin Smoothing**: Smooths basin shape (2 iterations)

## Hydrology System

### Overview

The hydrology system simulates water flow across the terrain:

- **Flow Accumulation**: Accumulates water flow downhill
- **Flow Memory**: Remembers previous flow direction
- **Seam Stabilization**: Stabilizes flow at chunk boundaries
- **Gradient Calculation**: Calculates gradient for flow direction

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `HydrologySmoothIterations` | Smoothing iterations | 2 |
| `HydrologySmoothBlend` | Smoothing blend factor | 0.6 |
| `HydrologyShorePush` | Push toward shores | 5.0 |
| `HydrologySlopePenalty` | Penalty for steep slopes | 6.0 |
| `HydrologyFlowGain` | Gain from hydrology | 0.5 |
| `HydrologyFlowPersistence` | Flow memory persistence | 0.68 |
| `HydrologyGradientWeight` | Weight of gradient | 0.35 |
| `HydrologyGradientClamp` | Clamp for gradient | 1.65 |

### Seam Stabilization

The hydrology system includes comprehensive seam stabilization:

1. **Edge Flow Bias**: Bias toward edge flow (0.35)
2. **Edge Tangent Weight**: Weight of tangent flow (0.45)
3. **Edge Flow Lock**: Lock flow at edges (0.38)
4. **Edge Stability**: Stability iterations (1, 0.32 weight)
5. **Seam Relax**: Relax seams (2 iterations, 0.5 blend)

## Configuration

All terrain generation parameters are configured in `config/world.json`:

```json
{
  "Water": {
    "GlobalWaterLevel": 62,
    "RiverCenterThreshold": 0.0125,
    "RiverBankThreshold": 0.028,
    "HydrologySmoothIterations": 2,
    "HydrologySmoothBlend": 0.6,
    "HydrologyFlowPersistence": 0.68,
    "EnableOceans": true,
    "EnableRivers": true,
    "EnableLakes": true,
    "UseImprovedRivers": true,
    "UseImprovedLakes": true
  },
  "Caves": {
    "EnableCaves": true,
    "UseImprovedCaves": true,
    "UseRegionalMainCaves": true,
    "RegionalMainCaveRegionSizeChunks": 4,
    "CaveDensity": 0.3,
    "CaveNoiseScale": 0.05
  },
  "Lakes": {
    "MinDepth": 3,
    "MaxDepth": 9,
    "MaxRadius": 9,
    "LakeBasinSmoothIterations": 2,
    "ShelfDepth": 2
  }
}
```

## Implementation Notes

### Performance Considerations

- **Chunk-based Generation**: Terrain is generated per chunk (16x16 blocks)
- **Async Generation**: Chunk generation can be performed asynchronously
- **Caching**: Generated chunks are cached to avoid regeneration
- **Budget Enforcement**: Chunk generation budget prevents overload

### Stability Guarantees

- **Seam Stabilization**: All features are stabilized at chunk boundaries
- **Flow Memory**: Ensures continuity of hydrological features
- **Edge Sealing**: Prevents artifacts at chunk edges
- **Confluence Handling**: Proper handling of river confluences

### Extensibility

The terrain generation system is designed for extensibility:

- **Pluggable Generators**: New generators can be added easily
- **Configurable Parameters**: All parameters are configurable via JSON
- **Profile-based**: Different terrain profiles can be created
- **Hot-reload**: Configuration can be hot-reloaded

## References

- [`ImprovedTerrainCoordinator.cs`](../GameServer/World/Generation/ImprovedTerrainCoordinator.cs)
- [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs)
- [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs)
- [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs)
- [`config/world.json`](../config/world.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

## Overview

This document describes the advanced terrain generation algorithms used in the Minecraft-style game server. The terrain generation system implements hydrology-aware generation with flow memory, seam stabilization, and multi-stage processing.

## Architecture

### Core Components

- **ImprovedTerrainCoordinator.cs**: Central coordinator managing the complete terrain generation pipeline
- **ImprovedCaveGenerator.cs**: Hydrology-aware cave mask generation
- **ImprovedRiverGenerator.cs**: Hydrology-driven river mask builder
- **ImprovedLakeGenerator.cs**: Lake basin mask generator

### Generation Pipeline

1. **Base Terrain**: Noise-based heightmap generation
2. **Hydrology**: Water flow accumulation and drainage network
3. **Caves**: Regional main caves with hydrology awareness
4. **Rivers**: Hydrology-driven river networks
5. **Lakes**: Basin formation with wetland alignment

## Cave Generation

### Algorithm Overview

The cave generation system uses a regional main cave approach with hydrology awareness:

- **Regional Main Caves**: Large-scale cave systems spanning multiple chunks
- **Worm-based Generation**: 3D worm algorithms for cave tunnel creation
- **Hydrology Awareness**: Caves respect water tables and river networks
- **Entrance Stability**: Cave entrances are stabilized near hydrology features

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `RegionalMainCaveRegionSizeChunks` | Size of cave regions in chunks | 4 |
| `RegionalMainCaveWormCountMin` | Minimum worms per region | 4 |
| `RegionalMainCaveWormCountMax` | Maximum worms per region | 9 |
| `RegionalMainCaveStepsMin` | Minimum steps per worm | 180 |
| `RegionalMainCaveStepsMax` | Maximum steps per worm | 320 |
| `RegionalMainCaveMinY` | Minimum Y level | 14 |
| `RegionalMainCaveMaxY` | Maximum Y level | 72 |
| `CaveDensity` | Overall cave density | 0.3 |
| `CaveNoiseScale` | Noise scale for cave generation | 0.05 |

### Hydrology Integration

Caves interact with hydrology through:

1. **Support Pillars**: Pillars placed near hydrology seams to prevent collapse
2. **Moisture Retention**: Cave ceilings retain moisture from nearby water
3. **River Suppression**: Caves are suppressed near river networks
4. **Edge Sealing**: Cave edges are sealed at chunk boundaries

### Stability Features

- **Support Pillars**: Generated near hydrology seams with configurable density
- **Riparian Plugs**: Depth-based plugs at river interfaces
- **Ceiling Stability**: Moisture-aware ceiling stabilization
- **Edge Sealing**: Strong sealing at chunk boundaries

## River Generation

### Algorithm Overview

River generation uses hydrology-driven algorithms with flow memory:

- **Hydrology-driven**: Rivers follow natural drainage patterns
- **Flow Memory**: Rivers remember previous flow direction for continuity
- **Confluence Detection**: Detects and handles river confluences
- **Seam Feathering**: Smooth transitions at chunk boundaries

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `RiverCenterThreshold` | Threshold for river center | 0.0125 |
| `RiverBankThreshold` | Threshold for river banks | 0.028 |
| `RiverDepth` | Depth of river channels | 6 |
| `RiverNoiseScale` | Noise scale for river variation | 0.015 |
| `RiverEdgeFeather` | Feathering at river edges | 0.45 |
| `RiverMouthSmoothRadius` | Smoothing radius at river mouths | 3 |
| `RiverDeltaWetlandStrength` | Strength of delta wetlands | 0.45 |
| `RiverConfluenceBoost` | Boost at river confluences | 0.35 |

### Flow Memory System

The flow memory system ensures river continuity:

1. **Flow Memory**: Stores previous flow direction
2. **Flow Persistence**: Controls how long flow memory persists (0.68)
3. **Gradient Weight**: Weight of gradient in flow calculation (0.35)
4. **Directional Iterations**: Number of directional smoothing passes (1)

### Hydrology Integration

Rivers interact with hydrology through:

1. **Hydrology Smooth**: Smooths hydrology field (2 iterations, 0.6 blend)
2. **Shore Push**: Pushes rivers toward shores (5.0)
3. **Slope Penalty**: Penalizes steep slopes (6.0)
4. **Flow Gain**: Gains flow from hydrology (0.5)
5. **Flow Shadow**: Considers flow shadows (0.45 weight, 0.35 slope weight)

### Seam Stabilization

Rivers are stabilized at chunk boundaries:

1. **Edge Flow Bias**: Bias toward edge flow (0.35)
2. **Edge Tangent Weight**: Weight of tangent flow (0.45)
3. **Edge Flow Lock**: Lock flow at edges (0.38)
4. **Seam Relax**: Relax seams (2 iterations, 0.5 blend)

## Lake Generation

### Algorithm Overview

Lake generation creates basin-shaped water bodies with wetland alignment:

- **Basin Formation**: Creates basin-shaped lake beds
- **Hydrology Blending**: Blends with hydrology field
- **River Suppression**: Suppresses lakes near rivers
- **Wetland Buffer**: Creates wetland buffers around lakes
- **Outflow Carving**: Carves outflow channels

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `MinDepth` | Minimum lake depth | 3 |
| `MaxDepth` | Maximum lake depth | 9 |
| `MaxRadius` | Maximum lake radius | 9 |
| `LakeBasinSmoothIterations` | Smoothing iterations for basin | 2 |
| `ShelfDepth` | Depth of lake shelf | 2 |
| `SpawnWeightBias` | Bias in spawn weight | 0.3 |
| `ShorelineBlend` | Blending at shoreline | 0.66 |
| `RiverProximitySuppression` | Suppression near rivers | 0.35 |
| `WetlandSaturationThreshold` | Threshold for wetlands | 0.55 |
| `OutflowCarveDepth` | Depth of outflow carving | 2 |
| `WetlandBufferRadius` | Radius of wetland buffer | 2 |

### Hydrology Integration

Lakes interact with hydrology through:

1. **Hydrology Suppression**: Suppression based on hydrology field
2. **Flow Suppression**: Suppression based on flow field
3. **River Suppression**: Suppression near rivers (0.35)
4. **Flow Seepage**: Seepage from flow field (0.25)

### Stability Features

- **Outflow Stability**: Stability of outflow channels (0.3)
- **Wetland Buffer**: Buffer radius around lakes (2)
- **Shelf Depth**: Depth of lake shelf (2)
- **Basin Smoothing**: Smooths basin shape (2 iterations)

## Hydrology System

### Overview

The hydrology system simulates water flow across the terrain:

- **Flow Accumulation**: Accumulates water flow downhill
- **Flow Memory**: Remembers previous flow direction
- **Seam Stabilization**: Stabilizes flow at chunk boundaries
- **Gradient Calculation**: Calculates gradient for flow direction

### Key Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `HydrologySmoothIterations` | Smoothing iterations | 2 |
| `HydrologySmoothBlend` | Smoothing blend factor | 0.6 |
| `HydrologyShorePush` | Push toward shores | 5.0 |
| `HydrologySlopePenalty` | Penalty for steep slopes | 6.0 |
| `HydrologyFlowGain` | Gain from hydrology | 0.5 |
| `HydrologyFlowPersistence` | Flow memory persistence | 0.68 |
| `HydrologyGradientWeight` | Weight of gradient | 0.35 |
| `HydrologyGradientClamp` | Clamp for gradient | 1.65 |

### Seam Stabilization

The hydrology system includes comprehensive seam stabilization:

1. **Edge Flow Bias**: Bias toward edge flow (0.35)
2. **Edge Tangent Weight**: Weight of tangent flow (0.45)
3. **Edge Flow Lock**: Lock flow at edges (0.38)
4. **Edge Stability**: Stability iterations (1, 0.32 weight)
5. **Seam Relax**: Relax seams (2 iterations, 0.5 blend)

## Configuration

All terrain generation parameters are configured in `config/world.json`:

```json
{
  "Water": {
    "GlobalWaterLevel": 62,
    "RiverCenterThreshold": 0.0125,
    "RiverBankThreshold": 0.028,
    "HydrologySmoothIterations": 2,
    "HydrologySmoothBlend": 0.6,
    "HydrologyFlowPersistence": 0.68,
    "EnableOceans": true,
    "EnableRivers": true,
    "EnableLakes": true,
    "UseImprovedRivers": true,
    "UseImprovedLakes": true
  },
  "Caves": {
    "EnableCaves": true,
    "UseImprovedCaves": true,
    "UseRegionalMainCaves": true,
    "RegionalMainCaveRegionSizeChunks": 4,
    "CaveDensity": 0.3,
    "CaveNoiseScale": 0.05
  },
  "Lakes": {
    "MinDepth": 3,
    "MaxDepth": 9,
    "MaxRadius": 9,
    "LakeBasinSmoothIterations": 2,
    "ShelfDepth": 2
  }
}
```

## Implementation Notes

### Performance Considerations

- **Chunk-based Generation**: Terrain is generated per chunk (16x16 blocks)
- **Async Generation**: Chunk generation can be performed asynchronously
- **Caching**: Generated chunks are cached to avoid regeneration
- **Budget Enforcement**: Chunk generation budget prevents overload

### Stability Guarantees

- **Seam Stabilization**: All features are stabilized at chunk boundaries
- **Flow Memory**: Ensures continuity of hydrological features
- **Edge Sealing**: Prevents artifacts at chunk edges
- **Confluence Handling**: Proper handling of river confluences

### Extensibility

The terrain generation system is designed for extensibility:

- **Pluggable Generators**: New generators can be added easily
- **Configurable Parameters**: All parameters are configurable via JSON
- **Profile-based**: Different terrain profiles can be created
- **Hot-reload**: Configuration can be hot-reloaded

## References

- [`ImprovedTerrainCoordinator.cs`](../GameServer/World/Generation/ImprovedTerrainCoordinator.cs)
- [`ImprovedCaveGenerator.cs`](../GameServer/World/Generation/ImprovedCaveGenerator.cs)
- [`ImprovedRiverGenerator.cs`](../GameServer/World/Generation/ImprovedRiverGenerator.cs)
- [`ImprovedLakeGenerator.cs`](../GameServer/World/Generation/ImprovedLakeGenerator.cs)
- [`config/world.json`](../config/world.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

