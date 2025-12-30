using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Lake basin mask generator with river suppression and shoreline blending.
    /// </summary>
    public sealed class ImprovedLakeGenerator
    {
        private readonly LakeConfig config;
        private readonly Random random;

        public ImprovedLakeGenerator(LakeConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)worldSeed ^ 0x1A2E0001);
        }

        public float[,] BuildMask(int chunkX, int chunkZ, int chunkSize, int[,] heightMap, float[,]? riverMask, int seaLevel)
        {
            var lakes = new float[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double primary = SimplexNoise.Generate(worldX * 0.008, worldZ * 0.008, 1.0, 3, 1.0, 0.55, random.Next());
                    double basin = SimplexNoise.Generate(worldX * 0.004 + 31, worldZ * 0.004 + 17, 1.0, 2, 1.0, 0.6, random.Next());
                    double weight = (primary * 0.6) + (basin * 0.4) + config.SpawnWeightBias;

                    if (riverMask != null)
                    {
                        weight -= riverMask[x, z] * config.RiverProximitySuppression;
                    }

                    if (weight > config.WetlandSaturationThreshold && heightMap[x, z] > seaLevel - config.MaxDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            Smooth(lakes, config.LakeBasinSmoothIterations, 0.55);
            return lakes;
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
