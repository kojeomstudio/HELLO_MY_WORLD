# World Map Control Architecture Review
**Date:** 2026-01-10  
**Status:** Completed Review

## Overview

This document reviews the world map control architecture for both server and client. The architecture provides profile-based configuration, hot-reload functionality, and synchronization between server and client.

---

## Server-Side Architecture

### WorldMapControlManager

**File:** [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

**Status:** ✅ Well-implemented

**Description:** Lightweight world map control service that reuses enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

### Key Features

#### Profile-Based Configuration
- **Control Profile**: Server-wide world map control profile
- **Player Profiles**: Per-player map preferences (render distance, map scale, etc.)
- **Profile Hash**: Hash-based profile validation for synchronization
- **Version Tracking**: Profile version tracking for compatibility

#### Hot-Reload Functionality
- **Config File Monitoring**: Monitors world.json for changes
- **Profile File Monitoring**: Monitors world_map_control_profile.json for changes
- **Automatic Reload**: Automatically reloads configuration when files change
- **Cache Invalidation**: Clears chunk cache when configuration changes

#### Generation Signature
- **Seed-Based Signature**: Includes world seed in generation signature
- **Config-Based Signature**: Includes all relevant configuration parameters
- **Profile Hash**: Includes profile hash for validation
- **Version Tracking**: Includes profile version for compatibility

### Request Types

| Type | Description |
|-------|-------------|
| `GetInitialMap` | Returns initial map data for a player |
| `UpdateChunk` | Updates specific chunks for a player |
| `GetPlayerProfile` | Returns player profile |
| `UpdatePlayerProfile` | Updates player profile |

### Profile Updates

| Type | Description |
|-------|-------------|
| `RenderDistance` | Updates render distance (2 to unload distance) |
| `MapScale` | Updates map scale (0.25 to 8.0) |
| `ShowCoordinates` | Toggles coordinate display |
| `ShowBiomeInfo` | Toggles biome info display |

### Configuration Files

| File | Description |
|-------|-------------|
| `config/world.json` | World generation configuration |
| `config/world_map_control_profile.json` | World map control profile |

### Key Methods

#### HandleAsync
Handles incoming world map requests and routes to appropriate handler.

#### EnsureProfile
Ensures profile is loaded and up-to-date. Handles:
- Config file changes
- Profile file changes
- Profile hash drift
- Version mismatches

#### MaybeReloadGenerationConfig
Reloads generation configuration if world.json has changed.

#### GenerateOrGetChunkAsync
Generates or retrieves cached chunk data.

#### ComputeGenerationSignature
Computes generation signature from configuration parameters.

---

## Client-Side Architecture

### WorldMapController

**File:** [`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs)

**Status:** ⚠️ Not Found (needs implementation)

**Description:** Client-side world map controller for rendering and user interaction.

### Required Features

#### Map Rendering
- **Chunk Rendering**: Render world map chunks
- **Zoom/Pan**: Support zoom and pan controls
- **Biome Colors**: Display biome-specific colors
- **Terrain Height**: Display terrain height information

#### Profile Management
- **Load Profile**: Load server-provided profile
- **Apply Settings**: Apply profile settings to map
- **Update Settings**: Send profile updates to server
- **Cache Profile**: Cache profile for offline use

#### User Interface
- **Map Toggle**: Toggle map visibility
- **Coordinate Display**: Show/hide coordinates
- **Biome Info**: Show/hide biome information
- **Settings Menu**: Access map settings

#### Network Integration
- **Request Initial Map**: Request initial map data
- **Request Chunk Updates**: Request chunk updates
- **Send Profile Updates**: Send profile changes to server
- **Handle Responses**: Process server responses

---

## Configuration Synchronization

### Profile Hash Computation

The profile hash is computed from all profile parameters to ensure server and client are synchronized.

**Parameters Included:**
- World name
- World seed
- Profile version
- Chunk size
- World height
- Render distance
- Simulation distance
- Global water level
- Sea level
- Hydrology parameters
- Cave parameters
- Lake parameters

### Generation Signature

The generation signature includes all parameters that affect terrain generation:

```
{WorldName}:{Seed}:{ProfileVersion}:{ProfileHash}:{Version}:{ChunkSize}:{WorldHeight}:{RenderDistance}:{SimulationDistance}:{GlobalWaterLevel}:{SeaLevel}:{HydrologyFlowPersistence}:{HydrologyWatershedStitchWeight}:{GradientStabilityIterations}:{GradientStabilityBlend}:{GradientClamp}:{FlowSeepageWeight}:{CeilingMoistureWeight}:{CeilingMoistureClamp}:{HydrologyEdgeBlendRadius}:{HydrologyEdgeVarianceClamp}:{HydrologyEdgeNormalizationBlend}:{HydrologyEdgeNormalizationIterations}:{HydrologyFlowMemoryWeight}:{RiverMeanderJitter}:{VarianceWeight}:{OutflowStabilityWeight}:{HydrologyFlowShadowWeight}:{HydrologyFlowShadowSlopeWeight}:{WetlandBufferRadius}:{LakeInflowBlendWeight}
```

### Hot-Reload Triggers

The configuration is reloaded when:
1. **world.json** write time changes
2. **world_map_control_profile.json** write time changes
3. **Profile hash** does not match loaded profile
4. **Profile version** is newer than loaded profile

---

## Data Structures

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
    public string ControlProfileHash { get; set; } = string.Empty;
    public string GenerationSignature { get; set; } = string.Empty;
}
```

### WorldMapData

```csharp
public sealed class WorldMapData
{
    public List<ChunkData>? Chunks { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = new();
}
```

### WorldMapProfile

```csharp
public sealed class WorldMapProfile
{
    public int PlayerId { get; set; }
    public int RenderDistance { get; set; }
    public double MapScale { get; set; }
    public bool ShowCoordinates { get; set; }
    public bool ShowBiomeInfo { get; set; }
    public int TerrainQuality { get; set; }
    public int WaterQuality { get; set; }
    public int VegetationQuality { get; set; }
    public PlayerPosition LastPosition { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
}
```

---

## Architecture Improvements Needed

### Server-Side Improvements

✅ **Completed:**
- Profile-based configuration
- Hot-reload functionality
- Generation signature computation
- Profile hash validation
- Chunk caching

⏳ **Needed:**
- Profile persistence to database
- Profile change history
- Profile validation
- Profile migration support
- Profile export/import

### Client-Side Improvements

⏳ **Needed:**
- Implement WorldMapController
- Map rendering system
- Profile management UI
- Map settings UI
- Network integration
- Cache management
- Offline map support

---

## Configuration Files

### world.json

**Location:** `config/world.json`  
**Purpose:** World generation configuration  
**Hot-Reload:** ✅ Supported

### world_map_control_profile.json

**Location:** `config/world_map_control_profile.json`  
**Purpose:** World map control profile  
**Hot-Reload:** ✅ Supported

### client-config.json

**Location:** `Assets/StreamingAssets/client-config.json`  
**Purpose:** Client configuration  
**Hot-Reload:** ⏳ Not yet supported

---

## Summary

### Server-Side Status

| Feature | Status |
|----------|--------|
| Profile-Based Configuration | ✅ Implemented |
| Hot-Reload Functionality | ✅ Implemented |
| Generation Signature | ✅ Implemented |
| Profile Hash Validation | ✅ Implemented |
| Chunk Caching | ✅ Implemented |
| Profile Persistence | ⏳ Needed |
| Profile Validation | ⏳ Needed |
| Profile Migration | ⏳ Needed |

### Client-Side Status

| Feature | Status |
|----------|--------|
| WorldMapController | ⏳ Not Implemented |
| Map Rendering | ⏳ Not Implemented |
| Profile Management | ⏳ Not Implemented |
| Network Integration | ⏳ Not Implemented |
| Cache Management | ⏳ Not Implemented |

### Overall Assessment

**Server-Side:** ✅ Well-implemented with hot-reload and profile management  
**Client-Side:** ⚠️ Needs implementation of WorldMapController

### Next Steps

1. Implement client-side WorldMapController
2. Add map rendering system
3. Implement profile management UI
4. Add map settings UI
5. Implement network integration
6. Add cache management
7. Add offline map support

---

**Last Updated:** 2026-01-10  
**Version:** 1.0.0
**Date:** 2026-01-10  
**Status:** Completed Review

## Overview

This document reviews the world map control architecture for both server and client. The architecture provides profile-based configuration, hot-reload functionality, and synchronization between server and client.

---

## Server-Side Architecture

### WorldMapControlManager

**File:** [`GameServer/World/WorldMapControlManager.cs`](../GameServer/World/WorldMapControlManager.cs)

**Status:** ✅ Well-implemented

**Description:** Lightweight world map control service that reuses enhanced terrain pipeline to generate preview chunks and track per-player map preferences.

### Key Features

#### Profile-Based Configuration
- **Control Profile**: Server-wide world map control profile
- **Player Profiles**: Per-player map preferences (render distance, map scale, etc.)
- **Profile Hash**: Hash-based profile validation for synchronization
- **Version Tracking**: Profile version tracking for compatibility

#### Hot-Reload Functionality
- **Config File Monitoring**: Monitors world.json for changes
- **Profile File Monitoring**: Monitors world_map_control_profile.json for changes
- **Automatic Reload**: Automatically reloads configuration when files change
- **Cache Invalidation**: Clears chunk cache when configuration changes

#### Generation Signature
- **Seed-Based Signature**: Includes world seed in generation signature
- **Config-Based Signature**: Includes all relevant configuration parameters
- **Profile Hash**: Includes profile hash for validation
- **Version Tracking**: Includes profile version for compatibility

### Request Types

| Type | Description |
|-------|-------------|
| `GetInitialMap` | Returns initial map data for a player |
| `UpdateChunk` | Updates specific chunks for a player |
| `GetPlayerProfile` | Returns player profile |
| `UpdatePlayerProfile` | Updates player profile |

### Profile Updates

| Type | Description |
|-------|-------------|
| `RenderDistance` | Updates render distance (2 to unload distance) |
| `MapScale` | Updates map scale (0.25 to 8.0) |
| `ShowCoordinates` | Toggles coordinate display |
| `ShowBiomeInfo` | Toggles biome info display |

### Configuration Files

| File | Description |
|-------|-------------|
| `config/world.json` | World generation configuration |
| `config/world_map_control_profile.json` | World map control profile |

### Key Methods

#### HandleAsync
Handles incoming world map requests and routes to appropriate handler.

#### EnsureProfile
Ensures profile is loaded and up-to-date. Handles:
- Config file changes
- Profile file changes
- Profile hash drift
- Version mismatches

#### MaybeReloadGenerationConfig
Reloads generation configuration if world.json has changed.

#### GenerateOrGetChunkAsync
Generates or retrieves cached chunk data.

#### ComputeGenerationSignature
Computes generation signature from configuration parameters.

---

## Client-Side Architecture

### WorldMapController

**File:** [`Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs`](../Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs)

**Status:** ⚠️ Not Found (needs implementation)

**Description:** Client-side world map controller for rendering and user interaction.

### Required Features

#### Map Rendering
- **Chunk Rendering**: Render world map chunks
- **Zoom/Pan**: Support zoom and pan controls
- **Biome Colors**: Display biome-specific colors
- **Terrain Height**: Display terrain height information

#### Profile Management
- **Load Profile**: Load server-provided profile
- **Apply Settings**: Apply profile settings to map
- **Update Settings**: Send profile updates to server
- **Cache Profile**: Cache profile for offline use

#### User Interface
- **Map Toggle**: Toggle map visibility
- **Coordinate Display**: Show/hide coordinates
- **Biome Info**: Show/hide biome information
- **Settings Menu**: Access map settings

#### Network Integration
- **Request Initial Map**: Request initial map data
- **Request Chunk Updates**: Request chunk updates
- **Send Profile Updates**: Send profile changes to server
- **Handle Responses**: Process server responses

---

## Configuration Synchronization

### Profile Hash Computation

The profile hash is computed from all profile parameters to ensure server and client are synchronized.

**Parameters Included:**
- World name
- World seed
- Profile version
- Chunk size
- World height
- Render distance
- Simulation distance
- Global water level
- Sea level
- Hydrology parameters
- Cave parameters
- Lake parameters

### Generation Signature

The generation signature includes all parameters that affect terrain generation:

```
{WorldName}:{Seed}:{ProfileVersion}:{ProfileHash}:{Version}:{ChunkSize}:{WorldHeight}:{RenderDistance}:{SimulationDistance}:{GlobalWaterLevel}:{SeaLevel}:{HydrologyFlowPersistence}:{HydrologyWatershedStitchWeight}:{GradientStabilityIterations}:{GradientStabilityBlend}:{GradientClamp}:{FlowSeepageWeight}:{CeilingMoistureWeight}:{CeilingMoistureClamp}:{HydrologyEdgeBlendRadius}:{HydrologyEdgeVarianceClamp}:{HydrologyEdgeNormalizationBlend}:{HydrologyEdgeNormalizationIterations}:{HydrologyFlowMemoryWeight}:{RiverMeanderJitter}:{VarianceWeight}:{OutflowStabilityWeight}:{HydrologyFlowShadowWeight}:{HydrologyFlowShadowSlopeWeight}:{WetlandBufferRadius}:{LakeInflowBlendWeight}
```

### Hot-Reload Triggers

The configuration is reloaded when:
1. **world.json** write time changes
2. **world_map_control_profile.json** write time changes
3. **Profile hash** does not match loaded profile
4. **Profile version** is newer than loaded profile

---

## Data Structures

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
    public string ControlProfileHash { get; set; } = string.Empty;
    public string GenerationSignature { get; set; } = string.Empty;
}
```

### WorldMapData

```csharp
public sealed class WorldMapData
{
    public List<ChunkData>? Chunks { get; set; }
    public PlayerPosition PlayerPosition { get; set; } = new();
}
```

### WorldMapProfile

```csharp
public sealed class WorldMapProfile
{
    public int PlayerId { get; set; }
    public int RenderDistance { get; set; }
    public double MapScale { get; set; }
    public bool ShowCoordinates { get; set; }
    public bool ShowBiomeInfo { get; set; }
    public int TerrainQuality { get; set; }
    public int WaterQuality { get; set; }
    public int VegetationQuality { get; set; }
    public PlayerPosition LastPosition { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
}
```

---

## Architecture Improvements Needed

### Server-Side Improvements

✅ **Completed:**
- Profile-based configuration
- Hot-reload functionality
- Generation signature computation
- Profile hash validation
- Chunk caching

⏳ **Needed:**
- Profile persistence to database
- Profile change history
- Profile validation
- Profile migration support
- Profile export/import

### Client-Side Improvements

⏳ **Needed:**
- Implement WorldMapController
- Map rendering system
- Profile management UI
- Map settings UI
- Network integration
- Cache management
- Offline map support

---

## Configuration Files

### world.json

**Location:** `config/world.json`  
**Purpose:** World generation configuration  
**Hot-Reload:** ✅ Supported

### world_map_control_profile.json

**Location:** `config/world_map_control_profile.json`  
**Purpose:** World map control profile  
**Hot-Reload:** ✅ Supported

### client-config.json

**Location:** `Assets/StreamingAssets/client-config.json`  
**Purpose:** Client configuration  
**Hot-Reload:** ⏳ Not yet supported

---

## Summary

### Server-Side Status

| Feature | Status |
|----------|--------|
| Profile-Based Configuration | ✅ Implemented |
| Hot-Reload Functionality | ✅ Implemented |
| Generation Signature | ✅ Implemented |
| Profile Hash Validation | ✅ Implemented |
| Chunk Caching | ✅ Implemented |
| Profile Persistence | ⏳ Needed |
| Profile Validation | ⏳ Needed |
| Profile Migration | ⏳ Needed |

### Client-Side Status

| Feature | Status |
|----------|--------|
| WorldMapController | ⏳ Not Implemented |
| Map Rendering | ⏳ Not Implemented |
| Profile Management | ⏳ Not Implemented |
| Network Integration | ⏳ Not Implemented |
| Cache Management | ⏳ Not Implemented |

### Overall Assessment

**Server-Side:** ✅ Well-implemented with hot-reload and profile management  
**Client-Side:** ⚠️ Needs implementation of WorldMapController

### Next Steps

1. Implement client-side WorldMapController
2. Add map rendering system
3. Implement profile management UI
4. Add map settings UI
5. Implement network integration
6. Add cache management
7. Add offline map support

---

**Last Updated:** 2026-01-10  
**Version:** 1.0.0

