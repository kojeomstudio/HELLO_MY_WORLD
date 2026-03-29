using System;
using System.Collections.Generic;
using GameServerApp;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Shared context passed between terrain stages. Holds chunk references,
    /// cached masks (caves/rivers/lakes), and data-driven configuration.
    /// </summary>
    public sealed class TerrainGenerationContext
    {
        private readonly Dictionary<string, object> metadata = new();

        public TerrainGenerationContext(
            WorldManager manager,
            ChunkData chunk,
            int chunkX,
            int chunkZ,
            WorldGenerationConfig? config = null,
            WorldSettings? worldSettings = null,
            long? worldSeed = null)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Config = config;
            WorldSettings = worldSettings;
            WorldSeed = worldSeed ?? worldSettings?.WorldSeed ?? config?.Seed ?? 0;
        }

        public WorldManager Manager { get; }
        public ChunkData Chunk { get; }
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public WorldGenerationConfig? Config { get; }
        public WorldSettings? WorldSettings { get; }
        public long WorldSeed { get; }

        public int[,]? HeightMap { get; set; }
        public int[,]? BiomeMap { get; set; }
        public bool[,,]? CaveMask { get; set; }
        public float[,]? RiverMask { get; set; }
        public float[,]? LakeMask { get; set; }
        public float[,]? HydrologyMask { get; set; }
        public float[,]? FlowAccumulation { get; set; }

        public void SetMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key must be provided", nameof(key));
            metadata[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool TryGetMetadata<T>(string key, out T value)
        {
            if (metadata.TryGetValue(key, out var stored) && stored is T typed)
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
            metadata[key] = created!;
            return created;
        }
    }
}
