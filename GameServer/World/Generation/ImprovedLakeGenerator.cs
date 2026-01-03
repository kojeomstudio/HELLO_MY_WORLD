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
            int seaLevel)
        {
            var lakes = new float[chunkSize, chunkSize];

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double basinNoise = SimplexNoise.Generate(worldX * 0.004, worldZ * 0.004, 1.0, 3, 1.0, 0.6, random.Next());
                    double rimNoise = SimplexNoise.Generate(worldX * 0.009 + 31, worldZ * 0.009 + 17, 1.0, 2, 1.0, 0.55, random.Next());
                    double hydrology = hydrologyMask[x, z];
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double riverSuppression = riverMask != null ? riverMask[x, z] * lakeConfig.RiverProximitySuppression : 0.0;
                    double inflowBlend = riverMask != null ? riverMask[x, z] * waterConfig.LakeInflowBlendWeight : 0.0;
                    double reliefPenalty = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
                    double radiusFalloff = Math.Clamp(edgeDistance / (double)Math.Max(1, lakeConfig.MaxRadius), 0.0, 1.0);

                    double wetness = hydrology * 0.65 + flow * 0.35;
                    double weight = (basinNoise * 0.45) + (rimNoise * 0.25) + wetness * 0.4 + lakeConfig.SpawnWeightBias;
                    weight += inflowBlend * 0.35;
                    weight -= slope * waterConfig.LakeRimErosionWeight * 0.05;
                    double hydrologyGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrologyMask, x, z) - hydrology);
                    weight -= hydrologyGradient * waterConfig.HydrologyEdgeStabilityWeight * 0.25;
                    weight -= riverSuppression * 0.5;
                    weight -= reliefPenalty * waterConfig.RiverReliefPenaltyWeight;
                    weight *= 0.75 + radiusFalloff * 0.25;
                    double seamCushion = 1.0 + Math.Clamp((TerrainMaskUtility.SampleInterior(hydrologyMask, x, z) - hydrology) * waterConfig.HydrologyEdgeFluxBlend, -0.2, 0.3);
                    weight *= seamCushion;

                    double wetlandThreshold = lakeConfig.WetlandSaturationThreshold - wetness * 0.1;
                    if (weight > wetlandThreshold && heightMap[x, z] > seaLevel - lakeConfig.MaxDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            TerrainMaskUtility.Smooth2D(lakes, lakeConfig.LakeBasinSmoothIterations, waterConfig.HydrologySmoothBlend);
            TerrainMaskUtility.StitchEdges(lakes, waterConfig.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.FillBasins(lakes, Math.Max(0.05, waterConfig.HydrologyEdgeStabilityWeight * 0.35), Math.Max(1, waterConfig.HydrologySeamRelaxIterations));
            TerrainMaskUtility.RelaxEdges(lakes, waterConfig.HydrologySeamRelaxIterations, waterConfig.HydrologySeamRelaxBlend);
            ApplyWetlandBuffer(lakes, Math.Min(lakeConfig.WetlandBufferRadius, lakeConfig.MaxRadius), lakeConfig.ShorelineBlend);
            ApplyOutflowChannels(lakes, heightMap, flowAccumulation, waterConfig.LakeInflowBlendWeight, lakeConfig.OutflowCarveDepth);
            return lakes;
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

        private static void ApplyOutflowChannels(float[,] lakes, int[,] heightMap, float[,] flow, double inflowBlendWeight, int outflowDepth)
        {
            inflowBlendWeight = Math.Clamp(inflowBlendWeight, 0.0, 1.0);
            outflowDepth = Math.Max(1, outflowDepth);
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

                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    if (downhill == (0, 0))
                    {
                        continue;
                    }

                    int currentX = x;
                    int currentZ = z;
                    float channelStrength = lakeStrength;

                    for (int step = 0; step < outflowDepth; step++)
                    {
                        currentX = Math.Clamp(currentX + downhill.X, 0, sizeX - 1);
                        currentZ = Math.Clamp(currentZ + downhill.Z, 0, sizeZ - 1);

                        float flowInfluence = TerrainMaskUtility.Clamp01(flow[currentX, currentZ] * (float)inflowBlendWeight);
                        float blended = Math.Max(channelStrength * 0.65f, lakeStrength * 0.35f);
                        buffer[currentX, currentZ] = Math.Max(buffer[currentX, currentZ], blended + flowInfluence * 0.5f);

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
