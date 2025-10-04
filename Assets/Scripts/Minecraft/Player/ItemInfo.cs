using System;
using SharedProtocol;

namespace Minecraft.Player
{
    /// <summary>
    /// Lightweight client-side representation of an inventory item used by the hotbar and UI.
    /// </summary>
    [Serializable]
    public class ItemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public ItemType Type { get; set; } = ItemType.Material;
        public string CustomData { get; set; } = string.Empty;

        public bool IsEmpty => Quantity <= 0 || string.IsNullOrEmpty(Name);

        public ItemInfo Clone()
        {
            return new ItemInfo
            {
                Id = Id,
                Name = Name,
                Quantity = Quantity,
                Type = Type,
                CustomData = CustomData
            };
        }

        public override string ToString()
        {
            return $"{Name} x{Quantity}";
        }
    }
}