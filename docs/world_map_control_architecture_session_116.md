# World Map Control Architecture - Session 116

## Overview

This document describes the world map control architecture improvements made during Session 116, focusing on centralized chunk generation, adaptive queue management, and profile-based configuration.

## Architecture Components

### 1. WorldMapController

**File**: [`GameServer/World/WorldMapController.cs`](../GameServer/World/WorldMapController.cs)

**Purpose**: Centralized world map controller responsible for generating and caching chunks, persisting the map-control profile, and coordinating hydrology-aware generation.

**Key Features**:

- **Chunk Generation and Caching**: Manages chunk generation tasks and caches generated chunks
- **Profile Management**: Loads and persists world map control profiles
- **Adaptive Queue Management**: Implements sophisticated queue pressure management
- **Pipeline Coordination**: Orchestrates the enhanced terrain generation pipeline

**Core Methods**:

```csharp
public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
public async Task PreloadAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
public void Dispose()
```

### 2. WorldMapControlManager

**File**: [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

**Purpose**: Lightweight world map control service that reuses the enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

**Key Features**:

- **Per-Player Profiles**: Manages individual player map preferences
- **Chunk Preview Generation**: Generates preview chunks for map display
- **Queue Policy Management**: Implements advanced queue policies
- **Request Handling**: Handles various world map request types

**Core Methods**:

```csharp
public Task<WorldMapResponse> HandleAsync(WorldMapRequest request)
private async Task<WorldMapResponse> HandleInitialMapAsync(WorldMapRequest request)
private async Task<WorldMapResponse> HandleChunkUpdateAsync(WorldMapRequest request)
private Task<WorldMapResponse> HandleProfileAsync(WorldMapRequest request, bool updateProfile)
```

## Adaptive Queue Management

### Queue Pressure Bands

The system classifies queue pressure into four bands:

```csharp
public enum QueuePressureBand
{
    Normal,      // Load < 0.7
    Elevated,     // 0.7 <= Load < 0.85
    High,         // 0.85 <= Load < 1.0
    Critical      // Load >= 1.0
}
```

### Adaptive Queue Limit Calculation

The queue limit is dynamically adjusted based on:

1. **Base Budget**: Calculated from render and simulation distances
2. **Slack Ratio**: Multiplier for queue capacity (2.4 - 6.0)
3. **Burst Multiplier**: Additional multiplier during burst periods (1.0 - 3.0)
4. **Shock Absorber**: Damps sudden load changes
5. **Trend Boost**: Accounts for load trends

```csharp
int adaptiveLimit = Math.Clamp(
    (int)Math.Ceiling(Math.Max(128, budget) * adaptiveSlack * burstMultiplier),
    128,
    16384);
```

### Load Shedding

The system implements load shedding to prevent overload:

1. **Load Shedding Threshold**: Typically 0.84 - 0.92
2. **Shedding Limit**: Calculated as `queueLimit * loadSheddingThreshold`
3. **Backoff Delay**: Delay based on pressure factor (5-8ms)

### Emergency Brake

When queue pressure becomes critical:

1. **Emergency Brake Threshold**: Typically 1.02 - 1.2
2. **Emergency Hold**: Maintains brake state for configurable ticks
3. **Recovery Ramp**: Gradually releases brake over time
4. **Enhanced Drain**: Increased drain factor during emergency

## Profile-Based Configuration

### WorldMapControlProfile

**Purpose**: Stores configuration and state for world map control.

**Key Properties**:

- `Version`: Profile version number
- `ProfileHash`: SHA-256 hash of profile content
- `HydrologySignature`: Signature of hydrology version
- `ChunkSize`: Size of chunks in blocks
- `RenderDistance`: Render distance in chunks
- `SimulationDistance`: Simulation distance in chunks
- `GlobalWaterLevel`: Global water level

### Profile Reload Logic

The system automatically reloads profiles when:

1. **Config File Updated**: World config file modification time changes
2. **Profile File Updated**: Profile file modification time changes
3. **Hash Mismatch**: Profile hash doesn't match computed hash
4. **Signature Mismatch**: Hydrology signature doesn't match
5. **Version Mismatch**: Profile version is outdated

### Profile Persistence

Profiles are persisted to JSON files:

- **Default Path**: `config/world_map_control_profile.json`
- **Format**: JSON
- **Hashing**: SHA-256 for integrity verification

## Generation Signature

### Purpose

The generation signature uniquely identifies the terrain generation configuration to ensure consistency across sessions.

### Components

The signature includes:

1. **Pipeline Version**: Hydrology signature
2. **World Name**: Name of the world
3. **Seed**: World generation seed
4. **Protocol Fingerprint**: Protobuf descriptor fingerprint
5. **Profile Version**: Map control profile version
6. **Profile Hash**: Hash of profile content
7. **Config Hash**: Hash of world config
8. **Hydrology Signature**: Hydrology version signature
9. **Generation Parameters**: All terrain generation parameters
10. **Queue Parameters**: Queue management parameters

### Usage

The signature is used to:

- Detect configuration changes
- Invalidate cached chunks when parameters change
- Ensure consistent terrain generation across sessions
- Verify client-server compatibility

## Chunk Caching Strategy

### Cache Budget

The cache budget is calculated based on:

1. **Render Window**: `(renderDistance * 2 + 1)^2`
2. **Simulation Window**: `(simulationDistance * 2 + 1)^2`
3. **Slack**: Additional capacity for burst handling
4. **Inflight Pressure**: Additional capacity for in-flight generations

```csharp
int budget = Math.Max(renderWindow, simulationWindow) + slack + inflightPressure;
```

### LRU Eviction

Chunks are evicted using LRU (Least Recently Used) policy:

1. **Access Tracking**: Tracks last access time for each chunk
2. **Timeout**: Unloads chunks after configurable timeout
3. **Budget Enforcement**: Enforces cache budget by evicting least recently used

### Inflight Generation Management

Manages in-flight chunk generations:

1. **Task Tracking**: Tracks all in-flight generation tasks
2. **Timeout Handling**: Removes stale in-flight tasks
3. **Stale Detection**: Identifies tasks that are no longer needed
4. **Pruning**: Periodically removes completed and stale tasks

## Request Handling

### WorldMapRequest Types

```csharp
public enum WorldMapRequestType
{
    GetInitialMap,      // Initial map load
    UpdateChunk,        // Chunk update request
    GetPlayerProfile,    // Get player profile
    UpdatePlayerProfile  // Update player profile
}
```

### Initial Map Handling

1. **Profile Loading**: Loads or creates player profile
2. **Chunk Enumeration**: Enumerates chunks by distance
3. **Prioritization**: Prioritizes chunks based on distance and pressure
4. **Generation**: Generates chunks in priority order
5. **Response**: Returns initial map data

### Chunk Update Handling

1. **Profile Loading**: Loads or creates player profile
2. **Update Processing**: Processes chunk update requests
3. **Prioritization**: Prioritizes updates based on distance
4. **Generation**: Generates updated chunks
5. **Response**: Returns updated map data

### Profile Handling

1. **Profile Retrieval**: Gets or creates player profile
2. **Update Processing**: Processes profile updates
3. **Validation**: Validates profile changes
4. **Persistence**: Saves updated profile
5. **Response**: Returns updated profile

## Performance Optimizations

### Adaptive Queue Policy

- **Dynamic Limits**: Queue limits adjust based on load
- **Pressure-Based Delays**: Backoff delays based on pressure
- **Load Shedding**: Shed requests when overloaded
- **Emergency Brake**: Emergency throttling when critical

### Caching Strategy

- **Chunk Caching**: Caches generated chunks
- **LRU Eviction**: Evicts least recently used chunks
- **Access Tracking**: Tracks chunk access times
- **Budget Enforcement**: Enforces cache budget

### Pipeline Optimization

- **Async Generation**: Asynchronous chunk generation
- **Task Reuse**: Reuses in-flight generation tasks
- **Pipeline Caching**: Caches generation pipeline
- **Profile Caching**: Caches loaded profiles

## Configuration

### WorldMapControlSettings

**Purpose**: Configuration for world map control manager.

**Key Settings**:

- `DefaultRenderDistance`: Default render distance
- `DefaultUnloadDistance`: Default unload distance
- `DefaultMapScale`: Default map scale
- `MaxCachedChunks`: Maximum cached chunks
- `MaxQueuedChunkRequests`: Maximum queued chunk requests
- `UpdateBatchSize`: Size of update batches
- `UpdateIntervalMs`: Interval between updates

### Queue Policy Settings

- `QueuePressureFactor`: Queue pressure factor (1-8)
- `QueueSlackRatio`: Queue slack ratio (1.1-6.0)
- `QueueBurstSlackMultiplier`: Burst slack multiplier (1.0-3.0)
- `QueueLoadSheddingThreshold`: Load shedding threshold (0.5-0.98)
- `QueueEmergencyBrakeThreshold`: Emergency brake threshold (0.75-4.0)
- `QueueLoadEmaBlend`: EMA blend factor (0.18-0.28)
- `QueueEmergencyReleaseRatio`: Emergency release ratio (0.84-0.84)
- `QueueTrendBoostWeight`: Trend boost weight (0.2-0.3)
- `QueueShockAbsorberWeight`: Shock absorber weight (0.16-0.28)
- `QueueOverloadDrainFactor`: Overload drain factor (1-16)
- `QueueBackoffDelayMs`: Backoff delay in ms (1-200)
- `QueueEmergencyHoldTicks`: Emergency hold ticks (1-128)
- `QueueRecoveryRampTicks`: Recovery ramp ticks (1-256)
- `QueueHotspotBias`: Hotspot bias (0.0-1.0)
- `QueueHotspotEmergencyPenalty`: Hotspot emergency penalty (0.0-2.0)

## Integration Points

### Enhanced Terrain Generation Pipeline

The world map control system integrates with:

1. **EnhancedTerrainGenerationPipeline**: Main terrain generation pipeline
2. **ImprovedCaveGenerator**: Cave generation
3. **ImprovedRiverGenerator**: River generation
4. **ImprovedLakeGenerator**: Lake generation

### Protocol Integration

The system integrates with the protocol system through:

1. **WorldMapRequest/Response**: Protocol messages for world map
2. **ChunkData**: Chunk data structure
3. **PlayerProfile**: Player profile structure
4. **WorldMapControlProfile**: Control profile structure

## Testing and Validation

### Compilation Status

- ✅ SharedProtocol builds successfully (10 warnings, 0 errors)
- ✅ GameServer builds successfully (37 warnings, 0 errors)

### Known Warnings

Most warnings are related to nullable reference types:

- Nullable reference warnings in WorldSyncMessages
- Nullable reference warnings in Session
- Async method warnings (missing await operators)

## Future Improvements

### Potential Enhancements

1. **Distributed Caching**: Implement distributed chunk caching
2. **Predictive Preloading**: Predict and preload chunks
3. **Adaptive Quality**: Adjust generation quality based on load
4. **Multi-Threaded Generation**: Parallel chunk generation
5. **Compression**: Implement chunk data compression

### Research Areas

1. **Machine Learning**: ML-based prediction for chunk loading
2. **Network Optimization**: Optimize network transfer of chunks
3. **Memory Management**: Advanced memory management strategies
4. **Performance Monitoring**: Real-time performance monitoring

## References

- [`GameServer/World/WorldMapController.cs`](../GameServer/World/WorldMapController.cs)
- [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- [`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`](../GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs)
- [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)
- [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Documentation, config updates, dummy client creation

## Overview

This document describes the world map control architecture improvements made during Session 116, focusing on centralized chunk generation, adaptive queue management, and profile-based configuration.

## Architecture Components

### 1. WorldMapController

**File**: [`GameServer/World/WorldMapController.cs`](../GameServer/World/WorldMapController.cs)

**Purpose**: Centralized world map controller responsible for generating and caching chunks, persisting the map-control profile, and coordinating hydrology-aware generation.

**Key Features**:

- **Chunk Generation and Caching**: Manages chunk generation tasks and caches generated chunks
- **Profile Management**: Loads and persists world map control profiles
- **Adaptive Queue Management**: Implements sophisticated queue pressure management
- **Pipeline Coordination**: Orchestrates the enhanced terrain generation pipeline

**Core Methods**:

```csharp
public async Task<ChunkData> GetChunkAsync(int chunkX, int chunkZ, CancellationToken cancellationToken = default)
public async Task PreloadAsync(int centerX, int centerZ, int radius, CancellationToken cancellationToken = default)
public void Dispose()
```

### 2. WorldMapControlManager

**File**: [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

**Purpose**: Lightweight world map control service that reuses the enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

**Key Features**:

- **Per-Player Profiles**: Manages individual player map preferences
- **Chunk Preview Generation**: Generates preview chunks for map display
- **Queue Policy Management**: Implements advanced queue policies
- **Request Handling**: Handles various world map request types

**Core Methods**:

```csharp
public Task<WorldMapResponse> HandleAsync(WorldMapRequest request)
private async Task<WorldMapResponse> HandleInitialMapAsync(WorldMapRequest request)
private async Task<WorldMapResponse> HandleChunkUpdateAsync(WorldMapRequest request)
private Task<WorldMapResponse> HandleProfileAsync(WorldMapRequest request, bool updateProfile)
```

## Adaptive Queue Management

### Queue Pressure Bands

The system classifies queue pressure into four bands:

```csharp
public enum QueuePressureBand
{
    Normal,      // Load < 0.7
    Elevated,     // 0.7 <= Load < 0.85
    High,         // 0.85 <= Load < 1.0
    Critical      // Load >= 1.0
}
```

### Adaptive Queue Limit Calculation

The queue limit is dynamically adjusted based on:

1. **Base Budget**: Calculated from render and simulation distances
2. **Slack Ratio**: Multiplier for queue capacity (2.4 - 6.0)
3. **Burst Multiplier**: Additional multiplier during burst periods (1.0 - 3.0)
4. **Shock Absorber**: Damps sudden load changes
5. **Trend Boost**: Accounts for load trends

```csharp
int adaptiveLimit = Math.Clamp(
    (int)Math.Ceiling(Math.Max(128, budget) * adaptiveSlack * burstMultiplier),
    128,
    16384);
```

### Load Shedding

The system implements load shedding to prevent overload:

1. **Load Shedding Threshold**: Typically 0.84 - 0.92
2. **Shedding Limit**: Calculated as `queueLimit * loadSheddingThreshold`
3. **Backoff Delay**: Delay based on pressure factor (5-8ms)

### Emergency Brake

When queue pressure becomes critical:

1. **Emergency Brake Threshold**: Typically 1.02 - 1.2
2. **Emergency Hold**: Maintains brake state for configurable ticks
3. **Recovery Ramp**: Gradually releases brake over time
4. **Enhanced Drain**: Increased drain factor during emergency

## Profile-Based Configuration

### WorldMapControlProfile

**Purpose**: Stores configuration and state for world map control.

**Key Properties**:

- `Version`: Profile version number
- `ProfileHash`: SHA-256 hash of profile content
- `HydrologySignature`: Signature of hydrology version
- `ChunkSize`: Size of chunks in blocks
- `RenderDistance`: Render distance in chunks
- `SimulationDistance`: Simulation distance in chunks
- `GlobalWaterLevel`: Global water level

### Profile Reload Logic

The system automatically reloads profiles when:

1. **Config File Updated**: World config file modification time changes
2. **Profile File Updated**: Profile file modification time changes
3. **Hash Mismatch**: Profile hash doesn't match computed hash
4. **Signature Mismatch**: Hydrology signature doesn't match
5. **Version Mismatch**: Profile version is outdated

### Profile Persistence

Profiles are persisted to JSON files:

- **Default Path**: `config/world_map_control_profile.json`
- **Format**: JSON
- **Hashing**: SHA-256 for integrity verification

## Generation Signature

### Purpose

The generation signature uniquely identifies the terrain generation configuration to ensure consistency across sessions.

### Components

The signature includes:

1. **Pipeline Version**: Hydrology signature
2. **World Name**: Name of the world
3. **Seed**: World generation seed
4. **Protocol Fingerprint**: Protobuf descriptor fingerprint
5. **Profile Version**: Map control profile version
6. **Profile Hash**: Hash of profile content
7. **Config Hash**: Hash of world config
8. **Hydrology Signature**: Hydrology version signature
9. **Generation Parameters**: All terrain generation parameters
10. **Queue Parameters**: Queue management parameters

### Usage

The signature is used to:

- Detect configuration changes
- Invalidate cached chunks when parameters change
- Ensure consistent terrain generation across sessions
- Verify client-server compatibility

## Chunk Caching Strategy

### Cache Budget

The cache budget is calculated based on:

1. **Render Window**: `(renderDistance * 2 + 1)^2`
2. **Simulation Window**: `(simulationDistance * 2 + 1)^2`
3. **Slack**: Additional capacity for burst handling
4. **Inflight Pressure**: Additional capacity for in-flight generations

```csharp
int budget = Math.Max(renderWindow, simulationWindow) + slack + inflightPressure;
```

### LRU Eviction

Chunks are evicted using LRU (Least Recently Used) policy:

1. **Access Tracking**: Tracks last access time for each chunk
2. **Timeout**: Unloads chunks after configurable timeout
3. **Budget Enforcement**: Enforces cache budget by evicting least recently used

### Inflight Generation Management

Manages in-flight chunk generations:

1. **Task Tracking**: Tracks all in-flight generation tasks
2. **Timeout Handling**: Removes stale in-flight tasks
3. **Stale Detection**: Identifies tasks that are no longer needed
4. **Pruning**: Periodically removes completed and stale tasks

## Request Handling

### WorldMapRequest Types

```csharp
public enum WorldMapRequestType
{
    GetInitialMap,      // Initial map load
    UpdateChunk,        // Chunk update request
    GetPlayerProfile,    // Get player profile
    UpdatePlayerProfile  // Update player profile
}
```

### Initial Map Handling

1. **Profile Loading**: Loads or creates player profile
2. **Chunk Enumeration**: Enumerates chunks by distance
3. **Prioritization**: Prioritizes chunks based on distance and pressure
4. **Generation**: Generates chunks in priority order
5. **Response**: Returns initial map data

### Chunk Update Handling

1. **Profile Loading**: Loads or creates player profile
2. **Update Processing**: Processes chunk update requests
3. **Prioritization**: Prioritizes updates based on distance
4. **Generation**: Generates updated chunks
5. **Response**: Returns updated map data

### Profile Handling

1. **Profile Retrieval**: Gets or creates player profile
2. **Update Processing**: Processes profile updates
3. **Validation**: Validates profile changes
4. **Persistence**: Saves updated profile
5. **Response**: Returns updated profile

## Performance Optimizations

### Adaptive Queue Policy

- **Dynamic Limits**: Queue limits adjust based on load
- **Pressure-Based Delays**: Backoff delays based on pressure
- **Load Shedding**: Shed requests when overloaded
- **Emergency Brake**: Emergency throttling when critical

### Caching Strategy

- **Chunk Caching**: Caches generated chunks
- **LRU Eviction**: Evicts least recently used chunks
- **Access Tracking**: Tracks chunk access times
- **Budget Enforcement**: Enforces cache budget

### Pipeline Optimization

- **Async Generation**: Asynchronous chunk generation
- **Task Reuse**: Reuses in-flight generation tasks
- **Pipeline Caching**: Caches generation pipeline
- **Profile Caching**: Caches loaded profiles

## Configuration

### WorldMapControlSettings

**Purpose**: Configuration for world map control manager.

**Key Settings**:

- `DefaultRenderDistance`: Default render distance
- `DefaultUnloadDistance`: Default unload distance
- `DefaultMapScale`: Default map scale
- `MaxCachedChunks`: Maximum cached chunks
- `MaxQueuedChunkRequests`: Maximum queued chunk requests
- `UpdateBatchSize`: Size of update batches
- `UpdateIntervalMs`: Interval between updates

### Queue Policy Settings

- `QueuePressureFactor`: Queue pressure factor (1-8)
- `QueueSlackRatio`: Queue slack ratio (1.1-6.0)
- `QueueBurstSlackMultiplier`: Burst slack multiplier (1.0-3.0)
- `QueueLoadSheddingThreshold`: Load shedding threshold (0.5-0.98)
- `QueueEmergencyBrakeThreshold`: Emergency brake threshold (0.75-4.0)
- `QueueLoadEmaBlend`: EMA blend factor (0.18-0.28)
- `QueueEmergencyReleaseRatio`: Emergency release ratio (0.84-0.84)
- `QueueTrendBoostWeight`: Trend boost weight (0.2-0.3)
- `QueueShockAbsorberWeight`: Shock absorber weight (0.16-0.28)
- `QueueOverloadDrainFactor`: Overload drain factor (1-16)
- `QueueBackoffDelayMs`: Backoff delay in ms (1-200)
- `QueueEmergencyHoldTicks`: Emergency hold ticks (1-128)
- `QueueRecoveryRampTicks`: Recovery ramp ticks (1-256)
- `QueueHotspotBias`: Hotspot bias (0.0-1.0)
- `QueueHotspotEmergencyPenalty`: Hotspot emergency penalty (0.0-2.0)

## Integration Points

### Enhanced Terrain Generation Pipeline

The world map control system integrates with:

1. **EnhancedTerrainGenerationPipeline**: Main terrain generation pipeline
2. **ImprovedCaveGenerator**: Cave generation
3. **ImprovedRiverGenerator**: River generation
4. **ImprovedLakeGenerator**: Lake generation

### Protocol Integration

The system integrates with the protocol system through:

1. **WorldMapRequest/Response**: Protocol messages for world map
2. **ChunkData**: Chunk data structure
3. **PlayerProfile**: Player profile structure
4. **WorldMapControlProfile**: Control profile structure

## Testing and Validation

### Compilation Status

- ✅ SharedProtocol builds successfully (10 warnings, 0 errors)
- ✅ GameServer builds successfully (37 warnings, 0 errors)

### Known Warnings

Most warnings are related to nullable reference types:

- Nullable reference warnings in WorldSyncMessages
- Nullable reference warnings in Session
- Async method warnings (missing await operators)

## Future Improvements

### Potential Enhancements

1. **Distributed Caching**: Implement distributed chunk caching
2. **Predictive Preloading**: Predict and preload chunks
3. **Adaptive Quality**: Adjust generation quality based on load
4. **Multi-Threaded Generation**: Parallel chunk generation
5. **Compression**: Implement chunk data compression

### Research Areas

1. **Machine Learning**: ML-based prediction for chunk loading
2. **Network Optimization**: Optimize network transfer of chunks
3. **Memory Management**: Advanced memory management strategies
4. **Performance Monitoring**: Real-time performance monitoring

## References

- [`GameServer/World/WorldMapController.cs`](../GameServer/World/WorldMapController.cs)
- [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- [`GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs`](../GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs)
- [`config/enhanced_world_map_control_server.json`](../config/enhanced_world_map_control_server.json)
- [`config/enhanced_world_map_control_client.json`](../config/enhanced_world_map_control_client.json)

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Documentation, config updates, dummy client creation

