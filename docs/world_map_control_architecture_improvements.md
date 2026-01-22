# World Map Control Architecture Improvements

**Date:** 2026-01-22  
**Session:** Session 10  
**Status:** Design Complete, Implementation In Progress

## Overview

This document outlines the improvements to the world map control architecture for both client and server, focusing on better integration with optimized terrain generators, improved caching, and enhanced client-server synchronization.

---

## 1. Current Architecture Analysis

### 1.1 Server-Side Architecture

**Components:**
- [`WorldMapControlManager`](GameServer/World/WorldMapControlManager.cs) - Request handler for world map operations
- [`WorldMapController`](GameServer/World/WorldMapController.cs) - Chunk generation and caching
- [`EnhancedTerrainGenerationPipeline`](GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs) - Terrain generation pipeline
- [`WorldMapControlProfile`](GameServer/World/WorldMapControlProfile.cs) - Profile management

**Current Features:**
- Profile-based terrain generation with signature-based cache invalidation
- Chunk caching with budget enforcement
- Automatic profile reloading on config changes
- Generation signature computation for cache validation
- Concurrent chunk generation with task deduplication

**Strengths:**
1. **Profile System** - Well-designed profile-based configuration
2. **Cache Invalidation** - Signature-based cache invalidation
3. **Concurrent Generation** - Efficient concurrent chunk generation
4. **Automatic Reloading** - Automatic config reloading

**Weaknesses:**
1. **Limited Biome Support** - No biome-aware generation
2. **Single Pipeline** - Only one terrain generation pipeline
3. **No Optimization** - Doesn't use optimized generators
4. **Limited Client Integration** - Limited client-server synchronization

### 1.2 Client-Side Architecture

**Components:**
- [`WorldMapControlSystem`](Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs) - Client-side world map control
- [`EnhancedWorldMapController`](Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs) - Enhanced controller
- [`WorldMapController`](Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs) - Legacy controller

**Current Features:**
- Profile loading and management
- Map preview generation
- Profile signature validation
- Automatic profile reloading

**Strengths:**
1. **Profile Management** - Good profile loading and management
2. **Preview Generation** - Map preview generation
3. **Signature Validation** - Profile signature validation

**Weaknesses:**
1. **Multiple Implementations** - Multiple conflicting implementations
2. **Limited Server Integration** - Limited server synchronization
3. **No Optimization** - Doesn't use optimized generators
4. **Inconsistent Architecture** - Inconsistent with server architecture

---

## 2. Proposed Architecture Improvements

### 2.1 Unified World Map Control System

**Goal:** Create a unified world map control system that works seamlessly on both client and server.

**Components:**

#### Server-Side Components
1. **OptimizedWorldMapController**
   - Integrates [`OptimizedCaveGenerator`](GameServer/World/Generation/OptimizedCaveGenerator.cs)
   - Integrates [`OptimizedRiverGenerator`](GameServer/World/Generation/OptimizedRiverGenerator.cs)
   - Integrates [`OptimizedLakeGenerator`](GameServer/World/Generation/OptimizedLakeGenerator.cs)
   - Biome-aware terrain generation
   - Enhanced caching with LRU eviction
   - Improved error handling and logging

2. **BiomeManager**
   - Manages biome configurations
   - Provides biome-specific parameters to generators
   - Handles biome transitions
   - Supports dynamic biome loading

3. **TerrainGeneratorFactory**
   - Factory for creating terrain generators
   - Supports multiple generator types
   - Allows runtime generator switching
   - Integrates with biome system

4. **WorldMapProfileManager**
   - Enhanced profile management
   - Profile versioning
   - Profile migration support
   - Profile validation

#### Client-Side Components
1. **ClientWorldMapController**
   - Matches server architecture
   - Client-side caching
   - Predictive chunk loading
   - Network-aware loading

2. **ClientProfileManager**
   - Client-side profile management
   - Profile synchronization with server
   - Profile conflict resolution
   - Profile backup and restore

3. **ChunkPredictionSystem**
   - Predictive chunk loading
   - Player movement prediction
   - Priority-based loading
   - Adaptive loading distance

### 2.2 Enhanced Protocol Integration

**Goal:** Integrate world map control with improved protobuf protocol validation.

**Components:**

1. **WorldMapProtocolHandler**
   - Handles world map protocol messages
   - Uses [`ProtocolValidation`](SharedProtocol/ProtocolValidation.cs)
   - Message validation and error handling
   - Protocol version negotiation

2. **WorldMapMessageTypes**
   - Enhanced world map message types
   - Versioned messages
   - Backward compatibility
   - Message compression

