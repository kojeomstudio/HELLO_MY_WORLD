# Protobuf Protocol Documentation

## Overview

This document describes the Protocol Buffers (protobuf) protocol used for communication between the client and server in the Minecraft-like game. The protocol uses Google Protocol Buffers for efficient binary serialization.

## Architecture

### Core Components

1. **Proto Files** - Protocol definition files in [`proto/`](../proto/) directory
2. **Generated Code** - Auto-generated C# code in [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)
3. **SharedProtocol** - Shared protocol library with message definitions
4. **ProtocolRegistry** - Registry for message type to handler mapping

### Dependencies

- **Google.Protobuf**: 3.27.2
- **protobuf-net**: 3.2.18 (warning: 3.2.26 found)

## Proto Files

### [`proto/common.proto`](../proto/common.proto)

Defines common data structures used across all protocol messages.

#### Common Types

- **Vector3**: 3D vector with x, y, z components
- **Vector3Int**: 3D integer vector
- **Vector2**: 2D vector with x, y components
- **Vector2Int**: 2D integer vector
- **Color**: RGBA color values
- **Timestamp**: Unix timestamp in milliseconds

#### Common Enums

```protobuf
enum ResultStatus {
    SUCCESS = 0;
    FAILURE = 1;
    ERROR = 2;
    PENDING = 3;
}

enum GameMode {
    SURVIVAL = 0;
    CREATIVE = 1;
    ADVENTURE = 2;
    SPECTATOR = 3;
}

enum Difficulty {
    PEACEFUL = 0;
    EASY = 1;
    NORMAL = 2;
    HARD = 3;
}

enum Dimension {
    OVERWORLD = 0;
    NETHER = 1;
    END = 2;
}

enum Weather {
    CLEAR = 0;
    RAIN = 1;
    THUNDER = 2;
}

enum TimeOfDay {
    DAY = 0;
    SUNSET = 1;
    NIGHT = 2;
    SUNRISE = 3;
}
```

### [`proto/game_core.proto`](../proto/game_core.proto)

Defines core game protocol messages.

### [`proto/game_world.proto`](../proto/game_world.proto)

Defines world-related protocol messages.

#### World Messages

```protobuf
message WorldBlockChangeRequest {
    Vector3Int position = 1;
    int32 blockType = 2;
}

message WorldBlockChangeResponse {
    ResultStatus status = 1;
    string message = 2;
}

message WorldBlockChangeBroadcast {
    Vector3Int position = 1;
    int32 blockType = 2;
    string playerId = 3;
}

message ChunkDataRequest {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkDataResponse {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    bytes blockData = 3;
    bytes biomeData = 4;
}
```

### [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto)

Defines enhanced Minecraft protocol messages.

#### Package: EnhancedMinecraftProtocol

#### Enhanced Messages

```protobuf
message PlayerInfo {
    string playerId = 1;
    string playerName = 2;
    Vector3 position = 3;
    PlayerStats stats = 4;
    PlayerInventory inventory = 5;
}

message PlayerStats {
    int32 health = 1;
    int32 maxHealth = 2;
    int32 foodLevel = 3;
    float saturation = 4;
    int32 experienceLevel = 5;
    float experienceProgress = 6;
}

message PlayerInventory {
    repeated InventorySlot slots = 1;
    int32 selectedSlot = 2;
}

message InventorySlot {
    int32 slotIndex = 1;
    ItemStack item = 2;
}

message ItemStack {
    int32 itemId = 1;
    int32 count = 2;
    repeated Enchantment enchantments = 3;
}

message Enchantment {
    int32 enchantmentId = 1;
    int32 level = 2;
}
```

#### Chunk Messages

```protobuf
message ChunkLoadRequest {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkLoadResponse {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    ChunkData chunkData = 3;
}

message ChunkUnloadNotification {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkUnloadAck {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkData {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    repeated int32 blocks = 3;
    repeated int32 biomes = 4;
    repeated TileEntityData tileEntities = 5;
}
```

#### Block Messages

```protobuf
message BlockBreakStartRequest {
    Vector3Int position = 1;
}

message BlockBreakStartResponse {
    ResultStatus status = 1;
    float breakTime = 2;
}

message BlockBreakProgressUpdate {
    Vector3Int position = 1;
    float progress = 2;
}

message BlockBreakCompleteRequest {
    Vector3Int position = 1;
}

message BlockBreakCompleteResponse {
    ResultStatus status = 1;
    repeated ItemStack drops = 2;
}

message BlockPlaceRequest {
    Vector3Int position = 1;
    int32 blockType = 2;
}

message BlockPlaceResponse {
    ResultStatus status = 1;
}

message BlockChangeBroadcast {
    Vector3Int position = 1;
    int32 blockType = 2;
    string playerId = 3;
}
```

