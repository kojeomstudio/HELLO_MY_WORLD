# Terrain Generation Improvements Design Document

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Design document for terrain generation algorithm improvements

---

## Overview

This document outlines improvements to the terrain generation system, specifically focusing on caves, rivers, and lakes. The current implementation provides a solid foundation with hydrology-aware generation, seam stabilization, and edge smoothing. The proposed improvements will add more natural variation, depth-based features, and additional content generation.

---

## Current Implementation Status

### Existing Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| ImprovedCaveGenerator | `GameServer/World/Generation/ImprovedCaveGenerator.cs` | ✅ Complete | 3D simplex noise caves with hydrology awareness |
| ImprovedRiverGenerator | `GameServer/World/Generation/ImprovedRiverGenerator.cs` | ✅ Complete | Hydrology-driven rivers with seam feathering |
| ImprovedLakeGenerator | `GameServer/World/Generation/ImprovedLakeGenerator.cs` | ✅ Complete | Lake basin masks with flow blending |
| ImprovedTerrainCoordinator | `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` | ✅ Complete | Hydrology-aware terrain mask coordination |
| SimplexNoise | `GameServer/Utils/SimplexNoise.cs` | ✅ Complete | 2D/3D simplex noise generation |
| PerlinNoise | `GameServer/Utils/Noise.cs` | ✅ Complete | 2D perlin noise generation |

### Current Features

- **Caves**: 3D simplex noise, hydrology awareness, river suppression, seam stabilization, support pillars
- **Rivers**: Hydrology-driven generation, seam feathering, flow-aware width modulation
- **Lakes**: Hydrology blending, flow integration, river suppression, wetland buffer
- **Coordination**: Flow accumulation, edge smoothing, stability checks, normalization

---

## Proposed Improvements

### 1. Enhanced Cave Generation

#### 1.1 Multi-Layered Cave Systems

**Current Limitation**: Caves use single-layer 3D simplex noise, resulting in somewhat uniform cave networks.

**Improvement**: Implement multi-layered cave generation using different noise frequencies and amplitudes.

```csharp
// Proposed method in ImprovedCaveGenerator
private float GenerateMultiLayeredCaveNoise(double x, double y, double z, int seed)
{
    // Layer 1: Large cave systems (low frequency)
    double layer1 = SimplexNoise.Generate(x, y, z, 0.01, 2, 0.5, 2.0, seed);
    
    // Layer 2: Medium cave systems (medium frequency)
    double layer2 = SimplexNoise.Generate(x, y, z, 0.03, 3, 0.5, 2.0, seed + 100);
    
    // Layer 3: Small cave tunnels (high frequency)
    double layer3 = SimplexNoise.Generate(x, y, z, 0.05, 2, 0.5, 2.0, seed + 200);
    
    // Blend layers with depth-based weighting
    double depthFactor = y / WorldHeight;
    double weight1 = 1.0 - depthFactor * 0.3;
    double weight2 = 0.5 + depthFactor * 0.2;
    double weight3 = depthFactor * 0.5;
    
    return layer1 * weight1 + layer2 * weight2 + layer3 * weight3;
}
```

#### 1.2 Depth-Based Cave Size Variation

**Current Limitation**: Cave size is relatively uniform across all depths.

**Improvement**: Vary cave size based on depth, with larger caves at lower depths.

```csharp
// Configuration in enhanced_terrain_generation.json
{
  "caveGeneration": {
    "depthLayers": [
      {
        "minDepth": 0,
        "maxDepth": 32,
        "caveSizeMultiplier": 0.5,
        "caveFrequencyMultiplier": 1.2
      },
      {
        "minDepth": 32,
        "maxDepth": 64,
        "caveSizeMultiplier": 0.8,
        "caveFrequencyMultiplier": 1.0
      },
      {
        "minDepth": 64,
        "maxDepth": 128,
        "caveSizeMultiplier": 1.2,
        "caveFrequencyMultiplier": 0.8
      }
    ]
  }
}
```

#### 1.3 Lava Lake Generation

**Current Limitation**: No lava lakes at cave bottoms.

**Improvement**: Generate lava lakes at the bottom of cave systems in deep areas.

```csharp
// New method in ImprovedCaveGenerator
private void GenerateLavaLakes(float[,,] caveMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                // Check if this is the bottom of a cave
                if (IsCaveBottom(caveMask, x, y, z))
                {
                    // Check depth and random chance
                    double depth = WorldHeight - y;
                    if (depth > 50 && ShouldGenerateLavaLake(x, y, z, seed))
                    {
                        // Fill area with lava
                        FillLavaLake(caveMask, x, y, z, seed);
                    }
                }
            }
        }
    }
}

private bool IsCaveBottom(float[,,] caveMask, int x, int y, int z)
{
    // Current block is cave (air)
    if (caveMask[x, y, z] > 0.5f) return false;
    
    // Block below is solid
    if (y > 0 && caveMask[x, y - 1, z] <= 0.5f) return true;
    
    return false;
}
```

