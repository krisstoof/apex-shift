using System.Collections.Generic;
using ApexShift.Editor.World;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class VegetationDataAssetTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<VegetationSpeciesAsset, VegetationSpeciesAsset> speciesBackups = new Dictionary<VegetationSpeciesAsset, VegetationSpeciesAsset>();
        private readonly List<string> temporaryAssetPaths = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (KeyValuePair<VegetationSpeciesAsset, VegetationSpeciesAsset> pair in speciesBackups)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    EditorUtility.CopySerialized(pair.Value, pair.Key);
                    EditorUtility.SetDirty(pair.Key);
                    Object.DestroyImmediate(pair.Value);
                }
            }
            speciesBackups.Clear();
            foreach (string path in temporaryAssetPaths) AssetDatabase.DeleteAsset(path);
            temporaryAssetPaths.Clear();
            foreach (Object item in created) if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void Species_NormalizesIdAndPreservesConstraints()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset species = CreateSpecies(" Leafy Tree-01 ", visual, new[] { "jungle_interior" }, false, string.Empty);
            Assert.That(species.SpeciesId, Is.EqualTo("leafy_tree_01"));
            Assert.That(species.MinScale, Is.EqualTo(0.7f));
            Assert.That(species.MaxSlopeDegrees, Is.EqualTo(35f));
            Assert.That(species.AllowsHabitat("jungle_interior"), Is.True);
            Assert.That(species.Validate(), Is.Empty);
        }

        [Test]
        public void Catalog_LookupIsByNormalizedStableId_AndReportsDuplicatesAndNulls()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset first = CreateSpecies("tree_a", visual, new[] { "jungle_interior" }, false, "");
            VegetationSpeciesAsset duplicate = CreateSpecies("tree_a", visual, new[] { "jungle_interior" }, false, "");
            VegetationCatalogAsset catalog = Track(ScriptableObject.CreateInstance<VegetationCatalogAsset>());
            catalog.SetSpecies(new[] { first, duplicate, null });
            Assert.That(catalog.TryGetSpecies(" Tree-A ", out VegetationSpeciesAsset found), Is.True);
            Assert.That(found, Is.SameAs(first));
            Assert.That(catalog.GetSpecies("unknown"), Is.Null);
            List<string> errors = catalog.Validate();
            Assert.That(errors.Exists(e => e.Contains("duplicate species ID")), Is.True);
            Assert.That(errors.Exists(e => e.Contains("null species asset")), Is.True);
        }

        [Test]
        public void Profile_RejectsDuplicateMissingAndDisallowedSpecies()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_allowed_interior", visual, new[] { "jungle_interior" }, false, "");
            VegetationCatalogAsset catalog = Track(ScriptableObject.CreateInstance<VegetationCatalogAsset>());
            catalog.SetSpecies(new[] { species });
            HabitatVegetationProfileAsset profile = Track(ScriptableObject.CreateInstance<HabitatVegetationProfileAsset>());
            profile.Configure("wet_jungle", 1f, new[]
            {
                new HabitatVegetationSpeciesEntry(species, 1f, 1f),
                new HabitatVegetationSpeciesEntry(species, 2f, 1f)
            });
            List<string> errors = profile.Validate(catalog);
            Assert.That(errors.Exists(e => e.Contains("repeats species")), Is.True);
            Assert.That(errors.Exists(e => e.Contains("not allowed")), Is.True);
        }

        [Test]
        public void SameVisualCanBeDecorativeOrHarvestable_AndMissingPrefabIsReported()
        {
            GameObject visual = Track(new GameObject("shared visual"));
            VegetationSpeciesAsset decorative = CreateSpecies("tree_decorative", visual, new[] { "jungle_interior" }, false, "");
            VegetationSpeciesAsset harvestable = CreateSpecies("tree_harvestable", visual, new[] { "jungle_interior" }, true, "leafy_tree");
            VegetationSpeciesAsset missing = CreateSpecies("tree_missing_visual", null, new[] { "jungle_interior" }, false, "");
            Assert.That(decorative.Validate(), Is.Empty);
            Assert.That(harvestable.Validate(), Is.Empty);
            Assert.That(missing.Validate().Exists(e => e.Contains("no visual prefab")), Is.True);
        }

        [Test]
        public void EnsureAssets_IsIdempotentAndPreservesAssignedVisualReferences()
        {
            const string root = "Assets/_Project/Data/Vegetation";
            string[] ids = { "tree_leafy_01", "tree_conifer_01", "tree_dead_01", "shrub_forest_01", "groundcover_forest_01" };
            float[] expectedSpacing = { 4.2f, 3.8f, 4.5f, 1.4f, 0.55f };
            float[] expectedTrunkRadii = { 0.20f, 0.16f, 0.18f, 0.08f, 0.05f };
            float[] expectedTrunkHeights = { 1.9f, 2.1f, 1.5f, 0.6f, 0.25f };
            float[] expectedTrunkCenters = { 0.95f, 1.05f, 0.75f, 0.3f, 0.125f };
            float[] expectedTreeHealth = { 100f, 120f, 70f, 100f, 100f };
            int[] expectedRegrowthDays = { 5, 6, 4, 0, 0 };
            float[] expectedFallDuration = { 1.2f, 1.35f, 0.9f, 0f, 0f };
            int speciesCountBefore = AssetDatabase.FindAssets("t:VegetationSpeciesAsset", new[] { root + "/Species" }).Length;
            int profileCountBefore = AssetDatabase.FindAssets("t:HabitatVegetationProfileAsset", new[] { root + "/Habitats" }).Length;
            var visualPrefabs = new Dictionary<string, GameObject>();
            var depletedPrefabs = new Dictionary<string, GameObject>();

            for (int i = 0; i < ids.Length; i++)
            {
                string path = root + "/Species/" + ids[i] + ".asset";
                VegetationSpeciesAsset species = AssetDatabase.LoadAssetAtPath<VegetationSpeciesAsset>(path);
                Assert.That(species, Is.Not.Null, "Missing canonical species asset " + ids[i]);
                VegetationSpeciesAsset backup = ScriptableObject.CreateInstance<VegetationSpeciesAsset>();
                EditorUtility.CopySerialized(species, backup);
                speciesBackups.Add(species, backup);

                visualPrefabs.Add(ids[i], CreateTemporaryPrefab(root + "/__Test_" + ids[i] + "_Visual.prefab"));
                depletedPrefabs.Add(ids[i], CreateTemporaryPrefab(root + "/__Test_" + ids[i] + "_Depleted.prefab"));
                species.Configure(species.SpeciesId, species.DisplayName, visualPrefabs[ids[i]], species.Category,
                    species.MinScale, species.MaxScale, species.MinimumSpacing, species.MinSlopeDegrees, species.MaxSlopeDegrees,
                    species.MinElevation01, species.MaxElevation01, species.MinMoisture01, species.MaxMoisture01,
                    species.AllowedHabitatIds, species.RandomYaw, species.Harvestable, species.ResourceKind,
                    depletedPrefabs[ids[i]], species.CollisionMode);
            }

            VegetationDataAssetCreator.EnsureAssets();
            VegetationDataAssetCreator.EnsureAssets();

            Assert.That(AssetDatabase.FindAssets("t:VegetationSpeciesAsset", new[] { root + "/Species" }).Length, Is.EqualTo(speciesCountBefore));
            Assert.That(AssetDatabase.FindAssets("t:HabitatVegetationProfileAsset", new[] { root + "/Habitats" }).Length, Is.EqualTo(profileCountBefore));
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                VegetationSpeciesAsset species = AssetDatabase.LoadAssetAtPath<VegetationSpeciesAsset>(root + "/Species/" + id + ".asset");
                Assert.That(species.VisualPrefab, Is.SameAs(visualPrefabs[id]), id + " visual prefab was not preserved");
                Assert.That(species.DepletedVisualPrefab, Is.SameAs(depletedPrefabs[id]), id + " depleted prefab was not preserved");
                Assert.That(species.MinimumSpacing, Is.EqualTo(expectedSpacing[i]).Within(0.0001f), id + " spacing was not retained by the data creator");
                Assert.That(species.TrunkRadius, Is.EqualTo(expectedTrunkRadii[i]).Within(0.0001f), id + " trunk radius");
                Assert.That(species.TrunkHeight, Is.EqualTo(expectedTrunkHeights[i]).Within(0.0001f), id + " trunk height");
                Assert.That(species.TrunkCenterY, Is.EqualTo(expectedTrunkCenters[i]).Within(0.0001f), id + " trunk center");
                Assert.That(species.TreeMaxHealth, Is.EqualTo(expectedTreeHealth[i]).Within(0.0001f), id + " tree max health");
                Assert.That(species.TreeRegrowthDays, Is.EqualTo(expectedRegrowthDays[i]), id + " tree regrowth days");
                Assert.That(species.TreeFallDuration, Is.EqualTo(expectedFallDuration[i]).Within(0.0001f), id + " tree fall duration");
            }

            AssertProfileDensity("coast", .45f);
            AssertProfileDensity("lowland_jungle", 1.55f);
            AssertProfileDensity("jungle_interior", 1.85f);
            AssertProfileDensity("wet_jungle", 2.10f);
            AssertProfileDensity("rocky_upland", .45f);
            AssertProfileWeights("coast", ("tree_leafy_01", 1f), ("shrub_forest_01", 1f), ("groundcover_forest_01", 1f));
            foreach (string id in new[] { "lowland_jungle", "jungle_interior", "wet_jungle" })
                AssertProfileWeights(id, ("tree_leafy_01", 1f), ("shrub_forest_01", 1f), ("groundcover_forest_01", 1f));
            AssertProfileWeights("rocky_upland", ("shrub_forest_01", 1f), ("groundcover_forest_01", 1f));
            var habitatCatalog = AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(root + "/HabitatVegetationCatalog.asset");
            Assert.That(habitatCatalog.Profiles.Count, Is.EqualTo(5));
            var anchor = AssetDatabase.LoadAssetAtPath<ApexShift.Runtime.World.Biomes.BiomeCatalogAsset>(
                "Assets/_Project/Data/Biomes/BiomeCatalog.asset");
            Assert.That(anchor.HabitatVegetationCatalog, Is.SameAs(habitatCatalog));
            CollectionAssert.AreEquivalent(HabitatVegetationProfileAsset.CanonicalHabitatIds,
                System.Linq.Enumerable.Select(habitatCatalog.Profiles, profile => profile.HabitatId));
            Assert.That(habitatCatalog.GetProfile("water"), Is.Null);
            Assert.That(habitatCatalog.GetProfile(" WET_JUNGLE "), Is.SameAs(habitatCatalog.GetProfile("wet_jungle")));
            foreach (HabitatVegetationProfileAsset profile in habitatCatalog.Profiles)
                Assert.That(System.Linq.Enumerable.Any(profile.Species, entry => entry.Species.SpeciesId == "tree_conifer_01"), Is.False);

        }

        [TestCase("water")]
        [TestCase("westwood")]
        [TestCase("unknown")]
        public void NeutralDataRejectsWaterLegacyAndUnknownHabitats(string habitat)
        {
            var visual = Track(new GameObject("visual"));
            var species = CreateSpecies("invalid_habitat", visual, new[] { habitat }, false, "");
            Assert.That(species.Validate().Exists(error => error.Contains("unknown habitat")), Is.True);
            Assert.That(species.AllowsHabitat(habitat), Is.False);
            var profile = Track(ScriptableObject.CreateInstance<HabitatVegetationProfileAsset>());
            profile.Configure(habitat, 1f, null);
            Assert.That(profile.Validate(null).Exists(error => error.Contains("unknown habitat")), Is.True);
        }

        [Test]
        public void SpeciesValidatesEnvironmentalDistanceTerrainAndRanges()
        {
            var species = CreateSpecies("invalid_filters", Track(new GameObject("visual")),
                new[] { "wet_jungle" }, false, "");
            species.ConfigureEnvironment(VegetationForm.CanopyTree, 5f, 2f, new[] { TerrainType.Water, (TerrainType)999 });
            List<string> errors = species.Validate();
            Assert.That(errors.Exists(error => error.Contains("distance-to-coast")), Is.True);
            Assert.That(errors.FindAll(error => error.Contains("TerrainType")).Count, Is.EqualTo(2));
            species.ConfigureEnvironment(VegetationForm.CanopyTree, 0f, float.MaxValue);
            Assert.That(species.AllowsTerrain(TerrainType.Forest), Is.True);
            Assert.That(species.AllowsTerrain(TerrainType.Ridge), Is.True);
            Assert.That(species.AllowsTerrain(TerrainType.Water), Is.False);
            Assert.That(species.Validate(), Is.Empty);
        }

        [Test]
        public void HabitatCatalogReportsDuplicateAndNullProfiles()
        {
            var profile = Track(ScriptableObject.CreateInstance<HabitatVegetationProfileAsset>());
            profile.Configure("coast", .45f, null);
            var catalog = Track(ScriptableObject.CreateInstance<HabitatVegetationCatalogAsset>());
            catalog.SetProfiles(new[] { profile, profile, null });
            List<string> errors = catalog.Validate(null);
            Assert.That(errors.Exists(error => error.Contains("Duplicate")), Is.True);
            Assert.That(errors.Exists(error => error.Contains("null")), Is.True);
            Assert.That(catalog.GetProfile(" COAST "), Is.SameAs(profile));
            Assert.That(catalog.GetProfile("water"), Is.Null);
        }

        private static void AssertProfileDensity(string habitatId, float expected)
        {
            HabitatVegetationProfileAsset profile = AssetDatabase.LoadAssetAtPath<HabitatVegetationProfileAsset>(
                "Assets/_Project/Data/Vegetation/Habitats/" + habitatId + "_vegetation.asset");
            Assert.That(profile, Is.Not.Null, "Missing profile for " + habitatId);
            Assert.That(profile.OverallDensity, Is.EqualTo(expected).Within(0.0001f), habitatId + " density was not applied");
        }

        private static void AssertProfileWeights(string habitatId, params (string speciesId, float weight)[] expected)
        {
            HabitatVegetationProfileAsset profile = AssetDatabase.LoadAssetAtPath<HabitatVegetationProfileAsset>(
                "Assets/_Project/Data/Vegetation/Habitats/" + habitatId + "_vegetation.asset");
            foreach ((string speciesId, float weight) in expected)
            {
                bool found = false;
                foreach (HabitatVegetationSpeciesEntry entry in profile.Species)
                    if (entry.Species.SpeciesId == speciesId)
                    {
                        Assert.That(entry.Weight, Is.EqualTo(weight).Within(0.0001f), habitatId + "/" + speciesId);
                        found = true;
                        break;
                    }
                Assert.That(found, Is.True, habitatId + " missing species " + speciesId);
            }
            Assert.That(profile.Species.Count, Is.EqualTo(expected.Length), habitatId + " has unexpected species entries");
        }

        private VegetationSpeciesAsset CreateSpecies(string id, GameObject visual, string[] biomes, bool harvestable, string resourceKind)
        {
            VegetationSpeciesAsset asset = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            asset.Configure(id, id, visual, VegetationCategory.Tree, 0.7f, 1.3f, 2f, 0f, 35f, 0.1f, 0.9f, 0.2f, 0.8f, biomes, true, harvestable, resourceKind, null, VegetationCollisionMode.VisualPrefab);
            return asset;
        }

        private T Track<T>(T item) where T : Object { created.Add(item); return item; }

        private GameObject CreateTemporaryPrefab(string path)
        {
            var source = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            temporaryAssetPaths.Add(path);
            Assert.That(prefab, Is.Not.Null);
            return prefab;
        }
    }
}
