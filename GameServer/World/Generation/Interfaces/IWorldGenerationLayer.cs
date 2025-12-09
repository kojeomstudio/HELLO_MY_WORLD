using System;

namespace GameServerApp.World.Generation.Interfaces
{
    /// <summary>
    /// Base interface for all world generation layers (Core, Content, Utility)
    /// </summary>
    public interface IWorldGenerationLayer
    {
        /// <summary>
        /// Unique identifier for this layer
        /// </summary>
        string LayerId { get; }
        
        /// <summary>
        /// Execution priority (lower numbers run first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this layer is enabled in current configuration
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Execute the generation layer
        /// </summary>
        /// <param name="context">Shared generation context</param>
        void Execute(TerrainGenerationContext context);
    }
    
    /// <summary>
    /// Core layer interfaces - fundamental world generation systems
    /// </summary>
    public interface ICoreLayer : IWorldGenerationLayer
    {
        /// <summary>
        /// Initialize core systems (noise generators, seed management)
        /// </summary>
        void InitializeCore();
    }
    
    /// <summary>
    /// Content layer interfaces - gameplay-related world features
    /// </summary>
    public interface IContentLayer : IWorldGenerationLayer
    {
        /// <summary>
        /// Get configuration data for this content layer
        /// </summary>
        T GetConfig<T>() where T : class, new();
    }
    
    /// <summary>
    /// Utility layer interfaces - support systems and optimizations
    /// </summary>
    public interface IUtilityLayer : IWorldGenerationLayer
    {
        /// <summary>
        /// Optimize generated data (compression, caching, etc.)
        /// </summary>
        void OptimizeData(TerrainGenerationContext context);
    }
}