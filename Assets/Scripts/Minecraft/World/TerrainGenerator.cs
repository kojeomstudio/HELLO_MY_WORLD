using System;
using System.IO;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Client-side terrain generation system that mirrors server-side generation
    /// Provides improved algorithms for caves, rivers, and lakes generation
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        // Noise generators
        private FastNoise _terrainNoise;
        private FastNoise _caveNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _biomeNoise;
        private FastNoise _oreNoise;
        
        // Cache for performance
        private readonly float[,] _heightMapCache = new float[16, 16];
        private readonly int[,] _biomeMapCache = new int[16, 16];
        private readonly float[,] _caveMapCache = new float[16, 256];
        private readonly float[,] _riverMapCache = new float[16, 16];
        private readonly float[,] _lakeMapCache = new float[16, 16];
        private TerrainTuning _tuning;

        [Serializable]
        private class TerrainTuning
        {
            public int RiverDepth;
            public float RiverEdgeFeather;
            public int RiverMouthSmoothRadius;
            public float RiverDeltaWetlandStrength;
            public int RiverIntensitySmoothIterations;
            public float RiverIntensitySmoothBlend;
            public int LakeOutflowCarveDepth;
            public float LakeWetlandSaturationThreshold;
            public float LakeShorelineBlend;
            public float CaveEdgeSealStrength;

            public static TerrainTuning FromDefaults(WorldConfig config)
            {
                return new TerrainTuning
                {
                    RiverDepth = Mathf.Max(2, config.Terrain.SeaLevel > 0 ? config.Terrain.SeaLevel / 10 : 6),
                    RiverEdgeFeather = Mathf.Clamp01(config.Water.RiverEdgeFeather),
                    RiverMouthSmoothRadius = Math.Max(1, config.Water.RiverMouthSmoothRadius),
                    RiverDeltaWetlandStrength = Mathf.Clamp01(config.Water.RiverDeltaWetlandStrength),
                    RiverIntensitySmoothIterations = Math.Max(1, config.Water.RiverIntensitySmoothIterations),
                    RiverIntensitySmoothBlend = Mathf.Clamp01(config.Water.RiverIntensitySmoothBlend),
                    LakeOutflowCarveDepth = Math.Max(1, config.Lakes.OutflowCarveDepth),
                    LakeWetlandSaturationThreshold = Mathf.Clamp01(config.Lakes.WetlandSaturationThreshold),
                    LakeShorelineBlend = Mathf.Clamp01(config.Lakes.ShorelineBlend),
                    CaveEdgeSealStrength = Mathf.Clamp01(config.Caves.EdgeSealStrength)
                };
            }
        }

        [Serializable]
        private class WorldConfigRaw
        {
            public WaterRaw Water = new WaterRaw();
            public LakeRaw Lakes = new LakeRaw();
            public CaveRaw Caves = new CaveRaw();
        }

        [Serializable]
        private class WaterRaw
        {
            public int RiverDepth = 6;
            public float RiverEdgeFeather = 0.35f;
            public int RiverMouthSmoothRadius = 3;
            public float RiverDeltaWetlandStrength = 0.35f;
            public int RiverIntensitySmoothIterations = 3;
            public float RiverIntensitySmoothBlend = 0.55f;
        }

        [Serializable]
        private class LakeRaw
        {
            public int OutflowCarveDepth = 2;
            public float WetlandSaturationThreshold = 0.55f;
            public float ShorelineBlend = 0.66f;
        }

        [Serializable]
        private class CaveRaw
        {
            public float EdgeSealStrength = 0.35f;
        }
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            _tuning = LoadTerrainTuning();
            
            InitializeNoiseGenerators();
        }
        
        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed;
            
            // Terrain noise
            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(_worldConfig.Terrain.Octaves);
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            
            // Cave noise
            _caveNoise = new FastNoise(seed + 1);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency);
            
            // River noise
            _riverNoise = new FastNoise(seed + 2);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            float riverNoiseScale = _worldConfig.Water.RiverNoiseScale <= 0f
                ? 0.003f
                : _worldConfig.Water.RiverNoiseScale;
            _riverNoise.SetFrequency(riverNoiseScale);
            
            // Lake noise
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            float lakeNoiseScale = 0.002f + Mathf.Clamp01(_worldConfig.Lakes.VarianceWeight) * 0.0005f;
            _lakeNoise.SetFrequency(lakeNoiseScale);
            
            // Biome noise
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Ore noise
            _oreNoise = new FastNoise(seed + 5);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
        }

        private TerrainTuning LoadTerrainTuning()
        {
            var tuning = TerrainTuning.FromDefaults(_worldConfig);
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var raw = JsonUtility.FromJson<WorldConfigRaw>(json);
                    if (raw != null)
                    {
                        tuning.RiverDepth = raw.Water.RiverDepth > 0 ? raw.Water.RiverDepth : tuning.RiverDepth;
                        tuning.RiverEdgeFeather = Mathf.Clamp01(raw.Water.RiverEdgeFeather);
                        tuning.RiverMouthSmoothRadius = Math.Max(1, raw.Water.RiverMouthSmoothRadius);
                        tuning.RiverDeltaWetlandStrength = Mathf.Clamp01(raw.Water.RiverDeltaWetlandStrength);
                        tuning.RiverIntensitySmoothIterations = Math.Max(1, raw.Water.RiverIntensitySmoothIterations);
                        tuning.RiverIntensitySmoothBlend = Mathf.Clamp01(raw.Water.RiverIntensitySmoothBlend);
                        tuning.LakeOutflowCarveDepth = Math.Max(1, raw.Lakes.OutflowCarveDepth);
                        tuning.LakeWetlandSaturationThreshold = Mathf.Clamp01(raw.Lakes.WetlandSaturationThreshold);
                        tuning.LakeShorelineBlend = Mathf.Clamp01(raw.Lakes.ShorelineBlend);
                        tuning.CaveEdgeSealStrength = Mathf.Clamp01(raw.Caves.EdgeSealStrength);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TerrainGenerator] Failed to load world-config.json tuning: {ex.Message}");
            }

            return tuning;
        }
        
        /// <summary>
        /// Generate terrain for a chunk
        /// </summary>
        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            var blocks = new int[chunkSize, worldHeight, chunkSize];
            
            // Generate height and biome maps
            GenerateHeightMap(chunkX, chunkZ);
            GenerateBiomeMap(chunkX, chunkZ);
            
            // Generate terrain features
            GenerateTerrain(blocks, chunkX, chunkZ);
            
            if (_worldConfig.Caves.EnableCaves)
            {
                GenerateCaves(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableRivers)
            {
                GenerateRivers(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableLakes)
            {
                GenerateLakes(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Ores.EnableOreGeneration)
            {
                GenerateOres(blocks, chunkX, chunkZ);
            }
            
            return blocks;
        }
        
        private void GenerateHeightMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Terrain.NoiseScale;
                    float worldZ = (chunkZ * chunkSize + z) * _worldConfig.Terrain.NoiseScale;
                    
                    // Base terrain height
                    float height = _terrainNoise.GetNoise(worldX, worldZ);
                    height = (height + 1f) * 0.5f; // Normalize to 0-1
                    
                    // Apply biome-specific modifications
                    int biome = _biomeMapCache[x, z];
                    height = ApplyBiomeHeightModifier(height, biome);
                    
                    _heightMapCache[x, z] = height;
                }
            }
        }
        
        private void GenerateBiomeMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Terrain.BiomeScale;
                    float worldZ = (chunkZ * chunkSize + z) * _worldConfig.Terrain.BiomeScale;
                    
                    float noise = _biomeNoise.GetNoise(worldX, worldZ);
                    noise = (noise + 1f) * 0.5f; // Normalize to 0-1
                    
                    // Determine biome based on noise value
                    int biome = DetermineBiome(noise);
                    _biomeMapCache[x, z] = biome;
                }
            }
        }
        
        private float ApplyBiomeHeightModifier(float baseHeight, int biome)
        {
            return biome switch
            {
                0 => baseHeight * 0.8f, // Plains - lower terrain
                1 => baseHeight * 1.3f, // Mountains - higher terrain
                2 => baseHeight * 0.9f, // Forest - slightly lower
                3 => baseHeight * 0.7f, // Desert - lower with dunes
                4 => baseHeight * 1.1f, // Hills - slightly higher
                _ => baseHeight
            };
        }
        
        private int DetermineBiome(float noiseValue)
        {
            return noiseValue switch
            {
                < 0.2f => 3, // Desert
                < 0.4f => 0, // Plains
                < 0.6f => 2, // Forest
                < 0.8f => 4, // Hills
                _ => 1       // Mountains
            };
        }
        
        private void GenerateTerrain(int[,,] blocks, int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float heightValue = _heightMapCache[x, z];
                    int terrainHeight = Mathf.RoundToInt(heightValue * _worldConfig.Terrain.MountainMaxHeight);
                    terrainHeight = Mathf.Clamp(terrainHeight, 5, worldHeight - 5);
                    
                    int biome = _biomeMapCache[x, z];
                    
                    // Generate terrain layers
                    for (int y = 0; y < terrainHeight; y++)
                    {
                        int blockId = GetBlockForTerrainLayer(y, terrainHeight, seaLevel, biome);
                        blocks[x, y, z] = blockId;
                    }
                    
                    // Add water below sea level
                    for (int y = terrainHeight; y < seaLevel; y++)
                    {
                        blocks[x, y, z] = GetBlockId("water");
                    }
                }
            }
            
            // Add bedrock layer
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    blocks[x, _worldConfig.Terrain.BedrockLevel, z] = GetBlockId("bedrock");
                }
            }
        }
        
        private int GetBlockForTerrainLayer(int y, int terrainHeight, int seaLevel, int biome)
        {
            // Surface layers
            if (y == terrainHeight - 1)
            {
                if (y < seaLevel - 1)
                    return GetBlockId("dirt");
                
                return biome switch
                {
                    0 => GetBlockId("grass"), // Plains
                    1 => GetBlockId("stone"), // Mountains
                    2 => GetBlockId("grass"), // Forest
                    3 => GetBlockId("sand"),  // Desert
                    4 => GetBlockId("grass"), // Hills
                    _ => GetBlockId("grass")
                };
            }
            
            // Sub-surface layers
            if (y >= terrainHeight - 4)
            {
                if (biome == 3) // Desert
                    return GetBlockId("sandstone");
                return GetBlockId("dirt");
            }
            
            // Underground
            return GetBlockId("stone");
        }
        
        private void GenerateCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Caves.EnableCaves) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate cave density map
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Caves.HorizontalFrequency;
                    float worldY = y * _worldConfig.Caves.VerticalFrequency;
                    
                    float caveValue = _caveNoise.GetNoise(worldX, worldY);
                    _caveMapCache[x, y] = caveValue;
                }
            }

            SmoothCaveField(_worldConfig.Caves.StabilitySmoothIterations, _worldConfig.Caves.StabilitySmoothBlend);
            
            // Apply cave generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = _worldConfig.Caves.MinCaveHeight; y < _worldConfig.Caves.MaxCaveHeight; y++)
                    {
                        float caveValue = _caveMapCache[x, y];
                        float normalized = Mathf.Clamp01((caveValue + 1f) * 0.5f);
                        float threshold = _worldConfig.Caves.CaveThreshold > 0f
                            ? _worldConfig.Caves.CaveThreshold
                            : _worldConfig.Caves.Threshold;

                        if (normalized > threshold)
                        {
                            float liquidityNoise = Mathf.PerlinNoise((chunkX * chunkSize + x) * 0.037f, (chunkZ * chunkSize + z + y) * 0.041f);
                            bool shouldFlood = normalized > _worldConfig.Caves.WaterThreshold &&
                                               y < _worldConfig.Water.GlobalWaterLevel - 4 &&
                                               liquidityNoise > 0.55f;
                            bool shouldLava = normalized > _worldConfig.Caves.LavaThreshold &&
                                              y < Mathf.Max(8, _worldConfig.Terrain.BedrockLevel + 4) &&
                                              liquidityNoise < 0.35f;

                            if (shouldLava)
                            {
                                blocks[x, y, z] = GetBlockId("lava");
                            }
                            else if (shouldFlood)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                            else
                            {
                                blocks[x, y, z] = 0; // Air pocket
                            }
                        }
                    }
                }
            }

            SealEdgeCaves(blocks, chunkX, chunkZ);
        }

        private void SmoothCaveField(int iterations, float blend)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int steps = Math.Max(0, iterations);
            float lerp = Mathf.Clamp01(blend);

            if (steps == 0 || lerp <= 0f)
            {
                return;
            }

            for (int iteration = 0; iteration < steps; iteration++)
            {
                var temp = new float[chunkSize, worldHeight];
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        float sum = 0f;
                        int count = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = Mathf.Clamp(x + dx, 0, chunkSize - 1);
                                int ny = Mathf.Clamp(y + dy, 0, worldHeight - 1);
                                sum += _caveMapCache[nx, ny];
                                count++;
                            }
                        }

                        float average = count > 0 ? sum / count : _caveMapCache[x, y];
                        temp[x, y] = Mathf.Lerp(_caveMapCache[x, y], average, lerp);
                    }
                }

                for (int x = 0; x < chunkSize; x++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        _caveMapCache[x, y] = temp[x, y];
                    }
                }
            }
        }
        
        private void GenerateRivers(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableRivers) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate river map
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    float warpFreq = Mathf.Max(0.00001f, _worldConfig.Water.HydrologyWarpFrequency);
                    float warpAmp = _worldConfig.Water.HydrologyWarpAmplitude;
                    float warpedX = worldX + Mathf.Sin(worldZ * warpFreq) * warpAmp;
                    float warpedZ = worldZ + Mathf.Cos(worldX * warpFreq) * warpAmp;
                    
                    float riverValue = _riverNoise.GetNoise(warpedX, warpedZ);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    _riverMapCache[x, z] = riverValue;
                }
            }

            ApplyHydrologySmoothing(_worldConfig.Water.HydrologySmoothIterations, _worldConfig.Water.HydrologySmoothBlend);
            SmoothRiverMapEdges();
            
            // Apply river generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float riverValue = _riverMapCache[x, z];
                    
                    if (riverValue < _worldConfig.Water.RiverCenterThreshold)
                    {
                        // River center - carve down to water level
                        for (int y = seaLevel; y < worldHeight; y++)
                        {
                            if (blocks[x, y, z] != 0) // If not already air
                            {
                                blocks[x, y, z] = 0; // Air
                            }
                        }
                        
                        // Add water at river bottom
                        for (int y = seaLevel - _tuning.RiverDepth; y < seaLevel; y++)
                        {
                            if (y >= 0 && y < worldHeight)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                    }
                    else if (riverValue < _worldConfig.Water.RiverBankThreshold)
                    {
                        // River banks - lower terrain
                        for (int y = seaLevel; y < worldHeight; y++)
                        {
                            if (blocks[x, y, z] != 0 && blocks[x, y, z] != GetBlockId("water"))
                            {
                                // Replace with sand or gravel for river banks
                                if (UnityEngine.Random.value < 0.7f)
                                    blocks[x, y, z] = GetBlockId("sand");
                                else
                                    blocks[x, y, z] = GetBlockId("gravel");
                            }
                        }
                    }
                }
            }

            FeatherRiverBanksAndMouths(blocks, chunkX, chunkZ, seaLevel);
        }
        
        private void GenerateLakes(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableLakes) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate lake map
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    float lakeValue = _lakeNoise.GetNoise(worldX, worldZ);
                    lakeValue = (lakeValue + 1f) * 0.5f; // Normalize to 0-1
                    _lakeMapCache[x, z] = lakeValue;
                }
            }

            SmoothLakeMap(_worldConfig.Lakes.LakeBasinSmoothIterations, _worldConfig.Lakes.VarianceWeight);
            
            // Apply lake generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    float spawnThreshold = Mathf.Clamp01(0.7f + _worldConfig.Lakes.SpawnWeightBias * 0.2f);
                    float riverInfluence = _riverMapCache[x, z];
                    bool suppressedByRiver = _worldConfig.Water.EnableRivers &&
                                             _worldConfig.Lakes.RiverProximitySuppression > 0f &&
                                             riverInfluence > _worldConfig.Water.RiverBankThreshold * _worldConfig.Lakes.RiverProximitySuppression;

                    if (lakeValue > spawnThreshold && !suppressedByRiver)
                    {
                        // Find terrain height
                        int terrainHeight = 0;
                        for (int y = worldHeight - 1; y >= 0; y--)
                        {
                            if (blocks[x, y, z] != 0)
                            {
                                terrainHeight = y;
                                break;
                            }
                        }
                        
                        // Create lake basin
                        int lakeDepth = Mathf.RoundToInt(Mathf.Lerp(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth, lakeValue));
                        lakeDepth = Mathf.Clamp(lakeDepth, _worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth);
                        int lakeBottom = Mathf.Max(terrainHeight - lakeDepth, 1);
                        
                        for (int y = lakeBottom; y <= terrainHeight; y++)
                        {
                            if (blocks[x, y, z] != 0)
                            {
                                blocks[x, y, z] = 0; // Remove terrain
                            }
                        }
                        
                        // Fill with water
                        for (int y = lakeBottom; y < lakeBottom + lakeDepth / 2; y++)
                        {
                            if (y >= 0 && y < worldHeight)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                        
                        // Add sand/sandstone around lake edges
                        for (int y = lakeBottom - 1; y <= lakeBottom + 1; y++)
                        {
                            if (y >= 0 && y < worldHeight && blocks[x, y, z] != 0)
                            {
                                if (UnityEngine.Random.value < 0.8f)
                                    blocks[x, y, z] = GetBlockId("sand");
                                else
                                    blocks[x, y, z] = GetBlockId("sandstone");
                            }
                        }

                        int waterSurface = Mathf.Clamp(lakeBottom + Mathf.Max(1, lakeDepth / 2), 1, worldHeight - 1);
                        CarveLakeOutflow(blocks, chunkX, chunkZ, x, z, waterSurface);
                    }
                }
            }

            BlendLakeWetlands(blocks, seaLevel);
        }

        private void SmoothLakeMap(int iterations, float blend)
        {
            int steps = Math.Max(0, iterations);
            float lerp = Mathf.Clamp01(blend);
            int chunkSize = _worldConfig.ChunkSize;

            if (steps == 0 || lerp <= 0f)
            {
                return;
            }

            for (int i = 0; i < steps; i++)
            {
                var temp = new float[chunkSize, chunkSize];
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = Mathf.Clamp(x + dx, 0, chunkSize - 1);
                                int nz = Mathf.Clamp(z + dz, 0, chunkSize - 1);
                                sum += _lakeMapCache[nx, nz];
                                count++;
                            }
                        }

                        float average = count > 0 ? sum / count : _lakeMapCache[x, z];
                        temp[x, z] = Mathf.Lerp(_lakeMapCache[x, z], average, lerp);
                    }
                }

                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        _lakeMapCache[x, z] = temp[x, z];
                    }
                }
            }
        }

        private void ApplyHydrologySmoothing(int iterations, float blend)
        {
            int steps = Math.Max(0, iterations);
            float lerp = Mathf.Clamp01(blend);
            for (int i = 0; i < steps; i++)
            {
                BlurRiverIntensity(lerp);
            }
        }

        private void SmoothRiverMapEdges()
        {
            if (_tuning.RiverEdgeFeather <= 0f)
                return;

            int chunkSize = _worldConfig.ChunkSize;
            int radius = Mathf.Clamp(_tuning.RiverMouthSmoothRadius, 1, Math.Max(1, chunkSize / 2));
            float blendBase = Mathf.Clamp01(_tuning.RiverEdgeFeather);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(chunkSize - 1 - x, chunkSize - 1 - z));
                    if (edgeDistance >= radius)
                        continue;

                    int inwardX = x <= chunkSize / 2 ? 1 : -1;
                    int inwardZ = z <= chunkSize / 2 ? 1 : -1;
                    int sampleX = Mathf.Clamp(x + inwardX, 0, chunkSize - 1);
                    int sampleZ = Mathf.Clamp(z + inwardZ, 0, chunkSize - 1);
                    float neighbor = _riverMapCache[sampleX, sampleZ];
                    float blend = blendBase * (1f - edgeDistance / (float)radius);
                    _riverMapCache[x, z] = Mathf.Lerp(_riverMapCache[x, z], neighbor, blend);
                }
            }

            int iterations = Math.Max(1, _tuning.RiverIntensitySmoothIterations);
            float intensityBlend = Mathf.Clamp01(_tuning.RiverIntensitySmoothBlend);
            for (int i = 0; i < iterations; i++)
            {
                BlurRiverIntensity(intensityBlend);
            }
        }

        private void BlurRiverIntensity(float blend)
        {
            int chunkSize = _worldConfig.ChunkSize;
            var temp = new float[chunkSize, chunkSize];

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float sum = 0f;
                    int samples = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, chunkSize - 1);
                            int nz = Mathf.Clamp(z + dz, 0, chunkSize - 1);
                            sum += _riverMapCache[nx, nz];
                            samples++;
                        }
                    }

                    float average = samples > 0 ? sum / samples : _riverMapCache[x, z];
                    temp[x, z] = Mathf.Lerp(_riverMapCache[x, z], average, blend);
                }
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    _riverMapCache[x, z] = temp[x, z];
                }
            }
        }

        private void FeatherRiverBanksAndMouths(int[,,] blocks, int chunkX, int chunkZ, int seaLevel)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            float bankThreshold = _worldConfig.Water.RiverBankThreshold;
            int radius = Mathf.Clamp(_tuning.RiverMouthSmoothRadius, 1, Math.Max(1, chunkSize / 2));
            float wetland = Mathf.Clamp01(_tuning.RiverDeltaWetlandStrength);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float riverValue = _riverMapCache[x, z];
                    if (riverValue <= 0f || riverValue >= bankThreshold)
                        continue;

                    int surface = GetSurfaceHeight(blocks, x, z, worldHeight);
                    if (surface < 0 || surface > seaLevel + radius)
                        continue;

                    float falloff = 1f - Mathf.Clamp01(Mathf.Abs(surface - seaLevel) / (radius + 0.5f));
                    float strength = Mathf.Clamp01(1f - (riverValue / bankThreshold));
                    int target = Math.Max(1, surface - Mathf.CeilToInt((_tuning.RiverDepth + 1) * falloff * strength));
                    int waterTop = Math.Min(surface, seaLevel);

                    for (int y = target; y <= waterTop && y < worldHeight; y++)
                    {
                        blocks[x, y, z] = GetBlockId("water");
                    }

                    if (wetland > 0f && falloff > 0.2f)
                    {
                        int bankY = Math.Max(1, target - 1);
                        blocks[x, bankY, z] = wetland > 0.5f ? GetBlockId("clay") : GetBlockId("sand");
                        if (waterTop + 1 < worldHeight)
                        {
                            blocks[x, waterTop + 1, z] = 0;
                        }
                    }
                }
            }
        }

        private void SealEdgeCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (_tuning.CaveEdgeSealStrength <= 0f)
                return;

            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int radius = Mathf.Clamp(Mathf.CeilToInt(3 + _tuning.CaveEdgeSealStrength * 4f), 1, Math.Max(1, chunkSize / 2));

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(chunkSize - 1 - x, chunkSize - 1 - z));
                    if (edgeDistance >= radius)
                        continue;

                    int surface = GetSurfaceHeight(blocks, x, z, worldHeight);
                    if (surface < 0)
                        continue;

                    float seal = Mathf.Clamp01(_tuning.CaveEdgeSealStrength * (1f - edgeDistance / (float)radius));
                    int minY = Math.Max(1, surface - 10);

                    for (int y = minY; y < surface; y++)
                    {
                        if (blocks[x, y, z] != 0)
                            continue;

                        float jitter = Mathf.Abs(Mathf.PerlinNoise((chunkX * chunkSize + x) * 0.17f, (chunkZ * chunkSize + z + y) * 0.19f));
                        if (jitter < seal)
                        {
                            blocks[x, y, z] = GetBlockId("stone");
                        }
                    }
                }
            }
        }

        private void CarveLakeOutflow(int[,,] blocks, int chunkX, int chunkZ, int startX, int startZ, int waterSurface)
        {
            if (_tuning.LakeOutflowCarveDepth <= 0)
                return;

            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            Vector2Int direction = GetSteepestNeighborDirection(blocks, startX, startZ, worldHeight);
            if (direction == Vector2Int.zero)
            {
                direction = new Vector2Int(0, 1);
            }

            int length = Mathf.Clamp(_tuning.RiverMouthSmoothRadius, 2, Math.Max(2, chunkSize / 2));
            int currentX = startX;
            int currentZ = startZ;

            for (int step = 0; step < length; step++)
            {
                currentX += direction.x;
                currentZ += direction.y;
                if (currentX < 1 || currentX >= chunkSize - 1 || currentZ < 1 || currentZ >= chunkSize - 1)
                {
                    break;
                }

                int surface = GetSurfaceHeight(blocks, currentX, currentZ, worldHeight);
                if (surface < 0)
                {
                    break;
                }

                int target = Math.Max(1, waterSurface - _tuning.LakeOutflowCarveDepth + step / 2);
                for (int y = target; y <= waterSurface && y < worldHeight; y++)
                {
                    blocks[currentX, y, currentZ] = GetBlockId("water");
                }

                int bankY = Math.Max(1, target - 1);
                blocks[currentX, bankY, currentZ] = GetBlockId("sand");
            }
        }

        private void BlendLakeWetlands(int[,,] blocks, int seaLevel)
        {
            if (_tuning.LakeWetlandSaturationThreshold <= 0f)
                return;

            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int clayId = GetBlockId("clay");
            int sandId = GetBlockId("sand");

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = GetSurfaceHeight(blocks, x, z, worldHeight);
                    if (surface < 1 || surface > worldHeight - 2)
                    {
                        continue;
                    }

                    bool hasWater = ColumnHasWater(blocks, x, z, worldHeight, Math.Max(0, surface - 3), surface + 1);
                    if (!hasWater)
                    {
                        continue;
                    }

                    float shoreline = Mathf.Clamp01(_tuning.LakeShorelineBlend);
                    int shoreBlock = shoreline > _tuning.LakeWetlandSaturationThreshold ? clayId : sandId;
                    blocks[x, surface, z] = shoreBlock;
                    if (surface + 1 < worldHeight)
                    {
                        blocks[x, surface + 1, z] = 0;
                    }
                }
            }
        }

        private int GetSurfaceHeight(int[,,] blocks, int x, int z, int worldHeight)
        {
            for (int y = worldHeight - 1; y >= 0; y--)
            {
                if (blocks[x, y, z] != 0)
                {
                    return y;
                }
            }
            return -1;
        }

        private bool ColumnHasWater(int[,,] blocks, int x, int z, int worldHeight, int minY, int maxY)
        {
            int waterId = GetBlockId("water");
            minY = Mathf.Clamp(minY, 0, worldHeight - 1);
            maxY = Mathf.Clamp(maxY, 0, worldHeight - 1);

            for (int y = maxY; y >= minY; y--)
            {
                if (blocks[x, y, z] == waterId)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int GetSteepestNeighborDirection(int[,,] blocks, int x, int z, int worldHeight)
        {
            int centerHeight = GetSurfaceHeight(blocks, x, z, worldHeight);
            int bestDrop = 0;
            Vector2Int bestDir = Vector2Int.zero;
            Vector2Int[] directions =
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int nz = z + dir.y;
                if (nx < 0 || nx >= _worldConfig.ChunkSize || nz < 0 || nz >= _worldConfig.ChunkSize)
                {
                    continue;
                }

                int neighborHeight = GetSurfaceHeight(blocks, nx, nz, worldHeight);
                if (neighborHeight < 0)
                {
                    continue;
                }

                int drop = centerHeight - neighborHeight;
                if (drop > bestDrop)
                {
                    bestDrop = drop;
                    bestDir = dir;
                }
            }

            return bestDir;
        }
        
        private void GenerateOres(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Ores.EnableOreGeneration) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate ore veins
            foreach (var oreEntry in _worldConfig.Ores.Ores)
            {
                string oreName = oreEntry.Key;
                var oreConfig = oreEntry.Value;
                
                if (oreConfig == null) continue;
                
                int veinsPerChunk = oreConfig.VeinsPerChunk;
                
                for (int i = 0; i < veinsPerChunk; i++)
                {
                    // Random position within chunk
                    int veinX = UnityEngine.Random.Range(0, chunkSize);
                    int veinY = UnityEngine.Random.Range(oreConfig.MinHeight, oreConfig.MaxHeight);
                    int veinZ = UnityEngine.Random.Range(0, chunkSize);
                    
                    // Generate vein
                    GenerateOreVein(blocks, veinX, veinY, veinZ, oreName, oreConfig.VeinSize);
                }
            }
        }
        
        private void GenerateOreVein(int[,,] blocks, int startX, int startY, int startZ, string oreName, int veinSize)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            // Simple blob-like vein generation
            for (int i = 0; i < veinSize; i++)
            {
                // Random walk from starting position
                int x = startX + UnityEngine.Random.Range(-2, 3);
                int y = startY + UnityEngine.Random.Range(-2, 3);
                int z = startZ + UnityEngine.Random.Range(-2, 3);
                
                // Check bounds
                if (x >= 0 && x < chunkSize && y >= 0 && y < worldHeight && z >= 0 && z < chunkSize)
                {
                    // Only replace stone
                    if (blocks[x, y, z] == GetBlockId("stone"))
                    {
                        blocks[x, y, z] = oreBlockId;
                    }
                }
            }
        }
        
        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
    
    /// <summary>
    /// Fast noise implementation for terrain generation
    /// Simplified version of FastNoise library
    /// </summary>
    public class FastNoise
    {
        public enum NoiseType
        {
            Simplex,
            SimplexFractal,
            WhiteNoise
        }
        
        private int _seed;
        private NoiseType _noiseType;
        private float _frequency = 0.01f;
        private int _fractalOctaves = 3;
        private float _fractalLacunarity = 2.0f;
        private float _fractalGain = 0.5f;
        
        public FastNoise(int seed)
        {
            _seed = seed;
        }
        
        public void SetNoiseType(NoiseType type) => _noiseType = type;
        public void SetFrequency(float frequency) => _frequency = frequency;
        public void SetFractalOctaves(int octaves) => _fractalOctaves = octaves;
        public void SetFractalLacunarity(float lacunarity) => _fractalLacunarity = lacunarity;
        public void SetFractalGain(float gain) => _fractalGain = gain;
        
        public float GetNoise(float x, float y)
        {
            return _noiseType switch
            {
                NoiseType.Simplex => Simplex(x, y),
                NoiseType.SimplexFractal => FractalSimplex(x, y),
                NoiseType.WhiteNoise => WhiteNoise(x, y),
                _ => 0f
            };
        }
        
        private float Simplex(float x, float y)
        {
            // Simplified simplex noise implementation
            float s = (x + y) * 0.366025403f;
            int i = Mathf.FloorToInt(x + s);
            int j = Mathf.FloorToInt(y + s);
            
            float t = (i + j) * 0.211324865f;
            float X0 = i - t;
            float Y0 = j - t;
            float x0 = x - X0;
            float y0 = y - Y0;
            
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }
            
            float x1 = x0 - i1 + 0.211324865f;
            float y1 = y0 - j1 + 0.211324865f;
            float x2 = x0 - 1.0f + 0.422649730f;
            float y2 = y0 - 1.0f + 0.422649730f;
            
            int ii = i & 255;
            int jj = j & 255;
            
            float n0 = Grad(ii + jj + _seed, x0, y0);
            float n1 = Grad(ii + i1 + jj + j1 + _seed, x1, y1);
            float n2 = Grad(ii + 1 + jj + 1 + _seed, x2, y2);
            
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0f) n0 = 0f;
            else t0 *= t0;
            
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0f) n1 = 0f;
            else t1 *= t1;
            
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0f) n2 = 0f;
            else t2 *= t2;
            
            return 70.0f * (n0 * t0 * t0 * t0 + n1 * t1 * t1 * t1 + n2 * t2 * t2 * t2);
        }
        
        private float FractalSimplex(float x, float y)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = _frequency;
            
            for (int i = 0; i < _fractalOctaves; i++)
            {
                sum += Simplex(x * frequency, y * frequency) * amplitude;
                amplitude *= _fractalGain;
                frequency *= _fractalLacunarity;
            }
            
            return sum;
        }
        
        private float WhiteNoise(float x, float y)
        {
            // Simple pseudo-random number generator
            int n = (int)Mathf.Floor(x * 1000 + y * 1000 + _seed * 1000);
            n = (n << 13) ^ n;
            n = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return (n / 1073741824.0f) - 1f;
        }
        
        private float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
