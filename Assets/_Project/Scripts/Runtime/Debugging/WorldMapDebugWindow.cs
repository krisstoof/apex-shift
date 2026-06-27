using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.UI.Debugging;
using ApexShift.Runtime.UI.Snapshots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Debugging
{
    [DisallowMultipleComponent]
    public sealed class WorldMapDebugWindow : MonoBehaviour
    {
        [SerializeField] private bool visible = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F4;
        [SerializeField] private float refreshIntervalSeconds = 0.5f;

        private Rect windowRect = new Rect(720f, 140f, 400f, 400f);
        private Vector2 scroll;
        private string cachedText = "World debug loading...";
        private bool showDebugControls = false;
        private bool isResizing = false;
        private float refreshTimer;
        private GameSnapshotProvider snapshotProvider;

        private void Update()
        {
            if (!GameSessionState.IsGameplayActive)
            {
                visible = false;
                DebugUIBounds.WorldMapWindowVisible = false;
                return;
            }

            bool togglePressed = false;
            if (Keyboard.current != null)
            {
                if (toggleKey == KeyCode.F4 && Keyboard.current[Key.F4].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F1 && Keyboard.current[Key.F1].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F2 && Keyboard.current[Key.F2].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F3 && Keyboard.current[Key.F3].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.Escape && Keyboard.current[Key.Escape].wasPressedThisFrame) togglePressed = true;
            }

            if (togglePressed)
            {
                visible = !visible;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (snapshotProvider == null || refreshTimer <= 0f)
            {
                snapshotProvider = Object.FindAnyObjectByType<GameSnapshotProvider>();
                refreshTimer = Mathf.Max(0.1f, refreshIntervalSeconds);
            }

            cachedText = snapshotProvider != null
                ? DebugPanelPresenter.FormatSnapshot(snapshotProvider.LastSnapshot)
                : "GameSnapshotProvider missing. Add it to the runtime scene.";
        }

        private void OnGUI()
        {
            if (!GameSessionState.IsGameplayActive || !visible)
            {
                DebugUIBounds.WorldMapWindowVisible = false;
                return;
            }

            DebugUIBounds.WorldMapWindowVisible = true;
            DebugUIBounds.WorldMapWindowRect = windowRect;

            windowRect = GUI.Window(6789, windowRect, DrawWindow, "World / Map Debug  [F4]");
        }

        private void DrawWindow(int id)
        {
            showDebugControls = GUILayout.Toggle(showDebugControls, "Show Debug Controls / Opcje Debugowania", "button");
            if (showDebugControls)
            {
                GUILayout.BeginVertical("box");
                bool hideFrames = CreatureDebugOverlay.HideAllDebugFrames;
                bool newHideFrames = GUILayout.Toggle(hideFrames, "Hide All Creature Overlays / Ukryj Ramki ZwierzÄ…t");
                if (newHideFrames != hideFrames)
                {
                    CreatureDebugOverlay.HideAllDebugFrames = newHideFrames;
                }

                GUILayout.Label("World debug is snapshot-driven. Scene mutation buttons were removed from UI.");
                GUILayout.EndVertical();
            }

            float scrollHeight = windowRect.height - (showDebugControls ? 140f : 44f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(windowRect.width - 16f), GUILayout.Height(Mathf.Max(50f, scrollHeight)));
            GUILayout.Label(cachedText);
            GUILayout.EndScrollView();

            Rect resizeHandleRect = new Rect(windowRect.width - 16f, windowRect.height - 16f, 16f, 16f);
            GUI.Box(resizeHandleRect, "", "label");

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                isResizing = true;
                currentEvent.Use();
            }

            if (isResizing)
            {
                if (currentEvent.type == EventType.MouseDrag)
                {
                    windowRect.width = Mathf.Max(230f, currentEvent.mousePosition.x + 10f);
                    windowRect.height = Mathf.Max(230f, currentEvent.mousePosition.y + 10f);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    isResizing = false;
                    currentEvent.Use();
                }
            }

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 22f));
        }
    }
}
