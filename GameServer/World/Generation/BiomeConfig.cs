using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for biome generation with temperature and humidity gradients
    /// </summary>
    public class BiomeConfig
    {
        /// <summary>
        /// Temperature noise parameters
        /// </summary>
        [JsonPropertyName("temperature")]
        public TemperatureConfig Temperature { get; set; } = new();
        
        /// <summary>
        /// Humidity noise parameters
        /// </summary>
        [JsonPropertyName("humidity")]
        public HumidityConfig Humidity { get; set; } = new();
        
        /// <summary>
        /// Variation parameters for biome boundaries
        /// </summary>
        [JsonPropertyName("variation")]
        public VariationConfig Variation { get; set; } = new();
        
        /// <summary>
        /// Smoothing parameters for biome transitions
        /// </summary>
        [JsonPropertyName("smoothing")]
        public SmoothingConfig Smoothing { get; set; } = new();
        
        /// <summary>
        /// World dimensions for climate calculations
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
        
        /// <summary>
        /// List of all available biomes
        /// </summary>
        [JsonPropertyName("biomes")]
        public List<BiomeDefinition> Biomes { get; set; } = new();
        
        // Legacy properties for backward compatibility
        [JsonIgnore]
        public float TemperatureFrequency => Temperature.Frequency;
        [JsonIgnore]
        public int TemperatureOctaves => Temperature.Octaves;
        [JsonIgnore]
        public float TemperatureLacunarity => Temperature.Lacunarity;
        [JsonIgnore]
        public float TemperatureGain => Temperature.Gain;
        [JsonIgnore]
        public float HumidityFrequency => Humidity.Frequency;
        [JsonIgnore]
        public int HumidityOctaves => Humidity.Octaves;
        [JsonIgnore]
        public float HumidityLacunarity => Humidity.Lacunarity;
        [JsonIgnore]
        public float HumidityGain => Humidity.Gain;
        [JsonIgnore]
        public float VariationFrequency => Variation.Frequency;
        [JsonIgnore]
        public float VariationStrength => Variation.Strength;
        [JsonIgnore]
        public bool EnableBiomeSmoothing => Smoothing.Enabled;
        [JsonIgnore]
        public int SmoothingThreshold => Smoothing.Threshold;
        [JsonIgnore]
        public int WorldHeight => World.Height;
    }
    
    /// <summary>
    /// Temperature noise configuration
    /// </summary>
    public class TemperatureConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.002f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 4;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.0f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.5f;
        
        [JsonPropertyName("equatorBonus")]
        public float EquatorBonus { get; set; } = 0.3f;
        
        [JsonPropertyName("polePenalty")]
        public float PolePenalty { get; set; } = 0.4f;
    }
    
    /// <summary>
    /// Humidity noise configuration
    /// </summary>
    public class HumidityConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.003f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 3;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.2f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.6f;
        
        [JsonPropertyName("waterBonus")]
        public float WaterBonus { get; set; } = 0.2f;
        
        [JsonPropertyName("desertPenalty")]
        public float DesertPenalty { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// Variation configuration for biome boundaries
    /// </summary>
    public class VariationConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.01f;
        
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.1f;
        
        [JsonPropertyName("seedOffset")]
        public int SeedOffset { get; set; } = 12345;
    }
    
    /// <summary>
    /// Smoothing configuration for biome transitions
    /// </summary>
    public class SmoothingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; } = 5;
        
        [JsonPropertyName("passes")]
        public int Passes { get; set; } = 1;
        
        [JsonPropertyName("preserveRivers")]
        public bool PreserveRivers { get; set; } = true;
    }
    
    /// <summary>
    /// World dimensions configuration
    /// </summary>
    public class WorldConfig
    {
        [JsonPropertyName("height")]
        public int Height { get; set; } = 10000;
        
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
    
    /// <summary>
    /// Configuration for a single biome type
    /// </summary>
    public class BiomeDefinition
    {
        [JsonPropertyName("type")]
        public BiomeType Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("minTemperature")]
        public float MinTemperature { get; set; }
        
        [JsonPropertyName("maxTemperature")]
        public float MaxTemperature { get; set; }
        
        [JsonPropertyName("minHumidity")]
        public float MinHumidity { get; set; }
        
        [JsonPropertyName("maxHumidity")]
        public float MaxHumidity { get; set; }
        
        [JsonPropertyName("baseHeight")]
        public float BaseHeight { get; set; }
        
        [JsonPropertyName("heightVariation")]
        public float HeightVariation { get; set; }
        
        [JsonPropertyName("surfaceBlock")]
        public string SurfaceBlock { get; set; }
        
        [JsonPropertyName("subSurfaceBlock")]
        public string SubSurfaceBlock { get; set; }
        
        [JsonPropertyName("vegetation")]
        public Dictionary<string, float> Vegetation { get; set; } = new();
        
        [JsonPropertyName("ores")]
        public Dictionary<string, float> Ores { get; set; } = new();
        
        [JsonPropertyName("mobs")]
        public Dictionary<string, float> Mobs { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default biome configurations
    /// </summary>
    public static class BiomeConfigFactory
    {
        /// <summary>
        /// Creates a default biome configuration with all standard biomes
        /// </summary>
        public static BiomeConfig CreateDefault()
        {
            var config = new BiomeConfig();
            
            // Add all standard biomes
            config.Biomes.AddRange(GetStandardBiomes());
            
            return config;
        }
        
        /// <summary>
        /// Gets a list of standard Minecraft-like biomes
        /// </summary>
        private static List<BiomeDefinition> GetStandardBiomes()
        {
            return new List<BiomeDefinition>
            {
                // Ocean biomes
                new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.5f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.5f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["kelp"] = 0.3f,
                        ["seagrass"] = 0.5f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["squid"] = 0.8f,
                        ["dolphin"] = 0.3f
                    }
                },
                
                // Plains
                new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.8f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.6f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["grass"] = 0.8f,
                        ["flowers"] = 0.2f,
                        ["trees_oak"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["cow"] = 0.6f,
                        ["sheep"] = 0.5f,
                        ["chicken"] = 0.4f,
                        ["horse"] = 0.3f
                    }
                },
                
                // Desert
                new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.2f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["cactus"] = 0.2f,
                        ["dead_bush"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["rabbit"] = 0.4f,
                        ["camel"] = 0.3f
                    }
                },
                
                // Forest
                new BiomeDefinition
                {
                    Type = BiomeType.Forest,
                    Name = "Forest",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.4f,
                    MaxHumidity = 0.8f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak"] = 0.7f,
                        ["trees_birch"] = 0.3f,
                        ["flowers"] = 0.3f,
                        ["grass"] = 0.9f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.3f,
                        ["deer"] = 0.5f,
                        ["bird"] = 0.6f
                    }
                },
                
                // Taiga
                new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.3f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.7f,
                    BaseHeight = 0.2f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.8f,
                        ["trees_pine"] = 0.4f,
                        ["ferns"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.7f,
                        ["bear"] = 0.4f,
                        ["rabbit"] = 0.5f
                    }
                },
                
                // Jungle
                new BiomeDefinition
                {
                    Type = BiomeType.Jungle,
                    Name = "Jungle",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_jungle",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_jungle"] = 0.9f,
                        ["vines"] = 0.8f,
                        ["melons"] = 0.2f,
                        ["flowers_jungle"] = 0.4f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["parrot"] = 0.6f,
                        ["jaguar"] = 0.3f,
                        ["monkey"] = 0.4f
                    }
                },
                
                // Mountains
                new BiomeDefinition
                {
                    Type = BiomeType.Mountains,
                    Name = "Mountains",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.6f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.8f,
                    HeightVariation = 0.6f,
                    SurfaceBlock = "stone",
                    SubSurfaceBlock = "stone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.1f,
                        ["grass_mountain"] = 0.2f
                    },
                    Ores = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.5f,
                        ["diamond"] = 2.0f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["goat"] = 0.5f,
                        ["eagle"] = 0.3f
                    }
                },
                
                // Swamp
                new BiomeDefinition
                {
                    Type = BiomeType.Swamp,
                    Name = "Swamp",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.1f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "grass_swamp",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak_swamp"] = 0.6f,
                        ["lily_pads"] = 0.4f,
                        ["mushrooms"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["frog"] = 0.7f,
                        ["crocodile"] = 0.2f,
                        ["mosquito"] = 0.8f
                    }
                },
                
                // Snowy Tundra
                new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.2f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.4f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce_snow"] = 0.2f,
                        ["ice_spires"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["polar_bear"] = 0.4f,
                        ["snow_wolf"] = 0.5f,
                        ["penguin"] = 0.3f
                    }
                },
                
                // Savanna
                new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    MinTemperature = 0.6f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass_savanna",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_acacia"] = 0.4f,
                        ["grass_tall"] = 0.8f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["lion"] = 0.3f,
                        ["giraffe"] = 0.4f,
                        ["zebra"] = 0.5f,
                        ["elephant"] = 0.2f
                    }
                },
                
                // River
                new BiomeDefinition
                {
                    Type = BiomeType.River,
                    Name = "River",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.8f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.3f,
                    HeightVariation = 0.05f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["sugarcane"] = 0.4f,
                        ["seagrass"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["fish"] = 0.8f,
                        ["turtle"] = 0.3f
                    }
                },
                
                // Beach
                new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    MinTemperature = 0.2f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.8f,
                    BaseHeight = -0.2f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["palm_trees"] = 0.2f,
                        ["seashells"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["crab"] = 0.5f,
                        ["seagull"] = 0.6f
                    }
                }
            };
        }
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for biome generation with temperature and humidity gradients
    /// </summary>
    public class BiomeConfig
    {
        /// <summary>
        /// Temperature noise parameters
        /// </summary>
        [JsonPropertyName("temperature")]
        public TemperatureConfig Temperature { get; set; } = new();
        
        /// <summary>
        /// Humidity noise parameters
        /// </summary>
        [JsonPropertyName("humidity")]
        public HumidityConfig Humidity { get; set; } = new();
        
        /// <summary>
        /// Variation parameters for biome boundaries
        /// </summary>
        [JsonPropertyName("variation")]
        public VariationConfig Variation { get; set; } = new();
        
        /// <summary>
        /// Smoothing parameters for biome transitions
        /// </summary>
        [JsonPropertyName("smoothing")]
        public SmoothingConfig Smoothing { get; set; } = new();
        
        /// <summary>
        /// World dimensions for climate calculations
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
        
        /// <summary>
        /// List of all available biomes
        /// </summary>
        [JsonPropertyName("biomes")]
        public List<BiomeDefinition> Biomes { get; set; } = new();
        
        // Legacy properties for backward compatibility
        [JsonIgnore]
        public float TemperatureFrequency => Temperature.Frequency;
        [JsonIgnore]
        public int TemperatureOctaves => Temperature.Octaves;
        [JsonIgnore]
        public float TemperatureLacunarity => Temperature.Lacunarity;
        [JsonIgnore]
        public float TemperatureGain => Temperature.Gain;
        [JsonIgnore]
        public float HumidityFrequency => Humidity.Frequency;
        [JsonIgnore]
        public int HumidityOctaves => Humidity.Octaves;
        [JsonIgnore]
        public float HumidityLacunarity => Humidity.Lacunarity;
        [JsonIgnore]
        public float HumidityGain => Humidity.Gain;
        [JsonIgnore]
        public float VariationFrequency => Variation.Frequency;
        [JsonIgnore]
        public float VariationStrength => Variation.Strength;
        [JsonIgnore]
        public bool EnableBiomeSmoothing => Smoothing.Enabled;
        [JsonIgnore]
        public int SmoothingThreshold => Smoothing.Threshold;
        [JsonIgnore]
        public int WorldHeight => World.Height;
    }
    
    /// <summary>
    /// Temperature noise configuration
    /// </summary>
    public class TemperatureConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.002f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 4;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.0f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.5f;
        
        [JsonPropertyName("equatorBonus")]
        public float EquatorBonus { get; set; } = 0.3f;
        
        [JsonPropertyName("polePenalty")]
        public float PolePenalty { get; set; } = 0.4f;
    }
    
    /// <summary>
    /// Humidity noise configuration
    /// </summary>
    public class HumidityConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.003f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 3;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.2f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.6f;
        
        [JsonPropertyName("waterBonus")]
        public float WaterBonus { get; set; } = 0.2f;
        
        [JsonPropertyName("desertPenalty")]
        public float DesertPenalty { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// Variation configuration for biome boundaries
    /// </summary>
    public class VariationConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.01f;
        
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.1f;
        
        [JsonPropertyName("seedOffset")]
        public int SeedOffset { get; set; } = 12345;
    }
    
    /// <summary>
    /// Smoothing configuration for biome transitions
    /// </summary>
    public class SmoothingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; } = 5;
        
        [JsonPropertyName("passes")]
        public int Passes { get; set; } = 1;
        
        [JsonPropertyName("preserveRivers")]
        public bool PreserveRivers { get; set; } = true;
    }
    
    /// <summary>
    /// World dimensions configuration
    /// </summary>
    public class WorldConfig
    {
        [JsonPropertyName("height")]
        public int Height { get; set; } = 10000;
        
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
    
    /// <summary>
    /// Configuration for a single biome type
    /// </summary>
    public class BiomeDefinition
    {
        [JsonPropertyName("type")]
        public BiomeType Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("minTemperature")]
        public float MinTemperature { get; set; }
        
        [JsonPropertyName("maxTemperature")]
        public float MaxTemperature { get; set; }
        
        [JsonPropertyName("minHumidity")]
        public float MinHumidity { get; set; }
        
        [JsonPropertyName("maxHumidity")]
        public float MaxHumidity { get; set; }
        
        [JsonPropertyName("baseHeight")]
        public float BaseHeight { get; set; }
        
        [JsonPropertyName("heightVariation")]
        public float HeightVariation { get; set; }
        
        [JsonPropertyName("surfaceBlock")]
        public string SurfaceBlock { get; set; }
        
        [JsonPropertyName("subSurfaceBlock")]
        public string SubSurfaceBlock { get; set; }
        
        [JsonPropertyName("vegetation")]
        public Dictionary<string, float> Vegetation { get; set; } = new();
        
        [JsonPropertyName("ores")]
        public Dictionary<string, float> Ores { get; set; } = new();
        
        [JsonPropertyName("mobs")]
        public Dictionary<string, float> Mobs { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default biome configurations
    /// </summary>
    public static class BiomeConfigFactory
    {
        /// <summary>
        /// Creates a default biome configuration with all standard biomes
        /// </summary>
        public static BiomeConfig CreateDefault()
        {
            var config = new BiomeConfig();
            
            // Add all standard biomes
            config.Biomes.AddRange(GetStandardBiomes());
            
            return config;
        }
        
        /// <summary>
        /// Gets a list of standard Minecraft-like biomes
        /// </summary>
        private static List<BiomeDefinition> GetStandardBiomes()
        {
            return new List<BiomeDefinition>
            {
                // Ocean biomes
                new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.5f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.5f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["kelp"] = 0.3f,
                        ["seagrass"] = 0.5f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["squid"] = 0.8f,
                        ["dolphin"] = 0.3f
                    }
                },
                
                // Plains
                new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.8f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.6f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["grass"] = 0.8f,
                        ["flowers"] = 0.2f,
                        ["trees_oak"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["cow"] = 0.6f,
                        ["sheep"] = 0.5f,
                        ["chicken"] = 0.4f,
                        ["horse"] = 0.3f
                    }
                },
                
                // Desert
                new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.2f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["cactus"] = 0.2f,
                        ["dead_bush"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["rabbit"] = 0.4f,
                        ["camel"] = 0.3f
                    }
                },
                
                // Forest
                new BiomeDefinition
                {
                    Type = BiomeType.Forest,
                    Name = "Forest",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.4f,
                    MaxHumidity = 0.8f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak"] = 0.7f,
                        ["trees_birch"] = 0.3f,
                        ["flowers"] = 0.3f,
                        ["grass"] = 0.9f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.3f,
                        ["deer"] = 0.5f,
                        ["bird"] = 0.6f
                    }
                },
                
                // Taiga
                new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.3f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.7f,
                    BaseHeight = 0.2f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.8f,
                        ["trees_pine"] = 0.4f,
                        ["ferns"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.7f,
                        ["bear"] = 0.4f,
                        ["rabbit"] = 0.5f
                    }
                },
                
                // Jungle
                new BiomeDefinition
                {
                    Type = BiomeType.Jungle,
                    Name = "Jungle",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_jungle",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_jungle"] = 0.9f,
                        ["vines"] = 0.8f,
                        ["melons"] = 0.2f,
                        ["flowers_jungle"] = 0.4f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["parrot"] = 0.6f,
                        ["jaguar"] = 0.3f,
                        ["monkey"] = 0.4f
                    }
                },
                
                // Mountains
                new BiomeDefinition
                {
                    Type = BiomeType.Mountains,
                    Name = "Mountains",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.6f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.8f,
                    HeightVariation = 0.6f,
                    SurfaceBlock = "stone",
                    SubSurfaceBlock = "stone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.1f,
                        ["grass_mountain"] = 0.2f
                    },
                    Ores = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.5f,
                        ["diamond"] = 2.0f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["goat"] = 0.5f,
                        ["eagle"] = 0.3f
                    }
                },
                
                // Swamp
                new BiomeDefinition
                {
                    Type = BiomeType.Swamp,
                    Name = "Swamp",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.1f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "grass_swamp",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak_swamp"] = 0.6f,
                        ["lily_pads"] = 0.4f,
                        ["mushrooms"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["frog"] = 0.7f,
                        ["crocodile"] = 0.2f,
                        ["mosquito"] = 0.8f
                    }
                },
                
                // Snowy Tundra
                new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.2f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.4f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce_snow"] = 0.2f,
                        ["ice_spires"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["polar_bear"] = 0.4f,
                        ["snow_wolf"] = 0.5f,
                        ["penguin"] = 0.3f
                    }
                },
                
                // Savanna
                new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    MinTemperature = 0.6f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass_savanna",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_acacia"] = 0.4f,
                        ["grass_tall"] = 0.8f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["lion"] = 0.3f,
                        ["giraffe"] = 0.4f,
                        ["zebra"] = 0.5f,
                        ["elephant"] = 0.2f
                    }
                },
                
                // River
                new BiomeDefinition
                {
                    Type = BiomeType.River,
                    Name = "River",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.8f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.3f,
                    HeightVariation = 0.05f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["sugarcane"] = 0.4f,
                        ["seagrass"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["fish"] = 0.8f,
                        ["turtle"] = 0.3f
                    }
                },
                
                // Beach
                new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    MinTemperature = 0.2f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.8f,
                    BaseHeight = -0.2f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["palm_trees"] = 0.2f,
                        ["seashells"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["crab"] = 0.5f,
                        ["seagull"] = 0.6f
                    }
                }
            };
        }
    }
}
}
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for biome generation with temperature and humidity gradients
    /// </summary>
    public class BiomeConfig
    {
        /// <summary>
        /// Temperature noise parameters
        /// </summary>
        [JsonPropertyName("temperature")]
        public TemperatureConfig Temperature { get; set; } = new();
        
        /// <summary>
        /// Humidity noise parameters
        /// </summary>
        [JsonPropertyName("humidity")]
        public HumidityConfig Humidity { get; set; } = new();
        
        /// <summary>
        /// Variation parameters for biome boundaries
        /// </summary>
        [JsonPropertyName("variation")]
        public VariationConfig Variation { get; set; } = new();
        
        /// <summary>
        /// Smoothing parameters for biome transitions
        /// </summary>
        [JsonPropertyName("smoothing")]
        public SmoothingConfig Smoothing { get; set; } = new();
        
        /// <summary>
        /// World dimensions for climate calculations
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
        
        /// <summary>
        /// List of all available biomes
        /// </summary>
        [JsonPropertyName("biomes")]
        public List<BiomeDefinition> Biomes { get; set; } = new();
        
        // Legacy properties for backward compatibility
        [JsonIgnore]
        public float TemperatureFrequency => Temperature.Frequency;
        [JsonIgnore]
        public int TemperatureOctaves => Temperature.Octaves;
        [JsonIgnore]
        public float TemperatureLacunarity => Temperature.Lacunarity;
        [JsonIgnore]
        public float TemperatureGain => Temperature.Gain;
        [JsonIgnore]
        public float HumidityFrequency => Humidity.Frequency;
        [JsonIgnore]
        public int HumidityOctaves => Humidity.Octaves;
        [JsonIgnore]
        public float HumidityLacunarity => Humidity.Lacunarity;
        [JsonIgnore]
        public float HumidityGain => Humidity.Gain;
        [JsonIgnore]
        public float VariationFrequency => Variation.Frequency;
        [JsonIgnore]
        public float VariationStrength => Variation.Strength;
        [JsonIgnore]
        public bool EnableBiomeSmoothing => Smoothing.Enabled;
        [JsonIgnore]
        public int SmoothingThreshold => Smoothing.Threshold;
        [JsonIgnore]
        public int WorldHeight => World.Height;
    }
    
    /// <summary>
    /// Temperature noise configuration
    /// </summary>
    public class TemperatureConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.002f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 4;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.0f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.5f;
        
        [JsonPropertyName("equatorBonus")]
        public float EquatorBonus { get; set; } = 0.3f;
        
        [JsonPropertyName("polePenalty")]
        public float PolePenalty { get; set; } = 0.4f;
    }
    
    /// <summary>
    /// Humidity noise configuration
    /// </summary>
    public class HumidityConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.003f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 3;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.2f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.6f;
        
        [JsonPropertyName("waterBonus")]
        public float WaterBonus { get; set; } = 0.2f;
        
        [JsonPropertyName("desertPenalty")]
        public float DesertPenalty { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// Variation configuration for biome boundaries
    /// </summary>
    public class VariationConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.01f;
        
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.1f;
        
        [JsonPropertyName("seedOffset")]
        public int SeedOffset { get; set; } = 12345;
    }
    
    /// <summary>
    /// Smoothing configuration for biome transitions
    /// </summary>
    public class SmoothingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; } = 5;
        
        [JsonPropertyName("passes")]
        public int Passes { get; set; } = 1;
        
        [JsonPropertyName("preserveRivers")]
        public bool PreserveRivers { get; set; } = true;
    }
    
    /// <summary>
    /// World dimensions configuration
    /// </summary>
    public class WorldConfig
    {
        [JsonPropertyName("height")]
        public int Height { get; set; } = 10000;
        
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
    
    /// <summary>
    /// Configuration for a single biome type
    /// </summary>
    public class BiomeDefinition
    {
        [JsonPropertyName("type")]
        public BiomeType Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("minTemperature")]
        public float MinTemperature { get; set; }
        
        [JsonPropertyName("maxTemperature")]
        public float MaxTemperature { get; set; }
        
        [JsonPropertyName("minHumidity")]
        public float MinHumidity { get; set; }
        
        [JsonPropertyName("maxHumidity")]
        public float MaxHumidity { get; set; }
        
        [JsonPropertyName("baseHeight")]
        public float BaseHeight { get; set; }
        
        [JsonPropertyName("heightVariation")]
        public float HeightVariation { get; set; }
        
        [JsonPropertyName("surfaceBlock")]
        public string SurfaceBlock { get; set; }
        
        [JsonPropertyName("subSurfaceBlock")]
        public string SubSurfaceBlock { get; set; }
        
        [JsonPropertyName("vegetation")]
        public Dictionary<string, float> Vegetation { get; set; } = new();
        
        [JsonPropertyName("ores")]
        public Dictionary<string, float> Ores { get; set; } = new();
        
        [JsonPropertyName("mobs")]
        public Dictionary<string, float> Mobs { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default biome configurations
    /// </summary>
    public static class BiomeConfigFactory
    {
        /// <summary>
        /// Creates a default biome configuration with all standard biomes
        /// </summary>
        public static BiomeConfig CreateDefault()
        {
            var config = new BiomeConfig();
            
            // Add all standard biomes
            config.Biomes.AddRange(GetStandardBiomes());
            
            return config;
        }
        
        /// <summary>
        /// Gets a list of standard Minecraft-like biomes
        /// </summary>
        private static List<BiomeDefinition> GetStandardBiomes()
        {
            return new List<BiomeDefinition>
            {
                // Ocean biomes
                new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.5f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.5f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["kelp"] = 0.3f,
                        ["seagrass"] = 0.5f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["squid"] = 0.8f,
                        ["dolphin"] = 0.3f
                    }
                },
                
                // Plains
                new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.8f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.6f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["grass"] = 0.8f,
                        ["flowers"] = 0.2f,
                        ["trees_oak"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["cow"] = 0.6f,
                        ["sheep"] = 0.5f,
                        ["chicken"] = 0.4f,
                        ["horse"] = 0.3f
                    }
                },
                
                // Desert
                new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.2f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["cactus"] = 0.2f,
                        ["dead_bush"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["rabbit"] = 0.4f,
                        ["camel"] = 0.3f
                    }
                },
                
                // Forest
                new BiomeDefinition
                {
                    Type = BiomeType.Forest,
                    Name = "Forest",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.4f,
                    MaxHumidity = 0.8f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak"] = 0.7f,
                        ["trees_birch"] = 0.3f,
                        ["flowers"] = 0.3f,
                        ["grass"] = 0.9f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.3f,
                        ["deer"] = 0.5f,
                        ["bird"] = 0.6f
                    }
                },
                
                // Taiga
                new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.3f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.7f,
                    BaseHeight = 0.2f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.8f,
                        ["trees_pine"] = 0.4f,
                        ["ferns"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.7f,
                        ["bear"] = 0.4f,
                        ["rabbit"] = 0.5f
                    }
                },
                
                // Jungle
                new BiomeDefinition
                {
                    Type = BiomeType.Jungle,
                    Name = "Jungle",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_jungle",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_jungle"] = 0.9f,
                        ["vines"] = 0.8f,
                        ["melons"] = 0.2f,
                        ["flowers_jungle"] = 0.4f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["parrot"] = 0.6f,
                        ["jaguar"] = 0.3f,
                        ["monkey"] = 0.4f
                    }
                },
                
                // Mountains
                new BiomeDefinition
                {
                    Type = BiomeType.Mountains,
                    Name = "Mountains",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.6f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.8f,
                    HeightVariation = 0.6f,
                    SurfaceBlock = "stone",
                    SubSurfaceBlock = "stone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.1f,
                        ["grass_mountain"] = 0.2f
                    },
                    Ores = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.5f,
                        ["diamond"] = 2.0f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["goat"] = 0.5f,
                        ["eagle"] = 0.3f
                    }
                },
                
                // Swamp
                new BiomeDefinition
                {
                    Type = BiomeType.Swamp,
                    Name = "Swamp",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.1f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "grass_swamp",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak_swamp"] = 0.6f,
                        ["lily_pads"] = 0.4f,
                        ["mushrooms"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["frog"] = 0.7f,
                        ["crocodile"] = 0.2f,
                        ["mosquito"] = 0.8f
                    }
                },
                
                // Snowy Tundra
                new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.2f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.4f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce_snow"] = 0.2f,
                        ["ice_spires"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["polar_bear"] = 0.4f,
                        ["snow_wolf"] = 0.5f,
                        ["penguin"] = 0.3f
                    }
                },
                
                // Savanna
                new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    MinTemperature = 0.6f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass_savanna",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_acacia"] = 0.4f,
                        ["grass_tall"] = 0.8f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["lion"] = 0.3f,
                        ["giraffe"] = 0.4f,
                        ["zebra"] = 0.5f,
                        ["elephant"] = 0.2f
                    }
                },
                
                // River
                new BiomeDefinition
                {
                    Type = BiomeType.River,
                    Name = "River",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.8f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.3f,
                    HeightVariation = 0.05f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["sugarcane"] = 0.4f,
                        ["seagrass"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["fish"] = 0.8f,
                        ["turtle"] = 0.3f
                    }
                },
                
                // Beach
                new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    MinTemperature = 0.2f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.8f,
                    BaseHeight = -0.2f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["palm_trees"] = 0.2f,
                        ["seashells"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["crab"] = 0.5f,
                        ["seagull"] = 0.6f
                    }
                }
            };
        }
    }
}
}
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for biome generation with temperature and humidity gradients
    /// </summary>
    public class BiomeConfig
    {
        /// <summary>
        /// Temperature noise parameters
        /// </summary>
        [JsonPropertyName("temperature")]
        public TemperatureConfig Temperature { get; set; } = new();
        
        /// <summary>
        /// Humidity noise parameters
        /// </summary>
        [JsonPropertyName("humidity")]
        public HumidityConfig Humidity { get; set; } = new();
        
        /// <summary>
        /// Variation parameters for biome boundaries
        /// </summary>
        [JsonPropertyName("variation")]
        public VariationConfig Variation { get; set; } = new();
        
        /// <summary>
        /// Smoothing parameters for biome transitions
        /// </summary>
        [JsonPropertyName("smoothing")]
        public SmoothingConfig Smoothing { get; set; } = new();
        
        /// <summary>
        /// World dimensions for climate calculations
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
        
        /// <summary>
        /// List of all available biomes
        /// </summary>
        [JsonPropertyName("biomes")]
        public List<BiomeDefinition> Biomes { get; set; } = new();
        
        // Legacy properties for backward compatibility
        [JsonIgnore]
        public float TemperatureFrequency => Temperature.Frequency;
        [JsonIgnore]
        public int TemperatureOctaves => Temperature.Octaves;
        [JsonIgnore]
        public float TemperatureLacunarity => Temperature.Lacunarity;
        [JsonIgnore]
        public float TemperatureGain => Temperature.Gain;
        [JsonIgnore]
        public float HumidityFrequency => Humidity.Frequency;
        [JsonIgnore]
        public int HumidityOctaves => Humidity.Octaves;
        [JsonIgnore]
        public float HumidityLacunarity => Humidity.Lacunarity;
        [JsonIgnore]
        public float HumidityGain => Humidity.Gain;
        [JsonIgnore]
        public float VariationFrequency => Variation.Frequency;
        [JsonIgnore]
        public float VariationStrength => Variation.Strength;
        [JsonIgnore]
        public bool EnableBiomeSmoothing => Smoothing.Enabled;
        [JsonIgnore]
        public int SmoothingThreshold => Smoothing.Threshold;
        [JsonIgnore]
        public int WorldHeight => World.Height;
    }
    
    /// <summary>
    /// Temperature noise configuration
    /// </summary>
    public class TemperatureConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.002f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 4;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.0f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.5f;
        
        [JsonPropertyName("equatorBonus")]
        public float EquatorBonus { get; set; } = 0.3f;
        
        [JsonPropertyName("polePenalty")]
        public float PolePenalty { get; set; } = 0.4f;
    }
    
    /// <summary>
    /// Humidity noise configuration
    /// </summary>
    public class HumidityConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.003f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 3;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.2f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.6f;
        
        [JsonPropertyName("waterBonus")]
        public float WaterBonus { get; set; } = 0.2f;
        
        [JsonPropertyName("desertPenalty")]
        public float DesertPenalty { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// Variation configuration for biome boundaries
    /// </summary>
    public class VariationConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.01f;
        
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.1f;
        
        [JsonPropertyName("seedOffset")]
        public int SeedOffset { get; set; } = 12345;
    }
    
    /// <summary>
    /// Smoothing configuration for biome transitions
    /// </summary>
    public class SmoothingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; } = 5;
        
        [JsonPropertyName("passes")]
        public int Passes { get; set; } = 1;
        
        [JsonPropertyName("preserveRivers")]
        public bool PreserveRivers { get; set; } = true;
    }
    
    /// <summary>
    /// World dimensions configuration
    /// </summary>
    public class WorldConfig
    {
        [JsonPropertyName("height")]
        public int Height { get; set; } = 10000;
        
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
    
    /// <summary>
    /// Configuration for a single biome type
    /// </summary>
    public class BiomeDefinition
    {
        [JsonPropertyName("type")]
        public BiomeType Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("minTemperature")]
        public float MinTemperature { get; set; }
        
        [JsonPropertyName("maxTemperature")]
        public float MaxTemperature { get; set; }
        
        [JsonPropertyName("minHumidity")]
        public float MinHumidity { get; set; }
        
        [JsonPropertyName("maxHumidity")]
        public float MaxHumidity { get; set; }
        
        [JsonPropertyName("baseHeight")]
        public float BaseHeight { get; set; }
        
        [JsonPropertyName("heightVariation")]
        public float HeightVariation { get; set; }
        
        [JsonPropertyName("surfaceBlock")]
        public string SurfaceBlock { get; set; }
        
        [JsonPropertyName("subSurfaceBlock")]
        public string SubSurfaceBlock { get; set; }
        
        [JsonPropertyName("vegetation")]
        public Dictionary<string, float> Vegetation { get; set; } = new();
        
        [JsonPropertyName("ores")]
        public Dictionary<string, float> Ores { get; set; } = new();
        
        [JsonPropertyName("mobs")]
        public Dictionary<string, float> Mobs { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default biome configurations
    /// </summary>
    public static class BiomeConfigFactory
    {
        /// <summary>
        /// Creates a default biome configuration with all standard biomes
        /// </summary>
        public static BiomeConfig CreateDefault()
        {
            var config = new BiomeConfig();
            
            // Add all standard biomes
            config.Biomes.AddRange(GetStandardBiomes());
            
            return config;
        }
        
        /// <summary>
        /// Gets a list of standard Minecraft-like biomes
        /// </summary>
        private static List<BiomeDefinition> GetStandardBiomes()
        {
            return new List<BiomeDefinition>
            {
                // Ocean biomes
                new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.5f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.5f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["kelp"] = 0.3f,
                        ["seagrass"] = 0.5f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["squid"] = 0.8f,
                        ["dolphin"] = 0.3f
                    }
                },
                
                // Plains
                new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.8f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.6f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["grass"] = 0.8f,
                        ["flowers"] = 0.2f,
                        ["trees_oak"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["cow"] = 0.6f,
                        ["sheep"] = 0.5f,
                        ["chicken"] = 0.4f,
                        ["horse"] = 0.3f
                    }
                },
                
                // Desert
                new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.2f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["cactus"] = 0.2f,
                        ["dead_bush"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["rabbit"] = 0.4f,
                        ["camel"] = 0.3f
                    }
                },
                
                // Forest
                new BiomeDefinition
                {
                    Type = BiomeType.Forest,
                    Name = "Forest",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.4f,
                    MaxHumidity = 0.8f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak"] = 0.7f,
                        ["trees_birch"] = 0.3f,
                        ["flowers"] = 0.3f,
                        ["grass"] = 0.9f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.3f,
                        ["deer"] = 0.5f,
                        ["bird"] = 0.6f
                    }
                },
                
                // Taiga
                new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.3f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.7f,
                    BaseHeight = 0.2f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.8f,
                        ["trees_pine"] = 0.4f,
                        ["ferns"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.7f,
                        ["bear"] = 0.4f,
                        ["rabbit"] = 0.5f
                    }
                },
                
                // Jungle
                new BiomeDefinition
                {
                    Type = BiomeType.Jungle,
                    Name = "Jungle",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_jungle",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_jungle"] = 0.9f,
                        ["vines"] = 0.8f,
                        ["melons"] = 0.2f,
                        ["flowers_jungle"] = 0.4f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["parrot"] = 0.6f,
                        ["jaguar"] = 0.3f,
                        ["monkey"] = 0.4f
                    }
                },
                
                // Mountains
                new BiomeDefinition
                {
                    Type = BiomeType.Mountains,
                    Name = "Mountains",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.6f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.8f,
                    HeightVariation = 0.6f,
                    SurfaceBlock = "stone",
                    SubSurfaceBlock = "stone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.1f,
                        ["grass_mountain"] = 0.2f
                    },
                    Ores = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.5f,
                        ["diamond"] = 2.0f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["goat"] = 0.5f,
                        ["eagle"] = 0.3f
                    }
                },
                
                // Swamp
                new BiomeDefinition
                {
                    Type = BiomeType.Swamp,
                    Name = "Swamp",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.1f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "grass_swamp",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak_swamp"] = 0.6f,
                        ["lily_pads"] = 0.4f,
                        ["mushrooms"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["frog"] = 0.7f,
                        ["crocodile"] = 0.2f,
                        ["mosquito"] = 0.8f
                    }
                },
                
                // Snowy Tundra
                new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.2f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.4f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce_snow"] = 0.2f,
                        ["ice_spires"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["polar_bear"] = 0.4f,
                        ["snow_wolf"] = 0.5f,
                        ["penguin"] = 0.3f
                    }
                },
                
                // Savanna
                new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    MinTemperature = 0.6f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass_savanna",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_acacia"] = 0.4f,
                        ["grass_tall"] = 0.8f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["lion"] = 0.3f,
                        ["giraffe"] = 0.4f,
                        ["zebra"] = 0.5f,
                        ["elephant"] = 0.2f
                    }
                },
                
                // River
                new BiomeDefinition
                {
                    Type = BiomeType.River,
                    Name = "River",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.8f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.3f,
                    HeightVariation = 0.05f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["sugarcane"] = 0.4f,
                        ["seagrass"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["fish"] = 0.8f,
                        ["turtle"] = 0.3f
                    }
                },
                
                // Beach
                new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    MinTemperature = 0.2f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.8f,
                    BaseHeight = -0.2f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["palm_trees"] = 0.2f,
                        ["seashells"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["crab"] = 0.5f,
                        ["seagull"] = 0.6f
                    }
                }
            };
        }
    }
}
}
        [JsonIgnore]
        public float TemperatureFrequency => Temperature.Frequency;
        [JsonIgnore]
        public int TemperatureOctaves => Temperature.Octaves;
        [JsonIgnore]
        public float TemperatureLacunarity => Temperature.Lacunarity;
        [JsonIgnore]
        public float TemperatureGain => Temperature.Gain;
        [JsonIgnore]
        public float HumidityFrequency => Humidity.Frequency;
        [JsonIgnore]
        public int HumidityOctaves => Humidity.Octaves;
        [JsonIgnore]
        public float HumidityLacunarity => Humidity.Lacunarity;
        [JsonIgnore]
        public float HumidityGain => Humidity.Gain;
        [JsonIgnore]
        public float VariationFrequency => Variation.Frequency;
        [JsonIgnore]
        public float VariationStrength => Variation.Strength;
        [JsonIgnore]
        public bool EnableBiomeSmoothing => Smoothing.Enabled;
        [JsonIgnore]
        public int SmoothingThreshold => Smoothing.Threshold;
        [JsonIgnore]
        public int WorldHeight => World.Height;
    }
    
    /// <summary>
    /// Temperature noise configuration
    /// </summary>
    public class TemperatureConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.002f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 4;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.0f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.5f;
        
        [JsonPropertyName("equatorBonus")]
        public float EquatorBonus { get; set; } = 0.3f;
        
        [JsonPropertyName("polePenalty")]
        public float PolePenalty { get; set; } = 0.4f;
    }
    
    /// <summary>
    /// Humidity noise configuration
    /// </summary>
    public class HumidityConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.003f;
        
        [JsonPropertyName("octaves")]
        public int Octaves { get; set; } = 3;
        
        [JsonPropertyName("lacunarity")]
        public float Lacunarity { get; set; } = 2.2f;
        
        [JsonPropertyName("gain")]
        public float Gain { get; set; } = 0.6f;
        
        [JsonPropertyName("waterBonus")]
        public float WaterBonus { get; set; } = 0.2f;
        
        [JsonPropertyName("desertPenalty")]
        public float DesertPenalty { get; set; } = 0.3f;
    }
    
    /// <summary>
    /// Variation configuration for biome boundaries
    /// </summary>
    public class VariationConfig
    {
        [JsonPropertyName("frequency")]
        public float Frequency { get; set; } = 0.01f;
        
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.1f;
        
        [JsonPropertyName("seedOffset")]
        public int SeedOffset { get; set; } = 12345;
    }
    
    /// <summary>
    /// Smoothing configuration for biome transitions
    /// </summary>
    public class SmoothingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("threshold")]
        public int Threshold { get; set; } = 5;
        
        [JsonPropertyName("passes")]
        public int Passes { get; set; } = 1;
        
        [JsonPropertyName("preserveRivers")]
        public bool PreserveRivers { get; set; } = true;
    }
    
    /// <summary>
    /// World dimensions configuration
    /// </summary>
    public class WorldConfig
    {
        [JsonPropertyName("height")]
        public int Height { get; set; } = 10000;
        
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
    
    /// <summary>
    /// Factory for creating default biome configurations
    /// </summary>
    public static class BiomeConfigFactory
    {
        /// <summary>
        /// Creates a default biome configuration with all standard biomes
        /// </summary>
        public static BiomeConfig CreateDefault()
        {
            var config = new BiomeConfig();
            
            // Add all standard biomes
            config.Biomes.AddRange(GetStandardBiomes());
            
            return config;
        }
        
        /// <summary>
        /// Gets a list of standard Minecraft-like biomes
        /// </summary>
        private static List<BiomeDefinition> GetStandardBiomes()
        {
            return new List<BiomeDefinition>
            {
                // Ocean biomes
                new BiomeDefinition
                {
                    Type = BiomeType.Ocean,
                    Name = "Ocean",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.5f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.5f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["kelp"] = 0.3f,
                        ["seagrass"] = 0.5f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["squid"] = 0.8f,
                        ["dolphin"] = 0.3f
                    }
                },
                
                // Plains
                new BiomeDefinition
                {
                    Type = BiomeType.Plains,
                    Name = "Plains",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.8f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.6f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["grass"] = 0.8f,
                        ["flowers"] = 0.2f,
                        ["trees_oak"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["cow"] = 0.6f,
                        ["sheep"] = 0.5f,
                        ["chicken"] = 0.4f,
                        ["horse"] = 0.3f
                    }
                },
                
                // Desert
                new BiomeDefinition
                {
                    Type = BiomeType.Desert,
                    Name = "Desert",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.2f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["cactus"] = 0.2f,
                        ["dead_bush"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["rabbit"] = 0.4f,
                        ["camel"] = 0.3f
                    }
                },
                
                // Forest
                new BiomeDefinition
                {
                    Type = BiomeType.Forest,
                    Name = "Forest",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.4f,
                    MaxHumidity = 0.8f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.3f,
                    SurfaceBlock = "grass",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak"] = 0.7f,
                        ["trees_birch"] = 0.3f,
                        ["flowers"] = 0.3f,
                        ["grass"] = 0.9f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.3f,
                        ["deer"] = 0.5f,
                        ["bird"] = 0.6f
                    }
                },
                
                // Taiga
                new BiomeDefinition
                {
                    Type = BiomeType.Taiga,
                    Name = "Taiga",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.3f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.7f,
                    BaseHeight = 0.2f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.8f,
                        ["trees_pine"] = 0.4f,
                        ["ferns"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["wolf"] = 0.7f,
                        ["bear"] = 0.4f,
                        ["rabbit"] = 0.5f
                    }
                },
                
                // Jungle
                new BiomeDefinition
                {
                    Type = BiomeType.Jungle,
                    Name = "Jungle",
                    MinTemperature = 0.7f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.4f,
                    SurfaceBlock = "grass_jungle",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_jungle"] = 0.9f,
                        ["vines"] = 0.8f,
                        ["melons"] = 0.2f,
                        ["flowers_jungle"] = 0.4f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["parrot"] = 0.6f,
                        ["jaguar"] = 0.3f,
                        ["monkey"] = 0.4f
                    }
                },
                
                // Mountains
                new BiomeDefinition
                {
                    Type = BiomeType.Mountains,
                    Name = "Mountains",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.6f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.8f,
                    HeightVariation = 0.6f,
                    SurfaceBlock = "stone",
                    SubSurfaceBlock = "stone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce"] = 0.1f,
                        ["grass_mountain"] = 0.2f
                    },
                    Ores = new Dictionary<string, float>
                    {
                        ["coal"] = 1.2f,
                        ["iron"] = 1.3f,
                        ["gold"] = 1.5f,
                        ["diamond"] = 2.0f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["goat"] = 0.5f,
                        ["eagle"] = 0.3f
                    }
                },
                
                // Swamp
                new BiomeDefinition
                {
                    Type = BiomeType.Swamp,
                    Name = "Swamp",
                    MinTemperature = 0.3f,
                    MaxTemperature = 0.7f,
                    MinHumidity = 0.7f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.1f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "grass_swamp",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_oak_swamp"] = 0.6f,
                        ["lily_pads"] = 0.4f,
                        ["mushrooms"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["frog"] = 0.7f,
                        ["crocodile"] = 0.2f,
                        ["mosquito"] = 0.8f
                    }
                },
                
                // Snowy Tundra
                new BiomeDefinition
                {
                    Type = BiomeType.SnowyTundra,
                    Name = "Snowy Tundra",
                    MinTemperature = 0.0f,
                    MaxTemperature = 0.2f,
                    MinHumidity = 0.0f,
                    MaxHumidity = 0.4f,
                    BaseHeight = 0.0f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "snow",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_spruce_snow"] = 0.2f,
                        ["ice_spires"] = 0.1f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["polar_bear"] = 0.4f,
                        ["snow_wolf"] = 0.5f,
                        ["penguin"] = 0.3f
                    }
                },
                
                // Savanna
                new BiomeDefinition
                {
                    Type = BiomeType.Savanna,
                    Name = "Savanna",
                    MinTemperature = 0.6f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.2f,
                    MaxHumidity = 0.5f,
                    BaseHeight = 0.1f,
                    HeightVariation = 0.2f,
                    SurfaceBlock = "grass_savanna",
                    SubSurfaceBlock = "dirt",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["trees_acacia"] = 0.4f,
                        ["grass_tall"] = 0.8f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["lion"] = 0.3f,
                        ["giraffe"] = 0.4f,
                        ["zebra"] = 0.5f,
                        ["elephant"] = 0.2f
                    }
                },
                
                // River
                new BiomeDefinition
                {
                    Type = BiomeType.River,
                    Name = "River",
                    MinTemperature = 0.0f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.8f,
                    MaxHumidity = 1.0f,
                    BaseHeight = -0.3f,
                    HeightVariation = 0.05f,
                    SurfaceBlock = "water",
                    SubSurfaceBlock = "sand",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["sugarcane"] = 0.4f,
                        ["seagrass"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["fish"] = 0.8f,
                        ["turtle"] = 0.3f
                    }
                },
                
                // Beach
                new BiomeDefinition
                {
                    Type = BiomeType.Beach,
                    Name = "Beach",
                    MinTemperature = 0.2f,
                    MaxTemperature = 1.0f,
                    MinHumidity = 0.3f,
                    MaxHumidity = 0.8f,
                    BaseHeight = -0.2f,
                    HeightVariation = 0.1f,
                    SurfaceBlock = "sand",
                    SubSurfaceBlock = "sandstone",
                    Vegetation = new Dictionary<string, float>
                    {
                        ["palm_trees"] = 0.2f,
                        ["seashells"] = 0.3f
                    },
                    Mobs = new Dictionary<string, float>
                    {
                        ["crab"] = 0.5f,
                        ["seagull"] = 0.6f
                    }
                }
            };
        }
    }
}
