# 2026-01-26 Terrain Generation Algorithm Review

## Overview
This document provides a comprehensive review of the terrain generation algorithms implemented in the Minecraft server, focusing on caves, rivers, and lakes generation.

## Metadata
- **Date**: 2026-01-26
- **Session**: 17
- **Review Scope**: Terrain generation algorithms
- **Status**: Comprehensive review completed

## Algorithm Architecture

### ImprovedTerrainCoordinator
**File**: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`

#### Purpose
Central coordinator that orchestrates terrain generation by integrating cave, river, and lake generation with advanced hydrology simulation.

#### Key Components

1. **Hydrology Mask Generation**
   - Water-table clamping based on sea level proximity
   - Slope-aware hydrology distribution
   - Curvature-guided water flow
   - Riparian buffer application
   - Edge stabilization and flow locks

2. **Flow Accumulation**
   - Downhill vector computation
   - Flow memory integration
   - Watershed stitching
   - Flow shadow application
   - Gradient-aware flow distribution

3. **Erosion Risk Field**
   - Surface range normalization
   - Slope-based erosion calculation
   - Hydrology and flow integration
   - Altitude and valley exposure
   - Combined risk assessment

4. **Advanced Processing Stages**
   - Flow memory application
   - Hydrology-flow blending
   - Curvature hydrology guidance
   - Hydrology continuity envelope
   - Hydrology edge envelope
   - Cross-chunk hydrology stitching
   - Hydrology edge cohesion
   - Surface harmonization
   - Erosion-aware damping
   - Hydrology momentum
   - Subterranean hydrology shield
   - Riparian flow bridge
   - Lake hydrology seepage
   - River/lake hydrology feedback
   - Riparian cave buffer

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Comprehensive hydrology simulation with multiple feedback loops
- ✅ Advanced edge handling for seamless chunk transitions
- ✅ Erosion-aware terrain generation
- ✅ Curvature-guided water flow
- ✅ Cross-chunk stitching for continuity
- ✅ Subterranean shield prevents cave flooding
- ✅ Riparian flow bridge ensures river/lake stability
- ✅ Data-driven configuration via WorldGenerationConfig

**Areas for Improvement:**
- 🔧 Performance optimization for large-scale generation
- 🔧 GPU acceleration potential for noise generation
- 🔧 Configurable algorithm complexity levels
- 🔧 Enhanced biome integration

### ImprovedCaveGenerator
**File**: `GameServer/World/Generation/ImprovedCaveGenerator.cs`

#### Purpose
Hydrology-aware cave mask generator that creates realistic cave systems while preventing flooding and ensuring structural integrity.

#### Key Features

1. **Hydrology Integration**
   - Hydrology mask influence on cave generation
   - Flow mask integration for water flow awareness
   - River pressure suppression near rivers
   - Erosion risk consideration
   - Edge factor calculation for chunk boundaries

2. **Cave Generation Algorithm**
   - Simplex noise-based density calculation
   - Perlin noise for secondary detail
   - Domain warping for natural cave shapes
   - Depth-based threshold adjustment
   - Moisture penalty for wet areas

3. **Advanced Stability Features**
   - Column stability computation
   - Seam stability for chunk edges
   - Ceiling moisture clamping
   - Slope stability weighting
   - Variance brake for roughness control
   - Saturation brake for wet areas
   - Flow shadow integration

4. **Post-Processing**
   - Mask smoothing for natural cave shapes
   - Riparian cave plugging near water
   - Support pillar generation
   - Edge sealing for chunk boundaries
   - Wet ceiling sealing below water table
   - Flooded cave detection

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Hydrology-aware generation prevents cave flooding
- ✅ River suppression near water bodies
- ✅ Chunk edge sealing prevents discontinuities
- ✅ Support pillars prevent collapse
- ✅ Riparian plugging maintains water integrity
- ✅ Wet ceiling sealing prevents water leakage
- ✅ Flooded cave detection for underwater features
- ✅ Comprehensive stability calculations

**Areas for Improvement:**
- 🔧 Enhanced cave connectivity analysis
- 🔧 Biome-specific cave generation
- 🔧 Ore distribution integration
- 🔧 Cave system complexity configuration

### ImprovedRiverGenerator
**File**: `GameServer/World/Generation/ImprovedRiverGenerator.cs`

#### Purpose
Hydrology-driven river mask builder that creates realistic river systems with proper flow dynamics and edge handling.

#### Key Features

1. **Noise Layering**
   - Base noise for primary river path
   - Macro noise for large-scale features
   - Detail noise for small-scale variations
   - Meander noise for natural river bends

2. **Hydrology Integration**
   - Hydrology mask for water distribution
   - Flow accumulation for river intensity
   - Erosion risk for bank stability
   - Flow memory for continuity
   - Seam hydrology for edge handling

3. **River Dynamics**
   - Meander factor calculation
   - Confluence boost for river junctions
   - Flow alignment with terrain
   - Directional bias for flow direction
   - Anisotropy for directional flow

4. **Edge Processing**
   - Seam feathering for smooth transitions
   - Edge normalization for consistency
   - Watershed stitching for continuity
   - Flow shadow application
   - Gradient stability enforcement

5. **Advanced Features**
   - Water table clamping
   - Relief penalty for high terrain
   - River bank erosion modeling
   - Delta wetland strength
   - Headwater stability
   - River mouth smoothing

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Multi-layered noise for natural river shapes
- ✅ Hydrology-driven generation
- ✅ Proper meander calculation
- ✅ Confluence boost for river junctions
- ✅ Comprehensive edge handling
- ✅ Flow-aware width modulation
- ✅ Erosion modeling
- ✅ Delta wetland support

**Areas for Improvement:**
- 🔧 Seasonal flow variation
- 🔧 Floodplain expansion
- 🔧 Tributary network generation
- 🔧 River depth variation

### ImprovedLakeGenerator
**File**: `GameServer/World/Generation/ImprovedLakeGenerator.cs`

#### Purpose
Lake basin mask generator that creates realistic lake systems with proper depth, shoreline, and outflow characteristics.

#### Key Features

1. **Noise Layering**
   - Basin noise for lake shape
   - Rim noise for shoreline detail
   - Macro noise for large-scale features
   - Detail noise for small variations

2. **Hydrology Integration**
   - Hydrology mask for water distribution
   - Flow accumulation for lake intensity
   - River suppression for separation
   - Inflow blend for river connections
   - Flow memory for continuity

3. **Lake Characteristics**
   - Depth-based thresholding
   - Shoreline jitter for natural edges
   - Lake shelf application
   - Wetland buffer creation
   - Outflow channel carving

4. **Stability Features**
   - Basin stability calculation
   - Outflow stability weighting
   - Flow seepage continuity
   - Momentum assistance
   - Variance control
   - Edge repair mechanisms

5. **Post-Processing**
   - Variance clamping
   - Edge band normalization
   - Gradient stability
   - Basin smoothing
   - Edge stitching
   - Basin filling
   - Edge relaxation

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Multi-layered noise for natural lake shapes
- ✅ Hydrology-seeded generation
- ✅ Proper depth handling
- ✅ Shoreline jitter for natural edges
- ✅ Lake shelf implementation
- ✅ Wetland buffer creation
- ✅ Outflow channel carving
- ✅ Comprehensive edge handling

**Areas for Improvement:**
- 🔧 Seasonal water level variation
- 🔧 Lake ecosystem integration
- 🔧 Underwater terrain generation
- 🔧 Lake depth variation

## TerrainMaskUtility

**File**: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` (internal static class)

