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
            GenerateVegetation(chunk, chunkX, chunkZ);
            
            return chunk;
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

        private void GenerateOres(ChunkData chunk, int chunkX, int chunkZ)
        {
            var rand = new Random(chunkX * 1000 + chunkZ);
            
            for (int i = 0; i < 8; i++)
            {
                var x = rand.Next(16);
                var y = rand.Next(1, 32);
                var z = rand.Next(16);
                
                if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                {
                    var oreType = rand.NextDouble() switch
                    {
                        < 0.05 => BlockType.DiamondOre,
                        < 0.15 => BlockType.GoldOre,
                        < 0.4 => BlockType.IronOre,
                        _ => BlockType.CoalOre
                    };
                    
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