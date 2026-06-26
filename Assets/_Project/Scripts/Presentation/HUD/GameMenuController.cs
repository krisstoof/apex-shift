using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Generation;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class GameMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject startMenuRoot;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private CanvasGroup startMenuGroup;
        [SerializeField] private CanvasGroup pauseMenuGroup;
        [SerializeField] private GameObject optionsMenuRoot;
        [SerializeField] private CanvasGroup optionsMenuGroup;
        [SerializeField] private WorldGeneratorRuntime worldGenerator;

        private PlayerInputReader inputReader;
        private GameSaveService saveService;
        private bool started;
        private bool paused;
        private bool optionsFromPause;
        private float targetStartAlpha;
        private float targetPauseAlpha;
        private float targetOptionsAlpha;

        public void Configure(
            GameObject hudRoot,
            GameObject startMenuRoot,
            GameObject pauseMenuRoot,
            Button startButton,
            Button continueButton,
            Button newGameButton,
            Button optionsButton,
            Button resumeButton,
            Button quitButton,
            Button saveButton,
            Button loadButton,
            Button backButton,
            Text titleText,
            Text statusText,
            CanvasGroup startMenuGroup,
            CanvasGroup pauseMenuGroup,
            GameObject optionsMenuRoot,
            CanvasGroup optionsMenuGroup,
            WorldGeneratorRuntime worldGenerator,
            PlayerInputReader inputReader,
            GameSaveService saveService)
        {
            this.hudRoot = hudRoot;
            this.startMenuRoot = startMenuRoot;
            this.pauseMenuRoot = pauseMenuRoot;
            this.startButton = startButton;
            this.continueButton = continueButton;
            this.newGameButton = newGameButton;
            this.optionsButton = optionsButton;
            this.resumeButton = resumeButton;
            this.quitButton = quitButton;
            this.saveButton = saveButton;
            this.loadButton = loadButton;
            this.backButton = backButton;
            this.titleText = titleText;
            this.statusText = statusText;
            this.startMenuGroup = startMenuGroup;
            this.pauseMenuGroup = pauseMenuGroup;
            this.optionsMenuRoot = optionsMenuRoot;
            this.optionsMenuGroup = optionsMenuGroup;
            this.worldGenerator = worldGenerator;
            this.inputReader = inputReader;
            this.saveService = saveService;

            BindButtons();
            ShowStartMenu();
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<PlayerInputReader>();
            }

            if (saveService == null)
            {
                saveService = FindAnyObjectByType<GameSaveService>();
            }
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.PausePressed += OnPausePressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.PausePressed -= OnPausePressed;
            }
        }

        private void BindButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
                startButton.onClick.AddListener(StartGame);
            }
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(LoadGame);
                continueButton.onClick.AddListener(LoadGame);
            }
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(StartGame);
                newGameButton.onClick.AddListener(StartGame);
            }
            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(ShowOptions);
                optionsButton.onClick.AddListener(ShowOptions);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitToDesktop);
                quitButton.onClick.AddListener(QuitToDesktop);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveGame);
                saveButton.onClick.AddListener(SaveGame);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(LoadGame);
                loadButton.onClick.AddListener(LoadGame);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowStartMenu);
                backButton.onClick.AddListener(ShowStartMenu);
            }
        }

        private void Update()
        {
            UpdateFade(startMenuGroup, ref targetStartAlpha);
            UpdateFade(pauseMenuGroup, ref targetPauseAlpha);
            UpdateFade(optionsMenuGroup, ref targetOptionsAlpha);
            if (started && Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void OnPausePressed()
        {
            if (!started)
            {
                return;
            }

            TogglePause();
        }

        public void StartGame()
        {
            if (worldGenerator != null)
            {
                worldGenerator.Generate();
            }

            started = true;
            paused = false;
            targetStartAlpha = 0f;
            targetPauseAlpha = 0f;
            targetOptionsAlpha = 0f;
            Time.timeScale = 1f;
            SetMenuVisible(startMenuRoot, false);
            SetMenuVisible(pauseMenuRoot, false);
            SetMenuVisible(optionsMenuRoot, false);
            SetMenuAlpha(startMenuGroup, 0f);
            SetMenuAlpha(pauseMenuGroup, 0f);
            SetMenuAlpha(optionsMenuGroup, 0f);
            SetHudVisible(true);
            ApplyCursorState(false);
            SetStatus("Game started");
        }

        public void ResumeGame()
        {
            paused = false;
            targetPauseAlpha = 0f;
            targetOptionsAlpha = 0f;
            Time.timeScale = 1f;
            SetMenuVisible(pauseMenuRoot, false);
            SetMenuVisible(optionsMenuRoot, false);
            SetMenuAlpha(pauseMenuGroup, 0f);
            SetMenuAlpha(optionsMenuGroup, 0f);
            SetHudVisible(true);
            ApplyCursorState(false);
            SetStatus("Resumed");
        }

        public void TogglePause()
        {
            if (!started)
            {
                ShowStartMenu();
                return;
            }

            if (paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            paused = true;
            targetPauseAlpha = 1f;
            Time.timeScale = 0f;
            SetMenuVisible(pauseMenuRoot, true);
            SetMenuAlpha(pauseMenuGroup, 1f);
            SetHudVisible(true);
            ApplyCursorState(true);
            SetStatus("Paused");
        }

        public void ShowStartMenu()
        {
            started = false;
            paused = true;
            targetStartAlpha = 1f;
            targetPauseAlpha = 0f;
            targetOptionsAlpha = 0f;
            Time.timeScale = 0f;
            SetMenuVisible(startMenuRoot, true);
            SetMenuVisible(pauseMenuRoot, false);
            SetMenuVisible(optionsMenuRoot, false);
            SetMenuAlpha(startMenuGroup, 1f);
            SetMenuAlpha(pauseMenuGroup, 0f);
            SetMenuAlpha(optionsMenuGroup, 0f);
            SetHudVisible(false);
            ApplyCursorState(true);
            SetStatus("Press Start to play");
        }

        public void ShowOptions()
        {
            optionsFromPause = started;
            paused = true;
            targetOptionsAlpha = 1f;
            targetStartAlpha = 0f;
            targetPauseAlpha = 0f;
            Time.timeScale = 0f;
            SetMenuVisible(optionsMenuRoot, true);
            SetMenuVisible(startMenuRoot, false);
            SetMenuVisible(pauseMenuRoot, false);
            SetMenuAlpha(optionsMenuGroup, 1f);
            SetMenuAlpha(startMenuGroup, 0f);
            SetMenuAlpha(pauseMenuGroup, 0f);
            SetHudVisible(false);
            ApplyCursorState(true);
            SetStatus("Options");
        }

        public void BackFromOptions()
        {
            if (optionsFromPause)
            {
                PauseGame();
            }
            else
            {
                ShowStartMenu();
            }
        }

        public void SaveGame()
        {
            if (saveService == null)
            {
                SetStatus("Save unavailable");
                return;
            }

            saveService.SaveGame("slot1");
            SetStatus("Game saved");
        }

        public void LoadGame()
        {
            if (saveService == null)
            {
                SetStatus("Load unavailable");
                return;
            }

            if (saveService.LoadGame("slot1"))
            {
                started = true;
                paused = false;
                targetStartAlpha = 0f;
                targetPauseAlpha = 0f;
                targetOptionsAlpha = 0f;
                Time.timeScale = 1f;
                SetMenuVisible(startMenuRoot, false);
                SetMenuVisible(pauseMenuRoot, false);
                SetMenuVisible(optionsMenuRoot, false);
                SetMenuAlpha(startMenuGroup, 0f);
                SetMenuAlpha(pauseMenuGroup, 0f);
                SetMenuAlpha(optionsMenuGroup, 0f);
                SetHudVisible(true);
                ApplyCursorState(false);
                SetStatus("Game loaded");
            }
            else
            {
                SetStatus("No save found");
            }
        }

        public void QuitToDesktop()
        {
            Application.Quit();
        }

        private void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible);
            }
        }

        private static void SetMenuVisible(GameObject root, bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private static void SetMenuAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
                group.interactable = alpha > 0.99f;
                group.blocksRaycasts = alpha > 0.99f;
            }
        }

        private static void UpdateFade(CanvasGroup group, ref float targetAlpha)
        {
            if (group == null)
            {
                return;
            }

            float next = Mathf.MoveTowards(group.alpha, targetAlpha, Time.unscaledDeltaTime * 4.5f);
            group.alpha = next;
            bool enabled = next > 0.99f;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }

        private static void ApplyCursorState(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }

            if (titleText != null)
            {
                titleText.text = started ? "Apex Shift" : "Apex Shift";
            }
        }
    }
}
