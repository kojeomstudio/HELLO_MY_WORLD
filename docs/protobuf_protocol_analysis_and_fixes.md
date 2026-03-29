# Protobuf Protocol Analysis and Fixes

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Analysis of protobuf protocol implementation and recommended fixes

---

## Overview

This document analyzes the current protobuf protocol implementation, identifies issues with mixed protocol usage, and provides recommendations for standardizing on Google.Protobuf across server and client.

---

## Current Protocol Status

### Protocol Implementations

| Protocol Type | Location | Status | Description |
|---------------|-----------|---------|-------------|
| Legacy Protobuf-Net | `SharedProtocol/GameProtocol.cs` | ⚠️ Mixed | Old protocol with ProtoContract attributes |
| Google.Protobuf | `Assets/Generated/Protobuf/` | ✅ Generated | New protocol using Google.Protobuf |
| Mixed Usage | Server code | ⚠️ Inconsistent | Server uses legacy, client uses new |

### Generated Protobuf Files

| File | Namespace | Messages | Status |
|------|-----------|----------|---------|
| `Common.cs` | `MinecraftGame.Common` | Vector3, Vector3Int, Vector2, Vector2Int, Color, Timestamp, BaseResponse, enums | ✅ Generated |
| `GameCore.cs` | `Game.Core` | InventoryItem, PlayerInfo | ✅ Generated |
| `GameMove.cs` | `Game.Move` | MoveRequest, MoveResponse | ✅ Generated |
| `GameChat.cs` | `Game.Chat` | ChatRequest, ChatResponse, ChatMessage | ✅ Generated |
| `GameDiag.cs` | `Game.Diag` | PingRequest, PingResponse | ✅ Generated |
| `GameWorld.cs` | `Game.World` | WorldBlockChangeRequest, WorldBlockChangeResponse, WorldBlockChangeBroadcast, ChunkDataRequest, ChunkDataResponse | ✅ Generated |
| `GameAuth.cs` | `Game.Auth` | LoginRequest, LoginResponse | ✅ Generated |
| `EnhancedMinecraftGame.cs` | `EnhancedMinecraftGame` | PlayerInfo, PlayerStats, PlayerInventory, InventorySlot, ItemStack, Enchantment, BlockOperation, ChunkDataRequest, ChunkDataResponse, ChunkUnloadNotification, TileEntity, Entity, PlayerAction, CraftingType, RecipeType, DamageType, EffectType, ParticleType, SoundType, ChatType, CommandResultType, WorldType, WorldDifficulty, WeatherType, AchievementType, StatisticCategory | ✅ Generated |

---

## Issues Identified

### 1. Mixed Protocol Usage

**Problem**: Server code uses legacy `SharedProtocol.Vector3` while client code uses new `Game.Core.Vector3` from Google.Protobuf.

**Impact**: 
- Incompatibility between server and client
- Cannot serialize/deserialize messages correctly
- Type mismatches cause runtime errors

**Evidence**:
```csharp
// Server code (GameServer/Handlers/MovementHandler.cs:71)
var currentPos = new SharedProtocol.Vector3((float)playerState.Position.X, (float)playerState.Position.Y, (float)playerState.Position.Z);

// Client code (Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:237)
var req = new Game.Move.MoveRequest
{
    TargetPosition = new Game.Core.Vector3
    {
        X = x,
        Y = y,
        Z = z
    }
};
```

### 2. Missing Using Statements

**Problem**: Server code doesn't have using statements for Google.Protobuf namespaces.

**Impact**: Cannot reference new protobuf types without full namespace paths.

**Evidence**:
```csharp
// Server code (GameServer/Handlers/MovementHandler.cs)
// Missing: using Game.Core;
// Missing: using Game.Move;
// Missing: using Game.Chat;
// Missing: using Game.World;
```

### 3. Inconsistent Type References

**Problem**: Different Vector3 types are used in different parts of the codebase.

