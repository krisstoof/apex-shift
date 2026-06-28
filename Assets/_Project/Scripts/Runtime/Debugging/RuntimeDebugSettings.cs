using UnityEngine;

namespace ApexShift.Runtime.Debugging
{
    /// <summary>
    /// Global runtime switchboard for debug UI. This lets debug rendering be disabled
    /// without removing components from scene objects or prefabs.
    /// </summary>
    public static class RuntimeDebugSettings
    {
        private const float MinimumRefreshInterval = 0.05f;

        public static bool DebugEnabled { get; private set; } = true;
        public static bool CreatureFramesEnabled { get; private set; } = true;
        public static bool EcosystemOverlayEnabled { get; private set; } = true;
        public static float RefreshIntervalSeconds { get; private set; } = 0.35f;

        public static void SetDebugEnabled(bool enabled)
        {
            DebugEnabled = enabled;
        }

        public static void SetCreatureFramesEnabled(bool enabled)
        {
            CreatureFramesEnabled = enabled;
        }

        public static void SetEcosystemOverlayEnabled(bool enabled)
        {
            EcosystemOverlayEnabled = enabled;
        }

        public static void SetRefreshInterval(float seconds)
        {
            RefreshIntervalSeconds = Mathf.Max(MinimumRefreshInterval, seconds);
        }

        public static void RestoreDefaults()
        {
            DebugEnabled = true;
            CreatureFramesEnabled = true;
            EcosystemOverlayEnabled = true;
            RefreshIntervalSeconds = 0.35f;
        }
    }
}