#### Entity Messages

```protobuf
message EntityData {
    string entityId = 1;
    string entityType = 2;
    Vector3 position = 3;
    Vector3 velocity = 4;
    EntityMetadata metadata = 5;
}

message EntityMetadata {
    map<string, string> data = 1;
}

message EntitySpawnBroadcast {
    EntityData entity = 1;
}

message EntityDespawnBroadcast {
    string entityId = 1;
}
```

#### Combat Messages

```protobuf
message CombatEvent {
    string attackerId = 1;
    string targetId = 2;
    int32 damage = 3;
}

message DeathEvent {
    string playerId = 1;
    string deathMessage = 2;
}
```

#### Experience Messages

```protobuf
message ExperienceUpdateBroadcast {
    string playerId = 1;
    int32 level = 2;
    float progress = 3;
}

message ExperienceOrbSpawnBroadcast {
    string orbId = 1;
    Vector3 position = 2;
    int32 experienceValue = 3;
}
```

#### Effect Messages

```protobuf
message ActiveEffect {
    int32 effectId = 1;
    int32 duration = 2;
    int32 amplifier = 3;
}

message EffectUpdateBroadcast {
    string playerId = 1;
    repeated ActiveEffect effects = 2;
}
```

#### Visual Messages

```protobuf
message ParticleEffect {
    int32 particleType = 1;
    Vector3 position = 2;
    Vector3 velocity = 3;
    int32 count = 4;
}

message SoundEffect {
    int32 soundId = 1;
    Vector3 position = 2;
    float volume = 3;
    float pitch = 4;
}
```

#### Chat Messages

```protobuf
message ChatMessage {
    string message = 1;
    ChatStyle style = 2;
    string sender = 3;
}

message ChatStyle {
    string color = 1;
    bool bold = 2;
    bool italic = 3;
    bool underlined = 4;
}
```

#### World Messages

```protobuf
message WorldInfo {
    string worldName = 1;
    int32 seed = 2;
    Dimension dimension = 3;
    Difficulty difficulty = 4;
    GameMode gameMode = 5;
}

message WeatherInfo {
    Weather weather = 1;
    int32 duration = 2;
}

message WorldBorder {
    Vector2 center = 1;
    double size = 2;
    double warningDistance = 3;
    int32 warningTime = 4;
}

message TimeUpdateBroadcast {
    int64 worldTime = 1;
    int32 dayTime = 2;
}

message WeatherUpdateBroadcast {
    Weather weather = 1;
    int32 duration = 2;
}
```

## Generated Code

### [`Assets/Generated/Protobuf/Common.cs`](../Assets/Generated/Protobuf/Common.cs)

Auto-generated from [`proto/common.proto`](../proto/common.proto).

### [`Assets/Generated/Protobuf/GameCore.cs`](../Assets/Generated/Protobuf/GameCore.cs)

Auto-generated from [`proto/game_core.proto`](../proto/game_core.proto).

### [`Assets/Generated/Protobuf/GameWorld.cs`](../Assets/Generated/Protobuf/GameWorld.cs)

Auto-generated from [`proto/game_world.proto`](../proto/game_world.proto).

### [`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`](../Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)

Auto-generated from [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto).

## Shared Protocol

### [`SharedProtocol/Messages.cs`](../SharedProtocol/Messages.cs)

Defines all protocol messages using ProtoBuf attributes.

### [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs)

Defines world synchronization messages.

#### WorldSync Messages

```csharp
[ProtoContract]
public class WorldBlockChangeBatchBroadcast
{
    [ProtoMember(1)]
    public List<WorldBlockChangeData> Changes { get; set; }
}

[ProtoContract]
public class WorldBlockChangeData
{
    [ProtoMember(1)]
    public Vector3Int Position { get; set; }
    
    [ProtoMember(2)]
    public int BlockType { get; set; }
    
    [ProtoMember(3)]
    public string PlayerId { get; set; }
}

[ProtoContract]
public class PlayerPositionUpdate
{
    [ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoMember(2)]
    public Vector3 Position { get; set; }
    
    [ProtoMember(3)]
    public Vector3 Rotation { get; set; }
}

[ProtoContract]
public class ChunkDataMessage
{
    [ProtoMember(1)]
    public int ChunkX { get; set; }
    
    [ProtoMember(2)]
    public int ChunkZ { get; set; }
    
    [ProtoMember(3)]
    public byte[] BlockData { get; set; }
    
    [ProtoMember(4)]
    public byte[] BiomeData { get; set; }
}

[ProtoContract]
public class ChunkUnloadMessage
{
    [ProtoMember(1)]
    public int ChunkX { get; set; }
    
    [ProtoMember(2)]
    public int ChunkZ { get; set; }
}
```

