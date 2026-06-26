using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MenuRaycastDebugProbe : MonoBehaviour
    {
        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current == null)
            {
                Debug.LogWarning("[HUD] Menu click probe: no EventSystem present.");
                return;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count == 0)
            {
                Debug.Log("[HUD] Menu click probe: no UI hit at " + Mouse.current.position.ReadValue());
                return;
            }

            string top = results[0].gameObject != null ? results[0].gameObject.name : "<null>";
            Debug.Log("[HUD] Menu click probe: top hit = " + top + " (" + results.Count + " hits)");
        }
    }
}
