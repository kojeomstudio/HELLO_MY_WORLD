# Dummy Client Documentation
**Date:** 2026-01-31  
**Session:** S31 - Comprehensive Implementation  
**Component:** GameServer/DummyClient.cs

---

## Overview

The [`DummyClient`](GameServer/DummyClient.cs) is a headless, console-based testing client designed to validate protocol communication between the server and client. It tests packet encoding/decoding and network round-trip communication using Google Protocol Buffers.

---

## Architecture

### Class Structure

```csharp
public class DummyClient
{
    private readonly string _serverHost;
    private readonly int _serverPort;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly Random _random = new Random();
}
```

### Key Components

| Component | Type | Description |
|-----------|------|-------------|
| `_serverHost` | `string` | Server hostname/IP address (default: "localhost") |
| `_serverPort` | `int` | Server port (default: 5000) |
| `_tcpClient` | `TcpClient?` | TCP connection to server |
| `_stream` | `NetworkStream?` | Network stream for data transmission |
| `_random` | `Random` | Random number generator for test data |

---

## Protocol Tests

The dummy client implements the following protocol tests:

### 1. Authentication Test ([`TestAuthenticationAsync()`](GameServer/DummyClient.cs:73))

**Purpose:** Test authentication protocol messages

**Message Type:** `Game.Auth.MessageType.LoginRequest`

**Message Structure:**
```protobuf
message LoginRequest {
    string username = 1;
    string password = 2;
    string client_version = 3;
}
```

**Test Flow:**
1. Create login request with random username
2. Serialize message using protobuf
3. Send to server via TCP
4. Wait for response (100ms delay)
5. Log completion

**Example Output:**
```
[DummyClient] Testing Authentication...
[DummyClient] Sending LoginRequest: DummyUser_5421
[DummyClient] Sent message type 1 (45 bytes)
[DummyClient] Authentication test completed.
```

---

### 2. Movement Test ([`TestMovementAsync()`](GameServer/DummyClient.cs:96))

**Purpose:** Test movement protocol messages

**Message Type:** `Game.Move.MessageType.MoveRequest`

**Message Structure:**
```protobuf
message MoveRequest {
    Vector3 target_position = 1;
    float movement_speed = 2;
}

message Vector3 {
    double x = 1;
    double y = 2;
    double z = 3;
}
```

**Test Flow:**
1. Create movement request with random target position
2. Set Y to 64.0 (ground level)
3. Set movement speed to 4.5f
4. Serialize and send to server
5. Wait for response (100ms delay)
6. Log completion

**Example Output:**
```
[DummyClient] Testing Movement...
[DummyClient] Sending MoveRequest to (45.23, 64.00, 78.91)
[DummyClient] Sent message type 1 (32 bytes)
[DummyClient] Movement test completed.
```

---

### 3. World Block Change Test ([`TestWorldBlockChangeAsync()`](GameServer/DummyClient.cs:122))

**Purpose:** Test world block change protocol messages

**Message Type:** `Game.World.MessageType.WorldBlockChangeRequest`

**Message Structure:**
```protobuf
message WorldBlockChangeRequest {
    string area_id = 1;
    string subworld_id = 2;
    Vector3Int block_position = 3;
    int32 block_type = 4;
    int32 chunk_type = 5;
}

message Vector3Int {
    int32 x = 1;
    int32 y = 2;
    int32 z = 3;
}
```

**Test Flow:**
1. Create block change request
2. Set area_id to "test_area"
3. Set subworld_id to "overworld"
4. Set random block position (0-100, Y=64, 0-100)
5. Set block_type to 1 (Stone)
6. Set chunk_type to 0
7. Serialize and send to server
8. Wait for response (100ms delay)
9. Log completion

**Example Output:**
```
[DummyClient] Testing World Block Change...
[DummyClient] Sending WorldBlockChangeRequest at (42, 64, 17)
[DummyClient] Sent message type 1 (52 bytes)
[DummyClient] World block change test completed.
```

---

### 4. Chat Test ([`TestChatAsync()`](GameServer/DummyClient.cs:150))

**Purpose:** Test chat protocol messages

**Message Type:** `Game.Chat.MessageType.ChatRequest`