### 2.3 Improved Caching System

**Goal:** Implement an improved caching system with better performance and memory management.

**Components:**

1. **LRUChunkCache**
   - LRU (Least Recently Used) cache eviction
   - Configurable cache size
   - Cache statistics and monitoring
   - Cache warming

2. **ChunkPrefetcher**
   - Predictive chunk prefetching
   - Player movement prediction
   - Priority-based prefetching
   - Network bandwidth management

3. **CacheCoordinator**
   - Coordinates cache operations
   - Cache invalidation management
   - Cache synchronization
   - Cache performance monitoring

---

## 3. Implementation Details

### 3.1 OptimizedWorldMapController

```csharp
public sealed class OptimizedWorldMapController : IDisposable
{
    private readonly ILogger<OptimizedWorldMapController> logger;
    private readonly WorldSettings worldSettings;
    private readonly WorldGenerationConfig generationConfig;
    private readonly BiomeManager biomeManager;
    private readonly TerrainGeneratorFactory generatorFactory;
    private readonly LRUChunkCache chunkCache;
    private readonly ChunkPrefetcher prefetcher;
    private readonly CacheCoordinator cacheCoordinator;
    
    // Optimized generators
    private OptimizedCaveGenerator caveGenerator;
    private OptimizedRiverGenerator riverGenerator;
    private OptimizedLakeGenerator lakeGenerator;
    
    public OptimizedWorldMapController(
        ILogger<OptimizedWorldMapController> logger,
        WorldSettings worldSettings,
        WorldGenerationConfig generationConfig,
        BiomeManager biomeManager,
        TerrainGeneratorFactory generatorFactory)
    {
        this.logger = logger;
        this.worldSettings = worldSettings;
        this.generationConfig = generationConfig;
        this.biomeManager = biomeManager;
        this.generatorFactory = generatorFactory;
        
        // Initialize optimized generators
        InitializeGenerators();
        
        // Initialize caching system
        InitializeCaching();
    }
    
    private void InitializeGenerators()
    {
        // Create optimized generators with biome support
        var caveConfig = generationConfig.Caves;
        var riverConfig = generationConfig.Water;
        var lakeConfig = generationConfig.Lakes;
        
        caveGenerator = new OptimizedCaveGenerator(caveConfig, biomeManager.GetBiomeConfig());
        riverGenerator = new OptimizedRiverGenerator(riverConfig, biomeManager.GetBiomeConfig());
        lakeGenerator = new OptimizedLakeGenerator(lakeConfig, biomeManager.GetBiomeConfig());
    }
    
    private void InitializeCaching()
    {
        // Initialize LRU cache
        var cacheSize = generationConfig.RenderDistance * generationConfig.RenderDistance * 4;
        chunkCache = new LRUChunkCache(cacheSize);
        
        // Initialize prefetcher
        prefetcher = new ChunkPrefetcher(logger, worldSettings);
        
        // Initialize cache coordinator
        cacheCoordinator = new CacheCoordinator(logger, chunkCache, prefetcher);
    }
    
    public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cached = chunkCache.Get(chunkX, chunkZ);
        if (cached != null)
        {
            return cached;
        }
        
        // Get biome for chunk
        var biome = biomeManager.GetBiomeAt(chunkX, chunkZ);
        
        // Generate chunk with optimized generators
        var chunk = await GenerateChunkAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Cache the chunk
        chunkCache.Put(chunkX, chunkZ, chunk);
        
        // Trigger prefetching
        prefetcher.PrefetchAround(chunkX, chunkZ);
        
        return chunk;
    }
    
    private async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ, Biome biome, CancellationToken cancellationToken)
    {
        // Generate hydrology mask
        var hydrologyMask = await GenerateHydrologyMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate flow mask
        var flowMask = await GenerateFlowMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate erosion risk mask
        var erosionRiskMask = await GenerateErosionRiskMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate cave mask with optimized generator
        var caveMask = caveGenerator.GenerateCaveMask(
            generationConfig.ChunkSize, generationConfig.WorldHeight, generationConfig.ChunkSize,
            hydrologyMask, flowMask, erosionRiskMask, biome.Name);
        
        // Generate river mask with optimized generator
        var riverMask = riverGenerator.GenerateRiverMask(
            generationConfig.ChunkSize, generationConfig.ChunkSize,
            flowMask, erosionRiskMask, hydrologyMask, biome.Name);
        
        // Generate lake mask with optimized generator
        var lakeMask = lakeGenerator.GenerateLakeMask(
            generationConfig.ChunkSize, generationConfig.ChunkSize,
            hydrologyMask, flowMask, erosionRiskMask, biome.Name);
        
        // Combine masks into chunk data
        return CombineMasksIntoChunk(chunkX, chunkZ, caveMask, riverMask, lakeMask);
    }
    
    public void Dispose()
    {
        chunkCache?.Dispose();
        prefetcher?.Dispose();
        cacheCoordinator?.Dispose();
    }
}
```

