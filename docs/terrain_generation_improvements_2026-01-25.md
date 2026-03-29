# Terrain Generation Improvements - 2026-01-25

## Overview
This document outlines proposed improvements to the terrain generation algorithms for caves, rivers, and lakes based on the comprehensive analysis of the current implementation.

## Cave Generation Improvements

### 1. Cave Connectivity Enhancement

#### Current State
- Caves are generated using noise-based algorithms
- Individual cave systems are not explicitly connected
- Connectivity is incidental through noise overlap

#### Proposed Improvements
```csharp
// Add cave network connectivity system
public class CaveNetworkGenerator
{
    private List<CaveSystem> caveSystems;
    
    public List<CaveSystem> GenerateConnectedNetworks(
        bool[,,] baseCaveMask,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight)
    {
        var systems = new List<CaveSystem>();
        var visited = new bool[chunkSize, worldHeight, chunkSize];
        
        // Flood fill to identify connected cave systems
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 1; y < worldHeight - 1; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (baseCaveMask[x, y, z] && !visited[x, y, z])
                    {
                        var system = FloodFillCaveSystem(baseCaveMask, visited, x, y, z);
                        if (system.Volume > config.MinCaveVolume)
                        {
                            systems.Add(system);
                        }
                    }
                }
            }
        }
        
        // Connect nearby cave systems with tunnels
        ConnectCaveSystems(systems);
        
        return systems;
    }
    
    private CaveSystem FloodFillCaveSystem(bool[,,] mask, bool[,,] visited, int startX, int startY, int startZ)
    {
        var system = new CaveSystem();
        var queue = new Queue<(int x, int y, int z)>();
        queue.Enqueue((startX, startY, startZ));
        visited[startX, startY, startZ] = true;
        
        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            system.AddBlock(x, y, z);
            
            // Check 6-connected neighbors
            var neighbors = new[] {
                (x + 1, y, z), (x - 1, y, z),
                (x, y + 1, z), (x, y - 1, z),
                (x, y, z + 1), (x, y, z - 1)
            };
            
            foreach (var (nx, ny, nz) in neighbors)
            {
                if (nx >= 0 && nx < mask.GetLength(0) &&
                    ny >= 0 && ny < mask.GetLength(1) &&
                    nz >= 0 && nz < mask.GetLength(2) &&
                    mask[nx, ny, nz] && !visited[nx, ny, nz])
                {
                    visited[nx, ny, nz] = true;
                    queue.Enqueue((nx, ny, nz));
                }
            }
        }
        
        return system;
    }
    
    private void ConnectCaveSystems(List<CaveSystem> systems)
    {
        // Find nearby cave systems and connect them with tunnels
        for (int i = 0; i < systems.Count; i++)
        {
            for (int j = i + 1; j < systems.Count; j++)
            {
                double distance = CalculateSystemDistance(systems[i], systems[j]);
                if (distance < config.MaxConnectionDistance)
                {
                    CreateTunnelConnection(systems[i], systems[j]);
                }
            }
        }
    }
}

public class CaveSystem
{
    public List<(int x, int y, int z)> Blocks { get; } = new();
    public int Volume => Blocks.Count;
    public Vector3Int Center { get; private set; }
    
    public void AddBlock(int x, int y, int z)
    {
        Blocks.Add((x, y, z));
        // Update center
    }
}
```

### 2. Dynamic Cave Size Variation

#### Current State
- Cave size controlled by threshold and depth
- Limited dynamic variation based on noise

#### Proposed Improvements
```csharp
// Add biome-based cave size modifiers
public class BiomeCaveModifiers
{
    public Dictionary<string, CaveSizeProfile> Profiles { get; } = new();
    
    public CaveSizeProfile GetProfile(string biomeId)
    {
        return Profiles.TryGetValue(biomeId, out var profile) 
            ? profile 
            : Profiles["default"];
    }
    
    public void LoadFromConfig(string configPath)
    {
        var config = JsonUtility.FromJson<Dictionary<string, CaveSizeProfile>>(configPath);
        foreach (var kvp in config)
        {
            Profiles[kvp.Key] = kvp.Value;
        }
    }
}

public class CaveSizeProfile
{
    public double MinRadius { get; set; } = 2.0;
    public double MaxRadius { get; set; } = 8.0;
    public double HeightMultiplier { get; set; } = 1.0;
    public double DensityMultiplier { get; set; } = 1.0;
    public double ConnectivityBonus { get; set; } = 0.0;
}

// Integrate into ImprovedCaveGenerator
public bool[,,] BuildMask(
    int chunkX, int chunkZ, int chunkSize,
    int worldHeight, int[,] heightMap,
    float[,] hydrologyMask, float[,] flowMask,
    float[,]? riverMask, float[,] erosionRisk,
    int seaLevel, string biomeId)
{
    // Get biome-specific cave profile
    var caveProfile = biomeModifiers.GetProfile(biomeId);
    
    // Apply biome modifiers to cave generation
    double biomeDensity = config.Threshold * caveProfile.DensityMultiplier;
    double biomeHeight = worldHeight * caveProfile.HeightMultiplier;
    
    // Generate caves with biome-aware parameters
    // ... existing cave generation code with biome modifiers
}
```

### 3. Enhanced Ceiling/Floor Shaping

#### Current State
- Basic ceiling/floor shaping
- Limited decoration

#### Proposed Improvements
```csharp
// Add stalactite and stalagmite generation
public class CaveDecorationGenerator
{
    public void GenerateDecorations(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 31 + chunkZ * 17));
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int surface = heightMap[x, z];
                
                // Find cave ceiling and floor
                int? ceilingY = FindCaveCeiling(caveMask, x, z, surface);
                int? floorY = FindCaveFloor(caveMask, x, z, surface);
                
                if (ceilingY.HasValue && floorY.HasValue)
                {
                    // Generate stalactites from ceiling
                    if (random.NextDouble() < config.StalactiteChance)
                    {
                        GenerateStalactite(caveMask, x, z, ceilingY.Value, random);
                    }
                    
                    // Generate stalagmites from floor
                    if (random.NextDouble() < config.StalagmiteChance)
                    {
                        GenerateStalagmite(caveMask, x, z, floorY.Value, random);
                    }
                }
            }
        }
    }
    
    private int? FindCaveCeiling(bool[,,] mask, int x, int z, int surface)
    {
        for (int y = surface - 1; y > 0; y--)
        {
            if (mask[x, y, z])
            {
                return y;
            }
        }
        return null;
    }
    
    private int? FindCaveFloor(bool[,,] mask, int x, int z, int surface)
    {
        for (int y = 1; y < surface; y++)
        {
            if (mask[x, y, z])
            {
                return y;
            }
        }
        return null;
    }
    
    private void GenerateStalactite(bool[,,] mask, int x, int z, int ceilingY, Random random)
    {
        int length = random.Next(config.MinStalactiteLength, config.MaxStalactiteLength);
        
        for (int i = 0; i < length && (ceilingY - i) > 0; i++)
        {
            int y = ceilingY - i;
            if (y < mask.GetLength(1) && !mask[x, y, z])
            {
                mask[x, y, z] = true;
            }
        }
    }
    
    private void GenerateStalagmite(bool[,,] mask, int x, int z, int floorY, Random random)
    {
        int length = random.Next(config.MinStalagmiteLength, config.MaxStalagmiteLength);
        
        for (int i = 0; i < length && (floorY + i) < mask.GetLength(1); i++)
        {
            int y = floorY + i;
            if (!mask[x, y, z])
            {
                mask[x, y, z] = true;
            }
        }
    }
}
```

### 4. Underground Water Bodies

#### Current State
- Proximity-based water table integration
- No explicit underground water bodies

#### Proposed Improvements
```csharp
// Add underground lake generation
public class UndergroundLakeGenerator
{
    public void GenerateUndergroundLakes(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 37 + chunkZ * 23));
        
        // Identify potential underground lake locations
        var lakeCandidates = FindLakeCandidates(caveMask, heightMap, chunkSize, worldHeight, seaLevel);
        
        // Generate lakes at candidate locations
        foreach (var candidate in lakeCandidates)
        {
            if (random.NextDouble() < config.UndergroundLakeChance)
            {
                FillUndergroundLake(caveMask, candidate, random);
            }
        }
    }
    
    private List<LakeCandidate> FindLakeCandidates(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkSize, int worldHeight,
        int seaLevel)
    {
        var candidates = new List<LakeCandidate>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int surface = heightMap[x, z];
                
                // Look for large cave spaces below water table
                for (int y = seaLevel - 1; y > 0; y--)
                {
                    if (caveMask[x, y, z] && IsLargeCaveSpace(caveMask, x, y, z))
                    {
                        candidates.Add(new LakeCandidate { X = x, Y = y, Z = z });
                        break;
                    }
                }
            }
        }
        
        return candidates;
    }
    
    private bool IsLargeCaveSpace(bool[,,] mask, int x, int y, int z)
    {
        // Check if there's enough space for a lake
        int space = 0;
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx >= 0 && nx < mask.GetLength(0) &&
                    nz >= 0 && nz < mask.GetLength(2) &&
                    mask[nx, y, nz])
                {
                    space++;
                }
            }
        }
        
        return space >= config.MinLakeSpace;
    }
    
    private void FillUndergroundLake(bool[,,] mask, LakeCandidate candidate, Random random)
    {
        int depth = random.Next(config.MinLakeDepth, config.MaxLakeDepth);
        int radius = random.Next(config.MinLakeRadius, config.MaxLakeRadius);
        
        // Fill spherical lake volume
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = 0; dy < depth; dy++)
                {
                    int nx = candidate.X + dx;
                    int ny = candidate.Y + dy;
                    int nz = candidate.Z + dz;
                    
                    if (nx >= 0 && nx < mask.GetLength(0) &&
                        ny >= 0 && ny < mask.GetLength(1) &&
                        nz >= 0 && nz < mask.GetLength(2))
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz + dy * dy);
                        if (distance <= radius)
                        {
                            mask[nx, ny, nz] = true; // Fill with water
                        }
                    }
                }
            }
        }
    }
}

public class LakeCandidate
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}
```

