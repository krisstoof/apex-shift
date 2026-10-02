using NUnit.Framework;
using UnityEngine;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Environment;

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

        [Test]
        public void HabitatClassifierUsesPhysicalClimateFieldsAndLegacyAdapterIsExplicit()
        {
            var classifier = new HabitatClassifier(new HabitatClassificationSettings());
            Assert.AreEqual(HabitatIds.Water, classifier.Classify(false, false, 0f, TerrainType.Water, 0f, 0f, .5f));
            Assert.AreEqual(HabitatIds.Coast, classifier.Classify(true, true, 0f, TerrainType.Beach, .1f, 1f, .5f));
            Assert.AreEqual(HabitatIds.RockyUpland, classifier.Classify(true, false, 80f, TerrainType.Ridge, .8f, 35f, .3f));
            Assert.AreEqual(HabitatIds.WetJungle, classifier.Classify(true, false, 80f, TerrainType.Forest, .6f, 4f, .9f));
            Assert.AreEqual(HabitatIds.LowlandJungle, classifier.Classify(true, false, 80f, TerrainType.Plain, .2f, 4f, .3f));
            Assert.AreEqual(HabitatIds.JungleInterior, classifier.Classify(true, false, 80f, TerrainType.Plain, .6f, 4f, .3f));
            Assert.AreEqual("south_thicket", LegacyBiomeCompatibility.ToLegacyBiomeId(HabitatIds.LowlandJungle));
        }

        [Test]
        public void SafePlayerSpawnIsDeterministicAndNearCoastWithoutUsingOriginIdentity()
        {
            GameObject go = new GameObject("HabitatSpawnTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(30, 4f, (x, z) => x * x + z * z <= 48f * 48f, _ => 0.2f, null);
                Vector3 first = topography.GetSafePlayerSpawnPoint();
                Vector3 second = topography.GetSafePlayerSpawnPoint();
                Assert.AreEqual(first, second);
                TopographyCell cell = topography.GetCellAt(first);
                Assert.IsNotNull(cell);
                Assert.IsTrue(cell.IsSafeForPlayerSpawn);
                Assert.That(cell.DistanceToCoast, Is.GreaterThan(16f));
            }
            finally { Object.DestroyImmediate(go); }
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

                topography.Build(4, 1f, (x, z) => x < 0f, p => p.x * 10f, null);
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
                var classifier = new ApexShift.Runtime.World.Environment.HabitatClassifier(new ApexShift.Runtime.World.Environment.HabitatClassificationSettings());
                topography.Build(8, 4f, (x, z) => true, p => p.x * 0.01f, null,
                    biomeField: field, habitatClassifier: classifier);
                typeof(IslandTopographyRuntime).GetProperty("Active",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .GetSetMethod(true).Invoke(null, new object[] { topography });
                director.InitializeFromRegions(null);

                Assert.That(topography.DenseHabitatMapResolutionPerTile, Is.EqualTo(12));
                Assert.That(topography.DenseHabitatMapCellSize, Is.EqualTo(4f / 12f).Within(0.00001f));
                var mapField = typeof(IslandTopographyRuntime).GetField("_habitatMap",
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
                    string expected = topography.GetHabitatIdAt(position);
                    Assert.That(worldQuery.GetHabitatIdForPosition(position), Is.EqualTo(expected), "WorldQueryRuntime at " + position);
                    Assert.That(topography.TryGetEnvironmentAt(position, out ApexShift.Runtime.World.Environment.EnvironmentSample sample), Is.True);
                    Assert.That(sample.HabitatId, Is.EqualTo(expected));
                    string legacy = topography.GetBiomeIdAt(position);
                    Assert.That(worldQuery.GetBiomeIdForPosition(position), Is.EqualTo(legacy), "Legacy WorldQueryRuntime adapter at " + position);
                    Assert.That(director.GetBiomeIdForPosition(position), Is.EqualTo(legacy), "Legacy EcosystemDirectorRuntime adapter at " + position);
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

        [Test]
        public void LandWaterAndShorelineQueriesUseDenseStatesWithinSameCoarseCell()
        {
            GameObject go = new GameObject("DenseCoastQueryContractTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                topography.Build(4, 4f, (x, z) => x < 1.2f, _ => 0f, null);

                Vector3 interior = new Vector3(0.5f, 0f, 1.5f);
                Vector3 shore = new Vector3(1.1f, 0f, 1.5f);
                Vector3 water = new Vector3(1.5f, 0f, 1.5f);
                TopographyCell coarse = topography.GetCellAt(interior);
                Assert.That(topography.GetCellAt(shore), Is.SameAs(coarse));
                Assert.That(topography.GetCellAt(water), Is.SameAs(coarse));
                Assert.That(coarse.IsWater, Is.True,
                    "The coarse cell center is water even though it contains dense land samples.");

                Assert.That(topography.IsLandAt(interior.x, interior.z), Is.True);
                Assert.That(topography.IsWaterAt(interior.x, interior.z), Is.False);
                Assert.That(topography.IsShorelineAt(interior.x, interior.z), Is.False);
                Assert.That(topography.IsLandAt(shore.x, shore.z), Is.True);
                Assert.That(topography.IsShorelineAt(shore.x, shore.z), Is.True);
                Assert.That(topography.IsWaterAt(water.x, water.z), Is.True);
                Assert.That(topography.IsLandAt(water.x, water.z), Is.False);

                foreach (Vector3 position in new[] { interior, shore, water })
                {
                    Assert.That(topography.TryGetEnvironmentAt(position,
                        out ApexShift.Runtime.World.Environment.EnvironmentSample sample), Is.True);
                    Assert.That(topography.IsLandAt(position.x, position.z), Is.EqualTo(sample.IsLand));
                    Assert.That(topography.IsWaterAt(position.x, position.z), Is.EqualTo(sample.IsWater));
                    Assert.That(topography.IsShorelineAt(position.x, position.z), Is.EqualTo(sample.IsShoreline));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DenseEnvironmentQueriesMatchCachedSamplesWithinCoarseCells()
        {
            GameObject go = new GameObject("DenseEnvironmentQueryContractTest");
            try
            {
                IslandTopographyRuntime topography = go.AddComponent<IslandTopographyRuntime>();
                var settings = new ApexShift.Runtime.World.Environment.HabitatClassificationSettings();
                settings.Configure(16f, .67f, 28f, .67f, .43f);
                topography.Build(20, 8f, (x, z) => x * x + z * z <= 62f * 62f,
                    p => p.x * 0.5f, null,
                    habitatSettings: settings,
                    habitatClassifier: new ApexShift.Runtime.World.Environment.HabitatClassifier(settings));

                Vector3 coastNear = new Vector3(41f, 0f, 1f);
                Vector3 coastFar = new Vector3(43f, 0f, 1f);
                Assert.That(topography.GetCellAt(coastNear), Is.SameAs(topography.GetCellAt(coastFar)));
                Assert.That(topography.TryGetEnvironmentAt(coastNear,
                    out ApexShift.Runtime.World.Environment.EnvironmentSample nearSample), Is.True);
                Assert.That(topography.TryGetEnvironmentAt(coastFar,
                    out ApexShift.Runtime.World.Environment.EnvironmentSample farSample), Is.True);
                Assert.That(nearSample.DistanceToCoast, Is.Not.EqualTo(farSample.DistanceToCoast));

                bool foundTerrainTransitionInSingleCoarseCell = false;
                float denseStep = topography.DenseHabitatMapCellSize;
                for (float z = -10f; z < 10f && !foundTerrainTransitionInSingleCoarseCell; z += denseStep)
                    for (float x = 20f; x < 32f && !foundTerrainTransitionInSingleCoarseCell; x += denseStep)
                    {
                        Vector3 a = new Vector3(x, 0f, z);
                        Vector3 b = new Vector3(x + denseStep, 0f, z);
                        if (!ReferenceEquals(topography.GetCellAt(a), topography.GetCellAt(b))) continue;
                        if (!topography.TryGetEnvironmentAt(a,
                                out ApexShift.Runtime.World.Environment.EnvironmentSample sampleA) ||
                            !topography.TryGetEnvironmentAt(b,
                                out ApexShift.Runtime.World.Environment.EnvironmentSample sampleB) ||
                            sampleA.TerrainType == sampleB.TerrainType) continue;
                        Assert.That(topography.GetTerrainTypeAt(a), Is.EqualTo(sampleA.TerrainType));
                        Assert.That(topography.GetTerrainTypeAt(b), Is.EqualTo(sampleB.TerrainType));
                        foundTerrainTransitionInSingleCoarseCell = true;
                    }
                Assert.That(foundTerrainTransitionInSingleCoarseCell, Is.True,
                    "Expected a dense terrain transition inside one coarse topography cell.");

                foreach (Vector3 position in new[] { coastNear, coastFar })
                {
                    Assert.That(topography.TryGetEnvironmentAt(position,
                        out ApexShift.Runtime.World.Environment.EnvironmentSample sample), Is.True);
                    Assert.That(topography.GetDistanceToCoastAt(position), Is.EqualTo(sample.DistanceToCoast));
                    Assert.That(topography.GetTerrainTypeAt(position), Is.EqualTo(sample.TerrainType));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
