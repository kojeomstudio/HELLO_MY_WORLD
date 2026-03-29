using System;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Contract for ordered terrain generation pipeline stages.
    /// </summary>
    public interface ITerrainGenerationStage
    {
        /// <summary>
        /// Human-readable stage name for logging and diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Execute the stage against the supplied terrain context.
        /// </summary>
        /// <param name="context">Shared generation context.</param>
        void Execute(TerrainGenerationContext context);
    }
}
