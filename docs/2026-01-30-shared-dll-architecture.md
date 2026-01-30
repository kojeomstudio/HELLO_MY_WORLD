# 2026-01-30 Shared DLL Architecture

## Document Overview
- **Date**: 2026-01-30
- **Session**: S30
- **Purpose**: Design shared DLL architecture for common enums/codes
- **Status**: Complete

## Current Architecture

### 1. GameCommon.dll

**Location**: [`GameCommon/GameCommon.csproj`](GameCommon/GameCommon.csproj)

**Target Framework**: .NET Standard 2.1
**Purpose**: Shared game logic and definitions for server and Unity client

**Components**:
- **Blocks** - Block types and properties
  - [`BlockType.cs`](GameCommon/Blocks/BlockType.cs) - Block type enum
  - [`BlockProperties.cs`](GameCommon/Blocks/BlockProperties.cs) - Block properties
  - [`BlockRegistry.cs`](GameCommon/Blocks/BlockRegistry.cs) - Block registry

- **Configuration** - Configuration management
  - [`ConfigManager.cs`](GameCommon/Configuration/ConfigManager.cs) - Config manager
  - [`ConfigModels.cs`](GameCommon/Configuration/ConfigModels.cs) - Config models
  - [`UnifiedConfigManager.cs`](GameCommon/Configuration/UnifiedConfigManager.cs) - Unified config manager

- **DataDriven** - Data-driven systems
  - [`DataManager.cs`](GameCommon/DataDriven/DataManager.cs) - Data manager
  - [`DataModels.cs`](GameCommon/DataDriven/DataModels.cs) - Data models
  - [`FeatureManifest.cs`](GameCommon/DataDriven/FeatureManifest.cs) - Feature manifest