#### 1.4 Ore Vein Generation in Caves

**Current Limitation**: No ore generation in cave systems.

**Improvement**: Generate ore veins along cave walls and floors.

```csharp
// New method in ImprovedCaveGenerator
private void GenerateCaveOres(float[,,] caveMask, int chunkX, int chunkZ, int seed)
{
    var oreConfig = LoadOreConfig();
    
    foreach (var ore in oreConfig.ores)
    {
        for (int vein = 0; vein < ore.veinsPerChunk; vein++)
        {
            // Find random cave location
            var pos = FindRandomCavePosition(caveMask, seed + vein);
            
            if (pos.HasValue)
            {
                // Generate ore vein
                GenerateOreVein(caveMask, pos.Value, ore, seed + vein * 1000);
            }
        }
    }
}

private void GenerateOreVein(float[,,] caveMask, Vector3Int startPos, OreConfig ore, int seed)
{
    int veinLength = Random.Range(ore.minVeinLength, ore.maxVeinLength);
    Vector3Int direction = GetRandomDirection();
    
    for (int i = 0; i < veinLength; i++)
    {
        Vector3Int pos = startPos + direction * i;
        
        if (IsValidCavePosition(caveMask, pos))
        {
            // Place ore block
            caveMask[pos.x, pos.y, pos.z] = ore.blockId;
        }
        else
        {
            break;
        }
    }
}
```

### 2. Enhanced River Generation

#### 2.1 River Width Variation

**Current Limitation**: River width is relatively uniform.

**Improvement**: Implement width variation based on flow accumulation and noise.

```csharp
// Enhanced method in ImprovedRiverGenerator
private float GetRiverWidth(double flowAccumulation, double x, double z, int seed)
{
    // Base width from flow accumulation
    double baseWidth = Math.Sqrt(flowAccumulation) * _config.baseWidthMultiplier;
    
    // Add noise-based variation
    double widthVariation = SimplexNoise.Generate(x, z, 0.02, 2, 0.5, 2.0, seed) * 
                          _config.widthVariationAmplitude;
    
    // Apply meandering factor
    double meanderFactor = CalculateMeanderFactor(x, z, seed);
    double meanderedWidth = baseWidth * (1.0 + meanderFactor * _config.meanderWidthMultiplier);
    
    return (float)(meanderedWidth + widthVariation);
}

private double CalculateMeanderFactor(double x, double z, int seed)
{
    // Use domain warping to create meandering effect
    var (dx, dz) = SimplexNoise.DomainWarp(
        x, z, 
        0.01, 0.005, 
        10.0, 5.0, 
        seed
    );
    
    // Calculate meander intensity
    return Math.Sqrt(dx * dx + dz * dz);
}
```

#### 2.2 River Meandering

**Current Limitation**: Rivers follow relatively straight paths.

**Improvement**: Implement proper meandering using Perlin noise flow fields.

```csharp
// New method in ImprovedRiverGenerator
private Vector2 CalculateRiverFlowDirection(double x, double z, int seed)
{
    // Use Perlin noise for flow direction
    double angle = PerlinNoise.Generate(x, z, 0.005, 3, 0.5, seed) * Math.PI * 2;
    
    // Add meandering variation
    double meanderAngle = SimplexNoise.Generate(x, z, 0.02, 2, 0.5, seed + 1000) * Math.PI * 0.5;
    
    double finalAngle = angle + meanderAngle;
    
    return new Vector2(
        Math.Cos(finalAngle),
        Math.Sin(finalAngle)
    );
}
```

#### 2.3 River Bank Generation

**Current Limitation**: River banks are basic transitions.

**Improvement**: Generate varied river banks with sand, gravel, and clay.

```csharp
// New method in ImprovedRiverGenerator
private void GenerateRiverBanks(float[,] riverMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float riverValue = riverMask[x, z];
            
            if (riverValue > 0.1f && riverValue < 0.5f)
            {
                // This is a river bank area
                double bankNoise = SimplexNoise.Generate(x, z, 0.1, 2, 0.5, seed + 5000);
                
                int blockType;
                if (bankNoise < 0.3f)
                    blockType = BlockType.Sand;
                else if (bankNoise < 0.6f)
                    blockType = BlockType.Gravel;
                else
                    blockType = BlockType.Clay;
                
                SetBankBlock(x, z, blockType);
            }
        }
    }
}
```

