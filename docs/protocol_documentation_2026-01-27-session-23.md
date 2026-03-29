# Protocol Documentation - Session 23 (2026-01-27)

## Table of Contents
1. [Protocol Overview](#protocol-overview)
2. [Protocol Stack](#protocol-stack)
3. [Message Types](#message-types)
4. [Protocol Registry](#protocol-registry)
5. [Message Flow](#message-flow)
6. [Serialization](#serialization)
7. [Validation](#validation)
8. [Testing](#testing)
9. [Client Integration](#client-integration)
10. [Server Integration](#server-integration)

---

## Protocol Overview

The HELLO_MY_WORLD project uses Google.Protobuf for client-server communication. The protocol is designed to be efficient, extensible, and type-safe.

### Protocol Characteristics
- **Format:** Google.Protobuf (Protocol Buffers)
- **Transport:** TCP/IP
- **Frame Format:** 6-byte header (4-byte big-endian length + 2-byte big-endian message type) + payload
- **Byte Order:** Big-endian (network byte order)
- **Encoding:** Binary protobuf encoding
- **Compression:** None (future enhancement)

### Protocol Versions
- **EnhancedMinecraftProtocol:** Current active protocol
- **Legacy protobuf-net:** Deprecated but still referenced in some handlers
- **Protocol Signature:** `2026-01-27-hydrology-shield-v4-flow-lock`

---

## Protocol Stack

### Protocol Definition Files

**Location:** [`proto/`](../proto/)

#### 1. common.proto
- **Purpose:** Common message definitions
- **Key Messages:**
  - Vector3Int
  - Vector3
  - BlockPosition
  - ItemStack

#### 2. enhanced_minecraft_game.proto
- **Purpose:** Enhanced Minecraft-specific messages
- **Key Messages:**
  - TimeUpdateBroadcast
  - ChunkLoadRequest
  - ChunkLoadResponse
  - ChunkUnloadNotification
  - ChunkUnloadAck
  - BlockChangeBroadcast
  - EntitySpawnBroadcast
  - EntityDespawnBroadcast
  - WeatherUpdateBroadcast
  - SoundEffect
  - ParticleEffect

#### 3. game_auth.proto
- **Purpose:** Authentication and authorization
- **Key Messages:**
  - LoginRequest
  - LoginResponse
  - LogoutRequest
  - LogoutResponse

#### 4. game_chat.proto
- **Purpose:** Chat system
- **Key Messages:**
  - ChatMessage
  - ChatBroadcast

#### 5. game_core.proto
- **Purpose:** Core gameplay messages
- **Key Messages:**
  - PlayerInfo
  - InventoryItem
  - PlayerActionRequest
  - PlayerActionResponse

#### 6. game_diag.proto
- **Purpose:** Diagnostics and debugging
- **Key Messages:**
  - DiagnosticRequest
  - DiagnosticResponse

#### 7. game_move.proto
- **Purpose:** Player movement
- **Key Messages:**
  - MovementRequest
  - MovementResponse
  - PositionUpdate

#### 8. game_world.proto
- **Purpose:** World and block interactions
- **Key Messages:**
  - WorldBlockChangeRequest
  - WorldBlockChangeResponse
  - WorldBlockChangeBroadcast

### Generated Code

#### Server-Side Generation
- **Location:** [`SharedProtocol/`](../SharedProtocol/)
- **Build Process:**
  ```bash
  protoc -I proto --csharp_out=SharedProtocol/Proto proto/*.proto
  ```
- **Output:** C# classes in `SharedProtocol/Proto/`

#### Client-Side Generation
- **Location:** [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)
- **Build Process:**
  ```bash
  protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
  ```
- **Output:** C# classes for Unity

---

## Message Types

### Message Type Enumeration

**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)

```csharp
public enum MinecraftMessageType
{
    PlayerStateUpdate = 1,
    PlayerActionRequest = 2,
    PlayerActionResponse = 3,
    ChunkDataRequest = 4,
    ChunkDataResponse = 5,
    ChunkUnloadNotification = 6,
    ChunkUnloadAcknowledge = 7,
    BlockChangeNotification = 8,
    EntitySpawn = 9,
    EntityDespawn = 10,
    TimeUpdate = 11,
    WeatherChange = 12,
    SoundEffect = 13,
    ParticleEffect = 14,
    // ... additional message types
}
```

### Required Messages (13)

These messages are required for core functionality and must be registered in the protocol registry.

1. **PlayerStateUpdate** - Player state synchronization
2. **PlayerActionRequest** - Player action requests
3. **PlayerActionResponse** - Player action responses
4. **ChunkDataRequest** - Chunk loading request
5. **ChunkDataResponse** - Chunk data response
6. **ChunkUnloadNotification** - Chunk unload notification
7. **ChunkUnloadAcknowledge** - Chunk unload acknowledgement
8. **BlockChangeNotification** - Block change broadcast
9. **EntitySpawn** - Entity spawn broadcast
10. **EntityDespawn** - Entity despawn broadcast
11. **TimeUpdate** - World time update
12. **WeatherChange** - Weather change broadcast
13. **SoundEffect** - Sound effect

### Optional Messages (10)

These messages are tracked but not required for core functionality.

1. **MultiBlockChange** - Multiple block changes
2. **InventoryUpdate** - Inventory state update
3. **ItemUse** - Item usage
4. **ItemDrop** - Item drop
5. **ItemPickup** - Item pickup
6. **EntityUpdate** - Entity state update
7. **EntityInteract** - Entity interaction
8. **ContainerOpen** - Container open
9. **ContainerClose** - Container close
10. **ContainerUpdate** - Container update

---

## Protocol Registry

### Registry Implementation

**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)

**Purpose:** Central registry mapping message types to protobuf message factories

**Key Features:**
- Message type to descriptor name mapping
- Message factory functions for creating prototypes
- Required binding enforcement
- Optional binding tracking
- Descriptor validation
- Fingerprint computation

### Registry Bindings

The registry maintains bindings for all required message types:

```csharp
private static readonly ProtocolBinding[] Bindings =
{
    new(MinecraftMessageType.PlayerStateUpdate, nameof(EnhancedMinecraftProtocol.PlayerInfo), () => new EnhancedMinecraftProtocol.PlayerInfo()),
    new(MinecraftMessageType.PlayerActionRequest, nameof(EnhancedMinecraftProtocol.PlayerActionRequest), () => new EnhancedMinecraftProtocol.PlayerActionRequest()),
    new(MinecraftMessageType.PlayerActionResponse, nameof(EnhancedMinecraftProtocol.PlayerActionResponse), () => new EnhancedMinecraftProtocol.PlayerActionResponse()),
    new(MinecraftMessageType.ChunkDataRequest, nameof(EnhancedMinecraftProtocol.ChunkLoadRequest), () => new EnhancedMinecraftProtocol.ChunkLoadRequest()),
    new(MinecraftMessageType.ChunkDataResponse, nameof(EnhancedMinecraftProtocol.ChunkLoadResponse), () => new EnhancedMinecraftProtocol.ChunkLoadResponse()),
    new(MinecraftMessageType.ChunkUnloadNotification, nameof(EnhancedMinecraftProtocol.ChunkUnloadNotification), () => new EnhancedMinecraftProtocol.ChunkUnloadNotification()),
    new(MinecraftMessageType.ChunkUnloadAcknowledge, nameof(EnhancedMinecraftProtocol.ChunkUnloadAck), () => new EnhancedMinecraftProtocol.ChunkUnloadAck()),
    new(MinecraftMessageType.BlockChangeNotification, nameof(EnhancedMinecraftProtocol.BlockChangeBroadcast), () => new EnhancedMinecraftProtocol.BlockChangeBroadcast()),
    new(MinecraftMessageType.EntitySpawn, nameof(EnhancedMinecraftProtocol.EntitySpawnBroadcast), () => new EnhancedMinecraftProtocol.EntitySpawnBroadcast()),
    new(MinecraftMessageType.EntityDespawn, nameof(EnhancedMinecraftProtocol.EntityDespawnBroadcast), () => new EnhancedMinecraftProtocol.EntityDespawnBroadcast()),
    new(MinecraftMessageType.TimeUpdate, nameof(EnhancedMinecraftProtocol.TimeUpdateBroadcast), () => new EnhancedMinecraftProtocol.TimeUpdateBroadcast()),
    new(MinecraftMessageType.WeatherChange, nameof(EnhancedMinecraftProtocol.WeatherUpdateBroadcast), () => new EnhancedMinecraftProtocol.WeatherUpdateBroadcast()),
    new(MinecraftMessageType.SoundEffect, nameof(EnhancedMinecraftProtocol.SoundEffect), () => new EnhancedMinecraftProtocol.SoundEffect()),
    new(MinecraftMessageType.ParticleEffect, nameof(EnhancedMinecraftProtocol.ParticleEffect), () => new EnhancedMinecraftProtocol.ParticleEffect())
};
```

### Validation Methods

#### 1. ValidateBindings()
- Validates all registered bindings
- Checks descriptor names match generated protobuf classes
- Ensures parsers are available
- Validates package names
- Throws on mismatch

#### 2. EnsureRegistered()
- Throws if message type is not registered
- Used for early validation during handler registration
- Prevents stale IDL changes

#### 3. EnsureRequiredBindings()
- Throws if any required (non-optional) message type is missing
- Ensures core functionality is available

#### 4. GetUnregisteredOptionalTypes()
- Returns optional message types that are not bound
- Used for auditing and reporting

---

## Message Flow

### Client to Server Flow

```
┌──────────────┐
│ Unity Client │
└──────┬───────┘
       │ TCP/IP
       │
┌──────▼──────────────────────────────────────────┐
│         .NET 6.0 Server                    │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. Receive framed packet            │  │
│  │ 2. Parse 6-byte header             │  │
│  │ 3. Extract message type and payload   │  │
│  │ 4. Route to MessageDispatcher       │  │
│  │ 5. Dispatch to appropriate handler  │  │
│  │ 6. Handler processes request       │  │
│  │ 7. Update world state             │  │
│  │ 8. Send response packet(s)       │  │
│  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### Frame Format

**Header (6 bytes):**
- Bytes 0-3: Payload length (big-endian, 32-bit integer)
- Bytes 4-5: Message type (big-endian, 16-bit integer)

**Payload (variable length):**
- Protobuf serialized message

**Total Frame:**
```
+-------------------+-------------------+
| Length (4 bytes) | Type (2 bytes) |
+-------------------+-------------------+
|              Payload (N bytes)              |
+-----------------------------------------+
```

---

## Serialization

### Serialization Process

#### Client-Side (Unity)
1. Create message instance (e.g., `ChunkLoadRequest`)
2. Set message properties
3. Serialize to byte array: `message.ToByteArray()`
4. Build frame with 6-byte header
5. Send via TCP socket

#### Server-Side (.NET)
1. Receive frame from TCP socket
2. Parse 6-byte header
3. Read payload bytes
4. Deserialize: `ChunkLoadRequest.Parser.ParseFrom(payload)`
5. Process message
6. Serialize response
7. Build frame with 6-byte header
8. Send back to client

### Serialization Performance

- **Efficiency:** Protobuf is highly efficient binary format
- **Size:** Typically 50-80% smaller than JSON
- **Speed:** Fast serialization/deserialization
- **Memory:** Zero-copy parsing where possible

---

## Validation

### Protocol Validation Components

#### 1. Protocol Validator
**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)

**Purpose:** Comprehensive protocol validation

**Validations:**
- Descriptor presence
- Parser availability
- Package name matching
- CLR type availability
- Descriptor file validation

#### 2. Protocol Diagnostics
**File:** [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)

**Purpose:** Protocol diagnostics and health checks

**Diagnostics:**
- Registry summary logging
- Missing binding detection
- Health check reporting
- Coverage analysis

#### 3. Protocol Fingerprint
**File:** [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)

**Purpose:** Protocol fingerprint computation for validation

**Fingerprint:**
- Descriptor fingerprint (hash of all descriptor names)
- Computed fingerprint (hash of all message types)
- Fingerprint assertions for validation

### Validation Flow

```
┌─────────────────────────────────────────────────┐
│ ProtocolRegistry.ValidateBindings()          │
│  ┌──────────────────────────────────────┐  │
│  │ 1. ProtoFingerprint.Assert...     │  │
│  │ 2. Check descriptor names          │  │
│  │ 3. Validate parsers              │  │
│  │ 4. Check packages                │  │
│  │ 5. EnsureRequiredBindings()       │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Testing

### Dummy Protocol Client

**File:** [`GameServer/Testing/DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs)

**Purpose:** Protocol round-trip testing and validation

**Test Methods:**

#### 1. BuildTimeUpdateRoundTrip()
- Creates TimeUpdateBroadcast message
- Serializes and deserializes locally
- Validates data integrity
- Returns ProtocolRoundTripResult with signature

#### 2. BuildChunkLoadRequestRoundTrip()
- Creates ChunkLoadRequest with multiple chunk positions
- Serializes and deserializes locally
- Validates view distance and chunk positions
- Returns ProtocolRoundTripResult with signature

#### 3. BuildBlockChangeRoundTrip()
- Creates BlockChangeBroadcast with drops
- Serializes and deserializes locally
- Validates block change and item drops
- Returns ProtocolRoundTripResult with signature

### Audit Functions

#### AuditProtocolRegistry()
- Calls ProtocolRegistry.ValidateBindings()
- Calls ProtocolValidator.ValidateEnhancedContracts()
- Calls ProtoRuntime.EnsureInitialized()
- Calls ProtoFingerprint.AssertDescriptorFingerprint()
- Calls ProtoDiagnostics.AssertRegistryClean()
- Calls ProtocolRegistry.EnsureRequiredBindings()
- Reports optional missing bindings

### Test Execution

```bash
# Run dummy client tests
dotnet run --project GameServer/GameServer.csproj -- --selftest

# Send test packets to running server
dotnet run --project GameServer/GameServer.csproj -- --dummy-client
```

---

## Client Integration

### Network Manager

**File:** [`Assets/MyAssets/Scripts/Network/NetworkManager.cs`](../Assets/MyAssets/Scripts/Network/NetworkManager.cs)

**Responsibilities:**
- TCP connection management
- Frame header parsing
- Message deserialization
- Handler dispatching
- Reconnection logic

### Message Dispatcher

**File:** [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs)

**Responsibilities:**
- Message routing to handlers
- Handler registration
- Async message processing
- Error handling

### Session Management

**File:** [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs)

**Responsibilities:**
- Session state tracking
- Incoming message handling
- Outgoing message handling
- Player session data

---

## Server Integration

### Handler Registration

Handlers are registered with the MessageDispatcher to handle specific message types:

#### Network Handlers
- [`LoginHandler`](../GameServer/Handlers/LoginHandler.cs) - Authentication
- [`MovementHandler`](../GameServer/Handlers/MovementHandler.cs) - Player movement
- [`WorldBlockHandler`](../GameServer/Handlers/WorldBlockHandler.cs) - Block interactions
- [`MinecraftChunkHandler`](../GameServer/Handlers/MinecraftChunkHandler.cs) - Chunk loading
- [`InventoryHandler`](../GameServer/Handlers/InventoryHandler.cs) - Inventory management
- [`CraftingHandler`](../GameServer/Handlers/CraftingHandler.cs) - Crafting system
- [`FoodSystemHandler`](../GameServer/Handlers/FoodSystemHandler.cs) - Food consumption
- [`HealthHandler`](../GameServer/Handlers/HealthHandler.cs) - Health management
- [`ChatHandler`](../GameServer/Handlers/ChatHandler.cs) - Chat system
- [`PlayerAttackHandler`](../GameServer/Handlers/PlayerAttackHandler.cs) - Combat
- [`MinecraftPlayerActionHandler`](../GameServer/Handlers/MinecraftPlayerActionHandler.cs) - Player actions

### Message Processing Flow

```
┌─────────────────────────────────────────────────┐
│ MessageDispatcher.Dispatch(messageType, payload) │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. Lookup handler by message type   │  │
│  │ 2. Deserialize protobuf payload       │  │
│  │ 3. Call handler with message data   │  │
│  │ 4. Handler processes request       │  │
│  │ 5. Handler returns response        │  │
│  │ 6. Serialize response              │  │
│  │ 7. Send response back to client   │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Protocol Best Practices

### 1. Always Validate Before Processing
- Check message type is registered
- Validate protobuf parsing
- Verify message integrity

### 2. Use Protocol Registry
- Never hardcode message types
- Use ProtocolRegistry.EnsureRegistered()
- Check for optional bindings

### 3. Handle Errors Gracefully
- Log serialization errors
- Send error responses to client
- Maintain connection stability

### 4. Test Round-Trip
- Verify serialization/deserialization
- Test with dummy client
- Validate data integrity

### 5. Keep Protobuf Definitions Updated
- Regenerate when proto files change
- Update registry bindings
- Test after regeneration

---

## Protocol Maintenance

### Regenerating Protobuf Code

When protocol definitions change, regenerate code:

```bash
# Server-side
protoc -I proto --csharp_out=SharedProtocol/Proto proto/*.proto

# Client-side
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

### Updating Protocol Registry

After regeneration:
1. Update ProtocolRegistry bindings
2. Add new message types to enum
3. Register new message factories
4. Update validation logic
5. Test with dummy client

### Protocol Versioning

- **Current Version:** `2026-01-27-hydrology-shield-v4-flow-lock`
- **Version Tracking:** WorldMapSignature.cs
- **Signature Fields:** Hydrology parameters, proto fingerprint
- **Backward Compatibility:** Maintained through signature validation

---

## Troubleshooting

### Common Issues

#### 1. Missing Descriptor
**Symptom:** ProtocolValidator throws about missing descriptor
**Solution:** Regenerate protobuf code from proto files

#### 2. Parser Not Available
**Symptom:** ProtocolValidator throws about missing parser
**Solution:** Check protobuf generation output, rebuild project

#### 3. Registry Mismatch
**Symptom:** Message type not registered
**Solution:** Update ProtocolRegistry with new binding

#### 4. Serialization Error
**Symptom:** Protobuf serialization fails
**Solution:** Check message structure, verify proto definition

### Debugging Tools

- **Dummy Protocol Client:** Test message round-trips
- **Protocol Diagnostics:** Registry health checks
- **Protocol Validator:** Comprehensive validation
- **Logging:** Server and client logs

---

## Future Enhancements

### Short Term
1. Add missing round-trip tests for optional packets
2. Improve error messages in protocol validation
3. Add protocol performance metrics

### Medium Term
1. Implement packet compression
2. Add protocol version negotiation
3. Implement protocol upgrade mechanism

### Long Term
1. Add protocol documentation generator
2. Implement protocol fuzzing tests
3. Add protocol compatibility matrix

---

## References

### Related Documentation
- [`README.md`](../README.md) - Project overview
- [`docs/architecture_overview_2026-01-27-session-23.md`](architecture_overview_2026-01-27-session-23.md) - System architecture
- [`plans/2026-01-27-session-23-comprehensive-implementation-plan.md`](../plans/2026-01-27-session-23-comprehensive-implementation-plan.md) - Implementation plan

### Protocol Files
- [`proto/common.proto`](../proto/common.proto)
- [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto)
- [`proto/game_auth.proto`](../proto/game_auth.proto)
- [`proto/game_chat.proto`](../proto/game_chat.proto)
- [`proto/game_core.proto`](../proto/game_core.proto)
- [`proto/game_diag.proto`](../proto/game_diag.proto)
- [`proto/game_move.proto`](../proto/game_move.proto)
- [`proto/game_world.proto`](../proto/game_world.proto)

### Generated Code
- [`SharedProtocol/Proto/`](../SharedProtocol/Proto/) - Server-side generated code
- [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/) - Client-side generated code

### Protocol Components
- [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs) - Message registry
- [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs) - Protocol validation
- [`SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs) - Runtime management
- [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs) - Diagnostics
- [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs) - Fingerprint
- [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs) - Message dispatching
- [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs) - Session management
- [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs) - World sync messages
- [`GameServer/Testing/DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs) - Testing client

---

**Document Version:** 1.0
**Last Updated:** 2026-01-27
**Author:** Kilo Code
**Session:** 23 - Comprehensive Implementation & Validation

## Table of Contents
1. [Protocol Overview](#protocol-overview)
2. [Protocol Stack](#protocol-stack)
3. [Message Types](#message-types)
4. [Protocol Registry](#protocol-registry)
5. [Message Flow](#message-flow)
6. [Serialization](#serialization)
7. [Validation](#validation)
8. [Testing](#testing)
9. [Client Integration](#client-integration)
10. [Server Integration](#server-integration)

---

## Protocol Overview

The HELLO_MY_WORLD project uses Google.Protobuf for client-server communication. The protocol is designed to be efficient, extensible, and type-safe.

### Protocol Characteristics
- **Format:** Google.Protobuf (Protocol Buffers)
- **Transport:** TCP/IP
- **Frame Format:** 6-byte header (4-byte big-endian length + 2-byte big-endian message type) + payload
- **Byte Order:** Big-endian (network byte order)
- **Encoding:** Binary protobuf encoding
- **Compression:** None (future enhancement)

### Protocol Versions
- **EnhancedMinecraftProtocol:** Current active protocol
- **Legacy protobuf-net:** Deprecated but still referenced in some handlers
- **Protocol Signature:** `2026-01-27-hydrology-shield-v4-flow-lock`

---

## Protocol Stack

### Protocol Definition Files

**Location:** [`proto/`](../proto/)

#### 1. common.proto
- **Purpose:** Common message definitions
- **Key Messages:**
  - Vector3Int
  - Vector3
  - BlockPosition
  - ItemStack

#### 2. enhanced_minecraft_game.proto
- **Purpose:** Enhanced Minecraft-specific messages
- **Key Messages:**
  - TimeUpdateBroadcast
  - ChunkLoadRequest
  - ChunkLoadResponse
  - ChunkUnloadNotification
  - ChunkUnloadAck
  - BlockChangeBroadcast
  - EntitySpawnBroadcast
  - EntityDespawnBroadcast
  - WeatherUpdateBroadcast
  - SoundEffect
  - ParticleEffect

#### 3. game_auth.proto
- **Purpose:** Authentication and authorization
- **Key Messages:**
  - LoginRequest
  - LoginResponse
  - LogoutRequest
  - LogoutResponse

#### 4. game_chat.proto
- **Purpose:** Chat system
- **Key Messages:**
  - ChatMessage
  - ChatBroadcast

#### 5. game_core.proto
- **Purpose:** Core gameplay messages
- **Key Messages:**
  - PlayerInfo
  - InventoryItem
  - PlayerActionRequest
  - PlayerActionResponse

#### 6. game_diag.proto
- **Purpose:** Diagnostics and debugging
- **Key Messages:**
  - DiagnosticRequest
  - DiagnosticResponse

#### 7. game_move.proto
- **Purpose:** Player movement
- **Key Messages:**
  - MovementRequest
  - MovementResponse
  - PositionUpdate

#### 8. game_world.proto
- **Purpose:** World and block interactions
- **Key Messages:**
  - WorldBlockChangeRequest
  - WorldBlockChangeResponse
  - WorldBlockChangeBroadcast

### Generated Code

#### Server-Side Generation
- **Location:** [`SharedProtocol/`](../SharedProtocol/)
- **Build Process:**
  ```bash
  protoc -I proto --csharp_out=SharedProtocol/Proto proto/*.proto
  ```
- **Output:** C# classes in `SharedProtocol/Proto/`

#### Client-Side Generation
- **Location:** [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)
- **Build Process:**
  ```bash
  protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
  ```
- **Output:** C# classes for Unity

---

## Message Types

### Message Type Enumeration

**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)

```csharp
public enum MinecraftMessageType
{
    PlayerStateUpdate = 1,
    PlayerActionRequest = 2,
    PlayerActionResponse = 3,
    ChunkDataRequest = 4,
    ChunkDataResponse = 5,
    ChunkUnloadNotification = 6,
    ChunkUnloadAcknowledge = 7,
    BlockChangeNotification = 8,
    EntitySpawn = 9,
    EntityDespawn = 10,
    TimeUpdate = 11,
    WeatherChange = 12,
    SoundEffect = 13,
    ParticleEffect = 14,
    // ... additional message types
}
```

### Required Messages (13)

These messages are required for core functionality and must be registered in the protocol registry.

1. **PlayerStateUpdate** - Player state synchronization
2. **PlayerActionRequest** - Player action requests
3. **PlayerActionResponse** - Player action responses
4. **ChunkDataRequest** - Chunk loading request
5. **ChunkDataResponse** - Chunk data response
6. **ChunkUnloadNotification** - Chunk unload notification
7. **ChunkUnloadAcknowledge** - Chunk unload acknowledgement
8. **BlockChangeNotification** - Block change broadcast
9. **EntitySpawn** - Entity spawn broadcast
10. **EntityDespawn** - Entity despawn broadcast
11. **TimeUpdate** - World time update
12. **WeatherChange** - Weather change broadcast
13. **SoundEffect** - Sound effect

### Optional Messages (10)

These messages are tracked but not required for core functionality.

1. **MultiBlockChange** - Multiple block changes
2. **InventoryUpdate** - Inventory state update
3. **ItemUse** - Item usage
4. **ItemDrop** - Item drop
5. **ItemPickup** - Item pickup
6. **EntityUpdate** - Entity state update
7. **EntityInteract** - Entity interaction
8. **ContainerOpen** - Container open
9. **ContainerClose** - Container close
10. **ContainerUpdate** - Container update

---

## Protocol Registry

### Registry Implementation

**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)

**Purpose:** Central registry mapping message types to protobuf message factories

**Key Features:**
- Message type to descriptor name mapping
- Message factory functions for creating prototypes
- Required binding enforcement
- Optional binding tracking
- Descriptor validation
- Fingerprint computation

### Registry Bindings

The registry maintains bindings for all required message types:

```csharp
private static readonly ProtocolBinding[] Bindings =
{
    new(MinecraftMessageType.PlayerStateUpdate, nameof(EnhancedMinecraftProtocol.PlayerInfo), () => new EnhancedMinecraftProtocol.PlayerInfo()),
    new(MinecraftMessageType.PlayerActionRequest, nameof(EnhancedMinecraftProtocol.PlayerActionRequest), () => new EnhancedMinecraftProtocol.PlayerActionRequest()),
    new(MinecraftMessageType.PlayerActionResponse, nameof(EnhancedMinecraftProtocol.PlayerActionResponse), () => new EnhancedMinecraftProtocol.PlayerActionResponse()),
    new(MinecraftMessageType.ChunkDataRequest, nameof(EnhancedMinecraftProtocol.ChunkLoadRequest), () => new EnhancedMinecraftProtocol.ChunkLoadRequest()),
    new(MinecraftMessageType.ChunkDataResponse, nameof(EnhancedMinecraftProtocol.ChunkLoadResponse), () => new EnhancedMinecraftProtocol.ChunkLoadResponse()),
    new(MinecraftMessageType.ChunkUnloadNotification, nameof(EnhancedMinecraftProtocol.ChunkUnloadNotification), () => new EnhancedMinecraftProtocol.ChunkUnloadNotification()),
    new(MinecraftMessageType.ChunkUnloadAcknowledge, nameof(EnhancedMinecraftProtocol.ChunkUnloadAck), () => new EnhancedMinecraftProtocol.ChunkUnloadAck()),
    new(MinecraftMessageType.BlockChangeNotification, nameof(EnhancedMinecraftProtocol.BlockChangeBroadcast), () => new EnhancedMinecraftProtocol.BlockChangeBroadcast()),
    new(MinecraftMessageType.EntitySpawn, nameof(EnhancedMinecraftProtocol.EntitySpawnBroadcast), () => new EnhancedMinecraftProtocol.EntitySpawnBroadcast()),
    new(MinecraftMessageType.EntityDespawn, nameof(EnhancedMinecraftProtocol.EntityDespawnBroadcast), () => new EnhancedMinecraftProtocol.EntityDespawnBroadcast()),
    new(MinecraftMessageType.TimeUpdate, nameof(EnhancedMinecraftProtocol.TimeUpdateBroadcast), () => new EnhancedMinecraftProtocol.TimeUpdateBroadcast()),
    new(MinecraftMessageType.WeatherChange, nameof(EnhancedMinecraftProtocol.WeatherUpdateBroadcast), () => new EnhancedMinecraftProtocol.WeatherUpdateBroadcast()),
    new(MinecraftMessageType.SoundEffect, nameof(EnhancedMinecraftProtocol.SoundEffect), () => new EnhancedMinecraftProtocol.SoundEffect()),
    new(MinecraftMessageType.ParticleEffect, nameof(EnhancedMinecraftProtocol.ParticleEffect), () => new EnhancedMinecraftProtocol.ParticleEffect())
};
```

### Validation Methods

#### 1. ValidateBindings()
- Validates all registered bindings
- Checks descriptor names match generated protobuf classes
- Ensures parsers are available
- Validates package names
- Throws on mismatch

#### 2. EnsureRegistered()
- Throws if message type is not registered
- Used for early validation during handler registration
- Prevents stale IDL changes

#### 3. EnsureRequiredBindings()
- Throws if any required (non-optional) message type is missing
- Ensures core functionality is available

#### 4. GetUnregisteredOptionalTypes()
- Returns optional message types that are not bound
- Used for auditing and reporting

---

## Message Flow

### Client to Server Flow

```
┌──────────────┐
│ Unity Client │
└──────┬───────┘
       │ TCP/IP
       │
┌──────▼──────────────────────────────────────────┐
│         .NET 6.0 Server                    │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. Receive framed packet            │  │
│  │ 2. Parse 6-byte header             │  │
│  │ 3. Extract message type and payload   │  │
│  │ 4. Route to MessageDispatcher       │  │
│  │ 5. Dispatch to appropriate handler  │  │
│  │ 6. Handler processes request       │  │
│  │ 7. Update world state             │  │
│  │ 8. Send response packet(s)       │  │
│  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### Frame Format

**Header (6 bytes):**
- Bytes 0-3: Payload length (big-endian, 32-bit integer)
- Bytes 4-5: Message type (big-endian, 16-bit integer)

**Payload (variable length):**
- Protobuf serialized message

**Total Frame:**
```
+-------------------+-------------------+
| Length (4 bytes) | Type (2 bytes) |
+-------------------+-------------------+
|              Payload (N bytes)              |
+-----------------------------------------+
```

---

## Serialization

### Serialization Process

#### Client-Side (Unity)
1. Create message instance (e.g., `ChunkLoadRequest`)
2. Set message properties
3. Serialize to byte array: `message.ToByteArray()`
4. Build frame with 6-byte header
5. Send via TCP socket

#### Server-Side (.NET)
1. Receive frame from TCP socket
2. Parse 6-byte header
3. Read payload bytes
4. Deserialize: `ChunkLoadRequest.Parser.ParseFrom(payload)`
5. Process message
6. Serialize response
7. Build frame with 6-byte header
8. Send back to client

### Serialization Performance

- **Efficiency:** Protobuf is highly efficient binary format
- **Size:** Typically 50-80% smaller than JSON
- **Speed:** Fast serialization/deserialization
- **Memory:** Zero-copy parsing where possible

---

## Validation

### Protocol Validation Components

#### 1. Protocol Validator
**File:** [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs)

**Purpose:** Comprehensive protocol validation

**Validations:**
- Descriptor presence
- Parser availability
- Package name matching
- CLR type availability
- Descriptor file validation

#### 2. Protocol Diagnostics
**File:** [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs)

**Purpose:** Protocol diagnostics and health checks

**Diagnostics:**
- Registry summary logging
- Missing binding detection
- Health check reporting
- Coverage analysis

#### 3. Protocol Fingerprint
**File:** [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs)

**Purpose:** Protocol fingerprint computation for validation

**Fingerprint:**
- Descriptor fingerprint (hash of all descriptor names)
- Computed fingerprint (hash of all message types)
- Fingerprint assertions for validation

### Validation Flow

```
┌─────────────────────────────────────────────────┐
│ ProtocolRegistry.ValidateBindings()          │
│  ┌──────────────────────────────────────┐  │
│  │ 1. ProtoFingerprint.Assert...     │  │
│  │ 2. Check descriptor names          │  │
│  │ 3. Validate parsers              │  │
│  │ 4. Check packages                │  │
│  │ 5. EnsureRequiredBindings()       │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Testing

### Dummy Protocol Client

**File:** [`GameServer/Testing/DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs)

**Purpose:** Protocol round-trip testing and validation

**Test Methods:**

#### 1. BuildTimeUpdateRoundTrip()
- Creates TimeUpdateBroadcast message
- Serializes and deserializes locally
- Validates data integrity
- Returns ProtocolRoundTripResult with signature

#### 2. BuildChunkLoadRequestRoundTrip()
- Creates ChunkLoadRequest with multiple chunk positions
- Serializes and deserializes locally
- Validates view distance and chunk positions
- Returns ProtocolRoundTripResult with signature

#### 3. BuildBlockChangeRoundTrip()
- Creates BlockChangeBroadcast with drops
- Serializes and deserializes locally
- Validates block change and item drops
- Returns ProtocolRoundTripResult with signature

### Audit Functions

#### AuditProtocolRegistry()
- Calls ProtocolRegistry.ValidateBindings()
- Calls ProtocolValidator.ValidateEnhancedContracts()
- Calls ProtoRuntime.EnsureInitialized()
- Calls ProtoFingerprint.AssertDescriptorFingerprint()
- Calls ProtoDiagnostics.AssertRegistryClean()
- Calls ProtocolRegistry.EnsureRequiredBindings()
- Reports optional missing bindings

### Test Execution

```bash
# Run dummy client tests
dotnet run --project GameServer/GameServer.csproj -- --selftest

# Send test packets to running server
dotnet run --project GameServer/GameServer.csproj -- --dummy-client
```

---

## Client Integration

### Network Manager

**File:** [`Assets/MyAssets/Scripts/Network/NetworkManager.cs`](../Assets/MyAssets/Scripts/Network/NetworkManager.cs)

**Responsibilities:**
- TCP connection management
- Frame header parsing
- Message deserialization
- Handler dispatching
- Reconnection logic

### Message Dispatcher

**File:** [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs)

**Responsibilities:**
- Message routing to handlers
- Handler registration
- Async message processing
- Error handling

### Session Management

**File:** [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs)

**Responsibilities:**
- Session state tracking
- Incoming message handling
- Outgoing message handling
- Player session data

---

## Server Integration

### Handler Registration

Handlers are registered with the MessageDispatcher to handle specific message types:

#### Network Handlers
- [`LoginHandler`](../GameServer/Handlers/LoginHandler.cs) - Authentication
- [`MovementHandler`](../GameServer/Handlers/MovementHandler.cs) - Player movement
- [`WorldBlockHandler`](../GameServer/Handlers/WorldBlockHandler.cs) - Block interactions
- [`MinecraftChunkHandler`](../GameServer/Handlers/MinecraftChunkHandler.cs) - Chunk loading
- [`InventoryHandler`](../GameServer/Handlers/InventoryHandler.cs) - Inventory management
- [`CraftingHandler`](../GameServer/Handlers/CraftingHandler.cs) - Crafting system
- [`FoodSystemHandler`](../GameServer/Handlers/FoodSystemHandler.cs) - Food consumption
- [`HealthHandler`](../GameServer/Handlers/HealthHandler.cs) - Health management
- [`ChatHandler`](../GameServer/Handlers/ChatHandler.cs) - Chat system
- [`PlayerAttackHandler`](../GameServer/Handlers/PlayerAttackHandler.cs) - Combat
- [`MinecraftPlayerActionHandler`](../GameServer/Handlers/MinecraftPlayerActionHandler.cs) - Player actions

### Message Processing Flow

```
┌─────────────────────────────────────────────────┐
│ MessageDispatcher.Dispatch(messageType, payload) │
│  ┌──────────────────────────────────────────┐  │
│  │ 1. Lookup handler by message type   │  │
│  │ 2. Deserialize protobuf payload       │  │
│  │ 3. Call handler with message data   │  │
│  │ 4. Handler processes request       │  │
│  │ 5. Handler returns response        │  │
│  │ 6. Serialize response              │  │
│  │ 7. Send response back to client   │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Protocol Best Practices

### 1. Always Validate Before Processing
- Check message type is registered
- Validate protobuf parsing
- Verify message integrity

### 2. Use Protocol Registry
- Never hardcode message types
- Use ProtocolRegistry.EnsureRegistered()
- Check for optional bindings

### 3. Handle Errors Gracefully
- Log serialization errors
- Send error responses to client
- Maintain connection stability

### 4. Test Round-Trip
- Verify serialization/deserialization
- Test with dummy client
- Validate data integrity

### 5. Keep Protobuf Definitions Updated
- Regenerate when proto files change
- Update registry bindings
- Test after regeneration

---

## Protocol Maintenance

### Regenerating Protobuf Code

When protocol definitions change, regenerate code:

```bash
# Server-side
protoc -I proto --csharp_out=SharedProtocol/Proto proto/*.proto

# Client-side
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

### Updating Protocol Registry

After regeneration:
1. Update ProtocolRegistry bindings
2. Add new message types to enum
3. Register new message factories
4. Update validation logic
5. Test with dummy client

### Protocol Versioning

- **Current Version:** `2026-01-27-hydrology-shield-v4-flow-lock`
- **Version Tracking:** WorldMapSignature.cs
- **Signature Fields:** Hydrology parameters, proto fingerprint
- **Backward Compatibility:** Maintained through signature validation

---

## Troubleshooting

### Common Issues

#### 1. Missing Descriptor
**Symptom:** ProtocolValidator throws about missing descriptor
**Solution:** Regenerate protobuf code from proto files

#### 2. Parser Not Available
**Symptom:** ProtocolValidator throws about missing parser
**Solution:** Check protobuf generation output, rebuild project

#### 3. Registry Mismatch
**Symptom:** Message type not registered
**Solution:** Update ProtocolRegistry with new binding

#### 4. Serialization Error
**Symptom:** Protobuf serialization fails
**Solution:** Check message structure, verify proto definition

### Debugging Tools

- **Dummy Protocol Client:** Test message round-trips
- **Protocol Diagnostics:** Registry health checks
- **Protocol Validator:** Comprehensive validation
- **Logging:** Server and client logs

---

## Future Enhancements

### Short Term
1. Add missing round-trip tests for optional packets
2. Improve error messages in protocol validation
3. Add protocol performance metrics

### Medium Term
1. Implement packet compression
2. Add protocol version negotiation
3. Implement protocol upgrade mechanism

### Long Term
1. Add protocol documentation generator
2. Implement protocol fuzzing tests
3. Add protocol compatibility matrix

---

## References

### Related Documentation
- [`README.md`](../README.md) - Project overview
- [`docs/architecture_overview_2026-01-27-session-23.md`](architecture_overview_2026-01-27-session-23.md) - System architecture
- [`plans/2026-01-27-session-23-comprehensive-implementation-plan.md`](../plans/2026-01-27-session-23-comprehensive-implementation-plan.md) - Implementation plan

### Protocol Files
- [`proto/common.proto`](../proto/common.proto)
- [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto)
- [`proto/game_auth.proto`](../proto/game_auth.proto)
- [`proto/game_chat.proto`](../proto/game_chat.proto)
- [`proto/game_core.proto`](../proto/game_core.proto)
- [`proto/game_diag.proto`](../proto/game_diag.proto)
- [`proto/game_move.proto`](../proto/game_move.proto)
- [`proto/game_world.proto`](../proto/game_world.proto)

### Generated Code
- [`SharedProtocol/Proto/`](../SharedProtocol/Proto/) - Server-side generated code
- [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/) - Client-side generated code

### Protocol Components
- [`SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs) - Message registry
- [`SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolValidator.cs) - Protocol validation
- [`SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs`](../SharedProtocol/EnhancedMinecraft/ProtoRuntime.cs) - Runtime management
- [`SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs`](../SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs) - Diagnostics
- [`SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs`](../SharedProtocol/EnhancedMinecraft/ProtoFingerprint.cs) - Fingerprint
- [`SharedProtocol/MessageDispatcher.cs`](../SharedProtocol/MessageDispatcher.cs) - Message dispatching
- [`SharedProtocol/Session.cs`](../SharedProtocol/Session.cs) - Session management
- [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs) - World sync messages
- [`GameServer/Testing/DummyProtocolClient.cs`](../GameServer/Testing/DummyProtocolClient.cs) - Testing client

---

**Document Version:** 1.0
**Last Updated:** 2026-01-27
**Author:** Kilo Code
**Session:** 23 - Comprehensive Implementation & Validation