#### Purpose
Utility class providing common terrain mask operations used across all generation algorithms.

#### Key Functions

1. **Basic Operations**
   - `Clamp01`: Value clamping to [0, 1]
   - `ComputeSlope`: Slope calculation from height map
   - `SampleInterior`: Interior sampling for edge handling
   - `SampleVariance`: Variance calculation

2. **Smoothing Operations**
   - `Smooth2D`: 2D Gaussian-like smoothing
   - `DirectionalSmooth`: Direction-aware smoothing
   - `BlendInterior`: Interior blending

3. **Edge Handling**
   - `StabilizeEdges`: Edge stabilization
   - `ApplyEdgeFlowLocks`: Flow locking at edges
   - `NormalizeEdgeBands`: Edge band normalization
   - `NormalizeEdges`: Edge normalization
   - `RelaxEdges`: Edge relaxation
   - `StitchEdges`: Edge stitching

4. **Advanced Operations**
   - `ApplyRiparianBuffer`: Riparian zone buffering
   - `ClampVariance`: Variance clamping
   - `FillBasins`: Basin filling
   - `ApplyFlowShadow`: Flow shadow application
   - `ApplyGradientStability`: Gradient stability enforcement
   - `BlendWatershedEdges`: Watershed edge blending
   - `BalanceHydrologyPressure`: Hydrology pressure balancing

