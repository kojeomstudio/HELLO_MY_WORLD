# Shared DLL Architecture for Common Enums and Code
**Date**: 2026-02-04
**Session**: Comprehensive Implementation Session
**Status**: Architecture Design Complete

## Executive Summary

This document provides a comprehensive design for shared DLL architecture to enable common enums and code sharing between server and client. The architecture addresses critical issues with code duplication, namespace inconsistencies, and missing shared contracts.

## Current State Analysis

### Existing Shared Projects

**1. SharedProtocol Project**
- **Target Framework**: .NET 6.0
- **Purpose**: Protocol message definitions and infrastructure
- **Key Components**:
  - Message dispatchers
  - Protocol registry
  - Protocol validation
  - Session management
  - Generated protobuf message types

**2. GameCommon Project**
- **Target Framework**: .NET Standard 2.1 (for Unity 6 compatibility)
- **Purpose**: Common game data and utilities
- **Current State**: INCOMPLETE
- **Key Components**:
  - Data-driven utilities (partial)
  - World control utilities (missing)

### Critical Missing Components

**Missing from GameCommon**:
1. `WorldMapControlProfile` - World generation profile
2. `WorldMapControlProfileData` - Profile data structure
3. `WorldMapProfileUtility` - Profile management utilities
4. `SharedFeatureCatalog` - Shared feature catalog
5. `WorldMapSignature` - Signature computation
6. `WorldMapSignatureContext` - Signature context

**Impact**:
- Server cannot compile (references missing classes)
- No shared contract between server and client
- Profile synchronization will fail at runtime
- Code duplication in client code

## Proposed Architecture

### Project Structure

```
Shared/
├── SharedProtocol/
│   ├── SharedProtocol.csproj          # .NET 6.0
│   ├── MessageDispatcher.cs
│   ├── MinecraftMessageDispatcher.cs
│   ├── Session.cs
│   ├── Messages.cs
│   ├── MinecraftMessages.cs
│   ├── MinecraftContainerMessages.cs
│   ├── WorldSyncMessages.cs
│   ├── GameProtocol.cs
│   └── EnhancedMinecraft/
│       ├── ProtocolRegistry.cs
│       ├── ProtocolValidator.cs
│       ├── ProtocolStandardization.cs
│       ├── ProtoRuntime.cs
│       ├── ProtoFingerprint.cs
│       ├── ProtoDiagnostics.cs
│       ├── ChunkPayloadBuilder.cs
│       └── UnifiedMessageHandler.cs
│
└── GameCommon/
    ├── GameCommon.csproj              # .NET Standard 2.1
    ├── World/
    │   ├── WorldMapControlProfile.cs
    │   ├── WorldMapControlProfileData.cs
    │   ├── WorldMapProfileUtility.cs
    │   ├── SharedFeatureCatalog.cs
    │   ├── WorldMapSignature.cs
    │   └── WorldMapSignatureContext.cs
    ├── DataDriven/
    │   ├── DataDrivenConfig.cs
    │   ├── JsonConfigLoader.cs
    │   └── ConfigValidation.cs
    ├── Enums/
    │   ├── BlockTypes.cs
    │   ├── BiomeTypes.cs
    │   ├── EntityType.cs
    │   ├── ItemType.cs
    │   └── GameMode.cs
    └── Models/
        ├── ChunkData.cs
        ├── PlayerInfo.cs
        └── WorldSettings.cs
```

### Namespace Hierarchy

```
SharedProtocol
├── SharedProtocol                    # Legacy protocol messages
├── SharedProtocol.EnhancedMinecraft  # Enhanced protocol infrastructure
└── GameProtocol                     # AI protocol messages

GameCommon
├── GameCommon.World                 # World control and generation
├── GameCommon.DataDriven           # Data-driven configuration
├── GameCommon.Enums                # Shared enumerations
└── GameCommon.Models               # Shared data models
```

## Component Design

