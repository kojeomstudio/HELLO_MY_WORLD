# World Map Control Architecture Analysis
**Date:** 2026-02-22  
**Session:** 110  
**Status:** Analysis Complete

## Executive Summary

This document analyzes the current world map control architecture for both server and client, identifying issues and proposing improvements for better synchronization, backpressure management, and request handling.

## Current Architecture Overview

### Server-Side: WorldMapController.cs

**Location:** `GameServer/World/WorldMapController.cs` (722 lines)

**Key Components:**
- Centralized chunk generation and caching
- Adaptive queue policy with pressure factors
- Inflight task tracking
- Profile-based configuration
- Automatic profile reload detection

**Strengths:**
- Comprehensive adaptive queue management
- Profile hash validation
- Hydrology-aware generation signature
- Automatic cleanup of old chunks
- Load shedding mechanisms

**Weaknesses:**
- No TTL (Time To Live) for inflight chunk requests
- No explicit request cancellation support
- No chunk request deduplication
- No request prioritization
- Limited backpressure signaling to clients
- No request timeout handling
- No metrics for request latency

### Client-Side: EnhancedWorldMapController.cs

**Location:** `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs` (805 lines)

**Key Components:**
- Map rendering and visualization
- Player marker management
- Chunk update queue with throttling
- Profile reload detection
- Runtime configuration loading

**Strengths:**
- Efficient chunk update queue processing
- Profile hash validation
- Runtime configuration support
- Event-driven architecture
- Proper resource cleanup

**Weaknesses:**
- No TTL for chunk requests
- No request cancellation support
- No request deduplication
- No prioritization of chunk updates
- Limited backpressure handling
- No retry mechanism for failed requests
- No request timeout handling

### Client-Side: WorldMapControlSystem.cs

**Location:** `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs` (1812 lines)

**Key Components:**
- Singleton pattern for configuration management
- Profile loading and saving
- Client and server config extraction
- Terrain parameter application

**Strengths:**
- Comprehensive configuration management
- Client/server config separation
- Profile hash validation
- Auto-save functionality

**Weaknesses:**
- No network request management
- No request tracking
- No backpressure coordination
- Limited runtime configuration updates

## Identified Issues

### 1. Missing TTL for Inflight Requests

**Server:**
- Inflight chunk generation tasks have no timeout
- Stuck requests can consume resources indefinitely
- No automatic cleanup of stale requests

**Client:**
- Chunk requests have no timeout
- Network issues can cause indefinite waiting
- No retry mechanism for failed requests

### 2. No Request Cancellation

**Server:**
- Cannot cancel inflight generation tasks
- Resources wasted on cancelled requests
- No way to signal client disconnection

**Client:**
- Cannot cancel pending chunk requests
- Resources wasted on cancelled requests
- No cleanup on scene changes

### 3. No Request Deduplication

**Server:**
- Multiple requests for same chunk can trigger multiple generations
- Wastes CPU and memory resources
- Can cause inconsistent chunk data

**Client:**
- Multiple requests for same chunk can be sent to server
- Wastes network bandwidth
- Can cause duplicate processing

### 4. No Request Prioritization

**Server:**
- All requests treated equally
- Critical chunks (near player) not prioritized
- No LOD-based prioritization

**Client:**
- All chunk updates processed in FIFO order
- Critical chunks not prioritized
- No distance-based prioritization

### 5. Limited Backpressure Signaling

**Server:**
- No explicit backpressure signals to clients
- Clients don't know server load
- Can overwhelm server with requests

**Client:**
- No throttling based on server load
- Can send too many requests
- No adaptive request rate

### 6. No Request Timeout Handling

**Server:**
- No timeout for chunk generation
- No timeout for client responses
- No cleanup of stale connections

**Client:**
- No timeout for chunk requests
- No timeout for server responses
- No cleanup of stale connections

### 7. Limited Metrics and Monitoring

**Server:**
- No request latency metrics
- No queue depth monitoring
- No generation time tracking

**Client:**
- No request latency metrics
- No network error tracking
- No chunk load time monitoring

## Proposed Improvements

### 1. Add TTL for Inflight Requests

**Server:**
```csharp
public class InflightChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public Task<ChunkData> GenerationTask { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string RequestId { get; set; }
    public CancellationTokenSource CancellationToken { get; set; }
}

// Default TTL: 30 seconds
private const int DefaultRequestTtlSeconds = 30;
```

