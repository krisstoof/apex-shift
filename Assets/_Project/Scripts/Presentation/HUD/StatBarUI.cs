using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class StatBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;
        [SerializeField] private string statName;

        public void Configure(Image fill, Text labelComp, string nameValue)
        {
            fillImage = fill;
            label = labelComp;
            statName = nameValue;
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
}
}
