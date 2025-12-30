
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerApp.Configuration;

namespace GameServerApp.World
{
    /// <summary>
    /// Enhanced world map control manager for terrain generation and world state synchronization.
    /// Handles client-side map rendering coordination and chunk management.
    /// </summary>
    public class WorldMapControlManager
    {
        private readonly WorldMapControlSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, WorldMapProfile> _playerProfiles;
        private readonly Dictionary<int, List<ChunkUpdate>> _chunkUpdates;
        private readonly Dictionary<int, List<MapFeature>> _mapFeatures;
        private readonly Dictionary<string, DateTime> _lastUpdateTime;
        
        // Performance tracking
        private readonly Dictionary<string, TimeSpan> _operationTimes;
        private DateTime _lastCleanup;
        private int _totalUpdates;
        
        public WorldMapControlManager(WorldMapControlSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _playerProfiles = new Dictionary<int, WorldMapProfile>();
            _chunkUpdates = new Dictionary<int, List<ChunkUpdate>>();
            _mapFeatures = new Dictionary<int, List<MapFeature>>();
            _lastUpdateTime = new Dictionary<string, DateTime>();
            _operationTimes = new Dictionary<string, TimeSpan>();
            _lastCleanup = DateTime.UtcNow;
            _totalUpdates = 0;
        }
        
        /// <summary>
        /// Initialize the world map control manager
        /// </summary>
        public void Initialize()
        {
            // Load default settings
            LoadDefaultSettings();
            
            // Initialize player profiles
            InitializePlayerProfiles();
            
            // Initialize chunk update system
            InitializeChunkUpdateSystem();
            
            // Initialize map features
            InitializeMapFeatures();
        }
        
        /// <summary>
        /// Process a player's world map request
        /// </summary>
        public async Task<WorldMapResponse> ProcessWorldMapRequestAsync(WorldMapRequest request)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Validate request
                if (!ValidateRequest(request))
                {
                    return new WorldMapResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request parameters"
                    };
                }
                
                // Get or create player profile
                var profile = GetOrCreatePlayerProfile(request.PlayerId);
                
                // Process request based on type
                WorldMapResponse response;
                switch (request.Type)
                {
                    case WorldMapRequestType.GetInitialMap:
                        response = await ProcessGetInitialMapRequest(request, profile);
                        break;
                    case WorldMapRequestType.UpdateChunk:
                        response = await ProcessUpdateChunkRequest(request, profile);
                        break;
                    case WorldMapRequestType.GetPlayerProfile:
                        response = await ProcessGetPlayerProfileRequest(request, profile);
                        break;
                    case WorldMapRequestType.UpdatePlayerProfile:
                        response = await ProcessUpdatePlayerProfileRequest(request, profile);
                        break;
                    default:
                        response = new WorldMapResponse
                        {
                            Success = false,
                            ErrorMessage = "Unknown request type"
                        };
                        break;
                }
                
                // Update statistics
                var endTime = DateTime.UtcNow;
                _operationTimes[$"{request.Type}_processing"] = endTime - startTime;
                _totalUpdates++;
                
                // Cleanup old data periodically
                if (DateTime.UtcNow - _lastCleanup > TimeSpan.FromMinutes(30))
                {
                    CleanupOldData();
                    _lastCleanup = DateTime.UtcNow;
                }
                