**Client:**
```csharp
public class PendingChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string RequestId { get; set; }
    public int RetryCount { get; set; }
}

// Default TTL: 10 seconds
private const int DefaultRequestTtlSeconds = 10;
```

### 2. Add Request Cancellation

**Server:**
```csharp
public void CancelChunkRequest(string requestId)
{
    if (generationTasks.TryGetValue(chunkPos, out var task))
    {
        task.CancellationToken?.Cancel();
        generationTasks.TryRemove(chunkPos, out _);
    }
}

public void CancelAllRequestsForPlayer(string playerId)
{
    // Cancel all requests from a specific player
}
```

**Client:**
```csharp
public void CancelChunkRequest(Vector2Int chunkPos)
{
    if (pendingRequests.TryGetValue(chunkPos, out var request))
    {
        pendingRequests.Remove(chunkPos);
        // Notify server if request was sent
    }
}

public void CancelAllRequests()
{
    pendingRequests.Clear();
}
```

### 3. Add Request Deduplication

**Server:**
```csharp
private readonly ConcurrentDictionary<Vector2Int, InflightChunkRequest> inflightRequests;

public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ)
{
    var pos = new Vector2Int(chunkX, chunkZ);
    
    // Check if request is already inflight
    if (inflightRequests.TryGetValue(pos, out var existingRequest))
    {
        // Check if request is still valid
        if (DateTime.UtcNow < existingRequest.ExpiryTime)
        {
            return await existingRequest.GenerationTask;
        }
        else
        {
            // Remove expired request
            inflightRequests.TryRemove(pos, out _);
        }
    }
    
    // Create new request
    var request = new InflightChunkRequest
    {
        ChunkPosition = pos,
        RequestTime = DateTime.UtcNow,
        ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds),
        RequestId = Guid.NewGuid().ToString()
    };
    
    inflightRequests[pos] = request;
    // ... rest of generation logic
}
```

**Client:**
```csharp
private readonly Dictionary<Vector2Int, PendingChunkRequest> pendingRequests;

public void RequestChunk(int chunkX, int chunkZ)
{
    var pos = new Vector2Int(chunkX, chunkZ);
    
    // Check if request is already pending
    if (pendingRequests.TryGetValue(pos, out var existingRequest))
    {
        if (DateTime.UtcNow < existingRequest.ExpiryTime)
        {
            // Request already pending, don't send duplicate
            return;
        }
        else
        {
            // Remove expired request
            pendingRequests.Remove(pos);
        }
    }
    
    // Create new request
    var request = new PendingChunkRequest
    {
        ChunkPosition = pos,
        RequestTime = DateTime.UtcNow,
        ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds),
        RequestId = Guid.NewGuid().ToString(),
        RetryCount = 0
    };
    
    pendingRequests[pos] = request;
    // Send request to server
}
```

### 4. Add Request Prioritization

**Server:**
```csharp
public enum ChunkPriority
{
    Critical = 0,    // Chunk containing player
    High = 1,        // Chunks within render distance
    Medium = 2,      // Chunks within simulation distance
    Low = 3,         // All other chunks
    Background = 4    // Preload requests
}

public class PrioritizedChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public ChunkPriority Priority { get; set; }
    public float DistanceToPlayer { get; set; }
    public DateTime RequestTime { get; set; }
}

private readonly PriorityQueue<PrioritizedChunkRequest, (int, float)> requestQueue;

public void EnqueueChunkRequest(Vector2Int chunkPos, Vector3 playerPos, ChunkPriority priority)
{
    var distance = Vector3.Distance(
        new Vector3(chunkPos.X * 16, 0, chunkPos.Y * 16),
        playerPos
    );
    
    var request = new PrioritizedChunkRequest
    {
        ChunkPosition = chunkPos,
        Priority = priority,
        DistanceToPlayer = distance,
        RequestTime = DateTime.UtcNow
    };
    
    // Priority: (priority level, distance) - lower is better
    requestQueue.Enqueue(request, ((int)priority, distance));
}
```

**Client:**
```csharp
private readonly PriorityQueue<ChunkUpdateTask, (int, float)> updateQueue;

public void EnqueueChunkUpdate(Vector2Int chunkPos, ChunkData chunkData, Vector3 playerPos)
{
    var distance = Vector3.Distance(
        new Vector3(chunkPos.x * 16, 0, chunkPos.z * 16),
        playerPos
    );
    
    var priority = distance < 32 ? 0 : distance < 64 ? 1 : distance < 96 ? 2 : 3;
    
    var task = new ChunkUpdateTask
    {
        ChunkPosition = chunkPos,
        ChunkData = chunkData,
        DistanceToPlayer = distance
    };
    
    updateQueue.Enqueue(task, (priority, distance));
}
```

