using System;

namespace ApexShift.Core.Resources
{
    [Serializable]
    public sealed class ResourceGrowthSaveData
    {
        public string ResourceId;
        public string ResourceKind;
        public int MatureAmount;
        public int GrowthStage;
        public int MaxGrowthStage;
        public float GrowthProgressDays;
        public float DaysSinceHarvested;
        public bool IsHarvested;
    }
}
