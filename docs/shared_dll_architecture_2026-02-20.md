# Shared .dll Architecture
**Date:** 2026-02-20  
**Session:** 102  
**Status:** Architecture Design Complete

## Executive Summary

This document provides the architecture design for a shared .dll library that contains common enums and codes shared between the server and client. This shared library ensures type safety and consistency across the codebase.

---

## Architecture Overview

### Current State

**Existing Shared Protocol Projects:**
- `SharedProtocol/` - Contains protobuf-net message definitions and protocol utilities
- `Assets/Generated/Protobuf/` - Contains Google.Protobuf generated code

**Issue:**
- No dedicated shared .dll for common enums and shared code
- Enums are scattered across multiple projects
- Type safety is not enforced at compile time

---

## Proposed Architecture

### 1. Shared Enums Library

**Project Name:** `SharedProtocol.Common`

**Location:** `SharedProtocol/Common/`

**Purpose:** Centralize all shared enums and constants used by both server and client

#### Enum Categories

| Category | Enums | Description |
|----------|-------|-------------|
| Core | `MessageType`, `ChatType`, `RoomRole`, `RoomVisibility`, `RoomStatus` | Core protocol enums |
| Game | `GameMode`, `Difficulty`, `EntityType`, `BlockFace` | Gameplay enums |
| Items | `ItemType`, `ItemRarity`, `ToolType` | Item-related enums |
| Combat | `DamageType`, `PlayerAction` | Combat enums |
| World | `WorldType`, `WeatherType`, `ChunkUnloadReason`, `SpawnReason`, `DespawnReason` | World enums |
| Biome | `BiomeType` | Biome enums |
| Block | `BlockType` | Block type enums |

---

### 2. Project Structure

```
SharedProtocol/
├── Common/
│   ├── Enums/
│   │   ├── CoreEnums.cs
│   │   ├── GameEnums.cs
│   │   ├── ItemEnums.cs
│   │   ├── CombatEnums.cs
│   │   ├── WorldEnums.cs
│   │   └── BiomeEnums.cs
│   ├── Constants/
│   │   ├── GameConstants.cs
│   │   ├── NetworkConstants.cs
│   │   └── WorldConstants.cs
│   ├── Interfaces/
│   │   ├── ISharedProtocol.cs
│   │   └── ISharedConstants.cs
│   └── SharedProtocol.Common.csproj
├── EnhancedMinecraft/
│   ├── ProtocolRegistry.cs (existing)
│   ├── ProtocolValidator.cs (existing)
│   ├── ProtocolStandardization.cs (existing)
│   ├── ProtoDiagnostics.cs (existing)
│   ├── ProtoFingerprint.cs (existing)
│   ├── ProtoRuntime.cs (existing)
│   └── UnifiedMessageHandler.cs (existing)
├── Messages.cs (existing - protobuf-net)
└── SharedProtocol.csproj (existing)
```

---

### 3. Enum Definitions

#### Core Enums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Core protocol enumeration types
/// </summary>
public static class CoreEnums
{
    /// <summary>
    /// Message types for client-server communication
    /// </summary>
    public enum MessageType
    {
        // Authentication
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,
        
        // Movement
        MoveRequest = 10,
        MoveResponse = 11,
        
        // World/Blocks
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,
        
        // Chat
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,
        
        // Server Status
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,
        
        // Player
        PlayerInfoUpdate = 50,
        
        // Inventory
        InventoryRequest = 60,
        InventoryResponse = 61,
        InventoryUpdateBroadcast = 62,
        
        // Crafting
        CraftingRequest = 70,
        CraftingResponse = 71,
        RecipeListRequest = 72,
        RecipeListResponse = 73,
        
        // Health
        HealthActionRequest = 80,
        HealthActionResponse = 81,
        HealthUpdate = 82,
        RespawnRequest = 83,
        RespawnResponse = 84,
        PlayerDeath = 85,
        PlayerRespawnBroadcast = 86,
        CombatEvent = 87,
        
