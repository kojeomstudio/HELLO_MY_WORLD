# Shared DLL Architecture - Session 116

## Overview

This document describes the shared DLL architecture for common enumerations and codes used across the Minecraft-like server and client implementation.

## SharedProtocol Project

**File**: [`SharedProtocol/SharedProtocol.csproj`](../SharedProtocol/SharedProtocol.csproj)

**Purpose**: Shared protocol library containing common types, enumerations, and protocol handling code used by both server and client.

**Target Framework**: .NET 6.0

## Project Structure

### Common Types

**File**: [`SharedProtocol/Common/MinecraftCommonTypes.cs`](../SharedProtocol/Common/MinecraftCommonTypes.cs)

**Purpose**: Common Minecraft-specific types used across server and client.

**Key Types**:

- `Vector2Int`: 2D integer vector for chunk coordinates
- `ChunkCoordinate`: Chunk coordinate wrapper
- `PlayerPosition`: Player position wrapper
- Various data structures for common operations

### Constants

**File**: [`SharedProtocol/Common/Constants/`](../SharedProtocol/Common/Constants/)

**Purpose**: Shared constants for game and network operations.

**Key Files**:

1. **[`GameConstants.cs`](../SharedProtocol/Common/Constants/GameConstants.cs)**
   - Game-related constants
   - Default values for game settings
   - Configuration defaults

2. **[`NetworkConstants.cs`](../SharedProtocol/Common/Constants/NetworkConstants.cs)**
   - Network-related constants
   - Protocol version numbers
   - Message type definitions
   - Buffer sizes and timeouts

3. **[`WorldConstants.cs`](../SharedProtocol/Common/Constants/WorldConstants.cs)**
   - World-related constants
   - Chunk dimensions
   - World height limits
   - Biome-related constants

### Enumerations

**Directory**: [`SharedProtocol/Common/Enums/`](../SharedProtocol/Common/Enums/)

**Purpose**: Shared enumerations used across server and client.

**Key Files**:

1. **[`BiomeEnums.cs`](../SharedProtocol/Common/Enums/BiomeEnums.cs)**
   - Biome type enumerations
   - Biome-related constants
   - Biome properties

2. **[`CombatEnums.cs`](../SharedProtocol/Common/Enums/CombatEnums.cs)**
   - Combat-related enumerations
   - Damage types
   - Combat states
   - Attack types

3. **[`CoreEnums.cs`](../SharedProtocol/Common/Enums/CoreEnums.cs)**
   - Core game enumerations
   - Game modes
   - Difficulty levels
   - Dimension types

4. **[`GameEnums.cs`](../SharedProtocol/Common/Enums/GameEnums.cs)**
   - Game-specific enumerations
   - Player states
   - Interaction types
   - Game events

5. **[`ItemEnums.cs`](../SharedProtocol/Common/Enums/ItemEnums.cs)**
   - Item-related enumerations
   - Item types
   - Tool types
   - Rarity levels

6. **[`WorldEnums.cs`](../SharedProtocol/Common/Enums/WorldEnums.cs)**
   - World-related enumerations
   - Block types
   - Weather types
   - Time of day

### Interfaces

**File**: [`SharedProtocol/Common/Interfaces/ISharedProtocol.cs`](../SharedProtocol/Common/Interfaces/ISharedProtocol.cs)

**Purpose**: Shared protocol interfaces.

**Key Interfaces**:

- `ISharedProtocol`: Main shared protocol interface
- Message handling interfaces
- Serialization interfaces

### Enhanced Minecraft Protocol

**Directory**: [`SharedProtocol/EnhancedMinecraft/`](../SharedProtocol/EnhancedMinecraft/)

**Purpose**: Enhanced Minecraft protocol utilities.

**Key Files**:

1. **[`ChunkPayloadBuilder.cs`](../SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs)**
   - Chunk payload building utilities
   - Chunk serialization
   - Chunk compression

2. **[`ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)**
   - Protocol message registry
   - Message type mappings
   - Message validation

3. **[`ProtocolStandardization.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs)**
   - Protocol standardization utilities
   - Message format standardization
   - Version compatibility

4. **[`ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)**
   - Protocol validation utilities
   - Message validation
   - Schema validation

5. **[`ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)**
   - Protocol diagnostics utilities
   - Error reporting
   - Performance monitoring

6. **[`ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)**
   - Protocol fingerprinting
   - Descriptor fingerprint
   - Protocol version verification

7. **[`ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs)**
   - Protocol runtime initialization
   - Runtime setup
   - Protocol initialization

8. **[`UnifiedMessageHandler.cs`](../SharedProtocol/EnhancedMinecraft/UnifiedMessageHandler.cs)**
   - Unified message handling
   - Message routing
   - Message processing

