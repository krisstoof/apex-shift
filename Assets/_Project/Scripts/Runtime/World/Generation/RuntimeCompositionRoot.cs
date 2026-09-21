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
        public void Compose(Transform parent)
        {
            Create<GameBootstrapper>("GameBootstrapper", parent);

            EcosystemRuntime ecosystem = GetOrCreate<EcosystemRuntime>("EcosystemRuntime", parent);
            if (ecosystem.GetComponent<EcosystemDirectorRuntime>() == null)
                ecosystem.gameObject.AddComponent<EcosystemDirectorRuntime>();
            if (ecosystem.GetComponent<WorldQueryRuntime>() == null)
                ecosystem.gameObject.AddComponent<WorldQueryRuntime>();

            Create<DayNightRuntime>("DayNightRuntime", parent);
            Create<DayNightSkyRuntime>("DayNightSkyRuntime", parent);
            Create<GameSnapshotProvider>("GameSnapshotProvider", parent);
            Create<DebugPanelPresenter>("DebugPanelPresenter", parent);
            Create<WorldMapDebugWindow>("WorldMapDebugWindow", parent);
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