**Impact**: Type conversion issues and compilation errors when trying to use the new protocol.

**Evidence**:
```csharp
// Legacy type (SharedProtocol/GameProtocol.cs:25)
[ProtoContract]
public class Vector3
{
    [ProtoMember(1)]
    public float X { get; set; }
    // ...
}

// New type (Assets/Generated/Protobuf/Common.cs:131)
public sealed partial class Vector3 : pb::IMessage<Vector3>
{
    public double X { get; set; }
    // ...
}
```

### 4. Handler Coverage Gaps

**Problem**: Not all protobuf message types have corresponding handlers.

**Impact**: Some messages cannot be processed by the server.

**Evidence**:
- `EnhancedMinecraftGame.cs` defines many message types that may not have handlers
- ProtocolValidator may not cover all message types

---

## Recommended Fixes

### 1. Standardize on Google.Protobuf

**Priority**: Critical

**Action**: Migrate all server code from `SharedProtocol` to Google.Protobuf namespaces.

**Steps**:
1. Add using statements for Google.Protobuf namespaces:
   ```csharp
   using Game.Core;
   using Game.Move;
   using Game.Chat;
   using Game.World;
   using Game.Auth;
   using Game.Diag;
   using EnhancedMinecraftGame;
   ```

2. Replace `SharedProtocol.Vector3` with `Game.Core.Vector3`:
   ```csharp
   // Before:
   var pos = new SharedProtocol.Vector3(x, y, z);
   
   // After:
   var pos = new Game.Core.Vector3 { X = x, Y = y, Z = z };
   ```

3. Replace `SharedProtocol.Vector3Int` with `Game.Core.Vector3Int`:
   ```csharp
   // Before:
   var pos = new SharedProtocol.Vector3Int(x, y, z);
   
   // After:
   var pos = new Game.Core.Vector3Int { X = x, Y = y, Z = z };
   ```

### 2. Update Handler Signatures

**Priority**: High

**Action**: Update handler method signatures to use Google.Protobuf types.

**Example**:
```csharp
// Before (GameServer/Handlers/MovementHandler.cs:136)
private async Task<bool> ValidateMovement(SharedProtocol.Vector3 currentPos, SharedProtocol.Vector3 targetPos, float movementSpeed)

// After:
private async Task<bool> ValidateMovement(Game.Core.Vector3 currentPos, Game.Core.Vector3 targetPos, float movementSpeed)
```

### 3. Update Message Construction

**Priority**: High

**Action**: Update message construction to use Google.Protobuf message types.

**Example**:
```csharp
// Before (GameServer/Handlers/MovementHandler.cs:81)
var newPositionClient = new SharedProtocol.Vector3(targetPos.X, targetPos.Y, targetPos.Z);

// After:
var newPositionClient = new Game.Core.Vector3 
{ 
    X = targetPos.X, 
    Y = targetPos.Y, 
    Z = targetPos.Z 
};
```

### 4. Create Adapter Layer (Transition Strategy)

**Priority**: Medium

**Action**: Create adapter classes to bridge between legacy and new protocols during migration.

**Example**:
```csharp
namespace GameServerApp.Adapters
{
    /// <summary>
    /// Adapter for converting between legacy and new protocol types.
    /// </summary>
    public static class ProtocolAdapter
    {
        public static Game.Core.Vector3 ToCoreVector3(SharedProtocol.Vector3 legacy)
        {
            return new Game.Core.Vector3
            {
                X = legacy.X,
                Y = legacy.Y,
                Z = legacy.Z
            };
        }
        
        public static SharedProtocol.Vector3 ToLegacyVector3(Game.Core.Vector3 core)
        {
            return new SharedProtocol.Vector3
            {
                X = core.X,
                Y = core.Y,
                Z = core.Z
            };
        }
        
        public static Game.Core.Vector3Int ToCoreVector3Int(SharedProtocol.Vector3Int legacy)
        {
            return new Game.Core.Vector3Int
            {
                X = legacy.X,
                Y = legacy.Y,
                Z = legacy.Z
            };
        }
        
        public static SharedProtocol.Vector3Int ToLegacyVector3Int(Game.Core.Vector3Int core)
        {
            return new SharedProtocol.Vector3Int
            {
                X = core.X,
                Y = core.Y,
                Z = core.Z
            };
        }
    }
}
```

