using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Noise-driven cave mask generator that respects world cave knobs and seed.
    /// </summary>
    public sealed class ImprovedCaveGenerator
    {
        private readonly CaveConfig config;
        private readonly Random random;

        public ImprovedCaveGenerator(CaveConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)worldSeed ^ 0x6CA5E001);
        }

        public bool[,,] BuildMask(int chunkX, int chunkZ, int chunkSize, int worldHeight, int seaLevel)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            double horizontalFreq = Math.Max(0.0005, config.HorizontalFrequency);
            double verticalFreq = Math.Max(0.005, config.VerticalFrequency);
            double threshold = config.Threshold;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = config.RegionalMainCaveMinY; y < Math.Min(worldHeight - 4, config.RegionalMainCaveMaxY); y++)
                    {
                        int worldX = chunkX * chunkSize + x;
                        int worldZ = chunkZ * chunkSize + z;

                        double primary = SimplexNoise.Generate(
                            worldX * horizontalFreq,
                            worldZ * horizontalFreq + y * verticalFreq,
                            1.0,
                            3,
                            1.0,
                            0.55,
                            random.Next());
                        double moistureBias = (double)(seaLevel - y) / Math.Max(1, seaLevel);
                        double depthRatio = (double)(y - config.RegionalMainCaveMinY) / Math.Max(1, config.RegionalMainCaveMaxY - config.RegionalMainCaveMinY);
                        double stabilityBias = depthRatio * config.CeilingStabilityWeight;
                        double hydrationBias = moistureBias * config.MoistureRetentionWeight;
                        double flowBias = moistureBias * config.SupportFlowBias;
                        double warped = primary + hydrationBias - stabilityBias + flowBias;

                        double adjustedThreshold = threshold - (config.SupportDensity * 0.1);
                        if (y < seaLevel - 6)
                        {
                            adjustedThreshold += config.RiverSuppressionWeight * 0.15;
                        }

                        mask[x, y, z] = warped > adjustedThreshold;
                    }
                }
            }

            SmoothMask(mask, config.StabilitySmoothIterations, config.StabilitySmoothBlend);
            ApplyEdgeSeal(mask, config.EdgeSealStrength);
            AddSupportPillars(mask, config.SupportPillarChance, config.RiparianPlugDepth, seaLevel);
            return mask;
        }

        private static void SmoothMask(bool[,,] mask, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);

            var buffer = new bool[sizeX, sizeY, sizeZ];
            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 1; y < sizeY - 1; y++)
                    {
                        for (int z = 0; z < sizeZ; z++)
                        {
                            int active = 0;
                            int samples = 0;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        int nx = x + dx;
                                        int ny = y + dy;
                                        int nz = z + dz;
                                        if (nx < 0 || ny < 0 || nz < 0 || nx >= sizeX || ny >= sizeY || nz >= sizeZ)
                                        {
                                            continue;
                                        }

                                        samples++;
                                        if (mask[nx, ny, nz])
                                        {
                                            active++;
                                        }
                                    }
                                }
                            }

                            double average = samples > 0 ? (double)active / samples : 0.0;
                            double current = mask[x, y, z] ? 1.0 : 0.0;
                            buffer[x, y, z] = (current * (1.0 - blend) + average * blend) > 0.5;
                        }
                    }
                }

                Array.Copy(buffer, mask, buffer.Length);
            }
        }

        private static void ApplyEdgeSeal(bool[,,] mask, double sealStrength)
        {
            sealStrength = Math.Clamp(sealStrength, 0.0, 1.0);
            if (sealStrength <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            var rand = new Random(0x6AF3E551);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    for (int y = 1; y < sizeY - 1; y++)
                    {
                        if (mask[x, y, z] && rand.NextDouble() < sealStrength)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private static void AddSupportPillars(bool[,,] mask, double pillarChance, int plugDepth, int seaLevel)
        {
            pillarChance = Math.Clamp(pillarChance, 0.0, 1.0);
            if (pillarChance <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            var rand = new Random(0x5AF3C0B1);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    if (rand.NextDouble() > pillarChance)
                    {
                        continue;
                    }

                    int baseY = rand.Next(Math.Max(2, plugDepth), Math.Max(plugDepth + 1, Math.Min(sizeY - 6, seaLevel - 2)));
                    int height = rand.Next(2, 6);
                    for (int y = baseY; y < Math.Min(sizeY - 1, baseY + height); y++)
                    {
                        mask[x, y, z] = false;
                    }
                }
            }
        }
    }
}
