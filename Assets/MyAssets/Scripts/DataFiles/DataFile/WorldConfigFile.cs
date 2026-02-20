using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WorldConfig
{
    public int SubWorld_Count_X_Axis_Per_WorldArea;
    public int SubWorld_Count_Y_Axis_Per_WorldArea;
    public int SubWorld_Count_Z_Axis_Per_WorldArea;
    public float OneTileUnit;
    public int SubWorldSizeX;
    public int SubWorldSizeY;
    public int SubWorldSizeZ;
    public int ChunkSize;
    public int RenderDistance;
    public int SimulationDistance;
    public string MapControlProfilePath;
    public int MapControlProfileVersion;
    public float ChunkLoadIntervalSeconds;
    public WorldEnviromentsConfig EnviromentsConfig;
    public int GlobalWaterLevel;
    public float RiverCenterThreshold;
    public float RiverBankThreshold;
    public bool EnableRivers;
    public bool EnableLakes;
    public bool EnableCaves;
    public bool UseImprovedRivers;
    public bool UseImprovedLakes;
    public bool UseImprovedCaves;
    public int LakeMinDepth;
    public int LakeMaxDepth;
    public int LakeMaxRadius;
    public int LakeBasinSmoothIterations;
    public int HydrologySmoothIterations;
    public float HydrologySmoothBlend;
    public float HydrologyShorePush;
    public float HydrologySlopePenalty;
    public float HydrologyFlowGain;
    public int HydrologyReservoirIterations;
    public float HydrologyReservoirBlend;
    public float HydrologyFlowMemoryWeight;
    public float HydrologyFlowShadowWeight;
    public float HydrologyFlowShadowSlopeWeight;
    public float HydrologyContinuityWeight;
    public float HydrologyPressureBlend;
    public float HydrologyPressureGradientClamp;
    public float HydrologyEdgeFlowBias;
    public float HydrologyEdgeTangentWeight;
    public float HydrologyEdgeFlowLockWeight;
    public int HydrologyEdgeBlendRadius;
    public int HydrologyEdgeStabilityIterations;
    public float HydrologyEdgeStabilityWeight;
    public float HydrologyEdgeVarianceClamp;
    public float HydrologyEdgeFluxBlend;
    public float HydrologyVarianceBlend;
    public float HydrologyVarianceClamp;
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
    public float HydrologyWarpFrequency;
    public float HydrologyWarpAmplitude;
    public int RiparianSmoothIterations;
    public float RiparianSmoothBlend;
    public float RiparianSaturationBoost;
    public int RiparianBufferRadius;
    public float RiverFlowAlignmentWeight;
    public float RiverGradientPenalty;
    public float RiverHeadwaterStabilityWeight;
    public float RiverAnisotropyWeight;
    public float RiverMeanderJitter;
    public float RiverReliefPenaltyWeight;
    public float RiverBankErosionWeight;
    public float LakeRimErosionWeight;
    public float LakeInflowBlendWeight;
    public float LakeSpawnWeightBias;
    public float LakeShorelineBlend;
    public float LakeVarianceWeight;
    public float WetlandSaturationThreshold;
    public int OutflowCarveDepth;
    public float OutflowStabilityWeight;
    public float LakeSpillRetentionWeight;
    public int LakeShelfDepth;
    public int LakeWetlandBufferRadius;
    public float RiverProximitySuppression;
    public float FlowSeepageWeight;
    public float RiverNoiseScale;
    public int RiverDepth;
    public int RiverIntensitySmoothIterations;
    public float RiverIntensitySmoothBlend;
    public float RiverConfluenceBoost;
    public float RiverTributaryCaptureWeight;
    public float RiverAvulsionResistance;
    public float RiverEdgeFeather;
    public int RiverMouthSmoothRadius;
    public float RiverDeltaWetlandStrength;
    public float RiverSeamFillStrength;
    public int CaveStabilitySmoothIterations;
    public float CaveStabilitySmoothBlend;
    public float CaveSupportDensity;
    public float SupportPillarChance;
    public float SupportHydrationBias;
    public float SupportFlowBias;
    public int RiparianPlugDepth;
    public float HydrologyStabilityWeight;
    public float FlowStabilityWeight;
    public float RoughnessStabilityWeight;
    public float RiverSuppressionWeight;
    public float MoistureRetentionWeight;
    public float GroundwaterConnectivityWeight;
    public float CaveVentilationBias;
    public float EdgeSealStrength;
    public float RiparianCaveGuardWeight;
    public float CaveCeilingStabilityWeight;
    public float CaveCeilingMoistureClamp;
}

