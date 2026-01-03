
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
            public int LakeOutflowCarveDepth = 3;
            public int LakeWetlandBufferRadius = 2;
            public float LakeWetlandSaturationThreshold = 0.55f;
            public float LakeShorelineBlend = 0.66f;
            public float CaveConnectivityThreshold = 0.42f;
            public float CaveMoisturePenalty = 0.35f;
            public float CaveFlowPenalty = 0.25f;
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
                    LakeOutflowCarveDepth = Math.Max(1, config.Lakes.OutflowCarveDepth),
                    LakeWetlandBufferRadius = Math.Max(0, config.Lakes.WetlandBufferRadius),
                    LakeWetlandSaturationThreshold = Mathf.Clamp(config.Lakes.WetlandSaturationThreshold, 0.0f, 1.0f),
                    LakeShorelineBlend = Mathf.Clamp(config.Lakes.ShorelineBlend, 0.0f, 1.0f)
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
                LakeOutflowCarveDepth = Math.Max(1, overrides.LakeOutflowCarveDepth);
                LakeWetlandBufferRadius = Math.Max(0, overrides.LakeWetlandBufferRadius);
                LakeWetlandSaturationThreshold = Mathf.Clamp(overrides.LakeWetlandSaturationThreshold, 0.0f, 1.0f);
                LakeShorelineBlend = Mathf.Clamp(overrides.LakeShorelineBlend, 0.0f, 1.0f);
                CaveConnectivityThreshold = Mathf.Clamp(overrides.CaveConnectivityThreshold, 0.1f, 0.9f);
                CaveMoisturePenalty = Mathf.Clamp01(overrides.CaveMoisturePenalty);
                CaveFlowPenalty = Mathf.Clamp01(overrides.CaveFlowPenalty);
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
            BlendHydrologyWithFlow(hydrology, flow);

            float[,] riverMask = _enableRivers ? BuildRiverMask(heightMap, hydrology, flow, chunkX, chunkZ) : new float[_chunkSize, _chunkSize];
            float[,] lakeMask = _enableLakes ? BuildLakeMask(heightMap, hydrology, flow, riverMask, chunkX, chunkZ) : new float[_chunkSize, _chunkSize];
            bool[,,]? caveMask = _enableCaves ? BuildCaveMask(heightMap, hydrology, flow, riverMask, chunkX, chunkZ) : null;

            FillTerrain(blocks, heightMap);
            ApplyRivers(blocks, heightMap, riverMask);
            ApplyLakes(blocks, heightMap, lakeMask);
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
                    flow[x, z] = Mathf.Clamp((float)(accumulation + hydrologyBoost), 0f, 12f);
                }
            }

            SmoothField(flow, _worldConfig.Water.HydrologySmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            return flow;
        }

        private void BlendHydrologyWithFlow(float[,] hydrology, float[,] flow)
        {
            float flowBlend = Mathf.Clamp(_worldConfig.Water.HydrologyContinuityWeight * 0.35f, 0.05f, 0.45f);
            float confluenceBoost = Mathf.Clamp(_tuning.RiverConfluenceBoost, 0f, 2f);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighborFlow = SampleInterior(flow, x, z) / Mathf.Max(1f, _worldConfig.Water.RiverDepth);
                    float neighborHydro = SampleInterior(hydrology, x, z);

                    float blend = Mathf.Clamp(flowBlend, 0f, 0.9f);
                    float confluence = confluenceBoost > 0f ? (neighborFlow * 0.5f + neighborHydro * 0.25f) * confluenceBoost : 0f;

                    float blended = hydro * (1f - blend) + Mathf.Clamp(flowValue / Mathf.Max(1f, _worldConfig.Water.RiverDepth), 0f, 1f) * blend;
                    blended *= 1f + confluence;
                    hydrology[x, z] = Mathf.Clamp(blended, 0f, 1.25f);
                }
            }

            ClampVariance(hydrology, Math.Max(_tuning.HydrologyVarianceClamp, _worldConfig.Water.HydrologyVarianceClamp));
        }

        private float[,] BuildRiverMask(int[,] heightMap, float[,] hydrology, float[,] flow, int chunkX, int chunkZ)
        {
            var mask = new float[_chunkSize, _chunkSize];
            double noiseScale = Math.Max(0.0001, _worldConfig.Water.RiverNoiseScale);
            double confluenceBoost = Math.Clamp(_tuning.RiverConfluenceBoost, 0.0, 2.0);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    int worldX = chunkX * _chunkSize + x;
                    int worldZ = chunkZ * _chunkSize + z;
                    double baseNoise = Math.Abs(_riverNoise.GetNoise(worldX * (float)noiseScale, worldZ * (float)noiseScale));

                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double gradient = ComputeSlope(heightMap, x, z);
                    double relief = Math.Max(0, heightMap[x, z] - _seaLevel) / Math.Max(1, _seaLevel);

                    double pressure = _worldConfig.Water.RiverBankThreshold - baseNoise;
                    pressure = Math.Max(0.0, pressure);
                    pressure *= 1.0 + hydrologySample * _worldConfig.Water.HydrologyContinuityWeight;
                    pressure *= 1.0 + flowSample * _worldConfig.Water.RiverFlowAlignmentWeight;
                    pressure *= 1.0 - Math.Clamp(gradient * _worldConfig.Water.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * _worldConfig.Water.RiverReliefPenaltyWeight, 0.0, 0.35);
                    if (confluenceBoost > 0.0)
                    {
                        double neighborFlow = SampleInterior(flow, x, z) / 6.0;
                        double tributary = Math.Clamp((flowSample + neighborFlow) * 0.5, 0.0, 1.0);
                        pressure *= 1.0 + tributary * confluenceBoost * 0.35;
                    }

                    mask[x, z] = Mathf.Clamp((float)pressure, 0f, 1.35f);
                }
            }

            SmoothField(mask, _worldConfig.Water.RiverIntensitySmoothIterations, _worldConfig.Water.RiverIntensitySmoothBlend);
            ApplyEdgeFeather(mask, Math.Max(_tuning.RiverEdgeFeather, _worldConfig.Water.RiverEdgeFeather));
            return mask;
        }

        private float[,] BuildLakeMask(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask, int chunkX, int chunkZ)
        {
            var lakes = new float[_chunkSize, _chunkSize];

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

                    double basinNoise = _lakeNoise.GetNoise(worldX * 0.004f, worldZ * 0.004f);
                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double weight = basinNoise * 0.6 + hydrologySample * 0.3 + flowSample * 0.1 + _worldConfig.Lakes.SpawnWeightBias;

                    if (weight > _tuning.LakeWetlandSaturationThreshold && heightMap[x, z] > _seaLevel - _worldConfig.Lakes.MaxDepth)
                    {
                        lakes[x, z] = Mathf.Clamp01((float)weight);
                    }
                }
            }

            SmoothField(lakes, _worldConfig.Lakes.LakeBasinSmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            ApplyRiparianBuffer(lakes, Math.Min(_tuning.LakeWetlandBufferRadius, _worldConfig.Lakes.MaxRadius), _tuning.LakeShorelineBlend);
            ApplyOutflowChannels(lakes, heightMap, flow, _worldConfig.Water.LakeInflowBlendWeight, _tuning.LakeOutflowCarveDepth);
            return lakes;
        }

        private bool[,,] BuildCaveMask(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask, int chunkX, int chunkZ)
        {
            var mask = new bool[_chunkSize, _worldHeight, _chunkSize];
            int minCave = Math.Max(1, _worldConfig.Caves.MinCaveHeight);
            int maxCave = Math.Min(_worldHeight - 2, _worldConfig.Caves.MaxCaveHeight);

            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    for (int y = minCave; y < maxCave; y++)
                    {
                        float worldX = chunkX * _chunkSize + x;
                        float worldZ = chunkZ * _chunkSize + z;
                        float noise = _caveNoise.GetNoise(worldX * _worldConfig.Caves.HorizontalFrequency, worldZ * _worldConfig.Caves.HorizontalFrequency + y * _worldConfig.Caves.VerticalFrequency);
                        double threshold = _tuning.CaveConnectivityThreshold + hydrology[x, z] * _tuning.CaveMoisturePenalty + flow[x, z] * _tuning.CaveFlowPenalty + riverMask[x, z] * _worldConfig.Caves.RiverSuppressionWeight;

                        if (noise > threshold)
                        {
                            mask[x, y, z] = true;
                        }
                    }
                }
            }

            SmoothField(mask, _worldConfig.Caves.StabilitySmoothIterations, _worldConfig.Caves.StabilitySmoothBlend);
            return mask;
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

        private void ApplyRivers(int[,,] blocks, int[,] heightMap, float[,] riverMask)
        {
            if (!_enableRivers)
            {
                return;
            }

            int waterId = GetBlockId("water");
            int sandId = GetBlockId("sand");

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
                    int depth = Mathf.Clamp(Mathf.RoundToInt(_worldConfig.Water.RiverDepth * (pressure + 0.35f)), 2, _worldConfig.Water.RiverDepth + 3);
                    int waterLevel = Math.Max(1, Math.Min(surface, _seaLevel));
                    int bottom = Math.Max(1, waterLevel - depth);

                    for (int y = bottom; y <= waterLevel; y++)
                    {
                        blocks[x, y, z] = waterId;
                    }

                    blocks[x, bottom, z] = sandId;
                }
            }
        }

        private void ApplyLakes(int[,,] blocks, int[,] heightMap, float[,] lakeMask)
        {
            if (!_enableLakes)
            {
                return;
            }

            int waterId = GetBlockId("water");
            int sandId = GetBlockId("sand");

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
                    int depth = Mathf.Clamp(Mathf.RoundToInt(_worldConfig.Lakes.MinDepth + lake * (_worldConfig.Lakes.MaxDepth - _worldConfig.Lakes.MinDepth)), _worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth + 2);
                    int bottom = Math.Max(1, surface - depth);

                    for (int y = bottom; y <= surface; y++)
                    {
                        blocks[x, y, z] = y <= _seaLevel ? waterId : 0;
                    }

                    blocks[x, bottom, z] = sandId;
                }
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

        private static void SmoothField(bool[,,] field, int iterations, float blend)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
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
