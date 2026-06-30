using UnityEngine;

namespace ApexShift.Runtime.Player
{
    /// <summary>
    /// Stable runtime player locator for AI/combat systems.
    /// Do not rely only on GameObject.Find("Player") or a Unity tag that may not exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPresenceRuntime : MonoBehaviour
    {
        private static PlayerPresenceRuntime active;

        public static PlayerPresenceRuntime Active => active;
        public static Transform ActiveTransform => active != null && active.gameObject.activeInHierarchy ? active.transform : null;

        private void OnEnable()
        {
            active = this;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                active = null;
            }
        }

        public void MarkActive()
        {
            active = this;
        }

        public static Transform ResolveFallback()
        {
            if (ActiveTransform != null)
            {
                return ActiveTransform;
            }

            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                return playerObject.transform;
            }

            try
            {
                playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                {
                    return playerObject.transform;
                }
            }
            catch (UnityException)
            {
            }

            IsometricPlayerController controller = Object.FindAnyObjectByType<IsometricPlayerController>();
            return controller != null ? controller.transform : null;
        }
    }
}