### 5. Add Backpressure Signaling

**Server:**
```csharp
public class BackpressureSignal
{
    public bool IsUnderPressure { get; set; }
    public double QueueLoadRatio { get; set; }
    public int QueueDepth { get; set; }
    public int QueueLimit { get; set; }
    public int RecommendedBackoffMs { get; set; }
    public DateTime SignalTime { get; set; }
}

public BackpressureSignal GetCurrentBackpressure()
{
    var queueState = GetAdaptiveQueueState();
    
    return new BackpressureSignal
    {
        IsUnderPressure = queueState.EmergencyBrake || queueState.PressureFactor > 2,
        QueueLoadRatio = generationTasks.Count / Math.Max(1.0, maxLoadedChunks),
        QueueDepth = generationTasks.Count,
        QueueLimit = queueState.QueueLimit,
        RecommendedBackoffMs = queueState.PressureFactor * queueBackoffDelayMs,
        SignalTime = DateTime.UtcNow
    };
}

// Send backpressure to clients via protocol
public void SendBackpressureToClients()
{
    var signal = GetCurrentBackpressure();
    // Send to all connected clients via network
}
```

**Client:**
```csharp
public class BackpressureHandler
{
    private BackpressureSignal? lastServerSignal;
    private DateTime lastSignalTime;
    
    public void HandleBackpressureSignal(BackpressureSignal signal)
    {
        lastServerSignal = signal;
        lastSignalTime = DateTime.UtcNow;
        
        if (signal.IsUnderPressure)
        {
            // Reduce request rate
            var backoffMs = signal.RecommendedBackoffMs;
            // Adjust request interval based on backoff
        }
    }
    
    public int GetRecommendedRequestInterval()
    {
        if (lastServerSignal == null || 
            DateTime.UtcNow - lastSignalTime > TimeSpan.FromSeconds(30))
        {
            return 100; // Default 100ms
        }
        
        return lastServerSignal.Value.RecommendedBackoffMs;
    }
}
```

### 6. Add Request Timeout Handling

**Server:**
```csharp
private readonly Timer timeoutCheckTimer;

public WorldMapController(...)
{
    // ... existing initialization
    
    // Check for expired requests every 5 seconds
    timeoutCheckTimer = new Timer(CheckExpiredRequests, null, 
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
}

private void CheckExpiredRequests(object? state)
{
    var now = DateTime.UtcNow;
    var expiredRequests = new List<Vector2Int>();
    
    foreach (var kvp in inflightRequests)
    {
        if (now > kvp.Value.ExpiryTime)
        {
            expiredRequests.Add(kvp.Key);
        }
    }
    
    foreach (var chunkPos in expiredRequests)
    {
        if (inflightRequests.TryRemove(chunkPos, out var request))
        {
            request.CancellationToken?.Cancel();
            logger.LogWarning(
                "[WorldMapController] Request expired for chunk {Pos} (RequestId: {RequestId})",
                chunkPos,
                request.RequestId);
        }
    }
}
```

**Client:**
```csharp
private readonly Timer timeoutCheckTimer;

public void Start()
{
    // ... existing initialization
    
    // Check for expired requests every 2 seconds
    timeoutCheckTimer = new Timer(CheckExpiredRequests, null, 
        TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
}

private void CheckExpiredRequests(object? state)
{
    var now = DateTime.UtcNow;
    var expiredRequests = new List<Vector2Int>();
    
    foreach (var kvp in pendingRequests)
    {
        if (now > kvp.Value.ExpiryTime)
        {
            expiredRequests.Add(kvp.Key);
        }
    }
    
    foreach (var chunkPos in expiredRequests)
    {
        if (pendingRequests.TryGetValue(chunkPos, out var request))
        {
            // Retry logic
            if (request.RetryCount < MaxRetryCount)
            {
                request.RetryCount++;
                request.ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds);
                // Resend request to server
            }
            else
            {
                pendingRequests.Remove(chunkPos);
                Debug.LogWarning($"[WorldMap] Request expired for chunk {chunkPos} after {request.RetryCount} retries");
            }
        }
    }
}
```