## Protocol Integration

### Generated Protobuf Code

The SharedProtocol project includes generated protobuf code:

```xml
<ItemGroup>
  <Compile Include="..\Assets\Generated\Protobuf\Common.cs">
    <Link>Generated\Common.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\EnhancedMinecraftGame.cs">
    <Link>Generated\EnhancedMinecraftGame.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameAuth.cs">
    <Link>Generated\GameAuth.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameChat.cs">
    <Link>Generated\GameChat.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameCore.cs">
    <Link>Generated\GameCore.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameDiag.cs">
    <Link>Generated\GameDiag.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameMove.cs">
    <Link>Generated\GameMove.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameWorld.cs">
    <Link>Generated\GameWorld.cs</Link>
  </Compile>
</ItemGroup>
```

### Dependencies

```xml
<PackageReference Include="Google.Protobuf" Version="3.27.2" />
<PackageReference Include="protobuf-net" Version="3.2.18" />
<PackageReference Include="Grpc.Tools" Version="2.64.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="System.Data.SQLite.Core" Version="1.0.118" />
```

## Server Integration

### GameServer Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
  <ProjectReference Include="../GameCommon/GameCommon.csproj" />
</ItemGroup>
```

### Usage in Server

Server code uses shared protocol:

```csharp
using SharedProtocol.EnhancedMinecraft;
using MinecraftGame.Common;
using GameCommon.World;
```

### Key Server Components

1. **[`GameServer/Handlers/`](../GameServer/Handlers/)** - Protocol handlers using shared types
2. **[`GameServer/World/`](../GameServer/World/)** - World management using shared types
3. **[`GameServer/Models/`](../GameServer/Models/)** - Data models using shared types

## Client Integration

### Unity Client Reference

The Unity client references the generated protobuf code directly:

```
Assets/Generated/Protobuf/
  - Common.cs
  - EnhancedMinecraftGame.cs
  - GameAuth.cs
  - GameChat.cs
  - GameCore.cs
  - GameDiag.cs
  - GameMove.cs
  - GameWorld.cs