## River Generation Improvements

### 1. Natural River Meandering

#### Current State
- Noise-based meandering
- Limited natural river behavior

#### Proposed Improvements
```csharp
// Add meander evolution algorithm
public class RiverMeanderEvolution
{
    public List<RiverNode> EvolveMeander(
        List<RiverNode> initialPath,
        int iterations,
        double meanderStrength,
        double erosionRate)
    {
        var path = new List<RiverNode>(initialPath);
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var newPath = new List<RiverNode>();
            
            for (int i = 0; i < path.Count; i++)
            {
                var current = path[i];
                var prev = i > 0 ? path[i - 1] : null;
                var next = i < path.Count - 1 ? path[i + 1] : null;
                
                // Calculate meander offset
                Vector2 meanderOffset = CalculateMeanderOffset(current, prev, next, meanderStrength);
                
                // Apply erosion
                double erosionFactor = CalculateErosionFactor(current, path, erosionRate);
                
                // Create new node
                var newNode = new RiverNode
                {
                    X = current.X + meanderOffset.X,
                    Z = current.Z + meanderOffset.Y,
                    Width = current.Width * (1.0 + erosionFactor * 0.1),
                    Depth = current.Depth * (1.0 + erosionFactor * 0.05)
                };
                
                newPath.Add(newNode);
            }
            
            path = newPath;
        }
        
        return path;
    }
    
    private Vector2 CalculateMeanderOffset(
        RiverNode current,
        RiverNode? prev,
        RiverNode? next,
        double strength)
    {
        if (prev == null && next == null)
        {
            return Vector2.zero;
        }
        
        Vector2 direction = Vector2.zero;
        if (next != null)
        {
            direction = new Vector2(next.X - current.X, next.Z - current.Z).normalized;
        }
        else if (prev != null)
        {
            direction = new Vector2(current.X - prev.X, current.Z - prev.Z).normalized;
        }
        
        // Perpendicular offset for meandering
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        double meanderAmount = Math.Sin(current.Distance * 0.1) * strength;
        
        return perpendicular * (float)meanderAmount;
    }
    
    private double CalculateErosionFactor(RiverNode node, List<RiverNode> path, double rate)
    {
        // Calculate curvature-based erosion
        double curvature = CalculateCurvature(node, path);
        return curvature * rate;
    }
    
    private double CalculateCurvature(RiverNode node, List<RiverNode> path)
    {
        int index = path.IndexOf(node);
        if (index < 2 || index >= path.Count - 2)
        {
            return 0.0;
        }
        
        var prev = path[index - 2];
        var curr = path[index - 1];
        var next = path[index];
        
        Vector2 v1 = new Vector2(curr.X - prev.X, curr.Z - prev.Z);
        Vector2 v2 = new Vector2(next.X - curr.X, next.Z - curr.Z);
        
        return Vector2.Angle(v1, v2);
    }
}

public class RiverNode
{
    public int X { get; set; }
    public int Z { get; set; }
    public double Width { get; set; }
    public double Depth { get; set; }
    public double Distance { get; set; }
}
```

### 2. Dynamic River Width Variation

#### Current State
- Flow-based width calculations
- Limited dynamic variation

#### Proposed Improvements
```csharp
// Add seasonal and terrain-based width variation
public class RiverWidthModulator
{
    public double CalculateWidth(
        double baseWidth,
        double flow,
        double slope,
        int season,
        int biomeId)
    {
        // Get biome-specific width modifiers
        var biomeMod = GetBiomeWidthModifier(biomeId);
        
        // Apply seasonal variation
        double seasonalMod = GetSeasonalModifier(season);
        
        // Apply slope-based variation (wider on flatter terrain)
        double slopeMod = 1.0 + (1.0 - Math.Clamp(slope / 10.0, 0.0, 1.0)) * 0.5;
        
        // Apply flow-based variation
        double flowMod = 1.0 + Math.Clamp(flow / 6.0, 0.0, 1.0) * 0.3;
        
        return baseWidth * biomeMod * seasonalMod * slopeMod * flowMod;
    }
    
    private double GetBiomeWidthModifier(string biomeId)
    {
        return biomeWidthModifiers.TryGetValue(biomeId, out var modifier)
            ? modifier
            : 1.0;
    }
    
    private double GetSeasonalModifier(int season)
    {
        // Season 0-3: Spring, Summer, Fall, Winter
        double[] seasonalMods = { 1.2, 0.8, 1.0, 1.1 };
        return seasonalMods[season % 4];
    }
}
```

### 3. Erosion-Based River Bank Shaping

#### Current State
- Noise-based river banks
- Limited erosion modeling

#### Proposed Improvements
```csharp
// Add erosion-based bank shaping
public class RiverBankErosion
{
    public void ErodeRiverBanks(
        float[,] riverMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 41 + chunkZ * 29));
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                if (riverStrength > 0.1f)
                {
                    // Erode river banks
                    ErodeBank(heightMap, x, z, riverStrength, random);
                    
                    // Deposit sediment
                    DepositSediment(heightMap, x, z, riverStrength, random);
                }
            }
        }
    }
    
    private void ErodeBank(int[,] heightMap, int x, int z, float riverStrength, Random random)
    {
        // Find river bank direction
        var bankDirection = FindBankDirection(heightMap, x, z);
        
        // Apply erosion based on river strength
        double erosionAmount = riverStrength * config.ErosionRate;
        
        int nx = x + bankDirection.X;
        int nz = z + bankDirection.Z;
        
        if (nx >= 0 && nx < heightMap.GetLength(0) &&
            nz >= 0 && nz < heightMap.GetLength(1))
        {
            // Add some randomness to erosion
            double noise = (random.NextDouble() - 0.5) * config.ErosionNoise;
            heightMap[nx, nz] = (int)(heightMap[nx, nz] - erosionAmount + noise);
        }
    }
    
    private void DepositSediment(int[,] heightMap, int x, int z, float riverStrength, Random random)
    {
        // Find downstream direction
        var downstream = FindDownstreamDirection(heightMap, x, z);
        
        // Deposit sediment based on river strength
        double depositAmount = riverStrength * config.SedimentDepositRate;
        
        int nx = x + downstream.X;
        int nz = z + downstream.Z;
        
        if (nx >= 0 && nx < heightMap.GetLength(0) &&
            nz >= 0 && nz < heightMap.GetLength(1))
        {
            // Add randomness to deposition
            double noise = (random.NextDouble() - 0.5) * config.SedimentNoise;
            heightMap[nx, nz] = (int)(heightMap[nx, nz] + depositAmount + noise);
        }
    }
    
    private Vector2Int FindBankDirection(int[,] heightMap, int x, int z)
    {
        // Find direction of steepest descent from river bank
        int center = heightMap[x, z];
        int bestDrop = 0;
        Vector2Int bestDir = Vector2Int.zero;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                
                int nx = x + dx;
                int nz = z + dz;
                
                if (nx >= 0 && nx < heightMap.GetLength(0) &&
                    nz >= 0 && nz < heightMap.GetLength(1))
                {
                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDir = new Vector2Int(dx, dz);
                    }
                }
            }
        }
        
        return bestDir;
    }
    
    private Vector2Int FindDownstreamDirection(int[,] heightMap, int x, int z)
    {
        // Find direction of steepest descent
        return FindBankDirection(heightMap, x, z);
    }
}
```

### 4. River-Lake Delta Formation

#### Current State
- Proximity-based river-lake integration
- Limited delta formation

#### Proposed Improvements
```csharp
// Add delta formation system
public class DeltaFormationGenerator
{
    public void GenerateDeltas(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 43 + chunkZ * 31));
        
        // Find river-lake intersections
        var intersections = FindRiverLakeIntersections(riverMask, lakeMask, chunkSize);
        
        // Generate deltas at intersections
        foreach (var intersection in intersections)
        {
            if (random.NextDouble() < config.DeltaFormationChance)
            {
                GenerateDelta(heightMap, intersection, random);
            }
        }
    }
    
    private List<RiverLakeIntersection> FindRiverLakeIntersections(
        float[,] riverMask,
        float[,] lakeMask,
        int chunkSize)
    {
        var intersections = new List<RiverLakeIntersection>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                float lakeStrength = lakeMask[x, z];
                
                // Check for river-lake intersection
                if (riverStrength > 0.3f && lakeStrength > 0.3f)
                {
                    intersections.Add(new RiverLakeIntersection
                    {
                        X = x,
                        Z = z,
                        RiverStrength = riverStrength,
                        LakeStrength = lakeStrength
                    });
                }
            }
        }
        
        return intersections;
    }
    
    private void GenerateDelta(int[,] heightMap, RiverLakeIntersection intersection, Random random)
    {
        int x = intersection.X;
        int z = intersection.Z;
        int baseHeight = heightMap[x, z];
        
        // Generate delta fan
        int deltaRadius = random.Next(config.MinDeltaRadius, config.MaxDeltaRadius);
        int deltaLayers = random.Next(config.MinDeltaLayers, config.MaxDeltaLayers);
        
        for (int layer = 0; layer < deltaLayers; layer++)
        {
            double layerHeight = baseHeight - layer * config.DeltaLayerHeight;
            int layerRadius = (int)(deltaRadius * (1.0 - layer * 0.2));
            
            for (int dx = -layerRadius; dx <= layerRadius; dx++)
            {
                for (int dz = -layerRadius; dz <= layerRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < heightMap.GetLength(0) &&
                        nz >= 0 && nz < heightMap.GetLength(1))
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        if (distance <= layerRadius)
                        {
                            // Smooth delta shape with noise
                            double noise = (random.NextDouble() - 0.5) * config.DeltaNoise;
                            int targetHeight = (int)(layerHeight + noise);
                            
                            // Only lower terrain (erosion/deposition)
                            if (targetHeight < heightMap[nx, nz])
                            {
                                heightMap[nx, nz] = targetHeight;
                            }
                        }
                    }
                }
            }
        }
    }
}

public class RiverLakeIntersection
{
    public int X { get; set; }
    public int Z { get; set; }
    public float RiverStrength { get; set; }
    public float LakeStrength { get; set; }
}
```

