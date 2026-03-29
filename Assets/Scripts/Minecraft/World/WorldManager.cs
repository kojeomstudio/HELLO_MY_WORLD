using System.Collections.Generic;
using UnityEngine;
using Networking.Core;
using GameProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Client-side world manager that handles block changes and world modifications
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int worldSize = 1000;
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int worldHeight = 256;
        [SerializeField] private int renderDistance = 8;
        
        private ProtobufNetworkClient _networkClient;
        private Dictionary<Vector3Int, int> _blockChanges = new Dictionary<Vector3Int, int>();
        private Dictionary<string, SubWorld> _subWorlds = new Dictionary<string, SubWorld>();
        
        // Dictionary to store loaded chunks
        private Dictionary<string, Chunk> loadedChunks = new Dictionary<string, Chunk>();
        
        public int WorldSize => worldSize;
        public int ChunkSize => chunkSize;
        public int RenderDistance => renderDistance;
        
        // Event for block changes
        public event System.Action<Vector3Int, int> OnBlockChanged;
        public event System.Action<int, int, int, int, byte> OnBlockChangedLegacy;
        
        private void Awake()
        {
            // Find network client in scene
            _networkClient = FindObjectOfType<ProtobufNetworkClient>();
            
            if (_networkClient != null)
            {
                // Register for block change broadcasts
                _networkClient.BlockChangeBroadcastReceived += OnBlockChangeBroadcast;
            }
            else
            {
                Debug.LogWarning("[WorldManager] ProtobufNetworkClient not found in scene");
            }
        }
        
        private void Start()
        {
            Debug.Log("[WorldManager] Initialized client-side world manager");
        }
        
        /// <summary>
        /// Modify a block in the world
        /// </summary>
        public void ModifyBlock(Vector3Int position, int blockType)
        {
            // Store local change
            _blockChanges[position] = blockType;
            
            // Send change request to server
            if (_networkClient != null)
            {
                _networkClient.SendBlockChangeRequest("main_world", "default", position, blockType, 0);
            }
            
            Debug.Log($"[WorldManager] Modified block at {position} to type {blockType}");
        }
        
        /// <summary>
        /// Modify multiple blocks in a batch
        /// </summary>
        public void ModifyBlocks(Dictionary<Vector3Int, int> blockChanges)
        {
            foreach (var change in blockChanges)
            {
                ModifyBlock(change.Key, change.Value);
            }
        }
        
        /// <summary>
        /// Modify world manager settings
        /// </summary>
        public void ModifyWorldManager(WorldManagerSettings settings)
        {
            if (settings != null)
            {
                worldSize = settings.WorldSize > 0 ? settings.WorldSize : worldSize;
                chunkSize = settings.ChunkSize > 0 ? settings.ChunkSize : chunkSize;
                renderDistance = settings.RenderDistance > 0 ? settings.RenderDistance : renderDistance;
                
                Debug.Log($"[WorldManager] Updated world settings: Size={worldSize}, ChunkSize={chunkSize}, RenderDistance={renderDistance}");
            }
        }
        
        /// <summary>
        /// Modify a specific sub-world
        /// </summary>
        public void ModifySpecificSubWorld(string subWorldId, SubWorldSettings settings)
        {
            if (!_subWorlds.ContainsKey(subWorldId))
            {
                _subWorlds[subWorldId] = new SubWorld
                {
                    Id = subWorldId,
                    Name = settings.Name ?? subWorldId,
                    Description = settings.Description ?? "",
                    IsEnabled = settings.IsEnabled,
                    BlockTypes = new List<int>(settings.BlockTypes ?? new int[0])
                };
            }
            else
            {
                var subWorld = _subWorlds[subWorldId];
                subWorld.Name = settings.Name ?? subWorld.Name;
                subWorld.Description = settings.Description ?? subWorld.Description;
                subWorld.IsEnabled = settings.IsEnabled;
                
                if (settings.BlockTypes != null)
                {
                    subWorld.BlockTypes.Clear();
                    subWorld.BlockTypes.AddRange(settings.BlockTypes);
                }
            }
            
            Debug.Log($"[WorldManager] Modified sub-world {subWorldId}: Name={_subWorlds[subWorldId].Name}, Enabled={_subWorlds[subWorldId].IsEnabled}");
        }
        
        /// <summary>
        /// Get block type at position
        /// </summary>
        public int GetBlockType(Vector3Int position)
        {
            if (_blockChanges.ContainsKey(position))
            {
                return _blockChanges[position];
            }
            
            // Default to air (0) if no change recorded
            return 0;
        }
        
        /// <summary>
        /// Get sub-world by ID
        /// </summary>
        public SubWorld GetSubWorld(string subWorldId)
        {
            return _subWorlds.ContainsKey(subWorldId) ? _subWorlds[subWorldId] : null;
        }
        
        /// <summary>
        /// Get all sub-worlds
        /// </summary>
        public Dictionary<string, SubWorld> GetAllSubWorlds()
        {
            return new Dictionary<string, SubWorld>(_subWorlds);
        }
        
        /// <summary>
        /// Handle block change broadcast from server
        /// </summary>
        private void OnBlockChangeBroadcast(Game.World.WorldBlockChangeBroadcast broadcast)
        {
            var position = new Vector3Int(
                (int)broadcast.BlockPosition.X,
                (int)broadcast.BlockPosition.Y,
                (int)broadcast.BlockPosition.Z
            );
            
            // Update local block changes
            _blockChanges[position] = broadcast.BlockType;
            
            // Notify listeners
            OnBlockChanged?.Invoke(position, broadcast.BlockType);
            
            Debug.Log($"[WorldManager] Received block change: {position} -> {broadcast.BlockType}");
        }
        
        /// <summary>
        /// Sets a block at the specified coordinates.
        /// This is called by NetworkManager when receiving block change broadcasts.
        /// </summary>
        /// <param name="chunkX">Chunk X coordinate</param>
        /// <param name="chunkZ">Chunk Z coordinate</param>
        /// <param name="blockX">Block X coordinate within chunk (0-15)</param>
        /// <param name="blockY">Block Y coordinate (0-255)</param>
        /// <param name="blockZ">Block Z coordinate within chunk (0-15)</param>
        /// <param name="blockType">Block type ID</param>
        public void SetBlock(int chunkX, int chunkZ, int blockX, int blockY, int blockZ, byte blockType)
        {
            try
            {
                // Validate coordinates
                if (blockX < 0 || blockX >= chunkSize || blockZ < 0 || blockZ >= chunkSize || 
                    blockY < 0 || blockY >= worldHeight)
                {
                    Debug.LogWarning($"[WorldManager] Invalid block coordinates: ({blockX}, {blockY}, {blockZ})");
                    return;
                }
                
                // Get or create chunk
                string chunkKey = GetChunkKey(chunkX, chunkZ);
                if (!loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
                {
                    chunk = CreateChunk(chunkX, chunkZ);
                    loadedChunks[chunkKey] = chunk;
                }
                
                // Set the block
                chunk.SetBlock(blockX, blockY, blockZ, blockType);
                
                // Convert to world coordinates for the event
                int worldX = chunkX * chunkSize + blockX;
                int worldZ = chunkZ * chunkSize + blockZ;
                
                // Notify listeners of block change
                OnBlockChangedLegacy?.Invoke(worldX, blockY, worldZ, blockType);
                
                Debug.Log($"[WorldManager] Set block at ({worldX}, {blockY}, {worldZ}) to type {blockType}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WorldManager] Failed to set block: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets a block at the specified world coordinates.
        /// </summary>
        public byte GetBlock(int worldX, int worldY, int worldZ)
        {
            try
            {
                // Convert to chunk coordinates
                int chunkX = Mathf.FloorToInt(worldX / (float)chunkSize);
                int chunkZ = Mathf.FloorToInt(worldZ / (float)chunkSize);
                int blockX = worldX - chunkX * chunkSize;
                int blockZ = worldZ - chunkZ * chunkSize;
                
                // Validate coordinates
                if (blockX < 0 || blockX >= chunkSize || blockZ < 0 || blockZ >= chunkSize || 
                    worldY < 0 || worldY >= worldHeight)
                {
                    return 0; // Air block
                }
                
                // Get chunk
                string chunkKey = GetChunkKey(chunkX, chunkZ);
                if (!loadedChunks.TryGetValue(chunkKey, out Chunk chunk))
                {
                    return 0; // Air block if chunk not loaded
                }
                
                return chunk.GetBlock(blockX, worldY, blockZ);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WorldManager] Failed to get block: {ex.Message}");
                return 0; // Air block on error
            }
        }
        
        /// <summary>
        /// Creates a new chunk at the specified coordinates.
        /// </summary>
        private Chunk CreateChunk(int chunkX, int chunkZ)
        {
            var chunk = new Chunk(chunkX, chunkZ, chunkSize, worldHeight);
            Debug.Log($"[WorldManager] Created chunk at ({chunkX}, {chunkZ})");
            return chunk;
        }
        
        /// <summary>
        /// Generates a unique key for a chunk.
        /// </summary>
        private string GetChunkKey(int chunkX, int chunkZ)
        {
            return $"{chunkX}_{chunkZ}";
        }
        
        /// <summary>
        /// Unloads chunks that are outside the render distance.
        /// </summary>
        public void UnloadDistantChunks(Vector3 playerPosition)
        {
            int playerChunkX = Mathf.FloorToInt(playerPosition.x / chunkSize);
            int playerChunkZ = Mathf.FloorToInt(playerPosition.z / chunkSize);
            
            var chunksToUnload = new List<string>();
            
            foreach (var kvp in loadedChunks)
            {
                var chunk = kvp.Value;
                int distance = Mathf.Max(
                    Mathf.Abs(chunk.X - playerChunkX),
                    Mathf.Abs(chunk.Z - playerChunkZ)
                );
                
                if (distance > renderDistance)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkKey in chunksToUnload)
            {
                loadedChunks.Remove(chunkKey);
                Debug.Log($"[WorldManager] Unloaded distant chunk: {chunkKey}");
            }
        }
        
        private void OnDestroy()
        {
            if (_networkClient != null)
            {
                _networkClient.BlockChangeBroadcastReceived -= OnBlockChangeBroadcast;
            }
            
            loadedChunks.Clear();
            Debug.Log("[WorldManager] Cleaned up world manager");
        }
    }
    
    /// <summary>
    /// World manager settings
    /// </summary>
    [System.Serializable]
    public class WorldManagerSettings
    {
        public int WorldSize;
        public int ChunkSize;
        public int RenderDistance;
    }
    
    /// <summary>
    /// Sub-world definition
    /// </summary>
    [System.Serializable]
    public class SubWorld
    {
        public string Id;
        public string Name;
        public string Description;
        public bool IsEnabled;
        public List<int> BlockTypes;
    }
    
    /// <summary>
    /// Sub-world settings
    /// </summary>
    [System.Serializable]
    public class SubWorldSettings
    {
        public string Name;
        public string Description;
        public bool IsEnabled = true;
        public int[] BlockTypes;
    }
    
    /// <summary>
    /// Simple chunk data structure for client-side world management.
    /// </summary>
    public class Chunk
    {
        public int X { get; }
        public int Z { get; }
        public int Size { get; }
        public int Height { get; }
        
        private byte[,,] blocks;
        
        public Chunk(int x, int z, int size, int height)
        {
            X = x;
            Z = z;
            Size = size;
            Height = height;
            
            // Initialize with air blocks (0)
            blocks = new byte[size, height, size];
        }
        
        public void SetBlock(int x, int y, int z, byte blockType)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Height && z >= 0 && z < Size)
            {
                blocks[x, y, z] = blockType;
            }
        }
        
        public byte GetBlock(int x, int y, int z)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Height && z >= 0 && z < Size)
            {
                return blocks[x, y, z];
            }
            return 0; // Air block
        }
    }
}
