using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using GameCommon.World;
using Minecraft.Core;
using SharedProtocol.EnhancedMinecraft;
using UnityEngine;

namespace GameWorld
{
    /// <summary>
    /// Unity-side world map controller that mirrors the server map-control profile.
    /// Generates local preview chunks (height, caves, rivers, lakes) using the JSON profile.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private string profileFileName = "world-map-control.json";
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private float profileReloadIntervalSeconds = 5f;

        [Header("Streaming")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private int viewRadiusChunks = 4;
        [SerializeField] private int maxConcurrentChunkBuilds = 4;
        [SerializeField] private int maxQueuedChunkRequests = 1024;
        [SerializeField] private int maxLoadedPreviewChunks = 2048;
        [SerializeField] private int queuePressureFactor = 2;
        [SerializeField] private float queueSlackRatio = 2.0f;
        [SerializeField] private float queueBurstSlackMultiplier = 1.15f;
        [SerializeField] private float queueLoadSheddingThreshold = 0.88f;
        [SerializeField] private float queueEmergencyBrakeThreshold = 1.15f;
        [SerializeField] private float queueLoadEmaBlend = 0.18f;
        [SerializeField] private float queueEmergencyReleaseRatio = 0.84f;
        [SerializeField] private float queueTrendBoostWeight = 0.22f;
        [SerializeField] private float queueShockAbsorberWeight = 0.24f;
        [SerializeField] private float queueAlluvialRelayWeight = 0.82f;
        [SerializeField] private float queueKarstSpillwayWeight = 0.9f;
        [SerializeField] private int queueOverloadDrainFactor = 2;
        [SerializeField] private int queueBackoffDelayMs = 4;
        [SerializeField] private int queueEmergencyHoldTicks = 8;
        [SerializeField] private int queueRecoveryRampTicks = 10;
        [SerializeField] private int queueNearChunkKeepCount = 24;
        [SerializeField] private int queueRequestTtlSeconds = 45;
        [SerializeField] private float queueHotspotBias = 0.42f;
        [SerializeField] private float queueHotspotEmergencyPenalty = 1.0f;
        [SerializeField] private int queueHotspotRetentionSeconds = 18;
        [SerializeField] private int queueStalePruneMax = 96;
        [SerializeField] private float queueStalePruneEmergencyMultiplier = 1.35f;
        [SerializeField] private string runtimeControlConfigFileName = "enhanced_world_map_control_client.json";
        [SerializeField] private string queuePolicyFileName = "world_map_control_queue_policy.json";

        private const string PipelineVersion = SharedFeatureCatalog.HydrologySignature;
        private WorldMapControlProfile profile = null!;
        private EnhancedTerrainGenerator generator = null!;
        private CancellationTokenSource cancellation = null!;
        private SemaphoreSlim buildSemaphore = null!;
        private DateTime lastProfileCheckUtc;
        private DateTime lastProfileWriteUtc;
        private DateTime lastConfigWriteUtc;
        private string configPath = null!;
        private WorldConfig worldConfig = null!;
        private string lastProfileHash = string.Empty;
        private string lastProfileSignature = string.Empty;
        private string lastProfileFileHash = string.Empty;
        private float queueLoadEma;
        private bool queueEmergencyBrakeLatched;
        private int queueEmergencyHoldTicksRemaining;
        private int queueRecoveryRampTicksRemaining;

        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, byte> queuedChunks = new();
        private readonly ConcurrentDictionary<Vector2Int, long> queuedChunkEnqueueTicks = new();
        private readonly ConcurrentDictionary<Vector2Int, byte> buildingChunks = new();
        private readonly ConcurrentQueue<Vector2Int> requestQueue = new();
        private int queuedRequestCount;

        private void Awake()
        {
            ProtoRuntime.EnsureInitialized();
            ProtoDiagnostics.AssertFingerprint();
            LoadProfile();
            ApplyRuntimeStreamingOverrides();
            ApplySharedQueuePolicyOverrides();
            configPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
            lastConfigWriteUtc = File.Exists(configPath) ? File.GetLastWriteTimeUtc(configPath) : DateTime.MinValue;
            worldConfig = WorldConfig.Instance;
            generator = new EnhancedTerrainGenerator(profile, worldConfig);
            lastProfileCheckUtc = DateTime.UtcNow;
            queueLoadEma = 0f;
            queueEmergencyBrakeLatched = false;
            queueEmergencyHoldTicksRemaining = 0;
            queueRecoveryRampTicksRemaining = 0;
            cancellation = new CancellationTokenSource();
            buildSemaphore = new SemaphoreSlim(Math.Max(1, maxConcurrentChunkBuilds));
            _ = ProcessQueueAsync(cancellation.Token);
        }

        private void ApplyRuntimeStreamingOverrides()
        {
            if (string.IsNullOrWhiteSpace(runtimeControlConfigFileName))
            {
                return;
            }

            var runtimePath = Path.Combine(Application.streamingAssetsPath, runtimeControlConfigFileName);
            if (!File.Exists(runtimePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(runtimePath);
                var runtime = JsonUtility.FromJson<ClientRuntimeRoot>(json);
                if (runtime?.worldMapControl == null)
                {
                    return;
                }

                if (runtime.worldMapControl.defaults != null && runtime.worldMapControl.defaults.renderDistance > 0)
                {
                    viewRadiusChunks = Mathf.Clamp(runtime.worldMapControl.defaults.renderDistance, 1, 16);
                }

                if (runtime.worldMapControl.defaults != null && runtime.worldMapControl.defaults.maxLoadedPreviewChunks > 0)
                {
                    maxLoadedPreviewChunks = Mathf.Clamp(runtime.worldMapControl.defaults.maxLoadedPreviewChunks, 64, 8192);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.maxConcurrentChunkRequests > 0)
                {
                    maxConcurrentChunkBuilds = Mathf.Clamp(runtime.worldMapControl.performance.maxConcurrentChunkRequests, 1, 64);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.maxQueuedChunkRequests > 0)
                {
                    maxQueuedChunkRequests = Mathf.Clamp(runtime.worldMapControl.performance.maxQueuedChunkRequests, 64, 16384);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queuePressureFactor > 0)
                {
                    queuePressureFactor = Mathf.Clamp(runtime.worldMapControl.performance.queuePressureFactor, 1, 8);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueSlackRatio > 0f)
                {
                    queueSlackRatio = Mathf.Clamp(runtime.worldMapControl.performance.queueSlackRatio, 1.1f, 6.0f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueBurstSlackMultiplier > 0f)
                {
                    queueBurstSlackMultiplier = Mathf.Clamp(runtime.worldMapControl.performance.queueBurstSlackMultiplier, 1.0f, 3.0f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueLoadSheddingThreshold > 0f)
                {
                    queueLoadSheddingThreshold = Mathf.Clamp(runtime.worldMapControl.performance.queueLoadSheddingThreshold, 0.5f, 0.98f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueEmergencyBrakeThreshold > 0f)
                {
                    queueEmergencyBrakeThreshold = Mathf.Clamp(runtime.worldMapControl.performance.queueEmergencyBrakeThreshold, 0.75f, 4.0f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueLoadEmaBlend > 0f)
                {
                    queueLoadEmaBlend = Mathf.Clamp(runtime.worldMapControl.performance.queueLoadEmaBlend, 0.05f, 0.65f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueEmergencyReleaseRatio > 0f)
                {
                    queueEmergencyReleaseRatio = Mathf.Clamp(runtime.worldMapControl.performance.queueEmergencyReleaseRatio, 0.5f, 0.99f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueTrendBoostWeight > 0f)
                {
                    queueTrendBoostWeight = Mathf.Clamp(runtime.worldMapControl.performance.queueTrendBoostWeight, 0.0f, 1.5f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueShockAbsorberWeight > 0f)
                {
                    queueShockAbsorberWeight = Mathf.Clamp(runtime.worldMapControl.performance.queueShockAbsorberWeight, 0.0f, 1.5f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueAlluvialRelayWeight > 0f)
                {
                    queueAlluvialRelayWeight = Mathf.Clamp(runtime.worldMapControl.performance.queueAlluvialRelayWeight, 0.0f, 1.5f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueKarstSpillwayWeight > 0f)
                {
                    queueKarstSpillwayWeight = Mathf.Clamp(runtime.worldMapControl.performance.queueKarstSpillwayWeight, 0.0f, 1.5f);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueOverloadDrainFactor > 0)
                {
                    queueOverloadDrainFactor = Mathf.Clamp(runtime.worldMapControl.performance.queueOverloadDrainFactor, 1, 16);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueBackoffDelayMs > 0)
                {
                    queueBackoffDelayMs = Mathf.Clamp(runtime.worldMapControl.performance.queueBackoffDelayMs, 1, 200);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueEmergencyHoldTicks > 0)
                {
                    queueEmergencyHoldTicks = Mathf.Clamp(runtime.worldMapControl.performance.queueEmergencyHoldTicks, 1, 128);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueRecoveryRampTicks > 0)
                {
                    queueRecoveryRampTicks = Mathf.Clamp(runtime.worldMapControl.performance.queueRecoveryRampTicks, 1, 256);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueNearChunkKeepCount > 0)
                {
                    queueNearChunkKeepCount = Mathf.Clamp(runtime.worldMapControl.performance.queueNearChunkKeepCount, 8, 512);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueHotspotBias > 0f)
                {
                    queueHotspotBias = (float)WorldMapQueuePolicy.ClampHotspotBias(runtime.worldMapControl.performance.queueHotspotBias, queueHotspotBias);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueHotspotEmergencyPenalty > 0f)
                {
                    queueHotspotEmergencyPenalty = (float)WorldMapQueuePolicy.ClampHotspotEmergencyPenalty(runtime.worldMapControl.performance.queueHotspotEmergencyPenalty, queueHotspotEmergencyPenalty);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueHotspotRetentionSeconds > 0)
                {
                    queueHotspotRetentionSeconds = Mathf.Clamp(runtime.worldMapControl.performance.queueHotspotRetentionSeconds, 5, 300);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueRequestTtlSeconds > 0)
                {
                    queueRequestTtlSeconds = Mathf.Clamp(runtime.worldMapControl.performance.queueRequestTtlSeconds, 5, 600);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueStalePruneMax > 0)
                {
                    queueStalePruneMax = Mathf.Clamp(runtime.worldMapControl.performance.queueStalePruneMax, 8, 512);
                }

                if (runtime.worldMapControl.performance != null && runtime.worldMapControl.performance.queueStalePruneEmergencyMultiplier > 0f)
                {
                    queueStalePruneEmergencyMultiplier = Mathf.Clamp(runtime.worldMapControl.performance.queueStalePruneEmergencyMultiplier, 1f, 3f);
                }

                if (enableDebugLogging)
                {
                    Debug.Log(
                        $"[WorldMapController] Applied runtime streaming config from {runtimePath} " +
                        $"(viewRadiusChunks={viewRadiusChunks}, maxConcurrentChunkBuilds={maxConcurrentChunkBuilds}, " +
                        $"maxQueuedChunkRequests={maxQueuedChunkRequests}, maxLoadedPreviewChunks={maxLoadedPreviewChunks}, " +
                        $"queuePressureFactor={queuePressureFactor}, queueSlackRatio={queueSlackRatio:F2}, burstSlack={queueBurstSlackMultiplier:F2}, queueLoadSheddingThreshold={queueLoadSheddingThreshold:F2}, emergencyBrake={queueEmergencyBrakeThreshold:F2}, emaBlend={queueLoadEmaBlend:F2}, releaseRatio={queueEmergencyReleaseRatio:F2}, trend={queueTrendBoostWeight:F2}, shock={queueShockAbsorberWeight:F2}, alluvialRelay={queueAlluvialRelayWeight:F2}, karstSpillway={queueKarstSpillwayWeight:F2}, hotspotBias={queueHotspotBias:F2}, hotspotEmergencyPenalty={queueHotspotEmergencyPenalty:F2}, hotspotRetentionSec={queueHotspotRetentionSeconds}, nearKeep={queueNearChunkKeepCount}, drain={queueOverloadDrainFactor}, backoffMs={queueBackoffDelayMs}, holdTicks={queueEmergencyHoldTicks}, recoveryRampTicks={queueRecoveryRampTicks}, queueTtlSec={queueRequestTtlSeconds}, stalePruneMax={queueStalePruneMax}, stalePruneEmergencyMultiplier={queueStalePruneEmergencyMultiplier:F2})");
                }
            }
            catch (Exception ex)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[WorldMapController] Failed to load runtime streaming config '{runtimePath}': {ex.Message}");
                }
            }
        }

        private void ApplySharedQueuePolicyOverrides()
        {
            if (string.IsNullOrWhiteSpace(queuePolicyFileName))
            {
                return;
            }

            var queuePolicyPath = Path.Combine(Application.streamingAssetsPath, queuePolicyFileName);
            if (!File.Exists(queuePolicyPath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(queuePolicyPath);
                var policy = JsonUtility.FromJson<ClientQueuePolicyRoot>(json);
                if (policy?.client == null)
                {
                    return;
                }

                if (policy.client.maxQueuedChunkRequests > 0)
                {
                    maxQueuedChunkRequests = Mathf.Clamp(policy.client.maxQueuedChunkRequests, 64, 16384);
                }

                if (policy.client.queuePressureFactor > 0)
                {
                    queuePressureFactor = Mathf.Clamp(policy.client.queuePressureFactor, 1, 8);
                }

                if (policy.client.queueSlackRatio > 0f)
                {
                    queueSlackRatio = Mathf.Clamp(policy.client.queueSlackRatio, 1.1f, 6.0f);
                }

                if (policy.client.queueBurstSlackMultiplier > 0f)
                {
                    queueBurstSlackMultiplier = Mathf.Clamp(policy.client.queueBurstSlackMultiplier, 1.0f, 3.0f);
                }

                if (policy.client.queueLoadSheddingThreshold > 0f)
                {
                    queueLoadSheddingThreshold = Mathf.Clamp(policy.client.queueLoadSheddingThreshold, 0.5f, 0.98f);
                }

                if (policy.client.queueEmergencyBrakeThreshold > 0f)
                {
                    queueEmergencyBrakeThreshold = Mathf.Clamp(policy.client.queueEmergencyBrakeThreshold, 0.75f, 4.0f);
                }

                if (policy.client.queueLoadEmaBlend > 0f)
                {
                    queueLoadEmaBlend = Mathf.Clamp(policy.client.queueLoadEmaBlend, 0.05f, 0.65f);
                }

                if (policy.client.queueEmergencyReleaseRatio > 0f)
                {
                    queueEmergencyReleaseRatio = Mathf.Clamp(policy.client.queueEmergencyReleaseRatio, 0.5f, 0.99f);
                }

                if (policy.client.queueTrendBoostWeight > 0f)
                {
                    queueTrendBoostWeight = Mathf.Clamp(policy.client.queueTrendBoostWeight, 0.0f, 1.5f);
                }

                if (policy.client.queueShockAbsorberWeight > 0f)
                {
                    queueShockAbsorberWeight = Mathf.Clamp(policy.client.queueShockAbsorberWeight, 0.0f, 1.5f);
                }

                if (policy.client.queueAlluvialRelayWeight > 0f)
                {
                    queueAlluvialRelayWeight = Mathf.Clamp(policy.client.queueAlluvialRelayWeight, 0.0f, 1.5f);
                }

                if (policy.client.queueKarstSpillwayWeight > 0f)
                {
                    queueKarstSpillwayWeight = Mathf.Clamp(policy.client.queueKarstSpillwayWeight, 0.0f, 1.5f);
                }

                if (policy.client.queueOverloadDrainFactor > 0)
                {
                    queueOverloadDrainFactor = Mathf.Clamp(policy.client.queueOverloadDrainFactor, 1, 16);
                }

                if (policy.client.queueBackoffDelayMs > 0)
                {
                    queueBackoffDelayMs = Mathf.Clamp(policy.client.queueBackoffDelayMs, 1, 200);
                }

                if (policy.client.queueEmergencyHoldTicks > 0)
                {
                    queueEmergencyHoldTicks = Mathf.Clamp(policy.client.queueEmergencyHoldTicks, 1, 128);
                }

                if (policy.client.queueRecoveryRampTicks > 0)
                {
                    queueRecoveryRampTicks = Mathf.Clamp(policy.client.queueRecoveryRampTicks, 1, 256);
                }

                if (policy.client.queueNearChunkKeepCount > 0)
                {
                    queueNearChunkKeepCount = Mathf.Clamp(policy.client.queueNearChunkKeepCount, 8, 512);
                }

                if (policy.client.queueHotspotBias > 0f)
                {
                    queueHotspotBias = (float)WorldMapQueuePolicy.ClampHotspotBias(policy.client.queueHotspotBias, queueHotspotBias);
                }

                if (policy.client.queueHotspotEmergencyPenalty > 0f)
                {
                    queueHotspotEmergencyPenalty = (float)WorldMapQueuePolicy.ClampHotspotEmergencyPenalty(policy.client.queueHotspotEmergencyPenalty, queueHotspotEmergencyPenalty);
                }

                if (policy.client.queueHotspotRetentionSeconds > 0)
                {
                    queueHotspotRetentionSeconds = Mathf.Clamp(policy.client.queueHotspotRetentionSeconds, 5, 300);
                }

                if (policy.client.maxLoadedPreviewChunks > 0)
                {
                    maxLoadedPreviewChunks = Mathf.Clamp(policy.client.maxLoadedPreviewChunks, 64, 8192);
                }

                if (policy.client.maxConcurrentChunkRequests > 0)
                {
                    maxConcurrentChunkBuilds = Mathf.Clamp(policy.client.maxConcurrentChunkRequests, 1, 64);
                }

                if (policy.client.queueRequestTtlSeconds > 0)
                {
                    queueRequestTtlSeconds = Mathf.Clamp(policy.client.queueRequestTtlSeconds, 5, 600);
                }

                if (policy.client.queueStalePruneMax > 0)
                {
                    queueStalePruneMax = Mathf.Clamp(policy.client.queueStalePruneMax, 8, 512);
                }

                if (policy.client.queueStalePruneEmergencyMultiplier > 0f)
                {
                    queueStalePruneEmergencyMultiplier = Mathf.Clamp(policy.client.queueStalePruneEmergencyMultiplier, 1f, 3f);
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] Applied shared queue policy from {queuePolicyPath} (queue={maxQueuedChunkRequests}, pressure={queuePressureFactor}, slack={queueSlackRatio:F2}, burstSlack={queueBurstSlackMultiplier:F2}, shed={queueLoadSheddingThreshold:F2}, emergencyBrake={queueEmergencyBrakeThreshold:F2}, emaBlend={queueLoadEmaBlend:F2}, releaseRatio={queueEmergencyReleaseRatio:F2}, trend={queueTrendBoostWeight:F2}, shock={queueShockAbsorberWeight:F2}, alluvialRelay={queueAlluvialRelayWeight:F2}, karstSpillway={queueKarstSpillwayWeight:F2}, hotspotBias={queueHotspotBias:F2}, hotspotEmergencyPenalty={queueHotspotEmergencyPenalty:F2}, hotspotRetentionSec={queueHotspotRetentionSeconds}, nearKeep={queueNearChunkKeepCount}, drain={queueOverloadDrainFactor}, backoffMs={queueBackoffDelayMs}, holdTicks={queueEmergencyHoldTicks}, recoveryRampTicks={queueRecoveryRampTicks}, queueTtlSec={queueRequestTtlSeconds}, stalePruneMax={queueStalePruneMax}, stalePruneEmergencyMultiplier={queueStalePruneEmergencyMultiplier:F2}, loaded={maxLoadedPreviewChunks}, concurrent={maxConcurrentChunkBuilds})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorldMapController] Failed to apply queue policy '{queuePolicyPath}': {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            buildSemaphore?.Dispose();
            queuedChunks.Clear();
            queuedChunkEnqueueTicks.Clear();
            buildingChunks.Clear();
            loadedChunks.Clear();
            while (requestQueue.TryDequeue(out _)) { }
            Interlocked.Exchange(ref queuedRequestCount, 0);
            queueLoadEma = 0f;
            queueEmergencyBrakeLatched = false;
            queueEmergencyHoldTicksRemaining = 0;
            queueRecoveryRampTicksRemaining = 0;
        }

        private void Update()
        {
            if (playerTransform == null || profile == null)
            {
                return;
            }

            MaybeReloadProfile();
            EnqueueAroundPlayer();
            UnloadDistantChunks();
        }

        private void LoadProfile()
        {
            var profilePath = Path.Combine(Application.streamingAssetsPath, profileFileName);
            profile = WorldMapControlProfile.LoadFromFile(profilePath, WorldConfig.Instance);
            lastProfileWriteUtc = File.Exists(profilePath) ? File.GetLastWriteTimeUtc(profilePath) : DateTime.MinValue;
            lastProfileHash = profile.ProfileHash;
            lastProfileFileHash = ComputeFileHash(profilePath);
            lastProfileSignature = ComputeGenerationSignature(profile, worldConfig ?? WorldConfig.Instance);

            if (!string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[WorldMapController] Hydrology signature mismatch (profile={profile.HydrologySignature}, expected={SharedFeatureCatalog.HydrologySignature}), rebuilding from config.");
                }

                profile = WorldMapControlProfile.FromConfig(WorldConfig.Instance);
                lastProfileHash = profile.ProfileHash;
                lastProfileSignature = ComputeGenerationSignature(profile, worldConfig ?? WorldConfig.Instance);
            }

            if (profile.Version < SharedFeatureCatalog.MapControlProfileVersion)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[WorldMapController] Profile version mismatch (profile={profile.Version}, required={SharedFeatureCatalog.MapControlProfileVersion}), rebuilding from config.");
                }

                profile = WorldMapControlProfile.FromConfig(WorldConfig.Instance);
                lastProfileHash = profile.ProfileHash;
                lastProfileSignature = ComputeGenerationSignature(profile, worldConfig ?? WorldConfig.Instance);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapController] Loaded profile hash={profile.ProfileHash} from {profilePath}");
            }
        }

        private void MaybeReloadProfile()
        {
            if (profileReloadIntervalSeconds <= 0f)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - lastProfileCheckUtc).TotalSeconds < profileReloadIntervalSeconds)
            {
                return;
            }

            var profilePath = Path.Combine(Application.streamingAssetsPath, profileFileName);
            lastProfileCheckUtc = now;
            bool generatorReloaded = false;
            if (!string.IsNullOrEmpty(configPath))
            {
                var configWrite = File.Exists(configPath) ? File.GetLastWriteTimeUtc(configPath) : DateTime.MinValue;
                if (configWrite > lastConfigWriteUtc)
                {
                    WorldConfig.ForceReload();
                    worldConfig = WorldConfig.Instance;
                    lastConfigWriteUtc = configWrite;
                    var expectedProfile = WorldMapControlProfile.FromConfig(worldConfig);
                    bool profileStale = !string.Equals(expectedProfile.ProfileHash, lastProfileHash, StringComparison.OrdinalIgnoreCase);
                    bool profileOlderThanConfig = File.Exists(profilePath) && File.GetLastWriteTimeUtc(profilePath) < configWrite;

                    profile = profileStale || profileOlderThanConfig ? expectedProfile : profile;
                    lastProfileHash = profile.ProfileHash;
                    lastProfileFileHash = ComputeFileHash(profilePath);
                    lastProfileSignature = ComputeGenerationSignature(profile, worldConfig);
                    generator = new EnhancedTerrainGenerator(profile, worldConfig);
                    loadedChunks.Clear();
                    queuedChunks.Clear();
                    buildingChunks.Clear();
                    while (requestQueue.TryDequeue(out _)) { }
                    Interlocked.Exchange(ref queuedRequestCount, 0);
                    queueLoadEma = 0f;
                    queueEmergencyBrakeLatched = false;
                    queueEmergencyHoldTicksRemaining = 0;
                    queueRecoveryRampTicksRemaining = 0;
                    generatorReloaded = true;
                    if (enableDebugLogging)
                    {
                        Debug.Log(profileStale || profileOlderThanConfig
                            ? $"[WorldMapController] Reloaded world-config and rebuilt profile hash={profile.ProfileHash} (config updated {configWrite:o})"
                            : $"[WorldMapController] Reloaded world-config and generator (updated {configWrite:o})");
                    }
                }
            }

            try
            {
                if (!File.Exists(profilePath))
                {
                    return;
                }

                var writeTime = File.GetLastWriteTimeUtc(profilePath);
                var fileHash = ComputeFileHash(profilePath);
                bool profileContentChanged = !string.IsNullOrEmpty(fileHash) &&
                                             !string.Equals(fileHash, lastProfileFileHash, StringComparison.OrdinalIgnoreCase);
                if (writeTime <= lastProfileWriteUtc && !profileContentChanged)
                {
                    return;
                }

                var newProfile = WorldMapControlProfile.LoadFromFile(profilePath, WorldConfig.Instance);
                bool signatureMismatch = !string.Equals(newProfile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase);
                bool versionMismatch = newProfile.Version < SharedFeatureCatalog.MapControlProfileVersion;
                if (signatureMismatch)
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"[WorldMapController] Profile hydrology signature drifted (profile={newProfile.HydrologySignature}, expected={SharedFeatureCatalog.HydrologySignature}); rebuilding from config.");
                    }

                    newProfile = WorldMapControlProfile.FromConfig(worldConfig ?? WorldConfig.Instance);
                    profileContentChanged = true;
                }

                if (versionMismatch)
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"[WorldMapController] Profile version drifted (profile={newProfile.Version}, required={SharedFeatureCatalog.MapControlProfileVersion}); rebuilding from config.");
                    }

                    newProfile = WorldMapControlProfile.FromConfig(worldConfig ?? WorldConfig.Instance);
                    profileContentChanged = true;
                }

                if (!string.Equals(newProfile.ProfileHash, profile.ProfileHash, StringComparison.OrdinalIgnoreCase) || profileContentChanged)
                {
                    profile = newProfile;
                    generator = new EnhancedTerrainGenerator(profile, worldConfig);
                    loadedChunks.Clear();
                    queuedChunks.Clear();
                    buildingChunks.Clear();
                    while (requestQueue.TryDequeue(out _)) { }
                    Interlocked.Exchange(ref queuedRequestCount, 0);
                    queueLoadEma = 0f;
                    queueEmergencyBrakeLatched = false;
                    queueEmergencyHoldTicksRemaining = 0;
                    queueRecoveryRampTicksRemaining = 0;
                    lastProfileHash = profile.ProfileHash;
                    lastProfileFileHash = fileHash;
                    lastProfileSignature = ComputeGenerationSignature(profile, worldConfig);
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Reloaded profile hash={profile.ProfileHash} (updated {writeTime:o})");
                    }
                }
                else if (generatorReloaded && enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] World config changed; reused profile hash={profile.ProfileHash}");
                }

                lastProfileWriteUtc = writeTime;
                lastProfileFileHash = string.IsNullOrEmpty(fileHash) ? lastProfileFileHash : fileHash;
            }
            catch (Exception ex)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[WorldMapController] Failed to reload profile: {ex.Message}");
                }
            }

            var generationSignature = ComputeGenerationSignature(profile, worldConfig);
            if (!string.Equals(generationSignature, lastProfileSignature, StringComparison.Ordinal))
            {
                lastProfileSignature = generationSignature;
                generator = new EnhancedTerrainGenerator(profile, worldConfig);
                loadedChunks.Clear();
                queuedChunks.Clear();
                queuedChunkEnqueueTicks.Clear();
                buildingChunks.Clear();
                while (requestQueue.TryDequeue(out _)) { }
                Interlocked.Exchange(ref queuedRequestCount, 0);
                queueLoadEma = 0f;
                queueEmergencyBrakeLatched = false;
                queueEmergencyHoldTicksRemaining = 0;
                queueRecoveryRampTicksRemaining = 0;
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] Regenerated map preview generator for signature={generationSignature}");
                }
            }
        }

        private void EnqueueAroundPlayer()
        {
            var playerChunk = WorldToChunk(playerTransform.position);
            float load = ComputeEffectiveQueueLoad(Mathf.Max(64, GetDynamicLoadedChunkBudget()));
            QueuePressureBand pressureBand = WorldMapQueuePolicy.ClassifyBand(load);
            var candidates = WorldMapQueuePolicy.EnumerateByDistance(playerChunk.x, playerChunk.y, viewRadiusChunks);
            var prioritizedChunks = WorldMapQueuePolicy.PrioritizeByDistance(
                playerChunk.x,
                playerChunk.y,
                candidates,
                0,
                pressureBand,
                queueEmergencyBrakeLatched);
            foreach (var chunk in prioritizedChunks)
            {
                var pos = new Vector2Int(chunk.X, chunk.Z);
                if (loadedChunks.ContainsKey(pos))
                {
                    continue;
                }

                EnqueueChunk(pos);
            }
        }

        private void EnqueueChunk(Vector2Int pos)
        {
            if (loadedChunks.ContainsKey(pos) || buildingChunks.ContainsKey(pos))
            {
                return;
            }

            if (!queuedChunks.TryAdd(pos, 0))
            {
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            queuedChunkEnqueueTicks[pos] = nowTicks;

            if (Volatile.Read(ref queuedRequestCount) >= Math.Max(64, maxQueuedChunkRequests))
            {
                queuedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
                return;
            }

            int pressureLimit = GetAdaptiveQueueLimit();
            int pending = loadedChunks.Count + buildingChunks.Count + Volatile.Read(ref queuedRequestCount);
            if (pending >= pressureLimit)
            {
                DrainStaleQueueEntries();
                queuedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
                return;
            }

            float pendingLoad = ComputeEffectiveQueueLoad(Mathf.Max(64, GetDynamicLoadedChunkBudget()));
            QueuePressureBand enqueueBand = WorldMapQueuePolicy.ClassifyBand(pendingLoad);
            bool hotspotProtected = IsHotspotChunk(pos, enqueueBand, pendingLoad) && IsWithinHotspotRetentionWindow(pos, nowTicks);
            if (!hotspotProtected &&
                pendingLoad >= Mathf.Clamp(queueLoadSheddingThreshold, 0.5f, 0.98f) &&
                IsFarChunkFromPlayer(pos, enqueueBand))
            {
                DrainStaleQueueEntries();
                queuedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
                return;
            }

            if (queueEmergencyBrakeLatched && !hotspotProtected)
            {
                DrainStaleQueueEntries(forceAggressive: true);
                queuedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
                return;
            }

            requestQueue.Enqueue(pos);
            Interlocked.Increment(ref queuedRequestCount);
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!requestQueue.TryDequeue(out var pos))
                {
                    await Task.Delay(Mathf.Max(1, queueBackoffDelayMs), token);
                    continue;
                }

                if (Volatile.Read(ref queuedRequestCount) > 0)
                {
                    Interlocked.Decrement(ref queuedRequestCount);
                }

                queuedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);

                long nowTicks = DateTime.UtcNow.Ticks;
                if (IsQueueEntryExpired(pos, nowTicks))
                {
                    continue;
                }

                int pendingPressure = loadedChunks.Count + buildingChunks.Count + Mathf.Max(0, Volatile.Read(ref queuedRequestCount));
                int pendingLimit = GetAdaptiveQueueLimit();
                if (pendingPressure > pendingLimit)
                {
                    DrainStaleQueueEntries();
                    await Task.Delay(Mathf.Max(1, queueBackoffDelayMs * GetAdaptiveQueuePressureFactor()), token);
                }

                float pendingLoad = ComputeEffectiveQueueLoad(Mathf.Max(64, GetDynamicLoadedChunkBudget()));
                QueuePressureBand processingBand = WorldMapQueuePolicy.ClassifyBand(pendingLoad);
                bool hotspotProtected = IsHotspotChunk(pos, processingBand, pendingLoad) && IsWithinHotspotRetentionWindow(pos, nowTicks);
                if (!hotspotProtected &&
                    pendingLoad >= Mathf.Clamp(queueLoadSheddingThreshold, 0.5f, 0.98f) &&
                    IsFarChunkFromPlayer(pos, processingBand))
                {
                    continue;
                }

                if (queueEmergencyBrakeLatched && !hotspotProtected)
                {
                    DrainStaleQueueEntries(forceAggressive: true);
                    await Task.Delay(Mathf.Max(1, queueBackoffDelayMs * (GetAdaptiveQueuePressureFactor() + 1)), token);
                    continue;
                }

                if (loadedChunks.ContainsKey(pos) || !buildingChunks.TryAdd(pos, 0))
                {
                    continue;
                }

                await buildSemaphore.WaitAsync(token);
                try
                {
                    var chunk = await generator.GenerateChunkAsync(pos, token);
                    loadedChunks[pos] = chunk;
                    EnforceLoadedChunkBudget();
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Built preview chunk {pos}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldMapController] Failed to build chunk {pos}: {ex.Message}");
                }
                finally
                {
                    buildingChunks.TryRemove(pos, out _);
                    buildSemaphore.Release();
                }
            }
        }

        private void UnloadDistantChunks()
        {
            if (playerTransform == null)
            {
                return;
            }

            var playerChunk = WorldToChunk(playerTransform.position);
            var maxDistance = viewRadiusChunks + 2;
            var removal = new List<Vector2Int>();

            foreach (var kvp in loadedChunks)
            {
                var pos = kvp.Key;
                if (Mathf.Abs(pos.x - playerChunk.x) > maxDistance || Mathf.Abs(pos.y - playerChunk.y) > maxDistance)
                {
                    removal.Add(pos);
                }
            }

            foreach (var pos in removal)
            {
                loadedChunks.TryRemove(pos, out _);
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] Unloaded preview chunk {pos}");
                }
            }
        }

        private void EnforceLoadedChunkBudget()
        {
            int budget = GetDynamicLoadedChunkBudget();
            if (loadedChunks.Count <= budget)
            {
                return;
            }

            var center = playerTransform != null ? WorldToChunk(playerTransform.position) : Vector2Int.zero;
            while (loadedChunks.Count > budget)
            {
                bool found = false;
                int farthestDistance = -1;
                Vector2Int farthest = Vector2Int.zero;

                foreach (var kvp in loadedChunks)
                {
                    int distance = Mathf.Abs(kvp.Key.x - center.x) + Mathf.Abs(kvp.Key.y - center.y);
                    if (distance <= farthestDistance)
                    {
                        continue;
                    }

                    farthestDistance = distance;
                    farthest = kvp.Key;
                    found = true;
                }

                if (!found || !loadedChunks.TryRemove(farthest, out _))
                {
                    break;
                }
            }
        }

        private int GetDynamicLoadedChunkBudget()
        {
            int profileRender = profile != null ? Math.Max(1, profile.RenderDistance) : Math.Max(1, viewRadiusChunks);
            int profileSimulation = profile != null ? Math.Max(1, profile.SimulationDistance) : profileRender;
            int renderWindow = (profileRender * 2 + 1) * (profileRender * 2 + 1);
            int simulationWindow = (profileSimulation * 2 + 1) * (profileSimulation * 2 + 1);
            int profileBudget = Math.Max(renderWindow, simulationWindow);
            int baseBudget = Math.Clamp(Math.Max(Math.Max(64, maxLoadedPreviewChunks), profileBudget), 64, 8192);
            int inFlight = buildingChunks.Count + Mathf.Max(0, Volatile.Read(ref queuedRequestCount));
            int pressureBoost = Mathf.Min(Math.Max(64, baseBudget / 4), inFlight);
            return Math.Clamp(baseBudget + pressureBoost, 64, 8192);
        }

        private float ComputeHydrologyQueueScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float thalwegWeight = profile != null ? profile.HydrologyThalwegStabilityWeight : 0.45f;
            return (float)WorldMapQueuePolicy.ComputeHydrologyQueueStabilityScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                thalwegWeight,
                Mathf.Clamp(queueBurstSlackMultiplier, 1.0f, 3.0f),
                queueEmergencyBrakeLatched,
                0.6,
                1.18);
        }

        private float ComputeHydrologySeamResilienceScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float seamRelaxBlend = profile != null ? (float)profile.HydrologySeamRelaxBlend : 0.5f;
            float edgeFluxBlend = profile != null ? (float)profile.HydrologyEdgeFluxBlend : 0.5f;
            return (float)WorldMapQueuePolicy.ComputeHydrologySeamResilienceScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                seamRelaxBlend,
                edgeFluxBlend,
                queueEmergencyBrakeLatched,
                0.62,
                1.2);
        }

        private float ComputeAlluvialAquiferRelayScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float seamRelaxBlend = profile != null ? (float)profile.HydrologySeamRelaxBlend : 0.5f;
            float edgeFluxBlend = profile != null ? (float)profile.HydrologyEdgeFluxBlend : 0.5f;
            float flowPersistence = profile != null ? profile.HydrologyFlowPersistence : 0.8f;
            float aquiferConnectivity = worldConfig != null ? worldConfig.Caves.GroundwaterConnectivityWeight : 0.75f;
            float rechargeSignal = worldConfig != null ? worldConfig.Lakes.FlowSeepageWeight : 0.65f;
            float alluvialWeight = Mathf.Clamp(queueAlluvialRelayWeight, 0.0f, 1.5f);
            return (float)WorldMapQueuePolicy.ComputeAlluvialAquiferRelayScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                seamRelaxBlend,
                edgeFluxBlend,
                flowPersistence * alluvialWeight,
                aquiferConnectivity * alluvialWeight,
                rechargeSignal * alluvialWeight,
                queueEmergencyBrakeLatched,
                0.62,
                1.24);
        }

        private float ComputeKarstFloodplainRelayScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float seamRelaxBlend = profile != null ? (float)profile.HydrologySeamRelaxBlend : 0.5f;
            float edgeFluxBlend = profile != null ? (float)profile.HydrologyEdgeFluxBlend : 0.5f;
            float flowPersistence = profile != null ? profile.HydrologyFlowPersistence : 0.8f;
            float aquiferConnectivity = worldConfig != null ? worldConfig.Caves.GroundwaterConnectivityWeight : 0.75f;
            float spillwayContinuity = worldConfig != null ? worldConfig.Lakes.SpillwayContinuityWeight : 0.7f;
            float caveVentilationBias = worldConfig != null ? worldConfig.Caves.CaveVentilationBias : 0.6f;
            float alluvialWeight = Mathf.Clamp(queueAlluvialRelayWeight, 0.0f, 1.5f);
            return (float)WorldMapQueuePolicy.ComputeKarstFloodplainRelayScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                seamRelaxBlend,
                edgeFluxBlend,
                flowPersistence * alluvialWeight,
                aquiferConnectivity * alluvialWeight,
                spillwayContinuity * alluvialWeight,
                caveVentilationBias * alluvialWeight,
                queueEmergencyBrakeLatched,
                0.62,
                1.26);
        }

        private float ComputeFloodplainSpillwayQueueScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float seamRelaxBlend = profile != null ? (float)profile.HydrologySeamRelaxBlend : 0.5f;
            float edgeFluxBlend = profile != null ? (float)profile.HydrologyEdgeFluxBlend : 0.5f;
            float flowPersistence = profile != null ? profile.HydrologyFlowPersistence : 0.8f;
            float aquiferConnectivity = worldConfig != null ? worldConfig.Caves.GroundwaterConnectivityWeight : 0.75f;
            float spillwayContinuity = worldConfig != null ? worldConfig.Lakes.SpillwayContinuityWeight : 0.7f;
            float spillRetention = worldConfig != null ? worldConfig.Lakes.SpillRetentionWeight : 0.68f;
            float caveVentilationBias = worldConfig != null ? worldConfig.Caves.CaveVentilationBias : 0.6f;
            float alluvialWeight = Mathf.Clamp(queueAlluvialRelayWeight, 0.0f, 1.5f);
            float spillwayWeight = Mathf.Clamp(queueKarstSpillwayWeight, 0.0f, 1.5f);
            return (float)WorldMapQueuePolicy.ComputeFloodplainSpillwayQueueScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                seamRelaxBlend,
                edgeFluxBlend,
                flowPersistence * alluvialWeight,
                aquiferConnectivity * alluvialWeight,
                spillwayContinuity * spillwayWeight,
                spillRetention * spillwayWeight,
                caveVentilationBias * spillwayWeight,
                queueEmergencyBrakeLatched,
                0.62,
                1.28);
        }

        private float ComputeAquiferConduitQueueScale(float effectiveLoad, float loadTrend, float volatilityRatio)
        {
            float continuityWeight = profile != null ? profile.HydrologyContinuityWeight : 0.45f;
            float seamRelaxBlend = profile != null ? (float)profile.HydrologySeamRelaxBlend : 0.5f;
            float edgeFluxBlend = profile != null ? (float)profile.HydrologyEdgeFluxBlend : 0.5f;
            float flowPersistence = profile != null ? profile.HydrologyFlowPersistence : 0.8f;
            float aquiferConnectivity = worldConfig != null ? worldConfig.Caves.GroundwaterConnectivityWeight : 0.75f;
            float spillwayContinuity = worldConfig != null ? worldConfig.Lakes.SpillwayContinuityWeight : 0.7f;
            float spillRetention = worldConfig != null ? worldConfig.Lakes.SpillRetentionWeight : 0.68f;
            float caveVentilationBias = worldConfig != null ? worldConfig.Caves.CaveVentilationBias : 0.6f;
            float alluvialWeight = Mathf.Clamp(queueAlluvialRelayWeight, 0.0f, 1.5f);
            float spillwayWeight = Mathf.Clamp(queueKarstSpillwayWeight, 0.0f, 1.5f);
            return (float)WorldMapQueuePolicy.ComputeAquiferConduitExchangeQueueScale(
                effectiveLoad,
                loadTrend,
                volatilityRatio,
                continuityWeight,
                seamRelaxBlend,
                edgeFluxBlend,
                flowPersistence * alluvialWeight,
                aquiferConnectivity * alluvialWeight,
                spillwayContinuity * spillwayWeight,
                spillRetention * spillwayWeight,
                caveVentilationBias * spillwayWeight,
                queueEmergencyBrakeLatched,
                0.62,
                1.3);
        }

        private float GetAdaptiveQueueSlackRatio()
        {
            int dynamicBudget = Math.Max(64, GetDynamicLoadedChunkBudget());
            float load = ComputeEffectiveQueueLoad(dynamicBudget);
            float loadTrend = (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma);
            float volatilityRatio = (float)WorldMapQueuePolicy.ComputeVolatilityRatio(
                load,
                queueLoadEma,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueTrendBoostWeight,
                queueShockAbsorberWeight);
            float volatilityGuard = (float)WorldMapQueuePolicy.ComputeVolatilityGuardScale(
                volatilityRatio,
                queueEmergencyBrakeLatched,
                0.62,
                1.0);
            float shockScale = (float)WorldMapQueuePolicy.ComputeShockAbsorberScale(
                load,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueShockAbsorberWeight);
            float hydrologyQueueScale = ComputeHydrologyQueueScale(load, loadTrend, volatilityRatio);
            float seamResilienceScale = ComputeHydrologySeamResilienceScale(load, loadTrend, volatilityRatio);
            float alluvialRelayScale = ComputeAlluvialAquiferRelayScale(load, loadTrend, volatilityRatio);
            float karstFloodplainRelayScale = ComputeKarstFloodplainRelayScale(load, loadTrend, volatilityRatio);
            float spillwayQueueScale = ComputeFloodplainSpillwayQueueScale(load, loadTrend, volatilityRatio);
            float aquiferConduitQueueScale = ComputeAquiferConduitQueueScale(load, loadTrend, volatilityRatio);
            float combinedHydrologyScale = Mathf.Clamp(
                hydrologyQueueScale * seamResilienceScale * alluvialRelayScale * karstFloodplainRelayScale * spillwayQueueScale * aquiferConduitQueueScale,
                0.56f,
                1.3f);
            float rawSlack = Mathf.Clamp(
                queueSlackRatio + load * 0.55f + Mathf.Max(0f, loadTrend) * queueTrendBoostWeight * 0.75f,
                Mathf.Max(1.1f, queueSlackRatio),
                6.0f);
            float stabilized = Mathf.Lerp(Mathf.Max(1.1f, queueSlackRatio), rawSlack, shockScale);
            float guarded = Mathf.Lerp(Mathf.Max(1.1f, queueSlackRatio), stabilized, volatilityGuard * combinedHydrologyScale);
            return Mathf.Clamp(guarded, Mathf.Max(1.1f, queueSlackRatio), 6.0f);
        }

        private int GetAdaptiveQueuePressureFactor()
        {
            int dynamicBudget = Math.Max(64, GetDynamicLoadedChunkBudget());
            float load = ComputeEffectiveQueueLoad(dynamicBudget);
            float loadTrend = (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma);
            float volatilityRatio = (float)WorldMapQueuePolicy.ComputeVolatilityRatio(
                load,
                queueLoadEma,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueTrendBoostWeight,
                queueShockAbsorberWeight);
            float hydrologyQueueScale = ComputeHydrologyQueueScale(load, loadTrend, volatilityRatio);
            float seamResilienceScale = ComputeHydrologySeamResilienceScale(load, loadTrend, volatilityRatio);
            float alluvialRelayScale = ComputeAlluvialAquiferRelayScale(load, loadTrend, volatilityRatio);
            float karstFloodplainRelayScale = ComputeKarstFloodplainRelayScale(load, loadTrend, volatilityRatio);
            float spillwayQueueScale = ComputeFloodplainSpillwayQueueScale(load, loadTrend, volatilityRatio);
            float aquiferConduitQueueScale = ComputeAquiferConduitQueueScale(load, loadTrend, volatilityRatio);
            float combinedHydrologyScale = Mathf.Clamp(
                hydrologyQueueScale * seamResilienceScale * alluvialRelayScale * karstFloodplainRelayScale * spillwayQueueScale * aquiferConduitQueueScale,
                0.56f,
                1.3f);
            QueuePressureBand pressureBand = WorldMapQueuePolicy.ClassifyBand(load);
            int adaptive = WorldMapQueuePolicy.ComputeAdaptivePressureFactor(
                Mathf.Clamp(queuePressureFactor, 1, 8),
                pressureBand,
                loadTrend,
                queueEmergencyBrakeLatched,
                Mathf.Clamp(queueTrendBoostWeight, 0.0f, 1.5f),
                1,
                8);
            adaptive = Mathf.Clamp(adaptive + Mathf.CeilToInt(volatilityRatio * 1.5f), 1, 8);
            if (combinedHydrologyScale < 0.9f)
            {
                adaptive = Mathf.Clamp(adaptive + 1, 1, 8);
            }
            return Mathf.Clamp(adaptive, 1, 8);
        }

        private int GetAdaptiveQueueLimit()
        {
            float adaptiveSlack = GetAdaptiveQueueSlackRatio();
            int adaptivePressure = GetAdaptiveQueuePressureFactor();
            float dynamicBudget = Mathf.Max(64, GetDynamicLoadedChunkBudget());
            float load = ComputeEffectiveQueueLoad(Mathf.CeilToInt(dynamicBudget));
            float loadTrend = (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma);
            float volatilityRatio = (float)WorldMapQueuePolicy.ComputeVolatilityRatio(
                load,
                queueLoadEma,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueTrendBoostWeight,
                queueShockAbsorberWeight);
            float volatilityGuard = (float)WorldMapQueuePolicy.ComputeVolatilityGuardScale(
                volatilityRatio,
                queueEmergencyBrakeLatched,
                0.62,
                1.0);
            float shockScale = (float)WorldMapQueuePolicy.ComputeShockAbsorberScale(
                load,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueShockAbsorberWeight);
            float hydrologyQueueScale = ComputeHydrologyQueueScale(load, loadTrend, volatilityRatio);
            float seamResilienceScale = ComputeHydrologySeamResilienceScale(load, loadTrend, volatilityRatio);
            float alluvialRelayScale = ComputeAlluvialAquiferRelayScale(load, loadTrend, volatilityRatio);
            float karstFloodplainRelayScale = ComputeKarstFloodplainRelayScale(load, loadTrend, volatilityRatio);
            float spillwayQueueScale = ComputeFloodplainSpillwayQueueScale(load, loadTrend, volatilityRatio);
            float aquiferConduitQueueScale = ComputeAquiferConduitQueueScale(load, loadTrend, volatilityRatio);
            float combinedHydrologyScale = Mathf.Clamp(
                hydrologyQueueScale * seamResilienceScale * alluvialRelayScale * karstFloodplainRelayScale * spillwayQueueScale * aquiferConduitQueueScale,
                0.56f,
                1.3f);
            bool emergencyBrake = queueEmergencyBrakeLatched;
            float burstMultiplier = !emergencyBrake && load >= 0.9f
                ? 1.0f + (Mathf.Clamp(queueBurstSlackMultiplier, 1.0f, 3.0f) - 1.0f) * shockScale
                : 1.0f;
            int limit = WorldMapQueuePolicy.ComputeQueueLimitFromBudget(
                Mathf.CeilToInt(dynamicBudget),
                Mathf.Max(1, adaptivePressure),
                Mathf.Max(1.1f, adaptiveSlack * Mathf.Max(0.8f, volatilityGuard) * Mathf.Clamp(combinedHydrologyScale, 0.72f, 1.2f)),
                burstMultiplier,
                load,
                emergencyBrake,
                64,
                16384);

            if (emergencyBrake)
            {
                queueRecoveryRampTicksRemaining = Mathf.Clamp(queueRecoveryRampTicks, 1, 256);
            }
            else if (load < Mathf.Clamp(queueLoadSheddingThreshold, 0.5f, 0.98f) * 0.65f)
            {
                int floorLimit = WorldMapQueuePolicy.ComputeQueueLimitFromBudget(
                    Mathf.CeilToInt(dynamicBudget),
                    1,
                    Mathf.Max(1.1f, queueSlackRatio),
                    1.0f,
                    load,
                    emergencyBrake: false,
                    64,
                    16384);
                int recoveryStep = Mathf.Max(8, Mathf.Clamp(queueOverloadDrainFactor, 1, 16) * 6);
                limit = Mathf.Max(floorLimit, limit - recoveryStep);
            }

            if (!emergencyBrake && queueRecoveryRampTicksRemaining > 0)
            {
                int totalRampTicks = Mathf.Clamp(queueRecoveryRampTicks, 1, 256);
                float recoveryScale = (float)WorldMapQueuePolicy.ComputeRecoveryRamp(
                    queueRecoveryRampTicksRemaining,
                    totalRampTicks);
                int floorLimit = WorldMapQueuePolicy.ComputeQueueLimitFromBudget(
                    Mathf.CeilToInt(dynamicBudget),
                    1,
                    Mathf.Max(1.1f, queueSlackRatio),
                    1.0f,
                    load,
                    emergencyBrake: false,
                    64,
                    16384);
                limit = Mathf.Clamp(
                    floorLimit + Mathf.RoundToInt((limit - floorLimit) * recoveryScale),
                    floorLimit,
                    16384);
                queueRecoveryRampTicksRemaining--;
            }

            return limit;
        }

        private float ComputeEffectiveQueueLoad(int dynamicBudget)
        {
            int inFlight = buildingChunks.Count + Mathf.Max(0, Volatile.Read(ref queuedRequestCount));
            float instantLoad = inFlight / Mathf.Max(1f, dynamicBudget);
            float adaptiveEmaBlend = (float)WorldMapQueuePolicy.ComputeAdaptiveEmaBlend(
                WorldMapQueuePolicy.ClampEmaBlend(queueLoadEmaBlend, 0.18),
                instantLoad,
                queueLoadEma,
                queueEmergencyBrakeLatched);
            queueLoadEma = (float)WorldMapQueuePolicy.UpdateEma(queueLoadEma, instantLoad, adaptiveEmaBlend);
            float effectiveLoad = Mathf.Max(instantLoad, queueLoadEma);
            float loadTrend = (float)WorldMapQueuePolicy.ComputeLoadTrend(instantLoad, queueLoadEma);
            float shockScale = (float)WorldMapQueuePolicy.ComputeShockAbsorberScale(
                effectiveLoad,
                loadTrend,
                queueEmergencyBrakeLatched,
                queueShockAbsorberWeight);
            effectiveLoad = Mathf.Clamp(
                effectiveLoad * shockScale + queueLoadEma * (1f - shockScale),
                0f,
                4f);
            float emergencyThreshold = Mathf.Clamp(queueEmergencyBrakeThreshold, 0.75f, 4.0f);
            bool wasEmergencyLatched = queueEmergencyBrakeLatched;
            bool emergencyLatched = WorldMapQueuePolicy.UpdateEmergencyLatch(
                queueEmergencyBrakeLatched,
                effectiveLoad,
                emergencyThreshold,
                WorldMapQueuePolicy.ClampEmergencyReleaseRatio(queueEmergencyReleaseRatio, 0.84));
            if (!wasEmergencyLatched && emergencyLatched)
            {
                queueEmergencyHoldTicksRemaining = Mathf.Clamp(queueEmergencyHoldTicks, 1, 128);
                queueRecoveryRampTicksRemaining = 0;
            }
            else if (wasEmergencyLatched && emergencyLatched && queueEmergencyHoldTicksRemaining > 0)
            {
                queueEmergencyHoldTicksRemaining--;
            }

            if (wasEmergencyLatched && !emergencyLatched && queueEmergencyHoldTicksRemaining > 0)
            {
                emergencyLatched = true;
                queueEmergencyHoldTicksRemaining--;
            }

            queueEmergencyBrakeLatched = emergencyLatched;
            if (wasEmergencyLatched && !queueEmergencyBrakeLatched)
            {
                queueRecoveryRampTicksRemaining = Mathf.Clamp(queueRecoveryRampTicks, 1, 256);
                queueEmergencyHoldTicksRemaining = 0;
            }

            return effectiveLoad;
        }

        private void DrainStaleQueueEntries(bool forceAggressive = false)
        {
            int queueLimit = Math.Max(64, maxQueuedChunkRequests);
            int preDrainEstimate = Mathf.Clamp(queueOverloadDrainFactor + (forceAggressive ? 2 : 0), 1, 24);
            var queued = new List<Vector2Int>(Mathf.Max(16, preDrainEstimate * 4));

            while (requestQueue.TryDequeue(out var pos))
            {
                if (Volatile.Read(ref queuedRequestCount) > 0)
                {
                    Interlocked.Decrement(ref queuedRequestCount);
                }

                queued.Add(pos);
            }

            if (queued.Count == 0)
            {
                return;
            }

            Vector2Int center = playerTransform != null ? WorldToChunk(playerTransform.position) : Vector2Int.zero;
            var coordinates = new List<ChunkCoordinate>(queued.Count);
            for (int i = 0; i < queued.Count; i++)
            {
                coordinates.Add(new ChunkCoordinate(queued[i].x, queued[i].y));
            }

            float load = ComputeEffectiveQueueLoad(Mathf.Max(64, GetDynamicLoadedChunkBudget()));
            QueuePressureBand pressureBand = WorldMapQueuePolicy.ClassifyBand(load);
            int baseDrain = Mathf.Clamp(queueOverloadDrainFactor + (forceAggressive ? 2 : 0), 1, 24);
            int effectiveStalePruneMax = WorldMapQueuePolicy.ComputeEmergencyScaledStalePruneMax(
                queueStalePruneMax,
                queueStalePruneEmergencyMultiplier,
                queueEmergencyBrakeLatched || forceAggressive,
                8,
                1024);

            int drainBudget = WorldMapQueuePolicy.ComputeStalePruneBudget(
                queued.Count,
                baseDrain,
                pressureBand,
                queueEmergencyBrakeLatched || forceAggressive,
                load,
                1,
                effectiveStalePruneMax);
            float seamResilienceScale = ComputeHydrologySeamResilienceScale(
                load,
                (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma),
                Mathf.Abs((float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma)));
            float alluvialRelayScale = ComputeAlluvialAquiferRelayScale(
                load,
                (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma),
                Mathf.Abs((float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma)));
            float karstFloodplainRelayScale = ComputeKarstFloodplainRelayScale(
                load,
                (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma),
                Mathf.Abs((float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma)));
            float spillwayQueueScale = ComputeFloodplainSpillwayQueueScale(
                load,
                (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma),
                Mathf.Abs((float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma)));
            float aquiferConduitQueueScale = ComputeAquiferConduitQueueScale(
                load,
                (float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma),
                Mathf.Abs((float)WorldMapQueuePolicy.ComputeLoadTrend(load, queueLoadEma)));
            int nearKeepBudget = WorldMapQueuePolicy.ComputeAdaptiveNearChunkKeepCount(
                queueNearChunkKeepCount,
                Mathf.Max(16, viewRadiusChunks * 2),
                pressureBand,
                load,
                queueEmergencyBrakeLatched || forceAggressive,
                queueHotspotBias,
                queueHotspotEmergencyPenalty,
                8,
                512);
            nearKeepBudget = Mathf.Clamp(
                Mathf.RoundToInt(nearKeepBudget * Mathf.Clamp(seamResilienceScale * alluvialRelayScale * karstFloodplainRelayScale * spillwayQueueScale * aquiferConduitQueueScale, 0.76f, 1.3f)),
                8,
                512);
            int protectedNearCount = 0;
            var prioritized = WorldMapQueuePolicy.PrioritizeByDistance(
                center.x,
                center.y,
                coordinates,
                queueLimit,
                pressureBand,
                queueEmergencyBrakeLatched);
            int drained = 0;
            long nowTicks = DateTime.UtcNow.Ticks;

            foreach (var coordinate in prioritized)
            {
                var prioritizedPos = new Vector2Int(coordinate.X, coordinate.Z);
                bool alreadyLoaded = loadedChunks.ContainsKey(prioritizedPos) || buildingChunks.ContainsKey(prioritizedPos);
                bool farChunk = IsFarChunkFromPlayer(prioritizedPos, pressureBand);
                bool expired = IsQueueEntryExpired(prioritizedPos, nowTicks);
                bool protectNear = !alreadyLoaded && !expired && protectedNearCount < nearKeepBudget;
                bool hotspotProtected = IsHotspotChunk(prioritizedPos, pressureBand, load) && IsWithinHotspotRetentionWindow(prioritizedPos, nowTicks);
                bool dropForPressure = drained < drainBudget &&
                    !protectNear &&
                    !hotspotProtected &&
                    (farChunk || (forceAggressive && drained < drainBudget / 2));

                if (alreadyLoaded || expired || dropForPressure)
                {
                    queuedChunks.TryRemove(prioritizedPos, out _);
                    queuedChunkEnqueueTicks.TryRemove(prioritizedPos, out _);
                    drained++;
                    continue;
                }

                requestQueue.Enqueue(prioritizedPos);
                if (protectNear)
                {
                    protectedNearCount++;
                }
                if (Volatile.Read(ref queuedRequestCount) < queueLimit)
                {
                    Interlocked.Increment(ref queuedRequestCount);
                }
            }
        }

        private bool IsQueueEntryExpired(Vector2Int pos, long nowTicks)
        {
            if (!queuedChunkEnqueueTicks.TryGetValue(pos, out long queuedAtTicks))
            {
                return false;
            }

            long ttlTicks = TimeSpan.FromSeconds(Mathf.Clamp(queueRequestTtlSeconds, 5, 600)).Ticks;
            if (queueHotspotRetentionSeconds > 0)
            {
                float queueLoadSnapshot = GetQueueLoadSnapshot();
                QueuePressureBand pressureBand = WorldMapQueuePolicy.ClassifyBand(queueLoadSnapshot);
                if (IsHotspotChunk(pos, pressureBand, queueLoadSnapshot))
                {
                    long hotspotTtlTicks = TimeSpan.FromSeconds(Mathf.Clamp(queueHotspotRetentionSeconds, 5, 300)).Ticks;
                    ttlTicks = Math.Max(ttlTicks, hotspotTtlTicks);
                }
            }

            bool expired = nowTicks - queuedAtTicks > ttlTicks;
            if (expired)
            {
                queuedChunkEnqueueTicks.TryRemove(pos, out _);
            }

            return expired;
        }

        private bool IsWithinHotspotRetentionWindow(Vector2Int pos, long nowTicks)
        {
            if (queueHotspotRetentionSeconds <= 0)
            {
                return false;
            }

            if (!queuedChunkEnqueueTicks.TryGetValue(pos, out long queuedAtTicks))
            {
                return false;
            }

            long retentionTicks = TimeSpan.FromSeconds(Mathf.Clamp(queueHotspotRetentionSeconds, 5, 300)).Ticks;
            return nowTicks - queuedAtTicks < retentionTicks;
        }

        private bool IsHotspotChunk(Vector2Int pos, QueuePressureBand pressureBand, float queueLoadSnapshot)
        {
            if (playerTransform == null)
            {
                return false;
            }

            Vector2Int center = WorldToChunk(playerTransform.position);
            bool outsideHotspotWindow = WorldMapQueuePolicy.IsOutsideAdaptiveDistanceThreshold(
                center.x,
                center.y,
                pos.x,
                pos.y,
                Mathf.Max(1, viewRadiusChunks),
                pressureBand,
                queueEmergencyBrakeLatched,
                queueLoadSnapshot,
                queueHotspotBias,
                queueHotspotEmergencyPenalty);
            return !outsideHotspotWindow;
        }

        private bool IsFarChunkFromPlayer(Vector2Int pos, QueuePressureBand pressureBand)
        {
            if (playerTransform == null)
            {
                return false;
            }

            Vector2Int center = WorldToChunk(playerTransform.position);
            return WorldMapQueuePolicy.IsOutsideAdaptiveDistanceThreshold(
                center.x,
                center.y,
                pos.x,
                pos.y,
                Mathf.Max(1, viewRadiusChunks),
                pressureBand,
                queueEmergencyBrakeLatched,
                GetQueueLoadSnapshot(),
                queueHotspotBias,
                queueHotspotEmergencyPenalty);
        }

        private float GetQueueLoadSnapshot()
        {
            int budget = Mathf.Max(64, GetDynamicLoadedChunkBudget());
            int inFlight = buildingChunks.Count + Mathf.Max(0, Volatile.Read(ref queuedRequestCount));
            float instantLoad = inFlight / Mathf.Max(1f, budget);
            return Mathf.Max(instantLoad, queueLoadEma);
        }

        private Vector2Int WorldToChunk(Vector3 position)
        {
            int size = profile != null ? Math.Max(1, profile.ChunkSize) : 16;
            int cx = Mathf.FloorToInt(position.x / size);
            int cz = Mathf.FloorToInt(position.z / size);
            return new Vector2Int(cx, cz);
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
                return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private sealed class ClientRuntimeRoot
        {
            public ClientRuntimeWorldMapControl worldMapControl = new ClientRuntimeWorldMapControl();
        }

        [Serializable]
        private sealed class ClientRuntimeWorldMapControl
        {
            public ClientRuntimeDefaults defaults = new ClientRuntimeDefaults();
            public ClientRuntimePerformance performance = new ClientRuntimePerformance();
        }

        [Serializable]
        private sealed class ClientRuntimeDefaults
        {
            public int renderDistance = 4;
            public int maxLoadedPreviewChunks = 2048;
        }

        [Serializable]
        private sealed class ClientRuntimePerformance
        {
            public int maxConcurrentChunkRequests = 4;
            public int maxQueuedChunkRequests = 1024;
            public int queuePressureFactor = 2;
            public float queueSlackRatio = 2.0f;
            public float queueBurstSlackMultiplier = 1.15f;
            public float queueLoadSheddingThreshold = 0.88f;
            public float queueEmergencyBrakeThreshold = 1.15f;
            public float queueLoadEmaBlend = 0.18f;
            public float queueEmergencyReleaseRatio = 0.84f;
            public float queueTrendBoostWeight = 0.22f;
            public float queueShockAbsorberWeight = 0.24f;
            public float queueAlluvialRelayWeight = 0.82f;
            public float queueKarstSpillwayWeight = 0.9f;
            public int queueOverloadDrainFactor = 2;
            public int queueBackoffDelayMs = 4;
            public int queueEmergencyHoldTicks = 8;
            public int queueRecoveryRampTicks = 10;
            public int queueNearChunkKeepCount = 24;
            public int queueRequestTtlSeconds = 45;
            public float queueHotspotBias = 0.42f;
            public float queueHotspotEmergencyPenalty = 1.0f;
            public int queueHotspotRetentionSeconds = 18;
            public int queueStalePruneMax = 96;
            public float queueStalePruneEmergencyMultiplier = 1.35f;
        }

        [Serializable]
        private sealed class ClientQueuePolicyRoot
        {
            public ClientQueuePolicySection client = new ClientQueuePolicySection();
        }

        [Serializable]
        private sealed class ClientQueuePolicySection
        {
            public int maxQueuedChunkRequests = 1024;
            public int queuePressureFactor = 2;
            public float queueSlackRatio = 2.0f;
            public float queueBurstSlackMultiplier = 1.15f;
            public float queueLoadSheddingThreshold = 0.88f;
            public float queueEmergencyBrakeThreshold = 1.15f;
            public float queueLoadEmaBlend = 0.18f;
            public float queueEmergencyReleaseRatio = 0.84f;
            public float queueTrendBoostWeight = 0.22f;
            public float queueShockAbsorberWeight = 0.24f;
            public float queueAlluvialRelayWeight = 0.82f;
            public float queueKarstSpillwayWeight = 0.9f;
            public int queueOverloadDrainFactor = 2;
            public int queueBackoffDelayMs = 4;
            public int queueEmergencyHoldTicks = 8;
            public int queueRecoveryRampTicks = 10;
            public int queueNearChunkKeepCount = 24;
            public float queueHotspotBias = 0.42f;
            public float queueHotspotEmergencyPenalty = 1.0f;
            public int queueHotspotRetentionSeconds = 18;
            public int queueRequestTtlSeconds = 45;
            public int queueStalePruneMax = 96;
            public float queueStalePruneEmergencyMultiplier = 1.35f;
            public int maxLoadedPreviewChunks = 2048;
            public int maxConcurrentChunkRequests = 4;
        }
    }

    /// <summary>
    /// Lightweight terrain generator for Unity previews. Mirrors the server hydrology/cave/lake rules.
    /// </summary>
    public sealed class EnhancedTerrainGenerator
    {
        private readonly WorldMapControlProfile profile;
        private readonly WorldConfig worldConfig;
        private readonly System.Random random;
        private readonly int chunkSize;
        private readonly int worldHeight;
        private readonly int seaLevel;

        public EnhancedTerrainGenerator(WorldMapControlProfile profile, WorldConfig worldConfig)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.worldConfig = worldConfig ?? throw new ArgumentNullException(nameof(worldConfig));
            chunkSize = Mathf.Max(1, profile.ChunkSize);
            worldHeight = Mathf.Max(1, worldConfig.WorldHeight);
            seaLevel = Mathf.Clamp(worldConfig.Terrain.SeaLevel, 4, worldHeight - 4);
            int seed = worldConfig.Seed != 0 ? worldConfig.Seed : profile.ProfileHash.GetHashCode();
            random = new System.Random(seed);
        }

        public Task<ChunkData> GenerateChunkAsync(Vector2Int chunkPos, CancellationToken token)
        {
            return Task.Run(() => GenerateChunk(chunkPos), token);
        }

        private ChunkData GenerateChunk(Vector2Int chunkPos)
        {
            var chunk = new ChunkData(chunkSize, chunkPos.x, chunkPos.y, worldHeight, profile.GlobalWaterLevel);
            var heightMap = BuildHeightMap(chunkPos);
            var hydrology = BuildHydrologyMask(heightMap);
            var flow = BuildFlowMask(heightMap, hydrology);
            ApplyFlowMemory(heightMap, hydrology, flow);
            BlendHydrologyWithFlow(heightMap, hydrology, flow);
            ApplyCurvatureHydrologyGuide(heightMap, hydrology, flow);
            ApplyHydrologyContinuityEnvelope(heightMap, hydrology, flow);
            NormalizeHydrologyFlowEdges(hydrology, flow);
            DiffuseHydrologyEdges(hydrology, flow);
            ApplyWaterTableEnvelope(heightMap, hydrology, flow);
            ApplyHydrologyEdgeEnvelope(hydrology, flow);
            ApplyCrossChunkHydrologyStitch(hydrology, flow);
            ApplyHydrologyEdgeCohesion(heightMap, hydrology, flow);
            HarmonizeHydrologyWithSurface(heightMap, hydrology, flow);
            var erosionRisk = BuildErosionRiskMask(heightMap, hydrology, flow);
            ApplyErosionDamping(hydrology, flow, erosionRisk);
            ApplyHydrologyMomentum(heightMap, hydrology, flow, erosionRisk);
            ApplyWatershedRetentionField(heightMap, hydrology, flow, erosionRisk);
            ApplySubterraneanHydrologyShield(heightMap, hydrology, flow, erosionRisk);
            ApplyRiparianFlowBridge(heightMap, hydrology, flow, erosionRisk);
            ApplyFloodplainSlackwaterRetention(heightMap, hydrology, flow, erosionRisk);
            ApplyKarstWetlandCoupling(heightMap, hydrology, flow, erosionRisk);
            ApplyDeltaWaterTableCoupling(heightMap, hydrology, flow, erosionRisk);
            ApplyLagoonKarstCoupling(heightMap, hydrology, flow, erosionRisk);
            ApplyFloodplainLeakageStability(heightMap, hydrology, flow, erosionRisk);
            ApplyHydrologySinkStabilityField(heightMap, hydrology, flow, erosionRisk);
            ApplyKarstConfluenceRetentionField(heightMap, hydrology, flow, erosionRisk);
            ApplyKarstSpringFloodplainCouplingField(heightMap, hydrology, flow, erosionRisk);
            ApplySeasonalRunoffCouplingField(heightMap, hydrology, flow, erosionRisk, chunkPos);
            ApplyIsolatedBasinSpillwayBalancing(heightMap, hydrology, flow, erosionRisk);

            var riverMask = profile.EnableRivers ? BuildRiverMask(chunkPos, heightMap, hydrology, flow, erosionRisk) : new float[chunkSize, chunkSize];
            var lakeMask = profile.EnableLakes ? BuildLakeMask(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask) : new float[chunkSize, chunkSize];
            ApplyLakeHydrologySeepage(heightMap, hydrology, flow, lakeMask, riverMask);
            ApplyRiverLakeHydrologyFeedback(heightMap, hydrology, flow, riverMask, lakeMask, erosionRisk);
            ApplyAquiferSuppression(hydrology, flow, riverMask, lakeMask);
            ApplyRiparianCaveBuffer(erosionRisk, hydrology, flow, riverMask, lakeMask);
            ApplyFloodplainBasinPressureCoupling(heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask);
            ApplyHyporheicExchangeRelay(heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask);
            ApplyRiparianAquiferMomentumCoupling(heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask);
            var caveMask = profile.EnableCaves ? BuildCaveMask(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask) : new bool[chunkSize, worldHeight, chunkSize];
            ApplySubsurfaceConduitExchangeBridge(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask, caveMask);
            ApplyRiparianAquiferContinuityBridge(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask, caveMask);
            ApplyRiparianKarstExchangeBridge(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask, caveMask);
            ApplyAquiferConduitExchangeBridge(chunkPos, heightMap, hydrology, flow, erosionRisk, riverMask, lakeMask, caveMask);

            ApplyHydrologyToHeight(heightMap, riverMask, lakeMask, hydrology, flow);

            CopyField(heightMap, chunk.HeightMap);
            CopyField(hydrology, chunk.HydrologyMask);
            CopyField(flow, chunk.FlowMask);
            CopyField(riverMask, chunk.RiverMask);
            CopyField(lakeMask, chunk.LakeMask);
            CopyField(caveMask, chunk.CaveMask);
            return chunk;
        }

        private void ApplySubsurfaceConduitExchangeBridge(
            Vector2Int chunkPos,
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask,
            bool[,,] caveMask)
        {
            float exchangeWeight = Mathf.Clamp(
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.42f +
                worldConfig.Water.RiverConfluenceBoost * 0.33f +
                worldConfig.Lakes.FlowSeepageWeight * 0.25f,
                0f,
                1.4f);
            if (exchangeWeight <= 0.01f)
            {
                return;
            }

            int bedrockLevel = Mathf.Max(1, worldConfig.Terrain.BedrockLevel);
            float slopePenalty = Mathf.Max(0f, worldConfig.Water.HydrologySlopePenalty);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float river = Mathf.Clamp01(riverMask[x, z]);
                    float lake = Mathf.Clamp01(lakeMask[x, z]);
                    float channelPressure = Mathf.Clamp01(river * 0.58f + lake * 0.42f);
                    if (channelPressure <= 0.001f)
                    {
                        continue;
                    }

                    float hydro = hydroCopy[x, z];
                    float seamHydro = SampleInterior(hydroCopy, x, z);
                    float flowNode = flowCopy[x, z];
                    float seamFlow = SampleInterior(flowCopy, x, z);
                    float flowNormalized = Mathf.Clamp01(flowNode / 6f);
                    float continuity = Mathf.Clamp01((seamHydro + Mathf.Clamp01(seamFlow / 6f)) * 0.5f);
                    float slope = ComputeSlope(heightMap, x, z);
                    float relief = Mathf.Clamp01(
                        ComputeLocalRelief(heightMap, x, z, Mathf.Max(1, profile.HydrologyEdgeBlendRadius)) / Mathf.Max(1f, worldConfig.Water.HydrologyWaterTableClampRange + 4f));

                    float coupling = channelPressure * (0.44f + continuity * 0.28f + flowNormalized * 0.18f + relief * 0.1f);
                    coupling *= 1f - Mathf.Clamp01(slope * slopePenalty * 0.012f);

                    float recharge = coupling * exchangeWeight;
                    float hydroTarget = hydro + recharge * 0.085f - Mathf.Max(0f, slope - 0.15f) * 0.02f;
                    float flowTarget = flowNode * (1f - recharge * 0.05f) + seamFlow * recharge * 0.08f + channelPressure * 0.04f;
                    hydrology[x, z] = Mathf.Clamp(hydroTarget, 0f, 1.2f);
                    flow[x, z] = Mathf.Clamp(
                        flowTarget,
                        0f,
                        Mathf.Max(flowNode + 1.5f, worldConfig.Water.HydrologyFlowDivergenceClamp * 12f));
                    erosionRisk[x, z] = Mathf.Clamp01(
                        erosionRisk[x, z] * (1f - recharge * 0.07f) +
                        channelPressure * 0.04f +
                        flowNormalized * 0.03f);

                    riverMask[x, z] = Mathf.Clamp01(river + hydrology[x, z] * 0.03f + continuity * 0.02f);
                    lakeMask[x, z] = Mathf.Clamp01(lake + hydrology[x, z] * 0.02f + channelPressure * 0.015f);

                    if (recharge <= 0.28f)
                    {
                        continue;
                    }

                    int surface = Mathf.Max(bedrockLevel + 9, heightMap[x, z]);
                    int conduitTop = Mathf.Clamp(
                        seaLevel - 2 + Mathf.RoundToInt((hydrology[x, z] - 0.5f) * 8f),
                        bedrockLevel + 8,
                        Mathf.Max(bedrockLevel + 8, surface - 6));
                    int conduitBottom = Mathf.Max(bedrockLevel + 3, conduitTop - 3 - Mathf.RoundToInt(channelPressure * 3f));
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    for (int y = conduitBottom; y <= conduitTop; y++)
                    {
                        if (y < 0 || y >= worldHeight || caveMask[x, y, z])
                        {
                            continue;
                        }

                        float pulse = ComputeSubsurfacePulse(worldX, worldZ, y);
                        float carveThreshold = 0.82f - recharge * 0.38f - channelPressure * 0.12f;
                        if (pulse > carveThreshold)
                        {
                            caveMask[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private void ApplyRiparianAquiferContinuityBridge(
            Vector2Int chunkPos,
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask,
            bool[,,] caveMask)
        {
            float continuityWeight = Mathf.Clamp(
                worldConfig.Water.HydrologyContinuityWeight * 0.36f +
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.34f +
                worldConfig.Lakes.SpillRetentionWeight * 0.30f,
                0f,
                1.2f);
            if (continuityWeight <= 0.01f)
            {
                return;
            }

            int bedrockLevel = Mathf.Max(1, worldConfig.Terrain.BedrockLevel);
            float slopePenalty = Mathf.Max(0f, worldConfig.Water.HydrologySlopePenalty);
            float divergenceClamp = Mathf.Max(0.0001f, worldConfig.Water.HydrologyFlowDivergenceClamp);
            int edgeRadius = Mathf.Max(2, profile.HydrologyEdgeBlendRadius);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float river = Mathf.Clamp01(riverMask[x, z]);
                    float lake = Mathf.Clamp01(lakeMask[x, z]);
                    float channelPressure = Mathf.Clamp(river * 0.62f + lake * 0.38f, 0f, 1.2f);
                    if (channelPressure <= 0.02f)
                    {
                        continue;
                    }

                    float hydro = hydroCopy[x, z];
                    float seamHydro = SampleInterior(hydroCopy, x, z);
                    float flowNodeRaw = flowCopy[x, z];
                    float seamFlowRaw = SampleInterior(flowCopy, x, z);
                    float flowNode = Mathf.Clamp(flowNodeRaw / 6f, 0f, 1.2f);
                    float seamFlow = Mathf.Clamp(seamFlowRaw / 6f, 0f, 1.2f);
                    float slope = ComputeSlope(heightMap, x, z);
                    float relief = Mathf.Clamp01(
                        ComputeLocalRelief(heightMap, x, z, edgeRadius) /
                        Mathf.Max(1f, worldConfig.Water.HydrologyWaterTableClampRange + 6f));
                    float divergence = Mathf.Clamp01(Mathf.Abs(flowNodeRaw - seamFlowRaw) / Mathf.Max(1f, divergenceClamp * 10f));

                    float continuitySignal = Mathf.Clamp(
                        channelPressure * 0.42f +
                        seamHydro * 0.24f +
                        flowNode * 0.18f +
                        seamFlow * 0.16f,
                        0f,
                        1.4f);
                    continuitySignal *= 1f - Mathf.Clamp01(
                        slope * slopePenalty * 0.012f +
                        relief * 0.24f +
                        divergence * 0.20f);

                    float recharge = continuitySignal * continuityWeight;
                    float hydroTarget = hydro + recharge * 0.064f - Mathf.Max(0f, slope - 0.18f) * 0.018f;
                    float flowTarget = flowNodeRaw * (1f - recharge * 0.045f) + seamFlowRaw * recharge * 0.035f + channelPressure * 0.18f;
                    hydrology[x, z] = Mathf.Clamp(hydroTarget, 0f, 1.2f);
                    flow[x, z] = Mathf.Clamp(flowTarget, 0f, 1.2f);
                    erosionRisk[x, z] = Mathf.Clamp01(
                        erosionRisk[x, z] * (1f - recharge * 0.06f) +
                        channelPressure * 0.03f +
                        flowNode * 0.02f);

                    riverMask[x, z] = Mathf.Clamp01(river + hydrology[x, z] * 0.02f + seamFlow * 0.02f);
                    lakeMask[x, z] = Mathf.Clamp01(lake + hydrology[x, z] * 0.016f + channelPressure * 0.014f);

                    if (recharge <= 0.30f)
                    {
                        continue;
                    }

                    int surface = Mathf.Max(bedrockLevel + 8, heightMap[x, z]);
                    int aquiferCeiling = Mathf.Clamp(
                        seaLevel - 3 + Mathf.RoundToInt((hydrology[x, z] - 0.5f) * 6f),
                        bedrockLevel + 6,
                        Mathf.Max(bedrockLevel + 6, surface - 7));
                    int aquiferFloor = Mathf.Max(bedrockLevel + 2, aquiferCeiling - 2 - Mathf.RoundToInt(channelPressure * 2f));
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    for (int y = aquiferFloor; y <= aquiferCeiling; y++)
                    {
                        if (y < 0 || y >= worldHeight || caveMask[x, y, z])
                        {
                            continue;
                        }

                        float pulse = ComputeSubsurfacePulse(worldX + 17, worldZ - 19, y + 5);
                        float carveThreshold = 0.86f - recharge * 0.28f - channelPressure * 0.10f;
                        if (pulse > carveThreshold)
                        {
                            caveMask[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private void ApplyRiparianKarstExchangeBridge(
            Vector2Int chunkPos,
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask,
            bool[,,] caveMask)
        {
            float exchangeWeight = Mathf.Clamp(
                worldConfig.Water.HydrologyFlowPersistence * 0.34f +
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.33f +
                worldConfig.Lakes.FlowSeepageWeight * 0.33f,
                0f,
                1.3f);
            if (exchangeWeight <= 0.01f)
            {
                return;
            }

            int bedrockLevel = Mathf.Max(1, worldConfig.Terrain.BedrockLevel);
            float slopePenalty = Mathf.Max(0f, worldConfig.Water.HydrologySlopePenalty);
            float divergenceClamp = Mathf.Max(0.0001f, worldConfig.Water.HydrologyFlowDivergenceClamp);
            int edgeRadius = Mathf.Max(2, profile.HydrologyEdgeBlendRadius);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float river = Mathf.Clamp01(riverMask[x, z]);
                    float lake = Mathf.Clamp01(lakeMask[x, z]);
                    float channelPressure = Mathf.Clamp(river * 0.54f + lake * 0.46f, 0f, 1.2f);
                    if (channelPressure <= 0.02f)
                    {
                        continue;
                    }

                    float hydro = hydroCopy[x, z];
                    float seamHydro = SampleInterior(hydroCopy, x, z);
                    float flowNodeRaw = flowCopy[x, z];
                    float seamFlowRaw = SampleInterior(flowCopy, x, z);
                    float flowNode = Mathf.Clamp(flowNodeRaw / 6f, 0f, 1.2f);
                    float seamFlow = Mathf.Clamp(seamFlowRaw / 6f, 0f, 1.2f);
                    float slope = ComputeSlope(heightMap, x, z);
                    float relief = Mathf.Clamp01(
                        ComputeLocalRelief(heightMap, x, z, edgeRadius) /
                        Mathf.Max(1f, worldConfig.Water.HydrologyWaterTableClampRange + 6f));
                    float curvature = Mathf.Clamp(Mathf.Max(0f, SampleCurvature(heightMap, x, z)), 0f, 1f);
                    float divergence = Mathf.Clamp01(Mathf.Abs(flowNodeRaw - seamFlowRaw) / Mathf.Max(1f, divergenceClamp * 10f));
                    float slopeNormalized = Mathf.Clamp01(slope * 0.08f);
                    float karstPocket = Mathf.Clamp01(
                        curvature * 0.52f +
                        (1f - slopeNormalized) * 0.33f +
                        (1f - relief) * 0.15f);
                    float continuity = Mathf.Clamp((seamHydro + flowNode + seamFlow) / 3f, 0f, 1.2f);

                    float relay = channelPressure * (0.42f + continuity * 0.28f + karstPocket * 0.30f);
                    relay *= 1f - Mathf.Clamp01(
                        slope * slopePenalty * 0.011f +
                        divergence * 0.30f);

                    float recharge = relay * exchangeWeight;
                    float hydroTarget = hydro + recharge * 0.058f - Mathf.Max(0f, slope - 0.2f) * 0.016f;
                    float flowTarget = flowNodeRaw * (1f - recharge * 0.04f) + seamFlowRaw * recharge * 0.06f + channelPressure * 0.12f + karstPocket * 0.06f;
                    hydrology[x, z] = Mathf.Clamp(hydroTarget, 0f, 1.2f);
                    flow[x, z] = Mathf.Clamp(
                        flowTarget,
                        0f,
                        Mathf.Max(flowNodeRaw + 1.25f, divergenceClamp * 12f));
                    erosionRisk[x, z] = Mathf.Clamp01(
                        erosionRisk[x, z] * (1f - recharge * 0.055f) +
                        channelPressure * 0.022f +
                        karstPocket * 0.018f);

                    riverMask[x, z] = Mathf.Clamp01(river + hydrology[x, z] * 0.015f + continuity * 0.012f);
                    lakeMask[x, z] = Mathf.Clamp01(lake + hydrology[x, z] * 0.013f + karstPocket * 0.015f);

                    if (recharge <= 0.32f)
                    {
                        continue;
                    }

                    int surface = Mathf.Max(bedrockLevel + 9, heightMap[x, z]);
                    int relayTop = Mathf.Clamp(
                        seaLevel - 5 + Mathf.RoundToInt((hydrology[x, z] - 0.5f) * 7f),
                        bedrockLevel + 5,
                        Mathf.Max(bedrockLevel + 5, surface - 8));
                    int relayBottom = Mathf.Max(bedrockLevel + 2, relayTop - 3 - Mathf.RoundToInt(karstPocket * 3f));
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    for (int y = relayBottom; y <= relayTop; y++)
                    {
                        if (y < 0 || y >= worldHeight || caveMask[x, y, z])
                        {
                            continue;
                        }

                        float pulse = ComputeSubsurfacePulse(worldX - 23, worldZ + 29, y + 11);
                        float carveThreshold = 0.84f - recharge * 0.24f - karstPocket * 0.11f;
                        if (pulse > carveThreshold)
                        {
                            caveMask[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private void ApplyAquiferConduitExchangeBridge(
            Vector2Int chunkPos,
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask,
            bool[,,] caveMask)
        {
            float bridgeWeight = Mathf.Clamp(
                worldConfig.Water.HydrologyContinuityWeight * 0.24f +
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.24f +
                worldConfig.Lakes.SpillwayContinuityWeight * 0.2f +
                worldConfig.Lakes.SpillRetentionWeight * 0.18f +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.14f,
                0f,
                1.45f);
            if (bridgeWeight <= 0.01f)
            {
                return;
            }

            int bedrockLevel = Mathf.Max(1, worldConfig.Terrain.BedrockLevel);
            int edgeRadius = Mathf.Max(2, profile.HydrologyEdgeBlendRadius + 2);
            float slopePenalty = Mathf.Max(0f, worldConfig.Water.HydrologySlopePenalty);
            float divergenceClamp = Mathf.Max(0.0001f, worldConfig.Water.HydrologyFlowDivergenceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    float river = Mathf.Clamp01(riverMask[x, z]);
                    float lake = Mathf.Clamp01(lakeMask[x, z]);
                    float channelSignal = Mathf.Clamp(river * 0.56f + lake * 0.44f, 0f, 1.2f);
                    if (channelSignal <= 0.01f)
                    {
                        continue;
                    }

                    float hydro = Mathf.Clamp01(hydroCopy[x, z]);
                    float seamHydro = SampleInterior(hydroCopy, x, z);
                    float flowNodeRaw = Mathf.Max(0f, flowCopy[x, z]);
                    float seamFlowRaw = Mathf.Max(0f, SampleInterior(flowCopy, x, z));
                    float flowNode = Mathf.Clamp(flowNodeRaw / 6f, 0f, 1.3f);
                    float seamFlow = Mathf.Clamp(seamFlowRaw / 6f, 0f, 1.3f);
                    float continuity = Mathf.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25f, 0f, 1.25f);
                    float slope = ComputeSlope(heightMap, x, z);
                    float relief = Mathf.Clamp01(
                        ComputeLocalRelief(heightMap, x, z, edgeRadius) /
                        Mathf.Max(1f, worldConfig.Water.HydrologyWaterTableClampRange + 10f));
                    float floodplainBand = Mathf.Clamp01(
                        1f - Mathf.Abs(heightMap[x, z] - seaLevel) / Mathf.Max(5f, worldConfig.Water.RiverMouthSmoothRadius * 1.9f));
                    float divergence = Mathf.Clamp01(Mathf.Abs(flowNodeRaw - seamFlowRaw) / Mathf.Max(1f, divergenceClamp * 10f));
                    float relayNoise = ComputeSubsurfacePulse(
                        chunkPos.x * chunkSize + x + 89,
                        chunkPos.y * chunkSize + z - 71,
                        seaLevel + 31);
                    float coupling = channelSignal * bridgeWeight *
                                     (0.22f + continuity * 0.28f + floodplainBand * 0.22f + relayNoise * 0.14f);
                    coupling *= 1f - Mathf.Clamp01(
                        slope * slopePenalty * 0.011f +
                        relief * 0.33f +
                        divergence * 0.23f);
                    if (coupling <= 0.0005f)
                    {
                        continue;
                    }

                    float hydroTarget = hydro + coupling * 0.076f + seamHydro * coupling * 0.022f;
                    float flowTarget = flowNodeRaw * (1f - coupling * 0.052f) + seamFlowRaw * coupling * 0.078f + channelSignal * 0.028f;
                    hydrology[x, z] = Mathf.Clamp(hydroTarget, 0f, 1.2f);
                    flow[x, z] = Mathf.Clamp(
                        flowTarget,
                        0f,
                        Mathf.Max(flowNodeRaw + 1.75f, worldConfig.Water.HydrologyFlowDivergenceClamp * 12f));
                    erosionRisk[x, z] = Mathf.Clamp01(
                        erosionRisk[x, z] * (1f - coupling * 0.085f) +
                        channelSignal * 0.031f +
                        continuity * 0.024f);

                    riverMask[x, z] = Mathf.Clamp01(river + coupling * 0.021f + floodplainBand * 0.012f);
                    lakeMask[x, z] = Mathf.Clamp01(lake + coupling * 0.018f + continuity * 0.010f);

                    if (coupling <= 0.29f)
                    {
                        continue;
                    }

                    int surface = Mathf.Max(bedrockLevel + 9, heightMap[x, z]);
                    int conduitTop = Mathf.Clamp(
                        seaLevel - 4 + Mathf.RoundToInt((hydrology[x, z] - 0.5f) * 6f),
                        bedrockLevel + 5,
                        Mathf.Max(bedrockLevel + 5, surface - 8));
                    int conduitBottom = Mathf.Max(bedrockLevel + 2, conduitTop - 3 - Mathf.RoundToInt(coupling * 3f));
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    for (int y = conduitBottom; y <= conduitTop; y++)
                    {
                        if (y < 0 || y >= worldHeight || caveMask[x, y, z])
                        {
                            continue;
                        }

                        float pulse = ComputeSubsurfacePulse(worldX + 37, worldZ - 41, y + 23);
                        float carveThreshold = 0.87f - coupling * 0.2f - continuity * 0.09f;
                        if (pulse > carveThreshold)
                        {
                            caveMask[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private float ComputeSubsurfacePulse(int worldX, int worldZ, int y)
        {
            unchecked
            {
                int hash = worldConfig.Seed;
                hash = (hash * 397) ^ worldX;
                hash = (hash * 397) ^ worldZ;
                hash = (hash * 397) ^ y;
                hash ^= hash >> 16;
                return (hash & int.MaxValue) / (float)int.MaxValue;
            }
        }

        private string ComputeGenerationSignature(WorldMapControlProfile controlProfile, WorldConfig config)
        {
            ProtoDiagnostics.AssertFingerprint();
            ProtocolRegistry.ValidateBindings();
            int adaptiveQueuePressure = GetAdaptiveQueuePressureFactor();
            int adaptiveQueueLimit = GetAdaptiveQueueLimit();
            float adaptiveQueueSlack = GetAdaptiveQueueSlackRatio();
            int effectiveChunkSize = controlProfile.ChunkSize > 0 ? controlProfile.ChunkSize : config.ChunkSize;
            int effectiveRenderDistance = controlProfile.RenderDistance > 0 ? controlProfile.RenderDistance : config.RenderDistance;
            int effectiveSimulationDistance = controlProfile.SimulationDistance > 0 ? controlProfile.SimulationDistance : config.SimulationDistance;
            int effectiveGlobalWaterLevel = controlProfile.GlobalWaterLevel > 0 ? controlProfile.GlobalWaterLevel : config.Water.GlobalWaterLevel;
            string effectiveHydrologySignature = string.IsNullOrEmpty(controlProfile.HydrologySignature)
                ? SharedFeatureCatalog.HydrologySignature
                : controlProfile.HydrologySignature;
            var context = new WorldMapSignatureContext(
                PipelineVersion,
                config.WorldName,
                config.Seed,
                ProtoFingerprint.DescriptorFingerprint,
                ProtoFingerprint.ComputeFingerprint(),
                controlProfile.Version,
                controlProfile.ProfileHash,
                ComputeFileHash(configPath),
                ComputeFileHash(Path.Combine(Application.streamingAssetsPath, profileFileName)),
                effectiveHydrologySignature,
                effectiveChunkSize,
                config.WorldHeight,
                effectiveRenderDistance,
                effectiveSimulationDistance,
                effectiveGlobalWaterLevel,
                config.Terrain.SeaLevel,
                config.Water.HydrologyFlowPersistence,
                config.Water.HydrologyCatchmentWeight,
                config.Water.HydrologyFlowGain,
                config.Water.HydrologyWatershedStitchWeight,
                config.Water.HydrologyWatershedStitchRadius,
                config.Water.HydrologyGradientStabilityIterations,
                config.Water.HydrologyGradientStabilityBlend,
                config.Water.HydrologyGradientClamp,
                config.Water.HydrologyCurvatureWeight,
                config.Water.HydrologySlopePenalty,
                config.Water.HydrologyWaterTableClampWeight,
                config.Water.HydrologyWaterTableClampRange,
                config.Water.HydrologyWaterTableSlopeWeight,
                config.Lakes.MinDepth,
                config.Lakes.MaxDepth,
                config.Lakes.MaxRadius,
                config.Lakes.ShelfDepth,
                config.Lakes.FlowSeepageWeight,
                config.Lakes.OutflowSealWeight,
                config.Lakes.OutflowStabilityWeight,
                config.Caves.CeilingMoistureWeight,
                config.Caves.CeilingMoistureClamp,
                config.Caves.MoistureFlowClamp,
                config.Caves.FloodedCaveNoiseFrequency,
                config.Caves.FloodedCaveThreshold,
                config.Caves.FloodedCaveProximityToWaterTableWeight,
                config.Caves.WaterThreshold,
                config.Caves.LavaThreshold,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeVarianceClamp,
                config.Water.HydrologyEdgeNormalizationBlend,
                config.Water.HydrologyEdgeNormalizationIterations,
                config.Water.HydrologyFlowMemoryWeight,
                config.Water.HydrologyContinuityWeight,
                config.Water.RiverMeanderJitter,
                config.Water.RiverReliefPenaltyWeight,
                config.Water.RiverAnisotropyDamping,
                config.Water.RiverBankStabilityClamp,
                config.Water.RiverSeamFillStrength,
                config.Lakes.RiverProximitySuppression,
                config.Water.HydrologyFlowShadowWeight,
                config.Water.HydrologyFlowShadowSlopeWeight,
                config.Water.HydrologyPressureBlend,
                config.Water.HydrologyPressureGradientClamp,
                config.Water.HydrologyEdgeFlowBias,
                config.Water.HydrologyEdgeFlowLockWeight,
                config.Water.HydrologyEdgeTangentWeight,
                config.Water.RiverFlowAlignmentWeight,
                config.Water.RiverConfluenceBoost,
                config.Water.RiverTributaryCaptureWeight,
                config.Water.RiverAvulsionResistance,
                config.Water.RiverBraidingWeight,
                config.Water.LakeRimErosionWeight,
                config.Lakes.VarianceWeight,
                config.Water.LakeInflowBlendWeight,
                config.Lakes.OutflowCarveDepth,
                config.Caves.EdgeSealStrength,
                config.Caves.RiverSuppressionWeight,
                config.Caves.RiparianCaveGuardWeight,
                config.Water.HydrologyReservoirIterations,
                config.Water.HydrologyReservoirBlend,
                config.Water.RiverEdgeContinuityWeight,
                config.Lakes.LakeOutflowTaper,
                config.Lakes.SpillRetentionWeight,
                config.Lakes.SpillwayContinuityWeight,
                config.Caves.CaveEntranceFlowDampening,
                config.Caves.GroundwaterConnectivityWeight,
                config.Caves.CaveVentilationBias,
                config.Caves.AquiferBarrierWeight,
                config.Water.RiverNoiseScale,
                config.Water.RiverIntensitySmoothIterations,
                config.Water.RiverIntensitySmoothBlend,
                config.Lakes.ShorelineBlend,
                config.Lakes.WetlandSaturationThreshold,
                config.Caves.SupportDensity,
                config.Caves.MoistureRetentionWeight,
                config.Caves.CeilingStabilityWeight,
                GetDynamicLoadedChunkBudget(),
                buildingChunks.Count + Mathf.Max(0, Volatile.Read(ref queuedRequestCount)),
                Mathf.Max(1, adaptiveQueuePressure),
                Mathf.Max(64, adaptiveQueueLimit),
                WorldMapQueuePolicy.ComputeAdaptiveNearChunkKeepCount(
                    queueNearChunkKeepCount,
                    Mathf.Max(16, viewRadiusChunks * 2),
                    WorldMapQueuePolicy.ClassifyBand(GetQueueLoadSnapshot()),
                    GetQueueLoadSnapshot(),
                    queueEmergencyBrakeLatched,
                    queueHotspotBias,
                    queueHotspotEmergencyPenalty,
                    8,
                    512),
                Mathf.Clamp(queueLoadSheddingThreshold, 0.5f, 0.98f),
                Mathf.Max(1.1f, adaptiveQueueSlack),
                Mathf.Clamp(queueBurstSlackMultiplier, 1.0f, 3.0f),
                Mathf.Clamp(queueShockAbsorberWeight, 0.0f, 1.5f));
            return WorldMapSignature.Compute(context);
        }

        private int[,] BuildHeightMap(Vector2Int chunkPos)
        {
            var heightMap = new int[chunkSize, chunkSize];
            var terrain = worldConfig.Terrain;
            double noiseScale = 1.0 / Math.Max(terrain.NoiseScale, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    float continental = Mathf.PerlinNoise(worldX * (float)(noiseScale * 0.25f), worldZ * (float)(noiseScale * 0.25f));
                    float macro = Mathf.PerlinNoise(worldX * (float)(noiseScale * 0.5f) + 77f, worldZ * (float)(noiseScale * 0.5f) + 19f);
                    float detail = Mathf.PerlinNoise(worldX * (float)(noiseScale * terrain.Lacunarity) + 13f, worldZ * (float)(noiseScale * terrain.Lacunarity) + 29f);

                    float blended = continental * 0.55f + macro * 0.3f + detail * 0.15f;
                    float baseHeight = terrain.PlainBaseHeight + blended * (float)terrain.NoiseAmplitude;
                    heightMap[x, z] = Mathf.Clamp(Mathf.RoundToInt(baseHeight), 1, worldHeight - 4);
                }
            }

            return heightMap;
        }

        private bool[,,] BuildCaveMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flowMask, float[,] erosionRisk, float[,] riverMask)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            double horizontal = Math.Max(0.0001, worldConfig.Caves.HorizontalFrequency);
            double vertical = Math.Max(0.0001, worldConfig.Caves.VerticalFrequency);
            double moistureFlowClamp = Math.Max(0.0, worldConfig.Caves.MoistureFlowClamp);
            double aquiferBarrierWeight = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    float hydrologySample = hydrology[x, z];
                    float flowSample = flowMask[x, z];
                    float riverPressure = riverMask != null ? riverMask[x, z] : 0f;
                    float seamHydro = SampleInterior(hydrology, x, z);
                    float seamFlow = SampleInterior(flowMask, x, z);
                    float seamRiver = riverMask != null ? SampleInterior(riverMask, x, z) : riverPressure;
                    double flowMemory = Math.Clamp((flowSample + seamFlow) * 0.5, 0.0, moistureFlowClamp);
                    double wetnessRetention = hydrologySample * worldConfig.Caves.MoistureRetentionWeight + flowMemory * worldConfig.Caves.MoistureRetentionWeight * 0.35;
                    double edgeFactor = ComputeEdgeFalloff(x, z);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double flowGradient = Math.Abs(seamFlow - flowSample);
                    double seamStability = 1.0 - Math.Clamp(hydrologyGradient * worldConfig.Caves.EdgeSealStrength, 0.0, 0.45);
                    double continuityClamp = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * worldConfig.Caves.EdgeSealStrength * 0.2, 0.0, 0.45);
                    double seamContinuity = 1.0 - Math.Clamp(hydrologyGradient * worldConfig.Caves.EdgeSealStrength * 0.5, 0.0, 0.65);
                    seamContinuity *= 1.0 - Math.Clamp(Math.Abs(flowMemory - flowSample) * worldConfig.Caves.EdgeSealStrength * 0.25, 0.0, 0.45);
                    double riparianPenalty = Math.Clamp(seamRiver * worldConfig.Caves.RiverSuppressionWeight, 0.0, 0.9);
                    double erosionPenalty = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double slopeStability = 1.0 - Math.Clamp(slope * worldConfig.Caves.CeilingStabilityWeight * 0.02, 0.0, 0.35);
                    double hydrologyShadow = Math.Clamp(
                        hydrologySample * worldConfig.Caves.HydrologyStabilityWeight +
                        seamHydro * worldConfig.Caves.HydrologyStabilityWeight * 0.25 +
                        flowMemory * worldConfig.Caves.FlowStabilityWeight * 0.35,
                        0.0,
                        1.0);
                    double moistureContinuity = Math.Clamp((hydrologyGradient + flowGradient) * worldConfig.Caves.MoistureRetentionWeight * 0.25, 0.0, 0.55);
                    double flowShadowDrift = Math.Clamp(Math.Abs(flowMemory - flowSample) * worldConfig.Caves.MoistureRetentionWeight * 0.5, 0.0, moistureFlowClamp);
                    double slopeThresholdPenalty = Math.Clamp(slope * worldConfig.Caves.CeilingStabilityWeight * 0.015, 0.0, 0.25);
                    double varianceBrake = Math.Clamp(Math.Abs(flowMemory - flowSample) * worldConfig.Caves.RoughnessStabilityWeight * 0.25, 0.0, 0.35);
                    double saturationBrake = Math.Clamp(
                        (hydrologySample + flowSample + seamHydro + seamFlow) * worldConfig.Caves.MoistureRetentionWeight * 0.15,
                        0.0,
                        0.45);
                    double aquiferPenalty = Math.Clamp(
                        wetnessRetention + riverPressure * worldConfig.Caves.RiverSuppressionWeight * 0.25,
                        0.0,
                        1.0);
                    double ceilingClamp = Math.Clamp(
                        hydrologySample * worldConfig.Caves.CeilingMoistureWeight +
                        flowMemory * worldConfig.Caves.CeilingMoistureWeight * 0.5 +
                        hydrologyGradient * worldConfig.Caves.CeilingMoistureWeight * 0.35,
                        0.0,
                        1.0);
                    double hydrologyEnvelope = (hydrologySample + seamHydro + flowMemory) / 3.0;
                    double aquiferBarrier = Math.Clamp(
                        (hydrologyEnvelope + flowMemory + riverPressure) * aquiferBarrierWeight * 0.5,
                        0.0,
                        0.75);
                    double flowContinuity = Math.Clamp(Math.Abs(flowMemory - flowSample) * worldConfig.Caves.FlowStabilityWeight * 0.5, 0.0, 0.6);
                    double riparianBridge = Math.Clamp((hydrologyEnvelope + riverPressure) * worldConfig.Caves.RiverSuppressionWeight * 0.35, 0.0, 0.65);
                    double divergenceGuard = Math.Clamp(
                        (hydrologyGradient + flowGradient) * worldConfig.Caves.CeilingMoistureClamp * 0.25 +
                        Math.Abs(seamRiver - riverPressure) * worldConfig.Caves.EdgeSealStrength * 0.25,
                        0.0,
                        0.6);
                    double karstPotential = Math.Clamp(
                        (1.0 - Math.Clamp(slope * 0.05, 0.0, 0.6)) * (hydrologyEnvelope * 0.6 + flowMemory * 0.4),
                        0.0,
                        1.0);
                    double stability = ComputeColumnStability(surface, hydrologySample, riverPressure, flowSample, edgeFactor) * seamStability * continuityClamp;
                    stability *= 1.0 - riparianPenalty * 0.25;
                    stability *= 1.0 - riparianBridge * 0.2;
                    stability *= Math.Max(0.35, seamContinuity);
                    stability *= 1.0 - Math.Clamp(erosionPenalty * worldConfig.Caves.EdgeSealStrength * 0.35, 0.0, 0.35);
                    stability *= 1.0 - varianceBrake * 0.35;
                    stability *= 1.0 - saturationBrake * 0.3;
                    stability *= 1.0 - ceilingClamp * 0.15;
                    stability *= slopeStability;
                    stability *= 1.0 - moistureContinuity * 0.35;
                    stability *= 1.0 - hydrologyShadow * 0.1;
                    stability *= 1.0 - aquiferPenalty * 0.3;
                    stability *= 1.0 - aquiferBarrier * 0.28;
                    stability *= 1.0 - divergenceGuard * 0.35;
                    stability *= 1.0 - Math.Clamp(karstPotential * worldConfig.Caves.CaveEntranceFlowDampening * 0.35, 0.0, 0.4);

                    for (int y = 1; y < Math.Min(surface - 1, worldHeight - 2); y++)
                    {
                        double depthFactor = 1.0 - (double)y / Math.Max(1, surface);
                        double warpX = (chunkPos.x * chunkSize + x) * horizontal;
                        double warpZ = (chunkPos.y * chunkSize + z) * horizontal;
                        double warpY = y * vertical;

                        double noise = Mathf.PerlinNoise((float)(warpX + warpY), (float)(warpZ + warpY));
                        double roughnessBias = Mathf.PerlinNoise((float)(warpX * 0.8), (float)(warpZ * 0.8)) * worldConfig.Caves.RoughnessStabilityWeight;
                        double moisturePenalty = hydrologySample * worldConfig.Caves.HydrologyStabilityWeight + riverPressure * worldConfig.Caves.RiverSuppressionWeight + wetnessRetention * 0.35;
                        double flowPenalty = flowSample * worldConfig.Caves.FlowStabilityWeight;
                        double threshold = worldConfig.Caves.Threshold + moisturePenalty * 0.35 + flowPenalty * 0.35 + roughnessBias * 0.25;
                        threshold -= depthFactor * profile.CaveDepthWeight * 0.6;
                        threshold += wetnessRetention * 0.15;
                        threshold += edgeFactor * worldConfig.Caves.EdgeSealStrength * 0.35;
                        threshold += Math.Clamp(hydrologyGradient * (worldConfig.Caves.EdgeSealStrength + worldConfig.Caves.HydrologyStabilityWeight * 0.25f), 0.0, 0.35);
                        threshold += Math.Clamp(flowGradient * worldConfig.Caves.EdgeSealStrength * 0.15f, 0.0, 0.25);
                        threshold += varianceBrake * 0.25;
                        threshold += saturationBrake;
                        threshold += ceilingClamp * 0.1;
                        threshold += riparianBridge * 0.35;
                        threshold += flowContinuity * 0.2;
                        threshold += hydrologyShadow * 0.2;
                        threshold += moistureContinuity * 0.25;
                        threshold += flowShadowDrift * 0.1;
                        threshold += aquiferPenalty * 0.2;
                        threshold += aquiferBarrier * 0.25;
                        threshold += slopeThresholdPenalty * 0.5;
                        threshold += divergenceGuard * 0.45;
                        threshold += karstPotential * worldConfig.Caves.CaveEntranceFlowDampening * 0.12;
                        threshold += Math.Clamp((1.0 - depthFactor) * karstPotential * 0.08, 0.0, 0.15);
                        threshold = Math.Clamp(threshold, 0.22, 0.8);

                        if (noise > threshold && stability > 0.08)
                        {
                            mask[x, y, z] = true;
                        }
                    }
                }
            }

            Smooth2D(mask, worldConfig.Caves.StabilitySmoothIterations, worldConfig.Caves.StabilitySmoothBlend);
            ApplyEdgeSeal(mask, hydrology, riverMask, worldConfig.Caves.EdgeSealStrength);
            ApplyRiparianPlugs(mask, hydrology, riverMask, worldConfig.Caves.RiparianPlugDepth);
            AddSupportColumns(mask, hydrology, riverMask);
            ApplyCaveVadoseBypassSeal(mask, hydrology, flowMask, riverMask, heightMap);
            ApplyCaveKarstSpringSeal(mask, hydrology, flowMask, riverMask, heightMap);
            ApplyCaveEpikarstRechargeSeal(mask, hydrology, flowMask, riverMask, heightMap);
            ApplyCaveHyporheicVentSeal(mask, hydrology, flowMask, riverMask, heightMap);
            ApplyCaveGroundwaterPerchSealBridge(mask, hydrology, flowMask, riverMask, heightMap);
            return mask;
        }

        private void ApplyCaveGroundwaterPerchSealBridge(bool[,,] mask, float[,] hydrology, float[,] flowMask, float[,] riverMask, int[,] heightMap)
        {
            double bridgeWeight = Math.Clamp(
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.36 +
                worldConfig.Caves.AquiferBarrierWeight * 0.34 +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.30,
                0.0,
                1.0);
            if (bridgeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(5, worldConfig.Caves.RiparianPlugDepth + 3));
            int bottom = Math.Max(2, seaLevel - Math.Max(7, worldConfig.Caves.RiparianPlugDepth + 4));
            double divergenceClamp = Math.Max(0.0001, worldConfig.Caves.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flow = Math.Clamp(flowMask[x, z], 0.0f, 1.0f);
                    double seamFlow = Math.Clamp(SampleInterior(flowMask, x, z), 0.0f, 1.0f);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(2, worldConfig.Caves.RiparianPlugDepth + 1));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double perchedBand = Math.Clamp(
                        (seaLevel + worldConfig.Caves.RiparianPlugDepth - heightMap[x, z]) / Math.Max(6.0, worldConfig.Caves.RiparianPlugDepth + 8.0),
                        0.0,
                        1.0);
                    double sealSignal = Math.Clamp(
                        hydro * 0.34 +
                        seamHydro * 0.24 +
                        flow * 0.18 +
                        seamFlow * 0.14 +
                        river * 0.10,
                        0.0,
                        1.25);
                    sealSignal *= 1.0 - Math.Clamp(slope * 0.024 + relief / 42.0 + divergence * 0.28, 0.0, 0.82);
                    sealSignal *= 0.7 + perchedBand * 0.3;
                    if (sealSignal <= 0.2)
                    {
                        continue;
                    }

                    int surface = Math.Clamp(heightMap[x, z], bottom + 2, sizeY - 2);
                    for (int y = top; y >= bottom; y--)
                    {
                        if (!mask[x, y, z] || y >= surface)
                        {
                            continue;
                        }

                        int roofThickness = surface - y;
                        if (roofThickness <= 1)
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double lateralFactor = lateralOpen / 4.0;
                        double sealChance = bridgeWeight *
                            sealSignal *
                            (0.38 + lateralFactor * 0.24 + perchedBand * 0.22 + Math.Clamp(roofThickness / 8.0, 0.0, 0.2));
                        if (sealChance > 0.47 || (sealChance > 0.33 && perchedBand > 0.4 && river > 0.28))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyCaveVadoseBypassSeal(bool[,,] mask, float[,] hydrology, float[,] flowMask, float[,] riverMask, int[,] heightMap)
        {
            double sealWeight = Math.Clamp(
                worldConfig.Caves.AquiferBarrierWeight * 0.4 +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.35 +
                worldConfig.Caves.EdgeSealStrength * 0.25,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, worldConfig.Caves.RiparianPlugDepth + 3));
            int bottom = Math.Max(2, seaLevel - Math.Max(6, worldConfig.Caves.RiparianPlugDepth + 4));
            double divergenceClamp = Math.Max(0.0001, worldConfig.Caves.MoistureFlowClamp);

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flow = Math.Clamp(flowMask[x, z], 0.0f, 1.0f);
                    double seamFlow = Math.Clamp(SampleInterior(flowMask, x, z), 0.0f, 1.0f);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double continuity = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flow);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double wetness = Math.Clamp(hydro * 0.38 + seamHydro * 0.22 + flow * 0.2 + seamFlow * 0.1 + river * 0.1, 0.0, 1.2);
                    if (wetness < 0.3)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double bypassRisk = wetness * (worldConfig.Caves.MoistureRetentionWeight * 0.3 + 0.42);
                        bypassRisk += continuity * worldConfig.Caves.EdgeSealStrength * 0.2;
                        bypassRisk += divergence * worldConfig.Caves.FlowStabilityWeight * 0.25;
                        bypassRisk += slope * worldConfig.Caves.CaveCeilingStabilityWeight * 0.015;
                        bypassRisk += Math.Clamp((2 - lateralOpen) * 0.1, 0.0, 0.3);
                        bypassRisk = Math.Clamp(bypassRisk * sealWeight, 0.0, 1.0);

                        if (bypassRisk > 0.56 || (bypassRisk > 0.4 && lateralOpen <= 1))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyCaveKarstSpringSeal(bool[,,] mask, float[,] hydrology, float[,] flowMask, float[,] riverMask, int[,] heightMap)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(1, Math.Min(sizeX, sizeZ) / 4);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, worldConfig.Caves.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(5, worldConfig.Caves.RiparianPlugDepth + 3));
            double divergenceClamp = Math.Max(0.0001, worldConfig.Caves.MoistureFlowClamp);
            double sealWeight = Math.Clamp(
                worldConfig.Caves.AquiferBarrierWeight * 0.38 +
                worldConfig.Caves.MoistureRetentionWeight * 0.34 +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.28,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flow = Math.Clamp(flowMask[x, z], 0.0f, 1.0f);
                    double seamFlow = Math.Clamp(SampleInterior(flowMask, x, z), 0.0f, 1.0f);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double springPotential = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + flow * 0.18 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);

                    if (springPotential < 0.28)
                    {
                        continue;
                    }

                    double continuity = 1.0 - Math.Clamp(
                        hydroGradient * worldConfig.Caves.EdgeSealStrength * 0.35 +
                        flowGradient * worldConfig.Caves.EdgeSealStrength * 0.25 +
                        divergence * worldConfig.Caves.FlowStabilityWeight * 0.25,
                        0.0,
                        0.85);
                    double reliefBrake = 1.0 - Math.Clamp(
                        slope * worldConfig.Caves.CeilingStabilityWeight * 0.02 +
                        relief * worldConfig.Caves.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.55);

                    double springSeal = springPotential * sealWeight * (0.62 + edgeBand * 0.28) * continuity * reliefBrake;
                    if (springSeal < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = springSeal * (0.55 + depthFactor * 0.45);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.08, 0.0, 0.25);

                        if (sealChance > 0.56 || (sealChance > 0.4 && springPotential > 0.45))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyCaveEpikarstRechargeSeal(bool[,,] mask, float[,] hydrology, float[,] flowMask, float[,] riverMask, int[,] heightMap)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(2, Math.Min(sizeX, sizeZ) / 3);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(5, worldConfig.Caves.RiparianPlugDepth + 5));
            int bottom = Math.Max(2, seaLevel - Math.Max(4, worldConfig.Caves.RiparianPlugDepth + 2));
            double divergenceClamp = Math.Max(0.0001, worldConfig.Caves.MoistureFlowClamp);
            double rechargeWeight = Math.Clamp(
                worldConfig.Caves.MoistureRetentionWeight * 0.36 +
                worldConfig.Caves.AquiferBarrierWeight * 0.34 +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.30,
                0.0,
                1.0);
            if (rechargeWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flow = Math.Clamp(flowMask[x, z], 0.0f, 1.0f);
                    double seamFlow = Math.Clamp(SampleInterior(flowMask, x, z), 0.0f, 1.0f);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double recharge = Math.Clamp(
                        hydro * 0.34 + seamHydro * 0.24 + flow * 0.16 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    if (recharge < 0.3)
                    {
                        continue;
                    }

                    double continuityBrake = 1.0 - Math.Clamp(
                        hydroGradient * worldConfig.Caves.EdgeSealStrength * 0.34 +
                        flowGradient * worldConfig.Caves.EdgeSealStrength * 0.24 +
                        divergence * worldConfig.Caves.FlowStabilityWeight * 0.22,
                        0.0,
                        0.8);
                    double rechargeRisk = recharge * rechargeWeight * (0.58 + edgeBand * 0.3);
                    rechargeRisk *= continuityBrake;
                    rechargeRisk *= 1.0 - Math.Clamp(
                        slope * worldConfig.Caves.CeilingStabilityWeight * 0.02 +
                        relief * worldConfig.Caves.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.58);
                    if (rechargeRisk < 0.2)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = rechargeRisk * (0.52 + depthFactor * 0.48);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.07, 0.0, 0.22);

                        if (sealChance > 0.54 || (sealChance > 0.39 && recharge > 0.46))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyCaveHyporheicVentSeal(bool[,,] mask, float[,] hydrology, float[,] flowMask, float[,] riverMask, int[,] heightMap)
        {
            int sizeX = mask.GetLength(0);
            int sizeY = mask.GetLength(1);
            int sizeZ = mask.GetLength(2);
            int edgeRadius = Math.Max(2, Math.Min(sizeX, sizeZ) / 4);
            int top = Math.Min(sizeY - 2, seaLevel + Math.Max(4, worldConfig.Caves.RiparianPlugDepth + 4));
            int bottom = Math.Max(2, seaLevel - Math.Max(4, worldConfig.Caves.RiparianPlugDepth + 4));
            double divergenceClamp = Math.Max(0.0001, worldConfig.Caves.MoistureFlowClamp);
            double sealWeight = Math.Clamp(
                worldConfig.Caves.AquiferBarrierWeight * 0.38 +
                worldConfig.Caves.CaveEntranceFlowDampening * 0.34 +
                worldConfig.Caves.RiparianCaveGuardWeight * 0.28,
                0.0,
                1.0);
            if (sealWeight <= 0.01)
            {
                return;
            }

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius * 2)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flow = Math.Clamp(flowMask[x, z], 0.0f, 1.0f);
                    double seamFlow = Math.Clamp(SampleInterior(flowMask, x, z), 0.0f, 1.0f);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double hydroGradient = Math.Abs(seamHydro - hydro);
                    double flowGradient = Math.Abs(seamFlow - flow);
                    double divergence = Math.Min(1.0, Math.Abs(flow - seamFlow) / divergenceClamp);
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius * 2 + 1), 0.0, 1.0);
                    double hyporheicPotential = Math.Clamp(
                        hydro * 0.32 + seamHydro * 0.24 + flow * 0.2 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);
                    if (hyporheicPotential < 0.3)
                    {
                        continue;
                    }

                    double continuity = 1.0 - Math.Clamp(
                        hydroGradient * worldConfig.Caves.EdgeSealStrength * 0.32 +
                        flowGradient * worldConfig.Caves.EdgeSealStrength * 0.24 +
                        divergence * worldConfig.Caves.FlowStabilityWeight * 0.24,
                        0.0,
                        0.82);
                    double ventRisk = hyporheicPotential * sealWeight * (0.56 + edgeBand * 0.32) * continuity;
                    ventRisk *= 1.0 - Math.Clamp(
                        slope * worldConfig.Caves.CeilingStabilityWeight * 0.018 +
                        relief * worldConfig.Caves.RiverSuppressionWeight * 0.012,
                        0.0,
                        0.55);
                    if (ventRisk < 0.18)
                    {
                        continue;
                    }

                    for (int y = bottom; y <= top; y++)
                    {
                        if (!mask[x, y, z])
                        {
                            continue;
                        }

                        int lateralOpen = 0;
                        if (mask[x - 1, y, z]) lateralOpen++;
                        if (mask[x + 1, y, z]) lateralOpen++;
                        if (mask[x, y, z - 1]) lateralOpen++;
                        if (mask[x, y, z + 1]) lateralOpen++;

                        double depthFactor = 1.0 - Math.Clamp((double)(y - bottom) / Math.Max(1.0, top - bottom), 0.0, 1.0);
                        double sealChance = ventRisk * (0.5 + depthFactor * 0.5);
                        sealChance += Math.Clamp((2 - lateralOpen) * 0.08, 0.0, 0.24);

                        if (sealChance > 0.55 || (sealChance > 0.4 && hyporheicPotential > 0.46))
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyRiverAnabranchBridge(float[,] mask, float[,] hydrology, float[,] flow, int[,] heightMap)
        {
            double branchWeight = Math.Clamp(
                profile.RiverBraidingWeight * 0.4 +
                profile.RiverConfluenceBoost * 0.35 +
                profile.RiverEdgeContinuityWeight * 0.25,
                0.0,
                1.0);
            if (branchWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.15)
                    {
                        continue;
                    }

                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowGradient = Math.Abs(flowNode - seamFlow);
                    double hydroGradient = Math.Abs(hydro - seamHydro);
                    double divergence = Math.Min(1.0, flowGradient / divergenceClamp);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, seaLevel * 1.5);

                    double branchMemory = Math.Clamp(
                        flowNode * 0.4 + seamFlow * 0.25 + hydro * 0.2 + seamHydro * 0.15,
                        0.0,
                        1.25);
                    double branchAssist = branchMemory * branchWeight * (0.18 + profile.RiverSeamFillStrength * 0.18);
                    branchAssist *= 1.0 - Math.Clamp(flowGradient * 0.45 + hydroGradient * 0.25, 0.0, 0.75);
                    double cutoffRisk = Math.Clamp(
                        flowGradient * 0.35 +
                        hydroGradient * 0.25 +
                        slope * profile.RiverGradientPenalty * 0.02 +
                        relief * profile.RiverReliefPenaltyWeight * 0.35,
                        0.0,
                        0.9);
                    double floor = Math.Max(river * (0.82 + profile.RiverEdgeContinuityWeight * 0.08), branchMemory * 0.16);
                    double target = river * (1.0 - cutoffRisk * 0.22) + branchAssist * (0.35 + cutoffRisk * 0.2);
                    mask[x, z] = Mathf.Clamp((float)Math.Max(target, floor), 0f, 1.35f);
                }
            }
        }

        private void ApplyRiverCutoffDamping(float[,] mask, float[,] hydrology, float[,] flow, int[,] heightMap)
        {
            double dampingWeight = Math.Clamp(
                profile.RiverBankStabilityClamp * 0.38 +
                profile.RiverEdgeContinuityWeight * 0.34 +
                profile.RiverMeanderJitter * 0.28,
                0.0,
                1.0);
            if (dampingWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, profile.HydrologyEdgeBlendRadius + 1);
            int mouthRadius = Math.Max(2, profile.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.1)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double seaBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.2), 0.0, 1.0);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double gradient = ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double cutoffRisk = Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.26 +
                        gradient * profile.RiverGradientPenalty * 0.02 +
                        edgeBand * 0.18,
                        0.0,
                        1.0);

                    double convergence = Math.Clamp(
                        (flowNode + seamFlow + hydro + seamHydro) * 0.25,
                        0.0,
                        1.2);
                    convergence *= 1.0 - Math.Clamp(cutoffRisk * 0.55, 0.0, 0.8);

                    double blend = dampingWeight * (0.5 + seaBand * 0.25 + edgeBand * 0.25);
                    double floor = Math.Max(river * (0.82 + profile.RiverEdgeContinuityWeight * 0.1), convergence * 0.15);
                    double target = river * (1.0 - blend * 0.2) + convergence * blend * 0.45;
                    target *= 1.0 - Math.Clamp(cutoffRisk * 0.35, 0.0, 0.35);
                    mask[x, z] = Mathf.Clamp((float)Math.Max(target, floor), 0f, 1.35f);
                }
            }
        }

        private void ApplyRiverDistributaryLeveeBridge(float[,] mask, float[,] hydrology, float[,] flow, int[,] heightMap)
        {
            double leveeWeight = Math.Clamp(
                profile.RiverEdgeContinuityWeight * 0.38 +
                profile.RiverBankStabilityClamp * 0.34 +
                profile.RiverDeltaWetlandStrength * 0.28,
                0.0,
                1.0);
            if (leveeWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int edgeRadius = Math.Max(2, profile.HydrologyEdgeBlendRadius + 1);
            int mouthRadius = Math.Max(2, profile.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.08)
                    {
                        continue;
                    }

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double mouthBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.5), 0.0, 1.0);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, seaLevel * 1.5);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double leveeSeed = Math.Clamp(
                        hydro * 0.28 + seamHydro * 0.24 + flowNode * 0.24 + seamFlow * 0.24,
                        0.0,
                        1.2);
                    double leveeContinuity = 1.0 - Math.Clamp(
                        divergence * 0.44 +
                        Math.Abs(hydro - seamHydro) * 0.26 +
                        slope * profile.RiverGradientPenalty * 0.02,
                        0.0,
                        0.78);
                    double leveeBridge = leveeSeed * leveeWeight * (0.46 + edgeBand * 0.28 + mouthBand * 0.26) * leveeContinuity;
                    leveeBridge *= 1.0 - Math.Clamp(relief * profile.RiverReliefPenaltyWeight * 0.32, 0.0, 0.4);
                    double floor = Math.Max(river * (0.82 + profile.RiverEdgeContinuityWeight * 0.11), leveeSeed * 0.16);
                    double target = river * (1.0 - leveeWeight * 0.18) + leveeBridge * 0.5;
                    mask[x, z] = Mathf.Clamp((float)Math.Max(target, floor), 0f, 1.35f);
                }
            }
        }

        private void ApplyRiverEstuaryConvergenceBridge(float[,] mask, float[,] hydrology, float[,] flow, int[,] heightMap)
        {
            double estuaryWeight = Math.Clamp(
                profile.RiverDeltaWetlandStrength * 0.34 +
                profile.RiverConfluenceBoost * 0.33 +
                profile.RiverEdgeContinuityWeight * 0.33,
                0.0,
                1.0);
            if (estuaryWeight <= 0.01)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            int mouthRadius = Math.Max(2, profile.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.08)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.8),
                        0.0,
                        1.0);
                    if (seaBand <= 0.02)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, profile.HydrologyEdgeBlendRadius));
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(Math.Max(1, profile.HydrologyEdgeBlendRadius) + 1), 0.0, 1.0);

                    double estuarySeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.25 + flowNode * 0.24 + seamFlow * 0.21,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.4 +
                        Math.Abs(hydro - seamHydro) * 0.2 +
                        slope * profile.RiverGradientPenalty * 0.02,
                        0.0,
                        0.78);
                    double convergence = estuarySeed * estuaryWeight * (0.52 + seaBand * 0.34 + edgeBand * 0.14);
                    convergence *= continuity;
                    convergence *= 1.0 - Math.Clamp(relief * profile.RiverReliefPenaltyWeight * 0.012, 0.0, 0.35);

                    double floor = Math.Max(river * (0.84 + profile.RiverEdgeContinuityWeight * 0.08), estuarySeed * 0.14);
                    double target = river * (1.0 - estuaryWeight * 0.18) + convergence * 0.5;
                    mask[x, z] = Mathf.Clamp((float)Math.Max(target, floor), 0f, 1.35f);
                }
            }
        }

        private void ApplyLakeFloodplainTerraceBridge(float[,] lakes, float[,] hydrology, float[,] flow, float[,] riverMask, int[,] heightMap)
        {
            double terraceWeight = Math.Clamp(
                profile.LakeShorelineBlend * 0.35 +
                profile.LakeOutflowStabilityWeight * 0.35 +
                profile.RiverDeltaWetlandStrength * 0.3,
                0.0,
                1.0);
            if (terraceWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int terraceBand = Math.Max(2, profile.RiverMouthSmoothRadius + profile.LakeShelfDepth);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.08)
                    {
                        continue;
                    }

                    double elevation = Math.Abs(heightMap[x, z] - seaLevel);
                    if (elevation > terraceBand * 3.0)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double riverAssist = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double bandBlend = 1.0 - Math.Clamp(elevation / Math.Max(1.0, terraceBand * 3.0), 0.0, 1.0);
                    double terraceSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.2 + flowNode * 0.2 + seamFlow * 0.15 + riverAssist * 0.15,
                        0.0,
                        1.2);

                    double terrace = terraceSeed * terraceWeight * (0.35 + bandBlend * 0.4);
                    terrace *= 1.0 - Math.Clamp(divergence * 0.35 + slope * profile.LakeRimErosionWeight * 0.02, 0.0, 0.65);
                    double floor = Math.Max(lake * (0.84 + profile.HydrologyContinuityWeight * 0.08), terraceSeed * 0.14);
                    double target = lake * (1.0 - terraceWeight * 0.12) + terrace * 0.35;
                    lakes[x, z] = Mathf.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyLakeTerraceBackfillBridge(float[,] lakes, float[,] hydrology, float[,] flow, float[,] riverMask, int[,] heightMap)
        {
            double backfillWeight = Math.Clamp(
                worldConfig.Lakes.SpillwayContinuityWeight * 0.36 +
                profile.LakeOutflowStabilityWeight * 0.34 +
                profile.LakeOutflowTaper * 0.30,
                0.0,
                1.0);
            if (backfillWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int terraceBand = Math.Max(2, Math.Max(profile.LakeShelfDepth, profile.HydrologyEdgeBlendRadius));
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.08)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, terraceBand * 3.0),
                        0.0,
                        1.0);
                    if (seaBand <= 0.01)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double terraceSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    double terraceContinuity = 1.0 - Math.Clamp(
                        divergence * 0.42 + slope * profile.LakeRimErosionWeight * 0.02,
                        0.0,
                        0.75);
                    double terraceBackfill = terraceSeed * backfillWeight * (0.45 + seaBand * 0.35) * terraceContinuity;
                    double floor = Math.Max(lake * (0.84 + profile.LakeOutflowStabilityWeight * 0.08), terraceSeed * 0.14);
                    double target = lake * (1.0 - backfillWeight * 0.15) + terraceBackfill * 0.45;
                    lakes[x, z] = Mathf.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyLakeDeltaBackswampBridge(float[,] lakes, float[,] hydrology, float[,] flow, float[,] riverMask, int[,] heightMap)
        {
            double retentionWeight = Math.Clamp(
                worldConfig.Lakes.SpillwayContinuityWeight * 0.36 +
                profile.LakeOutflowStabilityWeight * 0.34 +
                profile.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (retentionWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, profile.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.1)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.8),
                        0.0,
                        1.0);
                    if (seaBand <= 0.02)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double backswampSeed = Math.Clamp(
                        hydro * 0.32 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.14 + river * 0.1,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.24 +
                        slope * profile.LakeRimErosionWeight * 0.018,
                        0.0,
                        0.76);
                    double retention = backswampSeed * retentionWeight * (0.5 + seaBand * 0.4) * continuity;
                    double floor = Math.Max(lake * (0.83 + profile.LakeOutflowStabilityWeight * 0.1), backswampSeed * 0.16);
                    double target = lake * (1.0 - retentionWeight * 0.2) + retention * 0.52;
                    lakes[x, z] = Mathf.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyLakeLagoonOverflowBridge(float[,] lakes, float[,] hydrology, float[,] flow, float[,] riverMask, int[,] heightMap)
        {
            double overflowWeight = Math.Clamp(
                profile.LakeOutflowStabilityWeight * 0.36 +
                worldConfig.Lakes.SpillwayContinuityWeight * 0.34 +
                profile.RiverDeltaWetlandStrength * 0.30,
                0.0,
                1.0);
            if (overflowWeight <= 0.01)
            {
                return;
            }

            int sizeX = lakes.GetLength(0);
            int sizeZ = lakes.GetLength(1);
            int mouthRadius = Math.Max(2, profile.RiverMouthSmoothRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            var copy = (float[,])lakes.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.1)
                    {
                        continue;
                    }

                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.9),
                        0.0,
                        1.0);
                    if (seaBand <= 0.02)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.0);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, profile.HydrologyEdgeBlendRadius));
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double lagoonSeed = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.16 + river * 0.1,
                        0.0,
                        1.2);
                    double continuity = 1.0 - Math.Clamp(
                        divergence * 0.42 +
                        Math.Abs(hydro - seamHydro) * 0.22 +
                        slope * profile.LakeRimErosionWeight * 0.02,
                        0.0,
                        0.75);
                    double overflow = lagoonSeed * overflowWeight * (0.5 + seaBand * 0.36) * continuity;
                    overflow *= 1.0 - Math.Clamp(relief * profile.RiverReliefPenaltyWeight * 0.012, 0.0, 0.35);

                    double floor = Math.Max(lake * (0.83 + profile.LakeOutflowStabilityWeight * 0.1), lagoonSeed * 0.15);
                    double target = lake * (1.0 - overflowWeight * 0.2) + overflow * 0.52;
                    lakes[x, z] = Mathf.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private float[,] BuildRiverMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            var mask = new float[chunkSize, chunkSize];
            double noiseScale = Math.Max(0.0001, profile.RiverNoiseScale);
            double confluenceBoost = Math.Clamp(profile.RiverConfluenceBoost, 0.0, 2.0);
            double waterTableClampWeight = Math.Clamp(profile.HydrologyWaterTableClampWeight, 0.0f, 1.0f);
            double waterTableClampRange = Math.Max(1.0, profile.HydrologyWaterTableClampRange);
            double waterTableSlopeWeight = Math.Clamp(profile.HydrologyWaterTableSlopeWeight, 0.0f, 1.0f);
            double depthBias = Math.Clamp(profile.RiverDepth / 12.0, 0.0, 1.0);
            double anisotropyDamping = Math.Clamp(profile.RiverAnisotropyDamping, 0.0, 1.0);
            double bankStabilityClamp = Math.Clamp(profile.RiverBankStabilityClamp, 0.0, 1.0);
            double catchmentWeight = Math.Clamp(worldConfig.Water.HydrologyCatchmentWeight, 0.0, 1.0);
            double braidingWeight = Math.Clamp(worldConfig.Water.RiverBraidingWeight, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;
                    float warp = Mathf.PerlinNoise(worldX * profile.HydrologyWarpFrequency, worldZ * profile.HydrologyWarpFrequency);
                    double baseNoise = Math.Abs(Mathf.PerlinNoise((float)(worldX * noiseScale + warp), (float)(worldZ * noiseScale + warp * 0.5f)) - 0.5f) * 2.0;

                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double flowMemory = SampleInterior(flow, x, z) / 6.0;
                    double gradient = ComputeSlope(heightMap, x, z);
                    double relief = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    double directionality = (Math.Abs(downhill.x) + Math.Abs(downhill.y)) * 0.5;
                    double flowAlignment = 1.0 + Math.Clamp(flowSample * profile.RiverFlowAlignmentWeight * 0.35, 0.0, 0.45);
                    double seamHydro = SampleInterior(hydrology, x, z);
                    double seamStitch = 1.0 + Math.Clamp((seamHydro - hydrologySample) * profile.HydrologyEdgeFluxBlend, -0.35, 0.35);
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double flowDrift = Math.Abs(flowMemory - flowSample);
                    double flowShadow = Math.Clamp(
                        flowSample * profile.HydrologyFlowShadowWeight +
                        hydrologyGradient * profile.HydrologyFlowShadowSlopeWeight * 0.5,
                        0.0,
                        0.75);
                    double hydrologyShadow = Math.Clamp(flowShadow + hydrologySample * profile.HydrologyFlowShadowWeight * 0.25, 0.0, 0.85);
                    double divergencePenalty = Math.Min(1.0, flowDrift / Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp));
                    double pressureGradient = Math.Abs(hydrologyGradient - flowDrift);
                    double braidedAssist = Math.Clamp((hydrologyGradient + Math.Abs(flowSample - seamHydro)) * profile.HydrologyFlowPersistence * 0.15, 0.0, 0.25);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double erosionMemory = Math.Clamp(SampleInterior(erosionRisk, x, z), 0.0f, 1.0f);
                    double seamGuard = 1.0 - Math.Clamp(hydrologyGradient * profile.HydrologyEdgeStabilityWeight * 0.25, 0.0, 0.35);

                    double riverMask = profile.RiverBankThreshold - baseNoise - erosion * profile.RiverBankErosionWeight * 0.08;
                    double pressure = Math.Max(0.0, riverMask);
                    pressure *= 1.0 + hydrologySample * profile.HydrologyContinuityWeight;
                    pressure *= 1.0 + flowSample * profile.RiverFlowAlignmentWeight;
                    double anisotropyPenalty = 1.0 - Math.Clamp(gradient * anisotropyDamping * 0.05 + relief * anisotropyDamping * 0.1, 0.0, 0.45);
                    pressure *= (1.0 + directionality * profile.RiverAnisotropyWeight * 0.2) * anisotropyPenalty;
                    pressure *= 1.0 - Math.Clamp(gradient * profile.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * profile.RiverReliefPenaltyWeight, 0.0, 0.35);
                    double bankClamp = 1.0 - Math.Clamp((gradient + relief) * bankStabilityClamp * 0.08, 0.0, 0.55);
                    pressure *= bankClamp;
                    pressure *= flowAlignment * seamStitch;
                    pressure *= 1.0 + (flowMemory + hydrologySample) * profile.HydrologyFlowPersistence * 0.2;
                    pressure *= 1.0 - Math.Clamp(erosion * profile.RiverBankErosionWeight * 0.45, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp((hydrologyGradient + erosionMemory) * profile.HydrologyEdgeStabilityWeight * 0.2, 0.0, 0.35);
                    double waterTableDistance = seaLevel - heightMap[x, z];
                    double waterBias = 1.0 - Math.Clamp(Math.Abs(waterTableDistance) / waterTableClampRange, 0.0, 1.0);
                    double waterClamp = 1.0 + waterBias * waterTableClampWeight * (waterTableDistance >= 0 ? 0.45 : -0.25);
                    double waterSlopePenalty = Math.Clamp(gradient * waterTableSlopeWeight * 0.05, 0.0, 0.45);
                    double waterMemory = (hydrologySample + seamHydro + flowMemory) * waterTableClampWeight * 0.08;
                    pressure *= Math.Max(0.65, waterClamp);
                    pressure *= 1.0 - waterSlopePenalty;
                    pressure *= 1.0 + waterMemory;
                    pressure *= 1.0 + depthBias * 0.05;
                    pressure *= 1.0 - Math.Clamp(divergencePenalty * 0.35, 0.0, 0.35);
                    double braidingAssist = Math.Clamp((hydrologyGradient + Math.Abs(flowSample - seamHydro) + divergencePenalty) * braidingWeight * 0.25, 0.0, 0.35);
                    pressure = pressure * (1.0 - (braidedAssist + braidingAssist) * 0.25) + (braidedAssist + braidingAssist) * 0.08;
                    double pressureStabilizer = 1.0 - Math.Clamp(
                        (pressureGradient / Math.Max(0.0001, profile.HydrologyPressureGradientClamp)) * Math.Clamp(profile.HydrologyPressureBlend, 0.0, 1.0),
                        0.0,
                        0.45);
                    pressure *= Math.Max(0.55, pressureStabilizer);
                    pressure = pressure * (1.0 - hydrologyShadow * 0.25) + (hydrologySample + seamHydro) * hydrologyShadow * 0.15;
                    pressure *= seamGuard;
                    if (confluenceBoost > 0.0)
                    {
                        double neighbourFlow = SampleInterior(flow, x, z) / 6.0;
                        double tributary = Math.Clamp((flowSample + neighbourFlow) * 0.5, 0.0, 1.0);
                        double hydrologyAssist = hydrologySample * 0.5 + hydrologyGradient * 0.15;
                        pressure *= 1.0 + (tributary + hydrologyAssist) * confluenceBoost * 0.35;
                    }

                    double floodplain = Math.Clamp((hydrologySample + hydrologyGradient + flowMemory) * profile.RiverDeltaWetlandStrength * 0.25, 0.0, 0.6);
                    double varianceAssist = Math.Clamp((hydrologyGradient + flowSample) * profile.HydrologyVarianceBlend * 0.1, -0.25, 0.35);
                    pressure = pressure * (1.0 - floodplain * 0.2) + floodplain * 0.1;
                    pressure *= 1.0 + varianceAssist;
                    pressure *= 1.0 - Math.Clamp(erosion * profile.RiverReliefPenaltyWeight * 0.35, 0.0, 0.35);
                    pressure *= 1.0 - Math.Clamp((erosionMemory + erosion) * profile.RiverBankErosionWeight * 0.25, 0.0, 0.35);
                    double floodplainAnchor = Math.Clamp(
                        (hydrologySample + seamHydro + flowSample + flowMemory) * profile.RiverDeltaWetlandStrength * 0.2,
                        0.0,
                        0.7);
                    double avulsionPotential = Math.Clamp(
                        (hydrologyGradient + Math.Abs(flowSample - seamHydro) + erosion) * profile.RiverConfluenceBoost * 0.2,
                        0.0,
                        0.65);
                    double bankCohesion = 1.0 - Math.Clamp(
                        (gradient + erosion) * profile.RiverBankStabilityClamp * 0.1,
                        0.0,
                        0.55);
                    pressure = pressure * (1.0 - avulsionPotential * 0.18) + floodplainAnchor * avulsionPotential * 0.12;
                    pressure *= bankCohesion;
                    double catchmentAssist = Math.Clamp((seamHydro + flowMemory + Math.Max(0.0, seaLevel - heightMap[x, z]) * 0.02) * catchmentWeight * 0.2, 0.0, 0.4);
                    pressure = pressure * (1.0 - catchmentWeight * 0.15) + catchmentAssist * catchmentWeight * 0.35;

                    double headwater = 1.0 - Math.Clamp(flowSample * profile.RiverHeadwaterStabilityWeight, 0.0, 0.65);
                    pressure *= 1.0 + headwater * 0.1;
                    double deltaBlend = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, profile.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    pressure *= 1.0 + deltaBlend * profile.RiverDeltaWetlandStrength * 0.5;
                    pressure = ApplyEdgeBlend(pressure, hydrology[x, z], x, z);

                    mask[x, z] = Mathf.Clamp((float)pressure, 0f, 1.35f);
                }
            }

            ClampVariance(mask, profile.HydrologyVarianceClamp);
            Smooth2D(mask, profile.RiverIntensitySmoothIterations, profile.RiverIntensitySmoothBlend);
            DirectionalSmooth(heightMap, mask, Math.Max(1, profile.HydrologyDirectionalIterations), profile.HydrologyDirectionalBlend * 0.35f);
            StabilizeEdges(mask, profile.HydrologyEdgeBlendRadius, 1, profile.RiverEdgeFeather, profile.RiverSeamFillStrength);
            RelaxEdges(mask, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(mask, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            ApplyRiverAnabranchBridge(mask, hydrology, flow, heightMap);
            ApplyRiverCutoffDamping(mask, hydrology, flow, heightMap);
            ApplyRiverDistributaryLeveeBridge(mask, hydrology, flow, heightMap);
            ApplyRiverEstuaryConvergenceBridge(mask, hydrology, flow, heightMap);
            ApplyRiverGroundwaterExchangeBridge(mask, hydrology, flow, heightMap);
            return mask;
        }

        private void ApplyRiverGroundwaterExchangeBridge(float[,] mask, float[,] hydrology, float[,] flow, int[,] heightMap)
        {
            double exchangeWeight = Math.Clamp(
                profile.HydrologyFlowPersistence * 0.34 +
                profile.RiverEdgeContinuityWeight * 0.33 +
                profile.RiverTributaryCaptureWeight * 0.33,
                0.0,
                1.0);
            if (exchangeWeight <= 0.01)
            {
                return;
            }

            var copy = (float[,])mask.Clone();
            int reliefRadius = Math.Max(2, profile.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);

            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    double river = copy[x, z];
                    if (river <= 0.03)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.25);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.25);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double groundwaterBand = Math.Clamp(
                        (seaLevel + profile.RiverMouthSmoothRadius * 0.5 - heightMap[x, z]) / Math.Max(8.0, profile.HydrologyWaterTableClampRange),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double exchangeSignal = Math.Clamp(
                        hydro * 0.28 +
                        seamHydro * 0.24 +
                        flowNode * 0.2 +
                        seamFlow * 0.18 +
                        groundwaterBand * 0.1,
                        0.0,
                        1.25);
                    exchangeSignal *= 1.0 - Math.Clamp(slope * 0.024 + relief / 44.0 + divergence * 0.28, 0.0, 0.82);
                    if (exchangeSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(river * (0.85 + profile.RiverEdgeContinuityWeight * 0.08), exchangeSignal * 0.18);
                    double target = river * (1.0 - exchangeWeight * 0.12) + (river + exchangeSignal) * exchangeWeight * 0.12;
                    mask[x, z] = Mathf.Clamp((float)Math.Max(target, floor), 0f, 1.35f);
                }
            }
        }

        private double ApplyEdgeBlend(double pressure, float hydrology, int x, int z)
        {
            int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            if (edgeDistance >= edgeRadius)
            {
                return pressure;
            }

            double blend = 1.0 - edgeDistance / (double)(edgeRadius + 1);
            double seamFill = Math.Clamp(profile.RiverSeamFillStrength, 0.0, 1.0);
            double hydrologyPull = hydrology * seamFill * blend;
            return pressure * (1.0 - hydrologyPull) + hydrologyPull;
        }

        private float[,] BuildLakeMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk, float[,] riverMask)
        {
            var lakes = new float[chunkSize, chunkSize];
            double flowSeepageWeight = Math.Clamp(worldConfig.Lakes.FlowSeepageWeight, 0.0, 1.0);
            int minDepth = Math.Max(1, worldConfig.Lakes.MinDepth);
            int maxDepth = Math.Max(minDepth, worldConfig.Lakes.MaxDepth);
            int shelfDepth = Math.Max(0, profile.LakeShelfDepth);
            double waterTableClampWeight = Math.Clamp(profile.HydrologyWaterTableClampWeight, 0.0f, 1.0f);
            double waterTableClampRange = Math.Max(1.0, profile.HydrologyWaterTableClampRange);
            double waterTableSlopeWeight = Math.Clamp(profile.HydrologyWaterTableSlopeWeight, 0.0f, 1.0f);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            double flowPersistence = Math.Clamp(profile.HydrologyFlowPersistence, 0.0, 1.0);
            double outflowSealWeight = Math.Clamp(profile.LakeOutflowSealWeight, 0.0, 1.0);
            double spillwayContinuityWeight = Math.Clamp(worldConfig.Lakes.SpillwayContinuityWeight, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float riverPressure = riverMask[x, z];
                    if (riverPressure > profile.LakeRiverProximitySuppression)
                    {
                        continue;
                    }

                    int worldX = chunkPos.x * chunkSize + x;
                    int worldZ = chunkPos.y * chunkSize + z;

                    double basinNoise = Mathf.PerlinNoise(worldX * 0.004f, worldZ * 0.004f);
                    double rimNoise = Mathf.PerlinNoise(worldX * 0.009f + 31f, worldZ * 0.009f + 17f);
                    double hydrologySample = hydrology[x, z];
                    double flowSample = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double flowMemory = SampleInterior(flow, x, z) / 6.0;
                    double seamHydro = SampleInterior(hydrology, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    double downhillBias = Math.Abs(downhill.x) + Math.Abs(downhill.y);
                    double slope = ComputeSlope(heightMap, x, z);
                    double reliefPenalty = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
                    double radiusFalloff = Math.Clamp(edgeDistance / (double)Math.Max(1, profile.LakeMaxRadius), 0.0, 1.0);
                    double inflowBlend = riverPressure * profile.LakeInflowBlendWeight;
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double flowGradient = Math.Abs(SampleInterior(flow, x, z) - flowSample);
                    double pressureGradient = Math.Abs(hydrologyGradient - flowGradient);
                    double divergencePenalty = Math.Min(1.0, flowGradient / divergenceClamp);
                    double flowShadow = Math.Clamp(
                        flowSample * worldConfig.Water.HydrologyFlowShadowWeight +
                        hydrologyGradient * worldConfig.Water.HydrologyFlowShadowSlopeWeight * 0.5,
                        0.0,
                        0.7);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double erosionMemory = Math.Clamp(SampleInterior(erosionRisk, x, z), 0.0f, 1.0f);
                    double depthBelowSea = seaLevel - heightMap[x, z];
                    double depthPenalty = Math.Clamp(Math.Max(0.0, minDepth - depthBelowSea) / Math.Max(1.0, minDepth), 0.0, 1.0);
                    double waterClamp = 1.0 + Math.Clamp(1.0 - Math.Abs(depthBelowSea) / waterTableClampRange, 0.0, 1.0) * waterTableClampWeight * (depthBelowSea >= 0 ? 0.45 : -0.25);
                    double waterSlopePenalty = Math.Clamp(slope * waterTableSlopeWeight * 0.05, 0.0, 0.45);
                    double shorelineJitter = Math.Abs(Mathf.PerlinNoise(worldX * 0.0025f + 7f, worldZ * 0.0025f - 13f)) * profile.LakeShorelineBlend * 0.25f;
                    double wetness = hydrologySample * 0.65 + flowSample * 0.35;
                    double weight = (basinNoise * 0.45) + (rimNoise * 0.25) + wetness * 0.4 + profile.LakeSpawnWeightBias;
                    weight += inflowBlend * 0.35 * (1.0 - flowShadow * 0.5);
                    double seamContinuity = 1.0 + (seamHydro + flowMemory) * flowSeepageWeight * 0.2;
                    double seepage = (flowSample + hydrologyGradient + flowMemory * 0.5) * flowSeepageWeight;
                    double momentumAssist = (seamHydro + flowMemory) * flowPersistence * 0.08;
                    weight += seepage * (1.0 - flowShadow * 0.5);
                    weight += momentumAssist * (1.0 - divergencePenalty * 0.35);
                    double outflowAnchor = (seamHydro + flowMemory) * profile.LakeOutflowStabilityWeight * 0.1;
                    outflowAnchor *= 1.0 + outflowSealWeight * (1.0 - divergencePenalty) * 0.35;
                    outflowAnchor *= 1.0 - flowShadow * 0.4;
                    weight += outflowAnchor;
                    double catchmentConnectivity = Math.Clamp((seamHydro + flowMemory + flowSample) / 3.0, 0.0, 1.2);
                    double connectivityAssist = catchmentConnectivity *
                        (profile.RiverConfluenceBoost * 0.12 + profile.LakeOutflowStabilityWeight * 0.2);
                    weight += connectivityAssist * (1.0 - flowShadow * 0.35);
                    weight *= 1.0 + catchmentConnectivity * spillwayContinuityWeight * 0.08;
                    weight *= 1.0 - Math.Clamp(flowGradient * spillwayContinuityWeight * 0.18, 0.0, 0.25);
                    weight *= 1.0 - Math.Clamp(Math.Abs(catchmentConnectivity - wetness) * 0.15, 0.0, 0.25);
                    double varianceAssist = Math.Clamp((hydrologyGradient + flowGradient) * profile.HydrologyVarianceBlend * 0.1, -0.25, 0.35);
                    double seamNormalization = 1.0 - Math.Clamp(hydrologyGradient * profile.HydrologyEdgeNormalizationBlend, 0.0, 0.55);
                    double flowConsistency = 1.0 - Math.Clamp(Math.Abs(flowMemory - flowSample) * profile.HydrologyEdgeNormalizationBlend, 0.0, 0.5);
                    weight -= slope * profile.LakeRimErosionWeight * 0.05;
                    weight -= erosion * profile.LakeRimErosionWeight * 0.35;
                    weight -= erosionMemory * profile.LakeRimErosionWeight * 0.2;
                    weight -= hydrologyGradient * profile.HydrologyEdgeStabilityWeight * 0.25;
                    weight -= riverPressure * 0.5;
                    weight -= reliefPenalty * profile.RiverReliefPenaltyWeight;
                    weight *= 1.0 - divergencePenalty * 0.25;
                    double pressureStabilizer = 1.0 - Math.Clamp(
                        (pressureGradient / Math.Max(0.0001, profile.HydrologyPressureGradientClamp)) * Math.Clamp(profile.HydrologyPressureBlend, 0.0, 1.0),
                        0.0,
                        0.45);
                    weight *= Math.Max(0.55, pressureStabilizer);
                    weight *= Math.Max(0.55, waterClamp);
                    weight *= 1.0 - waterSlopePenalty;
                    weight *= 1.0 - depthPenalty * 0.6;
                    weight += shorelineJitter * (1.0 - flowShadow * 0.5);
                    weight *= 0.75 + radiusFalloff * 0.25;
                    double seamCushion = 1.0 + Math.Clamp((seamHydro - hydrologySample) * profile.HydrologyEdgeFluxBlend, -0.2, 0.3);
                    weight *= seamCushion * seamContinuity;
                    weight += varianceAssist * 0.25;
                    weight *= seamNormalization;
                    weight *= Math.Clamp(flowConsistency, 0.6, 1.05);
                    weight *= 1.0 - flowShadow * Math.Clamp(0.35 - outflowSealWeight * 0.1, 0.05, 0.35);
                    weight *= 1.0 + downhillBias * 0.02;

                    double seamRelax = Math.Clamp(profile.HydrologySeamRelaxBlend, 0.0, 1.0);
                    double wetlandThreshold = profile.LakeWetlandSaturationThreshold - hydrologySample * 0.05 - seamRelax * 0.05;
                    if (weight > wetlandThreshold && depthBelowSea <= maxDepth && depthBelowSea >= -shelfDepth)
                    {
                        lakes[x, z] = Mathf.Clamp01((float)weight);
                    }
                }
            }

            ClampVariance(lakes, profile.HydrologyVarianceClamp);
            Smooth2D(lakes, profile.LakeBasinSmoothIterations, profile.HydrologySmoothBlend);
            StitchEdges(lakes, profile.HydrologySeamRelaxBlend * 0.65f);
            FillBasins(lakes, Mathf.Max(0.05f, profile.HydrologyEdgeStabilityWeight * 0.35f), Math.Max(1, profile.HydrologySeamRelaxIterations));
            RelaxEdges(lakes, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(lakes, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            ApplyLakeShelves(lakes, heightMap, seaLevel, shelfDepth, maxDepth);
            ApplyRiparianBuffer(lakes, Math.Min(profile.LakeWetlandBufferRadius, profile.LakeMaxRadius), profile.LakeShorelineBlend);
            ApplyOutflowChannels(lakes, heightMap, flow, profile.LakeInflowBlendWeight, profile.LakeOutflowCarveDepth);
            ApplyLakeFloodplainTerraceBridge(lakes, hydrology, flow, riverMask, heightMap);
            ApplyLakeTerraceBackfillBridge(lakes, hydrology, flow, riverMask, heightMap);
            ApplyLakeDeltaBackswampBridge(lakes, hydrology, flow, riverMask, heightMap);
            ApplyLakeLagoonOverflowBridge(lakes, hydrology, flow, riverMask, heightMap);
            ApplyLakeGroundwaterLatchBridge(lakes, hydrology, flow, riverMask, heightMap);
            return lakes;
        }

        private void ApplyLakeGroundwaterLatchBridge(float[,] lakes, float[,] hydrology, float[,] flow, float[,] riverMask, int[,] heightMap)
        {
            double latchWeight = Math.Clamp(
                profile.LakeOutflowStabilityWeight * 0.36 +
                profile.HydrologyFlowMemoryWeight * 0.34 +
                worldConfig.Lakes.SpillwayContinuityWeight * 0.30,
                0.0,
                1.0);
            if (latchWeight <= 0.01)
            {
                return;
            }

            var copy = (float[,])lakes.Clone();
            int reliefRadius = Math.Max(2, profile.HydrologyWatershedStitchRadius + 1);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);

            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    double lake = copy[x, z];
                    if (lake <= 0.04)
                    {
                        continue;
                    }

                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double seamHydro = Math.Clamp(SampleInterior(hydrology, x, z), 0.0f, 1.0f);
                    double flowNode = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.3);
                    double seamFlow = Math.Clamp(SampleInterior(flow, x, z) / 6.0, 0.0, 1.3);
                    double river = Math.Clamp(riverMask[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, reliefRadius);
                    double waterTableBand = Math.Clamp(
                        (seaLevel + profile.LakeOutflowCarveDepth - heightMap[x, z]) / Math.Max(8.0, profile.HydrologyWaterTableClampRange),
                        0.0,
                        1.0);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double latchSignal = Math.Clamp(
                        hydro * 0.26 +
                        seamHydro * 0.24 +
                        flowNode * 0.2 +
                        seamFlow * 0.18 +
                        waterTableBand * 0.12,
                        0.0,
                        1.25);
                    latchSignal *= 1.0 - Math.Clamp(slope * 0.026 + relief / 46.0 + divergence * 0.28, 0.0, 0.84);
                    if (latchSignal <= 0.01)
                    {
                        continue;
                    }

                    double floor = Math.Max(lake * (0.85 + profile.LakeOutflowSealWeight * 0.08), latchSignal * 0.18);
                    double target = lake * (1.0 - latchWeight * 0.13) + (lake + latchSignal) * latchWeight * 0.13;
                    if (river > 0.52)
                    {
                        target *= 1.0 - Math.Clamp((river - 0.52) * 0.14, 0.0, 0.14);
                    }

                    lakes[x, z] = Mathf.Clamp01((float)Math.Max(target, floor));
                }
            }
        }

        private void ApplyHydrologyToHeight(int[,] heightMap, float[,] riverMask, float[,] lakeMask, float[,] hydrology, float[,] flow)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float river = riverMask[x, z];
                    float lake = lakeMask[x, z];
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    int surface = heightMap[x, z];
                    bool hasRiver = river > profile.RiverCenterThreshold;
                    bool hasLake = lake > profile.LakeShorelineBlend;

                    if (hasRiver)
                    {
                        int depth = Mathf.Clamp(Mathf.RoundToInt(profile.RiverDepth * (river + hydro * 0.5f + flowValue * 0.35f)), 2, profile.RiverDepth + 3);
                        heightMap[x, z] = Mathf.Max(surface - depth, seaLevel - depth);
                    }
                    else if (hasLake)
                    {
                        int depth = Mathf.Clamp(Mathf.RoundToInt(profile.LakeShelfDepth + lake * profile.LakeOutflowCarveDepth + hydro * 2f), profile.LakeOutflowCarveDepth, Mathf.Max(profile.LakeShelfDepth, profile.LakeOutflowCarveDepth + profile.LakeWetlandBufferRadius));
                        heightMap[x, z] = Mathf.Max(surface - depth, seaLevel - depth);
                    }
                    else if (hydro > 0.65f && flowValue > 0.35f)
                    {
                        int depth = Mathf.Clamp(Mathf.CeilToInt((hydro + flowValue) * 2f), 1, profile.LakeOutflowCarveDepth);
                        heightMap[x, z] = Mathf.Max(surface - depth, seaLevel - depth);
                    }
                }
            }
        }

        private void ApplyLakeShelves(float[,] field, int[,] heightMap, int seaLevel, int shelfDepth, int maxDepth)
        {
            shelfDepth = Mathf.Max(0, shelfDepth);
            if (shelfDepth == 0)
            {
                return;
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float value = field[x, z];
                    if (value <= 0f)
                    {
                        continue;
                    }

                    int depthBelowSea = seaLevel - heightMap[x, z];
                    if (depthBelowSea < 0 || depthBelowSea > maxDepth)
                    {
                        continue;
                    }

                    float shelfBlend = 1f - Mathf.Clamp(Mathf.Abs(depthBelowSea) / Mathf.Max(1f, shelfDepth), 0f, 1f);
                    field[x, z] = Mathf.Max(value, value * (0.85f + shelfBlend * 0.15f));
                }
            }
        }

        private void ApplyOutflowChannels(float[,] lakes, int[,] heightMap, float[,] flow, float inflowBlendWeight, int outflowDepth)
        {
            inflowBlendWeight = Mathf.Clamp01(inflowBlendWeight);
            outflowDepth = Mathf.Max(1, outflowDepth);
            if (inflowBlendWeight <= 0f && outflowDepth <= 0)
            {
                return;
            }

            var buffer = (float[,])lakes.Clone();
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float lakeStrength = lakes[x, z];
                    if (lakeStrength <= 0.25f)
                    {
                        continue;
                    }

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    if (downhill == Vector2Int.zero)
                    {
                        continue;
                    }

                    int currentX = x;
                    int currentZ = z;
                    float channelStrength = lakeStrength;

                    for (int step = 0; step < outflowDepth; step++)
                    {
                        currentX = Mathf.Clamp(currentX + downhill.x, 0, chunkSize - 1);
                        currentZ = Mathf.Clamp(currentZ + downhill.y, 0, chunkSize - 1);

                        float flowInfluence = Mathf.Clamp01(flow[currentX, currentZ] * inflowBlendWeight);
                        float blended = Mathf.Max(channelStrength * 0.65f, lakeStrength * 0.35f);
                        buffer[currentX, currentZ] = Mathf.Max(buffer[currentX, currentZ], blended + flowInfluence * 0.5f);

                        if (downhill == Vector2Int.zero)
                        {
                            break;
                        }
                    }
                }
            }

            Array.Copy(buffer, lakes, buffer.Length);
        }

        private float[,] BuildHydrologyMask(int[,] heightMap)
        {
            var hydrology = new float[chunkSize, chunkSize];

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - profile.GlobalWaterLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / Math.Max(1.0, profile.HydrologyWaterTableClampRange), 0.0, 1.0);
                    double shoreBoost = Math.Exp(-distance / Math.Max(0.1, profile.HydrologyShorePush));
                    double slope = ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slope * (profile.HydrologyWaterTableSlopeWeight + profile.HydrologySlopePenalty * 0.1) / 6.0, 0.0, 0.7);
                    double gradientDamp = 1.0 - Math.Clamp(slope * profile.HydrologyGradientWeight / Math.Max(1.0, profile.HydrologyGradientClamp * 8.0f), 0.0, 0.35);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * profile.HydrologyCurvatureWeight * 0.08;
                    double warp = Mathf.PerlinNoise((x + 17) * profile.HydrologyWarpFrequency, (z + 31) * profile.HydrologyWarpFrequency) * profile.HydrologyWarpAmplitude * 0.02;
                    double baseline = Math.Clamp(waterBias * profile.HydrologyWaterTableClampWeight * stability * gradientDamp, 0.0, 1.0);
                    baseline = Math.Clamp(baseline + warp + shoreBoost * 0.05 - curvature, 0.0, 1.2);
                    hydrology[x, z] = Mathf.Clamp01((float)baseline);
                }
            }

            float varianceBlend = Mathf.Clamp01(profile.HydrologyVarianceBlend);
            if (varianceBlend > 0f)
            {
                BlendInterior(hydrology, varianceBlend);
            }

            Smooth2D(hydrology, profile.HydrologySmoothIterations, profile.HydrologySmoothBlend);
            DirectionalSmooth(heightMap, hydrology, profile.HydrologyDirectionalIterations, profile.HydrologyDirectionalBlend);
            ApplyRiparianBuffer(hydrology, profile.RiparianBufferRadius, profile.RiparianSaturationBoost);
            StabilizeEdges(hydrology, profile.HydrologyEdgeBlendRadius, profile.HydrologyEdgeStabilityIterations, profile.HydrologyEdgeStabilityWeight, profile.HydrologyEdgeFluxBlend);
            ApplyEdgeFlowLocks(heightMap, hydrology, profile.HydrologyEdgeBlendRadius, profile.HydrologyEdgeFlowLockWeight, profile.HydrologyEdgeFlowBias, profile.HydrologyEdgeTangentWeight);
            ApplyGradientStability(hydrology, profile.HydrologyGradientStabilityIterations, profile.HydrologyGradientStabilityBlend, profile.HydrologyGradientClamp);
            FillBasins(hydrology, Mathf.Max(0.05f, profile.HydrologyEdgeStabilityWeight * 0.5f), Math.Max(1, profile.HydrologySeamRelaxIterations));
            StitchEdges(hydrology, profile.HydrologySeamRelaxBlend * 0.65f);
            ClampVariance(hydrology, profile.HydrologyVarianceClamp);
            RelaxEdges(hydrology, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(hydrology, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            return hydrology;
        }

        private float[,] BuildFlowMask(int[,] heightMap, float[,] hydrology)
        {
            var flow = new float[chunkSize, chunkSize];
            double persistence = Math.Clamp(profile.HydrologyFlowPersistence, 0.0f, 1.0f);
            double divergenceClamp = Math.Clamp(profile.HydrologyFlowDivergenceClamp, 0.1f, 1.5f);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    double accumulation = 0.0;
                    double current = heightMap[x, z];
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    double gradientMagnitude = Math.Sqrt(downhill.x * downhill.x + downhill.y * downhill.y);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= chunkSize || nz >= chunkSize)
                            {
                                continue;
                            }

                            double neighbor = heightMap[nx, nz];
                            if (neighbor < current)
                            {
                                accumulation += (current - neighbor) * 0.25;
                            }
                        }
                    }

                    double hydrologyBoost = hydrology[x, z] * profile.HydrologyFlowGain;
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * profile.HydrologyCurvatureWeight * 0.1;
                    double continuity = 1.0 + hydrology[x, z] * profile.HydrologyContinuityWeight;
                    double scaled = ((accumulation * (1.0 - persistence)) + hydrologyBoost) * continuity;
                    scaled *= 1.0 - Math.Clamp(curvature, 0.0, 0.6);
                    scaled *= 1.0 - Math.Clamp(gradientMagnitude * profile.HydrologyGradientSlopeWeight * 0.05, 0.0, 0.35);
                    double clampMax = Math.Max(2.5, divergenceClamp * 12.0);
                    flow[x, z] = Mathf.Clamp((float)scaled, 0f, (float)clampMax);
                }
            }

            Smooth2D(flow, profile.HydrologySmoothIterations, profile.HydrologySmoothBlend);
            DirectionalSmooth(heightMap, flow, profile.HydrologyDirectionalIterations, profile.HydrologyDirectionalBlend);
            StabilizeEdges(flow, profile.HydrologyEdgeBlendRadius, profile.HydrologyEdgeStabilityIterations, profile.HydrologyEdgeStabilityWeight, profile.HydrologyEdgeFluxBlend);
            ApplyGradientStability(flow, profile.HydrologyGradientStabilityIterations, profile.HydrologyGradientStabilityBlend, profile.HydrologyGradientClamp);
            ApplyEdgeFlowLocks(heightMap, flow, profile.HydrologyEdgeBlendRadius, profile.HydrologyEdgeFlowLockWeight, profile.HydrologyEdgeFlowBias, profile.HydrologyEdgeTangentWeight);
            FillBasins(flow, Mathf.Max(0.05f, profile.HydrologyEdgeStabilityWeight * 0.35f), Math.Max(1, profile.HydrologySeamRelaxIterations));
            StitchEdges(flow, profile.HydrologySeamRelaxBlend * 0.65f);
            RelaxEdges(flow, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(flow, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            return flow;
        }

        private float[,] BuildErosionRiskMask(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            var risk = new float[chunkSize, chunkSize];
            float surfaceRange = Mathf.Max(1, worldHeight);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 0)
                    {
                        risk[x, z] = 0f;
                        continue;
                    }

                    double slope = ComputeSlope(heightMap, x, z);
                    double slopeNorm = Math.Clamp(slope / 10.0, 0.0, 1.0);
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double flowNorm = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double altitude = Math.Clamp(surface / surfaceRange, 0.0, 1.0);
                    double valley = Math.Clamp((profile.GlobalWaterLevel - surface) / 16.0, 0.0, 1.0);
                    double exposure = Math.Clamp((1.0 - altitude) * 0.65 + valley * 0.45, 0.0, 1.0);
                    double combined = hydro * 0.4 + flowNorm * 0.28 + exposure * 0.2 + slopeNorm * 0.15;

                    risk[x, z] = Mathf.Clamp01((float)combined);
                }
            }

            Smooth2D(risk, profile.HydrologySmoothIterations, profile.HydrologySmoothBlend);
            return risk;
        }

        private void ApplyErosionDamping(float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            float hydroWeight = Mathf.Clamp01(profile.HydrologyEdgeStabilityWeight + profile.RiverBankErosionWeight) * 0.35f;
            float flowWeight = Mathf.Clamp01(profile.RiverBankErosionWeight + profile.LakeRimErosionWeight) * 0.35f;
            if (hydroWeight <= 0f && flowWeight <= 0f)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float risk = Mathf.Clamp01(erosionRisk[x, z]);
                    if (risk <= 0f)
                    {
                        continue;
                    }

                    float interiorHydro = SampleInterior(hydroCopy, x, z);
                    float interiorFlow = SampleInterior(flowCopy, x, z);
                    float damp = Mathf.Clamp(1f - risk * hydroWeight, 0.35f, 1f);
                    float flowDamp = Mathf.Clamp(1f - risk * flowWeight, 0.35f, 1f);
                    float smoothing = Mathf.Clamp(risk * profile.HydrologyVarianceBlend * 0.5f, 0f, 0.45f);

                    float anchoredHydro = hydroCopy[x, z] * damp + interiorHydro * (1f - damp) * 0.5f;
                    anchoredHydro = anchoredHydro * (1f - smoothing) + interiorHydro * smoothing;
                    hydrology[x, z] = Mathf.Clamp01(anchoredHydro);

                    float flowAnchor = flowCopy[x, z] * flowDamp + interiorFlow * (1f - flowDamp) * 0.35f + hydrology[x, z] * 0.15f;
                    float flowClamp = Mathf.Max(profile.HydrologyFlowDivergenceClamp * 12f, flowCopy[x, z] + 2f);
                    flow[x, z] = Mathf.Clamp(flowAnchor, 0f, flowClamp);

                    erosionRisk[x, z] = Mathf.Clamp01(
                        risk * 0.65f +
                        hydrology[x, z] * 0.2f +
                        Mathf.Clamp(flow[x, z] / 6f, 0f, 1f) * 0.15f);
                }
            }
        }

        private void ApplyLakeHydrologySeepage(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] lakeMask, float[,] riverMask)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double seepageWeight = Math.Clamp(worldConfig.Lakes.FlowSeepageWeight, 0.0, 1.0);
            double inflowBlend = Math.Clamp(worldConfig.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(profile.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, profile.HydrologySlopePenalty);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            double continuity = Math.Clamp(profile.HydrologyContinuityWeight, 0.0, 1.0);
            double edgeSeal = Math.Clamp(profile.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double outflowStability = Math.Clamp(profile.LakeOutflowStabilityWeight, 0.0, 1.0);
            double edgeLock = Math.Clamp(profile.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double outflowTaper = Math.Clamp(profile.LakeOutflowTaper, 0.0, 1.0);
            double edgeTangentWeight = Math.Clamp(profile.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double directionalBlend = Math.Clamp(profile.HydrologyDirectionalBlend, 0.0, 1.0);
            double riverContinuityWeight = Math.Clamp(profile.RiverEdgeContinuityWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(profile.HydrologyFlowPersistence, 0.0, 1.0);
            double spillwayDepthBias = Math.Clamp((profile.LakeOutflowCarveDepth + profile.LakeShelfDepth) / 24.0, 0.05, 0.55);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lake = lakeMask[x, z];
                    if (lake <= 0.01f)
                    {
                        continue;
                    }

                    float river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0f;
                    double flowMemory = SampleInterior(flowCopy, x, z) * (0.5 + inflowBlend * 0.35);
                    double hydroBase = hydroCopy[x, z];
                    double hydrologyGradient = Math.Abs(SampleInterior(hydroCopy, x, z) - hydroBase);
                    double flowGradient = Math.Abs(SampleInterior(flowCopy, x, z) - flowCopy[x, z]);
                    double infiltration = lake * (seepageWeight * 0.65 + inflowBlend * 0.35);
                    double slopeGuard = 1.0 - Math.Clamp(ComputeSlope(heightMap, x, z) * slopePenalty / 18.0, 0.0, 0.6);
                    double riverGuard = 1.0 - river * 0.35;
                    double continuityBrake = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * continuity * 0.35, 0.0, 0.35);
                    double edgeSealBlend = 1.0 - Math.Clamp(lake * edgeSeal * 0.25, 0.0, 0.25);
                    double shorelineGuard = 1.0 - Math.Clamp(outflowStability * lake * 0.5, 0.0, 0.4);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    int tangentX = Mathf.Clamp(x - downhill.y, 0, sizeX - 1);
                    int tangentZ = Mathf.Clamp(z + downhill.x, 0, sizeZ - 1);
                    double downHydro = hydroCopy[downX, downZ];
                    double downFlow = flowCopy[downX, downZ];
                    double tangentHydro = hydroCopy[tangentX, tangentZ];
                    double tangentFlow = flowCopy[tangentX, tangentZ];
                    double spillwayPressure =
                        Math.Max(0.0, hydroBase - downHydro) +
                        Math.Max(0.0, flowCopy[x, z] - downFlow) * 0.35;
                    double spillwayBlend = Math.Clamp(
                        lake * outflowTaper * (0.45 + riverContinuityWeight * 0.35) +
                        spillwayPressure * 0.2 +
                        spillwayDepthBias,
                        0.0,
                        1.25);
                    double directionalHydro = downHydro * (0.45 + outflowStability * 0.2) + tangentHydro * edgeTangentWeight * 0.15;
                    double directionalFlow = downFlow * (0.55 + directionalBlend * 0.25) + tangentFlow * edgeTangentWeight * 0.2;
                    double hydroTarget = hydroBase + infiltration * slopeGuard * riverGuard;
                    hydroTarget =
                        hydroTarget * continuityBrake * edgeSealBlend * shorelineGuard * (1.0 - spillwayBlend * 0.35) +
                        directionalHydro * spillwayBlend * 0.35 +
                        flowMemory * inflowBlend * 0.25;
                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.25));

                    double flowTarget = flowCopy[x, z] * (1.0 - lake * 0.25);
                    flowTarget += hydrology[x, z] * (seepageWeight * 0.35 + inflowBlend * 0.2);
                    flowTarget += directionalFlow * spillwayBlend * (0.25 + riverContinuityWeight * 0.35);
                    flowTarget += spillwayPressure * flowPersistence * (0.08 + outflowTaper * 0.1);
                    flowTarget += flowMemory * edgeLock * 0.15;
                    flowTarget *= continuityBrake * shorelineGuard;
                    flowTarget *= 1.0 - Math.Clamp(spillwayDepthBias * lake * 0.18, 0.0, 0.15);
                    flow[x, z] = Mathf.Clamp((float)Math.Clamp(flowTarget + lake * 0.05, 0.0, 1.2));
                }
            }

            StabilizeEdges(hydrology, edgeRadius, 1, Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend), profile.HydrologyEdgeFluxBlend);
            StabilizeEdges(flow, edgeRadius, 1, Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend * 0.85f), profile.HydrologyEdgeFluxBlend);
        }

        private void ApplyAquiferSuppression(float[,] hydrology, float[,] flow, float[,] riverMask, float[,] lakeMask)
        {
            if (riverMask == null && lakeMask == null)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double riverSuppression = Math.Clamp(worldConfig.Caves.RiverSuppressionWeight, 0.0, 1.0);
            double moistureRetention = Math.Clamp(worldConfig.Caves.MoistureRetentionWeight, 0.0, 1.0);
            double flowMemoryWeight = Math.Clamp(worldConfig.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double edgeLock = Math.Clamp(worldConfig.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double seepageWeight = Math.Clamp(worldConfig.Lakes.FlowSeepageWeight, 0.0, 1.0);
            double outflowStability = Math.Clamp(worldConfig.Lakes.OutflowStabilityWeight, 0.0, 1.0);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, profile.HydrologyVarianceClamp);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? Mathf.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = river * riverSuppression + lake * (seepageWeight * 0.5 + outflowStability * 0.35);
                    if (wetness <= 0.01)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double flowMemory = SampleInterior(flowCopy, x, z) * flowMemoryWeight;
                    double damp = 1.0 - Math.Clamp(wetness * (moistureRetention * 0.5 + edgeLock * 0.35), 0.0, 0.85);
                    double sealedHydro = hydro * damp + wetness * moistureRetention * 0.25;
                    double sealedFlow = flowValue * (1.0 - wetness * 0.45) + flowMemory * (wetness * 0.35);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(sealedHydro, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp((float)Math.Clamp(sealedFlow, 0.0, 1.25));
                }
            }

            StabilizeEdges(hydrology, edgeRadius, 1, Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend * 0.85f), profile.HydrologyEdgeFluxBlend);
            StabilizeEdges(flow, edgeRadius, 1, Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend * 0.65f), profile.HydrologyEdgeFluxBlend);
        }

        private void ApplyFloodplainSlackwaterRetention(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            double seamBlend = Math.Clamp(worldConfig.Water.HydrologySeamRelaxBlend, 0.0, 1.0);
            double continuity = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double persistence = Math.Clamp(worldConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double catchmentWeight = Math.Clamp(worldConfig.Water.HydrologyCatchmentWeight, 0.0, 1.0);
            double pressureBlend = Math.Clamp(worldConfig.Water.HydrologyPressureBlend, 0.0, 1.0);
            double gradientWeight = Math.Clamp(worldConfig.Water.HydrologyGradientWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double slope = ComputeSlope(heightMap, x, z);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    double slackwater = Math.Clamp(
                        (1.0 - slope / 10.0) * 0.45 +
                        (1.0 - relief / 18.0) * 0.2 +
                        seamHydro * 0.2 +
                        seamFlow * 0.08 +
                        edgeFalloff * 0.07,
                        0.0,
                        1.0);

                    double seepRetention = slackwater * (0.35 + continuity * 0.25 + catchmentWeight * 0.2);
                    seepRetention *= 1.0 - Math.Clamp(erosion * 0.3, 0.0, 0.3);

                    double pressure = (seamHydro + seamFlow * 0.5) * pressureBlend;
                    double pressureGuard = 1.0 - Math.Clamp(Math.Abs(pressure - hydro) * gradientWeight * 0.35, 0.0, 0.4);
                    double hydroTarget = hydro * (1.0 - seamBlend * 0.5) + (seamHydro + seepRetention + pressure * 0.2) * seamBlend * 0.5;
                    hydroTarget *= pressureGuard;
                    hydroTarget = Math.Clamp(hydroTarget, 0.0, 1.0 + varianceClamp * 0.35);

                    double flowTarget = flowValue * (1.0 - seamBlend * 0.45) + (seamFlow * (0.35 + persistence * 0.25) + seepRetention * 0.45) * seamBlend * 0.55;
                    flowTarget *= 1.0 - Math.Clamp(slackwater * 0.2, 0.0, 0.2);
                    flowTarget = Math.Clamp(flowTarget, 0.0, Math.Max(1.3, flowValue + seepRetention * 0.5));

                    hydrology[x, z] = Mathf.Clamp01((float)hydroTarget);
                    flow[x, z] = Mathf.Clamp((float)flowTarget, 0f, 1.35f);
                }
            }

            StabilizeEdges(hydrology, edgeRadius, 1, Mathf.Max(0.05f, (float)(seamBlend * 0.65)), (float)varianceClamp);
            StabilizeEdges(flow, edgeRadius, 1, Mathf.Max(0.05f, (float)(seamBlend * 0.5)), (float)(varianceClamp * 1.3));
        }

        private void ApplyHydrologyMomentum(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            double momentumWeight = Math.Clamp(worldConfig.Water.HydrologyFlowGain, 0.0, 1.0);
            double persistence = Math.Clamp(worldConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double erosionBrake = Math.Clamp(worldConfig.Water.RiverReliefPenaltyWeight, 0.0, 1.0);
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int dx = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int dz = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);

                    double baseHydro = hydroCopy[x, z];
                    double baseFlow = flowCopy[x, z];
                    double downhillHydro = hydroCopy[dx, dz];
                    double downhillFlow = flowCopy[dx, dz];
                    double pressure = baseHydro + baseFlow * 0.25;
                    double downhillPressure = downhillHydro + downhillFlow * 0.25;
                    double gradient = Math.Abs(downhillPressure - pressure);
                    double divergence = Math.Min(1.0, gradient / divergenceClamp);
                    double erosion = erosionRisk[x, z] * erosionBrake;
                    double momentum = (downhillPressure - pressure) * momentumWeight;
                    double blendedHydro = baseHydro * (1.0 - momentumWeight) + downhillHydro * momentumWeight + momentum * 0.25;
                    blendedHydro = blendedHydro * (1.0 - erosion * 0.25) + baseHydro * erosion * 0.25;
                    double blendedFlow = baseFlow * (1.0 - persistence) + (downhillFlow + momentum) * persistence;
                    blendedFlow *= 1.0 - divergence * 0.35;
                    blendedFlow = Math.Clamp(blendedFlow, 0.0, Math.Max(1.35, baseFlow + Math.Abs(momentum) * 0.5));

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(blendedHydro, 0.0, 1.25));
                    flow[x, z] = Mathf.Clamp((float)blendedFlow, 0f, 1.35f);
                }
            }
        }

        private void ApplyWatershedRetentionField(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            double persistence = Math.Clamp(worldConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double flowMemoryWeight = Math.Clamp(worldConfig.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double continuityWeight = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double edgeBlend = Math.Clamp(worldConfig.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            double flowClamp = Math.Max(0.5, worldConfig.Water.HydrologyFlowDivergenceClamp * 14.0);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);

                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double downhillHydro = hydroCopy[downX, downZ];
                    double downhillFlow = flowCopy[downX, downZ];
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double basinBias = Math.Clamp(1.0 - relief / 14.0, 0.0, 1.0);
                    double seamBias = (seamHydro + seamFlow + downhillHydro + downhillFlow) * 0.25;
                    double gradient = Math.Abs(seamFlow - flowValue) + Math.Abs(seamHydro - hydro) * 0.5;
                    double divergenceBrake = 1.0 - Math.Clamp(gradient * 0.35, 0.0, 0.45);
                    double slopeBrake = 1.0 - Math.Clamp(slope * worldConfig.Water.HydrologySlopePenalty / 96.0, 0.0, 0.55);
                    double erosionBrake = 1.0 - erosion * 0.25;
                    double retention = Math.Clamp(
                        basinBias * (0.25 + continuityWeight * 0.35) +
                        seamBias * (0.15 + flowMemoryWeight * 0.35),
                        0.0,
                        1.25);

                    double hydroTarget = hydro * (1.0 - continuityWeight * 0.35) + retention * divergenceBrake * erosionBrake;
                    hydroTarget += downhillHydro * persistence * 0.12;

                    double flowTarget = flowValue * (1.0 - persistence * 0.3) +
                        (seamFlow * 0.22 + downhillFlow * 0.28 + retention * 0.35) * persistence;
                    flowTarget *= slopeBrake * erosionBrake;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 0.9));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, Math.Max(flowClamp, flowValue + retention * 0.45)));
                }
            }

            NormalizeEdgeBands(hydrology, edgeRadius, edgeBlend * 0.75f, (float)varianceClamp);
            NormalizeEdgeBands(flow, edgeRadius, (float)Math.Max(0.05, edgeBlend * 0.6), (float)(varianceClamp * 1.3));
        }

        private void ApplySubterraneanHydrologyShield(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            double sealStrength = Math.Clamp(worldConfig.Caves.EdgeSealStrength, 0.0, 1.0);
            double moistureRetention = Math.Clamp(worldConfig.Caves.MoistureRetentionWeight, 0.0, 1.0);
            double flowMemory = Math.Clamp(worldConfig.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double slopePenalty = Math.Max(0.001, worldConfig.Water.HydrologySlopePenalty);
            double entranceDampening = Math.Clamp(worldConfig.Caves.CaveEntranceFlowDampening, 0.0, 1.0);
            double ceilingMoistureClamp = Math.Clamp(worldConfig.Caves.CeilingMoistureClamp, 0.0, 1.0);
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double slope = ComputeSlope(heightMap, x, z);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z));
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double seal = Math.Clamp(sealStrength * (0.25 + slope / (slopePenalty * 8.0) + curvature * 0.12), 0.0, 0.65);
                    double retention = 1.0 - Math.Clamp(erosion * moistureRetention * 0.5, 0.0, 0.55);
                    double entranceGuard = 1.0 - Math.Clamp(flowCopy[x, z] * entranceDampening * 0.25, 0.0, 0.35);
                    double aquiferGuard = 1.0 - Math.Clamp(hydroCopy[x, z] * ceilingMoistureClamp * 0.2, 0.0, 0.25);
                    double hydroTarget = hydroCopy[x, z] * (1.0 - seal) + flowCopy[x, z] * flowMemory * 0.25;
                    hydroTarget = Math.Clamp(hydroTarget * retention * aquiferGuard + hydroCopy[x, z] * (1.0 - aquiferGuard) * 0.15, 0.0, 1.3);

                    double flowTarget = flowCopy[x, z] * (1.0 - seal * 0.35) + hydroCopy[x, z] * 0.15;
                    flowTarget *= (1.0 - erosion * 0.25) * entranceGuard;
                    flowTarget = Math.Clamp(flowTarget, 0.0, 1.1);

                    hydrology[x, z] = Mathf.Clamp01((float)hydroTarget);
                    flow[x, z] = Mathf.Clamp01((float)flowTarget);
                }
            }
        }

        private void ApplyRiparianFlowBridge(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            double continuity = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double flowLock = Math.Clamp(worldConfig.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double flowBias = Math.Clamp(worldConfig.Water.HydrologyEdgeFlowBias, 0.0, 1.0);
            double tangentWeight = Math.Clamp(worldConfig.Water.HydrologyEdgeTangentWeight, 0.0, 1.5);
            double directionalBlend = Math.Clamp(worldConfig.Water.HydrologyDirectionalBlend, 0.0, 1.0);
            double edgeBlend = Math.Clamp(worldConfig.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            double erosionBrake = Math.Clamp(worldConfig.Water.RiverReliefPenaltyWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    double downhillHydro = hydroCopy[downX, downZ];
                    double downhillFlow = flowCopy[downX, downZ];
                    double gradient = ComputeSlope(heightMap, x, z);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f) * erosionBrake;
                    double corridorHydro = (hydro + seamHydro + downhillHydro) / 3.0;
                    double corridorFlow = (flowValue + seamFlow + downhillFlow) / 3.0;
                    double tangent = (Math.Abs(downhill.x) + Math.Abs(downhill.y)) * 0.5;
                    double tangentAssist = 1.0 + tangent * tangentWeight * 0.1;
                    double edgeFalloff = 1.0 - Math.Clamp(Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z)) / (double)(edgeRadius + 1), 0.0, 1.0);
                    double bridge = Math.Clamp(continuity * 0.35 + flowLock * 0.25 + edgeBlend * edgeFalloff * 0.35, 0.08, 0.85);
                    double erosionDamp = 1.0 - erosion * 0.35;
                    double gradientBrake = 1.0 - Math.Clamp(gradient * worldConfig.Water.HydrologyGradientWeight * 0.05, 0.0, 0.35);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(
                        hydro * (1.0 - bridge) + corridorHydro * bridge * tangentAssist * gradientBrake,
                        0.0,
                        varianceClamp + 1.0));

                    double directional = 1.0 + tangent * directionalBlend * 0.25;
                    double edgeBias = 1.0 + edgeFalloff * flowBias * 0.25;
                    double flowTarget = flowValue * (1.0 - bridge) + corridorFlow * bridge * directional * edgeBias;
                    flowTarget = flowTarget * erosionDamp + flowValue * (1.0 - erosionDamp);
                    flow[x, z] = Mathf.Clamp((float)Math.Clamp(
                        flowTarget,
                        0.0,
                        Math.Max(1.5, flowValue + corridorFlow * 0.5)), 0f, 1.35f);
                }
            }

            NormalizeEdges(hydrology, edgeRadius, edgeBlend * 0.75f, (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Math.Max(0.05f, (float)(edgeBlend * 0.55f)), (float)(varianceClamp * 1.35));
        }

        private void ApplyKarstWetlandCoupling(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            double continuityWeight = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double catchmentWeight = Math.Clamp(worldConfig.Water.HydrologyCatchmentWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double moistureRetention = Math.Clamp(worldConfig.Caves.MoistureRetentionWeight, 0.0, 1.0);
            double aquiferBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            double riparianGuard = Math.Clamp(worldConfig.Caves.RiparianCaveGuardWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowNode = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double erosion = erosionCopy[x, z];
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    double karstWetness = Math.Clamp(
                        hydro * 0.35 + seamHydro * 0.25 + flowNode * 0.2 + seamFlow * 0.2,
                        0.0,
                        1.25);
                    double basinAnchor = 1.0 - Math.Clamp(slope / 12.0 + relief / 28.0, 0.0, 1.0);
                    double wetlandCoupling = karstWetness * (0.3 + catchmentWeight * 0.25 + moistureRetention * 0.2);
                    wetlandCoupling *= basinAnchor * (0.75 + edgeBand * 0.25);
                    wetlandCoupling *= 1.0 - Math.Clamp(divergence * 0.4 + erosion * 0.25, 0.0, 0.75);

                    double hydroTarget = hydro + wetlandCoupling * (0.22 + continuityWeight * 0.18 + aquiferBarrier * 0.12);
                    double flowTarget = flowNode * (1.0 - divergence * 0.2) + wetlandCoupling * (0.28 + continuityWeight * 0.24);
                    double erosionTarget = erosion * (1.0 - wetlandCoupling * 0.12) + wetlandCoupling * (0.14 + riparianGuard * 0.1);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.72)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.62)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.33)));
        }

        private void ApplyDeltaWaterTableCoupling(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            int mouthRadius = Math.Max(2, worldConfig.Water.RiverMouthSmoothRadius);
            double continuityWeight = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double waterTableClampWeight = Math.Clamp(worldConfig.Water.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double waterTableClampRange = Math.Max(1.0, worldConfig.Water.HydrologyWaterTableClampRange);
            double outflowStability = Math.Clamp(worldConfig.Lakes.OutflowStabilityWeight, 0.0, 1.0);
            double spillwayWeight = Math.Clamp(worldConfig.Lakes.SpillwayContinuityWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowNode = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double slope = ComputeSlope(heightMap, x, z);
                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.6),
                        0.0,
                        1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double waterTableDistance = seaLevel - heightMap[x, z];
                    double tableBand = 1.0 - Math.Clamp(Math.Abs(waterTableDistance) / waterTableClampRange, 0.0, 1.0);
                    double deltaCoupling = Math.Clamp(
                        hydro * 0.3 + seamHydro * 0.24 + flowNode * 0.2 + seamFlow * 0.16 + tableBand * 0.1,
                        0.0,
                        1.2);
                    double stabilizer = 1.0 - Math.Clamp(
                        divergence * 0.4 +
                        Math.Abs(hydro - seamHydro) * 0.24 +
                        slope * worldConfig.Water.HydrologySlopePenalty * 0.01,
                        0.0,
                        0.78);
                    double couplingWeight = (0.25 + continuityWeight * 0.25 + outflowStability * 0.2 + spillwayWeight * 0.15 + waterTableClampWeight * 0.15);
                    double coupling = deltaCoupling * couplingWeight * (0.45 + seaBand * 0.35 + edgeBand * 0.2) * stabilizer;
                    double erosion = erosionCopy[x, z];

                    double hydroTarget = hydro + coupling * (0.22 + continuityWeight * 0.18);
                    double flowTarget = flowNode * (1.0 - divergence * 0.2) + coupling * (0.28 + continuityWeight * 0.24);
                    double erosionTarget = erosion * (1.0 - coupling * 0.12) + coupling * (0.1 + spillwayWeight * 0.1);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.70)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.60)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.32)));
        }

        private void ApplyLagoonKarstCoupling(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 2);
            int mouthRadius = Math.Max(2, worldConfig.Water.RiverMouthSmoothRadius);
            double continuityWeight = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(worldConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double spillwayWeight = Math.Clamp(worldConfig.Lakes.SpillwayContinuityWeight, 0.0, 1.0);
            double caveBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            double caveEntrance = Math.Clamp(worldConfig.Caves.CaveEntranceFlowDampening, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowNode = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double erosion = erosionCopy[x, z];
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double seaBand = 1.0 - Math.Clamp(
                        Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, mouthRadius * 2.8),
                        0.0,
                        1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double karstBand = 1.0 - Math.Clamp(slope / 12.0 + relief / 26.0, 0.0, 1.0);
                    double lagoonMemory = Math.Clamp(
                        hydro * 0.26 + seamHydro * 0.22 + flowNode * 0.2 + seamFlow * 0.16 + seaBand * 0.1 + karstBand * 0.06,
                        0.0,
                        1.2);
                    double stabilizer = 1.0 - Math.Clamp(
                        divergence * 0.38 +
                        Math.Abs(hydro - seamHydro) * 0.22 +
                        erosion * 0.22 +
                        slope * worldConfig.Water.HydrologySlopePenalty * 0.01,
                        0.0,
                        0.78);
                    double couplingWeight = 0.24 + continuityWeight * 0.22 + flowPersistence * 0.18 + spillwayWeight * 0.2 + caveBarrier * 0.16;
                    double coupling = lagoonMemory * couplingWeight * (0.5 + seaBand * 0.32 + edgeBand * 0.18 + karstBand * 0.12) * stabilizer;

                    double hydroTarget = hydro + coupling * (0.2 + caveBarrier * 0.2);
                    double flowTarget = flowNode * (1.0 - divergence * 0.18) + coupling * (0.26 + continuityWeight * 0.2 + caveEntrance * 0.12);
                    double erosionTarget = erosion * (1.0 - coupling * 0.1) + coupling * (0.1 + spillwayWeight * 0.12);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.68)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.58)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.30)));
        }

        private void ApplyFloodplainLeakageStability(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            double continuityWeight = Math.Clamp(worldConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double catchmentWeight = Math.Clamp(worldConfig.Water.HydrologyCatchmentWeight, 0.0, 1.0);
            double lakeInflowWeight = Math.Clamp(worldConfig.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double flowMemoryWeight = Math.Clamp(worldConfig.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double spillwayWeight = Math.Clamp(worldConfig.Lakes.SpillwayContinuityWeight, 0.0, 1.0);
            double caveBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            double riparianGuard = Math.Clamp(worldConfig.Caves.RiparianCaveGuardWeight, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, worldConfig.Water.HydrologySlopePenalty);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            double waterTableRange = Math.Max(1.0, worldConfig.Water.HydrologyWaterTableClampRange);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowNode = flowCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double erosion = erosionCopy[x, z];
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double waterTableBand = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / waterTableRange, 0.0, 1.0);
                    double floodplainBand = 1.0 - Math.Clamp(slope / 10.5 + relief / 28.0, 0.0, 1.0);
                    double seamDelta = Math.Abs(hydro - seamHydro) + Math.Abs(flowNode - seamFlow);
                    double leakRisk = Math.Clamp(
                        seamDelta * 0.3 +
                        slope * slopePenalty * 0.01 +
                        erosion * 0.25,
                        0.0,
                        1.0);

                    double recharge = floodplainBand * (0.22 + continuityWeight * 0.2 + catchmentWeight * 0.2 + lakeInflowWeight * 0.18 + waterTableBand * 0.2);
                    recharge *= 1.0 - Math.Clamp(erosion * 0.35, 0.0, 0.35);

                    double barrier = caveBarrier * 0.45 + riparianGuard * 0.35 + spillwayWeight * 0.2;
                    double stableRecharge = recharge * (0.72 + barrier * 0.28) * (1.0 - leakRisk * 0.45) * (0.82 + edgeBand * 0.18);

                    double hydroTarget =
                        hydro * (1.0 - barrier * 0.16) +
                        seamHydro * (0.16 + continuityWeight * 0.12) +
                        stableRecharge * (0.20 + catchmentWeight * 0.18) +
                        seamFlow * 0.06;

                    double flowTarget =
                        flowNode * (1.0 - barrier * 0.14) +
                        seamFlow * (0.18 + flowMemoryWeight * 0.2) +
                        stableRecharge * (0.18 + lakeInflowWeight * 0.16);

                    double erosionTarget =
                        erosion * (1.0 - stableRecharge * 0.1) +
                        leakRisk * (0.05 + riparianGuard * 0.05);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.73)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.61)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.34)));
        }

        private void ApplyHydrologySinkStabilityField(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            double sinkRepairWeight = Math.Clamp(
                worldConfig.Water.HydrologyContinuityWeight * 0.26 +
                worldConfig.Water.HydrologyThalwegStabilityWeight * 0.16 +
                worldConfig.Water.RiverConfluenceBoost * 0.14 +
                worldConfig.Water.LakeInflowBlendWeight * 0.18 +
                worldConfig.Lakes.SpillRetentionWeight * 0.14 +
                worldConfig.Caves.AquiferBarrierWeight * 0.12,
                0.0,
                1.35);
            double sinkDepthThreshold = Math.Max(0.5, 1.0 + worldConfig.Lakes.ShelfDepth * 0.18 - worldConfig.Lakes.TerraceBiasWeight * 0.12);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);

            if (sinkRepairWeight <= 0.01)
            {
                return;
            }

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 1; x < sizeX - 1; x++)
            {
                for (int z = 1; z < sizeZ - 1; z++)
                {
                    double neighbourHeight =
                        heightMap[x - 1, z] +
                        heightMap[x + 1, z] +
                        heightMap[x, z - 1] +
                        heightMap[x, z + 1] +
                        heightMap[x - 1, z - 1] +
                        heightMap[x + 1, z - 1] +
                        heightMap[x - 1, z + 1] +
                        heightMap[x + 1, z + 1];
                    double sinkDepth = neighbourHeight / 8.0 - heightMap[x, z];
                    if (sinkDepth <= sinkDepthThreshold)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowValue = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = erosionCopy[x, z];
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double depressionFactor = Math.Clamp(
                        (sinkDepth - sinkDepthThreshold) / Math.Max(1.0, sinkDepthThreshold + worldConfig.Lakes.MaxDepth * 0.35),
                        0.0,
                        1.0);

                    double leakBrake = 1.0 - Math.Clamp(erosion * 0.35 + slope * 0.025 + relief / 64.0, 0.0, 0.8);
                    if (leakBrake <= 0.0)
                    {
                        continue;
                    }

                    double infiltration = Math.Clamp(
                        (seamHydro * 0.45 + seamFlow * 0.35 + hydro * 0.2) * (0.45 + sinkRepairWeight * 0.35),
                        0.0,
                        1.35);
                    double repairBlend = sinkRepairWeight * depressionFactor * leakBrake;

                    double hydroTarget =
                        hydro * (1.0 - repairBlend * 0.28) +
                        (seamHydro * 0.35 + infiltration * 0.4) * repairBlend;

                    double flowTarget =
                        flowValue * (1.0 - repairBlend * 0.24) +
                        (seamFlow * 0.4 + infiltration * 0.3 + seamHydro * 0.2) * repairBlend;

                    double erosionTarget =
                        erosion * (1.0 - depressionFactor * 0.1) +
                        (1.0 - leakBrake) * 0.08 * depressionFactor;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.68)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.57)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.28)));
        }

        private void ApplyKarstConfluenceRetentionField(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            double convergenceWeight = Math.Clamp(
                worldConfig.Water.RiverConfluenceBoost * 0.35 +
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.35 +
                worldConfig.Lakes.SpillRetentionWeight * 0.30,
                0.0,
                1.25);
            if (convergenceWeight <= 0.01)
            {
                return;
            }

            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = erosionCopy[x, z];
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, edgeRadius));
                    double curvature = SampleCurvature(heightMap, x, z);
                    double flowDivergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double sinkPotential = Math.Clamp(
                        Math.Max(0.0, -curvature) * 0.28 +
                        Math.Max(0.0, seaLevel - heightMap[x, z]) / Math.Max(8.0, seaLevel * 0.5) * 0.32 +
                        (1.0 - Math.Clamp(slope / 11.0, 0.0, 1.0)) * 0.40,
                        0.0,
                        1.15);
                    if (sinkPotential <= 0.03)
                    {
                        continue;
                    }

                    double confluence = Math.Max(0.0, seamFlow - flowNode) + Math.Max(0.0, seamHydro - hydro) * 0.5;
                    double retention = sinkPotential * (0.16 + convergenceWeight * 0.3) * (1.0 + confluence * 0.22);
                    retention *= 1.0 - Math.Clamp(flowDivergence * 0.4 + erosion * 0.35 + relief / 42.0, 0.0, 0.8);

                    double hydroTarget = hydro * (1.0 - retention * 0.16) + (seamHydro + retention * 0.5) * retention * 0.16;
                    double flowTarget = flowNode * (1.0 - retention * 0.15) + (seamFlow + retention * 0.35) * retention * 0.15;
                    double erosionTarget = erosion * (1.0 - retention * 0.08) + flowDivergence * 0.03 * (1.0 - sinkPotential * 0.35);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.66)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.56)), (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.26)));
        }

        private void ApplyKarstSpringFloodplainCouplingField(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, worldConfig.Water.HydrologyEdgeBlendRadius);
            double couplingWeight = Math.Clamp(
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.34 +
                worldConfig.Water.RiverEdgeContinuityWeight * 0.33 +
                worldConfig.Lakes.SpillwayContinuityWeight * 0.33,
                0.0,
                1.25);
            if (couplingWeight <= 0.01)
            {
                return;
            }

            double divergenceClamp = Math.Max(0.05, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double slopePenalty = Math.Max(0.0, worldConfig.Water.HydrologySlopePenalty);
            double seaBand = Math.Max(3.0, worldConfig.Lakes.MaxDepth * 1.5);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = Mathf.Clamp01(erosionCopy[x, z]);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double seaDistance = Math.Abs(heightMap[x, z] - seaLevel);
                    double floodplainBand = 1.0 - Math.Clamp(seaDistance / seaBand, 0.0, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double confluence = Math.Clamp((hydro + seamHydro + flowNode * 0.2 + seamFlow * 0.2) * 0.5, 0.0, 1.3);
                    double springPotential = confluence * (0.35 + floodplainBand * 0.45 + edgeBand * 0.20);
                    springPotential *= 1.0 - Math.Clamp(divergence * 0.35 + slope * slopePenalty * 0.01 + relief / 128.0, 0.0, 0.8);
                    if (springPotential <= 0.03)
                    {
                        continue;
                    }

                    double ventilation = Math.Clamp(
                        (1.0 - hydro) * (1.0 - Math.Clamp(flowNode / 6.0, 0.0, 1.0)) * worldConfig.Caves.CaveVentilationBias,
                        0.0,
                        1.0);
                    double coupling = springPotential * couplingWeight;

                    double hydroTarget = hydro + coupling * 0.16 - ventilation * 0.05;
                    double flowTarget =
                        flowNode * (1.0 - coupling * 0.08) +
                        seamFlow * coupling * 0.08 +
                        springPotential * 0.03;
                    double erosionTarget =
                        erosion * (1.0 - coupling * 0.07) +
                        Math.Clamp(flowTarget / 6.0, 0.0, 1.0) * 0.03 -
                        floodplainBand * coupling * 0.02;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.25));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(
                hydrology,
                edgeRadius,
                Mathf.Clamp01((float)worldConfig.Water.HydrologyEdgeNormalizationBlend),
                (float)varianceClamp);
            NormalizeEdges(
                flow,
                edgeRadius,
                Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.72)),
                (float)(varianceClamp * 1.25));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.22)));
        }

        private void ApplySeasonalRunoffCouplingField(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            Vector2Int chunkPos)
        {
            double couplingWeight = Math.Clamp(
                profile.HydrologyFlowPersistence * 0.38 +
                profile.RiverConfluenceBoost * 0.34 +
                profile.LakeSpillRetentionWeight * 0.28,
                0.0,
                1.0);
            if (couplingWeight <= 0.01)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Mathf.Max(2, profile.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.05, profile.HydrologyFlowDivergenceClamp);
            double slopePenalty = Math.Max(0.0, profile.HydrologySlopePenalty);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = Mathf.Clamp01(erosionCopy[x, z]);
                    double slope = ComputeSlope(heightMap, x, z);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    int seasonalSeed = ComputeSeasonalSeed(chunkPos.x, chunkPos.y, x, z);
                    float worldX = chunkPos.x * chunkSize + x;
                    float worldZ = chunkPos.y * chunkSize + z;
                    double seasonalNoise = Mathf.PerlinNoise(
                        worldX * 0.017f + 29f + (seasonalSeed & 0xFF) * 0.003f,
                        worldZ * 0.017f - 13f + ((seasonalSeed >> 8) & 0xFF) * 0.003f);

                    double seasonalRunoff = Math.Clamp(
                        (hydro + seamHydro + flowNode * 0.7 + seamFlow * 0.7) * 0.28 +
                        seasonalNoise * 0.32 +
                        edgeBand * 0.16,
                        0.0,
                        1.35);
                    if (seasonalRunoff <= 0.22)
                    {
                        continue;
                    }

                    double runoffClamp = 1.0 - Math.Clamp(divergence * 0.4 + slope * slopePenalty * 0.01, 0.0, 0.72);
                    double coupling = seasonalRunoff * couplingWeight * runoffClamp * (0.12 + edgeBand * 0.08);
                    double hydroTarget = hydro * (1.0 - coupling * 0.25) + (hydro + seamHydro) * 0.5 * coupling * 0.25;
                    double flowTarget = flowNode * (1.0 - coupling * 0.18) + (seamFlow + seasonalRunoff * 0.22) * coupling * 0.18;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.2));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.2));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(
                        erosion * (1.0 - coupling * 0.1) +
                        Mathf.Clamp01(flow[x, z] / 6f) * 0.04 +
                        edgeBand * 0.02,
                        0.0,
                        1.0));
                }
            }
        }

        private void ApplyIsolatedBasinSpillwayBalancing(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Mathf.Max(2, profile.HydrologyEdgeBlendRadius + 1);
            double inflowBlend = Math.Clamp(profile.LakeInflowBlendWeight, 0.0, 1.0);
            double spillRetention = Math.Clamp(profile.LakeSpillRetentionWeight, 0.0, 1.0);
            double outflowStability = Math.Clamp(profile.LakeOutflowStabilityWeight, 0.0, 1.0);
            double confluenceBoost = Math.Clamp(profile.RiverConfluenceBoost, 0.0, 2.0);
            double slopePenalty = Math.Max(0.0, profile.HydrologySlopePenalty);
            double divergenceClamp = Math.Max(0.05, profile.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, profile.HydrologyVarianceClamp);
            double waterTableRange = Math.Max(4.0, profile.HydrologyWaterTableClampRange);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = Mathf.Clamp01(erosionCopy[x, z]);
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBand = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);

                    double basinPressure = Math.Clamp(
                        hydro * 0.52 + seamHydro * 0.22 + relief / Math.Max(8.0, waterTableRange * 0.85),
                        0.0,
                        1.45);
                    double drainageDeficit = Math.Clamp(1.0 - Math.Min(1.0, flowNode + seamFlow), 0.0, 1.0);
                    double slopeBrake = 1.0 - Math.Clamp(slope * slopePenalty / 24.0, 0.0, 0.82);
                    double isolation = basinPressure * drainageDeficit * slopeBrake * (0.72 + edgeBand * 0.28);

                    if (isolation <= 0.20)
                    {
                        continue;
                    }

                    double spillwayAssist = isolation *
                        (0.16 + spillRetention * 0.2 + outflowStability * 0.2 + confluenceBoost * 0.08) *
                        (1.0 - Math.Clamp(divergence * 0.35 + erosion * 0.25, 0.0, 0.85));
                    double hydroTarget = hydro + spillwayAssist * (0.07 + inflowBlend * 0.08) - flowNode * spillwayAssist * 0.05;
                    double flowTarget = flowNode * (1.0 - spillwayAssist * 0.1) +
                                        (seamFlow + isolation * 0.3) * spillwayAssist * (0.24 + outflowStability * 0.12);
                    double erosionTarget = erosion * (1.0 - spillwayAssist * 0.06) + spillwayAssist * (0.02 + confluenceBoost * 0.01);

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.3));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(
                hydrology,
                edgeRadius,
                Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.68)),
                (float)varianceClamp);
            NormalizeEdges(
                flow,
                edgeRadius,
                Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.6)),
                (float)(varianceClamp * 1.3));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(profile.HydrologySmoothBlend * 0.3)));
        }

        private static int ComputeSeasonalSeed(int chunkX, int chunkZ, int localX, int localZ)
        {
            unchecked
            {
                int hash = 0x2D2816FE;
                hash = (hash * 397) ^ chunkX;
                hash = (hash * 397) ^ chunkZ;
                hash = (hash * 397) ^ localX;
                hash = (hash * 397) ^ localZ;
                return hash;
            }
        }

        private void ApplyRiverLakeHydrologyFeedback(int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask, float[,] lakeMask, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            double edgeLock = Math.Clamp(worldConfig.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double tangentWeight = Math.Clamp(worldConfig.Water.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double anisotropy = Math.Clamp(worldConfig.Water.RiverAnisotropyWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(worldConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double gradientPenalty = Math.Clamp(worldConfig.Water.RiverGradientPenalty, 0.0, 1.5);
            double reliefPenalty = Math.Clamp(worldConfig.Water.RiverReliefPenaltyWeight, 0.0, 1.0);
            double confluenceBoost = Math.Clamp(worldConfig.Water.RiverConfluenceBoost, 0.0, 2.0);
            double lakeInflowBlend = Math.Clamp(worldConfig.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double spillRetention = Math.Clamp(worldConfig.Lakes.SpillRetentionWeight, 0.0, 1.0);
            double caveBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            double riparianGuard = Math.Clamp(worldConfig.Caves.RiparianCaveGuardWeight, 0.0, 1.0);
            double flowShadow = Math.Clamp(worldConfig.Water.HydrologyFlowShadowWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? Mathf.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = river * 0.65 + lake * 0.55;
                    if (wetness < 0.01 && erosionRisk[x, z] < 0.01f)
                    {
                        continue;
                    }

                    double nearbyRiver = riverMask != null ? SampleInterior(riverMask, x, z) : 0.0;
                    double nearbyLake = lakeMask != null ? SampleInterior(lakeMask, x, z) : 0.0;
                    double confluence = Math.Clamp(
                        Math.Max(0.0, nearbyRiver + nearbyLake - Math.Abs(nearbyRiver - nearbyLake) * 0.5),
                        0.0,
                        1.2);
                    double floodplainCoupling = Math.Clamp(
                        wetness * (0.62 + lakeInflowBlend * 0.18 + confluenceBoost * 0.12) +
                        confluence * (0.14 + spillRetention * 0.1),
                        0.0,
                        1.65);
                    double caveShield = Math.Clamp(caveBarrier * 0.42 + riparianGuard * 0.28 + spillRetention * 0.2, 0.0, 1.25);
                    double slope = ComputeSlope(heightMap, x, z);
                    double slopeGuard = 1.0 - Math.Clamp(slope * gradientPenalty / 64.0, 0.0, 0.55);
                    double erosionGuard = 1.0 - Math.Clamp(erosionRisk[x, z] * reliefPenalty, 0.0, 0.45);
                    double baseHydro = hydroCopy[x, z];
                    double baseFlow = flowCopy[x, z];
                    double lockedHydro = baseHydro * (1.0 - edgeLock) + wetness * edgeLock;
                    double tangentialBoost = (river + lake) * tangentWeight * 0.25;
                    double flowTarget = baseFlow * (1.0 - wetness * 0.35) + wetness * (flowPersistence * 0.35 + anisotropy * 0.25 + tangentialBoost);
                    double hydroTarget = lockedHydro * slopeGuard * erosionGuard + flowTarget * 0.1;
                    flowTarget = flowTarget * (1.0 - caveShield * 0.08) +
                                 floodplainCoupling * (0.10 + flowShadow * 0.08 + confluenceBoost * 0.05);
                    hydroTarget = hydroTarget +
                                  floodplainCoupling * (0.08 + lakeInflowBlend * 0.08 + spillRetention * 0.05) -
                                  erosionRisk[x, z] * caveShield * 0.06;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.35));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.2));
                }
            }
        }

        private void ApplyRiparianCaveBuffer(float[,] erosionRisk, float[,] hydrology, float[,] flow, float[,] riverMask, float[,] lakeMask)
        {
            int sizeX = erosionRisk.GetLength(0);
            int sizeZ = erosionRisk.GetLength(1);
            var copy = (float[,])erosionRisk.Clone();
            double riverSuppression = Math.Clamp(worldConfig.Caves.RiverSuppressionWeight, 0.0, 1.0);
            double rimErosion = Math.Clamp(worldConfig.Water.LakeRimErosionWeight, 0.0, 1.0);
            double riparianGuard = Math.Clamp(worldConfig.Caves.RiparianCaveGuardWeight, 0.0, 1.0);
            double aquiferBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            double spillRetention = Math.Clamp(worldConfig.Lakes.SpillRetentionWeight, 0.0, 1.0);
            double groundwaterConnectivity = Math.Clamp(worldConfig.Caves.GroundwaterConnectivityWeight, 0.0, 1.0);
            double caveSealBase = Math.Clamp(aquiferBarrier * 0.45 + spillRetention * 0.2 + groundwaterConnectivity * 0.2 + riparianGuard * 0.15, 0.0, 1.35);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? Mathf.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = Math.Max(river, lake);
                    if (wetness <= 0.01)
                    {
                        continue;
                    }

                    double nearbyRiver = riverMask != null ? SampleInterior(riverMask, x, z) : 0.0;
                    double nearbyLake = lakeMask != null ? SampleInterior(lakeMask, x, z) : 0.0;
                    double confluence = Math.Clamp((nearbyRiver + nearbyLake) * 0.5, 0.0, 1.0);
                    double subsurfacePressure = Math.Clamp(hydrology[x, z] * 0.4 + flow[x, z] * 0.35 + confluence * 0.25, 0.0, 1.5);
                    double variance = SampleInterior(copy, x, z);
                    double moistureGuard = wetness * riparianGuard + subsurfacePressure * caveSealBase * 0.2;
                    double wetBuffer = wetness * (riverSuppression * 0.65 + rimErosion * 0.25) + moistureGuard * (0.8 + confluence * 0.2);
                    double stability = 1.0 + variance * (0.2 + caveSealBase * 0.08);
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Min(1.0, copy[x, z] + wetBuffer * stability));
                }
            }

            Smooth2D(
                erosionRisk,
                Math.Max(1, worldConfig.Caves.StabilitySmoothIterations),
                Mathf.Clamp01((float)(worldConfig.Caves.StabilitySmoothBlend * 0.35 + riparianGuard * 0.15 + caveSealBase * 0.08)));
        }

        private void ApplyFloodplainBasinPressureCoupling(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask)
        {
            double couplingWeight = Math.Clamp(
                worldConfig.Water.HydrologyContinuityWeight * 0.30 +
                worldConfig.Water.HydrologyThalwegStabilityWeight * 0.20 +
                worldConfig.Lakes.SpillwayContinuityWeight * 0.28 +
                worldConfig.Caves.GroundwaterConnectivityWeight * 0.22,
                0.0,
                1.0);
            if (couplingWeight <= 0.01)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(2, worldConfig.Water.HydrologyEdgeBlendRadius + 1);
            double divergenceClamp = Math.Max(0.0001, worldConfig.Water.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, worldConfig.Water.HydrologyVarianceClamp);
            double lakeInflowWeight = Math.Clamp(worldConfig.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double confluenceBoost = Math.Clamp(worldConfig.Water.RiverConfluenceBoost, 0.0, 2.0);
            double aquiferBarrier = Math.Clamp(worldConfig.Caves.AquiferBarrierWeight, 0.0, 1.0);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? Mathf.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = Math.Max(river, lake);
                    if (wetness <= 0.005)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = erosionCopy[x, z];
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double nearbyRiver = riverMask != null ? SampleInterior(riverMask, x, z) : 0.0;
                    double nearbyLake = lakeMask != null ? SampleInterior(lakeMask, x, z) : 0.0;
                    double confluence = Math.Clamp((nearbyRiver + nearbyLake) * 0.5, 0.0, 1.2);
                    double basinSupport = 1.0 - Math.Clamp(slope * 0.05 + relief / 28.0, 0.0, 0.92);
                    double pressure = Math.Clamp(
                        wetness * (0.42 + confluence * 0.22 + basinSupport * 0.2) +
                        (hydro + seamHydro) * 0.08 +
                        (flowNode + seamFlow) * 0.05,
                        0.0,
                        1.5);
                    double leakBrake = 1.0 - Math.Clamp(divergence * 0.45 + erosion * 0.28 + relief / 36.0, 0.0, 0.82);
                    if (leakBrake <= 0.0)
                    {
                        continue;
                    }

                    double coupling = pressure * couplingWeight * leakBrake;
                    double hydroTarget = hydro +
                        coupling * (0.18 + lakeInflowWeight * 0.12 + aquiferBarrier * 0.1) -
                        erosion * 0.03;
                    double flowTarget = flowNode * (1.0 - coupling * 0.1) +
                        (seamFlow + coupling * (0.14 + confluenceBoost * 0.06)) * coupling * 0.1;
                    double erosionTarget = erosion * (1.0 - coupling * 0.09) + pressure * 0.06 + confluence * 0.04;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.62)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(worldConfig.Water.HydrologyEdgeNormalizationBlend * 0.50)), (float)(varianceClamp * 1.25));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(worldConfig.Water.HydrologySmoothBlend * 0.28)));
        }

        private void ApplyHyporheicExchangeRelay(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask)
        {
            double relayWeight = Math.Clamp(
                profile.HydrologyContinuityWeight * 0.24 +
                profile.HydrologyThalwegStabilityWeight * 0.22 +
                profile.RiverEdgeContinuityWeight * 0.22 +
                profile.LakeOutflowStabilityWeight * 0.18 +
                profile.CaveGroundwaterConnectivityWeight * 0.14,
                0.0,
                1.0);
            if (relayWeight <= 0.01)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(2, profile.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, profile.HydrologyVarianceClamp);
            double slopePenalty = Math.Max(0.0, profile.HydrologySlopePenalty);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? Mathf.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = Math.Max(river, lake);
                    if (wetness <= 0.01)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = erosionCopy[x, z];
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double confluence = Math.Clamp(
                        (riverMask != null ? SampleInterior(riverMask, x, z) : 0.0) * 0.55 +
                        (lakeMask != null ? SampleInterior(lakeMask, x, z) : 0.0) * 0.45,
                        0.0,
                        1.0);
                    double storageBias = Math.Clamp((seaLevel + 10 - heightMap[x, z]) / 18.0, 0.0, 1.0);
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.25);
                    double exchange = wetness * (0.2 + continuity * 0.24 + confluence * 0.2 + storageBias * 0.16);
                    exchange *= relayWeight;
                    exchange *= 1.0 - Math.Clamp(
                        divergence * 0.42 +
                        slope * slopePenalty * 0.01 +
                        relief / 42.0 +
                        erosion * 0.32,
                        0.0,
                        0.86);
                    if (exchange <= 0.01)
                    {
                        continue;
                    }

                    double hydroTarget = hydro +
                        exchange * (0.16 + profile.LakeInflowBlendWeight * 0.08) -
                        erosion * 0.02;
                    double flowTarget = flowNode * (1.0 - exchange * 0.1) +
                        (seamFlow + continuity * 0.14 + confluence * 0.06) * exchange * 0.1;
                    double erosionTarget = erosion * (1.0 - exchange * 0.08) + exchange * 0.04;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.58)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.46)), (float)(varianceClamp * 1.2));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(profile.HydrologySmoothBlend * 0.24)));
        }

        private void ApplyRiparianAquiferMomentumCoupling(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk,
            float[,] riverMask,
            float[,] lakeMask)
        {
            double couplingWeight = Math.Clamp(
                profile.HydrologyContinuityWeight * 0.27 +
                profile.HydrologyFlowMemoryWeight * 0.21 +
                profile.RiverEdgeContinuityWeight * 0.18 +
                profile.LakeSpillRetentionWeight * 0.18 +
                profile.CaveGroundwaterConnectivityWeight * 0.16,
                0.0,
                1.2);
            if (couplingWeight <= 0.01)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(2, profile.HydrologyEdgeBlendRadius);
            double divergenceClamp = Math.Max(0.0001, profile.HydrologyFlowDivergenceClamp);
            double varianceClamp = Math.Max(0.001, profile.HydrologyVarianceClamp);
            double slopePenalty = Math.Max(0.0, profile.HydrologySlopePenalty);
            double edgeLock = Math.Clamp(profile.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double lakeInflow = Math.Clamp(profile.LakeInflowBlendWeight, 0.0, 1.0);
            double confluenceBoost = Math.Clamp(profile.RiverConfluenceBoost, 0.0, 2.0);
            double caveGuard = Math.Clamp(profile.RiparianCaveGuardWeight, 0.0, 1.0);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            var erosionCopy = (float[,])erosionRisk.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = Mathf.Clamp01(riverMask[x, z]);
                    double lake = Mathf.Clamp01(lakeMask[x, z]);
                    double wetness = Math.Max(river, lake);
                    if (wetness <= 0.005)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double seamHydro = SampleInterior(hydroCopy, x, z);
                    double flowNode = flowCopy[x, z];
                    double seamFlow = SampleInterior(flowCopy, x, z);
                    double erosion = erosionCopy[x, z];
                    double slope = ComputeSlope(heightMap, x, z);
                    double relief = ComputeLocalRelief(heightMap, x, z, edgeRadius);
                    double divergence = Math.Min(1.0, Math.Abs(flowNode - seamFlow) / divergenceClamp);
                    double nearbyRiver = SampleInterior(riverMask, x, z);
                    double nearbyLake = SampleInterior(lakeMask, x, z);
                    double confluence = Math.Clamp((nearbyRiver + nearbyLake) * 0.5, 0.0, 1.2);
                    double continuity = Math.Clamp((hydro + seamHydro + flowNode + seamFlow) * 0.25, 0.0, 1.35);
                    double aquiferSupport = 1.0 - Math.Clamp(
                        divergence * 0.44 +
                        slope * slopePenalty * 0.009 +
                        relief / 38.0 +
                        erosion * 0.34,
                        0.0,
                        0.9);
                    if (aquiferSupport <= 0.0)
                    {
                        continue;
                    }

                    double momentum = Math.Clamp(
                        wetness * (0.22 + continuity * 0.2 + confluence * 0.18) +
                        (1.0 - Math.Abs(river - lake)) * 0.14,
                        0.0,
                        1.5);
                    double coupling = momentum * couplingWeight * aquiferSupport;
                    double lockedHydro = hydro * (1.0 - edgeLock * 0.35) + seamHydro * edgeLock * 0.35;
                    double hydroTarget = lockedHydro + coupling * (0.15 + lakeInflow * 0.1 + caveGuard * 0.08) - erosion * 0.02;
                    double flowTarget = flowNode * (1.0 - coupling * 0.09) +
                        (seamFlow + continuity * 0.12 + confluence * 0.08 * confluenceBoost) * coupling * 0.1;
                    double erosionTarget = erosion * (1.0 - coupling * 0.1) + wetness * 0.03 + Math.Max(0.0, confluence - 0.35) * 0.04;

                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.35));
                    erosionRisk[x, z] = Mathf.Clamp01((float)Math.Clamp(erosionTarget, 0.0, 1.0));
                    riverMask[x, z] = Mathf.Clamp01((float)Math.Clamp(river + hydrology[x, z] * 0.008 + continuity * 0.006, 0.0, 1.0));
                    lakeMask[x, z] = Mathf.Clamp01((float)Math.Clamp(lake + hydrology[x, z] * 0.007 + confluence * 0.005, 0.0, 1.0));
                }
            }

            NormalizeEdges(hydrology, edgeRadius, Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.54)), (float)varianceClamp);
            NormalizeEdges(flow, edgeRadius, Mathf.Clamp01((float)(profile.HydrologyEdgeNormalizationBlend * 0.44)), (float)(varianceClamp * 1.2));
            Smooth2D(erosionRisk, 1, Mathf.Clamp01((float)(profile.HydrologySmoothBlend * 0.22)));
        }

        private void ApplyFlowMemory(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            float memoryWeight = Mathf.Clamp01(profile.HydrologyFlowMemoryWeight + profile.HydrologyFlowPersistence * 0.2f);
            float watershedBlend = Mathf.Clamp01(profile.HydrologyWatershedStitchWeight);
            float flowShadowWeight = Mathf.Clamp01(profile.HydrologyFlowShadowWeight);
            int watershedRadius = Math.Max(1, profile.HydrologyWatershedStitchRadius);
            if (memoryWeight <= 0f && watershedBlend <= 0f && flowShadowWeight <= 0f)
            {
                return;
            }

            var buffer = (float[,])flow.Clone();
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float flowValue = flow[x, z];
                    float hydro = hydrology[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    float downhillFlow = flow[downX, downZ];
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);

                    float relief = ComputeLocalRelief(heightMap, x, z, Math.Max(1, watershedRadius));
                    float basinWeight = Mathf.Clamp01(1f - relief / 12f);
                    float continuity = 1f + hydro * profile.HydrologyContinuityWeight + neighbourHydro * 0.25f;
                    float memory = flowValue * (1f - memoryWeight);
                    memory += (downhillFlow + flowValue) * (memoryWeight * 0.2f);
                    memory += neighbourFlow * (memoryWeight * 0.35f);
                    memory += (hydro + neighbourHydro) * memoryWeight * 0.15f;
                    memory *= continuity;
                    memory *= 1f + basinWeight * 0.15f;
                    memory *= 1f - Mathf.Clamp(hydrologyGradient * flowShadowWeight * 0.25f, 0f, 0.3f);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float edgeFalloff = 1f - Mathf.Clamp(edgeDistance / (float)(watershedRadius + 1), 0f, 1f);
                    float edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0f)
                    {
                        float seamAnchor = neighbourHydro * 0.35f + hydro * 0.35f + neighbourFlow * 0.3f;
                        memory = memory * (1f - edgeRepair * 0.55f) + seamAnchor * edgeRepair;
                    }

                    float clampBase = Mathf.Max(flowValue + 1.5f, profile.HydrologyFlowDivergenceClamp * 12f);
                    float basinClamp = Mathf.Lerp(clampBase, clampBase * 0.9f + (hydro + neighbourHydro) * 6f * 0.15f, basinWeight * memoryWeight * 0.5f);
                    buffer[x, z] = Mathf.Clamp(memory, 0f, basinClamp);
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
        }

        private float ComputeLocalRelief(int[,] heightMap, int x, int z, int radius)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int min = int.MaxValue;
            int max = int.MinValue;
            int samples = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                    {
                        continue;
                    }

                    int height = heightMap[nx, nz];
                    if (height <= 0)
                    {
                        continue;
                    }

                    min = Math.Min(min, height);
                    max = Math.Max(max, height);
                    samples++;
                }
            }

            if (samples == 0)
            {
                return 0f;
            }

            return max - min;
        }

        private void BlendHydrologyWithFlow(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            float flowBlend = Mathf.Clamp(profile.HydrologyContinuityWeight * 0.35f, 0.05f, 0.45f);
            float edgeBlend = Mathf.Clamp(profile.HydrologyEdgeFlowLockWeight * 0.5f, 0.0f, 0.45f);
            float confluenceBoost = Mathf.Clamp(profile.RiverConfluenceBoost, 0f, 2f);
            int edgeRadius = Mathf.Max(1, profile.HydrologyEdgeBlendRadius);
            float flowShadowWeight = Mathf.Max(0.05f, profile.HydrologyContinuityWeight * 0.45f + profile.HydrologyEdgeStabilityWeight * 0.25f);
            float flowShadowSlopeWeight = Mathf.Max(0.01f, profile.HydrologyGradientWeight * 0.2f + profile.HydrologyEdgeVarianceClamp * 0.35f);
            float directionalBias = Mathf.Clamp(profile.HydrologyDirectionalBlend * 0.5f, 0f, 0.5f);

            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float normalizedFlow = Mathf.Clamp(flowValue / Mathf.Max(1f, profile.RiverDepth), 0f, 1f);
                    float neighbourFlow = SampleInterior(flow, x, z) / Mathf.Max(1f, profile.RiverDepth);
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float edgeFactor = edgeBlend * Mathf.Clamp01(1f - edgeDistance / (float)(edgeRadius + 1));
                    float blend = Mathf.Clamp(flowBlend + edgeFactor, 0f, 0.9f);

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    float directionalHydro = hydrology[downX, downZ];
                    float directionalFlow = Mathf.Clamp(flow[downX, downZ] / Mathf.Max(1f, profile.RiverDepth), 0f, 1f);
                    float directionalWeight = Mathf.Clamp((Mathf.Abs(downhill.x) + Mathf.Abs(downhill.y)) * directionalBias + directionalFlow * 0.2f, 0f, 0.45f);

                    float confluence = confluenceBoost > 0f
                        ? (neighbourFlow * 0.5f + neighbourHydro * 0.25f + hydrologyGradient * 0.15f) * confluenceBoost
                        : 0f;

                    float flowShadow = Mathf.Clamp(
                        (normalizedFlow + neighbourFlow) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5f +
                        directionalFlow * flowShadowWeight * 0.15f,
                        0f,
                        0.7f);

                    float blended = hydro * (1f - blend) + normalizedFlow * blend;
                    blended = blended * (1f - flowShadow * 0.35f) + neighbourHydro * flowShadow * 0.35f;
                    blended = blended * (1f - directionalWeight) + directionalHydro * directionalWeight;
                    blended *= 1f + confluence;
                    buffer[x, z] = Mathf.Clamp(blended, 0f, 1.25f);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
            ClampVariance(hydrology, profile.HydrologyVarianceClamp);
            ApplyFlowShadow(
                hydrology,
                flow,
                Mathf.Max(0.05f, profile.HydrologyContinuityWeight * 0.35f),
                Mathf.Max(0.01f, profile.HydrologyGradientWeight * 0.15f));
        }

        private void ApplyCurvatureHydrologyGuide(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            float curvatureWeight = Mathf.Clamp(profile.HydrologyCurvatureWeight, 0f, 1.5f);
            if (curvatureWeight <= 0f)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            float slopePenalty = Mathf.Max(0f, profile.HydrologySlopePenalty);
            float gradientWeight = Mathf.Clamp01(profile.HydrologyGradientWeight);
            float varianceClamp = Mathf.Max(0.001f, profile.HydrologyVarianceClamp);
            float flowClamp = Mathf.Max(1f, profile.HydrologyFlowDivergenceClamp * 12f);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydroCopy[x, z];
                    float flowValue = flowCopy[x, z];
                    float seamHydro = SampleInterior(hydroCopy, x, z);
                    float seamFlow = SampleInterior(flowCopy, x, z);
                    float curvature = SampleCurvature(heightMap, x, z);
                    float basinAssist = Mathf.Clamp(curvature * curvatureWeight * 0.35f, -0.65f, 0.65f);
                    float ridgePenalty = Mathf.Max(0f, -basinAssist);
                    float slope = ComputeSlope(heightMap, x, z);
                    float slopeBrake = 1f - Mathf.Clamp01(slope * slopePenalty * 0.02f);
                    float gradient = Mathf.Abs(seamHydro - hydro) + Mathf.Abs(seamFlow - flowValue) * 0.35f;
                    float stability = 1f - Mathf.Clamp01(gradient * gradientWeight * 0.35f + ridgePenalty * 0.35f);

                    float hydroAnchor = hydro * 0.55f + seamHydro * 0.3f + seamFlow * 0.15f;
                    float targetHydro = hydroAnchor + basinAssist * 0.35f;
                    targetHydro *= slopeBrake * stability;
                    float clampDelta = varianceClamp * 0.35f;
                    hydrology[x, z] = Mathf.Clamp(targetHydro, Mathf.Max(0f, hydro - clampDelta), Mathf.Min(1.05f, hydro + clampDelta));

                    float flowAnchor = flowValue * 0.6f + seamFlow * 0.25f + seamHydro * 0.15f;
                    float targetFlow = flowAnchor + Mathf.Max(0f, basinAssist) * 0.25f;
                    targetFlow *= slopeBrake;
                    targetFlow *= 1f - Mathf.Clamp01(ridgePenalty * 0.25f + gradient * gradientWeight * 0.25f);
                    flow[x, z] = Mathf.Clamp(targetFlow, 0f, flowClamp);
                }
            }
        }

        private void ApplyHydrologyContinuityEnvelope(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            float envelope = Mathf.Clamp(profile.HydrologyVarianceBlend * 0.5f + profile.HydrologyFlowMemoryWeight * 0.35f, 0.05f, 0.9f);
            float flowMemoryWeight = Mathf.Clamp01(profile.HydrologyFlowMemoryWeight);
            float slopePenalty = Mathf.Max(0f, profile.HydrologySlopePenalty);
            float stabilityWeight = Mathf.Clamp01(profile.HydrologyEdgeStabilityWeight);
            float varianceClamp = Mathf.Max(0f, profile.HydrologyVarianceClamp);
            float flowShadowWeight = Mathf.Clamp01(profile.HydrologyFlowShadowWeight);
            float flowShadowSlopeWeight = Mathf.Clamp01(profile.HydrologyFlowShadowSlopeWeight);
            float flowClamp = Mathf.Max(profile.HydrologyFlowDivergenceClamp * 12f, 2.5f);
            float edgeBlendBase = Mathf.Clamp(profile.HydrologyEdgeBlendRadius / (float)Mathf.Max(1, chunkSize), 0f, 0.35f);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);
                    float flowGradient = Mathf.Abs(neighbourFlow - flowValue);
                    float slope = ComputeSlope(heightMap, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    float directionalHydro = hydrology[downX, downZ];
                    float directionalFlow = flow[downX, downZ];

                    float edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float edgeFactor = edgeBlendBase * (1f - Mathf.Clamp(edgeDistance / (profile.HydrologyEdgeBlendRadius + 1f), 0f, 1f));
                    float stability = 1f - Mathf.Clamp(
                        (hydrologyGradient + flowGradient) * stabilityWeight * 0.5f +
                        slope * slopePenalty * 0.02f,
                        0f,
                        0.85f);
                    float flowShadow = Mathf.Clamp(
                        (flowValue / Mathf.Max(1f, profile.RiverDepth)) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5f,
                        0f,
                        0.8f);
                    float anchor = hydro * 0.55f + neighbourHydro * 0.25f + directionalHydro * 0.2f;
                    float directionalBias = (Mathf.Abs(downhill.x) + Mathf.Abs(downhill.y)) * 0.25f;
                    float blend = Mathf.Clamp(envelope * stability + edgeFactor + directionalBias * 0.15f, 0f, 0.9f);
                    float harmonizedHydro = hydro * (1f - blend) + anchor * blend;
                    harmonizedHydro *= 1f - flowShadow * 0.15f;
                    harmonizedHydro = Mathf.Clamp(harmonizedHydro, 0f, varianceClamp);
                    hydrology[x, z] = Mathf.Clamp01(harmonizedHydro);

                    float flowAnchor = flowValue * (0.6f + flowMemoryWeight * 0.25f) + neighbourFlow * 0.25f + directionalFlow * 0.15f + hydrologyGradient * flowMemoryWeight * 0.1f;
                    float blendedFlow = flowValue * (1f - blend * 0.35f) + flowAnchor * blend * 0.35f;
                    blendedFlow = Mathf.Clamp(blendedFlow, 0f, flowClamp * (1f - flowShadow * 0.1f));
                    flow[x, z] = blendedFlow;
                }
            }
        }

        private void ApplyHydrologyEdgeEnvelope(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            float continuityWeight = Mathf.Clamp01(profile.HydrologyContinuityWeight);
            float memoryWeight = Mathf.Clamp01(profile.HydrologyFlowMemoryWeight);
            float varianceClamp = Math.Max(0.001f, profile.HydrologyVarianceClamp);
            float flowClamp = Math.Max(0.5f, profile.HydrologyFlowDivergenceClamp * 12f);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= edgeRadius)
                    {
                        continue;
                    }

                    float edgeWeight = 1f - Mathf.Clamp(edgeDistance / (float)(edgeRadius + 1), 0f, 1f);
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float interiorHydro = SampleInterior(hydrology, x, z);
                    float interiorFlow = SampleInterior(flow, x, z);
                    float gradient = Mathf.Abs(interiorHydro - hydro) + Mathf.Abs(interiorFlow - flowValue) * 0.35f;
                    float stability = 1f - Mathf.Clamp(gradient * profile.HydrologyEdgeVarianceClamp * 0.5f, 0f, 0.85f);
                    float seamAnchor = (hydro + interiorHydro + flowValue * 0.5f + interiorFlow * 0.5f) / 3f;
                    float targetHydro = hydro * (1f - edgeWeight * 0.25f) + seamAnchor * edgeWeight * (0.65f + continuityWeight * 0.35f);
                    targetHydro += interiorFlow * memoryWeight * 0.05f;
                    hydrology[x, z] = Mathf.Clamp(targetHydro * stability, 0f, varianceClamp);

                    float targetFlow = flowValue * (1f - edgeWeight * 0.25f) + Mathf.Max(flowValue, interiorFlow) * edgeWeight;
                    targetFlow += seamAnchor * memoryWeight * 0.1f;
                    flow[x, z] = Mathf.Clamp(targetFlow * stability, 0f, flowClamp + 2f);
                }
            }
        }

        private void NormalizeHydrologyFlowEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, Math.Max(profile.HydrologyEdgeBlendRadius, profile.HydrologyWatershedStitchRadius));
            int iterations = Math.Max(1, profile.HydrologyEdgeNormalizationIterations);
            float blendBase = Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend);
            float memoryWeight = Mathf.Clamp01(profile.HydrologyFlowMemoryWeight);
            float watershedBlend = Mathf.Clamp01(profile.HydrologyWatershedStitchWeight);
            float varianceClamp = Mathf.Max(0f, profile.HydrologyEdgeVarianceClamp);

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            continue;
                        }

                        float edgeFalloff = 1f - Mathf.Clamp01(edgeDistance / (float)(edgeRadius + 1));
                        float blend = blendBase * edgeFalloff;
                        if (blend <= 0f)
                        {
                            continue;
                        }

                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        float neighbourHydro = SampleInterior(hydrology, x, z);
                        float neighbourFlow = SampleInterior(flow, x, z);
                        float seamAnchor = (neighbourHydro + hydro) * 0.5f + neighbourFlow * memoryWeight * 0.25f;

                        float targetHydro = (neighbourHydro * (1f + memoryWeight * 0.35f) + hydro * 0.65f + flowValue * memoryWeight * 0.15f) / (1.8f + memoryWeight * 0.35f);
                        float edgeRepair = watershedBlend * edgeFalloff;
                        targetHydro = (targetHydro + seamAnchor * (0.25f + edgeRepair * 0.35f)) / (1.25f + edgeRepair * 0.35f);

                        float candidateHydro = hydro + (targetHydro - hydro) * blend;
                        if (varianceClamp > 0f)
                        {
                            float clampRange = varianceClamp * 0.35f;
                            candidateHydro = Mathf.Clamp(candidateHydro, hydro - clampRange, hydro + clampRange);
                        }
                        hydroBuffer[x, z] = Mathf.Clamp(candidateHydro, 0f, 1.05f);

                        float targetFlow = (neighbourFlow * (1f + memoryWeight) + flowValue + hydro * memoryWeight * 0.35f) / (2f + memoryWeight);
                        targetFlow = (targetFlow + seamAnchor * (0.2f + edgeRepair * 0.35f)) / (1.2f + edgeRepair * 0.35f);
                        float clampMax = Mathf.Max(flowValue + 1.5f, profile.HydrologyFlowDivergenceClamp * 12f);
                        flowBuffer[x, z] = Mathf.Clamp(targetFlow, 0f, clampMax);
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }
        }

        private void DiffuseHydrologyEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            if (sizeX < 4 || sizeZ < 4)
            {
                return;
            }

            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            int iterations = Math.Max(1, Math.Min(3, profile.HydrologyEdgeStabilityIterations / 2));
            float baseBlend = Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend * 0.5f + profile.HydrologyContinuityWeight * 0.35f);
            float varianceClamp = Mathf.Max(0.001f, profile.HydrologyEdgeVarianceClamp);
            float fluxBlend = Mathf.Clamp01(profile.HydrologyEdgeFluxBlend);
            float flowClamp = Mathf.Max(0.5f, profile.HydrologyFlowDivergenceClamp * 12f);

            if (baseBlend <= 0f)
            {
                return;
            }

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            hydroBuffer[x, z] = hydrology[x, z];
                            flowBuffer[x, z] = flow[x, z];
                            continue;
                        }

                        float tension = 1f - Mathf.Clamp01(edgeDistance / (float)Math.Max(1, edgeRadius));
                        float blend = baseBlend * (0.65f + tension * 0.35f);
                        float neighbourHydro = SampleInterior(hydrology, x, z);
                        float neighbourFlow = SampleInterior(flow, x, z);
                        float hydroVariance = SampleVariance(hydrology, x, z);
                        float flowVariance = SampleVariance(flow, x, z);

                        float targetHydro = hydrology[x, z] * (1f - blend) + neighbourHydro * blend;
                        targetHydro -= hydroVariance * varianceClamp * 0.5f;
                        targetHydro = Mathf.Clamp(targetHydro, 0f, 1.25f);

                        float targetFlow = flow[x, z] * (1f - blend) + neighbourFlow * blend;
                        targetFlow -= flowVariance * varianceClamp * 0.35f;
                        targetFlow += targetHydro * fluxBlend * 0.1f;
                        targetFlow = Mathf.Clamp(targetFlow, 0f, Mathf.Max(flow[x, z] + 1f, flowClamp));

                        hydroBuffer[x, z] = Mathf.Clamp01(targetHydro);
                        flowBuffer[x, z] = Mathf.Clamp01(targetFlow);
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }

            NormalizeEdgeBands(hydrology, edgeRadius, baseBlend, varianceClamp);
            NormalizeEdgeBands(flow, edgeRadius, baseBlend * 0.85f, varianceClamp * 1.35f);
        }

        private void ApplyWaterTableEnvelope(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int waterLevel = Mathf.Clamp(profile.GlobalWaterLevel > 0 ? profile.GlobalWaterLevel : worldConfig.Terrain.SeaLevel, 1, worldHeight - 1);
            double clampRange = Math.Max(1.0, profile.HydrologyWaterTableClampRange + 6.0);
            double envelopeWeight = Math.Clamp(profile.HydrologyWaterTableClampWeight + 0.08, 0.0, 1.0);
            double seamBlend = Math.Clamp(profile.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, profile.HydrologyVarianceClamp);
            double flowClamp = Math.Max(2.5, profile.HydrologyFlowDivergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int surface = heightMap[x, z];
                    double waterBias = 1.0 - Math.Clamp(Math.Abs(surface - waterLevel) / clampRange, 0.0, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double seamWeight = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double blend = envelopeWeight * (0.6 * waterBias + 0.4 * seamWeight);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double neighbourHydro = SampleInterior(hydrology, x, z);
                    double neighbourFlow = SampleInterior(flow, x, z);
                    double stability = 1.0 - Math.Clamp(Math.Abs(surface - waterLevel) / (clampRange * 1.25), 0.0, 0.65);

                    double targetHydro = hydro * (1.0 - blend) + (hydro + neighbourHydro * (1.0 + seamWeight * seamBlend)) * 0.5 * blend;
                    targetHydro *= 1.0 + waterBias * 0.12;
                    targetHydro *= stability;
                    hydrology[x, z] = Mathf.Clamp01((float)Math.Clamp(targetHydro, 0.0, varianceClamp + 0.75));

                    double flowValue = flow[x, z];
                    double targetFlow = flowValue * (1.0 + waterBias * 0.1) + neighbourFlow * (0.15 + seamWeight * seamBlend * 0.25);
                    double flowBlend = Math.Clamp(blend + seamBlend * 0.15, 0.0, 1.0);
                    double blendedFlow = flowValue + (targetFlow - flowValue) * flowBlend;
                    flow[x, z] = Mathf.Clamp01((float)Math.Clamp(blendedFlow, 0.0, flowClamp + 2.0));
                }
            }

            ClampVariance(hydrology, (float)varianceClamp);
            ClampVariance(flow, (float)(varianceClamp * 1.25));
            RelaxEdges(hydrology, Math.Max(1, profile.HydrologySeamRelaxIterations), (float)(seamBlend * 0.65));
            RelaxEdges(flow, Math.Max(1, profile.HydrologySeamRelaxIterations), (float)(seamBlend * 0.45));
        }

        private void ApplyCrossChunkHydrologyStitch(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            float blendBase = Mathf.Clamp01(profile.HydrologySeamRelaxBlend + profile.HydrologyEdgeFluxBlend * 0.25f);
            float flowBlend = Mathf.Clamp01(profile.HydrologyEdgeNormalizationBlend + profile.HydrologyFlowMemoryWeight * 0.25f);
            float varianceClamp = Mathf.Max(0f, profile.HydrologyEdgeVarianceClamp);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    float falloff = 1f - Mathf.Clamp01(edgeDistance / (float)(edgeRadius + 1));
                    float interiorHydro = SampleInterior(hydroCopy, x, z);
                    float interiorFlow = SampleInterior(flowCopy, x, z);
                    float hydroTarget = hydroCopy[x, z] * (1f - blendBase * falloff * 0.5f) + interiorHydro * blendBase * falloff * 0.5f;
                    hydroTarget += interiorFlow * flowBlend * 0.05f;
                    if (varianceClamp > 0f)
                    {
                        float clampRange = varianceClamp * falloff * 0.35f;
                        hydroTarget = Mathf.Clamp(hydroTarget, hydroCopy[x, z] - clampRange, hydroCopy[x, z] + clampRange);
                    }

                    hydrology[x, z] = Mathf.Clamp01(hydroTarget);

                    float flowTarget = flowCopy[x, z] * (1f - flowBlend * falloff) + interiorFlow * flowBlend * falloff;
                    flow[x, z] = Mathf.Clamp(flowTarget, 0f, Mathf.Max(flowCopy[x, z] + 1f, profile.HydrologyFlowDivergenceClamp * 12f));
                }
            }
        }

        private void ApplyHydrologyEdgeCohesion(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, profile.HydrologyEdgeBlendRadius);
            float seamBlend = Mathf.Clamp(profile.HydrologySeamRelaxBlend + profile.HydrologyEdgeStabilityWeight * 0.35f, 0.05f, 0.95f);
            float memoryWeight = Mathf.Clamp01(profile.HydrologyFlowMemoryWeight);
            float varianceClamp = Mathf.Max(0f, profile.HydrologyEdgeVarianceClamp);
            float slopePenalty = Mathf.Max(0f, profile.HydrologySlopePenalty);
            float gradientWeight = Mathf.Clamp01(profile.HydrologyGradientWeight);
            float flowClamp = Mathf.Max(2.5f, profile.HydrologyFlowDivergenceClamp * 12f);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    float falloff = 1f - Mathf.Clamp01(edgeDistance / (float)(edgeRadius + 1));
                    float blend = seamBlend * falloff;
                    float hydro = hydroCopy[x, z];
                    float flowValue = flowCopy[x, z];
                    float neighbourHydro = SampleInterior(hydroCopy, x, z);
                    float neighbourFlow = SampleInterior(flowCopy, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                    float directionalHydro = hydroCopy[downX, downZ];
                    float directionalFlow = flowCopy[downX, downZ];
                    float slope = ComputeSlope(heightMap, x, z);
                    float hydroGradient = Mathf.Abs(neighbourHydro - hydro);
                    float flowGradient = Mathf.Abs(neighbourFlow - flowValue);
                    float stability = 1f - Mathf.Clamp(
                        (hydroGradient + flowGradient) * profile.HydrologyEdgeStabilityWeight * 0.35f +
                        slope * slopePenalty * 0.02f,
                        0f,
                        0.85f);

                    float anchorHydro = hydro * (0.6f + memoryWeight * 0.25f) + neighbourHydro * 0.25f + directionalHydro * 0.15f;
                    float directionalBias = (Mathf.Abs(downhill.x) + Mathf.Abs(downhill.y)) * 0.15f;
                    float edgeAnchor = hydro * (1f - varianceClamp * falloff * 0.35f) + neighbourHydro * varianceClamp * falloff * 0.35f;
                    float harmonized = hydro * (1f - blend) + anchorHydro * blend;
                    harmonized = harmonized * stability + edgeAnchor * (1f - stability) * 0.25f;
                    harmonized *= 1f - Mathf.Clamp(hydroGradient * gradientWeight * 0.15f + directionalBias, 0f, 0.4f);
                    float clampDelta = varianceClamp * falloff;
                    hydrology[x, z] = Mathf.Clamp(harmonized, hydro - clampDelta, hydro + clampDelta);

                    float flowAnchor = flowValue * (0.6f + memoryWeight * 0.25f) + neighbourFlow * 0.25f + directionalFlow * 0.15f + hydroGradient * memoryWeight * 0.1f;
                    float blendedFlow = flowValue * (1f - blend * 0.35f) + flowAnchor * blend * 0.35f;
                    blendedFlow = Mathf.Clamp(blendedFlow, 0f, flowClamp * (1f + varianceClamp * 0.15f));
                    flow[x, z] = blendedFlow;
                }
            }

            ClampVariance(hydrology, varianceClamp);
            ClampVariance(flow, varianceClamp * 1.25f);
            RelaxEdges(hydrology, Math.Max(1, profile.HydrologySeamRelaxIterations), seamBlend * 0.65f);
            RelaxEdges(flow, Math.Max(1, profile.HydrologySeamRelaxIterations), seamBlend * 0.45f);
        }

        private void HarmonizeHydrologyWithSurface(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            float edgeClamp = Mathf.Clamp01(profile.HydrologyEdgeVarianceClamp);
            float gradientWeight = Mathf.Clamp01(profile.HydrologyGradientWeight);
            float stabilityWeight = Mathf.Clamp01(profile.HydrologyEdgeStabilityWeight);
            float flowPersistence = Mathf.Clamp01(profile.HydrologyFlowPersistence);
            float slopePenalty = Mathf.Max(0.0f, profile.HydrologySlopePenalty);
            float curvatureWeight = Mathf.Clamp01(profile.HydrologyCurvatureWeight);
            int edgeRadius = Mathf.Max(1, profile.HydrologyEdgeBlendRadius);
            float clampMax = Mathf.Max(2.5f, profile.HydrologyFlowDivergenceClamp * 12f);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float hydrologyGradient = Mathf.Abs(neighbourHydro - hydro);
                    float flowGradient = Mathf.Abs(neighbourFlow - flowValue);
                    float slope = ComputeSlope(heightMap, x, z);
                    float curvature = Mathf.Abs(SampleCurvature(heightMap, x, z)) * curvatureWeight * 0.05f;

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                    int downZ = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);

                    float edgeDistance = Mathf.Min(Mathf.Min(x, sizeX - 1 - x), Mathf.Min(z, sizeZ - 1 - z));
                    float edgeBlend = 1f - Mathf.Clamp01(edgeDistance / (edgeRadius + 1f));

                    float stability = 1f - Mathf.Clamp01((hydrologyGradient + flowGradient) * stabilityWeight);
                    stability *= 1f - Mathf.Clamp01(slope / Mathf.Max(1f, slopePenalty * 1.1f));

                    float anchorHydro = hydro * (0.6f + flowPersistence * 0.25f) + neighbourHydro * 0.25f + neighbourFlow * 0.15f;
                    float directionalAnchor = hydrology[downX, downZ] * 0.25f + flow[downX, downZ] * 0.15f;
                    float blend = Mathf.Clamp01(
                        hydrologyGradient * (0.35f + gradientWeight * 0.35f) +
                        flowGradient * 0.15f +
                        edgeBlend * 0.35f +
                        curvature);

                    float harmonized = (anchorHydro + directionalAnchor) * stability;
                    float anchoredHydro = hydro * (1f - blend) + harmonized * blend;
                    float edgeAnchor = hydro * (1f - edgeBlend * edgeClamp) + neighbourHydro * edgeBlend * edgeClamp;
                    hydrology[x, z] = Mathf.Clamp(
                        anchoredHydro * (1f - edgeBlend * 0.35f) + edgeAnchor * edgeBlend * 0.35f,
                        0f,
                        1.25f);

                    float flowAnchor = hydrology[x, z] * 0.5f + flowValue * (0.5f + flowPersistence * 0.2f);
                    flow[x, z] = Mathf.Clamp(
                        flowValue * (1f - blend * 0.35f) + flowAnchor * blend * 0.35f,
                        0f,
                        clampMax);
                }
            }
        }

        private static void Smooth2D(float[,] field, int iterations, float blend)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float sum = field[x, z];
                        int samples = 1;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                                {
                                    continue;
                                }

                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = sum / samples;
                        buffer[x, z] = field[x, z] * (1f - blend) + average * blend;
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void Smooth2D(bool[,,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = field.GetLength(0);
            int sizeY = field.GetLength(1);
            int sizeZ = field.GetLength(2);

            for (int iter = 0; iter < iterations; iter++)
            {
                var buffer = new bool[sizeX, sizeY, sizeZ];
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        for (int y = 1; y < sizeY - 1; y++)
                        {
                            int neighbours = 0;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        if (dx == 0 && dy == 0 && dz == 0)
                                        {
                                            continue;
                                        }

                                        int nx = x + dx;
                                        int ny = y + dy;
                                        int nz = z + dz;
                                        if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ || ny < 0 || ny >= sizeY)
                                        {
                                            continue;
                                        }

                                        if (field[nx, ny, nz])
                                        {
                                            neighbours++;
                                        }
                                    }
                                }
                            }

                            bool carve = field[x, y, z];
                            if (neighbours >= 13)
                            {
                                buffer[x, y, z] = true;
                            }
                            else if (neighbours <= 3)
                            {
                                buffer[x, y, z] = false;
                            }
                            else
                            {
                                buffer[x, y, z] = blend > 0 ? neighbours >= 9 : carve;
                            }
                        }
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void DirectionalSmooth(int[,] heightMap, float[,] field, int iterations, float blend)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            if (iterations == 0 || blend <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        var downhill = ComputeDownhillVector(heightMap, x, z);
                        int nx = Mathf.Clamp(x + downhill.x, 0, sizeX - 1);
                        int nz = Mathf.Clamp(z + downhill.y, 0, sizeZ - 1);
                        float neighbour = field[nx, nz];
                        buffer[x, z] = field[x, z] * (1f - blend) + neighbour * blend;
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void RelaxEdges(float[,] field, int iterations, float blend)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (x > 0 && x < sizeX - 1 && z > 0 && z < sizeZ - 1)
                        {
                            continue;
                        }

                        float neighbour = SampleInterior(field, x, z);
                        field[x, z] = field[x, z] * (1f - blend) + neighbour * blend;
                    }
                }
            }
        }

        private static void StitchEdges(float[,] field, float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (blend <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = field[x, z] * (1f - blend) + interior * blend;
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void FillBasins(float[,] field, float strength, int iterations)
        {
            strength = Mathf.Clamp01(strength);
            iterations = Mathf.Max(0, iterations);
            if (strength <= 0.0f || iterations == 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float value = field[x, z];
                        float neighbour = SampleInterior(field, x, z);
                        if (value >= neighbour)
                        {
                            buffer[x, z] = value;
                            continue;
                        }

                        float delta = (neighbour - value) * strength * 0.5f;
                        buffer[x, z] = Mathf.Clamp01(value + delta);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void ApplyFlowShadow(float[,] hydrology, float[,] flow, float weight, float slopeWeight)
        {
            weight = Mathf.Clamp01(weight);
            slopeWeight = Mathf.Clamp01(slopeWeight);
            if (weight <= 0.0f && slopeWeight <= 0.0f)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    float flowShadow = Mathf.Clamp((flowValue + neighbourFlow) * 0.5f * weight, 0f, 0.6f);

                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    float slopeShadow = Mathf.Clamp(Mathf.Abs(hydro - neighbourHydro) * slopeWeight, 0f, 0.35f);

                    float dampened = hydro * (1f - flowShadow * 0.35f - slopeShadow * 0.35f) + neighbourHydro * (flowShadow * 0.2f);
                    buffer[x, z] = Mathf.Clamp01(dampened);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
        }

        private static void StabilizeEdges(float[,] field, int radius, int iterations, float weight, float fluxBlend)
        {
            radius = Math.Max(1, radius);
            iterations = Math.Max(0, iterations);
            weight = Mathf.Clamp01(weight);
            fluxBlend = Mathf.Clamp01(fluxBlend);
            if (iterations == 0 || weight <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isEdge = x < radius || z < radius || x >= sizeX - radius || z >= sizeZ - radius;
                        if (!isEdge)
                        {
                            buffer[x, z] = field[x, z];
                            continue;
                        }

                        float interior = SampleInterior(field, x, z);
                        double blend = weight * (1.0 - Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z)) / (double)radius);
                        double stabilised = field[x, z] * (1.0 - blend) + interior * blend;
                        buffer[x, z] = (float)(stabilised * (1.0 - fluxBlend) + interior * fluxBlend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void ApplyEdgeFlowLocks(int[,] heightMap, float[,] field, int radius, float lockWeight, float flowBias, float tangentWeight)
        {
            radius = Math.Max(1, radius);
            lockWeight = Mathf.Clamp01(lockWeight);
            flowBias = Mathf.Clamp01(flowBias);
            tangentWeight = Mathf.Clamp01(tangentWeight);
            if (lockWeight <= 0.0f && flowBias <= 0.0f && tangentWeight <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double blend = lockWeight * (1.0 - edgeDistance / (double)radius);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int nx = Math.Clamp(x + downhill.x, 0, sizeX - 1);
                    int nz = Math.Clamp(z + downhill.y, 0, sizeZ - 1);
                    float downhillValue = field[nx, nz];

                    int tx = Math.Clamp(x - downhill.y, 0, sizeX - 1);
                    int tz = Math.Clamp(z + downhill.x, 0, sizeZ - 1);
                    float tangentValue = field[tx, tz];

                    float interior = SampleInterior(field, x, z);
                    double flowAligned = field[x, z] * (1.0 - flowBias) + downhillValue * flowBias;
                    double tangentAligned = field[x, z] * (1.0 - tangentWeight) + tangentValue * tangentWeight;
                    double locked = (flowAligned * 0.6) + (tangentAligned * 0.4);
                    double blended = field[x, z] * (1.0 - blend) + interior * (blend * 0.3) + locked * (blend * 0.7);

                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, Math.Max(1.5, field[x, z] + 0.35));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void ApplyRiparianBuffer(float[,] field, int radius, float saturationBoost)
        {
            radius = Math.Max(0, radius);
            saturationBoost = Mathf.Clamp(saturationBoost, 0.0f, 2.0f);
            if (radius == 0 || saturationBoost <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    if (centre <= 0f)
                    {
                        continue;
                    }

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.001)
                            {
                                continue;
                            }

                            float influence = Mathf.Clamp01(centre * saturationBoost * (1.0f - (float)distance / (radius + 0.001f)));
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void ClampVariance(float[,] field, float clamp)
        {
            clamp = Mathf.Clamp(clamp, 0.0f, 2.0f);
            if (clamp <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = Mathf.Clamp01(centre * (1.0f - clamp * 0.5f) + interior * (clamp * 0.5f));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void NormalizeEdgeBands(float[,] field, int radius, float interiorBlend, float clampRange)
        {
            if (radius <= 0 || interiorBlend <= 0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    float falloff = 1f - Mathf.Clamp01(edgeDistance / (float)(radius + 1));
                    float blend = interiorBlend * falloff;
                    float interior = SampleInterior(field, x, z);
                    float candidate = field[x, z] * (1f - blend) + interior * blend;
                    if (clampRange > 0f)
                    {
                        candidate = Mathf.Clamp(candidate, field[x, z] - clampRange, field[x, z] + clampRange);
                    }

                    buffer[x, z] = Mathf.Clamp01(candidate);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static float SampleVariance(float[,] field, int x, int z, int radius = 1)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            float sum = 0f;
            float sumSq = 0f;
            int count = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = Mathf.Clamp(x + dx, 0, sizeX - 1);
                    int nz = Mathf.Clamp(z + dz, 0, sizeZ - 1);
                    float value = field[nx, nz];
                    sum += value;
                    sumSq += value * value;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0f;
            }

            float mean = sum / count;
            return Mathf.Max(0f, sumSq / count - mean * mean);
        }

        private static void BlendInterior(float[,] field, float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (blend <= 0.0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = field[x, z] * (1f - blend) + interior * blend;
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        private static void ApplyGradientStability(float[,] field, int iterations, float blend, float gradientClamp)
        {
            iterations = Mathf.Max(0, iterations);
            blend = Mathf.Clamp01(blend);
            gradientClamp = Mathf.Max(0.0001f, gradientClamp);
            if (iterations == 0 || blend <= 0f)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float centre = field[x, z];
                        float interior = SampleInterior(field, x, z);
                        float gradient = Mathf.Abs(centre - interior);
                        float weight = Mathf.Clamp01(gradient / gradientClamp) * blend;
                        if (weight <= 0f)
                        {
                            buffer[x, z] = centre;
                            continue;
                        }

                        float stabilised = centre * (1f - weight) + interior * weight;
                        float clampMax = Mathf.Max(Mathf.Max(centre, interior) + gradientClamp * 0.5f, 1f);
                        buffer[x, z] = Mathf.Clamp(stabilised, 0f, clampMax);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private void ApplyEdgeSeal(bool[,,] mask, float[,] hydrologyMask, float[,] riverMask, double strength)
        {
            strength = Math.Clamp(strength, 0.0, 1.0);
            if (strength <= 0)
            {
                return;
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (x != 0 && z != 0 && x != chunkSize - 1 && z != chunkSize - 1)
                    {
                        continue;
                    }

                    for (int y = 1; y < worldHeight - 1; y++)
                    {
                        float hydro = Mathf.Clamp01(hydrologyMask[x, z]);
                        float river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0f;
                        float neighbourHydro = SampleInterior(hydrologyMask, x, z);
                        float gradient = Mathf.Abs(neighbourHydro - hydro);
                        double sealingBias = 0.5 + hydro * 0.35 + river * 0.25 + gradient * 0.25;
                        double sealChance = strength * Math.Clamp(sealingBias, 0.0, 1.5);
                        if (mask[x, y, z] && random.NextDouble() < sealChance)
                        {
                            mask[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private void ApplyRiparianPlugs(bool[,,] mask, float[,] hydrologyMask, float[,] riverMask, int plugDepth)
        {
            if (plugDepth <= 0)
            {
                return;
            }

            int plugTop = Math.Min(worldHeight - 2, Math.Max(2, seaLevel));
            int plugBottom = Math.Max(1, plugTop - plugDepth);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float hydrology = Mathf.Clamp01(hydrologyMask[x, z]);
                    float river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0f;
                    float wetness = Mathf.Max(hydrology, river);
                    if (wetness < 0.35f)
                    {
                        continue;
                    }

                    for (int y = plugBottom; y <= plugTop; y++)
                    {
                        mask[x, y, z] = false;
                    }
                }
            }
        }

        private void AddSupportColumns(bool[,,] mask, float[,] hydrologyMask, float[,] riverMask)
        {
            double chance = Math.Clamp(worldConfig.Caves.SupportPillarChance * worldConfig.Caves.SupportDensity, 0.0, 1.0);
            if (chance <= 0.0)
            {
                return;
            }

            for (int x = 1; x < chunkSize - 1; x++)
            {
                for (int z = 1; z < chunkSize - 1; z++)
                {
                    float hydrology = Mathf.Clamp01(hydrologyMask[x, z]);
                    float river = riverMask != null ? Mathf.Clamp01(riverMask[x, z]) : 0f;
                    double pillarChance = chance * (1.0 + hydrology * worldConfig.Caves.SupportHydrationBias + river * worldConfig.Caves.SupportFlowBias);
                    if (random.NextDouble() > pillarChance)
                    {
                        continue;
                    }

                    int baseY = Math.Max(1, seaLevel - 6);
                    int height = random.Next(2, 6);
                    for (int y = baseY; y < Math.Min(worldHeight - 1, baseY + height); y++)
                    {
                        mask[x, y, z] = false;
                    }
                }
            }
        }

        private double ComputeColumnStability(int surface, float hydrology, float riverPressure, float flowPressure, double edgeFactor)
        {
            double waterBias = 1.0 - Math.Clamp(hydrology * worldConfig.Caves.HydrologyStabilityWeight, 0.0, 0.75);
            double riverBias = 1.0 - Math.Clamp(riverPressure * worldConfig.Caves.RiverSuppressionWeight, 0.0, 0.9);
            double flowBias = 1.0 - Math.Clamp(flowPressure * worldConfig.Caves.FlowStabilityWeight, 0.0, 0.85);
            double ceilingBias = 1.0 - Math.Clamp((surface / 128.0) * worldConfig.Caves.CeilingStabilityWeight, 0.0, 0.35);
            double edgeBias = 1.0 - Math.Clamp(edgeFactor * worldConfig.Caves.EdgeSealStrength, 0.0, 0.45);
            return Math.Clamp(waterBias * riverBias * flowBias * (1.0 - ceilingBias * 0.35) * edgeBias, 0.05, 1.25);
        }

        private double ComputeEdgeFalloff(int x, int z)
        {
            int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
            int maxRadius = Math.Max(1, chunkSize / 2);
            return 1.0 - Math.Clamp(edgeDistance / (double)maxRadius, 0.0, 1.0);
        }

        private static float ComputeSlope(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int east = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int north = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            double dx = center - east;
            double dz = center - north;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static float SampleCurvature(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int forward = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            int back = heightMap[x, Math.Max(0, z - 1)];
            double laplacian = (left + right + forward + back - 4 * center) / 4.0;
            return (float)laplacian;
        }

        private static Vector2Int ComputeDownhillVector(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int bestDrop = 0;
            int bestX = 0;
            int bestZ = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                    {
                        continue;
                    }

                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestX = dx;
                        bestZ = dz;
                    }
                }
            }

            return new Vector2Int(bestX, bestZ);
        }

        private static float SampleInterior(float[,] field, int x, int z)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            int cx = Math.Clamp(x, 1, sizeX - 2);
            int cz = Math.Clamp(z, 1, sizeZ - 2);
            float sum = 0f;
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = Math.Clamp(cx + dx, 1, sizeX - 2);
                    int nz = Math.Clamp(cz + dz, 1, sizeZ - 2);
                    sum += field[nx, nz];
                    count++;
                }
            }

            return count == 0 ? field[cx, cz] : sum / count;
        }

        private static void CopyField(int[,] source, int[,] target)
        {
            int sizeX = source.GetLength(0);
            int sizeZ = source.GetLength(1);
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    target[x, z] = source[x, z];
                }
            }
        }

        private static void CopyField(float[,] source, float[,] target)
        {
            int sizeX = source.GetLength(0);
            int sizeZ = source.GetLength(1);
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    target[x, z] = source[x, z];
                }
            }
        }

        private static void CopyField(bool[,,] source, bool[,,] target)
        {
            int sizeX = source.GetLength(0);
            int sizeY = source.GetLength(1);
            int sizeZ = source.GetLength(2);
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        target[x, y, z] = source[x, y, z];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Preview-friendly chunk container (height/cave/river/lake masks).
    /// </summary>
    public sealed class ChunkData
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public int Size { get; }
        public int WorldHeight { get; }
        public int WaterLevel { get; }

        public int[,] HeightMap { get; }
        public bool[,,] CaveMask { get; }
        public float[,] RiverMask { get; }
        public float[,] LakeMask { get; }
        public float[,] HydrologyMask { get; }
        public float[,] FlowMask { get; }

        public ChunkData(int size, int chunkX, int chunkZ, int worldHeight, int waterLevel)
        {
            Size = size;
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            WorldHeight = worldHeight;
            WaterLevel = waterLevel;

            HeightMap = new int[size, size];
            CaveMask = new bool[size, worldHeight, size];
            RiverMask = new float[size, size];
            LakeMask = new float[size, size];
            HydrologyMask = new float[size, size];
            FlowMask = new float[size, size];
        }
    }
}
