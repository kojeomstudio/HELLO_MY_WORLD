# World Map Control Architecture Improvements

## Current State Analysis

### Server Side (GameServer/World/WorldMapControlProfile.cs)
- Simple data structure with init-only properties
- Contains terrain generation parameters
- Used to synchronize world generation between server and client
- Limited to basic configuration values

### Client Side (Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs)
- Similar structure but with float types instead of double
- Constructor takes WorldConfig and applies clamping
- Used in WorldAreaManager for terrain generation
- Manages algorithm parameters for world generation

### Issues Identified
1. **Type Inconsistency**: Server uses `double`, client uses `float`
2. **Synchronization**: No mechanism to ensure server and client profiles match
3. **Missing Features**: No support for dynamic terrain feature toggles
4. **Limited Validation**: No validation of parameter ranges
5. **No Version Control**: No way to handle profile versioning

## Proposed Architecture Improvements

### 1. Unified WorldMapControlProfile System

#### Core Profile Class
```csharp
public sealed class WorldMapControlProfile
{
    // Version control
    public int Version { get; init; }
    public string ProfileHash { get; init; }
    
    // Basic terrain parameters
    public int ChunkSize { get; init; }
    public int RenderDistance { get; init; }
    public int SimulationDistance { get; init; }
    public int WorldHeight { get; init; }
    public int GlobalWaterLevel { get; init; }
    
    // Terrain generation toggles
    public bool EnableRivers { get; init; }
    public bool EnableLakes { get; init; }
    public bool EnableCaves { get; init; }
    public bool UseImprovedCaves { get; init; }
    public bool UseImprovedRivers { get; init; }
    public bool UseImprovedLakes { get; init; }
    
    // Cave generation parameters
    public CaveGenerationConfig CaveConfig { get; init; }
    
    // River generation parameters
    public RiverGenerationConfig RiverConfig { get; init; }
    
    // Lake generation parameters
    public LakeGenerationConfig LakeConfig { get; init; }
    
    // Hydrology parameters
    public HydrologyConfig HydrologyConfig { get; init; }
}
```

#### Configuration Sub-Classes
```csharp
public sealed class CaveGenerationConfig
{
    public double HorizontalFrequency { get; init; }
    public double VerticalFrequency { get; init; }
    public double Threshold { get; init; }
    public double LavaThreshold { get; init; }
    public double WaterThreshold { get; init; }
    public bool UseRegionalMainCaves { get; init; }
    public int RegionalMainCaveRegionSizeChunks { get; init; }
    // ... other cave parameters
}

public sealed class RiverGenerationConfig
{
    public double NoiseScale { get; init; }
    public int Depth { get; init; }
    public double CenterThreshold { get; init; }
    public double BankThreshold { get; init; }
    public double EdgeFeather { get; init; }
    // ... other river parameters
}

public sealed class LakeGenerationConfig
{
    public double RiverProximitySuppression { get; init; }
    public int BasinSmoothIterations { get; init; }
    public double InflowBlendWeight { get; init; }
    // ... other lake parameters
}

public sealed class HydrologyConfig
{
    public int SmoothIterations { get; init; }
    public double SmoothBlend { get; init; }
    public double ShorePush { get; init; }
    public double SlopePenalty { get; init; }
    public double FlowGain { get; init; }
    // ... other hydrology parameters
}
```

### 2. Profile Synchronization System

#### Server-Side Profile Manager
```csharp
public class WorldMapControlProfileManager
{
    private WorldMapControlProfile _currentProfile;
    private readonly Dictionary<string, WorldMapControlProfile> _profileHistory;
    
    public WorldMapControlProfile GetCurrentProfile() => _currentProfile;
    
    public void UpdateProfile(WorldMapControlProfile newProfile)
    {
        // Validate profile
        ValidateProfile(newProfile);
        
        // Store in history
        _profileHistory[newProfile.ProfileHash] = newProfile;
        
        // Update current
        _currentProfile = newProfile;
        
        // Notify clients
        BroadcastProfileUpdate(newProfile);
    }
    
    public byte[] SerializeProfile(WorldMapControlProfile profile)
    {
        // Use protobuf for efficient serialization
        return profile.ToByteArray();
    }
    
    private void ValidateProfile(WorldMapControlProfile profile)
    {
        // Validate parameter ranges
        // Check for required fields
        // Validate version compatibility
    }
}
```

