using System;

namespace GameCommon.World
{
    /// <summary>
    /// Shared enums for world map control messages consumed by both server and client code.
    /// These values must stay stable across protobuf handlers and Unity preview tooling.
    /// </summary>
    public enum WorldMapRequestType
    {
        GetInitialMap = 0,
        UpdateChunk = 1,
        GetPlayerProfile = 2,
        UpdatePlayerProfile = 3
    }

    public enum ProfileUpdateType
    {
        RenderDistance = 0,
        MapScale = 1,
        ShowCoordinates = 2,
        ShowBiomeInfo = 3
    }

    /// <summary>
    /// Signature context used by WorldMapSignature to compute a deterministic hash for terrain/map parity.
    /// </summary>
    public sealed class WorldMapSignatureContext
    {
        public WorldMapSignatureContext(
            string pipelineVersion,
            string worldName,
            long seed,
            string protoBaseline,
            string protoComputed,
            int profileVersion,
            string profileHash,
            string hydrologySignature,
            int chunkSize,
            int worldHeight,
            int renderDistance,
            int simulationDistance,
            int globalWaterLevel,
            int seaLevel,
            double flowPersistence,
            double flowGain,
            double watershedStitchWeight,
            int watershedStitchRadius,
            int hydrologyGradientStabilityIterations,
            double hydrologyGradientStabilityBlend,
            double hydrologyGradientClamp,
            double hydrologyCurvatureWeight,
            double hydrologySlopePenalty,
            double hydrologyWaterTableClampWeight,
            int hydrologyWaterTableClampRange,
            double hydrologyWaterTableSlopeWeight,
            int lakesMinDepth,
            int lakesMaxDepth,
            int lakesMaxRadius,
            int lakesShelfDepth,
            double lakesFlowSeepageWeight,
            double lakeOutflowSealWeight,
            double lakeOutflowStabilityWeight,
            double caveCeilingMoistureWeight,
            double caveCeilingMoistureClamp,
            double caveMoistureFlowClamp,
            double floodedCaveNoiseFrequency,
            double floodedCaveThreshold,
            double floodedCaveProximityWeight,
            double caveWaterThreshold,
            double caveLavaThreshold,
            int hydrologyEdgeBlendRadius,
            double hydrologyEdgeVarianceClamp,
            double hydrologyEdgeNormalizationBlend,
            int hydrologyEdgeNormalizationIterations,
            double hydrologyFlowMemoryWeight,
            double hydrologyContinuityWeight,
            double riverMeanderJitter,
            double riverReliefPenaltyWeight,
            double riverAnisotropyDamping,
            double riverBankStabilityClamp,
            double riverSeamFillStrength,
            double lakeRiverProximitySuppression,
            double hydrologyFlowShadowWeight,
            double hydrologyFlowShadowSlopeWeight,
            double hydrologyPressureBlend,
            double hydrologyPressureGradientClamp,
            double hydrologyEdgeFlowBias,
            double hydrologyEdgeFlowLockWeight,
            double riverFlowAlignmentWeight,
            double riverConfluenceBoost,
            double lakeRimErosionWeight,
            double lakeVarianceWeight,
            double caveEdgeSealStrength,
            double caveRiverSuppressionWeight,
            double riparianCaveGuardWeight,
            int hydrologyReservoirIterations,
            double hydrologyReservoirBlend)
        {
            PipelineVersion = pipelineVersion;
            WorldName = worldName;
            Seed = seed;
            ProtoBaseline = protoBaseline;
            ProtoComputed = protoComputed;
            ProfileVersion = profileVersion;
            ProfileHash = profileHash;
            HydrologySignature = hydrologySignature;
            ChunkSize = chunkSize;
            WorldHeight = worldHeight;
            RenderDistance = renderDistance;
            SimulationDistance = simulationDistance;
            GlobalWaterLevel = globalWaterLevel;
            SeaLevel = seaLevel;
            FlowPersistence = flowPersistence;
            FlowGain = flowGain;
            WatershedStitchWeight = watershedStitchWeight;
            WatershedStitchRadius = watershedStitchRadius;
            HydrologyGradientStabilityIterations = hydrologyGradientStabilityIterations;
            HydrologyGradientStabilityBlend = hydrologyGradientStabilityBlend;
            HydrologyGradientClamp = hydrologyGradientClamp;
            HydrologyCurvatureWeight = hydrologyCurvatureWeight;
            HydrologySlopePenalty = hydrologySlopePenalty;
            HydrologyWaterTableClampWeight = hydrologyWaterTableClampWeight;
            HydrologyWaterTableClampRange = hydrologyWaterTableClampRange;
            HydrologyWaterTableSlopeWeight = hydrologyWaterTableSlopeWeight;
            LakesMinDepth = lakesMinDepth;
            LakesMaxDepth = lakesMaxDepth;
            LakesMaxRadius = lakesMaxRadius;
            LakesShelfDepth = lakesShelfDepth;
            LakesFlowSeepageWeight = lakesFlowSeepageWeight;
            LakeOutflowSealWeight = lakeOutflowSealWeight;
            LakeOutflowStabilityWeight = lakeOutflowStabilityWeight;
            CaveCeilingMoistureWeight = caveCeilingMoistureWeight;
            CaveCeilingMoistureClamp = caveCeilingMoistureClamp;
            CaveMoistureFlowClamp = caveMoistureFlowClamp;
            FloodedCaveNoiseFrequency = floodedCaveNoiseFrequency;
            FloodedCaveThreshold = floodedCaveThreshold;
            FloodedCaveProximityWeight = floodedCaveProximityWeight;
            CaveWaterThreshold = caveWaterThreshold;
            CaveLavaThreshold = caveLavaThreshold;
            HydrologyEdgeBlendRadius = hydrologyEdgeBlendRadius;
            HydrologyEdgeVarianceClamp = hydrologyEdgeVarianceClamp;
            HydrologyEdgeNormalizationBlend = hydrologyEdgeNormalizationBlend;
            HydrologyEdgeNormalizationIterations = hydrologyEdgeNormalizationIterations;
            HydrologyFlowMemoryWeight = hydrologyFlowMemoryWeight;
            HydrologyContinuityWeight = hydrologyContinuityWeight;
            RiverMeanderJitter = riverMeanderJitter;
            RiverReliefPenaltyWeight = riverReliefPenaltyWeight;
            RiverAnisotropyDamping = riverAnisotropyDamping;
            RiverBankStabilityClamp = riverBankStabilityClamp;
            RiverSeamFillStrength = riverSeamFillStrength;
            LakeRiverProximitySuppression = lakeRiverProximitySuppression;
            HydrologyFlowShadowWeight = hydrologyFlowShadowWeight;
            HydrologyFlowShadowSlopeWeight = hydrologyFlowShadowSlopeWeight;
            HydrologyPressureBlend = hydrologyPressureBlend;
            HydrologyPressureGradientClamp = hydrologyPressureGradientClamp;
            HydrologyEdgeFlowBias = hydrologyEdgeFlowBias;
            HydrologyEdgeFlowLockWeight = hydrologyEdgeFlowLockWeight;
            RiverFlowAlignmentWeight = riverFlowAlignmentWeight;
            RiverConfluenceBoost = riverConfluenceBoost;
            LakeRimErosionWeight = lakeRimErosionWeight;
            LakeVarianceWeight = lakeVarianceWeight;
            CaveEdgeSealStrength = caveEdgeSealStrength;
            CaveRiverSuppressionWeight = caveRiverSuppressionWeight;
            RiparianCaveGuardWeight = riparianCaveGuardWeight;
            HydrologyReservoirIterations = hydrologyReservoirIterations;
            HydrologyReservoirBlend = hydrologyReservoirBlend;
        }