- **World** - World contracts
  - [`SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs) - Feature catalog
  - [`WorldMapContracts.cs`](GameCommon/World/WorldMapContracts.cs) - World map contracts
  - [`WorldMapSignature.cs`](GameCommon/World/WorldMapSignature.cs) - World map signature

**Dependencies**:
- `System.Text.Json` (8.0.5)

**Assembly Version**: 1.0.0

### 2. SharedProtocol.dll

**Location**: [`SharedProtocol/SharedProtocol.csproj`](SharedProtocol/SharedProtocol.csproj)

**Target Framework**: .NET 6.0
**Purpose**: Shared protocol definitions and protobuf contracts

**Components**:
- **Core Protocol Files**
  - [`GameProtocol.cs`](SharedProtocol/GameProtocol.cs) - Core protocol definitions
  - [`MessageDispatcher.cs`](SharedProtocol/MessageDispatcher.cs) - Message dispatcher
  - [`Messages.cs`](SharedProtocol/Messages.cs) - Message definitions
  - [`Session.cs`](SharedProtocol/Session.cs) - Session management

- **Minecraft Protocol Files**
  - [`MinecraftContainerMessages.cs`](SharedProtocol/MinecraftContainerMessages.cs) - Container messages
  - [`MinecraftMessageDispatcher.cs`](SharedProtocol/MinecraftMessageDispatcher.cs) - Minecraft message dispatcher
  - [`MinecraftMessages.cs`](SharedProtocol/MinecraftMessages.cs) - Minecraft messages
  - [`WorldSyncMessages.cs`](SharedProtocol/WorldSyncMessages.cs) - World sync messages

- **Enhanced Minecraft Protocol**
  - [`EnhancedMinecraft/ProtocolRegistry.cs`](SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs) - Protocol registry
  - [`ProtocolStandardization.cs`](SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs) - Protocol standardization
  - [`ProtocolValidator.cs`](SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs) - Protocol validator
  - [`ProtoDiagnostics.cs`](SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs) - Proto diagnostics
  - [`ProtoFingerprint.cs`](SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs) - Proto fingerprint
  - [`ProtoRuntime.cs`](SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs) - Proto runtime
  - [`UnifiedMessageHandler.cs`](SharedProtocol/EnhancedMinecraft/UnifiedMessageHandler.cs) - Unified message handler
  - [`ChunkPayloadBuilder.cs`](SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs) - Chunk payload builder

- **Proto Files**
  - [`Proto/enhanced_minecraft.proto`](SharedProtocol/Proto/enhanced_minecraft.proto) - Enhanced Minecraft protocol
  - [`Proto/game.proto`](SharedProtocol/Proto/game.proto) - Game protocol
  - [`Proto/minecraft_game.proto`](SharedProtocol/Proto/minecraft_game.proto) - Minecraft game protocol

**Dependencies**:
- `System.Data.SQLite.Core` (1.0.118)
- `Google.Protobuf` (3.27.2)
- `protobuf-net` (3.2.18)
- `Grpc.Tools` (2.64.0)

**Generated Protobuf Files** (Linked from Assets/Generated/Protobuf/):
- `Common.cs` - Common types
- `EnhancedMinecraftGame.cs` - Enhanced Minecraft protocol
- `GameAuth.cs` - Authentication protocol
- `GameChat.cs` - Chat protocol
- `GameCore.cs` - Core protocol
- `GameDiag.cs` - Diagnostics protocol
- `GameMove.cs` - Movement protocol
- `GameWorld.cs` - World protocol

**Assembly Version**: 1.0.0

### 3. Generated Protobuf Files

**Location**: [`Assets/Generated/Protobuf/`](Assets/Generated/Protobuf/)

**Purpose**: Generated C# code from protobuf definitions

**Files**:
- `Common.cs` - Common types (Vector3, Vector3Int, Color, etc.)
- `EnhancedMinecraftGame.cs` - Enhanced Minecraft protocol messages
- `GameAuth.cs` - Authentication messages
- `GameChat.cs` - Chat messages
- `GameCore.cs` - Core messages
- `GameDiag.cs` - Diagnostic messages
- `GameMove.cs` - Movement messages
- `GameWorld.cs` - World messages

**Namespaces**:
- `MinecraftGame.Common` - Common types
- `EnhancedMinecraftProtocol` - Enhanced protocol
- `Game.Auth` - Authentication
- `Game.Chat` - Chat
- `Game.Core` - Core
- `Game.Diag` - Diagnostics
- `Game.Move` - Movement
- `Game.World` - World

## Architecture Improvements

### 1. Assembly Versioning

**Current Issue**: No explicit assembly versioning

**Recommendation**: Add assembly versioning to all projects

```xml
<!-- GameCommon/GameCommon.csproj -->
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>

<!-- SharedProtocol/SharedProtocol.csproj -->
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

### 2. Unity Compatibility

**Current State**: GameCommon.dll targets .NET Standard 2.1 for Unity 6 compatibility

**Unity 6 Compatibility**:
- Unity 6 supports .NET Standard 2.1
- GameCommon.dll can be referenced by Unity 6 projects
- Generated protobuf files can be used in Unity

**Deployment**:
1. Build GameCommon.dll: `dotnet build GameCommon/GameCommon.csproj`
2. Copy to Unity project: `Assets/Plugins/GameCommon.dll`
3. Unity will automatically load the DLL

### 3. Namespace Organization

**Current State**: Multiple namespaces with potential overlap

**Recommendation**: Standardize namespace organization

#### GameCommon Namespaces
```
GameCommon.Blocks
GameCommon.Configuration
GameCommon.DataDriven
GameCommon.World
```

#### SharedProtocol Namespaces
```
SharedProtocol.Core
SharedProtocol.Auth
SharedProtocol.Chat
SharedProtocol.Core
SharedProtocol.Diag
SharedProtocol.Move
SharedProtocol.World
SharedProtocol.EnhancedMinecraft
```

### 4. Common Enums and Codes

#### Block Types
**Location**: [`GameCommon/Blocks/BlockType.cs`](GameCommon/Blocks/BlockType.cs)

**Enum**: `BlockType`
**Usage**: Shared between server and client for block identification

#### Feature Categories
**Location**: [`GameCommon/World/SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs)

**Enum**: `FeatureCategory`
**Values**: Core, Content, Utility

#### Feature Layers
**Location**: [`GameCommon/World/SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs)

**Enum**: `FeatureLayer`
**Values**: Shared, Server, Client

#### Protocol Enums
**Location**: Generated protobuf files

**Common Enums**:
- `ResultStatus` - Operation results
- `GameMode` - Game modes
- `Difficulty` - Difficulty levels
- `Dimension` - World dimensions
- `Weather` - Weather types
- `TimeOfDay` - Time periods

**Enhanced Protocol Enums**:
- `ItemType` - Item types
- `ItemRarity` - Item rarity
- `ChangeReason` - Block change reasons
- `ChunkUnloadReason` - Chunk unload reasons
- `TileEntityType` - Tile entity types
- `EntityType` - Entity types
- `SpawnReason` - Entity spawn reasons
- `DespawnReason` - Entity despawn reasons
- `PlayerAction` - Player action types
- `CraftingType` - Crafting types
- `RecipeType` - Recipe types
- `DamageType` - Damage types
- `EffectType` - Effect types
- `ParticleType` - Particle types
- `SoundType` - Sound types
- `SoundCategory` - Sound categories
- `ChatType` - Chat types
- `CommandResultType` - Command result types
- `WorldType` - World types
- `WorldDifficulty` - World difficulty levels
- `WeatherType` - Weather types
- `AchievementType` - Achievement types
- `StatisticCategory` - Statistic categories

### 5. DLL Deployment Scripts

#### Build Script
```bash
#!/bin/bash
# build-shared-dlls.sh

echo "Building GameCommon.dll..."
dotnet build GameCommon/GameCommon.csproj -c Release -o ../Assets/Plugins/GameCommon.dll

echo "Building SharedProtocol.dll..."
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ../Assets/Plugins/SharedProtocol.dll

echo "Build complete!"
```

#### Windows Build Script
```batch
@echo off
REM build-shared-dlls.bat

echo Building GameCommon.dll...
dotnet build GameCommon/GameCommon.csproj -c Release -o ..\Assets\Plugins\GameCommon.dll

echo Building SharedProtocol.dll...
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ..\Assets\Plugins\SharedProtocol.dll

echo Build complete!
```

#### PowerShell Build Script
```powershell
# build-shared-dlls.ps1

Write-Host "Building GameCommon.dll..."
dotnet build GameCommon/GameCommon.csproj -c Release -o ..\Assets\Plugins\GameCommon.dll

Write-Host "Building SharedProtocol.dll..."
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ..\Assets\Plugins\SharedProtocol.dll

Write-Host "Build complete!"
```

### 6. Unity Integration

#### Plugin Directory Structure
```
Assets/
├── Plugins/
│   ├── GameCommon.dll
│   ├── GameCommon.dll.meta
│   ├── SharedProtocol.dll
│   └── SharedProtocol.dll.meta
├── Generated/
│   └── Protobuf/
│       ├── Common.cs
│       ├── EnhancedMinecraftGame.cs
│       ├── GameAuth.cs
│       ├── GameChat.cs
│       ├── GameCore.cs
│       ├── GameDiag.cs
│       ├── GameMove.cs
│       └── GameWorld.cs
└── MyAssets/
    └── Scripts/
```

#### Unity Assembly Definition
```csharp
// Assets/Plugins/GameCommon.asmdef
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-firstpass")]
```

### 7. Version Compatibility

#### Semantic Versioning
```
MAJOR.MINOR.PATCH

MAJOR - Breaking changes
MINOR - New features, backward compatible
PATCH - Bug fixes, backward compatible
```

#### Protocol Versioning
```protobuf
// Add to common.proto
message ProtocolInfo {
  int32 major = 1;
  int32 minor = 2;
  int32 patch = 3;
  string build_metadata = 4;
}

message HandshakeRequest {
  ProtocolInfo client_version = 1;
  repeated string supported_features = 2;
}

message HandshakeResponse {
  ProtocolInfo server_version = 1;
  repeated string required_features = 2;
  bool compatible = 3;
  string error_message = 4;
}
```

## Usage Examples

### Server Usage

#### Referencing GameCommon.dll
```csharp
// GameServer/GameServer.csproj
<ItemGroup>
  <Reference Include="..\GameCommon\bin\Release\netstandard2.1\GameCommon.dll" />
</ItemGroup>
```

#### Referencing SharedProtocol.dll
```csharp
// GameServer/GameServer.csproj
<ItemGroup>
  <Reference Include="..\SharedProtocol\bin\Release\net6.0\SharedProtocol.dll" />
</ItemGroup>
```

### Unity Usage

#### Referencing GameCommon.dll
```csharp
// Unity automatically loads DLLs from Assets/Plugins/
using GameCommon.Blocks;
using GameCommon.Configuration;
using GameCommon.DataDriven;
using GameCommon.World;
```

#### Referencing Protobuf
```csharp
// Generated protobuf files are in Assets/Generated/Protobuf/
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Chat;
using Game.Core;
using Game.Diag;
using Game.Move;
using Game.World;
```

## Testing

### Unit Tests

#### GameCommon Tests
```csharp
// Tests/GameCommon/BlockTypeTests.cs
[Test]
public void BlockType_EnumValues_AreCorrect()
{
    Assert.AreEqual(0, (int)BlockType.Air);
    Assert.AreEqual(1, (int)BlockType.Stone);
    Assert.AreEqual(2, (int)BlockType.Grass);
}
```

#### SharedProtocol Tests
```csharp
// Tests/SharedProtocol/ProtocolSerializationTests.cs
[Test]
public void Protocol_SerializeDeserialize_WorksCorrectly()
{
    var message = new LoginRequest
    {
        Username = "test_user",
        Password = "test_password"
    };
    
    byte[] bytes = message.ToByteArray();
    var deserialized = new LoginRequest();
    deserialized.MergeFrom(bytes);
    
    Assert.AreEqual(message.Username, deserialized.Username);
    Assert.AreEqual(message.Password, deserialized.Password);
}
```

### Integration Tests

#### Server-Client Compatibility
```csharp
[Test]
public void ServerClient_SharedEnums_Match()
{
    // Verify that enums match between server and client
    var serverBlockTypes = Enum.GetValues(typeof(BlockType));
    var clientBlockTypes = Enum.GetValues(typeof(BlockType));
    
    Assert.AreEqual(serverBlockTypes.Length, clientBlockTypes.Length);
}
```

## Build Process

### 1. Build GameCommon.dll
```bash
dotnet build GameCommon/GameCommon.csproj -c Release
```

### 2. Build SharedProtocol.dll
```bash
dotnet build SharedProtocol/SharedProtocol.csproj -c Release
```

### 3. Build GameServer
```bash
dotnet build GameServer/GameServer.csproj -c Release
```

### 4. Generate Protobuf Files
```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

### 5. Unity Import
1. Copy GameCommon.dll to `Assets/Plugins/`
2. Copy SharedProtocol.dll to `Assets/Plugins/`
3. Ensure protobuf generated files are in `Assets/Generated/Protobuf/`
4. Open Unity project
5. Unity will automatically load the DLLs

## Troubleshooting

### Common Issues

#### 1. Assembly Not Found in Unity
**Symptoms**: `MissingMethodException` or `TypeLoadException`
**Solutions**:
- Verify DLL is in `Assets/Plugins/`
- Check .NET Standard compatibility
- Verify assembly version matches
- Restart Unity editor

#### 2. Namespace Not Found
**Symptoms**: `CS0246: The type or namespace name could not be found`
**Solutions**:
- Verify using statements
- Check namespace in generated files
- Verify assembly is referenced
- Clean and rebuild project

#### 3. Protocol Version Mismatch
**Symptoms**: Deserialization errors or wrong message types
**Solutions**:
- Verify protobuf files are up-to-date
- Regenerate protobuf files
- Check protocol version handshake
- Verify assembly versions match

## Best Practices

### 1. Version Management
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Update assembly version on breaking changes
- Maintain backward compatibility when possible
- Document version changes in CHANGELOG.md

### 2. Namespace Organization
- Use consistent naming conventions
- Organize by feature area
- Avoid namespace conflicts
- Use namespace aliases when needed

### 3. Code Sharing
- Put shared enums in GameCommon
- Put shared protocols in SharedProtocol
- Avoid duplicating code
- Use interfaces for abstraction

### 4. Testing
- Write unit tests for shared code
- Test serialization/deserialization
- Test version compatibility
- Test Unity integration

### 5. Documentation
- Document all shared enums
- Document protocol messages
- Document version compatibility
- Document usage examples

## Conclusion

The current shared DLL architecture provides a solid foundation for code sharing between server and Unity client:

1. **GameCommon.dll** - Shared game logic and definitions
2. **SharedProtocol.dll** - Shared protocol definitions
3. **Generated Protobuf** - Generated protocol code

The architecture can be improved with:
1. **Assembly versioning** - Add explicit versioning
2. **Namespace organization** - Standardize namespace structure
3. **Build scripts** - Automate DLL building and deployment
4. **Unity integration** - Ensure smooth Unity compatibility
5. **Testing** - Add comprehensive tests
6. **Documentation** - Improve documentation

The shared DLL architecture is ready for use and can be further enhanced based on requirements.

---

**Document Version**: 1.0
**Last Updated**: 2026-01-30
**Next Review**: After implementing recommended improvements

## Document Overview
- **Date**: 2026-01-30
- **Session**: S30
- **Purpose**: Design shared DLL architecture for common enums/codes
- **Status**: Complete

## Current Architecture

### 1. GameCommon.dll

**Location**: [`GameCommon/GameCommon.csproj`](GameCommon/GameCommon.csproj)

**Target Framework**: .NET Standard 2.1
**Purpose**: Shared game logic and definitions for server and Unity client

**Components**:
- **Blocks** - Block types and properties
  - [`BlockType.cs`](GameCommon/Blocks/BlockType.cs) - Block type enum
  - [`BlockProperties.cs`](GameCommon/Blocks/BlockProperties.cs) - Block properties
  - [`BlockRegistry.cs`](GameCommon/Blocks/BlockRegistry.cs) - Block registry

- **Configuration** - Configuration management
  - [`ConfigManager.cs`](GameCommon/Configuration/ConfigManager.cs) - Config manager
  - [`ConfigModels.cs`](GameCommon/Configuration/ConfigModels.cs) - Config models
  - [`UnifiedConfigManager.cs`](GameCommon/Configuration/UnifiedConfigManager.cs) - Unified config manager

- **DataDriven** - Data-driven systems
  - [`DataManager.cs`](GameCommon/DataDriven/DataManager.cs) - Data manager
  - [`DataModels.cs`](GameCommon/DataDriven/DataModels.cs) - Data models
  - [`FeatureManifest.cs`](GameCommon/DataDriven/FeatureManifest.cs) - Feature manifest

- **World** - World contracts
  - [`SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs) - Feature catalog
  - [`WorldMapContracts.cs`](GameCommon/World/WorldMapContracts.cs) - World map contracts
  - [`WorldMapSignature.cs`](GameCommon/World/WorldMapSignature.cs) - World map signature

