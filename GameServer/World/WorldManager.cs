using System.Collections.Concurrent;
using GameServerApp.Database;
using GameServerApp.Models;
using GameServerApp.World.Generation;
using System.Numerics;

namespace GameServerApp.World
{
    public class WorldManager
    {
        private readonly DatabaseHelper _database;
        private readonly ConcurrentDictionary<string, LoadedChunk> _loadedChunks = new();
        private readonly Random _random;
        private int _worldId;
        private readonly TerrainGenerationPipeline _terrainPipeline;

        private const int GlobalWaterLevel = 62;
        private const double RiverCenterThreshold = 0.0125;
        private const double RiverBankThreshold = 0.028;
        private const int CloudBaseAltitude = 200;
        private const double OceanThreshold = 0.36;
        private const double BeachThreshold = 0.42;
        private const int MinSurfaceHeight = 45;
        private const int MaxSurfaceHeight = 150;
        private const double CliffThreshold = 0.55;
        private const double DomainWarpSimplexFrequency = 0.00065;
        private const double DomainWarpPerlinFrequency = 0.0011;
        private const double DomainWarpSimplexAmplitude = 32.0;
        private const double DomainWarpPerlinAmplitude = 18.0;
        private const double ValleyDepthMultiplier = 12.0;
        private const string TerrainProfilesKey = "terrain.profiles";
        private const string RiverFieldCacheKey = "terrain.riverField";
        private const double NoiseCaveHorizontalFrequency = 0.0026;
        private const double NoiseCaveVerticalFrequency = 0.018;
        private const double NoiseCaveThreshold = 0.42;
        private const double NoiseCaveLavaThreshold = 0.28;
        private const double NoiseCaveWaterThreshold = 0.34;

        private struct TerrainProfile
        {
            public int SurfaceHeight;
            public bool HasWater;
            public int WaterLevel;
            public BiomeType Biome;
            public BlockType SurfaceBlock;
            public BlockType SubSurfaceBlock;
            public BlockType FillerBlock;
            public bool UseCliffFace;
        }

        private sealed class RiverFieldCache
        {
            public RiverFieldCache()
            {
                Intensity = new double[16, 16];
                Flow = new Vector2[16, 16];
            }

            public double[,] Intensity { get; }
            public Vector2[,] Flow { get; }
            public bool IsInitialized { get; set; }
        }

        public WorldManager(DatabaseHelper database, int worldId = 1)
        {
            _database = database;
            _worldId = worldId;
            _random = new Random();
            _terrainPipeline = new TerrainGenerationPipeline()
                .AddStage(new BaseTerrainStage(this))
                .AddStage(new OreGenerationStage(this))
                .AddStage(new CaveGenerationStage(this))
                .AddStage(new DungeonGenerationStage(this))
                .AddStage(new RiverGenerationStage(this))
                .AddStage(new LakeGenerationStage(this))
                .AddStage(new VegetationGenerationStage(this))
                .AddStage(new CloudGenerationStage(this));
        }

        public async Task<ChunkData?> GetChunkAsync(int chunkX, int chunkZ)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            if (_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
            {
                loadedChunk.LastAccessed = DateTime.UtcNow;
                return loadedChunk.Data;
            }

            var chunkData = await LoadChunkFromDatabase(chunkX, chunkZ);
            if (chunkData == null)
            {
                chunkData = await GenerateChunk(chunkX, chunkZ);
                await SaveChunkToDatabase(chunkX, chunkZ, chunkData);
            }

            _loadedChunks[chunkKey] = new LoadedChunk
            {
                Data = chunkData,
                LastAccessed = DateTime.UtcNow,
                IsModified = false
            };

            return chunkData;
        }

        public async Task UpdateBlockAsync(int chunkX, int chunkZ, int blockX, int blockY, int blockZ, 
            BlockType blockType, int playerId)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            if (!_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
            {
                loadedChunk = new LoadedChunk
                {
                    Data = await GetChunkAsync(chunkX, chunkZ),
                    LastAccessed = DateTime.UtcNow,
                    IsModified = false
                };
                _loadedChunks[chunkKey] = loadedChunk;
            }

            if (loadedChunk.Data != null)
            {
                var localX = blockX % 16;
                var localZ = blockZ % 16;
                
                if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16 && 
                    blockY >= 0 && blockY < 256)
                {
                    loadedChunk.Data.SetBlock(localX, blockY, localZ, blockType);
                    loadedChunk.IsModified = true;
                    loadedChunk.LastAccessed = DateTime.UtcNow;
                    
                    await _database.SaveBlockChangeAsync(_worldId, chunkX, chunkZ, 
                        blockX, blockY, blockZ, (int)blockType, playerId);
                }
            }
        }

        public async Task SaveModifiedChunksAsync()
        {
            var tasks = new List<Task>();
            
            foreach (var kvp in _loadedChunks)
            {
                if (kvp.Value.IsModified)
                {
                    var coords = ParseChunkKey(kvp.Key);
                    tasks.Add(SaveChunkToDatabase(coords.x, coords.z, kvp.Value.Data));
                    kvp.Value.IsModified = false;
                }
            }
            
            await Task.WhenAll(tasks);
        }

        public void UnloadOldChunks(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var chunksToUnload = new List<string>();
            
            foreach (var kvp in _loadedChunks)
            {
                if (kvp.Value.LastAccessed < cutoffTime)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkKey in chunksToUnload)
            {
                if (_loadedChunks.TryRemove(chunkKey, out var chunk) && chunk.IsModified)
                {
                    var coords = ParseChunkKey(chunkKey);
                    _ = SaveChunkToDatabase(coords.x, coords.z, chunk.Data);
                }
            }
        }

        private async Task<ChunkData?> LoadChunkFromDatabase(int chunkX, int chunkZ)
        {
            var result = await _database.LoadChunkAsync(_worldId, chunkX, chunkZ);
            if (result != null)
            {
                return ChunkData.FromBytes(result.Value.blockData, result.Value.biomeData);
            }
            return null;
        }

        private async Task SaveChunkToDatabase(int chunkX, int chunkZ, ChunkData chunkData)
        {
            var (blockData, biomeData) = chunkData.ToBytes();
            await _database.SaveChunkAsync(_worldId, chunkX, chunkZ, blockData, biomeData);
        }

