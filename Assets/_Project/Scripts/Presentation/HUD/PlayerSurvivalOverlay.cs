using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class PlayerSurvivalOverlay : MonoBehaviour
    {
        [SerializeField]
        private PlayerSurvivalRuntime survivalRuntime;

        [SerializeField]
        private bool showOverlay = true;

        [SerializeField]
        private KeyCode toggleOverlayKey = KeyCode.F3;

        [SerializeField]
        private Rect panelRect = new Rect(12f, 164f, 260f, 176f);

        private const int PanelWindowId = 431073;

        private void Awake()
        {
            if (survivalRuntime == null)
            {
                survivalRuntime = GetComponent<PlayerSurvivalRuntime>();
            }
        }

        private void OnGUI()
        {
            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == toggleOverlayKey)
            {
                showOverlay = !showOverlay;
                Event.current.Use();
            }

            if (!showOverlay || survivalRuntime == null)
            {
                return;
            }

            panelRect = GUI.Window(PanelWindowId, panelRect, DrawWindowContents, "Survival");
        }

        private void DrawWindowContents(int windowId)
        {
            if (survivalRuntime.Stats == null)
            {
                GUILayout.Label("Survival not initialized.");
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
                return;
            }

            GUILayout.Label("Health: " + survivalRuntime.Stats.Health.ToString("0.0"));
            GUILayout.Label("Hunger: " + survivalRuntime.Stats.Hunger.ToString("0.0"));
            GUILayout.Label("Stamina: " + survivalRuntime.Stats.Stamina.ToString("0.0"));
            GUILayout.Label("Rest: " + survivalRuntime.Stats.Rest.ToString("0.0"));
            GUILayout.Label("Condition: " + survivalRuntime.ConditionText);
            GUILayout.Label("Wants sprint: " + (survivalRuntime.WantsSprint ? "yes" : "no"));
            GUILayout.Label("Sprinting: " + (survivalRuntime.IsSprinting ? "yes" : "no"));
            GUILayout.Label("Speed x" + survivalRuntime.SpeedMultiplier.ToString("0.00"));
            GUILayout.Label("Campfire regen: " + (survivalRuntime.Stats.CampfireRegenActive ? "yes" : "no"));
            GUILayout.Label("God mode: " + (survivalRuntime.Stats.GodMode ? "yes" : "no"));

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }
    }
}