### 3.2 BiomeManager

```csharp
public sealed class BiomeManager
{
    private readonly ConcurrentDictionary<string, BiomeConfig> biomeConfigs;
    private readonly BiomeMap biomeMap;
    
    public BiomeManager(BiomeMap biomeMap)
    {
        this.biomeMap = biomeMap;
        biomeConfigs = new ConcurrentDictionary<string, BiomeConfig>();
        InitializeBiomes();
    }
    
    private void InitializeBiomes()
    {
        // Load biome configurations from JSON
        var biomeConfigPath = "config/biomes.json";
        var biomeData = File.ReadAllText(biomeConfigPath);
        var biomeList = JsonSerializer.Deserialize<List<BiomeConfig>>(biomeData);
        
        foreach (var biome in biomeList)
        {
            biomeConfigs[biome.Name] = biome;
        }
    }
    
    public BiomeConfig GetBiomeConfig(string biomeName)
    {
        return biomeConfigs.TryGetValue(biomeName, out var config) ? config : GetDefaultBiomeConfig();
    }
    
    public BiomeConfig GetBiomeAt(int chunkX, int chunkZ)
    {
        // Get biome at chunk position
        var biomeName = biomeMap.GetBiomeAt(chunkX, chunkZ);
        return GetBiomeConfig(biomeName);
    }
    
    public BiomeConfig GetDefaultBiomeConfig()
    {
        return biomeConfigs.TryGetValue("plains", out var config) ? config : new BiomeConfig();
    }
}
```

### 3.3 LRUChunkCache

```csharp
public sealed class LRUChunkCache : IDisposable
{
    private readonly int maxSize;
    private readonly LinkedList<CacheEntry> lruList;
    private readonly Dictionary<(int X, int Z), LinkedListNode<CacheEntry>> cacheMap;
    private readonly ILogger<LRUChunkCache> logger;
    private readonly object lockObject = new();
    
    public LRUChunkCache(int maxSize, ILogger<LRUChunkCache> logger = null)
    {
        this.maxSize = maxSize;
        this.logger = logger;
        lruList = new LinkedList<CacheEntry>();
        cacheMap = new Dictionary<(int X, int Z), LinkedListNode<CacheEntry>>();
    }
    
    public ChunkData Get(int chunkX, int chunkZ)
    {
        lock (lockObject)
        {
            var key = (chunkX, chunkZ);
            if (cacheMap.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                lruList.Remove(node);
                lruList.AddFirst(node);
                
                logger?.LogDebug($"Cache hit: ({chunkX}, {chunkZ})");
                return node.Value.Chunk;
            }
            
            logger?.LogDebug($"Cache miss: ({chunkX}, {chunkZ})");
            return null;
        }
    }
    
    public void Put(int chunkX, int chunkZ, ChunkData chunk)
    {
        lock (lockObject)
        {
            var key = (chunkX, chunkZ);
            
            // Remove existing entry if present
            if (cacheMap.TryGetValue(key, out var existingNode))
            {
                lruList.Remove(existingNode);
                cacheMap.Remove(key);
            }
            
            // Create new entry
            var entry = new CacheEntry { ChunkX = chunkX, ChunkZ = chunkZ, Chunk = chunk };
            var node = new LinkedListNode<CacheEntry>(entry);
            
            // Add to front (most recently used)
            lruList.AddFirst(node);
            cacheMap[key] = node;
            
            // Evict if over capacity
            while (lruList.Count > maxSize)
            {
                var lruNode = lruList.Last;
                lruList.RemoveLast();
                cacheMap.Remove((lruNode.Value.ChunkX, lruNode.Value.ChunkZ));
                
                logger?.LogDebug($"Evicted: ({lruNode.Value.ChunkX}, {lruNode.Value.ChunkZ})");
            }
        }
    }
    
    public void Clear()
    {
        lock (lockObject)
        {
            lruList.Clear();
            cacheMap.Clear();
        }
    }
    
    public void Dispose()
    {
        Clear();
    }
    
    private class CacheEntry
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public ChunkData Chunk { get; set; }
    }
}
```

---

## 4. Client-Server Synchronization

### 4.1 Profile Synchronization

**Goal:** Ensure client and server profiles are synchronized.