### 5. Update Protocol Validation

**Priority**: High

**Action**: Update ProtocolValidator to validate Google.Protobuf messages.

**Example**:
```csharp
// Add validation for Google.Protobuf message types
public static class ProtocolValidator
{
    public static void ValidateCoreContracts()
    {
        // Validate Game.Core messages
        var coreDescriptor = Game.Core.GameCoreReflection.Descriptor;
        ValidateDescriptor(coreDescriptor);
    }
    
    public static void ValidateMoveContracts()
    {
        // Validate Game.Move messages
        var moveDescriptor = Game.Move.GameMoveReflection.Descriptor;
        ValidateDescriptor(moveDescriptor);
    }
    
    public static void ValidateChatContracts()
    {
        // Validate Game.Chat messages
        var chatDescriptor = Game.Chat.GameChatReflection.Descriptor;
        ValidateDescriptor(chatDescriptor);
    }
    
    public static void ValidateWorldContracts()
    {
        // Validate Game.World messages
        var worldDescriptor = Game.World.GameWorldReflection.Descriptor;
        ValidateDescriptor(worldDescriptor);
    }
    
    public static void ValidateAuthContracts()
    {
        // Validate Game.Auth messages
        var authDescriptor = Game.Auth.GameAuthReflection.Descriptor;
        ValidateDescriptor(authDescriptor);
    }
    
    public static void ValidateDiagContracts()
    {
        // Validate Game.Diag messages
        var diagDescriptor = Game.Diag.GameDiagReflection.Descriptor;
        ValidateDescriptor(diagDescriptor);
    }
    
    public static void ValidateEnhancedContracts()
    {
        // Validate EnhancedMinecraftGame messages
        var enhancedDescriptor = EnhancedMinecraftGame.EnhancedMinecraftGameReflection.Descriptor;
        ValidateDescriptor(enhancedDescriptor);
    }
}
```

### 6. Update Protocol Registry

**Priority**: High

**Action**: Update ProtocolRegistry to register Google.Protobuf message types.

**Example**:
```csharp
public static class ProtocolRegistry
{
    public static void EnsureRegistered(MinecraftMessageType messageType)
    {
        // Check if handler exists for each message type
        switch (messageType)
        {
            case MinecraftMessageType.MoveRequest:
                EnsureHandlerExists<MoveRequestHandler>();
                break;
            case MinecraftMessageType.ChatRequest:
                EnsureHandlerExists<ChatRequestHandler>();
                break;
            case MinecraftMessageType.WorldBlockChangeRequest:
                EnsureHandlerExists<WorldBlockChangeRequestHandler>();
                break;
            // ... other message types
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Analysis and Planning (Current)
1. ✅ Analyze current protocol usage
2. ✅ Identify all files using legacy protocol
3. ✅ Document type mismatches
4. ✅ Create migration plan

### Phase 2: Server-Side Migration (High Priority)
1. Add using statements for Google.Protobuf namespaces
2. Update MovementHandler to use Game.Core.Vector3
3. Update ChatHandler to use Game.Chat types
4. Update WorldBlockHandler to use Game.World types
5. Update LoginHandler to use Game.Auth types
6. Update all other handlers
7. Test server compilation

### Phase 3: Client-Side Verification (Medium Priority)
1. Verify client uses Google.Protobuf consistently
2. Check for any legacy protocol usage
3. Update client if needed
4. Test client-server communication

### Phase 4: Protocol Validation (High Priority)
1. Update ProtocolValidator for Google.Protobuf
2. Add handler coverage checks
3. Validate all message types
4. Test validation at startup

### Phase 5: Testing and Validation (High Priority)
1. Test message serialization/deserialization
2. Test client-server message flow
3. Test error handling
4. Performance testing

---

## Files Requiring Updates

### Server Files

| File | Required Changes |
|------|-----------------|
| `GameServer/Handlers/MovementHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Handlers/ChatHandler.cs` | Add using Game.Chat, update message types |
| `GameServer/Handlers/WorldBlockHandler.cs` | Add using Game.World, update message types |
| `GameServer/Handlers/LoginHandler.cs` | Add using Game.Auth, update message types |
| `GameServer/Handlers/HealthHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Handlers/PlayerAttackHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Systems/EntitySyncService.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Systems/HealthAndHungerSystem.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/TestClient.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |

