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
            int minCaveY = Mathf.Clamp(_worldConfig.Caves.MinCaveHeight, 1, worldHeight - 1);
            int maxCaveY = Mathf.Clamp(_worldConfig.Caves.MaxCaveHeight, minCaveY, worldHeight - 1);
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = minCaveY; y <= maxCaveY; y++)
                    {
                        float worldX = (chunkX * chunkSize + x) * hFreq;
                        float worldZ = (chunkZ * chunkSize + z) * hFreq;
                        float worldY = y * vFreq;

                        float caveValue = _caveNoise.GetNoise(worldX, worldZ + worldY);
                        float moisture = Mathf.InverseLerp(_worldConfig.Terrain.BedrockLevel, _worldConfig.Terrain.SeaLevel, y);
                        float stabilityBias = Mathf.Lerp(_worldConfig.Caves.HydrologyStabilityWeight, _worldConfig.Caves.RoughnessStabilityWeight, moisture);
                        float adjustedThreshold = threshold + (1f - stabilityBias) * 0.1f;

                        if (caveValue > adjustedThreshold)
                        {
                            blocks[x, y, z] = 0; // Air
                        }
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
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    float warp = _riverNoise.GetNoise(worldX * _worldConfig.Water.HydrologyWarpFrequency, worldZ * _worldConfig.Water.HydrologyWarpFrequency);
                    float warpedX = worldX + warp * _worldConfig.Water.HydrologyWarpAmplitude;
                    float warpedZ = worldZ + warp * _worldConfig.Water.HydrologyWarpAmplitude;

                    float baseNoise = Mathf.Abs(_riverNoise.GetNoise(warpedX * _worldConfig.Water.RiverNoiseScale, warpedZ * _worldConfig.Water.RiverNoiseScale));
                    float intensity = Mathf.Clamp01(1f - baseNoise);

                    float slope = CalculateSlope(x, z);
                    float gradientPenalty = slope * _worldConfig.Water.RiverGradientPenalty * 0.5f;
                    float reliefPenalty = Mathf.Max(0f, ((_heightMapCache[x, z] * worldHeight) - _worldConfig.Water.GlobalWaterLevel) * _worldConfig.Water.RiverReliefPenaltyWeight * 0.01f);
                    float anisotropy = ComputeAnisotropy(x, z);
                    float flowAlignment = ComputeFlowAlignment(x, z);

                    intensity = intensity * (1f - gradientPenalty);
                    intensity = Mathf.Clamp01(intensity - reliefPenalty - anisotropy + flowAlignment);

                    _riverMapCache[x, z] = intensity > _worldConfig.Water.RiverCenterThreshold ? intensity : 0f;
                }
            }

            SmoothMask(_riverMapCache, _worldConfig.Water.RiverIntensitySmoothIterations, _worldConfig.Water.RiverIntensitySmoothBlend);
            BoostConfluences(_riverMapCache, _worldConfig.Water.RiverConfluenceBoost);
            SmoothMask(_riverMapCache, _worldConfig.Water.HydrologySeamRelaxIterations, _worldConfig.Water.HydrologySeamRelaxBlend);

            float centerCutoff = Mathf.Clamp01(1f - (_worldConfig.Water.RiverCenterThreshold * 40f));
            float bankCutoff = Mathf.Clamp01(centerCutoff - (_worldConfig.Water.RiverBankThreshold * 20f));

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float intensity = _riverMapCache[x, z];

                    if (intensity >= centerCutoff)
                    {
                        int carvedDepth = Mathf.Max(2, Mathf.RoundToInt(_worldConfig.Water.RiverDepth + intensity * _worldConfig.Water.RiverDepth * 0.5f));
                        for (int y = seaLevel; y < worldHeight; y++)
                        {
                            blocks[x, y, z] = 0;
                        }

                        for (int y = seaLevel - carvedDepth; y < seaLevel; y++)
                        {
                            if (y >= 0 && y < worldHeight)
                            {
                                blocks[x, y, z] = GetBlockId("water");
                            }
                        }
                    }
                    else if (intensity >= bankCutoff)
                    {
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
            int seaLevel = _worldConfig.Terrain.SeaLevel;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkX * chunkSize + x;
                    float worldZ = chunkZ * chunkSize + z;

                    float primary = _lakeNoise.GetNoise(worldX * 0.008f, worldZ * 0.008f);
                    float basin = _lakeNoise.GetNoise(worldX * 0.004f + 31f, worldZ * 0.004f + 17f);
                    float inflow = _riverMapCache[x, z] * _worldConfig.Water.LakeInflowBlendWeight;
                    float slope = CalculateSlope(x, z);
                    float rimPenalty = slope * _worldConfig.Lakes.ShorelineBlend * 0.05f;
                    float elevationBias = Mathf.Max(0f, _worldConfig.Water.GlobalWaterLevel - (_heightMapCache[x, z] * worldHeight)) * 0.0015f;
                    float weight = (primary * 0.6f) + (basin * 0.4f) + _worldConfig.Lakes.SpawnWeightBias + inflow + elevationBias - rimPenalty;

                    if (_riverMapCache[x, z] > 0f)
                    {
                        weight -= _riverMapCache[x, z] * _worldConfig.Lakes.RiverProximitySuppression * 0.5f;
                    }

                    float wetlandThreshold = _worldConfig.Lakes.WetlandSaturationThreshold - (inflow * 0.15f);
                    if (weight > wetlandThreshold && (_heightMapCache[x, z] * worldHeight) > seaLevel - _worldConfig.Lakes.MaxDepth)
                    {
                        _lakeMapCache[x, z] = Mathf.Clamp01(weight);
                    }
                    else
                    {
                        _lakeMapCache[x, z] = 0f;
                    }
                }
            }

            SmoothMask(_lakeMapCache, _worldConfig.Lakes.LakeBasinSmoothIterations, 0.55f);
            ApplyWetlandBuffer(_lakeMapCache, _worldConfig.Lakes.WetlandBufferRadius, _worldConfig.Lakes.ShorelineBlend);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float weight = _lakeMapCache[x, z];
                    if (weight <= 0f)
                    {
                        continue;
                    }

                    int terrainHeight = FindSurfaceHeight(blocks, x, z, worldHeight);
                    if (terrainHeight <= 0)
                    {
                        continue;
                    }

                    int lakeDepth = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(_worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth, weight)), _worldConfig.Lakes.MinDepth, _worldConfig.Lakes.MaxDepth);
                    int basinBottom = Mathf.Max(terrainHeight - lakeDepth, 1);

                    for (int y = basinBottom; y <= terrainHeight; y++)
                    {
                        blocks[x, y, z] = 0;
                    }

                    for (int y = basinBottom; y < basinBottom + lakeDepth - _worldConfig.Lakes.ShelfDepth; y++)
                    {
                        if (y >= 0 && y < worldHeight)
                        {
                            blocks[x, y, z] = GetBlockId("water");
                        }
                    }

                    int shorelineY = Math.Max(basinBottom - 1, 1);
                    if (shorelineY < worldHeight && blocks[x, shorelineY, z] != GetBlockId("water"))
                    {
                        blocks[x, shorelineY, z] = UnityEngine.Random.value < 0.5f ? GetBlockId("sand") : GetBlockId("clay");
                    }
                }
            }
        }
        
        private int FindSurfaceHeight(int[,,] blocks, int x, int z, int worldHeight)
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

        private float CalculateSlope(int x, int z)
        {
            int chunkSize = _worldConfig.ChunkSize;

            float hL = _heightMapCache[Mathf.Max(0, x - 1), z];
            float hR = _heightMapCache[Mathf.Min(chunkSize - 1, x + 1), z];
            float hD = _heightMapCache[x, Mathf.Max(0, z - 1)];
            float hU = _heightMapCache[x, Mathf.Min(chunkSize - 1, z + 1)];

            float dx = hR - hL;
            float dz = hU - hD;

            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private float ComputeAnisotropy(int x, int z)
        {
            int chunkSize = _worldConfig.ChunkSize;
            float left = _heightMapCache[Mathf.Max(0, x - 1), z];
            float right = _heightMapCache[Mathf.Min(chunkSize - 1, x + 1), z];
            float down = _heightMapCache[x, Mathf.Max(0, z - 1)];
            float up = _heightMapCache[x, Mathf.Min(chunkSize - 1, z + 1)];

            float slopeX = Mathf.Abs(right - left);
            float slopeZ = Mathf.Abs(up - down);
            float diff = Mathf.Abs(slopeX - slopeZ);
            float sum = Mathf.Max(0.0001f, slopeX + slopeZ);
            return Mathf.Clamp01(diff / sum) * _worldConfig.Water.RiverAnisotropyWeight;
        }

        private float ComputeFlowAlignment(int x, int z)
        {
            int chunkSize = _worldConfig.ChunkSize;
            float current = _heightMapCache[x, z];
            float east = _heightMapCache[Mathf.Min(chunkSize - 1, x + 1), z];
            float north = _heightMapCache[x, Mathf.Min(chunkSize - 1, z + 1)];

            float dx = current - east;
            float dz = current - north;
            float magnitude = Mathf.Sqrt(dx * dx + dz * dz);
            if (magnitude <= float.Epsilon)
            {
                return 0f;
            }

            float normalized = magnitude / (magnitude + 12f);
            return (1f - normalized) * _worldConfig.Water.RiverFlowAlignmentWeight;
        }

        private void SmoothMask(float[,] field, int iterations, float blend)
        {
            iterations = Math.Max(0, iterations);
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
                        buffer[x, z] = Mathf.Lerp(field[x, z], average, blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private void BoostConfluences(float[,] field, float confluenceBoost)
        {
            if (confluenceBoost <= 0f)
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
                    float boosted = center + average * confluenceBoost * 0.5f;
                    buffer[x, z] = Mathf.Clamp01(boosted);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private void ApplyWetlandBuffer(float[,] field, int radius, float shorelineBlend)
        {
            radius = Math.Max(0, radius);
            shorelineBlend = Mathf.Clamp01(shorelineBlend);
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

                            float distanceFalloff = 1f - (Mathf.Abs(dx) + Mathf.Abs(dz)) / (float)(radius + 1);
                            float influence = Mathf.Clamp(center * shorelineBlend * distanceFalloff, 0f, 1f);
                            buffer[nx, nz] = Mathf.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
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
