# Protocol Documentation

## Overview

This document describes the Protobuf-based network protocol used in the Minecraft-style game server. The protocol implements dual protocol support with protobuf-net and Google.Protobuf, providing comprehensive validation and standardization.

## Architecture

### Core Components

- **ProtocolStandardization.cs**: Protocol validation and standardization
- **ProtocolRegistry.cs**: Message type to protobuf message bindings
- **EnhancedProtocolHandler.cs**: Central protobuf packet gateway
- **enhanced_minecraft.proto**: Protocol definition file

### Protocol Features

1. **Dual Protocol Support**: Supports both protobuf-net and Google.Protobuf
2. **Message Type Registry**: Centralized message type bindings
3. **Validation**: Comprehensive validation of protocol messages
4. **Compression**: Optional packet compression
5. **Statistics**: Packet statistics tracking

## Protocol Registry

### Message Type Bindings

The protocol registry provides bindings between message types and protobuf messages:

```csharp
public class ProtocolRegistry
{
    private static readonly Dictionary<MessageType, Type> MessageBindings = new()
    {
        { MessageType.LoginRequest, typeof(LoginRequest) },
        { MessageType.LoginResponse, typeof(LoginResponse) },
        { MessageType.PlayerJoin, typeof(PlayerJoin) },
        { MessageType.PlayerLeave, typeof(PlayerLeave) },
        { MessageType.ChatMessage, typeof(ChatMessage) },
        { MessageType.BlockChange, typeof(BlockChange) },
        { MessageType.ChunkData, typeof(ChunkData) },
        { MessageType.PlayerPosition, typeof(PlayerPosition) },
        { MessageType.PlayerLook, typeof(PlayerLook) },
        { MessageType.PlayerPositionAndLook, typeof(PlayerPositionAndLook) },
        { MessageType.KeepAlive, typeof(KeepAlive) },
        { MessageType.Disconnect, typeof(Disconnect) },
        { MessageType.WorldInitialize, typeof(WorldInitialize) },
        { MessageType.TimeUpdate, typeof(TimeUpdate) },
        { MessageType.EntitySpawn, typeof(EntitySpawn) },
        { MessageType.EntityDespawn, typeof(EntityDespawn) },
        { MessageType.EntityMove, typeof(EntityMove) },
        { MessageType.EntityLook, typeof(EntityLook) },
        { MessageType.EntityMoveAndLook, typeof(EntityMoveAndLook) },
        { MessageType.EntityAnimation, typeof(EntityAnimation) },
        { MessageType.EntityDamage, typeof(EntityDamage) },
        { MessageType.EntityDeath, typeof(EntityDeath) },
        { MessageType.InventoryUpdate, typeof(InventoryUpdate) },
        { MessageType.InventorySlotChange, typeof(InventorySlotChange) },
        { MessageType.HeldItemChange, typeof(HeldItemChange) },
        { MessageType.UseItem, typeof(UseItem) },
        { MessageType.BlockBreak, typeof(BlockBreak) },
        { MessageType.BlockPlace, typeof(BlockPlace) },
        { MessageType.HealthUpdate, typeof(HealthUpdate) },
        { MessageType.HungerUpdate, typeof(HungerUpdate) },
        { MessageType.ExperienceUpdate, typeof(ExperienceUpdate) },
        { MessageType.AchievementAward, typeof(AchievementAward) },
        { MessageType.StatisticsUpdate, typeof(StatisticsUpdate) },
        { MessageType.PlayerList, typeof(PlayerList) },
        { MessageType.PlayerListAdd, typeof(PlayerListAdd) },
        { MessageType.PlayerListRemove, typeof(PlayerListRemove) },
        { MessageType.ScoreboardObjective, typeof(ScoreboardObjective) },
        { MessageType.ScoreboardScore, typeof(ScoreboardScore) },
        { MessageType.ScoreboardDisplay, typeof(ScoreboardDisplay) },
        { MessageType.TeamCreate, typeof(TeamCreate) },
        { MessageType.TeamRemove, typeof(TeamRemove) },
        { MessageType.TeamUpdate, typeof(TeamUpdate) },
        { MessageType.TeamMemberAdd, typeof(TeamMemberAdd) },
        { MessageType.TeamMemberRemove, typeof(TeamMemberRemove) },
        { MessageType.PluginMessage, typeof(PluginMessage) },
        { MessageType.ResourcePackSend, typeof(ResourcePackSend) },
        { MessageType.ResourcePackStatus, typeof(ResourcePackStatus) },
        { MessageType.SetDifficulty, typeof(SetDifficulty) },
        { MessageType.SetDefaultSpawnPosition, typeof(SetDefaultSpawnPosition) },
        {MessageType.WorldBorder, typeof(WorldBorder) },
        {MessageType.WorldBorderCenter, typeof(WorldBorderCenter) },
        {MessageType.WorldBorderSize, typeof(WorldBorderSize) },
        {MessageType.WorldBorderLerpSize, typeof(WorldBorderLerpSize) },
        {MessageType.WorldBorderWarningBlocks, typeof(WorldBorderWarningBlocks) },
        {MessageType.WorldBorderWarningTime, typeof(WorldBorderWarningTime) },
        {MessageType.Title, typeof(Title) },
        {MessageType.TitleSubtitle, typeof(TitleSubtitle) },
        {MessageType.TitleActionBar, typeof(TitleActionBar) },
        {MessageType.TitleTimes, typeof(TitleTimes) },
        {MessageType.TitleClear, typeof(TitleClear) },
        {MessageType.TitleReset, typeof(TitleReset) },
        {MessageType.OpenSignEditor, typeof(OpenSignEditor) },
        {MessageType.UpdateSign, typeof(UpdateSign) },
        {MessageType.MapData, typeof(MapData) },
        {MessageType.MapItemData, typeof(MapItemData) },
        {MessageType.TradeList, typeof(TradeList) },
        {MessageType.SelectTrade, typeof(SelectTrade) },
        {MessageType.UpdateCommandBlock, typeof(UpdateCommandBlock) },
        {MessageType.UpdateCommandBlockMinecart, typeof(UpdateCommandBlockMinecart) },
        {MessageType.CreativeInventoryAction, typeof(CreativeInventoryAction) },
        {MessageType.UpdateStructureBlock, typeof(UpdateStructureBlock) },
        {MessageType.UpdateSign, typeof(UpdateSign) },
        {MessageType.Animation, typeof(Animation) },
        {MessageType.SteeredBoat, typeof(SteeredBoat) },
        {MessageType.TabComplete, typeof(TabComplete) },
        {MessageType.ClientStatus, typeof(ClientStatus) },
        {MessageType.ClientSettings, typeof(ClientSettings) },
        {MessageType.ClientCommand, typeof(ClientCommand) },
        {MessageType.ConfirmTransaction, typeof(ConfirmTransaction) },
        {MessageType.EnchantItem, typeof(EnchantItem) },
        {MessageType.ClickWindow, typeof(ClickWindow) },
        {MessageType.CloseWindow, typeof(CloseWindow) },
        {MessageType.CustomPayload, typeof(CustomPayload) },
        {MessageType.TeleportConfirm, typeof(TeleportConfirm) },
        {MessageType.QueryBlockNBT, typeof(QueryBlockNBT) },
        {MessageType.SetDifficulty, typeof(SetDifficulty) },
        {MessageType.LockDifficulty, typeof(LockDifficulty) },
        {MessageType.AdvancementTab, typeof(AdvancementTab) },
        {MessageType.SelectAdvancementTab, typeof(SelectAdvancementTab) },
        {MessageType.AdvancementInfo, typeof(AdvancementInfo) },
        {MessageType.AdvancementProgress, typeof(AdvancementProgress) }
    };
}
```

