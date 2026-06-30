using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Player;
using ApexShift.Core.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class StorageTransferPanelUI : MonoBehaviour
    {
        private static StorageTransferPanelUI active;

        private StorageContainerRuntime storage;
        private PlayerInventoryRuntime playerInventory;
        private Canvas canvas;
        private RectTransform panelRoot;
        private RectTransform playerGrid;
        private RectTransform storageGrid;
        private Text titleText;
        private Text playerTitle;
        private Text storageTitle;
        private Font font;
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        private SlotDragSource draggedSource;
        private RectTransform dragLayer;

        public static void Open(StorageContainerRuntime storage, PlayerInventoryRuntime playerInventory)
        {
            if (storage == null || playerInventory == null)
            {
                return;
            }

            if (active == null)
            {
                GameObject go = new GameObject("StorageTransferPanelUI");
                active = go.AddComponent<StorageTransferPanelUI>();
            }

            active.Bind(storage, playerInventory);
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

        private void OnDestroy()
        {
            Unbind();
            if (active == this)
            {
                active = null;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        private void Bind(StorageContainerRuntime storage, PlayerInventoryRuntime playerInventory)
        {
            Unbind();
            this.storage = storage;
            this.playerInventory = playerInventory;
            if (this.storage != null)
            {
                this.storage.StorageChanged += Refresh;
            }

            if (this.playerInventory != null && this.playerInventory.Inventory != null)
            {
                this.playerInventory.Inventory.InventoryChanged += Refresh;
            }
        }

        private void Unbind()
        {
            if (storage != null)
            {
                storage.StorageChanged -= Refresh;
            }

            if (playerInventory != null && playerInventory.Inventory != null)
            {
                playerInventory.Inventory.InventoryChanged -= Refresh;
            }

            storage = null;
            playerInventory = null;
        }

        private void Show()
        {
            BuildIfNeeded();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 6200;
            }

            Refresh();
        }

        private void Close()
        {
            CleanupDragPreview();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            gameObject.SetActive(false);
            Unbind();
        }

        private void Refresh()
        {
            CleanupDragPreview();
            BuildIfNeeded();

            if (titleText != null)
            {
                titleText.text = storage != null ? $"Storage Box  {storage.ContainerId.Substring(0, Mathf.Min(6, storage.ContainerId.Length))}" : "Storage Box";
            }

            if (playerTitle != null && playerInventory != null && playerInventory.Inventory != null)
            {
                playerTitle.text = $"Player inventory  {playerInventory.Inventory.SlotCount - playerInventory.Inventory.GetEmptySlotCount()}/{playerInventory.Inventory.SlotCount}";
            }

            if (storageTitle != null && storage != null && storage.Inventory != null)
            {
                storageTitle.text = $"Box storage  {storage.Inventory.SlotCount - storage.Inventory.GetEmptySlotCount()}/{storage.Inventory.SlotCount}";
            }

            ClearSpawnedObjects();

            if (playerInventory != null && playerInventory.Inventory != null && playerGrid != null)
            {
                BuildInventoryGrid(playerGrid, playerInventory.Inventory, false);
            }

            if (storage != null && storage.Inventory != null && storageGrid != null)
            {
                BuildInventoryGrid(storageGrid, storage.Inventory, true);
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
            canvas.sortingOrder = 6200;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject dragLayerGo = new GameObject("DragLayer", typeof(RectTransform));
            dragLayerGo.transform.SetParent(transform, false);
            dragLayer = dragLayerGo.GetComponent<RectTransform>();
            dragLayer.anchorMin = Vector2.zero;
            dragLayer.anchorMax = Vector2.one;
            dragLayer.offsetMin = Vector2.zero;
            dragLayer.offsetMax = Vector2.zero;

            GameObject panelGo = new GameObject("StoragePanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            panelRoot = panelGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = Vector2.zero;
            panelRoot.sizeDelta = new Vector2(860f, 520f);
            panelGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.06f, 0.92f);

            titleText = CreateText(panelRoot, "Title", "Storage Box", 24, TextAnchor.MiddleLeft, new Vector2(24f, -22f), new Vector2(620f, 34f), true);
            CreateButton(panelRoot, "CloseButton", "Close", new Vector2(92f, 30f), new Vector2(-68f, -22f), Close);

            GameObject left = CreateColumn(panelRoot, "PlayerColumn", new Vector2(-230f, -54f), new Vector2(380f, 380f));
            GameObject right = CreateColumn(panelRoot, "StorageColumn", new Vector2(230f, -54f), new Vector2(430f, 430f));

            playerTitle = CreateText(left.transform, "PlayerTitle", "Player inventory", 16, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(320f, 24f), true);
            storageTitle = CreateText(right.transform, "StorageTitle", "Box storage", 16, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(320f, 24f), true);

            playerGrid = CreateGridRoot(left.transform, "PlayerGrid");
            storageGrid = CreateGridRoot(right.transform, "StorageGrid");
        }

        private GameObject CreateColumn(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.12f, 0.96f);
            return go;
        }

        private RectTransform CreateGridRoot(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(10f, 88f);
            rt.offsetMax = new Vector2(-10f, -52f);

            GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(72f, 72f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(0, 0, 0, 0);

            return rt;
        }

        private void BuildInventoryGrid(RectTransform parent, ApexShift.Core.Inventory.InventoryState inventory, bool isStorage)
        {
            if (parent == null || inventory == null)
            {
                return;
            }

            int slotCount = inventory.SlotCount;
            for (int i = 0; i < slotCount; i++)
            {
                ApexShift.Core.Inventory.InventorySlotSnapshot slot = inventory.PeekSlotStack(i);
                CreateSlot(parent, slot.ItemId, slot.Amount, isStorage);
            }
        }

        private void CreateSlot(Transform parent, string itemId, int amount, bool isStorage)
        {
            bool hasItem = amount > 0 && !string.IsNullOrWhiteSpace(itemId);
            GameObject slot = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SlotDropTarget));
            slot.transform.SetParent(parent, false);
            spawnedObjects.Add(slot);

            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(isStorage ? 0.16f : 0.14f, isStorage ? 0.20f : 0.18f, 0.16f, 0.94f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.55f);
            iconRt.anchorMax = new Vector2(0.5f, 0.55f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(46f, 46f);
            Image icon = iconGo.GetComponent<Image>();
            icon.color = Color.white;
            icon.sprite = ResolveItemIcon(itemId);
            icon.enabled = hasItem && icon.sprite != null;
            icon.raycastTarget = false;

            GameObject amountGo = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            amountGo.transform.SetParent(slot.transform, false);
            Text amountText = amountGo.GetComponent<Text>();
            amountText.font = font;
            amountText.fontSize = 12;
            amountText.alignment = TextAnchor.LowerRight;
            amountText.color = Color.white;
            amountText.text = amount > 0 ? amount.ToString() : string.Empty;
            amountText.raycastTarget = false;
            RectTransform amountRt = amountGo.GetComponent<RectTransform>();
            amountRt.anchorMin = new Vector2(0f, 0f);
            amountRt.anchorMax = new Vector2(1f, 1f);
            amountRt.offsetMin = new Vector2(4f, 4f);
            amountRt.offsetMax = new Vector2(-4f, -4f);

            SlotDragSource source = null;
            if (hasItem)
            {
                source = slot.AddComponent<SlotDragSource>();
                source.Bind(this, isStorage, itemId, amount, icon, amountText);
            }
            SlotDropTarget target = slot.GetComponent<SlotDropTarget>();
            target.Bind(this, isStorage);
        }

        private void ClearSpawnedObjects()
        {
            foreach (GameObject row in spawnedObjects)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            spawnedObjects.Clear();
        }

        private Sprite ResolveItemIcon(string itemId)
        {
            string normalized = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return LoadSprite("ApexShift2D/Art/Icons/Items/item_unknown");
            }

            if (iconCache.TryGetValue(normalized, out Sprite cached))
            {
                return cached;
            }

            string resourcePath = normalized switch
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
                _ => $"ApexShift2D/Art/Icons/Resources/resource_{normalized}",
            };

            Sprite sprite = LoadSprite(resourcePath);
            if (sprite == null)
            {
                sprite = LoadSprite($"ApexShift2D/Art/Icons/Items/item_{normalized}");
            }

            if (sprite == null)
            {
                sprite = LoadSprite("ApexShift2D/Art/Icons/Items/item_unknown");
            }

            iconCache[normalized] = sprite;
            return sprite;
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : UnityEngine.Resources.Load<Sprite>(path);
        }

        private bool TryMoveStack(bool fromStorage, string itemId, int amount)
        {
            if (storage == null || playerInventory == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            if (fromStorage)
            {
                return storage.TryTransferToPlayer(playerInventory, itemId, amount);
            }

            return storage.TryTransferFromPlayer(playerInventory, itemId, amount);
        }

        private bool TryMoveAllStacks(bool fromStorage, string itemId)
        {
            if (storage == null || playerInventory == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            int amount = fromStorage ? storage.Inventory.GetAmount(itemId) : playerInventory.Inventory.GetAmount(itemId);
            return TryMoveStack(fromStorage, itemId, amount);
        }

        private bool TryAcceptDrop(SlotDragSource source, bool targetIsStorage)
        {
            if (source == null || source.IsStorage == targetIsStorage)
            {
                return false;
            }

            return TryMoveAllStacks(source.IsStorage, source.ItemId);
        }

        private void SetDraggedSource(SlotDragSource source)
        {
            if (draggedSource != null && draggedSource != source)
            {
                draggedSource.CancelDragPreview();
            }

            draggedSource = source;
        }

        private void ClearDraggedSource(SlotDragSource source)
        {
            if (draggedSource == source)
            {
                draggedSource = null;
            }
        }

        private void CleanupDragPreview()
        {
            if (draggedSource != null)
            {
                draggedSource.CancelDragPreview();
                draggedSource = null;
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

        private sealed class SlotDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            private StorageTransferPanelUI owner;
            private CanvasGroup canvasGroup;
            private Image iconImage;
            private Text amountText;
            private bool dragging;
            private GameObject dragPreview;
            public bool IsStorage { get; private set; }
            public string ItemId { get; private set; } = string.Empty;
            public int Amount { get; private set; }

            public void Bind(StorageTransferPanelUI owner, bool isStorage, string itemId, int amount, Image icon, Text amountLabel)
            {
                this.owner = owner;
                IsStorage = isStorage;
                ItemId = itemId ?? string.Empty;
                Amount = amount;
                iconImage = icon;
                amountText = amountLabel;
                if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (string.IsNullOrWhiteSpace(ItemId) || Amount <= 0 || owner == null)
                {
                    return;
                }

                owner.SetDraggedSource(this);
                dragging = true;
                if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
                if (iconImage != null) iconImage.color = new Color(1f, 1f, 1f, 0.88f);
                CreateDragPreview(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (!dragging)
                {
                    return;
                }

                UpdateDragPreview(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
                dragging = false;
                if (iconImage != null) iconImage.color = Color.white;
                CancelDragPreview();
                owner?.ClearDraggedSource(this);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (owner == null || eventData == null || eventData.button != PointerEventData.InputButton.Left || string.IsNullOrWhiteSpace(ItemId) || Amount <= 0)
                {
                    return;
                }

                bool shiftHeld = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                owner.TryMoveStack(IsStorage, ItemId, shiftHeld ? Amount : 1);
                owner.Refresh();
            }

            private void DestroyDragPreview()
            {
                if (dragPreview != null)
                {
                    Destroy(dragPreview);
                }

                dragPreview = null;
            }

            private void CreateDragPreview(PointerEventData eventData)
            {
                DestroyDragPreview();
                if (owner == null || iconImage == null || iconImage.sprite == null)
                {
                    return;
                }

                dragPreview = new GameObject("DragPreview", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                dragPreview.transform.SetParent(owner.dragLayer != null ? owner.dragLayer : owner.transform, false);
                dragPreview.transform.SetAsLastSibling();

                RectTransform rect = dragPreview.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(56f, 56f);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image previewImage = dragPreview.GetComponent<Image>();
                previewImage.sprite = iconImage.sprite;
                previewImage.color = Color.white;
                previewImage.raycastTarget = false;

                CanvasGroup group = dragPreview.GetComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;

                UpdateDragPreview(eventData);
            }

            private void UpdateDragPreview(PointerEventData eventData)
            {
                if (dragPreview == null || owner == null || eventData == null)
                {
                    return;
                }

                RectTransform rect = dragPreview.GetComponent<RectTransform>();
                if (rect == null)
                {
                    return;
                }

                Canvas rootCanvas = owner.canvas;
                UnityEngine.Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(owner.dragLayer != null ? owner.dragLayer : owner.transform as RectTransform, eventData.position, cam, out Vector2 localPoint);
                rect.anchoredPosition = localPoint;
            }

            public void CancelDragPreview()
            {
                dragging = false;
                if (iconImage != null)
                {
                    iconImage.color = Color.white;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                }

                DestroyDragPreview();
            }
        }

        private sealed class SlotDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
        {
            private StorageTransferPanelUI owner;
            private bool targetIsStorage;
            private Image image;
            private Color baseColor;
            private Color hoverColor;

            public void Bind(StorageTransferPanelUI owner, bool targetIsStorage)
            {
                this.owner = owner;
                this.targetIsStorage = targetIsStorage;
                image = GetComponent<Image>();
                baseColor = image != null ? image.color : Color.white;
                hoverColor = Color.Lerp(baseColor, Color.white, 0.18f);
            }

            public void OnDrop(PointerEventData eventData)
            {
                if (owner == null || eventData == null)
                {
                    return;
                }

                GameObject dragged = eventData.pointerDrag;
                if (dragged == null)
                {
                    return;
                }

                SlotDragSource source = dragged.GetComponent<SlotDragSource>();
                if (source == null)
                {
                    return;
                }

                if (owner.TryAcceptDrop(source, targetIsStorage))
                {
                    owner.Refresh();
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (image != null)
                {
                    image.color = hoverColor;
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (image != null)
                {
                    image.color = baseColor;
                }
            }
        }
    }
}
