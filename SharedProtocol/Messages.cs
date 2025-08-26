using ProtoBuf;

namespace SharedProtocol;

public enum MessageType
{
    LoginRequest = 1,
    LoginResponse = 2,
    MoveRequest = 3,
    MoveResponse = 4,
}

[ProtoContract]
public class LoginRequest
{
    [ProtoMember(1)] public string Name { get; set; } = string.Empty;
}

[ProtoContract]
public class LoginResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}

[ProtoContract]
public class MoveRequest
{
    [ProtoMember(1)] public string Name { get; set; } = string.Empty;
    [ProtoMember(2)] public double Dx { get; set; }
    [ProtoMember(3)] public double Dy { get; set; }
}

[ProtoContract]
public class MoveResponse
{
    [ProtoMember(1)] public double X { get; set; }
    [ProtoMember(2)] public double Y { get; set; }
}
