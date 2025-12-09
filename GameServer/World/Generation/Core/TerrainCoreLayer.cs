using System;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Core
{
    /// <summary>
    /// Core terrain generation layer - handles fundamental terrain creation
    /// </summary>
    public class TerrainCoreLayer : ICoreLayer
    {
        public string LayerId => "TerrainCore";
        public int Priority => 100;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        private readonly WorldGenerationConfig _config;
        
        public TerrainCoreLayer(WorldManager worldManager, WorldGenerationConfig config)
        {
            _worldManager = worldManager;
            _config = config;
        }
        
        public void InitializeCore()
        {
            // Initialize noise generators and seed management
            Console.WriteLine($"[{LayerId}] Initialized core terrain systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // Delegate to existing base terrain generation
            _worldManager.GenerateBaseTerrainInternal(context);
        }
    }
    
    /// <summary>
    /// Core biome generation layer - handles biome distribution
    /// </summary>
    public class BiomeCoreLayer : ICoreLayer
    {
        public string LayerId => "BiomeCore";
        public int Priority => 110;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        
        public BiomeCoreLayer(WorldManager worldManager)
        {
            _worldManager = worldManager;
        }
        
        public void InitializeCore()
        {
            Console.WriteLine($"[{LayerId}] Initialized biome systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // Biome generation is already handled in base terrain
            // This layer could be expanded for more complex biome logic
        }
    }
    
    /// <summary>
    /// Core chunk data management layer - handles chunk persistence
    /// </summary>
    public class ChunkDataCoreLayer : ICoreLayer
    {
        public string LayerId => "ChunkDataCore";
        public int Priority => 900;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        
        public ChunkDataCoreLayer(WorldManager worldManager)
        {
            _worldManager = worldManager;
        }
        
        public void InitializeCore()
        {
            Console.WriteLine($"[{LayerId}] Initialized chunk data systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // This runs last to ensure all modifications are captured
            // Chunk persistence is handled by WorldManager
        }
    }
}using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Core
{
    /// <summary>
    /// Core terrain generation layer - handles fundamental terrain creation
    /// </summary>
    public class TerrainCoreLayer : ICoreLayer
    {
        public string LayerId => "TerrainCore";
        public int Priority => 100;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        private readonly WorldGenerationConfig _config;
        
        public TerrainCoreLayer(WorldManager worldManager, WorldGenerationConfig config)
        {
            _worldManager = worldManager;
            _config = config;
        }
        
        public void InitializeCore()
        {
            // Initialize noise generators and seed management
            Console.WriteLine($"[{LayerId}] Initialized core terrain systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // Delegate to existing base terrain generation
            _worldManager.GenerateBaseTerrainInternal(context);
        }
    }
    
    /// <summary>
    /// Core biome generation layer - handles biome distribution
    /// </summary>
    public class BiomeCoreLayer : ICoreLayer
    {
        public string LayerId => "BiomeCore";
        public int Priority => 110;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        
        public BiomeCoreLayer(WorldManager worldManager)
        {
            _worldManager = worldManager;
        }
        
        public void InitializeCore()
        {
            Console.WriteLine($"[{LayerId}] Initialized biome systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // Biome generation is already handled in base terrain
            // This layer could be expanded for more complex biome logic
        }
    }
    
    /// <summary>
    /// Core chunk data management layer - handles chunk persistence
    /// </summary>
    public class ChunkDataCoreLayer : ICoreLayer
    {
        public string LayerId => "ChunkDataCore";
        public int Priority => 900;
        public bool IsEnabled { get; set; } = true;
        
        private readonly WorldManager _worldManager;
        
        public ChunkDataCoreLayer(WorldManager worldManager)
        {
            _worldManager = worldManager;
        }
        
        public void InitializeCore()
        {
            Console.WriteLine($"[{LayerId}] Initialized chunk data systems");
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            if (!IsEnabled) return;
            
            // This runs last to ensure all modifications are captured
            // Chunk persistence is handled by WorldManager
        }
    }
}
