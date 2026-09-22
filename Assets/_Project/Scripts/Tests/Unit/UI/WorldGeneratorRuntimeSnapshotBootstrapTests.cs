using ApexShift.Presentation.Debugging;
using ApexShift.Runtime.UI.Snapshots;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.Debugging;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.UI
{
    public class WorldGeneratorRuntimeSnapshotBootstrapTests
    {
        [Test]
        public void RuntimeCompositionRoot_CreatesSnapshotWithoutDiagnostics()
        {
            GameObject root = new GameObject("GeneratorRoot");
            try
            {
                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);
                var composition = new RuntimeCompositionRoot();
                composition.Compose(root.transform);

                Assert.That(composition.SnapshotProvider, Is.Not.Null);
                Assert.IsFalse(composition.SnapshotProvider.AutoRefreshEnabled);
                Assert.IsNull(root.GetComponentInChildren<RuntimeDiagnosticsBootstrap>(true));
                Assert.IsNull(root.GetComponentInChildren<DebugPanelPresenter>(true));
                Assert.IsNull(root.GetComponentInChildren<WorldMapDebugWindow>(true));
                Assert.IsNull(root.GetComponentInChildren<WorldGenerationDebugPresenter>(true));
            }
            finally
            {
                RuntimeDebugSettings.RestoreDefaults();
                Object.DestroyImmediate(root);
                foreach (GameSnapshotProvider provider in Object.FindObjectsByType<GameSnapshotProvider>(FindObjectsInactive.Include))
                {
                    Object.DestroyImmediate(provider.gameObject);
                }

            }
        }
    }
}