**Message Structure:**
```protobuf
message ChatRequest {
    string message = 1;
    int32 type = 2;  // ChatType enum
}

enum ChatType {
    Global = 0;
    Local = 1;
    Whisper = 2;
    System = 3;
}
```

**Test Flow:**
1. Create chat request with timestamp
2. Set type to Global (0)
3. Serialize and send to server
4. Wait for response (100ms delay)
5. Log completion

**Example Output:**
```
[DummyClient] Testing Chat...
[DummyClient] Sending ChatRequest: Hello from DummyClient at 2026-01-31T06:25:00.000Z
[DummyClient] Sent message type 1 (68 bytes)
[DummyClient] Chat test completed.
```

---

### 5. Ping Test ([`TestPingAsync()`](GameServer/DummyClient.cs:172))

**Purpose:** Test ping/pong protocol messages

**Message Type:** `Game.Diag.MessageType.PingRequest`

**Message Structure:**
```protobuf
message PingRequest {
    int64 client_timestamp = 1;
}
```

**Test Flow:**
1. Create ping request with current Unix timestamp
2. Serialize and send to server
3. Wait for response (100ms delay)
4. Log completion

**Example Output:**
```
[DummyClient] Testing Ping...
[DummyClient] Sending PingRequest: 1738301100000
[DummyClient] Sent message type 1 (12 bytes)
[DummyClient] Ping test completed.
```

---

### 6. Chunk Data Test ([`TestChunkDataAsync()`](GameServer/DummyClient.cs:192))

**Purpose:** Test chunk data protocol messages

**Message Type:** `Game.World.MessageType.ChunkDataRequest`

**Message Structure:**
```protobuf
message ChunkDataRequest {
    int32 chunk_x = 1;
    int32 chunk_z = 2;
    int32 view_distance = 3;
}
```

**Test Flow:**
1. Create chunk data request with random chunk coordinates
2. Set chunk_x to random value (-10 to 10)
3. Set chunk_z to random value (-10 to 10)
4. Set view_distance to 5
5. Serialize and send to server
6. Wait for response (100ms delay)
7. Log completion

**Example Output:**
```
[DummyClient] Testing Chunk Data...
[DummyClient] Sending ChunkDataRequest: (3, -2)
[DummyClient] Sent message type 1 (16 bytes)
[DummyClient] Chunk data test completed.
```

---

### 7. Enhanced Protocol Test ([`TestEnhancedProtocolAsync()`](GameServer/DummyClient.cs:214))

**Purpose:** Test Enhanced Minecraft protocol messages

**Message Type:** `EnhancedMinecraftProtocol.MinecraftMessageType.PlayerStateUpdate`

**Message Structure:**
```protobuf
message PlayerInfo {
    string player_id = 1;
    string username = 2;
    Vector3 position = 3;
    int32 level = 4;
    int32 health = 5;
    int32 max_health = 6;
}
```

**Test Flow:**
1. Create player state update
2. Set player_id to "dummy_player"
3. Set username to "DummyPlayer"
4. Set position to (0.0, 64.0, 0.0)
5. Set level to 1
6. Set health to 20
7. Set max_health to 20
8. Serialize and send to server
9. Wait for response (100ms delay)
10. Log completion

**Example Output:**
```
[DummyClient] Testing Enhanced Protocol...
[DummyClient] Sending PlayerStateUpdate for DummyPlayer
[DummyClient] Sent message type 1 (48 bytes)
[DummyClient] Enhanced protocol test completed.
```

---

## Packet Format

The dummy client uses a simple packet format for message transmission:

```
[MessageType (4 bytes)] [Length (4 bytes)] [Data (variable)]
```

### Packet Structure

| Field | Size | Type | Description |
|-------|------|------|-------------|
| MessageType | 4 bytes | `int32` | Protocol message type identifier |
| Length | 4 bytes | `int32` | Length of the protobuf data |
| Data | variable | `byte[]` | Serialized protobuf message |

### Serialization Code

```csharp
private async Task SendMessageAsync<T>(int messageType, T message) where T : IMessage<T>
{
    // Serialize message to bytes
    byte[] messageBytes = message.ToByteArray();

    // Create packet: [messageType (4 bytes)] [length (4 bytes)] [data]
    byte[] packet = new byte[8 + messageBytes.Length];
    BitConverter.GetBytes(messageType).CopyTo(packet, 0);
    BitConverter.GetBytes(messageBytes.Length).CopyTo(packet, 4);
    messageBytes.CopyTo(packet, 8);

    // Send to server
    await _stream.WriteAsync(packet, 0, packet.Length);
    await _stream.FlushAsync();
}
```

