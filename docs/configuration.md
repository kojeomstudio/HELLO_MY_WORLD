# Configuration Documentation

## Overview

This document describes the configuration system used in the Minecraft-style game server. The configuration system provides JSON-based configuration with hot-reload support, data-driven approach, and comprehensive validation.

## Architecture

### Core Components

- **ServerConfig.cs**: Server configuration model
- **DataDrivenConfigManager.cs**: Data-driven configuration manager
- **Config Files**: JSON configuration files in `config/` directory

### Configuration Features

1. **JSON-based Configuration**: All configuration is stored in JSON format
2. **Hot-reload Support**: Configuration can be reloaded without restart
3. **Data-driven Approach**: Game data is driven by JSON configuration
4. **Validation**: Comprehensive validation of configuration values
5. **Environment Variables**: Support for environment variable overrides

## Configuration Files

### Server Configuration

**File**: `config/server.json`

The server configuration file contains network, database, performance, security, and logging settings:

```json
{
  "Network": {
    "Host": "0.0.0.0",
    "Port": 25565,
    "MaxPlayers": 20,
    "MaxConnectionsPerIP": 3,
    "ConnectionTimeoutSeconds": 30,
    "KeepAliveIntervalSeconds": 5,
    "PacketCompressionThreshold": 256
  },
  "Database": {
    "Provider": "sqlite",
    "ConnectionString": "Data Source=gameserver.db",
    "EnableAutoMigration": true,
    "CommandTimeoutSeconds": 30,
    "MaxPoolSize": 100
  },
  "Performance": {
    "TickRate": 20,
    "ChunkLoadThreads": 4,
    "MaxChunkLoadsPerTick": 10,
    "ChunkUnloadDelay": 30,
    "EntityUpdateDistance": 128,
    "EnableAsyncChunkGeneration": true,
    "ChunkCacheSize": 1000,
    "EnableGarbageCollection": true
  },
  "Security": {
    "EnableWhitelist": false,
    "EnableAuthentication": true,
    "EnableEncryption": true,
    "MaxPacketSize": 2097152,
    "RateLimitPacketsPerSecond": 100,
    "EnableAntiCheat": true,
    "MaxPlayerSpeed": 10.0,
    "MaxFlySpeed": 20.0
  },
  "Logging": {
    "LogLevel": "Information",
    "EnableFileLogging": true,
    "LogDirectory": "logs",
    "EnableConsoleLogging": true,
    "MaxLogFileSizeMB": 10,
    "MaxLogFiles": 10,
    "EnablePerformanceLogging": false,
    "EnableNetworkLogging": false
  }
}
```

### World Configuration

**File**: `config/world.json`

