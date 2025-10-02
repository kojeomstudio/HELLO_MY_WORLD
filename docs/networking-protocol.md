# Networking Protocol Guide (Client ??Server)

This document describes the wire protocol and message mapping between the Unity client and the standalone .NET server for HELLO_MY_WORLD.

## Framing

All messages use a simple length-prefixed binary frame:

- 4 bytes: `TotalLength` (little-endian int) ??equals `sizeof(int) + PayloadLength`.
- 4 bytes: `MessageType` (little-endian int) ??see Message Types below.
- N bytes: `Payload` ??a serialized Protocol Buffers message.

The Unity client reads a 4 byte length, then reads `length` bytes. The first 4 bytes of that block are the message type and the remainder is the protobuf payload.

## Serialization

- Server: uses protobuf-net attributes on C# DTOs in `SharedProtocol/`.
- Client: uses Google.Protobuf-generated DTOs located under `Assets/Generated/Protobuf/`.

Note: Protobuf wire format is interoperable across implementations as long as field numbers and types match. It is acceptable for the client to omit optional fields; they will be treated as default on the server.

### Generate client C# from .proto

Protos live in `proto/`. Generate Google.Protobuf C# files for Unity like this:

```
protoc \
  --csharp_out=Assets/Generated/Protobuf \
  -Iproto \
  proto/game_auth.proto \
  proto/game_core.proto \
  proto/game_move.proto \
  proto/game_chat.proto \
  proto/game_world.proto \
  proto/game_diag.proto
```

Then refresh Unity so the generated C# appears. Ensure `Assets/link.xml` preserves the Google.Protobuf assembly for IL2CPP builds.

## Message Types

Message type IDs mirror `SharedProtocol.MessageType` on the server and must remain stable:

- 1: `LoginRequest`
- 2: `LoginResponse`
- 3: `LogoutRequest`
- 4: `LogoutResponse`
- 10: `MoveRequest`
- 11: `MoveResponse`
- 20: `WorldBlockChangeRequest`
- 21: `WorldBlockChangeResponse`
- 22: `WorldBlockChangeBroadcast`
- 30: `ChatRequest`
- 31: `ChatResponse`
- 32: `ChatMessage`
- 40: `PingRequest`
- 41: `PingResponse`
- 42: `ServerStatusRequest`
- 43: `ServerStatusResponse`
- 50: `PlayerInfoUpdate`
- 90: `RoomListRequest`
- 91: `RoomListResponse`
- 92: `RoomEnterRequest`
- 93: `RoomEnterResponse`
- 94: `RoomLeaveRequest`
- 95: `RoomLeaveResponse`
- 96: `RoomQueueUpdate`
- 97: `RoomPromotionNotice`

Minecraft-specific extensions (100+) can be added similarly; the server now accepts unknown types and will deliver raw payloads to handlers.

## Room & Lobby Messages

The room DTOs were expanded so the Unity lobby browser and the dedicated server stay in sync:

- `RoomInfo` now carries `LobbyId`, `Owner`, `GameMode`, `QueueCount`, `SpectatorCount`, `Status`, `Visibility`, `RequiresPassword`, and a small `Tags` dictionary (string key/value pairs for arbitrary metadata).
- `RoomMemberList` preserves the legacy `Members` array while adding `MemberInfos`, a list of rich `RoomMemberInfo` objects containing role, ready state, queue position, and the UTC join timestamp.
- `RoomEnterRequest` accepts optional fields (`LobbyId`, `Password`, `AutoAssign`, `AllowQueue`, `JoinAsSpectator`, `PreferredRole`) so the client can request matchmaking behaviour without out-of-band parameters.
- `RoomEnterResponse` flags whether the caller is queued (`IsQueued`, `QueuePosition`, `EstimatedWaitMs`) and returns the caller?s `RoomMemberInfo` snapshot.
- `RoomLeaveResponse` reports whether someone was promoted from the queue when a seat freed up and whether the caller was automatically returned to the lobby.
- `RoomQueueUpdateMessage` and `RoomPromotionMessage` are server?client pushes used to keep queue UI responsive without reissuing `RoomListRequest`.

When introducing new room/lobby concepts, prefer extending these DTOs with additional optional fields instead of replacing them so older clients continue to understand the baseline contract.

## Unity Client Integration

- `Assets/Scripts/Networking/Core/TcpNetworkTransport.cs` still owns the raw socket and implements the `[length][payload]` frame contract described above.
- `Assets/Scripts/Minecraft/Core/MinecraftGameClient.cs` is the high-level façade used by gameplay systems. It now:
  - Serializes requests with `ProtoBuf.Serializer` so Unity can share DTOs with the server code.
  - Queues outgoing messages on the main thread while the transport executes on a background task.
  - Maintains a chunk cache (`ChunkSnapshot`) that mirrors the authoritative state pushed by the server.
  - Normalizes movement packets (`MoveRequest`) by clamping the speed server-side code expects.
- `Assets/Scripts/Minecraft/World/ChunkManager.cs` listens for chunk callbacks, instantiates `ChunkRenderer` instances, and applies block-change broadcasts to the cached snapshot so lighting/meshes stay in sync between client and server.
- `Assets/Scripts/Minecraft/UI/MinecraftGameManager.cs` subscribes to connection/login/chunk events and exposes debug output while wiring the Unity UI.

