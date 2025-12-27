using System;

namespace GameServerApp.World.Generation.Interfaces
{
    /// <summary>
    /// Interface for core terrain generation layers
    /// </summary>
    public interface ICoreLayer
    {
        /// <summary>
        /// Unique identifier for this layer
        /// </summary>
        string LayerId { get; }
        
        /// <summary>
        /// Priority of execution (lower numbers execute first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this layer is enabled
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Gets the configuration for this layer
        /// </summary>
        /// <typeparam name="T">Type of configuration</typeparam>
        /// <returns>Configuration instance</returns>
        T GetConfig<T>() where T : class, new();
        
        /// <summary>
        /// Executes the layer's generation logic
        /// </summary>
        /// <param name="context">Shared generation context</param>
        void Execute(TerrainGenerationContext context);
    }
}
namespace GameServerApp.World.Generation.Interfaces
{
    /// <summary>
    /// Interface for core terrain generation layers
    /// </summary>
    public interface ICoreLayer
    {
        /// <summary>
        /// Unique identifier for this layer
        /// </summary>
        string LayerId { get; }
        
        /// <summary>
        /// Priority of execution (lower numbers execute first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this layer is enabled
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Gets the configuration for this layer
        /// </summary>
        /// <typeparam name="T">Type of configuration</typeparam>
        /// <returns>Configuration instance</returns>
        T GetConfig<T>() where T : class, new();
        
        /// <summary>
        /// Executes the layer's generation logic
        /// </summary>
        /// <param name="context">Shared generation context</param>
        void Execute(TerrainGenerationContext context);
    }
}