### 7. Add Metrics and Monitoring

**Server:**
```csharp
public class ChunkRequestMetrics
{
    public long TotalRequests { get; set; }
    public long CompletedRequests { get; set; }
    public long ExpiredRequests { get; set; }
    public long CancelledRequests { get; set; }
    public double AverageGenerationTimeMs { get; set; }
    public double AverageQueueWaitTimeMs { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    
    public double CacheHitRatio => 
        TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0.0;
}

public ChunkRequestMetrics GetMetrics()
{
    return new ChunkRequestMetrics
    {
        TotalRequests = metrics.TotalRequests,
        CompletedRequests = metrics.CompletedRequests,
        ExpiredRequests = metrics.ExpiredRequests,
        CancelledRequests = metrics.CancelledRequests,
        AverageGenerationTimeMs = metrics.AverageGenerationTimeMs,
        AverageQueueWaitTimeMs = metrics.AverageQueueWaitTimeMs,
        CacheHits = metrics.CacheHits,
        CacheMisses = metrics.CacheMisses
    };
}
```

**Client:**
```csharp
public class ChunkLoadMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulLoads { get; set; }
    public long FailedLoads { get; set; }
    public long TimeoutLoads { get; set; }
    public double AverageLoadTimeMs { get; set; }
    public double AverageNetworkLatencyMs { get; set; }
    public long DuplicateRequests { get; set; }
    public long CancelledRequests { get; set; }
    
    public double SuccessRatio => 
        TotalRequests > 0 ? (double)SuccessfulLoads / TotalRequests : 0.0;
}

public ChunkLoadMetrics GetMetrics()
{
    return new ChunkLoadMetrics
    {
        TotalRequests = metrics.TotalRequests,
        SuccessfulLoads = metrics.SuccessfulLoads,
        FailedLoads = metrics.FailedLoads,
        TimeoutLoads = metrics.TimeoutLoads,
        AverageLoadTimeMs = metrics.AverageLoadTimeMs,
        AverageNetworkLatencyMs = metrics.AverageNetworkLatencyMs,
        DuplicateRequests = metrics.DuplicateRequests,
        CancelledRequests = metrics.CancelledRequests
    };
}
```

## Implementation Plan

### Phase 1: Server Improvements
1. Add TTL for inflight chunk requests
2. Add request cancellation support
3. Add request deduplication
4. Add request prioritization
5. Add backpressure signaling
6. Add request timeout handling
7. Add metrics and monitoring

### Phase 2: Client Improvements
1. Add TTL for chunk requests
2. Add request cancellation support
3. Add request deduplication
4. Add request prioritization
5. Add backpressure handling
6. Add request timeout handling
7. Add metrics and monitoring

### Phase 3: Protocol Extensions
1. Add backpressure message to protocol
2. Add request cancellation message to protocol
3. Add metrics reporting message to protocol
4. Update protobuf definitions

### Phase 4: Testing and Validation
1. Unit tests for TTL handling
2. Unit tests for request cancellation
3. Unit tests for request deduplication
4. Integration tests for backpressure
5. Performance tests for prioritization
6. Load tests for queue management

## Success Criteria

1. **TTL Handling**: All requests expire within configured TTL
2. **Cancellation**: All cancelled requests are properly cleaned up
3. **Deduplication**: No duplicate requests are processed
4. **Prioritization**: Critical chunks are processed first
5. **Backpressure**: Server load is properly signaled to clients
6. **Timeout Handling**: All timeouts are properly handled
7. **Metrics**: All metrics are accurately collected and reported

## Risks and Mitigations

### Risk 1: Increased Memory Usage
**Mitigation**: Use efficient data structures and limit queue sizes

### Risk 2: Increased Complexity
**Mitigation**: Keep implementation simple and well-documented

### Risk 3: Performance Impact
**Mitigation**: Profile and optimize critical paths

### Risk 4: Compatibility Issues
**Mitigation**: Maintain backward compatibility with existing protocol

## Conclusion

The current world map control architecture is functional but lacks critical features for production use. The proposed improvements will significantly enhance reliability, performance, and scalability of the chunk management system.

**Next Steps:**
1. Implement server-side improvements
2. Implement client-side improvements
3. Extend protocol with new messages
4. Test and validate all improvements
5. Update documentation

---

**Document Version:** 1.0  
**Last Updated:** 2026-02-22  
**Author:** Session 110 Implementation Team
**Date:** 2026-02-22  
**Session:** 110  
**Status:** Analysis Complete