### Factory Methods

The registry provides factory methods for creating messages:

```csharp
public static IMessage CreateMessage(MessageType messageType)
{
    if (MessageBindings.TryGetValue(messageType, out var messageType))
    {
        return (IMessage)Activator.CreateInstance(messageType);
    }
    throw new ArgumentException($"Unknown message type: {messageType}");
}

public static Type GetMessageType(MessageType messageType)
{
    if (MessageBindings.TryGetValue(messageType, out var type))
    {
        return type;
    }
    throw new ArgumentException($"Unknown message type: {messageType}");
}
```

## Protocol Validation

### Validation Overview

The protocol validation system provides:

1. **Descriptor Validation**: Validates message descriptors
2. **Parser Validation**: Validates message parsers with round-trip testing
3. **Fingerprint Drift Detection**: Detects protocol drift over time
4. **Coverage Validation**: Validates descriptor coverage

### Descriptor Validation

Descriptor validation ensures all messages have valid descriptors:

```csharp
public static bool ValidateDescriptors()
{
    var issues = new List<string>();

    foreach (var binding in MessageBindings)
    {
        var messageType = binding.Key;
        var type = binding.Value;

        try
        {
            var message = (IMessage)Activator.CreateInstance(type);
            var descriptor = message.Descriptor;

            if (descriptor == null)
            {
                issues.Add($"Message {messageType} has null descriptor");
                continue;
            }

            // Validate fields
            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                if (field == null)
                {
                    issues.Add($"Message {messageType} has null field descriptor");
                }
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to validate descriptor for {messageType}: {ex.Message}");
        }
    }

    if (issues.Any())
    {
        Console.WriteLine("Descriptor validation issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  - {issue}");
        }
        return false;
    }

    Console.WriteLine($"Descriptor validation passed for {MessageBindings.Count} messages");
    return true;
}
```

### Parser Validation

Parser validation ensures all messages can be parsed and serialized correctly:

```csharp
public static bool ValidateParsers()
{
    var issues = new List<string>();

    foreach (var binding in MessageBindings)
    {
        var messageType = binding.Key;
        var type = binding.Value;

        try
        {
            var message = (IMessage)Activator.CreateInstance(type);

            // Serialize to bytes
            using var stream = new MemoryStream();
            message.WriteTo(stream);
            var bytes = stream.ToArray();

            // Parse back
            var parser = type.GetMethod("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser == null)
            {
                issues.Add($"Message {messageType} has no static Parser property");
                continue;
            }

            var parserInstance = parser.Invoke(null, null);
            var parseMethod = parserInstance.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
            if (parseMethod == null)
            {
                issues.Add($"Message {messageType} parser has no ParseFrom method");
                continue;
            }

            var parsedMessage = parseMethod.Invoke(parserInstance, new object[] { bytes });

            if (parsedMessage == null)
            {
                issues.Add($"Failed to parse {messageType} after serialization");
                continue;
            }

            // Validate round-trip
            if (!message.Equals(parsedMessage))
            {
                issues.Add($"Round-trip validation failed for {messageType}");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to validate parser for {messageType}: {ex.Message}");
        }
    }

    if (issues.Any())
    {
        Console.WriteLine("Parser validation issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  - {issue}");
        }
        return false;
    }

    Console.WriteLine($"Parser validation passed for {MessageBindings.Count} messages");
    return true;
}
```

### Fingerprint Drift Detection

Fingerprint drift detection detects protocol changes over time:

```csharp
public static string ComputeFingerprint()
{
    var fingerprintBuilder = new StringBuilder();

    foreach (var binding in MessageBindings.OrderBy(b => b.Key))
    {
        var messageType = binding.Key;
        var type = binding.Value;

        var message = (IMessage)Activator.CreateInstance(type);
        var descriptor = message.Descriptor;

        fingerprintBuilder.Append(messageType);
        fingerprintBuilder.Append(descriptor.FullName);

        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            fingerprintBuilder.Append(field.FieldNumber);
            fingerprintBuilder.Append(field.Name);
            fingerprintBuilder.Append(field.FieldType);
        }
    }

    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(fingerprintBuilder.ToString());
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}

public static bool DetectDrift(string storedFingerprint)
{
    var currentFingerprint = ComputeFingerprint();
    return currentFingerprint != storedFingerprint;
}
```

## Protocol Handler

### Handler Overview

The protocol handler provides:

1. **Packet Processing**: Central packet processing
2. **Size Limits**: Enforces packet size limits
3. **Compression**: Optional packet compression
4. **Statistics**: Packet statistics tracking

### Handler Implementation

```csharp
public class EnhancedProtocolHandler
{
    private readonly int _maxPacketSize;
    private readonly int _compressionThreshold;
    private readonly bool _enableCompression;
    private readonly PacketStatistics _statistics;

    public EnhancedProtocolHandler(int maxPacketSize = 2 * 1024 * 1024,
                                   int compressionThreshold = 256,
                                   bool enableCompression = true)
    {
        _maxPacketSize = maxPacketSize;
        _compressionThreshold = compressionThreshold;
        _enableCompression = enableCompression;
        _statistics = new PacketStatistics();
    }

    public byte[] SerializeMessage(IMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        var bytes = stream.ToArray();

        // Compress if enabled and size exceeds threshold
        if (_enableCompression && bytes.Length > _compressionThreshold)
        {
            bytes = Compress(bytes);
            _statistics.CompressedPackets++;
        }

        // Validate size
        if (bytes.Length > _maxPacketSize)
        {
            throw new InvalidOperationException($"Packet size {bytes.Length} exceeds maximum {_maxPacketSize}");
        }

        _statistics.SentPackets++;
        _statistics.SentBytes += bytes.Length;

        return bytes;
    }

    public IMessage DeserializeMessage(byte[] data, Type messageType)
    {
        // Validate size
        if (data.Length > _maxPacketSize)
        {
            throw new InvalidOperationException($"Packet size {data.Length} exceeds maximum {_maxPacketSize}");
        }

        // Decompress if needed
        if (IsCompressed(data))
        {
            data = Decompress(data);
            _statistics.CompressedPackets++;
        }

        // Parse message
        var parser = messageType.GetMethod("Parser", BindingFlags.Static | BindingFlags.Public);
        var parserInstance = parser.Invoke(null, null);
        var parseMethod = parserInstance.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
        var message = parseMethod.Invoke(parserInstance, new object[] { data });

        _statistics.ReceivedPackets++;
        _statistics.ReceivedBytes += data.Length;

        return (IMessage)message;
    }
}
```

### Packet Statistics

The protocol handler tracks packet statistics:

```csharp
public class PacketStatistics
{
    public long SentPackets { get; set; }
    public long ReceivedPackets { get; set; }
    public long SentBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public long CompressedPackets { get; set; }
    public long FailedPackets { get; set; }

    public void Reset()
    {
        SentPackets = 0;
        ReceivedPackets = 0;
        SentBytes = 0;
        ReceivedBytes = 0;
        CompressedPackets = 0;
        FailedPackets = 0;
    }

    public void PrintStatistics()
    {
        Console.WriteLine("Packet Statistics:");
        Console.WriteLine($"  Sent: {SentPackets} packets ({SentBytes} bytes)");
        Console.WriteLine($"  Received: {ReceivedPackets} packets ({ReceivedBytes} bytes)");
        Console.WriteLine($"  Compressed: {CompressedPackets} packets");
        Console.WriteLine($"  Failed: {FailedPackets} packets");
    }
}
```

