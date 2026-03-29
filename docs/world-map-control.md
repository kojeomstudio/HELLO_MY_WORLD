# World Map Control Documentation

## Overview

This document describes the world map control system for the Minecraft-like game server. The system manages chunk generation, caching, and synchronization between server and client with adaptive load management and profile-based configuration.

## Architecture

### Core Components

1. **WorldMapControlManager** (Server) - Manages world map control with adaptive queue pressure bands
2. **WorldMapControlProfile** (Server) - Server-side profile with JSON serialization
3. **WorldMapControlProfile** (Client) - Client-side profile with JSON serialization

### Configuration

All world map control parameters are configured via JSON files:
- Server: [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- Client: [`Assets/StreamingAssets/world-map-control.json`](../Assets/StreamingAssets/world-map-control.json)

## Server-Side Implementation

### File: [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

### Key Features

- **Adaptive queue pressure bands**: Dynamic load management based on system pressure
- **Chunk caching**: Efficient caching with budget enforcement
- **Profile versioning**: Version tracking (currently v48) for compatibility
- **Hydrology signature**: Version tracking (currently v44) for hydrology features

### Queue Pressure Bands

The system uses adaptive pressure bands to manage chunk generation requests:

```csharp
// Pressure bands (0-1 scale)
const double EMERGENCY_PRESSURE = 0.95;  // Emergency brake
const double HIGH_PRESSURE = 0.80;        // High load
const double NORMAL_PRESSURE = 0.50;      // Normal load
const double LOW_PRESSURE = 0.20;         // Low load
```

### Chunk Caching

The system implements chunk caching with budget enforcement:

```csharp
// Cache budget
const int MAX_CACHE_SIZE = 256;
const int CACHE_PRUNE_THRESHOLD = 200;
```

### Profile Management

The profile is saved to `config/world_map_control_profile.json` with version v48 and hydrology signature v44.

### Key Methods

- **HandleAsync**: Handles world map control requests
- **HandleInitialMapAsync**: Handles initial map generation
- **HandleChunkUpdateAsync**: Handles chunk updates
- **HandleProfileAsync**: Handles profile requests
- **GenerateOrGetChunkAsync**: Generates or retrieves cached chunks

## Client-Side Implementation

### File: [`Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs)

### Key Features

- **JSON serialization**: Profile data is serialized to/from JSON
- **Hash validation**: Validates profile hash for compatibility
- **Version checking**: Checks version compatibility with server
- **Hydrology signature validation**: Validates hydrology features

### Key Methods

- **LoadFromFile**: Loads profile from JSON file
- **SaveToFile**: Saves profile to JSON file
- **FromConfig**: Creates profile from configuration
- **FromData**: Creates profile from data
- **ComputeHash**: Computes profile hash

## Profile Structure

### Server Profile

```json
{
  "version": 48,
  "profileHash": "42d1a6af63cd",
  "hydrologySignature": 44,
  "chunkSize": 16,
  "renderDistance": 10,
  "simulationDistance": 8,
  "globalWaterLevel": 62,
  "curvatureThreshold": 0.42,
  "riparianBuffer": 4,
  "riverSeamFill": 0.8,
  "lakeWetlandBuffer": 6,
  "caveCeilingStability": 0.46,
  "ceilingClamp": 0.42,
  "riparianCaveGuard": 0.64
}
```

### Client Profile

The client profile mirrors the server profile structure for compatibility.

## Configuration Parameters

### Chunk Settings

```json
{
  "chunkSize": 16,
  "renderDistance": 10,
  "simulationDistance": 8
}
```

### Water Settings

```json
{
  "globalWaterLevel": 62,
  "curvatureThreshold": 0.42
}
```

### Hydrology Settings

```json
{
  "riparianBuffer": 4,
  "riverSeamFill": 0.8,
  "lakeWetlandBuffer": 6,
  "caveCeilingStability": 0.46,
  "ceilingClamp": 0.42,
  "riparianCaveGuard": 0.64
}
```

## Version Management

### Profile Version

- **Current Version**: v48
- **Purpose**: Tracks profile structure changes
- **Validation**: Server and client must have compatible versions

### Hydrology Signature

- **Current Signature**: v44
- **Purpose**: Tracks hydrology feature changes
- **Validation**: Ensures hydrology features are compatible

## Cache Management

### Cache Strategy

1. **LRU Eviction**: Least recently used chunks are evicted first
2. **Budget Enforcement**: Cache size is limited by budget
3. **Pruning Threshold**: Cache is pruned when exceeding threshold

### Cache Keys

Cache keys are based on chunk coordinates:

```csharp
string cacheKey = $"{chunkX},{chunkZ}";
```

## Load Management

### Adaptive Pressure

The system adapts to load based on queue pressure:

1. **Low Pressure**: Accept all requests
2. **Normal Pressure**: Accept most requests, throttle some
3. **High Pressure**: Throttle requests, prioritize critical chunks
4. **Emergency Pressure**: Reject non-critical requests

### Priority Levels

Chunks are prioritized based on:

1. **Player proximity**: Chunks near players have higher priority
2. **Render distance**: Chunks within render distance have higher priority
3. **Simulation distance**: Chunks within simulation distance have higher priority

## Synchronization

### Server to Client

1. **Profile sync**: Server sends profile to client on connection
2. **Chunk data**: Server sends chunk data as needed
3. **Updates**: Server sends updates for changed chunks

### Client to Server

1. **Chunk requests**: Client requests chunks as needed
2. **Profile validation**: Client validates profile compatibility
3. **Update acknowledgments**: Client acknowledges chunk updates

## Performance Considerations

- **Chunk-based generation**: Scalable generation system
- **Adaptive load management**: Dynamic adjustment based on system load
- **Efficient caching**: Reduces redundant generation
- **Profile validation**: Ensures compatibility before processing

## Error Handling

### Profile Mismatch

When profile versions don't match:

1. Server sends updated profile to client
2. Client validates and accepts updated profile
3. Client clears cache and re-requests chunks

### Hydrology Signature Mismatch

When hydrology signatures don't match:

1. Server sends updated profile to client
2. Client validates hydrology features
3. Client clears cache and re-requests chunks

### Cache Miss

When a requested chunk is not in cache:

1. System generates the chunk
2. Chunk is added to cache
3. Chunk is sent to client

## Future Improvements

1. **Distributed caching**: Share cache across multiple server instances
2. **Predictive generation**: Pre-generate chunks based on player movement
3. **Dynamic profile updates**: Allow runtime profile changes
4. **Improved load balancing**: Better distribution of generation load
5. **Cache compression**: Reduce memory usage for cached chunks

## References

- [Terrain Generation Documentation](./terrain-generation.md)
- [Protobuf Protocol Documentation](./protobuf-protocol.md)
- [Enhanced World Map Control Client Config](../config/enhanced_world_map_control_client.json)
- [Enhanced World Map Control Server Config](../config/enhanced_world_map_control_server.json)

## Overview

This document describes the world map control system for the Minecraft-like game server. The system manages chunk generation, caching, and synchronization between server and client with adaptive load management and profile-based configuration.

## Architecture

### Core Components

1. **WorldMapControlManager** (Server) - Manages world map control with adaptive queue pressure bands
2. **WorldMapControlProfile** (Server) - Server-side profile with JSON serialization
3. **WorldMapControlProfile** (Client) - Client-side profile with JSON serialization

### Configuration

All world map control parameters are configured via JSON files:
- Server: [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- Client: [`Assets/StreamingAssets/world-map-control.json`](../Assets/StreamingAssets/world-map-control.json)

## Server-Side Implementation

### File: [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

### Key Features

- **Adaptive queue pressure bands**: Dynamic load management based on system pressure
- **Chunk caching**: Efficient caching with budget enforcement
- **Profile versioning**: Version tracking (currently v48) for compatibility
- **Hydrology signature**: Version tracking (currently v44) for hydrology features

### Queue Pressure Bands

The system uses adaptive pressure bands to manage chunk generation requests:

```csharp
// Pressure bands (0-1 scale)
const double EMERGENCY_PRESSURE = 0.95;  // Emergency brake
const double HIGH_PRESSURE = 0.80;        // High load
const double NORMAL_PRESSURE = 0.50;      // Normal load
const double LOW_PRESSURE = 0.20;         // Low load
```

### Chunk Caching

The system implements chunk caching with budget enforcement:

```csharp
// Cache budget
const int MAX_CACHE_SIZE = 256;
const int CACHE_PRUNE_THRESHOLD = 200;
```

### Profile Management

The profile is saved to `config/world_map_control_profile.json` with version v48 and hydrology signature v44.

### Key Methods

- **HandleAsync**: Handles world map control requests
- **HandleInitialMapAsync**: Handles initial map generation
- **HandleChunkUpdateAsync**: Handles chunk updates
- **HandleProfileAsync**: Handles profile requests
- **GenerateOrGetChunkAsync**: Generates or retrieves cached chunks

## Client-Side Implementation

### File: [`Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs)

### Key Features

- **JSON serialization**: Profile data is serialized to/from JSON
- **Hash validation**: Validates profile hash for compatibility
- **Version checking**: Checks version compatibility with server
- **Hydrology signature validation**: Validates hydrology features

### Key Methods

- **LoadFromFile**: Loads profile from JSON file
- **SaveToFile**: Saves profile to JSON file
- **FromConfig**: Creates profile from configuration
- **FromData**: Creates profile from data
- **ComputeHash**: Computes profile hash

## Profile Structure

### Server Profile

```json
{
  "version": 48,
  "profileHash": "42d1a6af63cd",
  "hydrologySignature": 44,
  "chunkSize": 16,
  "renderDistance": 10,
  "simulationDistance": 8,
  "globalWaterLevel": 62,
  "curvatureThreshold": 0.42,
  "riparianBuffer": 4,
  "riverSeamFill": 0.8,
  "lakeWetlandBuffer": 6,
  "caveCeilingStability": 0.46,
  "ceilingClamp": 0.42,
  "riparianCaveGuard": 0.64
}
```

### Client Profile

The client profile mirrors the server profile structure for compatibility.

## Configuration Parameters

### Chunk Settings

```json
{
  "chunkSize": 16,
  "renderDistance": 10,
  "simulationDistance": 8
}
```

### Water Settings

```json
{
  "globalWaterLevel": 62,
  "curvatureThreshold": 0.42
}
```

### Hydrology Settings

```json
{
  "riparianBuffer": 4,
  "riverSeamFill": 0.8,
  "lakeWetlandBuffer": 6,
  "caveCeilingStability": 0.46,
  "ceilingClamp": 0.42,
  "riparianCaveGuard": 0.64
}
```

## Version Management

### Profile Version

- **Current Version**: v48
- **Purpose**: Tracks profile structure changes
- **Validation**: Server and client must have compatible versions

### Hydrology Signature

- **Current Signature**: v44
- **Purpose**: Tracks hydrology feature changes
- **Validation**: Ensures hydrology features are compatible

## Cache Management

### Cache Strategy

1. **LRU Eviction**: Least recently used chunks are evicted first
2. **Budget Enforcement**: Cache size is limited by budget
3. **Pruning Threshold**: Cache is pruned when exceeding threshold

### Cache Keys

Cache keys are based on chunk coordinates:

```csharp
string cacheKey = $"{chunkX},{chunkZ}";
```

## Load Management

### Adaptive Pressure

The system adapts to load based on queue pressure:

1. **Low Pressure**: Accept all requests
2. **Normal Pressure**: Accept most requests, throttle some
3. **High Pressure**: Throttle requests, prioritize critical chunks
4. **Emergency Pressure**: Reject non-critical requests

### Priority Levels

Chunks are prioritized based on:

1. **Player proximity**: Chunks near players have higher priority
2. **Render distance**: Chunks within render distance have higher priority
3. **Simulation distance**: Chunks within simulation distance have higher priority

## Synchronization

### Server to Client

1. **Profile sync**: Server sends profile to client on connection
2. **Chunk data**: Server sends chunk data as needed
3. **Updates**: Server sends updates for changed chunks

### Client to Server

1. **Chunk requests**: Client requests chunks as needed
2. **Profile validation**: Client validates profile compatibility
3. **Update acknowledgments**: Client acknowledges chunk updates

## Performance Considerations

- **Chunk-based generation**: Scalable generation system
- **Adaptive load management**: Dynamic adjustment based on system load
- **Efficient caching**: Reduces redundant generation
- **Profile validation**: Ensures compatibility before processing

## Error Handling

### Profile Mismatch

When profile versions don't match:

1. Server sends updated profile to client
2. Client validates and accepts updated profile
3. Client clears cache and re-requests chunks

### Hydrology Signature Mismatch

When hydrology signatures don't match:

1. Server sends updated profile to client
2. Client validates hydrology features
3. Client clears cache and re-requests chunks

### Cache Miss

When a requested chunk is not in cache:

1. System generates the chunk
2. Chunk is added to cache
3. Chunk is sent to client

## Future Improvements

1. **Distributed caching**: Share cache across multiple server instances
2. **Predictive generation**: Pre-generate chunks based on player movement
3. **Dynamic profile updates**: Allow runtime profile changes
4. **Improved load balancing**: Better distribution of generation load
5. **Cache compression**: Reduce memory usage for cached chunks

## References

- [Terrain Generation Documentation](./terrain-generation.md)
- [Protobuf Protocol Documentation](./protobuf-protocol.md)
- [Enhanced World Map Control Client Config](../config/enhanced_world_map_control_client.json)
- [Enhanced World Map Control Server Config](../config/enhanced_world_map_control_server.json)

