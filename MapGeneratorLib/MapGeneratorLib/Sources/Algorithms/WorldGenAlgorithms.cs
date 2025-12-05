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
        public static int GlobalRiverWaterLevel = 62;
        public static int HydrologySmoothIterations = 3;
        public static float HydrologySmoothBlend = 0.68f;
        public static int CaveStabilitySmoothIterations = 2;
        public static float CaveStabilitySmoothBlend = 0.6f;
        public static float HydrologyShorePush = 5f;
        public static float HydrologySlopePenalty = 6f;
        public static float HydrologyFlowGain = 0.5f;
        public static float HydrologyContinuityWeight = 0.35f;
        public static float HydrologyEdgeFlowBias = 0.35f;
        public static float HydrologyEdgeTangentWeight = 0.45f;
        public static float HydrologyEdgeFlowLockWeight = 0.38f;
        public static int HydrologyEdgeBlendRadius = 3;
        public static int HydrologyEdgeStabilityIterations = 1;
        public static float HydrologyEdgeStabilityWeight = 0.32f;
        public static float HydrologyEdgeVarianceClamp = 0.32f;
        public static float HydrologyWaterTableClampWeight = 0.42f;
        public static int HydrologyWaterTableClampRange = 18;
        public static float HydrologyWaterTableSlopeWeight = 0.55f;
        public static float HydrologyFlowPersistence = 0.68f;
        public static float HydrologyWarpFrequency = 0.0009f;
        public static float HydrologyWarpAmplitude = 9f;
        public static int HydrologySeamRelaxIterations = 2;
        public static float HydrologySeamRelaxBlend = 0.5f;
        public static float RiverBankErosionWeight = 0.18f;
        public static float LakeRimErosionWeight = 0.30f;
        public static float LakeSpawnWeightBias = 0.3f;
        public static float LakeShorelineBlend = 0.66f;
        public static float RiverNoiseScale = 0.015f;
        public static int RiverDepth = 6;
        public static int RiverIntensitySmoothIterations = 3;
        public static float RiverIntensitySmoothBlend = 0.58f;
        public static float RiverFlowAlignmentWeight = 0.28f;
        public static float RiverGradientPenalty = 0.42f;
        public static float RiverReliefPenaltyWeight = 0.25f;
        public static float CaveSupportDensity = 0.6f;
        public static float CaveHydrologyWeight = 0.45f;
        public static float CaveFlowWeight = 0.25f;
        public static float CaveRoughnessWeight = 0.1f;
        public static float CaveDepthWeight = 0.2f;
        public static float CaveRiverSuppressionWeight = 0.35f;
        public static float CaveSupportHydrationBias = 0.42f;
        public static float CaveSupportFlowBias = 0.20f;
        public static float LakeRiverProximitySuppression = 0.35f;

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

            GenerateRiverSystems(subWorldBlockData, subWorldSize);
            GenerateSurfaceLakes(subWorldBlockData, subWorldSize);

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
        
        private static int[,] BuildSurfaceHeightCache(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] cache = new int[subWorldSize.SizeX, subWorldSize.SizeZ];

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    cache[x, z] = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                }
            }

            return cache;
        }

        private static float[,] BuildHydrologyMask(SubWorldSize subWorldSize, int[,] surfaceCache)
        {
            float[,] hydrology = new float[subWorldSize.SizeX, subWorldSize.SizeZ];
            int minSurface = int.MaxValue;
            int maxSurface = int.MinValue;
            bool hasSurface = false;

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    hasSurface = true;
                    minSurface = CustomMathf.Min(minSurface, surface);
                    maxSurface = CustomMathf.Max(maxSurface, surface);
                }
            }

            if (!hasSurface)
            {
                return hydrology;
            }

            float invRange = 1f / CustomMathf.Max(1, maxSurface - minSurface);
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        hydrology[x, z] = 0f;
                        continue;
                    }

                    float slopeAccum = 0f;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= subWorldSize.SizeX || nz < 0 || nz >= subWorldSize.SizeZ)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            slopeAccum += CustomMathf.Abs(surface - neighborSurface);
                            neighborCount++;
                        }
                    }

                    float slope = neighborCount > 0 ? slopeAccum / neighborCount : 0f;
                    slope = CustomMathf.Clamp01(slope / 14f);

                    float heightNormalized = CustomMathf.Clamp01((surface - minSurface) * invRange);
                    float relief = 1f - heightNormalized;
                    float valley = CustomMathf.Clamp01((GlobalRiverWaterLevel - surface) / CustomMathf.Max(1f, HydrologyShorePush * 1.15f));
                    int edgeDistance = CustomMathf.Min(
                        CustomMathf.Min(x, z),
                        CustomMathf.Min(subWorldSize.SizeX - 1 - x, subWorldSize.SizeZ - 1 - z));
                    float edgeFalloff = 1f - CustomMathf.Clamp01(edgeDistance / (edgeRadius * 1.2f));

                    float warpX = Noise.GetNoise((x + 91.5f) * HydrologyWarpFrequency, 0, (z - 37.5f) * HydrologyWarpFrequency);
                    float warpZ = Noise.GetNoise((x - 41.5f) * HydrologyWarpFrequency * 1.35f, 0, (z + 73.5f) * HydrologyWarpFrequency * 1.35f);
                    float warpedX = x + warpX * HydrologyWarpAmplitude;
                    float warpedZ = z + warpZ * HydrologyWarpAmplitude;
                    float humidityFrequency = CustomMathf.Clamp(RiverNoiseScale * 0.65f, 0.0008f, 0.0065f);
                    float humidityBase = Noise.GetNoise((warpedX + 13.5f) * humidityFrequency, 0, (warpedZ - 71.5f) * humidityFrequency);
                    float humidityRipples = Noise.GetNoise((warpedX - 113.5f) * humidityFrequency * 1.9f, 0, (warpedZ + 21.5f) * humidityFrequency * 1.9f);
                    float humidity = 1f - CustomMathf.Abs((humidityBase * 0.65f + humidityRipples * 0.35f) - 0.5f) * (1.35f - 0.25f * HydrologyFlowPersistence);
                    humidity = CustomMathf.Clamp01(humidity);

                    float flowMemory = CustomMathf.Clamp01(HydrologyFlowPersistence);
                    float hydrologyValue = slope * (0.32f + 0.18f * flowMemory + 0.08f * edgeFalloff)
                        + valley * (0.34f + 0.12f * edgeFalloff)
                        + relief * 0.12f
                        + humidity * (0.22f + 0.08f * flowMemory)
                        + flowMemory * 0.05f;

                    hydrology[x, z] = CustomMathf.Clamp01(hydrologyValue);
                }
            }

            return hydrology;
        }

        private static float[,] BuildFlowAccumulation(int[,] surfaceCache, SubWorldSize subWorldSize)
        {
            float[,] rawAccumulation = new float[subWorldSize.SizeX, subWorldSize.SizeZ];

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    float contribution = 0f;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= subWorldSize.SizeX || nz < 0 || nz >= subWorldSize.SizeZ)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            int heightDelta = neighborSurface - surface;
                            if (heightDelta <= 1)
                            {
                                continue;
                            }

                            float weight = 1f + CustomMathf.Min(6f, heightDelta) * 0.15f;
                            if (dx != 0 && dz != 0)
                            {
                                weight *= 0.65f;
                            }

                            contribution += weight;
                        }
                    }

                    rawAccumulation[x, z] = contribution;
                }
            }

            float[,] smoothed = new float[subWorldSize.SizeX, subWorldSize.SizeZ];
            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    float total = rawAccumulation[x, z];
                    int samples = 1;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= subWorldSize.SizeX || nz < 0 || nz >= subWorldSize.SizeZ)
                            {
                                continue;
                            }

                            total += rawAccumulation[nx, nz] * 0.5f;
                            samples++;
                        }
                    }

                    smoothed[x, z] = samples > 0 ? total / samples : rawAccumulation[x, z];
                }
            }

            return smoothed;
        }

        private static void StabilizeHydrologyGradients(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation, int[,] surfaceCache)
        {
            int width = subWorldSize.SizeX;
            int depth = subWorldSize.SizeZ;
            float[,] hydrologyBuffer = new float[width, depth];
            float[,] flowBuffer = new float[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    float flow = flowAccumulation[x, z];
                    float blendedHydrology = hydrology;
                    float blendedFlow = flow;
                    float weight = 1f;

                    int surface = surfaceCache[x, z];
                    float shoreBias = CustomMathf.Clamp01((GlobalRiverWaterLevel - surface) / CustomMathf.Max(0.001f, HydrologyShorePush));
                    shoreBias = CustomMathf.Max(0.1f, shoreBias * 0.6f);

                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                        {
                            if (offsetX == 0 && offsetZ == 0)
                            {
                                continue;
                            }

                            int sampleX = x + offsetX;
                            int sampleZ = z + offsetZ;
                            if (WorldGenerateUtils.CheckSubWorldBoundary(sampleX, 0, sampleZ, subWorldSize) == false)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[sampleX, sampleZ];
                            float slopePenalty = CustomMathf.Clamp01(CustomMathf.Abs(surface - neighborSurface) / CustomMathf.Max(0.001f, HydrologySlopePenalty));
                            float smoothingWeight = 1f - slopePenalty * 0.45f;

                            blendedHydrology += hydrologyMask[sampleX, sampleZ] * smoothingWeight;
                            blendedFlow += flowAccumulation[sampleX, sampleZ] * smoothingWeight;
                            weight += smoothingWeight;
                        }
                    }

                    float hydrologyBlend = CustomMathf.Clamp01(0.35f + shoreBias * HydrologyFlowGain);
                    hydrologyBlend = CustomMathf.Clamp01(hydrologyBlend + HydrologyFlowPersistence * 0.1f);
                    float flowBlend = CustomMathf.Clamp01(0.25f + shoreBias * HydrologyFlowGain * 0.65f + HydrologyFlowPersistence * 0.15f);
                    hydrologyBuffer[x, z] = CustomMathf.Clamp01(CustomMathf.Lerp(hydrology, blendedHydrology / weight, hydrologyBlend));
                    flowBuffer[x, z] = CustomMathf.Max(0f, CustomMathf.Lerp(flow, blendedFlow / weight, flowBlend));
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    hydrologyMask[x, z] = hydrologyBuffer[x, z];
                    flowAccumulation[x, z] = flowBuffer[x, z];
                }
            }
        }

        private static void SmoothHydrologyFields(float[,] hydrologyMask, float[,] flowAccumulation)
        {
            if (HydrologySmoothIterations <= 0 || HydrologySmoothBlend <= 0f)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var hydroBuffer = new float[width, depth];
            var flowBuffer = new float[width, depth];
            float baseBlend = CustomMathf.Clamp01(HydrologySmoothBlend);
            float anisotropy = CustomMathf.Clamp01(0.3f + HydrologyFlowPersistence * 0.55f + HydrologyContinuityWeight * 0.25f);

            for (int iteration = 0; iteration < HydrologySmoothIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float hydrology = hydrologyMask[x, z];
                        float flow = flowAccumulation[x, z];
                        float weightedHydrology = hydrology;
                        float weightedFlow = flow;
                        float weightTotal = 1f;
                        var gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        CustomVector2 downhill = gradient.sqrMagnitude > CustomVector2.kEpsilon ? gradient * -1f : CustomVector2.zero;
                        if (downhill.sqrMagnitude > CustomVector2.kEpsilon)
                        {
                            downhill.Normalize();
                        }
                        float maxAlignment = 0f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                float neighborHydrology = hydrologyMask[nx, nz];
                                float neighborFlow = flowAccumulation[nx, nz];

                                var neighborDir = new CustomVector2(dx, dz);
                                if (neighborDir.sqrMagnitude > CustomVector2.kEpsilon)
                                {
                                    neighborDir.Normalize();
                                }

                                float alignment = downhill.sqrMagnitude <= CustomVector2.kEpsilon ? 0f : CustomMathf.Max(0f, CustomVector2.Dot(downhill, neighborDir));
                                maxAlignment = CustomMathf.Max(maxAlignment, alignment);

                                float gradientDelta = CustomMathf.Abs(hydrology - neighborHydrology);
                                float gradientWeight = CustomMathf.Clamp(1f - gradientDelta * (0.45f + HydrologyContinuityWeight * 0.35f), 0.25f, 1f);
                                float continuityWeight = 1f + HydrologyContinuityWeight * 0.35f;
                                float alignmentWeight = 1f + alignment * (0.8f + anisotropy * 0.6f);
                                float baseWeight = 1f + hydrology * 0.3f + neighborHydrology * 0.35f + flow * 0.1f + neighborFlow * 0.1f;
                                float finalWeight = baseWeight * alignmentWeight * gradientWeight * continuityWeight;

                                weightedHydrology += neighborHydrology * finalWeight;
                                weightedFlow += neighborFlow * finalWeight * (1f + alignment * 0.5f);
                                weightTotal += finalWeight;
                            }
                        }

                        float hydroTarget = weightTotal > 0f ? weightedHydrology / weightTotal : hydrology;
                        float flowTarget = weightTotal > 0f ? weightedFlow / weightTotal : flow;
                        float blend = CustomMathf.Clamp(baseBlend + hydrology * 0.12f + maxAlignment * 0.18f + HydrologyFlowPersistence * 0.08f, 0f, 0.95f);
                        hydroBuffer[x, z] = CustomMathf.Lerp(hydrology, hydroTarget, blend);
                        flowBuffer[x, z] = CustomMathf.Max(0f, CustomMathf.Lerp(flow, flowTarget, blend));
                    }
                }

                Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
            }
        }

        private static void RelaxHydrologySeams(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            if (HydrologySeamRelaxIterations <= 0 || HydrologySeamRelaxBlend <= 0f || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = subWorldSize.SizeX;
            int depth = subWorldSize.SizeZ;
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);
            float flowPersistence = CustomMathf.Clamp01(HydrologyFlowPersistence);
            float baseBlend = CustomMathf.Clamp01(HydrologySeamRelaxBlend);
            var hydroBuffer = new float[width, depth];
            var flowBuffer = new float[width, depth];

            for (int iteration = 0; iteration < HydrologySeamRelaxIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        bool hasFlowDir = flowDir.sqrMagnitude > CustomVector2.kEpsilon;

                        int edgeDistance = CustomMathf.Min(CustomMathf.Min(x, z), CustomMathf.Min(width - 1 - x, depth - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            hydroBuffer[x, z] = hydrologyMask[x, z];
                            flowBuffer[x, z] = flowAccumulation[x, z];
                            continue;
                        }

                        float falloff = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                        float blend = baseBlend * falloff;
                        float weightedHydro = hydrologyMask[x, z] * 1.25f;
                        float weightedFlow = flowAccumulation[x, z] * (0.85f + flowPersistence * 0.25f);
                        float weight = 1.25f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                float distance = CustomMathf.Abs(dx) + CustomMathf.Abs(dz);
                                float neighborWeight = 1f - distance * 0.15f;
                                float continuity = CustomMathf.Clamp01(HydrologyContinuityWeight + falloff * 0.2f);
                                neighborWeight *= 0.85f + continuity * 0.35f;

                                if (hasFlowDir)
                                {
                                    var neighborDir = new CustomVector2(dx, dz);
                                    if (neighborDir.sqrMagnitude > CustomVector2.kEpsilon)
                                    {
                                        neighborDir.Normalize();
                                        float alignment = CustomMathf.Max(0f, CustomVector2.Dot(flowDir, neighborDir));
                                        float flowWeight = 1f + HydrologyEdgeFlowBias * alignment;
                                        neighborWeight *= flowWeight;
                                    }
                                }

                                weightedHydro += hydrologyMask[nx, nz] * neighborWeight;
                                weightedFlow += flowAccumulation[nx, nz] * neighborWeight * (0.8f + flowPersistence * 0.35f);
                                weight += neighborWeight;
                            }
                        }

                        float averagedHydro = weight > 0f ? weightedHydro / weight : hydrologyMask[x, z];
                        float averagedFlow = weight > 0f ? weightedFlow / weight : flowAccumulation[x, z];
                        float hydroBlend = CustomMathf.Clamp01(blend * (0.75f + flowPersistence * 0.25f));
                        float flowBlend = CustomMathf.Clamp01(blend * (0.6f + flowPersistence * 0.35f));

                        hydroBuffer[x, z] = CustomMathf.Clamp01(CustomMathf.Lerp(hydrologyMask[x, z], averagedHydro, hydroBlend));
                        flowBuffer[x, z] = CustomMathf.Max(0f, CustomMathf.Lerp(flowAccumulation[x, z], averagedFlow, flowBlend));
                    }
                }

                Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
            }
        }

        private static void AnchorHydrologyToSlope(SubWorldSize subWorldSize, int[,] surfaceCache, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            if (surfaceCache == null || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = subWorldSize.SizeX;
            int depth = subWorldSize.SizeZ;
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);
            float flowPersistence = CustomMathf.Clamp01(HydrologyFlowPersistence);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = CustomMathf.Min(CustomMathf.Min(x, z), CustomMathf.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    CustomVector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    if (slopeDir.sqrMagnitude <= CustomVector2.kEpsilon)
                    {
                        continue;
                    }

                    int step = CustomMathf.Max(1, edgeRadius - edgeDistance + 1);
                    int anchorX = CustomMathf.Clamp(x + CustomMathf.RoundToInt(slopeDir.x * step), 1, width - 2);
                    int anchorZ = CustomMathf.Clamp(z + CustomMathf.RoundToInt(slopeDir.y * step), 1, depth - 2);
                    float heightDelta = CustomMathf.Abs(surfaceCache[x, z] - surfaceCache[anchorX, anchorZ]);
                    float slopeStrength = CustomMathf.Clamp01(heightDelta / 12f);
                    float edgeWeight = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                    float blend = CustomMathf.Clamp01(edgeWeight * (0.25f + slopeStrength * 0.35f) * (0.7f + flowPersistence * 0.25f));

                    float anchorHydro = hydrologyMask[anchorX, anchorZ];
                    float anchorFlow = flowAccumulation[anchorX, anchorZ];

                    float anchoredHydro = CustomMathf.Clamp01(CustomMathf.Lerp(hydrologyMask[x, z], anchorHydro, blend));
                    float anchoredFlow = CustomMathf.Clamp(flowAccumulation[x, z] * (1f - blend) + anchorFlow * blend, 0f, 8f);

                    var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    if (flowDir.sqrMagnitude > CustomVector2.kEpsilon && HydrologyEdgeFlowLockWeight > 0f)
                    {
                        flowDir.Normalize();
                        int flowStep = CustomMathf.Max(1, edgeRadius - edgeDistance + 1);
                        int flowX = CustomMathf.Clamp(x + CustomMathf.RoundToInt(flowDir.x * flowStep), 1, width - 2);
                        int flowZ = CustomMathf.Clamp(z + CustomMathf.RoundToInt(flowDir.y * flowStep), 1, depth - 2);
                        float flowHydro = hydrologyMask[flowX, flowZ];
                        float flowFlow = flowAccumulation[flowX, flowZ];
                        var downstreamDir = ComputeHydrologyGradientVector(hydrologyMask, flowX, flowZ);
                        float alignment = 0f;
                        if (downstreamDir.sqrMagnitude > CustomVector2.kEpsilon)
                        {
                            downstreamDir.Normalize();
                            alignment = CustomVector2.Dot(flowDir, downstreamDir);
                        }

                        float alignmentStrength = CustomMathf.Clamp01(0.6f + CustomMathf.Max(0f, alignment) * 0.4f);
                        float flowBlend = CustomMathf.Clamp01(edgeWeight * HydrologyEdgeFlowLockWeight * alignmentStrength * (0.55f + flowPersistence * 0.35f));
                        float targetHydro = (anchoredHydro + flowHydro) * 0.5f;
                        float targetFlow = (anchoredFlow + flowFlow) * 0.5f;
                        anchoredHydro = CustomMathf.Lerp(anchoredHydro, targetHydro, flowBlend);
                        anchoredFlow = CustomMathf.Lerp(anchoredFlow, targetFlow, flowBlend);
                    }

                    CustomVector2 tangentDir = ComputeHydrologyTangentVector(hydrologyMask, x, z);
                    if (tangentDir.sqrMagnitude > CustomVector2.kEpsilon && HydrologyEdgeTangentWeight > 0f)
                    {
                        tangentDir.Normalize();
                        int tangentStepX = tangentDir.x >= 0f ? 1 : -1;
                        int tangentStepZ = tangentDir.y >= 0f ? 1 : -1;
                        int tangentX = CustomMathf.Clamp(x + tangentStepX, 1, width - 2);
                        int tangentZ = CustomMathf.Clamp(z + tangentStepZ, 1, depth - 2);
                        float tangentHydro = hydrologyMask[tangentX, tangentZ];
                        float tangentFlow = flowAccumulation[tangentX, tangentZ];
                        float tangentBlend = CustomMathf.Clamp01(edgeWeight * HydrologyEdgeTangentWeight * (0.55f + slopeStrength * 0.35f));
                        float targetHydro = (anchoredHydro + tangentHydro) * 0.5f;
                        float targetFlow = (anchoredFlow + tangentFlow) * 0.5f;
                        anchoredHydro = CustomMathf.Clamp01(CustomMathf.Lerp(anchoredHydro, targetHydro, tangentBlend));
                        anchoredFlow = CustomMathf.Clamp(anchoredFlow * (1f - tangentBlend) + targetFlow * tangentBlend, 0f, 8f);
                    }

                    hydrologyMask[x, z] = anchoredHydro;
                    flowAccumulation[x, z] = anchoredFlow;
                }
            }
        }

        private static void SmoothScalarField(float[,] field, int iterations, float blend)
        {
            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            var scratch = new float[width, depth];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float weightedSum = field[x, z];
                        float weightTotal = 1f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                float weight = (dx == 0 && dz == 0) ? 1.5f : 1f;
                                weightedSum += field[nx, nz] * weight;
                                weightTotal += weight;
                            }
                        }

                        float average = weightTotal > 0f ? weightedSum / weightTotal : field[x, z];
                        scratch[x, z] = field[x, z] * (1f - blend) + average * blend;
                    }
                }

                Array.Copy(scratch, field, field.Length);
            }
        }

        private static float[,] BuildErosionRiskField(SubWorldSize subWorldSize, int[,] surfaceCache, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            float[,] risk = new float[subWorldSize.SizeX, subWorldSize.SizeZ];
            float surfaceRange = CustomMathf.Max(1, subWorldSize.SizeY);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        risk[x, z] = 0f;
                        continue;
                    }

                    float slope = ComputeLocalRelief(surfaceCache, subWorldSize, x, z, 3);
                    float slopeNorm = CustomMathf.Clamp01(slope / 10f);
                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float altitude = CustomMathf.Clamp01(surface / surfaceRange);
                    float valley = CustomMathf.Clamp01((GlobalRiverWaterLevel - surface) / 16f);
                    float exposure = CustomMathf.Clamp01((1f - altitude) * 0.65f + valley * 0.45f);

                    float combined = hydrology * 0.4f + flow * 0.28f + exposure * 0.2f + slopeNorm * 0.15f;
                    risk[x, z] = CustomMathf.Clamp01(combined);
                }
            }

            SmoothScalarField(risk, HydrologySmoothIterations, HydrologySmoothBlend);
            return risk;
        }

        private static float ComputeInteriorAverage(float[,] field, int edgeRadius)
        {
            int width = field.GetLength(0);
            int depth = field.GetLength(1);

            float sum = 0f;
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = CustomMathf.Min(
                        CustomMathf.Min(x, z),
                        CustomMathf.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance <= edgeRadius)
                    {
                        continue;
                    }

                    sum += field[x, z];
                    count++;
                }
            }

            if (count == 0)
            {
                return 0f;
            }

            return sum / count;
        }

        private static float ClampEdgeVariance(float value, float anchor, float clampFraction, float absoluteFloor = 0.02f)
        {
            float maxDelta = CustomMathf.Max(absoluteFloor, CustomMathf.Abs(anchor) * clampFraction);
            float delta = value - anchor;
            if (CustomMathf.Abs(delta) <= maxDelta)
            {
                return value;
            }

            float clamped = anchor + (delta >= 0f ? maxDelta : -maxDelta);
            return clamped;
        }

        private static void BlendHydrologySeams(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            int maxX = subWorldSize.SizeX - 1;
            int maxZ = subWorldSize.SizeZ - 1;
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);
            int sampleRadius = CustomMathf.Max(1, edgeRadius);
            float flowPersistence = CustomMathf.Clamp01(HydrologyFlowPersistence);
            float interiorHydro = ComputeInteriorAverage(hydrologyMask, edgeRadius);
            float interiorFlow = ComputeInteriorAverage(flowAccumulation, edgeRadius);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    bool hasFlowDir = flowDir.sqrMagnitude > CustomVector2.kEpsilon;

                    int edgeDistance = CustomMathf.Min(
                        CustomMathf.Min(x, z),
                        CustomMathf.Min(maxX - x, maxZ - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    float falloff = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                    float continuity = CustomMathf.Clamp01(HydrologyContinuityWeight + falloff * 0.25f);
                    float blendWeight = CustomMathf.Clamp01(continuity * falloff);
                    float ringBlend = CustomMathf.Clamp01(blendWeight * (0.85f + falloff * 0.25f));
                    float neighborHydrologySum = 0f;
                    float neighborFlowSum = 0f;
                    float neighborWeightTotal = 0f;

                    for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
                    {
                        for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int neighborX = CustomMathf.Clamp(x + dx, 0, maxX);
                            int neighborZ = CustomMathf.Clamp(z + dz, 0, maxZ);
                            float radialDistance = CustomMathf.Sqrt(dx * dx + dz * dz);
                            float ringFalloff = 1f - CustomMathf.Clamp01((radialDistance - 1f) / CustomMathf.Max(1f, sampleRadius - 0.75f));
                            float manhattan = CustomMathf.Abs(dx) + CustomMathf.Abs(dz);
                            float neighborWeight = CustomMathf.Max(0f, (1.15f - manhattan * 0.18f) * ringFalloff);
                            float continuityBias = CustomMathf.Clamp01(0.82f + continuity * 0.4f);
                            neighborWeight *= continuityBias * ringFalloff;
                            if (neighborWeight <= 0f)
                            {
                                continue;
                            }

                            if (hasFlowDir)
                            {
                                var neighborDir = new CustomVector2(dx, dz);
                                if (neighborDir.sqrMagnitude > CustomVector2.kEpsilon)
                                {
                                    neighborDir.Normalize();
                                    float alignment = CustomMathf.Max(0f, CustomVector2.Dot(flowDir, neighborDir));
                                    float flowWeight = 1f + HydrologyEdgeFlowBias * alignment;
                                    neighborWeight *= flowWeight;
                                }
                            }

                            float flowBias = 0.88f + flowPersistence * 0.35f;
                            neighborHydrologySum += hydrologyMask[neighborX, neighborZ] * neighborWeight;
                            neighborFlowSum += flowAccumulation[neighborX, neighborZ] * neighborWeight * flowBias;
                            neighborWeightTotal += neighborWeight;
                        }
                    }

                    float neighborHydrology = neighborWeightTotal > 0f
                        ? neighborHydrologySum / neighborWeightTotal
                        : hydrologyMask[x, z];
                    float neighborFlow = neighborWeightTotal > 0f
                        ? neighborFlowSum / neighborWeightTotal
                        : flowAccumulation[x, z];
                    float anchorNoise = Noise.GetNoise((x + 19.5f) / 72f, 0, (z - 11.5f) / 72f);
                    float anchorHydrology = CustomMathf.Clamp01(0.55f + (anchorNoise - 0.5f) * 0.9f + falloff * 0.05f);
                    float baseHydrology = (hydrologyMask[x, z] * (2.2f + falloff * 0.4f) + neighborHydrology * (1.6f + falloff * 0.5f) + anchorHydrology * (0.45f + falloff * 0.15f)) / (4.25f + falloff * 1.05f);
                    float blendedHydrology = hydrologyMask[x, z] * (1f - ringBlend) + baseHydrology * ringBlend;
                    blendedHydrology = ClampEdgeVariance(blendedHydrology, interiorHydro, HydrologyEdgeVarianceClamp);
                    hydrologyMask[x, z] = CustomMathf.Clamp01(blendedHydrology);

                    float anchorFlow = CustomMathf.Clamp(anchorHydrology * 0.9f + CustomMathf.Abs(anchorNoise - 0.5f) * 1.35f, 0f, 8f);
                    float baseFlow = (flowAccumulation[x, z] * CustomMathf.Lerp(1.1f, 1.55f, flowPersistence) + neighborFlow * (0.85f + 0.15f * flowPersistence) + anchorFlow * (0.45f + falloff * 0.1f)) / (2.4f + flowPersistence * 0.35f + falloff * 0.1f);
                    float blendedFlow = flowAccumulation[x, z] * (1f - ringBlend) + baseFlow * ringBlend;
                    blendedFlow = ClampEdgeVariance(blendedFlow, interiorFlow, HydrologyEdgeVarianceClamp * 1.25f, 0.05f);
                    flowAccumulation[x, z] = CustomMathf.Clamp(blendedFlow, 0f, 8f);
                }
            }
        }

        private static void EnforceHydrologyEdgeConsistency(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            if (hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = subWorldSize.SizeX;
            int depth = subWorldSize.SizeZ;
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);
            float interiorHydro = ComputeInteriorAverage(hydrologyMask, edgeRadius);
            float interiorFlow = ComputeInteriorAverage(flowAccumulation, edgeRadius);
            float flowPersistence = CustomMathf.Clamp01(HydrologyFlowPersistence);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = CustomMathf.Min(CustomMathf.Min(x, z), CustomMathf.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    float falloff = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                    float targetHydro = ClampEdgeVariance(hydrologyMask[x, z], interiorHydro, HydrologyEdgeVarianceClamp);
                    float targetFlow = ClampEdgeVariance(flowAccumulation[x, z], interiorFlow, HydrologyEdgeVarianceClamp * 1.25f, 0.05f);
                    float blend = CustomMathf.Clamp01(0.35f + falloff * (0.35f + HydrologyFlowPersistence * 0.1f));

                    hydrologyMask[x, z] = CustomMathf.Clamp01(CustomMathf.Lerp(hydrologyMask[x, z], targetHydro, blend));
                    flowAccumulation[x, z] = CustomMathf.Max(0f, CustomMathf.Lerp(flowAccumulation[x, z], targetFlow, blend));
                }
            }

            if (HydrologyEdgeStabilityIterations > 0 && HydrologyEdgeStabilityWeight > 0f)
            {
                var hydroBuffer = new float[width, depth];
                var flowBuffer = new float[width, depth];
                float stabilityWeight = CustomMathf.Clamp01(HydrologyEdgeStabilityWeight);

                for (int iteration = 0; iteration < HydrologyEdgeStabilityIterations; iteration++)
                {
                    interiorHydro = ComputeInteriorAverage(hydrologyMask, edgeRadius);
                    interiorFlow = ComputeInteriorAverage(flowAccumulation, edgeRadius);

                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int edgeDistance = CustomMathf.Min(CustomMathf.Min(x, z), CustomMathf.Min(width - 1 - x, depth - 1 - z));
                            if (edgeDistance > edgeRadius)
                            {
                                hydroBuffer[x, z] = hydrologyMask[x, z];
                                flowBuffer[x, z] = flowAccumulation[x, z];
                                continue;
                            }

                            float falloff = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                            var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                            bool hasFlowDir = flowDir.sqrMagnitude > CustomVector2.kEpsilon;
                            int sampleX = x;
                            int sampleZ = z;
                            if (hasFlowDir)
                            {
                                sampleX = CustomMathf.Clamp(x + (flowDir.x >= 0f ? 2 : -2), 0, width - 1);
                                sampleZ = CustomMathf.Clamp(z + (flowDir.y >= 0f ? 2 : -2), 0, depth - 1);
                            }

                            float alongFlowHydro = hydrologyMask[sampleX, sampleZ];
                            float alongFlow = flowAccumulation[sampleX, sampleZ];
                            float continuity = CustomMathf.Clamp01(HydrologyContinuityWeight + falloff * 0.2f);
                            float targetHydro = (hydrologyMask[x, z] * 0.9f + interiorHydro * (0.75f + continuity * 0.25f) + alongFlowHydro * (0.55f + flowPersistence * 0.25f)) / (2.2f + continuity * 0.25f + flowPersistence * 0.25f);
                            float targetFlow = (flowAccumulation[x, z] * (0.85f + flowPersistence * 0.2f) + interiorFlow * (0.6f + flowPersistence * 0.3f) + alongFlow * (0.65f + continuity * 0.2f)) / (2.1f + flowPersistence * 0.5f + continuity * 0.2f);
                            float blend = CustomMathf.Clamp01(stabilityWeight * falloff * (0.6f + flowPersistence * 0.35f));

                            hydroBuffer[x, z] = CustomMathf.Clamp01(hydrologyMask[x, z] * (1f - blend) + targetHydro * blend);
                            flowBuffer[x, z] = CustomMathf.Max(0f, flowAccumulation[x, z] * (1f - blend * 0.55f) + targetFlow * (blend * 0.55f));
                        }
                    }

                    Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                    Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
                }
            }
        }

        private static void NormalizeHydrologyRange(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            if (hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0f;
            int count = 0;

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    float value = hydrologyMask[x, z];
                    min = CustomMathf.Min(min, value);
                    max = CustomMathf.Max(max, value);
                    sum += value;
                    count++;
                }
            }

            if (count == 0 || max <= min + float.Epsilon)
            {
                return;
            }

            float avg = sum / count;
            float invRange = 1f / CustomMathf.Max(0.0001f, max - min);
            float avgNorm = CustomMathf.Clamp01((avg - min) * invRange);
            float flowPersistence = CustomMathf.Clamp01(HydrologyFlowPersistence);
            int edgeRadius = CustomMathf.Max(1, HydrologyEdgeBlendRadius);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    float normalized = CustomMathf.Clamp01((hydrologyMask[x, z] - min) * invRange);
                    float flowNorm = CustomMathf.Clamp(flowAccumulation[x, z] / (6f + 6f * flowPersistence), 0f, 1f);
                    int edgeDistance = CustomMathf.Min(
                        CustomMathf.Min(x, z),
                        CustomMathf.Min(subWorldSize.SizeX - 1 - x, subWorldSize.SizeZ - 1 - z));
                    float edgeBlend = 1f - CustomMathf.Clamp01(edgeDistance / CustomMathf.Max(1f, (float)edgeRadius));
                    float continuity = CustomMathf.Clamp01(HydrologyContinuityWeight + edgeBlend * 0.2f);
                    float baseline = normalized * (1f - continuity) + avgNorm * continuity;
                    float flowBias = flowNorm * (0.35f + 0.3f * edgeBlend) * flowPersistence;
                    hydrologyMask[x, z] = CustomMathf.Clamp01(baseline + flowBias);
                }
            }
        }

        private static void ClampHydrologyToWaterTable(SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation, int[,] surfaceCache)
        {
            if (HydrologyWaterTableClampWeight <= 0f || HydrologyWaterTableClampRange <= 0 || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            float weight = CustomMathf.Clamp01(HydrologyWaterTableClampWeight);
            float invRange = 1f / CustomMathf.Max(1f, (float)HydrologyWaterTableClampRange);
            float flowBlendScale = 0.65f;
            float slopeWeight = CustomMathf.Clamp01(HydrologyWaterTableSlopeWeight);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    float delta = CustomMathf.Abs(GlobalRiverWaterLevel - surface);
                    float proximity = 1f - CustomMathf.Clamp01(delta * invRange);
                    if (proximity <= 0f)
                    {
                        continue;
                    }

                    float slopeFactor = ComputeWaterTableSlopeFactor(subWorldSize, surfaceCache, x, z);
                    float slopeAttenuation = CustomMathf.Clamp(1f - slopeFactor * slopeWeight, 0.25f, 1f);
                    float blend = weight * proximity * slopeAttenuation;
                    if (blend <= 0f)
                    {
                        continue;
                    }

                    float valleyBias = CustomMathf.Clamp((GlobalRiverWaterLevel - surface) / CustomMathf.Max(1f, HydrologyShorePush * 1.15f), -1f, 1f);
                    float hydroBoost = CustomMathf.Max(0.05f, 0.25f - slopeFactor * slopeWeight * 0.12f);
                    float targetHydro = CustomMathf.Clamp01(
                        hydrologyMask[x, z]
                        + hydroBoost * proximity
                        + CustomMathf.Max(0f, valleyBias) * (0.18f * slopeAttenuation));

                    float flowBoost = CustomMathf.Max(0.05f, 0.35f - slopeFactor * slopeWeight * 0.2f);
                    float targetFlow = CustomMathf.Clamp01(flowAccumulation[x, z] + flowBoost * proximity);
                    float flowBlend = flowBlendScale * (0.55f + slopeAttenuation * 0.45f);

                    hydrologyMask[x, z] = CustomMathf.Clamp01(hydrologyMask[x, z] * (1f - blend) + targetHydro * blend);
                    flowAccumulation[x, z] = CustomMathf.Max(0f, flowAccumulation[x, z] * (1f - blend * flowBlend) + targetFlow * (blend * flowBlend));
                }
            }
        }

        private static float ComputeWaterTableSlopeFactor(SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z)
        {
            int leftIndex = CustomMathf.Max(0, x - 1);
            int rightIndex = CustomMathf.Min(subWorldSize.SizeX - 1, x + 1);
            int backIndex = CustomMathf.Max(0, z - 1);
            int forwardIndex = CustomMathf.Min(subWorldSize.SizeZ - 1, z + 1);

            float gradientX = CustomMathf.Abs(surfaceCache[rightIndex, z] - surfaceCache[leftIndex, z]) * 0.5f;
            float gradientZ = CustomMathf.Abs(surfaceCache[x, forwardIndex] - surfaceCache[x, backIndex]) * 0.5f;
            float slope = CustomMathf.Sqrt(gradientX * gradientX + gradientZ * gradientZ);

            return CustomMathf.Clamp01(slope / CustomMathf.Max(1f, HydrologyShorePush * 0.9f));
        }

        private static float[,] BuildRiparianSaturationMap(SubWorldSize subWorldSize, int[,] surfaceCache, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            float[,] riparian = new float[subWorldSize.SizeX, subWorldSize.SizeZ];
            int maxHeight = CustomMathf.Max(1, subWorldSize.SizeY - 1);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        riparian[x, z] = 0f;
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float accumulation = CustomMathf.Clamp01(flowAccumulation[x, z] / 8f);
                    float relief = ComputeLocalRelief(surfaceCache, subWorldSize, x, z, 2);
                    float slopePenalty = CustomMathf.Clamp01(relief / 10f);
                    float altitude = CustomMathf.Clamp01(surface / (float)maxHeight);
                    float valleyBias = CustomMathf.Clamp01((GlobalRiverWaterLevel - surface) / 14f);
                    float lowlandBias = 1f - altitude;
                    float moisture = hydrology * 0.55f + accumulation * 0.3f + lowlandBias * 0.1f + valleyBias * 0.25f;
                    float erosionResilience = CustomMathf.Clamp01(1f - slopePenalty * 0.5f);
                    riparian[x, z] = CustomMathf.Clamp01(moisture * erosionResilience);
                }
            }

            return riparian;
        }

        private static CustomVector2 ComputeTerrainSlopeDirection(int[,] surfaceCache, SubWorldSize subWorldSize, int x, int z)
        {
            int leftIndex = CustomMathf.Clamp(x - 1, 0, subWorldSize.SizeX - 1);
            int rightIndex = CustomMathf.Clamp(x + 1, 0, subWorldSize.SizeX - 1);
            int backIndex = CustomMathf.Clamp(z - 1, 0, subWorldSize.SizeZ - 1);
            int forwardIndex = CustomMathf.Clamp(z + 1, 0, subWorldSize.SizeZ - 1);

            float dx = surfaceCache[rightIndex, z] - surfaceCache[leftIndex, z];
            float dz = surfaceCache[x, forwardIndex] - surfaceCache[x, backIndex];

            CustomVector2 slope = new CustomVector2(-dx, -dz);
            if (slope.sqrMagnitude < 0.0001f)
            {
                return CustomVector2.zero;
            }

            slope.Normalize();
            return slope;
        }

        private static float AdjustRiverMask(float baseMask, float hydrologyBias)
        {
            float clampedHydrology = CustomMathf.Clamp01(hydrologyBias);
            float scaled = baseMask * (1f - clampedHydrology * 0.55f) - clampedHydrology * 0.01f;
            return CustomMathf.Max(0f, scaled);
        }

        private static float ComputeRiverReliefScale(int[,] surfaceCache, SubWorldSize subWorldSize, int x, int z)
        {
            float weight = CustomMathf.Max(0f, RiverReliefPenaltyWeight);
            if (weight <= 0f)
            {
                return 1f;
            }

            float reliefPenalty = CustomMathf.Clamp01(ComputeLocalRelief(surfaceCache, subWorldSize, x, z, 2) / 8f);
            return CustomMathf.Clamp01(1f - reliefPenalty * weight);
        }

        private static CustomVector2 ComputeRiverFlowDirection(float sampleX, float sampleZ)
        {
            float baseFrequency = CustomMathf.Max(0.0001f, RiverNoiseScale * 1.25f);
            float gradientStep = CustomMathf.Clamp(baseFrequency * 0.68f, 0.0015f, 0.05f);

            float forwardX = Noise.GetNoise(sampleX + gradientStep, 0, sampleZ);
            float backwardX = Noise.GetNoise(sampleX - gradientStep, 0, sampleZ);
            float forwardZ = Noise.GetNoise(sampleX, 0, sampleZ + gradientStep);
            float backwardZ = Noise.GetNoise(sampleX, 0, sampleZ - gradientStep);

            CustomVector2 flow = new CustomVector2(forwardX - backwardX, forwardZ - backwardZ);
            if (flow.sqrMagnitude < 0.0001f)
            {
                flow = new CustomVector2(
                    Noise.GetNoise(sampleX + 37.31f, 0, sampleZ + 13.73f) - 0.5f,
                    Noise.GetNoise(sampleX - 42.11f, 0, sampleZ - 24.19f) - 0.5f);
            }

            if (flow.sqrMagnitude < CustomVector2.kEpsilon)
            {
                return CustomVector2.right;
            }

            flow.Normalize();
            return flow;
        }

        private static float EvaluateRiverIntensity(int x, int z, out CustomVector2 flowDir)
        {
            float baseFrequency = CustomMathf.Max(0.0001f, RiverNoiseScale * 1.25f);
            float sampleScale = 1f / baseFrequency;
            float warpFrequency = baseFrequency * 0.58f;
            float warpScale = 1f / CustomMathf.Max(0.0001f, warpFrequency);
            float warpStrength = CustomMathf.Max(2.5f, 5.25f * (RiverDepth / 5f));

            float warpX = Noise.GetNoise((x + 321.37f) / warpScale, 0, (z - 811.19f) / warpScale);
            float warpZ = Noise.GetNoise((x - 217.91f) / warpScale, 0, (z + 607.53f) / warpScale);

            float sampleX = (x + warpX * warpStrength) / sampleScale;
            float sampleZ = (z + warpZ * warpStrength) / sampleScale;

            float baseNoise = Noise.GetNoise(sampleX, 0, sampleZ);
            float detailNoise = Noise.GetNoise(sampleX * 0.35f, 0, sampleZ * 0.35f);
            float ridgeNoise = Noise.GetNoise(sampleX * 1.6f, 0, sampleZ * 1.6f);

            float riverMask = CustomMathf.Abs(baseNoise - 0.5f);
            riverMask = riverMask * (0.58f + 0.25f * ridgeNoise) - 0.045f * CustomMathf.Abs(detailNoise);
            riverMask = CustomMathf.Max(riverMask, 0.0f);

            flowDir = ComputeRiverFlowDirection(sampleX, sampleZ);
            return riverMask;
        }

        private static float ComputeChannelPressure(float catchmentStrength, float hydrology)
        {
            float baseValue = 0.35f + catchmentStrength * 0.5f + hydrology * 0.3f;
            return CustomMathf.Clamp01(baseValue);
        }

        private static void SmoothRiverIntensity(float[,] riverIntensity, float[,] erosionRiskField, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);
            int iterations = CustomMathf.Max(1, RiverIntensitySmoothIterations);
            float baseBlend = CustomMathf.Clamp01(RiverIntensitySmoothBlend);
            var scratch = new float[width, depth];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float hydrology = hydrologyMask[x, z];
                        float flow = flowAccumulation[x, z];
                        var gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        CustomVector2 flowDir = gradient.sqrMagnitude > CustomVector2.kEpsilon ? gradient * -1f : CustomVector2.zero;
                        if (flowDir.sqrMagnitude > CustomVector2.kEpsilon)
                        {
                            flowDir.Normalize();
                        }
                        float weightedSum = riverIntensity[x, z];
                        float weightTotal = 1f;
                        float maxAlignment = 0f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                float neighborHydrology = hydrologyMask[nx, nz];
                                float baseWeight = 1f + erosionRiskField[nx, nz] * 0.75f + hydrology * 0.35f + neighborHydrology * 0.25f;
                                float neighborFlow = flowAccumulation[nx, nz];
                                var neighborDir = new CustomVector2(dx, dz);
                                if (neighborDir.sqrMagnitude > CustomVector2.kEpsilon)
                                {
                                    neighborDir.Normalize();
                                }

                                float alignment = flowDir.sqrMagnitude <= CustomVector2.kEpsilon ? 0f : CustomMathf.Max(0f, CustomVector2.Dot(flowDir, neighborDir));
                                maxAlignment = CustomMathf.Max(maxAlignment, alignment);

                                float flowWeight = 1f + RiverFlowAlignmentWeight * (CustomMathf.Min(flow + neighborFlow, 2.5f) * 0.45f + alignment * 1.1f);
                                float hydrologyDelta = CustomMathf.Abs(hydrology - neighborHydrology);
                                float gradientWeight = CustomMathf.Clamp(1f - RiverGradientPenalty * hydrologyDelta, 0.15f, 1f);
                                float finalWeight = CustomMathf.Clamp(baseWeight * flowWeight * gradientWeight, 0.35f, 3.5f);
                                weightedSum += riverIntensity[nx, nz] * finalWeight;
                                weightTotal += finalWeight;
                            }
                        }

                        float average = weightTotal > 0f ? weightedSum / weightTotal : riverIntensity[x, z];
                        float blend = CustomMathf.Clamp(baseBlend + hydrology * 0.2f + flow * 0.12f + maxAlignment * 0.2f, 0f, 0.95f);
                        scratch[x, z] = riverIntensity[x, z] * (1f - blend) + average * blend;
                    }
                }

                Array.Copy(scratch, riverIntensity, riverIntensity.Length);
            }
        }

        private static float SampleDeterministicNoise(int x, int z, int salt)
        {
            unchecked
            {
                int hash = x * 734287 + z * 912271 + salt * 19997;
                hash ^= (hash << 13);
                hash ^= (hash >> 9);
                hash = hash * 60493 + 19990303;
                hash ^= (hash << 11);
                return (hash & int.MaxValue) / (float)int.MaxValue;
            }
        }

        private static void ResolvePerpendicularOffset(CustomVector2 direction, int step, out int offsetX, out int offsetZ)
        {
            float absX = CustomMathf.Abs(direction.x);
            float absZ = CustomMathf.Abs(direction.y);

            offsetX = absX >= 0.35f ? (direction.x >= 0f ? step : -step) : 0;
            offsetZ = absZ >= 0.35f ? (direction.y >= 0f ? step : -step) : 0;

            if (offsetX == 0 && offsetZ == 0)
            {
                if (absX >= absZ)
                {
                    offsetX = direction.x >= 0f ? step : -step;
                }
                else
                {
                    offsetZ = direction.y >= 0f ? step : -step;
                }
            }
        }

        private static void ExpandRiverChannel(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, int riverSurface, CustomVector2 flowDir, float channelPressure)
        {
            if (channelPressure < 0.45f)
            {
                return;
            }

            CustomVector2 perpendicular = new CustomVector2(-flowDir.y, flowDir.x);
            if (perpendicular.sqrMagnitude < CustomVector2.kEpsilon)
            {
                perpendicular = CustomVector2.right;
            }
            perpendicular.Normalize();

            int reach = channelPressure > 0.9f ? 2 : 1;
            for (int step = 1; step <= reach; step++)
            {
                float floodStrength = CustomMathf.Clamp01(channelPressure - 0.35f - 0.15f * (step - 1));
                if (floodStrength <= 0f)
                {
                    continue;
                }

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);

                ResolvePerpendicularOffset(new CustomVector2(-perpendicular.x, -perpendicular.y), step, out offsetX, out offsetZ);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);
            }
        }

        private static void GenerateRiverSystems(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            if (subWorldSize.SizeX < 4 || subWorldSize.SizeZ < 4)
            {
                return;
            }

            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            float[,] flowAccumulation = BuildFlowAccumulation(surfaceCache, subWorldSize);
            BlendHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            EnforceHydrologyEdgeConsistency(subWorldSize, hydrologyMask, flowAccumulation);
            StabilizeHydrologyGradients(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            SmoothHydrologyFields(hydrologyMask, flowAccumulation);
            NormalizeHydrologyRange(subWorldSize, hydrologyMask, flowAccumulation);
            RelaxHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            AnchorHydrologyToSlope(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] riparianSaturation = BuildRiparianSaturationMap(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] erosionRiskField = BuildErosionRiskField(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float riverBankErosionWeight = CustomMathf.Clamp01(RiverBankErosionWeight);

            const float channelThreshold = 0.033f;
            const float bankThreshold = 0.07f;
            float[,] riverIntensity = new float[subWorldSize.SizeX, subWorldSize.SizeZ];

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    CustomVector2 flowDir;
                    float riverMask = EvaluateRiverIntensity(x, z, out flowDir);
                    float hydrology = hydrologyMask[x, z];
                    float riparian = riparianSaturation[x, z];
                    float catchment = flowAccumulation[x, z];
                    float catchmentStrength = CustomMathf.Clamp01(catchment / 6f);
                    float erosionRisk = erosionRiskField[x, z];
                    float channelPressure = ComputeChannelPressure(catchmentStrength, hydrology);
                    channelPressure = CustomMathf.Clamp01(channelPressure + riparian * 0.2f + erosionRisk * riverBankErosionWeight);
                    float adjustedMask = AdjustRiverMask(riverMask, hydrology) - catchmentStrength * 0.015f - riparian * 0.0125f - erosionRisk * 0.01f;
                    adjustedMask = CustomMathf.Max(0f, adjustedMask);
                    adjustedMask *= ComputeRiverReliefScale(surfaceCache, subWorldSize, x, z);
                    riverIntensity[x, z] = CustomMathf.Max(0f, adjustedMask * (1f - riparian * 0.2f));

                    if (adjustedMask >= bankThreshold)
                    {
                        continue;
                    }

                    CustomVector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    if (slopeDir.sqrMagnitude > CustomVector2.kEpsilon)
                    {
                        CustomVector2 blended = CustomVector2.Lerp(flowDir, slopeDir, 0.65f);
                        flowDir = blended.sqrMagnitude > CustomVector2.kEpsilon ? blended.normalized : slopeDir;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));

                    if (adjustedMask < channelThreshold)
                    {
                        float normalized = CustomMathf.Clamp01(1.0f - adjustedMask / channelThreshold);
                        float enrichedPressure = CustomMathf.Clamp01(channelPressure + riparian * 0.2f + hydrology * 0.05f);
                        CarveRiverColumn(subWorldBlockData, subWorldSize, surfaceCache, x, z, normalized, enrichedPressure, riverSurface, flowDir);
                    }
                    else
                    {
                        float bankStrength = CustomMathf.Clamp01(1.0f - (adjustedMask - channelThreshold) / (bankThreshold - channelThreshold));
                        bankStrength *= 0.85f + channelPressure * 0.35f + riparian * 0.35f + erosionRisk * riverBankErosionWeight;
                        bankStrength = CustomMathf.Clamp(bankStrength, 0f, 1.25f);
                        FeatherRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x, z, bankStrength, riverSurface, flowDir);
                    }
                }
            }

            SmoothRiverIntensity(riverIntensity, erosionRiskField, hydrologyMask, flowAccumulation);

            StitchTributaryChannels(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, channelThreshold, bankThreshold);
            ApplyRiverBankErosion(subWorldBlockData, subWorldSize, surfaceCache, riverIntensity, bankThreshold, channelThreshold);
            ApplyRiverSedimentPass(subWorldBlockData, subWorldSize, surfaceCache, riverIntensity, channelThreshold, bankThreshold, hydrologyMask);
            ApplyRiverPointBarSediment(subWorldBlockData, subWorldSize, surfaceCache, riverIntensity, hydrologyMask);
            AddFloodplainWetlands(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, riverIntensity);
            AddFloodplainSwales(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, riverIntensity, channelThreshold, bankThreshold);
            ApplyRiparianBankStabilization(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, riparianSaturation, riverIntensity, channelThreshold, bankThreshold);
            AddRiverDeltaFans(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, channelThreshold, bankThreshold);
            ApplyRiverGradientSmoothing(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, riverIntensity, bankThreshold);
            ApplyRiverMeanderTerraces(subWorldBlockData, subWorldSize, surfaceCache, riverIntensity, hydrologyMask);
            ApplyRiverHydrologyFeedback(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, channelThreshold, bankThreshold);
            AddRiverSeepageChannels(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, channelThreshold, bankThreshold);
        }

        private static void NormalizeRiverIntensity(
            float[,] riverIntensity,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float channelThreshold,
            float bankThreshold)
        {
            float clamp = bankThreshold * 1.35f;
            float continuityWeight = CustomMathf.Clamp01(HydrologyContinuityWeight);
            float persistenceWeight = CustomMathf.Clamp01(HydrologyFlowPersistence);
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity <= 0f)
                    {
                        riverIntensity[x, z] = 0f;
                        continue;
                    }

                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 8f);
                    float hydrologyTerm = hydrology * (0.55f + continuityWeight * 0.6f);
                    float flowTerm = flow * (0.65f + persistenceWeight * 0.6f);
                    float weight = CustomMathf.Clamp(0.35f + hydrologyTerm + flowTerm, 0.35f, 1.75f);
                    float stabilized = intensity * weight;

                    if (stabilized < channelThreshold * 0.08f)
                    {
                        stabilized = 0f;
                    }

                    riverIntensity[x, z] = CustomMathf.Clamp(stabilized, 0f, clamp);
                }
            }
        }

        private static void CarveRiverColumn(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, float normalized, float channelPressure, int riverSurface, CustomVector2 flowDir)
        {
            int surface = surfaceCache[x, z];
            if (surface <= 1)
            {
                return;
            }

            float pressureScale = 0.85f + channelPressure * 0.65f;
            int baseDepth = CustomMathf.Clamp(RiverDepth, 2, 24);
            int channelDepth = CustomMathf.Clamp(
                baseDepth + CustomMathf.RoundToInt(CustomMathf.Lerp(1.0f, baseDepth * 0.6f, normalized * pressureScale)),
                baseDepth,
                baseDepth + 8);
            int waterFloor = CustomMathf.Max(1, riverSurface - channelDepth);

            for (int y = surface; y >= waterFloor; y--)
            {
                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
            }

            if (waterFloor - 1 >= 0)
            {
                subWorldBlockData[x, waterFloor - 1, z].CurrentType = (byte)BlockTileType.SAND;
            }

            for (int y = waterFloor; y <= riverSurface && y < subWorldSize.SizeY; y++)
            {
                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
            }

            if (riverSurface < GlobalRiverWaterLevel && GlobalRiverWaterLevel < subWorldSize.SizeY)
            {
                for (int y = riverSurface + 1; y <= GlobalRiverWaterLevel && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                }

                surfaceCache[x, z] = CustomMathf.Min(GlobalRiverWaterLevel, subWorldSize.SizeY - 1);
            }
            else
            {
                surfaceCache[x, z] = CustomMathf.Min(riverSurface, subWorldSize.SizeY - 1);
            }

            int waterTop = riverSurface < GlobalRiverWaterLevel && GlobalRiverWaterLevel < subWorldSize.SizeY
                ? CustomMathf.Min(GlobalRiverWaterLevel, subWorldSize.SizeY - 1)
                : CustomMathf.Min(riverSurface, subWorldSize.SizeY - 1);

            if (waterTop + 1 < subWorldSize.SizeY)
            {
                subWorldBlockData[x, waterTop + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
            }

            ExpandRiverChannel(subWorldBlockData, subWorldSize, surfaceCache, x, z, riverSurface, flowDir, channelPressure);

            int maxRadius = CustomMathf.Clamp(2 + CustomMathf.RoundToInt(normalized * (2f + channelPressure * 1.5f)), 2, 5);

            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dz = -maxRadius; dz <= maxRadius; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    float distance = CustomMathf.Sqrt(dx * dx + dz * dz);
                    if (distance > maxRadius + 0.25f)
                    {
                        continue;
                    }

                    float falloff = 1.0f - CustomMathf.Clamp01(distance / (maxRadius + 0.001f));
                    bool allowFlood = distance <= 1.5f;
                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + dx, z + dz, falloff, riverSurface, allowFlood);
                }
            }

            int forwardX = x + CustomMathf.RoundToInt(flowDir.x);
            int forwardZ = z + CustomMathf.RoundToInt(flowDir.y);
            ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, forwardX, forwardZ, 0.35f, riverSurface, false);

            int backX = x - CustomMathf.RoundToInt(flowDir.x);
            int backZ = z - CustomMathf.RoundToInt(flowDir.y);
            ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, backX, backZ, 0.35f, riverSurface, false);
        }

        private static void FeatherRiverBank(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, float strength, int riverSurface, CustomVector2 flowDir)
        {
            if (strength <= 0f)
            {
                return;
            }

            strength = CustomMathf.Clamp01(strength);
            ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x, z, strength * 0.65f, riverSurface, false);

            CustomVector2 perpendicular = new CustomVector2(-flowDir.y, flowDir.x);
            if (perpendicular.sqrMagnitude < CustomVector2.kEpsilon)
            {
                perpendicular = CustomVector2.right;
            }
            perpendicular.Normalize();

            int reach = CustomMathf.Max(1, CustomMathf.RoundToInt(1f + strength * 2f));
            for (int step = 1; step <= reach; step++)
            {
                float falloff = CustomMathf.Clamp01(strength - step * 0.25f);
                if (falloff <= 0f)
                {
                    break;
                }

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);

                ResolvePerpendicularOffset(new CustomVector2(-perpendicular.x, -perpendicular.y), step, out offsetX, out offsetZ);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);
            }
        }

        private static void ApplyRiparianBankStabilization(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] riparianSaturation,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float riparian = riparianSaturation[x, z];
                    if (riparian < 0.6f)
                    {
                        continue;
                    }

                    float intensity = riverIntensity[x, z];
                    if (intensity >= bankThreshold || intensity <= channelThreshold * 0.45f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));
                    float hydrology = hydrologyMask[x, z];
                    float shelfStrength = CustomMathf.Clamp01((riparian - 0.55f) * 1.4f + hydrology * 0.25f);
                    bool allowFlood = intensity < bankThreshold * 0.65f;

                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x, z, shelfStrength * 0.8f, riverSurface, allowFlood);
                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x, z, shelfStrength * 0.5f, riverSurface, false);
                }
            }
        }

        private static void StitchTributaryChannels(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.7f)
                    {
                        continue;
                    }

                    float catchment = CustomMathf.Clamp01(flowAccumulation[x, z] / 7.5f);
                    CustomVector2 flowDir;
                    float intensity = EvaluateRiverIntensity(x, z, out flowDir);
                    if (intensity >= bankThreshold * 1.1f || intensity <= channelThreshold * 0.35f)
                    {
                        continue;
                    }

                    float riverGap = 1f - CustomMathf.Clamp01(intensity / channelThreshold);
                    float weight = hydrology * 0.4f + catchment * 0.4f + riverGap * 0.35f;
                    if (weight < 0.55f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    CustomVector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    CustomVector2 direction = flowDir.sqrMagnitude > CustomVector2.kEpsilon ? flowDir : slopeDir;
                    if (direction.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        continue;
                    }

                    TraceTributaryChannel(subWorldBlockData, subWorldSize, surfaceCache, riverIntensity, x, z, direction, weight, channelThreshold);
                }
            }
        }

        private static void TraceTributaryChannel(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] riverIntensity,
            int startX,
            int startZ,
            CustomVector2 direction,
            float strength,
            float channelThreshold)
        {
            if (direction.sqrMagnitude < CustomVector2.kEpsilon)
            {
                return;
            }

            CustomVector2 dir = direction.normalized;
            int steps = CustomMathf.Clamp(CustomMathf.RoundToInt(3 + strength * 5f), 3, 8);
            float x = startX;
            float z = startZ;
            float channelPressure = CustomMathf.Clamp(0.35f + strength * 0.4f, 0.35f, 0.85f);

            for (int i = 0; i < steps; i++)
            {
                int cx = CustomMathf.Clamp(CustomMathf.RoundToInt(x), 0, subWorldSize.SizeX - 1);
                int cz = CustomMathf.Clamp(CustomMathf.RoundToInt(z), 0, subWorldSize.SizeZ - 1);
                int surface = surfaceCache[cx, cz];
                if (surface <= 1)
                {
                    break;
                }

                int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));
                float normalized = CustomMathf.Clamp(0.55f + strength * 0.35f - i * 0.08f, 0.2f, 0.95f);
                CarveRiverColumn(subWorldBlockData, subWorldSize, surfaceCache, cx, cz, normalized, channelPressure, riverSurface, dir);
                riverIntensity[cx, cz] = CustomMathf.Min(riverIntensity[cx, cz], channelThreshold * 0.5f);

                x += dir.x + (Noise.GetNoise(x * 0.2f, 0, z * 0.2f) - 0.5f) * 0.3f;
                z += dir.y + (Noise.GetNoise(x * 0.18f + 17f, 0, z * 0.18f) - 0.5f) * 0.3f;

                if (!WorldGenerateUtils.CheckSubWorldBoundary(CustomMathf.RoundToInt(x), riverSurface, CustomMathf.RoundToInt(z), subWorldSize))
                {
                    break;
                }
            }
        }

        private static void ApplyRiverBankErosion(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, float[,] intensityField, float bankThreshold, float channelThreshold)
        {
            int maxX = subWorldSize.SizeX;
            int maxZ = subWorldSize.SizeZ;
            for (int x = 0; x < maxX; x++)
            {
                for (int z = 0; z < maxZ; z++)
                {
                    float intensity = intensityField[x, z];
                    if (intensity >= bankThreshold + 0.01f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                        if (surface <= 1)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    int neighborSum = 0;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= maxX || nz < 0 || nz >= maxZ)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                neighborSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, nx, nz);
                                if (neighborSurface <= 0)
                                {
                                    continue;
                                }
                                surfaceCache[nx, nz] = neighborSurface;
                            }

                            neighborSum += neighborSurface;
                            neighborCount++;
                        }
                    }

                    if (neighborCount == 0)
                    {
                        continue;
                    }

                    int neighborAverage = neighborSum / neighborCount;
                    if (surface - neighborAverage > 2)
                    {
                        int target = CustomMathf.Max(1, neighborAverage + 1);
                        for (int y = surface; y > target; y--)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                        }
                        surface = target;
                        surfaceCache[x, z] = surface;
                    }

                    BlockTileType topBlock = (BlockTileType)subWorldBlockData[x, surface, z].CurrentType;
                    if (topBlock == BlockTileType.GRASS || topBlock == BlockTileType.STONE_SMALL || topBlock == BlockTileType.RED_STONE)
                    {
                        subWorldBlockData[x, surface, z].CurrentType = (byte)BlockTileType.SAND;
                    }

                    if (intensity < channelThreshold * 0.65f && surface - 1 >= 1)
                    {
                        BlockTileType belowBlock = (BlockTileType)subWorldBlockData[x, surface - 1, z].CurrentType;
                        if (belowBlock == BlockTileType.GRASS || belowBlock == BlockTileType.STONE_SMALL)
                        {
                            subWorldBlockData[x, surface - 1, z].CurrentType = (byte)BlockTileType.SAND;
                        }
                    }

                    if (surface + 1 < subWorldSize.SizeY && subWorldBlockData[x, surface + 1, z].CurrentType != (byte)BlockTileType.EMPTY)
                    {
                        subWorldBlockData[x, surface + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                }
            }

        }

        private static void ApplyRiverSedimentPass(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold,
            float[,] hydrologyMask)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity >= bankThreshold + 0.01f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    if (intensity < channelThreshold * 0.85f)
                    {
                        int bedY = CustomMathf.Max(1, surface - Utilitys.RandomInteger(1, 3));
                        subWorldBlockData[x, bedY, z].CurrentType = (byte)BlockTileType.SAND;
                        if (bedY - 1 >= 1 && Utilitys.RandomFloat(0f, 1f) < 0.35f)
                        {
                            subWorldBlockData[x, bedY - 1, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                        }

                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float floodChance = CustomMathf.Clamp01((hydrology - 0.45f) * 1.4f);
                    if (floodChance <= 0f || Utilitys.RandomFloat(0f, 1f) > floodChance)
                    {
                        continue;
                    }

                    int target = CustomMathf.Max(1, surface - 1);
                    subWorldBlockData[x, target, z].CurrentType = (byte)BlockTileType.SAND;
                    if (target + 1 < subWorldSize.SizeY)
                    {
                        subWorldBlockData[x, target + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                }
            }
        }

        private static void ApplyRiverPointBarSediment(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] riverIntensity,
            float[,] hydrologyMask)
        {
            const float channelThreshold = 0.033f;
            const float bankThreshold = 0.07f;

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity < channelThreshold * 0.85f || intensity > bankThreshold * 1.05f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    CustomVector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    if (slopeDir.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        slopeDir = CustomVector2.right;
                    }

                    CustomVector2 perpendicular = new CustomVector2(-slopeDir.y, slopeDir.x).normalized;
                    int targetX = x + CustomMathf.Clamp(CustomMathf.RoundToInt(perpendicular.x), -1, 1);
                    int targetZ = z + CustomMathf.Clamp(CustomMathf.RoundToInt(perpendicular.y), -1, 1);
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(targetX, surface, targetZ, subWorldSize))
                    {
                        continue;
                    }

                    BlockTileType topBlock = (BlockTileType)subWorldBlockData[targetX, surface, targetZ].CurrentType;
                    if (topBlock == BlockTileType.EMPTY)
                    {
                        continue;
                    }

                    if (topBlock == BlockTileType.GRASS || topBlock == BlockTileType.STONE_SMALL)
                    {
                        subWorldBlockData[targetX, surface, targetZ].CurrentType = (byte)BlockTileType.SAND;
                    }

                    if (intensity < channelThreshold * 1.05f)
                    {
                        int waterFloor = CustomMathf.Max(1, surface - 1);
                        subWorldBlockData[targetX, waterFloor, targetZ].CurrentType = (byte)BlockTileType.WATER;
                        if (waterFloor - 1 >= 1)
                        {
                            subWorldBlockData[targetX, waterFloor - 1, targetZ].CurrentType = (byte)BlockTileType.SAND;
                        }
                        surfaceCache[targetX, targetZ] = CustomMathf.Max(surfaceCache[targetX, targetZ], waterFloor);
                    }
                }
            }
        }

        private static void AddFloodplainWetlands(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] riverIntensity)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.7f)
                    {
                        continue;
                    }

                    float proximity = 1f - CustomMathf.Clamp01((riverIntensity[x, z] - 0.033f) / 0.04f);
                    float weight = hydrology * 0.6f + proximity * 0.4f;
                    if (weight < 0.78f || Utilitys.RandomFloat(0f, 1f) > weight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    int basinDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + weight * 3f), 1, 4);
                    int floor = CustomMathf.Max(surface - basinDepth, 1);
                    for (int y = surface; y >= floor; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.WATER;
                    if (floor - 1 >= 1)
                    {
                        subWorldBlockData[x, floor - 1, z].CurrentType = (byte)BlockTileType.SAND;
                    }

                    surfaceCache[x, z] = CustomMathf.Max(surfaceCache[x, z], floor);
                }
            }
        }

        private static void AddFloodplainSwales(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 2; x < subWorldSize.SizeX - 2; x++)
            {
                for (int z = 2; z < subWorldSize.SizeZ - 2; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity <= channelThreshold || intensity >= bankThreshold)
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.45f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    float wetness = CustomMathf.Clamp01((hydrology - 0.4f) * 0.9f + (bankThreshold - intensity) * 2.35f);
                    if (wetness <= 0.05f)
                    {
                        continue;
                    }

                    int swaleDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1f + wetness * 3.5f), 1, 4);
                    int floor = CustomMathf.Max(surface - swaleDepth, 1);
                    for (int y = surface; y >= floor; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    surfaceCache[x, z] = floor;
                    if (wetness > 0.6f)
                    {
                        subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.WATER;
                        if (floor + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[x, floor + 1, z].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                    else
                    {
                        subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.SAND;
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, floor, nz, subWorldSize))
                            {
                                continue;
                            }

                            int neighborSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, nx, nz);
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            BlockTileType rimType = wetness > 0.6f ? BlockTileType.SAND : BlockTileType.GRASS;
                            subWorldBlockData[nx, neighborSurface, nz].CurrentType = (byte)rimType;
                        }
                    }
                }
            }
        }

        private static void AddRiverDeltaFans(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 2; x < subWorldSize.SizeX - 2; x++)
            {
                for (int z = 2; z < subWorldSize.SizeZ - 2; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity <= channelThreshold * 0.55f || intensity >= bankThreshold * 1.05f)
                    {
                        continue;
                    }

                    float accumulation = flowAccumulation[x, z];
                    if (accumulation < 2.2f)
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float spawnWeight = CustomMathf.Clamp01((accumulation - 2f) * 0.18f + hydrology * 0.55f);
                    spawnWeight *= 1f - CustomMathf.Clamp01((intensity - channelThreshold) / (bankThreshold - channelThreshold + 0.001f));
                    float selector = SampleDeterministicNoise(x, z, 257);
                    if (spawnWeight < 0.3f || selector > spawnWeight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));
                    CustomVector2 slope = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    if (slope.sqrMagnitude <= CustomVector2.kEpsilon)
                    {
                        slope = CustomVector2.right;
                    }

                    CustomVector2 perpendicular = new CustomVector2(-slope.y, slope.x);
                    bool braidRight = SampleDeterministicNoise(x, z, 311) > 0.5f;
                    int fanReach = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + accumulation * 0.35f), 1, 4);

                    for (int step = 1; step <= fanReach; step++)
                    {
                        int targetX = CustomMathf.Clamp(x + CustomMathf.RoundToInt(slope.x * step), 1, subWorldSize.SizeX - 2);
                        int targetZ = CustomMathf.Clamp(z + CustomMathf.RoundToInt(slope.y * step), 1, subWorldSize.SizeZ - 2);

                        int columnSurface = surfaceCache[targetX, targetZ];
                        if (columnSurface <= 1)
                        {
                            columnSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, targetX, targetZ);
                            if (columnSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[targetX, targetZ] = columnSurface;
                        }

                        int columnRiverSurface = CustomMathf.Min(columnSurface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));
                        float falloff = CustomMathf.Clamp01(1f - step / (fanReach + 1f));
                        ShapeRiverBank(
                            subWorldBlockData,
                            subWorldSize,
                            surfaceCache,
                            targetX,
                            targetZ,
                            0.45f + falloff * 0.4f,
                            columnRiverSurface,
                            true);

                        ResolvePerpendicularOffset(perpendicular, 1, out int offsetX, out int offsetZ);
                        if (!braidRight)
                        {
                            offsetX = -offsetX;
                            offsetZ = -offsetZ;
                        }

                        int barX = targetX + offsetX;
                        int barZ = targetZ + offsetZ;
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(barX, 0, barZ, subWorldSize))
                        {
                            continue;
                        }

                        int barSurface = surfaceCache[barX, barZ];
                        if (barSurface <= 1)
                        {
                            barSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, barX, barZ);
                            if (barSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[barX, barZ] = barSurface;
                        }

                        subWorldBlockData[barX, barSurface, barZ].CurrentType = (byte)BlockTileType.SAND;
                    }
                }
            }
        }

        private static void ShapeRiverBank(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, float falloff, int riverSurface, bool allowFlood)
        {
            if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
            {
                return;
            }

            int surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                if (surface <= 0)
                {
                    return;
                }
                surfaceCache[x, z] = surface;
            }

            int desiredSurface = surface;
            int maxDrop = CustomMathf.Max(1, CustomMathf.RoundToInt(CustomMathf.Lerp(3f, 1f, falloff)));

            if (surface > riverSurface + maxDrop)
            {
                desiredSurface = CustomMathf.Max(riverSurface + maxDrop, 1);
            }

            if (desiredSurface < surface)
            {
                for (int y = surface; y > desiredSurface; y--)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                }
                surface = desiredSurface;
                surfaceCache[x, z] = desiredSurface;
            }

            if (allowFlood && surface <= riverSurface)
            {
                int fillStart = CustomMathf.Max(surface, 1);
                for (int y = fillStart; y <= riverSurface && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                }

                if (riverSurface < GlobalRiverWaterLevel && GlobalRiverWaterLevel < subWorldSize.SizeY)
                {
                    for (int y = riverSurface + 1; y <= GlobalRiverWaterLevel && y < subWorldSize.SizeY; y++)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                    }

                    surface = CustomMathf.Min(GlobalRiverWaterLevel, subWorldSize.SizeY - 1);
                }
                else
                {
                    surface = CustomMathf.Min(riverSurface, subWorldSize.SizeY - 1);
                }

                surfaceCache[x, z] = surface;
            }
            else
            {
                subWorldBlockData[x, surface, z].CurrentType = (byte)BlockTileType.SAND;
            }

            if (surface + 1 < subWorldSize.SizeY)
            {
                subWorldBlockData[x, surface + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
            }
        }

        private static float ComputeLocalRelief(int[,] surfaceCache, SubWorldSize subWorldSize, int centerX, int centerZ, int radius)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0f;
            float sumSq = 0f;
            int samples = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= subWorldSize.SizeX || z < 0 || z >= subWorldSize.SizeZ)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    samples++;
                    float height = surface;
                    sum += height;
                    sumSq += height * height;
                    min = CustomMathf.Min(min, height);
                    max = CustomMathf.Max(max, height);
                }
            }

            if (samples == 0)
            {
                return 0f;
            }

            float mean = sum / samples;
            float variance = CustomMathf.Max(0f, (sumSq / samples) - mean * mean);
            float stdDev = CustomMathf.Sqrt(variance);
            return (max - min) + stdDev;
        }

        private static float[,] BuildLakeCandidateHeatmap(SubWorldSize subWorldSize, int[,] surfaceCache, float[,] hydrologyMask, float[,] flowAccumulation, float[,] erosionRiskField)
        {
            float[,] heatmap = new float[subWorldSize.SizeX, subWorldSize.SizeZ];

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface >= subWorldSize.SizeY - 2)
                    {
                        heatmap[x, z] = 0f;
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float accumulation = CustomMathf.Clamp01(flowAccumulation[x, z] / 10f);
                    float relief = ComputeLocalRelief(surfaceCache, subWorldSize, x, z, 3);
                    float bowlStrength = CustomMathf.Clamp01(1f - relief / 8f);
                    float altitudeBias = 1f - CustomMathf.Clamp01(CustomMathf.Abs(surface - GlobalRiverWaterLevel) / 24f);
                    float shelterBias = CustomMathf.Clamp01((subWorldSize.SizeY - surface) / (float)subWorldSize.SizeY);
                    float erosion = CustomMathf.Clamp01(erosionRiskField[x, z]);
                    float erosionPenalty = CustomMathf.Clamp01(erosion * 0.6f);
                    float score = hydrology * 0.45f + accumulation * 0.3f + bowlStrength * 0.3f + altitudeBias * 0.2f + shelterBias * 0.1f;
                    score -= CustomMathf.Clamp01(relief / 18f) * 0.2f;
                    score -= erosionPenalty * 0.25f;
                    heatmap[x, z] = CustomMathf.Clamp01(score);
                }
            }

            return heatmap;
        }

        private static float[,] BuildRiverIntensityPreview(
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] riparianSaturation,
            float[,] erosionRiskField)
        {
            float[,] riverIntensity = new float[subWorldSize.SizeX, subWorldSize.SizeZ];
            float reliefWeight = CustomMathf.Max(0f, RiverReliefPenaltyWeight);

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    CustomVector2 flowDir;
                    float riverMask = EvaluateRiverIntensity(x, z, out flowDir);
                    float hydrology = hydrologyMask[x, z];
                    float riparian = riparianSaturation[x, z];
                    float catchmentStrength = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float erosionRisk = erosionRiskField[x, z];
                    float adjustedMask = AdjustRiverMask(riverMask, hydrology) - catchmentStrength * 0.015f - riparian * 0.0125f - erosionRisk * 0.01f;
                    adjustedMask = CustomMathf.Max(0f, adjustedMask);

                    if (reliefWeight > 0f)
                    {
                        adjustedMask *= ComputeRiverReliefScale(surfaceCache, subWorldSize, x, z);
                    }

                    riverIntensity[x, z] = CustomMathf.Max(0f, adjustedMask * (1f - riparian * 0.2f));
                }
            }

            NormalizeRiverIntensity(riverIntensity, hydrologyMask, flowAccumulation, channelThreshold, bankThreshold);
            SmoothRiverIntensity(riverIntensity, erosionRiskField, hydrologyMask, flowAccumulation);
            return riverIntensity;
        }

        private static void GenerateSurfaceLakes(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            float[,] flowAccumulation = BuildFlowAccumulation(surfaceCache, subWorldSize);
            BlendHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            EnforceHydrologyEdgeConsistency(subWorldSize, hydrologyMask, flowAccumulation);
            StabilizeHydrologyGradients(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            SmoothHydrologyFields(hydrologyMask, flowAccumulation);
            NormalizeHydrologyRange(subWorldSize, hydrologyMask, flowAccumulation);
            ClampHydrologyToWaterTable(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            RelaxHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            AnchorHydrologyToSlope(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] riparianSaturation = BuildRiparianSaturationMap(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] erosionRiskField = BuildErosionRiskField(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] lakeCandidateHeatmap = BuildLakeCandidateHeatmap(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, erosionRiskField);
            float[,] riverIntensityPreview = BuildRiverIntensityPreview(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riparianSaturation, erosionRiskField);
            float lakeSpawnBias = CustomMathf.Clamp(LakeSpawnWeightBias, 0f, 1.3f);

            int lakeAttempts = Utilitys.RandomInteger(1, 3);
            for (int attempt = 0; attempt < lakeAttempts; attempt++)
            {
                int centerX = Utilitys.RandomInteger(4, subWorldSize.SizeX - 4);
                int centerZ = Utilitys.RandomInteger(4, subWorldSize.SizeZ - 4);

                int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, centerX, centerZ);
                if (surface <= 2 || surface >= subWorldSize.SizeY - 4)
                {
                    continue;
                }

                float hydrology = hydrologyMask[centerX, centerZ];
                float riparian = riparianSaturation[centerX, centerZ];
                float flow = CustomMathf.Clamp01(flowAccumulation[centerX, centerZ] / 12f);
                float candidateScore = lakeCandidateHeatmap[centerX, centerZ];
                float relief = ComputeLocalRelief(surfaceCache, subWorldSize, centerX, centerZ, 6);
                float basinStability = 1f - CustomMathf.Clamp01(relief / 10f);
                float ridgeNoise = Noise.GetNoise((centerX + 37.0f) * 0.06f, 0, (centerZ - 11.0f) * 0.06f);
                float erosionRisk = erosionRiskField[centerX, centerZ];
                float hydrologyCoherence = CustomMathf.Clamp01((hydrology + riparian) * 0.5f + flow * 0.35f);
                float erosionPenalty = CustomMathf.Clamp(1f - erosionRisk * 0.65f, 0.25f, 1f);
                float spawnWeight = CustomMathf.Clamp((ridgeNoise - 0.35f) * 1.4f + hydrology * 0.9f + candidateScore * 0.85f, 0f, 1.2f);
                spawnWeight = CustomMathf.Clamp(spawnWeight + riparian * 0.25f + flow * 0.2f + hydrologyCoherence * 0.2f + lakeSpawnBias, 0f, 1.3f);
                float riverPressure = SampleRiverIntensity(riverIntensityPreview, centerX, centerZ, 2);
                if (LakeRiverProximitySuppression > 0f && riverPressure > 0f)
                {
                    float suppression = CustomMathf.Clamp(riverPressure * LakeRiverProximitySuppression, 0f, 0.85f);
                    spawnWeight *= 1f - suppression;
                }
                spawnWeight *= CustomMathf.Lerp(0.65f, 1.2f, basinStability);
                spawnWeight *= CustomMathf.Clamp(0.85f + hydrologyCoherence * 0.35f, 0.7f, 1.35f);
                spawnWeight *= erosionPenalty;

                if (spawnWeight < CustomMathf.Max(0.2f, lakeSpawnBias) || (candidateScore < 0.2f && spawnWeight < CustomMathf.Max(0.4f, lakeSpawnBias + 0.1f)))
                {
                    continue;
                }

                if (Utilitys.RandomFloat(0f, 1f) > spawnWeight)
                {
                    continue;
                }

                int radiusX = Utilitys.RandomInteger(4, 7) + CustomMathf.RoundToInt(hydrology * 2f) + CustomMathf.RoundToInt(candidateScore * 3f) + CustomMathf.RoundToInt(riparian * 2f) + CustomMathf.RoundToInt(flow * 1.5f);
                int radiusZ = Utilitys.RandomInteger(3, 6) + CustomMathf.RoundToInt(hydrology * 1.5f) + CustomMathf.RoundToInt(candidateScore * 2.5f) + CustomMathf.RoundToInt(riparian * 1.5f) + CustomMathf.RoundToInt(flow * 1.25f);
                radiusX = CustomMathf.RoundToInt(radiusX * (0.8f + erosionPenalty * 0.45f));
                radiusZ = CustomMathf.RoundToInt(radiusZ * (0.82f + erosionPenalty * 0.4f));
                radiusX = CustomMathf.Clamp(radiusX, 4, 9);
                radiusZ = CustomMathf.Clamp(radiusZ, 3, 8);
                int maxDepth = Utilitys.RandomInteger(3, 5) + CustomMathf.RoundToInt(hydrology * 2f) + CustomMathf.RoundToInt(candidateScore * 3f) + CustomMathf.RoundToInt(flow * 1.5f);
                maxDepth = CustomMathf.RoundToInt(maxDepth * (0.75f + erosionPenalty * 0.35f));
                maxDepth = CustomMathf.Clamp(maxDepth, 3, 8);

                int waterSurface = CustomMathf.Clamp(
                    GlobalRiverWaterLevel +
                    Utilitys.RandomInteger(-1, 2) +
                    CustomMathf.RoundToInt((hydrology - 0.5f) * 3f) +
                    CustomMathf.RoundToInt((candidateScore - 0.5f) * 2f) +
                    CustomMathf.RoundToInt((riparian - 0.5f) * 3f) +
                    CustomMathf.RoundToInt((0.5f - erosionRisk) * 2f) +
                    CustomMathf.RoundToInt((flow - 0.5f) * 2f),
                    45,
                    CustomMathf.Min(subWorldSize.SizeY - 3, GlobalRiverWaterLevel + 2));
                if (surface < waterSurface - 4 || surface > waterSurface + 8)
                {
                    continue;
                }

                float rotation = Noise.GetNoise(centerX * 0.12f, 0, centerZ * 0.12f) * CustomMathf.PI;
                relief = ComputeLocalRelief(surfaceCache, subWorldSize, centerX, centerZ, CustomMathf.Max(radiusX, radiusZ) + 4);
                basinStability = 1f - CustomMathf.Clamp01(relief / 12f);
                basinStability = CustomMathf.Clamp01(CustomMathf.Lerp(basinStability, basinStability + candidateScore * 0.25f, 0.5f));
                if (basinStability < 0.3f)
                {
                    continue;
                }

                maxDepth = CustomMathf.Clamp(
                    CustomMathf.RoundToInt(CustomMathf.Lerp(maxDepth * 0.65f, maxDepth * 1.2f, basinStability)),
                    3,
                    8);

                CarveLakeBasin(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ, maxDepth, rotation, erosionRiskField);
                DecorateLakeBanks(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ, rotation, basinStability);

                ConnectLakeToRiver(subWorldBlockData, subWorldSize, hydrologyMask, flowAccumulation, surfaceCache, centerX, centerZ, waterSurface, radiusX, radiusZ);
                ApplyLakeTerraces(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ, rotation);
                AddLakeShorelineBenches(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, centerX, centerZ, waterSurface, radiusX, radiusZ, rotation, basinStability);
                DepositLakeSedimentRings(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ);
                EnhanceLakeShoreVegetation(subWorldBlockData, subWorldSize, centerX, centerZ, radiusX, radiusZ);
                CreateLakeSeeps(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, centerX, centerZ, waterSurface, radiusX, radiusZ);
                AddLakeWetlandPockets(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, centerX, centerZ, waterSurface, radiusX, radiusZ);
                waterSurface = EqualizeLakeWaterTable(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, centerX, centerZ, radiusX, radiusZ, waterSurface);
                AddLakeOverflowChannels(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterSurface, radiusX, radiusZ);
                StabilizeLakeCatchments(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterSurface, radiusX, radiusZ);
                ApplyLakeHydrologyFeedback(subWorldBlockData, subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterSurface, radiusX, radiusZ);
            }
        }

        private static void CarveLakeBasin(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ, int maxDepth, float rotation, float[,] erosionRiskField)
        {
            float cos = CustomMathf.Cos(rotation);
            float sin = CustomMathf.Sin(rotation);
            float radiusXWithPadding = radiusX + 0.75f;
            float radiusZWithPadding = radiusZ + 0.75f;
            float shorelineBlend = CustomMathf.Clamp01(LakeShorelineBlend);
            float rimErosionWeight = CustomMathf.Max(0f, LakeRimErosionWeight);

            int extentX = radiusX + 3;
            int extentZ = radiusZ + 3;

            for (int offsetX = -extentX; offsetX <= extentX; offsetX++)
            {
                for (int offsetZ = -extentZ; offsetZ <= extentZ; offsetZ++)
                {
                    int worldX = centerX + offsetX;
                    int worldZ = centerZ + offsetZ;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float rotatedX = offsetX * cos - offsetZ * sin;
                    float rotatedZ = offsetX * sin + offsetZ * cos;

                    float ellipse = CustomMathf.Sqrt(
                        (rotatedX * rotatedX) / (radiusXWithPadding * radiusXWithPadding) +
                        (rotatedZ * rotatedZ) / (radiusZWithPadding * radiusZWithPadding));
                    float edgeNoise = Noise.GetNoise(
                        (worldX + centerX) * 0.22f,
                        0,
                        (worldZ + centerZ) * 0.22f);
                    float perturbation = edgeNoise * (0.08f + erosionRiskField[worldX, worldZ] * 0.06f);
                    float sdf = ellipse - 1.0f - perturbation;

                    if (sdf <= 0.18f)
                    {
                        int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, worldX, worldZ);
                        if (surface <= 1)
                        {
                            continue;
                        }

                        float bowl = CustomMathf.Clamp01(1.0f - ellipse);
                        float noise = Noise.GetNoise((worldX + centerX) * 0.18f, 0, (worldZ + centerZ) * 0.18f);
                        float depthFactor = CustomMathf.Clamp01(bowl + noise * 0.2f);
                        int columnDepth = CustomMathf.Clamp(maxDepth + CustomMathf.RoundToInt(depthFactor * maxDepth * 0.7f), 2, maxDepth + 3);
                        int waterFloor = CustomMathf.Max(1, waterSurface - columnDepth);

                        for (int y = surface; y >= waterFloor; y--)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        subWorldBlockData[worldX, waterFloor, worldZ].CurrentType = (byte)BlockTileType.SAND;

                        for (int y = waterFloor + 1; y <= waterSurface && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.WATER;
                        }

                        if (waterSurface < GlobalRiverWaterLevel && GlobalRiverWaterLevel < subWorldSize.SizeY)
                        {
                            for (int y = waterSurface + 1; y <= GlobalRiverWaterLevel && y < subWorldSize.SizeY; y++)
                            {
                                subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.WATER;
                            }
                        }
                    }
                    else if (sdf <= 0.45f)
                    {
                        float rimStrength = CustomMathf.Clamp01(0.45f - sdf) / 0.45f;
                        rimStrength *= 1f + erosionRiskField[worldX, worldZ] * rimErosionWeight;
                        rimStrength = CustomMathf.Clamp(rimStrength * (0.65f + shorelineBlend), 0f, 1.35f);
                        ShapeLakeBank(subWorldBlockData, subWorldSize, worldX, worldZ, waterSurface, rimStrength);
                    }
                }
            }
        }

        private static void DecorateLakeBanks(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ, float rotation, float rimStrengthScale)
        {
            float cos = CustomMathf.Cos(rotation);
            float sin = CustomMathf.Sin(rotation);
            float radiusXFeather = radiusX + 4.0f;
            float radiusZFeather = radiusZ + 4.0f;

            for (int dx = -radiusX - 5; dx <= radiusX + 5; dx++)
            {
                for (int dz = -radiusZ - 5; dz <= radiusZ + 5; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float rotatedX = dx * cos - dz * sin;
                    float rotatedZ = dx * sin + dz * cos;

                    float ellipse = CustomMathf.Sqrt(
                        (rotatedX * rotatedX) / (radiusXFeather * radiusXFeather) +
                        (rotatedZ * rotatedZ) / (radiusZFeather * radiusZFeather));

                    if (ellipse <= 1.0f || ellipse > 1.6f)
                    {
                        continue;
                    }

                    float rimStrength = CustomMathf.Clamp01(1.6f - ellipse) * CustomMathf.Clamp01(rimStrengthScale);
                    ShapeLakeBank(subWorldBlockData, subWorldSize, worldX, worldZ, waterSurface, rimStrength * 0.5f);
                }
            }
        }

        private static void ApplyLakeTerraces(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ, float rotation)
        {
            float cos = CustomMathf.Cos(rotation);
            float sin = CustomMathf.Sin(rotation);
            float shallowRadiusX = CustomMathf.Max(2f, radiusX * 0.7f);
            float shallowRadiusZ = CustomMathf.Max(2f, radiusZ * 0.7f);
            float bankRadiusX = radiusX + 4f;
            float bankRadiusZ = radiusZ + 4f;

            int extentX = (int)Math.Ceiling(bankRadiusX);
            int extentZ = (int)Math.Ceiling(bankRadiusZ);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float rotX = dx * cos - dz * sin;
                    float rotZ = dx * sin + dz * cos;

                    float shallowEllipse = CustomMathf.Sqrt(
                        (rotX * rotX) / CustomMathf.Max(1f, shallowRadiusX * shallowRadiusX) +
                        (rotZ * rotZ) / CustomMathf.Max(1f, shallowRadiusZ * shallowRadiusZ));

                    float bankEllipse = CustomMathf.Sqrt(
                        (rotX * rotX) / CustomMathf.Max(1f, bankRadiusX * bankRadiusX) +
                        (rotZ * rotZ) / CustomMathf.Max(1f, bankRadiusZ * bankRadiusZ));

                    if (shallowEllipse <= 1f)
                    {
                        int floor = CustomMathf.Max(1, waterSurface - 2);
                        subWorldBlockData[worldX, floor, worldZ].CurrentType = (byte)BlockTileType.SAND;
                        for (int y = floor + 1; y <= waterSurface && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                    else if (bankEllipse <= 1.2f)
                    {
                        int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, worldX, worldZ);
                        if (surface <= 0)
                        {
                            continue;
                        }

                        subWorldBlockData[worldX, surface, worldZ].CurrentType = (byte)BlockTileType.SAND;
                        if (surface + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[worldX, surface + 1, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }
                    }
                }
            }
        }

        private static void AddLakeShorelineBenches(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            float rotation,
            float basinStability)
        {
            if (basinStability < 0.2f)
            {
                return;
            }

            float cos = CustomMathf.Cos(rotation);
            float sin = CustomMathf.Sin(rotation);
            float radiusXWithPadding = radiusX + 0.75f;
            float radiusZWithPadding = radiusZ + 0.75f;
            int extentX = radiusX + 6;
            int extentZ = radiusZ + 6;

            var bands = new (float inner, float outer, bool floodable)[]
            {
                (1.02f, 1.18f, true),
                (1.18f, 1.36f, false)
            };

            foreach (var band in bands)
            {
                for (int dx = -extentX; dx <= extentX; dx++)
                {
                    for (int dz = -extentZ; dz <= extentZ; dz++)
                    {
                        int worldX = centerX + dx;
                        int worldZ = centerZ + dz;
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                        {
                            continue;
                        }

                        float rotX = dx * cos - dz * sin;
                        float rotZ = dx * sin + dz * cos;
                        float ellipse = CustomMathf.Sqrt(
                            (rotX * rotX) / CustomMathf.Max(1f, radiusXWithPadding * radiusXWithPadding) +
                            (rotZ * rotZ) / CustomMathf.Max(1f, radiusZWithPadding * radiusZWithPadding));

                        if (ellipse < band.inner || ellipse > band.outer)
                        {
                            continue;
                        }

                        if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, worldX, worldZ, out int surface))
                        {
                            continue;
                        }

                        int targetSurface = band.floodable
                            ? CustomMathf.Max(waterSurface + (basinStability > 0.6f ? 0 : 1), 1)
                            : CustomMathf.Max(waterSurface + 2 + (band.inner > 1.18f ? 1 : 0), 1);
                        targetSurface = CustomMathf.Min(surface, targetSurface);

                        for (int y = surface; y > targetSurface; y--)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        float hydrology = CustomMathf.Clamp01(
                            hydrologyMask[
                                CustomMathf.Clamp(worldX, 0, hydrologyMask.GetLength(0) - 1),
                                CustomMathf.Clamp(worldZ, 0, hydrologyMask.GetLength(1) - 1)]);
                        var material = band.floodable
                            ? (hydrology > 0.62f ? BlockTileType.CLAY : BlockTileType.SAND)
                            : (basinStability > 0.5f ? BlockTileType.GRASS : BlockTileType.DIRT);
                        subWorldBlockData[worldX, targetSurface, worldZ].CurrentType = (byte)material;

                        if (band.floodable)
                        {
                            for (int y = targetSurface + 1; y <= waterSurface && y < subWorldSize.SizeY; y++)
                            {
                                subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.WATER;
                            }
                        }
                        else if (targetSurface + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[worldX, targetSurface + 1, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        surfaceCache[worldX, worldZ] = targetSurface;
                    }
                }
            }
        }

        private static void DepositLakeSedimentRings(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ)
        {
            float innerRadiusX = radiusX + 0.5f;
            float innerRadiusZ = radiusZ + 0.5f;
            float outerRadiusX = innerRadiusX + 2.0f;
            float outerRadiusZ = innerRadiusZ + 2.0f;
            int extentX = (int)Math.Ceiling(outerRadiusX + 1.5f);
            int extentZ = (int)Math.Ceiling(outerRadiusZ + 1.5f);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / CustomMathf.Max(1f, innerRadiusX * innerRadiusX) +
                        (dz * dz) / CustomMathf.Max(1f, innerRadiusZ * innerRadiusZ));
                    if (ellipse > 1.35f)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, worldX, worldZ);
                    if (surface <= 1)
                    {
                        continue;
                    }

                    int targetY = CustomMathf.Max(1, CustomMathf.Min(surface, waterSurface) - 1);
                    if (ellipse <= 1.0f)
                    {
                        subWorldBlockData[worldX, targetY, worldZ].CurrentType = (byte)BlockTileType.CLAY;
                    }
                    else if (ellipse <= 1.25f)
                    {
                        subWorldBlockData[worldX, targetY, worldZ].CurrentType = (byte)BlockTileType.SAND;
                    }
                }
            }
        }

        private static void EnhanceLakeShoreVegetation(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int radiusX, int radiusZ)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / CustomMathf.Max(1f, radiusX * radiusX) +
                        (dz * dz) / CustomMathf.Max(1f, radiusZ * radiusZ));
                    if (ellipse > 1.45f)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, worldX, worldZ);
                    if (surface <= 0 || surface + 1 >= subWorldSize.SizeY)
                    {
                        continue;
                    }

                    var topBlock = (BlockTileType)subWorldBlockData[worldX, surface, worldZ].CurrentType;
                    if (topBlock == BlockTileType.SAND && Utilitys.RandomFloat(0f, 1f) < 0.45f)
                    {
                        subWorldBlockData[worldX, surface + 1, worldZ].CurrentType = (byte)BlockTileType.NORMAL_TREE_LEAF;
                    }
                    else if (topBlock == BlockTileType.GRASS && Utilitys.RandomFloat(0f, 1f) < 0.35f)
                    {
                        subWorldBlockData[worldX, surface + 1, worldZ].CurrentType = (byte)BlockTileType.SQAURE_TREE_LEAF;
                    }
                }
            }
        }

        private static void CreateLakeSeeps(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int attempts = 4;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int sampleX = CustomMathf.Clamp(centerX + Utilitys.RandomInteger(-radiusX - 5, radiusX + 6), 1, subWorldSize.SizeX - 2);
                int sampleZ = CustomMathf.Clamp(centerZ + Utilitys.RandomInteger(-radiusZ - 5, radiusZ + 6), 1, subWorldSize.SizeZ - 2);

                float hydrology = hydrologyMask[sampleX, sampleZ];
                if (hydrology < 0.75f)
                {
                    continue;
                }

                int surface = surfaceCache[sampleX, sampleZ];
                if (surface <= waterSurface + 1)
                {
                    continue;
                }

                int trenchDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + (hydrology - 0.7f) * 4f), 1, 4);
                int floor = CustomMathf.Max(surface - trenchDepth, 1);

                for (int y = surface; y >= floor; y--)
                {
                    subWorldBlockData[sampleX, y, sampleZ].CurrentType = (byte)BlockTileType.EMPTY;
                }

                for (int y = floor; y <= waterSurface && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[sampleX, y, sampleZ].CurrentType = (byte)BlockTileType.WATER;
                }

                if (floor - 1 >= 1)
                {
                    subWorldBlockData[sampleX, floor - 1, sampleZ].CurrentType = (byte)BlockTileType.SAND;
                }

                surfaceCache[sampleX, sampleZ] = CustomMathf.Max(surfaceCache[sampleX, sampleZ], floor);
            }
        }

        private static void ConnectLakeToRiver(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, float[,] hydrologyMask, float[,] flowAccumulation, int[,] surfaceCache, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ)
        {
            int searchRadius = CustomMathf.Max(radiusX, radiusZ) + 6;
            float bestScore = 0.09f;
            int bestX = -1;
            int bestZ = -1;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
                    {
                        continue;
                    }

                    CustomVector2 flowDummy;
                    float intensity = EvaluateRiverIntensity(x, z, out flowDummy);
                    float hydrology = hydrologyMask[x, z];
                    float adjustedIntensity = AdjustRiverMask(intensity, hydrology);
                    if (adjustedIntensity >= 0.09f)
                    {
                        continue;
                    }

                    float catchment = flowAccumulation[x, z];
                    float catchmentBias = CustomMathf.Clamp01(catchment / 8f);
                    float candidateScore = adjustedIntensity - hydrology * 0.02f - catchmentBias * 0.015f;
                    float relief = ComputeLocalRelief(surfaceCache, subWorldSize, x, z, 2);
                    candidateScore += CustomMathf.Clamp01(relief / 10f) * 0.01f;

                    if (candidateScore < bestScore)
                    {
                        bestScore = candidateScore;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            if (bestX < 0 || bestZ < 0)
            {
                return;
            }

            float dirX = bestX - centerX;
            float dirZ = bestZ - centerZ;
            float length = CustomMathf.Sqrt(dirX * dirX + dirZ * dirZ);

            int startX = centerX;
            int startZ = centerZ;
            if (length > 0.001f)
            {
                float inv = 1f / length;
                dirX *= inv;
                dirZ *= inv;
                int offset = CustomMathf.Max(CustomMathf.Max(radiusX, radiusZ) - 1, 1);
                startX = centerX + CustomMathf.RoundToInt(dirX * offset);
                startZ = centerZ + CustomMathf.RoundToInt(dirZ * offset);
            }

            startX = CustomMathf.Clamp(startX, 0, subWorldSize.SizeX - 1);
            startZ = CustomMathf.Clamp(startZ, 0, subWorldSize.SizeZ - 1);

            CarveLakeChannel(subWorldBlockData, subWorldSize, startX, startZ, bestX, bestZ, waterSurface);
        }

        private static void CarveLakeChannel(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int startX, int startZ, int endX, int endZ, int waterSurface)
        {
            int dx = endX - startX;
            int dz = endZ - startZ;
            int steps = CustomMathf.Max(CustomMathf.Abs(dx), CustomMathf.Abs(dz));
            if (steps == 0)
            {
                return;
            }

            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                int x = startX + CustomMathf.RoundToInt(dx * t);
                int z = startZ + CustomMathf.RoundToInt(dz * t);
                if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
                {
                    continue;
                }

                int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                if (surface <= 0)
                {
                    continue;
                }

                int carveFloor = CustomMathf.Max(1, waterSurface - 2);
                if (surface > waterSurface + 1)
                {
                    for (int y = surface; y > waterSurface + 1; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                    surface = waterSurface + 1;
                }

                for (int y = surface; y >= carveFloor; y--)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                }

                if (carveFloor - 1 >= 1)
                {
                    subWorldBlockData[x, carveFloor - 1, z].CurrentType = (byte)BlockTileType.SAND;
                }

                for (int y = carveFloor; y <= waterSurface && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                }

                if (waterSurface + 1 < subWorldSize.SizeY)
                {
                    subWorldBlockData[x, waterSurface + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
                }

                for (int widen = -1; widen <= 1; widen++)
                {
                    if (widen == 0)
                    {
                        continue;
                    }

                    int wx = x + widen;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(wx, 0, z, subWorldSize))
                    {
                        continue;
                    }

                    int ws = FindSurfaceLevel(subWorldBlockData, subWorldSize, wx, z);
                    if (ws <= 0)
                    {
                        continue;
                    }

                    if (ws > waterSurface)
                    {
                        subWorldBlockData[wx, ws, z].CurrentType = (byte)BlockTileType.SAND;
                    }
                }
            }
        }

        private static void ShapeLakeBank(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int x, int z, int waterSurface, float rimStrength)
        {
            if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
            {
                return;
            }

            int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
            if (surface <= 0)
            {
                return;
            }

            int maxDrop = CustomMathf.Max(1, CustomMathf.RoundToInt(CustomMathf.Lerp(3f, 1f, rimStrength)));

            if (surface > waterSurface + maxDrop)
            {
                int target = CustomMathf.Max(waterSurface + maxDrop, 1);
                for (int y = surface; y > target; y--)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                }
                surface = target;
            }

            if (surface <= waterSurface)
            {
                for (int y = surface; y <= waterSurface && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                }

                if (waterSurface < GlobalRiverWaterLevel && GlobalRiverWaterLevel < subWorldSize.SizeY)
                {
                    for (int y = waterSurface + 1; y <= GlobalRiverWaterLevel && y < subWorldSize.SizeY; y++)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                    }
                    surface = CustomMathf.Min(GlobalRiverWaterLevel, subWorldSize.SizeY - 1);
                }
                else
                {
                    surface = CustomMathf.Min(waterSurface, subWorldSize.SizeY - 1);
                }
            }
            else
            {
                subWorldBlockData[x, surface, z].CurrentType = (byte)BlockTileType.SAND;
            }

            if (surface + 1 < subWorldSize.SizeY)
            {
                subWorldBlockData[x, surface + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
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

                      SmoothPondBanks(subWorldBlockData, subWorldSize, pondX, pondZ, pondRadius, surfaceY);
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
            var caveStabilityField = BuildCaveStabilityField(subWorldBlockData, subWorldSize);
            SmoothScalarField(caveStabilityField, CaveStabilitySmoothIterations, CaveStabilitySmoothBlend);

            // 대형 동굴 시스템 생성
            GenerateLargeCaveSystem(subWorldBlockData, subWorldSize);
            
            // 소형 동굴 방 생성
            GenerateSmallCaves(subWorldBlockData, subWorldSize);
            
            // 지하 호수 생성
            GenerateUndergroundLakes(subWorldBlockData, subWorldSize);

            // 노이즈 기반 동굴 추가 - 청크 경계 일관성을 유지한다.
            GenerateNoiseCaves(subWorldBlockData, subWorldSize);
            ApplyHydrologyDrivenCavePools(subWorldBlockData, subWorldSize);
            AddCavePillars(subWorldBlockData, subWorldSize);
            int[,] caveSurfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] caveHydrologyMask = BuildHydrologyMask(subWorldSize, caveSurfaceCache);
            float[,] caveFlowAccumulation = BuildFlowAccumulation(caveSurfaceCache, subWorldSize);
            BlendHydrologySeams(subWorldSize, caveHydrologyMask, caveFlowAccumulation);
            StabilizeHydrologyGradients(subWorldSize, caveHydrologyMask, caveFlowAccumulation, caveSurfaceCache);
            SmoothHydrologyFields(caveHydrologyMask, caveFlowAccumulation);
            NormalizeHydrologyRange(subWorldSize, caveHydrologyMask, caveFlowAccumulation);
            ClampHydrologyToWaterTable(subWorldSize, caveHydrologyMask, caveFlowAccumulation, caveSurfaceCache);
            RelaxHydrologySeams(subWorldSize, caveHydrologyMask, caveFlowAccumulation);
            AnchorHydrologyToSlope(subWorldSize, caveSurfaceCache, caveHydrologyMask, caveFlowAccumulation);
            AddHydrologySupportColumns(subWorldBlockData, subWorldSize, caveStabilityField, caveHydrologyMask, caveFlowAccumulation);
            AddCaveShelfBands(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveStabilityField);
            AddCaveVentilationShafts(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveStabilityField);
            AddCaveAquiferChannels(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveFlowAccumulation);
            AddCaveRibbonTerraces(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveStabilityField, caveFlowAccumulation);
            ApplyCaveHydrologyErosion(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveFlowAccumulation);
            AddCaveDripstoneClusters(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveStabilityField);
            StabilizeMoistCaveCeilings(subWorldBlockData, subWorldSize, caveSurfaceCache, caveHydrologyMask, caveFlowAccumulation);
            IntegrateKarstSinkholes(subWorldBlockData, subWorldSize);
        }

        private static float[,] BuildCaveStabilityField(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            float[,] flowAccumulation = BuildFlowAccumulation(surfaceCache, subWorldSize);
            BlendHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            StabilizeHydrologyGradients(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            SmoothHydrologyFields(hydrologyMask, flowAccumulation);
            NormalizeHydrologyRange(subWorldSize, hydrologyMask, flowAccumulation);
            ClampHydrologyToWaterTable(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            RelaxHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            AnchorHydrologyToSlope(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] riparianSaturation = BuildRiparianSaturationMap(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] erosionRiskField = BuildErosionRiskField(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] riverIntensity = BuildRiverIntensityPreview(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation, riparianSaturation, erosionRiskField);
            var field = new float[subWorldSize.SizeX, subWorldSize.SizeZ];

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        field[x, z] = 0f;
                        continue;
                    }

                    float depthFactor = CustomMathf.Clamp01(surface / (float)subWorldSize.SizeY);
                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float roughness = CustomMathf.Clamp01((Noise.GetNoise((x + 17) * 0.08f, 0, (z - 9) * 0.08f) + 1f) * 0.5f);
                    float warp = CustomMathf.Clamp01((Noise.GetNoise((x + 5f) * 0.14f, 0, (z - 7f) * 0.14f) + 1f) * 0.5f);
                    float riverPressure = CustomMathf.Clamp01(1f - riverIntensity[x, z] / 0.07f);
                    float waterTableBias = CustomMathf.Clamp01((GlobalRiverWaterLevel - surface) / 48f);
                    float moisturePenalty = CustomMathf.Clamp01(hydrology * 0.35f + flow * 0.22f);
                    float roughnessBlend = (roughness * 0.7f + warp * 0.3f) * CaveRoughnessWeight;
                    float saturation = hydrology * CaveHydrologyWeight + flow * CaveFlowWeight + (1f - depthFactor) * CaveDepthWeight + roughnessBlend;
                    float suppression = 1f - CaveRiverSuppressionWeight * (1f - riverPressure);
                    float supportBoost = 1f + waterTableBias * 0.35f;
                    float stability = saturation * supportBoost * suppression;
                    stability *= 1f - moisturePenalty * 0.35f;
                    field[x, z] = CustomMathf.Clamp01(stability);
                }
            }

            return field;
        }

        private static void StabilizeMoistCaveCeilings(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, float[,] hydrologyMask, float[,] flowAccumulation)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float moisture = CustomMathf.Max(hydrologyMask[x, z], CustomMathf.Clamp01(flowAccumulation[x, z] / 6f));
                    if (moisture < 0.55f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 5)
                    {
                        continue;
                    }

                    int scanStart = CustomMathf.Max(1, surface - 6);
                    bool insideAir = false;
                    int airTop = -1;
                    int airBottom = -1;

                    for (int y = surface; y >= scanStart; y--)
                    {
                        bool isEmpty = subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.EMPTY ||
                                       subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.WATER;
                        if (isEmpty && !insideAir)
                        {
                            insideAir = true;
                            airTop = y;
                        }
                        else if (!isEmpty && insideAir)
                        {
                            airBottom = y + 1;
                            break;
                        }
                    }

                    if (!insideAir || airTop == -1)
                    {
                        continue;
                    }

                    if (airBottom == -1)
                    {
                        airBottom = scanStart;
                    }

                    int roofThickness = surface - airTop;
                    if (roofThickness >= 3)
                    {
                        continue;
                    }

                    int fillTop = CustomMathf.Clamp(surface - 1, 2, subWorldSize.SizeY - 2);
                    int fillBottom = CustomMathf.Max(airBottom, fillTop - 2);
                    float sealStrength = CustomMathf.Clamp01((0.75f - roofThickness * 0.25f) + moisture * 0.35f);

                    for (int y = fillTop; y >= fillBottom; y--)
                    {
                        if (Utilitys.RandomFloat(0f, 1f) <= sealStrength)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                            surfaceCache[x, z] = CustomMathf.Max(surfaceCache[x, z], y);
                        }
                    }
                }
            }
        }

        private static void AddHydrologySupportColumns(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            float[,] stabilityField,
            float[,] hydrologyMask,
            float[,] flowAccumulation)
        {
            float supportThreshold = CustomMathf.Clamp01(CaveSupportDensity);
            if (supportThreshold <= 0.01f)
            {
                supportThreshold = 0.58f;
            }

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float moisture = CustomMathf.Max(hydrology, flow);
                    float stability = stabilityField[x, z] * (1f + moisture * CaveSupportHydrationBias + flow * CaveSupportFlowBias);
                    float adaptiveThreshold = CustomMathf.Clamp01(supportThreshold * (0.85f + moisture * 0.4f));
                    if (stability < adaptiveThreshold)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                    if (surface <= 4)
                    {
                        continue;
                    }

                    bool insideAir = false;
                    int top = -1;
                    int bottom = -1;
                    int scanStart = CustomMathf.Min(surface - 2, subWorldSize.SizeY - 3);

                    for (int y = scanStart; y >= 5; y--)
                    {
                        bool isEmpty = subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.EMPTY ||
                                       subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.WATER;
                        if (isEmpty)
                        {
                            if (!insideAir)
                            {
                                insideAir = true;
                                top = y;
                            }
                        }
                        else if (insideAir)
                        {
                            bottom = y + 1;
                            break;
                        }
                    }

                    if (!insideAir || top == -1 || bottom == -1)
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom + 1;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    float densityFactor = CustomMathf.Clamp01(stability * 0.55f + adaptiveThreshold * 0.45f + moisture * 0.25f);
                    int span = CustomMathf.Clamp(CustomMathf.RoundToInt(cavityHeight * (0.18f + densityFactor * 0.45f + flow * 0.15f)), 3, cavityHeight - 1);
                    int offset = Utilitys.RandomInteger(0, CustomMathf.Max(1, cavityHeight - span));
                    int columnBase = bottom + offset;
                    int columnTop = CustomMathf.Min(top - 1, columnBase + span);
                    bool wide = stability > 0.82f || densityFactor > 0.75f || moisture > 0.55f;
                    int step = (densityFactor > 0.65f || flow > 0.35f) ? 1 : 2;

                    for (int y = columnBase; y <= columnTop; y++)
                    {
                        if ((y - columnBase) % step == 0 || y == columnTop)
                        {
                            PlaceSupportNode(subWorldBlockData, subWorldSize, x, y, z, wide);
                        }
                    }
                }
            }
        }

        private static void AddCaveDripstoneClusters(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] stabilityField)
        {
            for (int x = 2; x < subWorldSize.SizeX - 2; x++)
            {
                for (int z = 2; z < subWorldSize.SizeZ - 2; z++)
                {
                    float moisture = CustomMathf.Max(hydrologyMask[x, z], stabilityField[x, z]);
                    if (moisture < 0.35f)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0 || surface - top < 3)
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    float spawnChance = 0.03f + moisture * 0.07f;
                    spawnChance += Noise.GetNoise((x + 19.45f) * 0.09f, 0, (z - 4.31f) * 0.09f) * 0.02f;
                    if (Utilitys.RandomFloat(0f, 1f) > spawnChance)
                    {
                        continue;
                    }

                    int stalactiteLength = CustomMathf.Clamp(
                        CustomMathf.RoundToInt(cavityHeight * (0.18f + moisture * 0.25f)),
                        2,
                        cavityHeight - 2);
                    int stalagmiteLength = CustomMathf.Clamp(
                        CustomMathf.RoundToInt(cavityHeight * (0.12f + moisture * 0.2f)),
                        2,
                        cavityHeight - 2);

                    int available = CustomMathf.Max(2, cavityHeight - 2);
                    if (stalactiteLength + stalagmiteLength >= available)
                    {
                        int excess = (stalactiteLength + stalagmiteLength) - available;
                        int trimTop = excess / 2;
                        int trimBottom = excess - trimTop;
                        stalactiteLength = CustomMathf.Max(2, stalactiteLength - trimTop);
                        stalagmiteLength = CustomMathf.Max(2, stalagmiteLength - trimBottom);
                    }

                    for (int y = top; y > top - stalactiteLength && y > bottom + 1; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                    }

                    for (int y = bottom; y < bottom + stalagmiteLength && y < top; y++)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_BIG;
                    }

                    if (moisture > 0.72f)
                    {
                        int puddleFloor = CustomMathf.Clamp(bottom + 1, 1, subWorldSize.SizeY - 2);
                        subWorldBlockData[x, puddleFloor, z].CurrentType = (byte)BlockTileType.WATER;
                    }

                    int[,] offsets =
                    {
                        { 1, 0 },
                        { -1, 0 },
                        { 0, 1 },
                        { 0, -1 }
                    };

                    int floorBand = CustomMathf.Clamp(bottom + 1, 1, subWorldSize.SizeY - 2);
                    for (int i = 0; i < offsets.GetLength(0); i++)
                    {
                        int nx = x + offsets[i, 0];
                        int nz = z + offsets[i, 1];
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, floorBand, nz, subWorldSize))
                        {
                            continue;
                        }

                        if (subWorldBlockData[nx, floorBand, nz].CurrentType == (byte)BlockTileType.EMPTY)
                        {
                            subWorldBlockData[nx, floorBand, nz].CurrentType = (byte)BlockTileType.SAND;
                        }
                    }
                }
            }
        }

        private static void ApplyCaveHydrologyErosion(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation)
        {
            int[,] neighborOffsets =
            {
                { 1, 0 },
                { -1, 0 },
                { 0, 1 },
                { 0, -1 }
            };

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float moisture = CustomMathf.Max(hydrology, flow);

                    if (moisture < 0.55f)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 5)
                    {
                        continue;
                    }

                    int streamHeight = CustomMathf.Clamp(
                        CustomMathf.RoundToInt(cavityHeight * (0.25f + flow * 0.35f)),
                        2,
                        cavityHeight - 1);
                    int streamTop = CustomMathf.Min(top - 1, bottom + streamHeight);
                    int thickness = CustomMathf.Clamp(
                        CustomMathf.RoundToInt(1 + (moisture - 0.45f) * 6f),
                        1,
                        CustomMathf.Max(2, streamHeight));
                    int streamBottom = CustomMathf.Max(bottom + 1, streamTop - thickness);
                    bool fillWithWater = hydrology > 0.68f;
                    byte fillBlock = (byte)(fillWithWater ? BlockTileType.WATER : BlockTileType.EMPTY);
                    int floor = CustomMathf.Max(streamBottom - 1, 1);

                    for (int y = streamBottom; y <= streamTop && y < subWorldSize.SizeY; y++)
                    {
                        subWorldBlockData[x, y, z].CurrentType = fillBlock;
                    }

                    if (floor >= 1)
                    {
                        subWorldBlockData[x, floor, z].CurrentType = fillWithWater
                            ? (byte)BlockTileType.CLAY
                            : (byte)BlockTileType.STONE_SMALL;
                    }

                    for (int i = 0; i < neighborOffsets.GetLength(0); i++)
                    {
                        int nx = x + neighborOffsets[i, 0];
                        int nz = z + neighborOffsets[i, 1];
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, floor, nz, subWorldSize))
                        {
                            continue;
                        }

                        float cue = (Noise.GetNoise((nx + 11f) * 0.19f, floor * 0.05f, (nz - 7f) * 0.19f) + 1f) * 0.5f;
                        if (cue < moisture * 0.45f)
                        {
                            continue;
                        }

                        int linkBottom = streamBottom;
                        int linkTop = CustomMathf.Min(streamTop, linkBottom + 2 + CustomMathf.RoundToInt(flow * 3f));
                    for (int y = linkBottom; y <= linkTop && y < subWorldSize.SizeY; y++)
                    {
                        BlockTileType existing = (BlockTileType)subWorldBlockData[nx, y, nz].CurrentType;
                        if (existing == BlockTileType.STONE_SMALL || existing == BlockTileType.STONE_BIG)
                        {
                            subWorldBlockData[nx, y, nz].CurrentType = (byte)(fillWithWater ? BlockTileType.WATER : BlockTileType.EMPTY);
                        }
                    }

                    ExtendCaveHydrologyRunoff(
                        subWorldBlockData,
                        subWorldSize,
                        surfaceCache,
                        hydrologyMask,
                        flowAccumulation,
                        x,
                        z,
                        streamBottom,
                        streamTop,
                        fillWithWater);
                }
            }
        }
        }

        private static void AddLakeWetlandPockets(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;
            int pockets = 0;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 1 || x >= subWorldSize.SizeX - 1 || z < 1 || z >= subWorldSize.SizeZ - 1)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0f, (radiusX + 0.5f) * (radiusX + 0.5f)) +
                        (dz * dz) / Math.Max(1.0f, (radiusZ + 0.5f) * (radiusZ + 0.5f)));
                    if (ellipse <= 1.05 || ellipse >= 1.45)
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float spawnNoise = (Noise.GetNoise((x + 19f) * 0.21f, 0, (z - 7f) * 0.21f) + 1f) * 0.5f;
                    float spawnWeight = hydrology * 0.65f + CustomMathf.Clamp01((float)(ellipse - 1.05f) * 1.4f) * 0.25f + spawnNoise * 0.15f;
                    if (spawnWeight < 0.68f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface < waterSurface - 4 || surface > waterSurface + 6)
                    {
                        continue;
                    }

                    int pocketDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + (hydrology - 0.45f) * 3.5f), 1, 3);
                    int floor = CustomMathf.Max(surface - pocketDepth, 1);
                    for (int y = surface; y >= floor; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    if (hydrology > 0.63f)
                    {
                        subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.WATER;
                        if (floor + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[x, floor + 1, z].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                    else
                    {
                        subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.SAND;
                    }

                    surfaceCache[x, z] = floor;

                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int nz = -1; nz <= 1; nz++)
                        {
                            if (nx == 0 && nz == 0)
                            {
                                continue;
                            }

                            int rimX = x + nx;
                            int rimZ = z + nz;
                            if (!WorldGenerateUtils.CheckSubWorldBoundary(rimX, floor, rimZ, subWorldSize))
                            {
                                continue;
                            }

                            int rimSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, rimX, rimZ);
                            if (rimSurface <= 0)
                            {
                                continue;
                            }

                            BlockTileType rimType = hydrology > 0.63f ? BlockTileType.SAND : BlockTileType.GRASS;
                            subWorldBlockData[rimX, rimSurface, rimZ].CurrentType = (byte)rimType;
                        }
                    }

                    pockets++;
                    if (pockets >= 6)
                    {
                        return;
                    }
                }
            }
        }

        private static void ExtendCaveHydrologyRunoff(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            int originX,
            int originZ,
            int streamBottom,
            int streamTop,
            bool floodChannel)
        {
            CustomVector2 gradient = ComputeHydrologyGradientVector(hydrologyMask, originX, originZ);
            if (gradient.sqrMagnitude < CustomVector2.kEpsilon)
            {
                return;
            }

            float baseHydrology = hydrologyMask[originX, originZ];
            float baseFlow = CustomMathf.Clamp01(flowAccumulation[originX, originZ] / 6f);
            float pressure = CustomMathf.Max(baseHydrology, baseFlow);
            int steps = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + pressure * 3f), 1, 4);

            float cursorX = originX;
            float cursorZ = originZ;
            for (int step = 0; step < steps; step++)
            {
                cursorX += gradient.x;
                cursorZ += gradient.y;
                int targetX = CustomMathf.Clamp(CustomMathf.RoundToInt(cursorX), 1, subWorldSize.SizeX - 2);
                int targetZ = CustomMathf.Clamp(CustomMathf.RoundToInt(cursorZ), 1, subWorldSize.SizeZ - 2);

                if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, targetX, targetZ, out int top, out int bottom))
                {
                    continue;
                }

                int cavityHeight = top - bottom;
                if (cavityHeight < 4)
                {
                    continue;
                }

                float neighborHydrology = hydrologyMask[targetX, targetZ];
                float neighborFlow = CustomMathf.Clamp01(flowAccumulation[targetX, targetZ] / 6f);
                float neighborMoisture = CustomMathf.Max(neighborHydrology, neighborFlow);
                if (neighborMoisture < 0.45f)
                {
                    continue;
                }

                int localThickness = CustomMathf.Clamp(
                    CustomMathf.RoundToInt(1 + neighborMoisture * 3f + pressure),
                    1,
                    CustomMathf.Max(2, cavityHeight - 1));
                int localTop = CustomMathf.Min(top - 1, bottom + localThickness + 1);
                int localBottom = CustomMathf.Max(bottom + 1, localTop - localThickness);
                int floor = CustomMathf.Max(localBottom - 1, 1);
                byte fillBlock = (byte)(floodChannel ? BlockTileType.WATER : BlockTileType.EMPTY);

                for (int y = localBottom; y <= localTop && y < subWorldSize.SizeY; y++)
                {
                    subWorldBlockData[targetX, y, targetZ].CurrentType = fillBlock;
                }

                subWorldBlockData[targetX, floor, targetZ].CurrentType = (byte)(floodChannel ? BlockTileType.CLAY : BlockTileType.STONE_SMALL);
            }
        }

        private static CustomVector2 ComputeHydrologyGradientVector(float[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;
            float gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            float gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
            var gradient = new CustomVector2(gx, gz);
            return gradient.sqrMagnitude < CustomVector2.kEpsilon ? CustomVector2.zero : gradient.normalized;
        }

        private static int EqualizeLakeWaterTable(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            int waterSurface)
        {
            float rimHydrology = SampleLakeRingHydrology(hydrologyMask, centerX, centerZ, radiusX, radiusZ, 1.05f, 1.4f);
            float basinHydrology = hydrologyMask[CustomMathf.Clamp(centerX, 0, subWorldSize.SizeX - 1), CustomMathf.Clamp(centerZ, 0, subWorldSize.SizeZ - 1)];
            float pressureDelta = (rimHydrology - basinHydrology) * 3.5f;
            int targetLevel = CustomMathf.Clamp(waterSurface + CustomMathf.RoundToInt(pressureDelta), waterSurface - 2, waterSurface + 3);
            targetLevel = CustomMathf.Clamp(targetLevel, 32, CustomMathf.Min(GlobalRiverWaterLevel + 4, subWorldSize.SizeY - 2));

            if (targetLevel == waterSurface)
            {
                return waterSurface;
            }

            AdjustLakeWaterColumns(subWorldBlockData, subWorldSize, surfaceCache, centerX, centerZ, radiusX, radiusZ, waterSurface, targetLevel);
            return targetLevel;
        }

        private static float SampleLakeRingHydrology(
            float[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            float inner,
            float outer)
        {
            float sum = 0f;
            int samples = 0;
            int limitX = radiusX + 6;
            int limitZ = radiusZ + 6;

            for (int dx = -limitX; dx <= limitX; dx++)
            {
                for (int dz = -limitZ; dz <= limitZ; dz++)
                {
                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / Math.Max(1.0f, (radiusX + 0.5f) * (radiusX + 0.5f)) +
                        (dz * dz) / Math.Max(1.0f, (radiusZ + 0.5f) * (radiusZ + 0.5f)));

                    if (ellipse < inner || ellipse > outer)
                    {
                        continue;
                    }

                    int sampleX = CustomMathf.Clamp(centerX + dx, 0, hydrologyMask.GetLength(0) - 1);
                    int sampleZ = CustomMathf.Clamp(centerZ + dz, 0, hydrologyMask.GetLength(1) - 1);
                    sum += hydrologyMask[sampleX, sampleZ];
                    samples++;
                }
            }

            if (samples == 0)
            {
                return hydrologyMask[CustomMathf.Clamp(centerX, 0, hydrologyMask.GetLength(0) - 1), CustomMathf.Clamp(centerZ, 0, hydrologyMask.GetLength(1) - 1)];
            }

            return sum / samples;
        }

        private static void AdjustLakeWaterColumns(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            int previousSurface,
            int targetSurface)
        {
            int maxX = radiusX + 4;
            int maxZ = radiusZ + 4;

            for (int dx = -maxX; dx <= maxX; dx++)
            {
                for (int dz = -maxZ; dz <= maxZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
                    {
                        continue;
                    }

                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / Math.Max(1.0f, (radiusX + 0.35f) * (radiusX + 0.35f)) +
                        (dz * dz) / Math.Max(1.0f, (radiusZ + 0.35f) * (radiusZ + 0.35f)));

                    if (ellipse <= 1.05f)
                    {
                        int floor = FindLakeFloor(subWorldBlockData, subWorldSize, x, z, targetSurface);
                        floor = CustomMathf.Max(1, floor);
                        subWorldBlockData[x, floor, z].CurrentType = (byte)BlockTileType.SAND;

                        int waterTop = CustomMathf.Min(targetSurface, subWorldSize.SizeY - 2);
                        for (int y = floor + 1; y <= waterTop && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                        }

                        int maxClear = CustomMathf.Max(previousSurface, waterTop);
                        for (int y = waterTop + 1; y <= maxClear && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        surfaceCache[x, z] = waterTop;
                    }
                    else if (ellipse <= 1.35f)
                    {
                        double rimStrength = Math.Clamp(1.35 - ellipse, 0.0, 0.6);
                        SculptLakeBank(subWorldBlockData, subWorldSize, x, z, targetSurface, rimStrength + 0.2f);
                    }
                }
            }
        }

        private static int FindLakeFloor(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int x,
            int z,
            int searchStart)
        {
            for (int y = CustomMathf.Min(searchStart, subWorldSize.SizeY - 2); y >= 1; y--)
            {
                BlockTileType block = (BlockTileType)subWorldBlockData[x, y, z].CurrentType;
                if (block != BlockTileType.EMPTY && block != BlockTileType.WATER)
                {
                    return y;
                }
            }

            return 1;
        }

        private static void AddLakeOverflowChannels(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            (int dx, int dz, int extent)[] directions =
            {
                (1, 0, radiusX + 2),
                (-1, 0, radiusX + 2),
                (0, 1, radiusZ + 2),
                (0, -1, radiusZ + 2)
            };

            (int dx, int dz, float weight) primary = (0, 0, 0f);
            (int dx, int dz, float weight) secondary = (0, 0, 0f);

            foreach (var dir in directions)
            {
                int edgeX = centerX + dir.dx * dir.extent;
                int edgeZ = centerZ + dir.dz * dir.extent;
                if (edgeX < 1 || edgeX >= subWorldSize.SizeX - 1 || edgeZ < 1 || edgeZ >= subWorldSize.SizeZ - 1)
                {
                    continue;
                }

                float hydrology = hydrologyMask[edgeX, edgeZ];
                float accumulation = flowAccumulation[edgeX, edgeZ];
                int neighborSurface = surfaceCache[edgeX, edgeZ];
                if (neighborSurface <= 0)
                {
                    neighborSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, edgeX, edgeZ);
                    if (neighborSurface <= 0)
                    {
                        continue;
                    }
                    surfaceCache[edgeX, edgeZ] = neighborSurface;
                }

                float slope = CustomMathf.Clamp01((waterSurface - neighborSurface) / 8f);
                float weight = CustomMathf.Clamp01(hydrology * 0.6f + CustomMathf.Clamp01(accumulation / 4f) * 0.4f);
                weight *= 0.55f + slope;
                if (weight <= 0.35f)
                {
                    continue;
                }

                if (weight > primary.weight)
                {
                    secondary = primary;
                    primary = (dir.dx, dir.dz, weight);
                }
                else if (weight > secondary.weight)
                {
                    secondary = (dir.dx, dir.dz, weight);
                }
            }

            if (primary.weight > 0f)
            {
                CarveLakeOverflowChannel(
                    subWorldBlockData,
                    subWorldSize,
                    surfaceCache,
                    centerX,
                    centerZ,
                    primary.dx,
                    primary.dz,
                    waterSurface,
                    primary.weight);
            }

            if (secondary.weight > 0f)
            {
                CarveLakeOverflowChannel(
                    subWorldBlockData,
                    subWorldSize,
                    surfaceCache,
                    centerX,
                    centerZ,
                    secondary.dx,
                    secondary.dz,
                    waterSurface,
                    secondary.weight * 0.85f);
            }
        }

        private static void CarveLakeOverflowChannel(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            int originX,
            int originZ,
            int dirX,
            int dirZ,
            int waterSurface,
            float weight)
        {
            if (dirX == 0 && dirZ == 0)
            {
                return;
            }

            int length = CustomMathf.Clamp(CustomMathf.RoundToInt(3f + weight * 5f), 2, 8);
            int currentX = originX + dirX * 2;
            int currentZ = originZ + dirZ * 2;

            for (int step = 0; step < length; step++)
            {
                if (!WorldGenerateUtils.CheckSubWorldBoundary(currentX, 0, currentZ, subWorldSize))
                {
                    break;
                }

                int surface = surfaceCache[currentX, currentZ];
                if (surface <= 1)
                {
                    surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, currentX, currentZ);
                    if (surface <= 1)
                    {
                        break;
                    }
                    surfaceCache[currentX, currentZ] = surface;
                }

                int target = CustomMathf.Max(waterSurface - 2 - step, 1);
                if (surface > target)
                {
                    for (int y = surface; y > target; y--)
                    {
                        subWorldBlockData[currentX, y, currentZ].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                }

                int sandY = CustomMathf.Max(target - 1, 1);
                subWorldBlockData[currentX, sandY, currentZ].CurrentType = (byte)BlockTileType.SAND;
                for (int y = target; y <= CustomMathf.Min(waterSurface, subWorldSize.SizeY - 1); y++)
                {
                    subWorldBlockData[currentX, y, currentZ].CurrentType = (byte)BlockTileType.WATER;
                }

                surfaceCache[currentX, currentZ] = target;

                int bankX = currentX + dirZ;
                int bankZ = currentZ - dirX;
                if (WorldGenerateUtils.CheckSubWorldBoundary(bankX, target, bankZ, subWorldSize))
                {
                    ShapeLakeBank(subWorldBlockData, subWorldSize, bankX, bankZ, waterSurface, 0.6f);
                }

                bankX = currentX - dirZ;
                bankZ = currentZ + dirX;
                if (WorldGenerateUtils.CheckSubWorldBoundary(bankX, target, bankZ, subWorldSize))
                {
                    ShapeLakeBank(subWorldBlockData, subWorldSize, bankX, bankZ, waterSurface, 0.55f);
                }

                currentX += dirX;
                currentZ += dirZ;
            }
        }

        private static void AddCaveShelfBands(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] stabilityField)
        {
            for (int x = 2; x < subWorldSize.SizeX - 2; x++)
            {
                for (int z = 2; z < subWorldSize.SizeZ - 2; z++)
                {
                    float stability = stabilityField[x, z];
                    if (stability < 0.42f)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float wetness = CustomMathf.Clamp01(hydrology * 0.7f + stability * 0.25f);
                    int shelfThickness = CustomMathf.Clamp(CustomMathf.RoundToInt(CustomMathf.Lerp(1f, 4f, wetness)), 1, 4);
                    int shelfOffset = CustomMathf.Clamp(
                        CustomMathf.RoundToInt(cavityHeight * (0.35f + wetness * 0.2f)),
                        2,
                        cavityHeight - 2);
                    int shelfY = CustomMathf.Clamp(bottom + shelfOffset, bottom + 2, top - 2);
                    int radius = wetness > 0.65f ? 2 : 1;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int worldX = x + dx;
                            int worldZ = z + dz;
                            if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, shelfY, worldZ, subWorldSize))
                            {
                                continue;
                            }

                            float falloff = 1f - (CustomMathf.Abs(dx) + CustomMathf.Abs(dz)) * 0.35f;
                            if (falloff <= 0f)
                            {
                                continue;
                            }

                            for (int y = shelfY; y > shelfY - shelfThickness && y > bottom + 1; y--)
                            {
                                var current = (BlockTileType)subWorldBlockData[worldX, y, worldZ].CurrentType;
                                if (current == BlockTileType.EMPTY || current == BlockTileType.WATER)
                                {
                                    subWorldBlockData[worldX, y, worldZ].CurrentType =
                                        wetness > 0.6f ? (byte)BlockTileType.STONE_BIG : (byte)BlockTileType.STONE_SMALL;
                                }
                            }

                            int guardY = CustomMathf.Min(subWorldSize.SizeY - 2, shelfY + 1);
                            if (subWorldBlockData[worldX, guardY, worldZ].CurrentType != (byte)BlockTileType.EMPTY)
                            {
                                subWorldBlockData[worldX, guardY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                            }
                        }
                    }
                }
            }
        }

        private static void AddCaveVentilationShafts(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] stabilityField)
        {
            for (int x = 2; x < subWorldSize.SizeX - 2; x++)
            {
                for (int z = 2; z < subWorldSize.SizeZ - 2; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 6)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (surface - top < 4)
                    {
                        continue;
                    }

                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float instability = 1f - CustomMathf.Clamp01(stabilityField[x, z]);
                    if (instability < 0.08f)
                    {
                        continue;
                    }

                    float spawnWeight = CustomMathf.Clamp01(hydrology * 0.55f + instability * 0.6f);
                    float selector = SampleDeterministicNoise(x, z, 173);
                    if (spawnWeight < 0.35f || selector > spawnWeight)
                    {
                        continue;
                    }

                    int ventTop = CustomMathf.Min(surface - 1, subWorldSize.SizeY - 2);
                    int ventBottom = CustomMathf.Clamp(top, bottom + 1, ventTop - 1);

                    for (int y = ventTop; y >= ventBottom; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    surfaceCache[x, z] = CustomMathf.Max(ventBottom - 1, 1);

                    if (hydrology > 0.68f)
                    {
                        int poolY = CustomMathf.Max(ventBottom - 1, 1);
                        subWorldBlockData[x, poolY, z].CurrentType = (byte)BlockTileType.WATER;
                    }

                    ReinforceVentLip(subWorldBlockData, subWorldSize, surfaceCache, x, z);
                }
            }
        }

        private static void ReinforceVentLip(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            int centerX,
            int centerZ)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int rimX = centerX + dx;
                    int rimZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(rimX, 0, rimZ, subWorldSize))
                    {
                        continue;
                    }

                    int rimSurface = surfaceCache[rimX, rimZ];
                    if (rimSurface <= 0)
                    {
                        rimSurface = FindSurfaceLevel(subWorldBlockData, subWorldSize, rimX, rimZ);
                        if (rimSurface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[rimX, rimZ] = rimSurface;
                    }

                    if (subWorldBlockData[rimX, rimSurface, rimZ].CurrentType == (byte)BlockTileType.EMPTY)
                    {
                    subWorldBlockData[rimX, rimSurface, rimZ].CurrentType = (byte)BlockTileType.STONE_SMALL;
                }
            }
        }

        private static void StabilizeLakeCatchments(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
                    {
                        continue;
                    }

                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / Math.Max(1.0f, (radiusX + 0.6f) * (radiusX + 0.6f)) +
                        (dz * dz) / Math.Max(1.0f, (radiusZ + 0.6f) * (radiusZ + 0.6f)));
                    if (ellipse < 0.9f || ellipse > 1.6f)
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float pressure = CustomMathf.Max(hydrology, flow);
                    if (pressure < 0.55f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1 || surface < waterSurface - 4)
                    {
                        continue;
                    }

                    int erosionDepth = CustomMathf.Clamp(CustomMathf.RoundToInt((pressure - 0.45f) * 6f), 1, 4);
                    int floor = CustomMathf.Max(surface - erosionDepth, 1);
                    for (int y = surface; y >= floor && y >= 1; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    int fillTop = CustomMathf.Min(waterSurface, subWorldSize.SizeY - 2);
                    if (hydrology > 0.65f)
                    {
                        for (int y = floor; y <= fillTop && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                    else
                    {
                        var fillMaterial = hydrology > 0.78f ? BlockTileType.CLAY : BlockTileType.SAND;
                        subWorldBlockData[x, floor, z].CurrentType = (byte)fillMaterial;
                    }

                    surfaceCache[x, z] = floor;

                    double rimStrength = Math.Clamp(pressure * 0.8f, 0.2f, 0.85f);
                    ShapeLakeBank(subWorldBlockData, subWorldSize, x, z, waterSurface, (float)rimStrength);
                }
            }
        }

        private static void ApplyLakeHydrologyFeedback(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int extentX = radiusX + 6;
            int extentZ = radiusZ + 6;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
                    {
                        continue;
                    }

                    float ellipse = CustomMathf.Sqrt(
                        (dx * dx) / Math.Max(1.0f, (radiusX + 0.75f) * (radiusX + 0.75f)) +
                        (dz * dz) / Math.Max(1.0f, (radiusZ + 0.75f) * (radiusZ + 0.75f)));
                    if (ellipse <= 1.05f || ellipse >= 1.65f)
                    {
                        continue;
                    }

                    if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int surface) || surface <= 1)
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    float moisture = CustomMathf.Max(hydrology, flow);
                    if (moisture < 0.45f)
                    {
                        continue;
                    }

                    int drop = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + moisture * 3.5f), 1, 4);
                    int target = CustomMathf.Max(surface - drop, CustomMathf.Max(waterSurface - 1, 1));
                    for (int y = surface; y > target; y--)
                    {
                        subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    if (hydrology > 0.65f)
                    {
                        subWorldBlockData[x, target, z].CurrentType = (byte)BlockTileType.CLAY;
                        for (int y = target + 1; y <= waterSurface && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                        }
                        surfaceCache[x, z] = CustomMathf.Min(waterSurface, subWorldSize.SizeY - 1);
                    }
                    else
                    {
                        subWorldBlockData[x, target, z].CurrentType = (byte)BlockTileType.SAND;
                        if (target + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[x, target + 1, z].CurrentType = (byte)BlockTileType.EMPTY;
                        }
                        surfaceCache[x, z] = target;
                    }
                }
            }
        }

        private static void AddCaveAquiferChannels(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float catchment = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    if (hydrology < 0.7f && catchment < 0.35f)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (top - bottom < 6)
                    {
                        continue;
                    }

                    float pressure = CustomMathf.Clamp01((hydrology - 0.65f) * 1.4f + catchment * 0.85f);
                    int channelY = bottom + CustomMathf.Clamp(
                        CustomMathf.RoundToInt((top - bottom) * (0.25f + pressure * 0.35f)),
                        2,
                        top - 2);

                    if (surfaceCache[x, z] - channelY < 6)
                    {
                        continue;
                    }

                    CustomVector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    CustomVector2 flowDir = ComputeRiverFlowDirection(x + 0.5f, z + 0.5f);
                    if (slopeDir.sqrMagnitude > CustomVector2.kEpsilon)
                    {
                        flowDir = (flowDir * 0.55f + slopeDir * 0.45f).normalized;
                    }

                    if (flowDir.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        flowDir = CustomVector2.right;
                    }

                    int steps = CustomMathf.Clamp(CustomMathf.RoundToInt(3 + pressure * 5f + catchment * 4f), 3, 9);
                    int radius = pressure > 0.7f ? 2 : 1;
                    bool floodChannel = hydrology > 0.8f || catchment > 0.6f;

                    int cx = x;
                    int cz = z;
                    for (int step = 0; step < steps; step++)
                    {
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(cx, channelY, cz, subWorldSize))
                        {
                            break;
                        }

                        CarveAquiferChannelNode(subWorldBlockData, subWorldSize, cx, channelY, cz, radius, floodChannel);
                        var delta = GetAquiferStep(flowDir, step);
                        cx = CustomMathf.Clamp(cx + delta.dx, 1, subWorldSize.SizeX - 2);
                        cz = CustomMathf.Clamp(cz + delta.dz, 1, subWorldSize.SizeZ - 2);
                    }
                }
            }
        }

        private static void AddCaveRibbonTerraces(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] stabilityField,
            float[,] flowAccumulation)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = CustomMathf.Clamp01(hydrologyMask[x, z]);
                    float stability = CustomMathf.Clamp01(stabilityField[x, z]);
                    float catchment = CustomMathf.Clamp01(flowAccumulation[x, z] / 8f);
                    float ribbonWeight = hydrology * 0.6f + catchment * 0.25f + (1f - CustomMathf.Abs(stability - 0.55f)) * 0.35f;
                    if (ribbonWeight < 0.55f)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 7)
                    {
                        continue;
                    }

                    int ribbonY = bottom + CustomMathf.Clamp(
                        CustomMathf.RoundToInt(cavityHeight * (0.3f + hydrology * 0.35f)),
                        2,
                        cavityHeight - 2);
                    int ribbonThickness = CustomMathf.Clamp(CustomMathf.RoundToInt(1f + ribbonWeight * 2f), 1, 3);

                    CustomVector2 tangent = ComputeHydrologyTangentVector(hydrologyMask, x, z);
                    foreach (var (dx, dz) in BuildRibbonOffsets(tangent, ribbonWeight, hydrology))
                    {
                        int worldX = x + dx;
                        int worldZ = z + dz;
                        if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, worldX, worldZ, out int columnSurface))
                        {
                            continue;
                        }

                        if (columnSurface - ribbonY < 3)
                        {
                            continue;
                        }

                        int floorY = CustomMathf.Max(bottom + 1, ribbonY - ribbonThickness);
                        for (int y = ribbonY; y >= floorY && y > bottom + 1; y--)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        int supportY = CustomMathf.Max(floorY - 1, 1);
                        var supportBlock = stability > 0.72f ? BlockTileType.STONE_BIG : BlockTileType.STONE_SMALL;
                        subWorldBlockData[worldX, supportY, worldZ].CurrentType = (byte)supportBlock;

                        var walkway = hydrology > 0.78f ? BlockTileType.CLAY : BlockTileType.STONE_SMALL;
                        subWorldBlockData[worldX, floorY, worldZ].CurrentType = (byte)walkway;

                        int clearance = CustomMathf.Min(floorY + 2, subWorldSize.SizeY - 2);
                        for (int y = floorY + 1; y <= clearance; y++)
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                        }
                    }
                }
            }
        }

        private static CustomVector2 ComputeHydrologyTangentVector(float[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;

            float gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            float gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
            var gradient = new CustomVector2(gx, gz);

            if (gradient.sqrMagnitude < CustomVector2.kEpsilon)
            {
                return CustomVector2.right;
            }

            var tangent = new CustomVector2(-gradient.y, gradient.x);
            if (tangent.sqrMagnitude < CustomVector2.kEpsilon)
            {
                return CustomVector2.right;
            }

            tangent.Normalize();
            return tangent;
        }

        private static IReadOnlyCollection<(int dx, int dz)> BuildRibbonOffsets(CustomVector2 tangent, float ribbonWeight, float hydrology)
        {
            var offsets = new HashSet<(int dx, int dz)> { (0, 0) };
            var perpendicular = new CustomVector2(-tangent.y, tangent.x);
            if (perpendicular.sqrMagnitude < CustomVector2.kEpsilon)
            {
                perpendicular = CustomVector2.up;
            }
            perpendicular.Normalize();

            int steps = ribbonWeight > 0.85f ? 3 : 2;
            int halfWidth = hydrology > 0.75f ? 1 : 0;

            for (int step = -steps; step <= steps; step++)
            {
                int baseDx = CustomMathf.RoundToInt(tangent.x * step);
                int baseDz = CustomMathf.RoundToInt(tangent.y * step);

                for (int lateral = -halfWidth; lateral <= halfWidth; lateral++)
                {
                    int offsetX = baseDx + CustomMathf.RoundToInt(perpendicular.x * lateral);
                    int offsetZ = baseDz + CustomMathf.RoundToInt(perpendicular.y * lateral);
                    offsets.Add((offsetX, offsetZ));
                }
            }

            return offsets;
        }

        private static bool TryResolveSurface(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, out int surface)
        {
            surface = 0;
            if (!WorldGenerateUtils.CheckSubWorldBoundary(x, 0, z, subWorldSize))
            {
                return false;
            }

            surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, x, z);
                if (surface <= 0)
                {
                    return false;
                }
                surfaceCache[x, z] = surface;
            }

            return true;
        }

        private static void ApplyRiverGradientSmoothing(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] riverIntensity,
            float bankThreshold)
        {
            int[,] adjustments = new int[subWorldSize.SizeX, subWorldSize.SizeZ];
            bool hasAdjustment = false;

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity > bankThreshold * 1.15f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int neighborSum = 0;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= subWorldSize.SizeX || nz < 0 || nz >= subWorldSize.SizeZ)
                            {
                                continue;
                            }

                            if (riverIntensity[nx, nz] > bankThreshold * 1.2f)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            neighborSum += neighborSurface;
                            neighborCount++;
                        }
                    }

                    if (neighborCount < 3)
                    {
                        continue;
                    }

                    float averageSurface = neighborSum / (float)neighborCount;
                    float hydrologyBias = (hydrologyMask[x, z] - 0.5f) * 2f;
                    int targetSurface = CustomMathf.RoundToInt(averageSurface - hydrologyBias);
                    int delta = targetSurface - surface;
                    if (CustomMathf.Abs(delta) <= 2)
                    {
                        continue;
                    }

                    adjustments[x, z] = CustomMathf.Clamp(delta, -4, 3);
                    hasAdjustment = true;
                }
            }

            if (!hasAdjustment)
            {
                return;
            }

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    int delta = adjustments[x, z];
                    if (delta == 0)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    int targetSurface = CustomMathf.Clamp(surface + delta, 2, subWorldSize.SizeY - 2);
                    if (delta < 0)
                    {
                        for (int y = surface; y > targetSurface && y >= 1; y--)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        int waterTop = CustomMathf.Min(GlobalRiverWaterLevel, CustomMathf.Min(surface, subWorldSize.SizeY - 2));
                        for (int y = targetSurface; y <= waterTop && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                    else
                    {
                        for (int y = surface + 1; y <= targetSurface && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.SAND;
                        }

                        if (targetSurface < GlobalRiverWaterLevel)
                        {
                            int limit = CustomMathf.Min(GlobalRiverWaterLevel, surface);
                            for (int y = targetSurface + 1; y <= limit && y < subWorldSize.SizeY; y++)
                            {
                                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                            }
                        }
                    }

                    surfaceCache[x, z] = targetSurface;
                }
            }
        }

        private static void ApplyRiverMeanderTerraces(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] riverIntensity,
            float[,] hydrologyMask)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float intensity = riverIntensity[x, z];
                    if (intensity >= (float)RiverBankThreshold || intensity <= (float)RiverCenterThreshold * 0.35f)
                    {
                        continue;
                    }

                    CustomVector2 flowDir = ComputeRiverFlowDirection(x + 0.5f, z + 0.5f);
                    if (flowDir.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        continue;
                    }

                    flowDir.Normalize();
                    var perpendicular = new CustomVector2(-flowDir.y, flowDir.x);
                    if (perpendicular.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        perpendicular = CustomVector2.right;
                    }
                    perpendicular.Normalize();

                    ResolvePerpendicularOffset(perpendicular, 1, out int posOffsetX, out int posOffsetZ);
                    ResolvePerpendicularOffset(new CustomVector2(-perpendicular.x, -perpendicular.y), 1, out int negOffsetX, out int negOffsetZ);

                    if (!TrySampleField(riverIntensity, x + posOffsetX, z + posOffsetZ, out float posIntensity) ||
                        !TrySampleField(riverIntensity, x + negOffsetX, z + negOffsetZ, out float negIntensity))
                    {
                        continue;
                    }

                    bool posIsInner = posIntensity <= negIntensity;
                    var innerOffset = posIsInner ? (posOffsetX, posOffsetZ) : (negOffsetX, negOffsetZ);
                    var outerOffset = posIsInner ? (negOffsetX, negOffsetZ) : (posOffsetX, posOffsetZ);

                    if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int baseSurface) ||
                        !TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, x + innerOffset.Item1, z + innerOffset.Item2, out int innerSurface))
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(baseSurface, GlobalRiverWaterLevel);
                    float normalized = CustomMathf.Clamp01(1f - intensity / (float)RiverCenterThreshold);
                    int shelfDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1f + normalized * 3f), 1, 4);
                    int shelfFloor = CustomMathf.Max(riverSurface - shelfDepth, 1);

                    for (int y = innerSurface; y > shelfFloor; y--)
                    {
                        subWorldBlockData[x + innerOffset.Item1, y, z + innerOffset.Item2].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    float bankHydrology = CustomMathf.Clamp01(
                        hydrologyMask[
                            CustomMathf.Clamp(x + innerOffset.Item1, 0, hydrologyMask.GetLength(0) - 1),
                            CustomMathf.Clamp(z + innerOffset.Item2, 0, hydrologyMask.GetLength(1) - 1)]);
                    var shelfMaterial = bankHydrology > 0.62f ? BlockTileType.CLAY : BlockTileType.SAND;
                    subWorldBlockData[x + innerOffset.Item1, shelfFloor, z + innerOffset.Item2].CurrentType = (byte)shelfMaterial;

                    for (int y = shelfFloor + 1; y <= riverSurface && y < subWorldSize.SizeY; y++)
                    {
                        subWorldBlockData[x + innerOffset.Item1, y, z + innerOffset.Item2].CurrentType = (byte)BlockTileType.WATER;
                    }

                    surfaceCache[x + innerOffset.Item1, z + innerOffset.Item2] = shelfFloor;

                    int outerX = x + outerOffset.Item1;
                    int outerZ = z + outerOffset.Item2;
                    if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, outerX, outerZ, out _))
                    {
                        continue;
                    }

                    float gradient = CustomMathf.Clamp(CustomMathf.Abs(posIntensity - negIntensity) * 38f, 0.2f, 0.85f);
                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, outerX, outerZ, gradient * 0.5f + 0.2f, riverSurface, false);
                }
            }
        }

        private static void ApplyRiverHydrologyFeedback(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int surface))
                    {
                        continue;
                    }

                    float hydrology = hydrologyMask[x, z];
                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    if (hydrology < 0.45f && flow < 0.45f)
                    {
                        continue;
                    }

                    float intensity = riverIntensity[x, z];
                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));

                    CustomVector2 flowDir = ComputeRiverFlowDirection(x + 0.5f, z + 0.5f);
                    if (flowDir.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        flowDir = ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    }
                    if (flowDir.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        flowDir = CustomVector2.right;
                    }
                    flowDir.Normalize();

                    if (intensity < channelThreshold * 0.95f)
                    {
                        int infiltration = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + (hydrology + flow) * 3.5f), 1, 6);
                        int floor = CustomMathf.Max(riverSurface - infiltration, 1);
                        for (int y = surface; y >= floor && y >= 1; y--)
                        {
                            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                        }

                        int fillTop = CustomMathf.Min(riverSurface, subWorldSize.SizeY - 2);
                        if (hydrology > 0.62f)
                        {
                            for (int y = floor; y <= fillTop && y < subWorldSize.SizeY; y++)
                            {
                                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                            }
                        }

                        int supportY = CustomMathf.Max(floor - 1, 0);
                        subWorldBlockData[x, supportY, z].CurrentType = (byte)BlockTileType.SAND;
                        surfaceCache[x, z] = floor;
                        continue;
                    }

                    if (intensity >= bankThreshold)
                    {
                        subWorldBlockData[x, surface, z].CurrentType = (byte)BlockTileType.SAND;
                        if (surface + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[x, surface + 1, z].CurrentType = (byte)BlockTileType.GRASS;
                        }
                        continue;
                    }

                    float bankStrength = CustomMathf.Clamp01((hydrology + flow) * 0.5f);
                    var perpendicular = new CustomVector2(-flowDir.y, flowDir.x);
                    if (perpendicular.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        perpendicular = CustomVector2.up;
                    }
                    perpendicular.Normalize();

                    ResolvePerpendicularOffset(perpendicular, 1, out int offsetX, out int offsetZ);
                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.45f + 0.2f, riverSurface, true);

                    ResolvePerpendicularOffset(new CustomVector2(-perpendicular.x, -perpendicular.y), 1, out offsetX, out offsetZ);
                    ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.35f + 0.15f, riverSurface, false);
                }
            }
        }

        private static void AddRiverSeepageChannels(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            float[,] riverIntensity,
            float channelThreshold,
            float bankThreshold)
        {
            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.58f)
                    {
                        continue;
                    }

                    float intensity = riverIntensity[x, z];
                    if (intensity <= channelThreshold || intensity >= bankThreshold + 0.1f)
                    {
                        continue;
                    }

                    float flow = CustomMathf.Clamp01(flowAccumulation[x, z] / 6f);
                    if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, x, z, out int surface) || surface <= 1)
                    {
                        continue;
                    }

                    if (!TryFindDownstreamRiverCell(riverIntensity, x, z, out int targetX, out int targetZ, out float targetIntensity))
                    {
                        continue;
                    }

                    if (targetIntensity >= intensity || targetIntensity > channelThreshold * 1.05f)
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));
                    int depth = CustomMathf.Clamp(CustomMathf.RoundToInt(1 + (hydrology + flow) * 2.5f), 1, 4);
                    bool flood = hydrology > 0.7f || flow > 0.6f;

                    CarveRiverSeepagePath(subWorldBlockData, subWorldSize, surfaceCache, x, z, targetX, targetZ, riverSurface, depth, flood);
                }
            }
        }

        private static bool TryFindDownstreamRiverCell(float[,] riverIntensity, int x, int z, out int targetX, out int targetZ, out float targetIntensity)
        {
            (int dx, int dz)[] offsets =
            {
                (1, 0),
                (-1, 0),
                (0, 1),
                (0, -1)
            };

            targetX = -1;
            targetZ = -1;
            targetIntensity = float.MaxValue;

            foreach (var (dx, dz) in offsets)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= riverIntensity.GetLength(0) || nz < 0 || nz >= riverIntensity.GetLength(1))
                {
                    continue;
                }

                float sample = riverIntensity[nx, nz];
                if (sample < targetIntensity)
                {
                    targetIntensity = sample;
                    targetX = nx;
                    targetZ = nz;
                }
            }

            return targetX >= 0;
        }

        private static void CarveRiverSeepagePath(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            int startX,
            int startZ,
            int endX,
            int endZ,
            int riverSurface,
            int depth,
            bool flood)
        {
            int steps = CustomMathf.Clamp(CustomMathf.Max(CustomMathf.Abs(endX - startX), CustomMathf.Abs(endZ - startZ)), 1, 4);
            float stepX = (endX - startX) / (float)steps;
            float stepZ = (endZ - startZ) / (float)steps;
            float cursorX = startX;
            float cursorZ = startZ;

            for (int i = 0; i <= steps; i++)
            {
                int cx = CustomMathf.Clamp(CustomMathf.RoundToInt(cursorX), 0, subWorldSize.SizeX - 1);
                int cz = CustomMathf.Clamp(CustomMathf.RoundToInt(cursorZ), 0, subWorldSize.SizeZ - 1);

                if (!TryResolveSurface(subWorldBlockData, subWorldSize, surfaceCache, cx, cz, out int surface))
                {
                    cursorX += stepX;
                    cursorZ += stepZ;
                    continue;
                }

                int floor = CustomMathf.Max(surface - depth, 1);
                for (int y = surface; y > floor; y--)
                {
                    subWorldBlockData[cx, y, cz].CurrentType = (byte)BlockTileType.EMPTY;
                }

                if (flood)
                {
                    subWorldBlockData[cx, floor, cz].CurrentType = (byte)BlockTileType.CLAY;
                    for (int y = floor + 1; y <= riverSurface && y < subWorldSize.SizeY; y++)
                    {
                        subWorldBlockData[cx, y, cz].CurrentType = (byte)BlockTileType.WATER;
                    }
                    surfaceCache[cx, cz] = CustomMathf.Min(riverSurface, subWorldSize.SizeY - 1);
                }
                else
                {
                    subWorldBlockData[cx, floor, cz].CurrentType = (byte)BlockTileType.SAND;
                    if (floor + 1 < subWorldSize.SizeY)
                    {
                        subWorldBlockData[cx, floor + 1, cz].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                    surfaceCache[cx, cz] = floor;
                }

                cursorX += stepX;
                cursorZ += stepZ;
            }
        }

        private static bool TrySampleField(float[,] field, int x, int z, out float value)
        {
            if (x < 0 || x >= field.GetLength(0) || z < 0 || z >= field.GetLength(1))
            {
                value = 0f;
                return false;
            }

            value = field[x, z];
            return true;
        }

        private static float SampleRiverIntensity(float[,] riverIntensity, int centerX, int centerZ, int radius)
        {
            float sum = 0f;
            int samples = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (TrySampleField(riverIntensity, centerX + dx, centerZ + dz, out float value))
                    {
                        sum += value;
                        samples++;
                    }
                }
            }

            return samples > 0 ? sum / samples : 0f;
        }

        private static (int dx, int dz) GetAquiferStep(CustomVector2 direction, int stepIndex)
        {
            int dx = 0;
            int dz = 0;
            if (direction.x > 0.35f)
            {
                dx = 1;
            }
            else if (direction.x < -0.35f)
            {
                dx = -1;
            }

            if (direction.y > 0.35f)
            {
                dz = 1;
            }
            else if (direction.y < -0.35f)
            {
                dz = -1;
            }

            if (dx == 0 && dz == 0)
            {
                if (CustomMathf.Abs(direction.x) >= CustomMathf.Abs(direction.y))
                {
                    dx = direction.x >= 0 ? 1 : -1;
                }
                else
                {
                    dz = direction.y >= 0 ? 1 : -1;
                }
            }

            if (stepIndex % 3 == 2 && CustomMathf.Abs(direction.x) > 0.15f && CustomMathf.Abs(direction.y) > 0.15f)
            {
                dx = CustomMathf.Clamp(dx + (direction.x >= 0 ? 1 : -1), -1, 1);
                dz = CustomMathf.Clamp(dz + (direction.y >= 0 ? 1 : -1), -1, 1);
            }

            return (dx, dz);
        }

        private static void CarveAquiferChannelNode(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int centerX,
            int centerY,
            int centerZ,
            int radius,
            bool floodChannel)
        {
            int floor = CustomMathf.Max(2, centerY - 2);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, floor, nz, subWorldSize))
                    {
                        continue;
                    }

                    float falloff = 1f - (CustomMathf.Abs(dx) + CustomMathf.Abs(dz)) * (radius == 1 ? 0.6f : 0.45f);
                    if (falloff <= 0f)
                    {
                        continue;
                    }

                    int roof = CustomMathf.Min(subWorldSize.SizeY - 2, centerY + (radius > 1 ? 2 : 1));
                    for (int y = roof; y >= floor; y--)
                    {
                        subWorldBlockData[nx, y, nz].CurrentType = (byte)BlockTileType.EMPTY;
                    }

                    int floorBlock = CustomMathf.Max(1, floor - 1);
                    subWorldBlockData[nx, floorBlock, nz].CurrentType = floodChannel
                        ? (byte)BlockTileType.SAND
                        : (byte)BlockTileType.STONE_SMALL;

                    if (floodChannel)
                    {
                        for (int y = floor; y <= centerY && y < subWorldSize.SizeY; y++)
                        {
                            subWorldBlockData[nx, y, nz].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                }
            }
        }
        }

        private static bool TryFindCaveSpan(
            Block[,,] subWorldBlockData,
            SubWorldSize subWorldSize,
            int[,] surfaceCache,
            int x,
            int z,
            out int top,
            out int bottom)
        {
            top = -1;
            bottom = -1;

            if (x < 0 || x >= subWorldSize.SizeX || z < 0 || z >= subWorldSize.SizeZ)
            {
                return false;
            }

            int surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                return false;
            }

            int scanStart = CustomMathf.Clamp(surface - 2, 8, subWorldSize.SizeY - 3);
            bool insideAir = false;
            for (int y = scanStart; y >= 5; y--)
            {
                BlockTileType current = (BlockTileType)subWorldBlockData[x, y, z].CurrentType;
                bool isEmpty = current == BlockTileType.EMPTY || current == BlockTileType.WATER;
                if (isEmpty)
                {
                    if (!insideAir)
                    {
                        insideAir = true;
                        top = y;
                    }
                }
                else if (insideAir)
                {
                    bottom = y + 1;
                    return true;
                }
            }

            top = -1;
            bottom = -1;
            return false;
        }

        private static void PlaceSupportNode(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int x, int y, int z, bool wide)
        {
            if (!WorldGenerateUtils.CheckSubWorldBoundary(x, y, z, subWorldSize))
            {
                return;
            }

            subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.STONE_BIG;

            if (!wide)
            {
                return;
            }

            int[,] offsets = new int[,]
            {
                { 1, 0 },
                { -1, 0 },
                { 0, 1 },
                { 0, -1 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int nx = x + offsets[i, 0];
                int nz = z + offsets[i, 1];
                if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, y, nz, subWorldSize))
                {
                    continue;
                }

                if (subWorldBlockData[nx, y, nz].CurrentType == (byte)BlockTileType.EMPTY)
                {
                    subWorldBlockData[nx, y, nz].CurrentType = (byte)BlockTileType.STONE_SMALL;
                }
            }
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

                if (Utilitys.RandomFloat(0.0f, 1.0f) < 0.3f)
                {
                    int poolRadius = Utilitys.RandomInteger(3, 5);
                    int poolY = CustomMathf.Clamp(centerY - Utilitys.RandomInteger(1, 3), 2, subWorldSize.SizeY - 3);
                    CreateCavePool(subWorldBlockData, subWorldSize, centerX, poolY, centerZ, poolRadius);
                }
                
                // 중심에서 방사상으로 통로 생성
                int tunnelCount = Utilitys.RandomInteger(3, 6);
                for (int tunnel = 0; tunnel < tunnelCount; tunnel++)
                {
                    GenerateCaveTunnel(subWorldBlockData, subWorldSize, centerX, centerY, centerZ, 
                        Utilitys.RandomFloat(0, 360), Utilitys.RandomFloat(-30, 30));
                }

                if (Utilitys.RandomFloat(0.0f, 1.0f) < 0.45f)
                {
                    int shaftRadius = Utilitys.RandomInteger(2, 3);
                    int shaftLength = Utilitys.RandomInteger(6, 14);
                    int shaftDirection = Utilitys.RandomBool() ? 1 : -1;
                    bool fillWithWater = Utilitys.RandomFloat(0.0f, 1.0f) < 0.35f;
                    CreateVerticalShaft(subWorldBlockData, subWorldSize, centerX, centerY, centerZ, shaftRadius, shaftLength, shaftDirection, fillWithWater);
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

            int tunnelLength = Utilitys.RandomInteger(24, 58);
            float radiusBase = Utilitys.RandomFloat(2.3f, 4.1f);
            float radiusVariance = Utilitys.RandomFloat(0.6f, 1.3f);
            float noiseSeed = Utilitys.RandomFloat(-5000f, 5000f);
            
            for (int step = 0; step < tunnelLength; step++)
            {
                // 통로 파기
                float radiusNoise = Noise.GetNoise((currentX + noiseSeed) * 0.09f, (currentY + noiseSeed) * 0.09f, (currentZ + noiseSeed) * 0.09f);
                float tunnelRadius = CustomMathf.Clamp(radiusBase + radiusNoise * radiusVariance, 1.6f, radiusBase + radiusVariance + 1.4f);

                CarveTunnelSegment(subWorldBlockData, subWorldSize, (int)currentX, (int)currentY, (int)currentZ, tunnelRadius);
                
                // 다음 위치 계산
                currentX += CustomMathf.Cos(direction * CustomMathf.Deg2Rad) * CustomMathf.Cos(pitch * CustomMathf.Deg2Rad);
                currentY += CustomMathf.Sin(pitch * CustomMathf.Deg2Rad);
                currentZ += CustomMathf.Sin(direction * CustomMathf.Deg2Rad) * CustomMathf.Cos(pitch * CustomMathf.Deg2Rad);
                
                // 방향 약간 변경 (자연스러운 구불구불함)
                float directionalNoise = Noise.GetNoise((currentX - noiseSeed) * 0.05f, (currentY + noiseSeed) * 0.05f, (currentZ - noiseSeed) * 0.05f);
                direction += Utilitys.RandomFloat(-5, 5) + directionalNoise * 4.5f;
                pitch += Utilitys.RandomFloat(-3, 3) + directionalNoise * 2.0f;
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
                            float verticalScale = 0.8f + Noise.GetNoise((worldX + centerY) * 0.12f, (worldZ - centerX) * 0.12f, (worldY + centerZ) * 0.12f) * 0.3f;
                            float scaledY = y / CustomMathf.Max(0.55f, verticalScale);
                            float distance = CustomMathf.Sqrt(x * x + scaledY * scaledY + z * z);
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
                int centerX = Utilitys.RandomInteger(6, subWorldSize.SizeX - 6);
                int centerZ = Utilitys.RandomInteger(6, subWorldSize.SizeZ - 6);
                int centerY = Utilitys.RandomInteger(4, subWorldSize.SizeY / 2);

                int radiusX = Utilitys.RandomInteger(5, 9);
                int radiusZ = Utilitys.RandomInteger(5, 9);
                int verticalRadius = Utilitys.RandomInteger(2, 4);

                float rotation = Noise.GetNoise(centerX * 0.2f, centerY * 0.11f, centerZ * 0.2f) * CustomMathf.PI;
                float cos = CustomMathf.Cos(rotation);
                float sin = CustomMathf.Sin(rotation);

                float radiusXWithPadding = radiusX + 0.65f;
                float radiusZWithPadding = radiusZ + 0.65f;
                float verticalPadding = verticalRadius + 0.75f;

                int waterSurface = CustomMathf.Clamp(centerY + Utilitys.RandomInteger(-1, 2), 3, subWorldSize.SizeY - 4);
                int waterFloor = CustomMathf.Max(1, waterSurface - Utilitys.RandomInteger(verticalRadius + 2, verticalRadius + 5));

                for (int offsetX = -radiusX - 3; offsetX <= radiusX + 3; offsetX++)
                {
                    for (int offsetZ = -radiusZ - 3; offsetZ <= radiusZ + 3; offsetZ++)
                    {
                        int worldX = centerX + offsetX;
                        int worldZ = centerZ + offsetZ;
                        if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, 0, worldZ, subWorldSize))
                        {
                            continue;
                        }

                        float rotatedX = offsetX * cos - offsetZ * sin;
                        float rotatedZ = offsetX * sin + offsetZ * cos;
                        float horizontal = CustomMathf.Sqrt(
                            (rotatedX * rotatedX) / (radiusXWithPadding * radiusXWithPadding) +
                            (rotatedZ * rotatedZ) / (radiusZWithPadding * radiusZWithPadding));

                        if (horizontal > 1.45f)
                        {
                            continue;
                        }

                        for (int offsetY = -verticalRadius - 2; offsetY <= verticalRadius + 2; offsetY++)
                        {
                            int worldY = centerY + offsetY;
                            if (worldY <= 1 || worldY >= subWorldSize.SizeY - 2)
                            {
                                continue;
                            }

                            float verticalNormalized = CustomMathf.Abs(offsetY) / verticalPadding;
                            float sdf = CustomMathf.Sqrt(horizontal * horizontal + verticalNormalized * verticalNormalized) - 1.0f;

                            if (sdf <= 0.1f)
                            {
                                if (worldY < waterFloor - 1)
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.STONE_SMALL;
                                }
                                else if (worldY == waterFloor - 1)
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.SAND;
                                }
                                else if (worldY <= waterSurface)
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.WATER;
                                }
                                else
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                                }
                            }
                            else if (sdf <= 0.35f)
                            {
                                if (worldY <= waterSurface)
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.STONE_SMALL;
                                }
                                else
                                {
                                    subWorldBlockData[worldX, worldY, worldZ].CurrentType = (byte)BlockTileType.SAND;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private static void CreateVerticalShaft(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int startX, int startY, int startZ, int radius, int length, int direction, bool fillWithWater)
        {
            int steps = CustomMathf.Max(1, length);
            for (int step = 0; step < steps; step++)
            {
                int currentY = startY + step * direction;
                if (currentY <= 1 || currentY >= subWorldSize.SizeY - 2)
                {
                    break;
                }

                CarveTunnelSegment(subWorldBlockData, subWorldSize, startX, currentY, startZ, radius);

                if (fillWithWater && direction < 0 && step >= steps - 2)
                {
                    subWorldBlockData[startX, currentY, startZ].CurrentType = (byte)BlockTileType.WATER;
                }
            }
        }

        private static void CreateCavePool(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerY, int centerZ, int radius)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetZ = -radius; offsetZ <= radius; offsetZ++)
                {
                    int worldX = centerX + offsetX;
                    int worldZ = centerZ + offsetZ;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, centerY, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    float distance = CustomMathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
                    if (distance > radius)
                    {
                        continue;
                    }

                    int floorY = CustomMathf.Max(1, centerY - 1);
                    subWorldBlockData[worldX, floorY, worldZ].CurrentType = (byte)BlockTileType.SAND;

                    subWorldBlockData[worldX, centerY, worldZ].CurrentType = (byte)BlockTileType.WATER;

                    if (centerY + 1 < subWorldSize.SizeY)
                    {
                        subWorldBlockData[worldX, centerY + 1, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                }
            }
        }

        private static void GenerateNoiseCaves(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            float[,] flowAccumulation = BuildFlowAccumulation(surfaceCache, subWorldSize);
            BlendHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            StabilizeHydrologyGradients(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            SmoothHydrologyFields(hydrologyMask, flowAccumulation);
            NormalizeHydrologyRange(subWorldSize, hydrologyMask, flowAccumulation);
            ClampHydrologyToWaterTable(subWorldSize, hydrologyMask, flowAccumulation, surfaceCache);
            RelaxHydrologySeams(subWorldSize, hydrologyMask, flowAccumulation);
            AnchorHydrologyToSlope(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float[,] erosionRiskField = BuildErosionRiskField(subWorldSize, surfaceCache, hydrologyMask, flowAccumulation);
            float horizontalScale = 52f;
            float verticalScale = 30f;
            float warpScale = 180f;
            float warpStrength = 26f;
            int maxY = CustomMathf.Min(subWorldSize.SizeY - 4, 120);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    float warpX = (Noise.GetNoise((x + 37.17f) / warpScale, 0, (z - 19.41f) / warpScale) - 0.5f) * warpStrength;
                    float warpZ = (Noise.GetNoise((x - 11.73f) / (warpScale * 0.9f), 0, (z + 23.12f) / (warpScale * 0.9f)) - 0.5f) * warpStrength;
                    float sampleX = (x + warpX) / horizontalScale;
                    float sampleZ = (z + warpZ) / horizontalScale;
                    float erosionRisk = erosionRiskField[x, z];

                    for (int y = 6; y < maxY; y++)
                    {
                        float sampleY = y / verticalScale;
                        float baseNoise = Noise.GetNoise(sampleX, sampleY, sampleZ) - 0.5f;
                        float detailNoise = Noise.GetNoise(sampleX * 1.7f, sampleY * 0.85f, sampleZ * 1.7f) - 0.5f;
                        float ridgeNoise = Noise.GetNoise(sampleX * 0.65f, sampleY * 1.35f, sampleZ * 0.65f) - 0.5f;
                        float striationNoise = Noise.GetNoise(sampleX * 0.9f, sampleY * 0.45f, sampleZ * 0.9f) - 0.5f;
                        float flowNoise = Noise.GetNoise(sampleX * 0.25f + 37.1f, sampleY * 0.55f + 19.3f, sampleZ * 0.25f - 11.4f) - 0.5f;

                        float density = CustomMathf.Abs(baseNoise) * 0.55f + CustomMathf.Abs(detailNoise) * 0.35f;
                        density *= 0.75f + ridgeNoise * 0.45f;
                        density -= CustomMathf.Clamp(striationNoise, -0.35f, 0.35f) * 0.2f;
                        density += flowNoise * 0.18f;
                        density -= CustomMathf.Clamp01(erosionRisk * 1.15f) * 0.06f;

                        float verticalFade = CustomMathf.Clamp01((y - 16f) / 150f);
                        density -= verticalFade * 0.5f;

                        float strata = CustomMathf.Sin((x + z + y * 1.5f) * 0.035f);
                        density -= strata * 0.04f;

                        int cachedSurface = surfaceCache[x, z];
                        if (cachedSurface > 0)
                        {
                            float ceilingDepth = CustomMathf.Clamp01((cachedSurface - y) / 48f);
                            density += ceilingDepth * 0.08f;
                        }

                        float aquiferNoise = Noise.GetNoise((x + 211f) / 96f, y / 52f, (z - 73f) / 96f) - 0.5f;
                        float liquidity = CustomMathf.Clamp01((GlobalRiverWaterLevel - y) / 24f);
                        float flowBias = CustomMathf.Clamp01((flowNoise + 0.5f) * 0.5f + liquidity * 0.5f);
                        float threshold = 0.24f - liquidity * 0.08f + aquiferNoise * 0.02f - flowBias * 0.015f;
                        threshold -= CustomMathf.Clamp((erosionRisk - 0.35f) * 0.08f, -0.08f, 0.08f);
                        float hydrologyPenalty = CustomMathf.Clamp(hydrologyMask[x, z], 0f, 1f) * 0.02f;
                        float flowSuppression = CustomMathf.Clamp((float)flowAccumulation[x, z], 0f, 8f) * 0.01f;
                        threshold += hydrologyPenalty + flowSuppression;

                        if (density < threshold)
                        {
                            var currentType = (BlockTileType)subWorldBlockData[x, y, z].CurrentType;
                            if (currentType == BlockTileType.EMPTY || currentType == BlockTileType.WATER)
                            {
                                continue;
                            }

                            float floodedBias = CustomMathf.Clamp01(liquidity * 0.6f + erosionRisk * 0.5f);
                            if (density < threshold * (0.45f + floodedBias * 0.25f) && y < 14)
                            {
                                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                            }
                            else if (density < threshold * (0.65f + floodedBias * 0.2f) && y < GlobalRiverWaterLevel - 8)
                            {
                                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.WATER;
                            }
                            else
                            {
                                subWorldBlockData[x, y, z].CurrentType = (byte)BlockTileType.EMPTY;
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyHydrologyDrivenCavePools(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            var rand = new Random((subWorldSize.SizeX * 73856093) ^ (subWorldSize.SizeZ * 19349663) ^ 0xCAV3);

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.55f)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 12)
                    {
                        continue;
                    }

                    int y = CustomMathf.Min(surface - 4, CustomMathf.Min(subWorldSize.SizeY - 6, 110));
                    while (y > 8)
                    {
                        while (y > 8 && subWorldBlockData[x, y, z].CurrentType != (byte)BlockTileType.EMPTY)
                        {
                            y--;
                        }

                        if (y <= 8)
                        {
                            break;
                        }

                        int cavityTop = y;
                        while (y > 6 && subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.EMPTY)
                        {
                            y--;
                        }

                        int cavityBottom = y + 1;
                        int cavityHeight = cavityTop - cavityBottom + 1;
                        if (cavityHeight < 4)
                        {
                            continue;
                        }

                        float poolChance = CustomMathf.Clamp01((hydrology - 0.45f) * 1.4f);
                        if (poolChance <= 0f || rand.NextDouble() > poolChance)
                        {
                            continue;
                        }

                        int poolDepth = CustomMathf.Clamp(CustomMathf.RoundToInt(1f + hydrology * 4f), 2, CustomMathf.Min(6, cavityHeight - 1));
                        int sedimentY = CustomMathf.Max(cavityBottom, cavityTop - poolDepth);
                        subWorldBlockData[x, sedimentY, z].CurrentType = (byte)BlockTileType.SAND;

                        int waterStart = CustomMathf.Max(sedimentY + 1, cavityBottom);
                        int waterEnd = CustomMathf.Min(cavityTop - 1, sedimentY + poolDepth - 1);
                        for (int fillY = waterStart; fillY <= waterEnd && fillY < subWorldSize.SizeY; fillY++)
                        {
                            subWorldBlockData[x, fillY, z].CurrentType = (byte)BlockTileType.WATER;
                        }

                        break;
                    }
                }
            }
        }

        private static void AddCavePillars(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int maxY = CustomMathf.Min(subWorldSize.SizeY - 5, 120);
            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    int y = 6;
                    while (y < maxY)
                    {
                        while (y < maxY && subWorldBlockData[x, y, z].CurrentType != (byte)BlockTileType.EMPTY)
                        {
                            y++;
                        }

                        if (y >= maxY)
                        {
                            break;
                        }

                        int cavityStart = y;
                        while (y < maxY && subWorldBlockData[x, y, z].CurrentType == (byte)BlockTileType.EMPTY)
                        {
                            y++;
                        }
                        int cavityEnd = y - 1;
                        int cavityHeight = cavityEnd - cavityStart + 1;

                        if (cavityHeight >= 6 && Utilitys.RandomFloat(0.0f, 1.0f) < 0.18f)
                        {
                            if (cavityStart <= 0 || cavityEnd >= subWorldSize.SizeY - 1)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            BlockTileType floorBlock = (BlockTileType)subWorldBlockData[x, cavityStart - 1, z].CurrentType;
                            BlockTileType ceilingBlock = (BlockTileType)subWorldBlockData[x, cavityEnd + 1, z].CurrentType;
                            if (floorBlock == BlockTileType.EMPTY || ceilingBlock == BlockTileType.EMPTY)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            int maxFeatureHeight = CustomMathf.Min(3, (cavityHeight - 2) / 2);
                            if (maxFeatureHeight > 0)
                            {
                                int stalagmiteHeight = Utilitys.RandomInteger(1, maxFeatureHeight + 1);
                                int stalactiteHeight = Utilitys.RandomInteger(1, maxFeatureHeight + 1);
                                if (stalagmiteHeight + stalactiteHeight >= cavityHeight - 1)
                                {
                                    stalagmiteHeight = CustomMathf.Max(1, maxFeatureHeight - 1);
                                    stalactiteHeight = CustomMathf.Max(1, maxFeatureHeight);
                                }

                                for (int i = 0; i < stalagmiteHeight; i++)
                                {
                                    int py = cavityStart + i;
                                    if (py >= cavityEnd)
                                    {
                                        break;
                                    }
                                    subWorldBlockData[x, py, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                                }

                                for (int i = 0; i < stalactiteHeight; i++)
                                {
                                    int py = cavityEnd - i;
                                    if (py <= cavityStart + 1)
                                    {
                                        break;
                                    }
                                    subWorldBlockData[x, py, z].CurrentType = (byte)BlockTileType.STONE_SMALL;
                                }
                            }
                        }

                        y = cavityEnd + 1;
                    }
                }
            }
        }

        private static void IntegrateKarstSinkholes(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);
            float[,] hydrologyMask = BuildHydrologyMask(subWorldSize, surfaceCache);
            float[,] flowAccumulation = BuildFlowAccumulation(surfaceCache, subWorldSize);

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
                    float hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.62f)
                    {
                        continue;
                    }

                    float catchment = CustomMathf.Clamp01(flowAccumulation[x, z] / 8f);
                    CustomVector2 flowDir;
                    float riverIntensity = EvaluateRiverIntensity(x, z, out flowDir);
                    float riverAffinity = 1f - CustomMathf.Clamp01(riverIntensity / 0.12f);
                    float weight = hydrology * 0.55f + catchment * 0.3f + riverAffinity * 0.25f;

                    float sample = Noise.GetNoise((x + 73.1f) / 9f, 0, (z - 19.3f) / 9f) * 0.5f + 0.5f;
                    if (weight < 0.65f || sample > weight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface < 6 || surface >= subWorldSize.SizeY - 6)
                    {
                        continue;
                    }

                    int shaftDepth = CustomMathf.Clamp(Utilitys.RandomInteger(4, 9) + CustomMathf.RoundToInt(weight * 4f), 4, 12);
                    int shaftBottom = CustomMathf.Max(3, surface - shaftDepth);
                    int radius = weight > 1.05f ? 2 : 1;

                    CarveKarstColumn(subWorldBlockData, subWorldSize, x, z, surface - 2, shaftBottom, radius);

                    if (shaftBottom + 3 < GlobalRiverWaterLevel - 2 && Utilitys.RandomFloat(0f, 1f) < hydrology)
                    {
                        FillKarstPool(subWorldBlockData, subWorldSize, x, z, shaftBottom, CustomMathf.Clamp(CustomMathf.RoundToInt(1 + weight * 2.5f), 2, 5));
                    }

                    CustomVector2 direction = flowDir.sqrMagnitude > CustomVector2.kEpsilon
                        ? flowDir
                        : ComputeTerrainSlopeDirection(surfaceCache, subWorldSize, x, z);
                    if (direction.sqrMagnitude < CustomVector2.kEpsilon)
                    {
                        continue;
                    }

                    CreateKarstTunnel(subWorldBlockData, subWorldSize, x, z, CustomMathf.Max(shaftBottom + 1, 4), direction, weight);
                }
            }
        }

        private static void CarveKarstColumn(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int topY, int bottomY, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if ((dx * dx) + (dz * dz) > radius * radius + (radius == 1 ? 0 : 1))
                    {
                        continue;
                    }

                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, topY, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    for (int y = topY; y >= bottomY && y > 1; y--)
                    {
                        subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.EMPTY;
                    }
                }
            }
        }

        private static void FillKarstPool(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int baseY, int depth)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int worldX = centerX + dx;
                    int worldZ = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(worldX, baseY, worldZ, subWorldSize))
                    {
                        continue;
                    }

                    subWorldBlockData[worldX, baseY, worldZ].CurrentType = (byte)BlockTileType.SAND;
                    for (int y = baseY + 1; y <= baseY + depth && y < GlobalRiverWaterLevel - 2; y++)
                    {
                        if (WorldGenerateUtils.CheckSubWorldBoundary(worldX, y, worldZ, subWorldSize))
                        {
                            subWorldBlockData[worldX, y, worldZ].CurrentType = (byte)BlockTileType.WATER;
                        }
                    }
                }
            }
        }

        private static void CreateKarstTunnel(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int startX, int startZ, int baseY, CustomVector2 direction, float weight)
        {
            if (direction.sqrMagnitude < CustomVector2.kEpsilon || baseY <= 2)
            {
                return;
            }

            CustomVector2 dir = direction.normalized;
            float radius = CustomMathf.Clamp(1.3f + weight * 0.6f, 1.3f, 2.4f);
            int steps = CustomMathf.Clamp(CustomMathf.RoundToInt(3 + weight * 4f), 3, 8);
            float x = startX;
            float z = startZ;

            for (int i = 0; i < steps; i++)
            {
                int worldX = CustomMathf.Clamp(CustomMathf.RoundToInt(x), 1, subWorldSize.SizeX - 2);
                int worldZ = CustomMathf.Clamp(CustomMathf.RoundToInt(z), 1, subWorldSize.SizeZ - 2);
                CarveTunnelSegment(subWorldBlockData, subWorldSize, worldX, baseY, worldZ, radius);

                if (Utilitys.RandomFloat(0f, 1f) < 0.2f && baseY - 1 > 1)
                {
                    subWorldBlockData[worldX, baseY - 1, worldZ].CurrentType = (byte)BlockTileType.CLAY;
                }

                x += dir.x + (Noise.GetNoise(x * 0.15f, 0, z * 0.15f) - 0.5f) * 0.35f;
                z += dir.y + (Noise.GetNoise(x * 0.12f, 0, z * 0.12f + 17f) - 0.5f) * 0.35f;

                if (!WorldGenerateUtils.CheckSubWorldBoundary(CustomMathf.RoundToInt(x), baseY, CustomMathf.RoundToInt(z), subWorldSize))
                {
                    break;
                }
            }
        }

        private static void SmoothPondBanks(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int radius, int waterLevel)
        {
            int featherRadius = radius + 2;
            for (int dx = -featherRadius; dx <= featherRadius; dx++)
            {
                for (int dz = -featherRadius; dz <= featherRadius; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (!WorldGenerateUtils.CheckSubWorldBoundary(nx, 0, nz, subWorldSize))
                        continue;

                    float normalizedDistance = CustomMathf.Abs(dx) / (radius + 0.5f) + CustomMathf.Abs(dz) / (radius + 0.5f);
                    if (normalizedDistance <= 1.0f || normalizedDistance > 1.35f)
                        continue;

                    int surface = FindSurfaceLevel(subWorldBlockData, subWorldSize, nx, nz);
                    if (surface <= 0)
                        continue;

                    if (surface <= waterLevel + 2)
                    {
                        subWorldBlockData[nx, surface, nz].CurrentType = (byte)BlockTileType.SAND;
                        if (surface + 1 < subWorldSize.SizeY)
                        {
                            subWorldBlockData[nx, surface + 1, nz].CurrentType = (byte)BlockTileType.EMPTY;
                        }
                    }
                }
            }
        }
    }

}
