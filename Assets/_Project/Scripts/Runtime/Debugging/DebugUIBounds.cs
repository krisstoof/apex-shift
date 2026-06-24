using UnityEngine;

namespace ApexShift.Runtime.Debugging
{
    public static class DebugUIBounds
    {
        public static Rect WorldMapWindowRect;
        public static bool WorldMapWindowVisible;

        public static Rect PlayerActionWindowRect;
        public static bool PlayerActionWindowVisible;

        public static bool IsMouseOverAnyWindow()
        {
            Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 rawPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                mousePos = new Vector2(rawPos.x, Screen.height - rawPos.y);
            }
            else
#endif
            {
                mousePos = new Vector2(UnityEngine.Input.mousePosition.x, Screen.height - UnityEngine.Input.mousePosition.y);
            }

            if (WorldMapWindowVisible && WorldMapWindowRect.Contains(mousePos)) return true;
            if (PlayerActionWindowVisible && PlayerActionWindowRect.Contains(mousePos)) return true;

            return false;
        }
    }
}