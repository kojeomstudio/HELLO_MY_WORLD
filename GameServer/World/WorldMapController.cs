using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.World
{
    /// <summary>
    /// Server-side world map controller that manages terrain generation and chunk distribution
    /// Integrates with EnhancedTerrainGenerationPipeline and WorldMapControlProfile
    /// </summary>
    public class WorldMapController
    {
        private readonly ILogger<WorldMapController> logger;
        private readonly WorldSettings worldSettings;
        private readonly WorldGenerationConfig generationConfig;
        private readonly WorldMapControlProfile controlProfile;
        private readonly EnhancedTerrainGenerationPipeline terrainPipeline;
        
        // Chunk management
        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, Task<ChunkData>> chunkGenerationTasks = new();
        private readonly ConcurrentQueue<ChunkRequest> chunkRequestQueue = new();
        
        // Performance tracking
        private readonly Dictionary<Vector2Int, DateTime> chunkAccessTimes = new();
        private readonly Timer chunkCleanupTimer;
        
        // Cancellation
        private readonly CancellationTokenSource cancellationTokenSource = new();
        
        public WorldMapController(
            ILogger<WorldMapController> logger,
            WorldSettings worldSettings,
            WorldGenerationConfig generationConfig)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));
            this.generationConfig = generationConfig ?? throw new ArgumentNullException(nameof(generationConfig));
            
            // Create control profile from generation config
            controlProfile = WorldMapControlProfile.Create(generationConfig, worldSettings);
            
            // Initialize terrain generation pipeline
            terrainPipeline = new EnhancedTerrainGenerationPipeline(generationConfig, logger);
            
            // Start cleanup timer
            chunkCleanupTimer = new Timer(CleanupOldChunks, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            logger.LogInformation($"[WorldMapController] Initialized with profile: {controlProfile.SourceConfig} (Hash: {controlProfile.ProfileHash})");
        }
        
        /// <summary>
        /// Gets a chunk, generating it if necessary
        /// </summary>
        public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
        {
            var chunkPos = new Vector2Int(chunkX, chunkZ);
            
            // Update access time
            chunkAccessTimes[chunkPos] = DateTime.UtcNow;
            
            // Return existing chunk if already loaded
            if (loadedChunks.TryGetValue(chunkPos, out var existingChunk))
            {
                return existingChunk;
            }
            
            // Check if chunk is already being generated
            if (chunkGenerationTasks.TryGetValue(chunkPos, out var existingTask))
            {
                return await existingTask;
            }
            
            // Generate new chunk
            var generationTask = GenerateChunkAsync(chunkPos, cancellationToken);
            chunkGenerationTasks[chunkPos] = generationTask;
            
            try
            {
                var chunk = await generationTask;
                loadedChunks[chunkPos] = chunk;
                return chunk;
            }
            finally
            {
                chunkGenerationTasks.TryRemove(chunkPos, out _);
            }
        }
        
        /// <summary>
        /// Generates a chunk using the enhanced terrain pipeline
        /// </summary>
        private async Task<ChunkData> GenerateChunkAsync(Vector2Int chunkPos, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug($"[WorldMapController] Generating chunk at {chunkPos}");
                
                // Generate chunk using enhanced pipeline
                var chunkData = await terrainPipeline.GenerateChunkAsync(chunkPos.X, chunkPos.Y);
                
                if (chunkData == null)
                {
                    logger.LogError($"[WorldMapController] Failed to generate chunk at {chunkPos}");
                    return CreateEmptyChunk(chunkPos.X, chunkPos.Y);
                }
                
                // Apply control profile settings
                ApplyControlProfileSettings(chunkData);
                
                logger.LogDebug($"[WorldMapController] Successfully generated chunk at {chunkPos}");
                return chunkData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[WorldMapController] Error generating chunk at {chunkPos}");
                return CreateEmptyChunk(chunkPos.X, chunkPos.Y);
            }
        }
        
        /// <summary>
        /// Applies control profile settings to generated chunk data
        /// </summary>
        private void ApplyControlProfileSettings(ChunkData chunkData)
        {
            // Apply water level
            if (chunkData.HeightMap != null)
            {
                var size = chunkData.Size;
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        // Ensure water level consistency
                        if (chunkData.HeightMap[x, z] < controlProfile.GlobalWaterLevel)
                        {
                            chunkData.HeightMap[x, z] = controlProfile.GlobalWaterLevel;
                        }
                    }
                }
            }
            
            // Apply feature overrides based on control profile
            if (!controlProfile.EnableRivers)
            {
                chunkData.RiverMap = null;
            }
            
            if (!controlProfile.EnableLakes)
            {
                chunkData.LakeMap = null;
            }
            
            if (!controlProfile.EnableCaves)
            {
                chunkData.CaveMap = null;
            }
        }
        
        /// <summary>
        /// Creates an empty chunk as fallback
        /// </summary>
        private ChunkData CreateEmptyChunk(int chunkX, int chunkZ)
        {
            var size = generationConfig.ChunkSize;
            var chunkData = new ChunkData
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Size = size,
                HeightMap = new float[size, size],
                CaveMap = new bool[size, size, size],
                RiverMap = new float[size, size],
                LakeMap = new float[size, size]
            };
            
            // Fill with basic terrain
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    chunkData.HeightMap[x, z] = controlProfile.GlobalWaterLevel;
                }
            }
            
            return chunkData;
        }
        
        /// <summary>
        /// Gets multiple chunks for a player's view area
        /// </summary>
        public async Task<List<ChunkData>> GetChunksAroundAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
        {
            var chunks = new List<ChunkData>();
            var tasks = new List<Task<ChunkData>>();
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= radius)
                    {
                        tasks.Add(GetChunkAsync(x, z, cancellationToken));
                    }
                }
            }
            
            var chunkResults = await Task.WhenAll(tasks);
            chunks.AddRange(chunkResults.Where(c => c != null));
            
            return chunks;
        }
        
        /// <summary>
        /// Preloads chunks in a radius around the center
        /// </summary>
        public async Task PreloadChunksAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
        {
            logger.LogInformation($"[WorldMapController] Preloading chunks in radius {radius} around ({centerX}, {centerZ})");
            
            var tasks = new List<Task>();
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= radius)
                    {
                        tasks.Add(GetChunkAsync(x, z, cancellationToken));
                    }
                }
            }
            
            await Task.WhenAll(tasks);
            logger.LogInformation($"[WorldMapController] Preloaded {tasks.Count} chunks");
        }
        
        /// <summary>
        /// Gets the world map control profile
        /// </summary>
        public WorldMapControlProfile GetControlProfile()
        {
            return controlProfile;
        }
        
        /// <summary>
        /// Validates that a client profile matches the server profile
        /// </summary>
        public bool ValidateClientProfile(string clientProfileHash)
        {
            return !string.IsNullOrEmpty(controlProfile.ProfileHash) && 
                   controlProfile.ProfileHash == clientProfileHash;
        }
        
        /// <summary>
        /// Saves the control profile to disk
        /// </summary>
        public void SaveControlProfile(string path)
        {
            try
            {
                WorldMapControlProfileUtility.Save(controlProfile, path);
                logger.LogInformation($"[WorldMapController] Saved control profile to {path}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[WorldMapController] Failed to save control profile to {path}");
            }
        }
        
        /// <summary>
        /// Gets statistics about loaded chunks
        /// </summary>
        public WorldMapStatistics GetStatistics()
        {
            return new WorldMapStatistics
            {
                LoadedChunksCount = loadedChunks.Count,
                GeneratingChunksCount = chunkGenerationTasks.Count,
                QueuedRequestsCount = chunkRequestQueue.Count,
                MemoryUsageMB = EstimateMemoryUsage(),
                ControlProfileHash = controlProfile.ProfileHash,
                ControlProfileSource = controlProfile.SourceConfig
            };
        }
        
        /// <summary>
        /// Estimates memory usage of loaded chunks
        /// </summary>
        private double EstimateMemoryUsage()
        {
            const double bytesPerBlock = 2; // Approximate bytes per block
            const double blocksPerChunk = 16 * 16 * 256; // Chunk dimensions
            
            return (loadedChunks.Count * blocksPerChunk * bytesPerBlock) / (1024.0 * 1024.0);
        }
        
        /// <summary>
        /// Cleans up old chunks that haven't been accessed recently
        /// </summary>
        private void CleanupOldChunks(object state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Remove chunks not accessed in 10 minutes
                var chunksToRemove = new List<Vector2Int>();
                
                foreach (var kvp in chunkAccessTimes)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        chunksToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var chunkPos in chunksToRemove)
                {
                    if (loadedChunks.TryRemove(chunkPos, out _))
                    {
                        chunkAccessTimes.Remove(chunkPos);
                        logger.LogDebug($"[WorldMapController] Cleaned up old chunk at {chunkPos}");
                    }
                }
                
                if (chunksToRemove.Count > 0)
                {
                    logger.LogInformation($"[WorldMapController] Cleaned up {chunksToRemove.Count} old chunks");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WorldMapController] Error during chunk cleanup");
            }
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            chunkCleanupTimer?.Dispose();
            
            loadedChunks.Clear();
            chunkGenerationTasks.Clear();
            chunkRequestQueue.Clear();
            chunkAccessTimes.Clear();
            
            logger.LogInformation("[WorldMapController] Disposed");
        }
    }
    
    /// <summary>
    /// Vector2Int structure for chunk coordinates
    /// </summary>
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int X { get; }
        public int Y { get; }
        
        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public bool Equals(Vector2Int other)
        {
            return X == other.X && Y == other.Y;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Vector2Int other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
        
        public static bool operator ==(Vector2Int left, Vector2Int right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(Vector2Int left, Vector2Int right)
        {
            return !left.Equals(right);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
    
    /// <summary>
    /// Chunk request structure
    /// </summary>
    internal struct ChunkRequest
    {
        public Vector2Int Position;
        public int Priority;
        public DateTime RequestTime;
        public string RequesterId;
    }
    
    /// <summary>
    /// World map statistics
    /// </summary>
    public class WorldMapStatistics
    {
        public int LoadedChunksCount { get; set; }
        public int GeneratingChunksCount { get; set; }
        public int QueuedRequestsCount { get; set; }
        public double MemoryUsageMB { get; set; }
        public string ControlProfileHash { get; set; } = string.Empty;
        public string ControlProfileSource { get; set; } = string.Empty;
    }
}using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.World
{
    /// <summary>
    /// Server-side world map controller that manages terrain generation and chunk distribution
    /// Integrates with EnhancedTerrainGenerationPipeline and WorldMapControlProfile
    /// </summary>
    public class WorldMapController
    {
        private readonly ILogger<WorldMapController> logger;
        private readonly WorldSettings worldSettings;
        private readonly WorldGenerationConfig generationConfig;
        private readonly WorldMapControlProfile controlProfile;
        private readonly EnhancedTerrainGenerationPipeline terrainPipeline;
        
        // Chunk management
        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, Task<ChunkData>> chunkGenerationTasks = new();
        private readonly ConcurrentQueue<ChunkRequest> chunkRequestQueue = new();
        
        // Performance tracking
        private readonly Dictionary<Vector2Int, DateTime> chunkAccessTimes = new();
        private readonly Timer chunkCleanupTimer;
        
        // Cancellation
        private readonly CancellationTokenSource cancellationTokenSource = new();
        
        public WorldMapController(
            ILogger<WorldMapController> logger,
            WorldSettings worldSettings,
            WorldGenerationConfig generationConfig)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));
            this.generationConfig = generationConfig ?? throw new ArgumentNullException(nameof(generationConfig));
            
            // Create control profile from generation config
            controlProfile = WorldMapControlProfile.Create(generationConfig, worldSettings);
            
            // Initialize terrain generation pipeline
            terrainPipeline = new EnhancedTerrainGenerationPipeline(generationConfig, logger);
            
            // Start cleanup timer
            chunkCleanupTimer = new Timer(CleanupOldChunks, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            logger.LogInformation($"[WorldMapController] Initialized with profile: {controlProfile.SourceConfig} (Hash: {controlProfile.ProfileHash})");
        }
        
        /// <summary>
        /// Gets a chunk, generating it if necessary
        /// </summary>
        public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
        {
            var chunkPos = new Vector2Int(chunkX, chunkZ);
            
            // Update access time
            chunkAccessTimes[chunkPos] = DateTime.UtcNow;
            
            // Return existing chunk if already loaded
            if (loadedChunks.TryGetValue(chunkPos, out var existingChunk))
            {
                return existingChunk;
            }
            
            // Check if chunk is already being generated
            if (chunkGenerationTasks.TryGetValue(chunkPos, out var existingTask))
            {
                return await existingTask;
            }
            
            // Generate new chunk
            var generationTask = GenerateChunkAsync(chunkPos, cancellationToken);
            chunkGenerationTasks[chunkPos] = generationTask;
            
            try
            {
                var chunk = await generationTask;
                loadedChunks[chunkPos] = chunk;
                return chunk;
            }
            finally
            {
                chunkGenerationTasks.TryRemove(chunkPos, out _);
            }
        }
        
        /// <summary>
        /// Generates a chunk using the enhanced terrain pipeline
        /// </summary>
        private async Task<ChunkData> GenerateChunkAsync(Vector2Int chunkPos, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug($"[WorldMapController] Generating chunk at {chunkPos}");
                
                // Generate chunk using enhanced pipeline
                var chunkData = await terrainPipeline.GenerateChunkAsync(chunkPos.X, chunkPos.Y);
                
                if (chunkData == null)
                {
                    logger.LogError($"[WorldMapController] Failed to generate chunk at {chunkPos}");
                    return CreateEmptyChunk(chunkPos.X, chunkPos.Y);
                }
                
                // Apply control profile settings
                ApplyControlProfileSettings(chunkData);
                
                logger.LogDebug($"[WorldMapController] Successfully generated chunk at {chunkPos}");
                return chunkData;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[WorldMapController] Error generating chunk at {chunkPos}");
                return CreateEmptyChunk(chunkPos.X, chunkPos.Y);
            }
        }
        
        /// <summary>
        /// Applies control profile settings to generated chunk data
        /// </summary>
        private void ApplyControlProfileSettings(ChunkData chunkData)
        {
            // Apply water level
            if (chunkData.HeightMap != null)
            {
                var size = chunkData.Size;
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        // Ensure water level consistency
                        if (chunkData.HeightMap[x, z] < controlProfile.GlobalWaterLevel)
                        {
                            chunkData.HeightMap[x, z] = controlProfile.GlobalWaterLevel;
                        }
                    }
                }
            }
            
            // Apply feature overrides based on control profile
            if (!controlProfile.EnableRivers)
            {
                chunkData.RiverMap = null;
            }
            
            if (!controlProfile.EnableLakes)
            {
                chunkData.LakeMap = null;
            }
            
            if (!controlProfile.EnableCaves)
            {
                chunkData.CaveMap = null;
            }
        }
        
        /// <summary>
        /// Creates an empty chunk as fallback
        /// </summary>
        private ChunkData CreateEmptyChunk(int chunkX, int chunkZ)
        {
            var size = generationConfig.ChunkSize;
            var chunkData = new ChunkData
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Size = size,
                HeightMap = new float[size, size],
                CaveMap = new bool[size, size, size],
                RiverMap = new float[size, size],
                LakeMap = new float[size, size]
            };
            
            // Fill with basic terrain
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    chunkData.HeightMap[x, z] = controlProfile.GlobalWaterLevel;
                }
            }
            
            return chunkData;
        }
        
        /// <summary>
        /// Gets multiple chunks for a player's view area
        /// </summary>
        public async Task<List<ChunkData>> GetChunksAroundAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
        {
            var chunks = new List<ChunkData>();
            var tasks = new List<Task<ChunkData>>();
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= radius)
                    {
                        tasks.Add(GetChunkAsync(x, z, cancellationToken));
                    }
                }
            }
            
            var chunkResults = await Task.WhenAll(tasks);
            chunks.AddRange(chunkResults.Where(c => c != null));
            
            return chunks;
        }
        
        /// <summary>
        /// Preloads chunks in a radius around the center
        /// </summary>
        public async Task PreloadChunksAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
        {
            logger.LogInformation($"[WorldMapController] Preloading chunks in radius {radius} around ({centerX}, {centerZ})");
            
            var tasks = new List<Task>();
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    var distance = Math.Sqrt((x - centerX) * (x - centerX) + (z - centerZ) * (z - centerZ));
                    if (distance <= radius)
                    {
                        tasks.Add(GetChunkAsync(x, z, cancellationToken));
                    }
                }
            }
            
            await Task.WhenAll(tasks);
            logger.LogInformation($"[WorldMapController] Preloaded {tasks.Count} chunks");
        }
        
        /// <summary>
        /// Gets the world map control profile
        /// </summary>
        public WorldMapControlProfile GetControlProfile()
        {
            return controlProfile;
        }
        
        /// <summary>
        /// Validates that a client profile matches the server profile
        /// </summary>
        public bool ValidateClientProfile(string clientProfileHash)
        {
            return !string.IsNullOrEmpty(controlProfile.ProfileHash) && 
                   controlProfile.ProfileHash == clientProfileHash;
        }
        
        /// <summary>
        /// Saves the control profile to disk
        /// </summary>
        public void SaveControlProfile(string path)
        {
            try
            {
                WorldMapControlProfileUtility.Save(controlProfile, path);
                logger.LogInformation($"[WorldMapController] Saved control profile to {path}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[WorldMapController] Failed to save control profile to {path}");
            }
        }
        
        /// <summary>
        /// Gets statistics about loaded chunks
        /// </summary>
        public WorldMapStatistics GetStatistics()
        {
            return new WorldMapStatistics
            {
                LoadedChunksCount = loadedChunks.Count,
                GeneratingChunksCount = chunkGenerationTasks.Count,
                QueuedRequestsCount = chunkRequestQueue.Count,
                MemoryUsageMB = EstimateMemoryUsage(),
                ControlProfileHash = controlProfile.ProfileHash,
                ControlProfileSource = controlProfile.SourceConfig
            };
        }
        
        /// <summary>
        /// Estimates memory usage of loaded chunks
        /// </summary>
        private double EstimateMemoryUsage()
        {
            const double bytesPerBlock = 2; // Approximate bytes per block
            const double blocksPerChunk = 16 * 16 * 256; // Chunk dimensions
            
            return (loadedChunks.Count * blocksPerChunk * bytesPerBlock) / (1024.0 * 1024.0);
        }
        
        /// <summary>
        /// Cleans up old chunks that haven't been accessed recently
        /// </summary>
        private void CleanupOldChunks(object state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Remove chunks not accessed in 10 minutes
                var chunksToRemove = new List<Vector2Int>();
                
                foreach (var kvp in chunkAccessTimes)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        chunksToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var chunkPos in chunksToRemove)
                {
                    if (loadedChunks.TryRemove(chunkPos, out _))
                    {
                        chunkAccessTimes.Remove(chunkPos);
                        logger.LogDebug($"[WorldMapController] Cleaned up old chunk at {chunkPos}");
                    }
                }
                
                if (chunksToRemove.Count > 0)
                {
                    logger.LogInformation($"[WorldMapController] Cleaned up {chunksToRemove.Count} old chunks");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WorldMapController] Error during chunk cleanup");
            }
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            chunkCleanupTimer?.Dispose();
            
            loadedChunks.Clear();
            chunkGenerationTasks.Clear();
            chunkRequestQueue.Clear();
            chunkAccessTimes.Clear();
            
            logger.LogInformation("[WorldMapController] Disposed");
        }
    }
    
    /// <summary>
    /// Vector2Int structure for chunk coordinates
    /// </summary>
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int X { get; }
        public int Y { get; }
        
        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public bool Equals(Vector2Int other)
        {
            return X == other.X && Y == other.Y;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Vector2Int other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
        
        public static bool operator ==(Vector2Int left, Vector2Int right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(Vector2Int left, Vector2Int right)
        {
            return !left.Equals(right);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
    
    /// <summary>
    /// Chunk request structure
    /// </summary>
    internal struct ChunkRequest
    {
        public Vector2Int Position;
        public int Priority;
        public DateTime RequestTime;
        public string RequesterId;
    }
    
    /// <summary>
    /// World map statistics
    /// </summary>
    public class WorldMapStatistics
    {
        public int LoadedChunksCount { get; set; }
        public int GeneratingChunksCount { get; set; }
        public int QueuedRequestsCount { get; set; }
        public double MemoryUsageMB { get; set; }
        public string ControlProfileHash { get; set; } = string.Empty;
        public string ControlProfileSource { get; set; } = string.Empty;
    }
}