#### Client-Side Profile Receiver
```csharp
public class WorldMapControlProfileReceiver : MonoBehaviour
{
    private WorldMapControlProfile _serverProfile;
    private WorldMapControlProfile _localProfile;
    
    public void OnProfileReceived(byte[] profileData)
    {
        var serverProfile = WorldMapControlProfile.Parser.ParseFrom(profileData);
        
        // Validate compatibility
        if (IsProfileCompatible(serverProfile))
        {
            _serverProfile = serverProfile;
            ApplyProfileToTerrainGenerator(serverProfile);
        }
        else
        {
            Debug.LogError("Received incompatible world profile from server");
            RequestFullWorldSync();
        }
    }
    
    private void ApplyProfileToTerrainGenerator(WorldMapControlProfile profile)
    {
        var terrainGenerator = FindObjectOfType<TerrainGenerator>();
        if (terrainGenerator != null)
        {
            terrainGenerator.ApplyProfile(profile);
        }
    }
}
```

### 3. Enhanced Terrain Generator Integration

#### Updated TerrainGenerator
```csharp
public class TerrainGenerator : MonoBehaviour
{
    private WorldMapControlProfile _currentProfile;
    
    public void ApplyProfile(WorldMapControlProfile profile)
    {
        _currentProfile = profile;
        
        // Update noise generators with new parameters
        UpdateNoiseGenerators(profile);
        
        // Clear caches to force regeneration with new parameters
        ClearCaches();
    }
    
    private void UpdateNoiseGenerators(WorldMapControlProfile profile)
    {
        // Update cave noise
        _caveNoise.SetFrequency(profile.CaveConfig.HorizontalFrequency);
        
        // Update river noise
        _riverNoise.SetFrequency(profile.RiverConfig.NoiseScale);
        
        // Update lake noise
        _lakeNoise.SetFrequency(profile.LakeConfig.SpawnWeightBias);
        
        // Apply hydrology parameters
        ApplyHydrologyParameters(profile.HydrologyConfig);
    }
    
    public int[,,] GenerateChunk(int chunkX, int chunkZ)
    {
        if (_currentProfile == null)
        {
            Debug.LogError("No world profile applied to terrain generator");
            return CreateEmptyChunk();
        }
        
        return GenerateChunkWithProfile(chunkX, chunkZ, _currentProfile);
    }
}
```

### 4. Profile Versioning and Migration

#### Profile Version Handler
```csharp
public static class ProfileVersionHandler
{
    private static readonly Dictionary<int, Func<WorldMapControlProfile, WorldMapControlProfile>> _migrations = new()
    {
        { 1, MigrateFromV1ToV2 },
        { 2, MigrateFromV2ToV3 },
        // Add more migrations as needed
    };
    
    public static WorldMapControlProfile MigrateProfile(WorldMapControlProfile oldProfile)
    {
        var currentProfile = oldProfile;
        
        for (int version = oldProfile.Version; version < CurrentProfileVersion; version++)
        {
            if (_migrations.TryGetValue(version + 1, out var migrateFunc))
            {
                currentProfile = migrateFunc(currentProfile);
            }
        }
        
        return currentProfile;
    }
    
    private static WorldMapControlProfile MigrateFromV1ToV2(WorldMapControlProfile v1Profile)
    {
        // Add new parameters introduced in v2
        return v1Profile with
        {
            Version = 2,
            CaveConfig = v1Profile.CaveConfig with
            {
                // New cave parameters
            }
        };
    }
}
```

### 5. Profile Validation System

