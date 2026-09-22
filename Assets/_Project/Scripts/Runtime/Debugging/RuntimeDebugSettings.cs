using System;
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

        private static bool requestedDeveloperDiagnostics;
        private static bool requestedCreatureFrames;
        private static bool requestedEcosystemOverlay;
        private static bool requestedFreeBuilding;
        private static bool requestedFreeCrafting;

        public static event Action<bool> DeveloperDiagnosticsChanged;

        public static bool DeveloperDiagnosticsAvailable
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            get => true;
#else
            get => false;
#endif
        }

        public static bool DeveloperDiagnosticsEnabled =>
            RuntimeDiagnosticsPolicy.Resolve(DeveloperDiagnosticsAvailable, false, requestedDeveloperDiagnostics);
        public static bool DebugEnabled => DeveloperDiagnosticsEnabled;
        public static bool CreatureFramesEnabled => DeveloperDiagnosticsEnabled && requestedCreatureFrames;
        public static bool EcosystemOverlayEnabled => DeveloperDiagnosticsEnabled && requestedEcosystemOverlay;
        public static bool FreeBuildingEnabled => DeveloperDiagnosticsEnabled && requestedFreeBuilding;
        public static bool FreeCraftingEnabled => DeveloperDiagnosticsEnabled && requestedFreeCrafting;
        public static float RefreshIntervalSeconds { get; private set; } = 0.35f;

        public static void SetDeveloperDiagnosticsEnabled(bool enabled)
        {
            bool previous = DeveloperDiagnosticsEnabled;
            requestedDeveloperDiagnostics = enabled;
            bool current = DeveloperDiagnosticsEnabled;
            if (previous != current)
            {
                DeveloperDiagnosticsChanged?.Invoke(current);
            }
        }
        public static void SetDebugEnabled(bool enabled) => SetDeveloperDiagnosticsEnabled(enabled);

        public static void SetCreatureFramesEnabled(bool enabled)
        {
            requestedCreatureFrames = enabled;
        }

        public static void SetEcosystemOverlayEnabled(bool enabled)
        {
            requestedEcosystemOverlay = enabled;
        }

        public static void SetFreeBuildingEnabled(bool enabled)
        {
            requestedFreeBuilding = enabled;
        }

        public static void SetFreeCraftingEnabled(bool enabled)
        {
            requestedFreeCrafting = enabled;
        }

        public static void SetRefreshInterval(float seconds)
        {
            RefreshIntervalSeconds = Mathf.Max(MinimumRefreshInterval, seconds);
        }

        public static void RestoreDefaults()
        {
            bool previous = DeveloperDiagnosticsEnabled;
            requestedDeveloperDiagnostics = false;
            requestedCreatureFrames = false;
            requestedEcosystemOverlay = false;
            requestedFreeBuilding = false;
            requestedFreeCrafting = false;
            RefreshIntervalSeconds = 0.35f;
            if (previous && !DeveloperDiagnosticsEnabled)
            {
                DeveloperDiagnosticsChanged?.Invoke(false);
            }
        }
    }
}
