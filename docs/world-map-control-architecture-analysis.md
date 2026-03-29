# World Map Control Architecture Analysis

**Date**: 2026-02-05
**Session**: 46
**Status**: Analysis Phase

## Overview

This document analyzes the World Map Control architecture across server and client implementations. The system ensures server-client parity for terrain generation through a shared profile system.

## Architecture Components

### 1. Shared Profile (GameCommon)

**File**: `GameCommon/World/WorldMapControlProfile.cs`

**Purpose**: Shared, data-driven snapshot for world map control so server and client hydrology/cave previews stay aligned.

**Key Properties**:
- Version tracking (int)
- ProfileHash for validation
- SourceConfig reference
- GeneratedAtUtc timestamp
- HydrologySignature for algorithm versioning
- All terrain generation parameters (hydrology, rivers, lakes, caves)
- Feature flags (EnableRivers, EnableLakes, EnableCaves, UseImproved*)

**Methods**:
- Clone(): Creates a copy of the profile
- EnsureDefaults(): Sets default values for required fields

**Framework**: netstandard2.1 (Unity 6 compatible)

### 2. Server Manager (GameServer)

**File**: `GameServer/World/WorldMapControlManager.cs`

**Purpose**: Lightweight world map control service that reuses enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

**Key Features**:
- Profile loading and validation
- Generation signature computation
- Chunk caching with budget enforcement
- Profile reload on file changes
- Hydrology signature validation

**Request Types**:
- GetInitialMap: Returns initial map data for a player
- UpdateChunk: Updates specific chunks
- GetPlayerProfile: Retrieves player profile
- UpdatePlayerProfile: Updates player profile settings

**Profile Validation**:
The server validates profiles on load:
1. Config newer than profile check
2. Profile hash drift detection
3. Version mismatch detection
4. Profile file update detection
5. Profile content change detection
6. Hydrology signature mismatch detection

**Generation Signature**:
Computes a comprehensive signature including:
- Pipeline version
- World name and seed
- Protobuf fingerprint
- Profile version and hash
- Hydrology signature
- All terrain generation parameters

### 3. Client Controller (Unity)

