using ApexShift.Runtime.Interaction;
using UnityEngine;

namespace ApexShift.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionOverlay : MonoBehaviour
    {
        [SerializeField]
        private PlayerInteractionController interactionController;

        [SerializeField]
        private bool showOverlay = false;

        [SerializeField]
        private Rect panelRect = new Rect(320f, 352f, 400f, 120f);

        private const int PanelWindowId = 431074;

        private void Awake()
        {
            if (interactionController == null)
            {
                interactionController = GetComponent<PlayerInteractionController>();
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || interactionController == null)
            {
                return;
            }

            panelRect = GUI.Window(PanelWindowId, panelRect, DrawWindowContents, "Interaction");
        }

        private void DrawWindowContents(int windowId)
        {
            string prompt = interactionController.CurrentPrompt;
            GUILayout.Label(string.IsNullOrEmpty(prompt) ? "No interactable target." : prompt);
            GUILayout.Label("Has target: " + (interactionController.CurrentInteractable != null ? "yes" : "no"));
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }
    }
}
