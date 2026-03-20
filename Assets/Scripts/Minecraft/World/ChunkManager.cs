using System;
using System.Collections.Generic;
using UnityEngine;
using SharedProtocol;
using System.Linq;
using Minecraft.Core;

namespace Minecraft.World
{
    /// <summary>
    /// System that manages chunks in the Minecraft world
    /// Handles chunk loading, unloading, rendering, block management, etc.
    /// Improved with data-driven configuration and enhanced terrain generation
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField] private Material blockMaterial;
        [SerializeField] private GameObject chunkPrefab;
        
        [Header("Performance Settings")]
        [SerializeField] private int chunksPerFrame = 2;
        [SerializeField] private float chunkUpdateInterval = 0.1f;
        
        private readonly Dictionary<Vector2Int, ChunkSnapshot> _chunkData = new();
        private Dictionary<Vector2Int, GameObject> _chunkObjects = new();
        private Dictionary<Vector2Int, ChunkRenderer> _chunkRenderers = new();
        private Core.MinecraftGameClient _gameClient;
        private TerrainGenerator _terrainGenerator;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private BlockDataManager _blockDataManager;
        
        private Vector2Int _playerChunkPos;
        private Queue<Vector2Int> _chunksToLoad = new();
        private Queue<Vector2Int> _chunksToUnload = new();
        private Queue<Vector2Int> _chunksToUpdate = new();
        
        private Dictionary<int, BlockType> _blockTypes = new();
        
        // Configuration properties
        private int ChunkSize => _worldConfig != null ? _worldConfig.ChunkSize : 16;
        private int WorldHeight => _worldConfig != null ? _worldConfig.WorldHeight : 256;
        private int RenderDistance => _clientConfig != null && _clientConfig.Graphics != null ? _clientConfig.Graphics.RenderDistance : 8;
        
        public event Action<Vector2Int> ChunkLoaded;
        public event Action<Vector2Int> ChunkUnloaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        
        public int LoadedChunkCount => _chunkData.Count;
        public Vector2Int PlayerChunkPosition => _playerChunkPos;
        
        private void Start()
        {
            InitializeConfiguration();
            InitializeBlockTypes();
            InitializeComponents();
            InvokeRepeating(nameof(ProcessChunkUpdates), 0f, chunkUpdateInterval);
        }
        
        private void InitializeConfiguration()
        {
            _clientConfig = ClientConfig.Instance;
            _worldConfig = WorldConfig.Instance;
            _blockDataManager = BlockDataManager.Instance;
            
            // Apply configuration to Unity settings
            if (_clientConfig != null)
            {
                _clientConfig.ApplyToUnity();
            }
            if (_worldConfig != null)
            {
                _worldConfig.ApplyToUnity();
            }
        }
        
        private void InitializeComponents()
        {
            _gameClient = FindObjectOfType<Core.MinecraftGameClient>();
            
            // Initialize terrain generator if not already present
            if (_terrainGenerator == null)
            {
                _terrainGenerator = FindObjectOfType<TerrainGenerator>();
                if (_terrainGenerator == null)
                {
                    var terrainGenObj = new GameObject("TerrainGenerator");
                    terrainGenObj.transform.SetParent(transform);
                    _terrainGenerator = terrainGenObj.AddComponent<TerrainGenerator>();
                }
            }
        }
        
        private void Update()
        {
            UpdatePlayerChunkPosition();
            ProcessChunkQueues();
        }
        
        private void InitializeBlockTypes()
        {
            if (_blockDataManager == null) return;
            
            // Load block types from data manager
            var blockDefinitions = _blockDataManager.GetAllBlockDefinitions();
            
            foreach (var blockDef in blockDefinitions)
            {
                _blockTypes[blockDef.Id] = new BlockType(
                    blockDef.Id,
                    blockDef.Name,
                    blockDef.IsSolid,
                    blockDef.IsOpaque
                )
                {
                    Hardness = blockDef.Hardness,
                    TextureName = blockDef.TextureName
                };
            }
            
            Debug.Log($"Initialized {_blockTypes.Count} block types from data manager");
        }
        
        private void UpdatePlayerChunkPosition()
        {
            var playerPos = transform.position;
            var newChunkPos = new Vector2Int(
                Mathf.FloorToInt(playerPos.x / ChunkSize),
                Mathf.FloorToInt(playerPos.z / ChunkSize)
            );
            
            if (newChunkPos != _playerChunkPos)
            {
                _playerChunkPos = newChunkPos;
                UpdateChunkLoadingArea();
            }
        }
        
        private void UpdateChunkLoadingArea()
        {
            var chunksInRange = new HashSet<Vector2Int>();
            var renderDistance = RenderDistance;
            
            for (int x = _playerChunkPos.x - renderDistance; x <= _playerChunkPos.x + renderDistance; x++)
            {
                for (int z = _playerChunkPos.y - renderDistance; z <= _playerChunkPos.y + renderDistance; z++)
                {
                    var chunkPos = new Vector2Int(x, z);
                    var distance = Vector2Int.Distance(_playerChunkPos, chunkPos);
                    
                    if (distance <= renderDistance)
                    {
                        chunksInRange.Add(chunkPos);
                        
                        if (!_chunkData.ContainsKey(chunkPos) && !_chunksToLoad.Contains(chunkPos))
                        {
                            _chunksToLoad.Enqueue(chunkPos);
                        }
                    }
                }
            }
            
            var chunksToUnload = new List<Vector2Int>();
            foreach (var loadedChunk in _chunkData.Keys)
            {
                if (!chunksInRange.Contains(loadedChunk))
                {
                    chunksToUnload.Add(loadedChunk);
                }
            }
            
            foreach (var chunkPos in chunksToUnload)
            {
                if (!_chunksToUnload.Contains(chunkPos))
                {
                    _chunksToUnload.Enqueue(chunkPos);
                }
            }
        }
        
        private void ProcessChunkQueues()
        {
            int processedCount = 0;
            
            while (_chunksToUnload.Count > 0 && processedCount < chunksPerFrame)
            {
                var chunkPos = _chunksToUnload.Dequeue();
                UnloadChunk(chunkPos);
                processedCount++;
            }
            
            while (_chunksToLoad.Count > 0 && processedCount < chunksPerFrame)
            {
                var chunkPos = _chunksToLoad.Dequeue();
                RequestChunkFromServer(chunkPos);
                processedCount++;
            }
        }
        
        private void ProcessChunkUpdates()
        {
            int updateCount = 0;
            while (_chunksToUpdate.Count > 0 && updateCount < chunksPerFrame)
            {
                var chunkPos = _chunksToUpdate.Dequeue();
                if (_chunkRenderers.TryGetValue(chunkPos, out var renderer))
                {
                    renderer.UpdateMesh();
                }
                updateCount++;
            }
        }
        
        private void RequestChunkFromServer(Vector2Int chunkPos)
        {
            // First try to generate locally if offline mode or server unavailable
            if (_clientConfig != null && _clientConfig.Network != null && _clientConfig.Network.EnableOfflineMode || _gameClient == null)
            {
                GenerateChunkLocally(chunkPos);
                return;
            }

            if (_gameClient != null && _gameClient.IsConnected)
            {
                _gameClient.RequestChunk(chunkPos.x, chunkPos.y);
            }
            else
            {
                // Fallback to local generation
                GenerateChunkLocally(chunkPos);
            }
        }
        
        private void GenerateChunkLocally(Vector2Int chunkPos)
        {
            if (_terrainGenerator == null)
            {
                Debug.LogError("TerrainGenerator not available for local chunk generation");
                return;
            }
            
            try
            {
                var blocks = _terrainGenerator.GenerateChunk(chunkPos.x, chunkPos.y);
                var entities = new List<EntityInfo>(); // No entities in local generation
                var biomeData = new BiomeInfo(); // Basic biome info
                
                var chunkSnapshot = new ChunkSnapshot(
                    chunkPos.x,
                    chunkPos.y,
                    blocks,
                    biomeData,
                    entities,
                    isFromCache: false
                );
                
                LoadChunk(chunkSnapshot);
                Debug.Log($"Generated chunk locally: ({chunkPos.x}, {chunkPos.y})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate chunk locally ({chunkPos.x}, {chunkPos.y}): {ex.Message}");
            }
        }
        
        public void LoadChunk(ChunkSnapshot chunkData)
        {
            var chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            
            if (_chunkData.ContainsKey(chunkPos))
            {
                UpdateChunk(chunkData);
                return;
            }
            
            _chunkData[chunkPos] = chunkData;
            
            var chunkObj = CreateChunkObject(chunkPos);
            _chunkObjects[chunkPos] = chunkObj;
            
            var renderer = chunkObj.GetComponent<ChunkRenderer>();
            if (renderer == null)
            {
                renderer = chunkObj.AddComponent<ChunkRenderer>();
            }
            
            renderer.Initialize(chunkData, _blockTypes, blockMaterial);
            _chunkRenderers[chunkPos] = renderer;
            
            if (chunkData.Entities != null)
            {
                foreach (var entity in chunkData.Entities)
                {
                    CreateEntity(entity);
                }
            }
            
            ChunkLoaded?.Invoke(chunkPos);
            Debug.Log($"Loaded chunk ({chunkPos.x}, {chunkPos.y})");
        }
        
        public void UpdateChunk(ChunkSnapshot chunkData)
        {
            var chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            
            if (!_chunkData.ContainsKey(chunkPos)) return;
            
            _chunkData[chunkPos] = chunkData;
            
            if (_chunkRenderers.TryGetValue(chunkPos, out var renderer))
            {
                renderer.UpdateData(chunkData);
                _chunksToUpdate.Enqueue(chunkPos);
            }
        }
        
        public void UnloadChunk(Vector2Int chunkPos)
        {
            if (!_chunkData.ContainsKey(chunkPos)) return;
            
            _chunkData.Remove(chunkPos);
            
            if (_chunkObjects.TryGetValue(chunkPos, out var chunkObj))
            {
                DestroyImmediate(chunkObj);
                _chunkObjects.Remove(chunkPos);
            }
            
            _chunkRenderers.Remove(chunkPos);
            
            ChunkUnloaded?.Invoke(chunkPos);
            Debug.Log($"Unloaded chunk ({chunkPos.x}, {chunkPos.y})");
        }
        
        private GameObject CreateChunkObject(Vector2Int chunkPos)
        {
            var chunkObj = chunkPrefab != null ? Instantiate(chunkPrefab) : new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
            
            var worldPos = new Vector3(chunkPos.x * ChunkSize, 0, chunkPos.y * ChunkSize);
            chunkObj.transform.position = worldPos;
            chunkObj.transform.parent = transform;
            
            if (chunkObj.GetComponent<MeshFilter>() == null)
                chunkObj.AddComponent<MeshFilter>();
            if (chunkObj.GetComponent<MeshRenderer>() == null)
            {
                var meshRenderer = chunkObj.AddComponent<MeshRenderer>();
                meshRenderer.material = blockMaterial;
            }
            if (chunkObj.GetComponent<MeshCollider>() == null)
                chunkObj.AddComponent<MeshCollider>();
            
            return chunkObj;
        }
        
        private void CreateEntity(EntityInfo entityInfo)
        {
            var entityObj = new GameObject($"Entity_{entityInfo.EntityId}");
            var pos = entityInfo.Position;
            entityObj.transform.position = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z);
            entityObj.transform.parent = transform;
            
            var entityComponent = entityObj.AddComponent<EntityController>();
            entityComponent.Initialize(entityInfo);
        }
        
        public void ChangeBlock(Vector3Int blockPos, int oldBlockId, int newBlockId)
        {
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(blockPos.x / (float)ChunkSize),
                Mathf.FloorToInt(blockPos.z / (float)ChunkSize)
            );
            
            if (!_chunkData.TryGetValue(chunkPos, out var chunkData)) return;
            
            var localBlockPos = new Vector3Int(
                blockPos.x - (chunkPos.x * ChunkSize),
                blockPos.y,
                blockPos.z - (chunkPos.y * ChunkSize)
            );
            
            UpdateBlockInChunk(chunkData, localBlockPos, newBlockId);
            
            if (!_chunksToUpdate.Contains(chunkPos))
            {
                _chunksToUpdate.Enqueue(chunkPos);
            }
            
            BlockChanged?.Invoke(blockPos, oldBlockId, newBlockId);
        }
        
        private void UpdateBlockInChunk(ChunkSnapshot chunkData, Vector3Int localPos, int newBlockId)
        {
            chunkData.SetBlockId(localPos.x, localPos.y, localPos.z, newBlockId);
        }
        
        public int GetBlockAt(Vector3Int worldPos)
        {
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / (float)ChunkSize),
                Mathf.FloorToInt(worldPos.z / (float)ChunkSize)
            );
            
            if (!_chunkData.TryGetValue(chunkPos, out var chunkData))
                return 0;
            
            var localPos = new Vector3Int(
                worldPos.x - (chunkPos.x * ChunkSize),
                worldPos.y,
                worldPos.z - (chunkPos.y * ChunkSize)
            );
            
            return chunkData.GetBlockId(localPos.x, localPos.y, localPos.z);
        }
        
        public BlockType GetBlockType(int blockId)
        {
            _blockTypes.TryGetValue(blockId, out var blockType);
            return blockType ?? _blockTypes[0];
        }
        
        public bool IsChunkLoaded(Vector2Int chunkPos)
        {
            return _chunkData.ContainsKey(chunkPos);
        }
        
        public IEnumerable<Vector2Int> GetLoadedChunks()
        {
            return _chunkData.Keys;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_worldConfig == null) return;
            
            var chunkSize = ChunkSize;
            var renderDistance = RenderDistance;
            
            Gizmos.color = Color.yellow;
            var centerPos = new Vector3(_playerChunkPos.x * chunkSize, 0, _playerChunkPos.y * chunkSize);
            Gizmos.DrawWireCube(centerPos, new Vector3(renderDistance * chunkSize * 2, 10, renderDistance * chunkSize * 2));
            
            Gizmos.color = Color.green;
            foreach (var chunkPos in _chunkData.Keys)
            {
                var worldPos = new Vector3(chunkPos.x * chunkSize, 5, chunkPos.y * chunkSize);
                Gizmos.DrawWireCube(worldPos, new Vector3(chunkSize, 10, chunkSize));
            }
        }
        
        /// <summary>
        /// Reload chunk configuration and apply changes
        /// </summary>
        public void ReloadConfiguration()
        {
            InitializeConfiguration();
            Debug.Log("ChunkManager configuration reloaded");
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"Loaded Chunks: {_chunkData.Count} | " +
                   $"Queued Loads: {_chunksToLoad.Count} | " +
                   $"Queued Unloads: {_chunksToUnload.Count} | " +
                   $"Queued Updates: {_chunksToUpdate.Count} | " +
                   $"Render Distance: {RenderDistance}";
        }
    }
    
    /// <summary>
    /// Block type definition
    /// </summary>
    [System.Serializable]
    public class BlockType
    {
        public int Id { get; }
        public string Name { get; }
        public bool IsSolid { get; }
        public bool IsOpaque { get; }
        public float Hardness { get; set; } = 1f;
        public string TextureName { get; set; }
        
        public BlockType(int id, string name, bool isSolid, bool isOpaque)
        {
            Id = id;
            Name = name;
            IsSolid = isSolid;
            IsOpaque = isOpaque;
        }
    }
}
