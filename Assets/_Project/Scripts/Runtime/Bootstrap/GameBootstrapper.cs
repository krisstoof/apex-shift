using UnityEngine;

namespace ApexShift.Runtime.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            // Future systems should be composed through services and adapters.
            // Keep this class small so it only coordinates startup, not gameplay.
            Debug.Log("Apex Shift runtime foundation has started.");
        }
    }
}

