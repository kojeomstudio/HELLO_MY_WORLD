# World Map Control Architecture Improvements
**Date:** 2026-02-07  
**Session:** 54  
**Status:** Analysis Complete

## Overview

This document analyzes the current world map control architecture for both server and client sides, identifies improvements, and provides implementation recommendations.

## Current Architecture

### Server-Side: `WorldMapControlManager.cs` (510 lines)

**Key Components:**
- **Profile Management:** Loads and manages `WorldMapControlProfile` instances
- **Chunk Caching:** `ConcurrentDictionary<(int X, int Z), ChunkData>` for cached chunks
- **Signature Computation:** Comprehensive generation signature using `WorldMapSignatureContext`
- **Hash Verification:** SHA256-based file hash computation for profile and config changes
- **Profile Reload Detection:** Monitors file write times and content hashes

**Strengths:**
1. Comprehensive hash-based change detection
2. Profile version management
3. Chunk cache with budget enforcement
4. Multiple reload triggers (config, profile, signature mismatch)
5. Integration with `EnhancedTerrainGenerationPipeline`

**Weaknesses:**
1. No bidirectional synchronization with client
2. No fallback mechanism when profile validation fails
3. No LRU cache eviction policy
4. Limited error handling and logging
5. No profile diff/patch support for incremental updates

### Client-Side: `WorldMapController.cs` (3187 lines)

**Key Components:**
- **Profile Loading:** Loads profile from StreamingAssets with hash verification
- **Runtime Config Override:** Supports runtime configuration from JSON
- **Chunk Generation:** `EnhancedTerrainGenerator` for local preview chunks
- **Async Chunk Processing:** Background queue with semaphore-controlled concurrency
- **Profile Reload Detection:** Periodic file change monitoring

**Strengths:**
1. Comprehensive terrain generation algorithms mirroring server
2. Async chunk processing with configurable concurrency
3. Runtime configuration override support
4. Profile reload detection with hash verification
5. Hydrology-aware cave, river, and lake generation

**Weaknesses:**
1. No server-client profile synchronization
2. No version negotiation for profile compatibility
3. Limited error recovery mechanisms
4. No chunk priority queue for player-centered loading
5. No cache preloading for movement prediction

## Architecture Improvements

### 1. Bidirectional Profile Synchronization

**Problem:** Client and server profiles can diverge, causing generation inconsistencies.

**Solution:**
```csharp
// Server-side: Add profile sync endpoint
public class ProfileSyncManager
{
    public async Task<WorldMapProfileSyncResponse> HandleProfileSyncAsync(WorldMapProfileSyncRequest request)
    {
        var serverProfile = EnsureProfile(out _);
        var clientProfile = request.ClientProfile;
        
        // Version negotiation
        if (clientProfile.Version > serverProfile.Version)
        {
            return new WorldMapProfileSyncResponse
            {
                Action = SyncAction.UseClientProfile,
                Profile = null,
                RequiresFullReload = true
            };
        }
        
        // Check if profiles match
        if (string.Equals(serverProfile.ProfileHash, clientProfile.ProfileHash, StringComparison.OrdinalIgnoreCase))
        {
            return new WorldMapProfileSyncResponse
            {
                Action = SyncAction.NoChange,
                Profile = null,
                RequiresFullReload = false
            };
        }
        
        // Send server profile
        return new WorldMapProfileSyncResponse
        {
            Action = SyncAction.UseServerProfile,
            Profile = serverProfile,
            RequiresFullReload = true
        };
    }
}

// Client-side: Add profile sync request
public async Task<bool> SyncProfileWithServerAsync()
{
    var request = new WorldMapProfileSyncRequest
    {
        ClientProfile = profile,
        PlayerId = playerId
    };
    
    var response = await networkManager.SendRequestAsync<WorldMapProfileSyncResponse>(request);
    
    switch (response.Action)
    {
        case SyncAction.UseServerProfile:
            profile = response.Profile;
            generator = new EnhancedTerrainGenerator(profile, worldConfig);
            loadedChunks.Clear();
            return true;
            
        case SyncAction.UseClientProfile:
            // Client has newer profile, keep using it
            return true;
            
        case SyncAction.NoChange:
            return true;
            
        default:
            Debug.LogError("[WorldMapController] Unknown sync action");
            return false;
    }
}
```

### 2. Self-Healing Profile Validation

**Problem:** When profile hash mismatches occur, the system may fail without recovery.

