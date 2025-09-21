using System;
using System.Collections.Generic;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Shared context passed to each terrain generation stage allowing access to the chunk,
    /// coordinates, and owning world manager.
    /// </summary>
    public sealed class TerrainGenerationContext
    {
        public TerrainGenerationContext(WorldManager manager, ChunkData chunk, int chunkX, int chunkZ)
        {
            Manager = manager;
            Chunk = chunk;
            ChunkX = chunkX;
            ChunkZ = chunkZ;
        }

        public WorldManager Manager { get; }
        public ChunkData Chunk { get; }
        public int ChunkX { get; }
        public int ChunkZ { get; }

        private readonly Dictionary<string, object> _metadata = new();

        public void SetMetadata(string key, object value) => _metadata[key] = value;
        public bool TryGetMetadata<T>(string key, out T value)
        {
            if (_metadata.TryGetValue(key, out var stored) && stored is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Simple pipeline to execute ordered terrain features for a chunk.
    /// </summary>
    public sealed class TerrainGenerationPipeline
    {
        private readonly List<(string name, Action<TerrainGenerationContext> stage)> _stages = new();

        public TerrainGenerationPipeline AddStage(string name, Action<TerrainGenerationContext> stage)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Stage name required", nameof(name));
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            _stages.Add((name, stage));
            return this;
        }

        public void Execute(TerrainGenerationContext context)
        {
            foreach (var (name, stage) in _stages)
            {
                try
                {
                    stage(context);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Terrain stage '{name}' failed for chunk ({context.ChunkX},{context.ChunkZ}).", ex);
                }
            }
        }
    }
}