## Executive Summary

This document analyzes the current world map control architecture for both server and client, identifying issues and proposing improvements for better synchronization, backpressure management, and request handling.

## Current Architecture Overview

### Server-Side: WorldMapController.cs

**Location:** `GameServer/World/WorldMapController.cs` (722 lines)

**Key Components:**
- Centralized chunk generation and caching
- Adaptive queue policy with pressure factors
- Inflight task tracking
- Profile-based configuration
- Automatic profile reload detection

**Strengths:**
- Comprehensive adaptive queue management
- Profile hash validation
- Hydrology-aware generation signature
- Automatic cleanup of old chunks
- Load shedding mechanisms

**Weaknesses:**
- No TTL (Time To Live) for inflight chunk requests
- No explicit request cancellation support
- No chunk request deduplication
- No request prioritization
- Limited backpressure signaling to clients
- No request timeout handling
- No metrics for request latency

### Client-Side: EnhancedWorldMapController.cs

**Location:** `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs` (805 lines)

**Key Components:**
- Map rendering and visualization
- Player marker management
- Chunk update queue with throttling
- Profile reload detection
- Runtime configuration loading

**Strengths:**
- Efficient chunk update queue processing
- Profile hash validation
- Runtime configuration support
- Event-driven architecture
- Proper resource cleanup

**Weaknesses:**
- No TTL for chunk requests
- No request cancellation support
- No request deduplication
- No prioritization of chunk updates
- Limited backpressure handling
- No retry mechanism for failed requests
- No request timeout handling

### Client-Side: WorldMapControlSystem.cs

**Location:** `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs` (1812 lines)

**Key Components:**
- Singleton pattern for configuration management
- Profile loading and saving
- Client and server config extraction
- Terrain parameter application

**Strengths:**
- Comprehensive configuration management
- Client/server config separation
- Profile hash validation
- Auto-save functionality

**Weaknesses:**
- No network request management
- No request tracking
- No backpressure coordination
- Limited runtime configuration updates

## Identified Issues

### 1. Missing TTL for Inflight Requests

**Server:**
- Inflight chunk generation tasks have no timeout
- Stuck requests can consume resources indefinitely
- No automatic cleanup of stale requests

**Client:**
- Chunk requests have no timeout
- Network issues can cause indefinite waiting
- No retry mechanism for failed requests

### 2. No Request Cancellation

**Server:**
- Cannot cancel inflight generation tasks
- Resources wasted on cancelled requests
- No way to signal client disconnection

**Client:**
- Cannot cancel pending chunk requests
- Resources wasted on cancelled requests
- No cleanup on scene changes

### 3. No Request Deduplication

**Server:**
- Multiple requests for same chunk can trigger multiple generations
- Wastes CPU and memory resources
- Can cause inconsistent chunk data

**Client:**
- Multiple requests for same chunk can be sent to server
- Wastes network bandwidth
- Can cause duplicate processing

### 4. No Request Prioritization

**Server:**
- All requests treated equally
- Critical chunks (near player) not prioritized
- No LOD-based prioritization

**Client:**
- All chunk updates processed in FIFO order
- Critical chunks not prioritized
- No distance-based prioritization

### 5. Limited Backpressure Signaling

**Server:**
- No explicit backpressure signals to clients
- Clients don't know server load
- Can overwhelm server with requests

**Client:**
- No throttling based on server load
- Can send too many requests
- No adaptive request rate

### 6. No Request Timeout Handling

**Server:**
- No timeout for chunk generation
- No timeout for client responses
- No cleanup of stale connections

**Client:**
- No timeout for chunk requests
- No timeout for server responses
- No cleanup of stale connections

### 7. Limited Metrics and Monitoring

**Server:**
- No request latency metrics
- No queue depth monitoring
- No generation time tracking

**Client:**
- No request latency metrics
- No network error tracking
- No chunk load time monitoring

## Proposed Improvements

### 1. Add TTL for Inflight Requests

**Server:**
```csharp
public class InflightChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public Task<ChunkData> GenerationTask { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string RequestId { get; set; }
    public CancellationTokenSource CancellationToken { get; set; }
}

// Default TTL: 30 seconds
private const int DefaultRequestTtlSeconds = 30;
```

**Client:**
```csharp
public class PendingChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string RequestId { get; set; }
    public int RetryCount { get; set; }
}

// Default TTL: 10 seconds
private const int DefaultRequestTtlSeconds = 10;
```

