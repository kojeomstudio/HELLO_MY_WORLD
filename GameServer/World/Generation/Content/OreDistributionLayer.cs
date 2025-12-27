using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for ore distribution with configurable rarity
    /// </summary>
    public class OreDistributionLayer : IContentLayer
    {
        private readonly OreDistributionConfig _config;
        private readonly Dictionary<string, OreType> _oreTypes;
        private readonly FastNoise _oreNoise;
        
        public string LayerId => "OreDistribution";
        public int Priority => 20; // After terrain and caves, before structures
        public bool IsEnabled { get; set; } = true;
        
        public OreDistributionLayer(OreDistributionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _oreTypes = new Dictionary<string, OreType>();
            _oreNoise = new FastNoise();
            
            // Initialize ore types from configuration
            foreach (var oreConfig in _config.OreTypes)
            {
                _oreTypes[oreConfig.Name] = oreConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(OreDistributionConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Initialize ore distribution data
            context.OreData = new OreDistribution[chunkSize, chunkSize];
            
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeDefinition = _config.Biomes.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Initialize ore distribution for this position
                    var oreDistribution = new OreDistribution();
                    oreDistribution.Depth = CalculateDepth(context, localX, localZ);
                    
                    // Generate ore veins for each ore type
                    foreach (var oreType in _oreTypes.Values)
                    {
                        if (ShouldGenerateOre(oreType, worldX, worldZ, oreDistribution.Depth, biomeDefinition))
                        {
                            var veinData = GenerateOreVein(oreType, worldX, worldZ, context);
                            oreDistribution.OreVeins[oreType.Name] = veinData.Richness;
                        }
                    }
                    
                    // Apply biome-specific ore modifiers
                    if (biomeDefinition != null)
                    {
                        ApplyBiomeOreModifiers(oreDistribution, biomeDefinition);
                    }
                    
                    context.OreData[localX, localZ] = oreDistribution;
                }
            }
            
            Console.WriteLine($"[OreDistributionLayer] Generated ore distribution for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private int CalculateDepth(TerrainGenerationContext context, int localX, int localZ)
        {
            var height = context.GetHeight(localX, localZ);
            var seaLevel = context.Config.SeaLevel;
            
            // Calculate depth below surface
            var depth = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    depth++;
                }
                else
                {
                    break;
                }
            }
            
            return depth;
        }
        
        private bool ShouldGenerateOre(OreType oreType, int worldX, int worldZ, int depth, BiomeOreConfig biomeConfig)
        {
            // Check depth requirements
            if (depth < oreType.MinDepth || depth > oreType.MaxDepth)
                return false;
            
            // Check biome restrictions
            if (oreType.BiomeRestrictions.Count > 0)
            {
                // This would need biome type, but we're working with world coordinates here
                // For now, we'll assume all biomes are allowed unless specifically restricted
            }
            
            // Generate noise value for this position
            var noiseValue = _oreNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Apply rarity modifier
            var rarityModifier = oreType.Rarity;
            if (biomeConfig != null && biomeConfig.OreModifiers.ContainsKey(oreType.Name))
            {
                rarityModifier *= biomeConfig.OreModifiers[oreType.Name];
            }
            
            // Check if ore should generate based on noise and rarity
            return normalizedNoise < (1.0f / rarityModifier);
        }
        
        private OreVeinData GenerateOreVein(OreType oreType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var veinData = new OreVeinData();
            
            // Calculate vein size based on ore type configuration
            var baseSize = oreType.VeinSize;
            var sizeVariation = oreType.VeinSizeVariation;
            var actualSize = baseSize + (int)(context.Random.NextDouble() * sizeVariation * 2 - sizeVariation);
            
            // Calculate richness based on depth and configuration
            var depth = CalculateDepth(context, worldX % context.ChunkSize, worldZ % context.ChunkSize);
            var depthFactor = Math.Min(1.0f, (float)depth / oreType.MaxDepth);
            var richness = oreType.BaseRichness * depthFactor * (1.0f + (float)(context.Random.NextDouble() * oreType.RichnessVariation * 2 - oreType.RichnessVariation));
            
            veinData.Richness = Math.Max(0.1f, richness);
            veinData.Size = Math.Max(1, actualSize);
            veinData.Depth = depth;
            
            return veinData;
        }
        
        private void ApplyBiomeOreModifiers(OreDistribution oreDistribution, BiomeOreConfig biomeConfig)
        {
            foreach (var modifier in biomeConfig.OreModifiers)
            {
                if (oreDistribution.OreVeins.ContainsKey(modifier.Key))
                {
                    oreDistribution.OreVeins[modifier.Key] *= modifier.Value;
                }
            }
            
            // Apply overall biome richness modifier
            oreDistribution.Richness *= biomeConfig.OverallRichnessModifier;
        }
    }
    
    /// <summary>
    /// Data structure for ore vein information
    /// </summary>
    public class OreVeinData
    {
        public float Richness { get; set; }
        public int Size { get; set; }
        public int Depth { get; set; }
        public string Shape { get; set; } = "spherical";
    }
    
    /// <summary>
    /// Configuration for ore distribution
    /// </summary>
    public class OreDistributionConfig
    {
        public List<OreType> OreTypes { get; set; } = new();
        public List<BiomeOreConfig> Biomes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public bool EnableClusterGeneration { get; set; } = true;
        public float ClusterChance { get; set; } = 0.1f;
        public int MaxVeinsPerChunk { get; set; } = 50;
    }
    
    /// <summary>
    /// Configuration for a specific ore type
    /// </summary>
    public class OreType
    {
        public string Name { get; set; }
        public int BlockId { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinDepth { get; set; } = 0;
        public int MaxDepth { get; set; } = 256;
        public int VeinSize { get; set; } = 8;
        public int VeinSizeVariation { get; set; } = 4;
        public float BaseRichness { get; set; } = 1.0f;
        public float RichnessVariation { get; set; } = 0.2f;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public bool GenerateInCaves { get; set; } = true;
        public bool GenerateInMountains { get; set; } = true;
        public string VeinShape { get; set; } = "spherical"; // spherical, cylindrical, irregular
    }
    
    /// <summary>
    /// Configuration for ore distribution in a specific biome
    /// </summary>
    public class BiomeOreConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> OreModifiers { get; set; } = new();
        public float OverallRichnessModifier { get; set; } = 1.0f;
        public List<string> ExcludedOres { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default ore distribution configurations
    /// </summary>
    public static class OreDistributionConfigFactory
    {
        /// <summary>
        /// Creates a default ore distribution configuration
        /// </summary>
        public static OreDistributionConfig CreateDefault()
        {
            var config = new OreDistributionConfig();
            
            // Add standard ore types
            config.OreTypes.AddRange(GetStandardOreTypes());
            
            // Add biome configurations
            config.Biomes.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard ore types
        /// </summary>
        private static List<OreType> GetStandardOreTypes()
        {
            return new List<OreType>
            {
                new OreType
                {
                    Name = "coal",
                    BlockId = 16,
                    Rarity = 1.0f,
                    MinDepth = 0,
                    MaxDepth = 256,
                    VeinSize = 17,
                    VeinSizeVariation = 8,
                    BaseRichness = 1.0f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "iron",
                    BlockId = 15,
                    Rarity = 2.0f,
                    MinDepth = 0,
                    MaxDepth = 64,
                    VeinSize = 9,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.2f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "gold",
                    BlockId = 14,
                    Rarity = 5.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 8,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.5f,
                    RichnessVariation = 0.25f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "diamond",
                    BlockId = 56,
                    Rarity = 20.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 7,
                    VeinSizeVariation = 2,
                    BaseRichness = 2.0f,
                    RichnessVariation = 0.1f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "copper",
                    BlockId = 21,
                    Rarity = 3.0f,
                    MinDepth = 0,
                    MaxDepth = 96,
                    VeinSize = 10,
                    VeinSizeVariation = 5,
                    BaseRichness = 1.1f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "lapis_lazuli",
                    BlockId = 22,
                    Rarity = 8.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 6,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.8f,
                    RichnessVariation = 0.4f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "redstone",
                    BlockId = 73,
                    Rarity = 4.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 8,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.6f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "emerald",
                    BlockId = 129,
                    Rarity = 25.0f,
                    MinDepth = 4,
                    MaxDepth = 32,
                    VeinSize = 3,
                    VeinSizeVariation = 1,
                    BaseRichness = 3.0f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical",
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Mountains }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome ore configurations
        /// </summary>
        private static List<BiomeOreConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeOreConfig>
            {
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallRichnessModifier = 1.5f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.8f,
                        ["diamond"] = 2.0f,
                        ["emerald"] = 5.0f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallRichnessModifier = 0.8f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["gold"] = 1.5f,
                        ["copper"] = 1.3f,
                        ["iron"] = 0.7f
                    },
                    ExcludedOres = new List<string> { "emerald" }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Swamp,
                    OverallRichnessModifier = 0.6f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 0.8f,
                        ["iron"] = 0.5f,
                        ["lapis_lazuli"] = 1.2f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Ocean,
                    OverallRichnessModifier = 0.3f,
                    ExcludedOres = new List<string> { "emerald", "diamond" }
                }
            };
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for ore distribution with configurable rarity
    /// </summary>
    public class OreDistributionLayer : IContentLayer
    {
        private readonly OreDistributionConfig _config;
        private readonly Dictionary<string, OreType> _oreTypes;
        private readonly FastNoise _oreNoise;
        
        public string LayerId => "OreDistribution";
        public int Priority => 20; // After terrain and caves, before structures
        public bool IsEnabled { get; set; } = true;
        
        public OreDistributionLayer(OreDistributionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _oreTypes = new Dictionary<string, OreType>();
            _oreNoise = new FastNoise();
            
            // Initialize ore types from configuration
            foreach (var oreConfig in _config.OreTypes)
            {
                _oreTypes[oreConfig.Name] = oreConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(OreDistributionConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Initialize ore distribution data
            context.OreData = new OreDistribution[chunkSize, chunkSize];
            
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeDefinition = _config.Biomes.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Initialize ore distribution for this position
                    var oreDistribution = new OreDistribution();
                    oreDistribution.Depth = CalculateDepth(context, localX, localZ);
                    
                    // Generate ore veins for each ore type
                    foreach (var oreType in _oreTypes.Values)
                    {
                        if (ShouldGenerateOre(oreType, worldX, worldZ, oreDistribution.Depth, biomeDefinition))
                        {
                            var veinData = GenerateOreVein(oreType, worldX, worldZ, context);
                            oreDistribution.OreVeins[oreType.Name] = veinData.Richness;
                        }
                    }
                    
                    // Apply biome-specific ore modifiers
                    if (biomeDefinition != null)
                    {
                        ApplyBiomeOreModifiers(oreDistribution, biomeDefinition);
                    }
                    
                    context.OreData[localX, localZ] = oreDistribution;
                }
            }
            
            Console.WriteLine($"[OreDistributionLayer] Generated ore distribution for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private int CalculateDepth(TerrainGenerationContext context, int localX, int localZ)
        {
            var height = context.GetHeight(localX, localZ);
            var seaLevel = context.Config.SeaLevel;
            
            // Calculate depth below surface
            var depth = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    depth++;
                }
                else
                {
                    break;
                }
            }
            
            return depth;
        }
        
        private bool ShouldGenerateOre(OreType oreType, int worldX, int worldZ, int depth, BiomeOreConfig biomeConfig)
        {
            // Check depth requirements
            if (depth < oreType.MinDepth || depth > oreType.MaxDepth)
                return false;
            
            // Check biome restrictions
            if (oreType.BiomeRestrictions.Count > 0)
            {
                // This would need biome type, but we're working with world coordinates here
                // For now, we'll assume all biomes are allowed unless specifically restricted
            }
            
            // Generate noise value for this position
            var noiseValue = _oreNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Apply rarity modifier
            var rarityModifier = oreType.Rarity;
            if (biomeConfig != null && biomeConfig.OreModifiers.ContainsKey(oreType.Name))
            {
                rarityModifier *= biomeConfig.OreModifiers[oreType.Name];
            }
            
            // Check if ore should generate based on noise and rarity
            return normalizedNoise < (1.0f / rarityModifier);
        }
        
        private OreVeinData GenerateOreVein(OreType oreType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var veinData = new OreVeinData();
            
            // Calculate vein size based on ore type configuration
            var baseSize = oreType.VeinSize;
            var sizeVariation = oreType.VeinSizeVariation;
            var actualSize = baseSize + (int)(context.Random.NextDouble() * sizeVariation * 2 - sizeVariation);
            
            // Calculate richness based on depth and configuration
            var depth = CalculateDepth(context, worldX % context.ChunkSize, worldZ % context.ChunkSize);
            var depthFactor = Math.Min(1.0f, (float)depth / oreType.MaxDepth);
            var richness = oreType.BaseRichness * depthFactor * (1.0f + (float)(context.Random.NextDouble() * oreType.RichnessVariation * 2 - oreType.RichnessVariation));
            
            veinData.Richness = Math.Max(0.1f, richness);
            veinData.Size = Math.Max(1, actualSize);
            veinData.Depth = depth;
            
            return veinData;
        }
        
        private void ApplyBiomeOreModifiers(OreDistribution oreDistribution, BiomeOreConfig biomeConfig)
        {
            foreach (var modifier in biomeConfig.OreModifiers)
            {
                if (oreDistribution.OreVeins.ContainsKey(modifier.Key))
                {
                    oreDistribution.OreVeins[modifier.Key] *= modifier.Value;
                }
            }
            
            // Apply overall biome richness modifier
            oreDistribution.Richness *= biomeConfig.OverallRichnessModifier;
        }
    }
    
    /// <summary>
    /// Data structure for ore vein information
    /// </summary>
    public class OreVeinData
    {
        public float Richness { get; set; }
        public int Size { get; set; }
        public int Depth { get; set; }
        public string Shape { get; set; } = "spherical";
    }
    
    /// <summary>
    /// Configuration for ore distribution
    /// </summary>
    public class OreDistributionConfig
    {
        public List<OreType> OreTypes { get; set; } = new();
        public List<BiomeOreConfig> Biomes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public bool EnableClusterGeneration { get; set; } = true;
        public float ClusterChance { get; set; } = 0.1f;
        public int MaxVeinsPerChunk { get; set; } = 50;
    }
    
    /// <summary>
    /// Configuration for a specific ore type
    /// </summary>
    public class OreType
    {
        public string Name { get; set; }
        public int BlockId { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinDepth { get; set; } = 0;
        public int MaxDepth { get; set; } = 256;
        public int VeinSize { get; set; } = 8;
        public int VeinSizeVariation { get; set; } = 4;
        public float BaseRichness { get; set; } = 1.0f;
        public float RichnessVariation { get; set; } = 0.2f;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public bool GenerateInCaves { get; set; } = true;
        public bool GenerateInMountains { get; set; } = true;
        public string VeinShape { get; set; } = "spherical"; // spherical, cylindrical, irregular
    }
    
    /// <summary>
    /// Configuration for ore distribution in a specific biome
    /// </summary>
    public class BiomeOreConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> OreModifiers { get; set; } = new();
        public float OverallRichnessModifier { get; set; } = 1.0f;
        public List<string> ExcludedOres { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default ore distribution configurations
    /// </summary>
    public static class OreDistributionConfigFactory
    {
        /// <summary>
        /// Creates a default ore distribution configuration
        /// </summary>
        public static OreDistributionConfig CreateDefault()
        {
            var config = new OreDistributionConfig();
            
            // Add standard ore types
            config.OreTypes.AddRange(GetStandardOreTypes());
            
            // Add biome configurations
            config.Biomes.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard ore types
        /// </summary>
        private static List<OreType> GetStandardOreTypes()
        {
            return new List<OreType>
            {
                new OreType
                {
                    Name = "coal",
                    BlockId = 16,
                    Rarity = 1.0f,
                    MinDepth = 0,
                    MaxDepth = 256,
                    VeinSize = 17,
                    VeinSizeVariation = 8,
                    BaseRichness = 1.0f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "iron",
                    BlockId = 15,
                    Rarity = 2.0f,
                    MinDepth = 0,
                    MaxDepth = 64,
                    VeinSize = 9,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.2f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "gold",
                    BlockId = 14,
                    Rarity = 5.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 8,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.5f,
                    RichnessVariation = 0.25f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "diamond",
                    BlockId = 56,
                    Rarity = 20.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 7,
                    VeinSizeVariation = 2,
                    BaseRichness = 2.0f,
                    RichnessVariation = 0.1f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "copper",
                    BlockId = 21,
                    Rarity = 3.0f,
                    MinDepth = 0,
                    MaxDepth = 96,
                    VeinSize = 10,
                    VeinSizeVariation = 5,
                    BaseRichness = 1.1f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "lapis_lazuli",
                    BlockId = 22,
                    Rarity = 8.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 6,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.8f,
                    RichnessVariation = 0.4f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "redstone",
                    BlockId = 73,
                    Rarity = 4.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 8,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.6f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "emerald",
                    BlockId = 129,
                    Rarity = 25.0f,
                    MinDepth = 4,
                    MaxDepth = 32,
                    VeinSize = 3,
                    VeinSizeVariation = 1,
                    BaseRichness = 3.0f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical",
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Mountains }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome ore configurations
        /// </summary>
        private static List<BiomeOreConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeOreConfig>
            {
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallRichnessModifier = 1.5f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.8f,
                        ["diamond"] = 2.0f,
                        ["emerald"] = 5.0f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallRichnessModifier = 0.8f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["gold"] = 1.5f,
                        ["copper"] = 1.3f,
                        ["iron"] = 0.7f
                    },
                    ExcludedOres = new List<string> { "emerald" }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Swamp,
                    OverallRichnessModifier = 0.6f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 0.8f,
                        ["iron"] = 0.5f,
                        ["lapis_lazuli"] = 1.2f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Ocean,
                    OverallRichnessModifier = 0.3f,
                    ExcludedOres = new List<string> { "emerald", "diamond" }
                }
            };
        }
    }
}
}
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for ore distribution with configurable rarity
    /// </summary>
    public class OreDistributionLayer : IContentLayer
    {
        private readonly OreDistributionConfig _config;
        private readonly Dictionary<string, OreType> _oreTypes;
        private readonly FastNoise _oreNoise;
        
        public string LayerId => "OreDistribution";
        public int Priority => 20; // After terrain and caves, before structures
        public bool IsEnabled { get; set; } = true;
        
        public OreDistributionLayer(OreDistributionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _oreTypes = new Dictionary<string, OreType>();
            _oreNoise = new FastNoise();
            
            // Initialize ore types from configuration
            foreach (var oreConfig in _config.OreTypes)
            {
                _oreTypes[oreConfig.Name] = oreConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(OreDistributionConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Initialize ore distribution data
            context.OreData = new OreDistribution[chunkSize, chunkSize];
            
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeDefinition = _config.Biomes.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Initialize ore distribution for this position
                    var oreDistribution = new OreDistribution();
                    oreDistribution.Depth = CalculateDepth(context, localX, localZ);
                    
                    // Generate ore veins for each ore type
                    foreach (var oreType in _oreTypes.Values)
                    {
                        if (ShouldGenerateOre(oreType, worldX, worldZ, oreDistribution.Depth, biomeDefinition))
                        {
                            var veinData = GenerateOreVein(oreType, worldX, worldZ, context);
                            oreDistribution.OreVeins[oreType.Name] = veinData.Richness;
                        }
                    }
                    
                    // Apply biome-specific ore modifiers
                    if (biomeDefinition != null)
                    {
                        ApplyBiomeOreModifiers(oreDistribution, biomeDefinition);
                    }
                    
                    context.OreData[localX, localZ] = oreDistribution;
                }
            }
            
            Console.WriteLine($"[OreDistributionLayer] Generated ore distribution for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private int CalculateDepth(TerrainGenerationContext context, int localX, int localZ)
        {
            var height = context.GetHeight(localX, localZ);
            var seaLevel = context.Config.SeaLevel;
            
            // Calculate depth below surface
            var depth = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    depth++;
                }
                else
                {
                    break;
                }
            }
            
            return depth;
        }
        
        private bool ShouldGenerateOre(OreType oreType, int worldX, int worldZ, int depth, BiomeOreConfig biomeConfig)
        {
            // Check depth requirements
            if (depth < oreType.MinDepth || depth > oreType.MaxDepth)
                return false;
            
            // Check biome restrictions
            if (oreType.BiomeRestrictions.Count > 0)
            {
                // This would need biome type, but we're working with world coordinates here
                // For now, we'll assume all biomes are allowed unless specifically restricted
            }
            
            // Generate noise value for this position
            var noiseValue = _oreNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Apply rarity modifier
            var rarityModifier = oreType.Rarity;
            if (biomeConfig != null && biomeConfig.OreModifiers.ContainsKey(oreType.Name))
            {
                rarityModifier *= biomeConfig.OreModifiers[oreType.Name];
            }
            
            // Check if ore should generate based on noise and rarity
            return normalizedNoise < (1.0f / rarityModifier);
        }
        
        private OreVeinData GenerateOreVein(OreType oreType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var veinData = new OreVeinData();
            
            // Calculate vein size based on ore type configuration
            var baseSize = oreType.VeinSize;
            var sizeVariation = oreType.VeinSizeVariation;
            var actualSize = baseSize + (int)(context.Random.NextDouble() * sizeVariation * 2 - sizeVariation);
            
            // Calculate richness based on depth and configuration
            var depth = CalculateDepth(context, worldX % context.ChunkSize, worldZ % context.ChunkSize);
            var depthFactor = Math.Min(1.0f, (float)depth / oreType.MaxDepth);
            var richness = oreType.BaseRichness * depthFactor * (1.0f + (float)(context.Random.NextDouble() * oreType.RichnessVariation * 2 - oreType.RichnessVariation));
            
            veinData.Richness = Math.Max(0.1f, richness);
            veinData.Size = Math.Max(1, actualSize);
            veinData.Depth = depth;
            
            return veinData;
        }
        
        private void ApplyBiomeOreModifiers(OreDistribution oreDistribution, BiomeOreConfig biomeConfig)
        {
            foreach (var modifier in biomeConfig.OreModifiers)
            {
                if (oreDistribution.OreVeins.ContainsKey(modifier.Key))
                {
                    oreDistribution.OreVeins[modifier.Key] *= modifier.Value;
                }
            }
            
            // Apply overall biome richness modifier
            oreDistribution.Richness *= biomeConfig.OverallRichnessModifier;
        }
    }
    
    /// <summary>
    /// Data structure for ore vein information
    /// </summary>
    public class OreVeinData
    {
        public float Richness { get; set; }
        public int Size { get; set; }
        public int Depth { get; set; }
        public string Shape { get; set; } = "spherical";
    }
    
    /// <summary>
    /// Configuration for ore distribution
    /// </summary>
    public class OreDistributionConfig
    {
        public List<OreType> OreTypes { get; set; } = new();
        public List<BiomeOreConfig> Biomes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public bool EnableClusterGeneration { get; set; } = true;
        public float ClusterChance { get; set; } = 0.1f;
        public int MaxVeinsPerChunk { get; set; } = 50;
    }
    
    /// <summary>
    /// Configuration for a specific ore type
    /// </summary>
    public class OreType
    {
        public string Name { get; set; }
        public int BlockId { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinDepth { get; set; } = 0;
        public int MaxDepth { get; set; } = 256;
        public int VeinSize { get; set; } = 8;
        public int VeinSizeVariation { get; set; } = 4;
        public float BaseRichness { get; set; } = 1.0f;
        public float RichnessVariation { get; set; } = 0.2f;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public bool GenerateInCaves { get; set; } = true;
        public bool GenerateInMountains { get; set; } = true;
        public string VeinShape { get; set; } = "spherical"; // spherical, cylindrical, irregular
    }
    
    /// <summary>
    /// Configuration for ore distribution in a specific biome
    /// </summary>
    public class BiomeOreConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> OreModifiers { get; set; } = new();
        public float OverallRichnessModifier { get; set; } = 1.0f;
        public List<string> ExcludedOres { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default ore distribution configurations
    /// </summary>
    public static class OreDistributionConfigFactory
    {
        /// <summary>
        /// Creates a default ore distribution configuration
        /// </summary>
        public static OreDistributionConfig CreateDefault()
        {
            var config = new OreDistributionConfig();
            
            // Add standard ore types
            config.OreTypes.AddRange(GetStandardOreTypes());
            
            // Add biome configurations
            config.Biomes.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard ore types
        /// </summary>
        private static List<OreType> GetStandardOreTypes()
        {
            return new List<OreType>
            {
                new OreType
                {
                    Name = "coal",
                    BlockId = 16,
                    Rarity = 1.0f,
                    MinDepth = 0,
                    MaxDepth = 256,
                    VeinSize = 17,
                    VeinSizeVariation = 8,
                    BaseRichness = 1.0f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "iron",
                    BlockId = 15,
                    Rarity = 2.0f,
                    MinDepth = 0,
                    MaxDepth = 64,
                    VeinSize = 9,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.2f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "gold",
                    BlockId = 14,
                    Rarity = 5.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 8,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.5f,
                    RichnessVariation = 0.25f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "diamond",
                    BlockId = 56,
                    Rarity = 20.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 7,
                    VeinSizeVariation = 2,
                    BaseRichness = 2.0f,
                    RichnessVariation = 0.1f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "copper",
                    BlockId = 21,
                    Rarity = 3.0f,
                    MinDepth = 0,
                    MaxDepth = 96,
                    VeinSize = 10,
                    VeinSizeVariation = 5,
                    BaseRichness = 1.1f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "lapis_lazuli",
                    BlockId = 22,
                    Rarity = 8.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 6,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.8f,
                    RichnessVariation = 0.4f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "redstone",
                    BlockId = 73,
                    Rarity = 4.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 8,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.6f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "emerald",
                    BlockId = 129,
                    Rarity = 25.0f,
                    MinDepth = 4,
                    MaxDepth = 32,
                    VeinSize = 3,
                    VeinSizeVariation = 1,
                    BaseRichness = 3.0f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical",
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Mountains }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome ore configurations
        /// </summary>
        private static List<BiomeOreConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeOreConfig>
            {
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallRichnessModifier = 1.5f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.8f,
                        ["diamond"] = 2.0f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallRichnessModifier = 0.8f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["gold"] = 1.5f,
                        ["copper"] = 1.3f,
                        ["iron"] = 0.7f
                    },
                    ExcludedOres = new List<string> { "emerald" }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Swamp,
                    OverallRichnessModifier = 0.6f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 0.8f,
                        ["iron"] = 0.5f,
                        ["lapis_lazuli"] = 1.2f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Ocean,
                    OverallRichnessModifier = 0.3f,
                    ExcludedOres = new List<string> { "emerald", "diamond" }
                }
            };
        }
    }
}
}
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for ore distribution with configurable rarity
    /// </summary>
    public class OreDistributionLayer : IContentLayer
    {
        private readonly OreDistributionConfig _config;
        private readonly FastNoise _oreNoise;
        private readonly Dictionary<string, OreType> _oreTypes;
        
        public string LayerId => "OreDistribution";
        public int Priority => 20; // After terrain and caves, before structures
        public bool IsEnabled { get; set; } = true;
        
        public OreDistributionLayer(OreDistributionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _oreNoise = new FastNoise();
            _oreTypes = new Dictionary<string, OreType>();
            
            // Initialize ore types from configuration
            foreach (var oreConfig in _config.OreTypes)
            {
                _oreTypes[oreConfig.Name] = oreConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(OreDistributionConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.Config.MaxHeight;
            
            // Initialize ore distribution data
            context.OreData = new OreDistribution[chunkSize, chunkSize];
            
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeDefinition = _config.Biomes.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Initialize ore distribution for this position
                    var oreDistribution = new OreDistribution();
                    oreDistribution.Depth = CalculateDepth(context, localX, localZ);
                    
                    // Generate ore veins for each ore type
                    foreach (var oreType in _oreTypes.Values)
                    {
                        if (ShouldGenerateOre(oreType, worldX, worldZ, oreDistribution.Depth, biomeDefinition))
                        {
                            var veinData = GenerateOreVein(oreType, worldX, worldZ, context);
                            oreDistribution.OreVeins[oreType.Name] = veinData.Richness;
                        }
                    }
                    
                    // Apply biome-specific ore modifiers
                    if (biomeDefinition != null)
                    {
                        ApplyBiomeOreModifiers(oreDistribution, biomeDefinition);
                    }
                    
                    context.OreData[localX, localZ] = oreDistribution;
                }
            }
            
            Console.WriteLine($"[OreDistributionLayer] Generated ore distribution for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private int CalculateDepth(TerrainGenerationContext context, int localX, int localZ)
        {
            var height = context.GetHeight(localX, localZ);
            var seaLevel = context.Config.SeaLevel;
            
            // Calculate depth below surface
            var depth = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    depth++;
                }
                else
                {
                    break;
                }
            }
            
            return depth;
        }
        
        private bool ShouldGenerateOre(OreType oreType, int worldX, int worldZ, int depth, BiomeOreConfig biomeConfig)
        {
            // Check depth requirements
            if (depth < oreType.MinDepth || depth > oreType.MaxDepth)
                return false;
            
            // Check biome restrictions
            if (oreType.BiomeRestrictions.Count > 0)
            {
                // This would need biome type, but we're working with world coordinates here
                // For now, we'll assume all biomes are allowed unless specifically restricted
            }
            
            // Generate noise value for this position
            var noiseValue = _oreNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Apply rarity modifier
            var rarityModifier = oreType.Rarity;
            if (biomeConfig != null && biomeConfig.OreModifiers.ContainsKey(oreType.Name))
            {
                rarityModifier *= biomeConfig.OreModifiers[oreType.Name];
            }
            
            // Check if ore should generate based on noise and rarity
            return normalizedNoise < (1.0f / rarityModifier);
        }
        
        private OreVeinData GenerateOreVein(OreType oreType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var veinData = new OreVeinData();
            
            // Calculate vein size based on ore type configuration
            var baseSize = oreType.VeinSize;
            var sizeVariation = oreType.VeinSizeVariation;
            var actualSize = baseSize + (int)(context.Random.NextDouble() * sizeVariation * 2 - sizeVariation);
            
            // Calculate richness based on depth and configuration
            var depth = CalculateDepth(context, worldX % context.ChunkSize, worldZ % context.ChunkSize);
            var depthFactor = Math.Min(1.0f, (float)depth / oreType.MaxDepth);
            var richness = oreType.BaseRichness * depthFactor * (1.0f + (float)(context.Random.NextDouble() * oreType.RichnessVariation * 2 - oreType.RichnessVariation));
            
            veinData.Richness = Math.Max(0.1f, richness);
            veinData.Size = Math.Max(1, actualSize);
            veinData.Depth = depth;
            
            return veinData;
        }
        
        private void ApplyBiomeOreModifiers(OreDistribution oreDistribution, BiomeOreConfig biomeConfig)
        {
            foreach (var modifier in biomeConfig.OreModifiers)
            {
                if (oreDistribution.OreVeins.ContainsKey(modifier.Key))
                {
                    oreDistribution.OreVeins[modifier.Key] *= modifier.Value;
                }
            }
            
            // Apply overall biome richness modifier
            oreDistribution.Richness *= biomeConfig.OverallRichnessModifier;
        }
    }
    
    /// <summary>
    /// Data structure for ore vein information
    /// </summary>
    public class OreVeinData
    {
        public float Richness { get; set; }
        public int Size { get; set; }
        public int Depth { get; set; }
        public string Shape { get; set; } = "spherical";
    }
    
    /// <summary>
    /// Configuration for ore distribution
    /// </summary>
    public class OreDistributionConfig
    {
        public List<OreType> OreTypes { get; set; } = new();
        public List<BiomeOreConfig> Biomes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public bool EnableClusterGeneration { get; set; } = true;
        public float ClusterChance { get; set; } = 0.1f;
        public int MaxVeinsPerChunk { get; set; } = 50;
    }
    
    /// <summary>
    /// Configuration for a specific ore type
    /// </summary>
    public class OreType
    {
        public string Name { get; set; }
        public int BlockId { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinDepth { get; set; } = 0;
        public int MaxDepth { get; set; } = 256;
        public int VeinSize { get; set; } = 8;
        public int VeinSizeVariation { get; set; } = 4;
        public float BaseRichness { get; set; } = 1.0f;
        public float RichnessVariation { get; set; } = 0.2f;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public bool GenerateInCaves { get; set; } = true;
        public bool GenerateInMountains { get; set; } = true;
        public string VeinShape { get; set; } = "spherical"; // spherical, cylindrical, irregular
    }
    
    /// <summary>
    /// Configuration for ore distribution in a specific biome
    /// </summary>
    public class BiomeOreConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> OreModifiers { get; set; } = new();
        public float OverallRichnessModifier { get; set; } = 1.0f;
        public List<string> ExcludedOres { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default ore distribution configurations
    /// </summary>
    public static class OreDistributionConfigFactory
    {
        /// <summary>
        /// Creates a default ore distribution configuration
        /// </summary>
        public static OreDistributionConfig CreateDefault()
        {
            var config = new OreDistributionConfig();
            
            // Add standard ore types
            config.OreTypes.AddRange(GetStandardOreTypes());
            
            // Add biome configurations
            config.Biomes.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard ore types
        /// </summary>
        private static List<OreType> GetStandardOreTypes()
        {
            return new List<OreType>
            {
                new OreType
                {
                    Name = "coal",
                    BlockId = 16,
                    Rarity = 1.0f,
                    MinDepth = 0,
                    MaxDepth = 256,
                    VeinSize = 17,
                    VeinSizeVariation = 8,
                    BaseRichness = 1.0f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "iron",
                    BlockId = 15,
                    Rarity = 2.0f,
                    MinDepth = 0,
                    MaxDepth = 64,
                    VeinSize = 9,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.2f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "gold",
                    BlockId = 14,
                    Rarity = 5.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 8,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.5f,
                    RichnessVariation = 0.25f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "diamond",
                    BlockId = 56,
                    Rarity = 20.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 7,
                    VeinSizeVariation = 2,
                    BaseRichness = 2.0f,
                    RichnessVariation = 0.1f,
                    VeinShape = "spherical"
                },
                new OreType
                {
                    Name = "copper",
                    BlockId = 21,
                    Rarity = 3.0f,
                    MinDepth = 0,
                    MaxDepth = 96,
                    VeinSize = 10,
                    VeinSizeVariation = 5,
                    BaseRichness = 1.1f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "lapis_lazuli",
                    BlockId = 22,
                    Rarity = 8.0f,
                    MinDepth = 0,
                    MaxDepth = 32,
                    VeinSize = 6,
                    VeinSizeVariation = 3,
                    BaseRichness = 1.8f,
                    RichnessVariation = 0.4f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "redstone",
                    BlockId = 73,
                    Rarity = 4.0f,
                    MinDepth = 0,
                    MaxDepth = 16,
                    VeinSize = 8,
                    VeinSizeVariation = 4,
                    BaseRichness = 1.6f,
                    RichnessVariation = 0.3f,
                    VeinShape = "irregular"
                },
                new OreType
                {
                    Name = "emerald",
                    BlockId = 129,
                    Rarity = 25.0f,
                    MinDepth = 4,
                    MaxDepth = 32,
                    VeinSize = 3,
                    VeinSizeVariation = 1,
                    BaseRichness = 3.0f,
                    RichnessVariation = 0.2f,
                    VeinShape = "spherical",
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Mountains }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome ore configurations
        /// </summary>
        private static List<BiomeOreConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeOreConfig>
            {
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallRichnessModifier = 1.5f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.8f,
                        ["diamond"] = 2.0f,
                        ["emerald"] = 5.0f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallRichnessModifier = 0.8f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["gold"] = 1.5f,
                        ["copper"] = 1.3f,
                        ["iron"] = 0.7f
                    },
                    ExcludedOres = new List<string> { "emerald" }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Swamp,
                    OverallRichnessModifier = 0.6f,
                    OreModifiers = new Dictionary<string, float>
                    {
                        ["coal"] = 0.8f,
                        ["iron"] = 0.5f,
                        ["lapis_lazuli"] = 1.2f
                    }
                },
                new BiomeOreConfig
                {
                    BiomeType = BiomeType.Ocean,
                    OverallRichnessModifier = 0.3f,
                    ExcludedOres = new List<string> { "emerald", "diamond" }
                }
            };
        }
    }
}
