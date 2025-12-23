using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class WorldMapControlProfileData
{
    public int version;
    public string profileHash;
    public string sourceConfig;
    public string generatedAtUtc;

    public int chunkSize;
    public int renderDistance;
    public int simulationDistance;
    public int globalWaterLevel;
    public int hydrologyGradientStabilityIterations;
    public float hydrologyGradientStabilityBlend;
    public float hydrologyCurvatureWeight;
    public int hydrologyEdgeBlendRadius;
    public float hydrologyVarianceBlend;
    public float hydrologyVarianceClamp;
    public int hydrologySeamRelaxIterations;
    public float hydrologyEdgeFluxBlend;
    public float hydrologySmoothBlend;
    public int hydrologySmoothIterations;
    public float riverCenterThreshold;
    public float riverBankThreshold;
    public int riverDepth;
    public float riverEdgeFeather;
    public int riverMouthSmoothRadius;
    public float riverDeltaWetlandStrength;
    public float lakeSpawnWeightBias;
    public float lakeShorelineBlend;
    public float lakeWetlandSaturationThreshold;
    public int lakeOutflowCarveDepth;
    public float caveEdgeSealStrength;
    public float supportPillarChance;
    public bool enableRivers;
    public bool enableLakes;
    public bool enableCaves;
    public bool useImprovedRivers;
    public bool useImprovedLakes;
    public bool useImprovedCaves;
    public int lakeBasinSmoothIterations;
}

public sealed class WorldMapControlProfile
{
    public int Version { get; private set; }
    public string ProfileHash { get; private set; } = string.Empty;
    public string SourceConfig { get; private set; } = string.Empty;
    public string GeneratedAtUtc { get; private set; } = string.Empty;

    public int ChunkSize { get; private set; }
    public int RenderDistance { get; private set; }
    public int SimulationDistance { get; private set; }
    public int GlobalWaterLevel { get; private set; }
    public int HydrologyGradientStabilityIterations { get; private set; }
    public float HydrologyGradientStabilityBlend { get; private set; }
    public float HydrologyCurvatureWeight { get; private set; }
    public int HydrologyEdgeBlendRadius { get; private set; }
    public int HydrologySeamRelaxIterations { get; private set; }
    public float HydrologyVarianceBlend { get; private set; }
    public float HydrologyVarianceClamp { get; private set; }
    public float HydrologyEdgeFluxBlend { get; private set; }
    public float HydrologySmoothBlend { get; private set; }
    public int HydrologySmoothIterations { get; private set; }
    public float RiverCenterThreshold { get; private set; }
    public float RiverBankThreshold { get; private set; }
    public int RiverDepth { get; private set; }
    public float RiverEdgeFeather { get; private set; }
    public int RiverMouthSmoothRadius { get; private set; }
    public float RiverDeltaWetlandStrength { get; private set; }
    public float LakeSpawnWeightBias { get; private set; }
    public float LakeShorelineBlend { get; private set; }
    public float LakeWetlandSaturationThreshold { get; private set; }
    public int LakeOutflowCarveDepth { get; private set; }
    public float CaveEdgeSealStrength { get; private set; }
    public float SupportPillarChance { get; private set; }
    public int LakeBasinSmoothIterations { get; private set; }
    public bool EnableRivers { get; private set; }
    public bool EnableLakes { get; private set; }
    public bool EnableCaves { get; private set; }
    public bool UseImprovedRivers { get; private set; }
    public bool UseImprovedLakes { get; private set; }
    public bool UseImprovedCaves { get; private set; }

    private WorldMapControlProfile() { }

    public static WorldMapControlProfile LoadFromFile(string path, WorldConfig fallback)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<WorldMapControlProfileData>(json);
                var profile = FromData(data);
                var provided = string.IsNullOrWhiteSpace(data.profileHash) ? "(empty)" : data.profileHash;
                if (!string.Equals(provided, profile.ProfileHash, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[WorldMapControlProfile] Hash mismatch for '{path}': provided={provided}, computed={profile.ProfileHash}");
                }
                return profile;
            }

