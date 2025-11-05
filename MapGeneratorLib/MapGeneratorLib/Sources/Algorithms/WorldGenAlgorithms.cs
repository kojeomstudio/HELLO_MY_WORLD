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
        private const int GlobalRiverWaterLevel = 62;

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

        private static CustomVector2 ComputeRiverFlowDirection(float sampleX, float sampleZ)
        {
            const float gradientStep = 0.0125f;

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

        private static void GenerateRiverSystems(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
            if (subWorldSize.SizeX < 4 || subWorldSize.SizeZ < 4)
            {
                return;
            }

            int[,] surfaceCache = BuildSurfaceHeightCache(subWorldBlockData, subWorldSize);

            const float channelThreshold = 0.033f;
            const float bankThreshold = 0.07f;
            const float sampleScale = 54f;
            const float warpScale = 92f;
            const float warpStrength = 5.25f;

            for (int x = 1; x < subWorldSize.SizeX - 1; x++)
            {
                for (int z = 1; z < subWorldSize.SizeZ - 1; z++)
                {
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

                    if (riverMask >= bankThreshold)
                    {
                        continue;
                    }

                    CustomVector2 flowDir = ComputeRiverFlowDirection(sampleX, sampleZ);

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    int riverSurface = CustomMathf.Min(surface, CustomMathf.Min(subWorldSize.SizeY - 2, GlobalRiverWaterLevel));

                    if (riverMask < channelThreshold)
                    {
                        float normalized = CustomMathf.Clamp01(1.0f - riverMask / channelThreshold);
                        CarveRiverColumn(subWorldBlockData, subWorldSize, surfaceCache, x, z, normalized, riverSurface, flowDir);
                    }
                    else
                    {
                        float bankStrength = CustomMathf.Clamp01(1.0f - (riverMask - channelThreshold) / (bankThreshold - channelThreshold));
                        FeatherRiverBank(subWorldBlockData, subWorldSize, surfaceCache, x, z, bankStrength, riverSurface, flowDir);
                    }
                }
            }
        }

        private static void CarveRiverColumn(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int[,] surfaceCache, int x, int z, float normalized, int riverSurface, CustomVector2 flowDir)
        {
            int surface = surfaceCache[x, z];
            if (surface <= 1)
            {
                return;
            }

            int channelDepth = CustomMathf.Clamp(3 + CustomMathf.RoundToInt(CustomMathf.Lerp(0f, 5.5f, normalized)), 3, 8);
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

            int maxRadius = CustomMathf.Clamp(2 + CustomMathf.RoundToInt(normalized * 2f), 2, 4);

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

                int offsetX = x + CustomMathf.RoundToInt(perpendicular.x * step);
                int offsetZ = z + CustomMathf.RoundToInt(perpendicular.y * step);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, offsetX, offsetZ, falloff, riverSurface, false);

                offsetX = x - CustomMathf.RoundToInt(perpendicular.x * step);
                offsetZ = z - CustomMathf.RoundToInt(perpendicular.y * step);
                ShapeRiverBank(subWorldBlockData, subWorldSize, surfaceCache, offsetX, offsetZ, falloff, riverSurface, false);
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

        private static void GenerateSurfaceLakes(Block[,,] subWorldBlockData, SubWorldSize subWorldSize)
        {
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

                int radiusX = Utilitys.RandomInteger(4, 7);
                int radiusZ = Utilitys.RandomInteger(3, 6);
                int maxDepth = Utilitys.RandomInteger(3, 5);

                int waterSurface = CustomMathf.Clamp(GlobalRiverWaterLevel + Utilitys.RandomInteger(-1, 2), 45, CustomMathf.Min(subWorldSize.SizeY - 3, GlobalRiverWaterLevel + 2));
                if (surface < waterSurface - 4 || surface > waterSurface + 8)
                {
                    continue;
                }

                float rotation = Noise.GetNoise(centerX * 0.12f, 0, centerZ * 0.12f) * CustomMathf.PI;
                CarveLakeBasin(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ, maxDepth, rotation);
                DecorateLakeBanks(subWorldBlockData, subWorldSize, centerX, centerZ, waterSurface, radiusX, radiusZ, rotation);
            }
        }

        private static void CarveLakeBasin(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ, int maxDepth, float rotation)
        {
            float cos = CustomMathf.Cos(rotation);
            float sin = CustomMathf.Sin(rotation);
            float radiusXWithPadding = radiusX + 0.75f;
            float radiusZWithPadding = radiusZ + 0.75f;

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
                    float sdf = ellipse - 1.0f;

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
                        ShapeLakeBank(subWorldBlockData, subWorldSize, worldX, worldZ, waterSurface, rimStrength);
                    }
                }
            }
        }

        private static void DecorateLakeBanks(Block[,,] subWorldBlockData, SubWorldSize subWorldSize, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ, float rotation)
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

                    float rimStrength = CustomMathf.Clamp01(1.6f - ellipse);
                    ShapeLakeBank(subWorldBlockData, subWorldSize, worldX, worldZ, waterSurface, rimStrength * 0.5f);
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
            // 대형 동굴 시스템 생성
            GenerateLargeCaveSystem(subWorldBlockData, subWorldSize);
            
            // 소형 동굴 방 생성
            GenerateSmallCaves(subWorldBlockData, subWorldSize);
            
            // 지하 호수 생성
            GenerateUndergroundLakes(subWorldBlockData, subWorldSize);

            // 노이즈 기반 동굴 추가 - 청크 경계 일관성을 유지한다.
            GenerateNoiseCaves(subWorldBlockData, subWorldSize);
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
            float horizontalScale = 48f;
            float verticalScale = 28f;

            for (int x = 0; x < subWorldSize.SizeX; x++)
            {
                for (int z = 0; z < subWorldSize.SizeZ; z++)
                {
                    for (int y = 6; y < CustomMathf.Min(subWorldSize.SizeY - 4, 120); y++)
                    {
                        double noiseValue = Noise.GetNoise(x / horizontalScale, y / verticalScale, z / horizontalScale);
                        double density = CustomMathf.Abs((float)noiseValue);
                        density -= CustomMathf.Clamp((y - 18) / (float)subWorldSize.SizeY, 0.0f, 0.45f);

                        if (density < 0.28f)
                        {
                            var currentType = (BlockTileType)subWorldBlockData[x, y, z].CurrentType;
                            if (currentType != BlockTileType.EMPTY && currentType != BlockTileType.WATER)
                            {
                                if (density < 0.1f && y < 14)
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
