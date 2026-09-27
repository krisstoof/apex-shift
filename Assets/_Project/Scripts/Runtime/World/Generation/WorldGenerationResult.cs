using System.Collections.Generic;
using System;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WorldGenerationResult
    {
        public int Seed { get; set; }
        public int BiomeCount { get; set; }
        public int ResourceCount { get; set; }
        public int SpawnAttempts { get; set; }
        public int VegetationInstanceCount { get; private set; }
        public List<GeneratedBiomeRegion> Regions { get; } = new List<GeneratedBiomeRegion>();
        private readonly Dictionary<string, int> vegetationCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> VegetationCounts => vegetationCounts;

        public void RecordVegetation(string biomeId, string speciesId)
        {
            string key = (biomeId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            vegetationCounts.TryGetValue(key, out int count);
            vegetationCounts[key] = count + 1;
            VegetationInstanceCount++;
        }

        public int GetVegetationCount(string biomeId, string speciesId)
        {
            string key = (biomeId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            return vegetationCounts.TryGetValue(key, out int count) ? count : 0;
        }
    }
}
