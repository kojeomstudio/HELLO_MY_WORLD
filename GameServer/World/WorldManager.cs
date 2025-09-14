using System.Collections.Concurrent;
using GameServerApp.Database;
using GameServerApp.Models;

namespace GameServerApp.World
{
    public class WorldManager
    {
        private readonly DatabaseHelper _database;
        private readonly ConcurrentDictionary<string, LoadedChunk> _loadedChunks = new();
        private readonly Random _random;
        private int _worldId;
        
        public WorldManager(DatabaseHelper database, int worldId = 1)
        {
            _database = database;
            _worldId = worldId;
            _random = new Random();
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
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    var height = GenerateHeight(worldX, worldZ);
                    var biome = GenerateBiome(worldX, worldZ);
                    
                    chunk.SetBiome(x, z, biome);
                    
                    for (int y = 0; y <= height && y < 256; y++)
                    {
                        BlockType blockType;
                        
                        if (y == 0)
                            blockType = BlockType.Bedrock;
                        else if (y < height - 3)
                            blockType = BlockType.Stone;
                        else if (y < height)
                            blockType = BlockType.Dirt;
                        else if (y == height)
                            blockType = biome == BiomeType.Desert ? BlockType.Sand : BlockType.Grass;
                        else
                            blockType = BlockType.Air;
                        
                        chunk.SetBlock(x, y, z, blockType);
                    }
                    
                    for (int y = height + 1; y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            GenerateOres(chunk, chunkX, chunkZ);
            // Caves and dungeons are carved after base terrain and ores
            GenerateCaves(chunk, chunkX, chunkZ);
            GenerateDungeons(chunk, chunkX, chunkZ);
            GenerateVegetation(chunk, chunkX, chunkZ);
            
            return chunk;
        }

        /// <summary>
        /// 개선된 3D 동굴 생성 시스템 - 더 자연스럽고 다양한 동굴 구조
        /// </summary>
        private void GenerateCaves(ChunkData chunk, int chunkX, int chunkZ)
        {
            var rand = new Random((chunkX * 73856093) ^ (chunkZ * 19349663));
            
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
        private void GenerateDungeons(ChunkData chunk, int chunkX, int chunkZ)
        {
            var rand = new Random((chunkX * 83492791) ^ (chunkZ * 297657976));
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

            BuildDungeonRoom(chunk, ox, oy, oz, roomWidth, roomHeight, roomDepth);
            
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
                
                BuildDungeonRoom(chunk, ox, oy, oz, roomWidth, roomHeight, roomDepth);
                
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
        private void BuildDungeonRoom(ChunkData chunk, int ox, int oy, int oz, int width, int height, int depth)
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
                            BlockType wallMaterial = GetDungeonWallMaterial();
                            chunk.SetBlock(x, y, z, wallMaterial);
                        }
                    }
                }
            }

            // 입구 생성 (더 자연스럽게)
            CreateDungeonEntrance(chunk, ox, oy, oz, width, depth);
        }
        
        /// <summary>
        /// 던전 벽 재료 결정
        /// </summary>
        private BlockType GetDungeonWallMaterial()
        {
            var materials = new[] { BlockType.Cobblestone, BlockType.Stone, BlockType.Stone };
            var rand = new Random();
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
            var noise1 = SimplexNoise.Generate(worldX * 0.01, worldZ * 0.01, 0.5, 4, 1.0, 0.5, 12345);
            var noise2 = SimplexNoise.Generate(worldX * 0.005, worldZ * 0.005, 0.3, 6, 1.0, 0.4, 54321);
            
            var height = 64 + (int)(noise1 * 32 + noise2 * 16);
            return Math.Clamp(height, 5, 120);
        }

        private BiomeType GenerateBiome(int worldX, int worldZ)
        {
            var temperature = SimplexNoise.Generate(worldX * 0.003, worldZ * 0.003, 0.5, 3, 1.0, 0.5, 11111);
            var humidity = SimplexNoise.Generate(worldX * 0.004, worldZ * 0.004, 0.5, 3, 1.0, 0.5, 22222);
            
            if (temperature > 0.6 && humidity < 0.3)
                return BiomeType.Desert;
            else if (temperature < -0.3)
                return BiomeType.Tundra;
            else if (humidity > 0.4)
                return BiomeType.Forest;
            else
                return BiomeType.Plains;
        }

        /// <summary>
        /// 개선된 광물 생성 시스템 - 더 현실적이고 균형 잡힌 분배
        /// </summary>
        private void GenerateOres(ChunkData chunk, int chunkX, int chunkZ)
        {
            var rand = new Random(chunkX * 1000 + chunkZ);
            
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

        private void GenerateVegetation(ChunkData chunk, int chunkX, int chunkZ)
        {
            var rand = new Random(chunkX * 2000 + chunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var biome = chunk.GetBiome(x, z);
                    var surfaceY = FindSurfaceLevel(chunk, x, z);
                    
                    if (surfaceY > 0 && chunk.GetBlock(x, surfaceY, z) == BlockType.Grass)
                    {
                        if (rand.NextDouble() < GetVegetationDensity(biome))
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
                BiomeType.Desert => 0.05,
                BiomeType.Tundra => 0.1,
                _ => 0.2
            };
        }

        private BlockType GetVegetationType(BiomeType biome, Random rand)
        {
            return biome switch
            {
                BiomeType.Forest => rand.NextDouble() < 0.3 ? BlockType.Wood : BlockType.TallGrass,
                BiomeType.Desert => BlockType.DeadBush,
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
        Snow = 18
    }

    public enum BiomeType : byte
    {
        Plains = 0,
        Forest = 1,
        Desert = 2,
        Tundra = 3,
        Ocean = 4
    }

    public static class SimplexNoise
    {
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
}
