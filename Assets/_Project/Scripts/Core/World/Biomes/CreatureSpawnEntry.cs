using System;

namespace ApexShift.Core.World.Biomes
{
    public readonly struct CreatureSpawnEntry
    {
        public CreatureSpawnEntry(string creatureId, int minCount, int maxCount, float weight)
        {
            if (string.IsNullOrWhiteSpace(creatureId))
            {
                throw new ArgumentException("Creature id cannot be empty.", nameof(creatureId));
            }

            CreatureId = creatureId.Trim().ToLowerInvariant();
            MinCount = Math.Max(0, minCount);
            MaxCount = Math.Max(MinCount, maxCount);
            Weight = Math.Max(0f, weight);
        }

        public string CreatureId { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public float Weight { get; }
    }
}