**Solution:**
```csharp
public class ProfileValidator
{
    public static ValidationResult ValidateProfile(WorldMapControlProfile profile, WorldConfig config)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate version
        if (profile.Version <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Profile version must be greater than 0");
        }
        
        // Validate hash
        if (string.IsNullOrWhiteSpace(profile.ProfileHash))
        {
            result.IsValid = false;
            result.Errors.Add("Profile hash is empty");
        }
        else
        {
            var computedHash = WorldMapControlProfileUtility.ComputeHash(profile);
            if (!string.Equals(profile.ProfileHash, computedHash, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"Profile hash mismatch: expected {computedHash}, got {profile.ProfileHash}");
            }
        }
        
        // Validate hydrology signature
        if (!string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.Errors.Add($"Hydrology signature mismatch: expected {SharedFeatureCatalog.HydrologySignature}, got {profile.HydrologySignature}");
        }
        
        // Validate ranges
        if (profile.ChunkSize <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Chunk size must be greater than 0");
        }
        
        if (profile.RenderDistance <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Render distance must be greater than 0");
        }
        
        return result;
    }
    
    public static WorldMapControlProfile CreateFallbackProfile(WorldConfig config)
    {
        var profile = WorldMapControlProfileUtility.Create(config, config);
        profile.ProfileHash = WorldMapControlProfileUtility.ComputeHash(profile);
        return profile;
    }
}
```

### 3. LRU Cache Eviction Policy

**Problem:** Current cache eviction is simple FIFO, not optimal for player movement patterns.

**Solution:**
```csharp
public class LruChunkCache
{
    private readonly int maxCacheSize;
    private readonly LinkedList<(int X, int Z)> lruList;
    private readonly Dictionary<(int X, int Z), LinkedListNode<(int X, int Z)>> cacheMap;
    private readonly Dictionary<(int X, int Z), ChunkData> chunkData;
    
    public LruChunkCache(int maxCacheSize)
    {
        this.maxCacheSize = maxCacheSize;
        this.lruList = new LinkedList<(int X, int Z)>();
        this.cacheMap = new Dictionary<(int X, int Z), LinkedListNode<(int X, int Z)>>();
        this.chunkData = new Dictionary<(int X, int Z), ChunkData>();
    }
    
    public bool TryGet((int X, int Z) key, out ChunkData chunk)
    {
        if (cacheMap.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            lruList.Remove(node);
            lruList.AddFirst(node);
            chunk = chunkData[key];
            return true;
        }
        
        chunk = null;
        return false;
    }
    
    public void Add((int X, int Z) key, ChunkData chunk)
    {
        if (cacheMap.ContainsKey(key))
        {
            // Update existing
            TryGet(key, out _);
            chunkData[key] = chunk;
            return;
        }
        
        // Evict if necessary
        while (cacheMap.Count >= maxCacheSize)
        {
            var lru = lruList.Last.Value;
            lruList.RemoveLast();
            cacheMap.Remove(lru);
            chunkData.Remove(lru);
        }
        
        // Add new
        var node = lruList.AddFirst(key);
        cacheMap[key] = node;
        chunkData[key] = chunk;
    }
    
    public void Clear()
    {
        lruList.Clear();
        cacheMap.Clear();
        chunkData.Clear();
    }
    
    public int Count => cacheMap.Count;
}
```

### 4. Chunk Priority Queue

**Problem:** Chunks are loaded in FIFO order, not prioritized by player position.

**Solution:**
```csharp
public class ChunkPriorityQueue
{
    private readonly PriorityQueue<ChunkRequest, float> priorityQueue;
    private readonly HashSet<(int X, int Z)> inQueue;
    
    public ChunkPriorityQueue()
    {
        priorityQueue = new PriorityQueue<ChunkRequest, float>();
        inQueue = new HashSet<(int X, int Z)>();
    }
    
    public void Enqueue(int chunkX, int chunkZ, Vector2Int playerChunk)
    {
        var key = (chunkX, chunkZ);
        if (inQueue.Contains(key))
        {
            return;
        }
        
        // Priority based on distance to player (lower = higher priority)
        var distance = Vector2Int.Distance(new Vector2Int(chunkX, chunkZ), playerChunk);
        priorityQueue.Enqueue(new ChunkRequest { X = chunkX, Z = chunkZ }, distance);
        inQueue.Add(key);
    }
    
    public bool TryDequeue(out ChunkRequest request)
    {
        if (priorityQueue.TryDequeue(out request, out _))
        {
            inQueue.Remove((request.X, request.Z));
            return true;
        }
        
        request = default;
        return false;
    }
    
    public void Clear()
    {
        priorityQueue.Clear();
        inQueue.Clear();
    }
}

public struct ChunkRequest
{
    public int X { get; set; }
    public int Z { get; set; }
}
```