## Protocol Definition

### Proto File

The protocol is defined in `proto/enhanced_minecraft.proto`:

```protobuf
syntax = "proto3";

package EnhancedMinecraftProtocol;

// Message types
enum MessageType {
  LOGIN_REQUEST = 0;
  LOGIN_RESPONSE = 1;
  PLAYER_JOIN = 2;
  PLAYER_LEAVE = 3;
  CHAT_MESSAGE = 4;
  BLOCK_CHANGE = 5;
  CHUNK_DATA = 6;
  PLAYER_POSITION = 7;
  PLAYER_LOOK = 8;
  PLAYER_POSITION_AND_LOOK = 9;
  KEEP_ALIVE = 10;
  DISCONNECT = 11;
  WORLD_INITIALIZE = 12;
  TIME_UPDATE = 13;
  // ... more message types
}

// Login messages
message LoginRequest {
  string username = 1;
  string password = 2;
  string version = 3;
}

message LoginResponse {
  bool success = 1;
  string message = 2;
  string session_token = 3;
}

// Player messages
message PlayerJoin {
  string player_id = 1;
  string username = 2;
  Vector3 position = 3;
}

message PlayerLeave {
  string player_id = 1;
  string reason = 2;
}

// Chat messages
message ChatMessage {
  string sender = 1;
  string content = 2;
  ChatMessageType type = 3;
}

enum ChatMessageType {
  CHAT = 0;
  SYSTEM = 1;
  ACTION = 2;
}

// Block messages
message BlockChange {
  int32 x = 1;
  int32 y = 2;
  int32 z = 3;
  int32 block_id = 4;
  int32 metadata = 5;
}

// Chunk messages
message ChunkData {
  int32 chunk_x = 1;
  int32 chunk_z = 2;
  bytes block_data = 3;
  bytes biome_data = 4;
}

// Player movement messages
message PlayerPosition {
  double x = 1;
  double y = 2;
  double z = 3;
  bool on_ground = 4;
}

message PlayerLook {
  float yaw = 1;
  float pitch = 2;
  bool on_ground = 3;
}

message PlayerPositionAndLook {
  double x = 1;
  double y = 2;
  double z = 3;
  float yaw = 4;
  float pitch = 5;
  bool on_ground = 6;
}

// Common types
message Vector3 {
  double x = 1;
  double y = 2;
  double z = 3;
}
```

## Configuration

### Server Configuration

Server-side protocol configuration in `config/server.json`:

```json
{
  "Network": {
    "Host": "0.0.0.0",
    "Port": 25565,
    "MaxPlayers": 20,
    "MaxConnectionsPerIP": 3,
    "ConnectionTimeoutSeconds": 30,
    "KeepAliveIntervalSeconds": 5,
    "PacketCompressionThreshold": 256
  },
  "Security": {
    "EnableWhitelist": false,
    "EnableAuthentication": true,
    "EnableEncryption": true,
    "MaxPacketSize": 2097152,
    "RateLimitPacketsPerSecond": 100,
    "EnableAntiCheat": true,
    "MaxPlayerSpeed": 10.0,
    "MaxFlySpeed": 20.0
  }
}
```

### Client Configuration

Client-side protocol configuration in `config/client_config.json`:

```json
{
  "client": {
    "network": {
      "connectionTimeoutMs": 10000,
      "reconnectAttempts": 3,
      "reconnectDelayMs": 5000,
      "maxPacketSize": 1048576,
      "compressionEnabled": true,
      "compressionThreshold": 1024
    }
  }
}
```

## Implementation Notes

### Performance Considerations

- **Compression**: Reduces bandwidth but increases CPU usage
- **Caching**: Cache parsed messages for performance
- **Pooling**: Use object pooling for message instances
- **Async Processing**: Process packets asynchronously

### Security Considerations

- **Size Limits**: Enforce packet size limits to prevent DoS
- **Rate Limiting**: Rate limit packets per connection
- **Encryption**: Enable encryption for secure communication
- **Authentication**: Require authentication for sensitive operations

### Extensibility

The protocol system is designed for extensibility:

- **Message Registry**: Easy to add new message types
- **Validation**: Comprehensive validation system
- **Dual Protocol**: Supports multiple protobuf implementations
- **Statistics**: Detailed statistics for monitoring

## References