**Dependencies**:
- `System.Text.Json` (8.0.5)

**Assembly Version**: 1.0.0

### 2. SharedProtocol.dll

**Location**: [`SharedProtocol/SharedProtocol.csproj`](SharedProtocol/SharedProtocol.csproj)

**Target Framework**: .NET 6.0
**Purpose**: Shared protocol definitions and protobuf contracts

**Components**:
- **Core Protocol Files**
  - [`GameProtocol.cs`](SharedProtocol/GameProtocol.cs) - Core protocol definitions
  - [`MessageDispatcher.cs`](SharedProtocol/MessageDispatcher.cs) - Message dispatcher
  - [`Messages.cs`](SharedProtocol/Messages.cs) - Message definitions
  - [`Session.cs`](SharedProtocol/Session.cs) - Session management

- **Minecraft Protocol Files**
  - [`MinecraftContainerMessages.cs`](SharedProtocol/MinecraftContainerMessages.cs) - Container messages
  - [`MinecraftMessageDispatcher.cs`](SharedProtocol/MinecraftMessageDispatcher.cs) - Minecraft message dispatcher
  - [`MinecraftMessages.cs`](SharedProtocol/MinecraftMessages.cs) - Minecraft messages
  - [`WorldSyncMessages.cs`](SharedProtocol/WorldSyncMessages.cs) - World sync messages