**Mechanism:**
1. Client sends profile hash to server
2. Server validates profile hash
3. Server sends profile updates if needed
4. Client applies profile updates
5. Client acknowledges profile update

**Protocol Messages:**
```protobuf
message ProfileSyncRequest {
    string profile_hash = 1;
    int player_id = 2;
}

message ProfileSyncResponse {
    bool success = 1;
    string error_message = 2;
    WorldMapControlProfile profile = 3;
    bool profile_updated = 4;
}

message ProfileUpdateBroadcast {
    WorldMapControlProfile profile = 1;
    string update_reason = 2;
}
```

### 4.2 Chunk Synchronization

**Goal:** Efficient chunk synchronization between client and server.

**Mechanism:**
1. Client requests chunks
2. Server generates chunks with optimized generators
3. Server sends chunk data
4. Client caches chunks
5. Client validates chunk data

**Protocol Messages:**
```protobuf
message ChunkDataRequest {
    int chunk_x = 1;
    int chunk_z = 2;
    string profile_hash = 3;
}

message ChunkDataResponse {
    bool success = 1;
    string error_message = 2;
    ChunkData chunk = 3;
    string generation_signature = 4;
}
```

---

## 5. Performance Optimizations

### 5.1 Multi-threading

**Improvements:**
- Parallel chunk generation
- Parallel mask generation
- Parallel biome processing
- Parallel cache operations

### 5.2 Caching

**Improvements:**
- LRU cache eviction
- Cache warming
- Prefetching
- Cache compression

### 5.3 Memory Management

**Improvements:**
- Object pooling
- Memory limits
- Garbage collection optimization
- Memory profiling

---

## 6. Implementation Priority

### High Priority (Session 10)
1. Integrate optimized terrain generators into world map control
2. Implement LRU chunk cache
3. Add biome manager
4. Implement profile synchronization

### Medium Priority (Session 11)
1. Implement chunk prefetching
2. Add cache coordinator
3. Implement client-side caching
4. Add performance monitoring

### Low Priority (Session 12+)
1. Implement advanced caching strategies
2. Add machine learning for prefetching
3. Implement distributed caching
4. Add cache analytics

---

## 7. Testing Strategy

### 7.1 Unit Tests

**Test Areas:**
- Chunk generation with optimized generators
- Biome management
- Cache operations
- Profile synchronization

### 7.2 Integration Tests

**Test Areas:**
- Client-server profile synchronization
- Chunk synchronization
- Cache invalidation
- Error handling

### 7.3 Performance Tests

**Test Areas:**
- Chunk generation performance
- Cache hit rates
- Memory usage
- Network bandwidth

---

## 8. Documentation Requirements

### 8.1 Developer Documentation

**Required Documentation:**
- Architecture overview
- API documentation
- Configuration guide
- Troubleshooting guide

### 8.2 User Documentation

**Required Documentation:**
- User guide
- Configuration reference
- Performance tuning guide
- FAQ

---

## 9. Conclusion

The proposed world map control architecture improvements focus on:

1. **Integration with Optimized Generators** - Using the new optimized cave, river, and lake generators
2. **Biome-Aware Generation** - Adding biome support for more varied terrain
3. **Improved Caching** - Implementing LRU cache with prefetching
4. **Better Client-Server Synchronization** - Enhanced profile and chunk synchronization
5. **Performance Optimizations** - Multi-threading, caching, and memory management

These improvements will result in:
- Better terrain generation performance
- More varied and interesting terrain
- Improved client-server synchronization
- Better memory management
- Enhanced user experience

---

**Next Steps:**
1. Implement OptimizedWorldMapController
2. Implement BiomeManager
3. Implement LRUChunkCache
4. Implement profile synchronization
5. Add performance monitoring
6. Create comprehensive test suite
7. Update documentation

**References:**
- [`GameServer/World/WorldMapControlManager.cs`](GameServer/World/WorldMapControlManager.cs)
- [`GameServer/World/WorldMapController.cs`](GameServer/World/WorldMapController.cs)
- [`GameServer/World/Generation/OptimizedCaveGenerator.cs`](GameServer/World/Generation/OptimizedCaveGenerator.cs)
- [`GameServer/World/Generation/OptimizedRiverGenerator.cs`](GameServer/World/Generation/OptimizedRiverGenerator.cs)
- [`GameServer/World/Generation/OptimizedLakeGenerator.cs`](GameServer/World/Generation/OptimizedLakeGenerator.cs)
- [`SharedProtocol/ProtocolValidation.cs`](SharedProtocol/ProtocolValidation.cs)

