using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private readonly int maxCachedChunks;
        private DateTime worldConfigWriteTime;
        private DateTime profileWriteTime;
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
            worldConfigHash = ComputeFileHash(this.generationConfig.SourcePath);
            profileContentHash = ComputeFileHash(generationConfig.MapControlProfilePath);
            maxCachedChunks = Math.Max(this.settings.DefaultUnloadDistance * this.settings.DefaultUnloadDistance, this.settings.DefaultRenderDistance * this.settings.DefaultRenderDistance * 2);
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
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    var chunk = await GenerateOrGetChunkAsync(playerChunkX + x, playerChunkZ + z);
                    chunks.Add(chunk);
                }
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

            if (loaded == null || configNewerThanProfile || profileHashDrift || versionMismatch || profileFileUpdated || profileContentChanged || signatureMismatch)
            {
                controlProfile = WorldMapControlProfileUtility.LoadOrCreate(generationConfig, worldSettings);
                WorldMapControlProfileUtility.Save(controlProfile, generationConfig.MapControlProfilePath);
                chunkCache.Clear();
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
            var key = (chunkX, chunkZ);
            if (chunkCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var generated = await pipeline.GenerateChunkAsync(chunkX, chunkZ);
            chunkCache[key] = generated;
            EnforceCacheBudget();
            return generated;
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
            generationConfig = reloaded;
            worldConfigWriteTime = writeTime;
            worldConfigHash = newConfigHash;
            controlProfile = WorldMapControlProfileUtility.LoadOrCreate(generationConfig, worldSettings);
            pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
            chunkCache.Clear();
            profileChanged = true;
            RefreshGenerationSignature(rebuildPipeline: false);
            profileWriteTime = GetWriteTime(generationConfig.MapControlProfilePath);
            profileContentHash = ComputeFileHash(generationConfig.MapControlProfilePath);
        }

        private void EnforceCacheBudget()
        {
            int overBudget = chunkCache.Count - maxCachedChunks;
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
                    overBudget--;
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
            string newSignature = ComputeGenerationSignature();
            if (string.Equals(newSignature, generationSignature, StringComparison.Ordinal))
            {
                return;
            }

            generationSignature = newSignature;
            chunkCache.Clear();

            if (rebuildPipeline)
            {
                pipeline = new EnhancedTerrainGenerationPipeline(generationConfig, worldSettings);
            }
        }

        private string ComputeGenerationSignature()
        {
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtocolRegistry.ValidateBindings();
            long seed = worldSettings.WorldSeed != 0 ? worldSettings.WorldSeed : generationConfig.Seed;
            double gradientStabilityBlend = generationConfig.Water.HydrologyGradientStabilityBlend;
            int gradientStabilityIterations = generationConfig.Water.HydrologyGradientStabilityIterations;
            double gradientClamp = generationConfig.Water.HydrologyGradientClamp;
            string protoBaseline = ProtoFingerprint.DescriptorFingerprint;
            string protoComputed = ProtoFingerprint.ComputeFingerprint();

            return $"{PipelineVersion}:{generationConfig.WorldName}:{seed}:{protoBaseline}:{protoComputed}:{generationConfig.MapControlProfileVersion}:{controlProfile?.ProfileHash ?? "no-profile"}:{controlProfile?.HydrologySignature ?? SharedFeatureCatalog.HydrologySignature}:{controlProfile?.Version ?? 0}:{generationConfig.ChunkSize}:{generationConfig.WorldHeight}:{generationConfig.RenderDistance}:{generationConfig.SimulationDistance}:{generationConfig.Water.GlobalWaterLevel}:{generationConfig.TerrainGeneration.SeaLevel}:{generationConfig.Water.HydrologyFlowPersistence}:{generationConfig.Water.HydrologyFlowGain}:{generationConfig.Water.HydrologyWatershedStitchWeight}:{generationConfig.Water.HydrologyWatershedStitchRadius}:{gradientStabilityIterations}:{gradientStabilityBlend}:{gradientClamp}:{generationConfig.Water.HydrologyWaterTableClampWeight}:{generationConfig.Water.HydrologyWaterTableClampRange}:{generationConfig.Water.HydrologyWaterTableSlopeWeight}:{generationConfig.Lakes.MinDepth}:{generationConfig.Lakes.MaxDepth}:{generationConfig.Lakes.ShelfDepth}:{generationConfig.Lakes.FlowSeepageWeight}:{generationConfig.Caves.CeilingMoistureWeight}:{generationConfig.Caves.CeilingMoistureClamp}:{generationConfig.Caves.FloodedCaveNoiseFrequency}:{generationConfig.Caves.FloodedCaveThreshold}:{generationConfig.Caves.FloodedCaveProximityToWaterTableWeight}:{generationConfig.Caves.WaterThreshold}:{generationConfig.Caves.LavaThreshold}:{generationConfig.Water.HydrologyEdgeBlendRadius}:{generationConfig.Water.HydrologyEdgeVarianceClamp}:{generationConfig.Water.HydrologyEdgeNormalizationBlend}:{generationConfig.Water.HydrologyEdgeNormalizationIterations}:{generationConfig.Water.HydrologyFlowMemoryWeight}:{generationConfig.Water.HydrologyContinuityWeight}:{generationConfig.Water.RiverMeanderJitter}:{generationConfig.Water.RiverReliefPenaltyWeight}:{generationConfig.Lakes.VarianceWeight}:{generationConfig.Lakes.OutflowStabilityWeight}:{generationConfig.Water.HydrologyFlowShadowWeight}:{generationConfig.Water.HydrologyFlowShadowSlopeWeight}:{generationConfig.Lakes.WetlandBufferRadius}:{generationConfig.Water.LakeInflowBlendWeight}:{generationConfig.Water.HydrologyVarianceBlend}:{generationConfig.Water.HydrologyVarianceClamp}:{generationConfig.Water.HydrologyEdgeStabilityIterations}:{generationConfig.Water.HydrologyEdgeStabilityWeight}:{generationConfig.Water.HydrologyEdgeFlowLockWeight}:{generationConfig.Water.HydrologyEdgeFlowBias}:{generationConfig.Water.HydrologyEdgeTangentWeight}:{generationConfig.Water.HydrologyEdgeFluxBlend}:{generationConfig.Water.HydrologyDirectionalBlend}:{generationConfig.Water.HydrologyDirectionalIterations}:{generationConfig.Water.HydrologyFlowDivergenceClamp}:{generationConfig.Water.HydrologySeamRelaxBlend}:{generationConfig.Water.HydrologySeamRelaxIterations}:{generationConfig.Caves.EdgeSealStrength}:{generationConfig.Caves.SupportDensity}:{generationConfig.Caves.SupportPillarChance}:{generationConfig.Lakes.RiverProximitySuppression}:{worldConfigHash}:{profileContentHash}";
        }
    }

    public enum WorldMapRequestType
    {
        GetInitialMap,
        UpdateChunk,
        GetPlayerProfile,
        UpdatePlayerProfile
    }

    public enum ProfileUpdateType
    {
        RenderDistance,
        MapScale,
        ShowCoordinates,
        ShowBiomeInfo
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