- **Enhanced Minecraft Protocol**
  - [`EnhancedMinecraft/ProtocolRegistry.cs`](SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs) - Protocol registry
  - [`ProtocolStandardization.cs`](SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs) - Protocol standardization
  - [`ProtocolValidator.cs`](SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs) - Protocol validator
  - [`ProtoDiagnostics.cs`](SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs) - Proto diagnostics
  - [`ProtoFingerprint.cs`](SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs) - Proto fingerprint
  - [`ProtoRuntime.cs`](SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs) - Proto runtime
  - [`UnifiedMessageHandler.cs`](SharedProtocol/EnhancedMinecraft/UnifiedMessageHandler.cs) - Unified message handler
  - [`ChunkPayloadBuilder.cs`](SharedProtocol/EnhancedMinecraft/ChunkPayloadBuilder.cs) - Chunk payload builder

- **Proto Files**
  - [`Proto/enhanced_minecraft.proto`](SharedProtocol/Proto/enhanced_minecraft.proto) - Enhanced Minecraft protocol
  - [`Proto/game.proto`](SharedProtocol/Proto/game.proto) - Game protocol
  - [`Proto/minecraft_game.proto`](SharedProtocol/Proto/minecraft_game.proto) - Minecraft game protocol

**Dependencies**:
- `System.Data.SQLite.Core` (1.0.118)
- `Google.Protobuf` (3.27.2)
- `protobuf-net` (3.2.18)
- `Grpc.Tools` (2.64.0)

