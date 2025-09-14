using System;
using System.Collections;
using System.Collections.Generic;
using MapGenLib;

namespace MapGenLib
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
        WATER = 13
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
    public class WorldGenAlgorithms
    {
        private static List<CustomVector3> TreeSpawnCandidates = new List<CustomVector3>();

        public struct TerrainValue
        {
            public BlockTileType BlockType;
            public List<int> Layers;
        }
        public struct MakeWorldParam
        {
            public int BaseOffset;
            public bool bSurface;
        }
        public struct SubWorldSize
        {
            public int SizeX;
            public int SizeY;
            public int SizeZ;

            public SubWorldSize(int x, int y, int z)
            {
                SizeX = x;
                SizeY = y;
                SizeZ = z;
            }
        }

        public static TerrainValue[,] GenerateUndergroundTerrain(int areaSizeX, int areaSizeZ, int subWorldLayerNum, int subWorldSizeY, int randomSeed)
        {
            Utilitys.ChangeSeed(randomSeed);

            TerrainValue[,] terrainValues = new TerrainValue[areaSizeX, areaSizeZ];
            for (int x = 0; x < areaSizeX; x++)
            {
                for (int z = 0; z < areaSizeZ; z++)
                {
                    terrainValues[x, z].BlockType = BlockTileType.STONE_SMALL;
                    terrainValues[x, z].Layers = new List<int>();
                    for (int layer = 0; layer < subWorldLayerNum; layer++)
                    {
                        terrainValues[x, z].Layers.Add(subWorldSizeY);
                    }
                }
            }
            return terrainValues;
        }

        public static TerrainValue[,] GenerateNormalTerrain(int areaSizeX, int areaSizeZ, int subWorldLayerNum, int subWorldSizeY, int randomSeed, int generateNumber = 800)
        {
            Utilitys.ChangeSeed(randomSeed);
            //
            int[,] xzPlane = new int[areaSizeX, areaSizeZ];
            //
            int rangeValue = subWorldLayerNum * subWorldSizeY;
            int rangeHeightMin = -1 * rangeValue;
            int rangeHeightMax = rangeValue;
            CustomVector2[] startPoints = new CustomVector2[4];
            startPoints[0] = new CustomVector2(0, 0);
            startPoints[1] = new CustomVector2(0, areaSizeZ);
            startPoints[2] = new CustomVector2(areaSizeX, areaSizeZ);
            startPoints[3] = new CustomVector2(areaSizeX, 0);
            for (int loop = 0; loop < generateNumber; loop++)
            {
                CustomVector2 point1 = startPoints[Utilitys.RandomInteger(0, 4)];
                CustomVector2 point2 = new CustomVector2(Utilitys.RandomInteger(areaSizeX / 3, areaSizeX), Utilitys.RandomInteger(areaSizeZ / 3, areaSizeZ));
                CustomVector2 lineVector = point2 - point1;
                for (int x = 0; x < areaSizeX; x++)
                {
                    for (int z = 0; z < areaSizeZ; z++)
                    {
                        CustomVector2 point = new CustomVector2(x, z);
                        float dirValue = CustomVector3.Cross(new CustomVector3(point.x, point.y, 0.0f),
                                                             new CustomVector3(lineVector.x, lineVector.y, 0.0f)).z;
                        if (dirValue > 0)
                        {
                            if (Utilitys.RandomBool() == true) xzPlane[x, z]++;
                            else xzPlane[x, z]--;
                            xzPlane[x, z]++;

                        }
                        else if (dirValue <= 0)
                        {
                            if (Utilitys.RandomBool() == true) xzPlane[x, z]--;
                            else xzPlane[x, z]++;
                            xzPlane[x, z]--;
                        }
                        xzPlane[x, z] = CustomMathf.Clamp(xzPlane[x, z], rangeHeightMin, rangeHeightMax);
                    }
                }
            }

            // Normalize Terrain.
            int waterBasisValue = 0;
            int heightBasisValue = rangeHeightMax / 4;
            for (int x = 0; x < areaSizeX; x++)
            {
                for (int z = 0; z < areaSizeZ; z++)
                {
                    // Water 지형이라면, 평준화 시킨다.
                    if (xzPlane[x, z] <= waterBasisValue)
                    {
                        WorldGenerateUtils.NormalizeWaterTerrain(x, z, xzPlane, waterBasisValue, 6);
                    }
                    else if (xzPlane[x, z] >= heightBasisValue)
                    {
                        WorldGenerateUtils.ForceNormalize8Direction(x, z, xzPlane);
                    }
                }
            }

            // Calc Range per Chunk size.
            TerrainValue[,] terrainValues = new TerrainValue[areaSizeX, areaSizeZ];
            for (int x = 0; x < areaSizeX; x++)
            {
                for (int z = 0; z < areaSizeZ; z++)
                {
                    terrainValues[x, z].BlockType = WorldGenerateUtils.CalcTerrainValueToBlockType(xzPlane[x, z], subWorldLayerNum, subWorldSizeY);
                    terrainValues[x, z].Layers = new List<int>();
                    int absTerrainScalaValue = CustomMathf.Abs(xzPlane[x, z]);
                    for (int layer = 0; layer < subWorldLayerNum; layer++)
                    {
                        int rangeY = 0;
                        if (absTerrainScalaValue <= 0)
                        {
                            absTerrainScalaValue = 0;
                            rangeY = absTerrainScalaValue;
                        }
                        else if (absTerrainScalaValue < subWorldSizeY)
                        {
                            rangeY = absTerrainScalaValue;
                            absTerrainScalaValue -= subWorldSizeY;
                        }
                        else if (absTerrainScalaValue >= subWorldSizeY)
                        {
                            rangeY = subWorldSizeY;
                            absTerrainScalaValue -= subWorldSizeY;
                        }
                        //
                        terrainValues[x, z].Layers.Add(CustomMathf.Abs(rangeY));
                    }
                }
            }
            return terrainValues;
        }

        public static void GenerateUnderSubWorldWithPerlinNoise(Block[,,] subWorldBlockData, MakeWorldParam param, SubWorldSize subWorldSize)
        {
            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int internalTerrain = WorldGenerateUtils.PerlinNoise(x, 20, z, 3, Utilitys.RandomInteger(1, 3), 2);
                    internalTerrain += param.BaseOffset;

                    for (int y = 0; y < subWorldSize.SizeY; y++)
                    {
                        if (y <= internalTerrain)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)Utilitys.RandomInteger((int)BlockTileType.STONE_BIG, (int)BlockTileType.STONE_SILVER);
                        }
                        else
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_BIG;
                        }
                    }
                }
            }
            // caves
            GenerateSphereCaves(subWorldBlockData, subWorldSize);
        }

        /// <summary>
        /// 개선된 Perlin Noise를 이용한 월드 생성 - 더 다양한 지형과 구조물 포함
        /// </summary>
        public static void GenerateSubWorldWithPerlinNoise(Block[,,] subWorldBlockData, MakeWorldParam param, SubWorldSize subWorldSize)
        {
            CustomVector3 highestPoint = CustomVector3.zero;
            TreeSpawnCandidates.Clear(); // 이전 데이터 정리
            
            // 개선된 지형 생성 - 더 자연스러운 고도 변화
            GenerateImprovedTerrain(subWorldBlockData, param, subWorldSize, ref highestPoint);
            
            // 바이오움 기반 지형 처리
            GenerateBiomeSpecificTerrain(subWorldBlockData, subWorldSize);
            
            // 개선된 동굴 및 던전 시스템
            GenerateSphereCaves(subWorldBlockData, subWorldSize);
            GenerateDungeons(subWorldBlockData, subWorldSize);
            
            // 긑짐 및 광물 생성
            GenerateOreDeposits(subWorldBlockData, subWorldSize);
            
            // 다양한 식물 생성
            GenerateImprovedVegetation(subWorldBlockData, subWorldSize);
            
            // 물 생성 (개선된 버전)
            GenerateWaterSources(highestPoint, subWorldBlockData, subWorldSize);
        }
        
        /// <summary>
        /// 개선된 지형 생성
        /// </summary>
        private static void GenerateImprovedTerrain(Block[,,] subWorldBlockData, MakeWorldParam param, 
            SubWorldSize subWorldSize, ref CustomVector3 highestPoint)
        {
            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    // 다중 옥타브 Perlin Noise로 더 자연스러운 지형
                    int baseHeight = WorldGenerateUtils.PerlinNoise(x, 20, z, 4, 2, 3);
                    int detailHeight = WorldGenerateUtils.PerlinNoise(x, 21, z, 2, 1, 1);
                    int internalTerrain = baseHeight + detailHeight + param.BaseOffset;
                    
                    // 지형의 다양성을 위한 추가 노이즈
                    int surfaceVariation = WorldGenerateUtils.PerlinNoise(x, 22, z, 1, 1, 1);
                    int finalSurfaceHeight = internalTerrain + surfaceVariation;
                    
                    for (int y = 0; y < subWorldSize.SizeY; y++)
                    {
                        // 지하 지형 처리
                        if (y <= internalTerrain - 5)
                        {
                            // 깊은 지하는 단단한 돌
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_BIG;
                        }
                        else if (y <= internalTerrain)
                        {
                            // 지하 중간쫓은 일반 돌
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                        }
                        else if (y <= finalSurfaceHeight - 2)
                        {
                            // 표면 바로 아래는 흔 (두께 2블록)
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.SAND;
                        }
                        else if (y <= finalSurfaceHeight)
                        {
                            // 표면은 풀로 덮기
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.GRASS;
                            
                            // 가장 높은 지점 추적
                            if (y > highestPoint.y)
                            {
                                highestPoint = new CustomVector3(x, y, z);
                            }
                            
                            // 나무 심기 후보 위치 추가
                            if (y + 1 < subWorldSize.SizeY)
                            {
                                TreeSpawnCandidates.Add(new CustomVector3(x, y + 1, z));
                            }
                        }
                        // 그 위는 공기로 남겸니다 (기본값이 EMPTY)
                    }
                }
            }
        }
        
        /// <summary>
        /// 바이오움 기반 지형 처리
        /// </summary>
        private static void GenerateBiomeSpecificTerrain(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            // 처음에는 기본 바이오움을 사용, 나중에 바이오움 시스템 추가 시 확장 가능
            // TODO: 다른 바이오움(사막, 설원, 열대우림 등) 추가
        }
        
        /// <summary>
        /// 던전 생성
        /// </summary>
        private static void GenerateDungeons(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int dungeonCount = Utilitys.RandomInteger(1, 3);
            
            for (int i = 0; i < dungeonCount; i++)
            {
                // 던전 위치 (지하 깊은 곳에 생성)
                int dungeonX = Utilitys.RandomInteger(10, subWorldSize.SizeX - 10);
                int dungeonY = Utilitys.RandomInteger(5, subWorldSize.SizeY / 3);
                int dungeonZ = Utilitys.RandomInteger(10, subWorldSize.SizeZ - 10);
                
                // 던전 크기
                int roomWidth = Utilitys.RandomInteger(8, 15);
                int roomHeight = Utilitys.RandomInteger(5, 8);
                int roomDepth = Utilitys.RandomInteger(8, 15);
                
                // 던전 방 비우기
                for (int x = 0; x < roomWidth; x++)
                {
                    for (int y = 0; y < roomHeight; y++)
                    {
                        for (int z = 0; z < roomDepth; z++)
                        {
                            int worldX = dungeonX + x - roomWidth / 2;
                            int worldY = dungeonY + y;
                            int worldZ = dungeonZ + z - roomDepth / 2;
                            
                            if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                                worldY >= 0 && worldY < subWorldSize.SizeY &&
                                worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                            {
                                // 바닥과 천장, 벽은 돌로, 내부는 비우기
                                if (x == 0 || x == roomWidth - 1 || 
                                    y == 0 || y == roomHeight - 1 || 
                                    z == 0 || z == roomDepth - 1)
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.STONE_BIG;
                                }
                                else
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                                }
                            }
                        }
                    }
                }
                
                // 던전 입구 만들기
                int entranceX = dungeonX;
                int entranceY = dungeonY + 1;
                int entranceZ = dungeonZ - roomDepth / 2;
                
                if (entranceX >= 0 && entranceX < subWorldSize.SizeX &&
                    entranceY >= 0 && entranceY < subWorldSize.SizeY &&
                    entranceZ >= 0 && entranceZ < subWorldSize.SizeZ)
                {
                    subWorldBlockData[entranceX, entranceY, entranceZ].CurrentType = (byte)BlockTileType.EMPTY;
                    subWorldBlockData[entranceX, entranceY + 1, entranceZ].CurrentType = (byte)BlockTileType.EMPTY;
                }
            }
        }
        
        /// <summary>
        /// 긑짐 및 광물 생성
        /// </summary>
        private static void GenerateOreDeposits(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            // 금광
            GenerateOreType(subWorldBlockData, subWorldSize, BlockTileType.STONE_GOLD, 2, 4);
            
            // 철광
            GenerateOreType(subWorldBlockData, subWorldSize, BlockTileType.STONE_IRON, 3, 6);
            
            // 은광
            GenerateOreType(subWorldBlockData, subWorldSize, BlockTileType.STONE_SILVER, 2, 5);
            
            // 레드스톤
            GenerateOreType(subWorldBlockData, subWorldSize, BlockTileType.RED_STONE, 1, 3);
        }
        
        /// <summary>
        /// 특정 광물 종류를 생성
        /// </summary>
        private static void GenerateOreType(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, 
            BlockTileType oreType, int minDeposits, int maxDeposits)
        {
            int depositCount = Utilitys.RandomInteger(minDeposits, maxDeposits);
            
            for (int i = 0; i < depositCount; i++)
            {
                int depositX = Utilitys.RandomInteger(3, subWorldSize.SizeX - 3);
                int depositY = Utilitys.RandomInteger(3, subWorldSize.SizeY / 2); // 지하 깊은 곳에 생성
                int depositZ = Utilitys.RandomInteger(3, subWorldSize.SizeZ - 3);
                
                int depositSize = Utilitys.RandomInteger(2, 5);
                
                // 광물 덩어리 생성
                for (int x = -depositSize / 2; x <= depositSize / 2; x++)
                {
                    for (int y = -depositSize / 2; y <= depositSize / 2; y++)
                    {
                        for (int z = -depositSize / 2; z <= depositSize / 2; z++)
                        {
                            int worldX = depositX + x;
                            int worldY = depositY + y;
                            int worldZ = depositZ + z;
                            
                            if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                                worldY >= 0 && worldY < subWorldSize.SizeY &&
                                worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                            {
                                // 기존 돌 블록만 광물로 교체
                                if (subWorldBlockData[worldX, worldY, worldZ].CurrentType == (byte)BlockTileType.STONE_BIG ||
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType == (byte)BlockTileType.STONE_SMALL)
                                {
                                    if (Utilitys.RandomBool()) // 50% 확률로 대체
                                    {
                                        subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)oreType;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 개선된 식물 생성
        /// </summary>
        private static void GenerateImprovedVegetation(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            if (TreeSpawnCandidates.Count == 0) return;
            
            // 다양한 종류의 나무 생성
            int treeSpawnCount = Utilitys.RandomInteger(5, 12); // 더 많은 나무
            
            for (int spawnCnt = 0; spawnCnt < treeSpawnCount && TreeSpawnCandidates.Count > 0; spawnCnt++)
            {
                int candidateIndex = Utilitys.RandomInteger(0, TreeSpawnCandidates.Count);
                CustomVector3 spawnPos = TreeSpawnCandidates[candidateIndex];
                TreeSpawnCandidates.RemoveAt(candidateIndex); // 사용한 후보 제거
                
                // 나무 종류 선택
                TreeType randTreeType = (TreeType)Utilitys.RandomInteger(0, (int)TreeType.COUNT);
                switch (randTreeType)
                {
                    case TreeType.NORMAL:
                        EnviromentGenAlgorithms.GenerateDefaultTree(subWorldBlockData, spawnPos, subWorldSize);
                        break;
                    case TreeType.SQAURE:
                        EnviromentGenAlgorithms.GenerateSqaureTree(subWorldBlockData, spawnPos, subWorldSize);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 개선된 물 소스 생성
        /// </summary>
        private static void GenerateWaterSources(CustomVector3 highestPoint, Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            // 기존 방법 외에 추가로 물웅덩이나 강 생성
            EnviromentGenAlgorithms.MakeDefaultWaterArea(highestPoint, subWorldBlockData, subWorldSize);
            
            // 산 발적인 작은 물웅덩이 생성
            int pondCount = Utilitys.RandomInteger(1, 3);
            for (int i = 0; i < pondCount; i++)
            {
                int pondX = Utilitys.RandomInteger(5, subWorldSize.SizeX - 5);
                int pondZ = Utilitys.RandomInteger(5, subWorldSize.SizeZ - 5);
                
                // 해당 위치의 지표면 찾기
                int surfaceY = FindSurfaceLevel(subWorldBlockData, subWorldSize, pondX, pondZ);
                if (surfaceY > 0)
                {
                    // 작은 원형 물웅덩이 생성
                    int pondRadius = Utilitys.RandomInteger(2, 4);
                    for (int x = -pondRadius; x <= pondRadius; x++)
                    {
                        for (int z = -pondRadius; z <= pondRadius; z++)
                        {
                            int worldX = pondX + x;
                            int worldZ = pondZ + z;
                            
                            if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                                worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                            {
                                float distance = CustomMathf.Sqrt(x * x + z * z);
                                if (distance <= pondRadius)
                                {
                                    // 물 바닥 비우고 물 채우기
                                    if (surfaceY >= 0 && surfaceY < subWorldSize.SizeY)
                                    {
                                        subWorldBlockData[worldX, surfaceY, worldZ].CurrentType = (byte)BlockTileType.WATER;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 지표면 높이 찾기
        /// </summary>
        private static int FindSurfaceLevel(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int x, int z)
        {
            for (int y = subWorldSize.SizeY - 1; y >= 0; y--)
            {
                if (subWorldBlockData[x, y, z].CurrentType != (byte)BlockTileType.EMPTY)
                {
                    return y;
                }
            }
            return -1;
        }

        /// <summary>
        /// 개선된 동굴 생성 시스템 - 더 자연스럽고 다양한 동굴 구조
        /// </summary>
        /// <param name="subWorldBlockData"></param>
        /// <param name="subWorldSize"></param>
        private static void GenerateSphereCaves(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            // 대형 동굴 시스템 생성
            GenerateLargeCaveSystem(subWorldBlockData, subWorldSize);
            
            // 소형 동굴 방 생성
            GenerateSmallCaves(subWorldBlockData, subWorldSize);
            
            // 지하 호수 생성
            GenerateUndergroundLakes(subWorldBlockData, subWorldSize);
        }
        
        /// <summary>
        /// 대형 동굴 시스템 생성 - 다수의 연결된 통로
        /// </summary>
        private static void GenerateLargeCaveSystem(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int caveSystemCount = Utilitys.RandomInteger(1, 3); // 1-3개의 대형 동굴 시스템
            
            for (int system = 0; system < caveSystemCount; system++)
            {
                // 시작점 설정
                int centerX = Utilitys.RandomInteger(subWorldSize.SizeX / 4, 3 * subWorldSize.SizeX / 4);
                int centerY = Utilitys.RandomInteger(subWorldSize.SizeY / 4, 3 * subWorldSize.SizeY / 4);
                int centerZ = Utilitys.RandomInteger(subWorldSize.SizeZ / 4, 3 * subWorldSize.SizeZ / 4);
                
                // 대형 동굴 방 생성
                GenerateLargeCaveRoom(subWorldBlockData, subWorldSize, centerX, centerY, centerZ);
                
                // 중심에서 방사상으로 통로 생성
                int tunnelCount = Utilitys.RandomInteger(3, 6);
                for (int tunnel = 0; tunnel < tunnelCount; tunnel++)
                {
                    GenerateCaveTunnel(subWorldBlockData, subWorldSize, centerX, centerY, centerZ, 
                        Utilitys.RandomFloat(0, 360), Utilitys.RandomFloat(-30, 30));
                }
            }
        }
        
        /// <summary>
        /// 대형 동굴 방 생성
        /// </summary>
        private static void GenerateLargeCaveRoom(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, 
            int centerX, int centerY, int centerZ)
        {
            int roomRadius = Utilitys.RandomInteger(8, 15);
            
            for (int x = -roomRadius; x <= roomRadius; x++)
            {
                for (int y = -roomRadius; y <= roomRadius; y++)
                {
                    for (int z = -roomRadius; z <= roomRadius; z++)
                    {
                        int worldX = centerX + x;
                        int worldY = centerY + y;
                        int worldZ = centerZ + z;
                        
                        if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                            worldY >= 0 && worldY < subWorldSize.SizeY &&
                            worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                        {
                            float distance = CustomMathf.Sqrt(x * x + y * y + z * z);
                            
                            // 방의 테두리는 더 고르지 않게 만들기
                            if (distance <= roomRadius - Utilitys.RandomFloat(0, 3))
                            {
                                subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 동굴 통로 생성
        /// </summary>
        private static void GenerateCaveTunnel(Block[,,] subWorldBlockData, SubWorldSize subWorldSize,
            int startX, int startY, int startZ, float direction, float pitch)
        {
            float currentX = startX;
            float currentY = startY;
            float currentZ = startZ;
            
            int tunnelLength = Utilitys.RandomInteger(20, 50);
            float tunnelRadius = Utilitys.RandomFloat(2.5f, 4.5f);
            
            for (int step = 0; step < tunnelLength; step++)
            {
                // 통로 파기
                CarveTunnelSegment(subWorldBlockData, subWorldSize, (int)currentX, (int)currentY, (int)currentZ, tunnelRadius);
                
                // 다음 위치 계산
                currentX += CustomMathf.Cos(direction * CustomMathf.Deg2Rad) * CustomMathf.Cos(pitch * CustomMathf.Deg2Rad);
                currentY += CustomMathf.Sin(pitch * CustomMathf.Deg2Rad);
                currentZ += CustomMathf.Sin(direction * CustomMathf.Deg2Rad) * CustomMathf.Cos(pitch * CustomMathf.Deg2Rad);
                
                // 방향 약간 변경 (자연스러운 구불구불함)
                direction += Utilitys.RandomFloat(-5, 5);
                pitch += Utilitys.RandomFloat(-3, 3);
                pitch = CustomMathf.Clamp(pitch, -45, 45); // 수직 각도 제한
                
                // 범위를 벗어나면 중단
                if (currentX < 0 || currentX >= subWorldSize.SizeX ||
                    currentY < 0 || currentY >= subWorldSize.SizeY ||
                    currentZ < 0 || currentZ >= subWorldSize.SizeZ)
                {
                    break;
                }
            }
        }
        
        /// <summary>
        /// 통로 구간 파기
        /// </summary>
        private static void CarveTunnelSegment(Block[,,] subWorldBlockData, SubWorldSize subWorldSize,
            int centerX, int centerY, int centerZ, float radius)
        {
            int intRadius = (int)CustomMathf.Ceil(radius);
            
            for (int x = -intRadius; x <= intRadius; x++)
            {
                for (int y = -intRadius; y <= intRadius; y++)
                {
                    for (int z = -intRadius; z <= intRadius; z++)
                    {
                        int worldX = centerX + x;
                        int worldY = centerY + y;
                        int worldZ = centerZ + z;
                        
                        if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                            worldY >= 0 && worldY < subWorldSize.SizeY &&
                            worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                        {
                            float distance = CustomMathf.Sqrt(x * x + y * y + z * z);
                            if (distance <= radius)
                            {
                                subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 소형 동굴 방들 생성
        /// </summary>
        private static void GenerateSmallCaves(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int smallCaveCount = Utilitys.RandomInteger(5, 12);
            
            for (int i = 0; i < smallCaveCount; i++)
            {
                int caveX = Utilitys.RandomInteger(5, subWorldSize.SizeX - 5);
                int caveY = Utilitys.RandomInteger(5, subWorldSize.SizeY - 5);
                int caveZ = Utilitys.RandomInteger(5, subWorldSize.SizeZ - 5);
                int caveRadius = Utilitys.RandomInteger(3, 6);
                
                // 전통적인 방법으로 소형 동굴 파기
                for (int x = -caveRadius; x <= caveRadius; x++)
                {
                    for (int y = -caveRadius; y <= caveRadius; y++)
                    {
                        for (int z = -caveRadius; z <= caveRadius; z++)
                        {
                            int worldX = caveX + x;
                            int worldY = caveY + y;
                            int worldZ = caveZ + z;
                            
                            if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                                worldY >= 0 && worldY < subWorldSize.SizeY &&
                                worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                            {
                                float distance = CustomMathf.Sqrt(x * x + y * y + z * z);
                                if (distance <= caveRadius - Utilitys.RandomFloat(0, 2))
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 지하 호수 생성
        /// </summary>
        private static void GenerateUndergroundLakes(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int lakeCount = Utilitys.RandomInteger(1, 3);
            
            for (int i = 0; i < lakeCount; i++)
            {
                int lakeX = Utilitys.RandomInteger(8, subWorldSize.SizeX - 8);
                int lakeY = Utilitys.RandomInteger(5, subWorldSize.SizeY / 2); // 낮은 고도에 생성
                int lakeZ = Utilitys.RandomInteger(8, subWorldSize.SizeZ - 8);
                
                int lakeWidth = Utilitys.RandomInteger(6, 12);
                int lakeDepth = Utilitys.RandomInteger(3, 6);
                
                // 호수 영역 파기 및 물 채우기
                for (int x = -lakeWidth / 2; x <= lakeWidth / 2; x++)
                {
                    for (int z = -lakeWidth / 2; z <= lakeWidth / 2; z++)
                    {
                        for (int y = 0; y < lakeDepth; y++)
                        {
                            int worldX = lakeX + x;
                            int worldY = lakeY + y;
                            int worldZ = lakeZ + z;
                            
                            if (worldX >= 0 && worldX < subWorldSize.SizeX &&
                                worldY >= 0 && worldY < subWorldSize.SizeY &&
                                worldZ >= 0 && worldZ < subWorldSize.SizeZ)
                            {
                                float distance = CustomMathf.Sqrt(x * x + z * z);
                                if (distance <= lakeWidth / 2)
                                {
                                    // 호수 바닥 생성
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                                    
                                    // 물 채우기
                                    if (y < lakeDepth - 1)
                                    {
                                        subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.WATER;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

}
