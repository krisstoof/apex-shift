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
                Assert.IsFalse(composition.SnapshotProvider.AutoRefreshEnabled);
                Assert.IsNull(root.GetComponentInChildren<RuntimeDiagnosticsBootstrap>(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DiagnosticsPolicyRequiresEditorOrDevelopmentBuild()
        {
            Assert.IsFalse(RuntimeDiagnosticsPolicy.Resolve(false, false, true));
            Assert.IsTrue(RuntimeDiagnosticsPolicy.Resolve(true, false, true));
            Assert.IsTrue(RuntimeDiagnosticsPolicy.Resolve(false, true, true));
            Assert.IsFalse(RuntimeDiagnosticsPolicy.Resolve(true, true, false));
        }

        [Test]
        public void FreeBuildAndCraftingRequireDeveloperDiagnostics()
        {
            RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(false);
            RuntimeDebugSettings.SetFreeBuildingEnabled(true);
            RuntimeDebugSettings.SetFreeCraftingEnabled(true);
            Assert.IsFalse(RuntimeDebugSettings.FreeBuildingEnabled);
            Assert.IsFalse(RuntimeDebugSettings.FreeCraftingEnabled);

            RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);
            Assert.IsTrue(RuntimeDebugSettings.FreeBuildingEnabled);
            Assert.IsTrue(RuntimeDebugSettings.FreeCraftingEnabled);
        }

        [Test]
        public void UIDebuggerIsPresentationOwnedAndDoesNotScanTheSceneEveryFrame()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Presentation/Debugging/UIDebugger.cs");
            Assert.IsFalse(source.Contains("GetComponentsInChildren<Button>"));
            Assert.IsFalse(source.Contains("FindObjectsByType<Button>"));
            Assert.IsTrue(source.IndexOf("wasPressedThisFrame") < source.IndexOf("RaycastAll"));
        }

        [Test]
        public void RuntimeOwnersDoNotContainPresentationDiagnostics()
        {
            string generator = File.ReadAllText("Assets/_Project/Scripts/Runtime/World/Generation/WorldGeneratorRuntime.cs");
            string composition = File.ReadAllText("Assets/_Project/Scripts/Runtime/World/Generation/RuntimeCompositionRoot.cs");
            string hud = File.ReadAllText("Assets/_Project/Scripts/Presentation/HUD/RuntimeHUDProvisioner.cs");

            Assert.IsFalse(generator.Contains("PlayerActionDebugLog"));
            Assert.IsFalse(generator.Contains("CreatureDebugOverlay"));
            Assert.IsFalse(generator.Contains("WorldGenerationDebugPresenter"));
            Assert.IsFalse(generator.Contains("RuntimeDebugSettings.DeveloperDiagnosticsEnabled"));
            Assert.IsFalse(generator.Contains("RuntimeDebugSettings.DebugEnabled"));
            Assert.IsFalse(composition.Contains("DebugPanelPresenter"));
            Assert.IsFalse(composition.Contains("WorldMapDebugWindow"));
            Assert.IsFalse(composition.Contains("WorldGenerationDebugPresenter"));
            Assert.IsFalse(hud.Contains("UIDebugger"));
        }
    }
}
