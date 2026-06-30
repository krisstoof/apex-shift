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

        private readonly string[] assignedItemIds = new string[9];
        private readonly List<SlotView> slotViews = new List<SlotView>();
        private GameObject uiRoot;
        private Canvas canvas;
        private Font font;
        private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            Active = this;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            CleanupDuplicateActionBars();
            BuildIfNeeded();
            Refresh();
        }

        private void OnEnable()
        {
            Active = this;
            CleanupDuplicateActionBars();
            BuildIfNeeded();
            Refresh();
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
            if (Keyboard.current == null)
            {
                return;
            }

            for (int i = 0; i < Mathf.Min(slotCount, 9); i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    Debug.Log($"[ActionBar] selected slot {i + 1}: {assignedItemIds[i]}");
                }
            }
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
            Refresh();
        }

        public void SetInputReader(ApexShift.Runtime.PlayerInput.PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public bool TryAssignItemAtScreenPosition(string itemId, Vector2 screenPosition)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
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
                    Assign(i, itemId);
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

            assignedItemIds[slotIndex] = itemId.Trim().ToLowerInvariant();
            Refresh();
            Debug.Log($"[ActionBar] assigned {assignedItemIds[slotIndex]} to slot {slotIndex + 1}");
        }

        private void BuildIfNeeded()
        {
            if (uiRoot != null && canvas != null && slotViews.Count > 0)
            {
                return;
            }

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
            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.09f, 0.82f);

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

            return new SlotView(rt, icon, label);
        }

        private void Refresh()
        {
            BuildIfNeeded();
            for (int i = 0; i < slotViews.Count; i++)
            {
                string itemId = i < assignedItemIds.Length ? assignedItemIds[i] : string.Empty;
                SlotView view = slotViews[i];
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    view.Icon.enabled = false;
                    view.Label.text = string.Empty;
                    continue;
                }

                view.Icon.sprite = ResolveIcon(itemId);
                view.Icon.enabled = view.Icon.sprite != null;
                view.Label.text = itemId;
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
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            public SlotView(RectTransform rect, Image icon, Text label)
            {
                Rect = rect;
                Icon = icon;
                Label = label;
            }

            public RectTransform Rect { get; }
            public Image Icon { get; }
            public Text Label { get; }
        }
    }
}
