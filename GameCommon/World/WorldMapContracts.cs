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
            string worldConfigHash,
            string profileFileHash,
            string hydrologySignature,
            int chunkSize,
            int worldHeight,
            int renderDistance,
            int simulationDistance,
            int globalWaterLevel,
            int seaLevel,
            double flowPersistence,
            double hydrologyCatchmentWeight,
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
            double hydrologyEdgeTangentWeight,
            double riverFlowAlignmentWeight,
            double riverConfluenceBoost,
            double riverTributaryCaptureWeight,
            double riverAvulsionResistance,
            double riverBraidingWeight,
            double lakeRimErosionWeight,
            double lakeVarianceWeight,
            double lakeInflowBlendWeight,
            int lakeOutflowCarveDepth,
            double caveEdgeSealStrength,
            double caveRiverSuppressionWeight,
            double riparianCaveGuardWeight,
            int hydrologyReservoirIterations,
            double hydrologyReservoirBlend,
            double riverEdgeContinuityWeight,
            double lakeOutflowTaper,
            double lakeSpillRetentionWeight,
            double lakeSpillwayContinuityWeight,
            double caveEntranceFlowDampening,
            double caveGroundwaterConnectivityWeight,
            double caveVentilationBias,
            double caveAquiferBarrierWeight,
            double riverNoiseScale,
            int riverIntensitySmoothIterations,
            double riverIntensitySmoothBlend,
            double lakeShorelineBlend,
            double lakeWetlandSaturationThreshold,
            double caveSupportDensity,
            double caveMoistureRetentionWeight,
            double caveCeilingStabilityWeight,
            int previewChunkBudget,
            int previewInflightBudget,
            int previewQueuePressureFactor,
            int previewQueueLimit,
            int previewNearChunkKeepCount,
            double previewQueueLoadSheddingThreshold,
            double previewQueueSlackRatio,
            double previewQueueBurstSlackMultiplier,
            double previewQueueShockAbsorberWeight)
        {
            PipelineVersion = pipelineVersion;
            WorldName = worldName;
            Seed = seed;
            ProtoBaseline = protoBaseline;
            ProtoComputed = protoComputed;
            ProfileVersion = profileVersion;
            ProfileHash = profileHash;
            WorldConfigHash = worldConfigHash;
            ProfileFileHash = profileFileHash;
            HydrologySignature = hydrologySignature;
            ChunkSize = chunkSize;
            WorldHeight = worldHeight;
            RenderDistance = renderDistance;
            SimulationDistance = simulationDistance;
            GlobalWaterLevel = globalWaterLevel;
            SeaLevel = seaLevel;
            FlowPersistence = flowPersistence;
            HydrologyCatchmentWeight = hydrologyCatchmentWeight;
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
            HydrologyEdgeTangentWeight = hydrologyEdgeTangentWeight;
            RiverFlowAlignmentWeight = riverFlowAlignmentWeight;
            RiverConfluenceBoost = riverConfluenceBoost;
            RiverTributaryCaptureWeight = riverTributaryCaptureWeight;
            RiverAvulsionResistance = riverAvulsionResistance;
            RiverBraidingWeight = riverBraidingWeight;
            LakeRimErosionWeight = lakeRimErosionWeight;
            LakeVarianceWeight = lakeVarianceWeight;
            LakeInflowBlendWeight = lakeInflowBlendWeight;
            LakeOutflowCarveDepth = lakeOutflowCarveDepth;
            CaveEdgeSealStrength = caveEdgeSealStrength;
            CaveRiverSuppressionWeight = caveRiverSuppressionWeight;
            RiparianCaveGuardWeight = riparianCaveGuardWeight;
            HydrologyReservoirIterations = hydrologyReservoirIterations;
            HydrologyReservoirBlend = hydrologyReservoirBlend;
            RiverEdgeContinuityWeight = riverEdgeContinuityWeight;
            LakeOutflowTaper = lakeOutflowTaper;
            LakeSpillRetentionWeight = lakeSpillRetentionWeight;
            LakeSpillwayContinuityWeight = lakeSpillwayContinuityWeight;
            CaveEntranceFlowDampening = caveEntranceFlowDampening;
            CaveGroundwaterConnectivityWeight = caveGroundwaterConnectivityWeight;
            CaveVentilationBias = caveVentilationBias;
            CaveAquiferBarrierWeight = caveAquiferBarrierWeight;
            RiverNoiseScale = riverNoiseScale;
            RiverIntensitySmoothIterations = riverIntensitySmoothIterations;
            RiverIntensitySmoothBlend = riverIntensitySmoothBlend;
            LakeShorelineBlend = lakeShorelineBlend;
            LakeWetlandSaturationThreshold = lakeWetlandSaturationThreshold;
            CaveSupportDensity = caveSupportDensity;
            CaveMoistureRetentionWeight = caveMoistureRetentionWeight;
            CaveCeilingStabilityWeight = caveCeilingStabilityWeight;
            PreviewChunkBudget = previewChunkBudget;
            PreviewInflightBudget = previewInflightBudget;
            PreviewQueuePressureFactor = previewQueuePressureFactor;
            PreviewQueueLimit = previewQueueLimit;
            PreviewNearChunkKeepCount = previewNearChunkKeepCount;
            PreviewQueueLoadSheddingThreshold = previewQueueLoadSheddingThreshold;
            PreviewQueueSlackRatio = previewQueueSlackRatio;
            PreviewQueueBurstSlackMultiplier = previewQueueBurstSlackMultiplier;
            PreviewQueueShockAbsorberWeight = previewQueueShockAbsorberWeight;
        }

        public string PipelineVersion { get; }
        public string WorldName { get; }
        public long Seed { get; }
        public string ProtoBaseline { get; }
        public string ProtoComputed { get; }
        public int ProfileVersion { get; }
        public string ProfileHash { get; }
        public string WorldConfigHash { get; }
        public string ProfileFileHash { get; }
        public string HydrologySignature { get; }
        public int ChunkSize { get; }
        public int WorldHeight { get; }
        public int RenderDistance { get; }
        public int SimulationDistance { get; }
        public int GlobalWaterLevel { get; }
        public int SeaLevel { get; }
        public double FlowPersistence { get; }
        public double HydrologyCatchmentWeight { get; }
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
        public double RiverEdgeContinuityWeight { get; }
        public double LakeOutflowTaper { get; }
        public double LakeSpillRetentionWeight { get; }
        public double LakeSpillwayContinuityWeight { get; }
        public double CaveEntranceFlowDampening { get; }
        public double CaveGroundwaterConnectivityWeight { get; }
        public double CaveVentilationBias { get; }
        public double CaveAquiferBarrierWeight { get; }
        public double RiverNoiseScale { get; }
        public int RiverIntensitySmoothIterations { get; }
        public double RiverIntensitySmoothBlend { get; }
        public double LakeShorelineBlend { get; }
        public double LakeWetlandSaturationThreshold { get; }
        public double CaveSupportDensity { get; }
        public double CaveMoistureRetentionWeight { get; }
        public double CaveCeilingStabilityWeight { get; }
        public int PreviewChunkBudget { get; }
        public int PreviewInflightBudget { get; }
        public int PreviewQueuePressureFactor { get; }
        public int PreviewQueueLimit { get; }
        public int PreviewNearChunkKeepCount { get; }
        public double PreviewQueueLoadSheddingThreshold { get; }
        public double PreviewQueueSlackRatio { get; }
        public double PreviewQueueBurstSlackMultiplier { get; }
        public double PreviewQueueShockAbsorberWeight { get; }
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
        public double HydrologyEdgeTangentWeight { get; }
        public double RiverFlowAlignmentWeight { get; }
        public double RiverConfluenceBoost { get; }
        public double RiverTributaryCaptureWeight { get; }
        public double RiverAvulsionResistance { get; }
        public double RiverBraidingWeight { get; }
        public double LakeRimErosionWeight { get; }
        public double LakeVarianceWeight { get; }
        public double LakeInflowBlendWeight { get; }
        public int LakeOutflowCarveDepth { get; }
        public double CaveEdgeSealStrength { get; }
        public double CaveRiverSuppressionWeight { get; }
        public double RiparianCaveGuardWeight { get; }
    }
}
