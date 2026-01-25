# Protobuf Namespace Summary - 2026-01-25

## Correct Protobuf Namespaces

| Namespace | File | Classes |
|-----------|------|---------|
| MinecraftGame.Common | Common.cs | Vector3, Vector3Int, Vector2, Vector2Int, Color, Timestamp, BaseResponse |
| Game.Auth | GameAuth.cs | LoginRequest, LoginResponse |
| Game.Chat | GameChat.cs | ChatRequest, ChatResponse, ChatMessage |
| Game.Core | GameCore.cs | InventoryItem, PlayerInfo |
| Game.Move | GameMove.cs | MoveRequest, MoveResponse |
| Game.Diag | GameDiag.cs | PingRequest, PingResponse |
| Game.World | GameWorld.cs | WorldBlockChangeRequest, WorldBlockChangeResponse, ChunkDataRequest, ChunkDataResponse |
| EnhancedMinecraftProtocol | EnhancedMinecraftGame.cs | Enhanced protocol classes |

## Required Using Statements

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

## Common Issues to Avoid

❌ Do NOT use:
- GameProtocol (does not exist)
- SharedProtocol.EnhancedMinecraft (incorrect)

✅ Use instead:
- Game.Core
- EnhancedMinecraftProtocol

## Dependencies

Most Game.* namespaces depend on MinecraftGame.Common for vector types.

## Correct Protobuf Namespaces

| Namespace | File | Classes |
|-----------|------|---------|
| MinecraftGame.Common | Common.cs | Vector3, Vector3Int, Vector2, Vector2Int, Color, Timestamp, BaseResponse |
| Game.Auth | GameAuth.cs | LoginRequest, LoginResponse |
| Game.Chat | GameChat.cs | ChatRequest, ChatResponse, ChatMessage |
| Game.Core | GameCore.cs | InventoryItem, PlayerInfo |
| Game.Move | GameMove.cs | MoveRequest, MoveResponse |
| Game.Diag | GameDiag.cs | PingRequest, PingResponse |
| Game.World | GameWorld.cs | WorldBlockChangeRequest, WorldBlockChangeResponse, ChunkDataRequest, ChunkDataResponse |
| EnhancedMinecraftProtocol | EnhancedMinecraftGame.cs | Enhanced protocol classes |

## Required Using Statements

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

## Common Issues to Avoid

❌ Do NOT use:
- GameProtocol (does not exist)
- SharedProtocol.EnhancedMinecraft (incorrect)

✅ Use instead:
- Game.Core
- EnhancedMinecraftProtocol

## Dependencies

Most Game.* namespaces depend on MinecraftGame.Common for vector types.

