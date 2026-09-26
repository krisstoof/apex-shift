using System.Collections.Generic;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Validation
{
    public static class VegetationDataValidator
    {
        private const string VegetationCatalogPath = "Assets/_Project/Data/Vegetation/VegetationCatalog.asset";
        private const string BiomeCatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";

        [MenuItem("Apex Shift/Validation/Validate Vegetation Data")]
        public static void ValidateFromMenu()
        {
            List<string> errors = CollectProblems();
            if (errors.Count == 0) Debug.Log("Apex Shift vegetation data is valid.");
            else Debug.LogError("Apex Shift vegetation data is invalid:\n- " + string.Join("\n- ", errors));
        }

        public static bool Validate(bool logResult = true)
        {
            List<string> errors = CollectProblems();
            if (errors.Count == 0)
            {
                if (logResult) Debug.Log("Apex Shift vegetation data is valid.");
                return true;
            }
            if (logResult) Debug.LogError("Apex Shift vegetation data is invalid:\n- " + string.Join("\n- ", errors));
            return false;
        }

        public static void ValidateOrThrow()
        {
            List<string> errors = CollectProblems();
            if (errors.Count > 0) throw new System.InvalidOperationException("Apex Shift vegetation data is invalid:\n- " + string.Join("\n- ", errors));
            Debug.Log("Apex Shift vegetation data is valid.");
        }

        private static List<string> CollectProblems()
        {
            var errors = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>(VegetationCatalogPath);
            var biomeCatalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(BiomeCatalogPath);
            if (catalog == null) errors.Add("Missing vegetation catalog at " + VegetationCatalogPath + ".");
            if (biomeCatalog == null) errors.Add("Missing biome catalog at " + BiomeCatalogPath + ".");
            if (catalog == null || biomeCatalog == null) return errors;
            errors.AddRange(catalog.Validate(biomeCatalog));
            foreach (BiomeDefinitionAsset biome in biomeCatalog.Biomes)
            {
                if (biome == null) { errors.Add("Biome catalog contains a null biome definition."); continue; }
                // Non-land entries such as the synthetic water biome do not own vegetation profiles.
                if (System.Array.IndexOf(VegetationSpeciesAsset.CanonicalBiomeIds,
                        VegetationSpeciesAsset.NormalizeSpeciesId(biome.BiomeId)) < 0)
                    continue;
                if (biome.VegetationProfile == null)
                {
                    errors.Add($"Biome '{biome.BiomeId}' has no vegetation profile assigned.");
                    continue;
                }
                var ids = new HashSet<string>();
                foreach (BiomeDefinitionAsset known in biomeCatalog.Biomes)
                    if (known != null) ids.Add(VegetationSpeciesAsset.NormalizeSpeciesId(known.BiomeId));
                errors.AddRange(biome.VegetationProfile.Validate(ids, catalog));
                if (biome.VegetationProfile.BiomeId != VegetationSpeciesAsset.NormalizeSpeciesId(biome.BiomeId))
                    errors.Add($"Biome '{biome.BiomeId}' references profile for '{biome.VegetationProfile.BiomeId}'.");
            }
            return errors;
        }
    }
}
