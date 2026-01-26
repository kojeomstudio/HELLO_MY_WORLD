# World Map Control Architecture Analysis
**Session-19 | 2026-01-26**

## Executive Summary

This document provides a comprehensive analysis of the World Map Control architecture for the Minecraft-like game system. The analysis covers both server-side components and identifies requirements for client-side synchronization.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Server-Side Components](#server-side-components)
3. [Client-Side Requirements](#client-side-requirements)
4. [Synchronization Mechanisms](#synchronization-mechanisms)
5. [Data Flow](#data-flow)
6. [Configuration Management](#configuration-management)
7. [Identified Issues and Improvements](#identified-issues-and-improvements)
8. [Recommendations](#recommendations)

---

## Architecture Overview

The World Map Control system is designed to provide consistent terrain generation previews between server and client, ensuring that players see the same world regardless of which side generates the preview.

### Key Design Principles

1. **Hydrology Signature v2**: Version-controlled signature for terrain generation algorithms
2. **Data-Driven Configuration**: All parameters stored in JSON profiles
3. **Hash-Based Validation**: SHA-256 hashes for profile integrity verification
4. **Dynamic Reloading**: Automatic detection and reloading of configuration changes
5. **Chunk Caching**: Efficient caching of generated chunks with budget enforcement

### Component Relationships

```
WorldMapControlManager
    ├── EnhancedTerrainGenerationPipeline (terrain generation)
    ├── WorldMapControlProfile (configuration snapshot)
    ├── WorldGenerationConfig (source configuration)
    ├── WorldSettings (world parameters)
    └── ChunkCache (generated chunk storage)
```

---

## Server-Side Components

### 1. WorldMapControlManager

**File**: `GameServer/World/WorldMapControlManager.cs`  
**Lines**: 437  
**Purpose**: Central service for handling world map control requests

#### Key Features

| Feature | Description |
|---------|-------------|
| **Profile Management** | Maintains per-player map preferences (render distance, scale, quality settings) |
| **Chunk Caching** | Concurrent dictionary cache with budget enforcement |
| **Dynamic Reloading** | Automatic detection of configuration file changes |
| **Hash Validation** | SHA-256 hash verification for config and profile integrity |
| **Generation Signature** | Comprehensive signature including protobuf fingerprint and hydrology parameters |

#### Request Types

```csharp
public enum WorldMapRequestType
{
    GetInitialMap,      // Request initial map around player position
    UpdateChunk,        // Request specific chunk updates
    GetPlayerProfile,   // Retrieve player's map preferences
    UpdatePlayerProfile // Update player's map preferences
}
```

#### Profile Validation Triggers

The manager reloads the profile when any of these conditions are met:

1. **Config newer than profile**: `world-config.json` modified after profile
2. **Profile hash drift**: Computed hash doesn't match stored hash
3. **Version mismatch**: Profile version older than config version
4. **Profile file updated**: Profile file modified
5. **Profile content changed**: SHA-256 hash of profile file changed
6. **Signature mismatch**: Hydrology signature doesn't match expected version

#### Generation Signature

The generation signature is a comprehensive string that includes:

```csharp
{PipelineVersion}:{WorldName}:{Seed}:{ProtoBaseline}:{ProtoComputed}:
{MapControlProfileVersion}:{ProfileHash}:{HydrologySignature}:{Version}:
{ChunkSize}:{WorldHeight}:{RenderDistance}:{SimulationDistance}:
{GlobalWaterLevel}:{SeaLevel}:{HydrologyFlowPersistence}:{HydrologyFlowGain}:
{HydrologyWatershedStitchWeight}:{HydrologyWatershedStitchRadius}:
{GradientStabilityIterations}:{GradientStabilityBlend}:{GradientClamp}:
{HydrologyWaterTableClampWeight}:{HydrologyWaterTableClampRange}:
{HydrologyWaterTableSlopeWeight}:{MinDepth}:{MaxDepth}:{ShelfDepth}:
{FlowSeepageWeight}:{CeilingMoistureWeight}:{CeilingMoistureClamp}:
{FloodedCaveNoiseFrequency}:{FloodedCaveThreshold}:
{FloodedCaveProximityToWaterTableWeight}:{WaterThreshold}:{LavaThreshold}:
{HydrologyEdgeBlendRadius}:{HydrologyEdgeVarianceClamp}:
{HydrologyEdgeNormalizationBlend}:{HydrologyEdgeNormalizationIterations}:
{HydrologyFlowMemoryWeight}:{HydrologyContinuityWeight}:
{RiverMeanderJitter}:{RiverReliefPenaltyWeight}:{VarianceWeight}:
{OutflowStabilityWeight}:{HydrologyFlowShadowWeight}:
{HydrologyFlowShadowSlopeWeight}:{WetlandBufferRadius}:{LakeInflowBlendWeight}:
{HydrologyVarianceBlend}:{HydrologyVarianceClamp}:
{HydrologyEdgeStabilityIterations}:{HydrologyEdgeStabilityWeight}:
{HydrologyEdgeFlowLockWeight}:{HydrologyEdgeFlowBias}:
{HydrologyEdgeTangentWeight}:{HydrologyEdgeFluxBlend}:
{HydrologyDirectionalBlend}:{HydrologyDirectionalIterations}:
{HydrologyFlowDivergenceClamp}:{HydrologySeamRelaxBlend}:
{HydrologySeamRelaxIterations}:{EdgeSealStrength}:{SupportDensity}:
{SupportPillarChance}:{RiverProximitySuppression}:{WorldConfigHash}:{ProfileContentHash}
```

This signature ensures that any change to terrain generation parameters results in a new signature, triggering cache invalidation and profile regeneration.

### 2. WorldMapControlProfile

**File**: `GameServer/World/WorldMapControlProfile.cs`  
**Lines**: 449  
**Purpose**: Data-driven snapshot of world map control parameters

#### Profile Structure

```csharp
public sealed class WorldMapControlProfile
{
    // Metadata
    public int Version { get; set; }
    public string ProfileHash { get; set; }
    public string SourceConfig { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string HydrologySignature { get; set; }
    
    // World Parameters
    public int ChunkSize { get; set; }
    public int RenderDistance { get; set; }
    public int SimulationDistance { get; set; }
    public int GlobalWaterLevel { get; set; }
    
    // Hydrology Parameters (40+ parameters)
    // - Gradient stability
    // - Edge blending
    // - Variance control
    // - Flow dynamics
    // - Water table constraints
    // - Seam reduction
    
    // River Parameters (20+ parameters)
    // - Meandering
    // - Bank erosion
    // - Confluence boosting
    // - Delta wetlands
    
    // Lake Parameters (15+ parameters)
    // - Shelf depth
    // - Outflow channels
    // - Wetland buffering
    // - River proximity suppression
    
    // Cave Parameters (15+ parameters)
    // - Edge sealing
    // - Support pillars
    // - Hydrology integration
    // - Ceiling stability
    
    // Feature Flags
    public bool EnableRivers { get; set; }
    public bool EnableLakes { get; set; }
    public bool EnableCaves { get; set; }
    public bool UseImprovedCaves { get; set; }
    public bool UseImprovedRivers { get; set; }
    public bool UseImprovedLakes { get; set; }
}
```

#### Profile Hash Computation

The profile hash is computed by concatenating all profile parameters with pipe separators and computing SHA-256:

```csharp
public static string ComputeHash(WorldMapControlProfile profile)
{
    var builder = new StringBuilder();
    builder.Append(profile.Version).Append('|')
           .Append(profile.ChunkSize).Append('|')
           // ... all 90+ parameters ...
           .Append(profile.UseImprovedLakes);
    
    using var sha = SHA256.Create();
    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

#### Profile Lifecycle

1. **Creation**: `WorldMapControlProfile.Create(config, worldSettings)`
   - Reads from `WorldGenerationConfig`
   - Clamps values to valid ranges
   - Computes initial hash
   - Sets hydrology signature

2. **Serialization**: `WorldMapControlProfileUtility.Save(profile, path)`
   - JSON serialization with camelCase naming
   - Creates directory if needed
   - Writes to file with indentation

3. **Deserialization**: `WorldMapControlProfileUtility.Load(path)`
   - Reads from JSON file
   - Handles missing hydrology signature
   - Recomputes hash if missing
   - Returns null on error

4. **Load or Create**: `WorldMapControlProfileUtility.LoadOrCreate(config, worldSettings)`
   - Loads existing profile if hash matches
   - Creates new profile if hash differs or version is older
   - Saves new profile to disk

---

## Client-Side Requirements

### Required Components

The following client-side components are required for proper synchronization:

1. **WorldMapControlProfile Reader**
   - Load profile from `StreamingAssets/world-map-control.json`
   - Parse JSON with same options as server
   - Validate profile hash and hydrology signature

2. **Client-Side Terrain Pipeline**
   - Implement `EnhancedTerrainGenerationPipeline` equivalent
   - Use same parameters from profile
   - Generate preview chunks for minimap

3. **Profile Validation**
   - Compare received `ControlProfileHash` with local hash
   - Verify `GenerationSignature` matches server
   - Request new profile if mismatch detected

4. **Chunk Update Handler**
   - Process `WorldMapResponse` from server
   - Update local chunk cache
   - Render updated chunks on minimap

5. **Player Profile Manager**
   - Store player preferences locally
   - Send profile updates to server
   - Apply quality settings to rendering

### Synchronization Flow

```
Client                                      Server
  |                                           |
  |--- WorldMapRequest (GetInitialMap) ------>|
  |                                           |-- Load/Create Profile
  |                                           |-- Validate Signature
  |                                           |-- Generate Chunks
  |                                           |
  |<-- WorldMapResponse ----------------------|
  |   - ControlProfile                        |
  |   - ControlProfileHash                    |
  |   - GenerationSignature                  |
  |   - WorldMapData (Chunks)                |
  |                                           |
  |-- Validate Profile Hash ---------------->|
  |   (if mismatch)                           |
  |                                           |
  |--- WorldMapRequest (UpdateChunk) -------->|
  |                                           |
  |<-- WorldMapResponse ----------------------|
  |   - Updated Chunks                        |
```

---

## Synchronization Mechanisms

### 1. Hash-Based Validation

- **Profile Hash**: SHA-256 of all profile parameters
- **Config Hash**: SHA-256 of world-config.json content
- **Generation Signature**: Comprehensive signature including all generation parameters
- **Proto Fingerprint**: SHA-256 of protobuf descriptor

### 2. Version Control

- **Profile Version**: Incremented when profile structure changes
- **Hydrology Signature**: Version string for terrain generation algorithm
- **MapControlProfileVersion**: Config parameter for profile versioning

### 3. Change Detection

- **File Write Time**: Monitors modification times of config files
- **Content Hash**: Computes SHA-256 of file content
- **Signature Comparison**: Compares generation signatures before using cached data

### 4. Cache Invalidation

The chunk cache is cleared when:
- Profile is reloaded
- Generation signature changes
- Config file is modified
- Profile file is modified

---

## Data Flow

### Initial Map Request

```
1. Client sends WorldMapRequest with player position
2. Server validates profile (checks hash, signature, version)
3. Server generates chunks around player (render distance)
4. Server returns WorldMapResponse with:
   - ControlProfile (full profile data)
   - ControlProfileHash (for validation)
   - GenerationSignature (for parity check)
   - WorldMapData (chunk data)
   - PlayerProfile (player preferences)
5. Client validates profile hash
6. Client renders chunks on minimap
```

### Chunk Update Request

```
1. Client sends WorldMapRequest with chunk updates
2. Server validates profile (may reload if changed)
3. Server generates requested chunks
4. Server returns WorldMapResponse with:
   - ControlProfileHash (only if changed)
   - GenerationSignature
   - WorldMapData (updated chunks)
   - PlayerProfile (updated preferences)
5. Client updates local cache
6. Client renders updated chunks
```

### Profile Update Request

```
1. Client sends WorldMapRequest with profile updates
2. Server updates player profile in memory
3. Server returns WorldMapResponse with:
   - ControlProfileHash
   - GenerationSignature
   - PlayerProfile (updated)
4. Client applies new preferences
```

---

## Configuration Management

### Configuration Files

| File | Purpose | Format |
|------|---------|--------|
| `config/world.json` | World generation source configuration | JSON |
| `config/world_map_control_profile.json` | Generated profile snapshot | JSON |
| `Assets/StreamingAssets/world-map-control.json` | Client-side profile copy | JSON |

### Configuration Hierarchy

```
WorldGenerationConfig (source)
    ├── WorldSettings (seed, size, etc.)
    ├── WaterConfig (hydrology parameters)
    ├── LakesConfig (lake parameters)
    └── CavesConfig (cave parameters)
         ↓
WorldMapControlProfile (snapshot)
    ├── All generation parameters
    ├── Hashes and signatures
    └── Feature flags
         ↓
Client Profile (copy)
    ├── Same structure as server profile
    └── Used for local terrain generation
```

### Dynamic Reloading

The server automatically reloads configuration when:

1. **Config file modified**: `world-config.json` write time changes
2. **Config hash changes**: SHA-256 hash of config content changes
3. **Profile file modified**: Profile file write time changes
4. **Profile content changes**: SHA-256 hash of profile content changes

On reload:
- Config is reloaded from disk
- Profile is regenerated
- Chunk cache is cleared
- Pipeline is rebuilt
- Generation signature is refreshed

---

## Identified Issues and Improvements

### 1. Missing Client-Side Implementation

**Issue**: No client-side WorldMapControlManager equivalent exists in Unity project.

**Impact**: 
- Client cannot validate server profiles
- Client cannot generate local terrain previews
- No minimap functionality

**Recommendation**: 
- Implement `Assets/Scripts/Minecraft/World/WorldMapControlManager.cs`
- Copy profile structure from server
- Implement same validation logic
- Add Unity-specific rendering integration

### 2. No Profile Synchronization Protocol

**Issue**: No dedicated protobuf message for profile synchronization.

**Impact**:
- Profile data embedded in WorldMapResponse
- No separate profile validation endpoint
- Harder to debug profile mismatches

**Recommendation**:
- Add `WorldMapProfileSync` protobuf message
- Add dedicated `ValidateProfile` request type
- Separate profile validation from chunk requests

### 3. Limited Error Reporting

**Issue**: Profile load failures only log to console, no error response to client.

**Impact**:
- Client doesn't know why profile validation failed
- Difficult to diagnose configuration issues

**Recommendation**:
- Add error codes to WorldMapResponse
- Include detailed error messages
- Log errors to server log file

### 4. No Profile Diff Support

**Issue**: Full profile sent on every initial map request.

**Impact**:
- Unnecessary network bandwidth
- Slower initial map loading

**Recommendation**:
- Implement profile diff protocol
- Send only changed parameters
- Use incremental updates

### 5. Cache Inefficiency

**Issue**: Simple FIFO cache removal without LRU or access time tracking.

**Impact**:
- Frequently accessed chunks may be evicted
- Unnecessary regeneration

**Recommendation**:
- Implement LRU cache
- Track access times
- Prioritize chunks near player

### 6. No Profile Version Migration

**Issue**: Old profiles are discarded without migration.

**Impact**:
- Player preferences lost on profile update
- Need to reconfigure after update

**Recommendation**:
- Implement profile migration system
- Preserve player settings across versions
- Add migration path for each profile version

### 7. Hydrology Signature Hardcoded

**Issue**: `SharedFeatureCatalog.HydrologySignature` is a constant string.

**Impact**:
- Hard to track algorithm changes
- No automatic version increment

**Recommendation**:
- Move to versioned constant
- Add change log for each signature
- Auto-increment on algorithm changes

### 8. No Client-Side Profile Persistence

**Issue**: Client doesn't persist profile locally.

**Impact**:
- Must download profile on every connection
- Slower connection establishment

**Recommendation**:
- Cache profile locally
- Validate on startup
- Update only when hash changes

### 9. Limited Profile Validation

**Issue**: Profile only validated for hash and signature, not parameter ranges.

**Impact**:
- Invalid parameters may cause runtime errors
- Hard to catch configuration mistakes

**Recommendation**:
- Add parameter range validation
- Validate before applying profile
- Return validation errors to client

### 10. No Profile Rollback

**Issue**: No mechanism to rollback to previous profile version.

**Impact**:
- Bad profile update breaks all clients
- Must manually restore config files

**Recommendation**:
- Keep backup of previous profile
- Add rollback command
- Automatic rollback on validation failure

---

## Recommendations

### High Priority

1. **Implement Client-Side WorldMapControlManager**
   - Create Unity equivalent of server manager
   - Implement profile validation
   - Add minimap rendering

2. **Add Profile Validation Endpoint**
   - Separate profile validation from chunk requests
   - Return detailed validation results
   - Support profile diff

3. **Improve Error Reporting**
   - Add error codes to responses
   - Include detailed error messages
   - Log to server log file

4. **Implement Profile Caching on Client**
   - Persist profile locally
   - Validate on startup
   - Update only when needed

### Medium Priority

5. **Implement LRU Cache**
   - Track chunk access times
   - Prioritize near-player chunks
   - Improve cache efficiency

6. **Add Profile Migration System**
   - Preserve player settings
   - Support version upgrades
   - Document migration paths

7. **Improve Profile Validation**
   - Add parameter range checks
   - Validate before applying
   - Return validation errors

### Low Priority

8. **Add Profile Rollback**
   - Keep backup profiles
   - Support manual rollback
   - Auto-rollback on failure

9. **Implement Profile Diff**
   - Send only changed parameters
   - Reduce bandwidth usage
   - Faster updates

10. **Version Hydrology Signature**
    - Move to versioned constant
    - Add change log
    - Auto-increment on changes

---

## Conclusion

The World Map Control architecture provides a solid foundation for server-client terrain synchronization. The data-driven approach with hash-based validation ensures consistency between server and client. However, several improvements are needed to complete the implementation, particularly on the client side.

The most critical missing piece is the client-side WorldMapControlManager implementation, which is essential for minimap functionality and profile validation. Once implemented, the system will provide consistent terrain previews across server and client, with automatic detection and handling of configuration changes.

---

## Appendix A: Profile Parameters

### Complete Parameter List (90+ parameters)

| Category | Parameters |
|----------|------------|
| **Metadata** | Version, ProfileHash, SourceConfig, GeneratedAtUtc, HydrologySignature |
| **World** | ChunkSize, RenderDistance, SimulationDistance, GlobalWaterLevel |
| **Hydrology** | 40+ parameters for gradient stability, edge blending, variance control, flow dynamics, water table constraints, seam reduction |
| **Rivers** | 20+ parameters for meandering, bank erosion, confluence boosting, delta wetlands |
| **Lakes** | 15+ parameters for shelf depth, outflow channels, wetland buffering, river proximity suppression |
| **Caves** | 15+ parameters for edge sealing, support pillars, hydrology integration, ceiling stability |
| **Flags** | EnableRivers, EnableLakes, EnableCaves, UseImprovedCaves, UseImprovedRivers, UseImprovedLakes |

---

## Appendix B: Request/Response Structures

### WorldMapRequest

```csharp
public sealed class WorldMapRequest
{
    public WorldMapRequestType Type { get; set; }
    public int PlayerId { get; set; }
    public double PlayerX { get; set; }
    public double PlayerY { get; set; }
    public double PlayerZ { get; set; }
    public List<ChunkUpdate>? ChunkUpdates { get; set; }
    public List<ProfileUpdate>? ProfileUpdates { get; set; }
}
```

### WorldMapResponse

```csharp
public sealed class WorldMapResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public WorldMapData? WorldMapData { get; set; }
    public WorldMapProfile? PlayerProfile { get; set; }
    public WorldMapControlProfile? ControlProfile { get; set; }
    public string ControlProfileHash { get; set; }
    public string GenerationSignature { get; set; }
}
```

---

## Appendix C: Configuration File Paths

| Platform | Path |
|----------|------|
| **Server Config** | `config/world.json` |
| **Server Profile** | `config/world_map_control_profile.json` |
| **Client Profile** | `Assets/StreamingAssets/world-map-control.json` |

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-26  
**Session**: Session-19  
**Author**: Kilo Code
**Session-19 | 2026-01-26**

## Executive Summary

This document provides a comprehensive analysis of the World Map Control architecture for the Minecraft-like game system. The analysis covers both server-side components and identifies requirements for client-side synchronization.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Server-Side Components](#server-side-components)
3. [Client-Side Requirements](#client-side-requirements)
4. [Synchronization Mechanisms](#synchronization-mechanisms)
5. [Data Flow](#data-flow)
6. [Configuration Management](#configuration-management)
7. [Identified Issues and Improvements](#identified-issues-and-improvements)
8. [Recommendations](#recommendations)

---

## Architecture Overview

The World Map Control system is designed to provide consistent terrain generation previews between server and client, ensuring that players see the same world regardless of which side generates the preview.

### Key Design Principles

1. **Hydrology Signature v2**: Version-controlled signature for terrain generation algorithms
2. **Data-Driven Configuration**: All parameters stored in JSON profiles
3. **Hash-Based Validation**: SHA-256 hashes for profile integrity verification
4. **Dynamic Reloading**: Automatic detection and reloading of configuration changes
5. **Chunk Caching**: Efficient caching of generated chunks with budget enforcement

### Component Relationships

```
WorldMapControlManager
    ├── EnhancedTerrainGenerationPipeline (terrain generation)
    ├── WorldMapControlProfile (configuration snapshot)
    ├── WorldGenerationConfig (source configuration)
    ├── WorldSettings (world parameters)
    └── ChunkCache (generated chunk storage)
```

---

## Server-Side Components

### 1. WorldMapControlManager

**File**: `GameServer/World/WorldMapControlManager.cs`  
**Lines**: 437  
**Purpose**: Central service for handling world map control requests

#### Key Features

| Feature | Description |
|---------|-------------|
| **Profile Management** | Maintains per-player map preferences (render distance, scale, quality settings) |
| **Chunk Caching** | Concurrent dictionary cache with budget enforcement |
| **Dynamic Reloading** | Automatic detection of configuration file changes |
| **Hash Validation** | SHA-256 hash verification for config and profile integrity |
| **Generation Signature** | Comprehensive signature including protobuf fingerprint and hydrology parameters |

#### Request Types

```csharp
public enum WorldMapRequestType
{
    GetInitialMap,      // Request initial map around player position
    UpdateChunk,        // Request specific chunk updates
    GetPlayerProfile,   // Retrieve player's map preferences
    UpdatePlayerProfile // Update player's map preferences
}
```

#### Profile Validation Triggers

The manager reloads the profile when any of these conditions are met:

1. **Config newer than profile**: `world-config.json` modified after profile
2. **Profile hash drift**: Computed hash doesn't match stored hash
3. **Version mismatch**: Profile version older than config version
4. **Profile file updated**: Profile file modified
5. **Profile content changed**: SHA-256 hash of profile file changed
6. **Signature mismatch**: Hydrology signature doesn't match expected version

#### Generation Signature

The generation signature is a comprehensive string that includes:

```csharp
{PipelineVersion}:{WorldName}:{Seed}:{ProtoBaseline}:{ProtoComputed}:
{MapControlProfileVersion}:{ProfileHash}:{HydrologySignature}:{Version}:
{ChunkSize}:{WorldHeight}:{RenderDistance}:{SimulationDistance}:
{GlobalWaterLevel}:{SeaLevel}:{HydrologyFlowPersistence}:{HydrologyFlowGain}:
{HydrologyWatershedStitchWeight}:{HydrologyWatershedStitchRadius}:
{GradientStabilityIterations}:{GradientStabilityBlend}:{GradientClamp}:
{HydrologyWaterTableClampWeight}:{HydrologyWaterTableClampRange}:
{HydrologyWaterTableSlopeWeight}:{MinDepth}:{MaxDepth}:{ShelfDepth}:
{FlowSeepageWeight}:{CeilingMoistureWeight}:{CeilingMoistureClamp}:
{FloodedCaveNoiseFrequency}:{FloodedCaveThreshold}:
{FloodedCaveProximityToWaterTableWeight}:{WaterThreshold}:{LavaThreshold}:
{HydrologyEdgeBlendRadius}:{HydrologyEdgeVarianceClamp}:
{HydrologyEdgeNormalizationBlend}:{HydrologyEdgeNormalizationIterations}:
{HydrologyFlowMemoryWeight}:{HydrologyContinuityWeight}:
{RiverMeanderJitter}:{RiverReliefPenaltyWeight}:{VarianceWeight}:
{OutflowStabilityWeight}:{HydrologyFlowShadowWeight}:
{HydrologyFlowShadowSlopeWeight}:{WetlandBufferRadius}:{LakeInflowBlendWeight}:
{HydrologyVarianceBlend}:{HydrologyVarianceClamp}:
{HydrologyEdgeStabilityIterations}:{HydrologyEdgeStabilityWeight}:
{HydrologyEdgeFlowLockWeight}:{HydrologyEdgeFlowBias}:
{HydrologyEdgeTangentWeight}:{HydrologyEdgeFluxBlend}:
{HydrologyDirectionalBlend}:{HydrologyDirectionalIterations}:
{HydrologyFlowDivergenceClamp}:{HydrologySeamRelaxBlend}:
{HydrologySeamRelaxIterations}:{EdgeSealStrength}:{SupportDensity}:
{SupportPillarChance}:{RiverProximitySuppression}:{WorldConfigHash}:{ProfileContentHash}
```

This signature ensures that any change to terrain generation parameters results in a new signature, triggering cache invalidation and profile regeneration.

### 2. WorldMapControlProfile

**File**: `GameServer/World/WorldMapControlProfile.cs`  
**Lines**: 449  
**Purpose**: Data-driven snapshot of world map control parameters

#### Profile Structure

```csharp
public sealed class WorldMapControlProfile
{
    // Metadata
    public int Version { get; set; }
    public string ProfileHash { get; set; }
    public string SourceConfig { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string HydrologySignature { get; set; }
    
    // World Parameters
    public int ChunkSize { get; set; }
    public int RenderDistance { get; set; }
    public int SimulationDistance { get; set; }
    public int GlobalWaterLevel { get; set; }
    
    // Hydrology Parameters (40+ parameters)
    // - Gradient stability
    // - Edge blending
    // - Variance control
    // - Flow dynamics
    // - Water table constraints
    // - Seam reduction
    
    // River Parameters (20+ parameters)
    // - Meandering
    // - Bank erosion
    // - Confluence boosting
    // - Delta wetlands
    
    // Lake Parameters (15+ parameters)
    // - Shelf depth
    // - Outflow channels
    // - Wetland buffering
    // - River proximity suppression
    
    // Cave Parameters (15+ parameters)
    // - Edge sealing
    // - Support pillars
    // - Hydrology integration
    // - Ceiling stability
    
    // Feature Flags
    public bool EnableRivers { get; set; }
    public bool EnableLakes { get; set; }
    public bool EnableCaves { get; set; }
    public bool UseImprovedCaves { get; set; }
    public bool UseImprovedRivers { get; set; }
    public bool UseImprovedLakes { get; set; }
}
```

#### Profile Hash Computation

The profile hash is computed by concatenating all profile parameters with pipe separators and computing SHA-256:

```csharp
public static string ComputeHash(WorldMapControlProfile profile)
{
    var builder = new StringBuilder();
    builder.Append(profile.Version).Append('|')
           .Append(profile.ChunkSize).Append('|')
           // ... all 90+ parameters ...
           .Append(profile.UseImprovedLakes);
    
    using var sha = SHA256.Create();
    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```

#### Profile Lifecycle

1. **Creation**: `WorldMapControlProfile.Create(config, worldSettings)`
   - Reads from `WorldGenerationConfig`
   - Clamps values to valid ranges
   - Computes initial hash
   - Sets hydrology signature

2. **Serialization**: `WorldMapControlProfileUtility.Save(profile, path)`
   - JSON serialization with camelCase naming
   - Creates directory if needed
   - Writes to file with indentation

3. **Deserialization**: `WorldMapControlProfileUtility.Load(path)`
   - Reads from JSON file
   - Handles missing hydrology signature
   - Recomputes hash if missing
   - Returns null on error

4. **Load or Create**: `WorldMapControlProfileUtility.LoadOrCreate(config, worldSettings)`
   - Loads existing profile if hash matches
   - Creates new profile if hash differs or version is older
   - Saves new profile to disk

---

## Client-Side Requirements

### Required Components

The following client-side components are required for proper synchronization:

1. **WorldMapControlProfile Reader**
   - Load profile from `StreamingAssets/world-map-control.json`
   - Parse JSON with same options as server
   - Validate profile hash and hydrology signature

2. **Client-Side Terrain Pipeline**
   - Implement `EnhancedTerrainGenerationPipeline` equivalent
   - Use same parameters from profile
   - Generate preview chunks for minimap

3. **Profile Validation**
   - Compare received `ControlProfileHash` with local hash
   - Verify `GenerationSignature` matches server
   - Request new profile if mismatch detected

4. **Chunk Update Handler**
   - Process `WorldMapResponse` from server
   - Update local chunk cache
   - Render updated chunks on minimap

5. **Player Profile Manager**
   - Store player preferences locally
   - Send profile updates to server
   - Apply quality settings to rendering

### Synchronization Flow

```
Client                                      Server
  |                                           |
  |--- WorldMapRequest (GetInitialMap) ------>|
  |                                           |-- Load/Create Profile
  |                                           |-- Validate Signature
  |                                           |-- Generate Chunks
  |                                           |
  |<-- WorldMapResponse ----------------------|
  |   - ControlProfile                        |
  |   - ControlProfileHash                    |
  |   - GenerationSignature                  |
  |   - WorldMapData (Chunks)                |
  |                                           |
  |-- Validate Profile Hash ---------------->|
  |   (if mismatch)                           |
  |                                           |
  |--- WorldMapRequest (UpdateChunk) -------->|
  |                                           |
  |<-- WorldMapResponse ----------------------|
  |   - Updated Chunks                        |
```

---

## Synchronization Mechanisms

### 1. Hash-Based Validation

- **Profile Hash**: SHA-256 of all profile parameters
- **Config Hash**: SHA-256 of world-config.json content
- **Generation Signature**: Comprehensive signature including all generation parameters
- **Proto Fingerprint**: SHA-256 of protobuf descriptor

### 2. Version Control

- **Profile Version**: Incremented when profile structure changes
- **Hydrology Signature**: Version string for terrain generation algorithm
- **MapControlProfileVersion**: Config parameter for profile versioning

### 3. Change Detection

- **File Write Time**: Monitors modification times of config files
- **Content Hash**: Computes SHA-256 of file content
- **Signature Comparison**: Compares generation signatures before using cached data

### 4. Cache Invalidation

The chunk cache is cleared when:
- Profile is reloaded
- Generation signature changes
- Config file is modified
- Profile file is modified

---

## Data Flow

### Initial Map Request

```
1. Client sends WorldMapRequest with player position
2. Server validates profile (checks hash, signature, version)
3. Server generates chunks around player (render distance)
4. Server returns WorldMapResponse with:
   - ControlProfile (full profile data)
   - ControlProfileHash (for validation)
   - GenerationSignature (for parity check)
   - WorldMapData (chunk data)
   - PlayerProfile (player preferences)
5. Client validates profile hash
6. Client renders chunks on minimap
```

### Chunk Update Request

```
1. Client sends WorldMapRequest with chunk updates
2. Server validates profile (may reload if changed)
3. Server generates requested chunks
4. Server returns WorldMapResponse with:
   - ControlProfileHash (only if changed)
   - GenerationSignature
   - WorldMapData (updated chunks)
   - PlayerProfile (updated preferences)
5. Client updates local cache
6. Client renders updated chunks
```

### Profile Update Request

```
1. Client sends WorldMapRequest with profile updates
2. Server updates player profile in memory
3. Server returns WorldMapResponse with:
   - ControlProfileHash
   - GenerationSignature
   - PlayerProfile (updated)
4. Client applies new preferences
```

---

## Configuration Management

### Configuration Files

| File | Purpose | Format |
|------|---------|--------|
| `config/world.json` | World generation source configuration | JSON |
| `config/world_map_control_profile.json` | Generated profile snapshot | JSON |
| `Assets/StreamingAssets/world-map-control.json` | Client-side profile copy | JSON |

### Configuration Hierarchy

```
WorldGenerationConfig (source)
    ├── WorldSettings (seed, size, etc.)
    ├── WaterConfig (hydrology parameters)
    ├── LakesConfig (lake parameters)
    └── CavesConfig (cave parameters)
         ↓
WorldMapControlProfile (snapshot)
    ├── All generation parameters
    ├── Hashes and signatures
    └── Feature flags
         ↓
Client Profile (copy)
    ├── Same structure as server profile
    └── Used for local terrain generation
```

### Dynamic Reloading

The server automatically reloads configuration when:

1. **Config file modified**: `world-config.json` write time changes
2. **Config hash changes**: SHA-256 hash of config content changes
3. **Profile file modified**: Profile file write time changes
4. **Profile content changes**: SHA-256 hash of profile content changes

On reload:
- Config is reloaded from disk
- Profile is regenerated
- Chunk cache is cleared
- Pipeline is rebuilt
- Generation signature is refreshed

---

## Identified Issues and Improvements

### 1. Missing Client-Side Implementation

**Issue**: No client-side WorldMapControlManager equivalent exists in Unity project.

**Impact**: 
- Client cannot validate server profiles
- Client cannot generate local terrain previews
- No minimap functionality

**Recommendation**: 
- Implement `Assets/Scripts/Minecraft/World/WorldMapControlManager.cs`
- Copy profile structure from server
- Implement same validation logic
- Add Unity-specific rendering integration

### 2. No Profile Synchronization Protocol

**Issue**: No dedicated protobuf message for profile synchronization.

**Impact**:
- Profile data embedded in WorldMapResponse
- No separate profile validation endpoint
- Harder to debug profile mismatches

**Recommendation**:
- Add `WorldMapProfileSync` protobuf message
- Add dedicated `ValidateProfile` request type
- Separate profile validation from chunk requests

### 3. Limited Error Reporting

**Issue**: Profile load failures only log to console, no error response to client.

**Impact**:
- Client doesn't know why profile validation failed
- Difficult to diagnose configuration issues

**Recommendation**:
- Add error codes to WorldMapResponse
- Include detailed error messages
- Log errors to server log file

### 4. No Profile Diff Support

**Issue**: Full profile sent on every initial map request.

**Impact**:
- Unnecessary network bandwidth
- Slower initial map loading

**Recommendation**:
- Implement profile diff protocol
- Send only changed parameters
- Use incremental updates

### 5. Cache Inefficiency

**Issue**: Simple FIFO cache removal without LRU or access time tracking.

**Impact**:
- Frequently accessed chunks may be evicted
- Unnecessary regeneration

**Recommendation**:
- Implement LRU cache
- Track access times
- Prioritize chunks near player

### 6. No Profile Version Migration

**Issue**: Old profiles are discarded without migration.

**Impact**:
- Player preferences lost on profile update
- Need to reconfigure after update

**Recommendation**:
- Implement profile migration system
- Preserve player settings across versions
- Add migration path for each profile version

### 7. Hydrology Signature Hardcoded

**Issue**: `SharedFeatureCatalog.HydrologySignature` is a constant string.

**Impact**:
- Hard to track algorithm changes
- No automatic version increment

**Recommendation**:
- Move to versioned constant
- Add change log for each signature
- Auto-increment on algorithm changes

### 8. No Client-Side Profile Persistence

**Issue**: Client doesn't persist profile locally.

**Impact**:
- Must download profile on every connection
- Slower connection establishment

**Recommendation**:
- Cache profile locally
- Validate on startup
- Update only when hash changes

### 9. Limited Profile Validation

**Issue**: Profile only validated for hash and signature, not parameter ranges.

**Impact**:
- Invalid parameters may cause runtime errors
- Hard to catch configuration mistakes

**Recommendation**:
- Add parameter range validation
- Validate before applying profile
- Return validation errors to client

### 10. No Profile Rollback

**Issue**: No mechanism to rollback to previous profile version.

**Impact**:
- Bad profile update breaks all clients
- Must manually restore config files

**Recommendation**:
- Keep backup of previous profile
- Add rollback command
- Automatic rollback on validation failure

---

## Recommendations

### High Priority

1. **Implement Client-Side WorldMapControlManager**
   - Create Unity equivalent of server manager
   - Implement profile validation
   - Add minimap rendering

2. **Add Profile Validation Endpoint**
   - Separate profile validation from chunk requests
   - Return detailed validation results
   - Support profile diff

3. **Improve Error Reporting**
   - Add error codes to responses
   - Include detailed error messages
   - Log to server log file

4. **Implement Profile Caching on Client**
   - Persist profile locally
   - Validate on startup
   - Update only when needed

### Medium Priority

5. **Implement LRU Cache**
   - Track chunk access times
   - Prioritize near-player chunks
   - Improve cache efficiency

6. **Add Profile Migration System**
   - Preserve player settings
   - Support version upgrades
   - Document migration paths

7. **Improve Profile Validation**
   - Add parameter range checks
   - Validate before applying
   - Return validation errors

### Low Priority

8. **Add Profile Rollback**
   - Keep backup profiles
   - Support manual rollback
   - Auto-rollback on failure

9. **Implement Profile Diff**
   - Send only changed parameters
   - Reduce bandwidth usage
   - Faster updates

10. **Version Hydrology Signature**
    - Move to versioned constant
    - Add change log
    - Auto-increment on changes

---

## Conclusion

The World Map Control architecture provides a solid foundation for server-client terrain synchronization. The data-driven approach with hash-based validation ensures consistency between server and client. However, several improvements are needed to complete the implementation, particularly on the client side.

The most critical missing piece is the client-side WorldMapControlManager implementation, which is essential for minimap functionality and profile validation. Once implemented, the system will provide consistent terrain previews across server and client, with automatic detection and handling of configuration changes.

---

## Appendix A: Profile Parameters

### Complete Parameter List (90+ parameters)

| Category | Parameters |
|----------|------------|
| **Metadata** | Version, ProfileHash, SourceConfig, GeneratedAtUtc, HydrologySignature |
| **World** | ChunkSize, RenderDistance, SimulationDistance, GlobalWaterLevel |
| **Hydrology** | 40+ parameters for gradient stability, edge blending, variance control, flow dynamics, water table constraints, seam reduction |
| **Rivers** | 20+ parameters for meandering, bank erosion, confluence boosting, delta wetlands |
| **Lakes** | 15+ parameters for shelf depth, outflow channels, wetland buffering, river proximity suppression |
| **Caves** | 15+ parameters for edge sealing, support pillars, hydrology integration, ceiling stability |
| **Flags** | EnableRivers, EnableLakes, EnableCaves, UseImprovedCaves, UseImprovedRivers, UseImprovedLakes |

---

## Appendix B: Request/Response Structures

### WorldMapRequest

```csharp
public sealed class WorldMapRequest
{
    public WorldMapRequestType Type { get; set; }
    public int PlayerId { get; set; }
    public double PlayerX { get; set; }
    public double PlayerY { get; set; }
    public double PlayerZ { get; set; }
    public List<ChunkUpdate>? ChunkUpdates { get; set; }
    public List<ProfileUpdate>? ProfileUpdates { get; set; }
}
```

### WorldMapResponse

```csharp
public sealed class WorldMapResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public WorldMapData? WorldMapData { get; set; }
    public WorldMapProfile? PlayerProfile { get; set; }
    public WorldMapControlProfile? ControlProfile { get; set; }
    public string ControlProfileHash { get; set; }
    public string GenerationSignature { get; set; }
}
```

---

## Appendix C: Configuration File Paths

| Platform | Path |
|----------|------|
| **Server Config** | `config/world.json` |
| **Server Profile** | `config/world_map_control_profile.json` |
| **Client Profile** | `Assets/StreamingAssets/world-map-control.json` |

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-26  
**Session**: Session-19  
**Author**: Kilo Code