For legacy systems the lightweight `ProtobufNetworkClient` remains available, but new gameplay should use `MinecraftGameClient` so chunk and entity handling stays consistent with the authoritative server pipeline.

## Minecraft Message Extensions

The enhanced ?minecraft??messages extend the base `MessageType` enum. The numeric IDs live in `SharedProtocol/MinecraftMessages.cs` and mirror the values generated for the client (`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`). Key assignments:

- 100: `PlayerStateUpdate` (client ??server)
- 101: `PlayerActionRequest`
- 102: `PlayerActionResponse`
- 110: `ChunkDataRequest`
- 111: `ChunkDataResponse`
- 112: `BlockChangeNotification`
- 114: `ChunkUnloadNotification`
- 115: `ChunkUnloadAcknowledge`
- 120??52: Inventory/container events
- 130??33: Entity spawn/despawn/update
- 140??43: Time/weather/effect broadcasts
- `BlockChangeNotification` is broadcast to players occupying the affected chunk and includes optional `Drops` entries (item stacks) so clients can surface survival loot events.

When the Unity client writes one of these messages it feeds the raw integer ID into `TcpNetworkTransport`, which happily forwards any four-byte code even if it is outside the `MessageType` enum. On receipt the server?s `Session.ReceiveAsync()` produces an `IncomingMessage` that exposes both the raw integer and the optional typed enum. Unknown values keep their payload as `byte[]` so specialised dispatchers (e.g. `MinecraftMessageDispatcher`) can deserialize them without having to patch the core enum.

### Chunk Payload Encoding

- Server: `GameServer/Handlers/MinecraftChunkHandler` packs each chunk into a 65,536 byte block array (16x256x16). If the payload exceeds 1 KB it is gzipped before being written as `ChunkDataResponseMessage.CompressedBlockData`.
- Client: `MinecraftGameClient` runs the buffer through `ChunkCompression.DecodeBlocks`, which detects the gzip magic bytes and inflates the array if required. The decoded result is stored in a `ChunkSnapshot` for subsequent mesh generation and block mutation.
- `ChunkDataResponseMessage.IsFromCache` is set when the server serves a cached payload or the player re-requests an already streamed chunk; the client logs cache hits and `_pendingChunkRequests` deduplicate outstanding chunk loads.
- `ChunkManager` rehydrates the snapshot into a `byte[,,]` during `ChunkRenderer.GenerateMesh`. Server-driven block updates (`BlockChangeNotification` or `WorldBlockChangeBroadcast`) update the snapshot first, then schedule a mesh refresh so the change is visible locally.
- Residency pruning: the server evicts per-player chunk residency using `WorldSettings.ChunkUnloadTimeoutMinutes` and the configured load radius, so clients may occasionally receive a fresh chunk stream for areas that fell out of cache.

### Chunk Unload Handshake
1. `MinecraftGameClient` trims chunks beyond `renderDistance`, removes them from its `_loadedChunks`, and immediately sends a `ChunkUnloadNotificationMessage` that includes player id, chunk coords, reason, and the current view radius.
2. `MinecraftChunkHandler.HandleChunkUnloadAsync` drops the residency entry, updates `SessionManager`, and replies with a `ChunkUnloadAcknowledgeMessage` (ID 115) that notes whether the entry existed and how many chunks remain tracked.
3. The client logs the acknowledgement; if the server rejects the unload (`Accepted == false`) the chunk stays cached locally so the engine can retry or diagnose mismatches without tearing down meshes repeatedly.

Because both sides are dealing with raw byte arrays (rather than a repeated list of per-block messages) the protocol stays compact and avoids excessive allocations inside the Unity player.

## Protobuf DTOs

Generated code lives in `Assets/Generated/Protobuf/`. Alongside the classic `Game.*` protos, the Unity project includes `enhanced_minecraft_game.proto` which defines all Minecraft-specific DTOs (`ChunkDataResponse`, `PlayerActionRequest`, `EntityInfo`, etc.). Run the bundled `protoc` command whenever fields change, then commit the regenerated C# to keep the client in sync with `SharedProtocol/MinecraftMessages.cs`.

## Server Compatibility Changes

- `SharedProtocol/Session.cs` now supports:
  - `SendAsync(int rawMessageType, byte[] payload)` for raw (non-enum) message types.
- `ReceiveAsync()` returns an `IncomingMessage` exposing `RawType`, `MessageType?`, and the payload. Unknown message codes (e.g., Minecraft 100+) surface as `byte[]` while still reporting the raw integer so higher-level handlers can deserialize without enum churn.

## Versioning and Backwards Compatibility

- The wire format and type IDs are stable. Always evolve messages by adding new fields with new field numbers.
- Do not reuse or renumber existing fields. Removing fields is discouraged; prefer deprecating them.

## Build Notes

- Server: `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj`.
- Unity: Ensure `Google.Protobuf` runtime is present (see `Assets/link.xml`). Generated C# files from `.proto` go under `Assets/Generated/Protobuf/`.

