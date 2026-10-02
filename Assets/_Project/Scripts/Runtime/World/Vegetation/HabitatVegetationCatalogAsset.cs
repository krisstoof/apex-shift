using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [CreateAssetMenu(menuName = "Apex Shift/World/Habitat Vegetation Catalog", fileName = "HabitatVegetationCatalog")]
    public sealed class HabitatVegetationCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<HabitatVegetationProfileAsset> profiles = new List<HabitatVegetationProfileAsset>();
        [NonSerialized] private Dictionary<string, HabitatVegetationProfileAsset> lookup;

        public IReadOnlyList<HabitatVegetationProfileAsset> Profiles => profiles;

        public bool TryGetProfile(string habitatId, out HabitatVegetationProfileAsset profile)
        {
            EnsureLookup();
            return lookup.TryGetValue((habitatId ?? string.Empty).Trim().ToLowerInvariant(), out profile);
        }

        public HabitatVegetationProfileAsset GetProfile(string habitatId)
        {
            TryGetProfile(habitatId, out HabitatVegetationProfileAsset profile);
            return profile;
        }

        public void SetProfiles(IEnumerable<HabitatVegetationProfileAsset> values)
        {
            profiles = values == null ? new List<HabitatVegetationProfileAsset>() : new List<HabitatVegetationProfileAsset>(values);
            lookup = null;
        }

        public List<string> Validate(VegetationCatalogAsset speciesCatalog)
        {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (profiles == null) { errors.Add("Habitat vegetation catalog has a null profile list."); return errors; }
            foreach (HabitatVegetationProfileAsset profile in profiles)
            {
                if (profile == null) { errors.Add("Habitat vegetation catalog has a null profile."); continue; }
                if (!seen.Add(profile.HabitatId)) errors.Add("Duplicate habitat profile ID '" + profile.HabitatId + "'.");
                errors.AddRange(profile.Validate(speciesCatalog));
            }
            return errors;
        }

        private void OnEnable() => lookup = null;
        private void OnValidate() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, HabitatVegetationProfileAsset>(StringComparer.Ordinal);
            if (profiles == null) return;
            foreach (HabitatVegetationProfileAsset profile in profiles)
                if (profile != null && HabitatVegetationProfileAsset.IsValidHabitatId(profile.HabitatId)
                    && !lookup.ContainsKey(profile.HabitatId))
                    lookup.Add(profile.HabitatId, profile);
        }
    }
}
