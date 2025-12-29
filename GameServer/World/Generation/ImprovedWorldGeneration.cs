using System;
using GameServerApp.Models;
using GameServerApp.World.Generation;
using System.Numerics;

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
            var riverField = _enableRivers ? GetRiverFieldCache(context) : null;
            var riparianSaturation = GetRiparianSaturation(context, hydrologyField, riverField);

            SmoothScalarField(hydrologyField.HydrologyMask, Math.Max(1, _caveStabilitySmoothIterations), Math.Clamp(_caveStabilitySmoothBlend + 0.05, 0.0, 0.95));
            SmoothScalarField(hydrologyField.FlowAccumulation, 1, Math.Clamp(_caveStabilitySmoothBlend + 0.1, 0.0, 0.95));

            GenerateCavesInternal(context);

            AddCaveSupportPillars(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation);
            StabilizeCaveEntrances(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, hydrologyField.HydrologyCurvature);
            ReinforceCaveCeilings(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, riparianSaturation);
            SealChunkEdgeCaves(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation);
            SealRiparianCaves(chunk, surfaceCache, riparianSaturation);
            ApplyCaveHydrologyBarriers(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, riparianSaturation);
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
            HarmonizeRiverFieldWithHydrology(riverField, hydrologyField);
            BridgeRiverSeams(chunk, surfaceCache, riverField, hydrologyField.HydrologyMask);
            var riparianSaturation = GetRiparianSaturation(context, hydrologyField, riverField);
            EnhanceRiverBanks(chunk, surfaceCache, hydrologyField, riverField, riparianSaturation);
            SmoothRiverMouths(chunk, surfaceCache, riverField.Intensity, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, riparianSaturation);
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
            var riverField = GetRiverFieldCache(context);
            var riparianSaturation = GetRiparianSaturation(context, hydrologyField, riverField);

            int basinSmoothIterations = Math.Max(1, _lakeBasinSmoothIterations);
            double basinSmoothBlend = Math.Clamp(_hydrologySmoothBlend + 0.08, 0.0, 0.95);
            SmoothScalarField(hydrologyField.HydrologyMask, basinSmoothIterations, basinSmoothBlend);
            SmoothScalarField(hydrologyField.FlowAccumulation, basinSmoothIterations, Math.Clamp(basinSmoothBlend + 0.02, 0.0, 0.95));
            if (hydrologyField.HydrologyCurvature != null)
            {
            SmoothScalarField(hydrologyField.HydrologyCurvature, basinSmoothIterations, basinSmoothBlend);
            }

            GenerateLakesInternal(context);

            StabilizeLakeRims(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.HydrologyGradient);
            EnhanceLakeShoreline(chunk, surfaceCache, hydrologyField, riparianSaturation);
            ReinforceLakeWetlands(chunk, surfaceCache, hydrologyField.HydrologyMask, hydrologyField.FlowAccumulation, riparianSaturation, hydrologyField.HydrologyCurvature);
            ExtendLakeWetlands(chunk, surfaceCache, riparianSaturation, hydrologyField.FlowAccumulation);
            CreateLakeOutflowChannels(chunk, surfaceCache, hydrologyField.FlowAccumulation, hydrologyField.HydrologyMask);
            BlendLakeWithRivers(chunk, surfaceCache, hydrologyField, riverField);
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

        private void ReinforceCaveCeilings(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation, double[,] riparianSaturation)
        {
            if (_caveCeilingStabilityWeight <= 0.0)
            {
                return;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    double wetness = Math.Clamp(hydrologyMask[x, z] * 0.55 + flowAccumulation[x, z] * 0.35 + riparianSaturation[x, z] * 0.25, 0.0, 1.75);
                    if (wetness < 0.25)
                    {
                        continue;
                    }

                    int spanHeight = Math.Max(1, top - bottom);
                    int reinforceThickness = Math.Clamp((int)Math.Round(1 + wetness * 3.0 * _caveCeilingStabilityWeight), 1, Math.Min(4, spanHeight));
                    int reinforceStart = Math.Max(bottom + 1, top - reinforceThickness + 1);
                    var filler = wetness > 0.9 ? BlockType.Cobblestone : BlockType.Stone;

                    for (int y = reinforceStart; y <= top; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
                        {
                            chunk.SetBlock(x, y, z, filler);
                        }
                    }

                    int airCap = Math.Min(255, top + 1);
                    if (airCap < 256)
                    {
                        chunk.SetBlock(x, airCap, z, BlockType.Air);
                    }
                }
            }
        }

        private void SealChunkEdgeCaves(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_caveEdgeSealStrength <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double sealBase = Math.Clamp(_caveEdgeSealStrength, 0.0, 1.0);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(15 - x, 15 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 6)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surface, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 4)
                    {
                        continue;
                    }

                    double wetness = Math.Clamp(hydrologyMask[x, z] + flowAccumulation[x, z] * 0.15, 0.0, 1.0);
                    double edgeBlend = Math.Clamp(sealBase * (1.0 - edgeDistance / (double)radius) * (0.85 + wetness * 0.35), 0.0, 1.0);
                    int sealThickness = Math.Clamp((int)Math.Round(1 + cavityHeight * (0.15 + wetness * 0.2)), 1, Math.Min(3, cavityHeight - 1));
                    int sealStart = Math.Max(bottom + cavityHeight - sealThickness, bottom + 1);
                    var fillBlock = wetness > 0.6 ? BlockType.Cobblestone : BlockType.Stone;

                    for (int y = top; y >= sealStart; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
                        {
                            double selector = SampleDeterministicNoise(x * 41 + z * 17, y * 13 + surface, 0x6CA5E0FF);
                            if (selector <= edgeBlend)
                            {
                                chunk.SetBlock(x, y, z, fillBlock);
                                surfaceCache[x, z] = Math.Max(surfaceCache[x, z], y);
                            }
                        }
                    }
                }
            }
        }

        private void SealRiparianCaves(ChunkData chunk, int[,] surfaceCache, double[,] riparianSaturation)
        {
            if (!_enableCaves)
            {
                return;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double saturation = riparianSaturation[x, z];
                    if (saturation < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 6)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surface, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 3)
                    {
                        continue;
                    }

                    int sealThickness = Math.Clamp((int)Math.Round(1 + saturation * 2.0), 1, Math.Min(4, cavityHeight - 1));
                    int sealStart = Math.Max(bottom + 1, top - sealThickness + 1);
                    var fillBlock = saturation > 0.85 ? BlockType.Cobblestone : BlockType.Stone;

                    for (int y = top; y >= sealStart; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
                        {
                            chunk.SetBlock(x, y, z, fillBlock);
                            surfaceCache[x, z] = Math.Max(surfaceCache[x, z], y);
                        }
                    }

                    if (_caveRiparianPlugDepth > 0 && saturation > 0.65)
                    {
                        int plugDepth = Math.Min(_caveRiparianPlugDepth, cavityHeight - 1);
                        int plugTop = Math.Max(bottom + plugDepth, sealStart - 1);
                        int plugBottom = Math.Max(bottom + 1, plugTop - plugDepth + 1);

                        for (int y = plugTop; y >= plugBottom; y--)
                        {
                            var block = chunk.GetBlock(x, y, z);
                            if (block == BlockType.Air || block == BlockType.Water)
                            {
                                chunk.SetBlock(x, y, z, fillBlock);
                            }
                        }
                    }

                    if (saturation > 0.95 && bottom + 1 < 256 && chunk.GetBlock(x, bottom + 1, z) == BlockType.Air)
                    {
                        chunk.SetBlock(x, bottom + 1, z, BlockType.Water);
                    }
                }
            }
        }

        private void ApplyCaveHydrologyBarriers(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation, double[,] riparianSaturation)
        {
            if (_caveRiparianPlugDepth <= 0)
            {
                return;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    double saturation = riparianSaturation[x, z];
                    double moisture = Math.Clamp(hydrology * 0.55 + flow * _caveMoistureRetentionWeight * 0.4 + saturation * 0.45, 0.0, 1.8);
                    if (moisture < 0.55)
                    {
                        continue;
                    }

                    int spanHeight = Math.Max(1, top - bottom);
                    int barrierDepth = Math.Clamp((int)Math.Round(_caveRiparianPlugDepth * (0.6 + moisture * 0.4)), 1, Math.Min(spanHeight, 6));
                    int sealStart = Math.Max(bottom + 1, top - barrierDepth + 1);
                    var filler = moisture > 1.1 ? BlockType.Cobblestone : BlockType.Stone;

                    for (int y = sealStart; y <= top; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
                        {
                            chunk.SetBlock(x, y, z, filler);
                        }
                    }

                    if (top + 1 < 256)
                    {
                        chunk.SetBlock(x, top + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void HarmonizeRiverFieldWithHydrology(RiverFieldCache riverField, HydrologyFieldCache hydrologyField)
        {
            var gradientField = hydrologyField.HydrologyGradient;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = riverField.Intensity[x, z];
                    double hydrology = hydrologyField.HydrologyMask[x, z];
                    double flow = hydrologyField.FlowAccumulation[x, z];
                    double gradientMag = gradientField?[x, z].Length() ?? 0.0;

                    var flowDir = riverField.Flow[x, z];
                    double alignment = 0.0;
                    if (gradientField != null && flowDir.LengthSquared() > 1e-6f)
                    {
                        var gradDir = gradientField[x, z];
                        if (gradDir.LengthSquared() > 1e-6f)
                        {
                            alignment = Math.Abs(Vector2.Dot(Vector2.Normalize(flowDir), Vector2.Normalize(gradDir)));
                        }
                    }

                    double stabilityDamp = 1.0 - Math.Clamp(gradientMag * _riverGradientPenalty * 0.35, 0.0, 0.55);
                    double flowBoost = Math.Clamp(flow * (_riverHeadwaterStabilityWeight * 0.6 + 0.12), 0.0, 1.2);
                    double hydrologyBias = Math.Clamp(hydrology * (_riverFlowAlignmentWeight * 0.35 + 0.18), 0.0, 1.05);
                    double anisotropy = 1.0 - Math.Clamp(alignment * _riverAnisotropyWeight * 0.25, 0.0, 0.35);

                    double adjusted = intensity * stabilityDamp * anisotropy + hydrologyBias * RiverBankThreshold + flowBoost * 0.08;
                    riverField.Intensity[x, z] = Math.Clamp(adjusted, 0.0, RiverBankThreshold * 1.35);
                }
            }
        }

        private void BridgeRiverSeams(ChunkData chunk, int[,] surfaceCache, RiverFieldCache riverField, double[,] hydrologyMask)
        {
            if (_riverSeamFillStrength <= 0.0)
            {
                return;
            }

            int edgeRadius = Math.Max(1, _riparianBufferRadius);
            double seamBoost = Math.Clamp(_riverSeamFillStrength, 0.0, 2.0);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    bool nearEdge = x < edgeRadius || z < edgeRadius || x >= 16 - edgeRadius || z >= 16 - edgeRadius;
                    if (!nearEdge)
                    {
                        continue;
                    }

                    double riverPressure = riverField.Intensity[x, z];
                    if (riverPressure < RiverCenterThreshold * 0.6)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double seamStrength = Math.Clamp(riverPressure / Math.Max(RiverCenterThreshold, 1e-5) * seamBoost, 0.0, 3.0);
                    seamStrength = Math.Clamp(seamStrength * 0.7 + hydrology * 0.9, 0.0, 3.5);
                    int carveDepth = Math.Clamp((int)Math.Round(_riverDepth * 0.5 + seamStrength * 1.5), 1, Math.Max(_riverDepth + 2, 4));
                    int startY = Math.Max(1, surface - carveDepth);

                    for (int y = surface; y >= startY; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water || block == BlockType.Sand || block == BlockType.Clay)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }

                    if (surface + 1 < 256)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void EnhanceRiverBanks(ChunkData chunk, int[,] surfaceCache, HydrologyFieldCache hydrologyField, RiverFieldCache riverField, double[,] riparianSaturation)
        {
            var riverIntensity = riverField.Intensity;
            int maxRadius = 2;
            var curvatureField = hydrologyField.HydrologyCurvature;
            var gradientField = hydrologyField.HydrologyGradient;
            var hydrologyMask = hydrologyField.HydrologyMask;
            var flowAccumulation = hydrologyField.FlowAccumulation;

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

                    Vector2 flowDir = riverField.Flow[x, z];
                    if (flowDir.LengthSquared() <= 1e-6f && gradientField != null)
                    {
                        flowDir = gradientField[x, z];
                    }

                    double flowStrength = Math.Clamp(flowDir.Length(), 0.0, 2.0);
                    double curvature = curvatureField[x, z];
                    double confluenceBoost = Math.Clamp(curvature * _hydrologyCurvatureWeight + hydrologyField.FlowAccumulation[x, z] * 0.08, 0.0, 1.2);
                    double gradientMagnitude = gradientField != null ? gradientField[x, z].Length() : 0.0;
                    double anisotropy = Math.Clamp(flowStrength * _riverAnisotropyWeight + gradientMagnitude * _riverFlowAlignmentWeight * 0.35, 0.0, 1.25);
                    double gradientPenalty = Math.Clamp(gradientMagnitude * _riverGradientPenalty, 0.0, 1.25);
                    double headwaterBoost = Math.Clamp((1.0 - Math.Clamp(flowStrength, 0.0, 1.0)) * _riverHeadwaterStabilityWeight, 0.0, 0.6);
                    double riparian = riparianSaturation[x, z];
                    double saturationBias = Math.Clamp(riparian * 0.65 + hydrologyMask[x, z] * 0.25 + flowAccumulation[x, z] * 0.15, 0.0, 1.5);
                    int channelRadius = Math.Clamp(maxRadius + (confluenceBoost > 0.6 ? 1 : 0) + (anisotropy > 0.35 ? 1 : 0) - (int)Math.Round(gradientPenalty * 0.5), 1, 3);
                    channelRadius = Math.Clamp(channelRadius + (saturationBias > 0.55 ? 1 : 0), 1, 3);
                    int channelDepth = Math.Max(1, (int)Math.Round((_riverDepth - 2) * (1.0 + confluenceBoost * _riverConfluenceBoost + anisotropy * 0.35) - gradientPenalty * _riverReliefPenaltyWeight * 2.0));
                    channelDepth = Math.Clamp(channelDepth + (int)Math.Round(saturationBias * 1.2), 1, Math.Max(_riverDepth + 2, 3));
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
                            double pressure = Math.Clamp((hydrology + flow) * 0.5 + confluenceBoost * 0.5 + headwaterBoost * 0.25, 0.0, 1.35);
                            pressure = Math.Clamp(pressure + saturationBias * 0.2, 0.0, 1.5);

                            if (gradientPenalty > 0.9 && distance > channelRadius - 0.2)
                            {
                                continue;
                            }

                            var bankMaterial = pressure + anisotropy > 0.7 && gradientPenalty < 0.9 ? BlockType.Clay : BlockType.Sand;
                            if (saturationBias + anisotropy > 0.9 && gradientPenalty < 0.95)
                            {
                                bankMaterial = BlockType.Clay;
                            }
                            if (gradientPenalty > 0.95)
                            {
                                bankMaterial = BlockType.Stone;
                            }
                            int bankTop = Math.Max(channelSurface, neighborSurface - 1);
                            chunk.SetBlock(nx, bankTop, nz, bankMaterial);
                            if (confluenceBoost > 0.55 && bankTop > channelSurface)
                            {
                                chunk.SetBlock(nx, bankTop - 1, nz, bankMaterial);
                            }
                            if (saturationBias > 0.9 && bankTop + 1 < 256)
                            {
                                chunk.SetBlock(nx, Math.Max(channelSurface, bankTop - 1), nz, BlockType.Water);
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

        private void StabilizeLakeRims(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, Vector2[,]? hydrologyGradient)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface < 1 || surface >= 255)
                    {
                        continue;
                    }

                    var topBlock = chunk.GetBlock(x, surface, z);
                    if (topBlock != BlockType.Water)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double gradientMag = hydrologyGradient?[x, z].Length() ?? 0.0;
                    double rimStrength = Math.Clamp(hydrology * 0.6 + gradientMag * _lakeRimErosionWeight * 0.8, 0.0, 1.4);
                    if (rimStrength < 0.25)
                    {
                        continue;
                    }

                    SculptLakeBank(chunk, x, z, surface, rimStrength);
                    if (surface + 1 < 256)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void EnhanceLakeShoreline(ChunkData chunk, int[,] surfaceCache, HydrologyFieldCache hydrologyField, double[,] riparianSaturation)
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
                    double riparian = riparianSaturation[x, z];
                    double rimBias = hydrology + flow + curvatureBoost + riparian * 0.5;
                    var rimMaterial = rimBias > 1.0 ? BlockType.Clay : BlockType.Sand;

                    chunk.SetBlock(x, solidSurface, z, rimMaterial);
                    if (curvatureBoost > 0.55 && solidSurface - 1 > 0)
                    {
                        chunk.SetBlock(x, solidSurface - 1, z, rimMaterial);
                    }
                    int shelfDepth = Math.Max(0, _lakeShelfDepth);
                    if (shelfDepth > 0 && solidSurface >= waterSurface - shelfDepth && solidSurface <= waterSurface + shelfDepth + 1)
                    {
                        int shelfStart = Math.Max(1, waterSurface - shelfDepth);
                        for (int y = shelfStart; y < waterSurface; y++)
                        {
                            var block = chunk.GetBlock(x, y, z);
                            if (block == BlockType.Air)
                            {
                                chunk.SetBlock(x, y, z, rimMaterial);
                            }
                        }
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
                    if (riparian > 0.9 && solidSurface - 2 >= 0)
                    {
                        chunk.SetBlock(x, Math.Max(1, solidSurface - 2), z, rimMaterial);
                    }
                }
            }
        }

        private void ReinforceLakeWetlands(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation, double[,] riparianSaturation, double[,]? hydrologyCurvature = null)
        {
            double wetlandThreshold = Math.Clamp(_lakeWetlandSaturationThreshold, 0.35, 1.1);
            int outflowDepth = Math.Max(1, _lakeOutflowCarveDepth);

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    double curvature = hydrologyCurvature?[x, z] ?? 0.0;
                    double curvatureBoost = Math.Clamp(curvature * _hydrologyCurvatureWeight, 0.0, 1.0);
                    double wetness = hydrology + flow * 0.5 + curvatureBoost * 0.6 + riparianSaturation[x, z] * 0.6;
                    if (wetness < wetlandThreshold)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0 || surface > GlobalWaterLevel + 2 || surface < GlobalWaterLevel - 3)
                    {
                        continue;
                    }

                    int wetlandFloor = Math.Max(1, surface - outflowDepth - (int)Math.Round(curvatureBoost * _lakeBasinSmoothIterations * 0.25));
                    chunk.SetBlock(x, wetlandFloor, z, hydrology + curvatureBoost > 0.9 ? BlockType.Clay : BlockType.Sand);
                    chunk.SetBlock(x, wetlandFloor + 1, z, BlockType.Water);
                    if (wetlandFloor + 2 < 256)
                    {
                        chunk.SetBlock(x, wetlandFloor + 2, z, BlockType.Air);
                    }
                }
            }
        }

        private void ExtendLakeWetlands(ChunkData chunk, int[,] surfaceCache, double[,] riparianSaturation, double[,] flowAccumulation)
        {
            if (_lakeWetlandBufferRadius <= 0)
            {
                return;
            }

            int radius = Math.Clamp(_lakeWetlandBufferRadius, 1, 6);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int waterSurface = FindTopWaterLevel(chunk, x, z);
                    if (waterSurface < 1)
                    {
                        continue;
                    }

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.25)
                            {
                                continue;
                            }

                            double riparian = riparianSaturation[nx, nz];
                            if (riparian < _lakeWetlandSaturationThreshold * 0.8)
                            {
                                continue;
                            }

                            int surface = surfaceCache[nx, nz];
                            if (surface <= 0 || surface < waterSurface - 3 || surface > waterSurface + 4)
                            {
                                continue;
                            }

                            double flow = flowAccumulation[nx, nz];
                            double wetness = Math.Clamp(riparian * 0.6 + flow * 0.25, 0.0, 1.5);
                            var rimMaterial = wetness > 0.85 ? BlockType.Clay : BlockType.Sand;
                            chunk.SetBlock(nx, surface, nz, rimMaterial);
                            int fillDepth = Math.Max(1, (int)Math.Round(Math.Max(1.0, wetness) + _lakeBasinSmoothIterations * 0.25));
                            for (int y = surface - fillDepth; y < surface; y++)
                            {
                                if (y >= 1 && chunk.GetBlock(nx, y, nz) == BlockType.Air)
                                {
                                    chunk.SetBlock(nx, y, nz, rimMaterial);
                                }
                            }
                            if (surface + 1 < 256)
                            {
                                chunk.SetBlock(nx, surface + 1, nz, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        private void AddCaveSupportPillars(ChunkData chunk, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            double supportChance = Math.Clamp(_supportPillarChance * _caveSupportDensity, 0.0, 1.0);
            if (supportChance <= 0.01)
            {
                return;
            }

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 3)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surface, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    Vector2 gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    double gradientStrength = gradient.Length();
                    double flow = flowAccumulation[x, z];
                    double pillarBias = hydrology * _caveSupportHydrationBias + flow * _caveSupportFlowBias;
                    double roll = SampleDeterministicNoise(x * 131 + z * 37, top * 11 + bottom * 7, SaltCaveHydro);
                    double stabilityWeight = Math.Clamp(1.0 - gradientStrength * _hydrologyGradientWeight * 0.35, 0.65, 1.0);
                    double flowAlignment = gradientStrength * _riverFlowAlignmentWeight * 0.35;
                    if (roll > (supportChance + pillarBias * 0.5) * stabilityWeight + flowAlignment * 0.2)
                    {
                        continue;
                    }

                    int pillarTop = Math.Max(bottom + 1, top - 1 - (int)Math.Round(gradientStrength * 2.0));
                    var fill = hydrology > 0.65 ? BlockType.Cobblestone : BlockType.Stone;
                    for (int y = bottom + 1; y <= pillarTop; y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, fill);
                            surfaceCache[x, z] = Math.Max(surfaceCache[x, z], y);
                        }
                    }
                }
            }
        }

        private void SmoothRiverMouths(ChunkData chunk, int[,] surfaceCache, double[,] riverIntensity, double[,] hydrologyMask, double[,] flowAccumulation, double[,]? riparianSaturation = null)
        {
            if (_riverMouthSmoothRadius <= 0)
            {
                return;
            }

            int radius = Math.Max(1, _riverMouthSmoothRadius);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity < RiverCenterThreshold * 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0 || surface > GlobalWaterLevel + 6)
                    {
                        continue;
                    }

                    double riparian = riparianSaturation?[x, z] ?? 0.0;
                    double wetness = Math.Clamp(hydrologyMask[x, z] + flowAccumulation[x, z] * 0.35 + riparian * 0.35, 0.0, 1.25);
                    double blend = Math.Clamp(_riverDeltaWetlandStrength + wetness * 0.25, 0.0, 1.35);
                    int targetWater = Math.Min(surface, GlobalWaterLevel);

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.2)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0 || neighborSurface > GlobalWaterLevel + 6)
                            {
                                continue;
                            }

                            int carveDepth = Math.Max(1, (int)Math.Round((radius - distance + 1) * blend));
                            int waterFloor = Math.Max(1, targetWater - carveDepth);
                            for (int y = targetWater; y >= waterFloor; y--)
                            {
                                chunk.SetBlock(nx, y, nz, BlockType.Water);
                            }

                            var rimMaterial = wetness + blend > 0.9 ? BlockType.Clay : BlockType.Sand;
                            chunk.SetBlock(nx, waterFloor - 1 >= 0 ? Math.Max(0, waterFloor - 1) : 0, nz, rimMaterial);
                        }
                    }
                }
            }
        }

        private void CreateLakeOutflowChannels(ChunkData chunk, int[,] surfaceCache, double[,] flowAccumulation, double[,] hydrologyMask)
        {
            int carveDepth = Math.Max(1, _lakeOutflowCarveDepth);

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    int waterSurface = FindTopWaterLevel(chunk, x, z);
                    if (waterSurface < 1)
                    {
                        continue;
                    }

                    int solidSurface = surfaceCache[x, z];
                    if (solidSurface < waterSurface || solidSurface > waterSurface + 4)
                    {
                        continue;
                    }

                    double flow = flowAccumulation[x, z];
                    double hydrology = hydrologyMask[x, z];
                    double pressure = Math.Clamp(flow * 0.8 + hydrology * 0.4, 0.0, 1.35);
                    if (pressure < 0.55)
                    {
                        continue;
                    }

                    int targetFloor = Math.Max(1, waterSurface - carveDepth);
                    for (int y = waterSurface; y >= targetFloor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    var rimMaterial = pressure > 0.85 ? BlockType.Clay : BlockType.Sand;
                    chunk.SetBlock(x, targetFloor - 1, z, rimMaterial);
                }
            }
        }

        private void BlendLakeWithRivers(ChunkData chunk, int[,] surfaceCache, HydrologyFieldCache hydrologyField, RiverFieldCache riverField)
        {
            if (!_enableLakes || !_enableRivers)
            {
                return;
            }

            var riverIntensity = riverField.Intensity;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    int waterSurface = FindTopWaterLevel(chunk, x, z);
                    if (waterSurface < 1)
                    {
                        continue;
                    }

                    int solidSurface = surfaceCache[x, z];
                    if (solidSurface < waterSurface || solidSurface > waterSurface + 6)
                    {
                        continue;
                    }

                    double riverPressure = riverIntensity[x, z];
                    if (riverPressure < RiverBankThreshold * 0.5)
                    {
                        continue;
                    }

                    double hydrology = hydrologyField.HydrologyMask[x, z];
                    double flow = hydrologyField.FlowAccumulation[x, z];
                    double suppression = Math.Clamp(riverPressure * _lakeRiverProximitySuppression, 0.0, 1.0);
                    double inflow = Math.Clamp(hydrology * 0.6 + flow * 0.4, 0.0, 1.25);
                    int carveDepth = Math.Max(1, (int)Math.Round(_lakeOutflowCarveDepth * (0.8 + suppression)));
                    int targetFloor = Math.Max(1, waterSurface - carveDepth);

                    for (int y = waterSurface; y >= targetFloor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    var rimMaterial = inflow + suppression > 0.95 ? BlockType.Clay : BlockType.Sand;
                    chunk.SetBlock(x, targetFloor - 1, z, rimMaterial);
                    if (suppression > 0.55 && waterSurface + 1 < 256)
                    {
                        chunk.SetBlock(x, waterSurface + 1, z, BlockType.Air);
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
