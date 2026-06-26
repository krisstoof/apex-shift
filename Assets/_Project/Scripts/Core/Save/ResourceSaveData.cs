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

        public string ResourceId => resourceId;
        public string ResourceType => resourceType;
        public int Amount => amount;
        public int MaxAmount => maxAmount;
        public bool Depleted => depleted;

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
        {
            this.resourceId = resourceId ?? string.Empty;
            this.resourceType = resourceType ?? string.Empty;
            this.x = x;
            this.y = y;
            this.z = z;
            this.amount = Math.Max(0, amount);
            this.maxAmount = Math.Max(0, maxAmount);
            this.depleted = depleted;
        }
    }
}
