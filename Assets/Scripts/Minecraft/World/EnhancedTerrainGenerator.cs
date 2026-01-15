
using System;
using System.IO;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Hydrology-aware terrain generator for client previews. Couples height, rivers, lakes, and caves.
    /// </summary>
    public class EnhancedTerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        private EnhancedTerrainTuning _tuning;
        private FastNoise _terrainNoise;
        private FastNoise _detailNoise;
        private FastNoise _biomeNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _caveNoise;

        private int _chunkSize;
        private int _worldHeight;
        private int _seaLevel;
        private bool _enableCaves;
        private bool _enableRivers;
        private bool _enableLakes;

        [Serializable]
        private class EnhancedTerrainTuning
        {
            public float HydrologyVarianceBlend = 0.2f;
            public float HydrologyVarianceClamp = 0.65f;
            public float RiverConfluenceBoost = 0.35f;
            public float RiverEdgeFeather = 0.45f;
            public float RiverBankErosionWeight = 0.18f;
            public int LakeOutflowCarveDepth = 3;
            public int LakeWetlandBufferRadius = 2;
            public float LakeWetlandSaturationThreshold = 0.55f;
            public float LakeShorelineBlend = 0.66f;
            public int LakeShelfDepth = 2;
            public float CaveConnectivityThreshold = 0.42f;
            public float CaveMoisturePenalty = 0.35f;
            public float CaveFlowPenalty = 0.25f;
            public float CaveMoistureRetentionWeight = 0.35f;
            public float DetailNoiseStrength = 0.12f;
            public int DetailNoiseOctaves = 2;

            public static EnhancedTerrainTuning FromConfig(WorldConfig config)
            {
                return new EnhancedTerrainTuning
                {
                    HydrologyVarianceBlend = Mathf.Clamp01(config.Water.HydrologyVarianceBlend),
                    HydrologyVarianceClamp = Mathf.Clamp(config.Water.HydrologyVarianceClamp, 0.0f, 2.0f),
                    RiverConfluenceBoost = Mathf.Clamp(config.Water.RiverConfluenceBoost, 0.0f, 2.0f),
                    RiverEdgeFeather = Mathf.Clamp(config.Water.RiverEdgeFeather, 0.0f, 1.0f),
                    RiverBankErosionWeight = Mathf.Clamp01(config.Water.RiverBankErosionWeight),
                    LakeOutflowCarveDepth = Math.Max(1, config.Lakes.OutflowCarveDepth),
                    LakeWetlandBufferRadius = Math.Max(0, config.Lakes.WetlandBufferRadius),
                    LakeWetlandSaturationThreshold = Mathf.Clamp(config.Lakes.WetlandSaturationThreshold, 0.0f, 1.0f),
                    LakeShorelineBlend = Mathf.Clamp(config.Lakes.ShorelineBlend, 0.0f, 1.0f),
                    LakeShelfDepth = Math.Max(1, config.Lakes.ShelfDepth),
                    CaveConnectivityThreshold = Mathf.Clamp(config.Caves.CaveThreshold, 0.1f, 0.9f),
                    CaveMoistureRetentionWeight = Mathf.Clamp01(config.Caves.MoistureRetentionWeight)
                };
            }

            public void ApplyOverrides(EnhancedTerrainTuning overrides)
            {
                if (overrides == null)
                {
                    return;
                }

                HydrologyVarianceBlend = Mathf.Clamp01(overrides.HydrologyVarianceBlend);
                HydrologyVarianceClamp = Mathf.Clamp(overrides.HydrologyVarianceClamp, 0.0f, 2.0f);
                RiverConfluenceBoost = Mathf.Clamp(overrides.RiverConfluenceBoost, 0.0f, 2.0f);
                RiverEdgeFeather = Mathf.Clamp(overrides.RiverEdgeFeather, 0.0f, 1.0f);
                RiverBankErosionWeight = overrides.RiverBankErosionWeight > 0f ? Mathf.Clamp01(overrides.RiverBankErosionWeight) : RiverBankErosionWeight;
                LakeOutflowCarveDepth = Math.Max(1, overrides.LakeOutflowCarveDepth);
                LakeWetlandBufferRadius = Math.Max(0, overrides.LakeWetlandBufferRadius);
                LakeWetlandSaturationThreshold = Mathf.Clamp(overrides.LakeWetlandSaturationThreshold, 0.0f, 1.0f);
                LakeShorelineBlend = Mathf.Clamp(overrides.LakeShorelineBlend, 0.0f, 1.0f);
                LakeShelfDepth = overrides.LakeShelfDepth > 0 ? Math.Max(1, overrides.LakeShelfDepth) : LakeShelfDepth;
                CaveConnectivityThreshold = Mathf.Clamp(overrides.CaveConnectivityThreshold, 0.1f, 0.9f);
                CaveMoisturePenalty = Mathf.Clamp01(overrides.CaveMoisturePenalty);
                CaveFlowPenalty = Mathf.Clamp01(overrides.CaveFlowPenalty);
                CaveMoistureRetentionWeight = overrides.CaveMoistureRetentionWeight > 0f
                    ? Mathf.Clamp01(overrides.CaveMoistureRetentionWeight)
                    : CaveMoistureRetentionWeight;
                DetailNoiseStrength = Mathf.Clamp01(overrides.DetailNoiseStrength);
                DetailNoiseOctaves = Math.Max(1, overrides.DetailNoiseOctaves);
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            _chunkSize = _worldConfig.ChunkSize;
            _worldHeight = _worldConfig.WorldHeight;
            _seaLevel = _worldConfig.Terrain.SeaLevel;
            _enableCaves = _worldConfig.Caves.EnableCaves;
            _enableRivers = _worldConfig.Water.EnableRivers;
            _enableLakes = _worldConfig.Water.EnableLakes;

            _tuning = LoadTuning();
            InitializeNoiseGenerators();
        }

        public void SetTerrainParameters(TerrainParameters parameters)
        {
            if (parameters == null)
            {
                return;
            }

            _chunkSize = Math.Max(1, parameters.ChunkSize);
            _worldHeight = Math.Max(1, parameters.WorldHeight);
            _seaLevel = parameters.SeaLevel;
            _enableCaves = parameters.EnableCaves;
            _enableRivers = parameters.EnableRivers;
            _enableLakes = parameters.EnableLakes;
            InitializeNoiseGenerators();
        }

        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            if (_worldConfig == null || _blockDataManager == null)
            {
                Initialize();
            }

            var blocks = new int[_chunkSize, _worldHeight, _chunkSize];
            int[,] heightMap = BuildHeightMap(chunkX, chunkZ);
            float[,] hydrology = BuildHydrologyMask(heightMap);
            float[,] flow = BuildFlowMask(heightMap, hydrology);
            BlendHydrologyWithFlow(heightMap, hydrology, flow);
            NormalizeHydrologyFlowEdges(hydrology, flow);
            ApplyFlowShadow(hydrology, flow);

            float[,] riverMask = _enableRivers ? BuildRiverMask(heightMap, hydrology, flow, chunkX, chunkZ) : new float[_chunkSize, _chunkSize];
            float[,] lakeMask = _enableLakes ? BuildLakeMask(heightMap, hydrology, flow, riverMask, chunkX, chunkZ) : new float[_chunkSize, _chunkSize];
            bool[,,]? caveMask = _enableCaves ? BuildCaveMask(heightMap, hydrology, flow, riverMask, chunkX, chunkZ) : null;

            ApplyHydrologyEnvelope(riverMask, lakeMask, hydrology, flow);
            RefineTerrainForWater(heightMap, riverMask, lakeMask);
            FillTerrain(blocks, heightMap);
            ApplyRivers(blocks, heightMap, riverMask, hydrology, flow);
            ApplyLakes(blocks, heightMap, lakeMask, hydrology, flow);
            ApplyCaves(blocks, caveMask, hydrology);
            AddBedrock(blocks);
            return blocks;
        }

        private EnhancedTerrainTuning LoadTuning()
        {
            var tuning = EnhancedTerrainTuning.FromConfig(_worldConfig);
            string path = Path.Combine(Application.streamingAssetsPath, "enhanced-terrain-config.json");

            if (!File.Exists(path))
            {
                return tuning;
            }

            try
            {
                string json = File.ReadAllText(path);
                var overrides = JsonUtility.FromJson<EnhancedTerrainTuning>(json);
                tuning.ApplyOverrides(overrides);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EnhancedTerrainGenerator] Failed to read enhanced terrain tuning: {ex.Message}");
            }

            return tuning;
        }

        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed != 0 ? _worldConfig.Seed : Environment.TickCount;

            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(_worldConfig.Terrain.Octaves);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);

            _detailNoise = new FastNoise(seed + 1);
            _detailNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _detailNoise.SetFrequency(_worldConfig.Terrain.NoiseScale * 2.0f);
            _detailNoise.SetFractalOctaves(_tuning.DetailNoiseOctaves);
            _detailNoise.SetFractalGain(0.35f);
            _detailNoise.SetFractalLacunarity(2.0f);

            _biomeNoise = new FastNoise(seed + 2);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);

            _riverNoise = new FastNoise(seed + 3);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _riverNoise.SetFrequency(Mathf.Max(0.0001f, _worldConfig.Water.RiverNoiseScale));

            _lakeNoise = new FastNoise(seed + 4);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _lakeNoise.SetFrequency(0.0025f);

            _caveNoise = new FastNoise(seed + 5);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency);
        }

        private int[,] BuildHeightMap(int chunkX, int chunkZ)
        {
            var heightMap = new int[_chunkSize, _chunkSize];

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float worldX = (chunkX * _chunkSize + x) * _worldConfig.Terrain.NoiseScale;
                    float worldZ = (chunkZ * _chunkSize + z) * _worldConfig.Terrain.NoiseScale;
                    float baseNoise = _terrainNoise.GetNoise(worldX, worldZ);
                    float detail = _detailNoise.GetNoise(worldX * 1.5f, worldZ * 1.5f) * _tuning.DetailNoiseStrength;
                    float biome = (_biomeNoise.GetNoise(worldX, worldZ) + 1f) * 0.5f;

                    float normalized = (baseNoise + 1f) * 0.5f;
                    normalized = Mathf.Clamp01(normalized + detail);

                    float heightScale = Mathf.Lerp(0.8f, 1.3f, biome);
                    int height = Mathf.RoundToInt(_worldConfig.Terrain.PlainBaseHeight + normalized * _worldConfig.Terrain.MountainMaxHeight * 0.5f * heightScale);
                    heightMap[x, z] = Mathf.Clamp(height, 4, _worldHeight - 4);
                }
            }

            return heightMap;
        }
        private float[,] BuildHydrologyMask(int[,] heightMap)
        {
            var hydrology = new float[_chunkSize, _chunkSize];

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - _seaLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / Math.Max(1.0, _worldConfig.Water.HydrologyWaterTableClampRange), 0.0, 1.0);
                    double shoreBoost = Math.Exp(-distance / Math.Max(0.1, _worldConfig.Water.HydrologyShorePush));
                    double slope = ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slope * (_worldConfig.Water.HydrologyWaterTableSlopeWeight + _worldConfig.Water.HydrologySlopePenalty * 0.1) / 6.0, 0.0, 0.7);
                    double gradientDamp = 1.0 - Math.Clamp(slope * _worldConfig.Water.HydrologyGradientWeight / Math.Max(1.0, _worldConfig.Water.HydrologyGradientClamp * 8.0), 0.0, 0.35);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * _worldConfig.Water.HydrologyCurvatureWeight * 0.08;
                    double warp = Mathf.PerlinNoise((x + 17) * _worldConfig.Water.HydrologyWarpFrequency, (z + 31) * _worldConfig.Water.HydrologyWarpFrequency) * _worldConfig.Water.HydrologyWarpAmplitude * 0.02;

                    double baseline = Math.Clamp(waterBias * _worldConfig.Water.HydrologyWaterTableClampWeight * stability * gradientDamp, 0.0, 1.0);
                    baseline = Math.Clamp(baseline + warp + shoreBoost * 0.05 - curvature, 0.0, 1.2);
                    hydrology[x, z] = Mathf.Clamp01((float)baseline);
                }
            }

            if (_tuning.HydrologyVarianceBlend > 0f)
            {
                BlendInterior(hydrology, _tuning.HydrologyVarianceBlend);
            }

            SmoothField(hydrology, _worldConfig.Water.HydrologySmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            BlendInterior(hydrology, 0.1f);
            NormalizeEdgeBands(
                hydrology,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                Mathf.Max(0.05f, _worldConfig.Water.HydrologySeamRelaxBlend * 0.5f),
                _worldConfig.Water.HydrologyEdgeVarianceClamp);
            NormalizeEdges(
                hydrology,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                _worldConfig.Water.HydrologyEdgeNormalizationIterations,
                _worldConfig.Water.HydrologyEdgeNormalizationBlend);
            return hydrology;
        }

        private float[,] BuildFlowMask(int[,] heightMap, float[,] hydrology)
        {
            var flow = new float[_chunkSize, _chunkSize];

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    double accumulation = 0.0;
                    double current = heightMap[x, z];

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
                            if (nx < 0 || nz < 0 || nx >= _chunkSize || nz >= _chunkSize)
                            {
                                continue;
                            }

                            double neighbor = heightMap[nx, nz];
                            if (neighbor < current)
                            {
                                accumulation += (current - neighbor) * 0.25;
                            }
                        }
                    }

                    double hydrologyBoost = hydrology[x, z] * _worldConfig.Water.HydrologyFlowGain;
                    double meanderNoise = Math.Abs(Mathf.PerlinNoise(
                        (x + 17 + _worldConfig.Seed) * 0.07f,
                        (z - 11 + _worldConfig.Seed) * 0.07f));
                    double varianceScale = Mathf.Clamp(_tuning.HydrologyVarianceBlend, 0f, 1f) * 0.15f;
                    double scaled = (accumulation + hydrologyBoost) * (1.0 + meanderNoise * varianceScale);
                    flow[x, z] = Mathf.Clamp((float)scaled, 0f, 12f);
                }
            }

            SmoothField(flow, _worldConfig.Water.HydrologySmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            NormalizeEdgeBands(
                flow,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                Mathf.Max(0.05f, _worldConfig.Water.HydrologySeamRelaxBlend * 0.5f),
                _worldConfig.Water.HydrologyEdgeVarianceClamp);
            ApplyFlowMemory(hydrology, flow, _worldConfig.Water.HydrologyFlowMemoryWeight);
            NormalizeEdges(
                flow,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                _worldConfig.Water.HydrologyEdgeNormalizationIterations,
                _worldConfig.Water.HydrologyEdgeNormalizationBlend);
            return flow;
        }

        private void BlendHydrologyWithFlow(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            float flowBlend = Mathf.Clamp(_worldConfig.Water.HydrologyContinuityWeight * 0.35f, 0.05f, 0.45f);
            float edgeBlend = Mathf.Clamp((float)_worldConfig.Water.HydrologyEdgeFlowLockWeight * 0.5f, 0f, 0.45f);
            int edgeRadius = Mathf.Max(1, Mathf.Max(_worldConfig.Water.HydrologyEdgeBlendRadius, _worldConfig.Water.HydrologyWatershedStitchRadius));
            int watershedRadius = Mathf.Max(1, _worldConfig.Water.HydrologyWatershedStitchRadius);
            float confluenceBoost = Mathf.Clamp(_tuning.RiverConfluenceBoost, 0f, 2f);
            float flowShadowWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowWeight);
            float flowShadowSlopeWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowSlopeWeight);
            float directionalBias = Mathf.Clamp(_worldConfig.Water.HydrologyDirectionalBlend * 0.5f, 0f, 0.5f);
            float watershedBlend = Mathf.Clamp01(_worldConfig.Water.HydrologyWatershedStitchWeight);

            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float normalizedFlow = Mathf.Clamp(flowValue / Mathf.Max(1f, _worldConfig.Water.RiverDepth), 0f, 1f);
                    float neighborFlow = SampleInterior(flow, x, z) / Mathf.Max(1f, _worldConfig.Water.RiverDepth);
                    float neighborHydro = SampleInterior(hydrology, x, z);
                    float hydrologyGradient = Mathf.Abs(neighborHydro - hydro);

                    int edgeDistance = Mathf.Min(Mathf.Min(x, _chunkSize - 1 - x), Mathf.Min(z, _chunkSize - 1 - z));
                    float edgeFalloff = Mathf.Clamp01(1f - edgeDistance / (float)(edgeRadius + 1));
                    float edgeFactor = edgeBlend * edgeFalloff + watershedBlend * edgeFalloff * 0.5f;
                    float blend = Mathf.Clamp(flowBlend + edgeFactor, 0f, 0.9f);

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, _chunkSize - 1);
                    int downZ = Mathf.Clamp(z + downhill.z, 0, _chunkSize - 1);
                    float directionalHydro = hydrology[downX, downZ];
                    float directionalFlow = Mathf.Clamp(flow[downX, downZ] / Mathf.Max(1f, _worldConfig.Water.RiverDepth), 0f, 1f);
                    float directionalWeight = Mathf.Clamp((Mathf.Abs(downhill.x) + Mathf.Abs(downhill.z)) * directionalBias + directionalFlow * 0.2f, 0f, 0.45f);

                    float confluence = confluenceBoost > 0f ? (neighborFlow * 0.5f + neighborHydro * 0.25f + hydrologyGradient * 0.15f) * confluenceBoost : 0f;

                    float flowShadow = Mathf.Clamp(
                        (normalizedFlow + neighborFlow) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5f +
                        directionalFlow * flowShadowWeight * 0.15f,
                        0f,
                        0.7f);

                    float blended = hydro * (1f - blend) + normalizedFlow * blend;
                    blended = blended * (1f - flowShadow * 0.35f) + neighborHydro * flowShadow * 0.35f;
                    blended = blended * (1f - directionalWeight) + directionalHydro * directionalWeight;
                    blended *= 1f + confluence;
                    buffer[x, z] = Mathf.Clamp(blended, 0f, 1.25f);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
            BlendWatershedEdges(heightMap, hydrology, flow, watershedRadius, watershedBlend, flowShadowWeight);
            ClampVariance(hydrology, Math.Max(_tuning.HydrologyVarianceClamp, _worldConfig.Water.HydrologyVarianceClamp));
            BlendInterior(hydrology, Mathf.Clamp01(_worldConfig.Water.HydrologySeamRelaxBlend * 0.15f));
            NormalizeEdgeBands(
                hydrology,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                Mathf.Max(0.05f, _worldConfig.Water.HydrologySeamRelaxBlend * 0.4f),
                _worldConfig.Water.HydrologyEdgeVarianceClamp);
        }

        private void NormalizeHydrologyFlowEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Mathf.Max(1, _worldConfig.Water.HydrologyEdgeBlendRadius);
            int iterations = Mathf.Max(1, _worldConfig.Water.HydrologyEdgeNormalizationIterations);
            float blendBase = Mathf.Clamp01(_worldConfig.Water.HydrologyEdgeNormalizationBlend);
            float memoryWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowMemoryWeight);
            float flowClamp = Mathf.Max(0.5f, _worldConfig.Water.HydrologyFlowDivergenceClamp * 12f);

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Mathf.Min(Mathf.Min(x, sizeX - 1 - x), Mathf.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            continue;
                        }

                        float edgeFalloff = 1f - Mathf.Clamp01(edgeDistance / (float)(edgeRadius + 1));
                        float blend = blendBase * edgeFalloff;
                        if (blend <= 0f)
                        {
                            continue;
                        }

                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        float neighbourHydro = SampleInterior(hydrology, x, z);
                        float neighbourFlow = SampleInterior(flow, x, z);
                        float seamAnchor = (neighbourHydro + hydro) * 0.5f + neighbourFlow * memoryWeight * 0.25f;

                        float targetHydro = (float)((neighbourHydro * (1f + memoryWeight * 0.35f) + hydro * 0.65f + flowValue * memoryWeight * 0.15f) / (1.8f + memoryWeight * 0.35f));
                        targetHydro = (targetHydro + seamAnchor * 0.25f) / 1.25f;
                        hydroBuffer[x, z] = Mathf.Clamp01(Mathf.Lerp(hydro, targetHydro, blend));

                        float targetFlow = (float)((neighbourFlow * (1f + memoryWeight) + flowValue + hydro * memoryWeight * 0.35f) / (2f + memoryWeight));
                        targetFlow = (targetFlow + seamAnchor * 0.2f) / 1.2f;
                        flowBuffer[x, z] = Mathf.Clamp(targetFlow, 0f, Mathf.Max(flowValue + 1.5f, flowClamp));
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }
        }

        private void ApplyFlowShadow(float[,] hydrology, float[,] flow)
        {
            float weight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowWeight);
            float slopeWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowSlopeWeight);
            if (weight <= 0f && slopeWeight <= 0f)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();
            float flowClamp = Mathf.Max(0.5f, _worldConfig.Water.HydrologyFlowDivergenceClamp * 12f);
            float persistence = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowPersistence);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float flowShadow = Mathf.Clamp(((flowValue + neighbourFlow) * 0.5f) * weight, 0f, 0.6f);
                    float slopeShadow = Mathf.Clamp(Mathf.Abs(hydro - neighbourHydro) * slopeWeight, 0f, 0.35f);

                    float dampenedHydro = hydro * (1f - flowShadow * 0.35f - slopeShadow * 0.35f) + neighbourHydro * (flowShadow * 0.2f);
                    float flowDamp = flowValue * (1f - flowShadow * 0.25f - slopeShadow * 0.2f);
                    flowDamp += neighbourFlow * (flowShadow * 0.25f + slopeShadow * 0.15f);
                    flowDamp *= 0.75f + persistence * 0.25f;

                    hydroBuffer[x, z] = Mathf.Clamp01(dampenedHydro);
                    flowBuffer[x, z] = Mathf.Clamp(flowDamp, 0f, Mathf.Max(flowClamp, Mathf.Max(flowValue, neighbourFlow) + 1f));
                }
            }

            Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
            Array.Copy(flowBuffer, flow, flowBuffer.Length);
        }

        private void ApplyHydrologyEnvelope(float[,] riverMask, float[,] lakeMask, float[,] hydrology, float[,] flow)
        {
            bool hasRiver = riverMask != null && riverMask.Length > 0 && _enableRivers;
            bool hasLake = lakeMask != null && lakeMask.Length > 0 && _enableLakes;
            if (!hasRiver && !hasLake)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Mathf.Max(1, _worldConfig.Water.HydrologyEdgeBlendRadius);
            float seamLock = Mathf.Clamp01(_worldConfig.Water.HydrologyEdgeFlowLockWeight);
            float continuityWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyContinuityWeight);
            float memoryWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowMemoryWeight);
            float varianceClamp = Mathf.Max(0.001f, _worldConfig.Water.HydrologyVarianceClamp);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float seam = 1f - Mathf.Clamp01(edgeDistance / (float)(edgeRadius + 1));
                    float wetAnchor = Mathf.Max(hydro, neighbourHydro);
                    float flowAnchor = Mathf.Max(flowValue, neighbourFlow);
                    float continuityBoost = continuityWeight * (wetAnchor * 0.35f + flowAnchor * 0.65f);
                    float seamBoost = seam * seamLock * 0.5f;

                    if (hasRiver)
                    {
                        float current = riverMask[x, z];
                        float neighbour = SampleInterior(riverMask, x, z);
                        float target = Mathf.Max(current, neighbour * 0.6f + wetAnchor * 0.25f + flowAnchor * 0.2f);
                        target = target * (1f + seamBoost) + continuityBoost * 0.5f + memoryWeight * neighbourFlow * 0.05f;
                        riverMask[x, z] = Mathf.Clamp(target, 0f, varianceClamp);
                    }

                    if (hasLake)
                    {
                        float currentLake = lakeMask[x, z];
                        float neighbourLake = SampleInterior(lakeMask, x, z);
                        float basin = Mathf.Max(currentLake, neighbourLake);
                        float targetLake = basin + wetAnchor * 0.35f + continuityBoost * 0.35f + flowAnchor * 0.15f;
                        targetLake *= 1f + seamBoost * 0.5f;
                        lakeMask[x, z] = Mathf.Clamp(targetLake, 0f, varianceClamp + 0.35f);
                    }
                }
            }

            if (hasRiver)
            {
                SmoothField(riverMask, Mathf.Max(1, _worldConfig.Water.HydrologyEdgeStabilityIterations), _worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.5f);
            }

            if (hasLake)
            {
                SmoothField(lakeMask, Mathf.Max(1, _worldConfig.Water.HydrologyEdgeStabilityIterations), _worldConfig.Water.HydrologySeamRelaxBlend * 0.5f);
            }
        }

        private float[,] BuildRiverMask(int[,] heightMap, float[,] hydrology, float[,] flow, int chunkX, int chunkZ)
        {
            var mask = new float[_chunkSize, _chunkSize];
            double noiseScale = Math.Max(0.0001, _worldConfig.Water.RiverNoiseScale);
            double confluenceBoost = Math.Clamp(_tuning.RiverConfluenceBoost, 0.0, 2.0);
            float flowShadowWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowWeight);
            float flowShadowSlopeWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowSlopeWeight);
            float watershedBlend = Mathf.Clamp01(_worldConfig.Water.HydrologyWatershedStitchWeight);
            int watershedRadius = Mathf.Max(1, _worldConfig.Water.HydrologyWatershedStitchRadius);
            float edgeNormalizationStrength = Mathf.Clamp01(_worldConfig.Water.HydrologyEdgeNormalizationBlend);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    int worldX = chunkX * _chunkSize + x;
                    int worldZ = chunkZ * _chunkSize + z;
                    double baseNoise = Math.Abs(_riverNoise.GetNoise(worldX * (float)noiseScale, worldZ * (float)noiseScale));

                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double flowMemory = SampleInterior(flow, x, z) / 6.0;
                    double gradient = ComputeSlope(heightMap, x, z);
                    double relief = Math.Max(0, heightMap[x, z] - _seaLevel) / Math.Max(1, _seaLevel);
                    double meander = Math.Abs(_riverNoise.GetNoise(worldX * (float)noiseScale * 0.65f + 19f, worldZ * (float)noiseScale * 0.65f - 11f));
                    double meanderFactor = 1.0 + meander * (Mathf.Clamp(_worldConfig.Water.HydrologyWarpAmplitude * 0.02f, 0.05f, 0.2f) + Mathf.Max(0f, _worldConfig.Water.RiverMeanderJitter));
                    double seamHydro = SampleInterior(hydrology, x, z);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double hydrologyVariance = SampleVariance(hydrology, x, z);
                    double flowVariance = SampleVariance(flow, x, z);
                    float flowShadow = Mathf.Clamp(
                        (float)(flowSample * flowShadowWeight + hydrologyGradient * flowShadowSlopeWeight * 0.5),
                        0f,
                        0.75f);
                    double continuityBias = 1.0 + Math.Clamp((seamHydro + flowMemory) * _worldConfig.Water.HydrologyEdgeFluxBlend * 0.2, -0.2, 0.35);
                    continuityBias *= 1.0 - Math.Clamp(hydrologyVariance * 0.15 + flowVariance * 0.1, 0.0, 0.25);
                    double seamAnchor = (hydrologySample + seamHydro + flowSample + flowMemory) * 0.25;

                    double pressure = _worldConfig.Water.RiverBankThreshold - baseNoise;
                    pressure = Math.Max(0.0, pressure);
                    pressure *= 1.0 + hydrologySample * _worldConfig.Water.HydrologyContinuityWeight;
                    pressure *= 1.0 + flowSample * _worldConfig.Water.RiverFlowAlignmentWeight;
                    pressure *= 1.0 - Math.Clamp(gradient * _worldConfig.Water.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * _worldConfig.Water.RiverReliefPenaltyWeight, 0.0, 0.35);
                    pressure *= meanderFactor;
                    pressure *= 1.0 + seamAnchor * edgeNormalizationStrength * 0.15;
                    if (confluenceBoost > 0.0)
                    {
                        double neighborFlow = SampleInterior(flow, x, z) / 6.0;
                        double neighborHydro = SampleInterior(hydrology, x, z);
                        double tributary = Math.Clamp((flowSample + neighborFlow) * 0.5, 0.0, 1.0);
                        pressure *= 1.0 + (tributary + neighborHydro * 0.25) * confluenceBoost * 0.35;
                    }

                    double headwater = 1.0 - Math.Clamp(flowSample * _worldConfig.Water.RiverHeadwaterStabilityWeight, 0.0, 0.65);
                    pressure *= 1.0 + headwater * 0.1;
                    double deltaBlend = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - _seaLevel) / Math.Max(1.0, _worldConfig.Water.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    pressure *= 1.0 + deltaBlend * _worldConfig.Water.RiverDeltaWetlandStrength * 0.5;

                    int edgeDistance = Math.Min(Math.Min(x, _chunkSize - 1 - x), Math.Min(z, _chunkSize - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeNormalization = edgeNormalizationStrength * edgeFalloff;
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double neighborFlow = SampleInterior(flow, x, z) / 6.0;
                        double neighborHydro = SampleInterior(hydrology, x, z);
                        double seamAnchor = hydrologySample * 0.35 + neighborHydro * 0.35 + neighborFlow * 0.3;
                        pressure = pressure * (1.0 - edgeRepair * 0.35) + seamAnchor * edgeRepair * 0.5;
                        pressure = Math.Max(pressure, seamAnchor * edgeRepair * 0.25);
                    }

                    pressure = pressure * (1.0 - flowShadow * 0.25) + hydrologySample * flowShadow * 0.15;
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * _worldConfig.Water.HydrologyEdgeStabilityWeight * 0.25, 0.0, 0.35);
                    pressure *= seamGuard * continuityBias;
                    pressure *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + flowVariance * 0.15, 0.0, 0.35);
                    pressure = pressure * (1.0 - edgeNormalization * 0.25) + seamAnchor * edgeNormalization * 0.35;

                    mask[x, z] = Mathf.Clamp((float)pressure, 0f, 1.35f);
                }
            }

            NormalizeEdgeBands(
                mask,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                Mathf.Max(0.05f, _worldConfig.Water.HydrologySeamRelaxBlend * 0.35f),
                _worldConfig.Water.HydrologyEdgeVarianceClamp);
            SmoothField(mask, _worldConfig.Water.RiverIntensitySmoothIterations, _worldConfig.Water.RiverIntensitySmoothBlend);
            ApplyEdgeFeather(mask, Math.Max(_tuning.RiverEdgeFeather, _worldConfig.Water.RiverEdgeFeather));
            NormalizeEdges(
                mask,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                _worldConfig.Water.HydrologyEdgeNormalizationIterations,
                _worldConfig.Water.HydrologyEdgeNormalizationBlend);
            return mask;
        }

        private float[,] BuildLakeMask(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask, int chunkX, int chunkZ)
        {
            var lakes = new float[_chunkSize, _chunkSize];
            float flowShadowWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowWeight);
            float flowShadowSlopeWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowShadowSlopeWeight);
            float flowSeepageWeight = Mathf.Clamp01(_worldConfig.Lakes.FlowSeepageWeight);
            float watershedBlend = Mathf.Clamp01(_worldConfig.Water.HydrologyWatershedStitchWeight);
            int watershedRadius = Mathf.Max(1, _worldConfig.Water.HydrologyWatershedStitchRadius);
            float flowMemoryWeight = Mathf.Clamp01(_worldConfig.Water.HydrologyFlowMemoryWeight);
            float varianceWeight = Mathf.Clamp01(_worldConfig.Lakes.VarianceWeight);
            float outflowStabilityWeight = Mathf.Clamp01(_worldConfig.Lakes.OutflowStabilityWeight);
            float edgeNormalizationStrength = Mathf.Clamp01(_worldConfig.Water.HydrologyEdgeNormalizationBlend);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float riverPressure = riverMask[x, z];
                    if (riverPressure > _worldConfig.Lakes.RiverProximitySuppression)
                    {
                        continue;
                    }

                    int worldX = chunkX * _chunkSize + x;
                    int worldZ = chunkZ * _chunkSize + z;
                    int edgeDistance = Math.Min(Math.Min(x, _chunkSize - 1 - x), Math.Min(z, _chunkSize - 1 - z));
                    float edgeNormalization = edgeNormalizationStrength * (1f - Mathf.Clamp01(edgeDistance / (float)(watershedRadius + 1)));

                    double basinNoise = _lakeNoise.GetNoise(worldX * 0.004f, worldZ * 0.004f);
                    double rimNoise = Math.Abs(_lakeNoise.GetNoise(worldX * 0.009f + 31f, worldZ * 0.009f + 17f));
                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamHydro = SampleInterior(hydrology, x, z);
                    double interiorFlow = SampleInterior(flow, x, z) / 6.0;
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double hydrologyVariance = SampleVariance(hydrology, x, z);
                    double seamAnchor = (hydrologySample + seamHydro + flowSample + interiorFlow) * 0.25;
                    float flowShadow = Mathf.Clamp(
                        (float)(flowSample * flowShadowWeight + hydrologyGradient * flowShadowSlopeWeight * 0.5),
                        0f,
                        0.7f);
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * _worldConfig.Water.HydrologyEdgeStabilityWeight * 0.35, 0.0, 0.5);
                    double seamContinuity = 1.0 + Math.Clamp((seamHydro + interiorFlow + hydrologySample) * _worldConfig.Water.HydrologyEdgeFluxBlend * 0.15, -0.35, 0.35);
                    double shorelineJitter = Math.Abs(_lakeNoise.GetNoise(worldX * 0.0025f + 7f, worldZ * 0.0025f - 13f)) * _worldConfig.Lakes.ShorelineBlend * 0.25;
                    double reliefPenalty = Math.Max(0, heightMap[x, z] - _seaLevel) / Math.Max(1, _seaLevel);
                    double inflowBlend = riverPressure * _worldConfig.Water.LakeInflowBlendWeight;
                    double rimWeight = 0.2 + Math.Clamp(_worldConfig.Water.HydrologyVarianceBlend, 0.0, 1.0) * 0.2;
                    double weight = basinNoise * 0.42 + rimNoise * rimWeight + hydrologySample * 0.35 + flowSample * 0.15 + _worldConfig.Lakes.SpawnWeightBias;
                    double seepage = (flowSample + hydrologyGradient + flowMemoryWeight * interiorFlow * 0.5) * flowSeepageWeight;
                    weight += inflowBlend * 0.35 * (1.0 - flowShadow * 0.5);
                    weight += hydrologyVariance * varianceWeight * (1.0 - flowShadow * 0.5);
                    weight += seepage * (1.0 - flowShadow * 0.5);
                    weight -= hydrologyGradient * _worldConfig.Water.HydrologyEdgeStabilityWeight * 0.25;
                    weight -= reliefPenalty * _worldConfig.Water.RiverReliefPenaltyWeight;
                    weight += seamAnchor * edgeNormalization * 0.25;
                    weight += shorelineJitter * (1.0 - flowShadow * 0.5);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, _chunkSize - 1);
                    int downZ = Mathf.Clamp(z + downhill.z, 0, _chunkSize - 1);
                    double downhillHydro = hydrology[downX, downZ];
                    double downhillFlow = flow[downX, downZ] / 6.0;
                    double outflowAnchor = (downhillHydro + downhillFlow) * outflowStabilityWeight * 0.25;
                    weight += outflowAnchor * (1.0 - flowShadow * 0.5);
                    weight *= 1.0 - Math.Clamp(hydrologyVariance * 0.2 + hydrologyGradient * 0.1, 0.0, 0.35);
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double seamAnchor = hydrologySample * 0.35 + flowSample * 0.25 + inflowBlend * 0.2 + hydrologyGradient * 0.1;
                        weight = weight * (1.0 - edgeRepair * 0.4) + seamAnchor * edgeRepair;
                    }
                    weight *= seamGuard * seamContinuity;
                    weight *= 1.0 - flowShadow * 0.35;
                    weight = weight * (1.0 - edgeNormalization * 0.2) + seamAnchor * edgeNormalization * 0.25;

                    double wetlandThreshold = _tuning.LakeWetlandSaturationThreshold - edgeNormalization * 0.05;
                    if (weight > wetlandThreshold && heightMap[x, z] > _seaLevel - _worldConfig.Lakes.MaxDepth)
                    {
                        lakes[x, z] = Mathf.Clamp01((float)weight);
                    }
                }
            }

            NormalizeEdgeBands(
                lakes,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                Mathf.Max(0.05f, _worldConfig.Water.HydrologySeamRelaxBlend * 0.35f),
                _worldConfig.Water.HydrologyEdgeVarianceClamp);
            SmoothField(lakes, _worldConfig.Lakes.LakeBasinSmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            ApplyRiparianBuffer(lakes, Math.Min(_tuning.LakeWetlandBufferRadius, _worldConfig.Lakes.MaxRadius), _tuning.LakeShorelineBlend);
            NormalizeEdges(
                lakes,
                _worldConfig.Water.HydrologyEdgeBlendRadius,
                _worldConfig.Water.HydrologyEdgeNormalizationIterations,
                _worldConfig.Water.HydrologyEdgeNormalizationBlend);
            ApplyOutflowChannels(lakes, heightMap, flow, _worldConfig.Water.LakeInflowBlendWeight, _tuning.LakeOutflowCarveDepth, outflowStabilityWeight);
            return lakes;
        }

        private bool[,,] BuildCaveMask(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask, int chunkX, int chunkZ)
        {
            var mask = new bool[_chunkSize, _worldHeight, _chunkSize];
            int minCave = Math.Max(1, _worldConfig.Caves.MinCaveHeight);
            int maxCave = Math.Min(_worldHeight - 2, _worldConfig.Caves.MaxCaveHeight);
            float ceilingMoistureWeight = Mathf.Clamp01(_worldConfig.Caves.CeilingMoistureWeight);
            float ceilingMoistureClamp = Mathf.Clamp01(_worldConfig.Caves.CeilingMoistureClamp);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float hydrologySample = hydrology[x, z];
                    float flowSample = flow[x, z];
                    float seamMemory = (flowSample + SampleInterior(flow, x, z)) * 0.5f;
                    float hydrologyGradient = Mathf.Abs(SampleInterior(hydrology, x, z) - hydrologySample);
                    float flowGradient = Mathf.Abs(SampleInterior(flow, x, z) - flowSample);
                    float flowShadow = Mathf.Clamp(flowSample * _worldConfig.Caves.FlowStabilityWeight + hydrologySample * _worldConfig.Caves.HydrologyStabilityWeight, 0f, 1.5f);
                    float hydrologyVariance = SampleVariance(hydrology, x, z, 2);
                    float ceilingMoisturePenalty = Mathf.Clamp(hydrologySample * ceilingMoistureWeight + flowSample * ceilingMoistureWeight * 0.5f + hydrologyGradient * ceilingMoistureWeight * 0.25f, 0f, ceilingMoistureClamp);
                    float variancePenalty = Mathf.Clamp(hydrologyVariance * 0.3f, 0f, 0.35f);
                    float stabilityPenalty = Mathf.Clamp(
                        flowShadow * 0.35f +
                        hydrologyGradient * 0.25f +
                        flowGradient * 0.25f +
                        riverMask[x, z] * _worldConfig.Caves.RiverSuppressionWeight * 0.5f +
                        ceilingMoisturePenalty * 0.5f +
                        variancePenalty * 0.5f,
                        0f,
                        0.95f);
                    stabilityPenalty *= 1f - Mathf.Clamp(hydrologyGradient * _worldConfig.Caves.EdgeSealStrength * 0.2f, 0f, 0.35f);
                    float moistureRetention = Mathf.Clamp01(1f - (hydrologySample * _tuning.CaveMoistureRetentionWeight + flowSample * _tuning.CaveMoistureRetentionWeight * 0.5f));
                    int riparianPlugDepth = Math.Max(0, _worldConfig.Caves.RiparianPlugDepth);

                    for (int y = minCave; y < maxCave; y++)
                    {
                        if (riparianPlugDepth > 0 && riverMask[x, z] > 0.55f && y >= Math.Max(1, _seaLevel - riparianPlugDepth))
                        {
                            continue;
                        }

                        float worldX = chunkX * _chunkSize + x;
                        float worldZ = chunkZ * _chunkSize + z;
                        float noise = _caveNoise.GetNoise(worldX * _worldConfig.Caves.HorizontalFrequency, worldZ * _worldConfig.Caves.HorizontalFrequency + y * _worldConfig.Caves.VerticalFrequency);
                        double threshold = _tuning.CaveConnectivityThreshold
                            + hydrologySample * _tuning.CaveMoisturePenalty
                            + flowSample * _tuning.CaveFlowPenalty
                            + riverMask[x, z] * _worldConfig.Caves.RiverSuppressionWeight
                            + seamMemory * _worldConfig.Caves.FlowStabilityWeight * 0.1f
                            + stabilityPenalty * 0.25
                            + ceilingMoisturePenalty * 0.2f
                            + (1f - moistureRetention) * 0.12f
                            + Mathf.Clamp(flowShadow * 0.15f, 0f, 0.25f)
                            + Mathf.Clamp(flowGradient * _worldConfig.Caves.EdgeSealStrength * 0.2f, 0f, 0.2f)
                            + variancePenalty * 0.25f;

                        if (noise > threshold)
                        {
                            mask[x, y, z] = true;
                        }
                    }
                }
            }

            SmoothField(mask, _worldConfig.Caves.StabilitySmoothIterations, _worldConfig.Caves.StabilitySmoothBlend, _worldConfig.Caves.SupportDensity);
            return mask;
        }

        private void RefineTerrainForWater(int[,] heightMap, float[,] riverMask, float[,] lakeMask)
        {
            _ = lakeMask;

            if (_enableRivers)
            {
                ApplyRiverBankErosion(heightMap, riverMask);
            }
        }

        private void ApplyRiverBankErosion(int[,] heightMap, float[,] riverMask)
        {
            float erosionStrength = Mathf.Clamp01(_tuning.RiverBankErosionWeight);
            if (erosionStrength <= 0f)
            {
                return;
            }

            int radius = Mathf.Max(1, Mathf.CeilToInt(_worldConfig.Water.RiverDepth * erosionStrength * 0.5f));

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float pressure = riverMask[x, z];
                    if (pressure <= _worldConfig.Water.RiverBankThreshold * 0.35f)
                    {
                        continue;
                    }

                    int surface = heightMap[x, z];
                    int erosion = Mathf.Max(1, Mathf.RoundToInt(pressure * _worldConfig.Water.RiverDepth * erosionStrength));
                    heightMap[x, z] = Math.Max(1, surface - erosion);

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= _chunkSize || nz >= _chunkSize)
                            {
                                continue;
                            }

                            float falloff = 1f - (Mathf.Abs(dx) + Mathf.Abs(dz)) / (float)(radius + 1);
                            if (falloff <= 0f)
                            {
                                continue;
                            }

                            int neighborSurface = heightMap[nx, nz];
                            int bankCut = Mathf.RoundToInt(erosion * falloff * 0.6f);
                            if (bankCut <= 0)
                            {
                                continue;
                            }

                            heightMap[nx, nz] = Math.Max(1, neighborSurface - bankCut);
                        }
                    }
                }
            }
        }

        private void FillTerrain(int[,,] blocks, int[,] heightMap)
        {
            int waterId = GetBlockId("water");
            int grassId = GetBlockId("grass");
            int dirtId = GetBlockId("dirt");
            int stoneId = GetBlockId("stone");
            int sandId = GetBlockId("sand");
            int bedrockId = GetBlockId("bedrock");

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    int terrainHeight = heightMap[x, z];
                    terrainHeight = Mathf.Clamp(terrainHeight, 4, _worldHeight - 2);

                    for (int y = 0; y <= terrainHeight; y++)
                    {
                        if (y == terrainHeight)
                        {
                            blocks[x, y, z] = terrainHeight < _seaLevel - 1 ? dirtId : grassId;
                        }
                        else if (y >= terrainHeight - 3)
                        {
                            blocks[x, y, z] = sandId;
                        }
                        else
                        {
                            blocks[x, y, z] = stoneId;
                        }
                    }

                    for (int y = terrainHeight + 1; y < _seaLevel; y++)
                    {
                        blocks[x, y, z] = waterId;
                    }

                    blocks[x, 0, z] = bedrockId;
                }
            }
        }

        private void ApplyRivers(int[,,] blocks, int[,] heightMap, float[,] riverMask, float[,] hydrology, float[,] flow)
        {
            if (!_enableRivers)
            {
                return;
            }

            int waterId = GetBlockId("water");
            int sandId = GetBlockId("sand");
            int grassId = GetBlockId("grass");
            int dirtId = GetBlockId("dirt");

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float pressure = riverMask[x, z];
                    if (pressure <= _worldConfig.Water.RiverCenterThreshold)
                    {
                        continue;
                    }

                    int surface = heightMap[x, z];
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);
                    float continuityBoost = Mathf.Clamp((hydro + neighbourHydro + flowValue + neighbourFlow) * 0.25f * _worldConfig.Water.HydrologyContinuityWeight, 0f, 0.85f);
                    float flowShadow = Mathf.Clamp(
                        (flowValue / Mathf.Max(1f, _worldConfig.Water.RiverDepth)) * _worldConfig.Water.HydrologyFlowShadowWeight +
                        hydrologyGradient * _worldConfig.Water.HydrologyFlowShadowSlopeWeight * 0.5f,
                        0f,
                        1f);
                    float seamStability = 1f - Mathf.Clamp(hydrologyGradient * _worldConfig.Water.HydrologyEdgeStabilityWeight * 0.35f, 0f, 0.85f);
                    float seamContinuity = 1f + continuityBoost * 0.25f;
                    int depth = Mathf.Clamp(
                        Mathf.RoundToInt(_worldConfig.Water.RiverDepth * (pressure + 0.35f + hydro * 0.35f + flowValue * 0.25f) * (1f - flowShadow * 0.25f) * seamContinuity),
                        2,
                        _worldConfig.Water.RiverDepth + 3);
                    int waterLevel = Math.Max(1, Math.Min(surface, _seaLevel));
                    int bottom = Math.Max(1, waterLevel - depth);

                    for (int y = bottom; y <= waterLevel; y++)
                    {
                        blocks[x, y, z] = waterId;
                    }

                    blocks[x, bottom, z] = sandId;

                    int bankDepth = Mathf.Max(1, Mathf.RoundToInt((_worldConfig.Water.RiverEdgeFeather * 4f + hydro * 2f) * seamStability * seamContinuity));
                    for (int b = 0; b < bankDepth; b++)
                    {
                        int bankY = Mathf.Max(1, surface - b);
                        int current = blocks[x, bankY, z];
                        if (current == grassId)
                        {
                            blocks[x, bankY, z] = b == 0 ? sandId : dirtId;
                        }
                    }

                    float wetland = Mathf.Max(pressure, hydro + continuityBoost * 0.25f);
                    if (wetland > 0.35f && blocks[x, surface, z] == grassId)
                    {
                        blocks[x, surface, z] = dirtId;
                        if (surface <= _seaLevel && flowShadow < 0.9f)
                        {
                            blocks[x, Math.Max(1, surface + 1), z] = waterId;
                        }
                    }
                }
            }
        }

        private void ApplyLakes(int[,,] blocks, int[,] heightMap, float[,] lakeMask, float[,] hydrology, float[,] flow)
        {
            if (!_enableLakes)
            {
                return;
            }

            int waterId = GetBlockId("water");
            int sandId = GetBlockId("sand");
            int grassId = GetBlockId("grass");
            int dirtId = GetBlockId("dirt");

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float lake = lakeMask[x, z];
                    if (lake <= 0.55f)
                    {
                        continue;
                    }

                    int surface = heightMap[x, z];
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);
                    float continuityBoost = Mathf.Clamp((hydro + neighbourHydro + flowValue + neighbourFlow) * 0.25f * _worldConfig.Water.HydrologyContinuityWeight, 0f, 0.85f);
                    float flowShadow = Mathf.Clamp(
                        (flowValue / Mathf.Max(1f, _worldConfig.Water.RiverDepth)) * _worldConfig.Water.HydrologyFlowShadowWeight +
                        hydrologyGradient * _worldConfig.Water.HydrologyFlowShadowSlopeWeight * 0.5f,
                        0f,
                        1f);
                    int rawDepth = Mathf.Clamp(Mathf.RoundToInt(_worldConfig.Lakes.MinDepth + lake * (_worldConfig.Lakes.MaxDepth - _worldConfig.Lakes.MinDepth)), _worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth + 2);
                    float shorelineFactor = Mathf.Clamp01((lake - 0.55f) / 0.45f);
                    int shelfDepth = Math.Max(1, _tuning.LakeShelfDepth);
                    int adjustedDepth = Mathf.Clamp(
                        Mathf.RoundToInt(Mathf.Lerp(shelfDepth, rawDepth, shorelineFactor) * (1f - flowShadow * 0.2f) * (1f + continuityBoost * 0.35f) + hydro * 0.5f),
                        _worldConfig.Lakes.MinDepth,
                        _worldConfig.Lakes.MaxDepth + 2);
                    int bottom = Math.Max(1, surface - adjustedDepth);
                    int shelfLayer = Math.Max(bottom, surface - shelfDepth);

                    for (int y = bottom; y <= surface; y++)
                    {
                        bool belowSea = y <= _seaLevel;
                        if (y == bottom || y == shelfLayer)
                        {
                            blocks[x, y, z] = sandId;
                            continue;
                        }

                        blocks[x, y, z] = belowSea ? waterId : 0;
                    }

                    blocks[x, bottom, z] = sandId;

                    float wetland = Mathf.Max(lake, hydro + continuityBoost * 0.35f);
                    if (wetland > 0.45f && blocks[x, surface, z] == grassId)
                    {
                        blocks[x, surface, z] = dirtId;
                        if (surface <= _seaLevel && flowShadow < 0.9f)
                        {
                            blocks[x, Math.Max(1, surface + 1), z] = waterId;
                        }
                    }

                    if (flowValue > 0.75f)
                    {
                        CreateLakeOutflow(blocks, heightMap, x, z, surface, flow, flowValue);
                    }
                }
            }
        }

        private void CreateLakeOutflow(int[,,] blocks, int[,] heightMap, int x, int z, int surface, float[,] flow, float flowValue)
        {
            float normalizedFlow = Mathf.Clamp(flowValue / Mathf.Max(1f, _worldConfig.Water.RiverDepth), 0f, 1f);
            if (normalizedFlow <= 0.55f)
            {
                return;
            }

            var directions = new (int dx, int dz)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            float bestFlow = 0f;
            int bestDx = 0;
            int bestDz = 0;

            foreach (var dir in directions)
            {
                int nx = x + dir.dx;
                int nz = z + dir.dz;
                if (nx < 0 || nz < 0 || nx >= _chunkSize || nz >= _chunkSize)
                {
                    continue;
                }

                float neighbourFlow = flow[nx, nz];
                if (neighbourFlow > bestFlow)
                {
                    bestFlow = neighbourFlow;
                    bestDx = dir.dx;
                    bestDz = dir.dz;
                }
            }

            if (bestFlow <= 0f)
            {
                return;
            }

            int waterId = GetBlockId("water");
            int sandId = GetBlockId("sand");
            int steps = Mathf.Min(2, Mathf.Max(1, Mathf.RoundToInt(normalizedFlow * 2f)));
            int depth = Mathf.Max(1, Mathf.RoundToInt(_worldConfig.Lakes.OutflowStabilityWeight * 3f));

            for (int step = 0; step < steps; step++)
            {
                int cx = Mathf.Clamp(x + bestDx * step, 0, _chunkSize - 1);
                int cz = Mathf.Clamp(z + bestDz * step, 0, _chunkSize - 1);
                int bottom = Math.Max(1, heightMap[cx, cz] - depth);

                for (int y = bottom; y <= surface; y++)
                {
                    bool belowSea = y <= _seaLevel;
                    blocks[cx, y, cz] = belowSea ? waterId : blocks[cx, y, cz];
                }

                blocks[cx, bottom, cz] = sandId;
            }
        }

        private void ApplyCaves(int[,,] blocks, bool[,,]? caveMask, float[,] hydrology)
        {
            if (!_enableCaves || caveMask == null)
            {
                return;
            }

            int airId = 0;
            int lavaId = GetBlockId("lava");
            int waterId = GetBlockId("water");

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    for (int y = 1; y < _worldHeight - 1; y++)
                    {
                        if (!caveMask[x, y, z])
                        {
                            continue;
                        }

                        bool flooded = y < _seaLevel - 4 && hydrology[x, z] > 0.6f;
                        blocks[x, y, z] = flooded ? waterId : airId;

                        if (y < 10 && UnityEngine.Random.value < 0.05f)
                        {
                            blocks[x, y, z] = lavaId;
                        }
                    }
                }
            }
        }

        private void AddBedrock(int[,,] blocks)
        {
            int bedrockId = GetBlockId("bedrock");
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    blocks[x, 0, z] = bedrockId;
                }
            }
        }
        private static void SmoothField(float[,] field, int iterations, float blend)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
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
                        buffer[x, z] = field[x, z] * (1f - blend) + average * blend;
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void SmoothField(bool[,,] field, int iterations, float blend, float supportDensity)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            supportDensity = Mathf.Clamp01(supportDensity);
            int survivalThreshold = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(10f, 18f, supportDensity)), 6, 24);
            int birthThreshold = Math.Max(3, survivalThreshold - 6);
            int sizeX = field.GetLength(0);
            int sizeY = field.GetLength(1);
            int sizeZ = field.GetLength(2);

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

                                        if (field[nx, ny, nz])
                                        {
                                            neighbours++;
                                        }
                                    }
                                }
                            }

                            bool carve = field[x, y, z];
                            if (neighbours >= survivalThreshold)
                            {
                                buffer[x, y, z] = true;
                            }
                            else if (neighbours <= birthThreshold)
                            {
                                buffer[x, y, z] = false;
                            }
                            else
                            {
                                int softThreshold = Math.Max(4, survivalThreshold - 4);
                                buffer[x, y, z] = blend > 0 ? neighbours >= softThreshold : carve;
                            }
                        }
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void BlendInterior(float[,] field, float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (blend <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = field[x, z] * (1f - blend) + interior * blend;
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void BlendWatershedEdges(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            int radius,
            float blendWeight,
            float flowAnchorWeight)
        {
            radius = Mathf.Max(0, radius);
            blendWeight = Mathf.Clamp01(blendWeight);
            flowAnchorWeight = Mathf.Clamp01(flowAnchorWeight);
            if (radius <= 0 || blendWeight <= 0f)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Mathf.Min(Mathf.Min(x, sizeX - 1 - x), Mathf.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    float falloff = 1f - edgeDistance / (float)(radius + 1);
                    float blend = blendWeight * falloff;

                    float interiorHydro = SampleInterior(hydroCopy, x, z);
                    float interiorFlow = SampleInterior(flowCopy, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.z, 0, sizeZ - 1);
                    float downhillHydro = hydroCopy[downX, downZ];
                    float downhillFlow = flowCopy[downX, downZ];

                    float flowAnchor = Mathf.Clamp((flowCopy[x, z] + interiorFlow + downhillFlow) / 3f, 0f, 8f);
                    flowAnchor = Mathf.Clamp(flowAnchor * flowAnchorWeight, 0f, 4f);

                    float targetHydro = interiorHydro * 0.55f + downhillHydro * 0.25f + flowAnchor * 0.1f + hydroCopy[x, z] * 0.1f;
                    float targetFlow = interiorFlow * 0.5f + downhillFlow * 0.25f + flowAnchor * 0.25f;

                    hydrology[x, z] = Mathf.Clamp01(hydroCopy[x, z] * (1f - blend) + targetHydro * blend);
                    flow[x, z] = Mathf.Clamp(flowCopy[x, z] * (1f - blend * 0.5f) + targetFlow * blend, 0f, Mathf.Max(2.5f, targetFlow * 1.5f + 0.5f));
                }
            }
        }

        private static void NormalizeEdgeBands(float[,] field, int radius, float interiorBlend, float clampRange)
        {
            radius = Mathf.Max(1, radius);
            interiorBlend = Mathf.Clamp01(interiorBlend);
            clampRange = Math.Max(0f, clampRange);
            if (interiorBlend <= 0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var copy = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Mathf.Min(Mathf.Min(x, sizeX - 1 - x), Mathf.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    float falloff = 1f - edgeDistance / (float)(radius + 1);
                    float blend = interiorBlend * falloff;
                    float interior = SampleInterior(copy, x, z);
                    float target = copy[x, z] * (1f - blend) + interior * blend;

                    if (clampRange > 0f)
                    {
                        float deltaClamp = clampRange * falloff;
                        float min = copy[x, z] - deltaClamp;
                        float max = copy[x, z] + deltaClamp;
                        target = Mathf.Clamp(target, min, max);
                    }

                    field[x, z] = Mathf.Clamp01(target);
                }
            }
        }

        private static void ApplyEdgeFeather(float[,] field, float feather)
        {
            feather = Mathf.Clamp01(feather);
            if (feather <= 0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float interior = SampleInterior(field, x, z);
                    field[x, z] = field[x, z] * (1f - feather) + interior * feather;
                }
            }
        }

        private void ApplyFlowMemory(float[,] hydrology, float[,] flow, float memoryWeight)
        {
            memoryWeight = Mathf.Clamp01(memoryWeight);
            if (memoryWeight <= 0f)
            {
                return;
            }

            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            var buffer = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float flowValue = flow[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float hydro = hydrology[x, z];
                    float blended = Mathf.Lerp(flowValue, (flowValue + neighbourFlow) * 0.5f + hydro * 0.25f, memoryWeight * 0.6f);
                    buffer[x, z] = Mathf.Clamp(blended, 0f, Mathf.Max(flowValue + 2f, 16f));
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
        }

        private static void NormalizeEdges(float[,] field, int radius, int iterations, float blend)
        {
            radius = Mathf.Max(0, radius);
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            if (iterations == 0 || blend <= 0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                Array.Copy(field, buffer, field.Length);

                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Mathf.Min(Mathf.Min(x, sizeX - 1 - x), Mathf.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > radius)
                        {
                            continue;
                        }

                        float interior = SampleInterior(buffer, x, z);
                        float current = buffer[x, z];
                        float edgeFalloff = 1f - edgeDistance / (float)(radius + 1);
                        float lerp = blend * edgeFalloff;
                        field[x, z] = Mathf.Clamp01(current * (1f - lerp) + interior * lerp);
                    }
                }
            }
        }

        private static float SampleVariance(float[,] field, int x, int z, int radius = 1)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            float sum = 0f;
            float sumSq = 0f;
            int count = 0;

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

                    float value = field[nx, nz];
                    sum += value;
                    sumSq += value * value;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0f;
            }

            float mean = sum / count;
            float variance = Mathf.Max(0f, sumSq / count - mean * mean);
            return Mathf.Clamp01(variance);
        }

        private void ApplyRiparianBuffer(float[,] field, int radius, float shorelineBlend)
        {
            radius = Mathf.Max(0, radius);
            shorelineBlend = Mathf.Clamp01(shorelineBlend);
            if (radius == 0 || shorelineBlend <= 0f)
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

                            float distanceFalloff = 1f - (Mathf.Abs(dx) + Mathf.Abs(dz)) / (float)(radius + 1);
                            float influence = Mathf.Clamp01(center * shorelineBlend * distanceFalloff);
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private void ApplyOutflowChannels(float[,] lakes, int[,] heightMap, float[,] flow, float inflowBlendWeight, int outflowDepth, float outflowStabilityWeight)
        {
            inflowBlendWeight = Mathf.Clamp01(inflowBlendWeight);
            outflowDepth = Math.Max(1, outflowDepth);
            outflowStabilityWeight = Mathf.Clamp01(outflowStabilityWeight);
            if (inflowBlendWeight <= 0f && outflowDepth <= 0)
            {
                return;
            }

            float stabilityBlend = 1f - outflowStabilityWeight * 0.5f;
            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            var buffer = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lakeStrength = lakes[x, z];
                    if (lakeStrength <= 0.25f)
                    {
                        continue;
                    }

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    if (downhill == Vector3Int.zero)
                    {
                        continue;
                    }

                    int currentX = x;
                    int currentZ = z;
                    float channelStrength = lakeStrength;

                    for (int step = 0; step < outflowDepth; step++)
                    {
                        currentX = Math.Clamp(currentX + downhill.x, 0, sizeX - 1);
                        currentZ = Math.Clamp(currentZ + downhill.z, 0, sizeZ - 1);

                        float flowInfluence = Mathf.Clamp01(flow[currentX, currentZ] * inflowBlendWeight);
                        float blended = Math.Max(channelStrength * 0.65f, lakeStrength * 0.35f);
                        float outflowValue = blended * stabilityBlend + flowInfluence * (1f - stabilityBlend);
                        buffer[currentX, currentZ] = Math.Max(buffer[currentX, currentZ], outflowValue);

                        if (downhill == Vector3Int.zero)
                        {
                            break;
                        }
                    }
                }
            }

            Array.Copy(buffer, lakes, buffer.Length);
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

        private static Vector3Int ComputeDownhillVector(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int bestDrop = 0;
            var best = Vector3Int.zero;

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
                    if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                    {
                        continue;
                    }

                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        best = new Vector3Int(dx, -drop, dz);
                    }
                }
            }

            return best;
        }

        private static float ComputeSlope(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int east = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int north = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            double dx = center - east;
            double dz = center - north;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static float SampleCurvature(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int forward = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            int back = heightMap[x, Math.Max(0, z - 1)];
            double laplacian = (left + right + forward + back - 4 * center) / 4.0;
            return (float)laplacian;
        }

        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
}
