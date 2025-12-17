using System;
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
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            
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
            _riverNoise.SetFrequency(0.003f);
            
            // Lake noise
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _lakeNoise.SetFrequency(0.002f);
            
            // Biome noise
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Ore noise
            _oreNoise = new FastNoise(seed + 5);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
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
            
            // Apply cave generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = _worldConfig.Caves.MinCaveHeight; y < _worldConfig.Caves.MaxCaveHeight; y++)
                    {
                        float caveValue = _caveMapCache[x, y];
                        
                        if (caveValue > _worldConfig.Caves.Threshold)
                        {
                            blocks[x, y, z] = 0; // Air
                            
                            // Add lava at low levels
                            if (y < 10 && UnityEngine.Random.value < 0.1f)
                            {
                                blocks[x, y, z] = GetBlockId("lava");
                            }
                            // Add water at cave entrances
                            else if (y < _worldConfig.Terrain.SeaLevel && UnityEngine.Random.value < 0.05f)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
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
                    
                    float riverValue = _riverNoise.GetNoise(worldX, worldZ);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
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
                        for (int y = seaLevel - _worldConfig.Water.RiverDepth; y < seaLevel; y++)
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
            
            // Apply lake generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.7f) // Lake threshold
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
                        int lakeDepth = Mathf.RoundToInt(UnityEngine.Random.Range(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth));
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
                    }
                }
            }
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
}using UnityEngine;
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
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            
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
            _riverNoise.SetFrequency(0.003f);
            
            // Lake noise
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _lakeNoise.SetFrequency(0.002f);
            
            // Biome noise
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Ore noise
            _oreNoise = new FastNoise(seed + 5);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
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
            
            // Apply cave generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = _worldConfig.Caves.MinCaveHeight; y < _worldConfig.Caves.MaxCaveHeight; y++)
                    {
                        float caveValue = _caveMapCache[x, y];
                        
                        if (caveValue > _worldConfig.Caves.Threshold)
                        {
                            blocks[x, y, z] = 0; // Air
                            
                            // Add lava at low levels
                            if (y < 10 && UnityEngine.Random.value < 0.1f)
                            {
                                blocks[x, y, z] = GetBlockId("lava");
                            }
                            // Add water at cave entrances
                            else if (y < _worldConfig.Terrain.SeaLevel && UnityEngine.Random.value < 0.05f)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
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
                    
                    float riverValue = _riverNoise.GetNoise(worldX, worldZ);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
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
                        for (int y = seaLevel - _worldConfig.Water.RiverDepth; y < seaLevel; y++)
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
            
            // Apply lake generation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.7f) // Lake threshold
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
                        int lakeDepth = Mathf.RoundToInt(UnityEngine.Random.Range(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth));
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
                    }
                }
            }
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
}
