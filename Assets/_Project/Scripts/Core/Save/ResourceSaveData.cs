using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class ResourceSaveData
    {
        public string resourceId;
        public string resourceType;
        public float x;
        public float y;
        public float z;
        public int amount;
        public int maxAmount;
        public bool depleted;
        public float growthProgress;
        public int regrowthDays;
        public bool edibleByHerbivores;
        public float foodValue;
        public bool renderOnly;
        public bool pondVegetation;
        public bool isDrop;
        public int pickupPriority;

        public string ResourceId => resourceId;
        public string ResourceType => resourceType;
        public int Amount => amount;
        public int MaxAmount => maxAmount;
        public bool Depleted => depleted;
        public float GrowthProgress => growthProgress;
        public int RegrowthDays => regrowthDays;
        public bool EdibleByHerbivores => edibleByHerbivores;
        public float FoodValue => foodValue;
        public bool RenderOnly => renderOnly;
        public bool PondVegetation => pondVegetation;
        public bool IsDrop => isDrop;
        public int PickupPriority => pickupPriority;

        public ResourceSaveData()
        {
        }

        public ResourceSaveData(
            string resourceId,
            string resourceType,
            float x,
            float y,
            float z,
            int amount,
            int maxAmount,
            bool depleted)
            : this(resourceId, resourceType, x, y, z, amount, maxAmount, depleted, depleted ? 0f : 1f, 0, false, 0f, false, false, false, 0)
        {
        }

        public ResourceSaveData(
            string resourceId,
            string resourceType,
            float x,
            float y,
            float z,
            int amount,
            int maxAmount,
            bool depleted,
            float growthProgress,
            int regrowthDays,
            bool edibleByHerbivores,
            float foodValue,
            bool renderOnly,
            bool pondVegetation,
            bool isDrop,
            int pickupPriority)
        {
            this.resourceId = resourceId ?? string.Empty;
            this.resourceType = resourceType ?? string.Empty;
            this.x = x;
            this.y = y;
            this.z = z;
            this.amount = Math.Max(0, amount);
            this.maxAmount = Math.Max(0, maxAmount);
            this.depleted = depleted;
            this.growthProgress = Math.Max(0f, Math.Min(1f, growthProgress));
            this.regrowthDays = Math.Max(0, regrowthDays);
            this.edibleByHerbivores = edibleByHerbivores;
            this.foodValue = Math.Max(0f, foodValue);
            this.renderOnly = renderOnly;
            this.pondVegetation = pondVegetation;
            this.isDrop = isDrop;
            this.pickupPriority = Math.Max(0, pickupPriority);
        }
    }
}
