using System;
using GameServerApp.Models;
using GameServerApp.World.Generation;

namespace GameServerApp.World
{
    /// <summary>
    /// Improved terrain feature passes that layer stabilization and hydrology-aware refinements
    /// on top of the existing generation routines.
    /// </summary>
    public partial class WorldManager
    {
        public void GenerateImprovedCavesInternal(TerrainGenerationContext context)
        {
            if (!_enableCaves)
            {
                return;
            }

            var chunk = context.Chunk;
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyField = GetHydrologyField(context, surfaceCache);

            SmoothScalarField(hydrologyField.HydrologyMask, Math.Max(1, _caveStabilitySmoothIterations), Math.Clamp(_caveStabilitySmoothBlend + 0.05, 0.0, 0.95));
            SmoothScalarField(hydrologyField.FlowAccumulation, 1, Math.Clamp(_caveStabilitySmoothBlend + 0.1, 0.0, 0.95));

            GenerateCavesInternal(context);

            StabilizeCaveEntrances(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, hydrologyField.HydrologyCurvature);
        }

        public void GenerateImprovedRiversInternal(TerrainGenerationContext context)
        {
            if (!_enableRivers)
            {
                return;
            }

            var chunk = context.Chunk;
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyField = GetHydrologyField(context, surfaceCache);

            SmoothScalarField(hydrologyField.HydrologyMask, 1, Math.Clamp(_hydrologySmoothBlend + 0.05, 0.0, 0.95));
            SmoothScalarField(hydrologyField.FlowAccumulation, 1, Math.Clamp(_hydrologySmoothBlend + 0.05, 0.0, 0.95));

            GenerateRiversInternal(context);

            var riverField = GetRiverFieldCache(context);
            EnhanceRiverBanks(chunk, surfaceCache, hydrologyField, riverField.Intensity);
        }

        public void GenerateImprovedLakesInternal(TerrainGenerationContext context)
        {
            if (!_enableLakes)
            {
                return;
            }

            var chunk = context.Chunk;
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyField = GetHydrologyField(context, surfaceCache);

            int basinSmoothIterations = Math.Max(1, _lakeBasinSmoothIterations);
            double basinSmoothBlend = Math.Clamp(_hydrologySmoothBlend + 0.08, 0.0, 0.95);
            SmoothScalarField(hydrologyField.HydrologyMask, basinSmoothIterations, basinSmoothBlend);
            SmoothScalarField(hydrologyField.FlowAccumulation, basinSmoothIterations, Math.Clamp(basinSmoothBlend + 0.02, 0.0, 0.95));
            if (hydrologyField.HydrologyCurvature != null)
            {
                SmoothScalarField(hydrologyField.HydrologyCurvature, basinSmoothIterations, basinSmoothBlend);
            }

            GenerateLakesInternal(context);

            EnhanceLakeShoreline(chunk, surfaceCache, hydrologyField);
            ReinforceLakeWetlands(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, hydrologyField.HydrologyCurvature);
        }

        private void StabilizeCaveEntrances(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation, double[,]? hydrologyCurvature = null)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface < 4)
                    {
                        continue;
                    }

                    double curvature = hydrologyCurvature?[x, z] ?? 0.0;
                    double moisture = Math.Clamp(hydrologyMask[x, z] + flowAccumulation[x, z] * _caveMoistureRetentionWeight + curvature * _hydrologyCurvatureWeight * 0.35, 0.0, 1.0);
                    int taperDepth = Math.Max(2, (int)Math.Round(3 + curvature * _hydrologyCurvatureWeight * 2.0));
                    int startY = Math.Max(1, surface - taperDepth);
                    bool patched = false;

