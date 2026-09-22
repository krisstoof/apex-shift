using ApexShift.Runtime.Player;
using ApexShift.Presentation.Icons;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class ActionBarView : MonoBehaviour
    {
        [SerializeField] private ItemIconCatalog iconCatalog;
        private ActionBarRuntime actionBar;
        private Transform slotsRoot;

        public int SlotCount => slotsRoot == null ? 0 : slotsRoot.GetComponentsInChildren<ActionBarSlotView>(true).Length;

        public void Bind(ActionBarRuntime runtime, ItemIconCatalog catalog = null)
        {
            Unbind(); actionBar = runtime; if (catalog != null) iconCatalog = catalog; Build();
            ActionBarSlotView[] slots = slotsRoot.GetComponentsInChildren<ActionBarSlotView>(true);
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(actionBar, i, iconCatalog);
        }

        public void Unbind()
        {
            if (slotsRoot == null) return;
            foreach (ActionBarSlotView slot in slotsRoot.GetComponentsInChildren<ActionBarSlotView>(true)) slot.Unbind();
            actionBar = null;
        }

        private void OnDestroy() => Unbind();

        private void Build()
        {
            if (slotsRoot == null)
            {
                GameObject root = new GameObject("ActionBarPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                root.transform.SetParent(transform, false); slotsRoot = root.transform;
                RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = new Vector2(0f, 42f); rect.sizeDelta = new Vector2(690f, 58f);
                HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 8f; layout.childAlignment = TextAnchor.MiddleCenter; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            }
            while (slotsRoot.childCount < 9)
            {
                GameObject slot = new GameObject($"ActionSlot_{slotsRoot.childCount + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(ActionBarSlotView));
                slot.transform.SetParent(slotsRoot, false); slot.GetComponent<RectTransform>().sizeDelta = new Vector2(66f, 54f); LayoutElement element = slot.GetComponent<LayoutElement>(); element.preferredWidth = 66f; element.preferredHeight = 54f;
            }
        }
    }
}