#### Profile Validator
```csharp
public static class WorldMapControlProfileValidator
{
    public static ValidationResult ValidateProfile(WorldMapControlProfile profile)
    {
        var result = new ValidationResult();
        
        // Validate basic parameters
        if (profile.ChunkSize <= 0 || profile.ChunkSize > 64)
        {
            result.AddError("ChunkSize must be between 1 and 64");
        }
        
        if (profile.RenderDistance <= 0 || profile.RenderDistance > 32)
        {
            result.AddError("RenderDistance must be between 1 and 32");
        }
        
        // Validate cave parameters
        ValidateCaveConfig(profile.CaveConfig, result);
        
        // Validate river parameters
        ValidateRiverConfig(profile.RiverConfig, result);
        
        // Validate lake parameters
        ValidateLakeConfig(profile.LakeConfig, result);
        
        // Validate hydrology parameters
        ValidateHydrologyConfig(profile.HydrologyConfig, result);
        
        return result;
    }
    
    private static void ValidateCaveConfig(CaveGenerationConfig config, ValidationResult result)
    {
        if (config.HorizontalFrequency <= 0 || config.HorizontalFrequency > 0.1)
        {
            result.AddError("Cave HorizontalFrequency must be between 0 and 0.1");
        }
        
        if (config.VerticalFrequency <= 0 || config.VerticalFrequency > 0.1)
        {
            result.AddError("Cave VerticalFrequency must be between 0 and 0.1");
        }
        
        if (config.Threshold < 0 || config.Threshold > 1)
        {
            result.AddError("Cave Threshold must be between 0 and 1");
        }
    }
    
    // Similar validation methods for other config types
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}
```

### 6. Integration with Existing Systems

#### Server Integration
```csharp
public partial class WorldManager
{
    private WorldMapControlProfileManager _profileManager;
    
    public WorldManager(DatabaseHelper database, WorldSettings? worldSettings = null, 
        WorldGenerationConfig? generationConfig = null, int worldId = 1, 
        WorldSeedConfig? worldSeed = null)
    {
        // Existing initialization...
        
        // Initialize profile manager
        _profileManager = new WorldMapControlProfileManager();
        
        // Create initial profile from configuration
        var initialProfile = CreateProfileFromConfig(_worldGenConfig);
        _profileManager.UpdateProfile(initialProfile);
        
        // Expose profile through existing property
        _mapControlProfile = _profileManager.GetCurrentProfile();
    }
    
    private WorldMapControlProfile CreateProfileFromConfig(WorldGenerationConfig config)
    {
        return new WorldMapControlProfile
        {
            Version = CurrentProfileVersion,
            ProfileHash = GenerateProfileHash(config),
            ChunkSize = config.ChunkSize,
            RenderDistance = config.RenderDistance,
            SimulationDistance = config.SimulationDistance,
            WorldHeight = config.WorldHeight,
            GlobalWaterLevel = config.Water.GlobalWaterLevel,
            EnableRivers = config.Water.EnableRivers,
            EnableLakes = config.Water.EnableLakes,
            EnableCaves = config.Caves.EnableCaves,
            UseImprovedCaves = config.Caves.UseImprovedCaves,
            UseImprovedRivers = config.Water.UseImprovedRivers,
            UseImprovedLakes = config.Water.UseImprovedLakes,
            CaveConfig = new CaveGenerationConfig
            {
                HorizontalFrequency = config.Caves.HorizontalFrequency,
                VerticalFrequency = config.Caves.VerticalFrequency,
                Threshold = config.Caves.Threshold,
                LavaThreshold = config.Caves.LavaThreshold,
                WaterThreshold = config.Caves.WaterThreshold,
                UseRegionalMainCaves = config.Caves.UseRegionalMainCaves,
                RegionalMainCaveRegionSizeChunks = config.Caves.RegionalMainCaveRegionSizeChunks,
                // ... other cave parameters
            },
            RiverConfig = new RiverGenerationConfig
            {
                NoiseScale = config.Water.RiverNoiseScale,
                Depth = config.Water.RiverDepth,
                CenterThreshold = config.Water.RiverCenterThreshold,
                BankThreshold = config.Water.RiverBankThreshold,
                EdgeFeather = config.Water.RiverEdgeFeather,
                // ... other river parameters
            },
            LakeConfig = new LakeGenerationConfig
            {
                RiverProximitySuppression = config.Lakes.RiverProximitySuppression,
                BasinSmoothIterations = config.Lakes.LakeBasinSmoothIterations,
                InflowBlendWeight = config.Water.LakeInflowBlendWeight,
                // ... other lake parameters
            },
            HydrologyConfig = new HydrologyConfig
            {
                SmoothIterations = config.Water.HydrologySmoothIterations,
                SmoothBlend = config.Water.HydrologySmoothBlend,
                ShorePush = config.Water.HydrologyShorePush,
                SlopePenalty = config.Water.HydrologySlopePenalty,
                FlowGain = config.Water.HydrologyFlowGain,
                // ... other hydrology parameters
            }
        };
    }
    
    public void UpdateWorldProfile(WorldMapControlProfile newProfile)
    {
        _profileManager.UpdateProfile(newProfile);
        _mapControlProfile = newProfile;
        
        // Notify terrain generation pipeline of profile change
        OnProfileUpdated(newProfile);
    }
}
```

