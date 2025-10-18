using System.Net.Sockets;
using ProtoBuf;

namespace SharedProtocol;

public readonly struct IncomingMessage
{
    public IncomingMessage(int rawType, MessageType? messageType, object payload)
    {
        RawType = rawType;
        MessageType = messageType;
        Payload = payload;
    }

    /// <summary>
    /// Raw integer message identifier as received on the wire.
    /// </summary>
    public int RawType { get; }

    /// <summary>
    /// Strongly typed message identifier when it matches <see cref="MessageType"/>; otherwise <c>null</c>.
    /// </summary>
    public MessageType? MessageType { get; }

    /// <summary>
    /// Deserialized payload (typed object for known messages or <see cref="byte[]"/> for raw payloads).
    /// </summary>
    public object Payload { get; }

    public bool IsKnown => MessageType.HasValue;

    /// <summary>
    /// Convenience accessor that returns the known type, or casts the raw identifier into <see cref="MessageType"/> when undefined.
    /// </summary>
    public MessageType Type => MessageType ?? (MessageType)RawType;

    public void Deconstruct(out MessageType type, out object payload)
    {
        type = Type;
        payload = Payload;
    }

    public void Deconstruct(out MessageType type, out object payload, out int rawType)
    {
        type = Type;
        payload = Payload;
        rawType = RawType;
    }
}

