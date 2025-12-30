using System;
using System.Threading;
using System.Threading.Tasks;
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

            bool[,,]? caveMask = enableCaves
                ? improvedMasks?.Caves ?? BuildCaveMask(chunkX, chunkZ, heightMap)
                : null;
            float[,]? riverMask = enableRivers
                ? improvedMasks?.Rivers ?? BuildRiverMask(chunkX, chunkZ, heightMap)
                : null;
            float[,]? lakeMask = enableLakes
                ? improvedMasks?.Lakes ?? BuildLakeMask(chunkX, chunkZ, heightMap, riverMask)
                : null;

            if (caveMask != null)
            {
                CarveCaves(chunk, caveMask, heightMap);
            }

            ApplyHydrology(chunk, heightMap, riverMask, lakeMask);
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

        private bool[,,] BuildCaveMask(int chunkX, int chunkZ, int[,] heightMap)
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

            return mask;
        }

        private float[,] BuildRiverMask(int chunkX, int chunkZ, int[,] heightMap)
        {
            var mask = new float[chunkSize, chunkSize];
            var water = config.Water;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

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

                    double intensity = Math.Clamp(1.0 - Math.Abs(baseNoise), 0.0, 1.0);
                    double slope = ComputeSlope(heightMap, x, z);
                    double continuity = 1.0 - Math.Min(1.0, slope * water.RiverGradientPenalty * 0.01);
                    double anisotropy = ComputeAnisotropy(heightMap, x, z) * water.RiverAnisotropyWeight;
                    double reliefPenalty = Math.Max(0.0, (heightMap[x, z] - seaLevel) * water.RiverReliefPenaltyWeight * 0.01);

                    intensity = intensity * continuity - anisotropy - reliefPenalty;
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
            RelaxEdges(mask, water.HydrologySeamRelaxIterations, water.HydrologySeamRelaxBlend);
            return mask;
        }

        private float[,] BuildLakeMask(int chunkX, int chunkZ, int[,] heightMap, float[,]? riverMask)
        {
            var lakes = new float[chunkSize, chunkSize];
            var lakeConfig = config.Lakes;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

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
                    double riverSuppression = riverMask != null ? riverMask[x, z] * lakeConfig.RiverProximitySuppression : 0.0;
                    double altitudePenalty = Math.Max(0, seaLevel - heightMap[x, z]) * 0.0025;
                    weight -= riverSuppression + altitudePenalty;

                    if (weight > lakeConfig.WetlandSaturationThreshold && heightMap[x, z] > bedrockLevel + lakeConfig.MinDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            Smooth2D(lakes, lakeConfig.LakeBasinSmoothIterations, config.Water.HydrologySmoothBlend);
            RelaxEdges(lakes, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            return lakes;
        }

        private void CarveCaves(ChunkData chunk, bool[,,] mask, int[,] heightMap)
        {
            var caves = config.Caves;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = bedrockLevel + 1; y < Math.Min(worldHeight - 4, heightMap[x, z]); y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        bool nearBottom = y < bedrockLevel + 6;
                        if (nearBottom && random.NextDouble() < caves.LavaThreshold)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Lava);
                            continue;
                        }

                        bool flooded = y < seaLevel - 6 && random.NextDouble() < caves.WaterThreshold;
                        chunk.SetBlock(x, y, z, flooded ? BlockType.Water : BlockType.Air);
                    }
                }
            }
        }

        private void ApplyHydrology(ChunkData chunk, int[,] heightMap, float[,]? riverMask, float[,]? lakeMask)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    float river = riverMask != null ? riverMask[x, z] : 0f;
                    float lake = lakeMask != null ? lakeMask[x, z] : 0f;

                    bool hasRiver = river > config.Water.RiverCenterThreshold;
                    bool hasLake = lake > config.Lakes.ShorelineBlend;
                    float wetland = Math.Max(river, lake);

                    if (hasRiver)
                    {
                        int depth = Math.Clamp((int)(config.Water.RiverDepth * river), 2, config.Water.RiverDepth + 2);
                        for (int d = 0; d < depth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        // Feather river banks to reduce jagged edges and surface seams
                        int bankDepth = Math.Max(1, (int)(config.Water.RiverEdgeFeather * 4));
                        for (int b = 0; b < bankDepth; b++)
                        {
                            int bankY = Math.Max(bedrockLevel + 1, surface - b);
                            var current = chunk.GetBlock(x, bankY, z);
                            if (current == BlockType.Grass || current == BlockType.Dirt)
                            {
                                chunk.SetBlock(x, bankY, z, b == 0 ? BlockType.Sand : BlockType.Dirt);
                            }
                        }
                    }
                    else if (hasLake)
                    {
                        int depth = Math.Clamp((int)(config.Lakes.MaxDepth * lake), config.Lakes.MinDepth, config.Lakes.MaxDepth);
                        int shelfDepth = Math.Clamp(config.Lakes.ShelfDepth, 1, depth);

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
                    if (!hasRiver && !hasLake && wetland > 0.35f && chunk.GetBlock(x, surface, z) == BlockType.Grass)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Dirt);
                    }
                }
            }
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