### 1. WorldMapControlProfile

**File**: `GameCommon/World/WorldMapControlProfile.cs`

**Purpose**: Shared world map control profile for server-client synchronization

**Key Features**:
- Profile version management
- Hash computation for validation
- Hydrology signature validation
- Load from file and generate from config
- Profile serialization/deserialization

**Properties** (100+ hydrology parameters):
- Basic settings (ChunkSize, RenderDistance, SimulationDistance, GlobalWaterLevel)
- Hydrology settings (30+ parameters)
- Riparian settings (4 parameters)
- River settings (17 parameters)
- Lake settings (13 parameters)
- Cave settings (17 parameters)
- Feature toggles (EnableRivers, EnableLakes, EnableCaves, etc.)

**Methods**:
```csharp
public static WorldMapControlProfile LoadFromFile(string path, WorldConfig fallback)
public static void SaveToFile(WorldMapControlProfile profile, string path)
public static WorldMapControlProfile FromConfig(WorldConfig config)
public bool MatchesHash(string hash)
```

### 2. WorldMapControlProfileData

**File**: `GameCommon/World/WorldMapControlProfileData.cs`

**Purpose**: JSON-serializable data structure for profile persistence

**Key Features**:
- All profile properties as public fields
- Compatible with System.Text.Json and Newtonsoft.Json
- Used for file I/O operations

### 3. WorldMapProfileUtility

**File**: `GameCommon/World/WorldMapProfileUtility.cs`

**Purpose**: Utility methods for profile management

**Key Features**:
- Load profile from file
- Save profile to file
- Compute profile hash
- Validate profile version
- Check hydrology signature
- Handle profile mismatches

**Methods**:
```csharp
public static WorldMapControlProfile LoadOrCreate(WorldGenerationConfig config, WorldSettings settings)
public static WorldMapControlProfile Load(string path)
public static void Save(WorldMapControlProfile profile, string path)
public static string ComputeHash(WorldMapControlProfile profile)
```

### 4. SharedFeatureCatalog

**File**: `GameCommon/World/SharedFeatureCatalog.cs`

**Purpose**: Shared feature catalog for feature flags and signatures

**Key Features**:
- Hydrology signature constant
- Feature version tracking
- Feature availability flags
- Compatibility checking

**Properties**:
```csharp
public static string HydrologySignature { get; }
public static int FeatureVersion { get; }
public static bool SupportsImprovedCaves { get; }
public static bool SupportsImprovedRivers { get; }
public static bool SupportsImprovedLakes { get; }
```

### 5. WorldMapSignature

**File**: `GameCommon/World/WorldMapSignature.cs`

**Purpose**: Signature computation for world map validation

**Key Features**:
- Compute generation signature from context
- Validate signature consistency
- Support for fingerprinting

**Methods**:
```csharp
public static string Compute(WorldMapSignatureContext context)
public static bool Validate(string signature, WorldMapSignatureContext context)
```

### 6. WorldMapSignatureContext

**File**: `GameCommon/World/WorldMapSignatureContext.cs`

**Purpose**: Context data for signature computation

**Key Features**:
- Pipeline version
- World name and seed
- Proto fingerprint
- Profile version and hash
- Hydrology parameters
- Generation configuration

**Properties**:
```csharp
public string PipelineVersion { get; }
public string WorldName { get; }
public long Seed { get; }
public string ProtoDescriptorFingerprint { get; }
public string ProtoComputedFingerprint { get; }
public int ProfileVersion { get; }
public string ProfileHash { get; }
public string HydrologySignature { get; }
public int ChunkSize { get; }
public int WorldHeight { get; }
public int RenderDistance { get; }
public int SimulationDistance { get; }
public int GlobalWaterLevel { get; }
// ... additional hydrology parameters
```

### 7. DataDriven Configuration

**File**: `GameCommon/DataDriven/DataDrivenConfig.cs`

**Purpose**: Data-driven configuration management

