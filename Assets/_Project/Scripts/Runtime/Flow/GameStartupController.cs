using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Generation;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.Flow
{
    [DisallowMultipleComponent]
    public sealed class GameStartupController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private WorldGeneratorRuntime worldGenerator;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject gameplayHudRoot;
        [SerializeField] private GameObject optionsMenuRoot;
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup pauseMenuGroup;
        [SerializeField] private CanvasGroup optionsMenuGroup;
        [SerializeField] private CameraComponent menuCamera;

        [Header("Startup")]
        [SerializeField] private bool clearGeneratedWorldOnMainMenu = true;
        [SerializeField] private bool pauseTimeInMenu = true;

        private ApexShift.Runtime.Save.GameSaveService saveService;

        public void Configure(
WorldGeneratorRuntime worldGenerator,
            GameObject mainMenuRoot,
            GameObject pauseMenuRoot,
            GameObject gameplayHudRoot,
            GameObject optionsMenuRoot = null,
            CanvasGroup mainMenuGroup = null,
            CanvasGroup pauseMenuGroup = null,
            CanvasGroup optionsMenuGroup = null)
        {
            this.worldGenerator = worldGenerator;
            this.mainMenuRoot = mainMenuRoot;
            this.pauseMenuRoot = pauseMenuRoot;
            this.gameplayHudRoot = gameplayHudRoot;
            this.optionsMenuRoot = optionsMenuRoot;
            this.mainMenuGroup = mainMenuGroup;
            this.pauseMenuGroup = pauseMenuGroup;
            this.optionsMenuGroup = optionsMenuGroup;
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureMenuCamera();
            ShowMainMenu();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            // Direct keyboard check as fallback if actions are blocked
            if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Escape].wasPressedThisFrame)
            {
                bool isPauseOpen = pauseMenuRoot != null && pauseMenuRoot.activeSelf;
                bool isMainOpen = mainMenuRoot != null && mainMenuRoot.activeSelf;
                bool isOptionsOpen = optionsMenuRoot != null && optionsMenuRoot.activeSelf;

                Debug.Log($"[Flow] ESC Detected. GameplayActive: {GameSessionState.IsGameplayActive}, PauseOpen: {isPauseOpen}, MainOpen: {isMainOpen}, OptionsOpen: {isOptionsOpen}");

                if (isOptionsOpen)
                {
                    if (isMainOpen) ShowMainMenu();
                    else OpenPauseMenu();
                }
                else if (isPauseOpen)
                {
                    ResumeGame();
                }
                else if (GameSessionState.IsGameplayActive && !isMainOpen)
                {
                    OpenPauseMenu();
                }
            }
        }

        private void ResolveReferences()
        {
            if (worldGenerator == null)
            {
                worldGenerator = Object.FindAnyObjectByType<WorldGeneratorRuntime>();
            }

            if (saveService == null)
            {
                saveService = Object.FindAnyObjectByType<ApexShift.Runtime.Save.GameSaveService>();
                if (saveService == null)
                {
                    saveService = gameObject.AddComponent<ApexShift.Runtime.Save.GameSaveService>();
                }
            }
        }

        private void EnsureMenuCamera()
        {
            if (menuCamera != null)
            {
                return;
            }

            GameObject existing = GameObject.Find("Menu Camera");
            if (existing != null)
            {
                menuCamera = existing.GetComponent<CameraComponent>();
                if (menuCamera != null)
                {
                    menuCamera.enabled = true;
                }
                return;
            }

            GameObject cameraGo = new GameObject("Menu Camera");
            cameraGo.tag = "MainCamera";
            menuCamera = cameraGo.AddComponent<CameraComponent>();
            
            System.Type cameraDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (cameraDataType != null) {
                var data = cameraGo.AddComponent(cameraDataType);
                var prop = cameraDataType.GetProperty("renderType");
                if (prop != null) prop.SetValue(data, 0); // 0 is CameraRenderType.Base
            }

            menuCamera.clearFlags = CameraClearFlags.SolidColor;
menuCamera.backgroundColor = new Color(0.08f, 0.13f, 0.08f, 1f);
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 12f;
            cameraGo.transform.position = new Vector3(0f, 18f, -12f);
            cameraGo.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        }

        public void ShowMainMenu()
        {
            Debug.Log("[Flow] ShowMainMenu called.");
            ResolveReferences();

            GameSessionState.EnterMainMenu();
            CreatureDebugOverlay.HideAllDebugFrames = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseTimeInMenu)
            {
                Time.timeScale = 0f;
            }

            SetActive(mainMenuRoot, true);
            SetActive(pauseMenuRoot, false);
            SetActive(gameplayHudRoot, false);
            SetActive(optionsMenuRoot, false);
            SetGroupVisible(mainMenuGroup, true);
            SetGroupVisible(pauseMenuGroup, false);
            SetGroupVisible(optionsMenuGroup, false);

            if (menuCamera != null)
            {
                menuCamera.enabled = true;
                menuCamera.gameObject.SetActive(true);
            }

            if (clearGeneratedWorldOnMainMenu && worldGenerator != null)
            {
                worldGenerator.ClearGeneratedWorld();
            }
        }

        public void StartNewGame()
        {
            Debug.Log("[Flow] StartNewGame requested.");
            HideAllMenusImmediate();
            StartGameplay(generateWorld: true);
        }

        public void ContinueOrLoadGame()
        {
            Debug.Log("[Flow] ContinueOrLoadGame requested.");
            ResolveReferences();
            HideAllMenusImmediate();
            
            if (saveService != null && saveService.LoadGame("default"))
            {
                Debug.Log("[Flow] Save found and loaded.");
                // LoadGame already triggered world regeneration and UI refresh
                // Ensure we are in gameplay state
                StartGameplay(generateWorld: false);
            }
            else
            {
                Debug.Log("[Flow] No save found, starting new game.");
                StartGameplay(generateWorld: true);
            }
        }

        public void SaveGame()
        {
            ResolveReferences();
            if (saveService != null)
            {
                saveService.SaveGame("default");
                Debug.Log("[Flow] Game saved to 'default' slot.");
            }
        }

        public void LoadGame()
        {
            ResolveReferences();
            HideAllMenusImmediate();
            if (saveService != null)
            {
                if (saveService.LoadGame("default"))
                {
                    ResumeGame();
                    Debug.Log("[Flow] Game loaded from 'default' slot.");
                }
            }
        }

        public void ResumeGame()
{
            Debug.Log("[Flow] ResumeGame requested.");
            StartGameplay(generateWorld: false);
        }

        public void OpenPauseMenu()
        {
            Debug.Log("[Flow] OpenPauseMenu called.");
            SetActive(pauseMenuRoot, true);
            SetActive(mainMenuRoot, false);
            SetActive(gameplayHudRoot, false);
            SetActive(optionsMenuRoot, false);
            SetGroupVisible(mainMenuGroup, false);
            SetGroupVisible(pauseMenuGroup, true);
            SetGroupVisible(optionsMenuGroup, false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseTimeInMenu)
            {
                Time.timeScale = 0f;
            }

            CreatureDebugOverlay.HideAllDebugFrames = true;
        }

        public void OpenOptionsMenu()
        {
            Debug.Log("[Flow] OpenOptionsMenu called.");
            SetActive(optionsMenuRoot, true);
            SetActive(mainMenuRoot, false);
            SetActive(pauseMenuRoot, false);
            SetActive(gameplayHudRoot, false);
            SetGroupVisible(mainMenuGroup, false);
            SetGroupVisible(pauseMenuGroup, false);
            SetGroupVisible(optionsMenuGroup, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseTimeInMenu)
            {
                Time.timeScale = 0f;
            }
        }

        public void QuitGame()
        {
            Debug.Log("[Flow] QuitGame called.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartGameplay(bool generateWorld)
        {
            Debug.Log($"[Flow] StartGameplay(generateWorld={generateWorld}) called.");
            ResolveReferences();

            SetActive(mainMenuRoot, false);
            SetActive(pauseMenuRoot, false);
            SetActive(gameplayHudRoot, true);
            SetActive(optionsMenuRoot, false);
            SetGroupVisible(mainMenuGroup, false);
            SetGroupVisible(pauseMenuGroup, false);
            SetGroupVisible(optionsMenuGroup, false);

            if (menuCamera != null)
            {
                menuCamera.enabled = false;
                menuCamera.gameObject.SetActive(false);
            }

            Time.timeScale = 1f;
            GameSessionState.BeginGameplay();
            CreatureDebugOverlay.HideAllDebugFrames = false;

            if (generateWorld && worldGenerator != null)
            {
                worldGenerator.Generate();
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideAllMenusImmediate()
        {
            SetActive(mainMenuRoot, false);
            SetActive(pauseMenuRoot, false);
            SetActive(optionsMenuRoot, false);
            SetGroupVisible(mainMenuGroup, false);
            SetGroupVisible(pauseMenuGroup, false);
            SetGroupVisible(optionsMenuGroup, false);
        }

        private static void SetActive(GameObject go, bool visible)
        {
            if (go != null)
            {
                Debug.Log($"[Flow] Setting {go.name} active={visible}");
                go.SetActive(visible);
            }
            else
            {
                Debug.LogWarning("[Flow] SetActive called on NULL GameObject");
            }
        }

        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group != null)
            {
                Debug.Log($"[Flow] Setting group {group.gameObject.name} alpha={(visible ? 1 : 0)}");
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
            else
            {
                Debug.LogWarning("[Flow] SetGroupVisible called on NULL CanvasGroup");
            }
        }
}
}