        public string PipelineVersion { get; }
        public string WorldName { get; }
        public long Seed { get; }
        public string ProtoBaseline { get; }
        public string ProtoComputed { get; }
        public int ProfileVersion { get; }
        public string ProfileHash { get; }
        public string HydrologySignature { get; }
        public int ChunkSize { get; }
        public int WorldHeight { get; }
        public int RenderDistance { get; }
        public int SimulationDistance { get; }
        public int GlobalWaterLevel { get; }
        public int SeaLevel { get; }
        public double FlowPersistence { get; }
        public double FlowGain { get; }
        public double WatershedStitchWeight { get; }
        public int WatershedStitchRadius { get; }
        public int HydrologyGradientStabilityIterations { get; }
        public double HydrologyGradientStabilityBlend { get; }
        public double HydrologyGradientClamp { get; }
        public double HydrologyCurvatureWeight { get; }
        public double HydrologySlopePenalty { get; }
        public double HydrologyWaterTableClampWeight { get; }
        public int HydrologyWaterTableClampRange { get; }
        public double HydrologyWaterTableSlopeWeight { get; }
        public int LakesMinDepth { get; }
        public int LakesMaxDepth { get; }
        public int LakesMaxRadius { get; }
        public int LakesShelfDepth { get; }
        public double LakesFlowSeepageWeight { get; }
        public double LakeOutflowSealWeight { get; }
        public double LakeOutflowStabilityWeight { get; }
        public double CaveCeilingMoistureWeight { get; }
        public double CaveCeilingMoistureClamp { get; }
        public double CaveMoistureFlowClamp { get; }
        public double FloodedCaveNoiseFrequency { get; }
        public double FloodedCaveThreshold { get; }
        public double FloodedCaveProximityWeight { get; }
        public double CaveWaterThreshold { get; }
        public double CaveLavaThreshold { get; }
        public int HydrologyEdgeBlendRadius { get; }
        public double HydrologyEdgeVarianceClamp { get; }
        public double HydrologyEdgeNormalizationBlend { get; }
        public int HydrologyEdgeNormalizationIterations { get; }
        public double HydrologyFlowMemoryWeight { get; }
        public double HydrologyContinuityWeight { get; }
        public int HydrologyReservoirIterations { get; }
        public double HydrologyReservoirBlend { get; }
        public double RiverMeanderJitter { get; }
        public double RiverReliefPenaltyWeight { get; }
        public double RiverAnisotropyDamping { get; }
        public double RiverBankStabilityClamp { get; }
        public double RiverSeamFillStrength { get; }
        public double LakeRiverProximitySuppression { get; }
        public double HydrologyFlowShadowWeight { get; }
        public double HydrologyFlowShadowSlopeWeight { get; }
        public double HydrologyPressureBlend { get; }
        public double HydrologyPressureGradientClamp { get; }
        public double HydrologyEdgeFlowBias { get; }
        public double HydrologyEdgeFlowLockWeight { get; }
        public double RiverFlowAlignmentWeight { get; }
        public double RiverConfluenceBoost { get; }
        public double LakeRimErosionWeight { get; }
        public double LakeVarianceWeight { get; }
        public double CaveEdgeSealStrength { get; }
        public double CaveRiverSuppressionWeight { get; }
        public double RiparianCaveGuardWeight { get; }
    }
}
