# Protobuf Namespace Reference - 2026-01-25

## Overview

This document provides a comprehensive reference for all protobuf namespaces used in the Minecraft game project, including correct namespace mappings and usage guidelines.

## Protobuf Namespace Structure

### 1. MinecraftGame.Common

**File**: [`Common.cs`](Assets/Generated/Protobuf/Common.cs)

**Classes**:
- `Vector3` - 3D vector with float X, Y, Z
- `Vector3Int` - 3D integer vector with X, Y, Z
- `Vector2` - 2D vector with float X, Y
- `Vector2Int` - 2D integer vector with X, Y
- `Color` - RGBA color representation
- `Timestamp` - Timestamp for events
- `BaseResponse` - Base response class

**Usage**:
```csharp
using MinecraftGame.Common;

Vector3 position = new Vector3 { X = 10.0f, Y = 5.0f, Z = -3.0f };
Vector3Int blockPos = new Vector3Int { X = 10, Y = 64, Z = -3 };
```

### 2. Game.Auth

**File**: [`GameAuth.cs`](Assets/Generated/Protobuf/GameAuth.cs)

**Classes**:
- `LoginRequest` - Login credentials (username, password)
- `LoginResponse` - Login result (success, message)

**Usage**:
```csharp
using Game.Auth;

LoginRequest request = new LoginRequest
{
    Username = "player1",
    Password = "secure123"
};
```

### 3. Game.Chat

**File**: [`GameChat.cs`](Assets/Generated/Protobuf/GameChat.cs)

**Classes**:
- `ChatRequest` - Send chat message (message, type, targetPlayer)
- `ChatResponse` - Chat result (success, errorMessage)
- `ChatMessage` - Broadcast chat (senderId, senderName, message, type, timestamp)

**Usage**:
```csharp
using Game.Chat;

ChatRequest request = new ChatRequest
{
    Message = "Hello world!",
    Type = 0, // Global chat
    TargetPlayer = ""
};
```

### 4. Game.Core

**File**: [`GameCore.cs`](Assets/Generated/Protobuf/GameCore.cs)

**Classes**:
- `InventoryItem` - Item in inventory (itemId, itemName, quantity)
- `PlayerInfo` - Player data (playerId, username, position, level, health, maxHealth, inventory)

**Usage**:
```csharp
using Game.Core;
using MinecraftGame.Common;

PlayerInfo player = new PlayerInfo
{
    PlayerId = "player_001",
    Username = "Player1",
    Position = new Vector3 { X = 0, Y = 64, Z = 0 },
    Level = 5,
    Health = 20,
    MaxHealth = 20
};
```

### 5. Game.Move

**File**: [`GameMove.cs`](Assets/Generated/Protobuf/GameMove.cs)

**Classes**:
- `MoveRequest` - Movement request (targetPosition, movementSpeed)
- `MoveResponse` - Movement result (success, newPosition, timestamp)

**Usage**:
```csharp
using Game.Move;
using MinecraftGame.Common;

MoveRequest request = new MoveRequest
{
    TargetPosition = new Vector3 { X = 10, Y = 64, Z = 10 },
    MovementSpeed = 4.5f
};
```

### 6. Game.Diag

**File**: [`GameDiag.cs`](Assets/Generated/Protobuf/GameDiag.cs)

**Classes**:
- `PingRequest` - Ping for latency (clientTimestamp)
- `PingResponse` - Pong response (clientTimestamp, serverTimestamp)

**Usage**:
```csharp
using Game.Diag;

PingRequest request = new PingRequest
{
    ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
```

### 7. Game.World

**File**: [`GameWorld.cs`](Assets/Generated/Protobuf/GameWorld.cs)

**Classes**:
- `WorldBlockChangeRequest` - Block modification (areaId, subworldId, blockPosition, blockType, chunkType)
- `WorldBlockChangeResponse` - Block change result (success, message, timestamp)
- `WorldBlockChangeBroadcast` - Broadcast block change to other players
- `ChunkDataRequest` - Request chunk data (chunkX, chunkZ, viewDistance)
- `ChunkDataResponse` - Chunk data with compressed blocks (chunkX, chunkZ, success, compressedBlockData)
- `ChunkDataBroadcast` - Broadcast chunk to players
- `ChunkUnloadNotification` - Notify chunk unload
- `ChunkUnloadAck` - Acknowledge chunk unload

**Usage**:
```csharp
using Game.World;
using MinecraftGame.Common;

ChunkDataRequest request = new ChunkDataRequest
{
    ChunkX = 5,
    ChunkZ = 3,
    ViewDistance = 8
};

WorldBlockChangeRequest blockChange = new WorldBlockChangeRequest
{
    AreaId = "main_world",
    SubworldId = "surface",
    BlockPosition = new Vector3Int { X = 100, Y = 64, Z = 100 },
    BlockType = 1, // Stone
    ChunkType = 0 // Normal chunk
};
```

