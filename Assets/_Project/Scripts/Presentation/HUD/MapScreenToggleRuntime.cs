using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MapScreenToggleRuntime : MonoBehaviour
    {
        [SerializeField] private MapScreenUI mapScreen;
        [SerializeField] private Key toggleKey = Key.M;
        [SerializeField] private Key closeKey = Key.Escape;

        public void Configure(MapScreenUI screen)
        {
            mapScreen = screen;
        }

        private void Update()
        {
            if (mapScreen == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                mapScreen.Toggle();
                return;
            }

            if (mapScreen.IsOpen && keyboard[closeKey].wasPressedThisFrame)
            {
                mapScreen.SetVisible(false);
            }
        }
    }
}
