using System.Collections;
using System.Linq;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.Story.Clues;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ApexShift.Tests.Regression
{
    public sealed class StoryCluePlayModeTests
    {
        [TestCase("smuggler_cache_manifest")]
        [TestCase("smuggler_camp_evidence")]
        public void RepeatInspectionRejectsInvalidActorsAndNeverReplaysMilestones(string clueId)
        {
            var root = new GameObject("ClueInteraction");
            var player = new GameObject("Player");
            var other = new GameObject("NonPlayer");
            try
            {
                var story = root.AddComponent<StoryProgressionRuntime>();
                story.RestoreSaveData(new ApexShift.Core.Save.StorySaveData { currentStageId = StoryStageIds.InvestigateHumanTraces });
                player.AddComponent<IsometricPlayerController>();
                var clue = StoryClueWorldGenerator.CreateClue(root.transform,
                    StoryClueDefinition.Production().Single(d => d.ClueId == clueId), Vector3.zero);
                int inspected = 0, signals = 0, transitions = 0;
                story.StageChanged += _ => transitions++;
                using (GameEventBus.Subscribe(e =>
                {
                    if (e.kind == GameplayEventKind.StoryClueInspected) inspected++;
                    if (e.kind == GameplayEventKind.StorySignal) signals++;
                }))
                {
                    Assert.IsFalse(clue.CanInteract(null)); Assert.IsFalse(clue.Interact(null));
                    Assert.IsFalse(clue.CanInteract(other)); Assert.IsFalse(clue.Interact(other));
                    Assert.IsTrue(clue.Interact(player));
                    Assert.AreEqual(2, signals); Assert.AreEqual(1, inspected); Assert.AreEqual(1, transitions);
                    Assert.IsTrue(clue.Interact(player));
                    Assert.AreEqual(2, signals); Assert.AreEqual(2, inspected); Assert.AreEqual(1, transitions);
                    Assert.IsFalse(clue.Discover());
                    Assert.AreEqual(60, clue.Priority); Assert.AreEqual(0.3f, clue.InteractionDuration);
                    Assert.AreEqual("Inspect " + clue.DisplayName, clue.Prompt);
                    Assert.NotNull(clue.GetComponent<Collider>());
                    clue.gameObject.SetActive(false);
                    Assert.IsNull(StoryClueRegistry.FindById(clue.ClueId));
                    Assert.IsFalse(clue.CanInteract(player));
                    clue.gameObject.SetActive(true);
                    Assert.AreSame(clue, StoryClueRegistry.FindById(clue.ClueId));
                    Object.DestroyImmediate(clue.gameObject);
                    Assert.IsNull(StoryClueRegistry.FindById(clueId));
                }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(player); Object.DestroyImmediate(other); }
        }

        [TestCase("smuggler_cache_manifest")]
        [TestCase("smuggler_camp_evidence")]
        public void EarlyEvidenceUsesActualBusAndCascadesOnlyAfterRaftFailure(string clueId)
        {
            var root = new GameObject("EarlyClue");
            try
            {
                var story = root.AddComponent<StoryProgressionRuntime>();
                GameEventBus.PublishStorySignal(StorySignalIds.CrashSurvived);
                GameEventBus.PublishStorySignal(StorySignalIds.SurvivalEstablished);
                var clue = StoryClueWorldGenerator.CreateClue(root.transform,
                    StoryClueDefinition.Production().Single(d => d.ClueId == clueId), Vector3.zero);
                Assert.IsTrue(clue.Discover());
                Assert.IsTrue(story.HasMilestone(StoryMilestoneIds.ClueDiscovered(clueId)));
                Assert.IsTrue(story.HasMilestone(StoryClueMilestoneIds.HumanTracesFound));
                Assert.AreEqual(StoryStageIds.BuildRaft, story.CurrentStageId);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftBuilt);
                GameEventBus.PublishStorySignal(StorySignalIds.RaftEscapeFailed);
                Assert.AreEqual(StoryStageIds.LocateSmugglerBase, story.CurrentStageId);
                Assert.IsFalse(clue.Discover());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator InspectionHudUpdatesOnEventsHidesInMenuAndUnsubscribes()
        {
            var root = new GameObject("ClueHudTest");
            var viewOwner = new GameObject("HudOwner");
            var panel = new GameObject("InspectionPanel", typeof(RectTransform));
            panel.transform.SetParent(viewOwner.transform);
            var titleObject = new GameObject("Title", typeof(RectTransform)); titleObject.transform.SetParent(panel.transform);
            var bodyObject = new GameObject("Body", typeof(RectTransform)); bodyObject.transform.SetParent(panel.transform);
            try
            {
                var title = titleObject.AddComponent<Text>(); var body = bodyObject.AddComponent<Text>();
                var view = viewOwner.AddComponent<StoryClueHUDView>();
                var first = StoryClueWorldGenerator.CreateClue(root.transform, StoryClueDefinition.Production()[0], Vector3.zero);
                var second = StoryClueWorldGenerator.CreateClue(root.transform, StoryClueDefinition.Production()[2], Vector3.one);
                view.Configure(panel, title, body, false, 0.1f);
                GameEventBus.PublishStoryClueInspected(first.transform.position, first.ClueId);
                Assert.IsFalse(view.IsVisible);
                view.Configure(panel, title, body, true, 0.1f);
                Assert.IsFalse(view.IsVisible);
                GameEventBus.PublishStoryClueInspected(first.transform.position, " SMUGGLER_CACHE_MANIFEST ");
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual(first.DisplayName, title.text); Assert.AreEqual(first.InspectionText, body.text);
                GameEventBus.PublishStoryClueInspected(second.transform.position, second.ClueId);
                Assert.AreEqual(second.DisplayName, title.text); Assert.AreEqual(second.InspectionText, body.text);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.IsFalse(view.IsVisible);
                GameEventBus.PublishStoryClueInspected(Vector3.zero, "unknown"); Assert.IsFalse(view.IsVisible);
                view.enabled = false;
                GameEventBus.PublishStoryClueInspected(first.transform.position, first.ClueId); Assert.IsFalse(view.IsVisible);
                view.enabled = true;
                GameEventBus.PublishStoryClueInspected(first.transform.position, first.ClueId); Assert.IsTrue(view.IsVisible);
                Object.DestroyImmediate(viewOwner); viewOwner = null;
                GameEventBus.PublishStoryClueInspected(second.transform.position, second.ClueId);
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(root); if (viewOwner != null) Object.DestroyImmediate(viewOwner); }
        }
    }
}
