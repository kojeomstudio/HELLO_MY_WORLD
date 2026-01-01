using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Context object passed between terrain generation stages
    /// </summary>
    public class TerrainGenerationContext
    {
        /// <summary>
        /// The chunk being generated
        /// </summary>
        public ChunkData Chunk { get; set; }

        /// <summary>
        /// The X coordinate of the chunk
        /// </summary>
        public int ChunkX { get; set; }

        /// <summary>
        /// The Z coordinate of the chunk
        /// </summary>
        public int ChunkZ { get; set; }

        /// <summary>
        /// The height map for the chunk
        /// </summary>
        public int[,] HeightMap { get; set; }

        /// <summary>
        /// The biome map for the chunk
        /// </summary>
        public int[,] BiomeMap { get; set; }

        /// <summary>
        /// The cave mask for the chunk
        /// </summary>
        public bool[,,]? CaveMask { get; set; }

        /// <summary>
        /// The river mask for the chunk
        /// </summary>
        public float[,]? RiverMask { get; set; }

        /// <summary>
        /// The lake mask for the chunk
        /// </summary>
        public float[,]? LakeMask { get; set; }

        /// <summary>
        /// The hydrology mask for the chunk
        /// </summary>
        public float[,]? HydrologyMask { get; set; }

        /// <summary>
        /// The flow accumulation mask for the chunk
        /// </summary>
        public float[,]? FlowAccumulation { get; set; }

        /// <summary>
        /// The world generation configuration
        /// </summary>
        public WorldGenerationConfig Config { get; set; }

        /// <summary>
        /// The world seed
        /// </summary>
        public long WorldSeed { get; set; }

        /// <summary>
        /// The world settings
        /// </summary>
        public WorldSettings WorldSettings { get; set; }
    }
}
namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Context object passed between terrain generation stages
    /// </summary>
    public class TerrainGenerationContext
    {
        /// <summary>
        /// The chunk being generated
        /// </summary>
        public ChunkData Chunk { get; set; }

        /// <summary>
        /// The X coordinate of the chunk
        /// </summary>
        public int ChunkX { get; set; }

        /// <summary>
        /// The Z coordinate of the chunk
        /// </summary>
        public int ChunkZ { get; set; }

        /// <summary>
        /// The height map for the chunk
        /// </summary>
        public int[,] HeightMap { get; set; }

        /// <summary>
        /// The biome map for the chunk
        /// </summary>
        public int[,] BiomeMap { get; set; }

        /// <summary>
        /// The cave mask for the chunk
        /// </summary>
        public bool[,,]? CaveMask { get; set; }

        /// <summary>
        /// The river mask for the chunk
        /// </summary>
        public float[,]? RiverMask { get; set; }

        /// <summary>
        /// The lake mask for the chunk
        /// </summary>
        public float[,]? LakeMask { get; set; }

        /// <summary>
        /// The hydrology mask for the chunk
        /// </summary>
        public float[,]? HydrologyMask { get; set; }

        /// <summary>
        /// The flow accumulation mask for the chunk
        /// </summary>
        public float[,]? FlowAccumulation { get; set; }

        /// <summary>
        /// The world generation configuration
        /// </summary>
        public WorldGenerationConfig Config { get; set; }

        /// <summary>
        /// The world seed
        /// </summary>
        public long WorldSeed { get; set; }

        /// <summary>
        /// The world settings
        /// </summary>
        public WorldSettings WorldSettings { get; set; }
    }
}
