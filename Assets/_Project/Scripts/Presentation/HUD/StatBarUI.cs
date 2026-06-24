using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class StatBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text label;
        [SerializeField] private string statName;

        public void SetValue(float current, float max)
        {
            float ratio = Mathf.Clamp01(current / max);

            if (fillImage != null)
            {
                if (fillImage.type == Image.Type.Filled)
                {
                    fillImage.fillAmount = ratio;
                }
                else
                {
                    // Fallback to updating anchors if not using 'Filled' type
                    RectTransform rt = fillImage.rectTransform;
                    rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
                }
            }

            if (label != null)
            {
                string prefix = string.IsNullOrEmpty(statName) ? "" : $"{statName}: ";
                label.text = $"{prefix}{current:0}/{max:0}";
            }
        }
    }
}