**File**: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`

**Purpose**: Unity-side world map controller that mirrors server map-control profile. Generates local preview chunks using JSON profile.

**Key Features**:
- Profile loading from StreamingAssets
- Periodic profile reload
- Chunk generation queue with concurrency control
- Player-centered chunk loading
- Automatic distant chunk unloading

**Components**:
- **WorldMapController**: MonoBehaviour managing the system
- **EnhancedTerrainGenerator**: Lightweight terrain generator for Unity previews
- **ChunkData**: Preview-friendly chunk container

**Profile Validation**:
The client validates profiles on load:
1. Profile hash verification
2. Hydrology signature matching
3. Version compatibility check
4. Fallback to WorldConfig if validation fails

**Profile Reload**:
- Periodic reload based on `profileReloadIntervalSeconds`
- Reload on world config file changes
- Reload on profile file changes
- Regenerates generator on signature changes

## Profile Comparison

### Server Profile (GameCommon)
```csharp
public sealed class WorldMapControlProfile
{
    public int Version { get; set; }
    public string ProfileHash { get; set; }
    public string HydrologySignature { get; set; }
    // ... 70+ properties for terrain generation
}
```

### Client Profile (Unity)
```csharp
[Serializable]
public class WorldMapControlProfile
{
    public int Version { get; private set; }
    public string ProfileHash { get; private set; }
    public string HydrologySignature { get; private set; }
    // ... 70+ properties for terrain generation
}
```

### Unity Serializable Data
```csharp
[Serializable]
public class WorldMapControlProfileData
{
    public int version;
    public string profileHash;
    public string hydrologySignature;
    // ... 70+ fields for terrain generation (camelCase)
}
```

## Key Differences

| Aspect | Server (GameCommon) | Client (Unity) |
|--------|---------------------|----------------|
| Framework | netstandard2.1 | Unity (C#) |
| Serialization | System.Text.Json | JsonUtility |
| Property Access | Public get/set | Private set (read-only) |
| Case Convention | PascalCase | PascalCase (data is camelCase) |
| Validation | EnsureDefaults() | LoadFromFile() with validation |
| Hash Computation | StringBuilder-based | StringBuilder-based |

## Hydrology Signature

**Current Signature**: `2026-02-05-hydrology-riverlake-cave-v15`

**Usage**:
- Stored in profile
- Validated on profile load
- Included in generation signature
- Used to detect algorithm changes

## Generation Signature

The generation signature is computed from:
1. Pipeline version
2. World name
3. World seed
4. Protobuf descriptor fingerprint
5. Protobuf computed fingerprint
6. Profile version
7. Profile hash
8. Hydrology signature
9. All terrain generation parameters (40+ parameters)

**Purpose**: Ensures that any change to generation parameters triggers a cache clear and regeneration.

## Terrain Generation Pipeline

### Server Pipeline
- Uses `EnhancedTerrainGenerationPipeline`
- Generates full server chunks
- Supports all improved algorithms

### Client Pipeline
- Uses `EnhancedTerrainGenerator` (lightweight)
- Generates preview chunks only
- Mirrors server algorithms for parity

## Issues Identified

### 1. Code Duplication
**Problem**: The client implements its own version of `WorldMapControlProfile` with identical properties.

**Impact**: Maintenance burden - changes must be made in two places.

**Recommendation**: Consider using the GameCommon profile directly in Unity if possible, or create a shared assembly.

### 2. Framework Mismatch
**Problem**: GameCommon targets netstandard2.1, while SharedProtocol targets net6.0.

**Impact**: GameCommon cannot reference SharedProtocol types.

**Recommendation**: See `docs/shared-dll-architecture-analysis.md` for detailed recommendations.

### 3. Profile Synchronization
**Problem**: Server and client profiles are loaded from different sources:
- Server: `generationConfig.MapControlProfilePath`
- Client: `Application.streamingAssetsPath + profileFileName`

**Impact**: Potential for profiles to get out of sync if not properly deployed.

**Recommendation**: Ensure profile deployment process copies server-generated profiles to client StreamingAssets.

### 4. Validation Inconsistency
**Problem**: Client validation includes hash verification that server doesn't have in the same way.

**Impact**: Client may reject profiles that server accepts.

**Recommendation**: Standardize validation logic between server and client.

## Strengths

1. **Comprehensive Validation**: Multiple validation checks ensure profile integrity
2. **Generation Signature**: Robust signature computation detects all parameter changes
3. **Profile Versioning**: Version tracking allows for profile upgrades
4. **Hash Verification**: Profile hash prevents tampering and corruption
5. **Hydrology Signature**: Ensures algorithm version compatibility
6. **Cache Management**: Server implements chunk caching with budget enforcement
7. **Automatic Reload**: Both server and client automatically reload on file changes

## Recommendations

### Immediate Actions

1. **Profile Deployment**: Implement automatic profile deployment from server to client StreamingAssets
2. **Validation Standardization**: Create shared validation logic
3. **Code Consolidation**: Consider using GameCommon profile in Unity

### Future Enhancements

1. **Shared Assembly**: Create a .NET Standard 2.1 assembly for shared types
2. **Profile Synchronization**: Implement real-time profile synchronization
3. **Profile Versioning**: Implement automatic profile upgrades
4. **Configuration Management**: Centralize configuration management
5. **Testing**: Add integration tests for profile synchronization

## Conclusion

The World Map Control architecture is well-designed with comprehensive validation and signature tracking. The main areas for improvement are code consolidation, profile synchronization, and validation standardization.

## Next Steps

1. Review protobuf protocol implementation
2. Implement missing core features
3. Implement missing content features
4. Implement missing util features
5. Create dummy client for protocol testing
6. Ensure shared DLL architecture for common enums/code
7. Update configuration files (JSON format)
8. Update data-driven JSON files
9. Update documentation in docs folder
10. Run compilation tests
11. Test protobuf packet handling and generation
12. Commit all changes to local git
13. Push changes to origin branch

**Date**: 2026-02-05
**Session**: 46
**Status**: Analysis Phase

## Overview

This document analyzes the World Map Control architecture across server and client implementations. The system ensures server-client parity for terrain generation through a shared profile system.

## Architecture Components

### 1. Shared Profile (GameCommon)

**File**: `GameCommon/World/WorldMapControlProfile.cs`

**Purpose**: Shared, data-driven snapshot for world map control so server and client hydrology/cave previews stay aligned.

**Key Properties**:
- Version tracking (int)
- ProfileHash for validation
- SourceConfig reference
- GeneratedAtUtc timestamp
- HydrologySignature for algorithm versioning
- All terrain generation parameters (hydrology, rivers, lakes, caves)
- Feature flags (EnableRivers, EnableLakes, EnableCaves, UseImproved*)

**Methods**:
- Clone(): Creates a copy of the profile
- EnsureDefaults(): Sets default values for required fields

**Framework**: netstandard2.1 (Unity 6 compatible)

### 2. Server Manager (GameServer)

**File**: `GameServer/World/WorldMapControlManager.cs`

**Purpose**: Lightweight world map control service that reuses enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

**Key Features**:
- Profile loading and validation
- Generation signature computation
- Chunk caching with budget enforcement
- Profile reload on file changes
- Hydrology signature validation

**Request Types**:
- GetInitialMap: Returns initial map data for a player
- UpdateChunk: Updates specific chunks
- GetPlayerProfile: Retrieves player profile
- UpdatePlayerProfile: Updates player profile settings

**Profile Validation**:
The server validates profiles on load:
1. Config newer than profile check
2. Profile hash drift detection
3. Version mismatch detection
4. Profile file update detection
5. Profile content change detection
6. Hydrology signature mismatch detection

**Generation Signature**:
Computes a comprehensive signature including:
- Pipeline version
- World name and seed
- Protobuf fingerprint
- Profile version and hash
- Hydrology signature
- All terrain generation parameters

### 3. Client Controller (Unity)

**File**: `Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`

**Purpose**: Unity-side world map controller that mirrors server map-control profile. Generates local preview chunks using JSON profile.

**Key Features**:
- Profile loading from StreamingAssets
- Periodic profile reload
- Chunk generation queue with concurrency control
- Player-centered chunk loading
- Automatic distant chunk unloading

**Components**:
- **WorldMapController**: MonoBehaviour managing the system
- **EnhancedTerrainGenerator**: Lightweight terrain generator for Unity previews
- **ChunkData**: Preview-friendly chunk container

**Profile Validation**:
The client validates profiles on load:
1. Profile hash verification
2. Hydrology signature matching
3. Version compatibility check
4. Fallback to WorldConfig if validation fails

**Profile Reload**:
- Periodic reload based on `profileReloadIntervalSeconds`
- Reload on world config file changes
- Reload on profile file changes
- Regenerates generator on signature changes

## Profile Comparison

### Server Profile (GameCommon)
```csharp
public sealed class WorldMapControlProfile
{
    public int Version { get; set; }
    public string ProfileHash { get; set; }
    public string HydrologySignature { get; set; }
    // ... 70+ properties for terrain generation
}
```

### Client Profile (Unity)
```csharp
[Serializable]
public class WorldMapControlProfile
{
    public int Version { get; private set; }
    public string ProfileHash { get; private set; }
    public string HydrologySignature { get; private set; }
    // ... 70+ properties for terrain generation
}
```

### Unity Serializable Data
```csharp
[Serializable]
public class WorldMapControlProfileData
{
    public int version;
    public string profileHash;
    public string hydrologySignature;
    // ... 70+ fields for terrain generation (camelCase)
}
```

## Key Differences

| Aspect | Server (GameCommon) | Client (Unity) |
|--------|---------------------|----------------|
| Framework | netstandard2.1 | Unity (C#) |
| Serialization | System.Text.Json | JsonUtility |
| Property Access | Public get/set | Private set (read-only) |
| Case Convention | PascalCase | PascalCase (data is camelCase) |
| Validation | EnsureDefaults() | LoadFromFile() with validation |
| Hash Computation | StringBuilder-based | StringBuilder-based |

## Hydrology Signature

**Current Signature**: `2026-02-05-hydrology-riverlake-cave-v15`

**Usage**:
- Stored in profile
- Validated on profile load
- Included in generation signature
- Used to detect algorithm changes

## Generation Signature

The generation signature is computed from:
1. Pipeline version
2. World name
3. World seed
4. Protobuf descriptor fingerprint
5. Protobuf computed fingerprint
6. Profile version
7. Profile hash
8. Hydrology signature
9. All terrain generation parameters (40+ parameters)

**Purpose**: Ensures that any change to generation parameters triggers a cache clear and regeneration.

## Terrain Generation Pipeline

### Server Pipeline
- Uses `EnhancedTerrainGenerationPipeline`
- Generates full server chunks
- Supports all improved algorithms

### Client Pipeline
- Uses `EnhancedTerrainGenerator` (lightweight)
- Generates preview chunks only
- Mirrors server algorithms for parity

## Issues Identified

### 1. Code Duplication
**Problem**: The client implements its own version of `WorldMapControlProfile` with identical properties.

**Impact**: Maintenance burden - changes must be made in two places.

**Recommendation**: Consider using the GameCommon profile directly in Unity if possible, or create a shared assembly.

### 2. Framework Mismatch
**Problem**: GameCommon targets netstandard2.1, while SharedProtocol targets net6.0.

**Impact**: GameCommon cannot reference SharedProtocol types.

**Recommendation**: See `docs/shared-dll-architecture-analysis.md` for detailed recommendations.

### 3. Profile Synchronization
**Problem**: Server and client profiles are loaded from different sources:
- Server: `generationConfig.MapControlProfilePath`
- Client: `Application.streamingAssetsPath + profileFileName`

**Impact**: Potential for profiles to get out of sync if not properly deployed.

**Recommendation**: Ensure profile deployment process copies server-generated profiles to client StreamingAssets.

### 4. Validation Inconsistency
**Problem**: Client validation includes hash verification that server doesn't have in the same way.

**Impact**: Client may reject profiles that server accepts.

**Recommendation**: Standardize validation logic between server and client.

## Strengths

1. **Comprehensive Validation**: Multiple validation checks ensure profile integrity
2. **Generation Signature**: Robust signature computation detects all parameter changes
3. **Profile Versioning**: Version tracking allows for profile upgrades
4. **Hash Verification**: Profile hash prevents tampering and corruption
5. **Hydrology Signature**: Ensures algorithm version compatibility
6. **Cache Management**: Server implements chunk caching with budget enforcement
7. **Automatic Reload**: Both server and client automatically reload on file changes

## Recommendations

### Immediate Actions

1. **Profile Deployment**: Implement automatic profile deployment from server to client StreamingAssets
2. **Validation Standardization**: Create shared validation logic
3. **Code Consolidation**: Consider using GameCommon profile in Unity

### Future Enhancements

1. **Shared Assembly**: Create a .NET Standard 2.1 assembly for shared types
2. **Profile Synchronization**: Implement real-time profile synchronization
3. **Profile Versioning**: Implement automatic profile upgrades
4. **Configuration Management**: Centralize configuration management
5. **Testing**: Add integration tests for profile synchronization

## Conclusion

The World Map Control architecture is well-designed with comprehensive validation and signature tracking. The main areas for improvement are code consolidation, profile synchronization, and validation standardization.

## Next Steps

1. Review protobuf protocol implementation
2. Implement missing core features
3. Implement missing content features
4. Implement missing util features
5. Create dummy client for protocol testing
6. Ensure shared DLL architecture for common enums/code
7. Update configuration files (JSON format)
8. Update data-driven JSON files
9. Update documentation in docs folder
10. Run compilation tests
11. Test protobuf packet handling and generation
12. Commit all changes to local git
13. Push changes to origin branch

