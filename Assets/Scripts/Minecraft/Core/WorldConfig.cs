using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Client-side world configuration manager that mirrors server's WorldGenerationConfig
    /// Ensures terrain generation parity between server and client
    /// </summary>
    public class WorldConfig
    {
        private static WorldConfig _instance;
        public static WorldConfig Instance => _instance ??= LoadConfig();

        public string WorldName { get; private set; }
        public int Seed { get; private set; }
        public string GameMode { get; private set; }
        public int WorldHeight { get; private set; }
        public int ChunkSize { get; private set; }
        public int RenderDistance { get; private set; }
        public int SimulationDistance { get; private set; }
        
        public TerrainConfig Terrain { get; private set; }
        public WaterConfig Water { get; private set; }
        public CaveConfig Caves { get; private set; }
        public OreConfig Ores { get; private set; }
        public StructureConfig Structures { get; private set; }
        public LakeConfig Lakes { get; private set; }

        private WorldConfig()
        {
        }

        private static WorldConfig LoadConfig()
        {
            var config = new WorldConfig();
            
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
                
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var configData = JsonUtility.FromJson<WorldConfigData>(jsonContent);
                    config.InitializeFromData(configData);
                }
                else
                {
                    Debug.LogWarning($"[WorldConfig] Configuration file not found at {configPath}, using defaults");
                    config.InitializeDefaults();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldConfig] Failed to load configuration: {ex.Message}");
                config.InitializeDefaults();
            }

            return config;
        }

        private void InitializeFromData(WorldConfigData data)
        {
            WorldName = data.WorldName ?? "DefaultWorld";
            Seed = data.Seed;
            GameMode = data.GameMode ?? "survival";
            WorldHeight = data.WorldHeight;
            ChunkSize = data.ChunkSize;
            RenderDistance = data.RenderDistance;
            SimulationDistance = data.SimulationDistance;

            Terrain = new TerrainConfig(data.TerrainGeneration);
            Water = new WaterConfig(data.Water);
            Caves = new CaveConfig(data.Caves);
            Ores = new OreConfig(data.Ores);
            Structures = new StructureConfig(data.Structures);
            Lakes = new LakeConfig(data.Lakes);
        }

        private void InitializeDefaults()
        {
            WorldName = "DefaultWorld";
            Seed = 0;
            GameMode = "survival";
            WorldHeight = 256;
            ChunkSize = 16;
            RenderDistance = 10;
            SimulationDistance = 8;

            Terrain = new TerrainConfig();
            Water = new WaterConfig();
            Caves = new CaveConfig();
            Ores = new OreConfig();
            Structures = new StructureConfig();
            Lakes = new LakeConfig();
        }

        public void ApplyToUnity()
        {
            // Apply quality settings based on configuration
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1); // Max quality for testing
            
            // Apply render distance to any relevant Unity systems
            // This would be used by chunk loading systems
        }
    }

    [System.Serializable]
    public class WorldConfigData
    {
        public string WorldName;
        public int Seed;
        public string GameMode;
        public int WorldHeight;
        public int ChunkSize;
        public int RenderDistance;
        public int SimulationDistance;
        public TerrainGenerationData TerrainGeneration;
        public WaterData Water;
        public CaveData Caves;
        public OreData Ores;
        public StructureData Structures;
        public LakeData Lakes;
    }

    [System.Serializable]
    public class TerrainGenerationData
    {
        public int SeaLevel;
        public int BedrockLevel;
        public float NoiseScale;
        public float NoiseAmplitude;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public float BiomeScale;
        public float TemperatureScale;
        public float HumidityScale;
        public float MountainThreshold;
        public int MountainMaxHeight;
        public int PlainBaseHeight;
    }

    [System.Serializable]
    public class WaterData
    {
        public int GlobalWaterLevel;
        public float RiverCenterThreshold;
        public float RiverBankThreshold;
        public int HydrologySmoothIterations;
        public float HydrologySmoothBlend;
        public float HydrologyShorePush;
        public float HydrologySlopePenalty;
        public float HydrologyFlowGain;
        public float HydrologyContinuityWeight;
        public float HydrologyEdgeFlowBias;
        public float HydrologyEdgeTangentWeight;
        public float HydrologyEdgeFlowLockWeight;
        public int HydrologyEdgeBlendRadius;
        public int HydrologyEdgeStabilityIterations;
        public float HydrologyEdgeStabilityWeight;
        public float HydrologyEdgeVarianceClamp;
        public float HydrologyWaterTableClampWeight;
        public int HydrologyWaterTableClampRange;
        public float HydrologyWaterTableSlopeWeight;
        public float HydrologyFlowPersistence;
        public float HydrologyGradientWeight;
        public float HydrologyGradientSlopeWeight;
        public float HydrologyGradientClamp;
        public int HydrologyGradientStabilityIterations;
        public float HydrologyGradientStabilityBlend;
        public int HydrologyDirectionalIterations;
        public float HydrologyDirectionalBlend;
        public float HydrologyFlowDivergenceClamp;
        public float HydrologyCurvatureWeight;
        public int HydrologySeamRelaxIterations;
        public float HydrologySeamRelaxBlend;
        public float RiverReliefPenaltyWeight;
        public float HydrologyWarpFrequency;
        public float HydrologyWarpAmplitude;
        public float RiverFlowAlignmentWeight;
        public float RiverGradientPenalty;
        public float RiverHeadwaterStabilityWeight;
        public float RiverAnisotropyWeight;
        public float RiverBankErosionWeight;
        public float LakeRimErosionWeight;
        public float LakeInflowBlendWeight;
        public float RiverNoiseScale;
        public int RiverDepth;
        public int RiverIntensitySmoothIterations;
        public float RiverIntensitySmoothBlend;
        public float RiverConfluenceBoost;
        public bool EnableOceans;
        public bool EnableRivers;
        public bool EnableLakes;
        public bool UseImprovedRivers;
        public bool UseImprovedLakes;
    }

    [System.Serializable]
    public class CaveData
    {
        public bool EnableCaves;
        public bool UseImprovedCaves;
        public bool UseRegionalMainCaves;
        public int RegionalMainCaveRegionSizeChunks;
        public int RegionalMainCaveWormCountMin;
        public int RegionalMainCaveWormCountMax;
        public int RegionalMainCaveStepsMin;
        public int RegionalMainCaveStepsMax;
        public int RegionalMainCaveMinY;
        public int RegionalMainCaveMaxY;
        public float RegionalMainCaveRadiusMin;
        public float RegionalMainCaveRadiusMax;
        public float CaveDensity;
        public float CaveNoiseScale;
        public float Threshold;
        public float CaveThreshold;
        public int MinCaveHeight;
        public int MaxCaveHeight;
        public float HorizontalFrequency;
        public float VerticalFrequency;
        public float NoiseThreshold;
        public float LavaThreshold;
        public float WaterThreshold;
        public float FloodedCaveNoiseFrequency;
        public float FloodedCaveProximityToWaterTableWeight;
        public float FloodedCaveThreshold;
        public int StabilitySmoothIterations;
        public float StabilitySmoothBlend;
        public float SupportDensity;
        public float SupportHydrationBias;
        public float SupportFlowBias;
        public float HydrologyStabilityWeight;
        public float FlowStabilityWeight;
        public float RoughnessStabilityWeight;
        public float RiverSuppressionWeight;
        public float MoistureRetentionWeight;
    }

    [System.Serializable]
    public class OreData
    {
        public bool EnableOreGeneration;
        public OreConfigData Coal;
        public OreConfigData Iron;
        public OreConfigData Gold;
        public OreConfigData Diamond;
        public OreConfigData Redstone;
        public OreConfigData Lapis;
    }

    [System.Serializable]
    public class OreConfigData
    {
        public int MinHeight;
        public int MaxHeight;
        public int VeinSize;
        public int VeinsPerChunk;
    }

    [System.Serializable]
    public class StructureData
    {
        public bool EnableTrees;
        public float TreeDensity;
        public bool EnableVillages;
        public bool EnableMineshafts;
        public bool EnableDungeons;
        public float DungeonChance;
    }

    [System.Serializable]
    public class LakeData
    {
        public int MinDepth;
        public int MaxDepth;
        public int MaxRadius;
        public int LakeBasinSmoothIterations;
        public float SpawnWeightBias;
        public float ShorelineBlend;
        public float RiverProximitySuppression;
    }

    // Configuration wrapper classes for type safety and easier access
    public class TerrainConfig
    {
        public int SeaLevel { get; private set; }
        public int BedrockLevel { get; private set; }
        public float NoiseScale { get; private set; }
        public float NoiseAmplitude { get; private set; }
        public int Octaves { get; private set; }
        public float Persistence { get; private set; }
        public float Lacunarity { get; private set; }
        public float BiomeScale { get; private set; }
        public float TemperatureScale { get; private set; }
        public float HumidityScale { get; private set; }
        public float MountainThreshold { get; private set; }
        public int MountainMaxHeight { get; private set; }
        public int PlainBaseHeight { get; private set; }

        public TerrainConfig() : this(new TerrainGenerationData()) { }

        public TerrainConfig(TerrainGenerationData data)
        {
            SeaLevel = data.SeaLevel;
            BedrockLevel = data.BedrockLevel;
            NoiseScale = data.NoiseScale;
            NoiseAmplitude = data.NoiseAmplitude;
            Octaves = data.Octaves;
            Persistence = data.Persistence;
            Lacunarity = data.Lacunarity;
            BiomeScale = data.BiomeScale;
            TemperatureScale = data.TemperatureScale;
            HumidityScale = data.HumidityScale;
            MountainThreshold = data.MountainThreshold;
            MountainMaxHeight = data.MountainMaxHeight;
            PlainBaseHeight = data.PlainBaseHeight;
        }
    }

    public class WaterConfig
    {
        public int GlobalWaterLevel { get; private set; }
        public float RiverCenterThreshold { get; private set; }
        public float RiverBankThreshold { get; private set; }
        public bool EnableRivers { get; private set; }
        public bool EnableLakes { get; private set; }
        public bool UseImprovedRivers { get; private set; }
        public bool UseImprovedLakes { get; private set; }

        public WaterConfig() : this(new WaterData()) { }

        public WaterConfig(WaterData data)
        {
            GlobalWaterLevel = data.GlobalWaterLevel;
            RiverCenterThreshold = data.RiverCenterThreshold;
            RiverBankThreshold = data.RiverBankThreshold;
            EnableRivers = data.EnableRivers;
            EnableLakes = data.EnableLakes;
            UseImprovedRivers = data.UseImprovedRivers;
            UseImprovedLakes = data.UseImprovedLakes;
        }
    }

    public class CaveConfig
    {
        public bool EnableCaves { get; private set; }
        public bool UseImprovedCaves { get; private set; }
        public bool UseRegionalMainCaves { get; private set; }
        public float HorizontalFrequency { get; private set; }
        public float VerticalFrequency { get; private set; }
        public float Threshold { get; private set; }
        public float LavaThreshold { get; private set; }
        public float WaterThreshold { get; private set; }

        public CaveConfig() : this(new CaveData()) { }

        public CaveConfig(CaveData data)
        {
            EnableCaves = data.EnableCaves;
            UseImprovedCaves = data.UseImprovedCaves;
            UseRegionalMainCaves = data.UseRegionalMainCaves;
            HorizontalFrequency = data.HorizontalFrequency;
            VerticalFrequency = data.VerticalFrequency;
            Threshold = data.Threshold;
            LavaThreshold = data.LavaThreshold;
            WaterThreshold = data.WaterThreshold;
        }
    }

    public class OreConfig
    {
        public bool EnableOreGeneration { get; private set; }
        public Dictionary<string, OreConfigData> Ores { get; private set; }

        public OreConfig() : this(new OreData()) { }

        public OreConfig(OreData data)
        {
            EnableOreGeneration = data.EnableOreGeneration;
            Ores = new Dictionary<string, OreConfigData>
            {
                ["Coal"] = data.Coal,
                ["Iron"] = data.Iron,
                ["Gold"] = data.Gold,
                ["Diamond"] = data.Diamond,
                ["Redstone"] = data.Redstone,
                ["Lapis"] = data.Lapis
            };
        }
    }

    public class StructureConfig
    {
        public bool EnableTrees { get; private set; }
        public float TreeDensity { get; private set; }
        public bool EnableDungeons { get; private set; }
        public float DungeonChance { get; private set; }

        public StructureConfig() : this(new StructureData()) { }

        public StructureConfig(StructureData data)
        {
            EnableTrees = data.EnableTrees;
            TreeDensity = data.TreeDensity;
            EnableDungeons = data.EnableDungeons;
            DungeonChance = data.DungeonChance;
        }
    }

    public class LakeConfig
    {
        public int MinDepth { get; private set; }
        public int MaxDepth { get; private set; }
        public int MaxRadius { get; private set; }

        public LakeConfig() : this(new LakeData()) { }

        public LakeConfig(LakeData data)
        {
            MinDepth = data.MinDepth;
            MaxDepth = data.MaxDepth;
            MaxRadius = data.MaxRadius;
        }
    }
}using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Client-side world configuration manager that mirrors server's WorldGenerationConfig
    /// Ensures terrain generation parity between server and client
    /// </summary>
    public class WorldConfig
    {
        private static WorldConfig _instance;
        public static WorldConfig Instance => _instance ??= LoadConfig();

        public string WorldName { get; private set; }
        public int Seed { get; private set; }
        public string GameMode { get; private set; }
        public int WorldHeight { get; private set; }
        public int ChunkSize { get; private set; }
        public int RenderDistance { get; private set; }
        public int SimulationDistance { get; private set; }
        
        public TerrainConfig Terrain { get; private set; }
        public WaterConfig Water { get; private set; }
        public CaveConfig Caves { get; private set; }
        public OreConfig Ores { get; private set; }
        public StructureConfig Structures { get; private set; }
        public LakeConfig Lakes { get; private set; }

        private WorldConfig()
        {
        }

        private static WorldConfig LoadConfig()
        {
            var config = new WorldConfig();
            
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
                
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var configData = JsonUtility.FromJson<WorldConfigData>(jsonContent);
                    config.InitializeFromData(configData);
                }
                else
                {
                    Debug.LogWarning($"[WorldConfig] Configuration file not found at {configPath}, using defaults");
                    config.InitializeDefaults();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldConfig] Failed to load configuration: {ex.Message}");
                config.InitializeDefaults();
            }

            return config;
        }

        private void InitializeFromData(WorldConfigData data)
        {
            WorldName = data.WorldName ?? "DefaultWorld";
            Seed = data.Seed;
            GameMode = data.GameMode ?? "survival";
            WorldHeight = data.WorldHeight;
            ChunkSize = data.ChunkSize;
            RenderDistance = data.RenderDistance;
            SimulationDistance = data.SimulationDistance;

            Terrain = new TerrainConfig(data.TerrainGeneration);
            Water = new WaterConfig(data.Water);
            Caves = new CaveConfig(data.Caves);
            Ores = new OreConfig(data.Ores);
            Structures = new StructureConfig(data.Structures);
            Lakes = new LakeConfig(data.Lakes);
        }

        private void InitializeDefaults()
        {
            WorldName = "DefaultWorld";
            Seed = 0;
            GameMode = "survival";
            WorldHeight = 256;
            ChunkSize = 16;
            RenderDistance = 10;
            SimulationDistance = 8;

            Terrain = new TerrainConfig();
            Water = new WaterConfig();
            Caves = new CaveConfig();
            Ores = new OreConfig();
            Structures = new StructureConfig();
            Lakes = new LakeConfig();
        }

        public void ApplyToUnity()
        {
            // Apply quality settings based on configuration
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1); // Max quality for testing
            
            // Apply render distance to any relevant Unity systems
            // This would be used by chunk loading systems
        }
    }

    [System.Serializable]
    public class WorldConfigData
    {
        public string WorldName;
        public int Seed;
        public string GameMode;
        public int WorldHeight;
        public int ChunkSize;
        public int RenderDistance;
        public int SimulationDistance;
        public TerrainGenerationData TerrainGeneration;
        public WaterData Water;
        public CaveData Caves;
        public OreData Ores;
        public StructureData Structures;
        public LakeData Lakes;
    }

    [System.Serializable]
    public class TerrainGenerationData
    {
        public int SeaLevel;
        public int BedrockLevel;
        public float NoiseScale;
        public float NoiseAmplitude;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public float BiomeScale;
        public float TemperatureScale;
        public float HumidityScale;
        public float MountainThreshold;
        public int MountainMaxHeight;
        public int PlainBaseHeight;
    }

    [System.Serializable]
    public class WaterData
    {
        public int GlobalWaterLevel;
        public float RiverCenterThreshold;
        public float RiverBankThreshold;
        public int HydrologySmoothIterations;
        public float HydrologySmoothBlend;
        public float HydrologyShorePush;
        public float HydrologySlopePenalty;
        public float HydrologyFlowGain;
        public float HydrologyContinuityWeight;
        public float HydrologyEdgeFlowBias;
        public float HydrologyEdgeTangentWeight;
        public float HydrologyEdgeFlowLockWeight;
        public int HydrologyEdgeBlendRadius;
        public int HydrologyEdgeStabilityIterations;
        public float HydrologyEdgeStabilityWeight;
        public float HydrologyEdgeVarianceClamp;
        public float HydrologyWaterTableClampWeight;
        public int HydrologyWaterTableClampRange;
        public float HydrologyWaterTableSlopeWeight;
        public float HydrologyFlowPersistence;
        public float HydrologyGradientWeight;
        public float HydrologyGradientSlopeWeight;
        public float HydrologyGradientClamp;
        public int HydrologyGradientStabilityIterations;
        public float HydrologyGradientStabilityBlend;
        public int HydrologyDirectionalIterations;
        public float HydrologyDirectionalBlend;
        public float HydrologyFlowDivergenceClamp;
        public float HydrologyCurvatureWeight;
        public int HydrologySeamRelaxIterations;
        public float HydrologySeamRelaxBlend;
        public float RiverReliefPenaltyWeight;
        public float HydrologyWarpFrequency;
        public float HydrologyWarpAmplitude;
        public float RiverFlowAlignmentWeight;
        public float RiverGradientPenalty;
        public float RiverHeadwaterStabilityWeight;
        public float RiverAnisotropyWeight;
        public float RiverBankErosionWeight;
        public float LakeRimErosionWeight;
        public float LakeInflowBlendWeight;
        public float RiverNoiseScale;
        public int RiverDepth;
        public int RiverIntensitySmoothIterations;
        public float RiverIntensitySmoothBlend;
        public float RiverConfluenceBoost;
        public bool EnableOceans;
        public bool EnableRivers;
        public bool EnableLakes;
        public bool UseImprovedRivers;
        public bool UseImprovedLakes;
    }

    [System.Serializable]
    public class CaveData
    {
        public bool EnableCaves;
        public bool UseImprovedCaves;
        public bool UseRegionalMainCaves;
        public int RegionalMainCaveRegionSizeChunks;
        public int RegionalMainCaveWormCountMin;
        public int RegionalMainCaveWormCountMax;
        public int RegionalMainCaveStepsMin;
        public int RegionalMainCaveStepsMax;
        public int RegionalMainCaveMinY;
        public int RegionalMainCaveMaxY;
        public float RegionalMainCaveRadiusMin;
        public float RegionalMainCaveRadiusMax;
        public float CaveDensity;
        public float CaveNoiseScale;
        public float Threshold;
        public float CaveThreshold;
        public int MinCaveHeight;
        public int MaxCaveHeight;
        public float HorizontalFrequency;
        public float VerticalFrequency;
        public float NoiseThreshold;
        public float LavaThreshold;
        public float WaterThreshold;
        public float FloodedCaveNoiseFrequency;
        public float FloodedCaveProximityToWaterTableWeight;
        public float FloodedCaveThreshold;
        public int StabilitySmoothIterations;
        public float StabilitySmoothBlend;
        public float SupportDensity;
        public float SupportHydrationBias;
        public float SupportFlowBias;
        public float HydrologyStabilityWeight;
        public float FlowStabilityWeight;
        public float RoughnessStabilityWeight;
        public float RiverSuppressionWeight;
        public float MoistureRetentionWeight;
    }

    [System.Serializable]
    public class OreData
    {
        public bool EnableOreGeneration;
        public OreConfigData Coal;
        public OreConfigData Iron;
        public OreConfigData Gold;
        public OreConfigData Diamond;
        public OreConfigData Redstone;
        public OreConfigData Lapis;
    }

    [System.Serializable]
    public class OreConfigData
    {
        public int MinHeight;
        public int MaxHeight;
        public int VeinSize;
        public int VeinsPerChunk;
    }

    [System.Serializable]
    public class StructureData
    {
        public bool EnableTrees;
        public float TreeDensity;
        public bool EnableVillages;
        public bool EnableMineshafts;
        public bool EnableDungeons;
        public float DungeonChance;
    }

    [System.Serializable]
    public class LakeData
    {
        public int MinDepth;
        public int MaxDepth;
        public int MaxRadius;
        public int LakeBasinSmoothIterations;
        public float SpawnWeightBias;
        public float ShorelineBlend;
        public float RiverProximitySuppression;
    }

    // Configuration wrapper classes for type safety and easier access
    public class TerrainConfig
    {
        public int SeaLevel { get; private set; }
        public int BedrockLevel { get; private set; }
        public float NoiseScale { get; private set; }
        public float NoiseAmplitude { get; private set; }
        public int Octaves { get; private set; }
        public float Persistence { get; private set; }
        public float Lacunarity { get; private set; }
        public float BiomeScale { get; private set; }
        public float TemperatureScale { get; private set; }
        public float HumidityScale { get; private set; }
        public float MountainThreshold { get; private set; }
        public int MountainMaxHeight { get; private set; }
        public int PlainBaseHeight { get; private set; }

        public TerrainConfig() : this(new TerrainGenerationData()) { }

        public TerrainConfig(TerrainGenerationData data)
        {
            SeaLevel = data.SeaLevel;
            BedrockLevel = data.BedrockLevel;
            NoiseScale = data.NoiseScale;
            NoiseAmplitude = data.NoiseAmplitude;
            Octaves = data.Octaves;
            Persistence = data.Persistence;
            Lacunarity = data.Lacunarity;
            BiomeScale = data.BiomeScale;
            TemperatureScale = data.TemperatureScale;
            HumidityScale = data.HumidityScale;
            MountainThreshold = data.MountainThreshold;
            MountainMaxHeight = data.MountainMaxHeight;
            PlainBaseHeight = data.PlainBaseHeight;
        }
    }

    public class WaterConfig
    {
        public int GlobalWaterLevel { get; private set; }
        public float RiverCenterThreshold { get; private set; }
        public float RiverBankThreshold { get; private set; }
        public bool EnableRivers { get; private set; }
        public bool EnableLakes { get; private set; }
        public bool UseImprovedRivers { get; private set; }
        public bool UseImprovedLakes { get; private set; }

        public WaterConfig() : this(new WaterData()) { }

        public WaterConfig(WaterData data)
        {
            GlobalWaterLevel = data.GlobalWaterLevel;
            RiverCenterThreshold = data.RiverCenterThreshold;
            RiverBankThreshold = data.RiverBankThreshold;
            EnableRivers = data.EnableRivers;
            EnableLakes = data.EnableLakes;
            UseImprovedRivers = data.UseImprovedRivers;
            UseImprovedLakes = data.UseImprovedLakes;
        }
    }

    public class CaveConfig
    {
        public bool EnableCaves { get; private set; }
        public bool UseImprovedCaves { get; private set; }
        public bool UseRegionalMainCaves { get; private set; }
        public float HorizontalFrequency { get; private set; }
        public float VerticalFrequency { get; private set; }
        public float Threshold { get; private set; }
        public float LavaThreshold { get; private set; }
        public float WaterThreshold { get; private set; }

        public CaveConfig() : this(new CaveData()) { }

        public CaveConfig(CaveData data)
        {
            EnableCaves = data.EnableCaves;
            UseImprovedCaves = data.UseImprovedCaves;
            UseRegionalMainCaves = data.UseRegionalMainCaves;
            HorizontalFrequency = data.HorizontalFrequency;
            VerticalFrequency = data.VerticalFrequency;
            Threshold = data.Threshold;
            LavaThreshold = data.LavaThreshold;
            WaterThreshold = data.WaterThreshold;
        }
    }

    public class OreConfig
    {
        public bool EnableOreGeneration { get; private set; }
        public Dictionary<string, OreConfigData> Ores { get; private set; }

        public OreConfig() : this(new OreData()) { }

        public OreConfig(OreData data)
        {
            EnableOreGeneration = data.EnableOreGeneration;
            Ores = new Dictionary<string, OreConfigData>
            {
                ["Coal"] = data.Coal,
                ["Iron"] = data.Iron,
                ["Gold"] = data.Gold,
                ["Diamond"] = data.Diamond,
                ["Redstone"] = data.Redstone,
                ["Lapis"] = data.Lapis
            };
        }
    }

    public class StructureConfig
    {
        public bool EnableTrees { get; private set; }
        public float TreeDensity { get; private set; }
        public bool EnableDungeons { get; private set; }
        public float DungeonChance { get; private set; }

        public StructureConfig() : this(new StructureData()) { }

        public StructureConfig(StructureData data)
        {
            EnableTrees = data.EnableTrees;
            TreeDensity = data.TreeDensity;
            EnableDungeons = data.EnableDungeons;
            DungeonChance = data.DungeonChance;
        }
    }

    public class LakeConfig
    {
        public int MinDepth { get; private set; }
        public int MaxDepth { get; private set; }
        public int MaxRadius { get; private set; }

        public LakeConfig() : this(new LakeData()) { }

        public LakeConfig(LakeData data)
        {
            MinDepth = data.MinDepth;
            MaxDepth = data.MaxDepth;
            MaxRadius = data.MaxRadius;
        }
    }
}
}
