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
        private readonly Random random;

        public ImprovedLakeGenerator(LakeConfig lakeConfig, WaterConfig waterConfig, long worldSeed)
        {
            this.lakeConfig = lakeConfig ?? throw new ArgumentNullException(nameof(lakeConfig));
            this.waterConfig = waterConfig ?? throw new ArgumentNullException(nameof(waterConfig));
            random = new Random((int)(worldSeed ^ 0x1A2E0001));
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

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double basinNoise = SimplexNoise.Generate(worldX * 0.004, worldZ * 0.004, 1.0, 3, 1.0, 0.6, random.Next());
                    double rimNoise = SimplexNoise.Generate(worldX * 0.009 + 31, worldZ * 0.009 + 17, 1.0, 2, 1.0, 0.55, random.Next());
                    double macroNoise = SimplexNoise.Generate(worldX * 0.0017 - 37.0, worldZ * 0.0017 + 23.0, 1.0, 2, 1.0, 0.6, random.Next());
                    double detailNoise = Math.Abs(SimplexNoise.Generate(worldX * 0.0065 + 3.0, worldZ * 0.0065 - 5.0, 1.0, 2, 1.0, 0.55, random.Next()));
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
                        random.Next())) * lakeConfig.ShorelineBlend * 0.25;

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
                    double flowMemoryGradient = Math.Abs(flowMemory - flow);
                    weight *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + hydrologyGradient * 0.1 + flowMemoryGradient * 0.15, 0.0, 0.35);
                    double seamCushion = 1.0 + Math.Clamp((seamHydro - hydrology) * waterConfig.HydrologyEdgeFluxBlend, -0.2, 0.3);
                    weight *= seamCushion * seamGuard * seamContinuityBias * flowSeepageContinuity;
                    double divergenceBrake = Math.Min(1.0, Math.Abs(flowMemory - seamHydro) / divergenceClamp);
                    weight *= 1.0 - Math.Clamp(divergenceBrake * reservoirBlend, 0.0, 0.25);
                    weight = weight * (1.0 - reservoirBlend * 0.2) + (weight + catchmentMemory) * reservoirBlend * 0.2;
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

            TerrainMaskUtility.ApplyHydrologyContinuity(
                lakes,
                hydrologyMask,
                flowAccumulation,
                waterConfig.HydrologyEdgeBlendRadius,
                waterConfig.HydrologyContinuityWeight);
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
            return lakes;
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
    }
}
