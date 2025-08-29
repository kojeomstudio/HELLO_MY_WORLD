using System.Collections.Generic;
using UnityEngine;
using MinecraftProtocol;
using Minecraft.World;

namespace Minecraft.World
{
    /// <summary>
    /// Component responsible for Minecraft-style chunk rendering
    /// Converts blocks to efficient meshes and uses texture atlases.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        [Header("Rendering Settings")]
        [SerializeField] private bool enableAmbientOcclusion = true;
        [SerializeField] private bool enableBackfaceCulling = true;
        [SerializeField] private float blockSize = 1f;
        
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        
        private ChunkInfo _chunkData;
        private Dictionary<int, BlockType> _blockTypes;
        private Material _material;
        
        private List<Vector3> _vertices = new();
        private List<int> _triangles = new();
        private List<Vector2> _uvs = new();
        private List<Vector3> _normals = new();
        private List<Color> _colors = new();
        
        private bool _needsUpdate = false;
        private Mesh _mesh;
        
        private static readonly Vector3[] _blockVertices = new Vector3[8]
        {
            new Vector3(0, 0, 0), // 0: 왼쪽 아래 앞
            new Vector3(1, 0, 0), // 1: 오른쪽 아래 앞
            new Vector3(1, 1, 0), // 2: 오른쪽 위 앞
            new Vector3(0, 1, 0), // 3: 왼쪽 위 앞
            new Vector3(0, 0, 1), // 4: 왼쪽 아래 뒤
            new Vector3(1, 0, 1), // 5: 오른쪽 아래 뒤
            new Vector3(1, 1, 1), // 6: 오른쪽 위 뒤
            new Vector3(0, 1, 1)  // 7: 왼쪽 위 뒤
        };
        
        private static readonly int[][] _faceVertices = new int[6][]
        {
            new int[] { 0, 3, 1, 2 }, // 앞면 (Z-)
            new int[] { 5, 6, 4, 7 }, // 뒷면 (Z+)
            new int[] { 4, 7, 0, 3 }, // 왼쪽면 (X-)
            new int[] { 1, 2, 5, 6 }, // 오른쪽면 (X+)
            new int[] { 4, 0, 5, 1 }, // 아래면 (Y-)
            new int[] { 3, 7, 2, 6 }  // 위면 (Y+)
        };
        