**Generated Protobuf Files** (Linked from Assets/Generated/Protobuf/):
- `Common.cs` - Common types
- `EnhancedMinecraftGame.cs` - Enhanced Minecraft protocol
- `GameAuth.cs` - Authentication protocol
- `GameChat.cs` - Chat protocol
- `GameCore.cs` - Core protocol
- `GameDiag.cs` - Diagnostics protocol
- `GameMove.cs` - Movement protocol
- `GameWorld.cs` - World protocol

**Assembly Version**: 1.0.0

### 3. Generated Protobuf Files

**Location**: [`Assets/Generated/Protobuf/`](Assets/Generated/Protobuf/)

**Purpose**: Generated C# code from protobuf definitions

**Files**:
- `Common.cs` - Common types (Vector3, Vector3Int, Color, etc.)
- `EnhancedMinecraftGame.cs` - Enhanced Minecraft protocol messages
- `GameAuth.cs` - Authentication messages
- `GameChat.cs` - Chat messages
- `GameCore.cs` - Core messages
- `GameDiag.cs` - Diagnostic messages
- `GameMove.cs` - Movement messages
- `GameWorld.cs` - World messages

**Namespaces**:
- `MinecraftGame.Common` - Common types
- `EnhancedMinecraftProtocol` - Enhanced protocol
- `Game.Auth` - Authentication
- `Game.Chat` - Chat
- `Game.Core` - Core
- `Game.Diag` - Diagnostics
- `Game.Move` - Movement
- `Game.World` - World