public class Session
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    /// <summary>
    /// Gets or sets the user name associated with this session after a successful login.
    /// The property is optional and may remain <c>null</c> until authentication completes.
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// ?¸ì…˜ ? í° - ë¡œê·¸???±ê³µ ???ì„±?©ë‹ˆ??
    /// </summary>
    public string? SessionToken { get; set; }
    
    /// <summary>
    /// ?¸ì…˜ ?ì„± ?œê°„
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// ë§ˆì?ë§??œë™ ?œê°„
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ?Œë ˆ?´ì–´ ?•ë³´
    /// </summary>
    public PlayerInfo? PlayerInfo { get; set; }

    public Session(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    /// <summary>
    /// ë©”ì‹œì§€ë¥?ë¹„ë™ê¸°ì ?¼ë¡œ ì§ë ¬?”í•˜???„ì†¡?©ë‹ˆ??
    /// </summary>
    public async Task SendAsync<T>(MessageType type, T message)
    {
        try
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, message);
            var body = ms.ToArray();
            
            // ë©”ì‹œì§€ ?¬ê¸° ì²´í¬
            if (body.Length > 1024 * 1024) // 1MB ?œí•œ
                throw new InvalidDataException($"Message too large: {body.Length} bytes");
            
            var length = BitConverter.GetBytes(body.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes((int)type);
            
            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();
            
            // ë§ˆì?ë§??œë™ ?œê°„ ?…ë°?´íŠ¸
            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message of type {type}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ?ì‹œ ë°”ì´???˜ì´ë¡œë“œë¥?ì§€?•í•œ ?•ìˆ˜??ë©”ì‹œì§€ ?€?…ê³¼ ?¨ê»˜ ?„ì†¡?©ë‹ˆ??
    /// ë§ˆì¸?¬ë˜?„íŠ¸ ?•ì¥ ë©”ì‹œì§€(MinecraftMessageType) ??enum ???€??ì½”ë“œë¥?ì§€?í•©?ˆë‹¤.
    /// </summary>
    public async Task SendAsync(int rawMessageType, byte[] payload)
    {
        try
        {
            payload ??= Array.Empty<byte>();

            if (payload.Length > 1024 * 1024) // 1MB ?œí•œ
                throw new InvalidDataException($"Message too large: {payload.Length} bytes");

            var length = BitConverter.GetBytes(payload.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes(rawMessageType);

            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            if (payload.Length > 0)
            {
                await _stream.WriteAsync(payload, 0, payload.Length);
            }
            await _stream.FlushAsync();

            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send raw message type {rawMessageType}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ?œë²„ë¡œë???ë©”ì‹œì§€ë¥?ë¹„ë™ê¸°ì ?¼ë¡œ ?˜ì‹ ?˜ê³  ??§?¬í™”?©ë‹ˆ??
    /// </summary>
    public async Task<IncomingMessage> ReceiveAsync()
    {
        try
        {
            var lenBuf = await ReadExactAsync(sizeof(int));
            var length = BitConverter.ToInt32(lenBuf, 0);
            
            // ?˜ëª»??ê¸¸ì´ ì²´í¬
            if (length <= sizeof(int) || length > 1024 * 1024) // 1MB ?œí•œ
                throw new InvalidDataException($"Invalid message length: {length}");
            
            var typeBuf = await ReadExactAsync(sizeof(int));
            var rawType = BitConverter.ToInt32(typeBuf, 0);
            var body = await ReadExactAsync(length - sizeof(int));
            
            MessageType? knownType = Enum.IsDefined(typeof(MessageType), rawType) ? (MessageType)rawType : null;
            object message;

            if (knownType.HasValue)
            {
                using var ms = new MemoryStream(body);
                message = knownType.Value switch
                {
                    // ?¸ì¦ ê´€??
                    MessageType.LoginRequest => Serializer.Deserialize<LoginRequest>(ms),
                    MessageType.LoginResponse => Serializer.Deserialize<LoginResponse>(ms),
                    MessageType.LogoutRequest => Serializer.Deserialize<LogoutRequest>(ms),
                    MessageType.LogoutResponse => Serializer.Deserialize<LogoutResponse>(ms),

                    // ?´ë™ ê´€??
                    MessageType.MoveRequest => Serializer.Deserialize<MoveRequest>(ms),
                    MessageType.MoveResponse => Serializer.Deserialize<MoveResponse>(ms),

                    // ?”ë“œ/ë¸”ë¡ ê´€??
                    MessageType.WorldBlockChangeRequest => Serializer.Deserialize<WorldBlockChangeRequest>(ms),
                    MessageType.WorldBlockChangeResponse => Serializer.Deserialize<WorldBlockChangeResponse>(ms),
                    MessageType.WorldBlockChangeBroadcast => Serializer.Deserialize<WorldBlockChangeBroadcast>(ms),

                    // ì±„íŒ… ê´€??
                    MessageType.ChatRequest => Serializer.Deserialize<ChatRequest>(ms),
                    MessageType.ChatResponse => Serializer.Deserialize<ChatResponse>(ms),
                    MessageType.ChatMessage => Serializer.Deserialize<ChatMessage>(ms),

                    // ?œë²„ ?íƒœ/ì§„ë‹¨
                    MessageType.PingRequest => Serializer.Deserialize<PingRequest>(ms),
                    MessageType.PingResponse => Serializer.Deserialize<PingResponse>(ms),
                    MessageType.ServerStatusRequest => Serializer.Deserialize<ServerStatusRequest>(ms),
                    MessageType.ServerStatusResponse => Serializer.Deserialize<ServerStatusResponse>(ms),

                    // ?Œë ˆ?´ì–´ ?•ë³´
                    MessageType.PlayerInfoUpdate => Serializer.Deserialize<PlayerInfoUpdate>(ms),
                    MessageType.HealthActionRequest => Serializer.Deserialize<HealthActionRequest>(ms),
                    MessageType.HealthActionResponse => Serializer.Deserialize<HealthActionResponse>(ms),
                    MessageType.HealthUpdate => Serializer.Deserialize<HealthUpdateMessage>(ms),
                    MessageType.RespawnRequest => Serializer.Deserialize<RespawnRequest>(ms),
                    MessageType.RespawnResponse => Serializer.Deserialize<RespawnResponse>(ms),
                    MessageType.PlayerDeath => Serializer.Deserialize<PlayerDeathMessage>(ms),
                    MessageType.PlayerRespawnBroadcast => Serializer.Deserialize<PlayerRespawnBroadcast>(ms),

                    _ => body
                };
            }
            else
            {
                message = body;
            }

            return new IncomingMessage(rawType, knownType, message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to receive message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ì§€?•ëœ ?¬ê¸°ë§Œí¼ ?•í™•?˜ê²Œ ?°ì´?°ë? ?½ìŠµ?ˆë‹¤.
    /// </summary>
    private async Task<byte[]> ReadExactAsync(int size)
    {
        var buffer = new byte[size];
        int read = 0;
        while (read < size)
        {
            var n = await _stream.ReadAsync(buffer, read, size - read);
            if (n == 0) 
                throw new IOException("Client disconnected unexpectedly");
            read += n;
        }
        
        // ë§ˆì?ë§??œë™ ?œê°„ ?…ë°?´íŠ¸
        LastActivityAt = DateTime.UtcNow;
        return buffer;
    }
    
    /// <summary>
    /// ?¸ì…˜??? íš¨?œì? ?•ì¸?©ë‹ˆ??
    /// </summary>
    public bool IsValidSession(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(30); // ê¸°ë³¸ 30ë¶??€?„ì•„??
        return DateTime.UtcNow - LastActivityAt <= timeout;
    }
    
    /// <summary>
    /// ?¸ì…˜???ˆì „?˜ê²Œ ì¢…ë£Œ?©ë‹ˆ??
    /// </summary>
    public void Dispose()
    {
        try
        {
            _stream?.Dispose();
            _client?.Close();
        }
        catch (Exception)
        {
            // ì¢…ë£Œ ???ˆì™¸ ë¬´ì‹œ
        }
    }
}

