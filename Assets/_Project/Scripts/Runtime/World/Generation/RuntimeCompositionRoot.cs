using UnityEngine;
using ApexShift.Runtime.Bootstrap;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.UI.Debugging;
using ApexShift.Runtime.UI.Snapshots;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.World.Sky;
using ApexShift.Runtime.Debugging;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Composes runtime services owned by the active world generation.</summary>
    public sealed class RuntimeCompositionRoot
    {
        public EcosystemRuntime Ecosystem { get; private set; }
        public EcosystemDirectorRuntime EcosystemDirector { get; private set; }
        public WorldQueryRuntime WorldQuery { get; private set; }
        public DayNightRuntime DayNight { get; private set; }
        public GameSnapshotProvider SnapshotProvider { get; private set; }
        public DebugPanelPresenter DebugPanel { get; private set; }
        public WorldMapDebugWindow WorldMapDebug { get; private set; }

        public void Compose(Transform parent)
        {
            Create<GameBootstrapper>("GameBootstrapper", parent);

            Ecosystem = GetOrCreate<EcosystemRuntime>("EcosystemRuntime", parent);
            EcosystemDirector = Ecosystem.GetComponent<EcosystemDirectorRuntime>();
            if (EcosystemDirector == null) EcosystemDirector = Ecosystem.gameObject.AddComponent<EcosystemDirectorRuntime>();
            WorldQuery = Ecosystem.GetComponent<WorldQueryRuntime>();
            if (WorldQuery == null) WorldQuery = Ecosystem.gameObject.AddComponent<WorldQueryRuntime>();

            DayNight = Create<DayNightRuntime>("DayNightRuntime", parent);
            Create<DayNightSkyRuntime>("DayNightSkyRuntime", parent);
            SnapshotProvider = Create<GameSnapshotProvider>("GameSnapshotProvider", parent);
            SnapshotProvider.SetAutoRefresh(RuntimeDebugSettings.DeveloperDiagnosticsEnabled);
            if (RuntimeDebugSettings.DeveloperDiagnosticsEnabled)
            {
                DebugPanel = Create<DebugPanelPresenter>("DebugPanelPresenter", parent);
                WorldMapDebug = Create<WorldMapDebugWindow>("WorldMapDebugWindow", parent);
                Create<WorldGenerationDebugPresenter>("WorldGenerationDebugPresenter", parent);
                DebugPanel.SetSnapshotProvider(SnapshotProvider);
                WorldMapDebug.SetSnapshotProvider(SnapshotProvider);
            }
        }

        private static T Create<T>(string name, Transform parent) where T : Component
        {
            return GetOrCreate<T>(name, parent);
        }

        private static T GetOrCreate<T>(string name, Transform parent) where T : Component
        {
            T existing = parent != null ? parent.GetComponentInChildren<T>(true) : null;
            if (existing != null) return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
