#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using ApexShift.Runtime.World.Landmarks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ApexShift.Tests.Regression
{
    public sealed class StoryProgressionPlayModeTests
    {
        [Test]
        public void BusDiscoveryAndSignalsRespectRuntimeEnableDisableAndDestruction()
        {
            GameObject root = new GameObject("StoryBusLifecycle");
            try
            {
                StoryProgressionRuntime story = root.AddComponent<StoryProgressionRuntime>();
                LandmarkRuntime landmark = root.AddComponent<LandmarkRuntime>();
                landmark.Configure("smuggler_cache", LandmarkType.SmugglerCache, "Cache");
                int milestones = 0;
                story.MilestoneCompleted += _ => milestones++;
                Assert.IsTrue(landmark.Discover());
                Assert.IsFalse(landmark.Discover());
                Assert.IsTrue(story.HasMilestone(StoryMilestoneIds.LandmarkDiscovered("smuggler_cache")));
                landmark.ApplySaveData(landmark.ToSaveData());
                Assert.AreEqual(1, milestones);
                GameEventBus.Publish(new GameplayEvent(GameplayEventKind.ResourceHarvested, Vector3.zero));
                Assert.AreEqual(1, milestones);
                story.enabled = false;
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(StoryStageIds.SurviveCrash, story.CurrentStageId);
                story.enabled = true;
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(StoryStageIds.EstablishSurvival, story.CurrentStageId);
                Assert.AreEqual(2, milestones);
                Object.DestroyImmediate(story);
                GameEventBus.PublishStorySignal(StorySignalIds.SurvivalEstablished);
                Assert.AreEqual(2, milestones);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ProvisionerHidesMenuObjectiveAndBindsGeneratedAndRestoredObjective()
        {
            GameObject generatorObject = new GameObject("StoryHudGeneration");
            GameObject uiRoot = new GameObject("UI");
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            ConfigureProduction(generator);
            try
            {
                RuntimeHUDProvisioner provisioner = generatorObject.AddComponent<RuntimeHUDProvisioner>();
                provisioner.CreateHUD(null);
                Transform menuObjective = uiRoot.transform.Find("PlayerHUD/ObjectivePanel");
                Assert.NotNull(menuObjective);
                Assert.IsFalse(menuObjective.gameObject.activeSelf);
                generator.Generate();
                Transform panel = uiRoot.transform.Find("PlayerHUD/ObjectivePanel");
                Assert.IsTrue(panel.gameObject.activeInHierarchy);
                Assert.AreEqual(1, uiRoot.GetComponentsInChildren<ObjectiveHUDView>(true).Length);
                Text objective = panel.Find("ObjectiveText").GetComponent<Text>();
                StoryProgressionRuntime story = generator.CurrentGeneration.StoryProgression;
                Assert.AreEqual(story.CurrentObjectiveText, objective.text);
                story.Signal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(story.CurrentObjectiveText, objective.text);
                story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.LocateSmugglerBase });
                Assert.AreEqual("Follow the smugglers' trail.", objective.text);
            }
            finally
            {
                Object.DestroyImmediate(uiRoot);
                generator.ClearGeneratedWorld();
                Object.DestroyImmediate(generatorObject);
                ApexShift.Runtime.Flow.GameSessionState.EnterMainMenu();
                Time.timeScale = 1f;
            }
        }

        [Test]
        public void ObjectiveViewUpdatesImmediatelyOnTransitionRestoreRebindAndUnbind()
        {
            GameObject firstObject = new GameObject("FirstStory");
            GameObject secondObject = new GameObject("SecondStory");
            GameObject viewObject = new GameObject("Objective", typeof(RectTransform));
            try
            {
                StoryProgressionRuntime first = firstObject.AddComponent<StoryProgressionRuntime>();
                StoryProgressionRuntime second = secondObject.AddComponent<StoryProgressionRuntime>();
                Text label = viewObject.AddComponent<Text>();
                ObjectiveHUDView view = viewObject.AddComponent<ObjectiveHUDView>();
                view.Configure(label);
                view.Bind(first);
                Assert.AreEqual(first.CurrentObjectiveText, label.text);
                first.Signal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(first.CurrentObjectiveText, label.text);
                first.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.LocateSmugglerBase });
                Assert.AreEqual("Follow the smugglers' trail.", label.text);
                view.Bind(second);
                first.Signal(StoryMilestoneIds.LandmarkDiscovered("base_entrance"));
                Assert.AreEqual(second.CurrentObjectiveText, label.text);
                view.enabled = false;
                second.Signal(StorySignalIds.CrashSurvived);
                Assert.AreNotEqual(second.CurrentObjectiveText, label.text);
                view.enabled = true;
                Assert.AreEqual(second.CurrentObjectiveText, label.text);
                view.Bind(null);
                Assert.IsEmpty(label.text);
                Assert.IsFalse(label.enabled);
                second.Signal(StorySignalIds.SurvivalEstablished);
                Assert.IsEmpty(label.text);
                Object.DestroyImmediate(viewObject);
                second.Signal(StorySignalIds.RaftBuilt);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (viewObject != null) Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [UnityTest]
        public IEnumerator GenerateClearGenerateCreatesFreshStoryWithoutGhostSubscriptions()
        {
            GameObject generatorObject = new GameObject("StoryGeneration");
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            ConfigureProduction(generator);
            try
            {
                generator.Generate();
                StoryProgressionRuntime first = generator.CurrentGeneration.StoryProgression;
                Assert.NotNull(first);
                Assert.IsTrue(first.transform.IsChildOf(generator.CurrentGeneration.GenerationRoot));
                Assert.AreEqual(StoryStageIds.SurviveCrash, first.CurrentStageId);
                Assert.IsEmpty(first.CompletedMilestones);
                LandmarkRuntime crash = generator.CurrentGeneration.LandmarkRoot.GetComponentsInChildren<LandmarkRuntime>()
                    .Single(landmark => landmark.LandmarkId == "plane_crash");
                Assert.IsTrue(crash.IsDiscovered);
                int firstCompletions = 0;
                first.MilestoneCompleted += _ => firstCompletions++;
                first.Signal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(1, firstCompletions);
                generator.ClearGeneratedWorld();
                yield return null;
                Assert.IsTrue(first == null);
                GameEventBus.PublishStorySignal(StorySignalIds.SurvivalEstablished);
                Assert.AreEqual(1, firstCompletions);
                generator.Generate();
                StoryProgressionRuntime second = generator.CurrentGeneration.StoryProgression;
                Assert.AreNotSame(first, second);
                Assert.AreEqual(1, generator.CurrentGeneration.GenerationRoot.GetComponentsInChildren<StoryProgressionRuntime>().Length);
                Assert.AreEqual(StoryStageIds.SurviveCrash, second.CurrentStageId);
                Assert.IsEmpty(second.CompletedMilestones);
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                Assert.AreEqual(StoryStageIds.EstablishSurvival, second.CurrentStageId);
                Assert.AreEqual(1, firstCompletions);
            }
            finally
            {
                generator.ClearGeneratedWorld();
                Object.DestroyImmediate(generatorObject);
            }
        }

        [UnityTest]
        public IEnumerator SaveLoadRestoresLandmarkStoryAndBoundHudWithoutCompletionReplay()
        {
            GameObject generatorObject = new GameObject("StorySaveLoad");
            GameObject serviceObject = new GameObject("StorySaveService");
            GameObject viewObject = new GameObject("LoadObjective", typeof(RectTransform));
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            ConfigureProduction(generator);
            System.IDisposable listener = null;
            try
            {
                Text label = viewObject.AddComponent<Text>();
                ObjectiveHUDView view = viewObject.AddComponent<ObjectiveHUDView>();
                view.Configure(label);
                int restoredStageEvents = 0, restoredMilestoneEvents = 0, restoredStateEvents = 0;
                generator.OnGenerationComplete += player =>
                {
                    StoryProgressionRuntime runtime = generator.CurrentGeneration.StoryProgression;
                    view.Bind(runtime);
                    runtime.StageChanged += _ => restoredStageEvents++;
                    runtime.MilestoneCompleted += _ => restoredMilestoneEvents++;
                    runtime.StateChanged += () => restoredStateEvents++;
                };
                generator.Generate();
                StoryProgressionRuntime first = generator.CurrentGeneration.StoryProgression;
                first.Signal(StorySignalIds.CrashSurvived);
                first.Signal(StorySignalIds.SurvivalEstablished);
                first.Signal(StorySignalIds.RaftBuilt);
                first.Signal(StorySignalIds.RaftEscapeFailed);
                LandmarkRuntime cache = FindCache(generator);
                Assert.IsTrue(cache.Discover());
                Assert.AreEqual(StoryStageIds.LocateSmugglerBase, first.CurrentStageId);
                GameSaveService service = serviceObject.AddComponent<GameSaveService>();
                GameSaveData saved = service.CaptureCurrentState();
                Assert.AreEqual(first.CurrentStageId, saved.World.StoryState.CurrentStageId);
                Assert.IsTrue(saved.World.LandmarkStates.Single(landmark => landmark.LandmarkId == "smuggler_cache").Discovered);
                int busCompletions = 0;
                listener = GameEventBus.Subscribe(value =>
                {
                    if (value.kind == GameplayEventKind.StorySignal || value.kind == GameplayEventKind.LandmarkDiscovered)
                        busCompletions++;
                });
                restoredStageEvents = restoredMilestoneEvents = restoredStateEvents = 0;
                Assert.IsTrue(service.ApplyLoadedState(saved));
                StoryProgressionRuntime restored = generator.CurrentGeneration.StoryProgression;
                Assert.IsTrue(first == null);
                Assert.AreEqual(StoryStageIds.LocateSmugglerBase, restored.CurrentStageId);
                CollectionAssert.AreEquivalent(saved.World.StoryState.CompletedMilestoneIds, restored.CompletedMilestones);
                Assert.IsTrue(FindCache(generator).IsDiscovered);
                Assert.IsFalse(FindCache(generator).Discover());
                Assert.AreEqual(0, busCompletions);
                Assert.AreEqual(0, restoredStageEvents);
                Assert.AreEqual(0, restoredMilestoneEvents);
                Assert.AreEqual(1, restoredStateEvents);
                Assert.AreEqual(restored.CurrentObjectiveText, label.text);
                Assert.AreEqual("Follow the smugglers' trail.", label.text);
                GameSaveData recaptured = service.CaptureCurrentState();
                CollectionAssert.AreEqual(saved.World.StoryState.CompletedMilestoneIds, recaptured.World.StoryState.CompletedMilestoneIds);
                yield return null;
            }
            finally
            {
                listener?.Dispose();
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(serviceObject);
                generator.ClearGeneratedWorld();
                Object.DestroyImmediate(generatorObject);
            }
        }

        [Test]
        public void SaveServiceWithoutGeneratorUsesStandaloneStoryAndDefaultsWhenAbsent()
        {
            GameObject root = new GameObject("StandaloneStorySave");
            try
            {
                StoryProgressionRuntime story = root.AddComponent<StoryProgressionRuntime>();
                GameSaveService service = root.AddComponent<GameSaveService>();
                story.Signal(StorySignalIds.CrashSurvived);
                GameSaveData saved = service.CaptureCurrentState();
                story.RestoreSaveData(StorySaveData.Default);
                Assert.IsTrue(service.ApplyLoadedState(saved));
                Assert.AreEqual(StoryStageIds.EstablishSurvival, story.CurrentStageId);
                Object.DestroyImmediate(story);
                Assert.AreEqual(StoryStageIds.SurviveCrash, service.CaptureCurrentState().World.StoryState.CurrentStageId);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static LandmarkRuntime FindCache(WorldGeneratorRuntime generator)
            => generator.CurrentGeneration.LandmarkRoot.GetComponentsInChildren<LandmarkRuntime>()
                .Single(landmark => landmark.LandmarkId == "smuggler_cache");

        private static void ConfigureProduction(WorldGeneratorRuntime generator)
        {
            BiomeCatalogAsset biomes = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(
                "Assets/_Project/Data/Biomes/BiomeCatalog.asset");
            HabitatVegetationCatalogAsset vegetation = AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(
                "Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset");
            Assert.NotNull(biomes);
            Assert.NotNull(vegetation);
            generator.SetBiomeCatalog(biomes);
            generator.SetHabitatVegetationCatalog(vegetation);
            generator.SetSeed(12345);
        }
    }
}
#endif
