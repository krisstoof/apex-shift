using System;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceRegrowthSystem
    {
        private readonly ResourceRegrowthRules rules;

        public ResourceRegrowthSystem(ResourceRegrowthRules rules = null)
        {
            this.rules = rules ?? new ResourceRegrowthRules();
        }

        public void MarkHarvested(ResourceState resource, ResourceGrowthState growth)
        {
            if (resource == null || growth == null) return;
            
            growth.MarkHarvested();
            growth.UpdateDaysToNextStage(rules.GetDaysPerStage(growth.ResourceKind, growth.MaxGrowthStage));
            resource.MarkDepleted();
        }

        public ResourceRegrowthResult AdvanceDays(ResourceState resource, ResourceGrowthState growth, float days)
        {
            if (resource == null || growth == null || days <= 0) 
                return ResourceRegrowthResult.NoChange(growth?.GrowthStage ?? ResourceGrowthStage.Mature);

            if (!growth.IsHarvested && growth.GrowthStage == ResourceGrowthStage.Mature)
                return ResourceRegrowthResult.NoChange(growth.GrowthStage);

            if (!rules.CanRegrow(growth.ResourceKind))
                return ResourceRegrowthResult.NoChange(growth.GrowthStage);

            ResourceGrowthStage oldStage = growth.GrowthStage;
            float daysPerStage = rules.GetDaysPerStage(growth.ResourceKind, growth.MaxGrowthStage);
            if (daysPerStage <= 0f)
                return ResourceRegrowthResult.NoChange(growth.GrowthStage);

            growth.AdvanceProgress(days, daysPerStage);

            bool stageChanged = false;
            while (growth.GrowthProgressDays >= daysPerStage && (int)growth.GrowthStage < (int)ResourceGrowthStage.Mature)
            {
                ResourceGrowthStage nextStage = (ResourceGrowthStage)Math.Min(
                    (int)ResourceGrowthStage.Mature,
                    (int)growth.GrowthStage + 1);

                growth.SetGrowthStage(nextStage);
                growth.ConsumeProgress(daysPerStage);
                growth.UpdateDaysToNextStage(daysPerStage);
                stageChanged = true;
            }

            bool becameMature = stageChanged && growth.GrowthStage == ResourceGrowthStage.Mature;
            if (becameMature)
            {
                resource.RestoreToFull();
            }

            return new ResourceRegrowthResult(stageChanged, oldStage, growth.GrowthStage, becameMature);
        }

        public ResourceRegrowthResult AdvanceTime(ResourceState resource, ResourceGrowthState growth, float deltaSeconds, float secondsPerDay)
        {
            if (secondsPerDay <= 0) return ResourceRegrowthResult.NoChange(growth?.GrowthStage ?? ResourceGrowthStage.Mature);
            float days = deltaSeconds / secondsPerDay;
            return AdvanceDays(resource, growth, days);
        }

        public void ForceFullRegrowth(ResourceState resource, ResourceGrowthState growth)
        {
            if (resource == null || growth == null) return;
            growth.SetGrowthStage(ResourceGrowthStage.Mature);
            resource.RestoreToFull();
        }

        public string GetGrowthDebugText(ResourceGrowthState growth)
        {
            if (growth == null) return "No Growth Data";
            if (!growth.IsHarvested && growth.GrowthStage == ResourceGrowthStage.Mature) return "Mature";
            
            if (!rules.CanRegrow(growth.ResourceKind)) return "No Regrowth";

            return $"{growth.GrowthStage} ({growth.GrowthProgressDays:F1}/{rules.GetDaysPerStage(growth.ResourceKind, growth.MaxGrowthStage):F1} days)";
        }
    }
}
