using System.Net.Sockets;
using ProtoBuf;

namespace SharedProtocol;

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
    /// 세션 토큰 - 로그인 성공 후 생성됩니다.
    /// </summary>
    public string? SessionToken { get; set; }
    
    /// <summary>
    /// 세션 생성 시간
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// 마지막 활동 시간
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 플레이어 정보
    /// </summary>
    public PlayerInfo? PlayerInfo { get; set; }

    public Session(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    /// <summary>
    /// 메시지를 비동기적으로 직렬화하여 전송합니다.
    /// </summary>
    public async Task SendAsync<T>(MessageType type, T message)
    {
        try
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, message);
            var body = ms.ToArray();
            
            // 메시지 크기 체크
            if (body.Length > 1024 * 1024) // 1MB 제한
                throw new InvalidDataException($"Message too large: {body.Length} bytes");
            
            var length = BitConverter.GetBytes(body.Length + sizeof(int));
            var typeBytes = BitConverter.GetBytes((int)type);
            
            await _stream.WriteAsync(length, 0, length.Length);
            await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();
            
            // 마지막 활동 시간 업데이트
            LastActivityAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message of type {type}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 서버로부터 메시지를 비동기적으로 수신하고 역직렬화합니다.
    /// </summary>
    public async Task<(MessageType type, object message)> ReceiveAsync()
    {
        try
        {
            var lenBuf = await ReadExactAsync(sizeof(int));
            var length = BitConverter.ToInt32(lenBuf, 0);
            
            // 잘못된 길이 체크
            if (length <= sizeof(int) || length > 1024 * 1024) // 1MB 제한
                throw new InvalidDataException($"Invalid message length: {length}");
            
            var typeBuf = await ReadExactAsync(sizeof(int));
            var type = (MessageType)BitConverter.ToInt32(typeBuf, 0);
            var body = await ReadExactAsync(length - sizeof(int));
            
            using var ms = new MemoryStream(body);
            object message = type switch
            {
                // 인증 관련
                MessageType.LoginRequest => Serializer.Deserialize<LoginRequest>(ms),
                MessageType.LoginResponse => Serializer.Deserialize<LoginResponse>(ms),
                MessageType.LogoutRequest => Serializer.Deserialize<LogoutRequest>(ms),
                MessageType.LogoutResponse => Serializer.Deserialize<LogoutResponse>(ms),
                
                // 이동 관련
                MessageType.MoveRequest => Serializer.Deserialize<MoveRequest>(ms),
                MessageType.MoveResponse => Serializer.Deserialize<MoveResponse>(ms),
                
                // 월드/블록 관련
                MessageType.WorldBlockChangeRequest => Serializer.Deserialize<WorldBlockChangeRequest>(ms),
                MessageType.WorldBlockChangeResponse => Serializer.Deserialize<WorldBlockChangeResponse>(ms),
                MessageType.WorldBlockChangeBroadcast => Serializer.Deserialize<WorldBlockChangeBroadcast>(ms),
                
                // 채팅 관련
                MessageType.ChatRequest => Serializer.Deserialize<ChatRequest>(ms),
                MessageType.ChatResponse => Serializer.Deserialize<ChatResponse>(ms),
                MessageType.ChatMessage => Serializer.Deserialize<ChatMessage>(ms),
                
                // 서버 상태/진단
                MessageType.PingRequest => Serializer.Deserialize<PingRequest>(ms),
                MessageType.PingResponse => Serializer.Deserialize<PingResponse>(ms),
                MessageType.ServerStatusRequest => Serializer.Deserialize<ServerStatusRequest>(ms),
                MessageType.ServerStatusResponse => Serializer.Deserialize<ServerStatusResponse>(ms),
                
                // 플레이어 정보
                MessageType.PlayerInfoUpdate => Serializer.Deserialize<PlayerInfoUpdate>(ms),
                
                _ => throw new InvalidOperationException($"Unknown message type {type}")
            };
            return (type, message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to receive message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 지정된 크기만큼 정확하게 데이터를 읽습니다.
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
        
        // 마지막 활동 시간 업데이트
        LastActivityAt = DateTime.UtcNow;
        return buffer;
    }
    
    /// <summary>
    /// 세션이 유효한지 확인합니다.
    /// </summary>
    public bool IsValidSession(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(30); // 기본 30분 타임아웃
        return DateTime.UtcNow - LastActivityAt <= timeout;
    }
    
    /// <summary>
    /// 세션을 안전하게 종료합니다.
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
            // 종료 시 예외 무시
        }
    }
}
