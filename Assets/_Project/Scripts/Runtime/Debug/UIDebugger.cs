using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ApexShift.Runtime.Debugging
{
    public class UIDebugger : MonoBehaviour
    {
        private readonly HashSet<Button> trackedButtons = new HashSet<Button>();

        private void Update()
        {
            if (EventSystem.current == null || Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePos;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Track new buttons
            var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var b in allButtons)
{
                if (!trackedButtons.Contains(b))
                {
                    trackedButtons.Add(b);
                    b.onClick.AddListener(() => LogButtonClick(b));
                }
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var actions = InputSystem.actions;
                string actionsInfo = actions != null ? $"Enabled={actions.enabled}" : "NULL";
                var uiMap = actions?.FindActionMap("UI");
                string mapInfo = uiMap != null ? $"UI Map Enabled={uiMap.enabled}" : "UI Map Missing";

                Debug.Log($"[UIDebug] Mouse Click at {mousePos}. Screen: {Screen.width}x{Screen.height}. Hits: {results.Count}. Actions: {actionsInfo}, {mapInfo}");
if (results.Count == 0)
                {
                    Debug.Log("[UIDebug]  - No UI elements hit.");
                }
                foreach (var hit in results)
                {
                    var graphic = hit.gameObject.GetComponent<Graphic>();
                    var button = hit.gameObject.GetComponent<Button>();
                    string buttonInfo = button != null ? $" (Button Listeners: {button.onClick.GetPersistentEventCount()})" : "";
                    string extra = graphic != null ? $" (RaycastTarget: {graphic.raycastTarget}, Color: {graphic.color})" : "";
                    Debug.Log($"  - Hit: {hit.gameObject.name} on Canvas {hit.module.gameObject.name}{extra}{buttonInfo}");
                }

                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    Debug.Log($"[UIDebug] Current Selected: {EventSystem.current.currentSelectedGameObject.name}");
                }
            }
        }

        private void LogButtonClick(Button b)
        {
            Debug.Log($"[UIDebug] BUTTON EVENT: '{b.name}' onClick fired!");
        }
    }
}
