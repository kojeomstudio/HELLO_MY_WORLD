using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Hydrology-driven river mask builder with seam feathering and flow-aware width modulation.
    /// </summary>
    public sealed class ImprovedRiverGenerator
    {
        private readonly WaterConfig config;
        private readonly int worldSeedHash;

        public ImprovedRiverGenerator(WaterConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            worldSeedHash = (int)(worldSeed ^ 0x7B3C9A01);
        }

        public float[,] BuildMask(
            int chunkX,
            int chunkZ,
            int chunkSize,
            int[,] heightMap,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] erosionRisk,
            int seaLevel)
        {
            var mask = new float[chunkSize, chunkSize];
            double noiseScale = Math.Max(0.0001, config.RiverNoiseScale);
            double reliefPenalty = Math.Clamp(config.RiverReliefPenaltyWeight, 0.0, 1.0);
            double confluenceBoost = Math.Clamp(config.RiverConfluenceBoost, 0.0, 2.0);
            double flowShadowWeight = Math.Clamp(config.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double thalwegStabilityWeight = Math.Clamp(config.HydrologyThalwegStabilityWeight, 0.0, 1.5);
            double watershedBlend = Math.Clamp(config.HydrologyWatershedStitchWeight, 0.0, 1.0);
            int watershedRadius = Math.Max(1, config.HydrologyWatershedStitchRadius);
            double flowMemoryWeight = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double catchmentWeight = Math.Clamp(config.HydrologyCatchmentWeight, 0.0, 1.0);
            double braidingWeight = Math.Clamp(config.RiverBraidingWeight, 0.0, 1.0);
            double edgeNormalizationStrength = Math.Clamp(config.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double waterTableClampWeight = Math.Clamp(config.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double waterTableClampRange = Math.Max(1.0, config.HydrologyWaterTableClampRange);
            double waterTableSlopeWeight = Math.Clamp(config.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            double depthBias = Math.Clamp(config.RiverDepth / 12.0, 0.0, 1.0);
            double riverBankErosionWeight = Math.Clamp(config.RiverBankErosionWeight, 0.0, 1.0);
            double anisotropyDamping = Math.Clamp(config.RiverAnisotropyDamping, 0.0, 1.0);
            double bankStabilityClamp = Math.Clamp(config.RiverBankStabilityClamp, 0.0, 1.0);
            double warpFrequency = Math.Max(0.0001, config.HydrologyWarpFrequency);
            double warpAmplitude = Math.Max(0.0, config.HydrologyWarpAmplitude);
            double tangentWeight = Math.Clamp(config.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double reservoirBlend = Math.Clamp(config.HydrologyReservoirBlend, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            double continuityMemory = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double edgeGuardWeight = Math.Clamp(config.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double confluenceMemoryWeight = Math.Clamp(
                config.HydrologyFlowMemoryWeight * 0.55 +
                config.RiverConfluenceBoost * 0.45,
                0.0,
                1.0);
            double tributaryCaptureWeight = Math.Clamp(config.RiverTributaryCaptureWeight, 0.0, 1.0);
            double avulsionResistance = Math.Clamp(config.RiverAvulsionResistance, 0.0, 1.0);
            double floodPulseWeight = Math.Clamp(
                config.RiverDeltaWetlandStrength * 0.5 +
                config.HydrologyFlowPersistence * 0.5,
                0.0,
                1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int height = heightMap[x, z];
                    double worldX = chunkX * chunkSize + x;
                    double worldZ = chunkZ * chunkSize + z;
                    int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeNormalization = edgeNormalizationStrength * edgeFalloff;
                    double baseNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale,
                        worldZ * noiseScale,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 101)));
                    double macroNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 0.4 + 71.0,
                        worldZ * noiseScale * 0.4 - 53.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 131)));
                    double detailNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 1.85 - 17.0,
                        worldZ * noiseScale * 1.85 + 9.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 151)));

                    double meanderNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 0.65 + 19.0,
                        worldZ * noiseScale * 0.65 - 11.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 173)));
                    double warpNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * warpFrequency + 11.0,
                        worldZ * warpFrequency - 7.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 197)));
                    double meanderFactor = 1.0 + meanderNoise * (Math.Clamp(warpAmplitude * 0.02, 0.05, 0.22) + Math.Max(0.0, config.RiverMeanderJitter));
                    meanderFactor *= 1.0 + warpNoise * Math.Clamp(warpAmplitude * 0.01, 0.0, 0.15);
                    double layeredNoise = (baseNoise * 0.55) + (macroNoise * 0.25) + (detailNoise * 0.2);

                    double hydrology = hydrologyMask[x, z];
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double flowMemory = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double gradient = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / Math.Max(1.0, config.RiverDepth);
                    double relief = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    double hydrologyVariance = TerrainMaskUtility.SampleVariance(hydrologyMask, x, z);
                    double flowVariance = TerrainMaskUtility.SampleVariance(flowAccumulation, x, z);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    double flowShear = Math.Abs(flowMemory - flow);
                    double divergencePenalty = Math.Min(1.0, flowShear / Math.Max(0.0001, config.HydrologyFlowDivergenceClamp));
                    double braidedAssist = Math.Clamp((hydrologyVariance + flowVariance) * config.HydrologyFlowPersistence * 0.15, 0.0, 0.25);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrology);
                    double flowGradient = Math.Abs(interiorFlow - flow);
                    double pressureGradient = Math.Abs(hydrologyGradient - flowGradient);
                    double directionality = (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * 0.5;
                    double flowAlignment = 1.0 + Math.Clamp(flow * config.RiverFlowAlignmentWeight * 0.35, 0.0, 0.45);
                    double seamStitch = 1.0 + Math.Clamp((TerrainMaskUtility.SampleInterior(hydrologyMask, x, z) - hydrologyMask[x, z]) * config.HydrologyEdgeFluxBlend, -0.35, 0.35);
                    double flowShadow = Math.Clamp(
                        flow * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5 +
                        seamStitch * flowShadowWeight * 0.25,
                        0.0,
                        0.75);
                    double hydrologyShadow = Math.Clamp(flowShadow + hydrology * flowShadowWeight * 0.25, 0.0, 0.85);
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * config.HydrologyEdgeStabilityWeight * 0.25, 0.0, 0.35);
                    double continuityBias = 1.0 + Math.Clamp((seamHydro + interiorFlow) * config.HydrologyEdgeFluxBlend * 0.2, -0.2, 0.35);
                    continuityBias *= 1.0 - Math.Clamp(hydrologyVariance * 0.15 + flowVariance * 0.1, 0.0, 0.25);
                    double seamAnchor = hydrology * 0.25 + seamHydro * 0.25 + flow * 0.25 + flowMemory * 0.25;

                    double riverMask = config.RiverBankThreshold - layeredNoise - erosion * riverBankErosionWeight * 0.08;
                    double pressure = Math.Max(0.0, riverMask);
                    double erosionBrake = 1.0 - Math.Clamp(erosion * riverBankErosionWeight * 0.45, 0.0, 0.45);
                    pressure *= 1.0 + hydrology * config.HydrologyContinuityWeight;
                    pressure *= 1.0 + Math.Max(0.0, 1.0 - relief) * thalwegStabilityWeight * 0.07;
                    pressure *= 1.0 + flow * config.RiverFlowAlignmentWeight;
                    double anisotropyPenalty = 1.0 - Math.Clamp(gradient * anisotropyDamping * 0.05 + relief * anisotropyDamping * 0.1, 0.0, 0.45);
                    pressure *= (1.0 + directionality * config.RiverAnisotropyWeight * 0.2) * anisotropyPenalty;
                    pressure *= 1.0 - Math.Clamp(gradient * config.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * reliefPenalty, 0.0, 0.35);
                    double bankClamp = 1.0 - Math.Clamp((gradient + relief) * bankStabilityClamp * 0.08, 0.0, 0.55);
                    pressure *= bankClamp;
                    pressure *= flowAlignment * seamStitch * meanderFactor;
                    pressure *= 1.0 + (flowMemory + seamHydro) * flowMemoryWeight * 0.2;
                    pressure *= 1.0 + seamAnchor * edgeNormalization * 0.15;
                    pressure *= 1.0 - Math.Clamp(directionality * tangentWeight * 0.08, 0.0, 0.2);
                    double waterTableDistance = seaLevel - height;
                    double waterBias = 1.0 - Math.Clamp(Math.Abs(waterTableDistance) / waterTableClampRange, 0.0, 1.0);
                    double waterClamp = 1.0 + waterBias * waterTableClampWeight * (waterTableDistance >= 0 ? 0.45 : -0.25);
                    double waterSlopePenalty = Math.Clamp(gradient * waterTableSlopeWeight * 0.05, 0.0, 0.45);
                    double waterMemory = (hydrology + seamHydro + flowMemory) * waterTableClampWeight * 0.08;
                    pressure *= Math.Max(0.65, waterClamp);
                    pressure *= 1.0 - waterSlopePenalty;
                    pressure *= 1.0 + waterMemory;
                    pressure *= 1.0 + depthBias * 0.05;
                    pressure *= 1.0 - Math.Clamp(divergencePenalty * 0.35, 0.0, 0.35);
                    double braidingAssist = Math.Clamp((hydrologyVariance + flowVariance + divergencePenalty) * braidingWeight * 0.45, 0.0, 0.35);
                    pressure = pressure * (1.0 - (braidedAssist + braidingAssist) * 0.25) + (braidedAssist + braidingAssist) * 0.08;
                    pressure = pressure * (1.0 - hydrologyShadow * 0.25) + (hydrology + seamHydro) * hydrologyShadow * 0.15;
                    double flowMemoryContinuity = (flowMemory + seamHydro + hydrology) * 0.333;
                    double flowMemoryGradient = Math.Abs(flowMemory - flow);
                    double floodPulse = Math.Clamp(
                        flowMemory * 0.4 +
                        flow * 0.25 +
                        seamHydro * 0.2 +
                        Math.Max(0.0, flow - interiorFlow) * 0.15,
                        0.0,
                        1.2);
                    pressure *= seamGuard;
                    double reservoir = Math.Clamp((flowMemory + seamHydro + hydrology) * reservoirBlend * 0.5, 0.0, 0.45);
                    double pressureStabilizer = 1.0 - Math.Clamp(
                        (pressureGradient / Math.Max(0.0001, config.HydrologyPressureGradientClamp)) * Math.Clamp(config.HydrologyPressureBlend, 0.0, 1.0),
                        0.0,
                        0.45);
                    pressure *= Math.Max(0.55, pressureStabilizer);
                    pressure *= 1.0 + flowMemoryContinuity * 0.25;
                    pressure *= 1.0 + Math.Max(0.0, 1.0 - divergencePenalty) * thalwegStabilityWeight * 0.06;
                    pressure *= 1.0 - Math.Clamp(flowMemoryGradient * 0.2, 0.0, 0.35);
                    pressure *= 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * edgeGuardWeight * 0.2, 0.0, 0.4);
                    pressure *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + flowVariance * 0.15, 0.0, 0.35);
                    double curvature = ComputeCurvature(heightMap, x, z);
                    double basinAssist = Math.Clamp(curvature * config.HydrologyCurvatureWeight * 0.2, -0.35, 0.35);
                    double ridgePenalty = Math.Max(0.0, -basinAssist);
                    double catchmentAssist = Math.Clamp((seamHydro + interiorFlow + Math.Max(0.0, -curvature) * 0.15) * catchmentWeight * 0.35, 0.0, 0.45);
                    pressure *= 1.0 + Math.Max(0.0, basinAssist) * 0.4;
                    pressure *= 1.0 - Math.Clamp(ridgePenalty * 0.75, 0.0, 0.45);
                    pressure = pressure * (1.0 - catchmentWeight * 0.15) + catchmentAssist * catchmentWeight * 0.35;
                    pressure = pressure * (1.0 - floodPulseWeight * 0.14) + (pressure + floodPulse * 0.12) * floodPulseWeight * 0.14;
                    if (confluenceBoost > 0.0)
                    {
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                        double tributaryPressure = Math.Clamp((flow + neighbourFlow) * 0.5, 0.0, 1.0);
                        double hydrologyAssist = hydrology * 0.5 + hydrologyGradient * 0.15;
                        double confluenceMemory = Math.Clamp((flowMemory + seamHydro + neighbourFlow) / 3.0, 0.0, 1.1);
                        pressure *= 1.0 + (tributaryPressure + hydrologyAssist) * confluenceBoost * 0.35;
                        pressure *= 1.0 + confluenceMemory * confluenceMemoryWeight * 0.22;
                        pressure = pressure * (1.0 - confluenceMemoryWeight * 0.08) + (pressure + confluenceMemory * 0.06) * confluenceMemoryWeight * 0.08;
                    }

                    double floodplain = Math.Clamp((hydrology + seamHydro + flowMemory) * config.RiverDeltaWetlandStrength * 0.25, 0.0, 0.6);
                    double varianceAssist = Math.Clamp((hydrologyVariance + flowVariance) * config.HydrologyVarianceBlend * 0.15, -0.35, 0.45);
                    pressure = pressure * (1.0 - floodplain * 0.2) + floodplain * 0.1;
                    pressure *= 1.0 + varianceAssist;
                    pressure *= erosionBrake;
                    pressure *= 1.0 - Math.Clamp(erosion * reliefPenalty * 0.25, 0.0, 0.25);
                    double floodplainAnchor = Math.Clamp(
                        (hydrology + seamHydro + flow + flowMemory) * config.RiverDeltaWetlandStrength * 0.2,
                        0.0,
                        0.7);
                    double avulsionPotential = Math.Clamp(
                        (hydrologyVariance + flowVariance + erosion) * config.RiverConfluenceBoost * 0.2,
                        0.0,
                        0.65);
                    double tributaryCapture = Math.Clamp(
                        (flowMemoryContinuity + seamHydro + catchmentAssist) * 0.333,
                        0.0,
                        1.0);
                    double avulsionRisk = Math.Clamp(
                        (avulsionPotential + divergencePenalty + flowGradient) * 0.333,
                        0.0,
                        1.0);
                    double bankCohesion = 1.0 - Math.Clamp(
                        (gradient + erosion) * config.RiverBankStabilityClamp * 0.1,
                        0.0,
                        0.55);
                    pressure = pressure * (1.0 - avulsionPotential * 0.18) + floodplainAnchor * avulsionPotential * 0.12;
                    pressure *= 1.0 + tributaryCapture * tributaryCaptureWeight * 0.18;
                    pressure *= 1.0 - avulsionRisk * avulsionResistance * 0.22;
                    pressure += tributaryCapture * tributaryCaptureWeight * 0.03 * (1.0 - avulsionRisk);
                    pressure *= bankCohesion;

                    double flowBridge = (hydrology + seamHydro + flowMemory) * config.HydrologyEdgeFlowBias * 0.15;
                    double flowLockWeight = Math.Clamp(config.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
                    double directionalDrift = 1.0 + directionality * config.HydrologyDirectionalBlend * 0.15;
                    pressure = pressure * (1.0 - flowLockWeight * 0.15) + (pressure * directionalDrift + seamAnchor * flowLockWeight) * 0.15;
                    double divergenceBrake = Math.Min(1.0, Math.Abs(flowMemory - seamHydro) / divergenceClamp);
                    pressure *= 1.0 - Math.Clamp(divergenceBrake * continuityMemory, 0.0, 0.22);
                    pressure = pressure * (1.0 - reservoirBlend * 0.2) + (pressure + reservoir) * reservoirBlend * 0.2;
                    pressure *= 1.0 + flowBridge;

                    // Headwater stability slightly broadens shallow channels to avoid seams.
                    double headwater = 1.0 - Math.Clamp(flow * config.RiverHeadwaterStabilityWeight, 0.0, 0.65);
                    pressure *= 1.0 + headwater * 0.1;
                    pressure *= continuityBias;
                    double deltaBlend = 1.0 - Math.Clamp(Math.Abs(height - seaLevel) / Math.Max(1.0, config.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    pressure *= 1.0 + deltaBlend * config.RiverDeltaWetlandStrength * 0.5;
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                        double neighbourHydro = seamHydro;
                        double seam = hydrology * 0.3 + neighbourHydro * 0.3 + neighbourFlow * 0.25 + flowMemory * 0.15;
                        pressure = pressure * (1.0 - edgeRepair * 0.35) + seam * edgeRepair * 0.5;
                        pressure = Math.Max(pressure, seam * edgeRepair * 0.25);
                    }
                    pressure = pressure * (1.0 - edgeNormalization * 0.25) + seamAnchor * edgeNormalization * 0.35;
                    pressure *= 1.0 - Math.Clamp(edgeNormalization * tangentWeight * 0.25, 0.0, 0.25);
                    pressure = ApplyEdgeBlend(pressure, hydrologyMask[x, z], x, z, chunkSize);

                    mask[x, z] = (float)Math.Clamp(pressure, 0.0, 1.35);
                }
            }

            TerrainMaskUtility.ApplyHydrologyContinuity(
                mask,
                hydrologyMask,
                flowAccumulation,
                config.HydrologyEdgeBlendRadius,
                config.HydrologyContinuityWeight);
            TerrainMaskUtility.NormalizeEdgeBands(
                mask,
                config.HydrologyEdgeBlendRadius,
                Math.Max(0.05, config.HydrologySeamRelaxBlend * 0.35),
                config.HydrologyEdgeVarianceClamp);
            ApplyContinuityGuard(
                mask,
                hydrologyMask,
                flowAccumulation,
                config.HydrologyEdgeBlendRadius,
                config.RiverEdgeContinuityWeight,
                config.HydrologyEdgeVarianceClamp);
            ApplyHydrologyStability(
                mask,
                hydrologyMask,
                flowAccumulation,
                config.HydrologyGradientStabilityIterations,
                config.HydrologyGradientStabilityBlend,
                config.HydrologyGradientClamp);
            TerrainMaskUtility.ClampVariance(mask, config.HydrologyVarianceClamp);
            TerrainMaskUtility.Smooth2D(mask, config.RiverIntensitySmoothIterations, config.RiverIntensitySmoothBlend);
            TerrainMaskUtility.DirectionalSmooth(heightMap, mask, Math.Max(1, config.HydrologyDirectionalIterations), config.HydrologyDirectionalBlend * 0.35);
            TerrainMaskUtility.StitchEdges(mask, config.HydrologySeamRelaxBlend * 0.5);
            TerrainMaskUtility.NormalizeEdges(
                mask,
                config.HydrologyEdgeBlendRadius,
                config.HydrologyEdgeNormalizationIterations,
                config.HydrologyEdgeNormalizationBlend);
            ApplyRiparianEdgeFeather(mask, hydrologyMask, flowAccumulation);
            ApplyConfluenceMemory(mask, hydrologyMask, flowAccumulation);
            ApplyCatchmentBraidingBridge(mask, hydrologyMask, flowAccumulation);
            ApplyMouthContinuityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyTributaryConvergenceLock(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyAvulsionDampingBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyCrossChunkFloodplainBridge(mask, hydrologyMask, flowAccumulation, chunkX, chunkZ);
            ApplyAnabranchStabilityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyFloodPulseContinuityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel, chunkX, chunkZ);
            ApplyAnabranchCutoffDamping(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyDistributaryLeveeStabilityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyEstuaryConvergenceBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel, chunkX, chunkZ);
            ApplyHeadwaterSpringBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyFloodplainMeanderStabilityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyAlluvialChannelAnchorBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyFloodplainRetentionAnchorBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyThalwegContinuityBridge(mask, hydrologyMask, flowAccumulation, heightMap);
            ApplyConfluenceLagStorageBridge(mask, hydrologyMask, flowAccumulation, heightMap);
            ApplyConfluenceFloodplainRelayBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyOxbowCutoffContinuityBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplyAnabranchHotspotRelayBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplySeasonalRunoffPulseBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel, chunkX, chunkZ);
            ApplyGroundwaterExchangeBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            ApplySpringFloodplainRelayBridge(mask, hydrologyMask, flowAccumulation, heightMap, seaLevel);
            FeatherEdges(mask, config.RiverEdgeFeather, config.RiverSeamFillStrength);
            return mask;
        }

        private void ApplySpringFloodplainRelayBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                config.RiverTributaryCaptureWeight * 0.34 +
                config.RiverBraidingWeight * 0.33 +
                config.HydrologyFlowPersistence * 0.33,
                0.0,
                1.25);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 1);
            double divergenceScale = Math.Max(0.12, config.HydrologyFlowDivergenceClamp * 0.5);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.001)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Max(0.0, flow[x, z]);
                    double flowNormalized = Math.Clamp(flowNode / 6.0, 0.0, 1.0);
                    double seamFlowNormalized = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, config.HydrologyWaterTableClampRange + 4.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNormalized - seamFlowNormalized) / divergenceScale);
                    double variance = TerrainMaskUtility.SampleVariance(copy, x, z);
                    double floodplainBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, config.RiverMouthSmoothRadius * 1.5),
                        0.0,
                        1.0);

                    double relaySignal = Math.Clamp(
                        hydro * 0.27 +
                        seamHydro * 0.2 +
                        flowNormalized * 0.2 +
                        seamFlowNormalized * 0.15 +
                        floodplainBand * 0.1 +
                        variance * 0.08,
                        0.0,
                        1.25);

                    relaySignal *= 1.0 - Math.Clamp(
                        slope * config.HydrologySlopePenalty * 0.02 +
                        relief * 0.42 +
                        divergence * 0.22,
                        0.0,
                        0.82);

                    if (relaySignal <= 0.02)
                    {
                        continue;
                    }

                    double reinforce = relaySignal * relayWeight;
                    double relayTarget = river * (1.0 - reinforce * 0.08) +
                                         (river + seamHydro * 0.14 + floodplainBand * 0.08) * reinforce * 0.08;
                    relayTarget = Math.Max(relayTarget, river + reinforce * 0.03 * (1.0 - Math.Clamp(slope, 0.0, 1.0)));
                    mask[x, z] = (float)Math.Clamp(relayTarget, 0.0, 1.35);

                    if (reinforce <= 0.26)
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

                    double neighbor = mask[targetX, targetZ];
                    double seep = reinforce * 0.018;
                    mask[targetX, targetZ] = (float)Math.Clamp(Math.Max(neighbor, neighbor + seep), 0.0, 1.35);
                }
            }
        }

        private void ApplyGroundwaterExchangeBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double exchangeWeight = Math.Clamp(
                config.HydrologyFlowPersistence * 0.34 +
                config.RiverEdgeContinuityWeight * 0.33 +
                config.RiverTributaryCaptureWeight * 0.33,
                0.0,
                1.0);
            if (exchangeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double groundwaterBand = Math.Clamp(
                        (seaLevel + config.RiverMouthSmoothRadius * 0.5 - heightMap[x, z]) / Math.Max(8.0, config.HydrologyWaterTableClampRange),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double exchangeSignal = Math.Clamp(
                        hydro * 0.28 +
                        seamHydro * 0.24 +
                        flowNode * 0.2 +
                        seamFlow * 0.18 +
                        groundwaterBand * 0.1,
                        0.0,
                        1.25);
                    exchangeSignal *= 1.0 - Math.Clamp(slope * 0.024 + relief / 44.0 + divergence * 0.28, 0.0, 0.82);
                    if (exchangeSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(river * (0.85 + config.RiverEdgeContinuityWeight * 0.08), exchangeSignal * 0.18);
                    double target = river * (1.0 - exchangeWeight * 0.12) + (river + exchangeSignal) * exchangeWeight * 0.12;
                    mask[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyAnabranchHotspotRelayBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                config.RiverBraidingWeight * 0.38 +
                config.RiverEdgeContinuityWeight * 0.34 +
                config.HydrologyFlowMemoryWeight * 0.28,
                0.0,
                1.0);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.05)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double seaBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, config.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);

                    double hotspot = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 +
                        edgeBand * 0.25 +
                        Math.Max(0.0, river - 0.25) * 0.35,
                        0.0,
                        1.35);
                    double avulsionRisk = Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.25 +
                        slope * config.RiverGradientPenalty * 0.02,
                        0.0,
                        1.0);
                    double relay = hotspot * relayWeight * (0.12 + edgeBand * 0.12 + seaBand * 0.1);
                    relay *= 1.0 - Math.Clamp(avulsionRisk * config.RiverAvulsionResistance * 0.45, 0.0, 0.7);
                    double memoryFloor = Math.Clamp((seamHydro + seamFlow + river) / 3.0, 0.0, 1.2) * 0.1;
                    double target = river * (1.0 - relayWeight * 0.18) + (river + relay + memoryFloor) * relayWeight * 0.18;
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplySeasonalRunoffPulseBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double bridgeWeight = Math.Clamp(
                config.HydrologyFlowPersistence * 0.36 +
                config.RiverConfluenceBoost * 0.34 +
                config.RiverTributaryCaptureWeight * 0.3,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.03)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double floodplainBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, config.RiverMouthSmoothRadius * 2.5), 0.0, 1.0);
                    int seed = ComputeSeasonalSeed(chunkX, chunkZ, x, z);
                    double seasonalNoise = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.019 + 23.0,
                        (chunkZ * sizeZ + z) * 0.019 - 29.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        seed));
                    double runoffPulse = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 +
                        seasonalNoise * 0.32 +
                        floodplainBand * 0.16,
                        0.0,
                        1.35);
                    if (runoffPulse <= 0.24)
                    {
                        continue;
                    }

                    double pulseGuard = 1.0 - Math.Clamp(divergence * 0.42 + slope * config.RiverGradientPenalty * 0.02, 0.0, 0.78);
                    double pulse = runoffPulse * bridgeWeight * pulseGuard * (0.11 + edgeBand * 0.08 + floodplainBand * 0.1);
                    double continuityFloor = Math.Clamp((seamHydro + seamFlow + river) / 3.0, 0.0, 1.2) * 0.08;
                    double target = river * (1.0 - bridgeWeight * 0.16) + (river + pulse + continuityFloor) * bridgeWeight * 0.16;
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private static int ComputeSeasonalSeed(int chunkX, int chunkZ, int localX, int localZ)
        {
            unchecked
            {
                int hash = 0x6B1F5D13;
                hash = (hash * 397) ^ chunkX;
                hash = (hash * 397) ^ chunkZ;
                hash = (hash * 397) ^ localX;
                hash = (hash * 397) ^ localZ;
                return hash;
            }
        }

        private void ApplyHeadwaterSpringBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double springWeight = Math.Clamp(
                config.RiverHeadwaterStabilityWeight * 0.42 +
                config.HydrologyCatchmentWeight * 0.32 +
                config.RiverConfluenceBoost * 0.26,
                0.0,
                1.0);
            if (springWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            double slopePenalty = Math.Max(0.0, config.HydrologySlopePenalty);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = flow[x, z] / 6.0;
                    double seamFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double elevation = Math.Clamp((heightMap[x, z] - seaLevel) / Math.Max(8.0, seaLevel * 0.35), 0.0, 1.25);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    double sourcePotential = Math.Clamp(
                        (hydro * 0.45 + seamHydro * 0.25 + (1.0 - flowNode) * 0.2 + seamFlow * 0.1) * elevation,
                        0.0,
                        1.2);
                    sourcePotential *= 1.0 - Math.Clamp(slope * slopePenalty * 0.015 + divergence * 0.45, 0.0, 0.85);
                    if (sourcePotential <= 0.03)
                    {
                        continue;
                    }

                    double continuity = Math.Clamp((seamHydro + seamFlow + river) / 3.0, 0.0, 1.1);
                    double headwaterBridge = sourcePotential * springWeight * (0.16 + edgeFalloff * 0.18 + continuity * 0.22);
                    double target = river * (1.0 - sourcePotential * 0.18) +
                        (river + headwaterBridge + continuity * 0.08) * sourcePotential * 0.18;
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyFloodPulseContinuityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double pulseWeight = Math.Clamp(
                config.RiverSeamFillStrength * 0.4 +
                config.RiverEdgeContinuityWeight * 0.35 +
                config.HydrologyFlowMemoryWeight * 0.25,
                0.0,
                1.0);
            if (pulseWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
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

                    double river = copy[x, z];
                    if (river <= 0.06)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, config.HydrologyWatershedStitchRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double mouthBlend = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, config.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    double pulseNoise = ComputeEdgeNoise(chunkX, chunkZ, x, z);
                    double seamBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    double pulse = (hydro + seamHydro + flowNode + seamFlow) * 0.25;
                    pulse += mouthBlend * config.RiverDeltaWetlandStrength * 0.25;
                    pulse *= 0.85 + pulseNoise * 0.3;
                    pulse *= 1.0 - Math.Clamp(divergence * 0.35 + slope * config.RiverGradientPenalty * 0.02, 0.0, 0.6);
                    pulse *= 1.0 - Math.Clamp(relief * config.RiverReliefPenaltyWeight * 0.012, 0.0, 0.45);

                    double blend = pulseWeight * seamBand * (0.45 + config.HydrologyFlowPersistence * 0.35);
                    double floor = Math.Max(river * (0.8 + config.RiverEdgeContinuityWeight * 0.1), pulse * 0.14);
                    double target = river * (1.0 - blend) + pulse * blend;
                    mask[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyAnabranchCutoffDamping(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double dampingWeight = Math.Clamp(
                config.RiverBankStabilityClamp * 0.38 +
                config.RiverEdgeContinuityWeight * 0.34 +
                config.RiverMeanderJitter * 0.28,
                0.0,
                1.0);
            if (dampingWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius + 1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.1)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double seaBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.2), 0.0, 1.0);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double gradient = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double cutoffRisk = Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.26 +
                        gradient * config.RiverGradientPenalty * 0.02 +
                        edgeBand * 0.18,
                        0.0,
                        1.0);

                    double convergence = Math.Clamp(
                        (flowNode + seamFlow + hydro + seamHydro) * 0.25,
                        0.0,
                        1.2);
                    convergence *= 1.0 - Math.Clamp(cutoffRisk * 0.55, 0.0, 0.8);

                    double blend = dampingWeight * (0.5 + seaBand * 0.25 + edgeBand * 0.25);
                    double floor = Math.Max(river * (0.82 + config.RiverEdgeContinuityWeight * 0.1), convergence * 0.15);
                    double target = river * (1.0 - blend * 0.2) + convergence * blend * 0.45;
                    target *= 1.0 - Math.Clamp(cutoffRisk * 0.35, 0.0, 0.35);
                    mask[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyDistributaryLeveeStabilityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double leveeWeight = Math.Clamp(
                config.RiverEdgeContinuityWeight * 0.38 +
                config.RiverBankStabilityClamp * 0.34 +
                config.RiverDeltaWetlandStrength * 0.28,
                0.0,
                1.0);
            if (leveeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, config.HydrologyEdgeBlendRadius + 1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.08)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double mouthBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.5),
                        0.0,
                        1.0);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double leveeSeed = Math.Clamp(
                        hydro * 0.28 + seamHydro * 0.24 + flowNode * 0.24 + seamFlow * 0.24,
                        0.0,
                        1.2);
                    double leveeContinuity = 1.0 - Math.Clamp(
                        divergence * 0.44 +
                        Math.Abs(hydro - seamHydro) * 0.26 +
                        slope * config.RiverGradientPenalty * 0.02,
                        0.0,
                        0.78);
                    double leveeBridge = leveeSeed * leveeWeight * (0.46 + edgeBand * 0.28 + mouthBand * 0.26) * leveeContinuity;
                    leveeBridge *= 1.0 - Math.Clamp(relief * config.RiverReliefPenaltyWeight * 0.012, 0.0, 0.4);
                    double floor = Math.Max(river * (0.82 + config.RiverEdgeContinuityWeight * 0.11), leveeSeed * 0.16);
                    double target = river * (1.0 - leveeWeight * 0.18) + leveeBridge * 0.5;
                    mask[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyEstuaryConvergenceBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel,
            int chunkX,
            int chunkZ)
        {
            double estuaryWeight = Math.Clamp(
                config.RiverDeltaWetlandStrength * 0.34 +
                config.RiverConfluenceBoost * 0.33 +
                config.RiverEdgeContinuityWeight * 0.33,
                0.0,
                1.0);
            if (estuaryWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.08)
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
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, config.HydrologyEdgeBlendRadius));
                    double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(Math.Max(1, config.HydrologyEdgeBlendRadius) + 1), 0.0, 1.0);
                    double pulseNoise = ComputeEdgeNoise(chunkX, chunkZ, x, z);

                    double estuarySeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.25 + flowNode * 0.24 + seamFlow * 0.21,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.4 +
                        Math.Abs(hydro - seamHydro) * 0.2 +
                        slope * config.RiverGradientPenalty * 0.02,
                        0.0,
                        0.78);
                    double convergence = estuarySeed * estuaryWeight * (0.52 + seaBand * 0.34 + edgeBand * 0.14);
                    convergence *= continuity;
                    convergence *= 0.88 + pulseNoise * 0.24;
                    convergence *= 1.0 - Math.Clamp(relief * config.RiverReliefPenaltyWeight * 0.012, 0.0, 0.35);

                    double floor = Math.Max(river * (0.84 + config.RiverEdgeContinuityWeight * 0.08), estuarySeed * 0.14);
                    double target = river * (1.0 - estuaryWeight * 0.18) + convergence * 0.5;
                    mask[x, z] = (float)Math.Clamp(Math.Max(target, floor), 0.0, 1.35);
                }
            }
        }

        private void ApplyAvulsionDampingBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double dampingWeight = Math.Clamp(
                config.RiverBankStabilityClamp * 0.45 +
                config.RiverGradientPenalty * 0.35 +
                config.RiverEdgeContinuityWeight * 0.20,
                0.0,
                1.0);
            if (dampingWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.08)
                    {
                        continue;
                    }

                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, config.HydrologyEdgeBlendRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowSample - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double heightToSea = Math.Abs(heightMap[x, z] - seaLevel);
                    double mouthBlend = 1.0 - Math.Clamp(heightToSea / Math.Max(1.0, mouthRadius * 2.0), 0.0, 1.0);

                    double avulsionRisk = Math.Clamp(
                        divergence * 0.45 +
                        hydroGradient * 0.25 +
                        slope * config.RiverGradientPenalty * 0.02 +
                        relief * config.RiverReliefPenaltyWeight * 0.015,
                        0.0,
                        1.0);
                    double continuity = Math.Clamp(
                        (flowSample + seamFlow + hydro + seamHydro) * (0.2 + config.HydrologyFlowMemoryWeight * 0.2),
                        0.0,
                        1.2);
                    double damping = avulsionRisk * dampingWeight * (0.55 + mouthBlend * 0.35);
                    double floor = continuity * config.RiverSeamFillStrength * (0.12 + mouthBlend * 0.08);
                    double target = river * (1.0 - damping) + floor * damping;
                    target = Math.Max(target, floor);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyCrossChunkFloodplainBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int chunkX,
            int chunkZ)
        {
            double continuityWeight = Math.Clamp(config.RiverEdgeContinuityWeight, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double seamFill = Math.Clamp(config.RiverSeamFillStrength, 0.0, 1.0);
            double edgeVarianceClamp = Math.Max(0.001, config.HydrologyEdgeVarianceClamp);
            int edgeRadius = Math.Max(1, config.HydrologyEdgeBlendRadius + config.HydrologyWatershedStitchRadius);
            if (continuityWeight <= 0.0 && memoryWeight <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
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

                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double floodplainMemory = Math.Clamp((hydro + seamHydro + flowSample + seamFlow) * 0.25, 0.0, 1.2);
                    double noise = ComputeEdgeNoise(chunkX, chunkZ, x, z);
                    double memoryBridge = floodplainMemory * (0.1 + continuityWeight * 0.24 + memoryWeight * 0.2);
                    memoryBridge *= 0.8 + noise * 0.4;
                    double seamAnchor = floodplainMemory * seamFill * (0.6 + noise * 0.25);
                    double blend = edgeFalloff * (0.2 + continuityWeight * 0.2 + memoryWeight * 0.15);
                    double target = copy[x, z] * (1.0 - blend) + (seamAnchor + memoryBridge) * blend;
                    double clampRange = Math.Max(0.03, edgeVarianceClamp * (0.4 + edgeFalloff * 0.8));
                    target = Math.Clamp(target, copy[x, z] - clampRange, copy[x, z] + clampRange);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyAnabranchStabilityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double branchWeight = Math.Clamp(
                config.RiverBraidingWeight * 0.4 +
                config.RiverConfluenceBoost * 0.35 +
                config.RiverEdgeContinuityWeight * 0.25,
                0.0,
                1.0);
            if (branchWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            double reliefPenaltyWeight = Math.Clamp(config.RiverReliefPenaltyWeight, 0.0, 1.0);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.15)
                    {
                        continue;
                    }

                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = TerrainMaskUtility.Clamp01(hydrology[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowGradient = Math.Abs(flowNode - seamFlow);
                    double hydroGradient = Math.Abs(hydro - seamHydro);
                    double divergence = Math.Min(1.0, flowGradient / divergenceClamp);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, seaLevel * 1.5);

                    double branchMemory = Math.Clamp(
                        flowNode * 0.4 + seamFlow * 0.25 + hydro * 0.2 + seamHydro * 0.15,
                        0.0,
                        1.25);
                    double branchAssist = branchMemory * branchWeight * (0.18 + config.RiverSeamFillStrength * 0.18);
                    branchAssist *= 1.0 - Math.Clamp(divergence * 0.45 + hydroGradient * 0.25, 0.0, 0.75);

                    double cutoffRisk = Math.Clamp(
                        flowGradient * 0.35 +
                        hydroGradient * 0.25 +
                        slope * config.RiverGradientPenalty * 0.02 +
                        relief * reliefPenaltyWeight * 0.35,
                        0.0,
                        0.9);

                    double floor = Math.Max(river * (0.82 + config.RiverEdgeContinuityWeight * 0.08), branchMemory * 0.16);
                    double target = river * (1.0 - cutoffRisk * 0.22) + branchAssist * (0.35 + cutoffRisk * 0.2);
                    target = Math.Max(target, floor);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyTributaryConvergenceLock(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double confluenceWeight = Math.Clamp(
                config.RiverConfluenceBoost * 0.35 + config.RiverEdgeContinuityWeight * 0.45,
                0.0,
                1.0);
            if (confluenceWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            var copy = (float[,])mask.Clone();
            double memoryWeight = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            double meanderDamping = 1.0 - Math.Clamp(config.RiverMeanderJitter * 0.5, 0.0, 0.45);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    if (flowNode < 0.22)
                    {
                        continue;
                    }

                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double convergence = Math.Clamp(
                        flowNode * 0.45 + seamFlow * 0.25 + hydro * 0.15 + seamHydro * 0.15,
                        0.0,
                        1.2);
                    convergence *= 1.0 - Math.Clamp(divergence * 0.55 + Math.Abs(hydro - seamHydro) * 0.2, 0.0, 0.8);

                    double baseRiver = copy[x, z];
                    double memoryFloor = Math.Max(baseRiver, convergence * (0.16 + memoryWeight * 0.24));
                    double elevationPenalty = Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 3.0),
                        0.0,
                        1.0);
                    double lockWeight = (0.12 + confluenceWeight * 0.26 + memoryWeight * 0.12) * meanderDamping;
                    lockWeight *= 1.0 - elevationPenalty * 0.3;
                    double target = Math.Max(memoryFloor, baseRiver + convergence * lockWeight);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyMouthContinuityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double continuityWeight = Math.Clamp(config.RiverEdgeContinuityWeight, 0.0, 1.0);
            double deltaWeight = Math.Clamp(config.RiverDeltaWetlandStrength, 0.0, 1.0);
            if (continuityWeight <= 0.0 && deltaWeight <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int mouthRadius = Math.Max(2, config.RiverMouthSmoothRadius);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river < 0.18)
                    {
                        continue;
                    }

                    int elevation = heightMap[x, z];
                    double seaProximity = 1.0 - Math.Clamp(
                        Math.Abs(elevation - seaLevel) / Math.Max(1.0, mouthRadius * 2.5),
                        0.0,
                        1.0);
                    if (seaProximity <= 0.01)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double flowGradient = Math.Abs(flowSample - seamFlow);
                    double hydroGradient = Math.Abs(hydro - seamHydro);

                    double mouthMemory = Math.Clamp(
                        river * 0.55 + seamHydro * 0.2 + seamFlow * 0.25,
                        0.0,
                        1.35);
                    double bridge = seaProximity * (continuityWeight * 0.26 + deltaWeight * 0.2);
                    bridge *= 1.0 - Math.Clamp(flowGradient * 0.55 + hydroGradient * 0.45, 0.0, 0.85);
                    double target = Math.Max(river, mouthMemory + bridge);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyCatchmentBraidingBridge(float[,] mask, float[,] hydrology, float[,] flow)
        {
            double braidingWeight = Math.Clamp(config.RiverBraidingWeight, 0.0, 1.0);
            if (braidingWeight <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();
            double confluenceBoost = Math.Clamp(config.RiverConfluenceBoost, 0.0, 2.0);
            double continuityWeight = Math.Clamp(config.RiverEdgeContinuityWeight, 0.0, 1.0);
            double seamFill = Math.Clamp(config.RiverSeamFillStrength, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    if (flowSample < 0.18)
                    {
                        continue;
                    }

                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowSample - seamFlow) / divergenceClamp);
                    double catchment = Math.Clamp(
                        flowSample * 0.55 + seamFlow * 0.25 + seamHydro * 0.2,
                        0.0,
                        1.25);
                    double bridge = catchment * braidingWeight * (0.08 + continuityWeight * 0.24 + confluenceBoost * 0.06);
                    bridge *= 1.0 - Math.Clamp(divergence * 0.45 + Math.Abs(hydro - seamHydro) * 0.25, 0.0, 0.7);

                    double minFloor = Math.Max(copy[x, z], catchment * seamFill * 0.18);
                    double target = Math.Max(minFloor, copy[x, z] + bridge);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyRiparianEdgeFeather(float[,] mask, float[,] hydrology, float[,] flow)
        {
            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(1, config.HydrologyEdgeBlendRadius);
            double feather = Math.Clamp(config.HydrologySeamRelaxBlend * 0.35 + config.RiverEdgeFeather * 0.5, 0.0, 1.0);
            double clampRange = Math.Max(0.001, config.HydrologyEdgeVarianceClamp);
            double guardWeight = Math.Clamp(config.HydrologyEdgeStabilityWeight, 0.0, 1.0);
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
                    double interior = SampleInterior(copy, x, z);
                    double hydroGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrology, x, z) - hydrology[x, z]);
                    double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flow, x, z) - flow[x, z]);
                    double blend = feather * falloff;
                    double guard = Math.Clamp((hydroGradient + flowGradient) * guardWeight * 0.35, 0.0, 0.6);

                    double target = copy[x, z] * (1.0 - blend) + interior * blend;
                    target = Math.Clamp(target * (1.0 - guard), copy[x, z] - clampRange, copy[x, z] + clampRange);
                    mask[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyConfluenceMemory(float[,] mask, float[,] hydrology, float[,] flow)
        {
            double confluenceBoost = Math.Clamp(config.RiverConfluenceBoost, 0.0, 2.0);
            if (confluenceBoost <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();
            double continuityWeight = Math.Clamp(config.RiverEdgeContinuityWeight, 0.0, 1.0);
            double flowMemoryWeight = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            int edgeRadius = Math.Max(1, config.HydrologyEdgeBlendRadius);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double flowSample = flow[x, z] / 6.0;
                    double seamFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0;
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double hydrologySample = hydrology[x, z];
                    double divergence = Math.Min(1.0, Math.Abs(flowSample - seamFlow) / divergenceClamp);
                    double confluenceSeed = Math.Clamp((flowSample + seamFlow) * 0.5 + seamHydro * 0.35, 0.0, 1.0);
                    if (confluenceSeed <= 0.02)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double gradient = Math.Abs(seamHydro - hydrologySample);
                    double bridge = confluenceSeed * confluenceBoost * (0.1 + continuityWeight * 0.25 + flowMemoryWeight * 0.2);
                    bridge *= 1.0 - Math.Clamp(divergence * 0.35 + gradient * 0.3, 0.0, 0.55);
                    bridge *= 1.0 + edgeFalloff * continuityWeight * 0.15;

                    double target = copy[x, z] + bridge;
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyContinuityGuard(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int edgeRadius,
            double continuityWeight,
            double varianceClamp)
        {
            continuityWeight = Math.Clamp(continuityWeight, 0.0, 1.0);
            edgeRadius = Math.Max(1, edgeRadius);
            varianceClamp = Math.Max(0.001, varianceClamp);
            if (continuityWeight <= 0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
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
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0;
                    double flowSample = flow[x, z] / 6.0;
                    double gradient = Math.Abs(seamHydro - hydrology[x, z]) + Math.Abs(seamFlow - flowSample);
                    double seamAnchor = (copy[x, z] + hydrology[x, z] + (float)seamHydro) / 3.0;
                    seamAnchor = (seamAnchor + (float)seamFlow * 0.5f) * 0.75;
                    double blend = continuityWeight * falloff * (0.65 + gradient * 0.35);
                    double clampRange = Math.Max(varianceClamp * falloff, 0.02);
                    double target = copy[x, z] * (1.0 - blend) + seamAnchor * blend;
                    target = Math.Clamp(target, copy[x, z] - clampRange, copy[x, z] + clampRange);
                    mask[x, z] = TerrainMaskUtility.Clamp01((float)target);
                }
            }
        }

        private void ApplyHydrologyStability(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int iterations,
            double blend,
            double gradientClamp)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            gradientClamp = Math.Max(0.0001, gradientClamp);
            if (iterations == 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float centre = mask[x, z];
                        float interior = TerrainMaskUtility.SampleInterior(mask, x, z);
                        double wetness = Math.Max(hydrology[x, z], flow[x, z] * 0.5f);
                        double variance = TerrainMaskUtility.SampleVariance(mask, x, z);
                        double hydroGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrology, x, z) - hydrology[x, z]);
                        double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flow, x, z) - flow[x, z]);
                        double gradient = Math.Min(1.0, hydroGradient + flowGradient * 0.5);
                        double weight = blend * (0.35 + wetness * 0.25 + variance * 0.15 + gradient * 0.2);
                        double stabilised = centre * (1.0 - weight) + interior * weight;
                        stabilised *= 1.0 - Math.Clamp(gradient * blend * 0.25, 0.0, 0.35);
                        stabilised += hydroGradient * blend * 0.05;
                        buffer[x, z] = (float)Math.Clamp(stabilised, 0.0, Math.Max(1.35, centre + gradientClamp * (0.5 + gradient * 0.35)));
                    }
                }

                Array.Copy(buffer, mask, buffer.Length);
            }
        }

        private double ApplyEdgeBlend(double pressure, float hydrology, int x, int z, int chunkSize)
        {
            int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
            int edgeRadius = Math.Max(1, config.HydrologyEdgeBlendRadius);
            if (edgeDistance >= edgeRadius)
            {
                return pressure;
            }

            double blend = 1.0 - edgeDistance / (double)(edgeRadius + 1);
            double seamFill = Math.Clamp(config.RiverSeamFillStrength, 0.0, 1.0);
            double hydrologyPull = hydrology * seamFill * blend;
            return pressure * (1.0 - hydrologyPull) + hydrologyPull;
        }

        private void ApplyFloodplainMeanderStabilityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.RiverEdgeContinuityWeight * 0.38 +
                config.RiverConfluenceBoost * 0.32 +
                config.RiverBraidingWeight * 0.3,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.2);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.2);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(2, config.HydrologyWatershedStitchRadius + 1));
                    double floodplainBias = Math.Clamp((seaLevel + 12 - heightMap[x, z]) / 18.0, 0.0, 1.0);
                    double divergence = Math.Abs(flowNode - seamFlow);
                    double continuity = Math.Clamp((river + hydro + seamHydro + seamFlow) * 0.25, 0.0, 1.2);

                    double meanderStability = continuity * (0.2 + floodplainBias * 0.24 + bridgeWeight * 0.28);
                    meanderStability *= 1.0 - Math.Clamp(slope * 0.03 + relief / 36.0 + divergence * 0.32, 0.0, 0.82);
                    if (meanderStability <= 0.01)
                    {
                        continue;
                    }

                    double target = river * (1.0 - bridgeWeight * 0.14) +
                        (river + meanderStability) * bridgeWeight * 0.14;
                    if (slope > 10.0)
                    {
                        target *= 1.0 - Math.Clamp((slope - 10.0) * 0.02, 0.0, 0.18);
                    }

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyAlluvialChannelAnchorBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double anchorWeight = Math.Clamp(
                config.RiverEdgeContinuityWeight * 0.34 +
                config.RiverBankStabilityClamp * 0.33 +
                config.HydrologyCatchmentWeight * 0.33,
                0.0,
                1.0);
            if (anchorWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 1);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.02)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.2);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.2);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 8 - heightMap[x, z]) / 14.0, 0.0, 1.0);
                    double divergence = Math.Abs(flowNode - seamFlow);
                    double continuity = Math.Clamp((river + hydro + seamHydro + flowNode + seamFlow) * 0.2, 0.0, 1.2);

                    double anchorPressure = continuity * (0.18 + anchorWeight * 0.34 + floodplainBias * 0.22);
                    anchorPressure *= 1.0 - Math.Clamp(slope * 0.028 + relief / 38.0 + divergence * 0.28, 0.0, 0.82);
                    if (anchorPressure <= 0.01)
                    {
                        continue;
                    }

                    double target = river * (1.0 - anchorWeight * 0.12) +
                        (river + anchorPressure) * anchorWeight * 0.12;
                    if (slope > 11.0)
                    {
                        target *= 1.0 - Math.Clamp((slope - 11.0) * 0.018, 0.0, 0.16);
                    }

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyFloodplainRetentionAnchorBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double retentionWeight = Math.Clamp(
                config.RiverDeltaWetlandStrength * 0.36 +
                config.RiverEdgeContinuityWeight * 0.34 +
                config.HydrologyFlowMemoryWeight * 0.30,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 2);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 16.0, 0.0, 1.0);
                    double divergence = Math.Abs(flowNode - seamFlow);
                    double continuity = Math.Clamp((river + hydro + seamHydro + flowNode + seamFlow) * 0.2, 0.0, 1.25);
                    double retention = continuity * (0.2 + floodplainBias * 0.22 + retentionWeight * 0.26);
                    retention *= 1.0 - Math.Clamp(slope * 0.028 + relief / 42.0 + divergence * 0.3, 0.0, 0.82);
                    if (retention <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(river * (0.82 + config.RiverEdgeContinuityWeight * 0.1), continuity * 0.16);
                    double target = river * (1.0 - retentionWeight * 0.12) + (river + retention) * retentionWeight * 0.12;
                    target = Math.Max(target, floor);
                    if (slope > 10.5)
                    {
                        target *= 1.0 - Math.Clamp((slope - 10.5) * 0.016, 0.0, 0.14);
                    }

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
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

        private void ApplyThalwegContinuityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap)
        {
            double continuityWeight = Math.Clamp(
                config.RiverFlowAlignmentWeight * 0.38 +
                config.RiverConfluenceBoost * 0.32 +
                config.HydrologyFlowMemoryWeight * 0.30,
                0.0,
                1.0);
            if (continuityWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.05)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double curvature = Math.Abs(ComputeCurvature(heightMap, x, z));
                    double meanderPenalty = Math.Clamp(config.RiverMeanderJitter * 0.25 + curvature / 18.0, 0.0, 0.32);
                    double channelSignal = Math.Clamp(
                        flowNode * 0.34 +
                        seamFlow * 0.3 +
                        hydro * 0.2 +
                        seamHydro * 0.16,
                        0.0,
                        1.25);
                    double stability = 1.0 - Math.Clamp(slope * 0.024 + meanderPenalty, 0.0, 0.82);
                    double memoryFloor = Math.Max(river * 0.84, channelSignal * 0.18);
                    double target = river * (1.0 - continuityWeight * 0.14) +
                        (river + channelSignal * stability) * continuityWeight * 0.14;
                    target = Math.Max(target, memoryFloor);

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyConfluenceLagStorageBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap)
        {
            double storageWeight = Math.Clamp(
                config.RiverDeltaWetlandStrength * 0.34 +
                config.HydrologyFlowPersistence * 0.36 +
                config.RiverTributaryCaptureWeight * 0.30,
                0.0,
                1.0);
            if (storageWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.03)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.4);
                    double neighborFlow = Math.Clamp(
                        (flow[x - 1, z] + flow[x + 1, z] + flow[x, z - 1] + flow[x, z + 1]) / 24.0,
                        0.0,
                        1.4);
                    double flowGradient = Math.Abs(flow[x + 1, z] - flow[x - 1, z]) + Math.Abs(flow[x, z + 1] - flow[x, z - 1]);
                    double convergence = Math.Clamp(flowGradient / 12.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double reliefPenalty = Math.Clamp(slope * 0.02 + config.RiverReliefPenaltyWeight * 0.25, 0.0, 0.85);
                    double storageSignal = Math.Clamp(
                        flowNode * 0.42 +
                        neighborFlow * 0.28 +
                        hydro * 0.18 +
                        convergence * 0.12,
                        0.0,
                        1.25);
                    storageSignal *= 1.0 - reliefPenalty;

                    double floor = Math.Max(river * 0.88, storageSignal * 0.42);
                    double target = river * (1.0 - storageWeight * 0.14) +
                        (river + storageSignal * 0.72) * storageWeight * 0.14;
                    target = Math.Max(target, floor);
                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyConfluenceFloodplainRelayBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                config.RiverConfluenceBoost * 0.36 +
                config.RiverDeltaWetlandStrength * 0.34 +
                config.HydrologyFlowPersistence * 0.30,
                0.0,
                1.0);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 1);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.35);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.35);
                    double convergence = Math.Max(0.0, seamFlow - flowNode) + Math.Max(0.0, seamHydro - hydro);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double floodplainBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 18.0, 0.0, 1.0);
                    double continuity = Math.Clamp((river + hydro + seamHydro + flowNode + seamFlow) * 0.2, 0.0, 1.35);
                    double relay = continuity * (0.2 + floodplainBias * 0.28 + convergence * 0.18);
                    relay *= 1.0 - Math.Clamp(slope * 0.028 + relief / 42.0, 0.0, 0.84);
                    if (relay <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(river * (0.86 + config.RiverEdgeContinuityWeight * 0.08), continuity * 0.18);
                    double target = river * (1.0 - relayWeight * 0.14) +
                        (river + relay) * relayWeight * 0.14;
                    target = Math.Max(target, floor);
                    if (slope > 10.0)
                    {
                        target *= 1.0 - Math.Clamp((slope - 10.0) * 0.016, 0.0, 0.14);
                    }

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private void ApplyOxbowCutoffContinuityBridge(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.RiverEdgeContinuityWeight * 0.36 +
                config.RiverMeanderJitter * 0.34 +
                config.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int reliefRadius = Math.Max(2, config.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, config.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.04)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.35);
                    double seamFlow = Math.Clamp(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0, 0.0, 1.35);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double curvature = Math.Abs(ComputeCurvature(heightMap, x, z));
                    double floodplainBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 18.0, 0.0, 1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double cutoffSignal = Math.Clamp(
                        hydro * 0.22 +
                        seamHydro * 0.18 +
                        flowNode * 0.22 +
                        seamFlow * 0.18 +
                        curvature * 0.12 +
                        floodplainBias * 0.08,
                        0.0,
                        1.25);
                    cutoffSignal *= 1.0 - Math.Clamp(slope * 0.024 + relief / 42.0 + divergence * 0.25, 0.0, 0.86);
                    if (cutoffSignal <= 0.01)
                    {
                        continue;
                    }

                    double continuityFloor = Math.Max(river * (0.86 + config.RiverEdgeContinuityWeight * 0.08), cutoffSignal * 0.2);
                    double target = river * (1.0 - bridgeWeight * 0.12) + (river + cutoffSignal) * bridgeWeight * 0.12;
                    target = Math.Max(target, continuityFloor);
                    if (curvature > 0.95)
                    {
                        target *= 1.0 - Math.Clamp((curvature - 0.95) * 0.08, 0.0, 0.12);
                    }

                    mask[x, z] = (float)Math.Clamp(target, 0.0, 1.35);
                }
            }
        }

        private static void FeatherEdges(float[,] mask, double feather, double seamFill)
        {
            feather = Math.Clamp(feather, 0.0, 1.0);
            seamFill = Math.Clamp(seamFill, 0.0, 1.0);
            if (feather <= 0.0 && seamFill <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var buffer = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float centre = mask[x, z];
                    float neighbour = TerrainMaskUtility.Clamp01(SampleInterior(mask, x, z));
                    float blended = (float)(centre * (1.0 - feather) + neighbour * feather);
                    buffer[x, z] = Math.Max(blended, centre * (float)(1.0 - seamFill));
                }
            }

            Array.Copy(buffer, mask, buffer.Length);
        }

        private static float SampleInterior(float[,] field, int x, int z)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            int cx = Math.Clamp(x, 1, sizeX - 2);
            int cz = Math.Clamp(z, 1, sizeZ - 2);
            float sum = 0f;
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = Math.Clamp(cx + dx, 1, sizeX - 2);
                    int nz = Math.Clamp(cz + dz, 1, sizeZ - 2);
                    sum += field[nx, nz];
                    count++;
                }
            }

            return count == 0 ? field[cx, cz] : sum / count;
        }

        private int CreateNoiseSeed(int chunkX, int chunkZ, int localX, int localZ, int salt)
        {
            uint mixed = MixSeed((uint)worldSeedHash, (uint)chunkX, (uint)chunkZ, (uint)localX, (uint)localZ, (uint)salt);
            return (int)(mixed & 0x7FFFFFFF);
        }

        private static double ComputeEdgeNoise(int chunkX, int chunkZ, int x, int z)
        {
            uint value = MixSeed((uint)chunkX, (uint)chunkZ, (uint)x, (uint)z, 0x5F3759DFu);
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
    }
}