### 2. Add Request Cancellation

**Server:**
```csharp
public void CancelChunkRequest(string requestId)
{
    if (generationTasks.TryGetValue(chunkPos, out var task))
    {
        task.CancellationToken?.Cancel();
        generationTasks.TryRemove(chunkPos, out _);
    }
}

public void CancelAllRequestsForPlayer(string playerId)
{
    // Cancel all requests from a specific player
}
```

**Client:**
```csharp
public void CancelChunkRequest(Vector2Int chunkPos)
{
    if (pendingRequests.TryGetValue(chunkPos, out var request))
    {
        pendingRequests.Remove(chunkPos);
        // Notify server if request was sent
    }
}

public void CancelAllRequests()
{
    pendingRequests.Clear();
}
```

### 3. Add Request Deduplication

**Server:**
```csharp
private readonly ConcurrentDictionary<Vector2Int, InflightChunkRequest> inflightRequests;

public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ)
{
    var pos = new Vector2Int(chunkX, chunkZ);
    
    // Check if request is already inflight
    if (inflightRequests.TryGetValue(pos, out var existingRequest))
    {
        // Check if request is still valid
        if (DateTime.UtcNow < existingRequest.ExpiryTime)
        {
            return await existingRequest.GenerationTask;
        }
        else
        {
            // Remove expired request
            inflightRequests.TryRemove(pos, out _);
        }
    }
    
    // Create new request
    var request = new InflightChunkRequest
    {
        ChunkPosition = pos,
        RequestTime = DateTime.UtcNow,
        ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds),
        RequestId = Guid.NewGuid().ToString()
    };
    
    inflightRequests[pos] = request;
    // ... rest of generation logic
}
```

**Client:**
```csharp
private readonly Dictionary<Vector2Int, PendingChunkRequest> pendingRequests;

public void RequestChunk(int chunkX, int chunkZ)
{
    var pos = new Vector2Int(chunkX, chunkZ);
    
    // Check if request is already pending
    if (pendingRequests.TryGetValue(pos, out var existingRequest))
    {
        if (DateTime.UtcNow < existingRequest.ExpiryTime)
        {
            // Request already pending, don't send duplicate
            return;
        }
        else
        {
            // Remove expired request
            pendingRequests.Remove(pos);
        }
    }
    
    // Create new request
    var request = new PendingChunkRequest
    {
        ChunkPosition = pos,
        RequestTime = DateTime.UtcNow,
        ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds),
        RequestId = Guid.NewGuid().ToString(),
        RetryCount = 0
    };
    
    pendingRequests[pos] = request;
    // Send request to server
}
```

### 4. Add Request Prioritization

**Server:**
```csharp
public enum ChunkPriority
{
    Critical = 0,    // Chunk containing player
    High = 1,        // Chunks within render distance
    Medium = 2,      // Chunks within simulation distance
    Low = 3,         // All other chunks
    Background = 4    // Preload requests
}

public class PrioritizedChunkRequest
{
    public Vector2Int ChunkPosition { get; set; }
    public ChunkPriority Priority { get; set; }
    public float DistanceToPlayer { get; set; }
    public DateTime RequestTime { get; set; }
}

private readonly PriorityQueue<PrioritizedChunkRequest, (int, float)> requestQueue;

public void EnqueueChunkRequest(Vector2Int chunkPos, Vector3 playerPos, ChunkPriority priority)
{
    var distance = Vector3.Distance(
        new Vector3(chunkPos.X * 16, 0, chunkPos.Y * 16),
        playerPos
    );
    
    var request = new PrioritizedChunkRequest
    {
        ChunkPosition = chunkPos,
        Priority = priority,
        DistanceToPlayer = distance,
        RequestTime = DateTime.UtcNow
    };
    
    // Priority: (priority level, distance) - lower is better
    requestQueue.Enqueue(request, ((int)priority, distance));
}
```

**Client:**
```csharp
private readonly PriorityQueue<ChunkUpdateTask, (int, float)> updateQueue;

public void EnqueueChunkUpdate(Vector2Int chunkPos, ChunkData chunkData, Vector3 playerPos)
{
    var distance = Vector3.Distance(
        new Vector3(chunkPos.x * 16, 0, chunkPos.z * 16),
        playerPos
    );
    
    var priority = distance < 32 ? 0 : distance < 64 ? 1 : distance < 96 ? 2 : 3;
    
    var task = new ChunkUpdateTask
    {
        ChunkPosition = chunkPos,
        ChunkData = chunkData,
        DistanceToPlayer = distance
    };
    
    updateQueue.Enqueue(task, (priority, distance));
}
```