### 3. Enhanced Lake Generation

#### 3.1 Lake Depth Variation

**Current Limitation**: Lakes have uniform depth.

**Improvement**: Implement depth variation based on lake size and noise.

```csharp
// Enhanced method in ImprovedLakeGenerator
private float GetLakeDepth(double lakeSize, double x, double z, int seed)
{
    // Base depth from lake size
    double baseDepth = Math.Sqrt(lakeSize) * _config.depthSizeMultiplier;
    
    // Add noise-based depth variation
    double depthVariation = SimplexNoise.Generate(x, z, 0.05, 2, 0.5, seed) * 
                          _config.depthVariationAmplitude;
    
    // Ensure minimum depth
    double finalDepth = Math.Max(baseDepth + depthVariation, _config.minLakeDepth);
    
    return (float)Math.Min(finalDepth, _config.maxLakeDepth);
}
```

#### 3.2 Lake Shore Generation

**Current Limitation**: Lake shores are simple transitions.

**Improvement**: Generate varied shorelines with beaches and vegetation.

```csharp
// New method in ImprovedLakeGenerator
private void GenerateLakeShores(float[,] lakeMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float lakeValue = lakeMask[x, z];
            
            if (lakeValue > 0.1f && lakeValue < 0.4f)
            {
                // This is a shore area
                double shoreNoise = SimplexNoise.Generate(x, z, 0.1, 2, 0.5, seed + 6000);
                
                int blockType;
                if (shoreNoise < 0.4f)
                    blockType = BlockType.Sand; // Beach
                else if (shoreNoise < 0.7f)
                    blockType = BlockType.Grass; // Vegetated shore
                else
                    blockType = BlockType.Dirt; // Natural shore
                
                SetShoreBlock(x, z, blockType);
            }
        }
    }
}
```

#### 3.3 Underwater Features

**Current Limitation**: Lake bottoms are uniform.

**Improvement**: Generate underwater features like sand, gravel, and aquatic vegetation.

```csharp
// New method in ImprovedLakeGenerator
private void GenerateUnderwaterFeatures(float[,] lakeMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float lakeValue = lakeMask[x, z];
            
            if (lakeValue > 0.6f)
            {
                // This is underwater
                double featureNoise = SimplexNoise.Generate(x, z, 0.08, 2, 0.5, seed + 7000);
                
                int blockType;
                if (featureNoise < 0.3f)
                    blockType = BlockType.Sand;
                else if (featureNoise < 0.6f)
                    blockType = BlockType.Gravel;
                else
                    blockType = BlockType.Dirt;
                
                SetUnderwaterBlock(x, z, blockType);
                
                // Chance for aquatic vegetation
                if (ShouldGenerateSeagrass(x, z, seed))
                {
                    GenerateSeagrass(x, z, seed);
                }
            }
        }
    }
}
```

### 4. Underground River Systems

**New Feature**: Generate underground rivers that flow through cave systems.

```csharp
// New class: UndergroundRiverGenerator.cs
public class UndergroundRiverGenerator
{
    private UndergroundRiverConfig _config;
    
    public float[,,] Generate(int chunkX, int chunkZ, int seed)
    {
        var mask = new float[ChunkSize, WorldHeight, ChunkSize];
        
        // Generate underground river paths using flow fields
        var flowField = GenerateUndergroundFlowField(chunkX, chunkZ, seed);
        
        // Carve rivers through cave systems
        CarveUndergroundRivers(mask, flowField, seed);
        
        return mask;
    }
    
    private Vector3[,,] GenerateUndergroundFlowField(int chunkX, int chunkZ, int seed)
    {
        var flowField = new Vector3[ChunkSize, WorldHeight, ChunkSize];
        
        for (int y = 0; y < WorldHeight; y++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    // Calculate flow direction based on gradient
                    double gradientX = CalculateGradientX(x, y, z, seed);
                    double gradientY = CalculateGradientY(x, y, z, seed);
                    double gradientZ = CalculateGradientZ(x, y, z, seed);
                    
                    // Flow towards lower ground
                    flowField[x, y, z] = new Vector3(
                        -gradientX,
                        -gradientY,
                        -gradientZ
                    ).normalized;
                }
            }
        }
        
        return flowField;
    }
    
    private void CarveUndergroundRivers(float[,,] mask, Vector3[,,] flowField, int seed)
    {
        // Trace flow paths and carve tunnels
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                // Start from surface and follow flow down
                TraceUndergroundRiverPath(mask, flowField, x, z, seed);
            }
        }
    }
}
```

