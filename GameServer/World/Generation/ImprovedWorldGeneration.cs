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

            StabilizeCaveEntrances(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation);
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

            SmoothScalarField(hydrologyField.HydrologyMask, 1, Math.Clamp(_hydrologySmoothBlend + 0.08, 0.0, 0.95));

            GenerateLakesInternal(context);

            EnhanceLakeShoreline(chunk, surfaceCache, hydrologyField);
            ReinforceLakeWetlands(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation);
        }

        private void StabilizeCaveEntrances(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
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

                    double moisture = Math.Clamp(hydrologyMask[x, z] + flowAccumulation[x, z] * _caveMoistureRetentionWeight, 0.0, 1.0);
                    int startY = Math.Max(1, surface - 3);
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

                    int channelSurface = Math.Max(1, surface - Math.Max(1, _riverDepth - 2));
                    for (int y = surface; y >= channelSurface; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    for (int dx = -maxRadius; dx <= maxRadius; dx++)
                    {
                        for (int dz = -maxRadius; dz <= maxRadius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > maxRadius + 0.1)
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
                            double pressure = Math.Clamp((hydrology + flow) * 0.5, 0.0, 1.0);

                            var bankMaterial = pressure > 0.65 ? BlockType.Clay : BlockType.Sand;
                            int bankTop = Math.Max(channelSurface, neighborSurface - 1);
                            chunk.SetBlock(nx, bankTop, nz, bankMaterial);
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
                    var rimMaterial = hydrology + flow > 1.0 ? BlockType.Clay : BlockType.Sand;

                    chunk.SetBlock(x, solidSurface, z, rimMaterial);
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

        private void ReinforceLakeWetlands(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    if (hydrology < 0.55 && flow < 0.25)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0 || surface > GlobalWaterLevel + 2 || surface < GlobalWaterLevel - 3)
                    {
                        continue;
                    }

                    int wetlandFloor = Math.Max(1, surface - 1);
                    chunk.SetBlock(x, wetlandFloor, z, hydrology > 0.75 ? BlockType.Clay : BlockType.Sand);
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
