using ApexShift.Presentation.Icons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class ResourceTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string title;
        [SerializeField] private string itemId;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text tooltipText;

        private string description;

        public void Configure(string itemIdValue, string titleValue, string descriptionValue, GameObject root, Image icon, Text text)
        {
            itemId = itemIdValue;
            title = titleValue;
            description = descriptionValue;
            tooltipRoot = root;
            iconImage = icon;
            tooltipText = text;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetVisible(false);
        }

        private void Awake()
        {
            Apply();
            SetVisible(false);
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            if (tooltipRoot != null)
            {
                RectTransform rootRt = tooltipRoot.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    rootRt.SetAsLastSibling();
                }
            }

            if (iconImage != null && !string.IsNullOrWhiteSpace(itemId))
            {
                Texture2D iconTexture = ApexShiftIconPack.GetIcon(itemId);
                if (iconTexture != null)
                {
                    iconImage.sprite = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f), 128f);
                }
            }

            if (tooltipText != null)
            {
                tooltipText.supportRichText = true;
                tooltipText.text = string.IsNullOrWhiteSpace(description)
                    ? $"<b><size=15>{title}</size></b>"
                    : $"<b><size=15>{title}</size></b>\n<size=11><color=#D8D8D8>{description}</color></size>";
            }
        }

        private void SetVisible(bool visible)
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(visible);
            }

            if (tooltipText != null)
            {
                tooltipText.gameObject.SetActive(visible);
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(visible);
            }
        }
    }
}
