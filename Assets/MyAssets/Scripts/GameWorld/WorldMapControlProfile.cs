using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GameCommon.World;
using UnityEngine;

[Serializable]
public class WorldMapControlProfileData
{
    public int version;
    public string profileHash;
    public string sourceConfig;
    public string generatedAtUtc;
    public string hydrologySignature;

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
    public float hydrologySeamRelaxBlend;
    public float hydrologyEdgeFluxBlend;
    public float hydrologyEdgeVarianceClamp;
    public float hydrologySmoothBlend;
    public int hydrologySmoothIterations;
    public int hydrologyReservoirIterations;
    public float hydrologyReservoirBlend;
    public float hydrologyShorePush;
    public float hydrologySlopePenalty;
    public float hydrologyFlowGain;
    public float hydrologyFlowShadowWeight;
    public float hydrologyFlowShadowSlopeWeight;
    public float hydrologyEdgeNormalizationBlend;
    public int hydrologyEdgeNormalizationIterations;
    public float hydrologyFlowMemoryWeight;
    public float hydrologyContinuityWeight;
    public float hydrologyThalwegStabilityWeight;
    public float hydrologyPressureBlend;
    public float hydrologyPressureGradientClamp;
    public float hydrologyEdgeFlowBias;
    public float hydrologyEdgeTangentWeight;
    public float hydrologyEdgeFlowLockWeight;
    public int hydrologyEdgeStabilityIterations;
    public float hydrologyEdgeStabilityWeight;
    public float hydrologyWaterTableClampWeight;
    public int hydrologyWaterTableClampRange;
    public float hydrologyWaterTableSlopeWeight;
    public float hydrologyFlowPersistence;
    public float hydrologyGradientWeight;
    public float hydrologyGradientSlopeWeight;
    public float hydrologyGradientClamp;
    public int hydrologyDirectionalIterations;
    public float hydrologyDirectionalBlend;
    public float hydrologyFlowDivergenceClamp;
    public float hydrologyWarpFrequency;
    public float hydrologyWarpAmplitude;
    public int riparianSmoothIterations;
    public float riparianSmoothBlend;
    public float riparianSaturationBoost;
    public int riparianBufferRadius;
    public float riverCenterThreshold;
    public float riverBankThreshold;
    public int riverDepth;
    public float riverNoiseScale;
    public int riverIntensitySmoothIterations;
    public float riverIntensitySmoothBlend;
    public float riverConfluenceBoost;
    public float riverTributaryCaptureWeight;
    public float riverAvulsionResistance;
    public float riverFlowAlignmentWeight;
    public float riverGradientPenalty;
    public float riverHeadwaterStabilityWeight;
    public float riverAnisotropyWeight;
    public float riverAnisotropyDamping;
    public float riverMeanderJitter;
    public float riverReliefPenaltyWeight;
    public float riverBankStabilityClamp;
    public float riverEdgeFeather;
    public float riverEdgeContinuityWeight;
    public int riverMouthSmoothRadius;
    public float riverDeltaWetlandStrength;
    public float riverSeamFillStrength;
    public float riverBankErosionWeight;
    public float lakeSpawnWeightBias;
    public float lakeShorelineBlend;
    public float lakeWetlandSaturationThreshold;
    public int lakeOutflowCarveDepth;
    public int lakeBasinSmoothIterations;
    public int lakeShelfDepth;
    public int lakeMaxRadius;
    public int lakeWetlandBufferRadius;
    public float lakeRiverProximitySuppression;
    public float lakeInflowBlendWeight;
    public float lakeRimErosionWeight;
    public float lakeOutflowSealWeight;
    public float lakeFlowSeepageWeight;
    public float lakeVarianceWeight;
    public float lakeOutflowStabilityWeight;
    public float lakeOutflowTaper;
    public float lakeSpillRetentionWeight;
    public float caveEdgeSealStrength;
    public float supportPillarChance;
    public int caveStabilitySmoothIterations;
    public float caveStabilitySmoothBlend;
    public float caveSupportDensity;
    public float caveSupportHydrationBias;
    public float caveSupportFlowBias;
    public float caveMoistureRetentionWeight;
    public float caveMoistureFlowClamp;
    public float caveEntranceFlowDampening;
    public float caveGroundwaterConnectivityWeight;
    public float caveVentilationBias;
    public int caveRiparianPlugDepth;
    public float caveCeilingStabilityWeight;
    public float caveCeilingMoistureClamp;
    public float caveHydrologyWeight;
    public float caveFlowWeight;
    public float caveRoughnessWeight;
    public float caveDepthWeight;
    public float caveRiverSuppressionWeight;
    public float riparianCaveGuardWeight;
    public bool enableRivers;
    public bool enableLakes;
    public bool enableCaves;
    public bool useImprovedRivers;
    public bool useImprovedLakes;
    public bool useImprovedCaves;
}

