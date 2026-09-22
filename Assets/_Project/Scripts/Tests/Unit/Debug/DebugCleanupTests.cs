using System.IO;
using System.Reflection;
using ApexShift.Presentation.Debugging;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.UI.Snapshots;
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
        public void DeveloperDiagnosticsChangedPublishesOnlyEffectiveTransitions()
        {
            int notifications = 0;
            bool lastValue = false;
            System.Action<bool> handler = value => { notifications++; lastValue = value; };
            RuntimeDebugSettings.DeveloperDiagnosticsChanged += handler;
            try
            {
                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(false);
                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(false);
                Assert.AreEqual(0, notifications);

                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);
                Assert.AreEqual(1, notifications);
                Assert.IsTrue(lastValue);

                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);
                Assert.AreEqual(1, notifications);
            }
            finally
            {
                RuntimeDebugSettings.DeveloperDiagnosticsChanged -= handler;
                RuntimeDebugSettings.RestoreDefaults();
            }
        }

        [Test]
        public void DiagnosticsBootstrapRebindsWithoutWorldRegeneration()
        {
            GameObject generatorObject = new GameObject("DiagnosticsGenerator");
            GameObject generationRoot = new GameObject("GenerationRoot");
            GameObject player = new GameObject("Player");
            GameObject ui = new GameObject("UI");
            GameObject creature = new GameObject("Creature");
            try
            {
                WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
                WorldGenerationContext context = new WorldGenerationContext(1, generationRoot.transform) { Player = player };
                typeof(WorldGeneratorRuntime).GetField("_generationContext", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(generator, context);
                generationRoot.AddComponent<GameSnapshotProvider>();
                EcosystemRuntime ecosystem = generationRoot.AddComponent<EcosystemRuntime>();
                CreatureAgentView creatureView = creature.AddComponent<CreatureAgentView>();
                ecosystem.RegisterCreature(creatureView);

                RuntimeDiagnosticsBootstrap bootstrap = generatorObject.AddComponent<RuntimeDiagnosticsBootstrap>();
                bootstrap.Configure(generator);
                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);

                Assert.IsTrue(generationRoot.GetComponent<GameSnapshotProvider>().AutoRefreshEnabled);
                Assert.IsNotNull(player.GetComponent<PlayerActionDebugLog>());
                Assert.IsNotNull(ui.GetComponent<UIDebugger>());
                Assert.IsNotNull(generationRoot.GetComponent<DebugPanelPresenter>());
                Assert.IsNotNull(generationRoot.GetComponent<WorldMapDebugWindow>());
                Assert.IsNotNull(generationRoot.GetComponent<WorldGenerationDebugPresenter>());
                Assert.AreEqual(1, generationRoot.GetComponents<EcosystemDebugOverlay>().Length);
                Assert.IsNotNull(creature.GetComponent<CreatureDebugOverlay>());

                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(false);

                Assert.IsFalse(generationRoot.GetComponent<GameSnapshotProvider>().AutoRefreshEnabled);
                Assert.IsNull(player.GetComponent<PlayerActionDebugLog>());
                Assert.IsNull(ui.GetComponent<UIDebugger>());
                Assert.IsNull(generationRoot.GetComponent<DebugPanelPresenter>());
                Assert.IsNull(generationRoot.GetComponent<WorldMapDebugWindow>());
                Assert.IsNull(generationRoot.GetComponent<WorldGenerationDebugPresenter>());
                Assert.IsEmpty(generationRoot.GetComponents<EcosystemDebugOverlay>());
                Assert.IsNull(creature.GetComponent<CreatureDebugOverlay>());

                RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true);
                Assert.IsNotNull(player.GetComponent<PlayerActionDebugLog>());
                Assert.IsNotNull(ui.GetComponent<UIDebugger>());
                Assert.AreEqual(1, generationRoot.GetComponents<EcosystemDebugOverlay>().Length);
                Assert.IsNotNull(creature.GetComponent<CreatureDebugOverlay>());
            }
            finally
            {
                RuntimeDebugSettings.RestoreDefaults();
                Object.DestroyImmediate(generatorObject);
                Object.DestroyImmediate(generationRoot);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(ui);
                Object.DestroyImmediate(creature);
            }
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
            string ecosystem = File.ReadAllText("Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemRuntime.cs");

            Assert.IsFalse(generator.Contains("PlayerActionDebugLog"));
            Assert.IsFalse(generator.Contains("CreatureDebugOverlay"));
            Assert.IsFalse(generator.Contains("WorldGenerationDebugPresenter"));
            Assert.IsFalse(generator.Contains("RuntimeDebugSettings.DeveloperDiagnosticsEnabled"));
            Assert.IsFalse(generator.Contains("RuntimeDebugSettings.DebugEnabled"));
            Assert.IsFalse(composition.Contains("DebugPanelPresenter"));
            Assert.IsFalse(composition.Contains("WorldMapDebugWindow"));
            Assert.IsFalse(composition.Contains("WorldGenerationDebugPresenter"));
            Assert.IsFalse(hud.Contains("UIDebugger"));
            Assert.IsFalse(ecosystem.Contains("OnGUI"));
            Assert.IsFalse(ecosystem.Contains("GUI.Box"));
            Assert.IsFalse(ecosystem.Contains("showDebugOverlay"));
            Assert.IsFalse(ecosystem.Contains("Ecosystem Debug"));
        }
    }
}