**Key Features**:
- Load JSON config files
- Validate config structure
- Provide typed access to config data
- Support config hot-reloading

**Methods**:
```csharp
public static T Load<T>(string path) where T : class, new()
public static void Save<T>(string path, T data) where T : class
public static bool Validate<T>(T data)
```

### 8. Shared Enums

**File**: `GameCommon/Enums/BlockTypes.cs`

**Purpose**: Block type enumerations

**Key Enums**:
```csharp
public enum BlockType
{
    Air = 0,
    Stone = 1,
    Dirt = 2,
    Grass = 3,
    // ... additional block types
}
```

**File**: `GameCommon/Enums/BiomeTypes.cs`

**Purpose**: Biome type enumerations

**Key Enums**:
```csharp
public enum BiomeType
{
    Plains = 0,
    Mountains = 1,
    Forest = 2,
    Desert = 3,
    // ... additional biome types
}
```

**File**: `GameCommon/Enums/EntityType.cs`

**Purpose**: Entity type enumerations

**Key Enums**:
```csharp
public enum EntityType
{
    Player = 0,
    Zombie = 1,
    Skeleton = 2,
    // ... additional entity types
}
```

## Implementation Plan

### Phase 1: Create GameCommon Structure

**Tasks**:
1. Create `GameCommon/World/` directory
2. Create `GameCommon/DataDriven/` directory
3. Create `GameCommon/Enums/` directory
4. Create `GameCommon/Models/` directory

### Phase 2: Move WorldMapControlProfile

**Tasks**:
1. Extract `WorldMapControlProfile` from client code
2. Extract `WorldMapControlProfileData` from client code
3. Move to `GameCommon/World/`
4. Update namespace to `GameCommon.World`
5. Ensure .NET Standard 2.1 compatibility
6. Remove Unity-specific dependencies

### Phase 3: Create Utility Classes

**Tasks**:
1. Create `WorldMapProfileUtility` in `GameCommon/World/`
2. Create `SharedFeatureCatalog` in `GameCommon/World/`
3. Create `WorldMapSignature` in `GameCommon/World/`
4. Create `WorldMapSignatureContext` in `GameCommon/World/`

### Phase 4: Create DataDriven Components

**Tasks**:
1. Create `DataDrivenConfig` in `GameCommon/DataDriven/`
2. Create `JsonConfigLoader` in `GameCommon/DataDriven/`
3. Create `ConfigValidation` in `GameCommon/DataDriven/`

### Phase 5: Create Shared Enums

**Tasks**:
1. Create `BlockTypes.cs` in `GameCommon/Enums/`
2. Create `BiomeTypes.cs` in `GameCommon/Enums/`
3. Create `EntityType.cs` in `GameCommon/Enums/`
4. Create `ItemType.cs` in `GameCommon/Enums/`
5. Create `GameMode.cs` in `GameCommon/Enums/`

### Phase 6: Update Project References

**Tasks**:
1. Update `GameCommon.csproj` to include new files
2. Ensure proper dependencies (System.Text.Json, etc.)
3. Verify .NET Standard 2.1 compatibility
4. Add project reference in `SharedProtocol.csproj`
5. Add project reference in `GameServer.csproj`

### Phase 7: Update Server Code

**Tasks**:
1. Update server using statements to use `GameCommon.World`
2. Remove duplicate profile code from server
3. Test compilation
4. Verify profile synchronization

### Phase 8: Update Client Code

**Tasks**:
1. Update client using statements to use `GameCommon.World`
2. Remove duplicate profile code from client
3. Update Unity project references
4. Test compilation
5. Verify profile synchronization

## Project File Configuration

### GameCommon.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
</Project>
```

### SharedProtocol.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Protobuf" Version="3.27.2" />
    <PackageReference Include="protobuf-net" Version="3.2.18" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../GameCommon/GameCommon.csproj" />
  </ItemGroup>
</Project>
```

