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
        private const string DefaultVisualFolder = "Assets/_Project/Prefabs/World/Resources/Embersstorm/";

        [MenuItem("Apex Shift/World/Create or Update Vegetation Data")]
        public static void CreateOrUpdateAssets()
        {
            EnsureAssets();
            TropicalSpeedTreeVegetationBinder.SaveDataAssets();
            Debug.Log("Vegetation species and tropical habitat profiles created/updated. Assigned visual/stump prefabs preserved.");
        }

        public static void EnsureAssets()
        {
            // A migrated catalog stays migrated. Availability of exports alone never activates the plan.
            if (TropicalSpeedTreeVegetationBinder.IsActivated)
            {
                var manifest = TropicalSpeedTreeManifest.Load();
                TropicalSpeedTreeVegetationBinder.RequireReady(manifest);
                EnsureTropicalAssets(manifest, false);
                return;
            }
            EnsureFolder(Root);
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ProfilesFolder);
            var habitats = HabitatVegetationProfileAsset.CanonicalHabitatIds;
            var species = new List<VegetationSpeciesAsset>
            {
                EnsureSpecies("tree_leafy_01", "Leafy Tree 01", VegetationCategory.Tree, VegetationForm.CanopyTree, 4.2f, .20f, 1.9f, .95f, 100f, 5, 1.2f, habitats, true, "leafy_tree", "ES_LeafyTree.prefab"),
                EnsureSpecies("tree_conifer_01", "Conifer Tree 01", VegetationCategory.Tree, VegetationForm.CanopyTree, 3.8f, .16f, 2.1f, 1.05f, 120f, 6, 1.35f, new[] { HabitatIds.RockyUpland }, true, "conifer_tree", "ES_ConiferTree.prefab"),
                EnsureSpecies("tree_dead_01", "Dead Tree 01", VegetationCategory.DeadTree, VegetationForm.StandingDeadTree, 4.5f, .18f, 1.5f, .75f, 70f, 4, .9f, habitats, true, "dry_tree", "ES_DryTree.prefab"),
                EnsureSpecies("shrub_forest_01", "Forest Shrub 01", VegetationCategory.Shrub, VegetationForm.Shrub, 1.4f, .08f, .6f, .3f, 100f, 0, 0f, habitats, true, "berry_bush", "ES_BerryBush.prefab"),
                EnsureSpecies("groundcover_forest_01", "Forest Groundcover 01", VegetationCategory.GroundCover, VegetationForm.GroundCover, .55f, .05f, .25f, .125f, 100f, 0, 0f, habitats, false, string.Empty, "ES_GrassPatch.prefab")
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

        // Called only after a complete preflight, within the binder's rollback transaction.
        internal static void EnsureTropicalAssets(TropicalSpeedTreeManifest manifest, bool bindCanonicalVisuals)
        {
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ProfilesFolder);
            var species = new List<VegetationSpeciesAsset>();
            var byId = new Dictionary<string, VegetationSpeciesAsset>();
            foreach (var entry in manifest.assets)
            {
                if (entry.hero) continue;
                var asset = LoadOrCreate<VegetationSpeciesAsset>(SpeciesFolder + "/" + entry.speciesId + ".asset");
                var visual = !bindCanonicalVisuals && asset.VisualPrefab != null ? asset.VisualPrefab
                    : AssetDatabase.LoadAssetAtPath<GameObject>(entry.WrapperPath);
                if (visual == null) throw new FileNotFoundException("Required canonical SpeedTree wrapper is missing.", entry.WrapperPath);
                bool tree = entry.IsTree;
                // Existing four species retain GUID and optional depleted visual. Decoratives are not promoted as trees.
                asset.Configure(entry.speciesId, entry.modelName, visual, entry.Category, .8f, 1.2f, entry.spacing,
                    entry.minSlope, entry.maxSlope, entry.minElevation, entry.maxElevation,
                    entry.minMoisture, entry.maxMoisture, entry.allowedHabitats, true, entry.harvestable,
                    entry.resourceKind, asset.DepletedVisualPrefab,
                    tree || entry.speciesId == "shrub_forest_01" ? VegetationCollisionMode.GameplayResource : VegetationCollisionMode.None,
                    tree ? entry.trunkRadius : .05f, tree ? entry.trunkHeight : .25f, tree ? entry.trunkCenterY : .125f,
                    entry.category == "DeadTree" ? 70 : 100, tree ? (entry.category == "DeadTree" ? 4 : 5) : 0,
                    tree ? (entry.category == "DeadTree" ? .9f : 1.2f) : 0f);
                asset.ConfigureEnvironment(entry.Form, entry.minCoast, entry.maxCoast, entry.Terrains);
                EditorUtility.SetDirty(asset);
                species.Add(asset);
                byId.Add(entry.speciesId, asset);
            }
            // Compatibility asset stays available, with its existing ID, visual and resource kind; never in the active mix.
            var conifer = LoadOrCreate<VegetationSpeciesAsset>(SpeciesFolder + "/tree_conifer_01.asset");
            if (conifer.VisualPrefab == null)
                conifer = EnsureSpecies("tree_conifer_01", "Conifer Tree 01", VegetationCategory.Tree, VegetationForm.CanopyTree,
                    3.8f, .16f, 2.1f, 1.05f, 120f, 6, 1.35f, new[] { HabitatIds.RockyUpland }, true, "conifer_tree", "ES_ConiferTree.prefab");
            species.Add(conifer);
            var catalog = LoadOrCreate<VegetationCatalogAsset>(CatalogPath);
            catalog.SetSpecies(species);
            EditorUtility.SetDirty(catalog);
            var profiles = new List<HabitatVegetationProfileAsset>();
            foreach (var definition in manifest.profiles)
            {
                var profile = LoadOrCreate<HabitatVegetationProfileAsset>(ProfilesFolder + "/" + definition.habitatId + "_vegetation.asset");
                var entries = new List<HabitatVegetationSpeciesEntry>();
                foreach (var weight in definition.entries)
                {
                    var item = byId[weight.speciesId];
                    float multiplier = definition.habitatId == HabitatIds.Coast
                        ? item.Category == VegetationCategory.Tree ? .25f : item.Category == VegetationCategory.Shrub ? .6f : 1f : 1f;
                    entries.Add(new HabitatVegetationSpeciesEntry(item, weight.weight, multiplier));
                }
                profile.Configure(definition.habitatId, definition.density, entries);
                EditorUtility.SetDirty(profile);
                profiles.Add(profile);
            }
            var habitatCatalog = LoadOrCreate<HabitatVegetationCatalogAsset>(HabitatCatalogPath);
            habitatCatalog.SetProfiles(profiles);
            EditorUtility.SetDirty(habitatCatalog);
            var biomeCatalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(BiomeCatalogPath);
            if (biomeCatalog == null) throw new FileNotFoundException("Required world data anchor is missing.", BiomeCatalogPath);
            biomeCatalog.SetHabitatVegetationCatalog(habitatCatalog);
            EditorUtility.SetDirty(biomeCatalog);
        }

        private static VegetationSpeciesAsset EnsureSpecies(string id, string label, VegetationCategory category,
            VegetationForm form, float spacing, float trunkRadius, float trunkHeight, float trunkCenterY,
            float health, int regrowthDays, float fallDuration, IEnumerable<string> habitats, bool harvestable, string resourceKind,
            string defaultPrefabName)
        {
            var asset = LoadOrCreate<VegetationSpeciesAsset>(SpeciesFolder + "/" + id + ".asset");
            GameObject visual = asset.VisualPrefab;
            if (visual == null)
            {
                string defaultPrefabPath = DefaultVisualFolder + defaultPrefabName;
                visual = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPrefabPath);
                if (visual == null)
                    throw new FileNotFoundException(
                        $"Vegetation species '{id}' has no assigned VisualPrefab and its default prefab is missing: {defaultPrefabPath}",
                        defaultPrefabPath);
            }
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
