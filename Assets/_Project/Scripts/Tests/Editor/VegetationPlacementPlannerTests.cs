using System.Collections.Generic;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Generation;
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
                Assert.That(repeated[i].HabitatId, Is.EqualTo(first[i].HabitatId));
                Assert.That(repeated[i].ChunkX, Is.EqualTo(first[i].ChunkX));
                Assert.That(repeated[i].ChunkZ, Is.EqualTo(first[i].ChunkZ));
                Assert.That(repeated[i].SpeciesId, Is.EqualTo(first[i].SpeciesId));
                Assert.That(repeated[i].Position, Is.EqualTo(first[i].Position));
                Assert.That(repeated[i].Yaw, Is.EqualTo(first[i].Yaw));
                Assert.That(repeated[i].Scale, Is.EqualTo(first[i].Scale));
                Assert.That(repeated[i].InstanceId, Is.EqualTo(first[i].InstanceId));
                if (i >= differentSeed.Count || differentSeed[i].Position != first[i].Position) anyLayoutDifference = true;
            }
            Assert.That(anyLayoutDifference || differentSeed.Count != first.Count, Is.True);
        }

        [TestCase(false, true, false, 0.5f, 5f, 0.5f, "jungle_interior", TestName = "WaterCandidatesAreRejected")]
        [TestCase(true, false, true, 0.5f, 5f, 0.5f, "jungle_interior", TestName = "ShorelineCandidatesAreRejected")]
        [TestCase(true, false, false, 0.5f, 55f, 0.5f, "jungle_interior", TestName = "SteepCandidatesAreRejected")]
        [TestCase(true, false, false, 0.95f, 5f, 0.5f, "jungle_interior", TestName = "ElevationOutsideSpeciesRangeIsRejected")]
        [TestCase(true, false, false, 0.5f, 5f, 0.95f, "jungle_interior", TestName = "MoistureOutsideSpeciesRangeIsRejected")]
        [TestCase(true, false, false, 0.5f, 5f, 0.5f, "wet_jungle", TestName = "WrongDenseHabitatIsRejected")]
        public void EnvironmentConstraintsRejectInvalidCandidates(bool land, bool water, bool shoreline,
            float elevation, float slope, float moisture, string biome)
        {
            Fixture fixture = CreateFixture();
            VegetationEnvironmentSample environment = new VegetationEnvironmentSample(land, water, shoreline, biome, elevation, slope, moisture);
            Assert.That(Plan(fixture, 17, environment).Count, Is.EqualTo(0));
        }

        [TestCase(VegetationCategory.Tree)]
        [TestCase(VegetationCategory.Shrub)]
        public void PlayerAndLandmarkClearingsRejectInsideAndAllowJustOutsideRadius(VegetationCategory category)
        {
            Fixture fixture = CreateFixture();
            fixture.Species.Configure("test_tree", "Test Clearing Vegetation", fixture.Visual, category,
                .8f, 1.2f, 1f, 0f, 40f, .1f, .9f, .1f, .9f,
                new[] { HabitatIds.JungleInterior }, true, false, "", null, VegetationCollisionMode.None);
            fixture.Settings.ConfigureDensities(.04f, 0f, .04f, 0f);
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
            float step = Mathf.Sqrt(1f / (baseDensity * 1.35f));
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
        public void GroundCoverWeightDoesNotChangeTreePlacements()
        {
            Fixture equalWeights = CreateProfileFixture("jungle_interior",
                new EntrySpec("test_tree", VegetationCategory.Tree, 1f),
                new EntrySpec("test_groundcover", VegetationCategory.GroundCover, 1f));
            Fixture heavyGroundCover = CreateProfileFixture("jungle_interior",
                new EntrySpec("test_tree", VegetationCategory.Tree, 1f),
                new EntrySpec("test_groundcover", VegetationCategory.GroundCover, 100f));

            List<VegetationPlacement> first = Plan(equalWeights, 1204, new Bounds(Vector3.zero, new Vector3(240f, 1f, 240f)));
            List<VegetationPlacement> second = Plan(heavyGroundCover, 1204, new Bounds(Vector3.zero, new Vector3(240f, 1f, 240f)));
            List<VegetationPlacement> firstTrees = FilterCategory(first, VegetationCategory.Tree);
            List<VegetationPlacement> secondTrees = FilterCategory(second, VegetationCategory.Tree);

            Assert.That(secondTrees.Count, Is.EqualTo(firstTrees.Count));
            for (int i = 0; i < firstTrees.Count; i++)
            {
                Assert.That(secondTrees[i].InstanceId, Is.EqualTo(firstTrees[i].InstanceId));
                Assert.That(secondTrees[i].Position, Is.EqualTo(firstTrees[i].Position));
            }
        }

        [Test]
        public void TreeSpeciesWeightsInfluenceCompositionWithinCategory()
        {
            Fixture fixture = CreateProfileFixture("jungle_interior",
                new EntrySpec("test_conifer", VegetationCategory.Tree, 3f),
                new EntrySpec("test_leafy", VegetationCategory.Tree, 1f));
            List<VegetationPlacement> placements = Plan(fixture, 1205,
                new Bounds(Vector3.zero, new Vector3(600f, 1f, 600f)));
            int conifers = CountSpecies(placements, "test_conifer");
            int leafy = CountSpecies(placements, "test_leafy");

            Assert.That(leafy, Is.GreaterThan(0));
            Assert.That(conifers, Is.GreaterThan(leafy * 2), $"Expected the 3:1 profile weight to clearly favor conifers; got {conifers}:{leafy}.");
        }

        [Test]
        public void HabitatProfilesProduceExpectedRelativeCategoryDensities()
        {
            const int seed = 8512;
            var bounds = new Bounds(Vector3.zero, new Vector3(600f, 1f, 600f));
            int interiorTrees = CountFor("jungle_interior", VegetationCategory.Tree, seed, bounds);
            int ridgeTrees = CountFor("rocky_upland", VegetationCategory.Tree, seed, bounds);
            int coastTrees = CountFor("coast", VegetationCategory.Tree, seed, bounds);
            int wetShrubs = CountFor("wet_jungle", VegetationCategory.Shrub, seed, bounds);
            int interiorShrubs = CountFor("jungle_interior", VegetationCategory.Shrub, seed, bounds);
            int wetGroundcover = CountFor("wet_jungle", VegetationCategory.GroundCover, seed, bounds);
            int coastGroundcover = CountFor("coast", VegetationCategory.GroundCover, seed, bounds);

            Assert.That(interiorTrees, Is.GreaterThan(ridgeTrees));
            Assert.That(interiorTrees, Is.GreaterThan(coastTrees));
            Assert.That(wetShrubs, Is.GreaterThan(interiorShrubs));
            Assert.That(wetGroundcover, Is.GreaterThan(coastGroundcover));
        }

        [Test]
        public void DenseForestDefaultsAndCategoryStatisticsAreExposed()
        {
            var settings = new VegetationGenerationSettings();
            Assert.That(settings.TreeBaseDensity, Is.EqualTo(0.016f).Within(0.000001f));
            Assert.That(settings.DeadTreeBaseDensity, Is.EqualTo(0.0025f).Within(0.000001f));
            Assert.That(settings.ShrubBaseDensity, Is.EqualTo(0.030f).Within(0.000001f));
            Assert.That(settings.GroundCoverBaseDensity, Is.EqualTo(0.075f).Within(0.000001f));

            var result = new WorldGenerationResult();
            result.RecordVegetation("jungle_interior", "test_tree", VegetationCategory.Tree);
            result.RecordVegetation("jungle_interior", "test_dead_tree", VegetationCategory.DeadTree);
            result.RecordVegetation("jungle_interior", "test_shrub", VegetationCategory.Shrub);
            result.RecordVegetation("jungle_interior", "test_groundcover", VegetationCategory.GroundCover);
            Assert.That(result.VegetationInstanceCount, Is.EqualTo(4));
            Assert.That(result.GetVegetationCount("jungle_interior", "test_tree"), Is.EqualTo(1));
            Assert.That(result.GetVegetationCount("jungle_interior", VegetationCategory.Tree), Is.EqualTo(1));
            Assert.That(result.GetVegetationCount("jungle_interior", VegetationCategory.DeadTree), Is.EqualTo(1));
            Assert.That(result.GetVegetationCount("jungle_interior", VegetationCategory.Shrub), Is.EqualTo(1));
            Assert.That(result.GetVegetationCount("jungle_interior", VegetationCategory.GroundCover), Is.EqualTo(1));
        }

        [Test]
        public void GroundCoverMustBeExplicitlyAllowedInsideStartClearing()
        {
            Fixture fixture = CreateFixture();
            fixture.Species.Configure("test_tree", "Test Tree", fixture.Visual, VegetationCategory.GroundCover,
                0.8f, 1.2f, 0.2f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                new[] { "jungle_interior", "wet_jungle" }, true, false, string.Empty, null, VegetationCollisionMode.None);
            fixture.Settings.ConfigureDensities(0f, 0f, 0f, 0.04f);
            List<VegetationPlacement> baseline = Plan(fixture, 99);
            Assert.That(baseline.Count, Is.GreaterThan(0));
            VegetationPlacement candidate = baseline[0];
            Assert.That(ContainsId(Plan(fixture, 99, ValidEnvironment, candidate.Position, 0.01f), candidate.InstanceId), Is.False);

            fixture.Settings.ConfigurePlacement(16f, 0.9f, true);
            List<VegetationPlacement> allowed = Plan(fixture, 99, ValidEnvironment, candidate.Position, 0.01f);
            Assert.That(ContainsId(allowed, candidate.InstanceId), Is.True);
        }

        [Test]
        public void CoastProfileHonorsHabitatSpeciesDistanceAndCategoryClearance()
        {
            Fixture fixture = CreateProfileFixture(HabitatIds.Coast, new EntrySpec("coast_tree", VegetationCategory.Tree, 1f));
            fixture.Species.ConfigureEnvironment(VegetationForm.CanopyTree, 2f, 10f, new[] { TerrainType.Beach });
            fixture.Settings.ConfigureClearances(4f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            VegetationEnvironmentSample Sample(string habitat, float distance) =>
                new VegetationEnvironmentSample(true, false, false, habitat, .5f, 5f, .5f, TerrainType.Beach, distance);

            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 5f)), Is.Not.Empty);
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.WetJungle, 5f)), Is.Empty);
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 1f)), Is.Empty, "Species minimum");
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 3f)), Is.Empty, "Category coastal clearance");
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 11f)), Is.Empty, "Species maximum");
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 4f)), Is.Not.Empty, "Clearance boundary is inclusive");
            Assert.That(Plan(fixture, 81, Sample(HabitatIds.Coast, 10f)), Is.Not.Empty, "Species maximum is inclusive");
        }

        [Test]
        public void WetJungleSupportsDenseValidCanopyButRejectsRockyTerrainAndSlope()
        {
            Fixture fixture = CreateProfileFixture(HabitatIds.WetJungle, new EntrySpec("wet_canopy", VegetationCategory.Tree, 1f));
            fixture.Species.ConfigureEnvironment(VegetationForm.CanopyTree, 3f, 100f, new[] { TerrainType.Forest });
            VegetationEnvironmentSample Sample(TerrainType terrain, float slope, bool land = true, bool water = false) =>
                new VegetationEnvironmentSample(land, water, false, HabitatIds.WetJungle, .4f, slope, .85f, terrain, 20f);
            Assert.That(Plan(fixture, 56, Sample(TerrainType.Forest, 5f)), Is.Not.Empty);
            Assert.That(Plan(fixture, 56, Sample(TerrainType.Ridge, 5f)), Is.Empty);
            Assert.That(Plan(fixture, 56, Sample(TerrainType.Forest, 91f)), Is.Empty);
            Assert.That(Plan(fixture, 56, Sample(TerrainType.Water, 0f, false, true)), Is.Empty);
        }

        [Test]
        public void InstanceIdentityAtSamePositionDoesNotDependOnHabitatName()
        {
            Fixture first = CreateProfileFixture(HabitatIds.LowlandJungle, new EntrySpec("same_tree", VegetationCategory.Tree, 1f));
            Fixture second = CreateProfileFixture(HabitatIds.WetJungle, new EntrySpec("same_tree", VegetationCategory.Tree, 1f));
            foreach (Fixture fixture in new[] { first, second })
            {
                fixture.Catalog.Profiles[0].Configure(fixture.Catalog.Profiles[0].HabitatId, 1f,
                    new[] { new HabitatVegetationSpeciesEntry(fixture.Species, 1f, 1f) });
                fixture.Settings.ConfigurePlacement(24f, 0f, false);
            }
            var bounds = new Bounds(Vector3.zero, new Vector3(120f, 1f, 120f));
            List<VegetationPlacement> a = Plan(first, 782, bounds,
                new VegetationEnvironmentSample(true, false, false, HabitatIds.LowlandJungle, .5f, 0f, .5f));
            List<VegetationPlacement> b = Plan(second, 782, bounds,
                new VegetationEnvironmentSample(true, false, false, HabitatIds.WetJungle, .5f, 0f, .5f));
            var idsByPosition = new Dictionary<Vector3, string>();
            foreach (VegetationPlacement placement in a) idsByPosition.Add(placement.Position, placement.InstanceId);
            int common = 0;
            foreach (VegetationPlacement placement in b)
                if (idsByPosition.TryGetValue(placement.Position, out string id))
                {
                    Assert.That(placement.InstanceId, Is.EqualTo(id));
                    common++;
                }
            Assert.That(common, Is.GreaterThan(0));
        }

        private Fixture CreateFixture(bool includeDeadTree = false)
        {
            var visual = Track(new GameObject("test_vegetation_visual"));
            var species = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            species.Configure("test_tree", "Test Tree", visual, VegetationCategory.Tree,
                0.8f, 1.2f, includeDeadTree ? 5f : 1f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                new[] { "jungle_interior", "wet_jungle" }, true, true, "leafy_tree", null, VegetationCollisionMode.None);

            var profile = Track(ScriptableObject.CreateInstance<HabitatVegetationProfileAsset>());
            var entries = new List<HabitatVegetationSpeciesEntry> { new HabitatVegetationSpeciesEntry(species, 1f, 1f) };
            if (includeDeadTree)
            {
                var deadTree = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
                deadTree.Configure("test_dead_tree", "Test Dead Tree", visual, VegetationCategory.DeadTree,
                    0.8f, 1.2f, 5f, 0f, 40f, 0.1f, 0.9f, 0.1f, 0.9f,
                    new[] { "jungle_interior" }, true, true, "dry_tree", null, VegetationCollisionMode.None);
                entries.Add(new HabitatVegetationSpeciesEntry(deadTree, 1f, 1f));
            }
            profile.Configure("jungle_interior", 1f, entries);
            var catalog = Track(ScriptableObject.CreateInstance<HabitatVegetationCatalogAsset>());
            catalog.SetProfiles(new[] { profile });
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
            return Plan(fixture, seed, new Bounds(Vector3.zero, new Vector3(40f, 1f, 40f)),
                environment, playerSpawn, clearingRadius, landmarks);
        }

        private List<VegetationPlacement> Plan(Fixture fixture, int seed, Bounds bounds,
            VegetationEnvironmentSample? environment = null, Vector3? playerSpawn = null,
            float clearingRadius = 0f, IReadOnlyList<VegetationLandmarkClearance> landmarks = null)
        {
            VegetationEnvironmentSample selectedEnvironment = environment ?? ValidEnvironment;
            var planner = new VegetationPlacementPlanner();
            return planner.Plan(seed, fixture.Catalog, fixture.Settings,
                bounds,
                _ => selectedEnvironment,
                position => position.x * 0.1f + position.z * 0.05f,
                playerSpawn ?? (Vector3.one * 1000f), clearingRadius, landmarks);
        }

        private Fixture CreateProfileFixture(string habitatId, params EntrySpec[] specs)
        {
            var visual = Track(new GameObject("profile_test_visual"));
            var entries = new List<HabitatVegetationSpeciesEntry>();
            VegetationSpeciesAsset firstSpecies = null;
            for (int i = 0; i < specs.Length; i++)
            {
                var species = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
                species.Configure(specs[i].Id, specs[i].Id, visual, specs[i].Category,
                    1f, 1f, 0.05f, 0f, 90f, 0f, 1f, 0f, 1f,
                    HabitatVegetationProfileAsset.CanonicalHabitatIds, true, false, string.Empty, null, VegetationCollisionMode.None);
                firstSpecies ??= species;
                entries.Add(new HabitatVegetationSpeciesEntry(species, specs[i].Weight, 1f));
            }

            var profile = Track(ScriptableObject.CreateInstance<HabitatVegetationProfileAsset>());
            profile.Configure(habitatId, ProfileDensity(habitatId), entries);
            var catalog = Track(ScriptableObject.CreateInstance<HabitatVegetationCatalogAsset>());
            catalog.SetProfiles(new[] { profile });
            var settings = new VegetationGenerationSettings();
            settings.ConfigureDensities(0.016f, 0.0025f, 0.030f, 0.075f);
            settings.ConfigurePlacement(24f, 0.88f, false);
            settings.ConfigureClearances(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            return new Fixture(catalog, settings, firstSpecies, visual);
        }

        private int CountFor(string habitatId, VegetationCategory category, int seed, Bounds bounds)
        {
            Fixture fixture = CreateProfileFixture(habitatId, new EntrySpec("test_" + category, category, 1f));
            var environment = new VegetationEnvironmentSample(true, false, false, habitatId, 0.5f, 0f, 0.5f);
            return FilterCategory(Plan(fixture, seed, bounds, environment), category).Count;
        }

        private static float ProfileDensity(string habitatId)
        {
            switch (habitatId)
            {
                case "coast": return 0.45f;
                case "jungle_interior": return 1.85f;
                case "wet_jungle": return 2.10f;
                case "rocky_upland": return 0.45f;
                case "lowland_jungle": return 1.55f;
                default: return 1f;
            }
        }

        private static List<VegetationPlacement> FilterCategory(List<VegetationPlacement> placements, VegetationCategory category)
            => placements.FindAll(p => p.Category == category);

        private static int CountSpecies(List<VegetationPlacement> placements, string speciesId)
            => placements.FindAll(p => p.SpeciesId == speciesId).Count;

        private static readonly VegetationEnvironmentSample ValidEnvironment =
            new VegetationEnvironmentSample(true, false, false, "jungle_interior", 0.5f, 5f, 0.5f);

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
            public readonly HabitatVegetationCatalogAsset Catalog;
            public readonly VegetationGenerationSettings Settings;
            public readonly VegetationSpeciesAsset Species;
            public readonly GameObject Visual;
            public Fixture(HabitatVegetationCatalogAsset catalog, VegetationGenerationSettings settings, VegetationSpeciesAsset species, GameObject visual)
            { Catalog = catalog; Settings = settings; Species = species; Visual = visual; }
        }

        private readonly struct EntrySpec
        {
            public readonly string Id;
            public readonly VegetationCategory Category;
            public readonly float Weight;
            public EntrySpec(string id, VegetationCategory category, float weight)
            { Id = id; Category = category; Weight = weight; }
        }
    }
}
