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
            float[,]? riverMask,
            int seaLevel)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            double horizontal = Math.Max(0.0001, config.HorizontalFrequency);
            double vertical = Math.Max(0.0001, config.VerticalFrequency);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    float hydrology = TerrainMaskUtility.Clamp01(hydrologyMask[x, z]);
                    float riverPressure = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double stability = ComputeColumnStability(surface, hydrology, riverPressure);
                    double wetnessRetention = hydrology * config.MoistureRetentionWeight;

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

                        double density = (primary * 0.65) + (secondary * 0.35);
                        double moisturePenalty = hydrology * config.HydrologyStabilityWeight + riverPressure * config.RiverSuppressionWeight + wetnessRetention * 0.35;
                        double roughnessBias = (0.5 + SimplexNoise.Generate(warpX * 0.8, warpZ * 0.8, 1.0, 1, 1.0, 0.5, random.Next()) * 0.5) * config.RoughnessStabilityWeight;
                        double threshold = config.Threshold + moisturePenalty * 0.35 + config.FlowStabilityWeight * 0.2 + roughnessBias * 0.25;
                        threshold -= depthFactor * depthWeight * 0.6;
                        threshold += wetnessRetention * 0.15;
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
            SealEdges(mask, config.EdgeSealStrength);
            return mask;
        }

        private double ComputeColumnStability(int surface, float hydrology, float riverPressure)
        {
            double waterBias = 1.0 - Math.Clamp(hydrology * config.HydrologyStabilityWeight, 0.0, 0.75);
            double riverBias = 1.0 - Math.Clamp(riverPressure * config.RiverSuppressionWeight, 0.0, 0.9);
            double ceilingBias = 1.0 - Math.Clamp((surface / 128.0) * config.CeilingStabilityWeight, 0.0, 0.35);
            return Math.Clamp(waterBias * riverBias * (1.0 - ceilingBias * 0.35), 0.05, 1.25);
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
                    double pillarChance = chance * (1.0 + hydrology * config.SupportHydrationBias + river * config.SupportFlowBias);
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

        private void SealEdges(bool[,,] mask, double strength)
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
                        if (mask[x, y, z] && random.NextDouble() < strength)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }
    }
}
