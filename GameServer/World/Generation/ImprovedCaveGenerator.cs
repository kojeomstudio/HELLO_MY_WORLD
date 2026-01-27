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
        private readonly Random random;
        private readonly double depthWeight;

        public ImprovedCaveGenerator(CaveConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)(worldSeed ^ 0x5A3C7B01));
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
                        random.Next()) * 0.5 + 0.5;

                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float flow = TerrainMaskUtility.Clamp01(flowMask[x, z]);
                    float flowMemory = TerrainMaskUtility.Clamp01((flow + TerrainMaskUtility.SampleInterior(flowMask, x, z)) * 0.5f);
                    double flowMemoryClamped = Math.Min(flowMemory, moistureFlowClamp);
                    float seamHydro = TerrainMaskUtility.SampleInterior(hydrologyMask, x, z);
                    float seamFlow = TerrainMaskUtility.SampleInterior(flowMask, x, z);
                    float riverPressure = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
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
                    double riparianCeilingGuard = Math.Clamp(
                        (hydrologyGradient + flowGradient + riverPressure + seamMemory) * config.CeilingStabilityWeight * 0.25,
                        0.0,
                        0.5);
                    stability *= 1.0 - riparianCeilingGuard * 0.35;
                    double wetnessRetention = hydrology * config.MoistureRetentionWeight + flowMemoryClamped * config.MoistureRetentionWeight * 0.35;
                    wetnessRetention += erosion * config.MoistureRetentionWeight * 0.2;
                    stability *= 1.0 - moistureContinuity * 0.35;
                    stability *= 1.0 - hydrologyShadow * 0.1;
                    double riparianSuppression = Math.Clamp(
                        (riverPressure + hydrologyGradient + flowGradient + seamMemory) * config.RiverSuppressionWeight * 0.5,
                        0.0,
                        0.6);
                    double hydrologyEnvelope = (hydrology + seamHydro + flowMemory) * 0.333;
                    double flowContinuity = Math.Clamp(Math.Abs(flowMemory - flow) * config.FlowStabilityWeight * 0.5, 0.0, 0.6);
                    double riparianBridge = Math.Clamp((hydrologyEnvelope + riverPressure) * config.RiverSuppressionWeight * 0.35, 0.0, 0.65);

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
                            random.Next());

                        double primary = SimplexNoise.Generate(
                            warpX + warp.dx,
                            warpZ + warp.dz + warpY,
                            1.0,
                            3,
                            1.0,
                            0.55,
                            random.Next());

                        double secondary = PerlinNoise.Generate(
                            warpX + 17.0,
                            warpZ - 11.0,
                            vertical * 0.5,
                            2,
                            1.0,
                            0.6,
                            random.Next());

                        double detail = Math.Abs(SimplexNoise.Generate(
                            warpX * 1.35 - 23.0,
                            warpZ * 1.35 + warpY * 0.35 + 17.0,
                            1.0,
                            2,
                            1.0,
                            0.55,
                            random.Next()));

                        double density = (primary * 0.55) + (secondary * 0.25) + (detail * 0.2);
                        double moisturePenalty = hydrology * config.HydrologyStabilityWeight + riverPressure * config.RiverSuppressionWeight + wetnessRetention * 0.35;
                        double roughnessBias = (0.5 + SimplexNoise.Generate(warpX * 0.8, warpZ * 0.8, 1.0, 1, 1.0, 0.5, random.Next()) * 0.5) * config.RoughnessStabilityWeight;
                        double flowPenalty = flow * config.FlowStabilityWeight;
                        double flowMemoryClamp = Math.Clamp(flowMemory * config.MoistureRetentionWeight, 0.0, 1.0);
                        double threshold = config.Threshold + moisturePenalty * 0.35 + flowPenalty * 0.35 + roughnessBias * 0.25;
                        threshold -= depthFactor * depthWeight * 0.6;
                        threshold -= Math.Clamp(detail * depthFactor * 0.2, 0.0, 0.2);
                        threshold += wetnessRetention * 0.15;
                        threshold += edgeFactor * config.EdgeSealStrength * 0.35;
                        threshold += seamMemory * config.FlowStabilityWeight * 0.15;
                        threshold += Math.Clamp(hydrologyGradient * (config.EdgeSealStrength + config.HydrologyStabilityWeight * 0.25), 0.0, 0.35);
                        threshold += Math.Clamp(flowGradient * config.EdgeSealStrength * 0.2, 0.0, 0.2);
                        threshold += riparianSuppression * 0.25;
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
                        threshold += Math.Clamp(flowShadow * 0.15, 0.0, 0.25);
                        threshold += variancePenalty * 0.25;
                        threshold += Math.Clamp(flowVariance * config.RoughnessStabilityWeight * 0.2, 0.0, 0.25);
                        threshold += flowMemoryClamp * 0.15;
                        threshold += erosion * config.RiverSuppressionWeight * 0.2;
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
            AddSupportColumns(mask, hydrologyMask, riverMask, seaLevel);
            SealEdges(mask, hydrologyMask, riverMask, config.EdgeSealStrength);
            SealWetCeilings(mask, hydrologyMask, flowMask, seaLevel);
            return mask;
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

        private void AddSupportColumns(bool[,,] mask, float[,] hydrologyMask, float[,]? riverMask, int seaLevel)
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
                    if (random.NextDouble() > pillarChance)
                    {
                        continue;
                    }

                    int baseY = Math.Max(1, seaLevel - 6);
                    int height = random.Next(2, 6);
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

        private void SealEdges(bool[,,] mask, float[,] hydrologyMask, float[,]? riverMask, double strength)
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
                        if (mask[x, y, z] && random.NextDouble() < sealChance)
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
    }
}
