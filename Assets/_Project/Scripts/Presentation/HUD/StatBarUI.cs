using UnityEngine;
using UnityEngine.UI;
using ApexShift.Presentation.Icons;

namespace ApexShift.Presentation.HUD
{
    public sealed class StatBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text label;
        [SerializeField] private string statName;

        public void Configure(Image fill, Text labelComp, string nameValue, Image icon = null)
        {
            fillImage = fill;
            label = labelComp;
            statName = nameValue;
            iconImage = icon;
            RefreshIcon();
        }

        public void SetValue(float current, float max)
        {
            if (max <= 0) max = 100f; // Prevent division by zero
            float ratio = Mathf.Clamp01(current / max);

            if (fillImage != null)
            {
                if (fillImage.type == Image.Type.Filled)
                {
                    fillImage.fillAmount = ratio;
                }
                else
                {
                    RectTransform rt = fillImage.rectTransform;
                    rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
                }
            }

            if (label != null)
            {
                string prefix = string.IsNullOrEmpty(statName) ? "" : $"{statName}: ";
                label.text = $"{prefix}{current:0}/{max:0}";
            }

            // Debug log to verify updates
            // Debug.Log($"[HUD] {statName} set to {current}/{max} (ratio: {ratio})", this);
        }

        public void SetIcon(string iconId)
        {
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconId))
            {
                iconImage.enabled = false;
                return;
            }

            iconImage.enabled = true;
            Texture2D texture = ApexShiftIconPack.GetIcon(iconId);
            if (texture == null)
            {
                return;
            }

            iconImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 128f);
        }

        private void RefreshIcon()
        {
            if (iconImage != null && !string.IsNullOrWhiteSpace(statName))
            {
                SetIcon(statName.ToLowerInvariant());
            }
        }
    }
}