**Date:** 2026-01-22  
**Session:** Session 10  
**Status:** Design Complete, Implementation In Progress

## Overview

This document outlines the improvements to the world map control architecture for both client and server, focusing on better integration with optimized terrain generators, improved caching, and enhanced client-server synchronization.

---

## 1. Current Architecture Analysis

### 1.1 Server-Side Architecture

**Components:**
- [`WorldMapControlManager`](GameServer/World/WorldMapControlManager.cs) - Request handler for world map operations
- [`WorldMapController`](GameServer/World/WorldMapController.cs) - Chunk generation and caching
- [`EnhancedTerrainGenerationPipeline`](GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs) - Terrain generation pipeline
- [`WorldMapControlProfile`](GameServer/World/WorldMapControlProfile.cs) - Profile management

**Current Features:**
- Profile-based terrain generation with signature-based cache invalidation
- Chunk caching with budget enforcement
- Automatic profile reloading on config changes
- Generation signature computation for cache validation
- Concurrent chunk generation with task deduplication

**Strengths:**
1. **Profile System** - Well-designed profile-based configuration
2. **Cache Invalidation** - Signature-based cache invalidation
3. **Concurrent Generation** - Efficient concurrent chunk generation
4. **Automatic Reloading** - Automatic config reloading

**Weaknesses:**
1. **Limited Biome Support** - No biome-aware generation
2. **Single Pipeline** - Only one terrain generation pipeline
3. **No Optimization** - Doesn't use optimized generators
4. **Limited Client Integration** - Limited client-server synchronization

### 1.2 Client-Side Architecture

**Components:**
- [`WorldMapControlSystem`](Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs) - Client-side world map control
- [`EnhancedWorldMapController`](Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs) - Enhanced controller
- [`WorldMapController`](Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs) - Legacy controller

**Current Features:**
- Profile loading and management
- Map preview generation
- Profile signature validation
- Automatic profile reloading

**Strengths:**
1. **Profile Management** - Good profile loading and management
2. **Preview Generation** - Map preview generation
3. **Signature Validation** - Profile signature validation

**Weaknesses:**
1. **Multiple Implementations** - Multiple conflicting implementations
2. **Limited Server Integration** - Limited server synchronization
3. **No Optimization** - Doesn't use optimized generators
4. **Inconsistent Architecture** - Inconsistent with server architecture

---

## 2. Proposed Architecture Improvements

### 2.1 Unified World Map Control System

**Goal:** Create a unified world map control system that works seamlessly on both client and server.

**Components:**

#### Server-Side Components
1. **OptimizedWorldMapController**
   - Integrates [`OptimizedCaveGenerator`](GameServer/World/Generation/OptimizedCaveGenerator.cs)
   - Integrates [`OptimizedRiverGenerator`](GameServer/World/Generation/OptimizedRiverGenerator.cs)
   - Integrates [`OptimizedLakeGenerator`](GameServer/World/Generation/OptimizedLakeGenerator.cs)
   - Biome-aware terrain generation
   - Enhanced caching with LRU eviction
   - Improved error handling and logging

2. **BiomeManager**
   - Manages biome configurations
   - Provides biome-specific parameters to generators
   - Handles biome transitions
   - Supports dynamic biome loading

3. **TerrainGeneratorFactory**
   - Factory for creating terrain generators
   - Supports multiple generator types
   - Allows runtime generator switching
   - Integrates with biome system

4. **WorldMapProfileManager**
   - Enhanced profile management
   - Profile versioning
   - Profile migration support
   - Profile validation

#### Client-Side Components
1. **ClientWorldMapController**
   - Matches server architecture
   - Client-side caching
   - Predictive chunk loading
   - Network-aware loading

2. **ClientProfileManager**
   - Client-side profile management
   - Profile synchronization with server
   - Profile conflict resolution
   - Profile backup and restore

3. **ChunkPredictionSystem**
   - Predictive chunk loading
   - Player movement prediction
   - Priority-based loading
   - Adaptive loading distance

### 2.2 Enhanced Protocol Integration

**Goal:** Integrate world map control with improved protobuf protocol validation.

**Components:**

1. **WorldMapProtocolHandler**
   - Handles world map protocol messages
   - Uses [`ProtocolValidation`](SharedProtocol/ProtocolValidation.cs)
   - Message validation and error handling
   - Protocol version negotiation

2. **WorldMapMessageTypes**
   - Enhanced world map message types
   - Versioned messages
   - Backward compatibility
   - Message compression

### 2.3 Improved Caching System

**Goal:** Implement an improved caching system with better performance and memory management.

**Components:**

