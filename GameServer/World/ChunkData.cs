using System;
using GameServerApp.Models;

namespace GameServerApp.World
{
    public class LoadedChunk
    {
        public ChunkData Data { get; set; }
        public DateTime LastAccessed { get; set; }
        public bool IsModified { get; set; }
    }

    public class ChunkData
    {
        private readonly BlockType[,,] _blocks = new BlockType[16, 256, 16];
        private readonly BiomeType[,] _biomes = new BiomeType[16, 16];
        public int ChunkX { get; }
        public int ChunkZ { get; }

        public ChunkData(int chunkX, int chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }

        public BlockType GetBlock(int x, int y, int z)
        {
            if (x >= 0 && x < 16 && y >= 0 && y < 256 && z >= 0 && z < 16)
                return _blocks[x, y, z];
            return BlockType.Air;
        }

        public void SetBlock(int x, int y, int z, BlockType blockType)
        {
            if (x >= 0 && x < 16 && y >= 0 && y < 256 && z >= 0 && z < 16)
                _blocks[x, y, z] = blockType;
        }

        public BiomeType GetBiome(int x, int z)
        {
            if (x >= 0 && x < 16 && z >= 0 && z < 16)
                return _biomes[x, z];
            return BiomeType.Plains;
        }

        public void SetBiome(int x, int z, BiomeType biome)
        {
            if (x >= 0 && x < 16 && z >= 0 && z < 16)
                _biomes[x, z] = biome;
        }

        public (byte[] blockData, byte[] biomeData) ToBytes()
        {
            var blockData = new byte[16 * 256 * 16 * 2];
            var biomeData = new byte[16 * 16];
            
            int blockIndex = 0;
            for (int y = 0; y < 256; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        var blockType = (ushort)_blocks[x, y, z];
                        blockData[blockIndex] = (byte)(blockType & 0xFF);
                        blockData[blockIndex + 1] = (byte)((blockType >> 8) & 0xFF);
                        blockIndex += 2;
                    }
                }
            }
            
            int biomeIndex = 0;
            for (int z = 0; z < 16; z++)
            {
                for (int x = 0; x < 16; x++)
                {
                    biomeData[biomeIndex++] = (byte)_biomes[x, z];
                }
            }
            
            return (blockData, biomeData);
        }

        public static ChunkData FromBytes(byte[] blockData, byte[] biomeData)
        {
            var chunk = new ChunkData(0, 0);
            
            if (blockData.Length >= 16 * 256 * 16 * 2)
            {
                int blockIndex = 0;
                for (int y = 0; y < 256; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            var blockType = (BlockType)(blockData[blockIndex] | (blockData[blockIndex + 1] << 8));
                            chunk._blocks[x, y, z] = blockType;
                            blockIndex += 2;
                        }
                    }
                }
            }
            
            if (biomeData.Length >= 16 * 16)
            {
                int biomeIndex = 0;
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        chunk._biomes[x, z] = (BiomeType)biomeData[biomeIndex++];
                    }
                }
            }
            
            return chunk;
        }
    }
}