### Client Files

| File | Required Changes |
|------|-----------------|
| `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs` | Verify using statements, check for legacy types |
| `Assets/Scripts/Networking/NetworkManager.cs` | Verify using statements, check for legacy types |
| `Assets/Scripts/Minecraft/World/WorldManager.cs` | Verify using statements, check for legacy types |

---

## Testing Checklist

### Unit Tests
- [ ] Test Vector3 type conversion
- [ ] Test message serialization
- [ ] Test message deserialization
- [ ] Test handler registration
- [ ] Test protocol validation

### Integration Tests
- [ ] Test client-server login flow
- [ ] Test client-server movement flow
- [ ] Test client-server chat flow
- [ ] Test client-server block change flow
- [ ] Test client-server chunk loading flow

### Performance Tests
- [ ] Measure serialization overhead
- [ ] Measure deserialization overhead
- [ ] Test with multiple concurrent clients
- [ ] Profile memory usage

---

## Notes

- The migration should be done incrementally to minimize risk
- Consider maintaining backward compatibility during transition
- The adapter layer can help with gradual migration
- All changes should be tested thoroughly before committing
- Protocol versioning should be considered for future compatibility

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Analysis of protobuf protocol implementation and recommended fixes

---

## Overview

This document analyzes the current protobuf protocol implementation, identifies issues with mixed protocol usage, and provides recommendations for standardizing on Google.Protobuf across server and client.

---

## Current Protocol Status

### Protocol Implementations

| Protocol Type | Location | Status | Description |
|---------------|-----------|---------|-------------|
| Legacy Protobuf-Net | `SharedProtocol/GameProtocol.cs` | ⚠️ Mixed | Old protocol with ProtoContract attributes |
| Google.Protobuf | `Assets/Generated/Protobuf/` | ✅ Generated | New protocol using Google.Protobuf |
| Mixed Usage | Server code | ⚠️ Inconsistent | Server uses legacy, client uses new |

### Generated Protobuf Files

| File | Namespace | Messages | Status |
|------|-----------|----------|---------|
| `Common.cs` | `MinecraftGame.Common` | Vector3, Vector3Int, Vector2, Vector2Int, Color, Timestamp, BaseResponse, enums | ✅ Generated |
| `GameCore.cs` | `Game.Core` | InventoryItem, PlayerInfo | ✅ Generated |
| `GameMove.cs` | `Game.Move` | MoveRequest, MoveResponse | ✅ Generated |
| `GameChat.cs` | `Game.Chat` | ChatRequest, ChatResponse, ChatMessage | ✅ Generated |
| `GameDiag.cs` | `Game.Diag` | PingRequest, PingResponse | ✅ Generated |
| `GameWorld.cs` | `Game.World` | WorldBlockChangeRequest, WorldBlockChangeResponse, WorldBlockChangeBroadcast, ChunkDataRequest, ChunkDataResponse | ✅ Generated |
| `GameAuth.cs` | `Game.Auth` | LoginRequest, LoginResponse | ✅ Generated |
| `EnhancedMinecraftGame.cs` | `EnhancedMinecraftGame` | PlayerInfo, PlayerStats, PlayerInventory, InventorySlot, ItemStack, Enchantment, BlockOperation, ChunkDataRequest, ChunkDataResponse, ChunkUnloadNotification, TileEntity, Entity, PlayerAction, CraftingType, RecipeType, DamageType, EffectType, ParticleType, SoundType, ChatType, CommandResultType, WorldType, WorldDifficulty, WeatherType, AchievementType, StatisticCategory | ✅ Generated |

