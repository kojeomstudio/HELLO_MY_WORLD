using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorld
{
    /// <summary>
    /// Client-side world map controller that synchronizes with server terrain generation
    /// Uses WorldMapControlProfile to ensure consistent terrain rendering between client and server
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("World Map Configuration")]
        [SerializeField] private string worldProfilePath = "StreamingAssets/WorldMapControlProfile.json";
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private int maxConcurrentChunkRequests = 4;
        
        [Header("Rendering Settings")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Transform playerTransform;
        
        // World generation profile
        private WorldMapControlProfile worldProfile;
        private string profileHash;
        
        // Chunk management
        private readonly ConcurrentDictionary<Vector2Int, ChunkRenderer> loadedChunks = new();
        private readonly ConcurrentQueue<ChunkRequest> chunkRequestQueue = new();
        private readonly SemaphoreSlim chunkRequestSemaphore;
        
        // Terrain generation
        private EnhancedTerrainGenerator terrainGenerator;
        private CancellationTokenSource cancellationTokenSource;
        
        // Events
        public event Action<Vector2Int> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;
        public event Action<WorldMapControlProfile> OnProfileUpdated;
        
        private void Awake()
        {
            chunkRequestSemaphore = new SemaphoreSlim(maxConcurrentChunkRequests, maxConcurrentChunkRequests);
            cancellationTokenSource = new CancellationTokenSource();
            
            // Load world profile
            LoadWorldProfile();
            
            // Initialize terrain generator
            InitializeTerrainGenerator();
        }
        
        private void Start()
        {
            // Start chunk processing
            _ = ProcessChunkRequestsAsync(cancellationTokenSource.Token);
            
            // Load initial chunks around player
            if (playerTransform != null)
            {
                LoadChunksAroundPlayer();
            }
        }
        
        private void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            chunkRequestSemaphore?.Dispose();
            
            // Clean up loaded chunks
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    Destroy(chunk.gameObject);
                }
            }
            loadedChunks.Clear();
        }
        
        private void Update()
        {
            // Check if player moved to a new chunk
            if (playerTransform != null)
            {
                var playerChunkPos = WorldToChunkPosition(playerTransform.position);
                if (!loadedChunks.ContainsKey(playerChunkPos))
                {
                    LoadChunksAroundPlayer();
                }
            }
        }
        
        /// <summary>
        /// Loads the world map control profile from StreamingAssets
        /// </summary>
        private void LoadWorldProfile()
        {
            try
            {
                var profilePath = Path.Combine(Application.streamingAssetsPath, "WorldMapControlProfile.json");
                
                if (File.Exists(profilePath))
                {
                    var json = File.ReadAllText(profilePath);
                    worldProfile = JsonUtility.FromJson<WorldMapControlProfile>(json);
                    profileHash = ComputeProfileHash(worldProfile);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Loaded world profile: {worldProfile.SourceConfig} (Hash: {profileHash})");
                    }
                    
                    OnProfileUpdated?.Invoke(worldProfile);
                }
                else
                {
                    Debug.LogWarning($"[WorldMapController] World profile not found at {profilePath}, using defaults");
                    CreateDefaultProfile();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapController] Failed to load world profile: {ex.Message}");
                CreateDefaultProfile();
            }
        }
        
        /// <summary>
        /// Creates a default world profile when none is available
        /// </summary>
        private void CreateDefaultProfile()
        {
            worldProfile = new WorldMapControlProfile
            {
                Version = 1,
                ChunkSize = 16,
                RenderDistance = 8,
                SimulationDistance = 6,
                GlobalWaterLevel = 62,
                EnableRivers = true,
                EnableLakes = true,
                EnableCaves = true,
                UseImprovedRivers = true,
                UseImprovedLakes = true,
                UseImprovedCaves = true
            };
            
            profileHash = ComputeProfileHash(worldProfile);
            OnProfileUpdated?.Invoke(worldProfile);
        }
        
        /// <summary>
        /// Initializes the terrain generator with the current world profile
        /// </summary>
        private void InitializeTerrainGenerator()
        {
            if (worldProfile == null)
            {
                Debug.LogError("[WorldMapController] Cannot initialize terrain generator without world profile");
                return;
            }
            
            terrainGenerator = new EnhancedTerrainGenerator(worldProfile);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapController] Initialized terrain generator with profile: {worldProfile.SourceConfig}");
            }
        }
        
        /// <summary>
        /// Loads chunks around the player's current position
        /// </summary>
        private void LoadChunksAroundPlayer()
        {
            if (playerTransform == null || worldProfile == null) return;
            
            var playerChunkPos = WorldToChunkPosition(playerTransform.position);
            var renderDistance = worldProfile.RenderDistance;
            
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    var chunkPos = new Vector2Int(playerChunkPos.x + x, playerChunkPos.y + z);
                    var distance = Mathf.Sqrt(x * x + z * z);
                    
                    // Only load chunks within render distance
                    if (distance <= renderDistance && !loadedChunks.ContainsKey(chunkPos))
                    {
                        RequestChunk(chunkPos);
                    }
                }
            }
            
            // Unload distant chunks
            UnloadDistantChunks(playerChunkPos, renderDistance + 2);
        }
        
        /// <summary>
        /// Requests a chunk to be loaded
        /// </summary>
        private void RequestChunk(Vector2Int chunkPosition)
        {
            var request = new ChunkRequest
            {
                Position = chunkPosition,
                Priority = ComputeChunkPriority(chunkPosition),
                RequestTime = DateTime.UtcNow
            };
            
            chunkRequestQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Computes chunk priority based on distance to player
        /// </summary>
        private float ComputeChunkPriority(Vector2Int chunkPosition)
        {
            if (playerTransform == null) return 0f;
            
            var playerChunkPos = WorldToChunkPosition(playerTransform.position);
            var distance = Vector2Int.Distance(chunkPosition, playerChunkPos);
            
            // Higher priority for closer chunks
            return 1f / (1f + distance);
        }
        
        /// <summary>
        /// Processes chunk requests asynchronously
        /// </summary>
        private async Task ProcessChunkRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (chunkRequestQueue.TryDequeue(out var request))
                    {
                        await chunkRequestSemaphore.WaitAsync(cancellationToken);
                        
                        try
                        {
                            _ = LoadChunkAsync(request);
                        }
                        finally
                        {
                            chunkRequestSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldMapController] Error processing chunk request: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Loads a chunk asynchronously
        /// </summary>
        private async Task LoadChunkAsync(ChunkRequest request)
        {
            try
            {
                if (worldProfile == null || terrainGenerator == null)
                {
                    Debug.LogError("[WorldMapController] Cannot load chunk without world profile or terrain generator");
                    return;
                }
                
                var chunkData = await terrainGenerator.GenerateChunkAsync(request.Position.x, request.Position.y);
                
                if (chunkData != null)
                {
                    // Create chunk renderer on main thread
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        CreateChunkRenderer(request.Position, chunkData);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapController] Failed to load chunk {request.Position}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a chunk renderer for the generated chunk data
        /// </summary>
        private void CreateChunkRenderer(Vector2Int position, ChunkData chunkData)
        {
            if (loadedChunks.ContainsKey(position))
            {
                // Chunk already loaded, skip
                return;
            }
            
            var chunkObject = new GameObject($"Chunk_{position.x}_{position.y}");
            chunkObject.transform.SetParent(transform);
            
            var chunkRenderer = chunkObject.AddComponent<ChunkRenderer>();
            chunkRenderer.Initialize(chunkData, terrainMaterial, waterMaterial);
            
            // Position chunk in world
            var worldPos = ChunkToWorldPosition(position);
            chunkObject.transform.position = worldPos;
            
            loadedChunks[position] = chunkRenderer;
            OnChunkLoaded?.Invoke(position);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapController] Loaded chunk at {position}");
            }
        }
        
        /// <summary>
        /// Unloads chunks that are too far from the player
        /// </summary>
        private void UnloadDistantChunks(Vector2Int playerChunkPos, int maxDistance)
        {
            var chunksToUnload = new List<Vector2Int>();
            
            foreach (var kvp in loadedChunks)
            {
                var distance = Vector2Int.Distance(kvp.Key, playerChunkPos);
                if (distance > maxDistance)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkPos in chunksToUnload)
            {
                if (loadedChunks.TryRemove(chunkPos, out var chunkRenderer))
                {
                    if (chunkRenderer != null)
                    {
                        Destroy(chunkRenderer.gameObject);
                    }
                    
                    OnChunkUnloaded?.Invoke(chunkPos);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Unloaded distant chunk at {chunkPos}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Converts world position to chunk position
        /// </summary>
        private Vector2Int WorldToChunkPosition(Vector3 worldPosition)
        {
            if (worldProfile == null) return Vector2Int.zero;
            
            var chunkSize = worldProfile.ChunkSize;
            var x = Mathf.FloorToInt(worldPosition.x / chunkSize);
            var z = Mathf.FloorToInt(worldPosition.z / chunkSize);
            
            return new Vector2Int(x, z);
        }
        
        /// <summary>
        /// Converts chunk position to world position
        /// </summary>
        private Vector3 ChunkToWorldPosition(Vector2Int chunkPosition)
        {
            if (worldProfile == null) return Vector3.zero;
            
            var chunkSize = worldProfile.ChunkSize;
            return new Vector3(chunkPosition.x * chunkSize, 0f, chunkPosition.y * chunkSize);
        }
        
        /// <summary>
        /// Computes hash of world profile for validation
        /// </summary>
        private string ComputeProfileHash(WorldMapControlProfile profile)
        {
            if (profile == null) return string.Empty;
            
            var profileString = $"{profile.Version}|{profile.ChunkSize}|{profile.RenderDistance}|{profile.SimulationDistance}|{profile.GlobalWaterLevel}|{profile.EnableRivers}|{profile.EnableLakes}|{profile.EnableCaves}|{profile.UseImprovedRivers}|{profile.UseImprovedLakes}|{profile.UseImprovedCaves}";
            
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(profileString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        
        /// <summary>
        /// Reloads the world profile from disk
        /// </summary>
        public void ReloadProfile()
        {
            LoadWorldProfile();
            InitializeTerrainGenerator();
            
            // Reload all chunks with new profile
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    Destroy(chunk.gameObject);
                }
            }
            loadedChunks.Clear();
            
            if (playerTransform != null)
            {
                LoadChunksAroundPlayer();
            }
        }
        
        /// <summary>
        /// Gets the current world profile
        /// </summary>
        public WorldMapControlProfile GetWorldProfile()
        {
            return worldProfile;
        }
        
        /// <summary>
        /// Gets the profile hash for validation
        /// </summary>
        public string GetProfileHash()
        {
            return profileHash;
        }
        
        /// <summary>
        /// Checks if the profile matches the expected hash
        /// </summary>
        public bool ValidateProfile(string expectedHash)
        {
            return !string.IsNullOrEmpty(profileHash) && profileHash == expectedHash;
        }
    }
    
    /// <summary>
    /// Represents a chunk load request
    /// </summary>
    internal struct ChunkRequest
    {
        public Vector2Int Position;
        public float Priority;
        public DateTime RequestTime;
    }
    
    /// <summary>
    /// Enhanced terrain generator that uses WorldMapControlProfile
    /// </summary>
    public class EnhancedTerrainGenerator
    {
        private readonly WorldMapControlProfile profile;
        
        public EnhancedTerrainGenerator(WorldMapControlProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }
        
        /// <summary>
        /// Generates chunk data for the specified chunk coordinates
        /// </summary>
        public async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            await Task.Yield(); // Ensure async operation
            
            var chunkData = new ChunkData
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Size = profile.ChunkSize
            };
            
            // Generate terrain heightmap
            GenerateTerrainHeightmap(chunkData);
            
            // Generate caves if enabled
            if (profile.EnableCaves)
            {
                GenerateCaves(chunkData);
            }
            
            // Generate rivers if enabled
            if (profile.EnableRivers)
            {
                GenerateRivers(chunkData);
            }
            
            // Generate lakes if enabled
            if (profile.EnableLakes)
            {
                GenerateLakes(chunkData);
            }
            
            return chunkData;
        }
        
        private void GenerateTerrainHeightmap(ChunkData chunkData)
        {
            // Simple heightmap generation using Perlin noise
            var size = chunkData.Size;
            chunkData.HeightMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // Multi-octave Perlin noise
                    var height = 0f;
                    var amplitude = 1f;
                    var frequency = 0.01f;
                    
                    for (int octave = 0; octave < 4; octave++)
                    {
                        height += Mathf.PerlinNoise(worldX * frequency, worldZ * frequency) * amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }
                    
                    // Normalize and scale height
                    height = Mathf.Clamp01(height);
                    height *= 64f; // Max height variation
                    
                    chunkData.HeightMap[x, z] = height;
                }
            }
        }
        
        private void GenerateCaves(ChunkData chunkData)
        {
            // Simple cave generation using 3D Perlin noise
            var size = chunkData.Size;
            var caveMap = new bool[size, size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        var worldX = chunkData.ChunkX * size + x;
                        var worldY = y;
                        var worldZ = chunkData.ChunkZ * size + z;
                        
                        // 3D noise for cave generation
                        var noise = Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f) + 
                                   Mathf.PerlinNoise(worldX * 0.1f, worldY * 0.1f) + 
                                   Mathf.PerlinNoise(worldY * 0.1f, worldZ * 0.1f);
                        
                        caveMap[x, y, z] = noise > 1.5f && worldY < profile.GlobalWaterLevel + 20;
                    }
                }
            }
            
            chunkData.CaveMap = caveMap;
        }
        
        private void GenerateRivers(ChunkData chunkData)
        {
            // Simple river generation using 2D Perlin noise
            var size = chunkData.Size;
            var riverMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // River noise
                    var riverNoise = Mathf.PerlinNoise(worldX * 0.02f, worldZ * 0.02f);
                    riverMap[x, z] = riverNoise > 0.7f ? 1f : 0f;
                }
            }
            
            chunkData.RiverMap = riverMap;
        }
        
        private void GenerateLakes(ChunkData chunkData)
        {
            // Simple lake generation
            var size = chunkData.Size;
            var lakeMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // Lake noise
                    var lakeNoise = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f);
                    lakeMap[x, z] = lakeNoise > 0.8f ? 1f : 0f;
                }
            }
            
            chunkData.LakeMap = lakeMap;
        }
    }
    
    /// <summary>
    /// Chunk data structure
    /// </summary>
    public class ChunkData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public int Size { get; set; }
        public float[,] HeightMap { get; set; }
        public bool[,,] CaveMap { get; set; }
        public float[,] RiverMap { get; set; }
        public float[,] LakeMap { get; set; }
    }
}using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorld
{
    /// <summary>
    /// Client-side world map controller that synchronizes with server terrain generation
    /// Uses WorldMapControlProfile to ensure consistent terrain rendering between client and server
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("World Map Configuration")]
        [SerializeField] private string worldProfilePath = "StreamingAssets/WorldMapControlProfile.json";
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private int maxConcurrentChunkRequests = 4;
        
        [Header("Rendering Settings")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Transform playerTransform;
        
        // World generation profile
        private WorldMapControlProfile worldProfile;
        private string profileHash;
        
        // Chunk management
        private readonly ConcurrentDictionary<Vector2Int, ChunkRenderer> loadedChunks = new();
        private readonly ConcurrentQueue<ChunkRequest> chunkRequestQueue = new();
        private readonly SemaphoreSlim chunkRequestSemaphore;
        
        // Terrain generation
        private EnhancedTerrainGenerator terrainGenerator;
        private CancellationTokenSource cancellationTokenSource;
        
        // Events
        public event Action<Vector2Int> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;
        public event Action<WorldMapControlProfile> OnProfileUpdated;
        
        private void Awake()
        {
            chunkRequestSemaphore = new SemaphoreSlim(maxConcurrentChunkRequests, maxConcurrentChunkRequests);
            cancellationTokenSource = new CancellationTokenSource();
            
            // Load world profile
            LoadWorldProfile();
            
            // Initialize terrain generator
            InitializeTerrainGenerator();
        }
        
        private void Start()
        {
            // Start chunk processing
            _ = ProcessChunkRequestsAsync(cancellationTokenSource.Token);
            
            // Load initial chunks around player
            if (playerTransform != null)
            {
                LoadChunksAroundPlayer();
            }
        }
        
        private void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            chunkRequestSemaphore?.Dispose();
            
            // Clean up loaded chunks
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    Destroy(chunk.gameObject);
                }
            }
            loadedChunks.Clear();
        }
        
        private void Update()
        {
            // Check if player moved to a new chunk
            if (playerTransform != null)
            {
                var playerChunkPos = WorldToChunkPosition(playerTransform.position);
                if (!loadedChunks.ContainsKey(playerChunkPos))
                {
                    LoadChunksAroundPlayer();
                }
            }
        }
        
        /// <summary>
        /// Loads the world map control profile from StreamingAssets
        /// </summary>
        private void LoadWorldProfile()
        {
            try
            {
                var profilePath = Path.Combine(Application.streamingAssetsPath, "WorldMapControlProfile.json");
                
                if (File.Exists(profilePath))
                {
                    var json = File.ReadAllText(profilePath);
                    worldProfile = JsonUtility.FromJson<WorldMapControlProfile>(json);
                    profileHash = ComputeProfileHash(worldProfile);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Loaded world profile: {worldProfile.SourceConfig} (Hash: {profileHash})");
                    }
                    
                    OnProfileUpdated?.Invoke(worldProfile);
                }
                else
                {
                    Debug.LogWarning($"[WorldMapController] World profile not found at {profilePath}, using defaults");
                    CreateDefaultProfile();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapController] Failed to load world profile: {ex.Message}");
                CreateDefaultProfile();
            }
        }
        
        /// <summary>
        /// Creates a default world profile when none is available
        /// </summary>
        private void CreateDefaultProfile()
        {
            worldProfile = new WorldMapControlProfile
            {
                Version = 1,
                ChunkSize = 16,
                RenderDistance = 8,
                SimulationDistance = 6,
                GlobalWaterLevel = 62,
                EnableRivers = true,
                EnableLakes = true,
                EnableCaves = true,
                UseImprovedRivers = true,
                UseImprovedLakes = true,
                UseImprovedCaves = true
            };
            
            profileHash = ComputeProfileHash(worldProfile);
            OnProfileUpdated?.Invoke(worldProfile);
        }
        
        /// <summary>
        /// Initializes the terrain generator with the current world profile
        /// </summary>
        private void InitializeTerrainGenerator()
        {
            if (worldProfile == null)
            {
                Debug.LogError("[WorldMapController] Cannot initialize terrain generator without world profile");
                return;
            }
            
            terrainGenerator = new EnhancedTerrainGenerator(worldProfile);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapController] Initialized terrain generator with profile: {worldProfile.SourceConfig}");
            }
        }
        
        /// <summary>
        /// Loads chunks around the player's current position
        /// </summary>
        private void LoadChunksAroundPlayer()
        {
            if (playerTransform == null || worldProfile == null) return;
            
            var playerChunkPos = WorldToChunkPosition(playerTransform.position);
            var renderDistance = worldProfile.RenderDistance;
            
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    var chunkPos = new Vector2Int(playerChunkPos.x + x, playerChunkPos.y + z);
                    var distance = Mathf.Sqrt(x * x + z * z);
                    
                    // Only load chunks within render distance
                    if (distance <= renderDistance && !loadedChunks.ContainsKey(chunkPos))
                    {
                        RequestChunk(chunkPos);
                    }
                }
            }
            
            // Unload distant chunks
            UnloadDistantChunks(playerChunkPos, renderDistance + 2);
        }
        
        /// <summary>
        /// Requests a chunk to be loaded
        /// </summary>
        private void RequestChunk(Vector2Int chunkPosition)
        {
            var request = new ChunkRequest
            {
                Position = chunkPosition,
                Priority = ComputeChunkPriority(chunkPosition),
                RequestTime = DateTime.UtcNow
            };
            
            chunkRequestQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Computes chunk priority based on distance to player
        /// </summary>
        private float ComputeChunkPriority(Vector2Int chunkPosition)
        {
            if (playerTransform == null) return 0f;
            
            var playerChunkPos = WorldToChunkPosition(playerTransform.position);
            var distance = Vector2Int.Distance(chunkPosition, playerChunkPos);
            
            // Higher priority for closer chunks
            return 1f / (1f + distance);
        }
        
        /// <summary>
        /// Processes chunk requests asynchronously
        /// </summary>
        private async Task ProcessChunkRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (chunkRequestQueue.TryDequeue(out var request))
                    {
                        await chunkRequestSemaphore.WaitAsync(cancellationToken);
                        
                        try
                        {
                            _ = LoadChunkAsync(request);
                        }
                        finally
                        {
                            chunkRequestSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldMapController] Error processing chunk request: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Loads a chunk asynchronously
        /// </summary>
        private async Task LoadChunkAsync(ChunkRequest request)
        {
            try
            {
                if (worldProfile == null || terrainGenerator == null)
                {
                    Debug.LogError("[WorldMapController] Cannot load chunk without world profile or terrain generator");
                    return;
                }
                
                var chunkData = await terrainGenerator.GenerateChunkAsync(request.Position.x, request.Position.y);
                
                if (chunkData != null)
                {
                    // Create chunk renderer on main thread
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        CreateChunkRenderer(request.Position, chunkData);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapController] Failed to load chunk {request.Position}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a chunk renderer for the generated chunk data
        /// </summary>
        private void CreateChunkRenderer(Vector2Int position, ChunkData chunkData)
        {
            if (loadedChunks.ContainsKey(position))
            {
                // Chunk already loaded, skip
                return;
            }
            
            var chunkObject = new GameObject($"Chunk_{position.x}_{position.y}");
            chunkObject.transform.SetParent(transform);
            
            var chunkRenderer = chunkObject.AddComponent<ChunkRenderer>();
            chunkRenderer.Initialize(chunkData, terrainMaterial, waterMaterial);
            
            // Position chunk in world
            var worldPos = ChunkToWorldPosition(position);
            chunkObject.transform.position = worldPos;
            
            loadedChunks[position] = chunkRenderer;
            OnChunkLoaded?.Invoke(position);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapController] Loaded chunk at {position}");
            }
        }
        
        /// <summary>
        /// Unloads chunks that are too far from the player
        /// </summary>
        private void UnloadDistantChunks(Vector2Int playerChunkPos, int maxDistance)
        {
            var chunksToUnload = new List<Vector2Int>();
            
            foreach (var kvp in loadedChunks)
            {
                var distance = Vector2Int.Distance(kvp.Key, playerChunkPos);
                if (distance > maxDistance)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkPos in chunksToUnload)
            {
                if (loadedChunks.TryRemove(chunkPos, out var chunkRenderer))
                {
                    if (chunkRenderer != null)
                    {
                        Destroy(chunkRenderer.gameObject);
                    }
                    
                    OnChunkUnloaded?.Invoke(chunkPos);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WorldMapController] Unloaded distant chunk at {chunkPos}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Converts world position to chunk position
        /// </summary>
        private Vector2Int WorldToChunkPosition(Vector3 worldPosition)
        {
            if (worldProfile == null) return Vector2Int.zero;
            
            var chunkSize = worldProfile.ChunkSize;
            var x = Mathf.FloorToInt(worldPosition.x / chunkSize);
            var z = Mathf.FloorToInt(worldPosition.z / chunkSize);
            
            return new Vector2Int(x, z);
        }
        
        /// <summary>
        /// Converts chunk position to world position
        /// </summary>
        private Vector3 ChunkToWorldPosition(Vector2Int chunkPosition)
        {
            if (worldProfile == null) return Vector3.zero;
            
            var chunkSize = worldProfile.ChunkSize;
            return new Vector3(chunkPosition.x * chunkSize, 0f, chunkPosition.y * chunkSize);
        }
        
        /// <summary>
        /// Computes hash of world profile for validation
        /// </summary>
        private string ComputeProfileHash(WorldMapControlProfile profile)
        {
            if (profile == null) return string.Empty;
            
            var profileString = $"{profile.Version}|{profile.ChunkSize}|{profile.RenderDistance}|{profile.SimulationDistance}|{profile.GlobalWaterLevel}|{profile.EnableRivers}|{profile.EnableLakes}|{profile.EnableCaves}|{profile.UseImprovedRivers}|{profile.UseImprovedLakes}|{profile.UseImprovedCaves}";
            
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(profileString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        
        /// <summary>
        /// Reloads the world profile from disk
        /// </summary>
        public void ReloadProfile()
        {
            LoadWorldProfile();
            InitializeTerrainGenerator();
            
            // Reload all chunks with new profile
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    Destroy(chunk.gameObject);
                }
            }
            loadedChunks.Clear();
            
            if (playerTransform != null)
            {
                LoadChunksAroundPlayer();
            }
        }
        
        /// <summary>
        /// Gets the current world profile
        /// </summary>
        public WorldMapControlProfile GetWorldProfile()
        {
            return worldProfile;
        }
        
        /// <summary>
        /// Gets the profile hash for validation
        /// </summary>
        public string GetProfileHash()
        {
            return profileHash;
        }
        
        /// <summary>
        /// Checks if the profile matches the expected hash
        /// </summary>
        public bool ValidateProfile(string expectedHash)
        {
            return !string.IsNullOrEmpty(profileHash) && profileHash == expectedHash;
        }
    }
    
    /// <summary>
    /// Represents a chunk load request
    /// </summary>
    internal struct ChunkRequest
    {
        public Vector2Int Position;
        public float Priority;
        public DateTime RequestTime;
    }
    
    /// <summary>
    /// Enhanced terrain generator that uses WorldMapControlProfile
    /// </summary>
    public class EnhancedTerrainGenerator
    {
        private readonly WorldMapControlProfile profile;
        
        public EnhancedTerrainGenerator(WorldMapControlProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }
        
        /// <summary>
        /// Generates chunk data for the specified chunk coordinates
        /// </summary>
        public async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            await Task.Yield(); // Ensure async operation
            
            var chunkData = new ChunkData
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Size = profile.ChunkSize
            };
            
            // Generate terrain heightmap
            GenerateTerrainHeightmap(chunkData);
            
            // Generate caves if enabled
            if (profile.EnableCaves)
            {
                GenerateCaves(chunkData);
            }
            
            // Generate rivers if enabled
            if (profile.EnableRivers)
            {
                GenerateRivers(chunkData);
            }
            
            // Generate lakes if enabled
            if (profile.EnableLakes)
            {
                GenerateLakes(chunkData);
            }
            
            return chunkData;
        }
        
        private void GenerateTerrainHeightmap(ChunkData chunkData)
        {
            // Simple heightmap generation using Perlin noise
            var size = chunkData.Size;
            chunkData.HeightMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // Multi-octave Perlin noise
                    var height = 0f;
                    var amplitude = 1f;
                    var frequency = 0.01f;
                    
                    for (int octave = 0; octave < 4; octave++)
                    {
                        height += Mathf.PerlinNoise(worldX * frequency, worldZ * frequency) * amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }
                    
                    // Normalize and scale height
                    height = Mathf.Clamp01(height);
                    height *= 64f; // Max height variation
                    
                    chunkData.HeightMap[x, z] = height;
                }
            }
        }
        
        private void GenerateCaves(ChunkData chunkData)
        {
            // Simple cave generation using 3D Perlin noise
            var size = chunkData.Size;
            var caveMap = new bool[size, size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        var worldX = chunkData.ChunkX * size + x;
                        var worldY = y;
                        var worldZ = chunkData.ChunkZ * size + z;
                        
                        // 3D noise for cave generation
                        var noise = Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f) + 
                                   Mathf.PerlinNoise(worldX * 0.1f, worldY * 0.1f) + 
                                   Mathf.PerlinNoise(worldY * 0.1f, worldZ * 0.1f);
                        
                        caveMap[x, y, z] = noise > 1.5f && worldY < profile.GlobalWaterLevel + 20;
                    }
                }
            }
            
            chunkData.CaveMap = caveMap;
        }
        
        private void GenerateRivers(ChunkData chunkData)
        {
            // Simple river generation using 2D Perlin noise
            var size = chunkData.Size;
            var riverMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // River noise
                    var riverNoise = Mathf.PerlinNoise(worldX * 0.02f, worldZ * 0.02f);
                    riverMap[x, z] = riverNoise > 0.7f ? 1f : 0f;
                }
            }
            
            chunkData.RiverMap = riverMap;
        }
        
        private void GenerateLakes(ChunkData chunkData)
        {
            // Simple lake generation
            var size = chunkData.Size;
            var lakeMap = new float[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    
                    // Lake noise
                    var lakeNoise = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f);
                    lakeMap[x, z] = lakeNoise > 0.8f ? 1f : 0f;
                }
            }
            
            chunkData.LakeMap = lakeMap;
        }
    }
    
    /// <summary>
    /// Chunk data structure
    /// </summary>
    public class ChunkData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public int Size { get; set; }
        public float[,] HeightMap { get; set; }
        public bool[,,] CaveMap { get; set; }
        public float[,] RiverMap { get; set; }
        public float[,] LakeMap { get; set; }
    }
}
