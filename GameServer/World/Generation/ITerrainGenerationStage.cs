using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Interface for terrain generation pipeline stages
    /// </summary>
    public interface ITerrainGenerationStage
    {
        /// <summary>
        /// Name of the generation stage
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Execute the generation stage
        /// </summary>
        /// <param name="context">The terrain generation context</param>
        void Execute(TerrainGenerationContext context);
    }
}
namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Interface for terrain generation pipeline stages
    /// </summary>
    public interface ITerrainGenerationStage
    {
        /// <summary>
        /// Name of the generation stage
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Execute the generation stage
        /// </summary>
        /// <param name="context">The terrain generation context</param>
        void Execute(TerrainGenerationContext context);
    }
}