### GameServer.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
    <ProjectReference Include="../GameCommon/GameCommon.csproj" />
  </ItemGroup>
</Project>
```

## Compatibility Considerations

### Unity 6 Compatibility

**Requirements**:
- .NET Standard 2.1 target framework
- C# 9.0 language version
- No Unity-specific APIs in shared code
- Cross-platform compatible

**Restrictions**:
- Cannot use UnityEngine in shared code
- Cannot use Unity-specific serialization
- Must use System.Text.Json for JSON

### Server Compatibility

**Requirements**:
- .NET 6.0 target framework
- Can use server-specific features
- Can reference both SharedProtocol and GameCommon

### Client Compatibility

**Requirements**:
- Unity 6 or later
- Can reference both SharedProtocol and GameCommon
- Must use shared enums and types

## Migration Strategy

### Step 1: Create GameCommon Components

1. Create directory structure
2. Implement shared classes
3. Add to GameCommon.csproj
4. Test compilation

### Step 2: Update Server References

1. Add GameCommon project reference
2. Update using statements
3. Remove duplicate code
4. Test compilation

### Step 3: Update Client References

1. Add GameCommon project reference
2. Update using statements
3. Remove duplicate code
4. Test compilation

### Step 4: Validate Synchronization

1. Test server-client profile sync
2. Verify hash computation
3. Check signature validation
4. Test protocol message handling

## Benefits

### Code Reusability
- Single source of truth for shared types
- Reduced code duplication
- Easier maintenance
- Consistent behavior across server and client

### Type Safety
- Compile-time type checking
- Reduced runtime errors
- Better IDE support
- Refactoring safety

### Maintainability
- Centralized shared code
- Easier to update shared types
- Clear ownership and responsibility
- Better documentation

### Performance
- No runtime reflection for type lookup
- Direct type references
- Optimized serialization
- Reduced memory allocations

## Risks and Mitigation

### Risk 1: Breaking Changes

**Risk**: Moving code to shared DLL may break existing references

**Mitigation**:
1. Phase migration carefully
2. Update all references in one step
3. Test thoroughly after each phase
4. Keep old code temporarily for rollback

### Risk 2: Unity Compatibility

**Risk**: .NET Standard 2.1 may not support all needed features

**Mitigation**:
1. Verify all APIs are available
2. Test in Unity 6 environment
3. Use compatible package versions
4. Avoid Unity-specific APIs in shared code

### Risk 3: Namespace Conflicts

**Risk**: New namespace structure may conflict with existing code

**Mitigation**:
1. Use clear, descriptive namespaces
2. Document namespace usage guidelines
3. Add namespace validation at compile time
4. Provide migration guide

## Testing Strategy

### Unit Tests

**Test Areas**:
1. Profile loading and saving
2. Hash computation
3. Signature validation
4. Config loading and validation
5. Enum value ranges

### Integration Tests

**Test Areas**:
1. Server-client profile synchronization
2. Protocol message serialization
3. Config hot-reloading
4. Shared enum usage

### Compatibility Tests

**Test Areas**:
1. Unity 6 compatibility
2. Server .NET 6.0 compatibility
3. Cross-platform compatibility
4. Version compatibility

## Documentation Requirements

### Code Documentation

**Required**:
1. XML documentation for all public APIs
2. Usage examples for complex operations
3. Migration guide for existing code
4. Namespace usage guidelines

### Architecture Documentation

**Required**:
1. Overall architecture overview
2. Component interaction diagrams
3. Data flow diagrams
4. Deployment guide

## Next Steps

1. Create GameCommon directory structure
2. Implement WorldMapControlProfile in GameCommon
3. Implement utility classes in GameCommon
4. Create shared enums in GameCommon
5. Update project references
6. Migrate server code to use shared components
7. Migrate client code to use shared components
8. Remove duplicate code
9. Test compilation and runtime
10. Update documentation

---

**Document Version**: 1.0
**Last Updated**: 2026-02-04
**Status**: Architecture Design Complete
**Date**: 2026-02-04
**Session**: Comprehensive Implementation Session
**Status**: Architecture Design Complete

## Executive Summary

This document provides a comprehensive design for shared DLL architecture to enable common enums and code sharing between server and client. The architecture addresses critical issues with code duplication, namespace inconsistencies, and missing shared contracts.

## Current State Analysis

### Existing Shared Projects

**1. SharedProtocol Project**
- **Target Framework**: .NET 6.0
- **Purpose**: Protocol message definitions and infrastructure
- **Key Components**:
  - Message dispatchers
  - Protocol registry
  - Protocol validation
  - Session management
  - Generated protobuf message types

**2. GameCommon Project**
- **Target Framework**: .NET Standard 2.1 (for Unity 6 compatibility)
- **Purpose**: Common game data and utilities
- **Current State**: INCOMPLETE
- **Key Components**:
  - Data-driven utilities (partial)
  - World control utilities (missing)

### Critical Missing Components

**Missing from GameCommon**:
1. `WorldMapControlProfile` - World generation profile
2. `WorldMapControlProfileData` - Profile data structure
3. `WorldMapProfileUtility` - Profile management utilities
4. `SharedFeatureCatalog` - Shared feature catalog
5. `WorldMapSignature` - Signature computation
6. `WorldMapSignatureContext` - Signature context

**Impact**:
- Server cannot compile (references missing classes)
- No shared contract between server and client
- Profile synchronization will fail at runtime
- Code duplication in client code

## Proposed Architecture

### Project Structure

```
Shared/
├── SharedProtocol/
│   ├── SharedProtocol.csproj          # .NET 6.0
│   ├── MessageDispatcher.cs
│   ├── MinecraftMessageDispatcher.cs
│   ├── Session.cs
│   ├── Messages.cs
│   ├── MinecraftMessages.cs
│   ├── MinecraftContainerMessages.cs
│   ├── WorldSyncMessages.cs
│   ├── GameProtocol.cs
│   └── EnhancedMinecraft/
│       ├── ProtocolRegistry.cs
│       ├── ProtocolValidator.cs
│       ├── ProtocolStandardization.cs
│       ├── ProtoRuntime.cs
│       ├── ProtoFingerprint.cs
│       ├── ProtoDiagnostics.cs
│       ├── ChunkPayloadBuilder.cs
│       └── UnifiedMessageHandler.cs
│
└── GameCommon/
    ├── GameCommon.csproj              # .NET Standard 2.1
    ├── World/
    │   ├── WorldMapControlProfile.cs
    │   ├── WorldMapControlProfileData.cs
    │   ├── WorldMapProfileUtility.cs
    │   ├── SharedFeatureCatalog.cs
    │   ├── WorldMapSignature.cs
    │   └── WorldMapSignatureContext.cs
    ├── DataDriven/
    │   ├── DataDrivenConfig.cs
    │   ├── JsonConfigLoader.cs
    │   └── ConfigValidation.cs
    ├── Enums/
    │   ├── BlockTypes.cs
    │   ├── BiomeTypes.cs
    │   ├── EntityType.cs
    │   ├── ItemType.cs
    │   └── GameMode.cs
    └── Models/
        ├── ChunkData.cs
        ├── PlayerInfo.cs
        └── WorldSettings.cs