        private async Task<ChunkData> GenerateChunk(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ);
            var context = new TerrainGenerationContext(this, chunk, chunkX, chunkZ);
            _terrainPipeline.Execute(context);
            return chunk;
        }

        private TerrainProfile CalculateTerrainProfile(int worldX, int worldZ)
        {
            var warp = SimplexNoise.DomainWarp(
                worldX,
                worldZ,
                DomainWarpSimplexFrequency,
                DomainWarpPerlinFrequency,
                DomainWarpSimplexAmplitude,
                DomainWarpPerlinAmplitude,
                925113);

            double sampleX = worldX + warp.dx;
            double sampleZ = worldZ + warp.dz;

            var continentalness = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.00045, 5, 1.0, 0.52, 934113));
            var macroPerlin = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.00038, 4, 1.0, 0.5, 420031));
            var erosion = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.0012, 4, 1.0, 0.58, 811223));
            var peaks = SampleRidgedNoise(sampleX, sampleZ, 0.0018, 4, 1.0, 0.48, 51277);
            var hills = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.0028, 4, 1.0, 0.54, 22119));
            var valleyNoise = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.0036, 3, 1.0, 0.45, 20317));
            var humidity = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.00095, 3, 1.0, 0.58, 6711));
            var temperature = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.00075, 3, 1.0, 0.6, 9987));

            double blendedContinental = Math.Clamp(continentalness * 0.65 + macroPerlin * 0.35, 0.0, 1.0);

            if (blendedContinental < OceanThreshold)
            {
                double oceanDepthFactor = Math.Clamp((OceanThreshold - blendedContinental) / OceanThreshold, 0.0, 1.0);
                int depth = 14 + (int)(oceanDepthFactor * 28);
                int seafloor = Math.Max(6, GlobalWaterLevel - depth);

                return new TerrainProfile
                {
                    SurfaceHeight = seafloor,
                    HasWater = true,
                    WaterLevel = GlobalWaterLevel,
                    Biome = BiomeType.Ocean,
                    SurfaceBlock = BlockType.Sand,
                    SubSurfaceBlock = BlockType.Sand,
                    FillerBlock = BlockType.Stone,
                    UseCliffFace = false
                };
            }

            bool isBeach = blendedContinental < BeachThreshold;

            var profile = new TerrainProfile
            {
                HasWater = false,
                WaterLevel = GlobalWaterLevel,
                SurfaceBlock = BlockType.Grass,
                SubSurfaceBlock = BlockType.Dirt,
                FillerBlock = BlockType.Stone,
                Biome = BiomeType.Plains,
                UseCliffFace = false
            };

            int baseHeight = (int)(MinSurfaceHeight + blendedContinental * 62);
            baseHeight -= (int)(erosion * 10);
            baseHeight -= (int)(Math.Max(0.0, 0.55 - valleyNoise) * ValleyDepthMultiplier);
            baseHeight = Math.Clamp(baseHeight, MinSurfaceHeight, MaxSurfaceHeight);

            double hillWeight = Math.Max(0.0, hills - 0.32);
            double mountainWeight = Math.Max(0.0, peaks - 0.46) * (1.0 - erosion * 0.6);
            mountainWeight += Math.Max(0.0, valleyNoise - 0.7) * 0.25;

            int hillContribution = (int)(hillWeight * 20);
            int mountainContribution = (int)(mountainWeight * 64);

            int surfaceHeight = baseHeight + hillContribution + mountainContribution;
            surfaceHeight = Math.Clamp(surfaceHeight, MinSurfaceHeight, MaxSurfaceHeight);
            profile.SurfaceHeight = surfaceHeight;

            bool steep = mountainWeight > CliffThreshold && erosion < 0.35;
            bool mountainous = surfaceHeight > 108 || mountainContribution > 28;

            if (steep)
            {
                profile.Biome = BiomeType.Cliffs;
                profile.SurfaceBlock = BlockType.Cobblestone;
                profile.SubSurfaceBlock = BlockType.Stone;
                profile.UseCliffFace = true;
            }
            else if (mountainous)
            {
                profile.Biome = BiomeType.Mountains;
                profile.SurfaceBlock = BlockType.Stone;
                profile.SubSurfaceBlock = BlockType.Stone;
            }
            else if (hillContribution > 8)
            {
                profile.Biome = BiomeType.Hills;
            }
            else
            {
                profile.Biome = DetermineLandBiome(temperature, humidity);
            }

            if (profile.Biome == BiomeType.Desert)
            {
                profile.SurfaceBlock = BlockType.Sand;
                profile.SubSurfaceBlock = BlockType.Sand;
            }

            if (profile.Biome == BiomeType.Tundra)
            {
                profile.SurfaceBlock = BlockType.Grass;
                profile.SubSurfaceBlock = BlockType.Dirt;
            }

            if (isBeach)
            {
                profile.Biome = BiomeType.Beach;
                profile.SurfaceBlock = BlockType.Sand;
                profile.SubSurfaceBlock = BlockType.Sand;
                profile.HasWater = true;
                profile.WaterLevel = Math.Max(GlobalWaterLevel - 1, profile.SurfaceHeight + 1);
            }

            return profile;
        }

        private void GenerateBaseTerrainInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var profiles = context.GetOrAddMetadata(TerrainProfilesKey, () => new TerrainProfile[16, 16]);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = context.ChunkX * 16 + x;
                    var worldZ = context.ChunkZ * 16 + z;

                    var profile = CalculateTerrainProfile(worldX, worldZ);
                    profiles[x, z] = profile;
                    chunk.SetBiome(x, z, profile.Biome);
                    ApplyTerrainColumn(chunk, x, z, profile);

                    var clearanceTop = profile.HasWater
                        ? Math.Max(profile.WaterLevel, profile.SurfaceHeight)
                        : profile.SurfaceHeight;
                    clearanceTop = Math.Clamp(clearanceTop, 0, 255);

                    for (int y = clearanceTop + 1; y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        private void ApplyTerrainColumn(ChunkData chunk, int x, int z, TerrainProfile profile)
        {
            int surfaceHeight = Math.Clamp(profile.SurfaceHeight, 1, 255);

            for (int y = 0; y <= surfaceHeight && y < 256; y++)
            {
                BlockType block;

                if (y == 0)
                {
                    block = BlockType.Bedrock;
                }
                else if (y < surfaceHeight - 3)
                {
                    block = profile.FillerBlock;
                }
                else if (profile.UseCliffFace && y >= surfaceHeight - 4)
                {
                    block = BlockType.Cobblestone;
                }
                else if (y < surfaceHeight)
                {
                    block = profile.SubSurfaceBlock;
                }
                else
                {
                    block = profile.SurfaceBlock;
                }

                chunk.SetBlock(x, y, z, block);
            }

            if (profile.HasWater)
            {
                int waterTop = Math.Clamp(profile.WaterLevel, surfaceHeight, 255);
                for (int y = surfaceHeight + 1; y <= waterTop && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }
            }
        }

        private BiomeType DetermineLandBiome(double temperature, double humidity)
        {
            if (temperature > 0.7 && humidity < 0.35)
                return BiomeType.Desert;
            if (temperature < 0.3)
                return BiomeType.Tundra;
            if (humidity > 0.6)
                return BiomeType.Forest;
            return BiomeType.Plains;
        }

        private static double NormalizeNoise(double value)
        {
            return Math.Clamp((value + 1.0) * 0.5, 0.0, 1.0);
        }

        private double SampleRidgedNoise(double worldX, double worldZ, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var noise = SimplexNoise.Generate(worldX, worldZ, frequency, octaves, amplitude, persistence, seed);
            noise = Math.Clamp(noise, -1.0, 1.0);
            return 1.0 - Math.Abs(noise);
        }

        /// <summary>
        /// 개선된 3D 동굴 생성 시스템 - 더 자연스럽고 다양한 동굴 구조
        /// </summary>
        private void GenerateCavesInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random((context.ChunkX * 73856093) ^ (context.ChunkZ * 19349663));
            
            // 메인 동굴 시스템 (기존 웜 방식 개선)
            GenerateMainCaveSystem(chunk, rand);
            
            // 소형 동굴방 추가
            GenerateSmallCaveRooms(chunk, rand);
            
            // 수직 동굴 (수직갱)
            GenerateVerticalShafts(chunk, rand);

            // 노이즈 기반 동굴층 추가 (연속성 보장)
            GenerateNoiseCavePass(context, chunk);
        }
        
        /// <summary>
        /// 메인 동굴 시스템 생성
        /// </summary>
        private void GenerateMainCaveSystem(ChunkData chunk, Random rand)
        {
            int wormCount = 1 + rand.Next(3); // 1~3개의 메인 웜
            double radiusNoiseSeed = rand.NextDouble() * 1000.0;
            double directionalNoiseSeed = rand.NextDouble() * 500.0;

            for (int w = 0; w < wormCount; w++)
            {
                double x = rand.Next(16);
                double y = rand.Next(15, 55); // 더 깊은 지하부터
                double z = rand.Next(16);
                int steps = 100 + rand.Next(80); // 더 긴 동굴
                double yaw = rand.NextDouble() * Math.PI * 2.0;
                double pitch = (rand.NextDouble() - 0.5) * 0.4;
                double baseRadius = 2.0 + rand.NextDouble() * 1.5; // 기본 반지름

                for (int s = 0; s < steps; s++)
                {
                    // 동적으로 변하는 반지름 (넓어지고 좁아지는 효과)
                    double radiusNoise = SimplexNoise.Generate(x + radiusNoiseSeed, z + radiusNoiseSeed, 0.12, 2, 1.0, 0.55, 55127);
                    double currentRadius = baseRadius + Math.Sin(s * 0.1) * 0.8 + radiusNoise * 0.6;
                    currentRadius = Math.Clamp(currentRadius, 1.6, baseRadius + 1.9);
                    
                    int cx = (int)Math.Round(x);
                    int cy = (int)Math.Round(y);
                    int cz = (int)Math.Round(z);
                    
                    // 동굴 조각하기
                    CarveSphere(chunk, cx, cy, cz, currentRadius);
                    
                    // 가끔 큰 공간(방) 생성
                    if (s > 20 && rand.NextDouble() < 0.05) // 5% 확률
                    {
                        CarveRoom(chunk, cx, cy, cz, 4 + rand.Next(4));
                        if (rand.NextDouble() < 0.35)
                        {
                            CreateCavePool(chunk, cx, Math.Clamp(cy - rand.Next(1, 3), 2, 120), cz, rand.Next(2, 5));
                        }
                    }

                    // 이동
                    double speed = 0.8 + rand.NextDouble() * 0.4; // 가변 속도
                    x += Math.Cos(yaw) * speed;
                    z += Math.Sin(yaw) * speed;
                    y += Math.Sin(pitch) * 0.3;

                    // 방향 변화 (더 자연스럽게)
                    double directionalNoise = SimplexNoise.Generate(x + directionalNoiseSeed, y + directionalNoiseSeed, 0.05, 2, 1.0, 0.5, 91357);
                    yaw += (rand.NextDouble() - 0.5) * 0.3 + directionalNoise * 0.35;
                    pitch += (rand.NextDouble() - 0.5) * 0.15 + directionalNoise * 0.18;
                    pitch = Math.Clamp(pitch, -0.7, 0.7);

                    // 범위 체크
                    if (x < 0 || x > 15 || z < 0 || z > 15) break;
                    if (y < 5 || y > 100) break;
                }
            }
        }
        
        /// <summary>
        /// 소형 동굴방들 생성
        /// </summary>
        private void GenerateSmallCaveRooms(ChunkData chunk, Random rand)
        {
            int roomCount = rand.Next(2, 6); // 2~5개의 소형 방
            
            for (int i = 0; i < roomCount; i++)
            {
                int roomX = rand.Next(3, 13);
                int roomY = rand.Next(10, 60);
                int roomZ = rand.Next(3, 13);
                int roomSize = rand.Next(3, 7);
                
                CarveRoom(chunk, roomX, roomY, roomZ, roomSize);
            }
        }
        
        /// <summary>
        /// 수직 동굴 (갱도) 생성
        /// </summary>
        private void GenerateVerticalShafts(ChunkData chunk, Random rand)
        {
            if (rand.NextDouble() < 0.3) // 30% 확률로 수직갱 생성
            {
                int shaftX = rand.Next(4, 12);
                int shaftZ = rand.Next(4, 12);
                int shaftTop = rand.Next(80, 120);
                int shaftBottom = rand.Next(10, 30);
                double shaftRadius = 1.5 + rand.NextDouble();
                
                for (int y = shaftBottom; y < shaftTop; y++)
                {
                    CarveSphere(chunk, shaftX, y, shaftZ, shaftRadius);
                    
                    // 가끔 측면 통로 생성
                    if (rand.NextDouble() < 0.1)
                    {
                        int sideLength = rand.Next(3, 8);
                        double sideDirection = rand.NextDouble() * Math.PI * 2;
                        
                        for (int i = 0; i < sideLength; i++)
                        {
                            int sideX = shaftX + (int)(Math.Cos(sideDirection) * i);
                            int sideZ = shaftZ + (int)(Math.Sin(sideDirection) * i);
                            CarveSphere(chunk, sideX, y, sideZ, 1.2);
                        }
                    }
                }

                if (rand.NextDouble() < 0.35)
                {
                    int fillStart = Math.Max(shaftBottom, 1);
                    int fillEnd = Math.Min(shaftBottom + 2, shaftTop - 1);
                    for (int y = fillStart; y <= fillEnd; y++)
                    {
                        chunk.SetBlock(shaftX, y, shaftZ, BlockType.Water);
                    }
                }
            }

        }

        /// <summary>
        /// 노이즈 기반 동굴층 - 연속된 노이즈 필드를 사용하여 청크 경계를 넘는 동굴을 형성한다.
        /// </summary>
        private void GenerateNoiseCavePass(TerrainGenerationContext context, ChunkData chunk)
        {
            int baseX = context.ChunkX * 16;
            int baseZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                int worldX = baseX + x;
                for (int z = 0; z < 16; z++)
                {
                    int worldZ = baseZ + z;

                    double horizontalNoise = SimplexNoise.Generate(worldX, worldZ, NoiseCaveHorizontalFrequency, 4, 1.0, 0.55, 640371);
                    double ridged = SampleRidgedNoise(worldX * 0.85, worldZ * 0.85, NoiseCaveHorizontalFrequency * 1.25, 3, 1.0, 0.5, 91357);

                    for (int y = 8; y < 120; y++)
                    {
                        double verticalNoise = SimplexNoise.Generate(worldX, y, NoiseCaveVerticalFrequency, 3, 1.0, 0.62, 128947);
                        double density = Math.Abs(horizontalNoise) * 0.55 + Math.Abs(verticalNoise) * 0.45;
                        density = density * (0.65 + ridged * 0.35);
                        density -= Math.Clamp((y - 24) / 140.0, 0.0, 0.45);

                        if (density < NoiseCaveThreshold)
                        {
                            var block = chunk.GetBlock(x, y, z);
                            if (block != BlockType.Air && block != BlockType.Water && block != BlockType.Lava)
                            {
                                if (density < NoiseCaveLavaThreshold && y < 18)
                                {
                                    chunk.SetBlock(x, y, z, BlockType.Lava);
                                }
                                else if (density < NoiseCaveWaterThreshold && y < GlobalWaterLevel - 6)
                                {
                                    chunk.SetBlock(x, y, z, BlockType.Water);
                                }
                                else
                                {
                                    chunk.SetBlock(x, y, z, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddCaveDripstoneFeatures(TerrainGenerationContext context, ChunkData chunk)
        {
            var rand = new Random((context.ChunkX * 59359) ^ (context.ChunkZ * 99733) ^ 0x5A5A5A);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int y = 6;
                    while (y < 120)
                    {
                        while (y < 120 && chunk.GetBlock(x, y, z) != BlockType.Air)
                        {
                            y++;
                        }

                        if (y >= 120)
                        {
                            break;
                        }

                        int cavityStart = y;
                        while (y < 120 && chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            y++;
                        }
                        int cavityEnd = y - 1;
                        int cavityHeight = cavityEnd - cavityStart + 1;

                        if (cavityHeight >= 6 && rand.NextDouble() < 0.18)
                        {
                            int topSupportY = Math.Min(cavityEnd + 1, 255);
                            int bottomSupportY = Math.Max(cavityStart - 1, 0);
                            if (topSupportY >= 256 || bottomSupportY <= 0)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            var ceilingBlock = chunk.GetBlock(x, topSupportY, z);
                            var floorBlock = chunk.GetBlock(x, bottomSupportY, z);
                            if (ceilingBlock == BlockType.Air || floorBlock == BlockType.Air)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            int maxFeatureHeight = Math.Min(3, (cavityHeight - 2) / 2);
                            if (maxFeatureHeight > 0)
                            {
                                int stalagmiteHeight = 1 + rand.Next(maxFeatureHeight);
                                int stalactiteHeight = 1 + rand.Next(maxFeatureHeight);
                                if (stalagmiteHeight + stalactiteHeight >= cavityHeight - 1)
                                {
                                    stalagmiteHeight = Math.Max(1, maxFeatureHeight - 1);
                                    stalactiteHeight = Math.Max(1, maxFeatureHeight);
                                }

                                for (int i = 0; i < stalagmiteHeight; i++)
                                {
                                    int py = cavityStart + i;
                                    if (py >= cavityEnd)
                                    {
                                        break;
                                    }
                                    chunk.SetBlock(x, py, z, BlockType.Stone);
                                }

                                for (int i = 0; i < stalactiteHeight; i++)
                                {
                                    int py = cavityEnd - i;
                                    if (py <= cavityStart + 1)
                                    {
                                        break;
                                    }
                                    chunk.SetBlock(x, py, z, BlockType.Stone);
                                }
                            }
                        }

                        y = cavityEnd + 1;
                    }
                }
            }

        }

        /// <summary>
        /// 동굴방 조각하기
        /// </summary>
        private void CarveRoom(ChunkData chunk, int centerX, int centerY, int centerZ, int size)
        {
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dy = -size/2; dy <= size/2; dy++) // 방은 수평적으로 더 넓게
                {
                    for (int dz = -size; dz <= size; dz++)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;
                        int z = centerZ + dz;
                        
                        if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 1 && y < 255)
                        {
                            double dist = Math.Sqrt(dx*dx + dy*dy*1.5 + dz*dz); // 수직 압축
                            if (dist <= size)
                            {
                                var blockType = chunk.GetBlock(x, y, z);
                                if (blockType != BlockType.Air && blockType != BlockType.Water && blockType != BlockType.Lava)
                                {
                                    chunk.SetBlock(x, y, z, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carves a spherical pocket of air centered at (cx,cy,cz).
        /// </summary>
        private void CarveSphere(ChunkData chunk, int cx, int cy, int cz, double radius)
        {
            int r = (int)Math.Ceiling(radius);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        int x = cx + dx;
                        int y = cy + dy;
                        int z = cz + dz;
                        if (x < 0 || x >= 16 || z < 0 || z >= 16 || y < 1 || y >= 255) continue;

                        double dist2 = dx * dx + dy * dy + dz * dz;
                        if (dist2 <= radius * radius)
                        {
                            // Only carve solid materials
                            var bt = chunk.GetBlock(x, y, z);
                            if (bt != BlockType.Air && bt != BlockType.Water && bt != BlockType.Lava)
                                chunk.SetBlock(x, y, z, BlockType.Air);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 개선된 던전 생성 시스템 - 더 복잡하고 다양한 구조의 던전
        /// </summary>
        private void GenerateDungeonsInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random((context.ChunkX * 83492791) ^ (context.ChunkZ * 297657976));
            if (rand.NextDouble() > 0.15) return; // 15% 확률로 증가

            // 던전 타입 결정
            DungeonType dungeonType = (DungeonType)rand.Next(3);
            
            switch (dungeonType)
            {
                case DungeonType.SimpleRoom:
                    GenerateSimpleDungeon(chunk, rand);
                    break;
                case DungeonType.MultiRoom:
                    GenerateMultiRoomDungeon(chunk, rand);
                    break;
                case DungeonType.Maze:
                    GenerateMazeDungeon(chunk, rand);
                    break;
            }
        }

        private double SampleRiverField(double worldX, double worldZ)
        {
            var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.0008, 0.0016, 20.0, 12.0, 91111);
            double sampleX = worldX + warp.dx;
            double sampleZ = worldZ + warp.dz;
            return SimplexNoise.Generate(sampleX, sampleZ, 0.0012, 5, 1.0, 0.45, 91111);
        }

        private Vector2 ComputeRiverFlowVector(int worldX, int worldZ)
        {
            const double gradientStep = 1.0;

            double forwardX = SampleRiverField(worldX + gradientStep, worldZ);
            double backwardX = SampleRiverField(worldX - gradientStep, worldZ);
            double forwardZ = SampleRiverField(worldX, worldZ + gradientStep);
            double backwardZ = SampleRiverField(worldX, worldZ - gradientStep);

            var flow = new Vector2((float)(forwardX - backwardX), (float)(forwardZ - backwardZ));
            if (flow.LengthSquared() < 1e-4f)
            {
                flow = new Vector2(
                    (float)(SampleRiverField(worldX + 23.0, worldZ + 7.0) - 0.5),
                    (float)(SampleRiverField(worldX - 19.0, worldZ - 11.0) - 0.5));
            }

            if (flow.LengthSquared() < 1e-6f)
            {
                return Vector2.UnitX;
            }

            return Vector2.Normalize(flow);
        }

        private RiverFieldCache GetRiverFieldCache(TerrainGenerationContext context)
        {
            var cache = context.GetOrAddMetadata(RiverFieldCacheKey, () => new RiverFieldCache());
            if (!cache.IsInitialized)
            {
                PopulateRiverFieldCache(cache, context);
                cache.IsInitialized = true;
            }

            return cache;
        }

        private void PopulateRiverFieldCache(RiverFieldCache cache, TerrainGenerationContext context)
        {
            int baseX = context.ChunkX * 16;
            int baseZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                int worldX = baseX + x;
                for (int z = 0; z < 16; z++)
                {
                    int worldZ = baseZ + z;
                    double intensity = Math.Abs(SampleRiverField(worldX, worldZ));
                    cache.Intensity[x, z] = intensity;
                    cache.Flow[x, z] = ComputeRiverFlowVector(worldX, worldZ);
                }
            }

        }

        private void GenerateRiversInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var riverField = GetRiverFieldCache(context);
            TerrainProfile[,]? profiles = null;
            context.TryGetMetadata(TerrainProfilesKey, out profiles);

            int[,] surfaceCache = new int[16, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    surfaceCache[x, z] = FindSurfaceLevel(chunk, x, z);
                }
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (profiles != null && profiles[x, z].Biome == BiomeType.Ocean)
                    {
                        continue;
                    }

                    if (IsOceanColumn(chunk, x, z))
                    {
                        continue;
                    }

                    double intensity = riverField.Intensity[x, z];
                    if (intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    Vector2 flowDir = riverField.Flow[x, z];

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(surface, GlobalWaterLevel);

                    if (intensity < RiverCenterThreshold)
                    {
                        double normalized = 1.0 - Math.Clamp(intensity / RiverCenterThreshold, 0.0, 1.0);
                        CarveRiverColumn(chunk, surfaceCache, x, z, riverSurface, normalized, flowDir);
                    }
                    else
                    {
                        double bankStrength = 1.0 - Math.Clamp((intensity - RiverCenterThreshold) / (RiverBankThreshold - RiverCenterThreshold), 0.0, 1.0);
                        FeatherRiverBank(chunk, surfaceCache, x, z, bankStrength, riverSurface, flowDir);
                    }
                }
            }

        }

        private void CreateCavePool(ChunkData chunk, int centerX, int centerY, int centerZ, int radius)
        {
            if (centerY <= 1 || centerY >= 255)
                return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                        continue;

                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance > radius)
                        continue;

                    int floorY = Math.Max(1, centerY - 1);
                    chunk.SetBlock(x, floorY, z, BlockType.Sand);
                    chunk.SetBlock(x, centerY, z, BlockType.Water);

                    if (centerY + 1 < 256)
                    {
                        chunk.SetBlock(x, centerY + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void GenerateLakesInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var riverField = GetRiverFieldCache(context);
            var warp = SimplexNoise.DomainWarp(context.ChunkX * 16, context.ChunkZ * 16, 0.00045, 0.0009, 14.0, 9.0, 67891);
            double lakeSimplex = SimplexNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.035, 3, 1.0, 0.55, 67891);
            double lakePerlin = PerlinNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.028, 2, 1.0, 0.6, 77811);
            double lakeNoise = (lakeSimplex + lakePerlin) * 0.5;
            if (lakeNoise < 0.62)
                return;

            var rand = new Random((context.ChunkX * 928371) ^ (context.ChunkZ * 72341) ^ 0xC0FFEE);
            if (rand.NextDouble() > (lakeNoise - 0.62) * 1.8)
                return;

            int centerX = rand.Next(4, 12);
            int centerZ = rand.Next(4, 12);
            int radiusX = 3 + rand.Next(4);
            int radiusZ = 3 + rand.Next(4);
            int maxDepth = 3 + rand.Next(3);
            int waterLevel = Math.Clamp(GlobalWaterLevel + rand.Next(-1, 2), 45, 80);

            int sampleSurface = FindSurfaceLevel(chunk, centerX, centerZ);
            if (sampleSurface < waterLevel - 4 || sampleSurface > waterLevel + 8)
                return;

            double rotationNoise = SimplexNoise.Generate(context.ChunkX * 0.37 + warp.dx, context.ChunkZ * 0.37 + warp.dz, 0.12, 2, 1.0, 0.6, 91217);
            double rotation = rotationNoise * Math.PI;
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double radiusXWithPadding = radiusX + 0.75;
            double radiusZWithPadding = radiusZ + 0.75;
            bool lakeCreated = false;

            for (int dx = -radiusX - 3; dx <= radiusX + 3; dx++)
            {
                for (int dz = -radiusZ - 3; dz <= radiusZ + 3; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                        continue;

                    double rotX = dx * cos - dz * sin;
                    double rotZ = dx * sin + dz * cos;
                    double ellipse = Math.Sqrt(
                        (rotX * rotX) / (radiusXWithPadding * radiusXWithPadding) +
                        (rotZ * rotZ) / (radiusZWithPadding * radiusZWithPadding));
                    double sdf = ellipse - 1.0;

                    if (sdf <= 0.18)
                    {
                        int surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                            continue;

                        double bowl = Math.Clamp(1.0 - ellipse, 0.0, 1.0);
                        double depthNoise = SimplexNoise.Generate(
                            context.ChunkX * 16 + x,
                            context.ChunkZ * 16 + z,
                            0.18,
                            2,
                            1.0,
                            0.45,
                            82319);
                        double depthFactor = Math.Clamp(bowl + depthNoise * 0.2, 0.0, 1.0);
                        int columnDepth = Math.Clamp(maxDepth + (int)Math.Round(depthFactor * maxDepth * 0.7), 2, maxDepth + 3);
                        int waterFloor = Math.Max(1, waterLevel - columnDepth);

                        for (int y = surface; y >= waterFloor; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        if (waterFloor - 1 >= 0)
                        {
                            chunk.SetBlock(x, waterFloor - 1, z, BlockType.Sand);
                        }

                        for (int y = waterFloor; y <= waterLevel && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        if (waterLevel < GlobalWaterLevel)
                        {
                            for (int y = waterLevel + 1; y <= GlobalWaterLevel && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }

                        lakeCreated = true;

                        int topWater = waterLevel < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(waterLevel, 255);
                        if (topWater + 1 < 256)
                        {
                            chunk.SetBlock(x, topWater + 1, z, BlockType.Air);
                        }
                    }
                    else if (sdf <= 0.45)
                    {
                        double rimStrength = Math.Clamp((0.45 - sdf) / 0.45, 0.0, 1.0);
                        SculptLakeBank(chunk, x, z, waterLevel, rimStrength);
                    }
                }
            }

            if (lakeCreated)
            {
                TryLinkLakeToRiver(context, chunk, riverField, centerX, centerZ, waterLevel, radiusX, radiusZ);
            }
        }

        private void SculptLakeBank(ChunkData chunk, int x, int z, int waterSurface, double rimStrength)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
                return;

            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            int maxDrop = Math.Max(1, (int)Math.Round(3.0 - 2.0 * rimStrength));

            if (surface > waterSurface + maxDrop)
            {
                int target = Math.Max(waterSurface + maxDrop, 1);
                for (int y = surface; y > target; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
                surface = target;
            }

            if (surface <= waterSurface)
            {
                for (int y = surface; y <= waterSurface && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                int topWater = waterSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(waterSurface, 255);
                if (topWater + 1 < 256)
                {
                    chunk.SetBlock(x, topWater + 1, z, BlockType.Air);
                }
            }
            else
            {
                chunk.SetBlock(x, surface, z, BlockType.Sand);
                if (surface + 1 < 256)
                {
                    chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                }
            }

        }

        private void TryLinkLakeToRiver(TerrainGenerationContext context, ChunkData chunk, RiverFieldCache riverField, int centerX, int centerZ, int waterLevel, int radiusX, int radiusZ)
        {
            int searchRadius = Math.Max(radiusX, radiusZ) + 6;
            double bestIntensity = double.MaxValue;
            int bestX = -1;
            int bestZ = -1;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double intensity = riverField.Intensity[x, z];
                    if (intensity < bestIntensity && intensity < RiverBankThreshold)
                    {
                        bestIntensity = intensity;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            if (bestX < 0 || bestZ < 0)
            {
                return;
            }

            int dxTarget = bestX - centerX;
            int dzTarget = bestZ - centerZ;
            double length = Math.Sqrt(dxTarget * dxTarget + dzTarget * dzTarget);
            int startX = centerX;
            int startZ = centerZ;
            if (length > 1.0)
            {
                double normDx = dxTarget / length;
                double normDz = dzTarget / length;
                int offset = Math.Max(Math.Max(radiusX, radiusZ) - 1, 1);
                startX = centerX + (int)Math.Round(normDx * offset);
                startZ = centerZ + (int)Math.Round(normDz * offset);
            }

            startX = Math.Clamp(startX, 0, 15);
            startZ = Math.Clamp(startZ, 0, 15);

            CarveLakeChannel(chunk, startX, startZ, bestX, bestZ, waterLevel);
        }

        private void CarveLakeChannel(ChunkData chunk, int startX, int startZ, int endX, int endZ, int waterLevel)
        {
            int dx = endX - startX;
            int dz = endZ - startZ;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (steps == 0)
            {
                return;
            }

            for (int step = 0; step <= steps; step++)
            {
                double t = step / (double)steps;
                int x = startX + (int)Math.Round(dx * t);
                int z = startZ + (int)Math.Round(dz * t);
                if (x < 0 || x >= 16 || z < 0 || z >= 16)
                {
                    continue;
                }

                int surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                {
                    continue;
                }

                int carvingFloor = Math.Max(1, waterLevel - 2);
                if (surface > waterLevel + 1)
                {
                    for (int y = surface; y > waterLevel + 1; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                    surface = waterLevel + 1;
                }

                for (int y = surface; y >= carvingFloor; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }

                if (carvingFloor - 1 >= 1)
                {
                    chunk.SetBlock(x, carvingFloor - 1, z, BlockType.Sand);
                }

                for (int y = carvingFloor; y <= waterLevel && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                if (waterLevel + 1 < 256)
                {
                    chunk.SetBlock(x, waterLevel + 1, z, BlockType.Air);
                }

                // Slight widening for natural look
                for (int widen = -1; widen <= 1; widen++)
                {
                    if (widen == 0)
                    {
                        continue;
                    }

                    int wx = x + widen;
                    if (wx < 0 || wx >= 16)
                    {
                        continue;
                    }

                    int wSurface = FindSurfaceLevel(chunk, wx, z);
                    if (wSurface <= 0)
                    {
                        continue;
                    }

                    if (wSurface > waterLevel)
                    {
                        chunk.SetBlock(wx, wSurface, z, BlockType.Sand);
                    }
                }
            }
        }

        private void GenerateCloudsInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = context.ChunkX * 16 + x;
                    var worldZ = context.ChunkZ * 16 + z;

                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.0005, 0.001, 12.0, 6.0, 44444);
                    double noiseSimplex = SimplexNoise.Generate(worldX + warp.dx, worldZ + warp.dz, 0.0009, 4, 1.0, 0.6, 44444);
                    double noisePerlin = PerlinNoise.Generate(worldX + warp.dx, worldZ + warp.dz, 0.0012, 2, 1.0, 0.55, 55444);
                    double noise = (noiseSimplex * 0.65) + (noisePerlin * 0.35);
                    if (noise <= 0.68)
                        continue;

                    int baseAltitude = CloudBaseAltitude + (int)((noise - 0.68) * 45);
                    baseAltitude = Math.Clamp(baseAltitude, CloudBaseAltitude, 255);

                    if (chunk.GetBlock(x, baseAltitude, z) == BlockType.Air)
                    {
                        chunk.SetBlock(x, baseAltitude, z, BlockType.Cloud);

                        if (noise > 0.78 && baseAltitude + 1 < 256)
                        {
                            chunk.SetBlock(x, baseAltitude + 1, z, BlockType.Cloud);
                        }
                    }
                }
            }
        }

        private void CarveRiverColumn(ChunkData chunk, int[,] surfaceCache, int x, int z, int riverSurface, double normalized, Vector2 flowDir)
        {
            int surface = surfaceCache[x, z];
            if (surface <= 0)
                return;

            if (surface <= GlobalWaterLevel - 3 && chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            int channelDepth = Math.Clamp(3 + (int)Math.Round(normalized * 5.5), 3, 8);
            int waterFloor = Math.Max(1, riverSurface - channelDepth);

            for (int y = surface; y >= waterFloor; y--)
            {
                chunk.SetBlock(x, y, z, BlockType.Air);
            }

            if (waterFloor - 1 >= 0)
            {
                chunk.SetBlock(x, waterFloor - 1, z, BlockType.Sand);
            }

            for (int y = waterFloor; y <= riverSurface && y < 256; y++)
            {
                chunk.SetBlock(x, y, z, BlockType.Water);
            }

            if (riverSurface < GlobalWaterLevel)
            {
                for (int y = riverSurface + 1; y <= GlobalWaterLevel && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }
            }

            int updatedSurface = riverSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(riverSurface, 255);
            surfaceCache[x, z] = updatedSurface;

            int maxRadius = Math.Clamp(2 + (int)Math.Round(normalized * 2.0), 2, 4);
            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dz = -maxRadius; dz <= maxRadius; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance > maxRadius + 0.25)
                        continue;

                    double falloff = 1.0 - Math.Clamp(distance / (maxRadius + 0.001), 0.0, 1.0);
                    bool allowFlood = distance <= 1.5;
                    ShapeRiverBank(chunk, surfaceCache, x + dx, z + dz, falloff, riverSurface, allowFlood);
                }
            }

            Vector2 normalizedFlow = flowDir.LengthSquared() < 1e-6f ? Vector2.UnitX : Vector2.Normalize(flowDir);
            int forwardX = x + (int)Math.Round(normalizedFlow.X);
            int forwardZ = z + (int)Math.Round(normalizedFlow.Y);
            ShapeRiverBank(chunk, surfaceCache, forwardX, forwardZ, 0.35, riverSurface, false);

            int backX = x - (int)Math.Round(normalizedFlow.X);
            int backZ = z - (int)Math.Round(normalizedFlow.Y);
            ShapeRiverBank(chunk, surfaceCache, backX, backZ, 0.35, riverSurface, false);
        }

        private void FeatherRiverBank(ChunkData chunk, int[,] surfaceCache, int x, int z, double strength, int riverSurface, Vector2 flowDir)
        {
            if (strength <= 0.0)
                return;

            double clamped = Math.Clamp(strength, 0.0, 1.0);
            ShapeRiverBank(chunk, surfaceCache, x, z, clamped * 0.65, riverSurface, false);

            Vector2 perpendicular = new(-flowDir.Y, flowDir.X);
            if (perpendicular.LengthSquared() < 1e-6f)
            {
                perpendicular = Vector2.UnitX;
            }
            perpendicular = Vector2.Normalize(perpendicular);

            int reach = Math.Max(1, (int)Math.Round(1.0 + clamped * 2.0));
            for (int step = 1; step <= reach; step++)
            {
                double falloff = Math.Clamp(clamped - step * 0.25, 0.0, 1.0);
                if (falloff <= 0.0)
                {
                    break;
                }

                int offsetX = x + (int)Math.Round(perpendicular.X * step);
                int offsetZ = z + (int)Math.Round(perpendicular.Y * step);
                ShapeRiverBank(chunk, surfaceCache, offsetX, offsetZ, falloff, riverSurface, false);

                offsetX = x - (int)Math.Round(perpendicular.X * step);
                offsetZ = z - (int)Math.Round(perpendicular.Y * step);
                ShapeRiverBank(chunk, surfaceCache, offsetX, offsetZ, falloff, riverSurface, false);
            }
        }

        private void ApplyRiverbankErosion(ChunkData chunk, int[,] surfaceCache, RiverFieldCache riverField)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = riverField.Intensity[x, z];
                    if (intensity >= RiverBankThreshold + 0.01)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
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
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                neighborSurface = FindSurfaceLevel(chunk, nx, nz);
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
                        int target = Math.Max(neighborAverage + 1, 1);
                        for (int y = surface; y > target; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }
                        surface = target;
                        surfaceCache[x, z] = surface;
                    }

                    var topBlock = chunk.GetBlock(x, surface, z);
                    if (topBlock == BlockType.Grass || topBlock == BlockType.Dirt)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                    }

                    if (intensity < RiverCenterThreshold * 0.6 && surface - 1 >= 1)
                    {
                        var belowBlock = chunk.GetBlock(x, surface - 1, z);
                        if (belowBlock == BlockType.Dirt || belowBlock == BlockType.Grass)
                        {
                            chunk.SetBlock(x, surface - 1, z, BlockType.Sand);
                        }
                    }

                    if (surface + 1 < 256 && chunk.GetBlock(x, surface + 1, z) != BlockType.Air)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                    }
                }
            }

        }

        private void ShapeRiverBank(ChunkData chunk, int[,] surfaceCache, int x, int z, double falloff, int riverSurface, bool allowFlood)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
                return;

            int surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                    return;
                surfaceCache[x, z] = surface;
            }

            int maxDrop = Math.Max(1, (int)Math.Round(3.0 - 2.0 * falloff));

            if (surface > riverSurface + maxDrop)
            {
                int target = Math.Max(riverSurface + maxDrop, 1);
                for (int y = surface; y > target; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
                surface = target;
                surfaceCache[x, z] = target;
            }

            if (allowFlood && surface <= riverSurface)
            {
                for (int y = Math.Max(surface, 1); y <= riverSurface && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                int updatedSurface = riverSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(riverSurface, 255);
                surfaceCache[x, z] = updatedSurface;
                surface = updatedSurface;
            }
            else
            {
                chunk.SetBlock(x, surface, z, BlockType.Sand);
            }

            if (surface + 1 < 256)
            {
                chunk.SetBlock(x, surface + 1, z, BlockType.Air);
            }
        }

        private bool IsOceanColumn(ChunkData chunk, int x, int z)
        {
            var biome = chunk.GetBiome(x, z);
            if (biome == BiomeType.Ocean)
                return true;

            var waterAtLevel = GlobalWaterLevel >= 0 && GlobalWaterLevel < 256 && chunk.GetBlock(x, GlobalWaterLevel, z) == BlockType.Water;
            var waterBelow = GlobalWaterLevel - 1 >= 0 && chunk.GetBlock(x, GlobalWaterLevel - 1, z) == BlockType.Water;
            return waterAtLevel && waterBelow;
        }

        
        /// <summary>
        /// 던전 타입 열거형
        /// </summary>
        private enum DungeonType
        {
            SimpleRoom,
            MultiRoom,
            Maze
        }
        
        /// <summary>
        /// 단순한 방 형태 던전
        /// </summary>
        private void GenerateSimpleDungeon(ChunkData chunk, Random rand)
        {
            int roomWidth = 6 + rand.Next(4);   // 6..9
            int roomHeight = 4 + rand.Next(2);  // 4..5
            int roomDepth = 6 + rand.Next(4);

            int ox = rand.Next(2, 16 - roomWidth - 2);
            int oy = rand.Next(15, 40); // 더 깊은 지하
            int oz = rand.Next(2, 16 - roomDepth - 2);

            BuildDungeonRoom(chunk, rand, ox, oy, oz, roomWidth, roomHeight, roomDepth);
            
            // 보물 상자 위치 (중앙)
            int treasureX = ox + roomWidth / 2;
            int treasureZ = oz + roomDepth / 2;
            // TODO: 보물 상자 블록 추가 시 사용
            // chunk.SetBlock(treasureX, oy + 1, treasureZ, BlockType.Chest);
        }
        
        /// <summary>
        /// 다중 방 던전
        /// </summary>
        private void GenerateMultiRoomDungeon(ChunkData chunk, Random rand)
        {
            int roomCount = 2 + rand.Next(3); // 2~4개 방
            
            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = 5 + rand.Next(3);
                int roomHeight = 3 + rand.Next(2);
                int roomDepth = 5 + rand.Next(3);

                int ox = rand.Next(1, 16 - roomWidth - 1);
                int oy = rand.Next(15, 35);
                int oz = rand.Next(1, 16 - roomDepth - 1);
                
                BuildDungeonRoom(chunk, rand, ox, oy, oz, roomWidth, roomHeight, roomDepth);
                
                // 방들 사이에 복도 연결 (간단한 버전)
                if (i > 0)
                {
                    ConnectRooms(chunk, ox + roomWidth/2, oy + 1, oz + roomDepth/2, rand);
                }
            }
        }
        
        /// <summary>
        /// 미로 형태 던전
        /// </summary>
        private void GenerateMazeDungeon(ChunkData chunk, Random rand)
        {
            int startX = rand.Next(2, 6);
            int startZ = rand.Next(2, 6);
            int mazeY = rand.Next(20, 35);
            int mazeSize = 8; // 8x8 미로
            
            // 간단한 미로 생성 (더 복잡한 알고리즘으로 확장 가능)
            for (int x = 0; x < mazeSize; x++)
            {
                for (int z = 0; z < mazeSize; z++)
                {
                    int worldX = startX + x;
                    int worldZ = startZ + z;
                    
                    if (worldX < 16 && worldZ < 16)
                    {
                        // 체스판 패턴으로 벽과 통로 생성
                        if ((x + z) % 2 == 0 || rand.NextDouble() < 0.3)
                        {
                            // 통로
                            chunk.SetBlock(worldX, mazeY, worldZ, BlockType.Air);
                            chunk.SetBlock(worldX, mazeY + 1, worldZ, BlockType.Air);
                            chunk.SetBlock(worldX, mazeY + 2, worldZ, BlockType.Air);
                        }
                        else
                        {
                            // 벽
                            for (int y = 0; y < 4; y++)
                            {
                                chunk.SetBlock(worldX, mazeY + y, worldZ, BlockType.Cobblestone);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 던전 방 건설
        /// </summary>
        private void BuildDungeonRoom(ChunkData chunk, Random rand, int ox, int oy, int oz, int width, int height, int depth)
        {
            // 내부 비우기
            for (int x = ox + 1; x < ox + width - 1; x++)
            {
                for (int y = oy + 1; y < oy + height - 1; y++)
                {
                    for (int z = oz + 1; z < oz + depth - 1; z++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }

            // 벽, 바닥, 천장 건설
            for (int x = ox; x < ox + width; x++)
            {
                for (int y = oy; y < oy + height; y++)
                {
                    for (int z = oz; z < oz + depth; z++)
                    {
                        bool isWall = (x == ox || x == ox + width - 1 || 
                                      z == oz || z == oz + depth - 1 || 
                                      y == oy || y == oy + height - 1);
                        if (isWall)
                        {
                            // 다양한 재료 사용
                            BlockType wallMaterial = GetDungeonWallMaterial(rand);
                            chunk.SetBlock(x, y, z, wallMaterial);
                        }
                    }
                }
            }

            // 입구 생성 (더 자연스럽게)
            CreateDungeonEntrance(chunk, ox, oy, oz, width, depth);

            DecorateDungeonInterior(chunk, ox, oy, oz, width, height, depth, rand);
        }
        
        /// <summary>
        /// 던전 벽 재료 결정
        /// </summary>
        private BlockType GetDungeonWallMaterial(Random rand)
        {
            var materials = new[] { BlockType.Cobblestone, BlockType.Stone, BlockType.Stone };
            return materials[rand.Next(materials.Length)];
        }
        
        /// <summary>
        /// 던전 입구 생성
        /// </summary>
        private void CreateDungeonEntrance(ChunkData chunk, int ox, int oy, int oz, int width, int depth)
        {
            // 정면에 2x2 입구 생성
            for (int y = oy + 1; y < oy + 3; y++)
            {
                for (int x = ox + width/2 - 1; x <= ox + width/2; x++)
                {
                    chunk.SetBlock(x, y, oz, BlockType.Air);
                }
            }
        }

        private void DecorateDungeonInterior(ChunkData chunk, int ox, int oy, int oz, int width, int height, int depth, Random rand)
        {
            if (width > 4 && depth > 4)
            {
                var supports = new (int x, int z)[]
                {
                    (ox + 1, oz + 1),
                    (ox + width - 2, oz + 1),
                    (ox + 1, oz + depth - 2),
                    (ox + width - 2, oz + depth - 2)
                };

                int supportTop = Math.Max(oy + 1, Math.Min(oy + height - 2, 255));
                foreach (var (sx, sz) in supports)
                {
                    if (sx <= ox || sx >= ox + width - 1 || sz <= oz || sz >= oz + depth - 1)
                    {
                        continue;
                    }

                    for (int y = oy + 1; y <= supportTop; y++)
                    {
                        if (chunk.GetBlock(sx, y, sz) == BlockType.Air)
                        {
                            chunk.SetBlock(sx, y, sz, BlockType.Cobblestone);
                        }
                    }
                }
            }

            if (width > 6 && depth > 6 && rand.NextDouble() < 0.35)
            {
                int poolX = rand.Next(ox + 2, ox + width - 2);
                int poolZ = rand.Next(oz + 2, oz + depth - 2);
                var fluid = rand.NextDouble() < 0.55 ? BlockType.Water : BlockType.Lava;
                chunk.SetBlock(poolX, oy, poolZ, fluid);
            }

            if (width > 3 && depth > 3)
            {
                int treasureCount = rand.Next(1, 1 + Math.Max(1, (width * depth) / 24));
                for (int i = 0; i < treasureCount; i++)
                {
                    int lootX = rand.Next(ox + 1, ox + width - 1);
                    int lootZ = rand.Next(oz + 1, oz + depth - 1);

                    if (chunk.GetBlock(lootX, oy + 1, lootZ) == BlockType.Air)
                    {
                        chunk.SetBlock(lootX, oy, lootZ, BlockType.Cobblestone);
                        var lootBlock = rand.NextDouble() < 0.7 ? BlockType.GoldOre : BlockType.DiamondOre;
                        chunk.SetBlock(lootX, oy + 1, lootZ, lootBlock);
                    }
                }
            }
        }

        /// <summary>
        /// 방들을 복도로 연결
        /// </summary>
        private void ConnectRooms(ChunkData chunk, int x, int y, int z, Random rand)
        {
            // 간단한 직선 복도 (더 복잡한 연결 로직으로 확장 가능)
            int corridorLength = rand.Next(3, 8);
            int direction = rand.Next(4); // 0:북, 1:동, 2:남, 3:서
            
            int[] dx = {0, 1, 0, -1};
            int[] dz = {-1, 0, 1, 0};
            
            for (int i = 0; i < corridorLength; i++)
            {
                int newX = x + dx[direction] * i;
                int newZ = z + dz[direction] * i;
                
                if (newX >= 0 && newX < 16 && newZ >= 0 && newZ < 16)
                {
                    chunk.SetBlock(newX, y, newZ, BlockType.Air);
                    chunk.SetBlock(newX, y + 1, newZ, BlockType.Air);
                    chunk.SetBlock(newX, y + 2, newZ, BlockType.Air);
                }
            }
        }

        private int GenerateHeight(int worldX, int worldZ)
        {
            var profile = CalculateTerrainProfile(worldX, worldZ);
            var columnTop = profile.HasWater
                ? Math.Max(profile.SurfaceHeight, profile.WaterLevel)
                : profile.SurfaceHeight;
            return Math.Clamp(columnTop, 0, 255);
        }

        private BiomeType GenerateBiome(int worldX, int worldZ)
        {
            var profile = CalculateTerrainProfile(worldX, worldZ);
            return profile.Biome;
        }

        public BiomeType SampleBiome(int worldX, int worldZ)
        {
            return CalculateTerrainProfile(worldX, worldZ).Biome;
        }

        /// <summary>
        /// 개선된 광물 생성 시스템 - 더 현실적이고 균형 잡힌 분배
        /// </summary>
        private void GenerateOresInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random(context.ChunkX * 1000 + context.ChunkZ);

            // 각 광물별로 사실적인 깊이와 희귀성 설정
            GenerateOreType(chunk, rand, BlockType.CoalOre, 5, 50, 12, 6);      // 석탄: 언제나, 여러 층에서
            GenerateOreType(chunk, rand, BlockType.IronOre, 1, 40, 8, 4);       // 철: 중간 깊이
            GenerateOreType(chunk, rand, BlockType.GoldOre, 1, 25, 4, 3);       // 금: 깊은 곳
            GenerateOreType(chunk, rand, BlockType.DiamondOre, 1, 16, 2, 2);    // 다이아몬드: 가장 깊은 곳
        }
        
        /// <summary>
        /// 특정 광물 종류를 생성
        /// </summary>
        private void GenerateOreType(ChunkData chunk, Random rand, BlockType oreType, 
            int minY, int maxY, int maxVeins, int maxVeinSize)
        {
            int veinCount = rand.Next(1, maxVeins + 1);
            
            for (int vein = 0; vein < veinCount; vein++)
            {
                int centerX = rand.Next(16);
                int centerY = rand.Next(minY, maxY + 1);
                int centerZ = rand.Next(16);
                
                // 광맥 크기 결정
                int veinSize = rand.Next(1, maxVeinSize + 1);
                
                // 광맥 모양 생성 (구형이 아닌 불규칙한 형태)
                GenerateOreVein(chunk, rand, oreType, centerX, centerY, centerZ, veinSize);
            }
        }
        
        /// <summary>
        /// 광맥을 불규칙한 형태로 생성
        /// </summary>
        private void GenerateOreVein(ChunkData chunk, Random rand, BlockType oreType, 
            int centerX, int centerY, int centerZ, int size)
        {
            var oreBlocks = new List<(int x, int y, int z)>();
            
            // 시작점 추가
            oreBlocks.Add((centerX, centerY, centerZ));
            
            // 주변으로 확산
            for (int i = 0; i < size - 1; i++)
            {
                if (oreBlocks.Count == 0) break;
                
                // 기존 광물 블록 중 무작위로 하나 선택
                var baseBlock = oreBlocks[rand.Next(oreBlocks.Count)];
                
                // 6방향 중 무작위로 확산
                var directions = new (int dx, int dy, int dz)[] 
                {
                    (1, 0, 0), (-1, 0, 0), (0, 1, 0), 
                    (0, -1, 0), (0, 0, 1), (0, 0, -1)
                };
                
                var direction = directions[rand.Next(directions.Length)];
                int newX = baseBlock.x + direction.dx;
                int newY = baseBlock.y + direction.dy;
                int newZ = baseBlock.z + direction.dz;
                
                // 범위 체크 및 중복 방지
                if (newX >= 0 && newX < 16 && newY >= 0 && newY < 256 && newZ >= 0 && newZ < 16)
                {
                    if (!oreBlocks.Contains((newX, newY, newZ)))
                    {
                        oreBlocks.Add((newX, newY, newZ));
                    }
                }
            }
            
            // 실제로 광물 블록 배치
            foreach (var (x, y, z) in oreBlocks)
            {
                if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                {
                    chunk.SetBlock(x, y, z, oreType);
                }
            }
        }

        private void GenerateVegetationInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random(context.ChunkX * 2000 + context.ChunkZ);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var biome = chunk.GetBiome(x, z);
                    var surfaceY = FindSurfaceLevel(chunk, x, z);
                    var globalX = context.ChunkX * 16 + x;
                    var globalZ = context.ChunkZ * 16 + z;
                    var vegetationWarp = SimplexNoise.DomainWarp(globalX, globalZ, 0.0015, 0.0028, 6.0, 3.0, 9127);
                    double patchiness = NormalizeNoise(PerlinNoise.Generate(globalX + vegetationWarp.dx, globalZ + vegetationWarp.dz, 0.008, 2, 1.0, 0.5, 9127));
                    double density = GetVegetationDensity(biome) * (0.5 + patchiness * 0.5);

                    if (surfaceY > 0 && chunk.GetBlock(x, surfaceY, z) == BlockType.Grass)
                    {
                        if (rand.NextDouble() < density)
                        {
                            if (surfaceY + 1 < 256)
                            {
                                var vegType = GetVegetationType(biome, rand);
                                chunk.SetBlock(x, surfaceY + 1, z, vegType);
                                
                                if (vegType == BlockType.Wood && surfaceY + 5 < 256)
                                {
                                    for (int i = 1; i <= 4; i++)
                                        chunk.SetBlock(x, surfaceY + i, z, BlockType.Wood);
                                    
                                    for (int dx = -2; dx <= 2; dx++)
                                    {
                                        for (int dz = -2; dz <= 2; dz++)
                                        {
                                            if (x + dx >= 0 && x + dx < 16 && z + dz >= 0 && z + dz < 16)
                                            {
                                                for (int dy = 3; dy <= 5; dy++)
                                                {
                                                    if (surfaceY + dy < 256 && 
                                                        chunk.GetBlock(x + dx, surfaceY + dy, z + dz) == BlockType.Air)
                                                    {
                                                        chunk.SetBlock(x + dx, surfaceY + dy, z + dz, BlockType.Leaves);
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
            }
        }

        private int FindSurfaceLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                if (chunk.GetBlock(x, y, z) != BlockType.Air)
                    return y;
            }
            return -1;
        }

        private double GetVegetationDensity(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => 0.8,
                BiomeType.Plains => 0.3,
                BiomeType.Hills => 0.25,
                BiomeType.Mountains => 0.1,
                BiomeType.Desert => 0.05,
                BiomeType.Tundra => 0.12,
                BiomeType.Cliffs => 0.02,
                BiomeType.Beach => 0.03,
                BiomeType.Ocean => 0.0,
                _ => 0.2
            };
        }

        private BlockType GetVegetationType(BiomeType biome, Random rand)
        {
            return biome switch
            {
                BiomeType.Forest => rand.NextDouble() < 0.3 ? BlockType.Wood : BlockType.TallGrass,
                BiomeType.Desert => BlockType.DeadBush,
                BiomeType.Beach => BlockType.DeadBush,
                BiomeType.Cliffs => BlockType.DeadBush,
                _ => BlockType.TallGrass
            };
        }

        private sealed class BaseTerrainStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public BaseTerrainStage(WorldManager owner) => _owner = owner;
            public string Name => "base-terrain";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateBaseTerrainInternal(context);
        }

        private sealed class OreGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public OreGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "ores";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateOresInternal(context);
        }

        private sealed class CaveGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public CaveGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "caves";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateCavesInternal(context);
        }

        private sealed class DungeonGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public DungeonGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "dungeons";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateDungeonsInternal(context);
        }

        private sealed class RiverGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public RiverGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "rivers";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateRiversInternal(context);
        }

        private sealed class LakeGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public LakeGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "lakes";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateLakesInternal(context);
        }

        private sealed class VegetationGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public VegetationGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "vegetation";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateVegetationInternal(context);
        }

        private sealed class CloudGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public CloudGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "clouds";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateCloudsInternal(context);
        }

        private string GetChunkKey(int x, int z) => $"{x},{z}";
        
        private (int x, int z) ParseChunkKey(string key)
        {
            var parts = key.Split(',');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }

        // === 마인크래프트 핸들러용 추가 메서드들 ===

        /// <summary>
        /// 특정 블록 정보 가져오기
        /// </summary>
        public async Task<Models.BlockData?> GetBlockAsync(int x, int y, int z)
        {
            int chunkX = (int)Math.Floor(x / 16.0);
            int chunkZ = (int)Math.Floor(z / 16.0);
            int localX = x - chunkX * 16;
            int localZ = z - chunkZ * 16;

            var chunk = await GetChunkAsync(chunkX, chunkZ);
            if (chunk != null)
            {
                var blockType = chunk.GetBlock(localX, y, localZ);
                return new Models.BlockData(x, y, z, (int)blockType);
            }
            
            return null;
        }

        /// <summary>
        /// 블록 설정하기
        /// </summary>
        public async Task SetBlockAsync(Models.BlockData blockData)
        {
            int chunkX = (int)Math.Floor(blockData.X / 16.0);
            int chunkZ = (int)Math.Floor(blockData.Z / 16.0);
            int localX = blockData.X - chunkX * 16;
            int localZ = blockData.Z - chunkZ * 16;

            var chunk = await GetChunkAsync(chunkX, chunkZ);
            if (chunk != null)
            {
                chunk.SetBlock(localX, blockData.Y, localZ, (BlockType)blockData.BlockId);
                
                // 청크를 수정됨으로 표시
                var chunkKey = GetChunkKey(chunkX, chunkZ);
                if (_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
                {
                    loadedChunk.IsModified = true;
                }
            }
        }

        /// <summary>
        /// 블록 제거하기
        /// </summary>
        public async Task RemoveBlockAsync(int x, int y, int z)
        {
            var airBlock = new Models.BlockData(x, y, z, 0); // 0 = Air
            await SetBlockAsync(airBlock);
        }

        /// <summary>
        /// 청크 내 엔티티들 가져오기
        /// </summary>
        public async Task<List<Models.Entity>> GetEntitiesInChunk(int chunkX, int chunkZ)
        {
            // TODO: 실제 구현에서는 데이터베이스에서 엔티티 조회
            // 현재는 빈 리스트 반환
            return new List<Models.Entity>();
        }
    }

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

    public enum BlockType : ushort
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        Wood = 5,
        Leaves = 6,
        Sand = 7,
        Water = 8,
        Lava = 9,
        Bedrock = 10,
        CoalOre = 11,
        IronOre = 12,
        GoldOre = 13,
        DiamondOre = 14,
        TallGrass = 15,
        DeadBush = 16,
        Ice = 17,
        Snow = 18,
        Cloud = 19
    }

    public enum BiomeType : byte
    {
        Plains = 0,
        Forest = 1,
        Desert = 2,
        Tundra = 3,
        Ocean = 4,
        Mountains = 5,
        Hills = 6,
        Cliffs = 7,
        Beach = 8
    }

    public static class SimplexNoise
    {
        public static (double dx, double dz) DomainWarp(double x, double z, double simplexFrequency, double perlinFrequency, double simplexAmplitude, double perlinAmplitude, int seed)
        {
            double simplexOffsetX = Generate(x, z, simplexFrequency, 3, 1.0, 0.5, seed) * simplexAmplitude;
            double simplexOffsetZ = Generate(x + 37.0, z + 53.0, simplexFrequency, 3, 1.0, 0.5, seed ^ 0x5F5F5F5F) * simplexAmplitude;

            double perlinOffsetX = PerlinNoise.Generate(x, z, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x00FF00FF) * perlinAmplitude;
            double perlinOffsetZ = PerlinNoise.Generate(x + 17.0, z + 23.0, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x7F00EF00) * perlinAmplitude;

            return (simplexOffsetX + perlinOffsetX, simplexOffsetZ + perlinOffsetZ);
        }

        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            double total = 0;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateOctave(x * frequency, y * frequency, random) * amplitude;
                maxValue += amplitude;
                
                frequency *= 2;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        private static double GenerateOctave(double x, double y, Random random)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            
            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);
            
            double u = Fade(xf);
            double v = Fade(yf);
            
            var p = new int[512];
            for (int i = 0; i < 256; i++)
                p[i] = p[i + 256] = random.Next(256);
            
            int aa = p[p[xi] + yi];
            int ab = p[p[xi] + yi + 1];
            int ba = p[p[xi + 1] + yi];
            int bb = p[p[xi + 1] + yi + 1];
            
            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            
            return Lerp(x1, x2, v);
        }
        
        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);
        private static double Grad(int hash, double x, double y) => ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    public static class PerlinNoise
    {
        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            var permutation = BuildPermutation(random);

            double total = 0.0;
            double maxValue = 0.0;
            double currentFrequency = frequency;
            double currentAmplitude = amplitude;

            for (int i = 0; i < octaves; i++)
            {
                total += Perlin(permutation, x * currentFrequency, y * currentFrequency) * currentAmplitude;
                maxValue += currentAmplitude;
                currentFrequency *= 2.0;
                currentAmplitude *= persistence;
            }

            return maxValue == 0 ? 0 : total / maxValue;
        }

        private static double Perlin(int[] permutation, double x, double y)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);

            int aa = permutation[permutation[xi] + yi];
            int ab = permutation[permutation[xi] + yi + 1];
            int ba = permutation[permutation[xi + 1] + yi];
            int bb = permutation[permutation[xi + 1] + yi + 1];

            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        private static int[] BuildPermutation(Random random)
        {
            var baseArray = new int[256];
            for (int i = 0; i < 256; i++)
            {
                baseArray[i] = i;
            }

            for (int i = 255; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                var temp = baseArray[i];
                baseArray[i] = baseArray[swapIndex];
                baseArray[swapIndex] = temp;
            }

            var permutation = new int[512];
            for (int i = 0; i < 512; i++)
            {
                permutation[i] = baseArray[i & 255];
            }

            return permutation;
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 7;
            double u = h < 4 ? x : y;
            double v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