1. **LRUChunkCache**
   - LRU (Least Recently Used) cache eviction
   - Configurable cache size
   - Cache statistics and monitoring
   - Cache warming

2. **ChunkPrefetcher**
   - Predictive chunk prefetching
   - Player movement prediction
   - Priority-based prefetching
   - Network bandwidth management

3. **CacheCoordinator**
   - Coordinates cache operations
   - Cache invalidation management
   - Cache synchronization
   - Cache performance monitoring

---

## 3. Implementation Details

### 3.1 OptimizedWorldMapController

```csharp
public sealed class OptimizedWorldMapController : IDisposable
{
    private readonly ILogger<OptimizedWorldMapController> logger;
    private readonly WorldSettings worldSettings;
    private readonly WorldGenerationConfig generationConfig;
    private readonly BiomeManager biomeManager;
    private readonly TerrainGeneratorFactory generatorFactory;
    private readonly LRUChunkCache chunkCache;
    private readonly ChunkPrefetcher prefetcher;
    private readonly CacheCoordinator cacheCoordinator;
    
    // Optimized generators
    private OptimizedCaveGenerator caveGenerator;
    private OptimizedRiverGenerator riverGenerator;
    private OptimizedLakeGenerator lakeGenerator;
    
    public OptimizedWorldMapController(
        ILogger<OptimizedWorldMapController> logger,
        WorldSettings worldSettings,
        WorldGenerationConfig generationConfig,
        BiomeManager biomeManager,
        TerrainGeneratorFactory generatorFactory)
    {
        this.logger = logger;
        this.worldSettings = worldSettings;
        this.generationConfig = generationConfig;
        this.biomeManager = biomeManager;
        this.generatorFactory = generatorFactory;
        
        // Initialize optimized generators
        InitializeGenerators();
        
        // Initialize caching system
        InitializeCaching();
    }
    
    private void InitializeGenerators()
    {
        // Create optimized generators with biome support
        var caveConfig = generationConfig.Caves;
        var riverConfig = generationConfig.Water;
        var lakeConfig = generationConfig.Lakes;
        
        caveGenerator = new OptimizedCaveGenerator(caveConfig, biomeManager.GetBiomeConfig());
        riverGenerator = new OptimizedRiverGenerator(riverConfig, biomeManager.GetBiomeConfig());
        lakeGenerator = new OptimizedLakeGenerator(lakeConfig, biomeManager.GetBiomeConfig());
    }
    
    private void InitializeCaching()
    {
        // Initialize LRU cache
        var cacheSize = generationConfig.RenderDistance * generationConfig.RenderDistance * 4;
        chunkCache = new LRUChunkCache(cacheSize);
        
        // Initialize prefetcher
        prefetcher = new ChunkPrefetcher(logger, worldSettings);
        
        // Initialize cache coordinator
        cacheCoordinator = new CacheCoordinator(logger, chunkCache, prefetcher);
    }
    
    public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cached = chunkCache.Get(chunkX, chunkZ);
        if (cached != null)
        {
            return cached;
        }
        
        // Get biome for chunk
        var biome = biomeManager.GetBiomeAt(chunkX, chunkZ);
        
        // Generate chunk with optimized generators
        var chunk = await GenerateChunkAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Cache the chunk
        chunkCache.Put(chunkX, chunkZ, chunk);
        
        // Trigger prefetching
        prefetcher.PrefetchAround(chunkX, chunkZ);
        
        return chunk;
    }
    
    private async Task<ChunkData> GenerateChunkAsync(int chunkX, int chunkZ, Biome biome, CancellationToken cancellationToken)
    {
        // Generate hydrology mask
        var hydrologyMask = await GenerateHydrologyMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate flow mask
        var flowMask = await GenerateFlowMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate erosion risk mask
        var erosionRiskMask = await GenerateErosionRiskMaskAsync(chunkX, chunkZ, biome, cancellationToken);
        
        // Generate cave mask with optimized generator
        var caveMask = caveGenerator.GenerateCaveMask(
            generationConfig.ChunkSize, generationConfig.WorldHeight, generationConfig.ChunkSize,
            hydrologyMask, flowMask, erosionRiskMask, biome.Name);
        
        // Generate river mask with optimized generator
        var riverMask = riverGenerator.GenerateRiverMask(
            generationConfig.ChunkSize, generationConfig.ChunkSize,
            flowMask, erosionRiskMask, hydrologyMask, biome.Name);
        
        // Generate lake mask with optimized generator
        var lakeMask = lakeGenerator.GenerateLakeMask(
            generationConfig.ChunkSize, generationConfig.ChunkSize,
            hydrologyMask, flowMask, erosionRiskMask, biome.Name);
        
        // Combine masks into chunk data
        return CombineMasksIntoChunk(chunkX, chunkZ, caveMask, riverMask, lakeMask);
    }
    
    public void Dispose()
    {
        chunkCache?.Dispose();
        prefetcher?.Dispose();
        cacheCoordinator?.Dispose();
    }
}
```