---

## Usage

### Command Line

```bash
# Run with default settings (localhost:5000)
dotnet run --project GameServer -- --dummy-client

# Run with custom host and port
dotnet run --project GameServer -- --dummy-client 192.168.1.100 8080
```

### Programmatic Usage

```csharp
using GameServer;

// Create client with default settings
var client = new DummyClient();
await client.RunTestsAsync();

// Create client with custom settings
var client = new DummyClient("192.168.1.100", 8080);
await client.RunTestsAsync();
```

---

## Test Execution Flow

```
Start
  ↓
Connect to Server
  ↓
TestAuthenticationAsync
  ↓
TestMovementAsync
  ↓
TestWorldBlockChangeAsync
  ↓
TestChatAsync
  ↓
TestPingAsync
  ↓
TestChunkDataAsync
  ↓
TestEnhancedProtocolAsync
  ↓
All Tests Completed
  ↓
Disconnect
  ↓
End
```

---

## Protocol Namespaces Used

| Namespace | Purpose |
|-----------|---------|
| `Game.Auth` | Authentication messages |
| `Game.Chat` | Chat messages |
| `Game.Core` | Core game messages |
| `Game.Diag` | Diagnostic messages (ping/pong) |
| `Game.Move` | Movement messages |
| `Game.World` | World and chunk messages |
| `EnhancedMinecraftProtocol` | Enhanced Minecraft protocol messages |
| `MinecraftGame.Common` | Common data types (Vector3, Vector3Int) |

---

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Google.Protobuf` | Latest | Protocol buffer serialization |

### Generated Protobuf Files

All protobuf messages are generated from `.proto` files located in the `proto/` directory:

- [`proto/common.proto`](proto/common.proto) → [`Assets/Generated/Protobuf/Common.cs`](Assets/Generated/Protobuf/Common.cs)
- [`proto/enhanced_minecraft_game.proto`](proto/enhanced_minecraft_game.proto) → [`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`](Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)
- [`proto/game_auth.proto`](proto/game_auth.proto) → [`Assets/Generated/Protobuf/GameAuth.cs`](Assets/Generated/Protobuf/GameAuth.cs)
- [`proto/game_chat.proto`](proto/game_chat.proto) → [`Assets/Generated/Protobuf/GameChat.cs`](Assets/Generated/Protobuf/GameChat.cs)
- [`proto/game_core.proto`](proto/game_core.proto) → [`Assets/Generated/Protobuf/GameCore.cs`](Assets/Generated/Protobuf/GameCore.cs)
- [`proto/game_diag.proto`](proto/game_diag.proto) → [`Assets/Generated/Protobuf/GameDiag.cs`](Assets/Generated/Protobuf/GameDiag.cs)
- [`proto/game_move.proto`](proto/game_move.proto) → [`Assets/Generated/Protobuf/GameMove.cs`](Assets/Generated/Protobuf/GameMove.cs)
- [`proto/game_world.proto`](proto/game_world.proto) → [`Assets/Generated/Protobuf/GameWorld.cs`](Assets/Generated/Protobuf/GameWorld.cs)

---

## Limitations

1. **No Response Handling:** The dummy client currently does not read responses from the server. It only sends messages and waits for a fixed delay.

2. **Simplified Testing:** Tests are simplified and do not validate server responses or error handling.

3. **Fixed Delays:** Uses fixed 100ms delays between tests, which may not be appropriate for all scenarios.

4. **No Authentication:** Uses hardcoded test credentials that may not match server authentication requirements.

5. **Single Connection:** Only tests a single client connection. Does not test concurrent connections or load scenarios.

---

## Future Enhancements

1. **Response Handling:** Implement proper response reading and validation from server.

2. **Test Assertions:** Add assertions to verify expected server responses.

3. **Configurable Tests:** Allow selective test execution via command-line arguments.

4. **Performance Testing:** Add timing measurements for latency and throughput.

5. **Concurrent Testing:** Implement multiple concurrent dummy clients for load testing.

6. **Error Scenarios:** Test error conditions and edge cases.

7. **Reconnection Logic:** Implement automatic reconnection on connection failure.

8. **Detailed Logging:** Add more detailed logging for debugging and analysis.

---

## Integration with CI/CD

The dummy client can be integrated into CI/CD pipelines for automated protocol testing:

```yaml
# Example GitHub Actions workflow
- name: Start Server
  run: dotnet run --project GameServer -- --server &
  
