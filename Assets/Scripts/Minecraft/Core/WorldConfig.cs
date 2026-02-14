using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Client-side world configuration loader that mirrors the authoritative server JSON.
    /// Exposes hydrology/cave/lake tuning so Unity world previews match server generation.
    /// </summary>
    public class WorldConfig
    {
        private static WorldConfig _instance;
        public static WorldConfig Instance => _instance ??= LoadConfig();

        public static void ForceReload()
        {
            _instance = LoadConfig();
        }

        public string WorldName { get; private set; }
        public int Seed { get; private set; }
        public string GameMode { get; private set; }
        public int WorldHeight { get; private set; }
        public int ChunkSize { get; private set; }
        public int RenderDistance { get; private set; }
        public int SimulationDistance { get; private set; }
        public string MapControlProfilePath { get; private set; }
        public int MapControlProfileVersion { get; private set; }

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
            WorldName = string.IsNullOrWhiteSpace(data.WorldName) ? "DefaultWorld" : data.WorldName;
            Seed = data.Seed;
            GameMode = string.IsNullOrWhiteSpace(data.GameMode) ? "survival" : data.GameMode;
            WorldHeight = Math.Max(1, data.WorldHeight);
            ChunkSize = Math.Max(1, data.ChunkSize);
            RenderDistance = Math.Max(1, data.RenderDistance);
            SimulationDistance = Math.Max(1, data.SimulationDistance);
            MapControlProfilePath = string.IsNullOrWhiteSpace(data.MapControlProfilePath)
                ? "world-map-control.json"
                : data.MapControlProfilePath;
            MapControlProfileVersion = data.MapControlProfileVersion <= 0 ? 1 : data.MapControlProfileVersion;

            Terrain = new TerrainConfig(data.TerrainGeneration ?? new TerrainGenerationData());
            Water = new WaterConfig(data.Water ?? new WaterData());
            Caves = new CaveConfig(data.Caves ?? new CaveData());
            Ores = new OreConfig(data.Ores ?? new OreData());
            Structures = new StructureConfig(data.Structures ?? new StructureData());
            Lakes = new LakeConfig(data.Lakes ?? new LakeData());
        }

        private void InitializeDefaults()
        {
            InitializeFromData(new WorldConfigData());
        }

        public void ApplyToUnity()
        {
            // Apply quality settings based on configuration
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1);
        }
    }

    [Serializable]
    public class WorldConfigData
    {
        public string WorldName = "HELLO_MY_WORLD";
        public int Seed = 0;
        public string GameMode = "survival";
        public int WorldHeight = 256;
        public int ChunkSize = 16;
        public int RenderDistance = 10;
        public int SimulationDistance = 8;
        public string MapControlProfilePath = "world-map-control.json";
        public int MapControlProfileVersion = 35;
        public TerrainGenerationData TerrainGeneration = new TerrainGenerationData();
        public WaterData Water = new WaterData();
        public CaveData Caves = new CaveData();
        public OreData Ores = new OreData();
        public StructureData Structures = new StructureData();
        public LakeData Lakes = new LakeData();
    }

    [Serializable]
    public class TerrainGenerationData
    {
        public int SeaLevel = 62;
        public int BedrockLevel = 5;
        public float NoiseScale = 100.0f;
        public float NoiseAmplitude = 50.0f;
        public int Octaves = 4;
        public float Persistence = 0.5f;
        public float Lacunarity = 2.0f;
        public float BiomeScale = 0.005f;
        public float TemperatureScale = 0.003f;
        public float HumidityScale = 0.004f;
        public float MountainThreshold = 0.6f;
        public int MountainMaxHeight = 200;
        public int PlainBaseHeight = 64;
    }

    [Serializable]
    public class WaterData
    {
        public int GlobalWaterLevel = 62;
        public float RiverCenterThreshold = 0.0125f;
        public float RiverBankThreshold = 0.026f;
        public int HydrologySmoothIterations = 3;
        public float HydrologySmoothBlend = 0.62f;
        public int HydrologyReservoirIterations = 2;
        public float HydrologyReservoirBlend = 0.38f;
        public float HydrologyShorePush = 5.0f;
        public float HydrologySlopePenalty = 6.0f;
        public float HydrologyFlowGain = 0.5f;
        public float HydrologyFlowShadowWeight = 0.48f;
        public float HydrologyFlowShadowSlopeWeight = 0.38f;
        public float HydrologyPressureBlend = 0.42f;
        public float HydrologyPressureGradientClamp = 0.22f;
        public float HydrologyContinuityWeight = 0.35f;
        public float HydrologyEdgeFlowBias = 0.35f;
        public float HydrologyEdgeTangentWeight = 0.45f;
        public float HydrologyEdgeFlowLockWeight = 0.38f;
        public int HydrologyEdgeBlendRadius = 4;
        public int HydrologyWatershedStitchRadius = 2;
        public float HydrologyWatershedStitchWeight = 0.42f;
        public int HydrologyEdgeStabilityIterations = 3;
        public float HydrologyEdgeStabilityWeight = 0.35f;
        public float HydrologyEdgeVarianceClamp = 0.30f;
        public float HydrologyEdgeFluxBlend = 0.6f;
        public float HydrologyVarianceBlend = 0.58f;
        public float HydrologyVarianceClamp = 0.62f;
        public float HydrologyEdgeNormalizationBlend = 0.42f;
        public int HydrologyEdgeNormalizationIterations = 2;
        public float HydrologyFlowMemoryWeight = 0.42f;
        public float HydrologyWaterTableClampWeight = 0.42f;
        public int HydrologyWaterTableClampRange = 20;
        public float HydrologyWaterTableSlopeWeight = 0.55f;
        public float HydrologyFlowPersistence = 0.75f;
        public float HydrologyCatchmentWeight = 0.46f;
        public float HydrologyGradientWeight = 0.35f;
        public float HydrologyGradientSlopeWeight = 0.42f;
        public float HydrologyGradientClamp = 1.65f;
        public int HydrologyGradientStabilityIterations = 1;
        public float HydrologyGradientStabilityBlend = 0.45f;
        public int HydrologyDirectionalIterations = 1;
        public float HydrologyDirectionalBlend = 0.42f;
        public float HydrologyFlowDivergenceClamp = 0.55f;
        public float HydrologyCurvatureWeight = 0.32f;
        public int HydrologySeamRelaxIterations = 3;
        public float HydrologySeamRelaxBlend = 0.56f;
        public int RiparianSmoothIterations = 2;
        public float RiparianSmoothBlend = 0.65f;
        public float RiparianSaturationBoost = 0.18f;
        public int RiparianBufferRadius = 1;
        public float RiverReliefPenaltyWeight = 0.27f;
        public float HydrologyWarpFrequency = 0.0009f;
        public float HydrologyWarpAmplitude = 9.0f;
        public float RiverFlowAlignmentWeight = 0.28f;
        public float RiverGradientPenalty = 0.42f;
        public float RiverHeadwaterStabilityWeight = 0.35f;
        public float RiverAnisotropyWeight = 0.32f;
        public float RiverAnisotropyDamping = 0.35f;
        public float RiverMeanderJitter = 0.18f;
        public float RiverBankErosionWeight = 0.18f;
        public float RiverBankStabilityClamp = 0.35f;
        public float LakeRimErosionWeight = 0.32f;
        public float LakeInflowBlendWeight = 0.48f;
        public float RiverEdgeFeather = 0.5f;
        public float RiverEdgeContinuityWeight = 0.52f;
        public int RiverMouthSmoothRadius = 5;
        public float RiverDeltaWetlandStrength = 0.5f;
        public float RiverSeamFillStrength = 0.5f;
        public float RiverNoiseScale = 0.015f;
        public int RiverDepth = 7;
        public int RiverIntensitySmoothIterations = 3;
        public float RiverIntensitySmoothBlend = 0.6f;
        public float RiverConfluenceBoost = 0.38f;
        public float RiverBraidingWeight = 0.34f;
        public bool EnableOceans = true;
        public bool EnableRivers = true;
        public bool EnableLakes = true;
        public bool UseImprovedRivers = true;
        public bool UseImprovedLakes = true;
    }

    [Serializable]
    public class CaveData
    {
        public bool EnableCaves = true;
        public bool UseImprovedCaves = true;
        public bool UseRegionalMainCaves = true;
        public int RegionalMainCaveRegionSizeChunks = 4;
        public int RegionalMainCaveWormCountMin = 4;
        public int RegionalMainCaveWormCountMax = 9;
        public int RegionalMainCaveStepsMin = 180;
        public int RegionalMainCaveStepsMax = 320;
        public int RegionalMainCaveMinY = 14;
        public int RegionalMainCaveMaxY = 72;
        public float RegionalMainCaveRadiusMin = 1.8f;
        public float RegionalMainCaveRadiusMax = 3.2f;
        public float CaveDensity = 0.3f;
        public float CaveNoiseScale = 0.05f;
        public float Threshold = 0.45f;
        public float CaveThreshold = 0.45f;
        public int MinCaveHeight = 5;
        public int MaxCaveHeight = 128;
        public float HorizontalFrequency = 0.0026f;
        public float VerticalFrequency = 0.018f;
        public float NoiseThreshold = 0.45f;
        public float LavaThreshold = 0.28f;
        public float WaterThreshold = 0.34f;
        public float FloodedCaveNoiseFrequency = 0.0031f;
        public float FloodedCaveProximityToWaterTableWeight = 0.6f;
        public float FloodedCaveThreshold = 0.75f;
        public int StabilitySmoothIterations = 1;
        public float StabilitySmoothBlend = 0.55f;
        public float SupportDensity = 0.6f;
        public float SupportHydrationBias = 0.42f;
        public float SupportFlowBias = 0.2f;
        public float HydrologyStabilityWeight = 0.45f;
        public float FlowStabilityWeight = 0.25f;
        public float RoughnessStabilityWeight = 0.1f;
        public float RiverSuppressionWeight = 0.35f;
        public float MoistureRetentionWeight = 0.35f;
        public float MoistureFlowClamp = 0.65f;
        public float CaveEntranceFlowDampening = 0.58f;
        public float AquiferBarrierWeight = 0.52f;
        public float EdgeSealStrength = 0.5f;
        public float RiparianCaveGuardWeight = 0.42f;
        public float SupportPillarChance = 0.28f;
        public int RiparianPlugDepth = 2;
        public float CaveCeilingStabilityWeight = 0.35f;
        public float CeilingMoistureWeight = 0.28f;
        public float CeilingMoistureClamp = 0.35f;
    }

    [Serializable]
    public class OreData
    {
        public bool EnableOreGeneration = true;
        public OreConfigData Coal = new OreConfigData { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 };
        public OreConfigData Iron = new OreConfigData { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 };
        public OreConfigData Gold = new OreConfigData { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 };
        public OreConfigData Diamond = new OreConfigData { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 };
        public OreConfigData Redstone = new OreConfigData { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 };
        public OreConfigData Lapis = new OreConfigData { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 };
    }

    [Serializable]
    public class OreConfigData
    {
        public int MinHeight;
        public int MaxHeight;
        public int VeinSize;
        public int VeinsPerChunk;
    }

    [Serializable]
    public class StructureData
    {
        public bool EnableTrees = true;
        public float TreeDensity = 0.05f;
        public bool EnableVillages = false;
        public bool EnableMineshafts = false;
        public bool EnableDungeons = true;
        public float DungeonChance = 0.01f;
    }

    [Serializable]
    public class LakeData
    {
        public int MinDepth = 3;
        public int MaxDepth = 9;
        public int MaxRadius = 9;
        public int LakeBasinSmoothIterations = 4;
        public int ShelfDepth = 2;
        public float SpawnWeightBias = 0.3f;
        public float ShorelineBlend = 0.66f;
        public float RiverProximitySuppression = 0.35f;
        public float WetlandSaturationThreshold = 0.55f;
        public int OutflowCarveDepth = 2;
        public float OutflowSealWeight = 0.35f;
        public int WetlandBufferRadius = 3;
        public float FlowSeepageWeight = 0.38f;
        public float VarianceWeight = 0.3f;
        public float OutflowStabilityWeight = 0.36f;
        public float LakeOutflowTaper = 0.42f;
        public float SpillwayContinuityWeight = 0.58f;
    }

    // Configuration wrappers for type safety and easier access
    public class TerrainConfig
    {
        public int SeaLevel { get; }
        public int BedrockLevel { get; }
        public float NoiseScale { get; }
        public float NoiseAmplitude { get; }
        public int Octaves { get; }
        public float Persistence { get; }
        public float Lacunarity { get; }
        public float BiomeScale { get; }
        public float TemperatureScale { get; }
        public float HumidityScale { get; }
        public float MountainThreshold { get; }
        public int MountainMaxHeight { get; }
        public int PlainBaseHeight { get; }

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
        public int GlobalWaterLevel { get; }
        public float RiverCenterThreshold { get; }
        public float RiverBankThreshold { get; }
        public int HydrologySmoothIterations { get; }
        public float HydrologySmoothBlend { get; }
        public int HydrologyReservoirIterations { get; }
        public float HydrologyReservoirBlend { get; }
        public float HydrologyShorePush { get; }
        public float HydrologySlopePenalty { get; }
        public float HydrologyFlowGain { get; }
        public float HydrologyFlowShadowWeight { get; }
        public float HydrologyFlowShadowSlopeWeight { get; }
        public float HydrologyPressureBlend { get; }
        public float HydrologyPressureGradientClamp { get; }
        public float HydrologyContinuityWeight { get; }
        public float HydrologyEdgeFlowBias { get; }
        public float HydrologyEdgeTangentWeight { get; }
        public float HydrologyEdgeFlowLockWeight { get; }
        public int HydrologyEdgeBlendRadius { get; }
        public int HydrologyWatershedStitchRadius { get; }
        public float HydrologyWatershedStitchWeight { get; }
        public int HydrologyEdgeStabilityIterations { get; }
        public float HydrologyEdgeStabilityWeight { get; }
        public float HydrologyEdgeVarianceClamp { get; }
        public float HydrologyEdgeFluxBlend { get; }
        public float HydrologyVarianceBlend { get; }
        public float HydrologyVarianceClamp { get; }
        public float HydrologyEdgeNormalizationBlend { get; }
        public int HydrologyEdgeNormalizationIterations { get; }
        public float HydrologyFlowMemoryWeight { get; }
        public float HydrologyWaterTableClampWeight { get; }
        public int HydrologyWaterTableClampRange { get; }
        public float HydrologyWaterTableSlopeWeight { get; }
        public float HydrologyFlowPersistence { get; }
        public float HydrologyCatchmentWeight { get; }
        public float HydrologyGradientWeight { get; }
        public float HydrologyGradientSlopeWeight { get; }
        public float HydrologyGradientClamp { get; }
        public int HydrologyGradientStabilityIterations { get; }
        public float HydrologyGradientStabilityBlend { get; }
        public int HydrologyDirectionalIterations { get; }
        public float HydrologyDirectionalBlend { get; }
        public float HydrologyFlowDivergenceClamp { get; }
        public float HydrologyCurvatureWeight { get; }
        public int HydrologySeamRelaxIterations { get; }
        public float HydrologySeamRelaxBlend { get; }
        public int RiparianSmoothIterations { get; }
        public float RiparianSmoothBlend { get; }
        public float RiparianSaturationBoost { get; }
        public int RiparianBufferRadius { get; }
        public float RiverReliefPenaltyWeight { get; }
        public float HydrologyWarpFrequency { get; }
        public float HydrologyWarpAmplitude { get; }
        public float RiverFlowAlignmentWeight { get; }
        public float RiverGradientPenalty { get; }
        public float RiverHeadwaterStabilityWeight { get; }
        public float RiverAnisotropyWeight { get; }
        public float RiverAnisotropyDamping { get; }
        public float RiverMeanderJitter { get; }
        public float RiverBankErosionWeight { get; }
        public float RiverBankStabilityClamp { get; }
        public float LakeRimErosionWeight { get; }
        public float LakeInflowBlendWeight { get; }
        public float RiverEdgeFeather { get; }
        public float RiverEdgeContinuityWeight { get; }
        public int RiverMouthSmoothRadius { get; }
        public float RiverDeltaWetlandStrength { get; }
        public float RiverSeamFillStrength { get; }
        public float RiverNoiseScale { get; }
        public int RiverDepth { get; }
        public int RiverIntensitySmoothIterations { get; }
        public float RiverIntensitySmoothBlend { get; }
        public float RiverConfluenceBoost { get; }
        public float RiverBraidingWeight { get; }
        public bool EnableOceans { get; }
        public bool EnableRivers { get; }
        public bool EnableLakes { get; }
        public bool UseImprovedRivers { get; }
        public bool UseImprovedLakes { get; }

        public WaterConfig(WaterData data)
        {
            GlobalWaterLevel = data.GlobalWaterLevel;
            RiverCenterThreshold = data.RiverCenterThreshold;
            RiverBankThreshold = data.RiverBankThreshold;
            HydrologySmoothIterations = data.HydrologySmoothIterations;
            HydrologySmoothBlend = data.HydrologySmoothBlend;
            HydrologyReservoirIterations = data.HydrologyReservoirIterations;
            HydrologyReservoirBlend = data.HydrologyReservoirBlend;
            HydrologyShorePush = data.HydrologyShorePush;
            HydrologySlopePenalty = data.HydrologySlopePenalty;
            HydrologyFlowGain = data.HydrologyFlowGain;
            HydrologyFlowShadowWeight = data.HydrologyFlowShadowWeight;
            HydrologyFlowShadowSlopeWeight = data.HydrologyFlowShadowSlopeWeight;
            HydrologyPressureBlend = data.HydrologyPressureBlend;
            HydrologyPressureGradientClamp = data.HydrologyPressureGradientClamp;
            HydrologyContinuityWeight = data.HydrologyContinuityWeight;
            HydrologyEdgeFlowBias = data.HydrologyEdgeFlowBias;
            HydrologyEdgeTangentWeight = data.HydrologyEdgeTangentWeight;
            HydrologyEdgeFlowLockWeight = data.HydrologyEdgeFlowLockWeight;
            HydrologyEdgeBlendRadius = data.HydrologyEdgeBlendRadius;
            HydrologyWatershedStitchRadius = data.HydrologyWatershedStitchRadius;
            HydrologyWatershedStitchWeight = data.HydrologyWatershedStitchWeight;
            HydrologyEdgeStabilityIterations = data.HydrologyEdgeStabilityIterations;
            HydrologyEdgeStabilityWeight = data.HydrologyEdgeStabilityWeight;
            HydrologyEdgeVarianceClamp = data.HydrologyEdgeVarianceClamp;
            HydrologyEdgeFluxBlend = data.HydrologyEdgeFluxBlend;
            HydrologyVarianceBlend = data.HydrologyVarianceBlend;
            HydrologyVarianceClamp = data.HydrologyVarianceClamp;
            HydrologyEdgeNormalizationBlend = data.HydrologyEdgeNormalizationBlend;
            HydrologyEdgeNormalizationIterations = data.HydrologyEdgeNormalizationIterations;
            HydrologyFlowMemoryWeight = data.HydrologyFlowMemoryWeight;
            HydrologyWaterTableClampWeight = data.HydrologyWaterTableClampWeight;
            HydrologyWaterTableClampRange = data.HydrologyWaterTableClampRange;
            HydrologyWaterTableSlopeWeight = data.HydrologyWaterTableSlopeWeight;
            HydrologyFlowPersistence = data.HydrologyFlowPersistence;
            HydrologyCatchmentWeight = data.HydrologyCatchmentWeight;
            HydrologyGradientWeight = data.HydrologyGradientWeight;
            HydrologyGradientSlopeWeight = data.HydrologyGradientSlopeWeight;
            HydrologyGradientClamp = data.HydrologyGradientClamp;
            HydrologyGradientStabilityIterations = data.HydrologyGradientStabilityIterations;
            HydrologyGradientStabilityBlend = data.HydrologyGradientStabilityBlend;
            HydrologyDirectionalIterations = data.HydrologyDirectionalIterations;
            HydrologyDirectionalBlend = data.HydrologyDirectionalBlend;
            HydrologyFlowDivergenceClamp = data.HydrologyFlowDivergenceClamp;
            HydrologyCurvatureWeight = data.HydrologyCurvatureWeight;
            HydrologySeamRelaxIterations = data.HydrologySeamRelaxIterations;
            HydrologySeamRelaxBlend = data.HydrologySeamRelaxBlend;
            RiparianSmoothIterations = data.RiparianSmoothIterations;
            RiparianSmoothBlend = data.RiparianSmoothBlend;
            RiparianSaturationBoost = data.RiparianSaturationBoost;
            RiparianBufferRadius = data.RiparianBufferRadius;
            RiverReliefPenaltyWeight = data.RiverReliefPenaltyWeight;
            HydrologyWarpFrequency = data.HydrologyWarpFrequency;
            HydrologyWarpAmplitude = data.HydrologyWarpAmplitude;
            RiverFlowAlignmentWeight = data.RiverFlowAlignmentWeight;
            RiverGradientPenalty = data.RiverGradientPenalty;
            RiverHeadwaterStabilityWeight = data.RiverHeadwaterStabilityWeight;
            RiverAnisotropyWeight = data.RiverAnisotropyWeight;
            RiverAnisotropyDamping = data.RiverAnisotropyDamping;
            RiverMeanderJitter = data.RiverMeanderJitter;
            RiverBankErosionWeight = data.RiverBankErosionWeight;
            RiverBankStabilityClamp = data.RiverBankStabilityClamp;
            LakeRimErosionWeight = data.LakeRimErosionWeight;
            LakeInflowBlendWeight = data.LakeInflowBlendWeight;
            RiverEdgeFeather = data.RiverEdgeFeather;
            RiverEdgeContinuityWeight = data.RiverEdgeContinuityWeight;
            RiverMouthSmoothRadius = data.RiverMouthSmoothRadius;
            RiverDeltaWetlandStrength = data.RiverDeltaWetlandStrength;
            RiverSeamFillStrength = data.RiverSeamFillStrength;
            RiverNoiseScale = data.RiverNoiseScale;
            RiverDepth = data.RiverDepth;
            RiverIntensitySmoothIterations = data.RiverIntensitySmoothIterations;
            RiverIntensitySmoothBlend = data.RiverIntensitySmoothBlend;
            RiverConfluenceBoost = data.RiverConfluenceBoost;
            RiverBraidingWeight = data.RiverBraidingWeight;
            EnableOceans = data.EnableOceans;
            EnableRivers = data.EnableRivers;
            EnableLakes = data.EnableLakes;
            UseImprovedRivers = data.UseImprovedRivers;
            UseImprovedLakes = data.UseImprovedLakes;
        }
    }

    public class CaveConfig
    {
        public bool EnableCaves { get; }
        public bool UseImprovedCaves { get; }
        public bool UseRegionalMainCaves { get; }
        public int RegionalMainCaveRegionSizeChunks { get; }
        public int RegionalMainCaveWormCountMin { get; }
        public int RegionalMainCaveWormCountMax { get; }
        public int RegionalMainCaveStepsMin { get; }
        public int RegionalMainCaveStepsMax { get; }
        public int RegionalMainCaveMinY { get; }
        public int RegionalMainCaveMaxY { get; }
        public float RegionalMainCaveRadiusMin { get; }
        public float RegionalMainCaveRadiusMax { get; }
        public float CaveDensity { get; }
        public float CaveNoiseScale { get; }
        public float Threshold { get; }
        public float CaveThreshold { get; }
        public int MinCaveHeight { get; }
        public int MaxCaveHeight { get; }
        public float HorizontalFrequency { get; }
        public float VerticalFrequency { get; }
        public float NoiseThreshold { get; }
        public float LavaThreshold { get; }
        public float WaterThreshold { get; }
        public float FloodedCaveNoiseFrequency { get; }
        public float FloodedCaveProximityToWaterTableWeight { get; }
        public float FloodedCaveThreshold { get; }
        public int StabilitySmoothIterations { get; }
        public float StabilitySmoothBlend { get; }
        public float SupportDensity { get; }
        public float SupportHydrationBias { get; }
        public float SupportFlowBias { get; }
        public float HydrologyStabilityWeight { get; }
        public float FlowStabilityWeight { get; }
        public float RoughnessStabilityWeight { get; }
        public float RiverSuppressionWeight { get; }
        public float MoistureRetentionWeight { get; }
        public float MoistureFlowClamp { get; }
        public float CaveEntranceFlowDampening { get; }
        public float AquiferBarrierWeight { get; }
        public float RiparianCaveGuardWeight { get; }
        public float EdgeSealStrength { get; }
        public float SupportPillarChance { get; }
        public int RiparianPlugDepth { get; }
        public float CaveCeilingStabilityWeight { get; }
        public float CeilingMoistureWeight { get; }
        public float CeilingMoistureClamp { get; }

        public CaveConfig(CaveData data)
        {
            EnableCaves = data.EnableCaves;
            UseImprovedCaves = data.UseImprovedCaves;
            UseRegionalMainCaves = data.UseRegionalMainCaves;
            RegionalMainCaveRegionSizeChunks = data.RegionalMainCaveRegionSizeChunks;
            RegionalMainCaveWormCountMin = data.RegionalMainCaveWormCountMin;
            RegionalMainCaveWormCountMax = data.RegionalMainCaveWormCountMax;
            RegionalMainCaveStepsMin = data.RegionalMainCaveStepsMin;
            RegionalMainCaveStepsMax = data.RegionalMainCaveStepsMax;
            RegionalMainCaveMinY = data.RegionalMainCaveMinY;
            RegionalMainCaveMaxY = data.RegionalMainCaveMaxY;
            RegionalMainCaveRadiusMin = data.RegionalMainCaveRadiusMin;
            RegionalMainCaveRadiusMax = data.RegionalMainCaveRadiusMax;
            CaveDensity = data.CaveDensity;
            CaveNoiseScale = data.CaveNoiseScale;
            Threshold = data.Threshold;
            CaveThreshold = data.CaveThreshold;
            MinCaveHeight = data.MinCaveHeight;
            MaxCaveHeight = data.MaxCaveHeight;
            HorizontalFrequency = data.HorizontalFrequency;
            VerticalFrequency = data.VerticalFrequency;
            NoiseThreshold = data.NoiseThreshold;
            LavaThreshold = data.LavaThreshold;
            WaterThreshold = data.WaterThreshold;
            FloodedCaveNoiseFrequency = data.FloodedCaveNoiseFrequency;
            FloodedCaveProximityToWaterTableWeight = data.FloodedCaveProximityToWaterTableWeight;
            FloodedCaveThreshold = data.FloodedCaveThreshold;
            StabilitySmoothIterations = data.StabilitySmoothIterations;
            StabilitySmoothBlend = data.StabilitySmoothBlend;
            SupportDensity = data.SupportDensity;
            SupportHydrationBias = data.SupportHydrationBias;
            SupportFlowBias = data.SupportFlowBias;
            HydrologyStabilityWeight = data.HydrologyStabilityWeight;
            FlowStabilityWeight = data.FlowStabilityWeight;
            RoughnessStabilityWeight = data.RoughnessStabilityWeight;
            RiverSuppressionWeight = data.RiverSuppressionWeight;
            MoistureRetentionWeight = data.MoistureRetentionWeight;
            MoistureFlowClamp = data.MoistureFlowClamp;
            CaveEntranceFlowDampening = data.CaveEntranceFlowDampening;
            AquiferBarrierWeight = data.AquiferBarrierWeight;
            RiparianCaveGuardWeight = data.RiparianCaveGuardWeight;
            EdgeSealStrength = data.EdgeSealStrength;
            SupportPillarChance = data.SupportPillarChance;
            RiparianPlugDepth = data.RiparianPlugDepth;
            CaveCeilingStabilityWeight = data.CaveCeilingStabilityWeight;
            CeilingMoistureWeight = data.CeilingMoistureWeight;
            CeilingMoistureClamp = data.CeilingMoistureClamp;
        }
    }

    public class OreConfig
    {
        public bool EnableOreGeneration { get; }
        public Dictionary<string, OreConfigData> Ores { get; }

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
        public bool EnableTrees { get; }
        public float TreeDensity { get; }
        public bool EnableVillages { get; }
        public bool EnableMineshafts { get; }
        public bool EnableDungeons { get; }
        public float DungeonChance { get; }

        public StructureConfig(StructureData data)
        {
            EnableTrees = data.EnableTrees;
            TreeDensity = data.TreeDensity;
            EnableVillages = data.EnableVillages;
            EnableMineshafts = data.EnableMineshafts;
            EnableDungeons = data.EnableDungeons;
            DungeonChance = data.DungeonChance;
        }
    }

    public class LakeConfig
    {
        public int MinDepth { get; }
        public int MaxDepth { get; }
        public int MaxRadius { get; }
        public int LakeBasinSmoothIterations { get; }
        public int ShelfDepth { get; }
        public float SpawnWeightBias { get; }
        public float ShorelineBlend { get; }
        public float RiverProximitySuppression { get; }
        public float WetlandSaturationThreshold { get; }
        public int OutflowCarveDepth { get; }
        public float OutflowSealWeight { get; }
        public int WetlandBufferRadius { get; }
        public float FlowSeepageWeight { get; }
        public float VarianceWeight { get; }
        public float OutflowStabilityWeight { get; }
        public float LakeOutflowTaper { get; }
        public float SpillwayContinuityWeight { get; }

        public LakeConfig(LakeData data)
        {
            MinDepth = data.MinDepth;
            MaxDepth = data.MaxDepth;
            MaxRadius = data.MaxRadius;
            LakeBasinSmoothIterations = data.LakeBasinSmoothIterations;
            ShelfDepth = data.ShelfDepth;
            SpawnWeightBias = data.SpawnWeightBias;
            ShorelineBlend = data.ShorelineBlend;
            RiverProximitySuppression = data.RiverProximitySuppression;
            WetlandSaturationThreshold = data.WetlandSaturationThreshold;
            OutflowCarveDepth = data.OutflowCarveDepth;
            OutflowSealWeight = data.OutflowSealWeight;
            WetlandBufferRadius = data.WetlandBufferRadius;
            FlowSeepageWeight = data.FlowSeepageWeight;
            VarianceWeight = data.VarianceWeight;
            OutflowStabilityWeight = data.OutflowStabilityWeight;
            LakeOutflowTaper = data.LakeOutflowTaper;
            SpillwayContinuityWeight = data.SpillwayContinuityWeight;
        }
    }
}