```

### Usage in Client

Client code uses shared protocol:

```csharp
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
```

## Protocol Validation

### Fingerprinting

The protocol uses fingerprinting to ensure consistency:

```csharp
ProtoFingerprint.AssertDescriptorFingerprint();
ProtoFingerprint.ComputeFingerprint();
```

### Registry Validation

Protocol registry validates bindings:

```csharp
ProtocolRegistry.ValidateBindings();
```

### Runtime Initialization

Protocol runtime ensures initialization:

```csharp
ProtoRuntime.EnsureInitialized();
```

## Benefits of Shared DLL Architecture

### 1. Type Safety

- Strongly-typed enumerations
- Compile-time type checking
- IntelliSense support

### 2. Code Reuse

- Single source of truth for types
- Reduced code duplication
- Consistent behavior across components

### 3. Version Control

- Single version for protocol
- Easier to maintain compatibility
- Centralized version management

### 4. Performance

- Reduced memory footprint
- Faster compilation
- Better runtime performance

### 5. Maintainability

- Centralized protocol definitions
- Easier to update protocol
- Better code organization

## Best Practices

### 1. Protocol Design

- Use common types from SharedProtocol
- Define enums in SharedProtocol/Common/Enums/
- Keep protocol messages in proto files
- Generate protobuf code from proto definitions

### 2. Type Safety

- Use strongly-typed enums instead of magic numbers
- Use common types for data structures
- Validate protocol messages on both ends

### 3. Versioning

- Include version fields in protocol messages
- Use fingerprinting for protocol verification
- Support backward compatibility

### 4. Documentation

- Document all protocol messages
- Document enum values
- Document protocol changes

## Future Improvements

### 1. Protocol Enhancements

- Add protocol version negotiation
- Support protocol extensions
- Add protocol compression

### 2. Tooling

- Improve protobuf code generation
- Add protocol validation tools
- Add protocol documentation generators

### 3. Performance

- Optimize serialization performance
- Add message pooling
- Implement zero-copy where possible

## References

- [`SharedProtocol/SharedProtocol.csproj`](../SharedProtocol/SharedProtocol.csproj)
- [`SharedProtocol/Common/`](../SharedProtocol/Common/)
- [`SharedProtocol/Common/Enums/`](../SharedProtocol/Common/Enums/)
- [`SharedProtocol/EnhancedMinecraft/`](../SharedProtocol/EnhancedMinecraft/)
- [`GameServer/GameServer.csproj`](../GameServer/GameServer.csproj)
- [`proto/`](../proto/) - Protocol definition files

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Commit changes locally, push to origin branch

## Overview

This document describes the shared DLL architecture for common enumerations and codes used across the Minecraft-like server and client implementation.

## SharedProtocol Project

**File**: [`SharedProtocol/SharedProtocol.csproj`](../SharedProtocol/SharedProtocol.csproj)

**Purpose**: Shared protocol library containing common types, enumerations, and protocol handling code used by both server and client.

**Target Framework**: .NET 6.0

## Project Structure

### Common Types

**File**: [`SharedProtocol/Common/MinecraftCommonTypes.cs`](../SharedProtocol/Common/MinecraftCommonTypes.cs)

**Purpose**: Common Minecraft-specific types used across server and client.

**Key Types**:

- `Vector2Int`: 2D integer vector for chunk coordinates
- `ChunkCoordinate`: Chunk coordinate wrapper
- `PlayerPosition`: Player position wrapper
- Various data structures for common operations

### Constants

**File**: [`SharedProtocol/Common/Constants/`](../SharedProtocol/Common/Constants/)

**Purpose**: Shared constants for game and network operations.

**Key Files**:

1. **[`GameConstants.cs`](../SharedProtocol/Common/Constants/GameConstants.cs)**
   - Game-related constants
   - Default values for game settings
   - Configuration defaults

2. **[`NetworkConstants.cs`](../SharedProtocol/Common/Constants/NetworkConstants.cs)**
   - Network-related constants
   - Protocol version numbers
   - Message type definitions
   - Buffer sizes and timeouts

3. **[`WorldConstants.cs`](../SharedProtocol/Common/Constants/WorldConstants.cs)**
   - World-related constants
   - Chunk dimensions
   - World height limits
   - Biome-related constants

### Enumerations

**Directory**: [`SharedProtocol/Common/Enums/`](../SharedProtocol/Common/Enums/)

**Purpose**: Shared enumerations used across server and client.

**Key Files**:

1. **[`BiomeEnums.cs`](../SharedProtocol/Common/Enums/BiomeEnums.cs)**
   - Biome type enumerations
   - Biome-related constants
   - Biome properties

2. **[`CombatEnums.cs`](../SharedProtocol/Common/Enums/CombatEnums.cs)**
   - Combat-related enumerations
   - Damage types
   - Combat states
   - Attack types

3. **[`CoreEnums.cs`](../SharedProtocol/Common/Enums/CoreEnums.cs)**
   - Core game enumerations
   - Game modes
   - Difficulty levels
   - Dimension types

4. **[`GameEnums.cs`](../SharedProtocol/Common/Enums/GameEnums.cs)**
   - Game-specific enumerations
   - Player states
   - Interaction types
   - Game events

5. **[`ItemEnums.cs`](../SharedProtocol/Common/Enums/ItemEnums.cs)**
   - Item-related enumerations
   - Item types
   - Tool types
   - Rarity levels

6. **[`WorldEnums.cs`](../SharedProtocol/Common/Enums/WorldEnums.cs)**
   - World-related enumerations
   - Block types
   - Weather types
   - Time of day

### Interfaces

**File**: [`SharedProtocol/Common/Interfaces/ISharedProtocol.cs`](../SharedProtocol/Common/Interfaces/ISharedProtocol.cs)

**Purpose**: Shared protocol interfaces.

**Key Interfaces**:

- `ISharedProtocol`: Main shared protocol interface
- Message handling interfaces
- Serialization interfaces

### Enhanced Minecraft Protocol

**Directory**: [`SharedProtocol/EnhancedMinecraft/`](../SharedProtocol/EnhancedMinecraft/)

**Purpose**: Enhanced Minecraft protocol utilities.

**Key Files**:

1. **[`ChunkPayloadBuilder.cs`](../SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs)**
   - Chunk payload building utilities
   - Chunk serialization
   - Chunk compression

2. **[`ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)**
   - Protocol message registry
   - Message type mappings
   - Message validation

3. **[`ProtocolStandardization.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs)**
   - Protocol standardization utilities
   - Message format standardization
   - Version compatibility

4. **[`ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)**
   - Protocol validation utilities
   - Message validation
   - Schema validation

5. **[`ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)**
   - Protocol diagnostics utilities
   - Error reporting
   - Performance monitoring

6. **[`ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)**
   - Protocol fingerprinting
   - Descriptor fingerprint
   - Protocol version verification

