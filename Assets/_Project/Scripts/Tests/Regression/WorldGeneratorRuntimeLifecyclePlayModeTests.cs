using System.Collections;
using System.Linq;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Generation;
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
                        "SpawnResources", "GenerateLandmarks", "SpawnPlayer", "ConfigureCamera",
                        "BuildNavMesh", "SpawnCreatures", "FinalizeGeneration"
                    },
                    generator.LastGenerationStageOrder.ToArray(),
                    "The production generation pipeline is not running in the documented stage order.");

                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "Player"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "Main Camera"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "TerrainRoot"));
                Assert.AreEqual(1, CountNamed(first.GenerationRoot, "ResourceRoot"));
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

        private static int CountNamed(Transform root, string name)
        {
            return root == null
                ? 0
                : root.GetComponentsInChildren<Transform>(true).Count(transform => transform.name == name);
        }
    }
}
