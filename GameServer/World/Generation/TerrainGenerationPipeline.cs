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

        public T GetOrAddMetadata<T>(string key, Func<T> factory)
        {
            if (TryGetMetadata<T>(key, out var existing))
            {
                return existing;
            }

            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var created = factory();
            _metadata[key] = created!;
            return created;
        }
    }

    public interface ITerrainGenerationStage
    {
        string Name { get; }
        void Execute(TerrainGenerationContext context);
    }

    /// <summary>
    /// Simple pipeline to execute ordered terrain features for a chunk.
    /// </summary>
    public sealed class TerrainGenerationPipeline
    {
        private readonly List<ITerrainGenerationStage> _stages = new();

        public TerrainGenerationPipeline AddStage(ITerrainGenerationStage stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            _stages.Add(stage);
            return this;
        }

        public void Execute(TerrainGenerationContext context)
        {
            foreach (var stage in _stages)
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