        // Room/Lobby
        RoomListRequest = 90,
        RoomListResponse = 91,
        RoomEnterRequest = 92,
        RoomEnterResponse = 93,
        RoomLeaveRequest = 94,
        RoomLeaveResponse = 95,
        RoomQueueUpdate = 96,
        RoomPromotionNotice = 97,
        
        // AI System
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,
        
        // Combat System
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,
        
        // Commands
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122
    }
    
    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3,
        Team = 4,
        Announcement = 5,
        Death = 6,
        JoinLeave = 7,
        Achievement = 8,
        CommandResult = 9
    }
    
    /// <summary>
    /// Room visibility settings
    /// </summary>
    public enum RoomVisibility
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2
    }
    
    /// <summary>
    /// Room status
    /// </summary>
    public enum RoomStatus
    {
        Waiting = 0,
        InGame = 1,
        Completed = 2,
        Locked = 3
    }
    
    /// <summary>
    /// Room member roles
    /// </summary>
    public enum RoomRole
    {
        Player = 0,
        Host = 1,
        Moderator = 2,
        Spectator = 3,
        Queue = 4
    }
}
```

#### GameEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Gameplay enumeration types
/// </summary>
public static class GameEnums
{
    /// <summary>
    /// Game modes
    /// </summary>
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }
    
    /// <summary>
    /// Difficulty levels
    /// </summary>
    public enum Difficulty
    {
        Peaceful = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3
    }
    
    /// <summary>
    /// Entity types
    /// </summary>
    public enum EntityType
    {
        Unknown = 0,
        Player = 1,
        // Hostile mobs
        Zombie = 10,
        Skeleton = 11,
        Creeper = 12,
        Spider = 13,
        Enderman = 14,
        Witch = 15,
        Slime = 16,
        // Neutral/Passive mobs
        Pig = 20,
        Cow = 21,
        Sheep = 22,
        Chicken = 23,
        Horse = 24,
        Wolf = 25,
        Cat = 26,
        Villager = 27,
        // Other
        DroppedItem = 30,
        Arrow = 31,
        ExperienceOrb = 32,
        Boat = 33,
        Minecart = 34,
        Fireball = 35
    }
}
```

#### ItemEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Item-related enumeration types
/// </summary>
public static class ItemEnums
{
    /// <summary>
    /// Item categories
    /// </summary>
    public enum ItemCategory
    {
        Block = 0,
        Tool = 1,
        Weapon = 2,
        Armor = 3,
        Food = 4,
        Material = 5,
        Potion = 6,
        Misc = 7
    }
    
    /// <summary>
    /// Item rarity levels
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }
    
    /// <summary>
    /// Tool types
    /// </summary>
    public enum ToolType
    {
        None = 0,
        Hand = 1,
        Sword = 2,
        Axe = 3,
        Pickaxe = 4,
        Shovel = 5,
        Hoe = 6,
        Bow = 7,
        FishingRod = 8
    }
}
```

#### CombatEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Combat-related enumeration types
/// </summary>
public static class CombatEnums
{
    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction
    {
        // Block actions
        StartDestroyBlock = 0,
        AbortDestroyBlock = 1,
        FinishDestroyBlock = 2,
        PlaceBlock = 3,
        RightClickBlock = 4,
        
        // Item actions
        UseItem = 10,
        DropItem = 11,
        DropItemStack = 12,
        EatFood = 13,
        DrinkPotion = 14,
        
        // Combat actions
        AttackEntity = 20,
        ShootBow = 21,
        BlockWithShield = 22,
        
        // Movement
        Interact = 30,
        SneakStart = 31,
        SneakStop = 32,
        SprintStart = 33,
        SprintStop = 34,
        Jump = 35
    }
    
    /// <summary>
    /// Damage types
    /// </summary>
    public enum DamageType
    {
        Generic = 0,
        EntityAttack = 1,
        Projectile = 2,
        Fall = 3,
        Fire = 4,
        FireTick = 5,
        Lava = 6,
        Drowning = 7,
        Suffocation = 8,
        Explosion = 9,
        Void = 10,
        Poison = 11,
        Magic = 12,
        Wither = 13,
        Anvil = 14,
        Cactus = 15,
        Lightning = 16,
        Starvation = 17
    }
}
```

#### WorldEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// World-related enumeration types
/// </summary>
public static class WorldEnums
{
    /// <summary>
    /// World generation types
    /// </summary>
    public enum WorldType
    {
        Normal = 0,
        Flat = 1,
        LargeBiomes = 2,
        Amplified = 3,
        Debug = 4,
        Custom = 5
    }
    
    /// <summary>
    /// Weather types
    /// </summary>
    public enum WeatherType
    {
        Clear = 0,
        Rain = 1,
        Storm = 2,
        Snow = 3
    }
    
    /// <summary>
    /// Chunk unload reasons
    /// </summary>
    public enum ChunkUnloadReason
    {
        UnloadViewDistance = 0,
        UnloadManual = 1,
        UnloadWorldTransfer = 2,
        UnloadShutdown = 3
    }
    
    /// <summary>
    /// Entity spawn reasons
    /// </summary>
    public enum SpawnReason
    {
        Natural = 0,
        Spawner = 1,
        Breeding = 2,
        Command = 3,
        ItemDrop = 4,
        Projectile = 5
    }
    
    /// <summary>
    /// Entity despawn reasons
    /// </summary>
    public enum DespawnReason
    {
        Natural = 0,
        Death = 1,
        Pickup = 2,
        ChunkUnload = 3,
        Command = 4
    }
}
```

#### BiomeEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Biome-related enumeration types
/// </summary>
public static class BiomeEnums
{
    /// <summary>
    /// Biome types
    /// </summary>
    public enum BiomeType
    {
        Plains = 0,
        Desert = 1,
        Forest = 2,
        Taiga = 3,
        Swamp = 4,
        Mountains = 5,
        Ocean = 6,
        River = 7,
        Beach = 8
    }
    
    /// <summary>
    /// Block face directions
    /// </summary>
    public enum BlockFace
    {
        Top = 0,
        Bottom = 1,
        Front = 2,
        Back = 3,
        Left = 4,
        Right = 5
    }
}
```

---

### 4. Constants

#### GameConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Game-related constants
/// </summary>
public static class GameConstants
{
    public const int ChunkSize = 16;
    public const int WorldHeight = 256;
    public const int SeaLevel = 62;
    public const int BedrockLevel = 5;
    public const int MaxPlayersPerWorld = 20;
    public const int DefaultViewDistance = 8;
    public const int MaxViewDistance = 16;
    public const int MaxInventorySlots = 36;
    public const int HotbarSlots = 9;
    public const float PlayerHeight = 1.8f;
    public const float PlayerWidth = 0.6f;
    public const float PlayerDepth = 0.6f;
    public const float PlayerReachDistance = 4.5f;
}
```

#### NetworkConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Network-related constants
/// </summary>
public static class NetworkConstants
{
    public const int DefaultServerPort = 9000;
    public const string DefaultServerAddress = "127.0.0.1";
    public const int DefaultConnectionTimeoutMs = 10000;
    public const int DefaultMaxPacketSize = 1048576;
    public const int DefaultCompressionThreshold = 1024;
    public const int DefaultHeartbeatIntervalMs = 30000;
    public const int DefaultMaxConnections = 100;
}
```

#### WorldConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// World-related constants
/// </summary>
public static class WorldConstants
{
    public const int DayTimeTicks = 24000;
    public const int DefaultDayTime = 1000;
    public const int ClearWeatherDurationTicks = 360 * 20; // 7200 ticks
    public const int RainWeatherDurationTicks = 180 * 20; // 3600 ticks
    public const int StormWeatherDurationTicks = 120 * 20; // 2400 ticks
    public const int SnowWeatherDurationTicks = 240 * 20; // 4800 ticks
    public const float WeatherStormProbability = 0.1f;
    public const float WeatherSnowProbability = 0.05f;
}
```

---

### 5. Project File: SharedProtocol.Common.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>false</WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System" />
    <PackageReference Include="System.Runtime.Serialization" />
  </ItemGroup>

  <PropertyGroup>
    <OutputPath>bin\$(Configuration)\</OutputPath>
    <DocumentationFile>bin\$(Configuration)\SharedProtocol.Common.xml</DocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Enums\**\*.cs" />
    <Compile Include="Constants\**\*.cs" />
    <Compile Include="Interfaces\**\*.cs" />
  </ItemGroup>
</Project>
```

