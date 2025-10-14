using System.Collections.Generic;
using ProtoBuf;

namespace SharedProtocol
{
    [ProtoContract]
    public class SlotUpdate
    {
        [ProtoMember(1)] public int Slot { get; set; }
        [ProtoMember(2)] public InventoryItemInfo Item { get; set; } = new();
        [ProtoMember(3)] public string ItemIdentifier { get; set; } = string.Empty;
    }

    public enum ContainerType
    {
        Chest = 0,
        Furnace = 1,
        CraftingTable = 2,
        EnchantingTable = 3,
        BrewingStand = 4,
        Dispenser = 5,
        Hopper = 6,
        Beacon = 7,
        Anvil = 8
    }

    [ProtoContract]
    public class ContainerOpenRequestMessage
    {
        [ProtoMember(1)] public Vector3I Position { get; set; } = new();
        [ProtoMember(2)] public ContainerType ContainerType { get; set; }
    }

    [ProtoContract]
    public class ContainerOpenResponseMessage
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public int ContainerId { get; set; }
        [ProtoMember(3)] public List<SlotUpdate> Slots { get; set; } = new();
        [ProtoMember(4)] public string ContainerTitle { get; set; } = string.Empty;
        [ProtoMember(5)] public ContainerProperties Properties { get; set; } = new();
        [ProtoMember(6)] public string ErrorMessage { get; set; } = string.Empty;
        [ProtoMember(7)] public ContainerType ContainerType { get; set; }
        [ProtoMember(8)] public string SnapshotHash { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class ContainerProperties
    {
        [ProtoMember(1)] public int SlotCount { get; set; }
        [ProtoMember(2)] public int FuelSlot { get; set; }
        [ProtoMember(3)] public int ResultSlot { get; set; }
        [ProtoMember(4)] public float Progress { get; set; }
    }

    [ProtoContract]
    public class ContainerCloseRequestMessage
    {
        [ProtoMember(1)] public int ContainerId { get; set; }
    }

    [ProtoContract]
    public class ContainerCloseNotificationMessage
    {
        [ProtoMember(1)] public int ContainerId { get; set; }
        [ProtoMember(2)] public string Reason { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class ContainerUpdateRequestMessage
    {
        [ProtoMember(1)] public int ContainerId { get; set; }
        [ProtoMember(2)] public List<SlotUpdate> SlotUpdates { get; set; } = new();
        [ProtoMember(3)] public bool ForceFullSync { get; set; }
        [ProtoMember(4)] public string ClientSnapshotHash { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class ContainerUpdateBroadcastMessage
    {
        [ProtoMember(1)] public int ContainerId { get; set; }
        [ProtoMember(2)] public List<SlotUpdate> SlotUpdates { get; set; } = new();
        [ProtoMember(3)] public ContainerProperties Properties { get; set; } = new();
        [ProtoMember(4)] public bool IsFullSync { get; set; }
        [ProtoMember(5)] public ContainerType ContainerType { get; set; }
        [ProtoMember(6)] public string SnapshotHash { get; set; } = string.Empty;
    }
}
