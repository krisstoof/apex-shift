using NUnit.Framework;
using UnityEngine;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Tests.Editor
{
    public sealed class TerrainHeightfieldAndTopographyTests
    {
        [Test]
        public void Heightfield_IsDeterministicAndIndependentOfGlobalRandomState()
        {
            var first = new TerrainHeightfieldGenerator(12345, new TerrainHeightfieldSettings());
            Random.InitState(1);
            float firstSample = first.SampleHeight(12.5f, -8.25f);

            Random.InitState(987654);
            var second = new TerrainHeightfieldGenerator(12345, new TerrainHeightfieldSettings());
            float secondSample = second.SampleHeight(12.5f, -8.25f);

            Assert.That(secondSample, Is.EqualTo(firstSample).Within(0.000001f));
        }

        [Test]
        public void Heightfield_DifferentSeedsChangeSomeSamples()
        {
            var first = new TerrainHeightfieldGenerator(1, new TerrainHeightfieldSettings());
            var second = new TerrainHeightfieldGenerator(2, new TerrainHeightfieldSettings());
            int differences = 0;
            for (int i = 0; i < 16; i++)
            {
                float x = -32f + i * 4.7f;
                float z = 19f - i * 3.2f;
                if (Mathf.Abs(first.SampleHeight(x, z) - second.SampleHeight(x, z)) > 0.0001f)
                    differences++;
            }
            Assert.That(differences, Is.GreaterThan(0));
        }

        [TestCase(101)]
        [TestCase(202)]
        [TestCase(303)]
        public void Heightfield_DefaultScaleProducesMacroLandforms(int seed)
        {
            var generator = new TerrainHeightfieldGenerator(seed, new TerrainHeightfieldSettings());
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int z = -80; z <= 80; z += 8)
                for (int x = -104; x <= 104; x += 8)
                {
                    float height = generator.SampleHeight(x, z);
                    min = Mathf.Min(min, height);
                    max = Mathf.Max(max, height);
                }

            Assert.That(max - min, Is.GreaterThan(2f), $"Seed {seed} did not produce multi-unit macro relief.");
        }

        [Test]
        public void Topography_ConstantHeightHasZeroSlopeAndNormalizedElevation()
        {
            GameObject go = new GameObject("TopographyConstantTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(5, 1f, (x, z) => true, p => 0.25f, p => "hearth_meadow");

                for (int z = 0; z < topography.GridSize; z++)
                    for (int x = 0; x < topography.GridSize; x++)
                    {
                        TopographyCell cell = topography.GetCell(x, z);
                        Assert.That(cell.SlopeDegrees, Is.EqualTo(0f).Within(0.001f));
                        Assert.That(cell.NormalizedElevation, Is.EqualTo(0f).Within(0.001f));
                    }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Topography_SlopeUsesHeightGradientAndRejectsSteepPlayerSpawn()
        {
            GameObject go = new GameObject("TopographySlopeTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(5, 1f, (x, z) => true, p => p.x, p => "hearth_meadow");

                TopographyCell center = topography.GetCell(2, 2);
                Assert.That(center.SlopeDegrees, Is.EqualTo(45f).Within(0.5f));
                Assert.That(center.IsSafeForPlayerSpawn, Is.False);
                Assert.That(topography.GetCell(0, 2).NormalizedElevation, Is.EqualTo(0f).Within(0.001f));
                Assert.That(topography.GetCell(4, 2).NormalizedElevation, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Topography_UsesSameAuthoritativeSurfaceHeightAsTerrainSampler()
        {
            var generator = new TerrainHeightfieldGenerator(2468, new TerrainHeightfieldSettings());
            System.Func<float, float, bool> island = (x, z) => Mathf.Abs(x) <= 8f && Mathf.Abs(z) <= 8f;
            GameObject go = new GameObject("SurfaceContractTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(
                    4,
                    4f,
                    island,
                    p => generator.SampleSurfaceHeight(p.x, p.z, island),
                    p => "hearth_meadow");

                TopographyCell center = topography.GetCell(1, 1);
                float expected = generator.SampleSurfaceHeight(center.WorldCenter.x, center.WorldCenter.z, island);
                Assert.That(center.Height, Is.EqualTo(expected).Within(0.000001f));
                Assert.That(center.Height, Is.GreaterThanOrEqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Topography_ShorelineLandCellIsBeachUnlessItIsARidge()
        {
            GameObject go = new GameObject("TopographyBeachOverrideTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(4, 1f, (x, z) => x < 0f, p => 0f, p => "hearth_meadow");

                TopographyCell shore = topography.GetCell(1, 1);
                Assert.That(shore.IsShoreline, Is.True);
                Assert.That(shore.TerrainType, Is.EqualTo(TerrainType.Beach));
                Assert.That(shore.IsBeach, Is.True);

                topography.Build(4, 1f, (x, z) => x < 0f, p => 0f, p => "stoneback_ridge");
                TopographyCell ridgeShore = topography.GetCell(1, 1);
                Assert.That(ridgeShore.IsShoreline, Is.True);
                Assert.That(ridgeShore.TerrainType, Is.EqualTo(TerrainType.Ridge),
                    "The intentional steep-cliff/ridge shoreline rule should remain intact.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DenseBiomeMap_IsSharedByTopographyWorldQueryAndEcosystemQueries()
        {
            GameObject go = new GameObject("DenseBiomeMapIntegrationTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                EcosystemDirectorRuntime director = go.AddComponent<EcosystemDirectorRuntime>();
                WorldQueryRuntime worldQuery = go.AddComponent<WorldQueryRuntime>();
                var field = new BiomeFieldGenerator(347, new BiomeFieldSettings());
                var classifier = new BiomeClassifier(347, new BiomeFieldSettings());
                topography.Build(8, 4f, (x, z) => true, p => p.x * 0.01f, p => "hearth_meadow",
                    biomeField: field, biomeClassifier: classifier);
                typeof(IslandTopographyRuntime).GetProperty("Active",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .GetSetMethod(true).Invoke(null, new object[] { topography });
                director.InitializeFromRegions(null);

                Assert.That(topography.DenseBiomeMapResolutionPerTile, Is.EqualTo(12));
                Assert.That(topography.DenseBiomeMapCellSize, Is.EqualTo(4f / 12f).Within(0.00001f));
                var mapField = typeof(IslandTopographyRuntime).GetField("_biomeMap",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                object prebuiltMap = mapField.GetValue(topography);
                Assert.That(prebuiltMap, Is.Not.Null);

                Vector3[] positions =
                {
                    new Vector3(-11.3f, 0f, -7.1f),
                    new Vector3(-2.25f, 0f, 1.75f),
                    new Vector3(9.4f, 0f, 12.2f)
                };
                foreach (Vector3 position in positions)
                {
                    string expected = topography.GetBiomeIdAt(position);
                    Assert.That(worldQuery.GetBiomeIdForPosition(position), Is.EqualTo(expected), "WorldQueryRuntime at " + position);
                    Assert.That(director.GetBiomeIdForPosition(position), Is.EqualTo(expected), "EcosystemDirectorRuntime at " + position);
                    Assert.That(mapField.GetValue(topography), Is.SameAs(prebuiltMap),
                        "Gameplay biome lookups must use the built dense map, not rebuild/classify via noise.");
                }
            }
            finally
            {
                typeof(IslandTopographyRuntime).GetProperty("Active",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .GetSetMethod(true).Invoke(null, new object[] { null });
                Object.DestroyImmediate(go);
            }
        }
    }
}
