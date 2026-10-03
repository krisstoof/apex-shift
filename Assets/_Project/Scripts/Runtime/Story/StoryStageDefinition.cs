namespace ApexShift.Runtime.Story
{
    public sealed class StoryStageDefinition
    {
        public string Id { get; }
        public string ObjectiveText { get; }

        public StoryStageDefinition(string id, string objectiveText)
        {
            Id = StoryMilestoneIds.Normalize(id);
            ObjectiveText = objectiveText ?? string.Empty;
        }
    }
}
