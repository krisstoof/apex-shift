using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using UnityEngine;

namespace ApexShift.Runtime.Story
{
    /// <summary>Generation-owned story state. Milestones survive transitions; restore never replays completion.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryProgressionRuntime : MonoBehaviour
    {
        private StoryProgressionDefinition definition = StoryProgressionDefinition.Production;
        private string currentStageId = StoryStageIds.SurviveCrash;
        private readonly HashSet<string> milestones = new HashSet<string>(StringComparer.Ordinal);
        private IDisposable subscription;

        public string CurrentStageId => currentStageId;
        public StoryStageDefinition CurrentStage => definition.GetStage(currentStageId);
        public string CurrentObjectiveText => CurrentStage.ObjectiveText;
        public IReadOnlyCollection<string> CompletedMilestones => Array.AsReadOnly(milestones.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        public event Action<StoryStageDefinition> StageChanged;
        public event Action<string> MilestoneCompleted;
        public event Action StateChanged;

        private void OnEnable() => subscription = GameEventBus.Subscribe(HandleGameplayEvent);
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Unsubscribe() { subscription?.Dispose(); subscription = null; }

        private void HandleGameplayEvent(GameplayEvent value)
        {
            if (value.kind == GameplayEventKind.LandmarkDiscovered)
                Signal(StoryMilestoneIds.LandmarkDiscovered(value.landmarkId));
            else if (value.kind == GameplayEventKind.StorySignal)
                Signal(value.signalId);
        }

        public bool HasMilestone(string milestoneId) => milestones.Contains(StoryMilestoneIds.Normalize(milestoneId));

        /// <summary>True only when a new nonempty milestone is recorded, even without a stage change.
        /// Duplicate/empty signals return false and emit no events.</summary>
        public bool Signal(string signalId)
        {
            string id = StoryMilestoneIds.Normalize(signalId);
            if (id.Length == 0 || !milestones.Add(id)) return false;
            MilestoneCompleted?.Invoke(id);
            ReevaluateTransitions();
            StateChanged?.Invoke();
            return true;
        }

        private void ReevaluateTransitions()
        {
            int limit = definition.Stages.Count + 2;
            for (int count = 0; count < limit; count++)
            {
                StoryTransitionDefinition next = definition.Transitions.FirstOrDefault(rule =>
                    rule.FromStageId == currentStageId && milestones.Contains(rule.RequiredMilestoneId));
                if (next == null) return;
                currentStageId = next.ToStageId;
                StageChanged?.Invoke(CurrentStage);
            }
            Debug.LogWarning("[Story] Transition limit reached; check story definition for a cycle.", this);
        }

        public StorySaveData CaptureSaveData() => new StorySaveData
        {
            currentStageId = currentStageId,
            completedMilestoneIds = milestones.OrderBy(id => id, StringComparer.Ordinal).ToList()
        };

        /// <summary>Restore state without reevaluating transitions or replaying gameplay completion.
        /// A single presentation-only notification updates bound views.</summary>
        public void RestoreSaveData(StorySaveData data)
        {
            data = data ?? StorySaveData.Default;
            string stageId = StoryMilestoneIds.Normalize(data.CurrentStageId);
            if (definition.GetStage(stageId) == null)
            {
                Debug.LogWarning("[Story] Unknown saved stage '" + stageId + "'; using initial stage.", this);
                stageId = definition.InitialStageId;
            }
            currentStageId = stageId;
            milestones.Clear();
            foreach (string value in data.CompletedMilestoneIds)
            {
                string id = StoryMilestoneIds.Normalize(value);
                if (id.Length > 0) milestones.Add(id);
            }
            StateChanged?.Invoke();
        }

        public void Configure(StoryProgressionDefinition storyDefinition)
        {
            definition = storyDefinition ?? throw new ArgumentNullException(nameof(storyDefinition));
            RestoreSaveData(new StorySaveData { currentStageId = definition.InitialStageId });
        }
    }
}