## Lake Generation Improvements

### 1. Varied Lake Shapes

#### Current State
- Noise-based lake shapes
- Limited shape variety

#### Proposed Improvements
```csharp
// Add multiple lake shape types
public enum LakeShapeType
{
    Basin,
    Crater,
    Oxbow,
    Fjord,
    Complex
}

public class LakeShapeGenerator
{
    public float[,] GenerateLake(
        int centerX, int centerZ,
        LakeShapeType shapeType,
        int radius,
        int depth,
        int chunkX, int chunkZ,
        int chunkSize,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ centerX * 47 + centerZ * 33));
        var lakeMask = new float[chunkSize, chunkSize];
        
        switch (shapeType)
        {
            case LakeShapeType.Basin:
                GenerateBasinLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Crater:
                GenerateCraterLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Oxbow:
                GenerateOxbowLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Fjord:
                GenerateFjordLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Complex:
                GenerateComplexLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
        }
        
        return lakeMask;
    }
    
    private void GenerateBasinLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate smooth basin shape
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double distance = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(z - cz, 2));
                if (distance <= radius)
                {
                    double falloff = 1.0 - (distance / radius);
                    double noise = SimplexNoise.Generate(x * 0.1, z * 0.1, 1.0, 2, 1.0, 0.5, random.Next());
                    mask[x, z] = (float)(falloff * (1.0 + noise * 0.2));
                }
            }
        }
    }
    
    private void GenerateCraterLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate crater-like lake with raised rim
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double distance = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(z - cz, 2));
                if (distance <= radius * 1.2)
                {
                    double craterShape = CalculateCraterShape(distance, radius);
                    double noise = SimplexNoise.Generate(x * 0.15, z * 0.15, 1.0, 2, 1.0, 0.4, random.Next());
                    mask[x, z] = (float)(craterShape + noise * 0.15);
                }
            }
        }
    }
    
    private double CalculateCraterShape(double distance, double radius)
    {
        double normalizedDist = distance / radius;
        
        if (normalizedDist < 0.8)
        {
            // Crater floor (deep)
            return 1.0;
        }
        else if (normalizedDist < 1.0)
        {
            // Crater wall (transition)
            return 1.0 - (normalizedDist - 0.8) * 5.0;
        }
        else
        {
            // Crater rim (raised)
            double rimHeight = Math.Max(0, 1.0 - (normalizedDist - 1.0) * 2.0);
            return rimHeight * 0.3; // Lower than crater floor
        }
    }
    
    private void GenerateOxbowLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate curved oxbow lake shape
        double curvature = random.NextDouble() * 0.5 + 0.25;
        double angle = random.NextDouble() * Math.PI * 2;
        
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double dx = x - cx;
                double dz = z - cz;
                double distance = Math.Sqrt(dx * dx + dz * dz);
                
                if (distance <= radius)
                {
                    // Calculate curved shape
                    double theta = Math.Atan2(dz, dx);
                    double curvedDist = distance + Math.Sin(theta * 2 + angle) * curvature * radius;
                    
                    if (curvedDist <= radius)
                    {
                        double falloff = 1.0 - (curvedDist / radius);
                        mask[x, z] = (float)falloff;
                    }
                }
            }
        }
    }
    
    private void GenerateFjordLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate narrow fjord-like lake
        double direction = random.NextDouble() * Math.PI * 2;
        double width = radius * 0.3;
        double length = radius * 2.0;
        
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double dx = x - cx;
                double dz = z - cz;
                
                // Rotate to fjord direction
                double rotatedX = dx * Math.Cos(-direction) - dz * Math.Sin(-direction);
                double rotatedZ = dx * Math.Sin(-direction) + dz * Math.Cos(-direction);
                
                // Check if within fjord shape
                double alongFjord = Math.Abs(rotatedX) / length;
                double acrossFjord = Math.Abs(rotatedZ) / width;
                
                if (alongFjord <= 1.0 && acrossFjord <= 1.0)
                {
                    double falloff = 1.0 - acrossFjord;
                    double noise = SimplexNoise.Generate(x * 0.1, z * 0.1, 1.0, 2, 1.0, 0.3, random.Next());
                    mask[x, z] = (float)(falloff * (1.0 + noise * 0.2));
                }
            }
        }
    }
    
    private void GenerateComplexLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate complex multi-basin lake
        int numBasins = random.Next(2, 5);
        var basinCenters = new List<(double x, double z)>();
        
        for (int i = 0; i < numBasins; i++)
        {
            double angle = (double)i / numBasins * Math.PI * 2 + random.NextDouble() * 0.5;
            double basinDist = radius * (0.3 + random.NextDouble() * 0.4);
            double bx = cx + Math.Cos(angle) * basinDist;
            double bz = cz + Math.Sin(angle) * basinDist;
            basinCenters.Add((bx, bz));
        }
        
        // Combine basins
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double maxStrength = 0.0;
                
                foreach (var (bx, bz) in basinCenters)
                {
                    double distance = Math.Sqrt(Math.Pow(x - bx, 2) + Math.Pow(z - bz, 2));
                    double basinRadius = radius * (0.4 + random.NextDouble() * 0.3);
                    
                    if (distance <= basinRadius)
                    {
                        double falloff = 1.0 - (distance / basinRadius);
                        maxStrength = Math.Max(maxStrength, falloff);
                    }
                }
                
                if (maxStrength > 0.0)
                {
                    double noise = SimplexNoise.Generate(x * 0.08, z * 0.08, 1.0, 2, 1.0, 0.25, random.Next());
                    mask[x, z] = (float)(maxStrength * (1.0 + noise * 0.15));
                }
            }
        }
    }
}
```

### 2. Sophisticated Lake Depth Profiles

#### Current State
- Basin-based depth
- Limited depth variation

#### Proposed Improvements
```csharp
// Add thermocline and depth layer simulation
public class LakeDepthProfile
{
    public float[,] GenerateDepthProfile(
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 53 + chunkZ * 37));
        var depthProfile = new float[chunkSize, chunkSize];
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float lakeStrength = lakeMask[x, z];
                if (lakeStrength > 0.1f)
                {
                    int surface = heightMap[x, z];
                    int depthBelowSea = seaLevel - surface;
                    
                    if (depthBelowSea > 0)
                    {
                        // Generate depth profile with thermocline
                        double depthFactor = (double)depthBelowSea / config.MaxLakeDepth;
                        double thermocline = CalculateThermocline(depthFactor, random);
                        double depthNoise = SimplexNoise.Generate(x * 0.05, z * 0.05, 1.0, 2, 1.0, 0.3, random.Next());
                        
                        depthProfile[x, z] = (float)(lakeStrength * (1.0 + thermocline + depthNoise));
                    }
                }
            }
        }
        
        return depthProfile;
    }
    
    private double CalculateThermocline(double depthFactor, Random random)
    {
        // Simulate thermocline layering
        double epilimnion = Math.Max(0, 1.0 - depthFactor * 2.0);
        double metalimnion = Math.Max(0, Math.Min(1.0, depthFactor * 2.0 - 1.0));
        double hypolimnion = Math.Max(0, depthFactor - 1.0);
        
        // Add seasonal variation
        double seasonalVariation = Math.Sin(random.NextDouble() * Math.PI * 2) * 0.1;
        
        return epilimnion * 0.3 + metalimnion * 0.5 + hypolimnion * 0.2 + seasonalVariation;
    }
}
```

### 3. Enhanced River-Lake Connectivity

#### Current State
- Proximity-based integration
- Limited connectivity modeling

#### Proposed Improvements
```csharp
// Add sophisticated river-lake connectivity
public class RiverLakeConnectivity
{
    public void EnhanceConnectivity(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 59 + chunkZ * 41));
        
        // Find all river-lake connections
        var connections = FindConnections(riverMask, lakeMask, heightMap, chunkSize, seaLevel);
        
        // Enhance each connection
        foreach (var connection in connections)
        {
            EnhanceConnection(riverMask, lakeMask, heightMap, connection, random);
        }
    }
    
    private List<RiverLakeConnection> FindConnections(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkSize,
        int seaLevel)
    {
        var connections = new List<RiverLakeConnection>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                float lakeStrength = lakeMask[x, z];
                
                // Check for river entering lake
                if (riverStrength > 0.5f && lakeStrength > 0.3f)
                {
                    // Check if this is a river-to-lake transition
                    if (IsRiverToLakeTransition(riverMask, lakeMask, x, z, chunkSize))
                    {
                        connections.Add(new RiverLakeConnection
                        {
                            X = x,
                            Z = z,
                            Type = ConnectionType.RiverToLake,
                            Strength = riverStrength
                        });
                    }
                }
                // Check for lake-to-river transition
                else if (lakeStrength > 0.5f && riverStrength > 0.3f)
                {
                    if (IsLakeToRiverTransition(riverMask, lakeMask, x, z, chunkSize))
                    {
                        connections.Add(new RiverLakeConnection
                        {
                            X = x,
                            Z = z,
                            Type = ConnectionType.LakeToRiver,
                            Strength = lakeStrength
                        });
                    }
                }
            }
        }
        
        return connections;
    }
    
    private bool IsRiverToLakeTransition(float[,] riverMask, float[,] lakeMask, int x, int z, int chunkSize)
    {
        // Check if river flows into lake
        var downstream = FindDownstream(riverMask, x, z, chunkSize);
        return downstream.HasValue && lakeMask[downstream.Value.x, downstream.Value.z] > 0.3f;
    }
    
    private bool IsLakeToRiverTransition(float[,] riverMask, float[,] lakeMask, int x, int z, int chunkSize)
    {
        // Check if lake drains into river
        var downstream = FindDownstream(lakeMask, x, z, chunkSize);
        return downstream.HasValue && riverMask[downstream.Value.x, downstream.Value.z] > 0.3f;
    }
    
    private (int x, int z)? FindDownstream(float[,] mask, int x, int z, int chunkSize)
    {
        int centerHeight = mask[x, z];
        (int x, int z)? bestDownstream = null;
        float maxDownstream = float.MinValue;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                
                int nx = x + dx;
                int nz = z + dz;
                
                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                {
                    if (mask[nx, nz] > maxDownstream)
                    {
                        maxDownstream = mask[nx, nz];
                        bestDownstream = (nx, nz);
                    }
                }
            }
        }
        
        return bestDownstream;
    }
    
    private void EnhanceConnection(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        RiverLakeConnection connection,
        Random random)
    {
        // Smooth the transition between river and lake
        int radius = random.Next(2, 5);
        
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int nx = connection.X + dx;
                int nz = connection.Z + dz;
                
                if (nx >= 0 && nx < riverMask.GetLength(0) &&
                    nz >= 0 && nz < riverMask.GetLength(1))
                {
                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    double blend = 1.0 - (distance / (radius + 1));
                    
                    // Blend river and lake masks
                    float riverValue = riverMask[nx, nz];
                    float lakeValue = lakeMask[nx, nz];
                    float blendedValue = (float)(riverValue * (1.0 - blend) + lakeValue * blend);
                    
                    riverMask[nx, nz] = blendedValue;
                    lakeMask[nx, nz] = blendedValue;
                }
            }
        }
    }
}

public class RiverLakeConnection
{
    public int X { get; set; }
    public int Z { get; set; }
    public ConnectionType Type { get; set; }
    public float Strength { get; set; }
}

public enum ConnectionType
{
    RiverToLake,
    LakeToRiver
}
```