## Architecture Improvements

### 1. Assembly Versioning

**Current Issue**: No explicit assembly versioning

**Recommendation**: Add assembly versioning to all projects

```xml
<!-- GameCommon/GameCommon.csproj -->
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>

<!-- SharedProtocol/SharedProtocol.csproj -->
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

### 2. Unity Compatibility

**Current State**: GameCommon.dll targets .NET Standard 2.1 for Unity 6 compatibility

**Unity 6 Compatibility**:
- Unity 6 supports .NET Standard 2.1
- GameCommon.dll can be referenced by Unity 6 projects
- Generated protobuf files can be used in Unity

**Deployment**:
1. Build GameCommon.dll: `dotnet build GameCommon/GameCommon.csproj`
2. Copy to Unity project: `Assets/Plugins/GameCommon.dll`
3. Unity will automatically load the DLL

### 3. Namespace Organization

**Current State**: Multiple namespaces with potential overlap

**Recommendation**: Standardize namespace organization

#### GameCommon Namespaces
```
GameCommon.Blocks
GameCommon.Configuration
GameCommon.DataDriven
GameCommon.World
```

#### SharedProtocol Namespaces
```
SharedProtocol.Core
SharedProtocol.Auth
SharedProtocol.Chat
SharedProtocol.Core
SharedProtocol.Diag
SharedProtocol.Move
SharedProtocol.World
SharedProtocol.EnhancedMinecraft
```

### 4. Common Enums and Codes

#### Block Types
**Location**: [`GameCommon/Blocks/BlockType.cs`](GameCommon/Blocks/BlockType.cs)

**Enum**: `BlockType`
**Usage**: Shared between server and client for block identification

#### Feature Categories
**Location**: [`GameCommon/World/SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs)

**Enum**: `FeatureCategory`
**Values**: Core, Content, Utility

#### Feature Layers
**Location**: [`GameCommon/World/SharedFeatureCatalog.cs`](GameCommon/World/SharedFeatureCatalog.cs)

**Enum**: `FeatureLayer`
**Values**: Shared, Server, Client

#### Protocol Enums
**Location**: Generated protobuf files

**Common Enums**:
- `ResultStatus` - Operation results
- `GameMode` - Game modes
- `Difficulty` - Difficulty levels
- `Dimension` - World dimensions
- `Weather` - Weather types
- `TimeOfDay` - Time periods