### 5. Comprehensive Error Handling

**Problem:** Limited error handling and logging makes debugging difficult.

**Solution:**
```csharp
public class WorldMapErrorHandler
{
    private readonly ILogger logger;
    private readonly List<WorldMapError> errorHistory;
    private readonly int maxHistorySize = 100;
    
    public WorldMapErrorHandler(ILogger logger)
    {
        this.logger = logger;
        this.errorHistory = new List<WorldMapError>();
    }
    
    public void HandleError(string operation, Exception ex, WorldMapErrorSeverity severity = WorldMapErrorSeverity.Warning)
    {
        var error = new WorldMapError
        {
            Timestamp = DateTime.UtcNow,
            Operation = operation,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            Severity = severity
        };
        
        errorHistory.Add(error);
        if (errorHistory.Count > maxHistorySize)
        {
            errorHistory.RemoveAt(0);
        }
        
        switch (severity)
        {
            case WorldMapErrorSeverity.Debug:
                logger.LogDebug($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Info:
                logger.LogInformation($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Warning:
                logger.LogWarning($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Error:
                logger.LogError(ex, $"[WorldMap] {operation} failed");
                break;
            case WorldMapErrorSeverity.Critical:
                logger.LogCritical(ex, $"[WorldMap] {operation} critical failure");
                break;
        }
    }
    
    public IEnumerable<WorldMapError> GetErrorHistory(WorldMapErrorSeverity? minSeverity = null)
    {
        if (minSeverity.HasValue)
        {
            return errorHistory.Where(e => e.Severity >= minSeverity.Value);
        }
        return errorHistory.AsReadOnly();
    }
}

public enum WorldMapErrorSeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public class WorldMapError
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public WorldMapErrorSeverity Severity { get; set; }
}
```

### 6. Async Profile Loading

**Problem:** Profile loading can block the main thread, causing frame drops.

**Solution:**
```csharp
public class AsyncProfileLoader
{
    private readonly SemaphoreSlim loadSemaphore;
    private readonly Dictionary<string, Task<WorldMapControlProfile>> loadTasks;
    
    public AsyncProfileLoader(int maxConcurrentLoads = 2)
    {
        loadSemaphore = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);
        loadTasks = new Dictionary<string, Task<WorldMapControlProfile>>();
    }
    
    public async Task<WorldMapControlProfile> LoadProfileAsync(string path, WorldConfig config)
    {
        // Check if already loading
        if (loadTasks.TryGetValue(path, out var existingTask))
        {
            return await existingTask;
        }
        
        // Start new load
        var task = LoadProfileInternalAsync(path, config);
        loadTasks[path] = task;
        
        try
        {
            return await task;
        }
        finally
        {
            loadTasks.Remove(path);
        }
    }
    
    private async Task<WorldMapControlProfile> LoadProfileInternalAsync(string path, WorldConfig config)
    {
        await loadSemaphore.WaitAsync();
        try
        {
            return await Task.Run(() => WorldMapControlProfile.LoadFromFile(path, config));
        }
        finally
        {
            loadSemaphore.Release();
        }
    }
}
```

## Implementation Priority

### Phase 1: Critical Improvements (Session 54)
1. **Profile Validation** - Add comprehensive validation with fallback
2. **Error Handling** - Add error logging and recovery
3. **Async Profile Loading** - Prevent main thread blocking

### Phase 2: Performance Improvements (Session 55)
1. **LRU Cache** - Replace FIFO with LRU eviction
2. **Priority Queue** - Prioritize chunks near player
3. **Cache Statistics** - Add monitoring and metrics

### Phase 3: Synchronization (Session 56)
1. **Profile Sync** - Implement server-client synchronization
2. **Version Negotiation** - Handle version mismatches
3. **Profile Diff** - Support incremental updates

## Configuration Recommendations

