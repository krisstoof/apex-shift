using System;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceState
    {
        public ResourceState(ResourceDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ResourceId = definition.Id.ToString();
            DisplayName = definition.DisplayName;
            ItemId = definition.ItemId;
            Amount = definition.HarvestAmount;
            MaxAmount = definition.HarvestAmount;
            PlayerHarvestable = definition.PlayerHarvestable;
            CanBeHarvested = definition.PlayerHarvestable;
            RemoveWhenHarvested = definition.RemoveWhenHarvested;
            EdibleByHerbivores = definition.EdibleByHerbivores;
            FoodValue = definition.FoodValue;
            RenderOnly = definition.RenderOnly;
            PondVegetation = definition.PondVegetation;
            PickupPriority = definition.PickupPriority;
            RegrowthDays = definition.RegrowthDays;
            GrowthProgress = 1f;
        }

        public ResourceDefinition Definition { get; }
        public string ResourceId { get; }
        public string DisplayName { get; }
        public string ItemId { get; }
        public int Amount { get; private set; }
        public int MaxAmount { get; }
        public bool PlayerHarvestable { get; private set; }
        public bool CanBeHarvested { get; private set; }
        public bool RemoveWhenHarvested { get; }
        public bool EdibleByHerbivores { get; }
        public float FoodValue { get; }
        public bool RenderOnly { get; }
        public bool PondVegetation { get; }
        public int PickupPriority { get; }
        public int RegrowthDays { get; }
        public float GrowthProgress { get; private set; }
        public bool IsDepleted { get; private set; }

        public bool IsDrop => ResourceId == "meat_drop" || ResourceId == "bone_drop" || ResourceId == "item_drop";
        public bool IsFoodAvailableForHerbivores => EdibleByHerbivores && FoodValue > 0f && !IsDepleted;
        public bool IsInteractable => !RenderOnly && PlayerHarvestable && CanBeHarvested && Amount > 0 && !IsDepleted;
        public void SetAmount(int amount)
        {
            Amount = Math.Max(0, Math.Min(amount, MaxAmount));
            if (Amount <= 0) MarkDepleted();
            else IsDepleted = false;
        }
        public void SetHarvestable(bool canBeHarvested) => CanBeHarvested = canBeHarvested && PlayerHarvestable && !IsDepleted;
        public void MarkDepleted() { Amount = 0; IsDepleted = true; CanBeHarvested = false; GrowthProgress = 0f; }
        public void RestoreToFull() { Amount = MaxAmount; IsDepleted = false; CanBeHarvested = PlayerHarvestable; GrowthProgress = 1f; }

        public void SetGrowthProgress(float growthProgress)
        {
            GrowthProgress = Clamp01(growthProgress);
            if (GrowthProgress >= 1f && IsDepleted)
            {
                RestoreToFull();
            }
        }

        public bool AdvanceGrowthDays(int days)
        {
            if (RegrowthDays <= 0 || days <= 0 || !IsDepleted)
            {
                return false;
            }

            GrowthProgress = Math.Min(1f, GrowthProgress + days / (float)RegrowthDays);
            if (GrowthProgress >= 1f)
            {
                RestoreToFull();
            }

            return true;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
