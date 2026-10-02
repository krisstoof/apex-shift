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
        public WorldGenerationReport Report { get; internal set; }
        public List<GeneratedBiomeRegion> Regions { get; } = new List<GeneratedBiomeRegion>();
        private readonly Dictionary<string, int> vegetationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> vegetationCategoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> vegetationHarvestableCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> vegetationDecorativeCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> VegetationCounts => vegetationCounts;
        public IReadOnlyDictionary<string, int> VegetationCategoryCounts => vegetationCategoryCounts;
        public IReadOnlyDictionary<string, int> VegetationHarvestableCounts => vegetationHarvestableCounts;
        public IReadOnlyDictionary<string, int> VegetationDecorativeCounts => vegetationDecorativeCounts;

        public void RecordVegetation(string habitatId, string speciesId, VegetationCategory category)
            => RecordVegetation(habitatId, speciesId, category, category != VegetationCategory.GroundCover);

        public void RecordVegetation(string habitatId, string speciesId, VegetationCategory category, bool harvestable)
        {
            string key = (habitatId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            vegetationCounts.TryGetValue(key, out int count);
            vegetationCounts[key] = count + 1;
            string categoryKey = (habitatId ?? string.Empty) + "/" + category;
            vegetationCategoryCounts.TryGetValue(categoryKey, out int categoryCount);
            vegetationCategoryCounts[categoryKey] = categoryCount + 1;
            Dictionary<string, int> target = harvestable ? vegetationHarvestableCounts : vegetationDecorativeCounts;
            target.TryGetValue(key, out int kindCount);
            target[key] = kindCount + 1;
            VegetationInstanceCount++;
        }

        public int GetVegetationCount(string habitatId, string speciesId)
        {
            string key = (habitatId ?? string.Empty) + "/" + (speciesId ?? string.Empty);
            return vegetationCounts.TryGetValue(key, out int count) ? count : 0;
        }

        public int GetVegetationCount(string habitatId, VegetationCategory category)
        {
            string key = (habitatId ?? string.Empty) + "/" + category;
            return vegetationCategoryCounts.TryGetValue(key, out int count) ? count : 0;
        }
    }
}
