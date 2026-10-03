using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.Story.Clues;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Editor
{
    public sealed class StoryClueTests
    {
        private GameObject root;
        private StoryProgressionRuntime story;
        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearForTests();
            StoryClueRegistry.ClearForTests();
            LandmarkRegistry.ClearForTests();
            root = new GameObject("StoryClueTests");
            story = root.AddComponent<StoryProgressionRuntime>();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            StoryClueRegistry.ClearForTests();
            LandmarkRegistry.ClearForTests();
            GameEventBus.ClearForTests();
        }
        private StoryClueRuntime Clue(string id) => StoryClueWorldGenerator.CreateClue(root.transform,
            StoryClueDefinition.Production().Single(d => d.ClueId == id), Vector3.zero);
        private void RaftFailure()
        {
            story.Signal(StorySignalIds.CrashSurvived);
            story.Signal(StorySignalIds.SurvivalEstablished);
            story.Signal(StorySignalIds.RaftBuilt);
            story.Signal(StorySignalIds.RaftEscapeFailed);
        }

        [Test]
        public void ProductionDefinitionsAreUniqueAndInvestigationRequiresInspectionEvidence()
        {
            var definitions = StoryClueDefinition.Production();
            CollectionAssert.AreEquivalent(new[] { "smuggler_cache_manifest", "smuggler_camp_evidence", "smuggler_route_fragment" },
                definitions.Select(d => d.ClueId));
            Assert.AreEqual(definitions.Count, definitions.Select(d => d.ClueId).Distinct().Count());
            foreach (var definition in definitions)
            {
                Assert.IsNotEmpty(definition.DisplayName);
                Assert.IsNotEmpty(definition.InspectionText);
                CollectionAssert.Contains(new[] { "smuggler_cache", "smuggler_camp" }, definition.AnchorLandmarkId);
                Assert.Greater(definition.VegetationClearanceRadius, 0f);
            }
            var rules = StoryProgressionDefinition.Production.Transitions
                .Where(t => t.FromStageId == StoryStageIds.InvestigateHumanTraces).ToArray();
            Assert.AreEqual(1, rules.Length);
            Assert.AreEqual(StoryClueMilestoneIds.HumanTracesFound, rules[0].RequiredMilestoneId);
            Assert.AreEqual(StoryStageIds.LocateSmugglerBase, rules[0].ToStageId);
            Assert.AreEqual("clue_discovered:smuggler_cache_manifest", StoryMilestoneIds.ClueDiscovered(" SMUGGLER_CACHE_MANIFEST "));
            Assert.IsEmpty(StoryMilestoneIds.ClueDiscovered(null));
            Assert.AreEqual(22, (int)GameplayEventKind.StorySignal);
            Assert.AreEqual(23, (int)GameplayEventKind.StoryClueInspected);
        }

        [TestCase("smuggler_cache_manifest", "smuggler_cache", LandmarkType.SmugglerCache)]
        [TestCase("smuggler_camp_evidence", "smuggler_camp", LandmarkType.SmugglerCamp)]
        public void EitherEvidenceAdvancesButProximityAloneDoesNot(string id, string anchorId, LandmarkType type)
        {
            RaftFailure();
            var anchorObject = new GameObject("Anchor");
            anchorObject.transform.SetParent(root.transform);
            var anchor = anchorObject.AddComponent<LandmarkRuntime>();
            anchor.Configure(anchorId, type, "Anchor");
            Assert.IsTrue(anchor.Discover());
            story.Signal(StoryMilestoneIds.LandmarkDiscovered(anchorId));
            Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
            var clue = Clue(id);
            int transitions = 0;
            story.StageChanged += _ => transitions++;
            Assert.IsTrue(clue.Discover());
            // EditMode validates the definition/engine API; actual OnEnable bus integration
            // is covered by StoryCluePlayModeTests and the production interaction test.
            story.Signal(StoryMilestoneIds.ClueDiscovered(id));
            story.Signal(clue.AdditionalMilestoneId);
            Assert.IsTrue(story.HasMilestone(StoryMilestoneIds.ClueDiscovered(id)));
            Assert.IsTrue(story.HasMilestone(StoryClueMilestoneIds.HumanTracesFound));
            Assert.AreEqual(StoryStageIds.LocateSmugglerBase, story.CurrentStageId);
            int signals = GameEventBus.RecentEvents.Count(e => e.kind == GameplayEventKind.StorySignal);
            Assert.IsFalse(clue.Discover());
            Assert.AreEqual(signals, GameEventBus.RecentEvents.Count(e => e.kind == GameplayEventKind.StorySignal));
            Assert.AreEqual(1, transitions);
        }

        [Test]
        public void EarlierEvidenceCascadesAfterRaftFailureWithoutRediscovery()
        {
            story.Signal(StorySignalIds.CrashSurvived);
            story.Signal(StorySignalIds.SurvivalEstablished);
            story.Signal(StoryMilestoneIds.ClueDiscovered("smuggler_cache_manifest"));
            story.Signal(StoryClueMilestoneIds.HumanTracesFound);
            Assert.AreEqual(StoryStageIds.BuildRaft, story.CurrentStageId);
            story.Signal(StorySignalIds.RaftBuilt);
            story.Signal(StorySignalIds.RaftEscapeFailed);
            Assert.AreEqual(StoryStageIds.LocateSmugglerBase, story.CurrentStageId);
        }

        [Test]
        public void SharedEvidenceMilestoneIsPublishedOnceAndRouteDoesNotRevealEntrance()
        {
            var baseObject = new GameObject("Base");
            baseObject.transform.SetParent(root.transform);
            var entrance = baseObject.AddComponent<LandmarkRuntime>();
            entrance.Configure("base_entrance", LandmarkType.BaseEntrance, null);
            RaftFailure();
            foreach (var definition in StoryClueDefinition.Production()) Assert.IsTrue(Clue(definition.ClueId).Discover());
            Assert.AreEqual(1, GameEventBus.RecentEvents.Count(e => e.signalId == StoryClueMilestoneIds.HumanTracesFound));
            Assert.IsFalse(entrance.IsDiscovered);
            Assert.IsFalse(story.HasMilestone(StoryMilestoneIds.LandmarkDiscovered("base_entrance")));
            Assert.IsTrue(entrance.Discover());
            Assert.IsTrue(entrance.IsDiscovered);
        }

        [Test]
        public void SaveDtoRoundTripOldSaveAndRegistryRestoreNeverReplayDiscovery()
        {
            var clue = Clue("smuggler_cache_manifest");
            clue.transform.position = new Vector3(11f, 2f, 31f);
            clue.Discover();
            var serializer = new UnityJsonGameSaveSerializer();
            var data = new GameSaveData();
            data.World.clueStates = StoryClueRegistry.CaptureSaveData();
            var restored = serializer.Deserialize(serializer.Serialize(data));
            var state = restored.World.ClueStates.Single();
            Assert.AreEqual(clue.ClueId, state.ClueId);
            Assert.IsTrue(state.Discovered);
            Assert.AreEqual(11f, state.X); Assert.AreEqual(2f, state.Y); Assert.AreEqual(31f, state.Z);
            clue.transform.position = Vector3.zero;
            int events = GameEventBus.RecentEventCount;
            StoryClueRegistry.RestoreFromSaveData(restored.World.ClueStates);
            Assert.AreEqual(new Vector3(11f, 2f, 31f), clue.transform.position);
            Assert.AreEqual(events, GameEventBus.RecentEventCount);
            Assert.AreEqual(1, StoryClueRegistry.Clues.Count);
            Assert.IsTrue(clue.IsDiscovered);
            Assert.IsEmpty(serializer.Deserialize("{\"world\":{\"seed\":12345}}").World.ClueStates);
            data.World.clueStates = null;
            Assert.IsEmpty(data.World.ClueStates);
            StoryClueRegistry.RestoreFromSaveData(data.World.ClueStates);
            Assert.IsFalse(clue.IsDiscovered);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Saved clue is no longer available"));
            StoryClueRegistry.RestoreFromSaveData(new[] { new StoryClueSaveData("unknown", true, 0, 0, 0) });
            Assert.AreEqual(1, StoryClueRegistry.Clues.Count);
        }

        [Test]
        public void PlannerPreservesRandomStateAndMeetsAnchorRouteAndSafetyConstraints()
        {
            var topo = root.AddComponent<IslandTopographyRuntime>();
            topo.Build(64, 4f, (x, z) => x < 0, p => 1f, p => "westwood", biomeResolutionPerTile: 2);
            var planner = new StoryCluePlacementPlanner();
            Vector3 crash = new Vector3(-96f, 0f, -96f), camp = new Vector3(-20f, 0f, 20f), target = new Vector3(-10f, 0f, 70f);
            var random = Random.state;
            foreach (var definition in StoryClueDefinition.Production())
            {
                Vector3 anchor = definition.AnchorLandmarkId == "smuggler_cache" ? new Vector3(-48f, 0f, 0f) : camp;
                Assert.IsTrue(planner.TryPlan(12345, definition, topo, anchor, crash, target, out var first));
                Assert.IsTrue(planner.TryPlan(12345, definition, topo, anchor, crash, target, out var second));
                Assert.AreEqual(first, second);
                Assert.IsTrue(topo.TryGetEnvironmentAt(first, out ApexShift.Runtime.World.Environment.EnvironmentSample sample));
                Assert.IsTrue(sample.IsLand); Assert.IsFalse(sample.IsWater);
                Assert.GreaterOrEqual(StoryCluePlacementPlanner.Distance(first, crash), definition.MinCrashDistance);
                if (definition.IsRoute)
                {
                    Assert.IsFalse(sample.IsShoreline);
                    Assert.Less(StoryCluePlacementPlanner.Distance(first, target), StoryCluePlacementPlanner.Distance(camp, target));
                    Assert.IsFalse(planner.TryPlan(12345, definition, topo, anchor, crash, null, out _));
                }
                else Assert.That(StoryCluePlacementPlanner.Distance(first, anchor),
                    Is.InRange(definition.MinAnchorDistance - 0.001f, definition.MaxAnchorDistance + 0.001f));
            }
            Assert.AreEqual(random, Random.state);
        }
    }
}
