using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SharedProtocol
{
    /// <summary>
    /// Batch broadcast for multiple block changes.
    /// </summary>
    [ProtoContract]
    public class WorldBlockChangeBatchBroadcast
    {
        [ProtoMember(1)] public string AreaId { get; set; } = "world";
        [ProtoMember(2)] public string SubworldId { get; set; } = "overworld";
        [ProtoMember(3)] public List<WorldBlockChangeData> Changes { get; set; } = new();
        [ProtoMember(4)] public long Timestamp { get; set; }
    }

    /// <summary>
    /// Individual block change data.
    /// </summary>
    [ProtoContract]
    public class WorldBlockChangeData
    {
        [ProtoMember(1)] public Vector3Int Position { get; set; }
        [ProtoMember(2)] public int BlockType { get; set; }
        [ProtoMember(3)] public int ChunkType { get; set; }
    }

    /// <summary>
    /// Player position update message.
    /// </summary>
    [ProtoContract]
    public class PlayerPositionUpdate
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public Vector3 Position { get; set; }
        [ProtoMember(3)] public Vector3 Rotation { get; set; }
        [ProtoMember(4)] public long Timestamp { get; set; }
    }

    /// <summary>
    /// Chunk data message for sending entire chunks to clients.
    /// </summary>
    [ProtoContract]
    public class ChunkDataMessage
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] BlockData { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] BiomeData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }

    /// <summary>
    /// Chunk unload message for telling clients to unload chunks.
    /// </summary>
    [ProtoContract]
    public class ChunkUnloadMessage
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public long Timestamp { get; set; }
    }
}
