using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Editor.World;
using ApexShift.EditorTools.Validation;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ApexShift.Tests.Editor
{
    public sealed class TropicalSpeedTreeIntegrationTests
    {
        [Test]
        public void Manifest_HasCompleteDistinctNativeExportPlanAndSeparateHero()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            Assert.That(plan.Validate(), Is.Empty);
            Assert.That(plan.assets.Select(a => a.ModelPath).Distinct().Count(), Is.EqualTo(26));
            Assert.That(plan.assets.Count(a => !a.hero), Is.EqualTo(25));
            Assert.That(plan.assets.Count(a => a.IsTree && !a.hero), Is.EqualTo(11));
            var hero = plan.assets.Single(a => a.hero);
            Assert.That(hero.modelName, Is.EqualTo("ST_OldTree_Hero_Base_A"));
            Assert.That(hero.speciesId, Is.Empty);
            Assert.That(plan.profiles.SelectMany(p => p.entries).Any(e => e.speciesId == "tree_conifer_01"), Is.False);
            foreach (var asset in plan.assets)
            {
                Assert.That(asset.ModelPath, Does.EndWith(".st9"));
                Assert.That(asset.WrapperPath, Does.StartWith(TropicalSpeedTreeManifest.WrapperRoot));
            }
        }

        [TestCase("coast", 11, .45f)]
        [TestCase("lowland_jungle", 21, 1.55f)]
        [TestCase("jungle_interior", 19, 1.85f)]
        [TestCase("wet_jungle", 19, 2.10f)]
        [TestCase("rocky_upland", 8, .45f)]
        public void PlannedProfiles_HaveDistinctEligibleSpeciesAndUnchangedDensity(string habitat, int count, float density)
        {
            var plan = TropicalSpeedTreeManifest.Load();
            var profile = plan.profiles.Single(p => p.habitatId == habitat);
            Assert.That(profile.entries.Length, Is.EqualTo(count));
            Assert.That(profile.density, Is.EqualTo(density));
            foreach (var entry in profile.entries)
            {
                var species = plan.assets.Single(a => a.speciesId == entry.speciesId);
                Assert.That(species.allowedHabitats, Does.Contain(habitat));
                Assert.That(entry.weight, Is.GreaterThan(0));
            }
            var ids = profile.entries.Select(e => e.speciesId).ToArray();
            if (habitat == "coast")
            {
                Assert.That(ids, Does.Contain("palm_curved_01"));
                Assert.That(ids.Count(id => id.StartsWith("palm_")), Is.EqualTo(3));
                Assert.That(ids.Any(id => id.StartsWith("tree_wet") || id == "tree_upland_01"), Is.False);
            }
            if (habitat == "lowland_jungle")
                Assert.That(ids, Is.SupersetOf(new[] { "tree_leafy_01", "tree_broadleaf_02", "tree_broadleaf_03", "sapling_tropical_01", "sapling_tropical_02" }));
            if (habitat == "jungle_interior" || habitat == "wet_jungle")
                Assert.That(ids, Is.SupersetOf(new[] { "fern_large_01", "fern_large_02", "fern_small_01", "fern_small_02", "vine_ground_01" }));
            if (habitat == "wet_jungle")
                Assert.That(ids, Is.SupersetOf(new[] { "tree_wet_01", "tree_wet_02" }));
            if (habitat == "rocky_upland")
            {
                Assert.That(ids, Does.Contain("tree_upland_01"));
                Assert.That(ids.Any(id => id.StartsWith("palm") || id.StartsWith("tree_wet") || id.StartsWith("fern")), Is.False);
            }
        }

        [Test]
        public void Manifest_RejectsWaterDuplicateAndDisallowedMembership()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            plan.profiles[0].entries[0].speciesId = "tree_upland_01";
            Assert.That(plan.Validate().Any(e => e.Contains("disallowed profile membership")), Is.True);
            plan.profiles[0].habitatId = "water";
            Assert.That(plan.Validate().Any(e => e.Contains("habitat profile")), Is.True);
            plan.assets[1].speciesId = plan.assets[0].speciesId;
            Assert.That(plan.Validate().Any(e => e.Contains("duplicate species")), Is.True);
        }

        [Test]
        public void Manifest_DecorativeFormsAndTreeResourceKindsMatchExistingRuntime()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            foreach (var entry in plan.assets.Where(a => !a.hero))
            {
                if (entry.IsTree)
                {
                    Assert.That(entry.harvestable, Is.True);
                    Assert.That(entry.resourceKind, Is.EqualTo(entry.category == "DeadTree" ? "dry_tree" : "leafy_tree"));
                }
                else if (entry.speciesId != "shrub_forest_01")
                    Assert.That(entry.harvestable, Is.False);
                if (entry.speciesId.StartsWith("sapling")) Assert.That(entry.Form, Is.EqualTo(VegetationForm.UnderstoryTree));
                if (entry.speciesId.StartsWith("fern")) Assert.That(entry.Form, Is.EqualTo(VegetationForm.Fern));
                if (entry.speciesId.StartsWith("broadleaf_understory")) Assert.That(entry.Form, Is.EqualTo(VegetationForm.BroadleafUnderstory));
                if (entry.speciesId == "vine_ground_01") Assert.That(entry.Form, Is.EqualTo(VegetationForm.Vine));
                if (entry.speciesId == "tree_upland_01")
                {
                    Assert.That(entry.minElevation, Is.EqualTo(.4f));
                    Assert.That(entry.maxMoisture, Is.EqualTo(.7f));
                    Assert.That(entry.minCoast, Is.EqualTo(20f));
                }
            }
            var settings = new VegetationGenerationSettings();
            Assert.That(settings.TreeBaseDensity, Is.EqualTo(.016f));
            Assert.That(settings.DeadTreeBaseDensity, Is.EqualTo(.0025f));
            Assert.That(settings.ShrubBaseDensity, Is.EqualTo(.030f));
            Assert.That(settings.GroundCoverBaseDensity, Is.EqualTo(.075f));
        }

        [Test]
        public void Readiness_ReportsEveryMissingExportAndWrapperWithoutFailingLegacyProductionValidation()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            var errors = TropicalSpeedTreeValidator.CollectReadinessProblems(plan);
            foreach (var entry in plan.assets)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.ModelPath) == null)
                    Assert.That(errors.Any(e => e.Contains(entry.ModelPath)), Is.True, entry.ModelPath);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.WrapperPath) == null)
                    Assert.That(errors.Any(e => e.Contains(entry.WrapperPath)), Is.True, entry.WrapperPath);
            }
            Assert.That(VegetationDataValidator.CollectProblems(), Is.Empty);
            if (TropicalSpeedTreeVegetationBinder.IsActivated) Assert.That(errors, Is.Empty);
        }

        [Test]
        public void IncompleteBind_DoesNotMutateOrPartiallyActivateProduction()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            var before = ProductionSnapshot();
            if (TropicalSpeedTreeValidator.CollectReadinessProblems(plan).Count != 0)
            {
                var error = Assert.Throws<InvalidOperationException>(() => TropicalSpeedTreeVegetationBinder.Bind());
                Assert.That(error.Message, Does.Contain("production data unchanged"));
                CollectionAssert.AreEquivalent(before, ProductionSnapshot());
            }
            else
            {
                TropicalSpeedTreeVegetationBinder.RequireReady(plan);
                CollectionAssert.AreEquivalent(before, ProductionSnapshot());
            }
        }

        [Test]
        public void FailedDataTransaction_RestoresReferencesCatalogLookupAndRemovesNewAssets()
        {
            const string root = "Assets/_Project/Data/Vegetation/";
            var catalog = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>(root + "VegetationCatalog.asset");
            var leafy = catalog.GetSpecies("tree_leafy_01");
            string before = EditorJsonUtility.ToJson(catalog);
            const string temporaryPath = root + "__SpeedTree_Rollback_Test.asset";
            Assert.Throws<InvalidOperationException>(() => TropicalSpeedTreeVegetationBinder.RunDataTransaction(() =>
            {
                catalog.SetSpecies(Array.Empty<VegetationSpeciesAsset>());
                Assert.That(catalog.GetSpecies("tree_leafy_01"), Is.Null);
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<VegetationSpeciesAsset>(), temporaryPath);
                throw new InvalidOperationException("injected validation failure");
            }));
            Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(before));
            Assert.That(catalog.GetSpecies("tree_leafy_01"), Is.SameAs(leafy));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(temporaryPath), Is.Null);
        }

        [Test]
        public void LegacyFbxVisual_IsRejectedAsSpeedTreeEvenWithThePlannedSpeciesId()
        {
            var entry = TropicalSpeedTreeManifest.Load().assets.Single(a => a.speciesId == "tree_leafy_01");
            var legacy = AssetDatabase.LoadAssetAtPath<GameObject>(TropicalSpeedTreePrefabBuilder.GameplayRoot + "ES_LeafyTree.prefab");
            var errors = new List<string>();
            TropicalSpeedTreeValidator.ValidateWrapper(entry, legacy, errors);
            Assert.That(errors.Any(e => e.Contains("authentic native .st9")), Is.True);
        }

        [TestCase("ES_BerryBush.prefab", "berry_bush", "berries", 8f)]
        [TestCase("ES_GrassPatch.prefab", "grass_patch", "grass", 5f)]
        public void WrapperGameplayCopy_PreservesActualTemplateContractsWithoutCopyingLegacyVisuals(string template, string kind, string item, float biomass)
        {
            var root = new GameObject("GameplayContractFixture");
            GameObject fresh = null;
            try
            {
                TropicalSpeedTreePrefabBuilder.CopyGameplayContract(root, template);
                var errors = new List<string>();
                TropicalSpeedTreeValidator.ValidateGameplayContract(root, template, errors);
                Assert.That(errors, Is.Empty);
                Assert.That(root.GetComponentsInChildren<Renderer>(true), Is.Empty);
                // Reset initializes a transient default state when an editor component is added.
                // Runtime instances rebuild that nonserialized state from the copied prefab data.
                fresh = Object.Instantiate(root);
                var node = fresh.GetComponent<ResourceNodeView>();
                Assert.That(node.State.ResourceId, Is.EqualTo(kind));
                Assert.That(node.State.ItemId, Is.EqualTo(item));
                Assert.That(fresh.GetComponent<FoodSourceView>().Biomass, Is.EqualTo(biomass));
                var settings = new SerializedObject(root.GetComponent<ResourceNodeView>());
                Assert.That(settings.FindProperty("interactionRadius").floatValue,
                    Is.EqualTo(template == "ES_BerryBush.prefab" ? .45f : .3375f).Within(.001f));
                Assert.That(root.GetComponentsInChildren<Collider>(true).All(c => c.isTrigger), Is.True);
                Assert.Throws<InvalidOperationException>(() => TropicalSpeedTreePrefabBuilder.CopyGameplayContract(root, template));
            }
            finally { if (fresh != null) Object.DestroyImmediate(fresh); Object.DestroyImmediate(root); }
        }

        [Test]
        public void ActivatedCatalog_MatchesPlannedMembershipAndAuthenticSourcesOrKeepsCompleteLegacySet()
        {
            var plan = TropicalSpeedTreeManifest.Load();
            var catalog = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>("Assets/_Project/Data/Vegetation/VegetationCatalog.asset");
            var habitats = AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>("Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset");
            Assert.That(habitats.Profiles.Count, Is.EqualTo(5));
            Assert.That(habitats.GetProfile("water"), Is.Null);
            if (TropicalSpeedTreeVegetationBinder.IsActivated)
            {
                Assert.That(catalog.Species.Count, Is.EqualTo(26));
                foreach (var definition in plan.profiles)
                {
                    var actual = habitats.GetProfile(definition.habitatId);
                    CollectionAssert.AreEquivalent(definition.entries.Select(e => e.speciesId), actual.Species.Select(e => e.Species.SpeciesId));
                    foreach (var weight in definition.entries)
                        Assert.That(actual.Species.Single(e => e.Species.SpeciesId == weight.speciesId).Weight, Is.EqualTo(weight.weight));
                }
                Assert.That(TropicalSpeedTreeValidator.CollectReadinessProblems(plan), Is.Empty);
            }
            else
            {
                CollectionAssert.AreEquivalent(new[] { "tree_leafy_01", "tree_conifer_01", "tree_dead_01", "shrub_forest_01", "groundcover_forest_01" }, catalog.Species.Select(a => a.SpeciesId));
                foreach (var habitat in habitats.Profiles)
                    foreach (var entry in habitat.Species) Assert.That(entry.Species.VisualPrefab, Is.Not.Null);
            }
        }

        private static Dictionary<string, string> ProductionSnapshot()
        {
            var paths = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Project/Data/Vegetation" })
                .Select(AssetDatabase.GUIDToAssetPath).Append("Assets/_Project/Data/Biomes/BiomeCatalog.asset");
            return paths.ToDictionary(p => p, p => AssetDatabase.AssetPathToGUID(p) + "\n"
                + EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(p)) + "\n" + Convert.ToBase64String(File.ReadAllBytes(p)));
        }
    }
}
