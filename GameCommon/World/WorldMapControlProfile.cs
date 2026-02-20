using System;
using System.Text.Json.Serialization;

namespace GameCommon.World
{
    /// <summary>
    /// Shared, data-driven snapshot for world map control so server and client hydrology/cave previews stay aligned.
    /// Serialized to JSON for parity with Unity StreamingAssets.
    /// </summary>
    public sealed class WorldMapControlProfile
    {
        public int Version { get; set; }

        public string ProfileHash { get; set; } = string.Empty;

        public string SourceConfig { get; set; } = string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public string HydrologySignature { get; set; } = SharedFeatureCatalog.HydrologySignature;

        public int ChunkSize { get; set; }

        public int RenderDistance { get; set; }

        public int SimulationDistance { get; set; }

        public int GlobalWaterLevel { get; set; }

        public int HydrologyGradientStabilityIterations { get; set; }

        public double HydrologyGradientStabilityBlend { get; set; }

        public double HydrologyCurvatureWeight { get; set; }

        public int HydrologyEdgeBlendRadius { get; set; }

        public double HydrologyVarianceBlend { get; set; }

        public double HydrologyVarianceClamp { get; set; }

        public int HydrologySeamRelaxIterations { get; set; }

        public double HydrologySeamRelaxBlend { get; set; }

        public double HydrologyEdgeFluxBlend { get; set; }

        public double HydrologyEdgeVarianceClamp { get; set; }

        public double HydrologySmoothBlend { get; set; }

        public int HydrologySmoothIterations { get; set; }

        public int HydrologyReservoirIterations { get; set; }

        public double HydrologyReservoirBlend { get; set; }

        public double HydrologyShorePush { get; set; }

        public double HydrologySlopePenalty { get; set; }

        public double HydrologyFlowGain { get; set; }

        public double HydrologyFlowShadowWeight { get; set; }

        public double HydrologyFlowShadowSlopeWeight { get; set; }

        public double HydrologyEdgeNormalizationBlend { get; set; }

        public int HydrologyEdgeNormalizationIterations { get; set; }

        public double HydrologyFlowMemoryWeight { get; set; }

        public double HydrologyContinuityWeight { get; set; }

        public double HydrologyPressureBlend { get; set; }

        public double HydrologyPressureGradientClamp { get; set; }

        public double HydrologyEdgeFlowBias { get; set; }

        public double HydrologyEdgeTangentWeight { get; set; }

        public double HydrologyEdgeFlowLockWeight { get; set; }

        public int HydrologyEdgeStabilityIterations { get; set; }

        public double HydrologyEdgeStabilityWeight { get; set; }

        public double HydrologyWaterTableClampWeight { get; set; }

        public int HydrologyWaterTableClampRange { get; set; }

        public double HydrologyWaterTableSlopeWeight { get; set; }

        public double HydrologyFlowPersistence { get; set; }

        public double HydrologyGradientWeight { get; set; }

        public double HydrologyGradientSlopeWeight { get; set; }

        public double HydrologyGradientClamp { get; set; }

        public int HydrologyDirectionalIterations { get; set; }

        public double HydrologyDirectionalBlend { get; set; }

        public double HydrologyFlowDivergenceClamp { get; set; }

        public double HydrologyWarpFrequency { get; set; }

        public double HydrologyWarpAmplitude { get; set; }

        public int RiparianSmoothIterations { get; set; }

        public double RiparianSmoothBlend { get; set; }

        public double RiparianSaturationBoost { get; set; }

        public int RiparianBufferRadius { get; set; }

        public double RiverCenterThreshold { get; set; }

        public double RiverBankThreshold { get; set; }

        public int RiverDepth { get; set; }

        public double RiverNoiseScale { get; set; }

        public int RiverIntensitySmoothIterations { get; set; }

        public double RiverIntensitySmoothBlend { get; set; }

        public double RiverConfluenceBoost { get; set; }

        public double RiverTributaryCaptureWeight { get; set; }

        public double RiverAvulsionResistance { get; set; }

        public double RiverFlowAlignmentWeight { get; set; }

        public double RiverGradientPenalty { get; set; }

        public double RiverHeadwaterStabilityWeight { get; set; }

        public double RiverAnisotropyWeight { get; set; }

        public double RiverAnisotropyDamping { get; set; }

        public double RiverMeanderJitter { get; set; }

        public double RiverReliefPenaltyWeight { get; set; }

        public double RiverBankStabilityClamp { get; set; }

        public double RiverEdgeFeather { get; set; }

        public int RiverMouthSmoothRadius { get; set; }

        public double RiverDeltaWetlandStrength { get; set; }

        public double RiverSeamFillStrength { get; set; }

        public double RiverBankErosionWeight { get; set; }

        public double RiverEdgeContinuityWeight { get; set; }

        public double LakeSpawnWeightBias { get; set; }

        public double LakeShorelineBlend { get; set; }

        public double LakeWetlandSaturationThreshold { get; set; }

        public int LakeOutflowCarveDepth { get; set; }

        public int LakeBasinSmoothIterations { get; set; }

        public int LakeShelfDepth { get; set; }

        public int LakeMaxRadius { get; set; }

        public int LakeWetlandBufferRadius { get; set; }

        public double LakeRiverProximitySuppression { get; set; }

        public double LakeInflowBlendWeight { get; set; }

        public double LakeRimErosionWeight { get; set; }

        public double LakeOutflowSealWeight { get; set; }

        public double LakeFlowSeepageWeight { get; set; }

        public double LakeVarianceWeight { get; set; }

        public double LakeOutflowStabilityWeight { get; set; }

        public double LakeOutflowTaper { get; set; }

        public double LakeSpillRetentionWeight { get; set; }

        public double CaveEdgeSealStrength { get; set; }

        public double SupportPillarChance { get; set; }

        public int CaveStabilitySmoothIterations { get; set; }

        public double CaveStabilitySmoothBlend { get; set; }

        public double CaveSupportDensity { get; set; }

        public double CaveSupportHydrationBias { get; set; }

        public double CaveSupportFlowBias { get; set; }

        public double CaveMoistureRetentionWeight { get; set; }

        public double CaveMoistureFlowClamp { get; set; }

        public int CaveRiparianPlugDepth { get; set; }

        public double CaveCeilingStabilityWeight { get; set; }

        public double CaveHydrologyWeight { get; set; }

        public double CaveFlowWeight { get; set; }

        public double CaveRoughnessWeight { get; set; }

        public double CaveDepthWeight { get; set; }

        public double CaveRiverSuppressionWeight { get; set; }

        public double RiparianCaveGuardWeight { get; set; }

        public double CaveCeilingMoistureClamp { get; set; }

        public double CaveEntranceFlowDampening { get; set; }

        public double CaveGroundwaterConnectivityWeight { get; set; }

        public double CaveVentilationBias { get; set; }

        public bool EnableRivers { get; set; }

        public bool EnableLakes { get; set; }

        public bool EnableCaves { get; set; }

        public bool UseImprovedCaves { get; set; }

        public bool UseImprovedRivers { get; set; }

        public bool UseImprovedLakes { get; set; }

        public WorldMapControlProfile Clone()
        {
            return (WorldMapControlProfile)MemberwiseClone();
        }

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(HydrologySignature))
            {
                HydrologySignature = SharedFeatureCatalog.HydrologySignature;
            }

            if (GeneratedAtUtc == default)
            {
                GeneratedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
