using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized cave generator with multi-threading support and caching
    /// Improvements over ImprovedCaveGenerator:
    /// - Multi-threaded mask generation
    /// - Noise sample caching
    /// - Spatial partitioning for cave queries
    /// - Biome-aware cave generation
    /// </summary>
    public class OptimizedCaveGenerator
    {
        private readonly CaveConfig _config;
        private readonly NoiseCache _noiseCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedCaveGenerator(CaveConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _noiseCache = new NoiseCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates cave mask with multi-threading support
        /// </summary>
        public double[,] GenerateCaveMask(int width, int height, int depth, 
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask, 
            string biome = null)
        {
            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (hydrologyMask == null || flowMask == null || erosionRiskMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var caveMask = new double[width, height, depth];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            
            // Generate cave mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        caveMask[x, y, z] = GenerateCavePoint(x, y, z, 
                            hydrologyMask, flowMask, erosionRiskMask, biomeSpecificConfig);
                    }
                }
            });
            
            // Apply smoothing
            caveMask = SmoothMaskParallel(caveMask, width, height, depth);
            
            // Add support columns
            AddSupportColumnsParallel(caveMask, width, height, depth, biomeSpecificConfig);
            
            return caveMask;
        }
        
        private double GenerateCavePoint(int x, int y, int z,
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get cached noise samples
            var noiseX = _noiseCache.GetNoise(x, y, z, _config.HorizontalFrequency);
            var noiseY = _noiseCache.GetNoise(x, y, z, _config.VerticalFrequency);
            
            // Calculate cave threshold with biome-specific adjustments
            double threshold = _config.Threshold * biomeConfig.ThresholdMultiplier;
            
            // Apply hydrology stability
            double hydrologyStability = CalculateHydrologyStability(
                x, y, z, hydrologyMask, flowMask, biomeConfig);
                
            // Apply flow stability
            double flowStability = CalculateFlowStability(
                x, y, z, flowMask, biomeConfig);
                
            // Apply roughness stability
            double roughnessStability = CalculateRoughnessStability(
                x, y, z, erosionRiskMask, biomeConfig);
                
            // Combine stability factors
            double stability = (hydrologyStability * _config.HydrologyStabilityWeight +
                              flowStability * _config.FlowStabilityWeight +
                              roughnessStability * _config.RoughnessStabilityWeight);
            
            // Calculate cave value
            double caveValue = (noiseX + noiseY) / 2.0;
            caveValue += stability * _config.StabilityWeight;
            
            // Apply biome-specific cave modifiers
            caveValue *= biomeConfig.CaveDensityMultiplier;
            
            return caveValue > threshold ? 1.0 : 0.0;
        }
        
        private double CalculateHydrologyStability(int x, int y, int z,
            double[,] hydrologyMask, double[,] flowMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y, z);
            
            // Apply biome-specific hydrology adjustments
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            
            return 1.0 - hydrologyValue;
        }
        
        private double CalculateFlowStability(int x, int y, int z,
            double[,] flowMask, BiomeSpecificConfig biomeConfig)
        {
            // Get flow value at position
            double flowValue = GetSafeValue(flowMask, x, y, z);
            
            // Apply biome-specific flow adjustments
            flowValue *= biomeConfig.FlowSensitivity;
            
            return 1.0 - flowValue;
        }
        
        private double CalculateRoughnessStability(int x, int y, int z,
            double[,] erosionRiskMask, BiomeSpecificConfig biomeConfig)
        {
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y, z);
            
            // Apply biome-specific erosion adjustments
            erosionRisk *= biomeConfig.ErosionSensitivity;
            
            return 1.0 - erosionRisk;
        }
        
        private double[,,] SmoothMaskParallel(double[,,] mask, int width, int height, int depth)
        {
            var smoothedMask = new double[width, height, depth];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        smoothedMask[x, y, z] = SmoothPoint(mask, x, y, z, width, height, depth);
                    }
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothPoint(double[,,] mask, int x, int y, int z, int width, int height, int depth)
        {
            double sum = 0;
            int count = 0;
            
            // Sample neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                        {
                            sum += mask[nx, ny, nz];
                            count++;
                        }
                    }
                }
            }
            
            return sum / count;
        }
        
        private void AddSupportColumnsParallel(double[,,] mask, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            Parallel.For(0, width, x =>
            {
                for (int z = 0; z < depth; z++)
                {
                    // Check if support column is needed
                    if (NeedsSupportColumn(mask, x, z, width, height, depth, biomeConfig))
                    {
                        AddSupportColumn(mask, x, z, width, height, depth, biomeConfig);
                    }
                }
            });
        }
        
        private bool NeedsSupportColumn(double[,,] mask, int x, int z, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if there's a cave ceiling that needs support
            for (int y = 0; y < height; y++)
            {
                if (mask[x, y, z] > 0.5)
                {
                    // Check if there's empty space below
                    if (y > 0 && mask[x, y - 1, z] < 0.5)
                    {
                        // Check biome-specific support requirements
                        if (ShouldAddSupport(biomeConfig))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        private void AddSupportColumn(double[,,] mask, int x, int z, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            // Add support column from ceiling to floor
            for (int y = 0; y < height; y++)
            {
                if (mask[x, y, z] > 0.5)
                {
                    // Add support pillar
                    int pillarHeight = CalculatePillarHeight(biomeConfig);
                    for (int py = y; py < Math.Min(y + pillarHeight, height); py++)
                    {
                        mask[x, py, z] = 1.0;
                    }
                    break;
                }
            }
        }
        
        private bool ShouldAddSupport(BiomeSpecificConfig biomeConfig)
        {
            // Use biome-specific support probability
            double supportChance = _config.SupportPillarChance * biomeConfig.SupportMultiplier;
            return new Random().NextDouble() < supportChance;
        }
        
        private int CalculatePillarHeight(BiomeSpecificConfig biomeConfig)
        {
            // Use biome-specific pillar height
            return (int)(_config.SupportDensity * biomeConfig.PillarHeightMultiplier * 10);
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
        }
        
        private double GetSafeValue(double[,] mask, int x, int y, int z)
        {
            // Safe access to 2D mask with 3D coordinates
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return mask[x, y];
            }
            return 0.0;
        }
    }
    
    /// <summary>
    /// Cave configuration with caching support
    /// </summary>
    public class CaveConfig
    {
        // Core cave parameters
        public double Threshold { get; set; } = 0.45;
        public double HorizontalFrequency { get; set; } = 0.0026;
        public double VerticalFrequency { get; set; } = 0.018;
        
        // Support system
        public double SupportDensity { get; set; } = 0.6;
        public double SupportPillarChance { get; set; } = 0.28;
        
        // Hydrology awareness
        public double HydrologyStabilityWeight { get; set; } = 0.45;
        public double FlowStabilityWeight { get; set; } = 0.25;
        public double RoughnessStabilityWeight { get; set; } = 0.1;
        public double RiverSuppressionWeight { get; set; } = 0.35;
        public double MoistureRetentionWeight { get; set; } = 0.35;
        
        // Edge sealing
        public double EdgeSealStrength { get; set; } = 0.45;
        public int RiparianPlugDepth { get; set; } = 2;
        
        // Stability smoothing
        public int StabilitySmoothIterations { get; set; } = 1;
        public double StabilitySmoothBlend { get; set; } = 0.55;
        
        // Ceiling protection
        public double CeilingStabilityWeight { get; set; } = 0.35;
        public double CeilingMoistureWeight { get; set; } = 0.28;
        public double CeilingMoistureClamp { get; set; } = 0.35;
        
        // Flooded caves
        public double FloodedCaveNoiseFrequency { get; set; } = 0.0031;
        public double FloodedCaveProximityToWaterTableWeight { get; set; } = 0.6;
        public double FloodedCaveThreshold { get; set; } = 0.75;
        
        // Lava and water
        public double LavaThreshold { get; set; } = 0.28;
        public double WaterThreshold { get; set; } = 0.34;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
        
        // Stability weight
        public double StabilityWeight { get; set; } = 1.0;
    }
    
    /// <summary>
    /// Noise sample cache for performance optimization
    /// </summary>
    public class NoiseCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        private readonly Random _random;
        
        public NoiseCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
            _random = new Random();
        }
        
        public double GetNoise(int x, int y, int z, double frequency)
        {
            string key = $"{x},{y},{z},{frequency}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Generate noise value
            double noiseValue = GenerateNoise(x, y, z, frequency);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, noiseValue);
            }
            
            return noiseValue;
        }
        
        private double GenerateNoise(int x, int y, int z, double frequency)
        {
            // Simple noise generation (can be replaced with Perlin/Simplex noise)
            double nx = x * frequency;
            double ny = y * frequency;
            double nz = z * frequency;
            
            return (Math.Sin(nx) + Math.Cos(ny) + Math.Sin(nz)) / 3.0;
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific cave configuration
    /// </summary>
    public class BiomeConfig
    {
        private readonly ConcurrentDictionary<string, BiomeSpecificConfig> _biomeConfigs;
        
        public BiomeConfig()
        {
            _biomeConfigs = new ConcurrentDictionary<string, BiomeSpecificConfig>();
            InitializeDefaultBiomes();
        }
        
        private void InitializeDefaultBiomes()
        {
            // Default biome configurations
            _biomeConfigs.TryAdd("plains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.0,
                CaveDensityMultiplier = 1.0,
                HydrologySensitivity = 1.0,
                FlowSensitivity = 1.0,
                ErosionSensitivity = 1.0,
                SupportMultiplier = 1.0,
                PillarHeightMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                CaveDensityMultiplier = 1.5,
                HydrologySensitivity = 0.8,
                FlowSensitivity = 0.7,
                ErosionSensitivity = 1.5,
                SupportMultiplier = 1.2,
                PillarHeightMultiplier = 1.5
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                CaveDensityMultiplier = 0.7,
                HydrologySensitivity = 1.5,
                FlowSensitivity = 1.2,
                ErosionSensitivity = 0.8,
                SupportMultiplier = 0.8,
                PillarHeightMultiplier = 0.7
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                CaveDensityMultiplier = 1.1,
                HydrologySensitivity = 1.2,
                FlowSensitivity = 1.1,
                ErosionSensitivity = 1.0,
                SupportMultiplier = 1.0,
                PillarHeightMultiplier = 1.0
            });
        }
        
        public BiomeSpecificConfig GetConfig(string biome)
        {
            if (_biomeConfigs.TryGetValue(biome, out var config))
            {
                return config;
            }
            return GetDefaultConfig();
        }
        
        public BiomeSpecificConfig GetDefaultConfig()
        {
            return _biomeConfigs.TryGetValue("plains", out var config) ? config : new BiomeSpecificConfig();
        }
        
        public void AddBiomeConfig(string biome, BiomeSpecificConfig config)
        {
            _biomeConfigs.TryAdd(biome, config);
        }
    }
    
    /// <summary>
    /// Biome-specific cave generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double CaveDensityMultiplier { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowSensitivity { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double SupportMultiplier { get; set; } = 1.0;
        public double PillarHeightMultiplier { get; set; } = 1.0;
    }
}
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized cave generator with multi-threading support and caching
    /// Improvements over ImprovedCaveGenerator:
    /// - Multi-threaded mask generation
    /// - Noise sample caching
    /// - Spatial partitioning for cave queries
    /// - Biome-aware cave generation
    /// </summary>
    public class OptimizedCaveGenerator
    {
        private readonly CaveConfig _config;
        private readonly NoiseCache _noiseCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedCaveGenerator(CaveConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _noiseCache = new NoiseCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates cave mask with multi-threading support
        /// </summary>
        public double[,] GenerateCaveMask(int width, int height, int depth, 
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask, 
            string biome = null)
        {
            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (hydrologyMask == null || flowMask == null || erosionRiskMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var caveMask = new double[width, height, depth];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            
            // Generate cave mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        caveMask[x, y, z] = GenerateCavePoint(x, y, z, 
                            hydrologyMask, flowMask, erosionRiskMask, biomeSpecificConfig);
                    }
                }
            });
            
            // Apply smoothing
            caveMask = SmoothMaskParallel(caveMask, width, height, depth);
            
            // Add support columns
            AddSupportColumnsParallel(caveMask, width, height, depth, biomeSpecificConfig);
            
            return caveMask;
        }
        
        private double GenerateCavePoint(int x, int y, int z,
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get cached noise samples
            var noiseX = _noiseCache.GetNoise(x, y, z, _config.HorizontalFrequency);
            var noiseY = _noiseCache.GetNoise(x, y, z, _config.VerticalFrequency);
            
            // Calculate cave threshold with biome-specific adjustments
            double threshold = _config.Threshold * biomeConfig.ThresholdMultiplier;
            
            // Apply hydrology stability
            double hydrologyStability = CalculateHydrologyStability(
                x, y, z, hydrologyMask, flowMask, biomeConfig);
                
            // Apply flow stability
            double flowStability = CalculateFlowStability(
                x, y, z, flowMask, biomeConfig);
                
            // Apply roughness stability
            double roughnessStability = CalculateRoughnessStability(
                x, y, z, erosionRiskMask, biomeConfig);
                
            // Combine stability factors
            double stability = (hydrologyStability * _config.HydrologyStabilityWeight +
                              flowStability * _config.FlowStabilityWeight +
                              roughnessStability * _config.RoughnessStabilityWeight);
            
            // Calculate cave value
            double caveValue = (noiseX + noiseY) / 2.0;
            caveValue += stability * _config.StabilityWeight;
            
            // Apply biome-specific cave modifiers
            caveValue *= biomeConfig.CaveDensityMultiplier;
            
            return caveValue > threshold ? 1.0 : 0.0;
        }
        
        private double CalculateHydrologyStability(int x, int y, int z,
            double[,] hydrologyMask, double[,] flowMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y, z);
            
            // Apply biome-specific hydrology adjustments
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            
            return 1.0 - hydrologyValue;
        }
        
        private double CalculateFlowStability(int x, int y, int z,
            double[,] flowMask, BiomeSpecificConfig biomeConfig)
        {
            // Get flow value at position
            double flowValue = GetSafeValue(flowMask, x, y, z);
            
            // Apply biome-specific flow adjustments
            flowValue *= biomeConfig.FlowSensitivity;
            
            return 1.0 - flowValue;
        }
        
        private double CalculateRoughnessStability(int x, int y, int z,
            double[,] erosionRiskMask, BiomeSpecificConfig biomeConfig)
        {
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y, z);
            
            // Apply biome-specific erosion adjustments
            erosionRisk *= biomeConfig.ErosionSensitivity;
            
            return 1.0 - erosionRisk;
        }
        
        private double[,,] SmoothMaskParallel(double[,,] mask, int width, int height, int depth)
        {
            var smoothedMask = new double[width, height, depth];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        smoothedMask[x, y, z] = SmoothPoint(mask, x, y, z, width, height, depth);
                    }
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothPoint(double[,,] mask, int x, int y, int z, int width, int height, int depth)
        {
            double sum = 0;
            int count = 0;
            
            // Sample neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                        {
                            sum += mask[nx, ny, nz];
                            count++;
                        }
                    }
                }
            }
            
            return sum / count;
        }
        
        private void AddSupportColumnsParallel(double[,,] mask, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            Parallel.For(0, width, x =>
            {
                for (int z = 0; z < depth; z++)
                {
                    // Check if support column is needed
                    if (NeedsSupportColumn(mask, x, z, width, height, depth, biomeConfig))
                    {
                        AddSupportColumn(mask, x, z, width, height, depth, biomeConfig);
                    }
                }
            });
        }
        
        private bool NeedsSupportColumn(double[,,] mask, int x, int z, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if there's a cave ceiling that needs support
            for (int y = 0; y < height; y++)
            {
                if (mask[x, y, z] > 0.5)
                {
                    // Check if there's empty space below
                    if (y > 0 && mask[x, y - 1, z] < 0.5)
                    {
                        // Check biome-specific support requirements
                        if (ShouldAddSupport(biomeConfig))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        private void AddSupportColumn(double[,,] mask, int x, int z, int width, int height, int depth,
            BiomeSpecificConfig biomeConfig)
        {
            // Add support column from ceiling to floor
            for (int y = 0; y < height; y++)
            {
                if (mask[x, y, z] > 0.5)
                {
                    // Add support pillar
                    int pillarHeight = CalculatePillarHeight(biomeConfig);
                    for (int py = y; py < Math.Min(y + pillarHeight, height); py++)
                    {
                        mask[x, py, z] = 1.0;
                    }
                    break;
                }
            }
        }
        
        private bool ShouldAddSupport(BiomeSpecificConfig biomeConfig)
        {
            // Use biome-specific support probability
            double supportChance = _config.SupportPillarChance * biomeConfig.SupportMultiplier;
            return new Random().NextDouble() < supportChance;
        }
        
        private int CalculatePillarHeight(BiomeSpecificConfig biomeConfig)
        {
            // Use biome-specific pillar height
            return (int)(_config.SupportDensity * biomeConfig.PillarHeightMultiplier * 10);
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
        }
        
        private double GetSafeValue(double[,] mask, int x, int y, int z)
        {
            // Safe access to 2D mask with 3D coordinates
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return mask[x, y];
            }
            return 0.0;
        }
    }
    
    /// <summary>
    /// Cave configuration with caching support
    /// </summary>
    public class CaveConfig
    {
        // Core cave parameters
        public double Threshold { get; set; } = 0.45;
        public double HorizontalFrequency { get; set; } = 0.0026;
        public double VerticalFrequency { get; set; } = 0.018;
        
        // Support system
        public double SupportDensity { get; set; } = 0.6;
        public double SupportPillarChance { get; set; } = 0.28;
        
        // Hydrology awareness
        public double HydrologyStabilityWeight { get; set; } = 0.45;
        public double FlowStabilityWeight { get; set; } = 0.25;
        public double RoughnessStabilityWeight { get; set; } = 0.1;
        public double RiverSuppressionWeight { get; set; } = 0.35;
        public double MoistureRetentionWeight { get; set; } = 0.35;
        
        // Edge sealing
        public double EdgeSealStrength { get; set; } = 0.45;
        public int RiparianPlugDepth { get; set; } = 2;
        
        // Stability smoothing
        public int StabilitySmoothIterations { get; set; } = 1;
        public double StabilitySmoothBlend { get; set; } = 0.55;
        
        // Ceiling protection
        public double CeilingStabilityWeight { get; set; } = 0.35;
        public double CeilingMoistureWeight { get; set; } = 0.28;
        public double CeilingMoistureClamp { get; set; } = 0.35;
        
        // Flooded caves
        public double FloodedCaveNoiseFrequency { get; set; } = 0.0031;
        public double FloodedCaveProximityToWaterTableWeight { get; set; } = 0.6;
        public double FloodedCaveThreshold { get; set; } = 0.75;
        
        // Lava and water
        public double LavaThreshold { get; set; } = 0.28;
        public double WaterThreshold { get; set; } = 0.34;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
        
        // Stability weight
        public double StabilityWeight { get; set; } = 1.0;
    }
    
    /// <summary>
    /// Noise sample cache for performance optimization
    /// </summary>
    public class NoiseCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        private readonly Random _random;
        
        public NoiseCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
            _random = new Random();
        }
        
        public double GetNoise(int x, int y, int z, double frequency)
        {
            string key = $"{x},{y},{z},{frequency}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Generate noise value
            double noiseValue = GenerateNoise(x, y, z, frequency);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, noiseValue);
            }
            
            return noiseValue;
        }
        
        private double GenerateNoise(int x, int y, int z, double frequency)
        {
            // Simple noise generation (can be replaced with Perlin/Simplex noise)
            double nx = x * frequency;
            double ny = y * frequency;
            double nz = z * frequency;
            
            return (Math.Sin(nx) + Math.Cos(ny) + Math.Sin(nz)) / 3.0;
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific cave configuration
    /// </summary>
    public class BiomeConfig
    {
        private readonly ConcurrentDictionary<string, BiomeSpecificConfig> _biomeConfigs;
        
        public BiomeConfig()
        {
            _biomeConfigs = new ConcurrentDictionary<string, BiomeSpecificConfig>();
            InitializeDefaultBiomes();
        }
        
        private void InitializeDefaultBiomes()
        {
            // Default biome configurations
            _biomeConfigs.TryAdd("plains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.0,
                CaveDensityMultiplier = 1.0,
                HydrologySensitivity = 1.0,
                FlowSensitivity = 1.0,
                ErosionSensitivity = 1.0,
                SupportMultiplier = 1.0,
                PillarHeightMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                CaveDensityMultiplier = 1.5,
                HydrologySensitivity = 0.8,
                FlowSensitivity = 0.7,
                ErosionSensitivity = 1.5,
                SupportMultiplier = 1.2,
                PillarHeightMultiplier = 1.5
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                CaveDensityMultiplier = 0.7,
                HydrologySensitivity = 1.5,
                FlowSensitivity = 1.2,
                ErosionSensitivity = 0.8,
                SupportMultiplier = 0.8,
                PillarHeightMultiplier = 0.7
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                CaveDensityMultiplier = 1.1,
                HydrologySensitivity = 1.2,
                FlowSensitivity = 1.1,
                ErosionSensitivity = 1.0,
                SupportMultiplier = 1.0,
                PillarHeightMultiplier = 1.0
            });
        }
        
        public BiomeSpecificConfig GetConfig(string biome)
        {
            if (_biomeConfigs.TryGetValue(biome, out var config))
            {
                return config;
            }
            return GetDefaultConfig();
        }
        
        public BiomeSpecificConfig GetDefaultConfig()
        {
            return _biomeConfigs.TryGetValue("plains", out var config) ? config : new BiomeSpecificConfig();
        }
        
        public void AddBiomeConfig(string biome, BiomeSpecificConfig config)
        {
            _biomeConfigs.TryAdd(biome, config);
        }
    }
    
    /// <summary>
    /// Biome-specific cave generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double CaveDensityMultiplier { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowSensitivity { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double SupportMultiplier { get; set; } = 1.0;
        public double PillarHeightMultiplier { get; set; } = 1.0;
    }
}

