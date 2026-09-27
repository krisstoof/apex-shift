using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
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
                        "PrepareGeneration", "CreateWorldRoots", "GenerateTerrainAndBiomes",
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
                Assert.That(firstResultCount, Is.EqualTo(firstInstances.Length));
                int categoryTotal = 0;
                foreach (string biomeId in new[] { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" })
                    categoryTotal += first.Result.GetVegetationCount(biomeId, VegetationCategory.Tree);
                Assert.That(categoryTotal, Is.EqualTo(firstResultCount), "Per-biome/category counts should reconcile with total and existing species counts.");
                foreach (VegetationInstanceRuntime instance in firstInstances)
                {
                    Assert.That(instance.transform.parent.parent.name, Is.EqualTo($"Chunk_{instance.ChunkX}_{instance.ChunkZ}"));
                    Assert.That(first.Result.GetVegetationCount(instance.BiomeId, instance.SpeciesId), Is.GreaterThan(0));
                }
                Transform firstVegetationRoot = first.VegetationRoot;

                generator.ClearGeneratedWorld();
                yield return null;
                Assert.IsTrue(firstVegetationRoot == null, "ClearGeneratedWorld left the previous VegetationRoot alive.");

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
                Assert.That(second.VegetationRoot.childCount, Is.GreaterThan(0), "Chunk roots should be created only for actual placements.");
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
