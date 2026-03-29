using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// World map control protocol messages
/// </summary>
public static class WorldMapControlMessages
{
    [ProtoContract]
    public class WorldMapLoadRequest
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public int RegionSize { get; set; }
        [ProtoMember(4)] public Common.Enums.WorldEnums.WorldMapDetailLevel DetailLevel { get; set; }
    }
    
    [ProtoContract]
    public class WorldMapLoadResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public WorldMapData MapData { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapData
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public byte[] BiomeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] HeightMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] WaterMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] FeatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public System.Collections.Generic.List<WorldMapRegion> Regions { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapRegion
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Z { get; set; }
        [ProtoMember(3)] public int Width { get; set; }
        [ProtoMember(4)] public int Height { get; set; }
        [ProtoMember(5)] public Common.Enums.BiomeEnums.BiomeType PrimaryBiome { get; set; }
        [ProtoMember(6)] public float WaterCoverage { get; set; }
        [ProtoMember(7)] public float CaveDensity { get; set; }
    }
    
    [ProtoContract]
    public class WorldMapUpdateBroadcast
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public Common.Enums.WorldEnums.MapUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
}
