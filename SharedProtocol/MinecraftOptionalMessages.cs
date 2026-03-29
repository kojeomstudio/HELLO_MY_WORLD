using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SharedProtocol
{
    /// <summary>
    /// Legacy protobuf-net contracts for optional Minecraft message types that are
    /// not yet promoted to EnhancedMinecraft Google.Protobuf descriptors.
    /// </summary>
    [ProtoContract]
    public class MultiBlockChangeRequestMessage
    {
        [ProtoMember(1)] public List<BlockChangeRequestEntry> Changes { get; set; } = new();
    }

    [ProtoContract]
    public class BlockChangeRequestEntry
    {
        [ProtoMember(1)] public Vector3I Position { get; set; } = new();
        [ProtoMember(2)] public int NewBlockId { get; set; }
        [ProtoMember(3)] public int Metadata { get; set; }
        [ProtoMember(4)] public string BlockEntityData { get; set; } = string.Empty;
        [ProtoMember(5)] public PlayerActionType ActionType { get; set; } = PlayerActionType.PlaceBlock;
        [ProtoMember(6)] public int Sequence { get; set; }
    }

    [ProtoContract]
    public class MultiBlockChangeResponseMessage
    {
        [ProtoMember(1)] public List<BlockChangeResultEntry> Results { get; set; } = new();
        [ProtoMember(2)] public bool AllSuccess { get; set; }
    }

    [ProtoContract]
    public class BlockChangeResultEntry
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public Vector3I Position { get; set; } = new();
        [ProtoMember(4)] public int ActualBlockId { get; set; }
        [ProtoMember(5)] public int Sequence { get; set; }
    }

    [ProtoContract]
    public class ItemPickupRequestMessage
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public int RequestedQuantity { get; set; }
        [ProtoMember(3)] public int Sequence { get; set; }
    }

    [ProtoContract]
    public class ItemPickupResponseMessage
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(4)] public InventoryItemInfo PickedItem { get; set; } = new();
        [ProtoMember(5)] public int RemainingQuantity { get; set; }
        [ProtoMember(6)] public int Sequence { get; set; }
    }

    public enum EntityInteractionType
    {
        Interact = 0,
        Attack = 1,
        Trade = 2,
        Mount = 3
    }

    [ProtoContract]
    public class EntityInteractRequestMessage
    {
        [ProtoMember(1)] public string TargetEntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public EntityInteractionType InteractionType { get; set; } = EntityInteractionType.Interact;
        [ProtoMember(3)] public InventoryItemInfo UsedItem { get; set; } = new();
        [ProtoMember(4)] public int Sequence { get; set; }
        [ProtoMember(5)] public long Timestamp { get; set; }
    }

    [ProtoContract]
    public class EntityInteractResponseMessage
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public string TargetEntityId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Sequence { get; set; }
    }
}
