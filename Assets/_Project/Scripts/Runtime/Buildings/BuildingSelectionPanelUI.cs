using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class BuildingSelectionPanelUI : MonoBehaviour
    {
        private static BuildingSelectionPanelUI active;

        [SerializeField] private BuildingPlacementRuntime placementRuntime;
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text selectedText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Font font;
        [SerializeField] private int sortingOrder = 5100;

        private readonly List<Button> buttons = new List<Button>();
        private bool isCollapsed = true;
        private GameObject bodyRoot;
        private Text toggleText;

        public void ToggleCollapsed()
        {
            isCollapsed = !isCollapsed;
            ApplyCollapsedState();
        }

        public void SetCollapsed(bool collapsed)
        {
            isCollapsed = collapsed;
            ApplyCollapsedState();
        }

        private void Awake()
        {
            if (active != null && active != this)
            {
                Destroy(gameObject);
                return;
            }

            active = this;
            ResolveReferences();
            BuildIfNeeded();
            Refresh();
        }

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                Destroy(gameObject);
                return;
            }

            active = this;
            ResolveReferences();
            BuildIfNeeded();
            Refresh();
        }

        private void OnDestroy()
        {
            if (active == this)
            {
                active = null;
            }
        }

        private void Update()
        {
            if (placementRuntime == null)
            {
                return;
            }

            if (selectedText != null)
            {
                selectedText.text = placementRuntime.CurrentValidation.isValid
                    ? placementRuntime.GetSelectedHint()
                    : $"{placementRuntime.GetSelectedHint()} - {placementRuntime.CurrentValidation.reason}";
            }
        }

        public void SetPlacementRuntime(BuildingPlacementRuntime runtime)
        {
            placementRuntime = runtime;
            BuildIfNeeded();
            Refresh();
            SetCollapsed(true);
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
            BuildIfNeeded();
            Refresh();
        }

        public void Refresh()
        {
            if (placementRuntime == null)
            {
                return;
            }

            if (selectedText != null)
            {
                selectedText.text = placementRuntime.GetSelectedHint();
            }

            RebuildButtons();
        }

        private void ResolveReferences()
        {
            if (placementRuntime == null)
            {
                placementRuntime = GetComponentInParent<BuildingPlacementRuntime>();
            }

            if (inventoryRuntime == null)
            {
                inventoryRuntime = GetComponentInParent<PlayerInventoryRuntime>();
            }

            if (font == null)
            {
                font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void BuildIfNeeded()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("BuildingSelectionCanvas");
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (panelRoot == null)
            {
                Transform existing = canvas.transform.Find("BuildingSelectionPanel");
                if (existing != null)
                {
                    panelRoot = existing as RectTransform;
                }
            }

            if (panelRoot == null)
            {
                GameObject panelGo = new GameObject("BuildingSelectionPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelGo.transform.SetParent(canvas.transform, false);
                panelRoot = panelGo.GetComponent<RectTransform>();
                panelRoot.anchorMin = new Vector2(0f, 0f);
                panelRoot.anchorMax = new Vector2(0f, 0f);
                panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
                panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                panelRoot.pivot = new Vector2(0.5f, 0.5f);
                panelRoot.anchoredPosition = Vector2.zero;
                panelRoot.sizeDelta = new Vector2(680f, 520f);

                Image bg = panelGo.GetComponent<Image>();
                bg.color = new Color(0.05f, 0.055f, 0.06f, 0.88f);

                titleText = CreateText(panelRoot, "Building — Structures", 24, TextAnchor.MiddleCenter, new Vector2(0f, -28f), new Vector2(-32f, 42f), true);
                toggleText = CreateText(panelRoot, "Close", 14, TextAnchor.MiddleCenter, new Vector2(-54f, -28f), new Vector2(64f, 26f), true);
                Button toggleButton = toggleText.gameObject.AddComponent<Button>();
                toggleButton.targetGraphic = toggleText;
                toggleButton.onClick.AddListener(ToggleCollapsed);
                selectedText = CreateText(panelRoot, "Select building plan.", 15, TextAnchor.MiddleLeft, new Vector2(0f, 26f), new Vector2(-40f, 42f), false);

                GameObject bodyGo = new GameObject("ButtonContainer", typeof(RectTransform), typeof(Image));
                bodyGo.transform.SetParent(panelRoot, false);
                RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
                bodyRt.anchorMin = new Vector2(0f, 0f);
                bodyRt.anchorMax = new Vector2(1f, 1f);
                bodyRt.offsetMin = new Vector2(24f, 72f);
                bodyRt.offsetMax = new Vector2(-24f, -64f);

                Image bodyBg = bodyGo.GetComponent<Image>();
                bodyBg.color = new Color(0f, 0f, 0f, 0f);
                bodyRoot = bodyGo;
                buttonContainer = bodyGo.transform;

            }

            ApplyCollapsedState();
        }

        private void RebuildButtons()
        {
            if (placementRuntime == null || buttonContainer == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(buttons[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(buttons[i].gameObject);
                    }
                }
            }
            buttons.Clear();

            IReadOnlyList<PlaceableDefinition> definitions = placementRuntime.AvailableDefinitions;
            if (definitions == null || definitions.Count == 0)
            {
                selectedText.text = "No building definitions";
                return;
            }

            float y = 0f;
            foreach (PlaceableDefinition definition in definitions.Where(definition => definition != null))
            {
                GameObject buttonGo = CreateDefinitionButton(definition);
                buttonGo.transform.SetParent(buttonContainer, false);
                RectTransform rt = buttonGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(0f, 58f);
                buttons.Add(buttonGo.GetComponent<Button>());
                y -= 66f;
            }
        }

        private GameObject CreateDefinitionButton(PlaceableDefinition definition)
        {
            GameObject buttonGo = new GameObject($"Button_{definition.BuildingId}", typeof(RectTransform), typeof(Image), typeof(Button));
            Image bg = buttonGo.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.23f, 0.18f, 0.98f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => OnDefinitionClicked(definition.BuildingId));

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            Text label = labelGo.GetComponent<Text>();
            label.font = font;
            label.text = BuildDefinitionLabel(definition);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.fontSize = 15;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(14f, 4f);
            labelRt.offsetMax = new Vector2(-14f, -4f);

            return buttonGo;
        }

        private GameObject CreateButton(RectTransform parent, string label, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonGo = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image bg = buttonGo.GetComponent<Image>();
            bg.color = new Color(0.24f, 0.32f, 0.24f, 1f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            Text text = textGo.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return buttonGo;
        }

        private static Text CreateText(Transform parent, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPos, Vector2 sizeDelta, bool bold)
        {
            GameObject go = new GameObject(text, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text uiText = go.GetComponent<Text>();
            uiText.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            uiText.alignment = alignment;
            uiText.color = Color.white;
            uiText.text = text;

            RectTransform rt = uiText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return uiText;
        }

        private void OnDefinitionClicked(string buildingId)
        {
            if (placementRuntime == null)
            {
                return;
            }

            placementRuntime.SelectBuilding(buildingId);
            Refresh();
            SetCollapsed(false);
        }

        private void ApplyCollapsedState()
        {
            if (bodyRoot != null)
            {
                bodyRoot.SetActive(!isCollapsed);
            }

            if (toggleText != null)
            {
                toggleText.text = isCollapsed ? "Build" : "Close";
            }

            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(!isCollapsed);
                panelRoot.sizeDelta = new Vector2(680f, 520f);
            }
        }

        private string BuildDefinitionLabel(PlaceableDefinition definition)
        {
            if (definition == null)
            {
                return "Missing building definition";
            }

            string status = placementRuntime != null && placementRuntime.SelectedBuildingId == definition.BuildingId
                ? "selected"
                : "click to select";

            string requires = FormatBuildRequirements(definition);
            return $"{definition.DisplayName}\nRequires: {requires} — {status}";
        }

        private string FormatBuildRequirements(PlaceableDefinition definition)
        {
            if (definition == null || definition.MaterialCosts == null || definition.MaterialCosts.Count == 0)
            {
                return "free";
            }

            List<string> parts = new List<string>();
            foreach (PlaceableDefinition.PlaceableBuildCost cost in definition.MaterialCosts)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId))
                {
                    continue;
                }

                int owned = inventoryRuntime != null && inventoryRuntime.Inventory != null
                    ? inventoryRuntime.Inventory.GetAmount(cost.ItemId)
                    : 0;

                parts.Add($"{(owned >= cost.Amount ? "✓" : "✗")} {cost.ItemId} {owned}/{cost.Amount}");
            }

            return parts.Count > 0 ? string.Join("; ", parts) : "free";
        }
    }
}