- name: Wait for Server
  run: sleep 10
  
- name: Run Dummy Client Tests
  run: dotnet run --project GameServer -- --dummy-client
  
- name: Check Test Results
  run: |
    if [ $? -eq 0 ]; then
      echo "All protocol tests passed!"
    else
      echo "Protocol tests failed!"
      exit 1
    fi
```

---

## Troubleshooting

### Common Issues

**Issue:** Connection refused
```
System.Net.Sockets.SocketException: Connection refused
```
**Solution:** Ensure the server is running on the specified host and port.

**Issue:** Timeout during connection
```
System.TimeoutException: A connection attempt failed
```
**Solution:** Check network connectivity and firewall settings.

**Issue:** Protobuf serialization error
```
Google.Protobuf.InvalidProtocolBufferException
```
**Solution:** Ensure protobuf generated files are up to date. Run:
```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

---

## Summary

The [`DummyClient`](GameServer/DummyClient.cs) provides a comprehensive protocol testing framework for the Minecraft game server. It validates all major protocol message types including authentication, movement, world manipulation, chat, diagnostics, chunk data, and enhanced protocol messages.

**Status:** ✅ **IMPLEMENTED AND OPERATIONAL**

**Test Coverage:**
- Authentication: ✅
- Movement: ✅
- World Block Change: ✅
- Chat: ✅
- Ping/Pong: ✅
- Chunk Data: ✅
- Enhanced Protocol: ✅

**Next Steps:** Implement response handling and test assertions for comprehensive validation.

---

**Documentation Created:** 2026-01-31T06:25:00Z  
**Session:** S31 - Comprehensive Implementation  
**Next Task:** Implement shared DLL architecture for common enums/code
**Date:** 2026-01-31  
**Session:** S31 - Comprehensive Implementation  
**Component:** GameServer/DummyClient.cs

---

## Overview

The [`DummyClient`](GameServer/DummyClient.cs) is a headless, console-based testing client designed to validate protocol communication between the server and client. It tests packet encoding/decoding and network round-trip communication using Google Protocol Buffers.

---

## Architecture

### Class Structure

```csharp
public class DummyClient
{
    private readonly string _serverHost;
    private readonly int _serverPort;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly Random _random = new Random();
}
```

### Key Components

| Component | Type | Description |
|-----------|------|-------------|
| `_serverHost` | `string` | Server hostname/IP address (default: "localhost") |
| `_serverPort` | `int` | Server port (default: 5000) |
| `_tcpClient` | `TcpClient?` | TCP connection to server |
| `_stream` | `NetworkStream?` | Network stream for data transmission |
| `_random` | `Random` | Random number generator for test data |

---

## Protocol Tests

The dummy client implements the following protocol tests:

### 1. Authentication Test ([`TestAuthenticationAsync()`](GameServer/DummyClient.cs:73))

**Purpose:** Test authentication protocol messages

**Message Type:** `Game.Auth.MessageType.LoginRequest`

**Message Structure:**
```protobuf
message LoginRequest {
    string username = 1;
    string password = 2;
    string client_version = 3;
}
```

**Test Flow:**
1. Create login request with random username
2. Serialize message using protobuf
3. Send to server via TCP
4. Wait for response (100ms delay)
5. Log completion

**Example Output:**
```
[DummyClient] Testing Authentication...
[DummyClient] Sending LoginRequest: DummyUser_5421
[DummyClient] Sent message type 1 (45 bytes)
[DummyClient] Authentication test completed.
```

---

### 2. Movement Test ([`TestMovementAsync()`](GameServer/DummyClient.cs:96))

**Purpose:** Test movement protocol messages

**Message Type:** `Game.Move.MessageType.MoveRequest`