---

## Configuration Updates

### Enhanced Terrain Configuration

```json
{
  "terrainGeneration": {
    "seed": 0,
    "worldHeight": 128,
    "chunkSize": 16,
    
    "caveGeneration": {
      "enabled": true,
      "caveFrequency": 0.02,
      "caveSize": 0.5,
      "caveThreshold": 0.5,
      "depthLayers": [
        {
          "minDepth": 0,
          "maxDepth": 32,
          "caveSizeMultiplier": 0.5,
          "caveFrequencyMultiplier": 1.2
        },
        {
          "minDepth": 32,
          "maxDepth": 64,
          "caveSizeMultiplier": 0.8,
          "caveFrequencyMultiplier": 1.0
        },
        {
          "minDepth": 64,
          "maxDepth": 128,
          "caveSizeMultiplier": 1.2,
          "caveFrequencyMultiplier": 0.8
        }
      ],
      "multiLayeredCaves": true,
      "lavaLakes": {
        "enabled": true,
        "minDepth": 50,
        "chance": 0.05,
        "minSize": 3,
        "maxSize": 8
      },
      "oreGeneration": {
        "enabled": true,
        "ores": [
          {
            "blockId": 14,
            "minDepth": 5,
            "maxDepth": 60,
            "minVeinLength": 3,
            "maxVeinLength": 8,
            "veinsPerChunk": 2,
            "rarity": 0.02
          },
          {
            "blockId": 15,
            "minDepth": 10,
            "maxDepth": 128,
            "minVeinLength": 4,
            "maxVeinLength": 10,
            "veinsPerChunk": 1,
            "rarity": 0.01
          }
        ]
      }
    },
    
    "riverGeneration": {
      "enabled": true,
      "baseWidthMultiplier": 1.0,
      "widthVariationAmplitude": 2.0,
      "meanderWidthMultiplier": 0.5,
      "bankGeneration": {
        "enabled": true,
        "sandChance": 0.3,
        "gravelChance": 0.3,
        "clayChance": 0.4
      }
    },
    
    "lakeGeneration": {
      "enabled": true,
      "depthSizeMultiplier": 0.5,
      "depthVariationAmplitude": 2.0,
      "minLakeDepth": 2,
      "maxLakeDepth": 20,
      "shoreGeneration": {
        "enabled": true,
        "sandChance": 0.4,
        "grassChance": 0.3,
        "dirtChance": 0.3
      },
      "underwaterFeatures": {
        "enabled": true,
        "sandChance": 0.3,
        "gravelChance": 0.3,
        "dirtChance": 0.4,
        "seagrassChance": 0.1
      }
    },
    
    "undergroundRiverGeneration": {
      "enabled": true,
      "frequency": 0.01,
      "width": 2,
      "waterChance": 0.8,
      "lavaChance": 0.2
    }
  }
}
```

---

## Implementation Plan

### Phase 1: Cave Improvements (High Priority)
1. Implement multi-layered cave generation
2. Add depth-based cave size variation
3. Implement lava lake generation
4. Add ore vein generation in caves

### Phase 2: River Improvements (High Priority)
1. Implement river width variation
2. Add river meandering
3. Generate varied river banks

### Phase 3: Lake Improvements (Medium Priority)
1. Implement lake depth variation
2. Generate varied shorelines
3. Add underwater features

### Phase 4: Underground Rivers (Medium Priority)
1. Implement UndergroundRiverGenerator
2. Generate underground flow fields
3. Carve underground river tunnels

### Phase 5: Integration and Testing (High Priority)
1. Update ImprovedTerrainCoordinator to use new generators
2. Update configuration files
3. Test terrain generation with new features
4. Performance optimization

---

## Testing Strategy

### Unit Tests
- Test multi-layered cave noise generation
- Test depth-based cave size variation
- Test lava lake generation logic
- Test ore vein generation algorithms
- Test river width calculation
- Test river meandering
- Test lake depth variation

### Integration Tests
- Test complete terrain generation pipeline
- Test interaction between caves, rivers, and lakes
- Test ore distribution in generated terrain
- Test underground river integration

### Performance Tests
- Measure terrain generation time with new features
- Profile memory usage during generation
- Test generation of large worlds
- Optimize hotspots identified

---

## Notes

- All improvements should maintain backward compatibility with existing world saves
- Configuration changes should be optional with sensible defaults
- Performance impact should be minimal through optimization
- Visual quality should be significantly improved
- Natural variation should be increased across all terrain features

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Design document for terrain generation algorithm improvements

---

## Overview

