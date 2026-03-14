using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Lake basin mask generator that blends hydrology, flow, and river suppression.
    /// </summary>
    public sealed class ImprovedLakeGenerator
    {
        private readonly LakeConfig lakeConfig;
        private readonly WaterConfig waterConfig;
        private readonly int worldSeedHash;

        public ImprovedLakeGenerator(LakeConfig lakeConfig, WaterConfig waterConfig, long worldSeed)
        {
            this.lakeConfig = lakeConfig ?? throw new ArgumentNullException(nameof(lakeConfig));
            this.waterConfig = waterConfig ?? throw new ArgumentNullException(nameof(waterConfig));
            worldSeedHash = (int)(worldSeed ^ 0x1A2E0001);
        }

        public float[,] BuildMask(
            int chunkX,
            int chunkZ,
            int chunkSize,
            int[,] heightMap,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,]? riverMask,
            float[,] erosionRisk,
            int seaLevel)
        {
            var lakes = new float[chunkSize, chunkSize];
            double flowShadowWeight = Math.Clamp(waterConfig.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(waterConfig.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double flowSeepageWeight = Math.Clamp(lakeConfig.FlowSeepageWeight, 0.0, 1.0);
            double watershedBlend = Math.Clamp(waterConfig.HydrologyWatershedStitchWeight, 0.0, 1.0);
            int watershedRadius = Math.Max(1, waterConfig.HydrologyWatershedStitchRadius);
            double flowMemoryWeight = Math.Clamp(waterConfig.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double varianceWeight = Math.Clamp(lakeConfig.VarianceWeight, 0.0, 1.0);
            double outflowStabilityWeight = Math.Clamp(lakeConfig.OutflowStabilityWeight, 0.0, 1.0);
            double spillwayContinuityWeight = Math.Clamp(lakeConfig.SpillwayContinuityWeight, 0.0, 1.0);
            double outflowSealWeight = Math.Clamp(lakeConfig.OutflowSealWeight, 0.0, 1.0);
            double edgeNormalizationStrength = Math.Clamp(waterConfig.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double waterTableClampWeight = Math.Clamp(waterConfig.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double waterTableClampRange = Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange);
            double waterTableSlopeWeight = Math.Clamp(waterConfig.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            int minDepth = Math.Max(1, lakeConfig.MinDepth);
            int maxDepth = Math.Max(minDepth, lakeConfig.MaxDepth);
            int shelfDepth = Math.Max(0, lakeConfig.ShelfDepth);
            double rimErosionWeight = Math.Clamp(waterConfig.LakeRimErosionWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(waterConfig.HydrologyFlowPersistence, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            double edgeTangentWeight = Math.Clamp(waterConfig.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double reservoirBlend = Math.Clamp(waterConfig.HydrologyReservoirBlend, 0.0, 1.0);
            double spillwayErosionGuardWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.55 +
                lakeConfig.OutflowStabilityWeight * 0.45,
                0.0,
                1.0);
            double floodplainRetentionWeight = Math.Clamp(
                lakeConfig.FlowSeepageWeight * 0.5 +
                waterConfig.HydrologyFlowPersistence * 0.5,
                0.0,
                1.0);
            double terraceBiasWeight = Math.Clamp(lakeConfig.TerraceBiasWeight, 0.0, 1.0);
            double spillRetentionWeight = Math.Clamp(lakeConfig.SpillRetentionWeight, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double basinNoise = SimplexNoise.Generate(worldX * 0.004, worldZ * 0.004, 1.0, 3, 1.0, 0.6, CreateNoiseSeed(chunkX, chunkZ, x, z, 211));
                    double rimNoise = SimplexNoise.Generate(worldX * 0.009 + 31, worldZ * 0.009 + 17, 1.0, 2, 1.0, 0.55, CreateNoiseSeed(chunkX, chunkZ, x, z, 223));
                    double macroNoise = SimplexNoise.Generate(worldX * 0.0017 - 37.0, worldZ * 0.0017 + 23.0, 1.0, 2, 1.0, 0.6, CreateNoiseSeed(chunkX, chunkZ, x, z, 227));
                    double detailNoise = Math.Abs(SimplexNoise.Generate(worldX * 0.0065 + 3.0, worldZ * 0.0065 - 5.0, 1.0, 2, 1.0, 0.55, CreateNoiseSeed(chunkX, chunkZ, x, z, 229)));
                    double hydrology = hydrologyMask[x, z];
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double flowMemory = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double riverSuppression = riverMask != null ? riverMask[x, z] * lakeConfig.RiverProximitySuppression : 0.0;
                    double inflowBlend = riverMask != null ? riverMask[x, z] * waterConfig.LakeInflowBlendWeight : 0.0;
                    double reliefPenalty = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
                    double radiusFalloff = Math.Clamp(edgeDistance / (double)Math.Max(1, lakeConfig.MaxRadius), 0.0, 1.0);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrology);
                    double hydrologyVariance = TerrainMaskUtility.SampleVariance(hydrologyMask, x, z);
                    double curvature = ComputeCurvature(heightMap, x, z);
                    double terracePotential = Math.Clamp(
                        (Math.Max(0.0, -curvature) + (1.0 - Math.Clamp(slope * 0.08, 0.0, 0.95))) * 0.5,
                        0.0,
                        1.0);
                    double flowVariance = TerrainMaskUtility.SampleVariance(flowAccumulation, x, z);
                    double edgeNormalization = edgeNormalizationStrength * (1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0));
                    double flowShadow = Math.Clamp(
                        flow * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5,
                        0.0,
                        0.7);
                    double flowGradient = Math.Abs(flowMemory - flow);
                    double divergencePenalty = Math.Min(1.0, flowGradient / divergenceClamp);
                    double pressureGradient = Math.Abs(hydrologyGradient - flowGradient);
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * waterConfig.HydrologyEdgeStabilityWeight * 0.35, 0.0, 0.5);
                    double seamContinuityBias = 1.0 + Math.Clamp((seamHydro + interiorFlow + hydrology) * waterConfig.HydrologyEdgeFluxBlend * 0.15, -0.35, 0.35);
                    double shorelineJitter = Math.Abs(SimplexNoise.Generate(
                        worldX * 0.0025 + 7.0,
                        worldZ * 0.0025 - 13.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 233))) * lakeConfig.ShorelineBlend * 0.25;

                    double depthBelowSea = seaLevel - heightMap[x, z];
                    double depthPenalty = Math.Clamp(Math.Max(0.0, minDepth - depthBelowSea) / Math.Max(1.0, minDepth), 0.0, 1.0);
                    double waterClamp = 1.0 + Math.Clamp(1.0 - Math.Abs(depthBelowSea) / waterTableClampRange, 0.0, 1.0) * waterTableClampWeight * (depthBelowSea >= 0 ? 0.45 : -0.25);
                    double waterSlopePenalty = Math.Clamp(slope * waterTableSlopeWeight * 0.05, 0.0, 0.45);
                    double wetness = hydrology * 0.65 + flow * 0.35;
                    double rimWeight = 0.25 + Math.Clamp(waterConfig.HydrologyVarianceBlend, 0.0, 1.0) * 0.2;
                    double layeredNoise = (basinNoise * 0.42) + (rimNoise * rimWeight) + (macroNoise * 0.2) + (detailNoise * 0.15);
                    double weight = layeredNoise + wetness * 0.4 + lakeConfig.SpawnWeightBias;
                    weight += inflowBlend * 0.35 * (1.0 - flowShadow * 0.5);
                    double riparianCohesion = Math.Clamp((hydrology + seamHydro) * waterConfig.RiparianSaturationBoost * 0.5, 0.0, 0.65);
                    double seamAnchor = (hydrology + seamHydro + flow + flowMemory) * 0.25;
                    double seamMemory = (hydrology + seamHydro + flowMemory) * 0.333;
                    double catchmentMemory = Math.Clamp((flow + flowMemory + seamHydro) * waterConfig.HydrologyFlowMemoryWeight * 0.1, 0.0, 0.25);
                    double flowSeepageContinuity = 1.0 + (seamHydro + flowMemory * flowMemoryWeight + seamMemory) * flowSeepageWeight * 0.15;
                    double seepage = (flow + hydrologyGradient + flowMemory * 0.5 * flowMemoryWeight + seamMemory * 0.35) * flowSeepageWeight;
                    double memoryCohesion = (seamMemory + flowMemory) * flowPersistence * 0.15;
                    double momentumAssist = (seamHydro + flowMemory) * flowPersistence * 0.08;
                    weight += hydrologyVariance * varianceWeight * (1.0 - flowShadow * 0.5);
                    weight += seepage * (1.0 - flowShadow * 0.5);
                    weight += riparianCohesion * (1.0 - flowShadow * 0.35);
                    weight += memoryCohesion * (1.0 - flowShadow * 0.5);
                    weight += momentumAssist * (1.0 - divergencePenalty * 0.35);
                    double varianceAssist = Math.Clamp((hydrologyVariance + flowVariance) * waterConfig.HydrologyVarianceBlend * 0.1, -0.25, 0.35);
                    weight -= slope * waterConfig.LakeRimErosionWeight * 0.05;
                    weight -= hydrologyGradient * waterConfig.HydrologyEdgeStabilityWeight * 0.25;
                    weight -= riverSuppression * 0.5;
                    weight -= reliefPenalty * waterConfig.RiverReliefPenaltyWeight;
                    weight -= erosion * rimErosionWeight * 0.25;
                    weight += seamAnchor * edgeNormalization * 0.25;
                    weight += shorelineJitter * (1.0 - flowShadow * 0.5);
                    double basinAssist = Math.Clamp(curvature * waterConfig.HydrologyCurvatureWeight * 0.25, -0.45, 0.45);
                    double ridgePenalty = Math.Max(0.0, -basinAssist);
                    weight *= 1.0 + Math.Max(0.0, basinAssist) * 0.4;
                    weight *= 1.0 - Math.Clamp(ridgePenalty * 0.55, 0.0, 0.45);
                    weight *= 1.0 - divergencePenalty * 0.25;
                    double pressureStabilizer = 1.0 - Math.Clamp(
                        (pressureGradient / Math.Max(0.0001, waterConfig.HydrologyPressureGradientClamp)) * Math.Clamp(waterConfig.HydrologyPressureBlend, 0.0, 1.0),
                        0.0,
                        0.45);
                    weight *= Math.Max(0.55, pressureStabilizer);
                    weight *= Math.Max(0.55, waterClamp);
                    weight *= 1.0 - waterSlopePenalty;
                    weight *= 1.0 - depthPenalty * 0.6;
                    weight *= 1.0 + varianceAssist;
                    weight *= 0.75 + radiusFalloff * 0.25;
                    double slopePenalty = Math.Clamp(slope * waterConfig.HydrologyGradientWeight * 0.08, 0.0, 0.35);
                    weight *= 1.0 - slopePenalty;
                    double basinStability = 1.0 - Math.Clamp(hydrologyGradient * waterConfig.HydrologyEdgeStabilityWeight * 0.4 + slopePenalty * 0.85 + reliefPenalty * 0.35, 0.0, 0.65);
                    basinStability *= 1.0 - Math.Clamp(erosion * rimErosionWeight * 0.4, 0.0, 0.4);
                    weight *= basinStability;
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, chunkSize - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, chunkSize - 1);
                    double downhillHydro = hydrologyMask[downX, downZ];
                    double downhillFlow = flowAccumulation[downX, downZ] / 6.0;
                    double downhillBias = Math.Abs(downhill.X) + Math.Abs(downhill.Z);
                    double outflowAnchor = (downhillHydro + downhillFlow) * outflowStabilityWeight * 0.25;
                    double stabilitySeal = 1.0 + outflowSealWeight * (1.0 - divergencePenalty) * 0.35;
                    outflowAnchor *= (1.0 + downhillBias * 0.05) * stabilitySeal;
                    weight += outflowAnchor * (1.0 - flowShadow * 0.5);
                    double catchmentConnectivity = Math.Clamp((seamHydro + flowMemory + downhillFlow) / 3.0, 0.0, 1.2);
                    double connectivityAssist = catchmentConnectivity *
                        (waterConfig.RiverConfluenceBoost * 0.12 + outflowStabilityWeight * 0.2);
                    weight += connectivityAssist * (1.0 - flowShadow * 0.35);
                    weight *= 1.0 + catchmentConnectivity * spillwayContinuityWeight * 0.08;
                    weight *= 1.0 - Math.Clamp(flowGradient * spillwayContinuityWeight * 0.18, 0.0, 0.25);
                    weight *= 1.0 - Math.Clamp(Math.Abs(catchmentConnectivity - wetness) * 0.15, 0.0, 0.25);
                    double flowMemoryGradient = Math.Abs(flowMemory - flow);
                    weight *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + hydrologyGradient * 0.1 + flowMemoryGradient * 0.15, 0.0, 0.35);
                    double spillwayErosionGuard = Math.Clamp(
                        (1.0 - erosion) *
                        (seamHydro * 0.4 + flowMemory * 0.35 + catchmentConnectivity * 0.25) *
                        spillwayErosionGuardWeight * 0.25,
                        0.0,
                        0.3);
                    double floodplainRetention = Math.Clamp(
                        (hydrology + seamHydro + flow + flowMemory) * 0.25 *
                        floodplainRetentionWeight * 0.22,
                        0.0,
                        0.25);
                    weight += spillwayErosionGuard;
                    weight *= 1.0 + floodplainRetention * (1.0 - flowShadow * 0.4);
                    weight += terracePotential * terraceBiasWeight * 0.12 * (1.0 - flowShadow * 0.35);
                    double seamCushion = 1.0 + Math.Clamp((seamHydro - hydrology) * waterConfig.HydrologyEdgeFluxBlend, -0.2, 0.3);
                    weight *= seamCushion * seamGuard * seamContinuityBias * flowSeepageContinuity;
                    double divergenceBrake = Math.Min(1.0, Math.Abs(flowMemory - seamHydro) / divergenceClamp);
                    weight *= 1.0 - Math.Clamp(divergenceBrake * reservoirBlend, 0.0, 0.25);
                    weight = weight * (1.0 - reservoirBlend * 0.2) + (weight + catchmentMemory) * reservoirBlend * 0.2;
                    double spillRetention = Math.Clamp(
                        (catchmentConnectivity + seamMemory + (1.0 - erosion)) * 0.333,
                        0.0,
                        1.0);
                    weight *= 1.0 + spillRetention * spillRetentionWeight * 0.08;
                    weight *= 1.0 - divergencePenalty * spillRetentionWeight * 0.06;
                    double alluvialRechargeRelay = Math.Clamp(
                        (catchmentConnectivity + seamMemory + spillRetention) * 0.333,
                        0.0,
                        1.2);
                    double aquiferLatch = Math.Clamp(
                        (hydrology + seamHydro + flowMemory) * lakeConfig.FlowSeepageWeight * 0.18,
                        0.0,
                        0.3);
                    weight *= 1.0 + alluvialRechargeRelay * spillRetentionWeight * 0.06;
                    weight *= 1.0 - aquiferLatch * Math.Clamp(erosion + slope * 0.1, 0.0, 1.0) * 0.2;
                    weight += alluvialRechargeRelay * (1.0 - flowShadow) * 0.02;
                    weight *= 1.0 - flowShadow * Math.Clamp(0.35 - outflowSealWeight * 0.1, 0.05, 0.35);
                    weight *= 1.0 + riparianCohesion * 0.15;
                    double flowBridge = (hydrology + seamHydro + flowMemory) * waterConfig.HydrologyEdgeFlowBias * 0.1;
                    double flowLock = Math.Clamp(waterConfig.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
                    double directionalAssist = 1.0 + (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * Math.Clamp(waterConfig.HydrologyDirectionalBlend, 0.0, 1.0) * 0.1;
                    weight = weight * (1.0 - flowLock * 0.15) + (weight * directionalAssist + seamMemory * flowLock) * 0.15;
                    weight *= 1.0 + flowBridge;
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double edgeSeamAnchor = hydrology * 0.3 + seamHydro * 0.25 + flow * 0.2 + inflowBlend * 0.15 + flowMemory * 0.1;
                        weight = weight * (1.0 - edgeRepair * 0.4) + edgeSeamAnchor * edgeRepair;
                    }

                    double seamRelax = Math.Clamp(waterConfig.HydrologySeamRelaxBlend, 0.0, 1.0);
                    double edgeTangentGuard = Math.Clamp(edgeNormalization * edgeTangentWeight * 0.25, 0.0, 0.25);
                    weight = weight * (1.0 - edgeNormalization * 0.2) + (seamAnchor + seamMemory * 0.35) * edgeNormalization * 0.25;
                    weight *= 1.0 - edgeTangentGuard;
                    weight = weight * (1.0 - seamRelax * 0.1) + seamAnchor * seamRelax * 0.05;
                    double wetlandThreshold = lakeConfig.WetlandSaturationThreshold - wetness * 0.1 - edgeNormalization * 0.05 - seamRelax * 0.05 - riparianCohesion * 0.08;
                    if (weight > wetlandThreshold && depthBelowSea <= maxDepth && depthBelowSea >= -shelfDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            double lakeContinuityWeight = Math.Clamp(
                waterConfig.HydrologyContinuityWeight * 0.78 +
                waterConfig.HydrologyThalwegStabilityWeight * 0.22,
                0.0,
                1.5);

            TerrainMaskUtility.ApplyHydrologyContinuity(
                lakes,
                hydrologyMask,
                flowAccumulation,
                waterConfig.HydrologyEdgeBlendRadius,
                lakeContinuityWeight);
            TerrainMaskUtility.ClampVariance(lakes, waterConfig.HydrologyVarianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(
                lakes,
                waterConfig.HydrologyEdgeBlendRadius,
                Math.Max(0.05, waterConfig.HydrologySeamRelaxBlend * 0.35),
                waterConfig.HydrologyEdgeVarianceClamp);
            TerrainMaskUtility.ApplyGradientStability(
                lakes,
                waterConfig.HydrologyGradientStabilityIterations,
                waterConfig.HydrologyGradientStabilityBlend * 0.5,
                waterConfig.HydrologyGradientClamp);
            TerrainMaskUtility.ClampVariance(lakes, waterConfig.HydrologyVarianceClamp);
            TerrainMaskUtility.Smooth2D(lakes, lakeConfig.LakeBasinSmoothIterations, waterConfig.HydrologySmoothBlend);
            TerrainMaskUtility.StitchEdges(lakes, waterConfig.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.FillBasins(lakes, Math.Max(0.05, waterConfig.HydrologyEdgeStabilityWeight * 0.35), Math.Max(1, waterConfig.HydrologySeamRelaxIterations));
            TerrainMaskUtility.RelaxEdges(lakes, waterConfig.HydrologySeamRelaxIterations, waterConfig.HydrologySeamRelaxBlend);
            TerrainMaskUtility.NormalizeEdges(
                lakes,
                waterConfig.HydrologyEdgeBlendRadius,
                waterConfig.HydrologyEdgeNormalizationIterations,
                waterConfig.HydrologyEdgeNormalizationBlend);
            ApplyOutflowTaper(
                lakes,
                flowAccumulation,
                waterConfig.HydrologyEdgeBlendRadius,
                lakeConfig.LakeOutflowTaper,
                outflowStabilityWeight);
            ApplyRiparianEdgeFeather(lakes, hydrologyMask, flowAccumulation);
            ApplyLakeShelves(lakes, heightMap, seaLevel, shelfDepth, maxDepth);
            ApplyWetlandBuffer(lakes, Math.Min(lakeConfig.WetlandBufferRadius, lakeConfig.MaxRadius), lakeConfig.ShorelineBlend);
            ApplyOutflowChannels(lakes, heightMap, flowAccumulation, waterConfig.LakeInflowBlendWeight, lakeConfig.OutflowCarveDepth, outflowStabilityWeight);
            ApplySpillwayContinuity(lakes, heightMap, flowAccumulation, riverMask, spillwayContinuityWeight);
            ApplyCatchmentSpillwayStitch(lakes, hydrologyMask, flowAccumulation, riverMask, spillwayContinuityWeight);
            ApplyLakeMouthStability(lakes, riverMask, flowAccumulation, heightMap, seaLevel);
            ApplyBasinRetentionLock(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplySpillwayErosionDamping(lakes, hydrologyMask, heightMap, flowAccumulation, riverMask);
            ApplyBackwaterRetentionBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyFloodplainTerraceBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplySpillbackBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel, chunkX, chunkZ);
            ApplyTerraceBackfillBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyDeltaBackswampRetentionBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyLagoonOverflowBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel, chunkX, chunkZ);
            ApplyKarstOverflowRetentionBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyOxbowRetentionAnchorBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplySpillwayRetentionAnchorBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyFloodplainRetentionShelfBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplySpillwayBackflowDampingBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyWetlandLeakageClampBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyKarstOutletStabilityBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyAlluvialBackwaterLinkBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyFloodplainRetentionClampBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplySeasonalFloodplainRechargeBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel, chunkX, chunkZ);
            ApplyFloodplainStorageSpillBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyGroundwaterLatchBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, seaLevel);
            ApplyRiparianFloodplainLinkBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyBackwaterLagoonExchangeBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyKarstFloodplainRetentionRelayBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyAlluvialGroundwaterExchangeBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyPerchedFloodplainCascadeBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyKarstFloodplainSpillRelayBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyFloodplainSpillwayBalancingBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplySubsurfaceOverflowBalancingBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyHyporheicStorageBalancingBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyPhreaticResonanceStorageBridge(lakes, hydrologyMask, flowAccumulation, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            return lakes;
        }

        private void ApplyPhreaticResonanceStorageBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.35 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                waterConfig.HydrologyWaterTableClampWeight * 0.31,
                0.0,
                1.3);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.62);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.35);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.35);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 7.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double riverAssist = riverMask != null ? Math.Clamp(riverMask[x, z], 0.0f, 1.0f) * 0.18 : 0.0;
                    double resonanceNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0031 - 29.0,
                        (chunkZ * sizeZ + z) * 0.0031 + 11.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 457)));
                    double resonance = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 * 0.52 +
                        floodplainBand * 0.21 +
                        resonanceNoise * 0.19 +
                        riverAssist,
                        0.0,
                        1.3);
                    resonance *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.013 + relief * 0.3 + divergence * 0.23,
                        0.0,
                        0.86);
                    if (resonance <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.OutflowStabilityWeight * 0.08), resonance * 0.17);
                    double target = lake * (1.0 - bridgeWeight * 0.1) + (lake + resonance) * bridgeWeight * 0.1;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyHyporheicStorageBalancingBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                lakeConfig.FlowSeepageWeight * 0.34 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.25);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 3);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 8.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double bridgeNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 163.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 97.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1999)));
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.25);
                    double bridgeSignal = Math.Clamp(
                        lake * 0.34 +
                        hydro * 0.18 +
                        seamHydro * 0.15 +
                        flowNode * 0.15 +
                        seamFlow * 0.1 +
                        river * 0.04 +
                        floodplainBand * 0.04,
                        0.0,
                        1.35);
                    bridgeSignal += continuity * 0.05;
                    bridgeSignal *= 1.0 + Math.Clamp((bridgeNoise - 0.5) * 0.2, -0.15, 0.15);
                    bridgeSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.31 + divergence * 0.24,
                        0.0,
                        0.86);
                    if (bridgeSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.OutflowStabilityWeight * 0.08), continuity * 0.17);
                    double target = lake * (1.0 - bridgeWeight * 0.12) + (lake + bridgeSignal) * bridgeWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyKarstFloodplainSpillRelayBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                waterConfig.RiverDeltaWetlandStrength * 0.34 +
                lakeConfig.FlowSeepageWeight * 0.30,
                0.0,
                1.25);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    if (lake <= 0.01 && river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.35);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.35);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, waterConfig.RiverMouthSmoothRadius * 1.9),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double relayNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 67.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 31.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1471)));

                    double relaySignal = Math.Clamp(
                        lake * 0.34 +
                        river * 0.2 +
                        hydro * 0.16 +
                        seamHydro * 0.12 +
                        flowNode * 0.1 +
                        seamFlow * 0.08,
                        0.0,
                        1.35);
                    relaySignal *= 1.0 + Math.Clamp((relayNoise - 0.5) * waterConfig.RiverMeanderJitter * 0.2, -0.16, 0.16);
                    relaySignal *= 1.0 + floodplainBand * 0.16;
                    relaySignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.013 + relief * 0.32 + divergence * 0.24,
                        0.0,
                        0.86);
                    if (relaySignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.SpillwayContinuityWeight * 0.08), relaySignal * 0.2);
                    double target = lake * (1.0 - relayWeight * 0.12) + (lake + relaySignal) * relayWeight * 0.12;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyBackwaterLagoonExchangeBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double exchangeWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.37 +
                lakeConfig.FlowSeepageWeight * 0.33 +
                waterConfig.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.25);
            if (exchangeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 3);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    if (lake <= 0.01 && river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + waterConfig.RiverMouthSmoothRadius + 2.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double jitter = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 53.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 31.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 997)));

                    double exchangeSignal = Math.Clamp(
                        lake * 0.29 +
                        river * 0.13 +
                        hydro * 0.19 +
                        seamHydro * 0.16 +
                        flowNode * 0.14 +
                        seamFlow * 0.09,
                        0.0,
                        1.35);
                    exchangeSignal += floodplainBand * 0.05;
                    exchangeSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 +
                        relief * 0.34 +
                        divergence * 0.24,
                        0.0,
                        0.82);
                    exchangeSignal *= 1.0 + Math.Clamp((jitter - 0.5) * waterConfig.RiverMeanderJitter * 0.14, -0.14, 0.14);

                    if (exchangeSignal <= 0.01)
                    {
                        continue;
                    }

                    double stabilize = exchangeSignal * exchangeWeight;
                    double floor = Math.Max(lake * (0.84 + lakeConfig.OutflowStabilityWeight * 0.12), exchangeSignal * 0.16);
                    double target = lake * (1.0 - stabilize * 0.1) + (lake + exchangeSignal * 0.22 + river * 0.08) * stabilize * 0.1;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyRiparianFloodplainLinkBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double linkWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.35 +
                waterConfig.HydrologyFlowPersistence * 0.33 +
                waterConfig.LakeInflowBlendWeight * 0.32,
                0.0,
                1.25);
            if (linkWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.55);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    if (lake <= 0.01 && river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 6.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + waterConfig.HydrologyEdgeBlendRadius + 2.0),
                        0.0,
                        1.0);
                    double convergence = Math.Clamp(
                        Math.Max(0.0, seamFlow - flowNode) * 0.55 +
                        Math.Max(0.0, seamHydro - hydro) * 0.45,
                        0.0,
                        1.15);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double jitter = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0021 + 13.0,
                        (chunkZ * sizeZ + z) * 0.0021 - 21.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 991)));

                    double linkSignal = Math.Clamp(
                        lake * 0.26 +
                        river * 0.12 +
                        hydro * 0.18 +
                        seamHydro * 0.15 +
                        flowNode * 0.14 +
                        seamFlow * 0.1 +
                        floodplainBand * 0.05,
                        0.0,
                        1.35);
                    linkSignal += convergence * 0.06;
                    linkSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 +
                        relief * 0.34 +
                        divergence * 0.24,
                        0.0,
                        0.82);
                    linkSignal *= 1.0 + Math.Clamp((jitter - 0.5) * waterConfig.RiverMeanderJitter * 0.16, -0.16, 0.16);

                    if (linkSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.OutflowStabilityWeight * 0.1), linkSignal * 0.18);
                    double target = lake * (1.0 - linkWeight * 0.12) + (lake + linkSignal) * linkWeight * 0.12;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyFloodplainStorageSpillBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double storageWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.35 +
                lakeConfig.SpillwayContinuityWeight * 0.35 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.25);
            if (storageWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.5);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    if (lake <= 0.001 && river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNormalized = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.0);
                    double seamFlowNormalized = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNormalized - seamFlowNormalized) / divergenceScale);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 4.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + waterConfig.HydrologyEdgeBlendRadius),
                        0.0,
                        1.0);

                    double storageSignal = Math.Clamp(
                        lake * 0.26 +
                        river * 0.16 +
                        hydro * 0.2 +
                        seamHydro * 0.14 +
                        flowNormalized * 0.12 +
                        floodplainBand * 0.12,
                        0.0,
                        1.25);
                    storageSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.016 +
                        relief * 0.35 +
                        divergence * 0.22,
                        0.0,
                        0.8);

                    if (storageSignal <= 0.02)
                    {
                        continue;
                    }

                    double reinforce = storageSignal * storageWeight;
                    double target = lake * (1.0 - reinforce * 0.09) +
                                    (lake + seamHydro * 0.08 + river * 0.06 + floodplainBand * 0.06) * reinforce * 0.09;
                    target = Math.Max(target, lake + reinforce * 0.028 * (1.0 - Math.Clamp(slope, 0.0, 1.0)));
                    lakes[x, z] = (float)Math.Clamp(target, 0.0, 1.0);

                    if (reinforce <= 0.3)
                    {
                        continue;
                    }

                    int targetX = x;
                    int targetZ = z;
                    int center = heightMap[x, z];
                    int bestDrop = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if ((dx == 0 && dz == 0) || (dx != 0 && dz != 0))
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            int drop = center - heightMap[nx, nz];
                            if (drop > bestDrop)
                            {
                                bestDrop = drop;
                                targetX = nx;
                                targetZ = nz;
                            }
                        }
                    }

                    if (targetX == x && targetZ == z)
                    {
                        continue;
                    }

                    double neighbor = lakes[targetX, targetZ];
                    lakes[targetX, targetZ] = (float)Math.Clamp(Math.Max(neighbor, neighbor + reinforce * 0.015), 0.0, 1.0);
                }
            }
        }

        private void ApplyGroundwaterLatchBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double latchWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.36 +
                waterConfig.HydrologyFlowMemoryWeight * 0.34 +
                lakeConfig.SpillwayContinuityWeight * 0.30,
                0.0,
                1.0);
            if (latchWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.04)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double waterTableBand = Math.Clamp(
                        (seaLevel + lakeConfig.OutflowCarveDepth - heightMap[x, z]) / Math.Max(8.0, waterConfig.HydrologyWaterTableClampRange),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double latchSignal = Math.Clamp(
                        hydro * 0.26 +
                        seamHydro * 0.24 +
                        flowNode * 0.2 +
                        seamFlow * 0.18 +
                        waterTableBand * 0.12,
                        0.0,
                        1.25);
                    latchSignal *= 1.0 - Math.Clamp(slope * 0.026 + relief / 46.0 + divergence * 0.28, 0.0, 0.84);
                    if (latchSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.OutflowSealWeight * 0.08), latchSignal * 0.18);
                    double target = lake * (1.0 - latchWeight * 0.13) + (lake + latchSignal) * latchWeight * 0.13;
                    if (river > 0.52)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.52) * 0.14, 0.0, 0.14);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyFloodplainRetentionClampBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.42 +
                lakeConfig.SpillwayContinuityWeight * 0.33 +
                waterConfig.HydrologyFlowPersistence * 0.25,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int edgeRadius = Math.Max(2, waterConfig.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double floodplainBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, waterConfig.RiverMouthSmoothRadius * 2.5), 0.0, 1.0);

                    double retention = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 +
                        river * 0.2 +
                        floodplainBand * 0.2,
                        0.0,
                        1.35);
                    retention *= 1.0 - Math.Clamp(divergence * 0.4 + slope * waterConfig.HydrologySlopePenalty * 0.01, 0.0, 0.75);
                    retention *= 1.0 + edgeBand * 0.12;

                    double clamp = retentionWeight * (0.11 + edgeBand * 0.09 + floodplainBand * 0.08);
                    double target = lake * (1.0 - clamp) + (lake + retention * 0.12) * clamp;
                    double floor = Math.Max(lake * (0.82 + lakeConfig.SpillRetentionWeight * 0.08), retention * 0.05);
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.0);
                }
            }
        }

        private void ApplySeasonalFloodplainRechargeBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.36 +
                waterConfig.HydrologyFlowPersistence * 0.34 +
                lakeConfig.OutflowStabilityWeight * 0.3,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int edgeRadius = Math.Max(2, waterConfig.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double floodplainBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, waterConfig.RiverMouthSmoothRadius * 2.6), 0.0, 1.0);
                    int seed = ComputeSeasonalSeed(chunkX, chunkZ, x, z);
                    double seasonalNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0185 + 13.0,
                        (chunkZ * sizeZ + z) * 0.0185 - 41.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        seed));
                    double recharge = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 +
                        river * 0.18 +
                        floodplainBand * 0.14 +
                        seasonalNoise * 0.28,
                        0.0,
                        1.4);
                    if (recharge <= 0.22)
                    {
                        continue;
                    }

                    double rechargeGuard = 1.0 - Math.Clamp(divergence * 0.4 + slope * waterConfig.HydrologySlopePenalty * 0.01, 0.0, 0.76);
                    double pulse = recharge * bridgeWeight * rechargeGuard * (0.1 + edgeBand * 0.08 + floodplainBand * 0.1);
                    double floor = Math.Max(lake * (0.84 + lakeConfig.SpillRetentionWeight * 0.08), recharge * 0.04);
                    double target = lake * (1.0 - bridgeWeight * 0.15) + (lake + pulse) * bridgeWeight * 0.15;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.0);
                }
            }
        }

        private static int ComputeSeasonalSeed(int chunkX, int chunkZ, int localX, int localZ)
        {
            unchecked
            {
                int hash = 0x5B2D9157;
                hash = (hash * 397) ^ chunkX;
                hash = (hash * 397) ^ chunkZ;
                hash = (hash * 397) ^ localX;
                hash = (hash * 397) ^ localZ;
                return hash;
            }
        }

        private void ApplyKarstOverflowRetentionBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double couplingWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.4 +
                lakeConfig.OutflowStabilityWeight * 0.35 +
                waterConfig.HydrologyCatchmentWeight * 0.25,
                0.0,
                1.0);
            if (couplingWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int edgeRadius = Math.Max(2, waterConfig.HydrologyEdgeBlendRadius);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    if (lake <= 0.03 && river <= 0.05)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = flow[x, z] / 6.0;
                    double seamFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double depthBias = Math.Clamp((seaLevel - heightMap[x, z]) / Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange), -0.6, 1.0);
                    double wetPocket = Math.Clamp(
                        hydro * 0.35 +
                        seamHydro * 0.25 +
                        flowNode * 0.2 +
                        seamFlow * 0.1 +
                        river * 0.1,
                        0.0,
                        1.2);

                    double retention = wetPocket * (0.18 + couplingWeight * 0.35);
                    retention *= 1.0 - Math.Clamp(slope * 0.03 + relief / 34.0, 0.0, 0.8);
                    retention *= 1.0 + Math.Max(0.0, depthBias) * 0.25;

                    double target = lake * (1.0 - couplingWeight * 0.12) +
                        (lake + retention) * couplingWeight * 0.12;
                    if (river > 0.4)
                    {
                        target *= 1.0 - Math.Clamp(river * 0.12, 0.0, 0.25);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyOxbowRetentionAnchorBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double anchorWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                lakeConfig.FlowSeepageWeight * 0.3,
                0.0,
                1.0);
            if (anchorWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    if (lake <= 0.04 && river <= 0.08)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.2);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.2);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 9 - heightMap[x, z]) / 15.0, 0.0, 1.0);
                    double oxbowPotential = Math.Clamp(
                        hydro * 0.3 +
                        seamHydro * 0.2 +
                        flowNode * 0.16 +
                        seamFlow * 0.16 +
                        Math.Max(0.0, lake - river) * 0.18,
                        0.0,
                        1.2);

                    double retention = oxbowPotential * (0.2 + anchorWeight * 0.32 + floodplainBias * 0.2);
                    retention *= 1.0 - Math.Clamp(slope * 0.028 + relief / 34.0 + Math.Abs(flowNode - seamFlow) * 0.26, 0.0, 0.82);
                    if (retention <= 0.01)
                    {
                        continue;
                    }

                    double target = lake * (1.0 - anchorWeight * 0.14) +
                        (lake + retention) * anchorWeight * 0.14;
                    if (river > 0.52)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.52) * 0.16, 0.0, 0.18);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplySpillbackBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double spillbackWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.4 +
                lakeConfig.OutflowStabilityWeight * 0.35 +
                waterConfig.HydrologyFlowMemoryWeight * 0.25,
                0.0,
                1.0);
            if (spillbackWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int edgeRadius = Math.Max(2, waterConfig.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.08)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, waterConfig.HydrologyWatershedStitchRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double mouthBlend = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, waterConfig.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    double bandBlend = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double pulseNoise = ComputeEdgeNoise(chunkX, chunkZ, x, z);

                    double spillback = (hydro + seamHydro + flowNode + seamFlow) * 0.25;
                    spillback += river * 0.18 + mouthBlend * waterConfig.RiverDeltaWetlandStrength * 0.2;
                    spillback *= 0.85 + pulseNoise * 0.3;
                    spillback *= 1.0 - Math.Clamp(divergence * 0.35 + slope * waterConfig.LakeRimErosionWeight * 0.02, 0.0, 0.65);
                    spillback *= 1.0 - Math.Clamp(relief * waterConfig.RiverReliefPenaltyWeight * 0.01, 0.0, 0.45);

                    double blend = spillbackWeight * bandBlend * (0.45 + lakeConfig.FlowSeepageWeight * 0.35);
                    double floor = Math.Max(lake * (0.82 + lakeConfig.OutflowStabilityWeight * 0.1), spillback * 0.15);
                    double target = lake * (1.0 - blend) + spillback * blend;
                    lakes[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.0);
                }
            }
        }

        private void ApplyTerraceBackfillBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double backfillWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                lakeConfig.LakeOutflowTaper * 0.30,
                0.0,
                1.0);
            if (backfillWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int terraceBand = Math.Max(2, Math.Max(lakeConfig.ShelfDepth, waterConfig.HydrologyEdgeBlendRadius));
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.08)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, terraceBand * 3.0),
                        0.0,
                        1.0);
                    if (seaBand <= 0.01)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double terraceSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    double terraceContinuity = 1.0 - Math.Clamp(
                        divergence * 0.42 + slope * waterConfig.LakeRimErosionWeight * 0.02,
                        0.0,
                        0.75);
                    double terraceBackfill = terraceSeed * backfillWeight * (0.45 + seaBand * 0.35) * terraceContinuity;
                    double floor = Math.Max(lake * (0.84 + lakeConfig.OutflowStabilityWeight * 0.08), terraceSeed * 0.14);
                    double target = lake * (1.0 - backfillWeight * 0.15) + terraceBackfill * 0.45;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyDeltaBackswampRetentionBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                waterConfig.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, waterConfig.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.1)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.8),
                        0.0,
                        1.0);
                    if (seaBand <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double backswampSeed = Math.Clamp(
                        hydro * 0.32 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.24 +
                        slope * waterConfig.LakeRimErosionWeight * 0.018,
                        0.0,
                        0.76);
                    double retention = backswampSeed * retentionWeight * (0.5 + seaBand * 0.4) * continuity;
                    double floor = Math.Max(lake * (0.83 + lakeConfig.OutflowStabilityWeight * 0.1), backswampSeed * 0.16);
                    double target = lake * (1.0 - retentionWeight * 0.2) + retention * 0.52;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyLagoonOverflowBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double overflowWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.36 +
                lakeConfig.SpillwayContinuityWeight * 0.34 +
                waterConfig.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (overflowWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, waterConfig.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.1)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.9),
                        0.0,
                        1.0);
                    if (seaBand <= 0.02)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, waterConfig.HydrologyEdgeBlendRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double pulseNoise = ComputeEdgeNoise(chunkX, chunkZ, x, z);

                    double lagoonSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.22 +
                        slope * waterConfig.LakeRimErosionWeight * 0.02,
                        0.0,
                        0.75);
                    double overflow = lagoonSeed * overflowWeight * (0.5 + seaBand * 0.36) * continuity;
                    overflow *= 0.9 + pulseNoise * 0.22;
                    overflow *= 1.0 - Math.Clamp(relief * waterConfig.RiverReliefPenaltyWeight * 0.012, 0.0, 0.35);

                    double floor = Math.Max(lake * (0.83 + lakeConfig.OutflowStabilityWeight * 0.1), lagoonSeed * 0.15);
                    double target = lake * (1.0 - overflowWeight * 0.2) + overflow * 0.52;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private int CreateNoiseSeed(int chunkX, int chunkZ, int localX, int localZ, int salt)
        {
            uint mixed = MixSeed((uint)worldSeedHash, (uint)chunkX, (uint)chunkZ, (uint)localX, (uint)localZ, (uint)salt);
            return (int)(mixed & 0x7FFFFFFF);
        }

        private static double ComputeEdgeNoise(int chunkX, int chunkZ, int x, int z)
        {
            uint value = MixSeed((uint)chunkX, (uint)chunkZ, (uint)x, (uint)z, 0x9E3779B9u);
            return (value & 0xFFFFu) / 65535.0;
        }

        private static uint MixSeed(params uint[] values)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < values.Length; index++)
                {
                    hash ^= values[index] + 0x9E3779B9u + (hash << 6) + (hash >> 2);
                    hash *= 16777619u;
                }

                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private void ApplyBackwaterRetentionBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.45 +
                lakeConfig.SpillwayContinuityWeight * 0.35 +
                lakeConfig.FlowSeepageWeight * 0.20,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, waterConfig.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.12)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, waterConfig.HydrologyEdgeBlendRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowSample - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(hydro - seamHydro);
                    double heightToSea = Math.Abs(heightMap[x, z] - seaLevel);
                    double mouthBlend = 1.0 - Math.Clamp(heightToSea / Math.Max(1.0, mouthRadius * 2.0), 0.0, 1.0);
                    double backwater = Math.Clamp(
                        (hydro + seamHydro + flowSample + seamFlow) * 0.25 +
                        river * 0.2 +
                        mouthBlend * waterConfig.RiverDeltaWetlandStrength * 0.2,
                        0.0,
                        1.25);

                    double erosionPenalty = Math.Clamp(
                        slope * waterConfig.LakeRimErosionWeight * 0.02 +
                        relief * waterConfig.RiverReliefPenaltyWeight * 0.015 +
                        divergence * 0.35 +
                        hydroGradient * 0.25,
                        0.0,
                        0.85);

                    double retention = backwater * retentionWeight * (0.55 + mouthBlend * 0.3);
                    double target = lake * (1.0 - erosionPenalty) + retention * erosionPenalty;
                    target = Math.Max(target, backwater * 0.18);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(target, 0.0, 1.0));
                }
            }
        }

        private void ApplySpillwayErosionDamping(
            float[,] lakes,
            float[,] hydrology,
            int[,] heightMap,
            float[,] flow,
            float[,]? riverMask)
        {
            double stabilityWeight = Math.Clamp(lakeConfig.OutflowStabilityWeight, 0.0, 1.0);
            double continuityWeight = Math.Clamp(lakeConfig.SpillwayContinuityWeight, 0.0, 1.0);
            double erosionWeight = Math.Clamp(waterConfig.LakeRimErosionWeight, 0.0, 1.0);
            if (stabilityWeight <= 0.0 && continuityWeight <= 0.0 && erosionWeight <= 0.0)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();
            double varianceClamp = Math.Max(0.001, waterConfig.HydrologyEdgeVarianceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.14)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double flowGradient = Math.Abs(flowSample - seamFlow);
                    double hydroGradient = Math.Abs(hydro - seamHydro);
                    double spillwayMemory = Math.Clamp(
                        lake * 0.45 + flowSample * 0.2 + seamFlow * 0.15 + river * 0.1 + seamHydro * 0.1,
                        0.0,
                        1.2);

                    double damping = flowGradient * stabilityWeight * 0.2;
                    damping += hydroGradient * continuityWeight * 0.15;
                    damping += slope * erosionWeight * 0.015;
                    damping = Math.Clamp(damping, 0.0, 0.45);
                    double floor = Math.Max(lake * (1.0 - damping * 0.45), spillwayMemory * 0.12);
                    double target = lake * (1.0 - damping) + floor * damping;
                    double clampRange = Math.Max(0.03, varianceClamp * 0.85);
                    target = Math.Clamp(target, lake - clampRange, lake + clampRange);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyFloodplainTerraceBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double terraceWeight = Math.Clamp(
                lakeConfig.ShorelineBlend * 0.35 +
                lakeConfig.OutflowStabilityWeight * 0.35 +
                waterConfig.RiverDeltaWetlandStrength * 0.3,
                0.0,
                1.0);
            if (terraceWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int terraceBand = Math.Max(2, waterConfig.RiverMouthSmoothRadius + lakeConfig.ShelfDepth);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.08)
                    {
                        continue;
                    }

                    double elevation = Math.Abs(heightMap[x, z] - seaLevel);
                    if (elevation > terraceBand * 3.0)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double riverAssist = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double bandBlend = 1.0 - Math.Clamp(elevation / Math.Max(1.0, terraceBand * 3.0), 0.0, 1.0);
                    double terraceSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.2 + flowNode * 0.2 + seamFlow * 0.15 + riverAssist * 0.15,
                        0.0,
                        1.2);

                    double terrace = terraceSeed * terraceWeight * (0.35 + bandBlend * 0.4);
                    terrace *= 1.0 - Math.Clamp(divergence * 0.35 + slope * waterConfig.LakeRimErosionWeight * 0.02, 0.0, 0.65);
                    double floor = Math.Max(lake * (0.84 + waterConfig.HydrologyContinuityWeight * 0.08), terraceSeed * 0.14);
                    double target = lake * (1.0 - terraceWeight * 0.12) + terrace * 0.35;
                    target = Math.Max(target, floor);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(target, 0.0, 1.0));
                }
            }
        }

        private void ApplyBasinRetentionLock(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.4 + lakeConfig.SpillwayContinuityWeight * 0.35 + lakeConfig.OutflowSealWeight * 0.25,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, waterConfig.RiverMouthSmoothRadius);
            var copy = (float[,])lakes.Clone();
            double inflowWeight = Math.Clamp(waterConfig.LakeInflowBlendWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake < 0.2)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double riverAssist = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double saturation = Math.Clamp(
                        hydro * 0.35 + seamHydro * 0.2 + flowNode * 0.2 + seamFlow * 0.15 + riverAssist * 0.1,
                        0.0,
                        1.2);
                    saturation *= 1.0 - Math.Clamp(divergence * 0.45 + Math.Abs(hydro - seamHydro) * 0.2, 0.0, 0.75);

                    double seaProximity = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 3.0),
                        0.0,
                        1.0);
                    double retentionBias = 0.1 + retentionWeight * 0.24 + inflowWeight * 0.12;
                    retentionBias *= 0.75 + seaProximity * 0.25;
                    double retentionFloor = Math.Max(lake, saturation * (0.14 + lakeConfig.ShorelineBlend * 0.14));
                    double target = Math.Max(retentionFloor, lake + saturation * retentionBias);
                    lakes[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyLakeMouthStability(
            float[,] lakes,
            float[,]? riverMask,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            if (riverMask == null)
            {
                return;
            }

            double continuityWeight = Math.Clamp(lakeConfig.SpillwayContinuityWeight, 0.0, 1.0);
            double stabilityWeight = Math.Clamp(lakeConfig.OutflowStabilityWeight, 0.0, 1.0);
            if (continuityWeight <= 0.0 && stabilityWeight <= 0.0)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, waterConfig.RiverMouthSmoothRadius);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.18)
                    {
                        continue;
                    }

                    int elevation = heightMap[x, z];
                    double seaProximity = 1.0 - Math.Clamp(
                        Math.Abs(elevation - seaLevel) / Math.Max(1.0, mouthRadius * 2.0),
                        0.0,
                        1.0);
                    if (seaProximity <= 0.02)
                    {
                        continue;
                    }

                    double river = TerrainMaskUtility.Clamp01(riverMask[x, z]);
                    double seamRiver = TerrainMaskUtility.SampleInterior(riverMask, x, z);
                    double flowSample = TerrainMaskUtility.Clamp01(flow[x, z] * 0.2f);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double flowGradient = Math.Abs(flowSample - seamFlow);
                    double riverGradient = Math.Abs(river - seamRiver);

                    double mouthAssist = Math.Clamp(
                        river * 0.4 + seamRiver * 0.25 + seamFlow * 0.2 + flowSample * 0.15,
                        0.0,
                        1.0);
                    double boost = seaProximity * mouthAssist * (continuityWeight * 0.25 + stabilityWeight * 0.2);
                    boost *= 1.0 - Math.Clamp(flowGradient * 0.6 + riverGradient * 0.5, 0.0, 0.9);

                    double target = Math.Max(lake, lake + boost);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyCatchmentSpillwayStitch(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            double spillwayContinuityWeight)
        {
            spillwayContinuityWeight = Math.Clamp(spillwayContinuityWeight, 0.0, 1.0);
            if (spillwayContinuityWeight <= 0.0)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();
            double taperWeight = Math.Clamp(lakeConfig.LakeOutflowTaper, 0.0, 1.0);
            double varianceWeight = Math.Clamp(lakeConfig.VarianceWeight, 0.0, 1.0);
            double seamFill = Math.Clamp(waterConfig.RiverSeamFillStrength, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lake = copy[x, z];
                    if (lake <= 0.2f)
                    {
                        continue;
                    }

                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double riverAssist = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double divergence = Math.Min(1.0, Math.Abs(flowSample - seamFlow) / divergenceClamp);

                    double catchmentPressure = Math.Clamp(
                        lake * 0.35 + flowSample * 0.3 + seamFlow * 0.15 + seamHydro * 0.12 + riverAssist * 0.08,
                        0.0,
                        1.3);

                    double stitch = catchmentPressure * spillwayContinuityWeight * (0.14 + taperWeight * 0.2 + varianceWeight * 0.1);
                    stitch *= 1.0 - Math.Clamp(divergence * 0.45 + Math.Abs(hydro - seamHydro) * 0.25, 0.0, 0.75);

                    double minFloor = Math.Max(lake, catchmentPressure * seamFill * 0.22);
                    double target = Math.Max(minFloor, lake + stitch);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(target, 0.0, 1.0));
                }
            }
        }

        private void ApplyRiparianEdgeFeather(float[,] mask, float[,] hydrology, float[,] flow)
        {
            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(1, waterConfig.HydrologyEdgeBlendRadius + lakeConfig.WetlandBufferRadius);
            double feather = Math.Clamp(waterConfig.HydrologySeamRelaxBlend * 0.4 + lakeConfig.ShorelineBlend * 0.35, 0.0, 1.0);
            double clampRange = Math.Max(0.001, waterConfig.HydrologyEdgeVarianceClamp);
            double guard = Math.Clamp(waterConfig.HydrologyEdgeStabilityWeight + waterConfig.LakeRimErosionWeight * 0.5, 0.0, 1.5);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(edgeRadius + 1);
                    double interior = TerrainMaskUtility.SampleInterior(copy, x, z);
                    double hydroGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrology, x, z) - hydrology[x, z]);
                    double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flow, x, z) - flow[x, z]);
                    double blend = feather * falloff;
                    double guardBlend = Math.Clamp((hydroGradient + flowGradient) * guard * 0.25, 0.0, 0.55);

                    double target = copy[x, z] * (1.0 - blend) + interior * blend;
                    target = Math.Clamp(target * (1.0 - guardBlend), copy[x, z] - clampRange, copy[x, z] + clampRange);
                    mask[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private static double ComputeCurvature(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int forward = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            int back = heightMap[x, Math.Max(0, z - 1)];
            double laplacian = (left + right + forward + back - 4 * center) / 4.0;
            return laplacian;
        }

        private static void ApplyWetlandBuffer(float[,] field, int radius, double shorelineBlend)
        {
            radius = Math.Max(0, radius);
            shorelineBlend = Math.Clamp(shorelineBlend, 0.0, 1.0);
            if (radius == 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    if (centre <= 0f)
                    {
                        continue;
                    }

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                            {
                                continue;
                            }

                            float distanceFalloff = 1f - (Math.Abs(dx) + Math.Abs(dz)) / (float)(radius + 1);
                            float influence = TerrainMaskUtility.Clamp01(centre * shorelineBlend * distanceFalloff);
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void ApplyLakeShelves(float[,] field, int[,] heightMap, int seaLevel, int shelfDepth, int maxDepth)
        {
            shelfDepth = Math.Max(0, shelfDepth);
            if (shelfDepth == 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float value = field[x, z];
                    if (value <= 0f)
                    {
                        continue;
                    }

                    int depthBelowSea = seaLevel - heightMap[x, z];
                    if (depthBelowSea < 0 || depthBelowSea > maxDepth)
                    {
                        continue;
                    }

                    float shelfBlend = 1f - Math.Clamp(Math.Abs(depthBelowSea) / (float)Math.Max(1, shelfDepth), 0f, 1f);
                    field[x, z] = Math.Max(value, value * (0.85f + shelfBlend * 0.15f));
                }
            }
        }

        private void ApplyOutflowTaper(
            float[,] lakes,
            float[,] flow,
            int edgeRadius,
            double taperWeight,
            double outflowStabilityWeight)
        {
            taperWeight = Math.Clamp(taperWeight, 0.0, 1.0);
            outflowStabilityWeight = Math.Clamp(outflowStabilityWeight, 0.0, 1.0);
            edgeRadius = Math.Max(1, edgeRadius);
            if (taperWeight <= 0.0)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(edgeRadius + 1);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0;
                    double flowSample = flow[x, z] / 6.0;
                    double flowGradient = Math.Abs(seamFlow - flowSample);
                    double continuity = 1.0 - Math.Clamp(flowGradient * outflowStabilityWeight * 0.35, 0.0, 0.45);
                    double blend = taperWeight * falloff * (0.55 + flowGradient * 0.35);
                    double clampRange = Math.Max(taperWeight * falloff * 0.35, 0.05);
                    double tapered = copy[x, z] * continuity;
                    tapered = tapered * (1.0 - outflowStabilityWeight * 0.25) + (copy[x, z] * 0.65 + (float)seamFlow * 0.15) * outflowStabilityWeight * 0.25;
                    double target = copy[x, z] * (1.0 - blend) + tapered * blend;
                    target = Math.Clamp(target, copy[x, z] - clampRange, copy[x, z] + clampRange);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private static void ApplyOutflowChannels(float[,] lakes, int[,] heightMap, float[,] flow, double inflowBlendWeight, int outflowDepth, double outflowStabilityWeight)
        {
            inflowBlendWeight = Math.Clamp(inflowBlendWeight, 0.0, 1.0);
            outflowDepth = Math.Max(1, outflowDepth);
            outflowStabilityWeight = Math.Clamp(outflowStabilityWeight, 0.0, 1.0);
            if (inflowBlendWeight <= 0.0 && outflowDepth <= 0)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var buffer = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lakeStrength = lakes[x, z];
                    if (lakeStrength <= 0.25f)
                    {
                        continue;
                    }

                    double stabilityBlend = 1.0 - outflowStabilityWeight * 0.5;
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    if (downhill == (0, 0))
                    {
                        continue;
                    }

                    int currentX = x;
                    int currentZ = z;
                    float channelStrength = lakeStrength;
                    float lastFlow = flow[x, z];
                    int originHeight = heightMap[x, z];

                    for (int step = 0; step < outflowDepth; step++)
                    {
                        currentX = Math.Clamp(currentX + downhill.X, 0, sizeX - 1);
                        currentZ = Math.Clamp(currentZ + downhill.Z, 0, sizeZ - 1);

                        float flowInfluence = TerrainMaskUtility.Clamp01(flow[currentX, currentZ] * (float)inflowBlendWeight);
                        float blended = Math.Max(channelStrength * 0.65f, lakeStrength * 0.35f);
                        float stability = (float)stabilityBlend;
                        float flowGradient = Math.Abs(flow[currentX, currentZ] - lastFlow);
                        float gradientPenalty = Math.Clamp(flowGradient * (float)outflowStabilityWeight * 0.5f, 0f, 0.35f);
                        int elevationDelta = Math.Abs(heightMap[currentX, currentZ] - originHeight);
                        float slopePenalty = Math.Clamp(elevationDelta / Math.Max(1f, outflowDepth * 2f), 0f, 1f);
                        float outflowValue = blended * stability + flowInfluence * (1f - stability);
                        outflowValue *= 1f - gradientPenalty;
                        outflowValue *= 1f - slopePenalty * (float)outflowStabilityWeight * 0.35f;
                        buffer[currentX, currentZ] = Math.Max(buffer[currentX, currentZ], outflowValue);
                        lastFlow = flow[currentX, currentZ];

                        if (downhill == (0, 0))
                        {
                            break;
                        }
                    }
                }
            }

            Array.Copy(buffer, lakes, buffer.Length);
        }

        private void ApplySpillwayRetentionAnchorBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double anchorWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.38 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                lakeConfig.LakeOutflowTaper * 0.28,
                0.0,
                1.0);
            if (anchorWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.05)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(2, waterConfig.HydrologyEdgeBlendRadius));
                    double floodplainBias = Math.Clamp((seaLevel + 8 - heightMap[x, z]) / 14.0, 0.0, 1.0);
                    double divergence = Math.Abs(flowNode - seamFlow);
                    double continuity = Math.Clamp((lake + hydro + seamHydro + flowNode + seamFlow + river) / 6.0, 0.0, 1.25);
                    double retentionAnchor = continuity * (0.18 + anchorWeight * 0.3 + floodplainBias * 0.24);
                    retentionAnchor *= 1.0 - Math.Clamp(slope * 0.026 + relief / 44.0 + divergence * 0.32, 0.0, 0.84);
                    if (retentionAnchor <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.84 + lakeConfig.OutflowStabilityWeight * 0.08), continuity * 0.15);
                    double target = lake * (1.0 - anchorWeight * 0.14) + (lake + retentionAnchor) * anchorWeight * 0.14;
                    target = Math.Max(target, floor);
                    if (river > 0.48)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.48) * 0.16, 0.0, 0.16);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplySpillwayBackflowDampingBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double dampingWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.40 +
                waterConfig.HydrologyFlowPersistence * 0.32 +
                lakeConfig.FlowSeepageWeight * 0.28,
                0.0,
                1.0);
            if (dampingWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    if (lake <= 0.03 && river <= 0.06)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double lateralFlow = Math.Abs(flow[x + 1, z] - flow[x - 1, z]) + Math.Abs(flow[x, z + 1] - flow[x, z - 1]);
                    double backflow = Math.Clamp(lateralFlow / 14.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double depthBias = Math.Clamp((seaLevel - heightMap[x, z]) / Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange), -0.5, 1.0);
                    double dampingSignal = Math.Clamp(
                        flowNode * 0.30 +
                        seamFlow * 0.22 +
                        hydro * 0.18 +
                        seamHydro * 0.14 +
                        river * 0.16,
                        0.0,
                        1.2);
                    double damping = Math.Clamp(dampingSignal * (1.0 - Math.Clamp(slope * 0.02, 0.0, 0.7)) + backflow * 0.24 + depthBias * 0.18, 0.0, 1.3);
                    double floor = Math.Max(lake * 0.86, damping * 0.36);
                    double target = lake * (1.0 - dampingWeight * 0.16) +
                        (lake + damping * 0.7) * dampingWeight * 0.16;
                    target = Math.Max(target, floor);
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyWetlandLeakageClampBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double clampWeight = Math.Clamp(
                lakeConfig.WetlandSaturationThreshold * 0.36 +
                lakeConfig.FlowSeepageWeight * 0.34 +
                lakeConfig.OutflowSealWeight * 0.3,
                0.0,
                1.0);
            if (clampWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.2);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.2);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double floodplainBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 16.0, 0.0, 1.0);
                    double leakage = Math.Max(0.0, flowNode - seamFlow) + Math.Max(0.0, slope * 0.015 - floodplainBias * 0.06);
                    double wetlandPocket = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + seamFlow * 0.2 + floodplainBias * 0.14 + river * 0.08,
                        0.0,
                        1.15);
                    double clampFactor = Math.Clamp(wetlandPocket * clampWeight - leakage * 0.35, 0.0, 0.95);
                    if (clampFactor <= 0.01)
                    {
                        continue;
                    }

                    double target = lake * (1.0 - clampFactor * 0.16) +
                        (lake + wetlandPocket * 0.2) * clampFactor * 0.16;
                    if (river > 0.5)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.5) * 0.18, 0.0, 0.18);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyKarstOutletStabilityBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.36 +
                lakeConfig.SpillwayContinuityWeight * 0.34 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 9 - heightMap[x, z]) / 16.0, 0.0, 1.0);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double outletHydro = hydrology[downX, downZ];
                    double outletFlow = Math.Clamp(flow[downX, downZ] / 6.0, 0.0, 1.3);
                    double outletSignal = Math.Clamp(
                        outletHydro * 0.36 +
                        outletFlow * 0.34 +
                        Math.Max(0.0, seamFlow - flowNode) * 0.2 +
                        floodplainBias * 0.1,
                        0.0,
                        1.25);
                    double stability = 1.0 - Math.Clamp(slope * 0.026 + relief / 44.0, 0.0, 0.84);
                    if (river > 0.48)
                    {
                        stability *= 1.0 - Math.Clamp((river - 0.48) * 0.2, 0.0, 0.2);
                    }

                    double anchor = Math.Clamp((lake + hydro + seamHydro + seamFlow) * 0.25, 0.0, 1.2);
                    double bridge = anchor * (0.2 + outletSignal * 0.24 + bridgeWeight * 0.22) * stability;
                    if (bridge <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.OutflowSealWeight * 0.08), anchor * 0.16);
                    double target = lake * (1.0 - bridgeWeight * 0.14) + (lake + bridge) * bridgeWeight * 0.14;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyFloodplainRetentionShelfBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                lakeConfig.FlowSeepageWeight * 0.38 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                waterConfig.RiverDeltaWetlandStrength * 0.28,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.05)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.2);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.2);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 9 - heightMap[x, z]) / 15.0, 0.0, 1.0);
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow + lake) / 5.0, 0.0, 1.2);
                    double retention = continuity * (0.2 + floodplainBias * 0.26 + retentionWeight * 0.24);
                    retention *= 1.0 - Math.Clamp(slope * 0.025 + relief / 46.0, 0.0, 0.82);
                    if (river > 0.45)
                    {
                        retention *= 1.0 - Math.Clamp((river - 0.45) * 0.2, 0.0, 0.2);
                    }

                    if (retention <= 0.01)
                    {
                        continue;
                    }

                    double shelfFloor = Math.Max(lake * (0.84 + lakeConfig.ShorelineBlend * 0.08), continuity * 0.15);
                    double target = lake * (1.0 - retentionWeight * 0.12) + (lake + retention) * retentionWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, shelfFloor));
                }
            }
        }

        private void ApplyAlluvialBackwaterLinkBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.36 +
                lakeConfig.TerraceBiasWeight * 0.34 +
                waterConfig.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, waterConfig.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 16.0, 0.0, 1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double terraceCoupling = Math.Clamp((Math.Max(0.0, -ComputeCurvature(heightMap, x, z)) + floodplainBias) * 0.5, 0.0, 1.0);
                    double backwaterSignal = Math.Clamp(
                        hydro * 0.22 +
                        seamHydro * 0.18 +
                        flowNode * 0.2 +
                        seamFlow * 0.18 +
                        river * 0.14 +
                        terraceCoupling * 0.08,
                        0.0,
                        1.25);
                    backwaterSignal *= 1.0 - Math.Clamp(slope * 0.026 + relief / 44.0 + divergence * 0.22, 0.0, 0.84);
                    if (backwaterSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.ShorelineBlend * 0.08), backwaterSignal * 0.18);
                    double target = lake * (1.0 - bridgeWeight * 0.12) + (lake + backwaterSignal) * bridgeWeight * 0.12;
                    if (river > 0.52)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.52) * 0.16, 0.0, 0.14);
                    }

                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyKarstFloodplainRetentionRelayBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.38 +
                waterConfig.HydrologyFlowPersistence * 0.34 +
                lakeConfig.FlowSeepageWeight * 0.28,
                0.0,
                1.2);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 6.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double karstNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0024 + 71.0,
                        (chunkZ * sizeZ + z) * 0.0024 - 39.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1187)));
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.2);
                    double relay = continuity * (0.24 + floodplainBand * 0.24 + karstNoise * 0.2 + river * 0.12);
                    relay *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.3 + divergence * 0.22,
                        0.0,
                        0.84);
                    if (relay <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.OutflowStabilityWeight * 0.09), continuity * 0.16);
                    double target = lake * (1.0 - relayWeight * 0.12) + (lake + relay) * relayWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyAlluvialGroundwaterExchangeBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double exchangeWeight = Math.Clamp(
                lakeConfig.OutflowStabilityWeight * 0.36 +
                lakeConfig.FlowSeepageWeight * 0.34 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.25);
            if (exchangeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 7.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double exchangeNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0023 + 83.0,
                        (chunkZ * sizeZ + z) * 0.0023 - 47.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1381)));
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.2);
                    double exchange = continuity * (0.24 + floodplainBand * 0.24 + river * 0.16 + exchangeNoise * 0.14);
                    exchange *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.3 + divergence * 0.24,
                        0.0,
                        0.85);
                    if (exchange <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.OutflowSealWeight * 0.09), continuity * 0.16);
                    double target = lake * (1.0 - exchangeWeight * 0.12) + (lake + exchange) * exchangeWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyPerchedFloodplainCascadeBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillRetentionWeight * 0.36 +
                lakeConfig.OutflowStabilityWeight * 0.34 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.2);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.58);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 7.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double cascadeNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 97.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 51.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1457)));
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.2);
                    double cascade = continuity * (0.24 + floodplainBand * 0.24 + river * 0.14 + cascadeNoise * 0.14);
                    cascade *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.31 + divergence * 0.24,
                        0.0,
                        0.85);
                    if (cascade <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + lakeConfig.OutflowSealWeight * 0.08), continuity * 0.16);
                    double target = lake * (1.0 - bridgeWeight * 0.12) + (lake + cascade) * bridgeWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyFloodplainSpillwayBalancingBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.36 +
                lakeConfig.SpillRetentionWeight * 0.34 +
                waterConfig.HydrologyFlowPersistence * 0.30,
                0.0,
                1.2);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 2);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.58);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 8.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double balanceNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 107.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 63.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1583)));

                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.2);
                    double balanceSignal = Math.Clamp(
                        lake * 0.34 +
                        continuity * 0.24 +
                        river * 0.14 +
                        floodplainBand * 0.16 +
                        balanceNoise * 0.12,
                        0.0,
                        1.3);
                    balanceSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.31 + divergence * 0.24,
                        0.0,
                        0.86);
                    if (balanceSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.OutflowStabilityWeight * 0.08), continuity * 0.17);
                    double target = lake * (1.0 - bridgeWeight * 0.12) + (lake + balanceSignal) * bridgeWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplySubsurfaceOverflowBalancingBridge(
            float[,] lakes,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                lakeConfig.SpillwayContinuityWeight * 0.35 +
                lakeConfig.FlowSeepageWeight * 0.33 +
                waterConfig.HydrologyFlowPersistence * 0.32,
                0.0,
                1.25);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int reliefRadius = Math.Max(2, waterConfig.HydrologyWatershedStitchRadius + 3);
            double divergenceScale = Math.Max(0.12, waterConfig.HydrologyFlowDivergenceClamp * 0.6);
            var copy = (float[,])lakes.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(Math.Max(0.0, flow[x, z]) / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) /
                        Math.Max(1.0, waterConfig.HydrologyWaterTableClampRange + 8.0),
                        0.0,
                        1.0);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, lakeConfig.MaxRadius + 8.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceScale);
                    double convergence = Math.Max(0.0, seamFlow - flowNode) + Math.Max(0.0, seamHydro - hydro);
                    double bridgeNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0022 + 127.0,
                        (chunkZ * sizeZ + z) * 0.0022 - 79.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 1763)));

                    double overflowSignal = Math.Clamp(
                        lake * 0.34 +
                        hydro * 0.18 +
                        seamHydro * 0.15 +
                        flowNode * 0.15 +
                        seamFlow * 0.1 +
                        river * 0.04 +
                        floodplainBand * 0.04,
                        0.0,
                        1.35);
                    overflowSignal *= 1.0 + convergence * 0.22 + Math.Clamp((bridgeNoise - 0.5) * 0.2, -0.15, 0.15);
                    overflowSignal *= 1.0 - Math.Clamp(
                        slope * waterConfig.HydrologySlopePenalty * 0.014 + relief * 0.31 + divergence * 0.24,
                        0.0,
                        0.86);
                    if (overflowSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.86 + lakeConfig.OutflowStabilityWeight * 0.08), overflowSignal * 0.17);
                    double target = lake * (1.0 - bridgeWeight * 0.12) + (lake + overflowSignal) * bridgeWeight * 0.12;
                    lakes[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplySpillwayContinuity(float[,] lakes, int[,] heightMap, float[,] flow, float[,]? riverMask, double spillwayContinuityWeight)
        {
            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var copy = (float[,])lakes.Clone();
            double inflowBlend = Math.Clamp(waterConfig.LakeInflowBlendWeight, 0.0, 1.0);
            double outflowSeal = Math.Clamp(lakeConfig.OutflowSealWeight, 0.0, 1.0);
            double stability = Math.Clamp(lakeConfig.OutflowStabilityWeight, 0.0, 1.0);
            spillwayContinuityWeight = Math.Clamp(spillwayContinuityWeight, 0.0, 1.0);
            int spillDepth = Math.Max(1, lakeConfig.OutflowCarveDepth + 1);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lakeStrength = copy[x, z];
                    if (lakeStrength <= 0.3f)
                    {
                        continue;
                    }

                    int cx = x;
                    int cz = z;
                    float memory = lakeStrength;
                    float flowMemory = flow[x, z];
                    for (int step = 0; step < spillDepth; step++)
                    {
                        var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, cx, cz);
                        if (downhill == (0, 0))
                        {
                            break;
                        }

                        cx = Math.Clamp(cx + downhill.X, 0, sizeX - 1);
                        cz = Math.Clamp(cz + downhill.Z, 0, sizeZ - 1);
                        float riverAssist = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[cx, cz]) : 0f;
                        float channelFlow = flow[cx, cz];
                        float flowGradient = Math.Abs(channelFlow - flowMemory);
                        float channelBias = TerrainMaskUtility.Clamp01((float)(inflowBlend * 0.35 + outflowSeal * 0.25 + stability * 0.2));
                        float continuity = TerrainMaskUtility.Clamp01(1f - flowGradient * (float)(stability * 0.2 + spillwayContinuityWeight * 0.22));
                        float spill = TerrainMaskUtility.Clamp01(memory * (0.7f + channelBias * 0.3f + (float)spillwayContinuityWeight * 0.08f) * continuity + riverAssist * 0.2f);
                        lakes[cx, cz] = Math.Max(lakes[cx, cz], spill);
                        memory = spill;
                        flowMemory = channelFlow;
                    }
                }
            }
        }
    }
}