### Server Config (`config/enhanced_world_map_control_server.json`)
```json
{
  "worldMapControl": {
    "profile": {
      "version": 22,
      "autoReload": true,
      "reloadIntervalSeconds": 5,
      "validation": {
        "enabled": true,
        "strictMode": false,
        "fallbackToDefault": true
      }
    },
    "cache": {
      "maxCachedChunks": 256,
      "evictionPolicy": "LRU",
      "enableStatistics": true
    },
    "sync": {
      "enabled": true,
      "syncIntervalSeconds": 30,
      "versionNegotiation": true
    },
    "logging": {
      "level": "Info",
      "maxErrorHistory": 100,
      "logToFile": true
    }
  }
}
```

### Client Config (`config/enhanced_world_map_control_client.json`)
```json
{
  "worldMapControl": {
    "profile": {
      "autoReload": true,
      "reloadIntervalSeconds": 5,
      "syncWithServer": true,
      "validation": {
        "enabled": true,
        "fallbackToDefault": true
      }
    },
    "defaults": {
      "renderDistance": 4,
      "mapScale": 1.0,
      "showCoordinates": true,
      "showBiomeInfo": false
    },
    "performance": {
      "maxConcurrentChunkRequests": 4,
      "chunkPriorityQueue": true,
      "preloadRadius": 2
    },
    "cache": {
      "maxCachedChunks": 256,
      "evictionPolicy": "LRU",
      "enableStatistics": true
    }
  }
}
```

## Testing Recommendations

1. **Profile Validation Tests**
   - Test with valid profiles
   - Test with corrupted profiles
   - Test with hash mismatches
   - Test fallback profile generation

2. **Cache Performance Tests**
   - Test LRU vs FIFO eviction
   - Test cache hit rates
   - Test memory usage

3. **Synchronization Tests**
   - Test server-client sync
   - Test version negotiation
   - Test concurrent profile updates

4. **Error Recovery Tests**
   - Test profile load failures
   - Test network failures
   - Test hash verification failures

## Conclusion

The current world map control architecture is comprehensive but can be improved with:

1. **Better Synchronization:** Bidirectional profile sync between server and client
2. **Robust Error Handling:** Comprehensive validation with fallback mechanisms
3. **Performance Optimization:** LRU cache, priority queue, async loading
4. **Monitoring:** Cache statistics, error history, performance metrics

These improvements will enhance reliability, performance, and maintainability of the world map control system.
**Date:** 2026-02-07  
**Session:** 54  
**Status:** Analysis Complete

## Overview

This document analyzes the current world map control architecture for both server and client sides, identifies improvements, and provides implementation recommendations.

## Current Architecture

### Server-Side: `WorldMapControlManager.cs` (510 lines)

**Key Components:**
- **Profile Management:** Loads and manages `WorldMapControlProfile` instances
- **Chunk Caching:** `ConcurrentDictionary<(int X, int Z), ChunkData>` for cached chunks
- **Signature Computation:** Comprehensive generation signature using `WorldMapSignatureContext`
- **Hash Verification:** SHA256-based file hash computation for profile and config changes
- **Profile Reload Detection:** Monitors file write times and content hashes

**Strengths:**
1. Comprehensive hash-based change detection
2. Profile version management
3. Chunk cache with budget enforcement
4. Multiple reload triggers (config, profile, signature mismatch)
5. Integration with `EnhancedTerrainGenerationPipeline`

**Weaknesses:**
1. No bidirectional synchronization with client
2. No fallback mechanism when profile validation fails
3. No LRU cache eviction policy
4. Limited error handling and logging
5. No profile diff/patch support for incremental updates

### Client-Side: `WorldMapController.cs` (3187 lines)

**Key Components:**
- **Profile Loading:** Loads profile from StreamingAssets with hash verification
- **Runtime Config Override:** Supports runtime configuration from JSON
- **Chunk Generation:** `EnhancedTerrainGenerator` for local preview chunks
- **Async Chunk Processing:** Background queue with semaphore-controlled concurrency
- **Profile Reload Detection:** Periodic file change monitoring

**Strengths:**
1. Comprehensive terrain generation algorithms mirroring server
2. Async chunk processing with configurable concurrency
3. Runtime configuration override support
4. Profile reload detection with hash verification
5. Hydrology-aware cave, river, and lake generation

**Weaknesses:**
1. No server-client profile synchronization
2. No version negotiation for profile compatibility
3. Limited error recovery mechanisms
4. No chunk priority queue for player-centered loading
5. No cache preloading for movement prediction

## Architecture Improvements

### 1. Bidirectional Profile Synchronization

