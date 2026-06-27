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
            WorldGeneratorRuntime generator = root.AddComponent<WorldGeneratorRuntime>();

            InvokePrivate(generator, "EnsureGameSnapshotProvider");
            InvokePrivate(generator, "EnsureDebugPanelPresenter");

            Assert.IsNotNull(Object.FindAnyObjectByType<GameSnapshotProvider>());
            Assert.IsNotNull(Object.FindAnyObjectByType<DebugPanelPresenter>());

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(Object.FindAnyObjectByType<GameSnapshotProvider>()?.gameObject);
            Object.DestroyImmediate(Object.FindAnyObjectByType<DebugPanelPresenter>()?.gameObject);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Could not resolve " + methodName + ".");
            method.Invoke(target, null);
        }
    }
}
