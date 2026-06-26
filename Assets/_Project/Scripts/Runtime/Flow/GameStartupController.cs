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

        private void ResolveReferences()
        {
            if (worldGenerator == null)
            {
                worldGenerator = Object.FindAnyObjectByType<WorldGeneratorRuntime>();
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
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.08f, 0.13f, 0.08f, 1f);
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 12f;
            cameraGo.transform.position = new Vector3(0f, 18f, -12f);
            cameraGo.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        }

        public void ShowMainMenu()
        {
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
            StartGameplay(generateWorld: true);
        }

        public void ContinueOrLoadGame()
        {
            StartGameplay(generateWorld: true);
        }

        public void ResumeGame()
        {
            StartGameplay(generateWorld: false);
        }

        public void OpenPauseMenu()
        {
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

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartGameplay(bool generateWorld)
        {
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
        }

        private static void SetActive(GameObject go, bool visible)
        {
            if (go != null)
            {
                go.SetActive(visible);
            }
        }

        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
