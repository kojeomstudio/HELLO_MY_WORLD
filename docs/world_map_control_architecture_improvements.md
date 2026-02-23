# World Map Control Architecture Improvements

## Overview
This document describes improvements to the server and client world map control architecture for better synchronization, performance, and reliability.

## Current Architecture Analysis

### Server-side WorldMapController.cs
**Strengths:**
- Advanced queue management with adaptive pressure handling
- Load shedding and emergency braking mechanisms
- Profile reloading with hash validation
- Comprehensive generation signature computation
- Budget controls for loaded chunks and queue limits

**Areas for Improvement:**
1. Stale request mitigation could be more explicit
2. Budget harmonization with client needs alignment
3. Diagnostics for budget harmonization are limited
4. Profile synchronization could be more robust

### Client-side EnhancedWorldMapController.cs
**Strengths:**
- Map rendering with RenderTexture
- Player markers and chunk updates
- Profile synchronization with server
- Runtime configuration loading
- Budget controls for chunk updates

**Areas for Improvement:**
1. Budget calculations don't align with server
2. No explicit stale request handling
3. Limited diagnostics for performance monitoring
4. Profile hash validation could be more robust

## Improvements Implemented

### 1. Stale Request Mitigation (Server)

**Added to WorldMapController.cs:**
```csharp
// Track stale requests with timestamps
private readonly ConcurrentDictionary<Vector2Int, DateTime> requestTimestamps = new();
private readonly ConcurrentDictionary<Vector2Int, int> requestRetryCount = new();
private const int MAX_REQUEST_RETRIES = 3;
private const TimeSpan STALE_REQUEST_THRESHOLD = TimeSpan.FromSeconds(30);

// In GetChunkAsync, add stale request detection
if (requestTimestamps.TryGetValue(pos, out var requestTime))
{
    var age = DateTime.UtcNow - requestTime;
    if (age > STALE_REQUEST_THRESHOLD)
    {
        requestRetryCount.TryGetValue(pos, out var retries);
        if (retries >= MAX_REQUEST_RETRIES)
        {
            logger.LogWarning("[WorldMapController] Dropping stale request for {Pos} (age: {Age}s, retries: {Retries})", 
                pos, age.TotalSeconds, retries);
            requestTimestamps.TryRemove(pos, out _);
            requestRetryCount.TryRemove(pos, out _);
            return new ChunkData(chunkX, chunkZ);
        }
        requestRetryCount.AddOrUpdate(pos, 1, (k, v) => v + 1);
    }
}
requestTimestamps.AddOrUpdate(pos, DateTime.UtcNow, (k, v) => DateTime.UtcNow);
```

### 2. Budget Harmonization (Server-Client)

**Server-side improvements:**
```csharp
// Add budget harmonization metrics
public class BudgetHarmonizationMetrics
{
    public int ServerQueueLimit { get; set; }
    public int ServerLoadedChunkBudget { get; set; }
    public int ServerPressureFactor { get; set; }
    public int ClientMaxChunkUpdatesPerFrame { get; set; }
    public int ClientMaxQueuedChunkUpdates { get; set; }
    public double BudgetAlignmentRatio { get; set; }
    public DateTime LastSyncTime { get; set; }
}

private BudgetHarmonizationMetrics budgetMetrics;

// Add budget harmonization method
private void HarmonizeBudgets()
{
    budgetMetrics = new BudgetHarmonizationMetrics
    {
        ServerQueueLimit = queueLimit,
        ServerLoadedChunkBudget = maxLoadedChunks,
        ServerPressureFactor = queuePressureFactor,
        ClientMaxChunkUpdatesPerFrame = Math.Clamp(maxLoadedChunks / 64, 1, 512),
        ClientMaxQueuedChunkUpdates = Math.Clamp(queueLimit / 2, 64, 32768),
        BudgetAlignmentRatio = ComputeBudgetAlignmentRatio(),
        LastSyncTime = DateTime.UtcNow
    };
    
    logger.LogInformation(
        "[WorldMapController] Budget harmonized: ServerQueue={ServerQueue}, ServerBudget={ServerBudget}, " +
        "ClientUpdates={ClientUpdates}, ClientQueued={ClientQueued}, Alignment={Alignment:P2}",
        budgetMetrics.ServerQueueLimit,
        budgetMetrics.ServerLoadedChunkBudget,
        budgetMetrics.ClientMaxChunkUpdatesPerFrame,
        budgetMetrics.ClientMaxQueuedChunkUpdates,
        budgetMetrics.BudgetAlignmentRatio);
}

private double ComputeBudgetAlignmentRatio()
{
    double idealClientUpdates = maxLoadedChunks / 64.0;
    double idealClientQueued = queueLimit / 2.0;
    double actualClientUpdates = budgetMetrics?.ClientMaxChunkUpdatesPerFrame ?? idealClientUpdates;
    double actualClientQueued = budgetMetrics?.ClientMaxQueuedChunkUpdates ?? idealClientQueued;
    
    double updateAlignment = 1.0 - Math.Abs(idealClientUpdates - actualClientUpdates) / idealClientUpdates;
    double queuedAlignment = 1.0 - Math.Abs(idealClientQueued - actualClientQueued) / idealClientQueued;
    
    return (updateAlignment + queuedAlignment) / 2.0;
}
```

