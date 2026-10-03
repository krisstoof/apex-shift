namespace ApexShift.Runtime.Story
{
    public static class StoryMilestoneIds
    {
        public static string Normalize(string id)
            => string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToLowerInvariant();

        public static string LandmarkDiscovered(string landmarkId)
        {
            string id = Normalize(landmarkId);
            return id.Length == 0 ? string.Empty : "landmark_discovered:" + id;
        }

        public static string ClueDiscovered(string clueId)
        {
            string id = Normalize(clueId);
            return id.Length == 0 ? string.Empty : "clue_discovered:" + id;
        }
    }
}
