using System;
using GameServerApp;
using GameServerApp.World;
using GameServerApp.Utils;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Aggregates improved cave/river/lake mask generation using the data-driven world config.
    /// </summary>
    public sealed record TerrainMaskResult
    {
        public bool[,,]? Caves { get; init; }
        public float[,]? Rivers { get; init; }
        public float[,]? Lakes { get; init; }
        public float[,] Hydrology { get; init; } = default!;
        public float[,] FlowAccumulation { get; init; } = default!;
    }

    public sealed class ImprovedTerrainCoordinator
    {
        private readonly WorldGenerationConfig config;
        private readonly int chunkSize;
        private readonly int worldHeight;
        private readonly int seaLevel;
        private readonly long worldSeed;
        private readonly ImprovedCaveGenerator caveGenerator;
        private readonly ImprovedRiverGenerator riverGenerator;
        private readonly ImprovedLakeGenerator lakeGenerator;

        public ImprovedTerrainCoordinator(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (worldSettings == null) throw new ArgumentNullException(nameof(worldSettings));

            chunkSize = Math.Max(1, config.ChunkSize);
            worldHeight = Math.Max(1, config.WorldHeight);
            seaLevel = Math.Clamp(
                config.TerrainGeneration.SeaLevel <= 0 ? config.Water.GlobalWaterLevel : config.TerrainGeneration.SeaLevel,
                4,
                worldHeight - 4);

            worldSeed = worldSettings.WorldSeed != 0 ? worldSettings.WorldSeed : config.Seed;
            caveGenerator = new ImprovedCaveGenerator(config.Caves, worldSeed);
            riverGenerator = new ImprovedRiverGenerator(config.Water, worldSeed);
            lakeGenerator = new ImprovedLakeGenerator(config.Lakes, config.Water, worldSeed);
        }

        public TerrainMaskResult GenerateMasks(int chunkX, int chunkZ, int[,] heightMap, int sizeOverride)
        {
            int size = Math.Min(Math.Max(1, sizeOverride), chunkSize);
            var hydrology = BuildHydrologyMask(heightMap, size);
            var flow = BuildFlowAccumulation(heightMap, hydrology, size);
            ApplyFlowMemory(heightMap, hydrology, flow);
            BlendHydrologyWithFlow(heightMap, hydrology, flow);
            HarmonizeHydrologyWithSurface(heightMap, hydrology, flow);

            float[,]? riverMask = config.Water.EnableRivers
                ? riverGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, seaLevel)
                : null;

            float[,]? lakeMask = config.Water.EnableLakes
                ? lakeGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, riverMask, seaLevel)
                : null;

            bool[,,]? caveMask = config.Caves.EnableCaves
                ? caveGenerator.BuildMask(chunkX, chunkZ, size, worldHeight, heightMap, hydrology, flow, riverMask, seaLevel)
                : null;

            return new TerrainMaskResult
            {
                Caves = caveMask,
                Rivers = riverMask,
                Lakes = lakeMask,
                Hydrology = hydrology,
                FlowAccumulation = flow
            };
        }

        private float[,] BuildHydrologyMask(int[,] heightMap, int size)
        {
            var hydrology = new float[size, size];
            double clampRange = Math.Max(1, config.Water.HydrologyWaterTableClampRange);
            double clampWeight = Math.Clamp(config.Water.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double slopeWeight = Math.Clamp(config.Water.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            double slopePenaltyWeight = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double curvatureWeight = Math.Clamp(config.Water.HydrologyCurvatureWeight, 0.0, 1.5);
            double varianceClamp = Math.Clamp(config.Water.HydrologyVarianceClamp, 0.0, 2.0);
            double shorePush = Math.Max(0.1, config.Water.HydrologyShorePush);

            double varianceBlend = Math.Clamp(config.Water.HydrologyVarianceBlend, 0.0, 1.0);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - seaLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / clampRange, 0.0, 1.0);
                    double shoreBoost = Math.Exp(-distance / shorePush);
                    double slopePenalty = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slopePenalty * (slopeWeight + slopePenaltyWeight * 0.1) / 6.0, 0.0, 0.7);
                    double gradientDamp = 1.0 - Math.Clamp(slopePenalty * gradientWeight / Math.Max(1.0, config.Water.HydrologyGradientClamp * 8.0), 0.0, 0.35);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * curvatureWeight * 0.08;
                    double warpedNoise = SimplexNoise.Generate(
                        (x + 17) * config.Water.HydrologyWarpFrequency,
                        (z + 31) * config.Water.HydrologyWarpFrequency,
                        1.0,
                        2,
                        config.Water.HydrologyWarpAmplitude * 0.15,
                        0.6,
                        (int)(worldSeed ^ 0x6611));
                    double baseline = Math.Clamp(waterBias * clampWeight * stability * gradientDamp, 0.0, 1.0);
                    baseline = Math.Clamp(baseline + warpedNoise * 0.05 + shoreBoost * 0.05 - curvature, 0.0, 1.2);
                    hydrology[x, z] = (float)baseline;
                }
            }

            if (varianceBlend > 0.0)
            {
                TerrainMaskUtility.BlendInterior(hydrology, varianceBlend);
            }

            TerrainMaskUtility.Smooth2D(hydrology, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.DirectionalSmooth(heightMap, hydrology, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            TerrainMaskUtility.ApplyRiparianBuffer(hydrology, config.Water.RiparianBufferRadius, config.Water.RiparianSaturationBoost);
            TerrainMaskUtility.StabilizeEdges(
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeStabilityIterations,
                config.Water.HydrologyEdgeStabilityWeight,
                config.Water.HydrologyEdgeFluxBlend);
            TerrainMaskUtility.ApplyEdgeFlowLocks(
                heightMap,
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeFlowLockWeight,
                config.Water.HydrologyEdgeFlowBias,
                config.Water.HydrologyEdgeTangentWeight);
            TerrainMaskUtility.ApplyGradientStability(
                hydrology,
                config.Water.HydrologyGradientStabilityIterations,
                config.Water.HydrologyGradientStabilityBlend,
                config.Water.HydrologyGradientClamp);
            TerrainMaskUtility.FillBasins(
                hydrology,
                Math.Max(0.05, config.Water.HydrologyEdgeStabilityWeight * 0.5),
                Math.Max(1, config.Water.HydrologySeamRelaxIterations));
            TerrainMaskUtility.StitchEdges(hydrology, config.Water.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.ClampVariance(hydrology, varianceClamp);
            TerrainMaskUtility.RelaxEdges(hydrology, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return hydrology;
        }

        private float[,] BuildFlowAccumulation(int[,] heightMap, float[,] hydrology, int size)
        {
            var flow = new float[size, size];
            double persistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double divergenceClamp = Math.Clamp(config.Water.HydrologyFlowDivergenceClamp, 0.1, 1.5);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    double accumulation = 0.0;
                    double current = heightMap[x, z];
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    double gradientMagnitude = Math.Sqrt(downhill.X * downhill.X + downhill.Z * downhill.Z);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= size || nz >= size) continue;

                            double neighbor = heightMap[nx, nz];
                            if (neighbor < current)
                            {
                                accumulation += (current - neighbor) * 0.25;
                            }
                        }
                    }

                    double hydrologyBoost = hydrology[x, z] * config.Water.HydrologyFlowGain;
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * config.Water.HydrologyCurvatureWeight * 0.1;
                    double continuity = 1.0 + hydrology[x, z] * config.Water.HydrologyContinuityWeight;
                    double scaled = ((accumulation * (1.0 - persistence)) + hydrologyBoost) * continuity;
                    double meanderNoise = Math.Abs(SimplexNoise.Generate(
                        (x + 17 + worldSeed % 997) * config.Water.HydrologyWarpFrequency * 12.0,
                        (z - 31 + worldSeed % 883) * config.Water.HydrologyWarpFrequency * 12.0,
                        1.0,
                        2,
                        Math.Max(0.1, config.Water.HydrologyWarpAmplitude * 0.02),
                        0.55,
                        (int)(worldSeed ^ 0x5FFF)));
                    scaled *= 1.0 + meanderNoise * Math.Clamp(config.Water.HydrologyVarianceBlend, 0.0, 1.0) * 0.15;
                    scaled *= 1.0 - Math.Clamp(curvature, 0.0, 0.6);
                    scaled *= 1.0 - Math.Clamp(gradientMagnitude * config.Water.HydrologyGradientSlopeWeight * 0.05, 0.0, 0.35);
                    double clampMax = Math.Max(2.5, divergenceClamp * 12.0);
                    flow[x, z] = (float)Math.Clamp(scaled, 0.0, clampMax);
                }
            }

            TerrainMaskUtility.Smooth2D(flow, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.DirectionalSmooth(heightMap, flow, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            TerrainMaskUtility.StabilizeEdges(
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeStabilityIterations,
                config.Water.HydrologyEdgeStabilityWeight,
                config.Water.HydrologyEdgeFluxBlend);
            TerrainMaskUtility.ApplyGradientStability(
                flow,
                config.Water.HydrologyGradientStabilityIterations,
                config.Water.HydrologyGradientStabilityBlend,
                config.Water.HydrologyGradientClamp);
            TerrainMaskUtility.ApplyEdgeFlowLocks(
                heightMap,
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeFlowLockWeight,
                config.Water.HydrologyEdgeFlowBias,
                config.Water.HydrologyEdgeTangentWeight);
            TerrainMaskUtility.FillBasins(
                flow,
                Math.Max(0.05, config.Water.HydrologyEdgeStabilityWeight * 0.35),
                Math.Max(1, config.Water.HydrologySeamRelaxIterations));
            TerrainMaskUtility.StitchEdges(flow, config.Water.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.RelaxEdges(flow, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return flow;
        }

        private void ApplyFlowMemory(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowPersistence * 0.35, 0.0, 0.6);
            double watershedBlend = Math.Clamp(config.Water.HydrologyWatershedStitchWeight, 0.0, 1.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            int watershedRadius = Math.Max(1, config.Water.HydrologyWatershedStitchRadius);
            if (memoryWeight <= 0.0 && watershedBlend <= 0.0 && flowShadowWeight <= 0.0)
            {
                return;
            }

            var buffer = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float flowValue = flow[x, z];
                    float hydro = hydrology[x, z];
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillFlow = flow[downX, downZ];
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);

                    double continuity = 1.0 + hydro * config.Water.HydrologyContinuityWeight + neighbourHydro * 0.25;
                    double memory = flowValue * (1.0 - memoryWeight);
                    memory += (downhillFlow + flowValue) * (memoryWeight * 0.25);
                    memory += neighbourFlow * (memoryWeight * 0.35);
                    memory += hydro * memoryWeight * 0.25;
                    memory *= continuity;
                    memory *= 1.0 - Math.Clamp(hydrologyGradient * flowShadowWeight * 0.25, 0.0, 0.3);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double seamAnchor = neighbourHydro * 0.35 + hydro * 0.35 + neighbourFlow * 0.3;
                        memory = memory * (1.0 - edgeRepair * 0.55) + seamAnchor * edgeRepair;
                    }

                    buffer[x, z] = (float)Math.Clamp(
                        memory,
                        0.0,
                        Math.Max(flowValue + 1.5, config.Water.HydrologyFlowDivergenceClamp * 12.0));
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
        }

        private void BlendHydrologyWithFlow(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double flowBlend = Math.Clamp(config.Water.HydrologyContinuityWeight * 0.35, 0.05, 0.45);
            double edgeBlend = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight * 0.5, 0.0, 0.45);
            int edgeRadius = Math.Max(
                1,
                Math.Max(config.Water.HydrologyEdgeBlendRadius, config.Water.HydrologyWatershedStitchRadius));
            int watershedRadius = Math.Max(1, config.Water.HydrologyWatershedStitchRadius);
            double confluenceBoost = Math.Clamp(config.Water.RiverConfluenceBoost, 0.0, 2.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double directionalBias = Math.Clamp(config.Water.HydrologyDirectionalBlend * 0.5, 0.0, 0.5);
            double watershedBlend = Math.Clamp(config.Water.HydrologyWatershedStitchWeight, 0.0, 1.0);

            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    double normalizedFlow = Math.Clamp(flowValue / Math.Max(1.0, config.Water.RiverDepth), 0.0, 1.0);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / Math.Max(1.0, config.Water.RiverDepth);
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = Math.Clamp(1.0 - edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double edgeFactor = edgeBlend * edgeFalloff + watershedBlend * edgeFalloff * 0.5;
                    double blend = Math.Clamp(flowBlend + edgeFactor, 0.0, 0.9);

                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double directionalHydro = hydrology[downX, downZ];
                    double directionalFlow = Math.Clamp(flow[downX, downZ] / Math.Max(1.0, config.Water.RiverDepth), 0.0, 1.0);
                    double directionalWeight = Math.Clamp((Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * directionalBias + directionalFlow * 0.2, 0.0, 0.45);

                    double confluence = confluenceBoost > 0.0
                        ? (neighbourFlow * 0.5 + neighbourHydro * 0.25 + hydrologyGradient * 0.15) * confluenceBoost
                        : 0.0;

                    double flowShadow = Math.Clamp(
                        (normalizedFlow + neighbourFlow) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5 +
                        directionalFlow * flowShadowWeight * 0.15,
                        0.0,
                        0.7);

                    double blended = hydro * (1.0 - blend) + normalizedFlow * blend;
                    blended = blended * (1.0 - flowShadow * 0.35) + neighbourHydro * flowShadow * 0.35;
                    blended = blended * (1.0 - directionalWeight) + directionalHydro * directionalWeight;
                    blended *= 1.0 + confluence;
                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, 1.25);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
            TerrainMaskUtility.BlendWatershedEdges(
                heightMap,
                hydrology,
                flow,
                watershedRadius,
                watershedBlend,
                flowShadowWeight);
            TerrainMaskUtility.ClampVariance(hydrology, config.Water.HydrologyVarianceClamp);
            TerrainMaskUtility.ApplyFlowShadow(hydrology, flow, flowShadowWeight, flowShadowSlopeWeight);
            TerrainMaskUtility.StitchEdges(hydrology, Math.Min(0.65, config.Water.HydrologySeamRelaxBlend * 0.85));
        }

        private void HarmonizeHydrologyWithSurface(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double edgeClamp = Math.Clamp(config.Water.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double stabilityWeight = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double curvatureWeight = Math.Clamp(config.Water.HydrologyCurvatureWeight, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double clampMax = Math.Max(2.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);
                    double flowGradient = Math.Abs(neighbourFlow - flowValue);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * curvatureWeight * 0.05;

                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double downhillHydro = hydrology[downX, downZ];
                    double downhillFlow = flow[downX, downZ];

                    double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBlend = 1.0 - Math.Clamp(edgeDistance / (edgeRadius + 1.0), 0.0, 1.0);

                    double stability = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * stabilityWeight, 0.0, 0.55);
                    stability *= 1.0 - Math.Clamp(slope / Math.Max(1.0, slopePenalty * 1.1), 0.0, 0.55);

                    double anchorHydro = hydro * (0.6 + flowPersistence * 0.25) + neighbourHydro * 0.25 + neighbourFlow * 0.15;
                    double directionalAnchor = downhillHydro * 0.25 + downhillFlow * 0.15;
                    double blend = Math.Clamp(
                        hydrologyGradient * (0.35 + gradientWeight * 0.35) +
                        flowGradient * 0.15 +
                        edgeBlend * 0.35 +
                        curvature, 0.0, 0.85);

                    double harmonized = (anchorHydro + directionalAnchor) * stability;
                    double anchoredHydro = hydro * (1.0 - blend) + harmonized * blend;
                    double edgeAnchor = hydro * (1.0 - edgeBlend * edgeClamp) + neighbourHydro * edgeBlend * edgeClamp;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(
                        anchoredHydro * (1.0 - edgeBlend * 0.35) + edgeAnchor * edgeBlend * 0.35,
                        0.0,
                        1.25));

                    double flowAnchor = hydrology[x, z] * 0.5 + flowValue * (0.5 + flowPersistence * 0.2);
                    flow[x, z] = (float)Math.Clamp(
                        flowValue * (1.0 - blend * 0.35) + flowAnchor * blend * 0.35,
                        0.0,
                        clampMax);
                }
            }
        }

        private static double SampleCurvature(int[,] heightMap, int x, int z)
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
    }

    internal static class TerrainMaskUtility
    {
        public static float Clamp01(double value) => (float)Math.Clamp(value, 0.0, 1.0);

        public static double ComputeSlope(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int east = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int north = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            double dx = center - east;
            double dz = center - north;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public static void Smooth2D(float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float sum = field[x, z];
                        int samples = 1;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ) continue;
                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = sum / Math.Max(1, samples);
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + average * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void DirectionalSmooth(int[,] heightMap, float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (iterations == 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        var downhill = ComputeDownhillVector(heightMap, x, z);
                        int nx = Math.Clamp(x + Math.Sign(downhill.X), 0, sizeX - 1);
                        int nz = Math.Clamp(z + Math.Sign(downhill.Z), 0, sizeZ - 1);
                        float neighbour = field[nx, nz];
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + neighbour * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void StabilizeEdges(float[,] field, int radius, int iterations, double weight, double fluxBlend)
        {
            radius = Math.Max(1, radius);
            iterations = Math.Max(0, iterations);
            weight = Math.Clamp(weight, 0.0, 1.0);
            fluxBlend = Math.Clamp(fluxBlend, 0.0, 1.0);
            if (iterations == 0 || weight <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isEdge = x < radius || z < radius || x >= sizeX - radius || z >= sizeZ - radius;
                        if (!isEdge)
                        {
                            buffer[x, z] = field[x, z];
                            continue;
                        }

                        float interior = SampleInterior(field, x, z);
                        double blend = weight * (1.0 - Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z)) / (double)radius);
                        double stabilised = field[x, z] * (1.0 - blend) + interior * blend;
                        buffer[x, z] = (float)(stabilised * (1.0 - fluxBlend) + interior * fluxBlend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void ApplyRiparianBuffer(float[,] field, int radius, double saturationBoost)
        {
            radius = Math.Max(0, radius);
            saturationBoost = Math.Clamp(saturationBoost, 0.0, 2.0);
            if (radius == 0 || saturationBoost <= 0.0)
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

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.001)
                            {
                                continue;
                            }

                            float influence = Clamp01(centre * saturationBoost * (1.0 - distance / (radius + 0.001)));
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ApplyEdgeFlowLocks(int[,] heightMap, float[,] field, int radius, double lockWeight, double flowBias, double tangentWeight)
        {
            radius = Math.Max(1, radius);
            lockWeight = Math.Clamp(lockWeight, 0.0, 1.0);
            flowBias = Math.Clamp(flowBias, 0.0, 1.0);
            tangentWeight = Math.Clamp(tangentWeight, 0.0, 1.0);
            if (lockWeight <= 0.0 && flowBias <= 0.0 && tangentWeight <= 0.0)
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
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double blend = lockWeight * (1.0 - edgeDistance / (double)radius);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int nx = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int nz = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillValue = field[nx, nz];

                    int tx = Math.Clamp(x - downhill.Z, 0, sizeX - 1);
                    int tz = Math.Clamp(z + downhill.X, 0, sizeZ - 1);
                    float tangentValue = field[tx, tz];

                    float interior = SampleInterior(field, x, z);
                    double flowAligned = field[x, z] * (1.0 - flowBias) + downhillValue * flowBias;
                    double tangentAligned = field[x, z] * (1.0 - tangentWeight) + tangentValue * tangentWeight;
                    double locked = (flowAligned * 0.6) + (tangentAligned * 0.4);
                    double blended = field[x, z] * (1.0 - blend) + interior * (blend * 0.3) + locked * (blend * 0.7);

                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, Math.Max(1.5, field[x, z] + 0.35));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ClampVariance(float[,] field, double clamp)
        {
            clamp = Math.Clamp(clamp, 0.0, 2.0);
            if (clamp <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = Clamp01(centre * (float)(1.0 - clamp * 0.5) + interior * (float)(clamp * 0.5));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void RelaxEdges(float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (x > 0 && x < sizeX - 1 && z > 0 && z < sizeZ - 1)
                        {
                            continue;
                        }

                        float neighbour = SampleInterior(field, x, z);
                        field[x, z] = (float)(field[x, z] * (1.0 - blend) + neighbour * blend);
                    }
                }
            }
        }

        public static void StitchEdges(float[,] field, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
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
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + interior * blend);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void FillBasins(float[,] field, double strength, int iterations)
        {
            strength = Math.Clamp(strength, 0.0, 1.0);
            iterations = Math.Max(0, iterations);
            if (strength <= 0.0 || iterations == 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float value = field[x, z];
                        float neighbour = SampleInterior(field, x, z);
                        if (value >= neighbour)
                        {
                            buffer[x, z] = value;
                            continue;
                        }

                        double delta = (neighbour - value) * strength * 0.5;
                        buffer[x, z] = Clamp01(value + (float)delta);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void ApplyFlowShadow(float[,] hydrology, float[,] flow, double weight, double slopeWeight)
        {
            weight = Math.Clamp(weight, 0.0, 1.0);
            slopeWeight = Math.Clamp(slopeWeight, 0.0, 1.0);
            if (weight <= 0.0 && slopeWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    double flowShadow = Math.Clamp((flowValue + neighbourFlow) * 0.5 * weight, 0.0, 0.6);

                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    double slopeShadow = Math.Clamp(Math.Abs(hydro - neighbourHydro) * slopeWeight, 0.0, 0.35);

                    double dampened = hydro * (1.0 - flowShadow * 0.35 - slopeShadow * 0.35) + neighbourHydro * (flowShadow * 0.2);
                    buffer[x, z] = Clamp01(dampened);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
        }

        public static float SampleInterior(float[,] field, int x, int z)
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

        public static void BlendInterior(float[,] field, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + interior * blend);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ApplyGradientStability(float[,] field, int iterations, double blend, double gradientClamp)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            gradientClamp = Math.Max(0.0001, gradientClamp);
            if (iterations == 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float centre = field[x, z];
                        float interior = SampleInterior(field, x, z);
                        double gradient = Math.Abs(centre - interior);
                        double weight = Math.Clamp(gradient / gradientClamp, 0.0, 1.0) * blend;
                        if (weight <= 0.0)
                        {
                            buffer[x, z] = centre;
                            continue;
                        }

                        double stabilised = centre * (1.0 - weight) + interior * weight;
                        double clampMax = Math.Max(Math.Max(centre, interior) + gradientClamp * 0.5, 1.0);
                        buffer[x, z] = (float)Math.Clamp(stabilised, 0.0, clampMax);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void BlendWatershedEdges(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            int radius,
            double blendWeight,
            double flowAnchorWeight)
        {
            radius = Math.Max(0, radius);
            blendWeight = Math.Clamp(blendWeight, 0.0, 1.0);
            flowAnchorWeight = Math.Clamp(flowAnchorWeight, 0.0, 1.0);
            if (radius <= 0 || blendWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(radius + 1);
                    double blend = blendWeight * falloff;

                    float interiorHydro = SampleInterior(hydroCopy, x, z);
                    float interiorFlow = SampleInterior(flowCopy, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillHydro = hydroCopy[downX, downZ];
                    float downhillFlow = flowCopy[downX, downZ];

                    double flowAnchor = Math.Clamp((flowCopy[x, z] + interiorFlow + downhillFlow) / 3.0, 0.0, 8.0);
                    flowAnchor = Math.Clamp(flowAnchor * flowAnchorWeight, 0.0, 4.0);

                    double targetHydro = interiorHydro * 0.55 + downhillHydro * 0.25 + flowAnchor * 0.1 + hydroCopy[x, z] * 0.1;
                    double targetFlow = interiorFlow * 0.5 + downhillFlow * 0.25 + flowAnchor * 0.25;

                    hydrology[x, z] = Clamp01(hydroCopy[x, z] * (1.0 - blend) + targetHydro * blend);
                    flow[x, z] = (float)Math.Clamp(flowCopy[x, z] * (1.0 - blend * 0.5) + targetFlow * blend, 0.0, Math.Max(2.5, targetFlow * 1.5 + 0.5));
                }
            }
        }

        public static (int X, int Z) ComputeDownhillVector(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int bestDrop = 0;
            int bestX = 0;
            int bestZ = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                    {
                        continue;
                    }

                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestX = dx;
                        bestZ = dz;
                    }
                }
            }

            return (bestX, bestZ);
        }
    }
}