**Problem:** Client and server profiles can diverge, causing generation inconsistencies.

**Solution:**
```csharp
// Server-side: Add profile sync endpoint
public class ProfileSyncManager
{
    public async Task<WorldMapProfileSyncResponse> HandleProfileSyncAsync(WorldMapProfileSyncRequest request)
    {
        var serverProfile = EnsureProfile(out _);
        var clientProfile = request.ClientProfile;
        
        // Version negotiation
        if (clientProfile.Version > serverProfile.Version)
        {
            return new WorldMapProfileSyncResponse
            {
                Action = SyncAction.UseClientProfile,
                Profile = null,
                RequiresFullReload = true
            };
        }
        
        // Check if profiles match
        if (string.Equals(serverProfile.ProfileHash, clientProfile.ProfileHash, StringComparison.OrdinalIgnoreCase))
        {
            return new WorldMapProfileSyncResponse
            {
                Action = SyncAction.NoChange,
                Profile = null,
                RequiresFullReload = false
            };
        }
        
        // Send server profile
        return new WorldMapProfileSyncResponse
        {
            Action = SyncAction.UseServerProfile,
            Profile = serverProfile,
            RequiresFullReload = true
        };
    }
}

// Client-side: Add profile sync request
public async Task<bool> SyncProfileWithServerAsync()
{
    var request = new WorldMapProfileSyncRequest
    {
        ClientProfile = profile,
        PlayerId = playerId
    };
    
    var response = await networkManager.SendRequestAsync<WorldMapProfileSyncResponse>(request);
    
    switch (response.Action)
    {
        case SyncAction.UseServerProfile:
            profile = response.Profile;
            generator = new EnhancedTerrainGenerator(profile, worldConfig);
            loadedChunks.Clear();
            return true;
            
        case SyncAction.UseClientProfile:
            // Client has newer profile, keep using it
            return true;
            
        case SyncAction.NoChange:
            return true;
            
        default:
            Debug.LogError("[WorldMapController] Unknown sync action");
            return false;
    }
}
```

### 2. Self-Healing Profile Validation

**Problem:** When profile hash mismatches occur, the system may fail without recovery.

**Solution:**
```csharp
public class ProfileValidator
{
    public static ValidationResult ValidateProfile(WorldMapControlProfile profile, WorldConfig config)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate version
        if (profile.Version <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Profile version must be greater than 0");
        }
        
        // Validate hash
        if (string.IsNullOrWhiteSpace(profile.ProfileHash))
        {
            result.IsValid = false;
            result.Errors.Add("Profile hash is empty");
        }
        else
        {
            var computedHash = WorldMapControlProfileUtility.ComputeHash(profile);
            if (!string.Equals(profile.ProfileHash, computedHash, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Errors.Add($"Profile hash mismatch: expected {computedHash}, got {profile.ProfileHash}");
            }
        }
        
        // Validate hydrology signature
        if (!string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.Errors.Add($"Hydrology signature mismatch: expected {SharedFeatureCatalog.HydrologySignature}, got {profile.HydrologySignature}");
        }
        
        // Validate ranges
        if (profile.ChunkSize <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Chunk size must be greater than 0");
        }
        
        if (profile.RenderDistance <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Render distance must be greater than 0");
        }
        
        return result;
    }
    
    public static WorldMapControlProfile CreateFallbackProfile(WorldConfig config)
    {
        var profile = WorldMapControlProfileUtility.Create(config, config);
        profile.ProfileHash = WorldMapControlProfileUtility.ComputeHash(profile);
        return profile;
    }
}
```

### 3. LRU Cache Eviction Policy

**Problem:** Current cache eviction is simple FIFO, not optimal for player movement patterns.

