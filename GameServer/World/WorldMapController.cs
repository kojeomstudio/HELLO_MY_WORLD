using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerApp;
using GameServerApp.World.Generation;
using Microsoft.Extensions.Logging;
using GameCommon.World;
using SharedProtocol.EnhancedMinecraft;

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
        private const string PipelineVersion = SharedFeatureCatalog.HydrologySignature;
        private readonly ILogger<WorldMapController> logger;
        private readonly WorldSettings worldSettings;
        private WorldGenerationConfig generationConfig;
        private EnhancedTerrainGenerationPipeline pipeline;
        private WorldMapControlProfile controlProfile;
        private readonly string profilePath;
        private readonly string worldConfigPath;
        private DateTime profileWriteTime;
        private DateTime worldConfigWriteTime;
        private string generationSignature;
        private string worldConfigHash;
        private string profileFileHash;
        private int maxLoadedChunks;
        private int queuePressureFactor;
        private int queueLimit;
        private double queueSlackRatio;
        private double queueBurstSlackMultiplier;
        private double queueLoadSheddingThreshold;
        private double queueEmergencyBrakeThreshold;
        private double queueLoadEmaBlend;
        private double queueEmergencyReleaseRatio;
        private double queueTrendBoostWeight;
        private double queueShockAbsorberWeight;
        private double queueAlluvialRelayWeight;
        private int queueOverloadDrainFactor;
        private int queueBackoffDelayMs;
        private double queueLoadEma;
        private bool queueEmergencyBrakeLatched;

        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, Task<ChunkData>> generationTasks = new();
        private readonly ConcurrentDictionary<Vector2Int, DateTime> accessTimes = new();
        private readonly Timer cleanupTimer;
        private readonly object reloadLock = new();

        public WorldMapControlProfile ControlProfile => controlProfile;
        public string GenerationSignature => generationSignature;

        public WorldMapController(
            ILogger<WorldMapController> logger,
            WorldSettings worldSettings,
            WorldGenerationConfig generationConfig)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));
            this.generationConfig = generationConfig ?? throw new ArgumentNullException(nameof(generationConfig));

            profilePath = string.IsNullOrWhiteSpace(this.generationConfig.MapControlProfilePath)
                ? "config/world_map_control_profile.json"
                : this.generationConfig.MapControlProfilePath;
            worldConfigPath = string.IsNullOrWhiteSpace(this.generationConfig.SourcePath)
                ? "config/world.json"
                : this.generationConfig.SourcePath;

            pipeline = new EnhancedTerrainGenerationPipeline(this.generationConfig, worldSettings, logger);
            controlProfile = WorldMapControlProfileUtility.LoadOrCreate(this.generationConfig, worldSettings);
            WorldMapControlProfileUtility.Save(controlProfile, profilePath);
            EnsureProfileBaseline("initialization");
            profileWriteTime = GetWriteTime(profilePath);
            worldConfigWriteTime = GetWriteTime(worldConfigPath);
            worldConfigHash = ComputeFileHash(worldConfigPath);
            profileFileHash = ComputeFileHash(profilePath);
            maxLoadedChunks = ComputeLoadedChunkBudget();
            RecomputeQueuePolicy();
            queueLoadEma = 0.0;
            queueEmergencyBrakeLatched = false;
            generationSignature = ComputeGenerationSignature();

            var cleanupInterval = TimeSpan.FromMinutes(Math.Max(5, worldSettings.ChunkUnloadTimeoutMinutes));
            cleanupTimer = new Timer(_ => CleanupOldChunks(), null, cleanupInterval, cleanupInterval);

            this.logger.LogInformation(
                "[WorldMapController] Initialized. Profile hash: {Hash} (config: {ConfigPath}, signature: {Signature})",
                controlProfile.ProfileHash,
                generationConfig.SourcePath,
                generationSignature);
        }

        public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
        {
            MaybeReloadProfile();
            var pos = new Vector2Int(chunkX, chunkZ);
            accessTimes[pos] = DateTime.UtcNow;
            var queueState = GetAdaptiveQueueState();
            int loadSheddingLimit = Math.Max(64, (int)Math.Floor(queueState.QueueLimit * Math.Max(0.5, queueState.LoadSheddingThreshold)));

            if (loadedChunks.TryGetValue(pos, out var cached))
            {
                return cached;
            }

            if (generationTasks.TryGetValue(pos, out var inflight))
            {
                return await inflight.ConfigureAwait(false);
            }

            if (generationTasks.Count >= loadSheddingLimit)
            {
                TrimCompletedGenerationTasks();
                logger.LogDebug(
                    "[WorldMapController] Load-shedding gate for {Pos} (inflight={Inflight}, sheddingLimit={LoadSheddingLimit}, threshold={Threshold:F2})",
                    pos,
                    generationTasks.Count,
                    loadSheddingLimit,
                    queueState.LoadSheddingThreshold);
                await Task.Delay(Math.Max(1, queueBackoffDelayMs * queueState.PressureFactor), cancellationToken).ConfigureAwait(false);
                if (generationTasks.TryGetValue(pos, out var shedInflight))
                {
                    return await shedInflight.ConfigureAwait(false);
                }
            }

            if (queueState.EmergencyBrake)
            {
                TrimCompletedGenerationTasks();
                TrimCompletedGenerationTasks();
                await Task.Delay(Math.Max(1, queueBackoffDelayMs * (queueState.PressureFactor + 1)), cancellationToken).ConfigureAwait(false);
                if (generationTasks.TryGetValue(pos, out var emergencyInflight))
                {
                    return await emergencyInflight.ConfigureAwait(false);
                }
            }

            if (generationTasks.Count >= Math.Max(64, queueState.QueueLimit))
            {
                TrimCompletedGenerationTasks();
                logger.LogDebug(
                    "[WorldMapController] Queue pressure gate for {Pos} (inflight={Inflight}, queueLimit={QueueLimit}, pressureFactor={PressureFactor})",
                    pos,
                    generationTasks.Count,
                    queueState.QueueLimit,
                    queueState.PressureFactor);
                await Task.Delay(Math.Max(1, queueBackoffDelayMs * queueState.PressureFactor), cancellationToken).ConfigureAwait(false);
                if (generationTasks.TryGetValue(pos, out var delayedInflight))
                {
                    return await delayedInflight.ConfigureAwait(false);
                }
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
                EnforceLoadedChunkBudget();
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
            var prioritizedChunks = WorldMapQueuePolicy.EnumerateByDistance(centerX, centerZ, radius);
            foreach (var chunk in prioritizedChunks)
            {
                tasks.Add(GetChunkAsync(chunk.X, chunk.Z, cancellationToken));
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
                lock (reloadLock)
                {
                    ResetPipeline();
                }
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

        private void MaybeReloadProfile()
        {
            bool reloadNeeded = false;

            lock (reloadLock)
            {
                DateTime currentWorldWrite = GetWriteTime(worldConfigPath);
                if (currentWorldWrite > worldConfigWriteTime)
                {
                    generationConfig = WorldGenerationConfig.Load(worldConfigPath);
                    worldConfigWriteTime = currentWorldWrite;
                    worldConfigHash = ComputeFileHash(worldConfigPath);
                    reloadNeeded = true;
                }

                DateTime currentProfileWrite = GetWriteTime(profilePath);
                if (currentProfileWrite > profileWriteTime)
                {
                    var loaded = WorldMapControlProfileUtility.Load(profilePath);
                    if (loaded != null)
                    {
                        bool hashChanged = !string.Equals(loaded.ProfileHash, controlProfile.ProfileHash, StringComparison.OrdinalIgnoreCase);
                        bool signatureChanged = !string.Equals(loaded.HydrologySignature, controlProfile.HydrologySignature, StringComparison.OrdinalIgnoreCase);
                        bool versionChanged = loaded.Version != controlProfile.Version;
                        if (hashChanged || signatureChanged || versionChanged)
                        {
                            controlProfile = loaded;
                            reloadNeeded = true;
                        }
                    }

                    profileWriteTime = currentProfileWrite;
                    profileFileHash = ComputeFileHash(profilePath);
                }

                if (EnsureProfileBaseline("reload"))
                {
                    profileWriteTime = GetWriteTime(profilePath);
                    profileFileHash = ComputeFileHash(profilePath);
                    reloadNeeded = true;
                }

                if (reloadNeeded)
                {
                    ResetPipeline();
                    logger.LogInformation(
                        "[WorldMapController] Reloaded map-control profile hash={Hash} (config updated: {ConfigPath}, signature: {Signature})",
                        controlProfile.ProfileHash,
                        worldConfigPath,
                        generationSignature);
                }
            }
        }

        private bool EnsureProfileBaseline(string reason)
        {
            bool changed = false;
            int requiredVersion = Math.Max(1, SharedFeatureCatalog.MapControlProfileVersion);
            string expectedSignature = SharedFeatureCatalog.HydrologySignature;

            if (!string.Equals(controlProfile.HydrologySignature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                controlProfile.HydrologySignature = expectedSignature;
                changed = true;
            }

            if (controlProfile.Version < requiredVersion)
            {
                controlProfile.Version = requiredVersion;
                changed = true;
            }

            string expectedHash = WorldMapControlProfileUtility.ComputeHash(controlProfile);
            if (!string.Equals(controlProfile.ProfileHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                controlProfile.ProfileHash = expectedHash;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            controlProfile.GeneratedAtUtc = DateTime.UtcNow;
            WorldMapControlProfileUtility.Save(controlProfile, profilePath);
            logger.LogWarning(
                "[WorldMapController] Auto-healed map profile baseline during {Reason} (version={Version}, signature={Signature}, hash={Hash})",
                reason,
                controlProfile.Version,
                controlProfile.HydrologySignature,
                controlProfile.ProfileHash);
            return true;
        }

        private static DateTime GetWriteTime(string path)
        {
            try
            {
                return File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string ComputeFileHash(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return string.Empty;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var sha = System.Security.Cryptography.SHA256.Create();
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ResetPipeline()
        {
            pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings, logger);
            generationSignature = ComputeGenerationSignature();
            maxLoadedChunks = ComputeLoadedChunkBudget();
            RecomputeQueuePolicy();
            queueLoadEma = 0.0;
            queueEmergencyBrakeLatched = false;
            loadedChunks.Clear();
            generationTasks.Clear();
            accessTimes.Clear();
        }

        private int ComputeLoadedChunkBudget()
        {
            int renderDistance = controlProfile?.RenderDistance > 0
                ? controlProfile.RenderDistance
                : Math.Max(1, generationConfig.RenderDistance);
            int simulationDistance = controlProfile?.SimulationDistance > 0
                ? controlProfile.SimulationDistance
                : Math.Max(1, generationConfig.SimulationDistance);
            int renderWindow = (renderDistance * 2 + 1) * (renderDistance * 2 + 1);
            int simulationWindow = (simulationDistance * 2 + 1) * (simulationDistance * 2 + 1);
            int baseline = Math.Max(renderWindow, simulationWindow);
            int withSlack = baseline + Math.Max(64, baseline / 4);
            return Math.Clamp(withSlack, 128, 8192);
        }

        private void RecomputeQueuePolicy()
        {
            int renderWindow = Math.Max(1, controlProfile.RenderDistance * 2 + 1);
            int simulationWindow = Math.Max(1, controlProfile.SimulationDistance * 2 + 1);
            int profileBudget = Math.Max(renderWindow * renderWindow, simulationWindow * simulationWindow);
            queueSlackRatio = Math.Clamp(
                generationConfig.MapControlProfileVersion >= 86 ? 4.16 :
                generationConfig.MapControlProfileVersion >= 84 ? 4.08 :
                generationConfig.MapControlProfileVersion >= 82 ? 4.02 :
                generationConfig.MapControlProfileVersion >= 81 ? 3.96 :
                generationConfig.MapControlProfileVersion >= 80 ? 3.9 :
                generationConfig.MapControlProfileVersion >= 79 ? 3.87 :
                generationConfig.MapControlProfileVersion >= 78 ? 3.84 :
                generationConfig.MapControlProfileVersion >= 77 ? 3.78 :
                generationConfig.MapControlProfileVersion >= 76 ? 3.72 :
                generationConfig.MapControlProfileVersion >= 75 ? 3.65 :
                generationConfig.MapControlProfileVersion >= 57 ? 3.45 :
                generationConfig.MapControlProfileVersion >= 56 ? 3.35 :
                generationConfig.MapControlProfileVersion >= 45 ? 3.2 :
                generationConfig.MapControlProfileVersion >= 40 ? 3.0 :
                generationConfig.MapControlProfileVersion >= 34 ? 2.8 : 2.4,
                1.1,
                6.0);
            queueBurstSlackMultiplier = Math.Clamp(
                generationConfig.MapControlProfileVersion >= 86 ? 1.56 :
                generationConfig.MapControlProfileVersion >= 84 ? 1.53 :
                generationConfig.MapControlProfileVersion >= 82 ? 1.5 :
                generationConfig.MapControlProfileVersion >= 81 ? 1.47 :
                generationConfig.MapControlProfileVersion >= 80 ? 1.45 :
                generationConfig.MapControlProfileVersion >= 79 ? 1.43 :
                generationConfig.MapControlProfileVersion >= 78 ? 1.42 :
                generationConfig.MapControlProfileVersion >= 77 ? 1.38 :
                generationConfig.MapControlProfileVersion >= 76 ? 1.34 :
                generationConfig.MapControlProfileVersion >= 75 ? 1.31 :
                generationConfig.MapControlProfileVersion >= 57 ? 1.28 :
                generationConfig.MapControlProfileVersion >= 56 ? 1.26 :
                generationConfig.MapControlProfileVersion >= 45 ? 1.24 :
                generationConfig.MapControlProfileVersion >= 40 ? 1.2 :
                generationConfig.MapControlProfileVersion >= 35 ? 1.15 : 1.0,
                1.0,
                3.0);
            queueOverloadDrainFactor = generationConfig.MapControlProfileVersion >= 86 ? 16 :
                generationConfig.MapControlProfileVersion >= 84 ? 15 :
                generationConfig.MapControlProfileVersion >= 82 ? 14 :
                generationConfig.MapControlProfileVersion >= 81 ? 13 :
                generationConfig.MapControlProfileVersion >= 80 ? 13 :
                generationConfig.MapControlProfileVersion >= 79 ? 12 :
                generationConfig.MapControlProfileVersion >= 78 ? 12 :
                generationConfig.MapControlProfileVersion >= 77 ? 11 :
                generationConfig.MapControlProfileVersion >= 76 ? 10 :
                generationConfig.MapControlProfileVersion >= 75 ? 9 :
                generationConfig.MapControlProfileVersion >= 57 ? 8 :
                generationConfig.MapControlProfileVersion >= 56 ? 7 :
                generationConfig.MapControlProfileVersion >= 45 ? 6 :
                generationConfig.MapControlProfileVersion >= 40 ? 5 :
                generationConfig.MapControlProfileVersion >= 34 ? 4 : 3;
            queueBackoffDelayMs = generationConfig.MapControlProfileVersion >= 84 ? 1 :
                generationConfig.MapControlProfileVersion >= 82 ? 1 :
                generationConfig.MapControlProfileVersion >= 81 ? 1 :
                generationConfig.MapControlProfileVersion >= 80 ? 1 :
                generationConfig.MapControlProfileVersion >= 79 ? 1 :
                generationConfig.MapControlProfileVersion >= 78 ? 1 :
                generationConfig.MapControlProfileVersion >= 77 ? 1 :
                generationConfig.MapControlProfileVersion >= 76 ? 1 :
                generationConfig.MapControlProfileVersion >= 75 ? 2 :
                generationConfig.MapControlProfileVersion >= 57 ? 3 :
                generationConfig.MapControlProfileVersion >= 56 ? 4 :
                generationConfig.MapControlProfileVersion >= 45 ? 5 :
                generationConfig.MapControlProfileVersion >= 40 ? 6 :
                generationConfig.MapControlProfileVersion >= 34 ? 8 : 6;
            queueLoadSheddingThreshold = Math.Clamp(
                generationConfig.MapControlProfileVersion >= 86 ? 0.66 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.67 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.68 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.69 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.70 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.71 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.72 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.74 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.76 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.78 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.8 :
                generationConfig.MapControlProfileVersion >= 56 ? 0.82 :
                generationConfig.MapControlProfileVersion >= 45 ? 0.84 :
                generationConfig.MapControlProfileVersion >= 40 ? 0.86 :
                generationConfig.MapControlProfileVersion >= 34 ? 0.88 : 0.92,
                0.5,
                0.98);
            queueEmergencyBrakeThreshold = Math.Clamp(
                generationConfig.MapControlProfileVersion >= 86 ? 0.82 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.83 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.84 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.85 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.86 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.87 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.88 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.90 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.92 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.94 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.98 :
                generationConfig.MapControlProfileVersion >= 56 ? 1.0 :
                generationConfig.MapControlProfileVersion >= 45 ? 1.02 :
                generationConfig.MapControlProfileVersion >= 40 ? 1.04 :
                generationConfig.MapControlProfileVersion >= 36 ? 1.08 : 1.2,
                0.75,
                4.0);
            queueLoadEmaBlend = WorldMapQueuePolicy.ClampEmaBlend(
                generationConfig.MapControlProfileVersion >= 86 ? 0.44 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.43 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.42 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.41 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.40 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.39 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.38 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.36 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.34 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.32 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.3 :
                generationConfig.MapControlProfileVersion >= 56 ? 0.28 :
                generationConfig.MapControlProfileVersion >= 45 ? 0.26 :
                generationConfig.MapControlProfileVersion >= 43 ? 0.24 : 0.18,
                0.18);
            queueEmergencyReleaseRatio = WorldMapQueuePolicy.ClampEmergencyReleaseRatio(
                generationConfig.MapControlProfileVersion >= 86 ? 0.62 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.63 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.64 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.65 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.66 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.67 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.68 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.70 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.72 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.74 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.76 :
                generationConfig.MapControlProfileVersion >= 56 ? 0.78 :
                generationConfig.MapControlProfileVersion >= 45 ? 0.8 :
                generationConfig.MapControlProfileVersion >= 43 ? 0.82 : 0.84,
                0.84);
            queueTrendBoostWeight = WorldMapQueuePolicy.ClampTrendBoostWeight(
                generationConfig.MapControlProfileVersion >= 86 ? 0.50 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.49 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.48 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.47 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.46 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.45 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.44 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.42 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.40 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.38 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.34 :
                generationConfig.MapControlProfileVersion >= 56 ? 0.32 :
                generationConfig.MapControlProfileVersion >= 45 ? 0.3 :
                generationConfig.MapControlProfileVersion >= 44 ? 0.26 :
                generationConfig.MapControlProfileVersion >= 43 ? 0.22 : 0.18,
                0.2);
            queueShockAbsorberWeight = WorldMapQueuePolicy.ClampShockAbsorberWeight(
                generationConfig.MapControlProfileVersion >= 86 ? 0.50 :
                generationConfig.MapControlProfileVersion >= 84 ? 0.48 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.47 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.46 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.45 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.44 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.42 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.40 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.38 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.36 :
                generationConfig.MapControlProfileVersion >= 57 ? 0.32 :
                generationConfig.MapControlProfileVersion >= 56 ? 0.3 :
                generationConfig.MapControlProfileVersion >= 45 ? 0.28 :
                generationConfig.MapControlProfileVersion >= 44 ? 0.24 :
                generationConfig.MapControlProfileVersion >= 43 ? 0.2 : 0.16,
                0.24);
            queueAlluvialRelayWeight = Math.Clamp(
                generationConfig.MapControlProfileVersion >= 86 ? 1.06 :
                generationConfig.MapControlProfileVersion >= 84 ? 1.02 :
                generationConfig.MapControlProfileVersion >= 82 ? 0.98 :
                generationConfig.MapControlProfileVersion >= 81 ? 0.95 :
                generationConfig.MapControlProfileVersion >= 80 ? 0.92 :
                generationConfig.MapControlProfileVersion >= 79 ? 0.88 :
                generationConfig.MapControlProfileVersion >= 78 ? 0.84 :
                generationConfig.MapControlProfileVersion >= 77 ? 0.8 :
                generationConfig.MapControlProfileVersion >= 76 ? 0.76 :
                generationConfig.MapControlProfileVersion >= 75 ? 0.72 :
                0.68,
                0.0,
                1.5);
            queueLimit = Math.Clamp((int)Math.Ceiling(Math.Max(128, Math.Max(maxLoadedChunks, profileBudget) * queueSlackRatio)), 128, 16384);

            double ratio = queueLimit / Math.Max(1.0, maxLoadedChunks);
            queuePressureFactor = ratio >= 3.0 ? 3 : (ratio >= 2.0 ? 2 : 1);
            queuePressureFactor = Math.Clamp(queuePressureFactor, 1, 6);

            if (worldSettings != null)
            {
                int worldQueueHint = Math.Max(1, worldSettings.ChunkLoadRadius) * 2 + 1;
                queueLimit = Math.Clamp(Math.Max(queueLimit, (int)Math.Ceiling(worldQueueHint * worldQueueHint * queueSlackRatio)), 128, 16384);
            }
        }

        private (int QueueLimit, int PressureFactor, double SlackRatio, double LoadSheddingThreshold, bool EmergencyBrake) GetAdaptiveQueueState()
        {
            int inflight = generationTasks.Count;
            int budget = Math.Max(128, maxLoadedChunks);
            double load = inflight / Math.Max(1.0, budget);
            double adaptiveEmaBlend = WorldMapQueuePolicy.ComputeAdaptiveEmaBlend(
                queueLoadEmaBlend,
                load,
                queueLoadEma,
                queueEmergencyBrakeLatched);
            queueLoadEma = WorldMapQueuePolicy.UpdateEma(queueLoadEma, load, adaptiveEmaBlend);
            double effectiveLoad = Math.Max(load, queueLoadEma);
            double loadTrend = WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma);
            double volatilityRatio = WorldMapQueuePolicy.ComputeVolatilityRatio(
                load,
                queueLoadEma,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueTrendBoostWeight,
                queueShockAbsorberWeight);
            queueEmergencyBrakeLatched = WorldMapQueuePolicy.UpdateEmergencyLatch(
                queueEmergencyBrakeLatched,
                effectiveLoad,
                queueEmergencyBrakeThreshold,
                queueEmergencyReleaseRatio);

            bool emergencyBrake = queueEmergencyBrakeLatched;
            double volatilityGuard = WorldMapQueuePolicy.ComputeVolatilityGuardScale(
                volatilityRatio,
                emergencyBrake,
                0.62,
                1.0);
            double shockScale = WorldMapQueuePolicy.ComputeShockAbsorberScale(
                effectiveLoad,
                loadTrend,
                emergencyBrake,
                queueShockAbsorberWeight);
            double hydrologyQueueScale = WorldMapQueuePolicy.ComputeHydrologyQueueStabilityScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.HydrologyThalwegStabilityWeight,
                queueBurstSlackMultiplier,
                emergencyBrake,
                0.6,
                1.18);
            double seamResilienceScale = WorldMapQueuePolicy.ComputeHydrologySeamResilienceScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.HydrologySeamRelaxBlend,
                generationConfig.Water.HydrologyEdgeFluxBlend,
                emergencyBrake,
                0.62,
                1.2);
            double alluvialRelayScale = WorldMapQueuePolicy.ComputeAlluvialAquiferRelayScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.HydrologySeamRelaxBlend,
                generationConfig.Water.HydrologyEdgeFluxBlend,
                generationConfig.Water.HydrologyFlowPersistence * queueAlluvialRelayWeight,
                generationConfig.Caves.GroundwaterConnectivityWeight * queueAlluvialRelayWeight,
                generationConfig.Lakes.FlowSeepageWeight * queueAlluvialRelayWeight,
                emergencyBrake,
                0.62,
                1.24);
            double karstFloodplainRelayScale = WorldMapQueuePolicy.ComputeKarstFloodplainRelayScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.HydrologySeamRelaxBlend,
                generationConfig.Water.HydrologyEdgeFluxBlend,
                generationConfig.Water.HydrologyFlowPersistence * queueAlluvialRelayWeight,
                generationConfig.Caves.GroundwaterConnectivityWeight * queueAlluvialRelayWeight,
                generationConfig.Lakes.SpillwayContinuityWeight * queueAlluvialRelayWeight,
                generationConfig.Caves.CaveVentilationBias * queueAlluvialRelayWeight,
                emergencyBrake,
                0.62,
                1.26);
            double spillwayQueueScale = WorldMapQueuePolicy.ComputeFloodplainSpillwayQueueScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.HydrologySeamRelaxBlend,
                generationConfig.Water.HydrologyEdgeFluxBlend,
                generationConfig.Water.HydrologyFlowPersistence * queueAlluvialRelayWeight,
                generationConfig.Caves.GroundwaterConnectivityWeight * queueAlluvialRelayWeight,
                generationConfig.Lakes.SpillwayContinuityWeight * queueAlluvialRelayWeight,
                generationConfig.Lakes.SpillRetentionWeight * queueAlluvialRelayWeight,
                generationConfig.Caves.CaveVentilationBias * queueAlluvialRelayWeight,
                emergencyBrake,
                0.62,
                1.28);
            double combinedHydrologyScale = Math.Clamp(
                hydrologyQueueScale * seamResilienceScale * alluvialRelayScale * karstFloodplainRelayScale * spillwayQueueScale,
                0.56,
                1.3);
            QueuePressureBand pressureBand = WorldMapQueuePolicy.ClassifyBand(effectiveLoad);
            double adaptiveSlack = Math.Clamp(
                queueSlackRatio + effectiveLoad * 0.6 * shockScale + Math.Max(0.0, loadTrend) * queueTrendBoostWeight * 0.75 * shockScale,
                queueSlackRatio,
                6.0);
            adaptiveSlack = Math.Clamp(
                queueSlackRatio + (adaptiveSlack - queueSlackRatio) * volatilityGuard * combinedHydrologyScale,
                queueSlackRatio,
                6.0);
            double burstMultiplier = !emergencyBrake && load >= 0.9
                ? 1.0 + (queueBurstSlackMultiplier - 1.0) * shockScale
                : 1.0;
            int adaptiveLimit = Math.Clamp(
                (int)Math.Ceiling(Math.Max(128, budget) * adaptiveSlack * burstMultiplier * Math.Max(0.8, volatilityGuard) * Math.Clamp(combinedHydrologyScale, 0.7, 1.2)),
                128,
                16384);
            adaptiveLimit = Math.Max(adaptiveLimit, queueLimit);

            int adaptivePressure = WorldMapQueuePolicy.ComputeAdaptivePressureFactor(
                queuePressureFactor,
                pressureBand,
                loadTrend * shockScale,
                emergencyBrake,
                queueTrendBoostWeight);
            adaptivePressure = Math.Clamp(adaptivePressure + (int)Math.Ceiling(volatilityRatio * 1.5), 1, 8);
            if (combinedHydrologyScale < 0.9)
            {
                adaptivePressure = Math.Clamp(adaptivePressure + 1, 1, 8);
            }
            double pressurePenalty = pressureBand switch
            {
                QueuePressureBand.Critical => 0.07,
                QueuePressureBand.High => 0.04,
                QueuePressureBand.Elevated => 0.015,
                _ => 0.0
            };

            double adaptiveLoadSheddingThreshold = Math.Clamp(
                queueLoadSheddingThreshold - effectiveLoad * 0.08 * shockScale - pressurePenalty - Math.Clamp(volatilityRatio * 0.05, 0.0, 0.08),
                0.5,
                queueLoadSheddingThreshold);
            if (emergencyBrake)
            {
                adaptiveLoadSheddingThreshold = Math.Clamp(adaptiveLoadSheddingThreshold - 0.06, 0.5, queueLoadSheddingThreshold);
                adaptivePressure = Math.Clamp(Math.Max(adaptivePressure, queuePressureFactor + 1), 1, 8);
            }

            return (adaptiveLimit, adaptivePressure, adaptiveSlack, adaptiveLoadSheddingThreshold, emergencyBrake);
        }

        private void TrimCompletedGenerationTasks()
        {
            if (queueOverloadDrainFactor <= 0)
            {
                return;
            }

            int removed = 0;
            foreach (var pair in generationTasks)
            {
                if (!pair.Value.IsCompleted)
                {
                    continue;
                }

                if (generationTasks.TryRemove(pair.Key, out _))
                {
                    removed++;
                }

                if (removed >= queueOverloadDrainFactor)
                {
                    break;
                }
            }
        }

        private void EnforceLoadedChunkBudget()
        {
            TrimDanglingAccessTimes();
            int budget = Math.Max(128, Math.Min(8192, maxLoadedChunks + generationTasks.Count * 2));
            int overBudget = loadedChunks.Count - budget;
            if (overBudget <= 0)
            {
                return;
            }

            foreach (var key in accessTimes.OrderBy(entry => entry.Value).Select(entry => entry.Key))
            {
                if (overBudget <= 0)
                {
                    break;
                }

                if (loadedChunks.TryRemove(key, out _))
                {
                    accessTimes.TryRemove(key, out _);
                    overBudget--;
                }
            }

            if (overBudget <= 0)
            {
                return;
            }

            foreach (var key in loadedChunks.Keys)
            {
                if (overBudget <= 0)
                {
                    break;
                }

                if (loadedChunks.TryRemove(key, out _))
                {
                    accessTimes.TryRemove(key, out _);
                    overBudget--;
                }
            }
        }

        private void TrimDanglingAccessTimes()
        {
            foreach (var key in accessTimes.Keys)
            {
                if (loadedChunks.ContainsKey(key))
                {
                    continue;
                }

                accessTimes.TryRemove(key, out _);
            }
        }

        private string ComputeGenerationSignature()
        {
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtocolRegistry.ValidateBindings();
            var queueState = GetAdaptiveQueueState();
            double queueLoadSnapshot = Math.Max(
                generationTasks.Count / Math.Max(1.0, Math.Max(128, maxLoadedChunks)),
                queueLoadEma);
            int signatureNearKeepBaseline = WorldMapQueuePolicy.ClampNearChunkKeepCount(
                Math.Max(8, maxLoadedChunks / Math.Max(2, queuePressureFactor * 2)),
                24);
            int signatureNearKeepUpdateDriven = Math.Max(16, Math.Min(512, queueLimit / Math.Max(2, queuePressureFactor * 4)));
            int signatureNearKeepCount = WorldMapQueuePolicy.ComputeAdaptiveNearChunkKeepCount(
                signatureNearKeepBaseline,
                signatureNearKeepUpdateDriven,
                WorldMapQueuePolicy.ClassifyBand(queueLoadSnapshot),
                queueLoadSnapshot,
                queueState.EmergencyBrake,
                0.42,
                1.0,
                8,
                512);
            long seed = worldSettings.WorldSeed != 0 ? worldSettings.WorldSeed : generationConfig.Seed;
            int effectiveChunkSize = controlProfile?.ChunkSize > 0 ? controlProfile.ChunkSize : generationConfig.ChunkSize;
            int effectiveRenderDistance = controlProfile?.RenderDistance > 0 ? controlProfile.RenderDistance : generationConfig.RenderDistance;
            int effectiveSimulationDistance = controlProfile?.SimulationDistance > 0 ? controlProfile.SimulationDistance : generationConfig.SimulationDistance;
            int effectiveGlobalWaterLevel = controlProfile?.GlobalWaterLevel > 0 ? controlProfile.GlobalWaterLevel : generationConfig.Water.GlobalWaterLevel;
            string effectiveHydrologySignature = string.IsNullOrWhiteSpace(controlProfile?.HydrologySignature)
                ? SharedFeatureCatalog.HydrologySignature
                : controlProfile!.HydrologySignature;

            var context = new WorldMapSignatureContext(
                PipelineVersion,
                generationConfig.WorldName,
                seed,
                ProtoFingerprint.DescriptorFingerprint,
                ProtoFingerprint.ComputeFingerprint(),
                controlProfile?.Version ?? generationConfig.MapControlProfileVersion,
                controlProfile?.ProfileHash ?? "no-profile",
                string.IsNullOrWhiteSpace(worldConfigHash) ? ComputeFileHash(worldConfigPath) : worldConfigHash,
                string.IsNullOrWhiteSpace(profileFileHash) ? ComputeFileHash(profilePath) : profileFileHash,
                effectiveHydrologySignature,
                effectiveChunkSize,
                generationConfig.WorldHeight,
                effectiveRenderDistance,
                effectiveSimulationDistance,
                effectiveGlobalWaterLevel,
                generationConfig.TerrainGeneration.SeaLevel,
                generationConfig.Water.HydrologyFlowPersistence,
                generationConfig.Water.HydrologyCatchmentWeight,
                generationConfig.Water.HydrologyFlowGain,
                generationConfig.Water.HydrologyWatershedStitchWeight,
                generationConfig.Water.HydrologyWatershedStitchRadius,
                generationConfig.Water.HydrologyGradientStabilityIterations,
                generationConfig.Water.HydrologyGradientStabilityBlend,
                generationConfig.Water.HydrologyGradientClamp,
                generationConfig.Water.HydrologyCurvatureWeight,
                generationConfig.Water.HydrologySlopePenalty,
                generationConfig.Water.HydrologyWaterTableClampWeight,
                generationConfig.Water.HydrologyWaterTableClampRange,
                generationConfig.Water.HydrologyWaterTableSlopeWeight,
                generationConfig.Lakes.MinDepth,
                generationConfig.Lakes.MaxDepth,
                generationConfig.Lakes.MaxRadius,
                generationConfig.Lakes.ShelfDepth,
                generationConfig.Lakes.FlowSeepageWeight,
                generationConfig.Lakes.OutflowSealWeight,
                generationConfig.Lakes.OutflowStabilityWeight,
                generationConfig.Caves.CeilingMoistureWeight,
                generationConfig.Caves.CeilingMoistureClamp,
                generationConfig.Caves.MoistureFlowClamp,
                generationConfig.Caves.FloodedCaveNoiseFrequency,
                generationConfig.Caves.FloodedCaveThreshold,
                generationConfig.Caves.FloodedCaveProximityToWaterTableWeight,
                generationConfig.Caves.WaterThreshold,
                generationConfig.Caves.LavaThreshold,
                generationConfig.Water.HydrologyEdgeBlendRadius,
                generationConfig.Water.HydrologyEdgeVarianceClamp,
                generationConfig.Water.HydrologyEdgeNormalizationBlend,
                generationConfig.Water.HydrologyEdgeNormalizationIterations,
                generationConfig.Water.HydrologyFlowMemoryWeight,
                generationConfig.Water.HydrologyContinuityWeight,
                generationConfig.Water.RiverMeanderJitter,
                generationConfig.Water.RiverReliefPenaltyWeight,
                generationConfig.Water.RiverAnisotropyDamping,
                generationConfig.Water.RiverBankStabilityClamp,
                generationConfig.Water.RiverSeamFillStrength,
                generationConfig.Lakes.RiverProximitySuppression,
                generationConfig.Water.HydrologyFlowShadowWeight,
                generationConfig.Water.HydrologyFlowShadowSlopeWeight,
                generationConfig.Water.HydrologyPressureBlend,
                generationConfig.Water.HydrologyPressureGradientClamp,
                generationConfig.Water.HydrologyEdgeFlowBias,
                generationConfig.Water.HydrologyEdgeFlowLockWeight,
                generationConfig.Water.HydrologyEdgeTangentWeight,
                generationConfig.Water.RiverFlowAlignmentWeight,
                generationConfig.Water.RiverConfluenceBoost,
                generationConfig.Water.RiverTributaryCaptureWeight,
                generationConfig.Water.RiverAvulsionResistance,
                generationConfig.Water.RiverBraidingWeight,
                generationConfig.Water.LakeRimErosionWeight,
                generationConfig.Lakes.VarianceWeight,
                generationConfig.Water.LakeInflowBlendWeight,
                generationConfig.Lakes.OutflowCarveDepth,
                generationConfig.Caves.EdgeSealStrength,
                generationConfig.Caves.RiverSuppressionWeight,
                generationConfig.Caves.RiparianCaveGuardWeight,
                generationConfig.Water.HydrologyReservoirIterations,
                generationConfig.Water.HydrologyReservoirBlend,
                generationConfig.Water.RiverEdgeContinuityWeight,
                generationConfig.Lakes.LakeOutflowTaper,
                generationConfig.Lakes.SpillRetentionWeight,
                generationConfig.Lakes.SpillwayContinuityWeight,
                generationConfig.Caves.CaveEntranceFlowDampening,
                generationConfig.Caves.GroundwaterConnectivityWeight,
                generationConfig.Caves.CaveVentilationBias,
                generationConfig.Caves.AquiferBarrierWeight,
                generationConfig.Water.RiverNoiseScale,
                generationConfig.Water.RiverIntensitySmoothIterations,
                generationConfig.Water.RiverIntensitySmoothBlend,
                generationConfig.Lakes.ShorelineBlend,
                generationConfig.Lakes.WetlandSaturationThreshold,
                generationConfig.Caves.SupportDensity,
                generationConfig.Caves.MoistureRetentionWeight,
                generationConfig.Caves.CeilingStabilityWeight,
                Math.Max(64, maxLoadedChunks),
                generationTasks.Count,
                Math.Max(1, queueState.PressureFactor),
                Math.Max(64, queueState.QueueLimit),
                signatureNearKeepCount,
                Math.Clamp(queueState.LoadSheddingThreshold, 0.5, 0.98),
                Math.Max(1.1, queueState.SlackRatio),
                Math.Max(1.0, queueBurstSlackMultiplier),
                Math.Max(0.0, queueShockAbsorberWeight));

            return WorldMapSignature.Compute(context);
        }
    }
}
