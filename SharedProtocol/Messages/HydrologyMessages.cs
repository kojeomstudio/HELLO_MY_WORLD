using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// Hydrology protocol messages
/// </summary>
public static class HydrologyMessages
{
    [ProtoContract]
    public class HydrologyDataRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public Common.Enums.TerrainGenerationEnums.HydrologyDataType DataType { get; set; }
    }
    
    [ProtoContract]
    public class HydrologyDataResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public HydrologyData Data { get; set; } = new();
    }
    
    [ProtoContract]
    public class HydrologyData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] SlopeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] CurvatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ReliefMap { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class HydrologyUpdateBroadcast
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public Common.Enums.TerrainGenerationEnums.HydrologyUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
}

namespace SharedProtocol.Messages;

/// <summary>
/// Hydrology protocol messages
/// </summary>
public static class HydrologyMessages
{
    [ProtoContract]
    public class HydrologyDataRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public Common.Enums.TerrainGenerationEnums.HydrologyDataType DataType { get; set; }
    }
    
    [ProtoContract]
    public class HydrologyDataResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public HydrologyData Data { get; set; } = new();
    }
    
    [ProtoContract]
    public class HydrologyData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] SlopeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] CurvatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ReliefMap { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class HydrologyUpdateBroadcast
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public Common.Enums.TerrainGenerationEnums.HydrologyUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
}

