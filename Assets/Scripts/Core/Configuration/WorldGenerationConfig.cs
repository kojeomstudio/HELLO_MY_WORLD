using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Configuration
{
    /// <summary>
    /// World generation configuration data structure
    /// </summary>
    [Serializable]
    public class WorldGenerationConfig : ConfigBase
    {
        [Header("World Generation Settings")]
        public WorldSettings worldGeneration = new WorldSettings();
        
        [Header("Terrain Settings")]
        public TerrainSettings terrain = new TerrainSettings();
        
        [Header("Water Settings")]
        public WaterSettings water = new WaterSettings();
        
        [Header("Biome Settings")]
        public BiomeSettings biomes = new BiomeSettings();
        
        [Header("Structure Settings")]
        public StructureSettings structures = new StructureSettings();
        
        [Header("Tree Settings")]
        public TreeSettings trees = new TreeSettings();
        
        [Header("Vegetation Settings")]
        public VegetationSettings vegetation = new VegetationSettings();
        
        public override bool Validate()
        {
            bool isValid = true;
            
            // Validate world generation settings
            if (worldGeneration.chunkSize <= 0 || (worldGeneration.chunkSize & (worldGeneration.chunkSize - 1)) != 0)
            {
                Debug.LogError("Chunk size must be a power of 2");
                isValid = false;
            }
            
            if (worldGeneration.minWorldHeight >= worldGeneration.maxWorldHeight)
            {
                Debug.LogError("Min world height must be less than max world height");
                isValid = false;
            }
            
            // Validate terrain settings
            if (terrain.heightScale <= 0)
            {
                Debug.LogError("Height scale must be positive");
                isValid = false;
            }
            
            // Validate biome settings
            if (biomes.biomes == null || biomes.biomes.Count == 0)
            {
                Debug.LogError("At least one biome must be defined");
                isValid = false;
            }
            
            // Validate structure settings
            if (structures.villages.enabled && structures.villages.distance <= 0)
            {
                Debug.LogError("Village distance must be positive when villages are enabled");
                isValid = false;
            }
            
            // Validate tree settings
            if (trees.oak.enabled && trees.oak.minHeight > trees.oak.maxHeight)
            {
                Debug.LogError("Oak tree min height must be less than max height");
                isValid = false;
            }
            
            return isValid;
        }
    }
    
    [Serializable]
    public class WorldSettings
    {
        public int seed = 0;
        public string worldType = "default";
        public bool generateStructures = true;
        public bool generateFeatures = true;
        public int chunkSize = 16;
        public int renderDistance = 10;
        public int simulationDistance = 12;
        public int maxWorldHeight = 256;
        public int minWorldHeight = -64;
    }
    
    [Serializable]
    public class TerrainSettings
    {
        public float heightScale = 1.0f;
        public float heightVariation = 0.5f;
        public float roughness = 0.6f;
        public float detail = 0.4f;
        public int seaLevel = 63;
        public CaveSettings caves = new CaveSettings();
        public RavineSettings ravines = new RavineSettings();
        public OreSettings ores = new OreSettings();
    }
    
    [Serializable]
    public class CaveSettings
    {
        public bool enabled = true;
        public float density = 0.1f;
        public int minHeight = -10;
        public int maxHeight = 40;
        public int size = 8;
        public float branching = 0.7f;
        public float waterCaveChance = 0.05f;
    }
    
    [Serializable]
    public class RavineSettings
    {
        public bool enabled = true;
        public float density = 0.02f;
        public int minHeight = 20;
        public int maxHeight = 70;
        public int width = 5;
        public int depth = 30;
        public int length = 100;
    }
    
    [Serializable]
    public class OreSettings
    {
        public OreConfig coal = new OreConfig();
        public OreConfig iron = new OreConfig();
        public OreConfig gold = new OreConfig();
        public OreConfig diamond = new OreConfig();
        public OreConfig redstone = new OreConfig();
        public OreConfig lapis = new OreConfig();
        public OreConfig emerald = new OreConfig();
    }
    
    [Serializable]
    public class OreConfig
    {
        public bool enabled = true;
        public int minHeight = 0;
        public int maxHeight = 128;
        public int veinSize = 8;
        public int veinsPerChunk = 1;
        public float frequency = 0.1f;
    }
    
    [Serializable]
    public class WaterSettings
    {
        public RiverSettings rivers = new RiverSettings();
        public LakeSettings lakes = new LakeSettings();
        public OceanSettings oceans = new OceanSettings();
    }
    
    [Serializable]
    public class RiverSettings
    {
        public bool enabled = true;
        public float frequency = 0.02f;
        public int width = 4;
        public int depth = 3;
        public float meandering = 0.8f;
        public float flowSpeed = 1.0f;
    }
    
    [Serializable]
    public class LakeSettings
    {
        public bool enabled = true;
        public float frequency = 0.01f;
        public int minRadius = 20;
        public int maxRadius = 100;
        public int minDepth = 5;
        public int maxDepth = 30;
        public float shoreSmoothing = 0.7f;
    }
    
    [Serializable]
    public class OceanSettings
    {
        public bool enabled = true;
        public float temperatureThreshold = 0.5f;
        public int depth = 30;
        public string floorType = "sand";
        public bool generateKelp = true;
        public bool generateSeagrass = true;
    }
    
    [Serializable]
    public class BiomeSettings
    {
        public float temperatureScale = 1.0f;
        public float humidityScale = 1.0f;
        public float continentalnessScale = 0.25f;
        public float erosionScale = 0.5f;
        public float weirdnessScale = 0.5f;
        public List<BiomeConfig> biomes = new List<BiomeConfig>();
    }
    
    [Serializable]
    public class BiomeConfig
    {
        public string id = "";
        public string name = "";
        public float temperature = 0.5f;
        public float humidity = 0.5f;
        public string color = "#FFFFFF";
        public string grassColor = "#FFFFFF";
        public string foliageColor = "#FFFFFF";
        public string waterColor = "#FFFFFF";
        public List<string> features = new List<string>();
        public List<string> treeTypes = new List<string>();
        public List<string> flowerTypes = new List<string>();
        public float grassDensity = 0.5f;
    }
    
    [Serializable]
    public class StructureSettings
    {
        public VillageSettings villages = new VillageSettings();
        public DungeonSettings dungeons = new DungeonSettings();
        public StrongholdSettings strongholds = new StrongholdSettings();
        public MineshaftSettings mineshafts = new MineshaftSettings();
        public NetherFortressSettings nether_fortress = new NetherFortressSettings();
        public OceanMonumentSettings ocean_monument = new OceanMonumentSettings();
        public EndCitySettings end_city = new EndCitySettings();
        public WoodlandMansionSettings woodland_mansion = new WoodlandMansionSettings();
    }
    
    [Serializable]
    public class VillageSettings
    {
        public bool enabled = true;
        public int distance = 32;
        public int separation = 8;
        public int size = 6;
        public int minDistance = 8;
        public int maxDistance = 100;
        public List<string> biomes = new List<string>();
        public List<HouseType> houseTypes = new List<HouseType>();
    }
    
    [Serializable]
    public class HouseType
    {
        public string type = "";
        public int weight = 1;
        public int minCount = 0;
        public int maxCount = 1;
    }
    
    [Serializable]
    public class DungeonSettings
    {
        public bool enabled = true;
        public float frequency = 0.01f;
        public int minHeight = 0;
        public int maxHeight = 50;
        public int minSize = 5;
        public int maxSize = 7;
        public float spawnerChance = 0.8f;
        public float chestChance = 0.9f;
        public List<string> mobTypes = new List<string>();
    }
    
    [Serializable]
    public class StrongholdSettings
    {
        public bool enabled = true;
        public int count = 3;
        public int distance = 32;
        public int spread = 3;
        public List<string> structures = new List<string>();
    }
    
    [Serializable]
    public class MineshaftSettings
    {
        public bool enabled = true;
        public float frequency = 0.004f;
        public int minHeight = 0;
        public int maxHeight = 50;
        public int maxBranches = 10;
        public int branchLength = 8;
    }
    
    [Serializable]
    public class NetherFortressSettings
    {
        public bool enabled = true;
        public float frequency = 0.001f;
        public int minHeight = 60;
        public int maxHeight = 120;
        public int minSize = 10;
        public int maxSize = 20;
        public float spawnerChance = 0.5f;
    }
    
    [Serializable]
    public class OceanMonumentSettings
    {
        public bool enabled = true;
        public float frequency = 0.0005f;
        public int minDepth = 20;
        public int maxDepth = 40;
        public int size = 58;
        public float spawnerChance = 0.8f;
    }
    
    [Serializable]
    public class EndCitySettings
    {
        public bool enabled = true;
        public float frequency = 0.001f;
        public int minHeight = 60;
        public int maxHeight = 80;
        public int size = 100;
        public float shipChance = 0.1f;
    }
    
    [Serializable]
    public class WoodlandMansionSettings
    {
        public bool enabled = true;
        public float frequency = 0.0005f;
        public int minHeight = 60;
        public int maxHeight = 80;
        public int size = 80;
        public float spawnerChance = 0.7f;
    }
    
    [Serializable]
    public class TreeSettings
    {
        public TreeConfig oak = new TreeConfig();
        public TreeConfig birch = new TreeConfig();
        public TreeConfig spruce = new TreeConfig();
        public TreeConfig jungle = new TreeConfig();
        public TreeConfig acacia = new TreeConfig();
        public TreeConfig dark_oak = new TreeConfig();
    }
    
    [Serializable]
    public class TreeConfig
    {
        public bool enabled = true;
        public int minHeight = 4;
        public int maxHeight = 8;
        public int trunkWidth = 1;
        public int leafRadius = 3;
        public List<string> biomes = new List<string>();
        public bool vines = false;
    }
    
    [Serializable]
    public class VegetationSettings
    {
        public FlowerSettings flowers = new FlowerSettings();
        public GrassSettings grass = new GrassSettings();
        public MushroomSettings mushrooms = new MushroomSettings();
    }
    
    [Serializable]
    public class FlowerSettings
    {
        public bool enabled = true;
        public float density = 0.1f;
        public List<FlowerType> types = new List<FlowerType>();
    }
    
    [Serializable]
    public class FlowerType
    {
        public string id = "";
        public string name = "";
        public string color = "#FFFFFF";
        public List<string> biomes = new List<string>();
    }
    
    [Serializable]
    public class GrassSettings
    {
        public bool enabled = true;
        public float density = 0.8f;
        public List<GrassType> types = new List<GrassType>();
    }
    
    [Serializable]
    public class GrassType
    {
        public string id = "";
        public string name = "";
        public float height = 1.0f;
        public List<string> biomes = new List<string>();
    }
    
    [Serializable]
    public class MushroomSettings
    {
        public bool enabled = true;
        public float density = 0.05f;
        public List<MushroomType> types = new List<MushroomType>();
    }
    
    [Serializable]
    public class MushroomType
    {
        public string id = "";
        public string name = "";
        public List<string> biomes = new List<string>();
    }
}