```

### Namespace Hierarchy

```
SharedProtocol
├── SharedProtocol                    # Legacy protocol messages
├── SharedProtocol.EnhancedMinecraft  # Enhanced protocol infrastructure
└── GameProtocol                     # AI protocol messages

GameCommon
├── GameCommon.World                 # World control and generation
├── GameCommon.DataDriven           # Data-driven configuration
├── GameCommon.Enums                # Shared enumerations
└── GameCommon.Models               # Shared data models
```

## Component Design

### 1. WorldMapControlProfile

**File**: `GameCommon/World/WorldMapControlProfile.cs`

**Purpose**: Shared world map control profile for server-client synchronization

**Key Features**:
- Profile version management
- Hash computation for validation
- Hydrology signature validation
- Load from file and generate from config
- Profile serialization/deserialization

**Properties** (100+ hydrology parameters):
- Basic settings (ChunkSize, RenderDistance, SimulationDistance, GlobalWaterLevel)
- Hydrology settings (30+ parameters)
- Riparian settings (4 parameters)
- River settings (17 parameters)
- Lake settings (13 parameters)
- Cave settings (17 parameters)
- Feature toggles (EnableRivers, EnableLakes, EnableCaves, etc.)

**Methods**:
```csharp
public static WorldMapControlProfile LoadFromFile(string path, WorldConfig fallback)
public static void SaveToFile(WorldMapControlProfile profile, string path)
public static WorldMapControlProfile FromConfig(WorldConfig config)
public bool MatchesHash(string hash)
```

### 2. WorldMapControlProfileData

**File**: `GameCommon/World/WorldMapControlProfileData.cs`

**Purpose**: JSON-serializable data structure for profile persistence

**Key Features**:
- All profile properties as public fields
- Compatible with System.Text.Json and Newtonsoft.Json
- Used for file I/O operations

### 3. WorldMapProfileUtility

**File**: `GameCommon/World/WorldMapProfileUtility.cs`

**Purpose**: Utility methods for profile management

**Key Features**:
- Load profile from file
- Save profile to file
- Compute profile hash
- Validate profile version
- Check hydrology signature
- Handle profile mismatches

**Methods**:
```csharp
public static WorldMapControlProfile LoadOrCreate(WorldGenerationConfig config, WorldSettings settings)
public static WorldMapControlProfile Load(string path)
public static void Save(WorldMapControlProfile profile, string path)
public static string ComputeHash(WorldMapControlProfile profile)
```

### 4. SharedFeatureCatalog

**File**: `GameCommon/World/SharedFeatureCatalog.cs`

**Purpose**: Shared feature catalog for feature flags and signatures

**Key Features**:
- Hydrology signature constant
- Feature version tracking
- Feature availability flags
- Compatibility checking

**Properties**:
```csharp
public static string HydrologySignature { get; }
public static int FeatureVersion { get; }
public static bool SupportsImprovedCaves { get; }
public static bool SupportsImprovedRivers { get; }
public static bool SupportsImprovedLakes { get; }
```

### 5. WorldMapSignature

**File**: `GameCommon/World/WorldMapSignature.cs`

**Purpose**: Signature computation for world map validation

**Key Features**:
- Compute generation signature from context
- Validate signature consistency
- Support for fingerprinting

**Methods**:
```csharp
public static string Compute(WorldMapSignatureContext context)
public static bool Validate(string signature, WorldMapSignatureContext context)
```

### 6. WorldMapSignatureContext

**File**: `GameCommon/World/WorldMapSignatureContext.cs`

**Purpose**: Context data for signature computation

**Key Features**:
- Pipeline version
- World name and seed
- Proto fingerprint
- Profile version and hash
- Hydrology parameters
- Generation configuration

**Properties**:
```csharp
public string PipelineVersion { get; }
public string WorldName { get; }
public long Seed { get; }
public string ProtoDescriptorFingerprint { get; }
public string ProtoComputedFingerprint { get; }
public int ProfileVersion { get; }
public string ProfileHash { get; }
public string HydrologySignature { get; }
public int ChunkSize { get; }
public int WorldHeight { get; }
public int RenderDistance { get; }
public int SimulationDistance { get; }
public int GlobalWaterLevel { get; }
// ... additional hydrology parameters
```

### 7. DataDriven Configuration

**File**: `GameCommon/DataDriven/DataDrivenConfig.cs`

**Purpose**: Data-driven configuration management

**Key Features**:
- Load JSON config files
- Validate config structure
- Provide typed access to config data
- Support config hot-reloading

**Methods**:
```csharp
public static T Load<T>(string path) where T : class, new()
public static void Save<T>(string path, T data) where T : class
public static bool Validate<T>(T data)
```

### 8. Shared Enums

**File**: `GameCommon/Enums/BlockTypes.cs`

**Purpose**: Block type enumerations

**Key Enums**:
```csharp
public enum BlockType
{
    Air = 0,
    Stone = 1,
    Dirt = 2,
    Grass = 3,
    // ... additional block types
}
```

**File**: `GameCommon/Enums/BiomeTypes.cs`

**Purpose**: Biome type enumerations

**Key Enums**:
```csharp
public enum BiomeType
{
    Plains = 0,
    Mountains = 1,
    Forest = 2,
    Desert = 3,
    // ... additional biome types
}
```

**File**: `GameCommon/Enums/EntityType.cs`

**Purpose**: Entity type enumerations

**Key Enums**:
```csharp
public enum EntityType
{
    Player = 0,
    Zombie = 1,
    Skeleton = 2,
    // ... additional entity types
}
```

## Implementation Plan

### Phase 1: Create GameCommon Structure

**Tasks**:
1. Create `GameCommon/World/` directory
2. Create `GameCommon/DataDriven/` directory
3. Create `GameCommon/Enums/` directory
4. Create `GameCommon/Models/` directory

### Phase 2: Move WorldMapControlProfile

**Tasks**:
1. Extract `WorldMapControlProfile` from client code
2. Extract `WorldMapControlProfileData` from client code
3. Move to `GameCommon/World/`
4. Update namespace to `GameCommon.World`
5. Ensure .NET Standard 2.1 compatibility
6. Remove Unity-specific dependencies

### Phase 3: Create Utility Classes

**Tasks**:
1. Create `WorldMapProfileUtility` in `GameCommon/World/`
2. Create `SharedFeatureCatalog` in `GameCommon/World/`
3. Create `WorldMapSignature` in `GameCommon/World/`
4. Create `WorldMapSignatureContext` in `GameCommon/World/`

### Phase 4: Create DataDriven Components

**Tasks**:
1. Create `DataDrivenConfig` in `GameCommon/DataDriven/`
2. Create `JsonConfigLoader` in `GameCommon/DataDriven/`
3. Create `ConfigValidation` in `GameCommon/DataDriven/`

### Phase 5: Create Shared Enums

**Tasks**:
1. Create `BlockTypes.cs` in `GameCommon/Enums/`
2. Create `BiomeTypes.cs` in `GameCommon/Enums/`
3. Create `EntityType.cs` in `GameCommon/Enums/`
4. Create `ItemType.cs` in `GameCommon/Enums/`
5. Create `GameMode.cs` in `GameCommon/Enums/`

### Phase 6: Update Project References

**Tasks**:
1. Update `GameCommon.csproj` to include new files
2. Ensure proper dependencies (System.Text.Json, etc.)
3. Verify .NET Standard 2.1 compatibility
4. Add project reference in `SharedProtocol.csproj`
5. Add project reference in `GameServer.csproj`

### Phase 7: Update Server Code

**Tasks**:
1. Update server using statements to use `GameCommon.World`
2. Remove duplicate profile code from server
3. Test compilation
4. Verify profile synchronization

### Phase 8: Update Client Code

**Tasks**:
1. Update client using statements to use `GameCommon.World`
2. Remove duplicate profile code from client
3. Update Unity project references
4. Test compilation
5. Verify profile synchronization

## Project File Configuration

### GameCommon.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
</Project>
```