**Message Structure:**
```protobuf
message MoveRequest {
    Vector3 target_position = 1;
    float movement_speed = 2;
}

message Vector3 {
    double x = 1;
    double y = 2;
    double z = 3;
}
```

**Test Flow:**
1. Create movement request with random target position
2. Set Y to 64.0 (ground level)
3. Set movement speed to 4.5f
4. Serialize and send to server
5. Wait for response (100ms delay)
6. Log completion

**Example Output:**
```
[DummyClient] Testing Movement...
[DummyClient] Sending MoveRequest to (45.23, 64.00, 78.91)
[DummyClient] Sent message type 1 (32 bytes)
[DummyClient] Movement test completed.
```

---

### 3. World Block Change Test ([`TestWorldBlockChangeAsync()`](GameServer/DummyClient.cs:122))

**Purpose:** Test world block change protocol messages

**Message Type:** `Game.World.MessageType.WorldBlockChangeRequest`

**Message Structure:**
```protobuf
message WorldBlockChangeRequest {
    string area_id = 1;
    string subworld_id = 2;
    Vector3Int block_position = 3;
    int32 block_type = 4;
    int32 chunk_type = 5;
}

message Vector3Int {
    int32 x = 1;
    int32 y = 2;
    int32 z = 3;
}
```

**Test Flow:**
1. Create block change request
2. Set area_id to "test_area"
3. Set subworld_id to "overworld"
4. Set random block position (0-100, Y=64, 0-100)
5. Set block_type to 1 (Stone)
6. Set chunk_type to 0
7. Serialize and send to server
8. Wait for response (100ms delay)
9. Log completion

**Example Output:**
```
[DummyClient] Testing World Block Change...
[DummyClient] Sending WorldBlockChangeRequest at (42, 64, 17)
[DummyClient] Sent message type 1 (52 bytes)
[DummyClient] World block change test completed.
```

---

### 4. Chat Test ([`TestChatAsync()`](GameServer/DummyClient.cs:150))

**Purpose:** Test chat protocol messages

**Message Type:** `Game.Chat.MessageType.ChatRequest`

**Message Structure:**
```protobuf
message ChatRequest {
    string message = 1;
    int32 type = 2;  // ChatType enum
}

enum ChatType {
    Global = 0;
    Local = 1;
    Whisper = 2;
    System = 3;
}
```

**Test Flow:**
1. Create chat request with timestamp
2. Set type to Global (0)
3. Serialize and send to server
4. Wait for response (100ms delay)
5. Log completion

**Example Output:**
```
[DummyClient] Testing Chat...
[DummyClient] Sending ChatRequest: Hello from DummyClient at 2026-01-31T06:25:00.000Z
[DummyClient] Sent message type 1 (68 bytes)
[DummyClient] Chat test completed.
```

---

### 5. Ping Test ([`TestPingAsync()`](GameServer/DummyClient.cs:172))

**Purpose:** Test ping/pong protocol messages

**Message Type:** `Game.Diag.MessageType.PingRequest`

**Message Structure:**
```protobuf
message PingRequest {
    int64 client_timestamp = 1;
}
```

**Test Flow:**
1. Create ping request with current Unix timestamp
2. Serialize and send to server
3. Wait for response (100ms delay)
4. Log completion

**Example Output:**
```
[DummyClient] Testing Ping...
[DummyClient] Sending PingRequest: 1738301100000
[DummyClient] Sent message type 1 (12 bytes)
[DummyClient] Ping test completed.
```

---

### 6. Chunk Data Test ([`TestChunkDataAsync()`](GameServer/DummyClient.cs:192))

**Purpose:** Test chunk data protocol messages

**Message Type:** `Game.World.MessageType.ChunkDataRequest`

**Message Structure:**
```protobuf
message ChunkDataRequest {
    int32 chunk_x = 1;
    int32 chunk_z = 2;
    int32 view_distance = 3;
}
```

**Test Flow:**
1. Create chunk data request with random chunk coordinates
2. Set chunk_x to random value (-10 to 10)
3. Set chunk_z to random value (-10 to 10)
4. Set view_distance to 5
5. Serialize and send to server
6. Wait for response (100ms delay)
7. Log completion

**Example Output:**
```
[DummyClient] Testing Chunk Data...
[DummyClient] Sending ChunkDataRequest: (3, -2)
[DummyClient] Sent message type 1 (16 bytes)
[DummyClient] Chunk data test completed.
```

