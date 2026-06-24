using System.Collections.Generic;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

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
            
            hudGo.AddComponent<GraphicRaycaster>();
            
            PlayerHUDController hudController = hudGo.AddComponent<PlayerHUDController>();

            GameObject statsPanel = CreateUIPanel(hudGo.transform, "StatsPanel", new Vector2(0, 0), new Vector2(0, 0), new Vector2(250, 200), new Vector2(30, 30));
            StatBarUI healthBar = CreateStatBar(statsPanel.transform, "HealthBar", "Health", Color.red, new Vector2(0, 150));
            StatBarUI hungerBar = CreateStatBar(statsPanel.transform, "HungerBar", "Hunger", new Color(1f, 0.5f, 0f), new Vector2(0, 110));
            StatBarUI staminaBar = CreateStatBar(statsPanel.transform, "StaminaBar", "Stamina", Color.yellow, new Vector2(0, 70));
            StatBarUI restBar = CreateStatBar(statsPanel.transform, "RestBar", "Rest", Color.blue, new Vector2(0, 30));

            GameObject resourcePanel = CreateUIPanel(hudGo.transform, "ResourcePanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(220, 180), new Vector2(-30, -30));
            ResourceCounterUI woodCounter = CreateResourceCounter(resourcePanel.transform, "WoodCounter", "wood", "Wood", new Vector2(0, 0));
            ResourceCounterUI stoneCounter = CreateResourceCounter(resourcePanel.transform, "StoneCounter", "stone", "Stone", new Vector2(0, -40));
            ResourceCounterUI fiberCounter = CreateResourceCounter(resourcePanel.transform, "FiberCounter", "fiber", "Fiber", new Vector2(0, -80));

            hudController.Configure(
                player.GetComponent<PlayerSurvivalRuntime>(),
                player.GetComponent<PlayerInventoryRuntime>(),
                healthBar, hungerBar, staminaBar, restBar,
                new List<ResourceCounterUI> { woodCounter, stoneCounter, fiberCounter }
            );

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
            bg.color = new Color(0, 0, 0, 0.35f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return panel;
        }

        private StatBarUI CreateStatBar(Transform parent, string name, string labelText, Color color, Vector2 pos)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(200, 30);
            rt.anchoredPosition = pos;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(bar.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = color;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(bar.transform, false);
            Text t = lbl.AddComponent<Text>();
            t.text = labelText;
            if (uiFont != null) t.font = uiFont;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = new Vector2(-10, 0);
            lblRt.anchoredPosition = new Vector2(10, 0);

            StatBarUI ui = bar.AddComponent<StatBarUI>();
            ui.Configure(fillImg, t, labelText);
            return ui;
        }

        private ResourceCounterUI CreateResourceCounter(Transform parent, string name, string itemId, string labelText, Vector2 pos)
        {
            GameObject counter = new GameObject(name);
            counter.transform.SetParent(parent, false);
            RectTransform rt = counter.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 35);
            rt.anchoredPosition = pos;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(counter.transform, false);
            Text tL = lbl.AddComponent<Text>();
            tL.text = labelText + ":";
            if (uiFont != null) tL.font = uiFont;
            tL.fontSize = 18;
            tL.alignment = TextAnchor.MiddleRight;
            tL.color = Color.white;
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.7f, 1);
            lblRt.sizeDelta = Vector2.zero;

            GameObject val = new GameObject("Value");
            val.transform.SetParent(counter.transform, false);
            Text tV = val.AddComponent<Text>();
            tV.text = "0";
            if (uiFont != null) tV.font = uiFont;
            tV.fontSize = 18;
            tV.fontStyle = FontStyle.Bold;
            tV.alignment = TextAnchor.MiddleLeft;
            tV.color = Color.yellow;
            RectTransform valRt = val.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.75f, 0);
            valRt.anchorMax = new Vector2(1, 1);
            valRt.sizeDelta = Vector2.zero;

            ResourceCounterUI ui = counter.AddComponent<ResourceCounterUI>();
            ui.Configure(itemId, tV);
            return ui;
        }
    }
}