---

## Implementation Steps

### Phase 1: Create Project Structure
1. Create `SharedProtocol/Common/` directory
2. Create enum files
3. Create constant files
4. Create interface files
5. Create project file

### Phase 2: Update Existing Projects
1. Update `SharedProtocol.csproj` to reference new Common project
2. Update server project to reference SharedProtocol.Common
3. Update client code to use SharedProtocol.Common enums
4. Remove duplicate enum definitions

### Phase 3: Testing
1. Build SharedProtocol.Common project
2. Build SharedProtocol project
3. Build GameServer project
4. Run unit tests

### Phase 4: Documentation
1. Create API documentation
2. Create migration guide
3. Update README.md

---

## Benefits

### Type Safety
- Compile-time checking of enum values
- IDE autocomplete support
- Refactoring safety

### Consistency
- Single source of truth for shared values
- Easier to maintain
- Reduces bugs from mismatched enums

### Maintainability
- Centralized location for changes
- Clear separation of concerns
- Easier to add new shared enums

---

## Migration Guide

### For Server Code
```csharp
// Before:
using SharedProtocol;

// After:
using SharedProtocol;
using SharedProtocol.Common.Enums;
using SharedProtocol.Common.Constants;

// Replace enum references:
MessageType.LoginRequest -> CoreEnums.MessageType.LoginRequest
ChatType.Global -> CoreEnums.ChatType.Global
GameMode.Survival -> GameEnums.GameMode.Survival
// ... and so on
```

### For Client Code
```csharp
// Before:
using SharedProtocol;

// After:
using SharedProtocol;
using SharedProtocol.Common.Enums;
using SharedProtocol.Common.Constants;

// Replace enum references:
MessageType.LoginRequest -> CoreEnums.MessageType.LoginRequest
ChatType.Global -> CoreEnums.ChatType.Global
GameMode.Survival -> GameEnums.GameMode.Survival
// ... and so on
```

---

## Testing Checklist

- [ ] Create SharedProtocol.Common project structure
- [ ] Implement CoreEnums.cs
- [ ] Implement GameEnums.cs
- [ ] Implement ItemEnums.cs
- [ ] Implement CombatEnums.cs
- [ ] Implement WorldEnums.cs
- [ ] Implement BiomeEnums.cs
- [ ] Implement GameConstants.cs
- [ ] Implement NetworkConstants.cs
- [ ] Implement WorldConstants.cs
- [ ] Create project file
- [ ] Build SharedProtocol.Common.dll
- [ ] Update SharedProtocol.csproj reference
- [ ] Build server with new references
- [ ] Build client with new references
- [ ] Run unit tests
- [ ] Create API documentation

---

## Conclusion

The shared .dll architecture provides a **clean separation of concerns** with proper organization of shared enums and constants. This will significantly improve code maintainability and type safety across the server and client codebase.

**Status:** ✅ **ARCHITECTURE DESIGN COMPLETE - READY FOR IMPLEMENTATION**

**Next Steps:**
1. Create the project structure
2. Implement all enum and constant files
3. Update project references
4. Test compilation
5. Update documentation
**Date:** 2026-02-20  
**Session:** 102  
**Status:** Architecture Design Complete

## Executive Summary

This document provides the architecture design for a shared .dll library that contains common enums and codes shared between the server and client. This shared library ensures type safety and consistency across the codebase.

---

## Architecture Overview

### Current State

**Existing Shared Protocol Projects:**
- `SharedProtocol/` - Contains protobuf-net message definitions and protocol utilities
- `Assets/Generated/Protobuf/` - Contains Google.Protobuf generated code

