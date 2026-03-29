# World Map Control Documentation

## Overview

This document describes the world map control system used in the Minecraft-style game server. The world map control system provides profile-based map preferences, chunk caching, and generation signature validation.

## Architecture

### Core Components

- **WorldMapControlManager.cs**: Central manager for world map control
- **WorldMapControlProfile.cs**: Profile definition for map control preferences
- **WorldMapControlProfileManager.cs**: Profile management with hot-reload

### Key Features

1. **Profile-based Configuration**: Different profiles for different map types
2. **Chunk Caching**: Efficient caching of generated chunks
3. **Generation Signature**: Validation of generation consistency
4. **Hot-reload Support**: Configuration can be reloaded without restart
5. **Config Hash Drift Detection**: Detects configuration changes

## Profile System

### Profile Structure

A world map control profile defines preferences for terrain generation:

```csharp
public class WorldMapControlProfile
{
    public string ProfileName { get; set; }
    public int Version { get; set; }
    public WorldGenerationSettings Generation { get; set; }
    public WorldEnvironmentSettings Environment { get; set; }
    public WorldFeatureSettings Features { get; set; }
}
```

### Profile Management

The profile manager provides:

- **Profile Loading**: Load profiles from JSON files
- **Profile Validation**: Validate profile structure and values
- **Profile Hot-reload**: Reload profiles without restart
- **Profile Caching**: Cache loaded profiles for performance

### Profile Configuration

Profiles are stored in `config/world_map_control_profile.json`:

```json
{
  "ProfileName": "default",
  "Version": 1,
  "Generation": {
    "TerrainScale": 1.0,
    "HeightScale": 1.0,
    "BiomeScale": 1.0,
    "NoiseSeed": 0
  },
  "Environment": {
    "SeaLevel": 62,
    "BedrockLevel": 5,
    "WorldHeight": 256
  },
  "Features": {
    "EnableCaves": true,
    "EnableRivers": true,
    "EnableLakes": true,
    "EnableOres": true,
    "EnableStructures": true
  }
}
```

## Chunk Caching

### Cache Architecture

The chunk caching system provides:

- **LRU Cache**: Least recently used cache eviction
- **Budget Enforcement**: Enforces cache size limits
- **Async Loading**: Asynchronous chunk loading
- **Cache Invalidation**: Intelligent cache invalidation

### Cache Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `ChunkCacheSize` | Maximum number of cached chunks | 1000 |
| `ChunkLoadRadius` | Radius of chunks to load | 8 |
| `ChunkUnloadTimeoutMinutes` | Timeout before unloading | 30 |

### Cache Implementation

```csharp
public class ChunkCache
{
    private readonly Dictionary<Vector2Int, ChunkData> _cache;
    private readonly LinkedList<Vector2Int> _lruList;
    private readonly int _maxSize;

    public ChunkCache(int maxSize)
    {
        _cache = new Dictionary<Vector2Int, ChunkData>();
        _lruList = new LinkedList<Vector2Int>();
        _maxSize = maxSize;
    }

    public ChunkData GetOrAdd(Vector2Int chunkPos, Func<ChunkData> factory)
    {
        if (_cache.TryGetValue(chunkPos, out var data))
        {
            // Move to front of LRU list
            _lruList.Remove(chunkPos);
            _lruList.AddFirst(chunkPos);
            return data;
        }

        data = factory();
        _cache[chunkPos] = data;
        _lruList.AddFirst(chunkPos);

        // Enforce budget
        while (_cache.Count > _maxSize)
        {
            var oldest = _lruList.Last.Value;
            _cache.Remove(oldest);
            _lruList.RemoveLast();
        }

        return data;
    }
}
```

## Generation Signature

### Signature Calculation

The generation signature is a hash of all generation parameters:

```csharp
public string ComputeGenerationSignature()
{
    var signatureBuilder = new StringBuilder();

    // Include profile name and version
    signatureBuilder.Append(ProfileName);
    signatureBuilder.Append(Version);

    // Include generation settings
    signatureBuilder.Append(Generation.TerrainScale);
    signatureBuilder.Append(Generation.HeightScale);
    signatureBuilder.Append(Generation.BiomeScale);
    signatureBuilder.Append(Generation.NoiseSeed);

    // Include environment settings
    signatureBuilder.Append(Environment.SeaLevel);
    signatureBuilder.Append(Environment.BedrockLevel);
    signatureBuilder.Append(Environment.WorldHeight);

    // Include feature settings
    signatureBuilder.Append(Features.EnableCaves);
    signatureBuilder.Append(Features.EnableRivers);
    signatureBuilder.Append(Features.EnableLakes);

    // Compute hash
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(signatureBuilder.ToString());
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
```

### Signature Validation

The signature is used to validate generation consistency:

- **Chunk Validation**: Validate chunks match expected signature
- **Cache Invalidation**: Invalidate cache when signature changes
- **Drift Detection**: Detect configuration drift over time

## Config Hash Drift Detection

### Hash Calculation

The config hash is computed from the configuration file:

```csharp
public string ComputeConfigHash(string configPath)
{
    var json = File.ReadAllText(configPath);
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(json);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
```

### Drift Detection

Drift detection compares the current config hash with the stored hash:

```csharp
public bool DetectConfigDrift(string configPath, string storedHash)
{
    var currentHash = ComputeConfigHash(configPath);
    return currentHash != storedHash;
}
```

## Hot-reload Support

### Reload Mechanism

The hot-reload mechanism allows configuration changes without restart:

```csharp
public void ReloadProfile(string profilePath)
{
    var newProfile = LoadProfile(profilePath);
    var newSignature = newProfile.ComputeGenerationSignature();

    if (newSignature != CurrentProfile.ComputeGenerationSignature())
    {
        // Signature changed, invalidate cache
        InvalidateChunkCache();
    }

    CurrentProfile = newProfile;
    ConfigHash = ComputeConfigHash(profilePath);
}
```

### Reload Triggers

Configuration is reloaded on:

- **Manual Request**: Explicit reload request
- **File Watch**: Configuration file change detected
- **Periodic Check**: Periodic check for changes

## Configuration

### Server Configuration

Server-side world map control is configured in `config/world.json`:

```json
{
  "WorldName": "HELLO_MY_WORLD",
  "Seed": 0,
  "GameMode": "survival",
  "WorldHeight": 256,
  "ChunkSize": 16,
  "RenderDistance": 10,
  "SimulationDistance": 12,
  "MapControlProfilePath": "config/world_map_control_profile.json",
  "MapControlProfileVersion": 1
}
```

### Client Configuration

Client-side world map control is configured in `config/client_config.json`:

```json
{
  "client": {
    "world": {
      "seed": "",
      "worldType": "default",
      "generateStructures": true,
      "generateVillages": true,
      "generateTemples": true,
      "generateMineshafts": true
    }
  }
}
```

## Implementation Notes

### Performance Considerations

- **Chunk Caching**: Reduces redundant generation
- **Async Loading**: Prevents blocking the main thread
- **Budget Enforcement**: Prevents memory overflow
- **LRU Eviction**: Efficient cache management

### Stability Guarantees

- **Generation Signature**: Ensures generation consistency
- **Config Hash**: Detects configuration drift
- **Hot-reload**: Allows configuration changes without restart
- **Cache Invalidation**: Prevents stale data

### Extensibility

The world map control system is designed for extensibility:

- **Profile-based**: Different profiles for different map types
- **Configurable**: All parameters are configurable via JSON
- **Hot-reload**: Configuration can be reloaded without restart
- **Modular**: Easy to add new features

## References