---

## Issues Identified

### 1. Mixed Protocol Usage

**Problem**: Server code uses legacy `SharedProtocol.Vector3` while client code uses new `Game.Core.Vector3` from Google.Protobuf.

**Impact**: 
- Incompatibility between server and client
- Cannot serialize/deserialize messages correctly
- Type mismatches cause runtime errors

**Evidence**:
```csharp
// Server code (GameServer/Handlers/MovementHandler.cs:71)
var currentPos = new SharedProtocol.Vector3((float)playerState.Position.X, (float)playerState.Position.Y, (float)playerState.Position.Z);

// Client code (Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs:237)
var req = new Game.Move.MoveRequest
{
    TargetPosition = new Game.Core.Vector3
    {
        X = x,
        Y = y,
        Z = z
    }
};
```

### 2. Missing Using Statements

**Problem**: Server code doesn't have using statements for Google.Protobuf namespaces.

**Impact**: Cannot reference new protobuf types without full namespace paths.

**Evidence**:
```csharp
// Server code (GameServer/Handlers/MovementHandler.cs)
// Missing: using Game.Core;
// Missing: using Game.Move;
// Missing: using Game.Chat;
// Missing: using Game.World;
```

### 3. Inconsistent Type References

**Problem**: Different Vector3 types are used in different parts of the codebase.

**Impact**: Type conversion issues and compilation errors when trying to use the new protocol.

**Evidence**:
```csharp
// Legacy type (SharedProtocol/GameProtocol.cs:25)
[ProtoContract]
public class Vector3
{
    [ProtoMember(1)]
    public float X { get; set; }
    // ...
}

// New type (Assets/Generated/Protobuf/Common.cs:131)
public sealed partial class Vector3 : pb::IMessage<Vector3>
{
    public double X { get; set; }
    // ...
}
```

### 4. Handler Coverage Gaps

**Problem**: Not all protobuf message types have corresponding handlers.

**Impact**: Some messages cannot be processed by the server.

**Evidence**:
- `EnhancedMinecraftGame.cs` defines many message types that may not have handlers
- ProtocolValidator may not cover all message types

---

## Recommended Fixes

### 1. Standardize on Google.Protobuf

**Priority**: Critical

**Action**: Migrate all server code from `SharedProtocol` to Google.Protobuf namespaces.

**Steps**:
1. Add using statements for Google.Protobuf namespaces:
   ```csharp
   using Game.Core;
   using Game.Move;
   using Game.Chat;
   using Game.World;
   using Game.Auth;
   using Game.Diag;
   using EnhancedMinecraftGame;
   ```

2. Replace `SharedProtocol.Vector3` with `Game.Core.Vector3`:
   ```csharp
   // Before:
   var pos = new SharedProtocol.Vector3(x, y, z);
   
   // After:
   var pos = new Game.Core.Vector3 { X = x, Y = y, Z = z };
   ```

3. Replace `SharedProtocol.Vector3Int` with `Game.Core.Vector3Int`:
   ```csharp
   // Before:
   var pos = new SharedProtocol.Vector3Int(x, y, z);
   
   // After:
   var pos = new Game.Core.Vector3Int { X = x, Y = y, Z = z };
   ```

### 2. Update Handler Signatures

**Priority**: High

**Action**: Update handler method signatures to use Google.Protobuf types.