### 4. Enhanced Wetland Features

#### Current State
- Buffer-based wetlands
- Limited wetland variety

#### Proposed Improvements
```csharp
// Add multiple wetland types
public enum WetlandType
{
    Marsh,
    Swamp,
    Bog,
    Fen,
    Wetland
}

public class WetlandGenerator
{
    public float[,] GenerateWetlands(
        float[,] lakeMask,
        float[,] hydrologyMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 61 + chunkZ * 43));
        var wetlandMask = new float[chunkSize, chunkSize];
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float lakeStrength = lakeMask[x, z];
                float hydrologyStrength = hydrologyMask[x, z];
                int height = heightMap[x, z];
                int depthBelowSea = seaLevel - height;
                
                // Determine wetland type based on conditions
                WetlandType wetlandType = DetermineWetlandType(
                    lakeStrength, hydrologyStrength, depthBelowSea, random);
                
                // Generate wetland based on type
                float wetlandStrength = GenerateWetlandStrength(
                    wetlandType, lakeStrength, hydrologyStrength, depthBelowSea, random);
                
                wetlandMask[x, z] = wetlandStrength;
            }
        }
        
        return wetlandMask;
    }
    
    private WetlandType DetermineWetlandType(
        float lakeStrength,
        float hydrologyStrength,
        int depthBelowSea,
        Random random)
    {
        // Use conditions to determine wetland type
        double wetness = lakeStrength * 0.6 + hydrologyStrength * 0.4;
        
        if (depthBelowSea > 0 && depthBelowSea < 5)
        {
            // Shallow water - marsh or fen
            return random.NextDouble() < 0.6 ? WetlandType.Marsh : WetlandType.Fen;
        }
        else if (depthBelowSea >= 5 && depthBelowSea < 15)
        {
            // Medium depth - swamp or bog
            return random.NextDouble() < 0.5 ? WetlandType.Swamp : WetlandType.Bog;
        }
        else if (wetness > 0.3 && wetness < 0.7)
        {
            // Moderate wetness - wetland
            return WetlandType.Wetland;
        }
        else
        {
            // Default to wetland
            return WetlandType.Wetland;
        }
    }
    
    private float GenerateWetlandStrength(
        WetlandType type,
        float lakeStrength,
        float hydrologyStrength,
        int depthBelowSea,
        Random random)
    {
        double baseStrength = lakeStrength * 0.7 + hydrologyStrength * 0.3;
        double typeModifier = GetTypeModifier(type);
        double depthModifier = GetDepthModifier(depthBelowSea);
        double noise = SimplexNoise.Generate(
            lakeStrength.GetHashCode() * 0.1,
            hydrologyStrength.GetHashCode() * 0.1,
            1.0, 2, 1.0, 0.3, random.Next());
        
        return (float)(baseStrength * typeModifier * depthModifier * (1.0 + noise * 0.2));
    }
    
    private double GetTypeModifier(WetlandType type)
    {
        return type switch
        {
            WetlandType.Marsh => 1.2,
            WetlandType.Swamp => 1.0,
            WetlandType.Bog => 0.9,
            WetlandType.Fen => 1.1,
            WetlandType.Wetland => 1.0,
            _ => 1.0
        };
    }
    
    private double GetDepthModifier(int depthBelowSea)
    {
        if (depthBelowSea <= 0)
        {
            return 0.5; // No wetland above water
        }
        else if (depthBelowSea < 5)
        {
            return 1.3; // Shallow wetlands stronger
        }
        else if (depthBelowSea < 15)
        {
            return 1.0; // Medium depth normal
        }
        else
        {
            return 0.7; // Deep wetlands weaker
        }
    }
}
```

## Configuration Integration

### New Configuration Parameters

```json
{
  "caveImprovements": {
    "enableCaveConnectivity": true,
    "enableBiomeModifiers": true,
    "enableCaveDecorations": true,
    "enableUndergroundLakes": true,
    "minCaveVolume": 50,
    "maxConnectionDistance": 16,
    "stalactiteChance": 0.15,
    "stalagmiteChance": 0.15,
    "minStalactiteLength": 2,
    "maxStalactiteLength": 8,
    "minStalagmiteLength": 2,
    "maxStalagmiteLength": 6,
    "undergroundLakeChance": 0.05,
    "minLakeSpace": 20,
    "minLakeDepth": 3,
    "maxLakeDepth": 8,
    "minLakeRadius": 3,
    "maxLakeRadius": 6
  },
  "riverImprovements": {
    "enableMeanderEvolution": true,
    "enableSeasonalWidth": true,
    "enableBankErosion": true,
    "enableDeltaFormation": true,
    "meanderIterations": 10,
    "meanderStrength": 0.5,
    "erosionRate": 0.1,
    "sedimentDepositRate": 0.05,
    "erosionNoise": 0.3,
    "sedimentNoise": 0.2,
    "deltaFormationChance": 0.3,
    "minDeltaRadius": 4,
    "maxDeltaRadius": 12,
    "minDeltaLayers": 2,
    "maxDeltaLayers": 5,
    "deltaLayerHeight": 1,
    "deltaNoise": 0.5
  },
  "lakeImprovements": {
    "enableVariedShapes": true,
    "enableDepthProfiles": true,
    "enhanceConnectivity": true,
    "enhanceWetlands": true,
    "deltaFormationChance": 0.3,
    "minLakeDepth": 5,
    "maxLakeDepth": 30,
    "thermoclineStrength": 0.5,
    "wetlandBufferRadius": 8,
    "shorelineBlend": 0.25
  },
  "biomeCaveModifiers": {
    "default": {
      "minRadius": 2.0,
      "maxRadius": 8.0,
      "heightMultiplier": 1.0,
      "densityMultiplier": 1.0,
      "connectivityBonus": 0.0
    },
    "plains": {
      "minRadius": 2.5,
      "maxRadius": 10.0,
      "heightMultiplier": 1.2,
      "densityMultiplier": 1.1,
      "connectivityBonus": 0.1
    },
    "forest": {
      "minRadius": 1.5,
      "maxRadius": 6.0,
      "heightMultiplier": 0.8,
      "densityMultiplier": 1.3,
      "connectivityBonus": 0.2
    },
    "mountains": {
      "minRadius": 3.0,
      "maxRadius": 12.0,
      "heightMultiplier": 1.5,
      "densityMultiplier": 0.8,
      "connectivityBonus": 0.05
    }
  }
}
```

## Implementation Priority

### High Priority
1. **Cave Connectivity Enhancement**
   - Improves cave exploration experience
   - Reduces isolated cave pockets
   - Enhances gameplay flow

2. **River Meander Evolution**
   - Creates more natural river paths
   - Improves visual quality
   - Enhances navigation

3. **Lake Shape Variety**
   - Adds visual interest
   - Creates unique landmarks
   - Improves exploration

### Medium Priority
1. **Biome-Based Cave Modifiers**
   - Adds biome-specific cave characteristics
   - Enhances variety
   - Improves immersion

2. **River Bank Erosion**
   - Creates more realistic river banks
   - Adds dynamic terrain changes
   - Enhances visual quality

3. **Lake Depth Profiles**
   - Adds thermocline simulation
   - Improves water realism
   - Enhances fishing gameplay

### Low Priority
1. **Cave Decorations**
   - Adds stalactites and stalagmites
   - Enhances cave aesthetics
   - Improves exploration

2. **Delta Formation**
   - Creates river deltas
   - Adds visual interest
   - Enhances realism

3. **Enhanced Wetlands**
   - Adds wetland variety
   - Improves biome diversity
   - Enhances exploration

## Conclusion

The proposed improvements build upon the already sophisticated terrain generation system. They focus on:

1. **Enhanced Realism**
   - More natural cave connectivity
   - Improved river meandering
   - Varied lake shapes

2. **Increased Variety**
   - Biome-specific features
   - Multiple wetland types
   - Dynamic seasonal effects

3. **Better Gameplay**
   - Improved exploration
   - Enhanced navigation
   - More interesting landmarks

These improvements can be implemented incrementally, allowing for testing and refinement of each feature before moving to the next.

## Overview
This document outlines proposed improvements to the terrain generation algorithms for caves, rivers, and lakes based on the comprehensive analysis of the current implementation.

## Cave Generation Improvements

### 1. Cave Connectivity Enhancement

#### Current State
- Caves are generated using noise-based algorithms
- Individual cave systems are not explicitly connected
- Connectivity is incidental through noise overlap

