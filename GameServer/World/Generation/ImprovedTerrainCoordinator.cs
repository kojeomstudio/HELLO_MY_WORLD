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

            long seed = worldSettings.WorldSeed != 0 ? worldSettings.WorldSeed : config.Seed;
            caveGenerator = new ImprovedCaveGenerator(config.Caves, seed);
            riverGenerator = new ImprovedRiverGenerator(config.Water, seed);
            lakeGenerator = new ImprovedLakeGenerator(config.Lakes, config.Water, seed);
        }

        public TerrainMaskResult GenerateMasks(int chunkX, int chunkZ, int[,] heightMap, int sizeOverride)
        {
            int size = Math.Min(Math.Max(1, sizeOverride), chunkSize);
            var hydrology = BuildHydrologyMask(heightMap, size);
            var flow = BuildFlowAccumulation(heightMap, hydrology, size);

            float[,]? riverMask = config.Water.EnableRivers
                ? riverGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, seaLevel)
                : null;

            float[,]? lakeMask = config.Water.EnableLakes
                ? lakeGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, riverMask, seaLevel)
                : null;

            bool[,,]? caveMask = config.Caves.EnableCaves
                ? caveGenerator.BuildMask(chunkX, chunkZ, size, worldHeight, heightMap, hydrology, riverMask, seaLevel)
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

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - seaLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / clampRange, 0.0, 1.0);
                    double slopePenalty = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slopePenalty * slopeWeight / 6.0, 0.0, 0.6);
                    hydrology[x, z] = (float)Math.Clamp(waterBias * clampWeight * stability, 0.0, 1.0);
                }
            }

            TerrainMaskUtility.Smooth2D(hydrology, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.RelaxEdges(hydrology, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return hydrology;
        }

        private float[,] BuildFlowAccumulation(int[,] heightMap, float[,] hydrology, int size)
        {
            var flow = new float[size, size];
            double persistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    double accumulation = 0.0;
                    double current = heightMap[x, z];

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
                    flow[x, z] = (float)Math.Clamp((accumulation * (1.0 - persistence)) + hydrologyBoost, 0.0, 8.0);
                }
            }

            TerrainMaskUtility.Smooth2D(flow, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.RelaxEdges(flow, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return flow;
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
    }
}
