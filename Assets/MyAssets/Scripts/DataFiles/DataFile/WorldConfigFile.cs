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
    public float HydrologyContinuityWeight;
    public float HydrologyEdgeFlowBias;
    public float HydrologyEdgeTangentWeight;
    public float HydrologyEdgeFlowLockWeight;
    public int HydrologyEdgeBlendRadius;
    public int HydrologyEdgeStabilityIterations;
    public float HydrologyEdgeStabilityWeight;
    public float HydrologyEdgeVarianceClamp;
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
    public float HydrologyCurvatureWeight;
    public int HydrologySeamRelaxIterations;
    public float HydrologySeamRelaxBlend;
    public float HydrologyWarpFrequency;
    public float HydrologyWarpAmplitude;
    public float RiverFlowAlignmentWeight;
    public float RiverGradientPenalty;
    public float RiverHeadwaterStabilityWeight;
    public float RiverAnisotropyWeight;
    public float RiverReliefPenaltyWeight;
    public float RiverBankErosionWeight;
    public float LakeRimErosionWeight;
    public float LakeInflowBlendWeight;
    public float LakeSpawnWeightBias;
    public float LakeShorelineBlend;
    public float RiverProximitySuppression;
    public float RiverNoiseScale;
    public int RiverDepth;
    public int RiverIntensitySmoothIterations;
    public float RiverIntensitySmoothBlend;
    public float RiverConfluenceBoost;
    public int CaveStabilitySmoothIterations;
    public float CaveStabilitySmoothBlend;
    public float CaveSupportDensity;
    public float SupportHydrationBias;
    public float SupportFlowBias;
    public float HydrologyStabilityWeight;
    public float FlowStabilityWeight;
    public float RoughnessStabilityWeight;
    public float RiverSuppressionWeight;
    public float MoistureRetentionWeight;
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
                Config.HydrologySmoothIterations = ParseInt(data, "HydrologySmoothIterations", 2);
                Config.HydrologySmoothBlend = ParseFloat(data, "HydrologySmoothBlend", 0.6f);
                Config.HydrologyShorePush = ParseFloat(data, "HydrologyShorePush", 5.0f);
                Config.HydrologySlopePenalty = ParseFloat(data, "HydrologySlopePenalty", 6.0f);
                Config.HydrologyFlowGain = ParseFloat(data, "HydrologyFlowGain", 0.5f);
                Config.HydrologyContinuityWeight = ParseFloat(data, "HydrologyContinuityWeight", 0.35f);
                Config.HydrologyEdgeFlowBias = ParseFloat(data, "HydrologyEdgeFlowBias", 0.35f);
                Config.HydrologyEdgeTangentWeight = ParseFloat(data, "HydrologyEdgeTangentWeight", 0.45f);
                Config.HydrologyEdgeFlowLockWeight = ParseFloat(data, "HydrologyEdgeFlowLockWeight", 0.38f);
                Config.HydrologyEdgeBlendRadius = ParseInt(data, "HydrologyEdgeBlendRadius", 3);
                Config.HydrologyEdgeStabilityIterations = ParseInt(data, "HydrologyEdgeStabilityIterations", 1);
                Config.HydrologyEdgeStabilityWeight = ParseFloat(data, "HydrologyEdgeStabilityWeight", 0.32f);
                Config.HydrologyEdgeVarianceClamp = ParseFloat(data, "HydrologyEdgeVarianceClamp", 0.32f);
                Config.HydrologyVarianceBlend = ParseFloat(data, "HydrologyVarianceBlend", 0.55f);
                Config.HydrologyVarianceClamp = ParseFloat(data, "HydrologyVarianceClamp", 0.65f);
                Config.HydrologyWaterTableClampWeight = ParseFloat(data, "HydrologyWaterTableClampWeight", 0.42f);
                Config.HydrologyWaterTableClampRange = ParseInt(data, "HydrologyWaterTableClampRange", 18);
                Config.HydrologyWaterTableSlopeWeight = ParseFloat(data, "HydrologyWaterTableSlopeWeight", 0.55f);
                Config.HydrologyFlowPersistence = ParseFloat(data, "HydrologyFlowPersistence", 0.68f);
                Config.HydrologyGradientWeight = ParseFloat(data, "HydrologyGradientWeight", 0.35f);
                Config.HydrologyGradientSlopeWeight = ParseFloat(data, "HydrologyGradientSlopeWeight", 0.42f);
                Config.HydrologyGradientClamp = ParseFloat(data, "HydrologyGradientClamp", 1.65f);
                Config.HydrologyGradientStabilityIterations = ParseInt(data, "HydrologyGradientStabilityIterations", 1);
                Config.HydrologyGradientStabilityBlend = ParseFloat(data, "HydrologyGradientStabilityBlend", 0.45f);
                Config.HydrologyCurvatureWeight = ParseFloat(data, "HydrologyCurvatureWeight", 0.32f);
                Config.HydrologySeamRelaxIterations = ParseInt(data, "HydrologySeamRelaxIterations", 2);
                Config.HydrologySeamRelaxBlend = ParseFloat(data, "HydrologySeamRelaxBlend", 0.5f);
                Config.HydrologyWarpFrequency = ParseFloat(data, "HydrologyWarpFrequency", 0.0009f);
                Config.HydrologyWarpAmplitude = ParseFloat(data, "HydrologyWarpAmplitude", 9.0f);
                Config.RiverNoiseScale = ParseFloat(data, "RiverNoiseScale", 0.015f);
                Config.RiverDepth = ParseInt(data, "RiverDepth", 6);
                Config.RiverIntensitySmoothIterations = ParseInt(data, "RiverIntensitySmoothIterations", 3);
                Config.RiverIntensitySmoothBlend = ParseFloat(data, "RiverIntensitySmoothBlend", 0.58f);
                Config.RiverConfluenceBoost = ParseFloat(data, "RiverConfluenceBoost", 0.35f);
                Config.RiverFlowAlignmentWeight = ParseFloat(data, "RiverFlowAlignmentWeight", 0.28f);
                Config.RiverGradientPenalty = ParseFloat(data, "RiverGradientPenalty", 0.42f);
                Config.RiverHeadwaterStabilityWeight = ParseFloat(data, "RiverHeadwaterStabilityWeight", 0.35f);
                Config.RiverAnisotropyWeight = ParseFloat(data, "RiverAnisotropyWeight", 0.32f);
                Config.RiverReliefPenaltyWeight = ParseFloat(data, "RiverReliefPenaltyWeight", 0.25f);
                Config.RiverBankErosionWeight = ParseFloat(data, "RiverBankErosionWeight", 0.18f);
                Config.LakeRimErosionWeight = ParseFloat(data, "LakeRimErosionWeight", 0.3f);
                Config.LakeInflowBlendWeight = ParseFloat(data, "LakeInflowBlendWeight", 0.42f);
                Config.LakeSpawnWeightBias = ParseFloat(data, "LakeSpawnWeightBias", 0.3f);
                Config.LakeShorelineBlend = ParseFloat(data, "LakeShorelineBlend", 0.66f);
                Config.RiverProximitySuppression = ParseFloat(data, "RiverProximitySuppression", 0.35f);
                Config.CaveStabilitySmoothIterations = ParseInt(data, "CaveStabilitySmoothIterations", 1);
                Config.CaveStabilitySmoothBlend = ParseFloat(data, "CaveStabilitySmoothBlend", 0.55f);
                Config.CaveSupportDensity = ParseFloat(data, "CaveSupportDensity", 0.6f);
                Config.SupportHydrationBias = ParseFloat(data, "SupportHydrationBias", 0.42f);
                Config.SupportFlowBias = ParseFloat(data, "SupportFlowBias", 0.2f);
                Config.HydrologyStabilityWeight = ParseFloat(data, "HydrologyStabilityWeight", 0.45f);
                Config.FlowStabilityWeight = ParseFloat(data, "FlowStabilityWeight", 0.25f);
                Config.RoughnessStabilityWeight = ParseFloat(data, "RoughnessStabilityWeight", 0.1f);
                Config.RiverSuppressionWeight = ParseFloat(data, "RiverSuppressionWeight", 0.35f);
                Config.MoistureRetentionWeight = ParseFloat(data, "MoistureRetentionWeight", 0.35f);
                break;
            case JSONObject.Type.ARRAY:
                break;
            default:
                Debug.Log("Json Level Data Sheet Access ERROR");
                break;
        }

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
