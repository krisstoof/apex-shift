using ApexShift.Runtime.UI.Debugging;
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
        public void RuntimeCompositionRoot_CreatesSnapshotAndDebugComponents()
        {
            GameObject root = new GameObject("GeneratorRoot");
            try
            {
                RuntimeDebugSettings.SetDebugEnabled(true);
                var composition = new RuntimeCompositionRoot();
                composition.Compose(root.transform);

                Assert.That(Object.FindObjectsByType<GameSnapshotProvider>(FindObjectsInactive.Include), Is.Not.Empty);
                Assert.That(Object.FindObjectsByType<DebugPanelPresenter>(FindObjectsInactive.Include), Is.Not.Empty);
            }
            finally
            {
                RuntimeDebugSettings.RestoreDefaults();
                Object.DestroyImmediate(root);
                foreach (GameSnapshotProvider provider in Object.FindObjectsByType<GameSnapshotProvider>(FindObjectsInactive.Include))
                {
                    Object.DestroyImmediate(provider.gameObject);
                }

                foreach (DebugPanelPresenter presenter in Object.FindObjectsByType<DebugPanelPresenter>(FindObjectsInactive.Include))
                {
                    Object.DestroyImmediate(presenter.gameObject);
                }
            }
        }
    }
}
