using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized lake generator with multi-threading support and caching
    /// Improvements over ImprovedLakeGenerator:
    /// - Multi-threaded lake basin generation
    /// - Hierarchical lake generation
    /// - Enhanced lake variety with types
    /// - Biome-aware lake generation
    /// </summary>
    public class OptimizedLakeGenerator
    {
        private readonly LakeConfig _config;
        private readonly BasinCache _basinCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedLakeGenerator(LakeConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _basinCache = new BasinCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates lake mask with multi-threading support
        /// </summary>
        public double[,] GenerateLakeMask(int width, int height, 
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask, 
            string biome = null, string lakeType = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (hydrologyMask == null || flowMask == null || erosionRiskMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var lakeMask = new double[width, height];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            var lakeTypeConfig = GetLakeTypeConfig(lakeType);
            
            // Generate lake mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    lakeMask[x, y] = GenerateLakePoint(x, y, 
                        hydrologyMask, flowMask, erosionRiskMask, biomeSpecificConfig, lakeTypeConfig);
                }
            });
            
            // Apply wetland buffer
            lakeMask = ApplyWetlandBufferParallel(lakeMask, width, height, biomeSpecificConfig);
            
            // Apply lake shelves
            lakeMask = ApplyLakeShelvesParallel(lakeMask, width, height, biomeSpecificConfig, lakeTypeConfig);
            
            // Apply outflow channels
            lakeMask = ApplyOutflowChannelsParallel(lakeMask, width, height, 
                flowMask, biomeSpecificConfig, lakeTypeConfig);
            
            // Smooth basin
            lakeMask = SmoothBasinParallel(lakeMask, width, height, biomeSpecificConfig);
            
            return lakeMask;
        }
        
        private double GenerateLakePoint(int x, int y,
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y);
            
            // Get flow value at position
            double flowValue = GetSafeValue(flowMask, x, y);
            
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y);
            
            // Apply biome-specific adjustments
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            flowValue *= biomeConfig.FlowSensitivity;
            erosionRisk *= biomeConfig.ErosionSensitivity;
            
            // Apply lake type-specific adjustments
            hydrologyValue *= lakeTypeConfig.HydrologyMultiplier;
            flowValue *= lakeTypeConfig.FlowMultiplier;
            
            // Calculate lake threshold
            double lakeThreshold = _config.SpawnWeightBias * biomeConfig.ThresholdMultiplier * 
                lakeTypeConfig.ThresholdMultiplier;
            
            // Determine if position is in lake
            if (hydrologyValue > lakeThreshold && flowValue < _config.RiverProximitySuppression)
            {
                // Calculate lake depth based on erosion risk
                double depth = CalculateLakeDepth(erosionRisk, biomeConfig, lakeTypeConfig);
                
                // Apply shoreline blend
                double shorelineBlend = CalculateShorelineBlend(x, y, hydrologyMask, biomeConfig);
                
                return depth * shorelineBlend;
            }
            
            return 0.0;
        }
        
        private double CalculateLakeDepth(double erosionRisk, 
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Calculate depth based on erosion risk
            double depth = _config.MinDepth + (erosionRisk * (_config.MaxDepth - _config.MinDepth));
            
            // Apply biome-specific adjustments
            depth *= biomeConfig.DepthMultiplier;
            
            // Apply lake type-specific adjustments
            depth *= lakeTypeConfig.DepthMultiplier;
            
            return Math.Clamp(depth, _config.MinDepth, _config.MaxDepth);
        }
        
        private double CalculateShorelineBlend(int x, int y, double[,] hydrologyMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Calculate shoreline blend based on hydrology gradient
            double gradient = CalculateGradient(x, y, hydrologyMask);
            
            // Apply biome-specific adjustments
            double shorelineBlend = _config.ShorelineBlend * biomeConfig.ShorelineBlendMultiplier;
            
            // Blend based on gradient (higher gradient = sharper shoreline)
            return Math.Clamp(1.0 - gradient * shorelineBlend, 0.0, 1.0);
        }
        
        private double CalculateGradient(int x, int y, double[,] mask)
        {
            // Calculate gradient at position
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            
            double dx = 0;
            double dy = 0;
            
            // Sample neighbors
            if (x > 0 && x < width - 1)
            {
                dx = (mask[x + 1, y] - mask[x - 1, y]) / 2.0;
            }
            
            if (y > 0 && y < height - 1)
            {
                dy = (mask[x, y + 1] - mask[x, y - 1]) / 2.0;
            }
            
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        private double[,] ApplyWetlandBufferParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var bufferedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    bufferedMask[x, y] = ApplyWetlandBufferPoint(x, y, lakeMask, width, height, biomeConfig);
                }
            });
            
            return bufferedMask;
        }
        
        private double ApplyWetlandBufferPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                // Check if position is in wetland buffer
                double wetlandValue = CalculateWetlandBuffer(x, y, lakeMask, width, height, biomeConfig);
                
                if (wetlandValue > _config.WetlandSaturationThreshold)
                {
                    return wetlandValue;
                }
                
                return 0.0;
            }
            
            return lakeMask[x, y];
        }
        
        private double CalculateWetlandBuffer(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Calculate wetland buffer based on proximity to lake
            double bufferRadius = _config.WetlandBufferRadius * biomeConfig.WetlandBufferMultiplier;
            
            double maxLakeValue = 0;
            int count = 0;
            
            // Sample neighbors within buffer radius
            for (int dx = -(int)bufferRadius; dx <= (int)bufferRadius; dx++)
            {
                for (int dy = -(int)bufferRadius; dy <= (int)bufferRadius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= bufferRadius)
                        {
                            maxLakeValue = Math.Max(maxLakeValue, lakeMask[nx, ny]);
                            count++;
                        }
                    }
                }
            }
            
            // Apply wetland saturation based on proximity
            if (count > 0 && maxLakeValue > 0.1)
            {
                return maxLakeValue * (1.0 - (bufferRadius / (bufferRadius + 1.0)));
            }
            
            return 0.0;
        }
        
        private double[,] ApplyLakeShelvesParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            var shelvedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    shelvedMask[x, y] = ApplyLakeShelvesPoint(x, y, lakeMask, width, height, 
                        biomeConfig, lakeTypeConfig);
                }
            });
            
            return shelvedMask;
        }
        
        private double ApplyLakeShelvesPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Calculate distance to lake shore
            double shoreDistance = CalculateShoreDistance(x, y, lakeMask, width, height);
            
            // Calculate shelf depth
            double shelfDepth = _config.ShelfDepth * biomeConfig.ShelfDepthMultiplier * 
                lakeTypeConfig.ShelfDepthMultiplier;
            
            // Apply shelf effect
            if (shoreDistance < shelfDepth)
            {
                // Shallow water zone
                double shelfRatio = shoreDistance / shelfDepth;
                return lakeMask[x, y] * shelfRatio;
            }
            
            return lakeMask[x, y];
        }
        
        private double CalculateShoreDistance(int x, int y, double[,] lakeMask, int width, int height)
        {
            // Calculate distance to nearest shore
            double minDistance = double.MaxValue;
            
            // Search for nearest shore
            for (int dx = -_config.MaxRadius; dx <= _config.MaxRadius; dx++)
            {
                for (int dy = -_config.MaxRadius; dy <= _config.MaxRadius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (lakeMask[nx, ny] < 0.1)
                        {
                            double distance = Math.Sqrt(dx * dx + dy * dy);
                            minDistance = Math.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            return minDistance;
        }
        
        private double[,] ApplyOutflowChannelsParallel(double[,] lakeMask, int width, int height,
            double[,] flowMask, BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            var channeledMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    channeledMask[x, y] = ApplyOutflowChannelPoint(x, y, lakeMask, flowMask, 
                        width, height, biomeConfig, lakeTypeConfig);
                }
            });
            
            return channeledMask;
        }
        
        private double ApplyOutflowChannelPoint(int x, int y, double[,] lakeMask, double[,] flowMask,
            int width, int height, BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Check if position is at lake edge
            bool isEdge = IsLakeEdge(x, y, lakeMask, width, height);
            
            if (isEdge)
            {
                // Check if there's flow out of lake
                double flowValue = GetSafeValue(flowMask, x, y);
                
                if (flowValue > _config.OutflowStabilityWeight * biomeConfig.OutflowStabilityMultiplier)
                {
                    // Create outflow channel
                    double channelDepth = _config.OutflowCarveDepth * biomeConfig.OutflowDepthMultiplier * 
                        lakeTypeConfig.OutflowDepthMultiplier;
                    
                    return Math.Max(lakeMask[x, y] - channelDepth, 0.0);
                }
            }
            
            return lakeMask[x, y];
        }
        
        private bool IsLakeEdge(int x, int y, double[,] lakeMask, int width, int height)
        {
            // Check if position is at lake edge
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (lakeMask[nx, ny] < 0.1)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private double[,] SmoothBasinParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = (double[,])lakeMask.Clone();
            
            for (int i = 0; i < _config.BasinSmoothIterations; i++)
            {
                smoothedMask = SmoothBasinIteration(smoothedMask, width, height, biomeConfig);
            }
            
            return smoothedMask;
        }
        
        private double[,] SmoothBasinIteration(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    smoothedMask[x, y] = SmoothBasinPoint(x, y, lakeMask, width, height, biomeConfig);
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothBasinPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Sample neighbors
            double sum = 0;
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        sum += lakeMask[nx, ny];
                        count++;
                    }
                }
            }
            
            double average = sum / count;
            
            // Apply variance weight
            double varianceWeight = _config.VarianceWeight * biomeConfig.VarianceMultiplier;
            
            // Apply inflow blend weight
            double inflowBlendWeight = _config.InflowBlendWeight * biomeConfig.InflowBlendMultiplier;
            
            // Apply smoothing
            return lakeMask[x, y] * (1.0 - varianceWeight - inflowBlendWeight) + 
                   average * varianceWeight + 
                   lakeMask[x, y] * inflowBlendWeight;
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
        }
        
        private LakeTypeConfig GetLakeTypeConfig(string lakeType)
        {
            if (string.IsNullOrEmpty(lakeType))
            {
                return _biomeConfig.GetDefaultLakeTypeConfig();
            }
            return _biomeConfig.GetLakeTypeConfig(lakeType);
        }
        
        private double GetSafeValue(double[,] mask, int x, int y)
        {
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
    /// Lake configuration with caching support
    /// </summary>
    public class LakeConfig
    {
        // Lake dimensions
        public int MinDepth { get; set; } = 3;
        public int MaxDepth { get; set; } = 9;
        public int ShelfDepth { get; set; } = 2;
        public int MaxRadius { get; set; } = 9;
        
        // Lake generation
        public int BasinSmoothIterations { get; set; } = 2;
        public double SpawnWeightBias { get; set; } = 0.3;
        public double ShorelineBlend { get; set; } = 0.66;
        public double RiverProximitySuppression { get; set; } = 0.35;
        
        // Wetland handling
        public double WetlandSaturationThreshold { get; set; } = 0.55;
        public int WetlandBufferRadius { get; set; } = 2;
        
        // Outflow handling
        public int OutflowCarveDepth { get; set; } = 2;
        public double OutflowStabilityWeight { get; set; } = 0.3;
        
        // Lake shaping
        public double FlowSeepageWeight { get; set; } = 0.25;
        public double VarianceWeight { get; set; } = 0.25;
        public double RimErosionWeight { get; set; } = 0.3;
        public double InflowBlendWeight { get; set; } = 0.42;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
    }
    
    /// <summary>
    /// Basin cache for performance optimization
    /// </summary>
    public class BasinCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        
        public BasinCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
        }
        
        public double GetBasinValue(int x, int y, string keyPrefix)
        {
            string key = $"{keyPrefix}_{x}_{y}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Generate basin value
            double basinValue = GenerateBasinValue(x, y);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, basinValue);
            }
            
            return basinValue;
        }
        
        private double GenerateBasinValue(int x, int y)
        {
            // Simple basin generation (can be replaced with more complex algorithms)
            return (Math.Sin(x * 0.01) + Math.Cos(y * 0.01)) / 2.0;
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific lake configuration
    /// </summary>
    public class BiomeConfig
    {
        private readonly ConcurrentDictionary<string, BiomeSpecificConfig> _biomeConfigs;
        private readonly ConcurrentDictionary<string, LakeTypeConfig> _lakeTypeConfigs;
        
        public BiomeConfig()
        {
            _biomeConfigs = new ConcurrentDictionary<string, BiomeSpecificConfig>();
            _lakeTypeConfigs = new ConcurrentDictionary<string, LakeTypeConfig>();
            InitializeDefaultBiomes();
            InitializeDefaultLakeTypes();
        }
        
        private void InitializeDefaultBiomes()
        {
            // Default biome configurations
            _biomeConfigs.TryAdd("plains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.0,
                DepthMultiplier = 1.0,
                HydrologySensitivity = 1.0,
                FlowSensitivity = 1.0,
                ErosionSensitivity = 1.0,
                ShorelineBlendMultiplier = 1.0,
                WetlandBufferMultiplier = 1.0,
                ShelfDepthMultiplier = 1.0,
                OutflowStabilityMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0,
                VarianceMultiplier = 1.0,
                InflowBlendMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                DepthMultiplier = 1.5,
                HydrologySensitivity = 0.8,
                FlowSensitivity = 1.2,
                ErosionSensitivity = 1.5,
                ShorelineBlendMultiplier = 0.8,
                WetlandBufferMultiplier = 0.8,
                ShelfDepthMultiplier = 1.5,
                OutflowStabilityMultiplier = 1.2,
                OutflowDepthMultiplier = 1.5,
                VarianceMultiplier = 1.2,
                InflowBlendMultiplier = 0.8
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                DepthMultiplier = 0.7,
                HydrologySensitivity = 1.5,
                FlowSensitivity = 0.8,
                ErosionSensitivity = 0.8,
                ShorelineBlendMultiplier = 1.2,
                WetlandBufferMultiplier = 0.7,
                ShelfDepthMultiplier = 0.7,
                OutflowStabilityMultiplier = 0.8,
                OutflowDepthMultiplier = 0.7,
                VarianceMultiplier = 0.8,
                InflowBlendMultiplier = 1.2
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                DepthMultiplier = 1.1,
                HydrologySensitivity = 1.2,
                FlowSensitivity = 1.1,
                ErosionSensitivity = 1.0,
                ShorelineBlendMultiplier = 1.0,
                WetlandBufferMultiplier = 1.2,
                ShelfDepthMultiplier = 1.0,
                OutflowStabilityMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0,
                VarianceMultiplier = 1.0,
                InflowBlendMultiplier = 1.0
            });
        }
        
        private void InitializeDefaultLakeTypes()
        {
            // Default lake type configurations
            _lakeTypeConfigs.TryAdd("alpine", new LakeTypeConfig
            {
                ThresholdMultiplier = 0.8,
                DepthMultiplier = 1.5,
                HydrologyMultiplier = 0.8,
                FlowMultiplier = 1.2,
                ShelfDepthMultiplier = 1.5,
                OutflowDepthMultiplier = 1.5
            });
            
            _lakeTypeConfigs.TryAdd("crater", new LakeTypeConfig
            {
                ThresholdMultiplier = 0.7,
                DepthMultiplier = 2.0,
                HydrologyMultiplier = 0.7,
                FlowMultiplier = 0.8,
                ShelfDepthMultiplier = 0.5,
                OutflowDepthMultiplier = 0.5
            });
            
            _lakeTypeConfigs.TryAdd("oxbow", new LakeTypeConfig
            {
                ThresholdMultiplier = 1.2,
                DepthMultiplier = 0.8,
                HydrologyMultiplier = 1.2,
                FlowMultiplier = 1.5,
                ShelfDepthMultiplier = 1.2,
                OutflowDepthMultiplier = 1.5
            });
            
            _lakeTypeConfigs.TryAdd("plain", new LakeTypeConfig
            {
                ThresholdMultiplier = 1.0,
                DepthMultiplier = 1.0,
                HydrologyMultiplier = 1.0,
                FlowMultiplier = 1.0,
                ShelfDepthMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0
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
        
        public LakeTypeConfig GetLakeTypeConfig(string lakeType)
        {
            if (_lakeTypeConfigs.TryGetValue(lakeType, out var config))
            {
                return config;
            }
            return GetDefaultLakeTypeConfig();
        }
        
        public LakeTypeConfig GetDefaultLakeTypeConfig()
        {
            return _lakeTypeConfigs.TryGetValue("plain", out var config) ? config : new LakeTypeConfig();
        }
        
        public void AddBiomeConfig(string biome, BiomeSpecificConfig config)
        {
            _biomeConfigs.TryAdd(biome, config);
        }
        
        public void AddLakeTypeConfig(string lakeType, LakeTypeConfig config)
        {
            _lakeTypeConfigs.TryAdd(lakeType, config);
        }
    }
    
    /// <summary>
    /// Biome-specific lake generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double DepthMultiplier { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowSensitivity { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double ShorelineBlendMultiplier { get; set; } = 1.0;
        public double WetlandBufferMultiplier { get; set; } = 1.0;
        public double ShelfDepthMultiplier { get; set; } = 1.0;
        public double OutflowStabilityMultiplier { get; set; } = 1.0;
        public double OutflowDepthMultiplier { get; set; } = 1.0;
        public double VarianceMultiplier { get; set; } = 1.0;
        public double InflowBlendMultiplier { get; set; } = 1.0;
    }
    
    /// <summary>
    /// Lake type-specific generation parameters
    /// </summary>
    public class LakeTypeConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double DepthMultiplier { get; set; } = 1.0;
        public double HydrologyMultiplier { get; set; } = 1.0;
        public double FlowMultiplier { get; set; } = 1.0;
        public double ShelfDepthMultiplier { get; set; } = 1.0;
        public double OutflowDepthMultiplier { get; set; } = 1.0;
    }
}
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized lake generator with multi-threading support and caching
    /// Improvements over ImprovedLakeGenerator:
    /// - Multi-threaded lake basin generation
    /// - Hierarchical lake generation
    /// - Enhanced lake variety with types
    /// - Biome-aware lake generation
    /// </summary>
    public class OptimizedLakeGenerator
    {
        private readonly LakeConfig _config;
        private readonly BasinCache _basinCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedLakeGenerator(LakeConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _basinCache = new BasinCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates lake mask with multi-threading support
        /// </summary>
        public double[,] GenerateLakeMask(int width, int height, 
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask, 
            string biome = null, string lakeType = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (hydrologyMask == null || flowMask == null || erosionRiskMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var lakeMask = new double[width, height];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            var lakeTypeConfig = GetLakeTypeConfig(lakeType);
            
            // Generate lake mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    lakeMask[x, y] = GenerateLakePoint(x, y, 
                        hydrologyMask, flowMask, erosionRiskMask, biomeSpecificConfig, lakeTypeConfig);
                }
            });
            
            // Apply wetland buffer
            lakeMask = ApplyWetlandBufferParallel(lakeMask, width, height, biomeSpecificConfig);
            
            // Apply lake shelves
            lakeMask = ApplyLakeShelvesParallel(lakeMask, width, height, biomeSpecificConfig, lakeTypeConfig);
            
            // Apply outflow channels
            lakeMask = ApplyOutflowChannelsParallel(lakeMask, width, height, 
                flowMask, biomeSpecificConfig, lakeTypeConfig);
            
            // Smooth basin
            lakeMask = SmoothBasinParallel(lakeMask, width, height, biomeSpecificConfig);
            
            return lakeMask;
        }
        
        private double GenerateLakePoint(int x, int y,
            double[,] hydrologyMask, double[,] flowMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y);
            
            // Get flow value at position
            double flowValue = GetSafeValue(flowMask, x, y);
            
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y);
            
            // Apply biome-specific adjustments
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            flowValue *= biomeConfig.FlowSensitivity;
            erosionRisk *= biomeConfig.ErosionSensitivity;
            
            // Apply lake type-specific adjustments
            hydrologyValue *= lakeTypeConfig.HydrologyMultiplier;
            flowValue *= lakeTypeConfig.FlowMultiplier;
            
            // Calculate lake threshold
            double lakeThreshold = _config.SpawnWeightBias * biomeConfig.ThresholdMultiplier * 
                lakeTypeConfig.ThresholdMultiplier;
            
            // Determine if position is in lake
            if (hydrologyValue > lakeThreshold && flowValue < _config.RiverProximitySuppression)
            {
                // Calculate lake depth based on erosion risk
                double depth = CalculateLakeDepth(erosionRisk, biomeConfig, lakeTypeConfig);
                
                // Apply shoreline blend
                double shorelineBlend = CalculateShorelineBlend(x, y, hydrologyMask, biomeConfig);
                
                return depth * shorelineBlend;
            }
            
            return 0.0;
        }
        
        private double CalculateLakeDepth(double erosionRisk, 
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Calculate depth based on erosion risk
            double depth = _config.MinDepth + (erosionRisk * (_config.MaxDepth - _config.MinDepth));
            
            // Apply biome-specific adjustments
            depth *= biomeConfig.DepthMultiplier;
            
            // Apply lake type-specific adjustments
            depth *= lakeTypeConfig.DepthMultiplier;
            
            return Math.Clamp(depth, _config.MinDepth, _config.MaxDepth);
        }
        
        private double CalculateShorelineBlend(int x, int y, double[,] hydrologyMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Calculate shoreline blend based on hydrology gradient
            double gradient = CalculateGradient(x, y, hydrologyMask);
            
            // Apply biome-specific adjustments
            double shorelineBlend = _config.ShorelineBlend * biomeConfig.ShorelineBlendMultiplier;
            
            // Blend based on gradient (higher gradient = sharper shoreline)
            return Math.Clamp(1.0 - gradient * shorelineBlend, 0.0, 1.0);
        }
        
        private double CalculateGradient(int x, int y, double[,] mask)
        {
            // Calculate gradient at position
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            
            double dx = 0;
            double dy = 0;
            
            // Sample neighbors
            if (x > 0 && x < width - 1)
            {
                dx = (mask[x + 1, y] - mask[x - 1, y]) / 2.0;
            }
            
            if (y > 0 && y < height - 1)
            {
                dy = (mask[x, y + 1] - mask[x, y - 1]) / 2.0;
            }
            
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        private double[,] ApplyWetlandBufferParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var bufferedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    bufferedMask[x, y] = ApplyWetlandBufferPoint(x, y, lakeMask, width, height, biomeConfig);
                }
            });
            
            return bufferedMask;
        }
        
        private double ApplyWetlandBufferPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                // Check if position is in wetland buffer
                double wetlandValue = CalculateWetlandBuffer(x, y, lakeMask, width, height, biomeConfig);
                
                if (wetlandValue > _config.WetlandSaturationThreshold)
                {
                    return wetlandValue;
                }
                
                return 0.0;
            }
            
            return lakeMask[x, y];
        }
        
        private double CalculateWetlandBuffer(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Calculate wetland buffer based on proximity to lake
            double bufferRadius = _config.WetlandBufferRadius * biomeConfig.WetlandBufferMultiplier;
            
            double maxLakeValue = 0;
            int count = 0;
            
            // Sample neighbors within buffer radius
            for (int dx = -(int)bufferRadius; dx <= (int)bufferRadius; dx++)
            {
                for (int dy = -(int)bufferRadius; dy <= (int)bufferRadius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= bufferRadius)
                        {
                            maxLakeValue = Math.Max(maxLakeValue, lakeMask[nx, ny]);
                            count++;
                        }
                    }
                }
            }
            
            // Apply wetland saturation based on proximity
            if (count > 0 && maxLakeValue > 0.1)
            {
                return maxLakeValue * (1.0 - (bufferRadius / (bufferRadius + 1.0)));
            }
            
            return 0.0;
        }
        
        private double[,] ApplyLakeShelvesParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            var shelvedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    shelvedMask[x, y] = ApplyLakeShelvesPoint(x, y, lakeMask, width, height, 
                        biomeConfig, lakeTypeConfig);
                }
            });
            
            return shelvedMask;
        }
        
        private double ApplyLakeShelvesPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Calculate distance to lake shore
            double shoreDistance = CalculateShoreDistance(x, y, lakeMask, width, height);
            
            // Calculate shelf depth
            double shelfDepth = _config.ShelfDepth * biomeConfig.ShelfDepthMultiplier * 
                lakeTypeConfig.ShelfDepthMultiplier;
            
            // Apply shelf effect
            if (shoreDistance < shelfDepth)
            {
                // Shallow water zone
                double shelfRatio = shoreDistance / shelfDepth;
                return lakeMask[x, y] * shelfRatio;
            }
            
            return lakeMask[x, y];
        }
        
        private double CalculateShoreDistance(int x, int y, double[,] lakeMask, int width, int height)
        {
            // Calculate distance to nearest shore
            double minDistance = double.MaxValue;
            
            // Search for nearest shore
            for (int dx = -_config.MaxRadius; dx <= _config.MaxRadius; dx++)
            {
                for (int dy = -_config.MaxRadius; dy <= _config.MaxRadius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (lakeMask[nx, ny] < 0.1)
                        {
                            double distance = Math.Sqrt(dx * dx + dy * dy);
                            minDistance = Math.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            return minDistance;
        }
        
        private double[,] ApplyOutflowChannelsParallel(double[,] lakeMask, int width, int height,
            double[,] flowMask, BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            var channeledMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    channeledMask[x, y] = ApplyOutflowChannelPoint(x, y, lakeMask, flowMask, 
                        width, height, biomeConfig, lakeTypeConfig);
                }
            });
            
            return channeledMask;
        }
        
        private double ApplyOutflowChannelPoint(int x, int y, double[,] lakeMask, double[,] flowMask,
            int width, int height, BiomeSpecificConfig biomeConfig, LakeTypeConfig lakeTypeConfig)
        {
            // Check if position is in lake
            if (lakeMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Check if position is at lake edge
            bool isEdge = IsLakeEdge(x, y, lakeMask, width, height);
            
            if (isEdge)
            {
                // Check if there's flow out of lake
                double flowValue = GetSafeValue(flowMask, x, y);
                
                if (flowValue > _config.OutflowStabilityWeight * biomeConfig.OutflowStabilityMultiplier)
                {
                    // Create outflow channel
                    double channelDepth = _config.OutflowCarveDepth * biomeConfig.OutflowDepthMultiplier * 
                        lakeTypeConfig.OutflowDepthMultiplier;
                    
                    return Math.Max(lakeMask[x, y] - channelDepth, 0.0);
                }
            }
            
            return lakeMask[x, y];
        }
        
        private bool IsLakeEdge(int x, int y, double[,] lakeMask, int width, int height)
        {
            // Check if position is at lake edge
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (lakeMask[nx, ny] < 0.1)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private double[,] SmoothBasinParallel(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = (double[,])lakeMask.Clone();
            
            for (int i = 0; i < _config.BasinSmoothIterations; i++)
            {
                smoothedMask = SmoothBasinIteration(smoothedMask, width, height, biomeConfig);
            }
            
            return smoothedMask;
        }
        
        private double[,] SmoothBasinIteration(double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    smoothedMask[x, y] = SmoothBasinPoint(x, y, lakeMask, width, height, biomeConfig);
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothBasinPoint(int x, int y, double[,] lakeMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Sample neighbors
            double sum = 0;
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        sum += lakeMask[nx, ny];
                        count++;
                    }
                }
            }
            
            double average = sum / count;
            
            // Apply variance weight
            double varianceWeight = _config.VarianceWeight * biomeConfig.VarianceMultiplier;
            
            // Apply inflow blend weight
            double inflowBlendWeight = _config.InflowBlendWeight * biomeConfig.InflowBlendMultiplier;
            
            // Apply smoothing
            return lakeMask[x, y] * (1.0 - varianceWeight - inflowBlendWeight) + 
                   average * varianceWeight + 
                   lakeMask[x, y] * inflowBlendWeight;
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
        }
        
        private LakeTypeConfig GetLakeTypeConfig(string lakeType)
        {
            if (string.IsNullOrEmpty(lakeType))
            {
                return _biomeConfig.GetDefaultLakeTypeConfig();
            }
            return _biomeConfig.GetLakeTypeConfig(lakeType);
        }
        
        private double GetSafeValue(double[,] mask, int x, int y)
        {
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
    /// Lake configuration with caching support
    /// </summary>
    public class LakeConfig
    {
        // Lake dimensions
        public int MinDepth { get; set; } = 3;
        public int MaxDepth { get; set; } = 9;
        public int ShelfDepth { get; set; } = 2;
        public int MaxRadius { get; set; } = 9;
        
        // Lake generation
        public int BasinSmoothIterations { get; set; } = 2;
        public double SpawnWeightBias { get; set; } = 0.3;
        public double ShorelineBlend { get; set; } = 0.66;
        public double RiverProximitySuppression { get; set; } = 0.35;
        
        // Wetland handling
        public double WetlandSaturationThreshold { get; set; } = 0.55;
        public int WetlandBufferRadius { get; set; } = 2;
        
        // Outflow handling
        public int OutflowCarveDepth { get; set; } = 2;
        public double OutflowStabilityWeight { get; set; } = 0.3;
        
        // Lake shaping
        public double FlowSeepageWeight { get; set; } = 0.25;
        public double VarianceWeight { get; set; } = 0.25;
        public double RimErosionWeight { get; set; } = 0.3;
        public double InflowBlendWeight { get; set; } = 0.42;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
    }
    
    /// <summary>
    /// Basin cache for performance optimization
    /// </summary>
    public class BasinCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        
        public BasinCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
        }
        
        public double GetBasinValue(int x, int y, string keyPrefix)
        {
            string key = $"{keyPrefix}_{x}_{y}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Generate basin value
            double basinValue = GenerateBasinValue(x, y);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, basinValue);
            }
            
            return basinValue;
        }
        
        private double GenerateBasinValue(int x, int y)
        {
            // Simple basin generation (can be replaced with more complex algorithms)
            return (Math.Sin(x * 0.01) + Math.Cos(y * 0.01)) / 2.0;
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific lake configuration
    /// </summary>
    public class BiomeConfig
    {
        private readonly ConcurrentDictionary<string, BiomeSpecificConfig> _biomeConfigs;
        private readonly ConcurrentDictionary<string, LakeTypeConfig> _lakeTypeConfigs;
        
        public BiomeConfig()
        {
            _biomeConfigs = new ConcurrentDictionary<string, BiomeSpecificConfig>();
            _lakeTypeConfigs = new ConcurrentDictionary<string, LakeTypeConfig>();
            InitializeDefaultBiomes();
            InitializeDefaultLakeTypes();
        }
        
        private void InitializeDefaultBiomes()
        {
            // Default biome configurations
            _biomeConfigs.TryAdd("plains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.0,
                DepthMultiplier = 1.0,
                HydrologySensitivity = 1.0,
                FlowSensitivity = 1.0,
                ErosionSensitivity = 1.0,
                ShorelineBlendMultiplier = 1.0,
                WetlandBufferMultiplier = 1.0,
                ShelfDepthMultiplier = 1.0,
                OutflowStabilityMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0,
                VarianceMultiplier = 1.0,
                InflowBlendMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                DepthMultiplier = 1.5,
                HydrologySensitivity = 0.8,
                FlowSensitivity = 1.2,
                ErosionSensitivity = 1.5,
                ShorelineBlendMultiplier = 0.8,
                WetlandBufferMultiplier = 0.8,
                ShelfDepthMultiplier = 1.5,
                OutflowStabilityMultiplier = 1.2,
                OutflowDepthMultiplier = 1.5,
                VarianceMultiplier = 1.2,
                InflowBlendMultiplier = 0.8
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                DepthMultiplier = 0.7,
                HydrologySensitivity = 1.5,
                FlowSensitivity = 0.8,
                ErosionSensitivity = 0.8,
                ShorelineBlendMultiplier = 1.2,
                WetlandBufferMultiplier = 0.7,
                ShelfDepthMultiplier = 0.7,
                OutflowStabilityMultiplier = 0.8,
                OutflowDepthMultiplier = 0.7,
                VarianceMultiplier = 0.8,
                InflowBlendMultiplier = 1.2
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                DepthMultiplier = 1.1,
                HydrologySensitivity = 1.2,
                FlowSensitivity = 1.1,
                ErosionSensitivity = 1.0,
                ShorelineBlendMultiplier = 1.0,
                WetlandBufferMultiplier = 1.2,
                ShelfDepthMultiplier = 1.0,
                OutflowStabilityMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0,
                VarianceMultiplier = 1.0,
                InflowBlendMultiplier = 1.0
            });
        }
        
        private void InitializeDefaultLakeTypes()
        {
            // Default lake type configurations
            _lakeTypeConfigs.TryAdd("alpine", new LakeTypeConfig
            {
                ThresholdMultiplier = 0.8,
                DepthMultiplier = 1.5,
                HydrologyMultiplier = 0.8,
                FlowMultiplier = 1.2,
                ShelfDepthMultiplier = 1.5,
                OutflowDepthMultiplier = 1.5
            });
            
            _lakeTypeConfigs.TryAdd("crater", new LakeTypeConfig
            {
                ThresholdMultiplier = 0.7,
                DepthMultiplier = 2.0,
                HydrologyMultiplier = 0.7,
                FlowMultiplier = 0.8,
                ShelfDepthMultiplier = 0.5,
                OutflowDepthMultiplier = 0.5
            });
            
            _lakeTypeConfigs.TryAdd("oxbow", new LakeTypeConfig
            {
                ThresholdMultiplier = 1.2,
                DepthMultiplier = 0.8,
                HydrologyMultiplier = 1.2,
                FlowMultiplier = 1.5,
                ShelfDepthMultiplier = 1.2,
                OutflowDepthMultiplier = 1.5
            });
            
            _lakeTypeConfigs.TryAdd("plain", new LakeTypeConfig
            {
                ThresholdMultiplier = 1.0,
                DepthMultiplier = 1.0,
                HydrologyMultiplier = 1.0,
                FlowMultiplier = 1.0,
                ShelfDepthMultiplier = 1.0,
                OutflowDepthMultiplier = 1.0
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
        
        public LakeTypeConfig GetLakeTypeConfig(string lakeType)
        {
            if (_lakeTypeConfigs.TryGetValue(lakeType, out var config))
            {
                return config;
            }
            return GetDefaultLakeTypeConfig();
        }
        
        public LakeTypeConfig GetDefaultLakeTypeConfig()
        {
            return _lakeTypeConfigs.TryGetValue("plain", out var config) ? config : new LakeTypeConfig();
        }
        
        public void AddBiomeConfig(string biome, BiomeSpecificConfig config)
        {
            _biomeConfigs.TryAdd(biome, config);
        }
        
        public void AddLakeTypeConfig(string lakeType, LakeTypeConfig config)
        {
            _lakeTypeConfigs.TryAdd(lakeType, config);
        }
    }
    
    /// <summary>
    /// Biome-specific lake generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double DepthMultiplier { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowSensitivity { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double ShorelineBlendMultiplier { get; set; } = 1.0;
        public double WetlandBufferMultiplier { get; set; } = 1.0;
        public double ShelfDepthMultiplier { get; set; } = 1.0;
        public double OutflowStabilityMultiplier { get; set; } = 1.0;
        public double OutflowDepthMultiplier { get; set; } = 1.0;
        public double VarianceMultiplier { get; set; } = 1.0;
        public double InflowBlendMultiplier { get; set; } = 1.0;
    }
    
    /// <summary>
    /// Lake type-specific generation parameters
    /// </summary>
    public class LakeTypeConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double DepthMultiplier { get; set; } = 1.0;
        public double HydrologyMultiplier { get; set; } = 1.0;
        public double FlowMultiplier { get; set; } = 1.0;
        public double ShelfDepthMultiplier { get; set; } = 1.0;
        public double OutflowDepthMultiplier { get; set; } = 1.0;
    }
}

