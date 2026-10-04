#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using ApexShift.Core.Items;
using ApexShift.Core.Save;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.Story.Clues;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ApexShift.Tests.Regression
{
    public sealed class EscapeBoatPlayModeTests
    {
        [UnityTest]
        public IEnumerator FullProductionPreparationEscapeAndCompletedContinuePersist()
        {
            var owner = new GameObject("BoatAcceptanceWorld"); var ui = new GameObject("UI");
            var generator = Generator(owner);
            float priorTimeScale = Time.timeScale;
            try
            {
                var provisioner = owner.AddComponent<RuntimeHUDProvisioner>(); provisioner.CreateHUD(null); provisioner.enabled = false;
                var startup = ui.GetComponent<GameStartupController>(); startup.enabled = false;
                generator.Generate(); startup.ResumeGame();
                var c = generator.CurrentGeneration; var player = c.Player;
                var story = c.StoryProgression; var boat = c.SmugglerBaseInterior.EscapeBoat;
                Assert.IsFalse(boat.CanInteract(player));
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                GameEventBus.PublishStorySignal(StorySignalIds.SurvivalEstablished);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftBuilt);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftEscapeFailed);
                Assert.IsTrue(StoryClueRegistry.FindById("smuggler_cache_manifest").Interact(player));
                var entrance = LandmarkRegistry.FindById("base_entrance"); entrance.Discover();
                Assert.IsTrue(entrance.GetComponent<ApexShift.Runtime.World.Interiors.BaseEntranceInteractionRuntime>().Interact(player));
                Assert.AreEqual(StoryStageIds.PrepareBoat, story.CurrentStageId);
                Assert.AreSame(c, generator.CurrentGeneration);
                Assert.IsTrue(boat.Interact(player));
                Assert.AreEqual("Missing: Boat Fuel, Boat Battery.", story.ObjectiveDetail);
                var fuel = boat.Sources.Single(s => s.ItemId == EscapeBoatItemIds.Fuel);
                var battery = boat.Sources.Single(s => s.ItemId == EscapeBoatItemIds.Battery);
                Assert.IsTrue(fuel.Interact(player));
                Assert.AreEqual("Missing: Boat Battery.", story.ObjectiveDetail);
                var service = owner.AddComponent<GameSaveService>();
                int prepared = 0, escaped = 0, discovered = 0;
                using (GameEventBus.Subscribe(e => {
                    if (e.kind != GameplayEventKind.StorySignal) return;
                    if (e.signalId == StorySignalIds.BoatPrepared) prepared++;
                    if (e.signalId == StorySignalIds.IslandEscaped) escaped++;
                    if (e.signalId == EscapeBoatMilestoneIds.BoatDiscovered) discovered++;
                }))
                {
                    Assert.IsTrue(service.ApplyLoadedState(service.CaptureCurrentState()));
                    c = generator.CurrentGeneration; player = c.Player; story = c.StoryProgression; boat = c.SmugglerBaseInterior.EscapeBoat;
                    fuel = boat.Sources.Single(s => s.ItemId == EscapeBoatItemIds.Fuel);
                    battery = boat.Sources.Single(s => s.ItemId == EscapeBoatItemIds.Battery);
                    Assert.IsTrue(fuel.IsCollected); Assert.IsFalse(fuel.gameObject.activeSelf); Assert.IsTrue(battery.gameObject.activeInHierarchy);
                    Assert.AreEqual(1, player.GetComponent<PlayerInventoryRuntime>().Inventory.GetAmount(EscapeBoatItemIds.Fuel));
                    Assert.AreEqual("Missing: Boat Battery.", story.ObjectiveDetail);
                    Assert.AreEqual(0, prepared + escaped + discovered);
                    Assert.IsTrue(battery.Interact(player));
                    StringAssert.Contains("Return to the boat", story.CurrentObjectiveText);
                    Assert.AreEqual(1, ui.GetComponentsInChildren<ObjectiveHUDView>(true).Length);
                    StringAssert.Contains("Return to the boat", ui.transform.Find("PlayerHUD/ObjectivePanel/ObjectiveText").GetComponent<Text>().text);
                    Assert.IsTrue(service.ApplyLoadedState(service.CaptureCurrentState()));
                    c = generator.CurrentGeneration; player = c.Player; story = c.StoryProgression; boat = c.SmugglerBaseInterior.EscapeBoat;
                    Assert.IsTrue(boat.Sources.All(s => s.IsCollected && !s.gameObject.activeSelf));
                    Assert.IsFalse(story.HasMilestone(StorySignalIds.BoatPrepared));
                    Assert.IsTrue(boat.Interact(player)); Assert.AreEqual(1, prepared);
                    var inventory = player.GetComponent<PlayerInventoryRuntime>().Inventory;
                    Assert.AreEqual(0, inventory.GetAmount(EscapeBoatItemIds.Fuel)); Assert.AreEqual(0, inventory.GetAmount(EscapeBoatItemIds.Battery));
                    Assert.AreEqual(StoryStageIds.EscapeIsland, story.CurrentStageId); Assert.AreEqual("Escape island", boat.Prompt);
                    Assert.IsTrue(service.ApplyLoadedState(service.CaptureCurrentState()));
                    c = generator.CurrentGeneration; player = c.Player; boat = c.SmugglerBaseInterior.EscapeBoat;
                    Assert.AreEqual(EscapeBoatState.Ready, boat.State); Assert.AreEqual(1, prepared);
                    inventory = player.GetComponent<PlayerInventoryRuntime>().Inventory;
                    inventory.AddItem(EscapeBoatItemIds.Fuel, 1); inventory.AddItem(EscapeBoatItemIds.Battery, 1);
                    Assert.IsTrue(boat.Interact(player)); Assert.IsTrue(boat.EscapeInProgress);
                    Assert.IsFalse(player.GetComponent<IsometricPlayerController>().MovementEnabled);
                    Assert.IsFalse(boat.Interact(player)); Assert.IsFalse(c.SmugglerBaseInterior.TryExit(player));
                    Assert.AreEqual(1, inventory.GetAmount(EscapeBoatItemIds.Fuel)); Assert.AreEqual(1, prepared);
                    var view = ui.GetComponentInChildren<RunCompletionHUDView>(true);
                    Assert.IsTrue(view.IsVisible); StringAssert.Contains("Leaving", view.TitleText);
                    var duringSequence = service.CaptureCurrentState();
                    Assert.IsTrue(service.ApplyLoadedState(duringSequence));
                    c = generator.CurrentGeneration; player = c.Player; story = c.StoryProgression; boat = c.SmugglerBaseInterior.EscapeBoat;
                    Assert.AreEqual(EscapeBoatState.Ready, boat.State); Assert.IsFalse(boat.EscapeInProgress);
                    Assert.IsTrue(player.GetComponent<IsometricPlayerController>().MovementEnabled); Assert.AreEqual(0, escaped);
                    Assert.IsTrue(boat.Interact(player));
                    Time.timeScale = 0f; // Sequence must still finish in realtime.
                    yield return new WaitForSecondsRealtime(2.3f);
                    Assert.AreEqual(1, escaped); Assert.AreEqual(StoryStageIds.Completed, story.CurrentStageId);
                    Assert.AreEqual(EscapeBoatState.Escaped, boat.State); Assert.IsFalse(boat.CanInteract(player));
                    view = ui.GetComponentInChildren<RunCompletionHUDView>(true);
                    Assert.IsTrue(view.IsVisible); Assert.AreEqual("RUN COMPLETE", view.TitleText); Assert.AreEqual("You escaped the island.", view.MessageText);
                    Assert.IsFalse(GameSessionState.IsGameplayActive); Assert.AreEqual(0f, Time.timeScale);
                    var completed = service.CaptureCurrentState();
                    var store = new MemoryStore { Data = completed };
                    typeof(GameSaveService).GetField("saveStore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, store);
                    typeof(GameStartupController).GetField("saveService", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(startup, service);
                    startup.ContinueOrLoadGame();
                    c = generator.CurrentGeneration; boat = c.SmugglerBaseInterior.EscapeBoat;
                    Assert.AreEqual(StoryStageIds.Completed, c.StoryProgression.CurrentStageId);
                    Assert.AreEqual(EscapeBoatState.Escaped, boat.State);
                    Assert.IsTrue(ui.GetComponentInChildren<RunCompletionHUDView>(true).IsVisible);
                    Assert.IsFalse(GameSessionState.IsGameplayActive); Assert.AreEqual(0f, Time.timeScale); Assert.AreEqual(1, escaped);
                    ui.transform.Find("PlayerHUD/RunCompletionPanel/ReturnToMenu").GetComponent<Button>().onClick.Invoke();
                    Assert.IsNull(generator.CurrentGeneration); Assert.IsFalse(GameSessionState.IsGameplayActive);
                }
            }
            finally { Object.DestroyImmediate(ui); generator.ClearGeneratedWorld(); Object.DestroyImmediate(owner); GameSessionState.EnterMainMenu(); Time.timeScale = priorTimeScale; }
        }
        private static WorldGeneratorRuntime Generator(GameObject owner)
        {
            var generator = owner.AddComponent<WorldGeneratorRuntime>(); generator.SetGenerateOnStart(false); generator.SetSeed(81281);
            generator.SetBiomeCatalog(AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>("Assets/_Project/Data/Biomes/BiomeCatalog.asset"));
            generator.SetHabitatVegetationCatalog(AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>("Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset"));
            return generator;
        }
        private sealed class MemoryStore : IGameSaveStore
        {
            public GameSaveData Data;
            public bool Exists(string slotName) => Data != null;
            public void Save(string slotName, GameSaveData data) => Data = data;
            public GameSaveData Load(string slotName) => Data;
            public void Delete(string slotName) => Data = null;
        }
    }
}
#endif
