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
    public float ChunkLoadIntervalSeconds;
    public WorldEnviromentsConfig EnviromentsConfig;
    public int GlobalWaterLevel;
    public float RiverCenterThreshold;
    public float RiverBankThreshold;
    public bool EnableRivers;
    public bool EnableLakes;
    public bool EnableCaves;
    public int LakeMinDepth;
    public int LakeMaxDepth;
    public int LakeMaxRadius;
    public int HydrologySmoothIterations;
    public float HydrologySmoothBlend;
    public float HydrologyShorePush;
    public float HydrologySlopePenalty;
    public float HydrologyFlowGain;
    public float HydrologyContinuityWeight;
    public float RiverBankErosionWeight;
    public float LakeRimErosionWeight;
    public float LakeSpawnWeightBias;
    public float LakeShorelineBlend;
    public float RiverNoiseScale;
    public int RiverDepth;
    public int CaveStabilitySmoothIterations;
    public float CaveStabilitySmoothBlend;
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
                Config.SubWorld_Count_X_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_X_Axis_Per_WorldArea", 32);
                Config.SubWorld_Count_Y_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_Y_Axis_Per_WorldArea", 32);
                Config.SubWorld_Count_Z_Axis_Per_WorldArea = ParseInt(data, "SubWorld_Count_Z_Axis_Per_WorldArea", 32);
                Config.GlobalWaterLevel = ParseInt(data, "GlobalWaterLevel", 62);
                Config.RiverCenterThreshold = ParseFloat(data, "RiverCenterThreshold", 0.0125f);
                Config.RiverBankThreshold = ParseFloat(data, "RiverBankThreshold", 0.028f);
                Config.EnableRivers = ParseBool(data, "EnableRivers", true);
                Config.EnableLakes = ParseBool(data, "EnableLakes", true);
                Config.EnableCaves = ParseBool(data, "EnableCaves", true);
                Config.LakeMinDepth = ParseInt(data, "LakeMinDepth", 3);
                Config.LakeMaxDepth = ParseInt(data, "LakeMaxDepth", 9);
                Config.LakeMaxRadius = ParseInt(data, "LakeMaxRadius", 9);
                Config.HydrologySmoothIterations = ParseInt(data, "HydrologySmoothIterations", 1);
                Config.HydrologySmoothBlend = ParseFloat(data, "HydrologySmoothBlend", 0.55f);
                Config.HydrologyShorePush = ParseFloat(data, "HydrologyShorePush", 5.0f);
                Config.HydrologySlopePenalty = ParseFloat(data, "HydrologySlopePenalty", 6.0f);
                Config.HydrologyFlowGain = ParseFloat(data, "HydrologyFlowGain", 0.5f);
                Config.HydrologyContinuityWeight = ParseFloat(data, "HydrologyContinuityWeight", 0.35f);
                Config.RiverNoiseScale = ParseFloat(data, "RiverNoiseScale", 0.015f);
                Config.RiverDepth = ParseInt(data, "RiverDepth", 5);
                Config.RiverBankErosionWeight = ParseFloat(data, "RiverBankErosionWeight", 0.18f);
                Config.LakeRimErosionWeight = ParseFloat(data, "LakeRimErosionWeight", 0.25f);
                Config.LakeSpawnWeightBias = ParseFloat(data, "LakeSpawnWeightBias", 0.3f);
                Config.LakeShorelineBlend = ParseFloat(data, "LakeShorelineBlend", 0.6f);
                Config.CaveStabilitySmoothIterations = ParseInt(data, "CaveStabilitySmoothIterations", 1);
                Config.CaveStabilitySmoothBlend = ParseFloat(data, "CaveStabilitySmoothBlend", 0.55f);
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
