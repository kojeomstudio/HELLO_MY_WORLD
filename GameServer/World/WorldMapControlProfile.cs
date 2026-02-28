using System;
using GameCommon.World;
using WorldMapControlProfileUtilityShared = GameCommon.World.WorldMapControlProfileUtility;

namespace GameServerApp.World
{
    /// <summary>
    /// Server-side builder that maps world generation config into the shared world map control profile
    /// and persists it via the shared GameCommon utility.
    /// </summary>
    public static class WorldMapControlProfileUtility
    {
        public static WorldMapControlProfile Create(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            int chunkSize = Math.Max(1, config.ChunkSize);
            int renderDistance = Math.Max(config.RenderDistance, Math.Max(1, worldSettings.ChunkLoadRadius));
            int simulationDistance = Math.Max(config.SimulationDistance, Math.Max(1, renderDistance - 2));
            double caveWeightTotal = Math.Clamp(
                config.Caves.HydrologyStabilityWeight + config.Caves.FlowStabilityWeight + config.Caves.RoughnessStabilityWeight,
                0.0,
                1.0);
            double caveDepthWeight = Math.Clamp(1.0 - caveWeightTotal, 0.05, 0.45);

            var profile = new WorldMapControlProfile
            {
                Version = Math.Max(1, config.MapControlProfileVersion),
                SourceConfig = config.SourcePath,
                GeneratedAtUtc = DateTime.UtcNow,
                ChunkSize = chunkSize,
                RenderDistance = renderDistance,
                SimulationDistance = simulationDistance,
                HydrologySignature = SharedFeatureCatalog.HydrologySignature,
                GlobalWaterLevel = Math.Max(0, config.Water.GlobalWaterLevel),
                HydrologyGradientStabilityIterations = Math.Max(0, config.Water.HydrologyGradientStabilityIterations),
                HydrologyGradientStabilityBlend = config.Water.HydrologyGradientStabilityBlend,
                HydrologyCurvatureWeight = config.Water.HydrologyCurvatureWeight,
                HydrologyEdgeBlendRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius),
                HydrologyVarianceBlend = config.Water.HydrologyVarianceBlend,
                HydrologyVarianceClamp = config.Water.HydrologyVarianceClamp,
                HydrologySeamRelaxIterations = Math.Max(0, config.Water.HydrologySeamRelaxIterations),
                HydrologySeamRelaxBlend = config.Water.HydrologySeamRelaxBlend,
                HydrologyEdgeFluxBlend = config.Water.HydrologyEdgeFluxBlend,
                HydrologyEdgeVarianceClamp = config.Water.HydrologyEdgeVarianceClamp,
                HydrologySmoothBlend = config.Water.HydrologySmoothBlend,
                HydrologySmoothIterations = Math.Max(0, config.Water.HydrologySmoothIterations),
                HydrologyReservoirIterations = Math.Max(0, config.Water.HydrologyReservoirIterations),
                HydrologyReservoirBlend = config.Water.HydrologyReservoirBlend,
                HydrologyShorePush = config.Water.HydrologyShorePush,
                HydrologySlopePenalty = config.Water.HydrologySlopePenalty,
                HydrologyFlowGain = config.Water.HydrologyFlowGain,
                HydrologyFlowShadowWeight = config.Water.HydrologyFlowShadowWeight,
                HydrologyFlowShadowSlopeWeight = config.Water.HydrologyFlowShadowSlopeWeight,
                HydrologyEdgeNormalizationBlend = config.Water.HydrologyEdgeNormalizationBlend,
                HydrologyEdgeNormalizationIterations = Math.Max(0, config.Water.HydrologyEdgeNormalizationIterations),
                HydrologyFlowMemoryWeight = config.Water.HydrologyFlowMemoryWeight,
                HydrologyContinuityWeight = config.Water.HydrologyContinuityWeight,
                HydrologyThalwegStabilityWeight = config.Water.HydrologyThalwegStabilityWeight,
                HydrologyPressureBlend = config.Water.HydrologyPressureBlend,
                HydrologyPressureGradientClamp = config.Water.HydrologyPressureGradientClamp,
                HydrologyEdgeFlowBias = config.Water.HydrologyEdgeFlowBias,
                HydrologyEdgeTangentWeight = config.Water.HydrologyEdgeTangentWeight,
                HydrologyEdgeFlowLockWeight = config.Water.HydrologyEdgeFlowLockWeight,
                HydrologyEdgeStabilityIterations = Math.Max(0, config.Water.HydrologyEdgeStabilityIterations),
                HydrologyEdgeStabilityWeight = config.Water.HydrologyEdgeStabilityWeight,
                HydrologyWaterTableClampWeight = config.Water.HydrologyWaterTableClampWeight,
                HydrologyWaterTableClampRange = Math.Max(1, config.Water.HydrologyWaterTableClampRange),
                HydrologyWaterTableSlopeWeight = config.Water.HydrologyWaterTableSlopeWeight,
                HydrologyFlowPersistence = config.Water.HydrologyFlowPersistence,
                HydrologyGradientWeight = config.Water.HydrologyGradientWeight,
                HydrologyGradientSlopeWeight = config.Water.HydrologyGradientSlopeWeight,
                HydrologyGradientClamp = config.Water.HydrologyGradientClamp,
                HydrologyDirectionalIterations = Math.Max(0, config.Water.HydrologyDirectionalIterations),
                HydrologyDirectionalBlend = config.Water.HydrologyDirectionalBlend,
                HydrologyFlowDivergenceClamp = config.Water.HydrologyFlowDivergenceClamp,
                HydrologyWarpFrequency = config.Water.HydrologyWarpFrequency,
                HydrologyWarpAmplitude = config.Water.HydrologyWarpAmplitude,
                RiparianSmoothIterations = Math.Max(0, config.Water.RiparianSmoothIterations),
                RiparianSmoothBlend = config.Water.RiparianSmoothBlend,
                RiparianSaturationBoost = config.Water.RiparianSaturationBoost,
                RiparianBufferRadius = Math.Max(0, config.Water.RiparianBufferRadius),
                RiverCenterThreshold = config.Water.RiverCenterThreshold,
                RiverBankThreshold = config.Water.RiverBankThreshold,
                RiverDepth = Math.Max(0, config.Water.RiverDepth),
                RiverNoiseScale = config.Water.RiverNoiseScale,
                RiverIntensitySmoothIterations = Math.Max(0, config.Water.RiverIntensitySmoothIterations),
                RiverIntensitySmoothBlend = config.Water.RiverIntensitySmoothBlend,
                RiverConfluenceBoost = config.Water.RiverConfluenceBoost,
                RiverTributaryCaptureWeight = config.Water.RiverTributaryCaptureWeight,
                RiverAvulsionResistance = config.Water.RiverAvulsionResistance,
                RiverFlowAlignmentWeight = config.Water.RiverFlowAlignmentWeight,
                RiverGradientPenalty = config.Water.RiverGradientPenalty,
                RiverHeadwaterStabilityWeight = config.Water.RiverHeadwaterStabilityWeight,
                RiverAnisotropyWeight = config.Water.RiverAnisotropyWeight,
                RiverAnisotropyDamping = config.Water.RiverAnisotropyDamping,
                RiverMeanderJitter = config.Water.RiverMeanderJitter,
                RiverReliefPenaltyWeight = config.Water.RiverReliefPenaltyWeight,
                RiverBankStabilityClamp = config.Water.RiverBankStabilityClamp,
                RiverEdgeFeather = config.Water.RiverEdgeFeather,
                RiverMouthSmoothRadius = config.Water.RiverMouthSmoothRadius,
                RiverDeltaWetlandStrength = config.Water.RiverDeltaWetlandStrength,
                RiverSeamFillStrength = config.Water.RiverSeamFillStrength,
                RiverBankErosionWeight = config.Water.RiverBankErosionWeight,
                RiverEdgeContinuityWeight = config.Water.RiverEdgeContinuityWeight,
                LakeSpawnWeightBias = config.Lakes.SpawnWeightBias,
                LakeShorelineBlend = config.Lakes.ShorelineBlend,
                LakeWetlandSaturationThreshold = config.Lakes.WetlandSaturationThreshold,
                LakeOutflowCarveDepth = config.Lakes.OutflowCarveDepth,
                LakeBasinSmoothIterations = Math.Max(0, config.Lakes.LakeBasinSmoothIterations),
                LakeShelfDepth = config.Lakes.ShelfDepth,
                LakeMaxRadius = Math.Max(1, config.Lakes.MaxRadius),
                LakeWetlandBufferRadius = Math.Max(0, config.Lakes.WetlandBufferRadius),
                LakeRiverProximitySuppression = config.Lakes.RiverProximitySuppression,
                LakeInflowBlendWeight = config.Water.LakeInflowBlendWeight,
                LakeRimErosionWeight = config.Water.LakeRimErosionWeight,
                LakeOutflowSealWeight = config.Lakes.OutflowSealWeight,
                LakeFlowSeepageWeight = config.Lakes.FlowSeepageWeight,
                LakeVarianceWeight = config.Lakes.VarianceWeight,
                LakeOutflowStabilityWeight = config.Lakes.OutflowStabilityWeight,
                LakeOutflowTaper = config.Lakes.LakeOutflowTaper,
                LakeSpillRetentionWeight = config.Lakes.SpillRetentionWeight,
                CaveEdgeSealStrength = config.Caves.EdgeSealStrength,
                SupportPillarChance = config.Caves.SupportPillarChance,
                CaveStabilitySmoothIterations = Math.Max(0, config.Caves.StabilitySmoothIterations),
                CaveStabilitySmoothBlend = config.Caves.StabilitySmoothBlend,
                CaveSupportDensity = config.Caves.SupportDensity,
                CaveSupportHydrationBias = config.Caves.SupportHydrationBias,
                CaveSupportFlowBias = config.Caves.SupportFlowBias,
                CaveMoistureRetentionWeight = config.Caves.MoistureRetentionWeight,
                CaveMoistureFlowClamp = config.Caves.MoistureFlowClamp,
                CaveRiparianPlugDepth = config.Caves.RiparianPlugDepth,
                CaveCeilingStabilityWeight = config.Caves.CeilingStabilityWeight,
                CaveHydrologyWeight = config.Caves.HydrologyStabilityWeight,
                CaveFlowWeight = config.Caves.FlowStabilityWeight,
                CaveRoughnessWeight = config.Caves.RoughnessStabilityWeight,
                CaveDepthWeight = caveDepthWeight,
                CaveRiverSuppressionWeight = config.Caves.RiverSuppressionWeight,
                RiparianCaveGuardWeight = config.Caves.RiparianCaveGuardWeight,
                CaveCeilingMoistureClamp = config.Caves.CeilingMoistureClamp,
                CaveEntranceFlowDampening = config.Caves.CaveEntranceFlowDampening,
                CaveGroundwaterConnectivityWeight = config.Caves.GroundwaterConnectivityWeight,
                CaveVentilationBias = config.Caves.CaveVentilationBias,
                EnableRivers = config.Water.EnableRivers,
                EnableLakes = config.Water.EnableLakes,
                EnableCaves = config.Caves.EnableCaves,
                UseImprovedCaves = config.Caves.UseImprovedCaves,
                UseImprovedRivers = config.Water.UseImprovedRivers,
                UseImprovedLakes = config.Water.UseImprovedLakes
            };

            profile.ProfileHash = WorldMapControlProfileUtilityShared.ComputeHash(profile);
            return profile;
        }

        public static string ComputeHash(WorldMapControlProfile profile)
        {
            return WorldMapControlProfileUtilityShared.ComputeHash(profile);
        }

        public static void Save(WorldMapControlProfile profile, string path)
        {
            WorldMapControlProfileUtilityShared.Save(profile, path);
        }

        public static WorldMapControlProfile? Load(string path)
        {
            return WorldMapControlProfileUtilityShared.Load(path);
        }

        public static WorldMapControlProfile LoadOrCreate(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            return WorldMapControlProfileUtilityShared.LoadOrCreate(
                config.MapControlProfilePath,
                () => Create(config, worldSettings),
                profile => profile.ProfileHash,
                Math.Max(1, config.MapControlProfileVersion));
        }
    }
}
