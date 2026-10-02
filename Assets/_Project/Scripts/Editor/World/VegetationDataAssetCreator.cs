using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class VegetationDataAssetCreator
    {
        private const string Root = "Assets/_Project/Data/Vegetation";
        private const string SpeciesFolder = Root + "/Species";
        private const string ProfilesFolder = Root + "/Habitats";
        private const string CatalogPath = Root + "/VegetationCatalog.asset";
        private const string HabitatCatalogPath = Root + "/HabitatVegetationCatalog.asset";
        private const string BiomeCatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";

        [MenuItem("Apex Shift/World/Create or Update Vegetation Data")]
        public static void CreateOrUpdateAssets()
        {
            EnsureAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("Vegetation species and tropical habitat profiles created/updated. Assigned visual/stump prefabs preserved.");
        }

        public static void EnsureAssets()
        {
            EnsureFolder(Root);
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ProfilesFolder);
            var habitats = HabitatVegetationProfileAsset.CanonicalHabitatIds;
            var species = new List<VegetationSpeciesAsset>
            {
                EnsureSpecies("tree_leafy_01", "Leafy Tree 01", VegetationCategory.Tree, VegetationForm.CanopyTree, 4.2f, .20f, 1.9f, .95f, 100f, 5, 1.2f, habitats, true, "leafy_tree"),
                EnsureSpecies("tree_conifer_01", "Conifer Tree 01", VegetationCategory.Tree, VegetationForm.CanopyTree, 3.8f, .16f, 2.1f, 1.05f, 120f, 6, 1.35f, new[] { HabitatIds.RockyUpland }, true, "conifer_tree"),
                EnsureSpecies("tree_dead_01", "Dead Tree 01", VegetationCategory.DeadTree, VegetationForm.StandingDeadTree, 4.5f, .18f, 1.5f, .75f, 70f, 4, .9f, habitats, true, "dry_tree"),
                EnsureSpecies("shrub_forest_01", "Forest Shrub 01", VegetationCategory.Shrub, VegetationForm.Shrub, 1.4f, .08f, .6f, .3f, 100f, 0, 0f, habitats, true, "berry_bush"),
                EnsureSpecies("groundcover_forest_01", "Forest Groundcover 01", VegetationCategory.GroundCover, VegetationForm.GroundCover, .55f, .05f, .25f, .125f, 100f, 0, 0f, habitats, false, string.Empty)
            };
            VegetationCatalogAsset catalog = LoadOrCreate<VegetationCatalogAsset>(CatalogPath);
            catalog.SetSpecies(species);
            EditorUtility.SetDirty(catalog);

            var profiles = new List<HabitatVegetationProfileAsset>();
            foreach (string habitat in habitats)
            {
                var profile = LoadOrCreate<HabitatVegetationProfileAsset>(ProfilesFolder + "/" + habitat + "_vegetation.asset");
                var entries = new List<HabitatVegetationSpeciesEntry>();
                // Existing broadleaf/shrub/groundcover visuals form the tropical slice.
                // Conifer/dead assets remain available but are not part of its active mix.
                if (habitat != HabitatIds.RockyUpland)
                    entries.Add(new HabitatVegetationSpeciesEntry(species[0], 1f, habitat == HabitatIds.Coast ? .25f : 1f));
                entries.Add(new HabitatVegetationSpeciesEntry(species[3], 1f, habitat == HabitatIds.Coast ? .6f : 1f));
                entries.Add(new HabitatVegetationSpeciesEntry(species[4], 1f, 1f));
                profile.Configure(habitat, DensityFor(habitat), entries);
                EditorUtility.SetDirty(profile);
                profiles.Add(profile);
            }

            HabitatVegetationCatalogAsset habitatCatalog = LoadOrCreate<HabitatVegetationCatalogAsset>(HabitatCatalogPath);
            habitatCatalog.SetProfiles(profiles);
            EditorUtility.SetDirty(habitatCatalog);
            // Existing serialized RuntimeWorld points at this asset; no scene migration is needed.
            BiomeCatalogAsset biomeCatalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(BiomeCatalogPath);
            if (biomeCatalog == null) throw new FileNotFoundException("Required world data anchor is missing.", BiomeCatalogPath);
            biomeCatalog.SetHabitatVegetationCatalog(habitatCatalog);
            EditorUtility.SetDirty(biomeCatalog);
        }

        private static VegetationSpeciesAsset EnsureSpecies(string id, string label, VegetationCategory category,
            VegetationForm form, float spacing, float trunkRadius, float trunkHeight, float trunkCenterY,
            float health, int regrowthDays, float fallDuration, IEnumerable<string> habitats, bool harvestable, string resourceKind)
        {
            var asset = LoadOrCreate<VegetationSpeciesAsset>(SpeciesFolder + "/" + id + ".asset");
            GameObject visual = asset.VisualPrefab;
            GameObject depleted = asset.DepletedVisualPrefab;
            asset.Configure(id, label, visual, category, .8f, 1.2f, spacing,
                0f, category == VegetationCategory.Tree ? 28f : category == VegetationCategory.GroundCover ? 35f : 40f,
                0f, 1f, 0f, 1f, habitats, true, harvestable, resourceKind, depleted,
                category == VegetationCategory.GroundCover ? VegetationCollisionMode.None : VegetationCollisionMode.GameplayResource,
                trunkRadius, trunkHeight, trunkCenterY, health, regrowthDays, fallDuration);
            asset.ConfigureEnvironment(form, 0f, float.MaxValue,
                category == VegetationCategory.Tree
                    ? new[] { TerrainType.Plain, TerrainType.Forest, TerrainType.Hills, TerrainType.Beach }
                    : null);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static float DensityFor(string habitat)
        {
            switch (habitat)
            {
                case HabitatIds.Coast: return .45f;
                case HabitatIds.LowlandJungle: return 1.55f;
                case HabitatIds.JungleInterior: return 1.85f;
                case HabitatIds.WetJungle: return 2.10f;
                case HabitatIds.RockyUpland: return .45f;
                default: throw new System.ArgumentException("Unknown habitat", nameof(habitat));
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
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
