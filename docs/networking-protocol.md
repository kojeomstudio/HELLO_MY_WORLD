# Networking Protocol Guide (Client ↔ Server)

This document describes the wire protocol and message mapping between the Unity client and the standalone .NET server for HELLO_MY_WORLD.

## Framing

All messages use a simple length-prefixed binary frame:

- 4 bytes: `TotalLength` (little-endian int) — equals `sizeof(int) + PayloadLength`.
- 4 bytes: `MessageType` (little-endian int) — see Message Types below.
- N bytes: `Payload` — a serialized Protocol Buffers message.

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

Minecraft-specific extensions (100+) can be added similarly; the server now accepts unknown types and will deliver raw payloads to handlers.

## Unity Client Integration

- `Assets/Scripts/Networking/Core/TcpNetworkTransport.cs` performs the socket I/O and reads/writes the length-prefixed frames.
- `Assets/Scripts/Networking/Core/ProtobufNetworkClient.cs`:
  - Builds framed packets: `[type:int][payload]` and passes them to transport.
  - Parses received frames, switches on type, and deserializes payload using Google.Protobuf DTOs.
  - Currently implements Login (request/response). Additional messages should be added as .proto grows.

- `Assets/Scripts/Networking/NetworkManager.cs`:
  - Wires client features to UI and gameplay.
  - Applies `WorldBlockChangeBroadcast` to the active world via `ModifyWorldManager.ModifySpecificSubWorld(...)`.
  - Exposes `SendBlockChange(areaId, subworldId, Vector3Int pos, int blockType, int chunkType)` for gameplay code.

## Protobuf DTOs

Generated code lives in `Assets/Generated/Protobuf/`. The existing `game_auth.proto` contains:

- `Game.Auth.LoginRequest { string username=1; string password=2; }`
- `Game.Auth.LoginResponse { bool success=1; string message=2; }`

The server-side DTO (`SharedProtocol/LoginRequest`) also supports an optional `ClientVersion` (field 3). Omitting this field from the client is safe.

Planned additions (.proto files to be authored next):

- Core: `Vector3`, `Vector3Int`, `InventoryItem`, `PlayerInfo` (for LoginResponse, PlayerInfoUpdate)
- Movement: `MoveRequest`, `MoveResponse`
- Chat: `ChatRequest`, `ChatResponse`, `ChatMessage`
- World/Blocks: `WorldBlockChangeRequest`, `WorldBlockChangeResponse`, `WorldBlockChangeBroadcast`
- Diagnostics: `PingRequest`, `PingResponse`, `ServerStatusRequest`, `ServerStatusResponse`

## Server Compatibility Changes

- `SharedProtocol/Session.cs` now supports:
  - `SendAsync(int rawMessageType, byte[] payload)` for raw (non-enum) message types.
  - `ReceiveAsync()` returns raw `byte[]` payload for unknown message type codes (e.g., Minecraft 100+). This prevents exceptions and lets higher-level handlers deserialize.

## Versioning and Backwards Compatibility

- The wire format and type IDs are stable. Always evolve messages by adding new fields with new field numbers.
- Do not reuse or renumber existing fields. Removing fields is discouraged; prefer deprecating them.

## Build Notes

- Server: `dotnet build SharedProtocol/SharedProtocol.csproj` then `dotnet build GameServer/GameServer.csproj`.
- Unity: Ensure `Google.Protobuf` runtime is present (see `Assets/link.xml`). Generated C# files from `.proto` go under `Assets/Generated/Protobuf/`.
