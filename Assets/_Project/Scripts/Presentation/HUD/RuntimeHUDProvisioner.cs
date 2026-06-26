using System.Collections.Generic;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World.Generation;
using ApexShift.Presentation.Icons;
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
            if (generator == null)
            {
                generator = FindAnyObjectByType<WorldGeneratorRuntime>();
            }

            if (generator != null)
            {
                generator.SetGenerateOnStart(false);
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
            if (!Application.isPlaying)
            {
                return;
            }

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
            GameObject existingUI = GameObject.Find("UI");
            if (existingUI != null)
            {
                if (Application.isPlaying) Destroy(existingUI);
                else DestroyImmediate(existingUI);
            }

            GameObject uiRoot = new GameObject("UI");
            uiRoot.SetActive(true);

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

            GameObject menuGo = new GameObject("GameMenu");
            menuGo.transform.SetParent(uiRoot.transform, false);
            Canvas menuCanvas = menuGo.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 200;
            menuGo.AddComponent<GraphicRaycaster>();
            CanvasScaler menuScaler = menuGo.AddComponent<CanvasScaler>();
            menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            menuScaler.referenceResolution = new Vector2(1920, 1080);
            menuScaler.matchWidthOrHeight = GetCanvasMatchWeight();

            GameObject startMenu = CreateMenuPanel(menuGo.transform, "StartMenu", new Vector2(0.5f, 0.5f), new Vector2(980f, 620f), true);
            AddMenuBackdrop(startMenu.transform);
            AddMenuFrame(startMenu.transform);
            AddMenuGradient(startMenu.transform);
            CreateMenuHeroArt(startMenu.transform);
            CreateMenuText(startMenu.transform, "Eyebrow", "SURVIVAL  ISOMETRIC  WORLD", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Vector2(-44, -74), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            GameObject startTitle = CreateMenuText(startMenu.transform, "Title", "Apex Shift", 56, TextAnchor.UpperLeft, new Vector2(42, -74), new Vector2(-42, -142), new Color(0.97f, 0.97f, 0.92f, 1f));
            CreateMenuText(startMenu.transform, "Subtitle", "A low-poly survival sandbox", 20, TextAnchor.UpperLeft, new Vector2(46, -140), new Vector2(-46, -176), new Color(1f, 0.86f, 0.5f, 0.96f));
            GameObject startStatus = CreateMenuText(startMenu.transform, "Status", "Press Continue or start a new run.", 18, TextAnchor.UpperLeft, new Vector2(46, -188), new Vector2(-46, -228), new Color(1f, 0.94f, 0.48f, 1f));
            CreateMenuText(startMenu.transform, "Hint", "Esc opens the pause menu. F1/F4 still expose debug tools.", 14, TextAnchor.UpperLeft, new Vector2(46, -232), new Vector2(-46, -260), new Color(0.88f, 0.92f, 0.88f, 0.95f));
            Button continueButton = CreateMenuButton(startMenu.transform, "ContinueButton", "Continue", new Vector2(52, 330), new Vector2(260, 376));
            Button newGameButton = CreateMenuButton(startMenu.transform, "NewGameButton", "New Game", new Vector2(52, 386), new Vector2(260, 432));
            Button optionsButton = CreateMenuButton(startMenu.transform, "OptionsButton", "Options", new Vector2(52, 442), new Vector2(260, 488));
            Button quitButton = CreateMenuButton(startMenu.transform, "QuitButton", "Quit", new Vector2(52, 498), new Vector2(260, 544));

            GameObject pauseMenu = CreateMenuPanel(menuGo.transform, "PauseMenu", new Vector2(0.5f, 0.5f), new Vector2(900f, 560f), false);
            pauseMenu.SetActive(false);
            AddMenuBackdrop(pauseMenu.transform);
            AddMenuFrame(pauseMenu.transform);
            AddMenuGradient(pauseMenu.transform);
            CreateMenuHeroArt(pauseMenu.transform);
            CreateMenuText(pauseMenu.transform, "Eyebrow", "GAME MENU", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Vector2(-44, -74), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            CreateMenuText(pauseMenu.transform, "Title", "Game Paused", 52, TextAnchor.UpperLeft, new Vector2(42, -74), new Vector2(-42, -140), new Color(0.97f, 0.97f, 0.92f, 1f));
            CreateMenuText(pauseMenu.transform, "Hint", "Press Esc to resume", 18, TextAnchor.UpperLeft, new Vector2(46, -140), new Vector2(-46, -172), new Color(0.92f, 0.92f, 0.92f));
            CreateMenuText(pauseMenu.transform, "Hint2", "Save, load, or adjust settings before returning.", 14, TextAnchor.UpperLeft, new Vector2(46, -176), new Vector2(-46, -206), new Color(0.84f, 0.86f, 0.84f, 0.9f));
            Button resumeButton = CreateMenuButton(pauseMenu.transform, "ResumeButton", "Resume", new Vector2(52, 286), new Vector2(260, 332));
            Button saveButton = CreateMenuButton(pauseMenu.transform, "SaveButton", "Save Game", new Vector2(52, 342), new Vector2(260, 388));
            Button loadButton = CreateMenuButton(pauseMenu.transform, "LoadButton", "Load Game", new Vector2(52, 398), new Vector2(260, 444));
            Button pauseOptionsButton = CreateMenuButton(pauseMenu.transform, "OptionsButton", "Options", new Vector2(52, 454), new Vector2(260, 500));
            Button pauseQuitButton = CreateMenuButton(pauseMenu.transform, "QuitButton", "Quit", new Vector2(52, 510), new Vector2(260, 556));

            GameObject optionsMenu = CreateMenuPanel(menuGo.transform, "OptionsMenu", new Vector2(0.5f, 0.5f), new Vector2(860f, 520f), false);
            optionsMenu.SetActive(false);
            AddMenuBackdrop(optionsMenu.transform);
            AddMenuFrame(optionsMenu.transform);
            AddMenuGradient(optionsMenu.transform);
            CreateMenuHeroArt(optionsMenu.transform);
            CreateMenuText(optionsMenu.transform, "Eyebrow", "OPTIONS", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Vector2(-44, -74), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            CreateMenuText(optionsMenu.transform, "Title", "Settings", 50, TextAnchor.UpperLeft, new Vector2(42, -74), new Vector2(-42, -140), new Color(0.97f, 0.97f, 0.92f, 1f));
            CreateMenuText(optionsMenu.transform, "Hint", "Basic menu page for future settings.", 18, TextAnchor.UpperLeft, new Vector2(46, -140), new Vector2(-46, -172), new Color(0.92f, 0.92f, 0.92f));
            CreateMenuText(optionsMenu.transform, "Hint2", "Use Back to return to the previous menu.", 14, TextAnchor.UpperLeft, new Vector2(46, -176), new Vector2(-46, -206), new Color(0.84f, 0.86f, 0.84f, 0.9f));
            Button backButton = CreateMenuButton(optionsMenu.transform, "BackButton", "Back", new Vector2(52, 438), new Vector2(260, 484));

            CanvasGroup startGroup = startMenu.AddComponent<CanvasGroup>();
            CanvasGroup pauseGroup = pauseMenu.AddComponent<CanvasGroup>();
            CanvasGroup optionsGroup = optionsMenu.AddComponent<CanvasGroup>();
            startGroup.alpha = 0f;
            pauseGroup.alpha = 0f;
            optionsGroup.alpha = 0f;

            startMenu.AddComponent<MenuAmbientMotion>().Configure(18f, 8f, 0.08f, 0.05f);
            pauseMenu.AddComponent<MenuAmbientMotion>().Configure(14f, 7f, 0.06f, 0.04f);
            optionsMenu.AddComponent<MenuAmbientMotion>().Configure(12f, 6f, 0.05f, 0.04f);

            hudController.Configure(
                player.GetComponent<PlayerSurvivalRuntime>(),
                player.GetComponent<PlayerInventoryRuntime>(),
                healthBar, hungerBar, staminaBar, restBar,
                new List<ResourceCounterUI> { woodCounter, stoneCounter, fiberCounter, meatCounter }
            );
            hudController.ConfigureInventorySlots(inventorySlots);

            GameMenuController menuController = uiRoot.AddComponent<GameMenuController>();
            menuController.Configure(
                hudGo,
                startMenu,
                pauseMenu,
                null,
                continueButton,
                newGameButton,
                optionsButton,
                resumeButton,
                quitButton,
                saveButton,
                loadButton,
                backButton,
                startTitle.GetComponent<Text>(),
                startStatus.GetComponent<Text>(),
                startGroup,
                pauseGroup,
                optionsMenu,
                optionsGroup,
                generator,
                player.GetComponent<PlayerInputReader>(),
                FindAnyObjectByType<ApexShift.Runtime.Save.GameSaveService>());

            menuController.ShowStartMenu();
            pauseOptionsButton.onClick.AddListener(() => menuController.ShowOptions());
            pauseQuitButton.onClick.AddListener(() => menuController.QuitToDesktop());

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

        private GameObject CreateMenuPanel(Transform parent, string name, Vector2 anchor, Vector2 size, bool startScreen)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = startScreen ? new Color(0.035f, 0.05f, 0.035f, 0.93f) : new Color(0.025f, 0.035f, 0.03f, 0.92f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return panel;
        }

        private void AddMenuBackdrop(Transform parent)
        {
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parent, false);
            Image bg = backdrop.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.22f);
            RectTransform rt = backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void AddMenuGradient(Transform parent)
        {
            GameObject gradient = new GameObject("Gradient");
            gradient.transform.SetParent(parent, false);
            Image img = gradient.AddComponent<Image>();
            img.color = new Color(0.46f, 0.68f, 0.36f, 0.12f);
            RectTransform rt = gradient.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.45f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            Image glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(0.74f, 0.88f, 0.52f, 0.08f);
            RectTransform glowRt = glow.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.12f, 0.66f);
            glowRt.anchorMax = new Vector2(0.88f, 0.96f);
            glowRt.offsetMin = Vector2.zero;
            glowRt.offsetMax = Vector2.zero;
        }

        private void AddMenuFrame(Transform parent)
        {
            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(parent, false);
            Image img = frame.AddComponent<Image>();
            img.color = new Color(0.2f, 0.28f, 0.2f, 0.95f);
            RectTransform rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(parent, false);
            Image innerImg = inner.AddComponent<Image>();
            innerImg.color = new Color(0.03f, 0.05f, 0.03f, 0.94f);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0f, 0f);
            innerRt.anchorMax = new Vector2(1f, 1f);
            innerRt.offsetMin = new Vector2(6f, 6f);
            innerRt.offsetMax = new Vector2(-6f, -6f);
        }

        private void CreateMenuHeroArt(Transform parent)
        {
            GameObject art = new GameObject("HeroArt");
            art.transform.SetParent(parent, false);
            RectTransform artRt = art.AddComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.55f, 0.08f);
            artRt.anchorMax = new Vector2(0.98f, 0.92f);
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;

            CreateMenuPill(art.transform, "Sky", new Vector2(0.12f, 0.62f), new Vector2(0.9f, 0.95f), new Color(0.55f, 0.76f, 0.4f, 0.1f));
            CreateMenuPill(art.transform, "Ground", new Vector2(0.0f, 0.0f), new Vector2(0.88f, 0.66f), new Color(0.17f, 0.32f, 0.16f, 0.18f));
            CreateMenuPill(art.transform, "Mist", new Vector2(0.06f, 0.22f), new Vector2(0.88f, 0.86f), new Color(0.82f, 0.92f, 0.72f, 0.08f));
        }

        private void CreateMenuPill(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private GameObject CreateMenuText(Transform parent, string name, string text, int size, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.text = text;
            label.alignment = alignment;
            label.fontSize = size;
            label.color = color ?? Color.white;
            if (uiFont != null)
            {
                label.font = uiFont;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private Button CreateMenuButton(Transform parent, string name, string text, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.11f, 0.13f, 0.11f, 1f);
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.12f, 0.15f, 0.12f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.28f, 0.22f, 1f);
            colors.pressedColor = new Color(0.07f, 0.08f, 0.07f, 1f);
            colors.selectedColor = new Color(0.28f, 0.34f, 0.26f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.color = new Color(0.98f, 0.98f, 0.95f, 1f);
            if (uiFont != null)
            {
                label.font = uiFont;
            }

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            GameObject border = new GameObject("Border");
            border.transform.SetParent(go.transform, false);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.56f, 0.72f, 0.45f, 0.36f);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            border.transform.SetAsFirstSibling();
            return button;
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
            indexText.fontSize = 11;
            indexText.color = new Color(0.9f, 0.94f, 0.88f, 0.9f);
            if (uiFont != null) indexText.font = uiFont;
            indexText.text = (slotIndex + 1).ToString();
            RectTransform indexRt = index.GetComponent<RectTransform>();
            indexRt.anchorMin = Vector2.zero;
            indexRt.anchorMax = Vector2.one;
            indexRt.offsetMin = new Vector2(4, 4);
            indexRt.offsetMax = new Vector2(-4, -4);

            InventorySlotUI ui = slot.AddComponent<InventorySlotUI>();
            ui.Configure(iconImg, amountText, indexText);
            return ui;
        }
    }

    public sealed class MenuAmbientMotion : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector2 basePosition;
        private float amplitudeX;
        private float amplitudeY;
        private float speedX;
        private float speedY;

        public void Configure(float amplitudeX, float amplitudeY, float speedX, float speedY)
        {
            this.amplitudeX = amplitudeX;
            this.amplitudeY = amplitudeY;
            this.speedX = speedX;
            this.speedY = speedY;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                basePosition = rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            float t = Time.unscaledTime;
            rectTransform.anchoredPosition = basePosition + new Vector2(
                Mathf.Sin(t * speedX) * amplitudeX,
                Mathf.Cos(t * speedY) * amplitudeY);
        }
    }
}