        private static readonly Vector3[] _faceNormals = new Vector3[6]
        {
            Vector3.back,    // 앞면
            Vector3.forward, // 뒷면
            Vector3.left,    // 왼쪽면
            Vector3.right,   // 오른쪽면
            Vector3.down,    // 아래면
            Vector3.up       // 위면
        };
        
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            
            _mesh = new Mesh();
            _mesh.name = "ChunkMesh";
            _meshFilter.mesh = _mesh;
        }
        
        public void Initialize(ChunkInfo chunkData, Dictionary<int, BlockType> blockTypes, Material material)
        {
            _chunkData = chunkData;
            _blockTypes = blockTypes;
            _material = material;
            
            _meshRenderer.material = material;
            
            _needsUpdate = true;
            UpdateMesh();
        }
        
        public void UpdateData(ChunkInfo chunkData)
        {
            _chunkData = chunkData;
            _needsUpdate = true;
        }
        
        public void UpdateMesh()
        {
            if (!_needsUpdate || _chunkData == null) return;
            
            ClearMeshData();
            GenerateMesh();
            ApplyMeshData();
            
            _needsUpdate = false;
        }
        
        private void ClearMeshData()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _normals.Clear();
            _colors.Clear();
        }
        
        private void GenerateMesh()
        {
            if (_chunkData?.Blocks == null) return;
            
            var blockArray = CreateBlockArray();
            
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        var blockId = blockArray[x, y, z];
                        if (blockId == 0) continue;
                        
                        if (!_blockTypes.TryGetValue(blockId, out var blockType)) continue;
                        if (!blockType.IsSolid) continue;
                        
                        GenerateBlockMesh(x, y, z, blockType, blockArray);
                    }
                }
            }
        }
        
        private int[,,] CreateBlockArray()
        {
            var blockArray = new int[16, 256, 16];
            
            foreach (var block in _chunkData.Blocks)
            {
                var pos = block.Position;
                if (pos.X >= 0 && pos.X < 16 && pos.Y >= 0 && pos.Y < 256 && pos.Z >= 0 && pos.Z < 16)
                {
                    blockArray[pos.X, pos.Y, pos.Z] = block.BlockId;
                }
            }
            
            return blockArray;
        }
        
        private void GenerateBlockMesh(int x, int y, int z, BlockType blockType, int[,,] blockArray)
        {
            var blockPosition = new Vector3(x, y, z) * blockSize;
            
            for (int face = 0; face < 6; face++)
            {
                if (ShouldRenderFace(x, y, z, face, blockArray))
                {
                    AddBlockFace(blockPosition, face, blockType);
                }
            }
        }
        
        private bool ShouldRenderFace(int x, int y, int z, int face, int[,,] blockArray)
        {
            if (!enableBackfaceCulling) return true;
            
            var adjacentPos = GetAdjacentPosition(x, y, z, face);
            
            if (adjacentPos.x < 0 || adjacentPos.x >= 16 || 
                adjacentPos.y < 0 || adjacentPos.y >= 256 || 
                adjacentPos.z < 0 || adjacentPos.z >= 16)
            {
                return true;
            }
            
            var adjacentBlockId = blockArray[adjacentPos.x, adjacentPos.y, adjacentPos.z];
            if (adjacentBlockId == 0) return true;
            
            if (_blockTypes.TryGetValue(adjacentBlockId, out var adjacentBlockType))
            {
                return !adjacentBlockType.IsOpaque;
            }
            
            return false;
        }
        
        private Vector3Int GetAdjacentPosition(int x, int y, int z, int face)
        {
            return face switch
            {
                0 => new Vector3Int(x, y, z - 1), // 앞면
                1 => new Vector3Int(x, y, z + 1), // 뒷면
                2 => new Vector3Int(x - 1, y, z), // 왼쪽면
                3 => new Vector3Int(x + 1, y, z), // 오른쪽면
                4 => new Vector3Int(x, y - 1, z), // 아래면
                5 => new Vector3Int(x, y + 1, z), // 위면
                _ => new Vector3Int(x, y, z)
            };
        }
        
        private void AddBlockFace(Vector3 blockPosition, int faceIndex, BlockType blockType)
        {
            var startVertexIndex = _vertices.Count;
            var faceVertices = _faceVertices[faceIndex];
            var faceNormal = _faceNormals[faceIndex];
            
            for (int i = 0; i < 4; i++)
            {
                var localVertex = _blockVertices[faceVertices[i]] * blockSize;
                var worldVertex = blockPosition + localVertex;
                _vertices.Add(worldVertex);
                _normals.Add(faceNormal);
            }
            
            var uvs = GetBlockFaceUVs(blockType, faceIndex);
            _uvs.AddRange(uvs);
            
            var color = enableAmbientOcclusion ? CalculateAmbientOcclusion(blockPosition, faceIndex) : Color.white;
            for (int i = 0; i < 4; i++)
            {
                _colors.Add(color);
            }
            
            _triangles.AddRange(new int[]
            {
                startVertexIndex + 0, startVertexIndex + 1, startVertexIndex + 2,
                startVertexIndex + 0, startVertexIndex + 2, startVertexIndex + 3
            });
        }
        
        private Vector2[] GetBlockFaceUVs(BlockType blockType, int faceIndex)
        {
            float textureSize = 1f / 16f;
            float u = (blockType.Id % 16) * textureSize;
            float v = (blockType.Id / 16) * textureSize;
            
            return new Vector2[]
            {
                new Vector2(u, v),
                new Vector2(u + textureSize, v),
                new Vector2(u + textureSize, v + textureSize),
                new Vector2(u, v + textureSize)
            };
        }
        
        private Color CalculateAmbientOcclusion(Vector3 blockPosition, int faceIndex)
        {
            float brightness = faceIndex switch
            {
                5 => 1.0f,  // 위면
                4 => 0.5f,  // 아래면
                _ => 0.8f   // 옆면들
            };
            
            return new Color(brightness, brightness, brightness, 1f);
        }
        
        private void ApplyMeshData()
        {
            if (_vertices.Count == 0)
            {
                _mesh.Clear();
                return;
            }
            
            _mesh.Clear();
            
            _mesh.vertices = _vertices.ToArray();
            _mesh.triangles = _triangles.ToArray();
            _mesh.uv = _uvs.ToArray();
            _mesh.normals = _normals.ToArray();
            _mesh.colors = _colors.ToArray();
            
            _mesh.Optimize();
            _mesh.RecalculateBounds();
            
            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh = _mesh;
            }
            
            Debug.Log($"Generated mesh with {_vertices.Count} vertices, {_triangles.Count / 3} triangles");
        }
        
        public void UpdateBlock(Vector3Int localPosition, int newBlockId)
        {
            bool blockFound = false;
            foreach (var block in _chunkData.Blocks)
            {
                if (block.Position.X == localPosition.x && 
                    block.Position.Y == localPosition.y && 
                    block.Position.Z == localPosition.z)
                {
                    blockFound = true;
                    break;
                }
            }
            
            _needsUpdate = true;
        }
        
        public MeshStats GetMeshStats()
        {
            return new MeshStats
            {
                VertexCount = _vertices.Count,
                TriangleCount = _triangles.Count / 3,
                FaceCount = _triangles.Count / 6,
                HasMesh = _mesh != null && _vertices.Count > 0
            };
        }
        
        private void OnDestroy()
        {
            if (_mesh != null)
            {
                DestroyImmediate(_mesh);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_chunkData == null) return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + new Vector3(8, 128, 8), new Vector3(16, 256, 16));
            
            if (_chunkData.Blocks.Count < 100)
            {
                Gizmos.color = Color.yellow;
                foreach (var block in _chunkData.Blocks)
                {
                    if (block.BlockId != 0)
                    {
                        var worldPos = transform.position + new Vector3(block.Position.X, block.Position.Y, block.Position.Z);
                        Gizmos.DrawWireCube(worldPos + Vector3.one * 0.5f, Vector3.one);
                    }
                }
            }
        }
    }
    
    [System.Serializable]
    public struct MeshStats
    {
        public int VertexCount;
        public int TriangleCount;
        public int FaceCount;
        public bool HasMesh;
        
        public override string ToString()
        {
            return $"Vertices: {VertexCount}, Triangles: {TriangleCount}, Faces: {FaceCount}";
        }
    }
}