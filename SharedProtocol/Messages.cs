using ProtoBuf;

namespace SharedProtocol;

/// <summary>
/// 메시지 타입 정의 - 클라이언트와 서버 간 통신에 사용
/// </summary>
public enum MessageType
{
    // 인증 관련
    LoginRequest = 1,
    LoginResponse = 2,
    LogoutRequest = 3,
    LogoutResponse = 4,
    
    // 이동 관련
    MoveRequest = 10,
    MoveResponse = 11,
    
    // 월드/블록 관련
    WorldBlockChangeRequest = 20,
    WorldBlockChangeResponse = 21,
    WorldBlockChangeBroadcast = 22,
    
    // 채팅 관련
    ChatRequest = 30,
    ChatResponse = 31,
    ChatMessage = 32,
    
    // 서버 상태/진단
    PingRequest = 40,
    PingResponse = 41,
    ServerStatusRequest = 42,
    ServerStatusResponse = 43,
    
    // 플레이어 정보 업데이트
    PlayerInfoUpdate = 50,
}

// 기본 데이터 구조
[ProtoContract]
public class Vector3
{
    [ProtoMember(1)] public float X { get; set; }
    [ProtoMember(2)] public float Y { get; set; }
    [ProtoMember(3)] public float Z { get; set; }
    
    public Vector3() { }
    public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
}

[ProtoContract]
public class Vector3Int
{
    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public int Y { get; set; }
    [ProtoMember(3)] public int Z { get; set; }
    
    public Vector3Int() { }
    public Vector3Int(int x, int y, int z) { X = x; Y = y; Z = z; }
}

[ProtoContract]
public class InventoryItem
{
    [ProtoMember(1)] public int ItemId { get; set; }
    [ProtoMember(2)] public string ItemName { get; set; } = string.Empty;
    [ProtoMember(3)] public int Quantity { get; set; }
}

[ProtoContract]
public class PlayerInfo
{
    [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
    [ProtoMember(2)] public string Username { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3? Position { get; set; }
    [ProtoMember(4)] public int Level { get; set; }
    [ProtoMember(5)] public int Health { get; set; }
    [ProtoMember(6)] public int MaxHealth { get; set; }
    [ProtoMember(7)] public List<InventoryItem> Inventory { get; set; } = new();
}

// 인증 관련 메시지
[ProtoContract]
public class LoginRequest
{
    [ProtoMember(1)] public string Username { get; set; } = string.Empty;
    [ProtoMember(2)] public string Password { get; set; } = string.Empty;
    [ProtoMember(3)] public string ClientVersion { get; set; } = string.Empty;
}

[ProtoContract]
public class LoginResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public string SessionToken { get; set; } = string.Empty;
    [ProtoMember(4)] public PlayerInfo? PlayerInfo { get; set; }
}

[ProtoContract]
public class LogoutRequest
{
    [ProtoMember(1)] public string SessionToken { get; set; } = string.Empty;
}

[ProtoContract]
public class LogoutResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}

// 이동 관련 메시지
[ProtoContract]
public class MoveRequest
{
    [ProtoMember(1)] public Vector3? TargetPosition { get; set; }
    [ProtoMember(2)] public float MovementSpeed { get; set; }
}

[ProtoContract]
public class MoveResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public Vector3? NewPosition { get; set; }
    [ProtoMember(3)] public long Timestamp { get; set; }
}

// 월드/블록 관련 메시지
[ProtoContract]
public class WorldBlockChangeRequest
{
    [ProtoMember(1)] public string AreaId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SubworldId { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3Int? BlockPosition { get; set; }
    [ProtoMember(4)] public int BlockType { get; set; }
    [ProtoMember(5)] public int ChunkType { get; set; }
}

[ProtoContract]
public class WorldBlockChangeResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public long Timestamp { get; set; }
}

[ProtoContract]
public class WorldBlockChangeBroadcast
{
    [ProtoMember(1)] public string AreaId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SubworldId { get; set; } = string.Empty;
    [ProtoMember(3)] public Vector3Int? BlockPosition { get; set; }
    [ProtoMember(4)] public int BlockType { get; set; }
    [ProtoMember(5)] public int ChunkType { get; set; }
    [ProtoMember(6)] public string PlayerId { get; set; } = string.Empty;
    [ProtoMember(7)] public long Timestamp { get; set; }
}

// 채팅 관련 메시지
public enum ChatType
{
    Global = 0,
    Local = 1,
    Whisper = 2,
    System = 3
}

[ProtoContract]
public class ChatRequest
{
    [ProtoMember(1)] public string Message { get; set; } = string.Empty;
    [ProtoMember(2)] public int Type { get; set; } // ChatType을 int로 저장
    [ProtoMember(3)] public string TargetPlayer { get; set; } = string.Empty;
}

[ProtoContract]
public class ChatResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string ErrorMessage { get; set; } = string.Empty;
}

[ProtoContract]
public class ChatMessage
{
    [ProtoMember(1)] public string SenderId { get; set; } = string.Empty;
    [ProtoMember(2)] public string SenderName { get; set; } = string.Empty;
    [ProtoMember(3)] public string Message { get; set; } = string.Empty;
    [ProtoMember(4)] public int Type { get; set; } // ChatType을 int로 저장
    [ProtoMember(5)] public long Timestamp { get; set; }
}

// 서버 상태 및 진단 메시지
[ProtoContract]
public class PingRequest
{
    [ProtoMember(1)] public long ClientTimestamp { get; set; }
}

[ProtoContract]
public class PingResponse
{
    [ProtoMember(1)] public long ClientTimestamp { get; set; }
    [ProtoMember(2)] public long ServerTimestamp { get; set; }
}

[ProtoContract]
public class ServerStatusRequest
{
    [ProtoMember(1)] public string SessionToken { get; set; } = string.Empty;
}

[ProtoContract]
public class ServerStatusResponse
{
    [ProtoMember(1)] public int OnlinePlayers { get; set; }
    [ProtoMember(2)] public string ServerVersion { get; set; } = string.Empty;
    [ProtoMember(3)] public long ServerUptime { get; set; }
}

[ProtoContract]
public class PlayerInfoUpdate
{
    [ProtoMember(1)] public PlayerInfo? PlayerInfo { get; set; }
    [ProtoMember(2)] public long Timestamp { get; set; }
}