### 3.2 BiomeManager

```csharp
public sealed class BiomeManager
{
    private readonly ConcurrentDictionary<string, BiomeConfig> biomeConfigs;
    private readonly BiomeMap biomeMap;
    
    public BiomeManager(BiomeMap biomeMap)
    {
        this.biomeMap = biomeMap;
        biomeConfigs = new ConcurrentDictionary<string, BiomeConfig>();
        InitializeBiomes();
    }
    
    private void InitializeBiomes()
    {
        // Load biome configurations from JSON
        var biomeConfigPath = "config/biomes.json";
        var biomeData = File.ReadAllText(biomeConfigPath);
        var biomeList = JsonSerializer.Deserialize<List<BiomeConfig>>(biomeData);
        
        foreach (var biome in biomeList)
        {
            biomeConfigs[biome.Name] = biome;
        }
    }
    
    public BiomeConfig GetBiomeConfig(string biomeName)
    {
        return biomeConfigs.TryGetValue(biomeName, out var config) ? config : GetDefaultBiomeConfig();
    }
    
    public BiomeConfig GetBiomeAt(int chunkX, int chunkZ)
    {
        // Get biome at chunk position
        var biomeName = biomeMap.GetBiomeAt(chunkX, chunkZ);
        return GetBiomeConfig(biomeName);
    }
    
    public BiomeConfig GetDefaultBiomeConfig()
    {
        return biomeConfigs.TryGetValue("plains", out var config) ? config : new BiomeConfig();
    }
}
```

### 3.3 LRUChunkCache

```csharp
public sealed class LRUChunkCache : IDisposable
{
    private readonly int maxSize;
    private readonly LinkedList<CacheEntry> lruList;
    private readonly Dictionary<(int X, int Z), LinkedListNode<CacheEntry>> cacheMap;
    private readonly ILogger<LRUChunkCache> logger;
    private readonly object lockObject = new();
    
    public LRUChunkCache(int maxSize, ILogger<LRUChunkCache> logger = null)
    {
        this.maxSize = maxSize;
        this.logger = logger;
        lruList = new LinkedList<CacheEntry>();
        cacheMap = new Dictionary<(int X, int Z), LinkedListNode<CacheEntry>>();
    }
    
    public ChunkData Get(int chunkX, int chunkZ)
    {
        lock (lockObject)
        {
            var key = (chunkX, chunkZ);
            if (cacheMap.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                lruList.Remove(node);
                lruList.AddFirst(node);
                
                logger?.LogDebug($"Cache hit: ({chunkX}, {chunkZ})");
                return node.Value.Chunk;
            }
            
            logger?.LogDebug($"Cache miss: ({chunkX}, {chunkZ})");
            return null;
        }
    }
    
    public void Put(int chunkX, int chunkZ, ChunkData chunk)
    {
        lock (lockObject)
        {
            var key = (chunkX, chunkZ);
            
            // Remove existing entry if present
            if (cacheMap.TryGetValue(key, out var existingNode))
            {
                lruList.Remove(existingNode);
                cacheMap.Remove(key);
            }
            
            // Create new entry
            var entry = new CacheEntry { ChunkX = chunkX, ChunkZ = chunkZ, Chunk = chunk };
            var node = new LinkedListNode<CacheEntry>(entry);
            
            // Add to front (most recently used)
            lruList.AddFirst(node);
            cacheMap[key] = node;
            
            // Evict if over capacity
            while (lruList.Count > maxSize)
            {
                var lruNode = lruList.Last;
                lruList.RemoveLast();
                cacheMap.Remove((lruNode.Value.ChunkX, lruNode.Value.ChunkZ));
                
                logger?.LogDebug($"Evicted: ({lruNode.Value.ChunkX}, {lruNode.Value.ChunkZ})");
            }
        }
    }
    
    public void Clear()
    {
        lock (lockObject)
        {
            lruList.Clear();
            cacheMap.Clear();
        }
    }
    
    public void Dispose()
    {
        Clear();
    }
    
    private class CacheEntry
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public ChunkData Chunk { get; set; }
    }
}
```

---

## 4. Client-Server Synchronization

### 4.1 Profile Synchronization

**Goal:** Ensure client and server profiles are synchronized.