#### Proposed Improvements
```csharp
// Add cave network connectivity system
public class CaveNetworkGenerator
{
    private List<CaveSystem> caveSystems;
    
    public List<CaveSystem> GenerateConnectedNetworks(
        bool[,,] baseCaveMask,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight)
    {
        var systems = new List<CaveSystem>();
        var visited = new bool[chunkSize, worldHeight, chunkSize];
        
        // Flood fill to identify connected cave systems
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 1; y < worldHeight - 1; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (baseCaveMask[x, y, z] && !visited[x, y, z])
                    {
                        var system = FloodFillCaveSystem(baseCaveMask, visited, x, y, z);
                        if (system.Volume > config.MinCaveVolume)
                        {
                            systems.Add(system);
                        }
                    }
                }
            }
        }
        
        // Connect nearby cave systems with tunnels
        ConnectCaveSystems(systems);
        
        return systems;
    }
    
    private CaveSystem FloodFillCaveSystem(bool[,,] mask, bool[,,] visited, int startX, int startY, int startZ)
    {
        var system = new CaveSystem();
        var queue = new Queue<(int x, int y, int z)>();
        queue.Enqueue((startX, startY, startZ));
        visited[startX, startY, startZ] = true;
        
        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            system.AddBlock(x, y, z);
            
            // Check 6-connected neighbors
            var neighbors = new[] {
                (x + 1, y, z), (x - 1, y, z),
                (x, y + 1, z), (x, y - 1, z),
                (x, y, z + 1), (x, y, z - 1)
            };
            
            foreach (var (nx, ny, nz) in neighbors)
            {
                if (nx >= 0 && nx < mask.GetLength(0) &&
                    ny >= 0 && ny < mask.GetLength(1) &&
                    nz >= 0 && nz < mask.GetLength(2) &&
                    mask[nx, ny, nz] && !visited[nx, ny, nz])
                {
                    visited[nx, ny, nz] = true;
                    queue.Enqueue((nx, ny, nz));
                }
            }
        }
        
        return system;
    }
    
    private void ConnectCaveSystems(List<CaveSystem> systems)
    {
        // Find nearby cave systems and connect them with tunnels
        for (int i = 0; i < systems.Count; i++)
        {
            for (int j = i + 1; j < systems.Count; j++)
            {
                double distance = CalculateSystemDistance(systems[i], systems[j]);
                if (distance < config.MaxConnectionDistance)
                {
                    CreateTunnelConnection(systems[i], systems[j]);
                }
            }
        }
    }
}

public class CaveSystem
{
    public List<(int x, int y, int z)> Blocks { get; } = new();
    public int Volume => Blocks.Count;
    public Vector3Int Center { get; private set; }
    
    public void AddBlock(int x, int y, int z)
    {
        Blocks.Add((x, y, z));
        // Update center
    }
}
```

### 2. Dynamic Cave Size Variation

#### Current State
- Cave size controlled by threshold and depth
- Limited dynamic variation based on noise

#### Proposed Improvements
```csharp
// Add biome-based cave size modifiers
public class BiomeCaveModifiers
{
    public Dictionary<string, CaveSizeProfile> Profiles { get; } = new();
    
    public CaveSizeProfile GetProfile(string biomeId)
    {
        return Profiles.TryGetValue(biomeId, out var profile) 
            ? profile 
            : Profiles["default"];
    }
    
    public void LoadFromConfig(string configPath)
    {
        var config = JsonUtility.FromJson<Dictionary<string, CaveSizeProfile>>(configPath);
        foreach (var kvp in config)
        {
            Profiles[kvp.Key] = kvp.Value;
        }
    }
}

public class CaveSizeProfile
{
    public double MinRadius { get; set; } = 2.0;
    public double MaxRadius { get; set; } = 8.0;
    public double HeightMultiplier { get; set; } = 1.0;
    public double DensityMultiplier { get; set; } = 1.0;
    public double ConnectivityBonus { get; set; } = 0.0;
}

// Integrate into ImprovedCaveGenerator
public bool[,,] BuildMask(
    int chunkX, int chunkZ, int chunkSize,
    int worldHeight, int[,] heightMap,
    float[,] hydrologyMask, float[,] flowMask,
    float[,]? riverMask, float[,] erosionRisk,
    int seaLevel, string biomeId)
{
    // Get biome-specific cave profile
    var caveProfile = biomeModifiers.GetProfile(biomeId);
    
    // Apply biome modifiers to cave generation
    double biomeDensity = config.Threshold * caveProfile.DensityMultiplier;
    double biomeHeight = worldHeight * caveProfile.HeightMultiplier;
    
    // Generate caves with biome-aware parameters
    // ... existing cave generation code with biome modifiers
}
```

### 3. Enhanced Ceiling/Floor Shaping

#### Current State
- Basic ceiling/floor shaping
- Limited decoration

#### Proposed Improvements
```csharp
// Add stalactite and stalagmite generation
public class CaveDecorationGenerator
{
    public void GenerateDecorations(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 31 + chunkZ * 17));
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int surface = heightMap[x, z];
                
                // Find cave ceiling and floor
                int? ceilingY = FindCaveCeiling(caveMask, x, z, surface);
                int? floorY = FindCaveFloor(caveMask, x, z, surface);
                
                if (ceilingY.HasValue && floorY.HasValue)
                {
                    // Generate stalactites from ceiling
                    if (random.NextDouble() < config.StalactiteChance)
                    {
                        GenerateStalactite(caveMask, x, z, ceilingY.Value, random);
                    }
                    
                    // Generate stalagmites from floor
                    if (random.NextDouble() < config.StalagmiteChance)
                    {
                        GenerateStalagmite(caveMask, x, z, floorY.Value, random);
                    }
                }
            }
        }
    }
    
    private int? FindCaveCeiling(bool[,,] mask, int x, int z, int surface)
    {
        for (int y = surface - 1; y > 0; y--)
        {
            if (mask[x, y, z])
            {
                return y;
            }
        }
        return null;
    }
    
    private int? FindCaveFloor(bool[,,] mask, int x, int z, int surface)
    {
        for (int y = 1; y < surface; y++)
        {
            if (mask[x, y, z])
            {
                return y;
            }
        }
        return null;
    }
    
    private void GenerateStalactite(bool[,,] mask, int x, int z, int ceilingY, Random random)
    {
        int length = random.Next(config.MinStalactiteLength, config.MaxStalactiteLength);
        
        for (int i = 0; i < length && (ceilingY - i) > 0; i++)
        {
            int y = ceilingY - i;
            if (y < mask.GetLength(1) && !mask[x, y, z])
            {
                mask[x, y, z] = true;
            }
        }
    }
    
    private void GenerateStalagmite(bool[,,] mask, int x, int z, int floorY, Random random)
    {
        int length = random.Next(config.MinStalagmiteLength, config.MaxStalagmiteLength);
        
        for (int i = 0; i < length && (floorY + i) < mask.GetLength(1); i++)
        {
            int y = floorY + i;
            if (!mask[x, y, z])
            {
                mask[x, y, z] = true;
            }
        }
    }
}
```

### 4. Underground Water Bodies

#### Current State
- Proximity-based water table integration
- No explicit underground water bodies

#### Proposed Improvements
```csharp
// Add underground lake generation
public class UndergroundLakeGenerator
{
    public void GenerateUndergroundLakes(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize, int worldHeight,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 37 + chunkZ * 23));
        
        // Identify potential underground lake locations
        var lakeCandidates = FindLakeCandidates(caveMask, heightMap, chunkSize, worldHeight, seaLevel);
        
        // Generate lakes at candidate locations
        foreach (var candidate in lakeCandidates)
        {
            if (random.NextDouble() < config.UndergroundLakeChance)
            {
                FillUndergroundLake(caveMask, candidate, random);
            }
        }
    }
    
    private List<LakeCandidate> FindLakeCandidates(
        bool[,,] caveMask,
        int[,] heightMap,
        int chunkSize, int worldHeight,
        int seaLevel)
    {
        var candidates = new List<LakeCandidate>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int surface = heightMap[x, z];
                
                // Look for large cave spaces below water table
                for (int y = seaLevel - 1; y > 0; y--)
                {
                    if (caveMask[x, y, z] && IsLargeCaveSpace(caveMask, x, y, z))
                    {
                        candidates.Add(new LakeCandidate { X = x, Y = y, Z = z });
                        break;
                    }
                }
            }
        }
        
        return candidates;
    }
    
    private bool IsLargeCaveSpace(bool[,,] mask, int x, int y, int z)
    {
        // Check if there's enough space for a lake
        int space = 0;
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx >= 0 && nx < mask.GetLength(0) &&
                    nz >= 0 && nz < mask.GetLength(2) &&
                    mask[nx, y, nz])
                {
                    space++;
                }
            }
        }
        
        return space >= config.MinLakeSpace;
    }
    
    private void FillUndergroundLake(bool[,,] mask, LakeCandidate candidate, Random random)
    {
        int depth = random.Next(config.MinLakeDepth, config.MaxLakeDepth);
        int radius = random.Next(config.MinLakeRadius, config.MaxLakeRadius);
        
        // Fill spherical lake volume
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = 0; dy < depth; dy++)
                {
                    int nx = candidate.X + dx;
                    int ny = candidate.Y + dy;
                    int nz = candidate.Z + dz;
                    
                    if (nx >= 0 && nx < mask.GetLength(0) &&
                        ny >= 0 && ny < mask.GetLength(1) &&
                        nz >= 0 && nz < mask.GetLength(2))
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz + dy * dy);
                        if (distance <= radius)
                        {
                            mask[nx, ny, nz] = true; // Fill with water
                        }
                    }
                }
            }
        }
    }
}

public class LakeCandidate
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}
```

## River Generation Improvements

### 1. Natural River Meandering

#### Current State
- Noise-based meandering
- Limited natural river behavior