#### Client Integration
```csharp
public class WorldAreaManager : MonoBehaviour
{
    private WorldMapControlProfileReceiver _profileReceiver;
    
    public async void Init()
    {
        // Existing initialization...
        
        // Initialize profile receiver
        _profileReceiver = gameObject.AddComponent<WorldMapControlProfileReceiver>();
        
        // Request current profile from server
        await RequestWorldProfileFromServer();
    }
    
    private async Task RequestWorldProfileFromServer()
    {
        var gameClient = FindObjectOfType<MinecraftGameClient>();
        if (gameClient != null && gameClient.IsConnected)
        {
            // Send profile request to server
            gameClient.SendWorldProfileRequest();
        }
        else
        {
            // Use local configuration for offline mode
            var localProfile = CreateLocalProfile();
            _profileReceiver.OnProfileReceived(localProfile);
        }
    }
    
    private WorldMapControlProfile CreateLocalProfile()
    {
        var worldConfig = WorldConfigFile.Instance.GetConfig();
        return new WorldMapControlProfile
        {
            // Create profile from local configuration
            Version = CurrentProfileVersion,
            ProfileHash = GenerateLocalProfileHash(),
            // ... copy parameters from worldConfig
        };
    }
}
```

## Implementation Plan

### Phase 1: Core Profile System
1. Create unified WorldMapControlProfile class with sub-configurations
2. Implement profile serialization/deserialization
3. Create profile validation system
4. Add versioning support

### Phase 2: Server Integration
1. Update WorldManager to use profile system
2. Create profile manager for server side
3. Add profile update broadcasting
4. Integrate with existing terrain generation pipeline

### Phase 3: Client Integration
1. Update TerrainGenerator to use profile system
2. Create profile receiver for client side
3. Add profile request/response protocol
4. Update WorldAreaManager integration

### Phase 4: Synchronization & Validation
1. Implement profile synchronization protocol
2. Add compatibility checking
3. Create migration system for profile versions
4. Add validation for all profile parameters

### Phase 5: Testing & Optimization
1. Create comprehensive tests for profile system
2. Test server-client synchronization
3. Optimize profile serialization
4. Add error handling and recovery

## Benefits

1. **Consistency**: Ensures server and client use identical terrain generation parameters
2. **Flexibility**: Allows dynamic terrain feature toggles and parameter adjustments
3. **Maintainability**: Centralized configuration management
4. **Versioning**: Supports evolution of terrain generation algorithms
5. **Validation**: Prevents invalid configurations from causing issues
6. **Performance**: Efficient serialization and synchronization

## Migration Strategy

1. **Backward Compatibility**: Support existing configuration formats
2. **Gradual Migration**: Phase in new profile system alongside existing code
3. **Fallback Support**: Maintain fallback to local configuration if server sync fails
4. **Testing**: Comprehensive testing of migration scenarios
## Current State Analysis