**Example**:
```csharp
// Before (GameServer/Handlers/MovementHandler.cs:136)
private async Task<bool> ValidateMovement(SharedProtocol.Vector3 currentPos, SharedProtocol.Vector3 targetPos, float movementSpeed)

// After:
private async Task<bool> ValidateMovement(Game.Core.Vector3 currentPos, Game.Core.Vector3 targetPos, float movementSpeed)
```

### 3. Update Message Construction

**Priority**: High

**Action**: Update message construction to use Google.Protobuf message types.

**Example**:
```csharp
// Before (GameServer/Handlers/MovementHandler.cs:81)
var newPositionClient = new SharedProtocol.Vector3(targetPos.X, targetPos.Y, targetPos.Z);

// After:
var newPositionClient = new Game.Core.Vector3 
{ 
    X = targetPos.X, 
    Y = targetPos.Y, 
    Z = targetPos.Z 
};
```

### 4. Create Adapter Layer (Transition Strategy)

**Priority**: Medium

**Action**: Create adapter classes to bridge between legacy and new protocols during migration.

**Example**:
```csharp
namespace GameServerApp.Adapters
{
    /// <summary>
    /// Adapter for converting between legacy and new protocol types.
    /// </summary>
    public static class ProtocolAdapter
    {
        public static Game.Core.Vector3 ToCoreVector3(SharedProtocol.Vector3 legacy)
        {
            return new Game.Core.Vector3
            {
                X = legacy.X,
                Y = legacy.Y,
                Z = legacy.Z
            };
        }
        
        public static SharedProtocol.Vector3 ToLegacyVector3(Game.Core.Vector3 core)
        {
            return new SharedProtocol.Vector3
            {
                X = core.X,
                Y = core.Y,
                Z = core.Z
            };
        }
        
        public static Game.Core.Vector3Int ToCoreVector3Int(SharedProtocol.Vector3Int legacy)
        {
            return new Game.Core.Vector3Int
            {
                X = legacy.X,
                Y = legacy.Y,
                Z = legacy.Z
            };
        }
        
        public static SharedProtocol.Vector3Int ToLegacyVector3Int(Game.Core.Vector3Int core)
        {
            return new SharedProtocol.Vector3Int
            {
                X = core.X,
                Y = core.Y,
                Z = core.Z
            };
        }
    }
}
```

### 5. Update Protocol Validation

**Priority**: High

**Action**: Update ProtocolValidator to validate Google.Protobuf messages.

**Example**:
```csharp
// Add validation for Google.Protobuf message types
public static class ProtocolValidator
{
    public static void ValidateCoreContracts()
    {
        // Validate Game.Core messages
        var coreDescriptor = Game.Core.GameCoreReflection.Descriptor;
        ValidateDescriptor(coreDescriptor);
    }
    
    public static void ValidateMoveContracts()
    {
        // Validate Game.Move messages
        var moveDescriptor = Game.Move.GameMoveReflection.Descriptor;
        ValidateDescriptor(moveDescriptor);
    }
    
    public static void ValidateChatContracts()
    {
        // Validate Game.Chat messages
        var chatDescriptor = Game.Chat.GameChatReflection.Descriptor;
        ValidateDescriptor(chatDescriptor);
    }
    
    public static void ValidateWorldContracts()
    {
        // Validate Game.World messages
        var worldDescriptor = Game.World.GameWorldReflection.Descriptor;
        ValidateDescriptor(worldDescriptor);
    }
    
    public static void ValidateAuthContracts()
    {
        // Validate Game.Auth messages
        var authDescriptor = Game.Auth.GameAuthReflection.Descriptor;
        ValidateDescriptor(authDescriptor);
    }
    
    public static void ValidateDiagContracts()
    {
        // Validate Game.Diag messages
        var diagDescriptor = Game.Diag.GameDiagReflection.Descriptor;
        ValidateDescriptor(diagDescriptor);
    }
    
    public static void ValidateEnhancedContracts()
    {
        // Validate EnhancedMinecraftGame messages
        var enhancedDescriptor = EnhancedMinecraftGame.EnhancedMinecraftGameReflection.Descriptor;
        ValidateDescriptor(enhancedDescriptor);
    }
}
```