### 5. Add Backpressure Signaling

**Server:**
```csharp
public class BackpressureSignal
{
    public bool IsUnderPressure { get; set; }
    public double QueueLoadRatio { get; set; }
    public int QueueDepth { get; set; }
    public int QueueLimit { get; set; }
    public int RecommendedBackoffMs { get; set; }
    public DateTime SignalTime { get; set; }
}

public BackpressureSignal GetCurrentBackpressure()
{
    var queueState = GetAdaptiveQueueState();
    
    return new BackpressureSignal
    {
        IsUnderPressure = queueState.EmergencyBrake || queueState.PressureFactor > 2,
        QueueLoadRatio = generationTasks.Count / Math.Max(1.0, maxLoadedChunks),
        QueueDepth = generationTasks.Count,
        QueueLimit = queueState.QueueLimit,
        RecommendedBackoffMs = queueState.PressureFactor * queueBackoffDelayMs,
        SignalTime = DateTime.UtcNow
    };
}

// Send backpressure to clients via protocol
public void SendBackpressureToClients()
{
    var signal = GetCurrentBackpressure();
    // Send to all connected clients via network
}
```

**Client:**
```csharp
public class BackpressureHandler
{
    private BackpressureSignal? lastServerSignal;
    private DateTime lastSignalTime;
    
    public void HandleBackpressureSignal(BackpressureSignal signal)
    {
        lastServerSignal = signal;
        lastSignalTime = DateTime.UtcNow;
        
        if (signal.IsUnderPressure)
        {
            // Reduce request rate
            var backoffMs = signal.RecommendedBackoffMs;
            // Adjust request interval based on backoff
        }
    }
    
    public int GetRecommendedRequestInterval()
    {
        if (lastServerSignal == null || 
            DateTime.UtcNow - lastSignalTime > TimeSpan.FromSeconds(30))
        {
            return 100; // Default 100ms
        }
        
        return lastServerSignal.Value.RecommendedBackoffMs;
    }
}
```

### 6. Add Request Timeout Handling

**Server:**
```csharp
private readonly Timer timeoutCheckTimer;

public WorldMapController(...)
{
    // ... existing initialization
    
    // Check for expired requests every 5 seconds
    timeoutCheckTimer = new Timer(CheckExpiredRequests, null, 
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
}

private void CheckExpiredRequests(object? state)
{
    var now = DateTime.UtcNow;
    var expiredRequests = new List<Vector2Int>();
    
    foreach (var kvp in inflightRequests)
    {
        if (now > kvp.Value.ExpiryTime)
        {
            expiredRequests.Add(kvp.Key);
        }
    }
    
    foreach (var chunkPos in expiredRequests)
    {
        if (inflightRequests.TryRemove(chunkPos, out var request))
        {
            request.CancellationToken?.Cancel();
            logger.LogWarning(
                "[WorldMapController] Request expired for chunk {Pos} (RequestId: {RequestId})",
                chunkPos,
                request.RequestId);
        }
    }
}
```

**Client:**
```csharp
private readonly Timer timeoutCheckTimer;

public void Start()
{
    // ... existing initialization
    
    // Check for expired requests every 2 seconds
    timeoutCheckTimer = new Timer(CheckExpiredRequests, null, 
        TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
}

private void CheckExpiredRequests(object? state)
{
    var now = DateTime.UtcNow;
    var expiredRequests = new List<Vector2Int>();
    
    foreach (var kvp in pendingRequests)
    {
        if (now > kvp.Value.ExpiryTime)
        {
            expiredRequests.Add(kvp.Key);
        }
    }
    
    foreach (var chunkPos in expiredRequests)
    {
        if (pendingRequests.TryGetValue(chunkPos, out var request))
        {
            // Retry logic
            if (request.RetryCount < MaxRetryCount)
            {
                request.RetryCount++;
                request.ExpiryTime = DateTime.UtcNow.AddSeconds(DefaultRequestTtlSeconds);
                // Resend request to server
            }
            else
            {
                pendingRequests.Remove(chunkPos);
                Debug.LogWarning($"[WorldMap] Request expired for chunk {chunkPos} after {request.RetryCount} retries");
            }
        }
    }
}
```