### Server Side (GameServer/World/WorldMapControlProfile.cs)
- Simple data structure with init-only properties
- Contains terrain generation parameters
- Used to synchronize world generation between server and client
- Limited to basic configuration values

### Client Side (Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs)
- Similar structure but with float types instead of double
- Constructor takes WorldConfig and applies clamping
- Used in WorldAreaManager for terrain generation
- Manages algorithm parameters for world generation

### Issues Identified
1. **Type Inconsistency**: Server uses `double`, client uses `float`
2. **Synchronization**: No mechanism to ensure server and client profiles match
3. **Missing Features**: No support for dynamic terrain feature toggles
4. **Limited Validation**: No validation of parameter ranges
5. **No Version Control**: No way to handle profile versioning

## Proposed Architecture Improvements

### 1. Unified WorldMapControlProfile System

#### Core Profile Class
```csharp
public sealed class WorldMapControlProfile
{
    // Version control
    public int Version { get; init; }
    public string ProfileHash { get; init; }
    
    // Basic terrain parameters
    public int ChunkSize { get; init; }
    public int RenderDistance { get; init; }
    public int SimulationDistance { get; init; }
    public int WorldHeight { get; init; }
    public int GlobalWaterLevel { get; init; }
    
    // Terrain generation toggles
    public bool EnableRivers { get; init; }
    public bool EnableLakes { get; init; }
    public bool EnableCaves { get; init; }
    public bool UseImprovedCaves { get; init; }
    public bool UseImprovedRivers { get; init; }
    public bool UseImprovedLakes { get; init; }
    
    // Cave generation parameters
    public CaveGenerationConfig CaveConfig { get; init; }
    
    // River generation parameters
    public RiverGenerationConfig RiverConfig { get; init; }
    
    // Lake generation parameters
    public LakeGenerationConfig LakeConfig { get; init; }
    
    // Hydrology parameters
    public HydrologyConfig HydrologyConfig { get; init; }
}
```

#### Configuration Sub-Classes
```csharp
public sealed class CaveGenerationConfig
{
    public double HorizontalFrequency { get; init; }
    public double VerticalFrequency { get; init; }
    public double Threshold { get; init; }
    public double LavaThreshold { get; init; }
    public double WaterThreshold { get; init; }
    public bool UseRegionalMainCaves { get; init; }
    public int RegionalMainCaveRegionSizeChunks { get; init; }
    // ... other cave parameters
}

public sealed class RiverGenerationConfig
{
    public double NoiseScale { get; init; }
    public int Depth { get; init; }
    public double CenterThreshold { get; init; }
    public double BankThreshold { get; init; }
    public double EdgeFeather { get; init; }
    // ... other river parameters
}

public sealed class LakeGenerationConfig
{
    public double RiverProximitySuppression { get; init; }
    public int BasinSmoothIterations { get; init; }
    public double InflowBlendWeight { get; init; }
    // ... other lake parameters
}

public sealed class HydrologyConfig
{
    public int SmoothIterations { get; init; }
    public double SmoothBlend { get; init; }
    public double ShorePush { get; init; }
    public double SlopePenalty { get; init; }
    public double FlowGain { get; init; }
    // ... other hydrology parameters
}
```

### 2. Profile Synchronization System

#### Server-Side Profile Manager
```csharp
public class WorldMapControlProfileManager
{
    private WorldMapControlProfile _currentProfile;
    private readonly Dictionary<string, WorldMapControlProfile> _profileHistory;
    
    public WorldMapControlProfile GetCurrentProfile() => _currentProfile;
    
    public void UpdateProfile(WorldMapControlProfile newProfile)
    {
        // Validate profile
        ValidateProfile(newProfile);
        
        // Store in history
        _profileHistory[newProfile.ProfileHash] = newProfile;
        
        // Update current
        _currentProfile = newProfile;
        
        // Notify clients
        BroadcastProfileUpdate(newProfile);
    }
    
    public byte[] SerializeProfile(WorldMapControlProfile profile)
    {
        // Use protobuf for efficient serialization
        return profile.ToByteArray();
    }
    
    private void ValidateProfile(WorldMapControlProfile profile)
    {
        // Validate parameter ranges
        // Check for required fields
        // Validate version compatibility
    }
}
```

