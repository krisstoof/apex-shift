using System.Collections.Generic;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class VegetationPlacementPlannerTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test]
        public void SameSeedProducesSamePlacementsRegardlessOfUnityRandomHistory_AndDifferentSeedChangesLayout()
        {
            Fixture fixture = CreateFixture();
            List<VegetationPlacement> first = Plan(fixture, 7412);
            Random.InitState(991);
            for (int i = 0; i < 1000; i++) Random.value.ToString();
            List<VegetationPlacement> repeated = Plan(fixture, 7412);
            List<VegetationPlacement> differentSeed = Plan(fixture, 7413);

            Assert.That(first.Count, Is.GreaterThan(0));
            Assert.That(repeated.Count, Is.EqualTo(first.Count));
            bool anyLayoutDifference = false;
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(repeated[i].SpeciesId, Is.EqualTo(first[i].SpeciesId));
                Assert.That(repeated[i].Position, Is.EqualTo(first[i].Position));
                Assert.That(repeated[i].Yaw, Is.EqualTo(first[i].Yaw));
                Assert.That(repeated[i].Scale, Is.EqualTo(first[i].Scale));
                Assert.That(repeated[i].InstanceId, Is.EqualTo(first[i].InstanceId));
                if (i >= differentSeed.Count || differentSeed[i].Position != first[i].Position) anyLayoutDifference = true;
            }
            Assert.That(anyLayoutDifference || differentSeed.Count != first.Count, Is.True);
        }

        [TestCase(false, true, false, 0.5f, 5f, 0.5f, "westwood", TestName = "WaterCandidatesAreRejected")]
        [TestCase(true, false, true, 0.5f, 5f, 0.5f, "westwood", TestName = "ShorelineCandidatesAreRejected")]
        [TestCase(true, false, false, 0.5f, 55f, 0.5f, "westwood", TestName = "SteepCandidatesAreRejected")]
        [TestCase(true, false, false, 0.95f, 5f, 0.5f, "westwood", TestName = "ElevationOutsideSpeciesRangeIsRejected")]
        [TestCase(true, false, false, 0.5f, 5f, 0.95f, "westwood", TestName = "MoistureOutsideSpeciesRangeIsRejected")]
        [TestCase(true, false, false, 0.5f, 5f, 0.5f, "south_thicket", TestName = "WrongDenseBiomeIsRejected")]
        public void EnvironmentConstraintsRejectInvalidCandidates(bool land, bool water, bool shoreline,
            float elevation, float slope, float moisture, string biome)
        {
            Fixture fixture = CreateFixture();
            VegetationEnvironmentSample environment = new VegetationEnvironmentSample(land, water, shoreline, biome, elevation, slope, moisture);
            Assert.That(Plan(fixture, 17, environment).Count, Is.EqualTo(0));
        }

        [Test]
        public void PlayerAndLandmarkClearingsRejectInsideAndAllowJustOutsideRadius()
        {
            Fixture fixture = CreateFixture();
            List<VegetationPlacement> baseline = Plan(fixture, 82);
            Assert.That(baseline.Count, Is.GreaterThan(0));
            VegetationPlacement target = baseline[0];

            List<VegetationPlacement> playerBlocked = Plan(fixture, 82, ValidEnvironment,
                target.Position, 2f, null);
            Assert.That(ContainsId(playerBlocked, target.InstanceId), Is.False);
            List<VegetationPlacement> playerOutside = Plan(fixture, 82, ValidEnvironment,
                target.Position + Vector3.right * 2.01f, 2f, null);
            Assert.That(ContainsId(playerOutside, target.InstanceId), Is.True);

            var insideLandmark = new[] { new VegetationLandmarkClearance(target.Position, 2f) };
            Assert.That(ContainsId(Plan(fixture, 82, ValidEnvironment, Vector3.one * 1000f, 0f, insideLandmark), target.InstanceId), Is.False);
            var outsideLandmark = new[] { new VegetationLandmarkClearance(target.Position + Vector3.right * 2.01f, 2f) };
            Assert.That(ContainsId(Plan(fixture, 82, ValidEnvironment, Vector3.one * 1000f, 0f, outsideLandmark), target.InstanceId), Is.True);
        }

        [Test]
        public void TreeAndDeadTreeShareMinimumSpacingGroup()
        {
            Fixture fixture = CreateFixture(includeDeadTree: true);
            fixture.Settings.ConfigureDensities(0.12f, 0.12f, 0f, 0f);
            List<VegetationPlacement> placements = Plan(fixture, 612);
            Assert.That(placements.Exists(p => p.Category == VegetationCategory.Tree), Is.True);
            Assert.That(placements.Exists(p => p.Category == VegetationCategory.DeadTree), Is.True);
            for (int i = 0; i < placements.Count; i++)
                for (int j = i + 1; j < placements.Count; j++)
                    if (IsTreeGroup(placements[i].Category) && IsTreeGroup(placements[j].Category))
                        Assert.That(Vector2.Distance(new Vector2(placements[i].Position.x, placements[i].Position.z),
                            new Vector2(placements[j].Position.x, placements[j].Position.z)), Is.GreaterThanOrEqualTo(5f - 0.001f));
        }

        [Test]
        public void JitterMovesPlacementsOffTheRegularCandidateCellCenters_AndHeightUsesSampler()
        {
            Fixture fixture = CreateFixture();
            const float baseDensity = 0.04f;
            fixture.Settings.ConfigureDensities(baseDensity, 0f, 0f, 0f);
            List<VegetationPlacement> placements = Plan(fixture, 51);
            float step = Mathf.Sqrt(1f / baseDensity);
            bool foundJitter = false;
            foreach (VegetationPlacement placement in placements)
            {
                float normalizedX = placement.Position.x / step;
                float normalizedZ = placement.Position.z / step;
                if (Mathf.Abs((normalizedX - Mathf.Floor(normalizedX)) - 0.5f) > 0.05f
                    || Mathf.Abs((normalizedZ - Mathf.Floor(normalizedZ)) - 0.5f) > 0.05f) foundJitter = true;
                Assert.That(placement.Position.y, Is.EqualTo(placement.Position.x * 0.1f + placement.Position.z * 0.05f).Within(0.0001f));
            }
            Assert.That(foundJitter, Is.True);
        }

        [Test]
        public void GroundCoverMustBeExplicitlyAllowedInsideStartClearing()
        {
            Fixture fixture = CreateFixture();
            fixture.Species.Configure("test_tree", "Test Tree", fixture.Visual, VegetationCategory.GroundCover,
                0.8f, 1.2f, 0.2f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                new[] { "westwood", "south_thicket" }, true, false, string.Empty, null, VegetationCollisionMode.None);
            fixture.Settings.ConfigureDensities(0f, 0f, 0f, 0.04f);
            List<VegetationPlacement> baseline = Plan(fixture, 99);
            Assert.That(baseline.Count, Is.GreaterThan(0));
            VegetationPlacement candidate = baseline[0];
            Assert.That(ContainsId(Plan(fixture, 99, ValidEnvironment, candidate.Position, 0.01f), candidate.InstanceId), Is.False);

            fixture.Settings.ConfigurePlacement(16f, 0.9f, true);
            List<VegetationPlacement> allowed = Plan(fixture, 99, ValidEnvironment, candidate.Position, 0.01f);
            Assert.That(ContainsId(allowed, candidate.InstanceId), Is.True);
        }

        private Fixture CreateFixture(bool includeDeadTree = false)
        {
            var visual = Track(new GameObject("test_vegetation_visual"));
            var species = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            species.Configure("test_tree", "Test Tree", visual, VegetationCategory.Tree,
                0.8f, 1.2f, includeDeadTree ? 5f : 1f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                new[] { "westwood", "south_thicket" }, true, true, "leafy_tree", null, VegetationCollisionMode.None);

            var biome = Track(ScriptableObject.CreateInstance<BiomeDefinitionAsset>());
            biome.Configure("westwood", "Westwood", Color.green, false, new List<VegetationSpawnEntryAsset>());
            var profile = Track(ScriptableObject.CreateInstance<BiomeVegetationProfileAsset>());
            var entries = new List<BiomeVegetationSpeciesEntry> { new BiomeVegetationSpeciesEntry(species, 1f, 1f) };
            if (includeDeadTree)
            {
                var deadTree = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
                deadTree.Configure("test_dead_tree", "Test Dead Tree", visual, VegetationCategory.DeadTree,
                    0.8f, 1.2f, 5f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                    new[] { "westwood" }, true, true, "dry_tree", null, VegetationCollisionMode.None);
                entries.Add(new BiomeVegetationSpeciesEntry(deadTree, 1f, 1f));
            }
            profile.Configure("westwood", 1f, entries);
            biome.SetVegetationProfile(profile);
            var catalog = Track(ScriptableObject.CreateInstance<BiomeCatalogAsset>());
            catalog.SetBiomes(new[] { biome });
            var settings = new VegetationGenerationSettings();
            settings.ConfigureDensities(0.04f, 0f, 0f, 0f);
            settings.ConfigurePlacement(16f, 0.9f, false);
            settings.ConfigureClearances(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            return new Fixture(catalog, settings, species, visual);
        }

        private List<VegetationPlacement> Plan(Fixture fixture, int seed,
            VegetationEnvironmentSample? environment = null, Vector3? playerSpawn = null,
            float clearingRadius = 0f, IReadOnlyList<VegetationLandmarkClearance> landmarks = null)
        {
            VegetationEnvironmentSample selectedEnvironment = environment ?? ValidEnvironment;
            var planner = new VegetationPlacementPlanner();
            return planner.Plan(seed, fixture.Catalog, fixture.Settings,
                new Bounds(Vector3.zero, new Vector3(40f, 1f, 40f)),
                _ => selectedEnvironment,
                position => position.x * 0.1f + position.z * 0.05f,
                playerSpawn ?? (Vector3.one * 1000f), clearingRadius, landmarks, null);
        }

        private static readonly VegetationEnvironmentSample ValidEnvironment =
            new VegetationEnvironmentSample(true, false, false, "westwood", 0.5f, 5f, 0.5f);

        private static bool ContainsId(List<VegetationPlacement> placements, string id)
        {
            foreach (VegetationPlacement placement in placements) if (placement.InstanceId == id) return true;
            return false;
        }

        private static bool IsTreeGroup(VegetationCategory category)
            => category == VegetationCategory.Tree || category == VegetationCategory.DeadTree;

        private T Track<T>(T value) where T : Object { owned.Add(value); return value; }

        private sealed class Fixture
        {
            public readonly BiomeCatalogAsset Catalog;
            public readonly VegetationGenerationSettings Settings;
            public readonly VegetationSpeciesAsset Species;
            public readonly GameObject Visual;
            public Fixture(BiomeCatalogAsset catalog, VegetationGenerationSettings settings, VegetationSpeciesAsset species, GameObject visual)
            { Catalog = catalog; Settings = settings; Species = species; Visual = visual; }
        }
    }
}
