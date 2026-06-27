using System.Collections.Generic;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.UI;
using ApexShift.Presentation.Icons;
using ApexShift.Runtime.Debugging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
                generator.SetGenerateOnStart(false);
                generator.OnGenerationComplete += HandleGenerationComplete;
            }
            if (uiFont == null) uiFont = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            CreateHUD(null);
        }

        private void OnDestroy()
        {
            if (generator != null) generator.OnGenerationComplete -= HandleGenerationComplete;
        }

        private void HandleGenerationComplete(GameObject player) => CreateHUD(player);

        public void CreateHUD(GameObject player)
        {
            GameObject uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("UI");
                uiRoot.transform.position = Vector3.zero;
                uiRoot.transform.rotation = Quaternion.identity;
                uiRoot.transform.localScale = Vector3.one;
            }

            GameObject hudGo = uiRoot.transform.Find("PlayerHUD")?.gameObject;
            if (hudGo == null)
            {
                hudGo = new GameObject("PlayerHUD");
                hudGo.transform.SetParent(uiRoot.transform, false);
                RectTransform rt = hudGo.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                hudGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                hudGo.GetComponent<Canvas>().sortingOrder = 100;
                hudGo.AddComponent<GraphicRaycaster>();
            }
            
            CanvasScaler hudScaler = hudGo.GetComponent<CanvasScaler>() ?? hudGo.AddComponent<CanvasScaler>();
            hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution = new Vector2(1920, 1080);
            hudScaler.matchWidthOrHeight = 0.5f;

            GameObject menuGo = uiRoot.transform.Find("GameMenu")?.gameObject;
            if (menuGo == null)
            {
                menuGo = new GameObject("GameMenu");
                menuGo.transform.SetParent(uiRoot.transform, false);
                RectTransform rt = menuGo.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                menuGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                menuGo.GetComponent<Canvas>().sortingOrder = 200;
                menuGo.AddComponent<GraphicRaycaster>();
            }
            
            CanvasScaler menuScaler = menuGo.GetComponent<CanvasScaler>() ?? menuGo.AddComponent<CanvasScaler>();
            menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            menuScaler.referenceResolution = new Vector2(1920, 1080);
            menuScaler.matchWidthOrHeight = 0.5f;

            // CLEANUP
            foreach (Transform t in hudGo.transform) 
                if (t.name.EndsWith("Panel")) DestroyImmediate(t.gameObject);
            foreach (Transform t in menuGo.transform) 
                DestroyImmediate(t.gameObject);

            PlayerHUDController hudController = hudGo.GetComponent<PlayerHUDController>() ?? hudGo.AddComponent<PlayerHUDController>();

            // Group 1: Stats (Top Left) - LARGER
            GameObject statsPanel = CreateUIPanel(hudGo.transform, "StatsPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, 150), new Vector2(40, -40));
            StatBarUI healthBar = CreateStatBar(statsPanel.transform, "HealthBar", "Health", "health", Color.red, new Vector2(12, -15));
            StatBarUI hungerBar = CreateStatBar(statsPanel.transform, "HungerBar", "Hunger", "hunger", new Color(1f, 0.5f, 0f), new Vector2(12, -47));
            StatBarUI staminaBar = CreateStatBar(statsPanel.transform, "StaminaBar", "Stamina", "stamina", Color.yellow, new Vector2(12, -79));
            StatBarUI restBar = CreateStatBar(statsPanel.transform, "RestBar", "Rest", "rest", Color.blue, new Vector2(12, -111));

            // Group 2: Resources (Top Right) - LARGER
            GameObject resourcePanel = CreateUIPanel(hudGo.transform, "ResourcePanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 155), new Vector2(-40, -40));
            ResourceCounterUI woodCounter = CreateResourceCounter(resourcePanel.transform, "WoodCounter", "wood", "Wood", "resource_wood", new Vector2(-12, -12));
            ResourceCounterUI stoneCounter = CreateResourceCounter(resourcePanel.transform, "StoneCounter", "stone", "Stone", "resource_stone", new Vector2(-12, -46));
            ResourceCounterUI fiberCounter = CreateResourceCounter(resourcePanel.transform, "FiberCounter", "fiber", "Fiber", "resource_fiber", new Vector2(-12, -80));
            ResourceCounterUI meatCounter = CreateResourceCounter(resourcePanel.transform, "MeatCounter", "meat", "Meat", "resource_raw_meat", new Vector2(-12, -114));

            // Group 3: Minimap (Bottom Right)
            GameObject minimapPanel = CreateUIPanel(hudGo.transform, "MiniMapPanel", new Vector2(1, 0), new Vector2(1, 0), new Vector2(165, 165), new Vector2(-40, 40));
            if (player != null)
            {
                MiniMapUI minimap = minimapPanel.AddComponent<MiniMapUI>();
                minimap.Configure(player.transform, 110f);
            }
            else minimapPanel.SetActive(false);

            // Group 4: FPS (Bottom Left)
            GameObject fpsPanel = CreateUIPanel(hudGo.transform, "FpsPanel", new Vector2(0, 0), new Vector2(0, 0), new Vector2(100, 30), new Vector2(20, 20));
            GameObject fpsLabelGo = CreateMenuText(fpsPanel.transform, "FpsLabel", "FPS 0", 12, TextAnchor.MiddleCenter, Vector2.zero, new Color(1f, 0.92f, 0.42f));
            RectTransform fpsRt = fpsLabelGo.GetComponent<RectTransform>();
            fpsRt.anchorMin = Vector2.zero; fpsRt.anchorMax = Vector2.one; 
            fpsRt.pivot = new Vector2(0.5f, 0.5f);
            fpsRt.offsetMin = Vector2.zero; fpsRt.offsetMax = Vector2.zero;
            fpsRt.anchoredPosition = Vector2.zero;
            fpsLabelGo.AddComponent<FpsCounterUI>();

            // Group 5: Inventory (Bottom Center)
            GameObject inventoryPanel = CreateUIPanel(hudGo.transform, "InventoryPanel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(380, 50), new Vector2(0, 40));
            HorizontalLayoutGroup hlg = inventoryPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();
            for (int i = 0; i < 9; i++) 
                inventorySlots.Add(CreateInventorySlot(inventoryPanel.transform, $"Slot{i + 1}", i));

            // Menus setup remains similar but scaled
            GameObject startMenu = CreateMenuPanel(menuGo.transform, "StartMenu", new Vector2(0.5f, 0.5f), new Vector2(980f, 620f), true);
            startMenu.SetActive(false); // Ensure starts hidden
            CreateMenuBackdropFrame(startMenu.transform);
            AddMenuBackdrop(startMenu.transform);
            AddMenuFrame(startMenu.transform);
            AddMenuGradient(startMenu.transform);
            CreateMenuTitlePlate(startMenu.transform);
            CreateMenuHeroArt(startMenu.transform);
            CreateMenuText(startMenu.transform, "Eyebrow", "SURVIVAL  ISOMETRIC  WORLD", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            GameObject startTitle = CreateMenuText(startMenu.transform, "Title", "Apex Shift", 58, TextAnchor.UpperLeft, new Vector2(42, -82), new Color(0.98f, 0.98f, 0.93f, 1f));
            startTitle.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            CreateMenuText(startMenu.transform, "Subtitle", "A low-poly survival sandbox", 22, TextAnchor.UpperLeft, new Vector2(46, -146), new Color(1f, 0.88f, 0.54f, 0.98f));
            GameObject startStatus = CreateMenuText(startMenu.transform, "Status", "Press Continue or start a new run.", 18, TextAnchor.UpperLeft, new Vector2(46, -194), new Color(1f, 0.94f, 0.48f, 1f));
            startStatus.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            CreateMenuText(startMenu.transform, "Hint", "Esc opens the pause menu. F1/F4 still expose debug tools.", 14, TextAnchor.UpperLeft, new Vector2(46, -234), new Color(0.88f, 0.92f, 0.88f, 0.95f));
            Button continueButton = CreateMenuButton(startMenu.transform, "ContinueButton", "Continue", new Vector2(40, -330));
            Button newGameButton = CreateMenuButton(startMenu.transform, "NewGameButton", "New Game", new Vector2(40, -386));
            Button optionsButton = CreateMenuButton(startMenu.transform, "OptionsButton", "Options", new Vector2(40, -442));
            Button quitButton = CreateMenuButton(startMenu.transform, "QuitButton", "Quit", new Vector2(40, -498));

            GameObject pauseMenu = CreateMenuPanel(menuGo.transform, "PauseMenu", new Vector2(0.5f, 0.5f), new Vector2(900f, 560f), false);
            pauseMenu.SetActive(false); // Ensure starts hidden
            CreateMenuBackdropFrame(pauseMenu.transform);
            AddMenuBackdrop(pauseMenu.transform);
            AddMenuFrame(pauseMenu.transform);
            AddMenuGradient(pauseMenu.transform);
            CreateMenuHeroArt(pauseMenu.transform);
            CreateMenuText(pauseMenu.transform, "Eyebrow", "GAME MENU", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            GameObject pauseTitle = CreateMenuText(pauseMenu.transform, "Title", "Game Paused", 54, TextAnchor.UpperLeft, new Vector2(42, -82), new Color(0.98f, 0.98f, 0.93f, 1f));
            pauseTitle.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            CreateMenuText(pauseMenu.transform, "Hint", "Press Esc to resume", 18, TextAnchor.UpperLeft, new Vector2(46, -144), new Color(0.92f, 0.92f, 0.92f));
            CreateMenuText(pauseMenu.transform, "Hint2", "Save, load, or adjust settings before returning.", 14, TextAnchor.UpperLeft, new Vector2(46, -180), new Color(0.84f, 0.86f, 0.84f, 0.9f));
            Button resumeButton = CreateMenuButton(pauseMenu.transform, "ResumeButton", "Resume", new Vector2(40, -286));
            Button saveButton = CreateMenuButton(pauseMenu.transform, "SaveButton", "Save Game", new Vector2(40, -342));
            Button loadButton = CreateMenuButton(pauseMenu.transform, "LoadButton", "Load Game", new Vector2(40, -398));
            Button pauseOptionsButton = CreateMenuButton(pauseMenu.transform, "OptionsButton", "Options", new Vector2(40, -454));
            Button pauseQuitButton = CreateMenuButton(pauseMenu.transform, "QuitButton", "Quit", new Vector2(40, -510));

            GameObject optionsMenu = CreateMenuPanel(menuGo.transform, "OptionsMenu", new Vector2(0.5f, 0.5f), new Vector2(860f, 520f), false);
            optionsMenu.SetActive(false); // Ensure starts hidden
CreateMenuBackdropFrame(optionsMenu.transform);
            AddMenuBackdrop(optionsMenu.transform);
            AddMenuFrame(optionsMenu.transform);
            AddMenuGradient(optionsMenu.transform);
            CreateMenuHeroArt(optionsMenu.transform);
            CreateMenuText(optionsMenu.transform, "Eyebrow", "OPTIONS", 14, TextAnchor.UpperLeft, new Vector2(44, -38), new Color(0.74f, 0.9f, 0.74f, 0.95f));
            GameObject optionsTitle = CreateMenuText(optionsMenu.transform, "Title", "Settings", 52, TextAnchor.UpperLeft, new Vector2(42, -82), new Color(0.98f, 0.98f, 0.93f, 1f));
            optionsTitle.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            CreateMenuText(optionsMenu.transform, "Hint", "Basic menu page for future settings.", 18, TextAnchor.UpperLeft, new Vector2(46, -144), new Color(0.92f, 0.92f, 0.92f));
            CreateMenuText(optionsMenu.transform, "Hint2", "Use Back to return to the previous menu.", 14, TextAnchor.UpperLeft, new Vector2(46, -180), new Color(0.84f, 0.86f, 0.84f, 0.9f));
            Button backButton = CreateMenuButton(optionsMenu.transform, "BackButton", "Back", new Vector2(40, -438));

            CanvasGroup startGrp = startMenu.AddComponent<CanvasGroup>();
            CanvasGroup pauseGrp = pauseMenu.AddComponent<CanvasGroup>();
            CanvasGroup optionsGrp = optionsMenu.AddComponent<CanvasGroup>();

            if (player != null)
            {
                hudController.Configure(player.GetComponent<PlayerSurvivalRuntime>(), player.GetComponent<PlayerInventoryRuntime>(), healthBar, hungerBar, staminaBar, restBar,
                    new List<ResourceCounterUI> { woodCounter, stoneCounter, fiberCounter, meatCounter });
                hudController.ConfigureInventorySlots(inventorySlots);
                hudGo.SetActive(true);
            }
            else hudGo.SetActive(false);

            GameObject esGo = uiRoot.transform.Find("EventSystem")?.gameObject ?? new GameObject("EventSystem");
            esGo.transform.SetParent(uiRoot.transform, false);
            esGo.transform.localPosition = Vector3.zero;
            EventSystem es = esGo.GetComponent<EventSystem>() ?? esGo.AddComponent<EventSystem>();
            InputSystemUIInputModule uiInput = esGo.GetComponent<InputSystemUIInputModule>() ?? esGo.AddComponent<InputSystemUIInputModule>();

            InputActionAsset actions = (generator != null && generator.InputActions != null) ? generator.InputActions : InputSystem.actions;
            if (actions != null)
            {
                actions.Enable();
                uiInput.actionsAsset = actions;
                var p = actions.FindAction("UI/Point") ?? actions.FindAction("Point");
                var c = actions.FindAction("UI/Click") ?? actions.FindAction("Click");
                var n = actions.FindAction("UI/Navigate") ?? actions.FindAction("Navigate");
                var s = actions.FindAction("UI/Submit") ?? actions.FindAction("Submit");
                var x = actions.FindAction("UI/Cancel") ?? actions.FindAction("Cancel");
                if (p != null) uiInput.point = InputActionReference.Create(p);
                if (c != null) uiInput.leftClick = InputActionReference.Create(c);
                if (n != null) uiInput.move = InputActionReference.Create(n);
                if (s != null) uiInput.submit = InputActionReference.Create(s);
                if (x != null) uiInput.cancel = InputActionReference.Create(x);
            }

            GameStartupController startup = uiRoot.GetComponent<GameStartupController>() ?? uiRoot.AddComponent<GameStartupController>();
            if (uiRoot.GetComponent<InputEnabler>() == null) uiRoot.AddComponent<InputEnabler>();
            if (uiRoot.GetComponent<UIDebugger>() == null) uiRoot.AddComponent<UIDebugger>();

            menuGo.SetActive(true);
            startup.Configure(generator, startMenu, pauseMenu, hudGo, optionsMenu, startGrp, pauseGrp, optionsGrp);

            continueButton.onClick.AddListener(() => { LogMenuClick("Continue"); startup.ContinueOrLoadGame(); });
            newGameButton.onClick.AddListener(() => { LogMenuClick("New Game"); startup.StartNewGame(); });
            optionsButton.onClick.AddListener(() => { LogMenuClick("Options"); startup.OpenOptionsMenu(); });
            resumeButton.onClick.AddListener(() => { LogMenuClick("Resume"); startup.ResumeGame(); });
            saveButton.onClick.AddListener(() => { 
                LogMenuClick("Save"); 
                Debug.Log("[HUD] Triggering startup.SaveGame()");
                startup.SaveGame(); 
            });
            loadButton.onClick.AddListener(() => { 
                LogMenuClick("Load"); 
                Debug.Log("[HUD] Triggering startup.LoadGame()");
                startup.LoadGame(); 
            });
            quitButton.onClick.AddListener(() => { LogMenuClick("Quit"); startup.QuitGame(); });
pauseOptionsButton.onClick.AddListener(() => { LogMenuClick("Pause Options"); startup.OpenOptionsMenu(); });
            pauseQuitButton.onClick.AddListener(() => { LogMenuClick("Pause Quit"); startup.QuitGame(); });
            backButton.onClick.AddListener(() => { LogMenuClick("Back"); startup.ShowMainMenu(); });

            if (player == null)
            {
                // During the initial boot flow we want the start screen visible.
                // If HUD gets rebuilt while gameplay is already active, keep the
                // gameplay state intact instead of reopening the main menu.
                if (GameSessionState.IsGameplayActive)
                {
                    startup.ResumeGame();
                }
                else
                {
                    startup.ShowMainMenu();
                    es.firstSelectedGameObject = continueButton.gameObject;
                    es.SetSelectedGameObject(continueButton.gameObject);
                }
            }
            else startup.ResumeGame();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
            bg.raycastTarget = false;
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
            bg.raycastTarget = false;
            RectTransform rt = backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void CreateMenuBackdropFrame(Transform parent)
        {
            GameObject frame = new GameObject("BackdropFrame");
            frame.transform.SetParent(parent, false);
            Image bg = frame.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.20f, 0.13f, 1f);
            bg.raycastTarget = false;
            RectTransform rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 18f);
            rt.offsetMax = new Vector2(-18f, -18f);
        }

        private void CreateMenuTitlePlate(Transform parent)
        {
            GameObject plate = new GameObject("TitlePlate");
            plate.transform.SetParent(parent, false);
            Image img = plate.AddComponent<Image>();
            img.color = new Color(0.16f, 0.24f, 0.15f, 0.52f);
            img.raycastTarget = false;
            RectTransform rt = plate.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.60f);
            rt.anchorMax = new Vector2(0.63f, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void AddMenuGradient(Transform parent)
        {
            GameObject gradient = new GameObject("Gradient");
            gradient.transform.SetParent(parent, false);
            Image img = gradient.AddComponent<Image>();
            img.color = new Color(0.46f, 0.68f, 0.36f, 0.12f);
            img.raycastTarget = false;
            RectTransform rt = gradient.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.45f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            Image glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(0.74f, 0.88f, 0.52f, 0.08f);
            glowImg.raycastTarget = false;
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
            img.raycastTarget = false;
            RectTransform rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject inner = new GameObject("Inner");
            inner.transform.SetParent(parent, false);
            Image innerImg = inner.AddComponent<Image>();
            innerImg.color = new Color(0.03f, 0.05f, 0.03f, 0.94f);
            innerImg.raycastTarget = false;
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
            img.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private GameObject CreateMenuText(Transform parent, string name, string text, int size, TextAnchor alignment, Vector2 anchoredPosition, Color? color = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.text = text;
            label.alignment = alignment;
            label.fontSize = size;
            label.color = color ?? Color.white;
            label.raycastTarget = false;
            if (uiFont != null) label.font = uiFont;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(740f, size + 10f);
            rt.anchoredPosition = anchoredPosition;
            return go;
        }

        private Button CreateMenuButton(Transform parent, string name, string text, Vector2 anchoredPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.11f, 0.13f, 0.11f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.14f, 0.17f, 0.14f, 1f);
            cb.highlightedColor = new Color(0.34f, 0.40f, 0.28f, 1f);
            cb.pressedColor = new Color(0.09f, 0.11f, 0.09f, 1f);
            cb.selectedColor = new Color(0.42f, 0.48f, 0.34f, 1f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0f;
            button.colors = cb;
            button.transition = Selectable.Transition.ColorTint;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.color = new Color(0.98f, 0.98f, 0.95f, 1f);
            label.raycastTarget = false;
            if (uiFont != null) label.font = uiFont;
            Outline outline = labelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(270f, 48f);
            rt.anchoredPosition = anchoredPosition;

            GameObject border = new GameObject("Border");
            border.transform.SetParent(go.transform, false);
            Image bimg = border.AddComponent<Image>();
            bimg.color = new Color(0.78f, 0.92f, 0.54f, 0.48f);
            bimg.raycastTarget = false;
            RectTransform brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-2f, -2f);
            brt.offsetMax = new Vector2(2f, 2f);
            border.transform.SetAsFirstSibling();
            return button;
        }

        public static void LogMenuClick(string buttonName) => Debug.Log("[HUD] Menu clicked: " + buttonName);

        private StatBarUI CreateStatBar(Transform parent, string name, string labelText, string iconId, Color color, Vector2 pos)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(215, 24);
            rt.anchoredPosition = pos;

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(bar.transform, false);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.color = Color.white;
            RectTransform irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(0, 0.5f); irt.pivot = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(18, 18);
            irt.anchoredPosition = new Vector2(8, 0);

            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(bar.transform, false);
            bgGo.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.08f, 0.78f);
            RectTransform bgrt = bgGo.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.sizeDelta = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bar.transform, false);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = color;
            RectTransform fillrt = fillGo.GetComponent<RectTransform>();
            fillrt.anchorMin = Vector2.zero; fillrt.anchorMax = Vector2.one; fillrt.sizeDelta = Vector2.zero;

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(bar.transform, false);
            Text t = lblGo.AddComponent<Text>();
            t.text = labelText;
            if (uiFont != null) t.font = uiFont;
            t.fontSize = 13;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            RectTransform lblrt = lblGo.GetComponent<RectTransform>();
            lblrt.anchorMin = Vector2.zero; lblrt.anchorMax = Vector2.one;
            lblrt.sizeDelta = new Vector2(-40, 0);
            lblrt.anchoredPosition = new Vector2(32, 0);

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
            rt.sizeDelta = new Vector2(155, 32);
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = pos;

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(counter.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(0, 0.5f); irt.pivot = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(24, 24);
            irt.anchoredPosition = new Vector2(8, 0);

            GameObject valGo = new GameObject("Value");
            valGo.transform.SetParent(counter.transform, false);
            Text tV = valGo.AddComponent<Text>();
            tV.text = "0";
            if (uiFont != null) tV.font = uiFont;
            tV.fontSize = 14;
            tV.fontStyle = FontStyle.Bold;
            tV.alignment = TextAnchor.MiddleRight;
            tV.color = new Color(1f, 0.92f, 0.42f);
            valGo.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            RectTransform vrt = valGo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.pivot = new Vector2(1, 0.5f);
            vrt.offsetMin = new Vector2(40, 0);
            vrt.offsetMax = new Vector2(-8, 0);

            ResourceCounterUI ui = counter.AddComponent<ResourceCounterUI>();
            ui.Configure(itemId, iconImg, tV);
            return ui;
        }

        private InventorySlotUI CreateInventorySlot(Transform parent, string name, int slotIndex)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            RectTransform rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36, 36);

            slot.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.05f, 0.78f);

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slot.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-4, -4);

            GameObject amGo = new GameObject("Amount");
            amGo.transform.SetParent(slot.transform, false);
            Text amountText = amGo.AddComponent<Text>();
            amountText.alignment = TextAnchor.LowerRight;
            amountText.fontSize = 11;
            if (uiFont != null) amountText.font = uiFont;
            RectTransform art = amGo.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
            art.offsetMax = new Vector2(-2, 0);

            GameObject idxGo = new GameObject("Index");
            idxGo.transform.SetParent(slot.transform, false);
            Text indexText = idxGo.AddComponent<Text>();
            indexText.alignment = TextAnchor.UpperLeft;
            indexText.fontSize = 9;
            indexText.color = new Color(0.9f, 0.94f, 0.88f, 0.9f);
            if (uiFont != null) indexText.font = uiFont;
            indexText.text = (slotIndex + 1).ToString();
            RectTransform idxrt = idxGo.GetComponent<RectTransform>();
            idxrt.anchorMin = Vector2.zero; idxrt.anchorMax = Vector2.one;
            idxrt.offsetMin = new Vector2(3, 3); idxrt.offsetMax = new Vector2(-3, -3);

            InventorySlotUI ui = slot.AddComponent<InventorySlotUI>();
            ui.Configure(iconImg, amountText, indexText);
            return ui;
        }
}
}