#### Client-Side Profile Receiver
```csharp
public class WorldMapControlProfileReceiver : MonoBehaviour
{
    private WorldMapControlProfile _serverProfile;
    private WorldMapControlProfile _localProfile;
    
    public void OnProfileReceived(byte[] profileData)
    {
        var serverProfile = WorldMapControlProfile.Parser.ParseFrom(profileData);
        
        // Validate compatibility
        if (IsProfileCompatible(serverProfile))
        {
            _serverProfile = serverProfile;
            ApplyProfileToTerrainGenerator(serverProfile);
        }
        else
        {
            Debug.LogError("Received incompatible world profile from server");
            RequestFullWorldSync();
        }
    }
    
    private void ApplyProfileToTerrainGenerator(WorldMapControlProfile profile)
    {
        var terrainGenerator = FindObjectOfType<TerrainGenerator>();
        if (terrainGenerator != null)
        {
            terrainGenerator.ApplyProfile(profile);
        }
    }
}
```

### 3. Enhanced Terrain Generator Integration

#### Updated TerrainGenerator
```csharp
public class TerrainGenerator : MonoBehaviour
{
    private WorldMapControlProfile _currentProfile;
    
    public void ApplyProfile(WorldMapControlProfile profile)
    {
        _currentProfile = profile;
        
        // Update noise generators with new parameters
        UpdateNoiseGenerators(profile);
        
        // Clear caches to force regeneration with new parameters
        ClearCaches();
    }
    
    private void UpdateNoiseGenerators(WorldMapControlProfile profile)
    {
        // Update cave noise
        _caveNoise.SetFrequency(profile.CaveConfig.HorizontalFrequency);
        
        // Update river noise
        _riverNoise.SetFrequency(profile.RiverConfig.NoiseScale);
        
        // Update lake noise
        _lakeNoise.SetFrequency(profile.LakeConfig.SpawnWeightBias);
        
        // Apply hydrology parameters
        ApplyHydrologyParameters(profile.HydrologyConfig);
    }
    
    public int[,,] GenerateChunk(int chunkX, int chunkZ)
    {
        if (_currentProfile == null)
        {
            Debug.LogError("No world profile applied to terrain generator");
            return CreateEmptyChunk();
        }
        
        return GenerateChunkWithProfile(chunkX, chunkZ, _currentProfile);
    }
}
```

### 4. Profile Versioning and Migration

#### Profile Version Handler
```csharp
public static class ProfileVersionHandler
{
    private static readonly Dictionary<int, Func<WorldMapControlProfile, WorldMapControlProfile>> _migrations = new()
    {
        { 1, MigrateFromV1ToV2 },
        { 2, MigrateFromV2ToV3 },
        // Add more migrations as needed
    };
    
    public static WorldMapControlProfile MigrateProfile(WorldMapControlProfile oldProfile)
    {
        var currentProfile = oldProfile;
        
        for (int version = oldProfile.Version; version < CurrentProfileVersion; version++)
        {
            if (_migrations.TryGetValue(version + 1, out var migrateFunc))
            {
                currentProfile = migrateFunc(currentProfile);
            }
        }
        
        return currentProfile;
    }
    
    private static WorldMapControlProfile MigrateFromV1ToV2(WorldMapControlProfile v1Profile)
    {
        // Add new parameters introduced in v2
        return v1Profile with
        {
            Version = 2,
            CaveConfig = v1Profile.CaveConfig with
            {
                // New cave parameters
            }
        };
    }
}
```

### 5. Profile Validation System

