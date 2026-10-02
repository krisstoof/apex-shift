using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [CreateAssetMenu(menuName = "Apex Shift/World/Vegetation Catalog", fileName = "VegetationCatalog")]
    public sealed class VegetationCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<VegetationSpeciesAsset> species = new List<VegetationSpeciesAsset>();
        [NonSerialized] private Dictionary<string, VegetationSpeciesAsset> lookup;

        public IReadOnlyList<VegetationSpeciesAsset> Species => species;

        public VegetationSpeciesAsset GetSpecies(string id)
        {
            TryGetSpecies(id, out VegetationSpeciesAsset result);
            return result;
        }

        public bool TryGetSpecies(string id, out VegetationSpeciesAsset result)
        {
            EnsureLookup();
            return lookup.TryGetValue(VegetationSpeciesAsset.NormalizeSpeciesId(id), out result);
        }

        public void SetSpecies(IEnumerable<VegetationSpeciesAsset> assets)
        {
            species = assets == null ? new List<VegetationSpeciesAsset>() : new List<VegetationSpeciesAsset>(assets);
            lookup = null;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (species == null)
            {
                errors.Add("Vegetation catalog has a null species list.");
                return errors;
            }
            foreach (VegetationSpeciesAsset item in species)
            {
                if (item == null)
                {
                    errors.Add("Vegetation catalog contains a null species asset.");
                    continue;
                }
                if (!seen.Add(item.SpeciesId)) errors.Add($"Vegetation catalog contains duplicate species ID '{item.SpeciesId}'.");
                errors.AddRange(item.Validate());
            }
            return errors;
        }

        private void OnEnable() => lookup = null;
        private void OnValidate() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, VegetationSpeciesAsset>(StringComparer.Ordinal);
            if (species == null) return;
            foreach (VegetationSpeciesAsset item in species)
            {
                if (item == null) continue;
                string id = VegetationSpeciesAsset.NormalizeSpeciesId(item.SpeciesId);
                if (id.Length > 0 && !lookup.ContainsKey(id)) lookup.Add(id, item);
            }
        }
    }
}
