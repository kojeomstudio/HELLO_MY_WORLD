using System;
using System;
using System.Collections.Generic;
using EnhancedMinecraftProtocol;
using SharedProtocol;
using UnityEngine;

namespace Minecraft.World
{
    public readonly struct EnhancedChunkMetadata
    {
        public static readonly EnhancedChunkMetadata Empty = new EnhancedChunkMetadata(0, 0, 0, 0, 0, null);

        public EnhancedChunkMetadata(long generationTimestamp, int totalRequested, int totalSent, int blockBytes, int biomeBytes, ChunkData? chunkData)
        {
            GenerationTimestamp = generationTimestamp;
            TotalRequested = totalRequested;
            TotalSent = totalSent;
            BlockBytes = blockBytes;
            BiomeBytes = biomeBytes;
            ChunkData = chunkData;
        }

        public long GenerationTimestamp { get; }
        public int TotalRequested { get; }
        public int TotalSent { get; }
        public int BlockBytes { get; }
        public int BiomeBytes { get; }
        public ChunkData? ChunkData { get; }
        public bool HasEnhancedData => ChunkData != null;
    }

    /// <summary>
    /// Client-side representation of a chunk with decoded block data.
    /// Stores raw block ids (16x256x16) along with biome and entity metadata.
    /// </summary>
    public sealed class ChunkSnapshot
    {
        public const int ChunkSize = 16;
        public const int ChunkHeight = 256;
        public const int BlockCount = ChunkSize * ChunkSize * ChunkHeight;

        private readonly byte[] _blocks;

        public ChunkSnapshot(int chunkX, int chunkZ, byte[] blocks, BiomeInfo biome, IReadOnlyList<EntityInfo> entities, bool isFromCache, EnhancedChunkMetadata metadata)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            _blocks = blocks ?? Array.Empty<byte>();
            if (_blocks.Length != BlockCount)
            {
                Array.Resize(ref _blocks, BlockCount);
            }

            Biome = biome;
            Entities = entities ?? Array.Empty<EntityInfo>();
            IsFromCache = isFromCache;
            EnhancedMetadata = metadata;
        }

        public int ChunkX { get; }
        public int ChunkZ { get; }
        public BiomeInfo Biome { get; }
        public IReadOnlyList<EntityInfo> Entities { get; }
        public bool IsFromCache { get; }
        public EnhancedChunkMetadata EnhancedMetadata { get; }

        public byte[] Blocks => _blocks;

        public int GetBlockId(int localX, int y, int localZ)
        {
            if (!IsWithinBounds(localX, y, localZ))
            {
                return 0;
            }

            return _blocks[GetBlockIndex(localX, y, localZ)];
        }

        public void SetBlockId(int localX, int y, int localZ, int blockId)
        {
            if (!IsWithinBounds(localX, y, localZ))
            {
                return;
            }

            _blocks[GetBlockIndex(localX, y, localZ)] = (byte)Mathf.Clamp(blockId, 0, 255);
        }

        public void CopyBlocksTo(byte[,,] target)
        {
            if (target.GetLength(0) != ChunkSize ||
                target.GetLength(1) != ChunkHeight ||
                target.GetLength(2) != ChunkSize)
            {
                throw new ArgumentException("Target array dimensions must be 16x256x16", nameof(target));
            }

            int index = 0;
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        target[x, y, z] = index < _blocks.Length ? _blocks[index] : (byte)0;
                        index++;
                    }
                }
            }
        }

        private static int GetBlockIndex(int x, int y, int z)
        {
            return y * (ChunkSize * ChunkSize) + z * ChunkSize + x;
        }

        private static bool IsWithinBounds(int x, int y, int z)
        {
            return x >= 0 && x < ChunkSize &&
                   y >= 0 && y < ChunkHeight &&
                   z >= 0 && z < ChunkSize;
        }
    }
}
