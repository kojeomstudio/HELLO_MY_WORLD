using System;
using SharedProtocol;

namespace GameServerApp.Models
{
    /// <summary>
    /// Persistent representation of a world container (e.g., chest, furnace).
    /// </summary>
    public sealed class ContainerRecord
    {
        public int Id { get; init; }
        public int WorldId { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Z { get; init; }
        public ContainerType ContainerType { get; init; }
        public int SlotCount { get; init; }
        public string ItemsJson { get; init; } = string.Empty;
        public DateTime LastUpdatedUtc { get; init; }
    }
}