#### Profile Validator
```csharp
public static class WorldMapControlProfileValidator
{
    public static ValidationResult ValidateProfile(WorldMapControlProfile profile)
    {
        var result = new ValidationResult();
        
        // Validate basic parameters
        if (profile.ChunkSize <= 0 || profile.ChunkSize > 64)
        {
            result.AddError("ChunkSize must be between 1 and 64");
        }
        
        if (profile.RenderDistance <= 0 || profile.RenderDistance > 32)
        {
            result.AddError("RenderDistance must be between 1 and 32");
        }
        
        // Validate cave parameters
        ValidateCaveConfig(profile.CaveConfig, result);
        
        // Validate river parameters
        ValidateRiverConfig(profile.RiverConfig, result);
        
        // Validate lake parameters
        ValidateLakeConfig(profile.LakeConfig, result);
        
        // Validate hydrology parameters
        ValidateHydrologyConfig(profile.HydrologyConfig, result);
        
        return result;
    }
    
    private static void ValidateCaveConfig(CaveGenerationConfig config, ValidationResult result)
    {
        if (config.HorizontalFrequency <= 0 || config.HorizontalFrequency > 0.1)
        {
            result.AddError("Cave HorizontalFrequency must be between 0 and 0.1");
        }
        
        if (config.VerticalFrequency <= 0 || config.VerticalFrequency > 0.1)
        {
            result.AddError("Cave VerticalFrequency must be between 0 and 0.1");
        }
        
        if (config.Threshold < 0 || config.Threshold > 1)
        {
            result.AddError("Cave Threshold must be between 0 and 1");
        }
    }
    
    // Similar validation methods for other config types
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}
```

### 6. Integration with Existing Systems

#### Server Integration
```csharp
public partial class WorldManager
{
    private WorldMapControlProfileManager _profileManager;
    
    public WorldManager(DatabaseHelper database, WorldSettings? worldSettings = null, 
        WorldGenerationConfig? generationConfig = null, int worldId = 1, 
        WorldSeedConfig? worldSeed = null)
    {
        // Existing initialization...
        
        // Initialize profile manager
        _profileManager = new WorldMapControlProfileManager();
        
        // Create initial profile from configuration
        var initialProfile = CreateProfileFromConfig(_worldGenConfig);
        _profileManager.UpdateProfile(initialProfile);
        
        // Expose profile through existing property
        _mapControlProfile = _profileManager.GetCurrentProfile();
    }
    
    private WorldMapControlProfile CreateProfileFromConfig(WorldGenerationConfig config)
    {
        return new WorldMapControlProfile
        {
            Version = CurrentProfileVersion,
            ProfileHash = GenerateProfileHash(config),
            ChunkSize = config.ChunkSize,
            RenderDistance = config.RenderDistance,
            SimulationDistance = config.SimulationDistance,
            WorldHeight = config.WorldHeight,
            GlobalWaterLevel = config.Water.GlobalWaterLevel,
            EnableRivers = config.Water.EnableRivers,
            EnableLakes = config.Water.EnableLakes,
            EnableCaves = config.Caves.EnableCaves,
            UseImprovedCaves = config.Caves.UseImprovedCaves,
            UseImprovedRivers = config.Water.UseImprovedRivers,
            UseImprovedLakes = config.Water.UseImprovedLakes,
            CaveConfig = new CaveGenerationConfig
            {
                HorizontalFrequency = config.Caves.HorizontalFrequency,
                VerticalFrequency = config.Caves.VerticalFrequency,
                Threshold = config.Caves.Threshold,
                LavaThreshold = config.Caves.LavaThreshold,
                WaterThreshold = config.Caves.WaterThreshold,
                UseRegionalMainCaves = config.Caves.UseRegionalMainCaves,
                RegionalMainCaveRegionSizeChunks = config.Caves.RegionalMainCaveRegionSizeChunks,
                // ... other cave parameters
            },
            RiverConfig = new RiverGenerationConfig
            {
                NoiseScale = config.Water.RiverNoiseScale,
                Depth = config.Water.RiverDepth,
                CenterThreshold = config.Water.RiverCenterThreshold,
                BankThreshold = config.Water.RiverBankThreshold,
                EdgeFeather = config.Water.RiverEdgeFeather,
                // ... other river parameters
            },
            LakeConfig = new LakeGenerationConfig
            {
                RiverProximitySuppression = config.Lakes.RiverProximitySuppression,
                BasinSmoothIterations = config.Lakes.LakeBasinSmoothIterations,
                InflowBlendWeight = config.Water.LakeInflowBlendWeight,
                // ... other lake parameters
            },
            HydrologyConfig = new HydrologyConfig
            {
                SmoothIterations = config.Water.HydrologySmoothIterations,
                SmoothBlend = config.Water.HydrologySmoothBlend,
                ShorePush = config.Water.HydrologyShorePush,
                SlopePenalty = config.Water.HydrologySlopePenalty,
                FlowGain = config.Water.HydrologyFlowGain,
                // ... other hydrology parameters
            }
        };
    }
    
    public void UpdateWorldProfile(WorldMapControlProfile newProfile)
    {
        _profileManager.UpdateProfile(newProfile);
        _mapControlProfile = newProfile;
        
        // Notify terrain generation pipeline of profile change
        OnProfileUpdated(newProfile);
    }
}
```

