using ApexShift.Runtime.Player;
using ApexShift.Presentation.Icons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class ActionBarSlotView : MonoBehaviour, IActionBarItemDropTarget, IPointerClickHandler
    {
        private ActionBarRuntime actionBar;
        private int slotIndex;
        private Image background;
        private Outline outline;
        private Image icon;
        private Text label;
        private Text badge;
        private ItemIconCatalog catalog;

        public int SlotIndex => slotIndex;
        public string ItemId => actionBar != null ? actionBar.GetAssignedItemInSlot(slotIndex) : string.Empty;

        public void Bind(ActionBarRuntime runtime, int index, ItemIconCatalog iconCatalog)
        {
            Unbind();
            actionBar = runtime;
            slotIndex = index;
            catalog = iconCatalog;
            EnsureVisuals();
            if (actionBar != null) actionBar.StateChanged += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            if (actionBar != null) actionBar.StateChanged -= Refresh;
            actionBar = null;
        }

        public bool TryAssignItem(string itemId) => actionBar != null && actionBar.AssignItemToSlot(slotIndex, itemId);
        public void OnPointerClick(PointerEventData eventData) { if (actionBar != null) actionBar.SetActiveSlot(slotIndex); }

        private void OnDestroy() => Unbind();

        private void EnsureVisuals()
        {
            if (background != null) return;
            background = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            outline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3f, -3f);
            icon = CreateImage("Icon", new Vector2(30f, 30f));
            label = CreateText("Label", 10, TextAnchor.LowerCenter);
            badge = CreateText("ActiveBadge", 9, TextAnchor.UpperRight);
            badge.text = "ACTIVE";
            badge.color = new Color(1f, 0.98f, 0.55f, 1f);
            badge.enabled = false;
        }

        private void Refresh()
        {
            if (background == null || actionBar == null) return;
            bool active = actionBar.IsSlotActive(slotIndex);
            string itemId = actionBar.GetAssignedItemInSlot(slotIndex);
            Color normal = new Color(0.10f, 0.12f, 0.09f, 0.82f);
            Color selected = new Color(0.86f, 0.74f, 0.20f, 0.96f);
            background.color = active ? selected : normal;
            outline.enabled = active;
            badge.enabled = active;
            label.text = string.IsNullOrEmpty(itemId) ? string.Empty : (active ? $"> {itemId} <" : itemId);
            Sprite sprite = null;
            icon.enabled = catalog != null && catalog.TryGetIcon(itemId, out sprite);
            if (icon.enabled) icon.sprite = sprite;
        }

        private Image CreateImage(string name, Vector2 size)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(transform, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.55f); rect.sizeDelta = size;
            Image image = child.GetComponent<Image>(); image.raycastTarget = false; return image;
        }

        private Text CreateText(string name, int fontSize, TextAnchor anchor)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(transform, false);
            RectTransform rect = child.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(3f, 2f); rect.offsetMax = new Vector2(-3f, -2f);
            Text text = child.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.alignment = anchor; text.color = Color.white; text.raycastTarget = false; return text;
        }
    }
}