                    for (int y = surface; y >= startY; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air)
                        {
                            var fill = moisture > 0.65 ? BlockType.Cobblestone : BlockType.Stone;
                            chunk.SetBlock(x, y, z, fill);
                            patched = true;
                        }
                    }

                    if (patched && surface + 1 < 256)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void EnhanceRiverBanks(ChunkData chunk, int[,] surfaceCache, HydrologyFieldCache hydrologyField, double[,] riverIntensity)
        {
            int maxRadius = 2;
            var curvatureField = hydrologyField.HydrologyCurvature;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (riverIntensity[x, z] < RiverCenterThreshold || IsOceanColumn(chunk, x, z))
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    double curvature = curvatureField[x, z];
                    double confluenceBoost = Math.Clamp(curvature * _hydrologyCurvatureWeight + hydrologyField.FlowAccumulation[x, z] * 0.08, 0.0, 1.2);
                    int channelRadius = maxRadius + (confluenceBoost > 0.6 ? 1 : 0);
                    int channelDepth = Math.Max(1, (int)Math.Round((_riverDepth - 2) * (1.0 + confluenceBoost * _riverConfluenceBoost)));
                    int channelSurface = Math.Max(1, surface - channelDepth);

                    for (int y = surface; y >= channelSurface; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    for (int dx = -channelRadius; dx <= channelRadius; dx++)
                    {
                        for (int dz = -channelRadius; dz <= channelRadius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > channelRadius + 0.1)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            double hydrology = hydrologyField.HydrologyMask[nx, nz];
                            double flow = hydrologyField.FlowAccumulation[nx, nz];
                            double pressure = Math.Clamp((hydrology + flow) * 0.5 + confluenceBoost * 0.5, 0.0, 1.25);

                            var bankMaterial = pressure > 0.65 ? BlockType.Clay : BlockType.Sand;
                            int bankTop = Math.Max(channelSurface, neighborSurface - 1);
                            chunk.SetBlock(nx, bankTop, nz, bankMaterial);
                            if (confluenceBoost > 0.55 && bankTop > channelSurface)
                            {
                                chunk.SetBlock(nx, bankTop - 1, nz, bankMaterial);
                            }
                            if (bankTop + 1 < 256)
                            {
                                chunk.SetBlock(nx, bankTop + 1, nz, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        private void EnhanceLakeShoreline(ChunkData chunk, int[,] surfaceCache, HydrologyFieldCache hydrologyField)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (IsOceanColumn(chunk, x, z))
                    {
                        continue;
                    }

                    int waterSurface = FindTopWaterLevel(chunk, x, z);
                    if (waterSurface < 1)
                    {
                        continue;
                    }

                    int solidSurface = surfaceCache[x, z];
                    if (solidSurface <= 0 || solidSurface < waterSurface - 2 || solidSurface > waterSurface + 4)
                    {
                        continue;
                    }

                    double hydrology = hydrologyField.HydrologyMask[x, z];
                    double flow = hydrologyField.FlowAccumulation[x, z];
                    double curvature = hydrologyField.HydrologyCurvature?[x, z] ?? 0.0;
                    double curvatureBoost = Math.Clamp(curvature * _hydrologyCurvatureWeight, 0.0, 1.2);
                    var rimMaterial = hydrology + flow + curvatureBoost > 1.0 ? BlockType.Clay : BlockType.Sand;

                    chunk.SetBlock(x, solidSurface, z, rimMaterial);
                    if (curvatureBoost > 0.55 && solidSurface - 1 > 0)
                    {
                        chunk.SetBlock(x, solidSurface - 1, z, rimMaterial);
                    }
                    for (int y = waterSurface; y <= Math.Min(waterSurface + 1, 255); y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }

                    if (solidSurface + 1 < 256)
                    {
                        chunk.SetBlock(x, solidSurface + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void ReinforceLakeWetlands(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation, double[,]? hydrologyCurvature = null)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    double curvature = hydrologyCurvature?[x, z] ?? 0.0;
                    double curvatureBoost = Math.Clamp(curvature * _hydrologyCurvatureWeight, 0.0, 1.0);
                    double wetness = hydrology + flow * 0.5 + curvatureBoost * 0.6;
                    if (wetness < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0 || surface > GlobalWaterLevel + 2 || surface < GlobalWaterLevel - 3)
                    {
                        continue;
                    }

                    int wetlandFloor = Math.Max(1, surface - 1 - (int)Math.Round(curvatureBoost * _lakeBasinSmoothIterations * 0.5));
                    chunk.SetBlock(x, wetlandFloor, z, hydrology + curvatureBoost > 0.9 ? BlockType.Clay : BlockType.Sand);
                    chunk.SetBlock(x, wetlandFloor + 1, z, BlockType.Water);
                    if (wetlandFloor + 2 < 256)
                    {
                        chunk.SetBlock(x, wetlandFloor + 2, z, BlockType.Air);
                    }
                }
            }
        }

        private int FindTopWaterLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block == BlockType.Water)
                {
                    return y;
                }

                if (block != BlockType.Air)
                {
                    break;
                }
            }

            return -1;
        }
    }
}
