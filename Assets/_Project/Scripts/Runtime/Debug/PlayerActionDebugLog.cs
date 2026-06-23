using System.Collections.Generic;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Debugging
{
    public sealed class PlayerActionDebugLog : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private bool showOverlay = true;

        [SerializeField]
        private bool logToConsole = true;

        [SerializeField]
        private int maxEntries = 10;

        [SerializeField]
        private KeyCode toggleOverlayKey = KeyCode.F1;

        [SerializeField]
        private Rect panelRect = new Rect(12f, 12f, 280f, 140f);

        [SerializeField]
        private KeyCode resetPositionKey = KeyCode.F2;

        private readonly List<string> entries = new List<string>();
        private int totalActions;
        private Vector2 lastMove;
        private bool lastSprintHeld;
        private bool subscribed;

        private const string ShowOverlayPrefKey = "ApexShift.PlayerActionDebugLog.ShowOverlay";
        private const string LogToConsolePrefKey = "ApexShift.PlayerActionDebugLog.LogToConsole";
        private const string PanelPositionXPrefKey = "ApexShift.PlayerActionDebugLog.PanelPositionX";
        private const string PanelPositionYPrefKey = "ApexShift.PlayerActionDebugLog.PanelPositionY";
        private const int PanelWindowId = 431072;
        private static readonly Rect DefaultPanelRect = new Rect(12f, 12f, 240f, 110f);

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (inputReader == null && transform.root != null)
            {
                inputReader = transform.root.GetComponentInChildren<PlayerInputReader>(true);
            }

            LoadPreferences();
        }

        private void OnEnable()
        {
            TrySubscribe();
            CaptureInitialState();
        }

        private void OnDisable()
        {
            SavePreferences();
            Unsubscribe();
        }

        private void Update()
        {
            if (inputReader == null)
            {
                return;
            }

            Vector2 move = inputReader.Move;
            bool isMoving = move.sqrMagnitude > 0.0001f;
            bool wasMoving = lastMove.sqrMagnitude > 0.0001f;

            if (isMoving != wasMoving)
            {
                Append(isMoving ? "Move Started" : "Move Stopped");
            }

            bool sprintHeld = inputReader.SprintHeld;
            if (sprintHeld != lastSprintHeld)
            {
                Append(sprintHeld ? "Sprint Started" : "Sprint Stopped");
            }

            lastMove = move;
            lastSprintHeld = sprintHeld;
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.KeyDown && currentEvent.keyCode == toggleOverlayKey)
            {
                showOverlay = !showOverlay;
                SavePreferences();
                currentEvent.Use();
            }

            if (currentEvent != null && currentEvent.type == EventType.KeyDown && currentEvent.keyCode == resetPositionKey)
            {
                ResetPanelPosition();
                currentEvent.Use();
            }

            const float width = 240f;
            const float lineHeight = 16f;
            float height = Mathf.Max(110f, 84f + entries.Count * lineHeight);

            panelRect.width = width;
            panelRect.height = height;
            panelRect = GUI.Window(PanelWindowId, panelRect, DrawWindowContents, "Action Log");
        }

        private void DrawWindowContents(int windowId)
        {
            bool newShowOverlay = GUILayout.Toggle(showOverlay, "Show overlay");
            if (newShowOverlay != showOverlay)
            {
                showOverlay = newShowOverlay;
                SavePreferences();
            }

            bool newLogToConsole = GUILayout.Toggle(logToConsole, "Log to Console");
            if (newLogToConsole != logToConsole)
            {
                logToConsole = newLogToConsole;
                SavePreferences();
            }

            GUILayout.Label("Total actions: " + totalActions);

            if (GUILayout.Button("Clear"))
            {
                entries.Clear();
            }

            if (GUILayout.Button("Reset Position"))
            {
                ResetPanelPosition();
            }

            if (!showOverlay)
            {
                GUILayout.Label("Overlay hidden.");
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
                return;
            }

            if (inputReader == null)
            {
                GUILayout.Label("No PlayerInputReader found.");
            }
            else
            {
                GUILayout.Label("Reader: " + inputReader.name);
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(entries[i]);
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader)
            {
                return;
            }

            Unsubscribe();
            inputReader = reader;
            TrySubscribe();
            CaptureInitialState();
        }

        private void TrySubscribe()
        {
            if (subscribed || inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed += OnInteractPressed;
            inputReader.AttackPressed += OnAttackPressed;
            inputReader.OpenInventoryPressed += OnOpenInventoryPressed;
            inputReader.OpenCraftingPressed += OnOpenCraftingPressed;
            inputReader.ToggleMapPressed += OnToggleMapPressed;
            inputReader.PausePressed += OnPausePressed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inputReader == null)
            {
                subscribed = false;
                return;
            }

            inputReader.InteractPressed -= OnInteractPressed;
            inputReader.AttackPressed -= OnAttackPressed;
            inputReader.OpenInventoryPressed -= OnOpenInventoryPressed;
            inputReader.OpenCraftingPressed -= OnOpenCraftingPressed;
            inputReader.ToggleMapPressed -= OnToggleMapPressed;
            inputReader.PausePressed -= OnPausePressed;
            subscribed = false;
        }

        private void CaptureInitialState()
        {
            if (inputReader == null)
            {
                lastMove = Vector2.zero;
                lastSprintHeld = false;
                return;
            }

            lastMove = inputReader.Move;
            lastSprintHeld = inputReader.SprintHeld;
        }

        private void OnInteractPressed() => Append("Interact");

        private void OnAttackPressed() => Append("Attack");

        private void OnOpenInventoryPressed() => Append("Open Inventory");

        private void OnOpenCraftingPressed() => Append("Open Crafting");

        private void OnToggleMapPressed() => Append("Toggle Map");

        private void OnPausePressed() => Append("Pause");

        private void Append(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            entries.Add(message);
            totalActions++;
            while (entries.Count > Mathf.Max(1, maxEntries))
            {
                entries.RemoveAt(0);
            }

            if (logToConsole)
            {
                Debug.Log($"Player action: {message}", this);
            }
        }

        private void LoadPreferences()
        {
            showOverlay = PlayerPrefs.GetInt(ShowOverlayPrefKey, showOverlay ? 1 : 0) != 0;
            logToConsole = PlayerPrefs.GetInt(LogToConsolePrefKey, logToConsole ? 1 : 0) != 0;
            float x = PlayerPrefs.GetFloat(PanelPositionXPrefKey, panelRect.x);
            float y = PlayerPrefs.GetFloat(PanelPositionYPrefKey, panelRect.y);
            panelRect.x = x;
            panelRect.y = y;
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetInt(ShowOverlayPrefKey, showOverlay ? 1 : 0);
            PlayerPrefs.SetInt(LogToConsolePrefKey, logToConsole ? 1 : 0);
            PlayerPrefs.SetFloat(PanelPositionXPrefKey, panelRect.x);
            PlayerPrefs.SetFloat(PanelPositionYPrefKey, panelRect.y);
            PlayerPrefs.Save();
        }

        private void ResetPanelPosition()
        {
            panelRect = DefaultPanelRect;
            SavePreferences();
        }
    }
}
