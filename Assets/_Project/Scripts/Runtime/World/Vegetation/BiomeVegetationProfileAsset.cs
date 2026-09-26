using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [Serializable]
    public sealed class BiomeVegetationSpeciesEntry
    {
        [SerializeField] private VegetationSpeciesAsset species;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float densityMultiplier = 1f;

        public VegetationSpeciesAsset Species => species;
        public float Weight => weight;
        public float DensityMultiplier => densityMultiplier;

        public BiomeVegetationSpeciesEntry() { }

        public BiomeVegetationSpeciesEntry(VegetationSpeciesAsset value, float selectionWeight, float density)
        {
            species = value;
            weight = selectionWeight;
            densityMultiplier = density;
        }
    }

    [CreateAssetMenu(menuName = "Apex Shift/World/Biome Vegetation Profile", fileName = "BiomeVegetationProfile")]
    public sealed class BiomeVegetationProfileAsset : ScriptableObject
    {
        [SerializeField] private string biomeId = string.Empty;
        [SerializeField, Min(0f)] private float overallDensity = 1f;
        [SerializeField] private List<BiomeVegetationSpeciesEntry> species = new List<BiomeVegetationSpeciesEntry>();

        public string BiomeId => biomeId;
        public float OverallDensity => overallDensity;
        public IReadOnlyList<BiomeVegetationSpeciesEntry> Species => species;

        public void Configure(string id, float density, IEnumerable<BiomeVegetationSpeciesEntry> entries)
        {
            biomeId = VegetationSpeciesAsset.NormalizeSpeciesId(id);
            overallDensity = density;
            species = entries == null ? new List<BiomeVegetationSpeciesEntry>() : new List<BiomeVegetationSpeciesEntry>(entries);
        }

        public List<string> Validate(ICollection<string> validBiomeIds, VegetationCatalogAsset catalog)
        {
            var errors = new List<string>();
            string normalized = VegetationSpeciesAsset.NormalizeSpeciesId(biomeId);
            if (string.IsNullOrWhiteSpace(biomeId) || !string.Equals(biomeId, normalized, StringComparison.Ordinal))
                errors.Add($"Profile '{name}' has invalid biome ID '{biomeId}'.");
            if (validBiomeIds == null || !validBiomeIds.Contains(normalized))
                errors.Add($"Profile '{name}' references unknown biome ID '{biomeId}'.");
            if (!IsFinite(overallDensity) || overallDensity < 0f)
                errors.Add($"Profile '{name}' has invalid overall density {overallDensity}.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (species == null)
            {
                errors.Add($"Profile '{name}' has a null species entry list.");
                return errors;
            }

            foreach (BiomeVegetationSpeciesEntry entry in species)
            {
                if (entry == null || entry.Species == null)
                {
                    errors.Add($"Profile '{name}' contains a null species entry/reference.");
                    continue;
                }
                string id = entry.Species.SpeciesId;
                if (!seen.Add(id)) errors.Add($"Profile '{name}' repeats species '{id}'.");
                if (catalog == null || !catalog.TryGetSpecies(id, out VegetationSpeciesAsset catalogSpecies))
                    errors.Add($"Profile '{name}' references species '{id}' missing from the vegetation catalog.");
                else if (catalogSpecies != entry.Species)
                    errors.Add($"Profile '{name}' species '{id}' is not the catalog's canonical asset.");
                if (!IsFinite(entry.Weight) || entry.Weight < 0f)
                    errors.Add($"Profile '{name}' species '{id}' has invalid weight {entry.Weight}.");
                if (!IsFinite(entry.DensityMultiplier) || entry.DensityMultiplier < 0f)
                    errors.Add($"Profile '{name}' species '{id}' has invalid density multiplier {entry.DensityMultiplier}.");
                if (!entry.Species.AllowsBiome(normalized))
                    errors.Add($"Species '{id}' is not allowed in profile biome '{normalized}'.");
            }
            return errors;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
