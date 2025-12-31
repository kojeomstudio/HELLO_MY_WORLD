using System;
using System.Collections.Generic;
using UnityEngine;
using GameProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced client-side world controller with improved terrain handling
    /// </summary>
    public class EnhancedClientWorldController : MonoBehaviour
    {
        [Header("World Configuration")]
        [SerializeField] private int viewDistance = 8;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int worldHeight = 256;
        [SerializeField] private int seaLevel = 62;
        
        [Header("Terrain Settings")]
        [SerializeField] private bool enableCaves = true;
        [SerializeField] private bool enableRivers = true;
        [SerializeField] private bool enableLakes = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxChunksPerFrame = 2;
        [SerializeField] private float chunkUpdateInterval = 0.1f;
        
        // Dictionary to store chunk data
        private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
        private Dictionary<Vector2Int, GameObject> chunkObjects = new Dictionary<Vector2Int, GameObject>();
        
        // Player tracking
        private Vector3 lastPlayerPosition;
        private Vector2Int lastPlayerChunk;
        
        // Update queue for chunk processing
        private Queue<Vector2Int> chunkUpdateQueue = new Queue<Vector2Int>();
        private float lastChunkUpdateTime;
        
        // Network client reference
        private MinecraftNetworkClient networkClient;
        
        // Terrain generators
        private ImprovedTerrainGenerator terrainGenerator;
        
        // Events
        public event Action<Vector2Int> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;
        
        private void Start()
        {
            // Initialize terrain generator
            terrainGenerator = new ImprovedTerrainGenerator();
            
            // Find network client
            networkClient = FindObjectOfType<MinecraftNetworkClient>();
            if (networkClient != null)
            {
                networkClient.OnChunkDataReceived += HandleChunkDataReceived;
            }
            
            // Initialize player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                lastPlayerPosition = player.transform.position;
                lastPlayerChunk = WorldToChunkCoords(lastPlayerPosition);
            }
            
            // Start update loop
            InvokeRepeating(nameof(ProcessChunkUpdates), 0.0f, chunkUpdateInterval);
        }
        
        private void Update()
        {
            // Check if player moved to a new chunk
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 currentPos = player.transform.position;
                Vector2Int currentChunk = WorldToChunkCoords(currentPos);
                
                if (currentChunk != lastPlayerChunk)
                {
                    OnPlayerMovedToNewChunk(currentChunk);
                    lastPlayerChunk = currentChunk;
                }
                
                lastPlayerPosition = currentPos;
            }
        }
        
        /// <summary>
        /// Handle player moving to a new chunk
        /// </summary>
        private void OnPlayerMovedToNewChunk(Vector2Int newChunk)
        {
            // Queue chunks to load around new position
            for (int x = newChunk.x - viewDistance; x <= newChunk.x + viewDistance; x++)
            {
                for (int z = newChunk.z - viewDistance; z <= newChunk.z + viewDistance; z++)
                {
                    Vector2Int chunkPos = new Vector2Int(x, z);
                    if (!chunks.ContainsKey(chunkPos))
                    {
                        chunkUpdateQueue.Enqueue(chunkPos);
                    }
                }
            }
            
            // Queue chunks to unload that are too far away
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var chunkPos in chunks.Keys)
            {
                float distance = Vector2.Distance(chunkPos, newChunk);
                if (distance > viewDistance + 2)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }
            
            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
        }
        
        /// <summary>
        /// Process chunk updates from queue
        /// </summary>
        private void ProcessChunkUpdates()
        {
            int processed = 0;
            while (chunkUpdateQueue.Count > 0 && processed < maxChunksPerFrame)
            {
                Vector2Int chunkPos = chunkUpdateQueue.Dequeue();
                if (!chunks.ContainsKey(chunkPos))
                {
                    RequestChunkData(chunkPos);
                    processed++;
                }
            }
        }
        
        /// <summary>
        /// Request chunk data from server
        /// </summary>
        private void RequestChunkData(Vector2Int chunkPos)
        {
            if (networkClient != null)
            {
                // Send chunk request to server
                var request = new ChunkRequestMessage
                {
                    ChunkX = chunkPos.x,
                    ChunkZ = chunkPos.y
                };
                
                networkClient.SendMessage(request);
            }
        }
        
        /// <summary>
        /// Handle received chunk data from server
        /// </summary>
        private void HandleChunkDataReceived(ChunkDataMessage chunkData)
        {
            Vector2Int chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            
            // Create chunk data object
            var chunk = new ChunkData
            {
                X = chunkData.ChunkX,
                Z = chunkData.ChunkZ,
                Blocks = DecompressChunkData(chunkData.CompressedData)
            };
            
            // Store chunk data
            chunks[chunkPos] = chunk;
            
            // Create chunk game object
            CreateChunkGameObject(chunkPos, chunk);
            
            // Notify listeners
            OnChunkLoaded?.Invoke(chunkPos);
        }
        
        /// <summary>
        /// Decompress chunk data from server
        /// </summary>
        private byte[,,] DecompressChunkData(byte[] compressedData)
        {
            // Use the chunk compression utility to decompress
            return ChunkCompression.Decompress(compressedData, chunkSize, worldHeight, chunkSize);
        }
        
        /// <summary>
        /// Create chunk game object with mesh
        /// </summary>
        private void CreateChunkGameObject(Vector2Int chunkPos, ChunkData chunk)
        {
            // Create chunk game object
            GameObject chunkObj = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
            chunkObj.transform.position = new Vector3(chunkPos.x * chunkSize, 0, chunkPos.y * chunkSize);
            
            // Add mesh filter and renderer
            var meshFilter = chunkObj.AddComponent<MeshFilter>();
            var meshRenderer = chunkObj.AddComponent<MeshRenderer>();
            
            // Generate mesh from chunk data
            Mesh chunkMesh = GenerateChunkMesh(chunk);
            meshFilter.mesh = chunkMesh;
            
            // Assign material
            meshRenderer.material = Resources.Load<Material>("Materials/TerrainMaterial");
            
            // Add chunk component
            var chunkComponent = chunkObj.AddComponent<ClientChunk>();
            chunkComponent.Initialize(chunk);
            
            // Store reference
            chunkObjects[chunkPos] = chunkObj;
        }
        
        /// <summary>
        /// Generate mesh from chunk data
        /// </summary>
        private Mesh GenerateChunkMesh(ChunkData chunk)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            // Generate mesh for each block
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        byte blockType = chunk.GetBlock(x, y, z);
                        if (blockType != 0) // 0 = Air
                        {
                            AddBlockMesh(vertices, triangles, uvs, x, y, z, blockType, chunk);
                        }
                    }
                }
            }
            
            // Set mesh data
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Add mesh for a single block
        /// </summary>
        private void AddBlockMesh(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
            int x, int y, int z, byte blockType, ChunkData chunk)
        {
            // Check each face of the block
            Vector3[] faceVertices = new Vector3[4];
            
            // Top face (Y+)
            if (IsTransparentBlock(x, y + 1, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y + 1, z);
                faceVertices[1] = new Vector3(x + 1, y + 1, z);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y + 1, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Top);
            }
            
            // Bottom face (Y-)
            if (IsTransparentBlock(x, y - 1, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y, z + 1);
                faceVertices[3] = new Vector3(x + 1, y, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Bottom);
            }
            
            // Front face (Z+)
            if (IsTransparentBlock(x, y, z + 1, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z + 1);
                faceVertices[1] = new Vector3(x + 1, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y + 1, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Front);
            }
            
            // Back face (Z-)
            if (IsTransparentBlock(x, y, z - 1, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y + 1, z);
                faceVertices[2] = new Vector3(x + 1, y + 1, z);
                faceVertices[3] = new Vector3(x + 1, y, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Back);
            }
            
            // Right face (X+)
            if (IsTransparentBlock(x + 1, y, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x + 1, y, z);
                faceVertices[1] = new Vector3(x + 1, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x + 1, y + 1, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Right);
            }
            
            // Left face (X-)
            if (IsTransparentBlock(x - 1, y, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y + 1, z);
                faceVertices[2] = new Vector3(x, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Left);
            }
        }
        
        /// <summary>
        /// Check if a block is transparent (air, water, etc.)
        /// </summary>
        private bool IsTransparentBlock(int x, int y, int z, ChunkData chunk)
        {
            // Check bounds
            if (x < 0 || x >= chunkSize || y < 0 || y >= worldHeight || z < 0 || z >= chunkSize)
            {
                return true; // Outside chunk is considered transparent
            }
            
            byte blockType = chunk.GetBlock(x, y, z);
            return blockType == 0; // 0 = Air
        }
        
        /// <summary>
        /// Add UV coordinates for a block face
        /// </summary>
        private void AddBlockUVs(List<Vector2> uvs, byte blockType, BlockFace face)
        {
            // Simple UV mapping - can be enhanced with texture atlas
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }
        
        /// <summary>
        /// Unload a chunk
        /// </summary>
        private void UnloadChunk(Vector2Int chunkPos)
        {
            if (chunkObjects.TryGetValue(chunkPos, out GameObject chunkObj))
            {
                Destroy(chunkObj);
                chunkObjects.Remove(chunkPos);
            }
            
            chunks.Remove(chunkPos);
            OnChunkUnloaded?.Invoke(chunkPos);
        }
        
        /// <summary>
        /// Convert world coordinates to chunk coordinates
        /// </summary>
        private Vector2Int WorldToChunkCoords(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / chunkSize),
                Mathf.FloorToInt(worldPos.z / chunkSize)
            );
        }
        
        /// <summary>
        /// Get block at world position
        /// </summary>
        public byte GetBlockAtWorldPosition(Vector3 worldPos)
        {
            Vector2Int chunkPos = WorldToChunkCoords(worldPos);
            if (chunks.TryGetValue(chunkPos, out ChunkData chunk))
            {
                int localX = Mathf.FloorToInt(worldPos.x) - chunkPos.x * chunkSize;
                int localY = Mathf.FloorToInt(worldPos.y);
                int localZ = Mathf.FloorToInt(worldPos.z) - chunkPos.y * chunkSize;
                
                if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < worldHeight && localZ >= 0 && localZ < chunkSize)
                {
                    return chunk.GetBlock(localX, localY, localZ);
                }
            }
            
            return 0; // Air
        }
        
        /// <summary>
        /// Set block at world position
        /// </summary>
        public void SetBlockAtWorldPosition(Vector3 worldPos, byte blockType)
        {
            Vector2Int chunkPos = WorldToChunkCoords(worldPos);
            if (chunks.TryGetValue(chunkPos, out ChunkData chunk))
            {
                int localX = Mathf.FloorToInt(worldPos.x) - chunkPos.x * chunkSize;
                int localY = Mathf.FloorToInt(worldPos.y);
                int localZ = Mathf.FloorToInt(worldPos.z) - chunkPos.y * chunkSize;
                
                if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < worldHeight && localZ >= 0 && localZ < chunkSize)
                {
                    chunk.SetBlock(localX, localY, localZ, blockType);
                    
                    // Update chunk mesh
                    if (chunkObjects.TryGetValue(chunkPos, out GameObject chunkObj))
                    {
                        var meshFilter = chunkObj.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.mesh = GenerateChunkMesh(chunk);
                        }
                    }
                    
                    // Send block change to server
                    SendBlockChangeToServer(chunkPos, localX, localY, localZ, blockType);
                }
            }
        }
        
        /// <summary>
        /// Send block change to server
        /// </summary>
        private void SendBlockChangeToServer(Vector2Int chunkPos, int x, int y, int z, byte blockType)
        {
            if (networkClient != null)
            {
                var blockChange = new BlockChangeMessage
                {
                    ChunkX = chunkPos.x,
                    ChunkZ = chunkPos.y,
                    X = x,
                    Y = y,
                    Z = z,
                    BlockType = blockType
                };
                
                networkClient.SendMessage(blockChange);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            if (networkClient != null)
            {
                networkClient.OnChunkDataReceived -= HandleChunkDataReceived;
            }
            
            // Cancel invoke
            CancelInvoke();
        }
    }
    
    /// <summary>
    /// Client-side chunk component
    /// </summary>
    public class ClientChunk : MonoBehaviour
    {
        private ChunkData chunkData;
        
        public void Initialize(ChunkData data)
        {
            chunkData = data;
        }
        
        public ChunkData GetChunkData()
        {
            return chunkData;
        }
    }
    
    /// <summary>
    /// Block face enum for UV mapping
    /// </summary>
    public enum BlockFace
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }
}using System.Collections.Generic;
using UnityEngine;
using GameProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced client-side world controller with improved terrain handling
    /// </summary>
    public class EnhancedClientWorldController : MonoBehaviour
    {
        [Header("World Configuration")]
        [SerializeField] private int viewDistance = 8;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int worldHeight = 256;
        [SerializeField] private int seaLevel = 62;
        
        [Header("Terrain Settings")]
        [SerializeField] private bool enableCaves = true;
        [SerializeField] private bool enableRivers = true;
        [SerializeField] private bool enableLakes = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxChunksPerFrame = 2;
        [SerializeField] private float chunkUpdateInterval = 0.1f;
        
        // Dictionary to store chunk data
        private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
        private Dictionary<Vector2Int, GameObject> chunkObjects = new Dictionary<Vector2Int, GameObject>();
        
        // Player tracking
        private Vector3 lastPlayerPosition;
        private Vector2Int lastPlayerChunk;
        
        // Update queue for chunk processing
        private Queue<Vector2Int> chunkUpdateQueue = new Queue<Vector2Int>();
        private float lastChunkUpdateTime;
        
        // Network client reference
        private MinecraftNetworkClient networkClient;
        
        // Terrain generators
        private ImprovedTerrainGenerator terrainGenerator;
        
        // Events
        public event Action<Vector2Int> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;
        
        private void Start()
        {
            // Initialize terrain generator
            terrainGenerator = new ImprovedTerrainGenerator();
            
            // Find network client
            networkClient = FindObjectOfType<MinecraftNetworkClient>();
            if (networkClient != null)
            {
                networkClient.OnChunkDataReceived += HandleChunkDataReceived;
            }
            
            // Initialize player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                lastPlayerPosition = player.transform.position;
                lastPlayerChunk = WorldToChunkCoords(lastPlayerPosition);
            }
            
            // Start update loop
            InvokeRepeating(nameof(ProcessChunkUpdates), 0.0f, chunkUpdateInterval);
        }
        
        private void Update()
        {
            // Check if player moved to a new chunk
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 currentPos = player.transform.position;
                Vector2Int currentChunk = WorldToChunkCoords(currentPos);
                
                if (currentChunk != lastPlayerChunk)
                {
                    OnPlayerMovedToNewChunk(currentChunk);
                    lastPlayerChunk = currentChunk;
                }
                
                lastPlayerPosition = currentPos;
            }
        }
        
        /// <summary>
        /// Handle player moving to a new chunk
        /// </summary>
        private void OnPlayerMovedToNewChunk(Vector2Int newChunk)
        {
            // Queue chunks to load around new position
            for (int x = newChunk.x - viewDistance; x <= newChunk.x + viewDistance; x++)
            {
                for (int z = newChunk.z - viewDistance; z <= newChunk.z + viewDistance; z++)
                {
                    Vector2Int chunkPos = new Vector2Int(x, z);
                    if (!chunks.ContainsKey(chunkPos))
                    {
                        chunkUpdateQueue.Enqueue(chunkPos);
                    }
                }
            }
            
            // Queue chunks to unload that are too far away
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var chunkPos in chunks.Keys)
            {
                float distance = Vector2.Distance(chunkPos, newChunk);
                if (distance > viewDistance + 2)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }
            
            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
        }
        
        /// <summary>
        /// Process chunk updates from queue
        /// </summary>
        private void ProcessChunkUpdates()
        {
            int processed = 0;
            while (chunkUpdateQueue.Count > 0 && processed < maxChunksPerFrame)
            {
                Vector2Int chunkPos = chunkUpdateQueue.Dequeue();
                if (!chunks.ContainsKey(chunkPos))
                {
                    RequestChunkData(chunkPos);
                    processed++;
                }
            }
        }
        
        /// <summary>
        /// Request chunk data from server
        /// </summary>
        private void RequestChunkData(Vector2Int chunkPos)
        {
            if (networkClient != null)
            {
                // Send chunk request to server
                var request = new ChunkRequestMessage
                {
                    ChunkX = chunkPos.x,
                    ChunkZ = chunkPos.y
                };
                
                networkClient.SendMessage(request);
            }
        }
        
        /// <summary>
        /// Handle received chunk data from server
        /// </summary>
        private void HandleChunkDataReceived(ChunkDataMessage chunkData)
        {
            Vector2Int chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            
            // Create chunk data object
            var chunk = new ChunkData
            {
                X = chunkData.ChunkX,
                Z = chunkData.ChunkZ,
                Blocks = DecompressChunkData(chunkData.CompressedData)
            };
            
            // Store chunk data
            chunks[chunkPos] = chunk;
            
            // Create chunk game object
            CreateChunkGameObject(chunkPos, chunk);
            
            // Notify listeners
            OnChunkLoaded?.Invoke(chunkPos);
        }
        
        /// <summary>
        /// Decompress chunk data from server
        /// </summary>
        private byte[,,] DecompressChunkData(byte[] compressedData)
        {
            // Use the chunk compression utility to decompress
            return ChunkCompression.Decompress(compressedData, chunkSize, worldHeight, chunkSize);
        }
        
        /// <summary>
        /// Create chunk game object with mesh
        /// </summary>
        private void CreateChunkGameObject(Vector2Int chunkPos, ChunkData chunk)
        {
            // Create chunk game object
            GameObject chunkObj = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
            chunkObj.transform.position = new Vector3(chunkPos.x * chunkSize, 0, chunkPos.y * chunkSize);
            
            // Add mesh filter and renderer
            var meshFilter = chunkObj.AddComponent<MeshFilter>();
            var meshRenderer = chunkObj.AddComponent<MeshRenderer>();
            
            // Generate mesh from chunk data
            Mesh chunkMesh = GenerateChunkMesh(chunk);
            meshFilter.mesh = chunkMesh;
            
            // Assign material
            meshRenderer.material = Resources.Load<Material>("Materials/TerrainMaterial");
            
            // Add chunk component
            var chunkComponent = chunkObj.AddComponent<ClientChunk>();
            chunkComponent.Initialize(chunk);
            
            // Store reference
            chunkObjects[chunkPos] = chunkObj;
        }
        
        /// <summary>
        /// Generate mesh from chunk data
        /// </summary>
        private Mesh GenerateChunkMesh(ChunkData chunk)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            // Generate mesh for each block
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        byte blockType = chunk.GetBlock(x, y, z);
                        if (blockType != 0) // 0 = Air
                        {
                            AddBlockMesh(vertices, triangles, uvs, x, y, z, blockType, chunk);
                        }
                    }
                }
            }
            
            // Set mesh data
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Add mesh for a single block
        /// </summary>
        private void AddBlockMesh(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
            int x, int y, int z, byte blockType, ChunkData chunk)
        {
            // Check each face of the block
            Vector3[] faceVertices = new Vector3[4];
            
            // Top face (Y+)
            if (IsTransparentBlock(x, y + 1, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y + 1, z);
                faceVertices[1] = new Vector3(x + 1, y + 1, z);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y + 1, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Top);
            }
            
            // Bottom face (Y-)
            if (IsTransparentBlock(x, y - 1, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y, z + 1);
                faceVertices[3] = new Vector3(x + 1, y, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Bottom);
            }
            
            // Front face (Z+)
            if (IsTransparentBlock(x, y, z + 1, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z + 1);
                faceVertices[1] = new Vector3(x + 1, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y + 1, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Front);
            }
            
            // Back face (Z-)
            if (IsTransparentBlock(x, y, z - 1, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y + 1, z);
                faceVertices[2] = new Vector3(x + 1, y + 1, z);
                faceVertices[3] = new Vector3(x + 1, y, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Back);
            }
            
            // Right face (X+)
            if (IsTransparentBlock(x + 1, y, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x + 1, y, z);
                faceVertices[1] = new Vector3(x + 1, y, z + 1);
                faceVertices[2] = new Vector3(x + 1, y + 1, z + 1);
                faceVertices[3] = new Vector3(x + 1, y + 1, z);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Right);
            }
            
            // Left face (X-)
            if (IsTransparentBlock(x - 1, y, z, chunk))
            {
                int vertexIndex = vertices.Count;
                
                faceVertices[0] = new Vector3(x, y, z);
                faceVertices[1] = new Vector3(x, y + 1, z);
                faceVertices[2] = new Vector3(x, y + 1, z + 1);
                faceVertices[3] = new Vector3(x, y, z + 1);
                
                vertices.AddRange(faceVertices);
                
                // Add triangles
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
                
                // Add UVs
                AddBlockUVs(uvs, blockType, BlockFace.Left);
            }
        }
        
        /// <summary>
        /// Check if a block is transparent (air, water, etc.)
        /// </summary>
        private bool IsTransparentBlock(int x, int y, int z, ChunkData chunk)
        {
            // Check bounds
            if (x < 0 || x >= chunkSize || y < 0 || y >= worldHeight || z < 0 || z >= chunkSize)
            {
                return true; // Outside chunk is considered transparent
            }
            
            byte blockType = chunk.GetBlock(x, y, z);
            return blockType == 0; // 0 = Air
        }
        
        /// <summary>
        /// Add UV coordinates for a block face
        /// </summary>
        private void AddBlockUVs(List<Vector2> uvs, byte blockType, BlockFace face)
        {
            // Simple UV mapping - can be enhanced with texture atlas
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }
        
        /// <summary>
        /// Unload a chunk
        /// </summary>
        private void UnloadChunk(Vector2Int chunkPos)
        {
            if (chunkObjects.TryGetValue(chunkPos, out GameObject chunkObj))
            {
                Destroy(chunkObj);
                chunkObjects.Remove(chunkPos);
            }
            
            chunks.Remove(chunkPos);
            OnChunkUnloaded?.Invoke(chunkPos);
        }
        
        /// <summary>
        /// Convert world coordinates to chunk coordinates
        /// </summary>
        private Vector2Int WorldToChunkCoords(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / chunkSize),
                Mathf.FloorToInt(worldPos.z / chunkSize)
            );
        }
        
        /// <summary>
        /// Get block at world position
        /// </summary>
        public byte GetBlockAtWorldPosition(Vector3 worldPos)
        {
            Vector2Int chunkPos = WorldToChunkCoords(worldPos);
            if (chunks.TryGetValue(chunkPos, out ChunkData chunk))
            {
                int localX = Mathf.FloorToInt(worldPos.x) - chunkPos.x * chunkSize;
                int localY = Mathf.FloorToInt(worldPos.y);
                int localZ = Mathf.FloorToInt(worldPos.z) - chunkPos.y * chunkSize;
                
                if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < worldHeight && localZ >= 0 && localZ < chunkSize)
                {
                    return chunk.GetBlock(localX, localY, localZ);
                }
            }
            
            return 0; // Air
        }
        
        /// <summary>
        /// Set block at world position
        /// </summary>
        public void SetBlockAtWorldPosition(Vector3 worldPos, byte blockType)
        {
            Vector2Int chunkPos = WorldToChunkCoords(worldPos);
            if (chunks.TryGetValue(chunkPos, out ChunkData chunk))
            {
                int localX = Mathf.FloorToInt(worldPos.x) - chunkPos.x * chunkSize;
                int localY = Mathf.FloorToInt(worldPos.y);
                int localZ = Mathf.FloorToInt(worldPos.z) - chunkPos.y * chunkSize;
                
                if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < worldHeight && localZ >= 0 && localZ < chunkSize)
                {
                    chunk.SetBlock(localX, localY, localZ, blockType);
                    
                    // Update chunk mesh
                    if (chunkObjects.TryGetValue(chunkPos, out GameObject chunkObj))
                    {
                        var meshFilter = chunkObj.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.mesh = GenerateChunkMesh(chunk);
                        }
                    }
                    
                    // Send block change to server
                    SendBlockChangeToServer(chunkPos, localX, localY, localZ, blockType);
                }
            }
        }
        
        /// <summary>
        /// Send block change to server
        /// </summary>
        private void SendBlockChangeToServer(Vector2Int chunkPos, int x, int y, int z, byte blockType)
        {
            if (networkClient != null)
            {
                var blockChange = new BlockChangeMessage
                {
                    ChunkX = chunkPos.x,
                    ChunkZ = chunkPos.y,
                    X = x,
                    Y = y,
                    Z = z,
                    BlockType = blockType
                };
                
                networkClient.SendMessage(blockChange);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            if (networkClient != null)
            {
                networkClient.OnChunkDataReceived -= HandleChunkDataReceived;
            }
            
            // Cancel invoke
            CancelInvoke();
        }
    }
    
    /// <summary>
    /// Client-side chunk component
    /// </summary>
    public class ClientChunk : MonoBehaviour
    {
        private ChunkData chunkData;
        
        public void Initialize(ChunkData data)
        {
            chunkData = data;
        }
        
        public ChunkData GetChunkData()
        {
            return chunkData;
        }
    }
    
    /// <summary>
    /// Block face enum for UV mapping
    /// </summary>
    public enum BlockFace
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }
}
