using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GameCommon.World;
using GameServerApp;
using GameServerApp.Configuration;
using GameServerApp.World.Generation;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.World
{
    /// <summary>
    /// Lightweight world map control service that reuses the enhanced terrain pipeline to
    /// generate preview chunks and track per-player map preferences.
    /// </summary>
    public sealed class WorldMapControlManager
    {
        private const string PipelineVersion = SharedFeatureCatalog.HydrologySignature;
        private readonly WorldMapControlSettings settings;
        private EnhancedTerrainGenerationPipeline pipeline;
        private WorldMapControlProfile controlProfile;
        private WorldGenerationConfig generationConfig;
        private readonly WorldSettings worldSettings;
        private readonly ConcurrentDictionary<int, WorldMapProfile> profiles = new();
        private readonly ConcurrentDictionary<(int X, int Z), ChunkData> chunkCache = new();
        private readonly ConcurrentDictionary<(int X, int Z), DateTime> chunkAccessTimes = new();
        private readonly ConcurrentDictionary<(int X, int Z), Task<ChunkData>> inflightChunkGenerations = new();
        private readonly int maxCachedChunks;
        private readonly int configuredQueueLimit;
        private readonly int configuredQueuePressureFactor;
        private readonly double configuredQueueSlackRatio;
        private readonly double configuredQueueBurstSlackMultiplier;
        private readonly double configuredQueueLoadSheddingThreshold;
        private readonly double configuredQueueEmergencyBrakeThreshold;
        private readonly int configuredQueueOverloadDrainFactor;
        private readonly int configuredQueueBackoffDelayMs;
        private int dynamicQueueLimit;
        private int dynamicQueuePressureFactor;
        private double dynamicQueueSlackRatio;
        private double dynamicQueueLoadSheddingThreshold;
        private double queueLoadEma;
        private int queueOverloadTicks;
        private DateTime lastQueuePolicyAdjustUtc;
        private DateTime worldConfigWriteTime;
        private DateTime profileWriteTime;
        private DateTime lastInflightPruneUtc;
        private string generationSignature = string.Empty;
        private string worldConfigHash;
        private string profileContentHash;

        public WorldMapControlManager(WorldMapControlSettings settings, WorldGenerationConfig generationConfig, WorldSettings worldSettings)
        {
            ProtoRuntime.EnsureInitialized();
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.generationConfig = generationConfig ?? throw new ArgumentNullException(nameof(generationConfig));
            this.worldSettings = worldSettings ?? throw new ArgumentNullException(nameof(worldSettings));

            pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, this.worldSettings);
            controlProfile = WorldMapControlProfileUtility.LoadOrCreate(generationConfig, this.worldSettings);
            worldConfigWriteTime = GetWriteTime(this.generationConfig.SourcePath);
            profileWriteTime = GetWriteTime(generationConfig.MapControlProfilePath);
            lastInflightPruneUtc = DateTime.UtcNow;
            worldConfigHash = ComputeFileHash(this.generationConfig.SourcePath);
            profileContentHash = ComputeFileHash(generationConfig.MapControlProfilePath);
            queueLoadEma = 0.0;
            queueOverloadTicks = 0;
            lastQueuePolicyAdjustUtc = DateTime.UtcNow;
            int computedBudget = Math.Max(
                this.settings.DefaultUnloadDistance * this.settings.DefaultUnloadDistance,
                this.settings.DefaultRenderDistance * this.settings.DefaultRenderDistance * 2);
            maxCachedChunks = this.settings.MaxCachedChunks > 0
                ? Math.Max(64, this.settings.MaxCachedChunks)
                : computedBudget;
            configuredQueueLimit = Math.Clamp(Math.Max(128, this.settings.MaxQueuedChunkRequests), 128, 16384);
            configuredQueuePressureFactor = Math.Clamp(Math.Max(1, this.settings.QueuePressureFactor), 1, 8);
            configuredQueueSlackRatio = Math.Clamp(this.settings.QueueSlackRatio <= 0.0 ? 2.0 : this.settings.QueueSlackRatio, 1.1, 6.0);
            configuredQueueBurstSlackMultiplier = Math.Clamp(this.settings.QueueBurstSlackMultiplier <= 0.0 ? 1.15 : this.settings.QueueBurstSlackMultiplier, 1.0, 3.0);
            configuredQueueLoadSheddingThreshold = Math.Clamp(this.settings.QueueLoadSheddingThreshold <= 0.0 ? 0.88 : this.settings.QueueLoadSheddingThreshold, 0.5, 0.98);
            configuredQueueEmergencyBrakeThreshold = Math.Clamp(this.settings.QueueEmergencyBrakeThreshold <= 0.0 ? 1.15 : this.settings.QueueEmergencyBrakeThreshold, 0.75, 4.0);
            configuredQueueOverloadDrainFactor = Math.Clamp(Math.Max(1, this.settings.QueueOverloadDrainFactor), 1, 16);
            configuredQueueBackoffDelayMs = Math.Clamp(Math.Max(1, this.settings.QueueBackoffDelayMs), 1, 200);
            dynamicQueueLimit = Math.Max(64, this.settings.UpdateBatchSize * 16);
            dynamicQueuePressureFactor = Math.Max(1, this.settings.UpdateIntervalMs <= 75 ? 3 : 2);
            dynamicQueueSlackRatio = configuredQueueSlackRatio;
            dynamicQueueLoadSheddingThreshold = configuredQueueLoadSheddingThreshold;
            RefreshGenerationSignature(rebuildPipeline: false);
        }

        public Task<WorldMapResponse> HandleAsync(WorldMapRequest request)
        {
            ProtoRuntime.EnsureInitialized();
            RefreshGenerationSignature(rebuildPipeline: false);
            return request.Type switch
            {
                WorldMapRequestType.GetInitialMap => HandleInitialMapAsync(request),
                WorldMapRequestType.UpdateChunk => HandleChunkUpdateAsync(request),
                WorldMapRequestType.GetPlayerProfile => HandleProfileAsync(request, updateProfile: false),
                WorldMapRequestType.UpdatePlayerProfile => HandleProfileAsync(request, updateProfile: true),
                _ => Task.FromResult(new WorldMapResponse { Success = false, ErrorMessage = "Unknown request type" })
            };
        }

        private WorldMapProfile GetOrCreateProfile(int playerId)
        {
            return profiles.GetOrAdd(playerId, id => new WorldMapProfile
            {
                PlayerId = id,
                RenderDistance = settings.DefaultRenderDistance,
                MapScale = settings.DefaultMapScale,
                ShowCoordinates = settings.DefaultShowCoordinates,
                ShowBiomeInfo = settings.DefaultShowBiomeInfo,
                TerrainQuality = settings.DefaultTerrainQuality,
                WaterQuality = settings.DefaultWaterQuality,
                VegetationQuality = settings.DefaultVegetationQuality,
                LastUpdateTime = DateTime.UtcNow
            });
        }

        private async Task<WorldMapResponse> HandleInitialMapAsync(WorldMapRequest request)
        {
            var currentProfile = EnsureProfile(out _);
            var profile = GetOrCreateProfile(request.PlayerId);
            var playerChunkX = (int)(request.PlayerX / 16);
            var playerChunkZ = (int)(request.PlayerZ / 16);
            var renderDistance = Math.Max(1, profile.RenderDistance);

            var chunks = new List<ChunkData>();
            var prioritized = WorldMapQueuePolicy.EnumerateByDistance(playerChunkX, playerChunkZ, renderDistance);
            foreach (var chunkCoordinate in prioritized)
            {
                var chunk = await GenerateOrGetChunkAsync(chunkCoordinate.X, chunkCoordinate.Z);
                chunks.Add(chunk);
            }

            profile.LastPosition = new PlayerPosition { X = request.PlayerX, Y = request.PlayerY, Z = request.PlayerZ };
            profile.LastUpdateTime = DateTime.UtcNow;

            return new WorldMapResponse
            {
                Success = true,
                ControlProfile = currentProfile,
                ControlProfileHash = currentProfile.ProfileHash,
                GenerationSignature = generationSignature,
                WorldMapData = new WorldMapData
                {
                    Chunks = chunks,
                    PlayerPosition = profile.LastPosition
                },
                PlayerProfile = profile
            };
        }

        private async Task<WorldMapResponse> HandleChunkUpdateAsync(WorldMapRequest request)
        {
            var currentProfile = EnsureProfile(out bool profileChanged);
            var profile = GetOrCreateProfile(request.PlayerId);
            var updates = request.ChunkUpdates ?? new List<ChunkUpdate>();
            var chunkList = new List<ChunkData>();

            foreach (var update in updates)
            {
                var chunk = await GenerateOrGetChunkAsync(update.ChunkX, update.ChunkZ);
                chunkList.Add(chunk);
            }

            profile.LastUpdateTime = DateTime.UtcNow;

            return new WorldMapResponse
            {
                Success = true,
                ControlProfileHash = currentProfile.ProfileHash,
                GenerationSignature = generationSignature,
                ControlProfile = profileChanged ? currentProfile : null,
                WorldMapData = new WorldMapData
                {
                    Chunks = chunkList,
                    PlayerPosition = profile.LastPosition
                },
                PlayerProfile = profile
            };
        }

        private Task<WorldMapResponse> HandleProfileAsync(WorldMapRequest request, bool updateProfile)
        {
            var currentProfile = EnsureProfile(out _);
            var profile = GetOrCreateProfile(request.PlayerId);

            if (updateProfile && request.ProfileUpdates != null)
            {
                foreach (var update in request.ProfileUpdates)
                {
                    switch (update.Type)
                    {
                        case ProfileUpdateType.RenderDistance:
                            profile.RenderDistance = Math.Clamp(update.Value, 2, settings.DefaultUnloadDistance);
                            break;
                        case ProfileUpdateType.MapScale:
                            profile.MapScale = Math.Clamp(update.Number, 0.25, 8.0);
                            break;
                        case ProfileUpdateType.ShowCoordinates:
                            profile.ShowCoordinates = update.Flag;
                            break;
                        case ProfileUpdateType.ShowBiomeInfo:
                            profile.ShowBiomeInfo = update.Flag;
                            break;
                    }
                }

                profile.LastUpdateTime = DateTime.UtcNow;
            }

            return Task.FromResult(new WorldMapResponse
            {
                Success = true,
                ControlProfileHash = currentProfile.ProfileHash,
                GenerationSignature = generationSignature,
                PlayerProfile = profile
            });
        }

        private WorldMapControlProfile EnsureProfile(out bool profileChanged)
        {
            profileChanged = false;
            MaybeReloadGenerationConfig(ref profileChanged);
            var loaded = WorldMapControlProfileUtility.Load(generationConfig.MapControlProfilePath);
            if (loaded != null)
            {
                loaded.EnsureDefaults();
                if (string.IsNullOrWhiteSpace(loaded.ProfileHash))
                {
                    loaded.ProfileHash = WorldMapControlProfileUtility.ComputeHash(loaded);
                    WorldMapControlProfileUtility.Save(loaded, generationConfig.MapControlProfilePath);
                    profileChanged = true;
                }
            }

            string currentProfileContentHash = ComputeFileHash(generationConfig.MapControlProfilePath);

            bool configNewerThanProfile = GetWriteTime(generationConfig.SourcePath) > GetWriteTime(generationConfig.MapControlProfilePath);
            bool profileHashDrift = loaded != null &&
                !string.Equals(loaded.ProfileHash, WorldMapControlProfileUtility.ComputeHash(loaded), StringComparison.OrdinalIgnoreCase);
            bool versionMismatch = loaded != null && generationConfig.MapControlProfileVersion > loaded.Version;
            bool profileFileUpdated = GetWriteTime(generationConfig.MapControlProfilePath) > profileWriteTime;
            bool profileContentChanged = !string.IsNullOrWhiteSpace(profileContentHash) &&
                !string.Equals(profileContentHash, currentProfileContentHash, StringComparison.OrdinalIgnoreCase);
            bool signatureMismatch = loaded != null &&
                !string.Equals(loaded.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase);
            bool profileSignatureMismatch = loaded != null &&
                !string.Equals(ComputeGenerationSignatureForProfile(loaded), generationSignature, StringComparison.Ordinal);

            if (loaded == null || configNewerThanProfile || profileHashDrift || versionMismatch || profileFileUpdated || profileContentChanged || signatureMismatch || profileSignatureMismatch)
            {
                controlProfile = WorldMapControlProfileUtility.LoadOrCreate(generationConfig, worldSettings);
                WorldMapControlProfileUtility.Save(controlProfile, generationConfig.MapControlProfilePath);
                chunkCache.Clear();
                chunkAccessTimes.Clear();
                inflightChunkGenerations.Clear();
                pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
                profileChanged = true;
                RefreshGenerationSignature(rebuildPipeline: false);
                profileWriteTime = GetWriteTime(generationConfig.MapControlProfilePath);
                profileContentHash = currentProfileContentHash;
                return controlProfile;
            }

            if (!string.Equals(loaded.ProfileHash, controlProfile.ProfileHash, StringComparison.OrdinalIgnoreCase) ||
                loaded.Version > controlProfile.Version)
            {
                controlProfile = loaded;
                chunkCache.Clear();
                chunkAccessTimes.Clear();
                inflightChunkGenerations.Clear();
                pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
                profileChanged = true;
                profileWriteTime = GetWriteTime(generationConfig.MapControlProfilePath);
                profileContentHash = currentProfileContentHash;
            }

            RefreshGenerationSignature(rebuildPipeline: false);
            return controlProfile;
        }

        private async Task<ChunkData> GenerateOrGetChunkAsync(int chunkX, int chunkZ)
        {
            PruneInflightGenerations();
            int adaptiveQueueLimit = GetAdaptiveQueueLimit();
            int loadSheddingLimit = Math.Max(64, (int)Math.Floor(adaptiveQueueLimit * Math.Max(0.5, dynamicQueueLoadSheddingThreshold)));
            var key = (chunkX, chunkZ);
            if (chunkCache.TryGetValue(key, out var cached))
            {
                chunkAccessTimes[key] = DateTime.UtcNow;
                return cached;
            }

            if (inflightChunkGenerations.TryGetValue(key, out var inflight))
            {
                return await inflight.ConfigureAwait(false);
            }

            if (inflightChunkGenerations.Count >= loadSheddingLimit)
            {
                PruneInflightGenerations(Math.Max(configuredQueueOverloadDrainFactor, dynamicQueuePressureFactor));
                await Task.Delay(Math.Max(1, configuredQueueBackoffDelayMs * dynamicQueuePressureFactor)).ConfigureAwait(false);
                if (inflightChunkGenerations.TryGetValue(key, out var shedInflight))
                {
                    return await shedInflight.ConfigureAwait(false);
                }
            }

            int effectiveCacheBudget = Math.Max(64, GetEffectiveCacheBudget());
            double queueLoad = inflightChunkGenerations.Count / Math.Max(1.0, effectiveCacheBudget);
            if (queueLoad >= configuredQueueEmergencyBrakeThreshold)
            {
                int emergencyDrain = Math.Max(configuredQueueOverloadDrainFactor + 1, dynamicQueuePressureFactor + 1);
                PruneInflightGenerations(Math.Clamp(emergencyDrain, 1, 24));
                await Task.Delay(Math.Max(1, configuredQueueBackoffDelayMs * (dynamicQueuePressureFactor + 1))).ConfigureAwait(false);
                if (inflightChunkGenerations.TryGetValue(key, out var emergencyInflight))
                {
                    return await emergencyInflight.ConfigureAwait(false);
                }
            }

            if (inflightChunkGenerations.Count >= Math.Max(64, adaptiveQueueLimit))
            {
                PruneInflightGenerations(configuredQueueOverloadDrainFactor);
                await Task.Delay(Math.Max(1, configuredQueueBackoffDelayMs * dynamicQueuePressureFactor)).ConfigureAwait(false);
                if (inflightChunkGenerations.TryGetValue(key, out var delayedInflight))
                {
                    return await delayedInflight.ConfigureAwait(false);
                }
            }

            var generationTask = pipeline.GenerateChunkAsync(chunkX, chunkZ);
            if (!inflightChunkGenerations.TryAdd(key, generationTask))
            {
                if (inflightChunkGenerations.TryGetValue(key, out var existing))
                {
                    return await existing.ConfigureAwait(false);
                }
            }

            try
            {
                var generated = await generationTask.ConfigureAwait(false);
                chunkCache[key] = generated;
                chunkAccessTimes[key] = DateTime.UtcNow;
                EnforceCacheBudget();
                return generated;
            }
            finally
            {
                inflightChunkGenerations.TryRemove(key, out _);
            }
        }

        private void MaybeReloadGenerationConfig(ref bool profileChanged)
        {
            if (string.IsNullOrWhiteSpace(generationConfig.SourcePath) || !File.Exists(generationConfig.SourcePath))
            {
                return;
            }

            var writeTime = GetWriteTime(generationConfig.SourcePath);
            string newConfigHash = ComputeFileHash(generationConfig.SourcePath);
            if (writeTime <= worldConfigWriteTime && string.Equals(worldConfigHash, newConfigHash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var reloaded = WorldGenerationConfig.Load(generationConfig.SourcePath);
            reloaded.MapControlProfilePath = generationConfig.MapControlProfilePath;
            reloaded.MapControlProfileVersion = Math.Max(generationConfig.MapControlProfileVersion, reloaded.MapControlProfileVersion);
            generationConfig = reloaded;
            RecomputeQueuePolicy();
            worldConfigWriteTime = writeTime;
            worldConfigHash = newConfigHash;
            controlProfile = WorldMapControlProfileUtility.LoadOrCreate(generationConfig, worldSettings);
            pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
            chunkCache.Clear();
            chunkAccessTimes.Clear();
            inflightChunkGenerations.Clear();
            profileChanged = true;
            RefreshGenerationSignature(rebuildPipeline: false);
            profileWriteTime = GetWriteTime(generationConfig.MapControlProfilePath);
            profileContentHash = ComputeFileHash(generationConfig.MapControlProfilePath);
        }

        private void RecomputeQueuePolicy()
        {
            int renderWindow = Math.Max(1, settings.DefaultRenderDistance * 2 + 1);
            int profileWindow = Math.Max(1, controlProfile?.RenderDistance ?? settings.DefaultRenderDistance) * 2 + 1;
            int mapWindow = Math.Max(renderWindow * renderWindow, profileWindow * profileWindow);
            int inflightBudget = Math.Max(8, settings.MaxConcurrentChunkGenerations * Math.Max(2, settings.UpdateBatchSize / 8));
            int queueByCache = Math.Max(128, Math.Min(8192, (int)Math.Ceiling(GetEffectiveCacheBudget() * configuredQueueSlackRatio)));
            int queueByWindow = Math.Max(128, (int)Math.Ceiling(mapWindow * Math.Max(2, settings.UpdateBatchSize / 8) * Math.Min(2.0, configuredQueueSlackRatio * 0.75)));
            dynamicQueueLimit = Math.Clamp(Math.Max(Math.Max(queueByCache, queueByWindow), configuredQueueLimit), 128, 16384);
            dynamicQueueSlackRatio = configuredQueueSlackRatio;
            dynamicQueueLoadSheddingThreshold = configuredQueueLoadSheddingThreshold;
            queueLoadEma = 0.0;
            queueOverloadTicks = 0;
            lastQueuePolicyAdjustUtc = DateTime.UtcNow;

            double ratio = dynamicQueueLimit / Math.Max(1.0, GetEffectiveCacheBudget());
            dynamicQueuePressureFactor = ratio >= 3.0 ? 3 : (ratio >= 2.0 ? 2 : 1);
            dynamicQueuePressureFactor = Math.Clamp(Math.Max(dynamicQueuePressureFactor, configuredQueuePressureFactor), 1, 8);
            _ = inflightBudget;
        }

        private int GetAdaptiveQueueLimit()
        {
            int inflight = inflightChunkGenerations.Count;
            int cacheBudget = Math.Max(64, GetEffectiveCacheBudget());
            double instantaneousLoad = inflight / Math.Max(1.0, cacheBudget);
            queueLoadEma = queueLoadEma <= 0.0
                ? instantaneousLoad
                : queueLoadEma * 0.72 + instantaneousLoad * 0.28;
            double load = Math.Max(instantaneousLoad, queueLoadEma * 0.9);
            bool overloadTick = load >= configuredQueueLoadSheddingThreshold ||
                instantaneousLoad >= configuredQueueLoadSheddingThreshold;
            queueOverloadTicks = overloadTick
                ? Math.Min(queueOverloadTicks + 1, 128)
                : Math.Max(0, queueOverloadTicks - 1);
            double overloadBias = Math.Clamp(queueOverloadTicks / 24.0, 0.0, 0.45);

            dynamicQueueSlackRatio = Math.Clamp(
                configuredQueueSlackRatio + load * 0.6 + overloadBias * 0.35,
                configuredQueueSlackRatio,
                6.0);
            bool emergencyBrake = load >= configuredQueueEmergencyBrakeThreshold ||
                queueLoadEma >= configuredQueueEmergencyBrakeThreshold * 0.92;
            double burstMultiplier = !emergencyBrake && load >= 0.9
                ? configuredQueueBurstSlackMultiplier * (1.0 + overloadBias * 0.5)
                : 1.0;
            int candidateQueueLimit = (int)Math.Ceiling(cacheBudget * dynamicQueueSlackRatio * burstMultiplier);
            var now = DateTime.UtcNow;
            if ((now - lastQueuePolicyAdjustUtc).TotalMilliseconds >= Math.Max(15, configuredQueueBackoffDelayMs * 4))
            {
                dynamicQueueLimit = Math.Clamp(
                    Math.Max(dynamicQueueLimit, candidateQueueLimit),
                    128,
                    16384);
                lastQueuePolicyAdjustUtc = now;
            }
            else
            {
                int gradualIncrease = Math.Max(16, configuredQueueOverloadDrainFactor * 8);
                dynamicQueueLimit = Math.Clamp(
                    Math.Max(dynamicQueueLimit, Math.Min(candidateQueueLimit, dynamicQueueLimit + gradualIncrease)),
                    128,
                    16384);
            }

            dynamicQueueLoadSheddingThreshold = Math.Clamp(
                configuredQueueLoadSheddingThreshold - load * 0.08 - overloadBias * 0.05,
                0.5,
                configuredQueueLoadSheddingThreshold);

            if (emergencyBrake)
            {
                dynamicQueueSlackRatio = Math.Clamp(Math.Max(configuredQueueSlackRatio, dynamicQueueSlackRatio * 0.92), configuredQueueSlackRatio, 6.0);
                dynamicQueueLoadSheddingThreshold = Math.Clamp(dynamicQueueLoadSheddingThreshold - 0.06, 0.5, configuredQueueLoadSheddingThreshold);
            }

            int pressure = load >= 2.0
                ? 4
                : load >= 1.3
                    ? 3
                    : load >= 0.8
                        ? 2
                        : 1;
            if (overloadBias >= 0.35)
            {
                pressure = Math.Max(pressure, 4);
            }
            else if (overloadBias >= 0.15)
            {
                pressure = Math.Max(pressure, 3);
            }

            dynamicQueuePressureFactor = Math.Clamp(Math.Max(configuredQueuePressureFactor, pressure), 1, 8);
            if (emergencyBrake)
            {
                dynamicQueuePressureFactor = Math.Clamp(Math.Max(dynamicQueuePressureFactor, configuredQueuePressureFactor + 1), 1, 8);
            }
            return dynamicQueueLimit;
        }

        private void EnforceCacheBudget()
        {
            int budget = GetEffectiveCacheBudget();
            int overBudget = chunkCache.Count - budget;
            if (overBudget <= 0)
            {
                return;
            }

            foreach (var key in chunkAccessTimes.OrderBy(entry => entry.Value).Select(entry => entry.Key))
            {
                if (overBudget <= 0)
                {
                    break;
                }

                if (chunkCache.TryRemove(key, out _))
                {
                    chunkAccessTimes.TryRemove(key, out _);
                    overBudget--;
                }
            }

            if (overBudget <= 0)
            {
                return;
            }

            foreach (var key in chunkCache.Keys)
            {
                if (overBudget <= 0)
                {
                    break;
                }

                if (chunkCache.TryRemove(key, out _))
                {
                    chunkAccessTimes.TryRemove(key, out _);
                    overBudget--;
                }
            }
        }

        private int GetEffectiveCacheBudget()
        {
            int renderDistance = controlProfile?.RenderDistance > 0
                ? controlProfile.RenderDistance
                : settings.DefaultRenderDistance;
            int simulationDistance = controlProfile?.SimulationDistance > 0
                ? controlProfile.SimulationDistance
                : settings.DefaultUnloadDistance;

            renderDistance = Math.Max(1, renderDistance);
            simulationDistance = Math.Max(1, simulationDistance);

            int renderWindow = (renderDistance * 2 + 1) * (renderDistance * 2 + 1);
            int simulationWindow = (simulationDistance * 2 + 1) * (simulationDistance * 2 + 1);
            int profileBudget = Math.Max(renderWindow, simulationWindow);
            int inflightPressure = inflightChunkGenerations.Count;
            int pressureBudget = profileBudget + (int)Math.Ceiling(inflightPressure * Math.Clamp(configuredQueueSlackRatio, 1.1, 3.0));
            int expandedBudget = Math.Max(maxCachedChunks, pressureBudget);
            int hardCap = Math.Max(128, maxCachedChunks * 2);
            return Math.Clamp(expandedBudget, 64, hardCap);
        }

        private void PruneInflightGenerations(int maxRemove = 0)
        {
            var now = DateTime.UtcNow;
            if ((now - lastInflightPruneUtc).TotalSeconds < 2)
            {
                return;
            }

            lastInflightPruneUtc = now;
            int removed = 0;
            foreach (var pair in inflightChunkGenerations)
            {
                var task = pair.Value;
                if (!task.IsCompleted)
                {
                    continue;
                }

                inflightChunkGenerations.TryRemove(pair.Key, out _);
                removed++;
                if (maxRemove > 0 && removed >= maxRemove)
                {
                    break;
                }
            }
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
                using var sha = SHA256.Create();
                byte[] data = File.ReadAllBytes(path);
                return Convert.ToHexString(sha.ComputeHash(data));
            }
            catch
            {
                return string.Empty;
            }
        }

        private void RefreshGenerationSignature(bool rebuildPipeline)
        {
            RecomputeQueuePolicy();
            string newSignature = ComputeGenerationSignature();
            if (string.Equals(newSignature, generationSignature, StringComparison.Ordinal))
            {
                return;
            }

            generationSignature = newSignature;
            chunkCache.Clear();
            chunkAccessTimes.Clear();
            inflightChunkGenerations.Clear();
            lastInflightPruneUtc = DateTime.UtcNow;

            if (rebuildPipeline)
            {
                pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
            }
        }

        private string ComputeGenerationSignatureForProfile(WorldMapControlProfile profile)
        {
            var previous = controlProfile;
            controlProfile = profile;
            try
            {
                return ComputeGenerationSignature();
            }
            finally
            {
                controlProfile = previous;
            }
        }

        private string ComputeGenerationSignature()
        {
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtocolRegistry.ValidateBindings();
            int adaptiveQueueLimit = GetAdaptiveQueueLimit();
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
                string.IsNullOrWhiteSpace(worldConfigHash) ? ComputeFileHash(generationConfig.SourcePath) : worldConfigHash,
                string.IsNullOrWhiteSpace(profileContentHash) ? ComputeFileHash(generationConfig.MapControlProfilePath) : profileContentHash,
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
                generationConfig.Lakes.SpillwayContinuityWeight,
                generationConfig.Caves.CaveEntranceFlowDampening,
                generationConfig.Caves.AquiferBarrierWeight,
                generationConfig.Water.RiverNoiseScale,
                generationConfig.Water.RiverIntensitySmoothIterations,
                generationConfig.Water.RiverIntensitySmoothBlend,
                generationConfig.Lakes.ShorelineBlend,
                generationConfig.Lakes.WetlandSaturationThreshold,
                generationConfig.Caves.SupportDensity,
                generationConfig.Caves.MoistureRetentionWeight,
                generationConfig.Caves.CeilingStabilityWeight,
                GetEffectiveCacheBudget(),
                inflightChunkGenerations.Count,
                Math.Max(1, dynamicQueuePressureFactor),
                Math.Max(64, adaptiveQueueLimit),
                Math.Clamp(dynamicQueueLoadSheddingThreshold, 0.5, 0.98),
                Math.Max(1.1, dynamicQueueSlackRatio),
                Math.Max(1.0, configuredQueueBurstSlackMultiplier));

            return WorldMapSignature.Compute(context);
        }
    }

    public sealed class WorldMapRequest
    {
        public WorldMapRequestType Type { get; set; }
        public int PlayerId { get; set; }
        public double PlayerX { get; set; }
        public double PlayerY { get; set; }
        public double PlayerZ { get; set; }
        public List<ChunkUpdate>? ChunkUpdates { get; set; }
        public List<ProfileUpdate>? ProfileUpdates { get; set; }
    }

    public sealed class WorldMapResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public WorldMapData? WorldMapData { get; set; }
        public WorldMapProfile? PlayerProfile { get; set; }
        public WorldMapControlProfile? ControlProfile { get; set; }
        public string ControlProfileHash { get; set; } = string.Empty;
        public string GenerationSignature { get; set; } = string.Empty;
    }

    public sealed class WorldMapData
    {
        public List<ChunkData>? Chunks { get; set; }
        public PlayerPosition PlayerPosition { get; set; } = new();
    }

    public sealed class PlayerPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public sealed class WorldMapProfile
    {
        public int PlayerId { get; set; }
        public int RenderDistance { get; set; }
        public double MapScale { get; set; }
        public bool ShowCoordinates { get; set; }
        public bool ShowBiomeInfo { get; set; }
        public int TerrainQuality { get; set; }
        public int WaterQuality { get; set; }
        public int VegetationQuality { get; set; }
        public PlayerPosition LastPosition { get; set; } = new();
        public DateTime LastUpdateTime { get; set; }
    }

    public sealed class ChunkUpdate
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
    }

    public sealed class ProfileUpdate
    {
        public ProfileUpdateType Type { get; set; }
        public int Value { get; set; }
        public double Number { get; set; }
        public bool Flag { get; set; }
    }
}