**Enhanced Protocol Enums**:
- `ItemType` - Item types
- `ItemRarity` - Item rarity
- `ChangeReason` - Block change reasons
- `ChunkUnloadReason` - Chunk unload reasons
- `TileEntityType` - Tile entity types
- `EntityType` - Entity types
- `SpawnReason` - Entity spawn reasons
- `DespawnReason` - Entity despawn reasons
- `PlayerAction` - Player action types
- `CraftingType` - Crafting types
- `RecipeType` - Recipe types
- `DamageType` - Damage types
- `EffectType` - Effect types
- `ParticleType` - Particle types
- `SoundType` - Sound types
- `SoundCategory` - Sound categories
- `ChatType` - Chat types
- `CommandResultType` - Command result types
- `WorldType` - World types
- `WorldDifficulty` - World difficulty levels
- `WeatherType` - Weather types
- `AchievementType` - Achievement types
- `StatisticCategory` - Statistic categories

### 5. DLL Deployment Scripts

#### Build Script
```bash
#!/bin/bash
# build-shared-dlls.sh

echo "Building GameCommon.dll..."
dotnet build GameCommon/GameCommon.csproj -c Release -o ../Assets/Plugins/GameCommon.dll

echo "Building SharedProtocol.dll..."
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ../Assets/Plugins/SharedProtocol.dll

echo "Build complete!"
```

#### Windows Build Script
```batch
@echo off
REM build-shared-dlls.bat

echo Building GameCommon.dll...
dotnet build GameCommon/GameCommon.csproj -c Release -o ..\Assets\Plugins\GameCommon.dll

echo Building SharedProtocol.dll...
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ..\Assets\Plugins\SharedProtocol.dll

echo Build complete!
```

#### PowerShell Build Script
```powershell
# build-shared-dlls.ps1

Write-Host "Building GameCommon.dll..."
dotnet build GameCommon/GameCommon.csproj -c Release -o ..\Assets\Plugins\GameCommon.dll

Write-Host "Building SharedProtocol.dll..."
dotnet build SharedProtocol/SharedProtocol.csproj -c Release -o ..\Assets\Plugins\SharedProtocol.dll

Write-Host "Build complete!"
```

### 6. Unity Integration

#### Plugin Directory Structure
```
Assets/
├── Plugins/
│   ├── GameCommon.dll
│   ├── GameCommon.dll.meta
│   ├── SharedProtocol.dll
│   └── SharedProtocol.dll.meta
├── Generated/
│   └── Protobuf/
│       ├── Common.cs
│       ├── EnhancedMinecraftGame.cs
│       ├── GameAuth.cs
│       ├── GameChat.cs
│       ├── GameCore.cs
│       ├── GameDiag.cs
│       ├── GameMove.cs
│       └── GameWorld.cs
└── MyAssets/
    └── Scripts/
```

#### Unity Assembly Definition
```csharp
// Assets/Plugins/GameCommon.asmdef
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-firstpass")]
```

### 7. Version Compatibility

#### Semantic Versioning
```
MAJOR.MINOR.PATCH

MAJOR - Breaking changes
MINOR - New features, backward compatible
PATCH - Bug fixes, backward compatible
```

#### Protocol Versioning
```protobuf
// Add to common.proto
message ProtocolInfo {
  int32 major = 1;
  int32 minor = 2;
  int32 patch = 3;
  string build_metadata = 4;
}

message HandshakeRequest {
  ProtocolInfo client_version = 1;
  repeated string supported_features = 2;
}

message HandshakeResponse {
  ProtocolInfo server_version = 1;
  repeated string required_features = 2;
  bool compatible = 3;
  string error_message = 4;
}
```

## Usage Examples

### Server Usage

#### Referencing GameCommon.dll
```csharp
// GameServer/GameServer.csproj
<ItemGroup>
  <Reference Include="..\GameCommon\bin\Release\netstandard2.1\GameCommon.dll" />
</ItemGroup>
```

#### Referencing SharedProtocol.dll
```csharp
// GameServer/GameServer.csproj
<ItemGroup>
  <Reference Include="..\SharedProtocol\bin\Release\net6.0\SharedProtocol.dll" />
</ItemGroup>
```

### Unity Usage

#### Referencing GameCommon.dll
```csharp
// Unity automatically loads DLLs from Assets/Plugins/
using GameCommon.Blocks;
using GameCommon.Configuration;
using GameCommon.DataDriven;
using GameCommon.World;
```

#### Referencing Protobuf
```csharp
// Generated protobuf files are in Assets/Generated/Protobuf/
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Chat;
using Game.Core;
using Game.Diag;
using Game.Move;
using Game.World;
```

## Testing

### Unit Tests

