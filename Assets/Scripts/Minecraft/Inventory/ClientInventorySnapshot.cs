using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Minecraft.Inventory
{
    /// <summary>
    /// Immutable snapshot of the player's inventory sent from the server.
    /// Provides helper utilities for diffing and slot lookups.
    /// </summary>
    public sealed class ClientInventorySnapshot
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<int, ClientInventorySlot> _slots;

        private ClientInventorySnapshot(DateTime savedAtUtc, Dictionary<int, ClientInventorySlot> slots)
        {
            SavedAtUtc = savedAtUtc;
            _slots = slots;
        }

        public static ClientInventorySnapshot Empty { get; } = new ClientInventorySnapshot(DateTime.UtcNow, new Dictionary<int, ClientInventorySlot>());

        public DateTime SavedAtUtc { get; }

        public IReadOnlyCollection<ClientInventorySlot> Slots => _slots.Values;

        public ClientInventorySlot GetSlot(int slotIndex)
        {
            return _slots.TryGetValue(slotIndex, out var slot)
                ? slot
                : ClientInventorySlot.Empty(slotIndex);
        }

        public IReadOnlyList<ClientInventorySlot> GetOrderedSlots()
        {
            if (_slots.Count == 0)
            {
                return Array.Empty<ClientInventorySlot>();
            }

            return _slots.Values
                .OrderBy(slot => slot.SlotIndex)
                .ToArray();
        }

        public IReadOnlyList<ClientInventorySlot> GetChangedSlots(ClientInventorySnapshot? previous)
        {
            if (previous == null || previous == Empty || previous._slots.Count == 0)
            {
                return GetOrderedSlots();
            }

            var changed = new List<ClientInventorySlot>();

            foreach (var slot in _slots.Values)
            {
                var previousSlot = previous.GetSlot(slot.SlotIndex);
                if (!slot.Equals(previousSlot))
                {
                    changed.Add(slot);
                }
            }

            foreach (var kvp in previous._slots)
            {
                if (!_slots.ContainsKey(kvp.Key))
                {
                    changed.Add(ClientInventorySlot.Empty(kvp.Key));
                }
            }

            return changed;
        }

        public static bool TryParse(string json, out ClientInventorySnapshot snapshot, out string error)
        {
            snapshot = Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Snapshot payload was empty.";
                return false;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<InventorySnapshotPayload>(json, SerializerOptions);
                if (payload == null)
                {
                    error = "Snapshot payload deserialized to null.";
                    return false;
                }

                var slots = new Dictionary<int, ClientInventorySlot>();
                if (payload.Slots != null)
                {
                    foreach (var slot in payload.Slots)
                    {
                        if (slot == null)
                        {
                            continue;
                        }

                        var normalizedIndex = Math.Max(0, slot.SlotIndex);
                        slots[normalizedIndex] = new ClientInventorySlot(
                            normalizedIndex,
                            slot.ItemId ?? string.Empty,
                            Math.Max(0, slot.Amount),
                            slot.ItemData ?? string.Empty);
                    }
                }

                snapshot = new ClientInventorySnapshot(payload.SavedAtUtc, slots);
                return true;
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse inventory snapshot JSON: {ex.Message}";
                return false;
            }
        }

        private sealed class InventorySnapshotPayload
        {
            public string UserName { get; set; } = string.Empty;
            public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
            public List<InventorySlotPayload>? Slots { get; set; }
        }

        private sealed class InventorySlotPayload
        {
            public int SlotIndex { get; set; }
            public string? ItemId { get; set; }
            public int Amount { get; set; }
            public string? ItemData { get; set; }
        }
    }

    public readonly struct ClientInventorySlot : IEquatable<ClientInventorySlot>
    {
        public ClientInventorySlot(int slotIndex, string itemId, int amount, string itemData)
        {
            SlotIndex = slotIndex;
            ItemId = itemId ?? string.Empty;
            Amount = amount;
            ItemData = itemData ?? string.Empty;
        }

        public int SlotIndex { get; }
        public string ItemId { get; }
        public int Amount { get; }
        public string ItemData { get; }
        public bool IsEmpty => Amount <= 0 || string.IsNullOrWhiteSpace(ItemId);

        public static ClientInventorySlot Empty(int slotIndex)
        {
            return new ClientInventorySlot(slotIndex, string.Empty, 0, string.Empty);
        }

        public bool Equals(ClientInventorySlot other)
        {
            return SlotIndex == other.SlotIndex
                   && Amount == other.Amount
                   && string.Equals(ItemId, other.ItemId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(ItemData, other.ItemData, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ClientInventorySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + SlotIndex;
                hash = (hash * 31) + (ItemId?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = (hash * 31) + Amount;
                hash = (hash * 31) + (ItemData?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}