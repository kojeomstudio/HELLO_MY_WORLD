using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameServerApp.Models;
using GameServerApp.Utils;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced terrain generation pipeline with improved cave, river, and lake generation
    /// Implements realistic terrain features with configurable parameters
    /// </summary>
    public class EnhancedTerrainGenerationPipeline
    {
        private readonly WorldGenerationConfig _config;
        private readonly Noise _noiseGenerator;
        private readonly Dictionary<string, float> _biomeParameters;

        public EnhancedTerrainGenerationPipeline(WorldGenerationConfig config)
        {
            _config = config;
            _noiseGenerator = new Noise(config.WorldSeed);
            _biomeParameters = InitializeBiomeParameters();
        }

        /// <summary>
        /// Generates complete chunk with all terrain features
        /// </summary>
        public async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ);
            
            // Generate base terrain
            await GenerateBaseTerrainAsync(chunk);
            
            // Apply biome-specific modifications
            await ApplyBiomeModificationsAsync(chunk);
            
            // Generate caves
            if (_config.World.EnableCaves)
            {
                await GenerateCavesAsync(chunk);
            }
            
            // Generate rivers
            if (_config.World.EnableRivers)
            {
                await GenerateRiversAsync(chunk);
            }
            
            // Generate lakes
            if (_config.World.EnableLakes)
            {
                await GenerateLakesAsync(chunk);
            }
            
            // Generate ore deposits
            if (_config.World.EnableOreGeneration)
            {
                await GenerateOreDepositsAsync(chunk);
            }
            
            // Apply final smoothing and validation
            await FinalizeTerrainAsync(chunk);
            
            return chunk;
        }

        /// <summary>
        /// Generates base terrain using multi-octave noise
        /// </summary>
        private async Task GenerateBaseTerrainAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        // Multi-octave noise for realistic terrain
                        var height = GenerateTerrainHeight(worldX, worldZ);
                        var biome = DetermineBiome(worldX, worldZ, height);
                        
                        chunk.SetBlock(x, (int)height, z, GetBiomeSurfaceBlock(biome));
                        
                        // Fill below surface with appropriate blocks
                        FillUnderground(chunk, x, (int)height, z, biome);
                    }
                }
            });
        }

        /// <summary>
        /// Generates terrain height using multi-octave Perlin noise
        /// </summary>
        private float GenerateTerrainHeight(int worldX, int worldZ)
        {
            // Continental scale (large landmasses)
            var continental = _noiseGenerator.Perlin(worldX * 0.0005f, worldZ * 0.0005f) * 100f;
            
            // Mountain ranges
            var mountain = _noiseGenerator.Perlin(worldX * 0.002f, worldZ * 0.002f) * 50f;
            var mountainMask = Math.Max(0, _noiseGenerator.Perlin(worldX * 0.001f, worldZ * 0.001f));
            mountain *= mountainMask;
            
            // Hills and valleys
            var hills = _noiseGenerator.Perlin(worldX * 0.01f, worldZ * 0.01f) * 20f;
            
            // Fine detail
            var detail = _noiseGenerator.Perlin(worldX * 0.05f, worldZ * 0.05f) * 5f;
            
            // Combine all octaves
            var baseHeight = 60f + continental + mountain + hills + detail;
            
            // Apply ocean floor depth for water areas
            if (baseHeight < _config.World.SeaLevel)
            {
                var oceanDepth = (_config.World.SeaLevel - baseHeight) * 0.5f;
                baseHeight = _config.World.SeaLevel - oceanDepth;
            }
            
            return Math.Max(_config.World.MinHeight, Math.Min(_config.World.MaxHeight, baseHeight));
        }

        /// <summary>
        /// Determines biome based on position and climate parameters
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ, float height)
        {
            // Temperature gradient (equator to poles)
            var temperature = 1f - Math.Abs(worldZ / (float)(_config.World.WorldSize * 100)) * 2f;
            temperature += _noiseGenerator.Perlin(worldX * 0.003f, worldZ * 0.003f) * 0.3f;
            
            // Humidity based on distance from water and noise
            var humidity = _noiseGenerator.Perlin(worldX * 0.004f, worldZ * 0.004f) * 0.5f + 0.5f;
            
            // Elevation affects temperature and humidity
            if (height > _config.World.SnowLineHeight)
            {
                temperature -= 0.5f;
                humidity *= 0.7f;
            }
            
            // Determine biome based on climate parameters
            if (height < _config.World.SeaLevel - 2)
            {
                return BiomeType.Ocean;
            }
            
            if (temperature < 0.2f)
            {
                return height > _config.World.SnowLineHeight ? BiomeType.SnowyMountains : BiomeType.Tundra;
            }
            
            if (temperature < 0.4f)
            {
                return humidity > 0.6f ? BiomeType.Taiga : BiomeType.Plains;
            }
            
            if (temperature < 0.7f)
            {
                return humidity > 0.7f ? BiomeType.Swamp : BiomeType.Forest;
            }
            
            if (humidity > 0.8f)
            {
                return BiomeType.Jungle;
            }
            
            if (temperature > 0.8f)
            {
                return BiomeType.Desert;
            }
            
            return BiomeType.Plains;
        }

        /// <summary>
        /// Generates improved cave system using cellular automata and noise
        /// </summary>
        private async Task GenerateCavesAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                var caveMap = new bool[ChunkData.ChunkSize, ChunkData.ChunkSize, ChunkData.ChunkHeight];
                
                // Generate cave seeds using 3D noise
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                        {
                            var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                            var worldY = y;
                            var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                            
                            // 3D noise for cave generation
                            var caveNoise = _noiseGenerator.Perlin3D(
                                worldX * 0.03f, 
                                worldY * 0.03f, 
                                worldZ * 0.03f);
                            
                            // Cave threshold with depth variation
                            var threshold = 0.6f - (worldY - _config.World.SeaLevel) * 0.001f;
                            
                            caveMap[x, z, y - _config.World.MinHeight] = caveNoise > threshold;
                        }
                    }
                }
                
                // Apply cellular automata smoothing for more natural cave shapes
                ApplyCaveSmoothing(caveMap);
                
                // Carve caves into terrain
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                        {
                            if (caveMap[x, z, y - _config.World.MinHeight])
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Applies cellular automata smoothing to cave systems
        /// </summary>
        private void ApplyCaveSmoothing(bool[,,] caveMap)
        {
            var iterations = 2;
            var sizeX = caveMap.GetLength(0);
            var sizeZ = caveMap.GetLength(1);
            var sizeY = caveMap.GetLength(2);
            
            for (int iter = 0; iter < iterations; iter++)
            {
                var newMap = new bool[sizeX, sizeZ, sizeY];
                
                for (int x = 1; x < sizeX - 1; x++)
                {
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        for (int y = 1; y < sizeY - 1; y++)
                        {
                            var neighbors = CountCaveNeighbors(caveMap, x, z, y);
                            newMap[x, z, y] = neighbors >= 5;
                        }
                    }
                }
                
                caveMap = newMap;
            }
        }

        /// <summary>
        /// Counts cave neighbors for cellular automata
        /// </summary>
        private int CountCaveNeighbors(bool[,,] caveMap, int x, int z, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dz == 0 && dy == 0) continue;
                        
                        var nx = x + dx;
                        var ny = y + dy;
                        var nz = z + dz;
                        
                        if (nx >= 0 && nx < caveMap.GetLength(0) &&
                            ny >= 0 && ny < caveMap.GetLength(2) &&
                            nz >= 0 && nz < caveMap.GetLength(1))
                        {
                            if (caveMap[nx, nz, ny]) count++;
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Generates realistic river systems using watershed simulation
        /// </summary>
        private async Task GenerateRiversAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Generate river paths using noise and flow accumulation
                var riverMap = new float[ChunkData.ChunkSize, ChunkData.ChunkSize];
                
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        // River flow direction noise
                        var riverNoise = _noiseGenerator.Perlin(worldX * 0.008f, worldZ * 0.008f);
                        var flowNoise = _noiseGenerator.Perlin(worldX * 0.02f, worldZ * 0.02f);
                        
                        // Combine to determine river presence
                        riverMap[x, z] = Math.Max(0, riverNoise * 0.7f + flowNoise * 0.3f - 0.3f);
                    }
                }
                
                // Carve river channels
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        if (riverMap[x, z] > 0)
                        {
                            var riverWidth = (int)(2 + riverMap[x, z] * 3);
                            var riverDepth = (int)(1 + riverMap[x, z] * 2);
                            
                            CarveRiverChannel(chunk, x, z, riverWidth, riverDepth);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Carves river channel into terrain
        /// </summary>
        private void CarveRiverChannel(ChunkData chunk, int centerX, int centerZ, int width, int depth)
        {
            var seaLevel = _config.World.SeaLevel;
            
            for (int x = Math.Max(0, centerX - width); x < Math.Min(ChunkData.ChunkSize, centerX + width); x++)
            {
                for (int z = Math.Max(0, centerZ - width); z < Math.Min(ChunkData.ChunkSize, centerZ + width); z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= width)
                    {
                        // Carve down to sea level or specified depth
                        var targetY = Math.Min(seaLevel - depth, GetSurfaceHeight(chunk, x, z));
                        
                        for (int y = targetY + 1; y < _config.World.MaxHeight; y++)
                        {
                            if (chunk.GetBlock(x, y, z) != BlockType.Air)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }
                        
                        // River bed
                        if (targetY < seaLevel)
                        {
                            chunk.SetBlock(x, targetY, z, BlockType.Sand);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates realistic lakes with varied sizes and depths
        /// </summary>
        private async Task GenerateLakesAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Generate lake centers using noise
                var lakeCenters = new List<(int x, int z, float size)>();
                
                for (int x = 0; x < ChunkData.ChunkSize; x += 4)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z += 4)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        var lakeNoise = _noiseGenerator.Perlin(worldX * 0.01f, worldZ * 0.01f);
                        if (lakeNoise > 0.4f)
                        {
                            var size = (lakeNoise - 0.4f) * 2.5f;
                            lakeCenters.Add((x, z, size));
                        }
                    }
                }
                
                // Generate each lake
                foreach (var (centerX, centerZ, size) in lakeCenters)
                {
                    GenerateLake(chunk, centerX, centerZ, size);
                }
            });
        }

        /// <summary>
        /// Generates a single lake
        /// </summary>
        private void GenerateLake(ChunkData chunk, int centerX, int centerZ, float size)
        {
            var radius = (int)(2 + size * 3);
            var depth = (int)(1 + size * 2);
            var seaLevel = _config.World.SeaLevel;
            
            for (int x = Math.Max(0, centerX - radius); x < Math.Min(ChunkData.ChunkSize, centerX + radius); x++)
            {
                for (int z = Math.Max(0, centerZ - radius); z < Math.Min(ChunkData.ChunkSize, centerZ + radius); z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    
                    if (distance <= radius)
                    {
                        var surfaceY = GetSurfaceHeight(chunk, x, z);
                        var lakeBottom = Math.Min(seaLevel - depth, surfaceY);
                        
                        // Fill lake with water
                        for (int y = lakeBottom; y <= surfaceY; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                        
                        // Add sand/gravel around shores
                        if (distance > radius - 2 && surfaceY > lakeBottom)
                        {
                            chunk.SetBlock(x, lakeBottom, z, BlockType.Sand);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates ore deposits with realistic distribution
        /// </summary>
        private async Task GenerateOreDepositsAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                foreach (var oreConfig in _config.World.OreDeposits)
                {
                    GenerateOreType(chunk, oreConfig);
                }
            });
        }

        /// <summary>
        /// Generates deposits for a specific ore type
        /// </summary>
        private void GenerateOreType(ChunkData chunk, OreDepositConfig oreConfig)
        {
            var random = new Random(_config.World.WorldSeed + chunk.ChunkX * 73856093 ^ chunk.ChunkZ * 19349663);
            
            // Determine number of veins in this chunk
            var veinCount = random.NextDouble() < oreConfig.ChunkSpawnChance ? 
                random.Next(oreConfig.MinVeinsPerChunk, oreConfig.MaxVeinsPerChunk + 1) : 0;
            
            for (int i = 0; i < veinCount; i++)
            {
                // Generate vein parameters
                var veinX = random.Next(ChunkData.ChunkSize);
                var veinZ = random.Next(ChunkData.ChunkSize);
                var veinY = random.Next(oreConfig.MinY, oreConfig.MaxY + 1);
                var veinSize = random.Next(oreConfig.MinVeinSize, oreConfig.MaxVeinSize + 1);
                
                // Generate vein
                GenerateOreVein(chunk, veinX, veinY, veinZ, veinSize, oreConfig.BlockType);
            }
        }

        /// <summary>
        /// Generates a single ore vein
        /// </summary>
        private void GenerateOreVein(ChunkData chunk, int startX, int startY, int startZ, int size, BlockType oreType)
        {
            var random = new Random(_config.World.WorldSeed + startX * 73856093 ^ startZ * 19349663 ^ startY);
            var positions = new Queue<(int x, int y, int z)>();
            positions.Enqueue((startX, startY, startZ));
            
            var visited = new HashSet<(int, int, int)>();
            var placed = 0;
            
            while (positions.Count > 0 && placed < size)
            {
                var (x, y, z) = positions.Dequeue();
                var key = (x, y, z);
                
                if (visited.Contains(key)) continue;
                if (x < 0 || x >= ChunkData.ChunkSize || 
                    y < _config.World.MinHeight || y >= _config.World.MaxHeight || 
                    z < 0 || z >= ChunkData.ChunkSize) continue;
                
                visited.Add(key);
                
                // Only place ore in appropriate host blocks
                var currentBlock = chunk.GetBlock(x, y, z);
                if (IsValidOreHost(currentBlock))
                {
                    chunk.SetBlock(x, y, z, oreType);
                    placed++;
                }
                
                // Add neighbors for vein growth
                if (random.NextDouble() < 0.7) // 70% chance to continue vein
                {
                    positions.Enqueue((x + 1, y, z));
                    positions.Enqueue((x - 1, y, z));
                    positions.Enqueue((x, y + 1, z));
                    positions.Enqueue((x, y - 1, z));
                    positions.Enqueue((x, y, z + 1));
                    positions.Enqueue((x, y, z - 1));
                }
            }
        }

        /// <summary>
        /// Checks if a block can host ore deposits
        /// </summary>
        private bool IsValidOreHost(BlockType block)
        {
            return block == BlockType.Stone || 
                   block == BlockType.Dirt || 
                   block == BlockType.Grass || 
                   block == BlockType.Sand;
        }

        /// <summary>
        /// Applies biome-specific modifications to terrain
        /// </summary>
        private async Task ApplyBiomeModificationsAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        var height = GetSurfaceHeight(chunk, x, z);
                        var biome = DetermineBiome(worldX, worldZ, height);
                        
                        ApplyBiomeFeatures(chunk, x, height, z, biome);
                    }
                }
            });
        }

        /// <summary>
        /// Applies biome-specific features
        /// </summary>
        private void ApplyBiomeFeatures(ChunkData chunk, int x, int y, int z, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    // Add cacti and dead bushes
                    if (ShouldPlaceFeature(0.02f))
                    {
                        chunk.SetBlock(x, y + 1, z, BlockType.Cactus);
                    }
                    break;
                    
                case BiomeType.Forest:
                    // Add trees
                    if (ShouldPlaceFeature(0.1f))
                    {
                        GenerateTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Jungle:
                    // Add jungle trees and vegetation
                    if (ShouldPlaceFeature(0.15f))
                    {
                        GenerateJungleTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Taiga:
                    // Add pine trees
                    if (ShouldPlaceFeature(0.08f))
                    {
                        GeneratePineTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Swamp:
                    // Add swamp vegetation
                    if (ShouldPlaceFeature(0.05f))
                    {
                        chunk.SetBlock(x, y + 1, z, BlockType.TallGrass);
                    }
                    break;
            }
        }

        /// <summary>
        /// Finalizes terrain with smoothing and validation
        /// </summary>
        private async Task FinalizeTerrainAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Apply smoothing to reduce harsh transitions
                ApplyTerrainSmoothing(chunk);
                
                // Validate chunk integrity
                ValidateChunk(chunk);
            });
        }

        /// <summary>
        /// Applies smoothing to terrain transitions
        /// </summary>
        private void ApplyTerrainSmoothing(ChunkData chunk)
        {
            // Simple smoothing pass to reduce 1-block cliffs
            for (int x = 1; x < ChunkData.ChunkSize - 1; x++)
            {
                for (int z = 1; z < ChunkData.ChunkSize - 1; z++)
                {
                    for (int y = _config.World.MinHeight + 1; y < _config.World.MaxHeight - 1; y++)
                    {
                        var current = chunk.GetBlock(x, y, z);
                        if (current == BlockType.Air) continue;
                        
                        // Check for unsupported blocks
                        var below = chunk.GetBlock(x, y - 1, z);
                        if (below == BlockType.Air && y > _config.World.MinHeight + 5)
                        {
                            // Check if there's support nearby
                            var hasSupport = chunk.GetBlock(x - 1, y - 1, z) != BlockType.Air ||
                                           chunk.GetBlock(x + 1, y - 1, z) != BlockType.Air ||
                                           chunk.GetBlock(x, y - 1, z - 1) != BlockType.Air ||
                                           chunk.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                            
                            if (!hasSupport)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates chunk for any issues
        /// </summary>
        private void ValidateChunk(ChunkData chunk)
        {
            // Ensure water sources are valid
            for (int x = 0; x < ChunkData.ChunkSize; x++)
            {
                for (int z = 0; z < ChunkData.ChunkSize; z++)
                {
                    for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Water)
                        {
                            var below = chunk.GetBlock(x, y - 1, z);
                            if (below == BlockType.Air && y > _config.World.MinHeight)
                            {
                                // Remove floating water
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        // Helper methods
        private Dictionary<string, float> InitializeBiomeParameters()
        {
            return new Dictionary<string, float>
            {
                ["temperature_variation"] = 0.3f,
                ["humidity_variation"] = 0.4f,
                ["elevation_factor"] = 0.5f
            };
        }

        private BlockType GetBiomeSurfaceBlock(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Taiga => BlockType.Grass,
                BiomeType.Swamp => BlockType.Grass,
                BiomeType.Jungle => BlockType.Grass,
                BiomeType.Tundra => BlockType.SnowBlock,
                BiomeType.SnowyMountains => BlockType.SnowBlock,
                _ => BlockType.Grass
            };
        }

        private void FillUnderground(ChunkData chunk, int x, int surfaceY, int z, BiomeType biome)
        {
            for (int y = _config.World.MinHeight; y < surfaceY; y++)
            {
                var depth = surfaceY - y;
                BlockType block;
                
                if (depth < 3)
                {
                    block = BlockType.Dirt;
                }
                else if (biome == BiomeType.Desert && depth < 8)
                {
                    block = BlockType.Sandstone;
                }
                else
                {
                    block = BlockType.Stone;
                }
                
                chunk.SetBlock(x, y, z, block);
            }
        }

        private int GetSurfaceHeight(ChunkData chunk, int x, int z)
        {
            for (int y = _config.World.MaxHeight - 1; y >= _config.World.MinHeight; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }
            return _config.World.MinHeight;
        }

        private bool ShouldPlaceFeature(float probability)
        {
            return new Random().NextDouble() < probability;
        }

        private void GenerateTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple oak tree generation
            var height = 4 + new Random().Next(3);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= 3)
                        {
                            chunk.SetBlock(x + dx, y + height + dy, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }

        private void GenerateJungleTree(ChunkData chunk, int x, int y, int z)
        {
            // Jungle tree with larger canopy
            var height = 6 + new Random().Next(4);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Larger leaf canopy
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    for (int dz = -3; dz <= 3; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= 4)
                        {
                            chunk.SetBlock(x + dx, y + height + dy, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }

        private void GeneratePineTree(ChunkData chunk, int x, int y, int z)
        {
            // Pine tree - taller and thinner
            var height = 7 + new Random().Next(3);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Cone-shaped leaves
            for (int i = height - 2; i <= height + 1; i++)
            {
                var radius = height - i + 1;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= radius)
                        {
                            chunk.SetBlock(x + dx, y + i, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }
    }
}using System.Collections.Generic;
using System.Threading.Tasks;
using GameServerApp.Models;
using GameServerApp.Utils;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced terrain generation pipeline with improved cave, river, and lake generation
    /// Implements realistic terrain features with configurable parameters
    /// </summary>
    public class EnhancedTerrainGenerationPipeline
    {
        private readonly WorldGenerationConfig _config;
        private readonly Noise _noiseGenerator;
        private readonly Dictionary<string, float> _biomeParameters;

        public EnhancedTerrainGenerationPipeline(WorldGenerationConfig config)
        {
            _config = config;
            _noiseGenerator = new Noise(config.WorldSeed);
            _biomeParameters = InitializeBiomeParameters();
        }

        /// <summary>
        /// Generates complete chunk with all terrain features
        /// </summary>
        public async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ);
            
            // Generate base terrain
            await GenerateBaseTerrainAsync(chunk);
            
            // Apply biome-specific modifications
            await ApplyBiomeModificationsAsync(chunk);
            
            // Generate caves
            if (_config.World.EnableCaves)
            {
                await GenerateCavesAsync(chunk);
            }
            
            // Generate rivers
            if (_config.World.EnableRivers)
            {
                await GenerateRiversAsync(chunk);
            }
            
            // Generate lakes
            if (_config.World.EnableLakes)
            {
                await GenerateLakesAsync(chunk);
            }
            
            // Generate ore deposits
            if (_config.World.EnableOreGeneration)
            {
                await GenerateOreDepositsAsync(chunk);
            }
            
            // Apply final smoothing and validation
            await FinalizeTerrainAsync(chunk);
            
            return chunk;
        }

        /// <summary>
        /// Generates base terrain using multi-octave noise
        /// </summary>
        private async Task GenerateBaseTerrainAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        // Multi-octave noise for realistic terrain
                        var height = GenerateTerrainHeight(worldX, worldZ);
                        var biome = DetermineBiome(worldX, worldZ, height);
                        
                        chunk.SetBlock(x, (int)height, z, GetBiomeSurfaceBlock(biome));
                        
                        // Fill below surface with appropriate blocks
                        FillUnderground(chunk, x, (int)height, z, biome);
                    }
                }
            });
        }

        /// <summary>
        /// Generates terrain height using multi-octave Perlin noise
        /// </summary>
        private float GenerateTerrainHeight(int worldX, int worldZ)
        {
            // Continental scale (large landmasses)
            var continental = _noiseGenerator.Perlin(worldX * 0.0005f, worldZ * 0.0005f) * 100f;
            
            // Mountain ranges
            var mountain = _noiseGenerator.Perlin(worldX * 0.002f, worldZ * 0.002f) * 50f;
            var mountainMask = Math.Max(0, _noiseGenerator.Perlin(worldX * 0.001f, worldZ * 0.001f));
            mountain *= mountainMask;
            
            // Hills and valleys
            var hills = _noiseGenerator.Perlin(worldX * 0.01f, worldZ * 0.01f) * 20f;
            
            // Fine detail
            var detail = _noiseGenerator.Perlin(worldX * 0.05f, worldZ * 0.05f) * 5f;
            
            // Combine all octaves
            var baseHeight = 60f + continental + mountain + hills + detail;
            
            // Apply ocean floor depth for water areas
            if (baseHeight < _config.World.SeaLevel)
            {
                var oceanDepth = (_config.World.SeaLevel - baseHeight) * 0.5f;
                baseHeight = _config.World.SeaLevel - oceanDepth;
            }
            
            return Math.Max(_config.World.MinHeight, Math.Min(_config.World.MaxHeight, baseHeight));
        }

        /// <summary>
        /// Determines biome based on position and climate parameters
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ, float height)
        {
            // Temperature gradient (equator to poles)
            var temperature = 1f - Math.Abs(worldZ / (float)(_config.World.WorldSize * 100)) * 2f;
            temperature += _noiseGenerator.Perlin(worldX * 0.003f, worldZ * 0.003f) * 0.3f;
            
            // Humidity based on distance from water and noise
            var humidity = _noiseGenerator.Perlin(worldX * 0.004f, worldZ * 0.004f) * 0.5f + 0.5f;
            
            // Elevation affects temperature and humidity
            if (height > _config.World.SnowLineHeight)
            {
                temperature -= 0.5f;
                humidity *= 0.7f;
            }
            
            // Determine biome based on climate parameters
            if (height < _config.World.SeaLevel - 2)
            {
                return BiomeType.Ocean;
            }
            
            if (temperature < 0.2f)
            {
                return height > _config.World.SnowLineHeight ? BiomeType.SnowyMountains : BiomeType.Tundra;
            }
            
            if (temperature < 0.4f)
            {
                return humidity > 0.6f ? BiomeType.Taiga : BiomeType.Plains;
            }
            
            if (temperature < 0.7f)
            {
                return humidity > 0.7f ? BiomeType.Swamp : BiomeType.Forest;
            }
            
            if (humidity > 0.8f)
            {
                return BiomeType.Jungle;
            }
            
            if (temperature > 0.8f)
            {
                return BiomeType.Desert;
            }
            
            return BiomeType.Plains;
        }

        /// <summary>
        /// Generates improved cave system using cellular automata and noise
        /// </summary>
        private async Task GenerateCavesAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                var caveMap = new bool[ChunkData.ChunkSize, ChunkData.ChunkSize, ChunkData.ChunkHeight];
                
                // Generate cave seeds using 3D noise
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                        {
                            var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                            var worldY = y;
                            var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                            
                            // 3D noise for cave generation
                            var caveNoise = _noiseGenerator.Perlin3D(
                                worldX * 0.03f, 
                                worldY * 0.03f, 
                                worldZ * 0.03f);
                            
                            // Cave threshold with depth variation
                            var threshold = 0.6f - (worldY - _config.World.SeaLevel) * 0.001f;
                            
                            caveMap[x, z, y - _config.World.MinHeight] = caveNoise > threshold;
                        }
                    }
                }
                
                // Apply cellular automata smoothing for more natural cave shapes
                ApplyCaveSmoothing(caveMap);
                
                // Carve caves into terrain
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                        {
                            if (caveMap[x, z, y - _config.World.MinHeight])
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Applies cellular automata smoothing to cave systems
        /// </summary>
        private void ApplyCaveSmoothing(bool[,,] caveMap)
        {
            var iterations = 2;
            var sizeX = caveMap.GetLength(0);
            var sizeZ = caveMap.GetLength(1);
            var sizeY = caveMap.GetLength(2);
            
            for (int iter = 0; iter < iterations; iter++)
            {
                var newMap = new bool[sizeX, sizeZ, sizeY];
                
                for (int x = 1; x < sizeX - 1; x++)
                {
                    for (int z = 1; z < sizeZ - 1; z++)
                    {
                        for (int y = 1; y < sizeY - 1; y++)
                        {
                            var neighbors = CountCaveNeighbors(caveMap, x, z, y);
                            newMap[x, z, y] = neighbors >= 5;
                        }
                    }
                }
                
                caveMap = newMap;
            }
        }

        /// <summary>
        /// Counts cave neighbors for cellular automata
        /// </summary>
        private int CountCaveNeighbors(bool[,,] caveMap, int x, int z, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dz == 0 && dy == 0) continue;
                        
                        var nx = x + dx;
                        var ny = y + dy;
                        var nz = z + dz;
                        
                        if (nx >= 0 && nx < caveMap.GetLength(0) &&
                            ny >= 0 && ny < caveMap.GetLength(2) &&
                            nz >= 0 && nz < caveMap.GetLength(1))
                        {
                            if (caveMap[nx, nz, ny]) count++;
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Generates realistic river systems using watershed simulation
        /// </summary>
        private async Task GenerateRiversAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Generate river paths using noise and flow accumulation
                var riverMap = new float[ChunkData.ChunkSize, ChunkData.ChunkSize];
                
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        // River flow direction noise
                        var riverNoise = _noiseGenerator.Perlin(worldX * 0.008f, worldZ * 0.008f);
                        var flowNoise = _noiseGenerator.Perlin(worldX * 0.02f, worldZ * 0.02f);
                        
                        // Combine to determine river presence
                        riverMap[x, z] = Math.Max(0, riverNoise * 0.7f + flowNoise * 0.3f - 0.3f);
                    }
                }
                
                // Carve river channels
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        if (riverMap[x, z] > 0)
                        {
                            var riverWidth = (int)(2 + riverMap[x, z] * 3);
                            var riverDepth = (int)(1 + riverMap[x, z] * 2);
                            
                            CarveRiverChannel(chunk, x, z, riverWidth, riverDepth);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Carves river channel into terrain
        /// </summary>
        private void CarveRiverChannel(ChunkData chunk, int centerX, int centerZ, int width, int depth)
        {
            var seaLevel = _config.World.SeaLevel;
            
            for (int x = Math.Max(0, centerX - width); x < Math.Min(ChunkData.ChunkSize, centerX + width); x++)
            {
                for (int z = Math.Max(0, centerZ - width); z < Math.Min(ChunkData.ChunkSize, centerZ + width); z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= width)
                    {
                        // Carve down to sea level or specified depth
                        var targetY = Math.Min(seaLevel - depth, GetSurfaceHeight(chunk, x, z));
                        
                        for (int y = targetY + 1; y < _config.World.MaxHeight; y++)
                        {
                            if (chunk.GetBlock(x, y, z) != BlockType.Air)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }
                        
                        // River bed
                        if (targetY < seaLevel)
                        {
                            chunk.SetBlock(x, targetY, z, BlockType.Sand);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates realistic lakes with varied sizes and depths
        /// </summary>
        private async Task GenerateLakesAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Generate lake centers using noise
                var lakeCenters = new List<(int x, int z, float size)>();
                
                for (int x = 0; x < ChunkData.ChunkSize; x += 4)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z += 4)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        
                        var lakeNoise = _noiseGenerator.Perlin(worldX * 0.01f, worldZ * 0.01f);
                        if (lakeNoise > 0.4f)
                        {
                            var size = (lakeNoise - 0.4f) * 2.5f;
                            lakeCenters.Add((x, z, size));
                        }
                    }
                }
                
                // Generate each lake
                foreach (var (centerX, centerZ, size) in lakeCenters)
                {
                    GenerateLake(chunk, centerX, centerZ, size);
                }
            });
        }

        /// <summary>
        /// Generates a single lake
        /// </summary>
        private void GenerateLake(ChunkData chunk, int centerX, int centerZ, float size)
        {
            var radius = (int)(2 + size * 3);
            var depth = (int)(1 + size * 2);
            var seaLevel = _config.World.SeaLevel;
            
            for (int x = Math.Max(0, centerX - radius); x < Math.Min(ChunkData.ChunkSize, centerX + radius); x++)
            {
                for (int z = Math.Max(0, centerZ - radius); z < Math.Min(ChunkData.ChunkSize, centerZ + radius); z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    
                    if (distance <= radius)
                    {
                        var surfaceY = GetSurfaceHeight(chunk, x, z);
                        var lakeBottom = Math.Min(seaLevel - depth, surfaceY);
                        
                        // Fill lake with water
                        for (int y = lakeBottom; y <= surfaceY; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                        
                        // Add sand/gravel around shores
                        if (distance > radius - 2 && surfaceY > lakeBottom)
                        {
                            chunk.SetBlock(x, lakeBottom, z, BlockType.Sand);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates ore deposits with realistic distribution
        /// </summary>
        private async Task GenerateOreDepositsAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                foreach (var oreConfig in _config.World.OreDeposits)
                {
                    GenerateOreType(chunk, oreConfig);
                }
            });
        }

        /// <summary>
        /// Generates deposits for a specific ore type
        /// </summary>
        private void GenerateOreType(ChunkData chunk, OreDepositConfig oreConfig)
        {
            var random = new Random(_config.World.WorldSeed + chunk.ChunkX * 73856093 ^ chunk.ChunkZ * 19349663);
            
            // Determine number of veins in this chunk
            var veinCount = random.NextDouble() < oreConfig.ChunkSpawnChance ? 
                random.Next(oreConfig.MinVeinsPerChunk, oreConfig.MaxVeinsPerChunk + 1) : 0;
            
            for (int i = 0; i < veinCount; i++)
            {
                // Generate vein parameters
                var veinX = random.Next(ChunkData.ChunkSize);
                var veinZ = random.Next(ChunkData.ChunkSize);
                var veinY = random.Next(oreConfig.MinY, oreConfig.MaxY + 1);
                var veinSize = random.Next(oreConfig.MinVeinSize, oreConfig.MaxVeinSize + 1);
                
                // Generate vein
                GenerateOreVein(chunk, veinX, veinY, veinZ, veinSize, oreConfig.BlockType);
            }
        }

        /// <summary>
        /// Generates a single ore vein
        /// </summary>
        private void GenerateOreVein(ChunkData chunk, int startX, int startY, int startZ, int size, BlockType oreType)
        {
            var random = new Random(_config.World.WorldSeed + startX * 73856093 ^ startZ * 19349663 ^ startY);
            var positions = new Queue<(int x, int y, int z)>();
            positions.Enqueue((startX, startY, startZ));
            
            var visited = new HashSet<(int, int, int)>();
            var placed = 0;
            
            while (positions.Count > 0 && placed < size)
            {
                var (x, y, z) = positions.Dequeue();
                var key = (x, y, z);
                
                if (visited.Contains(key)) continue;
                if (x < 0 || x >= ChunkData.ChunkSize || 
                    y < _config.World.MinHeight || y >= _config.World.MaxHeight || 
                    z < 0 || z >= ChunkData.ChunkSize) continue;
                
                visited.Add(key);
                
                // Only place ore in appropriate host blocks
                var currentBlock = chunk.GetBlock(x, y, z);
                if (IsValidOreHost(currentBlock))
                {
                    chunk.SetBlock(x, y, z, oreType);
                    placed++;
                }
                
                // Add neighbors for vein growth
                if (random.NextDouble() < 0.7) // 70% chance to continue vein
                {
                    positions.Enqueue((x + 1, y, z));
                    positions.Enqueue((x - 1, y, z));
                    positions.Enqueue((x, y + 1, z));
                    positions.Enqueue((x, y - 1, z));
                    positions.Enqueue((x, y, z + 1));
                    positions.Enqueue((x, y, z - 1));
                }
            }
        }

        /// <summary>
        /// Checks if a block can host ore deposits
        /// </summary>
        private bool IsValidOreHost(BlockType block)
        {
            return block == BlockType.Stone || 
                   block == BlockType.Dirt || 
                   block == BlockType.Grass || 
                   block == BlockType.Sand;
        }

        /// <summary>
        /// Applies biome-specific modifications to terrain
        /// </summary>
        private async Task ApplyBiomeModificationsAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                for (int x = 0; x < ChunkData.ChunkSize; x++)
                {
                    for (int z = 0; z < ChunkData.ChunkSize; z++)
                    {
                        var worldX = chunk.ChunkX * ChunkData.ChunkSize + x;
                        var worldZ = chunk.ChunkZ * ChunkData.ChunkSize + z;
                        var height = GetSurfaceHeight(chunk, x, z);
                        var biome = DetermineBiome(worldX, worldZ, height);
                        
                        ApplyBiomeFeatures(chunk, x, height, z, biome);
                    }
                }
            });
        }

        /// <summary>
        /// Applies biome-specific features
        /// </summary>
        private void ApplyBiomeFeatures(ChunkData chunk, int x, int y, int z, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    // Add cacti and dead bushes
                    if (ShouldPlaceFeature(0.02f))
                    {
                        chunk.SetBlock(x, y + 1, z, BlockType.Cactus);
                    }
                    break;
                    
                case BiomeType.Forest:
                    // Add trees
                    if (ShouldPlaceFeature(0.1f))
                    {
                        GenerateTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Jungle:
                    // Add jungle trees and vegetation
                    if (ShouldPlaceFeature(0.15f))
                    {
                        GenerateJungleTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Taiga:
                    // Add pine trees
                    if (ShouldPlaceFeature(0.08f))
                    {
                        GeneratePineTree(chunk, x, y + 1, z);
                    }
                    break;
                    
                case BiomeType.Swamp:
                    // Add swamp vegetation
                    if (ShouldPlaceFeature(0.05f))
                    {
                        chunk.SetBlock(x, y + 1, z, BlockType.TallGrass);
                    }
                    break;
            }
        }

        /// <summary>
        /// Finalizes terrain with smoothing and validation
        /// </summary>
        private async Task FinalizeTerrainAsync(ChunkData chunk)
        {
            await Task.Run(() =>
            {
                // Apply smoothing to reduce harsh transitions
                ApplyTerrainSmoothing(chunk);
                
                // Validate chunk integrity
                ValidateChunk(chunk);
            });
        }

        /// <summary>
        /// Applies smoothing to terrain transitions
        /// </summary>
        private void ApplyTerrainSmoothing(ChunkData chunk)
        {
            // Simple smoothing pass to reduce 1-block cliffs
            for (int x = 1; x < ChunkData.ChunkSize - 1; x++)
            {
                for (int z = 1; z < ChunkData.ChunkSize - 1; z++)
                {
                    for (int y = _config.World.MinHeight + 1; y < _config.World.MaxHeight - 1; y++)
                    {
                        var current = chunk.GetBlock(x, y, z);
                        if (current == BlockType.Air) continue;
                        
                        // Check for unsupported blocks
                        var below = chunk.GetBlock(x, y - 1, z);
                        if (below == BlockType.Air && y > _config.World.MinHeight + 5)
                        {
                            // Check if there's support nearby
                            var hasSupport = chunk.GetBlock(x - 1, y - 1, z) != BlockType.Air ||
                                           chunk.GetBlock(x + 1, y - 1, z) != BlockType.Air ||
                                           chunk.GetBlock(x, y - 1, z - 1) != BlockType.Air ||
                                           chunk.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                            
                            if (!hasSupport)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates chunk for any issues
        /// </summary>
        private void ValidateChunk(ChunkData chunk)
        {
            // Ensure water sources are valid
            for (int x = 0; x < ChunkData.ChunkSize; x++)
            {
                for (int z = 0; z < ChunkData.ChunkSize; z++)
                {
                    for (int y = _config.World.MinHeight; y < _config.World.MaxHeight; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Water)
                        {
                            var below = chunk.GetBlock(x, y - 1, z);
                            if (below == BlockType.Air && y > _config.World.MinHeight)
                            {
                                // Remove floating water
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        // Helper methods
        private Dictionary<string, float> InitializeBiomeParameters()
        {
            return new Dictionary<string, float>
            {
                ["temperature_variation"] = 0.3f,
                ["humidity_variation"] = 0.4f,
                ["elevation_factor"] = 0.5f
            };
        }

        private BlockType GetBiomeSurfaceBlock(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Taiga => BlockType.Grass,
                BiomeType.Swamp => BlockType.Grass,
                BiomeType.Jungle => BlockType.Grass,
                BiomeType.Tundra => BlockType.SnowBlock,
                BiomeType.SnowyMountains => BlockType.SnowBlock,
                _ => BlockType.Grass
            };
        }

        private void FillUnderground(ChunkData chunk, int x, int surfaceY, int z, BiomeType biome)
        {
            for (int y = _config.World.MinHeight; y < surfaceY; y++)
            {
                var depth = surfaceY - y;
                BlockType block;
                
                if (depth < 3)
                {
                    block = BlockType.Dirt;
                }
                else if (biome == BiomeType.Desert && depth < 8)
                {
                    block = BlockType.Sandstone;
                }
                else
                {
                    block = BlockType.Stone;
                }
                
                chunk.SetBlock(x, y, z, block);
            }
        }

        private int GetSurfaceHeight(ChunkData chunk, int x, int z)
        {
            for (int y = _config.World.MaxHeight - 1; y >= _config.World.MinHeight; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }
            return _config.World.MinHeight;
        }

        private bool ShouldPlaceFeature(float probability)
        {
            return new Random().NextDouble() < probability;
        }

        private void GenerateTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple oak tree generation
            var height = 4 + new Random().Next(3);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= 3)
                        {
                            chunk.SetBlock(x + dx, y + height + dy, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }

        private void GenerateJungleTree(ChunkData chunk, int x, int y, int z)
        {
            // Jungle tree with larger canopy
            var height = 6 + new Random().Next(4);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Larger leaf canopy
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    for (int dz = -3; dz <= 3; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= 4)
                        {
                            chunk.SetBlock(x + dx, y + height + dy, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }

        private void GeneratePineTree(ChunkData chunk, int x, int y, int z)
        {
            // Pine tree - taller and thinner
            var height = 7 + new Random().Next(3);
            for (int i = 0; i < height; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Cone-shaped leaves
            for (int i = height - 2; i <= height + 1; i++)
            {
                var radius = height - i + 1;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dz) <= radius)
                        {
                            chunk.SetBlock(x + dx, y + i, z + dz, BlockType.Leaves);
                        }
                    }
                }
            }
        }
    }
}
