using System.Reflection;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class WorldGeneratorBiomeSpawnTests
    {
        [Test]
        public void ResourceAndCreatureSpawningRejectCandidatesOutsideDenseRegionBiome()
        {
            GameObject topographyObject = new GameObject("SpawnBiomeTopography");
            GameObject generatorObject = new GameObject("SpawnBiomeGenerator");
            BiomeDefinitionAsset westwood = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
            IslandTopographyRuntime topography = topographyObject.AddComponent<IslandTopographyRuntime>();
            WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
            PropertyInfo creatureBoundsActive = typeof(CreatureIslandBoundsRuntime).GetProperty("Active",
                BindingFlags.Static | BindingFlags.Public);
            object previousCreatureBounds = creatureBoundsActive.GetValue(null);
            try
            {
                creatureBoundsActive.GetSetMethod(true).Invoke(null, new object[] { null });
                westwood.Configure("westwood", "Westwood", Color.green, false, null, null);
                topography.Build(16, 4f, (x, z) => true, _ => 0f, _ => "westwood",
                    biomeField: new BiomeFieldGenerator(89, new BiomeFieldSettings()),
                    biomeClassifier: new BiomeClassifier(89, new BiomeFieldSettings()));

                typeof(WorldGeneratorRuntime).GetField("_islandTopography", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(generator, topography);

                Vector3 candidate = new Vector3(24f, 0f, 24f);
                SetDenseBiome(topography, candidate, "south_thicket");
                var region = new GeneratedBiomeRegion(westwood, new Bounds(candidate, new Vector3(20f, 2f, 20f)));

                Assert.That(InvokeResourceBiomeGuard(generator, candidate, region), Is.False,
                    "A resource candidate whose dense biome is south_thicket must not spawn from westwood.");
                Assert.That(InvokeCreatureBiomeGuard(generator, candidate, "westwood"), Is.False,
                    "A creature candidate whose dense biome is south_thicket must not spawn from westwood.");
                Assert.That(TryGetSafeCreatureSpawnPoint(generator, candidate, "westwood"), Is.False,
                    "The creature spawn-point selector must reject the mismatched dense biome.");

                SetDenseBiome(topography, candidate, "westwood");
                Assert.That(InvokeResourceBiomeGuard(generator, candidate, region), Is.True,
                    "A resource candidate in the region's dense westwood biome should be accepted.");
                Assert.That(InvokeCreatureBiomeGuard(generator, candidate, "westwood"), Is.True,
                    "A creature candidate in the expected dense westwood biome should be accepted.");
                Assert.That(TryGetSafeCreatureSpawnPoint(generator, candidate, "westwood"), Is.True,
                    "The creature spawn-point selector should accept a safe candidate in westwood.");
            }
            finally
            {
                creatureBoundsActive.GetSetMethod(true).Invoke(null, new[] { previousCreatureBounds });
                Object.DestroyImmediate(generatorObject);
                Object.DestroyImmediate(topographyObject);
                Object.DestroyImmediate(westwood);
            }
        }

        private static bool InvokeResourceBiomeGuard(WorldGeneratorRuntime generator, Vector3 position,
            GeneratedBiomeRegion region)
        {
            MethodInfo method = typeof(WorldGeneratorRuntime).GetMethod("IsResourceCandidateInRegionBiome",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(generator, new object[] { position, region });
        }

        private static bool InvokeCreatureBiomeGuard(WorldGeneratorRuntime generator, Vector3 position,
            string expectedBiomeId)
        {
            MethodInfo method = typeof(WorldGeneratorRuntime).GetMethod("IsCreatureBiomeCandidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(generator, new object[] { position, expectedBiomeId });
        }

        private static bool TryGetSafeCreatureSpawnPoint(WorldGeneratorRuntime generator, Vector3 position,
            string expectedBiomeId)
        {
            MethodInfo method = typeof(WorldGeneratorRuntime).GetMethod("TryGetSafeCreatureSpawnPoint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] arguments =
            {
                new Bounds(position, Vector3.zero), "small_prey", expectedBiomeId, default(Vector3)
            };
            return (bool)method.Invoke(generator, arguments);
        }

        private static void SetDenseBiome(IslandTopographyRuntime topography, Vector3 position, string biomeId)
        {
            FieldInfo mapField = typeof(IslandTopographyRuntime).GetField("_biomeMap",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var map = (string[,])mapField.GetValue(topography);
            FieldInfo originXField = typeof(IslandTopographyRuntime).GetField("_originX",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo originZField = typeof(IslandTopographyRuntime).GetField("_originZ",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float originX = (float)originXField.GetValue(topography);
            float originZ = (float)originZField.GetValue(topography);
            float cellSize = topography.DenseBiomeMapCellSize;
            int x = Mathf.FloorToInt((position.x - originX) / cellSize);
            int z = Mathf.FloorToInt((position.z - originZ) / cellSize);
            map[x, z] = biomeId;
        }
    }
}