**Mechanism:**
1. Client sends profile hash to server
2. Server validates profile hash
3. Server sends profile updates if needed
4. Client applies profile updates
5. Client acknowledges profile update

**Protocol Messages:**
```protobuf
message ProfileSyncRequest {
    string profile_hash = 1;
    int player_id = 2;
}

message ProfileSyncResponse {
    bool success = 1;
    string error_message = 2;
    WorldMapControlProfile profile = 3;
    bool profile_updated = 4;
}

message ProfileUpdateBroadcast {
    WorldMapControlProfile profile = 1;
    string update_reason = 2;
}
```

### 4.2 Chunk Synchronization

**Goal:** Efficient chunk synchronization between client and server.

**Mechanism:**
1. Client requests chunks
2. Server generates chunks with optimized generators
3. Server sends chunk data
4. Client caches chunks
5. Client validates chunk data

**Protocol Messages:**
```protobuf
message ChunkDataRequest {
    int chunk_x = 1;
    int chunk_z = 2;
    string profile_hash = 3;
}

message ChunkDataResponse {
    bool success = 1;
    string error_message = 2;
    ChunkData chunk = 3;
    string generation_signature = 4;
}
```

---

## 5. Performance Optimizations

### 5.1 Multi-threading

**Improvements:**
- Parallel chunk generation
- Parallel mask generation
- Parallel biome processing
- Parallel cache operations

### 5.2 Caching

**Improvements:**
- LRU cache eviction
- Cache warming
- Prefetching
- Cache compression

### 5.3 Memory Management

**Improvements:**
- Object pooling
- Memory limits
- Garbage collection optimization
- Memory profiling

---

## 6. Implementation Priority

### High Priority (Session 10)
1. Integrate optimized terrain generators into world map control
2. Implement LRU chunk cache
3. Add biome manager
4. Implement profile synchronization

### Medium Priority (Session 11)
1. Implement chunk prefetching
2. Add cache coordinator
3. Implement client-side caching
4. Add performance monitoring

### Low Priority (Session 12+)
1. Implement advanced caching strategies
2. Add machine learning for prefetching
3. Implement distributed caching
4. Add cache analytics

---

## 7. Testing Strategy

### 7.1 Unit Tests

**Test Areas:**
- Chunk generation with optimized generators
- Biome management
- Cache operations
- Profile synchronization

### 7.2 Integration Tests

**Test Areas:**
- Client-server profile synchronization
- Chunk synchronization
- Cache invalidation
- Error handling

### 7.3 Performance Tests

**Test Areas:**
- Chunk generation performance
- Cache hit rates
- Memory usage
- Network bandwidth

---

## 8. Documentation Requirements

### 8.1 Developer Documentation

**Required Documentation:**
- Architecture overview
- API documentation
- Configuration guide
- Troubleshooting guide

### 8.2 User Documentation

**Required Documentation:**
- User guide
- Configuration reference
- Performance tuning guide
- FAQ

---

## 9. Conclusion

The proposed world map control architecture improvements focus on:

1. **Integration with Optimized Generators** - Using the new optimized cave, river, and lake generators
2. **Biome-Aware Generation** - Adding biome support for more varied terrain
3. **Improved Caching** - Implementing LRU cache with prefetching
4. **Better Client-Server Synchronization** - Enhanced profile and chunk synchronization
5. **Performance Optimizations** - Multi-threading, caching, and memory management

These improvements will result in:
- Better terrain generation performance
- More varied and interesting terrain
- Improved client-server synchronization
- Better memory management
- Enhanced user experience

---

**Next Steps:**
1. Implement OptimizedWorldMapController
2. Implement BiomeManager
3. Implement LRUChunkCache
4. Implement profile synchronization
5. Add performance monitoring
6. Create comprehensive test suite
7. Update documentation

**References:**
- [`GameServer/World/WorldMapControlManager.cs`](GameServer/World/WorldMapControlManager.cs)
- [`GameServer/World/WorldMapController.cs`](GameServer/World/WorldMapController.cs)
- [`GameServer/World/Generation/OptimizedCaveGenerator.cs`](GameServer/World/Generation/OptimizedCaveGenerator.cs)
- [`GameServer/World/Generation/OptimizedRiverGenerator.cs`](GameServer/World/Generation/OptimizedRiverGenerator.cs)
- [`GameServer/World/Generation/OptimizedLakeGenerator.cs`](GameServer/World/Generation/OptimizedLakeGenerator.cs)
- [`SharedProtocol/ProtocolValidation.cs`](SharedProtocol/ProtocolValidation.cs)