#### Proposed Improvements
```csharp
// Add meander evolution algorithm
public class RiverMeanderEvolution
{
    public List<RiverNode> EvolveMeander(
        List<RiverNode> initialPath,
        int iterations,
        double meanderStrength,
        double erosionRate)
    {
        var path = new List<RiverNode>(initialPath);
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var newPath = new List<RiverNode>();
            
            for (int i = 0; i < path.Count; i++)
            {
                var current = path[i];
                var prev = i > 0 ? path[i - 1] : null;
                var next = i < path.Count - 1 ? path[i + 1] : null;
                
                // Calculate meander offset
                Vector2 meanderOffset = CalculateMeanderOffset(current, prev, next, meanderStrength);
                
                // Apply erosion
                double erosionFactor = CalculateErosionFactor(current, path, erosionRate);
                
                // Create new node
                var newNode = new RiverNode
                {
                    X = current.X + meanderOffset.X,
                    Z = current.Z + meanderOffset.Y,
                    Width = current.Width * (1.0 + erosionFactor * 0.1),
                    Depth = current.Depth * (1.0 + erosionFactor * 0.05)
                };
                
                newPath.Add(newNode);
            }
            
            path = newPath;
        }
        
        return path;
    }
    
    private Vector2 CalculateMeanderOffset(
        RiverNode current,
        RiverNode? prev,
        RiverNode? next,
        double strength)
    {
        if (prev == null && next == null)
        {
            return Vector2.zero;
        }
        
        Vector2 direction = Vector2.zero;
        if (next != null)
        {
            direction = new Vector2(next.X - current.X, next.Z - current.Z).normalized;
        }
        else if (prev != null)
        {
            direction = new Vector2(current.X - prev.X, current.Z - prev.Z).normalized;
        }
        
        // Perpendicular offset for meandering
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        double meanderAmount = Math.Sin(current.Distance * 0.1) * strength;
        
        return perpendicular * (float)meanderAmount;
    }
    
    private double CalculateErosionFactor(RiverNode node, List<RiverNode> path, double rate)
    {
        // Calculate curvature-based erosion
        double curvature = CalculateCurvature(node, path);
        return curvature * rate;
    }
    
    private double CalculateCurvature(RiverNode node, List<RiverNode> path)
    {
        int index = path.IndexOf(node);
        if (index < 2 || index >= path.Count - 2)
        {
            return 0.0;
        }
        
        var prev = path[index - 2];
        var curr = path[index - 1];
        var next = path[index];
        
        Vector2 v1 = new Vector2(curr.X - prev.X, curr.Z - prev.Z);
        Vector2 v2 = new Vector2(next.X - curr.X, next.Z - curr.Z);
        
        return Vector2.Angle(v1, v2);
    }
}

public class RiverNode
{
    public int X { get; set; }
    public int Z { get; set; }
    public double Width { get; set; }
    public double Depth { get; set; }
    public double Distance { get; set; }
}
```

### 2. Dynamic River Width Variation

#### Current State
- Flow-based width calculations
- Limited dynamic variation

#### Proposed Improvements
```csharp
// Add seasonal and terrain-based width variation
public class RiverWidthModulator
{
    public double CalculateWidth(
        double baseWidth,
        double flow,
        double slope,
        int season,
        int biomeId)
    {
        // Get biome-specific width modifiers
        var biomeMod = GetBiomeWidthModifier(biomeId);
        
        // Apply seasonal variation
        double seasonalMod = GetSeasonalModifier(season);
        
        // Apply slope-based variation (wider on flatter terrain)
        double slopeMod = 1.0 + (1.0 - Math.Clamp(slope / 10.0, 0.0, 1.0)) * 0.5;
        
        // Apply flow-based variation
        double flowMod = 1.0 + Math.Clamp(flow / 6.0, 0.0, 1.0) * 0.3;
        
        return baseWidth * biomeMod * seasonalMod * slopeMod * flowMod;
    }
    
    private double GetBiomeWidthModifier(string biomeId)
    {
        return biomeWidthModifiers.TryGetValue(biomeId, out var modifier)
            ? modifier
            : 1.0;
    }
    
    private double GetSeasonalModifier(int season)
    {
        // Season 0-3: Spring, Summer, Fall, Winter
        double[] seasonalMods = { 1.2, 0.8, 1.0, 1.1 };
        return seasonalMods[season % 4];
    }
}
```

### 3. Erosion-Based River Bank Shaping

#### Current State
- Noise-based river banks
- Limited erosion modeling

#### Proposed Improvements
```csharp
// Add erosion-based bank shaping
public class RiverBankErosion
{
    public void ErodeRiverBanks(
        float[,] riverMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 41 + chunkZ * 29));
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                if (riverStrength > 0.1f)
                {
                    // Erode river banks
                    ErodeBank(heightMap, x, z, riverStrength, random);
                    
                    // Deposit sediment
                    DepositSediment(heightMap, x, z, riverStrength, random);
                }
            }
        }
    }
    
    private void ErodeBank(int[,] heightMap, int x, int z, float riverStrength, Random random)
    {
        // Find river bank direction
        var bankDirection = FindBankDirection(heightMap, x, z);
        
        // Apply erosion based on river strength
        double erosionAmount = riverStrength * config.ErosionRate;
        
        int nx = x + bankDirection.X;
        int nz = z + bankDirection.Z;
        
        if (nx >= 0 && nx < heightMap.GetLength(0) &&
            nz >= 0 && nz < heightMap.GetLength(1))
        {
            // Add some randomness to erosion
            double noise = (random.NextDouble() - 0.5) * config.ErosionNoise;
            heightMap[nx, nz] = (int)(heightMap[nx, nz] - erosionAmount + noise);
        }
    }
    
    private void DepositSediment(int[,] heightMap, int x, int z, float riverStrength, Random random)
    {
        // Find downstream direction
        var downstream = FindDownstreamDirection(heightMap, x, z);
        
        // Deposit sediment based on river strength
        double depositAmount = riverStrength * config.SedimentDepositRate;
        
        int nx = x + downstream.X;
        int nz = z + downstream.Z;
        
        if (nx >= 0 && nx < heightMap.GetLength(0) &&
            nz >= 0 && nz < heightMap.GetLength(1))
        {
            // Add randomness to deposition
            double noise = (random.NextDouble() - 0.5) * config.SedimentNoise;
            heightMap[nx, nz] = (int)(heightMap[nx, nz] + depositAmount + noise);
        }
    }
    
    private Vector2Int FindBankDirection(int[,] heightMap, int x, int z)
    {
        // Find direction of steepest descent from river bank
        int center = heightMap[x, z];
        int bestDrop = 0;
        Vector2Int bestDir = Vector2Int.zero;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                
                int nx = x + dx;
                int nz = z + dz;
                
                if (nx >= 0 && nx < heightMap.GetLength(0) &&
                    nz >= 0 && nz < heightMap.GetLength(1))
                {
                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDir = new Vector2Int(dx, dz);
                    }
                }
            }
        }
        
        return bestDir;
    }
    
    private Vector2Int FindDownstreamDirection(int[,] heightMap, int x, int z)
    {
        // Find direction of steepest descent
        return FindBankDirection(heightMap, x, z);
    }
}
```

### 4. River-Lake Delta Formation

#### Current State
- Proximity-based river-lake integration
- Limited delta formation

#### Proposed Improvements
```csharp
// Add delta formation system
public class DeltaFormationGenerator
{
    public void GenerateDeltas(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 43 + chunkZ * 31));
        
        // Find river-lake intersections
        var intersections = FindRiverLakeIntersections(riverMask, lakeMask, chunkSize);
        
        // Generate deltas at intersections
        foreach (var intersection in intersections)
        {
            if (random.NextDouble() < config.DeltaFormationChance)
            {
                GenerateDelta(heightMap, intersection, random);
            }
        }
    }
    
    private List<RiverLakeIntersection> FindRiverLakeIntersections(
        float[,] riverMask,
        float[,] lakeMask,
        int chunkSize)
    {
        var intersections = new List<RiverLakeIntersection>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                float lakeStrength = lakeMask[x, z];
                
                // Check for river-lake intersection
                if (riverStrength > 0.3f && lakeStrength > 0.3f)
                {
                    intersections.Add(new RiverLakeIntersection
                    {
                        X = x,
                        Z = z,
                        RiverStrength = riverStrength,
                        LakeStrength = lakeStrength
                    });
                }
            }
        }
        
        return intersections;
    }
    
    private void GenerateDelta(int[,] heightMap, RiverLakeIntersection intersection, Random random)
    {
        int x = intersection.X;
        int z = intersection.Z;
        int baseHeight = heightMap[x, z];
        
        // Generate delta fan
        int deltaRadius = random.Next(config.MinDeltaRadius, config.MaxDeltaRadius);
        int deltaLayers = random.Next(config.MinDeltaLayers, config.MaxDeltaLayers);
        
        for (int layer = 0; layer < deltaLayers; layer++)
        {
            double layerHeight = baseHeight - layer * config.DeltaLayerHeight;
            int layerRadius = (int)(deltaRadius * (1.0 - layer * 0.2));
            
            for (int dx = -layerRadius; dx <= layerRadius; dx++)
            {
                for (int dz = -layerRadius; dz <= layerRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < heightMap.GetLength(0) &&
                        nz >= 0 && nz < heightMap.GetLength(1))
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        if (distance <= layerRadius)
                        {
                            // Smooth delta shape with noise
                            double noise = (random.NextDouble() - 0.5) * config.DeltaNoise;
                            int targetHeight = (int)(layerHeight + noise);
                            
                            // Only lower terrain (erosion/deposition)
                            if (targetHeight < heightMap[nx, nz])
                            {
                                heightMap[nx, nz] = targetHeight;
                            }
                        }
                    }
                }
            }
        }
    }
}

public class RiverLakeIntersection
{
    public int X { get; set; }
    public int Z { get; set; }
    public float RiverStrength { get; set; }
    public float LakeStrength { get; set; }
}
```

## Lake Generation Improvements

### 1. Varied Lake Shapes

#### Current State
- Noise-based lake shapes
- Limited shape variety

