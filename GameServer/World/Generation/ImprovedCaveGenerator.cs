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
                        double warped = primary + moistureBias * config.MoistureRetentionWeight;

                        mask[x, y, z] = warped > threshold;
                    }
                }
            }

            return mask;
        }
    }
}
