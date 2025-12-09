using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Manages the execution of world generation layers in proper order
    /// </summary>
    public class LayerManager
    {
        private readonly List<IWorldGenerationLayer> _layers = new();
        private readonly Dictionary<Type, IWorldGenerationLayer> _layerCache = new();
        
        /// <summary>
        /// Register a generation layer
        /// </summary>
        public void RegisterLayer(IWorldGenerationLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException(nameof(layer));
                
            var layerType = layer.GetType();
            if (_layerCache.ContainsKey(layerType))
            {
                throw new InvalidOperationException($"Layer of type {layerType.Name} is already registered");
            }
            
            _layers.Add(layer);
            _layerCache[layerType] = layer;
            
            // Sort layers by priority
            _layers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        /// <summary>
        /// Get a registered layer by type
        /// </summary>
        public T GetLayer<T>() where T : class, IWorldGenerationLayer
        {
            if (_layerCache.TryGetValue(typeof(T), out var layer))
            {
                return layer as T;
            }
            return null;
        }
        
        /// <summary>
        /// Execute all enabled layers for terrain generation
        /// </summary>
        public void ExecuteLayers(TerrainGenerationContext context)
        {
            Console.WriteLine($"[LayerManager] Executing {_layers.Count(l => l.IsEnabled)} enabled layers for chunk ({context.ChunkX},{context.ChunkZ})");
            
            foreach (var layer in _layers.Where(l => l.IsEnabled))
            {
                try
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    layer.Execute(context);
                    timer.Stop();
                    
                    Console.WriteLine($"[LayerManager] Layer {layer.LayerId} completed in {timer.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LayerManager] Layer {layer.LayerId} failed: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Initialize all core layers
        /// </summary>
        public void InitializeCoreLayers()
        {
            var coreLayers = _layers.OfType<ICoreLayer>();
            Console.WriteLine($"[LayerManager] Initializing {coreLayers.Count()} core layers");
            
            foreach (var coreLayer in coreLayers)
            {
                try
                {
                    coreLayer.InitializeCore();
                    Console.WriteLine($"[LayerManager] Core layer {coreLayer.LayerId} initialized");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LayerManager] Failed to initialize core layer {coreLayer.LayerId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Enable/disable a layer by ID
        /// </summary>
        public void SetLayerEnabled(string layerId, bool enabled)
        {
            var layer = _layers.FirstOrDefault(l => l.LayerId == layerId);
            if (layer != null)
            {
                layer.IsEnabled = enabled;
                Console.WriteLine($"[LayerManager] Layer {layerId} {(enabled ? "enabled" : "disabled")}");
            }
            else
            {
                Console.WriteLine($"[LayerManager] Layer {layerId} not found");
            }
        }
        
        /// <summary>
        /// Get all registered layers
        /// </summary>
        public IReadOnlyList<IWorldGenerationLayer> GetAllLayers()
        {
            return _layers.AsReadOnly();
        }
        
        /// <summary>
        /// Get layers by category
        /// </summary>
        public IEnumerable<T> GetLayersByCategory<T>() where T : IWorldGenerationLayer
        {
            return _layers.OfType<T>();
        }
        
        /// <summary>
        /// Clear all registered layers
        /// </summary>
        public void ClearLayers()
        {
            _layers.Clear();
            _layerCache.Clear();
            Console.WriteLine("[LayerManager] All layers cleared");
        }
        
        /// <summary>
        /// Get execution statistics
        /// </summary>
        public LayerExecutionStats GetExecutionStats()
        {
            return new LayerExecutionStats
            {
                TotalLayers = _layers.Count,
                EnabledLayers = _layers.Count(l => l.IsEnabled),
                CoreLayers = _layers.OfType<ICoreLayer>().Count(),
                ContentLayers = _layers.OfType<IContentLayer>().Count(),
                UtilityLayers = _layers.OfType<IUtilityLayer>().Count()
            };
        }
    }
    
    /// <summary>
    /// Statistics about layer execution
    /// </summary>
    public class LayerExecutionStats
    {
        public int TotalLayers { get; set; }
        public int EnabledLayers { get; set; }
        public int CoreLayers { get; set; }
        public int ContentLayers { get; set; }
        public int UtilityLayers { get; set; }
        
        public override string ToString()
        {
            return $"Layers: {EnabledLayers}/{TotalLayers} enabled " +
                   $"(Core: {CoreLayers}, Content: {ContentLayers}, Utility: {UtilityLayers})";
        }
    }
}using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Manages the execution of world generation layers in proper order
    /// </summary>
    public class LayerManager
    {
        private readonly List<IWorldGenerationLayer> _layers = new();
        private readonly Dictionary<Type, IWorldGenerationLayer> _layerCache = new();
        
        /// <summary>
        /// Register a generation layer
        /// </summary>
        public void RegisterLayer(IWorldGenerationLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException(nameof(layer));
                
            var layerType = layer.GetType();
            if (_layerCache.ContainsKey(layerType))
            {
                throw new InvalidOperationException($"Layer of type {layerType.Name} is already registered");
            }
            
            _layers.Add(layer);
            _layerCache[layerType] = layer;
            
            // Sort layers by priority
            _layers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        /// <summary>
        /// Get a registered layer by type
        /// </summary>
        public T GetLayer<T>() where T : class, IWorldGenerationLayer
        {
            if (_layerCache.TryGetValue(typeof(T), out var layer))
            {
                return layer as T;
            }
            return null;
        }
        
        /// <summary>
        /// Execute all enabled layers for terrain generation
        /// </summary>
        public void ExecuteLayers(TerrainGenerationContext context)
        {
            Console.WriteLine($"[LayerManager] Executing {_layers.Count(l => l.IsEnabled)} enabled layers for chunk ({context.ChunkX},{context.ChunkZ})");
            
            foreach (var layer in _layers.Where(l => l.IsEnabled))
            {
                try
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    layer.Execute(context);
                    timer.Stop();
                    
                    Console.WriteLine($"[LayerManager] Layer {layer.LayerId} completed in {timer.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LayerManager] Layer {layer.LayerId} failed: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Initialize all core layers
        /// </summary>
        public void InitializeCoreLayers()
        {
            var coreLayers = _layers.OfType<ICoreLayer>();
            Console.WriteLine($"[LayerManager] Initializing {coreLayers.Count()} core layers");
            
            foreach (var coreLayer in coreLayers)
            {
                try
                {
                    coreLayer.InitializeCore();
                    Console.WriteLine($"[LayerManager] Core layer {coreLayer.LayerId} initialized");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LayerManager] Failed to initialize core layer {coreLayer.LayerId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Enable/disable a layer by ID
        /// </summary>
        public void SetLayerEnabled(string layerId, bool enabled)
        {
            var layer = _layers.FirstOrDefault(l => l.LayerId == layerId);
            if (layer != null)
            {
                layer.IsEnabled = enabled;
                Console.WriteLine($"[LayerManager] Layer {layerId} {(enabled ? "enabled" : "disabled")}");
            }
            else
            {
                Console.WriteLine($"[LayerManager] Layer {layerId} not found");
            }
        }
        
        /// <summary>
        /// Get all registered layers
        /// </summary>
        public IReadOnlyList<IWorldGenerationLayer> GetAllLayers()
        {
            return _layers.AsReadOnly();
        }
        
        /// <summary>
        /// Get layers by category
        /// </summary>
        public IEnumerable<T> GetLayersByCategory<T>() where T : IWorldGenerationLayer
        {
            return _layers.OfType<T>();
        }
        
        /// <summary>
        /// Clear all registered layers
        /// </summary>
        public void ClearLayers()
        {
            _layers.Clear();
            _layerCache.Clear();
            Console.WriteLine("[LayerManager] All layers cleared");
        }
        
        /// <summary>
        /// Get execution statistics
        /// </summary>
        public LayerExecutionStats GetExecutionStats()
        {
            return new LayerExecutionStats
            {
                TotalLayers = _layers.Count,
                EnabledLayers = _layers.Count(l => l.IsEnabled),
                CoreLayers = _layers.OfType<ICoreLayer>().Count(),
                ContentLayers = _layers.OfType<IContentLayer>().Count(),
                UtilityLayers = _layers.OfType<IUtilityLayer>().Count()
            };
        }
    }
    
    /// <summary>
    /// Statistics about layer execution
    /// </summary>
    public class LayerExecutionStats
    {
        public int TotalLayers { get; set; }
        public int EnabledLayers { get; set; }
        public int CoreLayers { get; set; }
        public int ContentLayers { get; set; }
        public int UtilityLayers { get; set; }
        
        public override string ToString()
        {
            return $"Layers: {EnabledLayers}/{TotalLayers} enabled " +
                   $"(Core: {CoreLayers}, Content: {ContentLayers}, Utility: {UtilityLayers})";
        }
    }
}