#### Proposed Improvements
```csharp
// Add multiple lake shape types
public enum LakeShapeType
{
    Basin,
    Crater,
    Oxbow,
    Fjord,
    Complex
}

public class LakeShapeGenerator
{
    public float[,] GenerateLake(
        int centerX, int centerZ,
        LakeShapeType shapeType,
        int radius,
        int depth,
        int chunkX, int chunkZ,
        int chunkSize,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ centerX * 47 + centerZ * 33));
        var lakeMask = new float[chunkSize, chunkSize];
        
        switch (shapeType)
        {
            case LakeShapeType.Basin:
                GenerateBasinLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Crater:
                GenerateCraterLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Oxbow:
                GenerateOxbowLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Fjord:
                GenerateFjordLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
            case LakeShapeType.Complex:
                GenerateComplexLake(lakeMask, centerX, centerZ, radius, depth, random);
                break;
        }
        
        return lakeMask;
    }
    
    private void GenerateBasinLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate smooth basin shape
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double distance = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(z - cz, 2));
                if (distance <= radius)
                {
                    double falloff = 1.0 - (distance / radius);
                    double noise = SimplexNoise.Generate(x * 0.1, z * 0.1, 1.0, 2, 1.0, 0.5, random.Next());
                    mask[x, z] = (float)(falloff * (1.0 + noise * 0.2));
                }
            }
        }
    }
    
    private void GenerateCraterLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate crater-like lake with raised rim
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double distance = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(z - cz, 2));
                if (distance <= radius * 1.2)
                {
                    double craterShape = CalculateCraterShape(distance, radius);
                    double noise = SimplexNoise.Generate(x * 0.15, z * 0.15, 1.0, 2, 1.0, 0.4, random.Next());
                    mask[x, z] = (float)(craterShape + noise * 0.15);
                }
            }
        }
    }
    
    private double CalculateCraterShape(double distance, double radius)
    {
        double normalizedDist = distance / radius;
        
        if (normalizedDist < 0.8)
        {
            // Crater floor (deep)
            return 1.0;
        }
        else if (normalizedDist < 1.0)
        {
            // Crater wall (transition)
            return 1.0 - (normalizedDist - 0.8) * 5.0;
        }
        else
        {
            // Crater rim (raised)
            double rimHeight = Math.Max(0, 1.0 - (normalizedDist - 1.0) * 2.0);
            return rimHeight * 0.3; // Lower than crater floor
        }
    }
    
    private void GenerateOxbowLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate curved oxbow lake shape
        double curvature = random.NextDouble() * 0.5 + 0.25;
        double angle = random.NextDouble() * Math.PI * 2;
        
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double dx = x - cx;
                double dz = z - cz;
                double distance = Math.Sqrt(dx * dx + dz * dz);
                
                if (distance <= radius)
                {
                    // Calculate curved shape
                    double theta = Math.Atan2(dz, dx);
                    double curvedDist = distance + Math.Sin(theta * 2 + angle) * curvature * radius;
                    
                    if (curvedDist <= radius)
                    {
                        double falloff = 1.0 - (curvedDist / radius);
                        mask[x, z] = (float)falloff;
                    }
                }
            }
        }
    }
    
    private void GenerateFjordLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate narrow fjord-like lake
        double direction = random.NextDouble() * Math.PI * 2;
        double width = radius * 0.3;
        double length = radius * 2.0;
        
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double dx = x - cx;
                double dz = z - cz;
                
                // Rotate to fjord direction
                double rotatedX = dx * Math.Cos(-direction) - dz * Math.Sin(-direction);
                double rotatedZ = dx * Math.Sin(-direction) + dz * Math.Cos(-direction);
                
                // Check if within fjord shape
                double alongFjord = Math.Abs(rotatedX) / length;
                double acrossFjord = Math.Abs(rotatedZ) / width;
                
                if (alongFjord <= 1.0 && acrossFjord <= 1.0)
                {
                    double falloff = 1.0 - acrossFjord;
                    double noise = SimplexNoise.Generate(x * 0.1, z * 0.1, 1.0, 2, 1.0, 0.3, random.Next());
                    mask[x, z] = (float)(falloff * (1.0 + noise * 0.2));
                }
            }
        }
    }
    
    private void GenerateComplexLake(float[,] mask, int cx, int cz, int radius, int depth, Random random)
    {
        // Generate complex multi-basin lake
        int numBasins = random.Next(2, 5);
        var basinCenters = new List<(double x, double z)>();
        
        for (int i = 0; i < numBasins; i++)
        {
            double angle = (double)i / numBasins * Math.PI * 2 + random.NextDouble() * 0.5;
            double basinDist = radius * (0.3 + random.NextDouble() * 0.4);
            double bx = cx + Math.Cos(angle) * basinDist;
            double bz = cz + Math.Sin(angle) * basinDist;
            basinCenters.Add((bx, bz));
        }
        
        // Combine basins
        for (int x = 0; x < mask.GetLength(0); x++)
        {
            for (int z = 0; z < mask.GetLength(1); z++)
            {
                double maxStrength = 0.0;
                
                foreach (var (bx, bz) in basinCenters)
                {
                    double distance = Math.Sqrt(Math.Pow(x - bx, 2) + Math.Pow(z - bz, 2));
                    double basinRadius = radius * (0.4 + random.NextDouble() * 0.3);
                    
                    if (distance <= basinRadius)
                    {
                        double falloff = 1.0 - (distance / basinRadius);
                        maxStrength = Math.Max(maxStrength, falloff);
                    }
                }
                
                if (maxStrength > 0.0)
                {
                    double noise = SimplexNoise.Generate(x * 0.08, z * 0.08, 1.0, 2, 1.0, 0.25, random.Next());
                    mask[x, z] = (float)(maxStrength * (1.0 + noise * 0.15));
                }
            }
        }
    }
}
```

### 2. Sophisticated Lake Depth Profiles

#### Current State
- Basin-based depth
- Limited depth variation

#### Proposed Improvements
```csharp
// Add thermocline and depth layer simulation
public class LakeDepthProfile
{
    public float[,] GenerateDepthProfile(
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 53 + chunkZ * 37));
        var depthProfile = new float[chunkSize, chunkSize];
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float lakeStrength = lakeMask[x, z];
                if (lakeStrength > 0.1f)
                {
                    int surface = heightMap[x, z];
                    int depthBelowSea = seaLevel - surface;
                    
                    if (depthBelowSea > 0)
                    {
                        // Generate depth profile with thermocline
                        double depthFactor = (double)depthBelowSea / config.MaxLakeDepth;
                        double thermocline = CalculateThermocline(depthFactor, random);
                        double depthNoise = SimplexNoise.Generate(x * 0.05, z * 0.05, 1.0, 2, 1.0, 0.3, random.Next());
                        
                        depthProfile[x, z] = (float)(lakeStrength * (1.0 + thermocline + depthNoise));
                    }
                }
            }
        }
        
        return depthProfile;
    }
    
    private double CalculateThermocline(double depthFactor, Random random)
    {
        // Simulate thermocline layering
        double epilimnion = Math.Max(0, 1.0 - depthFactor * 2.0);
        double metalimnion = Math.Max(0, Math.Min(1.0, depthFactor * 2.0 - 1.0));
        double hypolimnion = Math.Max(0, depthFactor - 1.0);
        
        // Add seasonal variation
        double seasonalVariation = Math.Sin(random.NextDouble() * Math.PI * 2) * 0.1;
        
        return epilimnion * 0.3 + metalimnion * 0.5 + hypolimnion * 0.2 + seasonalVariation;
    }
}
```

### 3. Enhanced River-Lake Connectivity

#### Current State
- Proximity-based integration
- Limited connectivity modeling

#### Proposed Improvements
```csharp
// Add sophisticated river-lake connectivity
public class RiverLakeConnectivity
{
    public void EnhanceConnectivity(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 59 + chunkZ * 41));
        
        // Find all river-lake connections
        var connections = FindConnections(riverMask, lakeMask, heightMap, chunkSize, seaLevel);
        
        // Enhance each connection
        foreach (var connection in connections)
        {
            EnhanceConnection(riverMask, lakeMask, heightMap, connection, random);
        }
    }
    
    private List<RiverLakeConnection> FindConnections(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        int chunkSize,
        int seaLevel)
    {
        var connections = new List<RiverLakeConnection>();
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float riverStrength = riverMask[x, z];
                float lakeStrength = lakeMask[x, z];
                
                // Check for river entering lake
                if (riverStrength > 0.5f && lakeStrength > 0.3f)
                {
                    // Check if this is a river-to-lake transition
                    if (IsRiverToLakeTransition(riverMask, lakeMask, x, z, chunkSize))
                    {
                        connections.Add(new RiverLakeConnection
                        {
                            X = x,
                            Z = z,
                            Type = ConnectionType.RiverToLake,
                            Strength = riverStrength
                        });
                    }
                }
                // Check for lake-to-river transition
                else if (lakeStrength > 0.5f && riverStrength > 0.3f)
                {
                    if (IsLakeToRiverTransition(riverMask, lakeMask, x, z, chunkSize))
                    {
                        connections.Add(new RiverLakeConnection
                        {
                            X = x,
                            Z = z,
                            Type = ConnectionType.LakeToRiver,
                            Strength = lakeStrength
                        });
                    }
                }
            }
        }
        
        return connections;
    }
    
    private bool IsRiverToLakeTransition(float[,] riverMask, float[,] lakeMask, int x, int z, int chunkSize)
    {
        // Check if river flows into lake
        var downstream = FindDownstream(riverMask, x, z, chunkSize);
        return downstream.HasValue && lakeMask[downstream.Value.x, downstream.Value.z] > 0.3f;
    }
    
    private bool IsLakeToRiverTransition(float[,] riverMask, float[,] lakeMask, int x, int z, int chunkSize)
    {
        // Check if lake drains into river
        var downstream = FindDownstream(lakeMask, x, z, chunkSize);
        return downstream.HasValue && riverMask[downstream.Value.x, downstream.Value.z] > 0.3f;
    }
    
    private (int x, int z)? FindDownstream(float[,] mask, int x, int z, int chunkSize)
    {
        int centerHeight = mask[x, z];
        (int x, int z)? bestDownstream = null;
        float maxDownstream = float.MinValue;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                
                int nx = x + dx;
                int nz = z + dz;
                
                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                {
                    if (mask[nx, nz] > maxDownstream)
                    {
                        maxDownstream = mask[nx, nz];
                        bestDownstream = (nx, nz);
                    }
                }
            }
        }
        
        return bestDownstream;
    }
    
    private void EnhanceConnection(
        float[,] riverMask,
        float[,] lakeMask,
        int[,] heightMap,
        RiverLakeConnection connection,
        Random random)
    {
        // Smooth the transition between river and lake
        int radius = random.Next(2, 5);
        
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int nx = connection.X + dx;
                int nz = connection.Z + dz;
                
                if (nx >= 0 && nx < riverMask.GetLength(0) &&
                    nz >= 0 && nz < riverMask.GetLength(1))
                {
                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    double blend = 1.0 - (distance / (radius + 1));
                    
                    // Blend river and lake masks
                    float riverValue = riverMask[nx, nz];
                    float lakeValue = lakeMask[nx, nz];
                    float blendedValue = (float)(riverValue * (1.0 - blend) + lakeValue * blend);
                    
                    riverMask[nx, nz] = blendedValue;
                    lakeMask[nx, nz] = blendedValue;
                }
            }
        }
    }
}

public class RiverLakeConnection
{
    public int X { get; set; }
    public int Z { get; set; }
    public ConnectionType Type { get; set; }
    public float Strength { get; set; }
}

public enum ConnectionType
{
    RiverToLake,
    LakeToRiver
}
```

