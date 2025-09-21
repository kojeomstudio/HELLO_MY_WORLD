using System.Collections.Concurrent;
using GameServerApp.Database;
using GameServerApp.Models;
using GameServerApp.World.Generation;

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

        public WorldManager(DatabaseHelper database, int worldId = 1)
        {
            _database = database;
            _worldId = worldId;
            _random = new Random();
            _terrainPipeline = new TerrainGenerationPipeline()
                .AddStage("base-terrain", GenerateBaseTerrain)
                .AddStage("ores", GenerateOres)
                .AddStage("caves", GenerateCaves)
                .AddStage("dungeons", GenerateDungeons)
                .AddStage("rivers", GenerateRivers)
                .AddStage("lakes", GenerateLakes)
                .AddStage("vegetation", GenerateVegetation)
                .AddStage("clouds", GenerateClouds);
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

        private void GenerateBaseTerrain(TerrainGenerationContext context)
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
        private void GenerateCaves(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random((context.ChunkX * 73856093) ^ (context.ChunkZ * 19349663));
            
            // 메인 동굴 시스템 (기존 웜 방식 개선)
            GenerateMainCaveSystem(chunk, rand);
            
            // 소형 동굴방 추가
            GenerateSmallCaveRooms(chunk, rand);
            
            // 수직 동굴 (수직갱)
            GenerateVerticalShafts(chunk, rand);
        }
        
        /// <summary>
        /// 메인 동굴 시스템 생성
        /// </summary>
        private void GenerateMainCaveSystem(ChunkData chunk, Random rand)
        {
            int wormCount = 1 + rand.Next(3); // 1~3개의 메인 웜

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
                    double currentRadius = baseRadius + Math.Sin(s * 0.1) * 0.8;
                    
                    int cx = (int)Math.Round(x);
                    int cy = (int)Math.Round(y);
                    int cz = (int)Math.Round(z);
                    
                    // 동굴 조각하기
                    CarveSphere(chunk, cx, cy, cz, currentRadius);
                    
                    // 가끔 큰 공간(방) 생성
                    if (s > 20 && rand.NextDouble() < 0.05) // 5% 확률
                    {
                        CarveRoom(chunk, cx, cy, cz, 4 + rand.Next(4));
                    }

                    // 이동
                    double speed = 0.8 + rand.NextDouble() * 0.4; // 가변 속도
                    x += Math.Cos(yaw) * speed;
                    z += Math.Sin(yaw) * speed;
                    y += Math.Sin(pitch) * 0.3;

                    // 방향 변화 (더 자연스럽게)
                    yaw += (rand.NextDouble() - 0.5) * 0.3;
                    pitch += (rand.NextDouble() - 0.5) * 0.15;
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
        private void GenerateDungeons(TerrainGenerationContext context)
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

        private void GenerateRivers(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            TerrainProfile[,]? profiles = null;
            context.TryGetMetadata(TerrainProfilesKey, out profiles);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = context.ChunkX * 16 + x;
                    var worldZ = context.ChunkZ * 16 + z;

                    if (profiles != null)
                    {
                        var profile = profiles[x, z];
                        if (profile.Biome == BiomeType.Ocean)
                        {
                            continue;
                        }
                    }

                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.0008, 0.0016, 20.0, 12.0, 91111);
                    double sampleX = worldX + warp.dx;
                    double sampleZ = worldZ + warp.dz;

                    double riverNoise = SimplexNoise.Generate(sampleX, sampleZ, 0.0012, 5, 1.0, 0.45, 91111);
                    double intensity = Math.Abs(riverNoise);

                    if (intensity < RiverCenterThreshold)
                    {
                        if (!IsOceanColumn(chunk, x, z))
                        {
                            CarveRiverColumn(chunk, x, z, worldX, worldZ, intensity);
                        }
                    }
                    else if (intensity < RiverBankThreshold)
                    {
                        ShapeRiverBank(chunk, x, z);
                    }
                }
            }
        }

        private void GenerateLakes(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
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
            int radiusX = 3 + rand.Next(3);
            int radiusZ = 3 + rand.Next(3);
            int depth = 2 + rand.Next(2);
            int waterLevel = Math.Clamp(GlobalWaterLevel + rand.Next(-1, 2), 45, 80);

            int sampleSurface = FindSurfaceLevel(chunk, centerX, centerZ);
            if (sampleSurface < waterLevel - 3 || sampleSurface > waterLevel + 8)
                return;

            for (int x = Math.Max(0, centerX - radiusX - 1); x < Math.Min(16, centerX + radiusX + 2); x++)
            {
                for (int z = Math.Max(0, centerZ - radiusZ - 1); z < Math.Min(16, centerZ + radiusZ + 2); z++)
                {
                    double nx = (x - centerX) / (double)radiusX;
                    double nz = (z - centerZ) / (double)radiusZ;
                    double distance = nx * nx + nz * nz;

                    if (distance <= 1.0)
                    {
                        int localDepth = depth + (int)Math.Max(0, (1.0 - distance) * 2.0);
                        CarveLakeColumn(chunk, x, z, waterLevel, localDepth);
                    }
                    else if (distance <= 1.25)
                    {
                        DecorateLakeBank(chunk, x, z, waterLevel);
                    }
                }
            }
        }

        private void GenerateClouds(TerrainGenerationContext context)
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

        private void CarveRiverColumn(ChunkData chunk, int x, int z, int worldX, int worldZ, double intensity)
        {
            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            if (surface <= GlobalWaterLevel - 3 && chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            int waterTop = Math.Min(GlobalWaterLevel, surface);
            double depthFactor = Math.Clamp((RiverCenterThreshold - intensity) / RiverCenterThreshold, 0.0, 1.0);
            int bedDepth = Math.Clamp(2 + (int)Math.Round(depthFactor * 3), 2, 5);
            int bedBottom = Math.Max(1, waterTop - bedDepth);

            for (int y = surface; y > waterTop; y--)
            {
                chunk.SetBlock(x, y, z, BlockType.Air);
            }

            chunk.SetBlock(x, waterTop, z, BlockType.Water);

            if (waterTop - 1 >= bedBottom)
            {
                chunk.SetBlock(x, waterTop - 1, z, BlockType.Sand);
            }

            if (waterTop - 2 >= bedBottom)
            {
                for (int y = waterTop - 2; y >= bedBottom; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Stone);
                }
            }

            for (int y = waterTop + 1; y <= GlobalWaterLevel && y < 256; y++)
            {
                chunk.SetBlock(x, y, z, BlockType.Water);
            }

            for (int y = GlobalWaterLevel + 1; y <= surface + 2 && y < 256; y++)
            {
                if (chunk.GetBlock(x, y, z) != BlockType.Air)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
            }
        }

        private void ShapeRiverBank(ChunkData chunk, int x, int z)
        {
            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            if (chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            chunk.SetBlock(x, surface, z, BlockType.Sand);

            if (surface - 1 > 0 && chunk.GetBlock(x, surface - 1, z) == BlockType.Dirt)
            {
                chunk.SetBlock(x, surface - 1, z, BlockType.Sand);
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

        private void CarveLakeColumn(ChunkData chunk, int x, int z, int waterLevel, int depth)
        {
            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            int bottom = Math.Max(1, waterLevel - depth);

            for (int y = surface; y >= bottom; y--)
            {
                chunk.SetBlock(x, y, z, BlockType.Air);
            }

            chunk.SetBlock(x, bottom, z, BlockType.Stone);

            int sandLayer = Math.Min(waterLevel - 1, bottom + 1);
            if (sandLayer > bottom)
            {
                chunk.SetBlock(x, sandLayer, z, BlockType.Sand);
            }

            for (int y = sandLayer + 1; y <= waterLevel && y < 256; y++)
            {
                chunk.SetBlock(x, y, z, BlockType.Water);
            }

            for (int y = waterLevel + 1; y <= Math.Min(waterLevel + 3, 255); y++)
            {
                if (chunk.GetBlock(x, y, z) != BlockType.Air)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
            }
        }

        private void DecorateLakeBank(ChunkData chunk, int x, int z, int waterLevel)
        {
            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            if (chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            if (surface <= waterLevel + 2)
            {
                chunk.SetBlock(x, surface, z, BlockType.Sand);
                if (surface + 1 < 256)
                {
                    chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                }
            }
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
        private void GenerateOres(TerrainGenerationContext context)
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

        private void GenerateVegetation(TerrainGenerationContext context)
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