/// <summary>
/// 나무, 산, 바다,강 등 여러 자연적인 형태에 대한 Config 정보.
/// </summary>
public struct WorldEnviromentsConfig
{
    //tree
    public int minTreeBodyLength;
    public int maxTreeBodyLength;
    public int minTreeBranchDepth;
    public int maxTreeBranchDepth;
    //
}

public class WorldConfigFile : BaseDataFile
{
    private WorldConfig Config;
    public static WorldConfigFile Instance = null;
    // Use this for initialization
    public override void Init ()
    {
        Instance = this;
        JsonFile = Resources.Load(ConstFilePath.TXT_RESOURCE_WORLD_CONFIG_DATA) as TextAsset;
        JsonObject = new JSONObject(JsonFile.text);
        AccessData(JsonObject);
    }
    public WorldConfig GetConfig()
    {
        return Config;
    }
    protected override void AccessData(JSONObject jsonObj)
    {
        switch (jsonObj.type)
        {
            case JSONObject.Type.OBJECT:
                //to do
                var data = JsonObject.ToDictionary();
                Config.ChunkLoadIntervalSeconds = ParseFloat(data, "ChunkLoadIntervalSeconds", 0.05f);
                Config.SubWorldSizeX = ParseInt(data, "SubWorldSizeX", 32);
                Config.SubWorldSizeY = ParseInt(data, "SubWorldSizeY", 32);
                Config.SubWorldSizeZ = ParseInt(data, "SubWorldSizeZ", 32);
                Config.OneTileUnit = ParseFloat(data, "OneTileUnit", 0.0625f);
                Config.ChunkSize = ParseInt(data, "ChunkSize", 8);
                Config.RenderDistance = ParseInt(data, "RenderDistance", 10);
                Config.SimulationDistance = ParseInt(data, "SimulationDistance", 8);
                Config.MapControlProfilePath = data.ContainsKey("MapControlProfilePath") ? data["MapControlProfilePath"] : "world-map-control.json";
                Config.MapControlProfileVersion = ParseInt(data, "MapControlProfileVersion", 44);
                Config.SubWorld_Count_X_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_X_Axis_Per_WorldArea", 32);
                Config.SubWorld_Count_Y_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_Y_Axis_Per_WorldArea", 32);
                Config.SubWorld_Count_Z_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_Z_Axis_Per_WorldArea", 32);
                Config.GlobalWaterLevel = ParseInt(data, "GlobalWaterLevel", 62);
                Config.RiverCenterThreshold = ParseFloat(data, "RiverCenterThreshold", 0.0125f);
                Config.RiverBankThreshold = ParseFloat(data, "RiverBankThreshold", 0.028f);
                Config.EnableRivers = ParseBool(data, "EnableRivers", true);
                Config.EnableLakes = ParseBool(data, "EnableLakes", true);
                Config.EnableCaves = ParseBool(data, "EnableCaves", true);
                Config.UseImprovedRivers = ParseBool(data, "UseImprovedRivers", Config.EnableRivers);
                Config.UseImprovedLakes = ParseBool(data, "UseImprovedLakes", Config.EnableLakes);
                Config.UseImprovedCaves = ParseBool(data, "UseImprovedCaves", Config.EnableCaves);
                Config.LakeMinDepth = ParseInt(data, "LakeMinDepth", 3);
                Config.LakeMaxDepth = ParseInt(data, "LakeMaxDepth", 9);
                Config.LakeMaxRadius = ParseInt(data, "LakeMaxRadius", 9);
                Config.LakeBasinSmoothIterations = ParseInt(data, "LakeBasinSmoothIterations", 2);
                Config.LakeShelfDepth = ParseInt(data, "LakeShelfDepth", 2);
                Config.HydrologySmoothIterations = ParseInt(data, "HydrologySmoothIterations", 2);
                Config.HydrologySmoothBlend = ParseFloat(data, "HydrologySmoothBlend", 0.6f);
                Config.HydrologyReservoirIterations = ParseInt(data, "HydrologyReservoirIterations", 2);
                Config.HydrologyReservoirBlend = ParseFloat(data, "HydrologyReservoirBlend", 0.38f);
                Config.HydrologyShorePush = ParseFloat(data, "HydrologyShorePush", 5.0f);
                Config.HydrologySlopePenalty = ParseFloat(data, "HydrologySlopePenalty", 6.0f);
                Config.HydrologyFlowGain = ParseFloat(data, "HydrologyFlowGain", 0.6f);
                Config.HydrologyFlowMemoryWeight = ParseFloat(data, "HydrologyFlowMemoryWeight", 0.45f);
                Config.HydrologyFlowShadowWeight = ParseFloat(data, "HydrologyFlowShadowWeight", 0.52f);
                Config.HydrologyFlowShadowSlopeWeight = ParseFloat(data, "HydrologyFlowShadowSlopeWeight", 0.42f);
                Config.HydrologyContinuityWeight = ParseFloat(data, "HydrologyContinuityWeight", 0.35f);
                Config.HydrologyPressureBlend = ParseFloat(data, "HydrologyPressureBlend", 0.42f);
                Config.HydrologyPressureGradientClamp = ParseFloat(data, "HydrologyPressureGradientClamp", 0.22f);
                Config.HydrologyEdgeFlowBias = ParseFloat(data, "HydrologyEdgeFlowBias", 0.42f);
                Config.HydrologyEdgeTangentWeight = ParseFloat(data, "HydrologyEdgeTangentWeight", 0.45f);
                Config.HydrologyEdgeFlowLockWeight = ParseFloat(data, "HydrologyEdgeFlowLockWeight", 0.44f);
                Config.HydrologyEdgeBlendRadius = ParseInt(data, "HydrologyEdgeBlendRadius", 6);
                Config.HydrologyEdgeStabilityIterations = ParseInt(data, "HydrologyEdgeStabilityIterations", 4);
                Config.HydrologyEdgeStabilityWeight = ParseFloat(data, "HydrologyEdgeStabilityWeight", 0.38f);
                Config.HydrologyEdgeVarianceClamp = ParseFloat(data, "HydrologyEdgeVarianceClamp", 0.3f);
                Config.HydrologyEdgeFluxBlend = ParseFloat(data, "HydrologyEdgeFluxBlend", 0.6f);
                Config.HydrologyVarianceBlend = ParseFloat(data, "HydrologyVarianceBlend", 0.58f);
                Config.HydrologyVarianceClamp = ParseFloat(data, "HydrologyVarianceClamp", 0.62f);
                Config.HydrologyWaterTableClampWeight = ParseFloat(data, "HydrologyWaterTableClampWeight", 0.55f);
                Config.HydrologyWaterTableClampRange = ParseInt(data, "HydrologyWaterTableClampRange", 20);
                Config.HydrologyWaterTableSlopeWeight = ParseFloat(data, "HydrologyWaterTableSlopeWeight", 0.62f);
                Config.HydrologyFlowPersistence = ParseFloat(data, "HydrologyFlowPersistence", 0.75f);
                Config.HydrologyGradientWeight = ParseFloat(data, "HydrologyGradientWeight", 0.35f);
                Config.HydrologyGradientSlopeWeight = ParseFloat(data, "HydrologyGradientSlopeWeight", 0.42f);
                Config.HydrologyGradientClamp = ParseFloat(data, "HydrologyGradientClamp", 1.65f);
                Config.HydrologyGradientStabilityIterations = ParseInt(data, "HydrologyGradientStabilityIterations", 1);
                Config.HydrologyGradientStabilityBlend = ParseFloat(data, "HydrologyGradientStabilityBlend", 0.45f);
                Config.HydrologyDirectionalIterations = ParseInt(data, "HydrologyDirectionalIterations", 1);
                Config.HydrologyDirectionalBlend = ParseFloat(data, "HydrologyDirectionalBlend", 0.42f);
                Config.HydrologyFlowDivergenceClamp = ParseFloat(data, "HydrologyFlowDivergenceClamp", 0.55f);
                Config.HydrologyCurvatureWeight = ParseFloat(data, "HydrologyCurvatureWeight", 0.32f);
                Config.HydrologySeamRelaxIterations = ParseInt(data, "HydrologySeamRelaxIterations", 3);
                Config.HydrologySeamRelaxBlend = ParseFloat(data, "HydrologySeamRelaxBlend", 0.56f);
                Config.HydrologyWarpFrequency = ParseFloat(data, "HydrologyWarpFrequency", 0.0009f);
                Config.HydrologyWarpAmplitude = ParseFloat(data, "HydrologyWarpAmplitude", 9.0f);
                Config.RiparianSmoothIterations = ParseInt(data, "RiparianSmoothIterations", 2);
                Config.RiparianSmoothBlend = ParseFloat(data, "RiparianSmoothBlend", 0.65f);
                Config.RiparianSaturationBoost = ParseFloat(data, "RiparianSaturationBoost", 0.18f);
                Config.RiparianBufferRadius = ParseInt(data, "RiparianBufferRadius", 1);
                Config.RiverNoiseScale = ParseFloat(data, "RiverNoiseScale", 0.015f);
                Config.RiverDepth = ParseInt(data, "RiverDepth", 7);
                Config.RiverIntensitySmoothIterations = ParseInt(data, "RiverIntensitySmoothIterations", 3);
                Config.RiverIntensitySmoothBlend = ParseFloat(data, "RiverIntensitySmoothBlend", 0.6f);
                Config.RiverConfluenceBoost = ParseFloat(data, "RiverConfluenceBoost", 0.5f);
                Config.RiverTributaryCaptureWeight = ParseFloat(data, "RiverTributaryCaptureWeight", 0.46f);
                Config.RiverAvulsionResistance = ParseFloat(data, "RiverAvulsionResistance", 0.52f);
                Config.RiverFlowAlignmentWeight = ParseFloat(data, "RiverFlowAlignmentWeight", 0.32f);
                Config.RiverGradientPenalty = ParseFloat(data, "RiverGradientPenalty", 0.42f);
                Config.RiverHeadwaterStabilityWeight = ParseFloat(data, "RiverHeadwaterStabilityWeight", 0.35f);
                Config.RiverAnisotropyWeight = ParseFloat(data, "RiverAnisotropyWeight", 0.32f);
                Config.RiverMeanderJitter = ParseFloat(data, "RiverMeanderJitter", 0.18f);
                Config.RiverReliefPenaltyWeight = ParseFloat(data, "RiverReliefPenaltyWeight", 0.3f);
                Config.RiverBankErosionWeight = ParseFloat(data, "RiverBankErosionWeight", 0.16f);
                Config.LakeRimErosionWeight = ParseFloat(data, "LakeRimErosionWeight", 0.35f);
                Config.LakeInflowBlendWeight = ParseFloat(data, "LakeInflowBlendWeight", 0.48f);
                Config.RiverEdgeFeather = ParseFloat(data, "RiverEdgeFeather", 0.5f);
                Config.RiverMouthSmoothRadius = ParseInt(data, "RiverMouthSmoothRadius", 5);
                Config.RiverDeltaWetlandStrength = ParseFloat(data, "RiverDeltaWetlandStrength", 0.5f);
                Config.RiverSeamFillStrength = ParseFloat(data, "RiverSeamFillStrength", 0.5f);
                Config.LakeSpawnWeightBias = ParseFloat(data, "LakeSpawnWeightBias", 0.3f);
                Config.LakeShorelineBlend = ParseFloat(data, "LakeShorelineBlend", 0.66f);
                Config.LakeVarianceWeight = ParseFloat(data, "VarianceWeight", 0.34f);
                Config.WetlandSaturationThreshold = ParseFloat(data, "WetlandSaturationThreshold", 0.55f);
                Config.OutflowCarveDepth = ParseInt(data, "OutflowCarveDepth", 2);
                Config.OutflowStabilityWeight = ParseFloat(data, "OutflowStabilityWeight", 0.42f);
                Config.LakeSpillRetentionWeight = ParseFloat(data, "LakeSpillRetentionWeight", 0.58f);
                Config.LakeWetlandBufferRadius = ParseInt(data, "LakeWetlandBufferRadius", 2);
                Config.FlowSeepageWeight = ParseFloat(data, "FlowSeepageWeight", 0.48f);
                Config.RiverProximitySuppression = ParseFloat(data, "RiverProximitySuppression", 0.35f);
                Config.CaveStabilitySmoothIterations = ParseInt(data, "CaveStabilitySmoothIterations", 2);
                Config.CaveStabilitySmoothBlend = ParseFloat(data, "CaveStabilitySmoothBlend", 0.55f);
                Config.CaveSupportDensity = ParseFloat(data, "CaveSupportDensity", 0.6f);
                Config.SupportPillarChance = ParseFloat(data, "SupportPillarChance", 0.28f);
                Config.SupportHydrationBias = ParseFloat(data, "SupportHydrationBias", 0.42f);
                Config.SupportFlowBias = ParseFloat(data, "SupportFlowBias", 0.2f);
                Config.RiparianPlugDepth = ParseInt(data, "RiparianPlugDepth", 2);
                Config.HydrologyStabilityWeight = ParseFloat(data, "HydrologyStabilityWeight", 0.45f);
                Config.FlowStabilityWeight = ParseFloat(data, "FlowStabilityWeight", 0.25f);
                Config.RoughnessStabilityWeight = ParseFloat(data, "RoughnessStabilityWeight", 0.1f);
                Config.RiverSuppressionWeight = ParseFloat(data, "RiverSuppressionWeight", 0.38f);
                Config.MoistureRetentionWeight = ParseFloat(data, "MoistureRetentionWeight", 0.4f);
                Config.GroundwaterConnectivityWeight = ParseFloat(data, "GroundwaterConnectivityWeight", 0.58f);
                Config.CaveVentilationBias = ParseFloat(data, "CaveVentilationBias", 0.42f);
                Config.RiparianCaveGuardWeight = ParseFloat(data, "RiparianCaveGuardWeight", 0.42f);
                Config.EdgeSealStrength = ParseFloat(data, "EdgeSealStrength", 0.52f);
                Config.CaveCeilingStabilityWeight = ParseFloat(data, "CaveCeilingStabilityWeight", 0.35f);
                Config.CaveCeilingMoistureClamp = ParseFloat(data, "CaveCeilingMoistureClamp", 0.38f);
                break;
            case JSONObject.Type.ARRAY:
                break;
            default:
                Debug.Log("Json Level Data Sheet Access ERROR");
                break;
        }

    }