### 4. Enhanced Wetland Features

#### Current State
- Buffer-based wetlands
- Limited wetland variety

#### Proposed Improvements
```csharp
// Add multiple wetland types
public enum WetlandType
{
    Marsh,
    Swamp,
    Bog,
    Fen,
    Wetland
}

public class WetlandGenerator
{
    public float[,] GenerateWetlands(
        float[,] lakeMask,
        float[,] hydrologyMask,
        int[,] heightMap,
        int chunkX, int chunkZ,
        int chunkSize,
        int seaLevel,
        long worldSeed)
    {
        var random = new Random((int)(worldSeed ^ chunkX * 61 + chunkZ * 43));
        var wetlandMask = new float[chunkSize, chunkSize];
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float lakeStrength = lakeMask[x, z];
                float hydrologyStrength = hydrologyMask[x, z];
                int height = heightMap[x, z];
                int depthBelowSea = seaLevel - height;
                
                // Determine wetland type based on conditions
                WetlandType wetlandType = DetermineWetlandType(
                    lakeStrength, hydrologyStrength, depthBelowSea, random);
                
                // Generate wetland based on type
                float wetlandStrength = GenerateWetlandStrength(
                    wetlandType, lakeStrength, hydrologyStrength, depthBelowSea, random);
                
                wetlandMask[x, z] = wetlandStrength;
            }
        }
        
        return wetlandMask;
    }
    
    private WetlandType DetermineWetlandType(
        float lakeStrength,
        float hydrologyStrength,
        int depthBelowSea,
        Random random)
    {
        // Use conditions to determine wetland type
        double wetness = lakeStrength * 0.6 + hydrologyStrength * 0.4;
        
        if (depthBelowSea > 0 && depthBelowSea < 5)
        {
            // Shallow water - marsh or fen
            return random.NextDouble() < 0.6 ? WetlandType.Marsh : WetlandType.Fen;
        }
        else if (depthBelowSea >= 5 && depthBelowSea < 15)
        {
            // Medium depth - swamp or bog
            return random.NextDouble() < 0.5 ? WetlandType.Swamp : WetlandType.Bog;
        }
        else if (wetness > 0.3 && wetness < 0.7)
        {
            // Moderate wetness - wetland
            return WetlandType.Wetland;
        }
        else
        {
            // Default to wetland
            return WetlandType.Wetland;
        }
    }
    
    private float GenerateWetlandStrength(
        WetlandType type,
        float lakeStrength,
        float hydrologyStrength,
        int depthBelowSea,
        Random random)
    {
        double baseStrength = lakeStrength * 0.7 + hydrologyStrength * 0.3;
        double typeModifier = GetTypeModifier(type);
        double depthModifier = GetDepthModifier(depthBelowSea);
        double noise = SimplexNoise.Generate(
            lakeStrength.GetHashCode() * 0.1,
            hydrologyStrength.GetHashCode() * 0.1,
            1.0, 2, 1.0, 0.3, random.Next());
        
        return (float)(baseStrength * typeModifier * depthModifier * (1.0 + noise * 0.2));
    }
    
    private double GetTypeModifier(WetlandType type)
    {
        return type switch
        {
            WetlandType.Marsh => 1.2,
            WetlandType.Swamp => 1.0,
            WetlandType.Bog => 0.9,
            WetlandType.Fen => 1.1,
            WetlandType.Wetland => 1.0,
            _ => 1.0
        };
    }
    
    private double GetDepthModifier(int depthBelowSea)
    {
        if (depthBelowSea <= 0)
        {
            return 0.5; // No wetland above water
        }
        else if (depthBelowSea < 5)
        {
            return 1.3; // Shallow wetlands stronger
        }
        else if (depthBelowSea < 15)
        {
            return 1.0; // Medium depth normal
        }
        else
        {
            return 0.7; // Deep wetlands weaker
        }
    }
}
```

## Configuration Integration

### New Configuration Parameters

```json
{
  "caveImprovements": {
    "enableCaveConnectivity": true,
    "enableBiomeModifiers": true,
    "enableCaveDecorations": true,
    "enableUndergroundLakes": true,
    "minCaveVolume": 50,
    "maxConnectionDistance": 16,
    "stalactiteChance": 0.15,
    "stalagmiteChance": 0.15,
    "minStalactiteLength": 2,
    "maxStalactiteLength": 8,
    "minStalagmiteLength": 2,
    "maxStalagmiteLength": 6,
    "undergroundLakeChance": 0.05,
    "minLakeSpace": 20,
    "minLakeDepth": 3,
    "maxLakeDepth": 8,
    "minLakeRadius": 3,
    "maxLakeRadius": 6
  },
  "riverImprovements": {
    "enableMeanderEvolution": true,
    "enableSeasonalWidth": true,
    "enableBankErosion": true,
    "enableDeltaFormation": true,
    "meanderIterations": 10,
    "meanderStrength": 0.5,
    "erosionRate": 0.1,
    "sedimentDepositRate": 0.05,
    "erosionNoise": 0.3,
    "sedimentNoise": 0.2,
    "deltaFormationChance": 0.3,
    "minDeltaRadius": 4,
    "maxDeltaRadius": 12,
    "minDeltaLayers": 2,
    "maxDeltaLayers": 5,
    "deltaLayerHeight": 1,
    "deltaNoise": 0.5
  },
  "lakeImprovements": {
    "enableVariedShapes": true,
    "enableDepthProfiles": true,
    "enhanceConnectivity": true,
    "enhanceWetlands": true,
    "deltaFormationChance": 0.3,
    "minLakeDepth": 5,
    "maxLakeDepth": 30,
    "thermoclineStrength": 0.5,
    "wetlandBufferRadius": 8,
    "shorelineBlend": 0.25
  },
  "biomeCaveModifiers": {
    "default": {
      "minRadius": 2.0,
      "maxRadius": 8.0,
      "heightMultiplier": 1.0,
      "densityMultiplier": 1.0,
      "connectivityBonus": 0.0
    },
    "plains": {
      "minRadius": 2.5,
      "maxRadius": 10.0,
      "heightMultiplier": 1.2,
      "densityMultiplier": 1.1,
      "connectivityBonus": 0.1
    },
    "forest": {
      "minRadius": 1.5,
      "maxRadius": 6.0,
      "heightMultiplier": 0.8,
      "densityMultiplier": 1.3,
      "connectivityBonus": 0.2
    },
    "mountains": {
      "minRadius": 3.0,
      "maxRadius": 12.0,
      "heightMultiplier": 1.5,
      "densityMultiplier": 0.8,
      "connectivityBonus": 0.05
    }
  }
}
```

## Implementation Priority

### High Priority
1. **Cave Connectivity Enhancement**
   - Improves cave exploration experience
   - Reduces isolated cave pockets
   - Enhances gameplay flow

2. **River Meander Evolution**
   - Creates more natural river paths
   - Improves visual quality
   - Enhances navigation

3. **Lake Shape Variety**
   - Adds visual interest
   - Creates unique landmarks
   - Improves exploration

### Medium Priority
1. **Biome-Based Cave Modifiers**
   - Adds biome-specific cave characteristics
   - Enhances variety
   - Improves immersion

2. **River Bank Erosion**
   - Creates more realistic river banks
   - Adds dynamic terrain changes
   - Enhances visual quality

3. **Lake Depth Profiles**
   - Adds thermocline simulation
   - Improves water realism
   - Enhances fishing gameplay

### Low Priority
1. **Cave Decorations**
   - Adds stalactites and stalagmites
   - Enhances cave aesthetics
   - Improves exploration

2. **Delta Formation**
   - Creates river deltas
   - Adds visual interest
   - Enhances realism

3. **Enhanced Wetlands**
   - Adds wetland variety
   - Improves biome diversity
   - Enhances exploration

## Conclusion

The proposed improvements build upon the already sophisticated terrain generation system. They focus on:

1. **Enhanced Realism**
   - More natural cave connectivity
   - Improved river meandering
   - Varied lake shapes

2. **Increased Variety**
   - Biome-specific features
   - Multiple wetland types
   - Dynamic seasonal effects

3. **Better Gameplay**
   - Improved exploration
   - Enhanced navigation
   - More interesting landmarks

These improvements can be implemented incrementally, allowing for testing and refinement of each feature before moving to the next.