5. **Vector Operations**
   - `ComputeDownhillVector`: Downhill direction calculation

#### Quality Assessment

**Strengths:**
- ✅ Comprehensive utility functions
- ✅ Consistent edge handling
- ✅ Advanced smoothing algorithms
- ✅ Proper variance control
- ✅ Gradient-aware operations
- ✅ Flow-aware processing

## Configuration Integration

### WorldGenerationConfig
- **Caves**: Cave generation parameters
- **Water**: Water and hydrology parameters
- **Lakes**: Lake generation parameters
- **TerrainGeneration**: Base terrain parameters

### Key Configuration Parameters

#### Cave Configuration
- Threshold, frequency (horizontal/vertical)
- Edge seal strength, moisture retention
- River suppression, support pillars
- Riparian plug depth, ceiling stability

#### Water Configuration
- Hydrology pressure, flow gain, persistence
- Edge blend radius, stability weight
- Water table clamp, slope penalty
- River depth, meander jitter
- Lake inflow blend, rim erosion

#### Lake Configuration
- Min/max depth, shelf depth
- Spawn weight bias, variance weight
- Outflow stability, wetland buffer
- Shoreline blend, river suppression

## Performance Considerations

### Current Performance Characteristics
- **Coordinate Complexity**: O(n²) per chunk for 2D operations
- **Noise Generation**: Multiple Simplex/Perlin noise calls per pixel
- **Memory Usage**: Multiple float arrays per chunk
- **Iteration Count**: Multiple smoothing/stability iterations

### Optimization Opportunities
1. **GPU Acceleration**
   - Noise generation on GPU
   - Parallel smoothing operations
   - Vectorized calculations

2. **Algorithm Optimization**
   - Reduced iteration counts
   - Early termination conditions
   - Spatial partitioning

3. **Memory Optimization**
   - Array pooling
   - In-place operations where possible
   - Reduced temporary allocations

## Integration Points

### World Generation Pipeline
1. Base terrain generation
2. Height map creation
3. Hydrology mask generation
4. Flow accumulation calculation
5. Erosion risk field construction
6. River mask generation
7. Lake mask generation
8. Cave mask generation
9. Final terrain assembly

### Client-Server Synchronization
- Hydrology signature in map control profiles
- Profile version validation
- Real-time terrain preview updates
- Consistent random seed usage

## Testing Recommendations

### Unit Tests
- Individual algorithm validation
- Edge case handling
- Parameter boundary testing
- Configuration validation

### Integration Tests
- Full pipeline execution
- Chunk boundary handling
- Client-server synchronization
- Performance profiling

### Visual Tests
- Terrain quality assessment
- Cave system connectivity
- River flow naturalness
- Lake shape realism

## Conclusion

The terrain generation algorithms implemented in the Minecraft server demonstrate a high level of sophistication with:

1. **Comprehensive Hydrology Integration**: All algorithms properly integrate hydrology, flow, and erosion data
2. **Advanced Edge Handling**: Seamless chunk transitions through sophisticated edge processing
3. **Natural Terrain Features**: Multi-layered noise and advanced processing create realistic terrain
4. **Data-Driven Configuration**: Flexible configuration system allows easy tuning
5. **Stability Focus**: Multiple stability mechanisms ensure terrain integrity

The algorithms are production-ready with opportunities for performance optimization and additional feature integration.

## References
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
- `GameServer/World/Generation/ImprovedCaveGenerator.cs`
- `GameServer/World/Generation/ImprovedRiverGenerator.cs`
- `GameServer/World/Generation/ImprovedLakeGenerator.cs`
- `config/minecraft_feature_client_server_core_content_util_2026-01-26.json`

