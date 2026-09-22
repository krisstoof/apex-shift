using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ApexShift.Presentation.Debugging
{
    public class UIDebugger : MonoBehaviour
    {
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Update()
        {
            if (EventSystem.current == null || Mouse.current == null) return;

            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePos;

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

                var actions = InputSystem.actions;
                string actionsInfo = actions != null ? $"Enabled={actions.enabled}" : "NULL";
                var uiMap = actions?.FindActionMap("UI");
                string mapInfo = uiMap != null ? $"UI Map Enabled={uiMap.enabled}" : "UI Map Missing";

                Debug.Log($"[UIDebug] Mouse Click at {mousePos}. Screen: {Screen.width}x{Screen.height}. Hits: {raycastResults.Count}. Actions: {actionsInfo}, {mapInfo}");
if (raycastResults.Count == 0)
                {
                    Debug.Log("[UIDebug]  - No UI elements hit.");
                }
                foreach (var hit in raycastResults)
                {
                    var graphic = hit.gameObject.GetComponent<Graphic>();
                    var button = hit.gameObject.GetComponentInParent<Button>();
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
}