### 8. EnhancedMinecraftProtocol

**File**: [`EnhancedMinecraftGame.cs`](Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)

**Classes**: Enhanced protocol classes for advanced features

**Usage**:
```csharp
using EnhancedMinecraftProtocol;
```

## Namespace Dependencies

### Common Dependencies

Most protobuf classes depend on `MinecraftGame.Common` for vector types:

```csharp
using MinecraftGame.Common;  // Required for Vector3, Vector3Int, etc.
using Game.Core;            // Depends on MinecraftGame.Common
using Game.Move;            // Depends on MinecraftGame.Common
using Game.World;            // Depends on MinecraftGame.Common
```

### Google.Protobuf Base

All protobuf classes require:

```csharp
using Google.Protobuf;
```

## Correct vs Incorrect Namespace Usage

### ❌ INCORRECT

```csharp
using GameProtocol;  // Does not exist
using SharedProtocol.EnhancedMinecraft;  // Wrong namespace
using Game.Chat;  // Correct namespace, but may be misused
```

### ✅ CORRECT

```csharp
using Google.Protobuf;
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Core;
using Game.Move;
using Game.Chat;
using Game.Diag;
using Game.World;
```

## File-Specific Usage Guidelines

### LoginHandler.cs

```csharp
using Google.Protobuf;
using Game.Auth;
```

### ProtobufNetworkClient.cs

```csharp
using Google.Protobuf;
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Core;
using Game.Move;
using Game.Chat;
using Game.Diag;
using Game.World;
```

### EnhancedChunkPayloadBridge.cs

```csharp
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;
```

### EnhancedWorldMapController.cs

```csharp
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;
```

## Common Patterns

### Creating Requests

```csharp
using Game.World;
using MinecraftGame.Common;

ChunkDataRequest request = new ChunkDataRequest
{
    ChunkX = x,
    ChunkZ = z,
    ViewDistance = distance
};
```

### Handling Responses

```csharp
using Google.Protobuf;
using Game.Auth;

public void HandleLoginResponse(byte[] data)
{
    LoginResponse response = LoginResponse.Parser.ParseFrom(data);
    if (response.Success)
    {
        // Handle successful login
    }
    else
    {
        // Handle failure
    }
}
```

### Serialization

```csharp
using Google.Protobuf;
using Game.Move;

MoveRequest request = new MoveRequest { ... };
byte[] serialized = request.ToByteArray();
```

### Deserialization

```csharp
using Google.Protobuf;
using Game.Move;

MoveRequest request = MoveRequest.Parser.ParseFrom(byteArray);
```

## Verification Checklist

When using protobuf classes, verify:

- [ ] Correct `using` statements for namespaces
- [ ] Dependencies on `MinecraftGame.Common` are included
- [ ] `Google.Protobuf` is referenced
- [ ] Classes exist in the correct namespace
- [ ] Field names match protobuf definitions
- [ ] Data types are compatible

## Troubleshooting

### Error: "The type or namespace name 'X' could not be found"

**Solution**: Check namespace reference. Common issues:
- `GameProtocol` → Should be `Game.Core`
- `SharedProtocol.EnhancedMinecraft` → Should be `EnhancedMinecraftProtocol`

### Error: "Cannot implicitly convert type 'X' to 'Y'"

**Solution**: Ensure you're using the correct namespace for the type.

### Error: Missing Vector types

**Solution**: Add `using MinecraftGame.Common;`

## Summary

| Namespace | Purpose | Key Classes |
|-----------|---------|-------------|
| `MinecraftGame.Common` | Shared types | Vector3, Vector3Int, Color, Timestamp |
| `Game.Auth` | Authentication | LoginRequest, LoginResponse |
| `Game.Chat` | Chat system | ChatRequest, ChatResponse, ChatMessage |
| `Game.Core` | Core game data | InventoryItem, PlayerInfo |
| `Game.Move` | Player movement | MoveRequest, MoveResponse |
| `Game.Diag` | Diagnostics | PingRequest, PingResponse |
| `Game.World` | World/chunk data | ChunkDataRequest, WorldBlockChangeRequest |
| `EnhancedMinecraftProtocol` | Enhanced features | Various enhanced protocol classes |

## Related Documentation

- [Protobuf Protocol Analysis](protobuf_protocol_analysis.md)
- [Implementation Plan](../plans/implementation_plan_2026-01-25.md)
- [Terrain Generation Improvements](terrain_generation_improvements_2026-01-25.md)

---

**Last Updated**: 2026-01-25
**Status**: Active Reference
## Overview

