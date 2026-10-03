namespace ApexShift.Runtime.Story
{
    public sealed class StoryTransitionDefinition
    {
        public string FromStageId { get; }
        public string RequiredMilestoneId { get; }
        public string ToStageId { get; }

        public StoryTransitionDefinition(string fromStageId, string requiredMilestoneId, string toStageId)
        {
            FromStageId = StoryMilestoneIds.Normalize(fromStageId);
            RequiredMilestoneId = StoryMilestoneIds.Normalize(requiredMilestoneId);
            ToStageId = StoryMilestoneIds.Normalize(toStageId);
        }
    }
}
