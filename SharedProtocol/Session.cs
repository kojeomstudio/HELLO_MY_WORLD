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

    public Session(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public async Task SendAsync<T>(MessageType type, T message)
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, message);
        var body = ms.ToArray();
        var length = BitConverter.GetBytes(body.Length + sizeof(int));
        var typeBytes = BitConverter.GetBytes((int)type);
        await _stream.WriteAsync(length, 0, length.Length);
        await _stream.WriteAsync(typeBytes, 0, typeBytes.Length);
        await _stream.WriteAsync(body, 0, body.Length);
    }

    public async Task<(MessageType type, object message)> ReceiveAsync()
    {
        var lenBuf = await ReadExactAsync(sizeof(int));
        var length = BitConverter.ToInt32(lenBuf, 0);
        var typeBuf = await ReadExactAsync(sizeof(int));
        var type = (MessageType)BitConverter.ToInt32(typeBuf, 0);
        var body = await ReadExactAsync(length - sizeof(int));
        using var ms = new MemoryStream(body);
        object message = type switch
        {
            MessageType.LoginRequest => Serializer.Deserialize<LoginRequest>(ms),
            MessageType.LoginResponse => Serializer.Deserialize<LoginResponse>(ms),
            MessageType.MoveRequest => Serializer.Deserialize<MoveRequest>(ms),
            MessageType.MoveResponse => Serializer.Deserialize<MoveResponse>(ms),
            _ => throw new InvalidOperationException($"Unknown message type {type}")
        };
        return (type, message);
    }

    private async Task<byte[]> ReadExactAsync(int size)
    {
        var buffer = new byte[size];
        int read = 0;
        while (read < size)
        {
            var n = await _stream.ReadAsync(buffer, read, size - read);
            if (n == 0) throw new IOException("Disconnected");
            read += n;
        }
        return buffer;
    }
}