**Solution:**
```csharp
public class LruChunkCache
{
    private readonly int maxCacheSize;
    private readonly LinkedList<(int X, int Z)> lruList;
    private readonly Dictionary<(int X, int Z), LinkedListNode<(int X, int Z)>> cacheMap;
    private readonly Dictionary<(int X, int Z), ChunkData> chunkData;
    
    public LruChunkCache(int maxCacheSize)
    {
        this.maxCacheSize = maxCacheSize;
        this.lruList = new LinkedList<(int X, int Z)>();
        this.cacheMap = new Dictionary<(int X, int Z), LinkedListNode<(int X, int Z)>>();
        this.chunkData = new Dictionary<(int X, int Z), ChunkData>();
    }
    
    public bool TryGet((int X, int Z) key, out ChunkData chunk)
    {
        if (cacheMap.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            lruList.Remove(node);
            lruList.AddFirst(node);
            chunk = chunkData[key];
            return true;
        }
        
        chunk = null;
        return false;
    }
    
    public void Add((int X, int Z) key, ChunkData chunk)
    {
        if (cacheMap.ContainsKey(key))
        {
            // Update existing
            TryGet(key, out _);
            chunkData[key] = chunk;
            return;
        }
        
        // Evict if necessary
        while (cacheMap.Count >= maxCacheSize)
        {
            var lru = lruList.Last.Value;
            lruList.RemoveLast();
            cacheMap.Remove(lru);
            chunkData.Remove(lru);
        }
        
        // Add new
        var node = lruList.AddFirst(key);
        cacheMap[key] = node;
        chunkData[key] = chunk;
    }
    
    public void Clear()
    {
        lruList.Clear();
        cacheMap.Clear();
        chunkData.Clear();
    }
    
    public int Count => cacheMap.Count;
}
```

### 4. Chunk Priority Queue

**Problem:** Chunks are loaded in FIFO order, not prioritized by player position.

**Solution:**
```csharp
public class ChunkPriorityQueue
{
    private readonly PriorityQueue<ChunkRequest, float> priorityQueue;
    private readonly HashSet<(int X, int Z)> inQueue;
    
    public ChunkPriorityQueue()
    {
        priorityQueue = new PriorityQueue<ChunkRequest, float>();
        inQueue = new HashSet<(int X, int Z)>();
    }
    
    public void Enqueue(int chunkX, int chunkZ, Vector2Int playerChunk)
    {
        var key = (chunkX, chunkZ);
        if (inQueue.Contains(key))
        {
            return;
        }
        
        // Priority based on distance to player (lower = higher priority)
        var distance = Vector2Int.Distance(new Vector2Int(chunkX, chunkZ), playerChunk);
        priorityQueue.Enqueue(new ChunkRequest { X = chunkX, Z = chunkZ }, distance);
        inQueue.Add(key);
    }
    
    public bool TryDequeue(out ChunkRequest request)
    {
        if (priorityQueue.TryDequeue(out request, out _))
        {
            inQueue.Remove((request.X, request.Z));
            return true;
        }
        
        request = default;
        return false;
    }
    
    public void Clear()
    {
        priorityQueue.Clear();
        inQueue.Clear();
    }
}

public struct ChunkRequest
{
    public int X { get; set; }
    public int Z { get; set; }
}
```

### 5. Comprehensive Error Handling

**Problem:** Limited error handling and logging makes debugging difficult.

**Solution:**
```csharp
public class WorldMapErrorHandler
{
    private readonly ILogger logger;
    private readonly List<WorldMapError> errorHistory;
    private readonly int maxHistorySize = 100;
    
    public WorldMapErrorHandler(ILogger logger)
    {
        this.logger = logger;
        this.errorHistory = new List<WorldMapError>();
    }
    
    public void HandleError(string operation, Exception ex, WorldMapErrorSeverity severity = WorldMapErrorSeverity.Warning)
    {
        var error = new WorldMapError
        {
            Timestamp = DateTime.UtcNow,
            Operation = operation,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            Severity = severity
        };
        
        errorHistory.Add(error);
        if (errorHistory.Count > maxHistorySize)
        {
            errorHistory.RemoveAt(0);
        }
        
        switch (severity)
        {
            case WorldMapErrorSeverity.Debug:
                logger.LogDebug($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Info:
                logger.LogInformation($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Warning:
                logger.LogWarning($"[WorldMap] {operation}: {ex.Message}");
                break;
            case WorldMapErrorSeverity.Error:
                logger.LogError(ex, $"[WorldMap] {operation} failed");
                break;
            case WorldMapErrorSeverity.Critical:
                logger.LogCritical(ex, $"[WorldMap] {operation} critical failure");
                break;
        }
    }
    
    public IEnumerable<WorldMapError> GetErrorHistory(WorldMapErrorSeverity? minSeverity = null)
    {
        if (minSeverity.HasValue)
        {
            return errorHistory.Where(e => e.Severity >= minSeverity.Value);
        }
        return errorHistory.AsReadOnly();
    }
}

public enum WorldMapErrorSeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public class WorldMapError
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public WorldMapErrorSeverity Severity { get; set; }
}
```

### 6. Async Profile Loading

