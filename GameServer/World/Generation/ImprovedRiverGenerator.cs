using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Generates smooth river intensity masks with meander and gradient awareness.
    /// </summary>
    public sealed class ImprovedRiverGenerator
    {
        private readonly WaterConfig config;
        private readonly Random random;

        public ImprovedRiverGenerator(WaterConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)worldSeed ^ 0x5A7B1001);
        }

        public float[,] BuildMask(int chunkX, int chunkZ, int chunkSize, int[,] heightMap, int seaLevel)
        {
            var mask = new float[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    var warp = SimplexNoise.DomainWarp(
                        worldX,
                        worldZ,
                        config.HydrologyWarpFrequency,
                        config.RiverNoiseScale,
                        config.HydrologyWarpAmplitude,
                        6.0,
                        (int)random.Next());

                    double baseNoise = SimplexNoise.Generate(worldX + warp.dx, worldZ + warp.dz, config.RiverNoiseScale, 3, 1.0, 0.55, (int)random.Next());
                    double intensity = Math.Clamp(1.0 - Math.Abs(baseNoise), 0.0, 1.0);

                    double slope = ComputeSlope(heightMap, x, z, chunkSize);
                    double gradientPenalty = slope * config.RiverGradientPenalty * 0.01;
                    double reliefPenalty = Math.Max(0.0, (heightMap[x, z] - seaLevel) * config.RiverReliefPenaltyWeight * 0.01);
                    double anisotropy = ComputeAnisotropy(heightMap, x, z, chunkSize) * config.RiverAnisotropyWeight;
                    double headwaterStability = 1.0 - Math.Min(1.0, Math.Abs(heightMap[x, z] - seaLevel) * config.RiverHeadwaterStabilityWeight * 0.01);
                    double flowAlignment = ComputeFlowAlignment(heightMap, x, z, chunkSize) * config.RiverFlowAlignmentWeight;

                    intensity = intensity * (1.0 - gradientPenalty);
                    intensity = (intensity * headwaterStability) - reliefPenalty - anisotropy;
                    intensity += flowAlignment;

                    if (intensity > config.RiverCenterThreshold)
                    {
                        mask[x, z] = (float)intensity;
                    }
                }
            }

            Smooth(mask, config.RiverIntensitySmoothIterations, config.RiverIntensitySmoothBlend);
            BoostConfluences(mask, config.RiverConfluenceBoost);
            RelaxEdges(mask, config.HydrologySeamRelaxIterations, config.HydrologySeamRelaxBlend, config.HydrologyEdgeBlendRadius, config.HydrologyEdgeFluxBlend);
            return mask;
        }

        private static double ComputeSlope(int[,] heightMap, int x, int z, int chunkSize)
        {
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int down = heightMap[x, Math.Max(0, z - 1)];
            int up = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double dx = right - left;
            double dz = up - down;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private static double ComputeAnisotropy(int[,] heightMap, int x, int z, int chunkSize)
        {
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int down = heightMap[x, Math.Max(0, z - 1)];
            int up = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double slopeX = Math.Abs(right - left);
            double slopeZ = Math.Abs(up - down);
            double diff = Math.Abs(slopeX - slopeZ);
            return Math.Min(1.0, diff / (Math.Max(1.0, slopeX + slopeZ)));
        }

        private static double ComputeFlowAlignment(int[,] heightMap, int x, int z, int chunkSize)
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

        private static void BoostConfluences(float[,] field, double confluenceBoost)
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

        private static void Smooth(float[,] field, int iterations, double blend)
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

        private static void RelaxEdges(float[,] field, int iterations, double blend, int radius, double edgeFluxBlend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            radius = Math.Max(1, radius);
            edgeFluxBlend = Math.Clamp(edgeFluxBlend, 0.0, 1.0);

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = (float[,])field.Clone();
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (x > radius && z > radius && x < sizeX - radius - 1 && z < sizeZ - radius - 1)
                        {
                            continue;
                        }

                        float sum = 0f;
                        int samples = 0;
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

                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = samples > 0 ? sum / samples : field[x, z];
                        float relaxed = (float)(field[x, z] * (1.0 - blend) + average * blend);
                        buffer[x, z] = Math.Clamp(relaxed * (float)(1.0 - edgeFluxBlend) + field[x, z] * (float)edgeFluxBlend, 0f, 1f);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }
    }
}