## Protocol Registry

The ProtocolRegistry maps message types to their handlers.

### Registered Handlers

- **LoginRequest** → LoginHandler
- **MoveRequest** → MovementHandler
- **WorldBlockChangeRequest** → WorldBlockHandler
- **InventoryRequest** → InventoryHandler
- **CraftingRequest** → CraftingHandler
- **RecipeListRequest** → RecipeListHandler
- **RoomListRequest** → RoomListHandler
- **RoomEnterRequest** → RoomEnterHandler
- **RoomLeaveRequest** → RoomLeaveHandler
- **HealthActionRequest** → HealthHandler
- **RespawnRequest** → RespawnHandler
- **ChatRequest** → ChatHandler
- **PingRequest** → PingHandler
- **ServerStatusRequest** → ServerStatusHandler
- **AISpawnRequest** → AISpawnHandler
- **AIDebugInfoRequest** → AIDebugInfoHandler
- **PlayerAttackRequest** → PlayerAttackHandler
- **CommandRequest** → CommandHandler

### Optional Packets (Not Bound)

The following packets are optional and not currently bound in the ProtocolRegistry:

- MultiBlockChange
- InventoryUpdate
- ItemUse
- ItemDrop
- ItemPickup
- EntityUpdate
- EntityInteract
- ContainerOpen
- ContainerClose
- ContainerUpdate

## Message Type Mapping

### EnhancedMinecraftProtocol Mappings

| Internal Type | Proto Message |
|---------------|---------------|
| PlayerStateUpdate | PlayerInfo |
| PlayerActionRequest | PlayerActionRequest |
| PlayerActionResponse | PlayerActionResponse |
| ChunkDataRequest | ChunkLoadRequest |
| ChunkDataResponse | ChunkLoadResponse |
| ChunkUnloadNotification | ChunkUnloadNotification |
| ChunkUnloadAcknowledge | ChunkUnloadAck |
| BlockChangeNotification | BlockChangeBroadcast |
| EntitySpawn | EntitySpawnBroadcast |
| EntityDespawn | EntityDespawnBroadcast |
| TimeUpdate | TimeUpdateBroadcast |
| WeatherChange | WeatherUpdateBroadcast |
| SoundEffect | SoundEffect |
| ParticleEffect | ParticleEffect |

## Fingerprint Validation

The system validates proto file fingerprints to ensure compatibility:

- **Expected Fingerprint**: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`
- **Computed Fingerprint**: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`

## Regenerating Protobuf Code

To regenerate the protobuf C# code:

```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

## Warnings and Recommendations

### Protobuf Version Mismatch

- **Warning**: SharedProtocol requires protobuf-net >= 3.2.18 but found 3.2.26
- **Recommendation**: Update package references to use consistent version

### Optional Packet Bindings

- **Warning**: Optional EnhancedMinecraft packets not registered
- **Recommendation**: These are optional features; register bindings when needed

### Missing Handlers

- **Warning**: Some EnhancedMinecraft packets have no handlers
- **Recommendation**: Implement handlers as needed for feature development

## Future Improvements

1. **Consistent protobuf-net version**: Update all references to use the same version
2. **Complete handler implementation**: Implement handlers for all EnhancedMinecraft packets
3. **Optional packet registration**: Register optional packets when features are implemented
4. **Protocol versioning**: Add protocol version tracking for compatibility
5. **Compression**: Add message compression for large payloads

## References

- [Terrain Generation Documentation](./terrain-generation.md)
- [World Map Control Documentation](./world-map-control.md)
- [Session 104 Summary](./session-104-summary.md)

## Overview

This document describes the Protocol Buffers (protobuf) protocol used for communication between the client and server in the Minecraft-like game. The protocol uses Google Protocol Buffers for efficient binary serialization.

## Architecture

### Core Components

1. **Proto Files** - Protocol definition files in [`proto/`](../proto/) directory
2. **Generated Code** - Auto-generated C# code in [`Assets/Generated/Protobuf/`](../Assets/Generated/Protobuf/)
3. **SharedProtocol** - Shared protocol library with message definitions
4. **ProtocolRegistry** - Registry for message type to handler mapping

### Dependencies

- **Google.Protobuf**: 3.27.2
- **protobuf-net**: 3.2.18 (warning: 3.2.26 found)

## Proto Files

### [`proto/common.proto`](../proto/common.proto)

Defines common data structures used across all protocol messages.

#### Common Types

- **Vector3**: 3D vector with x, y, z components
- **Vector3Int**: 3D integer vector
- **Vector2**: 2D vector with x, y components
- **Vector2Int**: 2D integer vector
- **Color**: RGBA color values
- **Timestamp**: Unix timestamp in milliseconds

#### Common Enums

```protobuf
enum ResultStatus {
    SUCCESS = 0;
    FAILURE = 1;
    ERROR = 2;
    PENDING = 3;
}