This document outlines improvements to the terrain generation system, specifically focusing on caves, rivers, and lakes. The current implementation provides a solid foundation with hydrology-aware generation, seam stabilization, and edge smoothing. The proposed improvements will add more natural variation, depth-based features, and additional content generation.

---

## Current Implementation Status

### Existing Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| ImprovedCaveGenerator | `GameServer/World/Generation/ImprovedCaveGenerator.cs` | ✅ Complete | 3D simplex noise caves with hydrology awareness |
| ImprovedRiverGenerator | `GameServer/World/Generation/ImprovedRiverGenerator.cs` | ✅ Complete | Hydrology-driven rivers with seam feathering |
| ImprovedLakeGenerator | `GameServer/World/Generation/ImprovedLakeGenerator.cs` | ✅ Complete | Lake basin masks with flow blending |
| ImprovedTerrainCoordinator | `GameServer/World/Generation/ImprovedTerrainCoordinator.cs` | ✅ Complete | Hydrology-aware terrain mask coordination |
| SimplexNoise | `GameServer/Utils/SimplexNoise.cs` | ✅ Complete | 2D/3D simplex noise generation |
| PerlinNoise | `GameServer/Utils/Noise.cs` | ✅ Complete | 2D perlin noise generation |

### Current Features

- **Caves**: 3D simplex noise, hydrology awareness, river suppression, seam stabilization, support pillars
- **Rivers**: Hydrology-driven generation, seam feathering, flow-aware width modulation
- **Lakes**: Hydrology blending, flow integration, river suppression, wetland buffer
- **Coordination**: Flow accumulation, edge smoothing, stability checks, normalization

---

## Proposed Improvements

### 1. Enhanced Cave Generation

#### 1.1 Multi-Layered Cave Systems

**Current Limitation**: Caves use single-layer 3D simplex noise, resulting in somewhat uniform cave networks.

**Improvement**: Implement multi-layered cave generation using different noise frequencies and amplitudes.

```csharp
// Proposed method in ImprovedCaveGenerator
private float GenerateMultiLayeredCaveNoise(double x, double y, double z, int seed)
{
    // Layer 1: Large cave systems (low frequency)
    double layer1 = SimplexNoise.Generate(x, y, z, 0.01, 2, 0.5, 2.0, seed);
    
    // Layer 2: Medium cave systems (medium frequency)
    double layer2 = SimplexNoise.Generate(x, y, z, 0.03, 3, 0.5, 2.0, seed + 100);
    
    // Layer 3: Small cave tunnels (high frequency)
    double layer3 = SimplexNoise.Generate(x, y, z, 0.05, 2, 0.5, 2.0, seed + 200);
    
    // Blend layers with depth-based weighting
    double depthFactor = y / WorldHeight;
    double weight1 = 1.0 - depthFactor * 0.3;
    double weight2 = 0.5 + depthFactor * 0.2;
    double weight3 = depthFactor * 0.5;
    
    return layer1 * weight1 + layer2 * weight2 + layer3 * weight3;
}
```

#### 1.2 Depth-Based Cave Size Variation

**Current Limitation**: Cave size is relatively uniform across all depths.

**Improvement**: Vary cave size based on depth, with larger caves at lower depths.

```csharp
// Configuration in enhanced_terrain_generation.json
{
  "caveGeneration": {
    "depthLayers": [
      {
        "minDepth": 0,
        "maxDepth": 32,
        "caveSizeMultiplier": 0.5,
        "caveFrequencyMultiplier": 1.2
      },
      {
        "minDepth": 32,
        "maxDepth": 64,
        "caveSizeMultiplier": 0.8,
        "caveFrequencyMultiplier": 1.0
      },
      {
        "minDepth": 64,
        "maxDepth": 128,
        "caveSizeMultiplier": 1.2,
        "caveFrequencyMultiplier": 0.8
      }
    ]
  }
}
```

#### 1.3 Lava Lake Generation

**Current Limitation**: No lava lakes at cave bottoms.

**Improvement**: Generate lava lakes at the bottom of cave systems in deep areas.

```csharp
// New method in ImprovedCaveGenerator
private void GenerateLavaLakes(float[,,] caveMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                // Check if this is the bottom of a cave
                if (IsCaveBottom(caveMask, x, y, z))
                {
                    // Check depth and random chance
                    double depth = WorldHeight - y;
                    if (depth > 50 && ShouldGenerateLavaLake(x, y, z, seed))
                    {
                        // Fill area with lava
                        FillLavaLake(caveMask, x, y, z, seed);
                    }
                }
            }
        }
    }
}

private bool IsCaveBottom(float[,,] caveMask, int x, int y, int z)
{
    // Current block is cave (air)
    if (caveMask[x, y, z] > 0.5f) return false;
    
    // Block below is solid
    if (y > 0 && caveMask[x, y - 1, z] <= 0.5f) return true;
    
    return false;
}
```

