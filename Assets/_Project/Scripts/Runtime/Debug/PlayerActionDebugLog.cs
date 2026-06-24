using System.Collections.Generic;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Player;
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
        private Rect panelRect = new Rect(12f, 12f, 380f, 260f);

        [SerializeField]
        private Transform watchedTarget;

        [SerializeField]
        private Transform secondaryTarget;

        [SerializeField]
        private IsometricPlayerController playerController;

        [SerializeField]
        private PlayerMotionVisualFeedback motionFeedback;

        [SerializeField]
        private IsometricCameraFollow cameraFollow;

        [SerializeField]
        private KeyCode resetPositionKey = KeyCode.F2;

        private readonly List<string> entries = new List<string>();
        private int totalActions;
        private Vector2 lastMove;
        private bool lastSprintHeld;
        private bool subscribed;
        private Vector3 lastTargetPosition;
        private Vector3 lastTargetForward;
        private Vector3 targetPositionDelta;
        private Vector3 secondaryTargetPositionDelta;
        private Vector3 lastSecondaryTargetPosition;
        private bool movementEnabled = true;
        private bool bobbingEnabled = true;
        private bool smoothingEnabled = true;
        private bool guiShowOverlay;
        private string[] guiEntries = new string[0];

        private const string ShowOverlayPrefKey = "ApexShift.PlayerActionDebugLog.ShowOverlay";
        private const string LogToConsolePrefKey = "ApexShift.PlayerActionDebugLog.LogToConsole";
        private const string PanelPositionXPrefKey = "ApexShift.PlayerActionDebugLog.PanelPositionX";
        private const string PanelPositionYPrefKey = "ApexShift.PlayerActionDebugLog.PanelPositionY";
        private const int PanelWindowId = 431072;
        private static readonly Rect DefaultPanelRect = new Rect(12f, 12f, 380f, 260f);

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

            if (watchedTarget == null)
            {
                watchedTarget = transform;
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
            CaptureTargetState();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                guiShowOverlay = showOverlay;
                guiEntries = entries.ToArray();
            }

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

            const float width = 380f;
            const float lineHeight = 18f;
            float height = Mathf.Max(260f, 180f + guiEntries.Length * lineHeight);

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

            bool newMovementEnabled = GUILayout.Toggle(movementEnabled, "Movement Enabled");
            if (newMovementEnabled != movementEnabled)
            {
                movementEnabled = newMovementEnabled;
                ApplyRuntimeToggles();
            }

            bool newBobbingEnabled = GUILayout.Toggle(bobbingEnabled, "Bobbing Enabled");
            if (newBobbingEnabled != bobbingEnabled)
            {
                bobbingEnabled = newBobbingEnabled;
                ApplyRuntimeToggles();
            }

            bool newSmoothingEnabled = GUILayout.Toggle(smoothingEnabled, "Camera Smoothing");
            if (newSmoothingEnabled != smoothingEnabled)
            {
                smoothingEnabled = newSmoothingEnabled;
                ApplyRuntimeToggles();
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

            if (!guiShowOverlay)
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

            if (watchedTarget != null)
            {
                GUILayout.Label("Target: " + watchedTarget.name);
                GUILayout.Label("Pos: " + FormatVector(watchedTarget.position));
                GUILayout.Label("Delta: " + FormatVector(targetPositionDelta));
                GUILayout.Label("Fwd: " + FormatVector(watchedTarget.forward));
                GUILayout.Label("Last Pos: " + FormatVector(lastTargetPosition));
                GUILayout.Label("Last Fwd: " + FormatVector(lastTargetForward));
                GUILayout.Label("Move: " + FormatVector(lastMove));
                GUILayout.Label("Sprint: " + (lastSprintHeld ? "yes" : "no"));
            }

            if (secondaryTarget != null)
            {
                GUILayout.Label("Cam: " + secondaryTarget.name);
                GUILayout.Label("Cam Pos: " + FormatVector(secondaryTarget.position));
                GUILayout.Label("Cam Delta: " + FormatVector(secondaryTargetPositionDelta));
                GUILayout.Label("Cam Last Pos: " + FormatVector(lastSecondaryTargetPosition));
            }

            for (int i = guiEntries.Length - 1; i >= 0; i--)
            {
                GUILayout.Label(guiEntries[i]);
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

        public void SetWatchedTarget(Transform target)
        {
            watchedTarget = target;
            CaptureTargetState();
        }

        public void SetSecondaryTarget(Transform target)
        {
            secondaryTarget = target;
            CaptureSecondaryTargetState();
        }

        public void SetMovementController(IsometricPlayerController controller)
        {
            playerController = controller;
            ApplyRuntimeToggles();
        }

        public void SetMotionFeedback(PlayerMotionVisualFeedback feedback)
        {
            motionFeedback = feedback;
            ApplyRuntimeToggles();
        }

        public void SetCameraFollow(IsometricCameraFollow follow)
        {
            cameraFollow = follow;
            ApplyRuntimeToggles();
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
            CaptureTargetState();
            CaptureSecondaryTargetState();
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

        private void CaptureTargetState()
        {
            if (watchedTarget == null)
            {
                lastTargetPosition = Vector3.zero;
                lastTargetForward = Vector3.forward;
                targetPositionDelta = Vector3.zero;
                return;
            }

            Vector3 currentPosition = watchedTarget.position;
            targetPositionDelta = currentPosition - lastTargetPosition;
            lastTargetPosition = currentPosition;
            lastTargetForward = watchedTarget.forward;
        }

        private void CaptureSecondaryTargetState()
        {
            if (secondaryTarget == null)
            {
                secondaryTargetPositionDelta = Vector3.zero;
                lastSecondaryTargetPosition = Vector3.zero;
                return;
            }

            Vector3 currentPosition = secondaryTarget.position;
            secondaryTargetPositionDelta = currentPosition - lastSecondaryTargetPosition;
            lastSecondaryTargetPosition = currentPosition;
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + ", " + value.y.ToString("0.00") + ", " + value.z.ToString("0.00");
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

        private void ApplyRuntimeToggles()
        {
            if (playerController != null)
            {
                playerController.SetMovementEnabled(movementEnabled);
            }

            if (motionFeedback != null)
            {
                motionFeedback.SetBobbingEnabled(bobbingEnabled);
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetSmoothingEnabled(smoothingEnabled);
            }
        }
    }
}
