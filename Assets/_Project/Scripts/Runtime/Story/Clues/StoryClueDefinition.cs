using System.Collections.Generic;

namespace ApexShift.Runtime.Story.Clues
{
    public static class StoryClueMilestoneIds
    {
        public const string HumanTracesFound = "human_traces_found";
    }

    public sealed class StoryClueDefinition
    {
        public string ClueId { get; }
        public string DisplayName { get; }
        public string InspectionText { get; }
        public string AnchorLandmarkId { get; }
        public float MinAnchorDistance { get; }
        public float MaxAnchorDistance { get; }
        public float VegetationClearanceRadius { get; }
        public string AdditionalMilestoneId { get; }
        public float MinCrashDistance { get; }
        public float MaxSlopeDegrees => 22f;
        public bool IsRoute { get; }
        public string DirectionLandmarkId => IsRoute ? "base_entrance" : string.Empty;
        public float MinRouteFraction => 0.3f;
        public float MaxRouteFraction => 0.6f;

        public StoryClueDefinition(string id, string displayName, string inspectionText, string anchor,
            float minDistance, float maxDistance, float clearance, float crashDistance,
            string additionalMilestone = null, bool isRoute = false)
        {
            ClueId = StoryMilestoneIds.Normalize(id);
            DisplayName = displayName;
            InspectionText = inspectionText;
            AnchorLandmarkId = StoryMilestoneIds.Normalize(anchor);
            MinAnchorDistance = minDistance;
            MaxAnchorDistance = maxDistance;
            VegetationClearanceRadius = clearance;
            MinCrashDistance = crashDistance;
            AdditionalMilestoneId = StoryMilestoneIds.Normalize(additionalMilestone);
            IsRoute = isRoute;
        }

        private static readonly IReadOnlyList<StoryClueDefinition> production = System.Array.AsReadOnly(new[]
        {
            new StoryClueDefinition("smuggler_cache_manifest", "Shipping Markings",
                "These containers are too recent to have washed ashore years ago. The same shipping marks appear on several crates. Someone used this place regularly.",
                "smuggler_cache", 1f, 3.5f, 2.5f, 40f, StoryClueMilestoneIds.HumanTracesFound),
            new StoryClueDefinition("smuggler_camp_evidence", "Abandoned Equipment",
                "Fuel cans, radio parts and cut rope were left behind. This looks like a working camp, not the shelter of a stranded traveler.",
                "smuggler_camp", 1.5f, 4.5f, 3f, 55f, StoryClueMilestoneIds.HumanTracesFound),
            new StoryClueDefinition("smuggler_route_fragment", "Marked Route",
                "A marked route continues inland toward the rocky high ground. One annotation refers to a lower entrance.",
                "smuggler_camp", 4f, 12f, 2f, 55f, isRoute: true)
        });

        public static IReadOnlyList<StoryClueDefinition> Production() => production;
    }
}