#### 1.4 Ore Vein Generation in Caves

**Current Limitation**: No ore generation in cave systems.

**Improvement**: Generate ore veins along cave walls and floors.

```csharp
// New method in ImprovedCaveGenerator
private void GenerateCaveOres(float[,,] caveMask, int chunkX, int chunkZ, int seed)
{
    var oreConfig = LoadOreConfig();
    
    foreach (var ore in oreConfig.ores)
    {
        for (int vein = 0; vein < ore.veinsPerChunk; vein++)
        {
            // Find random cave location
            var pos = FindRandomCavePosition(caveMask, seed + vein);
            
            if (pos.HasValue)
            {
                // Generate ore vein
                GenerateOreVein(caveMask, pos.Value, ore, seed + vein * 1000);
            }
        }
    }
}

private void GenerateOreVein(float[,,] caveMask, Vector3Int startPos, OreConfig ore, int seed)
{
    int veinLength = Random.Range(ore.minVeinLength, ore.maxVeinLength);
    Vector3Int direction = GetRandomDirection();
    
    for (int i = 0; i < veinLength; i++)
    {
        Vector3Int pos = startPos + direction * i;
        
        if (IsValidCavePosition(caveMask, pos))
        {
            // Place ore block
            caveMask[pos.x, pos.y, pos.z] = ore.blockId;
        }
        else
        {
            break;
        }
    }
}
```

### 2. Enhanced River Generation

#### 2.1 River Width Variation

**Current Limitation**: River width is relatively uniform.

**Improvement**: Implement width variation based on flow accumulation and noise.

```csharp
// Enhanced method in ImprovedRiverGenerator
private float GetRiverWidth(double flowAccumulation, double x, double z, int seed)
{
    // Base width from flow accumulation
    double baseWidth = Math.Sqrt(flowAccumulation) * _config.baseWidthMultiplier;
    
    // Add noise-based variation
    double widthVariation = SimplexNoise.Generate(x, z, 0.02, 2, 0.5, 2.0, seed) * 
                          _config.widthVariationAmplitude;
    
    // Apply meandering factor
    double meanderFactor = CalculateMeanderFactor(x, z, seed);
    double meanderedWidth = baseWidth * (1.0 + meanderFactor * _config.meanderWidthMultiplier);
    
    return (float)(meanderedWidth + widthVariation);
}

private double CalculateMeanderFactor(double x, double z, int seed)
{
    // Use domain warping to create meandering effect
    var (dx, dz) = SimplexNoise.DomainWarp(
        x, z, 
        0.01, 0.005, 
        10.0, 5.0, 
        seed
    );
    
    // Calculate meander intensity
    return Math.Sqrt(dx * dx + dz * dz);
}
```

#### 2.2 River Meandering

**Current Limitation**: Rivers follow relatively straight paths.

**Improvement**: Implement proper meandering using Perlin noise flow fields.

```csharp
// New method in ImprovedRiverGenerator
private Vector2 CalculateRiverFlowDirection(double x, double z, int seed)
{
    // Use Perlin noise for flow direction
    double angle = PerlinNoise.Generate(x, z, 0.005, 3, 0.5, seed) * Math.PI * 2;
    
    // Add meandering variation
    double meanderAngle = SimplexNoise.Generate(x, z, 0.02, 2, 0.5, seed + 1000) * Math.PI * 0.5;
    
    double finalAngle = angle + meanderAngle;
    
    return new Vector2(
        Math.Cos(finalAngle),
        Math.Sin(finalAngle)
    );
}
```

#### 2.3 River Bank Generation

**Current Limitation**: River banks are basic transitions.

**Improvement**: Generate varied river banks with sand, gravel, and clay.

```csharp
// New method in ImprovedRiverGenerator
private void GenerateRiverBanks(float[,] riverMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float riverValue = riverMask[x, z];
            
            if (riverValue > 0.1f && riverValue < 0.5f)
            {
                // This is a river bank area
                double bankNoise = SimplexNoise.Generate(x, z, 0.1, 2, 0.5, seed + 5000);
                
                int blockType;
                if (bankNoise < 0.3f)
                    blockType = BlockType.Sand;
                else if (bankNoise < 0.6f)
                    blockType = BlockType.Gravel;
                else
                    blockType = BlockType.Clay;
                
                SetBankBlock(x, z, blockType);
            }
        }
    }
}
```

