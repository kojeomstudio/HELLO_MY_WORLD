using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerApp;
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
            float[,]? hydrologyMask = improvedMasks?.Hydrology;
            float[,]? flowAccumulation = improvedMasks?.FlowAccumulation;

            if (caveMask != null)
            {
                CarveCaves(chunk, caveMask, heightMap);
            }

            ApplyHydrology(chunk, heightMap, riverMask, lakeMask, hydrologyMask, flowAccumulation);
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
                    double flowAlignment = ComputeFlowAlignment(heightMap, x, z) * water.RiverFlowAlignmentWeight;
                    double headwaterStability = 1.0 - Math.Min(1.0, Math.Abs(heightMap[x, z] - seaLevel) * water.RiverHeadwaterStabilityWeight * 0.01);

                    intensity = intensity * continuity * headwaterStability + flowAlignment;
                    intensity -= anisotropy + reliefPenalty;
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
            BoostRiverConfluences(mask, water.RiverConfluenceBoost);
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
                    double inflow = riverMask != null ? riverMask[x, z] * config.Water.LakeInflowBlendWeight : 0.0;
                    double riverSuppression = riverMask != null ? riverMask[x, z] * lakeConfig.RiverProximitySuppression : 0.0;
                    double altitudePenalty = Math.Max(0, seaLevel - heightMap[x, z]) * 0.0025;
                    double slopePenalty = ComputeSlope(heightMap, x, z) * config.Water.LakeRimErosionWeight * 0.05;
                    weight += inflow;
                    weight -= riverSuppression + altitudePenalty + slopePenalty;

                    double wetlandThreshold = lakeConfig.WetlandSaturationThreshold - inflow * 0.1;
                    if (weight > wetlandThreshold && heightMap[x, z] > bedrockLevel + lakeConfig.MinDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            Smooth2D(lakes, lakeConfig.LakeBasinSmoothIterations, config.Water.HydrologySmoothBlend);
            RelaxEdges(lakes, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            ApplyWetlandBuffer(lakes, lakeConfig.WetlandBufferRadius, lakeConfig.ShorelineBlend);
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

                        bool isEdge = x == 0 || z == 0 || x == chunkSize - 1 || z == chunkSize - 1;
                        if (isEdge && random.NextDouble() < config.Caves.EdgeSealStrength)
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

            AddCaveSupports(chunk, caves);
        }

        private void ApplyHydrology(ChunkData chunk, int[,] heightMap, float[,]? riverMask, float[,]? lakeMask, float[,]? hydrologyMask = null, float[,]? flowMask = null)
        {
            if (riverMask != null)
            {
                FeatherMaskEdges(riverMask, config.Water.RiverEdgeFeather, config.Water.RiverSeamFillStrength);
            }

            if (lakeMask != null)
            {
                FeatherMaskEdges(lakeMask, config.Water.HydrologySmoothBlend * 0.25, config.Water.HydrologySeamRelaxBlend * 0.5);
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    float river = riverMask != null ? riverMask[x, z] : 0f;
                    float lake = lakeMask != null ? lakeMask[x, z] : 0f;
                    float hydrology = hydrologyMask != null ? hydrologyMask[x, z] : 0f;
                    float flow = flowMask != null ? flowMask[x, z] : 0f;

                    bool hasRiver = river > config.Water.RiverCenterThreshold;
                    bool hasLake = lake > config.Lakes.ShorelineBlend;
                    float wetland = Math.Max(Math.Max(river, lake), hydrology * (float)Math.Clamp(config.Water.RiparianSaturationBoost, 0.0, 1.0));

                    if (hasRiver)
                    {
                        float saturation = Math.Max(river, hydrology);
                        int depth = Math.Clamp((int)(config.Water.RiverDepth * (river + hydrology * 0.5f + flow * 0.35f + saturation * 0.15f)), 2, config.Water.RiverDepth + 3);
                        for (int d = 0; d < depth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        // Feather river banks to reduce jagged edges and surface seams
                        int bankDepth = Math.Max(1, (int)(config.Water.RiverEdgeFeather * 4 + hydrology * 2));
                        for (int b = 0; b < bankDepth; b++)
                        {
                            int bankY = Math.Max(bedrockLevel + 1, surface - b);
                            var current = chunk.GetBlock(x, bankY, z);
                            if (current == BlockType.Grass || current == BlockType.Dirt)
                            {
                                chunk.SetBlock(x, bankY, z, b == 0 ? BlockType.Sand : BlockType.Dirt);
                            }
                        }

                        ErodeRiverBanks(chunk, x, z, surface, bankDepth, config.Water.RiverBankErosionWeight);
                        ApplyRiverMouthBlend(chunk, x, z, surface, config.Water.RiverMouthSmoothRadius);
                        if (surface <= seaLevel + config.Water.RiverMouthSmoothRadius && config.Water.RiverDeltaWetlandStrength > 0)
                        {
                            int deltaDepth = Math.Max(1, (int)(config.Water.RiverDeltaWetlandStrength * 2));
                            for (int d = 0; d < deltaDepth; d++)
                            {
                                int dy = Math.Max(bedrockLevel + 1, surface - d);
                                if (chunk.GetBlock(x, dy, z) == BlockType.Grass)
                                {
                                    chunk.SetBlock(x, dy, z, BlockType.Sand);
                                }
                            }
                        }
                    }
                    else if (hasLake)
                    {
                        int depth = Math.Clamp((int)(config.Lakes.MaxDepth * (lake + hydrology * 0.25f)), config.Lakes.MinDepth, config.Lakes.MaxDepth);
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

                        ApplyWetlandRing(chunk, x, z, surface, config.Lakes.WetlandBufferRadius);
                    }
                    else if ((hydrology > 0.6f || flow > 0.85f) && chunk.GetBlock(x, surface, z) == BlockType.Grass)
                    {
                        int shallowDepth = Math.Max(1, (int)Math.Min(config.Water.RiverDepth, (hydrology + flow) * 2f));
                        for (int d = 0; d < shallowDepth; d++)
                        {
                            int y = Math.Max(bedrockLevel + 1, surface - d);
                            chunk.SetBlock(x, y, z, BlockType.Water);
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
                        if (hydrology > 0.55f && surface <= seaLevel + 1 && chunk.GetBlock(x, surface + 1, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Water);
                        }
                    }
                }
            }
        }

        private static void FeatherMaskEdges(float[,] mask, double feather, double seamFill)
        {
            feather = Math.Clamp(feather, 0.0, 1.0);
            seamFill = Math.Clamp(seamFill, 0.0, 1.0);
            if (feather <= 0.0 && seamFill <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var buffer = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float centre = mask[x, z];
                    float neighbour = TerrainMaskUtility.Clamp01(SampleInterior(mask, x, z));
                    float blended = (float)(centre * (1.0 - feather) + neighbour * feather);
                    buffer[x, z] = Math.Max(blended, centre * (float)(1.0 - seamFill));
                }
            }

            Array.Copy(buffer, mask, buffer.Length);
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

        private void BoostRiverConfluences(float[,] field, double confluenceBoost)
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

        private void ApplyWetlandBuffer(float[,] field, int radius, double shorelineBlend)
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
                    float center = field[x, z];
                    if (center <= 0f)
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

                            float falloff = 1f - (Math.Abs(dx) + Math.Abs(dz)) / (float)(radius + 1);
                            float influence = Math.Clamp(center * (float)shorelineBlend * falloff, 0f, 1f);
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
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

        private double ComputeFlowAlignment(int[,] heightMap, int x, int z)
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

        private void AddCaveSupports(ChunkData chunk, CaveConfig caves)
        {
            double chance = Math.Clamp(caves.SupportPillarChance, 0.0, 1.0);
            if (chance <= 0.0)
            {
                return;
            }

            int maxY = Math.Min(worldHeight - 4, seaLevel);
            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    if (random.NextDouble() > chance)
                    {
                        continue;
                    }

                    int baseY = random.Next(bedrockLevel + 2, Math.Max(bedrockLevel + 3, maxY - 6));
                    int height = random.Next(2, 5);
                    for (int i = 0; i < height; i++)
                    {
                        int y = baseY + i;
                        var current = chunk.GetBlock(x, y, z);
                        if (current == BlockType.Air || current == BlockType.Water)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }
            }
        }

        private void ErodeRiverBanks(ChunkData chunk, int x, int z, int surface, int bankDepth, double erosionWeight)
        {
            if (erosionWeight <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, (int)Math.Round(erosionWeight * 4));
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Math.Abs(dx) + Math.Abs(dz) > radius)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, surface - bankDepth);
                    var block = chunk.GetBlock(nx, ny, nz);
                    if (block == BlockType.Grass)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Dirt);
                    }
                    else if (block == BlockType.Dirt && ny <= seaLevel + 1)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Sand);
                    }
                }
            }
        }

        private void ApplyRiverMouthBlend(ChunkData chunk, int x, int z, int surface, int radius)
        {
            radius = Math.Max(0, radius);
            if (radius == 0 || surface > seaLevel + radius)
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, seaLevel - Math.Max(0, Math.Abs(dx) + Math.Abs(dz) - 1));
                    for (int y = ny; y <= seaLevel; y++)
                    {
                        if (chunk.GetBlock(nx, y, nz) == BlockType.Air || chunk.GetBlock(nx, y, nz) == BlockType.Dirt)
                        {
                            chunk.SetBlock(nx, y, nz, BlockType.Water);
                        }
                    }
                }
            }
        }

        private void ApplyWetlandRing(ChunkData chunk, int x, int z, int surface, int radius)
        {
            radius = Math.Max(0, radius);
            if (radius == 0)
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                    {
                        continue;
                    }

                    int ny = Math.Max(bedrockLevel + 1, surface - 1);
                    var block = chunk.GetBlock(nx, ny, nz);
                    if (block == BlockType.Grass)
                    {
                        chunk.SetBlock(nx, ny, nz, BlockType.Dirt);
                    }

                    if (ny < seaLevel && chunk.GetBlock(nx, ny + 1, nz) == BlockType.Air)
                    {
                        chunk.SetBlock(nx, ny + 1, nz, BlockType.Water);
                    }
                }
            }
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
