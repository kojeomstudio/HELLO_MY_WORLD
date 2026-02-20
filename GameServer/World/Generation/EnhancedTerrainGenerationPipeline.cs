using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerApp;
using GameServerApp.Models;
using GameServerApp.Utils;
using Microsoft.Extensions.Logging;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Terrain pipeline that keeps caves, rivers, and lakes consistent with the JSON world config.
    /// Hydrology-aware smoothing is applied to reduce chunk seam artifacts.
    /// </summary>
    public class EnhancedTerrainGenerationPipeline
    {
        private readonly WorldGenerationConfig config;
        private readonly WorldSettings worldSettings;
        private readonly ILogger? logger;
        private readonly Random random;
        private readonly ImprovedTerrainCoordinator? improvedCoordinator;

        private readonly int seaLevel;
        private readonly int bedrockLevel;
        private readonly int worldHeight;
        private readonly int chunkSize;

        public EnhancedTerrainGenerationPipeline(
            WorldGenerationConfig config,
            WorldSettings worldSettings,
            ILogger? logger = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));
            this.logger = logger;

            // ChunkData is fixed to 16x16 columns; clamp to avoid out-of-bounds writes if config drifts.
            chunkSize = Math.Min(16, Math.Max(1, config.ChunkSize));
            worldHeight = Math.Min(256, Math.Max(1, config.WorldHeight));
            seaLevel = (int)Math.Clamp(config.TerrainGeneration.SeaLevel <= 0
                ? config.Water.GlobalWaterLevel
                : config.TerrainGeneration.SeaLevel, 4, worldHeight - 8);
            bedrockLevel = Math.Max(1, config.TerrainGeneration.BedrockLevel);
            random = new Random((int)(worldSettings.WorldSeed ^ 0x5f3759df));

            if ((config.Caves.EnableCaves && config.Caves.UseImprovedCaves) ||
                (config.Water.EnableRivers && config.Water.UseImprovedRivers) ||
                (config.Water.EnableLakes && config.Water.UseImprovedLakes))
            {
                improvedCoordinator = new ImprovedTerrainCoordinator(config, worldSettings);
            }
        }

        public Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GenerateChunkInternal(chunkX, chunkZ), cancellationToken);
        }

        private ChunkData GenerateChunkInternal(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ);

            var heightMap = BuildHeightMap(chunkX, chunkZ);
            PaintBaseTerrain(chunk, heightMap);

            bool enableCaves = worldSettings.EnableCaves && config.Caves.EnableCaves;
            bool enableRivers = worldSettings.EnableRivers && config.Water.EnableRivers;
            bool enableLakes = worldSettings.EnableLakes && config.Water.EnableLakes;

            TerrainMaskResult? improvedMasks = null;
            if (improvedCoordinator != null && (enableCaves || enableRivers || enableLakes))
            {
                improvedMasks = improvedCoordinator.GenerateMasks(chunkX, chunkZ, heightMap, chunkSize);
            }

            float[,]? hydrologyMask = improvedMasks?.Hydrology;
            float[,]? flowAccumulation = improvedMasks?.FlowAccumulation;

            if (hydrologyMask == null && (enableRivers || enableLakes || enableCaves))
            {
                hydrologyMask = BuildHydrologyMask(heightMap);
            }

            if (flowAccumulation == null && hydrologyMask != null)
            {
                flowAccumulation = BuildFlowMask(heightMap, hydrologyMask);
            }

            if (hydrologyMask != null && flowAccumulation != null)
            {
                BlendHydrologyWithFlow(heightMap, hydrologyMask, flowAccumulation);
                NormalizeHydrologyFlowEdges(hydrologyMask, flowAccumulation);
                ApplyWaterTableEnvelope(heightMap, hydrologyMask, flowAccumulation);
                ApplyHydrologyEdgeEnvelope(hydrologyMask, flowAccumulation);
                ApplyHydrologyReservoirSmoothing(heightMap, hydrologyMask, flowAccumulation);
                StabilizeHydrologyFields(heightMap, hydrologyMask, flowAccumulation);
                if (improvedMasks == null)
                {
                    TerrainMaskUtility.BalanceHydrologyPressure(
                        hydrologyMask,
                        flowAccumulation,
                        config.Water.HydrologyPressureBlend,
                        config.Water.HydrologyPressureGradientClamp);
                }
            }

            float[,]? riverMask = enableRivers
                ? improvedMasks?.Rivers ?? BuildRiverMask(chunkX, chunkZ, heightMap, hydrologyMask, flowAccumulation)
                : null;
            float[,]? lakeMask = enableLakes
                ? improvedMasks?.Lakes ?? BuildLakeMask(chunkX, chunkZ, heightMap, hydrologyMask, flowAccumulation, riverMask)
                : null;

            if (hydrologyMask != null && flowAccumulation != null)
            {
                if (lakeMask != null)
                {
                    ApplyLakeHydrologySeepage(heightMap, hydrologyMask, flowAccumulation, lakeMask, riverMask);
                }

                ApplyHydrologyEnvelope(riverMask, lakeMask, hydrologyMask, flowAccumulation);
            }

            bool[,,]? caveMask = enableCaves
                ? improvedMasks?.Caves ?? BuildCaveMask(chunkX, chunkZ, heightMap, hydrologyMask, flowAccumulation, riverMask)
                : null;

            if (caveMask != null)
            {
                CarveCaves(chunk, caveMask, heightMap, hydrologyMask, flowAccumulation);
            }

            ApplyHydrology(chunk, heightMap, riverMask, lakeMask, hydrologyMask, flowAccumulation);
            return chunk;
        }

        private int[,] BuildHeightMap(int chunkX, int chunkZ)
        {
            var heightMap = new int[chunkSize, chunkSize];
            var terrain = config.TerrainGeneration;
            double noiseScale = Math.Max(0.00001, 1.0 / Math.Max(terrain.NoiseScale, 1.0));

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double continental = SimplexNoise.Generate(worldX, worldZ, noiseScale * 0.25, terrain.Octaves, terrain.NoiseAmplitude, terrain.Persistence, (int)worldSettings.WorldSeed);
                    double macro = SimplexNoise.Generate(worldX + 77, worldZ + 19, noiseScale * 0.5, terrain.Octaves + 1, terrain.NoiseAmplitude * 0.5, terrain.Persistence, (int)worldSettings.WorldSeed ^ 0x00FF00);
                    double detail = PerlinNoise.Generate(worldX + 13.0, worldZ + 29.0, noiseScale * terrain.Lacunarity, 3, 1.0, 0.55, (int)worldSettings.WorldSeed ^ 0x0F0F0F);

                    double blended = (continental * 0.55) + (macro * 0.3) + (detail * 0.15);
                    double baseHeight = terrain.PlainBaseHeight + blended * terrain.NoiseAmplitude;
                    baseHeight = Math.Clamp(baseHeight, bedrockLevel + 1, worldHeight - 4);

                    heightMap[x, z] = (int)baseHeight;
                }
            }

            return heightMap;
        }

        private float[,] BuildHydrologyMask(int[,] heightMap)
        {
            var hydrology = new float[chunkSize, chunkSize];
            double clampRange = Math.Max(1.0, config.Water.HydrologyWaterTableClampRange);
            double clampWeight = Math.Clamp(config.Water.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double slopeWeight = Math.Clamp(config.Water.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            double slopePenaltyWeight = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(config.Water.HydrologyVarianceClamp, 0.0, 2.0);
            double varianceBlend = Math.Clamp(config.Water.HydrologyVarianceBlend, 0.0, 1.0);
            double shorePush = Math.Max(0.1, config.Water.HydrologyShorePush);
            double warpFrequency = Math.Max(0.00001, config.Water.HydrologyWarpFrequency);
            double warpAmplitude = Math.Clamp(config.Water.HydrologyWarpAmplitude, 0.0, 32.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - seaLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / clampRange, 0.0, 1.0);
                    double shoreBoost = Math.Exp(-distance / shorePush);
                    double slope = ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slope * (slopeWeight + slopePenaltyWeight * 0.1) / 6.0, 0.0, 0.7);
                    double gradientDamp = 1.0 - Math.Clamp(slope * gradientWeight / Math.Max(1.0, config.Water.HydrologyGradientClamp * 8.0), 0.0, 0.35);
                    double warp = SimplexNoise.Generate(
                        (x + 17) * warpFrequency,
                        (z + 31) * warpFrequency,
                        1.0,
                        2,
                        warpAmplitude * 0.15,
                        0.6,
                        (int)(worldSettings.WorldSeed ^ 0x6611));

                    double baseline = Math.Clamp(waterBias * clampWeight * stability * gradientDamp, 0.0, 1.0);
                    baseline = Math.Clamp(baseline + shoreBoost * 0.05 + warp * 0.05, 0.0, 1.25);
                    hydrology[x, z] = (float)baseline;
                }
            }

            if (varianceBlend > 0.0)
            {
                BlendInterior(hydrology, varianceBlend);
            }

            Smooth2D(hydrology, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            DirectionalSmooth(heightMap, hydrology, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            ApplyGradientStability(hydrology, config.Water.HydrologyGradientStabilityIterations, config.Water.HydrologyGradientStabilityBlend, config.Water.HydrologyGradientClamp);
            RelaxEdges(hydrology, config.Water.HydrologyEdgeNormalizationIterations, config.Water.HydrologyEdgeNormalizationBlend);
            RelaxEdges(hydrology, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    hydrology[x, z] = (float)Math.Clamp(hydrology[x, z], 0.0, varianceClamp);
                }
            }

            return hydrology;
        }

        private float[,] BuildFlowMask(int[,] heightMap, float[,] hydrology)
        {
            var flow = new float[chunkSize, chunkSize];
            double persistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double divergenceClamp = Math.Clamp(config.Water.HydrologyFlowDivergenceClamp, 0.1, 2.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    double current = heightMap[x, z];
                    double lowest = current;
                    double accumulation = 0.0;

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
                            if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                            {
                                continue;
                            }

                            double neighbour = heightMap[nx, nz];
                            if (neighbour < lowest)
                            {
                                lowest = neighbour;
                            }

                            if (neighbour < current)
                            {
                                accumulation += (current - neighbour) * 0.25;
                            }
                        }
                    }

                    double hydrologyBoost = hydrology[x, z] * config.Water.HydrologyFlowGain;
                    double scaled = (accumulation * (1.0 - persistence) + hydrologyBoost) * (1.0 + hydrology[x, z] * continuityWeight);
                    scaled *= 1.0 - Math.Clamp((current - lowest) * 0.01 * config.Water.HydrologyGradientSlopeWeight, 0.0, 0.35);
                    flow[x, z] = (float)Math.Clamp(scaled, 0.0, divergenceClamp * 12.0);
                }
            }

            Smooth2D(flow, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            ApplyFlowMemoryWeight(flow, config.Water.HydrologyFlowMemoryWeight, config.Water.HydrologyFlowDivergenceClamp);
            DirectionalSmooth(heightMap, flow, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            ApplyGradientStability(flow, config.Water.HydrologyGradientStabilityIterations, config.Water.HydrologyGradientStabilityBlend, config.Water.HydrologyGradientClamp);
            RelaxEdges(flow, config.Water.HydrologyEdgeNormalizationIterations, config.Water.HydrologyEdgeNormalizationBlend);
            RelaxEdges(flow, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return flow;
        }

        private void BlendHydrologyWithFlow(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double flowBlend = Math.Clamp(config.Water.HydrologyContinuityWeight * 0.35, 0.05, 0.45);
            double edgeBlend = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight * 0.5, 0.0, 0.45);
            int edgeRadius = Math.Max(1, Math.Max(config.Water.HydrologyEdgeBlendRadius, config.Water.HydrologyWatershedStitchRadius));
            double confluenceBoost = Math.Clamp(config.Water.RiverConfluenceBoost, 0.0, 2.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double interiorBlend = Math.Clamp(config.Water.HydrologySeamRelaxBlend * 0.15, 0.0, 0.5);
            double directionalBlend = Math.Clamp(config.Water.HydrologyDirectionalBlend * 0.5, 0.0, 0.5);

            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydrology[x, z];
                    double flowValue = flow[x, z];
                    double normalizedFlow = flowValue / Math.Max(1.0, config.Water.RiverDepth);
                    double neighbourFlow = SampleInterior(flow, x, z) / Math.Max(1.0, config.Water.RiverDepth);
                    double neighbourHydro = SampleInterior(hydrology, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double blend = Math.Clamp(flowBlend + edgeBlend * edgeFalloff, 0.0, 0.9);

                    var downhill = ComputeDownhillDirection(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.dx, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.dz, 0, sizeZ - 1);
                    double directionalHydro = hydrology[downX, downZ];
                    double directionalFlow = flow[downX, downZ] / Math.Max(1.0, config.Water.RiverDepth);
                    double directionalWeight = Math.Clamp((Math.Abs(downhill.dx) + Math.Abs(downhill.dz)) * directionalBlend + directionalFlow * 0.2, 0.0, 0.45);

                    double flowShadow = Math.Clamp(
                        (normalizedFlow + neighbourFlow) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5 +
                        directionalFlow * flowShadowWeight * 0.15,
                        0.0,
                        0.7);

                    double confluence = confluenceBoost > 0.0
                        ? (neighbourFlow * 0.5 + neighbourHydro * 0.25 + hydrologyGradient * 0.15) * confluenceBoost
                        : 0.0;

                    double blended = hydro * (1.0 - blend) + normalizedFlow * blend;
                    blended = blended * (1.0 - flowShadow * 0.35) + neighbourHydro * flowShadow * 0.35;
                    blended = blended * (1.0 - directionalWeight) + directionalHydro * directionalWeight;
                    blended *= 1.0 + confluence;

                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, varianceClamp);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
            Smooth2D(hydrology, 1, Math.Clamp(config.Water.HydrologySeamRelaxBlend * 0.35, 0.0, 0.9));
            RelaxEdges(hydrology, Math.Max(1, config.Water.HydrologyEdgeNormalizationIterations), Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0));
            BlendInterior(hydrology, interiorBlend);
        }

        private void ApplyHydrologyEdgeEnvelope(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            double normalization = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double stabilityBoost = 1.0 + Math.Clamp(config.Water.HydrologyEdgeStabilityIterations * 0.05, 0.0, 0.3);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= edgeRadius)
                    {
                        continue;
                    }

                    double edgeWeight = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double flowValue = flow[x, z];
                    double interiorHydro = SampleInterior(hydrology, x, z);
                    double interiorFlow = SampleInterior(flow, x, z);
                    double seamMemory = (hydro + interiorHydro + flowValue + interiorFlow) * 0.25;
                    double gradient = Math.Abs(interiorHydro - hydro) + Math.Abs(interiorFlow - flowValue) * 0.35;
                    double stability = 1.0 - Math.Clamp(gradient * config.Water.HydrologyEdgeVarianceClamp * 0.5, 0.0, 0.85);
                    double seamAnchor = (hydro + interiorHydro + flowValue * 0.5 + interiorFlow * 0.5) / 3.0;
                    double targetHydro = hydro * (1.0 - edgeWeight * 0.25) + seamAnchor * edgeWeight * (0.65 + continuityWeight * 0.35);
                    targetHydro += interiorFlow * memoryWeight * 0.05;
                    targetHydro = targetHydro * (1.0 - normalization * 0.25) + seamMemory * normalization * 0.25;
                    hydrology[x, z] = (float)Math.Clamp(targetHydro * stability * stabilityBoost, 0.0, varianceClamp);

                    double targetFlow = flowValue * (1.0 - edgeWeight * 0.25) + Math.Max(flowValue, interiorFlow) * edgeWeight;
                    targetFlow += seamAnchor * memoryWeight * 0.1;
                    targetFlow = targetFlow * (1.0 - normalization * 0.25) + (seamMemory + interiorFlow) * normalization * 0.25;
                    flow[x, z] = (float)Math.Clamp(targetFlow * stability * stabilityBoost, 0.0, flowClamp + 2.0);
                }
            }
        }

        private void ApplyHydrologyReservoirSmoothing(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int iterations = Math.Max(0, config.Water.HydrologyReservoirIterations);
            double blend = Math.Clamp(config.Water.HydrologyReservoirBlend, 0.0, 1.0);
            if (iterations <= 0 || blend <= 0.0 || flow == null)
            {
                return;
            }

            var hydroBuffer = new float[sizeX, sizeZ];
            var flowBuffer = new float[sizeX, sizeZ];
            double clampRange = Math.Max(1.0, config.Water.HydrologyWaterTableClampRange);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int surface = heightMap[x, z];
                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        double neighbourHydro = SampleInterior(hydrology, x, z);
                        double neighbourFlow = SampleInterior(flow, x, z);
                        double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        double edgeAttenuation = 1.0 - Math.Clamp(edgeDistance / (edgeRadius * 1.5), 0.0, 1.0);
                        double waterDepth = Math.Max(0, config.Water.GlobalWaterLevel - surface);
                        double waterClamp = Math.Clamp(waterDepth / clampRange, 0.0, 1.0);
                        double reservoirBlend = blend * (0.65 + edgeAttenuation * 0.35) * (0.65 + waterClamp * 0.35);
                        double hydroTarget = hydro * (1.0 - reservoirBlend) + neighbourHydro * reservoirBlend;
                        double flowTarget = flowValue * (1.0 - reservoirBlend * 0.65) + neighbourFlow * reservoirBlend * 0.65;
                        hydroBuffer[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.35));
                        flowBuffer[x, z] = TerrainMaskUtility.Clamp01(
                            (float)Math.Clamp(flowTarget, 0.0, Math.Max(flowValue + 1.0, config.Water.HydrologyFlowDivergenceClamp * 12.0)));
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydrology.Length);
                Array.Copy(flowBuffer, flow, flow.Length);
            }
        }

        private void StabilizeHydrologyFields(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            if (hydrology == null || flow == null)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var water = config.Water;
            double varianceBlend = Math.Clamp(water.HydrologyVarianceBlend, 0.0, 1.0);
            double varianceClamp = Math.Max(0.0, water.HydrologyVarianceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float moisture = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);

                    float variance = (float)Math.Abs(moisture - neighbourHydro);
                    float flowVariance = (float)Math.Abs(flowValue - neighbourFlow);
                    float variancePenalty = (float)Math.Clamp(
                        variance * varianceClamp + flowVariance * water.HydrologyEdgeVarianceClamp,
                        0.0,
                        1.0);

                    float blendedHydro = (float)Math.Clamp(
                        (moisture + neighbourHydro) * 0.5 * varianceBlend +
                        moisture * (1.0 - varianceBlend),
                        0.0,
                        1.0);
                    double waterTableBias = Math.Clamp(
                        (water.GlobalWaterLevel - heightMap[x, z]) / Math.Max(1.0, water.HydrologyWaterTableClampRange),
                        -1.0,
                        1.0);
                    blendedHydro = (float)Math.Clamp(
                        blendedHydro + waterTableBias * water.HydrologyWaterTableClampWeight,
                        0.0,
                        1.0);
                    hydrology[x, z] = (float)Math.Clamp(
                        blendedHydro * (1.0 - variancePenalty * 0.5) +
                        moisture * water.HydrologySmoothBlend * 0.05,
                        0.0,
                        1.0);

                    float memory = (float)Math.Clamp(
                        water.HydrologyFlowMemoryWeight * neighbourFlow +
                        (1.0 - water.HydrologyFlowMemoryWeight) * flowValue,
                        0.0,
                        1.0);
                    float persistentFlow = (float)Math.Clamp(
                        flowValue * water.HydrologyFlowPersistence + memory,
                        0.0,
                        1.0);
                    flow[x, z] = (float)Math.Clamp(
                        persistentFlow * (1.0 - variancePenalty * water.HydrologyEdgeFluxBlend) +
                        hydrology[x, z] * water.HydrologyEdgeFluxBlend * 0.5,
                        0.0,
                        1.0);
                }
            }
        }

        private void NormalizeHydrologyFlowEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            int iterations = Math.Max(1, config.Water.HydrologyEdgeNormalizationIterations);
            double blendBase = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            double seamLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double edgeVarianceClamp = Math.Max(0.001, config.Water.HydrologyEdgeVarianceClamp);

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
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
                        double blend = blendBase * edgeFalloff;
                        if (blend <= 0.0)
                        {
                            continue;
                        }

                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        double neighbourHydro = SampleInterior(hydrology, x, z);
                        double neighbourFlow = SampleInterior(flow, x, z);
                        double gradient = Math.Abs(neighbourHydro - hydro);
                        double flowGradient = Math.Abs(neighbourFlow - flowValue);
                        double stability = 1.0 - Math.Clamp((gradient + flowGradient) * edgeVarianceClamp * 0.5, 0.0, 0.85);
                        double continuityBoost = Math.Clamp((hydro + neighbourHydro + flowValue + neighbourFlow) * 0.25 * continuityWeight, 0.0, 0.8);
                        double seamAnchor = (neighbourHydro + hydro) * 0.5 + neighbourFlow * memoryWeight * 0.25;
                        double seamMemory = seamAnchor * (seamLock * edgeFalloff + continuityBoost * 0.35);

                        double targetHydro = (neighbourHydro * (1.0 + memoryWeight * 0.35) + hydro * 0.65 + flowValue * memoryWeight * 0.15 + continuityBoost * 0.35) / (1.8 + memoryWeight * 0.35 + continuityWeight * 0.35);
                        targetHydro = (targetHydro + seamAnchor * 0.25 + seamMemory * 0.35) / (1.25 + seamLock * 0.15);
                        hydroBuffer[x, z] = (float)Math.Clamp(hydro + (targetHydro - hydro) * blend * stability, 0.0, varianceClamp);

                        double targetFlow = (neighbourFlow * (1.0 + memoryWeight) + flowValue + hydro * memoryWeight * 0.35 + seamMemory * 0.25) / (2.0 + memoryWeight);
                        targetFlow = (targetFlow * stability + seamAnchor * (0.05 + seamLock * 0.1)) / (1.0 + Math.Max(0.1, flowGradient * edgeVarianceClamp * 0.35));
                        flowBuffer[x, z] = (float)Math.Clamp(flowValue + (targetFlow - flowValue) * (blend + continuityBoost * 0.35), 0.0, Math.Max(flowValue + 1.5, flowClamp));
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }
        }

        private void ApplyWaterTableEnvelope(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double clampRange = Math.Max(1.0, config.Water.HydrologyWaterTableClampRange + 6.0);
            double envelopeWeight = Math.Clamp(config.Water.HydrologyWaterTableClampWeight + 0.08, 0.0, 1.0);
            double seamBlend = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyEdgeVarianceClamp);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int surface = heightMap[x, z];
                    double waterBias = 1.0 - Math.Clamp(Math.Abs(surface - seaLevel) / clampRange, 0.0, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double seamWeight = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double blend = envelopeWeight * (0.6 * waterBias + 0.4 * seamWeight);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double neighbourHydro = SampleInterior(hydrology, x, z);
                    double neighbourFlow = SampleInterior(flow, x, z);
                    double stability = 1.0 - Math.Clamp(Math.Abs(surface - seaLevel) / (clampRange * 1.25), 0.0, 0.65);

                    double targetHydro = hydro * (1.0 - blend) + (hydro + neighbourHydro * (1.0 + seamWeight * seamBlend)) * 0.5 * blend;
                    targetHydro *= 1.0 + waterBias * 0.12;
                    targetHydro *= stability;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(targetHydro, 0.0, varianceClamp + 0.75));

                    double flowValue = flow[x, z];
                    double targetFlow = flowValue * (1.0 + waterBias * 0.1) + neighbourFlow * (0.15 + seamWeight * seamBlend * 0.25);
                    double flowBlend = Math.Clamp(blend + seamBlend * 0.15, 0.0, 1.0);
                    double blendedFlow = flowValue + (targetFlow - flowValue) * flowBlend;
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(blendedFlow, 0.0, flowClamp + 2.0));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, seamBlend * 0.85, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, seamBlend * 0.65, varianceClamp * 1.25);
        }

        private void ApplyLakeHydrologySeepage(int[,] heightMap, float[,] hydrologyMask, float[,] flowMask, float[,] lakeMask, float[,]? riverMask)
        {
            double seepageWeight = Math.Clamp(config.Lakes.FlowSeepageWeight, 0.0, 1.0);
            double inflowBlend = Math.Clamp(config.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(config.Water.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            int sizeX = hydrologyMask.GetLength(0);
            int sizeZ = hydrologyMask.GetLength(1);

            var hydroCopy = (float[,])hydrologyMask.Clone();
            var flowCopy = (float[,])flowMask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lake = lakeMask[x, z];
                    if (lake <= 0.01f)
                    {
                        continue;
                    }

                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double flowMemory = TerrainMaskUtility.SampleInterior(flowCopy, x, z) / 6.0;
                    double hydroBase = hydroCopy[x, z];
                    double infiltration = lake * (seepageWeight * 0.75 + inflowBlend * 0.35);
                    double slopeGuard = 1.0 - Math.Clamp(TerrainMaskUtility.ComputeSlope(heightMap, x, z) * config.Water.HydrologySlopePenalty / 18.0, 0.0, 0.6);
                    double riverGuard = 1.0 - river * 0.35;
                    double hydroTarget = hydroBase + infiltration * slopeGuard * riverGuard + flowMemory * inflowBlend * 0.35;
                    hydrologyMask[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.25));

                    double flowTarget = flowCopy[x, z] * (1.0 - lake * 0.2) + hydrologyMask[x, z] * (seepageWeight * 0.35 + inflowBlend * 0.25);
                    flowMask[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(flowTarget + lake * 0.05, 0.0, 1.1));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrologyMask, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flowMask, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.85, varianceClamp * 1.35);
        }

        private void ApplyHydrologyEnvelope(float[,]? riverMask, float[,]? lakeMask, float[,] hydrologyMask, float[,] flowMask)
        {
            if (riverMask == null && lakeMask == null)
            {
                return;
            }

            int sizeX = hydrologyMask.GetLength(0);
            int sizeZ = hydrologyMask.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double seamLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double edgeVarianceClamp = Math.Max(0.001, config.Water.HydrologyEdgeVarianceClamp);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrologyMask[x, z];
                    float flow = flowMask[x, z];
                    float neighbourHydro = SampleInterior(hydrologyMask, x, z);
                    float neighbourFlow = SampleInterior(flowMask, x, z);
                    float hydrologyGradient = Math.Abs(neighbourHydro - hydro);
                    float flowGradient = Math.Abs(neighbourFlow - flow);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float seam = 1f - Math.Clamp(edgeDistance / (float)(edgeRadius + 1), 0f, 1f);
                    float wetAnchor = Math.Max(hydro, neighbourHydro);
                    float flowAnchor = Math.Max(flow, neighbourFlow);
                    float continuityBoost = (float)(continuityWeight * (wetAnchor * 0.35f + flowAnchor * 0.65f));
                    float seamBoost = (float)(seam * seamLock * 0.5);
                    float seamContinuity = seamBoost + continuityBoost * 0.5f;
                    float stability = 1f - Math.Clamp(hydrologyGradient * (float)edgeVarianceClamp + flowGradient * 0.25f, 0f, 0.9f);
                    float envelopeBlend = Math.Clamp(seamContinuity + continuityBoost * 0.25f, 0f, 1.1f);
                    float flowShadow = Math.Clamp(flow * (float)flowShadowWeight + hydrologyGradient * (float)flowShadowSlopeWeight, 0f, 1f);
                    float envelope = wetAnchor;

                    if (riverMask != null)
                    {
                        float current = riverMask[x, z];
                        float neighbour = SampleInterior(riverMask, x, z);
                        float target = Math.Max(current, neighbour * 0.6f + wetAnchor * 0.25f + flowAnchor * 0.2f);
                        float gradientPenalty = Math.Clamp(hydrologyGradient * (float)edgeVarianceClamp, 0f, 0.65f);
                        target = target * (1f + seamBoost) + continuityBoost * 0.5f + (float)(memoryWeight * neighbourFlow * 0.05f);
                        target *= 1f - gradientPenalty * 0.3f;
                        riverMask[x, z] = (float)Math.Clamp(target, 0.0, varianceClamp);
                        envelope = Math.Max(envelope, riverMask[x, z]);
                    }

                    if (lakeMask != null)
                    {
                        float currentLake = lakeMask[x, z];
                        float neighbourLake = SampleInterior(lakeMask, x, z);
                        float basin = Math.Max(currentLake, neighbourLake);
                        float targetLake = basin + wetAnchor * 0.35f + continuityBoost * 0.35f + flowAnchor * 0.15f;
                        targetLake *= 1f + seamBoost * 0.5f;
                        float gradientPenalty = Math.Clamp(hydrologyGradient * (float)edgeVarianceClamp, 0f, 0.6f);
                        targetLake *= 1f - gradientPenalty * 0.25f;
                        lakeMask[x, z] = (float)Math.Clamp(targetLake, 0.0, varianceClamp + 0.35);
                        envelope = Math.Max(envelope, lakeMask[x, z]);
                    }

                    float targetHydrology = Math.Max(hydro, envelope * (0.45f + (float)memoryWeight * 0.35f) + wetAnchor * 0.25f + flowAnchor * 0.15f);
                    targetHydrology = Math.Clamp(targetHydrology * (1f - flowShadow * 0.25f), 0f, (float)varianceClamp);
                    hydrologyMask[x, z] = (float)Math.Clamp(
                        hydrologyMask[x, z] * (1f - envelopeBlend * 0.25f) + targetHydrology * envelopeBlend * stability,
                        0.0,
                        varianceClamp);

                    float targetFlow = Math.Max(flow, flowAnchor + envelope * (float)memoryWeight * 0.25f + continuityBoost * 0.35f);
                    targetFlow = Math.Clamp(targetFlow * (1f - flowShadow * 0.15f) + wetAnchor * 0.05f, 0f, (float)flowClamp + 2f);
                    flowMask[x, z] = (float)Math.Clamp(
                        flowMask[x, z] * (1f - envelopeBlend * 0.35f) + targetFlow * (envelopeBlend + 0.15f) * stability,
                        0.0,
                        Math.Max(flow + 1.5, (float)flowClamp));
                }
            }

            if (riverMask != null)
            {
                RelaxEdges(riverMask, Math.Max(1, config.Water.HydrologyEdgeStabilityIterations), config.Water.HydrologyEdgeNormalizationBlend * 0.5);
            }

            if (lakeMask != null)
            {
                RelaxEdges(lakeMask, Math.Max(1, config.Water.HydrologyEdgeStabilityIterations), config.Water.HydrologySeamRelaxBlend * 0.5);
            }
        }

        private void PaintBaseTerrain(ChunkData chunk, int[,] heightMap)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int columnHeight = heightMap[x, z];
                    int biomeNoise = (int)(PerlinNoise.Generate(
                        chunk.ChunkX * chunkSize + x,
                        chunk.ChunkZ * chunkSize + z,
                        0.0022,
                        3,
                        1.0,
                        0.5,
                        (int)worldSettings.WorldSeed ^ 0xAB12) * 100);

                    BiomeType biome = ResolveBiome(columnHeight, biomeNoise);
                    chunk.SetBiome(x, z, biome);

                    for (int y = 0; y < worldHeight; y++)
                    {
                        if (y == 0 || y <= bedrockLevel)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Bedrock);
                            continue;
                        }

                        if (y > columnHeight)
                        {
                            chunk.SetBlock(x, y, z, y <= seaLevel ? BlockType.Water : BlockType.Air);
                            continue;
                        }

                        // Top block
                        if (y == columnHeight)
                        {
                            chunk.SetBlock(x, y, z, SelectSurfaceBlock(biome, columnHeight));
                            continue;
                        }

                        // Sub-surface
                        if (y >= columnHeight - 3)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Dirt);
                        }
                        else
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }
            }
        }

        private bool[,,] BuildCaveMask(int chunkX, int chunkZ, int[,] heightMap, float[,]? hydrologyMask, float[,]? flowMask, float[,]? riverMask)
        {
            if (hydrologyMask == null || flowMask == null)
            {
                return BuildLegacyCaveMask(chunkX, chunkZ, heightMap);
            }

            var mask = new bool[chunkSize, worldHeight, chunkSize];
            var caves = config.Caves;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    float hydrology = hydrologyMask[x, z];
                    float flow = flowMask[x, z];
                    float neighbourHydro = SampleInterior(hydrologyMask, x, z);
                    float neighbourFlow = SampleInterior(flowMask, x, z);
                    float hydrologyGradient = Math.Abs(neighbourHydro - hydrology);
                    float flowGradient = Math.Abs(neighbourFlow - flow);
                    float river = riverMask != null ? riverMask[x, z] : 0f;
                    float hydrologyEnvelope = Math.Min(1f, Math.Max(hydrology, neighbourHydro) + flow * 0.35f + hydrologyGradient * 0.35f);
                    float seamGuard = 1f - Math.Clamp(hydrologyGradient * (float)config.Water.HydrologyEdgeStabilityWeight, 0f, 0.55f);
                    double terrainRelief = ComputeLocalRelief(heightMap, x, z);
                    double basinPotential = ComputeBasinPotential(heightMap, x, z, seaLevel);
                    double groundwaterCoupling = Math.Clamp(
                        (hydrologyEnvelope * 0.55 + flow * 0.45 + basinPotential * 0.35) * caves.GroundwaterConnectivityWeight,
                        0.0,
                        1.35);
                    double ventilationBias = Math.Clamp(
                        caves.CaveVentilationBias + terrainRelief * 0.18 - groundwaterCoupling * 0.08,
                        0.0,
                        1.5);

                    for (int y = bedrockLevel + 1; y < Math.Min(worldHeight - 4, heightMap[x, z]); y++)
                    {
                        double warpFrequency = Math.Clamp(caves.HorizontalFrequency * 0.55, 0.0005, 0.01);
                        var warp = SimplexNoise.DomainWarp(worldX, y + worldZ, warpFrequency, caves.VerticalFrequency, 8.0, 5.0, (int)worldSettings.WorldSeed ^ 0xA5A5);

                        double primary = SimplexNoise.Generate(
                            worldX + warp.dx,
                            worldZ + warp.dz + y * caves.VerticalFrequency,
                            caves.HorizontalFrequency,
                            3,
                            1.0,
                            0.5,
                            (int)worldSettings.WorldSeed ^ 0x33AA);

                        double moistureBias = (double)(seaLevel - y) / seaLevel;
                        double stabilityPenalty = hydrologyEnvelope * caves.HydrologyStabilityWeight
                            + flow * caves.FlowStabilityWeight
                            + hydrologyGradient * caves.RoughnessStabilityWeight
                            + flowGradient * caves.RoughnessStabilityWeight * 0.35
                            + river * caves.RiverSuppressionWeight * 0.65;
                        double ventilationRelief = Math.Clamp(
                            ventilationBias * (1.0 - Math.Clamp(hydrologyEnvelope * config.Water.HydrologyEdgeFlowLockWeight, 0.0, 0.8)),
                            0.0,
                            1.0);
                        double depthRatio = (double)(y - bedrockLevel) / Math.Max(1.0, seaLevel - bedrockLevel);
                        double threshold = caves.Threshold
                            + moistureBias * caves.MoistureRetentionWeight * 0.5
                            + stabilityPenalty * 0.35
                            + groundwaterCoupling * 0.14
                            + depthRatio * caves.CeilingStabilityWeight * 0.25
                            + hydrologyEnvelope * (1.0 - ventilationRelief) * 0.25
                            + basinPotential * caves.CeilingMoistureWeight * 0.08
                            - ventilationRelief * 0.12
                            + flowGradient * caves.EdgeSealStrength * 0.1;

                        bool edgeBlocked = (hydrologyGradient > 0.6f || flowGradient > 0.6f || groundwaterCoupling > 1.1) && IsEdge(x, z);
                        if (!edgeBlocked && seamGuard > 0.05f && primary > threshold - ventilationRelief * 0.08)
                        {
                            mask[x, y, z] = true;
                        }
                    }
                }
            }

            StabilizeCaveMask(mask, heightMap, caves.StabilitySmoothIterations, caves.StabilitySmoothBlend);
            return mask;
        }

        private bool[,,] BuildLegacyCaveMask(int chunkX, int chunkZ, int[,] heightMap)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            var caves = config.Caves;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    for (int y = bedrockLevel + 1; y < Math.Min(worldHeight - 4, heightMap[x, z]); y++)
                    {
                        double warpFrequency = Math.Clamp(caves.HorizontalFrequency * 0.55, 0.0005, 0.01);
                        var warp = SimplexNoise.DomainWarp(worldX, y + worldZ, warpFrequency, caves.VerticalFrequency, 8.0, 5.0, (int)worldSettings.WorldSeed ^ 0xA5A5);

                        double primary = SimplexNoise.Generate(
                            worldX + warp.dx,
                            worldZ + warp.dz + y * caves.VerticalFrequency,
                            caves.HorizontalFrequency,
                            3,
                            1.0,
                            0.5,
                            (int)worldSettings.WorldSeed ^ 0x33AA);

                        double moistureBias = (double)(seaLevel - y) / seaLevel;
                        double threshold = caves.Threshold - (moistureBias * caves.MoistureRetentionWeight);

                        mask[x, y, z] = primary > threshold;
                    }
                }
            }

            StabilizeCaveMask(mask, heightMap, config.Caves.StabilitySmoothIterations, config.Caves.StabilitySmoothBlend);
            return mask;
        }

        private float[,] BuildRiverMask(int chunkX, int chunkZ, int[,] heightMap, float[,]? hydrologyMask, float[,]? flowMask)
        {
            var mask = new float[chunkSize, chunkSize];
            var water = config.Water;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    float hydrology = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;
                    float neighbourHydro = hydrologyMask != null ? SampleInterior(hydrologyMask, x, z) : hydrology;
                    float neighbourFlow = flowMask != null ? SampleInterior(flowMask, x, z) : flow;
                    float hydrologyGradient = hydrologyMask != null ? Math.Abs(neighbourHydro - hydrology) : 0f;
                    float flowGradient = flowMask != null ? Math.Abs(neighbourFlow - flow) : 0f;
                    float hydrologyEnvelope = Math.Min(1f, Math.Max(hydrology, neighbourHydro) + flow * 0.35f + hydrologyGradient * 0.35f);
                    double seamShield = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * water.HydrologyEdgeStabilityWeight * 0.2, 0.0, 0.6);
                    double waterTableDelta = Math.Abs(heightMap[x, z] - water.GlobalWaterLevel);
                    double waterTableClamp = 1.0 - Math.Clamp(waterTableDelta / Math.Max(1.0, water.HydrologyWaterTableClampRange), 0.0, 1.0) * water.HydrologyWaterTableClampWeight;

                    var warp = SimplexNoise.DomainWarp(
                        worldX,
                        worldZ,
                        water.HydrologyWarpFrequency,
                        water.RiverNoiseScale,
                        water.HydrologyWarpAmplitude,
                        9.0,
                        (int)worldSettings.WorldSeed ^ 0x77AA);

                    double baseNoise = SimplexNoise.Generate(
                        worldX + warp.dx,
                        worldZ + warp.dz,
                        water.RiverNoiseScale,
                        3,
                        1.0,
                        0.55,
                        (int)worldSettings.WorldSeed ^ 0x00DD);

                    double meander = SimplexNoise.Generate(
                        worldX + 211,
                        worldZ - 73,
                        Math.Max(0.0001, water.RiverNoiseScale * 0.5),
                        2,
                        1.0,
                        0.6,
                        (int)worldSettings.WorldSeed ^ 0x8844) * water.RiverMeanderJitter;

                    double intensity = Math.Clamp(1.0 - Math.Abs(baseNoise), 0.0, 1.0);
                    intensity = Math.Clamp(intensity + meander, 0.0, 1.0);
                    double slope = ComputeSlope(heightMap, x, z);
                    double continuity = 1.0 - Math.Min(1.0, slope * water.RiverGradientPenalty * 0.01);
                    double anisotropy = ComputeAnisotropy(heightMap, x, z) * water.RiverAnisotropyWeight;
                    double reliefPenalty = Math.Max(0.0, (heightMap[x, z] - seaLevel) * water.RiverReliefPenaltyWeight * 0.01);
                    double flowAlignment = ComputeFlowAlignment(heightMap, x, z) * water.RiverFlowAlignmentWeight;
                    double headwaterStability = 1.0 - Math.Min(1.0, Math.Abs(heightMap[x, z] - seaLevel) * water.RiverHeadwaterStabilityWeight * 0.01);
                    double localRelief = ComputeLocalRelief(heightMap, x, z);
                    double basinPotential = ComputeBasinPotential(heightMap, x, z, seaLevel);
                    double tributaryCapture = Math.Clamp(water.RiverTributaryCaptureWeight, 0.0, 2.0)
                        * (neighbourFlow * 0.35 + hydrologyEnvelope * 0.2 + basinPotential * 0.25);
                    double braiding = Math.Clamp(water.RiverBraidingWeight, 0.0, 2.0)
                        * Math.Clamp(flowGradient * 0.6 + hydrologyGradient * 0.4 + basinPotential * 0.25, 0.0, 1.5) * 0.18;

                    double seamStability = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * water.HydrologyEdgeStabilityWeight * 0.25, 0.0, 0.65);
                    double continuityBoost = (hydrologyEnvelope + neighbourFlow) * water.HydrologyContinuityWeight * 0.15;
                    double curvatureDamp = 1.0 - Math.Clamp(anisotropy * 0.5 + reliefPenalty * 0.35, 0.0, 0.65);
                    double directionalStability = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * water.HydrologyDirectionalBlend * 0.35, 0.0, 0.6);

                    intensity = (intensity * continuity * headwaterStability + flowAlignment + continuityBoost + tributaryCapture + basinPotential * 0.18 + braiding) * curvatureDamp;
                    intensity -= reliefPenalty * 0.35;
                    intensity *= 1.0 - Math.Clamp(localRelief * water.RiverReliefPenaltyWeight * 0.18, 0.0, 0.55);
                    intensity *= 1.0 + hydrologyEnvelope * water.HydrologyContinuityWeight * 0.35;
                    intensity *= seamStability * seamShield * directionalStability;
                    intensity *= Math.Clamp(waterTableClamp, 0.35, 1.0);
                    if (IsEdge(x, z))
                    {
                        intensity *= Math.Max(0.15, 1.0 - water.RiverEdgeFeather);
                    }

                    if (intensity > water.RiverBankThreshold)
                    {
                        mask[x, z] = (float)Math.Clamp(intensity, 0.0, 1.0);
                    }
                }
            }

            Smooth2D(mask, water.RiverIntensitySmoothIterations, water.RiverIntensitySmoothBlend);
            RelaxEdges(mask, water.HydrologyEdgeNormalizationIterations, water.HydrologyEdgeNormalizationBlend);
            RelaxEdges(mask, water.HydrologySeamRelaxIterations, water.HydrologySeamRelaxBlend);
            BoostRiverConfluences(mask, water.RiverConfluenceBoost);
            return mask;
        }

        private float[,] BuildLakeMask(int chunkX, int chunkZ, int[,] heightMap, float[,]? hydrologyMask, float[,]? flowMask, float[,]? riverMask)
        {
            var lakes = new float[chunkSize, chunkSize];
            var lakeConfig = config.Lakes;
            var water = config.Water;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float hydrology = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;
                    float neighbourHydro = hydrologyMask != null ? SampleInterior(hydrologyMask, x, z) : hydrology;
                    float neighbourFlow = flowMask != null ? SampleInterior(flowMask, x, z) : flow;
                    float hydrologyGradient = hydrologyMask != null ? Math.Abs(neighbourHydro - hydrology) : 0f;
                    float flowGradient = flowMask != null ? Math.Abs(neighbourFlow - flow) : 0f;
                    float hydrologyEnvelope = Math.Min(1f, hydrology + flow * 0.35f + hydrologyGradient * 0.35f);
                    double variance = Math.Abs(neighbourHydro - hydrology) + Math.Abs(neighbourFlow - flow);
                    double varianceDamp = 1.0 - Math.Clamp(variance * lakeConfig.VarianceWeight, 0.0, 0.55);
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;
                    float riverNeighbour = riverMask != null ? SampleNeighborhoodMax(riverMask, x, z, 1) : 0f;
                    double basinPotential = ComputeBasinPotential(heightMap, x, z, seaLevel);
                    double seamShield = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * water.HydrologyEdgeStabilityWeight * 0.2, 0.0, 0.55);
                    double waterTableDelta = Math.Abs(heightMap[x, z] - water.GlobalWaterLevel);
                    double waterTableBias = Math.Max(0.0, 1.0 - waterTableDelta / Math.Max(1.0, water.HydrologyWaterTableClampRange));
                    double waterTableWeight = waterTableBias * water.HydrologyWaterTableClampWeight;
                    double waterTablePenalty = Math.Max(0.0, (waterTableDelta - water.HydrologyWaterTableClampRange) * 0.01 * water.HydrologyWaterTableSlopeWeight);
                    double reservoirMemory = ((hydrology + neighbourHydro) * 0.5 + (flow + neighbourFlow) * 0.25) * water.HydrologyReservoirBlend;

                    double noise = SimplexNoise.Generate(
                        worldX,
                        worldZ,
                        0.008,
                        3,
                        1.0,
                        0.55,
                        (int)worldSettings.WorldSeed ^ 0x0B0B);

                    double basin = SimplexNoise.Generate(
                        worldX + 91.0,
                        worldZ + 37.0,
                        0.004,
                        2,
                        1.0,
                        0.6,
                        (int)worldSettings.WorldSeed ^ 0x99);

                    double weight = (noise * 0.6) + (basin * 0.4) + lakeConfig.SpawnWeightBias;
                    double inflow = riverMask != null ? riverMask[x, z] * water.LakeInflowBlendWeight : 0.0;
                    double tributaryInflow = riverNeighbour * water.RiverTributaryCaptureWeight * 0.35;
                    double riverSuppression = riverMask != null ? riverMask[x, z] * lakeConfig.RiverProximitySuppression : 0.0;
                    double spillRetentionBoost = lakeConfig.SpillRetentionWeight * (0.2 + basinPotential * 0.45 + riverNeighbour * 0.35);
                    double spillwayContinuity = lakeConfig.SpillwayContinuityWeight * (0.12 + flow * 0.22 + riverNeighbour * 0.25);
                    double altitudePenalty = Math.Max(0, seaLevel - heightMap[x, z]) * 0.0025;
                    double slopePenalty = ComputeSlope(heightMap, x, z) * water.LakeRimErosionWeight * 0.05;
                    double stabilityPenalty = (hydrologyGradient + flowGradient) * water.HydrologyEdgeStabilityWeight * 0.1;
                    double edgeDamp = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * water.HydrologyEdgeVarianceClamp * 0.5, 0.0, 0.45);
                    weight += inflow + tributaryInflow + hydrologyEnvelope * lakeConfig.FlowSeepageWeight + flow * water.LakeInflowBlendWeight * 0.25 + waterTableWeight + reservoirMemory + spillRetentionBoost + spillwayContinuity;
                    weight -= riverSuppression * (1.0 - lakeConfig.SpillRetentionWeight * 0.35) + altitudePenalty + slopePenalty + stabilityPenalty + waterTablePenalty;
                    weight *= varianceDamp * edgeDamp;

                    double wetlandThreshold = lakeConfig.WetlandSaturationThreshold - inflow * 0.08 - tributaryInflow * 0.05 - reservoirMemory * 0.1 - spillRetentionBoost * 0.08 - spillwayContinuity * 0.05;
                    weight *= Math.Max(0.25, seamShield);
                    if (weight > wetlandThreshold && heightMap[x, z] > bedrockLevel + lakeConfig.MinDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            Smooth2D(lakes, lakeConfig.LakeBasinSmoothIterations, config.Water.HydrologySmoothBlend);
            RelaxEdges(lakes, config.Water.HydrologyEdgeNormalizationIterations, config.Water.HydrologyEdgeNormalizationBlend);
            RelaxEdges(lakes, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            ApplyWetlandBuffer(lakes, lakeConfig.WetlandBufferRadius, lakeConfig.ShorelineBlend);
            return lakes;
        }

        private void CarveCaves(ChunkData chunk, bool[,,] mask, int[,] heightMap, float[,]? hydrologyMask, float[,]? flowMask)
        {
            var caves = config.Caves;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float hydrology = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;
                    float neighbourHydro = hydrologyMask != null ? SampleInterior(hydrologyMask, x, z) : hydrology;
                    float neighbourFlow = flowMask != null ? SampleInterior(flowMask, x, z) : flow;
                    float hydrologyGradient = hydrologyMask != null ? Math.Abs(neighbourHydro - hydrology) : 0f;
                    float hydrologyEnvelope = Math.Min(1f, Math.Max(hydrology, neighbourHydro) + flow * 0.25f + hydrologyGradient * 0.35f);
                    double terrainRelief = ComputeLocalRelief(heightMap, x, z);
                    double basinPotential = ComputeBasinPotential(heightMap, x, z, seaLevel);
                    float moistureBudget = Math.Clamp(hydrologyEnvelope + neighbourHydro * 0.25f + neighbourFlow * 0.1f, 0f, (float)caves.MoistureFlowClamp);
                    double groundwaterCoupling = Math.Clamp(
                        caves.GroundwaterConnectivityWeight * (hydrologyEnvelope * 0.6 + flow * 0.4 + basinPotential * 0.35),
                        0.0,
                        1.35);
                    double ventilationBias = Math.Clamp(caves.CaveVentilationBias + terrainRelief * 0.2 - groundwaterCoupling * 0.08, 0.0, 1.5);
                    double stabilityPenalty = moistureBudget * caves.HydrologyStabilityWeight + flow * caves.FlowStabilityWeight + hydrologyGradient * caves.RoughnessStabilityWeight;
                    stabilityPenalty *= 1.0 + caves.RiparianCaveGuardWeight * hydrologyGradient * 0.25;
                    double seamPenalty = (hydrologyGradient + hydrologyEnvelope * 0.5f) * config.Water.HydrologyEdgeStabilityWeight * caves.RiverSuppressionWeight * 0.5;
                    double ventilationRelief = Math.Clamp(ventilationBias * (1.0 - Math.Clamp(hydrologyEnvelope * caves.CaveEntranceFlowDampening, 0.0, 0.85)), 0.0, 1.0);
                    double waterTableDelta = Math.Abs(heightMap[x, z] - config.Water.GlobalWaterLevel);
                    double waterTableClamp = 1.0 - Math.Clamp(waterTableDelta / Math.Max(1.0, config.Water.HydrologyWaterTableClampRange), 0.0, 1.0);
                    double supportBias = Math.Clamp(caves.SupportDensity + hydrologyEnvelope * caves.SupportHydrationBias + flow * caves.SupportFlowBias, 0.0, 1.25);
                    double riparianGuard = Math.Clamp(caves.RiparianCaveGuardWeight * moistureBudget, 0.0, 1.0);

                    for (int y = bedrockLevel + 1; y < Math.Min(worldHeight - 4, heightMap[x, z]); y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        if (caves.RiparianPlugDepth > 0 && hydrologyEnvelope > 0.6f && y >= Math.Max(1, seaLevel - caves.RiparianPlugDepth))
                        {
                            continue;
                        }

                        double depthRatio = (double)(y - bedrockLevel) / Math.Max(1.0, seaLevel - bedrockLevel);
                        double ceilingPenalty = Math.Clamp(depthRatio * caves.CeilingStabilityWeight, 0.0, caves.CeilingStabilityWeight);
                        double moisturePenalty = Math.Clamp(moistureBudget * caves.MoistureRetentionWeight + flow * caves.FlowStabilityWeight * 0.5f, 0.0, 0.95);
                        double waterTableStability = Math.Clamp(waterTableClamp * config.Water.HydrologyWaterTableSlopeWeight * 0.2, 0.0, 0.45);
                        double supportPenalty = Math.Clamp(1.0 - supportBias, 0.0, 0.4);
                        double stability = stabilityPenalty + ceilingPenalty + moisturePenalty + seamPenalty + waterTableStability + supportPenalty + groundwaterCoupling * 0.18 - ventilationRelief * 0.12;
                        stability += riparianGuard * 0.25;

                        if ((stability > 0.9 && random.NextDouble() < stability * 0.5) || (ventilationRelief < 0.25 && random.NextDouble() < 0.35))
                        {
                            continue;
                        }

                        bool isEdge = x == 0 || z == 0 || x == chunkSize - 1 || z == chunkSize - 1;
                        if (isEdge && random.NextDouble() < config.Caves.EdgeSealStrength * (1.0 + hydrologyEnvelope * 0.15))
                        {
                            continue;
                        }

                        if ((hydrologyGradient > 0.4f || seamPenalty > 0.35) && IsEdge(x, z))
                        {
                            continue;
                        }

                        bool preserveAquiferBarrier = groundwaterCoupling > 0.85 &&
                            y < seaLevel - 10 &&
                            random.NextDouble() < caves.AquiferBarrierWeight * 0.25;
                        if (preserveAquiferBarrier)
                        {
                            continue;
                        }

                        bool nearBottom = y < bedrockLevel + 6;
                        if (nearBottom && random.NextDouble() < caves.LavaThreshold)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Lava);
                            continue;
                        }

                        double ventilationFloodDamp = Math.Clamp(1.0 - ventilationBias * 0.35, 0.45, 1.0);
                        bool flooded = y < seaLevel - 6 &&
                            (random.NextDouble() < caves.WaterThreshold * ventilationFloodDamp * (1.0 + moistureBudget * 0.35 + groundwaterCoupling * 0.15) ||
                             (hydrologyEnvelope > 0.6f && ventilationBias < 0.95) ||
                             (flow > 0.8f && ventilationBias < 1.05) ||
                             (waterTableClamp > 0.65 && hydrologyEnvelope > 0.55f));
                        chunk.SetBlock(x, y, z, flooded ? BlockType.Water : BlockType.Air);
                    }
                }
            }

            AddCaveSupports(chunk, caves);
        }

        private void ApplyHydrology(ChunkData chunk, int[,] heightMap, float[,]? riverMask, float[,]? lakeMask, float[,]? hydrologyMask = null, float[,]? flowMask = null)
        {
            if (riverMask != null)
            {
                ApplyRiverBankErosion(heightMap, riverMask);
                FeatherMaskEdges(riverMask, config.Water.RiverEdgeFeather, config.Water.RiverSeamFillStrength);
            }

            if (lakeMask != null)
            {
                SealLakeRims(lakeMask, hydrologyMask, flowMask);
                FeatherMaskEdges(lakeMask, config.Water.HydrologySmoothBlend * 0.25, config.Water.HydrologySeamRelaxBlend * 0.5);
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    float river = riverMask != null ? riverMask[x, z] : 0f;
                    float lake = lakeMask != null ? lakeMask[x, z] : 0f;
                    float hydrology = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;
                    float neighbourHydro = hydrologyMask != null ? SampleInterior(hydrologyMask, x, z) : hydrology;
                    float neighbourFlow = flowMask != null ? SampleInterior(flowMask, x, z) : flow;
                    float hydrologyGradient = hydrologyMask != null ? Math.Abs(neighbourHydro - hydrology) : 0f;
                    float continuityBoost = (float)Math.Clamp((hydrology + neighbourHydro + flow + neighbourFlow) * 0.25f * config.Water.HydrologyContinuityWeight, 0.0, 0.85);
                    float flowShadow = Math.Clamp(
                        (flow / Math.Max(1f, config.Water.RiverDepth)) * (float)config.Water.HydrologyFlowShadowWeight +
                        hydrologyGradient * (float)config.Water.HydrologyFlowShadowSlopeWeight * 0.5f,
                        0f,
                        1f);
                    float seamStability = 1f - Math.Clamp(hydrologyGradient * (float)config.Water.HydrologyEdgeStabilityWeight * 0.35f, 0f, 0.85f);
                    float seamContinuity = 1f + continuityBoost * 0.25f;

                    bool hasRiver = river > config.Water.RiverCenterThreshold;
                    bool hasLake = lake > config.Lakes.ShorelineBlend;
                    float wetland = Math.Max(Math.Max(river, lake), hydrology * (float)Math.Clamp(config.Water.RiparianSaturationBoost, 0.0, 1.0));
                    float wetlandPressure = Math.Max(wetland, (hydrology + neighbourHydro) * 0.5f + (flow + neighbourFlow) * 0.1f + continuityBoost * 0.35f);

                    if (hasRiver)
                    {
                        float saturation = Math.Max(river, hydrology);
                        int depth = Math.Clamp(
                            (int)(config.Water.RiverDepth * (river + hydrology * 0.5f + flow * 0.35f + saturation * 0.15f) * (1f - flowShadow * 0.25f) * seamContinuity),
                            2,
                            config.Water.RiverDepth + 3);
                        for (int d = 0; d < depth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        // Feather river banks to reduce jagged edges and surface seams
                        int bankDepth = Math.Max(1, (int)((config.Water.RiverEdgeFeather * 4 + hydrology * 2 - hydrologyGradient) * seamStability * seamContinuity));
                        for (int b = 0; b < bankDepth; b++)
                        {
                            int bankY = Math.Max(bedrockLevel + 1, surface - b);
                            var current = chunk.GetBlock(x, bankY, z);
                            if (current == BlockType.Grass || current == BlockType.Dirt)
                            {
                                chunk.SetBlock(x, bankY, z, b == 0 ? BlockType.Sand : BlockType.Dirt);
                            }
                        }

                        ErodeRiverBanks(chunk, x, z, surface, bankDepth, config.Water.RiverBankErosionWeight * seamStability * seamContinuity);
                        ApplyRiverMouthBlend(chunk, x, z, surface, (int)(config.Water.RiverMouthSmoothRadius * seamStability * seamContinuity));
                        if (surface <= seaLevel + config.Water.RiverMouthSmoothRadius && config.Water.RiverDeltaWetlandStrength > 0)
                        {
                            int deltaDepth = Math.Max(1, (int)(config.Water.RiverDeltaWetlandStrength * 2));
                            for (int d = 0; d < deltaDepth; d++)
                            {
                                int dy = Math.Max(bedrockLevel + 1, surface - d);
                                if (chunk.GetBlock(x, dy, z) == BlockType.Grass)
                                {
                                    chunk.SetBlock(x, dy, z, BlockType.Sand);
                                }
                            }
                        }
                    }
                    else if (hasLake)
                    {
                        int depth = Math.Clamp((int)(config.Lakes.MaxDepth * (lake + hydrology * 0.25f + flow * 0.15f + continuityBoost * 0.35f) * (1f - flowShadow * 0.2f)), config.Lakes.MinDepth, config.Lakes.MaxDepth);
                        int shelfDepth = Math.Clamp((int)(config.Lakes.ShelfDepth * seamContinuity), 1, depth);

                        for (int d = 0; d < depth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        for (int s = 0; s < shelfDepth; s++)
                        {
                            int bankY = Math.Max(bedrockLevel + 1, surface - s);
                            if (chunk.GetBlock(x, bankY, z) == BlockType.Grass)
                            {
                                chunk.SetBlock(x, bankY, z, BlockType.Sand);
                            }
                        }

                        ApplyWetlandRing(chunk, x, z, surface, config.Lakes.WetlandBufferRadius);
                        if (flowMask != null)
                        {
                            ApplyLakeOutflowChannel(chunk, x, z, surface, flow, flowMask, hydrology, seamStability * seamContinuity);
                        }
                    }
                    else if ((wetlandPressure > 0.6f || flow > 0.85f) && chunk.GetBlock(x, surface, z) == BlockType.Grass)
                    {
                        int shallowDepth = Math.Max(1, (int)Math.Min(config.Water.RiverDepth, (wetlandPressure + flow + continuityBoost) * 2f * (1f - flowShadow * 0.35f)));
                        for (int d = 0; d < shallowDepth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }

                    // Ensure sea level fill for low terrain
                    for (int y = bedrockLevel + 1; y <= seaLevel; y++)
                    {
                        if (y > surface && chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }

                    // Add a shallow wetland buffer to keep riparian zones consistent
                    if (!hasRiver && !hasLake && wetlandPressure > 0.35f && chunk.GetBlock(x, surface, z) == BlockType.Grass)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Dirt);
                        if (hydrology > 0.55f && surface <= seaLevel + 1 && chunk.GetBlock(x, surface + 1, z) == BlockType.Air && flowShadow < 0.9f)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Water);
                        }
                    }
                }
            }
        }

        private void ApplyRiverBankErosion(int[,] heightMap, float[,] riverMask)
        {
            double erosionWeight = Math.Clamp(config.Water.RiverBankErosionWeight, 0.0, 1.0);
            if (erosionWeight <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, (int)Math.Ceiling(config.Water.RiverDepth * erosionWeight * 0.5));
            double threshold = config.Water.RiverBankThreshold * 0.35;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float pressure = riverMask[x, z];
                    if (pressure <= threshold)
                    {
                        continue;
                    }

                    int erosion = Math.Max(1, (int)Math.Round(pressure * config.Water.RiverDepth * erosionWeight));
                    int targetHeight = Math.Max(bedrockLevel + 1, heightMap[x, z] - erosion);
                    if (targetHeight < heightMap[x, z])
                    {
                        heightMap[x, z] = targetHeight;
                    }

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                            {
                                continue;
                            }

                            double falloff = 1.0 - (Math.Abs(dx) + Math.Abs(dz)) / (double)(radius + 1);
                            if (falloff <= 0.0)
                            {
                                continue;
                            }

                            int neighbourErosion = Math.Max(0, (int)Math.Round(erosion * falloff * 0.5));
                            if (neighbourErosion <= 0)
                            {
                                continue;
                            }

                            int neighbourHeight = Math.Max(bedrockLevel + 1, heightMap[nx, nz] - neighbourErosion);
                            heightMap[nx, nz] = neighbourHeight;
                        }
                    }
                }
            }
        }

        private void SealLakeRims(float[,] lakeMask, float[,]? hydrologyMask, float[,]? flowMask)
        {
            if (lakeMask == null)
            {
                return;
            }

            double rimWeight = Math.Clamp(config.Water.LakeRimErosionWeight, 0.0, 1.0);
            if (rimWeight <= 0.0)
            {
                return;
            }

            int sizeX = lakeMask.GetLength(0);
            int sizeZ = lakeMask.GetLength(1);
            var buffer = (float[,])lakeMask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float value = buffer[x, z];
                    if (value <= 0f)
                    {
                        continue;
                    }

                    float hydro = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;
                    float neighbourHydro = hydrologyMask != null ? SampleInterior(hydrologyMask, x, z) : hydro;
                    float neighbourFlow = flowMask != null ? SampleInterior(flowMask, x, z) : flow;
                    float hydroGradient = Math.Abs(neighbourHydro - hydro);
                    float flowGradient = Math.Abs(neighbourFlow - flow);
                    float seamGuard = 1f - Math.Clamp(
                        (hydroGradient + flowGradient) * (float)config.Water.HydrologyEdgeStabilityWeight * 0.35f,
                        0f,
                        0.75f);
                    float edgePenalty = (x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1)
                        ? (float)config.Water.RiverSeamFillStrength * 0.35f
                        : 0f;

                    float rimLoss = (float)(rimWeight * (hydroGradient + flowGradient) * 0.5f) + edgePenalty;
                    rimLoss = Math.Clamp(rimLoss, 0f, 0.85f);
                    lakeMask[x, z] = Math.Max(0f, value * seamGuard * (1f - rimLoss));
                }
            }

            Smooth2D(lakeMask, Math.Max(1, config.Lakes.LakeBasinSmoothIterations), Math.Clamp(config.Water.HydrologySmoothBlend * 0.65, 0.0, 1.0));
        }

        private static void FeatherMaskEdges(float[,] mask, double feather, double seamFill)
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

        private static void BlendInterior(float[,] field, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    float neighbour = SampleInterior(field, x, z);
                    buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + neighbour * blend);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private void DirectionalSmooth(int[,] heightMap, float[,] field, int iterations, double blend)
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
                        var downhill = ComputeDownhillDirection(heightMap, x, z);
                        if (downhill.dx == 0 && downhill.dz == 0)
                        {
                            buffer[x, z] = field[x, z];
                            continue;
                        }

                        int nx = Math.Clamp(x + downhill.dx, 0, sizeX - 1);
                        int nz = Math.Clamp(z + downhill.dz, 0, sizeZ - 1);
                        float target = field[nx, nz];
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + target * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private void ApplyGradientStability(float[,] field, int iterations, double blend, double clamp)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            clamp = Math.Max(0.001, clamp);
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
                        float neighbour = SampleInterior(field, x, z);

                        double gradient = 0.0;
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
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                                {
                                    continue;
                                }

                                gradient = Math.Max(gradient, Math.Abs(field[nx, nz] - centre));
                            }
                        }

                        double gradientFactor = 1.0 - Math.Clamp(gradient / clamp, 0.0, 1.0);
                        double target = neighbour * gradientFactor + centre * (1.0 - gradientFactor * 0.35);
                        buffer[x, z] = (float)(centre + (target - centre) * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private (int dx, int dz) ComputeDownhillDirection(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int current = heightMap[x, z];
            int bestDrop = 0;
            int bestDx = 0;
            int bestDz = 0;

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
                    if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                    {
                        continue;
                    }

                    int drop = current - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDx = dx;
                        bestDz = dz;
                    }
                }
            }

            return (bestDx, bestDz);
        }

        private void StabilizeCaveMask(bool[,,] mask, int[,] heightMap, int iterations, double blend)
        {
            iterations = Math.Max(1, Math.Min(4, iterations));
            blend = Math.Clamp(blend, 0.0, 1.0);

            for (int iter = 0; iter < iterations; iter++)
            {
                var source = (bool[,,])mask.Clone();
                int pruneThreshold = blend >= 0.5 ? 3 : 2;
                int fillThreshold = blend >= 0.5 ? 6 : 7;

                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int limitY = Math.Min(worldHeight - 4, heightMap[x, z]);
                        for (int y = bedrockLevel + 1; y < limitY; y++)
                        {
                            bool open = source[x, y, z];
                            int neighbours = CountOpenNeighbours(source, x, y, z);

                            if (open && neighbours < pruneThreshold)
                            {
                                mask[x, y, z] = false;
                            }
                            else if (!open && neighbours >= fillThreshold)
                            {
                                mask[x, y, z] = true;
                            }
                        }
                    }
                }
            }
        }

        private int CountOpenNeighbours(bool[,,] mask, int x, int y, int z)
        {
            int count = 0;
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
                        if (nx < 0 || ny < 0 || nz < 0 || nx >= chunkSize || ny >= worldHeight || nz >= chunkSize)
                        {
                            continue;
                        }

                        if (mask[nx, ny, nz])
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
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

        private static void Smooth2D(float[,] field, int iterations, double blend)
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
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                                {
                                    continue;
                                }

                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = sum / samples;
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + average * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private void RelaxEdges(float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = (float[,])field.Clone();

                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (!IsEdge(x, z))
                        {
                            continue;
                        }

                        float sum = 0;
                        int samples = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                                {
                                    continue;
                                }

                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        var average = samples > 0 ? sum / samples : field[x, z];
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + average * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private void ApplyFlowMemoryWeight(float[,] flow, double memoryWeight, double divergenceClamp)
        {
            memoryWeight = Math.Clamp(memoryWeight, 0.0, 1.0);
            if (memoryWeight <= 0.0)
            {
                return;
            }

            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            var buffer = (float[,])flow.Clone();
            double clampMax = Math.Max(2.5, divergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float anchor = SampleInterior(flow, x, z);
                    buffer[x, z] = (float)Math.Clamp(
                        flow[x, z] * (1.0 - memoryWeight) + anchor * memoryWeight,
                        0.0,
                        clampMax);
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
        }

        private void BoostRiverConfluences(float[,] field, double confluenceBoost)
        {
            if (confluenceBoost <= 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    float center = field[x, z];
                    if (center <= 0f)
                    {
                        continue;
                    }

                    float neighbors = 0f;
                    int samples = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            neighbors += field[x + dx, z + dz];
                            samples++;
                        }
                    }

                    float average = samples > 0 ? neighbors / samples : 0f;
                    float boosted = center + (average * (float)confluenceBoost * 0.5f);
                    buffer[x, z] = Math.Clamp(boosted, 0f, 1f);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private void ApplyWetlandBuffer(float[,] field, int radius, double shorelineBlend)
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
                    float center = field[x, z];
                    if (center <= 0f)
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

                            float falloff = 1f - (Math.Abs(dx) + Math.Abs(dz)) / (float)(radius + 1);
                            float influence = Math.Clamp(center * (float)shorelineBlend * falloff, 0f, 1f);
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private double ComputeSlope(int[,] heightMap, int x, int z)
        {
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int down = heightMap[x, Math.Max(0, z - 1)];
            int up = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double dx = right - left;
            double dz = up - down;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private double ComputeLocalRelief(int[,] heightMap, int x, int z)
        {
            int minHeight = heightMap[x, z];
            int maxHeight = heightMap[x, z];

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = Math.Clamp(x + dx, 0, chunkSize - 1);
                    int nz = Math.Clamp(z + dz, 0, chunkSize - 1);
                    int sample = heightMap[nx, nz];
                    if (sample < minHeight)
                    {
                        minHeight = sample;
                    }

                    if (sample > maxHeight)
                    {
                        maxHeight = sample;
                    }
                }
            }

            return Math.Clamp((maxHeight - minHeight) / 24.0, 0.0, 1.0);
        }

        private double ComputeBasinPotential(int[,] heightMap, int x, int z, int waterLevel)
        {
            int center = heightMap[x, z];
            double enclosure = 0.0;
            int samples = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int nx = Math.Clamp(x + dx, 0, chunkSize - 1);
                    int nz = Math.Clamp(z + dz, 0, chunkSize - 1);
                    int neighbour = heightMap[nx, nz];
                    enclosure += (neighbour - center) * 0.08;
                    samples++;
                }
            }

            double averageEnclosure = samples > 0 ? enclosure / samples : 0.0;
            double waterBias = 1.0 - Math.Clamp(
                Math.Abs(center - waterLevel) / Math.Max(1.0, config.Water.HydrologyWaterTableClampRange * 1.5),
                0.0,
                1.0);
            return Math.Clamp(0.5 + averageEnclosure + waterBias * 0.35, 0.0, 1.0);
        }

        private static float SampleNeighborhoodMax(float[,] field, int x, int z, int radius)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            radius = Math.Max(0, radius);
            float max = 0f;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = Math.Clamp(x + dx, 0, sizeX - 1);
                    int nz = Math.Clamp(z + dz, 0, sizeZ - 1);
                    max = Math.Max(max, field[nx, nz]);
                }
            }

            return max;
        }

        private double ComputeAnisotropy(int[,] heightMap, int x, int z)
        {
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int down = heightMap[x, Math.Max(0, z - 1)];
            int up = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double slopeX = Math.Abs(right - left);
            double slopeZ = Math.Abs(up - down);
            double diff = Math.Abs(slopeX - slopeZ);
            return Math.Min(1.0, diff * 0.01);
        }

        private double ComputeFlowAlignment(int[,] heightMap, int x, int z)
        {
            int current = heightMap[x, z];
            int east = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int north = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double dx = current - east;
            double dz = current - north;
            double magnitude = Math.Sqrt(dx * dx + dz * dz);
            if (magnitude <= double.Epsilon)
            {
                return 0.0;
            }

            double normalized = magnitude / (magnitude + 12.0);
            return 1.0 - normalized;
        }

        private void AddCaveSupports(ChunkData chunk, CaveConfig caves)
        {
            double chance = Math.Clamp(caves.SupportPillarChance, 0.0, 1.0);
            if (chance <= 0.0)
            {
                return;
            }

            int maxY = Math.Min(worldHeight - 4, seaLevel);
            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    if (random.NextDouble() > chance)
                    {
                        continue;
                    }

                    int baseY = random.Next(bedrockLevel + 2, Math.Max(bedrockLevel + 3, maxY - 6));
                    int height = random.Next(2, 5);
                    for (int i = 0; i < height; i++)
                    {
                        int y = baseY + i;
                        var current = chunk.GetBlock(x, y, z);
                        if (current == BlockType.Air || current == BlockType.Water)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }
            }
        }

        private void ErodeRiverBanks(ChunkData chunk, int x, int z, int surface, int bankDepth, double erosionWeight)
        {
            if (erosionWeight <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, (int)Math.Round(erosionWeight * 4));
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Math.Abs(dx) + Math.Abs(dz) > radius)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, surface - bankDepth);
                    var block = chunk.GetBlock(nx, ny, nz);
                    if (block == BlockType.Grass)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Dirt);
                    }
                    else if (block == BlockType.Dirt && ny <= seaLevel + 1)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Sand);
                    }
                }
            }
        }

        private void ApplyRiverMouthBlend(ChunkData chunk, int x, int z, int surface, int radius)
        {
            radius = Math.Max(0, radius);
            if (radius == 0 || surface > seaLevel + radius)
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, seaLevel - Math.Max(0, Math.Abs(dx) + Math.Abs(dz) - 1));
                    for (int y = ny; y <= seaLevel; y++)
                    {
                        if (chunk.GetBlock(nx, y, nz) == BlockType.Air || chunk.GetBlock(nx, y, nz) == BlockType.Dirt)
                        {
                            chunk.SetBlock(nx, y, nz, BlockType.Water);
                        }
                    }
                }
            }
        }

        private void ApplyWetlandRing(ChunkData chunk, int x, int z, int surface, int radius)
        {
            radius = Math.Max(0, radius);
            if (radius == 0)
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, surface - 1);
                    var block = chunk.GetBlock(nx, ny, nz);
                    if (block == BlockType.Grass)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Dirt);
                    }

                    if (ny < seaLevel && chunk.GetBlock(nx, ny + 1, nz) == BlockType.Air)
                    {
                        chunk.SetBlock(nx, ny + 1, nz, BlockType.Water);
                    }
                }
            }
        }

        private void ApplyLakeOutflowChannel(ChunkData chunk, int x, int z, int surface, float flowValue, float[,] flowMask, float hydrology, float seamStability)
        {
            float normalizedFlow = Math.Clamp(flowValue / Math.Max(1f, config.Water.RiverDepth), 0f, 1f);
            if (normalizedFlow <= 0.55f || seamStability <= 0.1f)
            {
                return;
            }

            var directions = new (int dx, int dz)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            float bestFlow = 0f;
            int bestDx = 0;
            int bestDz = 0;

            foreach (var dir in directions)
            {
                int nx = x + dir.dx;
                int nz = z + dir.dz;
                if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                {
                    continue;
                }

                float neighbourFlow = flowMask[nx, nz];
                if (neighbourFlow > bestFlow)
                {
                    bestFlow = neighbourFlow;
                    bestDx = dir.dx;
                    bestDz = dir.dz;
                }
            }

            if (bestFlow <= 0f)
            {
                return;
            }

            int channelDepth = Math.Max(1, (int)Math.Round(config.Lakes.OutflowStabilityWeight * 3 * seamStability));
            int steps = Math.Min(2, Math.Max(1, (int)Math.Round(normalizedFlow * 2)));
            for (int step = 0; step < steps; step++)
            {
                int cx = Math.Clamp(x + bestDx * step, 0, chunkSize - 1);
                int cz = Math.Clamp(z + bestDz * step, 0, chunkSize - 1);
                int bottom = Math.Max(bedrockLevel + 1, surface - channelDepth);
                for (int d = 0; d <= channelDepth; d++)
                {
                    int y = Math.Max(bedrockLevel + 1, bottom + d);
                    var current = chunk.GetBlock(cx, y, cz);
                    if (current == BlockType.Grass || current == BlockType.Dirt || current == BlockType.Air)
                    {
                        chunk.SetBlock(cx, y, cz, y <= seaLevel || hydrology > 0.5f ? BlockType.Water : current);
                    }
                }
            }
        }

        private bool IsEdge(int x, int z)
        {
            return x == 0 || z == 0 || x == chunkSize - 1 || z == chunkSize - 1;
        }

        private BiomeType ResolveBiome(int height, int noiseSample)
        {
            if (height <= seaLevel + 1)
            {
                return BiomeType.Beach;
            }

            if (noiseSample > 55 && height > seaLevel + 24)
            {
                return BiomeType.Mountains;
            }

            if (noiseSample < -30)
            {
                return BiomeType.Desert;
            }

            if (noiseSample < 0)
            {
                return BiomeType.Plains;
            }

            return BiomeType.Forest;
        }

        private BlockType SelectSurfaceBlock(BiomeType biome, int height)
        {
            if (height <= seaLevel + 1)
            {
                return BlockType.Sand;
            }

            return biome switch
            {
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                _ => BlockType.Grass
            };
        }
    }
}