**Issue:**
- No dedicated shared .dll for common enums and shared code
- Enums are scattered across multiple projects
- Type safety is not enforced at compile time

---

## Proposed Architecture

### 1. Shared Enums Library

**Project Name:** `SharedProtocol.Common`

**Location:** `SharedProtocol/Common/`

**Purpose:** Centralize all shared enums and constants used by both server and client

#### Enum Categories

| Category | Enums | Description |
|----------|-------|-------------|
| Core | `MessageType`, `ChatType`, `RoomRole`, `RoomVisibility`, `RoomStatus` | Core protocol enums |
| Game | `GameMode`, `Difficulty`, `EntityType`, `BlockFace` | Gameplay enums |
| Items | `ItemType`, `ItemRarity`, `ToolType` | Item-related enums |
| Combat | `DamageType`, `PlayerAction` | Combat enums |
| World | `WorldType`, `WeatherType`, `ChunkUnloadReason`, `SpawnReason`, `DespawnReason` | World enums |
| Biome | `BiomeType` | Biome enums |
| Block | `BlockType` | Block type enums |

---

### 2. Project Structure

```
SharedProtocol/
├── Common/
│   ├── Enums/
│   │   ├── CoreEnums.cs
│   │   ├── GameEnums.cs
│   │   ├── ItemEnums.cs
│   │   ├── CombatEnums.cs
│   │   ├── WorldEnums.cs
│   │   └── BiomeEnums.cs
│   ├── Constants/
│   │   ├── GameConstants.cs
│   │   ├── NetworkConstants.cs
│   │   └── WorldConstants.cs
│   ├── Interfaces/
│   │   ├── ISharedProtocol.cs
│   │   └── ISharedConstants.cs
│   └── SharedProtocol.Common.csproj
├── EnhancedMinecraft/
│   ├── ProtocolRegistry.cs (existing)
│   ├── ProtocolValidator.cs (existing)
│   ├── ProtocolStandardization.cs (existing)
│   ├── ProtoDiagnostics.cs (existing)
│   ├── ProtoFingerprint.cs (existing)
│   ├── ProtoRuntime.cs (existing)
│   └── UnifiedMessageHandler.cs (existing)
├── Messages.cs (existing - protobuf-net)
└── SharedProtocol.csproj (existing)
```

---

### 3. Enum Definitions

#### Core Enums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Core protocol enumeration types
/// </summary>
public static class CoreEnums
{
    /// <summary>
    /// Message types for client-server communication
    /// </summary>
    public enum MessageType
    {
        // Authentication
        LoginRequest = 1,
        LoginResponse = 2,
        LogoutRequest = 3,
        LogoutResponse = 4,
        
        // Movement
        MoveRequest = 10,
        MoveResponse = 11,
        
        // World/Blocks
        WorldBlockChangeRequest = 20,
        WorldBlockChangeResponse = 21,
        WorldBlockChangeBroadcast = 22,
        
        // Chat
        ChatRequest = 30,
        ChatResponse = 31,
        ChatMessage = 32,
        
        // Server Status
        PingRequest = 40,
        PingResponse = 41,
        ServerStatusRequest = 42,
        ServerStatusResponse = 43,
        
        // Player
        PlayerInfoUpdate = 50,
        
        // Inventory
        InventoryRequest = 60,
        InventoryResponse = 61,
        InventoryUpdateBroadcast = 62,
        
        // Crafting
        CraftingRequest = 70,
        CraftingResponse = 71,
        RecipeListRequest = 72,
        RecipeListResponse = 73,
        
        // Health
        HealthActionRequest = 80,
        HealthActionResponse = 81,
        HealthUpdate = 82,
        RespawnRequest = 83,
        RespawnResponse = 84,
        PlayerDeath = 85,
        PlayerRespawnBroadcast = 86,
        CombatEvent = 87,
        