### 3. Enhanced Lake Generation

#### 3.1 Lake Depth Variation

**Current Limitation**: Lakes have uniform depth.

**Improvement**: Implement depth variation based on lake size and noise.

```csharp
// Enhanced method in ImprovedLakeGenerator
private float GetLakeDepth(double lakeSize, double x, double z, int seed)
{
    // Base depth from lake size
    double baseDepth = Math.Sqrt(lakeSize) * _config.depthSizeMultiplier;
    
    // Add noise-based depth variation
    double depthVariation = SimplexNoise.Generate(x, z, 0.05, 2, 0.5, seed) * 
                          _config.depthVariationAmplitude;
    
    // Ensure minimum depth
    double finalDepth = Math.Max(baseDepth + depthVariation, _config.minLakeDepth);
    
    return (float)Math.Min(finalDepth, _config.maxLakeDepth);
}
```

#### 3.2 Lake Shore Generation

**Current Limitation**: Lake shores are simple transitions.

**Improvement**: Generate varied shorelines with beaches and vegetation.

```csharp
// New method in ImprovedLakeGenerator
private void GenerateLakeShores(float[,] lakeMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float lakeValue = lakeMask[x, z];
            
            if (lakeValue > 0.1f && lakeValue < 0.4f)
            {
                // This is a shore area
                double shoreNoise = SimplexNoise.Generate(x, z, 0.1, 2, 0.5, seed + 6000);
                
                int blockType;
                if (shoreNoise < 0.4f)
                    blockType = BlockType.Sand; // Beach
                else if (shoreNoise < 0.7f)
                    blockType = BlockType.Grass; // Vegetated shore
                else
                    blockType = BlockType.Dirt; // Natural shore
                
                SetShoreBlock(x, z, blockType);
            }
        }
    }
}
```

#### 3.3 Underwater Features

**Current Limitation**: Lake bottoms are uniform.

**Improvement**: Generate underwater features like sand, gravel, and aquatic vegetation.

```csharp
// New method in ImprovedLakeGenerator
private void GenerateUnderwaterFeatures(float[,] lakeMask, int chunkX, int chunkZ, int seed)
{
    for (int x = 0; x < ChunkSize; x++)
    {
        for (int z = 0; z < ChunkSize; z++)
        {
            float lakeValue = lakeMask[x, z];
            
            if (lakeValue > 0.6f)
            {
                // This is underwater
                double featureNoise = SimplexNoise.Generate(x, z, 0.08, 2, 0.5, seed + 7000);
                
                int blockType;
                if (featureNoise < 0.3f)
                    blockType = BlockType.Sand;
                else if (featureNoise < 0.6f)
                    blockType = BlockType.Gravel;
                else
                    blockType = BlockType.Dirt;
                
                SetUnderwaterBlock(x, z, blockType);
                
                // Chance for aquatic vegetation
                if (ShouldGenerateSeagrass(x, z, seed))
                {
                    GenerateSeagrass(x, z, seed);
                }
            }
        }
    }
}
```

### 4. Underground River Systems

**New Feature**: Generate underground rivers that flow through cave systems.

```csharp
// New class: UndergroundRiverGenerator.cs
public class UndergroundRiverGenerator
{
    private UndergroundRiverConfig _config;
    
    public float[,,] Generate(int chunkX, int chunkZ, int seed)
    {
        var mask = new float[ChunkSize, WorldHeight, ChunkSize];
        
        // Generate underground river paths using flow fields
        var flowField = GenerateUndergroundFlowField(chunkX, chunkZ, seed);
        
        // Carve rivers through cave systems
        CarveUndergroundRivers(mask, flowField, seed);
        
        return mask;
    }
    
    private Vector3[,,] GenerateUndergroundFlowField(int chunkX, int chunkZ, int seed)
    {
        var flowField = new Vector3[ChunkSize, WorldHeight, ChunkSize];
        
        for (int y = 0; y < WorldHeight; y++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    // Calculate flow direction based on gradient
                    double gradientX = CalculateGradientX(x, y, z, seed);
                    double gradientY = CalculateGradientY(x, y, z, seed);
                    double gradientZ = CalculateGradientZ(x, y, z, seed);
                    
                    // Flow towards lower ground
                    flowField[x, y, z] = new Vector3(
                        -gradientX,
                        -gradientY,
                        -gradientZ
                    ).normalized;
                }
            }
        }
        
        return flowField;
    }
    
    private void CarveUndergroundRivers(float[,,] mask, Vector3[,,] flowField, int seed)
    {
        // Trace flow paths and carve tunnels
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                // Start from surface and follow flow down
                TraceUndergroundRiverPath(mask, flowField, x, z, seed);
            }
        }
    }
}
```

