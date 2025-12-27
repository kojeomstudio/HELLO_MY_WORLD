
using System;
using System.Collections.Generic;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Shared context for terrain generation layers
    /// </summary>
    public class TerrainGenerationContext
    {
        /// <summary>
        /// Chunk X coordinate
        /// </summary>
        public int ChunkX { get; set; }
        
        /// <summary>
        /// Chunk Z coordinate
        /// </summary>
        public int ChunkZ { get; set; }
        
        /// <summary>
        /// World seed for generation
        /// </summary>
        public int Seed { get; set; }
        
        /// <summary>
        /// Size of chunk (usually 16)
        /// </summary>
        public int ChunkSize { get; set; } = 16;
        
        /// <summary>
        /// Maximum height of world
        /// </summary>
        public int MaxHeight { get; set; } = 256;
        
        /// <summary>
        /// Random number generator for this chunk
        /// </summary>
        public Random Random { get; set; }
        
        /// <summary>
        /// World generation configuration
        /// </summary>
        public WorldGenerationConfig Config { get; set; }
        
        /// <summary>
        /// Height map for the chunk
        /// </summary>
        public int[,] HeightMap { get; set; }
        
        /// <summary>
        /// Biome data for the chunk
        /// </summary>
        public BiomeType[,] BiomeData { get; set; }
        
        /// <summary>
        /// Temperature data for the chunk
        /// </summary>
        public float[,] TemperatureData { get; set; }
        
        /// <summary>
        /// Humidity data for the chunk
        /// </summary>
        public float[,] HumidityData { get; set; }
        
        /// <summary>
        /// Cave data for the chunk
        /// </summary>
        public bool[,,] CaveData { get; set; }
        
        /// <summary>
        /// River data for the chunk
        /// </summary>
        public bool[,,] RiverData { get; set; }
        
        /// <summary>
        /// Lake data for the chunk
        /// </summary>
        public bool[,,] LakeData { get; set; }
        
        /// <summary>
        /// Ore distribution data for the chunk
        /// </summary>
        public OreDistribution[,] OreData { get; set; }
        
        /// <summary>
        /// Structure data for the chunk
        /// </summary>
        public StructureData[,] StructureData { get; set; }
        
        /// <summary>
        /// Entity spawn data for the chunk
        /// </summary>
        public EntitySpawnData[,] EntityData { get; set; }
        
        /// <summary>
        /// Final block types for the chunk
        /// </summary>
        public int[,,] BlockTypes { get; set; }
        
        /// <summary>
        /// Final block metadata for the chunk
        /// </summary>
        public byte[,,] BlockMetadata { get; set; }
        
        /// <summary>
        /// Gets height at a specific position
        /// </summary>
        public int GetHeight(int localX, int localZ)
        {
            if (HeightMap == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return MaxHeight / 2; // Default height
            
            return HeightMap[localX, localZ];
        }
        
        /// <summary>
        /// Sets height at a specific position
        /// </summary>
        public void SetHeight(int localX, int localZ, int height)
        {
            if (HeightMap == null)
                HeightMap = new int[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HeightMap[localX, localZ] = Math.Max(0, Math.Min(MaxHeight - 1, height));
            }
        }
        
        /// <summary>
        /// Gets biome at a specific position
        /// </summary>
        public BiomeType GetBiome(int localX, int localZ)
        {
            if (BiomeData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return BiomeType.Plains; // Default biome
            
            return BiomeData[localX, localZ];
        }
        
        /// <summary>
        /// Sets biome at a specific position
        /// </summary>
        public void SetBiome(int localX, int localZ, BiomeType biome)
        {
            if (BiomeData == null)
                BiomeData = new BiomeType[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                BiomeData[localX, localZ] = biome;
            }
        }
        
        /// <summary>
        /// Gets temperature at a specific position
        /// </summary>
        public float GetTemperature(int localX, int localZ)
        {
            if (TemperatureData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default temperature
            
            return TemperatureData[localX, localZ];
        }
        
        /// <summary>
        /// Sets temperature at a specific position
        /// </summary>
        public void SetTemperature(int localX, int localZ, float temperature)
        {
            if (TemperatureData == null)
                TemperatureData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                TemperatureData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, temperature));
            }
        }
        
        /// <summary>
        /// Gets humidity at a specific position
        /// </summary>
        public float GetHumidity(int localX, int localZ)
        {
            if (HumidityData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default humidity
            
            return HumidityData[localX, localZ];
        }
        
        /// <summary>
        /// Sets humidity at a specific position
        /// </summary>
        public void SetHumidity(int localX, int localZ, float humidity)
        {
            if (HumidityData == null)
                HumidityData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HumidityData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, humidity));
            }
        }
        
        /// <summary>
        /// Checks if a position is a cave
        /// </summary>
        public bool IsCave(int localX, int y, int localZ)
        {
                return false;
            
            return CaveData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a cave
        /// </summary>
        public void SetCave(int localX, int y, int localZ, bool isCave)
        {
            if (CaveData == null)
                CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                CaveData[localX, y, localZ] = isCave;
            }
        }
        
        /// <summary>
        /// Checks if a position is a river
        /// </summary>
        public bool IsRiver(int localX, int y, int localZ)
        {
            if (RiverData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return RiverData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a river
        /// </summary>
        public void SetRiver(int localX, int y, int localZ, bool isRiver)
        {
            if (RiverData == null)
                RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                RiverData[localX, y, localZ] = isRiver;
            }
        }
        
        /// <summary>
        /// Checks if a position is a lake
        /// </summary>
        public bool IsLake(int localX, int y, int localZ)
        {
            if (LakeData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return LakeData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a lake
        /// </summary>
        public void SetLake(int localX, int y, int localZ, bool isLake)
        {
            if (LakeData == null)
                LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                LakeData[localX, y, localZ] = isLake;
            }
        }
        
        /// <summary>
        /// Gets the block type at a specific position
        /// </summary>
        public int GetBlockType(int localX, int y, int localZ)
        {
            if (BlockTypes == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0; // Air
            
            return BlockTypes[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block type at a specific position
        /// </summary>
        public void SetBlockType(int localX, int y, int localZ, int blockType)
        {
            if (BlockTypes == null)
                BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockTypes[localX, y, localZ] = blockType;
            }
        }
        
        /// <summary>
        /// Gets the block metadata at a specific position
        /// </summary>
        public byte GetBlockMetadata(int localX, int y, int localZ)
        {
            if (BlockMetadata == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0;
            
            return BlockMetadata[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block metadata at a specific position
        /// </summary>
        public void SetBlockMetadata(int localX, int y, int localZ, byte metadata)
        {
            if (BlockMetadata == null)
                BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockMetadata[localX, y, localZ] = metadata;
            }
        }
        
        /// <summary>
        /// Initializes the context with default values
        /// </summary>
        public void Initialize()
        {
            Random = new Random(Seed + ChunkX * 341873128712L + ChunkZ * 132897987541L);
            
            // Initialize all data arrays
            HeightMap = new int[ChunkSize, ChunkSize];
            BiomeData = new BiomeType[ChunkSize, ChunkSize];
            TemperatureData = new float[ChunkSize, ChunkSize];
            HumidityData = new float[ChunkSize, ChunkSize];
            CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            OreData = new OreDistribution[ChunkSize, ChunkSize];
            StructureData = new StructureData[ChunkSize, ChunkSize];
            EntityData = new EntitySpawnData[ChunkSize, ChunkSize];
            BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
        }
    }
    
    /// <summary>
    /// Biome types for world generation
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Plains,
        Desert,
        Forest,
        Taiga,
        Jungle,
        Mountains,
        Swamp,
        SnowyTundra,
        Savanna,
        River,
        Beach
    }
    
    /// <summary>
    /// Data structure for ore distribution
    /// </summary>
    public class OreDistribution
    {
        public int Depth { get; set; }
        public float Richness { get; set; } = 1.0f;
        public Dictionary<string, float> OreVeins { get; set; } = new();
    }
    
    /// <summary>
    /// Data structure for structure placement
    /// </summary>
    public class StructureData
    {
        public Structure Structure { get; set; }
        public bool IsPlaced { get; set; }
        public bool IsGenerated { get; set; }
    }
    
    /// <summary>
    /// Data structure for entity spawning
    /// </summary>
    public class EntitySpawnData
    {
        public List<EntitySpawn> Entities { get; set; } = new();
        public bool IsProcessed { get; set; }
    }
    
    /// <summary>
    /// Data structure for a single entity spawn
    /// </summary>
    public class EntitySpawn
    {
        public string EntityType { get; set; }
        public Position3D Position { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// 3D position structure
    /// </summary>
    public struct Position3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        
        public Position3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    
    /// <summary>
    /// Structure data structure
    /// </summary>
    public class Structure
    {
        public string Type { get; set; }
        public Position3D Position { get; set; }
        public StructureRotation Rotation { get; set; }
        public string Variant { get; set; }
        public StructureTemplate Template { get; set; }
    }
    
    /// <summary>
    /// Structure rotation enum
    /// </summary>
    public enum StructureRotation
    {
        North,
        East,
        South,
        West
    }
    
    /// <summary>
    /// Structure template data structure
    /// </summary>
    public class StructureTemplate
    {
        public string Name { get; set; }
        public Position3D Size { get; set; }
        public StructureBlock[,,] Blocks { get; set; }
    }
    
    /// <summary>
    /// Structure block data structure
    /// </summary>
    public class StructureBlock
    {
        public int BlockId { get; set; }
        public byte Metadata { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
using System.Collections.Generic;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Shared context for terrain generation layers
    /// </summary>
    public class TerrainGenerationContext
    {
        /// <summary>
        /// Chunk X coordinate
        /// </summary>
        public int ChunkX { get; set; }
        
        /// <summary>
        /// Chunk Z coordinate
        /// </summary>
        public int ChunkZ { get; set; }
        
        /// <summary>
        /// World seed for generation
        /// </summary>
        public int Seed { get; set; }
        
        /// <summary>
        /// Size of the chunk (usually 16)
        /// </summary>
        public int ChunkSize { get; set; } = 16;
        
        /// <summary>
        /// Maximum height of the world
        /// </summary>
        public int MaxHeight { get; set; } = 256;
        
        /// <summary>
        /// Random number generator for this chunk
        /// </summary>
        public Random Random { get; set; }
        
        /// <summary>
        /// World generation configuration
        /// </summary>
        public WorldGenerationConfig Config { get; set; }
        
        /// <summary>
        /// Height map for the chunk
        /// </summary>
        public int[,] HeightMap { get; set; }
        
        /// <summary>
        /// Biome data for the chunk
        /// </summary>
        public BiomeType[,] BiomeData { get; set; }
        
        /// <summary>
        /// Temperature data for the chunk
        /// </summary>
        public float[,] TemperatureData { get; set; }
        
        /// <summary>
        /// Humidity data for the chunk
        /// </summary>
        public float[,] HumidityData { get; set; }
        
        /// <summary>
        /// Cave data for the chunk
        /// </summary>
        public bool[,,] CaveData { get; set; }
        
        /// <summary>
        /// River data for the chunk
        /// </summary>
        public bool[,,] RiverData { get; set; }
        
        /// <summary>
        /// Lake data for the chunk
        /// </summary>
        public bool[,,] LakeData { get; set; }
        
        /// <summary>
        /// Ore distribution data for the chunk
        /// </summary>
        public OreDistribution[,] OreData { get; set; }
        
        /// <summary>
        /// Structure data for the chunk
        /// </summary>
        public StructureData[,] StructureData { get; set; }
        
        /// <summary>
        /// Entity spawn data for the chunk
        /// </summary>
        public EntitySpawnData[,] EntityData { get; set; }
        
        /// <summary>
        /// Final block types for the chunk
        /// </summary>
        public int[,,] BlockTypes { get; set; }
        
        /// <summary>
        /// Final block metadata for the chunk
        /// </summary>
        public byte[,,] BlockMetadata { get; set; }
        
        /// <summary>
        /// Gets the height at a specific position
        /// </summary>
        public int GetHeight(int localX, int localZ)
        {
            if (HeightMap == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return MaxHeight / 2; // Default height
            
            return HeightMap[localX, localZ];
        }
        
        /// <summary>
        /// Sets the height at a specific position
        /// </summary>
        public void SetHeight(int localX, int localZ, int height)
        {
            if (HeightMap == null)
                HeightMap = new int[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HeightMap[localX, localZ] = Math.Max(0, Math.Min(MaxHeight - 1, height));
            }
        }
        
        /// <summary>
        /// Gets the biome at a specific position
        /// </summary>
        public BiomeType GetBiome(int localX, int localZ)
        {
            if (BiomeData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return BiomeType.Plains; // Default biome
            
            return BiomeData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the biome at a specific position
        /// </summary>
        public void SetBiome(int localX, int localZ, BiomeType biome)
        {
            if (BiomeData == null)
                BiomeData = new BiomeType[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                BiomeData[localX, localZ] = biome;
            }
        }
        
        /// <summary>
        /// Gets the temperature at a specific position
        /// </summary>
        public float GetTemperature(int localX, int localZ)
        {
            if (TemperatureData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default temperature
            
            return TemperatureData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the temperature at a specific position
        /// </summary>
        public void SetTemperature(int localX, int localZ, float temperature)
        {
            if (TemperatureData == null)
                TemperatureData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                TemperatureData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, temperature));
            }
        }
        
        /// <summary>
        /// Gets the humidity at a specific position
        /// </summary>
        public float GetHumidity(int localX, int localZ)
        {
            if (HumidityData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default humidity
            
            return HumidityData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the humidity at a specific position
        /// </summary>
        public void SetHumidity(int localX, int localZ, float humidity)
        {
            if (HumidityData == null)
                HumidityData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HumidityData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, humidity));
            }
        }
        
        /// <summary>
        /// Checks if a position is a cave
        /// </summary>
        public bool IsCave(int localX, int y, int localZ)
        {
            if (CaveData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return CaveData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a cave
        /// </summary>
        public void SetCave(int localX, int y, int localZ, bool isCave)
        {
            if (CaveData == null)
                CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                CaveData[localX, y, localZ] = isCave;
            }
        }
        
        /// <summary>
        /// Checks if a position is a river
        /// </summary>
        public bool IsRiver(int localX, int y, int localZ)
        {
            if (RiverData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return RiverData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a river
        /// </summary>
        public void SetRiver(int localX, int y, int localZ, bool isRiver)
        {
            if (RiverData == null)
                RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                RiverData[localX, y, localZ] = isRiver;
            }
        }
        
        /// <summary>
        /// Checks if a position is a lake
        /// </summary>
        public bool IsLake(int localX, int y, int localZ)
        {
            if (LakeData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return LakeData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a lake
        /// </summary>
        public void SetLake(int localX, int y, int localZ, bool isLake)
        {
            if (LakeData == null)
                LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                LakeData[localX, y, localZ] = isLake;
            }
        }
        
        /// <summary>
        /// Gets the block type at a specific position
        /// </summary>
        public int GetBlockType(int localX, int y, int localZ)
        {
            if (BlockTypes == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0; // Air
            
            return BlockTypes[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block type at a specific position
        /// </summary>
        public void SetBlockType(int localX, int y, int localZ, int blockType)
        {
            if (BlockTypes == null)
                BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockTypes[localX, y, localZ] = blockType;
            }
        }
        
        /// <summary>
        /// Gets the block metadata at a specific position
        /// </summary>
        public byte GetBlockMetadata(int localX, int y, int localZ)
        {
            if (BlockMetadata == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0;
            
            return BlockMetadata[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block metadata at a specific position
        /// </summary>
        public void SetBlockMetadata(int localX, int y, int localZ, byte metadata)
        {
            if (BlockMetadata == null)
                BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockMetadata[localX, y, localZ] = metadata;
            }
        }
        
        /// <summary>
        /// Initializes the context with default values
        /// </summary>
        public void Initialize()
        {
            Random = new Random(Seed + ChunkX * 341873128712L + ChunkZ * 132897987541L);
            
            // Initialize all data arrays
            HeightMap = new int[ChunkSize, ChunkSize];
            BiomeData = new BiomeType[ChunkSize, ChunkSize];
            TemperatureData = new float[ChunkSize, ChunkSize];
            HumidityData = new float[ChunkSize, ChunkSize];
            CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            OreData = new OreDistribution[ChunkSize, ChunkSize];
            StructureData = new StructureData[ChunkSize, ChunkSize];
            EntityData = new EntitySpawnData[ChunkSize, ChunkSize];
            BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
        }
    }
    
    /// <summary>
    /// Biome types for world generation
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Plains,
        Desert,
        Forest,
        Taiga,
        Jungle,
        Mountains,
        Swamp,
        SnowyTundra,
        Savanna,
        River,
        Beach
    }
    
    /// <summary>
    /// Data structure for ore distribution
    /// </summary>
    public class OreDistribution
    {
        public int Depth { get; set; }
        public float Richness { get; set; } = 1.0f;
        public Dictionary<string, float> OreVeins { get; set; } = new();
    }
    
    /// <summary>
    /// Data structure for structure placement
    /// </summary>
    public class StructureData
    {
        public Structure Structure { get; set; }
        public bool IsPlaced { get; set; }
        public bool IsGenerated { get; set; }
    }
    
    /// <summary>
    /// Data structure for entity spawning
    /// </summary>
    public class EntitySpawnData
    {
        public List<EntitySpawn> Entities { get; set; } = new();
        public bool IsProcessed { get; set; }
    }
    
    /// <summary>
    /// Data structure for a single entity spawn
    /// </summary>
    public class EntitySpawn
    {
        public string EntityType { get; set; }
        public Position3D Position { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// 3D position structure
    /// </summary>
    public struct Position3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        
        public Position3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    
    /// <summary>
    /// Structure data structure
    /// </summary>
    public class Structure
    {
        public string Type { get; set; }
        public Position3D Position { get; set; }
        public StructureRotation Rotation { get; set; }
        public string Variant { get; set; }
        public StructureTemplate Template { get; set; }
    }
    
    /// <summary>
    /// Structure rotation enum
    /// </summary>
    public enum StructureRotation
    {
        North,
        East,
        South,
        West
    }
    
    /// <summary>
    /// Structure template data structure
    /// </summary>
    public class StructureTemplate
    {
        public string Name { get; set; }
        public Position3D Size { get; set; }
        public StructureBlock[,,] Blocks { get; set; }
    }
    
    /// <summary>
    /// Structure block data structure
    /// </summary>
    public class StructureBlock
    {
        public int BlockId { get; set; }
        public byte Metadata { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
}

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Shared context for terrain generation layers
    /// </summary>
    public class TerrainGenerationContext
    {
        /// <summary>
        /// Chunk X coordinate
        /// </summary>
        public int ChunkX { get; set; }
        
        /// <summary>
        /// Chunk Z coordinate
        /// </summary>
        public int ChunkZ { get; set; }
        
        /// <summary>
        /// World seed for generation
        /// </summary>
        public int Seed { get; set; }
        
        /// <summary>
        /// Size of the chunk (usually 16)
        /// </summary>
        public int ChunkSize { get; set; } = 16;
        
        /// <summary>
        /// Maximum height of the world
        /// </summary>
        public int MaxHeight { get; set; } = 256;
        
        /// <summary>
        /// Random number generator for this chunk
        /// </summary>
        public Random Random { get; set; }
        
        /// <summary>
        /// World generation configuration
        /// </summary>
        public WorldGenerationConfig Config { get; set; }
        
        /// <summary>
        /// Height map for the chunk
        /// </summary>
        public int[,] HeightMap { get; set; }
        
        /// <summary>
        /// Biome data for the chunk
        /// </summary>
        public BiomeType[,] BiomeData { get; set; }
        
        /// <summary>
        /// Temperature data for the chunk
        /// </summary>
        public float[,] TemperatureData { get; set; }
        
        /// <summary>
        /// Humidity data for the chunk
        /// </summary>
        public float[,] HumidityData { get; set; }
        
        /// <summary>
        /// Cave data for the chunk
        /// </summary>
        public bool[,,] CaveData { get; set; }
        
        /// <summary>
        /// River data for the chunk
        /// </summary>
        public bool[,,] RiverData { get; set; }
        
        /// <summary>
        /// Lake data for the chunk
        /// </summary>
        public bool[,,] LakeData { get; set; }
        
        /// <summary>
        /// Ore distribution data for the chunk
        /// </summary>
        public OreDistribution[,] OreData { get; set; }
        
        /// <summary>
        /// Structure data for the chunk
        /// </summary>
        public StructureData[,] StructureData { get; set; }
        
        /// <summary>
        /// Entity spawn data for the chunk
        /// </summary>
        public EntitySpawnData[,] EntityData { get; set; }
        
        /// <summary>
        /// Final block types for the chunk
        /// </summary>
        public int[,,] BlockTypes { get; set; }
        
        /// <summary>
        /// Final block metadata for the chunk
        /// </summary>
        public byte[,,] BlockMetadata { get; set; }
        
        /// <summary>
        /// Gets the height at a specific position
        /// </summary>
        public int GetHeight(int localX, int localZ)
        {
            if (HeightMap == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return MaxHeight / 2; // Default height
            
            return HeightMap[localX, localZ];
        }
        
        /// <summary>
        /// Sets the height at a specific position
        /// </summary>
        public void SetHeight(int localX, int localZ, int height)
        {
            if (HeightMap == null)
                HeightMap = new int[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HeightMap[localX, localZ] = Math.Max(0, Math.Min(MaxHeight - 1, height));
            }
        }
        
        /// <summary>
        /// Gets the biome at a specific position
        /// </summary>
        public BiomeType GetBiome(int localX, int localZ)
        {
            if (BiomeData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return BiomeType.Plains; // Default biome
            
            return BiomeData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the biome at a specific position
        /// </summary>
        public void SetBiome(int localX, int localZ, BiomeType biome)
        {
            if (BiomeData == null)
                BiomeData = new BiomeType[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                BiomeData[localX, localZ] = biome;
            }
        }
        
        /// <summary>
        /// Gets the temperature at a specific position
        /// </summary>
        public float GetTemperature(int localX, int localZ)
        {
            if (TemperatureData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default temperature
            
            return TemperatureData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the temperature at a specific position
        /// </summary>
        public void SetTemperature(int localX, int localZ, float temperature)
        {
            if (TemperatureData == null)
                TemperatureData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                TemperatureData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, temperature));
            }
        }
        
        /// <summary>
        /// Gets the humidity at a specific position
        /// </summary>
        public float GetHumidity(int localX, int localZ)
        {
            if (HumidityData == null || localX < 0 || localX >= ChunkSize || localZ < 0 || localZ >= ChunkSize)
                return 0.5f; // Default humidity
            
            return HumidityData[localX, localZ];
        }
        
        /// <summary>
        /// Sets the humidity at a specific position
        /// </summary>
        public void SetHumidity(int localX, int localZ, float humidity)
        {
            if (HumidityData == null)
                HumidityData = new float[ChunkSize, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && localZ >= 0 && localZ < ChunkSize)
            {
                HumidityData[localX, localZ] = Math.Max(0.0f, Math.Min(1.0f, humidity));
            }
        }
        
        /// <summary>
        /// Checks if a position is a cave
        /// </summary>
        public bool IsCave(int localX, int y, int localZ)
        {
            if (CaveData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return CaveData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a cave
        /// </summary>
        public void SetCave(int localX, int y, int localZ, bool isCave)
        {
            if (CaveData == null)
                CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                CaveData[localX, y, localZ] = isCave;
            }
        }
        
        /// <summary>
        /// Checks if a position is a river
        /// </summary>
        public bool IsRiver(int localX, int y, int localZ)
        {
            if (RiverData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return RiverData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a river
        /// </summary>
        public void SetRiver(int localX, int y, int localZ, bool isRiver)
        {
            if (RiverData == null)
                RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                RiverData[localX, y, localZ] = isRiver;
            }
        }
        
        /// <summary>
        /// Checks if a position is a lake
        /// </summary>
        public bool IsLake(int localX, int y, int localZ)
        {
            if (LakeData == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return false;
            
            return LakeData[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets whether a position is a lake
        /// </summary>
        public void SetLake(int localX, int y, int localZ, bool isLake)
        {
            if (LakeData == null)
                LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                LakeData[localX, y, localZ] = isLake;
            }
        }
        
        /// <summary>
        /// Gets the block type at a specific position
        /// </summary>
        public int GetBlockType(int localX, int y, int localZ)
        {
            if (BlockTypes == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0; // Air
            
            return BlockTypes[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block type at a specific position
        /// </summary>
        public void SetBlockType(int localX, int y, int localZ, int blockType)
        {
            if (BlockTypes == null)
                BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockTypes[localX, y, localZ] = blockType;
            }
        }
        
        /// <summary>
        /// Gets the block metadata at a specific position
        /// </summary>
        public byte GetBlockMetadata(int localX, int y, int localZ)
        {
            if (BlockMetadata == null || localX < 0 || localX >= ChunkSize || y < 0 || y >= MaxHeight || localZ < 0 || localZ >= ChunkSize)
                return 0;
            
            return BlockMetadata[localX, y, localZ];
        }
        
        /// <summary>
        /// Sets the block metadata at a specific position
        /// </summary>
        public void SetBlockMetadata(int localX, int y, int localZ, byte metadata)
        {
            if (BlockMetadata == null)
                BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
            
            if (localX >= 0 && localX < ChunkSize && y >= 0 && y < MaxHeight && localZ >= 0 && localZ < ChunkSize)
            {
                BlockMetadata[localX, y, localZ] = metadata;
            }
        }
        
        /// <summary>
        /// Initializes the context with default values
        /// </summary>
        public void Initialize()
        {
            Random = new Random(Seed + ChunkX * 341873128712L + ChunkZ * 132897987541L);
            
            // Initialize all data arrays
            HeightMap = new int[ChunkSize, ChunkSize];
            BiomeData = new BiomeType[ChunkSize, ChunkSize];
            TemperatureData = new float[ChunkSize, ChunkSize];
            HumidityData = new float[ChunkSize, ChunkSize];
            CaveData = new bool[ChunkSize, MaxHeight, ChunkSize];
            RiverData = new bool[ChunkSize, MaxHeight, ChunkSize];
            LakeData = new bool[ChunkSize, MaxHeight, ChunkSize];
            OreData = new OreDistribution[ChunkSize, ChunkSize];
            StructureData = new StructureData[ChunkSize, ChunkSize];
            EntityData = new EntitySpawnData[ChunkSize, ChunkSize];
            BlockTypes = new int[ChunkSize, MaxHeight, ChunkSize];
            BlockMetadata = new byte[ChunkSize, MaxHeight, ChunkSize];
        }
    }
    
    /// <summary>
    /// Biome types for world generation
    /// </summary>
    public enum BiomeType
    {
        Ocean,
        Plains,
        Desert,
        Forest,
        Taiga,
        Jungle,
        Mountains,
        Swamp,
        SnowyTundra,
        Savanna,
        River,
        Beach
    }
    
    /// <summary>
    /// Data structure for ore distribution
    /// </summary>
    public class OreDistribution
    {
        public int Depth { get; set; }
        public float Richness { get; set; } = 1.0f;
        public Dictionary<string, float> OreVeins { get; set; } = new();
    }
    
    /// <summary>
    /// Data structure for structure placement
    /// </summary>
    public class StructureData
    {
        public Structure Structure { get; set; }
        public bool IsPlaced { get; set; }
        public bool IsGenerated { get; set; }
    }
    
    /// <summary>
    /// Data structure for entity spawning
    /// </summary>
    public class EntitySpawnData
    {
        public List<EntitySpawn> Entities { get; set; } = new();
        public bool IsProcessed { get; set; }
    }
    
    /// <summary>
    /// Data structure for a single entity spawn
    /// </summary>
    public class EntitySpawn
    {
        public string EntityType { get; set; }
        public Position3D Position { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// 3D position structure
    /// </summary>
    public struct Position3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        
        public Position3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    
    /// <summary>
    /// Structure data structure
    /// </summary>
    public class Structure
    {
        public string Type { get; set; }
        public Position3D Position { get; set; }
        public StructureRotation Rotation { get; set; }
        public string Variant { get; set; }
        public StructureTemplate Template { get; set; }
    }
    
    /// <summary>
    /// Structure rotation enum
    /// </summary>
    public enum StructureRotation
    {
        North,
        East,
        South,
        West
    }
    
    /// <summary>
    /// Structure template data structure
    /// </summary>
    public class StructureTemplate
    {
        public string Name { get; set; }
        public Position3D Size { get; set; }
        public StructureBlock[,,] Blocks { get; set; }
    }
    
    /// <summary>
    /// Structure block data structure
    /// </summary>
    public class StructureBlock
    {
        public int BlockId { get; set; }
        public byte Metadata { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
}
                return false;
                
            return LakeData[localX, y, localZ];
        }
        
        /// <summary>
        /// Gets the ore distribution at a specific position
        /// </summary>
        public OreDistribution GetOreDistribution(int localX, int localZ)
        {
            if (OreData == null)
                return new OreDistribution();
                
            return OreData[localX, localZ] ?? new OreDistribution();
        }
    }
    
    /// <summary>
    /// Data structure for ore distribution
    /// </summary>
    public class OreDistribution
    {
        public Dictionary<string, float> OreVeins { get; set; } = new();
        public float Richness { get; set; } = 1.0f;
        public int Depth { get; set; } = 0;
    }
    
    /// <summary>
    /// Data structure for generated structures
    /// </summary>
    public class StructureData
    {
        public string Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Data structure for entity spawn points
    /// </summary>
    public class EntitySpawnData
    {
        public string EntityType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public float SpawnChance { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
