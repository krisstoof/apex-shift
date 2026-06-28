using System.Reflection;
using ApexShift.Runtime.UI.Debugging;
using ApexShift.Runtime.UI.Snapshots;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.UI
{
    public class WorldGeneratorRuntimeSnapshotBootstrapTests
    {
        [Test]
        public void EnsureMethods_CreateSnapshotRuntimeComponents()
        {
            GameObject root = new GameObject("GeneratorRoot");
            try
            {
                WorldGeneratorRuntime generator = root.AddComponent<WorldGeneratorRuntime>();

                InvokePrivate(generator, "EnsureGameSnapshotProvider");
                InvokePrivate(generator, "EnsureDebugPanelPresenter");

                Assert.That(Object.FindObjectsByType<GameSnapshotProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Not.Empty);
                Assert.That(Object.FindObjectsByType<DebugPanelPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
                foreach (GameSnapshotProvider provider in Object.FindObjectsByType<GameSnapshotProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(provider.gameObject);
                }

                foreach (DebugPanelPresenter presenter in Object.FindObjectsByType<DebugPanelPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(presenter.gameObject);
                }
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Could not resolve " + methodName + ".");
            method.Invoke(target, null);
        }
    }
}