This document provides a comprehensive reference for all protobuf namespaces used in the Minecraft game project, including correct namespace mappings and usage guidelines.

## Protobuf Namespace Structure

### 1. MinecraftGame.Common

**File**: [`Common.cs`](Assets/Generated/Protobuf/Common.cs)

**Classes**:
- `Vector3` - 3D vector with float X, Y, Z
- `Vector3Int` - 3D integer vector with X, Y, Z
- `Vector2` - 2D vector with float X, Y
- `Vector2Int` - 2D integer vector with X, Y
- `Color` - RGBA color representation
- `Timestamp` - Timestamp for events
- `BaseResponse` - Base response class

**Usage**:
```csharp
using MinecraftGame.Common;

Vector3 position = new Vector3 { X = 10.0f, Y = 5.0f, Z = -3.0f };
Vector3Int blockPos = new Vector3Int { X = 10, Y = 64, Z = -3 };
```

### 2. Game.Auth

**File**: [`GameAuth.cs`](Assets/Generated/Protobuf/GameAuth.cs)

**Classes**:
- `LoginRequest` - Login credentials (username, password)
- `LoginResponse` - Login result (success, message)

**Usage**:
```csharp
using Game.Auth;

LoginRequest request = new LoginRequest
{
    Username = "player1",
    Password = "secure123"
};
```

### 3. Game.Chat

**File**: [`GameChat.cs`](Assets/Generated/Protobuf/GameChat.cs)

**Classes**:
- `ChatRequest` - Send chat message (message, type, targetPlayer)
- `ChatResponse` - Chat result (success, errorMessage)
- `ChatMessage` - Broadcast chat (senderId, senderName, message, type, timestamp)

**Usage**:
```csharp
using Game.Chat;

ChatRequest request = new ChatRequest
{
    Message = "Hello world!",
    Type = 0, // Global chat
    TargetPlayer = ""
};
```

### 4. Game.Core

**File**: [`GameCore.cs`](Assets/Generated/Protobuf/GameCore.cs)

**Classes**:
- `InventoryItem` - Item in inventory (itemId, itemName, quantity)
- `PlayerInfo` - Player data (playerId, username, position, level, health, maxHealth, inventory)

**Usage**:
```csharp
using Game.Core;
using MinecraftGame.Common;

PlayerInfo player = new PlayerInfo
{
    PlayerId = "player_001",
    Username = "Player1",
    Position = new Vector3 { X = 0, Y = 64, Z = 0 },
    Level = 5,
    Health = 20,
    MaxHealth = 20
};
```

### 5. Game.Move

**File**: [`GameMove.cs`](Assets/Generated/Protobuf/GameMove.cs)

**Classes**:
- `MoveRequest` - Movement request (targetPosition, movementSpeed)
- `MoveResponse` - Movement result (success, newPosition, timestamp)

**Usage**:
```csharp
using Game.Move;
using MinecraftGame.Common;

MoveRequest request = new MoveRequest
{
    TargetPosition = new Vector3 { X = 10, Y = 64, Z = 10 },
    MovementSpeed = 4.5f
};
```

### 6. Game.Diag

**File**: [`GameDiag.cs`](Assets/Generated/Protobuf/GameDiag.cs)

**Classes**:
- `PingRequest` - Ping for latency (clientTimestamp)
- `PingResponse` - Pong response (clientTimestamp, serverTimestamp)

**Usage**:
```csharp
using Game.Diag;

PingRequest request = new PingRequest
{
    ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
```

### 7. Game.World

**File**: [`GameWorld.cs`](Assets/Generated/Protobuf/GameWorld.cs)

**Classes**:
- `WorldBlockChangeRequest` - Block modification (areaId, subworldId, blockPosition, blockType, chunkType)
- `WorldBlockChangeResponse` - Block change result (success, message, timestamp)
- `WorldBlockChangeBroadcast` - Broadcast block change to other players
- `ChunkDataRequest` - Request chunk data (chunkX, chunkZ, viewDistance)
- `ChunkDataResponse` - Chunk data with compressed blocks (chunkX, chunkZ, success, compressedBlockData)
- `ChunkDataBroadcast` - Broadcast chunk to players
- `ChunkUnloadNotification` - Notify chunk unload
- `ChunkUnloadAck` - Acknowledge chunk unload

**Usage**:
```csharp
using Game.World;
using MinecraftGame.Common;

ChunkDataRequest request = new ChunkDataRequest
{
    ChunkX = 5,
    ChunkZ = 3,
    ViewDistance = 8
};

WorldBlockChangeRequest blockChange = new WorldBlockChangeRequest
{
    AreaId = "main_world",
    SubworldId = "surface",
    BlockPosition = new Vector3Int { X = 100, Y = 64, Z = 100 },
    BlockType = 1, // Stone
    ChunkType = 0 // Normal chunk
};
```