            Debug.LogWarning($"[WorldMapControlProfile] Profile file not found at '{path}', falling back to world config.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WorldMapControlProfile] Failed to read '{path}': {ex.Message}. Falling back to world config.");
        }

        return FromConfig(fallback);
    }

    public static WorldMapControlProfile FromConfig(WorldConfig config)
    {
        var data = new WorldMapControlProfileData
        {
            version = config.MapControlProfileVersion > 0 ? config.MapControlProfileVersion : 1,
            sourceConfig = string.IsNullOrEmpty(config.MapControlProfilePath) ? "WorldConfigData.json" : config.MapControlProfilePath,
            generatedAtUtc = DateTime.UtcNow.ToString("o"),
            chunkSize = Mathf.Max(1, config.ChunkSize),
            renderDistance = Mathf.Max(1, config.RenderDistance),
            simulationDistance = Mathf.Max(1, config.SimulationDistance),
            globalWaterLevel = config.GlobalWaterLevel,
            hydrologyGradientStabilityIterations = Mathf.Max(0, config.HydrologyGradientStabilityIterations),
            hydrologyGradientStabilityBlend = Mathf.Clamp01(config.HydrologyGradientStabilityBlend),
            hydrologyCurvatureWeight = Mathf.Clamp(config.HydrologyCurvatureWeight, 0f, 1.5f),
            hydrologyEdgeBlendRadius = Mathf.Max(1, config.HydrologyEdgeBlendRadius),
            hydrologyVarianceBlend = Mathf.Clamp01(config.HydrologyVarianceBlend),
            hydrologyVarianceClamp = Mathf.Clamp(config.HydrologyVarianceClamp, 0f, 1.25f),
            hydrologySeamRelaxIterations = Mathf.Max(0, config.HydrologySeamRelaxIterations),
            hydrologyEdgeFluxBlend = Mathf.Clamp01(config.HydrologyEdgeFluxBlend),
            hydrologySmoothBlend = Mathf.Clamp01(config.HydrologySmoothBlend),
            hydrologySmoothIterations = Mathf.Max(0, config.HydrologySmoothIterations),
            riverCenterThreshold = config.RiverCenterThreshold,
            riverBankThreshold = config.RiverBankThreshold,
            riverDepth = Mathf.Max(1, config.RiverDepth),
            riverEdgeFeather = Mathf.Clamp01(config.RiverEdgeFeather),
            riverMouthSmoothRadius = Mathf.Max(1, config.RiverMouthSmoothRadius),
            riverDeltaWetlandStrength = Mathf.Clamp01(config.RiverDeltaWetlandStrength),
            lakeSpawnWeightBias = Mathf.Clamp(config.LakeSpawnWeightBias, 0f, 1.5f),
            lakeShorelineBlend = Mathf.Clamp01(config.LakeShorelineBlend),
            lakeWetlandSaturationThreshold = Mathf.Clamp01(config.WetlandSaturationThreshold),
            lakeOutflowCarveDepth = Mathf.Max(1, config.OutflowCarveDepth),
            caveEdgeSealStrength = Mathf.Clamp01(config.EdgeSealStrength),
            supportPillarChance = Mathf.Clamp01(config.SupportPillarChance),
            enableRivers = config.EnableRivers,
            enableLakes = config.EnableLakes,
            enableCaves = config.EnableCaves,
            useImprovedRivers = config.UseImprovedRivers,
            useImprovedLakes = config.UseImprovedLakes,
            useImprovedCaves = config.UseImprovedCaves,
            lakeBasinSmoothIterations = Mathf.Max(0, config.LakeBasinSmoothIterations)
        };

        data.profileHash = ComputeHash(data);
        return FromData(data);
    }

    private static WorldMapControlProfile FromData(WorldMapControlProfileData data)
    {
        var computedHash = ComputeHash(data);
        return new WorldMapControlProfile
        {
            Version = data.version,
            ProfileHash = computedHash,
            SourceConfig = string.IsNullOrEmpty(data.sourceConfig) ? "unknown" : data.sourceConfig,
            GeneratedAtUtc = string.IsNullOrEmpty(data.generatedAtUtc) ? DateTime.UtcNow.ToString("o") : data.generatedAtUtc,
            ChunkSize = data.chunkSize,
            RenderDistance = data.renderDistance,
            SimulationDistance = data.simulationDistance,
            GlobalWaterLevel = data.globalWaterLevel,
            HydrologyGradientStabilityIterations = data.hydrologyGradientStabilityIterations,
            HydrologyGradientStabilityBlend = data.hydrologyGradientStabilityBlend,
            HydrologyCurvatureWeight = data.hydrologyCurvatureWeight,
            HydrologyEdgeBlendRadius = data.hydrologyEdgeBlendRadius,
            HydrologyVarianceBlend = data.hydrologyVarianceBlend,
            HydrologyVarianceClamp = data.hydrologyVarianceClamp,
            HydrologySeamRelaxIterations = data.hydrologySeamRelaxIterations,
            HydrologyEdgeFluxBlend = data.hydrologyEdgeFluxBlend,
            HydrologySmoothBlend = data.hydrologySmoothBlend,
            HydrologySmoothIterations = data.hydrologySmoothIterations,
            RiverCenterThreshold = data.riverCenterThreshold,
            RiverBankThreshold = data.riverBankThreshold,
            RiverDepth = data.riverDepth,
            RiverEdgeFeather = data.riverEdgeFeather,
            RiverMouthSmoothRadius = data.riverMouthSmoothRadius,
            RiverDeltaWetlandStrength = data.riverDeltaWetlandStrength,
            LakeSpawnWeightBias = data.lakeSpawnWeightBias,
            LakeShorelineBlend = data.lakeShorelineBlend,
            LakeWetlandSaturationThreshold = data.lakeWetlandSaturationThreshold,
            LakeOutflowCarveDepth = data.lakeOutflowCarveDepth,
            CaveEdgeSealStrength = data.caveEdgeSealStrength,
            SupportPillarChance = data.supportPillarChance,
            LakeBasinSmoothIterations = data.lakeBasinSmoothIterations,
            EnableRivers = data.enableRivers,
            EnableLakes = data.enableLakes,
            EnableCaves = data.enableCaves,
            UseImprovedRivers = data.useImprovedRivers,
            UseImprovedLakes = data.useImprovedLakes,
            UseImprovedCaves = data.useImprovedCaves
        };
    }

    private static string ComputeHash(WorldMapControlProfileData data)
    {
        var builder = new StringBuilder();
        builder
            .Append(data.version).Append('|')
            .Append(data.chunkSize).Append('|')
            .Append(data.renderDistance).Append('|')
            .Append(data.simulationDistance).Append('|')
            .Append(data.globalWaterLevel).Append('|')
            .Append(data.hydrologyGradientStabilityIterations).Append('|')
            .Append(data.hydrologyGradientStabilityBlend).Append('|')
            .Append(data.hydrologyCurvatureWeight).Append('|')
            .Append(data.hydrologyEdgeBlendRadius).Append('|')
            .Append(data.hydrologyVarianceBlend).Append('|')
            .Append(data.hydrologyVarianceClamp).Append('|')
            .Append(data.hydrologySeamRelaxIterations).Append('|')
            .Append(data.hydrologyEdgeFluxBlend).Append('|')
            .Append(data.hydrologySmoothBlend).Append('|')
            .Append(data.hydrologySmoothIterations).Append('|')
            .Append(data.riverCenterThreshold).Append('|')
            .Append(data.riverBankThreshold).Append('|')
            .Append(data.riverDepth).Append('|')
            .Append(data.riverEdgeFeather).Append('|')
            .Append(data.riverMouthSmoothRadius).Append('|')
            .Append(data.riverDeltaWetlandStrength).Append('|')
            .Append(data.lakeSpawnWeightBias).Append('|')
            .Append(data.lakeShorelineBlend).Append('|')
            .Append(data.lakeWetlandSaturationThreshold).Append('|')
            .Append(data.lakeOutflowCarveDepth).Append('|')
            .Append(data.caveEdgeSealStrength).Append('|')
            .Append(data.supportPillarChance).Append('|')
            .Append(data.enableRivers).Append('|')
            .Append(data.enableLakes).Append('|')
            .Append(data.enableCaves).Append('|')
            .Append(data.useImprovedCaves).Append('|')
            .Append(data.useImprovedRivers).Append('|')
            .Append(data.useImprovedLakes).Append('|')
            .Append(data.lakeBasinSmoothIterations);

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
