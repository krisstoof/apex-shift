using System.Collections;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class RaftEscapePlayModeTests
    {
        private GameObject world, player;
        private IslandTopographyRuntime topo;
        private BuildingRegistry registry;
        private StoryProgressionRuntime story;
        private PlayerInventoryRuntime inventory;
        private PlayerCraftingRuntime crafting;
        private BuildingPlacementRuntime placement;
        private GameSaveService save;
        private readonly Vector3 launch = new Vector3(4f, 0f, 0f);

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("RaftWorld");
            topo = world.AddComponent<IslandTopographyRuntime>();
            topo.Build(32, 4f, (x, z) => x < 0f, p => 1f, p => "westwood", biomeResolutionPerTile: 2);
            var bounds = world.AddComponent<WorldBounds>();
            bounds.Configure(4f, topo.GetAllLandCenters().Concat(new[] { launch }));
            registry = world.AddComponent<BuildingRegistry>();
            story = world.AddComponent<StoryProgressionRuntime>();
            player = new GameObject("RaftPlayer");
            player.transform.SetParent(world.transform);
            player.transform.position = new Vector3(-8f, 1f, 0f);
            player.AddComponent<CharacterController>();
            player.AddComponent<IsometricPlayerController>();
            player.AddComponent<PlayerSurvivalRuntime>();
            inventory = player.AddComponent<PlayerInventoryRuntime>();
            crafting = player.AddComponent<PlayerCraftingRuntime>();
            placement = player.AddComponent<BuildingPlacementRuntime>();
            placement.SetInventoryRuntime(inventory);
            placement.SetBuildingRegistry(registry);
            placement.SetBuildingParent(world.transform);
            save = world.AddComponent<GameSaveService>();
            story.Signal(StorySignalIds.CrashSurvived);
            story.Signal(StorySignalIds.SurvivalEstablished);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(world); }

        private PlaceableStructureRuntime CraftAndPlace()
        {
            inventory.Inventory.AddItem("wood", 10);
            inventory.Inventory.AddItem("fiber", 6);
            Assert.IsTrue(crafting.CraftRecipe("raft").Succeeded);
            Assert.AreEqual(0, inventory.Inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.Inventory.GetAmount("fiber"));
            Assert.AreEqual(1, inventory.Inventory.GetAmount("raft"));
            Assert.IsTrue(placement.TryPlaceAt("raft", launch - Vector3.up, Quaternion.identity), placement.CurrentValidation.reason);
            Assert.AreEqual(0, inventory.Inventory.GetAmount("raft"));
            var structure = registry.Structures.Single(s => s.BuildingId == "raft");
            Assert.AreEqual(WorldWaterLevel.SurfaceY, structure.transform.position.y);
            Assert.NotNull(structure.Raft);
            return structure;
        }

        [Test]
        public void PlacementRequiresCraftAndRejectsLandDeepOceanAndMissingBounds()
        {
            Assert.IsFalse(placement.TryPlaceAt("raft", launch, Quaternion.identity));
            inventory.Inventory.AddItem("raft", 1);
            Assert.IsFalse(placement.TryPlaceAt("raft", new Vector3(-12f, 0f, 0f), Quaternion.identity));
            Assert.IsFalse(placement.TryPlaceAt("raft", new Vector3(30f, 0f, 0f), Quaternion.identity));
            Assert.IsFalse(placement.TryPlaceAt("raft", new Vector3(70f, 0f, 0f), Quaternion.identity));
            Assert.AreEqual(1, inventory.Inventory.GetAmount("raft"));
            Assert.IsEmpty(registry.Structures);
        }

        [Test]
        public void LaunchRejectsNullNonPlayerSecondActorAndInvalidatedEnvironment()
        {
            var structure = CraftAndPlace();
            GameObject other = new GameObject("NotPlayer");
            try
            {
                Assert.IsFalse(structure.CanInteract(null));
                Assert.IsFalse(structure.Raft.TryLaunch(null));
                Assert.IsFalse(structure.Raft.TryLaunch(other));
                structure.transform.position = new Vector3(-12f, 0f, 0f);
                Assert.IsFalse(structure.Raft.TryLaunch(player));
                structure.transform.position = launch;
                Assert.IsTrue(structure.Interact(player));
                Assert.IsFalse(structure.Raft.TryLaunch(player));
                Assert.IsFalse(structure.Raft.TryLaunch(other));
            }
            finally { Object.DestroyImmediate(other); }
        }

        [UnityTest]
        public IEnumerator CraftPlaceBoardTravelFailureSaveLoadContinuedGameplay()
        {
            int failures = 0, failureTransitions = 0;
            using (GameEventBus.Subscribe(value =>
            {
                if (value.kind == GameplayEventKind.StorySignal && value.signalId == StorySignalIds.RaftEscapeFailed) failures++;
            }))
            {
                story.StageChanged += stage => { if (stage.Id == StoryStageIds.InvestigateHumanTraces) failureTransitions++; };
                var structure = CraftAndPlace();
                Assert.AreEqual("Board raft", structure.Prompt);
                Assert.AreEqual(40, structure.Priority);
                Assert.AreEqual(StoryStageIds.AttemptRaftEscape, story.CurrentStageId);
                float health = player.GetComponent<PlayerSurvivalRuntime>().ToSaveData().Health;
                Assert.IsTrue(structure.Interact(player));
                RaftRuntime raft = structure.Raft;
                Vector3 initial = raft.transform.position;
                Vector3 safe = raft.SafeReturnPoint;
                raft.TickWorldDirection(Vector3.right, 2f);
                Assert.Greater(raft.transform.position.x, initial.x);
                Assert.IsFalse(WorldBounds.Active.Contains(raft.transform.position));
                Assert.IsTrue(topo.WorldBounds.Contains(raft.transform.position));
                Assert.IsFalse(player.GetComponent<IsometricPlayerController>().MovementEnabled);
                float initialDanger = raft.Danger01;
                for (int i = 0; i < 200 && raft.IsAttemptActive; i++) raft.TickWorldDirection(Vector3.right, 0.1f);
                Assert.AreEqual(RaftState.Failed, raft.State);
                Assert.Greater(raft.Danger01, initialDanger);
                Assert.AreEqual(safe, player.transform.position);
                Assert.IsTrue(topo.IsLandAt(safe.x, safe.z));
                Assert.IsTrue(WorldBounds.Active.Contains(safe));
                Assert.AreSame(world.transform, player.transform.parent);
                Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
                Assert.IsTrue(player.GetComponent<IsometricPlayerController>().MovementEnabled);
                Assert.IsFalse(player.GetComponent<IsometricPlayerController>().IsInWater);
                Assert.AreEqual(health, player.GetComponent<PlayerSurvivalRuntime>().ToSaveData().Health);
                Assert.IsEmpty(registry.Structures);
                Assert.AreEqual(1, failures);
                Assert.AreEqual(1, failureTransitions);
                raft.FailEscapeAttempt();
                Assert.AreEqual(1, failures);
                GameSaveData saved = save.CaptureCurrentState();
                Assert.IsEmpty(saved.World.BuildingStates);
                Assert.IsTrue(save.ApplyLoadedState(saved));
                Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
                Assert.IsTrue(story.HasMilestone(StorySignalIds.RaftEscapeFailed));
                Assert.AreEqual(1, failures);
                yield return null;
                Assert.IsTrue(player.GetComponent<IsometricPlayerController>().MovementEnabled);
                Assert.IsFalse(player.GetComponent<PlayerSurvivalRuntime>().IsDead);
            }
        }

        [UnityTest]
        public IEnumerator PlacedRaftRoundTripPreservesIdentityAndDoesNotReplayBuilt()
        {
            var structure = CraftAndPlace();
            string id = structure.InstanceId;
            GameSaveData saved = save.CaptureCurrentState();
            int built = 0;
            using (GameEventBus.Subscribe(value => { if (value.signalId == StorySignalIds.RaftBuilt) built++; }))
            {
                Assert.IsTrue(save.ApplyLoadedState(saved));
                yield return null;
                var restored = registry.Structures.Single(s => s.BuildingId == "raft");
                Assert.AreEqual(id, restored.InstanceId);
                Assert.AreEqual(RaftState.Placed, restored.Raft.State);
                Assert.IsTrue(restored.CanInteract(player));
                Assert.AreEqual(0, built);
                Assert.AreEqual(StoryStageIds.AttemptRaftEscape, story.CurrentStageId);
            }
        }

        [UnityTest]
        public IEnumerator ActiveAttemptSaveReturnsPlayerToLandAndDoesNotResumeRaft()
        {
            var raft = CraftAndPlace().Raft;
            Assert.IsTrue(raft.TryLaunch(player));
            raft.TickWorldDirection(Vector3.right, 3f);
            Assert.IsFalse(WorldBounds.Active.Contains(raft.transform.position));
            Vector3 safe = raft.SafeReturnPoint;
            GameSaveData saved = save.CaptureCurrentState();
            Assert.IsEmpty(saved.World.BuildingStates);
            Assert.AreEqual(safe.x, saved.Survival.posX);
            Assert.AreEqual(safe.z, saved.Survival.posZ);
            Assert.IsTrue(save.ApplyLoadedState(saved));
            yield return null;
            Assert.IsTrue(topo.IsLandAt(player.transform.position.x, player.transform.position.z));
            Assert.IsTrue(player.GetComponent<IsometricPlayerController>().MovementEnabled);
            Assert.IsEmpty(registry.Structures);
            Assert.AreEqual(StoryStageIds.AttemptRaftEscape, story.CurrentStageId);
            Assert.IsFalse(story.HasMilestone(StorySignalIds.RaftEscapeFailed));
            Assert.IsTrue(CraftAndPlace().CanInteract(player));
        }

        [Test]
        public void TurningBackReducesDangerAndRepeatedFailuresDoNotReplaySignal()
        {
            int failures = 0;
            using (GameEventBus.Subscribe(value => { if (value.signalId == StorySignalIds.RaftEscapeFailed) failures++; }))
            {
                var raft = CraftAndPlace().Raft;
                Assert.IsTrue(raft.TryLaunch(player));
                raft.TickWorldDirection(Vector3.right, 5f);
                float danger = raft.Danger01;
                raft.TickWorldDirection(Vector3.left, 1f);
                Assert.Less(raft.Danger01, danger);
                raft.FailEscapeAttempt();
                Assert.AreEqual(1, failures);
                var second = CraftAndPlace().Raft;
                Assert.IsTrue(second.TryLaunch(player));
                for (int i = 0; i < 200 && second.IsAttemptActive; i++) second.TickWorldDirection(Vector3.right, 0.1f);
                Assert.AreEqual(RaftState.Failed, second.State);
                Assert.AreEqual(1, failures);
                Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
            }
        }

        [Test]
        public void PhysicalGridBoundaryFailsBeforeRaftLeavesGeneratedWater()
        {
            var raft = CraftAndPlace().Raft;
            Assert.IsTrue(raft.TryLaunch(player));
            raft.TickWorldDirection(Vector3.forward, 100f);
            Assert.AreEqual(RaftState.Failed, raft.State);
            Assert.AreEqual(1f, raft.Danger01);
            Assert.IsTrue(topo.IsLandAt(player.transform.position.x, player.transform.position.z));
        }
    }
}
