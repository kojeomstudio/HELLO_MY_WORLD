using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Coordinates improved terrain generation across all systems
    /// </summary>
    public class ImprovedTerrainCoordinator
    {
        private readonly ImprovedTerrainGenerator _terrainGenerator;
        private readonly ImprovedCaveGenerator _caveGenerator;
        private readonly ImprovedRiverGenerator _riverGenerator;
        private readonly ImprovedLakeGenerator _lakeGenerator;
        private readonly TerrainGenerationConfig _config;

        public ImprovedTerrainCoordinator(
            ImprovedTerrainGenerator terrainGenerator,
            ImprovedCaveGenerator caveGenerator,
            ImprovedRiverGenerator riverGenerator,
            ImprovedLakeGenerator lakeGenerator,
            TerrainGenerationConfig config)
        {
            _terrainGenerator = terrainGenerator ?? throw new ArgumentNullException(nameof(terrainGenerator));
            _caveGenerator = caveGenerator ?? throw new ArgumentNullException(nameof(caveGenerator));
            _riverGenerator = riverGenerator ?? throw new ArgumentNullException(nameof(riverGenerator));
            _lakeGenerator = lakeGenerator ?? throw new ArgumentNullException(nameof(lakeGenerator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Generate complete terrain for a chunk
        /// </summary>
        public async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ, long worldSeed)
        {
            var context = new TerrainGenerationContext
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                WorldSeed = worldSeed,
                ChunkSize = _config.ChunkSize,
                Height = _config.ChunkHeight,
                SeaLevel = _config.SeaLevel
            };

            // Generate base terrain
            context.HeightMap = await _terrainGenerator.GenerateHeightMapAsync(context);
            
            // Generate river mask
            context.RiverMask = await _riverGenerator.BuildMaskAsync(
                chunkX, chunkZ, _config.ChunkSize, context.HeightMap, _config.SeaLevel);
            
            // Generate lake mask
            context.LakeMask = await _lakeGenerator.BuildMaskAsync(
                chunkX, chunkZ, _config.ChunkSize, context.HeightMap, context.RiverMask, _config.SeaLevel);
            
            // Generate cave mask
            context.CaveMask = await _caveGenerator.BuildMaskAsync(
                _config.ChunkSize, _config.ChunkHeight, _config.ChunkSize, 
                context.HeightMap, _config.SeaLevel);
            
            // Apply all masks to create final terrain
            var chunkData = ApplyTerrainMasks(context);
            
            return chunkData;
        }

        /// <summary>
        /// Apply all terrain masks to create final chunk data
        /// </summary>
        private ChunkData ApplyTerrainMasks(TerrainGenerationContext context)
        {
            var chunkData = new ChunkData
            {
                ChunkX = context.ChunkX,
                ChunkZ = context.ChunkZ,
                Blocks = new int[context.ChunkSize, context.Height, context.ChunkSize]
            };

            // Apply base terrain
            for (int x = 0; x < context.ChunkSize; x++)
            {
                for (int z = 0; z < context.ChunkSize; z++)
                {
                    int terrainHeight = context.HeightMap[x, z];
                    
                    for (int y = 0; y < context.Height; y++)
                    {
                        // Apply cave mask
                        if (context.CaveMask[x, y, z])
                        {
                            chunkData.Blocks[x, y, z] = 0; // Air
                            continue;
                        }
                        
                        // Apply water/lake mask
                        if (y <= context.SeaLevel && context.LakeMask[x, z] > 0.5f)
                        {
                            chunkData.Blocks[x, y, z] = BlockType.Water;
                            continue;
                        }
                        
                        // Apply river mask
                        if (y <= context.SeaLevel && context.RiverMask[x, z] > 0.3f)
                        {
                            chunkData.Blocks[x, y, z] = BlockType.Water;
                            continue;
                        }
                        
                        // Generate terrain layers
                        if (y <= terrainHeight)
                        {
                            if (y == terrainHeight && y > context.SeaLevel)
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Grass;
                            }
                            else if (y > terrainHeight - 3 && y > context.SeaLevel)
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Dirt;
                            }
                            else
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Stone;
                            }
                        }
                        else
                        {
                            chunkData.Blocks[x, y, z] = 0; // Air
                        }
                    }
                }
            }

            return chunkData;
        }
    }

    /// <summary>
    /// Context for terrain generation operations
    /// </summary>
    public class TerrainGenerationContext
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public long WorldSeed { get; set; }
        public int ChunkSize { get; set; }
        public int Height { get; set; }
        public int SeaLevel { get; set; }
        public int[,] HeightMap { get; set; }
        public float[,] RiverMask { get; set; }
        public float[,] LakeMask { get; set; }
        public bool[,,] CaveMask { get; set; }
    }

    /// <summary>
    /// Generated chunk data
    /// </summary>
    public class ChunkData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public int[,,] Blocks { get; set; }
    }

    /// <summary>
    /// Block type constants
    /// </summary>
    public static class BlockType
    {
        public const int Air = 0;
        public const int Stone = 1;
        public const int Dirt = 2;
        public const int Grass = 3;
        public const int Water = 4;
        public const int Sand = 5;
    }
}
        /// Apply all terrain masks to create final chunk data
        /// </summary>
        private ChunkData ApplyTerrainMasks(TerrainGenerationContext context)
        {
            var chunkData = new ChunkData
            {
                ChunkX = context.ChunkX,
                ChunkZ = context.ChunkZ,
                Blocks = new int[context.ChunkSize, context.Height, context.ChunkSize]
            };

            // Apply base terrain
            for (int x = 0; x < context.ChunkSize; x++)
            {
                for (int z = 0; z < context.ChunkSize; z++)
                {
                    int terrainHeight = context.HeightMap[x, z];
                    
                    for (int y = 0; y < context.Height; y++)
                    {
                        // Apply cave mask
                        if (context.CaveMask[x, y, z])
                        {
                            chunkData.Blocks[x, y, z] = 0; // Air
                            continue;
                        }
                        
                        // Apply water/lake mask
                        if (y <= context.SeaLevel && context.LakeMask[x, z] > 0.5f)
                        {
                            chunkData.Blocks[x, y, z] = BlockType.Water;
                            continue;
                        }
                        
                        // Apply river mask
                        if (y <= context.SeaLevel && context.RiverMask[x, z] > 0.3f)
                        {
                            chunkData.Blocks[x, y, z] = BlockType.Water;
                            continue;
                        }
                        
                        // Generate terrain layers
                        if (y <= terrainHeight)
                        {
                            if (y == terrainHeight && y > context.SeaLevel)
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Grass;
                            }
                            else if (y > terrainHeight - 3 && y > context.SeaLevel)
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Dirt;
                            }
                            else
                            {
                                chunkData.Blocks[x, y, z] = BlockType.Stone;
                            }
                        }
                        else
                        {
                            chunkData.Blocks[x, y, z] = 0; // Air
                        }
                    }
                }
            }

            return chunkData;
        }
    }

    /// <summary>
    /// Context for terrain generation operations
    /// </summary>
    public class TerrainGenerationContext
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public long WorldSeed { get; set; }
        public int ChunkSize { get; set; }
        public int Height { get; set; }
        public int SeaLevel { get; set; }
        public int[,] HeightMap { get; set; }
        public float[,] RiverMask { get; set; }
        public float[,] LakeMask { get; set; }
        public bool[,,] CaveMask { get; set; }
    }

    /// <summary>
    /// Generated chunk data
    /// </summary>
    public class ChunkData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public int[,,] Blocks { get; set; }
    }

    /// <summary>
    /// Block type constants
    /// </summary>
    public static class BlockType
    {
        public const int Air = 0;
        public const int Stone = 1;
        public const int Dirt = 2;
        public const int Grass = 3;
        public const int Water = 4;
        public const int Sand = 5;
    }
}
}
        /// Finalize terrain with post-processing
        /// </summary>
        private void FinalizeTerrain(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            
            // Add final touches
            AddOreVeins(context);
            AddVegetation(context);
            AddStructures(context);
        }

        /// <summary>
        /// Determine biome at world position
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ)
        {
            // Use noise to determine biome
            double biomeNoise = SimplexNoise.Generate(
                worldX * 0.002,
                worldZ * 0.002,
                0.01,
                2,
                1.0,
                0.5,
                54321);
            
            // Map noise to biome
            if (biomeNoise < -0.3)
                return BiomeType.Ocean;
            else if (biomeNoise < -0.1)
                return BiomeType.Beach;
            else if (biomeNoise < 0.2)
                return BiomeType.Plains;
            else if (biomeNoise < 0.4)
                return BiomeType.Forest;
            else if (biomeNoise < 0.6)
                return BiomeType.Desert;
            else
                return BiomeType.Mountains;
        }

        /// <summary>
        /// Apply biome-specific height modifier
        /// </summary>
        private int ApplyBiomeHeightModifier(int baseHeight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => baseHeight - 20,
                BiomeType.Beach => baseHeight - 5,
                BiomeType.Plains => baseHeight,
                BiomeType.Forest => baseHeight + 5,
                BiomeType.Desert => baseHeight + 10,
                BiomeType.Mountains => baseHeight + 20,
                _ => baseHeight
            };
        }

        /// <summary>
        /// Determine surface block type
        /// </summary>
        private BlockType DetermineSurfaceBlock(TerrainGenerationContext context, int x, int z, 
            int surfaceHeight, float[,] riverMask, float[,] lakeMask)
        {
            var worldX = context.ChunkX * 16 + x;
            var worldZ = context.ChunkZ * 16 + z;
            var biome = DetermineBiome(worldX, worldZ);
            
            // Check if water/lake
            if (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f)
            {
                return BlockType.Water;
            }
            
            // Return biome-specific surface block
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Mountains => BlockType.Stone,
                _ => BlockType.Grass
            };
        }

        /// <summary>
        /// Determine sub-surface block type
        /// </summary>
        private BlockType DetermineSubSurfaceBlock(int y, int surfaceHeight)
        {
            int depth = surfaceHeight - y;
            
            if (depth <= 1)
                return BlockType.Dirt;
            else if (depth <= 3)
                return BlockType.Dirt;
            else
                return BlockType.Stone;
        }

        /// <summary>
        /// Smooth height map
        /// </summary>
        private void SmoothHeightMap(int[,] heightMap)
        {
            var smoothedMap = new int[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += heightMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Blend original and smoothed
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    heightMap[x, z] = (heightMap[x, z] + smoothedMap[x, z]) / 2;
                }
            }
        }

        /// <summary>
        /// Add ore veins to terrain
        /// </summary>
        private void AddOreVeins(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add coal veins
            if (random.NextDouble() < 0.7)
            {
                AddOreVein(chunk, random, BlockType.CoalOre, 5, 40);
            }
            
            // Add iron veins
            if (random.NextDouble() < 0.5)
            {
                AddOreVein(chunk, random, BlockType.IronOre, 10, 60);
            }
            
            // Add gold veins
            if (random.NextDouble() < 0.2)
            {
                AddOreVein(chunk, random, BlockType.GoldOre, 20, 40);
            }
        }

        /// <summary>
        /// Add a single ore vein
        /// </summary>
        private void AddOreVein(ChunkData chunk, Random random, BlockType oreType, int minY, int maxY)
        {
            int startX = random.Next(16);
            int startZ = random.Next(16);
            int startY = random.Next(minY, maxY);
            
            int veinLength = 5 + random.Next(10);
            double direction = random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < veinLength; i++)
            {
                int x = startX + (int)(Math.Cos(direction) * i);
                int z = startZ + (int)(Math.Sin(direction) * i);
                int y = startY + random.Next(-2, 2);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= minY && y < maxY)
                {
                    // Only replace stone with ore
                    if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                    {
                        chunk.SetBlock(x, y, z, oreType);
                    }
                }
            }
        }

        /// <summary>
        /// Add vegetation to terrain
        /// </summary>
        private void AddVegetation(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = context.ChunkX * 16 + x;
                    int worldZ = context.ChunkZ * 16 + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Find surface
                    int surfaceY = -1;
                    for (int y = 255; y >= 0; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY >= 0 && surfaceY + 1 < 256)
                    {
                        // Add biome-specific vegetation
                        AddBiomeVegetation(chunk, random, x, z, surfaceY, biome);
                    }
                }
            }
        }

        /// <summary>
        /// Add biome-specific vegetation
        /// </summary>
        private void AddBiomeVegetation(ChunkData chunk, Random random, int x, int z, int surfaceY, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    // Add trees
                    if (random.NextDouble() < 0.1)
                    {
                        AddTree(chunk, x, surfaceY + 1, z);
                    }
                    // Add tall grass
                    else if (random.NextDouble() < 0.3)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    break;
                    
                case BiomeType.Plains:
                    // Add grass and flowers
                    if (random.NextDouble() < 0.2)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    else if (random.NextDouble() < 0.05)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.Flower);
                    }
                    break;
                    
                case BiomeType.Desert:
                    // Add cacti
                    if (random.NextDouble() < 0.02)
                    {
                        AddCactus(chunk, x, surfaceY + 1, z, random);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add a tree
        /// </summary>
        private void AddTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple tree - trunk and leaves
            int treeHeight = 4 + new Random().Next(0, 3);
            
            // Trunk
            for (int i = 0; i < treeHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 3)
                        {
                            int lx = x + dx;
                            int ly = y + treeHeight + dy;
                            int lz = z + dz;
                            
                            if (lx >= 0 && lx < 16 && ly >= 0 && ly < 256 && lz >= 0 && lz < 16)
                            {
                                if (chunk.GetBlock(lx, ly, lz) == BlockType.Air)
                                {
                                    chunk.SetBlock(lx, ly, lz, BlockType.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cactus
        /// </summary>
        private void AddCactus(ChunkData chunk, int x, int y, int z, Random random)
        {
            int cactusHeight = 1 + random.Next(3);
            
            for (int i = 0; i < cactusHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }

        /// <summary>
        /// Add structures to terrain
        /// </summary>
        private void AddStructures(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add dungeons occasionally
            if (random.NextDouble() < 0.05)
            {
                AddDungeon(chunk, random);
            }
        }

        /// <summary>
        /// Add a dungeon
        /// </summary>
        private void AddDungeon(ChunkData chunk, Random random)
        {
            int dungeonX = 4 + random.Next(8);
            int dungeonZ = 4 + random.Next(8);
            int dungeonY = 10 + random.Next(30);
            
            int dungeonWidth = 5 + random.Next(4);
            int dungeonHeight = 4 + random.Next(3);
            int dungeonDepth = 5 + random.Next(5);
            
            // Carve out dungeon space
            for (int x = dungeonX; x < dungeonX + dungeonWidth && x < 16; x++)
            {
                for (int z = dungeonZ; z < dungeonZ + dungeonDepth && z < 16; z++)
                {
                    for (int y = dungeonY; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            // Add walls and floor
            for (int x = dungeonX - 1; x < dungeonX + dungeonWidth + 1 && x < 16; x++)
            {
                for (int z = dungeonZ - 1; z < dungeonZ + dungeonDepth + 1 && z < 16; z++)
                {
                    // Floor
                    chunk.SetBlock(x, dungeonY, z, BlockType.Cobblestone);
                    
                    // Walls
                    for (int y = dungeonY + 1; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        // Only place walls where there's air
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Cobblestone);
                        }
                    }
                }
            }
        }
    }
}
                    // Generate base height using noise
                    double heightNoise = SimplexNoise.Generate(
                        worldX * 0.005,
                        worldZ * 0.005,
                        0.01,
                        4,
                        1.0,
                        0.5,
                        12345);
                    
                    // Apply height modifiers
                    int baseHeight = 60 + (int)(heightNoise * 40);
                    
                    // Apply biome-specific height modifications
                    var biome = DetermineBiome(worldX, worldZ);
                    baseHeight = ApplyBiomeHeightModifier(baseHeight, biome);
                    
                    heightMap[x, z] = Math.Clamp(baseHeight, 5, 120);
                }
            }
            
            // Smooth height map
            SmoothHeightMap(heightMap);
            
            return heightMap;
        }

        /// <summary>
        /// Generate cave mask
        /// </summary>
        private bool[,,] GenerateCaveMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var caveGenerator = new ImprovedCaveGenerator(_caveConfig, worldSeed);
            return caveGenerator.BuildMask(16, 16, 256, heightMap, 62);
        }

        /// <summary>
        /// Generate river mask
        /// </summary>
        private float[,] GenerateRiverMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var riverGenerator = new ImprovedRiverGenerator(_waterConfig, worldSeed);
            return riverGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, 62);
        }

        /// <summary>
        /// Generate lake mask
        /// </summary>
        private float[,] GenerateLakeMask(TerrainGenerationContext context, int[,] heightMap, float[,] riverMask)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var lakeGenerator = new ImprovedLakeGenerator(_lakeConfig, worldSeed);
            return lakeGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, riverMask, 62);
        }

        /// <summary>
        /// Apply terrain features based on masks
        /// </summary>
        private void ApplyTerrainFeatures(TerrainGenerationContext context, int[,] heightMap, 
            bool[,,] caveMask, float[,] riverMask, float[,] lakeMask)
        {
            var chunk = context.Chunk;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surfaceHeight = heightMap[x, z];
                    
                    // Apply terrain from bottom up
                    for (int y = 0; y < 256; y++)
                    {
                        if (y > surfaceHeight)
                        {
                            // Above surface - air or water
                            if (y <= 62 && (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f))
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            else
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                        else if (y == surfaceHeight)
                        {
                            // Surface layer
                            BlockType surfaceBlock = DetermineSurfaceBlock(context, x, z, surfaceHeight, riverMask, lakeMask);
                            chunk.SetBlock(x, y, z, surfaceBlock);
                        }
                        else
                        {
                            // Below surface - stone or dirt
                            BlockType subSurfaceBlock = DetermineSubSurfaceBlock(y, surfaceHeight);
                            chunk.SetBlock(x, y, z, subSurfaceBlock);
                        }
                    }
                    
                    // Apply caves (carve out air/water)
                    if (y < 256 && x < 16 && z < 16 && caveMask[x, y, z])
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize terrain with post-processing
        /// </summary>
        private void FinalizeTerrain(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            
            // Add final touches
            AddOreVeins(context);
            AddVegetation(context);
            AddStructures(context);
        }

        /// <summary>
        /// Determine biome at world position
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ)
        {
            // Use noise to determine biome
            double biomeNoise = SimplexNoise.Generate(
                worldX * 0.002,
                worldZ * 0.002,
                0.01,
                2,
                1.0,
                0.5,
                54321);
            
            // Map noise to biome
            if (biomeNoise < -0.3)
                return BiomeType.Ocean;
            else if (biomeNoise < -0.1)
                return BiomeType.Beach;
            else if (biomeNoise < 0.2)
                return BiomeType.Plains;
            else if (biomeNoise < 0.4)
                return BiomeType.Forest;
            else if (biomeNoise < 0.6)
                return BiomeType.Desert;
            else
                return BiomeType.Mountains;
        }

        /// <summary>
        /// Apply biome-specific height modifier
        /// </summary>
        private int ApplyBiomeHeightModifier(int baseHeight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => baseHeight - 20,
                BiomeType.Beach => baseHeight - 5,
                BiomeType.Plains => baseHeight,
                BiomeType.Forest => baseHeight + 5,
                BiomeType.Desert => baseHeight + 10,
                BiomeType.Mountains => baseHeight + 20,
                _ => baseHeight
            };
        }

        /// <summary>
        /// Determine surface block type
        /// </summary>
        private BlockType DetermineSurfaceBlock(TerrainGenerationContext context, int x, int z, 
            int surfaceHeight, float[,] riverMask, float[,] lakeMask)
        {
            var worldX = context.ChunkX * 16 + x;
            var worldZ = context.ChunkZ * 16 + z;
            var biome = DetermineBiome(worldX, worldZ);
            
            // Check if water/lake
            if (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f)
            {
                return BlockType.Water;
            }
            
            // Return biome-specific surface block
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Mountains => BlockType.Stone,
                _ => BlockType.Grass
            };
        }

        /// <summary>
        /// Determine sub-surface block type
        /// </summary>
        private BlockType DetermineSubSurfaceBlock(int y, int surfaceHeight)
        {
            int depth = surfaceHeight - y;
            
            if (depth <= 1)
                return BlockType.Dirt;
            else if (depth <= 3)
                return BlockType.Dirt;
            else
                return BlockType.Stone;
        }

        /// <summary>
        /// Smooth height map
        /// </summary>
        private void SmoothHeightMap(int[,] heightMap)
        {
            var smoothedMap = new int[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += heightMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Blend original and smoothed
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    heightMap[x, z] = (heightMap[x, z] + smoothedMap[x, z]) / 2;
                }
            }
        }

        /// <summary>
        /// Add ore veins to terrain
        /// </summary>
        private void AddOreVeins(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add coal veins
            if (random.NextDouble() < 0.7)
            {
                AddOreVein(chunk, random, BlockType.CoalOre, 5, 40);
            }
            
            // Add iron veins
            if (random.NextDouble() < 0.5)
            {
                AddOreVein(chunk, random, BlockType.IronOre, 10, 60);
            }
            
            // Add gold veins
            if (random.NextDouble() < 0.2)
            {
                AddOreVein(chunk, random, BlockType.GoldOre, 20, 40);
            }
        }

        /// <summary>
        /// Add a single ore vein
        /// </summary>
        private void AddOreVein(ChunkData chunk, Random random, BlockType oreType, int minY, int maxY)
        {
            int startX = random.Next(16);
            int startZ = random.Next(16);
            int startY = random.Next(minY, maxY);
            
            int veinLength = 5 + random.Next(10);
            double direction = random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < veinLength; i++)
            {
                int x = startX + (int)(Math.Cos(direction) * i);
                int z = startZ + (int)(Math.Sin(direction) * i);
                int y = startY + random.Next(-2, 2);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= minY && y < maxY)
                {
                    // Only replace stone with ore
                    if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                    {
                        chunk.SetBlock(x, y, z, oreType);
                    }
                }
            }
        }

        /// <summary>
        /// Add vegetation to terrain
        /// </summary>
        private void AddVegetation(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = context.ChunkX * 16 + x;
                    int worldZ = context.ChunkZ * 16 + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Find surface
                    int surfaceY = -1;
                    for (int y = 255; y >= 0; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY >= 0 && surfaceY + 1 < 256)
                    {
                        // Add biome-specific vegetation
                        AddBiomeVegetation(chunk, random, x, z, surfaceY, biome);
                    }
                }
            }
        }

        /// <summary>
        /// Add biome-specific vegetation
        /// </summary>
        private void AddBiomeVegetation(ChunkData chunk, Random random, int x, int z, int surfaceY, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    // Add trees
                    if (random.NextDouble() < 0.1)
                    {
                        AddTree(chunk, x, surfaceY + 1, z);
                    }
                    // Add tall grass
                    else if (random.NextDouble() < 0.3)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    break;
                    
                case BiomeType.Plains:
                    // Add grass and flowers
                    if (random.NextDouble() < 0.2)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    else if (random.NextDouble() < 0.05)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.Flower);
                    }
                    break;
                    
                case BiomeType.Desert:
                    // Add cacti
                    if (random.NextDouble() < 0.02)
                    {
                        AddCactus(chunk, x, surfaceY + 1, z, random);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add a tree
        /// </summary>
        private void AddTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple tree - trunk and leaves
            int treeHeight = 4 + new Random().Next(0, 3);
            
            // Trunk
            for (int i = 0; i < treeHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 3)
                        {
                            int lx = x + dx;
                            int ly = y + treeHeight + dy;
                            int lz = z + dz;
                            
                            if (lx >= 0 && lx < 16 && ly >= 0 && ly < 256 && lz >= 0 && lz < 16)
                            {
                                if (chunk.GetBlock(lx, ly, lz) == BlockType.Air)
                                {
                                    chunk.SetBlock(lx, ly, lz, BlockType.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cactus
        /// </summary>
        private void AddCactus(ChunkData chunk, int x, int y, int z, Random random)
        {
            int cactusHeight = 1 + random.Next(3);
            
            for (int i = 0; i < cactusHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }

        /// <summary>
        /// Add structures to terrain
        /// </summary>
        private void AddStructures(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add dungeons occasionally
            if (random.NextDouble() < 0.05)
            {
                AddDungeon(chunk, random);
            }
        }

        /// <summary>
        /// Add a dungeon
        /// </summary>
        private void AddDungeon(ChunkData chunk, Random random)
        {
            int dungeonX = 4 + random.Next(8);
            int dungeonZ = 4 + random.Next(8);
            int dungeonY = 10 + random.Next(30);
            
            int dungeonWidth = 5 + random.Next(4);
            int dungeonHeight = 4 + random.Next(3);
            int dungeonDepth = 5 + random.Next(5);
            
            // Carve out dungeon space
            for (int x = dungeonX; x < dungeonX + dungeonWidth && x < 16; x++)
            {
                for (int z = dungeonZ; z < dungeonZ + dungeonDepth && z < 16; z++)
                {
                    for (int y = dungeonY; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            // Add walls and floor
            for (int x = dungeonX - 1; x < dungeonX + dungeonWidth + 1 && x < 16; x++)
            {
                for (int z = dungeonZ - 1; z < dungeonZ + dungeonDepth + 1 && z < 16; z++)
                {
                    // Floor
                    chunk.SetBlock(x, dungeonY, z, BlockType.Cobblestone);
                    
                    // Walls
                    for (int y = dungeonY + 1; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        // Only place walls where there's air
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Cobblestone);
                        }
                    }
                }
            }
        }
    }
}
}
                        worldX * 0.005,
                        worldZ * 0.005,
                        0.01,
                        4,
                        1.0,
                        0.5,
                        12345);
                    
                    // Apply height modifiers
                    int baseHeight = 60 + (int)(heightNoise * 40);
                    
                    // Apply biome-specific height modifications
                    var biome = DetermineBiome(worldX, worldZ);
                    baseHeight = ApplyBiomeHeightModifier(baseHeight, biome);
                    
                    heightMap[x, z] = Math.Clamp(baseHeight, 5, 120);
                }
            }
            
            // Smooth height map
            SmoothHeightMap(heightMap);
            
            return heightMap;
        }

        /// <summary>
        /// Generate cave mask
        /// </summary>
        private bool[,,] GenerateCaveMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var caveGenerator = new ImprovedCaveGenerator(_caveConfig, worldSeed);
            return caveGenerator.BuildMask(16, 16, 256, heightMap, 62);
        }

        /// <summary>
        /// Generate river mask
        /// </summary>
        private float[,] GenerateRiverMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var riverGenerator = new ImprovedRiverGenerator(_waterConfig, worldSeed);
            return riverGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, 62);
        }

        /// <summary>
        /// Generate lake mask
        /// </summary>
        private float[,] GenerateLakeMask(TerrainGenerationContext context, int[,] heightMap, float[,] riverMask)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var lakeGenerator = new ImprovedLakeGenerator(_lakeConfig, worldSeed);
            return lakeGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, riverMask, 62);
        }

        /// <summary>
        /// Apply terrain features based on masks
        /// </summary>
        private void ApplyTerrainFeatures(TerrainGenerationContext context, int[,] heightMap, 
            bool[,,] caveMask, float[,] riverMask, float[,] lakeMask)
        {
            var chunk = context.Chunk;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surfaceHeight = heightMap[x, z];
                    
                    // Apply terrain from bottom up
                    for (int y = 0; y < 256; y++)
                    {
                        if (y > surfaceHeight)
                        {
                            // Above surface - air or water
                            if (y <= 62 && (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f))
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            else
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                        else if (y == surfaceHeight)
                        {
                            // Surface layer
                            BlockType surfaceBlock = DetermineSurfaceBlock(context, x, z, surfaceHeight, riverMask, lakeMask);
                            chunk.SetBlock(x, y, z, surfaceBlock);
                        }
                        else
                        {
                            // Below surface - stone or dirt
                            BlockType subSurfaceBlock = DetermineSubSurfaceBlock(y, surfaceHeight);
                            chunk.SetBlock(x, y, z, subSurfaceBlock);
                        }
                    }
                    
                    // Apply caves (carve out air/water)
                    if (caveMask[x, y, z])
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize terrain with post-processing
        /// </summary>
        private void FinalizeTerrain(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            
            // Add final touches
            AddOreVeins(context);
            AddVegetation(context);
            AddStructures(context);
        }

        /// <summary>
        /// Determine biome at world position
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ)
        {
            // Use noise to determine biome
            double biomeNoise = SimplexNoise.Generate(
                worldX * 0.002,
                worldZ * 0.002,
                0.01,
                2,
                1.0,
                0.5,
                54321);
            
            // Map noise to biome
            if (biomeNoise < -0.3)
                return BiomeType.Ocean;
            else if (biomeNoise < -0.1)
                return BiomeType.Beach;
            else if (biomeNoise < 0.2)
                return BiomeType.Plains;
            else if (biomeNoise < 0.4)
                return BiomeType.Forest;
            else if (biomeNoise < 0.6)
                return BiomeType.Desert;
            else
                return BiomeType.Mountains;
        }

        /// <summary>
        /// Apply biome-specific height modifier
        /// </summary>
        private int ApplyBiomeHeightModifier(int baseHeight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => baseHeight - 20,
                BiomeType.Beach => baseHeight - 5,
                BiomeType.Plains => baseHeight,
                BiomeType.Forest => baseHeight + 5,
                BiomeType.Desert => baseHeight + 10,
                BiomeType.Mountains => baseHeight + 20,
                _ => baseHeight
            };
        }

        /// <summary>
        /// Determine surface block type
        /// </summary>
        private BlockType DetermineSurfaceBlock(TerrainGenerationContext context, int x, int z, 
            int surfaceHeight, float[,] riverMask, float[,] lakeMask)
        {
            var worldX = context.ChunkX * 16 + x;
            var worldZ = context.ChunkZ * 16 + z;
            var biome = DetermineBiome(worldX, worldZ);
            
            // Check if water/lake
            if (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f)
            {
                return BlockType.Water;
            }
            
            // Return biome-specific surface block
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Mountains => BlockType.Stone,
                _ => BlockType.Grass
            };
        }

        /// <summary>
        /// Determine sub-surface block type
        /// </summary>
        private BlockType DetermineSubSurfaceBlock(int y, int surfaceHeight)
        {
            int depth = surfaceHeight - y;
            
            if (depth <= 1)
                return BlockType.Dirt;
            else if (depth <= 3)
                return BlockType.Dirt;
            else
                return BlockType.Stone;
        }

        /// <summary>
        /// Smooth height map
        /// </summary>
        private void SmoothHeightMap(int[,] heightMap)
        {
            var smoothedMap = new int[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += heightMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Blend original and smoothed
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    heightMap[x, z] = (heightMap[x, z] + smoothedMap[x, z]) / 2;
                }
            }
        }

        /// <summary>
        /// Add ore veins to the terrain
        /// </summary>
        private void AddOreVeins(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add coal veins
            if (random.NextDouble() < 0.7)
            {
                AddOreVein(chunk, random, BlockType.CoalOre, 5, 40);
            }
            
            // Add iron veins
            if (random.NextDouble() < 0.5)
            {
                AddOreVein(chunk, random, BlockType.IronOre, 10, 60);
            }
            
            // Add gold veins
            if (random.NextDouble() < 0.2)
            {
                AddOreVein(chunk, random, BlockType.GoldOre, 20, 40);
            }
        }

        /// <summary>
        /// Add a single ore vein
        /// </summary>
        private void AddOreVein(ChunkData chunk, Random random, BlockType oreType, int minY, int maxY)
        {
            int startX = random.Next(16);
            int startZ = random.Next(16);
            int startY = random.Next(minY, maxY);
            
            int veinLength = 5 + random.Next(10);
            double direction = random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < veinLength; i++)
            {
                int x = startX + (int)(Math.Cos(direction) * i);
                int z = startZ + (int)(Math.Sin(direction) * i);
                int y = startY + random.Next(-2, 2);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= minY && y < maxY)
                {
                    // Only replace stone with ore
                    if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                    {
                        chunk.SetBlock(x, y, z, oreType);
                    }
                }
            }
        }

        /// <summary>
        /// Add vegetation to the terrain
        /// </summary>
        private void AddVegetation(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = context.ChunkX * 16 + x;
                    int worldZ = context.ChunkZ * 16 + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Find surface
                    int surfaceY = -1;
                    for (int y = 255; y >= 0; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY >= 0 && surfaceY + 1 < 256)
                    {
                        // Add biome-specific vegetation
                        AddBiomeVegetation(chunk, random, x, z, surfaceY, biome);
                    }
                }
            }
        }

        /// <summary>
        /// Add biome-specific vegetation
        /// </summary>
        private void AddBiomeVegetation(ChunkData chunk, Random random, int x, int z, int surfaceY, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    // Add trees
                    if (random.NextDouble() < 0.1)
                    {
                        AddTree(chunk, x, surfaceY + 1, z);
                    }
                    // Add tall grass
                    else if (random.NextDouble() < 0.3)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    break;
                    
                case BiomeType.Plains:
                    // Add grass and flowers
                    if (random.NextDouble() < 0.2)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    else if (random.NextDouble() < 0.05)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.Flower);
                    }
                    break;
                    
                case BiomeType.Desert:
                    // Add cacti
                    if (random.NextDouble() < 0.02)
                    {
                        AddCactus(chunk, x, surfaceY + 1, z, random);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add a tree
        /// </summary>
        private void AddTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple tree - trunk and leaves
            int treeHeight = 4 + UnityEngine.Random.Range(0, 3);
            
            // Trunk
            for (int i = 0; i < treeHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 3)
                        {
                            int lx = x + dx;
                            int ly = y + treeHeight + dy;
                            int lz = z + dz;
                            
                            if (lx >= 0 && lx < 16 && ly >= 0 && ly < 256 && lz >= 0 && lz < 16)
                            {
                                if (chunk.GetBlock(lx, ly, lz) == BlockType.Air)
                                {
                                    chunk.SetBlock(lx, ly, lz, BlockType.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cactus
        /// </summary>
        private void AddCactus(ChunkData chunk, int x, int y, int z, Random random)
        {
            int cactusHeight = 1 + random.Next(3);
            
            for (int i = 0; i < cactusHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }

        /// <summary>
        /// Add structures to the terrain
        /// </summary>
        private void AddStructures(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add dungeons occasionally
            if (random.NextDouble() < 0.05)
            {
                AddDungeon(chunk, random);
            }
        }

        /// <summary>
        /// Add a dungeon
        /// </summary>
        private void AddDungeon(ChunkData chunk, Random random)
        {
            int dungeonX = 4 + random.Next(8);
            int dungeonZ = 4 + random.Next(8);
            int dungeonY = 10 + random.Next(30);
            
            int dungeonWidth = 5 + random.Next(4);
            int dungeonHeight = 4 + random.Next(3);
            int dungeonDepth = 5 + random.Next(5);
            
            // Carve out dungeon space
            for (int x = dungeonX; x < dungeonX + dungeonWidth && x < 16; x++)
            {
                for (int z = dungeonZ; z < dungeonZ + dungeonDepth && z < 16; z++)
                {
                    for (int y = dungeonY; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            // Add walls and floor
            for (int x = dungeonX - 1; x < dungeonX + dungeonWidth + 1 && x < 16; x++)
            {
                for (int z = dungeonZ - 1; z < dungeonZ + dungeonDepth + 1 && z < 16; z++)
                {
                    // Floor
                    chunk.SetBlock(x, dungeonY, z, BlockType.Cobblestone);
                    
                    // Walls
                    for (int y = dungeonY + 1; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        // Only place walls where there's air
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Cobblestone);
                        }
                    }
                }
            }
        }
    }
}
}
                    // Generate base height using noise
                    double heightNoise = SimplexNoise.Generate(
                        worldX * 0.005,
                        worldZ * 0.005,
                        0.01,
                        4,
                        1.0,
                        0.5,
                        12345);
                    
                    // Apply height modifiers
                    int baseHeight = 60 + (int)(heightNoise * 40);
                    
                    // Apply biome-specific height modifications
                    var biome = DetermineBiome(worldX, worldZ);
                    baseHeight = ApplyBiomeHeightModifier(baseHeight, biome);
                    
                    heightMap[x, z] = Math.Clamp(baseHeight, 5, 120);
                }
            }
            
            // Smooth height map
            SmoothHeightMap(heightMap);
            
            return heightMap;
        }

        /// <summary>
        /// Generate cave mask
        /// </summary>
        private bool[,,] GenerateCaveMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var caveGenerator = new ImprovedCaveGenerator(_caveConfig, worldSeed);
            return caveGenerator.BuildMask(16, 16, 256, heightMap, 62);
        }

        /// <summary>
        /// Generate river mask
        /// </summary>
        private float[,] GenerateRiverMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var riverGenerator = new ImprovedRiverGenerator(_waterConfig, worldSeed);
            return riverGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, 62);
        }

        /// <summary>
        /// Generate lake mask
        /// </summary>
        private float[,] GenerateLakeMask(TerrainGenerationContext context, int[,] heightMap, float[,] riverMask)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var lakeGenerator = new ImprovedLakeGenerator(_lakeConfig, worldSeed);
            return lakeGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, riverMask, 62);
        }

        /// <summary>
        /// Apply terrain features based on masks
        /// </summary>
        private void ApplyTerrainFeatures(TerrainGenerationContext context, int[,] heightMap, 
            bool[,,] caveMask, float[,] riverMask, float[,] lakeMask)
        {
            var chunk = context.Chunk;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surfaceHeight = heightMap[x, z];
                    
                    // Apply terrain from bottom up
                    for (int y = 0; y < 256; y++)
                    {
                        if (y > surfaceHeight)
                        {
                            // Above surface - air or water
                            if (y <= 62 && (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f))
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            else
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                        else if (y == surfaceHeight)
                        {
                            // Surface layer
                            BlockType surfaceBlock = DetermineSurfaceBlock(context, x, z, surfaceHeight, riverMask, lakeMask);
                            chunk.SetBlock(x, y, z, surfaceBlock);
                        }
                        else
                        {
                            // Below surface - stone or dirt
                            BlockType subSurfaceBlock = DetermineSubSurfaceBlock(y, surfaceHeight);
                            chunk.SetBlock(x, y, z, subSurfaceBlock);
                        }
                    }
                    
                    // Apply caves (carve out air/water)
                    if (caveMask[x, y, z])
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize terrain with post-processing
        /// </summary>
        private void FinalizeTerrain(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            
            // Add final touches
            AddOreVeins(context);
            AddVegetation(context);
            AddStructures(context);
        }

        /// <summary>
        /// Determine biome at world position
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ)
        {
            // Use noise to determine biome
            double biomeNoise = SimplexNoise.Generate(
                worldX * 0.002,
                worldZ * 0.002,
                0.01,
                2,
                1.0,
                0.5,
                54321);
            
            // Map noise to biome
            if (biomeNoise < -0.3)
                return BiomeType.Ocean;
            else if (biomeNoise < -0.1)
                return BiomeType.Beach;
            else if (biomeNoise < 0.2)
                return BiomeType.Plains;
            else if (biomeNoise < 0.4)
                return BiomeType.Forest;
            else if (biomeNoise < 0.6)
                return BiomeType.Desert;
            else
                return BiomeType.Mountains;
        }

        /// <summary>
        /// Apply biome-specific height modifier
        /// </summary>
        private int ApplyBiomeHeightModifier(int baseHeight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => baseHeight - 20,
                BiomeType.Beach => baseHeight - 5,
                BiomeType.Plains => baseHeight,
                BiomeType.Forest => baseHeight + 5,
                BiomeType.Desert => baseHeight + 10,
                BiomeType.Mountains => baseHeight + 20,
                _ => baseHeight
            };
        }

        /// <summary>
        /// Determine surface block type
        /// </summary>
        private BlockType DetermineSurfaceBlock(TerrainGenerationContext context, int x, int z, 
            int surfaceHeight, float[,] riverMask, float[,] lakeMask)
        {
            var worldX = context.ChunkX * 16 + x;
            var worldZ = context.ChunkZ * 16 + z;
            var biome = DetermineBiome(worldX, worldZ);
            
            // Check if water/lake
            if (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f)
            {
                return BlockType.Water;
            }
            
            // Return biome-specific surface block
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Mountains => BlockType.Stone,
                _ => BlockType.Grass
            };
        }

        /// <summary>
        /// Determine sub-surface block type
        /// </summary>
        private BlockType DetermineSubSurfaceBlock(int y, int surfaceHeight)
        {
            int depth = surfaceHeight - y;
            
            if (depth <= 1)
                return BlockType.Dirt;
            else if (depth <= 3)
                return BlockType.Dirt;
            else
                return BlockType.Stone;
        }

        /// <summary>
        /// Smooth height map
        /// </summary>
        private void SmoothHeightMap(int[,] heightMap)
        {
            var smoothedMap = new int[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += heightMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Blend original and smoothed
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    heightMap[x, z] = (heightMap[x, z] + smoothedMap[x, z]) / 2;
                }
            }
        }

        /// <summary>
        /// Add ore veins to the terrain
        /// </summary>
        private void AddOreVeins(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add coal veins
            if (random.NextDouble() < 0.7)
            {
                AddOreVein(chunk, random, BlockType.CoalOre, 5, 40);
            }
            
            // Add iron veins
            if (random.NextDouble() < 0.5)
            {
                AddOreVein(chunk, random, BlockType.IronOre, 10, 60);
            }
            
            // Add gold veins
            if (random.NextDouble() < 0.2)
            {
                AddOreVein(chunk, random, BlockType.GoldOre, 20, 40);
            }
        }

        /// <summary>
        /// Add a single ore vein
        /// </summary>
        private void AddOreVein(ChunkData chunk, Random random, BlockType oreType, int minY, int maxY)
        {
            int startX = random.Next(16);
            int startZ = random.Next(16);
            int startY = random.Next(minY, maxY);
            
            int veinLength = 5 + random.Next(10);
            double direction = random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < veinLength; i++)
            {
                int x = startX + (int)(Math.Cos(direction) * i);
                int z = startZ + (int)(Math.Sin(direction) * i);
                int y = startY + random.Next(-2, 2);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= minY && y < maxY)
                {
                    // Only replace stone with ore
                    if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                    {
                        chunk.SetBlock(x, y, z, oreType);
                    }
                }
            }
        }

        /// <summary>
        /// Add vegetation to the terrain
        /// </summary>
        private void AddVegetation(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = context.ChunkX * 16 + x;
                    int worldZ = context.ChunkZ * 16 + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Find surface
                    int surfaceY = -1;
                    for (int y = 255; y >= 0; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY >= 0 && surfaceY + 1 < 256)
                    {
                        // Add biome-specific vegetation
                        AddBiomeVegetation(chunk, random, x, z, surfaceY, biome);
                    }
                }
            }
        }

        /// <summary>
        /// Add biome-specific vegetation
        /// </summary>
        private void AddBiomeVegetation(ChunkData chunk, Random random, int x, int z, int surfaceY, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    // Add trees
                    if (random.NextDouble() < 0.1)
                    {
                        AddTree(chunk, x, surfaceY + 1, z);
                    }
                    // Add tall grass
                    else if (random.NextDouble() < 0.3)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    break;
                    
                case BiomeType.Plains:
                    // Add grass and flowers
                    if (random.NextDouble() < 0.2)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    else if (random.NextDouble() < 0.05)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.Flower);
                    }
                    break;
                    
                case BiomeType.Desert:
                    // Add cacti
                    if (random.NextDouble() < 0.02)
                    {
                        AddCactus(chunk, x, surfaceY + 1, z, random);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add a tree
        /// </summary>
        private void AddTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple tree - trunk and leaves
            int treeHeight = 4 + UnityEngine.Random.Range(0, 3);
            
            // Trunk
            for (int i = 0; i < treeHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 3)
                        {
                            int lx = x + dx;
                            int ly = y + treeHeight + dy;
                            int lz = z + dz;
                            
                            if (lx >= 0 && lx < 16 && ly >= 0 && ly < 256 && lz >= 0 && lz < 16)
                            {
                                if (chunk.GetBlock(lx, ly, lz) == BlockType.Air)
                                {
                                    chunk.SetBlock(lx, ly, lz, BlockType.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cactus
        /// </summary>
        private void AddCactus(ChunkData chunk, int x, int y, int z, Random random)
        {
            int cactusHeight = 1 + random.Next(3);
            
            for (int i = 0; i < cactusHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }

        /// <summary>
        /// Add structures to the terrain
        /// </summary>
        private void AddStructures(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add dungeons occasionally
            if (random.NextDouble() < 0.05)
            {
                AddDungeon(chunk, random);
            }
        }

        /// <summary>
        /// Add a dungeon
        /// </summary>
        private void AddDungeon(ChunkData chunk, Random random)
        {
            int dungeonX = 4 + random.Next(8);
            int dungeonZ = 4 + random.Next(8);
            int dungeonY = 10 + random.Next(30);
            
            int dungeonWidth = 5 + random.Next(4);
            int dungeonHeight = 4 + random.Next(3);
            int dungeonDepth = 5 + random.Next(5);
            
            // Carve out dungeon space
            for (int x = dungeonX; x < dungeonX + dungeonWidth && x < 16; x++)
            {
                for (int z = dungeonZ; z < dungeonZ + dungeonDepth && z < 16; z++)
                {
                    for (int y = dungeonY; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            // Add walls and floor
            for (int x = dungeonX - 1; x < dungeonX + dungeonWidth + 1 && x < 16; x++)
            {
                for (int z = dungeonZ - 1; z < dungeonZ + dungeonDepth + 1 && z < 16; z++)
                {
                    // Floor
                    chunk.SetBlock(x, dungeonY, z, BlockType.Cobblestone);
                    
                    // Walls
                    for (int y = dungeonY + 1; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        // Only place walls where there's air
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Cobblestone);
                        }
                    }
                }
            }
        }
    }
}
                    double heightNoise = SimplexNoise.Generate(
                        worldX * 0.005,
                        worldZ * 0.005,
                        0.01,
                        4,
                        1.0,
                        0.5,
                        12345);
                    
                    // Apply height modifiers
                    int baseHeight = 60 + (int)(heightNoise * 40);
                    
                    // Apply biome-specific height modifications
                    var biome = DetermineBiome(worldX, worldZ);
                    baseHeight = ApplyBiomeHeightModifier(baseHeight, biome);
                    
                    heightMap[x, z] = Math.Clamp(baseHeight, 5, 120);
                }
            }
            
            // Smooth height map
            SmoothHeightMap(heightMap);
            
            return heightMap;
        }

        /// <summary>
        /// Generate cave mask
        /// </summary>
        private bool[,,] GenerateCaveMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var caveGenerator = new ImprovedCaveGenerator(_caveConfig, worldSeed);
            return caveGenerator.BuildMask(16, 16, 256, heightMap, 62);
        }

        /// <summary>
        /// Generate river mask
        /// </summary>
        private float[,] GenerateRiverMask(TerrainGenerationContext context, int[,] heightMap)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var riverGenerator = new ImprovedRiverGenerator(_waterConfig, worldSeed);
            return riverGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, 62);
        }

        /// <summary>
        /// Generate lake mask
        /// </summary>
        private float[,] GenerateLakeMask(TerrainGenerationContext context, int[,] heightMap, float[,] riverMask)
        {
            var worldSeed = context.WorldManager.GetWorldSeed().Seed;
            var lakeGenerator = new ImprovedLakeGenerator(_lakeConfig, worldSeed);
            return lakeGenerator.BuildMask(context.ChunkX, context.ChunkZ, 16, heightMap, riverMask, 62);
        }

        /// <summary>
        /// Apply terrain features based on masks
        /// </summary>
        private void ApplyTerrainFeatures(TerrainGenerationContext context, int[,] heightMap, 
            bool[,,] caveMask, float[,] riverMask, float[,] lakeMask)
        {
            var chunk = context.Chunk;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surfaceHeight = heightMap[x, z];
                    
                    // Apply terrain from bottom up
                    for (int y = 0; y < 256; y++)
                    {
                        if (y > surfaceHeight)
                        {
                            // Above surface - air or water
                            if (y <= 62 && (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f))
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            else
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                        else if (y == surfaceHeight)
                        {
                            // Surface layer
                            BlockType surfaceBlock = DetermineSurfaceBlock(context, x, z, surfaceHeight, riverMask, lakeMask);
                            chunk.SetBlock(x, y, z, surfaceBlock);
                        }
                        else
                        {
                            // Below surface - stone or dirt
                            BlockType subSurfaceBlock = DetermineSubSurfaceBlock(y, surfaceHeight);
                            chunk.SetBlock(x, y, z, subSurfaceBlock);
                        }
                    }
                    
                    // Apply caves (carve out air/water)
                    if (caveMask[x, y, z])
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        /// <summary>
        /// Finalize terrain with post-processing
        /// </summary>
        private void FinalizeTerrain(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            
            // Add final touches
            AddOreVeins(context);
            AddVegetation(context);
            AddStructures(context);
        }

        /// <summary>
        /// Determine biome at world position
        /// </summary>
        private BiomeType DetermineBiome(int worldX, int worldZ)
        {
            // Use noise to determine biome
            double biomeNoise = SimplexNoise.Generate(
                worldX * 0.002,
                worldZ * 0.002,
                0.01,
                2,
                1.0,
                0.5,
                54321);
            
            // Map noise to biome
            if (biomeNoise < -0.3)
                return BiomeType.Ocean;
            else if (biomeNoise < -0.1)
                return BiomeType.Beach;
            else if (biomeNoise < 0.2)
                return BiomeType.Plains;
            else if (biomeNoise < 0.4)
                return BiomeType.Forest;
            else if (biomeNoise < 0.6)
                return BiomeType.Desert;
            else
                return BiomeType.Mountains;
        }

        /// <summary>
        /// Apply biome-specific height modifier
        /// </summary>
        private int ApplyBiomeHeightModifier(int baseHeight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => baseHeight - 20,
                BiomeType.Beach => baseHeight - 5,
                BiomeType.Plains => baseHeight,
                BiomeType.Forest => baseHeight + 5,
                BiomeType.Desert => baseHeight + 10,
                BiomeType.Mountains => baseHeight + 20,
                _ => baseHeight
            };
        }

        /// <summary>
        /// Determine surface block type
        /// </summary>
        private BlockType DetermineSurfaceBlock(TerrainGenerationContext context, int x, int z, 
            int surfaceHeight, float[,] riverMask, float[,] lakeMask)
        {
            var worldX = context.ChunkX * 16 + x;
            var worldZ = context.ChunkZ * 16 + z;
            var biome = DetermineBiome(worldX, worldZ);
            
            // Check if water/lake
            if (riverMask[x, z] > 0.1f || lakeMask[x, z] > 0.1f)
            {
                return BlockType.Water;
            }
            
            // Return biome-specific surface block
            return biome switch
            {
                BiomeType.Ocean => BlockType.Sand,
                BiomeType.Beach => BlockType.Sand,
                BiomeType.Plains => BlockType.Grass,
                BiomeType.Forest => BlockType.Grass,
                BiomeType.Desert => BlockType.Sand,
                BiomeType.Mountains => BlockType.Stone,
                _ => BlockType.Grass
            };
        }

        /// <summary>
        /// Determine sub-surface block type
        /// </summary>
        private BlockType DetermineSubSurfaceBlock(int y, int surfaceHeight)
        {
            int depth = surfaceHeight - y;
            
            if (depth <= 1)
                return BlockType.Dirt;
            else if (depth <= 3)
                return BlockType.Dirt;
            else
                return BlockType.Stone;
        }

        /// <summary>
        /// Smooth height map
        /// </summary>
        private void SmoothHeightMap(int[,] heightMap)
        {
            var smoothedMap = new int[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += heightMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Blend original and smoothed
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    heightMap[x, z] = (heightMap[x, z] + smoothedMap[x, z]) / 2;
                }
            }
        }

        /// <summary>
        /// Add ore veins to the terrain
        /// </summary>
        private void AddOreVeins(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add coal veins
            if (random.NextDouble() < 0.7)
            {
                AddOreVein(chunk, random, BlockType.CoalOre, 5, 40);
            }
            
            // Add iron veins
            if (random.NextDouble() < 0.5)
            {
                AddOreVein(chunk, random, BlockType.IronOre, 10, 60);
            }
            
            // Add gold veins
            if (random.NextDouble() < 0.2)
            {
                AddOreVein(chunk, random, BlockType.GoldOre, 20, 40);
            }
        }

        /// <summary>
        /// Add a single ore vein
        /// </summary>
        private void AddOreVein(ChunkData chunk, Random random, BlockType oreType, int minY, int maxY)
        {
            int startX = random.Next(16);
            int startZ = random.Next(16);
            int startY = random.Next(minY, maxY);
            
            int veinLength = 5 + random.Next(10);
            double direction = random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < veinLength; i++)
            {
                int x = startX + (int)(Math.Cos(direction) * i);
                int z = startZ + (int)(Math.Sin(direction) * i);
                int y = startY + random.Next(-2, 2);
                
                if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= minY && y < maxY)
                {
                    // Only replace stone with ore
                    if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                    {
                        chunk.SetBlock(x, y, z, oreType);
                    }
                }
            }
        }

        /// <summary>
        /// Add vegetation to the terrain
        /// </summary>
        private void AddVegetation(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = context.ChunkX * 16 + x;
                    int worldZ = context.ChunkZ * 16 + z;
                    var biome = DetermineBiome(worldX, worldZ);
                    
                    // Find surface
                    int surfaceY = -1;
                    for (int y = 255; y >= 0; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air && block != BlockType.Water)
                        {
                            surfaceY = y;
                            break;
                        }
                    }
                    
                    if (surfaceY >= 0 && surfaceY + 1 < 256)
                    {
                        // Add biome-specific vegetation
                        AddBiomeVegetation(chunk, random, x, z, surfaceY, biome);
                    }
                }
            }
        }

        /// <summary>
        /// Add biome-specific vegetation
        /// </summary>
        private void AddBiomeVegetation(ChunkData chunk, Random random, int x, int z, int surfaceY, BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    // Add trees
                    if (random.NextDouble() < 0.1)
                    {
                        AddTree(chunk, x, surfaceY + 1, z);
                    }
                    // Add tall grass
                    else if (random.NextDouble() < 0.3)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    break;
                    
                case BiomeType.Plains:
                    // Add grass and flowers
                    if (random.NextDouble() < 0.2)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.TallGrass);
                    }
                    else if (random.NextDouble() < 0.05)
                    {
                        chunk.SetBlock(x, surfaceY + 1, z, BlockType.Flower);
                    }
                    break;
                    
                case BiomeType.Desert:
                    // Add cacti
                    if (random.NextDouble() < 0.02)
                    {
                        AddCactus(chunk, x, surfaceY + 1, z, random);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add a tree
        /// </summary>
        private void AddTree(ChunkData chunk, int x, int y, int z)
        {
            // Simple tree - trunk and leaves
            int treeHeight = 4 + UnityEngine.Random.Range(0, 3);
            
            // Trunk
            for (int i = 0; i < treeHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Log);
            }
            
            // Leaves
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 3)
                        {
                            int lx = x + dx;
                            int ly = y + treeHeight + dy;
                            int lz = z + dz;
                            
                            if (lx >= 0 && lx < 16 && ly >= 0 && ly < 256 && lz >= 0 && lz < 16)
                            {
                                if (chunk.GetBlock(lx, ly, lz) == BlockType.Air)
                                {
                                    chunk.SetBlock(lx, ly, lz, BlockType.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cactus
        /// </summary>
        private void AddCactus(ChunkData chunk, int x, int y, int z, Random random)
        {
            int cactusHeight = 1 + random.Next(3);
            
            for (int i = 0; i < cactusHeight && y + i < 256; i++)
            {
                chunk.SetBlock(x, y + i, z, BlockType.Cactus);
            }
        }

        /// <summary>
        /// Add structures to the terrain
        /// </summary>
        private void AddStructures(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var random = context.WorldManager.GetChunkRandom(context.ChunkX, context.ChunkZ);
            
            // Add dungeons occasionally
            if (random.NextDouble() < 0.05)
            {
                AddDungeon(chunk, random);
            }
        }

        /// <summary>
        /// Add a dungeon
        /// </summary>
        private void AddDungeon(ChunkData chunk, Random random)
        {
            int dungeonX = 4 + random.Next(8);
            int dungeonZ = 4 + random.Next(8);
            int dungeonY = 10 + random.Next(30);
            
            int dungeonWidth = 5 + random.Next(4);
            int dungeonHeight = 4 + random.Next(3);
            int dungeonDepth = 5 + random.Next(5);
            
            // Carve out dungeon space
            for (int x = dungeonX; x < dungeonX + dungeonWidth && x < 16; x++)
            {
                for (int z = dungeonZ; z < dungeonZ + dungeonDepth && z < 16; z++)
                {
                    for (int y = dungeonY; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
            
            // Add walls and floor
            for (int x = dungeonX - 1; x < dungeonX + dungeonWidth + 1 && x < 16; x++)
            {
                for (int z = dungeonZ - 1; z < dungeonZ + dungeonDepth + 1 && z < 16; z++)
                {
                    // Floor
                    chunk.SetBlock(x, dungeonY, z, BlockType.Cobblestone);
                    
                    // Walls
                    for (int y = dungeonY + 1; y < dungeonY + dungeonHeight && y < 256; y++)
                    {
                        // Only place walls where there's air
                        if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Cobblestone);
                        }
                    }
                }
            }
        }
    }
}
}