public sealed class WorldMapControlProfile
{
    public int Version { get; private set; }
    public string ProfileHash { get; private set; } = string.Empty;
    public string SourceConfig { get; private set; } = string.Empty;
    public string GeneratedAtUtc { get; private set; } = string.Empty;
    public string HydrologySignature { get; private set; } = string.Empty;

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
    public float HydrologySeamRelaxBlend { get; private set; }
    public float HydrologyEdgeFluxBlend { get; private set; }
    public float HydrologyEdgeVarianceClamp { get; private set; }
    public float HydrologySmoothBlend { get; private set; }
    public int HydrologySmoothIterations { get; private set; }
    public int HydrologyReservoirIterations { get; private set; }
    public float HydrologyReservoirBlend { get; private set; }
    public float HydrologyShorePush { get; private set; }
    public float HydrologySlopePenalty { get; private set; }
    public float HydrologyFlowGain { get; private set; }
    public float HydrologyFlowShadowWeight { get; private set; }
    public float HydrologyFlowShadowSlopeWeight { get; private set; }
    public float HydrologyEdgeNormalizationBlend { get; private set; }
    public int HydrologyEdgeNormalizationIterations { get; private set; }
    public float HydrologyFlowMemoryWeight { get; private set; }
    public float HydrologyContinuityWeight { get; private set; }
    public float HydrologyThalwegStabilityWeight { get; private set; }
    public float HydrologyPressureBlend { get; private set; }
    public float HydrologyPressureGradientClamp { get; private set; }
    public float HydrologyEdgeFlowBias { get; private set; }
    public float HydrologyEdgeTangentWeight { get; private set; }
    public float HydrologyEdgeFlowLockWeight { get; private set; }
    public int HydrologyEdgeStabilityIterations { get; private set; }
    public float HydrologyEdgeStabilityWeight { get; private set; }
    public float HydrologyWaterTableClampWeight { get; private set; }
    public int HydrologyWaterTableClampRange { get; private set; }
    public float HydrologyWaterTableSlopeWeight { get; private set; }
    public float HydrologyFlowPersistence { get; private set; }
    public float HydrologyGradientWeight { get; private set; }
    public float HydrologyGradientSlopeWeight { get; private set; }
    public float HydrologyGradientClamp { get; private set; }
    public int HydrologyDirectionalIterations { get; private set; }
    public float HydrologyDirectionalBlend { get; private set; }
    public float HydrologyFlowDivergenceClamp { get; private set; }
    public float HydrologyWarpFrequency { get; private set; }
    public float HydrologyWarpAmplitude { get; private set; }
    public int RiparianSmoothIterations { get; private set; }
    public float RiparianSmoothBlend { get; private set; }
    public float RiparianSaturationBoost { get; private set; }
    public int RiparianBufferRadius { get; private set; }
    public float RiverCenterThreshold { get; private set; }
    public float RiverBankThreshold { get; private set; }
    public int RiverDepth { get; private set; }
    public float RiverNoiseScale { get; private set; }
    public int RiverIntensitySmoothIterations { get; private set; }
    public float RiverIntensitySmoothBlend { get; private set; }
    public float RiverConfluenceBoost { get; private set; }
    public float RiverTributaryCaptureWeight { get; private set; }
    public float RiverAvulsionResistance { get; private set; }
    public float RiverFlowAlignmentWeight { get; private set; }
    public float RiverGradientPenalty { get; private set; }
    public float RiverHeadwaterStabilityWeight { get; private set; }
    public float RiverAnisotropyWeight { get; private set; }
    public float RiverAnisotropyDamping { get; private set; }
    public float RiverMeanderJitter { get; private set; }
    public float RiverReliefPenaltyWeight { get; private set; }
    public float RiverBankStabilityClamp { get; private set; }
    public float RiverEdgeFeather { get; private set; }
    public float RiverEdgeContinuityWeight { get; private set; }
    public int RiverMouthSmoothRadius { get; private set; }
    public float RiverDeltaWetlandStrength { get; private set; }
    public float RiverSeamFillStrength { get; private set; }
    public float RiverBankErosionWeight { get; private set; }
    public float LakeSpawnWeightBias { get; private set; }
    public float LakeShorelineBlend { get; private set; }
    public float LakeWetlandSaturationThreshold { get; private set; }
    public int LakeOutflowCarveDepth { get; private set; }
    public int LakeBasinSmoothIterations { get; private set; }
    public int LakeShelfDepth { get; private set; }
    public int LakeMaxRadius { get; private set; }
    public int LakeWetlandBufferRadius { get; private set; }
    public float LakeRiverProximitySuppression { get; private set; }
    public float LakeInflowBlendWeight { get; private set; }
    public float LakeRimErosionWeight { get; private set; }
    public float LakeOutflowSealWeight { get; private set; }
    public float LakeFlowSeepageWeight { get; private set; }
    public float LakeVarianceWeight { get; private set; }
    public float LakeOutflowStabilityWeight { get; private set; }
    public float LakeOutflowTaper { get; private set; }
    public float LakeSpillRetentionWeight { get; private set; }
    public float CaveEdgeSealStrength { get; private set; }
    public float SupportPillarChance { get; private set; }
    public int CaveStabilitySmoothIterations { get; private set; }
    public float CaveStabilitySmoothBlend { get; private set; }
    public float CaveSupportDensity { get; private set; }
    public float CaveSupportHydrationBias { get; private set; }
    public float CaveSupportFlowBias { get; private set; }
    public float CaveMoistureRetentionWeight { get; private set; }
    public float CaveMoistureFlowClamp { get; private set; }
    public float CaveEntranceFlowDampening { get; private set; }
    public float CaveGroundwaterConnectivityWeight { get; private set; }
    public float CaveVentilationBias { get; private set; }
    public int CaveRiparianPlugDepth { get; private set; }
    public float CaveCeilingStabilityWeight { get; private set; }
    public float CaveCeilingMoistureClamp { get; private set; }
    public float CaveHydrologyWeight { get; private set; }
    public float CaveFlowWeight { get; private set; }
    public float CaveRoughnessWeight { get; private set; }
    public float CaveDepthWeight { get; private set; }
    public float CaveRiverSuppressionWeight { get; private set; }
    public float RiparianCaveGuardWeight { get; private set; }
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
                    Debug.LogWarning($"[WorldMapControlProfile] Hash mismatch for '{path}': provided={provided}, computed={profile.ProfileHash}. Falling back to world config to avoid map-control drift.");
                    return FromConfig(fallback);
                }

                if (!string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[WorldMapControlProfile] Hydrology signature mismatch for '{path}' (profile={profile.HydrologySignature}, shared={SharedFeatureCatalog.HydrologySignature}). Falling back to world config.");
                    return FromConfig(fallback);
                }

                int requiredVersion = Math.Max(fallback.MapControlProfileVersion, SharedFeatureCatalog.MapControlProfileVersion);
                if (profile.Version < requiredVersion)
                {
                    Debug.LogWarning($"[WorldMapControlProfile] Profile version {profile.Version} older than required version {requiredVersion}. Falling back to world config.");
                    return FromConfig(fallback);
                }

                Debug.Log($"[WorldMapControlProfile] Loaded '{path}' v{profile.Version} chunk={profile.ChunkSize}, render={profile.RenderDistance}, sim={profile.SimulationDistance}, water={profile.GlobalWaterLevel}, hash={profile.ProfileHash}");
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

    public static void SaveToFile(WorldMapControlProfile profile, string path)
    {
        if (profile == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = ToData(profile);
            data.profileHash = ComputeHash(data);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Debug.Log($"[WorldMapControlProfile] Saved profile to '{path}' (v{data.version}, hash={data.profileHash})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WorldMapControlProfile] Failed to write '{path}': {ex.Message}");
        }
    }

    private static WorldMapControlProfileData ToData(WorldMapControlProfile profile)
    {
        return new WorldMapControlProfileData
        {
            version = profile.Version,
            profileHash = string.IsNullOrWhiteSpace(profile.ProfileHash) ? "(computed)" : profile.ProfileHash,
            sourceConfig = profile.SourceConfig,
            generatedAtUtc = string.IsNullOrWhiteSpace(profile.GeneratedAtUtc) ? DateTime.UtcNow.ToString("o") : profile.GeneratedAtUtc,
            hydrologySignature = string.IsNullOrWhiteSpace(profile.HydrologySignature) ? SharedFeatureCatalog.HydrologySignature : profile.HydrologySignature,
            chunkSize = profile.ChunkSize,
            renderDistance = profile.RenderDistance,
            simulationDistance = profile.SimulationDistance,
            globalWaterLevel = profile.GlobalWaterLevel,
            hydrologyGradientStabilityIterations = profile.HydrologyGradientStabilityIterations,
            hydrologyGradientStabilityBlend = profile.HydrologyGradientStabilityBlend,
            hydrologyCurvatureWeight = profile.HydrologyCurvatureWeight,
            hydrologyEdgeBlendRadius = profile.HydrologyEdgeBlendRadius,
            hydrologyVarianceBlend = profile.HydrologyVarianceBlend,
            hydrologyVarianceClamp = profile.HydrologyVarianceClamp,
            hydrologySeamRelaxIterations = profile.HydrologySeamRelaxIterations,
            hydrologySeamRelaxBlend = profile.HydrologySeamRelaxBlend,
            hydrologyEdgeFluxBlend = profile.HydrologyEdgeFluxBlend,
            hydrologyEdgeVarianceClamp = profile.HydrologyEdgeVarianceClamp,
            hydrologySmoothBlend = profile.HydrologySmoothBlend,
            hydrologySmoothIterations = profile.HydrologySmoothIterations,
            hydrologyReservoirIterations = profile.HydrologyReservoirIterations,
            hydrologyReservoirBlend = profile.HydrologyReservoirBlend,
            hydrologyShorePush = profile.HydrologyShorePush,
            hydrologySlopePenalty = profile.HydrologySlopePenalty,
            hydrologyFlowGain = profile.HydrologyFlowGain,
            hydrologyFlowShadowWeight = profile.HydrologyFlowShadowWeight,
            hydrologyFlowShadowSlopeWeight = profile.HydrologyFlowShadowSlopeWeight,
            hydrologyEdgeNormalizationBlend = profile.HydrologyEdgeNormalizationBlend,
            hydrologyEdgeNormalizationIterations = profile.HydrologyEdgeNormalizationIterations,
            hydrologyFlowMemoryWeight = profile.HydrologyFlowMemoryWeight,
            hydrologyContinuityWeight = profile.HydrologyContinuityWeight,
            hydrologyThalwegStabilityWeight = profile.HydrologyThalwegStabilityWeight,
            hydrologyPressureBlend = profile.HydrologyPressureBlend,
            hydrologyPressureGradientClamp = profile.HydrologyPressureGradientClamp,
            hydrologyEdgeFlowBias = profile.HydrologyEdgeFlowBias,
            hydrologyEdgeTangentWeight = profile.HydrologyEdgeTangentWeight,
            hydrologyEdgeFlowLockWeight = profile.HydrologyEdgeFlowLockWeight,
            hydrologyEdgeStabilityIterations = profile.HydrologyEdgeStabilityIterations,
            hydrologyEdgeStabilityWeight = profile.HydrologyEdgeStabilityWeight,
            hydrologyWaterTableClampWeight = profile.HydrologyWaterTableClampWeight,
            hydrologyWaterTableClampRange = profile.HydrologyWaterTableClampRange,
            hydrologyWaterTableSlopeWeight = profile.HydrologyWaterTableSlopeWeight,
            hydrologyFlowPersistence = profile.HydrologyFlowPersistence,
            hydrologyGradientWeight = profile.HydrologyGradientWeight,
            hydrologyGradientSlopeWeight = profile.HydrologyGradientSlopeWeight,
            hydrologyGradientClamp = profile.HydrologyGradientClamp,
            hydrologyDirectionalIterations = profile.HydrologyDirectionalIterations,
            hydrologyDirectionalBlend = profile.HydrologyDirectionalBlend,
            hydrologyFlowDivergenceClamp = profile.HydrologyFlowDivergenceClamp,
            hydrologyWarpFrequency = profile.HydrologyWarpFrequency,
            hydrologyWarpAmplitude = profile.HydrologyWarpAmplitude,
            riparianSmoothIterations = profile.RiparianSmoothIterations,
            riparianSmoothBlend = profile.RiparianSmoothBlend,
            riparianSaturationBoost = profile.RiparianSaturationBoost,
            riparianBufferRadius = profile.RiparianBufferRadius,
            riverCenterThreshold = profile.RiverCenterThreshold,
            riverBankThreshold = profile.RiverBankThreshold,
            riverDepth = profile.RiverDepth,
            riverNoiseScale = profile.RiverNoiseScale,
            riverIntensitySmoothIterations = profile.RiverIntensitySmoothIterations,
            riverIntensitySmoothBlend = profile.RiverIntensitySmoothBlend,
            riverConfluenceBoost = profile.RiverConfluenceBoost,
            riverTributaryCaptureWeight = profile.RiverTributaryCaptureWeight,
            riverAvulsionResistance = profile.RiverAvulsionResistance,
            riverFlowAlignmentWeight = profile.RiverFlowAlignmentWeight,
            riverGradientPenalty = profile.RiverGradientPenalty,
            riverHeadwaterStabilityWeight = profile.RiverHeadwaterStabilityWeight,
            riverAnisotropyWeight = profile.RiverAnisotropyWeight,
            riverAnisotropyDamping = profile.RiverAnisotropyDamping,
            riverMeanderJitter = profile.RiverMeanderJitter,
            riverReliefPenaltyWeight = profile.RiverReliefPenaltyWeight,
            riverBankStabilityClamp = profile.RiverBankStabilityClamp,
            riverEdgeFeather = profile.RiverEdgeFeather,
            riverEdgeContinuityWeight = profile.RiverEdgeContinuityWeight,
            riverMouthSmoothRadius = profile.RiverMouthSmoothRadius,
            riverDeltaWetlandStrength = profile.RiverDeltaWetlandStrength,
            riverSeamFillStrength = profile.RiverSeamFillStrength,
            riverBankErosionWeight = profile.RiverBankErosionWeight,
            lakeSpawnWeightBias = profile.LakeSpawnWeightBias,
            lakeShorelineBlend = profile.LakeShorelineBlend,
            lakeWetlandSaturationThreshold = profile.LakeWetlandSaturationThreshold,
            lakeOutflowCarveDepth = profile.LakeOutflowCarveDepth,
            lakeBasinSmoothIterations = profile.LakeBasinSmoothIterations,
            lakeShelfDepth = profile.LakeShelfDepth,
            lakeMaxRadius = profile.LakeMaxRadius,
            lakeWetlandBufferRadius = profile.LakeWetlandBufferRadius,
            lakeRiverProximitySuppression = profile.LakeRiverProximitySuppression,
            lakeInflowBlendWeight = profile.LakeInflowBlendWeight,
            lakeRimErosionWeight = profile.LakeRimErosionWeight,
            lakeOutflowSealWeight = profile.LakeOutflowSealWeight,
            lakeFlowSeepageWeight = profile.LakeFlowSeepageWeight,
            lakeVarianceWeight = profile.LakeVarianceWeight,
            lakeOutflowStabilityWeight = profile.LakeOutflowStabilityWeight,
            lakeOutflowTaper = profile.LakeOutflowTaper,
            lakeSpillRetentionWeight = profile.LakeSpillRetentionWeight,
            caveEdgeSealStrength = profile.CaveEdgeSealStrength,
            supportPillarChance = profile.SupportPillarChance,
            caveStabilitySmoothIterations = profile.CaveStabilitySmoothIterations,
            caveStabilitySmoothBlend = profile.CaveStabilitySmoothBlend,
            caveSupportDensity = profile.CaveSupportDensity,
            caveSupportHydrationBias = profile.CaveSupportHydrationBias,
            caveSupportFlowBias = profile.CaveSupportFlowBias,
            caveMoistureRetentionWeight = profile.CaveMoistureRetentionWeight,
            caveMoistureFlowClamp = profile.CaveMoistureFlowClamp,
            caveEntranceFlowDampening = profile.CaveEntranceFlowDampening,
            caveGroundwaterConnectivityWeight = profile.CaveGroundwaterConnectivityWeight,
            caveVentilationBias = profile.CaveVentilationBias,
            caveRiparianPlugDepth = profile.CaveRiparianPlugDepth,
            caveCeilingStabilityWeight = profile.CaveCeilingStabilityWeight,
            caveCeilingMoistureClamp = profile.CaveCeilingMoistureClamp,
            caveHydrologyWeight = profile.CaveHydrologyWeight,
            caveFlowWeight = profile.CaveFlowWeight,
            caveRoughnessWeight = profile.CaveRoughnessWeight,
            caveDepthWeight = profile.CaveDepthWeight,
            caveRiverSuppressionWeight = profile.CaveRiverSuppressionWeight,
            riparianCaveGuardWeight = profile.RiparianCaveGuardWeight,
            enableRivers = profile.EnableRivers,
            enableLakes = profile.EnableLakes,
            enableCaves = profile.EnableCaves,
            useImprovedRivers = profile.UseImprovedRivers,
            useImprovedLakes = profile.UseImprovedLakes,
            useImprovedCaves = profile.UseImprovedCaves
        };
    }

        public static WorldMapControlProfile FromConfig(WorldConfig config)
        {
            var water = config.Water;
            var caves = config.Caves;
            var lakes = config.Lakes;

            float caveWeightTotal = Mathf.Clamp01(caves.HydrologyStabilityWeight + caves.FlowStabilityWeight + caves.RoughnessStabilityWeight);
            float caveDepthWeight = Mathf.Clamp(1f - caveWeightTotal, 0.05f, 0.45f);

            var data = new WorldMapControlProfileData
            {
                version = Math.Max(SharedFeatureCatalog.MapControlProfileVersion, config.MapControlProfileVersion > 0 ? config.MapControlProfileVersion : 1),
                sourceConfig = string.IsNullOrEmpty(config.MapControlProfilePath) ? "WorldConfigData.json" : config.MapControlProfilePath,
                generatedAtUtc = DateTime.UtcNow.ToString("o"),
                hydrologySignature = SharedFeatureCatalog.HydrologySignature,
                chunkSize = Mathf.Max(1, config.ChunkSize),
                renderDistance = Mathf.Max(1, config.RenderDistance),
                simulationDistance = Mathf.Max(1, config.SimulationDistance),
                globalWaterLevel = water.GlobalWaterLevel,
                hydrologyGradientStabilityIterations = Mathf.Max(0, water.HydrologyGradientStabilityIterations),
                hydrologyGradientStabilityBlend = Mathf.Clamp01(water.HydrologyGradientStabilityBlend),
                hydrologyCurvatureWeight = Mathf.Clamp(water.HydrologyCurvatureWeight, 0f, 1.5f),
                hydrologyEdgeBlendRadius = Mathf.Max(1, water.HydrologyEdgeBlendRadius),
                hydrologyVarianceBlend = Mathf.Clamp01(water.HydrologyVarianceBlend),
                hydrologyVarianceClamp = Mathf.Clamp(water.HydrologyVarianceClamp, 0f, 1.25f),
                hydrologySeamRelaxIterations = Mathf.Max(0, water.HydrologySeamRelaxIterations),
                hydrologySeamRelaxBlend = Mathf.Clamp01(water.HydrologySeamRelaxBlend),
                hydrologyEdgeFluxBlend = Mathf.Clamp01(water.HydrologyEdgeFluxBlend),
                hydrologyEdgeVarianceClamp = Mathf.Clamp(water.HydrologyEdgeVarianceClamp, 0f, 1.25f),
                hydrologySmoothBlend = Mathf.Clamp01(water.HydrologySmoothBlend),
                hydrologySmoothIterations = Mathf.Max(0, water.HydrologySmoothIterations),
                hydrologyReservoirIterations = Mathf.Max(0, water.HydrologyReservoirIterations),
                hydrologyReservoirBlend = Mathf.Clamp01(water.HydrologyReservoirBlend),
                hydrologyShorePush = Mathf.Clamp(water.HydrologyShorePush, 0.1f, 64f),
                hydrologySlopePenalty = Mathf.Clamp(water.HydrologySlopePenalty, 0.1f, 64f),
                hydrologyFlowGain = Mathf.Clamp(water.HydrologyFlowGain, 0f, 2f),
                hydrologyFlowShadowWeight = Mathf.Clamp01(water.HydrologyFlowShadowWeight),
                hydrologyFlowShadowSlopeWeight = Mathf.Clamp01(water.HydrologyFlowShadowSlopeWeight),
                hydrologyEdgeNormalizationBlend = Mathf.Clamp01(water.HydrologyEdgeNormalizationBlend),
                hydrologyEdgeNormalizationIterations = Mathf.Max(0, water.HydrologyEdgeNormalizationIterations),
                hydrologyFlowMemoryWeight = Mathf.Clamp01(water.HydrologyFlowMemoryWeight),
                hydrologyContinuityWeight = Mathf.Clamp01(water.HydrologyContinuityWeight),
                hydrologyThalwegStabilityWeight = Mathf.Clamp(water.HydrologyThalwegStabilityWeight, 0f, 1.5f),
                hydrologyPressureBlend = Mathf.Clamp01(water.HydrologyPressureBlend),
                hydrologyPressureGradientClamp = Mathf.Clamp(water.HydrologyPressureGradientClamp, 0f, 1.5f),
                hydrologyEdgeFlowBias = Mathf.Clamp01(water.HydrologyEdgeFlowBias),
                hydrologyEdgeTangentWeight = Mathf.Clamp01(water.HydrologyEdgeTangentWeight),
                hydrologyEdgeFlowLockWeight = Mathf.Clamp01(water.HydrologyEdgeFlowLockWeight),
                hydrologyEdgeStabilityIterations = Mathf.Max(0, water.HydrologyEdgeStabilityIterations),
                hydrologyEdgeStabilityWeight = Mathf.Clamp01(water.HydrologyEdgeStabilityWeight),
                hydrologyWaterTableClampWeight = Mathf.Clamp01(water.HydrologyWaterTableClampWeight),
                hydrologyWaterTableClampRange = Mathf.Max(1, water.HydrologyWaterTableClampRange),
                hydrologyWaterTableSlopeWeight = Mathf.Clamp01(water.HydrologyWaterTableSlopeWeight),
                hydrologyFlowPersistence = Mathf.Clamp01(water.HydrologyFlowPersistence),
                hydrologyGradientWeight = Mathf.Clamp01(water.HydrologyGradientWeight),
                hydrologyGradientSlopeWeight = Mathf.Clamp01(water.HydrologyGradientSlopeWeight),
                hydrologyGradientClamp = Mathf.Clamp(water.HydrologyGradientClamp, 0.1f, 3.5f),
                hydrologyDirectionalIterations = Mathf.Max(0, water.HydrologyDirectionalIterations),
                hydrologyDirectionalBlend = Mathf.Clamp01(water.HydrologyDirectionalBlend),
                hydrologyFlowDivergenceClamp = Mathf.Clamp(water.HydrologyFlowDivergenceClamp, 0f, 1.5f),
                hydrologyWarpFrequency = Mathf.Clamp(water.HydrologyWarpFrequency, 0.0001f, 0.01f),
                hydrologyWarpAmplitude = Mathf.Clamp(water.HydrologyWarpAmplitude, 0f, 64f),
                riparianSmoothIterations = Mathf.Max(0, water.RiparianSmoothIterations),
                riparianSmoothBlend = Mathf.Clamp01(water.RiparianSmoothBlend),
                riparianSaturationBoost = Mathf.Clamp01(water.RiparianSaturationBoost),
                riparianBufferRadius = Mathf.Max(0, water.RiparianBufferRadius),
                riverCenterThreshold = water.RiverCenterThreshold,
                riverBankThreshold = water.RiverBankThreshold,
                riverDepth = Mathf.Max(1, water.RiverDepth),
                riverNoiseScale = water.RiverNoiseScale,
                riverIntensitySmoothIterations = Mathf.Max(1, water.RiverIntensitySmoothIterations),
                riverIntensitySmoothBlend = Mathf.Clamp01(water.RiverIntensitySmoothBlend),
                riverConfluenceBoost = Mathf.Clamp(water.RiverConfluenceBoost, 0f, 2f),
                riverTributaryCaptureWeight = Mathf.Clamp01(water.RiverTributaryCaptureWeight),
                riverAvulsionResistance = Mathf.Clamp01(water.RiverAvulsionResistance),
                riverFlowAlignmentWeight = Mathf.Clamp(water.RiverFlowAlignmentWeight, 0f, 2f),
                riverGradientPenalty = Mathf.Clamp01(water.RiverGradientPenalty),
                riverHeadwaterStabilityWeight = Mathf.Clamp01(water.RiverHeadwaterStabilityWeight),
                riverAnisotropyWeight = Mathf.Clamp(water.RiverAnisotropyWeight, 0f, 2f),
                riverAnisotropyDamping = Mathf.Clamp01(water.RiverAnisotropyDamping),
                riverMeanderJitter = Mathf.Clamp01(water.RiverMeanderJitter),
                riverReliefPenaltyWeight = Mathf.Clamp01(water.RiverReliefPenaltyWeight),
                riverBankStabilityClamp = Mathf.Clamp01(water.RiverBankStabilityClamp),
                riverEdgeFeather = Mathf.Clamp01(water.RiverEdgeFeather),
                riverEdgeContinuityWeight = Mathf.Clamp01(water.RiverEdgeContinuityWeight),
                riverMouthSmoothRadius = Mathf.Max(1, water.RiverMouthSmoothRadius),
                riverDeltaWetlandStrength = Mathf.Clamp01(water.RiverDeltaWetlandStrength),
                riverSeamFillStrength = Mathf.Clamp(water.RiverSeamFillStrength, 0f, 2f),
                riverBankErosionWeight = Mathf.Clamp01(water.RiverBankErosionWeight),
                lakeSpawnWeightBias = Mathf.Clamp(lakes.SpawnWeightBias, 0f, 1.5f),
                lakeShorelineBlend = Mathf.Clamp01(lakes.ShorelineBlend),
                lakeWetlandSaturationThreshold = Mathf.Clamp01(lakes.WetlandSaturationThreshold),
                lakeOutflowCarveDepth = Mathf.Max(1, lakes.OutflowCarveDepth),
                lakeBasinSmoothIterations = Mathf.Max(0, lakes.LakeBasinSmoothIterations),
                lakeShelfDepth = Mathf.Max(0, lakes.ShelfDepth),
                lakeMaxRadius = Mathf.Max(1, lakes.MaxRadius),
                lakeWetlandBufferRadius = Mathf.Max(0, lakes.WetlandBufferRadius),
                lakeRiverProximitySuppression = Mathf.Clamp01(lakes.RiverProximitySuppression),
                lakeInflowBlendWeight = Mathf.Clamp01(water.LakeInflowBlendWeight),
                lakeRimErosionWeight = Mathf.Clamp01(water.LakeRimErosionWeight),
                lakeOutflowSealWeight = Mathf.Clamp01(lakes.OutflowSealWeight),
                lakeFlowSeepageWeight = Mathf.Clamp01(lakes.FlowSeepageWeight),
                lakeVarianceWeight = Mathf.Clamp01(lakes.VarianceWeight),
                lakeOutflowStabilityWeight = Mathf.Clamp01(lakes.OutflowStabilityWeight),
                lakeOutflowTaper = Mathf.Clamp01(lakes.LakeOutflowTaper),
                lakeSpillRetentionWeight = Mathf.Clamp01(lakes.SpillRetentionWeight),
                caveEdgeSealStrength = Mathf.Clamp01(caves.EdgeSealStrength),
                supportPillarChance = Mathf.Clamp01(caves.SupportPillarChance),
                caveStabilitySmoothIterations = Mathf.Max(0, caves.StabilitySmoothIterations),
                caveStabilitySmoothBlend = Mathf.Clamp01(caves.StabilitySmoothBlend),
                caveSupportDensity = Mathf.Clamp01(caves.SupportDensity),
                caveSupportHydrationBias = Mathf.Clamp01(caves.SupportHydrationBias),
                caveSupportFlowBias = Mathf.Clamp01(caves.SupportFlowBias),
                caveRiparianPlugDepth = Mathf.Max(0, caves.RiparianPlugDepth),
                caveCeilingStabilityWeight = Mathf.Clamp01(caves.CaveCeilingStabilityWeight),
                caveCeilingMoistureClamp = Mathf.Clamp01(caves.CeilingMoistureClamp),
                caveMoistureRetentionWeight = Mathf.Clamp01(caves.MoistureRetentionWeight),
                caveMoistureFlowClamp = Mathf.Clamp01(caves.MoistureFlowClamp),
                caveEntranceFlowDampening = Mathf.Clamp01(caves.CaveEntranceFlowDampening),
                caveGroundwaterConnectivityWeight = Mathf.Clamp01(caves.GroundwaterConnectivityWeight),
                caveVentilationBias = Mathf.Clamp01(caves.CaveVentilationBias),
                caveHydrologyWeight = Mathf.Clamp01(caves.HydrologyStabilityWeight),
                caveFlowWeight = Mathf.Clamp01(caves.FlowStabilityWeight),
                caveRoughnessWeight = Mathf.Clamp01(caves.RoughnessStabilityWeight),
                caveDepthWeight = caveDepthWeight,
                caveRiverSuppressionWeight = Mathf.Clamp01(caves.RiverSuppressionWeight),
                riparianCaveGuardWeight = Mathf.Clamp01(caves.RiparianCaveGuardWeight),
                enableRivers = water.EnableRivers,
                enableLakes = water.EnableLakes,
                enableCaves = caves.EnableCaves,
                useImprovedRivers = water.UseImprovedRivers,
                useImprovedLakes = water.UseImprovedLakes,
                useImprovedCaves = caves.UseImprovedCaves,
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
            HydrologySignature = string.IsNullOrEmpty(data.hydrologySignature) ? SharedFeatureCatalog.HydrologySignature : data.hydrologySignature,
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
            HydrologySeamRelaxBlend = data.hydrologySeamRelaxBlend,
            HydrologyEdgeFluxBlend = data.hydrologyEdgeFluxBlend,
            HydrologyEdgeVarianceClamp = data.hydrologyEdgeVarianceClamp,
            HydrologySmoothBlend = data.hydrologySmoothBlend,
            HydrologySmoothIterations = data.hydrologySmoothIterations,
            HydrologyReservoirIterations = data.hydrologyReservoirIterations,
            HydrologyReservoirBlend = data.hydrologyReservoirBlend,
            HydrologyShorePush = data.hydrologyShorePush,
            HydrologySlopePenalty = data.hydrologySlopePenalty,
            HydrologyFlowGain = data.hydrologyFlowGain,
            HydrologyFlowShadowWeight = data.hydrologyFlowShadowWeight,
            HydrologyFlowShadowSlopeWeight = data.hydrologyFlowShadowSlopeWeight,
            HydrologyEdgeNormalizationBlend = data.hydrologyEdgeNormalizationBlend,
            HydrologyEdgeNormalizationIterations = data.hydrologyEdgeNormalizationIterations,
            HydrologyFlowMemoryWeight = data.hydrologyFlowMemoryWeight,
            HydrologyContinuityWeight = data.hydrologyContinuityWeight,
            HydrologyThalwegStabilityWeight = data.hydrologyThalwegStabilityWeight > 0f
                ? data.hydrologyThalwegStabilityWeight
                : Mathf.Clamp(data.hydrologyContinuityWeight, 0f, 1.5f),
            HydrologyPressureBlend = data.hydrologyPressureBlend,
            HydrologyPressureGradientClamp = data.hydrologyPressureGradientClamp,
            HydrologyEdgeFlowBias = data.hydrologyEdgeFlowBias,
            HydrologyEdgeTangentWeight = data.hydrologyEdgeTangentWeight,
            HydrologyEdgeFlowLockWeight = data.hydrologyEdgeFlowLockWeight,
            HydrologyEdgeStabilityIterations = data.hydrologyEdgeStabilityIterations,
            HydrologyEdgeStabilityWeight = data.hydrologyEdgeStabilityWeight,
            HydrologyWaterTableClampWeight = data.hydrologyWaterTableClampWeight,
            HydrologyWaterTableClampRange = data.hydrologyWaterTableClampRange,
            HydrologyWaterTableSlopeWeight = data.hydrologyWaterTableSlopeWeight,
            HydrologyFlowPersistence = data.hydrologyFlowPersistence,
            HydrologyGradientWeight = data.hydrologyGradientWeight,
            HydrologyGradientSlopeWeight = data.hydrologyGradientSlopeWeight,
            HydrologyGradientClamp = data.hydrologyGradientClamp,
            HydrologyDirectionalIterations = data.hydrologyDirectionalIterations,
            HydrologyDirectionalBlend = data.hydrologyDirectionalBlend,
            HydrologyFlowDivergenceClamp = data.hydrologyFlowDivergenceClamp,
            HydrologyWarpFrequency = data.hydrologyWarpFrequency,
            HydrologyWarpAmplitude = data.hydrologyWarpAmplitude,
            RiparianSmoothIterations = data.riparianSmoothIterations,
            RiparianSmoothBlend = data.riparianSmoothBlend,
            RiparianSaturationBoost = data.riparianSaturationBoost,
            RiparianBufferRadius = data.riparianBufferRadius,
            RiverCenterThreshold = data.riverCenterThreshold,
            RiverBankThreshold = data.riverBankThreshold,
            RiverDepth = data.riverDepth,
            RiverNoiseScale = data.riverNoiseScale,
            RiverIntensitySmoothIterations = data.riverIntensitySmoothIterations,
            RiverIntensitySmoothBlend = data.riverIntensitySmoothBlend,
            RiverConfluenceBoost = data.riverConfluenceBoost,
            RiverTributaryCaptureWeight = data.riverTributaryCaptureWeight,
            RiverAvulsionResistance = data.riverAvulsionResistance,
            RiverFlowAlignmentWeight = data.riverFlowAlignmentWeight,
            RiverGradientPenalty = data.riverGradientPenalty,
            RiverHeadwaterStabilityWeight = data.riverHeadwaterStabilityWeight,
            RiverAnisotropyWeight = data.riverAnisotropyWeight,
            RiverAnisotropyDamping = data.riverAnisotropyDamping,
            RiverMeanderJitter = data.riverMeanderJitter,
            RiverReliefPenaltyWeight = data.riverReliefPenaltyWeight,
            RiverBankStabilityClamp = data.riverBankStabilityClamp,
            RiverEdgeFeather = data.riverEdgeFeather,
            RiverEdgeContinuityWeight = data.riverEdgeContinuityWeight,
            RiverMouthSmoothRadius = data.riverMouthSmoothRadius,
            RiverDeltaWetlandStrength = data.riverDeltaWetlandStrength,
            RiverSeamFillStrength = data.riverSeamFillStrength,
            RiverBankErosionWeight = data.riverBankErosionWeight,
            LakeSpawnWeightBias = data.lakeSpawnWeightBias,
            LakeShorelineBlend = data.lakeShorelineBlend,
            LakeWetlandSaturationThreshold = data.lakeWetlandSaturationThreshold,
            LakeOutflowCarveDepth = data.lakeOutflowCarveDepth,
            LakeBasinSmoothIterations = data.lakeBasinSmoothIterations,
            LakeShelfDepth = data.lakeShelfDepth,
            LakeMaxRadius = data.lakeMaxRadius,
            LakeWetlandBufferRadius = data.lakeWetlandBufferRadius,
            LakeRiverProximitySuppression = data.lakeRiverProximitySuppression,
            LakeInflowBlendWeight = data.lakeInflowBlendWeight,
            LakeRimErosionWeight = data.lakeRimErosionWeight,
            LakeOutflowSealWeight = data.lakeOutflowSealWeight,
            LakeFlowSeepageWeight = data.lakeFlowSeepageWeight,
            LakeVarianceWeight = data.lakeVarianceWeight,
            LakeOutflowStabilityWeight = data.lakeOutflowStabilityWeight,
            LakeOutflowTaper = data.lakeOutflowTaper,
            LakeSpillRetentionWeight = data.lakeSpillRetentionWeight,
            CaveEdgeSealStrength = data.caveEdgeSealStrength,
            SupportPillarChance = data.supportPillarChance,
            CaveStabilitySmoothIterations = data.caveStabilitySmoothIterations,
            CaveStabilitySmoothBlend = data.caveStabilitySmoothBlend,
            CaveSupportDensity = data.caveSupportDensity,
            CaveSupportHydrationBias = data.caveSupportHydrationBias,
            CaveSupportFlowBias = data.caveSupportFlowBias,
            CaveMoistureFlowClamp = data.caveMoistureFlowClamp,
            CaveEntranceFlowDampening = data.caveEntranceFlowDampening,
            CaveGroundwaterConnectivityWeight = data.caveGroundwaterConnectivityWeight,
            CaveVentilationBias = data.caveVentilationBias,
            CaveRiparianPlugDepth = data.caveRiparianPlugDepth,
            CaveCeilingStabilityWeight = data.caveCeilingStabilityWeight,
            CaveCeilingMoistureClamp = data.caveCeilingMoistureClamp,
            CaveMoistureRetentionWeight = data.caveMoistureRetentionWeight,
            CaveHydrologyWeight = data.caveHydrologyWeight,
            CaveFlowWeight = data.caveFlowWeight,
            CaveRoughnessWeight = data.caveRoughnessWeight,
            CaveDepthWeight = data.caveDepthWeight,
            CaveRiverSuppressionWeight = data.caveRiverSuppressionWeight,
            RiparianCaveGuardWeight = data.riparianCaveGuardWeight,
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
            .Append(string.IsNullOrEmpty(data.hydrologySignature) ? SharedFeatureCatalog.HydrologySignature : data.hydrologySignature).Append('|')
            .Append(data.globalWaterLevel).Append('|')
            .Append(data.hydrologyGradientStabilityIterations).Append('|')
            .Append(data.hydrologyGradientStabilityBlend).Append('|')
            .Append(data.hydrologyCurvatureWeight).Append('|')
            .Append(data.hydrologyEdgeBlendRadius).Append('|')
            .Append(data.hydrologyVarianceBlend).Append('|')
            .Append(data.hydrologyVarianceClamp).Append('|')
            .Append(data.hydrologySeamRelaxIterations).Append('|')
            .Append(data.hydrologySeamRelaxBlend).Append('|')
            .Append(data.hydrologyEdgeFluxBlend).Append('|')
            .Append(data.hydrologyEdgeVarianceClamp).Append('|')
            .Append(data.hydrologySmoothBlend).Append('|')
            .Append(data.hydrologySmoothIterations).Append('|')
            .Append(data.hydrologyReservoirIterations).Append('|')
            .Append(data.hydrologyReservoirBlend).Append('|')
            .Append(data.hydrologyShorePush).Append('|')
            .Append(data.hydrologySlopePenalty).Append('|')
            .Append(data.hydrologyFlowGain).Append('|')
            .Append(data.hydrologyFlowShadowWeight).Append('|')
            .Append(data.hydrologyFlowShadowSlopeWeight).Append('|')
            .Append(data.hydrologyEdgeNormalizationBlend).Append('|')
            .Append(data.hydrologyEdgeNormalizationIterations).Append('|')
            .Append(data.hydrologyFlowMemoryWeight).Append('|')
            .Append(data.hydrologyContinuityWeight).Append('|')
            .Append(data.hydrologyThalwegStabilityWeight).Append('|')
            .Append(data.hydrologyPressureBlend).Append('|')
            .Append(data.hydrologyPressureGradientClamp).Append('|')
            .Append(data.hydrologyEdgeFlowBias).Append('|')
            .Append(data.hydrologyEdgeTangentWeight).Append('|')
            .Append(data.hydrologyEdgeFlowLockWeight).Append('|')
            .Append(data.hydrologyEdgeStabilityIterations).Append('|')
            .Append(data.hydrologyEdgeStabilityWeight).Append('|')
            .Append(data.hydrologyWaterTableClampWeight).Append('|')
            .Append(data.hydrologyWaterTableClampRange).Append('|')
            .Append(data.hydrologyWaterTableSlopeWeight).Append('|')
            .Append(data.hydrologyFlowPersistence).Append('|')
            .Append(data.hydrologyGradientWeight).Append('|')
            .Append(data.hydrologyGradientSlopeWeight).Append('|')
            .Append(data.hydrologyGradientClamp).Append('|')
            .Append(data.hydrologyDirectionalIterations).Append('|')
            .Append(data.hydrologyDirectionalBlend).Append('|')
            .Append(data.hydrologyFlowDivergenceClamp).Append('|')
            .Append(data.hydrologyWarpFrequency).Append('|')
            .Append(data.hydrologyWarpAmplitude).Append('|')
            .Append(data.riparianSmoothIterations).Append('|')
            .Append(data.riparianSmoothBlend).Append('|')
            .Append(data.riparianSaturationBoost).Append('|')
            .Append(data.riparianBufferRadius).Append('|')
            .Append(data.riverCenterThreshold).Append('|')
            .Append(data.riverBankThreshold).Append('|')
            .Append(data.riverDepth).Append('|')
            .Append(data.riverNoiseScale).Append('|')
            .Append(data.riverIntensitySmoothIterations).Append('|')
            .Append(data.riverIntensitySmoothBlend).Append('|')
            .Append(data.riverConfluenceBoost).Append('|')
            .Append(data.riverTributaryCaptureWeight).Append('|')
            .Append(data.riverAvulsionResistance).Append('|')
            .Append(data.riverFlowAlignmentWeight).Append('|')
            .Append(data.riverGradientPenalty).Append('|')
            .Append(data.riverHeadwaterStabilityWeight).Append('|')
            .Append(data.riverAnisotropyWeight).Append('|')
            .Append(data.riverAnisotropyDamping).Append('|')
            .Append(data.riverMeanderJitter).Append('|')
            .Append(data.riverReliefPenaltyWeight).Append('|')
            .Append(data.riverBankStabilityClamp).Append('|')
            .Append(data.riverEdgeFeather).Append('|')
            .Append(data.riverEdgeContinuityWeight).Append('|')
            .Append(data.riverMouthSmoothRadius).Append('|')
            .Append(data.riverDeltaWetlandStrength).Append('|')
            .Append(data.riverSeamFillStrength).Append('|')
            .Append(data.riverBankErosionWeight).Append('|')
            .Append(data.lakeSpawnWeightBias).Append('|')
            .Append(data.lakeShorelineBlend).Append('|')
            .Append(data.lakeWetlandSaturationThreshold).Append('|')
            .Append(data.lakeOutflowCarveDepth).Append('|')
            .Append(data.lakeBasinSmoothIterations).Append('|')
            .Append(data.lakeShelfDepth).Append('|')
            .Append(data.lakeMaxRadius).Append('|')
            .Append(data.lakeWetlandBufferRadius).Append('|')
            .Append(data.lakeRiverProximitySuppression).Append('|')
            .Append(data.lakeInflowBlendWeight).Append('|')
            .Append(data.lakeRimErosionWeight).Append('|')
            .Append(data.lakeOutflowSealWeight).Append('|')
            .Append(data.lakeFlowSeepageWeight).Append('|')
            .Append(data.lakeVarianceWeight).Append('|')
            .Append(data.lakeOutflowStabilityWeight).Append('|')
            .Append(data.lakeOutflowTaper).Append('|')
            .Append(data.lakeSpillRetentionWeight).Append('|')
            .Append(data.caveEdgeSealStrength).Append('|')
            .Append(data.supportPillarChance).Append('|')
            .Append(data.caveStabilitySmoothIterations).Append('|')
            .Append(data.caveStabilitySmoothBlend).Append('|')
            .Append(data.caveSupportDensity).Append('|')
            .Append(data.caveSupportHydrationBias).Append('|')
            .Append(data.caveSupportFlowBias).Append('|')
            .Append(data.caveMoistureFlowClamp).Append('|')
            .Append(data.caveEntranceFlowDampening).Append('|')
            .Append(data.caveGroundwaterConnectivityWeight).Append('|')
            .Append(data.caveVentilationBias).Append('|')
            .Append(data.caveRiparianPlugDepth).Append('|')
            .Append(data.caveCeilingStabilityWeight).Append('|')
            .Append(data.caveCeilingMoistureClamp).Append('|')
            .Append(data.caveMoistureRetentionWeight).Append('|')
            .Append(data.caveHydrologyWeight).Append('|')
            .Append(data.caveFlowWeight).Append('|')
            .Append(data.caveRoughnessWeight).Append('|')
            .Append(data.caveDepthWeight).Append('|')
            .Append(data.caveRiverSuppressionWeight).Append('|')
            .Append(data.riparianCaveGuardWeight).Append('|')
            .Append(data.enableRivers).Append('|')
            .Append(data.enableLakes).Append('|')
            .Append(data.enableCaves).Append('|')
            .Append(data.useImprovedCaves).Append('|')
            .Append(data.useImprovedRivers).Append('|')
            .Append(data.useImprovedLakes);

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    public bool MatchesHash(string hash)
    {
        return !string.IsNullOrWhiteSpace(hash) &&
               string.Equals(ProfileHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}
