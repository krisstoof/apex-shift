using System.Collections.Generic;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class VegetationDataAssetTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created) if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void Species_NormalizesIdAndPreservesConstraints()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset species = CreateSpecies(" Leafy Tree-01 ", visual, new[] { "westwood" }, false, string.Empty);
            Assert.That(species.SpeciesId, Is.EqualTo("leafy_tree_01"));
            Assert.That(species.MinScale, Is.EqualTo(0.7f));
            Assert.That(species.MaxSlopeDegrees, Is.EqualTo(35f));
            Assert.That(species.AllowsBiome("westwood"), Is.True);
            Assert.That(species.Validate(), Is.Empty);
        }

        [Test]
        public void Catalog_LookupIsByNormalizedStableId_AndReportsDuplicatesAndNulls()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset first = CreateSpecies("tree_a", visual, new[] { "westwood" }, false, "");
            VegetationSpeciesAsset duplicate = CreateSpecies("tree_a", visual, new[] { "westwood" }, false, "");
            VegetationCatalogAsset catalog = Track(ScriptableObject.CreateInstance<VegetationCatalogAsset>());
            catalog.SetSpecies(new[] { first, duplicate, null });
            Assert.That(catalog.TryGetSpecies(" Tree-A ", out VegetationSpeciesAsset found), Is.True);
            Assert.That(found, Is.SameAs(first));
            Assert.That(catalog.GetSpecies("unknown"), Is.Null);
            List<string> errors = catalog.Validate(null);
            Assert.That(errors.Exists(e => e.Contains("duplicate species ID")), Is.True);
            Assert.That(errors.Exists(e => e.Contains("null species asset")), Is.True);
        }

        [Test]
        public void Profile_RejectsDuplicateMissingAndDisallowedSpecies()
        {
            GameObject visual = Track(new GameObject("visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_allowed_westwood", visual, new[] { "westwood" }, false, "");
            VegetationCatalogAsset catalog = Track(ScriptableObject.CreateInstance<VegetationCatalogAsset>());
            catalog.SetSpecies(new[] { species });
            BiomeVegetationProfileAsset profile = Track(ScriptableObject.CreateInstance<BiomeVegetationProfileAsset>());
            profile.Configure("south_thicket", 1f, new[]
            {
                new BiomeVegetationSpeciesEntry(species, 1f, 1f),
                new BiomeVegetationSpeciesEntry(species, 2f, 1f)
            });
            List<string> errors = profile.Validate(new HashSet<string> { "westwood", "south_thicket" }, catalog);
            Assert.That(errors.Exists(e => e.Contains("repeats species")), Is.True);
            Assert.That(errors.Exists(e => e.Contains("not allowed")), Is.True);
        }

        [Test]
        public void SameVisualCanBeDecorativeOrHarvestable_AndMissingPrefabIsReported()
        {
            GameObject visual = Track(new GameObject("shared visual"));
            VegetationSpeciesAsset decorative = CreateSpecies("tree_decorative", visual, new[] { "westwood" }, false, "");
            VegetationSpeciesAsset harvestable = CreateSpecies("tree_harvestable", visual, new[] { "westwood" }, true, "wood");
            VegetationSpeciesAsset missing = CreateSpecies("tree_missing_visual", null, new[] { "westwood" }, false, "");
            Assert.That(decorative.Validate(), Is.Empty);
            Assert.That(harvestable.Validate(), Is.Empty);
            Assert.That(missing.Validate().Exists(e => e.Contains("no visual prefab")), Is.True);
        }

        private VegetationSpeciesAsset CreateSpecies(string id, GameObject visual, string[] biomes, bool harvestable, string resourceKind)
        {
            VegetationSpeciesAsset asset = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            asset.Configure(id, id, visual, VegetationCategory.Tree, 0.7f, 1.3f, 2f, 0f, 35f, 0.1f, 0.9f, 0.2f, 0.8f, biomes, true, harvestable, resourceKind, null, VegetationCollisionMode.VisualPrefab);
            return asset;
        }

        private T Track<T>(T item) where T : Object { created.Add(item); return item; }
    }
}
