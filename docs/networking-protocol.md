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

The enhanced “minecraft” messages extend the base `MessageType` enum. The numeric IDs live in `SharedProtocol/MinecraftMessages.cs` and mirror the values generated for the client (`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`). Key assignments:

- 100: `PlayerStateUpdate` (client → server)
- 101: `PlayerActionRequest`
- 102: `PlayerActionResponse`
- 110: `ChunkDataRequest`
- 111: `ChunkDataResponse`
- 112: `BlockChangeNotification`
- 120–152: Inventory/container events
- 130–133: Entity spawn/despawn/update
- 140–143: Time/weather/effect broadcasts

When the Unity client writes one of these messages it feeds the raw integer ID into `TcpNetworkTransport`, which happily forwards any four-byte code even if it is outside the `MessageType` enum. On receipt the server’s `Session.ReceiveAsync()` returns the raw payload to the `MinecraftMessageDispatcher` so strongly-typed handlers can pick it up.

### Chunk Payload Encoding

- Server: `GameServer/Handlers/MinecraftChunkHandler` packs each chunk into a 65 536 byte block array (16×256×16). If the payload exceeds 1 KB it is gzipped before being written as `ChunkDataResponseMessage.CompressedBlockData`.
- Client: `MinecraftGameClient` runs the buffer through `ChunkCompression.DecodeBlocks`, which detects the gzip magic bytes and inflates the array if required. The decoded result is stored in a `ChunkSnapshot` for subsequent mesh generation and block mutation.
- `ChunkManager` rehydrates the snapshot into a `byte[,,]` during `ChunkRenderer.GenerateMesh`. Server-driven block updates (`BlockChangeNotification` or `WorldBlockChangeBroadcast`) update the snapshot first, then schedule a mesh refresh so the change is visible locally.

Because both sides are dealing with raw byte arrays (rather than a repeated list of per-block messages) the protocol stays compact and avoids excessive allocations inside the Unity player.

## Protobuf DTOs

Generated code lives in `Assets/Generated/Protobuf/`. Alongside the classic `Game.*` protos, the Unity project includes `enhanced_minecraft_game.proto` which defines all Minecraft-specific DTOs (`ChunkDataResponse`, `PlayerActionRequest`, `EntityInfo`, etc.). Run the bundled `protoc` command whenever fields change, then commit the regenerated C# to keep the client in sync with `SharedProtocol/MinecraftMessages.cs`.

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
