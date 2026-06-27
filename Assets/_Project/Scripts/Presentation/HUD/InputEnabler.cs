using UnityEngine;
using UnityEngine.InputSystem;
using ApexShift.Runtime.Flow;

namespace ApexShift.Presentation.HUD
{
    public class InputEnabler : MonoBehaviour
    {
        private void Update()
        {
            if (InputSystem.actions != null)
            {
                var uiMap = InputSystem.actions.FindActionMap("UI");
                if (uiMap != null && !uiMap.enabled)
                {
                    Debug.Log("[InputEnabler] UI Action Map was disabled. Enabling it.");
                    uiMap.Enable();
                }
                
                var playerMap = InputSystem.actions.FindActionMap("Player");
                if (playerMap != null && !playerMap.enabled && GameSessionState.IsGameplayActive)
                {
                    Debug.Log("[InputEnabler] Player Action Map was disabled. Enabling it.");
                    playerMap.Enable();
                }

                if (!InputSystem.actions.enabled)
{
                    InputSystem.actions.Enable();
                }
            }
        }
    }
}
