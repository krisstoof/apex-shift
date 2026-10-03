using System;
using System.Collections.Generic;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class StorySaveData
    {
        // Core DTO must not depend on Runtime.Story.
        private const string InitialStageId = "survive_crash";
        public string currentStageId = InitialStageId;
        public List<string> completedMilestoneIds = new List<string>();

        public string CurrentStageId => string.IsNullOrWhiteSpace(currentStageId) ? InitialStageId : currentStageId;
        public IReadOnlyList<string> CompletedMilestoneIds
            => completedMilestoneIds ?? (completedMilestoneIds = new List<string>());
        public static StorySaveData Default => new StorySaveData();
    }
}