        // Room/Lobby
        RoomListRequest = 90,
        RoomListResponse = 91,
        RoomEnterRequest = 92,
        RoomEnterResponse = 93,
        RoomLeaveRequest = 94,
        RoomLeaveResponse = 95,
        RoomQueueUpdate = 96,
        RoomPromotionNotice = 97,
        
        // AI System
        AIStateSyncBroadcast = 100,
        AIAttackEventBroadcast = 101,
        AIDeathEventBroadcast = 102,
        AISpawnRequest = 103,
        AISpawnResponse = 104,
        AIDebugInfoRequest = 105,
        AIDebugInfoResponse = 106,
        
        // Combat System
        PlayerAttackRequest = 110,
        PlayerAttackResponse = 111,
        PlayerAttackBroadcast = 112,
        
        // Commands
        CommandRequest = 120,
        CommandResponse = 121,
        CommandBroadcast = 122
    }
    
    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3,
        Team = 4,
        Announcement = 5,
        Death = 6,
        JoinLeave = 7,
        Achievement = 8,
        CommandResult = 9
    }
    
    /// <summary>
    /// Room visibility settings
    /// </summary>
    public enum RoomVisibility
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2
    }
    
    /// <summary>
    /// Room status
    /// </summary>
    public enum RoomStatus
    {
        Waiting = 0,
        InGame = 1,
        Completed = 2,
        Locked = 3
    }
    
    /// <summary>
    /// Room member roles
    /// </summary>
    public enum RoomRole
    {
        Player = 0,
        Host = 1,
        Moderator = 2,
        Spectator = 3,
        Queue = 4
    }
}
```

#### GameEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Gameplay enumeration types
/// </summary>
public static class GameEnums
{
    /// <summary>
    /// Game modes
    /// </summary>
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }
    
    /// <summary>
    /// Difficulty levels
    /// </summary>
    public enum Difficulty
    {
        Peaceful = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3
    }
    
    /// <summary>
    /// Entity types
    /// </summary>
    public enum EntityType
    {
        Unknown = 0,
        Player = 1,
        // Hostile mobs
        Zombie = 10,
        Skeleton = 11,
        Creeper = 12,
        Spider = 13,
        Enderman = 14,
        Witch = 15,
        Slime = 16,
        // Neutral/Passive mobs
        Pig = 20,
        Cow = 21,
        Sheep = 22,
        Chicken = 23,
        Horse = 24,
        Wolf = 25,
        Cat = 26,
        Villager = 27,
        // Other
        DroppedItem = 30,
        Arrow = 31,
        ExperienceOrb = 32,
        Boat = 33,
        Minecart = 34,
        Fireball = 35
    }
}
```

#### ItemEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Item-related enumeration types
/// </summary>
public static class ItemEnums
{
    /// <summary>
    /// Item categories
    /// </summary>
    public enum ItemCategory
    {
        Block = 0,
        Tool = 1,
        Weapon = 2,
        Armor = 3,
        Food = 4,
        Material = 5,
        Potion = 6,
        Misc = 7
    }
    
    /// <summary>
    /// Item rarity levels
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }
    
    /// <summary>
    /// Tool types
    /// </summary>
    public enum ToolType
    {
        None = 0,
        Hand = 1,
        Sword = 2,
        Axe = 3,
        Pickaxe = 4,
        Shovel = 5,
        Hoe = 6,
        Bow = 7,
        FishingRod = 8
    }
}
```

#### CombatEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Combat-related enumeration types
/// </summary>
public static class CombatEnums
{
    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction
    {
        // Block actions
        StartDestroyBlock = 0,
        AbortDestroyBlock = 1,
        FinishDestroyBlock = 2,
        PlaceBlock = 3,
        RightClickBlock = 4,
        
        // Item actions
        UseItem = 10,
        DropItem = 11,
        DropItemStack = 12,
        EatFood = 13,
        DrinkPotion = 14,
        
        // Combat actions
        AttackEntity = 20,
        ShootBow = 21,
        BlockWithShield = 22,
        
        // Movement
        Interact = 30,
        SneakStart = 31,
        SneakStop = 32,
        SprintStart = 33,
        SprintStop = 34,
        Jump = 35
    }
    
    /// <summary>
    /// Damage types
    /// </summary>
    public enum DamageType
    {
        Generic = 0,
        EntityAttack = 1,
        Projectile = 2,
        Fall = 3,
        Fire = 4,
        FireTick = 5,
        Lava = 6,
        Drowning = 7,
        Suffocation = 8,
        Explosion = 9,
        Void = 10,
        Poison = 11,
        Magic = 12,
        Wither = 13,
        Anvil = 14,
        Cactus = 15,
        Lightning = 16,
        Starvation = 17
    }
}
```

#### WorldEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// World-related enumeration types
/// </summary>
public static class WorldEnums
{
    /// <summary>
    /// World generation types
    /// </summary>
    public enum WorldType
    {
        Normal = 0,
        Flat = 1,
        LargeBiomes = 2,
        Amplified = 3,
        Debug = 4,
        Custom = 5
    }
    
    /// <summary>
    /// Weather types
    /// </summary>
    public enum WeatherType
    {
        Clear = 0,
        Rain = 1,
        Storm = 2,
        Snow = 3
    }
    
    /// <summary>
    /// Chunk unload reasons
    /// </summary>
    public enum ChunkUnloadReason
    {
        UnloadViewDistance = 0,
        UnloadManual = 1,
        UnloadWorldTransfer = 2,
        UnloadShutdown = 3
    }
    
    /// <summary>
    /// Entity spawn reasons
    /// </summary>
    public enum SpawnReason
    {
        Natural = 0,
        Spawner = 1,
        Breeding = 2,
        Command = 3,
        ItemDrop = 4,
        Projectile = 5
    }
    
    /// <summary>
    /// Entity despawn reasons
    /// </summary>
    public enum DespawnReason
    {
        Natural = 0,
        Death = 1,
        Pickup = 2,
        ChunkUnload = 3,
        Command = 4
    }
}
```

#### BiomeEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Biome-related enumeration types
/// </summary>
public static class BiomeEnums
{
    /// <summary>
    /// Biome types
    /// </summary>
    public enum BiomeType
    {
        Plains = 0,
        Desert = 1,
        Forest = 2,
        Taiga = 3,
        Swamp = 4,
        Mountains = 5,
        Ocean = 6,
        River = 7,
        Beach = 8
    }
    
    /// <summary>
    /// Block face directions
    /// </summary>
    public enum BlockFace
    {
        Top = 0,
        Bottom = 1,
        Front = 2,
        Back = 3,
        Left = 4,
        Right = 5
    }
}
```

---

### 4. Constants

#### GameConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Game-related constants
/// </summary>
public static class GameConstants
{
    public const int ChunkSize = 16;
    public const int WorldHeight = 256;
    public const int SeaLevel = 62;
    public const int BedrockLevel = 5;
    public const int MaxPlayersPerWorld = 20;
    public const int DefaultViewDistance = 8;
    public const int MaxViewDistance = 16;
    public const int MaxInventorySlots = 36;
    public const int HotbarSlots = 9;
    public const float PlayerHeight = 1.8f;
    public const float PlayerWidth = 0.6f;
    public const float PlayerDepth = 0.6f;
    public const float PlayerReachDistance = 4.5f;
}
```

#### NetworkConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Network-related constants
/// </summary>
public static class NetworkConstants
{
    public const int DefaultServerPort = 9000;
    public const string DefaultServerAddress = "127.0.0.1";
    public const int DefaultConnectionTimeoutMs = 10000;
    public const int DefaultMaxPacketSize = 1048576;
    public const int DefaultCompressionThreshold = 1024;
    public const int DefaultHeartbeatIntervalMs = 30000;
    public const int DefaultMaxConnections = 100;
}
```

#### WorldConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// World-related constants
/// </summary>
public static class WorldConstants
{
    public const int DayTimeTicks = 24000;
    public const int DefaultDayTime = 1000;
    public const int ClearWeatherDurationTicks = 360 * 20; // 7200 ticks
    public const int RainWeatherDurationTicks = 180 * 20; // 3600 ticks
    public const int StormWeatherDurationTicks = 120 * 20; // 2400 ticks
    public const int SnowWeatherDurationTicks = 240 * 20; // 4800 ticks
    public const float WeatherStormProbability = 0.1f;
    public const float WeatherSnowProbability = 0.05f;
}
```

---

### 5. Project File: SharedProtocol.Common.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>false</WarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System" />
    <PackageReference Include="System.Runtime.Serialization" />
  </ItemGroup>

  <PropertyGroup>
    <OutputPath>bin\$(Configuration)\</OutputPath>
    <DocumentationFile>bin\$(Configuration)\SharedProtocol.Common.xml</DocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Enums\**\*.cs" />
    <Compile Include="Constants\**\*.cs" />
    <Compile Include="Interfaces\**\*.cs" />
  </ItemGroup>
</Project>
```

---

## Implementation Steps

### Phase 1: Create Project Structure
1. Create `SharedProtocol/Common/` directory
2. Create enum files
3. Create constant files
4. Create interface files
5. Create project file

### Phase 2: Update Existing Projects
1. Update `SharedProtocol.csproj` to reference new Common project
2. Update server project to reference SharedProtocol.Common
3. Update client code to use SharedProtocol.Common enums
4. Remove duplicate enum definitions

### Phase 3: Testing
1. Build SharedProtocol.Common project
2. Build SharedProtocol project
3. Build GameServer project
4. Run unit tests

### Phase 4: Documentation
1. Create API documentation
2. Create migration guide
3. Update README.md

---

## Benefits

### Type Safety
- Compile-time checking of enum values
- IDE autocomplete support
- Refactoring safety

### Consistency
- Single source of truth for shared values
- Easier to maintain
- Reduces bugs from mismatched enums

### Maintainability
- Centralized location for changes
- Clear separation of concerns
- Easier to add new shared enums

---

## Migration Guide

### For Server Code
```csharp
// Before:
using SharedProtocol;

// After:
using SharedProtocol;
using SharedProtocol.Common.Enums;
using SharedProtocol.Common.Constants;

// Replace enum references:
MessageType.LoginRequest -> CoreEnums.MessageType.LoginRequest
ChatType.Global -> CoreEnums.ChatType.Global
GameMode.Survival -> GameEnums.GameMode.Survival
// ... and so on
```

### For Client Code
```csharp
// Before:
using SharedProtocol;

// After:
using SharedProtocol;
using SharedProtocol.Common.Enums;
using SharedProtocol.Common.Constants;

// Replace enum references:
MessageType.LoginRequest -> CoreEnums.MessageType.LoginRequest
ChatType.Global -> CoreEnums.ChatType.Global
GameMode.Survival -> GameEnums.GameMode.Survival
// ... and so on
```

---

## Testing Checklist

- [ ] Create SharedProtocol.Common project structure
- [ ] Implement CoreEnums.cs
- [ ] Implement GameEnums.cs
- [ ] Implement ItemEnums.cs
- [ ] Implement CombatEnums.cs
- [ ] Implement WorldEnums.cs
- [ ] Implement BiomeEnums.cs
- [ ] Implement GameConstants.cs
- [ ] Implement NetworkConstants.cs
- [ ] Implement WorldConstants.cs
- [ ] Create project file
- [ ] Build SharedProtocol.Common.dll
- [ ] Update SharedProtocol.csproj reference
- [ ] Build server with new references
- [ ] Build client with new references
- [ ] Run unit tests
- [ ] Create API documentation

---

## Conclusion

The shared .dll architecture provides a **clean separation of concerns** with proper organization of shared enums and constants. This will significantly improve code maintainability and type safety across the server and client codebase.

**Status:** ✅ **ARCHITECTURE DESIGN COMPLETE - READY FOR IMPLEMENTATION**

**Next Steps:**
1. Create the project structure
2. Implement all enum and constant files
3. Update project references
4. Test compilation
5. Update documentation

