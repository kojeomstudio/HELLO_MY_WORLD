using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for world generation
    /// </summary>
    public class WorldGenerationConfig
    {
        /// <summary>
        /// Biome generation configuration
        /// </summary>
        [JsonPropertyName("biomeConfig")]
        public BiomeConfig BiomeConfig { get; set; } = new();
        
        /// <summary>
        /// Ore distribution configuration
        /// </summary>
        [JsonPropertyName("oreDistributionConfig")]
        public OreDistributionConfig OreDistributionConfig { get; set; } = new();
        
        /// <summary>
        /// Structure generation configuration
        /// </summary>
        [JsonPropertyName("structureGenerationConfig")]
        public StructureGenerationConfig StructureGenerationConfig { get; set; } = new();
        
        /// <summary>
        /// Entity spawn configuration
        /// </summary>
        [JsonPropertyName("entitySpawnConfig")]
        public EntitySpawnConfig EntitySpawnConfig { get; set; } = new();
        
        /// <summary>
        /// World configuration
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
    }
    
    /// <summary>
    /// World configuration
    /// </summary>
    public class WorldConfig
    {
        /// <summary>
        /// Maximum world height
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; } = 256;
        
        /// <summary>
        /// Sea level (0.0 to 1.0)
        /// </summary>
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        /// <summary>
        /// Number of climate zones
        /// </summary>
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
}
namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for world generation
    /// </summary>
    public class WorldGenerationConfig
    {
        /// <summary>
        /// Biome generation configuration
        /// </summary>
        [JsonPropertyName("biomeConfig")]
        public BiomeConfig BiomeConfig { get; set; } = new();
        
        /// <summary>
        /// Ore distribution configuration
        /// </summary>
        [JsonPropertyName("oreDistributionConfig")]
        public OreDistributionConfig OreDistributionConfig { get; set; } = new();
        
        /// <summary>
        /// Structure generation configuration
        /// </summary>
        [JsonPropertyName("structureGenerationConfig")]
        public StructureGenerationConfig StructureGenerationConfig { get; set; } = new();
        
        /// <summary>
        /// Entity spawn configuration
        /// </summary>
        [JsonPropertyName("entitySpawnConfig")]
        public EntitySpawnConfig EntitySpawnConfig { get; set; } = new();
        
        /// <summary>
        /// World configuration
        /// </summary>
        [JsonPropertyName("world")]
        public WorldConfig World { get; set; } = new();
    }
    
    /// <summary>
    /// World configuration
    /// </summary>
    public class WorldConfig
    {
        /// <summary>
        /// Maximum world height
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; } = 256;
        
        /// <summary>
        /// Sea level (0.0 to 1.0)
        /// </summary>
        [JsonPropertyName("seaLevel")]
        public float SeaLevel { get; set; } = 0.5f;
        
        /// <summary>
        /// Number of climate zones
        /// </summary>
        [JsonPropertyName("climateZones")]
        public int ClimateZones { get; set; } = 8;
    }
}
