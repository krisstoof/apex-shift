using ApexShift.Presentation.Icons;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text amountText;
        [SerializeField] private Text slotIndexText;

        private string itemId = string.Empty;

        public void Configure(Image icon, Text amount, Text slotIndex)
        {
            iconImage = icon;
            amountText = amount;
            slotIndexText = slotIndex;
        }

        public void UpdateSlot(int slotIndex, string newItemId, int amount)
        {
            itemId = newItemId ?? string.Empty;

            if (slotIndexText != null)
            {
                slotIndexText.text = (slotIndex + 1).ToString();
            }

            if (amountText != null)
            {
                amountText.text = amount > 0 ? amount.ToString() : string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.enabled = !string.IsNullOrWhiteSpace(itemId);
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    Texture2D iconTexture = ApexShiftIconPack.GetIcon(itemId);
                    if (iconTexture != null)
                    {
                        iconImage.sprite = Sprite.Create(
                            iconTexture,
                            new Rect(0f, 0f, iconTexture.width, iconTexture.height),
                            new Vector2(0.5f, 0.5f),
                            128f);
                    }
                }
            }
        }
    }
}
