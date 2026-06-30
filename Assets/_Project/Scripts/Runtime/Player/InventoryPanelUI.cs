using System.Collections.Generic;
using ApexShift.Core.Inventory;
using ApexShift.Runtime.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ApexShift.Runtime.Player
{
    public sealed class InventoryPanelUI : MonoBehaviour
    {
        private static InventoryPanelUI active;
        private PlayerInventoryRuntime inventoryRuntime;
        private Canvas canvas;
        private RectTransform gridRoot;
        private Text titleText;
        private Font font;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        public static void Open(PlayerInventoryRuntime inventoryRuntime)
        {
            if (inventoryRuntime == null)
            {
                return;
            }

            if (active == null)
            {
                GameObject go = new GameObject("InventoryPanelUI");
                active = go.AddComponent<InventoryPanelUI>();
            }

            active.inventoryRuntime = inventoryRuntime;
            active.Show();
        }

        private void Awake()
        {
            if (active != null && active != this)
            {
                Destroy(gameObject);
                return;
            }

            active = this;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildIfNeeded();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (active == this)
            {
                active = null;
            }
        }

        private void Show()
        {
            BuildIfNeeded();
            gameObject.SetActive(true);
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 6201;
            }
            Refresh();
        }

        private void Close()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            BuildIfNeeded();
            ClearChildren();
            if (titleText != null && inventoryRuntime != null && inventoryRuntime.Inventory != null)
            {
                InventoryState inv = inventoryRuntime.Inventory;
                titleText.text = $"Inventory  {inv.SlotCount - inv.GetEmptySlotCount()}/{inv.SlotCount}";
                for (int i = 0; i < inv.SlotCount; i++)
                {
                    InventorySlotSnapshot slot = inv.PeekSlotStack(i);
                    CreateSlot(gridRoot, slot.ItemId, slot.Amount);
                }
            }
        }

        private void BuildIfNeeded()
        {
            if (canvas != null)
            {
                return;
            }

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 380f);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.06f, 0.92f);

            titleText = CreateText(rt, "Title", "Inventory", 24, TextAnchor.MiddleLeft, new Vector2(18f, -18f), new Vector2(300f, 30f), true);
            CreateButton(rt, "CloseButton", "Close", new Vector2(92f, 30f), new Vector2(-52f, -18f), Close);

            GameObject gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(rt, false);
            gridRoot = gridGo.GetComponent<RectTransform>();
            gridRoot.anchorMin = new Vector2(0f, 0f);
            gridRoot.anchorMax = new Vector2(1f, 1f);
            gridRoot.offsetMin = new Vector2(18f, 18f);
            gridRoot.offsetMax = new Vector2(-18f, -60f);

            GridLayoutGroup grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(92f, 92f);
            grid.spacing = new Vector2(8f, 8f);
        }

        private void CreateSlot(Transform parent, string itemId, int amount)
        {
            bool hasItem = amount > 0 && !string.IsNullOrWhiteSpace(itemId);
            GameObject slot = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SlotClickHandler), typeof(SlotDragHandler));
            slot.transform.SetParent(parent, false);
            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.20f, 0.16f, 0.95f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.55f);
            iconRt.anchorMax = new Vector2(0.5f, 0.55f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(48f, 48f);
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = ResolveIcon(itemId);
            icon.enabled = hasItem && icon.sprite != null;
            icon.raycastTarget = false;

            GameObject amountGo = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            amountGo.transform.SetParent(slot.transform, false);
            Text amountText = amountGo.GetComponent<Text>();
            amountText.font = font;
            amountText.fontSize = 14;
            amountText.alignment = TextAnchor.LowerRight;
            amountText.color = Color.white;
            amountText.text = hasItem ? amount.ToString() : string.Empty;
            amountText.raycastTarget = false;
            RectTransform amountRt = amountGo.GetComponent<RectTransform>();
            amountRt.anchorMin = new Vector2(0f, 0f);
            amountRt.anchorMax = new Vector2(1f, 1f);
            amountRt.offsetMin = new Vector2(4f, 4f);
            amountRt.offsetMax = new Vector2(-4f, -4f);

            slot.GetComponent<SlotClickHandler>().Bind(this, itemId, amount, icon, amountText);
            slot.GetComponent<SlotDragHandler>().Bind(this, itemId, amount, icon);
        }

        private Sprite ResolveIcon(string itemId)
        {
            string normalized = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return UnityEngine.Resources.Load<Sprite>("ApexShift2D/Art/Icons/Items/item_unknown");
            }

            if (iconCache.TryGetValue(normalized, out Sprite cached))
            {
                return cached;
            }

            string path = normalized switch
            {
                "wood" => "ApexShift2D/Art/Icons/Resources/resource_wood_log",
                "stone" => "ApexShift2D/Art/Icons/Resources/resource_stone",
                "fiber" => "ApexShift2D/Art/Icons/Resources/resource_fiber",
                "meat" => "ApexShift2D/Art/Icons/Resources/resource_raw_meat",
                "hide" => "ApexShift2D/Art/Icons/Resources/resource_hide",
                "bone" => "ApexShift2D/Art/Icons/Resources/resource_bone",
                "berries" => "ApexShift2D/Art/Icons/Resources/resource_berries",
                "grass" => "ApexShift2D/Art/Icons/Resources/resource_leaf",
                "torch" => "ApexShift2D/Art/Icons/Items/item_torch",
                "storage_box" => "ApexShift2D/Art/Icons/Items/item_storage_box",
                "campfire" => "ApexShift2D/Art/Icons/Items/item_campfire",
                "bow" => "ApexShift2D/Art/Icons/Tools/tool_bow",
                "spear" => "ApexShift2D/Art/Icons/Tools/tool_spear",
                _ => $"ApexShift2D/Art/Icons/Items/item_{normalized}",
            };

            Sprite sprite = UnityEngine.Resources.Load<Sprite>(path) ?? UnityEngine.Resources.Load<Sprite>("ApexShift2D/Art/Icons/Items/item_unknown");
            iconCache[normalized] = sprite;
            return sprite;
        }

        private void ClearChildren()
        {
            if (gridRoot == null)
            {
                return;
            }

            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, bool bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.24f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText(go.transform, "Label", text, 13, TextAnchor.MiddleCenter, Vector2.zero, size, false);
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        public void DropItem(string itemId, int amount)
        {
            if (inventoryRuntime == null || inventoryRuntime.Inventory == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return;
            }

            int removed = inventoryRuntime.Inventory.RemoveItemById(itemId, amount);
            if (removed <= 0)
            {
                return;
            }

            Transform spawnOrigin = inventoryRuntime.transform;
            Vector3 forward = spawnOrigin != null ? spawnOrigin.forward : Vector3.forward;
            Vector3 right = spawnOrigin != null ? spawnOrigin.right : Vector3.right;
            Vector2 spread = Random.insideUnitCircle * 0.25f;
            Vector3 spawnPos = (spawnOrigin != null ? spawnOrigin.position : Vector3.zero)
                + forward * 1.2f
                + right * spread.x
                + Vector3.up * (0.2f + Mathf.Abs(spread.y));
            ItemPickupSpawner.Spawn(itemId, removed, spawnPos, Quaternion.identity);
            Refresh();
        }

        private sealed class SlotClickHandler : MonoBehaviour, IPointerClickHandler
        {
            private InventoryPanelUI owner;
            private string itemId;
            private int amount;
            private Image icon;
            private Text amountText;

            public void Bind(InventoryPanelUI owner, string itemId, int amount, Image icon, Text amountText)
            {
                this.owner = owner;
                this.itemId = itemId ?? string.Empty;
                this.amount = amount;
                this.icon = icon;
                this.amountText = amountText;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (owner == null || eventData == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                {
                    return;
                }

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    owner.DropItem(itemId, amount);
                }
            }
        }

        private sealed class SlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private InventoryPanelUI owner;
            private string itemId;
            private int amount;
            private Image sourceIcon;
            private CanvasGroup canvasGroup;
            private GameObject dragGhost;

            public void Bind(InventoryPanelUI owner, string itemId, int amount, Image sourceIcon)
            {
                this.owner = owner;
                this.itemId = itemId ?? string.Empty;
                this.amount = amount;
                this.sourceIcon = sourceIcon;
                canvasGroup = GetComponent<CanvasGroup>();
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (owner == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0 || sourceIcon == null || sourceIcon.sprite == null)
                {
                    return;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.alpha = 0.72f;
                }

                dragGhost = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas ghostCanvas = dragGhost.GetComponent<Canvas>();
                ghostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ghostCanvas.sortingOrder = 7000;

                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(dragGhost.transform, false);
                Image icon = iconGo.GetComponent<Image>();
                icon.sprite = sourceIcon.sprite;
                icon.raycastTarget = false;
                RectTransform rect = iconGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(48f, 48f);
                rect.position = eventData.position;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (dragGhost == null || eventData == null)
                {
                    return;
                }

                RectTransform rect = dragGhost.GetComponentInChildren<RectTransform>();
                if (rect != null)
                {
                    rect.position = eventData.position;
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.alpha = 1f;
                }

                if (dragGhost != null)
                {
                    Destroy(dragGhost);
                    dragGhost = null;
                }

                if (eventData == null || string.IsNullOrWhiteSpace(itemId))
                {
                    return;
                }

                ActionBarRuntime actionBar = ActionBarRuntime.Active;
                if (actionBar != null && actionBar.TryAssignItemAtScreenPosition(itemId, eventData.position))
                {
                    Debug.Log($"[Inventory] assigned {itemId} to action bar");
                }
            }
        }
    }
}
