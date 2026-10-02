using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [Serializable]
    public sealed class HabitatVegetationSpeciesEntry
    {
        [SerializeField] private VegetationSpeciesAsset species;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float densityMultiplier = 1f;

        public VegetationSpeciesAsset Species => species;
        public float Weight => weight;
        public float DensityMultiplier => densityMultiplier;

        public HabitatVegetationSpeciesEntry() { }

        public HabitatVegetationSpeciesEntry(VegetationSpeciesAsset value, float selectionWeight, float density)
        {
            species = value;
            weight = selectionWeight;
            densityMultiplier = density;
        }
    }

    [CreateAssetMenu(menuName = "Apex Shift/World/Habitat Vegetation Profile", fileName = "HabitatVegetationProfile")]
    public sealed class HabitatVegetationProfileAsset : ScriptableObject
    {
        [SerializeField] private string habitatId = string.Empty;
        [SerializeField, Min(0f)] private float overallDensity = 1f;
        [SerializeField] private List<HabitatVegetationSpeciesEntry> species = new List<HabitatVegetationSpeciesEntry>();

        public static readonly IReadOnlyList<string> CanonicalHabitatIds = Array.AsReadOnly(new[]
        {
            global::ApexShift.Runtime.World.Environment.HabitatIds.Coast,
            global::ApexShift.Runtime.World.Environment.HabitatIds.LowlandJungle,
            global::ApexShift.Runtime.World.Environment.HabitatIds.JungleInterior,
            global::ApexShift.Runtime.World.Environment.HabitatIds.WetJungle,
            global::ApexShift.Runtime.World.Environment.HabitatIds.RockyUpland
        });

        public static bool IsValidHabitatId(string id)
        {
            foreach (string known in CanonicalHabitatIds)
                if (string.Equals(known, id, StringComparison.Ordinal)) return true;
            return false;
        }

        public string HabitatId => habitatId;
        public float OverallDensity => overallDensity;
        public IReadOnlyList<HabitatVegetationSpeciesEntry> Species => species;

        public void Configure(string id, float density, IEnumerable<HabitatVegetationSpeciesEntry> entries)
        {
            habitatId = VegetationSpeciesAsset.NormalizeSpeciesId(id);
            overallDensity = density;
            species = entries == null ? new List<HabitatVegetationSpeciesEntry>() : new List<HabitatVegetationSpeciesEntry>(entries);
        }

        public List<string> Validate(VegetationCatalogAsset catalog)
        {
            var errors = new List<string>();
            string normalized = VegetationSpeciesAsset.NormalizeSpeciesId(habitatId);
            if (string.IsNullOrWhiteSpace(habitatId) || !string.Equals(habitatId, normalized, StringComparison.Ordinal))
                errors.Add($"Profile '{name}' has invalid habitat ID '{habitatId}'.");
            if (!IsValidHabitatId(normalized))
                errors.Add($"Profile '{name}' references unknown habitat ID '{habitatId}'.");
            if (!IsFinite(overallDensity) || overallDensity < 0f)
                errors.Add($"Profile '{name}' has invalid overall density {overallDensity}.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (species == null)
            {
                errors.Add($"Profile '{name}' has a null species entry list.");
                return errors;
            }

            foreach (HabitatVegetationSpeciesEntry entry in species)
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
                if (!entry.Species.AllowsHabitat(normalized))
                    errors.Add($"Species '{id}' is not allowed in profile habitat '{normalized}'.");
            }
            return errors;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