**Client-side improvements:**
```csharp
// Add budget harmonization with server
public class ClientBudgetHarmonization
{
    public int ServerQueueLimit { get; set; }
    public int ServerLoadedChunkBudget { get; set; }
    public int ServerPressureFactor { get; set; }
    public DateTime ServerSyncTime { get; set; }
    
    public void ApplyToClient(EnhancedWorldMapController controller)
    {
        if (ServerQueueLimit > 0)
        {
            controller._maxQueuedChunkUpdates = Math.Clamp(ServerQueueLimit / 2, 64, 32768);
        }
        
        if (ServerLoadedChunkBudget > 0)
        {
            controller._maxChunkUpdatesPerFrame = Math.Clamp(ServerLoadedChunkBudget / 64, 1, 512);
        }
    }
}

// Add method to receive server budget information
public void ApplyServerBudget(ClientBudgetHarmonization serverBudget)
{
    if (serverBudget == null) return;
    
    _maxQueuedChunkUpdates = Math.Clamp(serverBudget.ServerQueueLimit / 2, 64, 32768);
    _maxChunkUpdatesPerFrame = Math.Clamp(serverBudget.ServerLoadedChunkBudget / 64, 1, 512);
    
    Debug.Log($"[WorldMap] Applied server budget: Queued={_maxQueuedChunkUpdates}, Updates={_maxChunkUpdatesPerFrame}");
}
```

### 3. Diagnostics for Budget Harmonization

**Server-side diagnostics:**
```csharp
// Add diagnostics logging
private void LogBudgetDiagnostics()
{
    var queueState = GetAdaptiveQueueState();
    logger.LogInformation(
        "[WorldMapController] Budget Diagnostics: " +
        "QueueLimit={QueueLimit}, LoadedChunks={LoadedChunks}, InflightTasks={Inflight}, " +
        "PressureFactor={Pressure}, LoadSheddingThreshold={SheddingThreshold}, " +
        "EmergencyBrake={EmergencyBrake}, BudgetAlignment={Alignment:P2}",
        queueState.QueueLimit,
        loadedChunks.Count,
        generationTasks.Count,
        queueState.PressureFactor,
        queueState.LoadSheddingThreshold,
        queueState.EmergencyBrake,
        budgetMetrics?.BudgetAlignmentRatio ?? 0.0);
}

// Add periodic diagnostics timer
private readonly Timer diagnosticsTimer;

// In constructor:
var diagnosticsInterval = TimeSpan.FromMinutes(1);
diagnosticsTimer = new Timer(_ => LogBudgetDiagnostics(), null, diagnosticsInterval, diagnosticsInterval);
```

**Client-side diagnostics:**
```csharp
// Add diagnostics logging
private void LogBudgetDiagnostics()
{
    Debug.Log(
        $"[WorldMap] Budget Diagnostics: " +
        $"MaxUpdates={_maxChunkUpdatesPerFrame}, MaxQueued={_maxQueuedChunkUpdates}, " +
        $"LoadedChunks={_loadedChunks.Count}, QueuedUpdates={_queuedChunkUpdates.Count}, " +
        $"ChunksToUpdate={_chunksToUpdate.Count}, Budget={ComputeChunkUpdateBudget()}");
}

// Add periodic diagnostics
private float _lastDiagnosticsTime = 0f;
private const float DIAGNOSTICS_INTERVAL = 60f;

private void Update()
{
    MaybeReloadProfile();
    
    if (Time.time - _lastMapUpdate > MAP_UPDATE_INTERVAL)
    {
        UpdateMap();
        _lastMapUpdate = Time.time;
    }

    ProcessChunkUpdateQueue();
    
    if (Time.time - _lastDiagnosticsTime > DIAGNOSTICS_INTERVAL)
    {
        LogBudgetDiagnostics();
        _lastDiagnosticsTime = Time.time;
    }
}
```

### 4. Profile Synchronization Improvements

**Server-side:**
```csharp
// Add profile synchronization method
public WorldMapControlProfile GetSyncProfile()
{
    return new WorldMapControlProfile
    {
        Version = controlProfile.Version,
        ProfileHash = controlProfile.ProfileHash,
        HydrologySignature = controlProfile.HydrologySignature,
        ChunkSize = controlProfile.ChunkSize,
        RenderDistance = controlProfile.RenderDistance,
        SimulationDistance = controlProfile.SimulationDistance,
        GlobalWaterLevel = controlProfile.GlobalWaterLevel,
        EnableCaves = controlProfile.EnableCaves,
        EnableRivers = controlProfile.EnableRivers,
        EnableLakes = controlProfile.EnableLakes,
        QueueLimit = queueLimit,
        LoadedChunkBudget = maxLoadedChunks,
        PressureFactor = queuePressureFactor,
        GenerationSignature = generationSignature
    };
}
```

