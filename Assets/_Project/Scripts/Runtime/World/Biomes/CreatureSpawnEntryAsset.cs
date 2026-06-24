using System;
using ApexShift.Core.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    [Serializable]
    public sealed class CreatureSpawnEntryAsset
    {
        [SerializeField] private string creatureId = string.Empty;
        [SerializeField] private int minCount;
        [SerializeField] private int maxCount;
        [SerializeField] private float weight = 1f;

        public CreatureSpawnEntryAsset()
        {
        }

        public CreatureSpawnEntryAsset(string creatureId, int minCount, int maxCount, float weight)
        {
            this.creatureId = creatureId;
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.weight = weight;
        }

        public string CreatureId => creatureId ?? string.Empty;
        public int MinCount => Mathf.Max(0, minCount);
        public int MaxCount => Mathf.Max(MinCount, maxCount);
        public float Weight => Mathf.Max(0f, weight);

        public CreatureSpawnEntry ToCore()
        {
            return new CreatureSpawnEntry(CreatureId, MinCount, MaxCount, Weight);
        }
    }
}