The world configuration file contains world generation parameters:

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
  "MapControlProfileVersion": 1,
  "TerrainGeneration": {
    "SeaLevel": 62,
    "BedrockLevel": 5,
    "NoiseScale": 100.0,
    "NoiseAmplitude": 50.0,
    "Octaves": 4,
    "Persistence": 0.5,
    "Lacunarity": 2.0,
    "BiomeScale": 0.005,
    "TemperatureScale": 0.003,
    "HumidityScale": 0.004,
    "MountainThreshold": 0.6,
    "MountainMaxHeight": 200,
    "PlainBaseHeight": 64
  }
}
```

### Client Configuration

**File**: `config/client_config.json`

The client configuration file contains network, graphics, audio, controls, UI, gameplay, world, performance, and debug settings:

```json
{
  "client": {
    "network": {
      "connectionTimeoutMs": 10000,
      "reconnectAttempts": 3,
      "reconnectDelayMs": 5000,
      "maxPacketSize": 1048576,
      "compressionEnabled": true,
      "compressionThreshold": 1024
    },
    "graphics": {
      "renderDistance": 8,
      "maxRenderDistance": 16,
      "fov": 75,
      "maxFov": 110,
      "brightness": 0.7,
      "gamma": 1.0,
      "vsyncEnabled": true,
      "maxFps": 60,
      "antiAliasing": 2,
      "anisotropicFiltering": true,
      "textureQuality": "high",
      "shadowQuality": "medium",
      "particleQuality": "high",
      "waterQuality": "high"
    }
  }
}
```

### Game Data Configuration

**File**: `config/blocks.json`

The blocks configuration file contains block definitions:

```json
{
  "blocks": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "solid": false,
      "transparent": true,
      "liquid": false,
      "hardness": 0.0,
      "resistance": 0.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 1.5,
      "resistance": 6.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 1,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    }
  ]
}
```

**File**: `config/items.json`

The items configuration file contains item definitions:

```json
{
  "items": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "maxStackSize": 1,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 256,
      "name": "iron_shovel",
      "displayName": "Iron Shovel",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "shovel",
      "toolPower": 2
    }
  ]
}
```

**File**: `config/recipes.json`

The recipes configuration file contains crafting recipes:

```json
{
  "recipes": [
    {
      "result": {
        "itemId": 1,
        "count": 4
      },
      "pattern": [
        "S",
        "S"
      ],
      "ingredients": {
        "S": {
          "itemId": 3,
          "metadata": null
        }
      }
    }
  ]
}
```

**File**: `config/biomes.json`

The biomes configuration file contains biome definitions:

```json
{
  "biomes": [
    {
      "id": 0,
      "name": "plains",
      "displayName": "Plains",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#91BD59",
      "foliageColor": "#77AB2F"
    },
    {
      "id": 1,
      "name": "forest",
      "displayName": "Forest",
      "temperature": 0.7,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#5E9F3E",
      "foliageColor": "#4A7A2E"
    }
  ]
}
```

## Configuration Model

### ServerConfig

The `ServerConfig` class provides the server configuration model:

```csharp
public class ServerConfig
{
    public NetworkSettings Network { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public WorldSettings World { get; set; } = new();
    public GameplaySettings Gameplay { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();

    public static ServerConfig LoadFromFile(string configPath = "server-config.json")
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load server config: {ex.Message}");
        }

        var defaultConfig = new ServerConfig();
        defaultConfig.SaveToFile(configPath);
        return defaultConfig;
    }

    public void SaveToFile(string configPath = "server-config.json")
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, json);
            Console.WriteLine($"Server configuration saved to {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save server config: {ex.Message}");
        }
    }
}
```

### NetworkSettings

```csharp
public class NetworkSettings
{
    public int Port { get; set; } = 9000;
    public string BindAddress { get; set; } = "0.0.0.0";
    public int MaxConnections { get; set; } = 100;
    public int ConnectionTimeoutMinutes { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public bool EnableEncryption { get; set; } = false;
}
```

### DatabaseSettings

```csharp
public class DatabaseSettings
{
    public string DatabaseFile { get; set; } = "minecraft_game.db";
    public bool EnableWALMode { get; set; } = true;
    public int ConnectionPoolSize { get; set; } = 10;
    public bool AutoBackup { get; set; } = true;
    public int BackupIntervalHours { get; set; } = 24;
}
```

### WorldSettings

```csharp
public class WorldSettings
{
    public string DefaultWorldName { get; set; } = "default";
    public long WorldSeed { get; set; } = 12345;
    public string WorldConfigPath { get; set; } = "config/world.json";
    public int ChunkLoadRadius { get; set; } = 8;
    public int ChunkUnloadTimeoutMinutes { get; set; } = 30;
    public long InitialWorldTime { get; set; } = 0;
    public long InitialDayTime { get; set; } = 1000;
    public bool EnableDayNightCycle { get; set; } = false;
    public int DayNightCycleSecondsPerDay { get; set; } = 1200;
    public bool EnableWeatherCycle { get; set; } = true;
    public int WeatherTickIntervalSeconds { get; set; } = 30;
    public int ClearWeatherDurationSeconds { get; set; } = 360;
    public int RainWeatherDurationSeconds { get; set; } = 180;
    public int StormWeatherDurationSeconds { get; set; } = 120;
    public int SnowWeatherDurationSeconds { get; set; } = 240;
    public double WeatherStormProbability { get; set; } = 0.1;
    public double WeatherSnowProbability { get; set; } = 0.05;
    public bool EnableTerrainGeneration { get; set; } = true;
    public bool EnableOreGeneration { get; set; } = true;
    public bool EnableVegetationGeneration { get; set; } = true;
    public bool EnableCaves { get; set; } = true;
    public bool EnableRivers { get; set; } = true;
    public bool EnableLakes { get; set; } = true;
    public int MaxWorldHeight { get; set; } = 256;
    public int MinWorldHeight { get; set; } = -64;
}
```

### GameplaySettings

```csharp
public class GameplaySettings
{
    public int MaxPlayersPerWorld { get; set; } = 20;
    public bool EnablePvP { get; set; } = true;
    public bool EnableFlying { get; set; } = true;
    public double MovementValidationTolerance { get; set; } = 10.0;
    public int MaxBlockInteractionDistance { get; set; } = 5;
    public bool EnableInventorySystem { get; set; } = true;
    public int MaxInventorySlots { get; set; } = 36;
    public bool EnableChatSystem { get; set; } = true;
}
```

### SecuritySettings

```csharp
public class SecuritySettings
{
    public bool RequireAuthentication { get; set; } = true;
    public int MinPasswordLength { get; set; } = 6;
    public int SessionTimeoutHours { get; set; } = 24;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxMessagesPerSecond { get; set; } = 10;
    public bool EnableAntiCheat { get; set; } = true;
}
```

### PerformanceSettings

```csharp
public class PerformanceSettings
{
    public int MaintenanceIntervalMinutes { get; set; } = 5;
    public int ChunkSaveIntervalMinutes { get; set; } = 10;
    public int PlayerStateSaveIntervalMinutes { get; set; } = 2;
    public bool EnableGarbageCollection { get; set; } = true;
    public int MaxConcurrentChunkGenerations { get; set; } = 4;
    public bool EnableMetrics { get; set; } = true;
}
```

## Data-driven Configuration

### Game Data Loading

Game data is loaded from JSON configuration files:

```csharp
public class BlockData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Solid { get; set; }
    public bool Transparent { get; set; }
    public bool Liquid { get; set; }
    public double Hardness { get; set; }
    public double Resistance { get; set; }
    public int LightLevel { get; set; }
    public string Tool { get; set; }
    public List<BlockDrop> Drops { get; set; }
}