### SharedProtocol.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Protobuf" Version="3.27.2" />
    <PackageReference Include="protobuf-net" Version="3.2.18" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../GameCommon/GameCommon.csproj" />
  </ItemGroup>
</Project>
```

### GameServer.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
    <ProjectReference Include="../GameCommon/GameCommon.csproj" />
  </ItemGroup>
</Project>
```

## Compatibility Considerations

### Unity 6 Compatibility

**Requirements**:
- .NET Standard 2.1 target framework
- C# 9.0 language version
- No Unity-specific APIs in shared code
- Cross-platform compatible

**Restrictions**:
- Cannot use UnityEngine in shared code
- Cannot use Unity-specific serialization
- Must use System.Text.Json for JSON

### Server Compatibility

**Requirements**:
- .NET 6.0 target framework
- Can use server-specific features
- Can reference both SharedProtocol and GameCommon

### Client Compatibility

**Requirements**:
- Unity 6 or later
- Can reference both SharedProtocol and GameCommon
- Must use shared enums and types

## Migration Strategy

### Step 1: Create GameCommon Components

1. Create directory structure
2. Implement shared classes
3. Add to GameCommon.csproj
4. Test compilation

### Step 2: Update Server References

1. Add GameCommon project reference
2. Update using statements
3. Remove duplicate code
4. Test compilation