- [`ProtocolStandardization.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs)
- [`ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)
- [`EnhancedProtocolHandler.cs`](../GameServer/Network/EnhancedProtocolHandler.cs)
- [`proto/enhanced_minecraft.proto`](../proto/enhanced_minecraft.proto)
- [`config/server.json`](../config/server.json)
- [`config/client_config.json`](../config/client_config.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

## Overview

This document describes the Protobuf-based network protocol used in the Minecraft-style game server. The protocol implements dual protocol support with protobuf-net and Google.Protobuf, providing comprehensive validation and standardization.

## Architecture

### Core Components

- **ProtocolStandardization.cs**: Protocol validation and standardization
- **ProtocolRegistry.cs**: Message type to protobuf message bindings
- **EnhancedProtocolHandler.cs**: Central protobuf packet gateway
- **enhanced_minecraft.proto**: Protocol definition file

### Protocol Features

1. **Dual Protocol Support**: Supports both protobuf-net and Google.Protobuf
2. **Message Type Registry**: Centralized message type bindings
3. **Validation**: Comprehensive validation of protocol messages
4. **Compression**: Optional packet compression
5. **Statistics**: Packet statistics tracking

## Protocol Registry

### Message Type Bindings

The protocol registry provides bindings between message types and protobuf messages:

```csharp
public class ProtocolRegistry
{
    private static readonly Dictionary<MessageType, Type> MessageBindings = new()
    {
        { MessageType.LoginRequest, typeof(LoginRequest) },
        { MessageType.LoginResponse, typeof(LoginResponse) },
        { MessageType.PlayerJoin, typeof(PlayerJoin) },
        { MessageType.PlayerLeave, typeof(PlayerLeave) },
        { MessageType.ChatMessage, typeof(ChatMessage) },
        { MessageType.BlockChange, typeof(BlockChange) },
        { MessageType.ChunkData, typeof(ChunkData) },
        { MessageType.PlayerPosition, typeof(PlayerPosition) },
        { MessageType.PlayerLook, typeof(PlayerLook) },
        { MessageType.PlayerPositionAndLook, typeof(PlayerPositionAndLook) },
        { MessageType.KeepAlive, typeof(KeepAlive) },
        { MessageType.Disconnect, typeof(Disconnect) },
        { MessageType.WorldInitialize, typeof(WorldInitialize) },
        { MessageType.TimeUpdate, typeof(TimeUpdate) },
        { MessageType.EntitySpawn, typeof(EntitySpawn) },
        { MessageType.EntityDespawn, typeof(EntityDespawn) },
        { MessageType.EntityMove, typeof(EntityMove) },
        { MessageType.EntityLook, typeof(EntityLook) },
        { MessageType.EntityMoveAndLook, typeof(EntityMoveAndLook) },
        { MessageType.EntityAnimation, typeof(EntityAnimation) },
        { MessageType.EntityDamage, typeof(EntityDamage) },
        { MessageType.EntityDeath, typeof(EntityDeath) },
        { MessageType.InventoryUpdate, typeof(InventoryUpdate) },
        { MessageType.InventorySlotChange, typeof(InventorySlotChange) },
        { MessageType.HeldItemChange, typeof(HeldItemChange) },
        { MessageType.UseItem, typeof(UseItem) },
        { MessageType.BlockBreak, typeof(BlockBreak) },
        { MessageType.BlockPlace, typeof(BlockPlace) },
        { MessageType.HealthUpdate, typeof(HealthUpdate) },
        { MessageType.HungerUpdate, typeof(HungerUpdate) },
        { MessageType.ExperienceUpdate, typeof(ExperienceUpdate) },
        { MessageType.AchievementAward, typeof(AchievementAward) },
        { MessageType.StatisticsUpdate, typeof(StatisticsUpdate) },
        { MessageType.PlayerList, typeof(PlayerList) },
        { MessageType.PlayerListAdd, typeof(PlayerListAdd) },
        { MessageType.PlayerListRemove, typeof(PlayerListRemove) },
        { MessageType.ScoreboardObjective, typeof(ScoreboardObjective) },
        { MessageType.ScoreboardScore, typeof(ScoreboardScore) },
        { MessageType.ScoreboardDisplay, typeof(ScoreboardDisplay) },
        { MessageType.TeamCreate, typeof(TeamCreate) },
        { MessageType.TeamRemove, typeof(TeamRemove) },
        { MessageType.TeamUpdate, typeof(TeamUpdate) },
        { MessageType.TeamMemberAdd, typeof(TeamMemberAdd) },
        { MessageType.TeamMemberRemove, typeof(TeamMemberRemove) },
        { MessageType.PluginMessage, typeof(PluginMessage) },
        { MessageType.ResourcePackSend, typeof(ResourcePackSend) },
        { MessageType.ResourcePackStatus, typeof(ResourcePackStatus) },
        { MessageType.SetDifficulty, typeof(SetDifficulty) },
        { MessageType.SetDefaultSpawnPosition, typeof(SetDefaultSpawnPosition) },
        {MessageType.WorldBorder, typeof(WorldBorder) },
        {MessageType.WorldBorderCenter, typeof(WorldBorderCenter) },
        {MessageType.WorldBorderSize, typeof(WorldBorderSize) },
        {MessageType.WorldBorderLerpSize, typeof(WorldBorderLerpSize) },
        {MessageType.WorldBorderWarningBlocks, typeof(WorldBorderWarningBlocks) },
        {MessageType.WorldBorderWarningTime, typeof(WorldBorderWarningTime) },
        {MessageType.Title, typeof(Title) },
        {MessageType.TitleSubtitle, typeof(TitleSubtitle) },
        {MessageType.TitleActionBar, typeof(TitleActionBar) },
        {MessageType.TitleTimes, typeof(TitleTimes) },
        {MessageType.TitleClear, typeof(TitleClear) },
        {MessageType.TitleReset, typeof(TitleReset) },
        {MessageType.OpenSignEditor, typeof(OpenSignEditor) },
        {MessageType.UpdateSign, typeof(UpdateSign) },
        {MessageType.MapData, typeof(MapData) },
        {MessageType.MapItemData, typeof(MapItemData) },
        {MessageType.TradeList, typeof(TradeList) },
        {MessageType.SelectTrade, typeof(SelectTrade) },
        {MessageType.UpdateCommandBlock, typeof(UpdateCommandBlock) },
        {MessageType.UpdateCommandBlockMinecart, typeof(UpdateCommandBlockMinecart) },
        {MessageType.CreativeInventoryAction, typeof(CreativeInventoryAction) },
        {MessageType.UpdateStructureBlock, typeof(UpdateStructureBlock) },
        {MessageType.UpdateSign, typeof(UpdateSign) },
        {MessageType.Animation, typeof(Animation) },
        {MessageType.SteeredBoat, typeof(SteeredBoat) },
        {MessageType.TabComplete, typeof(TabComplete) },
        {MessageType.ClientStatus, typeof(ClientStatus) },
        {MessageType.ClientSettings, typeof(ClientSettings) },
        {MessageType.ClientCommand, typeof(ClientCommand) },
        {MessageType.ConfirmTransaction, typeof(ConfirmTransaction) },
        {MessageType.EnchantItem, typeof(EnchantItem) },
        {MessageType.ClickWindow, typeof(ClickWindow) },
        {MessageType.CloseWindow, typeof(CloseWindow) },
        {MessageType.CustomPayload, typeof(CustomPayload) },
        {MessageType.TeleportConfirm, typeof(TeleportConfirm) },
        {MessageType.QueryBlockNBT, typeof(QueryBlockNBT) },
        {MessageType.SetDifficulty, typeof(SetDifficulty) },
        {MessageType.LockDifficulty, typeof(LockDifficulty) },
        {MessageType.AdvancementTab, typeof(AdvancementTab) },
        {MessageType.SelectAdvancementTab, typeof(SelectAdvancementTab) },
        {MessageType.AdvancementInfo, typeof(AdvancementInfo) },
        {MessageType.AdvancementProgress, typeof(AdvancementProgress) }
    };
}
```

### Factory Methods

The registry provides factory methods for creating messages:

```csharp
public static IMessage CreateMessage(MessageType messageType)
{
    if (MessageBindings.TryGetValue(messageType, out var messageType))
    {
        return (IMessage)Activator.CreateInstance(messageType);
    }
    throw new ArgumentException($"Unknown message type: {messageType}");
}

public static Type GetMessageType(MessageType messageType)
{
    if (MessageBindings.TryGetValue(messageType, out var type))
    {
        return type;
    }
    throw new ArgumentException($"Unknown message type: {messageType}");
}
```

## Protocol Validation

### Validation Overview

The protocol validation system provides:

1. **Descriptor Validation**: Validates message descriptors
2. **Parser Validation**: Validates message parsers with round-trip testing
3. **Fingerprint Drift Detection**: Detects protocol drift over time
4. **Coverage Validation**: Validates descriptor coverage

### Descriptor Validation

Descriptor validation ensures all messages have valid descriptors:

```csharp
public static bool ValidateDescriptors()
{
    var issues = new List<string>();

    foreach (var binding in MessageBindings)
    {
        var messageType = binding.Key;
        var type = binding.Value;

        try
        {
            var message = (IMessage)Activator.CreateInstance(type);
            var descriptor = message.Descriptor;

            if (descriptor == null)
            {
                issues.Add($"Message {messageType} has null descriptor");
                continue;
            }

            // Validate fields
            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                if (field == null)
                {
                    issues.Add($"Message {messageType} has null field descriptor");
                }
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to validate descriptor for {messageType}: {ex.Message}");
        }
    }

    if (issues.Any())
    {
        Console.WriteLine("Descriptor validation issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  - {issue}");
        }
        return false;
    }

    Console.WriteLine($"Descriptor validation passed for {MessageBindings.Count} messages");
    return true;
}
```

### Parser Validation

Parser validation ensures all messages can be parsed and serialized correctly:

```csharp
public static bool ValidateParsers()
{
    var issues = new List<string>();

    foreach (var binding in MessageBindings)
    {
        var messageType = binding.Key;
        var type = binding.Value;

        try
        {
            var message = (IMessage)Activator.CreateInstance(type);

            // Serialize to bytes
            using var stream = new MemoryStream();
            message.WriteTo(stream);
            var bytes = stream.ToArray();

            // Parse back
            var parser = type.GetMethod("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser == null)
            {
                issues.Add($"Message {messageType} has no static Parser property");
                continue;
            }

            var parserInstance = parser.Invoke(null, null);
            var parseMethod = parserInstance.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
            if (parseMethod == null)
            {
                issues.Add($"Message {messageType} parser has no ParseFrom method");
                continue;
            }

            var parsedMessage = parseMethod.Invoke(parserInstance, new object[] { bytes });

            if (parsedMessage == null)
            {
                issues.Add($"Failed to parse {messageType} after serialization");
                continue;
            }

            // Validate round-trip
            if (!message.Equals(parsedMessage))
            {
                issues.Add($"Round-trip validation failed for {messageType}");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Failed to validate parser for {messageType}: {ex.Message}");
        }
    }

    if (issues.Any())
    {
        Console.WriteLine("Parser validation issues:");
        foreach (var issue in issues)
        {
            Console.WriteLine($"  - {issue}");
        }
        return false;
    }

    Console.WriteLine($"Parser validation passed for {MessageBindings.Count} messages");
    return true;
}
```

### Fingerprint Drift Detection

Fingerprint drift detection detects protocol changes over time:

```csharp
public static string ComputeFingerprint()
{
    var fingerprintBuilder = new StringBuilder();

    foreach (var binding in MessageBindings.OrderBy(b => b.Key))
    {
        var messageType = binding.Key;
        var type = binding.Value;

        var message = (IMessage)Activator.CreateInstance(type);
        var descriptor = message.Descriptor;

        fingerprintBuilder.Append(messageType);
        fingerprintBuilder.Append(descriptor.FullName);

        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            fingerprintBuilder.Append(field.FieldNumber);
            fingerprintBuilder.Append(field.Name);
            fingerprintBuilder.Append(field.FieldType);
        }
    }

    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(fingerprintBuilder.ToString());
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}

public static bool DetectDrift(string storedFingerprint)
{
    var currentFingerprint = ComputeFingerprint();
    return currentFingerprint != storedFingerprint;
}
```

## Protocol Handler

### Handler Overview

The protocol handler provides:

1. **Packet Processing**: Central packet processing
2. **Size Limits**: Enforces packet size limits
3. **Compression**: Optional packet compression
4. **Statistics**: Packet statistics tracking

### Handler Implementation

```csharp
public class EnhancedProtocolHandler
{
    private readonly int _maxPacketSize;
    private readonly int _compressionThreshold;
    private readonly bool _enableCompression;
    private readonly PacketStatistics _statistics;

    public EnhancedProtocolHandler(int maxPacketSize = 2 * 1024 * 1024,
                                   int compressionThreshold = 256,
                                   bool enableCompression = true)
    {
        _maxPacketSize = maxPacketSize;
        _compressionThreshold = compressionThreshold;
        _enableCompression = enableCompression;
        _statistics = new PacketStatistics();
    }

    public byte[] SerializeMessage(IMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        var bytes = stream.ToArray();

        // Compress if enabled and size exceeds threshold
        if (_enableCompression && bytes.Length > _compressionThreshold)
        {
            bytes = Compress(bytes);
            _statistics.CompressedPackets++;
        }

        // Validate size
        if (bytes.Length > _maxPacketSize)
        {
            throw new InvalidOperationException($"Packet size {bytes.Length} exceeds maximum {_maxPacketSize}");
        }

        _statistics.SentPackets++;
        _statistics.SentBytes += bytes.Length;

        return bytes;
    }

    public IMessage DeserializeMessage(byte[] data, Type messageType)
    {
        // Validate size
        if (data.Length > _maxPacketSize)
        {
            throw new InvalidOperationException($"Packet size {data.Length} exceeds maximum {_maxPacketSize}");
        }

        // Decompress if needed
        if (IsCompressed(data))
        {
            data = Decompress(data);
            _statistics.CompressedPackets++;
        }

        // Parse message
        var parser = messageType.GetMethod("Parser", BindingFlags.Static | BindingFlags.Public);
        var parserInstance = parser.Invoke(null, null);
        var parseMethod = parserInstance.GetType().GetMethod("ParseFrom", new[] { typeof(byte[]) });
        var message = parseMethod.Invoke(parserInstance, new object[] { data });

        _statistics.ReceivedPackets++;
        _statistics.ReceivedBytes += data.Length;

        return (IMessage)message;
    }
}
```

### Packet Statistics

The protocol handler tracks packet statistics:

```csharp
public class PacketStatistics
{
    public long SentPackets { get; set; }
    public long ReceivedPackets { get; set; }
    public long SentBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public long CompressedPackets { get; set; }
    public long FailedPackets { get; set; }

    public void Reset()
    {
        SentPackets = 0;
        ReceivedPackets = 0;
        SentBytes = 0;
        ReceivedBytes = 0;
        CompressedPackets = 0;
        FailedPackets = 0;
    }

    public void PrintStatistics()
    {
        Console.WriteLine("Packet Statistics:");
        Console.WriteLine($"  Sent: {SentPackets} packets ({SentBytes} bytes)");
        Console.WriteLine($"  Received: {ReceivedPackets} packets ({ReceivedBytes} bytes)");
        Console.WriteLine($"  Compressed: {CompressedPackets} packets");
        Console.WriteLine($"  Failed: {FailedPackets} packets");
    }
}
```

## Protocol Definition

### Proto File

The protocol is defined in `proto/enhanced_minecraft.proto`:

```protobuf
syntax = "proto3";

package EnhancedMinecraftProtocol;

// Message types
enum MessageType {
  LOGIN_REQUEST = 0;
  LOGIN_RESPONSE = 1;
  PLAYER_JOIN = 2;
  PLAYER_LEAVE = 3;
  CHAT_MESSAGE = 4;
  BLOCK_CHANGE = 5;
  CHUNK_DATA = 6;
  PLAYER_POSITION = 7;
  PLAYER_LOOK = 8;
  PLAYER_POSITION_AND_LOOK = 9;
  KEEP_ALIVE = 10;
  DISCONNECT = 11;
  WORLD_INITIALIZE = 12;
  TIME_UPDATE = 13;
  // ... more message types
}

// Login messages
message LoginRequest {
  string username = 1;
  string password = 2;
  string version = 3;
}

message LoginResponse {
  bool success = 1;
  string message = 2;
  string session_token = 3;
}

// Player messages
message PlayerJoin {
  string player_id = 1;
  string username = 2;
  Vector3 position = 3;
}

message PlayerLeave {
  string player_id = 1;
  string reason = 2;
}

// Chat messages
message ChatMessage {
  string sender = 1;
  string content = 2;
  ChatMessageType type = 3;
}

enum ChatMessageType {
  CHAT = 0;
  SYSTEM = 1;
  ACTION = 2;
}

// Block messages
message BlockChange {
  int32 x = 1;
  int32 y = 2;
  int32 z = 3;
  int32 block_id = 4;
  int32 metadata = 5;
}

// Chunk messages
message ChunkData {
  int32 chunk_x = 1;
  int32 chunk_z = 2;
  bytes block_data = 3;
  bytes biome_data = 4;
}

// Player movement messages
message PlayerPosition {
  double x = 1;
  double y = 2;
  double z = 3;
  bool on_ground = 4;
}

message PlayerLook {
  float yaw = 1;
  float pitch = 2;
  bool on_ground = 3;
}

message PlayerPositionAndLook {
  double x = 1;
  double y = 2;
  double z = 3;
  float yaw = 4;
  float pitch = 5;
  bool on_ground = 6;
}

// Common types
message Vector3 {
  double x = 1;
  double y = 2;
  double z = 3;
}
```

## Configuration

### Server Configuration

Server-side protocol configuration in `config/server.json`:

```json
{
  "Network": {
    "Host": "0.0.0.0",
    "Port": 25565,
    "MaxPlayers": 20,
    "MaxConnectionsPerIP": 3,
    "ConnectionTimeoutSeconds": 30,
    "KeepAliveIntervalSeconds": 5,
    "PacketCompressionThreshold": 256
  },
  "Security": {
    "EnableWhitelist": false,
    "EnableAuthentication": true,
    "EnableEncryption": true,
    "MaxPacketSize": 2097152,
    "RateLimitPacketsPerSecond": 100,
    "EnableAntiCheat": true,
    "MaxPlayerSpeed": 10.0,
    "MaxFlySpeed": 20.0
  }
}
```

### Client Configuration

Client-side protocol configuration in `config/client_config.json`:

```json
{
  "client": {
    "network": {
      "connectionTimeoutMs": 10000,
      "reconnectAttempts": 3,
      "reconnectDelayMs": 5000,
      "maxPacketSize": 1048576,
      "compressionEnabled": true,
      "compressionThreshold": 1024
    }
  }
}
```

## Implementation Notes

### Performance Considerations

- **Compression**: Reduces bandwidth but increases CPU usage
- **Caching**: Cache parsed messages for performance
- **Pooling**: Use object pooling for message instances
- **Async Processing**: Process packets asynchronously

### Security Considerations

- **Size Limits**: Enforce packet size limits to prevent DoS
- **Rate Limiting**: Rate limit packets per connection
- **Encryption**: Enable encryption for secure communication
- **Authentication**: Require authentication for sensitive operations

### Extensibility

The protocol system is designed for extensibility:

- **Message Registry**: Easy to add new message types
- **Validation**: Comprehensive validation system
- **Dual Protocol**: Supports multiple protobuf implementations
- **Statistics**: Detailed statistics for monitoring

## References

- [`ProtocolStandardization.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolStandardization.cs)
- [`ProtocolRegistry.cs`](../SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs)
- [`EnhancedProtocolHandler.cs`](../GameServer/Network/EnhancedProtocolHandler.cs)
- [`proto/enhanced_minecraft.proto`](../proto/enhanced_minecraft.proto)
- [`config/server.json`](../config/server.json)
- [`config/client_config.json`](../config/client_config.json)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-13 | Initial documentation |

