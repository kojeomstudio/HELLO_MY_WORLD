using System;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Coordinates improved cave/river/lake generators to produce hydrology masks for a chunk.
    /// </summary>
    public sealed class ImprovedTerrainCoordinator
    {
        private readonly ImprovedCaveGenerator? caves;
        private readonly ImprovedRiverGenerator? rivers;
        private readonly ImprovedLakeGenerator? lakes;
        private readonly WorldGenerationConfig config;
        private readonly int worldHeight;
        private readonly int seaLevel;

        public ImprovedTerrainCoordinator(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            worldHeight = config.WorldHeight;
            seaLevel = Math.Max(4, config.Water.GlobalWaterLevel);

            if (config.Caves.EnableCaves && config.Caves.UseImprovedCaves)
            {
                caves = new ImprovedCaveGenerator(config.Caves, worldSettings.WorldSeed);
            }

            if (config.Water.EnableRivers && config.Water.UseImprovedRivers)
            {
                rivers = new ImprovedRiverGenerator(config.Water, worldSettings.WorldSeed);
            }

            if (config.Water.EnableLakes && config.Water.UseImprovedLakes)
            {
                lakes = new ImprovedLakeGenerator(config.Lakes, worldSettings.WorldSeed);
            }
        }

        public TerrainMaskResult GenerateMasks(int chunkX, int chunkZ, int[,] heightMap, int chunkSize)
        {
            var result = new TerrainMaskResult();
            if (caves != null)
            {
                result.Caves = caves.BuildMask(chunkX, chunkZ, chunkSize, worldHeight, seaLevel);
            }

            if (rivers != null)
            {
                result.Rivers = rivers.BuildMask(chunkX, chunkZ, chunkSize, heightMap, seaLevel);
            }

            if (lakes != null)
            {
                result.Lakes = lakes.BuildMask(chunkX, chunkZ, chunkSize, heightMap, result.Rivers, seaLevel);
            }

            return result;
        }
    }

    public sealed class TerrainMaskResult
    {
        public bool[,,]? Caves { get; set; }
        public float[,]? Rivers { get; set; }
        public float[,]? Lakes { get; set; }
    }
}
