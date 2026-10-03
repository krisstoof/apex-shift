using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Landmarks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Editor
{
    public sealed class StoryProgressionTests
    {
        private GameObject root;
        private StoryProgressionRuntime story;

        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearForTests();
            root = new GameObject("StoryTest");
            story = root.AddComponent<StoryProgressionRuntime>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameEventBus.ClearForTests();
        }

        [Test]
        public void ProductionDefinitionHasUniqueValidStagesAndNoCycles()
        {
            StoryProgressionDefinition definition = StoryProgressionDefinition.Production;
            Assert.AreEqual(10, definition.Stages.Count);
            Assert.AreEqual(10, definition.Transitions.Count);
            Assert.AreEqual(definition.Stages.Count, definition.Stages.Select(stage => stage.Id).Distinct().Count());
            foreach (StoryStageDefinition stage in definition.Stages)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(stage.ObjectiveText));
                Assert.IsFalse(HasCycle(definition, stage.Id, new HashSet<string>()));
            }
            foreach (StoryTransitionDefinition rule in definition.Transitions)
            {
                Assert.NotNull(definition.GetStage(rule.FromStageId));
                Assert.NotNull(definition.GetStage(rule.ToStageId));
                Assert.AreNotEqual(StoryStageIds.Completed, rule.FromStageId);
            }
        }

        private static bool HasCycle(StoryProgressionDefinition definition, string id, HashSet<string> path)
        {
            if (!path.Add(id)) return true;
            foreach (StoryTransitionDefinition rule in definition.Transitions.Where(rule => rule.FromStageId == id))
                if (HasCycle(definition, rule.ToStageId, new HashSet<string>(path))) return true;
            return false;
        }

        [Test]
        public void NewGameStartsWithNoMilestonesAndCentralObjective()
        {
            Assert.AreEqual(StoryStageIds.SurviveCrash, story.CurrentStageId);
            Assert.IsEmpty(story.CompletedMilestones);
            Assert.AreEqual(StoryProgressionDefinition.Production.GetStage(StoryStageIds.SurviveCrash).ObjectiveText,
                story.CurrentObjectiveText);
            story.Signal(StoryMilestoneIds.LandmarkDiscovered("plane_crash"));
            Assert.AreEqual(StoryStageIds.SurviveCrash, story.CurrentStageId);
            Assert.IsFalse(story.HasMilestone(StorySignalIds.CrashSurvived));
        }

        [Test]
        public void NormalSignalAndDuplicatesEmitCompletionAndTransitionOnlyOnce()
        {
            int stages = 0, milestones = 0, states = 0;
            story.StageChanged += _ => stages++;
            story.MilestoneCompleted += _ => milestones++;
            story.StateChanged += () => states++;
            Assert.IsTrue(story.Signal(" CRASH_SURVIVED "));
            Assert.AreEqual(StoryStageIds.EstablishSurvival, story.CurrentStageId);
            Assert.IsFalse(story.Signal(StorySignalIds.CrashSurvived));
            Assert.IsFalse(story.Signal(" "));
            Assert.AreEqual(1, stages);
            Assert.AreEqual(1, milestones);
            Assert.AreEqual(1, states);
            Assert.AreEqual(1, story.CompletedMilestones.Count);
        }

        [TestCase("smuggler_cache")]
        [TestCase("smuggler_camp")]
        public void EitherHumanTraceUnlocksBaseTrailThenEntranceUnlocksAccessObjective(string clue)
        {
            EarlyFlow();
            Assert.AreEqual(StoryStageIds.InvestigateHumanTraces, story.CurrentStageId);
            story.Signal(StoryMilestoneIds.LandmarkDiscovered(clue));
            Assert.AreEqual(StoryStageIds.LocateSmugglerBase, story.CurrentStageId);
            story.Signal(StoryMilestoneIds.LandmarkDiscovered("base_entrance"));
            Assert.AreEqual(StoryStageIds.GainBaseAccess, story.CurrentStageId);
            Assert.IsFalse(story.HasMilestone(StorySignalIds.BaseAccessGained));
        }

        [Test]
        public void OutOfOrderDiscoveriesCascadeWithoutRediscoveryAndDuplicatesAreNoOps()
        {
            story.Signal(StoryMilestoneIds.LandmarkDiscovered(" SMUGGLER_CACHE "));
            story.Signal(StoryMilestoneIds.LandmarkDiscovered("base_entrance"));
            Assert.AreEqual(StoryStageIds.SurviveCrash, story.CurrentStageId);
            Assert.AreEqual(2, story.CompletedMilestones.Count);
            int changes = 0;
            story.StageChanged += _ => changes++;
            EarlyFlow();
            Assert.AreEqual(StoryStageIds.GainBaseAccess, story.CurrentStageId);
            Assert.AreEqual(6, changes);
            for (int i = 0; i < 3; i++) Assert.IsFalse(story.Signal(StorySignalIds.RaftEscapeFailed));
            Assert.AreEqual(6, changes);
            Assert.AreEqual(6, story.CompletedMilestones.Count);
        }

        [Test]
        public void CompleteFlowIsTerminalAndCaptureIsSortedAndIndependent()
        {
            EarlyFlow();
            story.Signal(StoryMilestoneIds.LandmarkDiscovered("smuggler_cache"));
            story.Signal(StoryMilestoneIds.LandmarkDiscovered("base_entrance"));
            story.Signal(StorySignalIds.BaseAccessGained);
            story.Signal(StorySignalIds.BoatPrepared);
            story.Signal(StorySignalIds.IslandEscaped);
            Assert.AreEqual(StoryStageIds.Completed, story.CurrentStageId);
            Assert.AreEqual(9, story.CompletedMilestones.Count);
            Assert.IsFalse(story.Signal(StorySignalIds.IslandEscaped));
            StorySaveData data = story.CaptureSaveData();
            CollectionAssert.AreEqual(data.completedMilestoneIds.OrderBy(id => id, System.StringComparer.Ordinal),
                data.completedMilestoneIds);
            data.completedMilestoneIds.Clear();
            Assert.AreEqual(9, story.CompletedMilestones.Count);
        }

        [Test]
        public void RestoreNormalizesRetainsUnknownMilestonesAndDoesNotReplayOrReevaluate()
        {
            int stages = 0, milestones = 0, states = 0;
            story.StageChanged += _ => stages++;
            story.MilestoneCompleted += _ => milestones++;
            story.StateChanged += () => states++;
            LogAssert.Expect(LogType.Warning, new Regex("Unknown saved stage"));
            story.RestoreSaveData(new StorySaveData
            {
                currentStageId = " future_stage ",
                completedMilestoneIds = new List<string> { " CRASH_SURVIVED ", "crash_survived", "", null, " FUTURE_MILESTONE " }
            });
            Assert.AreEqual(StoryStageIds.SurviveCrash, story.CurrentStageId);
            CollectionAssert.AreEquivalent(new[] { "crash_survived", "future_milestone" }, story.CompletedMilestones);
            Assert.AreEqual(0, stages);
            Assert.AreEqual(0, milestones);
            Assert.AreEqual(1, states);
            Assert.IsEmpty(GameEventBus.RecentEvents);
        }

        [Test]
        public void CyclicDefinitionStopsAtTransitionGuard()
        {
            story.Configure(new StoryProgressionDefinition("a",
                new[] { new StoryStageDefinition("a", "A"), new StoryStageDefinition("b", "B") },
                new[] { new StoryTransitionDefinition("a", "go", "b"), new StoryTransitionDefinition("b", "go", "a") }));
            int changes = 0;
            story.StageChanged += _ => changes++;
            LogAssert.Expect(LogType.Warning, new Regex("Transition limit reached"));
            Assert.IsTrue(story.Signal("go"));
            Assert.AreEqual(4, changes);
        }

        [Test]
        public void EventHelpersPreserveLegacyEnumValuesAndExplicitPayload()
        {
            Assert.AreEqual(20, (int)GameplayEventKind.FireSourceExpired);
            GameEventBus.PublishLandmarkDiscovered(new Vector3(1, 2, 3), " SMUGGLER_CACHE ");
            GameEventBus.PublishStorySignal(" RAFT_BUILT ", "raft-instance");
            Assert.AreEqual(2, GameEventBus.RecentEvents.Count);
            GameplayEvent landmark = GameEventBus.RecentEvents[0];
            Assert.AreEqual(GameplayEventKind.LandmarkDiscovered, landmark.kind);
            Assert.AreEqual("smuggler_cache", landmark.landmarkId);
            Assert.AreEqual(new Vector3(1, 2, 3), landmark.position);
            GameplayEvent signal = GameEventBus.RecentEvents[1];
            Assert.AreEqual(GameplayEventKind.StorySignal, signal.kind);
            Assert.AreEqual(StorySignalIds.RaftBuilt, signal.signalId);
            Assert.AreEqual("raft-instance", signal.subjectId);
        }

        [Test]
        public void LandmarkDiscoveryPublishesOnceAndSaveRestoreDoesNotPublish()
        {
            LandmarkRuntime landmark = root.AddComponent<LandmarkRuntime>();
            landmark.Configure("smuggler_cache", LandmarkType.SmugglerCache, "Cache");
            Assert.IsTrue(landmark.Discover());
            Assert.IsFalse(landmark.Discover());
            Assert.AreEqual(1, GameEventBus.RecentEvents.Count);
            landmark.ApplySaveData(landmark.ToSaveData());
            Assert.AreEqual(1, GameEventBus.RecentEvents.Count);
        }

        private void EarlyFlow()
        {
            story.Signal(StorySignalIds.CrashSurvived);
            story.Signal(StorySignalIds.SurvivalEstablished);
            story.Signal(StorySignalIds.RaftBuilt);
            Assert.AreEqual(StoryStageIds.AttemptRaftEscape, story.CurrentStageId);
            story.Signal(StorySignalIds.RaftEscapeFailed);
        }
    }
}