enum GameMode {
    SURVIVAL = 0;
    CREATIVE = 1;
    ADVENTURE = 2;
    SPECTATOR = 3;
}

enum Difficulty {
    PEACEFUL = 0;
    EASY = 1;
    NORMAL = 2;
    HARD = 3;
}

enum Dimension {
    OVERWORLD = 0;
    NETHER = 1;
    END = 2;
}

enum Weather {
    CLEAR = 0;
    RAIN = 1;
    THUNDER = 2;
}

enum TimeOfDay {
    DAY = 0;
    SUNSET = 1;
    NIGHT = 2;
    SUNRISE = 3;
}
```

### [`proto/game_core.proto`](../proto/game_core.proto)

Defines core game protocol messages.

### [`proto/game_world.proto`](../proto/game_world.proto)

Defines world-related protocol messages.

#### World Messages

```protobuf
message WorldBlockChangeRequest {
    Vector3Int position = 1;
    int32 blockType = 2;
}

message WorldBlockChangeResponse {
    ResultStatus status = 1;
    string message = 2;
}

message WorldBlockChangeBroadcast {
    Vector3Int position = 1;
    int32 blockType = 2;
    string playerId = 3;
}

message ChunkDataRequest {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkDataResponse {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    bytes blockData = 3;
    bytes biomeData = 4;
}
```

### [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto)

Defines enhanced Minecraft protocol messages.

#### Package: EnhancedMinecraftProtocol

#### Enhanced Messages

```protobuf
message PlayerInfo {
    string playerId = 1;
    string playerName = 2;
    Vector3 position = 3;
    PlayerStats stats = 4;
    PlayerInventory inventory = 5;
}

message PlayerStats {
    int32 health = 1;
    int32 maxHealth = 2;
    int32 foodLevel = 3;
    float saturation = 4;
    int32 experienceLevel = 5;
    float experienceProgress = 6;
}

message PlayerInventory {
    repeated InventorySlot slots = 1;
    int32 selectedSlot = 2;
}

message InventorySlot {
    int32 slotIndex = 1;
    ItemStack item = 2;
}

message ItemStack {
    int32 itemId = 1;
    int32 count = 2;
    repeated Enchantment enchantments = 3;
}

message Enchantment {
    int32 enchantmentId = 1;
    int32 level = 2;
}
```

#### Chunk Messages

```protobuf
message ChunkLoadRequest {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkLoadResponse {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    ChunkData chunkData = 3;
}

message ChunkUnloadNotification {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkUnloadAck {
    int32 chunkX = 1;
    int32 chunkZ = 2;
}

message ChunkData {
    int32 chunkX = 1;
    int32 chunkZ = 2;
    repeated int32 blocks = 3;
    repeated int32 biomes = 4;
    repeated TileEntityData tileEntities = 5;
}
```

#### Block Messages

```protobuf
message BlockBreakStartRequest {
    Vector3Int position = 1;
}

message BlockBreakStartResponse {
    ResultStatus status = 1;
    float breakTime = 2;
}

message BlockBreakProgressUpdate {
    Vector3Int position = 1;
    float progress = 2;
}

message BlockBreakCompleteRequest {
    Vector3Int position = 1;
}

message BlockBreakCompleteResponse {
    ResultStatus status = 1;
    repeated ItemStack drops = 2;
}

message BlockPlaceRequest {
    Vector3Int position = 1;
    int32 blockType = 2;
}

message BlockPlaceResponse {
    ResultStatus status = 1;
}

message BlockChangeBroadcast {
    Vector3Int position = 1;
    int32 blockType = 2;
    string playerId = 3;
}
```

#### Entity Messages

```protobuf
message EntityData {
    string entityId = 1;
    string entityType = 2;
    Vector3 position = 3;
    Vector3 velocity = 4;
    EntityMetadata metadata = 5;
}

message EntityMetadata {
    map<string, string> data = 1;
}

message EntitySpawnBroadcast {
    EntityData entity = 1;
}

message EntityDespawnBroadcast {
    string entityId = 1;
}
```

#### Combat Messages

```protobuf
message CombatEvent {
    string attackerId = 1;
    string targetId = 2;
    int32 damage = 3;
}

message DeathEvent {
    string playerId = 1;
    string deathMessage = 2;
}
```

#### Experience Messages

```protobuf
message ExperienceUpdateBroadcast {
    string playerId = 1;
    int32 level = 2;
    float progress = 3;
}

message ExperienceOrbSpawnBroadcast {
    string orbId = 1;
    Vector3 position = 2;
    int32 experienceValue = 3;
}
```

#### Effect Messages

```protobuf
message ActiveEffect {
    int32 effectId = 1;
    int32 duration = 2;
    int32 amplifier = 3;
}

message EffectUpdateBroadcast {
    string playerId = 1;
    repeated ActiveEffect effects = 2;
}
```

#### Visual Messages

```protobuf
message ParticleEffect {
    int32 particleType = 1;
    Vector3 position = 2;
    Vector3 velocity = 3;
    int32 count = 4;
}

message SoundEffect {
    int32 soundId = 1;
    Vector3 position = 2;
    float volume = 3;
    float pitch = 4;
}
```

#### Chat Messages

```protobuf
message ChatMessage {
    string message = 1;
    ChatStyle style = 2;
    string sender = 3;
}

message ChatStyle {
    string color = 1;
    bool bold = 2;
    bool italic = 3;
    bool underlined = 4;
}
```

#### World Messages

```protobuf
message WorldInfo {
    string worldName = 1;
    int32 seed = 2;
    Dimension dimension = 3;
    Difficulty difficulty = 4;
    GameMode gameMode = 5;
}

message WeatherInfo {
    Weather weather = 1;
    int32 duration = 2;
}

message WorldBorder {
    Vector2 center = 1;
    double size = 2;
    double warningDistance = 3;
    int32 warningTime = 4;
}

message TimeUpdateBroadcast {
    int64 worldTime = 1;
    int32 dayTime = 2;
}

message WeatherUpdateBroadcast {
    Weather weather = 1;
    int32 duration = 2;
}
```

## Generated Code

### [`Assets/Generated/Protobuf/Common.cs`](../Assets/Generated/Protobuf/Common.cs)

Auto-generated from [`proto/common.proto`](../proto/common.proto).

### [`Assets/Generated/Protobuf/GameCore.cs`](../Assets/Generated/Protobuf/GameCore.cs)

Auto-generated from [`proto/game_core.proto`](../proto/game_core.proto).

### [`Assets/Generated/Protobuf/GameWorld.cs`](../Assets/Generated/Protobuf/GameWorld.cs)

Auto-generated from [`proto/game_world.proto`](../proto/game_world.proto).

### [`Assets/Generated/Protobuf/EnhancedMinecraftGame.cs`](../Assets/Generated/Protobuf/EnhancedMinecraftGame.cs)

Auto-generated from [`proto/enhanced_minecraft_game.proto`](../proto/enhanced_minecraft_game.proto).

## Shared Protocol

### [`SharedProtocol/Messages.cs`](../SharedProtocol/Messages.cs)

Defines all protocol messages using ProtoBuf attributes.

### [`SharedProtocol/WorldSyncMessages.cs`](../SharedProtocol/WorldSyncMessages.cs)

Defines world synchronization messages.

#### WorldSync Messages

```csharp
[ProtoContract]
public class WorldBlockChangeBatchBroadcast
{
    [ProtoMember(1)]
    public List<WorldBlockChangeData> Changes { get; set; }
}

[ProtoContract]
public class WorldBlockChangeData
{
    [ProtoMember(1)]
    public Vector3Int Position { get; set; }
    
    [ProtoMember(2)]
    public int BlockType { get; set; }
    
    [ProtoMember(3)]
    public string PlayerId { get; set; }
}

[ProtoContract]
public class PlayerPositionUpdate
{
    [ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoMember(2)]
    public Vector3 Position { get; set; }
    
    [ProtoMember(3)]
    public Vector3 Rotation { get; set; }
}

[ProtoContract]
public class ChunkDataMessage
{
    [ProtoMember(1)]
    public int ChunkX { get; set; }
    
    [ProtoMember(2)]
    public int ChunkZ { get; set; }
    
    [ProtoMember(3)]
    public byte[] BlockData { get; set; }
    
    [ProtoMember(4)]
    public byte[] BiomeData { get; set; }
}

[ProtoContract]
public class ChunkUnloadMessage
{
    [ProtoMember(1)]
    public int ChunkX { get; set; }
    
    [ProtoMember(2)]
    public int ChunkZ { get; set; }
}
```

## Protocol Registry

The ProtocolRegistry maps message types to their handlers.

### Registered Handlers

- **LoginRequest** → LoginHandler
- **MoveRequest** → MovementHandler
- **WorldBlockChangeRequest** → WorldBlockHandler
- **InventoryRequest** → InventoryHandler
- **CraftingRequest** → CraftingHandler
- **RecipeListRequest** → RecipeListHandler
- **RoomListRequest** → RoomListHandler
- **RoomEnterRequest** → RoomEnterHandler
- **RoomLeaveRequest** → RoomLeaveHandler
- **HealthActionRequest** → HealthHandler
- **RespawnRequest** → RespawnHandler
- **ChatRequest** → ChatHandler
- **PingRequest** → PingHandler
- **ServerStatusRequest** → ServerStatusHandler
- **AISpawnRequest** → AISpawnHandler
- **AIDebugInfoRequest** → AIDebugInfoHandler
- **PlayerAttackRequest** → PlayerAttackHandler
- **CommandRequest** → CommandHandler

### Optional Packets (Not Bound)

The following packets are optional and not currently bound in the ProtocolRegistry:

- MultiBlockChange
- InventoryUpdate
- ItemUse
- ItemDrop
- ItemPickup
- EntityUpdate
- EntityInteract
- ContainerOpen
- ContainerClose
- ContainerUpdate

## Message Type Mapping

### EnhancedMinecraftProtocol Mappings

| Internal Type | Proto Message |
|---------------|---------------|
| PlayerStateUpdate | PlayerInfo |
| PlayerActionRequest | PlayerActionRequest |
| PlayerActionResponse | PlayerActionResponse |
| ChunkDataRequest | ChunkLoadRequest |
| ChunkDataResponse | ChunkLoadResponse |
| ChunkUnloadNotification | ChunkUnloadNotification |
| ChunkUnloadAcknowledge | ChunkUnloadAck |
| BlockChangeNotification | BlockChangeBroadcast |
| EntitySpawn | EntitySpawnBroadcast |
| EntityDespawn | EntityDespawnBroadcast |
| TimeUpdate | TimeUpdateBroadcast |
| WeatherChange | WeatherUpdateBroadcast |
| SoundEffect | SoundEffect |
| ParticleEffect | ParticleEffect |

## Fingerprint Validation

The system validates proto file fingerprints to ensure compatibility:

- **Expected Fingerprint**: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`
- **Computed Fingerprint**: `4922CE79B7C3DB9E6F55FB02AD41358F9A682F502D05F9E6229783527F1FA1B4`

## Regenerating Protobuf Code

To regenerate the protobuf C# code:

```bash
protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto
```

## Warnings and Recommendations

### Protobuf Version Mismatch

- **Warning**: SharedProtocol requires protobuf-net >= 3.2.18 but found 3.2.26
- **Recommendation**: Update package references to use consistent version

### Optional Packet Bindings

- **Warning**: Optional EnhancedMinecraft packets not registered
- **Recommendation**: These are optional features; register bindings when needed

### Missing Handlers

- **Warning**: Some EnhancedMinecraft packets have no handlers
- **Recommendation**: Implement handlers as needed for feature development

## Future Improvements

1. **Consistent protobuf-net version**: Update all references to use the same version
2. **Complete handler implementation**: Implement handlers for all EnhancedMinecraft packets
3. **Optional packet registration**: Register optional packets when features are implemented
4. **Protocol versioning**: Add protocol version tracking for compatibility
5. **Compression**: Add message compression for large payloads

## References

- [Terrain Generation Documentation](./terrain-generation.md)
- [World Map Control Documentation](./world-map-control.md)
- [Session 104 Summary](./session-104-summary.md)

