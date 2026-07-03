using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class ActionBarRuntime : MonoBehaviour
    {
        public static ActionBarRuntime Active { get; private set; }

        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private ApexShift.Runtime.PlayerInput.PlayerInputReader inputReader;
        [SerializeField] private int slotCount = 9;
        [SerializeField] private bool forceEquipOnSlotSelect = true;
        [SerializeField] private bool rejectNonActionItems = true;
        [SerializeField] private bool autoAssignTestItemsOnAwake = true;
        [SerializeField] private bool autoAssignTestItemsInEditMode = false;
        [SerializeField] private Color normalSlotColor = new Color(0.10f, 0.12f, 0.09f, 0.82f);
        [SerializeField] private Color activeSlotColor = new Color(0.86f, 0.74f, 0.20f, 0.96f);
        [SerializeField] private Color emptyActiveSlotColor = new Color(0.48f, 0.42f, 0.14f, 0.96f);
        [SerializeField] private Vector2 activeSlotScale = new Vector2(1.12f, 1.12f);
        [SerializeField] private Color activeOutlineColor = new Color(1f, 0.98f, 0.55f, 1f);

        private readonly string[] assignedItemIds = new string[9];
        private readonly List<SlotView> slotViews = new List<SlotView>();
        private GameObject uiRoot;
        private Canvas canvas;
        private Font font;
        private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private int activeSlotIndex = -1;
        private bool uiBuilt = false;

        public int ActiveSlotIndex => activeSlotIndex;
        public string ActiveItemId => activeSlotIndex >= 0 && activeSlotIndex < assignedItemIds.Length ? assignedItemIds[activeSlotIndex] ?? string.Empty : string.Empty;
        public event Action<int, string> ActiveSlotChanged;

        private void Awake()
        {
            Debug.Log($"[ActionBar] Awake() called on {gameObject.name}", this);
            Active = this;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            
            // Destroy all existing ActionBarUI instances
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in allObjects)
            {
                if (go != null && go.name == "ActionBarUI")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(go);
                    }
                    else
                    {
                        DestroyImmediate(go);
                    }
                }
            }
            
            BuildIfNeeded();
            Refresh();

            if (autoAssignTestItemsOnAwake)
            {
                // Only auto-assign if explicitly enabled via inspector flag
                if (autoAssignTestItemsInEditMode || Application.isPlaying)
                {
                    Debug.Log("[ActionBar] Auto-assigning test items on awake...", this);
                    AssignItemToSlot(0, "spear");
                    AssignItemToSlot(1, "bow");
                    AssignItemToSlot(2, "axe");
                    AssignItemToSlot(3, "pickaxe");
                    AssignItemToSlot(4, "torch");
                }
            }
        }

        private static bool IsInTestMode()
        {
            return false;  // Placeholder - not reliably detectable
        }

        private void OnEnable()
        {
            Active = this;
            Refresh();
            SubscribeToInput();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            if (uiRoot != null)
            {
                Destroy(uiRoot);
            }
        }

        private void Update()
        {
            // Keyboard polling moved to PlayerInputReader.PollActionSlotKeys()
            // ActionBarRuntime now listens to ActionSlotPressed event instead
        }

        private void SubscribeToInput()
        {
            if (inputReader != null)
            {
                inputReader.ActionSlotPressed -= OnActionSlotPressed;
                inputReader.ActionSlotPressed += OnActionSlotPressed;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (inputReader != null)
            {
                inputReader.ActionSlotPressed -= OnActionSlotPressed;
            }
        }

        private void OnActionSlotPressed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 9)
            {
                SetActiveSlot(slotIndex);
                Debug.Log($"[ActionBar] active slot -> {slotIndex + 1} ({ActiveItemId})");
            }
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
            Refresh();
        }

        public void SetInputReader(ApexShift.Runtime.PlayerInput.PlayerInputReader reader)
        {
            UnsubscribeFromInput();
            inputReader = reader;
            SubscribeToInput();
        }

        public bool TryAssignItemAtScreenPosition(string itemId, Vector2 screenPosition)
        {
            string normalized = NormalizeItemId(itemId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (rejectNonActionItems && !IsActionBarItem(normalized))
            {
                Debug.Log($"[ActionBar] rejected non-action item '{normalized}'. Resources should stay in inventory.", this);
                return false;
            }

            for (int i = 0; i < slotViews.Count; i++)
            {
                SlotView view = slotViews[i];
                if (view == null || view.Rect == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(view.Rect, screenPosition, null))
                {
                    Assign(i, normalized);
                    return true;
                }
            }

            return false;
        }

        private void Assign(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= assignedItemIds.Length)
            {
                return;
            }

            string normalized = NormalizeItemId(itemId);
            if (rejectNonActionItems && !IsActionBarItem(normalized))
            {
                Debug.Log($"[ActionBar] rejected non-action item '{normalized}'.", this);
                return;
            }

            assignedItemIds[slotIndex] = normalized;
            SetActiveSlot(slotIndex);
            Refresh();
            Debug.Log($"[ActionBar] assigned and activated {assignedItemIds[slotIndex]} in slot {slotIndex + 1}", this);
        }

        public void SetActiveSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= assignedItemIds.Length)
            {
                if (activeSlotIndex < 0)
                {
                    return;
                }

                activeSlotIndex = -1;
                Refresh();
                NotifyActiveSlotChanged();
                return;
            }

            activeSlotIndex = slotIndex;
            Refresh();
            NotifyActiveSlotChanged();
        }

        public void ClearActiveSlot()
        {
            activeSlotIndex = -1;
            Refresh();
            NotifyActiveSlotChanged();
        }

        public bool IsSlotActive(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < assignedItemIds.Length && activeSlotIndex == slotIndex;
        }

        public string GetAssignedItemInSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < assignedItemIds.Length
                ? assignedItemIds[slotIndex] ?? string.Empty
                : string.Empty;
        }

        public void AssignItemToSlot(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= assignedItemIds.Length)
            {
                Debug.LogWarning($"[ActionBar] invalid slot index {slotIndex}", this);
                return;
            }

            string normalized = NormalizeItemId(itemId);
            if (rejectNonActionItems && !IsActionBarItem(normalized))
            {
                Debug.Log($"[ActionBar] rejected non-action item '{normalized}'.", this);
                return;
            }

            assignedItemIds[slotIndex] = normalized;
            SetActiveSlot(slotIndex);
            Refresh();
            Debug.Log($"[ActionBar] assigned {normalized} to slot {slotIndex + 1}", this);
        }

        private void NotifyActiveSlotChanged()
        {
            string activeItem = ActiveItemId;
            ActiveSlotChanged?.Invoke(activeSlotIndex, activeItem);

            if (forceEquipOnSlotSelect)
            {
                PlayerHeldItemRuntime held = GetComponent<PlayerHeldItemRuntime>();
                if (held == null)
                {
                    held = gameObject.AddComponent<PlayerHeldItemRuntime>();
                }

                held.SetActionBarRuntime(this);
                held.SetInventoryRuntime(inventoryRuntime);
                held.ForceEquipActionItem(activeItem);
            }

            Debug.Log(
                $"[ActionBar] selected slot {activeSlotIndex + 1}; assigned='{GetAssignedItemInSlot(activeSlotIndex)}'; active='{activeItem}'",
                this);
        }

        private void BuildIfNeeded()
        {
            if (uiBuilt && uiRoot != null && slotViews.Count > 0)
            {
                return;
            }

            // Only destroy if we're starting fresh (uiRoot is null)
            if (uiRoot == null)
            {
                GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach (GameObject go in allObjects)
                {
                    if (go != null && go.name == "ActionBarUI")
                    {
                        DestroyImmediate(go);
                    }
                }
            }

            uiBuilt = true;

            if (uiRoot == null)
            {
                uiRoot = new GameObject("ActionBarUI", typeof(RectTransform));
                uiRoot.transform.SetParent(null, false);
                uiRoot.hideFlags = HideFlags.None;
            }

            canvas = uiRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = uiRoot.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 6100;

            if (uiRoot.GetComponent<GraphicRaycaster>() == null)
            {
                uiRoot.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = uiRoot.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Transform existingRoot = uiRoot.transform.Find("ActionBar");
            GameObject root = existingRoot != null ? existingRoot.gameObject : new GameObject("ActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            root.transform.SetParent(uiRoot.transform, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            if (rootRt == null)
            {
                rootRt = root.AddComponent<RectTransform>();
            }
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 42f);
            rootRt.sizeDelta = new Vector2(690f, 58f);

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = root.AddComponent<HorizontalLayoutGroup>();
            }
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            slotViews.Clear();
            for (int childIndex = root.transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                DestroyImmediateSafe(root.transform.GetChild(childIndex).gameObject);
            }

            for (int i = 0; i < Mathf.Min(slotCount, 9); i++)
            {
                slotViews.Add(CreateSlot(root.transform, i));
            }
        }

        private SlotView CreateSlot(Transform parent, int index)
        {
            GameObject slot = new GameObject($"ActionSlot_{index + 1}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(66f, 54f);
            
            // Add LayoutElement so HorizontalLayoutGroup knows the preferred size
            LayoutElement layoutElement = slot.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 66f;
            layoutElement.preferredHeight = 54f;
            
            Image bg = slot.GetComponent<Image>();
            bg.color = normalSlotColor;
            Outline outline = slot.AddComponent<Outline>();
            outline.enabled = false;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.effectColor = activeOutlineColor;

            Text number = CreateText(slot.transform, "Number", (index + 1).ToString(), 11, TextAnchor.UpperLeft);
            RectTransform numberRt = number.GetComponent<RectTransform>();
            numberRt.anchorMin = Vector2.zero;
            numberRt.anchorMax = Vector2.one;
            numberRt.offsetMin = new Vector2(4f, 2f);
            numberRt.offsetMax = new Vector2(-4f, -2f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            Image icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.55f);
            iconRt.anchorMax = new Vector2(0.5f, 0.55f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(30f, 30f);

            Text label = CreateText(slot.transform, "Label", string.Empty, 10, TextAnchor.LowerCenter);
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(2f, 2f);
            labelRt.offsetMax = new Vector2(-2f, -2f);

            Text badge = CreateText(slot.transform, "ActiveBadge", "ACTIVE", 9, TextAnchor.UpperRight);
            RectTransform badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.anchorMin = Vector2.zero;
            badgeRt.anchorMax = Vector2.one;
            badgeRt.offsetMin = new Vector2(2f, 2f);
            badgeRt.offsetMax = new Vector2(-2f, -2f);
            badge.color = activeOutlineColor;
            badge.enabled = false;

            return new SlotView(rt, bg, outline, icon, label, badge);
        }

        private void Refresh()
        {
            BuildIfNeeded();
            for (int i = 0; i < slotViews.Count; i++)
            {
                string itemId = i < assignedItemIds.Length ? assignedItemIds[i] : string.Empty;
                SlotView view = slotViews[i];
                bool isActive = IsSlotActive(i);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    view.Icon.enabled = false;
                    view.Background.color = isActive ? emptyActiveSlotColor : normalSlotColor;
                    view.Outline.enabled = isActive;
                    view.Rect.localScale = isActive ? new Vector3(activeSlotScale.x, activeSlotScale.y, 1f) : Vector3.one;
                    view.Label.text = isActive ? "active" : string.Empty;
                    view.Badge.enabled = isActive;
                    continue;
                }

                view.Icon.sprite = ResolveIcon(itemId);
                view.Icon.enabled = view.Icon.sprite != null;
                view.Background.color = isActive ? activeSlotColor : normalSlotColor;
                view.Outline.enabled = isActive;
                view.Outline.effectColor = activeOutlineColor;
                view.Rect.localScale = isActive ? new Vector3(activeSlotScale.x, activeSlotScale.y, 1f) : Vector3.one;
                view.Label.text = isActive ? $"> {itemId} <" : itemId;
                view.Badge.enabled = isActive;
            }
        }

        private Sprite ResolveIcon(string itemId)
        {
            string normalized = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
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
                "torch" => "ApexShift2D/Art/Icons/Items/item_torch",
                "bow" => "ApexShift2D/Art/Icons/Tools/tool_bow",
                "spear" => "ApexShift2D/Art/Icons/Tools/tool_spear",
                _ => $"ApexShift2D/Art/Icons/Items/item_{normalized}",
            };

            Sprite sprite = UnityEngine.Resources.Load<Sprite>(path) ?? UnityEngine.Resources.Load<Sprite>("ApexShift2D/Art/Icons/Items/item_unknown");
            iconCache[normalized] = sprite;
            return sprite;
        }

        public static bool IsActionBarItem(string itemId)
        {
            switch (NormalizeItemId(itemId))
            {
                case "spear":
                case "bow":
                case "axe":
                case "pickaxe":
                case "torch":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBlockedResourceItem(string itemId)
        {
            switch (NormalizeItemId(itemId))
            {
                case "wood":
                case "stone":
                case "fiber":
                case "meat":
                case "hide":
                case "bone":
                case "berries":
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizeItemId(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
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

        private void CleanupDuplicateActionBars()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in allObjects)
            {
                if (go == null || go.name != "ActionBarUI")
                {
                    continue;
                }

                if (uiRoot != null && go == uiRoot)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }
        }

        private static void DestroyImmediateSafe(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private sealed class SlotView
        {
            public SlotView(RectTransform rect, Image background, Outline outline, Image icon, Text label, Text badge)
            {
                Rect = rect;
                Background = background;
                Outline = outline;
                Icon = icon;
                Label = label;
                Badge = badge;
            }

            public RectTransform Rect { get; }
            public Image Background { get; }
            public Outline Outline { get; }
            public Image Icon { get; }
            public Text Label { get; }
            public Text Badge { get; }
        }
    }
}
