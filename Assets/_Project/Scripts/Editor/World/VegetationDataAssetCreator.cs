using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class VegetationDataAssetCreator
    {
        private const string Root = "Assets/_Project/Data/Vegetation";
        private const string SpeciesFolder = Root + "/Species";
        private const string ProfilesFolder = Root + "/Biomes";
        private const string CatalogPath = Root + "/VegetationCatalog.asset";
        private const string BiomeCatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";

        [MenuItem("Apex Shift/World/Create or Update Vegetation Data")]
        public static void CreateOrUpdateAssets()
        {
            EnsureAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("Vegetation species, biome profiles, catalog, and biome assignments created/updated. Visual prefab dependencies are reported by vegetation validation.");
        }

        public static void EnsureAssets()
        {
            EnsureFolder(Root);
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ProfilesFolder);

            var species = new List<VegetationSpeciesAsset>
            {
                EnsureSpecies("tree_leafy_01", "Leafy Tree 01", VegetationCategory.Tree, 4.2f, new[] { "hearth_meadow", "westwood", "south_thicket" }, true, "leafy_tree"),
                EnsureSpecies("tree_conifer_01", "Conifer Tree 01", VegetationCategory.Tree, 3.8f, new[] { "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" }, true, "conifer_tree"),
                EnsureSpecies("tree_dead_01", "Dead Tree 01", VegetationCategory.DeadTree, 4.5f, new[] { "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" }, true, "dry_tree"),
                EnsureSpecies("shrub_forest_01", "Forest Shrub 01", VegetationCategory.Shrub, 1.4f, new[] { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" }, true, "berry_bush"),
                EnsureSpecies("groundcover_forest_01", "Forest Groundcover 01", VegetationCategory.GroundCover, 0.55f, new[] { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" }, false, string.Empty)
            };

            VegetationCatalogAsset catalog = LoadOrCreate<VegetationCatalogAsset>(CatalogPath);
            catalog.SetSpecies(species);
            EditorUtility.SetDirty(catalog);

            BiomeCatalogAsset biomeCatalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(BiomeCatalogPath);
            if (biomeCatalog == null) throw new FileNotFoundException("Required biome catalog is missing.", BiomeCatalogPath);
            var profiles = new Dictionary<string, BiomeVegetationProfileAsset>();
            foreach (string biomeId in VegetationSpeciesAsset.CanonicalBiomeIds)
            {
                string path = ProfilesFolder + "/" + biomeId + "_vegetation.asset";
                var profile = LoadOrCreate<BiomeVegetationProfileAsset>(path);
                profile.Configure(biomeId, DensityFor(biomeId), BuildEntries(biomeId, species));
                EditorUtility.SetDirty(profile);
                profiles.Add(biomeId, profile);

                BiomeDefinitionAsset biome = biomeCatalog.GetBiome(biomeId);
                if (biome == null) throw new System.InvalidOperationException("Biome catalog has no definition for '" + biomeId + "'.");
                biome.SetVegetationProfile(profile);
                EditorUtility.SetDirty(biome);
            }
        }

        private static VegetationSpeciesAsset EnsureSpecies(string id, string label, VegetationCategory category, float minimumSpacing, string[] biomes, bool harvestable, string resourceKind)
        {
            var asset = LoadOrCreate<VegetationSpeciesAsset>(SpeciesFolder + "/" + id + ".asset");
            GameObject visual = asset.VisualPrefab; // Preserve any manually assigned content reference.
            GameObject depletedVisual = asset.DepletedVisualPrefab; // Preserve any manually assigned depleted/stump visual.
            asset.Configure(id, label, visual, category, 0.8f, 1.2f,
                minimumSpacing,
                0f, category == VegetationCategory.GroundCover ? 35f : 40f,
                0f, 1f, 0f, 1f, biomes, true, harvestable, resourceKind, depletedVisual,
                category == VegetationCategory.GroundCover ? VegetationCollisionMode.None : VegetationCollisionMode.GameplayResource);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<BiomeVegetationSpeciesEntry> BuildEntries(string biomeId, List<VegetationSpeciesAsset> species)
        {
            var result = new List<BiomeVegetationSpeciesEntry>();
            for (int i = 0; i < species.Count; i++)
            {
                float weight = WeightFor(biomeId, species[i].SpeciesId);
                if (weight > 0f && species[i].AllowsBiome(biomeId))
                    result.Add(new BiomeVegetationSpeciesEntry(species[i], weight, 1f));
            }
            return result;
        }

        private static float WeightFor(string biomeId, string speciesId)
        {
            switch (biomeId)
            {
                case "westwood":
                    switch (speciesId)
                    {
                        case "tree_conifer_01": return 3f;
                        case "tree_leafy_01": return 2f;
                        case "tree_dead_01": return 0.35f;
                        case "shrub_forest_01": return 1.5f;
                        case "groundcover_forest_01": return 2.5f;
                    }
                    break;
                case "south_thicket":
                    switch (speciesId)
                    {
                        case "tree_leafy_01": return 2.2f;
                        case "tree_conifer_01": return 0.8f;
                        case "tree_dead_01": return 0.2f;
                        case "shrub_forest_01": return 3f;
                        case "groundcover_forest_01": return 3.2f;
                    }
                    break;
                case "hearth_meadow":
                    switch (speciesId)
                    {
                        case "tree_leafy_01": return 1f;
                        case "shrub_forest_01": return 0.8f;
                        case "groundcover_forest_01": return 2f;
                    }
                    break;
                case "stoneback_ridge":
                    switch (speciesId)
                    {
                        case "tree_conifer_01": return 2.2f;
                        case "tree_dead_01": return 1.1f;
                        case "shrub_forest_01": return 0.5f;
                        case "groundcover_forest_01": return 0.8f;
                    }
                    break;
                case "redfang_wilds":
                    switch (speciesId)
                    {
                        case "tree_dead_01": return 2.2f;
                        case "tree_conifer_01": return 0.5f;
                        case "shrub_forest_01": return 0.45f;
                        case "groundcover_forest_01": return 0.35f;
                    }
                    break;
            }
            return 0f;
        }

        private static float DensityFor(string id)
        {
            switch (id)
            {
                case "hearth_meadow": return 0.45f;
                case "westwood": return 1.55f;
                case "south_thicket": return 1.70f;
                case "stoneback_ridge": return 0.65f;
                case "redfang_wilds": return 0.55f;
                default: return 1f;
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
