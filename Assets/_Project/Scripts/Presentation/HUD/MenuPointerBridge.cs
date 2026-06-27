using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MenuPointerBridge : MonoBehaviour
    {
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private GameObject currentHover;

        private void Update()
        {
            if (Mouse.current == null || EventSystem.current == null)
            {
                return;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            GameObject nextHover = raycastResults.Count > 0 ? raycastResults[0].gameObject : null;

            if (nextHover != currentHover)
            {
                if (currentHover != null)
                {
                    ExecuteEvents.ExecuteHierarchy(currentHover, eventData, ExecuteEvents.pointerExitHandler);
                }

                if (nextHover != null)
                {
                    ExecuteEvents.ExecuteHierarchy(nextHover, eventData, ExecuteEvents.pointerEnterHandler);
                }

                currentHover = nextHover;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && nextHover != null)
            {
                ExecuteEvents.ExecuteHierarchy(nextHover, eventData, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
