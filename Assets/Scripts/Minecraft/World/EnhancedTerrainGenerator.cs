using System;
using System.IO;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced terrain generation system with improved algorithms for caves, rivers, and lakes
    /// Addresses issues with connectivity, natural formations, and seamless chunk boundaries
    /// </summary>
    public class EnhancedTerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        // Noise generators with improved parameters
        private FastNoise _terrainNoise;
        private FastNoise _caveNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _biomeNoise;
        private FastNoise _oreNoise;
        private FastNoise _detailNoise;
        
        // Cache for performance
        private readonly float[,] _heightMapCache = new float[16, 16];
        private readonly int[,] _biomeMapCache = new int[16, 16];
        private readonly float[,] _caveMapCache = new float[16, 256];
        private readonly float[,] _riverMapCache = new float[16, 16];
        private readonly float[,] _lakeMapCache = new float[16, 16];
        private readonly float[,] _detailMapCache = new float[16, 16];
        
        // Enhanced tuning parameters
        private EnhancedTerrainTuning _tuning;
        
        [Serializable]
        private class EnhancedTerrainTuning
        {
            // Cave improvements
            public float CaveConnectivityThreshold = 0.3f;
            public float CaveTunnelWidth = 0.15f;
            public int CaveMinTunnelHeight = 2;
            public float CaveLavaPoolChance = 0.08f;
            public float CaveWaterPoolChance = 0.12f;
            public bool EnableMultiLevelCaves = true;
            
            // River improvements
            public float RiverMeanderStrength = 0.7f;
            public float RiverWidthVariation = 0.4f;
            public int RiverMinWidth = 2;
            public int RiverMaxWidth = 8;
            public float RiverDeltaFertility = 0.6f;
            public bool EnableRiverTributaries = true;
            
            // Lake improvements
            public float LakeDepthVariation = 0.5f;
            public float LakeShoreComplexity = 0.3f;
            public int LakeMinDepth = 3;
            public int LakeMaxDepth = 12;
            public float LakeIslandChance = 0.05f;
            public bool EnableLakeOutflow = true;
            
            // General improvements
            public float BiomeTransitionSmoothing = 0.3f;
            public int DetailNoiseOctaves = 2;
            public float DetailNoiseStrength = 0.15f;
            public bool EnableSeamlessChunks = true;
            
            public static EnhancedTerrainTuning FromDefaults(WorldConfig config)
            {
                return new EnhancedTerrainTuning
                {
                    // Cave defaults
                    CaveConnectivityThreshold = 0.3f,
                    CaveTunnelWidth = 0.15f,
                    CaveMinTunnelHeight = 2,
                    CaveLavaPoolChance = Mathf.Max(0.05f, config.Caves.Threshold * 0.2f),
                    CaveWaterPoolChance = Mathf.Max(0.08f, config.Caves.Threshold * 0.3f),
                    EnableMultiLevelCaves = true,
                    
                    // River defaults
                    RiverMeanderStrength = 0.7f,
                    RiverWidthVariation = 0.4f,
                    RiverMinWidth = Mathf.Max(2, Mathf.FloorToInt(config.Water.RiverDepth * 0.3f)),
                    RiverMaxWidth = Mathf.Max(6, Mathf.FloorToInt(config.Water.RiverDepth * 0.8f)),
                    RiverDeltaFertility = 0.6f,
                    EnableRiverTributaries = true,
                    
                    // Lake defaults
                    LakeDepthVariation = 0.5f,
                    LakeShoreComplexity = 0.3f,
                    LakeMinDepth = Mathf.Max(3, config.Lakes.MinDepth),
                    LakeMaxDepth = Mathf.Max(8, config.Lakes.MaxDepth),
                    LakeIslandChance = 0.05f,
                    EnableLakeOutflow = true,
                    
                    // General defaults
                    BiomeTransitionSmoothing = 0.3f,
                    DetailNoiseOctaves = 2,
                    DetailNoiseStrength = 0.15f,
                    EnableSeamlessChunks = true
                };
            }
        }
        
        [Serializable]
        private class WorldConfigRaw
        {
            public CaveRaw Caves = new CaveRaw();
            public WaterRaw Water = new WaterRaw();
            public LakeRaw Lakes = new LakeRaw();
            public TerrainRaw Terrain = new TerrainRaw();
        }
        
        [Serializable]
        private class CaveRaw
        {
            public float ConnectivityThreshold = 0.3f;
            public float TunnelWidth = 0.15f;
            public int MinTunnelHeight = 2;
            public float LavaPoolChance = 0.08f;
            public float WaterPoolChance = 0.12f;
            public bool EnableMultiLevel = true;
        }
        
        [Serializable]
        private class WaterRaw
        {
            public float MeanderStrength = 0.7f;
            public float WidthVariation = 0.4f;
            public int MinWidth = 2;
            public int MaxWidth = 8;
            public float DeltaFertility = 0.6f;
            public bool EnableTributaries = true;
        }
        
        [Serializable]
        private class LakeRaw
        {
            public float DepthVariation = 0.5f;
            public float ShoreComplexity = 0.3f;
            public int MinDepth = 3;
            public int MaxDepth = 12;
            public float IslandChance = 0.05f;
            public bool EnableOutflow = true;
        }
        
        [Serializable]
        private class TerrainRaw
        {
            public float BiomeTransitionSmoothing = 0.3f;
            public int DetailNoiseOctaves = 2;
            public float DetailNoiseStrength = 0.15f;
            public bool EnableSeamlessChunks = true;
        }
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            _tuning = LoadEnhancedTerrainTuning();
            
            InitializeNoiseGenerators();
        }
        
        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed;
            
            // Primary terrain noise
            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(_worldConfig.Terrain.Octaves);
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            
            // Enhanced cave noise with multiple octaves for complexity
            _caveNoise = new FastNoise(seed + 1);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency * 0.8f); // Slightly lower frequency for better caves
            _caveNoise.SetFractalOctaves(3); // More octaves for cave complexity
            _caveNoise.SetFractalGain(0.5f);
            _caveNoise.SetFractalLacunarity(2.0f);
            
            // Enhanced river noise
            _riverNoise = new FastNoise(seed + 2);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _riverNoise.SetFrequency(0.002f); // Lower frequency for longer rivers
            _riverNoise.SetFractalOctaves(2);
            _riverNoise.SetFractalGain(0.6f);
            _riverNoise.SetFractalLacunarity(2.5f);
            
            // Enhanced lake noise
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _lakeNoise.SetFrequency(0.0015f); // Lower frequency for larger lakes
            _lakeNoise.SetFractalOctaves(2);
            _lakeNoise.SetFractalGain(0.7f);
            _lakeNoise.SetFractalLacunarity(2.2f);
            
            // Biome noise
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Detail noise for surface variation
            _detailNoise = new FastNoise(seed + 6);
            _detailNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _detailNoise.SetFrequency(_worldConfig.Terrain.NoiseScale * 4.0f);
            _detailNoise.SetFractalOctaves(_tuning.DetailNoiseOctaves);
            _detailNoise.SetFractalGain(0.3f);
            _detailNoise.SetFractalLacunarity(2.0f);
            
            // Ore noise
            _oreNoise = new FastNoise(seed + 7);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
        }
        
        private EnhancedTerrainTuning LoadEnhancedTerrainTuning()
        {
            var tuning = EnhancedTerrainTuning.FromDefaults(_worldConfig);
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "enhanced-terrain-config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var raw = JsonUtility.FromJson<WorldConfigRaw>(json);
                    if (raw != null)
                    {
                        // Cave settings
                        tuning.CaveConnectivityThreshold = Mathf.Clamp01(raw.Caves.ConnectivityThreshold);
                        tuning.CaveTunnelWidth = Mathf.Clamp01(raw.Caves.TunnelWidth);
                        tuning.CaveMinTunnelHeight = Math.Max(1, raw.Caves.MinTunnelHeight);
                        tuning.CaveLavaPoolChance = Mathf.Clamp01(raw.Caves.LavaPoolChance);
                        tuning.CaveWaterPoolChance = Mathf.Clamp01(raw.Caves.WaterPoolChance);
                        tuning.EnableMultiLevelCaves = raw.Caves.EnableMultiLevel;
                        
                        // River settings
                        tuning.RiverMeanderStrength = Mathf.Clamp01(raw.Water.MeanderStrength);
                        tuning.RiverWidthVariation = Mathf.Clamp01(raw.Water.WidthVariation);
                        tuning.RiverMinWidth = Math.Max(1, raw.Water.MinWidth);
                        tuning.RiverMaxWidth = Math.Max(tuning.RiverMinWidth + 1, raw.Water.MaxWidth);
                        tuning.RiverDeltaFertility = Mathf.Clamp01(raw.Water.DeltaFertility);
                        tuning.EnableRiverTributaries = raw.Water.EnableTributaries;
                        
                        // Lake settings
                        tuning.LakeDepthVariation = Mathf.Clamp01(raw.Lakes.DepthVariation);
                        tuning.LakeShoreComplexity = Mathf.Clamp01(raw.Lakes.ShoreComplexity);
                        tuning.LakeMinDepth = Math.Max(2, raw.Lakes.MinDepth);
                        tuning.LakeMaxDepth = Math.Max(tuning.LakeMinDepth + 1, raw.Lakes.MaxDepth);
                        tuning.LakeIslandChance = Mathf.Clamp01(raw.Lakes.IslandChance);
                        tuning.EnableLakeOutflow = raw.Lakes.EnableOutflow;
                        
                        // Terrain settings
                        tuning.BiomeTransitionSmoothing = Mathf.Clamp01(raw.Terrain.BiomeTransitionSmoothing);
                        tuning.DetailNoiseOctaves = Math.Max(1, raw.Terrain.DetailNoiseOctaves);
                        tuning.DetailNoiseStrength = Mathf.Clamp01(raw.Terrain.DetailNoiseStrength);
                        tuning.EnableSeamlessChunks = raw.Terrain.EnableSeamlessChunks;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EnhancedTerrainGenerator] Failed to load enhanced-terrain-config.json: {ex.Message}");
            }

            return tuning;
        }
        
        /// <summary>
        /// Generate enhanced terrain for a chunk
        /// </summary>
        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            var blocks = new int[chunkSize, worldHeight, chunkSize];
            
            // Generate enhanced height and biome maps
            GenerateEnhancedHeightMap(chunkX, chunkZ);
            GenerateEnhancedBiomeMap(chunkX, chunkZ);
            
            // Apply seamless chunk boundaries if enabled
            if (_tuning.EnableSeamlessChunks)
            {
                ApplySeamlessChunkBoundaries(chunkX, chunkZ);
            }
            
            // Generate terrain features with enhanced algorithms
            GenerateEnhancedTerrain(blocks, chunkX, chunkZ);
            
            if (_worldConfig.Caves.EnableCaves)
            {
                GenerateEnhancedCaves(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableRivers)
            {
                GenerateEnhancedRivers(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableLakes)
            {
                GenerateEnhancedLakes(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Ores.EnableOreGeneration)
            {
                GenerateEnhancedOres(blocks, chunkX, chunkZ);
            }
            
            return blocks;
        }
        
        private void GenerateEnhancedHeightMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Terrain.NoiseScale;
                    float worldZ = (chunkZ * chunkSize + z) * _worldConfig.Terrain.NoiseScale;
                    
                    // Base terrain height with enhanced detail
                    float height = _terrainNoise.GetNoise(worldX, worldZ);
                    height = (height + 1f) * 0.5f; // Normalize to 0-1
                    
                    // Add detail noise for surface variation
                    float detail = _detailNoise.GetNoise(worldX * 2.0f, worldZ * 2.0f);
                    detail = (detail + 1f) * 0.5f;
                    height += detail * _tuning.DetailNoiseStrength;
                    
                    // Apply biome-specific modifications
                    int biome = _biomeMapCache[x, z];
                    height = ApplyBiomeHeightModifier(height, biome);
                    
                    _heightMapCache[x, z] = height;
                }
            }
        }
        
        private void GenerateEnhancedBiomeMap(int chunkX, int chunkZ)
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
                    
                    // Apply smoothing for biome transitions
                    if (_tuning.BiomeTransitionSmoothing > 0 && x > 0 && z > 0 && x < chunkSize - 1 && z < chunkSize - 1)
                    {
                        float neighborSum = noise;
                        int count = 1;
                        
                        // Sample neighbors for smoothing
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue; // Skip center
                                
                                int nx = x + dx;
                                int nz = z + dz;
                                
                                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                                {
                                    neighborSum += _biomeNoise.GetNoise(
                                        (chunkX * chunkSize + nx) * _worldConfig.Terrain.BiomeScale,
                                        (chunkZ * chunkSize + nz) * _worldConfig.Terrain.BiomeScale);
                                    count++;
                                }
                            }
                        }
                        
                        noise = Mathf.Lerp(noise, (neighborSum / count), _tuning.BiomeTransitionSmoothing);
                    }
                    
                    // Determine biome based on smoothed noise value
                    int biome = DetermineBiome(noise);
                    _biomeMapCache[x, z] = biome;
                }
            }
        }
        
        private void ApplySeamlessChunkBoundaries(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            // Sample from neighboring chunks to create seamless boundaries
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Get boundary weights based on distance to chunk edges
                    float edgeWeightX = CalculateEdgeWeight(x, chunkSize);
                    float edgeWeightZ = CalculateEdgeWeight(z, chunkSize);
                    
                    if (edgeWeightX > 0 || edgeWeightZ > 0)
                    {
                        // Sample from neighboring chunks
                        float neighborHeight = SampleNeighboringChunks(chunkX, chunkZ, x, z);
                        _heightMapCache[x, z] = Mathf.Lerp(_heightMapCache[x, z], neighborHeight, 
                            Mathf.Max(edgeWeightX, edgeWeightZ));
                    }
                }
            }
        }
        
        private float CalculateEdgeWeight(int coord, int chunkSize)
        {
            // Calculate distance to nearest edge (0 or chunkSize-1)
            int distanceToEdge = Mathf.Min(coord, chunkSize - 1 - coord);
            
            // Apply smooth falloff from edge
            if (distanceToEdge <= 2)
                return 0f; // No blending very close to edge
            else if (distanceToEdge <= 3)
                return 0.3f * (3 - distanceToEdge + 1); // Gradual increase
            else
                return 1f; // Full influence in interior
        }
        
        private float SampleNeighboringChunks(int chunkX, int chunkZ, int localX, int localZ)
        {
            // This would need access to neighboring chunk data
            // For now, return the current height as a fallback
            // In a full implementation, this would sample from adjacent chunks
            return _heightMapCache[localX, localZ];
        }
        
        private float ApplyBiomeHeightModifier(float baseHeight, int biome)
        {
            return biome switch
            {
                0 => baseHeight * 0.8f, // Plains - lower terrain
                1 => baseHeight * 1.4f, // Mountains - higher terrain
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
                < 0.15f => 3, // Desert
                < 0.35f => 0, // Plains
                < 0.55f => 2, // Forest
                < 0.75f => 4, // Hills
                _ => 1       // Mountains
            };
        }
        
        private void GenerateEnhancedTerrain(int[,,] blocks, int chunkX, int chunkZ)
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
                    
                    // Generate terrain layers with enhanced surface detail
                    for (int y = 0; y < terrainHeight; y++)
                    {
                        int blockId = GetBlockForTerrainLayer(y, terrainHeight, seaLevel, biome);
                        blocks[x, y, z] = blockId;
                    }
                    
                    // Add water below sea level with enhanced variation
                    for (int y = terrainHeight; y < seaLevel; y++)
                    {
                        // Add underwater terrain variation
                        float depth = seaLevel - y;
                        float depthVariation = Mathf.PerlinNoise(
                            (chunkX * chunkSize + x) * 0.1f,
                            (chunkZ * chunkSize + z) * 0.1f,
                            depth * 0.2f);
                        
                        if (depthVariation > 0.3f && depth > 2)
                        {
                            blocks[x, y, z] = GetBlockId("sand"); // Underwater sand variations
                        }
                        else if (depthVariation > -0.2f && depth > 4)
                        {
                            blocks[x, y, z] = GetBlockId("gravel"); // Underwater gravel
                        }
                        else
                        {
                            blocks[x, y, z] = GetBlockId("water");
                        }
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
            // Surface layers with enhanced detail
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
            
            // Sub-surface layers with biome-specific materials
            if (y >= terrainHeight - 4)
            {
                return biome switch
                {
                    0 => GetBlockId("dirt"), // Plains
                    1 => GetBlockId("stone"), // Mountains
                    2 => GetBlockId("dirt"), // Forest
                    3 => GetBlockId("sandstone"), // Desert
                    4 => GetBlockId("dirt"), // Hills
                    _ => GetBlockId("dirt")
                };
            }
            
            // Underground
            return GetBlockId("stone");
        }
        
        private void GenerateEnhancedCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Caves.EnableCaves) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate enhanced cave density map with multi-level support
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Caves.HorizontalFrequency;
                    float worldY = y * _worldConfig.Caves.VerticalFrequency;
                    
                    // Multi-octave cave noise for complexity
                    float caveValue = _caveNoise.GetNoise(worldX, worldY);
                    
                    // Apply connectivity enhancement
                    if (_tuning.EnableMultiLevelCaves && y > _worldConfig.Caves.MinCaveHeight)
                    {
                        // Check for connectivity with levels above/below
                        float connectivityBonus = CalculateCaveConnectivity(x, y, z, chunkX, chunkZ);
                        caveValue += connectivityBonus * _tuning.CaveConnectivityThreshold;
                    }
                    
                    _caveMapCache[x, y] = caveValue;
                }
            }
            
            // Apply enhanced cave generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = _worldConfig.Caves.MinCaveHeight; y < _worldConfig.Caves.MaxCaveHeight; y++)
                    {
                        float caveValue = _caveMapCache[x, y];
                        
                        // Enhanced cave threshold with tunnel width consideration
                        float threshold = _worldConfig.Caves.Threshold;
                        
                        // Create wider tunnels in certain areas
                        if (UnityEngine.Random.value < _tuning.CaveTunnelWidth)
                        {
                            threshold *= 0.7f; // Wider tunnels
                        }
                        
                        if (caveValue > threshold)
                        {
                            blocks[x, y, z] = 0; // Air
                            
                            // Enhanced liquid placement
                            if (y < 10 && UnityEngine.Random.value < _tuning.CaveLavaPoolChance)
                            {
                                // Create lava pools with better distribution
                                CreateLavaPool(blocks, x, y, z, chunkX, chunkZ);
                            }
                            else if (y < _worldConfig.Terrain.SeaLevel - 5 && UnityEngine.Random.value < _tuning.CaveWaterPoolChance)
                            {
                                // Create water pools at cave entrances
                                CreateWaterPool(blocks, x, y, z, chunkX, chunkZ);
                            }
                        }
                    }
                }
            }
            
            // Add cave support structures
            AddCaveSupports(blocks, chunkX, chunkZ);
        }
        
        private float CalculateCaveConnectivity(int x, int y, int z, int chunkX, int chunkZ)
        {
            // Calculate connectivity bonus based on neighboring cave spaces
            float connectivity = 0f;
            int sampleRadius = 2;
            
            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue; // Skip center
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                            ny >= 0 && ny < _worldConfig.WorldHeight && 
                            nz >= 0 && nz < _worldConfig.ChunkSize)
                        {
                            float neighborValue = _caveMapCache[nx, ny];
                            if (neighborValue > _worldConfig.Caves.Threshold)
                            {
                                connectivity += 0.2f;
                            }
                        }
                    }
                }
            }
            
            return Mathf.Clamp01(connectivity / 8f); // Normalize by maximum possible connections
        }
        
        private void CreateLavaPool(int[,,] blocks, int x, int y, int z, int chunkX, int chunkZ)
        {
            // Create small lava pools with better distribution
            int poolRadius = UnityEngine.Random.Range(1, 3);
            
            for (int dx = -poolRadius; dx <= poolRadius; dx++)
            {
                for (int dz = -poolRadius; dz <= poolRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize &&
                        blocks[nx, y, nz] == 0) // Only place in existing caves
                    {
                        blocks[nx, y, nz] = GetBlockId("lava");
                    }
                }
            }
        }
        
        private void CreateWaterPool(int[,,] blocks, int x, int y, int z, int chunkX, int chunkZ)
        {
            // Create water pools at cave entrances
            int poolRadius = UnityEngine.Random.Range(1, 2);
            
            for (int dx = -poolRadius; dx <= poolRadius; dx++)
            {
                for (int dz = -poolRadius; dz <= poolRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        blocks[nx, y, nz] = GetBlockId("water");
                    }
                }
            }
        }
        
        private void AddCaveSupports(int[,,] blocks, int chunkX, int chunkZ)
        {
            // Add support pillars and structures in caves for better stability
            if (UnityEngine.Random.value < 0.1f) // 10% chance per chunk
            {
                int supportCount = UnityEngine.Random.Range(1, 4);
                
                for (int i = 0; i < supportCount; i++)
                {
                    int x = UnityEngine.Random.Range(2, _worldConfig.ChunkSize - 3);
                    int z = UnityEngine.Random.Range(2, _worldConfig.ChunkSize - 3);
                    int y = UnityEngine.Random.Range(_worldConfig.Caves.MinCaveHeight + 2, _worldConfig.Caves.MaxCaveHeight - 2);
                    
                    // Find cave ceiling
                    int ceilingY = y;
                    while (ceilingY < _worldConfig.WorldHeight - 1 && blocks[x, ceilingY + 1, z] == 0)
                    {
                        ceilingY++;
                    }
                    
                    // Place support pillar
                    for (int py = y; py <= ceilingY; py++)
                    {
                        if (blocks[x, py, z] == 0)
                        {
                            blocks[x, py, z] = GetBlockId("stone");
                        }
                    }
                }
            }
        }
        
        private void GenerateEnhancedRivers(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableRivers) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate enhanced river map with meandering
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    // Multi-octave river noise for natural meandering
                    float riverValue = _riverNoise.GetNoise(worldX, worldZ);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    
                    // Apply meandering enhancement
                    float meander = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 0.5f;
                    riverValue = riverValue * (1f + meander * _tuning.RiverMeanderStrength);
                    
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
            // Apply enhanced river generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float riverValue = _riverMapCache[x, z];
                    
                    if (riverValue < _worldConfig.Water.RiverCenterThreshold)
                    {
                        // Enhanced river carving with variable width
                        int riverWidth = CalculateRiverWidth(riverValue);
                        
                        // Carve river channel
                        for (int y = seaLevel; y < worldHeight; y++)
                        {
                            bool inRiverChannel = IsInRiverChannel(x, z, y, riverWidth);
                            
                            if (inRiverChannel)
                            {
                                if (blocks[x, y, z] != 0) // If not already air
                                {
                                    blocks[x, y, z] = 0; // Air
                                }
                            }
                            else if (y < seaLevel + _tuning.RiverMinWidth)
                            {
                                // Add river water
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                        
                        // Create river banks with enhanced materials
                        CreateRiverBanks(blocks, x, z, y, riverValue, riverWidth, seaLevel);
                    }
                }
            }
            
            // Add river tributaries if enabled
            if (_tuning.EnableRiverTributaries)
            {
                AddRiverTributaries(blocks, chunkX, chunkZ);
            }
        }
        
        private int CalculateRiverWidth(float riverValue)
        {
            // Calculate variable river width based on river strength
            float normalizedStrength = 1f - Mathf.Clamp01(riverValue / _worldConfig.Water.RiverCenterThreshold);
            int baseWidth = Mathf.RoundToInt(_tuning.RiverMinWidth + 
                (_tuning.RiverMaxWidth - _tuning.RiverMinWidth) * normalizedStrength);
            
            // Add variation
            float variation = Mathf.PerlinNoise(
                (chunkX * _worldConfig.ChunkSize + x) * 0.1f,
                (chunkZ * _worldConfig.ChunkSize + z) * 0.1f) * 0.5f;
            
            return Mathf.RoundToInt(baseWidth * (1f + variation * _tuning.RiverWidthVariation));
        }
        
        private bool IsInRiverChannel(int x, int z, int y, int riverWidth)
        {
            // Check if position is within river channel
            float centerDistance = Mathf.Sqrt(
                Mathf.Pow(x - _worldConfig.ChunkSize * 0.5f, 2) + 
                Mathf.Pow(z - _worldConfig.ChunkSize * 0.5f, 2));
            
            // Wider channels at center, narrower at edges
            float widthAtDistance = riverWidth * (1f - centerDistance / (_worldConfig.ChunkSize * 0.5f));
            float verticalProfile = Mathf.Sin((y - _worldConfig.Terrain.SeaLevel) * Mathf.PI / riverWidth);
            
            return centerDistance < widthAtDistance && verticalProfile > 0.3f;
        }
        
        private void CreateRiverBanks(int[,,] blocks, int x, int z, int y, float riverValue, int riverWidth, int seaLevel)
        {
            // Enhanced river bank materials with erosion patterns
            float erosionFactor = Mathf.Clamp01(riverValue / _worldConfig.Water.RiverCenterThreshold);
            
            if (UnityEngine.Random.value < 0.7f)
            {
                // Main bank material
                blocks[x, y, z] = GetBlockId("sand");
            }
            else if (erosionFactor > 0.5f)
            {
                // Eroded material
                blocks[x, y, z] = GetBlockId("gravel");
            }
            else
            {
                // Natural bank material
                blocks[x, y, z] = GetBlockId("dirt");
            }
        }
        
        private void AddRiverTributaries(int[,,] blocks, int chunkX, int chunkZ)
        {
            // Add smaller tributary streams
            int tributaryCount = UnityEngine.Random.Range(0, 2);
            
            for (int i = 0; i < tributaryCount; i++)
            {
                int startX = UnityEngine.Random.Range(4, _worldConfig.ChunkSize - 5);
                int startZ = UnityEngine.Random.Range(4, _worldConfig.ChunkSize - 5);
                int length = UnityEngine.Random.Range(3, 8);
                int width = UnityEngine.Random.Range(1, 3);
                
                // Create tributary path
                for (int j = 0; j < length; j++)
                {
                    int x = startX + j;
                    int z = startZ + j;
                    
                    if (x >= 2 && x < _worldConfig.ChunkSize - 2 && 
                        z >= 2 && z < _worldConfig.ChunkSize - 2)
                    {
                        // Small stream channel
                        for (int w = -width; w <= width; w++)
                        {
                            for (int y = _worldConfig.Terrain.SeaLevel - 1; y < _worldConfig.Terrain.SeaLevel + 2; y++)
                            {
                                if (blocks[x + w, y, z + w] != 0)
                                {
                                    blocks[x + w, y, z + w] = 0;
                                }
                                else if (y == _worldConfig.Terrain.SeaLevel)
                                {
                                    blocks[x + w, y, z + w] = GetBlockId("water");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void GenerateEnhancedLakes(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableLakes) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate enhanced lake map with depth variation
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
            
            // Apply enhanced lake generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.65f) // Lake threshold
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
                        
                        // Enhanced lake depth calculation
                        int baseDepth = Mathf.RoundToInt(UnityEngine.Random.Range(
                            _tuning.LakeMinDepth, _tuning.LakeMaxDepth));
                        
                        // Apply depth variation
                        float depthVariation = Mathf.PerlinNoise(
                            worldX * 0.05f, worldZ * 0.05f) * 0.5f;
                        int lakeDepth = Mathf.RoundToInt(baseDepth * (1f + depthVariation * _tuning.LakeDepthVariation));
                        int lakeBottom = Mathf.Max(terrainHeight - lakeDepth, 1);
                        
                        // Create lake basin with enhanced shaping
                        for (int y = lakeBottom; y <= terrainHeight; y++)
                        {
                            if (blocks[x, y, z] != 0)
                            {
                                blocks[x, y, z] = 0; // Remove terrain
                            }
                        }
                        
                        // Fill with water
                        int waterLevel = Mathf.Min(lakeBottom + lakeDepth / 2, seaLevel);
                        for (int y = lakeBottom; y < waterLevel; y++)
                        {
                            if (y >= 0 && y < worldHeight)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                        
                        // Create enhanced shoreline with complexity
                        CreateEnhancedShoreline(blocks, x, z, lakeBottom, waterLevel, lakeValue, seaLevel);
                        
                        // Add lake outflow if enabled
                        if (_tuning.EnableLakeOutflow)
                        {
                            CreateLakeOutflow(blocks, x, z, waterLevel, seaLevel);
                        }
                        
                        // Add islands if enabled
                        if (UnityEngine.Random.value < _tuning.LakeIslandChance)
                        {
                            CreateLakeIsland(blocks, x, z, waterLevel, seaLevel);
                        }
                    }
                }
            }
        }
        
        private void CreateEnhancedShoreline(int[,,] blocks, int x, int z, int lakeBottom, int waterLevel, float lakeValue, int seaLevel)
        {
            // Create complex shoreline with multiple materials
            float shoreComplexity = _tuning.LakeShoreComplexity;
            
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        
                        if (distance <= 2)
                        {
                            // Close to shore - complex materials
                            float noise = UnityEngine.Random.value;
                            
                            if (noise < 0.3f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("sand");
                            }
                            else if (noise < 0.6f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("gravel");
                            }
                            else if (noise < 0.8f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("clay");
                            }
                            else
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("dirt");
                            }
                        }
                    }
                }
            }
        }
        
        private void CreateLakeOutflow(int[,,] blocks, int x, int z, int waterLevel, int seaLevel)
        {
            // Create natural lake outflow streams
            var downhill = FindDownhillDirection(x, z);
            
            if (downhill != Vector2Int.zero)
            {
                int outflowLength = UnityEngine.Random.Range(3, 8);
                int outflowWidth = UnityEngine.Random.Range(1, 2);
                
                for (int i = 1; i <= outflowLength; i++)
                {
                    int nx = x + downhill.x * i;
                    int nz = z + downhill.y * i;
                    
                    if (nx >= 1 && nx < _worldConfig.ChunkSize - 1 && 
                        nz >= 1 && nz < _worldConfig.ChunkSize - 1)
                    {
                        // Create outflow channel
                        for (int w = -outflowWidth; w <= outflowWidth; w++)
                        {
                            for (int y = waterLevel; y >= waterLevel - 2; y--)
                            {
                                if (blocks[nx + w, y, nz + w] != 0)
                                {
                                    blocks[nx + w, y, nz + w] = 0;
                                }
                            }
                            
                            if (y == waterLevel)
                            {
                                blocks[nx + w, y, nz + w] = GetBlockId("water");
                            }
                        }
                    }
                }
            }
        }
        
        private void CreateLakeIsland(int[,,] blocks, int x, int z, int waterLevel, int seaLevel)
        {
            // Create small islands in lakes
            int islandSize = UnityEngine.Random.Range(2, 4);
            int islandHeight = UnityEngine.Random.Range(waterLevel + 1, waterLevel + 3);
            
            for (int dx = -islandSize; dx <= islandSize; dx++)
            {
                for (int dz = -islandSize; dz <= islandSize; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        // Build island up from lake bottom
                        for (int y = waterLevel - 2; y <= islandHeight; y++)
                        {
                            if (blocks[nx, y, nz] == 0)
                            {
                                blocks[nx, y, nz] = GetBlockId("dirt");
                            }
                        }
                        
                        // Top with grass
                        blocks[nx, islandHeight, nz] = GetBlockId("grass");
                    }
                }
            }
        }
        
        private Vector2Int FindDownhillDirection(int x, int z)
        {
            // Find the direction of steepest descent
            int surfaceHeight = FindSurfaceHeight(x, z);
            Vector2Int bestDirection = Vector2Int.zero;
            int bestDrop = 0;
            
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            
            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int nz = z + dir.y;
                
                if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                    nz >= 0 && nz < _worldConfig.ChunkSize)
                {
                    int neighborHeight = FindSurfaceHeight(nx, nz);
                    int drop = surfaceHeight - neighborHeight;
                    
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDirection = dir;
                    }
                }
            }
            
            return bestDirection;
        }
        
        private int FindSurfaceHeight(int x, int z)
        {
            // Find the first non-air block from top down
            for (int y = _worldConfig.WorldHeight - 1; y >= 0; y--)
            {
                if (_heightMapCache[x, z] > 0.1f) // Approximate surface from heightmap
                {
                    return Mathf.RoundToInt(_heightMapCache[x, z] * _worldConfig.Terrain.MountainMaxHeight);
                }
            }
            return 0;
        }
        
        private void GenerateEnhancedOres(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Ores.EnableOreGeneration) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate enhanced ore veins with better distribution
            foreach (var oreEntry in _worldConfig.Ores.Ores)
            {
                string oreName = oreEntry.Key;
                var oreConfig = oreEntry.Value;
                
                if (oreConfig == null) continue;
                
                int veinsPerChunk = oreConfig.VeinsPerChunk;
                
                for (int i = 0; i < veinsPerChunk; i++)
                {
                    // Enhanced vein positioning with depth and biome consideration
                    int veinX = UnityEngine.Random.Range(0, chunkSize);
                    int biome = _biomeMapCache[veinX, UnityEngine.Random.Range(0, chunkSize)];
                    int minHeight = GetBiomeSpecificMinHeight(oreName, biome, oreConfig);
                    int maxY = GetBiomeSpecificMaxHeight(oreName, biome, oreConfig);
                    int veinY = UnityEngine.Random.Range(minHeight, maxY);
                    int veinZ = UnityEngine.Random.Range(0, chunkSize);
                    
                    // Generate enhanced ore vein
                    GenerateEnhancedOreVein(blocks, veinX, veinY, veinZ, oreName, oreConfig);
                }
            }
        }
        
        private int GetBiomeSpecificMinHeight(string oreName, int biome, OreConfig config)
        {
            // Different ores spawn at different depths based on biome
            return (oreName, biome) switch
            {
                ("coal", 0) => Mathf.Max(config.MinHeight, 5), // Coal in mountains/plains
                ("coal", 1) => Mathf.Max(config.MinHeight, 8), // Coal in forests
                ("coal", 3) => Mathf.Max(config.MinHeight, 2), // Coal in deserts
                ("iron", _) => Mathf.Max(config.MinHeight, 5), // Iron everywhere
                ("gold", 1) => Mathf.Max(config.MinHeight, 20), // Gold in mountains
                ("diamond", 1) => Mathf.Max(config.MinHeight, 12), // Diamond deep underground
                _ => config.MinHeight
            };
        }
        
        private int GetBiomeSpecificMaxHeight(string oreName, int biome, OreConfig config)
        {
            return (oreName, biome) switch
            {
                ("diamond", 1) => Mathf.Min(config.MaxHeight, 20), // Diamonds limited in mountains
                ("gold", 1) => Mathf.Min(config.MaxHeight, 35), // Gold in mountains
                _ => config.MaxHeight
            };
        }
        
        private void GenerateEnhancedOreVein(int[,,] blocks, int startX, int startY, int startZ, string oreName, OreConfig config)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            // Enhanced vein generation with better shapes
            int veinSize = config.VeinSize;
            Vector3Int currentPos = new Vector3Int(startX, startY, startZ);
            
            for (int i = 0; i < veinSize; i++)
            {
                // Random walk with directional bias
                Vector3Int direction = GetRandomWalkDirection();
                currentPos += direction;
                
                // Add some randomness to path
                if (UnityEngine.Random.value < 0.3f)
                {
                    direction = GetRandomWalkDirection();
                }
                
                // Check bounds and place ore
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    // Only replace certain stone types
                    int currentBlock = blocks[currentPos.x, currentPos.y, currentPos.z];
                    if (currentBlock == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                    else if (currentBlock == GetBlockId("dirt") && UnityEngine.Random.value < 0.1f)
                    {
                        // Small chance to replace dirt with ore
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
            }
        }
        
        private Vector3Int GetRandomWalkDirection()
        {
            // Random walk direction with 3D bias
            int choice = UnityEngine.Random.Range(0, 26);
            
            return choice switch
            {
                < 8 => new Vector3Int(1, 0, 0),   // East
                < 16 => new Vector3Int(-1, 0, 0),  // West
                < 2 => new Vector3Int(0, 1, 0),    // South
                < 10 => new Vector3Int(0, -1, 0),  // North
                < 20 => new Vector3Int(0, 0, 1),    // Up
                < 24 => new Vector3Int(0, 0, -1),  // Down
                _ => new Vector3Int(0, 0, 0)     // Stay
            };
        }
        
        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
    
    /// <summary>
    /// Enhanced FastNoise implementation with additional features
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
            // Enhanced simplex noise implementation
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
            // Enhanced white noise with better distribution
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
}using System.IO;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced terrain generation system with improved algorithms for caves, rivers, and lakes
    /// Addresses issues with connectivity, natural formations, and seamless chunk boundaries
    /// </summary>
    public class EnhancedTerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        // Noise generators with improved parameters
        private FastNoise _terrainNoise;
        private FastNoise _caveNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _biomeNoise;
        private FastNoise _oreNoise;
        private FastNoise _detailNoise;
        
        // Cache for performance
        private readonly float[,] _heightMapCache = new float[16, 16];
        private readonly int[,] _biomeMapCache = new int[16, 16];
        private readonly float[,] _caveMapCache = new float[16, 256];
        private readonly float[,] _riverMapCache = new float[16, 16];
        private readonly float[,] _lakeMapCache = new float[16, 16];
        private readonly float[,] _detailMapCache = new float[16, 16];
        
        // Enhanced tuning parameters
        private EnhancedTerrainTuning _tuning;
        
        [Serializable]
        private class EnhancedTerrainTuning
        {
            // Cave improvements
            public float CaveConnectivityThreshold = 0.3f;
            public float CaveTunnelWidth = 0.15f;
            public int CaveMinTunnelHeight = 2;
            public float CaveLavaPoolChance = 0.08f;
            public float CaveWaterPoolChance = 0.12f;
            public bool EnableMultiLevelCaves = true;
            
            // River improvements
            public float RiverMeanderStrength = 0.7f;
            public float RiverWidthVariation = 0.4f;
            public int RiverMinWidth = 2;
            public int RiverMaxWidth = 8;
            public float RiverDeltaFertility = 0.6f;
            public bool EnableRiverTributaries = true;
            
            // Lake improvements
            public float LakeDepthVariation = 0.5f;
            public float LakeShoreComplexity = 0.3f;
            public int LakeMinDepth = 3;
            public int LakeMaxDepth = 12;
            public float LakeIslandChance = 0.05f;
            public bool EnableLakeOutflow = true;
            
            // General improvements
            public float BiomeTransitionSmoothing = 0.3f;
            public int DetailNoiseOctaves = 2;
            public float DetailNoiseStrength = 0.15f;
            public bool EnableSeamlessChunks = true;
            
            public static EnhancedTerrainTuning FromDefaults(WorldConfig config)
            {
                return new EnhancedTerrainTuning
                {
                    // Cave defaults
                    CaveConnectivityThreshold = 0.3f,
                    CaveTunnelWidth = 0.15f,
                    CaveMinTunnelHeight = 2,
                    CaveLavaPoolChance = Mathf.Max(0.05f, config.Caves.Threshold * 0.2f),
                    CaveWaterPoolChance = Mathf.Max(0.08f, config.Caves.Threshold * 0.3f),
                    EnableMultiLevelCaves = true,
                    
                    // River defaults
                    RiverMeanderStrength = 0.7f,
                    RiverWidthVariation = 0.4f,
                    RiverMinWidth = Mathf.Max(2, Mathf.FloorToInt(config.Water.RiverDepth * 0.3f)),
                    RiverMaxWidth = Mathf.Max(6, Mathf.FloorToInt(config.Water.RiverDepth * 0.8f)),
                    RiverDeltaFertility = 0.6f,
                    EnableRiverTributaries = true,
                    
                    // Lake defaults
                    LakeDepthVariation = 0.5f,
                    LakeShoreComplexity = 0.3f,
                    LakeMinDepth = Mathf.Max(3, config.Lakes.MinDepth),
                    LakeMaxDepth = Mathf.Max(8, config.Lakes.MaxDepth),
                    LakeIslandChance = 0.05f,
                    EnableLakeOutflow = true,
                    
                    // General defaults
                    BiomeTransitionSmoothing = 0.3f,
                    DetailNoiseOctaves = 2,
                    DetailNoiseStrength = 0.15f,
                    EnableSeamlessChunks = true
                };
            }
        }
        
        [Serializable]
        private class WorldConfigRaw
        {
            public CaveRaw Caves = new CaveRaw();
            public WaterRaw Water = new WaterRaw();
            public LakeRaw Lakes = new LakeRaw();
            public TerrainRaw Terrain = new TerrainRaw();
        }
        
        [Serializable]
        private class CaveRaw
        {
            public float ConnectivityThreshold = 0.3f;
            public float TunnelWidth = 0.15f;
            public int MinTunnelHeight = 2;
            public float LavaPoolChance = 0.08f;
            public float WaterPoolChance = 0.12f;
            public bool EnableMultiLevel = true;
        }
        
        [Serializable]
        private class WaterRaw
        {
            public float MeanderStrength = 0.7f;
            public float WidthVariation = 0.4f;
            public int MinWidth = 2;
            public int MaxWidth = 8;
            public float DeltaFertility = 0.6f;
            public bool EnableTributaries = true;
        }
        
        [Serializable]
        private class LakeRaw
        {
            public float DepthVariation = 0.5f;
            public float ShoreComplexity = 0.3f;
            public int MinDepth = 3;
            public int MaxDepth = 12;
            public float IslandChance = 0.05f;
            public bool EnableOutflow = true;
        }
        
        [Serializable]
        private class TerrainRaw
        {
            public float BiomeTransitionSmoothing = 0.3f;
            public int DetailNoiseOctaves = 2;
            public float DetailNoiseStrength = 0.15f;
            public bool EnableSeamlessChunks = true;
        }
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            _tuning = LoadEnhancedTerrainTuning();
            
            InitializeNoiseGenerators();
        }
        
        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed;
            
            // Primary terrain noise
            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(_worldConfig.Terrain.Octaves);
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            
            // Enhanced cave noise with multiple octaves for complexity
            _caveNoise = new FastNoise(seed + 1);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency * 0.8f); // Slightly lower frequency for better caves
            _caveNoise.SetFractalOctaves(3); // More octaves for cave complexity
            _caveNoise.SetFractalGain(0.5f);
            _caveNoise.SetFractalLacunarity(2.0f);
            
            // Enhanced river noise
            _riverNoise = new FastNoise(seed + 2);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _riverNoise.SetFrequency(0.002f); // Lower frequency for longer rivers
            _riverNoise.SetFractalOctaves(2);
            _riverNoise.SetFractalGain(0.6f);
            _riverNoise.SetFractalLacunarity(2.5f);
            
            // Enhanced lake noise
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _lakeNoise.SetFrequency(0.0015f); // Lower frequency for larger lakes
            _lakeNoise.SetFractalOctaves(2);
            _lakeNoise.SetFractalGain(0.7f);
            _lakeNoise.SetFractalLacunarity(2.2f);
            
            // Biome noise
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Detail noise for surface variation
            _detailNoise = new FastNoise(seed + 6);
            _detailNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _detailNoise.SetFrequency(_worldConfig.Terrain.NoiseScale * 4.0f);
            _detailNoise.SetFractalOctaves(_tuning.DetailNoiseOctaves);
            _detailNoise.SetFractalGain(0.3f);
            _detailNoise.SetFractalLacunarity(2.0f);
            
            // Ore noise
            _oreNoise = new FastNoise(seed + 7);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
        }
        
        private EnhancedTerrainTuning LoadEnhancedTerrainTuning()
        {
            var tuning = EnhancedTerrainTuning.FromDefaults(_worldConfig);
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "enhanced-terrain-config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var raw = JsonUtility.FromJson<WorldConfigRaw>(json);
                    if (raw != null)
                    {
                        // Cave settings
                        tuning.CaveConnectivityThreshold = Mathf.Clamp01(raw.Caves.ConnectivityThreshold);
                        tuning.CaveTunnelWidth = Mathf.Clamp01(raw.Caves.TunnelWidth);
                        tuning.CaveMinTunnelHeight = Math.Max(1, raw.Caves.MinTunnelHeight);
                        tuning.CaveLavaPoolChance = Mathf.Clamp01(raw.Caves.LavaPoolChance);
                        tuning.CaveWaterPoolChance = Mathf.Clamp01(raw.Caves.WaterPoolChance);
                        tuning.EnableMultiLevelCaves = raw.Caves.EnableMultiLevel;
                        
                        // River settings
                        tuning.RiverMeanderStrength = Mathf.Clamp01(raw.Water.MeanderStrength);
                        tuning.RiverWidthVariation = Mathf.Clamp01(raw.Water.WidthVariation);
                        tuning.RiverMinWidth = Math.Max(1, raw.Water.MinWidth);
                        tuning.RiverMaxWidth = Math.Max(tuning.RiverMinWidth + 1, raw.Water.MaxWidth);
                        tuning.RiverDeltaFertility = Mathf.Clamp01(raw.Water.DeltaFertility);
                        tuning.EnableRiverTributaries = raw.Water.EnableTributaries;
                        
                        // Lake settings
                        tuning.LakeDepthVariation = Mathf.Clamp01(raw.Lakes.DepthVariation);
                        tuning.LakeShoreComplexity = Mathf.Clamp01(raw.Lakes.ShoreComplexity);
                        tuning.LakeMinDepth = Math.Max(2, raw.Lakes.MinDepth);
                        tuning.LakeMaxDepth = Math.Max(tuning.LakeMinDepth + 1, raw.Lakes.MaxDepth);
                        tuning.LakeIslandChance = Mathf.Clamp01(raw.Lakes.IslandChance);
                        tuning.EnableLakeOutflow = raw.Lakes.EnableOutflow;
                        
                        // Terrain settings
                        tuning.BiomeTransitionSmoothing = Mathf.Clamp01(raw.Terrain.BiomeTransitionSmoothing);
                        tuning.DetailNoiseOctaves = Math.Max(1, raw.Terrain.DetailNoiseOctaves);
                        tuning.DetailNoiseStrength = Mathf.Clamp01(raw.Terrain.DetailNoiseStrength);
                        tuning.EnableSeamlessChunks = raw.Terrain.EnableSeamlessChunks;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EnhancedTerrainGenerator] Failed to load enhanced-terrain-config.json: {ex.Message}");
            }

            return tuning;
        }
        
        /// <summary>
        /// Generate enhanced terrain for a chunk
        /// </summary>
        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            var blocks = new int[chunkSize, worldHeight, chunkSize];
            
            // Generate enhanced height and biome maps
            GenerateEnhancedHeightMap(chunkX, chunkZ);
            GenerateEnhancedBiomeMap(chunkX, chunkZ);
            
            // Apply seamless chunk boundaries if enabled
            if (_tuning.EnableSeamlessChunks)
            {
                ApplySeamlessChunkBoundaries(chunkX, chunkZ);
            }
            
            // Generate terrain features with enhanced algorithms
            GenerateEnhancedTerrain(blocks, chunkX, chunkZ);
            
            if (_worldConfig.Caves.EnableCaves)
            {
                GenerateEnhancedCaves(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableRivers)
            {
                GenerateEnhancedRivers(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Water.EnableLakes)
            {
                GenerateEnhancedLakes(blocks, chunkX, chunkZ);
            }
            
            if (_worldConfig.Ores.EnableOreGeneration)
            {
                GenerateEnhancedOres(blocks, chunkX, chunkZ);
            }
            
            return blocks;
        }
        
        private void GenerateEnhancedHeightMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Terrain.NoiseScale;
                    float worldZ = (chunkZ * chunkSize + z) * _worldConfig.Terrain.NoiseScale;
                    
                    // Base terrain height with enhanced detail
                    float height = _terrainNoise.GetNoise(worldX, worldZ);
                    height = (height + 1f) * 0.5f; // Normalize to 0-1
                    
                    // Add detail noise for surface variation
                    float detail = _detailNoise.GetNoise(worldX * 2.0f, worldZ * 2.0f);
                    detail = (detail + 1f) * 0.5f;
                    height += detail * _tuning.DetailNoiseStrength;
                    
                    // Apply biome-specific modifications
                    int biome = _biomeMapCache[x, z];
                    height = ApplyBiomeHeightModifier(height, biome);
                    
                    _heightMapCache[x, z] = height;
                }
            }
        }
        
        private void GenerateEnhancedBiomeMap(int chunkX, int chunkZ)
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
                    
                    // Apply smoothing for biome transitions
                    if (_tuning.BiomeTransitionSmoothing > 0 && x > 0 && z > 0 && x < chunkSize - 1 && z < chunkSize - 1)
                    {
                        float neighborSum = noise;
                        int count = 1;
                        
                        // Sample neighbors for smoothing
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue; // Skip center
                                
                                int nx = x + dx;
                                int nz = z + dz;
                                
                                if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                                {
                                    neighborSum += _biomeNoise.GetNoise(
                                        (chunkX * chunkSize + nx) * _worldConfig.Terrain.BiomeScale,
                                        (chunkZ * chunkSize + nz) * _worldConfig.Terrain.BiomeScale);
                                    count++;
                                }
                            }
                        }
                        
                        noise = Mathf.Lerp(noise, (neighborSum / count), _tuning.BiomeTransitionSmoothing);
                    }
                    
                    // Determine biome based on smoothed noise value
                    int biome = DetermineBiome(noise);
                    _biomeMapCache[x, z] = biome;
                }
            }
        }
        
        private void ApplySeamlessChunkBoundaries(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            // Sample from neighboring chunks to create seamless boundaries
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Get boundary weights based on distance to chunk edges
                    float edgeWeightX = CalculateEdgeWeight(x, chunkSize);
                    float edgeWeightZ = CalculateEdgeWeight(z, chunkSize);
                    
                    if (edgeWeightX > 0 || edgeWeightZ > 0)
                    {
                        // Sample from neighboring chunks
                        float neighborHeight = SampleNeighboringChunks(chunkX, chunkZ, x, z);
                        _heightMapCache[x, z] = Mathf.Lerp(_heightMapCache[x, z], neighborHeight, 
                            Mathf.Max(edgeWeightX, edgeWeightZ));
                    }
                }
            }
        }
        
        private float CalculateEdgeWeight(int coord, int chunkSize)
        {
            // Calculate distance to nearest edge (0 or chunkSize-1)
            int distanceToEdge = Mathf.Min(coord, chunkSize - 1 - coord);
            
            // Apply smooth falloff from edge
            if (distanceToEdge <= 2)
                return 0f; // No blending very close to edge
            else if (distanceToEdge <= 3)
                return 0.3f * (3 - distanceToEdge + 1); // Gradual increase
            else
                return 1f; // Full influence in interior
        }
        
        private float SampleNeighboringChunks(int chunkX, int chunkZ, int localX, int localZ)
        {
            // This would need access to neighboring chunk data
            // For now, return the current height as a fallback
            // In a full implementation, this would sample from adjacent chunks
            return _heightMapCache[localX, localZ];
        }
        
        private float ApplyBiomeHeightModifier(float baseHeight, int biome)
        {
            return biome switch
            {
                0 => baseHeight * 0.8f, // Plains - lower terrain
                1 => baseHeight * 1.4f, // Mountains - higher terrain
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
                < 0.15f => 3, // Desert
                < 0.35f => 0, // Plains
                < 0.55f => 2, // Forest
                < 0.75f => 4, // Hills
                _ => 1       // Mountains
            };
        }
        
        private void GenerateEnhancedTerrain(int[,,] blocks, int chunkX, int chunkZ)
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
                    
                    // Generate terrain layers with enhanced surface detail
                    for (int y = 0; y < terrainHeight; y++)
                    {
                        int blockId = GetBlockForTerrainLayer(y, terrainHeight, seaLevel, biome);
                        blocks[x, y, z] = blockId;
                    }
                    
                    // Add water below sea level with enhanced variation
                    for (int y = terrainHeight; y < seaLevel; y++)
                    {
                        // Add underwater terrain variation
                        float depth = seaLevel - y;
                        float depthVariation = Mathf.PerlinNoise(
                            (chunkX * chunkSize + x) * 0.1f,
                            (chunkZ * chunkSize + z) * 0.1f,
                            depth * 0.2f);
                        
                        if (depthVariation > 0.3f && depth > 2)
                        {
                            blocks[x, y, z] = GetBlockId("sand"); // Underwater sand variations
                        }
                        else if (depthVariation > -0.2f && depth > 4)
                        {
                            blocks[x, y, z] = GetBlockId("gravel"); // Underwater gravel
                        }
                        else
                        {
                            blocks[x, y, z] = GetBlockId("water");
                        }
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
            // Surface layers with enhanced detail
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
            
            // Sub-surface layers with biome-specific materials
            if (y >= terrainHeight - 4)
            {
                return biome switch
                {
                    0 => GetBlockId("dirt"), // Plains
                    1 => GetBlockId("stone"), // Mountains
                    2 => GetBlockId("dirt"), // Forest
                    3 => GetBlockId("sandstone"), // Desert
                    4 => GetBlockId("dirt"), // Hills
                    _ => GetBlockId("dirt")
                };
            }
            
            // Underground
            return GetBlockId("stone");
        }
        
        private void GenerateEnhancedCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Caves.EnableCaves) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate enhanced cave density map with multi-level support
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    float worldX = (chunkX * chunkSize + x) * _worldConfig.Caves.HorizontalFrequency;
                    float worldY = y * _worldConfig.Caves.VerticalFrequency;
                    
                    // Multi-octave cave noise for complexity
                    float caveValue = _caveNoise.GetNoise(worldX, worldY);
                    
                    // Apply connectivity enhancement
                    if (_tuning.EnableMultiLevelCaves && y > _worldConfig.Caves.MinCaveHeight)
                    {
                        // Check for connectivity with levels above/below
                        float connectivityBonus = CalculateCaveConnectivity(x, y, z, chunkX, chunkZ);
                        caveValue += connectivityBonus * _tuning.CaveConnectivityThreshold;
                    }
                    
                    _caveMapCache[x, y] = caveValue;
                }
            }
            
            // Apply enhanced cave generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = _worldConfig.Caves.MinCaveHeight; y < _worldConfig.Caves.MaxCaveHeight; y++)
                    {
                        float caveValue = _caveMapCache[x, y];
                        
                        // Enhanced cave threshold with tunnel width consideration
                        float threshold = _worldConfig.Caves.Threshold;
                        
                        // Create wider tunnels in certain areas
                        if (UnityEngine.Random.value < _tuning.CaveTunnelWidth)
                        {
                            threshold *= 0.7f; // Wider tunnels
                        }
                        
                        if (caveValue > threshold)
                        {
                            blocks[x, y, z] = 0; // Air
                            
                            // Enhanced liquid placement
                            if (y < 10 && UnityEngine.Random.value < _tuning.CaveLavaPoolChance)
                            {
                                // Create lava pools with better distribution
                                CreateLavaPool(blocks, x, y, z, chunkX, chunkZ);
                            }
                            else if (y < _worldConfig.Terrain.SeaLevel - 5 && UnityEngine.Random.value < _tuning.CaveWaterPoolChance)
                            {
                                // Create water pools at cave entrances
                                CreateWaterPool(blocks, x, y, z, chunkX, chunkZ);
                            }
                        }
                    }
                }
            }
            
            // Add cave support structures
            AddCaveSupports(blocks, chunkX, chunkZ);
        }
        
        private float CalculateCaveConnectivity(int x, int y, int z, int chunkX, int chunkZ)
        {
            // Calculate connectivity bonus based on neighboring cave spaces
            float connectivity = 0f;
            int sampleRadius = 2;
            
            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue; // Skip center
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                            ny >= 0 && ny < _worldConfig.WorldHeight && 
                            nz >= 0 && nz < _worldConfig.ChunkSize)
                        {
                            float neighborValue = _caveMapCache[nx, ny];
                            if (neighborValue > _worldConfig.Caves.Threshold)
                            {
                                connectivity += 0.2f;
                            }
                        }
                    }
                }
            }
            
            return Mathf.Clamp01(connectivity / 8f); // Normalize by maximum possible connections
        }
        
        private void CreateLavaPool(int[,,] blocks, int x, int y, int z, int chunkX, int chunkZ)
        {
            // Create small lava pools with better distribution
            int poolRadius = UnityEngine.Random.Range(1, 3);
            
            for (int dx = -poolRadius; dx <= poolRadius; dx++)
            {
                for (int dz = -poolRadius; dz <= poolRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize &&
                        blocks[nx, y, nz] == 0) // Only place in existing caves
                    {
                        blocks[nx, y, nz] = GetBlockId("lava");
                    }
                }
            }
        }
        
        private void CreateWaterPool(int[,,] blocks, int x, int y, int z, int chunkX, int chunkZ)
        {
            // Create water pools at cave entrances
            int poolRadius = UnityEngine.Random.Range(1, 2);
            
            for (int dx = -poolRadius; dx <= poolRadius; dx++)
            {
                for (int dz = -poolRadius; dz <= poolRadius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        blocks[nx, y, nz] = GetBlockId("water");
                    }
                }
            }
        }
        
        private void AddCaveSupports(int[,,] blocks, int chunkX, int chunkZ)
        {
            // Add support pillars and structures in caves for better stability
            if (UnityEngine.Random.value < 0.1f) // 10% chance per chunk
            {
                int supportCount = UnityEngine.Random.Range(1, 4);
                
                for (int i = 0; i < supportCount; i++)
                {
                    int x = UnityEngine.Random.Range(2, _worldConfig.ChunkSize - 3);
                    int z = UnityEngine.Random.Range(2, _worldConfig.ChunkSize - 3);
                    int y = UnityEngine.Random.Range(_worldConfig.Caves.MinCaveHeight + 2, _worldConfig.Caves.MaxCaveHeight - 2);
                    
                    // Find cave ceiling
                    int ceilingY = y;
                    while (ceilingY < _worldConfig.WorldHeight - 1 && blocks[x, ceilingY + 1, z] == 0)
                    {
                        ceilingY++;
                    }
                    
                    // Place support pillar
                    for (int py = y; py <= ceilingY; py++)
                    {
                        if (blocks[x, py, z] == 0)
                        {
                            blocks[x, py, z] = GetBlockId("stone");
                        }
                    }
                }
            }
        }
        
        private void GenerateEnhancedRivers(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableRivers) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate enhanced river map with meandering
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    // Multi-octave river noise for natural meandering
                    float riverValue = _riverNoise.GetNoise(worldX, worldZ);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    
                    // Apply meandering enhancement
                    float meander = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 0.5f;
                    riverValue = riverValue * (1f + meander * _tuning.RiverMeanderStrength);
                    
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
            // Apply enhanced river generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float riverValue = _riverMapCache[x, z];
                    
                    if (riverValue < _worldConfig.Water.RiverCenterThreshold)
                    {
                        // Enhanced river carving with variable width
                        int riverWidth = CalculateRiverWidth(riverValue);
                        
                        // Carve river channel
                        for (int y = seaLevel; y < worldHeight; y++)
                        {
                            bool inRiverChannel = IsInRiverChannel(x, z, y, riverWidth);
                            
                            if (inRiverChannel)
                            {
                                if (blocks[x, y, z] != 0) // If not already air
                                {
                                    blocks[x, y, z] = 0; // Air
                                }
                            }
                            else if (y < seaLevel + _tuning.RiverMinWidth)
                            {
                                // Add river water
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                        
                        // Create river banks with enhanced materials
                        CreateRiverBanks(blocks, x, z, y, riverValue, riverWidth, seaLevel);
                    }
                }
            }
            
            // Add river tributaries if enabled
            if (_tuning.EnableRiverTributaries)
            {
                AddRiverTributaries(blocks, chunkX, chunkZ);
            }
        }
        
        private int CalculateRiverWidth(float riverValue)
        {
            // Calculate variable river width based on river strength
            float normalizedStrength = 1f - Mathf.Clamp01(riverValue / _worldConfig.Water.RiverCenterThreshold);
            int baseWidth = Mathf.RoundToInt(_tuning.RiverMinWidth + 
                (_tuning.RiverMaxWidth - _tuning.RiverMinWidth) * normalizedStrength);
            
            // Add variation
            float variation = Mathf.PerlinNoise(
                (chunkX * _worldConfig.ChunkSize + x) * 0.1f,
                (chunkZ * _worldConfig.ChunkSize + z) * 0.1f) * 0.5f;
            
            return Mathf.RoundToInt(baseWidth * (1f + variation * _tuning.RiverWidthVariation));
        }
        
        private bool IsInRiverChannel(int x, int z, int y, int riverWidth)
        {
            // Check if position is within river channel
            float centerDistance = Mathf.Sqrt(
                Mathf.Pow(x - _worldConfig.ChunkSize * 0.5f, 2) + 
                Mathf.Pow(z - _worldConfig.ChunkSize * 0.5f, 2));
            
            // Wider channels at center, narrower at edges
            float widthAtDistance = riverWidth * (1f - centerDistance / (_worldConfig.ChunkSize * 0.5f));
            float verticalProfile = Mathf.Sin((y - _worldConfig.Terrain.SeaLevel) * Mathf.PI / riverWidth);
            
            return centerDistance < widthAtDistance && verticalProfile > 0.3f;
        }
        
        private void CreateRiverBanks(int[,,] blocks, int x, int z, int y, float riverValue, int riverWidth, int seaLevel)
        {
            // Enhanced river bank materials with erosion patterns
            float erosionFactor = Mathf.Clamp01(riverValue / _worldConfig.Water.RiverCenterThreshold);
            
            if (UnityEngine.Random.value < 0.7f)
            {
                // Main bank material
                blocks[x, y, z] = GetBlockId("sand");
            }
            else if (erosionFactor > 0.5f)
            {
                // Eroded material
                blocks[x, y, z] = GetBlockId("gravel");
            }
            else
            {
                // Natural bank material
                blocks[x, y, z] = GetBlockId("dirt");
            }
        }
        
        private void AddRiverTributaries(int[,,] blocks, int chunkX, int chunkZ)
        {
            // Add smaller tributary streams
            int tributaryCount = UnityEngine.Random.Range(0, 2);
            
            for (int i = 0; i < tributaryCount; i++)
            {
                int startX = UnityEngine.Random.Range(4, _worldConfig.ChunkSize - 5);
                int startZ = UnityEngine.Random.Range(4, _worldConfig.ChunkSize - 5);
                int length = UnityEngine.Random.Range(3, 8);
                int width = UnityEngine.Random.Range(1, 3);
                
                // Create tributary path
                for (int j = 0; j < length; j++)
                {
                    int x = startX + j;
                    int z = startZ + j;
                    
                    if (x >= 2 && x < _worldConfig.ChunkSize - 2 && 
                        z >= 2 && z < _worldConfig.ChunkSize - 2)
                    {
                        // Small stream channel
                        for (int w = -width; w <= width; w++)
                        {
                            for (int y = _worldConfig.Terrain.SeaLevel - 1; y < _worldConfig.Terrain.SeaLevel + 2; y++)
                            {
                                if (blocks[x + w, y, z + w] != 0)
                                {
                                    blocks[x + w, y, z + w] = 0;
                                }
                                else if (y == _worldConfig.Terrain.SeaLevel)
                                {
                                    blocks[x + w, y, z + w] = GetBlockId("water");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void GenerateEnhancedLakes(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableLakes) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate enhanced lake map with depth variation
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
            
            // Apply enhanced lake generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.65f) // Lake threshold
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
                        
                        // Enhanced lake depth calculation
                        int baseDepth = Mathf.RoundToInt(UnityEngine.Random.Range(
                            _tuning.LakeMinDepth, _tuning.LakeMaxDepth));
                        
                        // Apply depth variation
                        float depthVariation = Mathf.PerlinNoise(
                            worldX * 0.05f, worldZ * 0.05f) * 0.5f;
                        int lakeDepth = Mathf.RoundToInt(baseDepth * (1f + depthVariation * _tuning.LakeDepthVariation));
                        int lakeBottom = Mathf.Max(terrainHeight - lakeDepth, 1);
                        
                        // Create lake basin with enhanced shaping
                        for (int y = lakeBottom; y <= terrainHeight; y++)
                        {
                            if (blocks[x, y, z] != 0)
                            {
                                blocks[x, y, z] = 0; // Remove terrain
                            }
                        }
                        
                        // Fill with water
                        int waterLevel = Mathf.Min(lakeBottom + lakeDepth / 2, seaLevel);
                        for (int y = lakeBottom; y < waterLevel; y++)
                        {
                            if (y >= 0 && y < worldHeight)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                        
                        // Create enhanced shoreline with complexity
                        CreateEnhancedShoreline(blocks, x, z, lakeBottom, waterLevel, lakeValue, seaLevel);
                        
                        // Add lake outflow if enabled
                        if (_tuning.EnableLakeOutflow)
                        {
                            CreateLakeOutflow(blocks, x, z, waterLevel, seaLevel);
                        }
                        
                        // Add islands if enabled
                        if (UnityEngine.Random.value < _tuning.LakeIslandChance)
                        {
                            CreateLakeIsland(blocks, x, z, waterLevel, seaLevel);
                        }
                    }
                }
            }
        }
        
        private void CreateEnhancedShoreline(int[,,] blocks, int x, int z, int lakeBottom, int waterLevel, float lakeValue, int seaLevel)
        {
            // Create complex shoreline with multiple materials
            float shoreComplexity = _tuning.LakeShoreComplexity;
            
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        
                        if (distance <= 2)
                        {
                            // Close to shore - complex materials
                            float noise = UnityEngine.Random.value;
                            
                            if (noise < 0.3f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("sand");
                            }
                            else if (noise < 0.6f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("gravel");
                            }
                            else if (noise < 0.8f * shoreComplexity)
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("clay");
                            }
                            else
                            {
                                blocks[nx, waterLevel, nz] = GetBlockId("dirt");
                            }
                        }
                    }
                }
            }
        }
        
        private void CreateLakeOutflow(int[,,] blocks, int x, int z, int waterLevel, int seaLevel)
        {
            // Create natural lake outflow streams
            var downhill = FindDownhillDirection(x, z);
            
            if (downhill != Vector2Int.zero)
            {
                int outflowLength = UnityEngine.Random.Range(3, 8);
                int outflowWidth = UnityEngine.Random.Range(1, 2);
                
                for (int i = 1; i <= outflowLength; i++)
                {
                    int nx = x + downhill.x * i;
                    int nz = z + downhill.y * i;
                    
                    if (nx >= 1 && nx < _worldConfig.ChunkSize - 1 && 
                        nz >= 1 && nz < _worldConfig.ChunkSize - 1)
                    {
                        // Create outflow channel
                        for (int w = -outflowWidth; w <= outflowWidth; w++)
                        {
                            for (int y = waterLevel; y >= waterLevel - 2; y--)
                            {
                                if (blocks[nx + w, y, nz + w] != 0)
                                {
                                    blocks[nx + w, y, nz + w] = 0;
                                }
                            }
                            
                            if (y == waterLevel)
                            {
                                blocks[nx + w, y, nz + w] = GetBlockId("water");
                            }
                        }
                    }
                }
            }
        }
        
        private void CreateLakeIsland(int[,,] blocks, int x, int z, int waterLevel, int seaLevel)
        {
            // Create small islands in lakes
            int islandSize = UnityEngine.Random.Range(2, 4);
            int islandHeight = UnityEngine.Random.Range(waterLevel + 1, waterLevel + 3);
            
            for (int dx = -islandSize; dx <= islandSize; dx++)
            {
                for (int dz = -islandSize; dz <= islandSize; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                        nz >= 0 && nz < _worldConfig.ChunkSize)
                    {
                        // Build island up from lake bottom
                        for (int y = waterLevel - 2; y <= islandHeight; y++)
                        {
                            if (blocks[nx, y, nz] == 0)
                            {
                                blocks[nx, y, nz] = GetBlockId("dirt");
                            }
                        }
                        
                        // Top with grass
                        blocks[nx, islandHeight, nz] = GetBlockId("grass");
                    }
                }
            }
        }
        
        private Vector2Int FindDownhillDirection(int x, int z)
        {
            // Find the direction of steepest descent
            int surfaceHeight = FindSurfaceHeight(x, z);
            Vector2Int bestDirection = Vector2Int.zero;
            int bestDrop = 0;
            
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            
            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int nz = z + dir.y;
                
                if (nx >= 0 && nx < _worldConfig.ChunkSize && 
                    nz >= 0 && nz < _worldConfig.ChunkSize)
                {
                    int neighborHeight = FindSurfaceHeight(nx, nz);
                    int drop = surfaceHeight - neighborHeight;
                    
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDirection = dir;
                    }
                }
            }
            
            return bestDirection;
        }
        
        private int FindSurfaceHeight(int x, int z)
        {
            // Find the first non-air block from top down
            for (int y = _worldConfig.WorldHeight - 1; y >= 0; y--)
            {
                if (_heightMapCache[x, z] > 0.1f) // Approximate surface from heightmap
                {
                    return Mathf.RoundToInt(_heightMapCache[x, z] * _worldConfig.Terrain.MountainMaxHeight);
                }
            }
            return 0;
        }
        
        private void GenerateEnhancedOres(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Ores.EnableOreGeneration) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate enhanced ore veins with better distribution
            foreach (var oreEntry in _worldConfig.Ores.Ores)
            {
                string oreName = oreEntry.Key;
                var oreConfig = oreEntry.Value;
                
                if (oreConfig == null) continue;
                
                int veinsPerChunk = oreConfig.VeinsPerChunk;
                
                for (int i = 0; i < veinsPerChunk; i++)
                {
                    // Enhanced vein positioning with depth and biome consideration
                    int veinX = UnityEngine.Random.Range(0, chunkSize);
                    int biome = _biomeMapCache[veinX, UnityEngine.Random.Range(0, chunkSize)];
                    int minHeight = GetBiomeSpecificMinHeight(oreName, biome, oreConfig);
                    int maxY = GetBiomeSpecificMaxHeight(oreName, biome, oreConfig);
                    int veinY = UnityEngine.Random.Range(minHeight, maxY);
                    int veinZ = UnityEngine.Random.Range(0, chunkSize);
                    
                    // Generate enhanced ore vein
                    GenerateEnhancedOreVein(blocks, veinX, veinY, veinZ, oreName, oreConfig);
                }
            }
        }
        
        private int GetBiomeSpecificMinHeight(string oreName, int biome, OreConfig config)
        {
            // Different ores spawn at different depths based on biome
            return (oreName, biome) switch
            {
                ("coal", 0) => Mathf.Max(config.MinHeight, 5), // Coal in mountains/plains
                ("coal", 1) => Mathf.Max(config.MinHeight, 8), // Coal in forests
                ("coal", 3) => Mathf.Max(config.MinHeight, 2), // Coal in deserts
                ("iron", _) => Mathf.Max(config.MinHeight, 5), // Iron everywhere
                ("gold", 1) => Mathf.Max(config.MinHeight, 20), // Gold in mountains
                ("diamond", 1) => Mathf.Max(config.MinHeight, 12), // Diamond deep underground
                _ => config.MinHeight
            };
        }
        
        private int GetBiomeSpecificMaxHeight(string oreName, int biome, OreConfig config)
        {
            return (oreName, biome) switch
            {
                ("diamond", 1) => Mathf.Min(config.MaxHeight, 20), // Diamonds limited in mountains
                ("gold", 1) => Mathf.Min(config.MaxHeight, 35), // Gold in mountains
                _ => config.MaxHeight
            };
        }
        
        private void GenerateEnhancedOreVein(int[,,] blocks, int startX, int startY, int startZ, string oreName, OreConfig config)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            // Enhanced vein generation with better shapes
            int veinSize = config.VeinSize;
            Vector3Int currentPos = new Vector3Int(startX, startY, startZ);
            
            for (int i = 0; i < veinSize; i++)
            {
                // Random walk with directional bias
                Vector3Int direction = GetRandomWalkDirection();
                currentPos += direction;
                
                // Add some randomness to path
                if (UnityEngine.Random.value < 0.3f)
                {
                    direction = GetRandomWalkDirection();
                }
                
                // Check bounds and place ore
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    // Only replace certain stone types
                    int currentBlock = blocks[currentPos.x, currentPos.y, currentPos.z];
                    if (currentBlock == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                    else if (currentBlock == GetBlockId("dirt") && UnityEngine.Random.value < 0.1f)
                    {
                        // Small chance to replace dirt with ore
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
            }
        }
        
        private Vector3Int GetRandomWalkDirection()
        {
            // Random walk direction with 3D bias
            int choice = UnityEngine.Random.Range(0, 26);
            
            return choice switch
            {
                < 8 => new Vector3Int(1, 0, 0),   // East
                < 16 => new Vector3Int(-1, 0, 0),  // West
                < 2 => new Vector3Int(0, 1, 0),    // South
                < 10 => new Vector3Int(0, -1, 0),  // North
                < 20 => new Vector3Int(0, 0, 1),    // Up
                < 24 => new Vector3Int(0, 0, -1),  // Down
                _ => new Vector3Int(0, 0, 0)     // Stay
            };
        }
        
        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
    
    /// <summary>
    /// Enhanced FastNoise implementation with additional features
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
            // Enhanced simplex noise implementation
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
            // Enhanced white noise with better distribution
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
}
