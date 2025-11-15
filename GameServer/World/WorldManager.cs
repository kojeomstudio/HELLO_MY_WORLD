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
        private readonly WorldSeedConfig _worldSeed;
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

        public WorldManager(DatabaseHelper database, int worldId = 1, WorldSeedConfig? worldSeed = null)
        {
            _database = database;
            _worldId = worldId;

            // 월드 시드 초기화: 제공된 시드 또는 데이터베이스에서 로드, 또는 새로 생성
            _worldSeed = worldSeed ?? LoadWorldSeedFromDatabase() ?? WorldSeedConfig.Random();
            SaveWorldSeedToDatabase();

            // 시드를 사용하여 Random 초기화 (결정적 생성을 위함)
            _random = new Random(_worldSeed.Seed);

            Console.WriteLine($"[WorldManager] {_worldSeed}");

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

        // ==================== World Seed Management ====================

        private WorldSeedConfig? LoadWorldSeedFromDatabase() => null;

        private void SaveWorldSeedToDatabase()
        {
            // TODO: wire up DatabaseHelper persistence when raw query helpers are available.
        }

        public Random GetChunkRandom(int chunkX, int chunkZ)
        {
            int chunkSeed = _worldSeed.GetChunkSeed(chunkX, chunkZ);
            return new Random(chunkSeed);
        }

        public WorldSeedConfig GetWorldSeed() => _worldSeed;

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

        private static double SampleField(double[,] field, double x, double z)
        {
            if (field == null)
            {
                return 0.0;
            }

            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            double clampedX = Math.Clamp(x, 0.0, width - 1);
            double clampedZ = Math.Clamp(z, 0.0, depth - 1);

            int x0 = (int)Math.Floor(clampedX);
            int z0 = (int)Math.Floor(clampedZ);
            int x1 = Math.Min(x0 + 1, width - 1);
            int z1 = Math.Min(z0 + 1, depth - 1);
            double tx = clampedX - x0;
            double tz = clampedZ - z0;

            double v00 = field[x0, z0];
            double v10 = field[x1, z0];
            double v01 = field[x0, z1];
            double v11 = field[x1, z1];

            double vx0 = v00 + (v10 - v00) * tx;
            double vx1 = v01 + (v11 - v01) * tx;
            return vx0 + (vx1 - vx0) * tz;
        }

        private double SampleRidgedNoise(double worldX, double worldZ, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var noise = SimplexNoise.Generate(worldX, worldZ, frequency, octaves, amplitude, persistence, seed);
            noise = Math.Clamp(noise, -1.0, 1.0);
            return 1.0 - Math.Abs(noise);
        }

        private static double SampleDeterministicNoise(int x, int z, int salt)
        {
            unchecked
            {
                int hash = x * 73428767 ^ z * 91228541 ^ salt * 19997;
                hash ^= (hash << 13);
                hash ^= (hash >> 9);
                hash = hash * 60493 + 19990303;
                hash ^= (hash << 11);
                return (hash & int.MaxValue) / (double)int.MaxValue;
            }
        }

        /// <summary>
        /// 개선된 3D 동굴 생성 시스템 - 더 자연스럽고 다양한 동굴 구조
        /// </summary>
        private void GenerateCavesInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = new Random((context.ChunkX * 73856093) ^ (context.ChunkZ * 19349663));
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyMask = BuildHydrologyMask(context.ChunkX, context.ChunkZ, surfaceCache);
            var flowAccumulation = BuildFlowAccumulation(surfaceCache);
            var riverField = GetRiverFieldCache(context);
            
            // 메인 동굴 시스템 (기존 웜 방식 개선)
            var caveStabilityField = BuildCaveStabilityField(context, surfaceCache, hydrologyMask, flowAccumulation);

            GenerateMainCaveSystem(chunk, rand, caveStabilityField);

            // 소형 동굴방 추가
            GenerateSmallCaveRooms(chunk, rand);

            // 수직 동굴 (수직갱)
            GenerateVerticalShafts(chunk, rand);

            // 노이즈 기반 동굴층 추가 (연속성 보장)
            GenerateNoiseCavePass(context, chunk, surfaceCache, caveStabilityField);
            ApplyCaveHydrologyFeatures(context, chunk, surfaceCache, hydrologyMask);
            IntegrateKarstInlets(context, chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField);
            AddCaveColumnSupports(chunk, surfaceCache, caveStabilityField, rand);
            AddCaveShelfBands(chunk, surfaceCache, caveStabilityField, hydrologyMask);
            AddCaveDripstoneFeatures(context, chunk);
            AddCaveVentShafts(chunk, surfaceCache, hydrologyMask, caveStabilityField);
            AddCaveAquiferChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField);
            AddCaveRibbonTerraces(chunk, surfaceCache, hydrologyMask, caveStabilityField, flowAccumulation);
            ApplyCaveHydrologyErosion(chunk, surfaceCache, hydrologyMask, flowAccumulation);
        }
        
        private double[,] BuildCaveStabilityField(
            TerrainGenerationContext context,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation)
        {
            var stability = new double[16, 16];
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            double surfaceRange = Math.Max(1, MaxSurfaceHeight - MinSurfaceHeight);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        stability[x, z] = 0.0;
                        continue;
                    }

                    double depthFactor = Math.Clamp((surface - MinSurfaceHeight) / surfaceRange, 0.0, 1.0);
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double roughness = NormalizeNoise(SimplexNoise.Generate(originX + x * 0.85, originZ + z * 0.85, 0.012, 3, 1.0, 0.6, 91517));
                    double warp = NormalizeNoise(PerlinNoise.Generate(originX + x * 0.5, originZ + z * 0.5, 0.018, 2, 1.0, 0.55, 52301));

                    double saturation = hydrology * 0.45 + flow * 0.25 + (1.0 - depthFactor) * 0.2 + (roughness + warp) * 0.1;
                    stability[x, z] = Math.Clamp(saturation, 0.0, 1.0);
                }
            }

            return stability;
        }

        private void AddCaveColumnSupports(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] caveStabilityField,
            Random rand)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double stability = caveStabilityField[x, z];
                    if (stability < 0.58)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 12)
                    {
                        continue;
                    }

                    int top = -1;
                    int bottom = -1;
                    bool insideAir = false;
                    int scanStart = Math.Min(surface - 2, 140);

                    for (int y = scanStart; y >= 8; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
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

                    int supportSpan = Math.Clamp((int)Math.Round(cavityHeight * (0.3 + stability * 0.4)), 3, cavityHeight - 1);
                    int baseOffset = rand.Next(0, Math.Max(1, cavityHeight - supportSpan));
                    int columnBase = bottom + baseOffset;
                    int columnTop = Math.Min(top - 1, columnBase + supportSpan);
                    int radius = stability > 0.82 ? 2 : 1;

                    for (int y = columnBase; y <= columnTop; y++)
                    {
                        if ((y - columnBase) % 2 == 0 || y == columnTop)
                        {
                            PlaceSupportNode(chunk, x, y, z, radius);
                        }
                    }
                }
            }
        }

        private void AddCaveShelfBands(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] stabilityField,
            double[,] hydrologyMask)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double stability = stabilityField[x, z];
                    if (stability < 0.42)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double wetness = Math.Clamp(hydrology * 0.7 + stability * 0.25, 0.0, 1.0);
                    int shelfThickness = Math.Clamp((int)Math.Round(1 + wetness * 3.0), 1, 4);
                    int shelfOffset = Math.Clamp((int)Math.Round(cavityHeight * (0.35 + wetness * 0.2)), 2, cavityHeight - 2);
                    int shelfY = Math.Clamp(bottom + shelfOffset, bottom + 2, top - 2);
                    int radius = wetness > 0.62 ? 2 : 1;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                            {
                                continue;
                            }

                            double falloff = 1.0 - (Math.Abs(dx) + Math.Abs(dz)) * 0.35;
                            if (falloff <= 0.0)
                            {
                                continue;
                            }

                            for (int y = shelfY; y > shelfY - shelfThickness && y > bottom + 1; y--)
                            {
                                var block = chunk.GetBlock(nx, y, nz);
                                if (block == BlockType.Air || block == BlockType.Water)
                                {
                                    chunk.SetBlock(nx, y, nz, wetness > 0.6 ? BlockType.Cobblestone : BlockType.Stone);
                                }
                            }

                            if (shelfY + 1 < 256)
                            {
                                var above = chunk.GetBlock(nx, shelfY + 1, nz);
                                if (above != BlockType.Air)
                                {
                                    chunk.SetBlock(nx, shelfY + 1, nz, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddCaveVentShafts(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] stabilityField)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 6)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surface, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (surface - top < 4)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double instability = 1.0 - Math.Clamp(stabilityField[x, z], 0.0, 1.0);
                    if (instability < 0.08)
                    {
                        continue;
                    }

                    double spawnWeight = Math.Clamp(hydrology * 0.55 + instability * 0.6, 0.0, 1.0);
                    double selector = SampleDeterministicNoise(x, z, 173);
                    if (spawnWeight < 0.35 || selector > spawnWeight)
                    {
                        continue;
                    }

                    int ventTop = Math.Min(surface - 1, 254);
                    int ventBottom = Math.Clamp(top, bottom + 1, ventTop - 1);

                    for (int y = ventTop; y >= ventBottom; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = Math.Max(ventBottom - 1, 1);

                    if (hydrology > 0.68)
                    {
                        int poolY = Math.Max(ventBottom - 1, 1);
                        chunk.SetBlock(x, poolY, z, BlockType.Water);
                    }

                    HardenVentLip(chunk, surfaceCache, x, z);
                }
            }
        }

        private void HardenVentLip(ChunkData chunk, int[,] surfaceCache, int centerX, int centerZ)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    if (chunk.GetBlock(x, surface, z) == BlockType.Air)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Cobblestone);
                    }
                }
            }
        }

        private void AddCaveAquiferChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    if (hydrology < 0.7 && catchment < 0.35)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (top - bottom < 6)
                    {
                        continue;
                    }

                    double pressure = Math.Clamp((hydrology - 0.65) * 1.4 + catchment * 0.85, 0.0, 1.0);
                    int channelY = bottom + Math.Max(2, (int)Math.Round((top - bottom) * (0.25 + pressure * 0.35)));
                    if (surfaceCache[x, z] - channelY < 6)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    if (slopeDir.LengthSquared() > 1e-4f)
                    {
                        flowDir = Vector2.Normalize(flowDir * 0.55f + slopeDir * 0.45f);
                    }

                    if (flowDir.LengthSquared() < 1e-4f)
                    {
                        flowDir = new Vector2(1, 0);
                    }

                    int steps = Math.Clamp((int)Math.Round(3 + pressure * 5 + catchment * 4), 3, 9);
                    int radius = pressure > 0.7 ? 2 : 1;
                    bool floodChannel = hydrology > 0.8 || catchment > 0.6;

                    int cx = x;
                    int cz = z;
                    for (int step = 0; step < steps; step++)
                    {
                        if (cx < 1 || cx >= 15 || cz < 1 || cz >= 15)
                        {
                            break;
                        }

                        CarveAquiferChannelNode(chunk, cx, channelY, cz, radius, floodChannel);
                        var delta = GetAquiferStep(flowDir, step);
                        cx += delta.dx;
                        cz += delta.dz;
                    }
                }
            }
        }

        private void AddCaveRibbonTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] caveStabilityField,
            double[,] flowAccumulation)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double stability = Math.Clamp(caveStabilityField[x, z], 0.0, 1.0);
                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double ribbonWeight = hydrology * 0.6 + catchment * 0.25 + (1.0 - Math.Abs(stability - 0.55)) * 0.35;
                    if (ribbonWeight < 0.55)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 7)
                    {
                        continue;
                    }

                    int ribbonY = bottom + Math.Clamp((int)Math.Round(cavityHeight * (0.3 + hydrology * 0.35)), 2, cavityHeight - 2);
                    int ribbonThickness = Math.Clamp((int)Math.Round(1 + ribbonWeight * 2), 1, 3);

                    var tangent = ComputeHydrologyTangent(hydrologyMask, x, z);
                    foreach (var (dx, dz) in BuildRibbonOffsets(tangent, ribbonWeight, hydrology))
                    {
                        int worldX = x + dx;
                        int worldZ = z + dz;
                        if (worldX < 1 || worldX >= 15 || worldZ < 1 || worldZ >= 15)
                        {
                            continue;
                        }

                        if (!TryResolveSurface(chunk, surfaceCache, worldX, worldZ, out int columnSurface))
                        {
                            continue;
                        }

                        if (columnSurface - ribbonY < 3)
                        {
                            continue;
                        }

                        int floorY = Math.Max(bottom + 1, ribbonY - ribbonThickness);
                        for (int y = ribbonY; y >= floorY && y > bottom + 1; y--)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }

                        int supportY = Math.Max(floorY - 1, 1);
                        var supportMaterial = stability > 0.72 ? BlockType.Cobblestone : BlockType.Stone;
                        chunk.SetBlock(worldX, supportY, worldZ, supportMaterial);

                        var walkwayMaterial = hydrology > 0.78 ? BlockType.Clay : BlockType.Cobblestone;
                        chunk.SetBlock(worldX, floorY, worldZ, walkwayMaterial);

                        int clearanceTop = Math.Min(floorY + 2, 254);
                        for (int y = floorY + 1; y <= clearanceTop; y++)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }
                    }
                }
            }
        }

        private void ApplyCaveHydrologyErosion(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation)
        {
            int[,] neighborOffsets =
            {
                { 1, 0 },
                { -1, 0 },
                { 0, 1 },
                { 0, -1 }
            };

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double moisture = Math.Max(hydrology, flow);
                    if (moisture < 0.55)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 5)
                    {
                        continue;
                    }

                    int streamHeight = Math.Clamp((int)Math.Round(cavityHeight * (0.25 + flow * 0.35)), 2, cavityHeight - 1);
                    int streamTop = Math.Min(top - 1, bottom + streamHeight);
                    int thickness = Math.Clamp(
                        (int)Math.Round(1 + (moisture - 0.45) * 6.0),
                        1,
                        Math.Max(2, streamHeight));
                    int streamBottom = Math.Max(bottom + 1, streamTop - thickness);
                    bool fillWithWater = hydrology > 0.68;
                    BlockType fillBlock = fillWithWater ? BlockType.Water : BlockType.Air;
                    BlockType floorMaterial = fillWithWater ? BlockType.Clay : BlockType.Cobblestone;
                    int floor = Math.Max(streamBottom - 1, 1);

                    for (int y = streamBottom; y <= streamTop && y < 255; y++)
                    {
                        chunk.SetBlock(x, y, z, fillBlock);
                    }

                    chunk.SetBlock(x, floor, z, floorMaterial);

                    for (int i = 0; i < neighborOffsets.GetLength(0); i++)
                    {
                        int nx = x + neighborOffsets[i, 0];
                        int nz = z + neighborOffsets[i, 1];
                        if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                        {
                            continue;
                        }

                        double cue = SampleDeterministicNoise(nx + floor, nz + floor, 9731);
                        if (cue < moisture * 0.45)
                        {
                            continue;
                        }

                        int linkBottom = streamBottom;
                        int linkTop = Math.Min(streamTop, linkBottom + 2 + (int)Math.Round(flow * 3.0));
                        for (int y = linkBottom; y <= linkTop && y < 255; y++)
                        {
                            chunk.SetBlock(nx, y, nz, fillBlock);
                        }

                        int guardY = Math.Min(linkTop + 1, 254);
                        if (chunk.GetBlock(nx, guardY, nz) != BlockType.Air)
                        {
                            chunk.SetBlock(nx, guardY, nz, BlockType.Air);
                        }
                    }

                    ExtendCaveHydrologyRunoff(
                        chunk,
                        surfaceCache,
                        hydrologyMask,
                        flowAccumulation,
                        x,
                        z,
                        streamBottom,
                        streamTop,
                        fillBlock);
                }
            }
        }

        private void ExtendCaveHydrologyRunoff(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            int originX,
            int originZ,
            int streamBottom,
            int streamTop,
            BlockType fillBlock)
        {
            var gradient = ComputeHydrologyGradientVector(hydrologyMask, originX, originZ);
            if (gradient.LengthSquared() < 1e-5f)
            {
                return;
            }

            double baseHydrology = Math.Clamp(hydrologyMask[originX, originZ], 0.0, 1.0);
            double baseFlow = Math.Clamp(flowAccumulation[originX, originZ] / 6.0, 0.0, 1.0);
            double pressure = Math.Max(baseHydrology, baseFlow);
            int steps = Math.Clamp((int)Math.Round(1 + pressure * 3.0), 1, 4);

            double cursorX = originX;
            double cursorZ = originZ;
            for (int step = 0; step < steps; step++)
            {
                cursorX += gradient.X;
                cursorZ += gradient.Y;
                int targetX = (int)Math.Round(cursorX);
                int targetZ = (int)Math.Round(cursorZ);
                if (targetX < 1 || targetX >= 15 || targetZ < 1 || targetZ >= 15)
                {
                    break;
                }

                if (!TryResolveSurface(chunk, surfaceCache, targetX, targetZ, out int surface))
                {
                    continue;
                }

                if (!TryFindCaveSpan(chunk, surface, targetX, targetZ, out int top, out int bottom))
                {
                    continue;
                }

                int cavityHeight = top - bottom;
                if (cavityHeight < 4)
                {
                    continue;
                }

                double neighborHydrology = Math.Clamp(hydrologyMask[targetX, targetZ], 0.0, 1.0);
                double neighborFlow = Math.Clamp(flowAccumulation[targetX, targetZ] / 6.0, 0.0, 1.0);
                double neighborMoisture = Math.Max(neighborHydrology, neighborFlow);
                if (neighborMoisture < 0.45)
                {
                    continue;
                }

                int localThickness = Math.Clamp(
                    (int)Math.Round(1 + neighborMoisture * 3.0 + pressure),
                    1,
                    Math.Max(2, cavityHeight - 1));
                int localTop = Math.Min(top - 1, bottom + localThickness + 1);
                int localBottom = Math.Max(bottom + 1, localTop - localThickness);
                int floor = Math.Max(localBottom - 1, 1);
                BlockType floorMaterial = fillBlock == BlockType.Water ? BlockType.Clay : BlockType.Cobblestone;

                for (int y = localBottom; y <= localTop && y < 255; y++)
                {
                    chunk.SetBlock(targetX, y, targetZ, fillBlock);
                }

                chunk.SetBlock(targetX, floor, targetZ, floorMaterial);

                if (fillBlock == BlockType.Water)
                {
                    int cap = Math.Min(Math.Max(streamTop, localTop + 1), 254);
                    for (int y = localTop + 1; y <= cap; y++)
                    {
                        if (chunk.GetBlock(targetX, y, targetZ) != BlockType.Air)
                        {
                            chunk.SetBlock(targetX, y, targetZ, BlockType.Air);
                        }
                    }
                }
            }
        }

        private static Vector2 ComputeHydrologyGradientVector(double[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;
            double gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            double gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
            var gradient = new Vector2((float)gx, (float)gz);
            return gradient.LengthSquared() < 1e-5f ? Vector2.Zero : Vector2.Normalize(gradient);
        }

        private static Vector2 ComputeHydrologyTangent(double[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;

            double gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            double gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];

            var gradient = new Vector2((float)gx, (float)gz);
            if (gradient.LengthSquared() < 1e-5f)
            {
                return Vector2.UnitX;
            }

            var tangent = new Vector2(-gradient.Y, gradient.X);
            return tangent.LengthSquared() < 1e-5f ? Vector2.UnitX : Vector2.Normalize(tangent);
        }

        private static IReadOnlyCollection<(int dx, int dz)> BuildRibbonOffsets(Vector2 tangent, double ribbonWeight, double hydrology)
        {
            var offsets = new HashSet<(int dx, int dz)> { (0, 0) };
            var perpendicular = new Vector2(-tangent.Y, tangent.X);
            if (perpendicular.LengthSquared() < 1e-5f)
            {
                perpendicular = Vector2.UnitY;
            }

            int steps = ribbonWeight > 0.85 ? 3 : 2;
            int halfWidth = hydrology > 0.75 ? 1 : 0;

            for (int step = -steps; step <= steps; step++)
            {
                int baseDx = (int)Math.Round(tangent.X * step);
                int baseDz = (int)Math.Round(tangent.Y * step);

                for (int lateral = -halfWidth; lateral <= halfWidth; lateral++)
                {
                    int offsetX = baseDx + (int)Math.Round(perpendicular.X * lateral);
                    int offsetZ = baseDz + (int)Math.Round(perpendicular.Y * lateral);
                    offsets.Add((offsetX, offsetZ));
                }
            }

            return offsets;
        }

        private bool TryResolveSurface(ChunkData chunk, int[,] surfaceCache, int x, int z, out int surface)
        {
            surface = 0;
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
            {
                return false;
            }

            surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                {
                    return false;
                }
                surfaceCache[x, z] = surface;
            }

            return true;
        }

        private static (int dx, int dz) GetAquiferStep(Vector2 direction, int stepIndex)
        {
            int dx = 0;
            int dz = 0;
            if (direction.X > 0.35f)
            {
                dx = 1;
            }
            else if (direction.X < -0.35f)
            {
                dx = -1;
            }

            if (direction.Y > 0.35f)
            {
                dz = 1;
            }
            else if (direction.Y < -0.35f)
            {
                dz = -1;
            }

            if (dx == 0 && dz == 0)
            {
                if (Math.Abs(direction.X) >= Math.Abs(direction.Y))
                {
                    dx = direction.X >= 0 ? 1 : -1;
                }
                else
                {
                    dz = direction.Y >= 0 ? 1 : -1;
                }
            }

            if (stepIndex % 3 == 2 && Math.Abs(direction.X) > 0.15f && Math.Abs(direction.Y) > 0.15f)
            {
                dx = Math.Clamp(dx + (direction.X >= 0 ? 1 : -1), -1, 1);
                dz = Math.Clamp(dz + (direction.Y >= 0 ? 1 : -1), -1, 1);
            }

            return (dx, dz);
        }

        private void CarveAquiferChannelNode(ChunkData chunk, int centerX, int centerY, int centerZ, int radius, bool floodChannel)
        {
            int floor = Math.Max(2, centerY - 2);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                    {
                        continue;
                    }

                    double falloff = 1.0 - (Math.Abs(dx) + Math.Abs(dz)) * (radius == 1 ? 0.6 : 0.45);
                    if (falloff <= 0.0)
                    {
                        continue;
                    }

                    int roof = Math.Min(254, centerY + (radius > 1 ? 2 : 1));
                    for (int y = roof; y >= floor; y--)
                    {
                        chunk.SetBlock(nx, y, nz, BlockType.Air);
                    }

                    int floorBlock = Math.Max(1, floor - 1);
                    chunk.SetBlock(nx, floorBlock, nz, floodChannel ? BlockType.Clay : BlockType.Cobblestone);

                    if (floodChannel)
                    {
                        for (int y = floor; y <= centerY && y < 255; y++)
                        {
                            chunk.SetBlock(nx, y, nz, BlockType.Water);
                        }
                    }
                }
            }
        }

        private static bool TryFindCaveSpan(ChunkData chunk, int surface, int x, int z, out int top, out int bottom)
        {
            top = -1;
            bottom = -1;

            if (surface <= 0)
            {
                return false;
            }

            int scanStart = Math.Clamp(surface - 2, 8, 220);
            bool insideAir = false;
            for (int y = scanStart; y >= 6; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                bool isAir = block == BlockType.Air || block == BlockType.Water;
                if (isAir)
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

        private void PlaceSupportNode(ChunkData chunk, int x, int y, int z, int radius)
        {
            if (y < 1 || y >= 255 || x <= 0 || x >= 15 || z <= 0 || z >= 15)
            {
                return;
            }

            chunk.SetBlock(x, y, z, BlockType.Cobblestone);

            if (radius <= 1)
            {
                return;
            }

            var offsets = new (int dx, int dz)[]
            {
                (1, 0), (-1, 0), (0, 1), (0, -1)
            };

            foreach (var (dx, dz) in offsets)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                {
                    continue;
                }

                var block = chunk.GetBlock(nx, y, nz);
                if (block == BlockType.Air)
                {
                    chunk.SetBlock(nx, y, nz, BlockType.Stone);
                }
            }

            if (y - 1 >= 1 && chunk.GetBlock(x, y - 1, z) == BlockType.Air)
            {
                chunk.SetBlock(x, y - 1, z, BlockType.Stone);
            }
        }

        /// <summary>
        /// 메인 동굴 시스템 생성
        /// </summary>
        private void GenerateMainCaveSystem(ChunkData chunk, Random rand, double[,] caveStabilityField)
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
                    double stability = SampleField(caveStabilityField, x, z);
                    double radiusNoise = SimplexNoise.Generate(x + radiusNoiseSeed, z + radiusNoiseSeed, 0.12, 2, 1.0, 0.55, 55127);
                    double radiusTurbulence = Math.Sin(s * 0.1) * 0.8 + radiusNoise * 0.6;
                    double pressureBias = 0.65 + (1.0 - stability) * 0.45;
                    double currentRadius = (baseRadius + radiusTurbulence) * pressureBias;
                    if (stability > 0.75)
                    {
                        currentRadius *= 1.05 + stability * 0.2;
                    }
                    currentRadius = Math.Clamp(currentRadius, 1.6, baseRadius + 2.1);
                    
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
                    double turnBias = 0.7 - stability * 0.35;
                    yaw += (rand.NextDouble() - 0.5) * 0.3 * turnBias + directionalNoise * 0.35;
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
        private void GenerateNoiseCavePass(TerrainGenerationContext context, ChunkData chunk, int[,] surfaceCache, double[,] caveStabilityField)
        {
            int baseX = context.ChunkX * 16;
            int baseZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                int worldX = baseX + x;
                for (int z = 0; z < 16; z++)
                {
                    int worldZ = baseZ + z;

                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.00095, 0.0015, 22.0, 14.0, 53117);
                    double warpedX = worldX + warp.dx;
                    double warpedZ = worldZ + warp.dz;

                    double horizontalNoise = SimplexNoise.Generate(warpedX, warpedZ, NoiseCaveHorizontalFrequency, 4, 1.0, 0.55, 640371);
                    double secondaryNoise = SimplexNoise.Generate(warpedX * 1.35, warpedZ * 1.35, NoiseCaveHorizontalFrequency * 1.6, 2, 1.0, 0.5, 93217);
                    double ridged = SampleRidgedNoise(warpedX * 0.85, warpedZ * 0.85, NoiseCaveHorizontalFrequency * 1.25, 3, 1.0, 0.5, 91357);
                    double striation = SimplexNoise.Generate(warpedX * 0.9, warpedZ * 0.9, NoiseCaveHorizontalFrequency * 1.1, 2, 1.0, 0.55, 128713) - 0.5;
                    double flowNoise = SimplexNoise.Generate(warpedX * 0.25 + 37.1, warpedZ * 0.25 - 11.4, NoiseCaveHorizontalFrequency * 0.4, 2, 1.0, 0.6, 87121) - 0.5;

                    for (int y = 8; y < 120; y++)
                    {
                        double verticalNoise = SimplexNoise.Generate(warpedX, y, NoiseCaveVerticalFrequency, 3, 1.0, 0.62, 128947);
                        double density = Math.Abs(horizontalNoise) * 0.5 +
                                         Math.Abs(verticalNoise) * 0.35 +
                                         Math.Abs(secondaryNoise) * 0.2;
                        density = density * (0.65 + ridged * 0.35);
                        density -= Math.Clamp(striation, -0.35, 0.35) * 0.18;
                        density += flowNoise * 0.15;

                        double strata = Math.Sin((warpedX + warpedZ + y * 1.5) * 0.012);
                        density -= Math.Clamp(strata * 0.08, -0.08, 0.08);

                        density -= Math.Clamp((y - 24) / 140.0, 0.0, 0.45);

                        int cachedSurface = surfaceCache[x, z];
                        if (cachedSurface > 0)
                        {
                            double ceilingDepth = Math.Clamp((cachedSurface - y) / 48.0, 0.0, 1.0);
                            density += ceilingDepth * 0.08;
                        }

                        double aquifer = SimplexNoise.Generate(worldX, y, 0.0042, 2, 1.0, 0.58, 147113);
                    double liquidity = Math.Clamp((GlobalWaterLevel - y) / 28.0, 0.0, 1.0);
                    double flowBias = Math.Clamp((flowNoise + 0.5) * 0.5 + liquidity * 0.5, 0.0, 1.0);
                    double dynamicThreshold = NoiseCaveThreshold - liquidity * 0.08 + aquifer * 0.02 - flowBias * 0.015;
                    double stability = SampleField(caveStabilityField, x, z);
                    dynamicThreshold -= (stability - 0.5) * 0.08;

                    if (density < dynamicThreshold)
                    {
                            var block = chunk.GetBlock(x, y, z);
                            if (block == BlockType.Air || block == BlockType.Water || block == BlockType.Lava)
                            {
                                continue;
                            }

                            if (density < Math.Min(NoiseCaveLavaThreshold, dynamicThreshold * 0.55) && y < 18)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Lava);
                            }
                            else if (density < NoiseCaveWaterThreshold + liquidity * 0.05 && y < GlobalWaterLevel - 6)
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

        private void ApplyCaveHydrologyFeatures(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask)
        {
            var rand = new Random((context.ChunkX * 15731) ^ (context.ChunkZ * 31337) ^ 0xCAF3);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 12)
                    {
                        continue;
                    }

                    int y = Math.Min(surface - 4, 110);
                    while (y > 8)
                    {
                        while (y > 8 && chunk.GetBlock(x, y, z) != BlockType.Air)
                        {
                            y--;
                        }

                        if (y <= 8)
                        {
                            break;
                        }

                        int cavityTop = y;
                        while (y > 6 && chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            y--;
                        }

                        int cavityBottom = y + 1;
                        int cavityHeight = cavityTop - cavityBottom + 1;
                        if (cavityHeight < 4)
                        {
                            continue;
                        }

                        double poolChance = Math.Clamp((hydrology - 0.45) * 1.4, 0.0, 1.0);
                        if (poolChance <= 0.0 || rand.NextDouble() > poolChance)
                        {
                            continue;
                        }

                        int poolDepth = Math.Clamp((int)Math.Round(1 + hydrology * 4), 2, Math.Min(6, cavityHeight - 1));
                        int sedimentY = Math.Max(cavityBottom, cavityTop - poolDepth);
                        chunk.SetBlock(x, sedimentY, z, BlockType.Sand);

                        int waterStart = Math.Max(sedimentY + 1, cavityBottom);
                        int waterEnd = Math.Min(cavityTop - 1, sedimentY + poolDepth - 1);
                        for (int fillY = waterStart; fillY <= waterEnd; fillY++)
                        {
                            var fillBlock = fillY < GlobalWaterLevel - 4 ? BlockType.Water : BlockType.Air;
                            chunk.SetBlock(x, fillY, z, fillBlock);
                        }

                        if (sedimentY - 1 >= 1 && rand.NextDouble() < 0.35)
                        {
                            chunk.SetBlock(x, sedimentY - 1, z, BlockType.Cobblestone);
                        }

                        break;
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

        private void IntegrateKarstInlets(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField)
        {
            var rand = new Random((context.ChunkX * 48611) ^ (context.ChunkZ * 27361) ^ 0x51AE);
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.62)
                    {
                        continue;
                    }

                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double riverIntensity = riverField.Intensity[x, z];
                    double riverAffinity = 1.0 - Math.Clamp(riverIntensity / (RiverBankThreshold * 1.15), 0.0, 1.0);
                    double weight = hydrology * 0.5 + catchment * 0.35 + riverAffinity * 0.25;
                    if (weight < 0.65 || rand.NextDouble() > weight * 0.4)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface < 18)
                    {
                        continue;
                    }

                    int shaftTop = Math.Max(8, surface - rand.Next(2, 5));
                    int shaftDepth = Math.Clamp((int)Math.Round(4 + weight * 6 + rand.NextDouble() * 2), 3, 12);
                    int shaftBottom = Math.Max(6, shaftTop - shaftDepth);
                    int radius = weight > 1.05 ? 2 : 1;

                    CarveKarstColumn(chunk, x, z, shaftTop, shaftBottom, radius);

                    if (shaftBottom + 2 < GlobalWaterLevel - 4 && rand.NextDouble() < hydrology)
                    {
                        FillKarstPool(chunk, x, z, shaftBottom, Math.Clamp((int)Math.Round(1 + weight * 3), 2, 5));
                    }

                    var slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    Vector2 direction = slopeDir;
                    if (riverField.Flow[x, z].LengthSquared() > 1e-4f)
                    {
                        direction = Vector2.Normalize(riverField.Flow[x, z]);
                    }
                    else if (direction.LengthSquared() > 1e-4f)
                    {
                        direction = Vector2.Normalize(direction);
                    }
                    else
                    {
                        continue;
                    }

                    CreateKarstTunnel(chunk, x, z, Math.Max(shaftBottom + 1, 6), direction, weight, rand);
                }
            }
        }

        private void CarveKarstColumn(ChunkData chunk, int centerX, int centerZ, int topY, int bottomY, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if ((dx * dx) + (dz * dz) > radius * radius + (radius == 1 ? 0 : 1))
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    for (int y = topY; y >= bottomY && y > 0; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        private void FillKarstPool(ChunkData chunk, int centerX, int centerZ, int baseY, int depth)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    chunk.SetBlock(x, baseY, z, BlockType.Sand);
                    for (int y = baseY + 1; y <= baseY + depth && y < GlobalWaterLevel - 2; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }
                }
            }
        }

        private void CreateKarstTunnel(ChunkData chunk, int startX, int startZ, int baseY, Vector2 direction, double weight, Random rand)
        {
            if (direction.LengthSquared() < 1e-4f || baseY <= 2)
            {
                return;
            }

            var dir = Vector2.Normalize(direction);
            double radius = Math.Clamp(1.3 + weight * 0.6, 1.3, 2.4);
            int steps = Math.Clamp((int)Math.Round(3 + weight * 4), 3, 8);
            double x = startX;
            double z = startZ;

            for (int i = 0; i < steps; i++)
            {
                int cx = (int)Math.Round(x);
                int cz = (int)Math.Round(z);
                if (cx < 1 || cx > 14 || cz < 1 || cz > 14)
                {
                    break;
                }

                CarveSphere(chunk, cx, baseY, cz, radius);
                if (rand.NextDouble() < 0.25 && baseY - 1 > 0)
                {
                    chunk.SetBlock(cx, baseY - 1, cz, BlockType.Clay);
                }

                x += dir.X + (rand.NextDouble() - 0.5) * 0.35;
                z += dir.Y + (rand.NextDouble() - 0.5) * 0.35;
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

        private static void ResolvePerpendicularOffset(Vector2 direction, int step, out int offsetX, out int offsetZ)
        {
            float absX = Math.Abs(direction.X);
            float absZ = Math.Abs(direction.Y);

            offsetX = absX >= 0.35f ? (direction.X >= 0f ? step : -step) : 0;
            offsetZ = absZ >= 0.35f ? (direction.Y >= 0f ? step : -step) : 0;

            if (offsetX == 0 && offsetZ == 0)
            {
                if (absX >= absZ)
                {
                    offsetX = direction.X >= 0f ? step : -step;
                }
                else
                {
                    offsetZ = direction.Y >= 0f ? step : -step;
                }
            }
        }

        private void ExpandRiverChannel(ChunkData chunk, int[,] surfaceCache, int x, int z, int riverSurface, Vector2 flowDir, double channelPressure)
        {
            if (channelPressure < 0.45)
            {
                return;
            }

            var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
            if (perpendicular.LengthSquared() < 1e-6f)
            {
                perpendicular = Vector2.UnitX;
            }
            perpendicular = Vector2.Normalize(perpendicular);

            int reach = channelPressure > 0.9 ? 2 : 1;
            for (int step = 1; step <= reach; step++)
            {
                double floodStrength = Math.Clamp(channelPressure - 0.35 - 0.15 * (step - 1), 0.0, 1.0);
                if (floodStrength <= 0.0)
                {
                    continue;
                }

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);

                ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), step, out offsetX, out offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);
            }
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

        private int[,] BuildSurfaceCache(ChunkData chunk)
        {
            var cache = new int[16, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    cache[x, z] = FindSurfaceLevel(chunk, x, z);
                }
            }

            return cache;
        }

        private static Vector2 ComputeTerrainSlopeDirection(int[,] surfaceCache, int x, int z)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);

            int leftIndex = Math.Max(x - 1, 0);
            int rightIndex = Math.Min(x + 1, width - 1);
            int backIndex = Math.Max(z - 1, 0);
            int forwardIndex = Math.Min(z + 1, depth - 1);

            float dx = surfaceCache[rightIndex, z] - surfaceCache[leftIndex, z];
            float dz = surfaceCache[x, forwardIndex] - surfaceCache[x, backIndex];

            var flow = new Vector2(-dx, -dz);
            if (flow.LengthSquared() < 1e-4f)
            {
                return Vector2.Zero;
            }

            return Vector2.Normalize(flow);
        }

        private double[,] BuildHydrologyMask(int chunkX, int chunkZ, int[,] surfaceCache)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            var mask = new double[width, depth];

            int minSurface = int.MaxValue;
            int maxSurface = int.MinValue;
            bool hasSurface = false;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    hasSurface = true;
                    minSurface = Math.Min(minSurface, surface);
                    maxSurface = Math.Max(maxSurface, surface);
                }
            }

            if (!hasSurface)
            {
                return mask;
            }

            double invRange = 1.0 / Math.Max(1, maxSurface - minSurface);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        mask[x, z] = 0.0;
                        continue;
                    }

                    double slopeAccum = 0.0;
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
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            slopeAccum += Math.Abs(surface - neighborSurface);
                            neighborCount++;
                        }
                    }

                    double slope = neighborCount > 0 ? slopeAccum / neighborCount : 0.0;
                    slope = Math.Clamp(slope / 14.0, 0.0, 1.0);

                    double valley = Math.Clamp((GlobalWaterLevel - surface) / 20.0, 0.0, 1.0);
                    double relative = 1.0 - Math.Clamp((surface - minSurface) * invRange, 0.0, 1.0);

                    int worldX = chunkX * 16 + x;
                    int worldZ = chunkZ * 16 + z;
                    double humidityNoise = SimplexNoise.Generate(worldX + 13.5, worldZ - 71.5, 0.0012, 3, 1.0, 0.6, 71337);
                    double humidity = 1.0 - Math.Abs(humidityNoise) * 0.8;
                    humidity = Math.Clamp(humidity, 0.0, 1.0);

                    mask[x, z] = Math.Clamp(slope * 0.45 + valley * 0.3 + relative * 0.15 + humidity * 0.25, 0.0, 1.0);
                }
            }

            return mask;
        }

        private static double[,] BuildFlowAccumulation(int[,] surfaceCache)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            var raw = new double[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    double contribution = 0.0;
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

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            int delta = neighborSurface - surface;
                            if (delta <= 1)
                            {
                                continue;
                            }

                            double weight = 1.0 + Math.Min(6, delta) * 0.15;
                            if (dx != 0 && dz != 0)
                            {
                                weight *= 0.65;
                            }

                            contribution += weight;
                        }
                    }

                    raw[x, z] = contribution;
                }
            }

            var smoothed = new double[width, depth];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double total = raw[x, z];
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
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            total += raw[nx, nz] * 0.5;
                            samples++;
                        }
                    }

                    smoothed[x, z] = samples > 0 ? total / samples : raw[x, z];
                }
            }

            return smoothed;
        }

        private static double AdjustRiverIntensity(double baseIntensity, double hydrologyBias)
        {
            double clamped = Math.Clamp(hydrologyBias, 0.0, 1.0);
            double scaled = baseIntensity * (1.0 - clamped * 0.55) - clamped * 0.004;
            return Math.Max(0.0, scaled);
        }

        private static double ComputeChannelPressure(double catchmentStrength, double hydrology)
        {
            double baseValue = 0.35 + catchmentStrength * 0.5 + hydrology * 0.3;
            return Math.Clamp(baseValue, 0.0, 1.0);
        }

        private void GenerateRiversInternal(TerrainGenerationContext context)

        {

            var chunk = context.Chunk;

            var riverField = GetRiverFieldCache(context);

            TerrainProfile[,]? profiles = null;

            context.TryGetMetadata(TerrainProfilesKey, out profiles);



            var surfaceCache = BuildSurfaceCache(chunk);

            var hydrologyMask = BuildHydrologyMask(context.ChunkX, context.ChunkZ, surfaceCache);

            var flowAccumulation = BuildFlowAccumulation(surfaceCache);

            var riverIntensity = new double[16, 16];

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

                    double hydrology = hydrologyMask[x, z];
                    double catchment = flowAccumulation[x, z];
                    double catchmentStrength = Math.Clamp(catchment / 6.0, 0.0, 1.0);
                    double channelPressure = ComputeChannelPressure(catchmentStrength, hydrology);
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrology) - catchmentStrength * 0.015;
                    intensity = Math.Max(0.0, intensity);
                    riverIntensity[x, z] = intensity;
                    if (intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    Vector2 flowDir = riverField.Flow[x, z];
                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    if (slopeDir.LengthSquared() > 1e-4f)
                    {
                        Vector2 blended = Vector2.Lerp(flowDir, slopeDir, 0.65f);
                        flowDir = blended.LengthSquared() > 1e-5f ? Vector2.Normalize(blended) : slopeDir;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(surface, GlobalWaterLevel);

                    if (intensity < RiverCenterThreshold)
                    {
                        double normalized = 1.0 - Math.Clamp(intensity / RiverCenterThreshold, 0.0, 1.0);
                        CarveRiverColumn(chunk, surfaceCache, x, z, riverSurface, normalized, channelPressure, flowDir);
                    }
                    else
                    {
                        double bankStrength = 1.0 - Math.Clamp((intensity - RiverCenterThreshold) / (RiverBankThreshold - RiverCenterThreshold), 0.0, 1.0);
                        bankStrength *= 0.85 + channelPressure * 0.35;
                        FeatherRiverBank(chunk, surfaceCache, x, z, bankStrength, riverSurface, flowDir);
                    }
                }
            }

            StitchTributaryChannels(context, chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField, riverIntensity);
            ApplyRiverbankErosion(chunk, surfaceCache, riverField, hydrologyMask);
            ApplyRiverSedimentPass(context, chunk, surfaceCache, riverField, hydrologyMask);
            ApplyRiverPointBarSediment(context, chunk, surfaceCache, riverField, riverIntensity);
            AddFloodplainWetlands(context, chunk, surfaceCache, hydrologyMask, riverIntensity);
            AddFloodplainSwales(chunk, surfaceCache, hydrologyMask, riverIntensity);
            AddRiverDeltaFans(chunk, surfaceCache, riverField, hydrologyMask, flowAccumulation, riverIntensity);
            ApplyRiverGradientSmoothing(chunk, surfaceCache, hydrologyMask, riverIntensity);
            ApplyRiverMeanderTerraces(chunk, surfaceCache, riverIntensity, hydrologyMask, riverField);
            ApplyRiverHydrologyFeedback(chunk, surfaceCache, riverField, hydrologyMask, flowAccumulation, riverIntensity);
            AddRiverSeepageChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, riverField);
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

        private static double ComputeLocalRelief(int[,] surfaceCache, int centerX, int centerZ, int radius)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            double sum = 0.0;
            double sumSq = 0.0;
            int samples = 0;

            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    samples++;
                    double height = surface;
                    sum += height;
                    sumSq += height * height;
                    min = Math.Min(min, height);
                    max = Math.Max(max, height);
                }
            }

            if (samples == 0)
            {
                return 0.0;
            }

            double mean = sum / samples;
            double variance = Math.Max(0.0, (sumSq / samples) - mean * mean);
            double stdDev = Math.Sqrt(variance);
            return (max - min) + stdDev;
        }

        private void GenerateLakesInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var riverField = GetRiverFieldCache(context);
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyMask = BuildHydrologyMask(context.ChunkX, context.ChunkZ, surfaceCache);
            var flowAccumulation = BuildFlowAccumulation(surfaceCache);
            var warp = SimplexNoise.DomainWarp(context.ChunkX * 16, context.ChunkZ * 16, 0.00045, 0.0009, 14.0, 9.0, 67891);
            double lakeSimplex = SimplexNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.035, 3, 1.0, 0.55, 67891);
            double lakePerlin = PerlinNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.028, 2, 1.0, 0.6, 77811);
            double lakeNoise = (lakeSimplex + lakePerlin) * 0.5;
            if (lakeNoise < 0.62)
                return;

            var rand = new Random((context.ChunkX * 928371) ^ (context.ChunkZ * 72341) ^ 0xC0FFEE);
            double chunkWeight = Math.Clamp((lakeNoise - 0.62) * 1.8, 0.0, 1.0);

            int centerX = rand.Next(4, 12);
            int centerZ = rand.Next(4, 12);
            double hydrology = hydrologyMask[centerX, centerZ];
            double relief = ComputeLocalRelief(surfaceCache, centerX, centerZ, 6);
            double basinStability = 1.0 - Math.Clamp(relief / 10.0, 0.0, 1.0);
            double spawnWeight = Math.Clamp((chunkWeight * 0.6 + hydrology * 0.8) * (0.65 + basinStability * 0.5), 0.0, 1.2);
            if (spawnWeight < 0.25 || rand.NextDouble() > spawnWeight || basinStability < 0.3)
                return;

            int radiusX = 3 + rand.Next(4) + (int)Math.Round(hydrology * 2.0);
            int radiusZ = 3 + rand.Next(4) + (int)Math.Round(hydrology * 2.0);
            radiusX = Math.Clamp(radiusX, 3, 9);
            radiusZ = Math.Clamp(radiusZ, 3, 9);
            int maxDepth = 3 + rand.Next(3) + (int)Math.Round(hydrology * 2.0);
            maxDepth = Math.Clamp((int)Math.Round(Math.Clamp(maxDepth * (0.7 + basinStability * 0.6), 3, 9)), 3, 9);
            int waterLevel = Math.Clamp(
                GlobalWaterLevel + rand.Next(-1, 2) + (int)Math.Round((hydrology - 0.5) * 3.0),
                45,
                80);

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

            relief = ComputeLocalRelief(surfaceCache, centerX, centerZ, Math.Max(radiusX, radiusZ) + 4);
            basinStability = 1.0 - Math.Clamp(relief / 12.0, 0.0, 1.0);

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
                ApplyLakeTerraces(chunk, surfaceCache, centerX, centerZ, waterLevel, radiusX, radiusZ, rotation);
                AddLakeShorelineBenches(chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rotation, basinStability);
                DepositLakeSedimentRings(chunk, centerX, centerZ, waterLevel, radiusX, radiusZ);
                EnhanceLakeShoreVegetation(chunk, centerX, centerZ, radiusX, radiusZ, rand);
                CreateLakeSeeps(context, chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rand);
                TryLinkLakeToRiver(context, chunk, riverField, hydrologyMask, flowAccumulation, surfaceCache, centerX, centerZ, waterLevel, radiusX, radiusZ);
                AddLakeWetlandPockets(chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rand);
                waterLevel = EqualizeLakeWaterTable(chunk, surfaceCache, hydrologyMask, centerX, centerZ, radiusX, radiusZ, waterLevel);
                AddLakeOverflowChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterLevel, radiusX, radiusZ);
                StabilizeLakeCatchments(chunk, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterLevel, radiusX, radiusZ);
                ApplyLakeHydrologyFeedback(chunk, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterLevel, radiusX, radiusZ);
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

        private int EqualizeLakeWaterTable(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            int waterSurface)
        {
            double rimHydrology = SampleLakeRingHydrology(hydrologyMask, centerX, centerZ, radiusX, radiusZ, 1.05, 1.4);
            double basinHydrology = hydrologyMask[Math.Clamp(centerX, 0, 15), Math.Clamp(centerZ, 0, 15)];
            double pressureDelta = (rimHydrology - basinHydrology) * 3.5;
            int targetLevel = Math.Clamp(waterSurface + (int)Math.Round(pressureDelta), waterSurface - 2, waterSurface + 3);
            targetLevel = Math.Clamp(targetLevel, 45, Math.Min(GlobalWaterLevel + 4, 120));

            if (targetLevel == waterSurface)
            {
                return waterSurface;
            }

            AdjustLakeWaterColumns(chunk, surfaceCache, centerX, centerZ, radiusX, radiusZ, waterSurface, targetLevel);
            return targetLevel;
        }

        private static double SampleLakeRingHydrology(
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            double inner,
            double outer)
        {
            double sum = 0.0;
            int samples = 0;
            int limitX = radiusX + 6;
            int limitZ = radiusZ + 6;

            for (int dx = -limitX; dx <= limitX; dx++)
            {
                for (int dz = -limitZ; dz <= limitZ; dz++)
                {
                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.5, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.5, 2)));

                    if (ellipse < inner || ellipse > outer)
                    {
                        continue;
                    }

                    int sampleX = Math.Clamp(centerX + dx, 0, 15);
                    int sampleZ = Math.Clamp(centerZ + dz, 0, 15);
                    sum += hydrologyMask[sampleX, sampleZ];
                    samples++;
                }
            }

            if (samples == 0)
            {
                return hydrologyMask[Math.Clamp(centerX, 0, 15), Math.Clamp(centerZ, 0, 15)];
            }

            return sum / samples;
        }

        private void AdjustLakeWaterColumns(
            ChunkData chunk,
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
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.35, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.35, 2)));

                    if (ellipse <= 1.05)
                    {
                        int floor = FindLakeFloor(chunk, x, z, targetSurface);
                        floor = Math.Max(1, floor);
                        chunk.SetBlock(x, floor, z, BlockType.Clay);
                        int waterTop = Math.Min(targetSurface, 255);
                        for (int y = floor + 1; y <= waterTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        int maxClear = Math.Max(previousSurface, waterTop);
                        for (int y = waterTop + 1; y <= maxClear && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        surfaceCache[x, z] = waterTop;
                    }
                    else if (ellipse <= 1.35)
                    {
                        double rimStrength = Math.Clamp(1.35 - ellipse, 0.0, 0.6);
                        SculptLakeBank(chunk, x, z, targetSurface, rimStrength + 0.2);
                    }
                }
            }
        }

        private static int FindLakeFloor(ChunkData chunk, int x, int z, int searchStart)
        {
            for (int y = Math.Min(searchStart, 254); y >= 1; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }

            return 1;
        }

        private void ApplyLakeTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            double rotation)
        {
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double shallowRadiusX = Math.Max(2.0, radiusX * 0.7);
            double shallowRadiusZ = Math.Max(2.0, radiusZ * 0.7);
            double bankRadiusX = radiusX + 4.0;
            double bankRadiusZ = radiusZ + 4.0;

            int extentX = (int)Math.Ceiling(bankRadiusX);
            int extentZ = (int)Math.Ceiling(bankRadiusZ);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double rotX = dx * cos - dz * sin;
                    double rotZ = dx * sin + dz * cos;

                    double shallowEllipse = Math.Sqrt(
                        (rotX * rotX) / Math.Max(1.0, shallowRadiusX * shallowRadiusX) +
                        (rotZ * rotZ) / Math.Max(1.0, shallowRadiusZ * shallowRadiusZ));

                    double bankEllipse = Math.Sqrt(
                        (rotX * rotX) / Math.Max(1.0, bankRadiusX * bankRadiusX) +
                        (rotZ * rotZ) / Math.Max(1.0, bankRadiusZ * bankRadiusZ));

                    if (shallowEllipse <= 1.0)
                    {
                        int shelfTop = Math.Max(1, waterSurface - 1);
                        for (int y = shelfTop; y <= waterSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        int floor = Math.Max(1, shelfTop - 1);
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
                        surfaceCache[x, z] = Math.Min(waterSurface, 255);
                    }
                    else if (bankEllipse <= 1.2)
                    {
                        int surface = surfaceCache[x, z];
                        if (surface <= 0)
                        {
                            surface = FindSurfaceLevel(chunk, x, z);
                            if (surface <= 0)
                            {
                                continue;
                            }
                            surfaceCache[x, z] = surface;
                        }

                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                        }
                    }
                }
            }
        }

        private void AddLakeShorelineBenches(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            double rotation,
            double basinStability)
        {
            if (basinStability < 0.2)
            {
                return;
            }

            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double radiusXWithPadding = radiusX + 0.75;
            double radiusZWithPadding = radiusZ + 0.75;
            int extentX = radiusX + 6;
            int extentZ = radiusZ + 6;

            var bands = new (double inner, double outer, bool floodable)[]
            {
                (1.02, 1.18, true),
                (1.18, 1.36, false)
            };

            foreach (var band in bands)
            {
                for (int dx = -extentX; dx <= extentX; dx++)
                {
                    for (int dz = -extentZ; dz <= extentZ; dz++)
                    {
                        int worldX = centerX + dx;
                        int worldZ = centerZ + dz;
                        if (worldX < 0 || worldX >= 16 || worldZ < 0 || worldZ >= 16)
                        {
                            continue;
                        }

                        double rotX = dx * cos - dz * sin;
                        double rotZ = dx * sin + dz * cos;
                        double ellipse = Math.Sqrt(
                            (rotX * rotX) / Math.Max(1.0, radiusXWithPadding * radiusXWithPadding) +
                            (rotZ * rotZ) / Math.Max(1.0, radiusZWithPadding * radiusZWithPadding));

                        if (ellipse < band.inner || ellipse > band.outer)
                        {
                            continue;
                        }

                        if (!TryResolveSurface(chunk, surfaceCache, worldX, worldZ, out int surface))
                        {
                            continue;
                        }

                        int targetSurface = band.floodable
                            ? Math.Max(waterSurface + (basinStability > 0.6 ? 0 : 1), 1)
                            : Math.Max(waterSurface + 2 + (band.inner > 1.18 ? 1 : 0), 1);
                        targetSurface = Math.Min(surface, targetSurface);

                        for (int y = surface; y > targetSurface; y--)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }

                        double hydrology = Math.Clamp(
                            hydrologyMask[
                                Math.Clamp(worldX, 0, hydrologyMask.GetLength(0) - 1),
                                Math.Clamp(worldZ, 0, hydrologyMask.GetLength(1) - 1)],
                            0.0,
                            1.0);

                        BlockType material = band.floodable
                            ? (hydrology > 0.62 ? BlockType.Clay : BlockType.Sand)
                            : (basinStability > 0.5 ? BlockType.Grass : BlockType.Dirt);
                        chunk.SetBlock(worldX, targetSurface, worldZ, material);

                        if (band.floodable)
                        {
                            for (int y = targetSurface + 1; y <= waterSurface && y < 256; y++)
                            {
                                chunk.SetBlock(worldX, y, worldZ, BlockType.Water);
                            }
                        }
                        else if (targetSurface + 1 < 256)
                        {
                            chunk.SetBlock(worldX, targetSurface + 1, worldZ, BlockType.Air);
                        }

                        surfaceCache[worldX, worldZ] = targetSurface;
                    }
                }
            }
        }

        private void TryLinkLakeToRiver(TerrainGenerationContext context, ChunkData chunk, RiverFieldCache riverField, double[,] hydrologyMask, double[,] flowAccumulation, int[,] surfaceCache, int centerX, int centerZ, int waterLevel, int radiusX, int radiusZ)
        {
            int searchRadius = Math.Max(radiusX, radiusZ) + 6;
            double bestScore = 0.09;
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

                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrologyMask[x, z]);
                    if (intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    double catchment = flowAccumulation[x, z];
                    double catchmentBias = Math.Clamp(catchment / 8.0, 0.0, 1.0);
                    double score = intensity - hydrologyMask[x, z] * 0.02 - catchmentBias * 0.015;
                    double relief = ComputeLocalRelief(surfaceCache, x, z, 2);
                    score += Math.Clamp(relief / 10.0, 0.0, 1.0) * 0.01;

                    if (score < bestScore)
                    {
                        bestScore = score;
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

        private void CarveRiverColumn(ChunkData chunk, int[,] surfaceCache, int x, int z, int riverSurface, double normalized, double channelPressure, Vector2 flowDir)
        {
            int surface = surfaceCache[x, z];
            if (surface <= 0)
                return;

            if (surface <= GlobalWaterLevel - 3 && chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            double pressureScale = 0.85 + channelPressure * 0.65;
            int channelDepth = Math.Clamp(3 + (int)Math.Round(normalized * 6.5 * pressureScale), 3, 10);
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

            ExpandRiverChannel(chunk, surfaceCache, x, z, riverSurface, flowDir, channelPressure);

            int maxRadius = Math.Clamp(2 + (int)Math.Round(normalized * (2.0 + channelPressure * 1.5)), 2, 5);
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

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);

                ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), step, out offsetX, out offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);
            }
        }

        private void StitchTributaryChannels(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField,
            double[,] riverIntensity)
        {
            var rand = new Random((context.ChunkX * 29791) ^ (context.ChunkZ * 911) ^ 0x7F1B);
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.7)
                    {
                        continue;
                    }

                    double catchment = Math.Clamp(flowAccumulation[x, z] / 7.5, 0.0, 1.0);
                    double intensity = riverField.Intensity[x, z];
                    if (intensity >= RiverBankThreshold * 1.1 || intensity <= RiverCenterThreshold * 0.35)
                    {
                        continue;
                    }

                    double riverGap = 1.0 - Math.Clamp(intensity / RiverCenterThreshold, 0.0, 1.0);
                    double weight = hydrology * 0.4 + catchment * 0.4 + riverGap * 0.35;
                    if (weight < 0.55 || rand.NextDouble() > weight * 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    var slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    Vector2 direction = flowDir.LengthSquared() > 1e-4f ? flowDir : slopeDir;
                    if (direction.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    TraceTributaryChannel(chunk, surfaceCache, riverIntensity, x, z, direction, weight, rand);
                }
            }
        }

        private void DepositLakeSedimentRings(ChunkData chunk, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ)
        {
            double innerRadiusX = Math.Max(1.0, radiusX + 0.5);
            double innerRadiusZ = Math.Max(1.0, radiusZ + 0.5);
            double outerRadiusX = innerRadiusX + 2.0;
            double outerRadiusZ = innerRadiusZ + 2.0;
            int extentX = (int)Math.Ceiling(outerRadiusX + 2.0);
            int extentZ = (int)Math.Ceiling(outerRadiusZ + 2.0);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, innerRadiusX * innerRadiusX) +
                        (dz * dz) / Math.Max(1.0, innerRadiusZ * innerRadiusZ));
                    if (ellipse > 1.35)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(chunk, x, z);
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int targetY = Math.Max(1, Math.Min(surface, waterSurface) - 1);
                    if (ellipse <= 1.0)
                    {
                        chunk.SetBlock(x, targetY, z, BlockType.Clay);
                    }
                    else if (ellipse <= 1.25)
                    {
                        chunk.SetBlock(x, targetY, z, BlockType.Sand);
                    }
                }
            }
        }

        private void EnhanceLakeShoreVegetation(ChunkData chunk, int centerX, int centerZ, int radiusX, int radiusZ, Random rand)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, radiusX * radiusX) +
                        (dz * dz) / Math.Max(1.0, radiusZ * radiusZ));

                    if (ellipse > 1.45)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(chunk, x, z);
                    if (surface <= 0 || surface + 1 >= 256)
                    {
                        continue;
                    }

                    var topBlock = chunk.GetBlock(x, surface, z);
                    if (topBlock == BlockType.Sand && rand.NextDouble() < 0.45)
                    {
                        if (chunk.GetBlock(x, surface + 1, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, surface + 1, z, rand.NextDouble() < 0.7 ? BlockType.TallGrass : BlockType.DeadBush);
                        }
                    }
                    else if (topBlock == BlockType.Dirt && ellipse <= 1.1 && rand.NextDouble() < 0.35)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Grass);
                        if (chunk.GetBlock(x, surface + 1, z) == BlockType.Air && rand.NextDouble() < 0.4)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.TallGrass);
                        }
                    }
                }
            }
        }

        private void CreateLakeSeeps(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            Random rand)
        {
            int attempts = 4;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int sampleX = Math.Clamp(centerX + rand.Next(-radiusX - 5, radiusX + 6), 1, 14);
                int sampleZ = Math.Clamp(centerZ + rand.Next(-radiusZ - 5, radiusZ + 6), 1, 14);

                double hydrology = hydrologyMask[sampleX, sampleZ];
                if (hydrology < 0.75)
                {
                    continue;
                }

                int surface = surfaceCache[sampleX, sampleZ];
                if (surface <= waterSurface + 1)
                {
                    continue;
                }

                int trenchDepth = Math.Clamp((int)Math.Round(1 + (hydrology - 0.7) * 4.0), 1, 4);
                int floor = Math.Max(surface - trenchDepth, 1);

                for (int y = surface; y >= floor; y--)
                {
                    chunk.SetBlock(sampleX, y, sampleZ, BlockType.Air);
                }

                for (int y = floor; y <= waterSurface && y < 256; y++)
                {
                    chunk.SetBlock(sampleX, y, sampleZ, BlockType.Water);
                }

                if (floor - 1 >= 1)
                {
                    chunk.SetBlock(sampleX, floor - 1, sampleZ, BlockType.Clay);
                }

                surfaceCache[sampleX, sampleZ] = Math.Max(surfaceCache[sampleX, sampleZ], Math.Min(waterSurface, 255));
            }
        }

        private void AddLakeWetlandPockets(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            Random rand)
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
                    if (x < 1 || x >= 15 || z < 1 || z >= 15)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.5, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.5, 2)));
                    if (ellipse <= 1.05 || ellipse >= 1.45)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double spawnWeight = hydrology * 0.65 +
                                         Math.Clamp((ellipse - 1.05) * 1.4, 0.0, 0.35) +
                                         rand.NextDouble() * 0.2;
                    if (spawnWeight < 0.75)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface < waterSurface - 4 || surface > waterSurface + 6)
                    {
                        continue;
                    }

                    int pocketDepth = Math.Clamp((int)Math.Round(1 + (hydrology - 0.45) * 3.0), 1, 3);
                    int floor = Math.Max(1, surface - pocketDepth);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    if (hydrology > 0.65)
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Water);
                        if (floor + 1 < 256)
                        {
                            chunk.SetBlock(x, floor + 1, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
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
                            if (rimX < 0 || rimX >= 16 || rimZ < 0 || rimZ >= 16)
                            {
                                continue;
                            }

                            int rimSurface = FindSurfaceLevel(chunk, rimX, rimZ);
                            if (rimSurface <= 0)
                            {
                                continue;
                            }

                            var rimType = hydrology > 0.6 ? BlockType.Clay : BlockType.Grass;
                            chunk.SetBlock(rimX, rimSurface, rimZ, rimType);
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

        private void AddLakeOverflowChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
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

            (int dx, int dz, double weight) primary = (0, 0, 0.0);
            (int dx, int dz, double weight) secondary = (0, 0, 0.0);

            foreach (var dir in directions)
            {
                int edgeX = centerX + dir.dx * dir.extent;
                int edgeZ = centerZ + dir.dz * dir.extent;
                if (edgeX < 1 || edgeX >= 15 || edgeZ < 1 || edgeZ >= 15)
                {
                    continue;
                }

                double hydrology = Math.Clamp(hydrologyMask[edgeX, edgeZ], 0.0, 1.0);
                double accumulation = Math.Clamp(flowAccumulation[edgeX, edgeZ], 0.0, 6.0);
                int neighborSurface = surfaceCache[edgeX, edgeZ];
                if (neighborSurface <= 0)
                {
                    neighborSurface = FindSurfaceLevel(chunk, edgeX, edgeZ);
                    if (neighborSurface <= 0)
                    {
                        continue;
                    }
                    surfaceCache[edgeX, edgeZ] = neighborSurface;
                }

                double slope = Math.Clamp((waterSurface - neighborSurface) / 8.0, -1.0, 1.0);
                double weight = Math.Clamp(hydrology * 0.6 + Math.Clamp(accumulation / 4.0, 0.0, 1.0) * 0.4, 0.0, 1.0);
                weight *= 0.55 + Math.Max(0.0, slope);
                if (weight <= 0.35)
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

            if (primary.weight > 0.0)
            {
                CarveLakeOverflowChannel(chunk, surfaceCache, centerX, centerZ, primary.dx, primary.dz, waterSurface, primary.weight);
            }

            if (secondary.weight > 0.0)
            {
                CarveLakeOverflowChannel(chunk, surfaceCache, centerX, centerZ, secondary.dx, secondary.dz, waterSurface, secondary.weight * 0.85);
            }
        }

        private void CarveLakeOverflowChannel(
            ChunkData chunk,
            int[,] surfaceCache,
            int originX,
            int originZ,
            int dirX,
            int dirZ,
            int waterSurface,
            double weight)
        {
            if (dirX == 0 && dirZ == 0)
            {
                return;
            }

            int length = Math.Clamp((int)Math.Round(3 + weight * 5), 2, 8);
            int currentX = originX + dirX * 2;
            int currentZ = originZ + dirZ * 2;

            for (int step = 0; step < length; step++)
            {
                if (currentX < 1 || currentX >= 15 || currentZ < 1 || currentZ >= 15)
                {
                    break;
                }

                int surface = surfaceCache[currentX, currentZ];
                if (surface <= 1)
                {
                    surface = FindSurfaceLevel(chunk, currentX, currentZ);
                    if (surface <= 1)
                    {
                        break;
                    }
                    surfaceCache[currentX, currentZ] = surface;
                }

                int target = Math.Max(1, waterSurface - 2 - step);
                if (surface > target)
                {
                    for (int y = surface; y > target; y--)
                    {
                        chunk.SetBlock(currentX, y, currentZ, BlockType.Air);
                    }
                }

                int sandY = Math.Max(1, target - 1);
                chunk.SetBlock(currentX, sandY, currentZ, BlockType.Sand);
                for (int y = target; y <= Math.Min(waterSurface, 255); y++)
                {
                    chunk.SetBlock(currentX, y, currentZ, BlockType.Water);
                }

                surfaceCache[currentX, currentZ] = target;

                int bankX = currentX + dirZ;
                int bankZ = currentZ - dirX;
                if (bankX >= 0 && bankX < 16 && bankZ >= 0 && bankZ < 16)
                {
                    SculptLakeBank(chunk, bankX, bankZ, waterSurface, 0.6);
                }

                bankX = currentX - dirZ;
                bankZ = currentZ + dirX;
                if (bankX >= 0 && bankX < 16 && bankZ >= 0 && bankZ < 16)
                {
                    SculptLakeBank(chunk, bankX, bankZ, waterSurface, 0.55);
                }

                currentX += dirX;
                currentZ += dirZ;
            }
        }

        private void StabilizeLakeCatchments(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
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
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.6, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.6, 2)));
                    if (ellipse < 0.9 || ellipse > 1.6)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double pressure = Math.Max(hydrology, flow);
                    if (pressure < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1 || surface < waterSurface - 4)
                    {
                        continue;
                    }

                    int erosionDepth = Math.Clamp((int)Math.Round((pressure - 0.45) * 6.0), 1, 4);
                    int floor = Math.Max(surface - erosionDepth, 1);
                    for (int y = surface; y >= floor && y >= 1; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    int fillTop = Math.Min(waterSurface, 254);
                    if (hydrology > 0.65)
                    {
                        for (int y = floor; y <= fillTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        var fillMaterial = hydrology > 0.78 ? BlockType.Clay : BlockType.Sand;
                        chunk.SetBlock(x, floor, z, fillMaterial);
                    }

                    surfaceCache[x, z] = floor;
                    double rimStrength = Math.Clamp(pressure * 0.8, 0.2, 0.85);
                    SculptLakeBank(chunk, x, z, waterSurface, rimStrength);
                }
            }
        }

        private void ApplyLakeHydrologyFeedback(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
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
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.75, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.75, 2)));
                    if (ellipse <= 1.05 || ellipse >= 1.65)
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface) || surface <= 0)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double moisture = Math.Max(hydrology, flow);
                    if (moisture < 0.45)
                    {
                        continue;
                    }

                    int drop = Math.Clamp((int)Math.Round(1 + moisture * 3.5), 1, 4);
                    int target = Math.Max(surface - drop, Math.Max(waterSurface - 1, 1));

                    for (int y = surface; y > target; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    if (hydrology > 0.65)
                    {
                        chunk.SetBlock(x, target, z, BlockType.Clay);
                        for (int y = target + 1; y <= waterSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                        surfaceCache[x, z] = Math.Min(waterSurface, 255);
                    }
                    else
                    {
                        chunk.SetBlock(x, target, z, BlockType.Sand);
                        if (target + 1 < 256)
                        {
                            chunk.SetBlock(x, target + 1, z, BlockType.Air);
                        }
                        surfaceCache[x, z] = target;
                    }
                }
            }
        }

        private void TraceTributaryChannel(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] riverIntensity,
            int startX,
            int startZ,
            Vector2 direction,
            double strength,
            Random rand)
        {
            if (direction.LengthSquared() < 1e-4f)
            {
                return;
            }

            var dir = Vector2.Normalize(direction);
            double stepX = dir.X;
            double stepZ = dir.Y;
            int steps = Math.Clamp((int)Math.Round(3 + strength * 5), 3, 8);
            double x = startX;
            double z = startZ;
            double channelPressure = Math.Clamp(0.35 + strength * 0.4, 0.35, 0.85);

            for (int i = 0; i < steps; i++)
            {
                int cx = (int)Math.Round(x);
                int cz = (int)Math.Round(z);
                if (cx < 0 || cx >= 16 || cz < 0 || cz >= 16)
                {
                    break;
                }

                int surface = surfaceCache[cx, cz];
                if (surface <= 0)
                {
                    break;
                }

                int riverSurface = Math.Min(surface, GlobalWaterLevel);
                double normalized = Math.Clamp(0.55 + strength * 0.35 - i * 0.08, 0.2, 0.95);
                CarveRiverColumn(chunk, surfaceCache, cx, cz, riverSurface, normalized, channelPressure, dir);
                riverIntensity[cx, cz] = Math.Min(riverIntensity[cx, cz], RiverCenterThreshold * 0.5);

                x += stepX + (rand.NextDouble() - 0.5) * 0.3;
                z += stepZ + (rand.NextDouble() - 0.5) * 0.3;
            }
        }

        private void ApplyRiverbankErosion(ChunkData chunk, int[,] surfaceCache, RiverFieldCache riverField, double[,] hydrologyMask)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrologyMask[x, z]);
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

        private void ApplyRiverSedimentPass(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask)
        {
            var rand = new Random((context.ChunkX * 83492791) ^ (context.ChunkZ * 297657976) ^ 0x51DED);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrology);
                    if (intensity >= RiverBankThreshold + 0.01)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    if (intensity < RiverCenterThreshold * 0.85)
                    {
                        int bedY = Math.Max(1, surface - 2 - rand.Next(2));
                        chunk.SetBlock(x, bedY, z, rand.NextDouble() < 0.4 ? BlockType.Cobblestone : BlockType.Sand);
                        if (bedY - 1 >= 1 && rand.NextDouble() < 0.25)
                        {
                            chunk.SetBlock(x, bedY - 1, z, BlockType.Cobblestone);
                        }

                        continue;
                    }

                    double floodChance = Math.Clamp((hydrology - 0.45) * 1.4, 0.0, 1.0);
                    if (floodChance <= 0.0 || rand.NextDouble() > floodChance)
                    {
                        continue;
                    }

                    int targetHeight = Math.Max(surface - 1, 1);
                    chunk.SetBlock(x, targetHeight, z, BlockType.Sand);
                    if (targetHeight + 1 < 256)
                    {
                        chunk.SetBlock(x, targetHeight + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void ApplyRiverPointBarSediment(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] riverIntensity)
        {
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity < RiverCenterThreshold * 0.85 || intensity > RiverBankThreshold * 1.05)
                    {
                        continue;
                    }

                    Vector2 flow = riverField.Flow[x, z];
                    if (flow.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    Vector2 perpendicular = new Vector2(-flow.Y, flow.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = new Vector2(1, 0);
                    }
                    perpendicular = Vector2.Normalize(perpendicular);

                    double noise = SimplexNoise.Generate(originX + x * 0.45, originZ + z * 0.45, 0.08, 2, 1.0, 0.55, 8713);
                    int offsetSign = noise >= 0 ? 1 : -1;
                    int targetX = x + Math.Clamp((int)Math.Round(perpendicular.X) * offsetSign, -1, 1);
                    int targetZ = z + Math.Clamp((int)Math.Round(perpendicular.Y) * offsetSign, -1, 1);
                    if (targetX < 0 || targetX >= 16 || targetZ < 0 || targetZ >= 16)
                    {
                        continue;
                    }

                    int surface = surfaceCache[targetX, targetZ];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, targetX, targetZ);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[targetX, targetZ] = surface;
                    }

                    var topBlock = chunk.GetBlock(targetX, surface, targetZ);
                    if (topBlock == BlockType.Air)
                    {
                        continue;
                    }

                    if (topBlock == BlockType.Grass || topBlock == BlockType.Dirt || topBlock == BlockType.Clay)
                    {
                        chunk.SetBlock(targetX, surface, targetZ, BlockType.Sand);
                        if (surface - 1 >= 1 && chunk.GetBlock(targetX, surface - 1, targetZ) == BlockType.Dirt)
                        {
                            chunk.SetBlock(targetX, surface - 1, targetZ, BlockType.Clay);
                        }
                    }

                    if (intensity < RiverCenterThreshold * 1.05)
                    {
                        int waterFloor = Math.Max(1, surface - 1);
                        chunk.SetBlock(targetX, Math.Max(1, waterFloor - 1), targetZ, BlockType.Sand);
                        chunk.SetBlock(targetX, waterFloor, targetZ, BlockType.Water);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(targetX, surface + 1, targetZ, BlockType.Air);
                        }
                        surfaceCache[targetX, targetZ] = Math.Max(surfaceCache[targetX, targetZ], waterFloor);
                    }
                }
            }
        }

        private void AddFloodplainWetlands(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            var rand = new Random((context.ChunkX * 49157) ^ (context.ChunkZ * 12289) ^ 0xB17F);

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.72)
                    {
                        continue;
                    }

                    double channelProximity = 1.0 - Math.Clamp(
                        (riverIntensity[x, z] - RiverCenterThreshold) /
                        (RiverBankThreshold - RiverCenterThreshold + 1e-5),
                        0.0,
                        1.0);

                    double weight = hydrology * 0.6 + channelProximity * 0.4;
                    if (weight < 0.78 || rand.NextDouble() > weight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface >= 255)
                    {
                        continue;
                    }

                    int basinDepth = Math.Clamp((int)Math.Round(1 + weight * 3.0), 1, 4);
                    int floor = Math.Max(surface - basinDepth, 1);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    int supportY = Math.Max(1, floor - 1);
                    chunk.SetBlock(x, supportY, z, BlockType.Clay);
                    int waterTop = Math.Min(floor + 1, surface);
                    for (int y = floor; y <= waterTop; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    if (surface + 1 < 256 && rand.NextDouble() < 0.35)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.TallGrass);
                    }

                    surfaceCache[x, z] = Math.Max(surfaceCache[x, z], waterTop);
                }
            }
        }

        private void AddFloodplainSwales(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold || intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    double wetness = Math.Clamp((hydrology - 0.4) * 0.9 + (RiverBankThreshold - intensity) * 2.2, 0.0, 1.0);
                    if (wetness <= 0.05)
                    {
                        continue;
                    }

                    int swaleDepth = Math.Clamp((int)Math.Round(1 + wetness * 3.5), 1, 4);
                    int floor = Math.Max(1, surface - swaleDepth);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = floor;
                    if (wetness > 0.6)
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Water);
                        if (floor + 1 < 256)
                        {
                            chunk.SetBlock(x, floor + 1, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
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
                            if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                            {
                                continue;
                            }

                            int neighborSurface = FindSurfaceLevel(chunk, nx, nz);
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            var rimType = wetness > 0.62 ? BlockType.Sand : BlockType.Grass;
                            chunk.SetBlock(nx, neighborSurface, nz, rimType);

                            if (wetness > 0.62 && neighborSurface + 1 < 256)
                            {
                                chunk.SetBlock(nx, neighborSurface + 1, nz, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        private void AddRiverDeltaFans(
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold * 0.55 || intensity >= RiverBankThreshold * 1.05)
                    {
                        continue;
                    }

                    double accumulation = flowAccumulation[x, z];
                    if (accumulation < 2.2)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double spawnWeight = Math.Clamp((accumulation - 2.0) * 0.18 + hydrology * 0.55, 0.0, 1.0);
                    spawnWeight *= 1.0 - Math.Clamp(
                        (intensity - RiverCenterThreshold) / (RiverBankThreshold - RiverCenterThreshold + 1e-5),
                        0.0,
                        1.0);
                    double selector = SampleDeterministicNoise(x, z, 257);
                    if (spawnWeight < 0.3 || selector > spawnWeight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    Vector2 flow = riverField.Flow[x, z];
                    if (flow.LengthSquared() < 1e-4f)
                    {
                        flow = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    }

                    if (flow.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    flow = Vector2.Normalize(flow);
                    Vector2 perpendicular = new(-flow.Y, flow.X);
                    bool braidRight = SampleDeterministicNoise(x + surface, z + surface, 311) > 0.5;
                    int fanReach = Math.Clamp((int)Math.Round(1 + accumulation * 0.35), 1, 4);

                    for (int step = 1; step <= fanReach; step++)
                    {
                        int targetX = Math.Clamp(x + (int)Math.Round(flow.X * step), 1, 14);
                        int targetZ = Math.Clamp(z + (int)Math.Round(flow.Y * step), 1, 14);

                        int columnSurface = surfaceCache[targetX, targetZ];
                        if (columnSurface <= 1)
                        {
                            columnSurface = FindSurfaceLevel(chunk, targetX, targetZ);
                            if (columnSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[targetX, targetZ] = columnSurface;
                        }

                        int riverSurface = Math.Min(columnSurface, Math.Min(254, GlobalWaterLevel));
                        double falloff = Math.Clamp(1.0 - step / (fanReach + 1.0), 0.0, 1.0);
                        ShapeRiverBank(chunk, surfaceCache, targetX, targetZ, 0.45 + falloff * 0.4, riverSurface, allowFlood: true);

                        Vector2 offset = braidRight ? perpendicular : -perpendicular;
                        int barX = Math.Clamp(targetX + (int)Math.Round(offset.X), 0, 15);
                        int barZ = Math.Clamp(targetZ + (int)Math.Round(offset.Y), 0, 15);

                        int barSurface = surfaceCache[barX, barZ];
                        if (barSurface <= 1)
                        {
                            barSurface = FindSurfaceLevel(chunk, barX, barZ);
                            if (barSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[barX, barZ] = barSurface;
                        }

                        chunk.SetBlock(barX, barSurface, barZ, BlockType.Sand);
                    }
                }
            }
        }

        private void ApplyRiverGradientSmoothing(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            var adjustments = new int[16, 16];
            bool hasAdjustment = false;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity > RiverBankThreshold * 1.15)
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
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            if (riverIntensity[nx, nz] > RiverBankThreshold * 1.2)
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

                    double averageSurface = neighborSum / (double)neighborCount;
                    double hydrologyBias = (hydrologyMask[x, z] - 0.5) * 2.0;
                    int targetSurface = (int)Math.Round(averageSurface - hydrologyBias);
                    int delta = targetSurface - surface;
                    if (Math.Abs(delta) <= 2)
                    {
                        continue;
                    }

                    adjustments[x, z] = Math.Clamp(delta, -4, 3);
                    hasAdjustment = true;
                }
            }

            if (!hasAdjustment)
            {
                return;
            }

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    int delta = adjustments[x, z];
                    if (delta == 0)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    int targetSurface = Math.Clamp(surface + delta, 2, 254);
                    if (delta < 0)
                    {
                        for (int y = surface; y > targetSurface && y >= 1; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        int waterTop = Math.Min(GlobalWaterLevel, Math.Min(255, surface));
                        for (int y = targetSurface; y <= waterTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        for (int y = surface + 1; y <= targetSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Sand);
                        }

                        if (targetSurface < GlobalWaterLevel)
                        {
                            for (int y = targetSurface + 1; y <= Math.Min(GlobalWaterLevel, surface) && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }
                    }

                    if (targetSurface + 1 < 256)
                    {
                        chunk.SetBlock(x, targetSurface + 1, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = targetSurface;
                }
            }
        }

        private void ApplyRiverMeanderTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] riverIntensity,
            double[,] hydrologyMask,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity >= RiverBankThreshold || intensity <= RiverCenterThreshold * 0.35)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        continue;
                    }

                    flowDir = Vector2.Normalize(flowDir);
                    var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = Vector2.UnitX;
                    }
                    perpendicular = Vector2.Normalize(perpendicular);

                    ResolvePerpendicularOffset(perpendicular, 1, out int posOffsetX, out int posOffsetZ);
                    ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), 1, out int negOffsetX, out int negOffsetZ);

                    if (!TrySampleField(riverIntensity, x + posOffsetX, z + posOffsetZ, out double posIntensity) ||
                        !TrySampleField(riverIntensity, x + negOffsetX, z + negOffsetZ, out double negIntensity))
                    {
                        continue;
                    }

                    bool posIsInner = posIntensity <= negIntensity;
                    var innerOffset = posIsInner ? (posOffsetX, posOffsetZ) : (negOffsetX, negOffsetZ);
                    var outerOffset = posIsInner ? (negOffsetX, negOffsetZ) : (posOffsetX, posOffsetZ);

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int baseSurface))
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x + innerOffset.Item1, z + innerOffset.Item2, out int innerSurface))
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(baseSurface, GlobalWaterLevel);
                    double normalized = Math.Clamp(1.0 - intensity / RiverCenterThreshold, 0.0, 1.0);
                    int shelfDepth = Math.Clamp((int)Math.Round(1 + normalized * 3.0), 1, 4);
                    int shelfFloor = Math.Max(riverSurface - shelfDepth, 1);

                    for (int y = innerSurface; y > shelfFloor; y--)
                    {
                        chunk.SetBlock(x + innerOffset.Item1, y, z + innerOffset.Item2, BlockType.Air);
                    }

                    double bankHydrology = Math.Clamp(
                        hydrologyMask[
                            Math.Clamp(x + innerOffset.Item1, 0, hydrologyMask.GetLength(0) - 1),
                            Math.Clamp(z + innerOffset.Item2, 0, hydrologyMask.GetLength(1) - 1)],
                        0.0,
                        1.0);
                    var shelfMaterial = bankHydrology > 0.62 ? BlockType.Clay : BlockType.Sand;
                    chunk.SetBlock(x + innerOffset.Item1, shelfFloor, z + innerOffset.Item2, shelfMaterial);

                    for (int y = shelfFloor + 1; y <= riverSurface && y < 256; y++)
                    {
                        chunk.SetBlock(x + innerOffset.Item1, y, z + innerOffset.Item2, BlockType.Water);
                    }

                    surfaceCache[x + innerOffset.Item1, z + innerOffset.Item2] = shelfFloor;

                    int outerX = x + outerOffset.Item1;
                    int outerZ = z + outerOffset.Item2;
                    if (!TryResolveSurface(chunk, surfaceCache, outerX, outerZ, out _))
                    {
                        continue;
                    }

                    double gradient = Math.Clamp(Math.Abs(posIntensity - negIntensity) * 38.0, 0.2, 0.85);
                    ShapeRiverBank(chunk, surfaceCache, outerX, outerZ, gradient * 0.5 + 0.2, riverSurface, allowFlood: false);
                }
            }
        }

        private void ApplyRiverHydrologyFeedback(
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface))
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    if (hydrology < 0.45 && flow < 0.45)
                    {
                        continue;
                    }

                    double intensity = riverIntensity[x, z];
                    int riverSurface = Math.Min(surface, Math.Min(254, GlobalWaterLevel));

                    var flowDir = riverField.Flow[x, z];
                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        flowDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    }

                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        flowDir = Vector2.UnitX;
                    }

                    flowDir = Vector2.Normalize(flowDir);

                    if (intensity < RiverCenterThreshold * 0.95)
                    {
                        int infiltration = Math.Clamp((int)Math.Round(1 + (hydrology + flow) * 3.5), 1, 6);
                        int floor = Math.Max(riverSurface - infiltration, 1);
                        for (int y = surface; y >= floor && y >= 1; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        if (hydrology > 0.62)
                        {
                            int fillTop = Math.Min(riverSurface, 254);
                            for (int y = floor; y <= fillTop && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }

                        int supportY = Math.Max(floor - 1, 0);
                        chunk.SetBlock(x, supportY, z, BlockType.Sand);
                        surfaceCache[x, z] = floor;
                        continue;
                    }

                    if (intensity >= RiverBankThreshold)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Grass);
                        }
                        continue;
                    }

                    double bankStrength = Math.Clamp((hydrology + flow) * 0.5, 0.0, 1.0);
                    var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = Vector2.UnitY;
                    }

                    perpendicular = Vector2.Normalize(perpendicular);
                    ResolvePerpendicularOffset(perpendicular, 1, out int offsetX, out int offsetZ);
                    ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.45 + 0.2, riverSurface, true);

                    ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), 1, out offsetX, out offsetZ);
                    ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.35 + 0.15, riverSurface, false);
                }
            }
        }

        private void AddRiverSeepageChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    if (hydrology < 0.58)
                    {
                        continue;
                    }

                    var flowVector = riverField.Flow[x, z];
                    if (flowVector.LengthSquared() < 1e-5f)
                    {
                        continue;
                    }

                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold || intensity >= RiverBankThreshold + 0.1)
                    {
                        continue;
                    }

                    if (!TryFindDownstreamRiverCell(riverIntensity, x, z, out int targetX, out int targetZ, out double targetIntensity))
                    {
                        continue;
                    }

                    if (targetIntensity >= intensity || targetIntensity > RiverCenterThreshold * 1.05)
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface))
                    {
                        continue;
                    }

                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    int riverSurface = Math.Min(surface, GlobalWaterLevel);
                    int depth = Math.Clamp((int)Math.Round(1 + (hydrology + flow) * 2.5), 1, 4);
                    bool flood = hydrology > 0.7 || flow > 0.6;

                    CarveRiverSeepagePath(chunk, surfaceCache, x, z, targetX, targetZ, riverSurface, depth, flood);
                }
            }
        }

        private static bool TryFindDownstreamRiverCell(
            double[,] riverIntensity,
            int x,
            int z,
            out int targetX,
            out int targetZ,
            out double targetIntensity)
        {
            var offsets = new (int dx, int dz)[]
            {
                (1, 0),
                (-1, 0),
                (0, 1),
                (0, -1)
            };

            targetX = -1;
            targetZ = -1;
            targetIntensity = double.MaxValue;

            foreach (var (dx, dz) in offsets)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= riverIntensity.GetLength(0) || nz < 0 || nz >= riverIntensity.GetLength(1))
                {
                    continue;
                }

                double sample = riverIntensity[nx, nz];
                if (sample < targetIntensity)
                {
                    targetIntensity = sample;
                    targetX = nx;
                    targetZ = nz;
                }
            }

            return targetX >= 0;
        }

        private void CarveRiverSeepagePath(
            ChunkData chunk,
            int[,] surfaceCache,
            int startX,
            int startZ,
            int endX,
            int endZ,
            int riverSurface,
            int depth,
            bool flood)
        {
            int steps = Math.Max(Math.Abs(endX - startX), Math.Abs(endZ - startZ));
            steps = Math.Clamp(steps, 1, 4);
            double stepX = (endX - startX) / (double)steps;
            double stepZ = (endZ - startZ) / (double)steps;
            double cursorX = startX;
            double cursorZ = startZ;

            for (int i = 0; i <= steps; i++)
            {
                int cx = Math.Clamp((int)Math.Round(cursorX), 0, 15);
                int cz = Math.Clamp((int)Math.Round(cursorZ), 0, 15);

                if (!TryResolveSurface(chunk, surfaceCache, cx, cz, out int surface))
                {
                    cursorX += stepX;
                    cursorZ += stepZ;
                    continue;
                }

                int floor = Math.Max(surface - depth, 1);
                for (int y = surface; y > floor; y--)
                {
                    chunk.SetBlock(cx, y, cz, BlockType.Air);
                }

                if (flood)
                {
                    chunk.SetBlock(cx, floor, cz, BlockType.Clay);
                    for (int y = floor + 1; y <= riverSurface && y < 256; y++)
                    {
                        chunk.SetBlock(cx, y, cz, BlockType.Water);
                    }
                    surfaceCache[cx, cz] = Math.Min(riverSurface, 255);
                }
                else
                {
                    chunk.SetBlock(cx, floor, cz, BlockType.Sand);
                    if (floor + 1 < 256)
                    {
                        chunk.SetBlock(cx, floor + 1, cz, BlockType.Air);
                    }
                    surfaceCache[cx, cz] = floor;
                }

                cursorX += stepX;
                cursorZ += stepZ;
            }
        }

        private static bool TrySampleField(double[,] field, int x, int z, out double value)
        {
            if (x < 0 || x >= field.GetLength(0) || z < 0 || z >= field.GetLength(1))
            {
                value = 0;
                return false;
            }

            value = field[x, z];
            return true;
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
        Cloud = 19,
        Clay = 20
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


        // ==================== Utility Methods ====================

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
