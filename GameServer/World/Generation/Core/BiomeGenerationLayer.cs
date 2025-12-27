using System;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Core
{
    /// <summary>
    /// Core layer for biome generation using temperature and humidity gradients
    /// </summary>
    public class BiomeGenerationLayer : ICoreLayer
    {
        private readonly BiomeConfig _config;
        private readonly FastNoise _temperatureNoise;
        private readonly FastNoise _humidityNoise;
        private readonly FastNoise _variationNoise;
        
        public string LayerId => "BiomeGeneration";
        public int Priority => 10; // First layer to execute
        public bool IsEnabled { get; set; } = true;
        
        public BiomeGenerationLayer(BiomeConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _temperatureNoise = new FastNoise();
            _humidityNoise = new FastNoise();
            _variationNoise = new FastNoise();
            
            ConfigureNoise();
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(BiomeConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Generate temperature and humidity maps
            GenerateTemperatureMap(context);
            GenerateHumidityMap(context);
            
            // Generate biome map based on temperature and humidity
            GenerateBiomeMap(context);
            
            // Generate height map based on biomes
            GenerateHeightMap(context);
            
            Console.WriteLine($"[BiomeGenerationLayer] Generated biomes for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private void ConfigureNoise()
        {
            // Configure temperature noise
            _temperatureNoise.SetFrequency(_config.Temperature.Frequency);
            _temperatureNoise.SetFractalOctaves(_config.Temperature.Octaves);
            _temperatureNoise.SetFractalLacunarity(_config.Temperature.Lacunarity);
            _temperatureNoise.SetFractalGain(_config.Temperature.Gain);
            
            // Configure humidity noise
            _humidityNoise.SetFrequency(_config.Humidity.Frequency);
            _humidityNoise.SetFractalOctaves(_config.Humidity.Octaves);
            _humidityNoise.SetFractalLacunarity(_config.Humidity.Lacunarity);
            _humidityNoise.SetFractalGain(_config.Humidity.Gain);
            
            // Configure variation noise
            _variationNoise.SetFrequency(_config.Variation.Frequency);
        }
        
        private void GenerateTemperatureMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var worldHeight = _config.World.Height;
            
            context.TemperatureData = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Base temperature from latitude (Z-axis)
                    var latitudeFactor = 1.0f - Math.Abs(worldZ - worldHeight / 2) / (worldHeight / 2);
                    var baseTemperature = (float)(latitudeFactor * 0.8 + 0.2); // Range from 0.2 to 1.0
                    
                    // Add noise variation
                    var noiseValue = _temperatureNoise.GetNoise(worldX, worldZ);
                    var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
                    
                    // Apply equator bonus and pole penalty
                    var equatorBonus = _config.Temperature.EquatorBonus * latitudeFactor;
                    var polePenalty = _config.Temperature.PolePenalty * (1.0f - latitudeFactor);
                    
                    // Calculate final temperature
                    var temperature = baseTemperature + normalizedNoise * 0.3f + equatorBonus - polePenalty;
                    
                    // Clamp to valid range
                    temperature = Math.Max(0.0f, Math.Min(1.0f, temperature));
                    
                    context.TemperatureData[x, z] = temperature;
                }
            }
        }
        
        private void GenerateHumidityMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.HumidityData = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Base humidity from noise
                    var noiseValue = _humidityNoise.GetNoise(worldX, worldZ);
                    var baseHumidity = (noiseValue + 1.0f) * 0.5f;
                    
                    // Apply water bonus and desert penalty
                    var temperature = context.TemperatureData[x, z];
                    var waterBonus = _config.Humidity.WaterBonus * (1.0f - temperature);
                    var desertPenalty = _config.Humidity.DesertPenalty * temperature;
                    
                    // Calculate final humidity
                    var humidity = baseHumidity + waterBonus - desertPenalty;
                    
                    // Clamp to valid range
                    humidity = Math.Max(0.0f, Math.Min(1.0f, humidity));
                    
                    context.HumidityData[x, z] = humidity;
                }
            }
        }
        
        private void GenerateBiomeMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.BiomeData = new BiomeType[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var temperature = context.TemperatureData[x, z];
                    var humidity = context.HumidityData[x, z];
                    
                    // Add variation noise
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    var variationNoise = _variationNoise.GetNoise(worldX + _config.Variation.SeedOffset, worldZ + _config.Variation.SeedOffset);
                    var variation = (variationNoise + 1.0f) * 0.5f * _config.Variation.Strength;
                    
                    // Apply variation
                    var adjustedTemperature = Math.Max(0.0f, Math.Min(1.0f, temperature + variation));
                    var adjustedHumidity = Math.Max(0.0f, Math.Min(1.0f, humidity + variation));
                    
                    // Determine biome based on temperature and humidity
                    var biome = DetermineBiome(adjustedTemperature, adjustedHumidity);
                    
                    context.BiomeData[x, z] = biome;
                }
            }
            
            // Apply biome smoothing if enabled
            if (_config.Smoothing.Enabled)
            {
                ApplyBiomeSmoothing(context);
            }
        }
        
        private BiomeType DetermineBiome(float temperature, float humidity)
        {
            // Find the best matching biome based on temperature and humidity ranges
            var bestBiome = BiomeType.Plains; // Default
            var bestScore = float.MaxValue;
            
            foreach (var biomeDef in _config.Biomes)
            {
                // Calculate how well this position matches the biome requirements
                var tempScore = CalculateBiomeScore(temperature, biomeDef.MinTemperature, biomeDef.MaxTemperature);
                var humidityScore = CalculateBiomeScore(humidity, biomeDef.MinHumidity, biomeDef.MaxHumidity);
                var totalScore = tempScore + humidityScore;
                
                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestBiome = biomeDef.Type;
                }
            }
            
            return bestBiome;
        }
        
        private float CalculateBiomeScore(float value, float minRange, float maxRange)
        {
            if (value >= minRange && value <= maxRange)
            {
                return 0.0f; // Perfect match
            }
            
            // Calculate distance from range
            if (value < minRange)
            {
                return minRange - value;
            }
            else
            {
                return value - maxRange;
            }
        }
        
        private void ApplyBiomeSmoothing(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var threshold = _config.Smoothing.Threshold;
            var passes = _config.Smoothing.Passes;
            
            var originalBiomes = new BiomeType[chunkSize, chunkSize];
            
            for (int pass = 0; pass < passes; pass++)
            {
                // Copy current state
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        originalBiomes[x, z] = context.BiomeData[x, z];
                    }
                }
                
                // Apply smoothing
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        // Skip rivers if preservation is enabled
                        if (_config.Smoothing.PreserveRivers && originalBiomes[x, z] == BiomeType.River)
                        {
                            continue;
                        }
                        
                        // Count neighboring biomes
                        var biomeCounts = new Dictionary<BiomeType, int>();
                        
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;
                                
                                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                                {
                                    var neighborBiome = originalBiomes[nx, nz];
                                    biomeCounts[neighborBiome] = biomeCounts.GetValueOrDefault(neighborBiome, 0) + 1;
                                }
                            }
                        }
                        
                        // Find the most common neighboring biome
                        var mostCommon = biomeCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                        
                        // Apply smoothing if threshold is met
                        if (mostCommon.Value >= threshold)
                        {
                            context.BiomeData[x, z] = mostCommon.Key;
                        }
                    }
                }
            }
        }
        
        private void GenerateHeightMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.HeightMap = new int[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var biome = context.BiomeData[x, z];
                    var biomeDef = _config.Biomes.FirstOrDefault(b => b.Type == biome);
                    
                    if (biomeDef != null)
                    {
                        // Generate height based on biome configuration
                        var worldX = context.ChunkX * chunkSize + x;
                        var worldZ = context.ChunkZ * chunkSize + z;
                        
                        // Base height from biome
                        var baseHeight = biomeDef.BaseHeight * context.Config.MaxHeight;
                        
                        // Add height variation
                        var heightNoise = _temperatureNoise.GetNoise(worldX * 0.01f, worldZ * 0.01f);
                        var heightVariation = (heightNoise + 1.0f) * 0.5f * biomeDef.HeightVariation * context.Config.MaxHeight;
                        
                        // Calculate final height
                        var height = baseHeight + heightVariation;
                        
                        // Clamp to valid range
                        height = Math.Max(0, Math.Min(context.Config.MaxHeight - 1, height));
                        
                        context.HeightMap[x, z] = (int)height;
                    }
                    else
                    {
                        // Default height for unknown biomes
                        context.HeightMap[x, z] = context.Config.MaxHeight / 2;
                    }
                }
            }
        }
    }
}
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Core
{
    /// <summary>
    /// Core layer for biome generation using temperature and humidity gradients
    /// </summary>
    public class BiomeGenerationLayer : ICoreLayer
    {
        private readonly BiomeConfig _config;
        private readonly FastNoise _temperatureNoise;
        private readonly FastNoise _humidityNoise;
        private readonly FastNoise _variationNoise;
        
        public string LayerId => "BiomeGeneration";
        public int Priority => 10; // First layer to execute
        public bool IsEnabled { get; set; } = true;
        
        public BiomeGenerationLayer(BiomeConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _temperatureNoise = new FastNoise();
            _humidityNoise = new FastNoise();
            _variationNoise = new FastNoise();
            
            ConfigureNoise();
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(BiomeConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Generate temperature and humidity maps
            GenerateTemperatureMap(context);
            GenerateHumidityMap(context);
            
            // Generate biome map based on temperature and humidity
            GenerateBiomeMap(context);
            
            // Generate height map based on biomes
            GenerateHeightMap(context);
            
            Console.WriteLine($"[BiomeGenerationLayer] Generated biomes for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private void ConfigureNoise()
        {
            // Configure temperature noise
            _temperatureNoise.SetFrequency(_config.Temperature.Frequency);
            _temperatureNoise.SetFractalOctaves(_config.Temperature.Octaves);
            _temperatureNoise.SetFractalLacunarity(_config.Temperature.Lacunarity);
            _temperatureNoise.SetFractalGain(_config.Temperature.Gain);
            
            // Configure humidity noise
            _humidityNoise.SetFrequency(_config.Humidity.Frequency);
            _humidityNoise.SetFractalOctaves(_config.Humidity.Octaves);
            _humidityNoise.SetFractalLacunarity(_config.Humidity.Lacunarity);
            _humidityNoise.SetFractalGain(_config.Humidity.Gain);
            
            // Configure variation noise
            _variationNoise.SetFrequency(_config.Variation.Frequency);
        }
        
        private void GenerateTemperatureMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var worldHeight = _config.World.Height;
            
            context.TemperatureData = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Base temperature from latitude (Z-axis)
                    var latitudeFactor = 1.0f - Math.Abs(worldZ - worldHeight / 2) / (worldHeight / 2);
                    var baseTemperature = (float)(latitudeFactor * 0.8 + 0.2); // Range from 0.2 to 1.0
                    
                    // Add noise variation
                    var noiseValue = _temperatureNoise.GetNoise(worldX, worldZ);
                    var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
                    
                    // Apply equator bonus and pole penalty
                    var equatorBonus = _config.Temperature.EquatorBonus * latitudeFactor;
                    var polePenalty = _config.Temperature.PolePenalty * (1.0f - latitudeFactor);
                    
                    // Calculate final temperature
                    var temperature = baseTemperature + normalizedNoise * 0.3f + equatorBonus - polePenalty;
                    
                    // Clamp to valid range
                    temperature = Math.Max(0.0f, Math.Min(1.0f, temperature));
                    
                    context.TemperatureData[x, z] = temperature;
                }
            }
        }
        
        private void GenerateHumidityMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.HumidityData = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Base humidity from noise
                    var noiseValue = _humidityNoise.GetNoise(worldX, worldZ);
                    var baseHumidity = (noiseValue + 1.0f) * 0.5f;
                    
                    // Apply water bonus and desert penalty
                    var temperature = context.TemperatureData[x, z];
                    var waterBonus = _config.Humidity.WaterBonus * (1.0f - temperature);
                    var desertPenalty = _config.Humidity.DesertPenalty * temperature;
                    
                    // Calculate final humidity
                    var humidity = baseHumidity + waterBonus - desertPenalty;
                    
                    // Clamp to valid range
                    humidity = Math.Max(0.0f, Math.Min(1.0f, humidity));
                    
                    context.HumidityData[x, z] = humidity;
                }
            }
        }
        
        private void GenerateBiomeMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.BiomeData = new BiomeType[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var temperature = context.TemperatureData[x, z];
                    var humidity = context.HumidityData[x, z];
                    
                    // Add variation noise
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    var variationNoise = _variationNoise.GetNoise(worldX + _config.Variation.SeedOffset, worldZ + _config.Variation.SeedOffset);
                    var variation = (variationNoise + 1.0f) * 0.5f * _config.Variation.Strength;
                    
                    // Apply variation
                    var adjustedTemperature = Math.Max(0.0f, Math.Min(1.0f, temperature + variation));
                    var adjustedHumidity = Math.Max(0.0f, Math.Min(1.0f, humidity + variation));
                    
                    // Determine biome based on temperature and humidity
                    var biome = DetermineBiome(adjustedTemperature, adjustedHumidity);
                    
                    context.BiomeData[x, z] = biome;
                }
            }
            
            // Apply biome smoothing if enabled
            if (_config.Smoothing.Enabled)
            {
                ApplyBiomeSmoothing(context);
            }
        }
        
        private BiomeType DetermineBiome(float temperature, float humidity)
        {
            // Find the best matching biome based on temperature and humidity ranges
            var bestBiome = BiomeType.Plains; // Default
            var bestScore = float.MaxValue;
            
            foreach (var biomeDef in _config.Biomes)
            {
                // Calculate how well this position matches the biome requirements
                var tempScore = CalculateBiomeScore(temperature, biomeDef.MinTemperature, biomeDef.MaxTemperature);
                var humidityScore = CalculateBiomeScore(humidity, biomeDef.MinHumidity, biomeDef.MaxHumidity);
                var totalScore = tempScore + humidityScore;
                
                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestBiome = biomeDef.Type;
                }
            }
            
            return bestBiome;
        }
        
        private float CalculateBiomeScore(float value, float minRange, float maxRange)
        {
            if (value >= minRange && value <= maxRange)
            {
                return 0.0f; // Perfect match
            }
            
            // Calculate distance from range
            if (value < minRange)
            {
                return minRange - value;
            }
            else
            {
                return value - maxRange;
            }
        }
        
        private void ApplyBiomeSmoothing(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var threshold = _config.Smoothing.Threshold;
            var passes = _config.Smoothing.Passes;
            
            var originalBiomes = new BiomeType[chunkSize, chunkSize];
            
            for (int pass = 0; pass < passes; pass++)
            {
                // Copy current state
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        originalBiomes[x, z] = context.BiomeData[x, z];
                    }
                }
                
                // Apply smoothing
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        // Skip rivers if preservation is enabled
                        if (_config.Smoothing.PreserveRivers && originalBiomes[x, z] == BiomeType.River)
                        {
                            continue;
                        }
                        
                        // Count neighboring biomes
                        var biomeCounts = new Dictionary<BiomeType, int>();
                        
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                var nx = x + dx;
                                var nz = z + dz;
                                
                                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                                {
                                    var neighborBiome = originalBiomes[nx, nz];
                                    biomeCounts[neighborBiome] = biomeCounts.GetValueOrDefault(neighborBiome, 0) + 1;
                                }
                            }
                        }
                        
                        // Find the most common neighboring biome
                        var mostCommon = biomeCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                        
                        // Apply smoothing if threshold is met
                        if (mostCommon.Value >= threshold)
                        {
                            context.BiomeData[x, z] = mostCommon.Key;
                        }
                    }
                }
            }
        }
        
        private void GenerateHeightMap(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            context.HeightMap = new int[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var biome = context.BiomeData[x, z];
                    var biomeDef = _config.Biomes.FirstOrDefault(b => b.Type == biome);
                    
                    if (biomeDef != null)
                    {
                        // Generate height based on biome configuration
                        var worldX = context.ChunkX * chunkSize + x;
                        var worldZ = context.ChunkZ * chunkSize + z;
                        
                        // Base height from biome
                        var baseHeight = biomeDef.BaseHeight * context.Config.MaxHeight;
                        
                        // Add height variation
                        var heightNoise = _temperatureNoise.GetNoise(worldX * 0.01f, worldZ * 0.01f);
                        var heightVariation = (heightNoise + 1.0f) * 0.5f * biomeDef.HeightVariation * context.Config.MaxHeight;
                        
                        // Calculate final height
                        var height = baseHeight + heightVariation;
                        
                        // Clamp to valid range
                        height = Math.Max(0, Math.Min(context.Config.MaxHeight - 1, height));
                        
                        context.HeightMap[x, z] = (int)height;
                    }
                    else
                    {
                        // Default height for unknown biomes
                        context.HeightMap[x, z] = context.Config.MaxHeight / 2;
                    }
                }
            }
        }
    }
}
}
                    var temperature = GenerateTemperature(worldX, worldZ);
                    var humidity = GenerateHumidity(worldX, worldZ);
                    
                    // Store raw values
                    context.TemperatureData[localX, localZ] = temperature;
                    context.HumidityData[localX, localZ] = humidity;
                    
                    // Determine biome based on temperature and humidity
                    var biome = DetermineBiome(temperature, humidity, worldX, worldZ);
                    context.BiomeData[localX, localZ] = biome;
                }
            }
            
            // Apply biome smoothing for more natural transitions
            if (_config.EnableBiomeSmoothing)
            {
                ApplyBiomeSmoothing(context);
            }
            
            Console.WriteLine($"[BiomeGenerationLayer] Generated biomes for chunk ({chunkX},{chunkZ})");
        }
        
        private float GenerateTemperature(int worldX, int worldZ)
        {
            // Base temperature from noise
            var noiseValue = _temperatureNoise.GetNoise(worldX, worldZ);
            var normalizedTemp = (noiseValue + 1.0f) * 0.5f; // Normalize to 0-1
            
            // Apply latitude-based temperature variation (equator = hot, poles = cold)
            var latitudeFactor = 1.0f - Math.Abs(worldZ / (float)(_config.WorldHeight * 0.5)) * 0.5f;
            
            // Apply elevation-based temperature variation
            var elevationFactor = 1.0f; // This would be adjusted based on terrain height
            
            // Combine factors
            var finalTemp = normalizedTemp * latitudeFactor * elevationFactor;
            
            // Clamp to valid range
            return Math.Max(0.0f, Math.Min(1.0f, finalTemp));
        }
        
        private float GenerateHumidity(int worldX, int worldZ)
        {
            // Base humidity from noise
            var noiseValue = _humidityNoise.GetNoise(worldX, worldZ);
            var normalizedHumidity = (noiseValue + 1.0f) * 0.5f; // Normalize to 0-1
            
            // Apply distance from water bodies (if available)
            var waterFactor = 1.0f; // This would be adjusted based on proximity to water
            
            // Combine factors
            var finalHumidity = normalizedHumidity * waterFactor;
            
            // Clamp to valid range
            return Math.Max(0.0f, Math.Min(1.0f, finalHumidity));
        }
        
        private BiomeType DetermineBiome(float temperature, float humidity, int worldX, int worldZ)
        {
            // Add small variation to create more interesting biome boundaries
            var variation = _variationNoise.GetNoise(worldX, worldZ) * _config.VariationStrength;
            temperature += variation;
            humidity += variation;
            
            // Clamp after variation
            temperature = Math.Max(0.0f, Math.Min(1.0f, temperature));
            humidity = Math.Max(0.0f, Math.Min(1.0f, humidity));
            
            // Find matching biome based on temperature and humidity ranges
            foreach (var biome in _config.Biomes)
            {
                if (temperature >= biome.MinTemperature && temperature <= biome.MaxTemperature &&
                    humidity >= biome.MinHumidity && humidity <= biome.MaxHumidity)
                {
                    return biome.Type;
                }
            }
            
            // Default to plains if no biome matches
            return BiomeType.Plains;
        }
        
        private void ApplyBiomeSmoothing(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var smoothedBiomes = new BiomeType[chunkSize, chunkSize];
            
            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    // Count neighboring biomes
                    var biomeCounts = new Dictionary<BiomeType, int>();
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var biome = context.BiomeData[x + dx, z + dz];
                            biomeCounts[biome] = biomeCounts.GetValueOrDefault(biome, 0) + 1;
                        }
                    }
                    
                    // Find the most common neighboring biome
                    var dominantBiome = BiomeType.Plains;
                    var maxCount = 0;
                    
                    foreach (var kvp in biomeCounts)
                    {
                        if (kvp.Value > maxCount)
                        {
                            maxCount = kvp.Value;
                            dominantBiome = kvp.Key;
                        }
                    }
                    
                    // Apply smoothing with threshold
                    if (maxCount >= _config.SmoothingThreshold)
                    {
                        smoothedBiomes[x, z] = dominantBiome;
                    }
                    else
                    {
                        smoothedBiomes[x, z] = context.BiomeData[x, z];
                    }
                }
            }
            
            // Copy edges without smoothing
            for (int x = 0; x < chunkSize; x++)
            {
                smoothedBiomes[x, 0] = context.BiomeData[x, 0];
                smoothedBiomes[x, chunkSize - 1] = context.BiomeData[x, chunkSize - 1];
            }
            
            for (int z = 0; z < chunkSize; z++)
            {
                smoothedBiomes[0, z] = context.BiomeData[0, z];
                smoothedBiomes[chunkSize - 1, z] = context.BiomeData[chunkSize - 1, z];
            }
            
            // Replace original biome data with smoothed data
            context.BiomeData = smoothedBiomes;
        }
    }
    
    /// <summary>
    /// Biome types enumeration
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Plains,
        Desert,
        Forest,
        Taiga,
        Savanna,
        SnowyTundra,
        Jungle,
        Mountains,
        Swamp,
        River,
        Beach
    }
    
    /// <summary>
    /// Configuration for a single biome type
    /// </summary>
    public class BiomeDefinition
    {
        public BiomeType Type { get; set; }
        public string Name { get; set; }
        public float MinTemperature { get; set; }
        public float MaxTemperature { get; set; }
        public float MinHumidity { get; set; }
        public float MaxHumidity { get; set; }
        public float BaseHeight { get; set; }
        public float HeightVariation { get; set; }
        public string SurfaceBlock { get; set; }
        public string SubSurfaceBlock { get; set; }
        public Dictionary<string, float> Vegetation { get; set; } = new();
        public Dictionary<string, float> Ores { get; set; } = new();
        public Dictionary<string, float> Mobs { get; set; } = new();
    }
}
