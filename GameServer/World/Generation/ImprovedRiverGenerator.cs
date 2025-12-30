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

                    double gradientPenalty = Math.Abs(heightMap[x, z] - seaLevel) * config.RiverGradientPenalty * 0.01;
                    intensity = intensity * (1.0 - gradientPenalty);

                    if (intensity > config.RiverCenterThreshold)
                    {
                        mask[x, z] = (float)intensity;
                    }
                }
            }

            Smooth(mask, config.RiverIntensitySmoothIterations, config.RiverIntensitySmoothBlend);
            return mask;
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
    }
}