**Client-side:**
```csharp
// Enhanced profile application with validation
public void ApplyServerProfile(WorldMapControlProfile profile, string serverHash = "")
{
    if (profile == null)
    {
        Debug.LogWarning("[WorldMap] Received null server profile");
        return;
    }

    // Validate hydrology signature
    if (!string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
    {
        Debug.LogError($"[WorldMap] Critical: Hydrology signature mismatch! Server={profile.HydrologySignature}, Client={SharedFeatureCatalog.HydrologySignature}");
        // Don't apply profile if signatures don't match
        return;
    }

    // Validate profile hash
    if (!string.IsNullOrWhiteSpace(serverHash) &&
        !string.Equals(profile.ProfileHash, serverHash, StringComparison.OrdinalIgnoreCase))
    {
        Debug.LogWarning($"[WorldMap] Server profile hash mismatch: Server={serverHash}, Profile={profile.ProfileHash}");
    }

    // Apply profile
    _mapControlProfile = profile;
    _profileHash = profile.ProfileHash;
    _showCaves = profile.EnableCaves;
    _showRivers = profile.EnableRivers;
    _showLakes = profile.EnableLakes;
    
    // Apply server budget if available
    if (profile.QueueLimit > 0 || profile.LoadedChunkBudget > 0)
    {
        _maxQueuedChunkUpdates = Math.Clamp(profile.QueueLimit > 0 ? profile.QueueLimit / 2 : _maxQueuedChunkUpdates, 64, 32768);
        _maxChunkUpdatesPerFrame = Math.Clamp(profile.LoadedChunkBudget > 0 ? profile.LoadedChunkBudget / 64 : _maxChunkUpdatesPerFrame, 1, 512);
        Debug.Log($"[WorldMap] Applied server budget: Queued={_maxQueuedChunkUpdates}, Updates={_maxChunkUpdatesPerFrame}");
    }
    
    ApplyToggleDefaults();
    LoadClientRuntimeConfig();
    ResetMapCache();
    InitializeMapRendering();
    
    if (!string.IsNullOrWhiteSpace(_profilePath))
    {
        WorldMapControlProfile.SaveToFile(profile, _profilePath);
        if (File.Exists(_profilePath))
        {
            _profileWriteTime = File.GetLastWriteTimeUtc(_profilePath);
        }
    }
    
    UpdateMap();
    Debug.Log($"[WorldMap] Server profile applied successfully: Hash={profile.ProfileHash}, Version={profile.Version}");
}
```

## Configuration Updates

### Enhanced World Map Control Server Configuration

Update `config/enhanced_world_map_control_server.json`:

```json
{
  "budgetHarmonization": {
    "enabled": true,
    "syncIntervalSeconds": 60,
    "alignmentThreshold": 0.85,
    "autoAdjustClientBudget": true
  },
  "staleRequestMitigation": {
    "enabled": true,
    "maxRetries": 3,
    "staleThresholdSeconds": 30,
    "cleanupIntervalSeconds": 60
  },
  "diagnostics": {
    "enabled": true,
    "logIntervalSeconds": 60,
    "includeDetailedMetrics": true
  },
  "profileSynchronization": {
    "validateHydrologySignature": true,
    "validateProfileHash": true,
    "rejectMismatchedProfiles": true
  }
}
```

### Enhanced World Map Control Client Configuration

Update `Assets/StreamingAssets/enhanced_world_map_control_client.json`:

```json
{
  "worldMapControl": {
    "defaults": {
      "maxChunkUpdatesPerFrame": 12,
      "maxQueuedChunkUpdates": 4096
    },
    "performance": {
      "chunkUpdateThrottleMs": 16
    },
    "budgetHarmonization": {
      "enabled": true,
      "acceptServerBudget": true,
      "minClientUpdates": 1,
      "maxClientUpdates": 512,
      "minClientQueued": 64,
      "maxClientQueued": 32768
    },
    "diagnostics": {
      "enabled": true,
      "logIntervalSeconds": 60
    }
  }
}
```

## Testing Recommendations

1. **Budget Harmonization Testing:**
   - Verify server and client budgets are aligned
   - Test with different load scenarios
   - Monitor budget alignment ratio

2. **Stale Request Testing:**
   - Simulate long-running requests
   - Verify stale requests are dropped
   - Test retry behavior

3. **Profile Synchronization Testing:**
   - Test with matching hydrology signatures
   - Test with mismatched signatures (should reject)
   - Verify profile hash validation

4. **Diagnostics Testing:**
   - Verify diagnostic logs are generated
   - Check metrics accuracy
   - Monitor performance impact

## Benefits

1. **Improved Reliability:** Stale request mitigation prevents hung requests from affecting performance
2. **Better Performance:** Budget harmonization ensures server and client are optimized together
3. **Enhanced Monitoring:** Diagnostics provide visibility into system behavior
4. **Robust Synchronization:** Profile validation prevents configuration mismatches

## Future Improvements

1. Add real-time budget synchronization via network messages
2. Implement predictive budget adjustment based on load patterns
3. Add machine learning for optimal budget allocation
4. Create dashboard for monitoring budget harmonization metrics
