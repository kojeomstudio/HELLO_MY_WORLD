using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minecraft.Core;
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

        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentQueue<Vector2Int> requestQueue = new();

        private void Awake()
        {
            LoadProfile();
            configPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
            lastConfigWriteUtc = File.Exists(configPath) ? File.GetLastWriteTimeUtc(configPath) : DateTime.MinValue;
            worldConfig = WorldConfig.Instance;
            generator = new EnhancedTerrainGenerator(profile, worldConfig);
            lastProfileCheckUtc = DateTime.UtcNow;
            cancellation = new CancellationTokenSource();
            buildSemaphore = new SemaphoreSlim(Math.Max(1, maxConcurrentChunkBuilds));
            _ = ProcessQueueAsync(cancellation.Token);
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            buildSemaphore?.Dispose();
            loadedChunks.Clear();
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
            lastProfileSignature = ComputeGenerationSignature(profile, worldConfig ?? WorldConfig.Instance);

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
                    lastProfileSignature = ComputeGenerationSignature(profile, worldConfig);
                    generator = new EnhancedTerrainGenerator(profile, worldConfig);
                    loadedChunks.Clear();
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
                if (writeTime <= lastProfileWriteUtc)
                {
                    return;
                }

                var newProfile = WorldMapControlProfile.LoadFromFile(profilePath, WorldConfig.Instance);
                if (!string.Equals(newProfile.ProfileHash, profile.ProfileHash, StringComparison.OrdinalIgnoreCase))
                {
                    profile = newProfile;
                    generator = new EnhancedTerrainGenerator(profile, worldConfig);
                    loadedChunks.Clear();
                    lastProfileHash = profile.ProfileHash;
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
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] Regenerated map preview generator for signature={generationSignature}");
                }
            }
        }

        private void EnqueueAroundPlayer()
        {
            var playerChunk = WorldToChunk(playerTransform.position);
            for (int dx = -viewRadiusChunks; dx <= viewRadiusChunks; dx++)
            {
                for (int dz = -viewRadiusChunks; dz <= viewRadiusChunks; dz++)
                {
                    var pos = new Vector2Int(playerChunk.x + dx, playerChunk.y + dz);
                    if (loadedChunks.ContainsKey(pos))
                    {
                        continue;
                    }

                    requestQueue.Enqueue(pos);
                }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!requestQueue.TryDequeue(out var pos))
                {
                    await Task.Delay(10, token);
                    continue;
                }

                if (loadedChunks.ContainsKey(pos))
                {
                    continue;
                }

                await buildSemaphore.WaitAsync(token);
                try
                {
                    var chunk = await generator.GenerateChunkAsync(pos, token);
                    loadedChunks[pos] = chunk;
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
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapController] Unloaded preview chunk {pos}");
                }
            }
        }

        private Vector2Int WorldToChunk(Vector3 position)
        {
            int size = profile != null ? Math.Max(1, profile.ChunkSize) : 16;
            int cx = Mathf.FloorToInt(position.x / size);
            int cz = Mathf.FloorToInt(position.z / size);
            return new Vector2Int(cx, cz);
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
            ApplyHydrologyContinuityEnvelope(heightMap, hydrology, flow);
            NormalizeHydrologyFlowEdges(hydrology, flow);
            HarmonizeHydrologyWithSurface(heightMap, hydrology, flow);

            var riverMask = profile.EnableRivers ? BuildRiverMask(chunkPos, heightMap, hydrology, flow) : new float[chunkSize, chunkSize];
            var lakeMask = profile.EnableLakes ? BuildLakeMask(chunkPos, heightMap, hydrology, flow, riverMask) : new float[chunkSize, chunkSize];
            var caveMask = profile.EnableCaves ? BuildCaveMask(chunkPos, heightMap, hydrology, flow, riverMask) : new bool[chunkSize, worldHeight, chunkSize];

            ApplyHydrologyToHeight(heightMap, riverMask, lakeMask, hydrology, flow);

            CopyField(heightMap, chunk.HeightMap);
            CopyField(hydrology, chunk.HydrologyMask);
            CopyField(flow, chunk.FlowMask);
            CopyField(riverMask, chunk.RiverMask);
            CopyField(lakeMask, chunk.LakeMask);
            CopyField(caveMask, chunk.CaveMask);
            return chunk;
        }

        private string ComputeGenerationSignature(WorldMapControlProfile controlProfile, WorldConfig config)
        {
            return $"{config.WorldName}:{config.Seed}:{config.MapControlProfileVersion}:{controlProfile.ProfileHash}:{controlProfile.Version}:{controlProfile.ChunkSize}:{config.WorldHeight}:{config.RenderDistance}:{config.SimulationDistance}:{controlProfile.GlobalWaterLevel}:{config.Terrain.SeaLevel}:{config.Water.HydrologyFlowPersistence}:{config.Water.HydrologyWatershedStitchWeight}:{config.Water.HydrologyWatershedStitchRadius}:{config.Water.HydrologyGradientStabilityIterations}:{config.Water.HydrologyGradientStabilityBlend}:{config.Water.HydrologyGradientClamp}:{config.Lakes.FlowSeepageWeight}:{config.Caves.CeilingMoistureWeight}:{config.Caves.CeilingMoistureClamp}:{config.Water.HydrologyEdgeBlendRadius}:{config.Water.HydrologyEdgeVarianceClamp}:{config.Water.HydrologyEdgeNormalizationBlend}:{config.Water.HydrologyEdgeNormalizationIterations}:{config.Water.HydrologyFlowMemoryWeight}:{config.Water.RiverMeanderJitter}:{config.Lakes.VarianceWeight}:{config.Lakes.OutflowStabilityWeight}:{config.Water.HydrologyFlowShadowWeight}:{config.Water.HydrologyFlowShadowSlopeWeight}:{config.Lakes.WetlandBufferRadius}:{config.Water.LakeInflowBlendWeight}:{config.Water.HydrologyVarianceBlend}:{config.Water.HydrologyVarianceClamp}:{config.Water.HydrologyEdgeStabilityWeight}:{config.Water.HydrologyEdgeFlowLockWeight}:{config.Water.HydrologyEdgeFlowBias}:{config.Water.HydrologyDirectionalBlend}:{config.Water.HydrologyDirectionalIterations}:{config.Water.HydrologyFlowDivergenceClamp}:{config.Water.HydrologySeamRelaxBlend}:{config.Water.HydrologySeamRelaxIterations}:{config.Caves.EdgeSealStrength}:{config.Caves.SupportDensity}:{config.Caves.SupportPillarChance}:{config.Lakes.RiverProximitySuppression}";
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

        private bool[,,] BuildCaveMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flowMask, float[,] riverMask)
        {
            var mask = new bool[chunkSize, worldHeight, chunkSize];
            double horizontal = Math.Max(0.0001, worldConfig.Caves.HorizontalFrequency);
            double vertical = Math.Max(0.0001, worldConfig.Caves.VerticalFrequency);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int surface = heightMap[x, z];
                    float hydrologySample = hydrology[x, z];
                    float flowSample = flowMask[x, z];
                    float riverPressure = riverMask != null ? riverMask[x, z] : 0f;
                    double flowMemory = Math.Clamp((flowSample + SampleInterior(flowMask, x, z)) * 0.5, 0.0, 1.0);
                    double wetnessRetention = hydrologySample * worldConfig.Caves.MoistureRetentionWeight + flowMemory * worldConfig.Caves.MoistureRetentionWeight * 0.35;
                    double edgeFactor = ComputeEdgeFalloff(x, z);
                    double hydrologyGradient = Math.Abs(SampleInterior(hydrology, x, z) - hydrologySample);
                    double flowGradient = Math.Abs(SampleInterior(flowMask, x, z) - flowSample);
                    double seamStability = 1.0 - Math.Clamp(hydrologyGradient * worldConfig.Caves.EdgeSealStrength, 0.0, 0.45);
                    double continuityClamp = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * worldConfig.Caves.EdgeSealStrength * 0.2, 0.0, 0.45);
                    double ceilingClamp = Math.Clamp(
                        hydrologySample * worldConfig.Caves.CeilingMoistureWeight +
                        flowMemory * worldConfig.Caves.CeilingMoistureWeight * 0.5 +
                        hydrologyGradient * worldConfig.Caves.CeilingMoistureWeight * 0.35,
                        0.0,
                        1.0);
                    double stability = ComputeColumnStability(surface, hydrologySample, riverPressure, flowSample, edgeFactor) * seamStability * continuityClamp;
                    stability *= 1.0 - ceilingClamp * 0.15;

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
                        threshold += ceilingClamp * 0.1;
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
            return mask;
        }

        private float[,] BuildRiverMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            var mask = new float[chunkSize, chunkSize];
            double noiseScale = Math.Max(0.0001, profile.RiverNoiseScale);
            double confluenceBoost = Math.Clamp(profile.RiverConfluenceBoost, 0.0, 2.0);

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
                    double seamStitch = 1.0 + Math.Clamp((SampleInterior(hydrology, x, z) - hydrologySample) * profile.HydrologyEdgeFluxBlend, -0.35, 0.35);
                    double hydrologyGradient = Math.Abs(SampleInterior(hydrology, x, z) - hydrologySample);

                    double riverMask = profile.RiverBankThreshold - baseNoise;
                    double pressure = Math.Max(0.0, riverMask);
                    pressure *= 1.0 + hydrologySample * profile.HydrologyContinuityWeight;
                    pressure *= 1.0 + flowSample * profile.RiverFlowAlignmentWeight;
                    pressure *= 1.0 + directionality * profile.RiverAnisotropyWeight * 0.2;
                    pressure *= 1.0 - Math.Clamp(gradient * profile.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * profile.RiverReliefPenaltyWeight, 0.0, 0.35);
                    pressure *= flowAlignment * seamStitch;
                    pressure *= 1.0 + (flowMemory + hydrologySample) * profile.HydrologyFlowPersistence * 0.2;
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

                    double headwater = 1.0 - Math.Clamp(flowSample * profile.RiverHeadwaterStabilityWeight, 0.0, 0.65);
                    pressure *= 1.0 + headwater * 0.1;
                    double deltaBlend = 1.0 - Math.Clamp(Math.Abs(heightMap[x, z] - seaLevel) / Math.Max(1.0, profile.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    pressure *= 1.0 + deltaBlend * profile.RiverDeltaWetlandStrength * 0.5;
                    pressure = ApplyEdgeBlend(pressure, hydrology[x, z], x, z);

                    mask[x, z] = Mathf.Clamp((float)pressure, 0f, 1.35f);
                }
            }

            Smooth2D(mask, profile.RiverIntensitySmoothIterations, profile.RiverIntensitySmoothBlend);
            DirectionalSmooth(heightMap, mask, Math.Max(1, profile.HydrologyDirectionalIterations), profile.HydrologyDirectionalBlend * 0.35f);
            StabilizeEdges(mask, profile.HydrologyEdgeBlendRadius, 1, profile.RiverEdgeFeather, profile.RiverSeamFillStrength);
            RelaxEdges(mask, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(mask, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            return mask;
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

        private float[,] BuildLakeMask(Vector2Int chunkPos, int[,] heightMap, float[,] hydrology, float[,] flow, float[,] riverMask)
        {
            var lakes = new float[chunkSize, chunkSize];
            double flowSeepageWeight = Math.Clamp(worldConfig.Lakes.FlowSeepageWeight, 0.0, 1.0);

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
                    double slope = ComputeSlope(heightMap, x, z);
                    double reliefPenalty = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);
                    int edgeDistance = Math.Min(Math.Min(x, chunkSize - 1 - x), Math.Min(z, chunkSize - 1 - z));
                    double radiusFalloff = Math.Clamp(edgeDistance / (double)Math.Max(1, profile.LakeMaxRadius), 0.0, 1.0);
                    double inflowBlend = riverPressure * profile.LakeInflowBlendWeight;
                    double hydrologyGradient = Math.Abs(seamHydro - hydrologySample);
                    double flowGradient = Math.Abs(SampleInterior(flow, x, z) - flowSample);
                    double flowShadow = Math.Clamp(
                        flowSample * worldConfig.Water.HydrologyFlowShadowWeight +
                        hydrologyGradient * worldConfig.Water.HydrologyFlowShadowSlopeWeight * 0.5,
                        0.0,
                        0.7);

                    double wetness = hydrologySample * 0.65 + flowSample * 0.35;
                    double weight = (basinNoise * 0.45) + (rimNoise * 0.25) + wetness * 0.4 + profile.LakeSpawnWeightBias;
                    weight += inflowBlend * 0.35 * (1.0 - flowShadow * 0.5);
                    double seamContinuity = 1.0 + (seamHydro + flowMemory) * flowSeepageWeight * 0.2;
                    double seepage = (flowSample + hydrologyGradient + flowMemory * 0.5) * flowSeepageWeight;
                    weight += seepage * (1.0 - flowShadow * 0.5);
                    double varianceAssist = Math.Clamp((hydrologyGradient + flowGradient) * profile.HydrologyVarianceBlend * 0.1, -0.25, 0.35);
                    weight -= slope * profile.LakeRimErosionWeight * 0.05;
                    weight -= hydrologyGradient * profile.HydrologyEdgeStabilityWeight * 0.25;
                    weight -= riverPressure * 0.5;
                    weight -= reliefPenalty * profile.RiverReliefPenaltyWeight;
                    weight *= 0.75 + radiusFalloff * 0.25;
                    double seamCushion = 1.0 + Math.Clamp((seamHydro - hydrologySample) * profile.HydrologyEdgeFluxBlend, -0.2, 0.3);
                    weight *= seamCushion * seamContinuity;
                    weight *= 1.0 + varianceAssist;

                    double seamRelax = Math.Clamp(profile.HydrologySeamRelaxBlend, 0.0, 1.0);
                    double wetlandThreshold = profile.LakeWetlandSaturationThreshold - hydrologySample * 0.05 - seamRelax * 0.05;
                    if (weight > wetlandThreshold && heightMap[x, z] > seaLevel - worldConfig.Lakes.MaxDepth)
                    {
                        lakes[x, z] = Mathf.Clamp01((float)weight);
                    }
                }
            }

            Smooth2D(lakes, profile.LakeBasinSmoothIterations, profile.HydrologySmoothBlend);
            StitchEdges(lakes, profile.HydrologySeamRelaxBlend * 0.65f);
            FillBasins(lakes, Mathf.Max(0.05f, profile.HydrologyEdgeStabilityWeight * 0.35f), Math.Max(1, profile.HydrologySeamRelaxIterations));
            RelaxEdges(lakes, profile.HydrologyEdgeNormalizationIterations, profile.HydrologyEdgeNormalizationBlend);
            RelaxEdges(lakes, profile.HydrologySeamRelaxIterations, profile.HydrologySeamRelaxBlend);
            ApplyRiparianBuffer(lakes, Math.Min(profile.LakeWetlandBufferRadius, profile.LakeMaxRadius), profile.LakeShorelineBlend);
            ApplyOutflowChannels(lakes, heightMap, flow, profile.LakeInflowBlendWeight, profile.LakeOutflowCarveDepth);
            return lakes;
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

        private void ApplyFlowMemory(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            float memoryWeight = Mathf.Clamp01(profile.HydrologyFlowPersistence * 0.35f + profile.HydrologyFlowMemoryWeight);
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

                    float continuity = 1f + hydro * profile.HydrologyContinuityWeight + neighbourHydro * 0.25f;
                    float memory = flowValue * (1f - memoryWeight);
                    memory += (downhillFlow + flowValue) * (memoryWeight * 0.25f);
                    memory += neighbourFlow * (memoryWeight * 0.35f);
                    memory += hydro * memoryWeight * 0.25f;
                    memory *= continuity;
                    memory *= 1f - Mathf.Clamp(hydrologyGradient * flowShadowWeight * 0.25f, 0f, 0.3f);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    float edgeFalloff = 1f - Mathf.Clamp(edgeDistance / (float)(watershedRadius + 1), 0f, 1f);
                    float edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0f)
                    {
                        float seamAnchor = neighbourHydro * 0.35f + hydro * 0.35f + neighbourFlow * 0.3f;
                        memory = memory * (1f - edgeRepair * 0.55f) + seamAnchor * edgeRepair;
                    }

                    buffer[x, z] = Mathf.Clamp(memory, 0f, Mathf.Max(flowValue + 1.5f, profile.HydrologyFlowDivergenceClamp * 12f));
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
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