    public void OverrideWithProfile(WorldMapControlProfile profile)
    {
        // Keep Unity-side world config aligned with the authoritative server profile.
        Config.ChunkSize = profile.ChunkSize;
        Config.RenderDistance = profile.RenderDistance;
        Config.SimulationDistance = profile.SimulationDistance;
        Config.MapControlProfileVersion = profile.Version;
        Config.GlobalWaterLevel = profile.GlobalWaterLevel;
        Config.RiverCenterThreshold = profile.RiverCenterThreshold;
        Config.RiverBankThreshold = profile.RiverBankThreshold;
        Config.HydrologySmoothIterations = profile.HydrologySmoothIterations;
        Config.HydrologySmoothBlend = profile.HydrologySmoothBlend;
        Config.HydrologyReservoirIterations = profile.HydrologyReservoirIterations;
        Config.HydrologyReservoirBlend = profile.HydrologyReservoirBlend;
        Config.HydrologyShorePush = profile.HydrologyShorePush;
        Config.HydrologySlopePenalty = profile.HydrologySlopePenalty;
        Config.HydrologyFlowGain = profile.HydrologyFlowGain;
        Config.HydrologyFlowMemoryWeight = profile.HydrologyFlowMemoryWeight;
        Config.HydrologyFlowShadowWeight = profile.HydrologyFlowShadowWeight;
        Config.HydrologyFlowShadowSlopeWeight = profile.HydrologyFlowShadowSlopeWeight;
        Config.HydrologyContinuityWeight = profile.HydrologyContinuityWeight;
        Config.HydrologyPressureBlend = profile.HydrologyPressureBlend;
        Config.HydrologyPressureGradientClamp = profile.HydrologyPressureGradientClamp;
        Config.HydrologyEdgeFlowBias = profile.HydrologyEdgeFlowBias;
        Config.HydrologyEdgeTangentWeight = profile.HydrologyEdgeTangentWeight;
        Config.HydrologyEdgeFlowLockWeight = profile.HydrologyEdgeFlowLockWeight;
        Config.HydrologyEdgeBlendRadius = profile.HydrologyEdgeBlendRadius;
        Config.HydrologyEdgeStabilityIterations = profile.HydrologyEdgeStabilityIterations;
        Config.HydrologyEdgeStabilityWeight = profile.HydrologyEdgeStabilityWeight;
        Config.HydrologyEdgeVarianceClamp = profile.HydrologyEdgeVarianceClamp;
        Config.HydrologyEdgeFluxBlend = profile.HydrologyEdgeFluxBlend;
        Config.HydrologyVarianceBlend = profile.HydrologyVarianceBlend;
        Config.HydrologyVarianceClamp = profile.HydrologyVarianceClamp;
        Config.HydrologyWaterTableClampWeight = profile.HydrologyWaterTableClampWeight;
        Config.HydrologyWaterTableClampRange = profile.HydrologyWaterTableClampRange;
        Config.HydrologyWaterTableSlopeWeight = profile.HydrologyWaterTableSlopeWeight;
        Config.HydrologyFlowPersistence = profile.HydrologyFlowPersistence;
        Config.HydrologyGradientWeight = profile.HydrologyGradientWeight;
        Config.HydrologyGradientSlopeWeight = profile.HydrologyGradientSlopeWeight;
        Config.HydrologyGradientClamp = profile.HydrologyGradientClamp;
        Config.HydrologyGradientStabilityIterations = profile.HydrologyGradientStabilityIterations;
        Config.HydrologyGradientStabilityBlend = profile.HydrologyGradientStabilityBlend;
        Config.HydrologyDirectionalIterations = profile.HydrologyDirectionalIterations;
        Config.HydrologyDirectionalBlend = profile.HydrologyDirectionalBlend;
        Config.HydrologyFlowDivergenceClamp = profile.HydrologyFlowDivergenceClamp;
        Config.HydrologyCurvatureWeight = profile.HydrologyCurvatureWeight;
        Config.HydrologySeamRelaxIterations = profile.HydrologySeamRelaxIterations;
        Config.HydrologySeamRelaxBlend = profile.HydrologySeamRelaxBlend;
        Config.HydrologyWarpFrequency = profile.HydrologyWarpFrequency;
        Config.HydrologyWarpAmplitude = profile.HydrologyWarpAmplitude;
        Config.RiparianSmoothIterations = profile.RiparianSmoothIterations;
        Config.RiparianSmoothBlend = profile.RiparianSmoothBlend;
        Config.RiparianSaturationBoost = profile.RiparianSaturationBoost;
        Config.RiparianBufferRadius = profile.RiparianBufferRadius;
        Config.RiverFlowAlignmentWeight = profile.RiverFlowAlignmentWeight;
        Config.RiverGradientPenalty = profile.RiverGradientPenalty;
        Config.RiverHeadwaterStabilityWeight = profile.RiverHeadwaterStabilityWeight;
        Config.RiverAnisotropyWeight = profile.RiverAnisotropyWeight;
        Config.RiverMeanderJitter = profile.RiverMeanderJitter;
        Config.RiverReliefPenaltyWeight = profile.RiverReliefPenaltyWeight;
        Config.RiverBankErosionWeight = profile.RiverBankErosionWeight;
        Config.LakeRimErosionWeight = profile.LakeRimErosionWeight;
        Config.LakeInflowBlendWeight = profile.LakeInflowBlendWeight;
        Config.LakeSpawnWeightBias = profile.LakeSpawnWeightBias;
        Config.LakeShorelineBlend = profile.LakeShorelineBlend;
        Config.LakeVarianceWeight = profile.LakeVarianceWeight;
        Config.WetlandSaturationThreshold = profile.LakeWetlandSaturationThreshold;
        Config.OutflowCarveDepth = profile.LakeOutflowCarveDepth;
        Config.OutflowStabilityWeight = profile.LakeOutflowStabilityWeight;
        Config.LakeSpillRetentionWeight = profile.LakeSpillRetentionWeight;
        Config.LakeBasinSmoothIterations = profile.LakeBasinSmoothIterations;
        Config.LakeShelfDepth = profile.LakeShelfDepth;
        Config.LakeWetlandBufferRadius = profile.LakeWetlandBufferRadius;
        Config.RiverProximitySuppression = profile.LakeRiverProximitySuppression;
        Config.FlowSeepageWeight = profile.LakeFlowSeepageWeight;
        Config.RiverNoiseScale = profile.RiverNoiseScale;
        Config.RiverDepth = profile.RiverDepth;
        Config.RiverIntensitySmoothIterations = profile.RiverIntensitySmoothIterations;
        Config.RiverIntensitySmoothBlend = profile.RiverIntensitySmoothBlend;
        Config.RiverConfluenceBoost = profile.RiverConfluenceBoost;
        Config.RiverTributaryCaptureWeight = profile.RiverTributaryCaptureWeight;
        Config.RiverAvulsionResistance = profile.RiverAvulsionResistance;
        Config.RiverEdgeFeather = profile.RiverEdgeFeather;
        Config.RiverMouthSmoothRadius = profile.RiverMouthSmoothRadius;
        Config.RiverDeltaWetlandStrength = profile.RiverDeltaWetlandStrength;
        Config.RiverSeamFillStrength = (float)profile.RiverSeamFillStrength;
        Config.CaveStabilitySmoothIterations = profile.CaveStabilitySmoothIterations;
        Config.CaveStabilitySmoothBlend = profile.CaveStabilitySmoothBlend;
        Config.CaveSupportDensity = profile.CaveSupportDensity;
        Config.SupportPillarChance = profile.SupportPillarChance;
        Config.SupportHydrationBias = profile.CaveSupportHydrationBias;
        Config.SupportFlowBias = profile.CaveSupportFlowBias;
        Config.RiparianPlugDepth = profile.CaveRiparianPlugDepth;
        Config.HydrologyStabilityWeight = profile.CaveHydrologyWeight;
        Config.FlowStabilityWeight = profile.CaveFlowWeight;
        Config.RoughnessStabilityWeight = profile.CaveRoughnessWeight;
        Config.RiverSuppressionWeight = profile.CaveRiverSuppressionWeight;
        Config.MoistureRetentionWeight = profile.CaveMoistureRetentionWeight;
        Config.GroundwaterConnectivityWeight = profile.CaveGroundwaterConnectivityWeight;
        Config.CaveVentilationBias = profile.CaveVentilationBias;
        Config.RiparianCaveGuardWeight = profile.RiparianCaveGuardWeight;
        Config.EdgeSealStrength = profile.CaveEdgeSealStrength;
        Config.CaveCeilingStabilityWeight = (float)profile.CaveCeilingStabilityWeight;
        Config.CaveCeilingMoistureClamp = (float)profile.CaveCeilingMoistureClamp;
        Config.EnableRivers = profile.EnableRivers;
        Config.EnableLakes = profile.EnableLakes;
        Config.EnableCaves = profile.EnableCaves;
        Config.UseImprovedRivers = profile.UseImprovedRivers;
        Config.UseImprovedLakes = profile.UseImprovedLakes;
        Config.UseImprovedCaves = profile.UseImprovedCaves;
    }

    private static int ParseInt(Dictionary<string, string> data, string key, int defaultValue)
    {
        string extractedValue;
        if (data.TryGetValue(key, out extractedValue) == true && int.TryParse(extractedValue, out int parsed) == true)
        {
            return parsed;
        }
        return defaultValue;
    }

    private static float ParseFloat(Dictionary<string, string> data, string key, float defaultValue)
    {
        string extractedValue;
        if (data.TryGetValue(key, out extractedValue) == true && float.TryParse(extractedValue, out float parsed) == true)
        {
            return parsed;
        }
        return defaultValue;
    }

    private static bool ParseBool(Dictionary<string, string> data, string key, bool defaultValue)
    {
        string extractedValue;
        if (data.TryGetValue(key, out extractedValue) == true && bool.TryParse(extractedValue, out bool parsed) == true)
        {
            return parsed;
        }
        return defaultValue;
    }
}
