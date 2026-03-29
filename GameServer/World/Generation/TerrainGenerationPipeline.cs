using System;
using System.Collections.Generic;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Simple pipeline to execute ordered terrain features for a chunk.
    /// </summary>
    public sealed class TerrainGenerationPipeline
    {
        private readonly List<ITerrainGenerationStage> stages = new();

        public TerrainGenerationPipeline AddStage(ITerrainGenerationStage stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            stages.Add(stage);
            return this;
        }

        public void Execute(TerrainGenerationContext context)
        {
            foreach (var stage in stages)
            {
                try
                {
                    stage.Execute(context);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Terrain stage '{stage.Name}' failed for chunk ({context.ChunkX},{context.ChunkZ}).", ex);
                }
            }
        }
    }
}