7. **[`ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs)**
   - Protocol runtime initialization
   - Runtime setup
   - Protocol initialization

8. **[`UnifiedMessageHandler.cs`](../SharedProtocol/EnhancedMinecraft/UnifiedMessageHandler.cs)**
   - Unified message handling
   - Message routing
   - Message processing

## Protocol Integration

### Generated Protobuf Code

The SharedProtocol project includes generated protobuf code:

```xml
<ItemGroup>
  <Compile Include="..\Assets\Generated\Protobuf\Common.cs">
    <Link>Generated\Common.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\EnhancedMinecraftGame.cs">
    <Link>Generated\EnhancedMinecraftGame.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameAuth.cs">
    <Link>Generated\GameAuth.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameChat.cs">
    <Link>Generated\GameChat.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameCore.cs">
    <Link>Generated\GameCore.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameDiag.cs">
    <Link>Generated\GameDiag.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameMove.cs">
    <Link>Generated\GameMove.cs</Link>
  </Compile>
  <Compile Include="..\Assets\Generated\Protobuf\GameWorld.cs">
    <Link>Generated\GameWorld.cs</Link>
  </Compile>
</ItemGroup>
```

### Dependencies

```xml
<PackageReference Include="Google.Protobuf" Version="3.27.2" />
<PackageReference Include="protobuf-net" Version="3.2.18" />
<PackageReference Include="Grpc.Tools" Version="2.64.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="System.Data.SQLite.Core" Version="1.0.118" />
```

## Server Integration

### GameServer Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="../SharedProtocol/SharedProtocol.csproj" />
  <ProjectReference Include="../GameCommon/GameCommon.csproj" />
</ItemGroup>
```

### Usage in Server

Server code uses shared protocol:

```csharp
using SharedProtocol.EnhancedMinecraft;
using MinecraftGame.Common;
using GameCommon.World;
```

### Key Server Components

1. **[`GameServer/Handlers/`](../GameServer/Handlers/)** - Protocol handlers using shared types
2. **[`GameServer/World/`](../GameServer/World/)** - World management using shared types
3. **[`GameServer/Models/`](../GameServer/Models/)** - Data models using shared types

## Client Integration

### Unity Client Reference

The Unity client references the generated protobuf code directly:

```
Assets/Generated/Protobuf/
  - Common.cs
  - EnhancedMinecraftGame.cs
  - GameAuth.cs
  - GameChat.cs
  - GameCore.cs
  - GameDiag.cs
  - GameMove.cs
  - GameWorld.cs
```

### Usage in Client

Client code uses shared protocol:

```csharp
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
```

## Protocol Validation

### Fingerprinting

The protocol uses fingerprinting to ensure consistency:

```csharp
ProtoFingerprint.AssertDescriptorFingerprint();
ProtoFingerprint.ComputeFingerprint();
```

### Registry Validation

Protocol registry validates bindings:

```csharp
ProtocolRegistry.ValidateBindings();
```

### Runtime Initialization

Protocol runtime ensures initialization:

```csharp
ProtoRuntime.EnsureInitialized();
```

## Benefits of Shared DLL Architecture

### 1. Type Safety

- Strongly-typed enumerations
- Compile-time type checking
- IntelliSense support

### 2. Code Reuse

- Single source of truth for types
- Reduced code duplication
- Consistent behavior across components

### 3. Version Control

- Single version for protocol
- Easier to maintain compatibility
- Centralized version management

### 4. Performance

- Reduced memory footprint
- Faster compilation
- Better runtime performance

### 5. Maintainability

- Centralized protocol definitions
- Easier to update protocol
- Better code organization

## Best Practices

### 1. Protocol Design

- Use common types from SharedProtocol
- Define enums in SharedProtocol/Common/Enums/
- Keep protocol messages in proto files
- Generate protobuf code from proto definitions

### 2. Type Safety

- Use strongly-typed enums instead of magic numbers
- Use common types for data structures
- Validate protocol messages on both ends

### 3. Versioning

- Include version fields in protocol messages
- Use fingerprinting for protocol verification
- Support backward compatibility

### 4. Documentation

- Document all protocol messages
- Document enum values
- Document protocol changes

## Future Improvements

### 1. Protocol Enhancements

- Add protocol version negotiation
- Support protocol extensions
- Add protocol compression

### 2. Tooling

- Improve protobuf code generation
- Add protocol validation tools
- Add protocol documentation generators

### 3. Performance

- Optimize serialization performance
- Add message pooling
- Implement zero-copy where possible

## References

- [`SharedProtocol/SharedProtocol.csproj`](../SharedProtocol/SharedProtocol.csproj)
- [`SharedProtocol/Common/`](../SharedProtocol/Common/)
- [`SharedProtocol/Common/Enums/`](../SharedProtocol/Common/Enums/)
- [`SharedProtocol/EnhancedMinecraft/`](../SharedProtocol/EnhancedMinecraft/)
- [`GameServer/GameServer.csproj`](../GameServer/GameServer.csproj)
- [`proto/`](../proto/) - Protocol definition files

## Session Information

- **Session**: 116
- **Date**: 2026-02-23
- **Status**: Completed
- **Next Steps**: Commit changes locally, push to origin branch

