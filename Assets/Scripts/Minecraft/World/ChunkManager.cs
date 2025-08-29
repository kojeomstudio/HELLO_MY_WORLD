using System;
using System.Collections.Generic;
using UnityEngine;
using MinecraftProtocol;
using System.Linq;

namespace Minecraft.World
{
    /// <summary>
    /// System that manages chunks in the Minecraft world
    /// Handles chunk loading, unloading, rendering, block management, etc.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int worldHeight = 256;
        [SerializeField] private int renderDistance = 8;
        [SerializeField] private Material blockMaterial;
        [SerializeField] private GameObject chunkPrefab;
        
        [Header("Performance Settings")]
        [SerializeField] private int chunksPerFrame = 2;
        [SerializeField] private float chunkUpdateInterval = 0.1f;
        
        private Dictionary<Vector2Int, ChunkInfo> _chunkData = new();
        private Dictionary<Vector2Int, GameObject> _chunkObjects = new();
        private Dictionary<Vector2Int, ChunkRenderer> _chunkRenderers = new();
        
        private Vector2Int _playerChunkPos;
        private Queue<Vector2Int> _chunksToLoad = new();
        private Queue<Vector2Int> _chunksToUnload = new();
        private Queue<Vector2Int> _chunksToUpdate = new();
        
        private Dictionary<int, BlockType> _blockTypes = new();
        
        public event Action<Vector2Int> ChunkLoaded;
        public event Action<Vector2Int> ChunkUnloaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        
        public int LoadedChunkCount => _chunkData.Count;
        public Vector2Int PlayerChunkPosition => _playerChunkPos;
        
        private void Start()
        {
            InitializeBlockTypes();
            InvokeRepeating(nameof(ProcessChunkUpdates), 0f, chunkUpdateInterval);
        }
        
        private void Update()
        {
            UpdatePlayerChunkPosition();
            ProcessChunkQueues();
        }
        
        private void InitializeBlockTypes()
        {
            _blockTypes[0] = new BlockType(0, "Air", false, false);
            _blockTypes[1] = new BlockType(1, "Stone", true, true);
            _blockTypes[2] = new BlockType(2, "Grass", true, true);
            _blockTypes[3] = new BlockType(3, "Dirt", true, true);
            _blockTypes[4] = new BlockType(4, "Cobblestone", true, true);
            _blockTypes[5] = new BlockType(5, "Wood", true, true);
            _blockTypes[6] = new BlockType(6, "Leaves", true, false);
            _blockTypes[7] = new BlockType(7, "Sand", true, true);
            _blockTypes[8] = new BlockType(8, "Water", false, false);
            _blockTypes[9] = new BlockType(9, "Lava", false, false);
            
            Debug.Log($"Initialized {_blockTypes.Count} block types");
        }
        
        private void UpdatePlayerChunkPosition()
        {
            var playerPos = transform.position;
            var newChunkPos = new Vector2Int(
                Mathf.FloorToInt(playerPos.x / chunkSize),
                Mathf.FloorToInt(playerPos.z / chunkSize)
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
            var networkClient = FindObjectOfType<Core.MinecraftNetworkClient>();
            if (networkClient != null)
            {
                networkClient.RequestChunk(chunkPos.x, chunkPos.y);
            }
        }
        
        public void LoadChunk(ChunkInfo chunkInfo)
        {
            var chunkPos = new Vector2Int(chunkInfo.ChunkX, chunkInfo.ChunkZ);
            
            if (_chunkData.ContainsKey(chunkPos))
            {
                UpdateChunk(chunkInfo);
                return;
            }
            
            _chunkData[chunkPos] = chunkInfo;
            
            var chunkObj = CreateChunkObject(chunkPos);
            _chunkObjects[chunkPos] = chunkObj;
            
            var renderer = chunkObj.GetComponent<ChunkRenderer>();
            if (renderer == null)
            {
                renderer = chunkObj.AddComponent<ChunkRenderer>();
            }
            
            renderer.Initialize(chunkInfo, _blockTypes, blockMaterial);
            _chunkRenderers[chunkPos] = renderer;
            
            foreach (var entity in chunkInfo.Entities)
            {
                CreateEntity(entity);
            }
            
            ChunkLoaded?.Invoke(chunkPos);
            Debug.Log($"Loaded chunk ({chunkPos.x}, {chunkPos.y})");
        }
        
        public void UpdateChunk(ChunkInfo chunkInfo)
        {
            var chunkPos = new Vector2Int(chunkInfo.ChunkX, chunkInfo.ChunkZ);
            
            if (!_chunkData.ContainsKey(chunkPos)) return;
            
            _chunkData[chunkPos] = chunkInfo;
            
            if (_chunkRenderers.TryGetValue(chunkPos, out var renderer))
            {
                renderer.UpdateData(chunkInfo);
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
            
            var worldPos = new Vector3(chunkPos.x * chunkSize, 0, chunkPos.y * chunkSize);
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
                Mathf.FloorToInt(blockPos.x / (float)chunkSize),
                Mathf.FloorToInt(blockPos.z / (float)chunkSize)
            );
            
            if (!_chunkData.TryGetValue(chunkPos, out var chunkData)) return;
            
            var localBlockPos = new Vector3Int(
                blockPos.x - (chunkPos.x * chunkSize),
                blockPos.y,
                blockPos.z - (chunkPos.y * chunkSize)
            );
            
            UpdateBlockInChunk(chunkData, localBlockPos, newBlockId);
            
            if (!_chunksToUpdate.Contains(chunkPos))
            {
                _chunksToUpdate.Enqueue(chunkPos);
            }
            
            BlockChanged?.Invoke(blockPos, oldBlockId, newBlockId);
        }
        
        private void UpdateBlockInChunk(ChunkInfo chunkData, Vector3Int localPos, int newBlockId)
        {
            foreach (var block in chunkData.Blocks)
            {
                if (block.Position.X == localPos.x && 
                    block.Position.Y == localPos.y && 
                    block.Position.Z == localPos.z)
                {
                    var updatedBlock = new BlockInfo
                    {
                        BlockId = newBlockId,
                        Position = block.Position,
                        Metadata = block.Metadata,
                        BlockEntityData = block.BlockEntityData,
                        LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    break;
                }
            }
        }
        
        public int GetBlockAt(Vector3Int worldPos)
        {
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / (float)chunkSize),
                Mathf.FloorToInt(worldPos.z / (float)chunkSize)
            );
            
            if (!_chunkData.TryGetValue(chunkPos, out var chunkData))
                return 0;
            
            var localPos = new Vector3Int(
                worldPos.x - (chunkPos.x * chunkSize),
                worldPos.y,
                worldPos.z - (chunkPos.y * chunkSize)
            );
            
            foreach (var block in chunkData.Blocks)
            {
                if (block.Position.X == localPos.x && 
                    block.Position.Y == localPos.y && 
                    block.Position.Z == localPos.z)
                {
                    return block.BlockId;
                }
            }
            
            return 0;
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