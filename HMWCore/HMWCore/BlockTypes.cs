using System;
using System.Collections.Generic;

namespace HMWCore
{
    /// <summary>
    /// 블록 tile의 type 클래스.
    /// </summary>
    [Serializable]
    public enum BlockTileType
    {
        NONE = 0,
        EMPTY = 1,
        GRASS = 2,
        STONE_BIG = 3,
        STONE_SMALL = 4,
        SAND = 5,
        RED_STONE = 6,
        WOOD = 7,
        STONE_GOLD = 8,
        STONE_IRON = 9,
        STONE_SILVER = 10,
        NORMAL_TREE_LEAF = 11,
        SQAURE_TREE_LEAF = 12,
        WATER = 13,
        DIRT = 14,
        STONE = 15,
        COBBLESTONE = 16,
        IRON = 17,
        DIAMOND = 18
    }
    [Serializable]
    public enum ChunkType
    {
        TERRAIN = 0, // 지형 ( 동굴, 땅..)
        WATER = 1, // 물.
        ENVIROMENT = 2, // 환경 ( 나무, 풀..)
        NONE = 3,
        COUNT = NONE
    }
    [Serializable]
    public enum PlaneType
    {
        TOP,
        BOTTOM,
        FRONT,
        BACK,
        LEFT,
        RIGHT,
    }
    [Serializable]
    public enum WorldGenTypes
    {
        NONE = 0,
        GEN_NORMAL = 1,
        GEN_WITH_PERLIN = 2,
    }

    [Serializable]
    public struct PlaneData
    {
        public List<CustomVector3> Points;
        public CustomVector3 SurfaceNormal;
    }

    /// <summary>
    /// Block
    /// (1 x 1 x 1(unit))
    /// </summary>
    [Serializable]
    public struct Block
    {
        public byte CurrentType;
        public byte OriginalType;
        public float CenterX;
        public float CenterY;
        public float CenterZ;
        public bool bRendered;
        public int WorldDataIndexX;
        public int WorldDataIndexY;
        public int WorldDataIndexZ;
        public int Durability;
        public ChunkType OwnerChunkType; // 이 블록을 소유한 청크의 타입.
        public Dictionary<PlaneType, PlaneData> PlaneGroup;
        // 복사 생성자.
        public Block(Block b)
        {
            CurrentType = b.CurrentType;
            OriginalType = b.OriginalType;
            CenterX = b.CenterX;
            CenterY = b.CenterY;
            CenterZ = b.CenterZ;
            bRendered = b.bRendered;
            WorldDataIndexX = b.WorldDataIndexX;
            WorldDataIndexY = b.WorldDataIndexY;
            WorldDataIndexZ = b.WorldDataIndexZ;
            Durability = b.Durability;
            OwnerChunkType = b.OwnerChunkType;
            PlaneGroup = b.PlaneGroup;
        }

        public CustomVector3 GetCenterPosition()
        {
            return new CustomVector3(CenterX, CenterY, CenterZ);
        }
    }
}
