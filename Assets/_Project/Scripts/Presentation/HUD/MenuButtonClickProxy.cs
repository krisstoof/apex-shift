using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MenuButtonClickProxy : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Button button;
        private Action onActivated;

        public void Configure(Button button)
        {
            this.button = button;
            onActivated = () =>
            {
                if (this.button != null)
                {
                    this.button.onClick.Invoke();
                }
            };
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            onActivated?.Invoke();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            onActivated?.Invoke();
        }
    }
}
