# Shared DLL Architecture Analysis

**Date**: 2026-02-05
**Session**: 46
**Status**: Analysis Phase

## Current Architecture

### Project Structure

```
SharedProtocol/ (net6.0)
  ├── ProtocolRegistry.cs
  ├── EnhancedMinecraft/ProtocolRegistry.cs
  └── Generated Protobuf files

GameCommon/ (netstandard2.1)
  ├── World/WorldMapControlProfile.cs
  ├── World/WorldMapControlManager.cs
  └── Shared game logic

GameServer/ (net6.0)
  ├── References SharedProtocol ✓
  ├── References GameCommon ✓
  └── Server implementation
```

## Issues Identified

### 1. Framework Mismatch

| Project | Target Framework | Purpose |
|---------|------------------|---------|
| SharedProtocol | net6.0 | Protocol definitions, protobuf |
| GameCommon | netstandard2.1 | Shared game logic (Unity compatible) |
| GameServer | net6.0 | Server implementation |

**Problem**: GameCommon targets `netstandard2.1` for Unity 6 compatibility, while SharedProtocol targets `net6.0`. This creates a compatibility issue when GameCommon needs to reference SharedProtocol types.

### 2. Missing Reference

**Problem**: GameCommon does not reference SharedProtocol, which means:
- Cannot use common enums from protocol definitions
- Cannot use shared types defined in SharedProtocol
- Potential code duplication across projects

### 3. Circular Dependency Risk

If GameCommon references SharedProtocol and SharedProtocol needs GameCommon types, we'd have a circular dependency.

## Proposed Solutions

### Option 1: Unify Frameworks (Recommended)

Change all projects to target `net6.0`:

**Pros**:
- Simple and straightforward
- Full .NET 6 features available
- No framework compatibility issues

**Cons**:
- Unity 6 may not fully support .NET 6 assemblies yet
- Need to verify Unity compatibility

**Implementation**:
```xml
<!-- GameCommon/GameCommon.csproj -->
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <LangVersion>9.0</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
</ItemGroup>
```

### Option 2: Create SharedTypes Project

Create a new `SharedTypes` project targeting `netstandard2.1`:

```
SharedTypes/ (netstandard2.1)
  ├── Common enums
  ├── Shared types
  └── No protobuf dependencies

SharedProtocol/ (net6.0)
  ├── References SharedTypes
  └── Protocol definitions

GameCommon/ (netstandard2.1)
  ├── References SharedTypes
  └── Shared game logic
```

**Pros**:
- Maintains Unity compatibility
- Clear separation of concerns
- No circular dependencies

**Cons**:
- More complex project structure
- Additional maintenance overhead

### Option 3: Multi-Targeting

Multi-target SharedProtocol for both `net6.0` and `netstandard2.1`:

```xml
<PropertyGroup>
  <TargetFrameworks>net6.0;netstandard2.1</TargetFrameworks>
</PropertyGroup>
```

**Pros**:
- Supports both server and Unity
- Single project for protocol definitions

**Cons**:
- More complex build configuration
- Need to ensure compatibility across both targets

## Recommendation

**Option 1 (Unify Frameworks)** is recommended for the following reasons:

1. **Simplicity**: Single framework across all projects reduces complexity
2. **Future-proof**: .NET 6 is the current LTS version
3. **Unity 6 Support**: Unity 6 (6000.0.23f1) has improved .NET support and may work with .NET 6 assemblies
4. **Performance**: .NET 6 offers better performance than .NET Standard 2.1

## Implementation Plan

### Phase 1: Framework Unification
1. Change GameCommon to target `net6.0`
2. Add project reference from GameCommon to SharedProtocol
3. Test compilation

### Phase 2: Verify Unity Compatibility
1. Test Unity 6 with .NET 6 assemblies
2. Verify plugin distribution
3. Test client-server communication

### Phase 3: Migrate Shared Types
1. Move common enums to SharedProtocol
2. Update GameCommon to use SharedProtocol types
3. Remove duplicate code

### Phase 4: Testing
1. Run server compilation tests
2. Run Unity client tests
3. Verify protocol handling

## Next Steps

1. Verify Unity 6 .NET 6 assembly compatibility
2. Implement framework unification
3. Test and validate changes
4. Update documentation

## References

- Unity 6 .NET Support: https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html
- .NET 6 Documentation: https://docs.microsoft.com/dotnet/core/

