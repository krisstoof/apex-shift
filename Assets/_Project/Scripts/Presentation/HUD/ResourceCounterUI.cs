using UnityEngine;
using UnityEngine.UI;
using ApexShift.Presentation.Icons;

namespace ApexShift.Presentation.HUD
{
    public sealed class ResourceCounterUI : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text countText;

        public void Configure(string id, Image iconComp, Text textComp)
        {
            itemId = id;
            iconImage = iconComp;
            countText = textComp;
        }

        public string ItemId => itemId;

        private void Awake()
        {
            RefreshIcon();
        }

        private void OnValidate()
        {
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            if (iconImage == null || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            Texture2D iconTexture = ApexShiftIconPack.GetIcon(itemId);
            if (iconTexture == null)
            {
                return;
            }

            iconImage.sprite = Sprite.Create(
                iconTexture,
                new Rect(0f, 0f, iconTexture.width, iconTexture.height),
                new Vector2(0.5f, 0.5f),
                128f);
            iconImage.preserveAspect = true;
        }

        public void UpdateCount(int count)
        {
            if (countText != null)
            {
                countText.text = count.ToString();
                Debug.Log($"[HUD] Updated {itemId} count to {count}", this);
            }
        }
    }
}
