using System;
using System.Collections.Generic;
using GameServerApp.Utils;
using GameServerApp.World;
using GameServerApp.Models;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced cave generator with multiple cave types, decorations, and connectivity systems.
    /// Supports Normal, Lava, Ice, Mushroom, and Crystal caves.
    /// </summary>
    public sealed class EnhancedCaveGenerator
    {
        private readonly EnhancedCaveConfig config;
        private readonly Random random;

        public EnhancedCaveGenerator(EnhancedCaveConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)(worldSeed ^ 0x5A3C7B01));
        }

        public CaveSystem GenerateCaves(
            int chunkX,
            int chunkZ,
            long worldSeed,
            int[,] heightMap,
            float[,] hydrologyMask,
            int seaLevel,
            BiomeType biome)
        {
            var caveSystem = new CaveSystem();
            
            // Generate cave cells
            var caveCells = GenerateCaveCells(chunkX, chunkZ, heightMap, hydrologyMask, seaLevel, biome);
            
            // Determine cave type for this chunk
            var caveType = DetermineCaveType(heightMap, seaLevel, biome);
            
            // Generate cave decorations
            var decorations = GenerateCaveDecorations(caveCells, caveType, biome);
            
            // Generate cave connections
            var connections = GenerateCaveConnections(caveCells, caveType);
            
            // Apply cellular automata smoothing
            ApplyCellularAutomata(caveCells, config.CellularAutomataIterations, config.CellularAutomataThreshold);
            
            caveSystem.CaveCells = caveCells;
            caveSystem.CaveType = caveType;
            caveSystem.Decorations = decorations;
            caveSystem.Connections = connections;
            
            return caveSystem;
        }

        private List<CaveCell> GenerateCaveCells(
            int chunkX,
            int chunkZ,
            int[,] heightMap,
            float[,] hydrologyMask,
            int seaLevel,
            BiomeType biome)
        {
            var cells = new List<CaveCell>();
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }
                    
                    float hydrology = hydrologyMask[x, z];
                    
                    // Calculate cave probability based on depth
                    for (int y = 1; y < Math.Min(surface - 1, worldHeight - 2); y++)
                    {
                        double depthFactor = 1.0 - (double)y / Math.Max(1, surface);
                        double caveProbability = config.BaseCaveProbability * config.CaveDensityMultiplier;
                        
                        // Apply depth-based modification
                        caveProbability *= (1.0 + depthFactor * config.DepthProbabilityMultiplier);
                        
                        // Apply biome-specific modification
                        caveProbability *= GetBiomeCaveMultiplier(biome);
                        
                        // Generate noise for cave generation
                        int worldX = chunkX * chunkSize + x;
                        int worldZ = chunkZ * chunkSize + z;
                        double noise = SimplexNoise.Generate(
                            worldX * config.CaveSizeMultiplier,
                            worldZ * config.CaveSizeMultiplier,
                            y * config.CaveVerticalMultiplier,
                            3,
                            1.0,
                            0.5,
                            (int)worldSeed);
                        
                        // Add domain warping for more natural cave shapes
                        var warp = SimplexNoise.DomainWarp(
                            worldX * 0.01,
                            worldZ * 0.01,
                            y * 0.02,
                            0.005,
                            0.01,
                            4.0,
                            2.5,
                            (int)worldSeed ^ 0x3A5F);
                        
                        double density = (noise + warp.dx) * 0.65 + warp.dz * 0.35;
                        
                        // Apply hydrology influence
                        double hydrologyInfluence = hydrology * config.HydrologyStabilityWeight;
                        density -= hydrologyInfluence;
                        
                        if (density > caveProbability)
                        {
                            cells.Add(new CaveCell
                            {
                                Position = new Vector3Int { X = x, Y = y, Z = z },
                                IsCave = true,
                                Depth = y,
                                Hydrology = hydrology
                            });
                        }
                    }
                }
            }
            
            return cells;
        }

        private CaveType DetermineCaveType(int[,] heightMap, int seaLevel, BiomeType biome)
        {
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Calculate average depth
            double totalDepth = 0;
            int count = 0;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    totalDepth += surface - seaLevel;
                    count++;
                }
            }
            
            double avgDepth = count > 0 ? totalDepth / count : 0;
            
            // Determine cave type based on depth and biome
            if (avgDepth > config.DeepCaveDepth)
            {
                return CaveType.Lava;
            }
            else if (avgDepth > config.MidDepthCaveDepth)
            {
                return biome == BiomeType.Snowy || biome == BiomeType.IceSpikes ? CaveType.Ice : CaveType.Normal;
            }
            else if (random.NextDouble() < config.MushroomCaveProbability)
            {
                return CaveType.Mushroom;
            }
            else if (random.NextDouble() < config.CrystalCaveProbability)
            {
                return CaveType.Crystal;
            }
            
            return CaveType.Normal;
        }

        private double GetBiomeCaveMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return config.DesertCaveMultiplier;
                case BiomeType.Jungle:
                    return config.JungleCaveMultiplier;
                case BiomeType.Swamp:
                    return config.SwampCaveMultiplier;
                case BiomeType.Mountains:
                    return config.MountainCaveMultiplier;
                default:
                    return 1.0;
            }
        }

        private List<CaveDecoration> GenerateCaveDecorations(
            List<CaveCell> caveCells,
            CaveType caveType,
            BiomeType biome)
        {
            var decorations = new List<CaveDecoration>();
            
            foreach (var cell in caveCells)
            {
                if (!cell.IsCave)
                {
                    continue;
                }
                
                // Generate stalactites on ceiling
                if (cell.Y > 1 && random.NextDouble() < config.StalactiteProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Stalactite,
                        CaveType = caveType
                    });
                }
                
                // Generate stalagmites on floor
                if (cell.Y < config.WorldHeight - 2 && random.NextDouble() < config.StalagmiteProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Stalagmite,
                        CaveType = caveType
                    });
                }
                
                // Generate vines in humid caves
                if (cell.Hydrology > 0.5 && random.NextDouble() < config.VineProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Vine,
                        CaveType = caveType
                    });
                }
                
                // Generate moss in wet caves
                if (cell.Hydrology > 0.6 && random.NextDouble() < config.MossProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Moss,
                        CaveType = caveType
                    });
                }
                
                // Generate mineral deposits
                if (random.NextDouble() < config.MineralDepositProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.MineralDeposit,
                        CaveType = caveType
                    });
                }
            }
            
            return decorations;
        }

        private List<CaveConnection> GenerateCaveConnections(
            List<CaveCell> caveCells,
            CaveType caveType)
        {
            var connections = new List<CaveConnection>();
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Generate cave-to-cave connections
            if (random.NextDouble() < config.CaveToCaveConnectionProbability)
            {
                // Find cave cells and connect them
                var cavePositions = new List<Vector3Int>();
                foreach (var cell in caveCells)
                {
                    if (cell.IsCave)
                    {
                        cavePositions.Add(cell.Position);
                    }
                }
                
                // Create connections between nearby caves
                for (int i = 0; i < cavePositions.Count; i++)
                {
                    for (int j = i + 1; j < cavePositions.Count; j++)
                    {
                        var pos1 = cavePositions[i];
                        var pos2 = cavePositions[j];
                        
                        int distance = Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y) + Math.Abs(pos1.Z - pos2.Z);
                        
                        if (distance >= config.MinConnectionDistance && distance <= config.MaxConnectionDistance)
                        {
                            connections.Add(new CaveConnection
                            {
                                From = pos1,
                                To = pos2,
                                Type = CaveConnectionType.CaveToCave,
                                Distance = distance
                            });
                        }
                    }
                }
            }
            
            // Generate cave-to-surface connections
            if (random.NextDouble() < config.CaveToSurfaceConnectionProbability)
            {
                foreach (var cell in caveCells)
                {
                    if (!cell.IsCave || cell.Y < 5)
                    {
                        continue;
                    }
                    
                    // Check if this cave cell is near surface
                    int surfaceY = heightMap[cell.Position.X, cell.Position.Z];
                    if (surfaceY - cell.Y <= 10 && random.NextDouble() < 0.3)
                    {
                        connections.Add(new CaveConnection
                        {
                            From = cell.Position,
                            To = new Vector3Int { X = cell.Position.X, Y = surfaceY, Z = cell.Position.Z },
                            Type = CaveConnectionType.CaveToSurface,
                            Distance = surfaceY - cell.Y
                        });
                    }
                }
            }
            
            return connections;
        }

        private void ApplyCellularAutomata(List<CaveCell> cells, int iterations, int threshold)
        {
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Create 3D grid
            var grid = new bool[chunkSize, worldHeight, chunkSize];
            foreach (var cell in cells)
            {
                if (cell.Position.X >= 0 && cell.Position.X < chunkSize &&
                    cell.Position.Y >= 0 && cell.Position.Y < worldHeight &&
                    cell.Position.Z >= 0 && cell.Position.Z < chunkSize)
                {
                    grid[cell.Position.X, cell.Position.Y, cell.Position.Z] = cell.IsCave;
                }
            }
            
            // Apply cellular automata iterations
            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = (bool[,])grid.Clone();
                
                for (int x = 1; x < chunkSize - 1; x++)
                {
                    for (int y = 1; y < worldHeight - 1; y++)
                    {
                        for (int z = 1; z < chunkSize - 1; z++)
                        {
                            int neighbors = CountCaveNeighbors(buffer, x, y, z);
                            if (neighbors >= threshold)
                            {
                                grid[x, y, z] = true;
                            }
                            else if (neighbors <= 26 - threshold)
                            {
                                grid[x, y, z] = false;
                            }
                        }
                    }
                }
            }
            
            // Update cells
            foreach (var cell in cells)
            {
                if (cell.Position.X >= 0 && cell.Position.X < chunkSize &&
                    cell.Position.Y >= 0 && cell.Position.Y < worldHeight &&
                    cell.Position.Z >= 0 && cell.Position.Z < chunkSize)
                {
                    cell.IsCave = grid[cell.Position.X, cell.Position.Y, cell.Position.Z];
                }
            }
        }

        private int CountCaveNeighbors(bool[,,] grid, int x, int y, int z)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < grid.GetLength(0) &&
                            ny >= 0 && ny < grid.GetLength(1) &&
                            nz >= 0 && nz < grid.GetLength(2))
                        {
                            if (grid[nx, ny, nz])
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            
            return count;
        }

        // Data structures
        public class CaveSystem
        {
            public List<CaveCell> CaveCells { get; set; }
            public CaveType CaveType { get; set; }
            public List<CaveDecoration> Decorations { get; set; }
            public List<CaveConnection> Connections { get; set; }
        }

        public class CaveCell
        {
            public Vector3Int Position { get; set; }
            public bool IsCave { get; set; }
            public int Depth { get; set; }
            public float Hydrology { get; set; }
        }

        public class CaveDecoration
        {
            public Vector3Int Position { get; set; }
            public CaveDecorationType Type { get; set; }
            public CaveType CaveType { get; set; }
        }

        public class CaveConnection
        {
            public Vector3Int From { get; set; }
            public Vector3Int To { get; set; }
            public CaveConnectionType Type { get; set; }
            public int Distance { get; set; }
        }

        public enum CaveType
        {
            Normal,
            Lava,
            Ice,
            Mushroom,
            Crystal
        }

        public enum CaveDecorationType
        {
            Stalactite,
            Stalagmite,
            Vine,
            Moss,
            MineralDeposit
        }

        public enum CaveConnectionType
        {
            CaveToCave,
            CaveToSurface
        }

        // Configuration
        public class EnhancedCaveConfig
        {
            // Chunk parameters
            public int ChunkSize { get; set; } = 16;
            public int WorldHeight { get; set; } = 128;
            
            // Cave generation parameters
            public double BaseCaveProbability { get; set; } = 0.05;
            public double CaveDensityMultiplier { get; set; } = 1.0;
            public double CaveSizeMultiplier { get; set; } = 1.0;
            public double CaveVerticalMultiplier { get; set; } = 0.5;
            public double DepthProbabilityMultiplier { get; set; } = 0.5;
            
            // Cave type parameters
            public double DeepCaveDepth { get; set; } = 50.0;
            public double MidDepthCaveDepth { get; set; } = 30.0;
            public double MushroomCaveProbability { get; set; } = 0.05;
            public double CrystalCaveProbability { get; set; } = 0.02;
            
            // Biome-specific parameters
            public double DesertCaveMultiplier { get; set; } = 0.5;
            public double JungleCaveMultiplier { get; set; } = 1.2;
            public double SwampCaveMultiplier { get; set; } = 0.8;
            public double MountainCaveMultiplier { get; set; } = 1.5;
            
            // Decoration parameters
            public double StalactiteProbability { get; set; } = 0.1;
            public double StalagmiteProbability { get; set; } = 0.1;
            public double VineProbability { get; set; } = 0.05;
            public double MossProbability { get; set; } = 0.08;
            public double MineralDepositProbability { get; set; } = 0.03;
            
            // Connectivity parameters
            public double CaveToCaveConnectionProbability { get; set; } = 0.2;
            public double CaveToSurfaceConnectionProbability { get; set; } = 0.05;
            public int MinConnectionDistance { get; set; } = 5;
            public int MaxConnectionDistance { get; set; } = 50;
            
            // Hydrology parameters
            public double HydrologyStabilityWeight { get; set; } = 0.3;
            
            // Cellular automata parameters
            public int CellularAutomataIterations { get; set; } = 3;
            public int CellularAutomataThreshold { get; set; } = 13;
        }
    }
}
using System.Collections.Generic;
using GameServerApp.Utils;
using GameServerApp.World;
using GameServerApp.Models;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced cave generator with multiple cave types, decorations, and connectivity systems.
    /// Supports Normal, Lava, Ice, Mushroom, and Crystal caves.
    /// </summary>
    public sealed class EnhancedCaveGenerator
    {
        private readonly EnhancedCaveConfig config;
        private readonly Random random;

        public EnhancedCaveGenerator(EnhancedCaveConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)(worldSeed ^ 0x5A3C7B01));
        }

        public CaveSystem GenerateCaves(
            int chunkX,
            int chunkZ,
            long worldSeed,
            int[,] heightMap,
            float[,] hydrologyMask,
            int seaLevel,
            BiomeType biome)
        {
            var caveSystem = new CaveSystem();
            
            // Generate cave cells
            var caveCells = GenerateCaveCells(chunkX, chunkZ, heightMap, hydrologyMask, seaLevel, biome);
            
            // Determine cave type for this chunk
            var caveType = DetermineCaveType(heightMap, seaLevel, biome);
            
            // Generate cave decorations
            var decorations = GenerateCaveDecorations(caveCells, caveType, biome);
            
            // Generate cave connections
            var connections = GenerateCaveConnections(caveCells, caveType);
            
            // Apply cellular automata smoothing
            ApplyCellularAutomata(caveCells, config.CellularAutomataIterations, config.CellularAutomataThreshold);
            
            caveSystem.CaveCells = caveCells;
            caveSystem.CaveType = caveType;
            caveSystem.Decorations = decorations;
            caveSystem.Connections = connections;
            
            return caveSystem;
        }

        private List<CaveCell> GenerateCaveCells(
            int chunkX,
            int chunkZ,
            int[,] heightMap,
            float[,] hydrologyMask,
            int seaLevel,
            BiomeType biome)
        {
            var cells = new List<CaveCell>();
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }
                    
                    float hydrology = hydrologyMask[x, z];
                    
                    // Calculate cave probability based on depth
                    for (int y = 1; y < Math.Min(surface - 1, worldHeight - 2); y++)
                    {
                        double depthFactor = 1.0 - (double)y / Math.Max(1, surface);
                        double caveProbability = config.BaseCaveProbability * config.CaveDensityMultiplier;
                        
                        // Apply depth-based modification
                        caveProbability *= (1.0 + depthFactor * config.DepthProbabilityMultiplier);
                        
                        // Apply biome-specific modification
                        caveProbability *= GetBiomeCaveMultiplier(biome);
                        
                        // Generate noise for cave generation
                        int worldX = chunkX * chunkSize + x;
                        int worldZ = chunkZ * chunkSize + z;
                        double noise = SimplexNoise.Generate(
                            worldX * config.CaveSizeMultiplier,
                            worldZ * config.CaveSizeMultiplier,
                            y * config.CaveVerticalMultiplier,
                            3,
                            1.0,
                            0.5,
                            (int)worldSeed);
                        
                        // Add domain warping for more natural cave shapes
                        var warp = SimplexNoise.DomainWarp(
                            worldX * 0.01,
                            worldZ * 0.01,
                            y * 0.02,
                            0.005,
                            0.01,
                            4.0,
                            2.5,
                            (int)worldSeed ^ 0x3A5F);
                        
                        double density = (noise + warp.dx) * 0.65 + warp.dz * 0.35;
                        
                        // Apply hydrology influence
                        double hydrologyInfluence = hydrology * config.HydrologyStabilityWeight;
                        density -= hydrologyInfluence;
                        
                        if (density > caveProbability)
                        {
                            cells.Add(new CaveCell
                            {
                                Position = new Vector3Int { X = x, Y = y, Z = z },
                                IsCave = true,
                                Depth = y,
                                Hydrology = hydrology
                            });
                        }
                    }
                }
            }
            
            return cells;
        }

        private CaveType DetermineCaveType(int[,] heightMap, int seaLevel, BiomeType biome)
        {
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Calculate average depth
            double totalDepth = 0;
            int count = 0;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    totalDepth += surface - seaLevel;
                    count++;
                }
            }
            
            double avgDepth = count > 0 ? totalDepth / count : 0;
            
            // Determine cave type based on depth and biome
            if (avgDepth > config.DeepCaveDepth)
            {
                return CaveType.Lava;
            }
            else if (avgDepth > config.MidDepthCaveDepth)
            {
                return biome == BiomeType.Snowy || biome == BiomeType.IceSpikes ? CaveType.Ice : CaveType.Normal;
            }
            else if (random.NextDouble() < config.MushroomCaveProbability)
            {
                return CaveType.Mushroom;
            }
            else if (random.NextDouble() < config.CrystalCaveProbability)
            {
                return CaveType.Crystal;
            }
            
            return CaveType.Normal;
        }

        private double GetBiomeCaveMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return config.DesertCaveMultiplier;
                case BiomeType.Jungle:
                    return config.JungleCaveMultiplier;
                case BiomeType.Swamp:
                    return config.SwampCaveMultiplier;
                case BiomeType.Mountains:
                    return config.MountainCaveMultiplier;
                default:
                    return 1.0;
            }
        }

        private List<CaveDecoration> GenerateCaveDecorations(
            List<CaveCell> caveCells,
            CaveType caveType,
            BiomeType biome)
        {
            var decorations = new List<CaveDecoration>();
            
            foreach (var cell in caveCells)
            {
                if (!cell.IsCave)
                {
                    continue;
                }
                
                // Generate stalactites on ceiling
                if (cell.Y > 1 && random.NextDouble() < config.StalactiteProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Stalactite,
                        CaveType = caveType
                    });
                }
                
                // Generate stalagmites on floor
                if (cell.Y < config.WorldHeight - 2 && random.NextDouble() < config.StalagmiteProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Stalagmite,
                        CaveType = caveType
                    });
                }
                
                // Generate vines in humid caves
                if (cell.Hydrology > 0.5 && random.NextDouble() < config.VineProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Vine,
                        CaveType = caveType
                    });
                }
                
                // Generate moss in wet caves
                if (cell.Hydrology > 0.6 && random.NextDouble() < config.MossProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.Moss,
                        CaveType = caveType
                    });
                }
                
                // Generate mineral deposits
                if (random.NextDouble() < config.MineralDepositProbability)
                {
                    decorations.Add(new CaveDecoration
                    {
                        Position = cell.Position,
                        Type = CaveDecorationType.MineralDeposit,
                        CaveType = caveType
                    });
                }
            }
            
            return decorations;
        }

        private List<CaveConnection> GenerateCaveConnections(
            List<CaveCell> caveCells,
            CaveType caveType)
        {
            var connections = new List<CaveConnection>();
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Generate cave-to-cave connections
            if (random.NextDouble() < config.CaveToCaveConnectionProbability)
            {
                // Find cave cells and connect them
                var cavePositions = new List<Vector3Int>();
                foreach (var cell in caveCells)
                {
                    if (cell.IsCave)
                    {
                        cavePositions.Add(cell.Position);
                    }
                }
                
                // Create connections between nearby caves
                for (int i = 0; i < cavePositions.Count; i++)
                {
                    for (int j = i + 1; j < cavePositions.Count; j++)
                    {
                        var pos1 = cavePositions[i];
                        var pos2 = cavePositions[j];
                        
                        int distance = Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y) + Math.Abs(pos1.Z - pos2.Z);
                        
                        if (distance >= config.MinConnectionDistance && distance <= config.MaxConnectionDistance)
                        {
                            connections.Add(new CaveConnection
                            {
                                From = pos1,
                                To = pos2,
                                Type = CaveConnectionType.CaveToCave,
                                Distance = distance
                            });
                        }
                    }
                }
            }
            
            // Generate cave-to-surface connections
            if (random.NextDouble() < config.CaveToSurfaceConnectionProbability)
            {
                foreach (var cell in caveCells)
                {
                    if (!cell.IsCave || cell.Y < 5)
                    {
                        continue;
                    }
                    
                    // Check if this cave cell is near surface
                    int surfaceY = heightMap[cell.Position.X, cell.Position.Z];
                    if (surfaceY - cell.Y <= 10 && random.NextDouble() < 0.3)
                    {
                        connections.Add(new CaveConnection
                        {
                            From = cell.Position,
                            To = new Vector3Int { X = cell.Position.X, Y = surfaceY, Z = cell.Position.Z },
                            Type = CaveConnectionType.CaveToSurface,
                            Distance = surfaceY - cell.Y
                        });
                    }
                }
            }
            
            return connections;
        }

        private void ApplyCellularAutomata(List<CaveCell> cells, int iterations, int threshold)
        {
            int chunkSize = config.ChunkSize;
            int worldHeight = config.WorldHeight;
            
            // Create 3D grid
            var grid = new bool[chunkSize, worldHeight, chunkSize];
            foreach (var cell in cells)
            {
                if (cell.Position.X >= 0 && cell.Position.X < chunkSize &&
                    cell.Position.Y >= 0 && cell.Position.Y < worldHeight &&
                    cell.Position.Z >= 0 && cell.Position.Z < chunkSize)
                {
                    grid[cell.Position.X, cell.Position.Y, cell.Position.Z] = cell.IsCave;
                }
            }
            
            // Apply cellular automata iterations
            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = (bool[,])grid.Clone();
                
                for (int x = 1; x < chunkSize - 1; x++)
                {
                    for (int y = 1; y < worldHeight - 1; y++)
                    {
                        for (int z = 1; z < chunkSize - 1; z++)
                        {
                            int neighbors = CountCaveNeighbors(buffer, x, y, z);
                            if (neighbors >= threshold)
                            {
                                grid[x, y, z] = true;
                            }
                            else if (neighbors <= 26 - threshold)
                            {
                                grid[x, y, z] = false;
                            }
                        }
                    }
                }
            }
            
            // Update cells
            foreach (var cell in cells)
            {
                if (cell.Position.X >= 0 && cell.Position.X < chunkSize &&
                    cell.Position.Y >= 0 && cell.Position.Y < worldHeight &&
                    cell.Position.Z >= 0 && cell.Position.Z < chunkSize)
                {
                    cell.IsCave = grid[cell.Position.X, cell.Position.Y, cell.Position.Z];
                }
            }
        }

        private int CountCaveNeighbors(bool[,,] grid, int x, int y, int z)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < grid.GetLength(0) &&
                            ny >= 0 && ny < grid.GetLength(1) &&
                            nz >= 0 && nz < grid.GetLength(2))
                        {
                            if (grid[nx, ny, nz])
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            
            return count;
        }

        // Data structures
        public class CaveSystem
        {
            public List<CaveCell> CaveCells { get; set; }
            public CaveType CaveType { get; set; }
            public List<CaveDecoration> Decorations { get; set; }
            public List<CaveConnection> Connections { get; set; }
        }

        public class CaveCell
        {
            public Vector3Int Position { get; set; }
            public bool IsCave { get; set; }
            public int Depth { get; set; }
            public float Hydrology { get; set; }
        }

        public class CaveDecoration
        {
            public Vector3Int Position { get; set; }
            public CaveDecorationType Type { get; set; }
            public CaveType CaveType { get; set; }
        }

        public class CaveConnection
        {
            public Vector3Int From { get; set; }
            public Vector3Int To { get; set; }
            public CaveConnectionType Type { get; set; }
            public int Distance { get; set; }
        }

        public enum CaveType
        {
            Normal,
            Lava,
            Ice,
            Mushroom,
            Crystal
        }

        public enum CaveDecorationType
        {
            Stalactite,
            Stalagmite,
            Vine,
            Moss,
            MineralDeposit
        }

        public enum CaveConnectionType
        {
            CaveToCave,
            CaveToSurface
        }

        // Configuration
        public class EnhancedCaveConfig
        {
            // Chunk parameters
            public int ChunkSize { get; set; } = 16;
            public int WorldHeight { get; set; } = 128;
            
            // Cave generation parameters
            public double BaseCaveProbability { get; set; } = 0.05;
            public double CaveDensityMultiplier { get; set; } = 1.0;
            public double CaveSizeMultiplier { get; set; } = 1.0;
            public double CaveVerticalMultiplier { get; set; } = 0.5;
            public double DepthProbabilityMultiplier { get; set; } = 0.5;
            
            // Cave type parameters
            public double DeepCaveDepth { get; set; } = 50.0;
            public double MidDepthCaveDepth { get; set; } = 30.0;
            public double MushroomCaveProbability { get; set; } = 0.05;
            public double CrystalCaveProbability { get; set; } = 0.02;
            
            // Biome-specific parameters
            public double DesertCaveMultiplier { get; set; } = 0.5;
            public double JungleCaveMultiplier { get; set; } = 1.2;
            public double SwampCaveMultiplier { get; set; } = 0.8;
            public double MountainCaveMultiplier { get; set; } = 1.5;
            
            // Decoration parameters
            public double StalactiteProbability { get; set; } = 0.1;
            public double StalagmiteProbability { get; set; } = 0.1;
            public double VineProbability { get; set; } = 0.05;
            public double MossProbability { get; set; } = 0.08;
            public double MineralDepositProbability { get; set; } = 0.03;
            
            // Connectivity parameters
            public double CaveToCaveConnectionProbability { get; set; } = 0.2;
            public double CaveToSurfaceConnectionProbability { get; set; } = 0.05;
            public int MinConnectionDistance { get; set; } = 5;
            public int MaxConnectionDistance { get; set; } = 50;
            
            // Hydrology parameters
            public double HydrologyStabilityWeight { get; set; } = 0.3;
            
            // Cellular automata parameters
            public int CellularAutomataIterations { get; set; } = 3;
            public int CellularAutomataThreshold { get; set; } = 13;
        }
    }
}