**Date**: 2026-02-05
**Session**: 46
**Status**: Analysis Phase

## Current Architecture

### Project Structure

```
SharedProtocol/ (net6.0)
  ├── ProtocolRegistry.cs
  ├── EnhancedMinecraft/ProtocolRegistry.cs
  └── Generated Protobuf files

GameCommon/ (netstandard2.1)
  ├── World/WorldMapControlProfile.cs
  ├── World/WorldMapControlManager.cs
  └── Shared game logic

GameServer/ (net6.0)
  ├── References SharedProtocol ✓
  ├── References GameCommon ✓
  └── Server implementation
```

## Issues Identified

### 1. Framework Mismatch

| Project | Target Framework | Purpose |
|---------|------------------|---------|
| SharedProtocol | net6.0 | Protocol definitions, protobuf |
| GameCommon | netstandard2.1 | Shared game logic (Unity compatible) |
| GameServer | net6.0 | Server implementation |

**Problem**: GameCommon targets `netstandard2.1` for Unity 6 compatibility, while SharedProtocol targets `net6.0`. This creates a compatibility issue when GameCommon needs to reference SharedProtocol types.

### 2. Missing Reference

**Problem**: GameCommon does not reference SharedProtocol, which means:
- Cannot use common enums from protocol definitions
- Cannot use shared types defined in SharedProtocol
- Potential code duplication across projects

### 3. Circular Dependency Risk

If GameCommon references SharedProtocol and SharedProtocol needs GameCommon types, we'd have a circular dependency.

## Proposed Solutions

### Option 1: Unify Frameworks (Recommended)

Change all projects to target `net6.0`:

**Pros**:
- Simple and straightforward
- Full .NET 6 features available
- No framework compatibility issues

**Cons**:
- Unity 6 may not fully support .NET 6 assemblies yet
- Need to verify Unity compatibility

**Implementation**:
```xml
<!-- GameCommon/GameCommon.csproj -->
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <LangVersion>9.0</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
</ItemGroup>
```

### Option 2: Create SharedTypes Project

Create a new `SharedTypes` project targeting `netstandard2.1`:

```
SharedTypes/ (netstandard2.1)
  ├── Common enums
  ├── Shared types
  └── No protobuf dependencies

SharedProtocol/ (net6.0)
  ├── References SharedTypes
  └── Protocol definitions

GameCommon/ (netstandard2.1)
  ├── References SharedTypes
  └── Shared game logic
```

**Pros**:
- Maintains Unity compatibility
- Clear separation of concerns
- No circular dependencies

**Cons**:
- More complex project structure
- Additional maintenance overhead

### Option 3: Multi-Targeting

Multi-target SharedProtocol for both `net6.0` and `netstandard2.1`:

```xml
<PropertyGroup>
  <TargetFrameworks>net6.0;netstandard2.1</TargetFrameworks>
</PropertyGroup>
```

**Pros**:
- Supports both server and Unity
- Single project for protocol definitions

**Cons**:
- More complex build configuration
- Need to ensure compatibility across both targets

## Recommendation

**Option 1 (Unify Frameworks)** is recommended for the following reasons:

1. **Simplicity**: Single framework across all projects reduces complexity
2. **Future-proof**: .NET 6 is the current LTS version
3. **Unity 6 Support**: Unity 6 (6000.0.23f1) has improved .NET support and may work with .NET 6 assemblies
4. **Performance**: .NET 6 offers better performance than .NET Standard 2.1

## Implementation Plan

### Phase 1: Framework Unification
1. Change GameCommon to target `net6.0`
2. Add project reference from GameCommon to SharedProtocol
3. Test compilation

### Phase 2: Verify Unity Compatibility
1. Test Unity 6 with .NET 6 assemblies
2. Verify plugin distribution
3. Test client-server communication

### Phase 3: Migrate Shared Types
1. Move common enums to SharedProtocol
2. Update GameCommon to use SharedProtocol types
3. Remove duplicate code

### Phase 4: Testing
1. Run server compilation tests
2. Run Unity client tests
3. Verify protocol handling

## Next Steps

1. Verify Unity 6 .NET 6 assembly compatibility
2. Implement framework unification
3. Test and validate changes
4. Update documentation

## References

- Unity 6 .NET Support: https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html
- .NET 6 Documentation: https://docs.microsoft.com/dotnet/core/