### Step 3: Update Client References

1. Add GameCommon project reference
2. Update using statements
3. Remove duplicate code
4. Test compilation

### Step 4: Validate Synchronization

1. Test server-client profile sync
2. Verify hash computation
3. Check signature validation
4. Test protocol message handling

## Benefits

### Code Reusability
- Single source of truth for shared types
- Reduced code duplication
- Easier maintenance
- Consistent behavior across server and client

### Type Safety
- Compile-time type checking
- Reduced runtime errors
- Better IDE support
- Refactoring safety

### Maintainability
- Centralized shared code
- Easier to update shared types
- Clear ownership and responsibility
- Better documentation

### Performance
- No runtime reflection for type lookup
- Direct type references
- Optimized serialization
- Reduced memory allocations

## Risks and Mitigation

### Risk 1: Breaking Changes

**Risk**: Moving code to shared DLL may break existing references

**Mitigation**:
1. Phase migration carefully
2. Update all references in one step
3. Test thoroughly after each phase
4. Keep old code temporarily for rollback

### Risk 2: Unity Compatibility

**Risk**: .NET Standard 2.1 may not support all needed features

**Mitigation**:
1. Verify all APIs are available
2. Test in Unity 6 environment
3. Use compatible package versions
4. Avoid Unity-specific APIs in shared code

