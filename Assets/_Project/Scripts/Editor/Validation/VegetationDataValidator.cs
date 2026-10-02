using System.Collections.Generic;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Validation
{
    public static class VegetationDataValidator
    {
        private const string Root = "Assets/_Project/Data/Vegetation/";
        [MenuItem("Apex Shift/Validation/Validate Vegetation Data")]
        public static void ValidateFromMenu() => Validate();

        public static bool Validate(bool logResult = true)
        {
            List<string> errors = CollectProblems();
            if (logResult)
            {
                if (errors.Count == 0) Debug.Log("Apex Shift vegetation data is valid.");
                else Debug.LogError("Apex Shift vegetation data is invalid:\n- " + string.Join("\n- ", errors));
            }
            return errors.Count == 0;
        }

        public static void ValidateOrThrow()
        {
            List<string> errors = CollectProblems();
            if (errors.Count > 0) throw new System.InvalidOperationException(string.Join("\n", errors));
            Debug.Log("Apex Shift vegetation data is valid.");
        }

        public static List<string> CollectProblems()
        {
            var errors = new List<string>();
            var species = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>(Root + "VegetationCatalog.asset");
            var habitats = AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(Root + "HabitatVegetationCatalog.asset");
            if (species == null) errors.Add("Missing VegetationCatalog.asset.");
            if (habitats == null) errors.Add("Missing HabitatVegetationCatalog.asset.");
            if (species != null)
            {
                errors.AddRange(species.Validate());
                foreach (VegetationSpeciesAsset item in species.Species)
                    if (item != null)
                    {
                        ValidatePrefab(item.VisualPrefab, item.SpeciesId, errors);
                        if (item.DepletedVisualPrefab != null) ValidatePrefab(item.DepletedVisualPrefab, item.SpeciesId + " depleted", errors);
                    }
            }
            if (habitats != null)
            {
                errors.AddRange(habitats.Validate(species));
                foreach (string id in HabitatVegetationProfileAsset.CanonicalHabitatIds)
                    if (habitats.GetProfile(id) == null) errors.Add("Missing habitat profile '" + id + "'.");
            }
            // Optional scene-integration anchor; vegetation data validation itself is neutral.
            var bridge = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>("Assets/_Project/Data/Biomes/BiomeCatalog.asset");
            if (bridge != null && bridge.HabitatVegetationCatalog != habitats)
                errors.Add("World data anchor does not reference the canonical habitat vegetation catalog.");
            return errors;
        }

        private static void ValidatePrefab(GameObject prefab, string label, List<string> errors)
        {
            if (prefab == null) return; // Species validation reports the missing visual.
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                    errors.Add(label + " prefab has a missing script dependency.");
                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == null) errors.Add(label + " prefab has a missing mesh dependency.");
                SkinnedMeshRenderer skin = child.GetComponent<SkinnedMeshRenderer>();
                if (skin != null && skin.sharedMesh == null) errors.Add(label + " prefab has a missing skinned mesh dependency.");
            }
        }
    }
}
