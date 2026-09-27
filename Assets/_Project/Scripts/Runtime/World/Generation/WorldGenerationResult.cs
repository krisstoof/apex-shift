using System.Collections.Generic;
using System;
using ApexShift.Runtime.World.Vegetation;

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
        private readonly Dictionary<string, int> vegetationCategoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> VegetationCounts => vegetationCounts;
        public IReadOnlyDictionary<string, int> VegetationCategoryCounts => vegetationCategoryCounts;

        public void RecordVegetation(string biomeId, string speciesId, VegetationCategory category)
        {
            string key = (biomeId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            vegetationCounts.TryGetValue(key, out int count);
            vegetationCounts[key] = count + 1;
            string categoryKey = (biomeId ?? string.Empty) + "/" + category;
            vegetationCategoryCounts.TryGetValue(categoryKey, out int categoryCount);
            vegetationCategoryCounts[categoryKey] = categoryCount + 1;
            VegetationInstanceCount++;
        }

        public int GetVegetationCount(string biomeId, string speciesId)
        {
            string key = (biomeId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            return vegetationCounts.TryGetValue(key, out int count) ? count : 0;
        }

        public int GetVegetationCount(string biomeId, VegetationCategory category)
        {
            string key = (biomeId ?? string.Empty) + "/" + category;
            return vegetationCategoryCounts.TryGetValue(key, out int count) ? count : 0;
        }
    }
}