### Risk 3: Namespace Conflicts

**Risk**: New namespace structure may conflict with existing code

**Mitigation**:
1. Use clear, descriptive namespaces
2. Document namespace usage guidelines
3. Add namespace validation at compile time
4. Provide migration guide

## Testing Strategy

### Unit Tests

**Test Areas**:
1. Profile loading and saving
2. Hash computation
3. Signature validation
4. Config loading and validation
5. Enum value ranges

### Integration Tests

**Test Areas**:
1. Server-client profile synchronization
2. Protocol message serialization
3. Config hot-reloading
4. Shared enum usage

### Compatibility Tests

**Test Areas**:
1. Unity 6 compatibility
2. Server .NET 6.0 compatibility
3. Cross-platform compatibility
4. Version compatibility

## Documentation Requirements

### Code Documentation

**Required**:
1. XML documentation for all public APIs
2. Usage examples for complex operations
3. Migration guide for existing code
4. Namespace usage guidelines

### Architecture Documentation

**Required**:
1. Overall architecture overview
2. Component interaction diagrams
3. Data flow diagrams
4. Deployment guide

## Next Steps

1. Create GameCommon directory structure
2. Implement WorldMapControlProfile in GameCommon
3. Implement utility classes in GameCommon
4. Create shared enums in GameCommon
5. Update project references
6. Migrate server code to use shared components
7. Migrate client code to use shared components
8. Remove duplicate code
9. Test compilation and runtime
10. Update documentation

---

**Document Version**: 1.0
**Last Updated**: 2026-02-04
**Status**: Architecture Design Complete

