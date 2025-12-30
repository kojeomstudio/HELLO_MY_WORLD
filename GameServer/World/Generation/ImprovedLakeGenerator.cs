using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced lake generation system with realistic lake shapes,
    /// proper depth variations, and natural integration with terrain.
    /// </summary>
    public class ImprovedLakeGenerator
    {
        private readonly LakeGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<LakeSystem>> _lakeSystems;
        private readonly Dictionary<int, List<LakeFeature>> _lakeFeatures;
        
        public ImprovedLakeGenerator(LakeGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _lakeSystems = new Dictionary<int, List<LakeSystem>>();
            _lakeFeatures = new Dictionary<int, List<LakeFeature>>();
        }
        
        /// <summary>
        /// Generate lakes for a chunk
        /// </summary>
        public void GenerateLakes(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _lakeSystems[chunkKey] = new List<LakeSystem>();
            _lakeFeatures[chunkKey] = new List<LakeFeature>();
            
            // Generate lake systems for this chunk
            GenerateLakeSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate lake features
            GenerateLakeFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect lakes to rivers
            ConnectLakesToRivers(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add lake shores
            AddLakeShores(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add lake vegetation
            AddLakeVegetation(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate lake systems
        /// </summary>
        private void GenerateLakeSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Check if this chunk should contain a lake
            if (ShouldContainLake(chunkX, chunkZ, heightMap))
            {
                var lakeSystem = new LakeSystem
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = GetLakeSurfaceHeight(chunkX, chunkZ, heightMap),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    RadiusX = _random.Next(_settings.MinLakeRadius, _settings.MaxLakeRadius + 1),
                    RadiusZ = _random.Next(_settings.MinLakeRadius, _settings.MaxLakeRadius + 1),
                    Depth = _random.Next(_settings.MinLakeDepth, _settings.MaxLakeDepth + 1),
                    Type = (LakeType)_random.Next(Enum.GetValues(typeof(LakeType)).Length),
                    WaterLevel = GetLakeWaterLevel(chunkX, chunkZ, heightMap)
                };
                
                _lakeSystems[chunkKey].Add(lakeSystem);
                
                // Generate the lake
                GenerateLake(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single lake
        /// </summary>
        private void GenerateLake(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Create the lake basin
            CreateLakeBasin(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            
            // Fill with water
            FillLakeWithWater(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            
            // Create lake bed
            CreateLakeBed(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate lake features
        /// </summary>
        private void GenerateLakeFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate features for each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                // Generate islands for larger lakes
                if (lake.RadiusX > _settings.IslandThreshold || lake.RadiusZ > _settings.IslandThreshold)
                {
                    var islandCount = _random.Next(1, _settings.MaxIslandsPerLake + 1);
                    
                    for (int i = 0; i < islandCount; i++)
                    {
                        var island = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.Island,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX/2, lake.RadiusX/2 + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ/2, lake.RadiusZ/2 + 1),
                            RadiusX = _random.Next(_settings.MinIslandRadius, _settings.MaxIslandRadius + 1),
                            RadiusZ = _random.Next(_settings.MinIslandRadius, _settings.MaxIslandRadius + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(island);
                        AddIslandFeature(island, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
                
                // Generate lily pads
                if (lake.Type == LakeType.Plain || lake.Type == LakeType.Swamp)
                {
                    var lilyPadCount = _random.Next(_settings.MinLilyPadsPerLake, _settings.MaxLilyPadsPerLake + 1);
                    
                    for (int i = 0; i < lilyPadCount; i++)
                    {
                        var lilyPad = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.LilyPad,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX, lake.RadiusX + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ, lake.RadiusZ + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(lilyPad);
                        AddLilyPadFeature(lilyPad, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
                
                // Generate reeds
                if (lake.Type == LakeType.Swamp || lake.Type == LakeType.River)
                {
                    var reedCount = _random.Next(_settings.MinReedsPerLake, _settings.MaxReedsPerLake + 1);
                    
                    for (int i = 0; i < reedCount; i++)
                    {
                        var reed = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.Reeds,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX, lake.RadiusX + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ, lake.RadiusZ + 1),
                            Height = _random.Next(_settings.MinReedHeight, _settings.MaxReedHeight + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(reed);
                        AddReedsFeature(reed, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Connect lakes to rivers
        /// </summary>
        private void ConnectLakesToRivers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // For each lake, check if it should be connected to a river
            foreach (var lake in _lakeSystems[chunkKey])
            {
                if (_random.NextDouble() < _settings.RiverConnectionProbability)
                {
                    // Create an outlet or inlet
                    var connection = new LakeFeature
                    {
                        Id = _random.Next(),
                        LakeSystemId = lake.Id,
                        Type = LakeFeatureType.RiverConnection,
                        PositionX = lake.CenterX,
                        PositionY = lake.WaterLevel,
                        PositionZ = lake.CenterZ,
                        Width = _random.Next(_settings.MinRiverWidth, _settings.MaxRiverWidth + 1),
                        Direction = _random.NextDouble() * Math.PI * 2
                    };
                    
                    _lakeFeatures[chunkKey].Add(connection);
                    AddRiverConnectionFeature(connection, lake, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add lake shores
        /// </summary>
        private void AddLakeShores(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add shores to each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                AddLakeShoresToLake(lake, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Add lake vegetation
        /// </summary>
        private void AddLakeVegetation(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add vegetation to each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                AddVegetationToLake(lake, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Create a lake basin
        /// </summary>
        private void CreateLakeBasin(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Calculate depth based on distance from center
                        var depthFactor = 1.0 - distance;
                        var localDepth = (int)(lakeSystem.Depth * depthFactor);
                        
                        // Modify terrain to create basin
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block should be part of the basin
                                if (y >= lakeSystem.WaterLevel - localDepth && y <= lakeSystem.WaterLevel)
                                {
                                    // Replace with air for now (will be filled with water later)
                                    blockTypes[index] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fill a lake with water
        /// </summary>
        private void FillLakeWithWater(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Fill with water
                        for (int y = lakeSystem.WaterLevel - lakeSystem.Depth; y <= lakeSystem.WaterLevel; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is air (part of the basin)
                                if (blockTypes[index] == 0)
                                {
                                    blockTypes[index] = (int)BlockType.Water;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create a lake bed
        /// </summary>
        private void CreateLakeBed(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Calculate depth based on distance from center
                        var depthFactor = 1.0 - distance;
                        var localDepth = (int)(lakeSystem.Depth * depthFactor);
                        
                        // Create lake bed
                        var bedY = lakeSystem.WaterLevel - localDepth;
                        var index = bedY * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            // Use different materials for lake bed based on depth
                            if (localDepth < 2)
                            {
                                blockTypes[index] = (int)BlockType.Sand; // Sandy bottom for shallow areas
                            }
                            else if (localDepth < 5)
                            {
                                blockTypes[index] = (int)BlockType.Gravel; // Gravel for medium depth
                            }
                            else
                            {
                                blockTypes[index] = (int)BlockType.Dirt; // Dirt for deep areas
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add an island feature
        /// </summary>
        private void AddIslandFeature(LakeFeature island, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the island is within this chunk
            if (!IsPositionInChunk(island.PositionX, island.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(island.PositionX - chunkX * 16);
            var localZ = (int)(island.PositionZ - chunkZ * 16);
            
            // Create the island
            for (int x = -island.RadiusX; x <= island.RadiusX; x++)
            {
                for (int z = -island.RadiusZ; z <= island.RadiusZ; z++)
                {
                    var dx = x / (double)island.RadiusX;
                    var dz = z / (double)island.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        var worldX = localX + x;
                        var worldZ = localZ + z;
                        
                        if (worldX >= 0 && worldX < 16 && worldZ >= 0 && worldZ < 16)
                        {
                            // Create the island terrain
                            for (int y = island.PositionY; y <= island.PositionY + 3; y++)
                            {
                                var index = y * 16 * 16 + worldZ * 16 + worldX;
                                
                                if (index >= 0 && index < blockTypes.Length)
                                {
                                    if (y == island.PositionY)
                                    {
                                        // Grass on top
                                        blockTypes[index] = (int)BlockType.Grass;
                                    }
                                    else if (y < island.PositionY + 2)
                                    {
                                        // Dirt underneath
                                        blockTypes[index] = (int)BlockType.Dirt;
                                    }
                                    else
                                    {
                                        // Stone at the bottom
                                        blockTypes[index] = (int)BlockType.Stone;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a lily pad feature
        /// </summary>
        private void AddLilyPadFeature(LakeFeature lilyPad, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the lily pad is within this chunk
            if (!IsPositionInChunk(lilyPad.PositionX, lilyPad.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(lilyPad.PositionX - chunkX * 16);
            var localZ = (int)(lilyPad.PositionZ - chunkZ * 16);
            
            // Place the lily pad
            var index = lilyPad.PositionY * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this position is water
                if (blockTypes[index] == (int)BlockType.Water)
                {
                    // Replace with lily pad
                    blockTypes[index] = (int)BlockType.LilyPad;
                }
            }
        }
        
        /// <summary>
        /// Add reeds feature
        /// </summary>
        private void AddReedsFeature(LakeFeature reed, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the reeds are within this chunk
            if (!IsPositionInChunk(reed.PositionX, reed.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(reed.PositionX - chunkX * 16);
            var localZ = (int)(reed.PositionZ - chunkZ * 16);
            
            // Place the reeds
            for (int y = reed.PositionY; y < reed.PositionY + reed.Height; y++)
            {
                var index = y * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    // Check if this position is water or air
                    if (blockTypes[index] == (int)BlockType.Water || blockTypes[index] == 0)
                    {
                        // Place reed
                        blockTypes[index] = (int)BlockType.Reeds;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a river connection feature
        /// </summary>
        private void AddRiverConnectionFeature(LakeFeature connection, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the connection is within this chunk
            if (!IsPositionInChunk(connection.PositionX, connection.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(connection.PositionX - chunkX * 16);
            var localZ = (int)(connection.PositionZ - chunkZ * 16);
            
            // Create a channel for the river connection
            var length = 20; // Length of the channel within this chunk
            var direction = connection.Direction;
            
            for (int i = 0; i < length; i++)
            {
                var x = localX + (int)(Math.Cos(direction) * i);
                var z = localZ + (int)(Math.Sin(direction) * i);
                
                // Check if this position is within the chunk
                if (x >= 0 && x < 16 && z >= 0 && z < 16)
                {
                    // Create the channel
                    for (int wx = -connection.Width / 2; wx <= connection.Width / 2; wx++)
                    {
                        for (int wz = -connection.Width / 2; wz <= connection.Width / 2; wz++)
                        {
                            var channelX = x + wx;
                            var channelZ = z + wz;
                            
                            // Check if this position is within the lake
                            var dx = (channelX - localX) / (double)lake.RadiusX;
                            var dz = (channelZ - localZ) / (double)lake.RadiusZ;
                            var distance = dx * dx + dz * dz;
                            
                            if (distance > 1.0) // Outside the lake
                            {
                                for (int y = lake.WaterLevel - 2; y <= lake.WaterLevel; y++)
                                {
                                    var index = y * 16 * 16 + channelZ * 16 + channelX;
                                    
                                    if (index >= 0 && index < blockTypes.Length)
                                    {
                                        if (y >= lake.WaterLevel - 1)
                                        {
                                            // Water at the top
                                            blockTypes[index] = (int)BlockType.Water;
                                        }
                                        else
                                        {
                                            // Sand/gravel for the channel bed
                                            blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Gravel;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add shores to a lake
        /// </summary>
        private void AddLakeShoresToLake(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is near the lake edge
                    var dx = (worldX - lake.CenterX) / (double)lake.RadiusX;
                    var dz = (worldZ - lake.CenterZ) / (double)lake.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    // Add shores at the edge of the lake
                    if (distance > 0.9 && distance <= 1.1)
                    {
                        for (int y = lake.WaterLevel - 1; y <= lake.WaterLevel + 2; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                                
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is air or water
                                if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                                {
                                    // Use sand or dirt for shores
                                    blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Dirt;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a lake
        /// </summary>
        private void AddVegetationToLake(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add vegetation based on lake type
            switch (lake.Type)
            {
                case LakeType.Plain:
                    AddPlainLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.Swamp:
                    AddSwampLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.Mountain:
                    AddMountainLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.River:
                    AddRiverLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
        }
        
        /// <summary>
        /// Add vegetation to a plain lake
        /// </summary>
        private void AddPlainLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add some grass and flowers around the lake
            var vegetationCount = _random.Next(5, 15);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a swamp lake
        /// </summary>
        private void AddSwampLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add more reeds and trees around the lake
            var vegetationCount = _random.Next(10, 25);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add reeds or trees
                    if (_random.NextDouble() < 0.7)
                    {
                        AddReedsAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddTreeAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a mountain lake
        /// </summary>
        private void AddMountainLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add fewer plants, mostly rocks and some hardy vegetation
            var vegetationCount = _random.Next(3, 8);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add rocks or small plants
                    if (_random.NextDouble() < 0.6)
                    {
                        AddRockAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a river lake
        /// </summary>
        private void AddRiverLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add vegetation similar to plain lakes but with more reeds
            var vegetationCount = _random.Next(8, 18);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add reeds or regular vegetation
                    if (_random.NextDouble() < 0.5)
                    {
                        AddReedsAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation at a position
        /// </summary>
        private void AddVegetationAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Place the vegetation
            var index = y * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this block is air
                if (blockTypes[index] == 0)
                {
                    // Use different vegetation based on random chance
                    var rand = _random.NextDouble();
                    if (rand < 0.3)
                    {
                        blockTypes[index] = (int)BlockType.Grass; // Tall grass
                    }
                    else if (rand < 0.6)
                    {
                        blockTypes[index] = (int)BlockType.Flower; // Flower
                    }
                    else
                    {
                        blockTypes[index] = (int)BlockType.Sapling; // Sapling
                    }
                }
            }
        }
        
        /// <summary>
        /// Add reeds at a position
        /// </summary>
        private void AddReedsAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            var height = _random.Next(2, 4);
            
            // Place the reeds
            for (int i = 0; i < height; i++)
            {
                var index = (y + i) * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    // Check if this block is air or water
                    if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                    {
                        blockTypes[index] = (int)BlockType.Reeds;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a tree at a position
        /// </summary>
        private void AddTreeAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            var treeHeight = _random.Next(4, 7);
            
            // Place the tree
            for (int i = 0; i < treeHeight; i++)
            {
                var index = (y + i) * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    if (i < treeHeight - 2)
                    {
                        // Trunk
                        blockTypes[index] = (int)BlockType.Wood;
                    }
                    else
                    {
                        // Leaves
                        blockTypes[index] = (int)BlockType.Leaves;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a rock at a position
        /// </summary>
        private void AddRockAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Place the rock
            var index = y * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this block is air
                if (blockTypes[index] == 0)
                {
                    blockTypes[index] = (int)BlockType.Stone;
                }
            }
        }
        
        #region Utility Methods
        
        /// <summary>
        /// Check if a chunk should contain a lake
        /// </summary>
        private bool ShouldContainLake(int chunkX, int chunkZ, int[] heightMap)
        {
            // Use a noise function to determine if this chunk should contain a lake
            var noise = SimpleNoise(chunkX * 0.1, chunkZ * 0.1, _settings.Seed);
            var avgHeight = GetAverageHeight(chunkX, chunkZ, heightMap);
            
            // More likely to have lakes at lower elevations
            var heightFactor = avgHeight < 64 ? 1.5 : 1.0;
            return noise * heightFactor > _settings.LakeGenerationThreshold;
        }
        
        /// <summary>
        /// Get the surface height for a lake
        /// </summary>
        private int GetLakeSurfaceHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            // Find a low point in the chunk for the lake surface
            var minHeight = 255;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        minHeight = Math.Min(minHeight, heightMap[index]);
                    }
                }
            }
            
            // Return a height slightly above the minimum
            return minHeight + _random.Next(1, 4);
        }
        
        /// <summary>
        /// Get the water level for a lake
        /// </summary>
        private int GetLakeWaterLevel(int chunkX, int chunkZ, int[] heightMap)
        {
            // The water level should be slightly below the surface
            var surfaceHeight = GetLakeSurfaceHeight(chunkX, chunkZ, heightMap);
            return surfaceHeight - 1;
        }
        
        /// <summary>
        /// Get the average height of a chunk
        /// </summary>
        private int GetAverageHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            var sum = 0;
            var count = 0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        sum += heightMap[index];
                        count++;
                    }
                }
            }
            
            return count > 0 ? sum / count : 64;
        }
        
        /// <summary>
        /// Check if a position is within a chunk
        /// </summary>
        private bool IsPositionInChunk(double x, double z, int chunkX, int chunkZ)
        {
            var localX = x - chunkX * 16;
            var localZ = z - chunkZ * 16;
            return localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16;
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
        
        #endregion
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Lake system information
    /// </summary>
    public class LakeSystem
    {
        public int Id { get; set; }
        public double CenterX { get; set; }
        public int CenterY { get; set; }
        public double CenterZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusZ { get; set; }
        public int Depth { get; set; }
        public LakeType Type { get; set; }
        public int WaterLevel { get; set; }
    }
    
    /// <summary>
    /// Lake feature information
    /// </summary>
    public class LakeFeature
    {
        public int Id { get; set; }
        public int LakeSystemId { get; set; }
        public LakeFeatureType Type { get; set; }
        public double PositionX { get; set; }
        public int PositionY { get; set; }
        public double PositionZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusZ { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double Direction { get; set; }
    }
    
    /// <summary>
    /// Lake types
    /// </summary>
    public enum LakeType
    {
        Plain,
        Swamp,
        Mountain,
        River
    }
    
    /// <summary>
    /// Lake feature types
    /// </summary>
    public enum LakeFeatureType
    {
        Island,
        LilyPad,
        Reeds,
        RiverConnection
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
    /// Enhanced lake generation system with realistic lake shapes,
    /// proper depth variations, and natural integration with terrain.
    /// </summary>
    public class ImprovedLakeGenerator
    {
        private readonly LakeGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<LakeSystem>> _lakeSystems;
        private readonly Dictionary<int, List<LakeFeature>> _lakeFeatures;
        
        public ImprovedLakeGenerator(LakeGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _lakeSystems = new Dictionary<int, List<LakeSystem>>();
            _lakeFeatures = new Dictionary<int, List<LakeFeature>>();
        }
        
        /// <summary>
        /// Generate lakes for a chunk
        /// </summary>
        public void GenerateLakes(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _lakeSystems[chunkKey] = new List<LakeSystem>();
            _lakeFeatures[chunkKey] = new List<LakeFeature>();
            
            // Generate lake systems for this chunk
            GenerateLakeSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate lake features
            GenerateLakeFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect lakes to rivers
            ConnectLakesToRivers(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add lake shores
            AddLakeShores(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add lake vegetation
            AddLakeVegetation(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate lake systems
        /// </summary>
        private void GenerateLakeSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Check if this chunk should contain a lake
            if (ShouldContainLake(chunkX, chunkZ, heightMap))
            {
                var lakeSystem = new LakeSystem
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = GetLakeSurfaceHeight(chunkX, chunkZ, heightMap),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    RadiusX = _random.Next(_settings.MinLakeRadius, _settings.MaxLakeRadius + 1),
                    RadiusZ = _random.Next(_settings.MinLakeRadius, _settings.MaxLakeRadius + 1),
                    Depth = _random.Next(_settings.MinLakeDepth, _settings.MaxLakeDepth + 1),
                    Type = (LakeType)_random.Next(Enum.GetValues(typeof(LakeType)).Length),
                    WaterLevel = GetLakeWaterLevel(chunkX, chunkZ, heightMap)
                };
                
                _lakeSystems[chunkKey].Add(lakeSystem);
                
                // Generate the lake
                GenerateLake(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single lake
        /// </summary>
        private void GenerateLake(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Create the lake basin
            CreateLakeBasin(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            
            // Fill with water
            FillLakeWithWater(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
            
            // Create lake bed
            CreateLakeBed(lakeSystem, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate lake features
        /// </summary>
        private void GenerateLakeFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate features for each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                // Generate islands for larger lakes
                if (lake.RadiusX > _settings.IslandThreshold || lake.RadiusZ > _settings.IslandThreshold)
                {
                    var islandCount = _random.Next(1, _settings.MaxIslandsPerLake + 1);
                    
                    for (int i = 0; i < islandCount; i++)
                    {
                        var island = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.Island,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX/2, lake.RadiusX/2 + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ/2, lake.RadiusZ/2 + 1),
                            RadiusX = _random.Next(_settings.MinIslandRadius, _settings.MaxIslandRadius + 1),
                            RadiusZ = _random.Next(_settings.MinIslandRadius, _settings.MaxIslandRadius + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(island);
                        AddIslandFeature(island, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
                
                // Generate lily pads
                if (lake.Type == LakeType.Plain || lake.Type == LakeType.Swamp)
                {
                    var lilyPadCount = _random.Next(_settings.MinLilyPadsPerLake, _settings.MaxLilyPadsPerLake + 1);
                    
                    for (int i = 0; i < lilyPadCount; i++)
                    {
                        var lilyPad = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.LilyPad,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX, lake.RadiusX + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ, lake.RadiusZ + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(lilyPad);
                        AddLilyPadFeature(lilyPad, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
                
                // Generate reeds
                if (lake.Type == LakeType.Swamp || lake.Type == LakeType.River)
                {
                    var reedCount = _random.Next(_settings.MinReedsPerLake, _settings.MaxReedsPerLake + 1);
                    
                    for (int i = 0; i < reedCount; i++)
                    {
                        var reed = new LakeFeature
                        {
                            Id = _random.Next(),
                            LakeSystemId = lake.Id,
                            Type = LakeFeatureType.Reeds,
                            PositionX = lake.CenterX + _random.Next(-lake.RadiusX, lake.RadiusX + 1),
                            PositionY = lake.WaterLevel,
                            PositionZ = lake.CenterZ + _random.Next(-lake.RadiusZ, lake.RadiusZ + 1),
                            Height = _random.Next(_settings.MinReedHeight, _settings.MaxReedHeight + 1)
                        };
                        
                        _lakeFeatures[chunkKey].Add(reed);
                        AddReedsFeature(reed, lake, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Connect lakes to rivers
        /// </summary>
        private void ConnectLakesToRivers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // For each lake, check if it should be connected to a river
            foreach (var lake in _lakeSystems[chunkKey])
            {
                if (_random.NextDouble() < _settings.RiverConnectionProbability)
                {
                    // Create an outlet or inlet
                    var connection = new LakeFeature
                    {
                        Id = _random.Next(),
                        LakeSystemId = lake.Id,
                        Type = LakeFeatureType.RiverConnection,
                        PositionX = lake.CenterX,
                        PositionY = lake.WaterLevel,
                        PositionZ = lake.CenterZ,
                        Width = _random.Next(_settings.MinRiverWidth, _settings.MaxRiverWidth + 1),
                        Direction = _random.NextDouble() * Math.PI * 2
                    };
                    
                    _lakeFeatures[chunkKey].Add(connection);
                    AddRiverConnectionFeature(connection, lake, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add lake shores
        /// </summary>
        private void AddLakeShores(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add shores to each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                AddLakeShoresToLake(lake, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Add lake vegetation
        /// </summary>
        private void AddLakeVegetation(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add vegetation to each lake in this chunk
            foreach (var lake in _lakeSystems[chunkKey])
            {
                AddVegetationToLake(lake, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Create a lake basin
        /// </summary>
        private void CreateLakeBasin(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Calculate depth based on distance from center
                        var depthFactor = 1.0 - distance;
                        var localDepth = (int)(lakeSystem.Depth * depthFactor);
                        
                        // Modify terrain to create basin
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block should be part of the basin
                                if (y >= lakeSystem.WaterLevel - localDepth && y <= lakeSystem.WaterLevel)
                                {
                                    // Replace with air for now (will be filled with water later)
                                    blockTypes[index] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Fill a lake with water
        /// </summary>
        private void FillLakeWithWater(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Fill with water
                        for (int y = lakeSystem.WaterLevel - lakeSystem.Depth; y <= lakeSystem.WaterLevel; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is air (part of the basin)
                                if (blockTypes[index] == 0)
                                {
                                    blockTypes[index] = (int)BlockType.Water;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create a lake bed
        /// </summary>
        private void CreateLakeBed(LakeSystem lakeSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the lake
                    var dx = (worldX - lakeSystem.CenterX) / (double)lakeSystem.RadiusX;
                    var dz = (worldZ - lakeSystem.CenterZ) / (double)lakeSystem.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        // Calculate depth based on distance from center
                        var depthFactor = 1.0 - distance;
                        var localDepth = (int)(lakeSystem.Depth * depthFactor);
                        
                        // Create lake bed
                        var bedY = lakeSystem.WaterLevel - localDepth;
                        var index = bedY * 16 * 16 + z * 16 + x;
                        
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            // Use different materials for lake bed based on depth
                            if (localDepth < 2)
                            {
                                blockTypes[index] = (int)BlockType.Sand; // Sandy bottom for shallow areas
                            }
                            else if (localDepth < 5)
                            {
                                blockTypes[index] = (int)BlockType.Gravel; // Gravel for medium depth
                            }
                            else
                            {
                                blockTypes[index] = (int)BlockType.Dirt; // Dirt for deep areas
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add an island feature
        /// </summary>
        private void AddIslandFeature(LakeFeature island, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the island is within this chunk
            if (!IsPositionInChunk(island.PositionX, island.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(island.PositionX - chunkX * 16);
            var localZ = (int)(island.PositionZ - chunkZ * 16);
            
            // Create the island
            for (int x = -island.RadiusX; x <= island.RadiusX; x++)
            {
                for (int z = -island.RadiusZ; z <= island.RadiusZ; z++)
                {
                    var dx = x / (double)island.RadiusX;
                    var dz = z / (double)island.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    if (distance <= 1.0)
                    {
                        var worldX = localX + x;
                        var worldZ = localZ + z;
                        
                        if (worldX >= 0 && worldX < 16 && worldZ >= 0 && worldZ < 16)
                        {
                            // Create the island terrain
                            for (int y = island.PositionY; y <= island.PositionY + 3; y++)
                            {
                                var index = y * 16 * 16 + worldZ * 16 + worldX;
                                
                                if (index >= 0 && index < blockTypes.Length)
                                {
                                    if (y == island.PositionY)
                                    {
                                        // Grass on top
                                        blockTypes[index] = (int)BlockType.Grass;
                                    }
                                    else if (y < island.PositionY + 2)
                                    {
                                        // Dirt underneath
                                        blockTypes[index] = (int)BlockType.Dirt;
                                    }
                                    else
                                    {
                                        // Stone at the bottom
                                        blockTypes[index] = (int)BlockType.Stone;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a lily pad feature
        /// </summary>
        private void AddLilyPadFeature(LakeFeature lilyPad, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the lily pad is within this chunk
            if (!IsPositionInChunk(lilyPad.PositionX, lilyPad.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(lilyPad.PositionX - chunkX * 16);
            var localZ = (int)(lilyPad.PositionZ - chunkZ * 16);
            
            // Place the lily pad
            var index = lilyPad.PositionY * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this position is water
                if (blockTypes[index] == (int)BlockType.Water)
                {
                    // Replace with lily pad
                    blockTypes[index] = (int)BlockType.LilyPad;
                }
            }
        }
        
        /// <summary>
        /// Add reeds feature
        /// </summary>
        private void AddReedsFeature(LakeFeature reed, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the reeds are within this chunk
            if (!IsPositionInChunk(reed.PositionX, reed.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(reed.PositionX - chunkX * 16);
            var localZ = (int)(reed.PositionZ - chunkZ * 16);
            
            // Place the reeds
            for (int y = reed.PositionY; y < reed.PositionY + reed.Height; y++)
            {
                var index = y * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    // Check if this position is water or air
                    if (blockTypes[index] == (int)BlockType.Water || blockTypes[index] == 0)
                    {
                        // Place reed
                        blockTypes[index] = (int)BlockType.Reeds;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a river connection feature
        /// </summary>
        private void AddRiverConnectionFeature(LakeFeature connection, LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the connection is within this chunk
            if (!IsPositionInChunk(connection.PositionX, connection.PositionZ, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(connection.PositionX - chunkX * 16);
            var localZ = (int)(connection.PositionZ - chunkZ * 16);
            
            // Create a channel for the river connection
            var length = 20; // Length of the channel within this chunk
            var direction = connection.Direction;
            
            for (int i = 0; i < length; i++)
            {
                var x = localX + (int)(Math.Cos(direction) * i);
                var z = localZ + (int)(Math.Sin(direction) * i);
                
                // Check if this position is within the chunk
                if (x >= 0 && x < 16 && z >= 0 && z < 16)
                {
                    // Create the channel
                    for (int wx = -connection.Width / 2; wx <= connection.Width / 2; wx++)
                    {
                        for (int wz = -connection.Width / 2; wz <= connection.Width / 2; wz++)
                        {
                            var channelX = x + wx;
                            var channelZ = z + wz;
                            
                            // Check if this position is within the lake
                            var dx = (channelX - localX) / (double)lake.RadiusX;
                            var dz = (channelZ - localZ) / (double)lake.RadiusZ;
                            var distance = dx * dx + dz * dz;
                            
                            if (distance > 1.0) // Outside the lake
                            {
                                for (int y = lake.WaterLevel - 2; y <= lake.WaterLevel; y++)
                                {
                                    var index = y * 16 * 16 + channelZ * 16 + channelX;
                                    
                                    if (index >= 0 && index < blockTypes.Length)
                                    {
                                        if (y >= lake.WaterLevel - 1)
                                        {
                                            // Water at the top
                                            blockTypes[index] = (int)BlockType.Water;
                                        }
                                        else
                                        {
                                            // Sand/gravel for the channel bed
                                            blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Gravel;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add shores to a lake
        /// </summary>
        private void AddLakeShoresToLake(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is near the lake edge
                    var dx = (worldX - lake.CenterX) / (double)lake.RadiusX;
                    var dz = (worldZ - lake.CenterZ) / (double)lake.RadiusZ;
                    var distance = dx * dx + dz * dz;
                    
                    // Add shores at the edge of the lake
                    if (distance > 0.9 && distance <= 1.1)
                    {
                        for (int y = lake.WaterLevel - 1; y <= lake.WaterLevel + 2; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                                
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is air or water
                                if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                                {
                                    // Use sand or dirt for shores
                                    blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Dirt;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a lake
        /// </summary>
        private void AddVegetationToLake(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add vegetation based on lake type
            switch (lake.Type)
            {
                case LakeType.Plain:
                    AddPlainLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.Swamp:
                    AddSwampLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.Mountain:
                    AddMountainLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case LakeType.River:
                    AddRiverLakeVegetation(lake, chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
        }
        
        /// <summary>
        /// Add vegetation to a plain lake
        /// </summary>
        private void AddPlainLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add some grass and flowers around the lake
            var vegetationCount = _random.Next(5, 15);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a swamp lake
        /// </summary>
        private void AddSwampLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add more reeds and trees around the lake
            var vegetationCount = _random.Next(10, 25);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add reeds or trees
                    if (_random.NextDouble() < 0.7)
                    {
                        AddReedsAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddTreeAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a mountain lake
        /// </summary>
        private void AddMountainLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add fewer plants, mostly rocks and some hardy vegetation
            var vegetationCount = _random.Next(3, 8);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add rocks or small plants
                    if (_random.NextDouble() < 0.6)
                    {
                        AddRockAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation to a river lake
        /// </summary>
        private void AddRiverLakeVegetation(LakeSystem lake, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Add vegetation similar to plain lakes but with more reeds
            var vegetationCount = _random.Next(8, 18);
            
            for (int i = 0; i < vegetationCount; i++)
            {
                var x = lake.CenterX + _random.Next(-lake.RadiusX * 2, lake.RadiusX * 2 + 1);
                var z = lake.CenterZ + _random.Next(-lake.RadiusZ * 2, lake.RadiusZ * 2 + 1);
                
                // Check if this position is near the lake shore
                var dx = (x - lake.CenterX) / (double)lake.RadiusX;
                var dz = (z - lake.CenterZ) / (double)lake.RadiusZ;
                var distance = dx * dx + dz * dz;
                
                if (distance > 0.8 && distance <= 1.5)
                {
                    // Add reeds or regular vegetation
                    if (_random.NextDouble() < 0.5)
                    {
                        AddReedsAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                    else
                    {
                        AddVegetationAtPosition(x, z, lake.WaterLevel + 1, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add vegetation at a position
        /// </summary>
        private void AddVegetationAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Place the vegetation
            var index = y * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this block is air
                if (blockTypes[index] == 0)
                {
                    // Use different vegetation based on random chance
                    var rand = _random.NextDouble();
                    if (rand < 0.3)
                    {
                        blockTypes[index] = (int)BlockType.Grass; // Tall grass
                    }
                    else if (rand < 0.6)
                    {
                        blockTypes[index] = (int)BlockType.Flower; // Flower
                    }
                    else
                    {
                        blockTypes[index] = (int)BlockType.Sapling; // Sapling
                    }
                }
            }
        }
        
        /// <summary>
        /// Add reeds at a position
        /// </summary>
        private void AddReedsAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            var height = _random.Next(2, 4);
            
            // Place the reeds
            for (int i = 0; i < height; i++)
            {
                var index = (y + i) * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    // Check if this block is air or water
                    if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                    {
                        blockTypes[index] = (int)BlockType.Reeds;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a tree at a position
        /// </summary>
        private void AddTreeAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            var treeHeight = _random.Next(4, 7);
            
            // Place the tree
            for (int i = 0; i < treeHeight; i++)
            {
                var index = (y + i) * 16 * 16 + localZ * 16 + localX;
                
                if (index >= 0 && index < blockTypes.Length)
                {
                    if (i < treeHeight - 2)
                    {
                        // Trunk
                        blockTypes[index] = (int)BlockType.Wood;
                    }
                    else
                    {
                        // Leaves
                        blockTypes[index] = (int)BlockType.Leaves;
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a rock at a position
        /// </summary>
        private void AddRockAtPosition(double x, double z, int y, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Check if the position is within this chunk
            if (!IsPositionInChunk(x, z, chunkX, chunkZ))
            {
                return;
            }
            
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Place the rock
            var index = y * 16 * 16 + localZ * 16 + localX;
            
            if (index >= 0 && index < blockTypes.Length)
            {
                // Check if this block is air
                if (blockTypes[index] == 0)
                {
                    blockTypes[index] = (int)BlockType.Stone;
                }
            }
        }
        
        #region Utility Methods
        
        /// <summary>
        /// Check if a chunk should contain a lake
        /// </summary>
        private bool ShouldContainLake(int chunkX, int chunkZ, int[] heightMap)
        {
            // Use a noise function to determine if this chunk should contain a lake
            var noise = SimpleNoise(chunkX * 0.1, chunkZ * 0.1, _settings.Seed);
            var avgHeight = GetAverageHeight(chunkX, chunkZ, heightMap);
            
            // More likely to have lakes at lower elevations
            var heightFactor = avgHeight < 64 ? 1.5 : 1.0;
            return noise * heightFactor > _settings.LakeGenerationThreshold;
        }
        
        /// <summary>
        /// Get the surface height for a lake
        /// </summary>
        private int GetLakeSurfaceHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            // Find a low point in the chunk for the lake surface
            var minHeight = 255;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        minHeight = Math.Min(minHeight, heightMap[index]);
                    }
                }
            }
            
            // Return a height slightly above the minimum
            return minHeight + _random.Next(1, 4);
        }
        
        /// <summary>
        /// Get the water level for a lake
        /// </summary>
        private int GetLakeWaterLevel(int chunkX, int chunkZ, int[] heightMap)
        {
            // The water level should be slightly below the surface
            var surfaceHeight = GetLakeSurfaceHeight(chunkX, chunkZ, heightMap);
            return surfaceHeight - 1;
        }
        
        /// <summary>
        /// Get the average height of a chunk
        /// </summary>
        private int GetAverageHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            var sum = 0;
            var count = 0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        sum += heightMap[index];
                        count++;
                    }
                }
            }
            
            return count > 0 ? sum / count : 64;
        }
        
        /// <summary>
        /// Check if a position is within a chunk
        /// </summary>
        private bool IsPositionInChunk(double x, double z, int chunkX, int chunkZ)
        {
            var localX = x - chunkX * 16;
            var localZ = z - chunkZ * 16;
            return localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16;
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
        
        #endregion
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Lake system information
    /// </summary>
    public class LakeSystem
    {
        public int Id { get; set; }
        public double CenterX { get; set; }
        public int CenterY { get; set; }
        public double CenterZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusZ { get; set; }
        public int Depth { get; set; }
        public LakeType Type { get; set; }
        public int WaterLevel { get; set; }
    }
    
    /// <summary>
    /// Lake feature information
    /// </summary>
    public class LakeFeature
    {
        public int Id { get; set; }
        public int LakeSystemId { get; set; }
        public LakeFeatureType Type { get; set; }
        public double PositionX { get; set; }
        public int PositionY { get; set; }
        public double PositionZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusZ { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double Direction { get; set; }
    }
    
    /// <summary>
    /// Lake types
    /// </summary>
    public enum LakeType
    {
        Plain,
        Swamp,
        Mountain,
        River
    }
    
    /// <summary>
    /// Lake feature types
    /// </summary>
    public enum LakeFeatureType
    {
        Island,
        LilyPad,
        Reeds,
        RiverConnection
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
        }
        
        /// <summary>
        /// Carve islands in the lake
        /// </summary>
        private void CarveIsland(ChunkData chunk, LakeIsland island)
        {
            var waterLevel = _worldManager.GetTerrainHeight(island.X, island.Z);
            
            for (int dx = -island.Radius; dx <= island.Radius; dx++)
            {
                for (int dz = -island.Radius; dz <= island.Radius; dz++)
                {
                    var worldX = island.X + dx;
                    var worldZ = island.Z + dz;
                    
                    if (worldX < 0 || worldX >= 16 || worldZ < 0 || worldZ >= 16)
                        continue;
                        
                    var distSq = dx * dx + dz * dz;
                    
                    if (IsPointInIsland(distSq, island))
                    {
                        var height = CalculateIslandHeight(distSq, island);
                        
                        for (int y = waterLevel; y <= waterLevel + height; y++)
                        {
                            if (y >= 0 && y < 256)
                            {
                                var blockType = y == waterLevel + height ? BlockType.Grass : BlockType.Dirt;
                                chunk.SetBlock(worldX, y, worldZ, blockType);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if a point is within an island
        /// </summary>
        private bool IsPointInIsland(double distSq, LakeIsland island)
        {
            return island.ShapeType switch
            {
                0 => distSq <= island.Radius * island.Radius, // Circular
                1 => distSq <= island.Radius * island.Radius * 0.8, // Elliptical (simplified)
                2 => distSq <= island.Radius * island.Radius * (0.7 + _random.NextDouble() * 0.6), // Irregular
                _ => distSq <= island.Radius * island.Radius
            };
        }
        
        /// <summary>
        /// Calculate island height at a specific point
        /// </summary>
        private int CalculateIslandHeight(double distSq, LakeIsland island)
        {
            var distance = Math.Sqrt(distSq);
            var heightFactor = 1.0 - (distance / island.Radius);
            var baseHeight = (int)(island.Radius * 0.3 * heightFactor);
            
            // Add some variation
            var noise = SimplexNoise.Generate(distance * 0.2, 0, 1, 0.3, 1.0, 512773);
            var variation = (int)(noise * 2);
            
            return Math.Max(1, baseHeight + variation);
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
    /// Represents a complete lake system with islands
    /// </summary>
    public class LakeSystem
    {
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int Radius { get; set; }
        public double Depth { get; set; }
        public double Complexity { get; set; }
        public bool HasIslands { get; set; }
        public List<LakeIsland> Islands { get; set; } = new();
        public List<LakeShorePoint> ShorePoints { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point along the lake shore
    /// </summary>
    public class LakeShorePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Radius { get; set; }
        public double Angle { get; set; }
    }
    
    /// <summary>
    /// Represents an island within a lake
    /// </summary>
    public class LakeIsland
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Radius { get; set; }
        public int ShapeType { get; set; }
    }
}
}
        public double Depth { get; set; }
        public LakeShape ShapeType { get; set; }
        public List<LakeShapePoint> ShapePoints { get; set; } = new();
        public List<LakeIsland> Islands { get; set; } = new();
        public List<LakeConnection> Connections { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point defining the lake perimeter
    /// </summary>
    public class LakeShapePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Radius { get; set; }
        public double Angle { get; set; }
    }
    
    /// <summary>
    /// Represents an island within a lake
    /// </summary>
    public class LakeIsland
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Radius { get; set; }
        public LakeShape ShapeType { get; set; }
    }
    
    /// <summary>
    /// Represents a connection from lake to other water bodies
    /// </summary>
    public class LakeConnection
    {
        public double Angle { get; set; }
        public int Length { get; set; }
        public double Width { get; set; }
    }
    
    /// <summary>
    /// Lake shape types
    /// </summary>
    public enum LakeShape
    {
        Circular,
        Elliptical,
        Irregular,
        Dendritic
    }
}using System.Collections.Generic;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced lake generation system with realistic lake shapes,
    /// proper depth variation, and natural integration with terrain.
    /// </summary>
    public class ImprovedLakeGenerator
    {
        private readonly WorldManager _worldManager;
        private readonly Random _random;
        private readonly LakeGenerationSettings _settings;
        
        // Lake generation parameters
        private const int LakeMinRadius = 15;
        private const int LakeMaxRadius = 80;
        private const double LakeMinDepth = 3.0;
        private const double LakeMaxDepth = 20.0;
        private const double LakeShapeVariation = 0.4;
        private const int LakeIslandChance = 25; // 25% chance of islands
        private const int LakeMaxIslands = 3;
        private const double LakeShoreSteepness = 1.5;
        private const double LakeDepthVariation = 0.3;
        private const double LakeConnectionChance = 0.15; // 15% chance of connecting to other water bodies
        
        public ImprovedLakeGenerator(WorldManager worldManager)
        {
            _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
            _random = worldManager.GetChunkRandom(0, 0, 0);
            _settings = worldManager._lakeSettings;
        }
        
        /// <summary>
        /// Generate enhanced lake system for a chunk
        /// </summary>
        public void GenerateLakes(ChunkData chunk, int chunkX, int chunkZ)
        {
            if (!_worldManager._enableLakes || !_worldManager._useImprovedLakes)
                return;
                
            var lakeSystems = GenerateLakeSystems(chunkX, chunkZ);
            
            foreach (var lakeSystem in lakeSystems)
            {
                GenerateLakeSystem(chunk, lakeSystem);
            }
        }
        
        /// <summary>
        /// Generate multiple lake systems with realistic characteristics
        /// </summary>
        private List<LakeSystem> GenerateLakeSystems(int chunkX, int chunkZ)
        {
            var systems = new List<LakeSystem>();
            var worldSeed = _worldManager.GetWorldSeed();
            
            // Check if this chunk should contain a lake
            if (ShouldContainLake(chunkX, chunkZ))
            {
                var mainLake = GenerateMainLake(chunkX, chunkZ, worldSeed);
                if (mainLake != null)
                {
                    systems.Add(mainLake);
                    
                    // Generate islands
                    GenerateIslands(mainLake, systems, chunkX, chunkZ, worldSeed);
                }
            }
            
            // Check for lakes that span multiple chunks
            var spanningLakes = GetSpanningLakes(chunkX, chunkZ, worldSeed);
            systems.AddRange(spanningLakes);
            
            return systems;
        }
        
        /// <summary>
        /// Determine if chunk should contain a lake
        /// </summary>
        private bool ShouldContainLake(int chunkX, int chunkZ)
        {
            var lakeRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 234);
            var terrainFactor = GetLakeTerrainFactor(chunkX, chunkZ);
            
            return lakeRandom.NextDouble() < 0.08 * terrainFactor; // 8% base chance modified by terrain
        }
        
        /// <summary>
        /// Get terrain factor that influences lake generation
        /// </summary>
        private double GetLakeTerrainFactor(int chunkX, int chunkZ)
        {
            // Sample terrain characteristics
            var sampleX = chunkX * 16 + 8;
            var sampleZ = chunkZ * 16 + 8;
            
            var elevation = _worldManager.GetTerrainHeight(sampleX, sampleZ);
            var moisture = SimplexNoise.Generate(sampleX * 0.004f + 300, sampleZ * 0.004f + 300, 0, 3, 1.0, 834521);
            var flatness = CalculateTerrainFlatness(sampleX, sampleZ);
            
            // Lakes prefer low elevation, high moisture, and flat terrain
            var elevationFactor = Math.Max(0.2, 1.0 - elevation / 100.0);
            var moistureFactor = Math.Min(2.0, moisture + 0.8);
            var flatnessFactor = Math.Min(1.5, flatness * 2.0);
            
            return elevationFactor * moistureFactor * flatnessFactor;
        }
        
        /// <summary>
        /// Calculate terrain flatness at a position
        /// </summary>
        private double CalculateTerrainFlatness(int x, int z)
        {
            var centerHeight = _worldManager.GetTerrainHeight(x, z);
            var totalDeviation = 0.0;
            var sampleCount = 0;
            
            // Sample in a 5x5 area
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    
                    var sampleHeight = _worldManager.GetTerrainHeight(x + dx, z + dz);
                    totalDeviation += Math.Abs(sampleHeight - centerHeight);
                    sampleCount++;
                }
            }
            
            return sampleCount > 0 ? 1.0 - (totalDeviation / sampleCount / 10.0) : 0.0;
        }
        
        /// <summary>
        /// Generate a main lake with realistic shape
        /// </summary>
        private LakeSystem? GenerateMainLake(int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            var lakeRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 567);
            
            // Determine lake parameters
            var radius = lakeRandom.Next(LakeMinRadius, LakeMaxRadius);
            var depth = lakeRandom.NextDouble() * (LakeMaxDepth - LakeMinDepth) + LakeMinDepth;
            var shapeType = (LakeShape)lakeRandom.Next(0, Enum.GetValues<LakeShape>().Length);
            
            // Find center point
            var centerX = chunkX * 16 + 8;
            var centerZ = chunkZ * 16 + 8;
            var centerY = _worldManager.GetTerrainHeight(centerX, centerZ);
            
            var lake = new LakeSystem
            {
                CenterX = centerX,
                CenterY = centerY,
                CenterZ = centerZ,
                Radius = radius,
                Depth = depth,
                ShapeType = shapeType,
                Islands = new List<LakeIsland>(),
                Connections = new List<LakeConnection>()
            };
            
            // Generate lake shape
            GenerateLakeShape(lake, lakeRandom);
            
            // Check for connections to other water bodies
            if (lakeRandom.NextDouble() < LakeConnectionChance)
            {
                GenerateLakeConnections(lake, lakeRandom);
            }
            
            return lake;
        }
        
        /// <summary>
        /// Generate realistic lake shape
        /// </summary>
        private void GenerateLakeShape(LakeSystem lake, Random random)
        {
            var pointCount = 36; // Points around the lake perimeter
            lake.ShapePoints = new List<LakeShapePoint>();
            
            for (int i = 0; i < pointCount; i++)
            {
                var angle = (i / (double)pointCount) * Math.PI * 2.0;
                
                // Calculate radius variation based on shape type
                var radiusVariation = CalculateRadiusVariation(angle, lake.ShapeType, random);
                var pointRadius = lake.Radius * radiusVariation;
                
                var x = (int)(lake.CenterX + Math.Cos(angle) * pointRadius);
                var z = (int)(lake.CenterZ + Math.Sin(angle) * pointRadius);
                var y = _worldManager.GetTerrainHeight(x, z);
                
                lake.ShapePoints.Add(new LakeShapePoint
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Radius = pointRadius,
                    Angle = angle
                });
            }
        }
        
        /// <summary>
        /// Calculate radius variation based on lake shape type
        /// </summary>
        private double CalculateRadiusVariation(double angle, LakeShape shapeType, Random random)
        {
            var baseVariation = 1.0 + (random.NextDouble() - 0.5) * LakeShapeVariation;
            
            switch (shapeType)
            {
                case LakeShape.Circular:
                    return baseVariation * (0.9 + Math.Sin(angle * 6) * 0.1);
                    
                case LakeShape.Elliptical:
                    return baseVariation * (0.8 + Math.Cos(angle * 2) * 0.2);
                    
                case LakeShape.Irregular:
                    return baseVariation * (0.7 + 
                        Math.Sin(angle * 3) * 0.15 + 
                        Math.Sin(angle * 7) * 0.1 + 
                        Math.Sin(angle * 11) * 0.05);
                    
                case LakeShape.Dendritic:
                    return baseVariation * (0.8 + 
                        Math.Sin(angle * 4) * 0.2 + 
                        (random.NextDouble() < 0.3 ? Math.Sin(angle * 12) * 0.3 : 0));
                    
                default:
                    return baseVariation;
            }
        }
        
        /// <summary>
        /// Generate connections to other water bodies
        /// </summary>
        private void GenerateLakeConnections(LakeSystem lake, Random random)
        {
            var connectionCount = random.Next(1, 3);
            
            for (int i = 0; i < connectionCount; i++)
            {
                var angle = random.NextDouble() * Math.PI * 2.0;
                var connectionLength = random.Next(20, 60);
                
                var connection = new LakeConnection
                {
                    Angle = angle,
                    Length = connectionLength,
                    Width = lake.Radius * 0.1
                };
                
                lake.Connections.Add(connection);
            }
        }
        
        /// <summary>
        /// Generate islands within the lake
        /// </summary>
        private void GenerateIslands(LakeSystem mainLake, List<LakeSystem> systems, int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            var islandRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 890);
            var islandCount = islandRandom.Next(0, LakeMaxIslands + 1);
            
            for (int i = 0; i < islandCount; i++)
            {
                if (islandRandom.NextDouble() < LakeIslandChance / 100.0)
                {
                    var island = GenerateIsland(mainLake, islandRandom);
                    if (island != null)
                    {
                        mainLake.Islands.Add(island);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a single island within a lake
        /// </summary>
        private LakeIsland? GenerateIsland(LakeSystem lake, Random random)
        {
            if (lake.Radius < 30) // Too small for islands
                return null;
                
            // Determine island parameters
            var islandRadius = random.Next(lake.Radius / 8, lake.Radius / 3);
            var islandAngle = random.NextDouble() * Math.PI * 2.0;
            var distanceFromCenter = random.Next(lake.Radius / 4, lake.Radius * 3 / 4);
            
            var islandX = (int)(lake.CenterX + Math.Cos(islandAngle) * distanceFromCenter);
            var islandZ = (int)(lake.CenterZ + Math.Sin(islandAngle) * distanceFromCenter);
            var islandY = _worldManager.GetTerrainHeight(islandX, islandZ);
            
            return new LakeIsland
            {
                X = islandX,
                Y = islandY,
                Z = islandZ,
                Radius = islandRadius,
                ShapeType = (LakeShape)random.Next(0, 3) // Exclude dendritic for islands
            };
        }
        
        /// <summary>
        /// Get lakes that span multiple chunks
        /// </summary>
        private List<LakeSystem> GetSpanningLakes(int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            // This would be implemented by checking neighboring chunks for lakes
            // For now, return empty list
            return new List<LakeSystem>();
        }
        
        /// <summary>
        /// Apply lake system to chunk data
        /// </summary>
        private void GenerateLakeSystem(ChunkData chunk, LakeSystem lake)
        {
            CarveLake(chunk, lake);
            
            foreach (var island in lake.Islands)
            {
                CarveIsland(chunk, island, lake);
            }
            
            foreach (var connection in lake.Connections)
            {
                CarveConnection(chunk, lake, connection);
            }
        }
        
        /// <summary>
        /// Carve lake into terrain
        /// </summary>
        private void CarveLake(ChunkData chunk, LakeSystem lake)
        {
            // Find lake bounds within this chunk
            var bounds = GetLakeBoundsInChunk(lake);
            if (bounds == null)
                return;
                
            var (minX, maxX, minZ, maxZ) = bounds.Value;
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var worldX = x;
                    var worldZ = z;
                    
                    // Check if point is within lake shape
                    if (IsPointInLake(worldX, worldZ, lake))
                    {
                        var worldY = _worldManager.GetTerrainHeight(worldX, worldZ);
                        var depth = CalculateLakeDepth(worldX, worldZ, lake);
                        
                        // Carve lake bed
                        for (int y = worldY; y >= worldY - depth; y--)
                        {
                            if (y >= 0 && y < 256)
                            {
                                chunk.SetBlock(worldX, y, worldZ, BlockType.Water);
                            }
                        }
                        
                        // Create lake shores
                        CreateLakeShores(chunk, worldX, worldY, worldZ, lake);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get lake bounds within this chunk
        /// </summary>
        private (int minX, int maxX, int minZ, int maxZ)? GetLakeBoundsInChunk(LakeSystem lake)
        {
            var chunkMinX = 0;
            var chunkMaxX = 15;
            var chunkMinZ = 0;
            var chunkMaxZ = 15;
            
            // Convert to world coordinates
            var worldMinX = lake.CenterX - lake.Radius;
            var worldMaxX = lake.CenterX + lake.Radius;
            var worldMinZ = lake.CenterZ - lake.Radius;
            var worldMaxZ = lake.CenterZ + lake.Radius;
            
            // Check intersection with chunk
            if (worldMaxX < chunkMinX || worldMinX > chunkMaxX ||
                worldMaxZ < chunkMinZ || worldMinZ > chunkMaxZ)
                return null;
                
            return (
                Math.Max(chunkMinX, worldMinX),
                Math.Min(chunkMaxX, worldMaxX),
                Math.Max(chunkMinZ, worldMinZ),
                Math.Min(chunkMaxZ, worldMaxZ)
            );
        }
        
        /// <summary>
        /// Check if a point is within the lake shape
        /// </summary>
        private bool IsPointInLake(int x, int z, LakeSystem lake)
        {
            var dx = x - lake.CenterX;
            var dz = z - lake.CenterZ;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            var angle = Math.Atan2(dz, dx);
            
            // Normalize angle to 0-2π
            if (angle < 0) angle += Math.PI * 2.0;
            
            // Find closest shape points
            var pointIndex = (int)((angle / (Math.PI * 2.0)) * lake.ShapePoints.Count);
            var nextIndex = (pointIndex + 1) % lake.ShapePoints.Count;
            
            var point1 = lake.ShapePoints[pointIndex];
            var point2 = lake.ShapePoints[nextIndex];
            
            // Interpolate radius at this angle
            var t = (angle - point1.Angle) / (point2.Angle - point1.Angle);
            var interpolatedRadius = point1.Radius + (point2.Radius - point1.Radius) * t;
            
            return distance <= interpolatedRadius;
        }
        
        /// <summary>
        /// Calculate lake depth at a specific point
        /// </summary>
        private double CalculateLakeDepth(int x, int z, LakeSystem lake)
        {
            var dx = x - lake.CenterX;
            var dz = z - lake.CenterZ;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            
            // Depth varies based on distance from center
            var depthFactor = 1.0 - (distance / lake.Radius) * 0.7; // Deeper in center
            
            // Add some noise for natural variation
            var noise = SimplexNoise.Generate(x * 0.1f, z * 0.1f, 2, 0.5, 1.0, 456789);
            var depthVariation = 1.0 + noise * LakeDepthVariation;
            
            return lake.Depth * depthFactor * depthVariation;
        }
        
        /// <summary>
        /// Create natural lake shores
        /// </summary>
        private void CreateLakeShores(ChunkData chunk, int x, int y, int z, LakeSystem lake)
        {
            var dx = x - lake.CenterX;
            var dz = z - lake.CenterZ;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            
            // Check if near shore
            if (distance > lake.Radius * 0.8 && distance <= lake.Radius)
            {
                var shoreFactor = (distance - lake.Radius * 0.8) / (lake.Radius * 0.2);
                var shoreHeight = (int)(shoreFactor * LakeShoreSteepness);
                
                for (int sy = 1; sy <= shoreHeight; sy++)
                {
                    var blockY = y + sy;
                    if (blockY >= 0 && blockY < 256)
                    {
                        // Use sand or gravel for shores
                        var blockType = _random.NextDouble() < 0.6 ? BlockType.Sand : BlockType.Gravel;
                        chunk.SetBlock(x, blockY, z, blockType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve island within lake
        /// </summary>
        private void CarveIsland(ChunkData chunk, LakeIsland island, LakeSystem lake)
        {
            var radius = island.Radius;
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var distSq = dx * dx + dz * dz;
                    if (distSq <= radius * radius)
                    {
                        var worldX = island.X + dx;
                        var worldZ = island.Z + dz;
                        
                        if (worldX >= 0 && worldX < 16 && worldZ >= 0 && worldZ < 16)
                        {
                            var worldY = island.Y;
                            
                            // Build island terrain
                            for (int dy = 0; dy <= 3; dy++)
                            {
                                var blockY = worldY + dy;
                                if (blockY >= 0 && blockY < 256)
                                {
                                    var blockType = dy == 0 ? BlockType.Dirt : BlockType.Grass;
                                    chunk.SetBlock(worldX, blockY, worldZ, blockType);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve lake connection to other water bodies
        /// </summary>
        private void CarveConnection(ChunkData chunk, LakeSystem lake, LakeConnection connection)
        {
            var startX = lake.CenterX;
            var startZ = lake.CenterZ;
            var endX = (int)(startX + Math.Cos(connection.Angle) * connection.Length);
            var endZ = (int)(startZ + Math.Sin(connection.Angle) * connection.Length);
            
            var steps = connection.Length / 2;
            for (int step = 0; step <= steps; step++)
            {
                var t = step / (double)steps;
                var x = startX + (int)((endX - startX) * t);
                var z = startZ + (int)((endZ - startZ) * t);
                var y = _worldManager.GetTerrainHeight(x, z);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16)
                {
                    var width = (int)(connection.Width);
                    
                    for (int dx = -width; dx <= width; dx++)
                    {
                        for (int dz = -width; dz <= width; dz++)
                        {
                            var distSq = dx * dx + dz * dz;
                            if (distSq <= width * width)
                            {
                                var localX = x + dx;
                                var localZ = z + dz;
                                
                                if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
                                {
                                    chunk.SetBlock(localX, y, localZ, BlockType.Water);
                                }
                            }
                        }
                    }
                }
            }
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
    /// Represents a complete lake system with islands and connections
    /// </summary>
    public class LakeSystem
    {
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int Radius { get; set; }
        public double Depth { get; set; }
        public LakeShape ShapeType { get; set; }
        public List<LakeShapePoint> ShapePoints { get; set; } = new();
        public List<LakeIsland> Islands { get; set; } = new();
        public List<LakeConnection> Connections { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point defining the lake perimeter
    /// </summary>
    public class LakeShapePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Radius { get; set; }
        public double Angle { get; set; }
    }
    
    /// <summary>
    /// Represents an island within a lake
    /// </summary>
    public class LakeIsland
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Radius { get; set; }
        public LakeShape ShapeType { get; set; }
    }
    
    /// <summary>
    /// Represents a connection from lake to other water bodies
    /// </summary>
    public class LakeConnection
    {
        public double Angle { get; set; }
        public int Length { get; set; }
        public double Width { get; set; }
    }
    
    /// <summary>
    /// Lake shape types
    /// </summary>
    public enum LakeShape
    {
        Circular,
        Elliptical,
        Irregular,
        Dendritic
    }
}
