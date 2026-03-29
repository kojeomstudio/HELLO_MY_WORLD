#if false
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Biome generation system with temperature/humidity gradients
    /// Generates realistic biome distributions based on climate parameters
    /// </summary>
    public class BiomeGenerationSystem
    {
        private readonly ILogger<BiomeGenerationSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly Random random;
        
        // Noise generators for biome parameters
        private readonly FastNoise temperatureNoise;
        private readonly FastNoise humidityNoise;
        private readonly FastNoise elevationNoise;
        private readonly FastNoise continentNoise;
        
        // Biome definitions
        private readonly Dictionary<BiomeType, BiomeDefinition> biomeDefinitions;
        
        public BiomeGenerationSystem(ILogger<BiomeGenerationSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.random = new Random(config.Seed);
            
            // Initialize noise generators
            temperatureNoise = new FastNoise(random.Next());
            temperatureNoise.SetNoiseType(FastNoise.NoiseType.Perlin);
            temperatureNoise.SetFrequency(config.Biomes.TemperatureFrequency);
            
            humidityNoise = new FastNoise(random.Next());
            humidityNoise.SetNoiseType(FastNoise.NoiseType.Perlin);
            humidityNoise.SetFrequency(config.Biomes.HumidityFrequency);
            
            elevationNoise = new FastNoise(random.Next());
            elevationNoise.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
            elevationNoise.SetFrequency(config.Biomes.ElevationFrequency);
            elevationNoise.SetFractalOctaves(4);
            elevationNoise.SetFractalLacunarity(2.0f);
            elevationNoise.SetFractalGain(0.5f);
            
            continentNoise = new FastNoise(random.Next());
            continentNoise.SetNoiseType(FastNoise.NoiseType.Perlin);
            continentNoise.SetFrequency(config.Biomes.ContinentFrequency);
            
            // Initialize biome definitions
            InitializeBiomeDefinitions();
            
            logger.LogInformation("[BiomeGenerationSystem] Initialized with seed: {Seed}", config.Seed);
        }
        
        /// <summary>
        /// Generates biome data for a chunk
        /// </summary>
        public async Task<BiomeData> GenerateBiomeDataAsync(int chunkX, int chunkZ, ChunkData chunkData, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            
            var size = chunkData.Size;
            var biomeData = new BiomeData(size);
            
            // Generate biome parameters for each block
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkX * size + x;
                    var worldZ = chunkZ * size + z;
                    
                    // Generate climate parameters
                    var temperature = GenerateTemperature(worldX, worldZ);
                    var humidity = GenerateHumidity(worldX, worldZ);
                    var elevation = GenerateElevation(worldX, worldZ, chunkData.HeightMap[x, z]);
                    var continent = GenerateContinent(worldX, worldZ);
                    
                    // Apply elevation influence
                    temperature = ApplyElevationInfluence(temperature, elevation);
                    humidity = ApplyElevationInfluence(humidity, elevation);
                    
                    // Determine biome
                    var biomeType = DetermineBiome(temperature, humidity, elevation, continent);
                    var biomeDefinition = biomeDefinitions[biomeType];
                    
                    // Store biome data
                    biomeData.BiomeMap[x, z] = biomeType;
                    biomeData.TemperatureMap[x, z] = temperature;
                    biomeData.HumidityMap[x, z] = humidity;
                    biomeData.ElevationMap[x, z] = elevation;
                    biomeData.ContinentMap[x, z] = continent;
                    
                    // Apply biome-specific modifications
                    ApplyBiomeModifications(biomeDefinition, x, z, chunkData, biomeData);
                }
            }
            
            // Smooth biome transitions
            if (config.Biomes.EnableTransitions)
            {
                SmoothBiomeTransitions(biomeData);
            }
            
            return biomeData;
        }
        
        /// <summary>
        /// Generates temperature value for world coordinates
        /// </summary>
        private float GenerateTemperature(int worldX, int worldZ)
        {
            // Base temperature from latitude
            var latitude = Math.Abs(worldZ) / 1000.0f; // Normalize latitude
            var baseTemp = 1.0f - (latitude * 0.8f); // Temperature decreases toward poles
            
            // Add noise variation
            var noiseValue = temperatureNoise.GetNoise(worldX, worldZ);
            noiseValue = (noiseValue + 1.0f) * 0.5f; // Normalize to 0-1
            
            // Apply temperature scale
            var temperature = baseTemp + (noiseValue - 0.5f) * config.Biomes.TemperatureVariation;
            
            return Math.Clamp(temperature, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Generates humidity value for world coordinates
        /// </summary>
        private float GenerateHumidity(int worldX, int worldZ)
        {
            // Base humidity from distance from coast (simplified)
            var distanceFromCoast = Math.Abs(worldX % 500) / 500.0f;
            var baseHumidity = 0.5f + (1.0f - distanceFromCoast) * 0.3f;
            
            // Add noise variation
            var noiseValue = humidityNoise.GetNoise(worldX, worldZ);
            noiseValue = (noiseValue + 1.0f) * 0.5f; // Normalize to 0-1
            
            // Apply humidity scale
            var humidity = baseHumidity + (noiseValue - 0.5f) * config.Biomes.HumidityVariation;
            
            return Math.Clamp(humidity, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Generates elevation value for world coordinates
        /// </summary>
        private float GenerateElevation(int worldX, int worldZ, float heightValue)
        {
            // Use existing height map as base elevation
            var normalizedHeight = (heightValue - config.Terrain.MinHeight) / 
                                  (config.Terrain.MaxHeight - config.Terrain.MinHeight);
            
            // Add continental influence
            var continentValue = continentNoise.GetNoise(worldX, worldZ);
            continentValue = (continentValue + 1.0f) * 0.5f; // Normalize to 0-1
            
            // Blend height with continental features
            var elevation = normalizedHeight * 0.7f + continentValue * 0.3f;
            
            return Math.Clamp(elevation, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Generates continent value for world coordinates
        /// </summary>
        private float GenerateContinent(int worldX, int worldZ)
        {
            var noiseValue = continentNoise.GetNoise(worldX, worldZ);
            return (noiseValue + 1.0f) * 0.5f; // Normalize to 0-1
        }
        
        /// <summary>
        /// Applies elevation influence to climate parameters
        /// </summary>
        private float ApplyElevationInfluence(float value, float elevation)
        {
            // Temperature decreases with elevation (lapse rate)
            var elevationFactor = 1.0f - (elevation * config.Biomes.ElevationTemperatureInfluence);
            return value * elevationFactor;
        }
        
        /// <summary>
        /// Determines biome type based on climate parameters
        /// </summary>
        private BiomeType DetermineBiome(float temperature, float humidity, float elevation, float continent)
        {
            // Ocean biome for low elevation
            if (elevation < config.Biomes.OceanThreshold)
            {
                return BiomeType.Ocean;
            }
            
            // Beach biome for coastal areas
            if (elevation < config.Biomes.BeachThreshold && continent > 0.3f && continent < 0.7f)
            {
                return BiomeType.Beach;
            }
            
            // Determine biome based on temperature and humidity
            if (temperature > 0.8f)
            {
                // Hot biomes
                if (humidity > 0.7f)
                    return BiomeType.Rainforest;
                else if (humidity > 0.4f)
                    return BiomeType.Savanna;
                else
                    return BiomeType.Desert;
            }
            else if (temperature > 0.5f)
            {
                // Temperate biomes
                if (humidity > 0.6f)
                    return BiomeType.TemperateForest;
                else if (humidity > 0.3f)
                    return BiomeType.Plains;
                else
                    return BiomeType.Steppe;
            }
            else
            {
                // Cold biomes
                if (humidity > 0.5f)
                    return BiomeType.Taiga;
                else if (humidity > 0.3f)
                    return BiomeType.Tundra;
                else
                    return BiomeType.SnowyTundra;
            }
        }
        
        /// <summary>
        /// Applies biome-specific modifications to terrain
        /// </summary>
        private void ApplyBiomeModifications(BiomeDefinition biome, int x, int z, ChunkData chunkData, BiomeData biomeData)
        {
            // Modify height based on biome
            if (biome.HeightModifier != 0)
            {
                chunkData.HeightMap[x, z] += biome.HeightModifier;
            }
            
            // Apply biome-specific features
            switch (biome.Type)
            {
                case BiomeType.Ocean:
                    // Ensure ocean floor is at or below sea level
                    chunkData.HeightMap[x, z] = Math.Min(chunkData.HeightMap[x, z], config.Water.GlobalWaterLevel - 2);
                    break;
                    
                case BiomeType.Beach:
                    // Flatten beach areas
                    chunkData.HeightMap[x, z] = config.Water.GlobalWaterLevel + random.Next(-2, 2);
                    break;
                    
                case BiomeType.Mountains:
                    // Add mountain features
                    if (random.NextDouble() < 0.1f)
                    {
                        chunkData.HeightMap[x, z] += random.Next(5, 15);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Smooths biome transitions for more natural boundaries
        /// </summary>
        private void SmoothBiomeTransitions(BiomeData biomeData)
        {
            var size = biomeData.Size;
            var tempBiomeMap = new BiomeType[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    // Count neighboring biomes
                    var biomeCounts = new Dictionary<BiomeType, int>();
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var nx = x + dx;
                            var nz = z + dz;
                            
                            if (nx >= 0 && nx < size && nz >= 0 && nz < size)
                            {
                                var neighborBiome = biomeData.BiomeMap[nx, nz];
                                biomeCounts[neighborBiome] = biomeCounts.GetValueOrDefault(neighborBiome, 0) + 1;
                            }
                        }
                    }
                    
                    // Find most common biome
                    var mostCommonBiome = biomeData.BiomeMap[x, z];
                    var maxCount = 0;
                    
                    foreach (var kvp in biomeCounts)
                    {
                        if (kvp.Value > maxCount)
                        {
                            maxCount = kvp.Value;
                            mostCommonBiome = kvp.Key;
                        }
                    }
                    
                    // Apply smoothing based on blend factor
                    if (maxCount >= 5) // Strong majority
                    {
                        tempBiomeMap[x, z] = mostCommonBiome;
                    }
                    else
                    {
                        // Keep original biome for transitional areas
                        tempBiomeMap[x, z] = biomeData.BiomeMap[x, z];
                    }
                }
            }
            
            // Copy smoothed biome map back
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    biomeData.BiomeMap[x, z] = tempBiomeMap[x, z];
                }
            }
        }
        
        /// <summary>
        /// Initializes biome definitions
        /// </summary>
        private void InitializeBiomeDefinitions()
        {
            biomeDefinitions = new Dictionary<BiomeType, BiomeDefinition>
            {
                [BiomeType.Ocean] = new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    TemperatureRange = (0.0f, 1.0f),
                    HumidityRange = (0.5f, 1.0f),
                    ElevationRange = (0.0f, 0.3f),
                    HeightModifier = -5,
                    SurfaceBlock = BlockType.Sand,
                    UndergroundBlock = BlockType.Sandstone,
                    VegetationDensity = 0.0f,
                    TreeTypes = new List<TreeType>(),
                    AnimalTypes = new List<AnimalType> { AnimalType.Fish, AnimalType.Squid }
                },
                
                [BiomeType.Beach] = new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    TemperatureRange = (0.3f, 1.0f),
                    HumidityRange = (0.2f, 0.8f),
                    ElevationRange = (0.25f, 0.35f),
                    HeightModifier = 0,
                    SurfaceBlock = BlockType.Sand,
                    UndergroundBlock = BlockType.Sandstone,
                    VegetationDensity = 0.1f,
                    TreeTypes = new List<TreeType>(),
                    AnimalTypes = new List<AnimalType> { AnimalType.Turtle }
                },
                
                [BiomeType.Desert] = new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    TemperatureRange = (0.7f, 1.0f),
                    HumidityRange = (0.0f, 0.3f),
                    ElevationRange = (0.3f, 0.7f),
                    HeightModifier = 2,
                    SurfaceBlock = BlockType.Sand,
                    UndergroundBlock = BlockType.Sandstone,
                    VegetationDensity = 0.05f,
                    TreeTypes = new List<TreeType> { TreeType.Cactus },
                    AnimalTypes = new List<AnimalType> { AnimalType.Rabbit }
                },
                
                [BiomeType.Savanna] = new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    TemperatureRange = (0.6f, 0.9f),
                    HumidityRange = (0.3f, 0.6f),
                    ElevationRange = (0.3f, 0.6f),
                    HeightModifier = 1,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.3f,
                    TreeTypes = new List<TreeType> { TreeType.Acacia },
                    AnimalTypes = new List<AnimalType> { AnimalType.Lion, AnimalType.Zebra }
                },
                
                [BiomeType.Rainforest] = new BiomeDefinition
                {
                    Type = BiomeType.Rainforest,
                    Name = "Rainforest",
                    TemperatureRange = (0.7f, 1.0f),
                    HumidityRange = (0.7f, 1.0f),
                    ElevationRange = (0.3f, 0.6f),
                    HeightModifier = 3,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.9f,
                    TreeTypes = new List<TreeType> { TreeType.Jungle, TreeType.Oak },
                    AnimalTypes = new List<AnimalType> { AnimalType.Parrot, AnimalType.Monkey }
                },
                
                [BiomeType.Plains] = new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    TemperatureRange = (0.4f, 0.7f),
                    HumidityRange = (0.3f, 0.6f),
                    ElevationRange = (0.3f, 0.5f),
                    HeightModifier = 0,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.4f,
                    TreeTypes = new List<TreeType> { TreeType.Oak },
                    AnimalTypes = new List<AnimalType> { AnimalType.Cow, AnimalType.Sheep }
                },
                
                [BiomeType.TemperateForest] = new BiomeDefinition
                {
                    Type = BiomeType.TemperateForest,
                    Name = "Temperate Forest",
                    TemperatureRange = (0.4f, 0.7f),
                    HumidityRange = (0.5f, 0.8f),
                    ElevationRange = (0.3f, 0.6f),
                    HeightModifier = 2,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.7f,
                    TreeTypes = new List<TreeType> { TreeType.Oak, TreeType.Birch },
                    AnimalTypes = new List<AnimalType> { AnimalType.Wolf, AnimalType.Deer }
                },
                
                [BiomeType.Taiga] = new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    TemperatureRange = (0.2f, 0.5f),
                    HumidityRange = (0.4f, 0.7f),
                    ElevationRange = (0.3f, 0.6f),
                    HeightModifier = 1,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.6f,
                    TreeTypes = new List<TreeType> { TreeType.Spruce, TreeType.Pine },
                    AnimalTypes = new List<AnimalType> { AnimalType.Bear, AnimalType.Moose }
                },
                
                [BiomeType.Tundra] = new BiomeDefinition
                {
                    Type = BiomeType.Tundra,
                    Name = "Tundra",
                    TemperatureRange = (0.1f, 0.4f),
                    HumidityRange = (0.3f, 0.6f),
                    ElevationRange = (0.3f, 0.5f),
                    HeightModifier = -1,
                    SurfaceBlock = BlockType.Grass,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.2f,
                    TreeTypes = new List<TreeType>(),
                    AnimalTypes = new List<AnimalType> { AnimalType.PolarBear }
                },
                
                [BiomeType.SnowyTundra] = new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    TemperatureRange = (0.0f, 0.3f),
                    HumidityRange = (0.0f, 0.5f),
                    ElevationRange = (0.3f, 0.6f),
                    HeightModifier = -2,
                    SurfaceBlock = BlockType.Snow,
                    UndergroundBlock = BlockType.Dirt,
                    VegetationDensity = 0.1f,
                    TreeTypes = new List<TreeType>(),
                    AnimalTypes = new List<AnimalType> { AnimalType.PolarBear }
                }
            };
        }
    }
    
    /// <summary>
    /// Biome data for a chunk
    /// </summary>
    public class BiomeData
    {
        public int Size { get; }
        public BiomeType[,] BiomeMap { get; }
        public float[,] TemperatureMap { get; }
        public float[,] HumidityMap { get; }
        public float[,] ElevationMap { get; }
        public float[,] ContinentMap { get; }
        
        public BiomeData(int size)
        {
            Size = size;
            BiomeMap = new BiomeType[size, size];
            TemperatureMap = new float[size, size];
            HumidityMap = new float[size, size];
            ElevationMap = new float[size, size];
            ContinentMap = new float[size, size];
        }
    }
    
    /// <summary>
    /// Biome definition
    /// </summary>
    public class BiomeDefinition
    {
        public BiomeType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public (float Min, float Max) TemperatureRange { get; set; }
        public (float Min, float Max) HumidityRange { get; set; }
        public (float Min, float Max) ElevationRange { get; set; }
        public int HeightModifier { get; set; }
        public BlockType SurfaceBlock { get; set; }
        public BlockType UndergroundBlock { get; set; }
        public float VegetationDensity { get; set; }
        public List<TreeType> TreeTypes { get; set; } = new();
        public List<AnimalType> AnimalTypes { get; set; } = new();
    }
    
    /// <summary>
    /// Biome types
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Beach,
        Desert,
        Savanna,
        Rainforest,
        Plains,
        TemperateForest,
        Taiga,
        Tundra,
        SnowyTundra,
        Mountains
    }
    
    /// <summary>
    /// Tree types
    /// </summary>
    public enum TreeType
    {
        Oak,
        Birch,
        Spruce,
        Pine,
        Jungle,
        Acacia,
        Cactus
    }
    
    /// <summary>
    /// Animal types
    /// </summary>
    public enum AnimalType
    {
        Cow,
        Sheep,
        Pig,
        Chicken,
        Wolf,
        Bear,
        Deer,
        Moose,
        Rabbit,
        Lion,
        Zebra,
        Parrot,
        Monkey,
        PolarBear,
        Turtle,
        Fish,
        Squid
    }
}
#endif