- [`WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- [`WorldMapControlProfile.cs`](../GameServer/World/WorldMapControlProfile.cs)
- [`config/world.json`](../config/world.json)
- [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- [`config/client_config.json`](../config/client_config.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

## Overview

This document describes the world map control system used in the Minecraft-style game server. The world map control system provides profile-based map preferences, chunk caching, and generation signature validation.

## Architecture

### Core Components

- **WorldMapControlManager.cs**: Central manager for world map control
- **WorldMapControlProfile.cs**: Profile definition for map control preferences
- **WorldMapControlProfileManager.cs**: Profile management with hot-reload

### Key Features

1. **Profile-based Configuration**: Different profiles for different map types
2. **Chunk Caching**: Efficient caching of generated chunks
3. **Generation Signature**: Validation of generation consistency
4. **Hot-reload Support**: Configuration can be reloaded without restart
5. **Config Hash Drift Detection**: Detects configuration changes

## Profile System

### Profile Structure

A world map control profile defines preferences for terrain generation:

```csharp
public class WorldMapControlProfile
{
    public string ProfileName { get; set; }
    public int Version { get; set; }
    public WorldGenerationSettings Generation { get; set; }
    public WorldEnvironmentSettings Environment { get; set; }
    public WorldFeatureSettings Features { get; set; }
}
```

### Profile Management

The profile manager provides:

- **Profile Loading**: Load profiles from JSON files
- **Profile Validation**: Validate profile structure and values
- **Profile Hot-reload**: Reload profiles without restart
- **Profile Caching**: Cache loaded profiles for performance

### Profile Configuration

Profiles are stored in `config/world_map_control_profile.json`:

```json
{
  "ProfileName": "default",
  "Version": 1,
  "Generation": {
    "TerrainScale": 1.0,
    "HeightScale": 1.0,
    "BiomeScale": 1.0,
    "NoiseSeed": 0
  },
  "Environment": {
    "SeaLevel": 62,
    "BedrockLevel": 5,
    "WorldHeight": 256
  },
  "Features": {
    "EnableCaves": true,
    "EnableRivers": true,
    "EnableLakes": true,
    "EnableOres": true,
    "EnableStructures": true
  }
}
```

## Chunk Caching

### Cache Architecture

The chunk caching system provides:

- **LRU Cache**: Least recently used cache eviction
- **Budget Enforcement**: Enforces cache size limits
- **Async Loading**: Asynchronous chunk loading
- **Cache Invalidation**: Intelligent cache invalidation

### Cache Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `ChunkCacheSize` | Maximum number of cached chunks | 1000 |
| `ChunkLoadRadius` | Radius of chunks to load | 8 |
| `ChunkUnloadTimeoutMinutes` | Timeout before unloading | 30 |

### Cache Implementation

```csharp
public class ChunkCache
{
    private readonly Dictionary<Vector2Int, ChunkData> _cache;
    private readonly LinkedList<Vector2Int> _lruList;
    private readonly int _maxSize;

    public ChunkCache(int maxSize)
    {
        _cache = new Dictionary<Vector2Int, ChunkData>();
        _lruList = new LinkedList<Vector2Int>();
        _maxSize = maxSize;
    }

    public ChunkData GetOrAdd(Vector2Int chunkPos, Func<ChunkData> factory)
    {
        if (_cache.TryGetValue(chunkPos, out var data))
        {
            // Move to front of LRU list
            _lruList.Remove(chunkPos);
            _lruList.AddFirst(chunkPos);
            return data;
        }

        data = factory();
        _cache[chunkPos] = data;
        _lruList.AddFirst(chunkPos);

        // Enforce budget
        while (_cache.Count > _maxSize)
        {
            var oldest = _lruList.Last.Value;
            _cache.Remove(oldest);
            _lruList.RemoveLast();
        }

        return data;
    }
}
```

## Generation Signature

### Signature Calculation

The generation signature is a hash of all generation parameters:

```csharp
public string ComputeGenerationSignature()
{
    var signatureBuilder = new StringBuilder();

    // Include profile name and version
    signatureBuilder.Append(ProfileName);
    signatureBuilder.Append(Version);

    // Include generation settings
    signatureBuilder.Append(Generation.TerrainScale);
    signatureBuilder.Append(Generation.HeightScale);
    signatureBuilder.Append(Generation.BiomeScale);
    signatureBuilder.Append(Generation.NoiseSeed);

    // Include environment settings
    signatureBuilder.Append(Environment.SeaLevel);
    signatureBuilder.Append(Environment.BedrockLevel);
    signatureBuilder.Append(Environment.WorldHeight);

    // Include feature settings
    signatureBuilder.Append(Features.EnableCaves);
    signatureBuilder.Append(Features.EnableRivers);
    signatureBuilder.Append(Features.EnableLakes);

    // Compute hash
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(signatureBuilder.ToString());
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
```

### Signature Validation

The signature is used to validate generation consistency:

- **Chunk Validation**: Validate chunks match expected signature
- **Cache Invalidation**: Invalidate cache when signature changes
- **Drift Detection**: Detect configuration drift over time

## Config Hash Drift Detection

### Hash Calculation

The config hash is computed from the configuration file:

```csharp
public string ComputeConfigHash(string configPath)
{
    var json = File.ReadAllText(configPath);
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(json);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
```

### Drift Detection

Drift detection compares the current config hash with the stored hash:

```csharp
public bool DetectConfigDrift(string configPath, string storedHash)
{
    var currentHash = ComputeConfigHash(configPath);
    return currentHash != storedHash;
}
```

## Hot-reload Support

### Reload Mechanism

The hot-reload mechanism allows configuration changes without restart:

```csharp
public void ReloadProfile(string profilePath)
{
    var newProfile = LoadProfile(profilePath);
    var newSignature = newProfile.ComputeGenerationSignature();

    if (newSignature != CurrentProfile.ComputeGenerationSignature())
    {
        // Signature changed, invalidate cache
        InvalidateChunkCache();
    }

    CurrentProfile = newProfile;
    ConfigHash = ComputeConfigHash(profilePath);
}
```

### Reload Triggers

Configuration is reloaded on:

- **Manual Request**: Explicit reload request
- **File Watch**: Configuration file change detected
- **Periodic Check**: Periodic check for changes

## Configuration

### Server Configuration

Server-side world map control is configured in `config/world.json`:

```json
{
  "WorldName": "HELLO_MY_WORLD",
  "Seed": 0,
  "GameMode": "survival",
  "WorldHeight": 256,
  "ChunkSize": 16,
  "RenderDistance": 10,
  "SimulationDistance": 12,
  "MapControlProfilePath": "config/world_map_control_profile.json",
  "MapControlProfileVersion": 1
}
```

### Client Configuration

Client-side world map control is configured in `config/client_config.json`:

```json
{
  "client": {
    "world": {
      "seed": "",
      "worldType": "default",
      "generateStructures": true,
      "generateVillages": true,
      "generateTemples": true,
      "generateMineshafts": true
    }
  }
}
```

## Implementation Notes

### Performance Considerations

- **Chunk Caching**: Reduces redundant generation
- **Async Loading**: Prevents blocking the main thread
- **Budget Enforcement**: Prevents memory overflow
- **LRU Eviction**: Efficient cache management

### Stability Guarantees

- **Generation Signature**: Ensures generation consistency
- **Config Hash**: Detects configuration drift
- **Hot-reload**: Allows configuration changes without restart
- **Cache Invalidation**: Prevents stale data

### Extensibility

The world map control system is designed for extensibility:

- **Profile-based**: Different profiles for different map types
- **Configurable**: All parameters are configurable via JSON
- **Hot-reload**: Configuration can be reloaded without restart
- **Modular**: Easy to add new features

## References

- [`WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)
- [`WorldMapControlProfile.cs`](../GameServer/World/WorldMapControlProfile.cs)
- [`config/world.json`](../config/world.json)
- [`config/world_map_control_profile.json`](../config/world_map_control_profile.json)
- [`config/client_config.json`](../config/client_config.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

