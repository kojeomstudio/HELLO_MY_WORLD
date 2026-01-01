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

        private readonly ConcurrentDictionary<Vector2Int, ChunkData> loadedChunks = new();
        private readonly ConcurrentQueue<Vector2Int> requestQueue = new();

        private void Awake()
        {
            LoadProfile();
            generator = new EnhancedTerrainGenerator(profile);
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

            lastProfileCheckUtc = now;
            var profilePath = Path.Combine(Application.streamingAssetsPath, profileFileName);

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
                    generator = new EnhancedTerrainGenerator(profile);
                    loadedChunks.Clear();
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Reloaded profile hash={profile.ProfileHash} (updated {writeTime:o})");
                    }
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

        private static Vector2Int WorldToChunk(Vector3 position)
        {
            const int chunkSize = 16;
            int cx = Mathf.FloorToInt(position.x / chunkSize);
            int cz = Mathf.FloorToInt(position.z / chunkSize);
            return new Vector2Int(cx, cz);
        }
    }

    /// <summary>
    /// Lightweight terrain generator for Unity previews. Mirrors the server hydrology/cave/lake rules.
    /// </summary>
    public sealed class EnhancedTerrainGenerator
    {
        private readonly WorldMapControlProfile profile;
        private readonly System.Random random;

        public EnhancedTerrainGenerator(WorldMapControlProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            random = new System.Random(profile.ProfileHash.GetHashCode());
        }

        public Task<ChunkData> GenerateChunkAsync(Vector2Int chunkPos, CancellationToken token)
        {
            return Task.Run(() => GenerateChunk(chunkPos), token);
        }

        private ChunkData GenerateChunk(Vector2Int chunkPos)
        {
            var chunk = new ChunkData(profile.ChunkSize, chunkPos.x, chunkPos.y, profile.GlobalWaterLevel);
            BuildHeightMap(chunk);
            BuildCaves(chunk);
            BuildRivers(chunk);
            BuildLakes(chunk);
            return chunk;
        }

        private void BuildHeightMap(ChunkData chunk)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                for (int z = 0; z < chunk.Size; z++)
                {
                    int worldX = chunk.ChunkX * chunk.Size + x;
                    int worldZ = chunk.ChunkZ * chunk.Size + z;

                    float continental = Mathf.PerlinNoise(worldX * 0.003f, worldZ * 0.003f);
                    float macro = Mathf.PerlinNoise(worldX * 0.007f, worldZ * 0.007f);
                    float detail = Mathf.PerlinNoise(worldX * 0.017f, worldZ * 0.017f);

                    float blended = continental * 0.6f + macro * 0.3f + detail * 0.1f;
                    int height = Mathf.Clamp(Mathf.RoundToInt(profile.GlobalWaterLevel + blended * 32f), 4, 250);
                    chunk.HeightMap[x, z] = height;
                }
            }
        }

        private void BuildCaves(ChunkData chunk)
        {
            if (!profile.EnableCaves)
            {
                return;
            }

            for (int x = 0; x < chunk.Size; x++)
            {
                for (int z = 0; z < chunk.Size; z++)
                {
                    int columnHeight = chunk.HeightMap[x, z];
                    for (int y = 4; y < columnHeight; y++)
                    {
                        float warpX = Mathf.PerlinNoise((chunk.ChunkX * chunk.Size + x) * profile.HydrologyWarpFrequency, y * 0.03f) * profile.HydrologyWarpAmplitude;
                        float warpZ = Mathf.PerlinNoise((chunk.ChunkZ * chunk.Size + z) * profile.HydrologyWarpFrequency, y * 0.03f + 37f) * profile.HydrologyWarpAmplitude;

                        float noise = Mathf.PerlinNoise(
                            (chunk.ChunkX * chunk.Size + x + warpX) * 0.035f,
                            (chunk.ChunkZ * chunk.Size + z + warpZ + y * 0.05f));

                        float threshold = 0.55f - profile.CaveDepthWeight * 0.25f;
                        if (noise > threshold)
                        {
                            chunk.CaveMask[x, y, z] = true;
                        }
                    }
                }
            }
        }

        private void BuildRivers(ChunkData chunk)
        {
            if (!profile.EnableRivers)
            {
                return;
            }

            for (int x = 0; x < chunk.Size; x++)
            {
                for (int z = 0; z < chunk.Size; z++)
                {
                    int worldX = chunk.ChunkX * chunk.Size + x;
                    int worldZ = chunk.ChunkZ * chunk.Size + z;

                    float warp = Mathf.PerlinNoise(worldX * profile.HydrologyWarpFrequency, worldZ * profile.HydrologyWarpFrequency);
                    float riverNoise = Mathf.PerlinNoise(worldX * profile.RiverNoiseScale + warp, worldZ * profile.RiverNoiseScale + warp * 0.5f);
                    float intensity = 1f - Mathf.Abs(riverNoise - 0.5f) * 2f;

                    if (intensity > profile.RiverCenterThreshold)
                    {
                        chunk.RiverMask[x, z] = intensity;
                        int depth = Mathf.Clamp(Mathf.CeilToInt(profile.RiverDepth * intensity), 2, profile.RiverDepth + 2);
                        chunk.HeightMap[x, z] = Mathf.Max(chunk.HeightMap[x, z] - depth, profile.GlobalWaterLevel - depth);
                    }
                }
            }

            Smooth2D(chunk.RiverMask, profile.RiverIntensitySmoothIterations, profile.RiverIntensitySmoothBlend);
        }

        private void BuildLakes(ChunkData chunk)
        {
            if (!profile.EnableLakes)
            {
                return;
            }

            for (int x = 0; x < chunk.Size; x++)
            {
                for (int z = 0; z < chunk.Size; z++)
                {
                    float riverIntensity = chunk.RiverMask[x, z];
                    if (riverIntensity > profile.LakeRiverProximitySuppression)
                    {
                        continue;
                    }

                    int worldX = chunk.ChunkX * chunk.Size + x;
                    int worldZ = chunk.ChunkZ * chunk.Size + z;
                    float noise = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) + profile.LakeSpawnWeightBias;

                    if (noise > profile.LakeWetlandSaturationThreshold)
                    {
                        chunk.LakeMask[x, z] = noise;
                        int depth = Mathf.Clamp(Mathf.CeilToInt(profile.LakeShelfDepth + noise * profile.LakeOutflowCarveDepth), 1, profile.LakeOutflowCarveDepth + 2);
                        chunk.HeightMap[x, z] = Mathf.Max(chunk.HeightMap[x, z] - depth, profile.GlobalWaterLevel - depth);
                    }
                }
            }

            Smooth2D(chunk.LakeMask, profile.LakeBasinSmoothIterations, profile.HydrologySmoothBlend);
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
    }

    /// <summary>
    /// Preview-friendly chunk container (height/cave/river/lake masks).
    /// </summary>
    public sealed class ChunkData
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public int Size { get; }
        public int WaterLevel { get; }

        public int[,] HeightMap { get; }
        public bool[,,] CaveMask { get; }
        public float[,] RiverMask { get; }
        public float[,] LakeMask { get; }

        public ChunkData(int size, int chunkX, int chunkZ, int waterLevel)
        {
            Size = size;
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            WaterLevel = waterLevel;

            HeightMap = new int[size, size];
            CaveMask = new bool[size, 256, size];
            RiverMask = new float[size, size];
            LakeMask = new float[size, size];
        }
    }
}