**Date**: 2026-02-05
**Session**: 46
**Status**: Analysis Phase

## Current Architecture

### Project Structure

```
SharedProtocol/ (net6.0)
  ├── ProtocolRegistry.cs
  ├── EnhancedMinecraft/ProtocolRegistry.cs
  └── Generated Protobuf files

GameCommon/ (netstandard2.1)
  ├── World/WorldMapControlProfile.cs
  ├── World/WorldMapControlManager.cs
  └── Shared game logic

GameServer/ (net6.0)
  ├── References SharedProtocol ✓
  ├── References GameCommon ✓
  └── Server implementation
```

## Issues Identified

### 1. Framework Mismatch

| Project | Target Framework | Purpose |
|---------|------------------|---------|
| SharedProtocol | net6.0 | Protocol definitions, protobuf |
| GameCommon | netstandard2.1 | Shared game logic (Unity compatible) |
| GameServer | net6.0 | Server implementation |

**Problem**: GameCommon targets `netstandard2.1` for Unity 6 compatibility, while SharedProtocol targets `net6.0`. This creates a compatibility issue when GameCommon needs to reference SharedProtocol types.

### 2. Missing Reference

**Problem**: GameCommon does not reference SharedProtocol, which means:
- Cannot use common enums from protocol definitions
- Cannot use shared types defined in SharedProtocol
- Potential code duplication across projects

### 3. Circular Dependency Risk

If GameCommon references SharedProtocol and SharedProtocol needs GameCommon types, we'd have a circular dependency.

## Proposed Solutions

### Option 1: Unify Frameworks (Recommended)

Change all projects to target `net6.0`:

**Pros**:
- Simple and straightforward
- Full .NET 6 features available
- No framework compatibility issues

**Cons**:
- Unity 6 may not fully support .NET 6 assemblies yet
- Need to verify Unity compatibility

**Implementation**:
```xml
<!-- GameCommon/GameCommon.csproj -->
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <LangVersion>9.0</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
</ItemGroup>
```

### Option 2: Create SharedTypes Project

Create a new `SharedTypes` project targeting `netstandard2.1`:

```
SharedTypes/ (netstandard2.1)
  ├── Common enums
  ├── Shared types
  └── No protobuf dependencies

SharedProtocol/ (net6.0)
  ├── References SharedTypes
  └── Protocol definitions

GameCommon/ (netstandard2.1)
  ├── References SharedTypes
  └── Shared game logic
```

**Pros**:
- Maintains Unity compatibility
- Clear separation of concerns
- No circular dependencies

**Cons**:
- More complex project structure
- Additional maintenance overhead

### Option 3: Multi-Targeting

Multi-target SharedProtocol for both `net6.0` and `netstandard2.1`:

```xml
<PropertyGroup>
  <TargetFrameworks>net6.0;netstandard2.1</TargetFrameworks>
</PropertyGroup>
```

**Pros**:
- Supports both server and Unity
- Single project for protocol definitions

**Cons**:
- More complex build configuration
- Need to ensure compatibility across both targets

## Recommendation

**Option 1 (Unify Frameworks)** is recommended for the following reasons:

1. **Simplicity**: Single framework across all projects reduces complexity
2. **Future-proof**: .NET 6 is the current LTS version
3. **Unity 6 Support**: Unity 6 (6000.0.23f1) has improved .NET support and may work with .NET 6 assemblies
4. **Performance**: .NET 6 offers better performance than .NET Standard 2.1

## Implementation Plan

### Phase 1: Framework Unification
1. Change GameCommon to target `net6.0`
2. Add project reference from GameCommon to SharedProtocol
3. Test compilation

### Phase 2: Verify Unity Compatibility
1. Test Unity 6 with .NET 6 assemblies
2. Verify plugin distribution
3. Test client-server communication

### Phase 3: Migrate Shared Types
1. Move common enums to SharedProtocol
2. Update GameCommon to use SharedProtocol types
3. Remove duplicate code

### Phase 4: Testing
1. Run server compilation tests
2. Run Unity client tests
3. Verify protocol handling

## Next Steps

1. Verify Unity 6 .NET 6 assembly compatibility
2. Implement framework unification
3. Test and validate changes
4. Update documentation

## References

- Unity 6 .NET Support: https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html
- .NET 6 Documentation: https://docs.microsoft.com/dotnet/core/

