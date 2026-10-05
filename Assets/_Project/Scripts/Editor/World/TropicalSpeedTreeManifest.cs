using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;
using UnityEngine;

namespace ApexShift.Editor.World
{
    // Editor-only staging data. Loading this plan never changes the production catalog.
    [Serializable]
    public sealed class TropicalSpeedTreeManifest
    {
        public const string Path = "Tools/SpeedTree/Tropical/TropicalSpeedTreeManifest.json";
        public const string ModelRoot = "Assets/_Project/Art/SpeedTree/Tropical/";
        public const string WrapperRoot = "Assets/_Project/Prefabs/World/Vegetation/SpeedTree/";
        public int schemaVersion;
        public TropicalSpeedTreeEntry[] assets;
        public TropicalSpeedTreeProfile[] profiles;

        public static TropicalSpeedTreeManifest Load()
        {
            var manifest = JsonUtility.FromJson<TropicalSpeedTreeManifest>(File.ReadAllText(Path));
            var errors = manifest == null ? new List<string> { "Empty SpeedTree manifest." } : manifest.Validate();
            if (errors.Count != 0) throw new InvalidDataException(string.Join("\n", errors));
            return manifest;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (schemaVersion != 1) errors.Add("Unsupported SpeedTree manifest schema.");
            if (assets == null || profiles == null) { errors.Add("Missing manifest assets/profiles."); return errors; }
            if (assets.Length != 26 || assets.Count(a => a != null && a.hero) != 1)
                errors.Add("Expected 25 species and one non-procedural hero export.");
            var species = new Dictionary<string, TropicalSpeedTreeEntry>(StringComparer.Ordinal);
            var models = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in assets)
            {
                if (asset == null) { errors.Add("Null manifest asset."); continue; }
                if (string.IsNullOrEmpty(asset.modelName) || !asset.modelName.StartsWith("ST_", StringComparison.Ordinal)
                    || asset.modelName.IndexOfAny(new[] { '/', '\\', '.' }) >= 0 || !models.Add(asset.modelName))
                    errors.Add("Invalid or duplicate model name: " + asset.modelName);
                if (!new[] { "Canopy", "Understory", "GroundCover", "Hero" }.Contains(asset.folder))
                    errors.Add("Invalid model folder: " + asset.folder);
                if (!Enum.TryParse(asset.category, out VegetationCategory _) || !Enum.TryParse(asset.form, out VegetationForm _))
                    errors.Add("Invalid category/form: " + asset.modelName);
                if (asset.hero)
                {
                    if (!string.IsNullOrEmpty(asset.speciesId) || asset.harvestable || asset.folder != "Hero")
                        errors.Add("Hero must not be a harvestable procedural species.");
                    continue;
                }
                if (string.IsNullOrEmpty(asset.speciesId) || asset.speciesId != VegetationSpeciesAsset.NormalizeSpeciesId(asset.speciesId)
                    || species.ContainsKey(asset.speciesId)) errors.Add("Invalid or duplicate species: " + asset.speciesId);
                else species.Add(asset.speciesId, asset);
                if (asset.allowedHabitats == null || asset.allowedHabitats.Length == 0
                    || asset.allowedHabitats.Distinct().Count() != asset.allowedHabitats.Length
                    || asset.allowedHabitats.Any(h => !HabitatVegetationProfileAsset.IsValidHabitatId(h)))
                    errors.Add("Invalid habitat eligibility: " + asset.speciesId);
                if (asset.terrainTypes == null || asset.terrainTypes.Length == 0
                    || asset.terrainTypes.Any(t => !Enum.TryParse(t, out TerrainType terrain) || terrain == TerrainType.Water))
                    errors.Add("Invalid terrain eligibility: " + asset.speciesId);
                if (!Finite(asset.minHeight) || !Finite(asset.maxHeight) || asset.minHeight <= 0 || asset.maxHeight < asset.minHeight
                    || !Finite(asset.spacing) || asset.spacing <= 0
                    || !Range(asset.minSlope, asset.maxSlope, 90) || !Range(asset.minElevation, asset.maxElevation, 1)
                    || !Range(asset.minMoisture, asset.maxMoisture, 1) || !Range(asset.minCoast, asset.maxCoast, float.MaxValue))
                    errors.Add("Invalid environmental/dimension range: " + asset.speciesId);
                if (asset.IsTree && (!asset.harvestable || asset.resourceKind != (asset.category == "DeadTree" ? "dry_tree" : "leafy_tree")))
                    errors.Add("Trees must use the existing harvestable leafy_tree/dry_tree contracts.");
                if (!asset.IsTree && asset.speciesId != "shrub_forest_01" && (asset.harvestable || !string.IsNullOrEmpty(asset.resourceKind)))
                    errors.Add("Decorative vegetation must not be harvestable: " + asset.speciesId);
                string template = asset.speciesId == "shrub_forest_01" ? "ES_BerryBush.prefab"
                    : asset.speciesId == "groundcover_forest_01" ? "ES_GrassPatch.prefab" : string.Empty;
                if (asset.gameplayTemplate != template
                    || (asset.speciesId == "shrub_forest_01" && (!asset.harvestable || asset.resourceKind != "berry_bush")))
                    errors.Add("Invalid food/resource template contract: " + asset.speciesId);
            }
            var habitats = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (profile == null) { errors.Add("Null manifest profile."); continue; }
                if (!HabitatVegetationProfileAsset.IsValidHabitatId(profile.habitatId) || !habitats.Add(profile.habitatId))
                    errors.Add("Invalid or duplicate habitat profile: " + profile.habitatId);
                if (!Finite(profile.density) || profile.density <= 0 || profile.entries == null || profile.entries.Length == 0)
                { errors.Add("Invalid profile density/entries: " + profile.habitatId); continue; }
                var seen = new HashSet<string>();
                foreach (var entry in profile.entries)
                    if (entry == null || string.IsNullOrEmpty(entry.speciesId) || !seen.Add(entry.speciesId)
                        || !species.TryGetValue(entry.speciesId, out var asset) || asset.allowedHabitats == null
                        || !asset.allowedHabitats.Contains(profile.habitatId) || !Finite(entry.weight) || entry.weight <= 0)
                        errors.Add("Invalid/disallowed profile membership: " + profile.habitatId + "/" + entry?.speciesId);
            }
            if (!habitats.SetEquals(HabitatVegetationProfileAsset.CanonicalHabitatIds)) errors.Add("Expected exactly five land habitat profiles.");
            return errors;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Range(float min, float max, float limit) => Finite(min) && Finite(max) && min >= 0 && max >= min && max <= limit;
    }

    [Serializable]
    public sealed class TropicalSpeedTreeEntry
    {
        public string speciesId, modelName, folder, category, form, resourceKind, gameplayTemplate;
        public bool hero, harvestable, authoringReviewed, windReviewed;
        public string[] allowedHabitats, terrainTypes;
        public float minHeight, maxHeight, spacing, minSlope, maxSlope, minElevation, maxElevation,
            minMoisture, maxMoisture, minCoast, maxCoast;
        // Zero means not measured, never a fallback trunk collider.
        public float trunkRadius, trunkHeight, trunkCenterY;
        public string source, vendor, license, attribution;
        public string ModelPath => TropicalSpeedTreeManifest.ModelRoot + folder + "/" + modelName + ".st9";
        public string WrapperPath => TropicalSpeedTreeManifest.WrapperRoot + modelName + ".prefab";
        public bool IsTree => category == "Tree" || category == "DeadTree";
        public VegetationCategory Category => (VegetationCategory)Enum.Parse(typeof(VegetationCategory), category);
        public VegetationForm Form => (VegetationForm)Enum.Parse(typeof(VegetationForm), form);
        public TerrainType[] Terrains => terrainTypes.Select(t => (TerrainType)Enum.Parse(typeof(TerrainType), t)).ToArray();
    }

    [Serializable]
    public sealed class TropicalSpeedTreeProfile
    {
        public string habitatId;
        public float density;
        public TropicalSpeedTreeWeight[] entries;
    }

    [Serializable]
    public sealed class TropicalSpeedTreeWeight
    {
        public string speciesId;
        public float weight;
    }
}
