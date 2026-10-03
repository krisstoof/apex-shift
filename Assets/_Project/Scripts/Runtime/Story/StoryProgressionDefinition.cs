using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Runtime.Story
{
    /// <summary>Central immutable story data. Separate transitions express OR requirements.</summary>
    public sealed class StoryProgressionDefinition
    {
        public string InitialStageId { get; }
        public IReadOnlyList<StoryStageDefinition> Stages { get; }
        public IReadOnlyList<StoryTransitionDefinition> Transitions { get; }
        private readonly Dictionary<string, StoryStageDefinition> stagesById;

        public StoryProgressionDefinition(string initialStageId, IEnumerable<StoryStageDefinition> stages,
            IEnumerable<StoryTransitionDefinition> transitions)
        {
            InitialStageId = StoryMilestoneIds.Normalize(initialStageId);
            Stages = Array.AsReadOnly(stages.ToArray());
            Transitions = Array.AsReadOnly(transitions.ToArray());
            stagesById = Stages.ToDictionary(stage => stage.Id, StringComparer.Ordinal);
            if (!stagesById.ContainsKey(InitialStageId))
                throw new ArgumentException("Story definition must contain its initial stage.");
            foreach (StoryTransitionDefinition transition in Transitions)
                if (!stagesById.ContainsKey(transition.FromStageId) || !stagesById.ContainsKey(transition.ToStageId)
                    || string.IsNullOrEmpty(transition.RequiredMilestoneId))
                    throw new ArgumentException("Story transition contains an invalid stage or milestone.");
        }

        public StoryStageDefinition GetStage(string id)
            => stagesById.TryGetValue(StoryMilestoneIds.Normalize(id), out StoryStageDefinition stage) ? stage : null;

        public static StoryProgressionDefinition Production { get; } = new StoryProgressionDefinition(
            StoryStageIds.SurviveCrash,
            new[]
            {
                new StoryStageDefinition(StoryStageIds.SurviveCrash, "Assess the crash site and get your bearings."),
                new StoryStageDefinition(StoryStageIds.EstablishSurvival, "Secure the essentials needed to survive."),
                new StoryStageDefinition(StoryStageIds.BuildRaft, "Build a raft to leave the island."),
                new StoryStageDefinition(StoryStageIds.AttemptRaftEscape, "Take the raft beyond the island."),
                new StoryStageDefinition(StoryStageIds.InvestigateHumanTraces, "Find another way off the island. Look for signs of people."),
                new StoryStageDefinition(StoryStageIds.LocateSmugglerBase, "Follow the smugglers' trail."),
                new StoryStageDefinition(StoryStageIds.GainBaseAccess, "Find a way into the hidden facility."),
                new StoryStageDefinition(StoryStageIds.PrepareBoat, "Prepare the boat for escape."),
                new StoryStageDefinition(StoryStageIds.EscapeIsland, "Use the boat to leave the island."),
                new StoryStageDefinition(StoryStageIds.Completed, "You escaped the island.")
            },
            new[]
            {
                new StoryTransitionDefinition(StoryStageIds.SurviveCrash, StorySignalIds.CrashSurvived, StoryStageIds.EstablishSurvival),
                new StoryTransitionDefinition(StoryStageIds.EstablishSurvival, StorySignalIds.SurvivalEstablished, StoryStageIds.BuildRaft),
                new StoryTransitionDefinition(StoryStageIds.BuildRaft, StorySignalIds.RaftBuilt, StoryStageIds.AttemptRaftEscape),
                new StoryTransitionDefinition(StoryStageIds.AttemptRaftEscape, StorySignalIds.RaftEscapeFailed, StoryStageIds.InvestigateHumanTraces),
                new StoryTransitionDefinition(StoryStageIds.InvestigateHumanTraces, Clues.StoryClueMilestoneIds.HumanTracesFound, StoryStageIds.LocateSmugglerBase),
                new StoryTransitionDefinition(StoryStageIds.LocateSmugglerBase, StoryMilestoneIds.LandmarkDiscovered("base_entrance"), StoryStageIds.GainBaseAccess),
                new StoryTransitionDefinition(StoryStageIds.GainBaseAccess, StorySignalIds.BaseAccessGained, StoryStageIds.PrepareBoat),
                new StoryTransitionDefinition(StoryStageIds.PrepareBoat, StorySignalIds.BoatPrepared, StoryStageIds.EscapeIsland),
                new StoryTransitionDefinition(StoryStageIds.EscapeIsland, StorySignalIds.IslandEscaped, StoryStageIds.Completed)
            });
    }
}