## Overview
This document provides a comprehensive review of the terrain generation algorithms implemented in the Minecraft server, focusing on caves, rivers, and lakes generation.

## Metadata
- **Date**: 2026-01-26
- **Session**: 17
- **Review Scope**: Terrain generation algorithms
- **Status**: Comprehensive review completed

## Algorithm Architecture

### ImprovedTerrainCoordinator
**File**: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`

#### Purpose
Central coordinator that orchestrates terrain generation by integrating cave, river, and lake generation with advanced hydrology simulation.

#### Key Components

1. **Hydrology Mask Generation**
   - Water-table clamping based on sea level proximity
   - Slope-aware hydrology distribution
   - Curvature-guided water flow
   - Riparian buffer application
   - Edge stabilization and flow locks

2. **Flow Accumulation**
   - Downhill vector computation
   - Flow memory integration
   - Watershed stitching
   - Flow shadow application
   - Gradient-aware flow distribution

3. **Erosion Risk Field**
   - Surface range normalization
   - Slope-based erosion calculation
   - Hydrology and flow integration
   - Altitude and valley exposure
   - Combined risk assessment

4. **Advanced Processing Stages**
   - Flow memory application
   - Hydrology-flow blending
   - Curvature hydrology guidance
   - Hydrology continuity envelope
   - Hydrology edge envelope
   - Cross-chunk hydrology stitching
   - Hydrology edge cohesion
   - Surface harmonization
   - Erosion-aware damping
   - Hydrology momentum
   - Subterranean hydrology shield
   - Riparian flow bridge
   - Lake hydrology seepage
   - River/lake hydrology feedback
   - Riparian cave buffer

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Comprehensive hydrology simulation with multiple feedback loops
- ✅ Advanced edge handling for seamless chunk transitions
- ✅ Erosion-aware terrain generation
- ✅ Curvature-guided water flow
- ✅ Cross-chunk stitching for continuity
- ✅ Subterranean shield prevents cave flooding
- ✅ Riparian flow bridge ensures river/lake stability
- ✅ Data-driven configuration via WorldGenerationConfig

**Areas for Improvement:**
- 🔧 Performance optimization for large-scale generation
- 🔧 GPU acceleration potential for noise generation
- 🔧 Configurable algorithm complexity levels
- 🔧 Enhanced biome integration

### ImprovedCaveGenerator
**File**: `GameServer/World/Generation/ImprovedCaveGenerator.cs`

#### Purpose
Hydrology-aware cave mask generator that creates realistic cave systems while preventing flooding and ensuring structural integrity.

#### Key Features

1. **Hydrology Integration**
   - Hydrology mask influence on cave generation
   - Flow mask integration for water flow awareness
   - River pressure suppression near rivers
   - Erosion risk consideration
   - Edge factor calculation for chunk boundaries

2. **Cave Generation Algorithm**
   - Simplex noise-based density calculation
   - Perlin noise for secondary detail
   - Domain warping for natural cave shapes
   - Depth-based threshold adjustment
   - Moisture penalty for wet areas

3. **Advanced Stability Features**
   - Column stability computation
   - Seam stability for chunk edges
   - Ceiling moisture clamping
   - Slope stability weighting
   - Variance brake for roughness control
   - Saturation brake for wet areas
   - Flow shadow integration

4. **Post-Processing**
   - Mask smoothing for natural cave shapes
   - Riparian cave plugging near water
   - Support pillar generation
   - Edge sealing for chunk boundaries
   - Wet ceiling sealing below water table
   - Flooded cave detection

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Hydrology-aware generation prevents cave flooding
- ✅ River suppression near water bodies
- ✅ Chunk edge sealing prevents discontinuities
- ✅ Support pillars prevent collapse
- ✅ Riparian plugging maintains water integrity
- ✅ Wet ceiling sealing prevents water leakage
- ✅ Flooded cave detection for underwater features
- ✅ Comprehensive stability calculations

**Areas for Improvement:**
- 🔧 Enhanced cave connectivity analysis
- 🔧 Biome-specific cave generation
- 🔧 Ore distribution integration
- 🔧 Cave system complexity configuration

### ImprovedRiverGenerator
**File**: `GameServer/World/Generation/ImprovedRiverGenerator.cs`

#### Purpose
Hydrology-driven river mask builder that creates realistic river systems with proper flow dynamics and edge handling.

#### Key Features

1. **Noise Layering**
   - Base noise for primary river path
   - Macro noise for large-scale features
   - Detail noise for small-scale variations
   - Meander noise for natural river bends

2. **Hydrology Integration**
   - Hydrology mask for water distribution
   - Flow accumulation for river intensity
   - Erosion risk for bank stability
   - Flow memory for continuity
   - Seam hydrology for edge handling

3. **River Dynamics**
   - Meander factor calculation
   - Confluence boost for river junctions
   - Flow alignment with terrain
   - Directional bias for flow direction
   - Anisotropy for directional flow

4. **Edge Processing**
   - Seam feathering for smooth transitions
   - Edge normalization for consistency
   - Watershed stitching for continuity
   - Flow shadow application
   - Gradient stability enforcement

5. **Advanced Features**
   - Water table clamping
   - Relief penalty for high terrain
   - River bank erosion modeling
   - Delta wetland strength
   - Headwater stability
   - River mouth smoothing

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Multi-layered noise for natural river shapes
- ✅ Hydrology-driven generation
- ✅ Proper meander calculation
- ✅ Confluence boost for river junctions
- ✅ Comprehensive edge handling
- ✅ Flow-aware width modulation
- ✅ Erosion modeling
- ✅ Delta wetland support

**Areas for Improvement:**
- 🔧 Seasonal flow variation
- 🔧 Floodplain expansion
- 🔧 Tributary network generation
- 🔧 River depth variation

### ImprovedLakeGenerator
**File**: `GameServer/World/Generation/ImprovedLakeGenerator.cs`

#### Purpose
Lake basin mask generator that creates realistic lake systems with proper depth, shoreline, and outflow characteristics.

#### Key Features

1. **Noise Layering**
   - Basin noise for lake shape
   - Rim noise for shoreline detail
   - Macro noise for large-scale features
   - Detail noise for small variations

2. **Hydrology Integration**
   - Hydrology mask for water distribution
   - Flow accumulation for lake intensity
   - River suppression for separation
   - Inflow blend for river connections
   - Flow memory for continuity

3. **Lake Characteristics**
   - Depth-based thresholding
   - Shoreline jitter for natural edges
   - Lake shelf application
   - Wetland buffer creation
   - Outflow channel carving

4. **Stability Features**
   - Basin stability calculation
   - Outflow stability weighting
   - Flow seepage continuity
   - Momentum assistance
   - Variance control
   - Edge repair mechanisms

5. **Post-Processing**
   - Variance clamping
   - Edge band normalization
   - Gradient stability
   - Basin smoothing
   - Edge stitching
   - Basin filling
   - Edge relaxation

#### Algorithm Quality Assessment

**Strengths:**
- ✅ Multi-layered noise for natural lake shapes
- ✅ Hydrology-seeded generation
- ✅ Proper depth handling
- ✅ Shoreline jitter for natural edges
- ✅ Lake shelf implementation
- ✅ Wetland buffer creation
- ✅ Outflow channel carving
- ✅ Comprehensive edge handling

**Areas for Improvement:**
- 🔧 Seasonal water level variation
- 🔧 Lake ecosystem integration
- 🔧 Underwater terrain generation
- 🔧 Lake depth variation

## TerrainMaskUtility

**File**: `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` (internal static class)

#### Purpose
Utility class providing common terrain mask operations used across all generation algorithms.

#### Key Functions

1. **Basic Operations**
   - `Clamp01`: Value clamping to [0, 1]
   - `ComputeSlope`: Slope calculation from height map
   - `SampleInterior`: Interior sampling for edge handling
   - `SampleVariance`: Variance calculation

2. **Smoothing Operations**
   - `Smooth2D`: 2D Gaussian-like smoothing
   - `DirectionalSmooth`: Direction-aware smoothing
   - `BlendInterior`: Interior blending

3. **Edge Handling**
   - `StabilizeEdges`: Edge stabilization
   - `ApplyEdgeFlowLocks`: Flow locking at edges
   - `NormalizeEdgeBands`: Edge band normalization
   - `NormalizeEdges`: Edge normalization
   - `RelaxEdges`: Edge relaxation
   - `StitchEdges`: Edge stitching

4. **Advanced Operations**
   - `ApplyRiparianBuffer`: Riparian zone buffering
   - `ClampVariance`: Variance clamping
   - `FillBasins`: Basin filling
   - `ApplyFlowShadow`: Flow shadow application
   - `ApplyGradientStability`: Gradient stability enforcement
   - `BlendWatershedEdges`: Watershed edge blending
   - `BalanceHydrologyPressure`: Hydrology pressure balancing

5. **Vector Operations**
   - `ComputeDownhillVector`: Downhill direction calculation

#### Quality Assessment

**Strengths:**
- ✅ Comprehensive utility functions
- ✅ Consistent edge handling
- ✅ Advanced smoothing algorithms
- ✅ Proper variance control
- ✅ Gradient-aware operations
- ✅ Flow-aware processing

## Configuration Integration

### WorldGenerationConfig
- **Caves**: Cave generation parameters
- **Water**: Water and hydrology parameters
- **Lakes**: Lake generation parameters
- **TerrainGeneration**: Base terrain parameters

### Key Configuration Parameters

#### Cave Configuration
- Threshold, frequency (horizontal/vertical)
- Edge seal strength, moisture retention
- River suppression, support pillars
- Riparian plug depth, ceiling stability

#### Water Configuration
- Hydrology pressure, flow gain, persistence
- Edge blend radius, stability weight
- Water table clamp, slope penalty
- River depth, meander jitter
- Lake inflow blend, rim erosion

#### Lake Configuration
- Min/max depth, shelf depth
- Spawn weight bias, variance weight
- Outflow stability, wetland buffer
- Shoreline blend, river suppression

## Performance Considerations

### Current Performance Characteristics
- **Coordinate Complexity**: O(n²) per chunk for 2D operations
- **Noise Generation**: Multiple Simplex/Perlin noise calls per pixel
- **Memory Usage**: Multiple float arrays per chunk
- **Iteration Count**: Multiple smoothing/stability iterations

### Optimization Opportunities
1. **GPU Acceleration**
   - Noise generation on GPU
   - Parallel smoothing operations
   - Vectorized calculations

2. **Algorithm Optimization**
   - Reduced iteration counts
   - Early termination conditions
   - Spatial partitioning

3. **Memory Optimization**
   - Array pooling
   - In-place operations where possible
   - Reduced temporary allocations

## Integration Points

### World Generation Pipeline
1. Base terrain generation
2. Height map creation
3. Hydrology mask generation
4. Flow accumulation calculation
5. Erosion risk field construction
6. River mask generation
7. Lake mask generation
8. Cave mask generation
9. Final terrain assembly

### Client-Server Synchronization
- Hydrology signature in map control profiles
- Profile version validation
- Real-time terrain preview updates
- Consistent random seed usage

## Testing Recommendations

### Unit Tests
- Individual algorithm validation
- Edge case handling
- Parameter boundary testing
- Configuration validation

### Integration Tests
- Full pipeline execution
- Chunk boundary handling
- Client-server synchronization
- Performance profiling

### Visual Tests
- Terrain quality assessment
- Cave system connectivity
- River flow naturalness
- Lake shape realism

## Conclusion

The terrain generation algorithms implemented in the Minecraft server demonstrate a high level of sophistication with:

1. **Comprehensive Hydrology Integration**: All algorithms properly integrate hydrology, flow, and erosion data
2. **Advanced Edge Handling**: Seamless chunk transitions through sophisticated edge processing
3. **Natural Terrain Features**: Multi-layered noise and advanced processing create realistic terrain
4. **Data-Driven Configuration**: Flexible configuration system allows easy tuning
5. **Stability Focus**: Multiple stability mechanisms ensure terrain integrity

The algorithms are production-ready with opportunities for performance optimization and additional feature integration.

## References
- `GameServer/World/Generation/ImprovedTerrainCoordinator.cs`
- `GameServer/World/Generation/ImprovedCaveGenerator.cs`
- `GameServer/World/Generation/ImprovedRiverGenerator.cs`
- `GameServer/World/Generation/ImprovedLakeGenerator.cs`
- `config/minecraft_feature_client_server_core_content_util_2026-01-26.json`

