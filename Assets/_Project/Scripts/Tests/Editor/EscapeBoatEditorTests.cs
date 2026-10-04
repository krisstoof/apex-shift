using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Interiors;
using ApexShift.Runtime.World.Landmarks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ApexShift.Tests.Editor
{
    public sealed class EscapeBoatEditorTests
    {
        [Test]
        public void ExactlyTwoNormalNonEdibleItemsHaveMatchingProductionAssets()
        {
            var database = ItemDatabase.CreateDefault();
            Assert.AreEqual(2, BoatRequirementDefinition.Production.Count);
            foreach (var requirement in BoatRequirementDefinition.Production)
            {
                Assert.IsTrue(database.HasItem(requirement.ItemId));
                Assert.AreEqual(1, database.GetMaxStack(requirement.ItemId)); Assert.IsFalse(database.IsEdible(requirement.ItemId));
                var asset = AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>("Assets/_Project/Data/Items/" + requirement.ItemId + ".asset");
                Assert.NotNull(asset); Assert.AreEqual(requirement.ItemId, asset.ToCoreDefinition().Id.ToString());
                Assert.AreEqual(1, asset.ToCoreDefinition().MaxStackSize);
            }
        }
        [Test]
        public void SaveDtoNormalizesDeduplicatesSortsAndOldSavesDefault()
        {
            var dto = new EscapeBoatSaveData { collectedRequirementSourceIds = new List<string> { " SMUGGLER_BASE_FUEL ", "", null, "smuggler_base_fuel", "smuggler_base_battery" } };
            CollectionAssert.AreEqual(new[] { "smuggler_base_battery", "smuggler_base_fuel" }, dto.CollectedRequirementSourceIds);
            Assert.IsEmpty(JsonUtility.FromJson<WorldSaveData>("{}").EscapeBoatState.CollectedRequirementSourceIds);
        }
        [Test]
        public void ConsumptionIsAtomicAndListenersObserveOnlyTheCompletedCost()
        {
            var inventory = new InventoryState(ItemDatabase.CreateDefault());
            var cost = BoatRequirementDefinition.Production.ToDictionary(r => r.ItemId, _ => 1);
            inventory.AddItem(EscapeBoatItemIds.Fuel, 1);
            Assert.IsFalse(inventory.TryConsumeItems(cost)); Assert.AreEqual(1, inventory.GetAmount(EscapeBoatItemIds.Fuel));
            inventory.AddItem(EscapeBoatItemIds.Battery, 1);
            int changed = 0;
            inventory.InventoryChanged += () => {
                changed++; Assert.AreEqual(0, inventory.GetAmount(EscapeBoatItemIds.Fuel));
                Assert.AreEqual(0, inventory.GetAmount(EscapeBoatItemIds.Battery));
            };
            Assert.IsTrue(inventory.TryConsumeItems(cost)); Assert.AreEqual(1, changed);
            Assert.IsFalse(inventory.TryConsumeItems(cost)); Assert.AreEqual(1, changed);
        }
        [Test]
        public void ObjectiveOwnerAndRestoreDoNotPersistPresentation()
        {
            var root = new GameObject("ObjectiveOwnerTest");
            try
            {
                var story = root.AddComponent<StoryProgressionRuntime>();
                story.SetObjectiveDetail("escape_boat", "Missing: Boat Battery."); story.ClearObjectiveDetail("other");
                StringAssert.Contains("Boat Battery", story.CurrentObjectiveText);
                StringAssert.DoesNotContain("Missing", JsonUtility.ToJson(story.CaptureSaveData()));
                story.RestoreSaveData(story.CaptureSaveData()); Assert.IsEmpty(story.ObjectiveDetail);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void ProductionAnchorsHaveExactlyOneBoatAndTwoSources()
        {
            using (var f = new Fixture())
            {
                Assert.AreEqual(2, f.Boat.Sources.Count);
                Assert.AreSame(f.Interior.FuelAnchor, f.Fuel.transform.parent);
                Assert.AreSame(f.Interior.BatteryAnchor, f.Battery.transform.parent);
                Assert.AreSame(f.Interior.BoatAnchor, f.Boat.transform.parent);
                Assert.AreEqual(1, f.Root.GetComponentsInChildren<EscapeBoatRuntime>(true).Length);
                Assert.IsFalse(f.Boat.Interact(null)); Assert.IsFalse(f.Fuel.Interact(f.Root));
                Assert.IsFalse(f.Boat.CanInteract(f.Player));
            }
        }
        [TestCase(0)]
        [TestCase(1)]
        public void SourcesRejectFullInventoryAndNeverDuplicateOnRepeatOrRestore(int index)
        {
            using (var f = new Fixture())
            {
                f.Enter();
                var source = f.Boat.Sources[index]; var other = f.Boat.Sources[1 - index];
                f.Inventory.AddItem("wood", f.Inventory.SlotCount * 20);
                Assert.IsFalse(source.Interact(f.Player)); Assert.IsFalse(source.IsCollected);
                f.Inventory.RemoveItem("wood", 20);
                Assert.IsTrue(source.Interact(f.Player)); Assert.IsTrue(source.IsCollected);
                Assert.IsFalse(source.gameObject.activeSelf); Assert.IsFalse(source.Interact(f.Player));
                Assert.AreEqual(1, f.Inventory.GetAmount(source.ItemId));
                var saved = f.Boat.CaptureSaveData(); f.Boat.RestoreSaveData(saved);
                Assert.AreEqual(1, f.Inventory.GetAmount(source.ItemId));
                Assert.IsFalse(source.gameObject.activeSelf); Assert.IsTrue(other.gameObject.activeSelf);
            }
        }
        [Test]
        public void DiscoveryObjectivePartialCostAndPreparationUseEventsExactlyOnce()
        {
            using (var f = new Fixture())
            {
                f.Enter(); int discovered = 0, prepared = 0;
                using (GameEventBus.Subscribe(e => { if (e.kind != GameplayEventKind.StorySignal) return;
                    if (e.signalId == EscapeBoatMilestoneIds.BoatDiscovered) discovered++;
                    if (e.signalId == StorySignalIds.BoatPrepared) prepared++; }))
                {
                    Assert.IsTrue(f.Boat.Interact(f.Player)); Assert.AreEqual(1, discovered);
                    Assert.AreEqual(EscapeBoatState.MissingRequirements, f.Boat.State);
                    Assert.AreEqual("Missing: Boat Fuel, Boat Battery.", f.Story.ObjectiveDetail);
                    f.Fuel.Interact(f.Player); Assert.AreEqual("Missing: Boat Battery.", f.Story.ObjectiveDetail);
                    Assert.IsFalse(f.Boat.Interact(f.Player)); Assert.AreEqual(1, f.Inventory.GetAmount(EscapeBoatItemIds.Fuel));
                    f.Battery.Interact(f.Player); StringAssert.Contains("Return to the boat", f.Story.CurrentObjectiveText);
                    Assert.IsFalse(f.Story.HasMilestone(StorySignalIds.BoatPrepared));
                    Assert.IsTrue(f.Boat.Interact(f.Player)); Assert.AreEqual(1, prepared);
                    Assert.AreEqual(StoryStageIds.EscapeIsland, f.Story.CurrentStageId); Assert.IsEmpty(f.Story.ObjectiveDetail);
                    Assert.AreEqual(0, f.Inventory.GetAmount(EscapeBoatItemIds.Fuel)); Assert.AreEqual(0, f.Inventory.GetAmount(EscapeBoatItemIds.Battery));
                    Assert.AreEqual(EscapeBoatState.Ready, f.Boat.State); Assert.AreEqual("Escape island", f.Boat.Prompt);
                    f.Boat.RestoreSaveData(f.Boat.CaptureSaveData()); Assert.AreEqual(1, prepared);
                }
            }
        }
        [Test]
        public void BatteryFirstShowsFuelMissingAndFirstInspectionNeverConsumesItems()
        {
            using (var f = new Fixture())
            {
                f.Enter(); f.Battery.Interact(f.Player); f.Boat.Interact(f.Player);
                Assert.AreEqual("Missing: Boat Fuel.", f.Story.ObjectiveDetail);
                f.Fuel.Interact(f.Player);
                f.Story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.PrepareBoat,
                    completedMilestoneIds = new List<string> { StorySignalIds.BaseAccessGained } });
                Assert.IsTrue(f.Boat.Interact(f.Player)); Assert.AreEqual(1, f.Inventory.GetAmount(EscapeBoatItemIds.Fuel));
                Assert.IsFalse(f.Story.HasMilestone(StorySignalIds.BoatPrepared));
            }
        }
        [Test]
        public void InactiveInteriorStillUpdatesObjectiveThroughInventoryEvents()
        {
            using (var f = new Fixture())
            {
                f.Enter(); f.Boat.Interact(f.Player); Assert.IsTrue(f.Interior.TryExit(f.Player));
                Assert.IsFalse(f.Interior.InteriorRoot.gameObject.activeSelf);
                f.Inventory.AddItem(EscapeBoatItemIds.Fuel, 1);
                Assert.AreEqual("Missing: Boat Battery.", f.Story.ObjectiveDetail);
                f.Inventory.AddItem(EscapeBoatItemIds.Battery, 1);
                StringAssert.Contains("Return to the boat", f.Story.CurrentObjectiveText);
            }
        }
        [Test]
        public void OldPreparedSaveRemainsReadyAndSourceRestoreNeverReplaysGameplay()
        {
            using (var f = new Fixture())
            {
                f.Enter(); int signals = 0;
                using (GameEventBus.Subscribe(e => { if (e.kind == GameplayEventKind.StorySignal) signals++; }))
                {
                    f.Boat.RestoreSaveData(null); Assert.IsTrue(f.Boat.Sources.All(s => !s.IsCollected && s.gameObject.activeSelf));
                    f.Story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.EscapeIsland,
                        completedMilestoneIds = new List<string> { StorySignalIds.BaseAccessGained, StorySignalIds.BoatPrepared } });
                    f.Boat.RestoreSaveData(null);
                    Assert.AreEqual(EscapeBoatState.Ready, f.Boat.State); Assert.AreEqual("Escape island", f.Boat.Prompt);
                    Assert.AreEqual(0, f.Inventory.GetAmount(EscapeBoatItemIds.Fuel)); Assert.AreEqual(0, signals);
                    f.Story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.Completed,
                        completedMilestoneIds = new List<string> { StorySignalIds.IslandEscaped } });
                    Assert.AreEqual(EscapeBoatState.Escaped, f.Boat.State); Assert.IsFalse(f.Boat.Interact(f.Player));
                    Assert.AreEqual(0, signals); Assert.IsFalse(GameSessionState.IsGameplayActive);
                }
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root, Player;
            public readonly StoryProgressionRuntime Story;
            public readonly SmugglerBaseInteriorRuntime Interior;
            public EscapeBoatRuntime Boat => Interior.EscapeBoat;
            public InventoryState Inventory => Player.GetComponent<PlayerInventoryRuntime>().Inventory;
            public BoatRequirementSourceRuntime Fuel => Boat.Sources[0];
            public BoatRequirementSourceRuntime Battery => Boat.Sources[1];
            private readonly float timeScale = Time.timeScale;
            private readonly bool gameplayActive = GameSessionState.IsGameplayActive;
            public Fixture()
            {
                Root = new GameObject("BoatTestGeneration");
                Story = Root.AddComponent<StoryProgressionRuntime>();
                typeof(StoryProgressionRuntime).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Story, null);
                Player = new GameObject("BoatTestPlayer"); Player.transform.SetParent(Root.transform);
                Player.AddComponent<IsometricPlayerController>(); Player.AddComponent<PlayerInventoryRuntime>();
                var entrance = new GameObject("Entrance"); entrance.transform.SetParent(Root.transform);
                entrance.AddComponent<LandmarkRuntime>().Configure("base_entrance", LandmarkType.BaseEntrance, "Entrance", isDiscovered: true);
                var interiorRoot = new GameObject("InteriorRoot").transform; interiorRoot.SetParent(Root.transform);
                var context = new WorldGenerationContext(5, Root.transform) { InteriorRoot = interiorRoot, Player = Player, StoryProgression = Story };
                Interior = SmugglerBaseInteriorBuilder.Build(context); Boat.BindPlayer(Player);
            }
            public void Enter()
            {
                Story.RestoreSaveData(new StorySaveData { currentStageId = StoryStageIds.GainBaseAccess });
                Assert.IsTrue(Interior.TryEnter(Player)); Assert.AreEqual(StoryStageIds.PrepareBoat, Story.CurrentStageId);
            }
            public void Dispose()
            {
                typeof(EscapeBoatRuntime).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Boat, null);
                typeof(StoryProgressionRuntime).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Story, null);
                Object.DestroyImmediate(Root); Time.timeScale = timeScale;
                if (gameplayActive) GameSessionState.BeginGameplay(); else GameSessionState.EndGameplay();
            }
        }
    }
}