#### Client Integration
```csharp
public class WorldAreaManager : MonoBehaviour
{
    private WorldMapControlProfileReceiver _profileReceiver;
    
    public async void Init()
    {
        // Existing initialization...
        
        // Initialize profile receiver
        _profileReceiver = gameObject.AddComponent<WorldMapControlProfileReceiver>();
        
        // Request current profile from server
        await RequestWorldProfileFromServer();
    }
    
    private async Task RequestWorldProfileFromServer()
    {
        var gameClient = FindObjectOfType<MinecraftGameClient>();
        if (gameClient != null && gameClient.IsConnected)
        {
            // Send profile request to server
            gameClient.SendWorldProfileRequest();
        }
        else
        {
            // Use local configuration for offline mode
            var localProfile = CreateLocalProfile();
            _profileReceiver.OnProfileReceived(localProfile);
        }
    }
    
    private WorldMapControlProfile CreateLocalProfile()
    {
        var worldConfig = WorldConfigFile.Instance.GetConfig();
        return new WorldMapControlProfile
        {
            // Create profile from local configuration
            Version = CurrentProfileVersion,
            ProfileHash = GenerateLocalProfileHash(),
            // ... copy parameters from worldConfig
        };
    }
}
```

## Implementation Plan

### Phase 1: Core Profile System
1. Create unified WorldMapControlProfile class with sub-configurations
2. Implement profile serialization/deserialization
3. Create profile validation system
4. Add versioning support

### Phase 2: Server Integration
1. Update WorldManager to use profile system
2. Create profile manager for server side
3. Add profile update broadcasting
4. Integrate with existing terrain generation pipeline

### Phase 3: Client Integration
1. Update TerrainGenerator to use profile system
2. Create profile receiver for client side
3. Add profile request/response protocol
4. Update WorldAreaManager integration

### Phase 4: Synchronization & Validation
1. Implement profile synchronization protocol
2. Add compatibility checking
3. Create migration system for profile versions
4. Add validation for all profile parameters

### Phase 5: Testing & Optimization
1. Create comprehensive tests for profile system
2. Test server-client synchronization
3. Optimize profile serialization
4. Add error handling and recovery

## Benefits

1. **Consistency**: Ensures server and client use identical terrain generation parameters
2. **Flexibility**: Allows dynamic terrain feature toggles and parameter adjustments
3. **Maintainability**: Centralized configuration management
4. **Versioning**: Supports evolution of terrain generation algorithms
5. **Validation**: Prevents invalid configurations from causing issues
6. **Performance**: Efficient serialization and synchronization

## Migration Strategy

1. **Backward Compatibility**: Support existing configuration formats
2. **Gradual Migration**: Phase in new profile system alongside existing code
3. **Fallback Support**: Maintain fallback to local configuration if server sync fails
4. **Testing**: Comprehensive testing of migration scenarios
4. **Testing**: Comprehensive testing of migration scenarios
