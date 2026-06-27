using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApexShift.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuButtonClickProbe : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private Button button;
        private bool subscribed;

        public void Configure(Button button)
        {
            this.button = button;
            if (!subscribed && this.button != null)
            {
                this.button.onClick.AddListener(LogOnClick);
                subscribed = true;
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            Configure(button);
        }

        private void OnDestroy()
        {
            if (subscribed && button != null)
            {
                button.onClick.RemoveListener(LogOnClick);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[MenuButtonClickProbe] POINTER ENTER '{name}' pos={eventData.position}");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"[MenuButtonClickProbe] POINTER DOWN '{name}' button={eventData.button} pos={eventData.position}");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"[MenuButtonClickProbe] POINTER UP '{name}' button={eventData.button} pos={eventData.position}");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[MenuButtonClickProbe] POINTER CLICK '{name}' button={eventData.button} pos={eventData.position}");
        }

        private void LogOnClick()
        {
            int listeners = button != null ? button.onClick.GetPersistentEventCount() : -1;
            Debug.Log($"[MenuButtonClickProbe] ONCLICK FIRED '{name}' persistentListeners={listeners}");
        }
    }
}
