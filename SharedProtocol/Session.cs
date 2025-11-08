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
    /// ?�션 ?�큰 - 로그???�공 ???�성?�니??
    /// </summary>
    public string? SessionToken { get; set; }
    
    /// <summary>
    /// ?�션 ?�성 ?�간
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// 마�?�??�동 ?�간
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ?�레?�어 ?�보
    /// </summary>
    public PlayerInfo? PlayerInfo { get; set; }

    public Session(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    /// <summary>
    /// 메시지�?비동기적?�로 직렬?�하???�송?�니??
    /// </summary>
    public async Task SendAsync<T>(MessageType type, T message)
    {
        try
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, message);
            var body = ms.ToArray();
            
            // 메시지 ?�기 체크
            if (body.Length > 1024 * 1024) // 1MB ?�한
                throw new InvalidDataException($"Message too large: {body.Length} bytes");
            
            var length = BitConverter.GetBytes(body.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes((int)type);
            
            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();
            
            // 마�?�??�동 ?�간 ?�데?�트
            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message of type {type}: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// JSON으로 메시지를 직렬화하여 전송합니다 (AI 메시지용).
    /// ProtoBuf 대신 JSON을 사용하여 Unity JsonUtility와 호환됩니다.
    /// </summary>
    public async Task SendAsJsonAsync<T>(MessageType type, T message)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            if (body.Length > 1024 * 1024)
                throw new InvalidDataException($"Message too large: {body.Length} bytes");

            var length = BitConverter.GetBytes(body.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes((int)type);

            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();

            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send JSON message of type {type}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// JSON으로 메시지를 직렬화하여 전송합니다 (AI 메시지용).
    /// ProtoBuf 대신 JSON을 사용하여 Unity JsonUtility와 호환됩니다.
    /// </summary>
    public async Task SendAsJsonAsync<T>(MessageType type, T message)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            if (body.Length > 1024 * 1024)
                throw new InvalidDataException($"Message too large: {body.Length} bytes");

            var length = BitConverter.GetBytes(body.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes((int)type);

            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();

            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send JSON message of type {type}: {ex.Message}", ex);
        }
    }
    /// <summary>
    /// ?�시 바이???�이로드�?지?�한 ?�수??메시지 ?�?�과 ?�께 ?�송?�니??
    /// 마인?�래?�트 ?�장 메시지(MinecraftMessageType) ??enum ???�??코드�?지?�합?�다.
    /// </summary>
    public async Task SendAsync(int rawMessageType, byte[] payload)
    {
        try
        {
            payload ??= Array.Empty<byte>();

            if (payload.Length > 1024 * 1024) // 1MB ?�한
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
    /// ?�버로�???메시지�?비동기적?�로 ?�신?�고 ??��?�화?�니??
    /// </summary>
    public async Task<IncomingMessage> ReceiveAsync()
    {
        try
        {
            var lenBuf = await ReadExactAsync(sizeof(int));
            var length = BitConverter.ToInt32(lenBuf, 0);
            
            // ?�못??길이 체크
            if (length <= sizeof(int) || length > 1024 * 1024) // 1MB ?�한
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
                    // ?�증 관??
                    MessageType.LoginRequest => Serializer.Deserialize<LoginRequest>(ms),
                    MessageType.LoginResponse => Serializer.Deserialize<LoginResponse>(ms),
                    MessageType.LogoutRequest => Serializer.Deserialize<LogoutRequest>(ms),
                    MessageType.LogoutResponse => Serializer.Deserialize<LogoutResponse>(ms),

                    // ?�동 관??
                    MessageType.MoveRequest => Serializer.Deserialize<MoveRequest>(ms),
                    MessageType.MoveResponse => Serializer.Deserialize<MoveResponse>(ms),

                    // ?�드/블록 관??
                    MessageType.WorldBlockChangeRequest => Serializer.Deserialize<WorldBlockChangeRequest>(ms),
                    MessageType.WorldBlockChangeResponse => Serializer.Deserialize<WorldBlockChangeResponse>(ms),
                    MessageType.WorldBlockChangeBroadcast => Serializer.Deserialize<WorldBlockChangeBroadcast>(ms),

                    // 채팅 관??
                    MessageType.ChatRequest => Serializer.Deserialize<ChatRequest>(ms),
                    MessageType.ChatResponse => Serializer.Deserialize<ChatResponse>(ms),
                    MessageType.ChatMessage => Serializer.Deserialize<ChatMessage>(ms),

                    // ?�버 ?�태/진단
                    MessageType.PingRequest => Serializer.Deserialize<PingRequest>(ms),
                    MessageType.PingResponse => Serializer.Deserialize<PingResponse>(ms),
                    MessageType.ServerStatusRequest => Serializer.Deserialize<ServerStatusRequest>(ms),
                    MessageType.ServerStatusResponse => Serializer.Deserialize<ServerStatusResponse>(ms),

                    // ?�레?�어 ?�보
                    MessageType.PlayerInfoUpdate => Serializer.Deserialize<PlayerInfoUpdate>(ms),
                    MessageType.HealthActionRequest => Serializer.Deserialize<HealthActionRequest>(ms),
                    MessageType.HealthActionResponse => Serializer.Deserialize<HealthActionResponse>(ms),
                    MessageType.HealthUpdate => Serializer.Deserialize<HealthUpdateMessage>(ms),
                    MessageType.RespawnRequest => Serializer.Deserialize<RespawnRequest>(ms),
                    MessageType.RespawnResponse => Serializer.Deserialize<RespawnResponse>(ms),
                    MessageType.PlayerDeath => Serializer.Deserialize<PlayerDeathMessage>(ms),
                    MessageType.PlayerRespawnBroadcast => Serializer.Deserialize<PlayerRespawnBroadcast>(ms),


                    // AI System (Server-Authoritative) - JSON deserialization
                    MessageType.AIStateSyncBroadcast => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AIStateSyncBroadcast>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AIAttackEventBroadcast => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AIAttackEventBroadcast>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AIDeathEventBroadcast => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AIDeathEventBroadcast>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AISpawnRequest => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AISpawnRequest>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AISpawnResponse => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AISpawnResponse>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AIDebugInfoRequest => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AIDebugInfoRequest>(System.Text.Encoding.UTF8.GetString(body)),
                    MessageType.AIDebugInfoResponse => System.Text.Json.JsonSerializer.Deserialize<GameProtocol.AIDebugInfoResponse>(System.Text.Encoding.UTF8.GetString(body)),
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
    /// 지?�된 ?�기만큼 ?�확?�게 ?�이?��? ?�습?�다.
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
        
        // 마�?�??�동 ?�간 ?�데?�트
        LastActivityAt = DateTime.UtcNow;
        return buffer;
    }
    
    /// <summary>
    /// ?�션???�효?��? ?�인?�니??
    /// </summary>
    public bool IsValidSession(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(30); // 기본 30�??�?�아??
        return DateTime.UtcNow - LastActivityAt <= timeout;
    }
    
    /// <summary>
    /// ?�션???�전?�게 종료?�니??
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
            // 종료 ???�외 무시
        }
    }
}

