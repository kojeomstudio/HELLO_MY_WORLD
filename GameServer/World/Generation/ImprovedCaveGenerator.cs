using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Hydrology-aware cave mask generator that suppresses rivers, seals chunk edges,
    /// and biases support pillars toward saturated terrain.
    /// </summary>
    public sealed class ImprovedCaveGenerator
    {
        private readonly CaveConfig config;
        private readonly int worldSeedHash;
        private readonly double depthWeight;

        public ImprovedCaveGenerator(CaveConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            worldSeedHash = (int)(worldSeed ^ 0x5A3C7B01);
            depthWeight = Math.Clamp(
                1.0 - (config.HydrologyStabilityWeight + config.FlowStabilityWeight + config.RoughnessStabilityWeight),
                0.05,
                0.45);
        }

        public bool[,,] BuildMask(
            int chunkX,
            int chunkZ,
            int chunkSize,
            int worldHeight,
            int[,] heightMap,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            float[,] erosionRisk,
            int seaLevel)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            double horizontal = Math.Max(0.0001, config.HorizontalFrequency);
            double vertical = Math.Max(0.0001, config.VerticalFrequency);
            double ceilingMoistureWeight = Math.Clamp(config.CeilingMoistureWeight, 0.0, 1.0);
            double ceilingMoistureClampWeight = Math.Clamp(config.CeilingMoistureClamp, 0.0, 1.0);
            double floodedNoiseFrequency = Math.Max(0.0001, config.FloodedCaveNoiseFrequency);
            double floodedThreshold = Math.Clamp(config.FloodedCaveThreshold, 0.0, 2.0);
            double floodedProximityWeight = Math.Clamp(config.FloodedCaveProximityToWaterTableWeight, 0.0, 1.0);
            double lavaThreshold = Math.Clamp(config.LavaThreshold, 0.0, 1.0);
            double waterThreshold = Math.Clamp(config.WaterThreshold, 0.0, 1.0);
            double moistureFlowClamp = Math.Max(0.0, config.MoistureFlowClamp);
            double aquiferBarrierWeight = Math.Clamp(config.AquiferBarrierWeight, 0.0, 1.0);
            double groundwaterConnectivityWeight = Math.Clamp(config.GroundwaterConnectivityWeight, 0.0, 1.0);
            double caveVentilationBias = Math.Clamp(config.CaveVentilationBias, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    double floodedNoiseBase = SimplexNoise.Generate(
                        (chunkX * chunkSize + x) * floodedNoiseFrequency + 17.0,
                        (chunkZ * chunkSize + z) * floodedNoiseFrequency - 9.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, 0, 311)) * 0.5 + 0.5;

                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    float flowMemory = TerrainMaskUtility.Clamp01((flow + TerrainMaskUtility.SampleInterior(flowMask, x, z)) * 0.5f);
                    double flowMemoryClamped = Math.Min(flowMemory, moistureFlowClamp);
                    float seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    float seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    float riverPressure = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    float seamRiver = riverMask != null ? TerrainMaskUtility.SampleInterior(riverMask, x, z) : riverPressure;
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double edgeFactor = ComputeEdgeFalloff(x, z, chunkSize);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrology);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double hydrologyVariance = TerrainMaskUtility.SampleVariance(hydrologyMask, x, z, 2);
                    double flowVariance = TerrainMaskUtility.SampleVariance(flowMask, x, z, 2);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double slopeStability = 1.0 - Math.Clamp(slope * config.CeilingStabilityWeight * 0.02, 0.0, 0.35);
                    double hydrologyShadow = Math.Clamp(
                        hydrology * config.HydrologyStabilityWeight +
                        seamHydro * config.HydrologyStabilityWeight * 0.25 +
                        flowMemoryClamped * config.FlowStabilityWeight * 0.35,
                        0.0,
                        1.0);
                    double moistureContinuity = Math.Clamp((hydrologyGradient + flowGradient) * config.MoistureRetentionWeight * 0.25, 0.0, 0.55);
                    double flowShadowDrift = Math.Clamp(Math.Abs(flowMemory - flow) * config.MoistureRetentionWeight * 0.5, 0.0, moistureFlowClamp);
                    double slopeThresholdPenalty = Math.Clamp(slope * config.CeilingStabilityWeight * 0.015, 0.0, 0.25);
                    double varianceBrake = Math.Clamp((hydrologyVariance + flowVariance) * config.RoughnessStabilityWeight * 0.2, 0.0, 0.4);
                    double saturationBrake = Math.Clamp(
                        (hydrology + flow + seamHydro + seamFlow) * config.MoistureRetentionWeight * 0.15,
                        0.0,
                        0.45);
                    double seamStability = 1.0 - Math.Clamp(hydrologyGradient * config.EdgeSealStrength, 0.0, 0.45);
                    seamStability *= 1.0 - Math.Clamp(flowGradient * config.EdgeSealStrength * 0.35, 0.0, 0.35);
                    double flowShadow = Math.Clamp(flow * config.FlowStabilityWeight + hydrology * config.HydrologyStabilityWeight, 0.0, moistureFlowClamp);
                    double variancePenalty = Math.Clamp(hydrologyVariance * 0.25 + flowVariance * 0.18, 0.0, 0.35);
                    double stabilityPenalty = Math.Clamp(
                        flowShadow * 0.35 +
                        hydrologyGradient * 0.25 +
                        riverPressure * 0.25 +
                        flowGradient * 0.25 +
                        erosion * config.RiverSuppressionWeight * 0.35 +
                        variancePenalty * 0.6,
                        0.0,
                        0.95);
                    double continuityPenalty = Math.Clamp(Math.Abs(seamHydro - hydrology) + Math.Abs(seamFlow - flow) * 0.5, 0.0, 1.5);
                    double stability = ComputeColumnStability(surface, hydrology, riverPressure, flow, edgeFactor) * seamStability;
                    stability *= 1.0 - variancePenalty * 0.3;
                    stability *= 1.0 - Math.Clamp(flowVariance * 0.2, 0.0, 0.2);
                    stability *= 1.0 - varianceBrake * 0.5;
                    double seamMemory = (seamFlow + flowMemory) * 0.5;
                    double seamContinuity = 1.0 - Math.Clamp(continuityPenalty * config.EdgeSealStrength * 0.35, 0.0, 0.45);
                    double ceilingClamp = Math.Clamp(
                        hydrology * ceilingMoistureWeight +
                        flowMemory * ceilingMoistureWeight * 0.5 +
                        hydrologyGradient * ceilingMoistureWeight * 0.35,
                        0.0,
                        ceilingMoistureClampWeight);
                    double ceilingMoisturePenalty = Math.Clamp(
                        hydrology * ceilingMoistureWeight +
                        flow * ceilingMoistureWeight * 0.5 +
                        hydrologyGradient * ceilingMoistureWeight * 0.25,
                        0.0,
                        ceilingMoistureClampWeight);
                    stability *= 1.0 - stabilityPenalty * 0.4;
                    stability *= 1.0 - ceilingMoisturePenalty * 0.2;
                    stability *= 1.0 - ceilingClamp * 0.15;
                    stability *= seamContinuity;
                    stability *= 1.0 - continuityPenalty * 0.15;
                    stability *= 1.0 - Math.Clamp(erosion * config.EdgeSealStrength * 0.3, 0.0, 0.3);
                    stability *= 1.0 - saturationBrake * 0.35;
                    stability *= slopeStability;
                    double riparianGuard = Math.Clamp(
                        (hydrology + flowMemoryClamped + riverPressure) * config.RiparianCaveGuardWeight,
                        0.0,
                        0.65);
                    double riparianCeilingGuard = Math.Clamp(
                        (hydrologyGradient + flowGradient + riverPressure + seamMemory) * config.CeilingStabilityWeight * 0.25,
                        0.0,
                        0.5);
                    stability *= 1.0 - riparianCeilingGuard * 0.35;
                    stability *= 1.0 - riparianGuard * 0.3;
                    double wetnessRetention = hydrology * config.MoistureRetentionWeight + flowMemoryClamped * config.MoistureRetentionWeight * 0.35;
                    wetnessRetention += erosion * config.MoistureRetentionWeight * 0.2;
                    stability *= 1.0 - moistureContinuity * 0.35;
                    stability *= 1.0 - hydrologyShadow * 0.1;
                    double riparianSuppression = Math.Clamp(
                        (riverPressure + hydrologyGradient + flowGradient + seamMemory) * config.RiverSuppressionWeight * 0.5,
                        0.0,
                        0.6);
                    double aquiferPenalty = Math.Clamp(
                        wetnessRetention +
                        riverPressure * config.RiverSuppressionWeight * 0.25,
                        0.0,
                        1.0);
                    stability *= 1.0 - aquiferPenalty * 0.3;
                    double hydrologyEnvelope = (hydrology + seamHydro + flowMemory) * 0.333;
                    double groundwaterConnectivity = Math.Clamp(
                        (hydrologyEnvelope + seamMemory + flowMemory) * 0.333,
                        0.0,
                        1.0);
                    double ventilationPotential = Math.Clamp(
                        (1.0 - hydrology) * (1.0 - flow) * (1.0 - Math.Clamp(slope * 0.04, 0.0, 0.75)),
                        0.0,
                        1.0);
                    double aquiferBarrier = Math.Clamp(
                        (hydrologyEnvelope + seamMemory + riverPressure) * aquiferBarrierWeight * 0.5,
                        0.0,
                        0.75);
                    double flowContinuity = Math.Clamp(Math.Abs(flowMemory - flow) * config.FlowStabilityWeight * 0.5, 0.0, 0.6);
                    double riparianBridge = Math.Clamp((hydrologyEnvelope + riverPressure) * config.RiverSuppressionWeight * 0.35, 0.0, 0.65);
                    double divergenceGuard = Math.Clamp(
                        (hydrologyGradient + flowGradient) * config.CeilingMoistureClamp * 0.25 +
                        Math.Abs(seamRiver - riverPressure) * config.EdgeSealStrength * 0.25,
                        0.0,
                        0.6);
                    double erosionGradient = Math.Abs(erosion - TerrainMaskUtility.SampleInterior(erosionRisk, x, z));
                    double continuityStabilizer = 1.0 - Math.Clamp((hydrologyGradient + flowGradient + erosionGradient) * config.EdgeSealStrength * 0.2, 0.0, 0.45);
                    double seamMemoryBoost = Math.Clamp((seamHydro + flowMemory) * config.MoistureRetentionWeight * 0.15, 0.0, 0.35);
                    stability *= continuityStabilizer;
                    stability = stability * (1.0 - riparianGuard * 0.1) + stability * (1.0 + seamMemoryBoost) * 0.1;
                    stability *= 1.0 - divergenceGuard * 0.35;
                    double karstPotential = Math.Clamp(
                        (1.0 - Math.Clamp(slope * 0.05, 0.0, 0.6)) * (hydrologyEnvelope * 0.6 + flowMemory * 0.4),
                        0.0,
                        1.0);
                    double roofKarstGuard = Math.Clamp(karstPotential * config.CaveEntranceFlowDampening * 0.35, 0.0, 0.4);
                    stability *= 1.0 - roofKarstGuard;
                    stability *= 1.0 - aquiferBarrier * 0.28;
                    stability *= 1.0 - groundwaterConnectivity * groundwaterConnectivityWeight * 0.2;
                    stability = stability * (1.0 - caveVentilationBias * 0.1) +
                        stability * (1.0 + ventilationPotential * 0.14) * caveVentilationBias * 0.1;
                    double seamVaultStability = Math.Clamp(
                        (1.0 - hydrologyGradient) * 0.35 +
                        (1.0 - flowGradient) * 0.25 +
                        seamMemory * 0.2 +
                        (1.0 - erosionGradient) * 0.2,
                        0.0,
                        1.0);
                    double seamVaultWeight = Math.Clamp(
                        config.EdgeSealStrength * 0.32 +
                        config.CaveEntranceFlowDampening * 0.28 +
                        config.AquiferBarrierWeight * 0.2 +
                        config.MoistureRetentionWeight * 0.2,
                        0.0,
                        1.0);
                    stability *= 1.0 - (1.0 - seamVaultStability) * seamVaultWeight * 0.22;

                    for (int y = 1; y < Math.Min(surface - 1, worldHeight - 2); y++)
                    {
                        double depthFactor = 1.0 - (double)y / Math.Max(1, surface);
                        double warpX = (chunkX * chunkSize + x) * horizontal;
                        double warpZ = (chunkZ * chunkSize + z) * horizontal;
                        double warpY = y * vertical;

                        var warp = SimplexNoise.DomainWarp(
                            warpX,
                            warpZ + warpY,
                            horizontal * 0.35,
                            vertical * 0.6,
                            4.0,
                            2.5,
                            CreateNoiseSeed(chunkX, chunkZ, x, z, y, 313));

                        double primary = SimplexNoise.Generate(
                            warpX + warp.dx,
                            warpZ + warp.dz + warpY,
                            1.0,
                            3,
                            1.0,
                            0.55,
                            CreateNoiseSeed(chunkX, chunkZ, x, z, y, 317));

                        double secondary = PerlinNoise.Generate(
                            warpX + 17.0,
                            warpZ - 11.0,
                            vertical * 0.5,
                            2,
                            1.0,
                            0.6,
                            CreateNoiseSeed(chunkX, chunkZ, x, z, y, 331));

                        double detail = Math.Abs(SimplexNoise.Generate(
                            warpX * 1.35 - 23.0,
                            warpZ * 1.35 + warpY * 0.35 + 17.0,
                            1.0,
                            2,
                            1.0,
                            0.55,
                            CreateNoiseSeed(chunkX, chunkZ, x, z, y, 337)));

                        double density = (primary * 0.55) + (secondary * 0.25) + (detail * 0.2);
                        double moisturePenalty = hydrology * config.HydrologyStabilityWeight + riverPressure * config.RiverSuppressionWeight + wetnessRetention * 0.35;
                        double roughnessBias = (0.5 + SimplexNoise.Generate(warpX * 0.8, warpZ * 0.8, 1.0, 1, 1.0, 0.5, CreateNoiseSeed(chunkX, chunkZ, x, z, y, 347)) * 0.5) * config.RoughnessStabilityWeight;
                        double flowPenalty = flow * config.FlowStabilityWeight;
                        double flowMemoryClamp = Math.Clamp(flowMemory * config.MoistureRetentionWeight, 0.0, 1.0);
                        double roofThickness = surface - y;
                        double roofGuard = Math.Clamp(1.0 / Math.Max(1.0, roofThickness), 0.0, 1.0);
                        double ceilingHydration = Math.Clamp(hydrologyEnvelope * config.CeilingMoistureWeight * 0.35, 0.0, 0.35);
                        double divergenceBrake = Math.Min(1.0, Math.Abs(flowMemory - seamHydro) / Math.Max(0.0001, moistureFlowClamp));
                        double groundwaterThreshold = groundwaterConnectivity * groundwaterConnectivityWeight * 0.12;
                        double ventilationThreshold = ventilationPotential * caveVentilationBias * depthFactor * 0.11;
                        double threshold = config.Threshold + moisturePenalty * 0.35 + flowPenalty * 0.35 + roughnessBias * 0.25;
                        threshold += groundwaterThreshold;
                        threshold -= ventilationThreshold;
                        threshold -= depthFactor * depthWeight * 0.6;
                        threshold -= Math.Clamp(detail * depthFactor * 0.2, 0.0, 0.2);
                        threshold += wetnessRetention * 0.15;
                        threshold += edgeFactor * config.EdgeSealStrength * 0.35;
                        threshold += seamMemory * config.FlowStabilityWeight * 0.15;
                        threshold += Math.Clamp(hydrologyGradient * (config.EdgeSealStrength + config.HydrologyStabilityWeight * 0.25), 0.0, 0.35);
                        threshold += Math.Clamp(flowGradient * config.EdgeSealStrength * 0.2, 0.0, 0.2);
                        threshold += riparianSuppression * 0.25;
                        threshold += riparianGuard * 0.08;
                        threshold += stabilityPenalty * 0.25;
                        threshold += varianceBrake * 0.35;
                        threshold += saturationBrake;
                        threshold += ceilingMoisturePenalty * 0.2;
                        threshold += slopeThresholdPenalty * 0.5;
                        threshold += ceilingClamp * 0.1;
                        threshold += riparianCeilingGuard * 0.2;
                        threshold += riparianBridge * 0.35;
                        threshold += flowContinuity * 0.2;
                        threshold += hydrologyShadow * 0.2;
                        threshold += moistureContinuity * 0.25;
                        threshold += flowShadowDrift * 0.1;
                        threshold += divergenceGuard * 0.45;
                        threshold += aquiferPenalty * 0.2;
                        threshold += aquiferBarrier * 0.25;
                        threshold += Math.Clamp(flowShadow * 0.15, 0.0, 0.25);
                        threshold += variancePenalty * 0.25;
                        threshold += Math.Clamp(flowVariance * config.RoughnessStabilityWeight * 0.2, 0.0, 0.25);
                        threshold += flowMemoryClamp * 0.15;
                        threshold += erosion * config.RiverSuppressionWeight * 0.2;
                        threshold += Math.Clamp(slope * config.CeilingStabilityWeight * 0.02, 0.0, 0.2);
                        threshold += roofGuard * config.EdgeSealStrength * 0.2;
                        threshold += ceilingHydration;
                        threshold += divergenceBrake * config.FlowStabilityWeight * 0.2;
                        threshold += karstPotential * config.CaveEntranceFlowDampening * 0.12;
                        threshold += Math.Clamp((1.0 - depthFactor) * karstPotential * 0.08, 0.0, 0.15);
                        double seamVaultDrift = (1.0 - seamVaultStability) * (1.0 - depthFactor);
                        threshold += Math.Clamp(seamVaultDrift * seamVaultWeight * 0.14, 0.0, 0.16);
                        threshold -= Math.Clamp(seamVaultStability * seamVaultWeight * depthFactor * 0.05, 0.0, 0.08);
                        if (y >= surface - Math.Max(2, config.RiparianPlugDepth) && riparianSuppression > 0.2)
                        {
                            continue;
                        }
                        double depthBelowSea = seaLevel - y;
                        double floodedBias = Math.Clamp(depthBelowSea / Math.Max(1.0, seaLevel), -1.0, 1.0) * floodedProximityWeight;
                        double floodedNoise = floodedNoiseBase;
                        double floodedPressure = floodedNoise + floodedBias + hydrology * floodedProximityWeight * 0.5;
                        if (floodedPressure > floodedThreshold)
                        {
                            threshold += Math.Clamp((floodedPressure - floodedThreshold) * 0.25, 0.0, 0.25);
                        }

                        if (hydrology > waterThreshold && y < seaLevel - 2)
                        {
                            threshold += Math.Clamp((hydrology - waterThreshold) * 0.15, 0.0, 0.25);
                        }

                        double depthRatio = (double)y / Math.Max(1, surface);
                        if (depthRatio < lavaThreshold * 0.5)
                        {
                            threshold -= Math.Clamp((lavaThreshold * 0.5 - depthRatio) * 0.1, 0.0, 0.1);
                        }
                        threshold = Math.Clamp(threshold, 0.22, 0.8);

                        if (density > threshold && stability > 0.08)
                        {
                            mask[x, y, z] = true;
                        }
                    }
                }
            }

            SmoothMask(mask, config.StabilitySmoothIterations, config.StabilitySmoothBlend);
            PlugRiparianCaves(mask, hydrologyMask, riverMask, seaLevel);
            AddSupportColumns(mask, hydrologyMask, riverMask, seaLevel, chunkX, chunkZ);
            SealEdges(mask, hydrologyMask, riverMask, config.EdgeSealStrength, chunkX, chunkZ);
            SealWetCeilings(mask, hydrologyMask, flowMask, seaLevel);
            ApplyRiparianStability(mask, hydrologyMask, flowMask, riverMask, seaLevel);
            ApplyAquiferContinuitySeal(mask, hydrologyMask, flowMask, riverMask, seaLevel);
            ApplyHydrologySeamVault(mask, hydrologyMask, flowMask, riverMask, seaLevel);
            ApplyRiverLakeBoundarySeal(mask, hydrologyMask, flowMask, riverMask, seaLevel);
            ApplyFloodedPocketPruning(mask, hydrologyMask, flowMask, riverMask, seaLevel);
            ApplyMoistureChannelDampening(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyKarstRidgeCollapseGuard(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyVadoseBypassSeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyPhreaticSeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyKarstSpringContinuitySeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyEpikarstRechargeSeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyHyporheicVentSeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyFloodplainRoofArchStability(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyTalusButtressStability(mask, hydrologyMask, flowMask, riverMask, erosionRisk, heightMap, seaLevel);
            ApplySubsurfaceShearSeal(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyLithifiedRoofBridge(mask, hydrologyMask, flowMask, riverMask, erosionRisk, heightMap, seaLevel);
            ApplyFloodFeedbackSealBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyFloodBypassVentDampingBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyGroundwaterPressureReliefBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyPerchedAquiferBypassBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplyBankfullVentilationSealBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplySeasonalRechargeCaveSealBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyGroundwaterPerchSealBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, seaLevel);
            ApplySubsurfaceConduitRelayBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            ApplyRiparianRoofButtressBridge(mask, hydrologyMask, flowMask, riverMask, heightMap, chunkX, chunkZ, seaLevel);
            return mask;
        }

        private void ApplyRiparianRoofButtressBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double buttressWeight = Math.Clamp(
                config.CeilingStabilityWeight * 0.36 +
                config.EdgeSealStrength * 0.34 +
                config.GroundwaterConnectivityWeight * 0.30,
                0.0,
                1.25);
            if (buttressWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int reliefRadius = Math.Max(2, config.RiparianPlugDepth + 2);
            double divergenceScale = Math.Max(0.12, config.MoistureFlowClamp * 0.55);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, config.RiparianPlugDepth + 12.0),
                        0.0,
                        1.0);
                    double floodBand = Math.Clamp(
                        1.0 - Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(4.0, config.RiparianPlugDepth + 7.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceScale);
                    double jitter = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.0021 + 29.0,
                        (chunkZ * sizeZ + z) * 0.0021 - 35.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        CreateNoiseSeed(chunkX, chunkZ, x, z, seaLevel, 997)));

                    double wetnessSignal = Math.Clamp(
                        hydro * 0.3 +
                        seamHydro * 0.2 +
                        flow * 0.16 +
                        seamFlow * 0.14 +
                        river * 0.1 +
                        floodBand * 0.1,
                        0.0,
                        1.2);
                    wetnessSignal *= 1.0 - Math.Clamp(
                        slope * config.CeilingStabilityWeight * 0.014 +
                        relief * 0.3 +
                        divergence * 0.24,
                        0.0,
                        0.82);
                    wetnessSignal *= 1.0 + Math.Clamp((jitter - 0.5) * config.CaveVentilationBias * 0.16, -0.14, 0.14);

                    if (wetnessSignal <= 0.22)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 2, sizeY - 2);
                    int top = Math.Min(surface - 1, seaLevel + Math.Max(3, config.RiparianPlugDepth + 2));
                    int bottom = Math.Max(2, top - Math.Max(3, config.RiparianPlugDepth + 1));

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(top - y) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = wetnessSignal * buttressWeight * (0.42 + depthFactor * 0.28);
                        if (sealChance > 0.44)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplySubsurfaceConduitRelayBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double relayWeight = Math.Clamp(
                config.GroundwaterConnectivityWeight * 0.38 +
                config.CaveEntranceFlowDampening * 0.32 +
                config.RiparianCaveGuardWeight * 0.30,
                0.0,
                1.2);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 1));
            int bottom = Math.Max(3, seaLevel - Math.Max(10, config.RiparianPlugDepth + 5));
            int reliefRadius = Math.Max(2, config.RiparianPlugDepth + 1);
            double divergenceScale = Math.Max(0.12, config.MoistureFlowClamp * 0.5);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Clamp(
                        TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius) / Math.Max(1.0, config.RiparianPlugDepth + 12.0),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceScale);
                    double perchedBand = Math.Clamp(
                        (seaLevel + config.RiparianPlugDepth - heightMap[x, z]) / Math.Max(6.0, config.RiparianPlugDepth + 8.0),
                        0.0,
                        1.0);
                    double relaySignal = Math.Clamp(
                        hydro * 0.28 +
                        seamHydro * 0.22 +
                        flow * 0.18 +
                        seamFlow * 0.14 +
                        river * 0.1 +
                        perchedBand * 0.08,
                        0.0,
                        1.25);
                    relaySignal *= 1.0 - Math.Clamp(slope * 0.03 + relief * 0.38 + divergence * 0.22, 0.0, 0.82);
                    if (relaySignal <= 0.24)
                    {
                        continue;
                    }

                    int worldX = chunkX * sizeX + x;
                    int worldZ = chunkZ * sizeZ + z;
                    int surface = Math.Clamp(heightMap[x, z], bottom + 4, sizeY - 2);

                    for (int y = bottom + 1; y <= top - 1; y++)
                    {
                        if (y >= surface - 2 || mask[x, y, z])
                        {
                            continue;
                        }

                        int connected = 0;
                        if (mask[x + 1, y, z]) connected++;
                        if (mask[x - 1, y, z]) connected++;
                        if (mask[x, y + 1, z]) connected++;
                        if (mask[x, y - 1, z]) connected++;
                        if (mask[x, y, z + 1]) connected++;
                        if (mask[x, y, z - 1]) connected++;
                        if (connected <= 0)
                        {
                            continue;
                        }

                        double depthFactor = Math.Clamp((surface - y) / Math.Max(6.0, surface), 0.0, 1.0);
                        double conduitNoise = Math.Abs(SimplexNoise.Generate(
                            (worldX + 13) * 0.016,
                            (worldZ - 17) * 0.016 + y * 0.032,
                            1.0,
                            2,
                            1.0,
                            0.5,
                            worldX * 73856093 ^ worldZ * 19349663 ^ y * 83492791));
                        double carveThreshold = Math.Clamp(
                            0.63 - connected * 0.08 - depthFactor * 0.06 + slope * 0.09 + (1.0 - conduitNoise) * 0.1,
                            0.28,
                            0.78);
                        double conduitPotential = relaySignal * relayWeight * (0.84 + depthFactor * 0.16);
                        if (conduitPotential <= carveThreshold)
                        {
                            continue;
                        }

                        mask[x, y, z] = true;
                    }
                }
            }
        }

        private void ApplyGroundwaterPerchSealBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.GroundwaterConnectivityWeight * 0.36 +
                config.AquiferBarrierWeight * 0.34 +
                config.CaveEntranceFlowDampening * 0.30,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(5, config.RiparianPlugDepth + 3));
            int bottom = Math.Max(2, seaLevel - Math.Max(7, config.RiparianPlugDepth + 4));
            int reliefRadius = Math.Max(2, config.RiparianPlugDepth + 1);
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double perchedBand = Math.Clamp(
                        (seaLevel + config.RiparianPlugDepth - heightMap[x, z]) / Math.Max(6.0, config.RiparianPlugDepth + 8.0),
                        0.0,
                        1.0);
                    double sealSignal = Math.Clamp(
                        hydro * 0.34 +
                        seamHydro * 0.24 +
                        flow * 0.18 +
                        seamFlow * 0.14 +
                        river * 0.10,
                        0.0,
                        1.25);
                    sealSignal *= 1.0 - Math.Clamp(slope * 0.024 + relief / 42.0 + divergence * 0.28, 0.0, 0.82);
                    sealSignal *= 0.7 + perchedBand * 0.3;
                    if (sealSignal <= 0.2)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], bottom + 2, sizeY - 2);
                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z] || y >= surface)
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double lateralFactor = lateralOpen / 4.0;
                        double sealChance = bridgeWeight *
                            sealSignal *
                            (0.38 + lateralFactor * 0.24 + perchedBand * 0.22 + Math.Clamp(roofThickness / 8.0, 0.0, 0.2));
                        if (sealChance > 0.47 || (sealChance > 0.33 && perchedBand > 0.4 && river > 0.28))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyBankfullVentilationSealBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.CaveVentilationBias * 0.38 +
                config.GroundwaterConnectivityWeight * 0.34 +
                config.MoistureRetentionWeight * 0.28,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(6, config.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(8, config.RiparianPlugDepth + 4));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double wetCoupling = Math.Clamp(
                        hydro * 0.36 + seamHydro * 0.24 + flow * 0.22 + seamFlow * 0.12 + river * 0.06,
                        0.0,
                        1.2);
                    if (wetCoupling <= 0.28)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], bottom + 2, sizeY - 2);
                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z] || y >= surface)
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        int roofThickness = surface - y;
                        if (roofThickness <= 2)
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double ventilationPotential = Math.Clamp(
                            (1.0 - hydro) * (1.0 - flow) * (1.0 - Math.Clamp(slope * 0.05, 0.0, 0.75)),
                            0.0,
                            1.0);
                        double bankfullPressure = wetCoupling * (0.42 + depthFactor * 0.25 + river * 0.18);
                        bankfullPressure *= 1.0 - Math.Clamp(divergence * 0.45 + slope * config.CeilingStabilityWeight * 0.02, 0.0, 0.8);
                        bankfullPressure *= bridgeWeight;
                        bankfullPressure -= ventilationPotential * config.CaveVentilationBias * 0.22;
                        bankfullPressure += Math.Clamp((2 - lateralOpen) * 0.08, 0.0, 0.25);

                        if (bankfullPressure > 0.44 || (bankfullPressure > 0.34 && lateralOpen <= 1))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplySeasonalRechargeCaveSealBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int chunkX,
            int chunkZ,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.MoistureRetentionWeight * 0.4 +
                config.GroundwaterConnectivityWeight * 0.33 +
                config.CaveEntranceFlowDampening * 0.27,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(2, config.RiparianPlugDepth + 1);
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);
            int topBand = Math.Min(sizeY - 2, seaLevel + Math.Max(6, config.RiparianPlugDepth + 3));
            int bottomBand = Math.Max(2, seaLevel - Math.Max(8, config.RiparianPlugDepth + 3));

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flowNode = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    int seed = ComputeSeasonalSeed(chunkX, chunkZ, x, z);
                    double seasonalPulse = Math.Abs(SimplexNoise.Generate(
                        (chunkX * sizeX + x) * 0.018 + 41.0,
                        (chunkZ * sizeZ + z) * 0.018 - 19.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        seed));
                    double recharge = Math.Clamp(
                        (hydro + seamHydro + flowNode + seamFlow) * 0.25 +
                        river * 0.2 +
                        seasonalPulse * 0.25,
                        0.0,
                        1.4);
                    if (recharge <= 0.32)
                    {
                        continue;
                    }

                    double rechargeGuard = 1.0 - Math.Clamp(divergence * 0.35 + slope * config.CeilingStabilityWeight * 0.01, 0.0, 0.75);
                    int surface = Math.Clamp(heightMap[x, z], bottomBand + 2, sizeY - 2);
                    for (int y = topBand; y >= bottomBand; y--)
                    {
                        if (y >= surface || !mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        if (lateralOpen <= 1)
                        {
                            continue;
                        }

                        double nearSurface = 1.0 - Math.Clamp((surface - y) / 8.0, 0.0, 1.0);
                        double sealChance = recharge * bridgeWeight * rechargeGuard * (0.16 + nearSurface * 0.1 + edgeBand * 0.08);
                        if (sealChance <= 0.06)
                        {
                            continue;
                        }

                        mask[x, y, z] = false;
                    }
                }
            }
        }

        private static int ComputeSeasonalSeed(int chunkX, int chunkZ, int localX, int localZ)
        {
            unchecked
            {
                int hash = 0x43A9B17C;
                hash = (hash * 397) ^ chunkX;
                hash = (hash * 397) ^ chunkZ;
                hash = (hash * 397) ^ localX;
                hash = (hash * 397) ^ localZ;
                return hash;
            }
        }

        private void ApplyFloodplainRoofArchStability(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double archWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.CaveEntranceFlowDampening * 0.34 +
                config.RiparianCaveGuardWeight * 0.28,
                0.0,
                1.0);
            if (archWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int topClamp = Math.Min(sizeY - 2, seaLevel + Math.Max(6, config.RiparianPlugDepth + 4));
            int bottomClamp = Math.Max(2, seaLevel - Math.Max(4, config.RiparianPlugDepth + 2));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = hydrologyMask[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = flowMask[x, z];
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double coupling = Math.Clamp(
                        hydro * 0.35 +
                        seamHydro * 0.2 +
                        flow * 0.2 +
                        seamFlow * 0.1 +
                        river * 0.15,
                        0.0,
                        1.35);
                    if (coupling <= 0.35)
                    {
                        continue;
                    }

                    int localSurface = Math.Clamp(heightMap[x, z], bottomClamp + 2, sizeY - 2);
                    int top = Math.Min(topClamp, localSurface - 1);
                    int bottom = Math.Max(bottomClamp, top - Math.Max(4, config.RiparianPlugDepth + 2));
                    int depthSpan = Math.Max(1, top - bottom);

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = localSurface - y;
                        if (roofThickness <= 2)
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((top - y) / (double)depthSpan, 0.0, 1.0);
                        double archPressure = coupling * (0.25 + archWeight * 0.4 + depthFactor * 0.2);
                        archPressure *= 1.0 - Math.Clamp(slope * 0.04 + divergence * 0.45, 0.0, 0.85);
                        if (roofThickness <= config.RiparianPlugDepth + 1)
                        {
                            archPressure *= 1.1;
                        }

                        if (archPressure > 0.42)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyPhreaticSeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(1, Math.Min(sizeX, sizeZ) / 5);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(6, config.RiparianPlugDepth + 5));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);
            double sealWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.RiparianCaveGuardWeight * 0.34 +
                config.CaveEntranceFlowDampening * 0.28,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, config.RiparianPlugDepth + 2));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double wetness = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + flow * 0.18 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);
                    if (wetness < 0.32)
                    {
                        continue;
                    }

                    double continuity = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flow);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double phreaticRisk = wetness * (0.42 + config.MoistureRetentionWeight * 0.28);
                        phreaticRisk += continuity * config.EdgeSealStrength * 0.24;
                        phreaticRisk += divergence * config.FlowStabilityWeight * 0.2;
                        phreaticRisk += slope * config.CeilingStabilityWeight * 0.015;
                        phreaticRisk += relief * config.RiverSuppressionWeight * 0.012;
                        phreaticRisk += edgeBand * config.EdgeSealStrength * 0.1;
                        phreaticRisk += Math.Clamp((2 - lateralOpen) * 0.09, 0.0, 0.3);
                        phreaticRisk *= sealWeight * (0.75 + depthFactor * 0.25);
                        phreaticRisk = Math.Clamp(phreaticRisk, 0.0, 1.0);

                        if (phreaticRisk > 0.58 || (phreaticRisk > 0.42 && lateralOpen <= 1))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyKarstSpringContinuitySeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(1, Math.Min(sizeX, sizeZ) / 4);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(5, config.RiparianPlugDepth + 3));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);
            double sealWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.MoistureRetentionWeight * 0.34 +
                config.CaveEntranceFlowDampening * 0.28,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double springPotential = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + flow * 0.18 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);

                    if (springPotential < 0.28)
                    {
                        continue;
                    }

                    double continuity = 1.0 - Math.Clamp(
                        hydroGradient * config.EdgeSealStrength * 0.35 +
                        flowGradient * config.EdgeSealStrength * 0.25 +
                        divergence * config.FlowStabilityWeight * 0.25,
                        0.0,
                        0.85);
                    double reliefBrake = 1.0 - Math.Clamp(
                        slope * config.CeilingStabilityWeight * 0.02 +
                        relief * config.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.55);

                    double springSeal = springPotential * sealWeight * (0.62 + edgeBand * 0.28) * continuity * reliefBrake;
                    if (springSeal < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = springSeal * (0.55 + depthFactor * 0.45);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.08, 0.0, 0.25);

                        if (sealChance > 0.56 || (sealChance > 0.4 && springPotential > 0.45))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyEpikarstRechargeSeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(2, Math.Min(sizeX, sizeZ) / 3);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(5, config.RiparianPlugDepth + 5));
            int bottom = Math.Max(2, seaLevel - Math.Max(4, config.RiparianPlugDepth + 2));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);
            double rechargeWeight = Math.Clamp(
                config.MoistureRetentionWeight * 0.36 +
                config.AquiferBarrierWeight * 0.34 +
                config.CaveEntranceFlowDampening * 0.30,
                0.0,
                1.0);
            if (rechargeWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double recharge = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + flow * 0.16 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    if (recharge < 0.3)
                    {
                        continue;
                    }

                    double continuityBrake = 1.0 - Math.Clamp(
                        hydroGradient * config.EdgeSealStrength * 0.34 +
                        flowGradient * config.EdgeSealStrength * 0.24 +
                        divergence * config.FlowStabilityWeight * 0.22,
                        0.0,
                        0.8);
                    double rechargeRisk = recharge * rechargeWeight * (0.58 + edgeBand * 0.3);
                    rechargeRisk *= continuityBrake;
                    rechargeRisk *= 1.0 - Math.Clamp(
                        slope * config.CeilingStabilityWeight * 0.02 +
                        relief * config.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.58);

                    if (rechargeRisk < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = rechargeRisk * (0.52 + depthFactor * 0.48);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.07, 0.0, 0.22);

                        if (sealChance > 0.54 || (sealChance > 0.39 && recharge > 0.46))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyHyporheicVentSeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(2, Math.Min(sizeX, sizeZ) / 4);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(4, config.RiparianPlugDepth + 4));
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);
            double sealWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.CaveEntranceFlowDampening * 0.34 +
                config.RiparianCaveGuardWeight * 0.28,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double hyporheicPotential = Math.Clamp(
                        hydro * 0.32 + seamHydro * 0.24 + flow * 0.2 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);
                    if (hyporheicPotential < 0.3)
                    {
                        continue;
                    }

                    double continuity = 1.0 - Math.Clamp(
                        hydroGradient * config.EdgeSealStrength * 0.32 +
                        flowGradient * config.EdgeSealStrength * 0.24 +
                        divergence * config.FlowStabilityWeight * 0.24,
                        0.0,
                        0.82);
                    double ventRisk = hyporheicPotential * sealWeight * (0.56 + edgeBand * 0.32) * continuity;
                    ventRisk *= 1.0 - Math.Clamp(
                        slope * config.CeilingStabilityWeight * 0.018 +
                        relief * config.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.55);
                    if (ventRisk < 0.18)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = ventRisk * (0.5 + depthFactor * 0.5);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.08, 0.0, 0.24);

                        if (sealChance > 0.55 || (sealChance > 0.4 && hyporheicPotential > 0.46))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyKarstRidgeCollapseGuard(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            double guardWeight = Math.Clamp(
                config.CaveEntranceFlowDampening * 0.35 +
                config.CeilingStabilityWeight * 0.35 +
                config.AquiferBarrierWeight * 0.30,
                0.0,
                1.0);
            if (guardWeight <= 0.01)
            {
                return;
            }

            int edgeRadius = Math.Max(1, Math.Min(sizeX, sizeZ) / 5);
            int ridgeWindow = Math.Max(2, config.RiparianPlugDepth + 3);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 2, sizeY - 2);
                    int top = Math.Min(surface - 1, seaLevel + ridgeWindow + 4);
                    int bottom = Math.Max(1, top - ridgeWindow);
                    if (top <= bottom)
                    {
                        continue;
                    }

                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double seamGradient = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flow);
                    double ridgeRisk = Math.Clamp(
                        slope * config.CeilingStabilityWeight * 0.03 +
                        relief * 0.015 +
                        seamGradient * config.EdgeSealStrength * 0.25 +
                        river * config.RiverSuppressionWeight * 0.35 +
                        hydro * config.MoistureRetentionWeight * 0.25 +
                        flow * config.FlowStabilityWeight * 0.25,
                        0.0,
                        1.4);

                    ridgeRisk *= 0.65 + edgeFalloff * 0.35;
                    if (ridgeRisk < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = ridgeRisk * guardWeight * (0.45 + depthFactor * 0.55);
                        if (sealChance > 0.58 || (sealChance > 0.35 && relief > 3.5))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyMoistureChannelDampening(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(3, config.RiparianPlugDepth + 2));
            int bottom = Math.Max(1, seaLevel - Math.Max(4, config.RiparianPlugDepth + 2));
            double dampWeight = Math.Clamp(
                config.CaveEntranceFlowDampening * 0.4 + config.AquiferBarrierWeight * 0.35 + config.RiparianCaveGuardWeight * 0.25,
                0.0,
                1.0);
            if (dampWeight <= 0.0)
            {
                return;
            }

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double continuity = Math.Clamp(Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flow), 0.0, 1.0);
                    double channel = Math.Clamp(
                        hydro * 0.35 + seamHydro * 0.18 + flow * 0.2 + seamFlow * 0.17 + river * 0.1,
                        0.0,
                        1.2);
                    double dampening = channel * dampWeight;
                    dampening += continuity * config.EdgeSealStrength * 0.25;
                    dampening += slope * config.CeilingStabilityWeight * 0.015;
                    dampening = Math.Clamp(dampening, 0.0, 0.92);

                    if (dampening < 0.3)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = dampening * (0.48 + depthFactor * 0.42);
                        if (sealChance > 0.57 || (sealChance > 0.41 && channel > 0.55))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyVadoseBypassSeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 3));
            int bottom = Math.Max(2, seaLevel - Math.Max(6, config.RiparianPlugDepth + 4));
            double sealWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.4 +
                config.CaveEntranceFlowDampening * 0.35 +
                config.EdgeSealStrength * 0.25,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double continuity = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flow);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double wetness = Math.Clamp(hydro * 0.38 + seamHydro * 0.22 + flow * 0.2 + seamFlow * 0.1 + river * 0.1, 0.0, 1.2);
                    if (wetness < 0.3)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double bypassRisk = wetness * (0.42 + config.MoistureRetentionWeight * 0.25);
                        bypassRisk += continuity * config.EdgeSealStrength * 0.2;
                        bypassRisk += divergence * config.FlowStabilityWeight * 0.25;
                        bypassRisk += slope * config.CeilingStabilityWeight * 0.015;
                        bypassRisk += Math.Clamp((2 - lateralOpen) * 0.1, 0.0, 0.3);
                        bypassRisk = Math.Clamp(bypassRisk * sealWeight, 0.0, 1.0);

                        if (bypassRisk > 0.56 || (bypassRisk > 0.4 && lateralOpen <= 1))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyFloodedPocketPruning(bool[,,] mask, float[,] hydrologyMask, float[,] flowMask, float[,]? riverMask, int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, Math.Max(3, seaLevel + 5));
            int bottom = Math.Max(1, seaLevel - Math.Max(5, config.RiparianPlugDepth + 3));

            double barrierWeight = Math.Clamp(config.AquiferBarrierWeight, 0.0, 1.0);
            double guardWeight = Math.Clamp(config.RiparianCaveGuardWeight, 0.0, 1.0);
            double moistureWeight = Math.Clamp(config.MoistureRetentionWeight, 0.0, 1.0);
            double edgeSeal = Math.Clamp(config.EdgeSealStrength, 0.0, 1.0);
            double flowClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double continuity = Math.Clamp((hydroGradient + flowGradient) / flowClamp, 0.0, 1.0);

                    double wetness = Math.Clamp(
                        hydro * 0.4 + seamHydro * 0.2 + flow * 0.2 + seamFlow * 0.1 + river * 0.1,
                        0.0,
                        1.2);
                    double pruningWeight = wetness * (barrierWeight * 0.4 + guardWeight * 0.3 + moistureWeight * 0.2);
                    pruningWeight += continuity * edgeSeal * 0.25;
                    pruningWeight = Math.Clamp(pruningWeight, 0.0, 0.95);
                    if (pruningWeight < 0.3)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int localAirNeighbors = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = Math.Clamp(x + dx, 0, sizeX - 1);
                                int nz = Math.Clamp(z + dz, 0, sizeZ - 1);
                                if (mask[nx, y, nz])
                                {
                                    localAirNeighbors++;
                                }
                            }
                        }

                        double pocketBias = 1.0 - Math.Clamp(localAirNeighbors / 8.0, 0.0, 1.0);
                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double pruneChance = pruningWeight * (0.52 + pocketBias * 0.28 + depthFactor * 0.2);
                        if (pruneChance > 0.57 || (pruneChance > 0.4 && wetness > 0.48))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyRiverLakeBoundarySeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int seaLevel)
        {
            if (riverMask == null)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, Math.Max(3, seaLevel + 2));
            int bottom = Math.Max(1, seaLevel - Math.Max(3, config.RiparianPlugDepth));

            double riparianGuard = Math.Clamp(config.RiparianCaveGuardWeight, 0.0, 1.0);
            double aquiferBarrier = Math.Clamp(config.AquiferBarrierWeight, 0.0, 1.0);
            double sealStrength = Math.Clamp(config.EdgeSealStrength, 0.0, 1.0);
            double moistureRetention = Math.Clamp(config.MoistureRetentionWeight, 0.0, 1.0);
            double flowClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double river = TerrainMaskUtility.Clamp01(riverMask[x, z]);
                    if (hydro < 0.2 && river < 0.2)
                    {
                        continue;
                    }

                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double seamRiver = TerrainMaskUtility.SampleInterior(riverMask, x, z);

                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double riverGradient = Math.Abs(seamRiver - river);
                    double seamWetness = Math.Clamp(
                        hydro * 0.35 + seamHydro * 0.2 + flow * 0.15 + seamFlow * 0.15 + river * 0.15,
                        0.0,
                        1.15);
                    double seamContinuity = Math.Clamp((hydroGradient + flowGradient + riverGradient) / flowClamp, 0.0, 1.0);
                    double seal = seamWetness * (riparianGuard * 0.4 + aquiferBarrier * 0.35 + moistureRetention * 0.15);
                    seal += seamContinuity * sealStrength * 0.25;
                    seal = Math.Clamp(seal, 0.0, 0.96);

                    if (seal < 0.28)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = seal * (0.55 + depthFactor * 0.45);
                        if (sealChance > 0.57 || (sealChance > 0.39 && seamWetness > 0.4))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyFloodBypassVentDampingBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double dampingWeight = Math.Clamp(
                config.GroundwaterConnectivityWeight * 0.36 +
                config.CaveVentilationBias * 0.34 +
                config.CaveEntranceFlowDampening * 0.30,
                0.0,
                1.0);
            if (dampingWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int lowerBand = Math.Max(2, seaLevel - Math.Max(12, config.RiparianPlugDepth + 6));
            int upperBand = Math.Min(sizeY - 2, seaLevel + 2);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(2, config.RiparianPlugDepth));
                    double bypassSignal = Math.Clamp(
                        hydro * 0.34 +
                        seamHydro * 0.2 +
                        flow * 0.22 +
                        seamFlow * 0.14 +
                        river * 0.10,
                        0.0,
                        1.2);
                    if (bypassSignal <= 0.24)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(upperBand, surface - 1);
                    if (top <= lowerBand)
                    {
                        continue;
                    }

                    double damping = 1.0 - Math.Clamp(slope * 0.02 + relief / 42.0, 0.0, 0.7);
                    for (int y = lowerBand; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthRatio = 1.0 - Math.Clamp((double)(y - lowerBand) / Math.Max(1.0, top - lowerBand), 0.0, 1.0);
                        double ventBias = Math.Clamp(config.CaveVentilationBias * (0.35 + depthRatio * 0.3), 0.0, 0.75);
                        double sealChance = bypassSignal * dampingWeight * (0.28 + depthRatio * 0.38 + ventBias * 0.24) * damping;
                        if (sealChance > 0.54 || (sealChance > 0.42 && y >= seaLevel - 3))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyGroundwaterPressureReliefBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double reliefWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.RiparianCaveGuardWeight * 0.34 +
                config.CaveVentilationBias * 0.28,
                0.0,
                1.0);
            if (reliefWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int upperBand = Math.Min(sizeY - 2, seaLevel + Math.Max(4, config.RiparianPlugDepth + 2));
            int lowerBand = Math.Max(2, seaLevel - Math.Max(12, config.RiparianPlugDepth + 6));

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, Math.Max(2, config.RiparianPlugDepth));
                    double pressure = Math.Clamp(
                        hydro * 0.34 +
                        seamHydro * 0.2 +
                        flow * 0.22 +
                        seamFlow * 0.14 +
                        river * 0.1,
                        0.0,
                        1.2);
                    if (pressure <= 0.24)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(upperBand, surface - 1);
                    if (top <= lowerBand)
                    {
                        continue;
                    }

                    double terrainDamping = 1.0 - Math.Clamp(slope * 0.024 + relief / 40.0, 0.0, 0.78);
                    double ventilation = Math.Clamp(
                        (1.0 - hydro) * 0.4 +
                        (1.0 - flow) * 0.3 +
                        (1.0 - river) * 0.3,
                        0.0,
                        1.0);

                    for (int y = top; y >= lowerBand; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        double shallowFactor = 1.0 - Math.Clamp((double)(y - lowerBand) / Math.Max(1.0, top - lowerBand), 0.0, 1.0);
                        double sealChance = pressure * reliefWeight * (0.28 + shallowFactor * 0.42) * terrainDamping;
                        sealChance *= 1.0 - Math.Clamp(ventilation * 0.28, 0.0, 0.28);
                        if (roofThickness <= 3)
                        {
                            sealChance *= 1.0 + Math.Clamp((3 - roofThickness) * 0.12, 0.0, 0.24);
                        }

                        if (sealChance > 0.52 || (sealChance > 0.4 && y >= seaLevel - 2))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private double ComputeColumnStability(int surface, float hydrology, float riverPressure, float flowPressure, double edgeFactor)
        {
            double waterBias = 1.0 - Math.Clamp(hydrology * config.HydrologyStabilityWeight, 0.0, 0.75);
            double riverBias = 1.0 - Math.Clamp(riverPressure * config.RiverSuppressionWeight, 0.0, 0.9);
            double flowBias = 1.0 - Math.Clamp(flowPressure * config.FlowStabilityWeight, 0.0, 0.85);
            double ceilingBias = 1.0 - Math.Clamp((surface / 128.0) * config.CeilingStabilityWeight, 0.0, 0.35);
            double edgeBias = 1.0 - Math.Clamp(edgeFactor * config.EdgeSealStrength, 0.0, 0.45);
            return Math.Clamp(waterBias * riverBias * flowBias * (1.0 - ceilingBias * 0.35) * edgeBias, 0.05, 1.25);
        }

        private void ApplyFloodFeedbackSealBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double feedbackWeight = Math.Clamp(
                config.CaveEntranceFlowDampening * 0.4 +
                config.MoistureRetentionWeight * 0.35 +
                config.AquiferBarrierWeight * 0.25,
                0.0,
                1.0);
            if (feedbackWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int lowerBand = Math.Max(1, seaLevel - Math.Max(10, config.RiparianPlugDepth + 4));
            int upperBand = Math.Min(sizeY - 2, seaLevel + 1);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double seamRiver = riverMask != null ? TerrainMaskUtility.SampleInterior(riverMask, x, z) : river;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);

                    double feedback = Math.Clamp(
                        hydro * 0.32 +
                        seamHydro * 0.24 +
                        flow * 0.2 +
                        seamFlow * 0.12 +
                        river * 0.08 +
                        seamRiver * 0.04,
                        0.0,
                        1.2);
                    if (feedback <= 0.28)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 2, sizeY - 2);
                    int top = Math.Min(upperBand, surface - 1);
                    if (top <= lowerBand)
                    {
                        continue;
                    }

                    for (int y = lowerBand; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthRatio = 1.0 - Math.Clamp((double)(y - lowerBand) / Math.Max(1.0, top - lowerBand), 0.0, 1.0);
                        double slopeDamping = 1.0 - Math.Clamp(slope * 0.022, 0.0, 0.35);
                        double sealChance = feedback * feedbackWeight * (0.3 + depthRatio * 0.45) * slopeDamping;
                        if (sealChance > 0.58 || (sealChance > 0.44 && y >= seaLevel - 2))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private static double ComputeEdgeFalloff(int x, int z, int chunkSize)
        {
            int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
            int maxRadius = Math.Max(1, chunkSize / 2);
            return 1.0 - Math.Clamp(edgeDistance / (double)maxRadius, 0.0, 1.0);
        }

        private void SmoothMask(bool[,,] mask, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);

            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = new bool[sizeX, sizeY, sizeZ];
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        for (int y = 1; y < sizeY - 1; y++)
                        {
                            int neighbours = 0;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        if (dx == 0 && dy == 0 && dz == 0)
                                        {
                                            continue;
                                        }

                                        int nx = x + dx;
                                        int ny = y + dy;
                                        int nz = z + dz;
                                        if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ || ny < 0 || ny >= sizeY)
                                        {
                                            continue;
                                        }

                                        if (mask[nx, ny, nz])
                                        {
                                            neighbours++;
                                        }
                                    }
                                }
                            }

                            bool carve = mask[x, y, z];
                            if (neighbours >= 13)
                            {
                                buffer[x, y, z] = true;
                            }
                            else if (neighbours <= 3)
                            {
                                buffer[x, y, z] = false;
                            }
                            else
                            {
                                buffer[x, y, z] = blend > 0 ? neighbours >= 9 : carve;
                            }
                        }
                    }
                }

                Array.Copy(buffer, mask, buffer.Length);
            }
        }

        private void AddSupportColumns(bool[,,] mask, float[,] hydrologyMask, float[,]? riverMask, int seaLevel, int chunkX, int chunkZ)
        {
            double chance = Math.Clamp(config.SupportPillarChance * config.SupportDensity, 0.0, 1.0);
            if (chance <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double gradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrologyMask, x, z) - hydrology);
                    double pillarChance = chance * (1.0 + hydrology * config.SupportHydrationBias + river * config.SupportFlowBias + gradient * 0.15);
                    double pillarRoll = CreateDeterministicUnit(chunkX, chunkZ, x, z, seaLevel, 353);
                    if (pillarRoll > pillarChance)
                    {
                        continue;
                    }

                    int baseY = Math.Max(1, seaLevel - 6);
                    int height = CreateDeterministicRange(chunkX, chunkZ, x, z, seaLevel, 359, 2, 6);
                    for (int y = baseY; y < Math.Min(sizeY - 1, baseY + height); y++)
                    {
                        mask[x, y, z] = false;
                    }
                }
            }
        }

        private void PlugRiparianCaves(bool[,,] mask, float[,] hydrologyMask, float[,]? riverMask, int seaLevel)
        {
            if (config.RiparianPlugDepth <= 0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int plugTop = Math.Min(sizeY - 2, Math.Max(2, seaLevel));
            int plugBottom = Math.Max(1, plugTop - config.RiparianPlugDepth);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    float wetness = Math.Max(hydrology, river);
                    if (wetness < 0.35f)
                    {
                        continue;
                    }

                    for (int y = plugBottom; y <= plugTop; y++)
                    {
                        mask[x, y, z] = false;
                    }
                }
            }
        }

        private void SealEdges(bool[,,] mask, float[,] hydrologyMask, float[,]? riverMask, double strength, int chunkX, int chunkZ)
        {
            strength = Math.Clamp(strength, 0.0, 1.0);
            if (strength <= 0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (x != 0 && z != 0 && x != sizeX - 1 && z != sizeZ - 1)
                    {
                        continue;
                    }

                    for (int y = 1; y < sizeY - 1; y++)
                    {
                        float hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                        float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                        double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                        double gradient = Math.Abs(neighbourHydro - hydro);
                        double sealingBias = 0.5 + hydro * 0.35 + river * 0.25 + gradient * 0.25;
                        double sealChance = strength * Math.Clamp(sealingBias, 0.0, 1.5);
                        double sealRoll = CreateDeterministicUnit(chunkX, chunkZ, x, z, y, 367);
                        if (mask[x, y, z] && sealRoll < sealChance)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyRiparianStability(bool[,,] mask, float[,] hydrologyMask, float[,] flowMask, float[,]? riverMask, int seaLevel)
        {
            double edgeSeal = Math.Clamp(config.EdgeSealStrength, 0.0, 1.0);
            double riparianGuard = Math.Clamp(config.RiparianCaveGuardWeight, 0.0, 1.0);
            if (edgeSeal <= 0.0 && riparianGuard <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(1, Math.Min(sizeX, sizeZ) / 4);
            int clampTop = Math.Min(sizeY - 2, Math.Max(2, seaLevel));
            int clampBottom = Math.Max(1, clampTop - Math.Max(2, config.RiparianPlugDepth));

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double falloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    if (falloff <= 0.01)
                    {
                        continue;
                    }

                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrology);
                    double guard = Math.Clamp(
                        (hydrology + river) * riparianGuard * 0.65 +
                        hydrologyGradient * edgeSeal * 0.35 +
                        flow * config.MoistureRetentionWeight * 0.25,
                        0.0,
                        1.0);
                    guard *= falloff;
                    if (guard < 0.15)
                    {
                        continue;
                    }

                    for (int y = clampBottom; y <= clampTop; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - clampBottom) / Math.Max(1.0, clampTop - clampBottom), 0.0, 1.0);
                        double sealChance = guard * depthFactor;
                        if (sealChance > 0.6 || (sealChance > 0.3 && flow > 0.25f))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void SealWetCeilings(bool[,,] mask, float[,] hydrologyMask, float[,] flowMask, int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int clampTop = Math.Min(seaLevel, sizeY - 2);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float wetness = Math.Max(hydrologyMask[x, z], flowMask[x, z]);
                    if (wetness < 0.4f)
                    {
                        continue;
                    }

                    int startY = Math.Max(1, clampTop - 2);
                    for (int y = startY; y <= clampTop; y++)
                    {
                        if (mask[x, y, z])
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyAquiferContinuitySeal(bool[,,] mask, float[,] hydrologyMask, float[,] flowMask, float[,]? riverMask, int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, Math.Max(2, seaLevel + 2));
            int bottom = Math.Max(1, seaLevel - Math.Max(3, config.RiparianPlugDepth + 1));
            double guardWeight = Math.Clamp(config.RiparianCaveGuardWeight, 0.0, 1.0);
            double edgeSeal = Math.Clamp(config.EdgeSealStrength, 0.0, 1.0);
            double moistureRetention = Math.Clamp(config.MoistureRetentionWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    float seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    float seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double wetness = Math.Max(hydro, Math.Max(flow, river));
                    double continuity = Math.Clamp((hydroGradient + flowGradient) * edgeSeal * 0.35, 0.0, 0.65);
                    double seal = Math.Clamp(
                        wetness * (guardWeight * 0.55 + moistureRetention * 0.25) +
                        continuity,
                        0.0,
                        0.9);

                    if (seal < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = seal * (0.55 + depthFactor * 0.45);
                        if (sealChance > 0.58 || (sealChance > 0.36 && wetness > 0.4))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyTalusButtressStability(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            float[,] erosionRisk,
            int[,] heightMap,
            int seaLevel)
        {
            double buttressWeight = Math.Clamp(
                config.CeilingStabilityWeight * 0.42 +
                config.SupportDensity * 0.34 +
                config.RiparianCaveGuardWeight * 0.24,
                0.0,
                1.0);
            if (buttressWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int topClamp = Math.Min(sizeY - 2, Math.Max(5, seaLevel + Math.Max(4, config.RiparianPlugDepth + 2)));
            int maxDepth = Math.Max(3, config.RiparianPlugDepth + 2);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    if (slope < 5.5 && erosion < 0.28)
                    {
                        continue;
                    }

                    double hydro = hydrologyMask[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = flowMask[x, z];
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double seamRiver = riverMask != null ? TerrainMaskUtility.SampleInterior(riverMask, x, z) : river;
                    double moisture = Math.Clamp(
                        hydro * 0.28 + seamHydro * 0.2 + flow * 0.2 + seamFlow * 0.14 + river * 0.1 + seamRiver * 0.08,
                        0.0,
                        1.2);
                    double buttressPressure = Math.Clamp(
                        (slope / 18.0) * 0.42 +
                        erosion * 0.32 +
                        moisture * 0.26,
                        0.0,
                        1.25);
                    buttressPressure *= buttressWeight;
                    if (buttressPressure <= 0.22)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(topClamp, surface - 1);
                    int bottom = Math.Max(1, top - maxDepth);
                    int depthSpan = Math.Max(1, top - bottom);

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((top - y) / (double)depthSpan, 0.0, 1.0);
                        double sealChance = buttressPressure * (0.4 + depthFactor * 0.35 + roofThickness / (double)(maxDepth + 2) * 0.2);
                        if (sealChance > 0.46 || (sealChance > 0.32 && moisture > 0.42 && slope > 7.0))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplySubsurfaceShearSeal(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double shearWeight = Math.Clamp(
                config.CeilingStabilityWeight * 0.34 +
                config.AquiferBarrierWeight * 0.34 +
                config.EdgeSealStrength * 0.32,
                0.0,
                1.0);
            if (shearWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int topClamp = Math.Min(sizeY - 2, Math.Max(6, seaLevel + Math.Max(5, config.RiparianPlugDepth + 3)));
            int maxDepth = Math.Max(4, config.RiparianPlugDepth + 3);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, 2);
                    if (slope < 4.0 && relief < 4.0)
                    {
                        continue;
                    }

                    double hydro = hydrologyMask[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = flowMask[x, z];
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double shearPressure = Math.Clamp(
                        (slope / 18.0) * 0.36 +
                        (relief / 28.0) * 0.26 +
                        hydro * 0.18 +
                        seamFlow * 0.12 +
                        river * 0.08,
                        0.0,
                        1.25);
                    shearPressure *= shearWeight;
                    if (shearPressure <= 0.2)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(topClamp, surface - 1);
                    int bottom = Math.Max(1, top - maxDepth);
                    int depthSpan = Math.Max(1, top - bottom);

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((top - y) / (double)depthSpan, 0.0, 1.0);
                        double sealChance = shearPressure *
                            (0.38 + depthFactor * 0.32 + roofThickness / (double)(maxDepth + 2) * 0.2);
                        if (sealChance > 0.44 || (sealChance > 0.3 && hydro > 0.42 && flow > 0.35))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyPerchedAquiferBypassBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.AquiferBarrierWeight * 0.38 +
                config.GroundwaterConnectivityWeight * 0.34 +
                config.CaveEntranceFlowDampening * 0.28,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int topClamp = Math.Min(sizeY - 2, Math.Max(6, seaLevel + Math.Max(5, config.RiparianPlugDepth + 3)));
            int bottomClamp = Math.Max(2, seaLevel - Math.Max(6, config.RiparianPlugDepth + 4));
            int reliefRadius = Math.Max(2, config.RiparianPlugDepth + 1);
            double divergenceClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = hydrologyMask[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = flowMask[x, z];
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double continuity = Math.Abs(hydro - seamHydro) + Math.Abs(flow - seamFlow);
                    double perchedRisk = Math.Clamp(
                        hydro * 0.32 +
                        seamHydro * 0.22 +
                        flow * 0.18 +
                        seamFlow * 0.12 +
                        river * 0.16,
                        0.0,
                        1.4);
                    perchedRisk *= 1.0 - Math.Clamp(slope * 0.02 + relief / 42.0 + divergence * 0.32 + continuity * 0.16, 0.0, 0.88);
                    if (perchedRisk <= 0.2)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(topClamp, surface - 1);
                    int bottom = Math.Max(bottomClamp, top - Math.Max(3, config.RiparianPlugDepth + 3));
                    if (top <= bottom)
                    {
                        continue;
                    }

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;
                        bool underAquiferBand = y >= seaLevel - Math.Max(2, config.RiparianPlugDepth) && y <= seaLevel + 2;
                        double lateralFactor = lateralOpen / 4.0;
                        double sealChance = bridgeWeight *
                            perchedRisk *
                            (0.42 + lateralFactor * 0.24 + (underAquiferBand ? 0.18 : 0.0) + Math.Clamp(roofThickness / 8.0, 0.0, 0.2));

                        if (sealChance > 0.46 || (sealChance > 0.32 && underAquiferBand && river > 0.28))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyLithifiedRoofBridge(
            bool[,,] mask,
            float[,] hydrologyMask,
            float[,] flowMask,
            float[,]? riverMask,
            float[,] erosionRisk,
            int[,] heightMap,
            int seaLevel)
        {
            double bridgeWeight = Math.Clamp(
                config.CeilingStabilityWeight * 0.4 +
                config.AquiferBarrierWeight * 0.34 +
                config.RiverSuppressionWeight * 0.26,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int topClamp = Math.Min(sizeY - 2, Math.Max(6, seaLevel + Math.Max(4, config.RiparianPlugDepth + 2)));
            int maxDepth = Math.Max(4, config.RiparianPlugDepth + 4);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = hydrologyMask[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double flow = flowMask[x, z];
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double river = riverMask != null ? riverMask[x, z] : 0.0;
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = TerrainMaskUtility.ComputeLocalRelief(heightMap, x, z, 2);
                    double wetness = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.22 + flow * 0.2 + seamFlow * 0.16 + river * 0.12,
                        0.0,
                        1.25);
                    if (wetness < 0.28 && erosion < 0.22 && slope < 5.5)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], 3, sizeY - 2);
                    int top = Math.Min(topClamp, surface - 1);
                    int bottom = Math.Max(1, top - maxDepth);
                    int depthSpan = Math.Max(1, top - bottom);

                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        int openNeighbours = 0;
                        if (mask[x - 1, y, z]) openNeighbours++;
                        if (mask[x + 1, y, z]) openNeighbours++;
                        if (mask[x, y, z - 1]) openNeighbours++;
                        if (mask[x, y, z + 1]) openNeighbours++;

                        double continuity = openNeighbours / 4.0;
                        double thinRoofRisk = roofThickness <= 2
                            ? 1.0
                            : roofThickness <= 4
                                ? 0.55
                                : 0.2;
                        double depthFactor = 1.0 - Math.Clamp((top - y) / (double)depthSpan, 0.0, 1.0);
                        double slopePressure = Math.Clamp(slope / 20.0 + relief / 36.0, 0.0, 1.0);
                        double sealChance = bridgeWeight *
                            (wetness * 0.34 + erosion * 0.24 + slopePressure * 0.2 + thinRoofRisk * 0.12 + continuity * 0.1);
                        sealChance *= 0.68 + depthFactor * 0.32;

                        if (sealChance > 0.5 || (sealChance > 0.36 && thinRoofRisk > 0.5 && wetness > 0.42))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyHydrologySeamVault(bool[,,] mask, float[,] hydrologyMask, float[,] flowMask, float[,]? riverMask, int seaLevel)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, Math.Max(3, seaLevel + 4));
            int bottom = Math.Max(1, seaLevel - Math.Max(4, config.RiparianPlugDepth + 2));

            double aquiferBarrierWeight = Math.Clamp(config.AquiferBarrierWeight, 0.0, 1.0);
            double guardWeight = Math.Clamp(config.RiparianCaveGuardWeight, 0.0, 1.0);
            double edgeSeal = Math.Clamp(config.EdgeSealStrength, 0.0, 1.0);
            double moistureRetention = Math.Clamp(config.MoistureRetentionWeight, 0.0, 1.0);
            double moistureFlowClamp = Math.Max(0.0001, config.MoistureFlowClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    double flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);

                    double seamWetness = Math.Clamp(
                        hydro * 0.38 + seamHydro * 0.22 + flow * 0.18 + seamFlow * 0.12 + river * 0.1,
                        0.0,
                        1.15);

                    double continuity = Math.Clamp((hydroGradient + flowGradient) / moistureFlowClamp, 0.0, 1.0);
                    double vaultWeight = seamWetness * (aquiferBarrierWeight * 0.5 + guardWeight * 0.25 + moistureRetention * 0.15);
                    vaultWeight += continuity * edgeSeal * 0.3;
                    vaultWeight = Math.Clamp(vaultWeight, 0.0, 0.95);

                    if (vaultWeight < 0.28)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = vaultWeight * (0.55 + depthFactor * 0.45);
                        if (sealChance > 0.55 || (sealChance > 0.38 && seamWetness > 0.42))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private int CreateNoiseSeed(int chunkX, int chunkZ, int localX, int localZ, int y, int salt)
        {
            uint mixed = MixSeed((uint)worldSeedHash, (uint)chunkX, (uint)chunkZ, (uint)localX, (uint)localZ, (uint)y, (uint)salt);
            return (int)(mixed & 0x7FFFFFFF);
        }

        private double CreateDeterministicUnit(int chunkX, int chunkZ, int localX, int localZ, int y, int salt)
        {
            uint mixed = MixSeed((uint)worldSeedHash, (uint)chunkX, (uint)chunkZ, (uint)localX, (uint)localZ, (uint)y, (uint)salt);
            return (mixed & 0xFFFFFFu) / 16777215.0;
        }

        private int CreateDeterministicRange(int chunkX, int chunkZ, int localX, int localZ, int y, int salt, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint mixed = MixSeed((uint)worldSeedHash, (uint)chunkX, (uint)chunkZ, (uint)localX, (uint)localZ, (uint)y, (uint)salt);
            int width = maxExclusive - minInclusive;
            return minInclusive + (int)(mixed % (uint)width);
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