### 6. Update Protocol Registry

**Priority**: High

**Action**: Update ProtocolRegistry to register Google.Protobuf message types.

**Example**:
```csharp
public static class ProtocolRegistry
{
    public static void EnsureRegistered(MinecraftMessageType messageType)
    {
        // Check if handler exists for each message type
        switch (messageType)
        {
            case MinecraftMessageType.MoveRequest:
                EnsureHandlerExists<MoveRequestHandler>();
                break;
            case MinecraftMessageType.ChatRequest:
                EnsureHandlerExists<ChatRequestHandler>();
                break;
            case MinecraftMessageType.WorldBlockChangeRequest:
                EnsureHandlerExists<WorldBlockChangeRequestHandler>();
                break;
            // ... other message types
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Analysis and Planning (Current)
1. ✅ Analyze current protocol usage
2. ✅ Identify all files using legacy protocol
3. ✅ Document type mismatches
4. ✅ Create migration plan

### Phase 2: Server-Side Migration (High Priority)
1. Add using statements for Google.Protobuf namespaces
2. Update MovementHandler to use Game.Core.Vector3
3. Update ChatHandler to use Game.Chat types
4. Update WorldBlockHandler to use Game.World types
5. Update LoginHandler to use Game.Auth types
6. Update all other handlers
7. Test server compilation

### Phase 3: Client-Side Verification (Medium Priority)
1. Verify client uses Google.Protobuf consistently
2. Check for any legacy protocol usage
3. Update client if needed
4. Test client-server communication

### Phase 4: Protocol Validation (High Priority)
1. Update ProtocolValidator for Google.Protobuf
2. Add handler coverage checks
3. Validate all message types
4. Test validation at startup

### Phase 5: Testing and Validation (High Priority)
1. Test message serialization/deserialization
2. Test client-server message flow
3. Test error handling
4. Performance testing

---

## Files Requiring Updates

### Server Files

| File | Required Changes |
|------|-----------------|
| `GameServer/Handlers/MovementHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Handlers/ChatHandler.cs` | Add using Game.Chat, update message types |
| `GameServer/Handlers/WorldBlockHandler.cs` | Add using Game.World, update message types |
| `GameServer/Handlers/LoginHandler.cs` | Add using Game.Auth, update message types |
| `GameServer/Handlers/HealthHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Handlers/PlayerAttackHandler.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Systems/EntitySyncService.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/Systems/HealthAndHungerSystem.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |
| `GameServer/TestClient.cs` | Replace SharedProtocol.Vector3 with Game.Core.Vector3 |

### Client Files

| File | Required Changes |
|------|-----------------|
| `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs` | Verify using statements, check for legacy types |
| `Assets/Scripts/Networking/NetworkManager.cs` | Verify using statements, check for legacy types |
| `Assets/Scripts/Minecraft/World/WorldManager.cs` | Verify using statements, check for legacy types |

---

## Testing Checklist

### Unit Tests
- [ ] Test Vector3 type conversion
- [ ] Test message serialization
- [ ] Test message deserialization
- [ ] Test handler registration
- [ ] Test protocol validation

### Integration Tests
- [ ] Test client-server login flow
- [ ] Test client-server movement flow
- [ ] Test client-server chat flow
- [ ] Test client-server block change flow
- [ ] Test client-server chunk loading flow

### Performance Tests
- [ ] Measure serialization overhead
- [ ] Measure deserialization overhead
- [ ] Test with multiple concurrent clients
- [ ] Profile memory usage

---

## Notes

- The migration should be done incrementally to minimize risk
- Consider maintaining backward compatibility during transition
- The adapter layer can help with gradual migration
- All changes should be tested thoroughly before committing
- Protocol versioning should be considered for future compatibility

