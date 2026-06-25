using System.Collections.Generic;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;
using ApexShift.Presentation.Icons;

namespace ApexShift.Presentation.HUD
{
    public sealed class RuntimeHUDProvisioner : MonoBehaviour
    {
        [SerializeField] private WorldGeneratorRuntime generator;
        [SerializeField] private Font uiFont;

        private void Awake()
        {
            if (generator == null) generator = FindAnyObjectByType<WorldGeneratorRuntime>();
            
            if (generator != null)
            {
                generator.OnGenerationComplete += HandleGenerationComplete;
            }
            
            if (uiFont == null)
            {
                uiFont = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
            }
        }

        private void Start()
        {
            Debug.Log("[HUD] RuntimeHUDProvisioner Start. Playing: " + Application.isPlaying);
            if (Application.isPlaying)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    if (Object.FindAnyObjectByType<PlayerHUDController>() == null)
                    {
                        Debug.Log("[HUD] Player found in Start, provisioning HUD.");
                        CreateHUD(player);
                    }
                    else
                    {
                        Debug.Log("[HUD] PlayerHUD already exists (found via Controller).");
                    }
                }
                else
                {
                    Debug.Log("[HUD] Player not found in Start. Waiting for GenerationComplete event.");
                }
            }
        }

        private void OnDestroy()
{
            if (generator != null)
            {
                generator.OnGenerationComplete -= HandleGenerationComplete;
            }
        }

        private void HandleGenerationComplete(GameObject player)
        {
            CreateHUD(player);
        }

        public void CreateHUD(GameObject player)
        {
            // Cleanup existing HUD if any
            GameObject existingUI = GameObject.Find("UI");
            if (existingUI != null)
            {
                if (Application.isPlaying) Destroy(existingUI);
                else DestroyImmediate(existingUI);
            }

            GameObject uiRoot = new GameObject("UI");

            GameObject hudGo = new GameObject("PlayerHUD");
            hudGo.transform.SetParent(uiRoot.transform, false);
            
            Canvas canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = hudGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = GetCanvasMatchWeight();
            
            hudGo.AddComponent<GraphicRaycaster>();
            
            PlayerHUDController hudController = hudGo.AddComponent<PlayerHUDController>();

            GameObject statsPanel = CreateUIPanel(hudGo.transform, "StatsPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(340, 224), new Vector2(24, -24));
            StatBarUI healthBar = CreateStatBar(statsPanel.transform, "HealthBar", "Health", "health", Color.red, new Vector2(16, 164));
            StatBarUI hungerBar = CreateStatBar(statsPanel.transform, "HungerBar", "Hunger", "hunger", new Color(1f, 0.5f, 0f), new Vector2(16, 122));
            StatBarUI staminaBar = CreateStatBar(statsPanel.transform, "StaminaBar", "Stamina", "stamina", Color.yellow, new Vector2(16, 80));
            StatBarUI restBar = CreateStatBar(statsPanel.transform, "RestBar", "Rest", "rest", Color.blue, new Vector2(16, 38));

            GameObject resourcePanel = CreateUIPanel(hudGo.transform, "ResourcePanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(240, 220), new Vector2(-24, -24));
            ResourceCounterUI woodCounter = CreateResourceCounter(resourcePanel.transform, "WoodCounter", "wood", "Wood", "resource_wood", new Vector2(-16, -16));
            ResourceCounterUI stoneCounter = CreateResourceCounter(resourcePanel.transform, "StoneCounter", "stone", "Stone", "resource_stone", new Vector2(-16, -62));
            ResourceCounterUI fiberCounter = CreateResourceCounter(resourcePanel.transform, "FiberCounter", "fiber", "Fiber", "resource_fiber", new Vector2(-16, -108));
            ResourceCounterUI meatCounter = CreateResourceCounter(resourcePanel.transform, "MeatCounter", "meat", "Meat", "resource_raw_meat", new Vector2(-16, -154));

            GameObject minimapPanel = CreateUIPanel(hudGo.transform, "MiniMapPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 180), new Vector2(-24, -252));
            MiniMapUI minimap = minimapPanel.AddComponent<MiniMapUI>();
            minimap.Configure(player.transform, 140f);

            GameObject fpsPanel = CreateUIPanel(hudGo.transform, "FpsPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(104, 30), new Vector2(0, -16));
            GameObject fpsLabelGo = new GameObject("FpsLabel");
            fpsLabelGo.transform.SetParent(fpsPanel.transform, false);
            Text fpsText = fpsLabelGo.AddComponent<Text>();
            fpsText.text = "FPS 0";
            fpsText.alignment = TextAnchor.MiddleCenter;
            fpsText.fontSize = 16;
            fpsText.color = new Color(1f, 0.92f, 0.42f);
            if (uiFont != null) fpsText.font = uiFont;
            RectTransform fpsLabelRt = fpsLabelGo.GetComponent<RectTransform>();
            fpsLabelRt.anchorMin = Vector2.zero;
            fpsLabelRt.anchorMax = Vector2.one;
            fpsLabelRt.offsetMin = new Vector2(4, 2);
            fpsLabelRt.offsetMax = new Vector2(-4, -2);
            fpsLabelGo.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            fpsLabelGo.AddComponent<FpsCounterUI>();

            GameObject inventoryPanel = CreateUIPanel(hudGo.transform, "InventoryPanel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(332, 44), new Vector2(0, 14));
            List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();
            for (int i = 0; i < 9; i++)
            {
                inventorySlots.Add(CreateInventorySlot(inventoryPanel.transform, $"Slot{i + 1}", i, new Vector2(12 + i * 35, 5)));
            }

            hudController.Configure(
                player.GetComponent<PlayerSurvivalRuntime>(),
                player.GetComponent<PlayerInventoryRuntime>(),
                healthBar, hungerBar, staminaBar, restBar,
                new List<ResourceCounterUI> { woodCounter, stoneCounter, fiberCounter, meatCounter }
            );
            hudController.ConfigureInventorySlots(inventorySlots);

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            es.transform.SetParent(uiRoot.transform, false);
            
            Debug.Log("[HUD] Runtime HUD Provisioned.");
        }

        private GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.03f, 0.32f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return panel;
        }

        private float GetCanvasMatchWeight()
        {
            float aspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
            if (aspect >= 2.0f)
            {
                return 0.7f;
            }

            if (aspect <= 1.5f)
            {
                return 1f;
            }

            return 0.85f;
        }

        private StatBarUI CreateStatBar(Transform parent, string name, string labelText, string iconId, Color color, Vector2 pos)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(296, 36);
            rt.anchoredPosition = pos;

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(bar.transform, false);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.color = Color.white;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(22, 22);
            iconRt.anchoredPosition = new Vector2(10, 0);

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(bar.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.1f, 0.08f, 0.78f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0);
            bgRt.anchorMax = new Vector2(1, 1);
            bgRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = color;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.sizeDelta = Vector2.zero;

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(bar.transform, false);
            Text t = lbl.AddComponent<Text>();
            t.text = labelText;
            if (uiFont != null) t.font = uiFont;
            t.fontSize = 16;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(1, 1);
            lblRt.sizeDelta = new Vector2(-42, 0);
            lblRt.anchoredPosition = new Vector2(32, 0);

            StatBarUI ui = bar.AddComponent<StatBarUI>();
            ui.Configure(fillImg, t, labelText, iconImg);
            ui.SetIcon(iconId);
            return ui;
        }

        private ResourceCounterUI CreateResourceCounter(Transform parent, string name, string itemId, string labelText, string iconId, Vector2 pos)
        {
            GameObject counter = new GameObject(name);
            counter.transform.SetParent(parent, false);
            RectTransform rt = counter.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(168, 44);
            rt.anchoredPosition = pos;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(counter.transform, false);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.color = new Color(0.92f, 0.98f, 0.95f, 1f);
            iconImg.preserveAspect = true;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0);
            iconRt.anchorMax = new Vector2(0, 1);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(22, 22);
            iconRt.anchoredPosition = new Vector2(8, 0);

            GameObject val = new GameObject("Value");
            val.transform.SetParent(counter.transform, false);
            Text tV = val.AddComponent<Text>();
            tV.text = "0";
            if (uiFont != null) tV.font = uiFont;
            tV.fontSize = 16;
            tV.fontStyle = FontStyle.Bold;
            tV.alignment = TextAnchor.MiddleLeft;
            tV.color = new Color(1f, 0.92f, 0.42f);
            tV.supportRichText = false;
            Outline valueOutline = val.AddComponent<Outline>();
            valueOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            valueOutline.effectDistance = new Vector2(1f, -1f);
            RectTransform valRt = val.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(1, 0);
            valRt.anchorMax = new Vector2(1, 1);
            valRt.pivot = new Vector2(1, 0.5f);
            valRt.sizeDelta = new Vector2(52, 24);
            valRt.anchoredPosition = new Vector2(-8, 0);

            ResourceCounterUI ui = counter.AddComponent<ResourceCounterUI>();
            ui.Configure(itemId, iconImg, tV);
            return ui;
        }

        private InventorySlotUI CreateInventorySlot(Transform parent, string name, int slotIndex, Vector2 pos)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            RectTransform rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(32, 32);
            rt.anchoredPosition = pos;

            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.05f, 0.78f);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.color = Color.white;
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4, 4);
            iconRt.offsetMax = new Vector2(-4, -4);

            GameObject amount = new GameObject("Amount");
            amount.transform.SetParent(slot.transform, false);
            Text amountText = amount.AddComponent<Text>();
            amountText.alignment = TextAnchor.LowerRight;
            amountText.fontSize = 14;
            amountText.color = Color.white;
            if (uiFont != null) amountText.font = uiFont;
            RectTransform amountRt = amount.GetComponent<RectTransform>();
            amountRt.anchorMin = Vector2.zero;
            amountRt.anchorMax = Vector2.one;
            amountRt.offsetMin = new Vector2(0, 0);
            amountRt.offsetMax = new Vector2(-3, -1);

            GameObject index = new GameObject("Index");
            index.transform.SetParent(slot.transform, false);
            Text indexText = index.AddComponent<Text>();
            indexText.alignment = TextAnchor.UpperLeft;
            indexText.fontSize = 10;
            indexText.color = new Color(1f, 1f, 1f, 0.7f);
            indexText.text = (slotIndex + 1).ToString();
            if (uiFont != null) indexText.font = uiFont;
            RectTransform indexRt = index.GetComponent<RectTransform>();
            indexRt.anchorMin = Vector2.zero;
            indexRt.anchorMax = Vector2.one;
            indexRt.offsetMin = new Vector2(2, 1);
            indexRt.offsetMax = new Vector2(-2, -2);

            InventorySlotUI ui = slot.AddComponent<InventorySlotUI>();
            ui.Configure(iconImg, amountText, indexText);
            ui.UpdateSlot(slotIndex, string.Empty, 0);
            return ui;
        }
    }
}
