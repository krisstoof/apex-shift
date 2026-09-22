using System.IO;
using ApexShift.Presentation.Debugging;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Debug
{
    public sealed class DebugCleanupTests
    {
        [SetUp]
        public void SetUp() => RuntimeDebugSettings.RestoreDefaults();

        [TearDown]
        public void TearDown() => RuntimeDebugSettings.RestoreDefaults();

        [Test]
        public void DebugIsOptInAndCompositionDoesNotCreateDebugPresentersByDefault()
        {
            Assert.IsFalse(RuntimeDebugSettings.DebugEnabled);
            GameObject root = new GameObject("DebugCleanupCompositionRoot");
            try
            {
                var composition = new RuntimeCompositionRoot();
                composition.Compose(root.transform);
                Assert.IsNull(composition.DebugPanel);
                Assert.IsNull(composition.WorldMapDebug);
                Assert.IsEmpty(root.GetComponentsInChildren<WorldGenerationDebugPresenter>(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UIDebuggerIsPresentationOwnedAndDoesNotScanTheSceneEveryFrame()
        {
            Assert.AreEqual("ApexShift.Presentation.Debugging", typeof(UIDebugger).Namespace);
            string source = File.ReadAllText("Assets/_Project/Scripts/Presentation/Debugging/UIDebugger.cs");
            Assert.IsFalse(source.Contains("FindObjectsByType<Button>"));
            Assert.IsTrue(source.Contains("GetComponentsInChildren<Button>"));
        }
    }
}
