using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced river generation system with realistic hydrology,
    /// natural meandering patterns, and proper elevation-based flow.
    /// </summary>
    public class ImprovedRiverGenerator
    {
        private readonly RiverGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<RiverSystem>> _riverSystems;
        private readonly Dictionary<int, List<RiverSegment>> _riverSegments;
        private readonly Dictionary<int, List<RiverFeature>> _riverFeatures;
        
        public ImprovedRiverGenerator(RiverGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _riverSystems = new Dictionary<int, List<RiverSystem>>();
            _riverSegments = new Dictionary<int, List<RiverSegment>>();
            _riverFeatures = new Dictionary<int, List<RiverFeature>>();
        }
        
        /// <summary>
        /// Generate rivers for a chunk
        /// </summary>
        public void GenerateRivers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _riverSystems[chunkKey] = new List<RiverSystem>();
            _riverSegments[chunkKey] = new List<RiverSegment>();
            _riverFeatures[chunkKey] = new List<RiverFeature>();
            
            // Generate river systems for this chunk
            GenerateRiverSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate river segments
            GenerateRiverSegments(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate river features
            GenerateRiverFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect river systems
            ConnectRiverSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add waterfalls
            AddWaterfalls(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add river banks
            AddRiverBanks(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate river systems
        /// </summary>
        private void GenerateRiverSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Check if this chunk should contain a river source
            if (ShouldContainRiverSource(chunkX, chunkZ))
            {
                var riverSystem = new RiverSystem
                {
                    Id = _random.Next(),
                    SourceX = chunkX * 16 + _random.Next(16),
                    SourceY = GetRiverSourceHeight(chunkX, chunkZ, heightMap),
                    SourceZ = chunkZ * 16 + _random.Next(16),
                    Width = _random.Next(_settings.MinRiverWidth, _settings.MaxRiverWidth + 1),
                    FlowRate = _random.Next(_settings.MinFlowRate, _settings.MaxFlowRate + 1),
                    Type = (RiverType)_random.Next(Enum.GetValues(typeof(RiverType)).Length),
                    MeanderFactor = _random.NextDouble() * _settings.MeanderIntensity,
                    TributaryProbability = _settings.TributaryProbability
                };
                
                _riverSystems[chunkKey].Add(riverSystem);
                
                // Generate the river system
                GenerateRiverSystem(riverSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single river system
        /// </summary>
        private void GenerateRiverSystem(RiverSystem riverSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var currentX = riverSystem.SourceX;
            var currentY = riverSystem.SourceY;
            var currentZ = riverSystem.SourceZ;
            var direction = _random.NextDouble() * Math.PI * 2;
            var length = 0;
            
            // Generate river segments until we reach a lake, ocean, or max length
            while (length < _settings.MaxRiverLength && IsInWorldBounds(currentX, currentZ))
            {
                // Calculate meander
                direction += (_random.NextDouble() - 0.5) * riverSystem.MeanderFactor;
                
                // Calculate next position based on direction and elevation
                var nextX = currentX + Math.Cos(direction) * _settings.RiverSegmentLength;
                var nextZ = currentZ + Math.Sin(direction) * _settings.RiverSegmentLength;
                
                // Find the height at the next position
                var nextY = GetHeightAtPosition(nextX, nextZ, chunkX, chunkZ, heightMap);
                
                // Adjust direction based on elevation (water flows downhill)
                direction = AdjustDirectionForElevation(direction, currentY, nextY);
                
                // Create river segment
                var segment = new RiverSegment
                {
                    Id = _random.Next(),
                    RiverSystemId = riverSystem.Id,
                    StartX = currentX,
                    StartY = currentY,
                    StartZ = currentZ,
                    EndX = nextX,
                    EndY = nextY,
                    EndZ = nextZ,
                    Width = riverSystem.Width,
                    Depth = CalculateRiverDepth(riverSystem, length),
                    FlowRate = riverSystem.FlowRate,
                    Type = length == 0 ? RiverSegmentType.Source : RiverSegmentType.Main
                };
                
                _riverSegments[chunkKey].Add(segment);
                
                // Carve the river segment
                CarveRiverSegment(segment, chunkX, chunkZ, heightMap, blockTypes);
                
                // Check for tributaries
                if (_random.NextDouble() < riverSystem.TributaryProbability)
                {
                    GenerateTributary(segment, riverSystem, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Update position
                currentX = nextX;
                currentY = nextY;
                currentZ = nextZ;
                length++;
                
                // Check if we've reached a lake or ocean
                if (HasReachedWaterBody(currentX, currentY, currentZ, chunkX, chunkZ, heightMap, blockTypes))
                {
                    // Create a river mouth
                    var mouthSegment = new RiverSegment
                    {
                        Id = _random.Next(),
                        RiverSystemId = riverSystem.Id,
                        StartX = currentX,
                        StartY = currentY,
                        StartZ = currentZ,
                        EndX = currentX,
                        EndY = currentY,
                        EndZ = currentZ,
                        Width = riverSystem.Width * 2, // Wider at the mouth
                        Depth = CalculateRiverDepth(riverSystem, length) * 1.5f,
                        FlowRate = riverSystem.FlowRate,
                        Type = RiverSegmentType.Mouth
                    };
                    
                    _riverSegments[chunkKey].Add(mouthSegment);
                    CarveRiverSegment(mouthSegment, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Generate river segments
        /// </summary>
        private void GenerateRiverSegments(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // River segments are generated in GenerateRiverSystem
        }
        
        /// <summary>
        /// Generate river features
        /// </summary>
        private void GenerateRiverFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate features for each river segment in this chunk
            foreach (var segment in _riverSegments[chunkKey])
            {
                // Generate waterfalls for steep segments
                if (IsSteepSegment(segment))
                {
                    var waterfall = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Waterfall,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = segment.StartY,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Height = (int)(segment.StartY - segment.EndY),
                        Width = segment.Width
                    };
                    
                    _riverFeatures[chunkKey].Add(waterfall);
                    AddWaterfallFeature(waterfall, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Generate rapids for moderate slopes
                if (IsModerateSlopeSegment(segment))
                {
                    var rapids = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Rapids,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = (segment.StartY + segment.EndY) / 2,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Width = segment.Width,
                        Intensity = _random.NextDouble()
                    };
                    
                    _riverFeatures[chunkKey].Add(rapids);
                    AddRapidsFeature(rapids, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Generate river bends
                if (IsBendSegment(segment))
                {
                    var bend = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Bend,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = (segment.StartY + segment.EndY) / 2,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Width = segment.Width,
                        Curvature = CalculateBendCurvature(segment)
                    };
                    
                    _riverFeatures[chunkKey].Add(bend);
                    AddBendFeature(bend, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Generate a tributary
        /// </summary>
        private void GenerateTributary(RiverSegment parentSegment, RiverSystem parentSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Create a new river system for the tributary
            var tributarySystem = new RiverSystem
            {
                Id = _random.Next(),
                SourceX = parentSegment.StartX + _random.Next(-10, 11),
                SourceY = parentSegment.StartY + _random.Next(5, 11), // Start higher
                SourceZ = parentSegment.StartZ + _random.Next(-10, 11),
                Width = Math.Max(1, parentSystem.Width / 2), // Narrower than parent
                FlowRate = Math.Max(1, parentSystem.FlowRate / 2), // Less flow
                Type = parentSystem.Type,
                MeanderFactor = parentSystem.MeanderFactor,
                TributaryProbability = parentSystem.TributaryProbability * 0.5 // Less likely to have its own tributaries
            };
            
            _riverSystems[chunkKey].Add(tributarySystem);
            
            // Generate a short segment to connect to the parent
            var connectionSegment = new RiverSegment
            {
                Id = _random.Next(),
                RiverSystemId = tributarySystem.Id,
                StartX = tributarySystem.SourceX,
                StartY = tributarySystem.SourceY,
                StartZ = tributarySystem.SourceZ,
                EndX = parentSegment.StartX,
                EndY = parentSegment.StartY,
                EndZ = parentSegment.StartZ,
                Width = tributarySystem.Width,
                Depth = CalculateRiverDepth(tributarySystem, 0),
                FlowRate = tributarySystem.FlowRate,
                Type = RiverSegmentType.Tributary
            };
            
            _riverSegments[chunkKey].Add(connectionSegment);
            CarveRiverSegment(connectionSegment, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Connect river systems
        /// </summary>
        private void ConnectRiverSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Connect nearby river systems
            for (int i = 0; i < _riverSystems[chunkKey].Count; i++)
            {
                for (int j = i + 1; j < _riverSystems[chunkKey].Count; j++)
                {
                    var system1 = _riverSystems[chunkKey][i];
                    var system2 = _riverSystems[chunkKey][j];
                    
                    var distance = Math.Sqrt(
                        Math.Pow(system1.SourceX - system2.SourceX, 2) +
                        Math.Pow(system1.SourceY - system2.SourceY, 2) +
                        Math.Pow(system1.SourceZ - system2.SourceZ, 2)
                    );
                    
                    // Connect systems if they're close enough
                    if (distance < _settings.RiverConnectionDistance && _random.NextDouble() < _settings.RiverConnectionProbability)
                    {
                        var connection = new RiverSegment
                        {
                            Id = _random.Next(),
                            RiverSystemId = system1.Id,
                            StartX = system1.SourceX,
                            StartY = system1.SourceY,
                            StartZ = system1.SourceZ,
                            EndX = system2.SourceX,
                            EndY = system2.SourceY,
                            EndZ = system2.SourceZ,
                            Width = Math.Min(system1.Width, system2.Width),
                            Depth = Math.Min(CalculateRiverDepth(system1, 0), CalculateRiverDepth(system2, 0)),
                            FlowRate = system1.FlowRate + system2.FlowRate,
                            Type = RiverSegmentType.Connection
                        };
                        
                        _riverSegments[chunkKey].Add(connection);
                        CarveRiverSegment(connection, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add waterfalls
        /// </summary>
        private void AddWaterfalls(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Waterfalls are added in GenerateRiverFeatures
        }
        
        /// <summary>
        /// Add river banks
        /// </summary>
        private void AddRiverBanks(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add banks to each river segment
            foreach (var segment in _riverSegments[chunkKey])
            {
                AddRiverBanksToSegment(segment, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a river segment
        /// </summary>
        private void CarveRiverSegment(RiverSegment segment, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndY - segment.StartY, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = segment.StartX + t * (segment.EndX - segment.StartX);
                var y = segment.StartY + t * (segment.EndY - segment.StartY);
                var z = segment.StartZ + t * (segment.EndZ - segment.StartZ);
                
                // Carve a channel at this position
                CarveRiverChannel(x, y, z, segment.Width, segment.Depth, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a river channel
        /// </summary>
        private void CarveRiverChannel(double centerX, double centerY, double centerZ, int width, int depth, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var radius = width / 2.0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the river width
                    var dx = worldX - centerX;
                    var dz = worldZ - centerZ;
                    var distance = Math.Sqrt(dx * dx + dz * dz);
                    
                    if (distance <= radius)
                    {
                        // Calculate the depth at this position (deeper in the center)
                        var depthFactor = 1.0 - (distance / radius);
                        var localDepth = depth * depthFactor;
                        
                        // Carve the river bed
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is within the river depth
                                if (y >= centerY - localDepth && y <= centerY)
                                {
                                    // Replace with water for the top layer
                                    if (y >= centerY - 1)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                    else
                                    {
                                        // Use sand/gravel for the river bed
                                        blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Gravel;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add waterfall feature
        /// </summary>
        private void AddWaterfallFeature(RiverFeature waterfall, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var x = (int)waterfall.PositionX;
            var z = (int)waterfall.PositionZ;
            var startY = (int)waterfall.PositionY;
            var endY = startY - waterfall.Height;
            
            // Create the waterfall
            for (int y = endY; y <= startY; y++)
            {
                for (int wx = -waterfall.Width / 2; wx <= waterfall.Width / 2; wx++)
                {
                    for (int wz = -waterfall.Width / 2; wz <= waterfall.Width / 2; wz++)
                    {
                        var worldX = x + wx;
                        var worldZ = z + wz;
                        
                        // Check if this position is within the chunk
                        if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                            worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                        {
                            var localX = worldX - chunkX * 16;
                            var localZ = worldZ - chunkZ * 16;
                            var index = y * 16 * 16 + localZ * 16 + localX;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Use water for the waterfall
                                blockTypes[index] = (int)BlockType.Water;
                            }
                        }
                    }
                }
            }
            
            // Create a pool at the bottom of the waterfall
            CreateWaterfallPool(x, endY, z, waterfall.Width, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Create a pool at the bottom of a waterfall
        /// </summary>
        private void CreateWaterfallPool(int x, int y, int z, int width, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var poolRadius = width * 2;
            var poolDepth = width / 2;
            
            for (int px = -poolRadius; px <= poolRadius; px++)
            {
                for (int pz = -poolRadius; pz <= poolRadius; pz++)
                {
                    var distance = Math.Sqrt(px * px + pz * pz);
                    
                    if (distance <= poolRadius)
                    {
                        var worldX = x + px;
                        var worldZ = z + pz;
                        
                        // Check if this position is within the chunk
                        if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                            worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                        {
                            var localX = worldX - chunkX * 16;
                            var localZ = worldZ - chunkZ * 16;
                            
                            // Calculate depth based on distance from center
                            var depthFactor = 1.0 - (distance / poolRadius);
                            var localDepth = (int)(poolDepth * depthFactor);
                            
                            for (int py = y - localDepth; py <= y; py++)
                            {
                                var index = py * 16 * 16 + localZ * 16 + localX;
                                
                                if (index >= 0 && index < blockTypes.Length)
                                {
                                    if (py >= y - 1)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                    else
                                    {
                                        blockTypes[index] = (int)BlockType.Sand;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add rapids feature
        /// </summary>
        private void AddRapidsFeature(RiverFeature rapids, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var x = (int)rapids.PositionX;
            var z = (int)rapids.PositionZ;
            var y = (int)rapids.PositionY;
            
            // Create rapids by adding air blocks in the water
            for (int rx = -rapids.Width / 2; rx <= rapids.Width / 2; rx++)
            {
                for (int rz = -rapids.Width / 2; rz <= rapids.Width / 2; rz++)
                {
                    var worldX = x + rx;
                    var worldZ = z + rz;
                    
                    // Check if this position is within the chunk
                    if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                        worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                    {
                        var localX = worldX - chunkX * 16;
                        var localZ = worldZ - chunkZ * 16;
                        
                        // Add some air blocks to simulate rapids
                        if (_random.NextDouble() < rapids.Intensity)
                        {
                            var index = y * 16 * 16 + localZ * 16 + localX;
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == (int)BlockType.Water)
                            {
                                blockTypes[index] = 0; // Air
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add bend feature
        /// </summary>
        private void AddBendFeature(RiverFeature bend, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Bends are naturally created by the meandering algorithm
            // We can add some visual enhancements here if needed
        }
        
        /// <summary>
        /// Add river banks to a segment
        /// </summary>
        private void AddRiverBanksToSegment(RiverSegment segment, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndY - segment.StartY, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = segment.StartX + t * (segment.EndX - segment.StartX);
                var y = segment.StartY + t * (segment.EndY - segment.StartY);
                var z = segment.StartZ + t * (segment.EndZ - segment.StartZ);
                
                // Add banks at this position
                AddRiverBanksAtPosition(x, y, z, segment.Width, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Add river banks at a position
        /// </summary>
        private void AddRiverBanksAtPosition(double centerX, double centerY, double centerZ, int width, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var radius = width / 2.0;
            var bankWidth = 2; // Width of the river banks
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is near the river edge
                    var dx = worldX - centerX;
                    var dz = worldZ - centerZ;
                    var distance = Math.Sqrt(dx * dx + dz * dz);
                    
                    // Add banks at the edge of the river
                    if (distance > radius && distance <= radius + bankWidth)
                    {
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is at the right height
                                if (Math.Abs(y - centerY) <= 1)
                                {
                                    // Use dirt or sand for river banks
                                    if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                                    {
                                        blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Dirt : (int)BlockType.Sand;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        #region Utility Methods
        
        /// <summary>
        /// Check if a chunk should contain a river source
        /// </summary>
        private bool ShouldContainRiverSource(int chunkX, int chunkZ)
        {
            // Use a noise function to determine if this chunk should contain a river source
            var noise = SimpleNoise(chunkX * 0.1, chunkZ * 0.1, _settings.Seed);
            return noise > _settings.RiverSourceThreshold;
        }
        
        /// <summary>
        /// Get the height for a river source
        /// </summary>
        private int GetRiverSourceHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            // Find a high point in the chunk for the river source
            var maxHeight = 0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        maxHeight = Math.Max(maxHeight, heightMap[index]);
                    }
                }
            }
            
            // Return a height slightly below the maximum
            return maxHeight - _random.Next(5, 15);
        }
        
        /// <summary>
        /// Get the height at a position
        /// </summary>
        private int GetHeightAtPosition(double x, double z, int chunkX, int chunkZ, int[] heightMap)
        {
            // Convert world coordinates to chunk coordinates
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Check if the position is within the chunk
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                var index = localX + localZ * 16;
                if (index >= 0 && index < heightMap.Length)
                {
                    return heightMap[index];
                }
            }
            
            // Return a default height if outside the chunk
            return 64;
        }
        
        /// <summary>
        /// Adjust direction based on elevation
        /// </summary>
        private double AdjustDirectionForElevation(double direction, double currentY, double nextY)
        {
            // Water flows downhill, so adjust direction if needed
            if (nextY > currentY)
            {
                // We're going uphill, adjust direction
                return direction + Math.PI / 4; // Turn 45 degrees
            }
            
            return direction;
        }
        
        /// <summary>
        /// Check if position is in world bounds
        /// </summary>
        private bool IsInWorldBounds(double x, double z)
        {
            return x >= 0 && x < 30000000 && z >= 0 && z < 30000000;
        }
        
        /// <summary>
        /// Check if we've reached a water body
        /// </summary>
        private bool HasReachedWaterBody(double x, double y, double z, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Convert world coordinates to chunk coordinates
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Check if the position is within the chunk
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int py = (int)y - 5; py <= (int)y + 5; py++)
                {
                    if (py >= 0 && py < 256)
                    {
                        var index = py * 16 * 16 + localZ * 16 + localX;
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            // Check if we've reached water or a very low area
                            if (blockTypes[index] == (int)BlockType.Water || py < 50)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate river depth
        /// </summary>
        private int CalculateRiverDepth(RiverSystem riverSystem, int length)
        {
            // Rivers get deeper as they get longer
            var baseDepth = riverSystem.Width / 3;
            var lengthFactor = Math.Min(1.0, length / 100.0);
            return (int)(baseDepth * (1 + lengthFactor));
        }
        
        /// <summary>
        /// Check if a segment is steep
        /// </summary>
        private bool IsSteepSegment(RiverSegment segment)
        {
            var heightDiff = segment.StartY - segment.EndY;
            var horizontalDistance = Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            var slope = horizontalDistance > 0 ? heightDiff / horizontalDistance : 0;
            return slope > 0.5; // Steep if slope > 0.5
        }
        
        /// <summary>
        /// Check if a segment has moderate slope
        /// </summary>
        private bool IsModerateSlopeSegment(RiverSegment segment)
        {
            var heightDiff = segment.StartY - segment.EndY;
            var horizontalDistance = Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            var slope = horizontalDistance > 0 ? heightDiff / horizontalDistance : 0;
            return slope > 0.1 && slope <= 0.5; // Moderate if 0.1 < slope <= 0.5
        }
        
        /// <summary>
        /// Check if a segment is a bend
        /// </summary>
        private bool IsBendSegment(RiverSegment segment)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to look at the previous and next segments
            return _random.NextDouble() < 0.2; // 20% chance of being a bend
        }
        
        /// <summary>
        /// Calculate bend curvature
        /// </summary>
        private double CalculateBendCurvature(RiverSegment segment)
        {
            // This is a simplified calculation - in a real implementation,
            // we'd need to look at the previous and next segments
            return _random.NextDouble();
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
    /// River system information
    /// </summary>
    public class RiverSystem
    {
        public int Id { get; set; }
        public double SourceX { get; set; }
        public int SourceY { get; set; }
        public double SourceZ { get; set; }
        public int Width { get; set; }
        public int FlowRate { get; set; }
        public RiverType Type { get; set; }
        public double MeanderFactor { get; set; }
        public double TributaryProbability { get; set; }
    }
    
    /// <summary>
    /// River segment information
    /// </summary>
    public class RiverSegment
    {
        public int Id { get; set; }
        public int RiverSystemId { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double EndZ { get; set; }
        public int Width { get; set; }
        public int Depth { get; set; }
        public int FlowRate { get; set; }
        public RiverSegmentType Type { get; set; }
    }
    
    /// <summary>
    /// River feature information
    /// </summary>
    public class RiverFeature
    {
        public int Id { get; set; }
        public int RiverSegmentId { get; set; }
        public RiverFeatureType Type { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double PositionZ { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Intensity { get; set; }
        public double Curvature { get; set; }
    }
    
    /// <summary>
    /// River types
    /// </summary>
    public enum RiverType
    {
        Mountain,
        Plains,
        Jungle,
        Desert,
        Snowy
    }
    
    /// <summary>
    /// River segment types
    /// </summary>
    public enum RiverSegmentType
    {
        Source,
        Main,
        Tributary,
        Connection,
        Mouth
    }
    
    /// <summary>
    /// River feature types
    /// </summary>
    public enum RiverFeatureType
    {
        Waterfall,
        Rapids,
        Bend,
        Pool,
        Delta
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
        Cobweb = 14
    }
    
    #endregion
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced river generation system with realistic hydrology,
    /// natural meandering patterns, and proper elevation-based flow.
    /// </summary>
    public class ImprovedRiverGenerator
    {
        private readonly RiverGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<RiverSystem>> _riverSystems;
        private readonly Dictionary<int, List<RiverSegment>> _riverSegments;
        private readonly Dictionary<int, List<RiverFeature>> _riverFeatures;
        
        public ImprovedRiverGenerator(RiverGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _riverSystems = new Dictionary<int, List<RiverSystem>>();
            _riverSegments = new Dictionary<int, List<RiverSegment>>();
            _riverFeatures = new Dictionary<int, List<RiverFeature>>();
        }
        
        /// <summary>
        /// Generate rivers for a chunk
        /// </summary>
        public void GenerateRivers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _riverSystems[chunkKey] = new List<RiverSystem>();
            _riverSegments[chunkKey] = new List<RiverSegment>();
            _riverFeatures[chunkKey] = new List<RiverFeature>();
            
            // Generate river systems for this chunk
            GenerateRiverSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate river segments
            GenerateRiverSegments(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate river features
            GenerateRiverFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect river systems
            ConnectRiverSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add waterfalls
            AddWaterfalls(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add river banks
            AddRiverBanks(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate river systems
        /// </summary>
        private void GenerateRiverSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Check if this chunk should contain a river source
            if (ShouldContainRiverSource(chunkX, chunkZ))
            {
                var riverSystem = new RiverSystem
                {
                    Id = _random.Next(),
                    SourceX = chunkX * 16 + _random.Next(16),
                    SourceY = GetRiverSourceHeight(chunkX, chunkZ, heightMap),
                    SourceZ = chunkZ * 16 + _random.Next(16),
                    Width = _random.Next(_settings.MinRiverWidth, _settings.MaxRiverWidth + 1),
                    FlowRate = _random.Next(_settings.MinFlowRate, _settings.MaxFlowRate + 1),
                    Type = (RiverType)_random.Next(Enum.GetValues(typeof(RiverType)).Length),
                    MeanderFactor = _random.NextDouble() * _settings.MeanderIntensity,
                    TributaryProbability = _settings.TributaryProbability
                };
                
                _riverSystems[chunkKey].Add(riverSystem);
                
                // Generate the river system
                GenerateRiverSystem(riverSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single river system
        /// </summary>
        private void GenerateRiverSystem(RiverSystem riverSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var currentX = riverSystem.SourceX;
            var currentY = riverSystem.SourceY;
            var currentZ = riverSystem.SourceZ;
            var direction = _random.NextDouble() * Math.PI * 2;
            var length = 0;
            
            // Generate river segments until we reach a lake, ocean, or max length
            while (length < _settings.MaxRiverLength && IsInWorldBounds(currentX, currentZ))
            {
                // Calculate meander
                direction += (_random.NextDouble() - 0.5) * riverSystem.MeanderFactor;
                
                // Calculate next position based on direction and elevation
                var nextX = currentX + Math.Cos(direction) * _settings.RiverSegmentLength;
                var nextZ = currentZ + Math.Sin(direction) * _settings.RiverSegmentLength;
                
                // Find the height at the next position
                var nextY = GetHeightAtPosition(nextX, nextZ, chunkX, chunkZ, heightMap);
                
                // Adjust direction based on elevation (water flows downhill)
                direction = AdjustDirectionForElevation(direction, currentY, nextY);
                
                // Create river segment
                var segment = new RiverSegment
                {
                    Id = _random.Next(),
                    RiverSystemId = riverSystem.Id,
                    StartX = currentX,
                    StartY = currentY,
                    StartZ = currentZ,
                    EndX = nextX,
                    EndY = nextY,
                    EndZ = nextZ,
                    Width = riverSystem.Width,
                    Depth = CalculateRiverDepth(riverSystem, length),
                    FlowRate = riverSystem.FlowRate,
                    Type = length == 0 ? RiverSegmentType.Source : RiverSegmentType.Main
                };
                
                _riverSegments[chunkKey].Add(segment);
                
                // Carve the river segment
                CarveRiverSegment(segment, chunkX, chunkZ, heightMap, blockTypes);
                
                // Check for tributaries
                if (_random.NextDouble() < riverSystem.TributaryProbability)
                {
                    GenerateTributary(segment, riverSystem, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Update position
                currentX = nextX;
                currentY = nextY;
                currentZ = nextZ;
                length++;
                
                // Check if we've reached a lake or ocean
                if (HasReachedWaterBody(currentX, currentY, currentZ, chunkX, chunkZ, heightMap, blockTypes))
                {
                    // Create a river mouth
                    var mouthSegment = new RiverSegment
                    {
                        Id = _random.Next(),
                        RiverSystemId = riverSystem.Id,
                        StartX = currentX,
                        StartY = currentY,
                        StartZ = currentZ,
                        EndX = currentX,
                        EndY = currentY,
                        EndZ = currentZ,
                        Width = riverSystem.Width * 2, // Wider at the mouth
                        Depth = CalculateRiverDepth(riverSystem, length) * 1.5f,
                        FlowRate = riverSystem.FlowRate,
                        Type = RiverSegmentType.Mouth
                    };
                    
                    _riverSegments[chunkKey].Add(mouthSegment);
                    CarveRiverSegment(mouthSegment, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Generate river segments
        /// </summary>
        private void GenerateRiverSegments(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // River segments are generated in GenerateRiverSystem
        }
        
        /// <summary>
        /// Generate river features
        /// </summary>
        private void GenerateRiverFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate features for each river segment in this chunk
            foreach (var segment in _riverSegments[chunkKey])
            {
                // Generate waterfalls for steep segments
                if (IsSteepSegment(segment))
                {
                    var waterfall = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Waterfall,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = segment.StartY,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Height = (int)(segment.StartY - segment.EndY),
                        Width = segment.Width
                    };
                    
                    _riverFeatures[chunkKey].Add(waterfall);
                    AddWaterfallFeature(waterfall, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Generate rapids for moderate slopes
                if (IsModerateSlopeSegment(segment))
                {
                    var rapids = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Rapids,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = (segment.StartY + segment.EndY) / 2,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Width = segment.Width,
                        Intensity = _random.NextDouble()
                    };
                    
                    _riverFeatures[chunkKey].Add(rapids);
                    AddRapidsFeature(rapids, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Generate river bends
                if (IsBendSegment(segment))
                {
                    var bend = new RiverFeature
                    {
                        Id = _random.Next(),
                        RiverSegmentId = segment.Id,
                        Type = RiverFeatureType.Bend,
                        PositionX = (segment.StartX + segment.EndX) / 2,
                        PositionY = (segment.StartY + segment.EndY) / 2,
                        PositionZ = (segment.StartZ + segment.EndZ) / 2,
                        Width = segment.Width,
                        Curvature = CalculateBendCurvature(segment)
                    };
                    
                    _riverFeatures[chunkKey].Add(bend);
                    AddBendFeature(bend, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Generate a tributary
        /// </summary>
        private void GenerateTributary(RiverSegment parentSegment, RiverSystem parentSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Create a new river system for the tributary
            var tributarySystem = new RiverSystem
            {
                Id = _random.Next(),
                SourceX = parentSegment.StartX + _random.Next(-10, 11),
                SourceY = parentSegment.StartY + _random.Next(5, 11), // Start higher
                SourceZ = parentSegment.StartZ + _random.Next(-10, 11),
                Width = Math.Max(1, parentSystem.Width / 2), // Narrower than parent
                FlowRate = Math.Max(1, parentSystem.FlowRate / 2), // Less flow
                Type = parentSystem.Type,
                MeanderFactor = parentSystem.MeanderFactor,
                TributaryProbability = parentSystem.TributaryProbability * 0.5 // Less likely to have its own tributaries
            };
            
            _riverSystems[chunkKey].Add(tributarySystem);
            
            // Generate a short segment to connect to the parent
            var connectionSegment = new RiverSegment
            {
                Id = _random.Next(),
                RiverSystemId = tributarySystem.Id,
                StartX = tributarySystem.SourceX,
                StartY = tributarySystem.SourceY,
                StartZ = tributarySystem.SourceZ,
                EndX = parentSegment.StartX,
                EndY = parentSegment.StartY,
                EndZ = parentSegment.StartZ,
                Width = tributarySystem.Width,
                Depth = CalculateRiverDepth(tributarySystem, 0),
                FlowRate = tributarySystem.FlowRate,
                Type = RiverSegmentType.Tributary
            };
            
            _riverSegments[chunkKey].Add(connectionSegment);
            CarveRiverSegment(connectionSegment, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Connect river systems
        /// </summary>
        private void ConnectRiverSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Connect nearby river systems
            for (int i = 0; i < _riverSystems[chunkKey].Count; i++)
            {
                for (int j = i + 1; j < _riverSystems[chunkKey].Count; j++)
                {
                    var system1 = _riverSystems[chunkKey][i];
                    var system2 = _riverSystems[chunkKey][j];
                    
                    var distance = Math.Sqrt(
                        Math.Pow(system1.SourceX - system2.SourceX, 2) +
                        Math.Pow(system1.SourceY - system2.SourceY, 2) +
                        Math.Pow(system1.SourceZ - system2.SourceZ, 2)
                    );
                    
                    // Connect systems if they're close enough
                    if (distance < _settings.RiverConnectionDistance && _random.NextDouble() < _settings.RiverConnectionProbability)
                    {
                        var connection = new RiverSegment
                        {
                            Id = _random.Next(),
                            RiverSystemId = system1.Id,
                            StartX = system1.SourceX,
                            StartY = system1.SourceY,
                            StartZ = system1.SourceZ,
                            EndX = system2.SourceX,
                            EndY = system2.SourceY,
                            EndZ = system2.SourceZ,
                            Width = Math.Min(system1.Width, system2.Width),
                            Depth = Math.Min(CalculateRiverDepth(system1, 0), CalculateRiverDepth(system2, 0)),
                            FlowRate = system1.FlowRate + system2.FlowRate,
                            Type = RiverSegmentType.Connection
                        };
                        
                        _riverSegments[chunkKey].Add(connection);
                        CarveRiverSegment(connection, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add waterfalls
        /// </summary>
        private void AddWaterfalls(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Waterfalls are added in GenerateRiverFeatures
        }
        
        /// <summary>
        /// Add river banks
        /// </summary>
        private void AddRiverBanks(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add banks to each river segment
            foreach (var segment in _riverSegments[chunkKey])
            {
                AddRiverBanksToSegment(segment, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a river segment
        /// </summary>
        private void CarveRiverSegment(RiverSegment segment, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndY - segment.StartY, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = segment.StartX + t * (segment.EndX - segment.StartX);
                var y = segment.StartY + t * (segment.EndY - segment.StartY);
                var z = segment.StartZ + t * (segment.EndZ - segment.StartZ);
                
                // Carve a channel at this position
                CarveRiverChannel(x, y, z, segment.Width, segment.Depth, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a river channel
        /// </summary>
        private void CarveRiverChannel(double centerX, double centerY, double centerZ, int width, int depth, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var radius = width / 2.0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is within the river width
                    var dx = worldX - centerX;
                    var dz = worldZ - centerZ;
                    var distance = Math.Sqrt(dx * dx + dz * dz);
                    
                    if (distance <= radius)
                    {
                        // Calculate the depth at this position (deeper in the center)
                        var depthFactor = 1.0 - (distance / radius);
                        var localDepth = depth * depthFactor;
                        
                        // Carve the river bed
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is within the river depth
                                if (y >= centerY - localDepth && y <= centerY)
                                {
                                    // Replace with water for the top layer
                                    if (y >= centerY - 1)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                    else
                                    {
                                        // Use sand/gravel for the river bed
                                        blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Sand : (int)BlockType.Gravel;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add waterfall feature
        /// </summary>
        private void AddWaterfallFeature(RiverFeature waterfall, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var x = (int)waterfall.PositionX;
            var z = (int)waterfall.PositionZ;
            var startY = (int)waterfall.PositionY;
            var endY = startY - waterfall.Height;
            
            // Create the waterfall
            for (int y = endY; y <= startY; y++)
            {
                for (int wx = -waterfall.Width / 2; wx <= waterfall.Width / 2; wx++)
                {
                    for (int wz = -waterfall.Width / 2; wz <= waterfall.Width / 2; wz++)
                    {
                        var worldX = x + wx;
                        var worldZ = z + wz;
                        
                        // Check if this position is within the chunk
                        if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                            worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                        {
                            var localX = worldX - chunkX * 16;
                            var localZ = worldZ - chunkZ * 16;
                            var index = y * 16 * 16 + localZ * 16 + localX;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Use water for the waterfall
                                blockTypes[index] = (int)BlockType.Water;
                            }
                        }
                    }
                }
            }
            
            // Create a pool at the bottom of the waterfall
            CreateWaterfallPool(x, endY, z, waterfall.Width, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Create a pool at the bottom of a waterfall
        /// </summary>
        private void CreateWaterfallPool(int x, int y, int z, int width, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var poolRadius = width * 2;
            var poolDepth = width / 2;
            
            for (int px = -poolRadius; px <= poolRadius; px++)
            {
                for (int pz = -poolRadius; pz <= poolRadius; pz++)
                {
                    var distance = Math.Sqrt(px * px + pz * pz);
                    
                    if (distance <= poolRadius)
                    {
                        var worldX = x + px;
                        var worldZ = z + pz;
                        
                        // Check if this position is within the chunk
                        if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                            worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                        {
                            var localX = worldX - chunkX * 16;
                            var localZ = worldZ - chunkZ * 16;
                            
                            // Calculate depth based on distance from center
                            var depthFactor = 1.0 - (distance / poolRadius);
                            var localDepth = (int)(poolDepth * depthFactor);
                            
                            for (int py = y - localDepth; py <= y; py++)
                            {
                                var index = py * 16 * 16 + localZ * 16 + localX;
                                
                                if (index >= 0 && index < blockTypes.Length)
                                {
                                    if (py >= y - 1)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                    else
                                    {
                                        blockTypes[index] = (int)BlockType.Sand;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add rapids feature
        /// </summary>
        private void AddRapidsFeature(RiverFeature rapids, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var x = (int)rapids.PositionX;
            var z = (int)rapids.PositionZ;
            var y = (int)rapids.PositionY;
            
            // Create rapids by adding air blocks in the water
            for (int rx = -rapids.Width / 2; rx <= rapids.Width / 2; rx++)
            {
                for (int rz = -rapids.Width / 2; rz <= rapids.Width / 2; rz++)
                {
                    var worldX = x + rx;
                    var worldZ = z + rz;
                    
                    // Check if this position is within the chunk
                    if (worldX >= chunkX * 16 && worldX < (chunkX + 1) * 16 &&
                        worldZ >= chunkZ * 16 && worldZ < (chunkZ + 1) * 16)
                    {
                        var localX = worldX - chunkX * 16;
                        var localZ = worldZ - chunkZ * 16;
                        
                        // Add some air blocks to simulate rapids
                        if (_random.NextDouble() < rapids.Intensity)
                        {
                            var index = y * 16 * 16 + localZ * 16 + localX;
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == (int)BlockType.Water)
                            {
                                blockTypes[index] = 0; // Air
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add bend feature
        /// </summary>
        private void AddBendFeature(RiverFeature bend, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Bends are naturally created by the meandering algorithm
            // We can add some visual enhancements here if needed
        }
        
        /// <summary>
        /// Add river banks to a segment
        /// </summary>
        private void AddRiverBanksToSegment(RiverSegment segment, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndY - segment.StartY, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = segment.StartX + t * (segment.EndX - segment.StartX);
                var y = segment.StartY + t * (segment.EndY - segment.StartY);
                var z = segment.StartZ + t * (segment.EndZ - segment.StartZ);
                
                // Add banks at this position
                AddRiverBanksAtPosition(x, y, z, segment.Width, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Add river banks at a position
        /// </summary>
        private void AddRiverBanksAtPosition(double centerX, double centerY, double centerZ, int width, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var radius = width / 2.0;
            var bankWidth = 2; // Width of the river banks
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    // Check if this position is near the river edge
                    var dx = worldX - centerX;
                    var dz = worldZ - centerZ;
                    var distance = Math.Sqrt(dx * dx + dz * dz);
                    
                    // Add banks at the edge of the river
                    if (distance > radius && distance <= radius + bankWidth)
                    {
                        for (int y = 0; y < 256; y++)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Check if this block is at the right height
                                if (Math.Abs(y - centerY) <= 1)
                                {
                                    // Use dirt or sand for river banks
                                    if (blockTypes[index] == 0 || blockTypes[index] == (int)BlockType.Water)
                                    {
                                        blockTypes[index] = _random.NextDouble() < 0.7 ? (int)BlockType.Dirt : (int)BlockType.Sand;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        #region Utility Methods
        
        /// <summary>
        /// Check if a chunk should contain a river source
        /// </summary>
        private bool ShouldContainRiverSource(int chunkX, int chunkZ)
        {
            // Use a noise function to determine if this chunk should contain a river source
            var noise = SimpleNoise(chunkX * 0.1, chunkZ * 0.1, _settings.Seed);
            return noise > _settings.RiverSourceThreshold;
        }
        
        /// <summary>
        /// Get the height for a river source
        /// </summary>
        private int GetRiverSourceHeight(int chunkX, int chunkZ, int[] heightMap)
        {
            // Find a high point in the chunk for the river source
            var maxHeight = 0;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var index = x + z * 16;
                    if (index >= 0 && index < heightMap.Length)
                    {
                        maxHeight = Math.Max(maxHeight, heightMap[index]);
                    }
                }
            }
            
            // Return a height slightly below the maximum
            return maxHeight - _random.Next(5, 15);
        }
        
        /// <summary>
        /// Get the height at a position
        /// </summary>
        private int GetHeightAtPosition(double x, double z, int chunkX, int chunkZ, int[] heightMap)
        {
            // Convert world coordinates to chunk coordinates
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Check if the position is within the chunk
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                var index = localX + localZ * 16;
                if (index >= 0 && index < heightMap.Length)
                {
                    return heightMap[index];
                }
            }
            
            // Return a default height if outside the chunk
            return 64;
        }
        
        /// <summary>
        /// Adjust direction based on elevation
        /// </summary>
        private double AdjustDirectionForElevation(double direction, double currentY, double nextY)
        {
            // Water flows downhill, so adjust direction if needed
            if (nextY > currentY)
            {
                // We're going uphill, adjust direction
                return direction + Math.PI / 4; // Turn 45 degrees
            }
            
            return direction;
        }
        
        /// <summary>
        /// Check if position is in world bounds
        /// </summary>
        private bool IsInWorldBounds(double x, double z)
        {
            return x >= 0 && x < 30000000 && z >= 0 && z < 30000000;
        }
        
        /// <summary>
        /// Check if we've reached a water body
        /// </summary>
        private bool HasReachedWaterBody(double x, double y, double z, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Convert world coordinates to chunk coordinates
            var localX = (int)(x - chunkX * 16);
            var localZ = (int)(z - chunkZ * 16);
            
            // Check if the position is within the chunk
            if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
            {
                for (int py = (int)y - 5; py <= (int)y + 5; py++)
                {
                    if (py >= 0 && py < 256)
                    {
                        var index = py * 16 * 16 + localZ * 16 + localX;
                        if (index >= 0 && index < blockTypes.Length)
                        {
                            // Check if we've reached water or a very low area
                            if (blockTypes[index] == (int)BlockType.Water || py < 50)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate river depth
        /// </summary>
        private int CalculateRiverDepth(RiverSystem riverSystem, int length)
        {
            // Rivers get deeper as they get longer
            var baseDepth = riverSystem.Width / 3;
            var lengthFactor = Math.Min(1.0, length / 100.0);
            return (int)(baseDepth * (1 + lengthFactor));
        }
        
        /// <summary>
        /// Check if a segment is steep
        /// </summary>
        private bool IsSteepSegment(RiverSegment segment)
        {
            var heightDiff = segment.StartY - segment.EndY;
            var horizontalDistance = Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            var slope = horizontalDistance > 0 ? heightDiff / horizontalDistance : 0;
            return slope > 0.5; // Steep if slope > 0.5
        }
        
        /// <summary>
        /// Check if a segment has moderate slope
        /// </summary>
        private bool IsModerateSlopeSegment(RiverSegment segment)
        {
            var heightDiff = segment.StartY - segment.EndY;
            var horizontalDistance = Math.Sqrt(
                Math.Pow(segment.EndX - segment.StartX, 2) +
                Math.Pow(segment.EndZ - segment.StartZ, 2)
            );
            
            var slope = horizontalDistance > 0 ? heightDiff / horizontalDistance : 0;
            return slope > 0.1 && slope <= 0.5; // Moderate if 0.1 < slope <= 0.5
        }
        
        /// <summary>
        /// Check if a segment is a bend
        /// </summary>
        private bool IsBendSegment(RiverSegment segment)
        {
            // This is a simplified check - in a real implementation,
            // we'd need to look at the previous and next segments
            return _random.NextDouble() < 0.2; // 20% chance of being a bend
        }
        
        /// <summary>
        /// Calculate bend curvature
        /// </summary>
        private double CalculateBendCurvature(RiverSegment segment)
        {
            // This is a simplified calculation - in a real implementation,
            // we'd need to look at the previous and next segments
            return _random.NextDouble();
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
    /// River system information
    /// </summary>
    public class RiverSystem
    {
        public int Id { get; set; }
        public double SourceX { get; set; }
        public int SourceY { get; set; }
        public double SourceZ { get; set; }
        public int Width { get; set; }
        public int FlowRate { get; set; }
        public RiverType Type { get; set; }
        public double MeanderFactor { get; set; }
        public double TributaryProbability { get; set; }
    }
    
    /// <summary>
    /// River segment information
    /// </summary>
    public class RiverSegment
    {
        public int Id { get; set; }
        public int RiverSystemId { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double EndZ { get; set; }
        public int Width { get; set; }
        public int Depth { get; set; }
        public int FlowRate { get; set; }
        public RiverSegmentType Type { get; set; }
    }
    
    /// <summary>
    /// River feature information
    /// </summary>
    public class RiverFeature
    {
        public int Id { get; set; }
        public int RiverSegmentId { get; set; }
        public RiverFeatureType Type { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double PositionZ { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Intensity { get; set; }
        public double Curvature { get; set; }
    }
    
    /// <summary>
    /// River types
    /// </summary>
    public enum RiverType
    {
        Mountain,
        Plains,
        Jungle,
        Desert,
        Snowy
    }
    
    /// <summary>
    /// River segment types
    /// </summary>
    public enum RiverSegmentType
    {
        Source,
        Main,
        Tributary,
        Connection,
        Mouth
    }
    
    /// <summary>
    /// River feature types
    /// </summary>
    public enum RiverFeatureType
    {
        Waterfall,
        Rapids,
        Bend,
        Pool,
        Delta
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
        Cobweb = 14
    }
    
    #endregion
}
}
                
                // Add to path
                var widthVariation = 1.0 + Math.Sin(i * 0.1) * 0.2; // 20% width variation
                river.Path.Add(new RiverPoint
                {
                    X = currentX,
                    Y = currentY,
                    Z = currentZ,
                    Width = river.Width * widthVariation
                });
                
                // Check if river reached water body
                if (HasReachedWaterBody(currentX, currentZ))
                {
                    break;
                }
            }
        }
        
        /// <summary>
        /// Calculate meander angle for natural river curves
        /// </summary>
        private double CalculateMeanderAngle(int step, int totalSteps, Random random)
        {
            var progress = step / (double)totalSteps;
            
            // Use sine waves for natural meandering
            var primaryWave = Math.Sin(progress * Math.PI * 4) * RiverMeanderStrength;
            var secondaryWave = Math.Sin(progress * Math.PI * 8) * RiverMeanderStrength * 0.3;
            var randomVariation = (random.NextDouble() - 0.5) * 0.2;
            
            return (primaryWave + secondaryWave + randomVariation) * (Math.PI / 4); // Max 45 degrees
        }
        
        /// <summary>
        /// Rotate a 2D direction by an angle
        /// </summary>
        private Vector2D RotateDirection(Vector2D direction, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            
            return new Vector2D(
                direction.X * cos - direction.Y * sin,
                direction.X * sin + direction.Y * cos
            );
        }
        
        /// <summary>
        /// Find downhill direction with meandering influence
        /// </summary>
        private Vector2D FindDownhillDirection(int x, int z, Vector2D preferredDirection)
        {
            var directions = new[]
            {
                new Vector2D(1, 0),   // East
                new Vector2D(1, 1),   // Southeast
                new Vector2D(0, 1),   // South
                new Vector2D(-1, 1),  // Southwest
                new Vector2D(-1, 0),  // West
                new Vector2D(-1, -1), // Northwest
                new Vector2D(0, -1),  // North
                new Vector2D(1, -1)   // Northeast
            };
            
            var bestDirection = new Vector2D(0, 0);
            var bestScore = -1.0;
            
            foreach (var direction in directions)
            {
                var testX = x + (int)(direction.X * 3);
                var testZ = z + (int)(direction.Y * 3);
                
                // Calculate downhill slope
                var slope = CalculateDownhillSlope(x, z, testX, testZ);
                
                // Calculate alignment with preferred direction
                var alignment = direction.X * preferredDirection.X + direction.Y * preferredDirection.Y;
                
                // Combined score (slope is most important)
                var score = slope * 0.7 + alignment * 0.3;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// Check if river has reached a water body
        /// </summary>
        private bool HasReachedWaterBody(int x, int z)
        {
            var waterLevel = _worldManager.GlobalWaterLevel;
            var terrainHeight = _worldManager.GetTerrainHeight(x, z);
            
            return terrainHeight <= waterLevel;
        }
        
        /// <summary>
        /// Generate tributaries for a main river
        /// </summary>
        private void GenerateTributaries(RiverSystem mainRiver, List<RiverSystem> systems, int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            var tributaryRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 987);
            var tributaryCount = tributaryRandom.Next(0, RiverMaxTributaries + 1);
            
            for (int i = 0; i < tributaryCount; i++)
            {
                if (tributaryRandom.NextDouble() < RiverTributaryChance / 100.0)
                {
                    var tributary = GenerateTributary(mainRiver, tributaryRandom);
                    if (tributary != null)
                    {
                        systems.Add(tributary);
                        mainRiver.Tributaries.Add(tributary);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a single tributary
        /// </summary>
        private RiverSystem? GenerateTributary(RiverSystem mainRiver, Random random)
        {
            if (mainRiver.Path.Count < 10)
                return null;
                
            // Choose join point along main river
            var joinIndex = random.Next(mainRiver.Path.Count / 4, mainRiver.Path.Count * 3 / 4);
            var joinPoint = mainRiver.Path[joinIndex];
            
            // Determine tributary parameters
            var length = random.Next(RiverMinLength / 2, RiverMaxLength / 2);
            var width = mainRiver.Width * 0.6; // Tributaries are narrower
            
            // Find source point uphill from join point
            var sourceDirection = FindUphillDirection(joinPoint.X, joinPoint.Z);
            var sourceX = joinPoint.X + (int)(sourceDirection.X * length / 2);
            var sourceZ = joinPoint.Z + (int)(sourceDirection.Y * length / 2);
            var sourceY = _worldManager.GetTerrainHeight(sourceX, sourceZ);
            
            var tributary = new RiverSystem
            {
                SourceX = sourceX,
                SourceY = sourceY,
                SourceZ = sourceZ,
                Length = length,
                Width = width,
                FlowDirection = new Vector2D(-sourceDirection.X, -sourceDirection.Y), // Flow towards main river
                Path = new List<RiverPoint>(),
                Tributaries = new List<RiverSystem>()
            };
            
            // Generate tributary path
            GenerateTributaryPath(tributary, joinPoint, random);
            
            return tributary;
        }
        
        /// <summary>
        /// Find uphill direction from a point
        /// </summary>
        private Vector2D FindUphillDirection(int x, int z)
        {
            var directions = new[]
            {
                new Vector2D(1, 0),   // East
                new Vector2D(-1, 0),  // West
                new Vector2D(0, 1),   // South
                new Vector2D(0, -1),  // North
                new Vector2D(1, 1),   // Southeast
                new Vector2D(-1, 1),  // Southwest
                new Vector2D(1, -1),  // Northeast
                new Vector2D(-1, -1)  // Northwest
            };
            
            var bestDirection = new Vector2D(0, 0);
            var maxSlope = 0.0;
            
            foreach (var direction in directions)
            {
                var testX = x + (int)(direction.X * 8);
                var testZ = z + (int)(direction.Y * 8);
                var slope = -CalculateDownhillSlope(x, z, testX, testZ); // Negative for uphill
                
                if (slope > maxSlope)
                {
                    maxSlope = slope;
                    bestDirection = direction;
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// Generate tributary path that joins main river
        /// </summary>
        private void GenerateTributaryPath(RiverSystem tributary, RiverPoint joinPoint, Random random)
        {
            var currentX = tributary.SourceX;
            var currentY = tributary.SourceY;
            var currentZ = tributary.SourceZ;
            
            tributary.Path.Add(new RiverPoint
            {
                X = currentX,
                Y = currentY,
                Z = currentZ,
                Width = tributary.Width
            });
            
            var steps = tributary.Length / 2;
            for (int i = 0; i < steps; i++)
            {
                // Calculate direction towards join point
                var dx = joinPoint.X - currentX;
                var dz = joinPoint.Z - currentZ;
                var distance = Math.Sqrt(dx * dx + dz * dz);
                
                if (distance < 3)
                    break; // Close enough to join
                    
                var direction = new Vector2D(dx / distance, dz / distance);
                
                // Add some meandering
                var meanderAngle = (random.NextDouble() - 0.5) * 0.3;
                direction = RotateDirection(direction, meanderAngle);
                
                // Move towards join point
                var stepSize = 2;
                currentX += (int)(direction.X * stepSize);
                currentZ += (int)(direction.Y * stepSize);
                currentY = _worldManager.GetTerrainHeight(currentX, currentZ);
                
                tributary.Path.Add(new RiverPoint
                {
                    X = currentX,
                    Y = currentY,
                    Z = currentZ,
                    Width = tributary.Width * (1.0 - i / (double)steps * 0.3) // Narrow as it approaches main river
                });
            }
        }
        
        /// <summary>
        /// Get rivers that pass through this chunk
        /// </summary>
        private List<RiverSystem> GetPassingRivers(int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            // This would be implemented by checking neighboring chunks for rivers
            // For now, return empty list
            return new List<RiverSystem>();
        }
        
        /// <summary>
        /// Apply river system to chunk data
        /// </summary>
        private void GenerateRiverSystem(ChunkData chunk, RiverSystem river)
        {
            CarveRiver(chunk, river);
            
            foreach (var tributary in river.Tributaries)
            {
                CarveRiver(chunk, tributary);
            }
        }
        
        /// <summary>
        /// Carve river into terrain
        /// </summary>
        private void CarveRiver(ChunkData chunk, RiverSystem river)
        {
            for (int i = 0; i < river.Path.Count - 1; i++)
            {
                var point1 = river.Path[i];
                var point2 = river.Path[i + 1];
                
                CarveRiverSegment(chunk, point1, point2);
            }
        }
        
        /// <summary>
        /// Carve a river segment between two points
        /// </summary>
        private void CarveRiverSegment(ChunkData chunk, RiverPoint point1, RiverPoint point2)
        {
            var dx = point2.X - point1.X;
            var dz = point2.Z - point1.Z;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            var steps = Math.Max(1, (int)distance);
            
            for (int step = 0; step <= steps; step++)
            {
                var t = step / (double)steps;
                var x = point1.X + (int)(dx * t);
                var z = point1.Z + (int)(dz * t);
                var y = point1.Y + (int)((point2.Y - point1.Y) * t);
                var width = point1.Width + (point2.Width - point1.Width) * t;
                
                CarveRiverPoint(chunk, x, y, z, width);
            }
        }
        
        /// <summary>
        /// Carve river at a specific point
        /// </summary>
        private void CarveRiverPoint(ChunkData chunk, int worldX, int worldY, int worldZ, double width)
        {
            var radius = (int)(width / 2.0);
            var depth = (int)(width * RiverDepthFactor);
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var distSq = dx * dx + dz * dz;
                    if (distSq <= radius * radius)
                    {
                        var localX = worldX + dx;
                        var localZ = worldZ + dz;
                        
                        // Check if within chunk bounds
                        if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
                        {
                            // Calculate river depth at this point
                            var distFromCenter = Math.Sqrt(distSq);
                            var depthFactor = 1.0 - (distFromCenter / radius);
                            var riverDepth = (int)(depth * depthFactor);
                            
                            // Carve river bed
                            for (int y = worldY; y >= worldY - riverDepth; y--)
                            {
                                if (y >= 0 && y < 256)
                                {
                                    chunk.SetBlock(localX, y, localZ, BlockType.Water);
                                }
                            }
                            
                            // Create river banks
                            CreateRiverBanks(chunk, localX, worldY, localZ, radius, distFromCenter);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create natural river banks
        /// </summary>
        private void CreateRiverBanks(ChunkData chunk, int x, int y, int z, int radius, double distFromCenter)
        {
            if (distFromCenter > radius * 0.7 && distFromCenter <= radius)
            {
                // Create bank slope
                var bankHeight = (int)((distFromCenter - radius * 0.7) / (radius * 0.3) * RiverBankSteepness);
                
                for (int by = 1; by <= bankHeight; by++)
                {
                    var blockY = y + by;
                    if (blockY >= 0 && blockY < 256)
                    {
                        // Use sand or dirt for banks based on moisture
                        var blockType = _random.NextDouble() < 0.7 ? BlockType.Sand : BlockType.Dirt;
                        chunk.SetBlock(x, blockY, z, blockType);
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
    /// Represents a complete river system with tributaries
    /// </summary>
    public class RiverSystem
    {
        public int SourceX { get; set; }
        public int SourceY { get; set; }
        public int SourceZ { get; set; }
        public int Length { get; set; }
        public double Width { get; set; }
        public Vector2D FlowDirection { get; set; }
        public List<RiverPoint> Path { get; set; } = new();
        public List<RiverSystem> Tributaries { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point along a river path
    /// </summary>
    public class RiverPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Width { get; set; }
    }
    
    /// <summary>
    /// 2D vector for direction calculations
    /// </summary>
    public class Vector2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
}

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced river generation system with realistic hydrology,
    /// natural meandering patterns, and proper elevation-based flow.
    /// </summary>
    public class ImprovedRiverGenerator
    {
        private readonly WorldManager _worldManager;
        private readonly Random _random;
        private readonly RiverGenerationSettings _settings;
        
        // River generation parameters
        private const int RiverMinLength = 100;
        private const int RiverMaxLength = 500;
        private const double RiverMinWidth = 3.0;
        private const double RiverMaxWidth = 12.0;
        private const double RiverMeanderStrength = 0.7;
        private const double RiverSlopeFactor = 0.02;
        private const int RiverTributaryChance = 35; // 35% chance of tributaries
        private const int RiverMaxTributaries = 3;
        private const double RiverDepthFactor = 0.3;
        private const double RiverBankSteepness = 2.5;
        
        public ImprovedRiverGenerator(WorldManager worldManager)
        {
            _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
            _random = worldManager.GetChunkRandom(0, 0, 0);
            _settings = worldManager._riverSettings;
        }
        
        /// <summary>
        /// Generate enhanced river system for a chunk
        /// </summary>
        public void GenerateRivers(ChunkData chunk, int chunkX, int chunkZ)
        {
            if (!_worldManager._enableRivers || !_worldManager._useImprovedRivers)
                return;
                
            var riverSystems = GenerateRiverSystems(chunkX, chunkZ);
            
            foreach (var riverSystem in riverSystems)
            {
                GenerateRiverSystem(chunk, riverSystem);
            }
        }
        
        /// <summary>
        /// Generate multiple river systems with realistic hydrology
        /// </summary>
        private List<RiverSystem> GenerateRiverSystems(int chunkX, int chunkZ)
        {
            var systems = new List<RiverSystem>();
            var worldSeed = _worldManager.GetWorldSeed();
            
            // Check if this chunk should contain river sources
            if (ShouldContainRiverSource(chunkX, chunkZ))
            {
                var mainRiver = GenerateMainRiver(chunkX, chunkZ, worldSeed);
                if (mainRiver != null)
                {
                    systems.Add(mainRiver);
                    
                    // Generate tributaries
                    GenerateTributaries(mainRiver, systems, chunkX, chunkZ, worldSeed);
                }
            }
            
            // Check for rivers passing through this chunk
            var passingRivers = GetPassingRivers(chunkX, chunkZ, worldSeed);
            systems.AddRange(passingRivers);
            
            return systems;
        }
        
        /// <summary>
        /// Determine if chunk should contain a river source
        /// </summary>
        private bool ShouldContainRiverSource(int chunkX, int chunkZ)
        {
            var sourceRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 456);
            var terrainFactor = GetRiverSourceFactor(chunkX, chunkZ);
            
            return sourceRandom.NextDouble() < 0.15 * terrainFactor; // 15% base chance modified by terrain
        }
        
        /// <summary>
        /// Get terrain factor that influences river source generation
        /// </summary>
        private double GetRiverSourceFactor(int chunkX, int chunkZ)
        {
            // Sample terrain characteristics
            var sampleX = chunkX * 16 + 8;
            var sampleZ = chunkZ * 16 + 8;
            
            var elevation = _worldManager.GetTerrainHeight(sampleX, sampleZ);
            var moisture = SimplexNoise.Generate(sampleX * 0.005f + 200, sampleZ * 0.005f + 200, 0, 3, 1.0, 723451);
            var slope = CalculateTerrainSlope(sampleX, sampleZ);
            
            // Rivers prefer high elevation, high moisture, and moderate slopes
            var elevationFactor = Math.Min(1.5, elevation / 100.0);
            var moistureFactor = Math.Min(2.0, moisture + 0.5);
            var slopeFactor = Math.Max(0.2, 1.0 - Math.Abs(slope - 0.3) * 2.0);
            
            return elevationFactor * moistureFactor * slopeFactor;
        }
        
        /// <summary>
        /// Calculate terrain slope at a position
        /// </summary>
        private double CalculateTerrainSlope(int x, int z)
        {
            var h = _worldManager.GetTerrainHeight(x, z);
            var hNorth = _worldManager.GetTerrainHeight(x, z - 1);
            var hSouth = _worldManager.GetTerrainHeight(x, z + 1);
            var hEast = _worldManager.GetTerrainHeight(x + 1, z);
            var hWest = _worldManager.GetTerrainHeight(x - 1, z);
            
            var dx = (hEast - hWest) / 2.0;
            var dz = (hSouth - hNorth) / 2.0;
            
            return Math.Sqrt(dx * dx + dz * dz);
        }
        
        /// <summary>
        /// Generate a main river with realistic flow
        /// </summary>
        private RiverSystem? GenerateMainRiver(int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            var riverRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 789);
            
            // Determine river parameters
            var length = riverRandom.Next(RiverMinLength, RiverMaxLength);
            var width = riverRandom.NextDouble() * (RiverMaxWidth - RiverMinWidth) + RiverMinWidth;
            var flowDirection = DetermineFlowDirection(chunkX, chunkZ, riverRandom);
            
            // Find source point
            var sourceX = chunkX * 16 + 8;
            var sourceZ = chunkZ * 16 + 8;
            var sourceY = _worldManager.GetTerrainHeight(sourceX, sourceZ);
            
            var river = new RiverSystem
            {
                SourceX = sourceX,
                SourceY = sourceY,
                SourceZ = sourceZ,
                Length = length,
                Width = width,
                FlowDirection = flowDirection,
                Path = new List<RiverPoint>(),
                Tributaries = new List<RiverSystem>()
            };
            
            // Generate river path
            GenerateRiverPath(river, riverRandom);
            
            return river;
        }
        
        /// <summary>
        /// Determine initial flow direction based on terrain
        /// </summary>
        private Vector2D DetermineFlowDirection(int chunkX, int chunkZ, Random random)
        {
            var centerX = chunkX * 16 + 8;
            var centerZ = chunkZ * 16 + 8;
            
            // Sample surrounding terrain to find downhill direction
            var directions = new[]
            {
                new Vector2D(1, 0),   // East
                new Vector2D(-1, 0),  // West
                new Vector2D(0, 1),   // South
                new Vector2D(0, -1),  // North
                new Vector2D(1, 1),   // Southeast
                new Vector2D(-1, 1),  // Southwest
                new Vector2D(1, -1),  // Northeast
                new Vector2D(-1, -1)  // Northwest
            };
            
            var bestDirection = new Vector2D(0, 0);
            var maxSlope = 0.0;
            
            foreach (var direction in directions)
            {
                var testX = centerX + (int)(direction.X * 16);
                var testZ = centerZ + (int)(direction.Y * 16);
                var slope = CalculateDownhillSlope(centerX, centerZ, testX, testZ);
                
                if (slope > maxSlope)
                {
                    maxSlope = slope;
                    bestDirection = direction;
                }
            }
            
            // Add some randomness if slope is minimal
            if (maxSlope < 0.1)
            {
                var randomIndex = random.Next(directions.Length);
                bestDirection = directions[randomIndex];
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// Calculate downhill slope between two points
        /// </summary>
        private double CalculateDownhillSlope(int x1, int z1, int x2, int z2)
        {
            var h1 = _worldManager.GetTerrainHeight(x1, z1);
            var h2 = _worldManager.GetTerrainHeight(x2, z2);
            var distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
            
            return distance > 0 ? Math.Max(0, (h1 - h2) / distance) : 0;
        }
        
        /// <summary>
        /// Generate realistic river path with meandering
        /// </summary>
        private void GenerateRiverPath(RiverSystem river, Random random)
        {
            var currentX = river.SourceX;
            var currentY = river.SourceY;
            var currentZ = river.SourceZ;
            var currentDirection = river.FlowDirection;
            
            river.Path.Add(new RiverPoint
            {
                X = currentX,
                Y = currentY,
                Z = currentZ,
                Width = river.Width
            });
            
            for (int i = 0; i < river.Length; i++)
            {
                // Calculate meander
                var meanderAngle = CalculateMeanderAngle(i, river.Length, random);
                var meanderDirection = RotateDirection(currentDirection, meanderAngle);
                
                // Find downhill direction
                var downhillDirection = FindDownhillDirection(currentX, currentZ, meanderDirection);
                
                // Move river
                var stepSize = 2;
                currentX += (int)(downhillDirection.X * stepSize);
                currentZ += (int)(downhillDirection.Y * stepSize);
                currentY = _worldManager.GetTerrainHeight(currentX, currentZ);
                
                // Update direction
                currentDirection = new Vector2D(downhillDirection.X, downhillDirection.Y);
                
                // Add to path
                var widthVariation = 1.0 + Math.Sin(i * 0.1) * 0.2; // 20% width variation
                river.Path.Add(new RiverPoint
                {
                    X = currentX,
                    Y = currentY,
                    Z = currentZ,
                    Width = river.Width * widthVariation
                });
                
                // Check if river reached water body
                if (HasReachedWaterBody(currentX, currentZ))
                {
                    break;
                }
            }
        }
        
        /// <summary>
        /// Calculate meander angle for natural river curves
        /// </summary>
        private double CalculateMeanderAngle(int step, int totalSteps, Random random)
        {
            var progress = step / (double)totalSteps;
            
            // Use sine waves for natural meandering
            var primaryWave = Math.Sin(progress * Math.PI * 4) * RiverMeanderStrength;
            var secondaryWave = Math.Sin(progress * Math.PI * 8) * RiverMeanderStrength * 0.3;
            var randomVariation = (random.NextDouble() - 0.5) * 0.2;
            
            return (primaryWave + secondaryWave + randomVariation) * (Math.PI / 4); // Max 45 degrees
        }
        
        /// <summary>
        /// Rotate a 2D direction by an angle
        /// </summary>
        private Vector2D RotateDirection(Vector2D direction, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            
            return new Vector2D(
                direction.X * cos - direction.Y * sin,
                direction.X * sin + direction.Y * cos
            );
        }
        
        /// <summary>
        /// Find downhill direction with meandering influence
        /// </summary>
        private Vector2D FindDownhillDirection(int x, int z, Vector2D preferredDirection)
        {
            var directions = new[]
            {
                new Vector2D(1, 0),   // East
                new Vector2D(1, 1),   // Southeast
                new Vector2D(0, 1),   // South
                new Vector2D(-1, 1),  // Southwest
                new Vector2D(-1, 0),  // West
                new Vector2D(-1, -1), // Northwest
                new Vector2D(0, -1),  // North
                new Vector2D(1, -1)   // Northeast
            };
            
            var bestDirection = new Vector2D(0, 0);
            var bestScore = -1.0;
            
            foreach (var direction in directions)
            {
                var testX = x + (int)(direction.X * 3);
                var testZ = z + (int)(direction.Y * 3);
                
                // Calculate downhill slope
                var slope = CalculateDownhillSlope(x, z, testX, testZ);
                
                // Calculate alignment with preferred direction
                var alignment = direction.X * preferredDirection.X + direction.Y * preferredDirection.Y;
                
                // Combined score (slope is most important)
                var score = slope * 0.7 + alignment * 0.3;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// Check if river has reached a water body
        /// </summary>
        private bool HasReachedWaterBody(int x, int z)
        {
            var waterLevel = _worldManager.GlobalWaterLevel;
            var terrainHeight = _worldManager.GetTerrainHeight(x, z);
            
            return terrainHeight <= waterLevel;
        }
        
        /// <summary>
        /// Generate tributaries for a main river
        /// </summary>
        private void GenerateTributaries(RiverSystem mainRiver, List<RiverSystem> systems, int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            var tributaryRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 987);
            var tributaryCount = tributaryRandom.Next(0, RiverMaxTributaries + 1);
            
            for (int i = 0; i < tributaryCount; i++)
            {
                if (tributaryRandom.NextDouble() < RiverTributaryChance / 100.0)
                {
                    var tributary = GenerateTributary(mainRiver, tributaryRandom);
                    if (tributary != null)
                    {
                        systems.Add(tributary);
                        mainRiver.Tributaries.Add(tributary);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a single tributary
        /// </summary>
        private RiverSystem? GenerateTributary(RiverSystem mainRiver, Random random)
        {
            if (mainRiver.Path.Count < 10)
                return null;
                
            // Choose join point along main river
            var joinIndex = random.Next(mainRiver.Path.Count / 4, mainRiver.Path.Count * 3 / 4);
            var joinPoint = mainRiver.Path[joinIndex];
            
            // Determine tributary parameters
            var length = random.Next(RiverMinLength / 2, RiverMaxLength / 2);
            var width = mainRiver.Width * 0.6; // Tributaries are narrower
            
            // Find source point uphill from join point
            var sourceDirection = FindUphillDirection(joinPoint.X, joinPoint.Z);
            var sourceX = joinPoint.X + (int)(sourceDirection.X * length / 2);
            var sourceZ = joinPoint.Z + (int)(sourceDirection.Y * length / 2);
            var sourceY = _worldManager.GetTerrainHeight(sourceX, sourceZ);
            
            var tributary = new RiverSystem
            {
                SourceX = sourceX,
                SourceY = sourceY,
                SourceZ = sourceZ,
                Length = length,
                Width = width,
                FlowDirection = new Vector2D(-sourceDirection.X, -sourceDirection.Y), // Flow towards main river
                Path = new List<RiverPoint>(),
                Tributaries = new List<RiverSystem>()
            };
            
            // Generate tributary path
            GenerateTributaryPath(tributary, joinPoint, random);
            
            return tributary;
        }
        
        /// <summary>
        /// Find uphill direction from a point
        /// </summary>
        private Vector2D FindUphillDirection(int x, int z)
        {
            var directions = new[]
            {
                new Vector2D(1, 0),   // East
                new Vector2D(-1, 0),  // West
                new Vector2D(0, 1),   // South
                new Vector2D(0, -1),  // North
                new Vector2D(1, 1),   // Southeast
                new Vector2D(-1, 1),  // Southwest
                new Vector2D(1, -1),  // Northeast
                new Vector2D(-1, -1)  // Northwest
            };
            
            var bestDirection = new Vector2D(0, 0);
            var maxSlope = 0.0;
            
            foreach (var direction in directions)
            {
                var testX = x + (int)(direction.X * 8);
                var testZ = z + (int)(direction.Y * 8);
                var slope = -CalculateDownhillSlope(x, z, testX, testZ); // Negative for uphill
                
                if (slope > maxSlope)
                {
                    maxSlope = slope;
                    bestDirection = direction;
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// Generate tributary path that joins main river
        /// </summary>
        private void GenerateTributaryPath(RiverSystem tributary, RiverPoint joinPoint, Random random)
        {
            var currentX = tributary.SourceX;
            var currentY = tributary.SourceY;
            var currentZ = tributary.SourceZ;
            
            tributary.Path.Add(new RiverPoint
            {
                X = currentX,
                Y = currentY,
                Z = currentZ,
                Width = tributary.Width
            });
            
            var steps = tributary.Length / 2;
            for (int i = 0; i < steps; i++)
            {
                // Calculate direction towards join point
                var dx = joinPoint.X - currentX;
                var dz = joinPoint.Z - currentZ;
                var distance = Math.Sqrt(dx * dx + dz * dz);
                
                if (distance < 3)
                    break; // Close enough to join
                    
                var direction = new Vector2D(dx / distance, dz / distance);
                
                // Add some meandering
                var meanderAngle = (random.NextDouble() - 0.5) * 0.3;
                direction = RotateDirection(direction, meanderAngle);
                
                // Move towards join point
                var stepSize = 2;
                currentX += (int)(direction.X * stepSize);
                currentZ += (int)(direction.Y * stepSize);
                currentY = _worldManager.GetTerrainHeight(currentX, currentZ);
                
                tributary.Path.Add(new RiverPoint
                {
                    X = currentX,
                    Y = currentY,
                    Z = currentZ,
                    Width = tributary.Width * (1.0 - i / (double)steps * 0.3) // Narrow as it approaches main river
                });
            }
        }
        
        /// <summary>
        /// Get rivers that pass through this chunk
        /// </summary>
        private List<RiverSystem> GetPassingRivers(int chunkX, int chunkZ, WorldSeedConfig worldSeed)
        {
            // This would be implemented by checking neighboring chunks for rivers
            // For now, return empty list
            return new List<RiverSystem>();
        }
        
        /// <summary>
        /// Apply river system to chunk data
        /// </summary>
        private void GenerateRiverSystem(ChunkData chunk, RiverSystem river)
        {
            CarveRiver(chunk, river);
            
            foreach (var tributary in river.Tributaries)
            {
                CarveRiver(chunk, tributary);
            }
        }
        
        /// <summary>
        /// Carve river into terrain
        /// </summary>
        private void CarveRiver(ChunkData chunk, RiverSystem river)
        {
            for (int i = 0; i < river.Path.Count - 1; i++)
            {
                var point1 = river.Path[i];
                var point2 = river.Path[i + 1];
                
                CarveRiverSegment(chunk, point1, point2);
            }
        }
        
        /// <summary>
        /// Carve a river segment between two points
        /// </summary>
        private void CarveRiverSegment(ChunkData chunk, RiverPoint point1, RiverPoint point2)
        {
            var dx = point2.X - point1.X;
            var dz = point2.Z - point1.Z;
            var distance = Math.Sqrt(dx * dx + dz * dz);
            var steps = Math.Max(1, (int)distance);
            
            for (int step = 0; step <= steps; step++)
            {
                var t = step / (double)steps;
                var x = point1.X + (int)(dx * t);
                var z = point1.Z + (int)(dz * t);
                var y = point1.Y + (int)((point2.Y - point1.Y) * t);
                var width = point1.Width + (point2.Width - point1.Width) * t;
                
                CarveRiverPoint(chunk, x, y, z, width);
            }
        }
        
        /// <summary>
        /// Carve river at a specific point
        /// </summary>
        private void CarveRiverPoint(ChunkData chunk, int worldX, int worldY, int worldZ, double width)
        {
            var radius = (int)(width / 2.0);
            var depth = (int)(width * RiverDepthFactor);
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var distSq = dx * dx + dz * dz;
                    if (distSq <= radius * radius)
                    {
                        var localX = worldX + dx;
                        var localZ = worldZ + dz;
                        
                        // Check if within chunk bounds
                        if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16)
                        {
                            // Calculate river depth at this point
                            var distFromCenter = Math.Sqrt(distSq);
                            var depthFactor = 1.0 - (distFromCenter / radius);
                            var riverDepth = (int)(depth * depthFactor);
                            
                            // Carve river bed
                            for (int y = worldY; y >= worldY - riverDepth; y--)
                            {
                                if (y >= 0 && y < 256)
                                {
                                    chunk.SetBlock(localX, y, localZ, BlockType.Water);
                                }
                            }
                            
                            // Create river banks
                            CreateRiverBanks(chunk, localX, worldY, localZ, radius, distFromCenter);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Create natural river banks
        /// </summary>
        private void CreateRiverBanks(ChunkData chunk, int x, int y, int z, int radius, double distFromCenter)
        {
            if (distFromCenter > radius * 0.7 && distFromCenter <= radius)
            {
                // Create bank slope
                var bankHeight = (int)((distFromCenter - radius * 0.7) / (radius * 0.3) * RiverBankSteepness);
                
                for (int by = 1; by <= bankHeight; by++)
                {
                    var blockY = y + by;
                    if (blockY >= 0 && blockY < 256)
                    {
                        // Use sand or dirt for banks based on moisture
                        var blockType = _random.NextDouble() < 0.7 ? BlockType.Sand : BlockType.Dirt;
                        chunk.SetBlock(x, blockY, z, blockType);
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
    /// Represents a complete river system with tributaries
    /// </summary>
    public class RiverSystem
    {
        public int SourceX { get; set; }
        public int SourceY { get; set; }
        public int SourceZ { get; set; }
        public int Length { get; set; }
        public double Width { get; set; }
        public Vector2D FlowDirection { get; set; }
        public List<RiverPoint> Path { get; set; } = new();
        public List<RiverSystem> Tributaries { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point along a river path
    /// </summary>
    public class RiverPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public double Width { get; set; }
    }
    
    /// <summary>
    /// 2D vector for direction calculations
    /// </summary>
    public class Vector2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
