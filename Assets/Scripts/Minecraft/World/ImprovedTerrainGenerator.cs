using System;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Improved terrain generation system with enhanced algorithms
    /// Provides better performance and more realistic terrain features
    /// </summary>
    public class ImprovedTerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        // Noise generators with improved caching
        private FastNoise _terrainNoise;
        private FastNoise _caveNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _biomeNoise;
        private FastNoise _oreNoise;
        private FastNoise _detailNoise;
        
        // Performance optimization caches
        private readonly float[,] _heightMapCache = new float[16, 16];
        private readonly int[,] _biomeMapCache = new int[16, 16];
        private readonly float[,] _caveMapCache = new float[16, 256];
        private readonly float[,] _riverMapCache = new float[16, 16];
        private readonly float[,] _lakeMapCache = new float[16, 16];
        private readonly float[,] _detailMapCache = new float[16, 16];
        
        // Thread-safe chunk generation
        private readonly object _generationLock = new object();
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            
            InitializeNoiseGenerators();
        }
        
        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed;
            
            // Terrain noise with improved octaves
            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(6); // Increased from 4 for more detail
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            
            // Cave noise with multiple layers
            _caveNoise = new FastNoise(seed + 1);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency);
            _caveNoise.SetFractalOctaves(3); // Multi-layered caves
            
            // River noise with improved flow
            _riverNoise = new FastNoise(seed + 2);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _riverNoise.SetFrequency(0.003f);
            
            // Lake noise with better basin formation
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _lakeNoise.SetFrequency(0.002f);
            
            // Biome noise with improved transitions
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Ore noise with better distribution
            _oreNoise = new FastNoise(seed + 5);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
            
            // Detail noise for surface features
            _detailNoise = new FastNoise(seed + 6);
            _detailNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _detailNoise.SetFrequency(0.1f);
            _detailNoise.SetFractalOctaves(2);
        }
        
        /// <summary>
        /// Generate terrain for a chunk with improved algorithms
        /// </summary>
        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            lock (_generationLock)
            {
                int chunkSize = _worldConfig.ChunkSize;
                int worldHeight = _worldConfig.WorldHeight;
                var blocks = new int[chunkSize, worldHeight, chunkSize];
                
                // Generate all maps in parallel where possible
                GenerateHeightMap(chunkX, chunkZ);
                GenerateBiomeMap(chunkX, chunkZ);
                GenerateDetailMap(chunkX, chunkZ);
                
                // Generate base terrain
                GenerateTerrain(blocks, chunkX, chunkZ);
                
                // Apply features in order of importance
                if (_worldConfig.Caves.EnableCaves)
                {
                    GenerateImprovedCaves(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Water.EnableRivers)
                {
                    GenerateImprovedRivers(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Water.EnableLakes)
                {
                    GenerateImprovedLakes(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Ores.EnableOreGeneration)
                {
                    GenerateImprovedOres(blocks, chunkX, chunkZ);
                }
                
                // Apply surface details
                ApplySurfaceDetails(blocks, chunkX, chunkZ);
                
                return blocks;
            }
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
                    
                    // Multi-octave terrain with ridged noise for mountains
                    float baseHeight = _terrainNoise.GetNoise(worldX, worldZ);
                    float ridgeNoise = GenerateRidgedNoise(worldX, worldZ, 0.001f, 4);
                    
                    // Combine base terrain with ridged mountains
                    float height = baseHeight * 0.7f + ridgeNoise * 0.3f;
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
                    
                    // Multi-layer biome generation for smoother transitions
                    float primaryNoise = _biomeNoise.GetNoise(worldX, worldZ);
                    float secondaryNoise = _biomeNoise.GetNoise(worldX * 0.5f, worldZ * 0.5f);
                    
                    // Blend primary and secondary for smoother transitions
                    float noise = primaryNoise * 0.7f + secondaryNoise * 0.3f;
                    noise = (noise + 1f) * 0.5f; // Normalize to 0-1
                    
                    int biome = DetermineBiome(noise);
                    _biomeMapCache[x, z] = biome;
                }
            }
        }
        
        private void GenerateDetailMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    float detail = _detailNoise.GetNoise(worldX, worldZ);
                    detail = (detail + 1f) * 0.5f; // Normalize to 0-1
                    
                    _detailMapCache[x, z] = detail;
                }
            }
        }
        
        private float GenerateRidgedNoise(float x, float z, float frequency, int octaves)
        {
            float sum = 0f;
            float amplitude = 1f;
            float freq = frequency;
            
            for (int i = 0; i < octaves; i++)
            {
                float noise = Mathf.Abs(_terrainNoise.GetNoise(x * freq, z * freq));
                sum += (1f - noise) * amplitude;
                amplitude *= 0.5f;
                freq *= 2f;
            }
            
            return sum;
        }
        
        private float ApplyBiomeHeightModifier(float baseHeight, int biome)
        {
            return biome switch
            {
                0 => baseHeight * 0.8f, // Plains - lower terrain
                1 => baseHeight * 1.4f, // Mountains - higher terrain (increased)
                2 => baseHeight * 0.9f, // Forest - slightly lower
                3 => baseHeight * 0.7f, // Desert - lower with dunes
                4 => baseHeight * 1.1f, // Hills - slightly higher
                5 => baseHeight * 0.85f, // Swamp - lower, wet terrain
                6 => baseHeight * 1.2f, // Taiga - cold, elevated terrain
                _ => baseHeight
            };
        }
        
        private int DetermineBiome(float noiseValue)
        {
            // Expanded biome system with smoother transitions
            return noiseValue switch
            {
                < 0.15f => 3, // Desert
                < 0.35f => 0, // Plains
                < 0.5f => 2, // Forest
                < 0.65f => 5, // Swamp
                < 0.8f => 4, // Hills
                < 0.9f => 6, // Taiga
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
                    float detailValue = _detailMapCache[x, z];
                    
                    // Apply detail noise to height
                    heightValue += detailValue * 0.1f;
                    
                    int terrainHeight = Mathf.RoundToInt(heightValue * _worldConfig.Terrain.MountainMaxHeight);
                    terrainHeight = Mathf.Clamp(terrainHeight, 5, worldHeight - 5);
                    
                    int biome = _biomeMapCache[x, z];
                    
                    // Generate terrain layers with biome-specific variations
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
            
            // Add bedrock layer with variation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Variable bedrock thickness
                    int bedrockThickness = UnityEngine.Random.Range(1, 4);
                    for (int y = 0; y < bedrockThickness && y < _worldConfig.Terrain.BedrockLevel; y++)
                    {
                        blocks[x, y, z] = GetBlockId("bedrock");
                    }
                }
            }
        }
        
        private int GetBlockForTerrainLayer(int y, int terrainHeight, int seaLevel, int biome)
        {
            // Surface layers with biome-specific blocks
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
                    5 => GetBlockId("dirt"),  // Swamp
                    6 => GetBlockId("grass"), // Taiga
                    _ => GetBlockId("grass")
                };
            }
            
            // Sub-surface layers with depth-based variation
            int depthFromSurface = terrainHeight - y;
            if (depthFromSurface <= 3)
            {
                return biome switch
                {
                    3 => GetBlockId("sandstone"), // Desert
                    5 => GetBlockId("clay"),      // Swamp
                    _ => GetBlockId("dirt")
                };
            }
            
            // Underground
            return GetBlockId("stone");
        }
        
        private void GenerateImprovedCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Caves.EnableCaves) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Multi-layer cave generation
            GenerateCaveLayer(blocks, chunkX, chunkZ, _worldConfig.Caves.HorizontalFrequency, _worldConfig.Caves.VerticalFrequency, _worldConfig.Caves.Threshold);
            
            // Add secondary cave layer for more complexity
            GenerateCaveLayer(blocks, chunkX, chunkZ, _worldConfig.Caves.HorizontalFrequency * 2f, _worldConfig.Caves.VerticalFrequency * 1.5f, _worldConfig.Caves.Threshold * 0.8f);
            
            // Add lava and water in caves
            AddCaveLiquids(blocks, chunkX, chunkZ);
        }
        
        private void GenerateCaveLayer(int[,,] blocks, int chunkX, int chunkZ, float hFreq, float vFreq, float threshold)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    float worldX = (chunkX * chunkSize + x) * hFreq;
                    float worldY = y * vFreq;
                    
                    float caveValue = _caveNoise.GetNoise(worldX, worldY);
                    
                    if (caveValue > threshold)
                    {
                        blocks[x, y, z] = 0; // Air
                    }
                }
            }
        }
        
        private void AddCaveLiquids(int[,,] blocks, int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        if (blocks[x, y, z] != 0) continue; // Not air
                        
                        // Add lava at low levels
                        if (y < 10 && UnityEngine.Random.value < 0.05f)
                        {
                            blocks[x, y, z] = GetBlockId("lava");
                        }
                        // Add water at cave entrances
                        else if (y < _worldConfig.Terrain.SeaLevel && UnityEngine.Random.value < 0.03f)
                        {
                            blocks[x, y, z] = GetBlockId("water");
                        }
                    }
                }
            }
        }
        
        private void GenerateImprovedRivers(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableRivers) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate river map with improved flow
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    // Multi-octave river noise for more natural flow
                    float primaryRiver = _riverNoise.GetNoise(worldX, worldZ);
                    float secondaryRiver = _riverNoise.GetNoise(worldX * 0.5f, worldZ * 0.5f);
                    
                    float riverValue = (primaryRiver * 0.7f + secondaryRiver * 0.3f);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
            // Apply river generation with improved carving
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
                        // River banks with improved materials
                        GenerateRiverBanks(blocks, x, z, seaLevel, worldHeight);
                    }
                }
            }
        }
        
        private void GenerateRiverBanks(int[,,] blocks, int x, int z, int seaLevel, int worldHeight)
        {
            for (int y = seaLevel; y < worldHeight; y++)
            {
                if (blocks[x, y, z] != 0 && blocks[x, y, z] != GetBlockId("water"))
                {
                    // Replace with appropriate bank material
                    if (UnityEngine.Random.value < 0.6f)
                        blocks[x, y, z] = GetBlockId("sand");
                    else if (UnityEngine.Random.value < 0.8f)
                        blocks[x, y, z] = GetBlockId("gravel");
                    else
                        blocks[x, y, z] = GetBlockId("dirt");
                }
            }
        }
        
        private void GenerateImprovedLakes(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableLakes) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate lake map with improved basin formation
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
            
            // Apply lake generation with improved basins
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.7f) // Lake threshold
                    {
                        GenerateLakeBasin(blocks, x, z, worldHeight);
                    }
                }
            }
        }
        
        private void GenerateLakeBasin(int[,,] blocks, int x, int z, int worldHeight)
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
            
            // Create lake basin with variable depth
            int lakeDepth = UnityEngine.Random.Range(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth);
            int lakeBottom = Mathf.Max(terrainHeight - lakeDepth, 1);
            
            // Carve basin
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
        
        private void GenerateImprovedOres(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Ores.EnableOreGeneration) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate ore veins with improved distribution
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
                    
                    // Generate improved ore vein
                    GenerateImprovedOreVein(blocks, veinX, veinY, veinZ, oreName, oreConfig.VeinSize);
                }
            }
        }
        
        private void GenerateImprovedOreVein(int[,,] blocks, int startX, int startY, int startZ, string oreName, int veinSize)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            // Improved vein generation with better shape
            Vector3Int currentPos = new Vector3Int(startX, startY, startZ);
            int remainingSize = veinSize;
            
            while (remainingSize > 0)
            {
                // Place ore at current position
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    // Only replace stone
                    if (blocks[currentPos.x, currentPos.y, currentPos.z] == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
                
                // Random walk with bias
                Vector3Int direction = GetRandomVeinDirection();
                currentPos += direction;
                remainingSize--;
                
                // Add some branching
                if (remainingSize > 0 && UnityEngine.Random.value < 0.2f)
                {
                    Vector3Int branchStart = currentPos;
                    int branchSize = UnityEngine.Random.Range(1, remainingSize / 2);
                    GenerateOreBranch(blocks, branchStart, oreName, branchSize);
                }
            }
        }
        
        private void GenerateOreBranch(int[,,] blocks, Vector3Int startPos, string oreName, int branchSize)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            Vector3Int currentPos = startPos;
            
            for (int i = 0; i < branchSize; i++)
            {
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    if (blocks[currentPos.x, currentPos.y, currentPos.z] == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
                
                Vector3Int direction = GetRandomVeinDirection();
                currentPos += direction;
            }
        }
        
        private Vector3Int GetRandomVeinDirection()
        {
            int dir = UnityEngine.Random.Range(0, 6);
            return dir switch
            {
                0 => Vector3Int.up,
                1 => Vector3Int.down,
                2 => Vector3Int.left,
                3 => Vector3Int.right,
                4 => Vector3Int.forward,
                5 => Vector3Int.back,
                _ => Vector3Int.up
            };
        }
        
        private void ApplySurfaceDetails(int[,,] blocks, int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Find surface
                    int surfaceY = -1;
                    for (int y = worldHeight - 1; y >= 0; y--)
                    {
                        if (blocks[x, y, z] != 0)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY < 0) continue;
                    
                    int biome = _biomeMapCache[x, z];
                    float detail = _detailMapCache[x, z];
                    
                    // Apply biome-specific surface details
                    ApplyBiomeSurfaceDetails(blocks, x, z, surfaceY, biome, detail);
                }
            }
        }
        
        private void ApplyBiomeSurfaceDetails(int[,,] blocks, int x, int z, int surfaceY, int biome, float detail)
        {
            // Add surface features based on biome and detail noise
            switch (biome)
            {
                case 0: // Plains
                    if (detail > 0.7f && UnityEngine.Random.value < 0.1f)
                    {
                        // Add flowers
                        blocks[x, surfaceY + 1, z] = GetBlockId("flower");
                    }
                    break;
                    
                case 2: // Forest
                    if (UnityEngine.Random.value < 0.05f)
                    {
                        // Add trees (simplified - would need tree generation system)
                        blocks[x, surfaceY + 1, z] = GetBlockId("log");
                    }
                    break;
                    
                case 3: // Desert
                    if (detail > 0.6f && UnityEngine.Random.value < 0.05f)
                    {
                        // Add cacti
                        blocks[x, surfaceY + 1, z] = GetBlockId("cactus");
                    }
                    break;
            }
        }
        
        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
}using UnityEngine;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// Improved terrain generation system with enhanced algorithms
    /// Provides better performance and more realistic terrain features
    /// </summary>
    public class ImprovedTerrainGenerator : MonoBehaviour
    {
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        // Noise generators with improved caching
        private FastNoise _terrainNoise;
        private FastNoise _caveNoise;
        private FastNoise _riverNoise;
        private FastNoise _lakeNoise;
        private FastNoise _biomeNoise;
        private FastNoise _oreNoise;
        private FastNoise _detailNoise;
        
        // Performance optimization caches
        private readonly float[,] _heightMapCache = new float[16, 16];
        private readonly int[,] _biomeMapCache = new int[16, 16];
        private readonly float[,] _caveMapCache = new float[16, 256];
        private readonly float[,] _riverMapCache = new float[16, 16];
        private readonly float[,] _lakeMapCache = new float[16, 16];
        private readonly float[,] _detailMapCache = new float[16, 16];
        
        // Thread-safe chunk generation
        private readonly object _generationLock = new object();
        
        private void Awake()
        {
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            
            InitializeNoiseGenerators();
        }
        
        private void InitializeNoiseGenerators()
        {
            int seed = _worldConfig.Seed;
            
            // Terrain noise with improved octaves
            _terrainNoise = new FastNoise(seed);
            _terrainNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _terrainNoise.SetFrequency(_worldConfig.Terrain.NoiseScale);
            _terrainNoise.SetFractalOctaves(6); // Increased from 4 for more detail
            _terrainNoise.SetFractalLacunarity(_worldConfig.Terrain.Lacunarity);
            _terrainNoise.SetFractalGain(_worldConfig.Terrain.Persistence);
            
            // Cave noise with multiple layers
            _caveNoise = new FastNoise(seed + 1);
            _caveNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _caveNoise.SetFrequency(_worldConfig.Caves.HorizontalFrequency);
            _caveNoise.SetFractalOctaves(3); // Multi-layered caves
            
            // River noise with improved flow
            _riverNoise = new FastNoise(seed + 2);
            _riverNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _riverNoise.SetFrequency(0.003f);
            
            // Lake noise with better basin formation
            _lakeNoise = new FastNoise(seed + 3);
            _lakeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _lakeNoise.SetFrequency(0.002f);
            
            // Biome noise with improved transitions
            _biomeNoise = new FastNoise(seed + 4);
            _biomeNoise.SetNoiseType(FastNoise.NoiseType.Simplex);
            _biomeNoise.SetFrequency(_worldConfig.Terrain.BiomeScale);
            
            // Ore noise with better distribution
            _oreNoise = new FastNoise(seed + 5);
            _oreNoise.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
            
            // Detail noise for surface features
            _detailNoise = new FastNoise(seed + 6);
            _detailNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            _detailNoise.SetFrequency(0.1f);
            _detailNoise.SetFractalOctaves(2);
        }
        
        /// <summary>
        /// Generate terrain for a chunk with improved algorithms
        /// </summary>
        public int[,,] GenerateChunk(int chunkX, int chunkZ)
        {
            lock (_generationLock)
            {
                int chunkSize = _worldConfig.ChunkSize;
                int worldHeight = _worldConfig.WorldHeight;
                var blocks = new int[chunkSize, worldHeight, chunkSize];
                
                // Generate all maps in parallel where possible
                GenerateHeightMap(chunkX, chunkZ);
                GenerateBiomeMap(chunkX, chunkZ);
                GenerateDetailMap(chunkX, chunkZ);
                
                // Generate base terrain
                GenerateTerrain(blocks, chunkX, chunkZ);
                
                // Apply features in order of importance
                if (_worldConfig.Caves.EnableCaves)
                {
                    GenerateImprovedCaves(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Water.EnableRivers)
                {
                    GenerateImprovedRivers(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Water.EnableLakes)
                {
                    GenerateImprovedLakes(blocks, chunkX, chunkZ);
                }
                
                if (_worldConfig.Ores.EnableOreGeneration)
                {
                    GenerateImprovedOres(blocks, chunkX, chunkZ);
                }
                
                // Apply surface details
                ApplySurfaceDetails(blocks, chunkX, chunkZ);
                
                return blocks;
            }
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
                    
                    // Multi-octave terrain with ridged noise for mountains
                    float baseHeight = _terrainNoise.GetNoise(worldX, worldZ);
                    float ridgeNoise = GenerateRidgedNoise(worldX, worldZ, 0.001f, 4);
                    
                    // Combine base terrain with ridged mountains
                    float height = baseHeight * 0.7f + ridgeNoise * 0.3f;
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
                    
                    // Multi-layer biome generation for smoother transitions
                    float primaryNoise = _biomeNoise.GetNoise(worldX, worldZ);
                    float secondaryNoise = _biomeNoise.GetNoise(worldX * 0.5f, worldZ * 0.5f);
                    
                    // Blend primary and secondary for smoother transitions
                    float noise = primaryNoise * 0.7f + secondaryNoise * 0.3f;
                    noise = (noise + 1f) * 0.5f; // Normalize to 0-1
                    
                    int biome = DetermineBiome(noise);
                    _biomeMapCache[x, z] = biome;
                }
            }
        }
        
        private void GenerateDetailMap(int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    float detail = _detailNoise.GetNoise(worldX, worldZ);
                    detail = (detail + 1f) * 0.5f; // Normalize to 0-1
                    
                    _detailMapCache[x, z] = detail;
                }
            }
        }
        
        private float GenerateRidgedNoise(float x, float z, float frequency, int octaves)
        {
            float sum = 0f;
            float amplitude = 1f;
            float freq = frequency;
            
            for (int i = 0; i < octaves; i++)
            {
                float noise = Mathf.Abs(_terrainNoise.GetNoise(x * freq, z * freq));
                sum += (1f - noise) * amplitude;
                amplitude *= 0.5f;
                freq *= 2f;
            }
            
            return sum;
        }
        
        private float ApplyBiomeHeightModifier(float baseHeight, int biome)
        {
            return biome switch
            {
                0 => baseHeight * 0.8f, // Plains - lower terrain
                1 => baseHeight * 1.4f, // Mountains - higher terrain (increased)
                2 => baseHeight * 0.9f, // Forest - slightly lower
                3 => baseHeight * 0.7f, // Desert - lower with dunes
                4 => baseHeight * 1.1f, // Hills - slightly higher
                5 => baseHeight * 0.85f, // Swamp - lower, wet terrain
                6 => baseHeight * 1.2f, // Taiga - cold, elevated terrain
                _ => baseHeight
            };
        }
        
        private int DetermineBiome(float noiseValue)
        {
            // Expanded biome system with smoother transitions
            return noiseValue switch
            {
                < 0.15f => 3, // Desert
                < 0.35f => 0, // Plains
                < 0.5f => 2, // Forest
                < 0.65f => 5, // Swamp
                < 0.8f => 4, // Hills
                < 0.9f => 6, // Taiga
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
                    float detailValue = _detailMapCache[x, z];
                    
                    // Apply detail noise to height
                    heightValue += detailValue * 0.1f;
                    
                    int terrainHeight = Mathf.RoundToInt(heightValue * _worldConfig.Terrain.MountainMaxHeight);
                    terrainHeight = Mathf.Clamp(terrainHeight, 5, worldHeight - 5);
                    
                    int biome = _biomeMapCache[x, z];
                    
                    // Generate terrain layers with biome-specific variations
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
            
            // Add bedrock layer with variation
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Variable bedrock thickness
                    int bedrockThickness = UnityEngine.Random.Range(1, 4);
                    for (int y = 0; y < bedrockThickness && y < _worldConfig.Terrain.BedrockLevel; y++)
                    {
                        blocks[x, y, z] = GetBlockId("bedrock");
                    }
                }
            }
        }
        
        private int GetBlockForTerrainLayer(int y, int terrainHeight, int seaLevel, int biome)
        {
            // Surface layers with biome-specific blocks
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
                    5 => GetBlockId("dirt"),  // Swamp
                    6 => GetBlockId("grass"), // Taiga
                    _ => GetBlockId("grass")
                };
            }
            
            // Sub-surface layers with depth-based variation
            int depthFromSurface = terrainHeight - y;
            if (depthFromSurface <= 3)
            {
                return biome switch
                {
                    3 => GetBlockId("sandstone"), // Desert
                    5 => GetBlockId("clay"),      // Swamp
                    _ => GetBlockId("dirt")
                };
            }
            
            // Underground
            return GetBlockId("stone");
        }
        
        private void GenerateImprovedCaves(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Caves.EnableCaves) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Multi-layer cave generation
            GenerateCaveLayer(blocks, chunkX, chunkZ, _worldConfig.Caves.HorizontalFrequency, _worldConfig.Caves.VerticalFrequency, _worldConfig.Caves.Threshold);
            
            // Add secondary cave layer for more complexity
            GenerateCaveLayer(blocks, chunkX, chunkZ, _worldConfig.Caves.HorizontalFrequency * 2f, _worldConfig.Caves.VerticalFrequency * 1.5f, _worldConfig.Caves.Threshold * 0.8f);
            
            // Add lava and water in caves
            AddCaveLiquids(blocks, chunkX, chunkZ);
        }
        
        private void GenerateCaveLayer(int[,,] blocks, int chunkX, int chunkZ, float hFreq, float vFreq, float threshold)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    float worldX = (chunkX * chunkSize + x) * hFreq;
                    float worldY = y * vFreq;
                    
                    float caveValue = _caveNoise.GetNoise(worldX, worldY);
                    
                    if (caveValue > threshold)
                    {
                        blocks[x, y, z] = 0; // Air
                    }
                }
            }
        }
        
        private void AddCaveLiquids(int[,,] blocks, int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        if (blocks[x, y, z] != 0) continue; // Not air
                        
                        // Add lava at low levels
                        if (y < 10 && UnityEngine.Random.value < 0.05f)
                        {
                            blocks[x, y, z] = GetBlockId("lava");
                        }
                        // Add water at cave entrances
                        else if (y < _worldConfig.Terrain.SeaLevel && UnityEngine.Random.value < 0.03f)
                        {
                            blocks[x, y, z] = GetBlockId("water");
                        }
                    }
                }
            }
        }
        
        private void GenerateImprovedRivers(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableRivers) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            // Generate river map with improved flow
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;
                    
                    // Multi-octave river noise for more natural flow
                    float primaryRiver = _riverNoise.GetNoise(worldX, worldZ);
                    float secondaryRiver = _riverNoise.GetNoise(worldX * 0.5f, worldZ * 0.5f);
                    
                    float riverValue = (primaryRiver * 0.7f + secondaryRiver * 0.3f);
                    riverValue = Mathf.Abs(riverValue); // Make symmetrical
                    _riverMapCache[x, z] = riverValue;
                }
            }
            
            // Apply river generation with improved carving
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
                        // River banks with improved materials
                        GenerateRiverBanks(blocks, x, z, seaLevel, worldHeight);
                    }
                }
            }
        }
        
        private void GenerateRiverBanks(int[,,] blocks, int x, int z, int seaLevel, int worldHeight)
        {
            for (int y = seaLevel; y < worldHeight; y++)
            {
                if (blocks[x, y, z] != 0 && blocks[x, y, z] != GetBlockId("water"))
                {
                    // Replace with appropriate bank material
                    if (UnityEngine.Random.value < 0.6f)
                        blocks[x, y, z] = GetBlockId("sand");
                    else if (UnityEngine.Random.value < 0.8f)
                        blocks[x, y, z] = GetBlockId("gravel");
                    else
                        blocks[x, y, z] = GetBlockId("dirt");
                }
            }
        }
        
        private void GenerateImprovedLakes(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Water.EnableLakes) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate lake map with improved basin formation
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
            
            // Apply lake generation with improved basins
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeValue = _lakeMapCache[x, z];
                    
                    if (lakeValue > 0.7f) // Lake threshold
                    {
                        GenerateLakeBasin(blocks, x, z, worldHeight);
                    }
                }
            }
        }
        
        private void GenerateLakeBasin(int[,,] blocks, int x, int z, int worldHeight)
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
            
            // Create lake basin with variable depth
            int lakeDepth = UnityEngine.Random.Range(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth);
            int lakeBottom = Mathf.Max(terrainHeight - lakeDepth, 1);
            
            // Carve basin
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
        
        private void GenerateImprovedOres(int[,,] blocks, int chunkX, int chunkZ)
        {
            if (!_worldConfig.Ores.EnableOreGeneration) return;
            
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            // Generate ore veins with improved distribution
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
                    
                    // Generate improved ore vein
                    GenerateImprovedOreVein(blocks, veinX, veinY, veinZ, oreName, oreConfig.VeinSize);
                }
            }
        }
        
        private void GenerateImprovedOreVein(int[,,] blocks, int startX, int startY, int startZ, string oreName, int veinSize)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            // Improved vein generation with better shape
            Vector3Int currentPos = new Vector3Int(startX, startY, startZ);
            int remainingSize = veinSize;
            
            while (remainingSize > 0)
            {
                // Place ore at current position
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    // Only replace stone
                    if (blocks[currentPos.x, currentPos.y, currentPos.z] == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
                
                // Random walk with bias
                Vector3Int direction = GetRandomVeinDirection();
                currentPos += direction;
                remainingSize--;
                
                // Add some branching
                if (remainingSize > 0 && UnityEngine.Random.value < 0.2f)
                {
                    Vector3Int branchStart = currentPos;
                    int branchSize = UnityEngine.Random.Range(1, remainingSize / 2);
                    GenerateOreBranch(blocks, branchStart, oreName, branchSize);
                }
            }
        }
        
        private void GenerateOreBranch(int[,,] blocks, Vector3Int startPos, string oreName, int branchSize)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            int oreBlockId = GetBlockId(oreName);
            
            Vector3Int currentPos = startPos;
            
            for (int i = 0; i < branchSize; i++)
            {
                if (currentPos.x >= 0 && currentPos.x < chunkSize &&
                    currentPos.y >= 0 && currentPos.y < worldHeight &&
                    currentPos.z >= 0 && currentPos.z < chunkSize)
                {
                    if (blocks[currentPos.x, currentPos.y, currentPos.z] == GetBlockId("stone"))
                    {
                        blocks[currentPos.x, currentPos.y, currentPos.z] = oreBlockId;
                    }
                }
                
                Vector3Int direction = GetRandomVeinDirection();
                currentPos += direction;
            }
        }
        
        private Vector3Int GetRandomVeinDirection()
        {
            int dir = UnityEngine.Random.Range(0, 6);
            return dir switch
            {
                0 => Vector3Int.up,
                1 => Vector3Int.down,
                2 => Vector3Int.left,
                3 => Vector3Int.right,
                4 => Vector3Int.forward,
                5 => Vector3Int.back,
                _ => Vector3Int.up
            };
        }
        
        private void ApplySurfaceDetails(int[,,] blocks, int chunkX, int chunkZ)
        {
            int chunkSize = _worldConfig.ChunkSize;
            int worldHeight = _worldConfig.WorldHeight;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Find surface
                    int surfaceY = -1;
                    for (int y = worldHeight - 1; y >= 0; y--)
                    {
                        if (blocks[x, y, z] != 0)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY < 0) continue;
                    
                    int biome = _biomeMapCache[x, z];
                    float detail = _detailMapCache[x, z];
                    
                    // Apply biome-specific surface details
                    ApplyBiomeSurfaceDetails(blocks, x, z, surfaceY, biome, detail);
                }
            }
        }
        
        private void ApplyBiomeSurfaceDetails(int[,,] blocks, int x, int z, int surfaceY, int biome, float detail)
        {
            // Add surface features based on biome and detail noise
            switch (biome)
            {
                case 0: // Plains
                    if (detail > 0.7f && UnityEngine.Random.value < 0.1f)
                    {
                        // Add flowers
                        blocks[x, surfaceY + 1, z] = GetBlockId("flower");
                    }
                    break;
                    
                case 2: // Forest
                    if (UnityEngine.Random.value < 0.05f)
                    {
                        // Add trees (simplified - would need tree generation system)
                        blocks[x, surfaceY + 1, z] = GetBlockId("log");
                    }
                    break;
                    
                case 3: // Desert
                    if (detail > 0.6f && UnityEngine.Random.value < 0.05f)
                    {
                        // Add cacti
                        blocks[x, surfaceY + 1, z] = GetBlockId("cactus");
                    }
                    break;
            }
        }
        
        private int GetBlockId(string blockName)
        {
            return _blockDataManager.GetBlockId(blockName);
        }
    }
}