                return response;
            }
            catch (Exception ex)
            {
                return new WorldMapResponse
                {
                    Success = false,
                    ErrorMessage = $"Error processing request: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Get or create a player profile
        /// </summary>
        private WorldMapProfile GetOrCreatePlayerProfile(int playerId)
        {
            if (_playerProfiles.TryGetValue(playerId, out var profile))
            {
                return profile;
            }
            
            // Create a new profile with default settings
            var newProfile = new WorldMapProfile
            {
                PlayerId = playerId,
                RenderDistance = _settings.DefaultRenderDistance,
                MapScale = _settings.DefaultMapScale,
                ShowCoordinates = _settings.DefaultShowCoordinates,
                ShowBiomeInfo = _settings.DefaultShowBiomeInfo,
                TerrainQuality = _settings.DefaultTerrainQuality,
                LastUpdateTime = DateTime.UtcNow
            };
            
            _playerProfiles[playerId] = newProfile;
            return newProfile;
        }
        
        /// <summary>
        /// Process get initial map request
        /// </summary>
        private async Task<WorldMapResponse> ProcessGetInitialMapRequest(WorldMapRequest request, WorldMapProfile profile)
        {
            var response = new WorldMapResponse
            {
                Success = true,
                WorldMapData = new WorldMapData()
            };
            
            // Generate initial chunks around player
            var playerChunkX = (int)(request.PlayerX / 16);
            var playerChunkZ = (int)(request.PlayerZ / 16);
            
            var chunkData = new List<ChunkData>();
            var renderDistance = profile.RenderDistance;
            
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    var chunkX = playerChunkX + x;
                    var chunkZ = playerChunkZ + z;
                    
                    // Generate or get chunk data
                    var chunk = await GenerateOrGetChunkData(chunkX, chunkZ);
                    
                    if (chunk != null)
                    {
                        chunkData.Add(chunk);
                    }
                }
            }
            
            response.WorldMapData.Chunks = chunkData;
            response.WorldMapData.PlayerPosition = new PlayerPosition
            {
                X = request.PlayerX,
                Y = request.PlayerY,
                Z = request.PlayerZ
            };
            
            return response;
        }
        
        /// <summary>
        /// Process update chunk request
        /// </summary>
        private async Task<WorldMapResponse> ProcessUpdateChunkRequest(WorldMapRequest request, WorldMapProfile profile)
        {
            var response = new WorldMapResponse
            {
                Success = true,
                WorldMapData = new WorldMapData()
            };
            
            // Update specific chunks
            var chunkUpdates = request.ChunkUpdates ?? new List<ChunkUpdate>();
            
            foreach (var update in chunkUpdates)
            {
                // Generate or get updated chunk data
                var chunk = await GenerateOrGetChunkData(update.ChunkX, update.ChunkZ);
                
                if (chunk != null)
                {
                    // Apply the update to the chunk
                    ApplyChunkUpdate(chunk, update);
                    
                    // Add to response
                    if (response.WorldMapData.Chunks == null)
                    {
                        response.WorldMapData.Chunks = new List<ChunkData>();
                    }
                    
                    response.WorldMapData.Chunks.Add(chunk);
                }
            }
            
            return response;
        }
        
        /// <summary>
        /// Process get player profile request
        /// </summary>
        private async Task<WorldMapResponse> ProcessGetPlayerProfileRequest(WorldMapRequest request, WorldMapProfile profile)
        {
            var response = new WorldMapResponse
            {
                Success = true,
                PlayerProfile = profile
            };
            
            // Update last access time
            profile.LastUpdateTime = DateTime.UtcNow;
            _lastUpdateTime[$"player_{request.PlayerId}"] = DateTime.UtcNow;
            
            return response;
        }
        
        /// <summary>
        /// Process update player profile request
        /// </summary>
        private async Task<WorldMapResponse> ProcessUpdatePlayerProfileRequest(WorldMapRequest request, WorldMapProfile profile)
        {
            var response = new WorldMapResponse
            {
                Success = true
            };
            
            // Update profile with new settings
            if (request.ProfileUpdates != null)
            {
                foreach (var update in request.ProfileUpdates)
                {
                    switch (update.Type)
                    {
                        case ProfileUpdateType.RenderDistance:
                            profile.RenderDistance = update.Value;
                            break;
                        case ProfileUpdateType.MapScale:
                            profile.MapScale = update.Value;
                            break;
                        case ProfileUpdateType.ShowCoordinates:
                            profile.ShowCoordinates = update.Value;
                            break;
                        case ProfileUpdateType.ShowBiomeInfo:
                            profile.ShowBiomeInfo = update.Value;
                            break;
                        case ProfileUpdateType.TerrainQuality:
                            profile.TerrainQuality = (TerrainQuality)update.Value;
                            break;
                    }
                }
            }
            
            // Update last access time
            profile.LastUpdateTime = DateTime.UtcNow;
            _lastUpdateTime[$"player_{request.PlayerId}"] = DateTime.UtcNow;
            
            response.PlayerProfile = profile;
            return response;
        }
        
        /// <summary>
        /// Generate or get chunk data
        /// </summary>
        private async Task<ChunkData> GenerateOrGetChunkData(int chunkX, int chunkZ)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Check if we already have this chunk cached
            if (_chunkUpdates.TryGetValue(chunkKey, out var updates))
            {
                // Get the most recent chunk data
                var latestUpdate = updates.OrderByDescending(u => u.Timestamp).FirstOrDefault();
                
                if (latestUpdate != null)
                {
                    return new ChunkData
                    {
                        ChunkX = chunkX,
                        ChunkZ = chunkZ,
                        BlockData = latestUpdate.BlockData,
                        HeightMap = latestUpdate.HeightMap,
                        BiomeData = latestUpdate.BiomeData,
                        Timestamp = latestUpdate.Timestamp
                    };
                }
            }
            
            // Generate new chunk data
            return await GenerateNewChunkData(chunkX, chunkZ);
        }
        
        /// <summary>
        /// Generate new chunk data
        /// </summary>
        private async Task<ChunkData> GenerateNewChunkData(int chunkX, int chunkZ)
        {
            // This would typically involve calling the terrain generation system
            // For this implementation, we'll create a simple chunk
            var startTime = DateTime.UtcNow;
            
            var blockData = new int[16 * 16 * 256];
            var heightMap = new int[16 * 16];
            var biomeData = new BiomeType[16 * 16];
            
            // Generate simple terrain
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var height = 64 + (int)(_random.NextDouble() * 20 - 10);
                    heightMap[x + z * 16] = height;
                    
                    // Generate biome based on height
                    var biome = height < 50 ? BiomeType.Ocean : 
                             height < 70 ? BiomeType.Beach :
                             height < 90 ? BiomeType.Plains :
                             height < 110 ? BiomeType.Forest :
                             height < 130 ? BiomeType.Mountains :
                             BiomeType.Desert;
                    
                    biomeData[x + z * 16] = biome;
                    
                    // Generate blocks based on height
                    for (int y = 0; y < 256; y++)
                    {
                        var index = y * 16 * 16 + z * 16 + x;
                        
                        if (y < height - 5)
                        {
                            blockData[index] = (int)BlockType.Stone;
                        }
                        else if (y < height)
                        {
                            blockData[index] = (int)BlockType.Dirt;
                        }
                        else if (y < height + 2)
                        {
                            blockData[index] = (int)BlockType.Grass;
                        }
                        else
                        {
                            blockData[index] = 0; // Air
                        }
                    }
                }
            }
            
            // Add map features to this chunk
            await AddMapFeaturesToChunk(chunkX, chunkZ, blockData, heightMap, biomeData);
            
            // Store the chunk data
            var chunkData = new ChunkData
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                BlockData = blockData,
                HeightMap = heightMap,
                BiomeData = biomeData,
                Timestamp = DateTime.UtcNow
            };
            
            // Cache the chunk data
            CacheChunkData(chunkX, chunkZ, chunkData);
            
            // Track performance
            var endTime = DateTime.UtcNow;
            _operationTimes[$"chunk_generation_{chunkX}_{chunkZ}"] = endTime - startTime;
            
            return chunkData;
        }
        
        /// <summary>
        /// Apply a chunk update
        /// </summary>
        private void ApplyChunkUpdate(ChunkData chunk, ChunkUpdate update)
        {
            switch (update.Type)
            {
                case ChunkUpdateType.BlockChange:
                    // Update specific blocks
                    if (update.BlockChanges != null)
                    {
                        foreach (var change in update.BlockChanges)
                        {
                            var index = change.Y * 16 * 16 + change.Z * 16 + change.X;
                            
                            if (index >= 0 && index < chunk.BlockData.Length)
                            {
                                chunk.BlockData[index] = change.NewBlockType;
                            }
                        }
                    }
                    break;
                    
                case ChunkUpdateType.HeightMap:
                    // Update height map
                    if (update.HeightMapData != null)
                    {
                        foreach (var height in update.HeightMapData)
                        {
                            var index = height.Z * 16 + height.X;
                            
                            if (index >= 0 && index < chunk.HeightMap.Length)
                            {
                                chunk.HeightMap[index] = height.NewHeight;
                            }
                        }
                    }
                    break;
                    
                case ChunkUpdateType.BiomeData:
                    // Update biome data
                    if (update.BiomeChanges != null)
                    {
                        foreach (var biome in update.BiomeChanges)
                        {
                            var index = biome.Z * 16 + biome.X;
                            
                            if (index >= 0 && index < chunk.BiomeData.Length)
                            {
                                chunk.BiomeData[index] = biome.NewBiomeType;
                            }
                        }
                    }
                    break;
            }
            
            // Update timestamp
            chunk.Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Add map features to a chunk
        /// </summary>
        private async Task AddMapFeaturesToChunk(int chunkX, int chunkZ, int[] blockData, int[] heightMap, BiomeType[] biomeData)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Determine which features should be in this chunk
            var features = GetChunkFeatures(chunkX, chunkZ, heightMap, biomeData);
            
            // Add features to the chunk
            foreach (var feature in features)
            {
                switch (feature.Type)
                {
                    case MapFeatureType.Village:
                        await AddVillageFeature(chunkX, chunkZ, blockData, heightMap, feature);
                        break;
                    case MapFeatureType.Dungeon:
                        await AddDungeonFeature(chunkX, chunkZ, blockData, heightMap, feature);
                        break;
                    case MapFeatureType.Temple:
                        await AddTempleFeature(chunkX, chunkZ, blockData, heightMap, feature);
                        break;
                    case MapFeatureType.Mineshaft:
                        await AddMineshaftFeature(chunkX, chunkZ, blockData, heightMap, feature);
                        break;
                    case MapFeatureType.Stronghold:
                        await AddStrongholdFeature(chunkX, chunkZ, blockData, heightMap, feature);
                        break;
                }
            }
            
            // Store the features for this chunk
            _mapFeatures[chunkKey] = features;
        }
        
        /// <summary>
        /// Get features for a chunk
        /// </summary>
        private List<MapFeature> GetChunkFeatures(int chunkX, int chunkZ, int[] heightMap, BiomeType[] biomeData)
        {
            var features = new List<MapFeature>();
            var worldX = chunkX * 16;
            var worldZ = chunkZ * 16;
            
            // Use noise to determine feature placement
            var featureNoise = SimpleNoise(chunkX * 0.1, chunkZ * 0.1, _settings.Seed + 1000);
            
            // Check for villages in plains biomes
            if (featureNoise > 0.3 && HasBiomeType(biomeData, BiomeType.Plains))
            {
                features.Add(new MapFeature
                {
                    Type = MapFeatureType.Village,
                    PositionX = worldX + _random.Next(5, 12),
                    PositionY = GetSurfaceHeight(heightMap, 5, 12),
                    PositionZ = worldZ + _random.Next(5, 12),
                    Size = _random.Next(20, 50),
                    Orientation = _random.Next(0, 4)
                });
            }
            
            // Check for dungeons in most biomes
            if (featureNoise > 0.2)
            {
                features.Add(new MapFeature
                {
                    Type = MapFeatureType.Dungeon,
                    PositionX = worldX + _random.Next(2, 14),
                    PositionY = GetSurfaceHeight(heightMap, 2, 14),
                    PositionZ = worldZ + _random.Next(2, 14),
                    Size = _random.Next(10, 30),
                    Orientation = _random.Next(0, 4)
                });
            }
            
            // Check for temples in desert biomes
            if (featureNoise > 0.25 && HasBiomeType(biomeData, BiomeType.Desert))
            {
                features.Add(new MapFeature
                {
                    Type = MapFeatureType.Temple,
                    PositionX = worldX + _random.Next(3, 13),
                    PositionY = GetSurfaceHeight(heightMap, 3, 13),
                    PositionZ = worldZ + _random.Next(3, 13),
                    Size = _random.Next(15, 35),
                    Orientation = _random.Next(0, 4)
                });
            }
            
            // Check for mineshafts
            if (featureNoise > 0.15)
            {
                features.Add(new MapFeature
                {
                    Type = MapFeatureType.Mineshaft,
                    PositionX = worldX + _random.Next(1, 15),
                    PositionY = GetSurfaceHeight(heightMap, 1, 15),
                    PositionZ = worldZ + _random.Next(1, 15),
                    Size = _random.Next(5, 20),
                    Orientation = _random.Next(0, 4)
                });
            }
            
            // Check for strongholds
            if (featureNoise > 0.1)
            {
                features.Add(new MapFeature
                {
                    Type = MapFeatureType.Stronghold,
                    PositionX = worldX + _random.Next(0, 16),
                    PositionY = GetSurfaceHeight(heightMap, 0, 16),
                    PositionZ = worldZ + _random.Next(0, 16),
                    Size = _random.Next(30, 80),
                    Orientation = _random.Next(0, 4)
                });
            }
            
            return features;
        }
        
        /// <summary>
        /// Add a village feature
        /// </summary>
        private async Task AddVillageFeature(int chunkX, int chunkZ, int[] blockData, int[] heightMap, MapFeature feature)
        {
            // Generate village houses
            var houseCount = feature.Size / 10; // Rough estimate
            
            for (int i = 0; i < houseCount; i++)
            {
                var houseX = feature.PositionX + _random.Next(-20, 21);
                var houseZ = feature.PositionZ + _random.Next(-20, 21);
                var houseY = feature.PositionY;
                
                // Generate a simple house
                await GenerateHouse(houseX, houseY, houseZ, blockData, heightMap);
            }
            
            // Generate village well
            await GenerateWell(feature.PositionX, feature.PositionY, feature.PositionZ, blockData, heightMap);
        }
        
        /// <summary>
        /// Add a dungeon feature
        /// </summary>
        private async Task AddDungeonFeature(int chunkX, int chunkZ, int[] blockData, int[] heightMap, MapFeature feature)
        {
            // Generate dungeon entrance
            await GenerateDungeonEntrance(feature.PositionX, feature.PositionY, feature.PositionZ, blockData, heightMap);
            
            // Generate dungeon corridors
            var corridorCount = feature.Size / 5;
            
            for (int i = 0; i < corridorCount; i++)
            {
                var corridorX = feature.PositionX + _random.Next(-15, 16);
                var corridorZ = feature.PositionZ + _random.Next(-15, 16);
                var corridorY = feature.PositionY;
                
                await GenerateDungeonCorridor(corridorX, corridorY, corridorZ, blockData, heightMap);
            }
        }
        
        /// <summary>
        /// Add a temple feature
        /// </summary>
        private async Task AddTempleFeature(int chunkX, int chunkZ, int[] blockData, int[] heightMap, MapFeature feature)
        {
            // Generate temple base
            await GenerateTempleBase(feature.PositionX, feature.PositionY, feature.PositionZ, blockData, heightMap);
            
            // Generate temple towers
            var towerCount = 4;
            
            for (int i = 0; i < towerCount; i++)
            {
                var angle = (Math.PI * 2 / towerCount) * i;
                var towerX = feature.PositionX + (int)(Math.Cos(angle) * feature.Size / 2);
                var towerZ = feature.PositionZ + (int)(Math.Sin(angle) * feature.Size / 2);
                var towerY = feature.PositionY;
                
                await GenerateTempleTower(towerX, towerY, towerZ, blockData, heightMap);
            }
        }
        
        /// <summary>
        /// Add a mineshaft feature
        /// </summary>
        private async Task AddMineshaftFeature(int chunkX, int chunkZ, int[] blockData, int[] heightMap, MapFeature feature)
        {
            // Generate mineshaft entrance
            await GenerateMineshaftEntrance(feature.PositionX, feature.PositionY, feature.PositionZ, blockData, heightMap);
            
            // Generate mineshaft shaft
            var shaftDepth = feature.Size;
            var shaftX = feature.PositionX;
            var shaftZ = feature.PositionZ;
            var shaftY = feature.PositionY;
            
            for (int y = shaftY; y > shaftY - shaftDepth; y--)
            {
                var index = y * 16 * 16 + (shaftZ - chunkZ * 16) * 16 + (shaftX - chunkX * 16);
                
                if (index >= 0 && index < blockData.Length)
                {
                    // Create the shaft
                    if (y > shaftY - shaftDepth + 2)
                    {
                        blockData[index] = (int)BlockType.Wood; // Support beams
                    }
                    else
                    {
                        blockData[index] = 0; // Air shaft
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a stronghold feature
        /// </summary>
        private async Task AddStrongholdFeature(int chunkX, int chunkZ, int[] blockData, int[] heightMap, MapFeature feature)
        {
            // Generate stronghold base
            await GenerateStrongholdBase(feature.PositionX, feature.PositionY, feature.PositionZ, blockData, heightMap);
            
            // Generate stronghold towers
            var towerCount = 6;
            
            for (int i = 0; i < towerCount; i++)
            {
                var angle = (Math.PI * 2 / towerCount) * i;
                var towerX = feature.PositionX + (int)(Math.Cos(angle) * feature.Size / 3);
                var towerZ = feature.PositionZ + (int)(Math.Sin(angle) * feature.Size / 3);
                var towerY = feature.PositionY;
                
                await GenerateStrongholdTower(towerX, towerY, towerZ, blockData, heightMap);
            }
        }
        
        /// <summary>
        /// Generate a simple house
        /// </summary>
        private async Task GenerateHouse(int x, int y, int z, int[] blockData, int[] heightMap)
        {
            var width = 5;
            var height = 4;
            var depth = 5;
            
            // Generate house walls
            for (int wx = 0; wx < width; wx++)
            {
                for (int wz = 0; wz < depth; wz++)
                {
                    for (int wy = 0; wy < height; wy++)
                    {
                        var worldX = x + wx - width / 2;
                        var worldZ = z + wz - depth / 2;
                        var worldY = y + wy;
                        
                        var index = worldY * 16 * 16 + (worldZ - chunkZ * 16) * 16 + (worldX - chunkX * 16);
                        
                        if (index >= 0 && index < blockData.Length)
                        {
                            if (wy == 0) // Foundation
                            {
                                blockData[index] = (int)BlockType.Stone;
                            }
                            else if (wy == height - 1) // Walls
                            {
                                blockData[index] = (int)BlockType.Wood;
                            }
                            else if (wy == height - 2) // Windows
                            {
                                if (wx == 0 || wx == width - 1) // Front/back walls
                                {
                                    blockData[index] = 0; // Air for windows
                                }
                                else
                                {
                                    blockData[index] = (int)BlockType.Wood; // Wood for sides
                                }
                            }
                        }
                    }
                }
            }
            
            // Generate roof
            for (int wx = 0; wx < width + 2; wx++)
            {
                for (int wz = 0; wz < depth + 2; wz++)
                {
                    var worldX = x + wx - width / 2 - 1;
                    var worldZ = z + wz - depth / 2 - 1;
                    var worldY = y + height;
                    
                    var index = worldY * 16 * 16 + (worldZ - chunkZ * 16) * 16 + (worldX - chunkX * 16);
                    
                    if (index >= 0 && index < blockData.Length)
                    {
                        blockData[index] = (int)BlockType.Wood; // Wood roof
                    }
                }
            }
            
            // Generate door
            var doorX = x + width / 2;
            var doorZ = z + depth / 2;
            var doorY = y + 1;
            
            for (int dy = 0; dy < 2; dy++)
            {
                var worldY = doorY + dy;
                var index = worldY * 16 * 16 + (doorZ - chunkZ * 16) * 16 + (doorX - chunkX * 16);
                
                if (index >= 0 && index < blockData.Length)
                {
                    if (dy == 0) // Door bottom
                    {
                        blockData[index] = (int)BlockType.Wood;
                    }
                    else // Door top
                    {
                        blockData[index] = 0; // Air
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a well
        /// </summary>
        private async Task GenerateWell(int x, int y, int z, int[] blockData, int[] heightMap)
        {
            var radius = 2;
            var depth = 10;
            
            // Generate well shaft
            for (int wy = 0; wy < depth; wy++)
            {
                for (int wx = -radius; wx <= radius; wx++)
                {
                    for (int wz = -radius; wz <= radius; wz++)
                    {
                        var worldX = x + wx;
                        var worldZ = z + wz;
                        var worldY = y - wy;
                        
                        var index = worldY * 16 * 16 + (worldZ - chunkZ * 16) * 16 + (worldX - chunkX * 16);
                        
                        if (index >= 0 && index < blockData.Length)
                        {
                            if (wx == -radius || wx == radius || wz == -radius || wz == radius) // Shaft walls
                            {
                                blockData[index] = (int)BlockType.Stone;
                            }
                            else // Shaft interior
                            {
                                blockData[index] = wy < depth - 1 ? (int)BlockType.Water : 0; // Water at bottom, air above
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a dungeon entrance
        /// </summary>
        private async Task GenerateDungeonEntrance(int x, int y, int z, int[] blockData, int[] heightMap)
        {
            var width = 3;
            var height = 3;
            
            // Generate entrance structure
            for (int wx = 0; wx < width; wx++)
            {
                for (int wz = 0; wz < width; wz++)
                {
                    for (int wy = 0; wy < height; wy++)
                    {
                        var worldX = x + wx - width / 2;
                        var worldZ = z + wz - width / 2;
                        var worldY = y + wy;
                        
                        var index = worldY * 16 * 16 + (worldZ - chunkZ * 16) * 16 + (worldX - chunkX * 16);
                        
                        if (index >= 0 && index < blockData.Length)
                        {
                            if (wy == 0) // Floor
                            {
                                blockData[index] = (int)BlockType.Stone;
                            }
                            else if (wy == height - 1) // Walls
                            {
                                blockData[index] = (int)BlockType.Stone;
                            }
                            else // Ceiling
                            {
                                blockData[index] = (int)BlockType.Stone;
                            }
                        }
                    }
                }
            }
            
            // Generate entrance opening
            var entranceX = x + width / 2;
            var entranceZ = z + width / 2;
            var entranceY = y + 1;
            
            for (int wy = 0; wy < height; wy++)
            {
                var worldY = entranceY + wy;
                var index = worldY * 16 * 16 + (entranceZ - chunkZ * 16) * 16 + (entranceX - chunkX * 16);
                
                if (index >= 0 && index < blockData.Length)
                {
                    if (wy == 0 || wy == height - 1) // Floor and walls
                    {
                        blockData[index] = 0; // Air for opening
                    }
                    else // Ceiling
                    {
                        blockData[index] = (int)BlockType.Stone;
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a dungeon corridor
        /// </summary>
        private async Task GenerateDungeonCorridor(int x, int y, int z, int[] blockData, int[] heightMap)
        {
            var length = _random.Next(5, 15);
            var width = 3;
            var height = 3;
            
            // Generate corridor structure
            _updateQueue = new Queue<WorldMapUpdate>();
            
            // Load configuration
            _settings = _configManager.GetConfiguration<WorldMapControlSettings>("WorldMapControl") ?? new WorldMapControlSettings();
            _viewDistance = _settings.ViewDistance;
            _maxConcurrentChunkGenerations = _settings.MaxConcurrentChunkGenerations;
            _updateBatchSize = _settings.UpdateBatchSize;
            _updateInterval = TimeSpan.FromMilliseconds(_settings.UpdateIntervalMs);
            
            _activeChunkGenerations = 0;
            _lastUpdateTime = DateTime.UtcNow;
            _totalUpdatesSent = 0;
            
            // Initialize event handlers
            InitializeEventHandlers();
        }
        
        /// <summary>
        /// Initialize event handlers for world and player events
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Subscribe to player events
            _sessionManager.PlayerConnected += OnPlayerConnected;
            _sessionManager.PlayerDisconnected += OnPlayerDisconnected;
            _sessionManager.PlayerMoved += OnPlayerMoved;
            
            // Subscribe to world events
            _worldManager.ChunkGenerated += OnChunkGenerated;
            _worldManager.ChunkModified += OnChunkModified;
        }
        
        /// <summary>
        /// Handle player connection
        /// </summary>
        private void OnPlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            var profile = CreatePlayerProfile(e.PlayerId, e.Position);
            
            lock (_lockObject)
            {
                _playerProfiles[e.PlayerId] = profile;
            }
            
            // Send initial world data
            Task.Run(() => SendInitialWorldData(e.PlayerId, profile));
        }
        
        /// <summary>
        /// Handle player disconnection
        /// </summary>
        private void OnPlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            lock (_lockObject)
            {
                if (_playerProfiles.ContainsKey(e.PlayerId))
                {
                    _playerProfiles.Remove(e.PlayerId);
                }
            }
        }
        
        /// <summary>
        /// Handle player movement
        /// </summary>
        private void OnPlayerMoved(object sender, PlayerMovedEventArgs e)
        {
            WorldMapControlProfile profile;
            
            lock (_lockObject)
            {
                if (!_playerProfiles.TryGetValue(e.PlayerId, out profile))
                    return;
                    
                profile.LastPosition = e.NewPosition;
                profile.LastUpdateTime = DateTime.UtcNow;
            }
            
            // Check if new chunks need to be loaded
            CheckAndRequestNewChunks(e.PlayerId, profile);
        }
        
        /// <summary>
        /// Handle chunk generation completion
        /// </summary>
        private void OnChunkGenerated(object sender, ChunkGeneratedEventArgs e)
        {
            var chunkPos = new Vector2Int(e.ChunkX, e.ChunkZ);
            var controlData = CreateChunkControlData(e.ChunkX, e.ChunkZ, e.ChunkData);
            
            lock (_lockObject)
            {
                _chunkControlData[chunkPos] = controlData;
                _activeChunkGenerations--;
            }
            
            // Queue update for all nearby players
            QueueChunkUpdate(chunkPos, WorldMapUpdateType.ChunkGenerated);
        }
        
        /// <summary>
        /// Handle chunk modification
        /// </summary>
        private void OnChunkModified(object sender, ChunkModifiedEventArgs e)
        {
            var chunkPos = new Vector2Int(e.ChunkX, e.ChunkZ);
            
            lock (_lockObject)
            {
                if (_chunkControlData.TryGetValue(chunkPos, out var controlData))
                {
                    UpdateChunkControlData(controlData, e.Modifications);
                }
            }
            
            // Queue update for all nearby players
            QueueChunkUpdate(chunkPos, WorldMapUpdateType.ChunkModified);
        }
        
        /// <summary>
        /// Create a new player profile
        /// </summary>
        private WorldMapControlProfile CreatePlayerProfile(string playerId, Vector3 position)
        {
            return new WorldMapControlProfile
            {
                PlayerId = playerId,
                LastPosition = position,
                LastUpdateTime = DateTime.UtcNow,
                LoadedChunks = new HashSet<Vector2Int>(),
                ViewDistance = _viewDistance,
                RenderSettings = new RenderSettings
                {
                    TerrainQuality = _settings.DefaultTerrainQuality,
                    WaterQuality = _settings.DefaultWaterQuality,
                    VegetationQuality = _settings.DefaultVegetationQuality,
                    FogEnabled = _settings.DefaultFogEnabled,
                    ShadowEnabled = _settings.DefaultShadowEnabled
                },
                PerformanceSettings = new PerformanceSettings
                {
                    MaxChunkUpdatesPerFrame = _settings.DefaultMaxChunkUpdatesPerFrame,
                    ChunkLOD = _settings.DefaultChunkLOD,
                    UnloadDistance = _settings.DefaultUnloadDistance
                }
            };
        }
        
        /// <summary>
        /// Send initial world data to a player
        /// </summary>
        private async Task SendInitialWorldData(string playerId, WorldMapControlProfile profile)
        {
            var playerChunk = WorldToChunkCoordinates(profile.LastPosition);
            var chunksToLoad = GetChunksInRange(playerChunk, profile.ViewDistance);
            
            // Send initial chunks in batches
            var batches = chunksToLoad.Batch(_updateBatchSize);
            
            foreach (var batch in batches)
            {
                var chunkBatch = batch.ToList();
                await SendChunkBatch(playerId, chunkBatch);
                
                // Small delay between batches to prevent overwhelming the client
                await Task.Delay(50);
            }
            
            // Send world metadata
            await SendWorldMetadata(playerId);
        }
        
        /// <summary>
        /// Check and request new chunks for a player
        /// </summary>
        private void CheckAndRequestNewChunks(string playerId, WorldMapControlProfile profile)
        {
            var playerChunk = WorldToChunkCoordinates(profile.LastPosition);
            var chunksInRange = GetChunksInRange(playerChunk, profile.ViewDistance);
            var chunksToLoad = new List<Vector2Int>();
            
            lock (_lockObject)
            {
                foreach (var chunkPos in chunksInRange)
                {
                    if (!profile.LoadedChunks.Contains(chunkPos))
                    {
                        chunksToLoad.Add(chunkPos);
                        profile.LoadedChunks.Add(chunkPos);
                    }
                }
                
                // Mark distant chunks for unloading
                var chunksToUnload = profile.LoadedChunks
                    .Where(chunk => !IsChunkInRange(chunk, playerChunk, profile.ViewDistance + 2))
                    .ToList();
                    
                foreach (var chunk in chunksToUnload)
                {
                    profile.LoadedChunks.Remove(chunk);
                }
            }
            
            // Request chunk generation
            foreach (var chunkPos in chunksToLoad)
            {
                RequestChunkGeneration(chunkPos);
            }
            
            // Notify client of chunks to unload
            if (chunksToUnload.Count > 0)
            {
                QueuePlayerUpdate(playerId, new WorldMapUpdate
                {
                    Type = WorldMapUpdateType.ChunksUnloaded,
                    ChunkPositions = chunksToUnload.ToList()
                });
            }
        }
        
        /// <summary>
        /// Request generation of a chunk
        /// </summary>
        private void RequestChunkGeneration(Vector2Int chunkPos)
        {
            lock (_lockObject)
            {
                if (_activeChunkGenerations >= _maxConcurrentChunkGenerations)
                    return;
                    
                if (_chunkControlData.ContainsKey(chunkPos))
                    return;
                    
                _activeChunkGenerations++;
            }
            
            // Request chunk generation from world manager
            Task.Run(() => _worldManager.GenerateChunkAsync(chunkPos.x, chunkPos.y));
        }
        
        /// <summary>
        /// Send a batch of chunks to a player
        /// </summary>
        private async Task SendChunkBatch(string playerId, List<Vector2Int> chunkPositions)
        {
            var chunkDataList = new List<ChunkData>();
            
            lock (_lockObject)
            {
                foreach (var chunkPos in chunkPositions)
                {
                    if (_chunkControlData.TryGetValue(chunkPos, out var controlData))
                    {
                        chunkDataList.Add(controlData.ChunkData);
                    }
                }
            }
            
            // Create and send chunk batch packet
            var chunkBatchPacket = new ChunkBatchPacket
            {
                PlayerId = playerId,
                Chunks = { chunkDataList.Select(CreateChunkProto) }
            };
            
            await SendPacketToPlayer(playerId, chunkBatchPacket);
        }
        
        /// <summary>
        /// Send world metadata to a player
        /// </summary>
        private async Task SendWorldMetadata(string playerId)
        {
            var worldMetadata = new WorldMetadataPacket
            {
                WorldSeed = _worldManager.GetWorldSeed().ToString(),
                WorldSize = _worldManager.GetWorldSize(),
                GlobalWaterLevel = _worldManager.GlobalWaterLevel,
                DayLength = _worldManager.DayLength,
                CurrentTime = _worldManager.CurrentTime
            };
            
            await SendPacketToPlayer(playerId, worldMetadata);
        }
        
        /// <summary>
        /// Queue a chunk update for processing
        /// </summary>
        private void QueueChunkUpdate(Vector2Int chunkPos, WorldMapUpdateType updateType)
        {
            lock (_lockObject)
            {
                _updateQueue.Enqueue(new WorldMapUpdate
                {
                    Type = updateType,
                    ChunkPosition = chunkPos,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        /// <summary>
        /// Queue a player-specific update
        /// </summary>
        private void QueuePlayerUpdate(string playerId, WorldMapUpdate update)
        {
            update.PlayerId = playerId;
            update.Timestamp = DateTime.UtcNow;
            
            lock (_lockObject)
            {
                _updateQueue.Enqueue(update);
            }
        }
        
        /// <summary>
        /// Process queued updates
        /// </summary>
        public async Task ProcessUpdates()
        {
            var updatesToSend = new List<WorldMapUpdate>();
            
            lock (_lockObject)
            {
                while (_updateQueue.Count > 0 && updatesToSend.Count < _updateBatchSize)
                {
                    updatesToSend.Add(_updateQueue.Dequeue());
                }
            }
            
            if (updatesToSend.Count == 0)
                return;
                
            // Group updates by player
            var playerUpdates = updatesToSend
                .Where(u => !string.IsNullOrEmpty(u.PlayerId))
                .GroupBy(u => u.PlayerId)
                .ToDictionary(g => g.Key, g => g.ToList());
                
            // Group global updates (not player-specific)
            var globalUpdates = updatesToSend
                .Where(u => string.IsNullOrEmpty(u.PlayerId))
                .ToList();
                
            // Send player-specific updates
            foreach (var kvp in playerUpdates)
            {
                await SendPlayerUpdates(kvp.Key, kvp.Value);
            }
            
            // Send global updates to all nearby players
            if (globalUpdates.Count > 0)
            {
                await SendGlobalUpdates(globalUpdates);
            }
            
            _lastUpdateTime = DateTime.UtcNow;
            _totalUpdatesSent += updatesToSend.Count;
        }
        
        /// <summary>
        /// Send updates to a specific player
        /// </summary>
        private async Task SendPlayerUpdates(string playerId, List<WorldMapUpdate> updates)
        {
            var updatePacket = new WorldMapUpdatePacket
            {
                PlayerId = playerId,
                Updates = { updates.Select(CreateUpdateProto) }
            };
            
            await SendPacketToPlayer(playerId, updatePacket);
        }
        
        /// <summary>
        /// Send global updates to all relevant players
        /// </summary>
        private async Task SendGlobalUpdates(List<WorldMapUpdate> updates)
        {
            var affectedPlayers = GetPlayersNearUpdates(updates);
            
            foreach (var playerId in affectedPlayers)
            {
                await SendPlayerUpdates(playerId, updates);
            }
        }
        
        /// <summary>
        /// Get players who should receive updates
        /// </summary>
        private HashSet<string> GetPlayersNearUpdates(List<WorldMapUpdate> updates)
        {
            var affectedPlayers = new HashSet<string>();
            
            lock (_lockObject)
            {
                foreach (var update in updates)
                {
                    if (update.ChunkPosition.HasValue)
                    {
                        var nearbyPlayers = _playerProfiles
                            .Where(p => IsPlayerNearChunk(p.Value, update.ChunkPosition.Value))
                            .Select(p => p.Key);
                            
                        foreach (var playerId in nearbyPlayers)
                        {
                            affectedPlayers.Add(playerId);
                        }
                    }
                }
            }
            
            return affectedPlayers;
        }
        
        /// <summary>
        /// Check if a player is near a chunk
        /// </summary>
        private bool IsPlayerNearChunk(WorldMapControlProfile profile, Vector2Int chunkPos)
        {
            var playerChunk = WorldToChunkCoordinates(profile.LastPosition);
            return IsChunkInRange(chunkPos, playerChunk, profile.ViewDistance);
        }
        
        /// <summary>
        /// Create chunk control data
        /// </summary>
        private ChunkControlData CreateChunkControlData(int chunkX, int chunkZ, ChunkData chunkData)
        {
            return new ChunkControlData
            {
                ChunkPosition = new Vector2Int(chunkX, chunkZ),
                ChunkData = chunkData,
                LastModified = DateTime.UtcNow,
                ModificationCount = 0,
                HasTerrain = true,
                HasWater = chunkData.HasWater(),
                HasStructures = chunkData.HasStructures(),
                HasEntities = chunkData.HasEntities()
            };
        }
        
        /// <summary>
        /// Update chunk control data with modifications
        /// </summary>
        private void UpdateChunkControlData(ChunkControlData controlData, List<BlockModification> modifications)
        {
            controlData.LastModified = DateTime.UtcNow;
            controlData.ModificationCount += modifications.Count;
            
            // Update flags based on modifications
            foreach (var modification in modifications)
            {
                if (modification.NewBlock == BlockType.Water)
                {
                    controlData.HasWater = true;
                }
            }
        }
        
        /// <summary>
        /// Create a chunk protocol buffer
        /// </summary>
        private ChunkProto CreateChunkProto(ChunkData chunkData)
        {
            return new ChunkProto
            {
                X = chunkData.X,
                Z = chunkData.Z,
                Heightmap = { chunkData.GetHeightmap() },
                Blocks = { chunkData.GetBlockData() },
                Biomes = { chunkData.GetBiomeData() }
            };
        }
        
        /// <summary>
        /// Create an update protocol buffer
        /// </summary>
        private WorldMapUpdateProto CreateUpdateProto(WorldMapUpdate update)
        {
            var proto = new WorldMapUpdateProto
            {
                Type = (int)update.Type,
                Timestamp = ((DateTimeOffset)update.Timestamp).ToUnixTimeMilliseconds()
            };
            
            if (update.ChunkPosition.HasValue)
            {
                proto.ChunkX = update.ChunkPosition.Value.x;
                proto.ChunkZ = update.ChunkPosition.Value.y;
            }
            
            if (update.ChunkPositions != null)
            {
                proto.ChunkPositions.AddRange(update.ChunkPositions.Select(p => new ChunkPositionProto
                {
                    X = p.x,
                    Z = p.y
                }));
            }
            
            return proto;
        }
        
        /// <summary>
        /// Send a packet to a player
        /// </summary>
        private async Task SendPacketToPlayer(string playerId, IMessage packet)
        {
            try
            {
                var session = _sessionManager.GetSession(playerId);
                if (session != null)
                {
                    await session.SendPacketAsync(packet);
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error sending packet to player {playerId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert world coordinates to chunk coordinates
        /// </summary>
        private Vector2Int WorldToChunkCoordinates(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / 16.0f),
                Mathf.FloorToInt(worldPos.z / 16.0f)
            );
        }
        
        /// <summary>
        /// Get chunks in range of a position
        /// </summary>
        private IEnumerable<Vector2Int> GetChunksInRange(Vector2Int centerChunk, int viewDistance)
        {
            for (int x = centerChunk.x - viewDistance; x <= centerChunk.x + viewDistance; x++)
            {
                for (int z = centerChunk.y - viewDistance; z <= centerChunk.y + viewDistance; z++)
                {
                    yield return new Vector2Int(x, z);
                }
            }
        }
        
        /// <summary>
        /// Check if a chunk is in range of another chunk
        /// </summary>
        private bool IsChunkInRange(Vector2Int chunk, Vector2Int centerChunk, int range)
        {
            var dx = Math.Abs(chunk.x - centerChunk.x);
            var dz = Math.Abs(chunk.y - centerChunk.y);
            return dx <= range && dz <= range;
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        public WorldMapControlStats GetStats()
        {
            lock (_lockObject)
            {
                return new WorldMapControlStats
                {
                    ActivePlayers = _playerProfiles.Count,
                    LoadedChunks = _chunkControlData.Count,
                    ActiveChunkGenerations = _activeChunkGenerations,
                    QueuedUpdates = _updateQueue.Count,
                    TotalUpdatesSent = _totalUpdatesSent,
                    LastUpdateTime = _lastUpdateTime
                };
            }
        }
        
        /// <summary>
        /// Update player settings
        /// </summary>
        public void UpdatePlayerSettings(string playerId, PlayerSettingsUpdate settingsUpdate)
        {
            lock (_lockObject)
            {
                if (_playerProfiles.TryGetValue(playerId, out var profile))
                {
                    if (settingsUpdate.RenderSettings != null)
                    {
                        profile.RenderSettings = settingsUpdate.RenderSettings;
                    }
                    
                    if (settingsUpdate.PerformanceSettings != null)
                    {
                        profile.PerformanceSettings = settingsUpdate.PerformanceSettings;
                    }
                    
                    if (settingsUpdate.ViewDistance.HasValue)
                    {
                        profile.ViewDistance = settingsUpdate.ViewDistance.Value;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// World map control profile for a player
    /// </summary>
    public class WorldMapControlProfile
    {
        public string PlayerId { get; set; }
        public Vector3 LastPosition { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public HashSet<Vector2Int> LoadedChunks { get; set; }
        public int ViewDistance { get; set; }
        public RenderSettings RenderSettings { get; set; }
        public PerformanceSettings PerformanceSettings { get; set; }
    }
    
    /// <summary>
    /// Control data for a chunk
    /// </summary>
    public class ChunkControlData
    {
        public Vector2Int ChunkPosition { get; set; }
        public ChunkData ChunkData { get; set; }
        public DateTime LastModified { get; set; }
        public int ModificationCount { get; set; }
        public bool HasTerrain { get; set; }
        public bool HasWater { get; set; }
        public bool HasStructures { get; set; }
        public bool HasEntities { get; set; }
    }
    
    /// <summary>
    /// World map update
    /// </summary>
    public class WorldMapUpdate
    {
        public WorldMapUpdateType Type { get; set; }
        public string PlayerId { get; set; }
        public Vector2Int? ChunkPosition { get; set; }
        public List<Vector2Int> ChunkPositions { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Types of world map updates
    /// </summary>
    public enum WorldMapUpdateType
    {
        ChunkGenerated,
        ChunkModified,
        ChunksUnloaded,
        TerrainChanged,
        WaterLevelChanged,
        WeatherChanged
    }
    
    /// <summary>
    /// Render settings for a player
    /// </summary>
    public class RenderSettings
    {
        public int TerrainQuality { get; set; }
        public int WaterQuality { get; set; }
        public int VegetationQuality { get; set; }
        public bool FogEnabled { get; set; }
        public bool ShadowEnabled { get; set; }
    }
    
    /// <summary>
    /// Performance settings for a player
    /// </summary>
    public class PerformanceSettings
    {
        public int MaxChunkUpdatesPerFrame { get; set; }
        public int ChunkLOD { get; set; }
        public int UnloadDistance { get; set; }
    }
    
    /// <summary>
    /// Player settings update
    /// </summary>
    public class PlayerSettingsUpdate
    {
        public RenderSettings RenderSettings { get; set; }
        public PerformanceSettings PerformanceSettings { get; set; }
        public int? ViewDistance { get; set; }
    }
    
    /// <summary>
    /// World map control statistics
    /// </summary>
    public class WorldMapControlStats
    {
        public int ActivePlayers { get; set; }
        public int LoadedChunks { get; set; }
        public int ActiveChunkGenerations { get; set; }
        public int QueuedUpdates { get; set; }
        public int TotalUpdatesSent { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
    
    /// <summary>
    /// 2D integer vector
    /// </summary>
    public struct Vector2Int
    {
        public int x;
        public int y;
        
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    
    /// <summary>
    /// 3D float vector
    /// </summary>
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    
    /// <summary>
    /// Event arguments for player connected
    /// </summary>
    public class PlayerConnectedEventArgs : EventArgs
    {
        public string PlayerId { get; set; }
        public Vector3 Position { get; set; }
    }
    
    /// <summary>
    /// Event arguments for player disconnected
    /// </summary>
    public class PlayerDisconnectedEventArgs : EventArgs
    {
        public string PlayerId { get; set; }
    }
    
    /// <summary>
    /// Event arguments for player moved
    /// </summary>
    public class PlayerMovedEventArgs : EventArgs
    {
        public string PlayerId { get; set; }
        public Vector3 OldPosition { get; set; }
        public Vector3 NewPosition { get; set; }
    }
    
    /// <summary>
    /// Event arguments for chunk generated
    /// </summary>
    public class ChunkGeneratedEventArgs : EventArgs
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public ChunkData ChunkData { get; set; }
    }
    
    /// <summary>
    /// Event arguments for chunk modified
    /// </summary>
    public class ChunkModifiedEventArgs : EventArgs
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public List<BlockModification> Modifications { get; set; }
    }
    
    /// <summary>
    /// Block modification
    /// </summary>
    public class BlockModification
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public BlockType OldBlock { get; set; }
        public BlockType NewBlock { get; set; }
    }
    
    /// <summary>
    /// Extension methods for collections
    /// </summary>
    public static class CollectionExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            
            foreach (var item in source)
            {
                batch.Add(item);
                
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
}
            };
        }
        
        /// <summary>
        /// Create desert-focused profile
        /// </summary>
        private WorldMapControlProfile CreateDesertProfile()
        {
            return new WorldMapControlProfile
            {
                Name = "desert",
                Version = ProfileVersion,
                CreatedAt = DateTime.UtcNow,
                Description = "Arid desert with occasional oases and canyons",
                
                TerrainScale = 0.9,
                TerrainHeightMultiplier = 0.8,
                TerrainRoughness = 0.6,
                ContinentalnessScale = 0.0015,
                ErosionScale = 0.007,
                PeaksScale = 0.018,
                DetailScale = 0.15,
                
                CaveEnabled = true,
                CaveDensity = 0.3,
                CaveSizeMultiplier = 1.1,
                CaveComplexity = 0.3,
                CaveWaterChance = 0.1,
                CaveMinDepth = 15,
                CaveMaxDepth = 100,
                
                RiverEnabled = false,
                RiverDensity = 0.05,
                RiverWidthMultiplier = 0.3,
                RiverMeanderStrength = 0.3,
                RiverTributaryChance = 0.05,
                RiverMinLength = 30,
                RiverMaxLength = 100,
                
                LakeEnabled = true,
                LakeDensity = 0.05,
                LakeSizeMultiplier = 0.4,
                LakeDepthMultiplier = 0.6,
                LakeIslandChance = 0.05,
                LakeConnectionChance = 0.02,
                LakeMinRadius = 5,
                LakeMaxRadius = 20,
                
                BiomeTemperatureScale = 0.001,
                BiomeMoistureScale = 0.001,
                BiomeTransitionSmoothness = 0.4,
                
                VegetationDensity = 0.1,
                TreeDensity = 0.01,
                GrassDensity = 0.05,
                DesertVegetationDensity = 0.02,
                
                TerrainQuality = TerrainQuality.Medium,
                WaterQuality = WaterQuality.Low,
                CaveQuality = CaveQuality.Medium
            };
        }
        
        /// <summary>
        /// Load custom profiles from configuration
        /// </summary>
        private void LoadCustomProfiles()
        {
            try
            {
                // Load custom profiles from JSON files
                var customProfilesPath = "config/world-map-profiles.json";
                if (System.IO.File.Exists(customProfilesPath))
                {
                    var json = System.IO.File.ReadAllText(customProfilesPath);
                    var customProfiles = JsonSerializer.Deserialize<List<WorldMapControlProfile>>(json);
                    
                    if (customProfiles != null)
                    {
                        foreach (var profile in customProfiles)
                        {
                            AddProfile(profile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading custom profiles: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add a profile to the manager
        /// </summary>
        public void AddProfile(WorldMapControlProfile profile)
        {
            if (profile == null) return;
            
            lock (_profileLock)
            {
                _profiles[profile.Name] = profile;
                _profileHashes[profile.Name] = ComputeProfileHash(profile);
            }
        }
        
        /// <summary>
        /// Get a profile by name
        /// </summary>
        public WorldMapControlProfile? GetProfile(string name)
        {
            lock (_profileLock)
            {
                return _profiles.TryGetValue(name, out var profile) ? profile : null;
            }
        }
        
        /// <summary>
        /// Get all available profiles
        /// </summary>
        public List<WorldMapControlProfile> GetAllProfiles()
        {
            lock (_profileLock)
            {
                return _profiles.Values.ToList();
            }
        }
        
        /// <summary>
        /// Get profile hash for validation
        /// </summary>
        public string GetProfileHash(string name)
        {
            lock (_profileLock)
            {
                return _profileHashes.TryGetValue(name, out var hash) ? hash : string.Empty;
            }
        }
        
        /// <summary>
        /// Validate profile hash
        /// </summary>
        public bool ValidateProfileHash(string name, string hash)
        {
            lock (_profileLock)
            {
                return _profileHashes.TryGetValue(name, out var expectedHash) && 
                       expectedHash == hash;
            }
        }
        
        /// <summary>
        /// Compute hash for a profile
        /// </summary>
        private string ComputeProfileHash(WorldMapControlProfile profile)
        {
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes);
        }
        
        /// <summary>
        /// Create profile synchronization packet for client
        /// </summary>
        public WorldMapControlSyncPacket CreateSyncPacket(string profileName)
        {
            var profile = GetProfile(profileName) ?? GetProfile(DefaultProfileName);
            var hash = GetProfileHash(profileName);
            
            return new WorldMapControlSyncPacket
            {
                ProfileName = profile?.Name ?? DefaultProfileName,
                ProfileHash = hash,
                ProfileData = profile,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Apply profile to world manager
        /// </summary>
        public void ApplyProfile(string profileName)
        {
            var profile = GetProfile(profileName) ?? GetProfile(DefaultProfileName);
            if (profile == null) return;
            
            // Apply terrain settings
            _worldManager._terrainScale = profile.TerrainScale;
            _worldManager._terrainHeightMultiplier = profile.TerrainHeightMultiplier;
            _worldManager._terrainRoughness = profile.TerrainRoughness;
            
            // Apply cave settings
            _worldManager._enableCaves = profile.CaveEnabled;
            _worldManager._caveDensity = profile.CaveDensity;
            _worldManager._caveSizeMultiplier = profile.CaveSizeMultiplier;
            _worldManager._useImprovedCaves = profile.CaveQuality != CaveQuality.Low;
            
            // Apply river settings
            _worldManager._enableRivers = profile.RiverEnabled;
            _worldManager._riverDensity = profile.RiverDensity;
            _worldManager._riverWidthMultiplier = profile.RiverWidthMultiplier;
            _worldManager._useImprovedRivers = profile.WaterQuality != WaterQuality.Low;
            
            // Apply lake settings
            _worldManager._enableLakes = profile.LakeEnabled;
            _worldManager._lakeDensity = profile.LakeDensity;
            _worldManager._lakeSizeMultiplier = profile.LakeSizeMultiplier;
            _worldManager._useImprovedLakes = profile.WaterQuality != WaterQuality.Low;
            
            // Apply biome settings
            _worldManager._biomeTemperatureScale = profile.BiomeTemperatureScale;
            _worldManager._biomeMoistureScale = profile.BiomeMoistureScale;
            _worldManager._biomeTransitionSmoothness = profile.BiomeTransitionSmoothness;
            
            // Apply vegetation settings
            _worldManager._vegetationDensity = profile.VegetationDensity;
            _worldManager._treeDensity = profile.TreeDensity;
            _worldManager._grassDensity = profile.GrassDensity;
            
            // Apply performance settings
            _worldManager._chunkGenerationTimeout = profile.ChunkGenerationTimeout;
            _worldManager._maxConcurrentChunkGenerations = profile.MaxConcurrentChunkGenerations;
            _worldManager._enableChunkCaching = profile.EnableChunkCaching;
            _worldManager._maxCachedChunks = profile.MaxCachedChunks;
            
            // Apply quality settings
            _worldManager._terrainQuality = profile.TerrainQuality;
            _worldManager._waterQuality = profile.WaterQuality;
            _worldManager._caveQuality = profile.CaveQuality;
            _worldManager._enableSeamlessBorders = profile.EnableSeamlessBorders;
            _worldManager._borderSmoothingRadius = profile.BorderSmoothingRadius;
        }
        
        /// <summary>
        /// Save profiles to configuration file
        /// </summary>
        public void SaveProfiles()
        {
            try
            {
                var profilesPath = "config/world-map-profiles.json";
                var directory = System.IO.Path.GetDirectoryName(profilesPath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                var customProfiles = _profiles.Values
                    .Where(p => p.Name != DefaultProfileName && 
                               p.Name != "mountains" && 
                               p.Name != "islands" && 
                               p.Name != "desert")
                    .ToList();
                
                var json = JsonSerializer.Serialize(customProfiles, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                System.IO.File.WriteAllText(profilesPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// World map control profile containing all terrain generation settings
    /// </summary>
    public class WorldMapControlProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        
        // Terrain settings
        public double TerrainScale { get; set; } = 1.0;
        public double TerrainHeightMultiplier { get; set; } = 1.0;
        public double TerrainRoughness { get; set; } = 0.5;
        public double ContinentalnessScale { get; set; } = 0.001;
        public double ErosionScale { get; set; } = 0.005;
        public double PeaksScale { get; set; } = 0.02;
        public double DetailScale { get; set; } = 0.1;
        
        // Cave settings
        public bool CaveEnabled { get; set; } = true;
        public double CaveDensity { get; set; } = 0.5;
        public double CaveSizeMultiplier { get; set; } = 1.0;
        public double CaveComplexity { get; set; } = 0.5;
        public double CaveWaterChance { get; set; } = 0.4;
        public int CaveMinDepth { get; set; } = 20;
        public int CaveMaxDepth { get; set; } = 120;
        
        // River settings
        public bool RiverEnabled { get; set; } = true;
        public double RiverDensity { get; set; } = 0.3;
        public double RiverWidthMultiplier { get; set; } = 1.0;
        public double RiverMeanderStrength { get; set; } = 0.7;
        public double RiverTributaryChance { get; set; } = 0.35;
        public int RiverMinLength { get; set; } = 100;
        public int RiverMaxLength { get; set; } = 500;
        
        // Lake settings
        public bool LakeEnabled { get; set; } = true;
        public double LakeDensity { get; set; } = 0.2;
        public double LakeSizeMultiplier { get; set; } = 1.0;
        public double LakeDepthMultiplier { get; set; } = 1.0;
        public double LakeIslandChance { get; set; } = 0.25;
        public double LakeConnectionChance { get; set; } = 0.15;
        public int LakeMinRadius { get; set; } = 15;
        public int LakeMaxRadius { get; set; } = 80;
        
        // Biome settings
        public double BiomeTemperatureScale { get; set; } = 0.002;
        public double BiomeMoistureScale { get; set; } = 0.003;
        public double BiomeTransitionSmoothness { get; set; } = 0.3;
        
        // Vegetation settings
        public double VegetationDensity { get; set; } = 0.5;
        public double TreeDensity { get; set; } = 0.1;
        public double GrassDensity { get; set; } = 0.3;
        public double DesertVegetationDensity { get; set; } = 0.02;
        
        // Performance settings
        public int ChunkGenerationTimeout { get; set; } = 5000;
        public int MaxConcurrentChunkGenerations { get; set; } = 4;
        public bool EnableChunkCaching { get; set; } = true;
        public int MaxCachedChunks { get; set; } = 1000;
        
        // Quality settings
        public TerrainQuality TerrainQuality { get; set; } = TerrainQuality.High;
        public WaterQuality WaterQuality { get; set; } = WaterQuality.High;
        public CaveQuality CaveQuality { get; set; } = CaveQuality.Medium;
        public bool EnableSeamlessBorders { get; set; } = true;
        public int BorderSmoothingRadius { get; set; } = 2;
    }
    
    /// <summary>
    /// Terrain quality levels
    /// </summary>
    public enum TerrainQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
    
    /// <summary>
    /// Water quality levels
    /// </summary>
    public enum WaterQuality
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// Cave quality levels
    /// </summary>
    public enum CaveQuality
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// World map control synchronization packet
    /// </summary>
    public class WorldMapControlSyncPacket
    {
        public string ProfileName { get; set; } = string.Empty;
        public string ProfileHash { get; set; } = string.Empty;
        public WorldMapControlProfile? ProfileData { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