#### GameCommon Tests
```csharp
// Tests/GameCommon/BlockTypeTests.cs
[Test]
public void BlockType_EnumValues_AreCorrect()
{
    Assert.AreEqual(0, (int)BlockType.Air);
    Assert.AreEqual(1, (int)BlockType.Stone);
    Assert.AreEqual(2, (int)BlockType.Grass);
}
```

#### SharedProtocol Tests
```csharp
// Tests/SharedProtocol/ProtocolSerializationTests.cs
[Test]
public void Protocol_SerializeDeserialize_WorksCorrectly()
{
    var message = new LoginRequest
    {
        Username = "test_user",
        Password = "test_password"
    };
    
    byte[] bytes = message.ToByteArray();
    var deserialized = new LoginRequest();
    deserialized.MergeFrom(bytes);
    
    Assert.AreEqual(message.Username, deserialized.Username);
    Assert.AreEqual(message.Password, deserialized.Password);
}
```

### Integration Tests

#### Server-Client Compatibility
```csharp
[Test]
public void ServerClient_SharedEnums_Match()
{
    // Verify that enums match between server and client
    var serverBlockTypes = Enum.GetValues(typeof(BlockType));
    var clientBlockTypes = Enum.GetValues(typeof(BlockType));
    
    Assert.AreEqual(serverBlockTypes.Length, clientBlockTypes.Length);
}
```

## Build Process

### 1. Build GameCommon.dll
```bash
dotnet build GameCommon/GameCommon.csproj -c Release
```

### 2. Build SharedProtocol.dll
```bash
dotnet build SharedProtocol/SharedProtocol.csproj -c Release
```

### 3. Build GameServer
```bash
dotnet build GameServer/GameServer.csproj -c Release
```

### 4. Generate Protobuf Files
```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

### 5. Unity Import
1. Copy GameCommon.dll to `Assets/Plugins/`
2. Copy SharedProtocol.dll to `Assets/Plugins/`
3. Ensure protobuf generated files are in `Assets/Generated/Protobuf/`
4. Open Unity project
5. Unity will automatically load the DLLs

## Troubleshooting

### Common Issues

#### 1. Assembly Not Found in Unity
**Symptoms**: `MissingMethodException` or `TypeLoadException`
**Solutions**:
- Verify DLL is in `Assets/Plugins/`
- Check .NET Standard compatibility
- Verify assembly version matches
- Restart Unity editor

#### 2. Namespace Not Found
**Symptoms**: `CS0246: The type or namespace name could not be found`
**Solutions**:
- Verify using statements
- Check namespace in generated files
- Verify assembly is referenced
- Clean and rebuild project

#### 3. Protocol Version Mismatch
**Symptoms**: Deserialization errors or wrong message types
**Solutions**:
- Verify protobuf files are up-to-date
- Regenerate protobuf files
- Check protocol version handshake
- Verify assembly versions match

## Best Practices

### 1. Version Management
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Update assembly version on breaking changes
- Maintain backward compatibility when possible
- Document version changes in CHANGELOG.md

### 2. Namespace Organization
- Use consistent naming conventions
- Organize by feature area
- Avoid namespace conflicts
- Use namespace aliases when needed

### 3. Code Sharing
- Put shared enums in GameCommon
- Put shared protocols in SharedProtocol
- Avoid duplicating code
- Use interfaces for abstraction

### 4. Testing
- Write unit tests for shared code
- Test serialization/deserialization
- Test version compatibility
- Test Unity integration

### 5. Documentation
- Document all shared enums
- Document protocol messages
- Document version compatibility
- Document usage examples

## Conclusion

The current shared DLL architecture provides a solid foundation for code sharing between server and Unity client:

1. **GameCommon.dll** - Shared game logic and definitions
2. **SharedProtocol.dll** - Shared protocol definitions
3. **Generated Protobuf** - Generated protocol code

The architecture can be improved with:
1. **Assembly versioning** - Add explicit versioning
2. **Namespace organization** - Standardize namespace structure
3. **Build scripts** - Automate DLL building and deployment
4. **Unity integration** - Ensure smooth Unity compatibility
5. **Testing** - Add comprehensive tests
6. **Documentation** - Improve documentation

The shared DLL architecture is ready for use and can be further enhanced based on requirements.

---

**Document Version**: 1.0
**Last Updated**: 2026-01-30
**Next Review**: After implementing recommended improvements

