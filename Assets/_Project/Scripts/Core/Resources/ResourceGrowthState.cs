using System;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceGrowthState
    {
        public ResourceGrowthState(string resourceId, string resourceKind, int matureAmount, int maxGrowthStage = ResourceRegrowthRules.DefaultMaxStage)
        {
            ResourceId = resourceId;
            ResourceKind = resourceKind;
            MatureAmount = matureAmount;
            MaxGrowthStage = Math.Max(1, maxGrowthStage);
            GrowthStage = ResourceGrowthStage.Mature;
            IsHarvested = false;
        }

        public string ResourceId { get; private set; }
        public string ResourceKind { get; private set; }
        public int MatureAmount { get; private set; }
        public ResourceGrowthStage GrowthStage { get; private set; }
        public int MaxGrowthStage { get; private set; }
        public float GrowthProgressDays { get; private set; }
        public float DaysSinceHarvested { get; private set; }
        public bool IsHarvested { get; private set; }

        public float DaysToNextStage { get; private set; }
        public bool CanBeHarvested => GrowthStage == ResourceGrowthStage.Mature && !IsHarvested;

        public void MarkHarvested()
        {
            IsHarvested = true;
            GrowthStage = ResourceGrowthStage.Harvested;
            GrowthProgressDays = 0f;
            DaysSinceHarvested = 0f;
            DaysToNextStage = 0f;
        }

        public void SetGrowthStage(ResourceGrowthStage stage)
        {
            GrowthStage = stage;
            if (stage == ResourceGrowthStage.Mature)
            {
                IsHarvested = false;
                GrowthProgressDays = 0f;
                DaysToNextStage = 0f;
            }
        }

        public void AdvanceProgress(float days, float daysPerStage)
        {
            if (!IsHarvested && GrowthStage == ResourceGrowthStage.Mature) return;

            DaysSinceHarvested += days;
            
            if (daysPerStage <= 0) return;

            GrowthProgressDays += days;
            UpdateDaysToNextStage(daysPerStage);
        }

        public void UpdateDaysToNextStage(float daysPerStage)
        {
            if (GrowthStage == ResourceGrowthStage.Mature)
            {
                DaysToNextStage = 0f;
            }
            else
            {
                DaysToNextStage = Math.Max(0, daysPerStage - GrowthProgressDays);
            }
        }

        public void ConsumeProgress(float days)
        {
            GrowthProgressDays = Math.Max(0, GrowthProgressDays - days);
        }

        public void ResetProgress()
        {
            GrowthProgressDays = 0f;
        }

        public ResourceGrowthSaveData ToSaveData()
        {
            return new ResourceGrowthSaveData
            {
                ResourceId = ResourceId,
                ResourceKind = ResourceKind,
                MatureAmount = MatureAmount,
                GrowthStage = (int)GrowthStage,
                MaxGrowthStage = MaxGrowthStage,
                GrowthProgressDays = GrowthProgressDays,
                DaysSinceHarvested = DaysSinceHarvested,
                IsHarvested = IsHarvested
            };
        }

        public void FromSaveData(ResourceGrowthSaveData data)
        {
            if (data == null) return;
            ResourceId = data.ResourceId;
            ResourceKind = data.ResourceKind;
            MatureAmount = data.MatureAmount;
            GrowthStage = (ResourceGrowthStage)data.GrowthStage;
            MaxGrowthStage = data.MaxGrowthStage;
            GrowthProgressDays = data.GrowthProgressDays;
            DaysSinceHarvested = data.DaysSinceHarvested;
            IsHarvested = data.IsHarvested;
        }
    }
}