public class BlockDrop
{
    public int ItemId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
}

public class BlockRegistry
{
    private readonly Dictionary<int, BlockData> _blocks = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var blockData = JsonSerializer.Deserialize<List<BlockData>>(json);
        foreach (var block in blockData)
        {
            _blocks[block.Id] = block;
        }
    }

    public BlockData GetBlock(int id)
    {
        return _blocks.TryGetValue(id, out var block) ? block : null;
    }
}
```

### Hot-reload Support

Configuration can be hot-reloaded without restart:

```csharp
public class ConfigHotReload
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

    public void WatchConfig(string configPath, Action<string> onReload)
    {
        var directory = Path.GetDirectoryName(configPath);
        var fileName = Path.GetFileName(configPath);

        var watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (sender, e) =>
        {
            onReload(configPath);
        };

        watcher.EnableRaisingEvents = true;
        _watchers[configPath] = watcher;
    }
}
```

## Environment Variables

### Environment Variable Support

Configuration values can be overridden by environment variables:

```csharp
public static string GetEnvironmentValue(string key, string defaultValue = null)
{
    var value = Environment.GetEnvironmentVariable(key);
    return value ?? defaultValue;
}

public static int GetEnvironmentInt(string key, int defaultValue)
{
    var value = Environment.GetEnvironmentVariable(key);
    return int.TryParse(value, out var result) ? result : defaultValue;
}

