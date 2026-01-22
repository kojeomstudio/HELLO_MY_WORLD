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
        private readonly Random random;

        public ImprovedRiverGenerator(WaterConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)(worldSeed ^ 0x7B3C9A01));
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
            double watershedBlend = Math.Clamp(config.HydrologyWatershedStitchWeight, 0.0, 1.0);
            int watershedRadius = Math.Max(1, config.HydrologyWatershedStitchRadius);
            double flowMemoryWeight = Math.Clamp(config.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double edgeNormalizationStrength = Math.Clamp(config.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double waterTableClampWeight = Math.Clamp(config.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double waterTableClampRange = Math.Max(1.0, config.HydrologyWaterTableClampRange);
            double waterTableSlopeWeight = Math.Clamp(config.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            double depthBias = Math.Clamp(config.RiverDepth / 12.0, 0.0, 1.0);
            double riverBankErosionWeight = Math.Clamp(config.RiverBankErosionWeight, 0.0, 1.0);

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
                        random.Next()));
                    double macroNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 0.4 + 71.0,
                        worldZ * noiseScale * 0.4 - 53.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        random.Next()));
                    double detailNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 1.85 - 17.0,
                        worldZ * noiseScale * 1.85 + 9.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        random.Next()));

                    double meanderNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale * 0.65 + 19.0,
                        worldZ * noiseScale * 0.65 - 11.0,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        random.Next()));
                    double meanderFactor = 1.0 + meanderNoise * (Math.Clamp(config.HydrologyWarpAmplitude * 0.02, 0.05, 0.2) + Math.Max(0.0, config.RiverMeanderJitter));
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
                    double directionality = (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * 0.5;
                    double flowAlignment = 1.0 + Math.Clamp(flow * config.RiverFlowAlignmentWeight * 0.35, 0.0, 0.45);
                    double seamStitch = 1.0 + Math.Clamp((TerrainMaskUtility.SampleInterior(hydrologyMask, x, z) - hydrologyMask[x, z]) * config.HydrologyEdgeFluxBlend, -0.35, 0.35);
                    double flowShadow = Math.Clamp(
                        flow * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5 +
                        seamStitch * flowShadowWeight * 0.25,
                        0.0,
                        0.75);
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * config.HydrologyEdgeStabilityWeight * 0.25, 0.0, 0.35);
                    double continuityBias = 1.0 + Math.Clamp((seamHydro + interiorFlow) * config.HydrologyEdgeFluxBlend * 0.2, -0.2, 0.35);
                    continuityBias *= 1.0 - Math.Clamp(hydrologyVariance * 0.15 + flowVariance * 0.1, 0.0, 0.25);
                    double seamAnchor = hydrology * 0.25 + seamHydro * 0.25 + flow * 0.25 + flowMemory * 0.25;

                    double riverMask = config.RiverBankThreshold - layeredNoise - erosion * riverBankErosionWeight * 0.08;
                    double pressure = Math.Max(0.0, riverMask);
                    double erosionBrake = 1.0 - Math.Clamp(erosion * riverBankErosionWeight * 0.45, 0.0, 0.45);
                    pressure *= 1.0 + hydrology * config.HydrologyContinuityWeight;
                    pressure *= 1.0 + flow * config.RiverFlowAlignmentWeight;
                    pressure *= 1.0 + directionality * config.RiverAnisotropyWeight * 0.2;
                    pressure *= 1.0 - Math.Clamp(gradient * config.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * reliefPenalty, 0.0, 0.35);
                    pressure *= flowAlignment * seamStitch * meanderFactor;
                    pressure *= 1.0 + (flowMemory + seamHydro) * flowMemoryWeight * 0.2;
                    pressure *= 1.0 + seamAnchor * edgeNormalization * 0.15;
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
                    pressure = pressure * (1.0 - braidedAssist * 0.25) + braidedAssist * 0.08;
                    pressure = pressure * (1.0 - flowShadow * 0.25) + hydrology * flowShadow * 0.15;
                    double flowMemoryContinuity = (flowMemory + seamHydro + hydrology) * 0.333;
                    double flowMemoryGradient = Math.Abs(flowMemory - flow);
                    pressure *= seamGuard;
                    pressure *= 1.0 + flowMemoryContinuity * 0.25;
                    pressure *= 1.0 - Math.Clamp(flowMemoryGradient * 0.2, 0.0, 0.35);
                    pressure *= 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * config.HydrologyEdgeStabilityWeight * 0.2, 0.0, 0.4);
                    pressure *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + flowVariance * 0.15, 0.0, 0.35);
                    if (confluenceBoost > 0.0)
                    {
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flowAccumulation, x, z) / 6.0;
                        double tributaryPressure = Math.Clamp((flow + neighbourFlow) * 0.5, 0.0, 1.0);
                        double hydrologyAssist = hydrology * 0.5 + hydrologyGradient * 0.15;
                        pressure *= 1.0 + (tributaryPressure + hydrologyAssist) * confluenceBoost * 0.35;
                    }

                    double floodplain = Math.Clamp((hydrology + seamHydro + flowMemory) * config.RiverDeltaWetlandStrength * 0.25, 0.0, 0.6);
                    double varianceAssist = Math.Clamp((hydrologyVariance + flowVariance) * config.HydrologyVarianceBlend * 0.15, -0.35, 0.45);
                    pressure = pressure * (1.0 - floodplain * 0.2) + floodplain * 0.1;
                    pressure *= 1.0 + varianceAssist;
                    pressure *= erosionBrake;
                    pressure *= 1.0 - Math.Clamp(erosion * reliefPenalty * 0.25, 0.0, 0.25);

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
                    pressure = ApplyEdgeBlend(pressure, hydrologyMask[x, z], x, z, chunkSize);

                    mask[x, z] = (float)Math.Clamp(pressure, 0.0, 1.35);
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(
                mask,
                config.HydrologyEdgeBlendRadius,
                Math.Max(0.05, config.HydrologySeamRelaxBlend * 0.35),
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
            FeatherEdges(mask, config.RiverEdgeFeather, config.RiverSeamFillStrength);
            return mask;
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
    }
}
