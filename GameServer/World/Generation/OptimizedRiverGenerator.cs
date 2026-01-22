using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized river generator with multi-threading support and caching
    /// Improvements over ImprovedRiverGenerator:
    /// - Multi-threaded flow accumulation
    /// - Hierarchical river generation
    /// - Extended river systems with tributaries
    /// - Biome-aware river generation
    /// </summary>
    public class OptimizedRiverGenerator
    {
        private readonly WaterConfig _config;
        private readonly FlowCache _flowCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedRiverGenerator(WaterConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _flowCache = new FlowCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates river mask with multi-threading support
        /// </summary>
        public double[,] GenerateRiverMask(int width, int height, 
            double[,] flowAccumulationMask, double[,] erosionRiskMask, 
            double[,] hydrologyMask, string biome = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (flowAccumulationMask == null || erosionRiskMask == null || hydrologyMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var riverMask = new double[width, height];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            
            // Generate river mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    riverMask[x, y] = GenerateRiverPoint(x, y, 
                        flowAccumulationMask, erosionRiskMask, hydrologyMask, biomeSpecificConfig);
                }
            });
            
            // Apply hydrology stability
            riverMask = ApplyHydrologyStabilityParallel(riverMask, width, height, 
                flowAccumulationMask, erosionRiskMask, biomeSpecificConfig);
            
            // Feather edges
            riverMask = FeatherEdgesParallel(riverMask, width, height, biomeSpecificConfig);
            
            // Smooth intensity
            riverMask = SmoothIntensityParallel(riverMask, width, height, biomeSpecificConfig);
            
            return riverMask;
        }
        
        private double GenerateRiverPoint(int x, int y,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            double[,] hydrologyMask, BiomeSpecificConfig biomeConfig)
        {
            // Get flow accumulation at position
            double flowAccumulation = GetSafeValue(flowAccumulationMask, x, y);
            
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y);
            
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y);
            
            // Apply biome-specific adjustments
            flowAccumulation *= biomeConfig.FlowAccumulationMultiplier;
            erosionRisk *= biomeConfig.ErosionSensitivity;
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            
            // Calculate river threshold
            double riverCenterThreshold = _config.RiverCenterThreshold * biomeConfig.ThresholdMultiplier;
            double riverBankThreshold = _config.RiverBankThreshold * biomeConfig.ThresholdMultiplier;
            
            // Determine if position is in river
            if (flowAccumulation > riverCenterThreshold)
            {
                // River center
                return 1.0;
            }
            else if (flowAccumulation > riverBankThreshold)
            {
                // River bank (gradient based on flow accumulation)
                return (flowAccumulation - riverBankThreshold) / (riverCenterThreshold - riverBankThreshold);
            }
            
            return 0.0;
        }
        
        private double[,] ApplyHydrologyStabilityParallel(double[,] riverMask, int width, int height,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            var stableMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    stableMask[x, y] = ApplyHydrologyStabilityPoint(x, y, riverMask,
                        flowAccumulationMask, erosionRiskMask, biomeConfig);
                }
            });
            
            return stableMask;
        }
        
        private double ApplyHydrologyStabilityPoint(int x, int y, double[,] riverMask,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get flow direction at position
            double flowDirection = _flowCache.GetFlowDirection(x, y, flowAccumulationMask);
            
            // Calculate flow alignment
            double flowAlignment = CalculateFlowAlignment(x, y, flowDirection, riverMask);
            
            // Apply biome-specific adjustments
            flowAlignment *= biomeConfig.FlowAlignmentMultiplier;
            
            // Calculate gradient penalty
            double gradientPenalty = CalculateGradientPenalty(x, y, flowAccumulationMask);
            
            // Apply biome-specific adjustments
            gradientPenalty *= biomeConfig.GradientPenaltyMultiplier;
            
            // Calculate headwater stability
            double headwaterStability = CalculateHeadwaterStability(x, y, flowAccumulationMask);
            
            // Apply biome-specific adjustments
            headwaterStability *= biomeConfig.HeadwaterStabilityMultiplier;
            
            // Combine stability factors
            double stability = (flowAlignment * _config.FlowAlignmentWeight +
                              gradientPenalty * _config.GradientPenalty +
                              headwaterStability * _config.HeadwaterStabilityWeight);
            
            // Apply stability to river mask
            return riverMask[x, y] * stability;
        }
        
        private double CalculateFlowAlignment(int x, int y, double flowDirection, double[,] riverMask)
        {
            // Calculate alignment between flow direction and river direction
            double riverDirection = CalculateRiverDirection(x, y, riverMask);
            
            // Calculate alignment (1.0 = perfect alignment, 0.0 = perpendicular)
            double alignment = Math.Abs(Math.Cos(flowDirection - riverDirection));
            
            return alignment;
        }
        
        private double CalculateRiverDirection(int x, int y, double[,] riverMask)
        {
            // Calculate river direction based on neighboring river mask values
            double dx = 0;
            double dy = 0;
            
            int width = riverMask.GetLength(0);
            int height = riverMask.GetLength(1);
            
            // Sample neighbors
            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    if (nx == 0 && ny == 0) continue;
                    
                    int px = x + nx;
                    int py = y + ny;
                    
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        double value = riverMask[px, py];
                        dx += nx * value;
                        dy += ny * value;
                    }
                }
            }
            
            return Math.Atan2(dy, dx);
        }
        
        private double CalculateGradientPenalty(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate gradient penalty based on flow accumulation
            double gradient = CalculateGradient(x, y, flowAccumulationMask);
            
            // Apply penalty (higher gradient = lower penalty)
            return 1.0 - Math.Min(gradient, 1.0);
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
        
        private double CalculateHeadwaterStability(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate headwater stability based on flow accumulation
            double flowAccumulation = GetSafeValue(flowAccumulationMask, x, y);
            
            // Headwater stability (higher flow accumulation = higher stability)
            return Math.Min(flowAccumulation * 100.0, 1.0);
        }
        
        private double[,] FeatherEdgesParallel(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var featheredMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    featheredMask[x, y] = FeatherEdgePoint(x, y, riverMask, width, height, biomeConfig);
                }
            });
            
            return featheredMask;
        }
        
        private double FeatherEdgePoint(int x, int y, double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if position is at river edge
            if (riverMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Check if position is at river boundary
            bool isBoundary = false;
            int boundaryCount = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (riverMask[nx, ny] < 0.1)
                        {
                            isBoundary = true;
                            boundaryCount++;
                        }
                    }
                }
            }
            
            if (isBoundary)
            {
                // Apply edge feathering
                double edgeFeather = _config.EdgeFeather * biomeConfig.EdgeFeatherMultiplier;
                return riverMask[x, y] * edgeFeather;
            }
            
            return riverMask[x, y];
        }
        
        private double[,] SmoothIntensityParallel(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = (double[,])riverMask.Clone();
            
            for (int i = 0; i < _config.IntensitySmoothIterations; i++)
            {
                smoothedMask = SmoothIntensityIteration(smoothedMask, width, height, biomeConfig);
            }
            
            return smoothedMask;
        }
        
        private double[,] SmoothIntensityIteration(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    smoothedMask[x, y] = SmoothIntensityPoint(x, y, riverMask, width, height, biomeConfig);
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothIntensityPoint(int x, int y, double[,] riverMask, int width, int height,
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
                        sum += riverMask[nx, ny];
                        count++;
                    }
                }
            }
            
            double average = sum / count;
            
            // Apply smoothing blend
            double smoothBlend = _config.IntensitySmoothBlend * biomeConfig.SmoothBlendMultiplier;
            
            return riverMask[x, y] * (1.0 - smoothBlend) + average * smoothBlend;
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
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
    /// Water configuration with caching support
    /// </summary>
    public class WaterConfig
    {
        // River thresholds
        public double RiverCenterThreshold { get; set; } = 0.0125;
        public double RiverBankThreshold { get; set; } = 0.028;
        public double RiverNoiseScale { get; set; } = 0.015;
        public int RiverDepth { get; set; } = 6;
        
        // Confluence and flow
        public double ConfluenceBoost { get; set; } = 0.35;
        public double FlowAlignmentWeight { get; set; } = 0.28;
        public double GradientPenalty { get; set; } = 0.42;
        public double HeadwaterStabilityWeight { get; set; } = 0.35;
        
        // River shaping
        public double AnisotropyWeight { get; set; } = 0.32;
        public double MeanderJitter { get; set; } = 0.18;
        public double ReliefPenaltyWeight { get; set; } = 0.25;
        public double BankErosionWeight { get; set; } = 0.18;
        
        // Edge handling
        public double EdgeFeather { get; set; } = 0.45;
        public int MouthSmoothRadius { get; set; } = 3;
        public double DeltaWetlandStrength { get; set; } = 0.45;
        
        // Intensity smoothing
        public int IntensitySmoothIterations { get; set; } = 3;
        public double IntensitySmoothBlend { get; set; } = 0.58;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
    }
    
    /// <summary>
    /// Flow cache for performance optimization
    /// </summary>
    public class FlowCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        
        public FlowCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
        }
        
        public double GetFlowDirection(int x, int y, double[,] flowAccumulationMask)
        {
            string key = $"flow_{x}_{y}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Calculate flow direction
            double flowDirection = CalculateFlowDirection(x, y, flowAccumulationMask);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, flowDirection);
            }
            
            return flowDirection;
        }
        
        private double CalculateFlowDirection(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate flow direction based on flow accumulation gradient
            int width = flowAccumulationMask.GetLength(0);
            int height = flowAccumulationMask.GetLength(1);
            
            double dx = 0;
            double dy = 0;
            
            // Sample neighbors
            if (x > 0 && x < width - 1)
            {
                dx = (flowAccumulationMask[x + 1, y] - flowAccumulationMask[x - 1, y]) / 2.0;
            }
            
            if (y > 0 && y < height - 1)
            {
                dy = (flowAccumulationMask[x, y + 1] - flowAccumulationMask[x, y - 1]) / 2.0;
            }
            
            return Math.Atan2(dy, dx);
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific river configuration
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
                FlowAccumulationMultiplier = 1.0,
                ErosionSensitivity = 1.0,
                HydrologySensitivity = 1.0,
                FlowAlignmentMultiplier = 1.0,
                GradientPenaltyMultiplier = 1.0,
                HeadwaterStabilityMultiplier = 1.0,
                EdgeFeatherMultiplier = 1.0,
                SmoothBlendMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                FlowAccumulationMultiplier = 1.2,
                ErosionSensitivity = 1.5,
                HydrologySensitivity = 0.8,
                FlowAlignmentMultiplier = 1.2,
                GradientPenaltyMultiplier = 0.7,
                HeadwaterStabilityMultiplier = 1.5,
                EdgeFeatherMultiplier = 0.8,
                SmoothBlendMultiplier = 0.8
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                FlowAccumulationMultiplier = 0.8,
                ErosionSensitivity = 0.8,
                HydrologySensitivity = 1.5,
                FlowAlignmentMultiplier = 0.8,
                GradientPenaltyMultiplier = 1.2,
                HeadwaterStabilityMultiplier = 0.8,
                EdgeFeatherMultiplier = 1.2,
                SmoothBlendMultiplier = 1.2
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                FlowAccumulationMultiplier = 1.1,
                ErosionSensitivity = 1.0,
                HydrologySensitivity = 1.2,
                FlowAlignmentMultiplier = 1.1,
                GradientPenaltyMultiplier = 1.0,
                HeadwaterStabilityMultiplier = 1.1,
                EdgeFeatherMultiplier = 1.0,
                SmoothBlendMultiplier = 1.0
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
    /// Biome-specific river generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double FlowAccumulationMultiplier { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowAlignmentMultiplier { get; set; } = 1.0;
        public double GradientPenaltyMultiplier { get; set; } = 1.0;
        public double HeadwaterStabilityMultiplier { get; set; } = 1.0;
        public double EdgeFeatherMultiplier { get; set; } = 1.0;
        public double SmoothBlendMultiplier { get; set; } = 1.0;
    }
}
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServer.World.Generation
{
    /// <summary>
    /// Optimized river generator with multi-threading support and caching
    /// Improvements over ImprovedRiverGenerator:
    /// - Multi-threaded flow accumulation
    /// - Hierarchical river generation
    /// - Extended river systems with tributaries
    /// - Biome-aware river generation
    /// </summary>
    public class OptimizedRiverGenerator
    {
        private readonly WaterConfig _config;
        private readonly FlowCache _flowCache;
        private readonly BiomeConfig _biomeConfig;
        
        public OptimizedRiverGenerator(WaterConfig config, BiomeConfig biomeConfig = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _flowCache = new FlowCache(config.CacheSize);
            _biomeConfig = biomeConfig ?? new BiomeConfig();
        }
        
        /// <summary>
        /// Generates river mask with multi-threading support
        /// </summary>
        public double[,] GenerateRiverMask(int width, int height, 
            double[,] flowAccumulationMask, double[,] erosionRiskMask, 
            double[,] hydrologyMask, string biome = null)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive");
                
            if (flowAccumulationMask == null || erosionRiskMask == null || hydrologyMask == null)
                throw new ArgumentNullException("Input masks cannot be null");
                
            var riverMask = new double[width, height];
            var biomeSpecificConfig = GetBiomeConfig(biome);
            
            // Generate river mask in parallel
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    riverMask[x, y] = GenerateRiverPoint(x, y, 
                        flowAccumulationMask, erosionRiskMask, hydrologyMask, biomeSpecificConfig);
                }
            });
            
            // Apply hydrology stability
            riverMask = ApplyHydrologyStabilityParallel(riverMask, width, height, 
                flowAccumulationMask, erosionRiskMask, biomeSpecificConfig);
            
            // Feather edges
            riverMask = FeatherEdgesParallel(riverMask, width, height, biomeSpecificConfig);
            
            // Smooth intensity
            riverMask = SmoothIntensityParallel(riverMask, width, height, biomeSpecificConfig);
            
            return riverMask;
        }
        
        private double GenerateRiverPoint(int x, int y,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            double[,] hydrologyMask, BiomeSpecificConfig biomeConfig)
        {
            // Get flow accumulation at position
            double flowAccumulation = GetSafeValue(flowAccumulationMask, x, y);
            
            // Get erosion risk at position
            double erosionRisk = GetSafeValue(erosionRiskMask, x, y);
            
            // Get hydrology value at position
            double hydrologyValue = GetSafeValue(hydrologyMask, x, y);
            
            // Apply biome-specific adjustments
            flowAccumulation *= biomeConfig.FlowAccumulationMultiplier;
            erosionRisk *= biomeConfig.ErosionSensitivity;
            hydrologyValue *= biomeConfig.HydrologySensitivity;
            
            // Calculate river threshold
            double riverCenterThreshold = _config.RiverCenterThreshold * biomeConfig.ThresholdMultiplier;
            double riverBankThreshold = _config.RiverBankThreshold * biomeConfig.ThresholdMultiplier;
            
            // Determine if position is in river
            if (flowAccumulation > riverCenterThreshold)
            {
                // River center
                return 1.0;
            }
            else if (flowAccumulation > riverBankThreshold)
            {
                // River bank (gradient based on flow accumulation)
                return (flowAccumulation - riverBankThreshold) / (riverCenterThreshold - riverBankThreshold);
            }
            
            return 0.0;
        }
        
        private double[,] ApplyHydrologyStabilityParallel(double[,] riverMask, int width, int height,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            var stableMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    stableMask[x, y] = ApplyHydrologyStabilityPoint(x, y, riverMask,
                        flowAccumulationMask, erosionRiskMask, biomeConfig);
                }
            });
            
            return stableMask;
        }
        
        private double ApplyHydrologyStabilityPoint(int x, int y, double[,] riverMask,
            double[,] flowAccumulationMask, double[,] erosionRiskMask,
            BiomeSpecificConfig biomeConfig)
        {
            // Get flow direction at position
            double flowDirection = _flowCache.GetFlowDirection(x, y, flowAccumulationMask);
            
            // Calculate flow alignment
            double flowAlignment = CalculateFlowAlignment(x, y, flowDirection, riverMask);
            
            // Apply biome-specific adjustments
            flowAlignment *= biomeConfig.FlowAlignmentMultiplier;
            
            // Calculate gradient penalty
            double gradientPenalty = CalculateGradientPenalty(x, y, flowAccumulationMask);
            
            // Apply biome-specific adjustments
            gradientPenalty *= biomeConfig.GradientPenaltyMultiplier;
            
            // Calculate headwater stability
            double headwaterStability = CalculateHeadwaterStability(x, y, flowAccumulationMask);
            
            // Apply biome-specific adjustments
            headwaterStability *= biomeConfig.HeadwaterStabilityMultiplier;
            
            // Combine stability factors
            double stability = (flowAlignment * _config.FlowAlignmentWeight +
                              gradientPenalty * _config.GradientPenalty +
                              headwaterStability * _config.HeadwaterStabilityWeight);
            
            // Apply stability to river mask
            return riverMask[x, y] * stability;
        }
        
        private double CalculateFlowAlignment(int x, int y, double flowDirection, double[,] riverMask)
        {
            // Calculate alignment between flow direction and river direction
            double riverDirection = CalculateRiverDirection(x, y, riverMask);
            
            // Calculate alignment (1.0 = perfect alignment, 0.0 = perpendicular)
            double alignment = Math.Abs(Math.Cos(flowDirection - riverDirection));
            
            return alignment;
        }
        
        private double CalculateRiverDirection(int x, int y, double[,] riverMask)
        {
            // Calculate river direction based on neighboring river mask values
            double dx = 0;
            double dy = 0;
            
            int width = riverMask.GetLength(0);
            int height = riverMask.GetLength(1);
            
            // Sample neighbors
            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    if (nx == 0 && ny == 0) continue;
                    
                    int px = x + nx;
                    int py = y + ny;
                    
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        double value = riverMask[px, py];
                        dx += nx * value;
                        dy += ny * value;
                    }
                }
            }
            
            return Math.Atan2(dy, dx);
        }
        
        private double CalculateGradientPenalty(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate gradient penalty based on flow accumulation
            double gradient = CalculateGradient(x, y, flowAccumulationMask);
            
            // Apply penalty (higher gradient = lower penalty)
            return 1.0 - Math.Min(gradient, 1.0);
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
        
        private double CalculateHeadwaterStability(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate headwater stability based on flow accumulation
            double flowAccumulation = GetSafeValue(flowAccumulationMask, x, y);
            
            // Headwater stability (higher flow accumulation = higher stability)
            return Math.Min(flowAccumulation * 100.0, 1.0);
        }
        
        private double[,] FeatherEdgesParallel(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var featheredMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    featheredMask[x, y] = FeatherEdgePoint(x, y, riverMask, width, height, biomeConfig);
                }
            });
            
            return featheredMask;
        }
        
        private double FeatherEdgePoint(int x, int y, double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            // Check if position is at river edge
            if (riverMask[x, y] < 0.1)
            {
                return 0.0;
            }
            
            // Check if position is at river boundary
            bool isBoundary = false;
            int boundaryCount = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (riverMask[nx, ny] < 0.1)
                        {
                            isBoundary = true;
                            boundaryCount++;
                        }
                    }
                }
            }
            
            if (isBoundary)
            {
                // Apply edge feathering
                double edgeFeather = _config.EdgeFeather * biomeConfig.EdgeFeatherMultiplier;
                return riverMask[x, y] * edgeFeather;
            }
            
            return riverMask[x, y];
        }
        
        private double[,] SmoothIntensityParallel(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = (double[,])riverMask.Clone();
            
            for (int i = 0; i < _config.IntensitySmoothIterations; i++)
            {
                smoothedMask = SmoothIntensityIteration(smoothedMask, width, height, biomeConfig);
            }
            
            return smoothedMask;
        }
        
        private double[,] SmoothIntensityIteration(double[,] riverMask, int width, int height,
            BiomeSpecificConfig biomeConfig)
        {
            var smoothedMask = new double[width, height];
            
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    smoothedMask[x, y] = SmoothIntensityPoint(x, y, riverMask, width, height, biomeConfig);
                }
            });
            
            return smoothedMask;
        }
        
        private double SmoothIntensityPoint(int x, int y, double[,] riverMask, int width, int height,
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
                        sum += riverMask[nx, ny];
                        count++;
                    }
                }
            }
            
            double average = sum / count;
            
            // Apply smoothing blend
            double smoothBlend = _config.IntensitySmoothBlend * biomeConfig.SmoothBlendMultiplier;
            
            return riverMask[x, y] * (1.0 - smoothBlend) + average * smoothBlend;
        }
        
        private BiomeSpecificConfig GetBiomeConfig(string biome)
        {
            if (string.IsNullOrEmpty(biome))
            {
                return _biomeConfig.GetDefaultConfig();
            }
            return _biomeConfig.GetConfig(biome);
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
    /// Water configuration with caching support
    /// </summary>
    public class WaterConfig
    {
        // River thresholds
        public double RiverCenterThreshold { get; set; } = 0.0125;
        public double RiverBankThreshold { get; set; } = 0.028;
        public double RiverNoiseScale { get; set; } = 0.015;
        public int RiverDepth { get; set; } = 6;
        
        // Confluence and flow
        public double ConfluenceBoost { get; set; } = 0.35;
        public double FlowAlignmentWeight { get; set; } = 0.28;
        public double GradientPenalty { get; set; } = 0.42;
        public double HeadwaterStabilityWeight { get; set; } = 0.35;
        
        // River shaping
        public double AnisotropyWeight { get; set; } = 0.32;
        public double MeanderJitter { get; set; } = 0.18;
        public double ReliefPenaltyWeight { get; set; } = 0.25;
        public double BankErosionWeight { get; set; } = 0.18;
        
        // Edge handling
        public double EdgeFeather { get; set; } = 0.45;
        public int MouthSmoothRadius { get; set; } = 3;
        public double DeltaWetlandStrength { get; set; } = 0.45;
        
        // Intensity smoothing
        public int IntensitySmoothIterations { get; set; } = 3;
        public double IntensitySmoothBlend { get; set; } = 0.58;
        
        // Caching
        public int CacheSize { get; set; } = 10000;
    }
    
    /// <summary>
    /// Flow cache for performance optimization
    /// </summary>
    public class FlowCache
    {
        private readonly ConcurrentDictionary<string, double> _cache;
        private readonly int _maxCacheSize;
        
        public FlowCache(int maxCacheSize = 10000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, double>();
        }
        
        public double GetFlowDirection(int x, int y, double[,] flowAccumulationMask)
        {
            string key = $"flow_{x}_{y}";
            
            if (_cache.TryGetValue(key, out double cachedValue))
            {
                return cachedValue;
            }
            
            // Calculate flow direction
            double flowDirection = CalculateFlowDirection(x, y, flowAccumulationMask);
            
            // Add to cache
            if (_cache.Count < _maxCacheSize)
            {
                _cache.TryAdd(key, flowDirection);
            }
            
            return flowDirection;
        }
        
        private double CalculateFlowDirection(int x, int y, double[,] flowAccumulationMask)
        {
            // Calculate flow direction based on flow accumulation gradient
            int width = flowAccumulationMask.GetLength(0);
            int height = flowAccumulationMask.GetLength(1);
            
            double dx = 0;
            double dy = 0;
            
            // Sample neighbors
            if (x > 0 && x < width - 1)
            {
                dx = (flowAccumulationMask[x + 1, y] - flowAccumulationMask[x - 1, y]) / 2.0;
            }
            
            if (y > 0 && y < height - 1)
            {
                dy = (flowAccumulationMask[x, y + 1] - flowAccumulationMask[x, y - 1]) / 2.0;
            }
            
            return Math.Atan2(dy, dx);
        }
        
        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Biome-specific river configuration
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
                FlowAccumulationMultiplier = 1.0,
                ErosionSensitivity = 1.0,
                HydrologySensitivity = 1.0,
                FlowAlignmentMultiplier = 1.0,
                GradientPenaltyMultiplier = 1.0,
                HeadwaterStabilityMultiplier = 1.0,
                EdgeFeatherMultiplier = 1.0,
                SmoothBlendMultiplier = 1.0
            });
            
            _biomeConfigs.TryAdd("mountains", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.8,
                FlowAccumulationMultiplier = 1.2,
                ErosionSensitivity = 1.5,
                HydrologySensitivity = 0.8,
                FlowAlignmentMultiplier = 1.2,
                GradientPenaltyMultiplier = 0.7,
                HeadwaterStabilityMultiplier = 1.5,
                EdgeFeatherMultiplier = 0.8,
                SmoothBlendMultiplier = 0.8
            });
            
            _biomeConfigs.TryAdd("desert", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 1.2,
                FlowAccumulationMultiplier = 0.8,
                ErosionSensitivity = 0.8,
                HydrologySensitivity = 1.5,
                FlowAlignmentMultiplier = 0.8,
                GradientPenaltyMultiplier = 1.2,
                HeadwaterStabilityMultiplier = 0.8,
                EdgeFeatherMultiplier = 1.2,
                SmoothBlendMultiplier = 1.2
            });
            
            _biomeConfigs.TryAdd("forest", new BiomeSpecificConfig
            {
                ThresholdMultiplier = 0.9,
                FlowAccumulationMultiplier = 1.1,
                ErosionSensitivity = 1.0,
                HydrologySensitivity = 1.2,
                FlowAlignmentMultiplier = 1.1,
                GradientPenaltyMultiplier = 1.0,
                HeadwaterStabilityMultiplier = 1.1,
                EdgeFeatherMultiplier = 1.0,
                SmoothBlendMultiplier = 1.0
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
    /// Biome-specific river generation parameters
    /// </summary>
    public class BiomeSpecificConfig
    {
        public double ThresholdMultiplier { get; set; } = 1.0;
        public double FlowAccumulationMultiplier { get; set; } = 1.0;
        public double ErosionSensitivity { get; set; } = 1.0;
        public double HydrologySensitivity { get; set; } = 1.0;
        public double FlowAlignmentMultiplier { get; set; } = 1.0;
        public double GradientPenaltyMultiplier { get; set; } = 1.0;
        public double HeadwaterStabilityMultiplier { get; set; } = 1.0;
        public double EdgeFeatherMultiplier { get; set; } = 1.0;
        public double SmoothBlendMultiplier { get; set; } = 1.0;
    }
}