---

## Configuration Updates

### Enhanced Terrain Configuration

```json
{
  "terrainGeneration": {
    "seed": 0,
    "worldHeight": 128,
    "chunkSize": 16,
    
    "caveGeneration": {
      "enabled": true,
      "caveFrequency": 0.02,
      "caveSize": 0.5,
      "caveThreshold": 0.5,
      "depthLayers": [
        {
          "minDepth": 0,
          "maxDepth": 32,
          "caveSizeMultiplier": 0.5,
          "caveFrequencyMultiplier": 1.2
        },
        {
          "minDepth": 32,
          "maxDepth": 64,
          "caveSizeMultiplier": 0.8,
          "caveFrequencyMultiplier": 1.0
        },
        {
          "minDepth": 64,
          "maxDepth": 128,
          "caveSizeMultiplier": 1.2,
          "caveFrequencyMultiplier": 0.8
        }
      ],
      "multiLayeredCaves": true,
      "lavaLakes": {
        "enabled": true,
        "minDepth": 50,
        "chance": 0.05,
        "minSize": 3,
        "maxSize": 8
      },
      "oreGeneration": {
        "enabled": true,
        "ores": [
          {
            "blockId": 14,
            "minDepth": 5,
            "maxDepth": 60,
            "minVeinLength": 3,
            "maxVeinLength": 8,
            "veinsPerChunk": 2,
            "rarity": 0.02
          },
          {
            "blockId": 15,
            "minDepth": 10,
            "maxDepth": 128,
            "minVeinLength": 4,
            "maxVeinLength": 10,
            "veinsPerChunk": 1,
            "rarity": 0.01
          }
        ]
      }
    },
    
    "riverGeneration": {
      "enabled": true,
      "baseWidthMultiplier": 1.0,
      "widthVariationAmplitude": 2.0,
      "meanderWidthMultiplier": 0.5,
      "bankGeneration": {
        "enabled": true,
        "sandChance": 0.3,
        "gravelChance": 0.3,
        "clayChance": 0.4
      }
    },
    
    "lakeGeneration": {
      "enabled": true,
      "depthSizeMultiplier": 0.5,
      "depthVariationAmplitude": 2.0,
      "minLakeDepth": 2,
      "maxLakeDepth": 20,
      "shoreGeneration": {
        "enabled": true,
        "sandChance": 0.4,
        "grassChance": 0.3,
        "dirtChance": 0.3
      },
      "underwaterFeatures": {
        "enabled": true,
        "sandChance": 0.3,
        "gravelChance": 0.3,
        "dirtChance": 0.4,
        "seagrassChance": 0.1
      }
    },
    
    "undergroundRiverGeneration": {
      "enabled": true,
      "frequency": 0.01,
      "width": 2,
      "waterChance": 0.8,
      "lavaChance": 0.2
    }
  }
}
```

---

## Implementation Plan

### Phase 1: Cave Improvements (High Priority)
1. Implement multi-layered cave generation
2. Add depth-based cave size variation
3. Implement lava lake generation
4. Add ore vein generation in caves

### Phase 2: River Improvements (High Priority)
1. Implement river width variation
2. Add river meandering
3. Generate varied river banks

### Phase 3: Lake Improvements (Medium Priority)
1. Implement lake depth variation
2. Generate varied shorelines
3. Add underwater features

### Phase 4: Underground Rivers (Medium Priority)
1. Implement UndergroundRiverGenerator
2. Generate underground flow fields
3. Carve underground river tunnels

### Phase 5: Integration and Testing (High Priority)
1. Update ImprovedTerrainCoordinator to use new generators
2. Update configuration files
3. Test terrain generation with new features
4. Performance optimization

---

## Testing Strategy

### Unit Tests
- Test multi-layered cave noise generation
- Test depth-based cave size variation
- Test lava lake generation logic
- Test ore vein generation algorithms
- Test river width calculation
- Test river meandering
- Test lake depth variation

### Integration Tests
- Test complete terrain generation pipeline
- Test interaction between caves, rivers, and lakes
- Test ore distribution in generated terrain
- Test underground river integration

### Performance Tests
- Measure terrain generation time with new features
- Profile memory usage during generation
- Test generation of large worlds
- Optimize hotspots identified

---

## Notes

- All improvements should maintain backward compatibility with existing world saves
- Configuration changes should be optional with sensible defaults
- Performance impact should be minimal through optimization
- Visual quality should be significantly improved
- Natural variation should be increased across all terrain features