**Problem:** Profile loading can block the main thread, causing frame drops.

**Solution:**
```csharp
public class AsyncProfileLoader
{
    private readonly SemaphoreSlim loadSemaphore;
    private readonly Dictionary<string, Task<WorldMapControlProfile>> loadTasks;
    
    public AsyncProfileLoader(int maxConcurrentLoads = 2)
    {
        loadSemaphore = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);
        loadTasks = new Dictionary<string, Task<WorldMapControlProfile>>();
    }
    
    public async Task<WorldMapControlProfile> LoadProfileAsync(string path, WorldConfig config)
    {
        // Check if already loading
        if (loadTasks.TryGetValue(path, out var existingTask))
        {
            return await existingTask;
        }
        
        // Start new load
        var task = LoadProfileInternalAsync(path, config);
        loadTasks[path] = task;
        
        try
        {
            return await task;
        }
        finally
        {
            loadTasks.Remove(path);
        }
    }
    
    private async Task<WorldMapControlProfile> LoadProfileInternalAsync(string path, WorldConfig config)
    {
        await loadSemaphore.WaitAsync();
        try
        {
            return await Task.Run(() => WorldMapControlProfile.LoadFromFile(path, config));
        }
        finally
        {
            loadSemaphore.Release();
        }
    }
}
```

## Implementation Priority

### Phase 1: Critical Improvements (Session 54)
1. **Profile Validation** - Add comprehensive validation with fallback
2. **Error Handling** - Add error logging and recovery
3. **Async Profile Loading** - Prevent main thread blocking

### Phase 2: Performance Improvements (Session 55)
1. **LRU Cache** - Replace FIFO with LRU eviction
2. **Priority Queue** - Prioritize chunks near player
3. **Cache Statistics** - Add monitoring and metrics

### Phase 3: Synchronization (Session 56)
1. **Profile Sync** - Implement server-client synchronization
2. **Version Negotiation** - Handle version mismatches
3. **Profile Diff** - Support incremental updates

## Configuration Recommendations

### Server Config (`config/enhanced_world_map_control_server.json`)
```json
{
  "worldMapControl": {
    "profile": {
      "version": 22,
      "autoReload": true,
      "reloadIntervalSeconds": 5,
      "validation": {
        "enabled": true,
        "strictMode": false,
        "fallbackToDefault": true
      }
    },
    "cache": {
      "maxCachedChunks": 256,
      "evictionPolicy": "LRU",
      "enableStatistics": true
    },
    "sync": {
      "enabled": true,
      "syncIntervalSeconds": 30,
      "versionNegotiation": true
    },
    "logging": {
      "level": "Info",
      "maxErrorHistory": 100,
      "logToFile": true
    }
  }
}
```

### Client Config (`config/enhanced_world_map_control_client.json`)
```json
{
  "worldMapControl": {
    "profile": {
      "autoReload": true,
      "reloadIntervalSeconds": 5,
      "syncWithServer": true,
      "validation": {
        "enabled": true,
        "fallbackToDefault": true
      }
    },
    "defaults": {
      "renderDistance": 4,
      "mapScale": 1.0,
      "showCoordinates": true,
      "showBiomeInfo": false
    },
    "performance": {
      "maxConcurrentChunkRequests": 4,
      "chunkPriorityQueue": true,
      "preloadRadius": 2
    },
    "cache": {
      "maxCachedChunks": 256,
      "evictionPolicy": "LRU",
      "enableStatistics": true
    }
  }
}
```

## Testing Recommendations

1. **Profile Validation Tests**
   - Test with valid profiles
   - Test with corrupted profiles
   - Test with hash mismatches
   - Test fallback profile generation

2. **Cache Performance Tests**
   - Test LRU vs FIFO eviction
   - Test cache hit rates
   - Test memory usage

3. **Synchronization Tests**
   - Test server-client sync
   - Test version negotiation
   - Test concurrent profile updates

4. **Error Recovery Tests**
   - Test profile load failures
   - Test network failures
   - Test hash verification failures

## Conclusion

The current world map control architecture is comprehensive but can be improved with:

1. **Better Synchronization:** Bidirectional profile sync between server and client
2. **Robust Error Handling:** Comprehensive validation with fallback mechanisms
3. **Performance Optimization:** LRU cache, priority queue, async loading
4. **Monitoring:** Cache statistics, error history, performance metrics

These improvements will enhance reliability, performance, and maintainability of the world map control system.

