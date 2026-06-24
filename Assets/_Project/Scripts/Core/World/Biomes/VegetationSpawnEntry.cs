using System;

namespace ApexShift.Core.World.Biomes
{
    public readonly struct VegetationSpawnEntry
    {
        public VegetationSpawnEntry(
            string roleId,
            int count,
            float weight,
            float minScale,
            float maxScale,
            string resourceKind,
            bool harvestable)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                throw new ArgumentException("Vegetation role id cannot be empty.", nameof(roleId));
            }

            RoleId = BiomeId.NormalizeId(roleId);
Count = Math.Max(0, count);
            Weight = Math.Max(0f, weight);
            MinScale = Math.Max(0.01f, minScale);
            MaxScale = Math.Max(MinScale, maxScale);
            ResourceKind = string.IsNullOrWhiteSpace(resourceKind) ? string.Empty : resourceKind.Trim().ToLowerInvariant();
            Harvestable = harvestable;
        }

        public string RoleId { get; }
        public int Count { get; }
        public float Weight { get; }
        public float MinScale { get; }
        public float MaxScale { get; }
        public string ResourceKind { get; }
        public bool Harvestable { get; }
    }
}
