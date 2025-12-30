using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced terrain generation coordinator that manages integration of terrain features,
    /// handles feature interactions, and optimizes terrain generation performance.
    /// </summary>
    public class ImprovedTerrainCoordinator
    {
        private readonly TerrainGenerationSettings _settings;
        private readonly Random _random;
        private readonly ImprovedCaveGenerator _caveGenerator;
        private readonly ImprovedRiverGenerator _riverGenerator;
        private readonly ImprovedLakeGenerator _lakeGenerator;
        
        // Feature interaction data
        private readonly Dictionary<int, List<TerrainFeature>> _terrainFeatures;
        private readonly Dictionary<int, List<FeatureInteraction>> _featureInteractions;
        
        // Performance tracking
        private readonly Dictionary<string, TimeSpan> _generationTimes;
        private DateTime _lastCleanup;
        
        public ImprovedTerrainCoordinator(
            TerrainGenerationSettings settings,
            ImprovedCaveGenerator caveGenerator,
            ImprovedRiverGenerator riverGenerator,
            ImprovedLakeGenerator lakeGenerator)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _caveGenerator = caveGenerator ?? throw new ArgumentNullException(nameof(caveGenerator));
            _riverGenerator = riverGenerator ?? throw new ArgumentNullException(nameof(riverGenerator));
            _lakeGenerator = lakeGenerator ?? throw new ArgumentNullException(nameof(lakeGenerator));
            
            _terrainFeatures = new Dictionary<int, List<TerrainFeature>>();
            _featureInteractions = new Dictionary<int, List<FeatureInteraction>>();
            _generationTimes = new Dictionary<string, TimeSpan>();
            _lastCleanup = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Generate terrain for a chunk with all features
        /// </summary>
        public void GenerateTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var startTime = DateTime.UtcNow;
            
            // Initialize collections for this chunk
            _terrainFeatures[chunkKey] = new List<TerrainFeature>();
            _featureInteractions[chunkKey] = new List<FeatureInteraction>();
            
            // Generate base terrain
            GenerateBaseTerrain(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate terrain features based on priority
            var features = GetOrderedFeatures(chunkX, chunkZ, heightMap);
            
            foreach (var feature in features)
            {
                GenerateFeature(feature, chunkX, chunkZ, heightMap, blockTypes);
            }
            
            // Process feature interactions
            ProcessFeatureInteractions(chunkX, chunkZ, heightMap, blockTypes);
            
            // Optimize terrain
            OptimizeTerrain(chunkX, chunkZ, heightMap, blockTypes);
            
            // Track performance
            var endTime = DateTime.UtcNow;
            _generationTimes[$"chunk_{chunkX}_{chunkZ}"] = endTime - startTime;
            
            // Cleanup old data periodically
            if (DateTime.UtcNow - _lastCleanup > TimeSpan.FromMinutes(30))
            {
                CleanupOldData();
                _lastCleanup = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Generate base terrain without features
        /// </summary>
        private void GenerateBaseTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Generate basic terrain using noise functions
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Generate height using multiple octaves of noise
                    var height = GenerateHeight(worldX, worldZ);
                    heightMap[x + z * 16] = height;
                    
                    // Generate basic block types based on height
                    var blockType = GetBlockTypeForHeight(height);
                    blockTypes[height * 16 * 16 + z * 16 + x] = (int)blockType;
                }
            }
        }
        
        /// <summary>
        /// Get ordered list of features to generate for this chunk
        /// </summary>
        private List<TerrainFeature> GetOrderedFeatures(int chunkX, int chunkZ, int[] heightMap)
        {
            var features = new List<TerrainFeature>();
            var avgHeight = GetAverageHeight(heightMap);
            
            // Determine which features should be generated based on height and noise
            var caveNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 100);
            var riverNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 200);
            var lakeNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 300);
            
            // Add caves
            if (caveNoise > _settings.CaveThreshold && avgHeight > _settings.MinCaveHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Caves,
                    Priority = _settings.CavePriority,
                    Strength = Math.Min(1.0, caveNoise + 0.5)
                });
            }
            
            // Add rivers
            if (riverNoise > _settings.RiverThreshold && avgHeight > _settings.MinRiverHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Rivers,
                    Priority = _settings.RiverPriority,
                    Strength = Math.Min(1.0, riverNoise + 0.5)
                });
            }
            
            // Add lakes
            if (lakeNoise > _settings.LakeThreshold && avgHeight > _settings.MinLakeHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Lakes,
                    Priority = _settings.LakePriority,
                    Strength = Math.Min(1.0, lakeNoise + 0.5)
                });
            }
            
            // Sort by priority (lower number = higher priority)
            return features.OrderBy(f => f.Priority).ToList();
        }
        
        /// <summary>
        /// Generate a specific terrain feature
        /// </summary>
        private void GenerateFeature(TerrainFeature feature, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var startTime = DateTime.UtcNow;
            
            switch (feature.Type)
            {
                case TerrainFeatureType.Caves:
                    _caveGenerator.GenerateCaves(chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case TerrainFeatureType.Rivers:
                    _riverGenerator.GenerateRivers(chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case TerrainFeatureType.Lakes:
                    _lakeGenerator.GenerateLakes(chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
            
            // Track generation time
            var endTime = DateTime.UtcNow;
            _generationTimes[$"{feature.Type}_chunk_{chunkX}_{chunkZ}"] = endTime - startTime;
        }
        
        /// <summary>
        /// Process interactions between terrain features
        /// </summary>
        private void ProcessFeatureInteractions(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Find all feature interactions in this chunk
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check for cave-river interactions
                    ProcessCaveRiverInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                    
                    // Check for cave-lake interactions
                    ProcessCaveLakeInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                    
                    // Check for river-lake interactions
                    ProcessRiverLakeInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Process cave-river interactions
        /// </summary>
        private void ProcessCaveRiverInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if this position has both cave and river features
            var hasCave = HasCaveAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            var hasRiver = HasRiverAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            
            if (hasCave && HasRiver)
            {
                // Create a special interaction
                var localX = worldX - chunkX * 16;
                var localZ = worldZ - chunkZ * 16;
                
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Create a water-filled cave section
                        if (blockTypes[index] == 0) // Air (cave)
                        {
                            blockTypes[index] = (int)BlockType.Water;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process cave-lake interactions
        /// </summary>
        private void ProcessCaveLakeInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if this position has both cave and lake features
            var hasCave = HasCaveAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            var hasLake = HasLakeAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            
            if (hasCave && HasLake)
            {
                // Create underwater cave entrances
                var localX = worldX - chunkX * 16;
                var localZ = worldZ - chunkZ * 16;
                
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Create a special cave entrance
                        if (blockTypes[index] == 0 && y < GetWaterLevel(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes))
                        {
                            blockTypes[index] = (int)BlockType.Gravel; // Gravel for cave entrance
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process river-lake interactions
        /// </summary>
        private void ProcessRiverLakeInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is handled by the river and lake generators themselves
            // Rivers naturally flow into lakes
        }
        
        /// <summary>
        /// Optimize terrain for performance and visual quality
        /// </summary>
        private void OptimizeTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Smooth terrain transitions
            SmoothTerrainTransitions(chunkX, chunkZ, heightMap, blockTypes);
            
            // Fix floating blocks
            FixFloatingBlocks(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add support columns
            AddSupportColumns(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Smooth terrain transitions between different block types
        /// </summary>
        private void SmoothTerrainTransitions(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Create a copy of the block types for reference
            var originalBlockTypes = new int[blockTypes.Length];
            Array.Copy(blockTypes, originalBlockTypes, blockTypes.Length);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 0; y < 256; y++)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            var blockType = (BlockType)originalBlockTypes[index];
                            
                            // Check neighbors for smooth transitions
                            var neighbors = GetNeighborBlockTypes(x, y, z, originalBlockTypes);
                            var mostCommon = neighbors.GroupBy(b => b)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key ?? blockType;
                            
                            // Apply smooth transition
                            if (ShouldSmoothTransition(blockType, mostCommon))
                            {
                                blockTypes[index] = (int)mostCommon;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fix floating blocks in the terrain
        /// </summary>
        private void FixFloatingBlocks(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 1; y < 256; y++)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            var blockType = (BlockType)blockTypes[index];
                            
                            // Check if this block should be supported
                            if (ShouldBeSupported(blockType))
                            {
                                // Check if there's a supporting block below
                                var belowIndex = (y - 1) * 16 * 16 + z * 16 + x;
                                
                                if (belowIndex >= 0 && belowIndex < blockTypes.Length)
                                {
                                    var belowBlockType = (BlockType)blockTypes[belowIndex];
                                    
                                    if (!CanSupport(belowBlockType, blockType))
                                    {
                                        // Replace with air
                                        blockTypes[index] = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add support columns for floating structures
        /// </summary>
        private void AddSupportColumns(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    // Find the highest non-air block
                    var highestNonAir = -1;
                    
                    for (int y = 255; y >= 0; y--)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                        {
                            highestNonAir = y;
                            break;
                        }
                    }
                    
                    // If we found a non-air block, check if it needs support
                    if (highestNonAir >= 0)
                    {
                        var blockIndex = highestNonAir * 16 * 16 + z * 16 + x;
                        var blockType = (BlockType)blockTypes[blockIndex];
                        
                        if (NeedsSupportColumn(blockType))
                        {
                            // Add a support column from bedrock to this block
                            for (int y = 0; y < highestNonAir; y++)
                            {
                                var supportIndex = y * 16 * 16 + z * 16 + x;
                                
                                if (supportIndex >= 0 && supportIndex < blockTypes.Length && blockTypes[supportIndex] == 0)
                                {
                                    blockTypes[supportIndex] = (int)BlockType.Stone; // Stone support
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate height using multiple octaves of noise
        /// </summary>
        private int GenerateHeight(int x, int z)
        {
            var height = 0.0;
            var amplitude = 1.0;
            var frequency = 1.0;
            
            for (int i = 0; i < _settings.Octaves; i++)
            {
                height += SimpleNoise(x * frequency, z * frequency, _settings.Seed) * amplitude;
                amplitude *= _settings.Persistence;
                frequency *= _settings.Lacunarity;
            }
            
            // Normalize and scale the height
            height = (height + 1.0) / 2.0; // Normalize to [0, 1]
            height = Math.Pow(height, _settings.Exponent); // Apply exponent for more dramatic terrain
            return (int)(height * _settings.Scale) + _settings.Offset;
        }
        
        /// <summary>
        /// Get block type based on height
        /// </summary>
        private BlockType GetBlockTypeForHeight(int height)
        {
            if (height < _settings.WaterLevel)
            {
                return BlockType.Water;
            }
            else if (height < _settings.BeachLevel)
            {
                return BlockType.Sand;
            }
            else if (height < _settings.GrassLevel)
            {
                return BlockType.Grass;
            }
            else if (height < _settings.StoneLevel)
            {
                return BlockType.Dirt;
            }
            else
            {
                return BlockType.Stone;
            }
        }
        
        /// <summary>
        /// Get neighbor block types
        /// </summary>
        private List<BlockType> GetNeighborBlockTypes(int x, int y, int z, int[] blockTypes)
        {
            var neighbors = new List<BlockType>();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue; // Skip the center block
                            
                        var nx = x + dx;
                        var ny = y + dy;
                        var nz = z + dz;
                        
                        if (nx >= 0 && nx < 16 && ny >= 0 && ny < 256 && nz >= 0 && nz < 16)
                        {
                            var index = ny * 16 * 16 + nz * 16 + nx;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                neighbors.Add((BlockType)blockTypes[index]);
                            }
                        }
                    }
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Check if a transition should be smoothed
        /// </summary>
        private bool ShouldSmoothTransition(BlockType from, BlockType to)
        {
            // Define which transitions should be smoothed
            return (from == BlockType.Grass && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Grass) ||
                   (from == BlockType.Sand && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Sand) ||
                   (from == BlockType.Stone && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Stone);
        }
        
        /// <summary>
        /// Check if a block type should be supported
        /// </summary>
        private bool ShouldBeSupported(BlockType blockType)
        {
            // Non-solid blocks don't need support
            return blockType != BlockType.Air && blockType != BlockType.Water;
        }
        
        /// <summary>
        /// Check if a block can support another block
        /// </summary>
        private bool CanSupport(BlockType support, BlockType supported)
        {
            // Most solid blocks can support other blocks
            return support != BlockType.Air && support != BlockType.Water;
        }
        
        /// <summary>
        /// Check if a block needs a support column
        /// </summary>
        private bool NeedsSupportColumn(BlockType blockType)
        {
            // Floating structures need support columns
            return blockType == BlockType.Wood || blockType == BlockType.Leaves;
        }
        
        /// <summary>
        /// Check if there's a cave at a position
        /// </summary>
        private bool HasCaveAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the cave generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a cave block (simplified)
                        if (blockTypes[index] == 0 && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if there's a river at a position
        /// </summary>
        private bool HasRiverAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the river generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a river block (simplified)
                        if (blockTypes[index] == (int)BlockType.Water && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if there's a lake at a position
        /// </summary>
        private bool HasLakeAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the lake generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a lake block (simplified)
                        if (blockTypes[index] == (int)BlockType.Water && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the water level at a position
        /// </summary>
        private int GetWaterLevel(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the river/lake generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        if (blockTypes[index] == (int)BlockType.Water)
                        {
                            return y;
                        }
                    }
                }
            }
            
            return GetAverageHeight(heightMap);
        }
        
        /// <summary>
        /// Get the average height of a chunk
        /// </summary>
        private int GetAverageHeight(int[] heightMap)
        {
            var sum = 0;
            var count = 0;
            
            for (int i = 0; i < heightMap.Length; i++)
            {
                sum += heightMap[i];
                count++;
            }
            
            return count > 0 ? sum / count : 64;
        }
        
        /// <summary>
        /// Simple noise function
        /// </summary>
        private double SimpleNoise(double x, double z, int seed)
        {
            var n = (int)Math.Sin(x * 12.9898 + z * 78.233 + seed * 43.5453) * 43758.5453;
            return (n - Math.Floor(n)) * 2 - 1;
        }
        
        /// <summary>
        /// Get a unique key for a chunk
        /// </summary>
        private int GetChunkKey(int chunkX, int chunkZ)
        {
            return chunkX * 1000000 + chunkZ;
        }
        
        /// <summary>
        /// Clean up old data to prevent memory leaks
        /// </summary>
        private void CleanupOldData()
        {
            // Remove data for chunks that are no longer needed
            var keysToRemove = new List<int>();
            
            foreach (var key in _terrainFeatures.Keys)
            {
                // Extract chunk coordinates from key
                var chunkX = key / 1000000;
                var chunkZ = key % 1000000;
                
                // Remove if this chunk is too far from origin (simplified)
                if (Math.Abs(chunkX) > 100 || Math.Abs(chunkZ) > 100)
                {
                    keysToRemove.Add(key);
                }
            }
            
            // Remove old data
            foreach (var key in keysToRemove)
            {
                _terrainFeatures.Remove(key);
                _featureInteractions.Remove(key);
            }
            
            // Clean up old performance data
            var timeKeysToRemove = new List<string>();
            
            foreach (var key in _generationTimes.Keys)
            {
                // Remove if this is older than 1 hour
                if (key.Contains("chunk_") && _generationTimes[key] > TimeSpan.FromHours(1))
                {
                    timeKeysToRemove.Add(key);
                }
            }
            
            foreach (var key in timeKeysToRemove)
            {
                _generationTimes.Remove(key);
            }
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        public TerrainGenerationStats GetStats()
        {
            var stats = new TerrainGenerationStats();
            
            // Calculate average generation times
            var caveTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Caves")).Select(kvp => kvp.Value).ToList();
            var riverTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Rivers")).Select(kvp => kvp.Value).ToList();
            var lakeTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Lakes")).Select(kvp => kvp.Value).ToList();
            
            if (caveTimes.Count > 0)
            {
                stats.AverageCaveGenerationTime = TimeSpan.FromTicks((long)caveTimes.Average(t => t.Ticks));
            }
            
            if (riverTimes.Count > 0)
            {
                stats.AverageRiverGenerationTime = TimeSpan.FromTicks((long)riverTimes.Average(t => t.Ticks));
            }
            
            if (lakeTimes.Count > 0)
            {
                stats.AverageLakeGenerationTime = TimeSpan.FromTicks((long)lakeTimes.Average(t => t.Ticks));
            }
            
            stats.TotalChunksGenerated = _terrainFeatures.Count;
            stats.MemoryUsage = EstimateMemoryUsage();
            
            return stats;
        }
        
        /// <summary>
        /// Estimate memory usage
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // Rough estimation of memory usage
            var featureCount = _terrainFeatures.Values.Sum(list => list.Count);
            var interactionCount = _featureInteractions.Values.Sum(list => list.Count);
            
            // Each feature is roughly 100 bytes
            // Each interaction is roughly 50 bytes
            return featureCount * 100 + interactionCount * 50;
        }
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Terrain feature information
    /// </summary>
    public class TerrainFeature
    {
        public TerrainFeatureType Type { get; set; }
        public int Priority { get; set; }
        public double Strength { get; set; }
    }
    
    /// <summary>
    /// Feature interaction information
    /// </summary>
    public class FeatureInteraction
    {
        public TerrainFeatureType Feature1 { get; set; }
        public TerrainFeatureType Feature2 { get; set; }
        public InteractionType Type { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int PositionZ { get; set; }
    }
    
    /// <summary>
    /// Terrain generation statistics
    /// </summary>
    public class TerrainGenerationStats
    {
        public TimeSpan AverageCaveGenerationTime { get; set; }
        public TimeSpan AverageRiverGenerationTime { get; set; }
        public TimeSpan AverageLakeGenerationTime { get; set; }
        public int TotalChunksGenerated { get; set; }
        public long MemoryUsage { get; set; }
    }
    
    /// <summary>
    /// Terrain feature types
    /// </summary>
    public enum TerrainFeatureType
    {
        Caves,
        Rivers,
        Lakes
    }
    
    /// <summary>
    /// Feature interaction types
    /// </summary>
    public enum InteractionType
    {
        Overlap,
        Connection,
        Modification
    }
    
    /// <summary>
    /// Block types
    /// </summary>
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Water = 4,
        Sand = 5,
        Gravel = 6,
        Wood = 7,
        Leaves = 8,
        Coal = 9,
        Iron = 10,
        Gold = 11,
        Diamond = 12,
        Mushroom = 13,
        Cobweb = 14,
        LilyPad = 15,
        Reeds = 16,
        Flower = 17,
        Sapling = 18
    }
    
    #endregion
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced terrain generation coordinator that manages integration of terrain features,
    /// handles feature interactions, and optimizes terrain generation performance.
    /// </summary>
    public class ImprovedTerrainCoordinator
    {
        private readonly TerrainGenerationSettings _settings;
        private readonly Random _random;
        private readonly ImprovedCaveGenerator _caveGenerator;
        private readonly ImprovedRiverGenerator _riverGenerator;
        private readonly ImprovedLakeGenerator _lakeGenerator;
        
        // Feature interaction data
        private readonly Dictionary<int, List<TerrainFeature>> _terrainFeatures;
        private readonly Dictionary<int, List<FeatureInteraction>> _featureInteractions;
        
        // Performance tracking
        private readonly Dictionary<string, TimeSpan> _generationTimes;
        private DateTime _lastCleanup;
        
        public ImprovedTerrainCoordinator(
            TerrainGenerationSettings settings,
            ImprovedCaveGenerator caveGenerator,
            ImprovedRiverGenerator riverGenerator,
            ImprovedLakeGenerator lakeGenerator)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _caveGenerator = caveGenerator ?? throw new ArgumentNullException(nameof(caveGenerator));
            _riverGenerator = riverGenerator ?? throw new ArgumentNullException(nameof(riverGenerator));
            _lakeGenerator = lakeGenerator ?? throw new ArgumentNullException(nameof(lakeGenerator));
            
            _terrainFeatures = new Dictionary<int, List<TerrainFeature>>();
            _featureInteractions = new Dictionary<int, List<FeatureInteraction>>();
            _generationTimes = new Dictionary<string, TimeSpan>();
            _lastCleanup = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Generate terrain for a chunk with all features
        /// </summary>
        public void GenerateTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var startTime = DateTime.UtcNow;
            
            // Initialize collections for this chunk
            _terrainFeatures[chunkKey] = new List<TerrainFeature>();
            _featureInteractions[chunkKey] = new List<FeatureInteraction>();
            
            // Generate base terrain
            GenerateBaseTerrain(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate terrain features based on priority
            var features = GetOrderedFeatures(chunkX, chunkZ, heightMap);
            
            foreach (var feature in features)
            {
                GenerateFeature(feature, chunkX, chunkZ, heightMap, blockTypes);
            }
            
            // Process feature interactions
            ProcessFeatureInteractions(chunkX, chunkZ, heightMap, blockTypes);
            
            // Optimize terrain
            OptimizeTerrain(chunkX, chunkZ, heightMap, blockTypes);
            
            // Track performance
            var endTime = DateTime.UtcNow;
            _generationTimes[$"chunk_{chunkX}_{chunkZ}"] = endTime - startTime;
            
            // Cleanup old data periodically
            if (DateTime.UtcNow - _lastCleanup > TimeSpan.FromMinutes(30))
            {
                CleanupOldData();
                _lastCleanup = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Generate base terrain without features
        /// </summary>
        private void GenerateBaseTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Generate basic terrain using noise functions
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Generate height using multiple octaves of noise
                    var height = GenerateHeight(worldX, worldZ);
                    heightMap[x + z * 16] = height;
                    
                    // Generate basic block types based on height
                    var blockType = GetBlockTypeForHeight(height);
                    blockTypes[height * 16 * 16 + z * 16 + x] = (int)blockType;
                }
            }
        }
        
        /// <summary>
        /// Get ordered list of features to generate for this chunk
        /// </summary>
        private List<TerrainFeature> GetOrderedFeatures(int chunkX, int chunkZ, int[] heightMap)
        {
            var features = new List<TerrainFeature>();
            var avgHeight = GetAverageHeight(heightMap);
            
            // Determine which features should be generated based on height and noise
            var caveNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 100);
            var riverNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 200);
            var lakeNoise = SimpleNoise(chunkX * 0.05, chunkZ * 0.05, _settings.Seed + 300);
            
            // Add caves
            if (caveNoise > _settings.CaveThreshold && avgHeight > _settings.MinCaveHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Caves,
                    Priority = _settings.CavePriority,
                    Strength = Math.Min(1.0, caveNoise + 0.5)
                });
            }
            
            // Add rivers
            if (riverNoise > _settings.RiverThreshold && avgHeight > _settings.MinRiverHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Rivers,
                    Priority = _settings.RiverPriority,
                    Strength = Math.Min(1.0, riverNoise + 0.5)
                });
            }
            
            // Add lakes
            if (lakeNoise > _settings.LakeThreshold && avgHeight > _settings.MinLakeHeight)
            {
                features.Add(new TerrainFeature
                {
                    Type = TerrainFeatureType.Lakes,
                    Priority = _settings.LakePriority,
                    Strength = Math.Min(1.0, lakeNoise + 0.5)
                });
            }
            
            // Sort by priority (lower number = higher priority)
            return features.OrderBy(f => f.Priority).ToList();
        }
        
        /// <summary>
        /// Generate a specific terrain feature
        /// </summary>
        private void GenerateFeature(TerrainFeature feature, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var startTime = DateTime.UtcNow;
            
            switch (feature.Type)
            {
                case TerrainFeatureType.Caves:
                    _caveGenerator.GenerateCaves(chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case TerrainFeatureType.Rivers:
                    _riverGenerator.GenerateRivers(chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case TerrainFeatureType.Lakes:
                    _lakeGenerator.GenerateLakes(chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
            
            // Track generation time
            var endTime = DateTime.UtcNow;
            _generationTimes[$"{feature.Type}_chunk_{chunkX}_{chunkZ}"] = endTime - startTime;
        }
        
        /// <summary>
        /// Process interactions between terrain features
        /// </summary>
        private void ProcessFeatureInteractions(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Find all feature interactions in this chunk
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check for cave-river interactions
                    ProcessCaveRiverInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                    
                    // Check for cave-lake interactions
                    ProcessCaveLakeInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                    
                    // Check for river-lake interactions
                    ProcessRiverLakeInteraction(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Process cave-river interactions
        /// </summary>
        private void ProcessCaveRiverInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if this position has both cave and river features
            var hasCave = HasCaveAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            var hasRiver = HasRiverAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            
            if (hasCave && HasRiver)
            {
                // Create a special interaction
                var localX = worldX - chunkX * 16;
                var localZ = worldZ - chunkZ * 16;
                
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Create a water-filled cave section
                        if (blockTypes[index] == 0) // Air (cave)
                        {
                            blockTypes[index] = (int)BlockType.Water;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process cave-lake interactions
        /// </summary>
        private void ProcessCaveLakeInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if this position has both cave and lake features
            var hasCave = HasCaveAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            var hasLake = HasLakeAtPosition(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes);
            
            if (hasCave && HasLake)
            {
                // Create underwater cave entrances
                var localX = worldX - chunkX * 16;
                var localZ = worldZ - chunkZ * 16;
                
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Create a special cave entrance
                        if (blockTypes[index] == 0 && y < GetWaterLevel(worldX, worldZ, chunkX, chunkZ, heightMap, blockTypes))
                        {
                            blockTypes[index] = (int)BlockType.Gravel; // Gravel for cave entrance
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process river-lake interactions
        /// </summary>
        private void ProcessRiverLakeInteraction(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is handled by the river and lake generators themselves
            // Rivers naturally flow into lakes
        }
        
        /// <summary>
        /// Optimize terrain for performance and visual quality
        /// </summary>
        private void OptimizeTerrain(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Smooth terrain transitions
            SmoothTerrainTransitions(chunkX, chunkZ, heightMap, blockTypes);
            
            // Fix floating blocks
            FixFloatingBlocks(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add support columns
            AddSupportColumns(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Smooth terrain transitions between different block types
        /// </summary>
        private void SmoothTerrainTransitions(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Create a copy of the block types for reference
            var originalBlockTypes = new int[blockTypes.Length];
            Array.Copy(blockTypes, originalBlockTypes, blockTypes.Length);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 0; y < 256; y++)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            var blockType = (BlockType)originalBlockTypes[index];
                            
                            // Check neighbors for smooth transitions
                            var neighbors = GetNeighborBlockTypes(x, y, z, originalBlockTypes);
                            var mostCommon = neighbors.GroupBy(b => b)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key ?? blockType;
                            
                            // Apply smooth transition
                            if (ShouldSmoothTransition(blockType, mostCommon))
                            {
                                blockTypes[index] = (int)mostCommon;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fix floating blocks in the terrain
        /// </summary>
        private void FixFloatingBlocks(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 1; y < 256; y++)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            var blockType = (BlockType)blockTypes[index];
                            
                            // Check if this block should be supported
                            if (ShouldBeSupported(blockType))
                            {
                                // Check if there's a supporting block below
                                var belowIndex = (y - 1) * 16 * 16 + z * 16 + x;
                                
                                if (belowIndex >= 0 && belowIndex < blockTypes.Length)
                                {
                                    var belowBlockType = (BlockType)blockTypes[belowIndex];
                                    
                                    if (!CanSupport(belowBlockType, blockType))
                                    {
                                        // Replace with air
                                        blockTypes[index] = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add support columns for floating structures
        /// </summary>
        private void AddSupportColumns(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    // Find the highest non-air block
                    var highestNonAir = -1;
                    
                    for (int y = 255; y >= 0; y--)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                        {
                            highestNonAir = y;
                            break;
                        }
                    }
                    
                    // If we found a non-air block, check if it needs support
                    if (highestNonAir >= 0)
                    {
                        var blockIndex = highestNonAir * 16 * 16 + z * 16 + x;
                        var blockType = (BlockType)blockTypes[blockIndex];
                        
                        if (NeedsSupportColumn(blockType))
                        {
                            // Add a support column from bedrock to this block
                            for (int y = 0; y < highestNonAir; y++)
                            {
                                var supportIndex = y * 16 * 16 + z * 16 + x;
                                
                                if (supportIndex >= 0 && supportIndex < blockTypes.Length && blockTypes[supportIndex] == 0)
                                {
                                    blockTypes[supportIndex] = (int)BlockType.Stone; // Stone support
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate height using multiple octaves of noise
        /// </summary>
        private int GenerateHeight(int x, int z)
        {
            var height = 0.0;
            var amplitude = 1.0;
            var frequency = 1.0;
            
            for (int i = 0; i < _settings.Octaves; i++)
            {
                height += SimpleNoise(x * frequency, z * frequency, _settings.Seed) * amplitude;
                amplitude *= _settings.Persistence;
                frequency *= _settings.Lacunarity;
            }
            
            // Normalize and scale the height
            height = (height + 1.0) / 2.0; // Normalize to [0, 1]
            height = Math.Pow(height, _settings.Exponent); // Apply exponent for more dramatic terrain
            return (int)(height * _settings.Scale) + _settings.Offset;
        }
        
        /// <summary>
        /// Get block type based on height
        /// </summary>
        private BlockType GetBlockTypeForHeight(int height)
        {
            if (height < _settings.WaterLevel)
            {
                return BlockType.Water;
            }
            else if (height < _settings.BeachLevel)
            {
                return BlockType.Sand;
            }
            else if (height < _settings.GrassLevel)
            {
                return BlockType.Grass;
            }
            else if (height < _settings.StoneLevel)
            {
                return BlockType.Dirt;
            }
            else
            {
                return BlockType.Stone;
            }
        }
        
        /// <summary>
        /// Get neighbor block types
        /// </summary>
        private List<BlockType> GetNeighborBlockTypes(int x, int y, int z, int[] blockTypes)
        {
            var neighbors = new List<BlockType>();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue; // Skip the center block
                            
                        var nx = x + dx;
                        var ny = y + dy;
                        var nz = z + dz;
                        
                        if (nx >= 0 && nx < 16 && ny >= 0 && ny < 256 && nz >= 0 && nz < 16)
                        {
                            var index = ny * 16 * 16 + nz * 16 + nx;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                neighbors.Add((BlockType)blockTypes[index]);
                            }
                        }
                    }
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Check if a transition should be smoothed
        /// </summary>
        private bool ShouldSmoothTransition(BlockType from, BlockType to)
        {
            // Define which transitions should be smoothed
            return (from == BlockType.Grass && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Grass) ||
                   (from == BlockType.Sand && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Sand) ||
                   (from == BlockType.Stone && to == BlockType.Dirt) ||
                   (from == BlockType.Dirt && to == BlockType.Stone);
        }
        
        /// <summary>
        /// Check if a block type should be supported
        /// </summary>
        private bool ShouldBeSupported(BlockType blockType)
        {
            // Non-solid blocks don't need support
            return blockType != BlockType.Air && blockType != BlockType.Water;
        }
        
        /// <summary>
        /// Check if a block can support another block
        /// </summary>
        private bool CanSupport(BlockType support, BlockType supported)
        {
            // Most solid blocks can support other blocks
            return support != BlockType.Air && support != BlockType.Water;
        }
        
        /// <summary>
        /// Check if a block needs a support column
        /// </summary>
        private bool NeedsSupportColumn(BlockType blockType)
        {
            // Floating structures need support columns
            return blockType == BlockType.Wood || blockType == BlockType.Leaves;
        }
        
        /// <summary>
        /// Check if there's a cave at a position
        /// </summary>
        private bool HasCaveAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the cave generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a cave block (simplified)
                        if (blockTypes[index] == 0 && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if there's a river at a position
        /// </summary>
        private bool HasRiverAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the river generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a river block (simplified)
                        if (blockTypes[index] == (int)BlockType.Water && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if there's a lake at a position
        /// </summary>
        private bool HasLakeAtPosition(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the lake generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        // Check if this is a lake block (simplified)
                        if (blockTypes[index] == (int)BlockType.Water && y < GetAverageHeight(heightMap))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the water level at a position
        /// </summary>
        private int GetWaterLevel(int worldX, int worldZ, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to check against the river/lake generator's data
            var localX = worldX - chunkX * 16;
            var localZ = worldZ - chunkZ * 16;
            
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int y = 0; y < 256; y++)
                {
                    var index = y * 16 * 16 + localZ * 16 + localX;
                    
                    if (index >= 0 && index < blockTypes.Length)
                    {
                        if (blockTypes[index] == (int)BlockType.Water)
                        {
                            return y;
                        }
                    }
                }
            }
            
            return GetAverageHeight(heightMap);
        }
        
        /// <summary>
        /// Get the average height of a chunk
        /// </summary>
        private int GetAverageHeight(int[] heightMap)
        {
            var sum = 0;
            var count = 0;
            
            for (int i = 0; i < heightMap.Length; i++)
            {
                sum += heightMap[i];
                count++;
            }
            
            return count > 0 ? sum / count : 64;
        }
        
        /// <summary>
        /// Simple noise function
        /// </summary>
        private double SimpleNoise(double x, double z, int seed)
        {
            var n = (int)Math.Sin(x * 12.9898 + z * 78.233 + seed * 43.5453) * 43758.5453;
            return (n - Math.Floor(n)) * 2 - 1;
        }
        
        /// <summary>
        /// Get a unique key for a chunk
        /// </summary>
        private int GetChunkKey(int chunkX, int chunkZ)
        {
            return chunkX * 1000000 + chunkZ;
        }
        
        /// <summary>
        /// Clean up old data to prevent memory leaks
        /// </summary>
        private void CleanupOldData()
        {
            // Remove data for chunks that are no longer needed
            var keysToRemove = new List<int>();
            
            foreach (var key in _terrainFeatures.Keys)
            {
                // Extract chunk coordinates from key
                var chunkX = key / 1000000;
                var chunkZ = key % 1000000;
                
                // Remove if this chunk is too far from origin (simplified)
                if (Math.Abs(chunkX) > 100 || Math.Abs(chunkZ) > 100)
                {
                    keysToRemove.Add(key);
                }
            }
            
            // Remove old data
            foreach (var key in keysToRemove)
            {
                _terrainFeatures.Remove(key);
                _featureInteractions.Remove(key);
            }
            
            // Clean up old performance data
            var timeKeysToRemove = new List<string>();
            
            foreach (var key in _generationTimes.Keys)
            {
                // Remove if this is older than 1 hour
                if (key.Contains("chunk_") && _generationTimes[key] > TimeSpan.FromHours(1))
                {
                    timeKeysToRemove.Add(key);
                }
            }
            
            foreach (var key in timeKeysToRemove)
            {
                _generationTimes.Remove(key);
            }
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        public TerrainGenerationStats GetStats()
        {
            var stats = new TerrainGenerationStats();
            
            // Calculate average generation times
            var caveTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Caves")).Select(kvp => kvp.Value).ToList();
            var riverTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Rivers")).Select(kvp => kvp.Value).ToList();
            var lakeTimes = _generationTimes.Where(kvp => kvp.Key.Contains("Lakes")).Select(kvp => kvp.Value).ToList();
            
            if (caveTimes.Count > 0)
            {
                stats.AverageCaveGenerationTime = TimeSpan.FromTicks((long)caveTimes.Average(t => t.Ticks));
            }
            
            if (riverTimes.Count > 0)
            {
                stats.AverageRiverGenerationTime = TimeSpan.FromTicks((long)riverTimes.Average(t => t.Ticks));
            }
            
            if (lakeTimes.Count > 0)
            {
                stats.AverageLakeGenerationTime = TimeSpan.FromTicks((long)lakeTimes.Average(t => t.Ticks));
            }
            
            stats.TotalChunksGenerated = _terrainFeatures.Count;
            stats.MemoryUsage = EstimateMemoryUsage();
            
            return stats;
        }
        
        /// <summary>
        /// Estimate memory usage
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // Rough estimation of memory usage
            var featureCount = _terrainFeatures.Values.Sum(list => list.Count);
            var interactionCount = _featureInteractions.Values.Sum(list => list.Count);
            
            // Each feature is roughly 100 bytes
            // Each interaction is roughly 50 bytes
            return featureCount * 100 + interactionCount * 50;
        }
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Terrain feature information
    /// </summary>
    public class TerrainFeature
    {
        public TerrainFeatureType Type { get; set; }
        public int Priority { get; set; }
        public double Strength { get; set; }
    }
    
    /// <summary>
    /// Feature interaction information
    /// </summary>
    public class FeatureInteraction
    {
        public TerrainFeatureType Feature1 { get; set; }
        public TerrainFeatureType Feature2 { get; set; }
        public InteractionType Type { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int PositionZ { get; set; }
    }
    
    /// <summary>
    /// Terrain generation statistics
    /// </summary>
    public class TerrainGenerationStats
    {
        public TimeSpan AverageCaveGenerationTime { get; set; }
        public TimeSpan AverageRiverGenerationTime { get; set; }
        public TimeSpan AverageLakeGenerationTime { get; set; }
        public int TotalChunksGenerated { get; set; }
        public long MemoryUsage { get; set; }
    }
    
    /// <summary>
    /// Terrain feature types
    /// </summary>
    public enum TerrainFeatureType
    {
        Caves,
        Rivers,
        Lakes
    }
    
    /// <summary>
    /// Feature interaction types
    /// </summary>
    public enum InteractionType
    {
        Overlap,
        Connection,
        Modification
    }
    
    /// <summary>
    /// Block types
    /// </summary>
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Water = 4,
        Sand = 5,
        Gravel = 6,
        Wood = 7,
        Leaves = 8,
        Coal = 9,
        Iron = 10,
        Gold = 11,
        Diamond = 12,
        Mushroom = 13,
        Cobweb = 14,
        LilyPad = 15,
        Reeds = 16,
        Flower = 17,
        Sapling = 18
    }
    
    #endregion
}
}
            
            var finalHeight = baseHeight + mountainHeight + hillHeight + detailHeight;
            
            // Apply elevation limits
            return Math.Clamp((int)finalHeight, 20, 120);
        }
        
        /// <summary>
        /// Collect all terrain features for this chunk
        /// </summary>
        private List<TerrainFeature> CollectTerrainFeatures(int chunkX, int chunkZ)
        {
            var features = new List<TerrainFeature>();
            
            // Collect cave systems
            if (_worldManager._enableCaves)
            {
                var caveSystems = _caveGenerator.GenerateCaveSystems(chunkX, chunkZ);
                foreach (var cave in caveSystems)
                {
                    features.Add(new TerrainFeature
                    {
                        Type = TerrainFeatureType.Caves,
                        Data = cave,
                        Bounds = CalculateFeatureBounds(cave)
                    });
                }
            }
            
            // Collect river systems
            if (_worldManager._enableRivers)
            {
                var riverSystems = _riverGenerator.GenerateRiverSystems(chunkX, chunkZ);
                foreach (var river in riverSystems)
                {
                    features.Add(new TerrainFeature
                    {
                        Type = TerrainFeatureType.Rivers,
                        Data = river,
                        Bounds = CalculateFeatureBounds(river)
                    });
                }
            }
            
            // Collect lake systems
            if (_worldManager._enableLakes)
            {
                var lakeSystems = _lakeGenerator.GenerateLakeSystems(chunkX, chunkZ);
                foreach (var lake in lakeSystems)
                {
                    features.Add(new TerrainFeature
                    {
                        Type = TerrainFeatureType.Lakes,
                        Data = lake,
                        Bounds = CalculateFeatureBounds(lake)
                    });
                }
            }
            
            return features;
        }
        
        /// <summary>
        /// Calculate bounding box for a terrain feature
        /// </summary>
        private FeatureBounds CalculateFeatureBounds(object feature)
        {
            return feature switch
            {
                CaveSystem cave => new FeatureBounds
                {
                    MinX = cave.StartX - cave.Size,
                    MaxX = cave.StartX + cave.Size,
                    MinZ = cave.StartZ - cave.Size,
                    MaxZ = cave.StartZ + cave.Size,
                    MinY = Math.Max(0, cave.StartY - cave.Size),
                    MaxY = Math.Min(255, cave.StartY + cave.Size)
                },
                RiverSystem river => new FeatureBounds
                {
                    MinX = river.SourceX - river.Length / 2,
                    MaxX = river.SourceX + river.Length / 2,
                    MinZ = river.SourceZ - river.Length / 2,
                    MaxZ = river.SourceZ + river.Length / 2,
                    MinY = Math.Max(0, river.SourceY - (int)river.Width),
                    MaxY = Math.Min(255, river.SourceY + (int)river.Width)
                },
                LakeSystem lake => new FeatureBounds
                {
                    MinX = lake.CenterX - lake.Radius,
                    MaxX = lake.CenterX + lake.Radius,
                    MinZ = lake.CenterZ - lake.Radius,
                    MaxZ = lake.CenterZ + lake.Radius,
                    MinY = Math.Max(0, lake.CenterY - (int)lake.Depth),
                    MaxY = Math.Min(255, lake.CenterY + 5)
                },
                _ => new FeatureBounds()
            };
        }
        
        /// <summary>
        /// Apply a terrain feature to the chunk
        /// </summary>
        private void ApplyTerrainFeature(ChunkData chunk, TerrainFeature feature, int chunkX, int chunkZ)
        {
            switch (feature.Type)
            {
                case TerrainFeatureType.Caves:
                    _caveGenerator.GenerateCaves(chunk, chunkX, chunkZ);
                    break;
                    
                case TerrainFeatureType.Rivers:
                    _riverGenerator.GenerateRivers(chunk, chunkX, chunkZ);
                    break;
                    
                case TerrainFeatureType.Lakes:
                    _lakeGenerator.GenerateLakes(chunk, chunkX, chunkZ);
                    break;
                    
                case TerrainFeatureType.Vegetation:
                    GenerateVegetation(chunk, feature, chunkX, chunkZ);
                    break;
                    
                case TerrainFeatureType.Structures:
                    GenerateStructures(chunk, feature, chunkX, chunkZ);
                    break;
                    
                case TerrainFeatureType.Ore:
                    GenerateOreDeposits(chunk, feature, chunkX, chunkZ);
                    break;
            }
        }
        
        /// <summary>
        /// Apply interactions between different terrain features
        /// </summary>
        private void ApplyFeatureInteractions(ChunkData chunk, List<TerrainFeature> features, int chunkX, int chunkZ)
        {
            // Find caves that intersect with water features
            var caves = features.FindAll(f => f.Type == TerrainFeatureType.Caves);
            var waterFeatures = features.FindAll(f => f.Type == TerrainFeatureType.Rivers || f.Type == TerrainFeatureType.Lakes);
            
            foreach (var cave in caves)
            {
                foreach (var water in waterFeatures)
                {
                    if (FeaturesIntersect(cave, water))
                    {
                        ApplyCaveWaterInteraction(chunk, cave, water);
                    }
                }
            }
            
            // Apply river-lake interactions
            var rivers = features.FindAll(f => f.Type == TerrainFeatureType.Rivers);
            var lakes = features.FindAll(f => f.Type == TerrainFeatureType.Lakes);
            
            foreach (var river in rivers)
            {
                foreach (var lake in lakes)
                {
                    if (FeaturesIntersect(river, lake))
                    {
                        ApplyRiverLakeInteraction(chunk, river, lake);
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if two terrain features intersect
        /// </summary>
        private bool FeaturesIntersect(TerrainFeature feature1, TerrainFeature feature2)
        {
            var bounds1 = feature1.Bounds;
            var bounds2 = feature2.Bounds;
            
            return !(bounds1.MaxX < bounds2.MinX || bounds1.MinX > bounds2.MaxX ||
                     bounds1.MaxZ < bounds2.MinZ || bounds1.MinZ > bounds2.MaxZ ||
                     bounds1.MaxY < bounds2.MinY || bounds1.MinY > bounds2.MaxY);
        }
        
        /// <summary>
        /// Apply interaction between caves and water features
        /// </summary>
        private void ApplyCaveWaterInteraction(ChunkData chunk, TerrainFeature cave, TerrainFeature water)
        {
            var caveSystem = cave.Data as CaveSystem;
            var waterFeature = water.Data;
            
            if (caveSystem == null) return;
            
            // Create underground water pools where caves intersect with water
            foreach (var chamber in caveSystem.Chambers)
            {
                if (IsNearWaterFeature(chamber, waterFeature))
                {
                    CreateUndergroundPool(chunk, chamber, waterFeature);
                }
            }
            
            // Create water streams in tunnels near water features
            foreach (var tunnel in caveSystem.Tunnels)
            {
                if (IsNearWaterFeature(tunnel, waterFeature))
                {
                    CreateCaveWaterStream(chunk, tunnel, waterFeature);
                }
            }
        }
        
        /// <summary>
        /// Check if a cave chamber is near a water feature
        /// </summary>
        private bool IsNearWaterFeature(object caveFeature, object waterFeature)
        {
            return waterFeature switch
            {
                RiverSystem river => IsNearRiver(caveFeature, river),
                LakeSystem lake => IsNearLake(caveFeature, lake),
                _ => false
            };
        }
        
        /// <summary>
        /// Check if cave feature is near a river
        /// </summary>
        private bool IsNearRiver(object caveFeature, RiverSystem river)
        {
            var position = GetFeaturePosition(caveFeature);
            foreach (var point in river.Path)
            {
                var distance = Math.Sqrt(Math.Pow(position.X - point.X, 2) + Math.Pow(position.Z - point.Z, 2));
                if (distance < RiverCaveInteractionRadius)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Check if cave feature is near a lake
        /// </summary>
        private bool IsNearLake(object caveFeature, LakeSystem lake)
        {
            var position = GetFeaturePosition(caveFeature);
            var distance = Math.Sqrt(Math.Pow(position.X - lake.CenterX, 2) + Math.Pow(position.Z - lake.CenterZ, 2));
            return distance < lake.Radius + LakeCaveInteractionRadius;
        }
        
        /// <summary>
        /// Get position of a cave feature
        /// </summary>
        private (int X, int Y, int Z) GetFeaturePosition(object feature)
        {
            return feature switch
            {
                CaveChamber chamber => (chamber.StartX, chamber.StartY, chamber.StartZ),
                CaveTunnel tunnel => (tunnel.StartX, tunnel.StartY, tunnel.StartZ),
                _ => (0, 0, 0)
            };
        }
        
        /// <summary>
        /// Create underground pool in cave chamber
        /// </summary>
        private void CreateUndergroundPool(ChunkData chunk, CaveChamber chamber, object waterFeature)
        {
            var waterLevel = _worldManager.GlobalWaterLevel - 5;
            
            for (int x = chamber.StartX; x < chamber.EndX; x++)
            {
                for (int z = chamber.StartZ; z < chamber.EndZ; z++)
                {
                    var distFromCenter = Math.Sqrt(
                        Math.Pow(x - (chamber.StartX + chamber.EndX) / 2.0, 2) +
                        Math.Pow(z - (chamber.StartZ + chamber.EndZ) / 2.0, 2));
                        
                    if (distFromCenter <= chamber.Radius * 0.6)
                    {
                        for (int y = chamber.StartY; y < waterLevel; y++)
                        {
                            if (IsInChunkBounds(x, y, z))
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create water stream in cave tunnel
        /// </summary>
        private void CreateCaveWaterStream(ChunkData chunk, CaveTunnel tunnel, object waterFeature)
        {
            var streamWidth = 1;
            
            foreach (var point in tunnel.Path)
            {
                if (IsInChunkBounds(point.X, point.Y - 1, point.Z))
                {
                    chunk.SetBlock(point.X, point.Y - 1, point.Z, BlockType.Water);
                }
            }
        }
        
        /// <summary>
        /// Apply interaction between rivers and lakes
        /// </summary>
        private void ApplyRiverLakeInteraction(ChunkData chunk, TerrainFeature river, TerrainFeature lake)
        {
            var riverSystem = river.Data as RiverSystem;
            var lakeSystem = lake.Data as LakeSystem;
            
            if (riverSystem == null || lakeSystem == null) return;
            
            // Create river deltas where rivers enter lakes
            foreach (var point in riverSystem.Path)
            {
                var distance = Math.Sqrt(Math.Pow(point.X - lakeSystem.CenterX, 2) + 
                                       Math.Pow(point.Z - lakeSystem.CenterZ, 2));
                
                if (distance < lakeSystem.Radius + RiverLakeInteractionRadius)
                {
                    CreateRiverDelta(chunk, point, lakeSystem);
                }
            }
        }
        
        /// <summary>
        /// Create river delta where river enters lake
        /// </summary>
        private void CreateRiverDelta(ChunkData chunk, RiverPoint point, LakeSystem lake)
        {
            var deltaRadius = (int)(point.Width * 1.5);
            
            for (int dx = -deltaRadius; dx <= deltaRadius; dx++)
            {
                for (int dz = -deltaRadius; dz <= deltaRadius; dz++)
                {
                    var distSq = dx * dx + dz * dz;
                    if (distSq <= deltaRadius * deltaRadius)
                    {
                        var x = point.X + dx;
                        var z = point.Z + dz;
                        
                        if (IsInChunkBounds(x, point.Y, z))
                        {
                            // Create sandy delta bottom
                            chunk.SetBlock(x, point.Y - 1, z, BlockType.Sand);
                            
                            // Shallow water
                            if (distSq > deltaRadius * deltaRadius * 0.3)
                            {
                                chunk.SetBlock(x, point.Y, z, BlockType.Water);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate vegetation for the chunk
        /// </summary>
        private void GenerateVegetation(ChunkData chunk, TerrainFeature feature, int chunkX, int chunkZ)
        {
            // This would be implemented with the vegetation generation system
            // For now, it's a placeholder
        }
        
        /// <summary>
        /// Generate structures for the chunk
        /// </summary>
        private void GenerateStructures(ChunkData chunk, TerrainFeature feature, int chunkX, int chunkZ)
        {
            // This would be implemented with the structure generation system
            // For now, it's a placeholder
        }
        
        /// <summary>
        /// Generate ore deposits for the chunk
        /// </summary>
        private void GenerateOreDeposits(ChunkData chunk, TerrainFeature feature, int chunkX, int chunkZ)
        {
            // This would be implemented with the ore generation system
            // For now, it's a placeholder
        }
        
        /// <summary>
        /// Optimize terrain after all features are applied
        /// </summary>
        private void OptimizeTerrain(ChunkData chunk, int chunkX, int chunkZ)
        {
            // Smooth terrain transitions
            SmoothTerrainTransitions(chunk);
            
            // Fix floating blocks
            FixFloatingBlocks(chunk);
            
            // Ensure structural integrity
            EnsureStructuralIntegrity(chunk);
        }
        
        /// <summary>
        /// Smooth transitions between different terrain features
        /// </summary>
        private void SmoothTerrainTransitions(ChunkData chunk)
        {
            // Apply smoothing algorithm to reduce harsh transitions
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    for (int y = 1; y < 255; y++)
                    {
                        if (ShouldSmoothBlock(chunk, x, y, z))
                        {
                            SmoothBlock(chunk, x, y, z);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if a block should be smoothed
        /// </summary>
        private bool ShouldSmoothBlock(ChunkData chunk, int x, int y, int z)
        {
            var centerBlock = chunk.GetBlock(x, y, z);
            if (centerBlock == BlockType.Air) return false;
            
            // Check neighboring blocks for harsh transitions
            var differentNeighbors = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        
                        var nx = x + dx;
                        var ny = y + dy;
                        var nz = z + dz;
                        
                        if (nx >= 0 && nx < 16 && ny >= 0 && ny < 256 && nz >= 0 && nz < 16)
                        {
                            var neighborBlock = chunk.GetBlock(nx, ny, nz);
                            if (neighborBlock != BlockType.Air && neighborBlock != centerBlock)
                            {
                                differentNeighbors++;
                            }
                        }
                    }
                }
            }
            
            return differentNeighbors > 4;
        }
        
        /// <summary>
        /// Smooth a specific block
        /// </summary>
        private void SmoothBlock(ChunkData chunk, int x, int y, int z)
        {
            // Replace with transitional block or adjust position
            // This is a simplified implementation
            var centerBlock = chunk.GetBlock(x, y, z);
            
            if (centerBlock == BlockType.Stone)
            {
                // Check if should become dirt or grass
                var airAbove = y < 255 && chunk.GetBlock(x, y + 1, z) == BlockType.Air;
                if (airAbove)
                {
                    chunk.SetBlock(x, y, z, BlockType.Dirt);
                    if (y + 1 < 255)
                    {
                        chunk.SetBlock(x, y + 1, z, BlockType.Grass);
                    }
                }
            }
        }
        
        /// <summary>
        /// Fix floating blocks in the chunk
        /// </summary>
        private void FixFloatingBlocks(ChunkData chunk)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 1; y < 255; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            var below = chunk.GetBlock(x, y - 1, z);
                            if (below == BlockType.Air)
                            {
                                // Block is floating, replace with air
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Ensure structural integrity of terrain
        /// </summary>
        private void EnsureStructuralIntegrity(ChunkData chunk)
        {
            // Check for and fix structural issues
            // This is a simplified implementation
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    for (int y = 1; y < 254; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Water)
                        {
                            // Ensure water has proper support
                            var below = chunk.GetBlock(x, y - 1, z);
                            if (below == BlockType.Air)
                            {
                                // Find the first solid block below
                                for (int checkY = y - 1; checkY >= 0; checkY--)
                                {
                                    var checkBlock = chunk.GetBlock(x, checkY, z);
                                    if (checkBlock != BlockType.Air && checkBlock != BlockType.Water)
                                    {
                                        // Fill gap with water
                                        for (int fillY = checkY + 1; fillY <= y; fillY++)
                                        {
                                            chunk.SetBlock(x, fillY, z, BlockType.Water);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if world coordinates are within chunk bounds
        /// </summary>
        private bool IsInChunkBounds(int worldX, int worldY, int worldZ)
        {
            return worldX >= 0 && worldX < 16 && 
                   worldY >= 0 && worldY < 256 && 
                   worldZ >= 0 && worldZ < 16;
        }
        
        /// <summary>
        /// Simplex noise implementation
        /// </summary>
        private static double SimplexNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var simplexValue = SimplexNoise2D(x * freq / scale, y * freq / scale, seed + i);
                total += simplexValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise calculation
        /// </summary>
        private static double SimplexNoise2D(double x, double y, int seed)
        {
            // Simplified 2D Simplex noise
            var s = (seed & 0xFF);
            var n = (int)x + (int)y * 57 + s * 131;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff)) / 1073741824.0;
        }
    }
    
    /// <summary>
    /// Types of terrain features
    /// </summary>
    public enum TerrainFeatureType
    {
        BaseTerrain,
        Lakes,
        Rivers,
        Caves,
        Vegetation,
        Structures,
        Ore
    }
    
    /// <summary>
    /// Represents a terrain feature with its data and bounds
    /// </summary>
    public class TerrainFeature
    {
        public TerrainFeatureType Type { get; set; }
        public object Data { get; set; }
        public FeatureBounds Bounds { get; set; }
    }
    
    /// <summary>
    /// Bounding box for a terrain feature
    /// </summary>
    public class FeatureBounds
    {
        public int MinX { get; set; }
        public int MaxX { get; set; }
        public int MinY { get; set; }
        public int MaxY { get; set; }
        public int MinZ { get; set; }
        public int MaxZ { get; set; }
    }
}
}
            return SimplexNoise.Generate(x * 0.001f, z * 0.001f, 4, 0.5, 1.0, 123456);
        }
        
        /// <summary>
        /// Generate erosion (medium-scale terrain features)
        /// </summary>
        private double GenerateErosion(int x, int z)
        {
            return SimplexNoise.Generate(x * 0.005f, z * 0.005f, 3, 0.6, 1.0, 234567);
        }
        
        /// <summary>
        /// Generate peaks (small-scale terrain features)
        /// </summary>
        private double GeneratePeaks(int x, int z)
        {
            return SimplexNoise.Generate(x * 0.02f, z * 0.02f, 2, 0.7, 1.0, 345678);
        }
        
        /// <summary>
        /// Generate detail (fine-scale terrain features)
        /// </summary>
        private double GenerateDetail(int x, int z)
        {
            return SimplexNoise.Generate(x * 0.1f, z * 0.1f, 1, 0.8, 1.0, 456789);
        }
        
        /// <summary>
        /// Generate terrain layers (stone, dirt, grass, etc.)
        /// </summary>
        private void GenerateTerrainLayers(ChunkData chunk, int x, int z, int surfaceHeight, int worldX, int worldZ)
        {
            // Generate stone layers
            for (int y = 0; y < surfaceHeight; y++)
            {
                var blockType = DetermineStoneType(y, surfaceHeight, worldX, worldZ);
                chunk.SetBlock(x, y, z, blockType);
            }
            
            // Generate surface layers
            GenerateSurfaceLayers(chunk, x, z, surfaceHeight, worldX, worldZ);
        }
        
        /// <summary>
        /// Determine stone type based on depth and location
        /// </summary>
        private BlockType DetermineStoneType(int y, int surfaceHeight, int worldX, int worldZ)
        {
            var depth = surfaceHeight - y;
            
            if (depth < 3)
                return BlockType.Dirt;
            else if (depth < 5)
                return BlockType.Stone;
            else if (y < 20)
                return BlockType.Deepslate;
            else
                return BlockType.Stone;
        }
        
        /// <summary>
        /// Generate surface layers (grass, sand, etc.)
        /// </summary>
        private void GenerateSurfaceLayers(ChunkData chunk, int x, int z, int surfaceHeight, int worldX, int worldZ)
        {
            var biome = DetermineBiome(worldX, worldZ);
            var moisture = GetMoisture(worldX, worldZ);
            var temperature = GetTemperature(worldX, worldZ);
            
            switch (biome)
            {
                case BiomeType.Plains:
                    chunk.SetBlock(x, surfaceHeight, z, BlockType.Grass);
                    chunk.SetBlock(x, surfaceHeight - 1, z, BlockType.Dirt);
                    chunk.SetBlock(x, surfaceHeight - 2, z, BlockType.Dirt);
                    break;
                    
                case BiomeType.Desert:
                    for (int i = 0; i < 3; i++)
                        chunk.SetBlock(x, surfaceHeight - i, z, BlockType.Sand);
                    break;
                    
                case BiomeType.Mountains:
                    chunk.SetBlock(x, surfaceHeight, z, BlockType.Stone);
                    if (_random.NextDouble() < 0.3)
                        chunk.SetBlock(x, surfaceHeight + 1, z, BlockType.Gravel);
                    break;
                    
                case BiomeType.Forest:
                    chunk.SetBlock(x, surfaceHeight, z, BlockType.Grass);
                    chunk.SetBlock(x, surfaceHeight - 1, z, BlockType.Dirt);
                    chunk.SetBlock(x, surfaceHeight - 2, z, BlockType.Dirt);
                    break;
                    
                case BiomeType.Ocean:
                    // Water will be added later
                    break;
                    
                default:
                    chunk.SetBlock(x, surfaceHeight, z, BlockType.Grass);
                    chunk.SetBlock(x, surfaceHeight - 1, z, BlockType.Dirt);
                    break;
            }
        }
        
        /// <summary>
        /// Determine biome type based on location
        /// </summary>
        private BiomeType DetermineBiome(int x, int z)
        {
            var temperature = GetTemperature(x, z);
            var moisture = GetMoisture(x, z);
            
            // Simple biome determination based on temperature and moisture
            if (temperature < 0.2)
                return BiomeType.Mountains;
            else if (temperature > 0.8 && moisture < 0.3)
                return BiomeType.Desert;
            else if (moisture > 0.7)
                return temperature > 0.5 ? BiomeType.Ocean : BiomeType.Forest;
            else
                return BiomeType.Plains;
        }
        
        /// <summary>
        /// Get moisture value for a location
        /// </summary>
        private double GetMoisture(int x, int z)
        {
            return (SimplexNoise.Generate(x * 0.003f, z * 0.003f, 2, 0.5, 1.0, 567890) + 1.0) / 2.0;
        }
        
        /// <summary>
        /// Get temperature value for a location
        /// </summary>
        private double GetTemperature(int x, int z)
        {
            return (SimplexNoise.Generate(x * 0.002f, z * 0.002f, 2, 0.5, 1.0, 678901) + 1.0) / 2.0;
        }
        
        /// <summary>
        /// Generate water features (rivers and lakes)
        /// </summary>
        private void GenerateWaterFeatures(ChunkData chunk, int chunkX, int chunkZ)
        {
            // Generate lakes first (they affect river generation)
            _lakeGenerator.GenerateLakes(chunk, chunkX, chunkZ);
            
            // Generate rivers
            _riverGenerator.GenerateRivers(chunk, chunkX, chunkZ);
            
            // Fill ocean areas
            FillOceanAreas(chunk, chunkX, chunkZ);
        }
        
        /// <summary>
        /// Fill ocean areas with water
        /// </summary>
        private void FillOceanAreas(ChunkData chunk, int chunkX, int chunkZ)
        {
            var waterLevel = _worldManager.GlobalWaterLevel;
            
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    var worldX = chunkX * ChunkSize + x;
                    var worldZ = chunkZ * ChunkSize + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    if (biome == BiomeType.Ocean)
                    {
                        // Find surface height
                        var surfaceY = FindSurfaceHeight(chunk, x, z);
                        
                        // Fill with water from surface down to water level
                        for (int y = surfaceY; y >= waterLevel; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Find the surface height at a position
        /// </summary>
        private int FindSurfaceHeight(ChunkData chunk, int x, int z)
        {
            for (int y = ChunkHeight - 1; y >= 0; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                    return y;
            }
            return _worldManager.GlobalWaterLevel;
        }
        
        /// <summary>
        /// Generate caves
        /// </summary>
        private void GenerateCaves(ChunkData chunk, int chunkX, int chunkZ)
        {
            _caveGenerator.GenerateCaves(chunk, chunkX, chunkZ);
        }
        
        /// <summary>
        /// Generate surface features (vegetation, trees, etc.)
        /// </summary>
        private void GenerateSurfaceFeatures(ChunkData chunk, int chunkX, int chunkZ)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    var worldX = chunkX * ChunkSize + x;
                    var worldZ = chunkZ * ChunkSize + z;
                    var surfaceY = FindSurfaceHeight(chunk, x, z);
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Generate vegetation based on biome
                    GenerateVegetation(chunk, x, surfaceY, z, biome, worldX, worldZ);
                }
            }
        }
        
        /// <summary>
        /// Generate vegetation based on biome
        /// </summary>
        private void GenerateVegetation(ChunkData chunk, int x, int surfaceY, int z, BiomeType biome, int worldX, int worldZ)
        {
            var vegetationRandom = _worldManager.GetChunkRandom(worldX, worldZ, 999);
            
            switch (biome)
            {
                case BiomeType.Forest:
                    if (vegetationRandom.NextDouble() < 0.1) // 10% chance
                        GenerateTree(chunk, x, surfaceY + 1, z);
                    else if (vegetationRandom.NextDouble() < 0.3) // 30% chance
                        GenerateGrass(chunk, x, surfaceY + 1, z);
                    break;
                    
                case BiomeType.Plains:
                    if (vegetationRandom.NextDouble() < 0.05) // 5% chance
                        GenerateTree(chunk, x, surfaceY + 1, z);
                    else if (vegetationRandom.NextDouble() < 0.4) // 40% chance
                        GenerateGrass(chunk, x, surfaceY + 1, z);
                    break;
                    
                case BiomeType.Desert:
                    if (vegetationRandom.NextDouble() < 0.02) // 2% chance
                        GenerateCactus(chunk, x, surfaceY + 1, z);
                    break;
            }
        }
        
        /// <summary>
        /// Generate a tree
        /// </summary>
        private void GenerateTree(ChunkData chunk, int x, int y, int z)
        {
            if (y + 6 >= ChunkHeight) return;
            
            // Generate trunk
            for (int i = 0; i < 4; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.OakLog);
            }
            
            // Generate leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    for (int dy = 3; dy <= 5; dy++)
                    {
                        var dist = Math.Sqrt(dx * dx + dz * dz + Math.Pow(dy - 4, 2));
                        if (dist <= 2.5)
                        {
                            var blockX = x + dx;
                            var blockY = y + dy;
                            var blockZ = z + dz;
                            
                            if (blockX >= 0 && blockX < ChunkSize && 
                                blockY >= 0 && blockY < ChunkHeight && 
                                blockZ >= 0 && blockZ < ChunkSize)
                            {
                                var currentBlock = chunk.GetBlock(blockX, blockY, blockZ);
                                if (currentBlock == BlockType.Air)
                                    chunk.SetBlock(blockX, blockY, blockZ, BlockType.OakLeaves);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate grass
        /// </summary>
        private void GenerateGrass(ChunkData chunk, int x, int y, int z)
        {
            if (y >= ChunkHeight) return;
            
            chunk.SetBlock(x, y, z, BlockType.Grass);
        }
        
        /// <summary>
        /// Generate cactus
        /// </summary>
        private void GenerateCactus(ChunkData chunk, int x, int y, int z)
        {
            if (y + 3 >= ChunkHeight) return;
            
            for (int i = 0; i < 3; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }
        
        /// <summary>
        /// Smooth chunk borders for seamless terrain
        /// </summary>
        private void SmoothChunkBorders(ChunkData chunk, int chunkX, int chunkZ)
        {
            // Get neighboring chunks for border smoothing
            var neighbors = GetNeighboringChunks(chunkX, chunkZ);
            
            // Smooth borders with neighbors
            SmoothWithNeighbors(chunk, neighbors, chunkX, chunkZ);
        }
        
        /// <summary>
        /// Get neighboring chunks for border smoothing
        /// </summary>
        private Dictionary<string, ChunkData> GetNeighboringChunks(int chunkX, int chunkZ)
        {
            var neighbors = new Dictionary<string, ChunkData>();
            
            // In a real implementation, this would load neighboring chunks
            // For now, we'll use a simplified approach
            
            return neighbors;
        }
        
        /// <summary>
        /// Smooth terrain with neighboring chunks
        /// </summary>
        private void SmoothWithNeighbors(ChunkData chunk, Dictionary<string, ChunkData> neighbors, int chunkX, int chunkZ)
        {
            // Smooth horizontal borders
            SmoothHorizontalBorders(chunk, neighbors, chunkX, chunkZ);
            
            // Smooth vertical borders
            SmoothVerticalBorders(chunk, neighbors, chunkX, chunkZ);
        }
        
        /// <summary>
        /// Smooth horizontal borders
        /// </summary>
        private void SmoothHorizontalBorders(ChunkData chunk, Dictionary<string, ChunkData> neighbors, int chunkX, int chunkZ)
        {
            // Implementation for smoothing horizontal borders
            // This would interpolate heights and blocks between chunks
        }
        
        /// <summary>
        /// Smooth vertical borders
        /// </summary>
        private void SmoothVerticalBorders(ChunkData chunk, Dictionary<string, ChunkData> neighbors, int chunkX, int chunkZ)
        {
            // Implementation for smoothing vertical borders
            // This would interpolate heights and blocks between chunks
        }
        
        /// <summary>
        /// Validate generated chunk
        /// </summary>
        private void ValidateChunk(ChunkData chunk)
        {
            // Check for invalid blocks
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkHeight; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Unknown)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }
                    }
                }
            }
            
            // Ensure proper water levels
            ValidateWaterLevels(chunk);
        }
        
        /// <summary>
        /// Validate water levels in chunk
        /// </summary>
        private void ValidateWaterLevels(ChunkData chunk)
        {
            var waterLevel = _worldManager.GlobalWaterLevel;
            
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    var surfaceY = FindSurfaceHeight(chunk, x, z);
                    
                    // If below water level and not water, make it water
                    if (surfaceY < waterLevel)
                    {
                        for (int y = surfaceY; y >= waterLevel; y--)
                        {
                            var block = chunk.GetBlock(x, y, z);
                            if (block == BlockType.Air)
                                chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate fallback chunk in case of errors
        /// </summary>
        private ChunkData GenerateFallbackChunk(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ, ChunkSize, ChunkHeight);
            
            // Generate simple flat terrain
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Stone);
                    }
                    for (int y = 64; y < 67; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Dirt);
                    }
                    chunk.SetBlock(x, 67, z, BlockType.Grass);
                }
            }
            
            return chunk;
        }
        
        /// <summary>
        /// 2D Simplex noise implementation
        /// </summary>
        private static double SimplexNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var simplexValue = SimplexNoise2D(x * freq / scale, y * freq / scale, seed + i);
                total += simplexValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise calculation
        /// </summary>
        private static double SimplexNoise2D(double x, double y, int seed)
        {
            // Simplified 2D Simplex noise
            var s = (seed & 0xFF);
            var n = (int)x + (int)y * 57 + s * 131;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff)) / 1073741824.0;
        }
    }
    
    /// <summary>
    /// Biome types for terrain generation
    /// </summary>
    public enum BiomeType
    {
        Plains,
        Forest,
        Desert,
        Mountains,
        Ocean
    }
}