### 7. Add Metrics and Monitoring

**Server:**
```csharp
public class ChunkRequestMetrics
{
    public long TotalRequests { get; set; }
    public long CompletedRequests { get; set; }
    public long ExpiredRequests { get; set; }
    public long CancelledRequests { get; set; }
    public double AverageGenerationTimeMs { get; set; }
    public double AverageQueueWaitTimeMs { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    
    public double CacheHitRatio => 
        TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0.0;
}

public ChunkRequestMetrics GetMetrics()
{
    return new ChunkRequestMetrics
    {
        TotalRequests = metrics.TotalRequests,
        CompletedRequests = metrics.CompletedRequests,
        ExpiredRequests = metrics.ExpiredRequests,
        CancelledRequests = metrics.CancelledRequests,
        AverageGenerationTimeMs = metrics.AverageGenerationTimeMs,
        AverageQueueWaitTimeMs = metrics.AverageQueueWaitTimeMs,
        CacheHits = metrics.CacheHits,
        CacheMisses = metrics.CacheMisses
    };
}
```

**Client:**
```csharp
public class ChunkLoadMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulLoads { get; set; }
    public long FailedLoads { get; set; }
    public long TimeoutLoads { get; set; }
    public double AverageLoadTimeMs { get; set; }
    public double AverageNetworkLatencyMs { get; set; }
    public long DuplicateRequests { get; set; }
    public long CancelledRequests { get; set; }
    
    public double SuccessRatio => 
        TotalRequests > 0 ? (double)SuccessfulLoads / TotalRequests : 0.0;
}

public ChunkLoadMetrics GetMetrics()
{
    return new ChunkLoadMetrics
    {
        TotalRequests = metrics.TotalRequests,
        SuccessfulLoads = metrics.SuccessfulLoads,
        FailedLoads = metrics.FailedLoads,
        TimeoutLoads = metrics.TimeoutLoads,
        AverageLoadTimeMs = metrics.AverageLoadTimeMs,
        AverageNetworkLatencyMs = metrics.AverageNetworkLatencyMs,
        DuplicateRequests = metrics.DuplicateRequests,
        CancelledRequests = metrics.CancelledRequests
    };
}
```

## Implementation Plan

### Phase 1: Server Improvements
1. Add TTL for inflight chunk requests
2. Add request cancellation support
3. Add request deduplication
4. Add request prioritization
5. Add backpressure signaling
6. Add request timeout handling
7. Add metrics and monitoring

### Phase 2: Client Improvements
1. Add TTL for chunk requests
2. Add request cancellation support
3. Add request deduplication
4. Add request prioritization
5. Add backpressure handling
6. Add request timeout handling
7. Add metrics and monitoring

### Phase 3: Protocol Extensions
1. Add backpressure message to protocol
2. Add request cancellation message to protocol
3. Add metrics reporting message to protocol
4. Update protobuf definitions

### Phase 4: Testing and Validation
1. Unit tests for TTL handling
2. Unit tests for request cancellation
3. Unit tests for request deduplication
4. Integration tests for backpressure
5. Performance tests for prioritization
6. Load tests for queue management

## Success Criteria

1. **TTL Handling**: All requests expire within configured TTL
2. **Cancellation**: All cancelled requests are properly cleaned up
3. **Deduplication**: No duplicate requests are processed
4. **Prioritization**: Critical chunks are processed first
5. **Backpressure**: Server load is properly signaled to clients
6. **Timeout Handling**: All timeouts are properly handled
7. **Metrics**: All metrics are accurately collected and reported

## Risks and Mitigations

### Risk 1: Increased Memory Usage
**Mitigation**: Use efficient data structures and limit queue sizes

### Risk 2: Increased Complexity
**Mitigation**: Keep implementation simple and well-documented

### Risk 3: Performance Impact
**Mitigation**: Profile and optimize critical paths

### Risk 4: Compatibility Issues
**Mitigation**: Maintain backward compatibility with existing protocol

## Conclusion

The current world map control architecture is functional but lacks critical features for production use. The proposed improvements will significantly enhance reliability, performance, and scalability of the chunk management system.

**Next Steps:**
1. Implement server-side improvements
2. Implement client-side improvements
3. Extend protocol with new messages
4. Test and validate all improvements
5. Update documentation

---

**Document Version:** 1.0  
**Last Updated:** 2026-02-22  
**Author:** Session 110 Implementation Team

