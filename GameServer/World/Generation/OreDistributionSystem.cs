#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Ore distribution system with configurable rarity and distribution patterns
    /// Generates realistic ore deposits based on geological parameters
    /// </summary>
    public class OreDistributionSystem
    {
        private readonly ILogger<OreDistributionSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly Random random;
        
        // Noise generators for ore distribution
        private readonly FastNoise ironNoise;
        private readonly FastNoise goldNoise;
        private readonly FastNoise diamondNoise;
        private readonly FastNoise coalNoise;
        private readonly FastNoise copperNoise;
        private readonly FastNoise emeraldNoise;
        private readonly FastNoise redstoneNoise;
        private readonly FastNoise lapisNoise;
        
        // Ore definitions
        private readonly Dictionary<OreType, OreDefinition> oreDefinitions;
        
        public OreDistributionSystem(ILogger<OreDistributionSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.random = new Random(config.Seed);
            
            // Initialize noise generators for each ore type
            InitializeNoiseGenerators();
            
            // Initialize ore definitions
            InitializeOreDefinitions();
            
            logger.LogInformation("[OreDistributionSystem] Initialized with seed: {Seed}", config.Seed);
        }
        
        /// <summary>
        /// Generates ore distribution for a chunk
        /// </summary>
        public async Task<OreData> GenerateOreDataAsync(int chunkX, int chunkZ, ChunkData chunkData, BiomeData biomeData, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            
            var size = chunkData.Size;
            var oreData = new OreData(size);
            
            // Generate ore deposits for each ore type
            foreach (var oreType in Enum.GetValues<OreType>())
            {
                if (!config.Ores.IsEnabled(oreType)) continue;
                
                var oreDefinition = oreDefinitions[oreType];
                GenerateOreDeposits(oreType, oreDefinition, chunkX, chunkZ, chunkData, biomeData, oreData);
            }
            
            // Apply post-processing
            ApplyOrePostProcessing(oreData);
            
            return oreData;
        }
        
        /// <summary>
        /// Generates ore deposits for a specific ore type
        /// </summary>
        private void GenerateOreDeposits(OreType oreType, OreDefinition oreDefinition, int chunkX, int chunkZ, ChunkData chunkData, BiomeData biomeData, OreData oreData)
        {
            var size = chunkData.Size;
            var noiseGenerator = GetNoiseGenerator(oreType);
            
            // Calculate number of veins to generate
            var veinCount = CalculateVeinCount(oreDefinition, size);
            
            for (int veinIndex = 0; veinIndex < veinCount; veinIndex++)
            {
                // Generate vein position
                var veinPosition = GenerateVeinPosition(oreDefinition, chunkX, chunkZ, size);
                
                // Check if vein is valid (within height range and biome constraints)
                if (IsValidVeinPosition(veinPosition, oreDefinition, chunkData, biomeData))
                {
                    // Generate vein
                    GenerateVein(oreType, oreDefinition, veinPosition, chunkData, oreData);
                }
            }
        }
        
        /// <summary>
        /// Calculates the number of veins to generate for an ore type
        /// </summary>
        private int CalculateVeinCount(OreDefinition oreDefinition, int chunkSize)
        {
            // Base vein count adjusted by rarity
            var baseVeinCount = (chunkSize * chunkSize) / (oreDefinition.VeinFrequency * 100);
            
            // Apply rarity modifier
            var adjustedCount = baseVeinCount * oreDefinition.RarityMultiplier;
            
            // Add random variation
            var variation = random.Next(-1, 2);
            
            return Math.Max(0, (int)adjustedCount + variation);
        }
        
        /// <summary>
        /// Generates a position for a vein
        /// </summary>
        private Vector3Int GenerateVeinPosition(OreDefinition oreDefinition, int chunkX, int chunkZ, int chunkSize)
        {
            // Random position within chunk
            var localX = random.Next(0, chunkSize);
            var localZ = random.Next(0, chunkSize);
            
            // Height within ore's range
            var minHeight = oreDefinition.MinHeight;
            var maxHeight = oreDefinition.MaxHeight;
            var height = random.Next(minHeight, maxHeight + 1);
            
            return new Vector3Int(localX, height, localZ);
        }
        
        /// <summary>
        /// Checks if a vein position is valid
        /// </summary>
        private bool IsValidVeinPosition(Vector3Int position, OreDefinition oreDefinition, ChunkData chunkData, BiomeData biomeData)
        {
            // Check height constraints
            if (position.Y < oreDefinition.MinHeight || position.Y > oreDefinition.MaxHeight)
                return false;
            
            // Check biome constraints
            if (oreDefinition.AllowedBiomes.Count > 0)
            {
                var biomeType = biomeData.BiomeMap[position.X, position.Z];
                if (!oreDefinition.AllowedBiomes.Contains(biomeType))
                    return false;
            }
            
            // Check if position is solid (not air or water)
            var heightValue = chunkData.HeightMap[position.X, position.Z];
            if (position.Y > heightValue)
                return false;
            
            // Check if position is not in a cave
            if (chunkData.CaveMap != null && position.X < chunkSize && position.Z < chunkSize && position.Y < chunkSize)
            {
                if (chunkData.CaveMap[position.X, position.Y, position.Z])
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Generates a vein at the specified position
        /// </summary>
        private void GenerateVein(OreType oreType, OreDefinition oreDefinition, Vector3Int position, ChunkData chunkData, OreData oreData)
        {
            var veinSize = random.Next(oreDefinition.MinVeinSize, oreDefinition.MaxVeinSize + 1);
            
            // Generate vein using blob or line pattern
            switch (oreDefinition.VeinPattern)
            {
                case VeinPattern.Blob:
                    GenerateBlobVein(oreType, position, veinSize, oreData);
                    break;
                case VeinPattern.Line:
                    GenerateLineVein(oreType, position, veinSize, oreData);
                    break;
                case VeinPattern.Cluster:
                    GenerateClusterVein(oreType, position, veinSize, oreData);
                    break;
            }
        }
        
        /// <summary>
        /// Generates a blob-shaped vein
        /// </summary>
        private void GenerateBlobVein(OreType oreType, Vector3Int center, int veinSize, OreData oreData)
        {
            var radius = veinSize / 2;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        var distance = Math.Sqrt(x * x + y * y + z * z);
                        if (distance <= radius)
                        {
                            var pos = new Vector3Int(center.X + x, center.Y + y, center.Z + z);
                            if (IsValidOrePosition(pos))
                            {
                                oreData.SetOre(pos.X, pos.Y, pos.Z, oreType);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generates a line-shaped vein
        /// </summary>
        private void GenerateLineVein(OreType oreType, Vector3Int start, int veinSize, OreData oreData)
        {
            var direction = new Vector3Int(
                random.Next(-1, 2),
                random.Next(-1, 2),
                random.Next(-1, 2)
            );
            
            var current = start;
            
            for (int i = 0; i < veinSize; i++)
            {
                if (IsValidOrePosition(current))
                {
                    oreData.SetOre(current.X, current.Y, current.Z, oreType);
                    
                    // Add some thickness to the vein
                    for (int j = 0; j < 2; j++)
                    {
                        var offset = new Vector3Int(
                            random.Next(-1, 2),
                            random.Next(-1, 2),
                            random.Next(-1, 2)
                        );
                        var thickPos = current + offset;
                        if (IsValidOrePosition(thickPos))
                        {
                            oreData.SetOre(thickPos.X, thickPos.Y, thickPos.Z, oreType);
                        }
                    }
                }
                
                current = current + direction;
            }
        }
        
        /// <summary>
        /// Generates a cluster-shaped vein
        /// </summary>
        private void GenerateClusterVein(OreType oreType, Vector3Int center, int veinSize, OreData oreData)
        {
            var clusterCount = veinSize / 3;
            
            for (int i = 0; i < clusterCount; i++)
            {
                var offset = new Vector3Int(
                    random.Next(-3, 4),
                    random.Next(-3, 4),
                    random.Next(-3, 4)
                );
                var clusterCenter = center + offset;
                
                // Generate small blob at cluster center
                GenerateBlobVein(oreType, clusterCenter, 3, oreData);
            }
        }
        
        /// <summary>
        /// Checks if a position is valid for ore placement
        /// </summary>
        private bool IsValidOrePosition(Vector3Int position)
        {
            // Check bounds
            if (position.X < 0 || position.X >= config.ChunkSize ||
                position.Y < 0 || position.Y >= config.ChunkSize ||
                position.Z < 0 || position.Z >= config.ChunkSize)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Applies post-processing to ore distribution
        /// </summary>
        private void ApplyOrePostProcessing(OreData oreData)
        {
            // Remove isolated ore blocks
            RemoveIsolatedOres(oreData);
            
            // Apply smoothing if enabled
            if (config.Ores.EnableSmoothing)
            {
                SmoothOreDistribution(oreData);
            }
        }
        
        /// <summary>
        /// Removes isolated ore blocks
        /// </summary>
        private void RemoveIsolatedOres(OreData oreData)
        {
            var size = oreData.Size;
            var toRemove = new List<Vector3Int>();
            
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int z = 1; z < size - 1; z++)
                    {
                        var oreType = oreData.GetOre(x, y, z);
                        if (oreType == OreType.None) continue;
                        
                        // Count neighboring ore blocks
                        var neighborCount = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dz = -1; dz <= 1; dz++)
                                {
                                    if (dx == 0 && dy == 0 && dz == 0) continue;
                                    
                                    var neighborOre = oreData.GetOre(x + dx, y + dy, z + dz);
                                    if (neighborOre == oreType)
                                        neighborCount++;
                                }
                            }
                        }
                        
                        // Remove if too few neighbors
                        if (neighborCount < 2)
                        {
                            toRemove.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
            
            // Remove isolated ores
            foreach (var pos in toRemove)
            {
                oreData.SetOre(pos.X, pos.Y, pos.Z, OreType.None);
            }
        }
        
        /// <summary>
        /// Smooths ore distribution
        /// </summary>
        private void SmoothOreDistribution(OreData oreData)
        {
            var size = oreData.Size;
            var smoothedData = new OreData(size);
            
            for (int x = 1; x < size - 1; x++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int z = 1; z < size - 1; z++)
                    {
                        var oreType = oreData.GetOre(x, y, z);
                        if (oreType == OreType.None) continue;
                        
                        // Count neighboring ore types
                        var oreCounts = new Dictionary<OreType, int>();
                        
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dz = -1; dz <= 1; dz++)
                                {
                                    var neighborOre = oreData.GetOre(x + dx, y + dy, z + dz);
                                    if (neighborOre != OreType.None)
                                    {
                                        oreCounts[neighborOre] = oreCounts.GetValueOrDefault(neighborOre, 0) + 1;
                                    }
                                }
                            }
                        }
                        
                        // Keep ore if it has enough neighbors of the same type
                        if (oreCounts.GetValueOrDefault(oreType, 0) >= 4)
                        {
                            smoothedData.SetOre(x, y, z, oreType);
                        }
                    }
                }
            }
            
            // Copy smoothed data back
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        var smoothedOre = smoothedData.GetOre(x, y, z);
                        if (smoothedOre != OreType.None)
                        {
                            oreData.SetOre(x, y, z, smoothedOre);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the noise generator for a specific ore type
        /// </summary>
        private FastNoise GetNoiseGenerator(OreType oreType)
        {
            return oreType switch
            {
                OreType.Iron => ironNoise,
                OreType.Gold => goldNoise,
                OreType.Diamond => diamondNoise,
                OreType.Coal => coalNoise,
                OreType.Copper => copperNoise,
                OreType.Emerald => emeraldNoise,
                OreType.Redstone => redstoneNoise,
                OreType.Lapis => lapisNoise,
                _ => throw new ArgumentException($"Unknown ore type: {oreType}")
            };
        }
        
        /// <summary>
        /// Initializes noise generators
        /// </summary>
        private void InitializeNoiseGenerators()
        {
            ironNoise = new FastNoise(random.Next());
            ironNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            ironNoise.SetFrequency(config.Ores.IronFrequency);
            
            goldNoise = new FastNoise(random.Next());
            goldNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            goldNoise.SetFrequency(config.Ores.GoldFrequency);
            
            diamondNoise = new FastNoise(random.Next());
            diamondNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            diamondNoise.SetFrequency(config.Ores.DiamondFrequency);
            
            coalNoise = new FastNoise(random.Next());
            coalNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            coalNoise.SetFrequency(config.Ores.CoalFrequency);
            
            copperNoise = new FastNoise(random.Next());
            copperNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            copperNoise.SetFrequency(config.Ores.CopperFrequency);
            
            emeraldNoise = new FastNoise(random.Next());
            emeraldNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            emeraldNoise.SetFrequency(config.Ores.EmeraldFrequency);
            
            redstoneNoise = new FastNoise(random.Next());
            redstoneNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            redstoneNoise.SetFrequency(config.Ores.RedstoneFrequency);
            
            lapisNoise = new FastNoise(random.Next());
            lapisNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            lapisNoise.SetFrequency(config.Ores.LapisFrequency);
        }
        
        /// <summary>
        /// Initializes ore definitions
        /// </summary>
        private void InitializeOreDefinitions()
        {
            oreDefinitions = new Dictionary<OreType, OreDefinition>
            {
                [OreType.Coal] = new OreDefinition
                {
                    Type = OreType.Coal,
                    Name = "Coal",
                    RarityMultiplier = 1.0f,
                    MinHeight = 0,
                    MaxHeight = 128,
                    MinVeinSize = 4,
                    MaxVeinSize = 8,
                    VeinFrequency = 20,
                    VeinPattern = VeinPattern.Blob,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Iron] = new OreDefinition
                {
                    Type = OreType.Iron,
                    Name = "Iron",
                    RarityMultiplier = 0.8f,
                    MinHeight = 0,
                    MaxHeight = 64,
                    MinVeinSize = 3,
                    MaxVeinSize = 6,
                    VeinFrequency = 25,
                    VeinPattern = VeinPattern.Blob,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Copper] = new OreDefinition
                {
                    Type = OreType.Copper,
                    Name = "Copper",
                    RarityMultiplier = 0.6f,
                    MinHeight = 0,
                    MaxHeight = 96,
                    MinVeinSize = 2,
                    MaxVeinSize = 5,
                    VeinFrequency = 30,
                    VeinPattern = VeinPattern.Line,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Gold] = new OreDefinition
                {
                    Type = OreType.Gold,
                    Name = "Gold",
                    RarityMultiplier = 0.3f,
                    MinHeight = 0,
                    MaxHeight = 32,
                    MinVeinSize = 2,
                    MaxVeinSize = 4,
                    VeinFrequency = 40,
                    VeinPattern = VeinPattern.Blob,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Redstone] = new OreDefinition
                {
                    Type = OreType.Redstone,
                    Name = "Redstone",
                    RarityMultiplier = 0.4f,
                    MinHeight = 0,
                    MaxHeight = 16,
                    MinVeinSize = 3,
                    MaxVeinSize = 7,
                    VeinFrequency = 35,
                    VeinPattern = VeinPattern.Cluster,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Lapis] = new OreDefinition
                {
                    Type = OreType.Lapis,
                    Name = "Lapis Lazuli",
                    RarityMultiplier = 0.2f,
                    MinHeight = 0,
                    MaxHeight = 32,
                    MinVeinSize = 2,
                    MaxVeinSize = 5,
                    VeinFrequency = 45,
                    VeinPattern = VeinPattern.Cluster,
                    AllowedBiomes = new List<BiomeType>()
                },
                
                [OreType.Emerald] = new OreDefinition
                {
                    Type = OreType.Emerald,
                    Name = "Emerald",
                    RarityMultiplier = 0.1f,
                    MinHeight = 4,
                    MaxHeight = 32,
                    MinVeinSize = 1,
                    MaxVeinSize = 2,
                    VeinFrequency = 80,
                    VeinPattern = VeinPattern.Blob,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Mountains }
                },
                
                [OreType.Diamond] = new OreDefinition
                {
                    Type = OreType.Diamond,
                    Name = "Diamond",
                    RarityMultiplier = 0.05f,
                    MinHeight = 0,
                    MaxHeight = 16,
                    MinVeinSize = 1,
                    MaxVeinSize = 3,
                    VeinFrequency = 100,
                    VeinPattern = VeinPattern.Blob,
                    AllowedBiomes = new List<BiomeType>()
                }
            };
        }
    }
    
    /// <summary>
    /// Ore data for a chunk
    /// </summary>
    public class OreData
    {
        private readonly OreType[,,] oreMap;
        
        public int Size { get; }
        
        public OreData(int size)
        {
            Size = size;
            oreMap = new OreType[size, size, size];
        }
        
        public OreType GetOre(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return OreType.None;
                
            return oreMap[x, y, z];
        }
        
        public void SetOre(int x, int y, int z, OreType oreType)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return;
                
            oreMap[x, y, z] = oreType;
        }
        
        public bool HasOre(int x, int y, int z)
        {
            return GetOre(x, y, z) != OreType.None;
        }
    }
    
    /// <summary>
    /// Ore definition
    /// </summary>
    public class OreDefinition
    {
        public OreType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public float RarityMultiplier { get; set; }
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int MinVeinSize { get; set; }
        public int MaxVeinSize { get; set; }
        public int VeinFrequency { get; set; }
        public VeinPattern VeinPattern { get; set; }
        public List<BiomeType> AllowedBiomes { get; set; } = new();
    }
    
    /// <summary>
    /// Ore types
    /// </summary>
    public enum OreType
    {
        None,
        Coal,
        Iron,
        Copper,
        Gold,
        Redstone,
        Lapis,
        Emerald,
        Diamond
    }
    
    /// <summary>
    /// Vein generation patterns
    /// </summary>
    public enum VeinPattern
    {
        Blob,
        Line,
        Cluster
    }
    
    /// <summary>
    /// 3D integer vector
    /// </summary>
    public struct Vector3Int
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        
        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
#endif