public static bool GetEnvironmentBool(string key, bool defaultValue)
{
    var value = Environment.GetEnvironmentVariable(key);
    return bool.TryParse(value, out var result) ? result : defaultValue;
}
```

### Environment Variable Naming

Environment variables use uppercase names with underscores:

- `SERVER_PORT`: Server port
- `SERVER_HOST`: Server host
- `DATABASE_CONNECTION_STRING`: Database connection string
- `WORLD_SEED`: World seed
- `MAX_PLAYERS`: Maximum players

## Configuration Validation

### Validation Overview

Configuration values are validated on load:

```csharp
public class ConfigValidator
{
    public static ValidationResult Validate(ServerConfig config)
    {
        var issues = new List<string>();

        // Validate network settings
        if (config.Network.Port < 1 || config.Network.Port > 65535)
        {
            issues.Add($"Invalid port: {config.Network.Port}");
        }

        if (config.Network.MaxConnections < 1)
        {
            issues.Add($"Invalid max connections: {config.Network.MaxConnections}");
        }

        // Validate world settings
        if (config.World.MaxWorldHeight < 1)
        {
            issues.Add($"Invalid max world height: {config.World.MaxWorldHeight}");
        }

        if (config.World.MinWorldHeight >= config.World.MaxWorldHeight)
        {
            issues.Add($"Min world height must be less than max world height");
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; }
}
```

## Configuration Best Practices

### File Organization

1. **Separate Config Files**: Separate config files for different concerns
2. **Default Configs**: Provide default config files
3. **Config Validation**: Validate config on load
4. **Hot-reload**: Support hot-reload for non-critical settings
5. **Environment Variables**: Support environment variable overrides

### Data-driven Approach

1. **JSON Format**: Use JSON for all game data
2. **Validation**: Validate data on load
3. **Extensibility**: Easy to add new data
4. **Versioning**: Support data versioning
5. **Migration**: Support data migration

### Security

1. **No Secrets**: Don't store secrets in config files
2. **Environment Variables**: Use environment variables for secrets
3. **Validation**: Validate all input
4. **Sanitization**: Sanitize file paths
5. **Permissions**: Set appropriate file permissions

## References

- [`ServerConfig.cs`](../GameServer/ServerConfig.cs)
- [`config/server.json`](../config/server.json)
- [`config/world.json`](../config/world.json)
- [`config/client_config.json`](../config/client_config.json)
- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)
- [`config/biomes.json`](../config/biomes.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

## Overview

This document describes the configuration system used in the Minecraft-style game server. The configuration system provides JSON-based configuration with hot-reload support, data-driven approach, and comprehensive validation.

## Architecture

### Core Components

- **ServerConfig.cs**: Server configuration model
- **DataDrivenConfigManager.cs**: Data-driven configuration manager
- **Config Files**: JSON configuration files in `config/` directory

### Configuration Features

1. **JSON-based Configuration**: All configuration is stored in JSON format
2. **Hot-reload Support**: Configuration can be reloaded without restart
3. **Data-driven Approach**: Game data is driven by JSON configuration
4. **Validation**: Comprehensive validation of configuration values
5. **Environment Variables**: Support for environment variable overrides

## Configuration Files

### Server Configuration

**File**: `config/server.json`

The server configuration file contains network, database, performance, security, and logging settings:

```json
{
  "Network": {
    "Host": "0.0.0.0",
    "Port": 25565,
    "MaxPlayers": 20,
    "MaxConnectionsPerIP": 3,
    "ConnectionTimeoutSeconds": 30,
    "KeepAliveIntervalSeconds": 5,
    "PacketCompressionThreshold": 256
  },
  "Database": {
    "Provider": "sqlite",
    "ConnectionString": "Data Source=gameserver.db",
    "EnableAutoMigration": true,
    "CommandTimeoutSeconds": 30,
    "MaxPoolSize": 100
  },
  "Performance": {
    "TickRate": 20,
    "ChunkLoadThreads": 4,
    "MaxChunkLoadsPerTick": 10,
    "ChunkUnloadDelay": 30,
    "EntityUpdateDistance": 128,
    "EnableAsyncChunkGeneration": true,
    "ChunkCacheSize": 1000,
    "EnableGarbageCollection": true
  },
  "Security": {
    "EnableWhitelist": false,
    "EnableAuthentication": true,
    "EnableEncryption": true,
    "MaxPacketSize": 2097152,
    "RateLimitPacketsPerSecond": 100,
    "EnableAntiCheat": true,
    "MaxPlayerSpeed": 10.0,
    "MaxFlySpeed": 20.0
  },
  "Logging": {
    "LogLevel": "Information",
    "EnableFileLogging": true,
    "LogDirectory": "logs",
    "EnableConsoleLogging": true,
    "MaxLogFileSizeMB": 10,
    "MaxLogFiles": 10,
    "EnablePerformanceLogging": false,
    "EnableNetworkLogging": false
  }
}
```

### World Configuration

**File**: `config/world.json`

The world configuration file contains world generation parameters:

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
  "MapControlProfileVersion": 1,
  "TerrainGeneration": {
    "SeaLevel": 62,
    "BedrockLevel": 5,
    "NoiseScale": 100.0,
    "NoiseAmplitude": 50.0,
    "Octaves": 4,
    "Persistence": 0.5,
    "Lacunarity": 2.0,
    "BiomeScale": 0.005,
    "TemperatureScale": 0.003,
    "HumidityScale": 0.004,
    "MountainThreshold": 0.6,
    "MountainMaxHeight": 200,
    "PlainBaseHeight": 64
  }
}
```

### Client Configuration

**File**: `config/client_config.json`

The client configuration file contains network, graphics, audio, controls, UI, gameplay, world, performance, and debug settings:

```json
{
  "client": {
    "network": {
      "connectionTimeoutMs": 10000,
      "reconnectAttempts": 3,
      "reconnectDelayMs": 5000,
      "maxPacketSize": 1048576,
      "compressionEnabled": true,
      "compressionThreshold": 1024
    },
    "graphics": {
      "renderDistance": 8,
      "maxRenderDistance": 16,
      "fov": 75,
      "maxFov": 110,
      "brightness": 0.7,
      "gamma": 1.0,
      "vsyncEnabled": true,
      "maxFps": 60,
      "antiAliasing": 2,
      "anisotropicFiltering": true,
      "textureQuality": "high",
      "shadowQuality": "medium",
      "particleQuality": "high",
      "waterQuality": "high"
    }
  }
}
```

### Game Data Configuration

**File**: `config/blocks.json`

The blocks configuration file contains block definitions:

```json
{
  "blocks": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "solid": false,
      "transparent": true,
      "liquid": false,
      "hardness": 0.0,
      "resistance": 0.0,
      "lightLevel": 0,
      "tool": null,
      "drops": []
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "solid": true,
      "transparent": false,
      "liquid": false,
      "hardness": 1.5,
      "resistance": 6.0,
      "lightLevel": 0,
      "tool": "pickaxe",
      "drops": [
        {
          "itemId": 1,
          "minCount": 1,
          "maxCount": 1
        }
      ]
    }
  ]
}
```

**File**: `config/items.json`

The items configuration file contains item definitions:

```json
{
  "items": [
    {
      "id": 0,
      "name": "air",
      "displayName": "Air",
      "maxStackSize": 1,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 1,
      "name": "stone",
      "displayName": "Stone",
      "maxStackSize": 64,
      "maxDurability": 0,
      "toolType": null,
      "toolPower": 0
    },
    {
      "id": 256,
      "name": "iron_shovel",
      "displayName": "Iron Shovel",
      "maxStackSize": 1,
      "maxDurability": 250,
      "toolType": "shovel",
      "toolPower": 2
    }
  ]
}
```

**File**: `config/recipes.json`

The recipes configuration file contains crafting recipes:

```json
{
  "recipes": [
    {
      "result": {
        "itemId": 1,
        "count": 4
      },
      "pattern": [
        "S",
        "S"
      ],
      "ingredients": {
        "S": {
          "itemId": 3,
          "metadata": null
        }
      }
    }
  ]
}
```

**File**: `config/biomes.json`

The biomes configuration file contains biome definitions:

```json
{
  "biomes": [
    {
      "id": 0,
      "name": "plains",
      "displayName": "Plains",
      "temperature": 0.8,
      "humidity": 0.4,
      "baseHeight": 64,
      "heightVariation": 0.1,
      "waterColor": "#3F76E4",
      "grassColor": "#91BD59",
      "foliageColor": "#77AB2F"
    },
    {
      "id": 1,
      "name": "forest",
      "displayName": "Forest",
      "temperature": 0.7,
      "humidity": 0.8,
      "baseHeight": 68,
      "heightVariation": 0.2,
      "waterColor": "#3F76E4",
      "grassColor": "#5E9F3E",
      "foliageColor": "#4A7A2E"
    }
  ]
}
```

## Configuration Model

### ServerConfig

The `ServerConfig` class provides the server configuration model:

```csharp
public class ServerConfig
{
    public NetworkSettings Network { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public WorldSettings World { get; set; } = new();
    public GameplaySettings Gameplay { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();

    public static ServerConfig LoadFromFile(string configPath = "server-config.json")
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load server config: {ex.Message}");
        }

        var defaultConfig = new ServerConfig();
        defaultConfig.SaveToFile(configPath);
        return defaultConfig;
    }

    public void SaveToFile(string configPath = "server-config.json")
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, json);
            Console.WriteLine($"Server configuration saved to {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save server config: {ex.Message}");
        }
    }
}
```

### NetworkSettings

```csharp
public class NetworkSettings
{
    public int Port { get; set; } = 9000;
    public string BindAddress { get; set; } = "0.0.0.0";
    public int MaxConnections { get; set; } = 100;
    public int ConnectionTimeoutMinutes { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public bool EnableEncryption { get; set; } = false;
}
```

### DatabaseSettings

```csharp
public class DatabaseSettings
{
    public string DatabaseFile { get; set; } = "minecraft_game.db";
    public bool EnableWALMode { get; set; } = true;
    public int ConnectionPoolSize { get; set; } = 10;
    public bool AutoBackup { get; set; } = true;
    public int BackupIntervalHours { get; set; } = 24;
}
```

### WorldSettings

```csharp
public class WorldSettings
{
    public string DefaultWorldName { get; set; } = "default";
    public long WorldSeed { get; set; } = 12345;
    public string WorldConfigPath { get; set; } = "config/world.json";
    public int ChunkLoadRadius { get; set; } = 8;
    public int ChunkUnloadTimeoutMinutes { get; set; } = 30;
    public long InitialWorldTime { get; set; } = 0;
    public long InitialDayTime { get; set; } = 1000;
    public bool EnableDayNightCycle { get; set; } = false;
    public int DayNightCycleSecondsPerDay { get; set; } = 1200;
    public bool EnableWeatherCycle { get; set; } = true;
    public int WeatherTickIntervalSeconds { get; set; } = 30;
    public int ClearWeatherDurationSeconds { get; set; } = 360;
    public int RainWeatherDurationSeconds { get; set; } = 180;
    public int StormWeatherDurationSeconds { get; set; } = 120;
    public int SnowWeatherDurationSeconds { get; set; } = 240;
    public double WeatherStormProbability { get; set; } = 0.1;
    public double WeatherSnowProbability { get; set; } = 0.05;
    public bool EnableTerrainGeneration { get; set; } = true;
    public bool EnableOreGeneration { get; set; } = true;
    public bool EnableVegetationGeneration { get; set; } = true;
    public bool EnableCaves { get; set; } = true;
    public bool EnableRivers { get; set; } = true;
    public bool EnableLakes { get; set; } = true;
    public int MaxWorldHeight { get; set; } = 256;
    public int MinWorldHeight { get; set; } = -64;
}
```

### GameplaySettings

```csharp
public class GameplaySettings
{
    public int MaxPlayersPerWorld { get; set; } = 20;
    public bool EnablePvP { get; set; } = true;
    public bool EnableFlying { get; set; } = true;
    public double MovementValidationTolerance { get; set; } = 10.0;
    public int MaxBlockInteractionDistance { get; set; } = 5;
    public bool EnableInventorySystem { get; set; } = true;
    public int MaxInventorySlots { get; set; } = 36;
    public bool EnableChatSystem { get; set; } = true;
}
```

### SecuritySettings

```csharp
public class SecuritySettings
{
    public bool RequireAuthentication { get; set; } = true;
    public int MinPasswordLength { get; set; } = 6;
    public int SessionTimeoutHours { get; set; } = 24;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxMessagesPerSecond { get; set; } = 10;
    public bool EnableAntiCheat { get; set; } = true;
}
```

### PerformanceSettings

```csharp
public class PerformanceSettings
{
    public int MaintenanceIntervalMinutes { get; set; } = 5;
    public int ChunkSaveIntervalMinutes { get; set; } = 10;
    public int PlayerStateSaveIntervalMinutes { get; set; } = 2;
    public bool EnableGarbageCollection { get; set; } = true;
    public int MaxConcurrentChunkGenerations { get; set; } = 4;
    public bool EnableMetrics { get; set; } = true;
}
```

## Data-driven Configuration

### Game Data Loading

Game data is loaded from JSON configuration files:

```csharp
public class BlockData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool Solid { get; set; }
    public bool Transparent { get; set; }
    public bool Liquid { get; set; }
    public double Hardness { get; set; }
    public double Resistance { get; set; }
    public int LightLevel { get; set; }
    public string Tool { get; set; }
    public List<BlockDrop> Drops { get; set; }
}

public class BlockDrop
{
    public int ItemId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
}

public class BlockRegistry
{
    private readonly Dictionary<int, BlockData> _blocks = new();

    public void LoadFromJson(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var blockData = JsonSerializer.Deserialize<List<BlockData>>(json);
        foreach (var block in blockData)
        {
            _blocks[block.Id] = block;
        }
    }

    public BlockData GetBlock(int id)
    {
        return _blocks.TryGetValue(id, out var block) ? block : null;
    }
}
```

### Hot-reload Support

Configuration can be hot-reloaded without restart:

```csharp
public class ConfigHotReload
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

    public void WatchConfig(string configPath, Action<string> onReload)
    {
        var directory = Path.GetDirectoryName(configPath);
        var fileName = Path.GetFileName(configPath);

        var watcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (sender, e) =>
        {
            onReload(configPath);
        };

        watcher.EnableRaisingEvents = true;
        _watchers[configPath] = watcher;
    }
}
```

## Environment Variables

### Environment Variable Support

Configuration values can be overridden by environment variables:

```csharp
public static string GetEnvironmentValue(string key, string defaultValue = null)
{
    var value = Environment.GetEnvironmentVariable(key);
    return value ?? defaultValue;
}

public static int GetEnvironmentInt(string key, int defaultValue)
{
    var value = Environment.GetEnvironmentVariable(key);
    return int.TryParse(value, out var result) ? result : defaultValue;
}

public static bool GetEnvironmentBool(string key, bool defaultValue)
{
    var value = Environment.GetEnvironmentVariable(key);
    return bool.TryParse(value, out var result) ? result : defaultValue;
}
```

### Environment Variable Naming

Environment variables use uppercase names with underscores:

- `SERVER_PORT`: Server port
- `SERVER_HOST`: Server host
- `DATABASE_CONNECTION_STRING`: Database connection string
- `WORLD_SEED`: World seed
- `MAX_PLAYERS`: Maximum players

## Configuration Validation

### Validation Overview

Configuration values are validated on load:

```csharp
public class ConfigValidator
{
    public static ValidationResult Validate(ServerConfig config)
    {
        var issues = new List<string>();

        // Validate network settings
        if (config.Network.Port < 1 || config.Network.Port > 65535)
        {
            issues.Add($"Invalid port: {config.Network.Port}");
        }

        if (config.Network.MaxConnections < 1)
        {
            issues.Add($"Invalid max connections: {config.Network.MaxConnections}");
        }

        // Validate world settings
        if (config.World.MaxWorldHeight < 1)
        {
            issues.Add($"Invalid max world height: {config.World.MaxWorldHeight}");
        }

        if (config.World.MinWorldHeight >= config.World.MaxWorldHeight)
        {
            issues.Add($"Min world height must be less than max world height");
        }

        return new ValidationResult
        {
            IsValid = !issues.Any(),
            Issues = issues
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; }
}
```

## Configuration Best Practices

### File Organization

1. **Separate Config Files**: Separate config files for different concerns
2. **Default Configs**: Provide default config files
3. **Config Validation**: Validate config on load
4. **Hot-reload**: Support hot-reload for non-critical settings
5. **Environment Variables**: Support environment variable overrides

### Data-driven Approach

1. **JSON Format**: Use JSON for all game data
2. **Validation**: Validate data on load
3. **Extensibility**: Easy to add new data
4. **Versioning**: Support data versioning
5. **Migration**: Support data migration

### Security

1. **No Secrets**: Don't store secrets in config files
2. **Environment Variables**: Use environment variables for secrets
3. **Validation**: Validate all input
4. **Sanitization**: Sanitize file paths
5. **Permissions**: Set appropriate file permissions

## References

- [`ServerConfig.cs`](../GameServer/ServerConfig.cs)
- [`config/server.json`](../config/server.json)
- [`config/world.json`](../config/world.json)
- [`config/client_config.json`](../config/client_config.json)
- [`config/blocks.json`](../config/blocks.json)
- [`config/items.json`](../config/items.json)
- [`config/recipes.json`](../config/recipes.json)
- [`config/biomes.json`](../config/biomes.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

