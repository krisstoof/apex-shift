using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class WorldGeneratorRuntimeLifecyclePlayModeTests
    {
        // Lifecycle regression coverage for serialized world ownership.
        [UnityTest]
        public IEnumerator GenerateClearGenerate_UsesOneOwnedRuntimeAndPreservesUnrelatedRoots()
        {
            GameObject generatorObject = new GameObject("Lifecycle_WorldGenerator");
            GameObject unrelatedPlayer = new GameObject("Player");
            GameObject unrelatedTerrain = new GameObject("TerrainRoot");
            GameObject unrelatedCamera = new GameObject("Main Camera");
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetSeed(81281);

            try
            {
                generator.Generate();
                yield return null;

                WorldGenerationContext first = generator.CurrentGeneration;
                Assert.NotNull(first, "The first generation did not create a context.");
                Assert.AreEqual(
                    new[]
                    {
                        "PrepareGeneration", "CreateWorldRoots", "GenerateTerrainAndEnvironment",
                        "SpawnResources", "GenerateLandmarks", "GenerateVegetation", "SpawnPlayer", "ConfigureCamera",
                        "BuildNavMesh", "SpawnCreatures", "FinalizeGeneration"
                    },
                    generator.LastGenerationStageOrder.ToArray(),
                    "The production generation pipeline is not running in the documented stage order.");

                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "Player"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "Main Camera"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "TerrainRoot"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "ResourceRoot"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "VegetationRoot"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "CreatureRoot"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "EcosystemRuntime"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "DayNightRuntime"));
                foreach (CreatureAgentView creature in first.GenerationRoot.GetComponentsInChildren<CreatureAgentView>(true))
                {
                    CreatureHitboxRuntime hitbox = creature.GetComponent<CreatureHitboxRuntime>();
                    Assert.IsNotNull(hitbox, $"Generated creature {creature.CreatureId} has no CreatureHitboxRuntime.");
                    Assert.IsNotNull(hitbox.CombatCollider, $"Generated creature {creature.CreatureId} has no combat collider.");
                    Assert.IsTrue(hitbox.CombatCollider.enabled);
                    Assert.IsTrue(hitbox.CombatCollider.isTrigger);
                    Assert.IsTrue(hitbox.IsValidForMask(Physics.DefaultRaycastLayers, out string reason), reason);
                }

                Transform firstGenerationRoot = first.GenerationRoot;
                generator.ClearGeneratedWorld();
                yield return null;
                Assert.IsTrue(firstGenerationRoot == null, "The first generation root survived ClearGeneratedWorld().");
                Assert.IsNotNull(unrelatedPlayer, "ClearGeneratedWorld destroyed an unrelated Player root.");
                Assert.IsNotNull(unrelatedTerrain, "ClearGeneratedWorld destroyed an unrelated TerrainRoot.");
                Assert.IsNotNull(unrelatedCamera, "ClearGeneratedWorld destroyed an unrelated Main Camera root.");

                generator.Generate();
                yield return null;
                WorldGenerationContext second = generator.CurrentGeneration;
                Assert.NotNull(second, "The second generation did not create a context.");
                Assert.AreNotSame(firstGenerationRoot, second.GenerationRoot);
                Assert.AreEqual(1, CountNamed(second.GenerationRoot, "Player"));
                Assert.AreEqual(1, CountNamed(second.GenerationRoot, "Main Camera"));
            Assert.AreEqual(1, CountNamed(second.GenerationRoot, "EcosystemRuntime"));
            Assert.AreEqual(1, CountNamed(second.GenerationRoot, "DayNightRuntime"));
            }
            finally
            {
                if (generator != null) generator.ClearGeneratedWorld();
                Object.Destroy(generatorObject);
                Object.Destroy(unrelatedPlayer);
                Object.Destroy(unrelatedTerrain);
                Object.Destroy(unrelatedCamera);
            }
        }

        [UnityTest]
        public IEnumerator GenerateClearGenerate_ReplacesVegetationRootAndPreservesPlacementMetadata()
        {
            GameObject generatorObject = new GameObject("Vegetation_Lifecycle_WorldGenerator");
            GameObject visual = new GameObject("test_visual_prefab");
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetSeed(91284);
            BiomeCatalogAsset catalog = BuildBiomeCatalog(visual);
            generator.SetBiomeCatalog(catalog);

            try
            {
                generator.Generate();
                yield return null;
                WorldGenerationContext first = generator.CurrentGeneration;
                Assert.NotNull(first);
                Assert.NotNull(first.VegetationRoot);
                VegetationInstanceRuntime[] firstInstances = first.VegetationRoot.GetComponentsInChildren<VegetationInstanceRuntime>(true);
                Assert.That(firstInstances.Length, Is.GreaterThan(0), "Test catalog should generate vegetation using its temporary visual.");
                string[] firstMetadata = firstInstances.Select(ToMetadata).OrderBy(value => value).ToArray();
                int firstResultCount = first.Result.VegetationInstanceCount;
                Assert.IsNotNull(first.Result.Report, "Completed generation should publish its deterministic QA report.");
                string firstReport = first.Result.Report.ToDeterministicString();
                StringAssert.Contains(";habitats=", firstReport);
                StringAssert.Contains(";habitatPercent=", firstReport);
                StringAssert.DoesNotContain(";biomes=", firstReport);
                CultureInfo originalCulture = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                    Assert.AreEqual(firstReport, first.Result.Report.ToDeterministicString(), "Report serialization must use invariant culture.");
                }
                finally { CultureInfo.CurrentCulture = originalCulture; }
                Assert.AreEqual(first.Result.Report.LandCells, first.Result.Report.HabitatCells.Values.Sum(), "Habitat samples must reconcile to land cells.");
                Assert.AreEqual(first.Result.Report.LandCells, first.Result.Report.SlopeBuckets.Sum());
                Assert.That(first.Result.Report.HabitatLandPercentages.Values.Sum(), Is.EqualTo(100f).Within(0.01f));
                Assert.AreEqual(first.Result.Report.VegetationPlacements, first.Result.Report.VegetationByBiomeSpecies.Values.Sum());
                Assert.AreEqual(first.Result.Report.VegetationPlacements, first.Result.Report.Harvestable + first.Result.Report.Decorative);
                Assert.AreEqual(first.Result.Report.VegetationPlacements, first.Result.Report.Trees + first.Result.Report.Shrubs + first.Result.Report.GroundCover);
                Assert.LessOrEqual(first.Result.Report.MinElevation, first.Result.Report.AverageElevation);
                Assert.LessOrEqual(first.Result.Report.AverageElevation, first.Result.Report.MaxElevation);
                Assert.That(first.Result.Report.Rejections.Water, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.ExcessiveSlope, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.BiomeMismatch, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.Elevation, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.Moisture, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.ShorelineOrClearing, Is.GreaterThanOrEqualTo(0));
                Assert.That(first.Result.Report.Rejections.SpacingOrCollision, Is.GreaterThanOrEqualTo(0));
                Assert.That(firstResultCount, Is.EqualTo(firstInstances.Length));
                int categoryTotal = 0;
                foreach (string biomeId in new[] { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" })
                    categoryTotal += first.Result.GetVegetationCount(biomeId, VegetationCategory.Tree);
                Assert.That(categoryTotal, Is.EqualTo(firstResultCount), "Per-biome/category counts should reconcile with total and existing species counts.");
                foreach (VegetationInstanceRuntime instance in firstInstances)
                {
                    Assert.That(instance.transform.parent.parent.name, Is.EqualTo($"Chunk_{instance.ChunkX}_{instance.ChunkZ}"));
                    Assert.That(first.Result.GetVegetationCount(instance.BiomeId, instance.SpeciesId), Is.GreaterThan(0));
                    Assert.IsFalse(first.IslandTopography.IsWaterAt(instance.transform.position.x, instance.transform.position.z), "Generated vegetation placement must not be in water.");
                    Assert.AreEqual(instance.BiomeId, first.IslandTopography.GetBiomeIdAt(instance.transform.position), "Placement metadata must match the authoritative dense biome map.");
                    TopographyCell cell = first.IslandTopography.GetCellAt(instance.transform.position);
                    VegetationSpeciesAsset species = FindSpecies(catalog, instance.SpeciesId);
                    Assert.IsNotNull(species);
                    Assert.That(cell.SlopeDegrees, Is.LessThanOrEqualTo(species.MaxSlopeDegrees + 0.05f), $"{instance.SpeciesId} exceeds its slope profile.");
                    if (species.Category == VegetationCategory.Tree || species.Category == VegetationCategory.DeadTree)
                    {
                        Assert.That(HorizontalDistance(instance.transform.position, first.Player.transform.position), Is.GreaterThanOrEqualTo(generator.StartClearingRadius - 0.05f), "Large trees must respect the configured player clearing.");
                        foreach (LandmarkRuntime landmark in LandmarkRegistry.Landmarks)
                        {
                            float radius = generator.GenerationSettings.Vegetation.LandmarkClearances.GetRadius(landmark.Type);
                            if (radius > 0f) Assert.That(HorizontalDistance(instance.transform.position, landmark.transform.position), Is.GreaterThanOrEqualTo(radius - 0.05f), $"Large tree violates {landmark.Type} clearing.");
                        }
                    }
                }
                Transform firstVegetationRoot = first.VegetationRoot;

                generator.ClearGeneratedWorld();
                yield return null;
                Assert.IsTrue(firstVegetationRoot == null, "ClearGeneratedWorld left the previous VegetationRoot alive.");
                Assert.IsNull(generator.GetLastResult(), "ClearGeneratedWorld must discard stale report/result state.");

                generator.Generate();
                yield return null;
                WorldGenerationContext second = generator.CurrentGeneration;
                Assert.NotNull(second);
                Assert.AreNotSame(firstVegetationRoot, second.VegetationRoot);
                VegetationInstanceRuntime[] secondInstances = second.VegetationRoot.GetComponentsInChildren<VegetationInstanceRuntime>(true);
                string[] secondMetadata = secondInstances.Select(ToMetadata).OrderBy(value => value).ToArray();
                Assert.That(secondInstances.Length, Is.EqualTo(firstInstances.Length), "Regeneration duplicated or lost vegetation instances.");
                CollectionAssert.AreEqual(firstMetadata, secondMetadata, "The same seed must reproduce placement metadata.");
                Assert.That(second.Result.VegetationInstanceCount, Is.EqualTo(firstResultCount));
                Assert.AreEqual(firstReport, second.Result.Report.ToDeterministicString(), "Same seed/settings must reproduce the full generation report and rejection counters.");
                Assert.That(second.VegetationRoot.childCount, Is.GreaterThan(0), "Chunk roots should be created only for actual placements.");

                generator.SetSeed(91285);
                generator.Generate();
                yield return null;
                WorldGenerationContext differentSeed = generator.CurrentGeneration;
                string[] differentSeedMetadata = differentSeed.VegetationRoot.GetComponentsInChildren<VegetationInstanceRuntime>(true)
                    .Select(ToMetadata).OrderBy(value => value).ToArray();
                Assert.AreNotEqual(firstReport, differentSeed.Result.Report.ToDeterministicString(), "The seed is part of the complete deterministic report.");
                CollectionAssert.AreNotEqual(firstMetadata, differentSeedMetadata, "Different seeds should change placement data without requiring every individual statistic to change.");
            }
            finally
            {
                if (generator != null) generator.ClearGeneratedWorld();
                Object.Destroy(generatorObject);
                Object.Destroy(visual);
                DestroyCatalog(catalog);
                LandmarkRegistry.ClearForTests();
            }
        }

        private static BiomeCatalogAsset BuildBiomeCatalog(GameObject visual)
        {
            string[] biomeIds = { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" };
            var allowed = new List<string>(biomeIds);
            var species = ScriptableObject.CreateInstance<VegetationSpeciesAsset>();
            species.Configure("lifecycle_test_tree", "Lifecycle Test Tree", visual, VegetationCategory.Tree,
                0.9f, 1.1f, 2.5f, 0f, 50f, 0f, 1f, 0f, 1f, allowed, true, true, "leafy_tree", null,
                VegetationCollisionMode.None);

            var biomes = new List<BiomeDefinitionAsset>();
            foreach (string biomeId in biomeIds)
            {
                var biome = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
                biome.Configure(biomeId, biomeId, Color.green, biomeId == "hearth_meadow", new List<VegetationSpawnEntryAsset>());
                var profile = ScriptableObject.CreateInstance<BiomeVegetationProfileAsset>();
                profile.Configure(biomeId, 1f, new[] { new BiomeVegetationSpeciesEntry(species, 1f, 1f) });
                biome.SetVegetationProfile(profile);
                biomes.Add(biome);
            }
            var water = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
            water.Configure("water", "Water", Color.blue, false, new List<VegetationSpawnEntryAsset>());
            biomes.Add(water);

            var catalog = ScriptableObject.CreateInstance<BiomeCatalogAsset>();
            catalog.SetBiomes(biomes);
            // The test owns all transient profile/biome/species objects via a hide-and-destroy helper list.
            TransientVegetationTestAssets.Register(catalog, species, biomes);
            return catalog;
        }

        private static string ToMetadata(VegetationInstanceRuntime instance)
        {
            return $"{instance.InstanceId}|{instance.SpeciesId}|{instance.BiomeId}|{instance.ChunkX}|{instance.ChunkZ}|{instance.transform.position.x:R}|{instance.transform.position.y:R}|{instance.transform.position.z:R}";
        }

        private static VegetationSpeciesAsset FindSpecies(BiomeCatalogAsset catalog, string id)
        {
            foreach (BiomeDefinitionAsset biome in catalog.Biomes)
            {
                if (biome == null || biome.VegetationProfile == null || biome.VegetationProfile.Species == null) continue;
                foreach (BiomeVegetationSpeciesEntry entry in biome.VegetationProfile.Species)
                    if (entry.Species != null && entry.Species.SpeciesId == id) return entry.Species;
            }
            return null;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        { float x = a.x - b.x, z = a.z - b.z; return Mathf.Sqrt(x * x + z * z); }

        private static void DestroyCatalog(BiomeCatalogAsset catalog)
        {
            TransientVegetationTestAssets.Destroy(catalog);
        }

        private static class TransientVegetationTestAssets
        {
            private static readonly List<Object> Assets = new List<Object>();
            public static void Register(BiomeCatalogAsset catalog, VegetationSpeciesAsset species, List<BiomeDefinitionAsset> biomes)
            {
                Assets.Add(catalog);
                Assets.Add(species);
                foreach (BiomeDefinitionAsset biome in biomes)
                {
                    Assets.Add(biome);
                    if (biome.VegetationProfile != null) Assets.Add(biome.VegetationProfile);
                }
            }
            public static void Destroy(BiomeCatalogAsset catalog)
            {
                for (int i = Assets.Count - 1; i >= 0; i--)
                    if (Assets[i] != null) Object.Destroy(Assets[i]);
                Assets.Clear();
            }
        }

        private static int CountNamed(Transform root, string name)
        {
            return root == null
                ? 0
                : root.GetComponentsInChildren<Transform>(true).Count(transform => transform.name == name);
        }
    }
}
