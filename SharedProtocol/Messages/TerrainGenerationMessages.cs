using ProtoBuf;
using SharedProtocol;

namespace SharedProtocol.Messages;

/// <summary>
/// Terrain generation protocol messages
/// </summary>
public static class TerrainGenerationMessages
{
    [ProtoContract]
    public class TerrainGenerationRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public int WorldHeight { get; set; }
        [ProtoMember(5)] public long WorldSeed { get; set; }
        [ProtoMember(6)] public TerrainGenerationOptions Options { get; set; } = new();
    }
    
    [ProtoContract]
    public class TerrainGenerationOptions
    {
        [ProtoMember(1)] public bool GenerateCaves { get; set; }
        [ProtoMember(2)] public bool GenerateRivers { get; set; }
        [ProtoMember(3)] public bool GenerateLakes { get; set; }
        [ProtoMember(4)] public CaveGenerationOptions CaveOptions { get; set; } = new();
        [ProtoMember(5)] public RiverGenerationOptions RiverOptions { get; set; } = new();
        [ProtoMember(6)] public LakeGenerationOptions LakeOptions { get; set; } = new();
    }
    
    [ProtoContract]
    public class CaveGenerationOptions
    {
        [ProtoMember(1)] public double Threshold { get; set; }
        [ProtoMember(2)] public double HorizontalFrequency { get; set; }
        [ProtoMember(3)] public double VerticalFrequency { get; set; }
    }
    
    [ProtoContract]
    public class RiverGenerationOptions
    {
        [ProtoMember(1)] public double BankThreshold { get; set; }
        [ProtoMember(2)] public double NoiseScale { get; set; }
    }
    
    [ProtoContract]
    public class LakeGenerationOptions
    {
        [ProtoMember(1)] public double WetlandThreshold { get; set; }
        [ProtoMember(2)] public double SpawnWeightBias { get; set; }
    }
    
    [ProtoContract]
    public class TerrainGenerationResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public TerrainData TerrainData { get; set; } = new();
        [ProtoMember(4)] public long GenerationTimeMs { get; set; }
    }
    
    [ProtoContract]
    public class TerrainData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] CaveMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] RiverMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] LakeMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class TerrainFeatureData
    {
        [ProtoMember(1)] public Common.Enums.TerrainGenerationEnums.TerrainFeatureType FeatureType { get; set; }
        [ProtoMember(2)] public Vector3Int Position { get; set; } = new();
        [ProtoMember(3)] public int FeatureId { get; set; }
        [ProtoMember(4)] public string FeatureData { get; set; } = string.Empty;
    }
}
