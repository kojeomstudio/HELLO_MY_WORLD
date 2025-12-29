using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameServerApp.World.Generation;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World
{
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        public int X { get; }
        public int Y { get; }

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vector2Int other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// Centralized world map controller responsible for generating and caching chunks,
    /// persisting the map-control profile, and coordinating hydrology-aware generation.
    /// </summary>
    public sealed class WorldMapController : IDisposable
    {
        private readonly ILogger<WorldMapController> logger;
        private readonly WorldSettings worldSettings;
        private readonly WorldGenerationConfig generationConfig;
        private readonly EnhancedTerrainGenerationPipeline pipeline;
        private readonly WorldMapControlProfile controlProfile;

        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, Task<ChunkData>> generationTasks = new();
        private readonly ConcurrentDictionary<Vector2Int, DateTime> accessTimes = new();
        private readonly Timer cleanupTimer;

        public WorldMapControlProfile ControlProfile => controlProfile;

        public WorldMapController(
            ILogger<WorldMapController> logger,
            WorldSettings worldSettings,
            WorldGenerationConfig generationConfig)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));
            this.generationConfig = generationConfig ?? throw new ArgumentNullException(nameof(generationConfig));

            pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings, logger);
            controlProfile = WorldMapControlProfile.Create(generationConfig, worldSettings);
            WorldMapControlProfileUtility.Save(controlProfile, generationConfig.MapControlProfilePath);

            var cleanupInterval = TimeSpan.FromMinutes(Math.Max(5, worldSettings.ChunkUnloadTimeoutMinutes));
            cleanupTimer = new Timer(_ => CleanupOldChunks(), null, cleanupInterval, cleanupInterval);

            this.logger.LogInformation(
                "[WorldMapController] Initialized. Profile hash: {Hash} (config: {ConfigPath})",
                controlProfile.ProfileHash,
                generationConfig.SourcePath);
        }

        public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
        {
            var pos = new Vector2Int(chunkX, chunkZ);
            accessTimes[pos] = DateTime.UtcNow;

            if (loadedChunks.TryGetValue(pos, out var cached))
            {
                return cached;
            }

            if (generationTasks.TryGetValue(pos, out var inflight))
            {
                return await inflight.ConfigureAwait(false);
            }

            var task = GenerateChunkAsync(pos, cancellationToken);
            if (!generationTasks.TryAdd(pos, task))
            {
                // Another task raced us; reuse it
                if (generationTasks.TryGetValue(pos, out var existing))
                {
                    return await existing.ConfigureAwait(false);
                }
            }

            try
            {
                var chunk = await task.ConfigureAwait(false);
                loadedChunks[pos] = chunk;
                return chunk;
            }
            finally
            {
                generationTasks.TryRemove(pos, out _);
            }
        }

        public async Task PreloadAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    tasks.Add(GetChunkAsync(x, z, cancellationToken));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            logger.LogInformation("[WorldMapController] Preloaded {Count} chunks around ({X}, {Z})", tasks.Count, centerX, centerZ);
        }

        public void Dispose()
        {
            cleanupTimer.Dispose();
            generationTasks.Clear();
            loadedChunks.Clear();
            accessTimes.Clear();
            logger.LogInformation("[WorldMapController] Disposed");
        }

        private async Task<ChunkData> GenerateChunkAsync(Vector2Int chunkPos, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug("[WorldMapController] Generating chunk {Pos}", chunkPos);
                var chunk = await pipeline.GenerateChunkAsync(chunkPos.X, chunkPos.Y, cancellationToken).ConfigureAwait(false);
                ApplyControlProfile(chunk);
                logger.LogDebug("[WorldMapController] Generated chunk {Pos}", chunkPos);
                return chunk;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WorldMapController] Failed to generate chunk {Pos}", chunkPos);
                return new ChunkData(chunkPos.X, chunkPos.Y);
            }
        }

        private void ApplyControlProfile(ChunkData chunk)
        {
            // Currently the profile is used for bookkeeping and versioning.
            // This hook keeps the shape for future hydration (biome tagging, debug payloads).
            accessTimes[new Vector2Int(chunk.ChunkX, chunk.ChunkZ)] = DateTime.UtcNow;
        }

        private void CleanupOldChunks()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expired = new List<Vector2Int>();
                var timeout = TimeSpan.FromMinutes(Math.Max(1, worldSettings.ChunkUnloadTimeoutMinutes));

                foreach (var kvp in accessTimes)
                {
                    if (now - kvp.Value > timeout)
                    {
                        expired.Add(kvp.Key);
                    }
                }

                foreach (var pos in expired)
                {
                    loadedChunks.TryRemove(pos, out _);
                    accessTimes.TryRemove(pos, out _);
                    logger.LogDebug("[WorldMapController] Unloaded idle chunk {Pos}", pos);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WorldMapController] Chunk cleanup failed");
            }
        }
    }
}
