using System;
using Minecraft.Containers;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    public enum ContainerSlotRole
    {
        Generic = 0,
        Fuel = 1,
        Result = 2
    }

    /// <summary>
    /// Displays a single container slot entry (item name, quantity, and role hint).
    /// </summary>
    public class ContainerSlotView : MonoBehaviour
    {
        [SerializeField] private Text slotIndexText;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text quantityText;
        [SerializeField] private Text roleLabelText;
        [SerializeField] private Image iconImage;

        private int _slotIndex;

        public int SlotIndex => _slotIndex;

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;

            if (slotIndexText != null)
            {
                slotIndexText.text = (slotIndex + 1).ToString();
            }

            Clear();
        }

        public void Bind(ContainerSlotState slotState)
        {
            if (slotState == null || slotState.IsEmpty)
            {
                Clear();
                return;
            }

            if (itemNameText != null)
            {
                var itemName = !string.IsNullOrWhiteSpace(slotState.Item?.ItemName)
                    ? slotState.Item.ItemName
                    : slotState.ItemIdentifier;
                itemNameText.text = itemName;
            }

            if (quantityText != null)
            {
                if (slotState.Item != null && slotState.Item.Quantity > 1)
                {
                    quantityText.text = $"x{slotState.Item.Quantity}";
                }
                else
                {
                    quantityText.text = string.Empty;
                }
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
            }
        }

        public void Clear()
        {
            if (itemNameText != null)
            {
                itemNameText.text = "Empty";
            }

            if (quantityText != null)
            {
                quantityText.text = string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
        }

        public void SetRole(ContainerSlotRole role)
        {
            if (roleLabelText == null)
            {
                return;
            }

            roleLabelText.text = role switch
            {
                ContainerSlotRole.Fuel => "Fuel",
                ContainerSlotRole.Result => "Output",
                _ => string.Empty
            };
        }
    }
}