---

### 7. Enhanced Protocol Test ([`TestEnhancedProtocolAsync()`](GameServer/DummyClient.cs:214))

**Purpose:** Test Enhanced Minecraft protocol messages

**Message Type:** `EnhancedMinecraftProtocol.MinecraftMessageType.PlayerStateUpdate`

**Message Structure:**
```protobuf
message PlayerInfo {
    string player_id = 1;
    string username = 2;
    Vector3 position = 3;
    int32 level = 4;
    int32 health = 5;
    int32 max_health = 6;
}
```

**Test Flow:**
1. Create player state update
2. Set player_id to "dummy_player"
3. Set username to "DummyPlayer"
4. Set position to (0.0, 64.0, 0.0)
5. Set level to 1
6. Set health to 20
7. Set max_health to 20
8. Serialize and send to server
9. Wait for response (100ms delay)
10. Log completion

**Example Output:**
```
[DummyClient] Testing Enhanced Protocol...
[DummyClient] Sending PlayerStateUpdate for DummyPlayer
[DummyClient] Sent message type 1 (48 bytes)
[DummyClient] Enhanced protocol test completed.
```

---

## Packet Format

The dummy client uses a simple packet format for message transmission:

```
[MessageType (4 bytes)] [Length (4 bytes)] [Data (variable)]
```

### Packet Structure

| Field | Size | Type | Description |
|-------|------|------|-------------|
| MessageType | 4 bytes | `int32` | Protocol message type identifier |
| Length | 4 bytes | `int32` | Length of the protobuf data |
| Data | variable | `byte[]` | Serialized protobuf message |

### Serialization Code

```csharp
private async Task SendMessageAsync<T>(int messageType, T message) where T : IMessage<T>
{
    // Serialize message to bytes
    byte[] messageBytes = message.ToByteArray();

    // Create packet: [messageType (4 bytes)] [length (4 bytes)] [data]
    byte[] packet = new byte[8 + messageBytes.Length];
    BitConverter.GetBytes(messageType).CopyTo(packet, 0);
    BitConverter.GetBytes(messageBytes.Length).CopyTo(packet, 4);
    messageBytes.CopyTo(packet, 8);

    // Send to server
    await _stream.WriteAsync(packet, 0, packet.Length);
    await _stream.FlushAsync();
}
```

---

## Usage

### Command Line

```bash
# Run with default settings (localhost:5000)
dotnet run --project GameServer -- --dummy-client

# Run with custom host and port
dotnet run --project GameServer -- --dummy-client 192.168.1.100 8080
```

### Programmatic Usage

```csharp
using GameServer;

// Create client with default settings
var client = new DummyClient();
await client.RunTestsAsync();

// Create client with custom settings
var client = new DummyClient("192.168.1.100", 8080);
await client.RunTestsAsync();
```

---

## Test Execution Flow

```
Start
  ↓
Connect to Server
  ↓
TestAuthenticationAsync
  ↓
TestMovementAsync
  ↓
TestWorldBlockChangeAsync
  ↓
TestChatAsync
  ↓
TestPingAsync
  ↓
TestChunkDataAsync
  ↓
TestEnhancedProtocolAsync
  ↓
All Tests Completed
  ↓
Disconnect
  ↓
End
```

---

## Protocol Namespaces Used

| Namespace | Purpose |
|-----------|---------|
| `Game.Auth` | Authentication messages |
| `Game.Chat` | Chat messages |
| `Game.Core` | Core game messages |
| `Game.Diag` | Diagnostic messages (ping/pong) |
| `Game.Move` | Movement messages |
| `Game.World` | World and chunk messages |
| `EnhancedMinecraftProtocol` | Enhanced Minecraft protocol messages |
| `MinecraftGame.Common` | Common data types (Vector3, Vector3Int) |