### 8. EnhancedMinecraftProtocol

**File**: [`EnhancedMinecraftGame.cs`](Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)

**Classes**: Enhanced protocol classes for advanced features

**Usage**:
```csharp
using EnhancedMinecraftProtocol;
```

## Namespace Dependencies

### Common Dependencies

Most protobuf classes depend on `MinecraftGame.Common` for vector types:

```csharp
using MinecraftGame.Common;  // Required for Vector3, Vector3Int, etc.
using Game.Core;            // Depends on MinecraftGame.Common
using Game.Move;            // Depends on MinecraftGame.Common
using Game.World;            // Depends on MinecraftGame.Common
```

### Google.Protobuf Base

All protobuf classes require:

```csharp
using Google.Protobuf;
```

## Correct vs Incorrect Namespace Usage

### ❌ INCORRECT

```csharp
using GameProtocol;  // Does not exist
using SharedProtocol.EnhancedMinecraft;  // Wrong namespace
using Game.Chat;  // Correct namespace, but may be misused
```

### ✅ CORRECT

```csharp
using Google.Protobuf;
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Core;
using Game.Move;
using Game.Chat;
using Game.Diag;
using Game.World;
```

## File-Specific Usage Guidelines

### LoginHandler.cs

```csharp
using Google.Protobuf;
using Game.Auth;
```

### ProtobufNetworkClient.cs

```csharp
using Google.Protobuf;
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;
using Game.Auth;
using Game.Core;
using Game.Move;
using Game.Chat;
using Game.Diag;
using Game.World;
```

### EnhancedChunkPayloadBridge.cs

```csharp
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;
```

### EnhancedWorldMapController.cs

```csharp
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;
```

## Common Patterns

### Creating Requests

```csharp
using Game.World;
using MinecraftGame.Common;

ChunkDataRequest request = new ChunkDataRequest
{
    ChunkX = x,
    ChunkZ = z,
    ViewDistance = distance
};
```

### Handling Responses

```csharp
using Google.Protobuf;
using Game.Auth;

public void HandleLoginResponse(byte[] data)
{
    LoginResponse response = LoginResponse.Parser.ParseFrom(data);
    if (response.Success)
    {
        // Handle successful login
    }
    else
    {
        // Handle failure
    }
}
```

### Serialization

```csharp
using Google.Protobuf;
using Game.Move;

MoveRequest request = new MoveRequest { ... };
byte[] serialized = request.ToByteArray();
```

### Deserialization

```csharp
using Google.Protobuf;
using Game.Move;

MoveRequest request = MoveRequest.Parser.ParseFrom(byteArray);
```

## Verification Checklist

When using protobuf classes, verify:

- [ ] Correct `using` statements for namespaces
- [ ] Dependencies on `MinecraftGame.Common` are included
- [ ] `Google.Protobuf` is referenced
- [ ] Classes exist in the correct namespace
- [ ] Field names match protobuf definitions
- [ ] Data types are compatible

## Troubleshooting

### Error: "The type or namespace name 'X' could not be found"

**Solution**: Check namespace reference. Common issues:
- `GameProtocol` → Should be `Game.Core`
- `SharedProtocol.EnhancedMinecraft` → Should be `EnhancedMinecraftProtocol`

### Error: "Cannot implicitly convert type 'X' to 'Y'"

**Solution**: Ensure you're using the correct namespace for the type.

### Error: Missing Vector types

**Solution**: Add `using MinecraftGame.Common;`

## Summary

| Namespace | Purpose | Key Classes |
|-----------|---------|-------------|
| `MinecraftGame.Common` | Shared types | Vector3, Vector3Int, Color, Timestamp |
| `Game.Auth` | Authentication | LoginRequest, LoginResponse |
| `Game.Chat` | Chat system | ChatRequest, ChatResponse, ChatMessage |
| `Game.Core` | Core game data | InventoryItem, PlayerInfo |
| `Game.Move` | Player movement | MoveRequest, MoveResponse |
| `Game.Diag` | Diagnostics | PingRequest, PingResponse |
| `Game.World` | World/chunk data | ChunkDataRequest, WorldBlockChangeRequest |
| `EnhancedMinecraftProtocol` | Enhanced features | Various enhanced protocol classes |

## Related Documentation

- [Protobuf Protocol Analysis](protobuf_protocol_analysis.md)
- [Implementation Plan](../plans/implementation_plan_2026-01-25.md)
- [Terrain Generation Improvements](terrain_generation_improvements_2026-01-25.md)

---

**Last Updated**: 2026-01-25
**Status**: Active Reference