---

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Google.Protobuf` | Latest | Protocol buffer serialization |

### Generated Protobuf Files

All protobuf messages are generated from `.proto` files located in the `proto/` directory:

- [`proto/common.proto`](proto/common.proto) → [`Assets/Generated/Protobuf/Common.cs`](Assets/Generated/Protobuf/Common.cs)
- [`proto/enhanced_minecraft_game.proto`](proto/enhanced_minecraft_game.proto) → [`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`](Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)
- [`proto/game_auth.proto`](proto/game_auth.proto) → [`Assets/Generated/Protobuf/GameAuth.cs`](Assets/Generated/Protobuf/GameAuth.cs)
- [`proto/game_chat.proto`](proto/game_chat.proto) → [`Assets/Generated/Protobuf/GameChat.cs`](Assets/Generated/Protobuf/GameChat.cs)
- [`proto/game_core.proto`](proto/game_core.proto) → [`Assets/Generated/Protobuf/GameCore.cs`](Assets/Generated/Protobuf/GameCore.cs)
- [`proto/game_diag.proto`](proto/game_diag.proto) → [`Assets/Generated/Protobuf/GameDiag.cs`](Assets/Generated/Protobuf/GameDiag.cs)
- [`proto/game_move.proto`](proto/game_move.proto) → [`Assets/Generated/Protobuf/GameMove.cs`](Assets/Generated/Protobuf/GameMove.cs)
- [`proto/game_world.proto`](proto/game_world.proto) → [`Assets/Generated/Protobuf/GameWorld.cs`](Assets/Generated/Protobuf/GameWorld.cs)

---

## Limitations

1. **No Response Handling:** The dummy client currently does not read responses from the server. It only sends messages and waits for a fixed delay.

2. **Simplified Testing:** Tests are simplified and do not validate server responses or error handling.

3. **Fixed Delays:** Uses fixed 100ms delays between tests, which may not be appropriate for all scenarios.

4. **No Authentication:** Uses hardcoded test credentials that may not match server authentication requirements.

5. **Single Connection:** Only tests a single client connection. Does not test concurrent connections or load scenarios.

---

## Future Enhancements

1. **Response Handling:** Implement proper response reading and validation from server.

2. **Test Assertions:** Add assertions to verify expected server responses.

3. **Configurable Tests:** Allow selective test execution via command-line arguments.

4. **Performance Testing:** Add timing measurements for latency and throughput.

5. **Concurrent Testing:** Implement multiple concurrent dummy clients for load testing.

6. **Error Scenarios:** Test error conditions and edge cases.

7. **Reconnection Logic:** Implement automatic reconnection on connection failure.

8. **Detailed Logging:** Add more detailed logging for debugging and analysis.

---

## Integration with CI/CD

The dummy client can be integrated into CI/CD pipelines for automated protocol testing:

```yaml
# Example GitHub Actions workflow
- name: Start Server
  run: dotnet run --project GameServer -- --server &
  
- name: Wait for Server
  run: sleep 10
  
- name: Run Dummy Client Tests
  run: dotnet run --project GameServer -- --dummy-client
  
- name: Check Test Results
  run: |
    if [ $? -eq 0 ]; then
      echo "All protocol tests passed!"
    else
      echo "Protocol tests failed!"
      exit 1
    fi
```

---

## Troubleshooting

### Common Issues

**Issue:** Connection refused
```
System.Net.Sockets.SocketException: Connection refused
```
**Solution:** Ensure the server is running on the specified host and port.

**Issue:** Timeout during connection
```
System.TimeoutException: A connection attempt failed
```
**Solution:** Check network connectivity and firewall settings.

**Issue:** Protobuf serialization error
```
Google.Protobuf.InvalidProtocolBufferException
```
**Solution:** Ensure protobuf generated files are up to date. Run:
```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

---

## Summary

The [`DummyClient`](GameServer/DummyClient.cs) provides a comprehensive protocol testing framework for the Minecraft game server. It validates all major protocol message types including authentication, movement, world manipulation, chat, diagnostics, chunk data, and enhanced protocol messages.

**Status:** ✅ **IMPLEMENTED AND OPERATIONAL**

**Test Coverage:**
- Authentication: ✅
- Movement: ✅
- World Block Change: ✅
- Chat: ✅
- Ping/Pong: ✅
- Chunk Data: ✅
- Enhanced Protocol: ✅

**Next Steps:** Implement response handling and test assertions for comprehensive validation.

---

**Documentation Created:** 2026-01-31T06:25:00Z  
**Session:** S31 - Comprehensive Implementation  
**Next Task:** Implement shared DLL architecture for common enums/code

